/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
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
using System.Reflection;
using UnityEngine;

namespace Trajectories
{
    class FARModel: VesselAerodynamicModel
    {
        private MethodInfo FARAPI_CalculateVesselAeroForces;

        public override string AerodynamicModelName { get { return "FAR"; } }

        public FARModel(Vessel ship, CelestialBody body, MethodInfo CalculateVesselAeroForces)
            : base(ship, body)
        {
            FARAPI_CalculateVesselAeroForces = CalculateVesselAeroForces;
        }

        protected override Vector3d ComputeForces_Model(Vector3d airVelocity, double altitude)
        {
            //Debug.Log("Trajectories: getting FAR forces");
            if (vessel_ == null || vessel_.packed)
                return Vector3.zero;

            if (airVelocity.x == 0d || airVelocity.y == 0d || airVelocity.z == 0d)
            {
                //Debug.LogWarning(string.Format("Trajectories: Getting FAR forces - Velocity: {0} | Altitude: {1}", airVelocity, altitude));
                return Vector3.zero;
            }

            Vector3 worldAirVel = new Vector3((float)airVelocity.x, (float)airVelocity.y, (float)airVelocity.z);
            var parameters = new object[] { vessel_, new Vector3(), new Vector3(), worldAirVel, altitude };
            FARAPI_CalculateVesselAeroForces.Invoke(null, parameters);
            return (Vector3)parameters[1];
        }

        public override Vector2 PackForces(Vector3d forces, double altitudeAboveSea, double velocity)
        {
            double rho = StockAeroUtil.GetDensity(altitudeAboveSea, body_); // would be even better to use FAR method of computing the air density (which also depends on velocity), but this is already better than nothing

            if (rho < 0.0000000001)
                return new Vector2(0, 0);
            double invScale = 1.0 / (rho * Math.Max(1.0, velocity * velocity));
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
