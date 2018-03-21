/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2014-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2018 S.Gray, (aka PiezPiedPy).

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

using KSP.UI.Screens;
using System;
using System.Linq;
using UnityEngine;

namespace Trajectories
{
    [Obsolete("use MainGUI")]
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class MapGUI: MonoBehaviour
    {
        private static readonly int GUIId = 934824;

        private string tooltip = String.Empty;

        private string coords = "";

        private Vector3d impactPos = new Vector3d();
        private Vector3d targetPos = new Vector3d();

        // click through locks
        private bool clickThroughLocked = false;
        private const ControlTypes FlightLockTypes = ControlTypes.MANNODE_ADDEDIT | ControlTypes.MANNODE_DELETE | ControlTypes.MAP_UI |
            ControlTypes.TARGETING | ControlTypes.VESSEL_SWITCHING | ControlTypes.TWEAKABLES;

        public void OnGUI()
        {
            if (!Settings.fetch.GUIEnabled || !Util.IsFlight || PlanetariumCamera.Camera == null)
                return;

            Settings.fetch.MapGUIWindowPos = new Rect(Settings.fetch.MapGUIWindowPos.xMin, Settings.fetch.MapGUIWindowPos.yMin, Settings.fetch.MapGUIWindowPos.width, Settings.fetch.MapGUIWindowPos.height - 3);
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

        private string guistring_gForce = "";
        private string guistring_impactVelocity = "";

        private string guistring_targetDistance = "";

        string guistring_Latitude = "";
        string guistring_Longitude = "";

        float lastStringRenderTime = 0.0f;
        const float stringRenderInterval = 0.5f;

        private void FixedUpdate()
        {
            if (Settings.fetch.NewGui)
                return;

            float t = Time.realtimeSinceStartup;
            if (t < lastStringRenderTime + stringRenderInterval)
                return;

            lastStringRenderTime = t;

            Trajectory traj = Trajectory.fetch;
            Trajectory.Patch lastPatch = traj.Patches.LastOrDefault();
            CelestialBody lastPatchBody = lastPatch?.StartingState.ReferenceBody;
            CelestialBody targetBody = Trajectory.Target.Body;

            guistring_gForce = (traj.MaxAccel / 9.81).ToString("0.00");

            if (lastPatch != null && lastPatch.ImpactPosition.HasValue)
            {
                Vector3 up = lastPatch.ImpactPosition.Value.normalized;
                Vector3 vel = lastPatch.ImpactVelocity.Value - lastPatchBody.getRFrmVel(lastPatch.ImpactPosition.Value + lastPatchBody.position);
                float vVelMag = Vector3.Dot(vel, up);
                Vector3 vVel = up * vVelMag;
                float hVelMag = (vel - vVel).magnitude;

                guistring_impactVelocity = String.Format("Impact: V = {0,6:F0}m/s, H = {1,6:F0}m/s", -vVelMag, hVelMag);
            }
            else
            {
                guistring_impactVelocity = "";
            }

            if (Settings.fetch.DisplayTargetGUI)
            {
                if (lastPatchBody != null && targetBody != null && lastPatch.ImpactPosition.HasValue
                    && lastPatchBody == targetBody && Trajectory.Target.WorldPosition.HasValue)
                {
                    // Get Latitude and Longitude from impact position
                    double impactLat;
                    double impatLon;
                    double impactAlt;

                    // Get Latitude and Longitude from impact position
                    impactPos = lastPatch.ImpactPosition.Value + lastPatchBody.position;
                    lastPatchBody.GetLatLonAlt(impactPos, out impactLat, out impatLon, out impactAlt);

                    // Get Latitude and Longitude for target position
                    double targetLat;
                    double targetLon;
                    double targetAlt;
                    targetPos = Trajectory.Target.WorldPosition.Value + targetBody.position;
                    targetBody.GetLatLonAlt(targetPos, out targetLat, out targetLon, out targetAlt);

                    float targetDistance = (float)(Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        impactLat, impatLon, targetLat, targetLon) / 1e3);

                    float targetDistanceNorth = (float)(Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        impactLat, targetLon, targetLat, targetLon) / 1e3) * ((targetLat - impactLat) < 0 ? -1.0f : +1.0f);

                    float targetDistanceEast = (float)(Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        targetLat, impatLon, targetLat, targetLon) / 1e3) * ((targetLon - impatLon) < 0 ? -1.0f : +1.0f);

                    // format distance to target string
                    guistring_targetDistance = String.Format("{0,6:F1}km | {1}: {2,6:F1}km | {3}: {4,6:F1}km",
                        targetDistance,
                        targetDistanceNorth > 0.0f ? 'N' : 'S',
                        Math.Abs(targetDistanceNorth),
                        targetDistanceEast > 0.0f ? 'E' : 'W',
                        Math.Abs(targetDistanceEast));
                }
                else
                {
                    guistring_targetDistance = "";
                }
            }

