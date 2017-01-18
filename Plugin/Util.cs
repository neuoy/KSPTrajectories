/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Trajectories
{
    static class Util
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
            catch(Exception e)
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
        public static double distanceFromLatitudeAndLongitude(
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
