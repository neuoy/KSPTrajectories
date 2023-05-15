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

using System.Linq;
using UnityEngine;

namespace Trajectories
{
    /// <summary> Trajectories KSP Flight scenario class. </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] { GameScenes.FLIGHT })]
    internal sealed class Trajectories : ScenarioModule
    {
        //public static string version;                           // savegame version
        //public static int uid;                                  // savegame unique id

        // version string
        internal static string Version { get; private set; } = "X.X.X";

        internal static Settings Settings { get; private set; }

        /// <summary> The vessel that trajectories is attached to </summary>
        internal static Vessel AttachedVessel { get; private set; }

        /// <returns> True if trajectories is attached to a vessel </returns>
        internal static bool IsVesselAttached => AttachedVessel != null;

        /// <returns> True if trajectories is attached to a vessel and that the vessel also has parts </returns>
        internal static bool VesselHasParts => IsVesselAttached ? AttachedVessel.Parts.Count != 0 : false;

        private static bool init_aerodynamic_model = true;
        private static bool vessel_unpacked = false;

        //  constructor
        static Trajectories()
        {
            // set and log version string
            Version = typeof(Trajectories).Assembly.GetName().Version.ToString();
            Version = Version.Remove(Version.LastIndexOf("."));
            Util.Log("v{0} Starting", Version);
        }

        public override void OnLoad(ConfigNode node)
        {
            Util.DebugLog("");

#if DEBUG
            DebugLines.Transform = null;
#endif
            if (node == null)
                return;

            GameEvents.onTimeWarpRateChanged.Add(WarpChanged);
            GameEvents.onVesselLoaded.Add(VesselLoaded);

            AttachedVessel = null;
            init_aerodynamic_model = true;
            vessel_unpacked = false;

            //version = Util.ConfigValue(node, "version", Version);     // get saved version, defaults to current version if none

            Settings ??= new Settings();                          // get trajectories settings from the config.xml file if it exists or create a new one
            if (Settings != null)
            {
                Settings.Load();

                DescentProfile.Start();
                Trajectory.Start();
                MapOverlay.Start();
                FlightOverlay.Start();
                NavBallOverlay.Start();
                MainGUI.Start();
                AppLauncherButton.Start();
            }
            else
            {
                Util.LogError("There was a problem with the config.xml settings file");
            }
        }

        /*public override void OnSave(ConfigNode node)
        {
            if (node == null)
                return;

            Util.DebugLog("Node: {0}", node.name);

            //node.AddValue("version", Version);                       // save version
        }*/

        internal void Update()
        {
            if (Util.IsPaused || Settings == null || !Util.IsFlight)
                return;

            if (AttachedVessel != FlightGlobals.ActiveVessel)
                AttachVessel();

            if (init_aerodynamic_model && TimeWarp.CurrentRate == 1f)
            {
                Trajectory.InvalidateAerodynamicModel();
                init_aerodynamic_model = false;
            }

            if (IsVesselAttached && !AttachedVessel.packed && !vessel_unpacked)
            {
                Util.DebugLog("Vessel unpacked");
                Trajectory.InvalidateAerodynamicModel();
                init_aerodynamic_model = false;
                vessel_unpacked = true;
            }

            Trajectory.Update();
            MapOverlay.Update();
            FlightOverlay.Update();
            NavBallOverlay.Update();
            MainGUI.Update();
        }

        internal void FixedUpdate()
        {
#if DEBUG_TELEMETRY
            Trajectory.DebugTelemetry();
#endif
            FlightOverlay.FixedUpdate();
        }

        internal void OnDestroy()
        {
            Util.DebugLog("");
            AttachedVessel = null;
            GameEvents.onVesselLoaded.Remove(VesselLoaded);
            GameEvents.onTimeWarpRateChanged.Remove(WarpChanged);
            AppLauncherButton.DestroyToolbarButton();
            MainGUI.DeSpawn();
            NavBallOverlay.DestroyTransforms();
            FlightOverlay.Destroy();
            MapOverlay.DestroyRenderer();
            Trajectory.Destroy();
            DescentProfile.Clear();
        }

        internal void OnApplicationQuit()
        {
            Util.Log("Ending after {0} seconds", Time.time);
            AttachedVessel = null;
            AppLauncherButton.Destroy();
            MainGUI.Destroy();
            NavBallOverlay.Destroy();
            FlightOverlay.Destroy();
            MapOverlay.Destroy();
            Trajectory.Destroy();
            DescentProfile.Destroy();
            if (Settings != null)
                Settings.Destroy();
            Settings = null;
        }

        private void AttachVessel()
        {
            Util.DebugLog("Loading profiles for vessel");

            AttachedVessel = FlightGlobals.ActiveVessel;

            if (AttachedVessel == null)
            {
                Util.DebugLog("No vessel");
                DescentProfile.Clear();
                TargetProfile.Clear();
                TargetProfile.ManualText = "";
#if DEBUG
                DebugLines.Transform = null;
#endif
            }
            else
            {
                TrajectoriesVesselSettings module = AttachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                if (module == null)
                {
                    Util.DebugLog("No TrajectoriesVesselSettings module");
                    DescentProfile.Clear();
                    TargetProfile.Clear();
                    TargetProfile.ManualText = "";
                }
                else if (!module.Initialized)
                {
                    Util.DebugLog("Initializing TrajectoriesVesselSettings module");
                    DescentProfile.Clear();
                    DescentProfile.Save(module);
                    TargetProfile.Clear();
                    TargetProfile.ManualText = "";
                    TargetProfile.Save(module);
                    module.Initialized = true;
                    Util.Log("New vessel, profiles created");
                }
                else
                {
                    Util.DebugLog("Reading profile settings...");
                    // descent profile
                    if (DescentProfile.Ready)
                    {
                        DescentProfile.AtmosEntry.AngleRad = module.EntryAngle;
                        DescentProfile.AtmosEntry.Horizon = module.EntryHorizon;
                        DescentProfile.HighAltitude.AngleRad = module.HighAngle;
                        DescentProfile.HighAltitude.Horizon = module.HighHorizon;
                        DescentProfile.LowAltitude.AngleRad = module.LowAngle;
                        DescentProfile.LowAltitude.Horizon = module.LowHorizon;
                        DescentProfile.FinalApproach.AngleRad = module.GroundAngle;
                        DescentProfile.FinalApproach.Horizon = module.GroundHorizon;
                        DescentProfile.RefreshGui();
                    }

                    // target profile
                    TargetProfile.SetFromLocalPos(FlightGlobals.Bodies.FirstOrDefault(b => b.name == module.TargetBody),
                        new Vector3d(module.TargetPosition_x, module.TargetPosition_y, module.TargetPosition_z));
                    TargetProfile.ManualText = module.ManualTargetTxt;
                    Util.Log("Profiles loaded");
                }
#if DEBUG
                DebugLines.Transform = AttachedVessel.transform;
                //DebugLines.Transform = AttachedVessel.mainBody.transform;
#endif
            }
        }

        private void WarpChanged() => init_aerodynamic_model = TimeWarp.CurrentRate != 1f;

        private void VesselLoaded(Vessel vessel) => init_aerodynamic_model = true;
    }
}
