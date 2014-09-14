using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class MapGUI : MonoBehaviour
    {
        private static readonly int GUIId = 934824;

        private ApplicationLauncherButton GUIToggleButton = null;
        private bool GUIEnabled = false;

        public void OnGUI()
        {
            if(!GUIEnabled)
                return;

            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;

            if (!MapView.MapIsEnabled || MapView.MapCamera == null)
                return;

            Settings.fetch.MapGUIWindowPos = ClampToScreen( GUILayout.Window(GUIId + 1, Settings.fetch.MapGUIWindowPos, MainWindow, "Trajectories") );
        }

        private void MainWindow(int id)
        {
            Settings.fetch.DisplayTrajectories = GUILayout.Toggle(Settings.fetch.DisplayTrajectories, "Display trajectory");

            GUI.enabled = Trajectory.fetch.targetPosition.HasValue;
            if (GUILayout.Button("Unset target"))
                Trajectory.SetTarget();
            GUI.enabled = true;

            var patch = Trajectory.fetch.patches.LastOrDefault();
            GUI.enabled = (patch != null && patch.impactPosition.HasValue);
            if (GUILayout.Button("Set current impact as target"))
            {
                Trajectory.SetTarget(patch.startingState.referenceBody, patch.impactPosition);
            }
            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            Settings.fetch.AutoUpdateAerodynamicModel = GUILayout.Toggle(Settings.fetch.AutoUpdateAerodynamicModel, "Auto update");
            if (GUILayout.Button("Update now"))
                Trajectory.fetch.InvalidateAerodynamicModel();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Descent profile");
            DescentProfile.fetch.DoGUI();

            GUI.DragWindow();
        }

        public void Awake()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
        }

        void OnGUIAppLauncherReady()
        {
            if (ApplicationLauncher.Ready)
            {
                GUIToggleButton = ApplicationLauncher.Instance.AddModApplication(
                    OnToggleOn,
                    OnToggleOff,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    ApplicationLauncher.AppScenes.MAPVIEW,
                    (Texture)GameDatabase.Instance.GetTexture("Trajectories/Textures/icon", false));
            }
        }

        void DummyVoid() { }

        void OnToggleOn()
        {
            GUIEnabled = true;
        }

        void OnToggleOff()
        {
            GUIEnabled = false;
        }

        void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            if (GUIToggleButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(GUIToggleButton);
        }

        private static Rect ClampToScreen(Rect window)
        {
            Util.PostSingleScreenMessage("tmp", window.x.ToString());
            return new Rect(Mathf.Clamp(window.x, 0, Screen.width - window.width), Mathf.Clamp(window.y, 0, Screen.height - window.height), window.width, window.height);
        }
    }
}
