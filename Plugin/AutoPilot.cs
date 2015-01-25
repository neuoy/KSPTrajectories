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

        public bool IsAvailable { get { return Settings.fetch.AutoPilotAvailable; } }

        private bool enabled_;
        public bool Enabled
        {
            get
            {
                return IsAvailable && enabled_;
            }

            set
            {
                if (enabled_ == value)
                    return;
                enabled_ = value;
                if(attachedVessel == null)
                    enabled_ = false;
                if (enabled_)
                {
                    // disable stock auto-pilot when ours is active
                    attachedVessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                }
            }
        }
        public float Strength { get; set; }
        public float Smoothness { get; set; }

        private Vessel attachedVessel;
        private FlightInputCallback callback;

        public void Start()
        {
            fetch_ = this;
        }

        public void Update()
        {
            Vessel activeVessel = HighLogic.LoadedScene == GameScenes.FLIGHT ? FlightGlobals.ActiveVessel : null;

            if (attachedVessel != activeVessel)
            {
                if (callback != null)
                {
                    attachedVessel.OnFlyByWire -= callback;
                    callback = null;
                }

                attachedVessel = activeVessel;

                if (attachedVessel != null)
                {
                    Strength = 5.0f;
                    Enabled = false;
                    TrajectoriesVesselSettings module = attachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                    if (module != null)
                    {
                        Enabled = module.AutoPilotEnabled;
                        Strength = module.AutoPilotStrength;
                        if (Strength < 0.5f)
                            Strength = 5.0f;
                    }
                    
                    callback = new FlightInputCallback((controls) => autoPilot(this, controls));
                    activeVessel.OnFlyByWire += callback;
                }
            }

            Save();
        }

        public void OnGUI()
        {
            if (attachedVessel == null || !IsAvailable)
                return;

            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;

            // get stock KSP buttons position
            VesselAutopilotUI aui = (VesselAutopilotUI)FindObjectOfType(typeof(VesselAutopilotUI));
            Vector2 stabilityAssistButtonCenter = new Vector2(aui.modeButtons[0].sprite.transform.position.x + Screen.width / 2, -aui.modeButtons[0].sprite.transform.position.y + Screen.height / 2);
            int x0 = (int)stabilityAssistButtonCenter.x - 10;
            int y0 = (int)stabilityAssistButtonCenter.y - 35;

            Enabled = GUI.Toggle(new Rect(x0, y0, 60, 30), Enabled, new GUIContent("Traj.", "Enable trajectory auto-pilot (atmospheric flight) ; touch controls or engage SAS to disable"));
            if (Enabled)
            {
                GUI.Label(new Rect(x0, y0+30, 60, 30), "Strong");
                Strength = GUI.VerticalSlider(new Rect(x0 + 10, y0 + 50, 30, 70), Strength, 10.0f, 0.5f);
                GUI.Label(new Rect(x0, y0 + 120, 60, 30), "Smooth");

                Smoothness = 10.5f - Strength;
            }
        }

        private void Save()
        {
            if (attachedVessel == null)
                return;

            foreach (var module in attachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
            {
                module.AutoPilotEnabled = Enabled;
                module.AutoPilotStrength = Strength;
            }
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
                if (!targetPosition.HasValue || patch == null || !patch.impactPosition.HasValue || patch.startingState.referenceBody != body)
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

                float maxCorrection = Math.Min(0.7f, 1.0f / Smoothness);
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

        private void autoPilot(AutoPilot pilot, FlightCtrlState controls)
        {
            controls.killRot = false;
            if (!controls.isIdle)
                Enabled = false;

            if (attachedVessel == null || !Enabled)
                return;

            Vessel vessel = attachedVessel;

            CelestialBody body = vessel.mainBody;
            Vector3d pos = vessel.GetWorldPos3D() - body.position;
            Vector3d airVelocity = vessel.obt_velocity - body.getRFrmVel(body.position + pos);

            Transform vesselTransform = vessel.ReferenceTransform;

            Vector3d vesselBackward = (Vector3d)(-vesselTransform.up.normalized);
            Vector3d vesselForward = -vesselBackward;
            Vector3d vesselUp = (Vector3d)(-vesselTransform.forward.normalized);
            Vector3d vesselRight = Vector3d.Cross(vesselUp, vesselBackward).normalized;

            Vector3d localVel = new Vector3d(Vector3d.Dot(vesselRight, airVelocity), Vector3d.Dot(vesselUp, airVelocity), Vector3d.Dot(vesselBackward, airVelocity));
            Vector3d prograde = localVel.normalized;

            Vector3d localGravityUp = new Vector3d(Vector3d.Dot(vesselRight, pos), Vector3d.Dot(vesselUp, pos), Vector3d.Dot(vesselBackward, pos)).normalized;
            Vector3d localProgradeRight = Vector3d.Cross(prograde, localGravityUp).normalized;
            localGravityUp = Vector3d.Cross(localProgradeRight, prograde);

            Vector3d vel = vessel.obt_velocity - body.getRFrmVel(body.position + pos); // air velocity
            double AoA = (float)DescentProfile.fetch.GetAngleOfAttack(body, pos, vel);
            Vector3d worldTargetDirection = CorrectedDirection;
            Vector3d targetDirection = new Vector3d(Vector3d.Dot(vesselRight, worldTargetDirection), Vector3d.Dot(vesselUp, worldTargetDirection), Vector3d.Dot(vesselBackward, worldTargetDirection));
            Vector3d targetUp = prograde * (-Math.Sin(AoA)) + localGravityUp * Math.Cos(AoA);

            Vector2 correction = Correction;

            float dirx = targetDirection.z > 0.0f ? Mathf.Sign((float)targetDirection.x) : (float)targetDirection.x;
            float diry = targetDirection.z > 0.0f ? Mathf.Sign((float)targetDirection.y) : (float)targetDirection.y;
            float dirz = targetUp.y < 0.0f ? Mathf.Sign((float)targetUp.x) : (float)targetUp.x;

            if(targetUp.y > 0.0f)
            {
                dirz += correction.x; // in case the craft has wings, roll it to use lift for left/right correction
            }

            float maxSteer = Mathf.Clamp(2.0f - Smoothness*0.2f, 0.1f, 1.0f);

            float warpDamp = 1.0f / TimeWarp.CurrentRate;
            controls.pitch = Mathf.Clamp(diry * Strength + vessel.angularVelocity.x * Smoothness, -maxSteer, maxSteer) * warpDamp;
            controls.yaw = Mathf.Clamp(-dirx * Strength + vessel.angularVelocity.z * Smoothness, -maxSteer, maxSteer) * warpDamp;
            controls.roll = Mathf.Clamp(-dirz * Strength + vessel.angularVelocity.y * Smoothness, -maxSteer, maxSteer) * warpDamp;
        }
    }
}
