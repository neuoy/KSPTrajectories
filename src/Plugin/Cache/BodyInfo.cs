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
        // body properties
        internal class BodyInfo
        {
            internal CelestialBody Body => FlightGlobals.Bodies[BodyIndex];   // Not thread safe
            internal int BodyIndex { get; private set; }
            internal Vector3d BodyWorldPos { get; private set; }
            internal bool BodyHasAtmosphere { get; private set; }
            internal bool BodyHasOcean { get; private set; }
            internal bool BodyHasSolidSurface { get; private set; }
            internal double BodyAtmosphereDepth { get; private set; }
            internal double BodyAtmosTempOffset { get; private set; }       // The average day/night temperature at the equator
            internal double BodyMaxGroundHeight { get; private set; }
            internal double BodyRadius { get; private set; }       // Bodies with an atmosphere have a radius equal to their max atmosphere height
            internal double? BodyPqsRadius { get; private set; }   // Bodies without a surface have no PQS data
            internal Vector3d BodyAngularVelocity { get; private set; }
            internal double BodyGravityParameter { get; private set; }
            internal double BodyRotationPeriod { get; private set; }
            internal Vector3d BodyTransformUp { get; private set; }
            internal Vector3d BodyFrameX { get; private set; }
            internal Vector3d BodyFrameY { get; private set; }
            internal Vector3d BodyFrameZ { get; private set; }

            internal BodyInfo(CelestialBody body)
            {
                BodyIndex = body.flightGlobalsIndex;
                BodyHasAtmosphere = body.atmosphere;
                BodyHasOcean = body.ocean;
                BodyHasSolidSurface = body.hasSolidSurface;
                BodyAtmosphereDepth = body.atmosphereDepth;

                BodyAtmosTempOffset = body.latitudeTemperatureBiasCurve.Evaluate(0f)
                    + body.latitudeTemperatureSunMultCurve.Evaluate(0f) * 0.5d
                    + body.axialTemperatureSunMultCurve.Evaluate(0f);

                BodyMaxGroundHeight = body.pqsController != null ? body.pqsController.mapMaxHeight : 0d;
                BodyRadius = body.Radius;
                BodyPqsRadius = body.pqsController.radius;
                BodyAngularVelocity = body.angularVelocity;
                BodyGravityParameter = body.gravParameter;
                BodyRotationPeriod = body.rotationPeriod;
                BodyTransformUp = body.bodyTransform.up;
                Update();
            }

            internal void Update()
            {
                BodyFrameX = Body.BodyFrame.X;
                BodyFrameY = Body.BodyFrame.Y;
                BodyFrameZ = Body.BodyFrame.Z;
                BodyWorldPos = Body.position;
            }
        }

        /// <summary> Adds a collection of KSP CelestialBody's to a collection of BodyInfo's </summary>
        internal static void Add(this ICollection<BodyInfo> collection, IEnumerable<CelestialBody> bodies)
        {
            foreach (CelestialBody body in bodies)
            {
                collection.Add(new BodyInfo(body));
            }
        }

        /// <summary> Updates a collection of BodyInfo's </summary>
        internal static void Update(this ICollection<BodyInfo> collection)
        {
            foreach (BodyInfo body in collection)
            {
                body.Update();
            }
        }
    }
}
