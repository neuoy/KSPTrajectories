/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2014-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2020 S.Gray, (aka PiezPiedPy).

  This file is part of Trajectories.
  Trajectories is available under the terms of GPL-3.0-or-later.
  See the LICENSE.md file for more details.

  Trajectories is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  Trajectories is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

  You should have received a copy of the GNU General Public License
  along with Trajectories.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Trajectories
{
    /// <summary>
    /// Handles trajectory prediction, performing a lightweight physical simulation to
    /// predict a vessels trajectory in space and atmosphere.
    /// </summary>
    internal static class Trajectory
    {
        internal class VesselState
        {
            internal CelestialBody ReferenceBody { get; set; }

            // universal time
            internal double Time { get; set; }

            // position in world frame relatively to the reference body
            internal Vector3d Position { get; set; }

            // velocity in world frame relatively to the reference body
            internal Vector3d Velocity { get; set; }

            // tells whether the patch starting from this state is superimposed on a stock KSP patch, or null if
            // something makes it diverge (atmospheric entry for example)
            internal Orbit StockPatch { get; set; }

            internal VesselState() { }
        }

        internal struct Point
        {
            internal Vector3d pos;
            internal Vector3d aerodynamicForce;
            internal Vector3d orbitalVelocity;

            /// <summary>
            /// Ground altitude above (or under) sea level, in meters.
            /// </summary>
            internal double groundAltitude;

            /// <summary>
            /// Universal time
            /// </summary>
            internal double time;
        }

        internal class Patch
        {
            internal VesselState StartingState { get; set; }

            internal double EndTime { get; set; }

            internal bool IsAtmospheric { get; set; }

            // // position array in body space (world frame centered on the body) ; only used when isAtmospheric is true
            internal Point[] AtmosphericTrajectory { get; set; }

            // only used when isAtmospheric is false
            internal Orbit SpaceOrbit { get; set; }

            internal Vector3d? ImpactPosition { get; set; }

            internal Vector3d? RawImpactPosition { get; set; }

            internal Vector3d? ImpactVelocity { get; set; }
        }

        internal const double INTEGRATOR_MIN = 0.1d;         // RK4 Integrator minimum step size
        internal const double INTEGRATOR_MAX = 5.0d;         // RK4 Integrator maximum step size

        private static List<Patch> patches_buffer = new List<Patch>();
        private static List<Patch> patches_swap_buffer;

        internal static List<Patch> Patches { get; private set; } = new List<Patch>();

        private static double max_accel_buffer;

        internal static double MaxAccel { get; private set; }

        internal static int ErrorCount { get; private set; }

        private static double calculation_time;

        ///<summary> Trajectory calculation time in ms </summary>
        internal static double ComputationTime { get; private set; }

        private static double patches_calculated;

        ///<summary> Trajectory patches calculated </summary>
        internal static double CalculatedPatches { get; private set; }

        ///<summary> Progress percentage of Trajectory calculation </summary>
        internal static double Progress { get; set; }

        internal static void Start() => Util.DebugLog("Constructing");

#if DEBUG_TELEMETRY
            ConstructTelemetry();
#endif

        internal static void Destroy() => Util.DebugLog("");

        internal static void Update()
        {
            // should the trajectory be calculated?
            if (!Worker.Busy && (Settings.DisplayTrajectories || Settings.AlwaysUpdate || TargetProfile.WorldPosition.HasValue))
                ComputeTrajectory();
        }

        private static void UpdateTiming()
        {
            // compute computation time
            ComputationTime = ComputationTime * 0.9d + calculation_time * 0.1d;

            // compute total patches calculated
            CalculatedPatches = CalculatedPatches * 0.5d + patches_calculated * 0.5d;
        }

#if DEBUG_TELEMETRY

        internal static void ConstructTelemetry()
        {
            // Add telemetry channels for real and predicted variable values
            Telemetry.AddChannel<double>("ut");
            Telemetry.AddChannel<double>("altitude");
            Telemetry.AddChannel<double>("airspeed");
            Telemetry.AddChannel<double>("aoa");
            Telemetry.AddChannel<float>("drag");

            Telemetry.AddChannel<double>("density");
            Telemetry.AddChannel<double>("density_calc");
            Telemetry.AddChannel<double>("density_calc_precise");

            Telemetry.AddChannel<double>("temperature");
            Telemetry.AddChannel<double>("temperature_calc");

            Telemetry.AddChannel<double>("force_actual");
            Telemetry.AddChannel<double>("force_actual.x");
            Telemetry.AddChannel<double>("force_actual.y");
            Telemetry.AddChannel<double>("force_actual.z");
            //Telemetry.AddChannel<double>("force_total");
            Telemetry.AddChannel<double>("force_predicted");
            Telemetry.AddChannel<double>("force_predicted.x");
            Telemetry.AddChannel<double>("force_predicted.y");
            Telemetry.AddChannel<double>("force_predicted.z");
            Telemetry.AddChannel<double>("force_predicted_cache");
            //Telemetry.AddChannel<double>("force_reference");
        }

        private static Vector3d PreviousFramePos;
        private static Vector3d PreviousFrameVelocity;
        private static double PreviousFrameTime = 0;

        internal static void DebugTelemetry()
        {
            if (!Util.IsFlight)
                return;

            double now = Planetarium.GetUniversalTime();
            double dt = now - PreviousFrameTime;

            if (dt > 0.5 || dt < 0.0)
            {
                Vector3d bodySpacePosition = new Vector3d();
                Vector3d bodySpaceVelocity = new Vector3d();

                if (aerodynamicModel_ != null && Trajectories.IsVesselAttached)
                {
                    CelestialBody body = Trajectories.AttachedVessel.orbit.referenceBody;

                    bodySpacePosition = Trajectories.AttachedVessel.GetWorldPos3D() - body.position;
                    bodySpaceVelocity = Trajectories.AttachedVessel.obt_velocity;

                    double altitudeAboveSea = bodySpacePosition.magnitude - body.Radius;

                    Vector3d airVelocity = bodySpaceVelocity - body.getRFrmVel(body.position + bodySpacePosition);

                    double R = PreviousFramePos.magnitude;
                    Vector3d gravityForce = PreviousFramePos * (-body.gravParameter / (R * R * R) * Trajectories.AttachedVessel.totalMass);

                    Quaternion inverseRotationFix = body.inverseRotation ?
                        Quaternion.AngleAxis((float)(body.angularVelocity.magnitude / Math.PI * 180.0 * dt), Vector3.up)
                        : Quaternion.identity;
                    Vector3d TotalForce = (bodySpaceVelocity - inverseRotationFix * PreviousFrameVelocity) * (Trajectories.AttachedVessel.totalMass / dt);
                    TotalForce += bodySpaceVelocity * (dt * 0.000015); // numeric precision fix
                    Vector3d ActualForce = TotalForce - gravityForce;

                    Transform vesselTransform = Trajectories.AttachedVessel.ReferenceTransform;
                    Vector3d vesselBackward = (Vector3d)(-vesselTransform.up.normalized);
                    Vector3d vesselForward = -vesselBackward;
                    Vector3d vesselUp = (Vector3d)(-vesselTransform.forward.normalized);
                    Vector3d vesselRight = Vector3d.Cross(vesselUp, vesselBackward).normalized;
                    double AoA = Math.Acos(Vector3d.Dot(airVelocity.normalized, vesselForward.normalized));
                    if (Vector3d.Dot(airVelocity, vesselUp) > 0)
                        AoA = -AoA;

                    VesselAerodynamicModel.DebugParts = true;
                    Vector3d referenceForce = aerodynamicModel_.ComputeForces(20000, new Vector3d(0, 0, 1500), new Vector3d(0, 1, 0), 0);
                    VesselAerodynamicModel.DebugParts = false;

                    Vector3d predictedForce = aerodynamicModel_.ComputeForces(altitudeAboveSea, airVelocity, vesselUp, AoA);
                    //VesselAerodynamicModel.Verbose = true;
                    Vector3d predictedForceWithCache = aerodynamicModel_.GetForces(body, bodySpacePosition, airVelocity, AoA);
                    //VesselAerodynamicModel.Verbose = false;

                    Vector3d localTotalForce = new Vector3d(
                        Vector3d.Dot(TotalForce, vesselRight),
                        Vector3d.Dot(TotalForce, vesselUp),
                        Vector3d.Dot(TotalForce, vesselBackward));
                    Vector3d localActualForce = new Vector3d(
                        Vector3d.Dot(ActualForce, vesselRight),
                        Vector3d.Dot(ActualForce, vesselUp),
                        Vector3d.Dot(ActualForce, vesselBackward));
                    Vector3d localPredictedForce = new Vector3d(
                        Vector3d.Dot(predictedForce, vesselRight),
                        Vector3d.Dot(predictedForce, vesselUp),
                        Vector3d.Dot(predictedForce, vesselBackward));
                    Vector3d localPredictedForceWithCache = new Vector3d(
                        Vector3d.Dot(predictedForceWithCache, vesselRight),
                        Vector3d.Dot(predictedForceWithCache, vesselUp),
                        Vector3d.Dot(predictedForceWithCache, vesselBackward));

                    Telemetry.Send("ut", now);
                    Telemetry.Send("altitude", Trajectories.AttachedVessel.altitude);

                    Telemetry.Send("airspeed", Math.Floor(airVelocity.magnitude));
                    Telemetry.Send("aoa", (AoA * 180.0 / Math.PI));

                    Telemetry.Send("force_actual", localActualForce.magnitude);
                    Telemetry.Send("force_actual.x", localActualForce.x);
                    Telemetry.Send("force_actual.y", localActualForce.y);
                    Telemetry.Send("force_actual.z", localActualForce.z);


                    //Telemetry.Send("force_total", localTotalForce.magnitude);
                    //Telemetry.Send("force_total.x", localTotalForce.x);
                    //Telemetry.Send("force_total.y", localTotalForce.y);
                    //Telemetry.Send("force_total.z", localTotalForce.z);

                    Telemetry.Send("force_predicted", localPredictedForce.magnitude);
                    Telemetry.Send("force_predicted.x", localPredictedForce.x);
                    Telemetry.Send("force_predicted.y", localPredictedForce.y);
                    Telemetry.Send("force_predicted.z", localPredictedForce.z);

                    Telemetry.Send("force_predicted_cache", localPredictedForceWithCache.magnitude);
                    //Telemetry.Send("force_predicted_cache.x", localPredictedForceWithCache.x);
                    //Telemetry.Send("force_predicted_cache.y", localPredictedForceWithCache.y);
                    //Telemetry.Send("force_predicted_cache.z", localPredictedForceWithCache.z);

                    //Telemetry.Send("force_reference", referenceForce.magnitude);
                    //Telemetry.Send("force_reference.x", referenceForce.x);
                    //Telemetry.Send("force_reference.y", referenceForce.y);
                    //Telemetry.Send("force_reference.z", referenceForce.z);

                    //Telemetry.Send("velocity.x", bodySpaceVelocity.x);
                    //Telemetry.Send("velocity.y", bodySpaceVelocity.y);
                    //Telemetry.Send("velocity.z", bodySpaceVelocity.z);

                    //Vector3d velocity_pos = (bodySpacePosition - PreviousFramePos) / dt;
                    //Telemetry.Send("velocity_pos.x", velocity_pos.x);
                    //Telemetry.Send("velocity_pos.y", velocity_pos.y);
                    //Telemetry.Send("velocity_pos.z", velocity_pos.z);

                    Telemetry.Send("drag", Trajectories.AttachedVessel.rootPart.rb.drag);

                    Telemetry.Send("density", Trajectories.AttachedVessel.atmDensity);
                    Telemetry.Send("density_calc", StockAeroUtil.GetDensity(altitudeAboveSea, body));
                    Telemetry.Send("density_calc_precise", StockAeroUtil.GetDensity(Trajectories.AttachedVessel.GetWorldPos3D(), body));

                    Telemetry.Send("temperature", Trajectories.AttachedVessel.atmosphericTemperature);
                    Telemetry.Send("temperature_calc", StockAeroUtil.GetTemperature(Trajectories.AttachedVessel.GetWorldPos3D(), body));
                }

                PreviousFrameVelocity = bodySpaceVelocity;
                PreviousFramePos = bodySpacePosition;
                PreviousFrameTime = now;
            }
        }
#endif

        internal static void ComputeTrajectory()
        {
            if (!Trajectories.VesselHasParts || Trajectories.AttachedVessel.LandedOrSplashed)
            {
                ComputeClear();
                GameDataCache.Clear();
                return;
            }

            try
            {
                // start of trajectory calculation timing
                Progress = 0d;
                calculation_time = Util.Clocks;

                // update game data cache
                if (!GameDataCache.Update())
                {
                    ComputeClear();
                    return;
                }

                // update aerodynamic model cache
                if (Settings.UseCache)
                    Trajectories.AerodynamicModel.UpdateCache();

                // clear the buffers
                patches_buffer.Clear();
                max_accel_buffer = 0d;

                // start compute patches thread, calls ComputeTrajectoryPatches()
                Worker.Thread.RunWorkerAsync(Worker.JOB.COMPUTE_PATCHES);
            }
            catch (Exception)
            {
                ++ErrorCount;
                throw;
            }
        }

        /// <summary> Computes trajectory patches, executed by the worker thread </summary>
        internal static void ComputeTrajectoryPatches()
        {
            double progress_step = 1d / Settings.MaxPatchCount;

            // create starting VesselState
            VesselState state = new VesselState
            {
                ReferenceBody = GameDataCache.Body,
                Time = GameDataCache.UniversalTime,
                Position = GameDataCache.VesselWorldPos - GameDataCache.BodyWorldPos,
                Velocity = GameDataCache.VesselOrbitVelocity,
                StockPatch = GameDataCache.Orbit
            };

            // iterate over patches until MaxPatchCount is reached
            for (int patchIdx = 0; patchIdx < Settings.MaxPatchCount; ++patchIdx)
            {
                double progress = progress_step;

                // stop if we don't have a vessel state or thread is canceled
                if (state == null || Worker.Thread.CancellationPending)
                    break;

                // search through maneuver nodes of the vessel
                foreach (ManeuverNode node in GameDataCache.ManeuverNodes)
                {
                    // if the maneuver node time corresponds to the end time of the last patch
                    if (node.UT == state.Time)
                    {
                        // add the velocity change of the burn to the velocity of the last patch
                        state.Velocity += node.GetBurnVector(CreateOrbitFromState(state));
                        break;
                    }
                }

                // Add patch
                Profiler.Start("Trajectory.AddPatch");
                state = AddPatch(state);
                Profiler.Stop("Trajectory.AddPatch");

                Worker.Thread.ReportProgress((int)(progress * 100d));
                progress += progress_step;
            }
        }

        /// <summary> Clears the patch buffers and resets timers.</summary>
        internal static void ComputeClear()
        {
            calculation_time = 0d;
            patches_calculated = 0d;
            Progress = 0d;
            patches_buffer.Clear();
            max_accel_buffer = 0d;
            Patches.Clear();
            MaxAccel = 0d;
            UpdateTiming();
        }

        /// <summary> Swaps the buffers. Called via Trajectories.Update when the worker thread flags it has finished executing ComputeTrajectoryPatches() </summary>
        internal static void ComputeComplete()
        {
            patches_swap_buffer = Patches;
            Patches = patches_buffer;
            patches_buffer = patches_swap_buffer;

            MaxAccel = max_accel_buffer;
        }

        /// <summary> Stops calculation timer, Called when the worker thread has finished executing ComputeTrajectoryPatches() </summary>
        internal static void StopTiming()
        {
            // how long did the calculation take?
            calculation_time = Util.ElapsedMilliseconds(calculation_time);
            patches_calculated = patches_buffer.Count;
            UpdateTiming();
        }

        /// <summary> Called when the worker thread has detected an error while executing ComputeTrajectoryPatches() </summary>
        internal static void ComputeError()
        {
            calculation_time = Util.ElapsedMilliseconds(calculation_time);
            UpdateTiming();
        }

        /// <summary>
        /// relativePosition is in world frame, but relative to the GameDataCache body (i.e. inertial body space)
        /// returns the altitude above sea level (can be negative for bodies without ocean)
        /// </summary>
        internal static double GetGroundAltitude(Vector3d relativePosition)
        {
            double lat = GameDataCache.Body.GetLatitude(relativePosition + GameDataCache.BodyWorldPos) / 180d * Math.PI;
            double lon = GameDataCache.Body.GetLongitude(relativePosition + GameDataCache.BodyWorldPos) / 180d * Math.PI;
            Vector3d rad = new Vector3d(Math.Cos(lat) * Math.Cos(lon), Math.Sin(lat), Math.Cos(lat) * Math.Sin(lon));
            double elevation = 0d;   // Temporary until I find a workaround //GameDataCache.BodyPQScontroller.GetSurfaceHeight(rad) - GameDataCache.BodyRadius;
            if (GameDataCache.BodyHasOcean)
                elevation = Math.Max(elevation, 0d);

            return elevation;
        }

        /// <summary>
        /// relativePosition is in world frame, but relative to the body (i.e. inertial body space)
        /// returns the altitude above sea level (can be negative for bodies without ocean)
        /// </summary>
        internal static double GetGroundAltitude(CelestialBody body, Vector3d relativePosition)
        {
            if (body.pqsController == null)
                return 0d;

            double lat = body.GetLatitude(relativePosition + body.position) / 180d * Math.PI;
            double lon = body.GetLongitude(relativePosition + body.position) / 180d * Math.PI;
            Vector3d rad = new Vector3d(Math.Cos(lat) * Math.Cos(lon), Math.Sin(lat), Math.Cos(lat) * Math.Sin(lon));
            double elevation = body.pqsController.GetSurfaceHeight(rad) - body.Radius;
            if (body.ocean)
                elevation = Math.Max(elevation, 0d);

            return elevation;
        }

        private static Orbit CreateOrbitFromState(VesselState state)
        {
            //Util.DebugLog("UT {0}", state.Time);

            Orbit orbit = new Orbit();
            orbit.UpdateFromStateVectors(Util.SwapYZ(state.Position), Util.SwapYZ(state.Velocity), GameDataCache.Body, state.Time);
            PatchedConics.SolverParameters pars = new PatchedConics.SolverParameters
            {
                FollowManeuvers = false
            };

            //Util.DebugLog("Calculating patch for orbit {0}", orbit.ToString());

            PatchedConics.CalculatePatch(orbit, new Orbit(), state.Time, pars, null);
            return orbit;
        }

        private static double FindOrbitBodyIntersection(Orbit orbit, double startTime, double endTime, double bodyAltitude)
        {
            // binary search of entry time in atmosphere
            // I guess an analytic solution could be found, but I'm too lazy to search it

            double from = startTime;
            double to = endTime;

            int loopCount = 0;
            while (to - from > 0.1d)
            {
                ++loopCount;
                if (loopCount > 1000)
                {
                    Util.LogWarning("Infinite loop? Trajectory.AddPatch or atmosphere limit search");
                    ++ErrorCount;
                    break;
                }
                double middle = (from + to) * 0.5d;
                if (orbit.getRelativePositionAtUT(middle).magnitude < bodyAltitude)
                {
                    to = middle;
                }
                else
                {
                    from = middle;
                }
            }

            return to;
        }

        private struct SimulationState
        {
            internal Vector3d position;
            internal Vector3d velocity;
        }

        /// <summary>
        /// Integration step function for Runge-Kutta 4 integration
        /// </summary>
        /// <param name="state">Position+Velocity of the current time step</param>
        /// <param name="accelerationFunc">Functor that returns the acceleration for a given Position+Velocity</param>
        /// <param name="dt">Time step interval</param>
        /// <param name="accel">Stores the value of the accelerationFunc at the current time step</param>
        /// <returns></returns>
        private static SimulationState RK4Step(SimulationState state, Func<Vector3d, Vector3d, Vector3d> accelerationFunc, double dt, out Vector3d accel)
        {
            Vector3d p1 = state.position;
            Vector3d v1 = state.velocity;
            accel = accelerationFunc(p1, v1);

            Vector3d p2 = state.position + 0.5d * v1 * dt;
            Vector3d v2 = state.velocity + 0.5d * accel * dt;
            Vector3d a2 = accelerationFunc(p2, v2);

            Vector3d p3 = state.position + 0.5d * v2 * dt;
            Vector3d v3 = state.velocity + 0.5d * a2 * dt;
            Vector3d a3 = accelerationFunc(p3, v3);

            Vector3d p4 = state.position + v3 * dt;
            Vector3d v4 = state.velocity + a3 * dt;
            Vector3d a4 = accelerationFunc(p4, v4);

            state.position = state.position + (dt / 6d) * (v1 + 2d * v2 + 2d * v3 + v4);
            state.velocity = state.velocity + (dt / 6d) * (accel + 2d * a2 + 2d * a3 + a4);

            return state;
        }

        private static VesselState AddPatch(VesselState startingState)
        {
            Patch patch = new Patch
            {
                StartingState = startingState,
                IsAtmospheric = false,
                SpaceOrbit = startingState.StockPatch ?? CreateOrbitFromState(startingState)
            };
            patch.EndTime = patch.StartingState.Time + patch.SpaceOrbit.period;

            // the flight plan does not always contain the first patches (before the first maneuver node),
            // so we populate it with the current orbit and associated encounters etc.
            List<Orbit> flightPlan = new List<Orbit>();
            for (Orbit orbit = GameDataCache.Orbit; orbit != null && orbit.activePatch; orbit = orbit.nextPatch)
            {
                if (GameDataCache.FlightPlan.Contains(orbit))
                    break;
                flightPlan.Add(orbit);
            }

            foreach (Orbit orbit in GameDataCache.FlightPlan)
            {
                flightPlan.Add(orbit);
            }


            Orbit nextPatch = null;
            if (startingState.StockPatch == null)
            {
                nextPatch = patch.SpaceOrbit.nextPatch;
            }
            else
            {
                int planIdx = flightPlan.IndexOf(startingState.StockPatch);
                if (planIdx >= 0 && planIdx < flightPlan.Count - 1)
                {
                    nextPatch = flightPlan[planIdx + 1];
                }
            }

            if (nextPatch != null)
                patch.EndTime = nextPatch.StartUT;

            double maxAtmosphereAltitude = GameDataCache.BodyHasAtmosphere ? GameDataCache.BodyAtmosphereDepth : GameDataCache.BodyMaxGroundHeight;

            double minAltitude = patch.SpaceOrbit.PeA;
            if (patch.SpaceOrbit.timeToPe < 0d || patch.EndTime < startingState.Time + patch.SpaceOrbit.timeToPe)
            {
                minAltitude = Math.Min(
                    patch.SpaceOrbit.getRelativePositionAtUT(patch.EndTime).magnitude,
                    patch.SpaceOrbit.getRelativePositionAtUT(patch.StartingState.Time + 1d).magnitude
                    ) - GameDataCache.BodyRadius;
            }

            if (minAltitude < maxAtmosphereAltitude)
            {
                double entryTime;
                if (startingState.Position.magnitude <= GameDataCache.BodyRadius + maxAtmosphereAltitude)
                {
                    // whole orbit is inside the atmosphere
                    entryTime = startingState.Time;
                }
                else
                {
                    entryTime = FindOrbitBodyIntersection(
                        patch.SpaceOrbit,
                        startingState.Time, startingState.Time + patch.SpaceOrbit.timeToPe,
                        GameDataCache.BodyRadius + maxAtmosphereAltitude);
                }

                if (entryTime > startingState.Time + 0.1d || !GameDataCache.BodyHasAtmosphere)
                {
                    if (GameDataCache.BodyHasAtmosphere)
                    {
                        // add the space patch before atmospheric entry

                        patch.EndTime = entryTime;
                        patches_buffer.Add(patch);
                        return new VesselState
                        {
                            ReferenceBody = GameDataCache.Body,
                            Position = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(entryTime)),
                            Time = entryTime,
                            Velocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(entryTime))
                        };
                    }
                    else
                    {
                        // the body has no atmosphere, so what we actually computed is the entry
                        // inside the "ground sphere" (defined by the maximal ground altitude)
                        // now we iterate until the inner ground sphere (minimal altitude), and
                        // check if we hit the ground along the way
                        double groundRangeExit = FindOrbitBodyIntersection(
                            patch.SpaceOrbit,
                            startingState.Time, startingState.Time + patch.SpaceOrbit.timeToPe,
                            GameDataCache.BodyRadius - maxAtmosphereAltitude);

                        if (groundRangeExit <= entryTime)
                            groundRangeExit = startingState.Time + patch.SpaceOrbit.timeToPe;

                        double iterationSize = (groundRangeExit - entryTime) / 100d;
                        double t;
                        bool groundImpact = false;
                        for (t = entryTime; t < groundRangeExit; t += iterationSize)
                        {
                            Vector3d pos = patch.SpaceOrbit.getRelativePositionAtUT(t);
                            double groundAltitude = GetGroundAltitude(CalculateRotatedPosition(Util.SwapYZ(pos), t))
                                + GameDataCache.BodyRadius;
                            if (pos.magnitude < groundAltitude)
                            {
                                t -= iterationSize;
                                groundImpact = true;
                                break;
                            }
                        }

                        if (groundImpact)
                        {
                            patch.EndTime = t;
                            patch.RawImpactPosition = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(t));
                            patch.ImpactPosition = CalculateRotatedPosition(patch.RawImpactPosition.Value, t);
                            patch.ImpactVelocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(t));
                            patches_buffer.Add(patch);
                            return null;
                        }
                        else
                        {
                            // no impact, just add the space orbit
                            patches_buffer.Add(patch);

                            // currently, we can't handle predictions for another body, so we stop if the next patch body is different
                            if (nextPatch == null || nextPatch.referenceBody != GameDataCache.Body)
                                return null;

                            return new VesselState
                            {
                                ReferenceBody = GameDataCache.Body,
                                Position = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(patch.EndTime)),
                                Velocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(patch.EndTime)),
                                Time = patch.EndTime,
                                StockPatch = nextPatch
                            };
                        }
                    }
                }
                else
                {
                    // simulate atmospheric flight (drag and lift), until impact or atmosphere exit
                    // (typically for an aerobraking maneuver) assuming a constant angle of attack
                    patch.IsAtmospheric = true;
                    patch.StartingState.StockPatch = null;

                    // lower dt would be more accurate, but a trade-off has to be found between performances and accuracy
                    double dt = Settings.IntegrationStepSize;

                    // some shallow entries can result in very long flight. For performances reasons,
                    // we limit the prediction duration
                    int maxIterations = (int)(60d * 60d / dt);

                    int chunkSize = 128;

                    // time between two consecutive stored positions (more intermediate positions are computed for better accuracy),
                    // also used for ground collision checks
                    double trajectoryInterval = 10d;

                    List<Point[]> buffer = new List<Point[]>
                    {
                        new Point[chunkSize]
                    };
                    int nextPosIdx = 0;

                    SimulationState state;
                    state.position = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(entryTime));
                    state.velocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(entryTime));

                    // Initialize a patch with zero acceleration
                    Vector3d currentAccel = Vector3d.zero;


                    double currentTime = entryTime;
                    double lastPositionStoredUT = 0d;
                    Vector3d lastPositionStored = Vector3d.zero;
                    bool hitGround = false;
                    int iteration = 0;

                    #region Acceleration Functor

                    // function that calculates the acceleration under current parameters
                    Func<Vector3d, Vector3d, Vector3d> accelerationFunc = (position, velocity) =>
                    {
                        Profiler.Start("accelerationFunc inside");

                        // gravity acceleration
                        double R_ = position.magnitude;
                        Vector3d accel_g = position * (-GameDataCache.BodyGravityParameter / (R_ * R_ * R_));

                        // aero force
                        Vector3d vel_air = velocity - Vector3d.Cross(GameDataCache.BodyAngularVelocity, position);

                        double aoa = DescentProfile.GetAngleOfAttack(GameDataCache.Body, position, vel_air) ?? 0d;

                        Profiler.Start("GetForces");
                        Vector3d force_aero = Trajectories.AerodynamicModel.GetForces(position, vel_air, aoa);
                        Profiler.Stop("GetForces");

                        Vector3d accel = accel_g + force_aero / GameDataCache.VesselMass;

                        Profiler.Stop("accelerationFunc inside");
                        return accel;
                    };
                    #endregion


                    #region Integration Loop

                    while (true)
                    {
                        if (Worker.Thread.CancellationPending)
                            return null;

                        ++iteration;

                        double R = state.position.magnitude;
                        double altitude = R - GameDataCache.BodyRadius;
                        double atmosphereCoeff = altitude / maxAtmosphereAltitude;
                        if (hitGround
                            || atmosphereCoeff <= 0d || atmosphereCoeff >= 1d
                            || iteration == maxIterations || currentTime > patch.EndTime)
                        {
                            //Util.PostSingleScreenMessage("atmo force", "Atmospheric accumulated force: " + accumulatedForces.ToString("0.00"));

                            if (hitGround || atmosphereCoeff <= 0d)
                            {
                                patch.RawImpactPosition = state.position;
                                patch.ImpactPosition = CalculateRotatedPosition(patch.RawImpactPosition.Value, currentTime);
                                patch.ImpactVelocity = state.velocity;
                            }

                            patch.EndTime = Math.Min(currentTime, patch.EndTime);

                            int totalCount = (buffer.Count - 1) * chunkSize + nextPosIdx;
                            patch.AtmosphericTrajectory = new Point[totalCount];
                            int outIdx = 0;
                            foreach (Point[] chunk in buffer)
                            {
                                foreach (Point p in chunk)
                                {
                                    if (outIdx == totalCount)
                                        break;
                                    patch.AtmosphericTrajectory[outIdx++] = p;
                                }
                            }

                            if (iteration == maxIterations)
                            {
                                //ScreenMessages.PostScreenMessage("WARNING: trajectory prediction stopped, too many iterations");
                                Util.LogWarning("trajectory prediction stopped, too many iterations");
                                patches_buffer.Add(patch);
                                return null;
                            }
                            else if (atmosphereCoeff <= 0d || hitGround)
                            {
                                patches_buffer.Add(patch);
                                return null;
                            }
                            else
                            {
                                patches_buffer.Add(patch);
                                return new VesselState
                                {
                                    ReferenceBody = GameDataCache.Body,
                                    Position = state.position,
                                    Velocity = state.velocity,
                                    Time = patch.EndTime
                                };
                            }
                        }

                        Vector3d lastAccel = currentAccel;
                        SimulationState lastState = state;

                        #region Integration Step

                        Profiler.Start("IntegrationStep");

                        state = RK4Step(state, accelerationFunc, dt, out currentAccel);

                        currentTime += dt;

                        // KSP presumably uses Euler integration for position updates. Since RK4 is actually more precise than that,
                        // we try to reintroduce an approximation of the error.

                        // The local truncation error for euler integration is:
                        // LTE = 1/2 * h^2 * y''(t)
                        // https://en.wikipedia.org/wiki/Euler_method#Local_truncation_error
                        //
                        // For us,
                        // h is the time step of the outer simulation (KSP), which is the physics time step
                        // y''(t) is the difference of the velocity/acceleration divided by the physics time step
                        state.position += 0.5d * GameDataCache.WarpDeltaTime * currentAccel * dt;
                        state.velocity += 0.5d * GameDataCache.WarpDeltaTime * (currentAccel - lastAccel);

                        Profiler.Stop("IntegrationStep");
                        #endregion

                        // calculate gravity and aerodynamic force
                        Vector3d gravityAccel = lastState.position * (-GameDataCache.BodyGravityParameter / (R * R * R));
                        Vector3d aerodynamicForce = (currentAccel - gravityAccel) / GameDataCache.VesselMass;

                        // acceleration in the vessel reference frame is acceleration - gravityAccel
                        max_accel_buffer = Math.Max(aerodynamicForce.magnitude / GameDataCache.VesselMass, max_accel_buffer);

                        #region Impact Calculation

                        Profiler.Start("AddPatch#impact");

                        double interval = altitude < 10000d ? trajectoryInterval * 0.1d : trajectoryInterval;
                        if (currentTime >= lastPositionStoredUT + interval)
                        {
                            double groundAltitude = GetGroundAltitude(CalculateRotatedPosition(state.position, currentTime));
                            if (lastPositionStoredUT > 0d)
                            {
                                // check terrain collision, to detect impact on mountains etc.
                                Vector3d rayOrigin = lastPositionStored;
                                Vector3d rayEnd = state.position;
                                double absGroundAltitude = groundAltitude + GameDataCache.BodyRadius;
                                if (absGroundAltitude > rayEnd.magnitude)
                                {
                                    hitGround = true;
                                    double coeff = Math.Max(0.01d, (absGroundAltitude - rayOrigin.magnitude)
                                        / (rayEnd.magnitude - rayOrigin.magnitude));
                                    state.position = rayEnd * coeff + rayOrigin * (1d - coeff);
                                    currentTime = currentTime * coeff + lastPositionStoredUT * (1d - coeff);
                                }
                            }

                            lastPositionStoredUT = currentTime;
                            if (nextPosIdx == chunkSize)
                            {
                                buffer.Add(new Point[chunkSize]);
                                nextPosIdx = 0;
                            }
                            Vector3d nextPos = state.position;
                            if (Settings.BodyFixedMode)
                            {
                                nextPos = CalculateRotatedPosition(nextPos, currentTime);
                            }
                            buffer.Last()[nextPosIdx].aerodynamicForce = aerodynamicForce;
                            buffer.Last()[nextPosIdx].orbitalVelocity = state.velocity;
                            buffer.Last()[nextPosIdx].groundAltitude = groundAltitude;
                            buffer.Last()[nextPosIdx].time = currentTime;
                            buffer.Last()[nextPosIdx++].pos = nextPos;
                            lastPositionStored = state.position;
                        }

                        Profiler.Stop("AddPatch#impact");

                        #endregion
                    }

                    #endregion
                }
            }
            else
            {
                // no atmospheric entry, just add the space orbit
                patches_buffer.Add(patch);

                // currently, we can't handle predictions for another body, so we stop if the next patch body is different
                if (nextPatch == null || nextPatch.referenceBody != GameDataCache.Body)
                    return null;

                return new VesselState
                {
                    ReferenceBody = GameDataCache.Body,
                    Position = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(patch.EndTime)),
                    Velocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(patch.EndTime)),
                    Time = patch.EndTime,
                    StockPatch = nextPatch
                };
            }
        }

        internal static Vector3d CalculateRotatedPosition(Vector3d relativePosition, double time)
        {
            double angle = -(time - GameDataCache.UniversalTime) * GameDataCache.BodyAngularVelocity.magnitude / Math.PI * 180d;
            Quaternion bodyRotation = Quaternion.AngleAxis((float)angle, GameDataCache.BodyAngularVelocity.normalized);
            return bodyRotation * relativePosition;
        }

        internal static Vector3d CalculateRotatedPosition(CelestialBody body, Vector3d relativePosition, double time)
        {
            double angle = -(time - Planetarium.GetUniversalTime()) * body.angularVelocity.magnitude / Math.PI * 180d;
            Quaternion bodyRotation = Quaternion.AngleAxis((float)angle, body.angularVelocity.normalized);
            return bodyRotation * relativePosition;
        }
    }
}
