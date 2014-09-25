using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Framework;

namespace AutomatedTesting.Tests
{
    [KSPTest]
    class PodLanding
    {
        public void Run()
        {
            var ksp = new KSP();
            ksp.GameConfig = new TestGameConfig
            {
                SaveFile = "pod_landing.sfs",
                VesselId = "b7a8fcd07f424323b1d4be23057446ec",
                LandingZone = new Vector3(),
                LandingZoneRadius = 1000,
            };

            ksp.Mods = new ModInfo[]
            {
                new ModInfo("FerramAerospaceResearch", "0.14.1.1")
            };

            ksp.RunTest();
        }
    }
}
