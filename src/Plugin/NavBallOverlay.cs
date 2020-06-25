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
        private const float scale = 10f;

        private static NavBall navball;
        private static GameObject trajectoryGuide;
        private static GameObject trajectoryReference;
        private static Renderer guideRenderer;
        private static Renderer referenceRenderer;

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
                trajectoryGuide.transform.localScale = Vector3.one * scale;
            }

            if (trajectoryReference == null)
            {
                trajectoryReference = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trajectoryReference.layer = navball.progradeVector.gameObject.layer;
                trajectoryReference.transform.parent = navball.progradeVector.gameObject.transform.parent;
                trajectoryReference.transform.localScale = Vector3.one * scale;
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
            Trajectory.Patch patch = Trajectory.Patches.LastOrDefault();

            if ((!Util.IsFlight && !Util.IsTrackingStation) || !FlightGlobals.ActiveVessel || !Trajectory.Target.WorldPosition.HasValue ||
                patch == null || !patch.ImpactPosition.HasValue || patch.StartingState.ReferenceBody != Trajectory.Target.Body)
            {
                SetDisplayEnabled(false);
                return;
            }

            SetDisplayEnabled(true);

            trajectoryGuide.transform.localPosition = navball.attitudeGymbal * GetCorrectedDirection() * navball.VectorUnitScale;
            trajectoryGuide.SetActive(trajectoryGuide.transform.localPosition.z > 0f); // hide if behind navball

            trajectoryReference.transform.localPosition = navball.attitudeGymbal * GetPlannedDirection() * navball.VectorUnitScale;
            trajectoryReference.SetActive(trajectoryReference.transform.localPosition.z > 0f); // hide if behind navball
        }

        internal static void DestroyRenderer()
        {
            navball = null;
            guideRenderer = null;
            referenceRenderer = null;
        }

        internal static Vector3 GetPlannedDirection()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;

            if (vessel == null || Trajectory.Target.Body == null)
                return new Vector3(0, 0, 0);

            CelestialBody body = vessel.mainBody;

            Vector3d pos = vessel.GetWorldPos3D() - body.position;
            Vector3d vel = vessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity

            Vector3 up = pos.normalized;
            Vector3 velRight = Vector3.Cross(vel, up).normalized;
            Vector3 velUp = Vector3.Cross(velRight, vel).normalized;

            float plannedAngleOfAttack = (float)DescentProfile.GetAngleOfAttack(Trajectory.Target.Body, pos, vel);

            return vel.normalized * Mathf.Cos(plannedAngleOfAttack) + velUp * Mathf.Sin(plannedAngleOfAttack);
        }

        internal static Vector3 GetCorrectedDirection()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;

            if (vessel == null)
                return new Vector3(0, 0, 0);

            CelestialBody body = vessel.mainBody;

            Vector3d pos = vessel.GetWorldPos3D() - body.position;
            Vector3d vel = vessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity

            Vector3 referenceVector = GetPlannedDirection();

            Vector3 up = pos.normalized;
            Vector3 velRight = Vector3.Cross(vel, up).normalized;

            Vector3 refUp = Vector3.Cross(velRight, referenceVector).normalized;
            Vector3 refRight = velRight;

            Vector2 offsetDir = GetCorrection();

            return (referenceVector + refUp * offsetDir.y + refRight * offsetDir.x).normalized;
        }

        private static void SetDisplayEnabled(bool enabled)
        {
            if (trajectoryGuide == null || trajectoryReference == null)
                return;

            guideRenderer.enabled = enabled;
            referenceRenderer.enabled = enabled;
        }

        private static Vector2 GetCorrection()
        {
            Vessel vessel = FlightGlobals.ActiveVessel;

            if (vessel == null)
                return new Vector2(0, 0);

            Vector3? targetPosition = Trajectory.Target.WorldPosition;
            Trajectory.Patch patch = Trajectory.Patches.LastOrDefault();
            CelestialBody body = Trajectory.Target.Body;
            if (!targetPosition.HasValue || patch == null || !patch.ImpactPosition.HasValue || patch.StartingState.ReferenceBody != body || !patch.IsAtmospheric)
                return new Vector2(0, 0);

            // Get impact position, or, if some point over the trajectory has not enough clearance, smoothly interpolate to that point depending on how much clearance is missing
            Vector3 impactPosition = patch.ImpactPosition.Value;
            foreach (Trajectory.Point p in patch.AtmosphericTrajectory)
            {
                float neededClearance = 600.0f;
                float missingClearance = neededClearance - (p.pos.magnitude - (float)body.Radius - p.groundAltitude);
                if (missingClearance > 0.0f)
                {
                    if (Vector3.Distance(p.pos, patch.RawImpactPosition.Value) > 3000.0f)
                    {
                        float coeff = missingClearance / neededClearance;
                        Vector3 rotatedPos = p.pos;
                        if (!Settings.BodyFixedMode)
                        {
                            rotatedPos = Trajectory.CalculateRotatedPosition(body, p.pos, p.time);
                        }
                        impactPosition = impactPosition * (1.0f - coeff) + rotatedPos * coeff;
                    }
                    break;
                }
            }

            Vector3 right = Vector3.Cross(patch.ImpactVelocity.Value, impactPosition).normalized;
            Vector3 behind = Vector3.Cross(right, impactPosition).normalized;

            Vector3 offset = targetPosition.Value - impactPosition;
            Vector2 offsetDir = new Vector2(Vector3.Dot(right, offset), Vector3.Dot(behind, offset));
            offsetDir *= 0.00005f; // 20km <-> 1 <-> 45° (this is purely indicative, no physical meaning, it would be very complicated to compute an actual correction angle as it depends on the spacecraft behavior in the atmosphere ; a small angle will suffice for a plane, but even a big angle might do almost nothing for a rocket)

            Vector3d pos = vessel.GetWorldPos3D() - body.position;
            Vector3d vel = vessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity
            float plannedAngleOfAttack = (float)DescentProfile.GetAngleOfAttack(body, pos, vel);
            if (plannedAngleOfAttack < Math.PI * 0.5f)
                offsetDir.y = -offsetDir.y; // behavior is different for prograde or retrograde entry

            float maxCorrection = 1.0f;
            offsetDir.x = Mathf.Clamp(offsetDir.x, -maxCorrection, maxCorrection);
            offsetDir.y = Mathf.Clamp(offsetDir.y, -maxCorrection, maxCorrection);

            return offsetDir;
        }
    }
}
