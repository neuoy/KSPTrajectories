/*
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

        internal static Trajectory ActiveVesselTrajectory { get; private set; }

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
            if (node == null)
                return;

            Util.DebugLog("");

            //version = Util.ConfigValue(node, "version", Version);     // get saved version, defaults to current version if none

            Settings ??= new Settings();                          // get trajectories settings from the config.xml file if it exists or create a new one
            if (Settings != null)
            {
                Settings.Load();

                DescentProfile.Start();
                ActiveVesselTrajectory = new Trajectory(FlightGlobals.ActiveVessel);
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

            if (ActiveVesselTrajectory.AttachedVessel != FlightGlobals.ActiveVessel)
                AttachVessel();

            ActiveVesselTrajectory.Update();
            MapOverlay.Update();
            FlightOverlay.Update();
            NavBallOverlay.Update();
            MainGUI.Update();
        }

#if DEBUG_TELEMETRY
        internal void FixedUpdate() => Trajectory.DebugTelemetry();
#endif

        internal void OnDestroy()
        {
            Util.DebugLog("");
            AppLauncherButton.DestroyToolbarButton();
            MainGUI.DeSpawn();
            NavBallOverlay.DestroyTransforms();
            FlightOverlay.Destroy();
            MapOverlay.DestroyRenderer();
            ActiveVesselTrajectory.Destroy();
            ActiveVesselTrajectory = null;
            DescentProfile.Clear();
        }

        internal void OnApplicationQuit()
        {
            Util.Log("Ending after {0} seconds", Time.time);
            AppLauncherButton.Destroy();
            MainGUI.Destroy();
            NavBallOverlay.Destroy();
            FlightOverlay.Destroy();
            MapOverlay.Destroy();
            ActiveVesselTrajectory.Destroy();
            ActiveVesselTrajectory = null;
            DescentProfile.Destroy();
            if (Settings != null)
                Settings.Destroy();
            Settings = null;
        }

        private static void AttachVessel()
        {
            Util.DebugLog("Loading profiles for vessel");

            ActiveVesselTrajectory = new Trajectory(FlightGlobals.ActiveVessel);

            if (ActiveVesselTrajectory.AttachedVessel == null)
            {
                Util.DebugLog("No vessel");
                DescentProfile.Clear();
                ActiveVesselTrajectory.TargetProfile.Clear();
                ActiveVesselTrajectory.TargetProfile.ManualText = "";
            }
            else
            {
                TrajectoriesVesselSettings module = ActiveVesselTrajectory.AttachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                if (module == null)
                {
                    Util.DebugLog("No TrajectoriesVesselSettings module");
                    DescentProfile.Clear();
                    ActiveVesselTrajectory.TargetProfile.Clear();
                    ActiveVesselTrajectory.TargetProfile.ManualText = "";
                }
                else if (!module.Initialized)
                {
                    Util.DebugLog("Initializing TrajectoriesVesselSettings module");
                    DescentProfile.Clear();
                    DescentProfile.Save(module);
                    ActiveVesselTrajectory.TargetProfile.Clear();
                    ActiveVesselTrajectory.TargetProfile.ManualText = "";
                    ActiveVesselTrajectory.TargetProfile.Save(module);
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
                    ActiveVesselTrajectory.TargetProfile.SetFromLocalPos(FlightGlobals.Bodies.FirstOrDefault(b => b.name == module.TargetBody),
                        new Vector3d(module.TargetPosition_x, module.TargetPosition_y, module.TargetPosition_z));
                    ActiveVesselTrajectory.TargetProfile.ManualText = module.ManualTargetTxt;
                    Util.Log("Profiles loaded");
                }
            }
        }
    }
}
