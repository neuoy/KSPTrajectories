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

using System.Collections.Generic;
using UnityEngine;

namespace Trajectories
{
    /// <summary> Contains copied game data that can be used outside the Unity main thread. All needed data must be copied and not referenced. </summary>
    internal static class GameDataCache
    {
        #region VESSEL_PART_PROPERTIES
        // vessel part wing properties
        internal class WingInfo
        {
            internal ModuleLiftingSurface.TransformDir TransformDir { get; private set; }
            internal double TransformSign { get; private set; }
            internal bool OmniDirectional { get; private set; }
            internal bool HasPartAttached { get; private set; }
            internal double DeflectionLiftCoeff { get; private set; }
            internal bool PerpendicularOnly { get; private set; }
            internal Vector3d VelocityNormal { get; private set; }
            internal FloatCurve LiftCurve { get; private set; }
            internal FloatCurve LiftMachCurve { get; private set; }
            internal FloatCurve DragCurve { get; private set; }
            internal FloatCurve DragMachCurve { get; private set; }

            internal WingInfo(ModuleLiftingSurface module)
            {
                TransformDir = module.transformDir;
                TransformSign = module.transformSign;
                OmniDirectional = module.omnidirectional;
                AttachNode node = Util.ReflectionValue<AttachNode>(module, "attachNode");
                HasPartAttached = module.nodeEnabled && node?.attachedPart != null;
                DeflectionLiftCoeff = module.deflectionLiftCoeff;
                PerpendicularOnly = module.perpendicularOnly;
                VelocityNormal = Util.ReflectionValue<Vector3>(module, "nVel");
                LiftCurve = new FloatCurve(module.liftCurve.Curve.keys);
                LiftMachCurve = new FloatCurve(module.liftMachCurve.Curve.keys);
                DragCurve = new FloatCurve(module.dragCurve.Curve.keys);
                DragMachCurve = new FloatCurve(module.dragMachCurve.Curve.keys);
            }

        }

        // vessel part properties
        internal class PartInfo
        {
            internal Part Part { get; private set; }
            internal List<WingInfo> Wings { get; private set; }
            internal bool ShieldedFromAirstream { get; private set; }
            internal bool HasRigidbody { get; private set; }
            internal Part.DragModel DragModel { get; private set; }
            internal bool HasCubes { get; private set; }
            internal DragCubeList DragCubes { get; private set; }
            internal Quaternion Rotation { get; private set; }
            internal double MaxDrag { get; private set; }
            internal double MinDrag { get; private set; }
            internal Vector3d DragVector { get; private set; }
            internal bool HasLiftModule { get; private set; }
            internal double BodyLiftMultiplier { get; private set; }
            internal Vector3d TransformRight { get; private set; }
            internal Vector3d TransformUp { get; private set; }
            internal Vector3d TransformForward { get; private set; }

            internal PartInfo(Part part)
            {
                Part = part;

                Wings = new List<WingInfo>() { part.protoPartSnapshot.modules };

                ShieldedFromAirstream = part.ShieldedFromAirstream;
                HasRigidbody = part.Rigidbody != null;

                DragModel = part.dragModel;
                HasCubes = !part.DragCubes.None;
                DragCubes = part.DragCubes;

                MaxDrag = part.maximum_drag;
                MinDrag = part.minimum_drag;
                HasLiftModule = part.hasLiftModule;

                Update(part);
            }

            internal void Update(Part part)
            {
                Rotation = part.transform.rotation.Clone();
                DragVector = part.dragReferenceVector;
                BodyLiftMultiplier = part.bodyLiftMultiplier;
                TransformRight = part.transform.right;
                TransformUp = part.transform.up;
                TransformForward = part.transform.forward;

                // update vessel mass
                if (part.physicalSignificance != Part.PhysicalSignificance.NONE)
                    VesselMass += part.mass + part.GetResourceMass() + part.GetPhysicslessChildMass();
            }
        }

        /// <summary> Adds a collection of KSP Part's LiftingSurface's to a collection of WingInfo's </summary>
        internal static void Add(this ICollection<WingInfo> collection, IEnumerable<ProtoPartModuleSnapshot> modules)
        {
            foreach (ProtoPartModuleSnapshot module in modules)
            {
                if (module.moduleRef is ModuleLiftingSurface)
                    collection.Add(new WingInfo(module.moduleRef as ModuleLiftingSurface));
            }
        }

