/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
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

using System;
using System.Linq;
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
            get
            {
                return Settings.fetch.AlwaysUpdate;
            }
            set
            {
                Settings.fetch.AlwaysUpdate = value;
            }
        }

        /// <summary>
        /// Returns trajectory patch EndTime or Null if no active vessel or calculated trajectory.
        /// See GetTimeTillImpact for remaining time until impact.
        /// </summary>
        public static double? GetEndTime()
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (Trajectory.Patch patch in Trajectory.fetch.Patches)
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
            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (Trajectory.Patch patch in Trajectory.fetch.Patches)
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
            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (Trajectory.Patch patch in Trajectory.fetch.Patches)
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
            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (Trajectory.Patch patch in Trajectory.fetch.Patches)
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
            if (FlightGlobals.ActiveVessel != null)
            {
                foreach (Trajectory.Patch patch in Trajectory.fetch.Patches)
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
        /// Returns the planned direction or Null if no active vessel or set target.
        /// </summary>
        public static Vector3? PlannedDirection()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                return NavBallOverlay.GetPlannedDirection();
            return null;
        }

        /// <summary>
        /// Returns the planned orientation as Quaternion for attitude control and vector operations or Null if no active vessel or set target.
        /// </summary>
        public static Quaternion? PlannedOrientation()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
            {

                Vector3d pos = FlightGlobals.ActiveVessel.GetWorldPos3D() - Trajectory.Target.Body.position;
                Vector3d vel = FlightGlobals.ActiveVessel.obt_velocity - Trajectory.Target.Body.getRFrmVel(Trajectory.Target.Body.position + pos);
                return DescentProfile.fetch.GetUnityOrientation(Trajectory.Target.Body, pos, vel);
            }
            else
                return null;
        }

        /// <summary>
        /// Returns the corrected direction or Null if no active vessel or set target.
        /// </summary>
        public static Vector3? CorrectedDirection()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                return NavBallOverlay.GetCorrectedDirection();
            return null;
        }

        /// <summary>
        /// Returns true if a target has been set, false if not, or Null if no active vessel.
        /// </summary>
        public static bool HasTarget()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                return true;
            return false;
        }

        /// <summary>
        /// Set the trajectories target to a latitude, longitude and altitude at the HomeWorld.
        /// </summary>
        public static void SetTarget(double lat, double lon, double? alt = null)
        {
            if (FlightGlobals.ActiveVessel != null)
            {
                CelestialBody body = FlightGlobals.GetHomeBody();
                if (body != null)
                    Trajectory.Target.SetFromLatLonAlt(body, lat, lon, alt);
            }
        }

        /// <summary>
        /// Set the trajectories descent profile to Prograde.
        /// </summary>
        public static bool? ProgradeEntry
        {
            get
            {
                if (FlightGlobals.ActiveVessel != null)
                    return DescentProfile.fetch.ProgradeEntry;
                return null;
            }
            set
            {
                if ((FlightGlobals.ActiveVessel != null) && !DescentProfile.fetch.ProgradeEntry)
                {
                    DescentProfile.fetch.Reset();
                    DescentProfile.fetch.Save();
                }
            }
        }

        /// <summary>
        /// Set the trajectories descent profile to Retrograde.
        /// </summary>
        public static bool? RetrogradeEntry
        {
            get
            {
                if (FlightGlobals.ActiveVessel != null)
                    return DescentProfile.fetch.RetrogradeEntry;
                return null;
            }
            set
            {
                if ((FlightGlobals.ActiveVessel != null) && !DescentProfile.fetch.RetrogradeEntry)
                {
                    DescentProfile.fetch.Reset(retrograde: true);
                    DescentProfile.fetch.Save();
                }
            }
        }

        /// <summary>
        /// provide read/write access to current effective angle of attack
        /// if set during transition then both angles are changed
        /// </summary>
        public static double? AoA
        {
            get
            {
                if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                {
                    Vector3d pos = FlightGlobals.ActiveVessel.GetWorldPos3D() - Trajectory.Target.Body.position;
                    Vector3d vel = FlightGlobals.ActiveVessel.obt_velocity - Trajectory.Target.Body.getRFrmVel(Trajectory.Target.Body.position + pos);
                    return Mathf.Rad2Deg * DescentProfile.fetch.GetAngleOfAttack(Trajectory.Target.Body, pos, vel);                    
                }

                return null;
            }
            set
            {
                if (value.HasValue && FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                {
                    double newvalue = Mathf.Deg2Rad * (double)value;
                    Vector3d pos = FlightGlobals.ActiveVessel.GetWorldPos3D() - Trajectory.Target.Body.position;
                    Vector3d vel = FlightGlobals.ActiveVessel.obt_velocity - Trajectory.Target.Body.getRFrmVel(Trajectory.Target.Body.position + pos);
                    double altitude = pos.magnitude - Trajectory.Target.Body.Radius;
                    double altitudeRatio = Trajectory.Target.Body.atmosphere ? altitude / Trajectory.Target.Body.atmosphereDepth : 0;

                    foreach (var conf in DescentProfile.fetch.NodeList)
                    {
                        if (conf.Key <= altitudeRatio)
                        {
                            double factor = (altitudeRatio - conf.Key) * conf.Value.transition;
                            double delta = newvalue - Util.dLerp(conf.Value.lower.GetAngleOfAttack(pos, vel), conf.Value.higher.GetAngleOfAttack(pos, vel), factor );
                            Debug.Log("Traj API: newangle for "+ conf.Value.higher.Name +" with delta " + (Mathf.Rad2Deg * delta).ToString("F1")+" gives " + (Mathf.Rad2Deg * (conf.Value.higher.Angle + delta)).ToString("F1"));
                            conf.Value.higher.Angle += delta;
                            conf.Value.higher.RefreshSliderPos();
                            if (factor < 1)
                            {
                                conf.Value.lower.Angle += delta;
                                conf.Value.lower.RefreshSliderPos();
                            }
                            break;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// provide read/write access to next configured angle
        /// ignores transition phases
        /// </summary>
        public static double? nextAoA
        {
            get
            {
                if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                {
                    Vector3d pos = FlightGlobals.ActiveVessel.GetWorldPos3D() - Trajectory.Target.Body.position;
                    Vector3d vel = FlightGlobals.ActiveVessel.obt_velocity - Trajectory.Target.Body.getRFrmVel(Trajectory.Target.Body.position + pos);
                    double altitude = pos.magnitude - Trajectory.Target.Body.Radius;
                    double altitudeRatio = Trajectory.Target.Body.atmosphere ? altitude / Trajectory.Target.Body.atmosphereDepth : 0;

                    foreach (var conf in DescentProfile.fetch.NodeList)
                    {
                        if (conf.Key <= altitudeRatio && (altitudeRatio - conf.Key) * conf.Value.transition > 1)
                        {
                            return Mathf.Rad2Deg * conf.Value.higher.GetAngleOfAttack(pos, vel);
                        }
                    }
                    return Mathf.Rad2Deg * DescentProfile.fetch.finalApproach.GetAngleOfAttack(pos, vel);
                }

                return null;
            }
            set
            {
                if (value.HasValue && FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
                {
                    double newvalue = Mathf.Deg2Rad * (double)value;
                    Vector3d pos = FlightGlobals.ActiveVessel.GetWorldPos3D() - Trajectory.Target.Body.position;
                    Vector3d vel = FlightGlobals.ActiveVessel.obt_velocity - Trajectory.Target.Body.getRFrmVel(Trajectory.Target.Body.position + pos);
                    double altitude = pos.magnitude - Trajectory.Target.Body.Radius;
                    double altitudeRatio = Trajectory.Target.Body.atmosphere ? altitude / Trajectory.Target.Body.atmosphereDepth : 0;

                    foreach (var conf in DescentProfile.fetch.NodeList)
                    {
                        if (conf.Key <= altitudeRatio && (altitudeRatio - conf.Key) * conf.Value.transition > 1)
                        {
                            double delta = newvalue - conf.Value.higher.GetAngleOfAttack(pos, vel);
                            conf.Value.higher.Angle += delta;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// are we below entry phase ?
        /// </summary>
        public static bool isBelowEntry()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
            {
                Vector3d pos = FlightGlobals.ActiveVessel.GetWorldPos3D() - Trajectory.Target.Body.position;
                double altitude = pos.magnitude - Trajectory.Target.Body.Radius;
                double altitudeRatio = Trajectory.Target.Body.atmosphere ? altitude / Trajectory.Target.Body.atmosphereDepth : 0;

                return DescentProfile.fetch.NodeList.Any(c => c.Value.higher == DescentProfile.fetch.entry && altitudeRatio < c.Key );

                
            }
            else
                return false; // neither true or false make sense, but failing condition is good idea
        }

        public static bool isTransitionAlt()
        {
            if (FlightGlobals.ActiveVessel != null && Trajectory.Target.Body != null)
            {
                Vector3d pos = FlightGlobals.ActiveVessel.GetWorldPos3D() - Trajectory.Target.Body.position;
                double altitude = pos.magnitude - Trajectory.Target.Body.Radius;
                double altitudeRatio = Trajectory.Target.Body.atmosphere ? altitude / Trajectory.Target.Body.atmosphereDepth : 0;

                return DescentProfile.fetch.NodeList.Any(c => altitudeRatio >= c.Key && (altitudeRatio - c.Key) * c.Value.transition <= 1d);
            }
            else
                return false;
        }


        /// <summary>
        /// Clear current calculation, can be called if known outside changes make current values worthless
        /// </summary>
        public static void invalidateCalculation()
        {
            Trajectory.fetch.InvalidateCalculation();
        }

        /// <summary>
        /// Triggers a recalculation of the trajectory.
        /// </summary>
        private static void UpdateTrajectory()
        {
            if (FlightGlobals.ActiveVessel != null)
                Trajectory.fetch.ComputeTrajectory(FlightGlobals.ActiveVessel, DescentProfile.fetch);
        }
    }
}
