/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

using ferram4;
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
            public CelestialBody referenceBody;
            public double time; // universal time
            public Vector3d position; // position in world frame relatively to the reference body
            public Vector3d velocity; // velocity in world frame relatively to the reference body

            public VesselState(Vessel vessel)
            {
                referenceBody = vessel.orbit.referenceBody;
                time = Planetarium.GetUniversalTime();
                position = vessel.GetWorldPos3D() - referenceBody.position;
                velocity = vessel.obt_velocity;
            }

            public VesselState()
            {
            }
        }

        public class Patch
        {
            public VesselState startingState;
            public double endTime;
            public bool isAtmospheric;
            public Vector3[] atmosphericTrajectory; // position array in body space (world frame centered on the body) ; only used when isAtmospheric is true
            public Orbit spaceOrbit; // only used when isAtmospheric is false
            public Vector3? impactPosition;
            public Vector3 impactVelocity;
        }

        private static Trajectory fetch_;
        public static Trajectory fetch { get { return fetch_; } }

        private Vessel vessel_;
        private VesselAerodynamicModel aerodynamicModel_;
        private List<Patch> patches_ = new List<Patch>();
        public List<Patch> patches { get { return patches_; } }
        private readonly int MaxPatchCount = 3;

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
            if ((HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION) && FlightGlobals.ActiveVessel != null)
            {
                ComputeTrajectory(FlightGlobals.ActiveVessel);
            }
        }

        public void ComputeTrajectory(Vessel vessel)
        {
            patches_.Clear();
            maxaccel = 0;

            vessel_ = vessel;

            if (vessel == null)
                return;

            if (aerodynamicModel_ == null || !aerodynamicModel_.isValidFor(vessel))
                aerodynamicModel_ = new VesselAerodynamicModel(vessel);
            else
                aerodynamicModel_.IncrementalUpdate();

            var state = vessel.LandedOrSplashed ? null : new VesselState(vessel);
            for (int patchIdx = 0; patchIdx < MaxPatchCount; ++patchIdx)
            {
                if (state == null)
                    break;
                state = AddPatch(state);
            }
        }

        // relativePosition is in world frame, but relative to the body
        private double GetGroundAltitude(CelestialBody body, Vector3 relativePosition)
        {
            if (body.pqsController == null)
                return body.Radius;

            double lat = body.GetLatitude(relativePosition + body.position) / 180.0 * Math.PI;
            double lon = body.GetLongitude(relativePosition + body.position) / 180.0 * Math.PI;
            Vector3d rad = new Vector3d(Math.Cos(lat) * Math.Cos(lon), Math.Sin(lat), Math.Cos(lat) * Math.Sin(lon));
            double elevation = body.pqsController.GetSurfaceHeight(rad) - body.Radius;
            return Math.Max(elevation, 0.0);
        }

        private VesselState AddPatch(VesselState startingState)
        {
            CelestialBody body = startingState.referenceBody;

            var patch = new Patch();
            patch.startingState = startingState;           
            patch.isAtmospheric = false;
            patch.spaceOrbit = new Orbit();
            patch.spaceOrbit.UpdateFromStateVectors(startingState.position, startingState.velocity, body, startingState.time);
            patch.endTime = patch.startingState.time + patch.spaceOrbit.period;

            // TODO: predict encounters and use maneuver nodes
            // easy to do for encounters and maneuver nodes before the first atmospheric entry (just follow the KSP flight plan)
            // more difficult to do after aerobraking or other custom trajectory modifications (need to implement independent encounter algorithm? snap future maneuver nodes to the modified trajectory?)

            if (patch.spaceOrbit.PeA < body.maxAtmosphereAltitude)
            {
                double entryTime;
                if (startingState.position.magnitude <= body.Radius + body.maxAtmosphereAltitude)
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

                    while (to - from > 5.0)
                    {
                        double middle = (from + to) * 0.5;
                        if (patch.spaceOrbit.getRelativePositionAtUT(middle).magnitude < body.Radius + body.maxAtmosphereAltitude)
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

                if (entryTime > startingState.time + 10.0)
                {
                    // add the space patch before atmospheric entry
                    patch.endTime = entryTime;

                    if (body.atmosphere)
                    {
                        patches_.Add(patch);
                        return new VesselState
                        {
                            position = patch.spaceOrbit.getRelativePositionAtUT(entryTime),
                            referenceBody = body,
                            time = entryTime,
                            velocity = patch.spaceOrbit.getOrbitalVelocityAtUT(entryTime)
                        };
                    }
                    else
                    {
                        // the body has no atmosphere, so what we actually computed is the impact on the body surface
                        // now, go back in time until the impact point is above the ground to take ground height in account
                        // we assume the ground is horizontal around the impact position
                        double groundAltitude = GetGroundAltitude(body, predictImpactPosition(body, patch.spaceOrbit.getRelativePositionAtUT(entryTime), entryTime)) + body.Radius;

                        double iterationSize = 1.0;
                        while (entryTime > startingState.time + iterationSize && patch.spaceOrbit.getRelativePositionAtUT(entryTime).magnitude < groundAltitude)
                            entryTime -= iterationSize;

                        patch.endTime = entryTime;
                        patch.impactPosition = predictImpactPosition(body, patch.spaceOrbit.getRelativePositionAtUT(entryTime), entryTime);
                        patch.impactVelocity = patch.spaceOrbit.getOrbitalVelocityAtUT(entryTime);
                        patches_.Add(patch);
                        return null;
                    }
                }
                else
                {
                    // simulate atmospheric flight (drag and lift), until landing (more likely to be a crash as we don't predict user piloting) or atmosphere exit (typically for an aerobraking maneuver)
                    // the simulation assumes a constant angle of attack

                    patch.isAtmospheric = true;

                    double G = 6.674E-11;
                    double dt = 0.15; // lower dt would be more accurate, but a tradeoff has to be found between performances and accuracy

                    int maxIterations = (int)(30.0 * 60.0 / dt); // some shallow entries can result in very long flight, for performances reasons, we limit the prediction duration

                    int chunkSize = 128;
                    double trajectoryInterval = 5.0; // time between two consecutive stored positions (more intermediate positions are computed for better accuracy)
                    var buffer = new List<Vector3[]>();
                    buffer.Add(new Vector3[chunkSize]);
                    int nextPosIdx = 0;
                    
                    Vector3d pos = patch.spaceOrbit.getRelativePositionAtUT(entryTime);
                    Vector3d vel = patch.spaceOrbit.getOrbitalVelocityAtUT(entryTime);
                    //Util.PostSingleScreenMessage("initial vel", "initial vel = " + vel);
                    double currentTime = entryTime;
                    double lastPositionStored = 0;
                    int iteration = 0;
                    while (true)
                    {
                        ++iteration;

                        double R = pos.magnitude;
                        double altitude = R - body.Radius;
                        double atmosphereCoeff = altitude / body.maxAtmosphereAltitude;
                        if (atmosphereCoeff <= 0.0 || atmosphereCoeff >= 1.0 || iteration == maxIterations)
                        {
                            if (atmosphereCoeff <= 0.0)
                            {
                                //rewind trajectory a bit to get actual intersection with the ground (we assume the ground is horizontal around the impact position)
                                double groundAltitude = GetGroundAltitude(body, predictImpactPosition(body, pos, currentTime)) + body.Radius;
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
                                    pos = buffer.Last()[nextPosIdx - 1];
                                }

                                patch.impactPosition = predictImpactPosition(body, pos, currentTime);
                                patch.impactVelocity = vel;
                            }

                            patch.endTime = currentTime;

                            int totalCount = (buffer.Count - 1) * chunkSize + nextPosIdx;
                            patch.atmosphericTrajectory = new Vector3[totalCount];
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
                                    time = currentTime
                                };
                            }
                        }

                        Vector3d gravityAccel = pos * (-G * body.Mass / (R * R * R));
                        vel += gravityAccel * dt;
                        //Util.PostSingleScreenMessage("prediction vel", "prediction vel = " + vel);
                        Vector3d airVelocity = vel - body.getRFrmVel(body.position + pos);
                        double angleOfAttack = DescentProfile.fetch.GetAngleOfAttack(body, pos, airVelocity);
                        Vector3d acceleration = aerodynamicModel_.computeForces(body, pos, airVelocity, angleOfAttack, dt) / aerodynamicModel_.mass;
                        maxaccel = Math.Max((float) acceleration.magnitude, maxaccel);

                        vel += acceleration * dt;
                        pos += vel * dt;
                        currentTime += dt;

                        double interval = altitude < 15000.0 ? trajectoryInterval * 0.1 : trajectoryInterval;
                        if (currentTime >= lastPositionStored + interval)
                        {
                            lastPositionStored = currentTime;
                            if (nextPosIdx == chunkSize)
                            {
                                buffer.Add(new Vector3[chunkSize]);
                                nextPosIdx = 0;
                            }
                            buffer.Last()[nextPosIdx++] = pos;
                        }
                    }
                }
            }
            else
            {
                // no atmospheric entry, just add the space orbit
                patches_.Add(patch);
                return null;
            }
        }

        // TODO : double check this function, I suspect it is not accurate
        private static Vector3 predictImpactPosition(CelestialBody body, Vector3 relativePosition, double time)
        {
            float angle = (float)(-(time - Planetarium.GetUniversalTime()) * body.angularVelocity.magnitude / Math.PI * 180.0);
            Quaternion bodyRotation = Quaternion.AngleAxis(angle, body.angularVelocity.normalized);
            return bodyRotation * relativePosition;
        }

        private Vector3d GetWorldPositionAtUT(Orbit orbit, double ut)
        {
            Vector3d worldPos = orbit.getRelativePositionAtUT(ut);
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
                Vector3d airVelocity = bodySpaceVelocity - body.getRFrmVel(body.position + bodySpacePosition);
                double machNumber = FARAeroUtil.GetMachNumber(body, altitudeAboveSea, airVelocity);

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
