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
using KSP.Localization;
using UnityEngine;

namespace Trajectories
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class DescentProfile : MonoBehaviour
    {
        private const double GUI_MAX_ANGLE = Math.PI / 2d;

        public class Node
        {
            private bool retrograde;
            private bool horizon;           // If true, angle is relative to horizon, otherwise it's relative to velocity (i.e. angle of attack)
            private double angle_rad;       // In radians
            private double angle_deg;       // In degrees
            private float sliderPos;

            public string Name { get; private set; }
            public string Description { get; private set; }
            public string Horizon_txt { get; private set; }
            public string Angle_txt { get; private set; }

            public bool Horizon
            {
                get => horizon;
                set
                {
                    horizon = value;
                    Horizon_txt = value ? "Horiz" : "AoA";
                }
            }

            public double AngleRad
            {
                get => angle_rad;
                set
                {
                    angle_rad = Util.ClampAbs(value, 0.00001d, Math.PI, 0d, Math.PI);
                    angle_deg = angle_rad * Mathf.Rad2Deg;
                    RefreshGui();
                }
            }

            public double AngleDeg
            {
                get => angle_deg;
                set
                {
                    angle_deg = Util.ClampAbs(value, 0.00001d, 180d, 0d, 180d);
                    angle_rad = angle_deg * Mathf.Deg2Rad;
                    RefreshGui();
                }
            }

            public bool Retrograde
            {
                get => retrograde;
                set
                {
                    retrograde = value;
                    if ((retrograde && (Math.Abs(angle_rad) < GUI_MAX_ANGLE)) || (!retrograde && (Math.Abs(angle_rad) > GUI_MAX_ANGLE)))
                    {
                        if (angle_rad < 0d)
                            AngleRad = -Math.PI - angle_rad;
                        else
                            AngleRad = Math.PI - angle_rad;
                    }
                }
            }

            public float SliderPos
            {
                get => sliderPos;
                set
                {
                    // convert slider position into radians rounded to 0.5 degrees
                    double gui_angle_rad = (Math.Round(((value * GUI_MAX_ANGLE) * Mathf.Rad2Deg) * 2d, 0) * 0.5d) * Mathf.Deg2Rad;

                    if (retrograde)
                    {
                        if (gui_angle_rad < 0d)
                            AngleRad = -Math.PI - gui_angle_rad;
                        else
                            AngleRad = Math.PI - gui_angle_rad;
                    }
                    else
                    {
                        AngleRad = gui_angle_rad;
                    }
                }
            }

            //  constructor
            public Node(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public void RefreshGui()
            {
                // check if angle is retrograde or prograde
                double gui_angle_rad = AngleRad;

                if (Math.Abs(gui_angle_rad) > GUI_MAX_ANGLE)
                {
                    // angle is retrograde
                    retrograde = true;
                    if (gui_angle_rad < 0d)
                        gui_angle_rad = -Math.PI - gui_angle_rad;
                    else
                        gui_angle_rad = Math.PI - gui_angle_rad;
                }

                // update gui descent page slider position
                float position = (float)(Math.Abs(gui_angle_rad) / GUI_MAX_ANGLE);
                if (gui_angle_rad < 0d)
                    sliderPos = -position;
                else
                    sliderPos = position;

                // update gui descent page angle text
                Angle_txt = (gui_angle_rad * Mathf.Rad2Deg).ToString("F1") + "°";
            }

            public double GetAngleOfAttack(Vector3d position, Vector3d velocity)
            {
                if (!Horizon)
                    return AngleRad;

                return Math.Acos(Vector3d.Dot(position, velocity) / (position.magnitude * velocity.magnitude)) - Math.PI * 0.5 + AngleRad;
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

        private Vessel attachedVessel;
        private bool retrograde_entry;

        public Node entry;
        public Node highAltitude;
        public Node lowAltitude;
        public Node finalApproach;

        /// <summary>
        /// Sets the profile to Pro/Retrograde or Returns true if a Retrograde entry is selected.
        /// </summary>
        public bool RetrogradeEntry
        {
            get => retrograde_entry;
            set
            {
                entry.Retrograde = value;
                highAltitude.Retrograde = value;
                lowAltitude.Retrograde = value;
                finalApproach.Retrograde = value;
                retrograde_entry = value;
            }
        }

        // permit global access
        public static DescentProfile fetch { get; private set; } = null;

        //  constructors
        public DescentProfile()
        {
            fetch = this;
            Allocate();
        }

        // Awake is called only once when the script instance is being loaded. Used in place of the constructor for initialization.
        public void Awake()
        {
            if (Settings.fetch.DefaultDescentIsRetro)
                Reset();    // Reset to 180 degrees (retrograde)
            else
                Reset(0d);  // Reset to 0 degrees (prograde)
        }

        public void OnDestroy()
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
            highAltitude = new Node(Localizer.Format("#autoLOC_Trajectories_High"), Localizer.Format("#autoLOC_Trajectories_HighDesc"));
            lowAltitude = new Node(Localizer.Format("#autoLOC_Trajectories_Low"), Localizer.Format("#autoLOC_Trajectories_LowDesc"));
            finalApproach = new Node(Localizer.Format("#autoLOC_Trajectories_Final"), Localizer.Format("#autoLOC_Trajectories_FinalDesc"));
        }

        /// <summary>
        /// Resets the descent profile to the given AoA value in radians, default value is Retrograde =(PI = 180 degrees)
        /// </summary>
        public void Reset(double AoA = Math.PI)
        {
            //Util.DebugLog("Resetting vessel descent profile to {0} degrees", AoA));
            RetrogradeEntry = Math.Abs(AoA) > GUI_MAX_ANGLE;   // sets to prograde entry if AoA is greater than +-PI/2 (+-90 degrees)

            entry.AngleRad = AoA;
            entry.Horizon = false;
            highAltitude.AngleRad = AoA;
            highAltitude.Horizon = false;
            lowAltitude.AngleRad = AoA;
            lowAltitude.Horizon = false;
            finalApproach.AngleRad = AoA;
            finalApproach.Horizon = false;

            RefreshGui();
        }

        public void RefreshGui()
        {
            entry.RefreshGui();
            highAltitude.RefreshGui();
            lowAltitude.RefreshGui();
            finalApproach.RefreshGui();

            RetrogradeEntry = entry.Retrograde || highAltitude.Retrograde || lowAltitude.Retrograde || finalApproach.Retrograde;
        }

        public void Update()
        {
            if (attachedVessel != FlightGlobals.ActiveVessel)
            {
                //Util.DebugLog("Loading vessels descent profile");
                attachedVessel = FlightGlobals.ActiveVessel;

                if (attachedVessel == null)
                {
                    //Util.DebugLog("No vessel");
                    if (Settings.fetch.DefaultDescentIsRetro)
                        Reset();
                    else
                        Reset(0d);
                }
                else
                {
                    TrajectoriesVesselSettings module = attachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                    if (module == null)
                    {
                        //Util.DebugLog("No TrajectoriesVesselSettings module");
                        if (Settings.fetch.DefaultDescentIsRetro)
                            Reset();
                        else
                            Reset(0d);
                    }
                    else if (!module.Initialized)
                    {
                        //Util.DebugLog("Initializing TrajectoriesVesselSettings module");
                        if (Settings.fetch.DefaultDescentIsRetro)
                            Reset();
                        else
                            Reset(0d);

                        module.EntryAngle = entry.AngleRad;
                        module.EntryHorizon = entry.Horizon;
                        module.HighAngle = highAltitude.AngleRad;
                        module.HighHorizon = highAltitude.Horizon;
                        module.LowAngle = lowAltitude.AngleRad;
                        module.LowHorizon = lowAltitude.Horizon;
                        module.GroundAngle = finalApproach.AngleRad;
                        module.GroundHorizon = finalApproach.Horizon;

                        module.RetrogradeEntry = RetrogradeEntry;

                        module.Initialized = true;
                    }
                    else
                    {
                        //Util.DebugLog("Reading settings...");
                        RetrogradeEntry = module.RetrogradeEntry;

                        entry.AngleRad = module.EntryAngle;
                        entry.Horizon = module.EntryHorizon;
                        highAltitude.AngleRad = module.HighAngle;
                        highAltitude.Horizon = module.HighHorizon;
                        lowAltitude.AngleRad = module.LowAngle;
                        lowAltitude.Horizon = module.LowHorizon;
                        finalApproach.AngleRad = module.GroundAngle;
                        finalApproach.Horizon = module.GroundHorizon;


                        RefreshGui();

                        //Util.DebugLog("Descent profile loaded");
                    }
                }
            }
        }

        public void Save()
        {
            if (attachedVessel == null)
                return;

            //Util.DebugLog("Saving vessels descent profile");
            foreach (TrajectoriesVesselSettings module in attachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
            {
                module.EntryAngle = entry.AngleRad;
                module.EntryHorizon = entry.Horizon;
                module.HighAngle = highAltitude.AngleRad;
                module.HighHorizon = highAltitude.Horizon;
                module.LowAngle = lowAltitude.AngleRad;
                module.LowHorizon = lowAltitude.Horizon;
                module.GroundAngle = finalApproach.AngleRad;
                module.GroundHorizon = finalApproach.Horizon;

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
        }

        [Obsolete("use MainGUI")]
        public void DoQuickControlsGUI()
        {
            bool newPrograde = GUILayout.Toggle(!RetrogradeEntry, "Progr.", GUILayout.Width(50));
            bool newRetrograde = GUILayout.Toggle(RetrogradeEntry, "Retro.", GUILayout.Width(50));

            if (newPrograde && RetrogradeEntry)
            {
                Reset(0d);
                Save();
            }
            else if (newRetrograde && !RetrogradeEntry)
            {
                Reset();
                Save();
            }
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
