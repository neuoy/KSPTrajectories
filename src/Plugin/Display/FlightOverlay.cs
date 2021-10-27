/*
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
  Copyright© (c) 2017-2021 S.Gray, (aka PiezPiedPy).

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
        private const int DEFAULT_VERTEX_COUNT = 32;

        private static GfxUtil.TrajectoryLine line;                // Todo: Modify to draw a splined curve
        private static GfxUtil.TargetingCross impact_cross;        // Todo: Modify to use a projected texture   Projector proj = GetComponent<Projector>();
        private static GfxUtil.TargetingCross target_cross;

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

            line = FlightCamera.fetch.mainCamera.gameObject.AddComponent<GfxUtil.TrajectoryLine>();
            line.Scene = GameScenes.FLIGHT;
            impact_cross = FlightCamera.fetch.mainCamera.gameObject.AddComponent<GfxUtil.TargetingCross>();
            target_cross = FlightCamera.fetch.mainCamera.gameObject.AddComponent<GfxUtil.TargetingCross>();
            impact_cross.Color = XKCDColors.FireEngineRed;
            target_cross.Color = XKCDColors.AcidGreen;
        }

        internal static void Destroy()
        {
            Util.DebugLog("");
            if (line)
                Object.Destroy(line);

            if (impact_cross)
                Object.Destroy(impact_cross);

            if (target_cross)
                Object.Destroy(target_cross);

            line = null;
            impact_cross = null;
            target_cross = null;
        }

        internal static void FixedUpdate()
        {
            if (Trajectories.AttachedVessel)
                line?.SetStart(Trajectories.AttachedVessel.transform.TransformPoint(Vector3.zero));
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
            line.Add(Trajectories.AttachedVessel.transform.TransformPoint(Vector3.zero));

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
