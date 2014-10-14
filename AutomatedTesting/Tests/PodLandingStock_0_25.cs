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
    class PodLandingStock_0_25
    {
        public void Run()
        {
            var ksp = new KSP() { Version = "0.25.0" };
            ksp.GameConfig = new TestAutomation.Config
            {
                SaveFile = "pod_landing_0_25",
                VesselId = "85ffa3d4dd104ec18467f67e02c21630",
                AoA = Mathf.PI
            };

            ksp.Mods = new ModInfo[]
            {
                new ModInfo { Name = "Squad", Version = "0.25.0_light" }
            };

            ksp.RunTest();
        }
    }
}
