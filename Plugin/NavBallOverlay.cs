/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

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
            if(trajectoryGuide != null)
                trajectoryGuide.renderer.enabled = enabled;

            if (trajectoryReference != null)
                trajectoryReference.renderer.enabled = enabled;
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

            
            
            GameObject navballGameObject = GameObject.Find("NavBall");
            Transform vectorsPivotTransform = navballGameObject.transform.FindChild("vectorsPivot");
            navball = navballGameObject.GetComponent<NavBall>();

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

            Vector3 right = Vector3.Cross(patch.impactVelocity, patch.impactPosition.Value).normalized;
            Vector3 behind = Vector3.Cross(right, patch.impactPosition.Value).normalized;

            Vector3 offset = targetPosition.Value - patch.impactPosition.Value;
            Vector2 offsetDir = new Vector2(Vector3.Dot(right, offset), Vector3.Dot(behind, offset));
            offsetDir *= 0.00005f; // 20km <-> 1 <-> 45° (this is purely indicative, no physical meaning, it would be very complicated to compute an actual correction angle as it depends on the spacecraft behavior in the atmosphere ; a small angle will suffice for a plane, but even a big angle might do almost nothing for a rocket)
            offsetDir.y = -offsetDir.y;

            Vector3d pos = FlightGlobals.ActiveVessel.GetWorldPos3D() - Trajectory.fetch.targetBody.position;
            Vector3d vel = FlightGlobals.ActiveVessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity
            float plannedAngleOfAttack = (float)DescentProfile.fetch.GetAngleOfAttack(Trajectory.fetch.targetBody, pos, vel);
            //Util.PostSingleScreenMessage("plannedAngleOfAttack", "plannedAngleOfAttack=" + (plannedAngleOfAttack * 180.0f / Mathf.PI));

            Vector3 up = pos.normalized;
            Vector3 velRight = Vector3.Cross(vel, up).normalized;
            //Vector3 horizon = Vector3.Cross(velRight, up).normalized;
            Vector3 velUp = Vector3.Cross(velRight, vel).normalized;

            //Vector3 referenceVector = FlightGlobals.ActiveVessel.obt_velocity.normalized;
            Vector3 referenceVector = vel.normalized * Mathf.Cos(plannedAngleOfAttack) + velUp * Mathf.Sin(plannedAngleOfAttack);

            trajectoryReference.transform.localPosition = (navball.attitudeGymbal * referenceVector).normalized * navBallRadius;

            Vector3 refUp = Vector3.Cross(velRight, referenceVector).normalized;
            Vector3 refRight = velRight;

            Vector3 guideDir = referenceVector + refUp * offsetDir.y + refRight * offsetDir.x;
            trajectoryGuide.transform.localPosition = (navball.attitudeGymbal * guideDir).normalized * navBallRadius;
        }
    }
}
