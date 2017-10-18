using System;
using System.Linq;
using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;

namespace Trajectories
{
    /// <summary> MainGUI window handler. </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MainGUI: MonoBehaviour
    {
        // constants
        private const float width = 320.0f;
        private const float height = 250.0f;
        private const float button_width = 75.0f;
        private const float button_height = 25.0f;

        private enum PageType
        {
            INFO = 0,
            TARGET,
            DESCENT,
            SETTINGS
        }

        // permit global access
        private static MainGUI instance = null;

        // visible flag
        private static bool visible = false;

        // popup window, page box, buttons and pages
        private static MultiOptionDialog multi_dialog;
        private static PopupDialog popup_dialog;
        private static DialogGUIBox page_box;

        private static DialogGUIButton info_button;
        private static DialogGUIButton target_button;
        private static DialogGUIButton descent_button;
        private static DialogGUIButton settings_button;

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
        private static string aerodynamic_model_txt = "";
        private static string performance_txt = "";
        private static string num_errors_txt = "";

        public static MainGUI Instance
        {
            get
            {
                return instance;
            }
        }

        //  constructor
        public MainGUI()
        {
            // enable global access
            instance = this;

            // allocate and define window for use in the popup dialog
            Allocate();
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        private void Awake()
        {
            // create and display App launch button
            AppLauncherButton.Create();

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

        private void OnDestroy()
        {
            instance = null;

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

            // create buttons
            info_button = new DialogGUIButton(Localizer.Format("#autoLOC_900629"),
                OnButtonClick_Info, ButtonEnabler_Info, button_width, button_height, false);
            target_button = new DialogGUIButton(Localizer.Format("#autoLOC_900591"),
                OnButtonClick_Target, ButtonEnabler_Target, button_width, button_height, false);
            descent_button = new DialogGUIButton(Localizer.Format("#autoLOC_Trajectories_Descent"),
                OnButtonClick_Descent, ButtonEnabler_Descent, button_width, button_height, false);
            settings_button = new DialogGUIButton(Localizer.Format("#autoLOC_900734"),
                OnButtonClick_Settings, ButtonEnabler_Settings, button_width, button_height, false);

            // create pages
            info_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUIHorizontalLayout(
                    new DialogGUIToggle(() => { return Util.isPatchedConicsAvailable ? Settings.fetch.DisplayTrajectories : false; },
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
                new DialogGUIHorizontalLayout(
                    new DialogGUILabel(() => { return Settings.fetch.ShowPerformance ? performance_txt : ""; }, true),
                    new DialogGUILabel(() => { return Settings.fetch.ShowPerformance ? num_errors_txt : ""; }, true)),
                new DialogGUILabel(() => { return aerodynamic_model_txt; }, true)
                );

            target_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUISpace(4),
                new DialogGUILabel("<b>   Target Page</b>", true));

            descent_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
                new DialogGUISpace(4),
                new DialogGUILabel("<b>   Descent Page</b>", true));

            settings_page = new DialogGUIVerticalLayout(false, true, 0, new RectOffset(), TextAnchor.UpperCenter,
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
               HighLogic.UISkin,
               // window origin is center of rect, position is offset from lower left corner of screen and normalized i.e (0.5, 0.5 is screen center)
               new Rect(Settings.fetch.MainGUIWindowPos.x, Settings.fetch.MainGUIWindowPos.y, width, height),
               new DialogGUIBase[]
               {
                   // create line of buttons
                   new DialogGUIHorizontalLayout(info_button, target_button, descent_button, settings_button),
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

        private void OnButtonClick_DisplayTrajectories(bool inState)
        {
            // check that we have patched conics. If not, apologize to the user and return.
            if (inState && !Util.isPatchedConicsAvailable)
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

        private void OnButtonClick_DisplayTrajectoriesInFlight(bool inState)
        {
            Settings.fetch.DisplayTrajectoriesInFlight = inState;
        }

        private void OnButtonClick_BodyFixedMode(bool inState)
        {
            Settings.fetch.BodyFixedMode = inState;
        }

        private void OnButtonClick_DisplayCompleteTrajectory(bool inState)
        {
            Settings.fetch.DisplayCompleteTrajectory = inState;
        }
        #endregion

        #region Page methods for changing/updating the pages in the Gui page box
        /// <summary> Changes the page inside the page box. </summary>
        private void ChangePage(PageType inpage)
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
            switch ((PageType)Settings.fetch.MainGUICurrentPage)
            {
                case PageType.INFO:
                    UpdateInfoPage();
                    return;
                case PageType.TARGET:
                    UpdateTargetPage();
                    return;
                case PageType.DESCENT:
                    UpdateDescentPage();
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
            Trajectory.Patch lastPatch = Trajectory.fetch.patches.LastOrDefault();

            // max G-force
            max_gforce_txt = max_gforce_hdrtxt +
                    string.Format("{0:0.00}", Settings.fetch.DisplayTrajectories ? Trajectory.fetch.MaxAccel / 9.81 : 0);

            // impact values
            if (lastPatch != null && lastPatch.impactPosition.HasValue && Settings.fetch.DisplayTrajectories)
            {
                // calculate body offset position
                CelestialBody lastPatchBody = lastPatch.startingState.referenceBody;
                Vector3 position = lastPatch.impactPosition.Value + lastPatchBody.position;

                // impact position
                impact_position_txt = Localizer.Format("#autoLOC_Trajectories_ImpactPosition",
                    string.Format("{0:000.000000}",lastPatchBody.GetLatitude(position)),
                    string.Format("{0:000.000000}",lastPatchBody.GetLongitude(position)));

                // impact velocity
                Vector3 up = lastPatch.impactPosition.Value.normalized;
                Vector3 vel = lastPatch.impactVelocity.Value - lastPatchBody.getRFrmVel(position);
                float vVelMag = Vector3.Dot(vel, up);
                float hVelMag = (vel - (up * vVelMag)).magnitude;

                impact_velocity_txt = Localizer.Format("#autoLOC_Trajectories_ImpactVelocity",
                    string.Format("{0:0.0}", -vVelMag),
                    string.Format("{0:0.0}", hVelMag));
            }
            else
            {
                impact_position_txt = Localizer.Format("#autoLOC_Trajectories_ImpactPosition", "---", "---");
                impact_velocity_txt = Localizer.Format("#autoLOC_Trajectories_ImpactVelocity", "---", "---");
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

        /// <summary> Updates the strings used by the descent page to display changing values/data </summary>
        private static void UpdateDescentPage()
        {
        }

        /// <summary> Updates the strings used by the settings page to display changing values/data </summary>
        private static void UpdateSettingsPage()
        {
            Trajectory traj = Trajectory.fetch;

            // performance
            performance_txt = performance_hdrtxt +
                string.Format("{0:0.0}ms ({1:0.0})%", traj.ComputationTime * 1000.0f, (traj.ComputationTime / traj.GameFrameTime) * 100.0f);

            // num errors
            num_errors_txt = errors_hdrtxt + string.Format("{0:0}", traj.ErrorCount);
        }
        #endregion
    }
}
