/*
  Copyright© (c) 2017-2021 S.Gray, (aka PiezPiedPy).

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
using UnityEngine;

namespace Trajectories
{
    /// <summary> Contains data such as ground altitude data of all celestial bodies. </summary>
    internal static class CelestialBodyMaps
    {
        ///<summary> Initializes the celestial body maps </summary>
        internal static void Start()
        {
            Util.DebugLog(GroundAltitudeMaps != null ? "Resetting" : "Constructing");
            // create or update maps as needed

            // parse celestial bodies, one map per body

        }

        ///<summary> Clean up any resources being used </summary>
        internal static void Destroy() => Util.DebugLog("");

        internal static bool Update()
        {
            //Profiler.Start("CelestialBodyMaps.Update");
            return true;
        }

        private static void UpdateAltitudeMaps()
        {
            Util.DebugLog("");
        }

        /// <summary> Gets the ground altitude of the body using the world space relative position </summary>
        /// <returns> the altitude above sea level (can be negative for bodies without ocean) </returns>
        internal static double GetPQSGroundAltitude(CelestialBody body, Vector3d relative_position)
        {
            if (body.pqsController == null)
                return 0d;

            Vector2d lat_long = body.GetLatitudeAndLongitude(relative_position + body.position);
            Vector3d radial = QuaternionD.AngleAxis(lat_long.y, Vector3d.down) * QuaternionD.AngleAxis(lat_long.x, Vector3d.forward) * Vector3d.right;
            double elevation = body.pqsController.GetSurfaceHeight(radial) - body.Radius;

            if (body.ocean)
                elevation = Math.Max(elevation, 0d);

            return elevation;
        }

        /// <summary> Gets the ground altitude of the GameDataCache body using the world space relative position </summary>
        /// <returns> the altitude above sea level (can be negative for bodies without an ocean) </returns>
        internal static double GetPQSGroundAltitude(Vector3d relative_position)
        {
            Vector2d lat_long = GameDataCache.Body.GetLatitudeAndLongitude(relative_position + GameDataCache.BodyWorldPos);
            Vector3d radial = QuaternionD.AngleAxis(lat_long.y, Vector3d.down) * QuaternionD.AngleAxis(lat_long.x, Vector3d.forward) * Vector3d.right;
            double elevation = 0d;   // Temporary until I find a workaround //GameDataCache.Body.pqsController.GetSurfaceHeight(radial) - GameDataCache.BodyRadius;

            if (GameDataCache.BodyHasOcean)
                elevation = Math.Max(elevation, 0d);

            return elevation;
        }

    }
}
