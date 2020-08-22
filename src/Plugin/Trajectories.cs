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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Trajectories
{
    /// <summary> Trajectories KSP Flight scenario class. </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT)]
    internal sealed class Trajectories : ScenarioModule
    {
        internal static string Version { get; }
        internal static Settings Settings { get; private set; }
        internal static Trajectory ActiveVesselTrajectory => LoadedVesselsTrajectories.FirstOrDefault(t => t.AttachedVessel == FlightGlobals.ActiveVessel);
        internal static List<Trajectory> LoadedVesselsTrajectories { get; private set; }

        static Trajectories()
        {
            // set and log version string
            Version = typeof(Trajectories).Assembly.GetName().Version.ToString();
            Version = Version.Remove(Version.LastIndexOf(".", StringComparison.Ordinal));
            Util.Log("v{0} Starting", Version);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node == null)
                return;
            Util.DebugLog("");

            LoadedVesselsTrajectories ??= new List<Trajectory>();
            Settings ??= new Settings(); // get trajectories settings from the config.xml file if it exists or create a new one
            if (Settings != null)
            {
                Settings.Load();

                AttachVessel(FlightGlobals.ActiveVessel);
                MainGUI.Start();
                AppLauncherButton.Start();
            }
            else
            {
                Util.LogError("There was a problem with the config.xml settings file");
            }
        }

        internal void Update()
        {
            if (Util.IsPaused || Settings == null || !Util.IsFlight)
                return;

            foreach (var vessel in FlightGlobals.VesselsLoaded)
            {
                if (LoadedVesselsTrajectories.All(t => t.AttachedVessel != vessel))
                {
                    AttachVessel(vessel);
                }
            }

            for (var i = LoadedVesselsTrajectories.Count - 1; i >= 0; i--)
            {
                Trajectory trajectory = LoadedVesselsTrajectories[i];
                if (FlightGlobals.VesselsLoaded.All(v => v != trajectory.AttachedVessel))
                {
                    trajectory.Destroy();
                    LoadedVesselsTrajectories.RemoveAt(i);
                }
            }

            foreach (var trajectory in LoadedVesselsTrajectories)
                trajectory.Update();

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
            foreach (var trajectory in LoadedVesselsTrajectories)
                trajectory.Destroy();
        }

        internal void OnApplicationQuit()
        {
            Util.Log("Ending after {0} seconds", Time.time);
            AppLauncherButton.Destroy();
            MainGUI.Destroy();
            foreach (var trajectory in LoadedVesselsTrajectories)
                trajectory.Destroy();
            if (Settings != null)
                Settings.Destroy();
            Settings = null;
        }

        private static void AttachVessel(Vessel vessel)
        {
            Util.DebugLog("Loading profiles for vessel: " + vessel);

            Trajectory trajectory = new Trajectory(vessel);

            if (trajectory.AttachedVessel == null)
            {
                Util.DebugLog("No vessel");
                trajectory.DescentProfile.Clear();
                trajectory.TargetProfile.Clear();
                trajectory.TargetProfile.ManualText = "";
            }
            else
            {
                LoadedVesselsTrajectories.Add(trajectory);
                TrajectoriesVesselSettings module = trajectory.AttachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                if (module == null)
                {
                    Util.DebugLog("No TrajectoriesVesselSettings module");
                    trajectory.DescentProfile.Clear();
                    trajectory.TargetProfile.Clear();
                    trajectory.TargetProfile.ManualText = "";
                }
                else if (!module.Initialized)
                {
                    Util.DebugLog("Initializing TrajectoriesVesselSettings module");
                    trajectory.DescentProfile.Clear();
                    trajectory.DescentProfile.Save(module);
                    trajectory.TargetProfile.Clear();
                    trajectory.TargetProfile.ManualText = "";
                    trajectory.TargetProfile.Save(module);
                    module.Initialized = true;
                    Util.Log("New vessel, profiles created");
                }
                else
                {
                    Util.DebugLog("Reading profile settings...");
                    // descent profile
                    if (trajectory.DescentProfile.Ready)
                    {
                        trajectory.DescentProfile.AtmosEntry.AngleRad = module.EntryAngle;
                        trajectory.DescentProfile.AtmosEntry.Horizon = module.EntryHorizon;
                        trajectory.DescentProfile.HighAltitude.AngleRad = module.HighAngle;
                        trajectory.DescentProfile.HighAltitude.Horizon = module.HighHorizon;
                        trajectory.DescentProfile.LowAltitude.AngleRad = module.LowAngle;
                        trajectory.DescentProfile.LowAltitude.Horizon = module.LowHorizon;
                        trajectory.DescentProfile.FinalApproach.AngleRad = module.GroundAngle;
                        trajectory.DescentProfile.FinalApproach.Horizon = module.GroundHorizon;
                        trajectory.DescentProfile.RefreshGui();
                    }

                    // target profile
                    trajectory.TargetProfile.SetFromLocalPos(FlightGlobals.Bodies.FirstOrDefault(b => b.name == module.TargetBody),
                        new Vector3d(module.TargetPosition_x, module.TargetPosition_y, module.TargetPosition_z));
                    trajectory.TargetProfile.ManualText = module.ManualTargetTxt;

                    Util.Log("Profiles loaded");
                }
            }
        }
    }
}
