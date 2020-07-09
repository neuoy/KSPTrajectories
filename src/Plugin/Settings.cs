/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
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
using System.Reflection;
using KSP.IO;
using UnityEngine;

namespace Trajectories
{
    internal sealed class Settings
    {
        #region User settings
        private sealed class Persistent : Attribute
        {
            internal object DefaultValue;
            internal Persistent(object Default) => DefaultValue = Default;
        }

        [Persistent(Default: true)]
        internal static bool UseBlizzyToolbar { get; set; }

        [Persistent(Default: true)]
        internal static bool DisplayTrajectories { get; set; }

        [Persistent(Default: false)]
        internal static bool DisplayTrajectoriesInFlight { get; set; }

        [Persistent(Default: false)]
        internal static bool AlwaysUpdate { get; set; } //Compute trajectory even if DisplayTrajectories && MapView.MapIsEnabled == false.

        [Persistent(Default: false)]
        internal static bool DisplayCompleteTrajectory { get; set; }

        [Persistent(Default: false)]
        internal static bool BodyFixedMode { get; set; }

        [Persistent(Default: true)]
        internal static bool AutoUpdateAerodynamicModel { get; set; }

        [Persistent(Default: false)]
        internal static bool MainGUIEnabled { get; set; }

        [Persistent(Default: null)]
        internal static Vector2 MainGUIWindowPos { get; set; }

        [Persistent(Default: null)]
        internal static int MainGUICurrentPage { get; set; }

        [Persistent(Default: 2.0d)]
        internal static double IntegrationStepSize { get; set; }

        [Persistent(Default: 4)]
        internal static int MaxPatchCount { get; set; }

        [Persistent(Default: 15)]
        internal static int MaxFramesPerPatch { get; set; }

        [Persistent(Default: true)]
        internal static bool UseCache { get; set; }

        [Persistent(Default: true)]
        internal static bool DefaultDescentIsRetro { get; set; }
        #endregion

        private static PluginConfiguration config;
        private static bool ConfigError = false;

        //  constructor
        static Settings()
        {
            Util.DebugLog("");
            config ??= PluginConfiguration.CreateForType<Settings>();
        }

        internal static void Destroy()
        {
            Util.DebugLog("");
            config = null;
        }

        internal static void Load()
        {
            if (Trajectories.Settings == null)
                return;

            Util.Log("Loading settings");
            config ??= PluginConfiguration.CreateForType<Settings>();

            try
            {
                config.load();
            }
            catch (System.Xml.XmlException e)
            {
                if (ConfigError)
                    throw; // if previous error handling failed, we give up

                ConfigError = true;

                Util.LogError("Loading config: {0}", e.ToString());

                string TrajPluginPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Util.Log("Installed at: {0}", TrajPluginPath);
                TrajPluginPath += "/PluginData/" + System.Reflection.Assembly.GetExecutingAssembly().FullName + "/config.xml";
                if (System.IO.File.Exists(TrajPluginPath))
                {
                    Util.Log("Clearing config file...");
                    int idx = 1;
                    while (System.IO.File.Exists(TrajPluginPath + ".bak." + idx))
                        ++idx;
                    System.IO.File.Move(TrajPluginPath, TrajPluginPath + ".bak." + idx);

                    Util.Log("Creating new config...");
                    config.load();

                    Util.Log("New config created");
                }
                else
                {
                    Util.Log("No config file exists");
                    throw;
                }
            }

            Serialize();
        }

        internal static void Save()
        {
            if (Trajectories.Settings == null)
                return;
            Util.Log("Saving settings");
            Serialize(true);
        }

        private static void Serialize(bool write = false)
        {
            Util.DebugLog("");
            var props = from p in typeof(Settings).GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                        let attr = p.GetCustomAttributes(typeof(Persistent), true)
                        where attr.Length == 1
                        select new { Property = p, Attribute = attr.First() as Persistent };

            foreach (var prop in props)
            {
                if (write)
                    config.SetValue(prop.Property.Name, prop.Property.GetValue(Trajectories.Settings, null));
                else
                    prop.Property.SetValue(Trajectories.Settings, config.GetValue(prop.Property.Name, prop.Attribute.DefaultValue), null);
            }

            if (write)
                config.save();
        }
    }
}
