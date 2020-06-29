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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    internal static class FlightOverlay
    {
        private sealed class TrajectoryLine : MonoBehaviour
        {
            internal List<Vector3d> Vertices = new List<Vector3d>();
            internal CelestialBody Body = null;

            internal void OnPostRender()
            {
                if (Body == null || Vertices == null || Vertices.Count == 0)
                    return;

                GLUtils.DrawPath(Body, Vertices, Color.blue, false, false);
            }
        }

        private sealed class TargetingCross : MonoBehaviour
        {
            internal const double MARKER_SIZE = 2.0d; // in meters

            private double latitude = 0d;
            private double longitude = 0d;
            private double altitude = 0d;
            private Vector3 screen_point;
            private Vector3 cam_pos;
            private double cross_dist = 0d;

            internal Vector3? Position { get; set; }
            internal CelestialBody Body { get; set; }
            internal Color Color { get; set; } = Color.red;

            internal void OnPostRender()
            {
                if (Position == null || Body == null)
                    return;

                // get impact position, translate to latitude and longitude
                Body.GetLatLonAlt(Position.Value + Body.position, out latitude, out longitude, out altitude);

                // only draw if visible on the camera
                screen_point = PlanetariumCamera.Camera.WorldToViewportPoint(Position.Value + Body.position);
                if (!(screen_point.z > 0 && screen_point.x > 0 && screen_point.x < 1 && screen_point.y > 0 && screen_point.y < 1))
                    return;

                // resize marker in respect to distance from camera.
                cam_pos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position) - Body.position;
                cross_dist = System.Math.Max(Vector3.Distance(cam_pos, Position.Value) / 80.0d, 1.0d);

                // draw ground marker at this position
                GLUtils.DrawGroundMarker(Body, latitude, longitude, Color, false, 0, Math.Min(MARKER_SIZE * cross_dist, 1500.0d));
            }
        }

        private const int DEFAULT_VERTEX_COUNT = 32;
        //private const float lineWidth = 2.0f;

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

            line.Vertices.Clear();
            line.Vertices.Add(Trajectories.AttachedVessel.GetWorldPos3D());

            lastPatch = Trajectory.Patches[Trajectory.Patches.Count - 1];
            bodyPosition = lastPatch.StartingState.ReferenceBody.position;
            if (lastPatch.IsAtmospheric)
            {
                for (uint i = 0; i < lastPatch.AtmosphericTrajectory.Length; ++i)
                {
                    vertex = lastPatch.AtmosphericTrajectory[i].pos + bodyPosition;
                    line.Vertices.Add(vertex);
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

                    line.Vertices.Add(vertex);

                    time += time_increment;
                }
            }

            line.Body = lastPatch.StartingState.ReferenceBody;
            line.enabled = true;

            // red impact cross
            if (lastPatch.ImpactPosition != null)
            {
                impact_cross.Position = lastPatch.ImpactPosition.Value;
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
                target_cross.Position = TargetProfile.WorldPosition.Value;
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
