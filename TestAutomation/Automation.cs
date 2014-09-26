using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TestAutomation
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class AutomationPlugin : MonoBehaviour
    {
        public void Awake()
        {
            Automation.Startup();
        }

        public void Start()
        {
            Automation.fetch.OnSceneStart();
        }

        public void Update()
        {
            Automation.fetch.OnUpdate();
        }
    }

    class Automation
    {
        public static Automation fetch;

        private bool started = false;
        private int initialWait = 200; // this is needed to avoid conflict with some event that seems to happen in main menu even if we start a game

        private Config config;

        public static void Startup()
        {
            if (fetch == null)
                fetch = new Automation();
        }

        public Automation()
        {
            string data = File.ReadAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"/TestAutomation.json");
            config = JsonConvert.DeserializeObject<Config>(data);
        }

        public void OnSceneStart()
        {
            
        }

        public void OnUpdate()
        {
            if(HighLogic.LoadedScene == GameScenes.MAINMENU)
                --initialWait;
            if (initialWait > 0)
                return;

            if (!started && HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                started = true;

                HighLogic.SaveFolder = "default";
                Game game = GamePersistence.LoadGame(config.SaveFile, HighLogic.SaveFolder, true, false);

                if (game != null && game.flightState != null && game.compatible)
                {
                    ProtoVessel vessel = game.flightState.protoVessels.First(v => v.vesselID == new Guid(config.VesselId));
                    Debug.Log("Loading vessel: " + vessel.vesselName);
                    FlightDriver.StartAndFocusVessel(game, game.flightState.protoVessels.IndexOf(vessel));
                }
                else
                {
                    throw new Exception("Can't load save file "+config.SaveFile);
                }
            }
        }
    }
}
