/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
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
using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    /// <summary>
    /// API for Trajectories. Note: this API only returns correct values for the "active vessel".
    /// </summary>
    public static class API
    {
        /// <summary>
        /// Returns the version number of trajectories in a string formated as Major.Minor.Patch i.e. 2.1.0
        /// </summary>
        public static string GetVersion
        {
            get
            {
                string version_txt = typeof(API).Assembly.GetName().Version.ToString();
                version_txt = version_txt.Remove(version_txt.LastIndexOf("."));
                return version_txt;
            }
        }

        /// <summary>
        /// Returns the major version number of trajectories
        /// </summary>
        public static int GetVersionMajor
        {
            get
            {
                string[] version = GetVersion.Split('.');
                return System.Convert.ToInt32(version[0]);
            }
        }


        /// <summary>
        /// Returns the minor version number of trajectories
        /// </summary>
        public static int GetVersionMinor
        {
            get
            {
                string[] version = GetVersion.Split('.');
                return System.Convert.ToInt32(version[1]);
            }
        }


        /// <summary>
        /// Returns the patch version number of trajectories
        /// </summary>
        public static int GetVersionPatch
        {
            get
            {
                string[] version = GetVersion.Split('.');
                return System.Convert.ToInt32(version[2]);
            }
        }

        /// <summary>
        /// Modifies the AlwaysUpdate value in the settings page.
        /// </summary>
        public static bool AlwaysUpdate
        {
            get => Settings.AlwaysUpdate;
            set => Settings.AlwaysUpdate = value;
        }

        /// <summary>
        /// Returns trajectory patch EndTime or Null if no active vessel or calculated trajectory.
        /// See GetTimeTillImpact for remaining time until impact.
        /// </summary>
        public static double? GetEndTime()
        {
            if (Trajectories.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectory.Patches)
                {
                    if (patch.ImpactPosition.HasValue)
                        return patch.EndTime;
                }
            }
            return null;
        }


        /// <summary>
        /// Returns the remaining time until Impact in seconds or Null if no active vessel or calculated trajectory.
        /// </summary>
        public static double? GetTimeTillImpact()
        {
            if (Trajectories.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectory.Patches)
                {
                    if (patch.ImpactPosition.HasValue)
                        return patch.EndTime - Planetarium.GetUniversalTime();
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the calculated impact position of the trajectory or Null if no active vessel or calculated trajectory.
        /// </summary>
        public static Vector3? GetImpactPosition()
        {
            if (Trajectories.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectory.Patches)
                {
                    if (patch.ImpactPosition != null)
                        return patch.ImpactPosition;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the calculated impact velocity of the trajectory or Null if no active vessel or calculated trajectory.
        /// </summary>
        public static Vector3? GetImpactVelocity()
        {
            if (Trajectories.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectory.Patches)
                {
                    if (patch.ImpactVelocity != null)
                        return patch.ImpactVelocity;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the space orbit of the calculated trajectory or Null if orbit is atmospheric or there is no active vessel or calculated trajectory.
        /// </summary>
        public static Orbit GetSpaceOrbit()
        {
            if (Trajectories.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectory.Patches)
                {

                    if ((patch.StartingState.StockPatch != null) || patch.IsAtmospheric)
                        continue;

                    if (patch.SpaceOrbit != null)
                        return patch.SpaceOrbit;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if a target has been set, false if not, or Null if no active vessel.
        /// </summary>
        public static bool HasTarget() => Trajectories.IsVesselAttached && TargetProfile.HasTarget();

        /// <summary>
        /// Returns the planned direction or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3? PlannedDirection() => HasTarget() ? NavBallOverlay.PlannedDirection : null;

        /// <summary>
        /// Returns the corrected direction or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3? CorrectedDirection() => HasTarget() ? NavBallOverlay.CorrectedDirection : null;

        /// <summary>
        /// Set the trajectories target to a latitude, longitude and altitude at the  active vessels celestial body.
        /// </summary>
        public static void SetTarget(double lat, double lon, double? alt = null)
        {
            if (Trajectories.IsVesselAttached)
            {
                CelestialBody body = Trajectories.AttachedVessel.mainBody;
                if (body != null)
                    TargetProfile.SetFromLatLonAlt(body, lat, lon, alt);
            }
        }

        /// <summary>
        /// Returns the trajectories target as latitude, longitude and altitude at the HomeWorld or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3d? GetTarget() => Trajectories.IsVesselAttached ? TargetProfile.GetLatLonAlt() : null;

        /// <summary>
        /// Clears the trajectories target.
        /// </summary>
        public static void ClearTarget()
        {
            if (Trajectories.IsVesselAttached)
                TargetProfile.Clear();
        }

        /// <summary> Resets all the trajectories descent profile nodes to Prograde at 0° if true or Retrograde at 0° if false. </summary>
        /// <returns> true if all nodes are Prograde, null if no active vessel. </returns>
        public static bool? ProgradeEntry
        {
            get
            {
                if (Trajectories.IsVesselAttached)
                    return !DescentProfile.AtmosEntry.Retrograde && !DescentProfile.HighAltitude.Retrograde &&
                            !DescentProfile.LowAltitude.Retrograde && !DescentProfile.FinalApproach.Retrograde;
                return null;
            }
            set
            {
                if (Trajectories.IsVesselAttached && value.HasValue)
                {
                    DescentProfile.Reset(value.Value ? 0d : Math.PI);
                    DescentProfile.Save();
                }
            }
        }

        /// <summary> Sets all the trajectories descent profile nodes to Retrograde at 0° if true or Prograde at 0° if false. </summary>
        /// <returns> true if all nodes are Retrograde, null if no active vessel. </returns>
        public static bool? RetrogradeEntry
        {
            get
            {
                if (Trajectories.IsVesselAttached)
                    return DescentProfile.AtmosEntry.Retrograde && DescentProfile.HighAltitude.Retrograde &&
                            DescentProfile.LowAltitude.Retrograde && DescentProfile.FinalApproach.Retrograde;
                return null;
            }
            set
            {
                if (Trajectories.IsVesselAttached && value.HasValue)
                {
                    DescentProfile.Reset(value.Value ? Math.PI : 0d);
                    DescentProfile.Save();
                }
            }
        }

        /// <summary>
        /// Resets the trajectories descent profile to the passed AoA value in radians, default value is Retrograde =(PI = 180°),
		///  also sets Retrograde if angle is greater than ±PI/2 (±90°) otherwise sets to Prograde.
        /// </summary>
        public static void ResetDescentProfile(double AoA = Math.PI)
        {
            if (Trajectories.IsVesselAttached)
            {
                DescentProfile.Reset(AoA);
                DescentProfile.Save();
            }
        }

        /// <summary>
        /// Returns or sets the trajectories descent profile to the passed AoA values in radians, also sets Retrograde if AoA is greater than ±PI/2 (±90°).
        ///  Note. also use with the DescentProfileGrades property if using AoA values as displayed in the gui with max ±PI/2 (±90°).
        /// List order (0 = entry node, 1 = high altitude node, 2 = low altitude node, 3 = final approach node)
        /// Returns null if no active vessel.
        /// </summary>
        public static List<double> DescentProfileAngles
        {
            get
            {
                if (Trajectories.IsVesselAttached)
                {
                    return new List<double>
                    {
                        DescentProfile.AtmosEntry.AngleRad,
                        DescentProfile.HighAltitude.AngleRad,
                        DescentProfile.LowAltitude.AngleRad,
                        DescentProfile.FinalApproach.AngleRad
                    };
                }
                return null;
            }
            set
            {
                if (Trajectories.IsVesselAttached && value.Count == 4)
                {
                    DescentProfile.AtmosEntry.AngleRad = value[0];
                    DescentProfile.HighAltitude.AngleRad = value[1];
                    DescentProfile.LowAltitude.AngleRad = value[2];
                    DescentProfile.FinalApproach.AngleRad = value[3];
                    DescentProfile.Save();
                }
            }
        }

        /// <summary>
        /// Returns or set the trajectories descent profile modes, true = AoA, false = Horizon, returns null if no active vessel.
        /// List order (0 = entry node, 1 = high altitude node, 2 = low altitude node, 3 = final approach node)
        /// </summary>
        public static List<bool> DescentProfileModes
        {
            get
            {
                if (Trajectories.IsVesselAttached)
                {
                    return new List<bool>
                    {
                        !DescentProfile.AtmosEntry.Horizon,
                        !DescentProfile.HighAltitude.Horizon,
                        !DescentProfile.LowAltitude.Horizon,
                        !DescentProfile.FinalApproach.Horizon
                    };
                }
                return null;
            }
            set
            {
                if (Trajectories.IsVesselAttached && value.Count == 4)
                {
                    DescentProfile.AtmosEntry.Horizon = !value[0];
                    DescentProfile.HighAltitude.Horizon = !value[1];
                    DescentProfile.LowAltitude.Horizon = !value[2];
                    DescentProfile.FinalApproach.Horizon = !value[3];
                    DescentProfile.Save();
                }
            }
        }

        /// <summary>
        /// Returns or set the trajectories descent profile grades, true = Retrograde, false = Prograde, returns null if no active vessel.
        /// List order (0 = entry node, 1 = high altitude node, 2 = low altitude node, 3 = final approach node)
        /// </summary>
        public static List<bool> DescentProfileGrades
        {
            get
            {
                if (Trajectories.IsVesselAttached)
                {
                    return new List<bool>
                    {
                        DescentProfile.AtmosEntry.Retrograde,
                        DescentProfile.HighAltitude.Retrograde,
                        DescentProfile.LowAltitude.Retrograde,
                        DescentProfile.FinalApproach.Retrograde
                    };
                }
                return null;
            }
            set
            {
                if (Trajectories.IsVesselAttached && value.Count == 4)
                {
                    DescentProfile.AtmosEntry.Retrograde = value[0];
                    DescentProfile.HighAltitude.Retrograde = value[1];
                    DescentProfile.LowAltitude.Retrograde = value[2];
                    DescentProfile.FinalApproach.Retrograde = value[3];
                    DescentProfile.Save();
                }
            }
        }

        /// <summary>
        /// Triggers a recalculation of the trajectory.
        /// </summary>
        public static void UpdateTrajectory() => Trajectory.ComputeTrajectory();
    }
}
