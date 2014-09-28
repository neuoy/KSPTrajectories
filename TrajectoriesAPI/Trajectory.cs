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
        private Vessel Vessel;

        public Trajectory(Vessel vessel)
        {
            Vessel = vessel;
        }

        /// <summary>
        /// Gets the impact position of the vessel associated to this Trajectory, relatively to the Vessel main CelestialBody (in the inertial reference frame of the body), or null if the Vessel is not going to collide with the body.
        /// </summary>
        public Vector3? GetRawImpactPosition()
        {
            object trajectory = TrajectoriesAPI.Trajectory_fetch.GetValue(null, null);

            Debug.Log("Computing trajectory from API");
            TrajectoriesAPI.Trajectory_computeTrajectory.Invoke(trajectory, new object[] { Vessel });

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
    }
}
