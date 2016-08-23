using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    class Settings
    {
        private class Persistent : Attribute
        {
            public object DefaultValue;
            public Persistent(object Default) { DefaultValue = Default; }
        }

        public static Settings fetch { get { settings_ = settings_ ?? new Settings(); return settings_; } }

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
        public bool GUIEnabled { get; set; }

        [Persistent(Default: 4)]
        public int MaxPatchCount { get; set; }

        [Persistent(Default: 15)]
        public int MaxFramesPerPatch { get; set; }

        [Persistent(Default: true)]
        public bool UseCache { get; set; }

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
                if(write)
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
