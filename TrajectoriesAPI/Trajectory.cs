using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TrajectoriesAPI
{
    public class Trajectory
    {
        public struct Point
        {
            public Vector3 pos;
            public Vector3 aerodynamicForce;
            public Vector3 orbitalVelocity;
            public Vector3 airVelocity;
        }

        object trajectory;

        internal Trajectory()
        {
            trajectory = Activator.CreateInstance(TrajectoriesAPI.TrajectoryType);
        }

        internal Trajectory(object traj)
        {
            trajectory = traj;
        }

        /// <summary>
        /// Computes the trajectory of the specified vessel, assuming it will maintain the specified angle of attack at all times.
        /// </summary>
        public void ComputeTrajectory(Vessel vessel, float AoA = 0)
        {
            TrajectoriesAPI.Trajectory_computeTrajectory.Invoke(trajectory, new object[] { vessel, AoA });
        }

        /// <summary>
        /// Gets the impact position of the vessel associated to this Trajectory, relatively to the Vessel main CelestialBody (in the inertial reference frame of the body), or null if the Vessel is not going to collide with the body.
        /// </summary>
        public Vector3? GetImpactPosition()
        {
            IList patches = (IList)TrajectoriesAPI.Trajectory_patches.GetValue(trajectory, null);
            Debug.Log(patches.Count.ToString() + " patches");
            foreach (object patch in patches)
            {
                object startingState = TrajectoriesAPI.Patch_startingState.GetValue(patch, null);
                CelestialBody body = (CelestialBody)TrajectoriesAPI.VesselState_referenceBody.GetValue(startingState, null);
                if (body != FlightGlobals.ActiveVessel.mainBody)
                    return null;

                Vector3? impact = (Vector3?)TrajectoriesAPI.Patch_impactPosition.GetValue(patch, null);
                if (impact.HasValue)
                    return impact;
            }

            return null;
        }

        /// <summary>
        /// Gets the predicted aerodynamic force (in world coordinates) when the vessel will be at the specified altitude arround the currently orbited body.
        /// </summary>
        public Point? GetInfo(float altitudeAboveSeaLevel)
        {
            IList patches = (IList)TrajectoriesAPI.Trajectory_patches.GetValue(trajectory, null);
            Debug.Log(patches.Count.ToString() + " patches");
            foreach (object patch in patches)
            {
                object startingState = TrajectoriesAPI.Patch_startingState.GetValue(patch, null);
                CelestialBody body = (CelestialBody)TrajectoriesAPI.VesselState_referenceBody.GetValue(startingState, null);
                if (body != FlightGlobals.ActiveVessel.mainBody)
                    return null;

                if ((bool)TrajectoriesAPI.Patch_isAtmospheric.GetValue(patch, null))
                {
                    object p = TrajectoriesAPI.Patch_GetInfo.Invoke(patch, new object[] { altitudeAboveSeaLevel });
                    return new Point
                    {
                        pos = (Vector3)TrajectoriesAPI.Point_pos.GetValue(p),
                        aerodynamicForce = (Vector3)TrajectoriesAPI.Point_aerodynamicForce.GetValue(p),
                        orbitalVelocity = (Vector3)TrajectoriesAPI.Point_orbitalVelocity.GetValue(p),
                        airVelocity = (Vector3)TrajectoriesAPI.Point_airVelocity.GetValue(p)
                    };
                }
            }

            return null;
        }
    }
}
