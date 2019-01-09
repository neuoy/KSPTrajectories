/*
  Copyright© (c) 2014-2017 Youen Toupin, (aka neuoy).
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
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using UnityEngine;

namespace Trajectories
{
    public static class Util
    {
        private static Dictionary<string, ScreenMessage> messages = new Dictionary<string, ScreenMessage>();

        public static void PostSingleScreenMessage(string id, string message)
        {
            if (messages.ContainsKey(id))
                ScreenMessages.RemoveMessage(messages[id]);
            messages[id] = ScreenMessages.PostScreenMessage(message);
        }

        public static MethodInfo GetMethodEx(this Type type, string methodName, BindingFlags flags)
        {
            try
            {
                var res = type.GetMethod(methodName, flags);
                if (res == null)
                    throw new Exception("method not found");
                return res;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to GetMethod " + methodName + " on type " + type.FullName + " with flags " + flags + ":\n" + e.Message + "\n" + e.StackTrace);
            }
        }

        public static MethodInfo GetMethodEx(this Type type, string methodName, Type[] types)
        {
            try
            {
                var res = type.GetMethod(methodName, types);
                if (res == null)
                    throw new Exception("method not found");
                return res;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to GetMethod " + methodName + " on type " + type.FullName + " with types " + types.ToString() + ":\n" + e.Message + "\n" + e.StackTrace);
            }
        }

        public static MethodInfo GetMethodEx(this Type type, string methodName, BindingFlags flags, Type[] types)
        {
            try
            {
                var res = type.GetMethod(methodName, flags, null, types, null);
                if (res == null)
                    throw new Exception("method not found");
                return res;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to GetMethod " + methodName + " on type " + type.FullName + " with types " + types.ToString() + ":\n" + e.Message + "\n" + e.StackTrace);
            }
        }

        public static Vector3d SwapYZ(Vector3d v)
        {
            return new Vector3d(v.x, v.z, v.y);
        }

        public static Vector3 SwapYZ(Vector3 v)
        {
            return new Vector3(v.x, v.z, v.y);
        }

        public static string ToString(this Vector3d v, string format = "0.000")
        {
            return "[" + v.x.ToString(format) + ", " + v.y.ToString(format) + ", " + v.z.ToString(format) + "]";
        }

        public static string ToString(this Vector3 v, string format = "0.000")
        {
            return "[" + v.x.ToString(format) + ", " + v.y.ToString(format) + ", " + v.z.ToString(format) + "]";
        }



        // --------------------------------------------------------------------------
        // --- TIME -----------------------------------------------------------------

        /// <summary> Return hours in a KSP day. </summary>
        public static double HoursInDay
        {
            get
            {
                return GameSettings.KERBIN_TIME ? 6.0 : 24.0;
            }
        }

        /// <summary> Return days in a KSP year. </summary>
        public static double DaysInYear
        {
            get
            {
                if (!FlightGlobals.ready)
                    return 426.0;
                return Math.Floor(FlightGlobals.GetHomeBody().orbit.period / (HoursInDay * 60.0 * 60.0));
            }
        }

        /// <summary> Get current time in clocks. </summary>
        public static double Clocks
        {
            get
            {
                return Stopwatch.GetTimestamp();
            }
        }

        /// <summary> Convert from clocks to microseconds. </summary>
        public static double Microseconds(double clocks)
        {
            return clocks * 1000000.0 / Stopwatch.Frequency;
        }

        /// <summary> Convert from clocks to milliseconds. </summary>
        public static double Milliseconds(double clocks)
        {
            return clocks * 1000.0 / Stopwatch.Frequency;
        }

        /// <summary> Convert from clocks to seconds. </summary>
        public static double Seconds(double clocks)
        {
            return clocks / Stopwatch.Frequency;
        }



        // --------------------------------------------------------------------------
        // --- GAME LOGIC -----------------------------------------------------------

        /// <summary> Returns true if the current scene is flight. </summary>
        public static bool IsFlight
        {
            get
            {
                return HighLogic.LoadedSceneIsFlight;
            }
        }

        /// <summary> Returns true if the current scene is editor. </summary>
        public static bool IsEditor
        {
            get
            {
                return HighLogic.LoadedSceneIsEditor;
            }
        }

        /// <summary> Returns true if the current scene is not the main menu. </summary>
        public static bool IsGame
        {
            get
            {
                return HighLogic.LoadedSceneIsGame;
            }
        }

        /// <summary> Returns true if the current scene is tracking station. </summary>
        public static bool IsTrackingStation
        {
            get
            {
                return (HighLogic.LoadedScene == GameScenes.TRACKSTATION);
            }
        }

        /// <summary> Returns true if the current view is map. </summary>
        public static bool IsMap
        {
            get
            {
                return MapView.MapIsEnabled;
            }
        }

        /// <summary> Returns true if game is paused. </summary>
        public static bool IsPaused
        {
            get
            {
                return FlightDriver.Pause || Planetarium.Pause;
            }
        }

        /// <summary> Check if patched conics are available in the current save. </summary>
        /// <returns>True if patched conics are available</returns>
        public static bool IsPatchedConicsAvailable
        {
            get
            {
                // Get our level of tracking station
                float trackingstation_level = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation);

                // Check if the tracking station knows Patched Conics
                return GameVariables.Instance.GetOrbitDisplayMode(trackingstation_level).CompareTo(
                        GameVariables.OrbitDisplayMode.PatchedConics) >= 0;
            }
        }



        // --------------------------------------------------------------------------
        // --- RANDOM ---------------------------------------------------------------

        /// <summary> Random number generator. </summary>
        static System.Random rng = new System.Random();

        /// <summary> Returns random integer in [0..max_value] range. </summary>
        public static int RandomInt(int max_value)
        {
            return rng.Next(max_value);
        }

        /// <summary> Returns random float in [0..1] range. </summary>
        public static float RandomFloat()
        {
            return (float)rng.NextDouble();
        }

        /// <summary> Returns random double in [0..1] range. </summary>
        public static double RandomDouble()
        {
            return rng.NextDouble();
        }

        static int fast_float_seed = 1;
        /// <summary> Returns random float in [-1,+1] range.
        /// Note: it is less random than the c# RNG, but is way faster. </summary>
        public static float FastRandomFloat()
        {
            fast_float_seed *= 16807;
            return (float)fast_float_seed * 4.6566129e-010f;
        }



        /// <summary>
        /// Calculate the shortest great-circle distance between two points on a sphere which are given by latitude and longitude.
        ///
        ///
        /// https://en.wikipedia.org/wiki/Haversine_formula
        /// </summary>
        /// <param name="bodyRadius"></param> Radius of the sphere in meters
        /// <param name="originLatidue"></param>Latitude of the origin of the distance
        /// <param name="originLongitude"></param>Longitude of the origin of the distance
        /// <param name="destinationLatitude"></param>Latitude of the destination of the distance
        /// <param name="destinationLongitude"></param>Longitude of the destination of the distance
        /// <returns>Distance between origin and source in meters</returns>
        public static double DistanceFromLatitudeAndLongitude(
            double bodyRadius,
            double originLatidue, double originLongitude,
            double destinationLatitude, double destinationLongitude)
        {
            double sin1 = Math.Sin(Math.PI / 180.0 * (originLatidue - destinationLatitude) / 2);
            double sin2 = Math.Sin(Math.PI / 180.0 * (originLongitude - destinationLongitude) / 2);
            double cos1 = Math.Cos(Math.PI / 180.0 * destinationLatitude);
            double cos2 = Math.Cos(Math.PI / 180.0 * originLatidue);

            double lateralDist = 2 * bodyRadius *
                Math.Asin(Math.Sqrt(sin1 * sin1 + cos1 * cos2 * sin2 * sin2));

            return lateralDist;
        }
    }
}
