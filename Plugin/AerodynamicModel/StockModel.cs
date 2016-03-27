using System;
using UnityEngine;

namespace Trajectories
{
    class StockModel : VesselAerodynamicModel
    {
        public override string AerodynamicModelName { get { return "Stock"; } }

        public StockModel(Vessel ship, CelestialBody body) : base(ship, body) { }

        protected override Vector3d ComputeForces_Model(Vector3d airVelocity, double altitude, double absoluteVelocity)
        {
            return ApplyForceCorrection((Vector3d)StockAeroUtil.SimAeroForce(vessel_, (Vector3)airVelocity, altitude), altitude, absoluteVelocity);
        }

        protected override Vector3d GetForces_Model(CelestialBody body, Vector3d bodySpacePosition, Vector3d airVelocity, double angleOfAttack, bool useCache)
        {
            Vector3d position = body.position + bodySpacePosition;

            double altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
            {
                return Vector3d.zero;
            }

            if (!useCache)
                return ComputeForces(altitude, airVelocity, new Vector3(0, 1, 0), angleOfAttack);
            //double approxMachNumber = useNEAR ? 0.0 : (double)FARAeroUtil_GetMachNumber.Invoke(null, new object[] { body_, body.maxAtmosphereAltitude * 0.5, new Vector3d((float)airVelocity.magnitude, 0, 0) });
            //Util.PostSingleScreenMessage("machNum", "machNumber = " + actualMachNumber + " ; approx machNumber = " + approxMachNumber);

            Vector2 force = cachedForces.GetForce(airVelocity.magnitude, angleOfAttack, altitude);
            force = ReverseForceCorrection(force, altitude, airVelocity.magnitude);

            // adjust force using the more accurate air density that we can compute knowing where the vessel is relatively to the sun and body
            double preciseRho = StockAeroUtil.GetDensity(position, body);
            double approximateRho = StockAeroUtil.GetDensity(altitude, body);
            if (approximateRho > 0)
                force = force * (float)(preciseRho / approximateRho);

            Vector3d forward = airVelocity.normalized;
            Vector3d right = Vector3d.Cross(forward, bodySpacePosition).normalized;
            Vector3d up = Vector3d.Cross(right, forward).normalized;

            return forward * force.x + up * force.y;
        }

        private Vector3d ApplyForceCorrection(Vector3d res, double currentAltitude, double velocity)
        {
            double rho = StockAeroUtil.GetDensity(currentAltitude, body_);
            if (rho < 0.0000000001)
                return new Vector3d(0, 0);
            double invScale = 1.0 / (rho * Math.Max(1.0, velocity * velocity)); // divide by v² and rho before storing the force, to increase accuracy (the reverse operation is performed when reading from the cache)
            return res * invScale;
        }

        private Vector2 ReverseForceCorrection(Vector2 res, double altitudeAboveSea, double velocity)
        {
            double rho = StockAeroUtil.GetDensity(altitudeAboveSea, body_);
            res = res * (float)(velocity * velocity * rho);

            return res;
        }
    }
}
