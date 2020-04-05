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

using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
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

            // Angle in radiant
            public double Angle
            {
                get => angle;
                set
                {
                    if (Math.Abs(value) < 0.00001)
                        angle = 0d;
                    else
                        angle = value;

                    double calc_angle = angle * Mathf.Rad2Deg;
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

            public double GetAngleOfAttack(double AoS)
            {
                if (!Horizon)
                    return Angle;
                else
                    return AoS + Angle;
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

        public SortedList<double, (Node higher, Node lower, double transition)> NodeList;

        public bool ProgradeEntry { get { return !RetrogradeEntry; } }

        public bool RetrogradeEntry { get; set; }

        private Vessel attachedVessel;

        // permit global access
        public static DescentProfile fetch { get; private set; } = null;

        //  constructors
        public DescentProfile()
        {
            fetch = this;
            Allocate();
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        private void Awake()
        {
            Reset(Settings.fetch.DefaultDescentIsRetro);
        }

        private void OnDestroy()
        {
            fetch = null;
            entry = null;
            highAltitude = null;
            lowAltitude = null;
            finalApproach = null;
        }

        class revSort : IComparer<double> { public int Compare(double x, double y) { return y.CompareTo(x);  } }

        private void Allocate()
        {
            entry = new Node(Localizer.Format("#autoLOC_Trajectories_Entry"), Localizer.Format("#autoLOC_Trajectories_EntryDesc"));
            highAltitude = new Node(Localizer.Format("#autoLOC_Trajectories_High"), Localizer.Format("#autoLOC_Trajectories_HighDesc"));
            lowAltitude = new Node(Localizer.Format("#autoLOC_Trajectories_Low"), Localizer.Format("#autoLOC_Trajectories_LowDesc"));
            finalApproach = new Node(Localizer.Format("#autoLOC_Trajectories_Ground"), Localizer.Format("#autoLOC_Trajectories_GroundDesc"));
            // stores connection between the different settings
            NodeList = new SortedList<double, (Node higher, Node lower, double transition)>(new revSort())
            {
                { 0.50, (entry,        highAltitude,  8) }, // 0.55+1/5=0.575 -> 0.55 is transition interval
                { 0.25, (highAltitude, lowAltitude,   10) }, // 0.25+1/20=0.3 -> 0.25
                { 0.05, (lowAltitude,  finalApproach, 20) }  // 0.05+1/20=0.1 -> 0.05
            };
        }

        public void Reset(bool retrograde = false)
        {
            Debug.Log(string.Format("Resetting vessel descent profile with option retrograde: ", retrograde));

            RetrogradeEntry = retrograde;

            double orientationAngle = retrograde ? Math.PI : 0d;

            entry.Angle = highAltitude.Angle = lowAltitude.Angle = finalApproach.Angle = orientationAngle; 
            
            entry.Horizon = highAltitude.Horizon = lowAltitude.Horizon = finalApproach.Horizon = false;

            RefreshSliders();
        }

        private void RefreshSliders()
        {
            entry.RefreshSliderPos();
            highAltitude.RefreshSliderPos();
            lowAltitude.RefreshSliderPos();
            finalApproach.RefreshSliderPos();
        }

        public void Update()
        {
            if (attachedVessel != FlightGlobals.ActiveVessel)
            {
                //Debug.Log("Loading vessel descent profile");
                attachedVessel = FlightGlobals.ActiveVessel;

                if (attachedVessel == null)
                {
                    //Debug.Log("No vessel");
                    Reset(Settings.fetch.DefaultDescentIsRetro);
                    
                }
                else
                {
                    TrajectoriesVesselSettings module = attachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                    if (module == null)
                    {
                        //Debug.Log("No TrajectoriesVesselSettings module");
                        Reset(Settings.fetch.DefaultDescentIsRetro);
                        
                    }
                    else if (!module.Initialized)
                    {
                        //Debug.Log("Initializing TrajectoriesVesselSettings module");
                        Reset(Settings.fetch.DefaultDescentIsRetro);

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

                        module.Initialized = true;
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

                        //ProgradeEntry = module.ProgradeEntry;
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

            //Debug.Log("Saving vessel descent profile");
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
                module.Initialized = true;
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
                Reset(true);
                Save();
            }
        }

        public void CheckGUI()
        {
            /*double? AoA = entry.Horizon ? (double?)null : entry.Angle;

            if (highAltitude.Angle != AoA || highAltitude.Horizon)
                AoA = null;
            if (lowAltitude.Angle != AoA || lowAltitude.Horizon)
                AoA = null;
            if (finalApproach.Angle != AoA || finalApproach.Horizon)
                AoA = null;
                */
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

            foreach (var conf in NodeList)
            {
                if (conf.Key <= altitudeRatio)
                    return Util.dLerp(conf.Value.lower.GetAngleOfAttack(position, velocity),
                                      conf.Value.higher.GetAngleOfAttack(position, velocity),
                                      (altitudeRatio - conf.Key) * conf.Value.transition);
            }

            return 0; // should never happen
           
        }

        /// <summary>
        /// Computes the orientation Quaternion for API (intended for MechJeb).
        /// Note: we are using Unity directions, not KSP swapped forward/up stuff
        /// </summary>
        public Quaternion GetUnityOrientation(CelestialBody body, Vector3d position, Vector3d velocity)
        {
            if (ProgradeEntry)
            {
                return Quaternion.AngleAxis((float) GetAngleOfAttack(body, position, velocity) * Mathf.Rad2Deg, Vector3.right);
            }
            else
            {
                //retrograde is actually 180° rotation around Vesselup to keep orientation and then pitch for remaining AoA, not pitch by nearly 180°
                return Quaternion.AngleAxis((float) GetAngleOfAttack(body, position, velocity) * Mathf.Rad2Deg - 180f, Vector3.right) * Quaternion.AngleAxis(180f, Vector3.up);
            }
        }

        /// <summary>
        /// Computes the orientation Quaternion for rotations in ksp coordinates (flying up instead of forward)
        /// </summary>
        public Quaternion GetKspOrientation(CelestialBody body, Vector3d position, Vector3d velocity)
        {
            if (ProgradeEntry)
            {
                return Quaternion.AngleAxis((float)GetAngleOfAttack(body, position, velocity) * Mathf.Rad2Deg, Vector3.right);
            }
            else
            {
                //retrograde is actually 180° rotation around Vesselup to keep orientation and then pitch for remaining AoA, not pitch by nearly 180°
                return Quaternion.AngleAxis((float)GetAngleOfAttack(body, position, velocity) * Mathf.Rad2Deg - 180f, Vector3.right) * Quaternion.AngleAxis(180f, Vector3.forward);
            }
        }
    }
}
