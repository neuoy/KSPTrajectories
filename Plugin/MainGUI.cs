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
        }

        private void OnDestroy()
        {
            instance = null;
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
                );

            // create page box with current page inserted into page box
            switch ((PageType)Settings.fetch.MainGUICurrentPage)
            {
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
        }

        private void OnButtonClick_Target()
        {
        }

        private void OnButtonClick_Descent()
        {
        }

        private void OnButtonClick_Settings()
        {
        }
    }
}
