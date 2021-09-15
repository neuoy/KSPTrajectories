/*
  Copyright© (c) 2017-2021 S.Gray, (aka PiezPiedPy).

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

using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    /// <summary> Contains copied game data that can be used outside the Unity main thread. All needed data must be copied and not referenced. </summary>
    internal static partial class GameDataCache
    {
        #region VESSEL_PROPERTIES
        // vessel properties
        internal static Vessel AttachedVessel { get; private set; }
        internal static CelestialBody VesselBody => VesselBodyIndex.HasValue ? FlightGlobals.Bodies[VesselBodyIndex.Value] : null;   // Not thread safe
        internal static BodyInfo VesselBodyInfo => VesselBodyIndex.HasValue ? Bodies[VesselBodyIndex.Value] : null;
        internal static int? VesselBodyIndex { get; private set; }
        internal static List<PartInfo> VesselParts { get; private set; }
        internal static double VesselMass { get; private set; }
        internal static Vector3d VesselWorldPos { get; private set; }
        internal static Vector3d VesselOrbitVelocity { get; private set; }
        internal static Vector3d VesselTransformUp { get; private set; }
        internal static Vector3d VesselTransformForward { get; private set; }
        #endregion

        internal static double UniversalTime { get; private set; }
        internal static double WarpDeltaTime { get; private set; }
        internal static Vector3d SunWorldPos { get; private set; }
        internal static List<BodyInfo> Bodies { get; private set; }


        internal static List<ManeuverNode> ManeuverNodes { get; private set; }
        internal static Orbit Orbit { get; private set; }
        internal static List<Orbit> FlightPlan { get; private set; }

        internal static void Start()
        {
            Util.DebugLog("Constructing");
            SunWorldPos = FlightGlobals.Bodies[0].position;
            Bodies = new() { FlightGlobals.Bodies };
        }

        /// <summary> Updates entire cache </summary>
        internal static bool Update()
        {
            Profiler.Start("GameDataCache.Update");

            UniversalTime = Planetarium.GetUniversalTime();
            WarpDeltaTime = TimeWarp.fixedDeltaTime;
            SunWorldPos = FlightGlobals.Bodies[0].position;

            if (Trajectories.AttachedVessel.mainBody == null)
            {
                Clear();
                return false;
            }

            // check for celestial body change
            if (VesselBodyIndex != Trajectories.AttachedVessel.mainBody.flightGlobalsIndex)
            {
                Util.DebugLog("Updating body to {0}", Trajectories.AttachedVessel.mainBody?.name);

                VesselBodyIndex = Trajectories.AttachedVessel.mainBody.flightGlobalsIndex;
            }

            // check for vessel changes
            if (AttachedVessel != Trajectories.AttachedVessel || VesselParts?.Count != Trajectories.AttachedVessel.Parts.Count)
            {
                Util.DebugLog("Updating {0} due to {1} change",
                    Trajectories.AttachedVessel.name, AttachedVessel != Trajectories.AttachedVessel ? "vessel" : "parts count");

                AttachedVessel = Trajectories.AttachedVessel;
                VesselParts?.Release();
                VesselParts = new() { AttachedVessel.Parts };

                Trajectories.AerodynamicModel.InitCache();
            }

            if (AttachedVessel.patchedConicSolver == null)
            {
                Util.DebugLogWarning("PatchedConicsSolver is null, skipping.");
                return false;
            }

            // update only the data that changes
            Bodies[VesselBodyIndex.Value].Update();
            UpdateVesselCache();

            ManeuverNodes = new List<ManeuverNode>(AttachedVessel.patchedConicSolver.maneuverNodes);
            Orbit = new Orbit(AttachedVessel.orbit);
            FlightPlan = new(AttachedVessel.patchedConicSolver.flightPlan);

            Profiler.Stop("GameDataCache.Update");
            return true;
        }

        /// <summary> Clears the cache </summary>
        internal static void Clear()
        {
            ClearVesselCache();
        }

        private static void ClearVesselCache()
        {
            AttachedVessel = null;
            VesselBodyIndex = null;
            VesselWorldPos = Vector3d.zero;
            VesselOrbitVelocity = Vector3d.zero;
            VesselTransformUp = Vector3d.zero;
            VesselTransformForward = Vector3d.zero;

            VesselParts?.Clear();
            VesselParts = null;
        }

        private static void UpdateVesselCache()
        {
            VesselWorldPos = AttachedVessel.GetWorldPos3D();
            VesselOrbitVelocity = AttachedVessel.obt_velocity;
            VesselTransformUp = AttachedVessel.ReferenceTransform.up;
            VesselTransformForward = AttachedVessel.ReferenceTransform.forward;

            VesselParts.Update(AttachedVessel.Parts);
        }
    }
}
