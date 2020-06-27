/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2014-2018 A.Korsunsky, (aka fat-lobyte).
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
using System.Linq;
using UnityEngine;

namespace Trajectories
{
    [Obsolete("use MainGUI")]
    internal static class OldGUI
    {
        private static readonly int GUIId = 934824;

        private static string tooltip = string.Empty;

        private static string coords = "";

        private static Vector3d impactPos = new Vector3d();
        private static Vector3d targetPos = new Vector3d();

        // click through locks
        private static bool clickThroughLocked = false;
        private const ControlTypes FlightLockTypes = ControlTypes.MANNODE_ADDEDIT | ControlTypes.MANNODE_DELETE | ControlTypes.MAP_UI |
            ControlTypes.TARGETING | ControlTypes.VESSEL_SWITCHING | ControlTypes.TWEAKABLES;

        internal static void OnGUI()
        {
            if (!Settings.GUIEnabled || !Util.IsFlight || PlanetariumCamera.Camera == null)
                return;

            Settings.MapGUIWindowPos = new Rect(Settings.MapGUIWindowPos.xMin, Settings.MapGUIWindowPos.yMin, Settings.MapGUIWindowPos.width, Settings.MapGUIWindowPos.height - 3);
            Settings.MapGUIWindowPos = ClampToScreen(GUILayout.Window(GUIId + 1, Settings.MapGUIWindowPos, MainWindow, "Trajectories"));

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
            bool mouse_over = Settings.MapGUIWindowPos.Contains(Event.current.mousePosition);
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

        private static bool ToggleGroup(bool visible, string label, int? width = null)
        {
            //return GUILayout.Toggle(visible, label);
            TextAnchor oldAlignement = GUI.skin.button.alignment;
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

        private static string guistring_gForce = "";
        private static string guistring_impactVelocity = "";

        private static string guistring_targetDistance = "";
        private static string guistring_Latitude = "";
        private static string guistring_Longitude = "";
        private static float lastStringRenderTime = 0.0f;
        private const float stringRenderInterval = 0.5f;

        internal static void Update()
        {
            if (Settings.NewGui)
                return;

            float t = Time.realtimeSinceStartup;
            if (t < lastStringRenderTime + stringRenderInterval)
                return;

            lastStringRenderTime = t;

            Trajectory.Patch lastPatch = Trajectory.Patches.LastOrDefault();
            CelestialBody lastPatchBody = lastPatch?.StartingState.ReferenceBody;
            CelestialBody targetBody = TargetProfile.Body;

            guistring_gForce = (Trajectory.MaxAccel / 9.81).ToString("0.00");

            if (lastPatch != null && lastPatch.ImpactPosition.HasValue)
            {
                Vector3 up = lastPatch.ImpactPosition.Value.normalized;
                Vector3 vel = lastPatch.ImpactVelocity.Value - lastPatchBody.getRFrmVel(lastPatch.ImpactPosition.Value + lastPatchBody.position);
                float vVelMag = Vector3.Dot(vel, up);
                Vector3 vVel = up * vVelMag;
                float hVelMag = (vel - vVel).magnitude;

                guistring_impactVelocity = string.Format("Impact: V = {0,6:F0}m/s, H = {1,6:F0}m/s", -vVelMag, hVelMag);
            }
            else
            {
                guistring_impactVelocity = "";
            }

            if (Settings.DisplayTargetGUI)
            {
                if (lastPatchBody != null && targetBody != null && lastPatch.ImpactPosition.HasValue
                    && lastPatchBody == targetBody && TargetProfile.WorldPosition.HasValue)
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
                    targetPos = TargetProfile.WorldPosition.Value + targetBody.position;
                    targetBody.GetLatLonAlt(targetPos, out targetLat, out targetLon, out targetAlt);

                    float targetDistance = (float)(Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        impactLat, impatLon, targetLat, targetLon) / 1e3);

                    float targetDistanceNorth = (float)(Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        impactLat, targetLon, targetLat, targetLon) / 1e3) * ((targetLat - impactLat) < 0 ? -1.0f : +1.0f);

                    float targetDistanceEast = (float)(Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        targetLat, impatLon, targetLat, targetLon) / 1e3) * ((targetLon - impatLon) < 0 ? -1.0f : +1.0f);

                    // format distance to target string
                    guistring_targetDistance = string.Format("{0,6:F1}km | {1}: {2,6:F1}km | {3}: {4,6:F1}km",
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

            if (Trajectories.IsVesselAttached)
            {
                CelestialBody body = Trajectories.AttachedVessel.mainBody;

                guistring_Latitude = body.GetLatitude(Trajectories.AttachedVessel.GetWorldPos3D()).ToString("000.000000");
                guistring_Longitude = body.GetLongitude(Trajectories.AttachedVessel.GetWorldPos3D()).ToString("000.000000");
            }
        }

        private static void MainWindow(int id)
        {
            GUILayout.BeginHorizontal();

            Settings.DisplayTrajectories = GUILayout.Toggle(Settings.DisplayTrajectories, "Show trajectory", GUILayout.Width(125));

            Settings.DisplayTrajectoriesInFlight = GUILayout.Toggle(Settings.DisplayTrajectoriesInFlight, "In-Flight");

            // check that we have patched conics. If not, apologize to the user and return.
            if (Settings.DisplayTrajectories && !Util.IsPatchedConicsAvailable)
            {
                ScreenMessages.PostScreenMessage(
                    "Can't show trajectory because patched conics are not available." +
                    " Please update your tracking station facility.");
                Settings.DisplayTrajectories = false;
                return;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            Settings.BodyFixedMode = GUILayout.Toggle(Settings.BodyFixedMode, "Body-fixed mode");

            if (Settings.DisplayTrajectories)
            {
                Settings.DisplayCompleteTrajectory = GUILayout.Toggle(Settings.DisplayCompleteTrajectory, "complete", GUILayout.Width(70));
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Max G-force: " + guistring_gForce);

            GUILayout.Label(guistring_impactVelocity);
            GUILayout.Space(10);


            if (Settings.DisplayTargetGUI = ToggleGroup(Settings.DisplayTargetGUI, "Target"))
            {
                GUI.enabled = TargetProfile.WorldPosition.HasValue;

                GUILayout.Label(guistring_targetDistance);

                if (GUILayout.Button("Unset target"))
                    TargetProfile.Clear();
                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                Trajectory.Patch patch = Trajectory.Patches.LastOrDefault();
                GUI.enabled = (patch != null && patch.ImpactPosition.HasValue);
                if (GUILayout.Button("Set current impact", GUILayout.Width(150)))
                {
                    if (patch.ImpactPosition.HasValue)
                        TargetProfile.SetFromWorldPos(patch.StartingState.ReferenceBody, patch.ImpactPosition.Value);
                }
                GUI.enabled = true;
                if (GUILayout.Button("Set KSC", GUILayout.Width(70)))
                {
                    CelestialBody homebody = FlightGlobals.GetHomeBody();

                    double latitude = SpaceCenter.Instance.Latitude;
                    double longitude = SpaceCenter.Instance.Longitude;

                    if (homebody != null)
                        TargetProfile.SetFromLatLonAlt(homebody, latitude, longitude);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                Vessel targetVessel = FlightGlobals.fetch.VesselTarget?.GetVessel();
                GUI.enabled = (patch != null && targetVessel != null && targetVessel.Landed
                // && targetVessel.lastBody == patch.startingState.referenceBody
                );
                if (GUILayout.Button("Target vessel"))
                {
                    TargetProfile.SetFromWorldPos(targetVessel.lastBody, targetVessel.GetWorldPos3D() - targetVessel.lastBody.position);
                    ScreenMessages.PostScreenMessage("Targeting vessel " + targetVessel.GetName());
                }

                FinePrint.Waypoint navigationWaypoint = Trajectories.AttachedVessel?.navigationWaypoint;
                GUI.enabled = (navigationWaypoint != null);
                if (GUILayout.Button("Active waypoint"))
                {
                    TargetProfile.SetFromLatLonAlt(navigationWaypoint.celestialBody,
                        navigationWaypoint.latitude, navigationWaypoint.longitude, navigationWaypoint.altitude);
                    ScreenMessages.PostScreenMessage("Targeting waypoint " + navigationWaypoint.name);
                }
                GUILayout.EndHorizontal();

                GUI.enabled = true;

                GUILayout.BeginHorizontal();
                coords = GUILayout.TextField(TargetProfile.ManualText, GUILayout.Width(170));
                if (coords != TargetProfile.ManualText)
                {
                    TargetProfile.ManualText = coords;
                    TargetProfile.Save();
                }
                if (GUILayout.Button(new GUIContent("Set",
                        "Enter target latitude and longitude, separated by a comma, in decimal format (with a dot for decimal separator)"),
                        GUILayout.Width(50)))
                {
                    string[] latLng = coords.Split(new char[] { ',', ';' });
                    CelestialBody body = FlightGlobals.currentMainBody;
                    if (latLng.Length == 2 && body != null)
                    {
                        double lat;
                        double lng;
                        if (double.TryParse(latLng[0].Trim(), out lat) && double.TryParse(latLng[1].Trim(), out lng))
                        {
                            TargetProfile.SetFromLatLonAlt(body, lat, lng);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            bool descentProfileGroup = Settings.DisplayDescentProfileGUI = ToggleGroup(Settings.DisplayDescentProfileGUI, "Descent profile", 120);
            DescentProfile.DoQuickControlsGUI();
            GUILayout.EndHorizontal();
            if (descentProfileGroup)
            {
                DescentProfile.DoGUI();
            }
            GUILayout.Space(10);

            if (Settings.DisplaySettingsGUI = ToggleGroup(Settings.DisplaySettingsGUI, "Settings"))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Max patches", GUILayout.Width(100));
                Settings.MaxPatchCount = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)Settings.MaxPatchCount, 3, 10, GUILayout.Width(100)));
                GUILayout.Label(Settings.MaxPatchCount.ToString(), GUILayout.Width(15));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Max frames per patch", GUILayout.Width(100));
                Settings.MaxFramesPerPatch = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)Settings.MaxFramesPerPatch, 1, 50, GUILayout.Width(100)));
                GUILayout.Label(Settings.MaxFramesPerPatch.ToString(), GUILayout.Width(15));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Settings.UseCache = GUILayout.Toggle(Settings.UseCache, new GUIContent("Use Cache", "Toggle cache usage. Trajectory will be more precise when cache disabled, but computation time will be higher. It's not recommended to keep it unchecked, unless your CPU can handle the load."), GUILayout.Width(80));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                Settings.AutoUpdateAerodynamicModel = GUILayout.Toggle(Settings.AutoUpdateAerodynamicModel, new GUIContent("Auto update", "Auto-update of the aerodynamic model. For example if a part is decoupled, the model needs to be updated. This is independent from trajectory update."));
                if (GUILayout.Button("Update now"))
                    Trajectory.InvalidateAerodynamicModel();
                GUILayout.EndHorizontal();

                if (ToolbarManager.ToolbarAvailable)
                {
                    Settings.UseBlizzyToolbar = GUILayout.Toggle(Settings.UseBlizzyToolbar, new GUIContent("Use Blizzy's toolbar", "Will take effect after restart"));
                }

                if (Trajectories.IsVesselAttached)
                {
                    GUILayout.Label("Position:");
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("lat=" + guistring_Latitude, GUILayout.Width(110));
                    GUILayout.Label("lng=" + guistring_Longitude, GUILayout.Width(110));
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("Aerodynamic model: " + Trajectory.AerodynamicModelName);
                GUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("Perf: {0,5:F1}ms ({1,4:F1})%",
                        Trajectory.ComputationTime * 1000.0f,
                        Trajectory.ComputationTime / Trajectory.GameFrameTime * 100.0f
                    ), GUILayout.Width(130));
                GUILayout.Label(Trajectory.ErrorCount + " error(s)", GUILayout.Width(80));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(Settings.NewGui, new GUIContent("New Gui", "Swap to the New Gui")))
                {
                    Settings.NewGui = true;
                    Settings.MainGUIEnabled = true;
                    Settings.GUIEnabled = false;
                    InputLockManager.RemoveControlLock("TrajectoriesFlightLock");
                    clickThroughLocked = false;
                }
                else
                {
                    Settings.NewGui = false;
                    Settings.MainGUIEnabled = false;
                    Settings.GUIEnabled = true;
                }
                GUILayout.EndHorizontal();
            }

            tooltip = GUI.tooltip;

            GUI.DragWindow();
        }

        private static void TooltipWindow(int id)
        {
            GUIStyle toolTipStyle = new GUIStyle(GUI.skin.label);
            toolTipStyle.hover = toolTipStyle.active = toolTipStyle.normal;
            toolTipStyle.normal.textColor = toolTipStyle.active.textColor = toolTipStyle.hover.textColor = toolTipStyle.focused.textColor = toolTipStyle.onNormal.textColor = toolTipStyle.onHover.textColor = toolTipStyle.onActive.textColor = toolTipStyle.onFocused.textColor = new Color(1, 0.75f, 0);
            toolTipStyle.wordWrap = true;
            GUILayout.Label(tooltip, toolTipStyle);
        }

        private static Rect ClampToScreen(Rect window) => new Rect(Mathf.Clamp(window.x, 0, Screen.width - window.width), Mathf.Clamp(window.y, 0, Screen.height - window.height), window.width, window.height);
    }
}
