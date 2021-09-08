﻿/*
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
using KSP.Localization;
using UnityEngine;

namespace Trajectories
{
    internal class StockModel : AerodynamicModel
    {
        internal override string AerodynamicModelName => Localizer.Format("#autoLOC_Trajectories_Stock");

        internal StockModel() : base() { }

        protected override Vector3d ComputeForces_Model(Vector3d airVelocity, double altitude) => StockAeroUtil.SimAeroForce(airVelocity, altitude);

        internal override Vector2d PackForces(Vector3d forces, double altitudeAboveSea, double velocity)
        {
            // would be even better to use FAR method of computing the air density (which also depends on velocity), but this is already better than nothing
            double rho = StockAeroUtil.GetDensity(altitudeAboveSea);

            if (rho < 0.0000000001d)
                return Vector2d.zero;

            // divide by v² and rho before packing the force, to increase accuracy (the reverse operation is performed when reading from the cache)
            forces *= 1d / (rho * Math.Max(1d, velocity * velocity));
            return new Vector2d(forces.x, forces.y);
        }

        internal override Vector3d UnpackForces(Vector2d packedForces, double altitudeAboveSea, double velocity)
        {
            double rho = StockAeroUtil.GetDensity(altitudeAboveSea);

            packedForces *= velocity * velocity * rho;
            return new Vector3d(packedForces.x, packedForces.y, 0d);
        }
    }
}
