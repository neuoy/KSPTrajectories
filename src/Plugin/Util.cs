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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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

        ///<summary> writes a message to the log </summary>
        public static void Log(string message, params object[] param) => UnityEngine.Debug.Log(string.Format("[{0}] {1}",
            MainGUI.TrajectoriesTitle, string.Format(message, param)));

        ///<summary> writes a warning message to the log </summary>
        public static void LogWarning(string message, params object[] param) => UnityEngine.Debug.LogWarning(string.Format("[{0}] Warning: {1}",
            MainGUI.TrajectoriesTitle, string.Format(message, param)));

        ///<summary> writes an error message to the log </summary>
        public static void LogError(string message, params object[] param) => UnityEngine.Debug.LogError(string.Format("[{0}] Error: {1}",
            MainGUI.TrajectoriesTitle, string.Format(message, param)));

        ///<summary> writes a debug message to the log with stack trace info added </summary>
        [Conditional("DEBUG")]
        public static void DebugLog(string message, params object[] param)
        {
            StackTrace stackTrace = new StackTrace();
            UnityEngine.Debug.Log(string.Format("[{0}] Debug: {1}.{2} - {3}",
                MainGUI.TrajectoriesTitle, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name,
                stackTrace.GetFrame(1).GetMethod().Name, string.Format(message, param)));
        }

        ///<summary> writes a debug warning message to the log with stack trace info added </summary>
        [Conditional("DEBUG")]
        public static void DebugLogWarning(string message, params object[] param)
        {
            StackTrace stackTrace = new StackTrace();
            UnityEngine.Debug.LogWarning(string.Format("[{0}] Warning: {1}.{2} - {3}",
                MainGUI.TrajectoriesTitle, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name,
                stackTrace.GetFrame(1).GetMethod().Name, string.Format(message, param)));
        }

        ///<summary> writes a debug error message to the log with stack trace info added </summary>
        [Conditional("DEBUG")]
        public static void DebugLogError(string message, params object[] param)
        {
            StackTrace stackTrace = new StackTrace();
            UnityEngine.Debug.LogError(string.Format("[{0}] Error: {1}.{2} - {3}",
                MainGUI.TrajectoriesTitle, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name,
                stackTrace.GetFrame(1).GetMethod().Name, string.Format(message, param)));
        }

        public static MethodInfo GetMethodEx(this Type type, string methodName, BindingFlags flags)
        {
            try
            {
                MethodInfo res = type.GetMethod(methodName, flags);
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
                MethodInfo res = type.GetMethod(methodName, types);
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
                MethodInfo res = type.GetMethod(methodName, flags, null, types, null);
                if (res == null)
                    throw new Exception("method not found");
                return res;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to GetMethod " + methodName + " on type " + type.FullName + " with types " + types.ToString() + ":\n" + e.Message + "\n" + e.StackTrace);
            }
        }


        // --------------------------------------------------------------------------
        // --- Math --------------------------------------------------------------

        /// <summary>
        /// Clamps a double value using the absolute value for comparison, optional return values for min and max can be passed
        /// </summary>
        public static double ClampAbs(double value, double min, double max, double rtn_min = 0d, double rtn_max = 1d)
        {
            if (Math.Abs(value) < min)
                return rtn_min;
            else if (Math.Abs(value) > max)
                return rtn_max;
            else
                return value;
        }


        // --------------------------------------------------------------------------
        // --- Vectors --------------------------------------------------------------

        public static Vector3d SwapYZ(Vector3d v) => new Vector3d(v.x, v.z, v.y);

        public static Vector3 SwapYZ(Vector3 v) => new Vector3(v.x, v.z, v.y);

        public static string ToString(this Vector3d v, string format = "0.000") => "[" + v.x.ToString(format) + ", " + v.y.ToString(format) + ", " + v.z.ToString(format) + "]";

        public static string ToString(this Vector3 v, string format = "0.000") => "[" + v.x.ToString(format) + ", " + v.y.ToString(format) + ", " + v.z.ToString(format) + "]";



        // --------------------------------------------------------------------------
        // --- TIME -----------------------------------------------------------------

        /// <summary> Return hours in a KSP day. </summary>
        public static double HoursInDay => GameSettings.KERBIN_TIME ? 6.0 : 24.0;

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
        public static double Clocks => Stopwatch.GetTimestamp();

        /// <summary> Convert from clocks to microseconds. </summary>
        public static double Microseconds(double clocks) => clocks * 1000000.0 / Stopwatch.Frequency;

        /// <summary> Convert from clocks to milliseconds. </summary>
        public static double Milliseconds(double clocks) => clocks * 1000.0 / Stopwatch.Frequency;

        /// <summary> Convert from clocks to seconds. </summary>
        public static double Seconds(double clocks) => clocks / Stopwatch.Frequency;



        // --------------------------------------------------------------------------
        // --- GAME LOGIC -----------------------------------------------------------

        /// <summary> Returns true if the current scene is flight. </summary>
        public static bool IsFlight => HighLogic.LoadedSceneIsFlight;

        /// <summary> Returns true if the current scene is editor. </summary>
        public static bool IsEditor => HighLogic.LoadedSceneIsEditor;

        /// <summary> Returns true if the current scene is not the main menu. </summary>
        public static bool IsGame => HighLogic.LoadedSceneIsGame;

        /// <summary> Returns true if the current scene is tracking station. </summary>
        public static bool IsTrackingStation => (HighLogic.LoadedScene == GameScenes.TRACKSTATION);

        /// <summary> Returns true if the current view is map. </summary>
        public static bool IsMap => MapView.MapIsEnabled;

        /// <summary> Returns true if game is paused. </summary>
        public static bool IsPaused => FlightDriver.Pause || Planetarium.Pause;

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
        private static System.Random rng = new System.Random();

        /// <summary> Returns random integer in [0..max_value] range. </summary>
        public static int RandomInt(int max_value) => rng.Next(max_value);

        /// <summary> Returns random float in [0..1] range. </summary>
        public static float RandomFloat() => (float)rng.NextDouble();

        /// <summary> Returns random double in [0..1] range. </summary>
        public static double RandomDouble() => rng.NextDouble();

        private static int fast_float_seed = 1;
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
