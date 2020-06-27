/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
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
using System.Linq;
using KSP.UI.Screens.Flight;
using UnityEngine;

namespace Trajectories
{
    // Display indications on the navball. Code inspired from Enhanced NavBall mod.
    internal static class NavBallOverlay
    {
        private const float SCALE = 10f;

        private static NavBall navball;
        private static GameObject trajectoryGuide;
        private static GameObject trajectoryReference;
        private static Renderer guideRenderer;
        private static Renderer referenceRenderer;

        // updated variables, put here to stop over use of the garbage collector.
        private static Trajectory.Patch patch;
        private static CelestialBody body;
        private static Vector3d position;
        private static Vector3d velocity;
        private static Vector3d up;
        private static Vector3d vel_right;
        private static Vector3d reference;

        internal static Vector3d PlannedDirection => reference;

        internal static Vector3d CorrectedDirection
        {
            get
            {
                if (!Trajectories.IsVesselAttached)
                    return Vector3d.zero;

                Vector2d offsetDir = GetCorrection();

                return (reference + Vector3d.Cross(vel_right, reference).normalized * offsetDir.y + vel_right * offsetDir.x).normalized;
            }
        }

        internal static void Start()
        {
            Util.DebugLog(trajectoryGuide != null ? "Resetting" : "Constructing");

            //navball = FlightUIModeController.Instance.navBall.gameObject.GetComponentInChildren<NavBall>();
            navball = Trajectories.FindObjectOfType<NavBall>();

            if (trajectoryGuide == null)
            {
                trajectoryGuide = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                trajectoryGuide.layer = navball.progradeVector.gameObject.layer;
                trajectoryGuide.transform.parent = navball.progradeVector.gameObject.transform.parent;
                trajectoryGuide.transform.localScale = Vector3.one * SCALE;
            }

            if (trajectoryReference == null)
            {
                trajectoryReference = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trajectoryReference.layer = navball.progradeVector.gameObject.layer;
                trajectoryReference.transform.parent = navball.progradeVector.gameObject.transform.parent;
                trajectoryReference.transform.localScale = Vector3.one * SCALE;
            }

            guideRenderer = trajectoryGuide.GetComponent<Renderer>();
            referenceRenderer = trajectoryReference.GetComponent<Renderer>();
        }

        internal static void Destroy()
        {
            Util.DebugLog("");
            DestroyRenderer();
            if (trajectoryGuide != null)
                Trajectories.Destroy(trajectoryGuide);

            if (trajectoryReference != null)
                Trajectories.Destroy(trajectoryReference);

            trajectoryGuide = null;
            trajectoryReference = null;
        }

        internal static void Update()
        {
            patch = Trajectory.Patches.LastOrDefault();

            if ((!Util.IsFlight && !Util.IsTrackingStation) || !Trajectories.IsVesselAttached || !TargetProfile.WorldPosition.HasValue ||
                patch == null || !patch.ImpactPosition.HasValue || patch.StartingState.ReferenceBody != TargetProfile.Body)
            {
                SetDisplayEnabled(false);
                return;
            }

            body = Trajectories.AttachedVessel.mainBody;

            position = Trajectories.AttachedVessel.GetWorldPos3D() - body.position;
            velocity = Trajectories.AttachedVessel.obt_velocity - body.getRFrmVel(body.position + position); // air velocity
            up = position.normalized;
            vel_right = Vector3d.Cross(velocity, up).normalized;
            reference = CalcReference();

            SetDisplayEnabled(true);

            trajectoryGuide.transform.localPosition = navball.attitudeGymbal * CorrectedDirection * navball.VectorUnitScale;
            trajectoryGuide.SetActive(trajectoryGuide.transform.localPosition.z > 0f); // hide if behind navball

            trajectoryReference.transform.localPosition = navball.attitudeGymbal * reference * navball.VectorUnitScale;
            trajectoryReference.SetActive(trajectoryReference.transform.localPosition.z > 0f); // hide if behind navball
        }

        internal static void DestroyRenderer()
        {
            navball = null;
            guideRenderer = null;
            referenceRenderer = null;
        }

        private static void SetDisplayEnabled(bool enabled)
        {
            if (trajectoryGuide == null || trajectoryReference == null)
                return;

            guideRenderer.enabled = enabled;
            referenceRenderer.enabled = enabled;
        }

        private static Vector3d CalcReference()
        {
            if (!Trajectories.IsVesselAttached || TargetProfile.Body == null)
                return Vector3d.zero;

            double plannedAngleOfAttack = (double)DescentProfile.GetAngleOfAttack(TargetProfile.Body, position, velocity);

            return velocity.normalized * Math.Cos(plannedAngleOfAttack) + Vector3d.Cross(vel_right, velocity).normalized * Math.Sin(plannedAngleOfAttack);
        }

        private static Vector2d GetCorrection()
        {
            if (!Trajectories.IsVesselAttached)
                return Vector2d.zero;

            Vector3d? targetPosition = TargetProfile.WorldPosition;
            CelestialBody body = TargetProfile.Body;
            if (!targetPosition.HasValue || patch == null || !patch.ImpactPosition.HasValue || patch.StartingState.ReferenceBody != body || !patch.IsAtmospheric)
                return Vector2d.zero;

            // Get impact position, or, if some point over the trajectory has not enough clearance, smoothly interpolate to that point depending on how much clearance is missing
            Vector3d impactPosition = patch.ImpactPosition.Value;
            foreach (Trajectory.Point p in patch.AtmosphericTrajectory)
            {
                double neededClearance = 600.0d;
                double missingClearance = neededClearance - (p.pos.magnitude - body.Radius - p.groundAltitude);
                if (missingClearance > 0.0d)
                {
                    if (Vector3d.Distance(p.pos, patch.RawImpactPosition.Value) > 3000.0d)
                    {
                        double coeff = missingClearance / neededClearance;
                        Vector3d rotatedPos = p.pos;
                        if (!Settings.BodyFixedMode)
                        {
                            rotatedPos = Trajectory.CalculateRotatedPosition(body, p.pos, p.time);
                        }
                        impactPosition = impactPosition * (1.0d - coeff) + rotatedPos * coeff;
                    }
                    break;
                }
            }

            Vector3d right = Vector3d.Cross(patch.ImpactVelocity.Value, impactPosition).normalized;
            Vector3d behind = Vector3d.Cross(right, impactPosition).normalized;

            Vector3d offset = targetPosition.Value - impactPosition;
            Vector2d offsetDir = new Vector2d(Vector3d.Dot(right, offset), Vector3d.Dot(behind, offset));
            offsetDir *= 0.00005d; // 20km <-> 1 <-> 45° (this is purely indicative, no physical meaning, it would be very complicated to compute an actual correction angle as it depends on the spacecraft behavior in the atmosphere ; a small angle will suffice for a plane, but even a big angle might do almost nothing for a rocket)

            Vector3d pos = Trajectories.AttachedVessel.GetWorldPos3D() - body.position;
            Vector3d vel = Trajectories.AttachedVessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity

            double plannedAngleOfAttack = (double)DescentProfile.GetAngleOfAttack(body, pos, vel);
            if (plannedAngleOfAttack < Util.HALF_PI)
                offsetDir.y = -offsetDir.y; // behavior is different for prograde or retrograde entry

            double maxCorrection = 1.0d;
            offsetDir.x = Util.Clamp(offsetDir.x, -maxCorrection, maxCorrection);
            offsetDir.y = Util.Clamp(offsetDir.y, -maxCorrection, maxCorrection);

            return offsetDir;
        }
    }
}
