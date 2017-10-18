using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;

namespace Trajectories
{
    /// <summary>
    /// Handles the creation, destroying etc of an App start button for either KSP's stock toolbar or Blizzy's toolbar.
    /// </summary>
    public static class AppLauncherButton
    {
        private class BlizzyToolbarButtonVisibility: IVisibility
        {
            // permit global access
            private static BlizzyToolbarButtonVisibility instance = null;

            private static IVisibility flight_visibility;

            public static BlizzyToolbarButtonVisibility Instance
            {
                get
                {
                    return instance;
                }
            }

            //  constructor
            public BlizzyToolbarButtonVisibility()
            {
                // enable global access
                instance = this;

                flight_visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            }

            public bool Visible
            {
                get
                {
                    return flight_visibility.Visible;
                }
            }
        }

        private static Texture2D normal_icon_texture;

        // Toolbar buttons
        private static ApplicationLauncherButton stock_toolbar_button = null;
        private static IButton blizzy_toolbar_button = null;

        /// <summary> Creates the toolbar button for either a KSP stock toolbar or Blizzy toolbar if available. </summary>
        public static void Create()
        {
            if (ToolbarManager.ToolbarAvailable && Settings.fetch.UseBlizzyToolbar)
            {
                // setup a toolbar button for the blizzy toolbar
                Debug.Log("Using Blizzy toolbar for Trajectories");
                blizzy_toolbar_button = ToolbarManager.Instance.add(Localizer.Format("#autoLOC_Trajectories_Title"), "TrajectoriesGUI");
                blizzy_toolbar_button.Visibility = BlizzyToolbarButtonVisibility.Instance;
                blizzy_toolbar_button.TexturePath = "Trajectories/Textures/icon-blizzy";
                blizzy_toolbar_button.ToolTip = Localizer.Format("#autoLOC_Trajectories_AppButtonTooltip");
                blizzy_toolbar_button.OnClick += OnBlizzyToggle;
            }
            else
            {
                // setup a toolbar button for the stock toolbar
                Debug.Log("Using Stock toolbar for Trajectories");
                normal_icon_texture = GameDatabase.Instance.GetTexture("Trajectories/Textures/icon", false);
                GameEvents.onGUIApplicationLauncherReady.Add(delegate
                {
                    CreateStockToolbarButton();
                });
                GameEvents.onGUIApplicationLauncherUnreadifying.Add(delegate
                {
                    DestroyStockToolbarButton();
                });
            }
        }

        /// <summary> Destroys the toolbar button if it exists. </summary>
        public static void Destroy()
        {
            if (blizzy_toolbar_button != null)
                blizzy_toolbar_button.Destroy();
        }

        private static void OnBlizzyToggle(ClickEvent e)
        {
            if (e.MouseButton == 0)
            {
                // check that we have patched conics. If not, apologize to the user and return.
                if (!Util.isPatchedConicsAvailable)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_Trajectories_ConicsErr"));
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

        private static void OnStockTrue()
        {
                Settings.fetch.GUIEnabled = true;
        }

        private static void OnStockFalse()
        {
                Settings.fetch.GUIEnabled = false;
        }

        private static void DestroyStockToolbarButton()
        {
            if (stock_toolbar_button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(stock_toolbar_button);
                stock_toolbar_button = null;
            }
            normal_icon_texture = null;
        }

        private static void CreateStockToolbarButton()
        {
            if (stock_toolbar_button == null)
            {
                stock_toolbar_button = ApplicationLauncher.Instance.AddModApplication(
                    OnStockTrue,
                    OnStockFalse,
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.FLIGHT,
                    normal_icon_texture
                    );

                if (Settings.fetch.MainGUIEnabled || Settings.fetch.GUIEnabled)
                    stock_toolbar_button.SetTrue(false);

            }
        }
    }
}
