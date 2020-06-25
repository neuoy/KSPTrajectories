/*
  Copyright© (c) 2017-2020 S.Gray, (aka PiezPiedPy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).

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
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Trajectories
{
    /// <summary> MainGUI window handler. </summary>
    internal static class MainGUI
    {
        // constants
        private const float width = 370.0f;
        private const float height = 285.0f;
        private const float button_width = 75.0f;
        private const float button_height = 25.0f;
        private const float targetbutton_width = 120.0f;
        private const float descent_slider_width = 130.0f;
        private const float settings_slider_width = 195.0f;
        private const float integrator_slidermin = 1.0f;
        private const float integrator_slidermax = 50.0f;
        private const float lat_long_width = 28.0f;
        private const int page_padding = 10;

        // page type enum
        private enum PageType
        {
            INFO = 0,
            TARGET,
            DESCENT,
            SETTINGS
        }

        // spawned and visible flags
        private static int spawned = 0;
        private static bool visible = false;

        // popup window, page box and pages
        private static MultiOptionDialog multi_dialog;
        private static PopupDialog popup_dialog;
        private static DialogGUIBox page_box;

        private static DialogGUIVerticalLayout info_page;
        private static DialogGUIVerticalLayout target_page;
        private static DialogGUIVerticalLayout descent_page;
        private static DialogGUIVerticalLayout settings_page;

        private static UnityAction<string> keyboard_lockout_action;
        private static UnityAction<string> keyboard_unlock_action;

        // manual target text input
        private static string manual_target_txt = "";
        private static bool manual_target_txt_ok = false;
        private static bool manual_target_txt_changed = false;
        private static DialogGUITextInput manual_target_textinput;
        private static TMP_InputField tmpro_manual_target_textinput;

        // descent profile text inputs
        // entry text input
        private static string descent_entry_txt = "";
        private static DialogGUITextInput descent_entry_textinput;
        private static TMP_InputField tmpro_descent_entry_textinput;
        // high text input
        private static string descent_high_txt = "";
        private static DialogGUITextInput descent_high_textinput;
        private static TMP_InputField tmpro_descent_high_textinput;
        // low text input
        private static string descent_low_txt = "";
        private static DialogGUITextInput descent_low_textinput;
        private static TMP_InputField tmpro_descent_low_textinput;
        // ground text input
        private static string descent_final_txt = "";
        private static DialogGUITextInput descent_final_textinput;
        private static TMP_InputField tmpro_descent_ground_textinput;

        // data field labels
        private static DialogGUILabel impact_latitude_label;
        private static DialogGUILabel impact_longitude_label;
        private static DialogGUILabel impact_vertical_label;
        private static DialogGUILabel impact_horizontal_label;
        private static DialogGUILabel info_distance_label;
        private static DialogGUILabel info_distance_latitude_label;
        private static DialogGUILabel info_distance_longitude_label;
        private static DialogGUILabel target_latitude_label;
        private static DialogGUILabel target_longitude_label;
        private static DialogGUILabel target_distance_label;
        private static DialogGUILabel target_distance_latitude_label;
        private static DialogGUILabel target_distance_longitude_label;

        // display update strings
        private static readonly string trajectories_title = Localizer.Format("#autoLOC_Trajectories_Title");
        private static readonly string aerodynamic_model_hdrtxt = Localizer.Format("#autoLOC_Trajectories_AeroModel") + ": ";
        private static readonly string calculation_time_hdrtxt = Localizer.Format("#autoLOC_Trajectories_CalcTime") + ": ";
        private static readonly string errors_hdrtxt = Localizer.Format("#autoLOC_Trajectories_Errors") + ": ";

        private static string max_gforce_txt = "";
        private static string impact_latitude_txt = "";
        private static string impact_longitude_txt = "";
        private static string impact_vertical_txt = "";
        private static string impact_horizontal_txt = "";
        private static string impact_time_txt = "";
        private static string aerodynamic_model_txt = "";
        private static string calculation_time_txt = "";
        private static string num_errors_txt = "";
        private static string target_body_txt = "";
        private static string target_latitude_txt = "";
        private static string target_longitude_txt = "";
        private static string target_distance_txt = "";
        private static string target_distance_latitude_txt = "";
        private static string target_distance_longitude_txt = "";

        // integrator logarithmic slider
        private static float integrator_sliderPos;

        private static float IntegratorSliderPos
        {
            get => integrator_sliderPos;
            set
            {
                // logarithmic step from position
                double a = Math.Log(Trajectory.integrator_max / Trajectory.integrator_min) / (integrator_slidermax - integrator_slidermin);
                double b = Trajectory.integrator_max / Math.Exp(a * integrator_slidermax);
                double stepsize = b * Math.Exp(a * value);

                // round off step;
                if (stepsize < 0.25)
                    Settings.IntegrationStepSize = Math.Round(stepsize * 100, MidpointRounding.AwayFromZero) / 100;    // 0.01
                else if (stepsize < 0.5)
                    Settings.IntegrationStepSize = Math.Round(stepsize * 20, MidpointRounding.AwayFromZero) / 20;     // 0.05
                else if (stepsize < 1)
                    Settings.IntegrationStepSize = Math.Round(stepsize * 10, MidpointRounding.AwayFromZero) / 10;     // 0.1
                else if (stepsize < 2)
                    Settings.IntegrationStepSize = Math.Round(stepsize * 4, MidpointRounding.AwayFromZero) / 4;       // 0.25
                else
                    Settings.IntegrationStepSize = Math.Round(stepsize * 2, MidpointRounding.AwayFromZero) / 2;       // 0.5

                // set slider pos
                integrator_sliderPos = (float)(integrator_slidermin + (Math.Log(Settings.IntegrationStepSize) -
                    Math.Log(Trajectory.integrator_min)) / (Math.Log(Trajectory.integrator_max) - Math.Log(Trajectory.integrator_min)) *
                    (integrator_slidermax - integrator_slidermin));
            }
        }

        // display update timer
        private static double update_timer = Util.Clocks;
        private const double update_fps = 10;  // Frames per second the data values displayed in the Gui will update.

        internal static string TrajectoriesTitle => trajectories_title;

        /// <summary>
        /// Allocates required resources
        /// </summary>
        internal static void Start()
        {
            Util.DebugLog(multi_dialog != null ? "Resetting" : "Constructing");
            // allocate and define window for use in the popup dialog
            if (multi_dialog == null)
                Allocate();

            // set page padding
            info_page.padding.left = page_padding;
            info_page.padding.right = page_padding;
            target_page.padding.left = page_padding;
            target_page.padding.right = page_padding;
            descent_page.padding.left = page_padding;
            descent_page.padding.right = page_padding;
            settings_page.padding.left = page_padding;
            settings_page.padding.right = page_padding;

            // create popup dialog and add onDestroy listener
            spawned = 0;
            SpawnDialog();

            //set data field labels justification
            SetDataFieldJustification();
        }

        /// <summary> Releases held resources. </summary>
        internal static void Destroy()
        {
            Util.DebugLog("");

            DeSpawn();
            multi_dialog = null;
        }

        internal static void Update()
        {
            // initialization for dialog box window position
            if (spawned != 2 && popup_dialog != null)
            {
                if (spawned == 0)
                {
                    popup_dialog.gameObject.SetActive(true);
                    spawned = 1;
                }
                else
                {
                    Hide();
                    spawned = 2;
                }
                return;
            }

            // keyboard unlock for manual target edit box
            if ((Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)) &&
                (InputLockManager.GetControlLock("TrajectoriesKeyboardLockout") == ControlTypes.KEYBOARDINPUT))
                KeyboardUnlock("");

            // hide or show the dialog box
            if ((!Settings.MainGUIEnabled || PlanetariumCamera.Camera == null) && visible)
            {
                Hide();
                return;
            }
            else if (Settings.MainGUIEnabled && (!visible || popup_dialog == null))
            {
                Show();
            }

            UpdatePages();
        }

        internal static void DeSpawn()
        {
            Util.DebugLog("");

            KeyboardUnlock("");

            if (popup_dialog != null)
                popup_dialog.Dismiss();

            popup_dialog = null;
        }

        private static void SpawnDialog()
        {
            if (multi_dialog != null)
            {
                if (ClampToScreen())
                    multi_dialog.dialogRect.Set(Settings.MainGUIWindowPos.x, Settings.MainGUIWindowPos.y, width, height);

                popup_dialog = PopupDialog.SpawnPopupDialog(multi_dialog, false, HighLogic.UISkin, false, "");
                popup_dialog.onDestroy.AddListener(new UnityAction(OnPopupDialogDestroy));

                // create text input box event listeners
                SetTextInputBoxEvents();
            }
        }

        /// <summary>
        /// Defaults window to center of screen and also ensures it remains within the screen bounds.
        /// Returns true if position is adjusted.
        /// </summary>
        private static bool ClampToScreen()
        {
            float border = 50f;
            bool adjusted = false;

            if (Settings.MainGUIWindowPos.x <= 0.0f || Settings.MainGUIWindowPos.y <= 0.0f)
            {
                // default window to center of screen
                Settings.MainGUIWindowPos = new Vector2(0.5f, 0.5f);
                adjusted = true;
            }
            else
            {
                // ensure window remains within the screen bounds
                Vector2 pos = new Vector2(((Settings.MainGUIWindowPos.x * Screen.width) - (Screen.width / 2)) * GameSettings.UI_SCALE,
                                          ((Settings.MainGUIWindowPos.y * Screen.height) - (Screen.height / 2)) * GameSettings.UI_SCALE);

                if (pos.x > (Screen.width / 2) - border)
                {
                    pos.x = (Screen.width / 2) - (border + (width / 2));
                    adjusted = true;
                }
                else if (pos.x < ((Screen.width / 2) - border) * -1f)
                {
                    pos.x = ((Screen.width / 2) - (border + (width / 2))) * -1f;
                    adjusted = true;
                }

                if (pos.y > (Screen.height / 2) - border)
                {
                    pos.y = (Screen.height / 2) - (border + (height / 2));
                    adjusted = true;
                }
                else if (pos.y < ((Screen.height / 2) - border) * -1f)
                {
                    pos.y = ((Screen.height / 2) - (border + (height / 2))) * -1f;
                    adjusted = true;
                }

                if (adjusted)
                {
                    Settings.MainGUIWindowPos = new Vector2(
                        ((Screen.width / 2) + (pos.x / GameSettings.UI_SCALE)) / Screen.width,
                        ((Screen.height / 2) + (pos.y / GameSettings.UI_SCALE)) / Screen.height);
                }
            }
            return adjusted;
        }

        /// <summary>
        /// Allocates any classes, variables etc needed for the MainGUI,
        ///   note that this method should be called from the constructor.
        /// </summary>
        private static void Allocate()
        {
            ClampToScreen();

            // create manual target text input box
            manual_target_textinput = new DialogGUITextInput(manual_target_txt, " ", false, 35, OnTextInput_TargetManual, 23);

            // create descent profile text input boxes
            descent_entry_textinput = new DialogGUITextInput(descent_entry_txt, " ", false, 6, OnTextInput_DescentEntry, 23);
            descent_high_textinput = new DialogGUITextInput(descent_high_txt, " ", false, 6, OnTextInput_DescentHigh, 23);
            descent_low_textinput = new DialogGUITextInput(descent_low_txt, " ", false, 6, OnTextInput_DescentLow, 23);
            descent_final_textinput = new DialogGUITextInput(descent_final_txt, " ", false, 6, OnTextInput_DescentFinal, 23);

            // create data field labels
            impact_latitude_label = new DialogGUILabel(() => { return impact_latitude_txt; }, 65);
            impact_longitude_label = new DialogGUILabel(() => { return impact_longitude_txt; }, 65);
            impact_vertical_label = new DialogGUILabel(() => { return impact_vertical_txt; }, 65);
            impact_horizontal_label = new DialogGUILabel(() => { return impact_horizontal_txt; }, 65);
            info_distance_label = new DialogGUILabel(() => { return Trajectory.Target.Body == null ? "" : target_distance_txt; }, 60);
            info_distance_latitude_label = new DialogGUILabel(() => { return Trajectory.Target.Body == null ? "" : target_distance_latitude_txt; }, 80);
            info_distance_longitude_label = new DialogGUILabel(() => { return Trajectory.Target.Body == null ? "" : target_distance_longitude_txt; }, 80);
            target_latitude_label = new DialogGUILabel(() => { return target_latitude_txt; }, 65);
            target_longitude_label = new DialogGUILabel(() => { return target_longitude_txt; }, 65);
            target_distance_label = new DialogGUILabel(() => { return target_distance_txt; }, 60);
            target_distance_latitude_label = new DialogGUILabel(() => { return target_distance_latitude_txt; }, 80);
            target_distance_longitude_label = new DialogGUILabel(() => { return target_distance_longitude_txt; }, 80);

            // set integrator slider pos;
            SetIntegratorSlider();

            // create pages
            info_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIHorizontalLayout(
                    new DialogGUIToggle(() => { return Util.IsPatchedConicsAvailable ? Settings.DisplayTrajectories : false; },
                        Localizer.Format("#autoLOC_Trajectories_ShowTrajectory"), OnButtonClick_DisplayTrajectories),
                    new DialogGUIToggle(() => { return Settings.DisplayTrajectoriesInFlight; },
                        Localizer.Format("#autoLOC_Trajectories_InFlight"), OnButtonClick_DisplayTrajectoriesInFlight)),
                new DialogGUIHorizontalLayout(
                    new DialogGUIToggle(() => { return Settings.BodyFixedMode; },
                        Localizer.Format("#autoLOC_Trajectories_FixedBody"), OnButtonClick_BodyFixedMode),
                    new DialogGUIToggle(() => { return Settings.DisplayCompleteTrajectory; },
                        Localizer.Format("#autoLOC_Trajectories_Complete"), OnButtonClick_DisplayCompleteTrajectory)),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_MaxGforce"), true),
                    new DialogGUILabel(() => { return max_gforce_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_ImpactTime"), true),
                    new DialogGUILabel(() => { return impact_time_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_ImpactPosition"), true),
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_Lat"), lat_long_width),
                    impact_latitude_label,
                    new DialogGUISpace(10),
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_Long"), lat_long_width),
                    impact_longitude_label),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_ImpactVelocity"), true),
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_Vert"), lat_long_width),
                    impact_vertical_label,
                    new DialogGUISpace(10),
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_Hori"), lat_long_width),
                    impact_horizontal_label),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(() => { return Trajectory.Target.Body == null ? "" :
                        Localizer.Format("#autoLOC_Trajectories_TargetDistance"); }, true),
                    info_distance_label,
                    new DialogGUISpace(2),
                    info_distance_latitude_label,
                    new DialogGUISpace(2),
                    info_distance_longitude_label)
                );

            target_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_TargetBody"), true),
                    new DialogGUILabel(() => { return target_body_txt; })),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_TargetPosition"), true),
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_Lat"), lat_long_width),
                    target_latitude_label,
                    new DialogGUISpace(10),
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_Long"), lat_long_width),
                    target_longitude_label),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleRight,
                    new DialogGUILabel(() => { return Localizer.Format("#autoLOC_Trajectories_TargetDistance"); }, true),
                    target_distance_label,
                    new DialogGUISpace(2),
                    target_distance_latitude_label,
                    new DialogGUISpace(2),
                    target_distance_longitude_label),
                new DialogGUISpace(4),
                new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_TargetSelect")),
                new DialogGUIHorizontalLayout(
                    new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_TargetImpact"),
                        OnButtonClick_TargetImpact, ButtonEnabler_TargetImpact, targetbutton_width, button_height, false),
                    new DialogGUISpace(10),
                    new DialogGUIButton(Localizer.Format("#autoLOC_6002159"),
                        OnButtonClick_TargetKSC, () => { return true; }, targetbutton_width, button_height, false)),
                new DialogGUIHorizontalLayout(
                    new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_TargetVessel"),
                        OnButtonClick_TargetVessel, ButtonEnabler_TargetVessel, targetbutton_width, button_height, false),
                    new DialogGUISpace(10),
                    new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_TargetWaypoint"),
                        OnButtonClick_TargetWaypoint, ButtonEnabler_TargetWaypoint, targetbutton_width, button_height, false)),
                new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_TargetHelp")),
                manual_target_textinput,
                new DialogGUISpace(2),
                new DialogGUIHorizontalLayout(
                    new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_TargetManual"),
                        OnButtonClick_TargetManual, ButtonEnabler_TargetManual, targetbutton_width, button_height, false),
                    new DialogGUISpace(10),
                    new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_TargetClear"),
                        OnButtonClick_TargetClear, ButtonEnabler_TargetClear, targetbutton_width, button_height, false)),
                new DialogGUISpace(2)
                );


            descent_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIHorizontalLayout(false, false, 0, new RectOffset(), TextAnchor.MiddleCenter,
                    new DialogGUIToggle(() => { return !DescentProfile.RetrogradeEntry; },
                        Localizer.Format("#autoLOC_900597"), OnButtonClick_Prograde),
                    new DialogGUIToggle(() => { return DescentProfile.RetrogradeEntry; },
                        Localizer.Format("#autoLOC_900607"), OnButtonClick_Retrograde)),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUILabel(DescentProfile.atmos_entry.Name, 45f),
                    new DialogGUIToggle(() => { return DescentProfile.atmos_entry.Horizon; },
                        () => { return DescentProfile.atmos_entry.Horizon_txt; }, OnButtonClick_EntryHorizon, 60f),
                    new DialogGUISlider(() => { return DescentProfile.atmos_entry.SliderPos; },
                        -1f, 1f, false, descent_slider_width, -1, OnSliderSet_EntryAngle),
                    new DialogGUILabel(() => { return DescentProfile.atmos_entry.Angle_txt; }, 30f),
                    descent_entry_textinput),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUILabel(DescentProfile.high_altitude.Name, 45f),
                    new DialogGUIToggle(() => { return DescentProfile.high_altitude.Horizon; },
                        () => { return DescentProfile.high_altitude.Horizon_txt; }, OnButtonClick_HighHorizon, 60f),
                    new DialogGUISlider(() => { return DescentProfile.high_altitude.SliderPos; },
                        -1f, 1f, false, descent_slider_width, -1, OnSliderSet_HighAngle),
                    new DialogGUILabel(() => { return DescentProfile.high_altitude.Angle_txt; }, 30f),
                    descent_high_textinput),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUILabel(DescentProfile.low_altitude.Name, 45f),
                    new DialogGUIToggle(() => { return DescentProfile.low_altitude.Horizon; },
                        () => { return DescentProfile.low_altitude.Horizon_txt; }, OnButtonClick_LowHorizon, 60f),
                    new DialogGUISlider(() => { return DescentProfile.low_altitude.SliderPos; },
                        -1f, 1f, false, descent_slider_width, -1, OnSliderSet_LowAngle),
                    new DialogGUILabel(() => { return DescentProfile.low_altitude.Angle_txt; }, 30f),
                    descent_low_textinput),
                new DialogGUIHorizontalLayout(TextAnchor.MiddleLeft,
                    new DialogGUILabel(DescentProfile.final_approach.Name, 45f),
                    new DialogGUIToggle(() => { return DescentProfile.final_approach.Horizon; },
                        () => { return DescentProfile.final_approach.Horizon_txt; }, OnButtonClick_FinalHorizon, 60f),
                    new DialogGUISlider(() => { return DescentProfile.final_approach.SliderPos; },
                        -1f, 1f, false, descent_slider_width, -1, OnSliderSet_GroundAngle),
                    new DialogGUILabel(() => { return DescentProfile.final_approach.Angle_txt; }, 30f),
                    descent_final_textinput)
                );

            settings_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIToggle(() => { return ToolbarManager.ToolbarAvailable ? Settings.UseBlizzyToolbar : false; },
                    Localizer.Format("#autoLOC_Trajectories_UseBlizzyToolbar"), OnButtonClick_UseBlizzyToolbar),
                new DialogGUIToggle(() => { return Settings.DefaultDescentIsRetro; },
                    Localizer.Format("#autoLOC_Trajectories_DefaultDescent"), OnButtonClick_UseDescentRetro),
                new DialogGUIHorizontalLayout(false, false, 0, new RectOffset(), TextAnchor.MiddleCenter,
                    new DialogGUIToggle(() => { return Settings.UseCache; },
                        Localizer.Format("#autoLOC_Trajectories_UseCache"), OnButtonClick_UseCache),
                    new DialogGUIToggle(() => { return Settings.AutoUpdateAerodynamicModel; },
                        Localizer.Format("#autoLOC_Trajectories_AutoUpdate"), OnButtonClick_AutoUpdateAerodynamicModel),
                    new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_Update"),
                        OnButtonClick_Update, button_width, button_height, false)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_MaxPatches"), true),
                    new DialogGUISlider(() => { return Settings.MaxPatchCount; },
                        3f, 10f, true, settings_slider_width + 10f, -1, OnSliderSet_MaxPatches),
                    new DialogGUILabel(() => { return Settings.MaxPatchCount.ToString(); }, 25f)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_MaxFramesPatch"), true),
                    new DialogGUISlider(() => { return Settings.MaxFramesPerPatch; },
                        1f, 50f, true, settings_slider_width + 10f, -1, OnSliderSet_MaxFramesPatch),
                    new DialogGUILabel(() => { return Settings.MaxFramesPerPatch.ToString(); }, 25f)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_IntegrationStep"), true),
                    new DialogGUISlider(() => { return IntegratorSliderPos; },
                        integrator_slidermin, integrator_slidermax, false, settings_slider_width + 10f, -1, OnSliderSet_IntegrationStep),
                    new DialogGUILabel(() => { return Settings.IntegrationStepSize.ToString("F2"); }, 25f)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(() => { return calculation_time_txt; }, true),
                    new DialogGUILabel(() => { return num_errors_txt; }, true)),
                new DialogGUIHorizontalLayout(true, false, 0, new RectOffset(), TextAnchor.MiddleCenter,
                    new DialogGUILabel(() => { return aerodynamic_model_txt; }, true),
                    new DialogGUIToggle(() => { return Settings.NewGui; },
                        Localizer.Format("#autoLOC_Trajectories_NewGui"), OnButtonClick_NewGui))
                );

            // create page box with current page inserted into page box
            switch ((PageType)Settings.MainGUICurrentPage)
            {
                case PageType.TARGET:
                    page_box = new DialogGUIBox(null, -1, -1, () => true, target_page);
                    break;
                case PageType.DESCENT:
                    page_box = new DialogGUIBox(null, -1, -1, () => true, descent_page);
                    break;
                case PageType.SETTINGS:
                    page_box = new DialogGUIBox(null, -1, -1, () => true, settings_page);
                    break;
                default:
                    page_box = new DialogGUIBox(null, -1, -1, () => true, info_page);
                    break;
            }

            // create base window for popup dialog
            multi_dialog = new MultiOptionDialog(
               "TrajectoriesMainGUI",
               "",
               Localizer.Format("#autoLOC_Trajectories_Title") + " - v" + Trajectories.Version,
               HighLogic.UISkin,
               // window origin is center of rect, position is offset from lower left corner of screen and normalized
               // i.e (0.5, 0.5 is screen center)
               new Rect(Settings.MainGUIWindowPos.x, Settings.MainGUIWindowPos.y, width, height),
               new DialogGUIBase[]
               {
                   // create page select buttons
                   new DialogGUIHorizontalLayout(true, false, 4, new RectOffset(), TextAnchor.MiddleCenter,
                       new DialogGUIButton(Localizer.Format("#autoLOC_900629"),
                           OnButtonClick_Info, ButtonEnabler_Info, button_width, button_height, false),
                       new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_Target"),
                           OnButtonClick_Target, ButtonEnabler_Target, button_width, button_height, false),
                       new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_Descent"),
                           OnButtonClick_Descent, ButtonEnabler_Descent, button_width, button_height, false),
                       new DialogGUIButton(Localizer.Format("#autoLOC_900734"),
                           OnButtonClick_Settings, ButtonEnabler_Settings, button_width, button_height, false)),
                   // insert page box
                   page_box
               });
        }

        /// <summary>
        /// Called when the PopupDialog OnDestroy method is called. Used for saving the MainGUI window position.
        /// </summary>
        private static void OnPopupDialogDestroy()
        {
            // save popup position. Note. PopupDialog.RTrf is an offset from the center of the screen.
            if (popup_dialog != null)
            {
                Settings.MainGUIWindowPos = new Vector2(
                    ((Screen.width / 2) + (popup_dialog.RTrf.position.x / GameSettings.UI_SCALE)) / Screen.width,
                    ((Screen.height / 2) + (popup_dialog.RTrf.position.y / GameSettings.UI_SCALE)) / Screen.height);
                //Util.DebugLog("Saving MainGUI window position as {0}", Settings.MainGUIWindowPos.ToString("F4"));
                multi_dialog.dialogRect.Set(Settings.MainGUIWindowPos.x, Settings.MainGUIWindowPos.y, width, height);
            }
            visible = false;
            Settings.Save();
        }

        /// <summary>
        /// Sets the justification of the data field labels
        /// </summary>
        private static void SetDataFieldJustification()
        {
            if (Settings.MainGUICurrentPage == (int)PageType.INFO && impact_latitude_label.text != null)
            {
                impact_latitude_label.text.alignment = TextAlignmentOptions.MidlineRight;
                impact_longitude_label.text.alignment = TextAlignmentOptions.MidlineRight;
                impact_vertical_label.text.alignment = TextAlignmentOptions.MidlineRight;
                impact_horizontal_label.text.alignment = TextAlignmentOptions.MidlineRight;
                info_distance_label.text.alignment = TextAlignmentOptions.MidlineRight;
                info_distance_latitude_label.text.alignment = TextAlignmentOptions.MidlineRight;
                info_distance_longitude_label.text.alignment = TextAlignmentOptions.MidlineRight;
            }
            else if (Settings.MainGUICurrentPage == (int)PageType.TARGET && target_latitude_label.text != null)
            {
                target_latitude_label.text.alignment = TextAlignmentOptions.MidlineRight;
                target_longitude_label.text.alignment = TextAlignmentOptions.MidlineRight;
                target_distance_label.text.alignment = TextAlignmentOptions.MidlineRight;
                target_distance_latitude_label.text.alignment = TextAlignmentOptions.MidlineRight;
                target_distance_longitude_label.text.alignment = TextAlignmentOptions.MidlineRight;
            }
        }

        /// <summary>
        /// Creates the event listeners for the text input boxes
        /// </summary>
        private static void SetTextInputBoxEvents()
        {
            keyboard_lockout_action = new UnityAction<string>(KeyboardLockout);
            keyboard_unlock_action = new UnityAction<string>(KeyboardUnlock);

            // manual target text input
            if (manual_target_textinput?.uiItem != null)
            {
                tmpro_manual_target_textinput = manual_target_textinput.uiItem.GetComponent<TMP_InputField>();
                tmpro_manual_target_textinput.onSelect.AddListener(keyboard_lockout_action);
                tmpro_manual_target_textinput.onDeselect.AddListener(keyboard_unlock_action);
                tmpro_manual_target_textinput.onEndEdit.AddListener(keyboard_unlock_action);
            }
            // descent profile text inputs
            if (descent_entry_textinput?.uiItem != null)
            {
                tmpro_descent_entry_textinput = descent_entry_textinput.uiItem.GetComponent<TMP_InputField>();
                tmpro_descent_entry_textinput.onSelect.AddListener(keyboard_lockout_action);
                tmpro_descent_entry_textinput.onDeselect.AddListener(keyboard_unlock_action);
                tmpro_descent_entry_textinput.onEndEdit.AddListener(keyboard_unlock_action);
            }
            if (descent_high_textinput?.uiItem != null)
            {
                tmpro_descent_high_textinput = descent_high_textinput.uiItem.GetComponent<TMP_InputField>();
                tmpro_descent_high_textinput.onSelect.AddListener(keyboard_lockout_action);
                tmpro_descent_high_textinput.onDeselect.AddListener(keyboard_unlock_action);
                tmpro_descent_high_textinput.onEndEdit.AddListener(keyboard_unlock_action);
            }
            if (descent_low_textinput?.uiItem != null)
            {
                tmpro_descent_low_textinput = descent_low_textinput.uiItem.GetComponent<TMP_InputField>();
                tmpro_descent_low_textinput.onSelect.AddListener(keyboard_lockout_action);
                tmpro_descent_low_textinput.onDeselect.AddListener(keyboard_unlock_action);
                tmpro_descent_low_textinput.onEndEdit.AddListener(keyboard_unlock_action);
            }
            if (descent_final_textinput?.uiItem != null)
            {
                tmpro_descent_ground_textinput = descent_final_textinput.uiItem.GetComponent<TMP_InputField>();
                tmpro_descent_ground_textinput.onSelect.AddListener(keyboard_lockout_action);
                tmpro_descent_ground_textinput.onDeselect.AddListener(keyboard_unlock_action);
                tmpro_descent_ground_textinput.onEndEdit.AddListener(keyboard_unlock_action);
            }
        }

        /// <summary>
        /// Locks out the keyboards input
        /// </summary>
        private static void KeyboardLockout(string inString) => InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, "TrajectoriesKeyboardLockout");

        /// <summary>
        /// Removes the keyboard lockout
        /// </summary>
        private static void KeyboardUnlock(string inString) => InputLockManager.RemoveControlLock("TrajectoriesKeyboardLockout");

        /// <summary>
        /// Sets the logarithmic slider position for the plug-in setting IntegrationStepSize
        /// </summary>
        private static void SetIntegratorSlider()
        {
            IntegratorSliderPos = (float)(integrator_slidermin + (Math.Log(Settings.IntegrationStepSize) -
                Math.Log(Trajectory.integrator_min)) / (Math.Log(Trajectory.integrator_max) - Math.Log(Trajectory.integrator_min)) *
                (integrator_slidermax - integrator_slidermin));
        }

        /// <summary> Shows window. </summary>
        public static void Show()
        {
            if (popup_dialog == null)
            {
                SpawnDialog();
            }
            visible = true;
            popup_dialog.gameObject.SetActive(true);
        }

        /// <summary> Hides window. </summary>
        public static void Hide()
        {
            if (popup_dialog != null)
            {
                visible = false;
                popup_dialog.gameObject.SetActive(false);
            }
        }

        #region ButtonEnabler methods called by the GuiButtons
        // ButtonEnabler methods are Callbacks used by the GuiButtons to decide if they should enable or disable themselves.
        //  A page select button will call its ButtonEnabler method which returns false if the currently viewed page matches the button
        private static bool ButtonEnabler_Info()
        {
            if ((PageType)Settings.MainGUICurrentPage == PageType.INFO)
                return false;
            return true;
        }

        private static bool ButtonEnabler_Target()
        {
            if ((PageType)Settings.MainGUICurrentPage == PageType.TARGET)
                return false;
            return true;
        }

        private static bool ButtonEnabler_Descent()
        {
            if ((PageType)Settings.MainGUICurrentPage == PageType.DESCENT)
                return false;
            return true;
        }

        private static bool ButtonEnabler_Settings()
        {
            if ((PageType)Settings.MainGUICurrentPage == PageType.SETTINGS)
                return false;
            return true;
        }

        private static bool ButtonEnabler_TargetImpact()
        {
            // grab the last patch that was calculated
            Trajectory.Patch lastPatch = Trajectory.Patches.LastOrDefault();

            if (lastPatch != null && lastPatch.ImpactPosition.HasValue)
                return true;
            return false;
        }

        private static bool ButtonEnabler_TargetVessel()
        {
            // grab the currently targeted vessel
            Vessel targetVessel = FlightGlobals.fetch.VesselTarget?.GetVessel();

            if (targetVessel != null && targetVessel.Landed)
                return true;
            return false;
        }

        private static bool ButtonEnabler_TargetWaypoint()
        {
            if (FlightGlobals.ActiveVessel?.navigationWaypoint != null)
                return true;
            return false;
        }

        private static bool ButtonEnabler_TargetManual()
        {
            if (manual_target_txt_ok)
                return true;
            return false;
        }

        private static bool ButtonEnabler_TargetClear()
        {
            if ((Trajectory.Target.Body != null) && Trajectory.Target.WorldPosition.HasValue)
                return true;
            return false;
        }
        #endregion

        #region  OnButtonClick methods called by the GuiButtons and Toggles
        private static void OnButtonClick_Info() => ChangePage(PageType.INFO);

        private static void OnButtonClick_Target() => ChangePage(PageType.TARGET);

        private static void OnButtonClick_Descent() => ChangePage(PageType.DESCENT);

        private static void OnButtonClick_Settings() => ChangePage(PageType.SETTINGS);

        private static void OnButtonClick_DisplayTrajectories(bool inState)
        {
            // check that we have patched conics. If not, apologize to the user and return.
            if (inState && !Util.IsPatchedConicsAvailable)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_Trajectories_ConicsErr"));
                Settings.DisplayTrajectories = false;
                if (AppLauncherButton.IconStyle != AppLauncherButton.IconStyleType.NORMAL)
                    AppLauncherButton.ChangeIcon(AppLauncherButton.IconStyleType.NORMAL);
                return;
            }

            Settings.DisplayTrajectories = inState;
            // change app toolbar button icon state
            if (inState && (AppLauncherButton.IconStyle == AppLauncherButton.IconStyleType.NORMAL))
                AppLauncherButton.ChangeIcon(AppLauncherButton.IconStyleType.ACTIVE);
            else if (!inState && (AppLauncherButton.IconStyle != AppLauncherButton.IconStyleType.NORMAL))
                AppLauncherButton.ChangeIcon(AppLauncherButton.IconStyleType.NORMAL);

        }

        private static void OnButtonClick_DisplayTrajectoriesInFlight(bool inState) => Settings.DisplayTrajectoriesInFlight = inState;

        private static void OnButtonClick_BodyFixedMode(bool inState) => Settings.BodyFixedMode = inState;

        private static void OnButtonClick_DisplayCompleteTrajectory(bool inState) => Settings.DisplayCompleteTrajectory = inState;

        private static void OnButtonClick_UseCache(bool inState) => Settings.UseCache = inState;

        private static void OnButtonClick_UseDescentRetro(bool inState) => Settings.DefaultDescentIsRetro = inState;

        private static void OnButtonClick_AutoUpdateAerodynamicModel(bool inState) => Settings.AutoUpdateAerodynamicModel = inState;

        private static void OnButtonClick_Update() => Trajectory.InvalidateAerodynamicModel();

        private static void OnButtonClick_UseBlizzyToolbar(bool inState)
        {
            if (ToolbarManager.ToolbarAvailable)
                Settings.UseBlizzyToolbar = inState;
        }

        private static void OnButtonClick_NewGui(bool inState)
        {
            Settings.NewGui = inState;
            Settings.MainGUIEnabled = inState;
            Settings.GUIEnabled = !inState;
        }

        private static void OnButtonClick_Prograde(bool inState)
        {
            DescentProfile.RetrogradeEntry = !inState;
            DescentProfile.Save();
        }

        private static void OnButtonClick_Retrograde(bool inState)
        {
            DescentProfile.RetrogradeEntry = inState;
            DescentProfile.Save();
        }

        private static void OnButtonClick_EntryHorizon(bool inState)
        {
            DescentProfile.atmos_entry.Horizon = inState;
            DescentProfile.Save();
        }

        private static void OnButtonClick_HighHorizon(bool inState)
        {
            DescentProfile.high_altitude.Horizon = inState;
            DescentProfile.Save();
        }

        private static void OnButtonClick_LowHorizon(bool inState)
        {
            DescentProfile.low_altitude.Horizon = inState;
            DescentProfile.Save();
        }

        private static void OnButtonClick_FinalHorizon(bool inState)
        {
            DescentProfile.final_approach.Horizon = inState;
            DescentProfile.Save();
        }

        private static void OnButtonClick_TargetImpact()
        {
            // grab the last patch that was calculated
            Trajectory.Patch lastPatch = Trajectory.Patches.LastOrDefault();

            if (lastPatch != null && lastPatch.ImpactPosition.HasValue)
            {
                Trajectory.Target.SetFromWorldPos(lastPatch.StartingState.ReferenceBody, lastPatch.ImpactPosition.Value);
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_Trajectories_TargetingImpact"));
            }
        }

        private static void OnButtonClick_TargetKSC()
        {
            CelestialBody homebody = FlightGlobals.GetHomeBody();

            double latitude = SpaceCenter.Instance.Latitude;
            double longitude = SpaceCenter.Instance.Longitude;

            if (homebody != null)
                Trajectory.Target.SetFromLatLonAlt(homebody, latitude, longitude);
        }

        private static void OnButtonClick_TargetVessel()
        {
            // grab the currently targeted vessel
            Vessel targetVessel = FlightGlobals.fetch.VesselTarget?.GetVessel();

            if (targetVessel != null && targetVessel.Landed)
            {
                Trajectory.Target.SetFromWorldPos(targetVessel.lastBody, targetVessel.GetWorldPos3D() - targetVessel.lastBody.position);
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_Trajectories_TargetingVessel", targetVessel.GetName()));
            }
        }

        private static void OnButtonClick_TargetWaypoint()
        {
            // grab the currently selected waypoint
            FinePrint.Waypoint navigationWaypoint = FlightGlobals.ActiveVessel?.navigationWaypoint;

            if (navigationWaypoint != null)
            {
                Trajectory.Target.SetFromLatLonAlt(navigationWaypoint.celestialBody,
                    navigationWaypoint.latitude, navigationWaypoint.longitude, navigationWaypoint.altitude);
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_Trajectories_TargetingWaypoint", navigationWaypoint.name));
            }
        }

        private static void OnButtonClick_TargetManual()
        {
            CelestialBody body = FlightGlobals.currentMainBody;

            string[] latLng = Trajectory.Target.ManualText.Split(new char[] { ',', ';' });

            if (latLng.Length == 2 && body != null)
            {
                double lat;
                double lng;

                if (double.TryParse(latLng[0].Trim(), out lat) && double.TryParse(latLng[1].Trim(), out lng))
                {
                    Trajectory.Target.SetFromLatLonAlt(body, lat, lng);
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_Trajectories_TargetingManual"));
                }
            }
        }

        private static void OnButtonClick_TargetClear() => Trajectory.Target.Clear();

        private static string OnTextInput_TargetManual(string inString)
        {
            string[] latLng = inString.Split(new char[] { ',', ';' });
            if (latLng.Length == 2)
            {
                latLng[0].Trim();
                latLng[1].Trim();

                double lat;
                double lng;

                if (double.TryParse(latLng[0], out lat) && double.TryParse(latLng[1], out lng))
                {
                    manual_target_txt = latLng[0] + " , " + latLng[1];
                    manual_target_txt_changed = true;
                    manual_target_txt_ok = true;
                }
                else
                {
                    manual_target_txt = inString;
                    manual_target_txt_ok = false;
                }
            }
            else
            {
                manual_target_txt = inString;
                manual_target_txt_ok = false;
            }

            manual_target_textinput.text = manual_target_txt;
            Trajectory.Target.ManualText = manual_target_txt;
            Trajectory.Target.Save();
            return null;
        }

        private static string OnTextInput_DescentEntry(string inString)
        {
            string trimmed = inString.Trim();
            float angle;

            if (float.TryParse(inString, out angle))
            {
                descent_entry_txt = trimmed;
            }
            else
            {
                descent_entry_txt = inString;
            }

            descent_entry_textinput.text = descent_entry_txt;

            if (Math.Abs(angle) <= 180f)
            {
                DescentProfile.atmos_entry.AngleDeg = angle;
                DescentProfile.RefreshGui();
                DescentProfile.Save();
            }
            return null;
        }

        private static string OnTextInput_DescentHigh(string inString)
        {
            string trimmed = inString.Trim();
            float angle;

            if (float.TryParse(inString, out angle))
            {
                descent_high_txt = trimmed;
            }
            else
            {
                descent_high_txt = inString;
            }

            descent_high_textinput.text = descent_high_txt;

            if (Math.Abs(angle) <= 180f)
            {
                DescentProfile.high_altitude.AngleDeg = angle;
                DescentProfile.RefreshGui();
                DescentProfile.Save();
            }
            return null;
        }

        private static string OnTextInput_DescentLow(string inString)
        {
            string trimmed = inString.Trim();
            float angle;

            if (float.TryParse(inString, out angle))
            {
                descent_low_txt = trimmed;
            }
            else
            {
                descent_low_txt = inString;
            }

            descent_low_textinput.text = descent_low_txt;

            if (Math.Abs(angle) <= 180f)
            {
                DescentProfile.low_altitude.AngleDeg = angle;
                DescentProfile.RefreshGui();
                DescentProfile.Save();
            }
            return null;
        }

        private static string OnTextInput_DescentFinal(string inString)
        {
            string trimmed = inString.Trim();
            float angle;

            if (float.TryParse(inString, out angle))
            {
                descent_final_txt = trimmed;
            }
            else
            {
                descent_final_txt = inString;
            }

            descent_final_textinput.text = descent_final_txt;

            if (Math.Abs(angle) <= 180f)
            {
                DescentProfile.final_approach.AngleDeg = angle;
                DescentProfile.RefreshGui();
                DescentProfile.Save();
            }
            return null;
        }
        #endregion

        #region Callback methods for the Gui components
        // Callback methods are used by the Gui to retrieve information it needs either for displaying or setting values.
        private static void OnSliderSet_MaxPatches(float invalue) => Settings.MaxPatchCount = (int)invalue;

        private static void OnSliderSet_MaxFramesPatch(float invalue) => Settings.MaxFramesPerPatch = (int)invalue;

        private static void OnSliderSet_IntegrationStep(float invalue) => IntegratorSliderPos = invalue;

        private static void OnSliderSet_EntryAngle(float invalue)
        {
            DescentProfile.atmos_entry.SliderPos = invalue;
            DescentProfile.RefreshGui();
            DescentProfile.Save();
        }

        private static void OnSliderSet_HighAngle(float invalue)
        {
            DescentProfile.high_altitude.SliderPos = invalue;
            DescentProfile.RefreshGui();
            DescentProfile.Save();
        }

        private static void OnSliderSet_LowAngle(float invalue)
        {
            DescentProfile.low_altitude.SliderPos = invalue;
            DescentProfile.RefreshGui();
            DescentProfile.Save();
        }

        private static void OnSliderSet_GroundAngle(float invalue)
        {
            DescentProfile.final_approach.SliderPos = invalue;
            DescentProfile.RefreshGui();
            DescentProfile.Save();
        }
        #endregion

        #region Page methods for changing/updating the pages in the Gui page box
        /// <summary> Changes the page inside the page box. </summary>
        private static void ChangePage(PageType inpage)
        {
            Settings.MainGUICurrentPage = (int)inpage;

            // remove current page from page box
            page_box.children[0].uiItem.gameObject.DestroyGameObjectImmediate();
            page_box.children.Clear();

            // insert desired page into page box
            switch (inpage)
            {
                case PageType.TARGET:
                    page_box.children.Add(target_page);
                    break;
                case PageType.DESCENT:
                    page_box.children.Add(descent_page);
                    break;
                case PageType.SETTINGS:
                    page_box.children.Add(settings_page);
                    break;
                default:
                    page_box.children.Add(info_page);
                    break;
            }

            // required to force the Gui to update
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(page_box.uiItem.gameObject.transform);
            page_box.children[0].Create(ref stack, HighLogic.UISkin);

            //set data field label justification
            SetDataFieldJustification();

            // create text input box event listeners
            if (inpage == PageType.TARGET || inpage == PageType.DESCENT)
                SetTextInputBoxEvents();
        }

        /// <summary> Updates the strings used by the Gui components to display changing values/data </summary>
        private static void UpdatePages()
        {
            // skip updates for a smoother display and increased performance
            if (Util.Clocks - update_timer <= System.Diagnostics.Stopwatch.Frequency / update_fps)
                return;
            update_timer = Util.Clocks;

            switch ((PageType)Settings.MainGUICurrentPage)
            {
                case PageType.INFO:
                    UpdateInfoPage();
                    return;
                case PageType.TARGET:
                    UpdateTargetPage();
                    if (manual_target_txt_changed)
                    {
                        ChangePage(PageType.TARGET);
                        manual_target_txt_changed = false;
                    }
                    return;
                case PageType.SETTINGS:
                    UpdateSettingsPage();
                    return;
            }
        }

        /// <summary> Updates the strings used by the info page to display changing values/data </summary>
        private static void UpdateInfoPage()
        {
            // grab the last patch that was calculated
            Trajectory.Patch lastPatch = Trajectory.Patches.LastOrDefault();

            // max G-force
            max_gforce_txt = string.Format("{0:0.00}", Settings.DisplayTrajectories ? Trajectory.MaxAccel / 9.81 : 0);

            // impact values
            if (lastPatch != null && lastPatch.ImpactPosition.HasValue && Settings.DisplayTrajectories)
            {
                // calculate body offset impact position
                CelestialBody lastPatchBody = lastPatch.StartingState.ReferenceBody;
                Vector3d impactPos = lastPatch.ImpactPosition.Value + lastPatchBody.position;

                // impact position
                impact_latitude_txt = string.Format("{0:000.000000}", lastPatchBody.GetLatitude(impactPos));
                impact_longitude_txt = string.Format("{0:000.000000}", lastPatchBody.GetLongitude(impactPos));

                // impact velocity
                Vector3d up = lastPatch.ImpactPosition.Value.normalized;
                Vector3d vel = lastPatch.ImpactVelocity.Value - lastPatchBody.getRFrmVel(impactPos);
                double vVelMag = Vector3d.Dot(vel, up);
                double hVelMag = (vel - (up * vVelMag)).magnitude;

                impact_vertical_txt = string.Format("{0:F1} {1}", -vVelMag, Localizer.Format("#autoLOC_Trajectories_ms"));
                impact_horizontal_txt = string.Format("{0:F1} {1}", hVelMag, Localizer.Format("#autoLOC_Trajectories_ms"));

                // time to impact
                double duration = (lastPatch.EndTime - Planetarium.GetUniversalTime()) / 3600.0;   // duration in hrs
                double hours = Math.Truncate(duration);
                double mins = Math.Truncate((duration - hours) * 60.0);
                double secs = (((duration - hours) * 60.0) - mins) * 60.0;
                impact_time_txt = string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
            }
            else
            {
                impact_latitude_txt = "---.------";
                impact_longitude_txt = "---.------";
                impact_vertical_txt = "-.- " + Localizer.Format("#autoLOC_Trajectories_ms");
                impact_horizontal_txt = "-.- " + Localizer.Format("#autoLOC_Trajectories_ms");
                impact_time_txt = "--:--:--";
            }

            // target distance
            UpdateTargetDistance();
        }

        /// <summary> Updates the strings used by the target page to display changing values/data </summary>
        private static void UpdateTargetPage()
        {
            // grab the last patch that was calculated
            Trajectory.Patch lastPatch = Trajectory.Patches.LastOrDefault();
            CelestialBody targetBody = Trajectory.Target.Body;

            // target position and distance values
            if (targetBody != null && Trajectory.Target.WorldPosition.HasValue)
            {
                // calculate body offset target position
                Vector3d targetPos = Trajectory.Target.WorldPosition.Value + targetBody.position;

                // target body
                target_body_txt = targetBody.bodyName;

                // target position
                target_latitude_txt = string.Format("{0:000.000000}", targetBody.GetLatitude(targetPos));
                target_longitude_txt = string.Format("{0:000.000000}", targetBody.GetLongitude(targetPos));

                // target distance
                UpdateTargetDistance();
            }
            else
            {
                target_body_txt = "---";
                target_latitude_txt = "---.------";
                target_longitude_txt = "---.------";
                target_distance_txt = "-.-- " + Localizer.Format("#autoLOC_Trajectories_km");
                target_distance_latitude_txt = "-: -.-- " + Localizer.Format("#autoLOC_Trajectories_km");
                target_distance_longitude_txt = "-: -.-- " + Localizer.Format("#autoLOC_Trajectories_km");
            }

            if (manual_target_txt != Trajectory.Target.ManualText)
            {
                OnTextInput_TargetManual(Trajectory.Target.ManualText);
                manual_target_txt_changed = true;
            }
        }

        /// <summary> Updates the strings used by the settings page to display changing values/data </summary>
        private static void UpdateSettingsPage()
        {
            // aerodynamic model
            aerodynamic_model_txt = aerodynamic_model_hdrtxt + Trajectory.AerodynamicModelName;

            // performance
            calculation_time_txt = calculation_time_hdrtxt +
                string.Format("{0:0.0}ms | {1:0.0} %", Trajectory.ComputationTime * 1000.0f,
                    (Trajectory.ComputationTime / Trajectory.GameFrameTime) * 100.0f);

            // num errors
            num_errors_txt = errors_hdrtxt + string.Format("{0:0}", Trajectory.ErrorCount);
        }

        /// <summary> Updates the strings used by the info and target page to display target distance </summary>
        private static void UpdateTargetDistance()
        {
            // grab the last patch that was calculated
            Trajectory.Patch lastPatch = Trajectory.Patches.LastOrDefault();
            CelestialBody targetBody = Trajectory.Target.Body;
            CelestialBody lastPatchBody = lastPatch?.StartingState.ReferenceBody;

            // target position and distance values
            if (targetBody != null && Trajectory.Target.WorldPosition.HasValue)
            {
                // calculate body offset target position
                Vector3d targetPos = Trajectory.Target.WorldPosition.Value + targetBody.position;

                // target distance values
                if (lastPatch != null && lastPatch.ImpactPosition.HasValue && lastPatchBody == targetBody &&
                    Settings.DisplayTrajectories)
                {
                    // calculate body offset impact position
                    Vector3d impactPos = lastPatch.ImpactPosition.Value + lastPatchBody.position;

                    // get latitude, longitude and altitude for impact position
                    double impactLat;
                    double impatLon;
                    double impactAlt;
                    lastPatchBody.GetLatLonAlt(impactPos, out impactLat, out impatLon, out impactAlt);

                    // get latitude, longitude and altitude for target position
                    double targetLat;
                    double targetLon;
                    double targetAlt;
                    targetBody.GetLatLonAlt(targetPos, out targetLat, out targetLon, out targetAlt);

                    // calculate distances
                    double targetDistance = Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        impactLat, impatLon, targetLat, targetLon) / 1e3;

                    double targetDistanceNorth = (Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        impactLat, targetLon, targetLat, targetLon) / 1e3) * ((targetLat - impactLat) < 0.0d ? -1.0d : +1.0d);

                    double targetDistanceEast = (Util.DistanceFromLatitudeAndLongitude(targetBody.Radius + impactAlt,
                        targetLat, impatLon, targetLat, targetLon) / 1e3) * ((targetLon - impatLon) < 0.0d ? -1.0d : +1.0d);

                    // target distance
                    target_distance_txt = string.Format("{0:F2} {1}", targetDistance, Localizer.Format("#autoLOC_Trajectories_km"));
                    target_distance_latitude_txt = string.Format("{0}: {1:F2} {2}", targetDistanceNorth > 0.0d ? 'N' : 'S',
                        Math.Abs(targetDistanceNorth), Localizer.Format("#autoLOC_Trajectories_km"));
                    target_distance_longitude_txt = string.Format("{0}: {1:F2} {2}", targetDistanceEast > 0.0d ? 'E' : 'W',
                        Math.Abs(targetDistanceEast), Localizer.Format("#autoLOC_Trajectories_km"));
                }
                else
                {
                    target_distance_txt = "-.-- " + Localizer.Format("#autoLOC_Trajectories_km");
                    target_distance_latitude_txt = "-: -.-- " + Localizer.Format("#autoLOC_Trajectories_km");
                    target_distance_longitude_txt = "-: -.-- " + Localizer.Format("#autoLOC_Trajectories_km");
                }
            }
            else
            {
                target_distance_txt = "-.-- " + Localizer.Format("#autoLOC_Trajectories_km");
                target_distance_latitude_txt = "-: -.-- " + Localizer.Format("#autoLOC_Trajectories_km");
                target_distance_longitude_txt = "-: -.-- " + Localizer.Format("#autoLOC_Trajectories_km");
            }

        }
        #endregion
    }
}
