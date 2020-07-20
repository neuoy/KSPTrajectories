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
using System.IO;
using System.Linq;
using KSP.UI.Screens.Flight;
using UnityEngine;

namespace Trajectories
{
    // Display indications on the navball. Code inspired from Enhanced NavBall mod.
    internal static class NavBallOverlay
    {
        private const float SCALE = 0.5f;

        private static Texture2D guide_texture = null;
        private static Texture2D reference_texture = null;

        private static bool constructed = false;

        private static NavBall navball;
        private static Transform guide_transform;
        private static Transform reference_transform;
        private static Renderer guide_renderer;
        private static Renderer reference_renderer;

        // updated variables, put here to stop over use of the garbage collector.
        private static Trajectory.Patch patch;
        private static CelestialBody body;
        private static Vector3d position;
        private static Vector3d velocity;
        private static Vector3d up;
        private static Vector3d vel_right;
        private static Vector3d reference;

        private static bool TexturesAllocated => (guide_texture != null && reference_texture != null);
        private static bool TransformsAllocated => (guide_transform != null && reference_transform != null);
        private static bool RenderersAllocated => (guide_renderer != null && reference_renderer != null);

        internal static bool Ready => (TexturesAllocated && TransformsAllocated && RenderersAllocated && navball != null);

        internal static Vector3d? PlannedDirection => reference;

        internal static Vector3d? CorrectedDirection
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
            Util.DebugLog(constructed ? "Resetting" : "Constructing");

            guide_texture ??= new Texture2D(36, 36);
            reference_texture ??= new Texture2D(36, 36);

            if (TexturesAllocated)
            {
                if (!constructed)
                {
                    string TrajTexturePath = KSPUtil.ApplicationRootPath + "GameData/Trajectories/Textures/";
                    guide_texture.LoadImage(File.ReadAllBytes(TrajTexturePath + "GuideNavMarker.png"));
                    reference_texture.LoadImage(File.ReadAllBytes(TrajTexturePath + "RefNavMarker.png"));
                    constructed = true;
                }

                navball = UnityEngine.Object.FindObjectOfType<NavBall>();

                if (navball != null)
                {
                    // green circle for target
                    guide_transform = (Transform)GameObject.Instantiate(navball.progradeVector, navball.progradeVector.parent);
                    if (guide_transform != null)
                    {
                        guide_transform.gameObject.transform.localScale = guide_transform.gameObject.transform.localScale * SCALE;
                        guide_renderer = guide_transform.GetComponent<Renderer>();
                        if (guide_renderer != null)
                        {
                            //Util.DebugLog("Scale {0}", guide_renderer.material.GetTextureScale("_MainTexture").ToString());
                            guide_renderer.material.SetTexture("_MainTexture", guide_texture);
                            guide_renderer.material.SetTextureOffset("_MainTexture", Vector2.zero);
                            guide_renderer.material.SetTextureScale("_MainTexture", Vector2.one);
                        }
                    }

                    // red square for crash site
                    reference_transform = (Transform)GameObject.Instantiate(navball.progradeVector, navball.progradeVector.parent);
                    if (reference_transform != null)
                    {
                        reference_transform.gameObject.transform.localScale = reference_transform.gameObject.transform.localScale * SCALE;
                        reference_renderer = reference_transform.GetComponent<Renderer>();
                        if (reference_renderer != null)
                        {
                            reference_renderer.material.SetTexture("_MainTexture", reference_texture);
                            reference_renderer.material.SetTextureOffset("_MainTexture", Vector2.zero);
                            reference_renderer.material.SetTextureScale("_MainTexture", Vector2.one);
                        }
                    }
                }
            }
        }

        internal static void Destroy()
        {
            Util.DebugLog("");
            DestroyTransforms();
            if (guide_texture != null)
                UnityEngine.Object.Destroy(guide_texture);

            if (reference_texture != null)
                UnityEngine.Object.Destroy(reference_texture);

            guide_texture = null;
            reference_texture = null;
        }

        internal static void Update()
        {
            patch = Trajectory.Patches.LastOrDefault();

            if ((!Util.IsFlight && !Util.IsTrackingStation) || !Trajectories.IsVesselAttached || !TargetProfile.WorldPosition.HasValue ||
                patch == null || !patch.ImpactPosition.HasValue || patch.StartingState.ReferenceBody != TargetProfile.Body || !Ready)
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

            guide_transform.gameObject.transform.localPosition = navball.attitudeGymbal * (CorrectedDirection.Value * navball.VectorUnitScale);
            reference_transform.gameObject.transform.localPosition = navball.attitudeGymbal * (reference * navball.VectorUnitScale);

            // hide if behind navball
            guide_transform.gameObject.SetActive(guide_transform.gameObject.transform.localPosition.z >= navball.VectorUnitCutoff);
            reference_transform.gameObject.SetActive(reference_transform.gameObject.transform.localPosition.z >= navball.VectorUnitCutoff);
        }

        internal static void DestroyTransforms()
        {
            navball = null;

            if (guide_renderer != null)
                UnityEngine.Object.Destroy(guide_renderer);
            if (reference_renderer != null)
                UnityEngine.Object.Destroy(reference_renderer);
            guide_renderer = null;
            reference_renderer = null;

            if (guide_transform != null)
                guide_transform.gameObject.DestroyGameObject();
            if (reference_transform != null)
                reference_transform.gameObject.DestroyGameObject();
            guide_transform = null;
            reference_transform = null;

        }

        private static void SetDisplayEnabled(bool enabled)
        {
            if (!RenderersAllocated)
                return;

            guide_renderer.enabled = enabled;
            reference_renderer.enabled = enabled;
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
