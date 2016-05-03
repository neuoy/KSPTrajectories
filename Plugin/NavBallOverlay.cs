/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

using KSP.UI.Screens.Flight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    // Display indications on the navball. Code inspired from Enhanced NavBall mod.
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class NavBallOverlay : MonoBehaviour
    {
        private GameObject trajectoryGuide;
        private GameObject trajectoryReference;
        private float navBallRadius = 0.0f;
        private NavBall navball;

        public void Update()
        {
            if ((HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.TRACKSTATION) || !FlightGlobals.ActiveVessel)
            {
                SetDisplayEnabled(false);
                return;
            }

            UpdateNavBall();
        }

        private void SetDisplayEnabled(bool enabled)
        {
            if (trajectoryGuide == null || trajectoryReference == null)
                return;

            var guideRenderer = trajectoryGuide.GetComponent<Renderer>();
            var referenceRenderer = trajectoryReference.GetComponent<Renderer>();

            guideRenderer.enabled = enabled;
            referenceRenderer.enabled = enabled;
        }

        private void Init()
        {
            /*trajectoryGuide = new GameObject();
            trajectoryGuide.layer = 12;
            MeshFilter meshFilter = trajectoryGuide.AddComponent<MeshFilter>();
            trajectoryGuide.AddComponent<MeshRenderer>();
            Mesh mesh = meshFilter.mesh;

            float width = 0.25f;
            float height = 0.5f;

            var vertices = new Vector3[4];
            vertices[0] = new Vector3();

            vertices[0] = new Vector3(-width * 0.5f, 0, height);
            vertices[1] = new Vector3(width * 0.5f, 0, height);
            vertices[2] = new Vector3(-width * 0.5f, 0, 0);
            vertices[3] = new Vector3(width * 0.5f, 0, 0);

            mesh.vertices = vertices;

            mesh.triangles = new[]
            {
                0, 1, 2,
                3, 4, 5
            };

            mesh.RecalculateBounds();
            mesh.Optimize();*/

            
            
            GameObject navballGameObject = FlightUIModeController.Instance.navBall.gameObject;
            Transform vectorsPivotTransform = navballGameObject.transform.FindChild("vectorsPivot");
            navball = navballGameObject.GetComponentInChildren<NavBall>();

            trajectoryGuide = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            trajectoryGuide.layer = 12;
            trajectoryGuide.transform.parent = navball.transform.FindChild("vectorsPivot");
            trajectoryGuide.transform.localScale = Vector3.one * 0.004f;

            trajectoryReference = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trajectoryReference.layer = 12;
            trajectoryReference.transform.parent = navball.transform.FindChild("vectorsPivot");
            trajectoryReference.transform.localScale = Vector3.one * 0.004f;
        }

        private void UpdateNavBall()
        {
            Vector3? targetPosition = Trajectory.fetch.targetPosition;
            var patch = Trajectory.fetch.patches.LastOrDefault();
            CelestialBody body = Trajectory.fetch.targetBody;
            if (!targetPosition.HasValue || patch == null || !patch.impactPosition.HasValue || patch.startingState.referenceBody != body)
            {
                SetDisplayEnabled(false);
                return;
            }

            if (trajectoryGuide == null)
                Init();

            SetDisplayEnabled(true);

            if(navBallRadius == 0.0f)
                navBallRadius = navball.progradeVector.localPosition.magnitude;

            Vector3 referenceVector = AutoPilot.fetch.PlannedDirection;
            trajectoryReference.transform.localPosition = (navball.attitudeGymbal * referenceVector).normalized * navBallRadius;
            trajectoryReference.SetActive(trajectoryReference.transform.localPosition.z > 0); // hide if behind navball

            Vector3 guideDir = AutoPilot.fetch.CorrectedDirection;
            trajectoryGuide.transform.localPosition = (navball.attitudeGymbal * guideDir).normalized * navBallRadius;
            trajectoryGuide.SetActive(trajectoryGuide.transform.localPosition.z > 0); // hide if behind navball
        }
    }
}
