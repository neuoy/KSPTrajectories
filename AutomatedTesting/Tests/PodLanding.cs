using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Framework;
using UnityEngine;

namespace AutomatedTesting.Tests
{
    [KSPTest]
    class PodLanding
    {
        public void Run()
        {
            var ksp = new KSP() { Version = "0.24.2" };
            ksp.GameConfig = new TestAutomation.Config
            {
                SaveFile = "pod_landing",
                VesselId = "b7a8fcd07f424323b1d4be23057446ec",
                LandingZoneRadius = 1000,
            };

            ksp.Mods = new ModInfo[]
            {
                new ModInfo { Name = "FerramAerospaceResearch", Version = "0.14.1.1" },
                new ModInfo { Name = "Squad", Version = "0.24.2_light" }
            };

            ksp.RunTest();
        }
    }
}
