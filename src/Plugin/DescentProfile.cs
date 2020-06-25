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
    internal static class DescentProfile
    {
        private const double GUI_MAX_ANGLE = Math.PI / 2d;

        private static Vessel attached_vessel;
        private static bool retrograde_entry;

        internal class Node
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

        internal static Node atmos_entry;
        internal static Node high_altitude;
        internal static Node low_altitude;
        internal static Node final_approach;

        /// <summary>
        /// Returns true if all nodes have been allocated.
        /// </summary>
        internal static bool Ready => (atmos_entry != null && high_altitude != null && low_altitude != null && final_approach != null);

        /// <summary>
        /// Sets the profile to Pro/Retrograde or Returns true if a Retrograde entry is selected.
        /// </summary>
        internal static bool RetrogradeEntry
        {
            get => retrograde_entry;
            set
            {
                if (Ready)
                {
                    atmos_entry.Retrograde = value;
                    high_altitude.Retrograde = value;
                    low_altitude.Retrograde = value;
                    final_approach.Retrograde = value;
                }
                retrograde_entry = value;
            }
        }

        /// <summary>
        /// Allocates new nodes or resets existing nodes
        /// </summary>
        internal static void Start()
        {
            Util.DebugLog(Ready ? "Resetting" : "Constructing");
            if (!Ready)
            {
                atmos_entry ??= new Node(Localizer.Format("#autoLOC_Trajectories_Entry"), Localizer.Format("#autoLOC_Trajectories_EntryDesc"));
                high_altitude ??= new Node(Localizer.Format("#autoLOC_Trajectories_High"), Localizer.Format("#autoLOC_Trajectories_HighDesc"));
                low_altitude ??= new Node(Localizer.Format("#autoLOC_Trajectories_Low"), Localizer.Format("#autoLOC_Trajectories_LowDesc"));
                final_approach ??= new Node(Localizer.Format("#autoLOC_Trajectories_Final"), Localizer.Format("#autoLOC_Trajectories_FinalDesc"));
            }

            if (Settings.DefaultDescentIsRetro)
            {
                Reset();
            }
            else
            {
                Reset(0d);
            }
        }

        /// <summary> Releases held resources. </summary>
        internal static void Destroy()
        {
            Util.DebugLog("");
            atmos_entry = null;
            high_altitude = null;
            low_altitude = null;
            final_approach = null;
            attached_vessel = null;
        }

        /// <summary>
        /// Updates the descent profile on a vessel change with the setting contained in the vessels <see cref="TrajectoriesVesselSettings"/> module.
        /// Resets the profile to default settings if no vessel or module is found.
        /// </summary>
        internal static void Update()
        {
            if (!Ready)
                return;

            if (attached_vessel != FlightGlobals.ActiveVessel)
            {
                Util.DebugLog("Loading vessels descent profile");
                attached_vessel = FlightGlobals.ActiveVessel;

                if (attached_vessel == null)
                {
                    Util.DebugLog("No vessel");
                    if (Settings.DefaultDescentIsRetro)
                        Reset();
                    else
                        Reset(0d);
                }
                else
                {
                    TrajectoriesVesselSettings module = attached_vessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()).FirstOrDefault();
                    if (module == null)
                    {
                        Util.DebugLog("No TrajectoriesVesselSettings module");
                        if (Settings.DefaultDescentIsRetro)
                            Reset();
                        else
                            Reset(0d);
                    }
                    else if (!module.Initialized)
                    {
                        //Util.DebugLog("Initializing TrajectoriesVesselSettings module");
                        if (Settings.DefaultDescentIsRetro)
                            Reset();
                        else
                            Reset(0d);

                        module.EntryAngle = atmos_entry.AngleRad;
                        module.EntryHorizon = atmos_entry.Horizon;
                        module.HighAngle = high_altitude.AngleRad;
                        module.HighHorizon = high_altitude.Horizon;
                        module.LowAngle = low_altitude.AngleRad;
                        module.LowHorizon = low_altitude.Horizon;
                        module.GroundAngle = final_approach.AngleRad;
                        module.GroundHorizon = final_approach.Horizon;

                        module.RetrogradeEntry = RetrogradeEntry;

                        module.Initialized = true;
                    }
                    else
                    {
                        Util.DebugLog("Reading profile settings...");
                        RetrogradeEntry = module.RetrogradeEntry;

                        atmos_entry.AngleRad = module.EntryAngle;
                        atmos_entry.Horizon = module.EntryHorizon;
                        high_altitude.AngleRad = module.HighAngle;
                        high_altitude.Horizon = module.HighHorizon;
                        low_altitude.AngleRad = module.LowAngle;
                        low_altitude.Horizon = module.LowHorizon;
                        final_approach.AngleRad = module.GroundAngle;
                        final_approach.Horizon = module.GroundHorizon;


                        RefreshGui();

                        Util.Log("Descent profile loaded");
                    }
                }
            }
        }

        /// <summary> Resets nodes to defaults and releases attached vessel. </summary>
        internal static void Clear()
        {
            Util.DebugLog("");
            if (Settings.DefaultDescentIsRetro)
                Reset();
            else
                Reset(0d);
            attached_vessel = null;
        }

        /// <summary>
        /// Resets the descent profile to the given AoA value in radians, default value is Retrograde =(PI = 180 degrees)
        /// </summary>
        internal static void Reset(double AoA = Math.PI)
        {
            if (!Ready)
                return;

            //Util.DebugLog("Resetting vessel descent profile to {0} degrees", AoA));
            RetrogradeEntry = Math.Abs(AoA) > GUI_MAX_ANGLE;   // sets to prograde entry if AoA is greater than +-PI/2 (+-90 degrees)

            atmos_entry.AngleRad = AoA;
            atmos_entry.Horizon = false;
            high_altitude.AngleRad = AoA;
            high_altitude.Horizon = false;
            low_altitude.AngleRad = AoA;
            low_altitude.Horizon = false;
            final_approach.AngleRad = AoA;
            final_approach.Horizon = false;

            RefreshGui();
        }

        internal static void RefreshGui()
        {
            if (!Ready)
                return;

            atmos_entry.RefreshGui();
            high_altitude.RefreshGui();
            low_altitude.RefreshGui();
            final_approach.RefreshGui();

            RetrogradeEntry = atmos_entry.Retrograde || high_altitude.Retrograde || low_altitude.Retrograde || final_approach.Retrograde;
        }

        internal static void Save()
        {
            if (attached_vessel == null || !Ready)
                return;

            //Util.DebugLog("Saving vessels descent profile");
            foreach (TrajectoriesVesselSettings module in attached_vessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
            {
                module.EntryAngle = atmos_entry.AngleRad;
                module.EntryHorizon = atmos_entry.Horizon;
                module.HighAngle = high_altitude.AngleRad;
                module.HighHorizon = high_altitude.Horizon;
                module.LowAngle = low_altitude.AngleRad;
                module.LowHorizon = low_altitude.Horizon;
                module.GroundAngle = final_approach.AngleRad;
                module.GroundHorizon = final_approach.Horizon;

                module.RetrogradeEntry = RetrogradeEntry;
                module.Initialized = true;
            }
        }

        [Obsolete("use MainGUI")]
        internal static void DoGUI()
        {
            if (!Ready)
                return;

            atmos_entry.DoGUI();
            high_altitude.DoGUI();
            low_altitude.DoGUI();
            final_approach.DoGUI();
        }

        [Obsolete("use MainGUI")]
        internal static void DoQuickControlsGUI()
        {
            if (!Ready)
                return;

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
        internal static double? GetAngleOfAttack(CelestialBody body, Vector3d position, Vector3d velocity)
        {
            if (!Ready)
                return null;

            double altitude = position.magnitude - body.Radius;
            double altitudeRatio = body.atmosphere ? altitude / body.atmosphereDepth : 0;

            Node a, b;
            double aCoeff;

            if (altitudeRatio > 0.5)  // Atmospheric entry
            {
                a = atmos_entry;
                b = high_altitude;
                aCoeff = Math.Min((altitudeRatio - 0.5) * 2.0, 1.0);
            }
            else if (altitudeRatio > 0.25)  // High Altitude
            {
                a = high_altitude;
                b = low_altitude;
                aCoeff = altitudeRatio * 4.0 - 1.0;
            }
            else if (altitudeRatio > 0.05)  // Low Altitude
            {
                a = low_altitude;
                b = final_approach;
                aCoeff = altitudeRatio * 5.0 - 0.25;

                aCoeff = 1.0 - aCoeff;
                aCoeff = 1.0 - aCoeff * aCoeff;
            }
            else    // Final Approach or Non-Atmospheric Body
            {
                return final_approach.GetAngleOfAttack(position, velocity);
            }

            double aAoA = a.GetAngleOfAttack(position, velocity);
            double bAoA = b.GetAngleOfAttack(position, velocity);

            return aAoA * aCoeff + bAoA * (1.0 - aCoeff);
        }
    }
}
