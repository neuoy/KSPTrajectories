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
            #region PROPERTIES
            internal CelestialBody Body => FlightGlobals.Bodies[Index];   // Not thread safe
            internal int Index { get; private set; }
            internal BodyInfo ReferenceBody { get; private set; }
            internal Vector3d Position { get; private set; }
            internal List<BodyInfo> OrbitingBodies { get; private set; }
            internal bool HasAtmosphere { get; private set; }
            internal bool HasOcean { get; private set; }
            internal bool HasSolidSurface { get; private set; }
            internal double AtmosphereDepth { get; private set; }
            internal double AtmosTempOffset { get; private set; }       // The average day/night temperature at the equator
            internal double MaxGroundHeight { get; private set; }
            internal double Radius { get; private set; }       // Bodies with an atmosphere have a radius equal to their max atmosphere height
            internal double? PqsRadius { get; private set; }   // Bodies without a surface have no PQS data
            internal Vector3d AngularVelocity { get; private set; }
            internal double GravityParameter { get; private set; }
            internal double RotationPeriod { get; private set; }
            internal double SphereOfInfluence { get; private set; }
            internal Vector3d TransformUp { get; private set; }
            internal Util.Frame Frame { get; private set; }
            #endregion

            internal BodyInfo(CelestialBody body)
            {
                Index = body.flightGlobalsIndex;
                ReferenceBody = body.referenceBody ? Bodies[body.referenceBody.flightGlobalsIndex] : null;
                OrbitingBodies = new() { body.orbitingBodies };

                HasAtmosphere = body.atmosphere;
                HasOcean = body.ocean;
                HasSolidSurface = body.hasSolidSurface;
                AtmosphereDepth = body.atmosphereDepth;

                AtmosTempOffset = body.latitudeTemperatureBiasCurve.Evaluate(0f)
                    + body.latitudeTemperatureSunMultCurve.Evaluate(0f) * 0.5d
                    + body.axialTemperatureSunMultCurve.Evaluate(0f);

                MaxGroundHeight = body.pqsController != null ? body.pqsController.mapMaxHeight : 0d;
                Radius = body.Radius;
                PqsRadius = body.pqsController?.radius;
                AngularVelocity = body.angularVelocity;
                GravityParameter = body.gravParameter;
                RotationPeriod = body.rotationPeriod;
                SphereOfInfluence = body.sphereOfInfluence;
                TransformUp = body.bodyTransform.up;
                Frame = new();
                Update();
            }

            internal void Update()
            {
                Frame.Set(Body.BodyFrame);
                Position = Body.position;
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
