using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TrajectoriesAPI
{
    public static class TrajectoriesAPI
    {
        internal static Type TrajectoryType;
        internal static PropertyInfo Trajectory_fetch;
        internal static PropertyInfo Trajectory_patches;
        internal static MethodInfo Trajectory_computeTrajectory;

        internal static Type PatchType;
        internal static PropertyInfo Patch_startingState;
        internal static PropertyInfo Patch_impactPosition;
        internal static PropertyInfo Patch_rawImpactPosition;
        internal static PropertyInfo Patch_isAtmospheric;
        internal static MethodInfo Patch_GetInfo;

        internal static Type PointType;
        internal static FieldInfo Point_pos;
        internal static FieldInfo Point_aerodynamicForce;
        internal static FieldInfo Point_orbitalVelocity;
        internal static FieldInfo Point_airVelocity;

        internal static Type VesselStateType;
        internal static PropertyInfo VesselState_referenceBody;

        /// <summary>
        /// Returns true if the Trajectories mod is installed and compatible with this version of TrajectoriesAPI.
        /// Other functions usually throw an exception if called while the mod is not installed.
        /// </summary>
        public static bool IsModInstalled()
        {
            if (TrajectoryType != null)
                return true;

            AssemblyLoader.LoadedAssembly loadedAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.name == "Trajectories");
            if (loadedAssembly == null)
                return false;

            Debug.Log("Initializing Trajectories API...");

            Type traj = GetType(loadedAssembly.assembly, "Trajectories.Trajectory");
            if (traj == null) return false;

            if ((Trajectory_fetch = GetProperty(traj, "fetch")) == null) return false;
            if ((Trajectory_patches = GetProperty(traj, "patches")) == null) return false;
            if ((Trajectory_computeTrajectory = GetMethod(traj, "ComputeTrajectory", new Type[] { typeof(Vessel), typeof(float) })) == null) return false;

            if ((PatchType = GetNestedType(traj, "Patch")) == null) return false;
            if ((Patch_startingState = GetProperty(PatchType, "startingState")) == null) return false;
            if ((Patch_impactPosition = GetProperty(PatchType, "impactPosition")) == null) return false;
            if ((Patch_rawImpactPosition = GetProperty(PatchType, "rawImpactPosition")) == null) return false;
            if ((Patch_isAtmospheric = GetProperty(PatchType, "isAtmospheric")) == null) return false;
            if ((Patch_GetInfo = GetMethod(PatchType, "GetInfo", new Type[] { typeof(float) })) == null) return false;

            if ((PointType = GetNestedType(traj, "Point")) == null) return false;
            if ((Point_pos = GetField(PointType, "pos")) == null) return false;
            if ((Point_aerodynamicForce = GetField(PointType, "aerodynamicForce")) == null) return false;
            if ((Point_orbitalVelocity = GetField(PointType, "orbitalVelocity")) == null) return false;
            if ((Point_airVelocity = GetField(PointType, "airVelocity")) == null) return false;

            if ((VesselStateType = GetNestedType(traj, "VesselState")) == null) return false;
            if ((VesselState_referenceBody = GetProperty(VesselStateType, "referenceBody")) == null) return false;

            Debug.Log("Trajectories API initialized");

            TrajectoryType = traj;
            return true;
        }

        private static Type GetType(Assembly assembly, string typeName)
        {
            Type res = assembly.GetType(typeName);
            if (res == null)
                Debug.Log("Type " + typeName + " not found in assembly " + assembly.FullName);
            return res;
        }

        private static Type GetNestedType(Type type, string nestedTypeName)
        {
            Type res = type.GetNestedType(nestedTypeName);
            if (res == null)
                Debug.Log("Nested type " + nestedTypeName + " not found in type " + type.FullName);
            return res;
        }

        private static PropertyInfo GetProperty(Type type, string propertyName)
        {
            PropertyInfo res = type.GetProperty(propertyName);
            if (res == null)
                Debug.Log("Property " + propertyName + " not found in type " + type.FullName);
            return res;
        }

        private static FieldInfo GetField(Type type, string fieldName)
        {
            FieldInfo res = type.GetField(fieldName);
            if (res == null)
                Debug.Log("Field " + fieldName + " not found in type " + type.FullName);
            return res;
        }

        private static MethodInfo GetMethod(Type type, string methodName, Type[] types)
        {
            MethodInfo res = type.GetMethod(methodName, types);
            if (res == null)
                Debug.Log("Property " + methodName + " not found in type " + type.FullName);
            return res;
        }

        /// <summary>
        /// Creates a Trajectory object that can be used to compute a predicted trajectory and access data about it.
        /// </summary>
        public static Trajectory CreateTrajectory()
        {
            CheckModInstalled();
            return new Trajectory();
        }

        /// <summary>
        /// Returns the Trajectory that associated to the active vessel (the one that is displayed in the map view).
        /// </summary>
        /// <returns></returns>
        public static Trajectory GetCurrentTrajectory()
        {
            CheckModInstalled();
            return new Trajectory(Trajectory_fetch.GetValue(null, null));
        }

        private static void CheckModInstalled()
        {
            if (!IsModInstalled())
                throw new Exception("Trying to access TrajectoriesAPI, but the Trajectories mod is not installed");
        }
    }
}
