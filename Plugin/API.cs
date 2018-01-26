using System.Linq;
using UnityEngine;

namespace Trajectories
{
    /// <summary>
    /// This class only returns correct values for the "active vessel".
    /// </summary>
    public static class API
    {
        public static bool AlwaysUpdate
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

        public static double? GetEndTime()
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (var patch in Trajectory.fetch.Patches)
                {
                    if (patch.ImpactPosition.HasValue)
                        return patch.EndTime;
                }
            }
            return null;
        }

        public static Vector3? GetImpactPosition()
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (var patch in Trajectory.fetch.Patches)
                {
                    if (patch.ImpactPosition != null)
                        return patch.ImpactPosition;
                }
            }
            return null;
        }

        public static Vector3? GetImpactVelocity()
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (var patch in Trajectory.fetch.Patches)
                {
                    if (patch.ImpactVelocity != null)
                        return patch.ImpactVelocity;
                }
            }
            return null;
        }

        public static Orbit GetSpaceOrbit()
        {
            foreach (var patch in Trajectory.fetch.Patches)
            {
                if (FlightGlobals.ActiveVessel != null)
                {
                    if (patch.StartingState.StockPatch != null)
                    {
                        continue;
                    }

                    if (patch.IsAtmospheric)
                    {
                        continue;
                    }

                    if (patch.SpaceOrbit != null)
                    {
                        return patch.SpaceOrbit;
                    }
                }
            }
            return null;
        }

        public static Vector3? PlannedDirection()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                return NavBallOverlay.GetPlannedDirection();
            return null;
        }

        public static Vector3? CorrectedDirection()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                return NavBallOverlay.GetCorrectedDirection();
            return null;
        }

        public static bool HasTarget()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                return true;
            return false;
        }

        public static void SetTarget(double lat,double lon,double alt = 2.0)
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                var body = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);    // needs fixing, vessel is not allways at kerbin
                if (body != null)
                {
                    Vector3d worldPos = body.GetWorldSurfacePosition(lat, lon, alt);
                    Trajectory.Target.Set(body, worldPos - body.position);
                }
            }
        }

        private static void UpdateTrajectory()
        {
            Trajectory.fetch.ComputeTrajectory(FlightGlobals.ActiveVessel, DescentProfile.fetch);
        }
    }
}
