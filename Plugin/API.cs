using System.Linq;
using UnityEngine;
namespace Trajectories
{
    //This class only returns correct values for the "active vessel."
    public static class API
    {
        public static Vector3? getImpactPosition()
        {
            updateTrajectory();
            foreach (var patch in Trajectory.fetch.patches)
            {
                if (patch.impactPosition != null)
                    return patch.impactPosition;
            }
            return null;
        }
        public static Vector3 plannedDirection()
        {
            updateTrajectory();
            return AutoPilot.fetch.PlannedDirection;
        }
        public static Vector3 correctedDirection()
        {
            updateTrajectory();
            return AutoPilot.fetch.CorrectedDirection;
        }
        public static void setTarget(double lat,double lon,double alt = 2.0)
        {
            var body = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);
            if (body != null)
            {
                Vector3d worldPos = body.GetWorldSurfacePosition(lat, lon, alt);
                Trajectory.fetch.SetTarget(body, worldPos - body.position);
            }
        }
        private static void updateTrajectory()
        {
            Trajectory.fetch.ComputeTrajectory(FlightGlobals.ActiveVessel, DescentProfile.fetch, true);
        }
    }
}
