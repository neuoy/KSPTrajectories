using UnityEngine;
namespace Trajectories
{
    public static class API
    {

        public static bool APIAvailable()
        {
            return true;
        }
        public static Vector3? getImpactPosition()
        {
            Trajectory trajectory = Trajectory.fetch;
            trajectory.ComputeTrajectory(FlightGlobals.ActiveVessel, DescentProfile.fetch, true);
            foreach (var patch in trajectory.patches)
            {
                if (patch.impactPosition != null)
                {
                    return patch.impactPosition;
                }
            }
            return null;
        }
    }
}

