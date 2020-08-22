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
                version_txt = version_txt.Remove(version_txt.LastIndexOf(".", StringComparison.Ordinal));
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
                return Convert.ToInt32(version[0]);
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
                return Convert.ToInt32(version[1]);
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
                return Convert.ToInt32(version[2]);
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
            if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectories.ActiveVesselTrajectory.Patches)
                {
                    if (patch.ImpactPosition.HasValue)
                        return patch.EndTime;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns trajectory patch EndTime or Null if no active vessel or calculated trajectory.
        /// See GetTimeTillImpact for remaining time until impact.
        /// </summary>
        public static double? GetEndTime(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory != null && trajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in trajectory.Patches)
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
            if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectories.ActiveVesselTrajectory.Patches)
                {
                    if (patch.ImpactPosition.HasValue)
                        return patch.EndTime - Planetarium.GetUniversalTime();
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the remaining time until Impact in seconds or Null if no active vessel or calculated trajectory.
        /// </summary>
        public static double? GetTimeTillImpact(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory != null && trajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in trajectory.Patches)
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
            if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectories.ActiveVesselTrajectory.Patches)
                {
                    if (patch.ImpactPosition != null)
                        return patch.ImpactPosition;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the calculated impact position of the trajectory or Null if no active vessel or calculated trajectory.
        /// </summary>
        public static Vector3? GetImpactPosition(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory != null && trajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in trajectory.Patches)
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
            if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectories.ActiveVesselTrajectory.Patches)
                {
                    if (patch.ImpactVelocity != null)
                        return patch.ImpactVelocity;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the calculated impact velocity of the trajectory or Null if no active vessel or calculated trajectory.
        /// </summary>
        public static Vector3? GetImpactVelocity(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory != null && trajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in trajectory.Patches)
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
            if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in Trajectories.ActiveVesselTrajectory.Patches)
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
        /// Returns the space orbit of the calculated trajectory or Null if orbit is atmospheric or there is no active vessel or calculated trajectory.
        /// </summary>
        public static Orbit GetSpaceOrbit(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory != null && trajectory.IsVesselAttached)
            {
                foreach (Trajectory.Patch patch in trajectory.Patches)
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
        public static bool HasTarget() => Trajectories.ActiveVesselTrajectory.IsVesselAttached && Trajectories.ActiveVesselTrajectory.TargetProfile.HasTarget();

        /// <summary>
        /// Returns true if a target has been set, false if not, or Null if no active vessel.
        /// </summary>
        public static bool HasTarget(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory == null) return false;
            return trajectory.IsVesselAttached && trajectory.TargetProfile.HasTarget();
        }

        /// <summary>
        /// Returns the planned direction or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3? PlannedDirection() => HasTarget() ? Trajectories.ActiveVesselTrajectory.NavBallOverlay.PlannedDirection : null;

        /// <summary>
        /// Returns the planned direction or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3? PlannedDirection(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            return HasTarget(vessel) ? trajectory?.NavBallOverlay.PlannedDirection : null;
        }

        /// <summary>
        /// Returns the corrected direction or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3? CorrectedDirection() => HasTarget() ? Trajectories.ActiveVesselTrajectory.NavBallOverlay.CorrectedDirection : null;

        /// <summary>
        /// Returns the corrected direction or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3? CorrectedDirection(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            return HasTarget(vessel) ? trajectory?.NavBallOverlay.CorrectedDirection : null;
        }

        /// <summary>
        /// Set the trajectories target to a latitude, longitude and altitude at the HomeWorld.
        /// </summary>
        public static void SetTarget(double lat, double lon, double? alt = null)
        {
            if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
            {
                CelestialBody body = FlightGlobals.GetHomeBody();
                if (body != null)
                    Trajectories.ActiveVesselTrajectory.TargetProfile.SetFromLatLonAlt(body, lat, lon, alt);
            }
        }

        /// <summary>
        /// Set the trajectories target to a latitude, longitude and altitude at the HomeWorld.
        /// </summary>
        public static void SetTarget(Vessel vessel, double lat, double lon, double? alt = null)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory != null && trajectory.IsVesselAttached)
            {
                CelestialBody body = FlightGlobals.GetHomeBody();
                if (body != null)
                    trajectory.TargetProfile.SetFromLatLonAlt(body, lat, lon, alt);
            }
        }

        /// <summary>
        /// Returns the trajectories target as latitude, longitude and altitude at the HomeWorld or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3d? GetTarget() => Trajectories.ActiveVesselTrajectory.IsVesselAttached ? Trajectories.ActiveVesselTrajectory.TargetProfile.GetLatLonAlt() : null;

        /// <summary>
        /// Returns the trajectories target as latitude, longitude and altitude at the HomeWorld or Null if no active vessel or no set target.
        /// </summary>
        public static Vector3d? GetTarget(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory == null) return null;
            return trajectory.IsVesselAttached ? trajectory.TargetProfile.GetLatLonAlt() : null;
        }

        /// <summary>
        /// Clears the trajectories target.
        /// </summary>
        public static void ClearTarget()
        {
            if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
                Trajectories.ActiveVesselTrajectory.TargetProfile.Clear();
        }

        /// <summary>
        /// Clears the trajectories target.
        /// </summary>
        public static void ClearTarget(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory != null && trajectory.IsVesselAttached)
                trajectory.TargetProfile.Clear();
        }

        /// <summary> Resets all the trajectories descent profile nodes to Prograde at 0° if true or Retrograde at 0° if false. </summary>
        /// <returns> true if all nodes are Prograde, null if no active vessel. </returns>
        public static bool? ProgradeEntry
        {
            get
            {
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
                    return !Trajectories.ActiveVesselTrajectory.DescentProfile.AtmosEntry.Retrograde && !Trajectories.ActiveVesselTrajectory.DescentProfile.HighAltitude.Retrograde &&
                           !Trajectories.ActiveVesselTrajectory.DescentProfile.LowAltitude.Retrograde && !Trajectories.ActiveVesselTrajectory.DescentProfile.FinalApproach.Retrograde;
                return null;
            }
            set
            {
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached && value.HasValue)
                {
                    Trajectories.ActiveVesselTrajectory.DescentProfile.Reset(value.Value ? 0d : Math.PI);
                    Trajectories.ActiveVesselTrajectory.DescentProfile.Save();
                }
            }
        }

        /// <summary> Sets all the trajectories descent profile nodes to Retrograde at 0° if true or Prograde at 0° if false. </summary>
        /// <returns> true if all nodes are Retrograde, null if no active vessel. </returns>
        public static bool? RetrogradeEntry
        {
            get
            {
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
                    return Trajectories.ActiveVesselTrajectory.DescentProfile.AtmosEntry.Retrograde && Trajectories.ActiveVesselTrajectory.DescentProfile.HighAltitude.Retrograde &&
                           Trajectories.ActiveVesselTrajectory.DescentProfile.LowAltitude.Retrograde && Trajectories.ActiveVesselTrajectory.DescentProfile.FinalApproach.Retrograde;
                return null;
            }
            set
            {
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached && value.HasValue)
                {
                    Trajectories.ActiveVesselTrajectory.DescentProfile.Reset(value.Value ? Math.PI : 0d);
                    Trajectories.ActiveVesselTrajectory.DescentProfile.Save();
                }
            }
        }

        /// <summary>
        /// Resets the trajectories descent profile to the passed AoA value in radians, default value is Retrograde =(PI = 180°),
        ///  also sets Retrograde if angle is greater than ±PI/2 (±90°) otherwise sets to Prograde.
        /// </summary>
        public static void ResetDescentProfile(double AoA = Math.PI)
        {
            if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
            {
                Trajectories.ActiveVesselTrajectory.DescentProfile.Reset(AoA);
                Trajectories.ActiveVesselTrajectory.DescentProfile.Save();
            }
        }


        /// <summary>
        /// Resets the trajectories descent profile to the passed AoA value in radians, default value is Retrograde =(PI = 180°),
        ///  also sets Retrograde if angle is greater than ±PI/2 (±90°) otherwise sets to Prograde.
        /// </summary>
        public static void ResetDescentProfile(Vessel vessel, double AoA = Math.PI)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            if (trajectory != null && trajectory.IsVesselAttached)
            {
                trajectory.DescentProfile.Reset(AoA);
                trajectory.DescentProfile.Save();
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
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
                {
                    return new List<double>
                    {
                        Trajectories.ActiveVesselTrajectory.DescentProfile.AtmosEntry.AngleRad,
                        Trajectories.ActiveVesselTrajectory.DescentProfile.HighAltitude.AngleRad,
                        Trajectories.ActiveVesselTrajectory.DescentProfile.LowAltitude.AngleRad,
                        Trajectories.ActiveVesselTrajectory.DescentProfile.FinalApproach.AngleRad
                    };
                }

                return null;
            }
            set
            {
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached && value.Count == 4)
                {
                    Trajectories.ActiveVesselTrajectory.DescentProfile.AtmosEntry.AngleRad = value[0];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.HighAltitude.AngleRad = value[1];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.LowAltitude.AngleRad = value[2];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.FinalApproach.AngleRad = value[3];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.Save();
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
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
                {
                    return new List<bool>
                    {
                        !Trajectories.ActiveVesselTrajectory.DescentProfile.AtmosEntry.Horizon,
                        !Trajectories.ActiveVesselTrajectory.DescentProfile.HighAltitude.Horizon,
                        !Trajectories.ActiveVesselTrajectory.DescentProfile.LowAltitude.Horizon,
                        !Trajectories.ActiveVesselTrajectory.DescentProfile.FinalApproach.Horizon
                    };
                }

                return null;
            }
            set
            {
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached && value.Count == 4)
                {
                    Trajectories.ActiveVesselTrajectory.DescentProfile.AtmosEntry.Horizon = !value[0];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.HighAltitude.Horizon = !value[1];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.LowAltitude.Horizon = !value[2];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.FinalApproach.Horizon = !value[3];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.Save();
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
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached)
                {
                    return new List<bool>
                    {
                        Trajectories.ActiveVesselTrajectory.DescentProfile.AtmosEntry.Retrograde,
                        Trajectories.ActiveVesselTrajectory.DescentProfile.HighAltitude.Retrograde,
                        Trajectories.ActiveVesselTrajectory.DescentProfile.LowAltitude.Retrograde,
                        Trajectories.ActiveVesselTrajectory.DescentProfile.FinalApproach.Retrograde
                    };
                }

                return null;
            }
            set
            {
                if (Trajectories.ActiveVesselTrajectory.IsVesselAttached && value.Count == 4)
                {
                    Trajectories.ActiveVesselTrajectory.DescentProfile.AtmosEntry.Retrograde = value[0];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.HighAltitude.Retrograde = value[1];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.LowAltitude.Retrograde = value[2];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.FinalApproach.Retrograde = value[3];
                    Trajectories.ActiveVesselTrajectory.DescentProfile.Save();
                }
            }
        }

        /// <summary>
        /// Triggers a recalculation of the trajectory.
        /// </summary>
        public static void UpdateTrajectory() => Trajectories.ActiveVesselTrajectory.ComputeTrajectory();

        /// <summary>
        /// Triggers a recalculation of the trajectory.
        /// </summary>
        public static void UpdateTrajectory(Vessel vessel)
        {
            Trajectory trajectory = Trajectories.GetTrajectoryForVessel(vessel);
            trajectory?.ComputeTrajectory();
        }
    }
}
