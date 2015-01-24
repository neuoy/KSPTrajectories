using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Trajectories
{
#if DEBUG
    //This will kick us into the save called default and set the first vessel active
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class DebugFastStart : MonoBehaviour
    {
        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        private static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern System.IntPtr FindWindow(System.String className, System.String windowName);

        //use this variable for first run to avoid the issue with when this is true and multiple addons use it
        public static bool first = true;
        public void Start()
        {
            var windowPtr = FindWindow(null, "Kerbal Space Program");
            if(windowPtr != null)
                SetWindowText(windowPtr, "KSP - Trajectories Debug"); // this is handy to have AutoSizer or similar program correctly set the window position when launching

            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.ToLower().Contains("testautomation")))
                return;

            //only do it on the first entry to the menu
            if (first)
            {
                first = false;
                HighLogic.SaveFolder = "default";
                Game game = GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);

                if (game != null && game.flightState != null && game.compatible)
                {
                    Int32 FirstVessel;
                    Boolean blnFoundVessel = false;
                    for (FirstVessel = 0; FirstVessel < game.flightState.protoVessels.Count; FirstVessel++)
                    {
                        //This logic finds the first non-asteroid vessel
                        if (game.flightState.protoVessels[FirstVessel].vesselType != VesselType.SpaceObject &&
                            game.flightState.protoVessels[FirstVessel].vesselType != VesselType.Unknown &&
                            game.flightState.protoVessels[FirstVessel].vesselType != VesselType.Flag)
                        {
                            ////////////////////////////////////////////////////
                            //PUT ANY OTHER LOGIC YOU WANT IN HERE//
                            ////////////////////////////////////////////////////
                            blnFoundVessel = true;
                            break;
                        }
                    }
                    if (!blnFoundVessel)
                        FirstVessel = 0;

                    // warning : with KSP 0.90 and new NEAR version, skipping the KSC screen prevents NEAR from initializing correctly, so we can't directly load a vessel
                    //FlightDriver.StartAndFocusVessel(game, FirstVessel);
                }

                CheatOptions.InfiniteFuel = true;
            }
        }
    }
#endif
}
