/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

//using ferram4;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    // this class handles trajectory prediction (performing a lightweight physical simulation to predict a vessel trajectory in space and atmosphere)
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class Trajectory : MonoBehaviour
    {
        public class VesselState
        {
            public CelestialBody referenceBody { get; set; }
            public double time { get; set; } // universal time
            public Vector3d position { get; set; } // position in world frame relatively to the reference body
            public Vector3d velocity { get; set; } // velocity in world frame relatively to the reference body
            public Orbit stockPatch { get; set; } // tells wether the patch starting from this state is superimposed on a stock KSP patch, or null if something makes it diverge (atmospheric entry for example)

            public VesselState(Vessel vessel)
            {
                referenceBody = vessel.orbit.referenceBody;
                time = Planetarium.GetUniversalTime();
                position = vessel.GetWorldPos3D() - referenceBody.position;
                velocity = vessel.obt_velocity;
                stockPatch = vessel.orbit;
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
            public Vector3 airVelocity;
        }

        public class Patch
        {
            public VesselState startingState { get; set; }
            public double endTime { get; set; }
            public bool isAtmospheric { get; set; }
            public Point[] atmosphericTrajectory { get; set; } // position array in body space (world frame centered on the body) ; only used when isAtmospheric is true
            public Orbit spaceOrbit { get; set; } // only used when isAtmospheric is false
            public Vector3? impactPosition { get; set; }
            public Vector3? rawImpactPosition { get; set; }
            public Vector3 impactVelocity { get; set; }

            public Point GetInfo(float altitudeAboveSeaLevel)
            {
                if(!isAtmospheric)
                    throw new Exception("Trajectory info available only for atmospheric patches");

                if (atmosphericTrajectory.Length == 1)
                    return atmosphericTrajectory[0];
                else if (atmosphericTrajectory.Length == 0)
                    return new Point();

                float absAltitude = (float)startingState.referenceBody.Radius + altitudeAboveSeaLevel;
                float sqMag = absAltitude * absAltitude;

                // TODO: optimize by doing a dichotomic search (this function assumes that altitude variation is monotonic anyway)
                int idx = 1;
                while (idx < atmosphericTrajectory.Length && atmosphericTrajectory[idx].pos.sqrMagnitude > sqMag)
                    ++idx;

                float coeff = (absAltitude - atmosphericTrajectory[idx].pos.magnitude) / Mathf.Max(0.00001f, atmosphericTrajectory[idx-1].pos.magnitude - atmosphericTrajectory[idx].pos.magnitude);
                coeff = Math.Min(1.0f, Math.Max(0.0f, coeff));

                Point res = new Point();
                res.pos = atmosphericTrajectory[idx].pos * (1.0f - coeff) + atmosphericTrajectory[idx-1].pos * coeff;
                res.aerodynamicForce = atmosphericTrajectory[idx].aerodynamicForce * (1.0f - coeff) + atmosphericTrajectory[idx - 1].aerodynamicForce * coeff;
                res.orbitalVelocity = atmosphericTrajectory[idx].orbitalVelocity * (1.0f - coeff) + atmosphericTrajectory[idx - 1].orbitalVelocity * coeff;
                res.airVelocity = atmosphericTrajectory[idx].airVelocity * (1.0f - coeff) + atmosphericTrajectory[idx - 1].airVelocity * coeff;

                return res;
            }
        }

        private static Trajectory fetch_;
        public static Trajectory fetch { get { return fetch_; } }

        private Vessel vessel_;
        private VesselAerodynamicModel aerodynamicModel_;
        private List<Patch> patches_ = new List<Patch>();
        public List<Patch> patches { get { return patches_; } }

        public float maxaccel;

        private Vector3? targetPosition_;
        private double targetSetTime_;
        private CelestialBody targetBody_;

        public static void SetTarget(CelestialBody body = null, Vector3? relativePosition = null)
        {
            fetch.targetBody_ = body;
            if (body != null && relativePosition.HasValue)
            {
                fetch.targetPosition_ = body.transform.InverseTransformDirection((Vector3)relativePosition);
            }
            else
            {
                fetch.targetPosition_ = relativePosition;
            }
            fetch.targetSetTime_ = Planetarium.GetUniversalTime();
        }

        public Vector3? targetPosition { get { return targetPosition_.HasValue ? (Vector3?)targetBody_.transform.TransformDirection(targetPosition_.Value) : null; } }
        public CelestialBody targetBody { get { return targetBody_; } }

        public void InvalidateAerodynamicModel()
        {
            aerodynamicModel_.Invalidate();
        }

        public void Start()
        {
            fetch_ = this;
        }

        public void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.Parts.Count != 0 && (MapView.MapIsEnabled || targetPosition_.HasValue))
            {
                ComputeTrajectory(FlightGlobals.ActiveVessel, DescentProfile.fetch);
            }
        }

        public void ComputeTrajectory(Vessel vessel, float AoA)
        {
            DescentProfile profile = new DescentProfile(AoA);
            ComputeTrajectory(vessel, profile);
        }

        public void ComputeTrajectory(Vessel vessel, DescentProfile profile)
        {
            patches_.Clear();
            maxaccel = 0;

            vessel_ = vessel;

            if (vessel == null)
                return;

            if (aerodynamicModel_ == null || !aerodynamicModel_.isValidFor(vessel, vessel.mainBody))
                aerodynamicModel_ = new VesselAerodynamicModel(vessel, vessel.mainBody);
            else
                aerodynamicModel_.IncrementalUpdate();

            var state = vessel.LandedOrSplashed ? null : new VesselState(vessel);
            for (int patchIdx = 0; patchIdx < Settings.fetch.MaxPatchCount; ++patchIdx)
            {
                if (state == null)
                    break;

                var maneuverNodes = vessel_.patchedConicSolver.maneuverNodes;
                foreach (var node in maneuverNodes)
                {
                    if (node.UT == state.time)
                    {
                        state.velocity += node.GetBurnVector(createOrbitFromState(state));
                        break;
                    }
                }

                state = AddPatch(state, profile);
            }
        }

        // relativePosition is in world frame, but relative to the body
        private double GetGroundAltitude(CelestialBody body, Vector3 relativePosition)
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
            if (!body.atmosphere) return 0;
            if (body.useLegacyAtmosphere)
            {
                //Atmosphere actually cuts out when exp(-altitude / scale height) = 1e-6
                return -body.atmosphereScaleHeight * 1000 * Math.Log(1e-6);
            }
            else
            {
                return body.pressureCurve.keys.Last().time * 1000;
            }
        }

        private Orbit createOrbitFromState(VesselState state)
        {
            var orbit = new Orbit();
            orbit.UpdateFromStateVectors(Util.SwapYZ(state.position), Util.SwapYZ(state.velocity), state.referenceBody, state.time);
            return orbit;
        }

        private VesselState AddPatch(VesselState startingState, DescentProfile profile)
        {
            CelestialBody body = startingState.referenceBody;

            var patch = new Patch();
            patch.startingState = startingState;           
            patch.isAtmospheric = false;
            patch.spaceOrbit = startingState.stockPatch ?? createOrbitFromState(startingState);
            patch.endTime = patch.startingState.time + patch.spaceOrbit.period;

            var flightPlan = vessel_.patchedConicSolver.flightPlan;
            if (!flightPlan.Any())
            {
                // when there is no maneuver node, the flight plan is empty, so we populate it with the current orbit and associated encounters etc.
                flightPlan = new List<Orbit>();
                for (var orbit = vessel_.orbit; orbit != null && orbit.activePatch; orbit = orbit.nextPatch)
                {
                    flightPlan.Add(orbit);
                }
            }

            Orbit nextStockPatch = null;
            if (startingState.stockPatch != null)
            {
                int planIdx = flightPlan.IndexOf(startingState.stockPatch);
                if (planIdx >= 0 && planIdx < flightPlan.Count - 1)
                {
                    nextStockPatch = flightPlan[planIdx + 1];
                }
            }

            if (nextStockPatch != null)
            {
                patch.endTime = nextStockPatch.StartUT;
            }

            // TODO: predict encounters and use maneuver nodes
            // easy to do for encounters and maneuver nodes before the first atmospheric entry (just follow the KSP flight plan)
            // more difficult to do after aerobraking or other custom trajectory modifications (need to implement independent encounter algorithm? snap future maneuver nodes to the modified trajectory?)

            double maxAtmosphereAltitude = RealMaxAtmosphereAltitude(body);

            double minAltitude = patch.spaceOrbit.PeA;
            if (patch.endTime < startingState.time + patch.spaceOrbit.timeToPe)
            {
                minAltitude = patch.spaceOrbit.getRelativePositionAtUT(patch.endTime).magnitude;
            }
            if (minAltitude < maxAtmosphereAltitude)
            {
                double entryTime;
                if (startingState.position.magnitude <= body.Radius + maxAtmosphereAltitude)
                {
                    // whole orbit is inside the atmosphere
                    entryTime = startingState.time;
                }
                else
                {
                    // binary search of entry time in atmosphere
                    // I guess an analytic solution could be found, but I'm too lazy to search it
                    double from = startingState.time;
                    double to = from + patch.spaceOrbit.timeToPe;

                    while (to - from > 0.1)
                    {
                        double middle = (from + to) * 0.5;
                        if (patch.spaceOrbit.getRelativePositionAtUT(middle).magnitude < body.Radius + maxAtmosphereAltitude)
                        {
                            to = middle;
                        }
                        else
                        {
                            from = middle;
                        }
                    }

                    entryTime = to;
                }

                if (entryTime > startingState.time + 0.1)
                {
                    // add the space patch before atmospheric entry
                    patch.endTime = entryTime;

                    if (body.atmosphere)
                    {
                        patches_.Add(patch);
                        return new VesselState
                        {
                            position = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(entryTime)),
                            referenceBody = body,
                            time = entryTime,
                            velocity = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(entryTime))
                        };
                    }
                    else
                    {
                        // the body has no atmosphere, so what we actually computed is the impact on the body surface
                        // now, go back in time until the impact point is above the ground to take ground height in account
                        // we assume the ground is horizontal around the impact position
                        double groundAltitude = GetGroundAltitude(body, calculateRotatedPosition(body, Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(entryTime)), entryTime)) + body.Radius;

                        double iterationSize = 1.0;
                        while (entryTime > startingState.time + iterationSize && patch.spaceOrbit.getRelativePositionAtUT(entryTime).magnitude < groundAltitude)
                            entryTime -= iterationSize;

                        patch.endTime = entryTime;
                        patch.rawImpactPosition = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(entryTime));
                        patch.impactPosition = calculateRotatedPosition(body, patch.rawImpactPosition.Value, entryTime);
                        patch.impactVelocity = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(entryTime));
                        patches_.Add(patch);
                        return null;
                    }
                }
                else
                {
                    // simulate atmospheric flight (drag and lift), until landing (more likely to be a crash as we don't predict user piloting) or atmosphere exit (typically for an aerobraking maneuver)
                    // the simulation assumes a constant angle of attack

                    patch.isAtmospheric = true;
                    patch.startingState.stockPatch = null;

                    double dt = 0.1; // lower dt would be more accurate, but a tradeoff has to be found between performances and accuracy

                    int maxIterations = (int)(30.0 * 60.0 / dt); // some shallow entries can result in very long flight, for performances reasons, we limit the prediction duration

                    int chunkSize = 128;
                    double trajectoryInterval = 5.0; // time between two consecutive stored positions (more intermediate positions are computed for better accuracy)
                    var buffer = new List<Point[]>();
                    buffer.Add(new Point[chunkSize]);
                    int nextPosIdx = 0;
                    
                    Vector3d pos = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(entryTime));
                    Vector3d vel = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(entryTime));
                    Vector3d prevPos = pos - vel * dt;
                    //Util.PostSingleScreenMessage("initial vel", "initial vel = " + vel);
                    double currentTime = entryTime;
                    double lastPositionStored = 0;
                    int iteration = 0;
                    while (true)
                    {
                        ++iteration;

                        double R = pos.magnitude;
                        double altitude = R - body.Radius;
                        double atmosphereCoeff = altitude / maxAtmosphereAltitude;
                        if (atmosphereCoeff <= 0.0 || atmosphereCoeff >= 1.0 || iteration == maxIterations || currentTime > patch.endTime)
                        {
                            if (atmosphereCoeff <= 0.0)
                            {
                                //rewind trajectory a bit to get actual intersection with the ground (we assume the ground is horizontal around the impact position)
                                double groundAltitude = GetGroundAltitude(body, calculateRotatedPosition(body, pos, currentTime)) + body.Radius;
                                if (nextPosIdx == 0 && buffer.Count > 1)
                                {
                                    nextPosIdx = chunkSize;
                                    buffer.RemoveAt(buffer.Count - 1);
                                }
                                while (pos.magnitude <= groundAltitude)
                                {
                                    --nextPosIdx;
                                    currentTime -= dt;
                                    if (nextPosIdx == 0)
                                    {
                                        if (buffer.Count == 1)
                                            break;
                                        nextPosIdx = chunkSize;
                                        buffer.RemoveAt(buffer.Count - 1);
                                    }
                                    pos = buffer.Last()[nextPosIdx - 1].pos;
                                }

                                if (Settings.fetch.BodyFixedMode) {
                                    //if we do fixed-mode calculations, pos is already rotated
                                    patch.impactPosition = pos;
                                    patch.rawImpactPosition = calculateRotatedPosition(body, patch.impactPosition.Value, 2.0 * Planetarium.GetUniversalTime() - currentTime);
                                } else {
                                    patch.rawImpactPosition = pos;
                                    patch.impactPosition = calculateRotatedPosition(body, patch.rawImpactPosition.Value, currentTime);
                                }
                                patch.impactVelocity = vel;
                            }

                            patch.endTime = Math.Min(currentTime, patch.endTime);

                            int totalCount = (buffer.Count - 1) * chunkSize + nextPosIdx;
                            patch.atmosphericTrajectory = new Point[totalCount];
                            int outIdx = 0;
                            foreach (var chunk in buffer)
                            {
                                foreach (var p in chunk)
                                {
                                    if (outIdx == totalCount)
                                        break;
                                    patch.atmosphericTrajectory[outIdx++] = p;
                                }
                            }

                            if (iteration == maxIterations)
                            {
                                ScreenMessages.PostScreenMessage("WARNING: trajectory prediction stopped, too many iterations");
                                patches_.Add(patch);
                                return null;
                            }
                            else if (atmosphereCoeff <= 0.0)
                            {
                                patches_.Add(patch);
                                return null;
                            }
                            else
                            {
                                patches_.Add(patch);
                                return new VesselState
                                {
                                    position = pos,
                                    velocity = vel,
                                    referenceBody = body,
                                    time = patch.endTime
                                };
                            }
                        }

                        Vector3d gravityAccel = pos * (-body.gravParameter / (R * R * R));
                        
                        //Util.PostSingleScreenMessage("prediction vel", "prediction vel = " + vel);
                        Vector3d airVelocity = vel - body.getRFrmVel(body.position + pos);
                        double angleOfAttack = profile.GetAngleOfAttack(body, pos, airVelocity);
                        Vector3d aerodynamicForce = aerodynamicModel_.computeForces(body, pos, airVelocity, angleOfAttack, dt);
                        Vector3d acceleration = gravityAccel + aerodynamicForce / aerodynamicModel_.mass;
                        maxaccel = Math.Max((float) acceleration.magnitude, maxaccel);

                        //vel += acceleration * dt;
                        //pos += vel * dt;
                        
                        // Verlet integration (more precise than using the velocity)
                        Vector3d ppos = prevPos;
                        prevPos = pos;
                        pos = pos + pos - ppos + acceleration * (dt * dt);
                        vel = (pos - prevPos) / dt;

                        currentTime += dt;

                        double interval = altitude < 15000.0 ? trajectoryInterval * 0.1 : trajectoryInterval;
                        if (currentTime >= lastPositionStored + interval)
                        {
                            lastPositionStored = currentTime;
                            if (nextPosIdx == chunkSize)
                            {
                                buffer.Add(new Point[chunkSize]);
                                nextPosIdx = 0;
                            }
                            Vector3d nextPos = pos;
                            if (Settings.fetch.BodyFixedMode) {
                                nextPos = calculateRotatedPosition(body, nextPos, currentTime);
                            }
                            buffer.Last()[nextPosIdx].aerodynamicForce = aerodynamicForce;
                            buffer.Last()[nextPosIdx].orbitalVelocity = vel;
                            buffer.Last()[nextPosIdx].airVelocity = airVelocity;
                            buffer.Last()[nextPosIdx++].pos = nextPos;
                        }
                    }
                }
            }
            else
            {
                // no atmospheric entry, just add the space orbit
                patches_.Add(patch);
                if (nextStockPatch != null)
                {
                    return new VesselState
                    {
                        position = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(patch.endTime)),
                        velocity = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(patch.endTime)),
                        referenceBody = nextStockPatch == null ? body : nextStockPatch.referenceBody,
                        time = patch.endTime,
                        stockPatch = nextStockPatch
                    };
                }
                else
                    return null;
            }
        }

        // TODO : double check this function, I suspect it is not accurate
        public static Vector3 calculateRotatedPosition(CelestialBody body, Vector3 relativePosition, double time)
        {
            float angle = (float)(-(time - Planetarium.GetUniversalTime()) * body.angularVelocity.magnitude / Math.PI * 180.0);
            Quaternion bodyRotation = Quaternion.AngleAxis(angle, body.angularVelocity.normalized);
            return bodyRotation * relativePosition;
        }

        private Vector3d GetWorldPositionAtUT(Orbit orbit, double ut)
        {
            Vector3d worldPos = Util.SwapYZ(orbit.getRelativePositionAtUT(ut));
            if (orbit.referenceBody != FlightGlobals.Bodies[0])
                worldPos += GetWorldPositionAtUT(orbit.referenceBody.orbit, ut);
            return worldPos;
        }

        /*public void FixedUpdate()
        {
            if (aerodynamicModel_ != null && vessel_ != null)
            {
                Vector3d FARForce = FARBasicDragModel.debugForceAccumulator + FARWingAerodynamicModel.debugForceAccumulator;
                FARBasicDragModel.debugForceAccumulator = new Vector3d(0, 0, 0);
                FARWingAerodynamicModel.debugForceAccumulator = new Vector3d(0, 0, 0);

                CelestialBody body = vessel_.orbit.referenceBody;
                Vector3d bodySpacePosition = vessel_.GetWorldPos3D() - body.position;
                Vector3d bodySpaceVelocity = vessel_.obt_velocity;
                double altitudeAboveSea = bodySpacePosition.magnitude - body.Radius;

                double rho = FARAeroUtil.GetCurrentDensity(body, altitudeAboveSea);
                //double rho = vessel_.atmDensity;
                //double pressure = FlightGlobals.getStaticPressure(altitudeAboveSea, body);
                //double rho = FlightGlobals.getAtmDensity(pressure);

                Vector3d airVelocity = bodySpaceVelocity - body.getRFrmVel(body.position + bodySpacePosition);
                
                
                double machNumber = FARAeroUtil.GetMachNumber(body, altitudeAboveSea, airVelocity);
                //double machNumber = airVelocity.magnitude / 300.0;

                Transform vesselTransform = vessel_.ReferenceTransform;
                Vector3d vesselBackward = (Vector3d)(-vesselTransform.up.normalized);
                Vector3d vesselForward = -vesselBackward;
                Vector3d vesselUp = (Vector3d)(-vesselTransform.forward.normalized);
                Vector3d vesselRight = Vector3d.Cross(vesselUp, vesselBackward).normalized;
                double AoA = Math.Acos(Vector3d.Dot(airVelocity.normalized, vesselForward.normalized));
                if (Vector3d.Dot(airVelocity, vesselUp) > 0)
                    AoA = -AoA;

                Vector3d predictedForce = aerodynamicModel_.computeForces_FAR(rho, machNumber, airVelocity, vesselUp, AoA, 0.05);
                //Vector3d predictedForce = aerodynamicModel_.computeForces(body, bodySpacePosition, bodySpaceVelocity, 0.05);

                Vector3d localFARForce = new Vector3d(Vector3d.Dot(FARForce, vesselRight), Vector3d.Dot(FARForce, vesselUp), Vector3d.Dot(FARForce, vesselBackward));
                Vector3d localPredictedForce = new Vector3d(Vector3d.Dot(predictedForce, vesselRight), Vector3d.Dot(predictedForce, vesselUp), Vector3d.Dot(predictedForce, vesselBackward));

                Util.PostSingleScreenMessage("FAR/predict comparison", "air vel=" + Math.Floor(airVelocity.magnitude) + ", AoA=" + (AoA*180.0/Math.PI) + ", FAR force=" + localFARForce + ", predicted force=" + localPredictedForce);
            }
        }*/
    }
}
