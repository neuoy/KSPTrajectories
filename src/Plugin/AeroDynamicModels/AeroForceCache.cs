/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
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
using UnityEngine;

namespace Trajectories
{
    public class AeroForceCache
    {
        public double MaxVelocity { get; private set; }
        public double MaxAoA { get; private set; }
        public double MaxAltitude { get; private set; }

        public int VelocityResolution { get; private set; }
        public int AoAResolution { get; private set; }
        public int AltitudeResolution { get; private set; }

        private Vector2d[,,] InternalArray;

        private AeroDynamicModel Model;

        public AeroForceCache(double maxCacheVelocity, double maxCacheAoA, double atmosphereDepth, int vRes, int aoaRes, int altRes, AeroDynamicModel model)
        {
            Model = model;

            this.MaxVelocity = maxCacheVelocity;
            this.MaxAoA = maxCacheAoA;
            this.MaxAltitude = atmosphereDepth;
            VelocityResolution = vRes;
            AoAResolution = aoaRes;
            AltitudeResolution = altRes;

            InternalArray = new Vector2d[VelocityResolution, AoAResolution, AltitudeResolution];
            for (int v = 0; v < VelocityResolution; ++v)
                for (int a = 0; a < AoAResolution; ++a)
                    for (int m = 0; m < AltitudeResolution; ++m)
                        InternalArray[v, a, m] = new Vector2d(double.NaN, double.NaN);
        }

        public Vector3d GetForce(double velocity, double angleOfAttack, double altitude)
        {
            double vFrac = (velocity / MaxVelocity * (double)(InternalArray.GetLength(0) - 1));
            int vFloor = Math.Max(0, Math.Min(InternalArray.GetLength(0) - 2, (int)vFrac));
            vFrac = Math.Max(0.0f, Math.Min(1.0f, vFrac - (float)vFloor));

            double aFrac = ((angleOfAttack / MaxAoA * 0.5 + 0.5) * (double)(InternalArray.GetLength(1) - 1));
            int aFloor = Math.Max(0, Math.Min(InternalArray.GetLength(1) - 2, (int)aFrac));
            aFrac = Math.Max(0.0f, Math.Min(1.0f, aFrac - (float)aFloor));

            double mFrac = (altitude / MaxAltitude * (double)(InternalArray.GetLength(2) - 1));
            int mFloor = Math.Max(0, Math.Min(InternalArray.GetLength(2) - 2, (int)mFrac));
            mFrac = Math.Max(0.0f, Math.Min(1.0f, mFrac - (float)mFloor));

            //if (Verbose)
            //{
            //    ScreenMessages.PostScreenMessage("cache cell", "cache cell: [" + vFloor + ", " + aFloor + ", " + mFloor + "]");
            //    ScreenMessages.PostScreenMessage("altitude cell", "altitude cell: " + altitude + " / " + MaxAltitude + " * " + (double)(InternalArray.GetLength(2) - 1));
            //}

            Vector2d res = Sample3d(vFloor, vFrac, aFloor, aFrac, mFloor, mFrac);
            return Model.UnpackForces(res, altitude, velocity);
        }

        private Vector2d Sample2d(int vFloor, double vFrac, int aFloor, double aFrac, int mFloor)
        {
            Vector2d f00 = GetCachedForce(vFloor, aFloor, mFloor);
            Vector2d f10 = GetCachedForce(vFloor + 1, aFloor, mFloor);

            Vector2d f01 = GetCachedForce(vFloor, aFloor + 1, mFloor);
            Vector2d f11 = GetCachedForce(vFloor + 1, aFloor + 1, mFloor);

            Vector2d f0 = f01 * aFrac + f00 * (1.0f - aFrac);
            Vector2d f1 = f11 * aFrac + f10 * (1.0f - aFrac);

            return f1 * vFrac + f0 * (1.0f - vFrac);
        }

        private Vector2d Sample3d(int vFloor, double vFrac, int aFloor, double aFrac, int mFloor, double mFrac)
        {
            Vector2d f0 = Sample2d(vFloor, vFrac, aFloor, aFrac, mFloor);
            Vector2d f1 = Sample2d(vFloor, vFrac, aFloor, aFrac, mFloor + 1);

            return f1 * mFrac + f0 * (1.0f - mFrac);
        }

        private Vector2d GetCachedForce(int v, int a, int m)
        {
            Vector2d f = InternalArray[v, a, m];

            if (double.IsNaN(f.x))
                f = ComputeCacheEntry(v, a, m);

            return f;
        }

        private Vector2d ComputeCacheEntry(int v, int a, int m)
        {
            double vel = MaxVelocity * (double)v / (double)(InternalArray.GetLength(0) - 1);
            Vector3d velocity = new Vector3d(vel, 0, 0);
            double AoA = MaxAoA * ((double)a / (double)(InternalArray.GetLength(1) - 1) * 2.0 - 1.0);
            double currentAltitude = MaxAltitude * (double)m / (double)(InternalArray.GetLength(2) - 1);

            Vector2d packedForce = Model.PackForces(Model.ComputeForces(currentAltitude, velocity, new Vector3d(0, 1, 0), AoA), currentAltitude, vel);

            return InternalArray[v, a, m] = packedForce;
        }
    }
}
