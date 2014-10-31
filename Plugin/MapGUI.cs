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

        public void OnGUI()
        {
            if(!Settings.fetch.GUIEnabled)
                return;

            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;

            if (!MapView.MapIsEnabled || MapView.MapCamera == null)
                return;

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

        private void MainWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Computations: " + Trajectory.fetch.ComputationTime + "s", GUILayout.Width(100));
            GUILayout.Label(Trajectory.fetch.ErrorCount+" error(s)", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            if (ToolbarManager.ToolbarAvailable)
            {
                Settings.fetch.UseBlizzyToolbar = GUILayout.Toggle(Settings.fetch.UseBlizzyToolbar, new GUIContent("Use Blizzy's toolbar", "Will take effect after restart"));
            }

            GUILayout.BeginHorizontal();
            Settings.fetch.DisplayTrajectories = GUILayout.Toggle(Settings.fetch.DisplayTrajectories, "Display trajectory", GUILayout.Width(125));

            if (Settings.fetch.DisplayTrajectories)
            {
                Settings.fetch.DisplayCompleteTrajectory = GUILayout.Toggle(Settings.fetch.DisplayCompleteTrajectory, "complete", GUILayout.Width(70));
            }
            GUILayout.EndHorizontal();

            Settings.fetch.BodyFixedMode = GUILayout.Toggle(Settings.fetch.BodyFixedMode, "Body-fixed mode");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max patches", GUILayout.Width(80));
            Settings.fetch.MaxPatchCount = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)Settings.fetch.MaxPatchCount, 3, 10, GUILayout.Width(120)));
            GUILayout.Label(Settings.fetch.MaxPatchCount.ToString(), GUILayout.Width(15));
            GUILayout.EndHorizontal();

            GUI.enabled = Trajectory.fetch.targetPosition.HasValue;
            if (GUILayout.Button("Unset target"))
                Trajectory.SetTarget();
            GUI.enabled = true;

            var patch = Trajectory.fetch.patches.LastOrDefault();
            GUI.enabled = (patch != null && patch.impactPosition.HasValue);
            if (GUILayout.Button("Set current impact as target"))
            {
                Trajectory.SetTarget(patch.startingState.referenceBody, patch.impactPosition);
            }
            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            Settings.fetch.AutoUpdateAerodynamicModel = GUILayout.Toggle(Settings.fetch.AutoUpdateAerodynamicModel, new GUIContent("Auto update", "Auto-update of the aerodynamic model. For example if a part is decoupled, the model needs to be updated. This is independent from trajectory update."));
            if (GUILayout.Button("Update now"))
                Trajectory.fetch.InvalidateAerodynamicModel();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Descent profile");
            DescentProfile.fetch.DoGUI();

            GUILayout.Label ("Expected g loading: " + (Trajectory.fetch.maxaccel / 9.81).ToString("0.00"));

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
