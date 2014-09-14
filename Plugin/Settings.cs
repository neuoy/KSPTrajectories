using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    class Settings
    {
        public static Settings fetch { get { settings_ = settings_ ?? new Settings(); return settings_; } }

        public bool DisplayTrajectories { get; set; }
        public bool AutoUpdateAerodynamicModel { get; set; }
        public Rect MapGUIWindowPos { get; set; }

        public Settings()
        {
            DisplayTrajectories = true;
            AutoUpdateAerodynamicModel = true;
        }

        private static Settings settings_;
    }
}
