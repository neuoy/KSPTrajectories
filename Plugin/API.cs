using System.Linq;
using UnityEngine;
namespace Trajectories
{
    //This class only returns correct values for the "active vessel."
    public static class API
    {
        public static bool alwaysUpdate
        {
            get
            {
                return Settings.fetch.AlwaysUpdate;
            }
            set
            {
                Settings.fetch.AlwaysUpdate = value;
            }
        }

        public static Vector3? getImpactPosition()
        {
            foreach (var patch in Trajectory.fetch.patches)
            {
                if (patch.impactPosition != null)
                    return patch.impactPosition;
            }
            return null;
        }

        public static Vector3? getImpactVelocity()
        {
            foreach (var patch in Trajectory.fetch.patches)
            {
                if (patch.impactVelocity != null)
                    return patch.impactVelocity;
            }
            return null;
        }

        public static Orbit getSpaceOrbit()
        {
            foreach (var patch in Trajectory.fetch.patches)
            {
                if (patch.startingState.stockPatch != null)
		        {
                    continue;
		        }

		        if (patch.isAtmospheric)
		        {
                    continue;
		        }

                if (patch.spaceOrbit != null)
		        {
                    return patch.spaceOrbit;
		        }
            }

            return null;	
	    }

        public static Vector3 plannedDirection()
        {
            return NavBallOverlay.GetPlannedDirection();
        }

        public static Vector3 correctedDirection()
        {
            return NavBallOverlay.GetCorrectedDirection();
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
