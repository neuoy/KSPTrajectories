/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

//#define PRECOMPUTE_CACHE

using System;
using UnityEngine;

namespace Trajectories
{
    // Delegate for authorizing AeroForceCache to call ComputeForce
    public delegate Vector3d ComputeForceDeleg(double altitude, Vector3d airVelocity, Vector3d vup, double angleOfAttack);

    // this class abstracts the game aerodynamic computations to provide an unified interface wether the stock drag is used, or a supported mod is installed
    public abstract class VesselAerodynamicModel
    {
        private double mass_;
        public double mass { get { return mass_; } }

        public abstract string AerodynamicModelName { get; }

        protected Vessel vessel_;
        protected CelestialBody body_;
        private bool isValid;
        private double referenceDrag = 0;
        private int referencePartCount = 0;
        private DateTime nextAllowedAutomaticUpdate = DateTime.Now;

        public bool UseCache { get { return Settings.fetch.UseCache; } }
        protected AeroForceCache cachedForces;

        public static bool Verbose { get; set; }

        public VesselAerodynamicModel(Vessel vessel, CelestialBody body)
        {
            vessel_ = vessel;
            body_ = body;

            referencePartCount = vessel.Parts.Count;

            updateVesselInfo();

            InitCache();
        }

        private void updateVesselInfo()
        {
            mass_ = 0.0;
            foreach (var part in vessel_.Parts)
            {
                if (part.physicalSignificance == Part.PhysicalSignificance.NONE)
                    continue;

                float partMass = part.mass + part.GetResourceMass() + part.GetPhysicslessChildMass();
                mass_ += partMass;
            }
        }

        private void InitCache()
        {
            Debug.Log("Trajectories: Initializing cache");

            double maxCacheVelocity = 10000.0;
            double maxCacheAoA = 180.0 / 180.0 * Math.PI;

            int velocityResolution = 128;
            int angleOfAttackResolution = 129; // even number to include exactly 0°
            int altitudeResolution = 128;

            cachedForces = new AeroForceCache(maxCacheVelocity, maxCacheAoA, body_.atmosphereDepth, velocityResolution, angleOfAttackResolution, altitudeResolution, ComputeForces);

            isValid = true;

            return;
        }

        public bool isValidFor(Vessel vessel, CelestialBody body)
        {
            if (vessel != vessel_ || body_ != body)
                return false;

            if (Settings.fetch.AutoUpdateAerodynamicModel)
            {
                double newRefDrag = ComputeReferenceDrag();
                if (referenceDrag == 0)
                {
                    referenceDrag = newRefDrag;
                }
                double ratio = Math.Max(newRefDrag, referenceDrag) / Math.Max(1, Math.Min(newRefDrag, referenceDrag));
                if (ratio > 1.2 && DateTime.Now > nextAllowedAutomaticUpdate || referencePartCount != vessel.Parts.Count)
                {
                    nextAllowedAutomaticUpdate = DateTime.Now.AddSeconds(10); // limit updates frequency (could make the game almost unresponsive on some computers)
#if DEBUG
                    ScreenMessages.PostScreenMessage("Trajectory aerodynamic model auto-updated");
#endif
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

        private double ComputeReferenceDrag()
        {
            Vector3 forces = ComputeForces(3000, new Vector3d(3000.0, 0, 0), new Vector3(0, 1, 0), 0);
            return forces.sqrMagnitude;
        }

        // returns the total aerodynamic forces that would be applied on the vessel if it was at bodySpacePosition with bodySpaceVelocity relatively to the specified celestial body
        // dt is the time delta during which the force will be applied, so if the model supports it, it can compute an average force (to be more accurate than a simple instantaneous force)
        public Vector3d getForces(CelestialBody body, Vector3d bodySpacePosition, Vector3d airVelocity, double angleOfAttack)
        {
            if (body != vessel_.mainBody)
                throw new Exception("Can't predict aerodynamic forces on another body in current implementation");

            return GetForces_Model(body, bodySpacePosition, airVelocity, angleOfAttack);
        }

        protected Vector3d ComputeForces(double altitude, Vector3d airVelocity, Vector3d vup, double angleOfAttack)
        {
            if (!vessel_.mainBody.atmosphere)
                return Vector3d.zero;
            if (altitude >= body_.atmosphereDepth)
                return Vector3d.zero;

            Transform vesselTransform = vessel_.ReferenceTransform;

            // this is weird, but the vessel orientation does not match the reference transform (up is forward), this code fixes it but I don't know if it'll work in all cases
            Vector3d vesselBackward = (Vector3d)(-vesselTransform.up.normalized);
            Vector3d vesselForward = -vesselBackward;
            Vector3d vesselUp = (Vector3d)(-vesselTransform.forward.normalized);
            Vector3d vesselRight = Vector3d.Cross(vesselUp, vesselBackward).normalized;

            Vector3d airVelocityForFixedAoA = (vesselForward * Math.Cos(-angleOfAttack) + vesselUp * Math.Sin(-angleOfAttack)) * airVelocity.magnitude;

            Vector3d totalForce = ComputeForces_Model(airVelocityForFixedAoA, altitude, airVelocity.magnitude);

            if (Double.IsNaN(totalForce.x) || Double.IsNaN(totalForce.y) || Double.IsNaN(totalForce.z))
            {
                Debug.Log(string.Format("Trajectories: WARNING: {0} totalForce is NAN (altitude={1}, airVelocity={2}, angleOfAttack={3}", AerodynamicModelName, altitude, airVelocity.magnitude, angleOfAttack));
                return Vector3d.zero; // Don't send NaN into the simulation as it would cause bad things (infinite loops, crash, etc.). I think this case only happens at the atmosphere edge, so the total force should be 0 anyway.
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

        protected abstract Vector3d GetForces_Model(CelestialBody body, Vector3d bodySpacePosition, Vector3d airVelocity, double angleOfAttack);
        protected abstract Vector3d ComputeForces_Model(Vector3d airVelocity, double altitude, double absoluteVelocity);
    }
}
