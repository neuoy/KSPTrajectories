#if DEBUG && DEBUG_FASTSTART

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Trajectories
{

    //This will kick us into the save called default and set the first vessel active
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class DebugFastStart : MonoBehaviour
    {
        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        private static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern System.IntPtr FindWindow(System.String className, System.String windowName);

        //use this variable for first run to avoid the issue with when this is true and multiple addons use it
        private static bool first = true;
        public void Start()
        {
            if(HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                //only do it on the first entry to the menu
                if (first)
                {
                    first = false;

                    var windowPtr = FindWindow(null, "Kerbal Space Program");
                    if (windowPtr != null)
                        // this is handy to have AutoSizer or similar program correctly set the window position when launching
                        SetWindowText(windowPtr, "KSP - Trajectories Debug");

                    HighLogic.SaveFolder = "default";
                    HighLogic.LoadScene(GameScenes.SPACECENTER);

                    // When debugging, we are dirty cheating alpacas.
                    CheatOptions.InfiniteElectricity = true;
                    CheatOptions.InfinitePropellant = true;
                    CheatOptions.IgnoreMaxTemperature = true;
                }
            }
        }

        private static int loadedVessel = 100;
        public void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER && loadedVessel > 0)
            {
                Util.PostSingleScreenMessage("debug fast start", "count: " + loadedVessel);
                --loadedVessel;
                if (loadedVessel == 0)
                {
                    Game game = GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
                    if (game != null && game.flightState != null && game.compatible)
                    {
                        Int32 FirstVessel;
                        Boolean blnFoundVessel = false;
                        VesselType vesselType;

                        for (FirstVessel = 0; FirstVessel < game.flightState.protoVessels.Count; FirstVessel++)
                        {
                            vesselType = game.flightState.protoVessels[FirstVessel].vesselType;

                            //This logic finds the first non-asteroid vessel
                            if (vesselType == VesselType.Flag)
                                continue;
                            if (vesselType == VesselType.Debris)
                                continue;
                            if (vesselType == VesselType.SpaceObject)
                                continue;
                            if (vesselType == VesselType.Unknown)
                                continue;

                            ////////////////////////////////////////////////////
                            //PUT ANY OTHER LOGIC YOU WANT IN HERE//
                            ////////////////////////////////////////////////////

                            blnFoundVessel = true;
                            break;
                        }

                        if (!blnFoundVessel)
                            FirstVessel = 0;

                        FlightDriver.StartAndFocusVessel(game, FirstVessel);
                    }
                }
            }
        }
    }
}

#endif
