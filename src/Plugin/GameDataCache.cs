using System.Collections.Generic;

namespace Trajectories
{
    /// <summary> Contains copied game data that can be used outside the Unity main thread </summary>
    internal static class GameDataCache
    {

        internal static Vessel AttachedVessel { get; private set; }

        internal static List<ManeuverNode> ManeuverNodes { get; private set; }
        internal static Orbit Orbit { get; private set; }
        internal static List<Orbit> FlightPlan { get; private set; }


        /// <summary> Updates entire cache </summary>
        internal static void Update()
        {
            Profiler.Start("GameDataCache.Update");

            AttachedVessel = Trajectories.AttachedVessel;
            ManeuverNodes = new List<ManeuverNode>(AttachedVessel.patchedConicSolver.maneuverNodes);
            Orbit = new Orbit(AttachedVessel.orbit);
            FlightPlan = new List<Orbit>(AttachedVessel.patchedConicSolver.flightPlan);

            Profiler.Stop("GameDataCache.Update");
        }
    }
}