            if (FlightGlobals.ActiveVessel != null)
            {
                var body = FlightGlobals.ActiveVessel.mainBody;

                guistring_Latitude = body.GetLatitude(FlightGlobals.ActiveVessel.GetWorldPos3D()).ToString("000.000000");
                guistring_Longitude = body.GetLongitude(FlightGlobals.ActiveVessel.GetWorldPos3D()).ToString("000.000000");
            }
        }

        private void MainWindow(int id)
        {
            Trajectory traj = Trajectory.fetch;

            GUILayout.BeginHorizontal();

            Settings.fetch.DisplayTrajectories = GUILayout.Toggle(Settings.fetch.DisplayTrajectories, "Show trajectory", GUILayout.Width(125));

            Settings.fetch.DisplayTrajectoriesInFlight = GUILayout.Toggle(Settings.fetch.DisplayTrajectoriesInFlight, "In-Flight");

            // check that we have patched conics. If not, apologize to the user and return.
            if (Settings.fetch.DisplayTrajectories && !Util.IsPatchedConicsAvailable)
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

            GUILayout.Label("Max G-force: " + guistring_gForce);

            GUILayout.Label(guistring_impactVelocity);
            GUILayout.Space(10);


            if (Settings.fetch.DisplayTargetGUI = ToggleGroup(Settings.fetch.DisplayTargetGUI, "Target"))
            {
                GUI.enabled = Trajectory.Target.WorldPosition.HasValue;

                GUILayout.Label(guistring_targetDistance);

                if (GUILayout.Button("Unset target"))
                    Trajectory.Target.Set();
                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                var patch = traj.Patches.LastOrDefault();
                GUI.enabled = (patch != null && patch.ImpactPosition.HasValue);
                if (GUILayout.Button("Set current impact", GUILayout.Width(150)))
                {
                    Trajectory.Target.Set(patch.StartingState.ReferenceBody, patch.ImpactPosition);
                }
                GUI.enabled = true;
                if (GUILayout.Button("Set KSC", GUILayout.Width(70)))
                {
                    var body = FlightGlobals.Bodies.SingleOrDefault(b => b.isHomeWorld);
                    if (body != null)
                    {
                        Vector3d worldPos = body.GetWorldSurfacePosition(-0.04860002, -74.72425635, 2.0);
                        Trajectory.Target.Set(body, worldPos - body.position);
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
                    Trajectory.Target.Set(targetVessel.lastBody, targetVessel.GetWorldPos3D() - targetVessel.lastBody.position);
                    ScreenMessages.PostScreenMessage("Targeting vessel " + targetVessel.GetName());
                }

                FinePrint.Waypoint navigationWaypoint = FlightGlobals.ActiveVessel?.navigationWaypoint;
                GUI.enabled = (navigationWaypoint != null);
                if (GUILayout.Button("Active waypoint"))
                {
                    Trajectory.Target.Set(navigationWaypoint.celestialBody,navigationWaypoint.celestialBody.
                        GetWorldSurfacePosition(navigationWaypoint.latitude, navigationWaypoint.longitude,navigationWaypoint.altitude)
                        - navigationWaypoint.celestialBody.position);
                    ScreenMessages.PostScreenMessage("Targeting waypoint " + navigationWaypoint.name);
                }
                GUILayout.EndHorizontal();

                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                coords = GUILayout.TextField(Trajectory.Target.ManualText, GUILayout.Width(170));
                if (coords != Trajectory.Target.ManualText)
                {
                    Trajectory.Target.ManualText = coords;
                    Trajectory.Target.Save();
                }
                if (GUILayout.Button(new GUIContent("Set",
                        "Enter target latitude and longitude, separated by a comma, in decimal format (with a dot for decimal separator)"),
                        GUILayout.Width(50)))
                {
                    string[] latLng = coords.Split(new char[] { ',', ';' });
                    var body = FlightGlobals.currentMainBody;
                    if (latLng.Length == 2 && body != null)
                    {
                        double lat;
                        double lng;
                        if (double.TryParse(latLng[0].Trim(), out lat) && double.TryParse(latLng[1].Trim(), out lng))
                        {
                            Vector3d relPos = body.GetWorldSurfacePosition(lat, lng, 2.0) - body.position;
                            double altitude = Trajectory.GetGroundAltitude(body, relPos) + body.Radius;
                            Trajectory.Target.Set(body, relPos * (altitude / relPos.magnitude));
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

                if (FlightGlobals.ActiveVessel != null)
                {
                    GUILayout.Label("Position:");
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("lat=" + guistring_Latitude, GUILayout.Width(110));
                    GUILayout.Label("lng=" + guistring_Longitude, GUILayout.Width(110));
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("Aerodynamic model: " + traj.AerodynamicModelName);
                GUILayout.BeginHorizontal();
                GUILayout.Label(String.Format("Perf: {0,5:F1}ms ({1,4:F1})%",
                        traj.ComputationTime * 1000.0f,
                        traj.ComputationTime / traj.GameFrameTime * 100.0f
                    ), GUILayout.Width(130));
                GUILayout.Label(traj.ErrorCount + " error(s)", GUILayout.Width(80));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(Settings.fetch.NewGui, new GUIContent("New Gui", "Swap to the New Gui")))
                {
                    Settings.fetch.NewGui = true;
                    Settings.fetch.MainGUIEnabled = true;
                    Settings.fetch.GUIEnabled = false;
                    InputLockManager.RemoveControlLock("TrajectoriesFlightLock");
                    clickThroughLocked = false;
                }
                else
                {
                    Settings.fetch.NewGui = false;
                    Settings.fetch.MainGUIEnabled = false;
                    Settings.fetch.GUIEnabled = true;
                }
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

        /// <summary>
        /// Determines if the current game scane is valid for the plugin.
        /// This plugin should be able to run in VAB/SPH, Flight, Space Center, and Tracking Station scenes.
        /// </summary>
        /// <returns>True if valid; false if not valid.</returns>
        Boolean IsValidScene()
        {
            return HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION;
        }

        private static Rect ClampToScreen(Rect window)
        {
            return new Rect(Mathf.Clamp(window.x, 0, Screen.width - window.width), Mathf.Clamp(window.y, 0, Screen.height - window.height), window.width, window.height);
        }
    }
}
