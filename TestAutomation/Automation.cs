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
        private int waitTrajectory = 100; // Some aerodynamic models need a few frames to initialize

        private Config config;

        private string logFile;

        private Vector3 predictedPosition;
        private Vector3 lastPosition;

        public static void Startup()
        {
            if (fetch == null)
                fetch = new Automation();
        }

        public Automation()
        {
            logFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/TestAutomation.log";
            File.WriteAllText(logFile, "");
            LogLine("Initializing TestAutomation");

            string data = File.ReadAllText(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"/TestAutomation.json");
            config = JsonConvert.DeserializeObject<Config>(data);
        }

        public void LogLine(string msg)
        {
            DateTime now = DateTime.UtcNow;
            msg = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ ") + msg;
            Debug.Log("TestAutomation: " + msg);
            File.AppendAllText(logFile, msg + "\n");
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
                    LogLine("Loading vessel: " + vessel.vesselName);
                    FlightDriver.StartAndFocusVessel(game, game.flightState.protoVessels.IndexOf(vessel));
                }
                else
                {
                    throw new Exception("Can't load save file "+config.SaveFile);
                }
            }

            if (started && HighLogic.LoadedScene == GameScenes.FLIGHT && FlightGlobals.ActiveVessel != null)
            {
                TrajectoriesAPI.TrajectoriesAPI.GetTrajectory(FlightGlobals.ActiveVessel).GetRawImpactPosition();
                --waitTrajectory;
            }
            if (waitTrajectory > 0)
                return;

            if (!autoPilotStarted)
            {
                LogLine("Starting auto pilot");
                autoPilotStarted = true;
                FlightGlobals.ActiveVessel.OnFlyByWire += new FlightInputCallback(autoPilot);
                FlightGlobals.ActiveVessel.VesselSAS.ManualOverride(true);

                try
                {
                    Vector3? predictedImpact = TrajectoriesAPI.TrajectoriesAPI.GetTrajectory(FlightGlobals.ActiveVessel).GetRawImpactPosition();
                    if (!predictedImpact.HasValue)
                        Terminate(false, "The Trajectories mod did not return an impact position on the current body");
                    predictedPosition = predictedImpact.Value;
                    LogLine("Predicted impact position: " + predictedImpact.ToString());
                }
                catch (Exception e)
                {
                    Terminate(false, "Failed to get impact position: " + e.ToString());
                }

                LogLine("Setting time warp");
                TimeWarp timeWarp = (TimeWarp)UnityEngine.Object.FindObjectOfType(typeof(TimeWarp));
                timeWarp.physicsWarpRates[3] = config.PhysicsWarpRate;
                TimeWarp.SetRate(3, false);
            }
            else
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                if (vessel == null || vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED || vessel.Parts.Count == 0)
                {
                    fetch.LogLine("Vessel destroyed or landed");
                    float distanceFromPrediction = Vector3.Distance(lastPosition, predictedPosition);
                    LogLine("Last known position: " + lastPosition.ToString() + "(" + distanceFromPrediction + "m away from prediction)");
                    if (distanceFromPrediction > config.LandingZoneRadius)
                        Terminate(false, "Too far away from predicted landing zone");
                    else
                        Terminate(true, "Predicted impact position reached");
                }
                else
                {
                    lastPosition = vessel.GetWorldPos3D() - vessel.mainBody.position;
                    Vector3 newPrediction = TrajectoriesAPI.TrajectoriesAPI.GetTrajectory(FlightGlobals.ActiveVessel).GetRawImpactPosition().Value;
                    PostSingleScreenMessage("prediction dist", "dist=" + (int)Vector3.Distance(lastPosition, predictedPosition) + ", updated prediction dist=" + (int)Vector3.Distance(newPrediction, predictedPosition));
                }
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

            if (TimeWarp.CurrentRateIndex == 0) // this happens when entering atmosphere
            {
                TimeWarp.SetRate(3, false);
            }
        }

        public void Terminate(bool success, string message)
        {
            LogLine("Test result: " + (success ? "success" : "failure") + ", with message: " + message);
            Application.Quit();
            throw new Exception("TestAutomation: Terminating application");
        }

        private static Dictionary<string, ScreenMessage> messages = new Dictionary<string, ScreenMessage>();
        public static void PostSingleScreenMessage(string id, string message)
        {
            if (messages.ContainsKey(id))
                ScreenMessages.RemoveMessage(messages[id]);
            messages[id] = ScreenMessages.PostScreenMessage(message);
        }
    }
}