        /// <summary> Adds a collection of KSP Part's to a collection of PartInfo's </summary>
        internal static void Add(this ICollection<PartInfo> collection, IEnumerable<Part> parts)
        {
            VesselMass = 0d;
            foreach (Part part in parts)
            {
                collection.Add(new PartInfo(part));
            }
        }

        /// <summary> Updates a collection of PartInfo's from a collection of KSP Part's </summary>
        internal static void Update(this ICollection<PartInfo> collection, IEnumerable<Part> parts)
        {
            IEnumerator<Part> enumerator = parts.GetEnumerator();
            VesselMass = 0d;

            foreach (PartInfo part_info in collection)
            {
                if (!enumerator.MoveNext())
                    break;
                part_info.Update(enumerator.Current);
            }
        }

        /// <summary> Clears a collection of PartInfo's </summary>
        internal static void Release(this ICollection<PartInfo> collection)
        {
            VesselMass = 0d;

            foreach (PartInfo part_info in collection)
            {
                part_info.Wings?.Clear();
            }

            collection.Clear();
        }
        #endregion

        #region BODY_PROPERTIES
        // body properties
        internal static CelestialBody Body { get; private set; }
        internal static int BodyIndex { get; private set; }
        internal static Vector3d BodyWorldPos { get; private set; }
        internal static bool BodyHasAtmosphere { get; private set; }
        internal static bool BodyHasOcean { get; private set; }
        internal static bool BodyHasSolidSurface { get; private set; }
        internal static double BodyAtmosphereDepth { get; private set; }
        internal static double BodyAtmosTempOffset { get; private set; }       // The average day/night temperature at the equator
        internal static double BodyMaxGroundHeight { get; private set; }
        internal static double BodyRadius { get; private set; }       // Bodies with an atmosphere have a radius equal to their max atmosphere height
        internal static double? BodyPqsRadius { get; private set; }   // Bodies without a surface have no PQS data
        internal static Vector3d BodyAngularVelocity { get; private set; }
        internal static double BodyGravityParameter { get; private set; }
        internal static double BodyRotationPeriod { get; private set; }
        internal static Vector3d BodyTransformUp { get; private set; }
        internal static Vector3d BodyFrameX { get; private set; }
        internal static Vector3d BodyFrameY { get; private set; }
        internal static Vector3d BodyFrameZ { get; private set; }
        #endregion

        #region VESSEL_PROPERTIES
        // vessel properties
        internal static Vessel AttachedVessel { get; private set; }
        internal static List<PartInfo> VesselParts { get; private set; }
        internal static double VesselMass { get; private set; }
        internal static Vector3d VesselWorldPos { get; private set; }
        internal static Vector3d VesselOrbitVelocity { get; private set; }
        internal static Vector3d VesselTransformUp { get; private set; }
        internal static Vector3d VesselTransformForward { get; private set; }
        #endregion

        internal static double UniversalTime { get; private set; }
        internal static double WarpDeltaTime { get; private set; }
        internal static Vector3d SunWorldPos { get; private set; }

        internal static List<ManeuverNode> ManeuverNodes { get; private set; }
        internal static Orbit Orbit { get; private set; }
        internal static List<Orbit> FlightPlan { get; private set; }

        internal static void Start()
        {
            Util.DebugLog("Constructing");
            SunWorldPos = FlightGlobals.Bodies[0].position;
        }

