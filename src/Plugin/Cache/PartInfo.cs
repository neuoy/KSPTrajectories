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
    /// <summary> Contains copied game data that can be used outside the Unity main thread. All needed data must be copied and not referenced. </summary>
    internal static partial class GameDataCache
    {
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
    }
}
