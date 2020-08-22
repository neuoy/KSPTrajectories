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
using Object = UnityEngine.Object;

namespace Trajectories
{
    // Display indications on the navball. Code inspired from Enhanced NavBall mod.
    internal class NavBallOverlay
    {
        private const float SCALE = 0.5f;

        private Texture2D guide_texture;
        private Texture2D reference_texture;

        private bool constructed;

        private NavBall navball;
        private Transform guide_transform;
        private Transform reference_transform;
        private Renderer guide_renderer;
        private Renderer reference_renderer;

        // updated variables, put here to stop over use of the garbage collector.
        private Trajectory.Patch patch;
        private CelestialBody body;
        private Vector3d position;
        private Vector3d velocity;
        private Vector3d up;
        private Vector3d vel_right;
        private Vector3d reference;
        private readonly int MainTexture = Shader.PropertyToID("_MainTexture");

        private bool TexturesAllocated => guide_texture != null && reference_texture != null;
        private bool TransformsAllocated => guide_transform != null && reference_transform != null;
        private bool RenderersAllocated => guide_renderer != null && reference_renderer != null;

        internal bool Ready => (TexturesAllocated && TransformsAllocated && RenderersAllocated && navball != null);

        internal Vector3d? PlannedDirection => reference;

        internal Vector3d? CorrectedDirection
        {
            get
            {
                if (!_trajectory.IsVesselAttached)
                    return Vector3d.zero;

                Vector2d offsetDir = GetCorrection();

                return (reference + Vector3d.Cross(vel_right, reference).normalized * offsetDir.y + vel_right * offsetDir.x).normalized;
            }
        }

        private readonly Trajectory _trajectory;

        internal NavBallOverlay(Trajectory trajectory)
        {
            Util.DebugLog(constructed ? "Resetting" : "Constructing");

            _trajectory = trajectory;

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

                navball = Object.FindObjectOfType<NavBall>();

                if (navball != null)
                {
                    // green circle for target
                    guide_transform = Object.Instantiate(navball.progradeVector, navball.progradeVector.parent);
                    if (guide_transform != null)
                    {
                        guide_transform.gameObject.transform.localScale = guide_transform.gameObject.transform.localScale * SCALE;
                        guide_renderer = guide_transform.GetComponent<Renderer>();
                        if (guide_renderer != null)
                        {
                            //Util.DebugLog("Scale {0}", guide_renderer.material.GetTextureScale("_MainTexture").ToString());
                            guide_renderer.material.SetTexture(MainTexture, guide_texture);
                            guide_renderer.material.SetTextureOffset(MainTexture, Vector2.zero);
                            guide_renderer.material.SetTextureScale(MainTexture, Vector2.one);
                        }
                    }

                    // red square for crash site
                    reference_transform = Object.Instantiate(navball.progradeVector, navball.progradeVector.parent);
                    if (reference_transform != null)
                    {
                        reference_transform.gameObject.transform.localScale = reference_transform.gameObject.transform.localScale * SCALE;
                        reference_renderer = reference_transform.GetComponent<Renderer>();
                        if (reference_renderer != null)
                        {
                            reference_renderer.material.SetTexture(MainTexture, reference_texture);
                            reference_renderer.material.SetTextureOffset(MainTexture, Vector2.zero);
                            reference_renderer.material.SetTextureScale(MainTexture, Vector2.one);
                        }
                    }
                }
            }
        }

        internal void Destroy()
        {
            Util.DebugLog("");
            DestroyTransforms();
            if (guide_texture != null)
                Object.Destroy(guide_texture);

            if (reference_texture != null)
                Object.Destroy(reference_texture);

            guide_texture = null;
            reference_texture = null;
        }

        internal void Update()
        {
            patch = _trajectory.Patches.LastOrDefault();

            if ((!Util.IsFlight && !Util.IsTrackingStation) || !_trajectory.IsVesselAttached || !_trajectory.TargetProfile.WorldPosition.HasValue ||
                patch == null || !patch.ImpactPosition.HasValue || patch.StartingState.ReferenceBody != _trajectory.TargetProfile.Body || !Ready)
            {
                SetDisplayEnabled(false);
                return;
            }

            body = _trajectory.AttachedVessel.mainBody;

            position = _trajectory.AttachedVessel.GetWorldPos3D() - body.position;
            velocity = _trajectory.AttachedVessel.obt_velocity - body.getRFrmVel(body.position + position); // air velocity
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

        internal void DestroyTransforms()
        {
            navball = null;

            if (guide_renderer != null)
                Object.Destroy(guide_renderer);
            if (reference_renderer != null)
                Object.Destroy(reference_renderer);
            guide_renderer = null;
            reference_renderer = null;

            if (guide_transform != null)
                guide_transform.gameObject.DestroyGameObject();
            if (reference_transform != null)
                reference_transform.gameObject.DestroyGameObject();
            guide_transform = null;
            reference_transform = null;
        }

        private void SetDisplayEnabled(bool enabled)
        {
            if (!RenderersAllocated)
                return;

            guide_renderer.enabled = enabled;
            reference_renderer.enabled = enabled;
        }

        private Vector3d CalcReference()
        {
            if (!_trajectory.IsVesselAttached || _trajectory.TargetProfile.Body == null)
                return Vector3d.zero;

            double plannedAngleOfAttack = (double) _trajectory.DescentProfile.GetAngleOfAttack(_trajectory.TargetProfile.Body, position, velocity);

            return velocity.normalized * Math.Cos(plannedAngleOfAttack) + Vector3d.Cross(vel_right, velocity).normalized * Math.Sin(plannedAngleOfAttack);
        }

        private Vector2d GetCorrection()
        {
            if (!_trajectory.IsVesselAttached)
                return Vector2d.zero;

            Vector3d? targetPosition = _trajectory.TargetProfile.WorldPosition;
            CelestialBody body = _trajectory.TargetProfile.Body;
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

            Vector3d pos = _trajectory.AttachedVessel.GetWorldPos3D() - body.position;
            Vector3d vel = _trajectory.AttachedVessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity

            double plannedAngleOfAttack = (double) _trajectory.DescentProfile.GetAngleOfAttack(body, pos, vel);
            if (plannedAngleOfAttack < Util.HALF_PI)
                offsetDir.y = -offsetDir.y; // behavior is different for prograde or retrograde entry

            double maxCorrection = 1.0d;
            offsetDir.x = Util.Clamp(offsetDir.x, -maxCorrection, maxCorrection);
            offsetDir.y = Util.Clamp(offsetDir.y, -maxCorrection, maxCorrection);

            return offsetDir;
        }
    }
}
