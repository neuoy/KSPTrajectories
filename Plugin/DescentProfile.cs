/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

using System;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class DescentProfile: MonoBehaviour
    {
        public class Node
        {
            public string Name { get; private set; }
            public string Description { get; private set; }
            public string Horizon_txt { get; private set; }
            public string Angle_txt { get; private set; }
            private bool horizon;   // If true, angle is relative to horizon, otherwise it's relative to velocity (i.e. angle of attack)
            private double angle;   // In radians
            private float sliderPos;

            public bool Horizon
            {
                get => horizon;
                set
                {
                    horizon = value;
                    Horizon_txt = value ? "Horiz" : "AoA";
                }
            }

            public double Angle
            {
                get => angle;
                set
                {
                    if (Math.Abs(value) < 0.00001)
                        angle = 0d;
                    else
                        angle = value;

                    double calc_angle = angle * 180.0 / Math.PI;
                    if (calc_angle <= -100d || calc_angle >= 100d)
                        Angle_txt = calc_angle.ToString("F1") + "°";
                    else if (calc_angle <= -10d || calc_angle >= 10d)
                        Angle_txt = calc_angle.ToString("F2") + "°";
                    else
                        Angle_txt = calc_angle.ToString("F3") + "°";
                }
            }

            public float SliderPos
            {
                get => sliderPos;
                set
                {
                    sliderPos = value;
                    Angle = value * value * value * Math.PI; // This helps to have high precision near 0° while still allowing big angles
                }
            }

            //  constructor
            public Node(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public void RefreshSliderPos()
            {
                float position = (float)Math.Pow(Math.Abs(Angle) / Math.PI, 1d / 3d);
                if (Angle < 0d)
                    SliderPos = -position;
                else
                    SliderPos = position;
            }

            public double GetAngleOfAttack(Vector3d position, Vector3d velocity)
            {
                if (!Horizon)
                    return Angle;

                return Math.Acos(Vector3d.Dot(position, velocity) / (position.magnitude * velocity.magnitude)) - Math.PI * 0.5 + Angle;
            }

            [Obsolete("use MainGUI")]
            public void DoGUI()
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(Name, Description), GUILayout.Width(45));
                Horizon = GUILayout.Toggle(Horizon, new GUIContent(Horizon_txt, "AoA = Angle of Attack = angle relatively to the velocity vector.\nHoriz = angle relatively to the horizon."), GUILayout.Width(45));
                SliderPos = GUILayout.HorizontalSlider(SliderPos, -1.0f, 1.0f, GUILayout.Width(90));
                GUILayout.Label(Angle_txt, GUILayout.Width(42));
                GUILayout.EndHorizontal();
            }
        }

        public Node entry;
        public Node highAltitude;
        public Node lowAltitude;
        public Node finalApproach;

        public bool ProgradeEntry { get; set; }

        public bool RetrogradeEntry { get; set; }

        private Vessel attachedVessel;

        // permit global access
        public static DescentProfile fetch { get; private set; }

        //  constructors
        public DescentProfile()
        {
            fetch = this;
            Allocate();
            Reset();
        }

        public DescentProfile(double AoA)
        {
            fetch = this;
            Allocate();
            Reset(AoA);
        }

        private void OnDestroy()
        {
            fetch = null;
            entry = null;
            highAltitude = null;
            lowAltitude = null;
            finalApproach = null;
        }

        private void Allocate()
        {
            entry = new Node(Localizer.Format("#autoLOC_Trajectories_Entry"), Localizer.Format("#autoLOC_Trajectories_EntryDesc"));
            highAltitude = new Node(Localizer.Format("#autoLOC_6001508"), Localizer.Format("#autoLOC_Trajectories_HighDesc"));
            lowAltitude = new Node(Localizer.Format("#autoLOC_7000051"), Localizer.Format("#autoLOC_Trajectories_LowDesc"));
            finalApproach = new Node(Localizer.Format("#autoLOC_453573"), Localizer.Format("#autoLOC_Trajectories_GroundDesc"));
        }

        public void Reset(double AoA = 0d)
        {
            entry.Angle = AoA;
            entry.Horizon = false;
            highAltitude.Angle = AoA;
            highAltitude.Horizon = false;
            lowAltitude.Angle = AoA;
            lowAltitude.Horizon = false;
            finalApproach.Angle = AoA;
            finalApproach.Horizon = false;

            ProgradeEntry = AoA == 0d;
            RetrogradeEntry = AoA == Math.PI;

            RefreshSliders();
        }

        private void RefreshSliders()
        {
            entry.RefreshSliderPos();
            highAltitude.RefreshSliderPos();
            lowAltitude.RefreshSliderPos();
            finalApproach.RefreshSliderPos();
        }

        private void Update()
        {
            if (Util.IsFlight && (attachedVessel != FlightGlobals.ActiveVessel))
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
                        entry.Angle = module.EntryAngle;
                        entry.Horizon = module.EntryHorizon;
                        highAltitude.Angle = module.HighAngle;
                        highAltitude.Horizon = module.HighHorizon;
                        lowAltitude.Angle = module.LowAngle;
                        lowAltitude.Horizon = module.LowHorizon;
                        finalApproach.Angle = module.GroundAngle;
                        finalApproach.Horizon = module.GroundHorizon;

                        ProgradeEntry = module.ProgradeEntry;
                        RetrogradeEntry = module.RetrogradeEntry;

                        RefreshSliders();

                        //Debug.Log("Descent profile loaded");
                    }
                }
            }
        }

        public void Save()
        {
            if (attachedVessel == null)
                return;

            foreach (var module in attachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
            {
                module.EntryAngle = entry.Angle;
                module.EntryHorizon = entry.Horizon;
                module.HighAngle = highAltitude.Angle;
                module.HighHorizon = highAltitude.Horizon;
                module.LowAngle = lowAltitude.Angle;
                module.LowHorizon = lowAltitude.Horizon;
                module.GroundAngle = finalApproach.Angle;
                module.GroundHorizon = finalApproach.Horizon;

                module.ProgradeEntry = ProgradeEntry;
                module.RetrogradeEntry = RetrogradeEntry;
            }
        }

        [Obsolete("use MainGUI")]
        public void DoGUI()
        {
            entry.DoGUI();
            highAltitude.DoGUI();
            lowAltitude.DoGUI();
            finalApproach.DoGUI();
            CheckGUI();
        }

        [Obsolete("use MainGUI")]
        public void DoQuickControlsGUI()
        {
            bool newPrograde = GUILayout.Toggle(ProgradeEntry, "Progr.", GUILayout.Width(50));
            bool newRetrograde = GUILayout.Toggle(RetrogradeEntry, "Retro.", GUILayout.Width(50));

            if (newPrograde && !ProgradeEntry)
            {
                Reset();
                Save();
            }
            else if (newRetrograde && !RetrogradeEntry)
            {
                Reset(Math.PI);
                Save();
            }
        }

        public void CheckGUI()
        {
            double? AoA = entry.Horizon ? (double?)null : entry.Angle;

            if (highAltitude.Angle != AoA || highAltitude.Horizon)
                AoA = null;
            if (lowAltitude.Angle != AoA || lowAltitude.Horizon)
                AoA = null;
            if (finalApproach.Angle != AoA || finalApproach.Horizon)
                AoA = null;

            if (!AoA.HasValue)
            {
                ProgradeEntry = false;
                RetrogradeEntry = false;
            }
            else
            {
                if (Math.Abs(AoA.Value) < 0.00001)
                    ProgradeEntry = true;
                if (Math.Abs((Math.Abs(AoA.Value) - Math.PI)) < 0.00001)
                    RetrogradeEntry = true;
            }

            Save();
        }

        /// <summary>
        /// Computes the angle of attack to follow the current profile if the aircraft is at the specified position
        /// (in world frame, but relative to the body) with the specified velocity
        /// (relative to the air, so it takes the body rotation into account)
        /// </summary>
        public double GetAngleOfAttack(CelestialBody body, Vector3d position, Vector3d velocity)
        {
            double altitude = position.magnitude - body.Radius;
            double altitudeRatio = body.atmosphere ? altitude / body.atmosphereDepth : 0;

            Node a, b;
            double aCoeff;

            if (altitudeRatio > 0.5)  // Atmospheric entry
            {
                a = entry;
                b = highAltitude;
                aCoeff = Math.Min((altitudeRatio - 0.5) * 2.0, 1.0);
            }
            else if (altitudeRatio > 0.25)  // High Altitude
            {
                a = highAltitude;
                b = lowAltitude;
                aCoeff = altitudeRatio * 4.0 - 1.0;
            }
            else if (altitudeRatio > 0.05)  // Low Altitude
            {
                a = lowAltitude;
                b = finalApproach;
                aCoeff = altitudeRatio * 5.0 - 0.25;

                aCoeff = 1.0 - aCoeff;
                aCoeff = 1.0 - aCoeff * aCoeff;
            }
            else    // Final Approach or Non-Atmospheric Body
            {
                return finalApproach.GetAngleOfAttack(position, velocity);
            }

            double aAoA = a.GetAngleOfAttack(position, velocity);
            double bAoA = b.GetAngleOfAttack(position, velocity);

            return aAoA * aCoeff + bAoA * (1.0 - aCoeff);
        }
    }
}
