/*
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2020 S.Gray, (aka PiezPiedPy).

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

using UnityEngine;

namespace Trajectories
{
    internal static class FlightOverlay
    {
        private sealed class TrajectoryLine : MonoBehaviour
        {
            internal const float MIN_WIDTH = 0.025f;
            internal const float MAX_WIDTH = 250f;
            internal const float DIST_DIV = 1e3f;

            private LineRenderer line_renderer;
            private Material material;
            private Vector3 cam_pos;

            private bool Ready => (gameObject && line_renderer && material);
#if !KSP13
            private int Count => Ready ? line_renderer.positionCount : 0;
#else
            private Vector3 start = Vector3.zero;
            private Vector3 end = Vector3.zero;
            private int vertex_count = 0;
            private int Count => Ready ? vertex_count : 0;
#endif
            internal void Awake()
            {
                if (!gameObject)
                    return;

                line_renderer = gameObject.GetComponent<LineRenderer>();
                if (!line_renderer)
                {
                    gameObject.AddComponent<LineRenderer>();
                    line_renderer = gameObject.GetComponent<LineRenderer>();
                }
                if (!line_renderer)
                    return;

                material ??= new Material(Shader.Find("KSP/Particles/Additive"));
                material ??= new Material(Shader.Find("Particles/Additive"));
                if (!material)
                    return;

                line_renderer.enabled = false;
                line_renderer.material = material;
#if !KSP13
                line_renderer.positionCount = 0;
                line_renderer.startColor = Color.blue;
                line_renderer.endColor = Color.blue;
                line_renderer.numCapVertices = 5;
                line_renderer.numCornerVertices = 7;
                line_renderer.startWidth = MIN_WIDTH;
                line_renderer.endWidth = MIN_WIDTH;

#else
                vertex_count = 0;
                line_renderer.SetVertexCount(0);
                line_renderer.SetColors(Color.blue, Color.blue);
                line_renderer.SetWidth(MIN_WIDTH, MIN_WIDTH);
                start = Vector3.zero;
                end = Vector3.zero;
#endif
            }

            internal void OnPreRender()
            {
                if (Util.IsPaused || !Util.IsFlight)
                    return;

                if (!Ready)
                    Awake();

                // adjust line width according to its distance from the camera
                if (Ready && (Count > 0) && line_renderer.enabled)
                {
                    cam_pos = FlightCamera.fetch.mainCamera.transform.position;
#if !KSP13
                    line_renderer.startWidth = Mathf.Clamp(Vector3.Distance(cam_pos, line_renderer.GetPosition(0)) / DIST_DIV, MIN_WIDTH, MAX_WIDTH);
                    line_renderer.endWidth = Mathf.Clamp(Vector3.Distance(cam_pos, line_renderer.GetPosition(Count - 1)) / DIST_DIV, MIN_WIDTH, MAX_WIDTH);
#else
                    line_renderer.SetWidth(Mathf.Clamp(Vector3.Distance(cam_pos, start) / DIST_DIV, MIN_WIDTH, MAX_WIDTH),
                                           Mathf.Clamp(Vector3.Distance(cam_pos, end) / DIST_DIV, MIN_WIDTH, MAX_WIDTH));
#endif
                }
            }

            internal void OnEnable()
            {
                if (!Ready)
                    return;
                line_renderer.enabled = true;
            }

            internal void OnDisable()
            {
                if (!Ready)
                    return;
                line_renderer.enabled = false;
            }

            internal void OnDestroy()
            {
                if (line_renderer != null)
                    UnityEngine.Object.Destroy(line_renderer);
                if (material != null)
                    UnityEngine.Object.Destroy(material);

                line_renderer = null;
                material = null;
            }

            internal void Clear()
            {
                if (!Ready)
                    return;
#if !KSP13
                line_renderer.positionCount = 0;
#else
                vertex_count = 0;
                line_renderer.SetVertexCount(0);
                start = Vector3.zero;
                end = Vector3.zero;
#endif
            }

            internal void Add(Vector3 point)
            {
                if (!Ready)
                    return;
#if !KSP13
                line_renderer.positionCount++;
                line_renderer.SetPosition(line_renderer.positionCount - 1, point);
#else
                vertex_count++;
                line_renderer.SetVertexCount(vertex_count);
                line_renderer.SetPosition(vertex_count - 1, point);
                if (vertex_count == 0)
                    start = point;
                end = point;
#endif
            }
        }

        private sealed class TargetingCross : MonoBehaviour
        {
            internal const float MIN_SIZE = 2f;
            internal const float MAX_SIZE = 2e3f;
            internal const float DIST_DIV = 50f;

            private double latitude = 0d;
            private double longitude = 0d;
            private double altitude = 0d;
            private Vector3 screen_point;
            private float size = 0f;

            internal Vector3? Position { get; set; }
            internal CelestialBody Body { get; set; }
            internal Color Color { get; set; } = Color.red;

            internal void OnPostRender()
            {
                if (Position == null || Body == null)
                    return;

                // get impact position, translate to latitude and longitude
                Body.GetLatLonAlt(Position.Value, out latitude, out longitude, out altitude);
                // only draw if visible on the camera
                screen_point = FlightCamera.fetch.mainCamera.WorldToViewportPoint(Position.Value);
                if (!(screen_point.z >= 0 && screen_point.x >= 0 && screen_point.x <= 1 && screen_point.y >= 0 && screen_point.y <= 1))
                    return;
                // resize marker in respect to distance from camera.
                size = Mathf.Clamp(Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, Position.Value) / DIST_DIV, MIN_SIZE, MAX_SIZE);
                // draw ground marker at this position
                GLUtils.DrawGroundMarker(Body, latitude, longitude, Color, false, 0, size);
            }
        }

        private const int DEFAULT_VERTEX_COUNT = 32;

        private static TrajectoryLine line;
        private static TargetingCross impact_cross;
        private static TargetingCross target_cross;

        // update method variables, put here to stop over use of the garbage collector.
        private static double time = 0d;
        private static double time_increment = 0d;
        private static Orbit orbit = null;
        private static Trajectory.Patch lastPatch = null;
        private static Vector3d bodyPosition = Vector3d.zero;
        private static Vector3d vertex = Vector3.zero;

        internal static void Start()
        {
            Util.DebugLog("Constructing");
            line = FlightCamera.fetch.mainCamera.gameObject.AddComponent<TrajectoryLine>();
            impact_cross = FlightCamera.fetch.mainCamera.gameObject.AddComponent<TargetingCross>();
            target_cross = FlightCamera.fetch.mainCamera.gameObject.AddComponent<TargetingCross>();
            target_cross.Color = Color.green;
        }

        internal static void Destroy()
        {
            Util.DebugLog("");
            if (line != null)
                UnityEngine.Object.Destroy(line);

            if (impact_cross != null)
                UnityEngine.Object.Destroy(impact_cross);

            if (target_cross != null)
                UnityEngine.Object.Destroy(target_cross);

            line = null;
            impact_cross = null;
            target_cross = null;
        }

        internal static void Update()
        {
            line.enabled = false;
            impact_cross.enabled = false;
            target_cross.enabled = false;

            if (!Settings.DisplayTrajectories
                || Util.IsMap
                || !Settings.DisplayTrajectoriesInFlight
                || Trajectory.Patches.Count == 0)
                return;

            line.Clear();
            line.Add(Trajectories.AttachedVessel.GetWorldPos3D());

            lastPatch = Trajectory.Patches[Trajectory.Patches.Count - 1];
            bodyPosition = lastPatch.StartingState.ReferenceBody.position;
            if (lastPatch.IsAtmospheric)
            {
                for (uint i = 0; i < lastPatch.AtmosphericTrajectory.Length; ++i)
                {
                    vertex = lastPatch.AtmosphericTrajectory[i].pos + bodyPosition;
                    line.Add(vertex);
                }
            }
            else
            {
                time = lastPatch.StartingState.Time;
                time_increment = (lastPatch.EndTime - lastPatch.StartingState.Time) / DEFAULT_VERTEX_COUNT;
                orbit = lastPatch.SpaceOrbit;
                for (uint i = 0; i < DEFAULT_VERTEX_COUNT; ++i)
                {
                    vertex = Util.SwapYZ(orbit.getRelativePositionAtUT(time));
                    if (Settings.BodyFixedMode)
                        vertex = Trajectory.CalculateRotatedPosition(orbit.referenceBody, vertex, time);

                    vertex += bodyPosition;

                    line.Add(vertex);

                    time += time_increment;
                }
            }

            line.enabled = true;

            // red impact cross
            if (lastPatch.ImpactPosition != null)
            {
                impact_cross.Position = lastPatch.ImpactPosition.Value + bodyPosition;
                impact_cross.Body = lastPatch.StartingState.ReferenceBody;
                impact_cross.enabled = true;
            }
            else
            {
                impact_cross.Position = null;
                impact_cross.Body = null;
            }

            // green target cross
            if (TargetProfile.WorldPosition != null)
            {
                target_cross.Position = TargetProfile.WorldPosition.Value + TargetProfile.Body.position;
                target_cross.Body = TargetProfile.Body;
                target_cross.enabled = true;
            }
            else
            {
                target_cross.Position = null;
                target_cross.Body = null;
            }
        }
    }
}
