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

        protected override Vector3d ComputeForces_Model(Vector3d airVelocity, double altitude, double absoluteVelocity)
        {
            //Debug.Log("Trajectories: getting FAR forces");
            Vector3 worldAirVel = new Vector3((float)airVelocity.x, (float)airVelocity.y, (float)airVelocity.z);
            var parameters = new object[] { vessel_, new Vector3(), new Vector3(), worldAirVel, altitude };
            FARAPI_CalculateVesselAeroForces.Invoke(null, parameters);
            return (Vector3)parameters[1];
        }

        protected override Vector3d GetForces_Model(CelestialBody body, Vector3d bodySpacePosition, Vector3d airVelocity, double angleOfAttack, bool useCache)
        {
            double altitudeAboveSea = bodySpacePosition.magnitude - body.Radius;

            if (!useCache)
                return ComputeForces(altitudeAboveSea, airVelocity, bodySpacePosition, angleOfAttack);
            //double approxMachNumber = useNEAR ? 0.0 : (double)FARAeroUtil_GetMachNumber.Invoke(null, new object[] { body_, body.maxAtmosphereAltitude * 0.5, new Vector3d((float)airVelocity.magnitude, 0, 0) });
            //Util.PostSingleScreenMessage("machNum", "machNumber = " + actualMachNumber + " ; approx machNumber = " + approxMachNumber);

            Vector2 force = cachedForces.GetForce(airVelocity.magnitude, angleOfAttack, altitudeAboveSea);

            Vector3d forward = airVelocity.normalized;
            Vector3d right = Vector3d.Cross(forward, bodySpacePosition).normalized;
            Vector3d up = Vector3d.Cross(right, forward).normalized;

            return forward * force.x + up * force.y;
        }
    }
}
