using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using KSP.Localization;
using UnityEngine;

namespace Trajectories
{
    /// <summary> MainGUI window handler. </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class MainGUI: MonoBehaviour
    {
        // constants
        private const float width = 320.0f;
        private const float height = 250.0f;
        private const float button_width = 75.0f;
        private const float button_height = 25.0f;
        private const float slider_width = 130.0f;
        private const int page_padding = 10;

        // version string
        private static string version_txt = " vX.X.X";

        private enum PageType
        {
            INFO = 0,
            TARGET,
            DESCENT,
            SETTINGS
        }

        // visible flag
        private static bool visible = false;

        // popup window, page box and pages
        private static MultiOptionDialog multi_dialog;
        private static PopupDialog popup_dialog;
        private static DialogGUIBox page_box;

        private static DialogGUIVerticalLayout info_page;
        private static DialogGUIVerticalLayout target_page;
        private static DialogGUIVerticalLayout descent_page;
        private static DialogGUIVerticalLayout settings_page;

        // display update strings
        private static string max_gforce_hdrtxt = Localizer.Format("#autoLOC_Trajectories_MaxGforce") + ": ";
        private static string aerodynamic_model_hdrtxt = Localizer.Format("#autoLOC_Trajectories_AeroModel") + ": ";
        private static string performance_hdrtxt = Localizer.Format("#autoLOC_900334") + ": ";
        private static string errors_hdrtxt = Localizer.Format("#autoLOC_Trajectories_Errors") + ": ";

        private static string max_gforce_txt = "";
        private static string impact_position_txt = "";
        private static string impact_velocity_txt = "";
        private static string impact_time_txt = "";
        private static string aerodynamic_model_txt = "";
        private static string performance_txt = "";
        private static string num_errors_txt = "";

        // display update timer
        private static double update_timer = Util.Clocks;
        private static double update_fps = 10;  // Frames per second the data values displayed in the Gui will update.

        // permit global access
        public static MainGUI Fetch
        {
            get; private set;
        } = null;

        //  constructor
        public MainGUI()
        {
            // enable global access
            Fetch = this;

            // version string
            version_txt = " v" + typeof(MainGUI).Assembly.GetName().Version;
            version_txt = version_txt.Remove(version_txt.LastIndexOf("."));
            UnityEngine.Debug.Log(Localizer.Format("#autoLOC_Trajectories_Title") + version_txt);

            // allocate and define window for use in the popup dialog
            Allocate();
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        private void Awake()
        {
            // create and display App launch button
            AppLauncherButton.Create();

            // set page padding
            info_page.padding.left = page_padding;
            info_page.padding.right = page_padding;
            target_page.padding.left = page_padding;
            target_page.padding.right = page_padding;
            descent_page.padding.left = page_padding;
            descent_page.padding.right = page_padding;
            settings_page.padding.left = page_padding;
            settings_page.padding.right = page_padding;

            // create popup dialog and hide it
            popup_dialog = PopupDialog.SpawnPopupDialog(multi_dialog, true, HighLogic.UISkin, false, "");
            Hide();
        }

        private void Update()
        {
            // hide or show the dialog box
            if ((!Settings.fetch.MainGUIEnabled || !Util.IsFlight || PlanetariumCamera.Camera == null) && visible)
            {
                Hide();
                return;
            }
            else if (Settings.fetch.MainGUIEnabled && !visible)
            {
                Show();
            }

            UpdatePages();
        }

        // FixedUpdate is required to fix the vessel switching bug!
        private void FixedUpdate()
        {
        }

        private void OnDestroy()
        {
            Fetch = null;

            // save popup position. Note. PopupDialog.RTrf is an offset from the center of the screeen.
            Settings.fetch.MainGUIWindowPos = new Vector2(
                ((Screen.width / 2) + popup_dialog.RTrf.position.x) / Screen.width,
                ((Screen.height / 2) + popup_dialog.RTrf.position.y) / Screen.height);

            popup_dialog.Dismiss();
            popup_dialog = null;
            AppLauncherButton.Destroy();
        }

        /// <summary>
        /// Allocates any classes, variables etc needed for the MainGUI,
        ///   note that this method should be called from the constructor.
        /// </summary>
        private void Allocate()
        {
            // default window to center of screen
            if (Settings.fetch.MainGUIWindowPos.x <= 0 || Settings.fetch.MainGUIWindowPos.y <= 0)
                Settings.fetch.MainGUIWindowPos = new Vector2(0.5f, 0.5f);

            // create pages
            info_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIHorizontalLayout(
                    new DialogGUIToggle(() => { return Util.IsPatchedConicsAvailable ? Settings.fetch.DisplayTrajectories : false; },
                        Localizer.Format("#autoLOC_Trajectories_ShowTrajectory"), OnButtonClick_DisplayTrajectories),
                    new DialogGUIToggle(() => { return Settings.fetch.DisplayTrajectoriesInFlight; },
                        Localizer.Format("#autoLOC_Trajectories_InFlight"), OnButtonClick_DisplayTrajectoriesInFlight)),
                new DialogGUIHorizontalLayout(
                    new DialogGUIToggle(() => { return Settings.fetch.BodyFixedMode; },
                        Localizer.Format("#autoLOC_Trajectories_FixedBody"), OnButtonClick_BodyFixedMode),
                    new DialogGUIToggle(() => { return Settings.fetch.DisplayCompleteTrajectory; },
                        Localizer.Format("#autoLOC_7001028"), OnButtonClick_DisplayCompleteTrajectory)),
                new DialogGUILabel(() => { return max_gforce_txt; }, true),
                new DialogGUILabel(() => { return impact_position_txt; }, true),
                new DialogGUILabel(() => { return impact_velocity_txt; }, true),
                new DialogGUILabel(() => { return impact_time_txt; }, true),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(() => { return Settings.fetch.ShowPerformance ? performance_txt : ""; }, true),
                    new DialogGUILabel(() => { return Settings.fetch.ShowPerformance ? num_errors_txt : ""; }, true)),
                new DialogGUILabel(() => { return aerodynamic_model_txt; }, true)
                );

            target_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUISpace(4),
                new DialogGUILabel("<b>   Target Page</b>", true));

