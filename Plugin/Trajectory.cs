/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

//#define DEBUG_COMPARE_FORCES

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
            public VesselState startingState { get; set; }
            public double endTime { get; set; }
            public bool isAtmospheric { get; set; }
            public Point[] atmosphericTrajectory { get; set; } // position array in body space (world frame centered on the body) ; only used when isAtmospheric is true
            public Orbit spaceOrbit { get; set; } // only used when isAtmospheric is false
            public Vector3? impactPosition { get; set; }
            public Vector3? rawImpactPosition { get; set; }
            public Vector3? impactVelocity { get; set; }

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
                res.groundAltitude = atmosphericTrajectory[idx].groundAltitude * (1.0f - coeff) + atmosphericTrajectory[idx - 1].groundAltitude * coeff;
                res.time = atmosphericTrajectory[idx].time * (1.0f - coeff) + atmosphericTrajectory[idx - 1].time * coeff;

                return res;
            }
        }

        private int MaxIncrementTime { get { return 2; } }

        private static Trajectory fetch_;
        public static Trajectory fetch { get { return fetch_; } }

        private Vessel vessel_;
        private VesselAerodynamicModel aerodynamicModel_;
        public string AerodynamicModelName { get { return aerodynamicModel_ == null ? "Not loaded" : aerodynamicModel_.AerodynamicModelName; } }
        private List<Patch> patches_ = new List<Patch>();
        private List<Patch> patchesBackBuffer_ = new List<Patch>();
        public List<Patch> patches { get { return patches_; } }

        private Stopwatch incrementTime_;
        private IEnumerator<bool> partialComputation_;

        private float maxAccel_;
        private float maxAccelBackBuffer_;
        public float MaxAccel { get { return maxAccel_; } }

        private Vector3? targetPosition_;
        private CelestialBody targetBody_;

        private static int errorCount_;
        public int ErrorCount { get { return errorCount_; } }

        private static float frameTime_;
        private static float computationTime_;
        public float ComputationTime { get { return computationTime_ * 0.001f; } }

        private static Stopwatch gameFrameTime_;
        private static float averageGameFrameTime_;
        public float GameFrameTime { get { return averageGameFrameTime_ * 0.001f; } }

        public void SetTarget(CelestialBody body = null, Vector3? relativePosition = null)
        {
            if (body != null && relativePosition.HasValue)
            {
                targetBody_ = body;
                targetPosition_ = body.transform.InverseTransformDirection((Vector3)relativePosition);
            }
            else
            {
                targetBody_ = null;
                targetPosition_ = null;
            }

            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (var module in FlightGlobals.ActiveVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
                {
                    module.hasTarget = targetPosition_ != null;
                    module.targetLocation = targetPosition_.HasValue ? targetPosition_.Value : new Vector3();
                    module.targetReferenceBody = targetBody_ == null ? "" : targetBody_.name;
                }
            }
        }

        public Vector3? targetPosition { get { return targetPosition_.HasValue ? (Vector3?)targetBody_.transform.TransformDirection(targetPosition_.Value) : null; } }
        public Vector3? groundRelativeTargetPosition { get { return targetPosition_; } }
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
            computationTime_ = computationTime_ * 0.99f + frameTime_ * 0.01f;
            float offset = frameTime_ - computationTime_;
            frameTime_ = 0;

            if (gameFrameTime_ != null)
            {
                float t = (float)gameFrameTime_.ElapsedMilliseconds;
                averageGameFrameTime_ = averageGameFrameTime_ * 0.99f + t * 0.01f;
            }
            gameFrameTime_ = Stopwatch.StartNew();

            if (HighLogic.LoadedScene == GameScenes.FLIGHT && vessel_ != FlightGlobals.ActiveVessel)
            {
                TrajectoriesVesselSettings module = FlightGlobals.ActiveVessel == null ? null : FlightGlobals.ActiveVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                CelestialBody body = module == null ? null : FlightGlobals.Bodies.FirstOrDefault(b => b.name == module.targetReferenceBody);

                if (body == null || !module.hasTarget)
                {
                    SetTarget();
                }
                else
                {
                    SetTarget(body, body.transform.TransformDirection(module.targetLocation));
                }
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.Parts.Count != 0 && ((MapView.MapIsEnabled && Settings.fetch.DisplayTrajectories) || Settings.fetch.AlwaysUpdate || targetPosition_.HasValue))
            {
                ComputeTrajectory(FlightGlobals.ActiveVessel, DescentProfile.fetch, true);
            }
        }

        public void ComputeTrajectory(Vessel vessel, float AoA, bool incremental)
        {
            DescentProfile profile = new DescentProfile(AoA);
            ComputeTrajectory(vessel, profile, incremental);
        }

        public void ComputeTrajectory(Vessel vessel, DescentProfile profile, bool incremental)
        {
			try
            {
                incrementTime_ = Stopwatch.StartNew();

                if (partialComputation_ == null || vessel != vessel_)
                {
                    patchesBackBuffer_.Clear();
                    maxAccelBackBuffer_ = 0;

                    vessel_ = vessel;

                    if (vessel == null)
                    {
                        patches_.Clear();
                        return;
                    }

                    if (partialComputation_ != null)
                        partialComputation_.Dispose();
                    partialComputation_ = computeTrajectoryIncrement(vessel, profile).GetEnumerator();
                }

                bool finished = !partialComputation_.MoveNext();

                if (finished)
                {
                    var tmp = patches_;
                    patches_ = patchesBackBuffer_;
                    patchesBackBuffer_ = tmp;

                    maxAccel_ = maxAccelBackBuffer_;

                    partialComputation_.Dispose();
                    partialComputation_ = null;
                }

                frameTime_ += (float)incrementTime_.ElapsedMilliseconds;
            }
            catch (Exception)
            {
                ++errorCount_;
                throw;
            }
        }

        private IEnumerable<bool> computeTrajectoryIncrement(Vessel vessel, DescentProfile profile)
        {
            if (aerodynamicModel_ == null || !aerodynamicModel_.isValidFor(vessel, vessel.mainBody))
                aerodynamicModel_ = AerodynamicModelFactory.GetModel(vessel, vessel.mainBody);
            else
                aerodynamicModel_.IncrementalUpdate();

            var state = vessel.LandedOrSplashed ? null : new VesselState(vessel);
            for (int patchIdx = 0; patchIdx < Settings.fetch.MaxPatchCount; ++patchIdx)
            {
                if (state == null)
                    break;

                if (incrementTime_.ElapsedMilliseconds > MaxIncrementTime)
                    yield return false;

                if (null != vessel_.patchedConicSolver)
                {
                    var maneuverNodes = vessel_.patchedConicSolver.maneuverNodes;
                    foreach (var node in maneuverNodes)
                    {
                        if (node.UT == state.time)
                        {
                            state.velocity += node.GetBurnVector(createOrbitFromState(state));
                            break;
                        }
                    }
                    foreach (var result in AddPatch(state, profile))
                        yield return false;
                }
                
                state = AddPatch_outState;
            }
        }

        // relativePosition is in world frame, but relative to the body (i.e. inertial body space)
        // returns the altitude above sea level (can be negative for bodies without ocean)
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
            if (!body.atmosphere) return 0;
            // Change for 1.0 refer to atmosphereDepth
            return body.atmosphereDepth;
        }

        private Orbit createOrbitFromState(VesselState state)
        {
            var orbit = new Orbit();
            orbit.UpdateFromStateVectors(Util.SwapYZ(state.position), Util.SwapYZ(state.velocity), state.referenceBody, state.time);
			var pars = new PatchedConics.SolverParameters();
			pars.FollowManeuvers = false;
			PatchedConics.CalculatePatch(orbit, new Orbit(), state.time, pars, null);
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
        private IEnumerable<bool> AddPatch(VesselState startingState, DescentProfile profile)
        {
            if (null == vessel_.patchedConicSolver)
            {
                UnityEngine.Debug.LogWarning("Trajectories: AddPatch() attempted when patchedConicsSolver is null; Skipping.");
                yield break;
            }

            CelestialBody body = startingState.referenceBody;

            var patch = new Patch();
            patch.startingState = startingState;           
            patch.isAtmospheric = false;
            patch.spaceOrbit = startingState.stockPatch ?? createOrbitFromState(startingState);
            patch.endTime = patch.startingState.time + patch.spaceOrbit.period;

            // the flight plan does not always contain the first patches (before the first maneuver node), so we populate it with the current orbit and associated encounters etc.
            var flightPlan = new List<Orbit>();
            for (var orbit = vessel_.orbit; orbit != null && orbit.activePatch; orbit = orbit.nextPatch)
            {
                if (vessel_.patchedConicSolver.flightPlan.Contains(orbit))
                    break;
                flightPlan.Add(orbit);
            }

            foreach (var orbit in vessel_.patchedConicSolver.flightPlan)
            {
                flightPlan.Add(orbit);
            }


            Orbit nextPatch = null;
            if (startingState.stockPatch == null)
			{
				nextPatch = patch.spaceOrbit.nextPatch;
			}
			else
            {
                int planIdx = flightPlan.IndexOf(startingState.stockPatch);
                if (planIdx >= 0 && planIdx < flightPlan.Count - 1)
                {
                    nextPatch = flightPlan[planIdx + 1];
                }
            }

            if (nextPatch != null)
            {
                patch.endTime = nextPatch.StartUT;
            }

            double maxAtmosphereAltitude = RealMaxAtmosphereAltitude(body);
			if(!body.atmosphere)
			{
				maxAtmosphereAltitude = body.pqsController.mapMaxHeight;
			}

            double minAltitude = patch.spaceOrbit.PeA;
            if (patch.spaceOrbit.timeToPe < 0 || patch.endTime < startingState.time + patch.spaceOrbit.timeToPe)
            {
                minAltitude = Math.Min(patch.spaceOrbit.getRelativePositionAtUT(patch.endTime).magnitude, patch.spaceOrbit.getRelativePositionAtUT(patch.startingState.time + 1.0).magnitude) - body.Radius;
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
					entryTime = FindOrbitBodyIntersection(patch.spaceOrbit, startingState.time, startingState.time + patch.spaceOrbit.timeToPe, body.Radius + maxAtmosphereAltitude);
				}

                if (entryTime > startingState.time + 0.1 || !body.atmosphere)
                {
                    if (body.atmosphere)
                    {
						// add the space patch before atmospheric entry

						patch.endTime = entryTime;
						patchesBackBuffer_.Add(patch);
                        AddPatch_outState = new VesselState
                        {
                            position = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(entryTime)),
                            referenceBody = body,
                            time = entryTime,
                            velocity = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(entryTime))
                        };
                        yield break;
                    }
                    else
                    {
						// the body has no atmosphere, so what we actually computed is the entry inside the "ground sphere" (defined by the maximal ground altitude)
						// now we iterate until the inner ground sphere (minimal altitude), and check if we hit the ground along the way

						double groundRangeExit = FindOrbitBodyIntersection(patch.spaceOrbit, startingState.time, startingState.time + patch.spaceOrbit.timeToPe, body.Radius - maxAtmosphereAltitude);
						if (groundRangeExit <= entryTime)
							groundRangeExit = startingState.time + patch.spaceOrbit.timeToPe;

						double iterationSize = (groundRangeExit - entryTime) / 100.0;
						double t;
						bool groundImpact = false;
						for(t = entryTime; t < groundRangeExit; t += iterationSize)
						{
							Vector3d pos = patch.spaceOrbit.getRelativePositionAtUT(t);
							double groundAltitude = GetGroundAltitude(body, calculateRotatedPosition(body, Util.SwapYZ(pos), t)) + body.Radius;
							if (pos.magnitude < groundAltitude)
							{
								t -= iterationSize;
								groundImpact = true;
								break;
							}
						}

						if (groundImpact)
						{
							patch.endTime = t;
							patch.rawImpactPosition = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(t));
							patch.impactPosition = calculateRotatedPosition(body, patch.rawImpactPosition.Value, t);
							patch.impactVelocity = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(t));
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
									position = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(patch.endTime)),
									velocity = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(patch.endTime)),
									referenceBody = nextPatch == null ? body : nextPatch.referenceBody,
									time = patch.endTime,
									stockPatch = nextPatch
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
                    if (patch.startingState.referenceBody != vessel_.mainBody)
                    {
                        // in current aerodynamic prediction code, we can't handle predictions for another body, so we stop here
                        AddPatch_outState = null;
                        yield break;
                    }

                    // simulate atmospheric flight (drag and lift), until landing (more likely to be a crash as we don't predict user piloting) or atmosphere exit (typically for an aerobraking maneuver)
                    // the simulation assumes a constant angle of attack

                    patch.isAtmospheric = true;
                    patch.startingState.stockPatch = null;

                    double dt = 0.1; // lower dt would be more accurate, but a tradeoff has to be found between performances and accuracy

                    int maxIterations = (int)(30.0 * 60.0 / dt); // some shallow entries can result in very long flight, for performances reasons, we limit the prediction duration

                    int chunkSize = 128;
                    double trajectoryInterval = 10.0; // time between two consecutive stored positions (more intermediate positions are computed for better accuracy), also used for ground collision checks
                    var buffer = new List<Point[]>();
                    buffer.Add(new Point[chunkSize]);
                    int nextPosIdx = 0;
                    
                    Vector3d pos = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(entryTime));
                    Vector3d vel = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(entryTime));

					//Util.PostSingleScreenMessage("atmo start cond", "Atmospheric start: vel=" + vel.ToString("0.00") + " (mag=" + vel.magnitude.ToString("0.00") + ")");

                    Vector3d prevPos = pos - vel * dt;
                    double currentTime = entryTime;
                    double lastPositionStoredUT = 0;
                    Vector3d lastPositionStored = new Vector3d();
                    bool hitGround = false;
                    int iteration = 0;
                    int incrementIterations = 0;
                    int minIterationsPerIncrement = maxIterations / Settings.fetch.MaxFramesPerPatch;
					double accumulatedForces = 0;
                    while (true)
                    {
                        ++iteration;
                        ++incrementIterations;

                        if (incrementIterations > minIterationsPerIncrement && incrementTime_.ElapsedMilliseconds > MaxIncrementTime)
                        {
                            yield return false;
                            incrementIterations = 0;
                        }

                        double R = pos.magnitude;
                        double altitude = R - body.Radius;
                        double atmosphereCoeff = altitude / maxAtmosphereAltitude;
                        if (hitGround || atmosphereCoeff <= 0.0 || atmosphereCoeff >= 1.0 || iteration == maxIterations || currentTime > patch.endTime)
                        {
							//Util.PostSingleScreenMessage("atmo force", "Atmospheric accumulated force: " + accumulatedForces.ToString("0.00"));

                            if (hitGround || atmosphereCoeff <= 0.0)
                            {
                                patch.rawImpactPosition = pos;
                                patch.impactPosition = calculateRotatedPosition(body, patch.rawImpactPosition.Value, currentTime);
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
                                    position = pos,
                                    velocity = vel,
                                    referenceBody = body,
                                    time = patch.endTime
                                };
                                yield break;
                            }
                        }

                        Vector3d gravityAccel = pos * (-body.gravParameter / (R * R * R));
                        
                        //Util.PostSingleScreenMessage("prediction vel", "prediction vel = " + vel);
                        Vector3d airVelocity = vel - body.getRFrmVel(body.position + pos);
                        double angleOfAttack = profile.GetAngleOfAttack(body, pos, airVelocity);
                        Vector3d aerodynamicForce = aerodynamicModel_.GetForces(body, pos, airVelocity, angleOfAttack);
						accumulatedForces += aerodynamicForce.magnitude * dt;
						Vector3d acceleration = gravityAccel + aerodynamicForce / aerodynamicModel_.mass;

                        // acceleration in the vessel reference frame is acceleration - gravityAccel
                        maxAccelBackBuffer_ = Math.Max((float) (aerodynamicForce.magnitude / aerodynamicModel_.mass), maxAccelBackBuffer_);


                        //vel += acceleration * dt;
                        //pos += vel * dt;
                        
                        // Verlet integration (more precise than using the velocity)
                        Vector3d ppos = prevPos;
                        prevPos = pos;
                        pos = pos + pos - ppos + acceleration * (dt * dt);
                        vel = (pos - prevPos) / dt;

                        currentTime += dt;

                        double interval = altitude < 10000.0 ? trajectoryInterval * 0.1 : trajectoryInterval;
                        if (currentTime >= lastPositionStoredUT + interval)
                        {
                            double groundAltitude = GetGroundAltitude(body, calculateRotatedPosition(body, pos, currentTime));
                            if (lastPositionStoredUT > 0)
                            {
                                // check terrain collision, to detect impact on mountains etc.
                                Vector3 rayOrigin = lastPositionStored;
                                Vector3 rayEnd = pos;
                                double absGroundAltitude = groundAltitude + body.Radius;
                                if (absGroundAltitude > rayEnd.magnitude)
                                {
                                    hitGround = true;
                                    float coeff = Math.Max(0.01f, (float)((absGroundAltitude - rayOrigin.magnitude) / (rayEnd.magnitude - rayOrigin.magnitude)));
                                    pos = rayEnd * coeff + rayOrigin * (1.0f - coeff);
                                    currentTime = currentTime * coeff + lastPositionStoredUT * (1.0f - coeff);
                                }
                            }

                            lastPositionStoredUT = currentTime;
                            if (nextPosIdx == chunkSize)
                            {
                                buffer.Add(new Point[chunkSize]);
                                nextPosIdx = 0;
                            }
                            Vector3d nextPos = pos;
                            if (Settings.fetch.BodyFixedMode)
                            {
                                nextPos = calculateRotatedPosition(body, nextPos, currentTime);
                            }
                            buffer.Last()[nextPosIdx].aerodynamicForce = aerodynamicForce;
                            buffer.Last()[nextPosIdx].orbitalVelocity = vel;
                            buffer.Last()[nextPosIdx].groundAltitude = (float)groundAltitude;
                            buffer.Last()[nextPosIdx].time = currentTime;
                            buffer.Last()[nextPosIdx++].pos = nextPos;
                            lastPositionStored = pos;
                        }
                    }
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
                        position = Util.SwapYZ(patch.spaceOrbit.getRelativePositionAtUT(patch.endTime)),
                        velocity = Util.SwapYZ(patch.spaceOrbit.getOrbitalVelocityAtUT(patch.endTime)),
                        referenceBody = nextPatch == null ? body : nextPatch.referenceBody,
                        time = patch.endTime,
                        stockPatch = nextPatch
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

		#if DEBUG
		private static Vector3d PreviousFramePos;
		private static Vector3d PreviousFrameVelocity;
		private static double PreviousFrameTime = 0;
        public void FixedUpdate()
        {
			if (HighLogic.LoadedScene != GameScenes.FLIGHT)
				return;

			double now = Planetarium.GetUniversalTime();
			double dt = now - PreviousFrameTime;

			if (dt > 0.5 || dt < 0.0)
			{

				Vector3d bodySpacePosition = new Vector3d();
				Vector3d bodySpaceVelocity = new Vector3d();

				if (aerodynamicModel_ != null && vessel_ != null)
				{
					CelestialBody body = vessel_.orbit.referenceBody;

					bodySpacePosition = vessel_.GetWorldPos3D() - body.position;
					bodySpaceVelocity = vessel_.obt_velocity;

					double altitudeAboveSea = bodySpacePosition.magnitude - body.Radius;

					Vector3d airVelocity = bodySpaceVelocity - body.getRFrmVel(body.position + bodySpacePosition);

					#if DEBUG_COMPARE_FORCES
					double R = PreviousFramePos.magnitude;
					Vector3d gravityForce = PreviousFramePos * (-body.gravParameter / (R * R * R) * vessel_.totalMass);

					Quaternion inverseRotationFix = body.inverseRotation ? Quaternion.AngleAxis((float)(body.angularVelocity.magnitude / Math.PI * 180.0 * dt), Vector3.up) : Quaternion.identity;
					Vector3d TotalForce = (bodySpaceVelocity - inverseRotationFix * PreviousFrameVelocity) * (vessel_.totalMass / dt);
					TotalForce += bodySpaceVelocity * (dt * 0.000015); // numeric precision fix
					Vector3d ActualForce = TotalForce - gravityForce;

					Transform vesselTransform = vessel_.ReferenceTransform;
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

					Vector3d localTotalForce = new Vector3d(Vector3d.Dot(TotalForce, vesselRight), Vector3d.Dot(TotalForce, vesselUp), Vector3d.Dot(TotalForce, vesselBackward));
					Vector3d localActualForce = new Vector3d(Vector3d.Dot(ActualForce, vesselRight), Vector3d.Dot(ActualForce, vesselUp), Vector3d.Dot(ActualForce, vesselBackward));
					Vector3d localPredictedForce = new Vector3d(Vector3d.Dot(predictedForce, vesselRight), Vector3d.Dot(predictedForce, vesselUp), Vector3d.Dot(predictedForce, vesselBackward));
					Vector3d localPredictedForceWithCache = new Vector3d(Vector3d.Dot(predictedForceWithCache, vesselRight), Vector3d.Dot(predictedForceWithCache, vesselUp), Vector3d.Dot(predictedForceWithCache, vesselBackward));

					Util.PostSingleScreenMessage("actual/predict comparison", "air vel=" + Math.Floor(airVelocity.magnitude) + " ; AoA=" + (AoA * 180.0 / Math.PI));
					//Util.PostSingleScreenMessage("total force", "actual total force=" + localTotalForce.ToString("0.000"));
					Util.PostSingleScreenMessage("actual force", "actual force=" + localActualForce.ToString("0.000"));
					Util.PostSingleScreenMessage("predicted force", "predicted force=" + localPredictedForce.ToString("0.000"));
					Util.PostSingleScreenMessage("predict with cache", "predicted force with cache=" + localPredictedForceWithCache.ToString("0.000"));

					Util.PostSingleScreenMessage("reference force", "reference force=" + referenceForce.ToString("0.000"));

					Util.PostSingleScreenMessage("current vel", "current vel=" + bodySpaceVelocity.ToString("0.00") + " (mag=" + bodySpaceVelocity.magnitude.ToString("0.00") + ")");
					//Util.PostSingleScreenMessage("vel from pos", "vel from pos=" + ((bodySpacePosition - PreviousFramePos) / dt).ToString("0.000") + " (mag=" + ((bodySpacePosition - PreviousFramePos) / dt).magnitude.ToString("0.00") + ")");
					Util.PostSingleScreenMessage("force diff", "force ratio=" + (localActualForce.z / localPredictedForce.z).ToString("0.000"));
					Util.PostSingleScreenMessage("drag", "physics drag=" + vessel_.rootPart.rb.drag);
					#endif

					double approximateRho = StockAeroUtil.GetDensity(altitudeAboveSea, body);
					double preciseRho = StockAeroUtil.GetDensity(vessel_.GetWorldPos3D(), body);
					double actualRho = vessel_.atmDensity;
					Util.PostSingleScreenMessage("rho info", /*"preciseRho=" + preciseRho.ToString("0.0000") + " ; " +*/ "rho=" + approximateRho.ToString("0.0000") + " ; actual=" + actualRho.ToString("0.0000") + " ; ratio=" + (actualRho / approximateRho).ToString("0.00"));
				}

				PreviousFrameVelocity = bodySpaceVelocity;
				PreviousFramePos = bodySpacePosition;
				PreviousFrameTime = now;
			}
		}
		#endif
    }
}
