using System;
using System.Reflection;
using UnityEngine;

namespace Trajectories
{
    class FARModel : VesselAerodynamicModel
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
