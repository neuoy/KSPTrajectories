/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2014-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2018 S.Gray, (aka PiezPiedPy).

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
using System.Diagnostics;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace Trajectories
{
    /// <summary>
    /// Handles trajectory prediction, performing a lightweight physical simulation to
    /// predict a vessels trajectory in space and atmosphere.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class Trajectory: MonoBehaviour
    {
        public class VesselState
        {
            public CelestialBody ReferenceBody { get; set; }

            // universal time
            public double Time { get; set; }

            // position in world frame relatively to the reference body
            public Vector3d Position { get; set; }

            // velocity in world frame relatively to the reference body
            public Vector3d Velocity { get; set; }

            // tells wether the patch starting from this state is superimposed on a stock KSP patch, or null if
            // something makes it diverge (atmospheric entry for example)
            public Orbit StockPatch { get; set; }

            public VesselState(Vessel vessel)
            {
                ReferenceBody = vessel.orbit.referenceBody;
                Time = Planetarium.GetUniversalTime();
                Position = vessel.GetWorldPos3D() - ReferenceBody.position;
                Velocity = vessel.obt_velocity;
                StockPatch = vessel.orbit;
            }

            public VesselState()
            {
            }
        }

        public struct Point
        {
            public Vector3 pos;
            public Vector3 aerodynamicForce;
            public Vector3 orbitalVelocity;

            /// <summary>
            /// Ground altitude above (or under) sea level, in meters.
            /// </summary>
            public float groundAltitude;

            /// <summary>
            /// Universal time
            /// </summary>
            public double time;
        }

        public class Patch
        {
            public VesselState StartingState { get; set; }

            public double EndTime { get; set; }

            public bool IsAtmospheric { get; set; }

            // // position array in body space (world frame centered on the body) ; only used when isAtmospheric is true
            public Point[] AtmosphericTrajectory { get; set; }

            // only used when isAtmospheric is false
            public Orbit SpaceOrbit { get; set; }

            public Vector3? ImpactPosition { get; set; }

            public Vector3? RawImpactPosition { get; set; }

            public Vector3? ImpactVelocity { get; set; }
        }

        public static class Target
        {
            /// <summary>
            /// Tests if the target has been set.
            /// </summary>
            /// <returns></returns>
            public static bool HasTarget()
            {
                return LocalPosition.HasValue && Body != null;
            }

            /// <summary>
            /// Targets reference body
            /// </summary>
            public static CelestialBody Body { get; set; } = null;

            /// <summary>
            /// Targets position in WorldSpace
            /// </summary>
            public static Vector3d? WorldPosition
            {
                get => LocalPosition.HasValue ? Body?.transform?.TransformDirection(LocalPosition.Value) : null;
                set
                {
                    // A transform that has double precision would be nice so we can have target precision in meter's
                    LocalPosition = value.HasValue ? Body?.transform?.InverseTransformDirection(value.Value) : null;
                }
            }

            /// <summary>
            /// Targets position in LocalSpace relative to the target body
            /// </summary>
            public static Vector3d? LocalPosition { get; private set; } = null;

            /// <summary>
            /// Manual target TextBox string
            /// </summary>
            public static string ManualText { get; set; } = "";

            /// <summary>
            /// Sets the target to a body and a World position
            /// Saves the target to the active vessel.
            /// </summary>
            public static void SetFromWorldPos(CelestialBody body, Vector3d position)
            {
                Body = body;
                WorldPosition = position;
                
                Save();
            }

            /// <summary>
            /// Sets the target to a body and a body-relative position
            /// Saves the target to the active vessel.
            /// </summary>
            public static void SetFromLocalPos(CelestialBody body, Vector3d position)
            {
                Body = body;
                LocalPosition = position;

                Save();
            }

            /// <summary>
            /// Sets the target to a body and a World position. If the altitude is not given, it will be calculated as the surface altitude at that latitude/longitude.
            /// Saves the target to the active vessel.
            /// </summary>
            public static void SetFromLatLonAlt(CelestialBody body, double latitude, double longitude, double? altitude = null)
            {
                Body = body;
            
                if (!altitude.HasValue)
                {
                    Vector3d relPos = body.GetWorldSurfacePosition(latitude, longitude, 2.0) - body.position;
                    altitude = Trajectory.GetGroundAltitude(body, relPos);
                }

                LocalPosition = body.GetRelSurfacePosition(latitude, longitude, altitude.Value);

                Save();
            }

            /// <summary>
            /// Clears the target
            /// </summary>
            public static void Clear()
            {
                Body = null;
                LocalPosition = null;
            }


            /// <summary>
            /// Saves the target to the active vessel module
            /// </summary>
            public static void Save()
            {
                if (fetch.attachedVessel == null)
                    return;

                //UnityEngine.Debug.Log("Trajectories: Writing target profile settings...");
                foreach (var module in FlightGlobals.ActiveVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
                {
                    module.TargetBody = Body == null ? "" : Body.name;
                    module.TargetPosition_x = LocalPosition.HasValue ? LocalPosition.Value.x : 0d;
                    module.TargetPosition_y = LocalPosition.HasValue ? LocalPosition.Value.y : 0d;
                    module.TargetPosition_z = LocalPosition.HasValue ? LocalPosition.Value.z : 0d;
                    module.ManualTargetTxt = ManualText;
                    //UnityEngine.Debug.Log("Trajectories: Target profile saved");
                }
            }
        }

        public const double integrator_min = 0.1d;         // RK4 Integrator minimum step size
        public const double integrator_max = 5.0d;         // RK4 Integrator maximum step size

        private int MaxIncrementTime { get { return 2; } }

        private Vessel attachedVessel;

        private VesselAerodynamicModel aerodynamicModel_;

        public string AerodynamicModelName
        {
            get
            {
                return aerodynamicModel_ == null ? Localizer.Format("#autoLOC_Trajectories_NotLoaded") :
                    aerodynamicModel_.AerodynamicModelName;
            }
        }

        private List<Patch> patches_ = new List<Patch>();

        private List<Patch> patchesBackBuffer_ = new List<Patch>();

        public List<Patch> Patches { get { return patches_; } }

        private Stopwatch incrementTime_;

        private IEnumerator<bool> partialComputation_;

        private float maxAccel_;

        private float maxAccelBackBuffer_;

        public float MaxAccel { get { return maxAccel_; } }

        private static int errorCount_;

        public int ErrorCount { get { return errorCount_; } }

        /// <summary>
        /// Trajectory calculation time in last frame
        /// </summary>
        private static float frameTime_;

        private static float computationTime_;

        public float ComputationTime { get { return computationTime_ * 0.001f; } }

        private static Stopwatch gameFrameTime_;

        private static float averageGameFrameTime_;

        public float GameFrameTime { get { return averageGameFrameTime_ * 0.001f; } }

        // permit global access
        public static Trajectory fetch { get; private set; } = null;

        //  constructor
        public Trajectory()
        {
            fetch = this;
        }

        private void OnDestroy()
        {
            fetch = null;
        }

#if DEBUG && DEBUG_TELEMETRY

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        public void Awake()
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

        public void FixedUpdate()
        {
            DebugTelemetry();
        }

        private static Vector3d PreviousFramePos;
        private static Vector3d PreviousFrameVelocity;
        private static double PreviousFrameTime = 0;


        public void DebugTelemetry()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;

            double now = Planetarium.GetUniversalTime();
            double dt = now - PreviousFrameTime;

            if (dt > 0.5 || dt < 0.0)
            {
                Vector3d bodySpacePosition = new Vector3d();
                Vector3d bodySpaceVelocity = new Vector3d();

                if (aerodynamicModel_ != null && attachedVessel != null)
                {
                    CelestialBody body = attachedVessel.orbit.referenceBody;

                    bodySpacePosition = attachedVessel.GetWorldPos3D() - body.position;
                    bodySpaceVelocity = attachedVessel.obt_velocity;

                    double altitudeAboveSea = bodySpacePosition.magnitude - body.Radius;

                    Vector3d airVelocity = bodySpaceVelocity - body.getRFrmVel(body.position + bodySpacePosition);

                    double R = PreviousFramePos.magnitude;
                    Vector3d gravityForce = PreviousFramePos * (-body.gravParameter / (R * R * R) * attachedVessel.totalMass);

                    Quaternion inverseRotationFix = body.inverseRotation ?
                        Quaternion.AngleAxis((float)(body.angularVelocity.magnitude / Math.PI * 180.0 * dt), Vector3.up)
                        : Quaternion.identity;
                    Vector3d TotalForce = (bodySpaceVelocity - inverseRotationFix * PreviousFrameVelocity) * (attachedVessel.totalMass / dt);
                    TotalForce += bodySpaceVelocity * (dt * 0.000015); // numeric precision fix
                    Vector3d ActualForce = TotalForce - gravityForce;

                    Transform vesselTransform = attachedVessel.ReferenceTransform;
                    Vector3d vesselBackward = (Vector3d)(-vesselTransform.up.normalized);
                    Vector3d vesselForward = -vesselBackward;
                    Vector3d vesselUp = (Vector3d)(-vesselTransform.forward.normalized);
                    Vector3d vesselRight = Vector3d.Cross(vesselUp, vesselBackward).normalized;
                    double AoA = Math.Acos(Vector3d.Dot(airVelocity.normalized, vesselForward.normalized));
                    if (Vector3d.Dot(airVelocity, vesselUp) > 0)
                        AoA = -AoA;

                    VesselAerodynamicModel.DebugParts = true;
                    Vector3d referenceForce = aerodynamicModel_.ComputeForces(20000, new Vector3d(0, 0, 1500), new Vector3d(0,1,0), 0);
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
                    Telemetry.Send("altitude", attachedVessel.altitude);

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

                    Telemetry.Send("drag", attachedVessel.rootPart.rb.drag);

                    Telemetry.Send("density", attachedVessel.atmDensity);
                    Telemetry.Send("density_calc", StockAeroUtil.GetDensity(altitudeAboveSea, body));
                    Telemetry.Send("density_calc_precise", StockAeroUtil.GetDensity(attachedVessel.GetWorldPos3D(), body));

                    Telemetry.Send("temperature", attachedVessel.atmosphericTemperature);
                    Telemetry.Send("temperature_calc", StockAeroUtil.GetTemperature(attachedVessel.GetWorldPos3D(), body));
                }

                PreviousFrameVelocity = bodySpaceVelocity;
                PreviousFramePos = bodySpacePosition;
                PreviousFrameTime = now;
            }
        }
#endif

        public void Update()
        {
            // compute frame time
            computationTime_ = computationTime_ * 0.99f + frameTime_ * 0.01f;
            float offset = frameTime_ - computationTime_;
            frameTime_ = 0;

            if (gameFrameTime_ != null)
            {
                float t = (float)gameFrameTime_.ElapsedMilliseconds;
                averageGameFrameTime_ = averageGameFrameTime_ * 0.99f + t * 0.01f;
            }
            gameFrameTime_ = Stopwatch.StartNew();

            if (Util.IsFlight)
            {
                if (attachedVessel != FlightGlobals.ActiveVessel)
                {
                    //UnityEngine.Debug.Log("Trajectories: Loading vessel target profile");
                    attachedVessel = FlightGlobals.ActiveVessel;
                    if (attachedVessel == null)
                    {
                        //UnityEngine.Debug.Log("Trajectories: No vessel");
                        Target.Clear();
                        Target.ManualText = "";
                    }
                    else
                    {
                        TrajectoriesVesselSettings module = attachedVessel.Parts.SelectMany(p => p.Modules
                            .OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                        if (module == null)
                        {
                            //UnityEngine.Debug.Log("Trajectories: No TrajectoriesVesselSettings module");
                            Target.Clear();
                            Target.ManualText = "";
                        }
                        else
                        {
                            //UnityEngine.Debug.Log("Trajectories: Reading target settings...");
                            Target.SetFromLocalPos(FlightGlobals.Bodies.FirstOrDefault(b => b.name == module.TargetBody),
                                new Vector3d(module.TargetPosition_x, module.TargetPosition_y, module.TargetPosition_z));
                            Target.ManualText = module.ManualTargetTxt;
                            //UnityEngine.Debug.Log("Trajectories: Target profile loaded");
                        }
                    }
                }

                // should the trajectory be calculated?
                if (attachedVessel != null &&
                    (Settings.fetch.DisplayTrajectories || Settings.fetch.AlwaysUpdate || Target.WorldPosition.HasValue))
                {
                    if (attachedVessel.Parts.Count != 0)
                        ComputeTrajectory(attachedVessel, DescentProfile.fetch);
                }
            }

        }

        public void InvalidateAerodynamicModel()
        {
            aerodynamicModel_.Invalidate();
        }

        public void ComputeTrajectory(Vessel vessel, DescentProfile profile)
        {
            try
            {
                // start of trajectory calculation in current frame
                incrementTime_ = Stopwatch.StartNew();

                // if there is no ongoing partial computation, start a new one
                if (partialComputation_ == null || vessel != attachedVessel)
                {
                    // restart the public buffers
                    patchesBackBuffer_.Clear();
                    maxAccelBackBuffer_ = 0;

                    attachedVessel = vessel;

                    // no vessel, no calculation
                    if (attachedVessel == null)
                    {
                        patches_.Clear();
                        return;
                    }

                    // Create enumerator for Trajectory increment calculator
                    partialComputation_ = ComputeTrajectoryIncrement(vessel, profile).GetEnumerator();
                }

                // we are finished when there are no more partial computations to be done
                bool finished = !partialComputation_.MoveNext();

                // when calculation is finished,
                if (finished)
                {
                    // swap the buffers for the patches and the maximum acceleration,
                    // "publishing" the results
                    var tmp = patches_;
                    patches_ = patchesBackBuffer_;
                    patchesBackBuffer_ = tmp;

                    maxAccel_ = maxAccelBackBuffer_;

                    // Reset partial computation
                    partialComputation_.Dispose();
                    partialComputation_ = null;
                }

                // how long did the calculation in this frame take?
                frameTime_ += (float)incrementTime_.ElapsedMilliseconds;
            }
            catch (Exception)
            {
                ++errorCount_;
                throw;
            }
        }

        private IEnumerable<bool> ComputeTrajectoryIncrement(Vessel vessel, DescentProfile profile)
        {
            // create or update aerodynamic model
            if (aerodynamicModel_ == null || !aerodynamicModel_.isValidFor(vessel, vessel.mainBody))
                aerodynamicModel_ = AerodynamicModelFactory.GetModel(vessel, vessel.mainBody);
            else
                aerodynamicModel_.IncrementalUpdate();

            // create new VesselState from vessel, or null if it's on the ground
            var state = vessel.LandedOrSplashed ? null : new VesselState(vessel);

            // iterate over patches until MaxPatchCount is reached
            for (int patchIdx = 0; patchIdx < Settings.fetch.MaxPatchCount; ++patchIdx)
            {
                // stop if we don't have a vessel state
                if (state == null)
                    break;

                // If we spent more time in this calculation than allowed, pause until the next frame
                if (incrementTime_.ElapsedMilliseconds > MaxIncrementTime)
                    yield return false;

                // if we have a patched conics solver, check for maneuver nodes
                if (null != attachedVessel.patchedConicSolver)
                {
                    // search through maneuver nodes of the vessel
                    var maneuverNodes = attachedVessel.patchedConicSolver.maneuverNodes;
                    foreach (var node in maneuverNodes)
                    {
                        // if the maneuver node time corresponds to the end time of the last patch
                        if (node.UT == state.Time)
                        {
                            // add the velocity change of the burn to the velocity of the last patch
                            state.Velocity += node.GetBurnVector(CreateOrbitFromState(state));
                            break;
                        }
                    }

                    // Add one patch, then pause execution after every patch
                    foreach (var result in AddPatch(state, profile))
                        yield return false;
                }

                state = AddPatch_outState;
            }
        }

        /// <summary>
        /// relativePosition is in world frame, but relative to the body (i.e. inertial body space)
        /// returns the altitude above sea level (can be negative for bodies without ocean)
        /// </summary>
        public static double GetGroundAltitude(CelestialBody body, Vector3 relativePosition)
        {
            if (body.pqsController == null)
                return 0;

            double lat = body.GetLatitude(relativePosition + body.position) / 180.0 * Math.PI;
            double lon = body.GetLongitude(relativePosition + body.position) / 180.0 * Math.PI;
            Vector3d rad = new Vector3d(Math.Cos(lat) * Math.Cos(lon), Math.Sin(lat), Math.Cos(lat) * Math.Sin(lon));
            double elevation = body.pqsController.GetSurfaceHeight(rad) - body.Radius;
            if (body.ocean)
                elevation = Math.Max(elevation, 0.0);

            return elevation;
        }

        public static double RealMaxAtmosphereAltitude(CelestialBody body)
        {
            if (!body.atmosphere)
                return 0;
            // Change for 1.0 refer to atmosphereDepth
            return body.atmosphereDepth;
        }

        private Orbit CreateOrbitFromState(VesselState state)
        {
            var orbit = new Orbit();
            orbit.UpdateFromStateVectors(Util.SwapYZ(state.Position), Util.SwapYZ(state.Velocity), state.ReferenceBody, state.Time);
            var pars = new PatchedConics.SolverParameters
            {
                FollowManeuvers = false
            };
            PatchedConics.CalculatePatch(orbit, new Orbit(), state.Time, pars, null);
            return orbit;
        }

        private double FindOrbitBodyIntersection(Orbit orbit, double startTime, double endTime, double bodyAltitude)
        {
            // binary search of entry time in atmosphere
            // I guess an analytic solution could be found, but I'm too lazy to search it

            double from = startTime;
            double to = endTime;

            int loopCount = 0;
            while (to - from > 0.1)
            {
                ++loopCount;
                if (loopCount > 1000)
                {
                    UnityEngine.Debug.Log("WARNING: infinite loop? (Trajectories.Trajectory.AddPatch, atmosphere limit search)");
                    ++errorCount_;
                    break;
                }
                double middle = (from + to) * 0.5;
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

        private VesselState AddPatch_outState;

        struct SimulationState
        {
            public Vector3d position;
            public Vector3d velocity;
        }

        /// <summary>
        /// Integration step function for Verlet integration
        /// </summary>
        /// <param name="state">Position+Velocity of the current time step</param>
        /// <param name="accelerationFunc">Functor that returns the acceleration for a given Position+Velocity</param>
        /// <param name="dt">Time step interval</param>
        /// <param name="accel">Stores the value of the accelerationFunc at the current time step</param>
        /// <returns></returns>
        static SimulationState VerletStep(SimulationState state,
            Func<Vector3d, Vector3d, Vector3d> accelerationFunc,
            double dt,
            out Vector3d accel)
        {

            Profiler.Start("accelerationFunc outside");
            accel = accelerationFunc(state.position, state.velocity);
            Profiler.Stop("accelerationFunc outside");

            Vector3d prevPos = state.position - dt * state.velocity;

            SimulationState nextState;

            nextState.position = 2.0 * state.position - prevPos + accel * (dt * dt);
            nextState.velocity = (nextState.position - state.position) / dt;

            return nextState;
        }

        /// <summary>
        /// Integration step function for Runge-Kutta 4 integration
        /// </summary>
        /// <param name="state">Position+Velocity of the current time step</param>
        /// <param name="accelerationFunc">Functor that returns the acceleration for a given Position+Velocity</param>
        /// <param name="dt">Time step interval</param>
        /// <param name="accel">Stores the value of the accelerationFunc at the current time step</param>
        /// <returns></returns>
        static SimulationState RK4Step(
            SimulationState state,
            Func<Vector3d, Vector3d, Vector3d> accelerationFunc,
            double dt,
            out Vector3d accel)
        {
            Vector3d p1 = state.position;
            Vector3d v1 = state.velocity;
            accel = accelerationFunc(p1, v1);

            Vector3d p2 = state.position + 0.5 * v1 * dt;
            Vector3d v2 = state.velocity + 0.5 * accel * dt;
            Vector3d a2 = accelerationFunc(p2, v2);

            Vector3d p3 = state.position + 0.5 * v2 * dt;
            Vector3d v3 = state.velocity + 0.5 * a2 * dt;
            Vector3d a3 = accelerationFunc(p3, v3);

            Vector3d p4 = state.position + v3 * dt;
            Vector3d v4 = state.velocity + a3 * dt;
            Vector3d a4 = accelerationFunc(p4, v4);

            state.position = state.position + (dt / 6.0) * (v1 + 2.0 * v2 + 2.0 * v3 + v4);
            state.velocity = state.velocity + (dt / 6.0) * (accel + 2.0 * a2 + 2.0 * a3 + a4);

            return state;
        }

        private IEnumerable<bool> AddPatch(VesselState startingState, DescentProfile profile)
        {
            if (null == attachedVessel.patchedConicSolver)
            {
                UnityEngine.Debug.LogWarning("Trajectories: AddPatch() attempted when patchedConicsSolver is null; Skipping.");
                yield break;
            }

            CelestialBody body = startingState.ReferenceBody;

            var patch = new Patch
            {
                StartingState = startingState,
                IsAtmospheric = false,
                SpaceOrbit = startingState.StockPatch ?? CreateOrbitFromState(startingState)
            };
            patch.EndTime = patch.StartingState.Time + patch.SpaceOrbit.period;

            // the flight plan does not always contain the first patches (before the first maneuver node),
            // so we populate it with the current orbit and associated encounters etc.
            var flightPlan = new List<Orbit>();
            for (var orbit = attachedVessel.orbit; orbit != null && orbit.activePatch; orbit = orbit.nextPatch)
            {
                if (attachedVessel.patchedConicSolver.flightPlan.Contains(orbit))
                    break;
                flightPlan.Add(orbit);
            }

            foreach (var orbit in attachedVessel.patchedConicSolver.flightPlan)
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
            {
                patch.EndTime = nextPatch.StartUT;
            }

            double maxAtmosphereAltitude = RealMaxAtmosphereAltitude(body);
            if (!body.atmosphere)
            {
                maxAtmosphereAltitude = body.pqsController.mapMaxHeight;
            }

            double minAltitude = patch.SpaceOrbit.PeA;
            if (patch.SpaceOrbit.timeToPe < 0 || patch.EndTime < startingState.Time + patch.SpaceOrbit.timeToPe)
            {
                minAltitude = Math.Min(
                    patch.SpaceOrbit.getRelativePositionAtUT(patch.EndTime).magnitude,
                    patch.SpaceOrbit.getRelativePositionAtUT(patch.StartingState.Time + 1.0).magnitude
                    ) - body.Radius;
            }
            if (minAltitude < maxAtmosphereAltitude)
            {
                double entryTime;
                if (startingState.Position.magnitude <= body.Radius + maxAtmosphereAltitude)
                {
                    // whole orbit is inside the atmosphere
                    entryTime = startingState.Time;
                }
                else
                {
                    entryTime = FindOrbitBodyIntersection(
                        patch.SpaceOrbit,
                        startingState.Time, startingState.Time + patch.SpaceOrbit.timeToPe,
                        body.Radius + maxAtmosphereAltitude);
                }

                if (entryTime > startingState.Time + 0.1 || !body.atmosphere)
                {
                    if (body.atmosphere)
                    {
                        // add the space patch before atmospheric entry

                        patch.EndTime = entryTime;
                        patchesBackBuffer_.Add(patch);
                        AddPatch_outState = new VesselState
                        {
                            Position = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(entryTime)),
                            ReferenceBody = body,
                            Time = entryTime,
                            Velocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(entryTime))
                        };
                        yield break;
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
                            body.Radius - maxAtmosphereAltitude);

                        if (groundRangeExit <= entryTime)
                            groundRangeExit = startingState.Time + patch.SpaceOrbit.timeToPe;

                        double iterationSize = (groundRangeExit - entryTime) / 100.0;
                        double t;
                        bool groundImpact = false;
                        for (t = entryTime; t < groundRangeExit; t += iterationSize)
                        {
                            Vector3d pos = patch.SpaceOrbit.getRelativePositionAtUT(t);
                            double groundAltitude = GetGroundAltitude(body, CalculateRotatedPosition(body, Util.SwapYZ(pos), t))
                                + body.Radius;
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
                            patch.ImpactPosition = CalculateRotatedPosition(body, patch.RawImpactPosition.Value, t);
                            patch.ImpactVelocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(t));
                            patchesBackBuffer_.Add(patch);
                            AddPatch_outState = null;
                            yield break;
                        }
                        else
                        {
                            // no impact, just add the space orbit
                            patchesBackBuffer_.Add(patch);
                            if (nextPatch != null)
                            {
                                AddPatch_outState = new VesselState
                                {
                                    Position = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(patch.EndTime)),
                                    Velocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(patch.EndTime)),
                                    ReferenceBody = nextPatch == null ? body : nextPatch.referenceBody,
                                    Time = patch.EndTime,
                                    StockPatch = nextPatch
                                };
                                yield break;
                            }
                            else
                            {
                                AddPatch_outState = null;
                                yield break;
                            }
                        }
                    }
                }
                else
                {
                    if (patch.StartingState.ReferenceBody != attachedVessel.mainBody)
                    {
                        // currently, we can't handle predictions for another body, so we stop
                        AddPatch_outState = null;
                        yield break;
                    }

                    // simulate atmospheric flight (drag and lift), until impact or atmosphere exit
                    // (typically for an aerobraking maneuver) assuming a constant angle of attack
                    patch.IsAtmospheric = true;
                    patch.StartingState.StockPatch = null;

                    // lower dt would be more accurate, but a tradeoff has to be found between performances and accuracy
                    double dt = Settings.fetch.IntegrationStepSize;

                    // some shallow entries can result in very long flight. For performances reasons,
                    // we limit the prediction duration
                    int maxIterations = (int)(60.0 * 60.0 / dt);

                    int chunkSize = 128;

                    // time between two consecutive stored positions (more intermediate positions are computed for better accuracy),
                    // also used for ground collision checks
                    double trajectoryInterval = 10.0;

                    var buffer = new List<Point[]>
                    {
                        new Point[chunkSize]
                    };
                    int nextPosIdx = 0;

                    SimulationState state;
                    state.position = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(entryTime));
                    state.velocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(entryTime));

                    // Initialize a patch with zero acceleration
                    Vector3d currentAccel = new Vector3d(0.0, 0.0, 0.0);


                    double currentTime = entryTime;
                    double lastPositionStoredUT = 0;
                    Vector3d lastPositionStored = new Vector3d();
                    bool hitGround = false;
                    int iteration = 0;
                    int incrementIterations = 0;
                    int minIterationsPerIncrement = maxIterations / Settings.fetch.MaxFramesPerPatch;

                    #region Acceleration Functor

                    // function that calculates the acceleration under current parmeters
                    Func<Vector3d, Vector3d, Vector3d> accelerationFunc = (position, velocity) =>
                    {
                        Profiler.Start("accelerationFunc inside");

                        // gravity acceleration
                        double R_ = position.magnitude;
                        Vector3d accel_g = position * (-body.gravParameter / (R_ * R_ * R_));

                        // aero force
                        Vector3d vel_air = velocity - body.getRFrmVel(body.position + position);

                        double aoa = profile.GetAngleOfAttack(body, position, vel_air);

                        Profiler.Start("GetForces");
                        Vector3d force_aero = aerodynamicModel_.GetForces(body, position, vel_air, aoa);
                        Profiler.Stop("GetForces");

                        Vector3d accel = accel_g + force_aero / aerodynamicModel_.mass;

                        Profiler.Stop("accelerationFunc inside");
                        return accel;
                    };
                    #endregion


                    #region Integration Loop

                    while (true)
                    {
                        ++iteration;
                        ++incrementIterations;

                        if (incrementIterations > minIterationsPerIncrement && incrementTime_.ElapsedMilliseconds > MaxIncrementTime)
                        {
                            yield return false;
                            incrementIterations = 0;
                        }


                        double R = state.position.magnitude;
                        double altitude = R - body.Radius;
                        double atmosphereCoeff = altitude / maxAtmosphereAltitude;
                        if (hitGround
                            || atmosphereCoeff <= 0.0 || atmosphereCoeff >= 1.0
                            || iteration == maxIterations || currentTime > patch.EndTime)
                        {
                            //Util.PostSingleScreenMessage("atmo force", "Atmospheric accumulated force: " + accumulatedForces.ToString("0.00"));

                            if (hitGround || atmosphereCoeff <= 0.0)
                            {
                                patch.RawImpactPosition = state.position;
                                patch.ImpactPosition = CalculateRotatedPosition(body, patch.RawImpactPosition.Value, currentTime);
                                patch.ImpactVelocity = state.velocity;
                            }

                            patch.EndTime = Math.Min(currentTime, patch.EndTime);

                            int totalCount = (buffer.Count - 1) * chunkSize + nextPosIdx;
                            patch.AtmosphericTrajectory = new Point[totalCount];
                            int outIdx = 0;
                            foreach (var chunk in buffer)
                            {
                                foreach (var p in chunk)
                                {
                                    if (outIdx == totalCount)
                                        break;
                                    patch.AtmosphericTrajectory[outIdx++] = p;
                                }
                            }

                            if (iteration == maxIterations)
                            {
                                ScreenMessages.PostScreenMessage("WARNING: trajectory prediction stopped, too many iterations");
                                patchesBackBuffer_.Add(patch);
                                AddPatch_outState = null;
                                yield break;
                            }
                            else if (atmosphereCoeff <= 0.0 || hitGround)
                            {
                                patchesBackBuffer_.Add(patch);
                                AddPatch_outState = null;
                                yield break;
                            }
                            else
                            {
                                patchesBackBuffer_.Add(patch);
                                AddPatch_outState = new VesselState
                                {
                                    Position = state.position,
                                    Velocity = state.velocity,
                                    ReferenceBody = body,
                                    Time = patch.EndTime
                                };
                                yield break;
                            }
                        }

                        Vector3d lastAccel = currentAccel;
                        SimulationState lastState = state;

                        Profiler.Start("IntegrationStep");

                        // Verlet integration (more precise than using the velocity)
                        // state = VerletStep(state, accelerationFunc, dt);
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
                        state.position += 0.5 * TimeWarp.fixedDeltaTime * currentAccel * dt;
                        state.velocity += 0.5 * TimeWarp.fixedDeltaTime * (currentAccel - lastAccel);

                        Profiler.Stop("IntegrationStep");

                        // calculate gravity and aerodynamic force
                        Vector3d gravityAccel = lastState.position * (-body.gravParameter / (R * R * R));
                        Vector3d aerodynamicForce = (currentAccel - gravityAccel) / aerodynamicModel_.mass;

                        // acceleration in the vessel reference frame is acceleration - gravityAccel
                        maxAccelBackBuffer_ = Math.Max(
                            (float)(aerodynamicForce.magnitude / aerodynamicModel_.mass),
                            maxAccelBackBuffer_);

                        #region Impact Calculation

                        Profiler.Start("AddPatch#impact");

                        double interval = altitude < 10000.0 ? trajectoryInterval * 0.1 : trajectoryInterval;
                        if (currentTime >= lastPositionStoredUT + interval)
                        {
                            double groundAltitude = GetGroundAltitude(body, CalculateRotatedPosition(body, state.position, currentTime));
                            if (lastPositionStoredUT > 0)
                            {
                                // check terrain collision, to detect impact on mountains etc.
                                Vector3 rayOrigin = lastPositionStored;
                                Vector3 rayEnd = state.position;
                                double absGroundAltitude = groundAltitude + body.Radius;
                                if (absGroundAltitude > rayEnd.magnitude)
                                {
                                    hitGround = true;
                                    float coeff = Math.Max(0.01f, (float)((absGroundAltitude - rayOrigin.magnitude)
                                        / (rayEnd.magnitude - rayOrigin.magnitude)));
                                    state.position = rayEnd * coeff + rayOrigin * (1.0f - coeff);
                                    currentTime = currentTime * coeff + lastPositionStoredUT * (1.0f - coeff);
                                }
                            }

                            lastPositionStoredUT = currentTime;
                            if (nextPosIdx == chunkSize)
                            {
                                buffer.Add(new Point[chunkSize]);
                                nextPosIdx = 0;
                            }
                            Vector3d nextPos = state.position;
                            if (Settings.fetch.BodyFixedMode)
                            {
                                nextPos = CalculateRotatedPosition(body, nextPos, currentTime);
                            }
                            buffer.Last()[nextPosIdx].aerodynamicForce = aerodynamicForce;
                            buffer.Last()[nextPosIdx].orbitalVelocity = state.velocity;
                            buffer.Last()[nextPosIdx].groundAltitude = (float)groundAltitude;
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
                patchesBackBuffer_.Add(patch);
                if (nextPatch != null)
                {
                    AddPatch_outState = new VesselState
                    {
                        Position = Util.SwapYZ(patch.SpaceOrbit.getRelativePositionAtUT(patch.EndTime)),
                        Velocity = Util.SwapYZ(patch.SpaceOrbit.getOrbitalVelocityAtUT(patch.EndTime)),
                        ReferenceBody = nextPatch == null ? body : nextPatch.referenceBody,
                        Time = patch.EndTime,
                        StockPatch = nextPatch
                    };
                    yield break;
                }
                else
                {
                    AddPatch_outState = null;
                    yield break;
                }
            }
        }

        public static Vector3 CalculateRotatedPosition(CelestialBody body, Vector3 relativePosition, double time)
        {
            float angle = (float)(-(time - Planetarium.GetUniversalTime()) * body.angularVelocity.magnitude / Math.PI * 180.0);
            Quaternion bodyRotation = Quaternion.AngleAxis(angle, body.angularVelocity.normalized);
            return bodyRotation * relativePosition;
        }

        public static Vector3d GetWorldPositionAtUT(Orbit orbit, double ut)
        {
            Vector3d worldPos = Util.SwapYZ(orbit.getRelativePositionAtUT(ut));
            if (orbit.referenceBody != FlightGlobals.Bodies[0])
                worldPos += GetWorldPositionAtUT(orbit.referenceBody.orbit, ut);
            return worldPos;
        }

    }
}
