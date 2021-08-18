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
    internal static class Util
    {
        #region LOGGING
        // --------------------------------------------------------------------------
        // --- Logging --------------------------------------------------------------

        private static Dictionary<string, ScreenMessage> messages = new Dictionary<string, ScreenMessage>();

        internal static void PostSingleScreenMessage(string id, string message)
        {
            if (messages.ContainsKey(id))
                ScreenMessages.RemoveMessage(messages[id]);
            messages[id] = ScreenMessages.PostScreenMessage(message);
        }

        ///<summary> Writes a message to the log with 'Trajectories' appended to the message </summary>
        internal static void Log(string message, params object[] param) => UnityEngine.Debug.Log(string.Format("[{0}] {1}",
            MainGUI.TrajectoriesTitle, string.Format(message, param)));

        ///<summary> Writes a warning message to the log with 'Trajectories' appended to the message </summary>
        internal static void LogWarning(string message, params object[] param) => UnityEngine.Debug.LogWarning(string.Format("[{0}] Warning: {1}",
            MainGUI.TrajectoriesTitle, string.Format(message, param)));

        ///<summary> Writes an error message to the log with 'Trajectories' appended to the message </summary>
        internal static void LogError(string message, params object[] param) => UnityEngine.Debug.LogError(string.Format("[{0}] Error: {1}",
            MainGUI.TrajectoriesTitle, string.Format(message, param)));

        ///<summary> Writes a debug message to the log with 'Trajectories' and stack trace info appended to the message </summary>
        [Conditional("DEBUG")]
        internal static void DebugLog(string message, params object[] param)
        {
            StackTrace stackTrace = new StackTrace();
            UnityEngine.Debug.Log(string.Format("[{0}] Debug: {1}.{2} - {3}",
                MainGUI.TrajectoriesTitle, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name,
                stackTrace.GetFrame(1).GetMethod().Name, string.Format(message, param)));
        }

        ///<summary> Writes a debug warning message to the log with 'Trajectories' and stack trace info appended to the message </summary>
        [Conditional("DEBUG")]
        internal static void DebugLogWarning(string message, params object[] param)
        {
            StackTrace stackTrace = new StackTrace();
            UnityEngine.Debug.LogWarning(string.Format("[{0}] Warning: {1}.{2} - {3}",
                MainGUI.TrajectoriesTitle, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name,
                stackTrace.GetFrame(1).GetMethod().Name, string.Format(message, param)));
        }

        ///<summary> Writes a debug error message to the log with 'Trajectories' and stack trace info appended to the message </summary>
        [Conditional("DEBUG")]
        internal static void DebugLogError(string message, params object[] param)
        {
            StackTrace stackTrace = new StackTrace();
            UnityEngine.Debug.LogError(string.Format("[{0}] Error: {1}.{2} - {3}",
                MainGUI.TrajectoriesTitle, stackTrace.GetFrame(1).GetMethod().ReflectedType.Name,
                stackTrace.GetFrame(1).GetMethod().Name, string.Format(message, param)));
        }
        #endregion


        #region REFLECTION
        // --------------------------------------------------------------------------
        // --- Reflection -----------------------------------------------------------

        private static readonly BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        internal static MethodInfo GetMethodEx(this Type type, string methodName, BindingFlags flags)
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

        internal static MethodInfo GetMethodEx(this Type type, string methodName, Type[] types)
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

        internal static MethodInfo GetMethodEx(this Type type, string methodName, BindingFlags flags, Type[] types)
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

        // return a value from a module using reflection
        // note: useful when the module is from another assembly, unknown at build time
        // note: useful when the value isn't persistent
        // note: this function break hard when external API change, by design
        public static T ReflectionValue<T>(PartModule m, string value_name)
        {
            return (T)m.GetType().GetField(value_name, flags).GetValue(m);
        }

        public static T? SafeReflectionValue<T>(PartModule m, string value_name) where T : struct
        {
            FieldInfo fi = m.GetType().GetField(value_name, flags);
            if (fi == null)
                return null;
            return (T)fi.GetValue(m);
        }

        // set a value from a module using reflection
        // note: useful when the module is from another assembly, unknown at build time
        // note: useful when the value isn't persistent
        // note: this function break hard when external API change, by design
        public static void ReflectionValue<T>(PartModule m, string value_name, T value)
        {
            m.GetType().GetField(value_name, flags).SetValue(m, value);
        }

        ///<summary> Sets the value of a private field via reflection </summary>
        public static void ReflectionValue<T>(object instance, string value_name, T value)
        {
            instance.GetType().GetField(value_name, flags).SetValue(instance, value);
        }

        ///<summary> Returns the value of a private field via reflection </summary>
        public static T ReflectionValue<T>(object instance, string field_name)
        {
            return (T)instance.GetType().GetField(field_name, flags).GetValue(instance);
        }

        public static void ReflectionCall(object m, string call_name)
        {
            m.GetType().GetMethod(call_name, flags).Invoke(m, null);
        }

        public static T ReflectionCall<T>(object m, string call_name)
        {
            return (T)(m.GetType().GetMethod(call_name, flags).Invoke(m, null));
        }


        #endregion


        #region MATH
        // --------------------------------------------------------------------------
        // --- Math -----------------------------------------------------------------

        internal const double HALF_PI = Math.PI * 0.5d;

        /// <returns> true if not a number </returns>
        internal static bool IsNaN(this float value) => float.IsNaN(value);

        /// <returns> true if not a number </returns>
        internal static bool IsNaN(this double value) => double.IsNaN(value);

        /// <summary> Linearly interpolates a double value between a and b by t </summary>
        internal static double Lerp(double a, double b, double t) => (a * (1 - t)) + (b * t);

        /// <summary> Clamps a double value </summary>
        internal static double Clamp01(double value)
        {
            if (value < 0d)
                return 0d;
            else if (value > 1d)
                return 1d;
            else
                return value;
        }

        /// <summary> Clamps a double value </summary>
        internal static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            else if (value > max)
                return max;
            else
                return value;
        }

        /// <summary> Clamps a double value, optional return values for min and max can be passed </summary>
        internal static double Clamp(double value, double min, double max, double rtn_min = 0d, double rtn_max = 1d)
        {
            if (value < min)
                return rtn_min;
            else if (value > max)
                return rtn_max;
            else
                return value;
        }

        /// <summary> Clamps a double value using the absolute value for comparison, optional return values for min and max can be passed </summary>
        internal static double ClampAbs(double value, double min, double max, double rtn_min = 0d, double rtn_max = 1d)
        {
            if (Math.Abs(value) < min)
                return rtn_min;
            else if (Math.Abs(value) > max)
                return rtn_max;
            else
                return value;
        }

        #endregion


        #region VECTORS
        // --------------------------------------------------------------------------
        // --- Vectors --------------------------------------------------------------

        /// <returns> A new cloned Quaternion </returns>
        internal static Quaternion Clone(this Quaternion value) => new Quaternion(value.x, value.y, value.z, value.w);

        /// <returns> true if any of the Vector2 elements are not a number </returns>
        internal static bool IsNaN(this Vector2 value) => value.x.IsNaN() || value.y.IsNaN();

        /// <returns> true if any of the Vector2d elements are not a number </returns>
        internal static bool IsNaN(this Vector2d value) => value.x.IsNaN() || value.y.IsNaN();

        /// <returns> true if any of the Vector3 elements are not a number </returns>
        internal static bool IsNaN(this Vector3 value) => value.x.IsNaN() || value.y.IsNaN() || value.z.IsNaN();

        /// <returns> true if any of the Vector3d elements are not a number </returns>
        internal static bool IsNaN(this Vector3d value) => value.x.IsNaN() || value.y.IsNaN() || value.z.IsNaN();

        /// <summary> Clamps a Vector2 returning a new Vector2 </summary>
        internal static Vector2 Clamp(this Vector2 value, Vector2 min, Vector2 max)
        {
            return new Vector2(Mathf.Clamp(value.x, min.x, max.x),
                               Mathf.Clamp(value.y, min.y, max.y));
        }

        /// <summary> Clamps a Vector2, result also appears in the input vector </summary>
        internal static Vector2 ClampSelf(this Vector2 value, Vector2 min, Vector2 max)
        {
            value.x = Mathf.Clamp(value.x, min.x, max.x);
            value.y = Mathf.Clamp(value.y, min.y, max.y);
            return value;
        }

        /// <summary> Clamps a Vector2d returning a new Vector2d </summary>
        internal static Vector2d Clamp(this Vector2d value, Vector2d min, Vector2d max)
        {
            return new Vector2d(Clamp(value.x, min.x, max.x),
                                Clamp(value.y, min.y, max.y));
        }

        /// <summary> Clamps a Vector2d, result also appears in the input vector </summary>
        internal static Vector2d ClampSelf(this Vector2d value, Vector2d min, Vector2d max)
        {
            value.x = Clamp(value.x, min.x, max.x);
            value.y = Clamp(value.y, min.y, max.y);
            return value;
        }

        /// <summary> Clamps a Vector3 returning a new Vector3 </summary>
        internal static Vector3 Clamp(this Vector3 value, Vector3 min, Vector3 max)
        {
            return new Vector3(Mathf.Clamp(value.x, min.x, max.x),
                               Mathf.Clamp(value.y, min.y, max.y),
                               Mathf.Clamp(value.z, min.z, max.z));
        }

        /// <summary> Clamps a Vector3, result also appears in the input vector </summary>
        internal static Vector3 ClampSelf(this Vector3 value, Vector3 min, Vector3 max)
        {
            value.x = Mathf.Clamp(value.x, min.x, max.x);
            value.y = Mathf.Clamp(value.y, min.y, max.y);
            value.z = Mathf.Clamp(value.z, min.z, max.z);
            return value;
        }

        /// <summary> Clamps a Vector3d returning a new Vector3d </summary>
        internal static Vector3d Clamp(this Vector3d value, Vector3d min, Vector3d max)
        {
            return new Vector3d(Clamp(value.x, min.x, max.x),
                                Clamp(value.y, min.y, max.y),
                                Clamp(value.z, min.z, max.z));
        }

        /// <summary> Clamps a Vector3d, result also appears in the input vector </summary>
        internal static Vector3d ClampSelf(this Vector3d value, Vector3d min, Vector3d max)
        {
            value.x = Clamp(value.x, min.x, max.x);
            value.y = Clamp(value.y, min.y, max.y);
            value.z = Clamp(value.z, min.z, max.z);
            return value;
        }

        private static double swap_double;
        private static float swap_float;

        internal static Vector3d SwapYZ(this Vector3d v)
        {
            swap_double = v.y;
            v.y = v.z;
            v.z = swap_double;
            return v;
        }

        internal static Vector3 SwapYZ(this Vector3 v)
        {
            swap_float = v.y;
            v.y = v.z;
            v.z = swap_float;
            return v;
        }

        internal static string ToString(this Vector3d v, string format = "0.000") => "[" + v.x.ToString(format) + ", " + v.y.ToString(format) + ", " + v.z.ToString(format) + "]";

        internal static string ToString(this Vector3 v, string format = "0.000") => "[" + v.x.ToString(format) + ", " + v.y.ToString(format) + ", " + v.z.ToString(format) + "]";

        #endregion


        #region TIME
        // --------------------------------------------------------------------------
        // --- Time -----------------------------------------------------------------

        /// <returns> Number of hours in a KSP day. </returns>
        internal static double HoursInDay => GameSettings.KERBIN_TIME ? 6.0d : 24.0d;

        /// <returns> Number of days in a KSP year. </returns>
        internal static double DaysInYear
        {
            get
            {
                if (!FlightGlobals.ready)
                    return 426.0d;
                return Math.Floor(FlightGlobals.GetHomeBody().orbit.period / (HoursInDay * 60.0d * 60.0d));
            }
        }

        internal static double clock_frequency = 1d / Stopwatch.Frequency;
        private const double MINUETS_SCALAR = 1d / 60d;

        /// <returns> Current time in clocks. </returns>
        internal static double Clocks => Stopwatch.GetTimestamp();

        /// <summary> Convert from clocks to microseconds. </summary>
        internal static double Microseconds(double clocks) => clocks * 1e6d * clock_frequency;

        /// <summary> Convert from clocks to milliseconds. </summary>
        internal static double Milliseconds(double clocks) => clocks * 1e3d * clock_frequency;

        /// <summary> Convert from clocks to seconds. </summary>
        internal static double Seconds(double clocks) => clocks * clock_frequency;

        /// <returns> Elapsed time in clocks. </returns>
        internal static double Elapsed(double clocks) => Stopwatch.GetTimestamp() - clocks;

        /// <returns> Elapsed time in microseconds from clocks. </returns>
        internal static double ElapsedMicroseconds(double clocks) => (Stopwatch.GetTimestamp() - clocks) * 1e6d * clock_frequency;

        /// <returns> Elapsed time in milliseconds from clocks. </returns>
        internal static double ElapsedMilliseconds(double clocks) => (Stopwatch.GetTimestamp() - clocks) * 1e3d * clock_frequency;

        /// <returns> Elapsed time in seconds from clocks. </returns>
        internal static double ElapsedSeconds(double clocks) => (Stopwatch.GetTimestamp() - clocks) * clock_frequency;

        /// <returns> Elapsed time in minuets from clocks. </returns>
        internal static double ElapsedMinuets(double clocks) => ((Stopwatch.GetTimestamp() - clocks) * clock_frequency) * MINUETS_SCALAR;

        /// <returns> Elapsed time in minuets and seconds from clocks. eg 108.369s returns 1 minuet and 48.369 seconds </returns>
        internal static void ElapsedMinuetsSeconds(double clocks, out int minuets, out double seconds)
        {
            double ts = Stopwatch.GetTimestamp();
            minuets = (int)(((ts - clocks) * clock_frequency) * MINUETS_SCALAR);
            seconds = ((ts - clocks) * clock_frequency) - (minuets * 60);
        }
        #endregion


        #region GAME_LOGIC
        // --------------------------------------------------------------------------
        // --- Game logic -----------------------------------------------------------

        /// <returns> True if the current scene is space center. </returns>
        internal static bool IsSpaceCenter => HighLogic.LoadedScene == GameScenes.SPACECENTER;

        /// <returns> True if the current scene is flight. </returns>
        internal static bool IsFlight => HighLogic.LoadedSceneIsFlight;

        /// <returns> True if the current scene is editor. </returns>
        internal static bool IsEditor => HighLogic.LoadedSceneIsEditor;

        /// <returns> True if the current scene is not the main menu. </returns>
        internal static bool IsGame => HighLogic.LoadedSceneIsGame;

        /// <returns> True if the current scene is tracking station. </returns>
        internal static bool IsTrackingStation => (HighLogic.LoadedScene == GameScenes.TRACKSTATION);

        /// <returns> True if the current view is map. </returns>
        internal static bool IsMap => MapView.MapIsEnabled;

        /// <returns> True if game is paused. </returns>
        internal static bool IsPaused => FlightDriver.Pause || Planetarium.Pause;

        /// <summary> Check if patched conics are available in the current save. </summary>
        /// <returns> True if patched conics are available</returns>
        internal static bool IsPatchedConicsAvailable
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
        #endregion


        #region RANDOM_NUMBER
        // --------------------------------------------------------------------------
        // --- Random number --------------------------------------------------------

        /// <summary> Random number generator. </summary>
        private static System.Random rng = new System.Random();

        /// <returns> A random integer in the [0..max_value] range. </returns>
        internal static int RandomInt(int max_value) => rng.Next(max_value);

        /// <returns> A random float in the [0..1] range. </returns>
        internal static float RandomFloat() => (float)rng.NextDouble();

        /// <returns> A random double in the [0..1] range. </returns>
        internal static double RandomDouble() => rng.NextDouble();

        internal static int fast_float_seed = 1;
        /// <returns> A random float in the [-1..+1] range.
        /// Note: it is less random than the C# RNG, but is way faster. </returns>
        public static float FastRandomFloat()
        {
            fast_float_seed *= 16807;
            return (float)fast_float_seed * 4.6566129e-010f;
        }
        #endregion


        #region CONFIG
        // --------------------------------------------------------------------------
        // --- Config ---------------------------------------------------------------

        /// <returns> A config node from the config system </returns>
        internal static ConfigNode ParseConfig(string path) => GameDatabase.Instance.GetConfigNode(path) ?? new ConfigNode();

        /// <returns> A set of config nodes from the config system </returns>
        internal static ConfigNode[] ParseConfigs(string path) => GameDatabase.Instance.GetConfigNodes(path);

        /// <returns> A value from a config node </returns>
        internal static T ConfigValue<T>(ConfigNode node, string key, T default_value)
        {
            try
            {
                return node.HasValue(key) ? (T)Convert.ChangeType(node.GetValue(key), typeof(T)) : default_value;
            }
            catch (Exception e)
            {
                LogError("While trying to parse '{0}' from {1} ({2})", key, node.name, e.Message);
                return default_value;
            }
        }

        /// <returns> An enum from a config node </returns>
        internal static T ConfigEnum<T>(ConfigNode node, string key, T default_value)
        {
            try
            {
                return node.HasValue(key) ? (T)Enum.Parse(typeof(T), node.GetValue(key)) : default_value;
            }
            catch (Exception e)
            {
                LogError("Invalid enum in '{0}' from {1} ({2})", key, node.name, e.Message);
                return default_value;
            }
        }
        #endregion


        #region MISC
        // --------------------------------------------------------------------------
        // --- Misc -----------------------------------------------------------------

        /// <summary>
        /// Calculate the shortest great-circle distance between two points on a sphere which are given by latitude and longitude.
        /// https://en.wikipedia.org/wiki/Haversine_formula
        /// </summary>
        /// <param name="bodyRadius"></param> Radius of the sphere in meters
        /// <param name="originLatidue"></param>Latitude of the origin of the distance
        /// <param name="originLongitude"></param>Longitude of the origin of the distance
        /// <param name="destinationLatitude"></param>Latitude of the destination of the distance
        /// <param name="destinationLongitude"></param>Longitude of the destination of the distance
        /// <returns>Distance between origin and source in meters</returns>
        internal static double DistanceFromLatitudeAndLongitude(
            double bodyRadius,
            double originLatidue, double originLongitude,
            double destinationLatitude, double destinationLongitude)
        {

            double sin1 = Math.Sin(Mathf.Deg2Rad * (originLatidue - destinationLatitude) / 2d);
            double sin2 = Math.Sin(Mathf.Deg2Rad * (originLongitude - destinationLongitude) / 2d);
            double cos1 = Math.Cos(Mathf.Deg2Rad * destinationLatitude);
            double cos2 = Math.Cos(Mathf.Deg2Rad * originLatidue);

            double lateralDist = 2d * bodyRadius *
                Math.Asin(Math.Sqrt(sin1 * sin1 + cos1 * cos2 * sin2 * sin2));

            return lateralDist;
        }
        #endregion
    }
}
