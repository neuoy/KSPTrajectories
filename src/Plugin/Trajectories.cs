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

using UnityEngine;

namespace Trajectories
{
    /// <summary> Trajectories Top class. </summary>
	[KSPScenario(ScenarioCreationOptions.AddToAllGames, new[] { GameScenes.FLIGHT })]
    internal sealed class Trajectories : ScenarioModule
    {
        //public static string version;                           // savegame version
        //public static int uid;                                  // savegame unique id

        // version string
        internal static string Version { get; private set; } = "X.X.X";

        internal static Settings Settings { get; private set; }

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
            if (Util.IsPaused || Settings == null)
                return;

            DescentProfile.Update();
            Trajectory.Update();
            MapOverlay.Update();
            FlightOverlay.Update();
            NavBallOverlay.Update();
            MainGUI.Update();
            if (!Settings.NewGui)
                OldGUI.Update();
        }

#if DEBUG && DEBUG_TELEMETRY
        internal void FixedUpdate() => Trajectory.DebugTelemetry();
#endif

        internal void OnGUI()
        {
            if (!Settings.NewGui)
                OldGUI.OnGUI();
        }

        internal void OnDestroy()
        {
            Util.DebugLog("");
            AppLauncherButton.DestroyToolbarButton();
            MainGUI.DeSpawn();
            NavBallOverlay.DestroyRenderer();
            FlightOverlay.Destroy();
            MapOverlay.DestroyRenderer();
            Trajectory.Destroy();
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
            Trajectory.Destroy();
            DescentProfile.Destroy();
        }
    }
}
