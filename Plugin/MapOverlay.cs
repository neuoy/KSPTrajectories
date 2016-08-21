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

		private float lineWidth = 3.0f;

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
            setDisplayEnabled((HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION) && MapView.MapIsEnabled && PlanetariumCamera.Camera != null);

            if (attachedCamera != null && (PlanetariumCamera.Camera == null || PlanetariumCamera.Camera.gameObject != attachedCamera))
            {
                DetachCamera();
            }

            if ((HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.TRACKSTATION) || !MapView.MapIsEnabled || PlanetariumCamera.Camera == null)
                return;

            if (listener == null)
            {
                Debug.Log("Trajectories: attaching camera listener");
                listener = PlanetariumCamera.Camera.gameObject.AddComponent<CameraListener>();
                listener.overlay = this;
                attachedCamera = PlanetariumCamera.Camera.gameObject;
            }
        }

        public void Render()
        {
            if ((HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.TRACKSTATION) || !MapView.MapIsEnabled || PlanetariumCamera.Camera == null)
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
                //renderer.castShadows = false;
                renderer.receiveShadows = false;
                newMesh.layer = 31;

                meshes.Add(newMesh);

                obj = newMesh;
            }

            obj.GetComponent<Renderer>().sharedMaterial = material;

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
			if (lineMaterial == null)
			{
				//    lineMaterial = new Material("Shader \"Vertex Colors/Alpha\" {Category{Tags {\"Queue\"=\"Transparent\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\"}SubShader {Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha Pass {BindChannels {Bind \"Color\", color Bind \"Vertex\", vertex}}}}}");
				lineMaterial = MapView.fetch.orbitLinesMaterial;
			}
			

            foreach (var patch in Trajectory.fetch.patches)
            {
                if (patch.startingState.stockPatch != null && !Settings.fetch.BodyFixedMode && !Settings.fetch.DisplayCompleteTrajectory)
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
                    initMeshFromImpact(patch.startingState.referenceBody, mesh, patch.impactPosition.Value, Color.red);
                }
            }

            Vector3? targetPosition = Trajectory.fetch.targetPosition;
            if (targetPosition.HasValue)
            {
                var obj = GetMesh(Trajectory.fetch.targetBody, lineMaterial);
                var mesh = obj.GetComponent<MeshFilter>().mesh;
                initMeshFromImpact(Trajectory.fetch.targetBody, mesh, targetPosition.Value, Color.green);
            }
        }

        private void MakeRibbonEdge(Vector3d prevPos, Vector3d edgeCenter, float width, Vector3[] vertices, int startIndex)
        {
            // Code taken from RemoteTech mod

            var camera = PlanetariumCamera.Camera;

            var start = camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(prevPos));
            var end = camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(edgeCenter));

            var segment = new Vector3(end.y - start.y, start.x - end.x, 0).normalized * (width  * 0.5f);

            if (!MapView.Draw3DLines)
            {
                var dist = Screen.height / 2 + 0.01f;
                start.z = start.z >= 0.15f ? dist : -dist;
                end.z = end.z >= 0.15f ? dist : -dist;
            }

            Vector3 p0 = (end + segment);
            Vector3 p1 = (end - segment);

            if(MapView.Draw3DLines)
            {
                p0 = camera.ScreenToWorldPoint(p0);
                p1 = camera.ScreenToWorldPoint(p1);
            }
            
            vertices[startIndex + 0] = p0;
            vertices[startIndex + 1] = p1;

            // in 2D mode, if one point is in front of the screen and the other is behind, we don't draw the segment (to achieve this, we draw degenerated triangles, i.e. triangles that have two identical vertices which make them "flat")
            if (!MapView.Draw3DLines && (start.z > 0) != (end.z > 0))
            {
                vertices[startIndex + 0] = vertices[startIndex + 1];
                if (startIndex >= 2)
                    vertices[startIndex - 2] = vertices[startIndex - 1];
            }
        }

        private void initMeshFromOrbit(Vector3 bodyPosition, Mesh mesh, Orbit orbit, double startTime, double duration, Color color)
        {
            int steps = 128;

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
                for (int count = 0; count < 100; ++count)
                {
                    if (count == 99)
                        Debug.Log("WARNING: infinite loop? (Trajectories.MapOverlay.initMeshFromOrbit)");
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
            var uvs = new Vector2[utIdx * 2 + 2];
            var triangles = new int[utIdx * 6];

            Vector3 prevMeshPos = Util.SwapYZ(orbit.getRelativePositionAtUT(startTime - duration / (double)steps)) + bodyPosition;
            for(int i = 0; i < utIdx; ++i)
            {
                double time = stepUT[i];

                Vector3 curMeshPos = Util.SwapYZ(orbit.getRelativePositionAtUT(time));
                if (Settings.fetch.BodyFixedMode) {
                    curMeshPos = Trajectory.calculateRotatedPosition(orbit.referenceBody, curMeshPos, time);
                }
                curMeshPos += bodyPosition;

                // add a segment to the trajectory mesh
                MakeRibbonEdge(prevMeshPos, curMeshPos, lineWidth, vertices, i * 2);
                uvs[i * 2 + 0] = new Vector2(0.8f, 0);
                uvs[i * 2 + 1] = new Vector2(0.8f, 1);

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

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.MarkDynamic();
        }

        private void initMeshFromTrajectory(Vector3 bodyPosition, Mesh mesh, Trajectory.Point[] trajectory, Color color)
        {
            var vertices = new Vector3[trajectory.Length * 2];
            var uvs = new Vector2[trajectory.Length * 2];
            var triangles = new int[(trajectory.Length-1) * 6];

            Vector3 prevMeshPos = trajectory[0].pos - (trajectory[1].pos-trajectory[0].pos) + bodyPosition;
            for(int i = 0; i < trajectory.Length; ++i)
            {
                // the fixed-body rotation transformation has already been applied in AddPatch.
                Vector3 curMeshPos = trajectory[i].pos + bodyPosition;

                // add a segment to the trajectory mesh
                MakeRibbonEdge(prevMeshPos, curMeshPos, lineWidth, vertices, i * 2);
                uvs[i * 2 + 0] = new Vector2(0.8f, 0);
                uvs[i * 2 + 1] = new Vector2(0.8f, 1);

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

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private void initMeshFromImpact(CelestialBody body, Mesh mesh, Vector3 impactPosition, Color color)
        {
            var vertices = new Vector3[8];
            var uvs = new Vector2[8];
            var triangles = new int[12];

            Vector3 camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position) - body.position;

			double impactDistFromBody = impactPosition.magnitude;
			double altitude = impactDistFromBody - body.Radius;
			altitude = altitude * 1.0 + 1200; // hack to avoid the cross being hidden under the ground in map view
			impactPosition *= (float)((body.Radius + altitude) / impactDistFromBody);

			Vector3 crossV1 = Vector3.Cross(impactPosition, Vector3.right).normalized;
            Vector3 crossV2 = Vector3.Cross(impactPosition, crossV1).normalized;
            
            float crossThickness = Mathf.Min(lineWidth * 0.001f * Vector3.Distance(camPos, impactPosition), 6000.0f);
            float crossSize = crossThickness * 10.0f;

            vertices[0] = impactPosition - crossV1 * crossSize + crossV2 * crossThickness; uvs[0] = new Vector2(0.8f, 1);
            vertices[1] = impactPosition - crossV1 * crossSize - crossV2 * crossThickness; uvs[1] = new Vector2(0.8f, 0);
            vertices[2] = impactPosition + crossV1 * crossSize + crossV2 * crossThickness; uvs[2] = new Vector2(0.8f, 1);
            vertices[3] = impactPosition + crossV1 * crossSize - crossV2 * crossThickness; uvs[3] = new Vector2(0.8f, 0);

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 3;
            triangles[3] = 0;
            triangles[4] = 3;
            triangles[5] = 2;

            vertices[4] = impactPosition - crossV2 * crossSize - crossV1 * crossThickness; uvs[4] = new Vector2(0.8f, 0);
            vertices[5] = impactPosition - crossV2 * crossSize + crossV1 * crossThickness; uvs[5] = new Vector2(0.8f, 1);
            vertices[6] = impactPosition + crossV2 * crossSize - crossV1 * crossThickness; uvs[6] = new Vector2(0.8f, 0);
            vertices[7] = impactPosition + crossV2 * crossSize + crossV1 * crossThickness; uvs[7] = new Vector2(0.8f, 1);

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
                // in current implementation, impact positions are displayed only if MapView is in 3D mode (i.e. not zoomed out too far)
                vertices[i] = MapView.Draw3DLines ? (Vector3)ScaledSpace.LocalToScaledSpace(vertices[i] + body.position) : new Vector3(0,0,0);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
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
