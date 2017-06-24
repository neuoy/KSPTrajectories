﻿using KSP.UI.Screens;
using System;
using System.Linq;
using UnityEngine;

namespace Trajectories
{
    class GUIToggleButtonBlizzyVisibility : IVisibility
    {
        internal static GUIToggleButtonBlizzyVisibility Instance
        {
            get { return new GUIToggleButtonBlizzyVisibility(); }
        }

        private static IVisibility FLIGHT_VISIBILITY;

        public bool Visible
        {
            get
            {
                return FLIGHT_VISIBILITY.Visible;
            }
        }

        private GUIToggleButtonBlizzyVisibility()
        {
            if (FLIGHT_VISIBILITY == null)
                FLIGHT_VISIBILITY = new GameScenesVisibility(GameScenes.FLIGHT);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class MapGUI : MonoBehaviour
    {
        private static readonly int GUIId = 934824;

        private static ApplicationLauncherButton GUIToggleButton = null;
        private IButton GUIToggleButtonBlizzy;

        private string tooltip = String.Empty;

        private string coords = "";

        private Vector3d impactPos = new Vector3d();
        private Vector3d targetPos = new Vector3d();

        // click through locks
        private bool clickThroughLocked = false;
        private const ControlTypes FlightLockTypes = ControlTypes.MANNODE_ADDEDIT | ControlTypes.MANNODE_DELETE | ControlTypes.MAP_UI |
            ControlTypes.TARGETING | ControlTypes.VESSEL_SWITCHING | ControlTypes.TWEAKABLES;

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

        public void OnGUI()
        {
            if (!Settings.fetch.GUIEnabled || !Util.IsFlight || PlanetariumCamera.Camera == null)
                return;

            Settings.fetch.MapGUIWindowPos = new Rect(Settings.fetch.MapGUIWindowPos.xMin, Settings.fetch.MapGUIWindowPos.yMin, Settings.fetch.MapGUIWindowPos.width, Settings.fetch.MapGUIWindowPos.height - 1);
            Settings.fetch.MapGUIWindowPos = ClampToScreen(GUILayout.Window(GUIId + 1, Settings.fetch.MapGUIWindowPos, MainWindow, "Trajectories"));

            if (tooltip != "")
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;
                int tooltipWidth = 400;
                int tooltipHeight = 80;
                Rect tooltipRect = new Rect(mousePos.x - 50, mousePos.y + 10, tooltipWidth, tooltipHeight);

                GUILayout.Window(GUIId + 2, ClampToScreen(tooltipRect), TooltipWindow, "");
            }

            // Disable Click through
            bool mouse_over = Settings.fetch.MapGUIWindowPos.Contains(Event.current.mousePosition);
            if (mouse_over && !clickThroughLocked)
            {
                InputLockManager.SetControlLock(FlightLockTypes, "TrajectoriesFlightLock");
                clickThroughLocked = true;
            }
            if (!mouse_over && clickThroughLocked)
            {
                InputLockManager.RemoveControlLock("TrajectoriesFlightLock");
                clickThroughLocked = false;
            }
        }

        private bool ToggleGroup(bool visible, string label, int? width = null)
        {
            //return GUILayout.Toggle(visible, label);
            var oldAlignement = GUI.skin.button.alignment;
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            bool buttonClicked;
            if (width.HasValue)
                buttonClicked = GUILayout.Button((visible ? "^ " : "v ") + label, GUILayout.Width(width.Value));
            else
                buttonClicked = GUILayout.Button((visible ? "^ " : "v ") + label);
            if (buttonClicked)
                visible = !visible;
            GUI.skin.button.alignment = oldAlignement;
            return visible;
        }

        private void MainWindow(int id)
        {
            Trajectory traj = Trajectory.fetch;
            Trajectory.Patch lastPatch = traj.patches.LastOrDefault();
            CelestialBody lastPatchBody = lastPatch?.startingState.referenceBody;
            CelestialBody targetBody = traj.targetBody;

            GUILayout.BeginHorizontal();

            Settings.fetch.DisplayTrajectories = GUILayout.Toggle(Settings.fetch.DisplayTrajectories, "Show trajectory", GUILayout.Width(125));

            Settings.fetch.DisplayTrajectoriesInFlight = GUILayout.Toggle(Settings.fetch.DisplayTrajectoriesInFlight, "In-Flight");

            // check that we have patched conics. If not, apologize to the user and return.
            if (Settings.fetch.DisplayTrajectories && !isPatchedConicsAvailable())
            {
                ScreenMessages.PostScreenMessage(
                    "Can't show trajectory because patched conics are not available." +
                    " Please update your tracking station facility.");
                Settings.fetch.DisplayTrajectories = false;
                return;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            Settings.fetch.BodyFixedMode = GUILayout.Toggle(Settings.fetch.BodyFixedMode, "Body-fixed mode");

            if (Settings.fetch.DisplayTrajectories)
            {
                Settings.fetch.DisplayCompleteTrajectory = GUILayout.Toggle(Settings.fetch.DisplayCompleteTrajectory, "complete", GUILayout.Width(70));
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Max G-force: " + (traj.MaxAccel / 9.81).ToString("0.00"));
            if (lastPatch != null && lastPatch.impactPosition.HasValue)
            {
                Vector3 up = lastPatch.impactPosition.Value.normalized;
                Vector3 vel = lastPatch.impactVelocity.Value - lastPatchBody.getRFrmVel(lastPatch.impactPosition.Value + lastPatchBody.position);
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

                float targetDistance = float.NaN;
                float targetDistanceNorth = float.NaN;
                float targetDistanceEast = float.NaN;
                if (lastPatchBody != null && targetBody != null && lastPatch.impactPosition.HasValue
                    && lastPatchBody == targetBody && traj.targetPosition.HasValue)
                {
                    // Set Vector3d (required by CelestialBody.GetLanLonAlt) coordinates by impactPosition
                    // impactPosition is in Body-relative World frame, but CelestialBody.GetLanLonAlt needs the absolute world frame.
                    impactPos.x = lastPatch.impactPosition.Value.x + lastPatchBody.position.x;
                    impactPos.y = lastPatch.impactPosition.Value.y + lastPatchBody.position.y;
                    impactPos.z = lastPatch.impactPosition.Value.z + lastPatchBody.position.z;

                    double impactLat, impatLon, impactAlt;
                    lastPatchBody.GetLatLonAlt(impactPos, out impactLat, out impatLon, out impactAlt);

                    targetPos.x = traj.targetPosition.Value.x + targetBody.position.x;
                    targetPos.y = traj.targetPosition.Value.y + targetBody.position.y;
                    targetPos.z = traj.targetPosition.Value.z + targetBody.position.z;

                    double targetLat, targetLon, targetAlt;
                    targetBody.GetLatLonAlt(targetPos, out targetLat, out targetLon, out targetAlt);

                    targetDistance = (float) (Util.distanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        impactLat, impatLon, targetLat, targetLon) / 1e3);

                    targetDistanceNorth = (float)(Util.distanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        impactLat, targetLon, targetLat, targetLon) / 1e3)* ((targetLat - impactLat) < 0 ? -1.0f : +1.0f);

                    targetDistanceEast = (float)(Util.distanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        targetLat, impatLon, targetLat, targetLon) / 1e3) * ((targetLon - impatLon) < 0 ? -1.0f : +1.0f);
                }

                GUILayout.Label("D: " + targetDistance.ToString("0.0") + "km"
                    + " /  N: " + targetDistanceNorth.ToString("0.0") + "km"
                    +" / E: " + targetDistanceEast.ToString("0.0") + "km");

                if (GUILayout.Button("Unset target"))
                    traj.SetTarget();
                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                var patch = traj.patches.LastOrDefault();
                GUI.enabled = (patch != null && patch.impactPosition.HasValue);
                if (GUILayout.Button("Set current impact", GUILayout.Width(150)))
                {
                    traj.SetTarget(patch.startingState.referenceBody, patch.impactPosition);
                }
                GUI.enabled = true;
                if (GUILayout.Button("Set KSC", GUILayout.Width(70)))
                {
                    var body = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);
                    if (body != null)
                    {
                        Vector3d worldPos = body.GetWorldSurfacePosition(-0.04860002, -74.72425635, 2.0);
                        traj.SetTarget(body, worldPos - body.position);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                Vessel targetVessel = FlightGlobals.fetch.VesselTarget?.GetVessel();
                GUI.enabled = (patch != null && targetVessel != null && targetVessel.Landed
                    // && targetVessel.lastBody == patch.startingState.referenceBody
                );
                if (GUILayout.Button("Target vessel"))
                {
                    traj.SetTarget(targetVessel.lastBody, targetVessel.GetWorldPos3D() - targetVessel.lastBody.position);
                    ScreenMessages.PostScreenMessage("Targeting vessel " + targetVessel.GetName());
                }

                FinePrint.Waypoint navigationWaypoint = FlightGlobals.ActiveVessel?.navigationWaypoint;
                GUI.enabled = (navigationWaypoint != null);
                if (GUILayout.Button("Active waypoint"))
                {
                    traj.SetTarget(navigationWaypoint.celestialBody,
                        navigationWaypoint.celestialBody.GetRelSurfacePosition(navigationWaypoint.latitude, navigationWaypoint.longitude, navigationWaypoint.altitude));
                    ScreenMessages.PostScreenMessage("Targeting waypoint " + navigationWaypoint.name);
                }
                GUILayout.EndHorizontal();

                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                coords = GUILayout.TextField(coords, GUILayout.Width(170));
                if (GUILayout.Button(new GUIContent("Set", "Enter target latitude and longitude, separated by a comma, in decimal format (with a dot for decimal separator)"), GUILayout.Width(50)))
                {
                    string[] latLng = coords.Split(new char[] { ',', ';' });
                    var body = FlightGlobals.currentMainBody;
                    if(latLng.Length == 2 && body != null)
                    {
                        double lat, lng;
                        if(Double.TryParse(latLng[0].Trim(), out lat) && Double.TryParse(latLng[1].Trim(), out lng))
                        {
                            Vector3d relPos = body.GetWorldSurfacePosition(lat, lng, 2.0) - body.position;
                            double altitude = Trajectory.GetGroundAltitude(body, relPos) + body.Radius;
                            traj.SetTarget(body, relPos * (altitude / relPos.magnitude));
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            bool descentProfileGroup = Settings.fetch.DisplayDescentProfileGUI = ToggleGroup(Settings.fetch.DisplayDescentProfileGUI, "Descent profile", 120);
            DescentProfile.fetch.DoQuickControlsGUI();
            GUILayout.EndHorizontal();
            if (descentProfileGroup)
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
                Settings.fetch.UseCache = GUILayout.Toggle(Settings.fetch.UseCache, new GUIContent("Use Cache", "Toggle cache usage. Trajectory will be more precise when cache disabled, but computation time will be higher. It's not recommended to keep it unchecked, unless your CPU can handle the load."), GUILayout.Width(80));
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

                if(FlightGlobals.ActiveVessel != null)
                {
                    GUILayout.Label("Position:");
                    GUILayout.BeginHorizontal();
                    var body = FlightGlobals.ActiveVessel.mainBody;
                    var worldPos = FlightGlobals.ActiveVessel.GetWorldPos3D();
                    GUILayout.Label("lat=" + body.GetLatitude(worldPos).ToString("000.000000"), GUILayout.Width(110));
                    GUILayout.Label("lng=" + body.GetLongitude(worldPos).ToString("000.000000"), GUILayout.Width(110));
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("Aerodynamic model: " + traj.AerodynamicModelName);
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
            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;

            if (ToolbarManager.ToolbarAvailable && Settings.fetch.UseBlizzyToolbar)
            {
                Debug.Log("Using Blizzy toolbar for Trajectories GUI");
                GUIToggleButtonBlizzy = ToolbarManager.Instance.add("Trajectories", "ToggleUI");
                GUIToggleButtonBlizzy.Visibility = GUIToggleButtonBlizzyVisibility.Instance;
                GUIToggleButtonBlizzy.TexturePath = "Trajectories/Textures/icon-blizzy1";
                GUIToggleButtonBlizzy.ToolTip = "Right click toggles Trajectories window";
                GUIToggleButtonBlizzy.OnClick += OnToggleGUIBlizzy;
            }
            else
            {
                Debug.Log("Using stock toolbar for Trajectories GUI");
                GameEvents.onGUIApplicationLauncherReady.Add(CreateStockToolbarButton);
                GameEvents.onGUIApplicationLauncherUnreadifying.Add(DestroyStockToolbarButton);
            }
        }

        void DummyVoid() { }

        void DestroyStockToolbarButton(GameScenes scene)
        {
            if (GUIToggleButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(GUIToggleButton);
                GUIToggleButton = null;
            }
        }

        void CreateStockToolbarButton()
        {
            if (GUIToggleButton == null)
            {
                GUIToggleButton = ApplicationLauncher.Instance.AddModApplication(
                    OnToggleOn,
                    OnToggleOff,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.FLIGHT,
                    (Texture)GameDatabase.Instance.GetTexture("Trajectories/Textures/icon", false));

                if(Settings.fetch.GUIEnabled)
                    GUIToggleButton.SetTrue(false);
            }
        }

        /// <summary>
        /// Determines if the current game scane is valid for the plugin.
        /// This plugin should be able to run in VAB/SPH, Flight, Space Center, and Tracking Station scenes.
        /// </summary>
        /// <returns>True if valid; false if not valid.</returns>
        Boolean IsValidScene()
        {
            return HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION;
        }

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
            if (GUIToggleButtonBlizzy != null)
                GUIToggleButtonBlizzy.Destroy();
        }

        private static Rect ClampToScreen(Rect window)
        {
            return new Rect(Mathf.Clamp(window.x, 0, Screen.width - window.width), Mathf.Clamp(window.y, 0, Screen.height - window.height), window.width, window.height);
        }
    }
}
