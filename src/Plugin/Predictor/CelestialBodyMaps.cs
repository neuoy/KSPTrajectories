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
        private class GroundAltitudeMap
        {
            internal int BodyIndex { get; private set; }
            internal double[] HeightMap { get; private set; }

            private string file_name;

            // constructor
            internal GroundAltitudeMap(CelestialBody body)
            {
                if (FlightGlobals.Bodies == null || body == null)
                    return;

                BodyIndex = FlightGlobals.Bodies.IndexOf(body);

                // check if body has surface data
                if (body.pqsController == null || !body.hasSolidSurface)
                {
                    Util.Log("Skipping ground altitude map for {0} - No surface found.", body.name);
                    return;
                }

                HeightMap = new double[MAP_WIDTH * MAP_HEIGHT];
                file_name = body.name + " Height Map" + ".bin";
            }

            /// <summary> Samples the PQS surface height in increments </summary>
            internal IEnumerable<bool> SampleIncrement()
            {
                CelestialBody body = FlightGlobals.Bodies?[BodyIndex];

                if (HeightMap == null || body == null)
                    yield return true;

                Util.DebugLog("Creating a ground altitude map for {0} with index {1}", body.name, BodyIndex);

                calculation_time = Util.Clocks;

                current_sampler_index = 0;

                for (int y = 0; y < MAP_HEIGHT; y++)
                {
                    for (int x = 0; x < MAP_WIDTH; x++)
                    {
                        // sample and store surface height
                        Vector3d radial = QuaternionD.AngleAxis((x * MAP_WIDTH_SCALAR) - 180d, Vector3d.down) *                         // longitude
                                            QuaternionD.AngleAxis((y * MAP_HEIGHT_SCALAR) - 90d, Vector3d.forward) * Vector3d.right;    // latitude
                        HeightMap[current_sampler_index] = body.pqsController.GetSurfaceHeight(radial) - body.pqsController.radius;
                        current_sampler_index++;
                    }
                    // return if calculation time is too long so we don't lag the game
                    if (Util.ElapsedMilliseconds(calculation_time) > 25d)     // 40 fps
                        yield return false;
                }

                calculation_time = Util.ElapsedSeconds(calculation_time);
                Util.Log("Ground altitude map for {0} completed in {1:0.0}s", body.name, calculation_time);
            }

            /// <summary> Clears all data </summary>
            internal void Clear()
            {
                BodyIndex = 0;
                HeightMap = null;
            }

            /// <summary> Saves the surface height map to a binary file </summary>
            internal void Save()
            {
                CelestialBody body = FlightGlobals.Bodies?[BodyIndex];

                if (maps_path?.Length <= 0 || file_name?.Length <= 0 || HeightMap == null || body == null
                    || body.pqsController == null || !body.hasSolidSurface)
                    return;

                try
                {
                    System.IO.File.WriteAllBytes(maps_path + file_name, HeightMap.ToByteArray());
                }
                catch (Exception e)
                {
                    Util.LogWarning("Error saving Celestial body map '{0}' - {1}", file_name, e.Message);
                    return;
                }

                Util.Log("Saved Celestial body map '{0}'", file_name);
            }

            /// <summary> Loads the surface height map from a binary file </summary>
            internal void Load()
            {
                CelestialBody body = FlightGlobals.Bodies?[BodyIndex];

                if (maps_path?.Length <= 0 || file_name?.Length <= 0 || HeightMap == null || body == null
                    || body.pqsController == null || !body.hasSolidSurface)
                    return;

                // todo: check file exists
                calculation_time = Util.Clocks;

                try
                {
                    HeightMap = System.IO.File.ReadAllBytes(maps_path + file_name).ToDoubleArray();
                }
                catch (Exception e)
                {
                    Util.LogWarning("Error loading Celestial body map '{0}' - {1}", file_name, e.Message);
                    return;
                }

                calculation_time = Util.ElapsedMilliseconds(calculation_time);
                Util.DebugLog("Loaded Celestial body map '{0}' in {1:0.000}ms", file_name, calculation_time);
            }
        }

        /// <summary> Adds a collection of KSP CelestialBody's to a collection of GroundAltitudeMap's.
        ///  Also attempts to load saved bin files unless the load parameter is set to false </summary>
        private static void Add(this ICollection<GroundAltitudeMap> collection, IEnumerable<CelestialBody> bodies, bool load = true)
        {
            foreach (CelestialBody body in bodies)
            {
                GroundAltitudeMap altitude_map = new GroundAltitudeMap(body);
                if (load)
                    altitude_map.Load();
                collection.Add(altitude_map);
            }
        }

        /// <summary> Clears a collection of GroundAltitudeMap's </summary>
        private static void Release(this ICollection<GroundAltitudeMap> collection)
        {
            foreach (GroundAltitudeMap altitude_map in collection)
            {
                altitude_map.Clear();
            }

            collection.Clear();
        }

        /// <returns> The name of the celestial body currently being mapped </returns>
        internal static string CurrentBodyName => FlightGlobals.Bodies?[CurrentBodyIndex]?.name ?? "";
        /// <returns> The index of the celestial body currently being mapped </returns>
        internal static int CurrentBodyIndex => current_body_index >= 0 && (current_body_index < (FlightGlobals.Bodies?.Count ?? 0d)) ? current_body_index : 0;
        /// <returns> true if the maps are outdated </returns>
        internal static bool NeedsUpdate { get; private set; }
        /// <summary> If set to true, triggers a map update </summary>
        /// <returns> true if the maps are updating otherwise false </returns>
        internal static bool RunUpdate { get => run_update; set { if (value) run_update = true; } }
        /// <returns> The total percentage of all celestial bodies mapped </returns>
        internal static double PercentComplete => ((double)current_body_index / FlightGlobals.Bodies?.Count ?? 0d) * 100d;

        private static bool first_check;
        private static string maps_path;
        private static List<string> height_files;
        private static List<GroundAltitudeMap> ground_altitude_maps;
        private static IEnumerator<bool> body_incrementer;
        private static IEnumerator<bool> sample_incrementer;
        private static bool run_update;
        private static int current_body_index;
        private static int current_sampler_index;
        private static double update_time;
        private static double calculation_time;

        ///<summary> Initializes the celestial body maps </summary>
        internal static void Start()
        {
            Util.DebugLog(ground_altitude_maps != null ? "Resetting" : "Constructing");

            current_body_index = 0;
            first_check = false;
            NeedsUpdate = false;
            run_update = false;
        }

        ///<summary> Clean up any resources being used </summary>
        internal static void Destroy()
        {
            Util.DebugLog("");

            ground_altitude_maps?.Release();
        }

        internal static void Update()
        {
            //Profiler.Start("CelestialBodyMaps.Update");
            if (run_update)
                UpdateAltitudeMaps();
        }

        ///<summary> Sets the celestial body maps file path </summary>
        private static void SetPath()
        {
            maps_path = KSPUtil.ApplicationRootPath + "GameData/Trajectories/BodyMaps/";
            Util.DebugLog("Path set to {0}", maps_path);

            try
            {
                System.IO.Directory.CreateDirectory(maps_path);
            }
            catch (Exception e)
            {
                Util.LogWarning("Error creating Celestial body maps directory {0} - {1}", maps_path, e.Message);
                return;
            }
        }

        ///<summary> Updates the list of celestial body map files </summary>
        private static void GetFiles()
        {
            try
            {
                height_files = new(System.IO.Directory.EnumerateFiles(maps_path));
            }
            catch (Exception e)
            {
                Util.LogWarning("Error enumerating Celestial body maps directory {0} - {1}", maps_path, e.Message);
                return;
            }
        }

        ///<summary> Loads celestial body maps and checks for changes </summary>
        internal static void CheckMaps()
        {
            if (first_check)
                return;

            SetPath();
            GetFiles();

            int num_bodies = 0;
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body?.pqsController != null && body.hasSolidSurface)
                {
                    num_bodies++;
                }
            }

            if (height_files?.Count != num_bodies)
            {
                height_files = height_files?.Count <= 0 ? height_files = null : height_files;
                Util.LogWarning("Celestial body cache needs updating due to {0}", height_files == null ? "no maps in cache" : "count difference");
                NeedsUpdate = true;
            }
            else
            {
                // load saved maps
                Util.Log("Loading Celestial body maps");
                LoadAltitudeMaps();
            }

            first_check = true;
        }

        ///<summary> Loads the celestial body altitude maps from previously saved bin files </summary>
        internal static void LoadAltitudeMaps()
        {
            ground_altitude_maps?.Release();
            ground_altitude_maps = new();
            ground_altitude_maps.Add(FlightGlobals.Bodies);

            if (ground_altitude_maps == null)
            {
                Util.LogWarning("There was a problem loading the ground altitude maps");
                return;
            }
        }

        ///<summary> Updates the celestial body altitude maps by sampling PQS data </summary>
        private static void UpdateAltitudeMaps()
        {
            if (FlightGlobals.Bodies == null)
                return;

            // is this a new update or an ongoing update
            if (body_incrementer == null)
            {
                Util.Log("Sampling celestial bodies, this may take a few minuets");
                update_time = Util.Clocks;
                current_body_index = 0;
                ground_altitude_maps?.Release();
                ground_altitude_maps = new();

                if (ground_altitude_maps == null)
                {
                    run_update = false;
                    Util.LogWarning("There was a problem creating the ground altitude maps");
                    return;
                }

                // create enumerator for stepping through the celestial bodies
                body_incrementer = BodyIncrement().GetEnumerator();
            }

            // execute a celestial body sampler
            bool finished = !body_incrementer.MoveNext();
            bool error = body_incrementer.Current;

            if (error && !finished)
            {
                Util.LogWarning("There was a problem sampling the celestial bodies");
            }

            // finished when no more celestial bodies are left to be done
            if (finished)    // || error)
            {
                // clear sampling enumerator
                sample_incrementer?.Dispose();
                sample_incrementer = null;
                current_sampler_index = 0;

                // clear celestial bodies enumerator
                body_incrementer.Dispose();
                body_incrementer = null;
                current_body_index = 0;

                run_update = false;
                NeedsUpdate = false;

                // how long did the sampling of all the celestial bodies take?
                Util.ElapsedMinuetsSeconds(update_time, out int minuets, out double seconds);
                Util.Log("Finished Sampling of celestial bodies, completed in {0}:{1:0.0}s", minuets, seconds);
            }
        }

        private static IEnumerable<bool> BodyIncrement()
        {
            current_body_index = 0;

            while (current_body_index < FlightGlobals.Bodies.Count)
            {
                CelestialBody body = FlightGlobals.Bodies[current_body_index];

                if (body == null)
                    yield return true;        // report problem

                // is this a new sample or an ongoing sample
                if (sample_incrementer == null)
                {
                    Util.Log("Sampling {0}...", CurrentBodyName);
                    current_sampler_index = 0;
                    ground_altitude_maps?.Add(new GroundAltitudeMap(body));

                    if (ground_altitude_maps?[current_body_index] == null)
                        yield return true;        // report problem

                    // create enumerator for sampling increments
                    sample_incrementer = ground_altitude_maps?[current_body_index].SampleIncrement().GetEnumerator();
                }

                // execute a sampling increment
                bool finished = !sample_incrementer.MoveNext();

                // finished when no more sampling increments are left to be done
                if (finished || sample_incrementer.Current)
                {
                    // clear sampling enumerator
                    sample_incrementer.Dispose();
                    sample_incrementer = null;
                    current_sampler_index = 0;

                    // save sampled data
                    ground_altitude_maps?[current_body_index].Save();
                    current_body_index++;
                }

                yield return false;   // return so we don't lag the game
            }
        }

        /// <summary> Gets the ground altitude of the body using the world space relative position </summary>
        /// <returns> The altitude above sea level, can be negative for bodies without an ocean or null for bodies without a surface </returns>
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
        /// <returns> The altitude above sea level, can be negative for bodies without an ocean or null for bodies without a surface </returns>
        internal static double? GroundAltitude(Vector3d relative_position)
        {
            if (GameDataCache.BodyIndex >= ground_altitude_maps?.Count ||
                (ground_altitude_maps?[GameDataCache.BodyIndex]?.BodyIndex != GameDataCache.BodyIndex))
                return null;      // todo: move checks to calling methods or elsewhere

            Vector3d local_position = (relative_position.normalized).xzy;
            local_position = new(
                Vector3d.Dot(local_position, GameDataCache.BodyFrameX),
                Vector3d.Dot(local_position, GameDataCache.BodyFrameY),
                Vector3d.Dot(local_position, GameDataCache.BodyFrameZ));

            int index = ((int)(((Math.Atan2(local_position.y, local_position.x) * Mathf.Rad2Deg) + 180d) * MAP_WIDTH_DIVISOR) * MAP_WIDTH) +
                (int)(((Math.Asin(local_position.z) * Mathf.Rad2Deg) + 90d) * MAP_HEIGHT_DIVISOR);

            if (index < 0 || index > ground_altitude_maps[GameDataCache.BodyIndex].HeightMap?.Length)
            {
                Util.LogWarning("Ground altitude map index {0} out of range [0-{1}]", index, ground_altitude_maps[GameDataCache.BodyIndex].HeightMap?.Length);
                return null;
            }

            double? elevation = ground_altitude_maps[GameDataCache.BodyIndex].HeightMap?[index];

            if (GameDataCache.BodyHasOcean)
                elevation = elevation.HasValue ? Math.Max(elevation.Value, 0d) : null;

            return elevation;
        }

    }
}