        /// <summary> Updates entire cache </summary>
        internal static bool Update()
        {
            Profiler.Start("GameDataCache.Update");

            UniversalTime = Planetarium.GetUniversalTime();
            WarpDeltaTime = TimeWarp.fixedDeltaTime;
            SunWorldPos = FlightGlobals.Bodies[0].position;

            // check for celestial body change
            if (Trajectories.AttachedVessel.mainBody?.name != null && Body != Trajectories.AttachedVessel.mainBody)
            {
                Util.DebugLog("Updating body to {0}", Trajectories.AttachedVessel.mainBody?.name);

                Body = null;
                BodyIndex = 0;

                int index = 0;
                foreach (CelestialBody body in FlightGlobals.Bodies)
                {
                    if (body?.name != null && body.name == Trajectories.AttachedVessel.mainBody.name)
                    {
                        Body = body;
                        BodyIndex = index;
                        break;
                    }
                    index++;
                }

                if (Body == null)
                {
                    Clear();
                    return false;
                }

                UpdateBodyCache();
            }

            // check for vessel changes
            if (AttachedVessel != Trajectories.AttachedVessel || VesselParts?.Count != Trajectories.AttachedVessel.Parts.Count)
            {
                Util.DebugLog("Updating {0} due to {1} change",
                    Trajectories.AttachedVessel.name, AttachedVessel != Trajectories.AttachedVessel ? "vessel" : "parts count");

                AttachedVessel = Trajectories.AttachedVessel;
                VesselParts?.Release();
                VesselParts = new List<PartInfo>() { AttachedVessel.Parts };

                Trajectories.AerodynamicModel.InitCache();
            }

            if (AttachedVessel.patchedConicSolver == null)
            {
                Util.DebugLogWarning("PatchedConicsSolver is null, skipping.");
                return false;
            }

            BodyFrameX = Body.BodyFrame.X;
            BodyFrameY = Body.BodyFrame.Y;
            BodyFrameZ = Body.BodyFrame.Z;
            BodyWorldPos = Body.position;

            UpdateVesselCache();

            ManeuverNodes = new List<ManeuverNode>(AttachedVessel.patchedConicSolver.maneuverNodes);
            Orbit = new Orbit(AttachedVessel.orbit);
            FlightPlan = new List<Orbit>(AttachedVessel.patchedConicSolver.flightPlan);

            Profiler.Stop("GameDataCache.Update");
            return true;
        }

        /// <summary> Clears the cache </summary>
        internal static void Clear()
        {
            ClearBodyCache();
            ClearVesselCache();
        }

        private static void ClearBodyCache()
        {
            Body = null;
            BodyIndex = 0;
            BodyHasAtmosphere = false;
            BodyHasOcean = false;
            BodyHasSolidSurface = false;
            BodyAtmosphereDepth = 0d;

            BodyAtmosTempOffset = 0d;

            BodyMaxGroundHeight = 0d;
            BodyRadius = 0d;
            BodyPqsRadius = null;
            BodyAngularVelocity = Vector3d.zero;
            BodyGravityParameter = 0d;
            BodyRotationPeriod = 0d;
            BodyTransformUp = Vector3d.zero;
            BodyFrameX = Vector3d.zero;
            BodyFrameY = Vector3d.zero;
            BodyFrameZ = Vector3d.zero;
            BodyWorldPos = Vector3d.zero;
        }

        private static void ClearVesselCache()
        {
            AttachedVessel = null;
            VesselWorldPos = Vector3d.zero;
            VesselOrbitVelocity = Vector3d.zero;
            VesselTransformUp = Vector3d.zero;
            VesselTransformForward = Vector3d.zero;

            VesselParts?.Clear();
            VesselParts = null;
        }

        private static void UpdateBodyCache()
        {
            BodyHasAtmosphere = Body.atmosphere;
            BodyHasOcean = Body.ocean;
            BodyHasSolidSurface = Body.hasSolidSurface;
            BodyAtmosphereDepth = Body.atmosphereDepth;

            BodyAtmosTempOffset = Body.latitudeTemperatureBiasCurve.Evaluate(0f)
                + Body.latitudeTemperatureSunMultCurve.Evaluate(0f) * 0.5d
                + Body.axialTemperatureSunMultCurve.Evaluate(0f);

            BodyMaxGroundHeight = Body.pqsController != null ? Body.pqsController.mapMaxHeight : 0d;
            BodyRadius = Body.Radius;
            BodyPqsRadius = Body.pqsController.radius;
            BodyAngularVelocity = Body.angularVelocity;
            BodyGravityParameter = Body.gravParameter;
            BodyRotationPeriod = Body.rotationPeriod;
            BodyTransformUp = Body.bodyTransform.up;
        }

        private static void UpdateVesselCache()
        {
            VesselWorldPos = AttachedVessel.GetWorldPos3D();
            VesselOrbitVelocity = AttachedVessel.obt_velocity;
            VesselTransformUp = AttachedVessel.ReferenceTransform.up;
            VesselTransformForward = AttachedVessel.ReferenceTransform.forward;

            VesselParts.Update(AttachedVessel.Parts);
        }
    }
}
