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
using KSP.Localization;
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

        /// <summary> The Aerodynamic Model used for atmospheric trajectory calculations </summary>
        internal static VesselAerodynamicModel AerodynamicModel { get; private set; }

        /// <returns> The name of the Aerodynamic Model </returns>
        internal static string AerodynamicModelName => AerodynamicModel == null ? Localizer.Format("#autoLOC_Trajectories_NotLoaded") :
                                                                                   AerodynamicModel.AerodynamicModelName;

        /// <summary> The vessel that trajectories is attached to </summary>
        internal static Vessel AttachedVessel { get; private set; }

        /// <returns> True if trajectories is attached to a vessel </returns>
        internal static bool IsVesselAttached => AttachedVessel != null;

        /// <returns> True if trajectories is attached to a vessel and that the vessel also has parts </returns>
        internal static bool VesselHasParts => IsVesselAttached && AttachedVessel.Parts.Count != 0;

        //  constructor
        static Trajectories()
        {
            // set and log version string
            Version = typeof(Trajectories).Assembly.GetName().Version.ToString();
            Version = Version.Remove(Version.LastIndexOf("."));
            Util.Log("v{0} Starting", Version);

            // setup worker
            Worker.OnReport += Worker_OnReport;
            Worker.OnUpdate += Worker_OnUpdate;
            Worker.OnError += Worker_OnError;
            Worker.Initialize();
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node == null)
                return;

            Util.DebugLog("");

            //version = Util.ConfigValue(node, "version", Version);             // get saved version, defaults to current version if none

            Settings ??= new Settings();                                // get trajectories settings from the config.xml file if it exists or create a new one
            AerodynamicModel ??= AerodynamicModelFactory.GetModel();    // get aerodynamic model, searches for compatible API's

            if (Settings != null && AerodynamicModel != null)
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
            if (Util.IsPaused || Settings == null || AerodynamicModel == null || !Util.IsFlight)
                return;

            if (AttachedVessel != FlightGlobals.ActiveVessel)
                AttachVessel();

            Trajectory.Update();
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
            AttachedVessel = null;
            AppLauncherButton.DestroyToolbarButton();
            MainGUI.DeSpawn();
            NavBallOverlay.DestroyTransforms();
            FlightOverlay.Destroy();
            MapOverlay.DestroyRenderer();
            Trajectory.Destroy();
            DescentProfile.Clear();
            Worker.Cancel();
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
            Worker.Dispose();
        }

        private void AttachVessel()
        {
            Util.DebugLog("Loading profiles for vessel");

            Worker.Cancel();
            AttachedVessel = FlightGlobals.ActiveVessel;

            if (AttachedVessel == null)
            {
                Util.DebugLog("No vessel");
                DescentProfile.Clear();
                TargetProfile.Clear();
                TargetProfile.ManualText = "";
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
            }
        }

        #region WORKER_THREAD_CALLBACKS

        internal static void Worker_OnUpdate(Worker.JOB job, bool result)
        {
            switch (job)
            {
                case Worker.JOB.COMPUTE_PATCHES:
                    Trajectory.ComputeComplete();
                    break;
            }
            //if (ModulesForm.Exists)
            //ModulesForm.Instance.UpdateData();
        }

        private static void Worker_OnReport(Worker.EVENT_TYPE type, int progress_percentage)
        {
            switch (type)
            {
                case Worker.EVENT_TYPE.PERCENTAGE:
                    //MainForm.Instance.ToolStripProgressBar.Value = progress_percentage;
                    //MainForm.Instance.StatusStrip.Update();
                    break;
                case Worker.EVENT_TYPE.STATUSBAR:
                    //Update_StatusBar();
                    break;
                case Worker.EVENT_TYPE.ALL:
                    //MainForm.Instance.ToolStripProgressBar.Value = progress_percentage;
                    //Update_AllButtons(true);
                    //Update_StatusBar();
                    break;
                case Worker.EVENT_TYPE.APPCLOSE:
                    //MainForm.Instance.Close();
                    break;
            }
        }

        private static void Worker_OnError(Worker.JOB job, System.Exception error)
        {
            switch (job)
            {
                case Worker.JOB.COMPUTE_PATCHES:
                    Trajectory.ComputeError();
                    break;
            }

            Util.DebugLogError("{0} failed, {1}", job, error.Message);
        }

        #endregion
    }
}
