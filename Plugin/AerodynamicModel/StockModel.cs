using System;
using UnityEngine;

namespace Trajectories
{
    class StockModel : VesselAerodynamicModel
    {
        public override string AerodynamicModelName { get { return "Stock"; } }

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
