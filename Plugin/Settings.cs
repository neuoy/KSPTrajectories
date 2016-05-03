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

        public Settings()
        {
            config = KSP.IO.PluginConfiguration.CreateForType<Settings>();
            config.load();

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
