/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2018 S.Gray, (aka PiezPiedPy).

  This file is part of Trajectories.
  Trajectories is available under the terms of GPL-3.0-or-later.
  See the LICENSE.md file for more details.

  Trajectories is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  Trajectories is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

  You should have received a copy of the GNU General Public License
  along with Trajectories.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    /// <summary>
    /// Trajectory map view overlay handler
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class MapOverlay: MonoBehaviour
    {
        /// <summary>
        /// Trajectory map view renderer
        /// </summary>
        private sealed class MapTrajectoryRenderer: MonoBehaviour
        {
            public List<GameObject> meshes = new List<GameObject>();

            public void OnPreRender()
            {
                if (meshes != null)
                    fetch.RenderMesh();
            }

            /// <summary>
            /// Shows or hides the rendered trajectory on the map view
            /// </summary>
            public void Visible(bool show)
            {
                if (meshes != null)
                {
                    foreach (GameObject mesh in meshes)
                        mesh.GetComponent<MeshRenderer>().enabled = show;

                    enabled = show;
                }
            }
        }

        // constants
        private const float line_width = 3.0f;
        private const int layer2D = 31;
        private const int layer3D = 24;

        // Map trajectory material
        private static Material material;

        // Map trajectory renderer
        private static MapTrajectoryRenderer map_traj_renderer;

        // visible flag
        private static bool visible = false;

        // permit global access
        public static MapOverlay fetch
        {
            get; private set;
        } = null;

        //  constructor
        public MapOverlay()
        {
            // enable global access
            fetch = this;
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        private void Awake()
        {
            material = MapView.fetch.orbitLinesMaterial;
            map_traj_renderer = PlanetariumCamera.Camera.gameObject.AddComponent<MapTrajectoryRenderer>();
            map_traj_renderer.Visible(false);
        }

        private void OnDestroy()
        {
            if (map_traj_renderer != null)
                Destroy(map_traj_renderer);
            map_traj_renderer = null;
        }

        private void Update()
        {
            // return if no renderer or camera
            if ((map_traj_renderer == null) || (PlanetariumCamera.Camera == null))
            {
                visible = false;
                return;
            }

            // hide or show the Map trajectory
            if ((!Util.IsMap || !Settings.fetch.DisplayTrajectories) && visible)
            {
                //Debug.Log("Trajectories: Hide map trajectory");
                visible = false;
                map_traj_renderer.Visible(false);
                return;
            }
            else if (Util.IsMap && Settings.fetch.DisplayTrajectories && !visible)
            {
                //Debug.Log("Trajectories: Show map trajectory");
                visible = true;
                map_traj_renderer.Visible(true);
            }
        }

        /// <summary>
        /// Returns first found Non-Active mesh, if none are found then adds a new mesh to the end of the list.
        /// </summary>
        private GameObject GetMesh()
        {
            // find a Non-Active mesh in the list
            GameObject mesh_found = null;
            foreach (GameObject mesh in map_traj_renderer.meshes)
            {
                if (!mesh.activeSelf)
                {
                    mesh.SetActive(true);
                    mesh_found = mesh;
                    break;
                }
            }

            // create a new mesh if a Non-Active mesh is not found
            if (mesh_found == null)
            {
                //Debug.Log("Trajectories: Adding map trajectory mesh " + map_traj_renderer.meshes.Count);

                GameObject newMesh = new GameObject();
                newMesh.AddComponent<MeshFilter>();
                MeshRenderer renderer = newMesh.AddComponent<MeshRenderer>();
                renderer.enabled = visible;
                renderer.receiveShadows = false;
                newMesh.layer = MapView.Draw3DLines ? layer3D : layer2D;
                map_traj_renderer.meshes.Add(newMesh);

                mesh_found = newMesh;
            }

            mesh_found.GetComponent<Renderer>().sharedMaterial = material;

            return mesh_found;
        }

        private void RenderMesh()
        {
            // set all meshes to Non-Active to init mesh list search.
            foreach (GameObject mesh in map_traj_renderer.meshes)
            {
                mesh.SetActive(false);
            }

            // create/update meshes from Trajectory patches.
            foreach (Trajectory.Patch patch in Trajectory.fetch.Patches)
            {
                if (patch.StartingState.StockPatch != null && !Settings.fetch.BodyFixedMode &&
                    !Settings.fetch.DisplayCompleteTrajectory)
                    continue;

                if (patch.IsAtmospheric && patch.AtmosphericTrajectory.Length < 2)
                    continue;

                GameObject mesh_found = GetMesh();
                mesh_found.layer = MapView.Draw3DLines ? layer3D : layer2D;
                Mesh mesh = mesh_found.GetComponent<MeshFilter>().mesh;

                if (patch.IsAtmospheric)
                {
                    InitMeshFromTrajectory(patch.StartingState.ReferenceBody.position, mesh,
                        patch.AtmosphericTrajectory, Color.red);
                }
                else
                {
                    InitMeshFromOrbit(patch.StartingState.ReferenceBody.position, mesh, patch.SpaceOrbit,
                        patch.StartingState.Time, patch.EndTime - patch.StartingState.Time, Color.white);
                }

                // create/update red crosshair mesh from ImpactPosition.
                if (patch.ImpactPosition.HasValue)
                {
                    mesh_found = GetMesh();
                    mesh_found.layer = MapView.Draw3DLines ? layer3D : layer2D;
                    mesh = mesh_found.GetComponent<MeshFilter>().mesh;
                    InitMeshCrosshair(patch.StartingState.ReferenceBody, mesh, patch.ImpactPosition.Value, Color.red);
                }
            }

            // create/update green crosshair mesh from TargetPosition.
            Vector3? target_position = Trajectory.Target.WorldPosition;
            if (target_position.HasValue)
            {
                GameObject mesh_found = GetMesh();
                mesh_found.layer = MapView.Draw3DLines ? layer3D : layer2D;
                Mesh mesh = mesh_found.GetComponent<MeshFilter>().mesh;
                InitMeshCrosshair(Trajectory.Target.Body, mesh, target_position.Value, Color.green);
            }
        }

        private void MakeRibbonEdge(Vector3d prevPos, Vector3d edgeCenter, float width, Vector3[] vertices, int startIndex)
        {
            // Code taken from RemoteTech mod

            Camera camera = PlanetariumCamera.Camera;

            Vector3 start = camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(prevPos));
            Vector3 end = camera.WorldToScreenPoint(ScaledSpace.LocalToScaledSpace(edgeCenter));

            Vector3 segment = new Vector3(end.y - start.y, start.x - end.x, 0).normalized * (width * 0.5f);

            if (!MapView.Draw3DLines)
            {
                float dist = Screen.height / 2 + 0.01f;
                start.z = start.z >= 0.15f ? dist : -dist;
                end.z = end.z >= 0.15f ? dist : -dist;
            }

            Vector3 p0 = (end + segment);
            Vector3 p1 = (end - segment);

            if (MapView.Draw3DLines)
            {
                p0 = camera.ScreenToWorldPoint(p0);
                p1 = camera.ScreenToWorldPoint(p1);
            }

            vertices[startIndex + 0] = p0;
            vertices[startIndex + 1] = p1;

            // in 2D mode, if one point is in front of the screen and the other is behind, we don't draw the segment
            // (to achieve this, we draw degenerated triangles, i.e. triangles that have two identical vertices which
            // make them "flat")
            if (!MapView.Draw3DLines && (start.z > 0) != (end.z > 0))
            {
                vertices[startIndex + 0] = vertices[startIndex + 1];
                if (startIndex >= 2)
                    vertices[startIndex - 2] = vertices[startIndex - 1];
            }
        }

        private void InitMeshFromOrbit(Vector3 bodyPosition, Mesh mesh, Orbit orbit, double startTime, double duration, Color color)
        {
            int steps = 128;

            double prevTA = orbit.TrueAnomalyAtUT(startTime);
            double prevTime = startTime;

            double[] stepUT = new double[steps * 4];
            int utIdx = 0;
            double maxDT = Math.Max(1.0, duration / steps);
            double maxDTA = 2.0 * Math.PI / steps;
            stepUT[utIdx++] = startTime;
            while (true)
            {
                double time = prevTime + maxDT;
                for (int count = 0; count < 100; ++count)
                {
                    if (count == 99)
                        Debug.Log("Trajectories: *WARNING* Infinite loop in map view renderer. (InitMeshFromOrbit)");
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

                if (time > startTime + duration - (time - prevTime) * 0.5)
                    break;

                prevTime = time;

                stepUT[utIdx++] = time;
                if (utIdx >= stepUT.Length - 1)
                {
                    //Debug.Log("ut overflow", "ut overflow");
                    break; // this should never happen, but better stop than overflow if it does
                }
            }
            stepUT[utIdx++] = startTime + duration;

            Vector3[] vertices = new Vector3[utIdx * 2 + 2];
            Vector2[] uvs = new Vector2[utIdx * 2 + 2];
            int[] triangles = new int[utIdx * 6];

            Vector3 prevMeshPos = Util.SwapYZ(orbit.getRelativePositionAtUT(startTime - duration / steps)) + bodyPosition;
            for (int i = 0; i < utIdx; ++i)
            {
                double time = stepUT[i];

                Vector3 curMeshPos = Util.SwapYZ(orbit.getRelativePositionAtUT(time));
                if (Settings.fetch.BodyFixedMode)
                {
                    curMeshPos = Trajectory.CalculateRotatedPosition(orbit.referenceBody, curMeshPos, time);
                }
                curMeshPos += bodyPosition;

                // add a segment to the trajectory mesh
                MakeRibbonEdge(prevMeshPos, curMeshPos, line_width, vertices, i * 2);
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

            Color[] colors = new Color[vertices.Length];
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

        private void InitMeshFromTrajectory(Vector3 bodyPosition, Mesh mesh, Trajectory.Point[] trajectory, Color color)
        {
            Vector3[] vertices = new Vector3[trajectory.Length * 2];
            Vector2[] uvs = new Vector2[trajectory.Length * 2];
            int[] triangles = new int[(trajectory.Length - 1) * 6];

            Vector3 prevMeshPos = trajectory[0].pos - (trajectory[1].pos - trajectory[0].pos) + bodyPosition;
            for (int i = 0; i < trajectory.Length; ++i)
            {
                // the fixed-body rotation transformation has already been applied in AddPatch.
                Vector3 curMeshPos = trajectory[i].pos + bodyPosition;

                // add a segment to the trajectory mesh
                MakeRibbonEdge(prevMeshPos, curMeshPos, line_width, vertices, i * 2);
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

            Color[] colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; ++i)
                colors[i] = color;

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }

        private void InitMeshCrosshair(CelestialBody body, Mesh mesh, Vector3 position, Color color)
        {
            Vector3[] vertices = new Vector3[8];
            Vector2[] uvs = new Vector2[8];
            int[] triangles = new int[12];

            Vector3 camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position) - body.position;

            double impactDistFromBody = position.magnitude;
            double altitude = impactDistFromBody - body.Radius;
            altitude = altitude + 1200; // hack to avoid the crosshair being hidden under the ground in map view
            position *= (float)((body.Radius + altitude) / impactDistFromBody);

            Vector3 crossV1 = Vector3.Cross(position, Vector3.right).normalized;
            Vector3 crossV2 = Vector3.Cross(position, crossV1).normalized;

            float crossThickness = Mathf.Min(line_width * 0.001f * Vector3.Distance(camPos, position), 6000.0f);
            float crossSize = crossThickness * 10.0f;

            vertices[0] = position - crossV1 * crossSize + crossV2 * crossThickness;
            uvs[0] = new Vector2(0.8f, 1);
            vertices[1] = position - crossV1 * crossSize - crossV2 * crossThickness;
            uvs[1] = new Vector2(0.8f, 0);
            vertices[2] = position + crossV1 * crossSize + crossV2 * crossThickness;
            uvs[2] = new Vector2(0.8f, 1);
            vertices[3] = position + crossV1 * crossSize - crossV2 * crossThickness;
            uvs[3] = new Vector2(0.8f, 0);

            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 3;
            triangles[3] = 0;
            triangles[4] = 3;
            triangles[5] = 2;

            vertices[4] = position - crossV2 * crossSize - crossV1 * crossThickness;
            uvs[4] = new Vector2(0.8f, 0);
            vertices[5] = position - crossV2 * crossSize + crossV1 * crossThickness;
            uvs[5] = new Vector2(0.8f, 1);
            vertices[6] = position + crossV2 * crossSize - crossV1 * crossThickness;
            uvs[6] = new Vector2(0.8f, 0);
            vertices[7] = position + crossV2 * crossSize + crossV1 * crossThickness;
            uvs[7] = new Vector2(0.8f, 1);

            triangles[6] = 4;
            triangles[7] = 5;
            triangles[8] = 7;
            triangles[9] = 4;
            triangles[10] = 7;
            triangles[11] = 6;

            Color[] colors = new Color[vertices.Length];
            for (int i = 0; i < colors.Length; ++i)
                colors[i] = color;

            for (int i = 0; i < vertices.Length; ++i)
            {
                // in current implementation, impact positions are displayed only if MapView is in 3D mode
                // (i.e. not zoomed out too far)
                vertices[i] = MapView.Draw3DLines ?
                    (Vector3)ScaledSpace.LocalToScaledSpace(vertices[i] + body.position) : new Vector3(0, 0, 0);
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }
    }
}
