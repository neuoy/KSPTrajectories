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

        /// <summary> Toolbar button icon style</summary>
        public enum IconStyleType
        {
            NORMAL = 0,
            ACTIVE,
            AUTO
        }

        private static IconStyleType current_iconstyle;

        // Textures for icons (held here for better performance when switching icons on the stock toolbar)
        private static Texture2D normal_icon_texture;
        private static Texture2D active_icon_texture;
        private static Texture2D auto_icon_texture;

        // Toolbar buttons
        private static ApplicationLauncherButton stock_toolbar_button = null;
        private static IButton blizzy_toolbar_button = null;

        /// <summary> Return the current style of the toolbar button icon </summary>
        public static IconStyleType IconStyle
        {
            get
            {
                return current_iconstyle;
            }
        }

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
                active_icon_texture = GameDatabase.Instance.GetTexture("Trajectories/Textures/iconActive", false);
                auto_icon_texture = GameDatabase.Instance.GetTexture("Trajectories/Textures/iconAuto", false);

                if (Settings.fetch.DisplayTrajectories)
                    current_iconstyle = IconStyleType.ACTIVE;
                else
                    current_iconstyle = IconStyleType.NORMAL;

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
            active_icon_texture = null;
            auto_icon_texture = null;
            current_iconstyle = IconStyleType.NORMAL;
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

                if (current_iconstyle == IconStyleType.ACTIVE)
                    stock_toolbar_button.SetTexture(active_icon_texture);

                if (current_iconstyle == IconStyleType.AUTO)
                    stock_toolbar_button.SetTexture(auto_icon_texture);

                if (Settings.fetch.MainGUIEnabled || Settings.fetch.GUIEnabled)
                    stock_toolbar_button.SetTrue(false);

            }
        }

        /// <summary> Changes the toolbar button icon </summary>
        public static void ChangeIcon(IconStyleType iconstyle)
        {
            // no icons for blizzy yet so only change the current icon style
            if (ToolbarManager.ToolbarAvailable && Settings.fetch.UseBlizzyToolbar)
                switch (iconstyle)
                {
                    case IconStyleType.ACTIVE:
                        current_iconstyle = IconStyleType.ACTIVE;
                        break;
                    case IconStyleType.AUTO:
                        current_iconstyle = IconStyleType.AUTO;
                        break;
                    default:
                        current_iconstyle = IconStyleType.NORMAL;
                        break;
                }

            else switch (iconstyle)
            {
                case IconStyleType.ACTIVE:
                    stock_toolbar_button.SetTexture(active_icon_texture);
                    current_iconstyle = IconStyleType.ACTIVE;
                    break;
                case IconStyleType.AUTO:
                    stock_toolbar_button.SetTexture(auto_icon_texture);
                    current_iconstyle = IconStyleType.AUTO;
                    break;
                default:
                    stock_toolbar_button.SetTexture(normal_icon_texture);
                    current_iconstyle = IconStyleType.NORMAL;
                    break;
            }
        }
    }
}
