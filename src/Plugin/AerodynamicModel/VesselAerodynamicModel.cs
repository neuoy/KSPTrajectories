/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
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

//#define PRECOMPUTE_CACHE

using System;
using UnityEngine;

namespace Trajectories
{
    ///<summary> Abstracts the game aerodynamic computations to provide an unified interface whether the stock drag is used, or a supported mod is installed </summary>
    internal abstract class VesselAerodynamicModel
    {
        private double reference_drag = 0d;
        private double next_update_delay = Util.Clocks;

        protected AeroForceCache cachedForces;

        internal abstract string AerodynamicModelName { get; }
        internal static bool DebugParts { get; set; }

        // constructor
        protected VesselAerodynamicModel() { }

        internal void Init() => InitCache();

        private void InitCache()
        {
            //Util.DebugLog("");

            double maxCacheVelocity = 10000d;
            double maxCacheAoA = Math.PI;     //  180.0 / 180.0 * Math.PI

            int velocityResolution = 32;
            int angleOfAttackResolution = 33; // even number to include exactly 0°
            int altitudeResolution = 32;

            cachedForces = new AeroForceCache(maxCacheVelocity, maxCacheAoA, GameDataCache.BodyAtmosphereDepth, velocityResolution, angleOfAttackResolution, altitudeResolution, this);
        }

        internal void Update()
        {
            // limit update frequency to 5 seconds (could make the game almost unresponsive on some computers)
            if (Util.ElapsedSeconds(next_update_delay) < 5d)
                return;

            next_update_delay = Util.Clocks;

            Vector3d forces = ComputeForces(3000d, new Vector3d(3000d, 0d, 0d), new Vector3d(0d, 1d, 0d), 0d);
            double newRefDrag = forces.sqrMagnitude;
            if (reference_drag == 0d)
            {
                reference_drag = newRefDrag;
                return;
            }

            if ((Math.Max(newRefDrag, reference_drag) / Math.Max(1d, Math.Min(newRefDrag, reference_drag))) > 1.2d)
            {
#if DEBUG
                ScreenMessages.PostScreenMessage("Trajectories aerodynamic model updated due to ref drag ratio > 1.2");
#endif
                Init();
            }
        }

        /// <summary>
        /// Returns the total aerodynamic forces that would be applied on the vessel if it was at bodySpacePosition with bodySpaceVelocity relatively
        /// to the GameDataCache celestial body
        /// This method makes use of the cache if available, otherwise it will call ComputeForces.
        /// </summary>
        internal Vector3d GetForces(Vector3d bodySpacePosition, Vector3d airVelocity, double angleOfAttack)
        {
            double altitudeAboveSea = bodySpacePosition.magnitude - GameDataCache.BodyRadius;
            if (altitudeAboveSea > GameDataCache.BodyAtmosphereDepth)
            {
                return Vector3d.zero;
            }

            if (!Settings.UseCache)
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
        internal Vector3d ComputeForces(double altitude, Vector3d airVelocity, Vector3d vup, double angleOfAttack)
        {
            Profiler.Start("ComputeForces");

            if (!GameDataCache.BodyHasAtmosphere || altitude >= GameDataCache.BodyAtmosphereDepth)
                return Vector3d.zero;

            // this is weird, but the vessel orientation does not match the reference transform (up is forward), this code fixes it but I don't know if it'll work in all cases
            Vector3d vesselBackward = -GameDataCache.VesselTransformUp.normalized;
            Vector3d vesselForward = -vesselBackward;
            Vector3d vesselUp = -GameDataCache.VesselTransformForward.normalized;
            Vector3d vesselRight = Vector3d.Cross(vesselUp, vesselBackward).normalized;

            Vector3d airVelocityForFixedAoA = (vesselForward * Math.Cos(-angleOfAttack) + vesselUp * Math.Sin(-angleOfAttack)) * airVelocity.magnitude;

            Vector3d totalForce = ComputeForces_Model(airVelocityForFixedAoA, altitude);

            if (totalForce.IsNaN())
            {
                Util.LogError("{0} totalForce {1} is NaN : (altitude={2}, airVelocity={3}, angleOfAttack={4})",
                    AerodynamicModelName, totalForce, altitude, airVelocity.magnitude, angleOfAttack);
                // Don't send NaN into the simulation as it would cause bad things (infinite loops, crash, etc.).
                // I think this case only happens at the atmosphere edge, so the total force should be 0 anyway.
                return Vector3d.zero;
            }

            // convert the force computed by the model (depends on the current vessel orientation, which is irrelevant for the prediction) to the predicted vessel orientation (which depends on the predicted velocity)
            Vector3d localForce = new Vector3d(Vector3d.Dot(vesselRight, totalForce), Vector3d.Dot(vesselUp, totalForce), Vector3d.Dot(vesselBackward, totalForce));

            if (localForce.IsNaN())
            {
                Util.LogError("{0} localForce {1} is NaN : (altitude={2}, airVelocity={3}, angleOfAttack={4})",
                    AerodynamicModelName, localForce, altitude, airVelocity.magnitude, angleOfAttack);
                // Don't send NaN into the simulation as it would cause bad things (infinite loops, crash, etc.).
                //I think this case only happens at the atmosphere edge, so the total force should be 0 anyway.
                return Vector3d.zero;
            }

            Vector3d velForward = airVelocity.normalized;
            Vector3d velBackward = -velForward;
            Vector3d velRight = Vector3d.Cross(vup, velBackward);
            if (velRight.sqrMagnitude < 0.001d)
            {
                velRight = Vector3d.Cross(vesselUp, velBackward);
                if (velRight.sqrMagnitude < 0.001d)
                {
                    velRight = Vector3d.Cross(vesselBackward, velBackward).normalized;
                }
                else
                {
                    velRight = velRight.normalized;
                }
            }
            else
            {
                velRight = velRight.normalized;
            }

            Vector3d velUp = Vector3d.Cross(velBackward, velRight).normalized;

            Vector3d predictedVesselForward = velForward * Math.Cos(angleOfAttack) + velUp * Math.Sin(angleOfAttack);
            Vector3d predictedVesselBackward = -predictedVesselForward;
            Vector3d predictedVesselRight = velRight;
            Vector3d predictedVesselUp = Vector3d.Cross(predictedVesselBackward, predictedVesselRight).normalized;

            Vector3d res = predictedVesselRight * localForce.x + predictedVesselUp * localForce.y + predictedVesselBackward * localForce.z;
            if (res.IsNaN())
            {
                Util.LogError("{0} res {1} is NaN : (altitude={2}, airVelocity={3}, angleOfAttack={4})",
                    AerodynamicModelName, res, altitude, airVelocity.magnitude, angleOfAttack);
                // Don't send NaN into the simulation as it would cause bad things (infinite loops, crash, etc.).
                //I think this case only happens at the atmosphere edge, so the total force should be 0 anyway.
                return Vector3d.zero;
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
        internal virtual Vector2d PackForces(Vector3d forces, double altitudeAboveSea, double velocity) => new Vector2d(forces.x, forces.y);

        /// <summary>
        /// See PackForces
        /// </summary>
        internal virtual Vector3d UnpackForces(Vector2d packedForces, double altitudeAboveSea, double velocity) => new Vector3d(packedForces.x, packedForces.y, 0.0);
    }
}
