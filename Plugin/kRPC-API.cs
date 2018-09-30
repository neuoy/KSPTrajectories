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

// API expansion by Somfic.

using System.Linq;
using UnityEngine;

using KRPC.Service.Attributes;
using KRPC.Utils;

namespace Trajectories
{
    /// <summary>
    /// API for Trajectories. Note: this API only returns correct values for the "active vessel".
    /// </summary>
    [KRPCService(GameScene = KRPC.Service.GameScene.Flight, Name = "Trajectories")]
    public static class kRPC_API
    {
        /// <summary>
        /// Returns the version number of trajectories in a string formated as Major.Minor.Patch i.e. 2.1.0
        /// </summary>
        [KRPCProperty]
        public static string GetVersion
        {
            get
            {
                return API.GetVersion;
            }
        }

        /// <summary>
        /// Returns the major version number of trajectories
        /// </summary>
        [KRPCProperty]
        public static int GetVersionMajor
        {
            get
            {
                return API.GetVersionMajor;
            }
        }


        /// <summary>
        /// Returns the minor version number of trajectories
        /// </summary>
        [KRPCProperty]
        public static int GetVersionMinor
        {
            get
            {
                return API.GetVersionMinor;
            }
        }

        /// <summary>
        /// Returns the patch version number of trajectories
        /// </summary>
        [KRPCProperty]
        public static int GetVersionPatch
        {
            get
            {
                return API.GetVersionPatch;
            }
        }

        /// <summary>
        /// Modifies the AlwaysUpdate value in the settings page.
        /// </summary>
        [KRPCProperty]
        public static bool AlwaysUpdate
        {
            get
            {
                return API.AlwaysUpdate;
            }
            set
            {
               API.AlwaysUpdate = value;
            }
        }

        /// <summary>
        /// Returns trajectory patch EndTime or (-1) if no active vessel or calculated trajectory.
        /// See GetTimeTillImpact for remaining time until impact.
        /// </summary>
        [KRPCProcedure]
        public static double GetEndTime()
        {
            double? endTime = API.GetEndTime();
            if (endTime.HasValue) { return endTime.Value; }
            else { return -1; }
        }


        /// <summary>
        /// Returns the remaining time until Impact in seconds or (-1) if no active vessel or calculated trajectory.
        /// </summary>
        [KRPCProcedure]
        public static double GetTimeTillImpact()
        {
            double? timeTillImpact = API.GetTimeTillImpact();
            if (timeTillImpact.HasValue) { return timeTillImpact.Value; }
            else { return -1; }
        }

        /// <summary>
        /// Returns the calculated impact position of the trajectory or (-1, -1, -1) if no active vessel or calculated trajectory.
        /// </summary>
        [KRPCProcedure]
        public static Tuple<double, double, double> GetImpactPosition()
        {
            Vector3? impactPosition = API.GetImpactPosition();
            if (impactPosition.HasValue) { return new Tuple<double, double, double>(impactPosition.Value.x, impactPosition.Value.y, impactPosition.Value.z); }
            else { return new Tuple<double, double, double>(-1, -1, -1); }
        }

        /// <summary>
        /// Returns the calculated impact velocity of the trajectory or (-1, -1, -1) if no active vessel or calculated trajectory.
        /// </summary>
        [KRPCProcedure]
        public static Tuple<double, double, double> GetImpactVelocity()
        {
            Vector3? impactVelocity = API.GetImpactVelocity();
            if (impactVelocity.HasValue) { return new Tuple<double, double, double>(impactVelocity.Value.x, impactVelocity.Value.y, impactVelocity.Value.z); }
            else { return new Tuple<double, double, double>(-1, -1, -1); }
        }

        /// <summary>
        /// Returns the planned direction or (-1, -1, -1) if no active vessel or set target.
        /// </summary>
        [KRPCProcedure]
        static Tuple<double, double, double> PlannedDirection()
        {
            Vector3? plannedDirection = API.PlannedDirection();
            if (plannedDirection.HasValue) { return new Tuple<double, double, double>(plannedDirection.Value.x, plannedDirection.Value.y, plannedDirection.Value.z); }
            else { return new Tuple<double, double, double>(-1, -1, -1); }
        }

        /// <summary>
        /// Returns the corrected direction or (-1, -1, -1) if no active Vessel.
        /// </summary>
        [KRPCProcedure]
        public static Tuple<double, double, double> CorrectedDirection()
        {
            Vector3? correctedDirection = API.CorrectedDirection();
            if (correctedDirection.HasValue) { return new Tuple<double, double, double>(correctedDirection.Value.x, correctedDirection.Value.y, correctedDirection.Value.z); }
            else { return new Tuple<double, double, double>(-1, -1, -1); }
        }

        /// <summary>
        /// Returns true if a target has been set, false if not.
        /// </summary>
        [KRPCProcedure]
        public static bool HasTarget()
        {
            return API.HasTarget();
        }

        /// <summary>
        /// Set the trajectories target to a latitude, longitude and altitude at the HomeWorld.
        /// </summary>
        [KRPCProcedure]
        public static void SetTarget(double lat, double lon, double alt = 2.0)
        {
            API.SetTarget(lat, lon, alt);
        }

        /// <summary>
        /// Set the trajectories descent profile to Prograde.
        /// </summary>
        [KRPCProperty]
        public static bool ProgradeEntry
        {
            get
            {
                bool? progradeEntry = API.ProgradeEntry;
                if (progradeEntry.HasValue) { return progradeEntry.Value; }
                else { return false; }
            }
            set
            {
                if ((FlightGlobals.ActiveVessel != null) && !DescentProfile.fetch.ProgradeEntry)
                {
                    DescentProfile.fetch.ProgradeEntry = true;
                    DescentProfile.fetch.Reset(0d);
                    DescentProfile.fetch.Save();
                }
            }
        }

        /// <summary>
        /// Set the trajectories descent profile to Prograde.
        /// </summary>
        [KRPCProperty]
        public static bool? RetrogradeEntry
        {
            get
            {
                bool? retogradeEntry = API.RetrogradeEntry;
                if (retogradeEntry.HasValue) { return retogradeEntry.Value; }
                else { return false; }
            }
            set
            {
                if ((FlightGlobals.ActiveVessel != null) && !DescentProfile.fetch.RetrogradeEntry)
                {
                    DescentProfile.fetch.RetrogradeEntry = true;
                    DescentProfile.fetch.Reset();
                    DescentProfile.fetch.Save();
                }
            }
        }
    }
}