            descent_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIHorizontalLayout(false, false, 0, new RectOffset(), TextAnchor.MiddleCenter,
                    new DialogGUIToggle(() => { return DescentProfile.fetch.ProgradeEntry; },
                        Localizer.Format("#autoLOC_900597"), OnButtonClick_Prograde),
                    new DialogGUIToggle(() => { return DescentProfile.fetch.RetrogradeEntry; },
                        Localizer.Format("#autoLOC_900607"), OnButtonClick_Retrograde)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(DescentProfile.fetch.entry.Name, true),
                    new DialogGUIToggle(() => { return DescentProfile.fetch.entry.Horizon; },
                        () => { return DescentProfile.fetch.entry.Horizon_txt; }, OnButtonClick_EntryHorizon, 60f),
                    new DialogGUISlider(() => { return DescentProfile.fetch.entry.SliderPos; },
                        -1f, 1f, false, slider_width, -1, OnSliderSet_EntryAngle),
                    new DialogGUILabel(() => { return DescentProfile.fetch.entry.Angle_txt; }, 36f)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(DescentProfile.fetch.highAltitude.Name, true),
                    new DialogGUIToggle(() => { return DescentProfile.fetch.highAltitude.Horizon; },
                        () => { return DescentProfile.fetch.highAltitude.Horizon_txt; }, OnButtonClick_HighHorizon, 60f),
                    new DialogGUISlider(() => { return DescentProfile.fetch.highAltitude.SliderPos; },
                        -1f, 1f, false, slider_width, -1, OnSliderSet_HighAngle),
                    new DialogGUILabel(() => { return DescentProfile.fetch.highAltitude.Angle_txt; }, 36f)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(DescentProfile.fetch.lowAltitude.Name, true),
                    new DialogGUIToggle(() => { return DescentProfile.fetch.lowAltitude.Horizon; },
                        () => { return DescentProfile.fetch.lowAltitude.Horizon_txt; }, OnButtonClick_LowHorizon, 60f),
                    new DialogGUISlider(() => { return DescentProfile.fetch.lowAltitude.SliderPos; },
                        -1f, 1f, false, slider_width, -1, OnSliderSet_LowAngle),
                    new DialogGUILabel(() => { return DescentProfile.fetch.lowAltitude.Angle_txt; }, 36f)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(DescentProfile.fetch.finalApproach.Name, true),
                    new DialogGUIToggle(() => { return DescentProfile.fetch.finalApproach.Horizon; },
                        () => { return DescentProfile.fetch.finalApproach.Horizon_txt; }, OnButtonClick_GroundHorizon, 60f),
                    new DialogGUISlider(() => { return DescentProfile.fetch.finalApproach.SliderPos; },
                        -1f, 1f, false, slider_width, -1, OnSliderSet_GroundAngle),
                    new DialogGUILabel(() => { return DescentProfile.fetch.finalApproach.Angle_txt; }, 36f))
                );

            settings_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIToggle(() => { return ToolbarManager.ToolbarAvailable ? Settings.fetch.UseBlizzyToolbar : false; },
                    Localizer.Format("#autoLOC_Trajectories_UseBlizzyToolbar"), OnButtonClick_UseBlizzyToolbar),
                new DialogGUIHorizontalLayout(false, false, 0, new RectOffset(), TextAnchor.MiddleCenter,
                    new DialogGUIToggle(() => { return Settings.fetch.UseCache; },
                        Localizer.Format("#autoLOC_Trajectories_UseCache"), OnButtonClick_UseCache),
                    new DialogGUIToggle(() => { return Settings.fetch.AutoUpdateAerodynamicModel; },
                        Localizer.Format("#autoLOC_Trajectories_AutoUpdate"), OnButtonClick_AutoUpdateAerodynamicModel),
                    new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_Update"),
                        OnButtonClick_Update, button_width, button_height, false)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_MaxPatches"), true),
                    new DialogGUISlider(() => { return Settings.fetch.MaxPatchCount; },
                        3f, 10f, true, slider_width, -1, OnSliderSet_MaxPatches),
                    new DialogGUILabel(() => { return Settings.fetch.MaxPatchCount.ToString(); }, 15f)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(Localizer.Format("#autoLOC_Trajectories_MaxFramesPatch"), true),
                    new DialogGUISlider(() => { return Settings.fetch.MaxFramesPerPatch; },
                        1f, 50f, true, slider_width, -1, OnSliderSet_MaxFramesPatch),
                    new DialogGUILabel(() => { return Settings.fetch.MaxFramesPerPatch.ToString(); }, 15f)),
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(() => { return performance_txt; }, true),
                    new DialogGUILabel(() => { return num_errors_txt; }, true)),
                new DialogGUIHorizontalLayout(true, false,
                    new DialogGUIToggle(() => { return Settings.fetch.ShowPerformance; },
                        Localizer.Format("#autoLOC_Trajectories_ShowPerformance"), OnButtonClick_ShowPerformance, 125f),
                    new DialogGUIToggle(() => { return Settings.fetch.NewGui; },
                        Localizer.Format("#autoLOC_Trajectories_NewGui"), OnButtonClick_NewGui))
                );

            // create page box with current page inserted into page box
            switch ((PageType)Settings.fetch.MainGUICurrentPage)
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
               Localizer.Format("#autoLOC_Trajectories_Title"),
               Localizer.Format("#autoLOC_Trajectories_Title") + " -" + version_txt,
               HighLogic.UISkin,
               // window origin is center of rect, position is offset from lower left corner of screen and normalized i.e (0.5, 0.5 is screen center)
               new Rect(Settings.fetch.MainGUIWindowPos.x, Settings.fetch.MainGUIWindowPos.y, width, height),
               new DialogGUIBase[]
               {
                   // create page select buttons
                   new DialogGUIHorizontalLayout(
                       new DialogGUIButton(Localizer.Format("#autoLOC_900629"),
                           OnButtonClick_Info, ButtonEnabler_Info, button_width, button_height, false),
                       new DialogGUIButton(Localizer.Format("#autoLOC_900591"),
                           OnButtonClick_Target, ButtonEnabler_Target, button_width, button_height, false),
                       new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_Descent"),
                           OnButtonClick_Descent, ButtonEnabler_Descent, button_width, button_height, false),
                       new DialogGUIButton(Localizer.Format("#autoLOC_900734"),
                           OnButtonClick_Settings, ButtonEnabler_Settings, button_width, button_height, false)),
                   // insert page box
                   page_box
               });
        }

        /// <summary> Shows window. </summary>
        public void Show()
        {
            if (popup_dialog != null)
            {
                visible = true;
                popup_dialog.gameObject.SetActive(true);
            }
        }

        /// <summary> Hides window. </summary>
        public void Hide()
        {
            if (popup_dialog != null)
            {
                visible = false;
                popup_dialog.gameObject.SetActive(false);
            }
        }

        #region ButtonEnabler methods called by the GuiButtons
        // ButtonEnabler methods are Callbacks used by the GuiButtons to decide if they should enable or disable themselves.
        //  A GuiButton will call its ButtonEnabler method which returns false if the currently viewed page matches the GuiButton
        private static bool ButtonEnabler_Info()
        {
            if ((PageType)Settings.fetch.MainGUICurrentPage == PageType.INFO)
                return false;
            return true;
        }

        private static bool ButtonEnabler_Target()
        {
            if ((PageType)Settings.fetch.MainGUICurrentPage == PageType.TARGET)
                return false;
            return true;
        }

        private static bool ButtonEnabler_Descent()
        {
            if ((PageType)Settings.fetch.MainGUICurrentPage == PageType.DESCENT)
                return false;
            return true;
        }

        private static bool ButtonEnabler_Settings()
        {
            if ((PageType)Settings.fetch.MainGUICurrentPage == PageType.SETTINGS)
                return false;
            return true;
        }
        #endregion

        #region  OnButtonClick methods called by the GuiButtons and Toggles
        private void OnButtonClick_Info()
        {
            ChangePage(PageType.INFO);
        }

        private void OnButtonClick_Target()
        {
            ChangePage(PageType.TARGET);
        }

        private void OnButtonClick_Descent()
        {
            ChangePage(PageType.DESCENT);
        }

        private void OnButtonClick_Settings()
        {
            ChangePage(PageType.SETTINGS);
        }

        private static void OnButtonClick_DisplayTrajectories(bool inState)
        {
            // check that we have patched conics. If not, apologize to the user and return.
            if (inState && !Util.IsPatchedConicsAvailable)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_Trajectories_ConicsErr"));
                Settings.fetch.DisplayTrajectories = false;
                if (AppLauncherButton.IconStyle != AppLauncherButton.IconStyleType.NORMAL)
                    AppLauncherButton.ChangeIcon(AppLauncherButton.IconStyleType.NORMAL);
                return;
            }

            Settings.fetch.DisplayTrajectories = inState;
            // change app toolbar button icon state
            if (inState && (AppLauncherButton.IconStyle == AppLauncherButton.IconStyleType.NORMAL))
                AppLauncherButton.ChangeIcon(AppLauncherButton.IconStyleType.ACTIVE);
            else if (!inState && (AppLauncherButton.IconStyle != AppLauncherButton.IconStyleType.NORMAL))
                AppLauncherButton.ChangeIcon(AppLauncherButton.IconStyleType.NORMAL);

        }

        private static void OnButtonClick_DisplayTrajectoriesInFlight(bool inState)
        {
            Settings.fetch.DisplayTrajectoriesInFlight = inState;
        }

        private static void OnButtonClick_BodyFixedMode(bool inState)
        {
            Settings.fetch.BodyFixedMode = inState;
        }

        private static void OnButtonClick_DisplayCompleteTrajectory(bool inState)
        {
            Settings.fetch.DisplayCompleteTrajectory = inState;
        }

        private static void OnButtonClick_UseCache(bool inState)
        {
            Settings.fetch.UseCache = inState;
        }

        private static void OnButtonClick_AutoUpdateAerodynamicModel(bool inState)
        {
            Settings.fetch.AutoUpdateAerodynamicModel = inState;
        }

        private static void OnButtonClick_Update()
        {
            Trajectory.fetch.InvalidateAerodynamicModel();
        }

        private static void OnButtonClick_UseBlizzyToolbar(bool inState)
        {
            if (ToolbarManager.ToolbarAvailable)
                Settings.fetch.UseBlizzyToolbar = inState;
        }

        private static void OnButtonClick_ShowPerformance(bool inState)
        {
            Settings.fetch.ShowPerformance = inState;
        }

        private static void OnButtonClick_NewGui(bool inState)
        {
            Settings.fetch.NewGui = inState;
            Settings.fetch.MainGUIEnabled = inState;
            Settings.fetch.GUIEnabled = !inState;
        }

        private static void OnButtonClick_Prograde(bool inState)
        {
            if (inState != DescentProfile.fetch.ProgradeEntry)
            {
                DescentProfile.fetch.ProgradeEntry = inState;
                if (inState)
                    DescentProfile.fetch.Reset();
                DescentProfile.fetch.Save();
            }
        }

        private static void OnButtonClick_Retrograde(bool inState)
        {
            if (inState != DescentProfile.fetch.RetrogradeEntry)
            {
                DescentProfile.fetch.RetrogradeEntry = inState;
                if (inState)
                    DescentProfile.fetch.Reset(Math.PI);
                DescentProfile.fetch.Save();
            }
        }

        private static void OnButtonClick_EntryHorizon(bool inState)
        {
            DescentProfile.fetch.entry.Horizon = inState;
            DescentProfile.fetch.CheckGUI();
        }

        private static void OnButtonClick_HighHorizon(bool inState)
        {
            DescentProfile.fetch.highAltitude.Horizon = inState;
            DescentProfile.fetch.CheckGUI();
        }

        private static void OnButtonClick_LowHorizon(bool inState)
        {
            DescentProfile.fetch.lowAltitude.Horizon = inState;
            DescentProfile.fetch.CheckGUI();
        }

        private static void OnButtonClick_GroundHorizon(bool inState)
        {
            DescentProfile.fetch.finalApproach.Horizon = inState;
            DescentProfile.fetch.CheckGUI();
        }
        #endregion

        #region Callback methods for the Gui components
        // Callback methods are used by the Gui to retrieve information it needs either for display or setting values.
        private static void OnSliderSet_MaxPatches(float invalue)
        {
            Settings.fetch.MaxPatchCount = (int)invalue;
        }

        private static void OnSliderSet_MaxFramesPatch(float invalue)
        {
            Settings.fetch.MaxFramesPerPatch = (int)invalue;
        }

        private static void OnSliderSet_EntryAngle(float invalue)
        {
            DescentProfile.fetch.entry.SliderPos = invalue;
            DescentProfile.fetch.CheckGUI();
        }

        private static void OnSliderSet_HighAngle(float invalue)
        {
            DescentProfile.fetch.highAltitude.SliderPos = invalue;
            DescentProfile.fetch.CheckGUI();
        }

        private static void OnSliderSet_LowAngle(float invalue)
        {
            DescentProfile.fetch.lowAltitude.SliderPos = invalue;
            DescentProfile.fetch.CheckGUI();
        }

        private static void OnSliderSet_GroundAngle(float invalue)
        {
            DescentProfile.fetch.finalApproach.SliderPos = invalue;
            DescentProfile.fetch.CheckGUI();
        }
        #endregion

        #region Page methods for changing/updating the pages in the Gui page box
        /// <summary> Changes the page inside the page box. </summary>
        private static void ChangePage(PageType inpage)
        {
            Settings.fetch.MainGUICurrentPage = (int)inpage;

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
        }

        /// <summary> Updates the strings used by the Gui components to display changing values/data </summary>
        private static void UpdatePages()
        {
            // skip updates for a smoother display and increased performance
            if (Util.Clocks - update_timer <= Stopwatch.Frequency / update_fps)
                return;
            update_timer = Util.Clocks;

            switch ((PageType)Settings.fetch.MainGUICurrentPage)
            {
                case PageType.INFO:
                    UpdateInfoPage();
                    return;
                case PageType.TARGET:
                    UpdateTargetPage();
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
            Trajectory.Patch lastPatch = Trajectory.fetch?.Patches.LastOrDefault();

            // max G-force
            max_gforce_txt = max_gforce_hdrtxt +
                    string.Format("{0:0.00}", Settings.fetch.DisplayTrajectories ? Trajectory.fetch.MaxAccel / 9.81 : 0);

            // impact values
            if (lastPatch != null && lastPatch.ImpactPosition.HasValue && Settings.fetch.DisplayTrajectories)
            {
                // calculate body offset impact position
                CelestialBody lastPatchBody = lastPatch.StartingState.ReferenceBody;
                Vector3d impactPos = lastPatch.ImpactPosition.Value + lastPatchBody.position;

                // impact position
                impact_position_txt = Localizer.Format("#autoLOC_Trajectories_ImpactPosition",
                    string.Format("{0:000.000000}", lastPatchBody.GetLatitude(impactPos)),
                    string.Format("{0:000.000000}", lastPatchBody.GetLongitude(impactPos)));

                // impact velocity
                Vector3d up = lastPatch.ImpactPosition.Value.normalized;
                Vector3d vel = lastPatch.ImpactVelocity.Value - lastPatchBody.getRFrmVel(impactPos);
                double vVelMag = Vector3d.Dot(vel, up);
                double hVelMag = (vel - (up * vVelMag)).magnitude;

                impact_velocity_txt = Localizer.Format("#autoLOC_Trajectories_ImpactVelocity",
                    string.Format("{0:0.0}", -vVelMag),
                    string.Format("{0:0.0}", hVelMag));

                // time to impact
                double duration = (lastPatch.EndTime - lastPatch.StartingState.Time) / 3600.0;   // duration in hrs
                double hours = Math.Truncate(duration);
                double mins = Math.Truncate((duration - hours) * 60.0);
                double secs = (((duration - hours) * 60.0) - mins) * 60.0;
                impact_time_txt = Localizer.Format("#autoLOC_Trajectories_ImpactTime",
                    string.Format("{0:00}:{1:00}:{2:00.00}", hours, mins, secs));
            }
            else
            {
                impact_position_txt = Localizer.Format("#autoLOC_Trajectories_ImpactPosition", "---", "---");
                impact_velocity_txt = Localizer.Format("#autoLOC_Trajectories_ImpactVelocity", "---", "---");
                impact_time_txt = Localizer.Format("#autoLOC_Trajectories_ImpactTime", "--:--:--");
            }

            // performace and errors
            if (Settings.fetch.ShowPerformance)
                UpdateSettingsPage();

            // aerodynamic model
            aerodynamic_model_txt = aerodynamic_model_hdrtxt + Trajectory.fetch.AerodynamicModelName;
        }

        /// <summary> Updates the strings used by the target page to display changing values/data </summary>
        private static void UpdateTargetPage()
        {
        }

        /// <summary> Updates the strings used by the settings page to display changing values/data </summary>
        private static void UpdateSettingsPage()
        {
            Trajectory traj = Trajectory.fetch;

            if (traj != null)
            {
                // performance
                performance_txt = performance_hdrtxt +
                    string.Format("{0:0.0}ms | {1:0.0} %", traj.ComputationTime * 1000.0f, (traj.ComputationTime / traj.GameFrameTime) * 100.0f);

                // num errors
                num_errors_txt = errors_hdrtxt + string.Format("{0:0}", traj.ErrorCount);
            }
            else
            {
                performance_txt = performance_hdrtxt + "0.0ms | 0.0 %";
                num_errors_txt = errors_hdrtxt + "0";
            }
        }
        #endregion
    }
}
