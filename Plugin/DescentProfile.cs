/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class DescentProfile : MonoBehaviour
    {
        struct Node
        {
            public string name;
            public string description;
            private float sliderPos;
            public double angle; // in radians
            public bool horizon; // if true, angle is relative to horizon, otherwise it's relative to velocity (i.e. angle of attack)

            static private readonly float maxAngle = 180.0f / 180.0f * Mathf.PI;

            public double GetAngleOfAttack(Vector3d position, Vector3d velocity)
            {
                if (!horizon)
                    return angle;

                return Math.Acos(Vector3d.Dot(position, velocity) / (position.magnitude * velocity.magnitude)) - Math.PI * 0.5 + angle;
            }

            public void DoGUI()
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(name, description), GUILayout.Width(50));
                horizon = GUILayout.Toggle(horizon, new GUIContent(horizon ? "Horiz" : "AoA", "AoA = Angle of Attack = angle relatively to the velocity vector.\nHoriz = angle relatively to the horizon."), GUILayout.Width(50));
                sliderPos = GUILayout.HorizontalSlider(sliderPos, -1.0f, 1.0f, GUILayout.Width(90));
                angle = (double)(sliderPos * sliderPos * sliderPos * maxAngle); // this helps to have high precision near 0° while still allowing big angles
                GUILayout.Label(Math.Round(angle * 180.0 / Math.PI).ToString() + "°", GUILayout.Width(30));
                GUILayout.EndHorizontal();
            }

            public void RefreshSliderPos()
            {
                bool negative = angle < 0;
                sliderPos = (float)Math.Pow(Math.Abs(angle) / maxAngle, 1.0 / 3.0);
                if (negative)
                    sliderPos = -sliderPos;
            }
        }
        
        private Node entry = new Node { name = "Entry", description = "Spacecraft angle when entering the atmosphere" };
        private Node highAltitude = new Node { name = "High", description = "Spacecraft angle at 50% of atmosphere height" };
        private Node lowAltitude = new Node { name = "Low", description = "Spacecraft angle at 25% of atmosphere height" };
        private Node finalApproach = new Node { name = "Ground", description = "Spacecraft angle near the ground" };

        private bool progradeEntry;
        private bool retrogradeEntry;

        private static DescentProfile fetch_;
        public static DescentProfile fetch { get { return fetch_; } }

        private Vessel attachedVessel;

        public DescentProfile()
        {
        }

        public DescentProfile(float AoA)
        {
            Reset(AoA);
        }

        public void Reset(float AoA = 0)
        {
            entry.angle = AoA;
            entry.horizon = false;

            highAltitude.angle = AoA;
            highAltitude.horizon = false;

            lowAltitude.angle = AoA;
            lowAltitude.horizon = false;

            finalApproach.angle = AoA;
            finalApproach.horizon = false;

            progradeEntry = AoA == 0;
            retrogradeEntry = AoA == 180;
        }

        public void Update()
        {
            if (HighLogic.LoadedScene == GameScenes.FLIGHT && attachedVessel != FlightGlobals.ActiveVessel)
            {
                //Debug.Log("Loading vessel descent profile");
                attachedVessel = FlightGlobals.ActiveVessel;

                if (attachedVessel == null)
                {
                    //Debug.Log("No vessel");
                    Reset();
                }
                else
                {
                    TrajectoriesVesselSettings module = attachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                    if (module == null)
                    {
                        //Debug.Log("No TrajectoriesVesselSettings module");
                        Reset();
                    }
                    else
                    {
                        //Debug.Log("Reading settings...");
                        entry.angle = module.EntryAngle;
                        entry.horizon = module.EntryHorizon;
                        highAltitude.angle = module.HighAngle;
                        highAltitude.horizon = module.HighHorizon;
                        lowAltitude.angle = module.LowAngle;
                        lowAltitude.horizon = module.LowHorizon;
                        finalApproach.angle = module.GroundAngle;
                        finalApproach.horizon = module.GroundHorizon;

                        entry.RefreshSliderPos();
                        highAltitude.RefreshSliderPos();
                        lowAltitude.RefreshSliderPos();
                        finalApproach.RefreshSliderPos();

                        progradeEntry = module.ProgradeEntry;
                        retrogradeEntry = module.RetrogradeEntry;
                        //Debug.Log("Descent profile loaded");
                    }
                }
            }
        }

        private void Save()
        {
            if(attachedVessel == null)
                return;

            foreach (var module in attachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
            {
                module.EntryAngle = (float)entry.angle;
                module.EntryHorizon = entry.horizon;
                module.HighAngle = (float)highAltitude.angle;
                module.HighHorizon = highAltitude.horizon;
                module.LowAngle = (float)lowAltitude.angle;
                module.LowHorizon = lowAltitude.horizon;
                module.GroundAngle = (float)finalApproach.angle;
                module.GroundHorizon = finalApproach.horizon;

                module.ProgradeEntry = progradeEntry;
                module.RetrogradeEntry = retrogradeEntry;
            }
        }

        public void Start()
        {
            fetch_ = this;
        }

        public void DoGUI()
        {
            Update();

            entry.DoGUI();
            highAltitude.DoGUI();
            lowAltitude.DoGUI();
            finalApproach.DoGUI();

            Save();
        }

        public void DoQuickControlsGUI()
        {
            Update();

            bool newPrograde = GUILayout.Toggle(progradeEntry, "Progr.", GUILayout.Width(50));
            bool newRetrograde = GUILayout.Toggle(retrogradeEntry, "Retro.", GUILayout.Width(50));

            bool presetActivated = false;
            if (newPrograde && !progradeEntry)
            {
                newRetrograde = false;
                presetActivated = true;
            }
            else if (newRetrograde && !retrogradeEntry)
            {
                newPrograde = false;
                presetActivated = true;
            }

            progradeEntry = newPrograde;
            retrogradeEntry = newRetrograde;

            if (presetActivated)
            {
                double setAoA = progradeEntry ? 0 : Math.PI;

                entry.angle = setAoA;
                entry.horizon = false;
                entry.RefreshSliderPos();
                highAltitude.angle = setAoA;
                highAltitude.horizon = false;
                highAltitude.RefreshSliderPos();
                lowAltitude.angle = setAoA;
                lowAltitude.horizon = false;
                lowAltitude.RefreshSliderPos();
                finalApproach.angle = setAoA;
                finalApproach.horizon = false;
                finalApproach.RefreshSliderPos();
            }
            else
            {
                double? AoA = entry.horizon ? (double?)null : entry.angle;
                if (highAltitude.angle != AoA || highAltitude.horizon) AoA = null;
                if (lowAltitude.angle != AoA || lowAltitude.horizon) AoA = null;
                if (finalApproach.angle != AoA || finalApproach.horizon) AoA = null;

                if (!AoA.HasValue || Math.Abs(AoA.Value - 0) > 0.01)
                    progradeEntry = false;
                if (!AoA.HasValue || Math.Abs(AoA.Value - Math.PI) > 0.01)
                    retrogradeEntry = false;
            }

            Save();
        }

        // Computes the angle of attack to follow the current profile if the aircraft is at the specified position (in world frame, but relative to the body) with the specified velocity (relative to the air, so it takes the body rotation into account)
        public double GetAngleOfAttack(CelestialBody body, Vector3d position, Vector3d velocity)
        {
            double altitude = position.magnitude - body.Radius;
            double altitudeRatio = body.atmosphere ? altitude / body.atmosphereDepth : 0;

            Node a, b;
            double aCoeff;
            
            if (altitudeRatio > 0.5)
            {
                a = entry;
                b = highAltitude;
                aCoeff = Math.Min((altitudeRatio - 0.5) * 2.0, 1.0);
            }
            else if(altitudeRatio > 0.25)
            {
                a = highAltitude;
                b = lowAltitude;
                aCoeff = altitudeRatio * 4.0 - 1.0;
            }
            else if (altitudeRatio > 0.05)
            {
                a = lowAltitude;
                b = finalApproach;
                aCoeff = altitudeRatio * 5.0 - 0.25;

                aCoeff = 1.0 - aCoeff;
                aCoeff = 1.0 - aCoeff * aCoeff;
            }
            else
            {
                return finalApproach.GetAngleOfAttack(position, velocity);
            }

            double aAoA = a.GetAngleOfAttack(position, velocity);
            double bAoA = b.GetAngleOfAttack(position, velocity);

            return aAoA * aCoeff + bAoA * (1.0 - aCoeff);
        }
    }
}
