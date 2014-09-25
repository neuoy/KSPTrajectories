using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
#if DEBUG
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class TestAutomationPlugin : MonoBehaviour
    {
        public void Awake()
        {
            TestAutomation.Startup();
        }

        public void Start()
        {
            TestAutomation.fetch.OnSceneStart();
        }

        public void Update()
        {
            TestAutomation.fetch.OnUpdate();
        }
    }

    class TestAutomation
    {
        public static TestAutomation fetch;
        public bool IsActive { get { return true; } }

        private string SaveFile { get { return "persistent"; } }
        private Guid VesselId { get { return new Guid("b7a8fcd07f424323b1d4be23057446ec"); } }

        private bool started = false;
        private int initialWait = 300; // this is needed to avoid conflict with some event that seems to happen in main menu even if we start a game

        public static void Startup()
        {
            if (fetch == null)
                fetch = new TestAutomation();
        }

        public void OnSceneStart()
        {
            
        }

        public void OnUpdate()
        {
            --initialWait;
            if (initialWait > 0)
                return;

            if (!started && HighLogic.LoadedScene == GameScenes.MAINMENU && IsActive)
            {
                started = true;

                HighLogic.SaveFolder = "default";
                Game game = GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);

                if (game != null && game.flightState != null && game.compatible)
                {
                    ProtoVessel vessel = game.flightState.protoVessels.First(v => v.vesselID == VesselId);
                    Debug.Log("Loading vessel: " + vessel.vesselName);
                    FlightDriver.StartAndFocusVessel(game, game.flightState.protoVessels.IndexOf(vessel));
                }
                else
                {
                    throw new Exception("Can't load save file");
                }
            }
        }
    }
#endif
}
