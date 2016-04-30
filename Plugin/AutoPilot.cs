using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class AutoPilot : MonoBehaviour
    {
        private static AutoPilot fetch_;
        public static AutoPilot fetch { get { return fetch_; } }

        private Vessel attachedVessel;

        public void Start()
        {
            fetch_ = this;
        }

        public void Update()
        {
            attachedVessel = FlightGlobals.ActiveVessel;
        }

        public Vector3 PlannedDirection
        {
            get
            {
                if (attachedVessel == null)
                    return new Vector3(0, 0, 0);

                CelestialBody body = attachedVessel.mainBody;

                Vector3d pos = attachedVessel.GetWorldPos3D() - body.position;
                Vector3d vel = attachedVessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity

                Vector3 up = pos.normalized;
                Vector3 velRight = Vector3.Cross(vel, up).normalized;
                Vector3 velUp = Vector3.Cross(velRight, vel).normalized;

                float plannedAngleOfAttack = (float)DescentProfile.fetch.GetAngleOfAttack(Trajectory.fetch.targetBody, pos, vel);

                return vel.normalized * Mathf.Cos(plannedAngleOfAttack) + velUp * Mathf.Sin(plannedAngleOfAttack);
            }
        }

        public Vector2 Correction
        {
            get
            {
                if (attachedVessel == null)
                    return new Vector2(0, 0);

                Vector3? targetPosition = Trajectory.fetch.targetPosition;
                var patch = Trajectory.fetch.patches.LastOrDefault();
                CelestialBody body = Trajectory.fetch.targetBody;
				if (!targetPosition.HasValue || patch == null || !patch.impactPosition.HasValue || patch.startingState.referenceBody != body || !patch.isAtmospheric)
                    return new Vector2(0, 0);

                // Get impact position, or, if some point over the trajectory has not enough clearance, smoothly interpolate to that point depending on how much clearance is missing
                Vector3 impactPosition = patch.impactPosition.Value;
                foreach(var p in patch.atmosphericTrajectory)
                {
                    float neededClearance = 600.0f;
                    float missingClearance = neededClearance - (p.pos.magnitude - (float)body.Radius - p.groundAltitude);
                    if (missingClearance > 0.0f)
                    {
                        if(Vector3.Distance(p.pos, patch.rawImpactPosition.Value) > 3000.0f)
                        {
                            float coeff = missingClearance / neededClearance;
                            Vector3 rotatedPos = p.pos;
                            if(!Settings.fetch.BodyFixedMode)
                            {
                                rotatedPos = Trajectory.calculateRotatedPosition(body, p.pos, p.time);
                            }
                            impactPosition = impactPosition * (1.0f - coeff) + rotatedPos * coeff;
                        }
                        break;
                    }
                }

                Vector3 right = Vector3.Cross(patch.impactVelocity, impactPosition).normalized;
                Vector3 behind = Vector3.Cross(right, impactPosition).normalized;

                Vector3 offset = targetPosition.Value - impactPosition;
                Vector2 offsetDir = new Vector2(Vector3.Dot(right, offset), Vector3.Dot(behind, offset));
                offsetDir *= 0.00005f; // 20km <-> 1 <-> 45° (this is purely indicative, no physical meaning, it would be very complicated to compute an actual correction angle as it depends on the spacecraft behavior in the atmosphere ; a small angle will suffice for a plane, but even a big angle might do almost nothing for a rocket)

                Vector3d pos = attachedVessel.GetWorldPos3D() - body.position;
                Vector3d vel = attachedVessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity
                float plannedAngleOfAttack = (float)DescentProfile.fetch.GetAngleOfAttack(body, pos, vel);
                if (plannedAngleOfAttack < Math.PI * 0.5f)
                    offsetDir.y = -offsetDir.y; // behavior is different for prograde or retrograde entry

                float maxCorrection = 1.0f;
                offsetDir.x = Mathf.Clamp(offsetDir.x, -maxCorrection, maxCorrection);
                offsetDir.y = Mathf.Clamp(offsetDir.y, -maxCorrection, maxCorrection);

                return offsetDir;
            }
        }

        public Vector3 CorrectedDirection
        {
            get
            {
                if (attachedVessel == null)
                    return new Vector3(0, 0, 0);

                CelestialBody body = attachedVessel.mainBody;

                Vector3d pos = attachedVessel.GetWorldPos3D() - body.position;
                Vector3d vel = attachedVessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity

                Vector3 referenceVector = PlannedDirection;

                Vector3 up = pos.normalized;
                Vector3 velRight = Vector3.Cross(vel, up).normalized;

                Vector3 refUp = Vector3.Cross(velRight, referenceVector).normalized;
                Vector3 refRight = velRight;

                Vector2 offsetDir = Correction;

                return referenceVector + refUp * offsetDir.y + refRight * offsetDir.x;
            }
        }
    }
}
