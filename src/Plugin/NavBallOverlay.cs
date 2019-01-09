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

using KSP.UI.Screens.Flight;
using System;
using System.Linq;
using UnityEngine;

namespace Trajectories
{
    // Display indications on the navball. Code inspired from Enhanced NavBall mod.
    [KSPAddon(KSPAddon.Startup.Flight, false)]
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
            if (trajectoryGuide == null || trajectoryReference == null)
                return;

            var guideRenderer = trajectoryGuide.GetComponent<Renderer>();
            var referenceRenderer = trajectoryReference.GetComponent<Renderer>();

            guideRenderer.enabled = enabled;
            referenceRenderer.enabled = enabled;
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



            GameObject navballGameObject = FlightUIModeController.Instance.navBall.gameObject;
            Transform vectorsPivotTransform = navballGameObject.transform.Find("vectorsPivot");
            navball = navballGameObject.GetComponentInChildren<NavBall>();

            trajectoryGuide = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            trajectoryGuide.layer = navball.progradeVector.gameObject.layer;
            trajectoryGuide.transform.parent = navball.progradeVector.gameObject.transform.parent;
            trajectoryGuide.transform.localScale = Vector3.one * 10.0f;

            trajectoryReference = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trajectoryReference.layer = navball.progradeVector.gameObject.layer;
            trajectoryReference.transform.parent = navball.progradeVector.gameObject.transform.parent;
            trajectoryReference.transform.localScale = Vector3.one * 10.0f;
        }

        private void UpdateNavBall()
        {
            Vector3? targetPosition = Trajectory.Target.WorldPosition;
            var patch = Trajectory.fetch.Patches.LastOrDefault();
            CelestialBody body = Trajectory.Target.Body;
            if (!targetPosition.HasValue || patch == null || !patch.ImpactPosition.HasValue || patch.StartingState.ReferenceBody != body)
            {
                SetDisplayEnabled(false);
                return;
            }

            if (trajectoryGuide == null)
                Init();

            SetDisplayEnabled(true);

            if(navBallRadius == 0.0f)
                navBallRadius = navball.progradeVector.localPosition.magnitude;

            Vector3 referenceVector = GetPlannedDirection();
            trajectoryReference.transform.localPosition = navball.attitudeGymbal * referenceVector * navball.VectorUnitScale;
            trajectoryReference.SetActive(trajectoryReference.transform.localPosition.z > 0); // hide if behind navball

            Vector3 guideDir = GetCorrectedDirection();
            trajectoryGuide.transform.localPosition = navball.attitudeGymbal * guideDir * navball.VectorUnitScale;
            trajectoryGuide.SetActive(trajectoryGuide.transform.localPosition.z > 0); // hide if behind navball
        }

        public static Vector3 GetPlannedDirection()
        {
            var vessel = FlightGlobals.ActiveVessel;

            if (vessel == null || Trajectory.Target.Body == null)
                return new Vector3(0, 0, 0);

            CelestialBody body = vessel.mainBody;

            Vector3d pos = vessel.GetWorldPos3D() - body.position;
            Vector3d vel = vessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity

            Vector3 up = pos.normalized;
            Vector3 velRight = Vector3.Cross(vel, up).normalized;
            Vector3 velUp = Vector3.Cross(velRight, vel).normalized;

            float plannedAngleOfAttack = (float)DescentProfile.fetch.GetAngleOfAttack(Trajectory.Target.Body, pos, vel);

            return vel.normalized * Mathf.Cos(plannedAngleOfAttack) + velUp * Mathf.Sin(plannedAngleOfAttack);
        }

        private static Vector2 GetCorrection()
        {
            var vessel = FlightGlobals.ActiveVessel;

            if (vessel == null)
                return new Vector2(0, 0);

            Vector3? targetPosition = Trajectory.Target.WorldPosition;
            var patch = Trajectory.fetch.Patches.LastOrDefault();
            CelestialBody body = Trajectory.Target.Body;
            if (!targetPosition.HasValue || patch == null || !patch.ImpactPosition.HasValue || patch.StartingState.ReferenceBody != body || !patch.IsAtmospheric)
                return new Vector2(0, 0);

            // Get impact position, or, if some point over the trajectory has not enough clearance, smoothly interpolate to that point depending on how much clearance is missing
            Vector3 impactPosition = patch.ImpactPosition.Value;
            foreach (var p in patch.AtmosphericTrajectory)
            {
                float neededClearance = 600.0f;
                float missingClearance = neededClearance - (p.pos.magnitude - (float)body.Radius - p.groundAltitude);
                if (missingClearance > 0.0f)
                {
                    if (Vector3.Distance(p.pos, patch.RawImpactPosition.Value) > 3000.0f)
                    {
                        float coeff = missingClearance / neededClearance;
                        Vector3 rotatedPos = p.pos;
                        if (!Settings.fetch.BodyFixedMode)
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
            float plannedAngleOfAttack = (float)DescentProfile.fetch.GetAngleOfAttack(body, pos, vel);
            if (plannedAngleOfAttack < Math.PI * 0.5f)
                offsetDir.y = -offsetDir.y; // behavior is different for prograde or retrograde entry

            float maxCorrection = 1.0f;
            offsetDir.x = Mathf.Clamp(offsetDir.x, -maxCorrection, maxCorrection);
            offsetDir.y = Mathf.Clamp(offsetDir.y, -maxCorrection, maxCorrection);

            return offsetDir;
        }

        public static Vector3 GetCorrectedDirection()
        {
            var vessel = FlightGlobals.ActiveVessel;

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
    }
}
