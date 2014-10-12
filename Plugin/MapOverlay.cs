/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class MapOverlay : MonoBehaviour
    {
        private class CameraListener : MonoBehaviour
        {
            public MapOverlay overlay;

            public void OnPreRender()
            {
                overlay.Render();
            }
        }

        private GameObject attachedCamera;
        private CameraListener listener;
        private List<GameObject> meshes = new List<GameObject>();
        private bool displayEnabled = false;

        private Material lineMaterial;
        private float lineWidth = 0.002f;

        private void DetachCamera()
        {
            if (attachedCamera == null)
                return;

            Debug.Log("Trajectories: detaching camera listener");

            Destroy(listener);
            listener = null;
            attachedCamera = null;
        }

        public void Update()
        {
            setDisplayEnabled((HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION) && MapView.MapIsEnabled && MapView.MapCamera != null);

            if (attachedCamera != null && (MapView.MapCamera == null || MapView.MapCamera.gameObject != attachedCamera))
            {
                DetachCamera();
            }

            if ((HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.TRACKSTATION) || !MapView.MapIsEnabled || MapView.MapCamera == null)
                return;

            if (listener == null)
            {
                Debug.Log("Trajectories: attaching camera listener");
                listener = MapView.MapCamera.gameObject.AddComponent<CameraListener>();
                listener.overlay = this;
                attachedCamera = MapView.MapCamera.gameObject;
            }
        }

        public void Render()
        {
            if ((HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.TRACKSTATION) || !MapView.MapIsEnabled || MapView.MapCamera == null)
            {
                setDisplayEnabled(false);
                return;
            }

            setDisplayEnabled(true);
            refreshMesh();
        }

        private void setDisplayEnabled(bool enabled)
        {
            enabled = enabled && Settings.fetch.DisplayTrajectories;

            if (displayEnabled == enabled)
                return;
            displayEnabled = enabled;

            foreach (var mesh in meshes)
            {
                mesh.GetComponent<MeshRenderer>().enabled = enabled;
            }
        }

        private GameObject GetMesh(CelestialBody body, Material material)
        {
            GameObject obj = null;
            foreach (var mesh in meshes)
            {
                if (!mesh.activeSelf)
                {
                    mesh.SetActive(true);
                    obj = mesh;
                    break;
                }
            }

            if (obj == null)
            {
                //ScreenMessages.PostScreenMessage("adding trajectory mesh " + meshes.Count);

                var newMesh = new GameObject();
                newMesh.AddComponent<MeshFilter>();
                var renderer = newMesh.AddComponent<MeshRenderer>();
                renderer.enabled = displayEnabled;
                renderer.castShadows = false;
                renderer.receiveShadows = false;
                newMesh.layer = 10;

                meshes.Add(newMesh);

                obj = newMesh;
            }

            obj.renderer.sharedMaterial = material;

            return obj;
        }

        public void Clear()
        {
            foreach (var mesh in meshes)
            {
                GameObject.Destroy(mesh);
            }
        }

        private void refreshMesh()
        {
            foreach (var mesh in meshes)
            {
                mesh.SetActive(false);
            }

            // material from RemoteTech
            if(lineMaterial == null)
                lineMaterial = new Material("Shader \"Vertex Colors/Alpha\" {Category{Tags {\"Queue\"=\"Transparent\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\"}SubShader {Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha Pass {BindChannels {Bind \"Color\", color Bind \"Vertex\", vertex}}}}}");

            foreach (var patch in Trajectory.fetch.patches)
            {
                if (!patch.isDifferentFromStockTrajectory && !Settings.fetch.BodyFixedMode && !Settings.fetch.DisplayCompleteTrajectory)
                    continue;

                if (patch.isAtmospheric && patch.atmosphericTrajectory.Length < 2)
                    continue;

                var obj = GetMesh(patch.startingState.referenceBody, lineMaterial);
                var mesh = obj.GetComponent<MeshFilter>().mesh;
                
                if (patch.isAtmospheric)
                {
                    initMeshFromTrajectory(patch.startingState.referenceBody.position, mesh, patch.atmosphericTrajectory, Color.red);
                }
                else
                {
                    initMeshFromOrbit(patch.startingState.referenceBody.position, mesh, patch.spaceOrbit, patch.startingState.time, patch.endTime - patch.startingState.time, Color.white);
                }

                if (patch.impactPosition.HasValue)
                {
                    obj = GetMesh(patch.startingState.referenceBody, lineMaterial);
                    mesh = obj.GetComponent<MeshFilter>().mesh;
                    initMeshFromImpact(patch.startingState.referenceBody.position, mesh, patch.impactPosition.Value, Color.red);
                }
            }

            Vector3? targetPosition = Trajectory.fetch.targetPosition;
            if (targetPosition.HasValue)
            {
                var obj = GetMesh(Trajectory.fetch.targetBody, lineMaterial);
                var mesh = obj.GetComponent<MeshFilter>().mesh;
                initMeshFromImpact(Trajectory.fetch.patches[0].startingState.referenceBody.position, mesh, targetPosition.Value, Color.green);
            }
        }

        private void initMeshFromOrbit(Vector3 bodyPosition, Mesh mesh, Orbit orbit, double startTime, double duration, Color color)
        {
            int steps = 128;

            Vector3 camPos = ScaledSpace.ScaledToLocalSpace(MapView.MapCamera.transform.position) - bodyPosition;

            double prevTA = orbit.TrueAnomalyAtUT(startTime);
            double prevTime = startTime;

            double[] stepUT = new double[steps * 4];
            int utIdx = 0;
            double maxDT = Math.Max(1.0, duration / (double)steps);
            double maxDTA = 2.0 * Math.PI / (double)steps;
            stepUT[utIdx++] = startTime;
            while(true)
            {
                double time = prevTime + maxDT;
                while (true)
                {
                    double ta = orbit.TrueAnomalyAtUT(time);
                    while (ta < prevTA)
                        ta += 2.0 * Math.PI;
                    if (ta - prevTA <= maxDTA)
                    {
                        prevTA = ta;
                        break;
                    }
                    time = (prevTime + time) * 0.5;
                }

                if (time > startTime + duration - (time-prevTime) * 0.5)
                    break;

                prevTime = time;

                stepUT[utIdx++] = time;
                if (utIdx >= stepUT.Length - 1)
                {
                    //Util.PostSingleScreenMessage("ut overflow", "ut overflow");
                    break; // this should never happen, but better stop than overflow if it does
                }
            }
            stepUT[utIdx++] = startTime + duration;

            var vertices = new Vector3[utIdx * 2 + 2];
            var triangles = new int[utIdx * 6];

            Vector3 prevMeshPos = Util.SwapYZ(orbit.getRelativePositionAtUT(startTime - duration / (double)steps));
            for(int i = 0; i < utIdx; ++i)
            {
                double time = stepUT[i];

                Vector3 curMeshPos = Util.SwapYZ(orbit.getRelativePositionAtUT(time));
                if (Settings.fetch.BodyFixedMode) {
                    curMeshPos = Trajectory.calculateRotatedPosition(orbit.referenceBody, curMeshPos, time);
                }

                // compute an "up" vector that is orthogonal to the trajectory orientation and to the camera vector (used to correctly orient quads to always face the camera)
                Vector3 up = Vector3.Cross(curMeshPos - prevMeshPos, camPos - curMeshPos).normalized * (lineWidth * Vector3.Distance(camPos, curMeshPos));

                // add a segment to the trajectory mesh
                vertices[i * 2 + 0] = curMeshPos - up;
                vertices[i * 2 + 1] = curMeshPos + up;

                if (i > 0)
                {
                    int idx = (i - 1) * 6;
                    triangles[idx + 0] = (i - 1) * 2 + 0;
                    triangles[idx + 1] = (i - 1) * 2 + 1;
                    triangles[idx + 2] = i * 2 + 1;

                    triangles[idx + 3] = (i - 1) * 2 + 0;
                    triangles[idx + 4] = i * 2 + 1;
                    triangles[idx + 5] = i * 2 + 0;
                }

                prevMeshPos = curMeshPos;
            }

            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; ++i)
            {
                //if (color.g < 0.5)
                    colors[i] = color;
                /*else
                    colors[i] = new Color(0, (float)i / (float)colors.Length, 1.0f - (float)i / (float)colors.Length);*/
            }

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = ScaledSpace.LocalToScaledSpace(vertices[i] + bodyPosition);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private void initMeshFromTrajectory(Vector3 bodyPosition, Mesh mesh, Trajectory.Point[] trajectory, Color color)
        {
            var vertices = new Vector3[trajectory.Length * 2];
            var triangles = new int[(trajectory.Length-1) * 6];

            Vector3 camPos = ScaledSpace.ScaledToLocalSpace(MapView.MapCamera.transform.position) - bodyPosition;

            Vector3 prevMeshPos = trajectory[0].pos - (trajectory[1].pos-trajectory[0].pos);
            for(int i = 0; i < trajectory.Length; ++i)
            {
                Vector3 curMeshPos = trajectory[i].pos;
                // the fixed-body rotation transformation has already been applied in AddPatch.

                // compute an "up" vector that is orthogonal to the trajectory orientation and to the camera vector (used to correctly orient quads to always face the camera)
                Vector3 up = Vector3.Cross(curMeshPos - prevMeshPos, camPos - curMeshPos).normalized * (lineWidth * Vector3.Distance(camPos, curMeshPos));

                // add a segment to the trajectory mesh
                vertices[i * 2 + 0] = curMeshPos - up;
                vertices[i * 2 + 1] = curMeshPos + up;

                if (i > 0)
                {
                    int idx = (i - 1) * 6;
                    triangles[idx + 0] = (i - 1) * 2 + 0;
                    triangles[idx + 1] = (i - 1) * 2 + 1;
                    triangles[idx + 2] = i * 2 + 1;

                    triangles[idx + 3] = (i - 1) * 2 + 0;
                    triangles[idx + 4] = i * 2 + 1;
                    triangles[idx + 5] = i * 2 + 0;
                }

                prevMeshPos = curMeshPos;
            }

            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; ++i)
                colors[i] = color;

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = ScaledSpace.LocalToScaledSpace(vertices[i] + bodyPosition);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private void initMeshFromImpact(Vector3 bodyPosition, Mesh mesh, Vector3 impactPosition, Color color)
        {
            var vertices = new Vector3[8];
            var triangles = new int[12];

            Vector3 camPos = ScaledSpace.ScaledToLocalSpace(MapView.MapCamera.transform.position) - bodyPosition;

            Vector3 crossV1 = Vector3.Cross(impactPosition, Vector3.right).normalized;
            Vector3 crossV2 = Vector3.Cross(impactPosition, crossV1).normalized;
            
            float crossThickness = Mathf.Min(lineWidth * Vector3.Distance(camPos, impactPosition), 6000.0f);
            float crossSize = crossThickness * 10.0f;

            vertices[0] = impactPosition - crossV1 * crossSize + crossV2 * crossThickness;
            vertices[1] = impactPosition - crossV1 * crossSize - crossV2 * crossThickness;
            vertices[2] = impactPosition + crossV1 * crossSize + crossV2 * crossThickness;
            vertices[3] = impactPosition + crossV1 * crossSize - crossV2 * crossThickness;

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 3;
            triangles[3] = 0;
            triangles[4] = 3;
            triangles[5] = 2;

            vertices[4] = impactPosition - crossV2 * crossSize - crossV1 * crossThickness;
            vertices[5] = impactPosition - crossV2 * crossSize + crossV1 * crossThickness;
            vertices[6] = impactPosition + crossV2 * crossSize - crossV1 * crossThickness;
            vertices[7] = impactPosition + crossV2 * crossSize + crossV1 * crossThickness;

            triangles[6] = 4;
            triangles[7] = 5;
            triangles[8] = 7;
            triangles[9] = 4;
            triangles[10] = 7;
            triangles[11] = 6;

            var colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; ++i)
                colors[i] = color;

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = ScaledSpace.LocalToScaledSpace(vertices[i] + bodyPosition);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        public void OnDestroy()
        {
            Settings.fetch.Save();
            DetachCamera();
            Clear();
        }
    }
}
