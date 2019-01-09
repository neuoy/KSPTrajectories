/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).

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

//#define PRECOMPUTE_CACHE

using System;
using UnityEngine;

namespace Trajectories
{
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

        public static bool DebugParts { get; set; }

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
            // // this kills performance on vessel load, so we don't do that anymore
            // mass_ = vessel_.totalMass;

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

            int velocityResolution = 32;
            int angleOfAttackResolution = 33; // even number to include exactly 0°
            int altitudeResolution = 32;

            cachedForces = new AeroForceCache(maxCacheVelocity, maxCacheAoA, body_.atmosphereDepth, velocityResolution, angleOfAttackResolution, altitudeResolution, this);

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

        /// <summary>
        /// Returns the total aerodynamic forces that would be applied on the vessel if it was at bodySpacePosition with bodySpaceVelocity relatively to the specified celestial body
        /// This method makes use of the cache if available, otherwise it will call ComputeForces.
        /// </summary>
        public Vector3d GetForces(CelestialBody body, Vector3d bodySpacePosition, Vector3d airVelocity, double angleOfAttack)
        {
            if (body != vessel_.mainBody)
                throw new Exception("Can't predict aerodynamic forces on another body in current implementation");

            double altitudeAboveSea = bodySpacePosition.magnitude - body.Radius;
            if (altitudeAboveSea > body.atmosphereDepth)
            {
                return Vector3d.zero;
            }

            if (!UseCache)
                return ComputeForces(altitudeAboveSea, airVelocity, bodySpacePosition, angleOfAttack);

            Vector3d force = cachedForces.GetForce(airVelocity.magnitude, angleOfAttack, altitudeAboveSea);

            // adjust force using the more accurate air density that we can compute knowing where the vessel is relatively to the sun and body
            //Vector3d position = body.position + bodySpacePosition;
            //double preciseRho = StockAeroUtil.GetDensity(position, body);
            //double approximateRho = StockAeroUtil.GetDensity(altitude, body);
            //if (approximateRho > 0)
            //    force = force * (float)(preciseRho / approximateRho);

            Vector3d forward = airVelocity.normalized;
            Vector3d right = Vector3d.Cross(forward, bodySpacePosition).normalized;
            Vector3d up = Vector3d.Cross(right, forward).normalized;

            return forward * force.x + up * force.y;
        }

        /// <summary>
        /// Compute the aerodynamic forces that would be applied to the vessel if it was in the specified situation (air velocity, altitude and angle of attack).
        /// </summary>
        /// <returns>The computed aerodynamic forces in world space</returns>
        public Vector3d ComputeForces(double altitude, Vector3d airVelocity, Vector3d vup, double angleOfAttack)
        {
            Profiler.Start("ComputeForces");
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

            Vector3d totalForce = ComputeForces_Model(airVelocityForFixedAoA, altitude);

            if (Double.IsNaN(totalForce.x) || Double.IsNaN(totalForce.y) || Double.IsNaN(totalForce.z))
            {
                Debug.Log(string.Format("Trajectories: WARNING: {0} totalForce is NAN (altitude={1}, airVelocity={2}, angleOfAttack={3}", AerodynamicModelName, altitude, airVelocity.magnitude, angleOfAttack));
                return Vector3d.zero; // Don't send NaN into the simulation as it would cause bad things (infinite loops, crash, etc.). I think this case only happens at the atmosphere edge, so the total force should be 0 anyway.
            }

            // convert the force computed by the model (depends on the current vessel orientation, which is irrelevant for the prediction) to the predicted vessel orientation (which depends on the predicted velocity)
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


            Profiler.Stop("ComputeForces");
            return res;
        }

        /// <summary>
        /// Computes the aerodynamic forces that would be applied to the vessel if it was in the specified situation (air velocity and altitude). The vessel is assumed to be in its current orientation (the air velocity is already adjusted as needed).
        /// </summary>
        /// <returns>The computed aerodynamic forces in world space</returns>
        protected abstract Vector3d ComputeForces_Model(Vector3d airVelocity, double altitude);

        /// <summary>
        /// Aerodynamic forces are roughly proportional to rho and squared air velocity, so we divide by these values to get something that can be linearly interpolated (the reverse operation is then applied after interpolation)
        /// This operation is optional but should slightly increase the cache accuracy
        /// </summary>
        public virtual Vector2 PackForces(Vector3d forces, double altitudeAboveSea, double velocity)
        {
            return new Vector2((float)forces.x, (float)forces.y);
        }

        /// <summary>
        /// See PackForces
        /// </summary>
        public virtual Vector3d UnpackForces(Vector2 packedForces, double altitudeAboveSea, double velocity)
        {
            return new Vector3d((double)packedForces.x, (double)packedForces.y, 0.0);
        }
    }
}
