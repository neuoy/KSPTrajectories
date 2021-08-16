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
using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    /// <summary> Contains data such as ground altitude data of all celestial bodies. </summary>
    internal static class CelestialBodyMaps
    {
        private const int MAP_WIDTH = 360 * 2;       // 720 x 360 map size , 0.5deg/index
        private const int MAP_HEIGHT = (int)(MAP_WIDTH * 0.5f);
        private const double MAP_WIDTH_SCALAR = 360d / MAP_WIDTH;
        private const double MAP_HEIGHT_SCALAR = 180d / MAP_HEIGHT;
        private const double MAP_WIDTH_DIVISOR = 1d / MAP_WIDTH_SCALAR;
        private const double MAP_HEIGHT_DIVISOR = 1d / MAP_HEIGHT_SCALAR;

        // Map of the ground altitudes for a celestial body
        internal class GroundAltitudeMap
        {
            internal int BodyIndex { get; private set; }
            internal double[] HeightMap { get; private set; }

            // constructor
            internal GroundAltitudeMap(CelestialBody body)
            {
                if (body == null)
                    return;

                BodyIndex = FlightGlobals.Bodies.IndexOf(body);

                // check if body has surface data
                if (body.pqsController == null || !body.hasSolidSurface)
                    return;

                CurrentBodyName = body.name;
                Util.DebugLog("Creating a ground altitude map for {0} with index {1}", body.name, BodyIndex);

                calculation_time = Util.Clocks;

                int index = 0;
                HeightMap = new double[MAP_WIDTH * MAP_HEIGHT];

                for (int y = 0; y < MAP_HEIGHT; y++)
                {
                    for (int x = 0; x < MAP_WIDTH; x++)
                    {
                        // sample and store surface height
                        Vector3d radial = QuaternionD.AngleAxis((x * MAP_WIDTH_SCALAR) - 180d, Vector3d.down) *                         // longitude
                                            QuaternionD.AngleAxis((y * MAP_HEIGHT_SCALAR) - 90d, Vector3d.forward) * Vector3d.right;    // latitude
                        HeightMap[index] = body.pqsController.GetSurfaceHeight(radial) - body.pqsController.radius;
                        index++;
                    }
                }

                calculation_time = Util.ElapsedSeconds(calculation_time);
                Util.DebugLog("Ground altitude map for {0} with index {1} completed in {2:0.00}s", body.name, BodyIndex, calculation_time);
            }

            internal void Clear()
            {
                BodyIndex = 0;
                HeightMap = null;
            }
        }

        /// <summary> Adds a collection of KSP CelestialBody's ground altitudes to a collection of GroundAltitudeMap's </summary>
        internal static void Add(this ICollection<GroundAltitudeMap> collection, IEnumerable<CelestialBody> bodies)
        {
            foreach (CelestialBody body in bodies)
            {
                collection.Add(new GroundAltitudeMap(body));
            }
        }

        /// <summary> Clears a collection of GroundAltitudeMap's </summary>
        internal static void Release(this ICollection<GroundAltitudeMap> collection)
        {
            foreach (GroundAltitudeMap altitude_map in collection)
            {
                altitude_map.Clear();
            }

            collection.Clear();
        }

        internal static List<GroundAltitudeMap> GroundAltitudeMaps { get; private set; }
        internal static string CurrentBodyName { get; private set; }
        internal static bool NeedsUpdate { get; private set; }
        internal static bool RunUpdate { get; set; }

        private static double calculation_time;

        ///<summary> Initializes the celestial body maps </summary>
        internal static void Start()
        {
            Util.DebugLog(GroundAltitudeMaps != null ? "Resetting" : "Constructing");

            CurrentBodyName = "";
            NeedsUpdate = false;
            RunUpdate = false;

            // check for changes in the celestial bodies
            if (GroundAltitudeMaps?.Count != FlightGlobals.Bodies?.Count)
            {
                Util.Log("Celestial body cache needs updating due to {0}", GroundAltitudeMaps == null ? "no maps in cache" : "count difference");
                NeedsUpdate = true;
            }
        }

        ///<summary> Clean up any resources being used </summary>
        internal static void Destroy()
        {
            Util.DebugLog("");

            GroundAltitudeMaps?.Release();
        }

        internal static void Update()
        {
            //Profiler.Start("CelestialBodyMaps.Update");
            if (RunUpdate)
                UpdateAltitudeMaps();

            RunUpdate = false;
        }

        private static void UpdateAltitudeMaps()
        {
            Util.DebugLog("");

            GroundAltitudeMaps?.Release();
            //GroundAltitudeMaps = new List<GroundAltitudeMap>() { FlightGlobals.Bodies };
            GroundAltitudeMaps = new List<GroundAltitudeMap>();
            GroundAltitudeMaps?.Add(new GroundAltitudeMap(FlightGlobals.Bodies[1]));

            NeedsUpdate = false;
        }

        /// <summary> Gets the ground altitude of the body using the world space relative position </summary>
        /// <returns> the altitude above sea level (can be negative for bodies without ocean) </returns>
        internal static double? GetPQSGroundAltitude(CelestialBody body, Vector3d relative_position)
        {
            if (body == null || body.pqsController == null || !body.hasSolidSurface)
                return null;

            Vector2d lat_long = body.GetLatitudeAndLongitude(relative_position + body.position);
            Vector3d radial = QuaternionD.AngleAxis(lat_long.y, Vector3d.down) * QuaternionD.AngleAxis(lat_long.x, Vector3d.forward) * Vector3d.right;
            double elevation = body.pqsController.GetSurfaceHeight(radial) - body.pqsController.radius;

            if (body.ocean)
                elevation = Math.Max(elevation, 0d);

            return elevation;
        }

        /// <summary> Gets the ground altitude of the GameDataCache body using the world space relative position </summary>
        /// <returns> the altitude above sea level (can be negative for bodies without an ocean) </returns>
        internal static double? GroundAltitude(Vector3d relative_position)
        {
            if (!GameDataCache.BodyHasSolidSurface || GameDataCache.BodyIndex >= GroundAltitudeMaps?.Count ||
                (GroundAltitudeMaps?[0]?.BodyIndex != GameDataCache.BodyIndex))
                return null;

            Vector3d world_position = (relative_position + GameDataCache.BodyWorldPos).normalized;
            Vector3d local_position = new(
                Vector3d.Dot(world_position.xzy, GameDataCache.BodyFrameX),
                Vector3d.Dot(world_position.xzy, GameDataCache.BodyFrameY),
                Vector3d.Dot(world_position.xzy, GameDataCache.BodyFrameZ));

            double latitude = Math.Asin(local_position.z) * Mathf.Rad2Deg;
            double longitude = Math.Atan2(local_position.y, local_position.x) * Mathf.Rad2Deg;
            Vector3d radial = QuaternionD.AngleAxis(longitude, Vector3d.down) * QuaternionD.AngleAxis(latitude, Vector3d.forward) * Vector3d.right;
            //double elevation = GameDataCache.Body.pqsController.GetSurfaceHeight(radial) - GameDataCache.BodyPqsRadius.Value;
            double elevation = 0d;                // Temporary until I find a workaround

            if (GameDataCache.BodyHasOcean)
                elevation = Math.Max(elevation, 0d);

            return elevation;
        }

    }
}
