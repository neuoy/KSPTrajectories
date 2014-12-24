using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    class FlightMapVisibility : IVisibility
    {
        internal static FlightMapVisibility Instance
        {
            get { return new FlightMapVisibility(); }
        }
        
        private static IVisibility FLIGHT_VISIBILITY;

        public bool Visible
        {
            get
            {
                return FLIGHT_VISIBILITY.Visible && MapView.MapIsEnabled;
            }
        }

        private FlightMapVisibility()
        {
            if (FLIGHT_VISIBILITY == null)
                FLIGHT_VISIBILITY = new GameScenesVisibility(GameScenes.FLIGHT);
        }
    }

    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class MapGUI : MonoBehaviour
    {
        private static readonly int GUIId = 934824;

        private ApplicationLauncherButton GUIToggleButton;
        private IButton GUIToggleButtonBlizzy;

        private string tooltip = String.Empty;


        /// <summary>
        /// Check if patched conics are available in the current save.
        /// </summary>
        /// <returns>True if patched conics are available</returns>
        private bool isPatchedConicsAvailable()
        {
            // Get our level of tracking station
            float trackingstation_level =
                ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation);

            // Check if the tracking station knows Patched Conics
            return 
                GameVariables.Instance.GetOrbitDisplayMode(trackingstation_level).CompareTo(
                    GameVariables.OrbitDisplayMode.PatchedConics)
                >= 0;
        }

        private bool farInstalled()
        {
            // remove when FAR prediction is fixed.
            foreach (var loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name.Equals("NEAR"))
                    return true;

                if(loadedAssembly.name.Equals("FerramAerospaceResearch"))
                    return true;
            }

            return false;
        }

        public void OnGUI()
        {
            if(!Settings.fetch.GUIEnabled)
                return;

            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;

            if (!MapView.MapIsEnabled || MapView.MapCamera == null)
                return;

            Settings.fetch.MapGUIWindowPos = new Rect(Settings.fetch.MapGUIWindowPos.xMin, Settings.fetch.MapGUIWindowPos.yMin, Settings.fetch.MapGUIWindowPos.width, Settings.fetch.MapGUIWindowPos.height - 1);
            Settings.fetch.MapGUIWindowPos = ClampToScreen( GUILayout.Window(GUIId + 1, Settings.fetch.MapGUIWindowPos, MainWindow, "Trajectories") );

            if (tooltip != "")
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                int tooltipWidth = 400;
                int tooltipHeight = 80;
                Rect tooltipRect = new Rect(mousePos.x - 50, mousePos.y + 10, tooltipWidth, tooltipHeight);

                GUILayout.Window(GUIId + 2, ClampToScreen(tooltipRect), TooltipWindow, "");
            }
        }

        private bool ToggleGroup(bool visible, string label)
        {
            //return GUILayout.Toggle(visible, label);
            var oldAlignement = GUI.skin.button.alignment;
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button((visible ? "^ " : "v ") + label))
                visible = !visible;
            GUI.skin.button.alignment = oldAlignement;
            return visible;
        }

        private void MainWindow(int id)
        {
            Trajectory traj = Trajectory.fetch;
            var lastPatch = traj.patches.LastOrDefault();

            GUILayout.BeginHorizontal();
            Settings.fetch.DisplayTrajectories = GUILayout.Toggle(Settings.fetch.DisplayTrajectories, "Display trajectory", GUILayout.Width(125));

            // check that we have patched conics. If not, apologize to the user and return.
            if (Settings.fetch.DisplayTrajectories && !isPatchedConicsAvailable())
            {
                ScreenMessages.PostScreenMessage(
                    "Can't show trajectory because patched conics are not available." +
                    " Please update your tracking station facility.");
                Settings.fetch.DisplayTrajectories = false;
                return;
            }


            // :(
            if (Settings.fetch.DisplayTrajectories && farInstalled())
            {
                ScreenMessages.PostScreenMessage("Sorry, trajectory prediction does not work with FAR or NEAR.");
                Settings.fetch.DisplayTrajectories = false;
                return;
            }

            if (Settings.fetch.DisplayTrajectories)
            {
                Settings.fetch.DisplayCompleteTrajectory = GUILayout.Toggle(Settings.fetch.DisplayCompleteTrajectory, "complete", GUILayout.Width(70));
            }
            GUILayout.EndHorizontal();

            Settings.fetch.BodyFixedMode = GUILayout.Toggle(Settings.fetch.BodyFixedMode, "Body-fixed mode");

            GUILayout.Label("Max G-force: " + (traj.MaxAccel / 9.81).ToString("0.00"));
            if (lastPatch != null && lastPatch.impactPosition.HasValue)
            {
                Vector3 up = lastPatch.impactPosition.Value.normalized;
                Vector3 vel = lastPatch.impactVelocity - lastPatch.startingState.referenceBody.getRFrmVel(lastPatch.impactPosition.Value + lastPatch.startingState.referenceBody.position);
                float vVelMag = Vector3.Dot(vel, up);
                Vector3 vVel = up * vVelMag;
                float hVelMag = (vel - vVel).magnitude;
                GUILayout.Label("Impact: V = " + vVelMag.ToString("0.0") + "m/s, H = " + hVelMag.ToString("0.0") + "m/s");
            }
            else
            {
                GUILayout.Label("Impact velocity: -");
            }
            GUILayout.Space(10);

            if (Settings.fetch.DisplayTargetGUI = ToggleGroup(Settings.fetch.DisplayTargetGUI, "Target"))
            {
                GUI.enabled = traj.targetPosition.HasValue;
                if (GUILayout.Button("Unset target"))
                    Trajectory.SetTarget();
                GUI.enabled = true;

                var patch = traj.patches.LastOrDefault();
                GUI.enabled = (patch != null && patch.impactPosition.HasValue);
                if (GUILayout.Button("Set current impact as target"))
                {
                    Trajectory.SetTarget(patch.startingState.referenceBody, patch.impactPosition);
                }
                GUI.enabled = true;
            }
            GUILayout.Space(10);

            if (Settings.fetch.DisplayDescentProfileGUI = ToggleGroup(Settings.fetch.DisplayDescentProfileGUI, "Descent profile"))
            {
                DescentProfile.fetch.DoGUI();
            }
            GUILayout.Space(10);

            if (Settings.fetch.DisplaySettingsGUI = ToggleGroup(Settings.fetch.DisplaySettingsGUI, "Settings"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Max patches", GUILayout.Width(100));
                Settings.fetch.MaxPatchCount = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)Settings.fetch.MaxPatchCount, 3, 10, GUILayout.Width(100)));
                GUILayout.Label(Settings.fetch.MaxPatchCount.ToString(), GUILayout.Width(15));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Max frames per patch", GUILayout.Width(100));
                Settings.fetch.MaxFramesPerPatch = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)Settings.fetch.MaxFramesPerPatch, 1, 50, GUILayout.Width(100)));
                GUILayout.Label(Settings.fetch.MaxFramesPerPatch.ToString(), GUILayout.Width(15));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Settings.fetch.AutoUpdateAerodynamicModel = GUILayout.Toggle(Settings.fetch.AutoUpdateAerodynamicModel, new GUIContent("Auto update", "Auto-update of the aerodynamic model. For example if a part is decoupled, the model needs to be updated. This is independent from trajectory update."));
                if (GUILayout.Button("Update now"))
                    traj.InvalidateAerodynamicModel();
                GUILayout.EndHorizontal();

                if (ToolbarManager.ToolbarAvailable)
                {
                    Settings.fetch.UseBlizzyToolbar = GUILayout.Toggle(Settings.fetch.UseBlizzyToolbar, new GUIContent("Use Blizzy's toolbar", "Will take effect after restart"));
                }

                GUILayout.Label("Aerodynamic model: " + VesselAerodynamicModel.AerodynamicModelName);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Perf: " + (traj.ComputationTime * 1000.0f).ToString("0.0") + "ms (" + (traj.ComputationTime/traj.GameFrameTime*100.0f).ToString("0") + "%)", GUILayout.Width(120));
                GUILayout.Label(traj.ErrorCount + " error(s)", GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }

            tooltip = GUI.tooltip;

            GUI.DragWindow();
        }

        private void TooltipWindow(int id)
        {
            GUIStyle toolTipStyle = new GUIStyle(GUI.skin.label);
            toolTipStyle.hover = toolTipStyle.active = toolTipStyle.normal;
            toolTipStyle.normal.textColor = toolTipStyle.active.textColor = toolTipStyle.hover.textColor = toolTipStyle.focused.textColor = toolTipStyle.onNormal.textColor = toolTipStyle.onHover.textColor = toolTipStyle.onActive.textColor = toolTipStyle.onFocused.textColor = new Color(1, 0.75f, 0);
            toolTipStyle.wordWrap = true;
            GUILayout.Label(tooltip, toolTipStyle);
        }

        public void Awake()
        {
            if (ToolbarManager.ToolbarAvailable && Settings.fetch.UseBlizzyToolbar)
            {
                Debug.Log("Using Blizzy toolbar for Trajectories GUI");
                GUIToggleButtonBlizzy = ToolbarManager.Instance.add("Trajectories", "ToggleUI");
                GUIToggleButtonBlizzy.Visibility = FlightMapVisibility.Instance;
                GUIToggleButtonBlizzy.TexturePath = "Trajectories/Textures/icon-blizzy";
                GUIToggleButtonBlizzy.ToolTip = "Right click toggles Trajectories window";
                GUIToggleButtonBlizzy.OnClick += OnToggleGUIBlizzy;
            }
            else
            {
                Debug.Log("Using stock toolbar for Trajectories GUI");
                GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            }
        }

        void OnGUIAppLauncherReady()
        {
            if (ApplicationLauncher.Ready)
            {
                GUIToggleButton = ApplicationLauncher.Instance.AddModApplication(
                    OnToggleOn,
                    OnToggleOff,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    ApplicationLauncher.AppScenes.MAPVIEW,
                    (Texture)GameDatabase.Instance.GetTexture("Trajectories/Textures/icon", false));

                if(Settings.fetch.GUIEnabled)
                    GUIToggleButton.SetTrue(false);
            }
        }

        void DummyVoid() { }

        void OnToggleGUIBlizzy(ClickEvent e)
        {
            if (e.MouseButton == 0)
            {
                // check that we have patched conics. If not, apologize to the user and return.
                if (!isPatchedConicsAvailable())
                {
                    ScreenMessages.PostScreenMessage(
                        "Can't show trajectory because patched conics are not available." +
                        " Please update your tracking station facility.");
                    Settings.fetch.DisplayTrajectories = false;
                    return;
                }

                // :(
                if (farInstalled())
                {
                    ScreenMessages.PostScreenMessage("Sorry, trajectory prediction does not work with FAR or NEAR.");
                    Settings.fetch.DisplayTrajectories = false;
                    return;
                }

                Settings.fetch.DisplayTrajectories = !Settings.fetch.DisplayTrajectories;
            }
            else
            {
                Settings.fetch.GUIEnabled = !Settings.fetch.GUIEnabled;
            }
        }

        void OnToggleOn()
        {
            Settings.fetch.GUIEnabled = true;
        }

        void OnToggleOff()
        {
            Settings.fetch.GUIEnabled = false;
        }

        void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            if (GUIToggleButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(GUIToggleButton);
            if (GUIToggleButtonBlizzy != null)
                GUIToggleButtonBlizzy.Destroy();
        }

        private static Rect ClampToScreen(Rect window)
        {
            return new Rect(Mathf.Clamp(window.x, 0, Screen.width - window.width), Mathf.Clamp(window.y, 0, Screen.height - window.height), window.width, window.height);
        }
    }
}
