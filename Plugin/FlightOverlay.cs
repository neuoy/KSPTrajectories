/*
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

using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class FlightOverlay: MonoBehaviour
    {
        private const int defaultVertexCount = 32;
        private const float lineWidth = 2.0f;

        private static TrajectoryLine line;
        private static TargetingCross targetingCross;

        // update method variables, put here to stop over use of the garbage collector.
        private static double time = 0d;
        private static double time_increment = 0d;
        private static Orbit orbit = null;
        private static Trajectory.Patch lastPatch = null;
        private static Vector3d bodyPosition = Vector3d.zero;
        private static Vector3 vertex = Vector3.zero;

        private void Awake()
        {
            line = FlightCamera.fetch.mainCamera.gameObject.AddComponent<TrajectoryLine>();
            targetingCross = FlightCamera.fetch.mainCamera.gameObject.AddComponent<TargetingCross>();
        }

        private void OnDestroy()
        {
            if (line != null)
                Destroy(line);

            if (targetingCross != null)
                Destroy(targetingCross);

            line = null;
            targetingCross = null;
        }

        private void Update()
        {
            line.enabled = false;
            targetingCross.enabled = false;

            if (!Settings.fetch.DisplayTrajectories
                || Util.IsMap
                || !Settings.fetch.DisplayTrajectoriesInFlight
                || Trajectory.fetch.Patches.Count == 0)
                return;

            line.Vertices.Clear();

            lastPatch = Trajectory.fetch.Patches[Trajectory.fetch.Patches.Count - 1];
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
                time_increment = (lastPatch.EndTime - lastPatch.StartingState.Time) / defaultVertexCount;
                orbit = lastPatch.SpaceOrbit;
                for (uint i = 0; i < defaultVertexCount; ++i)
                {
                    vertex = Util.SwapYZ(orbit.getRelativePositionAtUT(time));
                    if (Settings.fetch.BodyFixedMode)
                        vertex = Trajectory.CalculateRotatedPosition(orbit.referenceBody, vertex, time);

                    vertex += bodyPosition;

                    line.Vertices.Add(vertex);

                    time += time_increment;
                }
            }

            line.Body = lastPatch.StartingState.ReferenceBody;
            line.enabled = true;

            if (lastPatch.ImpactPosition != null)
            {
                targetingCross.ImpactPosition = lastPatch.ImpactPosition.Value;
                targetingCross.ImpactBody = lastPatch.StartingState.ReferenceBody;
                targetingCross.enabled = true;
            }
            else
            {
                targetingCross.ImpactPosition = null;
                targetingCross.ImpactBody = null;
            }
        }
    }

    public class TrajectoryLine: MonoBehaviour
    {
        public List<Vector3d> Vertices = new List<Vector3d>();
        public CelestialBody Body = null;

        public void OnPostRender()
        {
            if (Body == null || Vertices == null || Vertices.Count == 0)
                return;

            GLUtils.DrawPath(Body, Vertices, Color.blue, false, false);
        }
    }

    public class TargetingCross: MonoBehaviour
    {
        public const double markerSize = 2.0d; // in meters

        private static double impactLat = 0d;
        private static double impactLon = 0d;
        private static double impactAlt = 0d;
        private static Vector3 screen_point;
        private static Vector3 cam_pos;
        private static double cross_dist = 0d;

        public Vector3? ImpactPosition { get; internal set; }
        public CelestialBody ImpactBody { get; internal set; }


        public void OnPostRender()
        {
            if (ImpactPosition == null || ImpactBody == null)
                return;

            // get impact position, translate to latitude and longitude
            ImpactBody.GetLatLonAlt(ImpactPosition.Value + ImpactBody.position, out impactLat, out impactLon, out impactAlt);

            // only draw if visable on the camera
            screen_point = PlanetariumCamera.Camera.WorldToViewportPoint(ImpactPosition.Value + ImpactBody.position);
            if (!(screen_point.z > 0 && screen_point.x > 0 && screen_point.x < 1 && screen_point.y > 0 && screen_point.y < 1))
                return;

            // resize marker in respect to distance from camera.
            cam_pos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position) - ImpactBody.position;
            cross_dist = System.Math.Max(Vector3.Distance(cam_pos, ImpactPosition.Value) / 80.0d, 1.0d);

            // draw ground marker at this position
            GLUtils.DrawGroundMarker(ImpactBody, impactLat, impactLon, Color.red, false, 0, System.Math.Min(markerSize * cross_dist, 1500.0d));
        }
    }
}
