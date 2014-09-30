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
    class RocketBackwardLandingKSCNEAR
    {
        public void Run()
        {
            var ksp = new KSP() { Version = "0.24.2" };
            ksp.GameConfig = new TestAutomation.Config
            {
                SaveFile = "rocket_backward_landing_KSC",
                VesselId = "b7a8fcd07f424323b1d4be23057446ec",
                AoA = Mathf.PI
            };

            ksp.Mods = new ModInfo[]
            {
                new ModInfo { Name = "NEAR", Version = "1.0.3.0" },
                new ModInfo { Name = "Squad", Version = "0.24.2_light" }
            };

            ksp.RunTest();
        }
    }
}
