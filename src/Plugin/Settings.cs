/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
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

using System;
using System.Linq;
using UnityEngine;

namespace Trajectories
{
    class Settings
    {
        private class Persistent: Attribute
        {
            public object DefaultValue;
            public Persistent(object Default) { DefaultValue = Default; }
        }

        public static Settings fetch { get { settings_ = settings_ ?? new Settings(); return settings_; } }

        #region User settings
        [Persistent(Default: false)]
        public bool DisplayTargetGUI { get; set; }

        [Persistent(Default: false)]
        public bool DisplayDescentProfileGUI { get; set; }

        [Persistent(Default: false)]
        public bool DisplaySettingsGUI { get; set; }

        [Persistent(Default: true)]
        public bool UseBlizzyToolbar { get; set; }

        [Persistent(Default: true)]
        public bool DisplayTrajectories { get; set; }

        [Persistent(Default: false)]
        public bool DisplayTrajectoriesInFlight { get; set; }

        [Persistent(Default: false)]
        public bool AlwaysUpdate { get; set; } //Compute trajectory even if DisplayTrajectories && MapView.MapIsEnabled == false.

        [Persistent(Default: false)]
        public bool DisplayCompleteTrajectory { get; set; }

        [Persistent(Default: false)]
        public bool BodyFixedMode { get; set; }

        [Persistent(Default: true)]
        public bool AutoUpdateAerodynamicModel { get; set; }

        [Persistent(Default: null)]
        public Rect MapGUIWindowPos { get; set; }

        [Persistent(Default: false)]
        public bool MainGUIEnabled { get; set; }

        [Persistent(Default: null)]
        public Vector2 MainGUIWindowPos { get; set; }

        [Persistent(Default: null)]
        public int MainGUICurrentPage { get; set; }

        [Persistent(Default: false)]
        public bool GUIEnabled { get; set; }

        [Persistent(Default: true)]
        public bool NewGui { get; set; }

        [Persistent(Default: 2.0d)]
        public double IntegrationStepSize { get; set; }

        [Persistent(Default: 4)]
        public int MaxPatchCount { get; set; }

        [Persistent(Default: 15)]
        public int MaxFramesPerPatch { get; set; }

        [Persistent(Default: true)]
        public bool UseCache { get; set; }

        [Persistent(Default: true)]
        public bool DefaultDescentIsRetro { get; set; }
        #endregion

        private KSP.IO.PluginConfiguration config;

        private static bool ConfigError = false;

        public Settings()
        {
            config = KSP.IO.PluginConfiguration.CreateForType<Settings>();
            try
            {
                config.load();
            }
            catch (System.Xml.XmlException e)
            {
                if (ConfigError)
                    throw; // if previous error handling failed, we give up

                ConfigError = true;

                Debug.Log("Error loading Trajectories config: " + e.ToString());

                string TrajPluginPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                Debug.Log("Trajectories installed in: " + TrajPluginPath);
                if (System.IO.File.Exists(TrajPluginPath + "/PluginData/Trajectories/config.xml"))
                {
                    Debug.Log("Clearing config file...");
                    int idx = 1;
                    while (System.IO.File.Exists(TrajPluginPath + "/PluginData/Trajectories/config.xml.bak." + idx))
                        ++idx;
                    System.IO.File.Move(TrajPluginPath + "/PluginData/Trajectories/config.xml", TrajPluginPath + "/PluginData/Trajectories/config.xml.bak." + idx);

                    Debug.Log("Creating new config...");
                    config.load();

                    Debug.Log("New config created");
                }
                else
                {
                    Debug.Log("No config file exists");
                    throw;
                }
            }

            Serialize(false);

            MapGUIWindowPos = new Rect(MapGUIWindowPos.xMin, MapGUIWindowPos.yMin, 1, MapGUIWindowPos.height); // width will be auto-sized to fit contents
        }

        public void Save()
        {
            Debug.Log("Trajectories: Saving settings.");
            Serialize(true);
        }

        private void Serialize(bool write)
        {
            var props = from p in this.GetType().GetProperties()
                        let attr = p.GetCustomAttributes(typeof(Persistent), true)
                        where attr.Length == 1
                        select new { Property = p, Attribute = attr.First() as Persistent };

            foreach (var prop in props)
            {
                if (write)
                    config.SetValue(prop.Property.Name, prop.Property.GetValue(this, null));
                else
                    prop.Property.SetValue(this, config.GetValue<object>(prop.Property.Name, prop.Attribute.DefaultValue), null);
            }

            if (write)
                config.save();
        }

        private static Settings settings_;
    }
}
