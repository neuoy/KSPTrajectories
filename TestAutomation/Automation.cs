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

        public void LateUpdate()
        {
            Automation.fetch.OnLateUpdate();
        }
    }

    class Automation
    {
        public static Automation fetch;

        private bool started = false;
        private bool autoPilotStarted = false;
        private int initialWait = 100; // this is needed to avoid conflict with some event that seems to happen in main menu even if we start a game

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

            if (started && HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null && !autoPilotStarted)
            {
                Debug.Log("Starting auto pilot");
                autoPilotStarted = true;
                FlightGlobals.ActiveVessel.OnFlyByWire += new FlightInputCallback(autoPilot);
                FlightGlobals.ActiveVessel.VesselSAS.ManualOverride(true);
            }
        }

        public void OnLateUpdate()
        {
            
        }

        private static void autoPilot(FlightCtrlState controls)
        {
            Vessel vessel = FlightGlobals.ActiveVessel;
            CelestialBody body = vessel.mainBody;
            Vector3d pos = vessel.GetWorldPos3D() - body.position;
            Vector3d airVelocity = vessel.obt_velocity - body.getRFrmVel(body.position + pos);

            Transform vesselTransform = vessel.ReferenceTransform;

            Vector3d vesselBackward = (Vector3d)(-vesselTransform.up.normalized);
            Vector3d vesselForward = -vesselBackward;
            Vector3d vesselUp = (Vector3d)(-vesselTransform.forward.normalized);
            Vector3d vesselRight = Vector3d.Cross(vesselUp, vesselBackward).normalized;

            Vector3d localVel = new Vector3d(Vector3d.Dot(vesselRight, airVelocity), Vector3d.Dot(vesselUp, airVelocity), Vector3d.Dot(vesselBackward, airVelocity));
            Vector3d prograde = localVel.normalized;

            Vector3d localGravityUp = new Vector3d(Vector3d.Dot(vesselRight, pos), Vector3d.Dot(vesselUp, pos), Vector3d.Dot(vesselBackward, pos)).normalized;

            float dirx = prograde.z > 0.0f ? Mathf.Sign((float)prograde.x) : (float)prograde.x;
            float diry = prograde.z > 0.0f ? Mathf.Sign((float)prograde.y) : (float)prograde.y;
            float dirz = localGravityUp.y < 0.0f ? Mathf.Sign((float)localGravityUp.x) : (float)localGravityUp.x;

            float warpDamp = 1.0f / TimeWarp.CurrentRate;

            controls.pitch = Mathf.Clamp(diry + vessel.angularVelocity.x, -1.0f, 1.0f) * warpDamp;
            controls.yaw = Mathf.Clamp(-dirx + vessel.angularVelocity.z, -1.0f, 1.0f) * warpDamp;
            controls.roll = Mathf.Clamp(-dirz + vessel.angularVelocity.y, -1.0f, 1.0f) * warpDamp;
        }
    }
}
