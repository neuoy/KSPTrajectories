/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 S.Gray, (aka PiezPiedPy).

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
    class StockModel: VesselAerodynamicModel
    {
        public override string AerodynamicModelName { get { return Localizer.Format("#autoLOC_Trajectories_Stock"); } }

        public StockModel(Vessel ship, CelestialBody body) : base(ship, body) { }

        protected override Vector3d ComputeForces_Model(Vector3d airVelocity, double altitude)
        {
            return (Vector3d)StockAeroUtil.SimAeroForce(vessel_, (Vector3)airVelocity, altitude);
        }

        public override Vector2 PackForces(Vector3d forces, double altitudeAboveSea, double velocity)
        {
            double rho = StockAeroUtil.GetDensity(altitudeAboveSea, body_);
            if (rho < 0.0000000001)
                return new Vector2(0, 0);
            double invScale = 1.0 / (rho * Math.Max(1.0, velocity * velocity)); // divide by v² and rho before storing the force, to increase accuracy (the reverse operation is performed when reading from the cache)
            forces *= invScale;
            return new Vector2((float)forces.x, (float)forces.y);
        }

        public override Vector3d UnpackForces(Vector2 packedForces, double altitudeAboveSea, double velocity)
        {
            double rho = StockAeroUtil.GetDensity(altitudeAboveSea, body_);
            double scale = velocity * velocity * rho;

            return new Vector3d((double)packedForces.x * scale, (double)packedForces.y * scale, 0.0);
        }
    }
}
