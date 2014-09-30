using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TestAutomation
{
    public class Config
    {
        public Config()
        {
            PhysicsWarpRate = 4;
            LandingZoneRadius = 3000.0f;
        }

        public string SaveFile { get; set; }
        public string VesselId { get; set; }
        public float LandingZoneRadius { get; set; } // in meters
        public float PhysicsWarpRate { get; set; }
        public float AoA { get; set; } // in radians
    }
}
