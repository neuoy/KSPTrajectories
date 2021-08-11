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

        internal class Node
        {
            private bool retrograde;
            private double angle_rad;       // In radians
            private double angle_deg;       // In degrees
            private float sliderPos;

            public string Name { get; private set; }
            public string Description { get; private set; }
            public string HorizonText => Horizon ? "Horiz" : "AoA";
            public string AngleText { get; private set; }

            /// <summary> If true, angle is relative to horizon, otherwise it's relative to velocity (i.e. angle of attack relative to 0° prograde) </summary>
            public bool Horizon { get; set; }


            /// <summary> Angle in radians </summary>
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

            /// <summary> Angle in degrees </summary>
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

            /// <summary> True if angle is retrograde </summary>
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

            /// <summary> Slider position for the Gui </summary>
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

            // constructor
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
                AngleText = (gui_angle_rad * Mathf.Rad2Deg).ToString("F1") + "°";
            }

            public double GetAngleOfAttack(Vector3d position, Vector3d velocity)
            {
                if (!Horizon)
                    return AngleRad;

                return Math.Acos(Vector3d.Dot(position, velocity) / (position.magnitude * velocity.magnitude)) - Util.HALF_PI + AngleRad;
            }
        }

        internal static Node AtmosEntry { get; private set; }
        internal static Node HighAltitude { get; private set; }
        internal static Node LowAltitude { get; private set; }
        internal static Node FinalApproach { get; private set; }

        /// <returns> true if all nodes have been allocated. </returns>
        internal static bool Ready => (AtmosEntry != null && HighAltitude != null && LowAltitude != null && FinalApproach != null);

        /// <summary> Sets all the profile nodes to Retrograde if true or Prograde if false. </summary>
        internal static bool Retrograde
        {
            set
            {
                if (Ready)
                {
                    AtmosEntry.Retrograde = value;
                    HighAltitude.Retrograde = value;
                    LowAltitude.Retrograde = value;
                    FinalApproach.Retrograde = value;
                }
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
                AtmosEntry ??= new Node(Localizer.Format("#autoLOC_Trajectories_Entry"), Localizer.Format("#autoLOC_Trajectories_EntryDesc"));
                HighAltitude ??= new Node(Localizer.Format("#autoLOC_Trajectories_High"), Localizer.Format("#autoLOC_Trajectories_HighDesc"));
                LowAltitude ??= new Node(Localizer.Format("#autoLOC_Trajectories_Low"), Localizer.Format("#autoLOC_Trajectories_LowDesc"));
                FinalApproach ??= new Node(Localizer.Format("#autoLOC_Trajectories_Final"), Localizer.Format("#autoLOC_Trajectories_FinalDesc"));
            }

            Clear();
        }

        /// <summary> Releases held resources. </summary>
        internal static void Destroy()
        {
            Util.DebugLog("");
            AtmosEntry = null;
            HighAltitude = null;
            LowAltitude = null;
            FinalApproach = null;
        }

        /// <summary> Resets nodes to defaults </summary>
        internal static void Clear()
        {
            Util.DebugLog("");
            if (Settings.DefaultDescentIsRetro)
                Reset();
            else
                Reset(0d);
        }

        /// <summary>
        /// Resets the descent profile to the given AoA value in radians, default value is Retrograde =(PI = 180 degrees)
        /// </summary>
        internal static void Reset(double AoA = Math.PI)
        {
            if (!Ready)
                return;

            //Util.DebugLog("Resetting vessel descent profile to {0} degrees", AoA));
            Retrograde = Math.Abs(AoA) > GUI_MAX_ANGLE;   // sets to retrograde entry if AoA is greater than +-PI/2 (+-90 degrees)

            AtmosEntry.AngleRad = AoA;
            AtmosEntry.Horizon = false;
            HighAltitude.AngleRad = AoA;
            HighAltitude.Horizon = false;
            LowAltitude.AngleRad = AoA;
            LowAltitude.Horizon = false;
            FinalApproach.AngleRad = AoA;
            FinalApproach.Horizon = false;

            RefreshGui();
        }

        internal static void RefreshGui()
        {
            if (!Ready)
                return;

            AtmosEntry.RefreshGui();
            HighAltitude.RefreshGui();
            LowAltitude.RefreshGui();
            FinalApproach.RefreshGui();
        }

        /// <summary> Saves the profile to the passed vessel module </summary>
        internal static void Save(TrajectoriesVesselSettings module)
        {
            module.EntryAngle = AtmosEntry.AngleRad;
            module.EntryHorizon = AtmosEntry.Horizon;
            module.HighAngle = HighAltitude.AngleRad;
            module.HighHorizon = HighAltitude.Horizon;
            module.LowAngle = LowAltitude.AngleRad;
            module.LowHorizon = LowAltitude.Horizon;
            module.GroundAngle = FinalApproach.AngleRad;
            module.GroundHorizon = FinalApproach.Horizon;
        }

        /// <summary> Saves the profile to the active vessel module </summary>
        internal static void Save()
        {
            if (!Trajectories.IsVesselAttached || !Ready)
                return;

            //Util.DebugLog("Saving vessels descent profile...");
            foreach (TrajectoriesVesselSettings module in Trajectories.AttachedVessel.Parts.SelectMany(p => p.Modules.OfType<TrajectoriesVesselSettings>()))
            {
                Save(module);
            }
            //Util.DebugLog("Descent profile saved");
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

            double altitudeRatio = body.atmosphere ? (position.magnitude - body.Radius) / body.atmosphereDepth : 0d;

            Node a, b;
            double aCoeff;

            if (altitudeRatio > 0.5d)  // Atmospheric entry, 50 to 100% of body atmosphere depth
            {
                a = AtmosEntry;
                b = HighAltitude;
                aCoeff = Math.Min((altitudeRatio - 0.5d) * 2d, 1d);       // 0.5..1+ = 0..1
            }
            else if (altitudeRatio > 0.25d)  // High Altitude, 25 to 50% of body atmosphere depth
            {
                a = HighAltitude;
                b = LowAltitude;
                aCoeff = (altitudeRatio * 4d) - 1d;                       // 0.25..0.5 = 0..1
            }
            else if (altitudeRatio > 0.05d)  // Low Altitude, 5 to 25% of body atmosphere depth
            {
                a = LowAltitude;
                b = FinalApproach;
                aCoeff = 1d - ((altitudeRatio * 5d) - 0.25d);             // 0.05..0.25 = 0..1
            }
            else    // Final Approach, under 5% of body atmosphere depth or Non-Atmospheric Body
            {
                return FinalApproach.GetAngleOfAttack(position, velocity);
            }

            return Util.Lerp(b.GetAngleOfAttack(position, velocity), a.GetAngleOfAttack(position, velocity), aCoeff);
        }
    }
}
