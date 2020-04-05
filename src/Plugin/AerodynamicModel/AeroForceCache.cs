/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).

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
    public class AeroForceCache
    {
        private int VelocityResolution { get;  set; }
        private double AoAResolution { get;  set; }
        private int AltitudeResolution { get;  set; }

        private Dictionary<(int, int, int), Vector2> _cache;

        private VesselAerodynamicModel Model;

        public AeroForceCache(int vRes, int aoaRes, int altRes, VesselAerodynamicModel model)
        {
            Model = model;

            VelocityResolution = vRes;
            AoAResolution =  aoaRes * Mathf.Deg2Rad;
            AltitudeResolution = altRes;
            
            _cache = new Dictionary<(int, int, int), Vector2>();

        }

        public Vector3d GetForce(double velocity, double angleOfAttack, double altitude)
        {
            float vFrac = (float)(velocity / VelocityResolution);
            int vFloor = (int)vFrac;
            vFrac = Mathf.Clamp01(vFrac - (float)vFloor);

            float aFrac = (float)(angleOfAttack / AoAResolution);
            int aFloor = (int)aFrac;
            aFrac = Mathf.Clamp01(aFrac - (float)aFloor);

            float mFrac = (float)(altitude / AltitudeResolution);
            int mFloor = (int)mFrac;
            mFrac = Mathf.Clamp01(mFrac - (float)mFloor);

            //if (Verbose)
            //{
            //    Util.PostSingleScreenMessage("cache cell", "cache cell: [" + vFloor + ", " + aFloor + ", " + mFloor + "]");
            //    Util.PostSingleScreenMessage("altitude cell", "altitude cell: " + altitude + " / " + MaxAltitude + " * " + (double)(InternalArray.GetLength(2) - 1));
            //}

            Vector2 res = Sample3d(vFloor, vFrac, aFloor, aFrac, mFloor, mFrac);
            return Model.UnpackForces(res, altitude, velocity);
        }

        private Vector2 Sample2d(int vFloor, float vFrac, int aFloor, float aFrac, int mFloor)
        {
            Vector2 f00 = GetCachedForce(vFloor, aFloor, mFloor);
            Vector2 f10 = GetCachedForce(vFloor + 1, aFloor, mFloor);

            Vector2 f01 = GetCachedForce(vFloor, aFloor + 1, mFloor);
            Vector2 f11 = GetCachedForce(vFloor + 1, aFloor + 1, mFloor);

            Vector2 f0 = f01 * aFrac + f00 * (1.0f - aFrac);
            Vector2 f1 = f11 * aFrac + f10 * (1.0f - aFrac);

            return f1 * vFrac + f0 * (1.0f - vFrac);
        }

        private Vector2 Sample3d(int vFloor, float vFrac, int aFloor, float aFrac, int mFloor, float mFrac)
        {
            Vector2 f0 = Sample2d(vFloor, vFrac, aFloor, aFrac, mFloor);
            Vector2 f1 = Sample2d(vFloor, vFrac, aFloor, aFrac, mFloor + 1);

            return f1 * mFrac + f0 * (1.0f - mFrac);
        }

        private Vector2 GetCachedForce(int v, int a, int m)
        {

            Vector2 f;
            if (! _cache.TryGetValue((v, a, m), out f))
                f = ComputeCacheEntry(v, a, m);

            return f;
        }

        private Vector2 ComputeCacheEntry(int v, int a, int m)
        {
            Vector3d velocity = v * VelocityResolution * Vector3d.right;
            double AoA = a * AoAResolution;
            double currentAltitude = m * AltitudeResolution;

            Vector2 packedForce = Model.PackForces(Model.ComputeForces(currentAltitude, velocity, new Vector3(0, 1, 0), AoA), currentAltitude, velocity.x);

            _cache.Add((v, a, m), packedForce);

            return packedForce;
        }
    }
}
