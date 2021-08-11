/*
  Copyright� (c) 2017-2020 S.Gray, (aka PiezPiedPy).

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

using System.IO;
using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;

namespace Trajectories
{
    /// <summary>
    /// Handles the creation, destroying etc of an App start button for either KSP's stock toolbar or Blizzy's toolbar.
    /// </summary>
    internal static class AppLauncherButton
    {
        private class BlizzyToolbarButtonVisibility : IVisibility
        {
            private static IVisibility flight_visibility;

            // permit global access
            public static BlizzyToolbarButtonVisibility fetch
            {
                get; private set;
            } = null;

            //  constructor
            public BlizzyToolbarButtonVisibility()
            {
                // enable global access
                fetch = this;

                flight_visibility = new GameScenesVisibility(GameScenes.FLIGHT);
            }

            public bool Visible => flight_visibility.Visible;
        }

        /// <summary> Toolbar button icon style</summary>
        internal enum IconStyleType
        {
            NORMAL = 0,
            ACTIVE,
            AUTO
        }

        // Textures for icons (held here for better performance when switching icons on the stock toolbar)
        private static Texture2D normal_icon_texture = null;
        private static Texture2D active_icon_texture = null;
        private static Texture2D auto_icon_texture = null;

        // Toolbar buttons
        private static ApplicationLauncherButton stock_toolbar_button = null;
        private static IButton blizzy_toolbar_button = null;

        private static bool constructed = false;

        private static bool StockTexturesAllocated => (normal_icon_texture != null && active_icon_texture != null && auto_icon_texture != null);
        /// <summary> Current style of the toolbar button icon </summary>
        internal static IconStyleType IconStyle { get; private set; } = IconStyleType.NORMAL;

        /// <summary> Creates the toolbar button for either a KSP stock toolbar or Blizzy toolbar if available. </summary>
        internal static void Start()
        {
            Util.DebugLog(constructed ? "Resetting" : "Constructing");

            if (Settings.DisplayTrajectories)
                IconStyle = IconStyleType.ACTIVE;
            else
                IconStyle = IconStyleType.NORMAL;

            if (ToolbarManager.ToolbarAvailable && Settings.UseBlizzyToolbar)
            {
                // setup a toolbar button for the blizzy toolbar
                Util.Log("Using Blizzy toolbar");
                blizzy_toolbar_button = ToolbarManager.Instance.add(Localizer.Format("#autoLOC_Trajectories_Title"), "TrajectoriesGUI");
                blizzy_toolbar_button.Visibility = BlizzyToolbarButtonVisibility.fetch;
                blizzy_toolbar_button.TexturePath = "Trajectories/Textures/icon-blizzy";
                blizzy_toolbar_button.ToolTip = Localizer.Format("#autoLOC_Trajectories_AppButtonTooltip");
                blizzy_toolbar_button.OnClick += OnBlizzyToggle;
            }
            else if (!constructed)
            {
                // setup a toolbar button for the stock toolbar
                Util.Log("Using KSP stock toolbar");
                string TrajTexturePath = KSPUtil.ApplicationRootPath + "GameData/Trajectories/Textures/";
                normal_icon_texture ??= new Texture2D(36, 36);
                active_icon_texture ??= new Texture2D(36, 36);
                auto_icon_texture ??= new Texture2D(36, 36);
                if (StockTexturesAllocated)
                {
                    normal_icon_texture.LoadImage(File.ReadAllBytes(TrajTexturePath + "icon.png"));
                    active_icon_texture.LoadImage(File.ReadAllBytes(TrajTexturePath + "iconActive.png"));
                    auto_icon_texture.LoadImage(File.ReadAllBytes(TrajTexturePath + "iconAuto.png"));
                }

                GameEvents.onGUIApplicationLauncherReady.Add(delegate { CreateStockToolbarButton(); });
                GameEvents.onGUIApplicationLauncherUnreadifying.Add(delegate { DestroyStockToolbarButton(); });
            }
            else
            {
                Util.Log("Using KSP stock toolbar");
            }

            constructed = true;
        }

        /// <summary> Releases held resources. </summary>
        internal static void Destroy()
        {
            Util.DebugLog("");
            DestroyToolbarButton();
            normal_icon_texture = null;
            active_icon_texture = null;
            auto_icon_texture = null;
            constructed = false;
        }

        internal static void DestroyToolbarButton()
        {
            Util.DebugLog("");
            DestroyBlizzyToolbarButton();
            DestroyStockToolbarButton();
            IconStyle = IconStyleType.NORMAL;
        }

        /// <summary> Destroys the blizzy toolbar button if it exists. </summary>
        private static void DestroyBlizzyToolbarButton()
        {
            if (blizzy_toolbar_button != null)
                blizzy_toolbar_button.Destroy();

            blizzy_toolbar_button = null;
        }

        private static void OnBlizzyToggle(ClickEvent e)
        {
            if (e.MouseButton == 0)
            {
                // check that we have patched conics. If not, apologize to the user and return.
                if (!Util.IsPatchedConicsAvailable)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_Trajectories_ConicsErr"));
                    Settings.DisplayTrajectories = false;
                    return;
                }

                Settings.DisplayTrajectories = !Settings.DisplayTrajectories;
            }
            else
            {
                Settings.MainGUIEnabled = !Settings.MainGUIEnabled;
            }
        }

        private static void OnStockTrue() => Settings.MainGUIEnabled = true;

        private static void OnStockFalse() => Settings.MainGUIEnabled = false;

        private static void DestroyStockToolbarButton()
        {
            Util.DebugLog("");
            if (stock_toolbar_button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(stock_toolbar_button);
                stock_toolbar_button = null;
            }
        }

        private static void CreateStockToolbarButton()
        {
            Util.DebugLog(!Util.IsFlight ? "Not a flight scene, skipping creation" : "");

            if (!Util.IsFlight)
                return;

            stock_toolbar_button ??= ApplicationLauncher.Instance.AddModApplication(
                OnStockTrue,
                OnStockFalse,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.FLIGHT,
                normal_icon_texture
                );

            if (stock_toolbar_button != null && StockTexturesAllocated)
            {
                if (IconStyle == IconStyleType.ACTIVE)
                    stock_toolbar_button.SetTexture(active_icon_texture);

                if (IconStyle == IconStyleType.AUTO)
                    stock_toolbar_button.SetTexture(auto_icon_texture);

                if (Settings.MainGUIEnabled)
                    stock_toolbar_button.SetTrue(false);
            }
        }

        /// <summary> Changes the toolbar button icon </summary>
        internal static void ChangeIcon(IconStyleType iconstyle)
        {
            // no icons for blizzy yet so only change the current icon style
            if (ToolbarManager.ToolbarAvailable && Settings.UseBlizzyToolbar)
                switch (iconstyle)
                {
                    case IconStyleType.ACTIVE:
                        IconStyle = IconStyleType.ACTIVE;
                        break;
                    case IconStyleType.AUTO:
                        IconStyle = IconStyleType.AUTO;
                        break;
                    default:
                        IconStyle = IconStyleType.NORMAL;
                        break;
                }

            else
                switch (iconstyle)
                {
                    case IconStyleType.ACTIVE:
                        if (stock_toolbar_button != null && StockTexturesAllocated)
                            stock_toolbar_button.SetTexture(active_icon_texture);
                        IconStyle = IconStyleType.ACTIVE;
                        break;
                    case IconStyleType.AUTO:
                        if (stock_toolbar_button != null && StockTexturesAllocated)
                            stock_toolbar_button.SetTexture(auto_icon_texture);
                        IconStyle = IconStyleType.AUTO;
                        break;
                    default:
                        if (stock_toolbar_button != null && StockTexturesAllocated)
                            stock_toolbar_button.SetTexture(normal_icon_texture);
                        IconStyle = IconStyleType.NORMAL;
                        break;
                }
        }
    }
}
