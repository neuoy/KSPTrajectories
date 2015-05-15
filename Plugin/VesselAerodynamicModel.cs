/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

//#define PRECOMPUTE_CACHE
#define USE_CACHE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace Trajectories
{
    // this class abstracts the game aerodynamic computations to provide an unified interface wether the stock drag is used, or a supported mod is installed
    class VesselAerodynamicModel
    {
        private double mass_;
        public double mass { get { return mass_; } }

        private static string aerodynamicModelName_ = "unknown";
        public static string AerodynamicModelName { get { return aerodynamicModelName_; } }

        private Vessel vessel_;
        private CelestialBody body_;
        private double stockDragCoeff_;

        private Vector2[,,] cachedFARForces; // cached aerodynamic forces in a two dimensional array : indexed by velocity magnitude, atmosphere density and angle of attack
        private double maxFARVelocity;
        private double maxFARAngleOfAttack; // valid values are in range [-maxFARAngleOfAttack ; maxFARAngleOfAttack]
        private bool isValid;
        private bool farInitialized = false;
        private bool useNEAR = false;
        private bool useStockModel;
        #if PRECOMPUTE_CACHE
        private bool cachePrecomputed = false;
        #endif
        private double referenceDrag = 0;
        private DateTime nextAllowedAutomaticUpdate = DateTime.Now;

        private Type FARAPIType;
        private MethodInfo FARAPI_CalculateVesselAeroForces;
		private MethodInfo FARAeroUtil_GetCurrentDensity;

        public bool Verbose { get; set; }

        public VesselAerodynamicModel(Vessel vessel, CelestialBody body)
        {
            vessel_ = vessel;
            body_ = body;

            updateVesselInfo();

            initFARModel();
        }

        private void updateVesselInfo()
        {
            stockDragCoeff_ = 0.0;
            mass_ = 0.0;
            foreach (var part in vessel_.Parts)
            {
                if (part.physicalSignificance == Part.PhysicalSignificance.NONE)
                    continue;

                float partMass = part.mass + part.GetResourceMass();
                stockDragCoeff_ += part.maximum_drag * partMass;
                mass_ += partMass;
            }
            stockDragCoeff_ /= mass_;
        }

        public bool isValidFor(Vessel vessel, CelestialBody body)
        {
            if (vessel != vessel_ || body_ != body)
                return false;

            if (!useStockModel && Settings.fetch.AutoUpdateAerodynamicModel)
            {
                double newRefDrag = computeFARReferenceDrag();
                if (referenceDrag == 0)
                    referenceDrag = newRefDrag;
                double ratio = Math.Max(newRefDrag, referenceDrag) / Math.Max(1, Math.Min(newRefDrag, referenceDrag));
                if (ratio > 1.2 && DateTime.Now > nextAllowedAutomaticUpdate)
                {
                    nextAllowedAutomaticUpdate = DateTime.Now.AddSeconds(10); // limit updates frequency (could make the game almost unresponsive on some computers)
                    //ScreenMessages.PostScreenMessage("Trajectory aerodynamic model auto-updated");
                    isValid = false;
                }
            }

            return isValid;
        }

        public void IncrementalUpdate()
        {
            updateVesselInfo();
        }

        public void Invalidate()
        {
            isValid = false;
        }

        public double computeFARReferenceDrag()
        {
            Vector3 forces = computeForces_FAR(3000, new Vector3d(3000.0, 0, 0), new Vector3(0, 1, 0), 0, 0.25);
            return forces.sqrMagnitude;
        }

        private void initFARModel()
        {
            Debug.Log("Trajectories: Initializing aerodynamic model...");

            bool farInstalled = false;

            foreach (var loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                try
                {
                    switch (loadedAssembly.name)
                    {
                        case "NEAR":
                            useNEAR = true;
                            goto case "FerramAerospaceResearch";
                        case "FerramAerospaceResearch":
                            string namespaceName = useNEAR ? "NEAR" : "FerramAerospaceResearch";
                            FARAPIType = loadedAssembly.assembly.GetType(namespaceName + ".FARAPI");
                            if (useNEAR)
                            {
                                throw new Exception("NEAR support not implemented yet");
                            }
                            else
                            {
                                // public static void FerramAerospaceResearch.FARAPI.CalculateVesselAeroForces(Vessel vessel, out Vector3 aeroForce, out Vector3 aeroTorque, Vector3 velocityWorldVector, double altitude)
                                FARAPI_CalculateVesselAeroForces = FARAPIType.GetMethodEx("CalculateVesselAeroForces", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(Vessel), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(Vector3), typeof(double) });

                                // public static double FerramAerospaceResearch.FARAeroUtil.GetCurrentDensity(CelestialBody body, double altitude, bool densitySmoothingAtOcean = true)
								FARAeroUtil_GetCurrentDensity = loadedAssembly.assembly.GetType(namespaceName + ".FARAeroUtil").GetMethodEx("GetCurrentDensity", new Type[] { typeof(CelestialBody), typeof(double), typeof(bool) });
                            }
                            farInstalled = true;
                            break;
                    }
                }
                catch(Exception e)
                {
                    Debug.Log("Trajectories: failed to interface with assembly " + loadedAssembly.name);
                    Debug.Log("Using stock model instead");
                    Debug.Log(e.ToString());
                }
            }

            if (!farInstalled)
            {
                //ScreenMessages.PostScreenMessage("Ferram Aerospace Research (FAR or NEAR) not installed, or incompatible version, using stock aerodynamics");
                //ScreenMessages.PostScreenMessage("WARNING: stock aerodynamic model does not predict lift, spacecrafts with wings will have inaccurate predictions");
                aerodynamicModelName_ = "stock";
                useStockModel = true;
                isValid = true;
                return;
            }
            else
            {
                if(useNEAR)
                    aerodynamicModelName_ = "NEAR";
                else
                    aerodynamicModelName_ = "FAR";
            }

            maxFARVelocity = 10000.0;
            maxFARAngleOfAttack = 180.0 / 180.0 * Math.PI;

            int velocityResolution = 32;
            int angleOfAttackResolution = 32;
            int altitudeResolution = 32;

            cachedFARForces = new Vector2[velocityResolution, angleOfAttackResolution, altitudeResolution];

            for (int v = 0; v < velocityResolution; ++v)
            {
                for (int a = 0; a < angleOfAttackResolution; ++a)
                {
                    for (int m = 0; m < altitudeResolution; ++m)
                    {
                        cachedFARForces[v, a, m] = new Vector2(float.NaN, float.NaN);
                    }
                }
            }

            isValid = true;

            precomputeCache();
        }

        private void precomputeCache()
        {
            #if PRECOMPUTE_CACHE
            if (!cachePrecomputed && isFARInitialized())
            {
                for (int v = 0; v < cachedFARForces.GetLength(0); ++v)
                {
                    for (int a = 0; a < cachedFARForces.GetLength(1); ++a)
                    {
                        for(int m = 0; m < cachedFARForces.GetLength(2); ++m)
                        {
                            computeCacheEntry(v, a, m);
                        }
                    }
                }
                cachePrecomputed = true;
            }
#endif
        }

        private Vector2 computeCacheEntry(int v, int a, int m)
        {
            if (!isFARInitialized())
                throw new Exception("Internal error");

            double vel = maxFARVelocity * (double)v / (double)(cachedFARForces.GetLength(0) - 1);
            double v2 = Math.Max(1.0, vel * vel);

            Vector3d velocity = new Vector3d(vel, 0, 0);
            
            double maxAltitude = body_.atmosphereDepth;
            double currentAltitude = maxAltitude * (double)m / (double)(cachedFARForces.GetLength(2) - 1);
            double pressure = FlightGlobals.getStaticPressure(currentAltitude, body_);
            double temperature = FlightGlobals.getExternalTemperature(currentAltitude, body_);
            double stockRho = FlightGlobals.getAtmDensity(pressure, temperature);
            double rho = useNEAR ? stockRho : (double)FARAeroUtil_GetCurrentDensity.Invoke(null, new object[] { body_, currentAltitude, false });
            if (rho < 0.0000000001)
                return new Vector2(0, 0);
            double invScale = 1.0 / (rho * v2); // divide by v² and rho before storing the force, to increase accuracy (the reverse operation is performed when reading from the cache)

            double AoA = maxFARAngleOfAttack * ((double)a / (double)(cachedFARForces.GetLength(1) - 1) * 2.0 - 1.0);
            Vector3d force = computeForces_FAR(currentAltitude, velocity, new Vector3(0, 1, 0), AoA, 0.25) * invScale;
            return cachedFARForces[v, a, m] = new Vector2((float)force.x, (float)force.y);
        }

        private bool isFARInitialized()
        {
            if (!farInitialized)
                farInitialized = (computeFARReferenceDrag() >= 1);

            return farInitialized;
        }

        private Vector2 getCachedFARForce(int v, int a, int m)
        {
            if (!isFARInitialized())
                return new Vector2(0, 0);

            #if PRECOMPUTE_CACHE
            return cachedFARForces[v,a,m];
            #else
            Vector2 f = cachedFARForces[v, a, m];

            if(float.IsNaN(f.x))
            {
                f = computeCacheEntry(v,a,m); 
            }

            return f;
            #endif
        }

        private Vector2 sample2d(int vFloor, float vFrac, int aFloor, float aFrac, int mFloor)
        {
            Vector2 f00 = getCachedFARForce(vFloor, aFloor, mFloor);
            Vector2 f10 = getCachedFARForce(vFloor + 1, aFloor, mFloor);

            Vector2 f01 = getCachedFARForce(vFloor, aFloor + 1, mFloor);
            Vector2 f11 = getCachedFARForce(vFloor + 1, aFloor + 1, mFloor);

            Vector2 f0 = f01 * aFrac + f00 * (1.0f - aFrac);
            Vector2 f1 = f11 * aFrac + f10 * (1.0f - aFrac);

            return f1 * vFrac + f0 * (1.0f - vFrac);
        }

        private Vector2 sample3d(int vFloor, float vFrac, int aFloor, float aFrac, int mFloor, float mFrac)
        {
            Vector2 f0 = sample2d(vFloor, vFrac, aFloor, aFrac, mFloor);
            Vector2 f1 = sample2d(vFloor, vFrac, aFloor, aFrac, mFloor + 1);

            return f1 * mFrac + f0 * (1.0f - mFrac);
        }

        private Vector2 getFARForce(double velocity, double altitudeAboveSea, double angleOfAttack)
        {
            precomputeCache();

            //Util.PostSingleScreenMessage("getFARForce velocity", "velocity = " + velocity);
            float vFrac = (float)(velocity / maxFARVelocity * (double)(cachedFARForces.GetLength(0)-1));
            int vFloor = Math.Min(cachedFARForces.GetLength(0)-2, (int)vFrac);
            vFrac = Math.Min(1.0f, vFrac - (float)vFloor);

            float aFrac = (float)((angleOfAttack / maxFARAngleOfAttack * 0.5 + 0.5) * (double)(cachedFARForces.GetLength(1) - 1));
            int aFloor = Math.Max(0, Math.Min(cachedFARForces.GetLength(1) - 2, (int)aFrac));
            aFrac = Math.Max(0.0f, Math.Min(1.0f, aFrac - (float)aFloor));

            double maxAltitude = body_.atmosphereDepth;
            float mFrac = (float)(altitudeAboveSea / maxAltitude * (double)(cachedFARForces.GetLength(2) - 1));
            int mFloor = Math.Max(0, Math.Min(cachedFARForces.GetLength(2) - 2, (int)mFrac));
            mFrac = Math.Max(0.0f, Math.Min(1.0f, mFrac - (float)mFloor));

            if(Verbose)
            {
                Util.PostSingleScreenMessage("cache cell", "cache cell: ["+vFloor+", "+aFloor+", "+mFloor+"]");
                Util.PostSingleScreenMessage("altitude cell", "altitude cell: " + altitudeAboveSea + " / " + maxAltitude + " * " + (double)(cachedFARForces.GetLength(2) - 1));
            }

            Vector2 res = sample3d(vFloor, vFrac, aFloor, aFrac, mFloor, mFrac);
            
            double pressure = FlightGlobals.getStaticPressure(altitudeAboveSea, body_);
            double temperature = FlightGlobals.getExternalTemperature(altitudeAboveSea, body_);
            double stockRho = FlightGlobals.getAtmDensity(pressure, temperature);
            double rho = useNEAR ? stockRho : (double)FARAeroUtil_GetCurrentDensity.Invoke(null, new object[] { body_, altitudeAboveSea, false });

            res = res * (float)(velocity * velocity * rho);

            return res;
        }

        // returns the total aerodynamic forces that would be applied on the vessel if it was at bodySpacePosition with bodySpaceVelocity relatively to the specified celestial body
        // dt is the time delta during which the force will be applied, so if the model supports it, it can compute an average force (to be more accurate than a simple instantaneous force)
        public Vector3d computeForces(CelestialBody body, Vector3d bodySpacePosition, Vector3d airVelocity, double angleOfAttack, double dt)
        {
            if(useStockModel)
                return computeForces_StockDrag(body, bodySpacePosition, airVelocity, dt); // TODO: compute stock lift
            else
                return computeForces_FAR(body, bodySpacePosition, airVelocity, angleOfAttack, dt);
        }

        private Vector3d computeForces_StockDrag(CelestialBody body, Vector3d bodySpacePosition, Vector3d airVelocity, double dt)
        {
            double altitudeAboveSea = bodySpacePosition.magnitude - body.Radius;
            double pressure = FlightGlobals.getStaticPressure(altitudeAboveSea, body);
            double temperature = FlightGlobals.getExternalTemperature(altitudeAboveSea, body);
            if (pressure <= 0)
                return Vector3d.zero;

            double rho = FlightGlobals.getAtmDensity(pressure, temperature);

            double velocityMag = airVelocity.magnitude;

            double crossSectionalArea = PhysicsGlobals.DragMultiplier * mass_;
            return airVelocity * (-0.5 * rho * velocityMag * stockDragCoeff_ * crossSectionalArea);
        }

        public Vector3d computeForces_FAR(double altitude, Vector3d airVelocity, Vector3d vup, double angleOfAttack, double dt)
        {
            Transform vesselTransform = vessel_.ReferenceTransform;

            // this is weird, but the vessel orientation does not match the reference transform (up is forward), this code fixes it but I don't know if it'll work in all cases
            Vector3d vesselBackward = (Vector3d)(-vesselTransform.up.normalized);
            Vector3d vesselForward = -vesselBackward;
            Vector3d vesselUp = (Vector3d)(-vesselTransform.forward.normalized);
            Vector3d vesselRight = Vector3d.Cross(vesselUp, vesselBackward).normalized;

            Vector3d airVelocityForFixedAoA = (vesselForward * Math.Cos(-angleOfAttack) + vesselUp * Math.Sin(-angleOfAttack)) * airVelocity.magnitude;

            Debug.Log("Trajectories: getting FAR forces");
            Vector3 worldAirVel = new Vector3((float)airVelocityForFixedAoA.x, (float)airVelocityForFixedAoA.y, (float)airVelocityForFixedAoA.z);
            var parameters = new object[] { vessel_, new Vector3(), new Vector3(), worldAirVel, altitude };
            FARAPI_CalculateVesselAeroForces.Invoke(null, parameters);
            Vector3d totalForce = (Vector3)parameters[1];
            Debug.Log("Trajectories: got FAR forces");

            if (Double.IsNaN(totalForce.x) || Double.IsNaN(totalForce.y) || Double.IsNaN(totalForce.z))
            {
                Debug.Log("Trajectories: WARNING: FAR/NEAR totalForce is NAN (altitude=" + altitude + ", airVelocity=" + airVelocity.magnitude + ", angleOfAttack=" + angleOfAttack);
                return new Vector3d(0, 0, 0); // Don't send NaN into the simulation as it would cause bad things (infinite loops, crash, etc.). I think this case only happens at the atmosphere edge, so the total force should be 0 anyway.
            }

            // convert the force computed by FAR (depends on the current vessel orientation, which is irrelevant for the prediction) to the predicted vessel orientation (which depends on the predicted velocity)
            Vector3d localForce = new Vector3d(Vector3d.Dot(vesselRight, totalForce), Vector3d.Dot(vesselUp, totalForce), Vector3d.Dot(vesselBackward, totalForce));

            //if (Double.IsNaN(localForce.x) || Double.IsNaN(localForce.y) || Double.IsNaN(localForce.z))
            //    throw new Exception("localForce is NAN");

            Vector3d velForward = airVelocity.normalized;
            Vector3d velBackward = -velForward;
            Vector3d velRight = Vector3d.Cross(vup, velBackward);
            if (velRight.sqrMagnitude < 0.001)
            {
                velRight = Vector3d.Cross(vesselUp, velBackward);
                if (velRight.sqrMagnitude < 0.001)
                {
                    velRight = Vector3d.Cross(vesselBackward, velBackward).normalized;
                }
                else
                {
                    velRight = velRight.normalized;
                }
            }
            else
                velRight = velRight.normalized;
            Vector3d velUp = Vector3d.Cross(velBackward, velRight).normalized;

            Vector3d predictedVesselForward = velForward * Math.Cos(angleOfAttack) + velUp * Math.Sin(angleOfAttack);
            Vector3d predictedVesselBackward = -predictedVesselForward;
            Vector3d predictedVesselRight = velRight;
            Vector3d predictedVesselUp = Vector3d.Cross(predictedVesselBackward, predictedVesselRight).normalized;

            Vector3d res = predictedVesselRight * localForce.x + predictedVesselUp * localForce.y + predictedVesselBackward * localForce.z;
            if (Double.IsNaN(res.x) || Double.IsNaN(res.y) || Double.IsNaN(res.z))
            {
                Debug.Log("Trajectories: res is NaN (altitude=" + altitude + ", airVelocity=" + airVelocity.magnitude + ", angleOfAttack=" + angleOfAttack);
                return new Vector3d(0, 0, 0); // Don't send NaN into the simulation as it would cause bad things (infinite loops, crash, etc.). I think this case only happens at the atmosphere edge, so the total force should be 0 anyway.
            }
            return res;
        }

        private Vector3d computeForces_FAR(CelestialBody body, Vector3d bodySpacePosition, Vector3d airVelocity, double angleOfAttack, double dt)
        {
            double altitudeAboveSea = bodySpacePosition.magnitude - body.Radius;

            double pressure = FlightGlobals.getStaticPressure(altitudeAboveSea, body);
            if (pressure <= 0) {
                return Vector3d.zero;
            }
                
            double temperature = FlightGlobals.getExternalTemperature(altitudeAboveSea, body);
            
            double stockRho = FlightGlobals.getAtmDensity(pressure, temperature);


            double rho = useNEAR ? stockRho : (double)FARAeroUtil_GetCurrentDensity.Invoke(null, new object[] { body, altitudeAboveSea, false });

#if !USE_CACHE
            return computeForces_FAR(altitudeAboveSea, airVelocity, bodySpacePosition, angleOfAttack, dt);
#else
            //double approxMachNumber = useNEAR ? 0.0 : (double)FARAeroUtil_GetMachNumber.Invoke(null, new object[] { body_, body.maxAtmosphereAltitude * 0.5, new Vector3d((float)airVelocity.magnitude, 0, 0) });
            //Util.PostSingleScreenMessage("machNum", "machNumber = " + actualMachNumber + " ; approx machNumber = " + approxMachNumber);

            Vector2 force = getFARForce(airVelocity.magnitude, altitudeAboveSea, angleOfAttack);

            Vector3d forward = airVelocity.normalized;
            Vector3d right = Vector3d.Cross(forward, bodySpacePosition).normalized;
            Vector3d up = Vector3d.Cross(right, forward).normalized;

            return forward * force.x + up * force.y;
#endif
        }
    }
}
