/*
  Copyright© (c) 2015-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).
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

// StockAeroUtil by atomicfury.

//#define PRECOMPUTE_CACHE
using System;
using UnityEngine;

namespace Trajectories
{
    /// <summary> Provides several methods to access stock aero information </summary>
    public static class StockAeroUtil
    {
        #region METHODS_TAKING_BODY_PARAMETER
        /// <returns>
        /// This function should return exactly the same value as Vessel.atmDensity,
        ///  but is more generic because you don't need an actual vessel updated by KSP to get a value at the desired location.
        /// Computations are performed for the current body position, which means it's theoretically wrong if you want to know the temperature in the future,
        ///  but since body rotation is not used (position is given in sun frame), you should get accurate results up to a few weeks.
        /// </returns>
        /// <param name="position"></param>
        /// <param name="body"></param>
        public static double GetTemperature(Vector3d position, CelestialBody body)
        {
            if (!body.atmosphere)
                return PhysicsGlobals.SpaceTemperature;

            double altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
                return PhysicsGlobals.SpaceTemperature;

            Vector3d up = (position - body.position).normalized;
            float polarAngle = (float)Math.Acos(Vector3d.Dot(body.bodyTransform.up, up));
            if (polarAngle > Util.HALF_PI)
                polarAngle = Mathf.PI - polarAngle;

            float time = ((float)Util.HALF_PI - polarAngle) * Mathf.Rad2Deg;

            Vector3d sunVector = (FlightGlobals.Bodies[0].position - position).normalized;
            double sunAxialDot = Vector3d.Dot(sunVector, body.bodyTransform.up);
            double bodyPolarAngle = Math.Acos(Vector3d.Dot(body.bodyTransform.up, up));
            double sunPolarAngle = Math.Acos(sunAxialDot);
            double sunBodyMaxDot = (1d + Math.Cos(sunPolarAngle - bodyPolarAngle)) * 0.5d;
            double sunBodyMinDot = (1d + Math.Cos(sunPolarAngle + bodyPolarAngle)) * 0.5d;
            double sunDotCorrected = (1d + Vector3d.Dot(
                sunVector,
                Quaternion.AngleAxis(45f * Math.Sign(body.rotationPeriod), body.bodyTransform.up) * up))
                * 0.5d;
            double sunDotNormalized = (sunDotCorrected - sunBodyMinDot) / (sunBodyMaxDot - sunBodyMinDot);
            double atmosphereTemperatureOffset = body.latitudeTemperatureBiasCurve.Evaluate(time)
                + body.latitudeTemperatureSunMultCurve.Evaluate(time) * sunDotNormalized
                + body.axialTemperatureSunMultCurve.Evaluate((float)sunAxialDot);
            double temperature = body.GetTemperature(altitude)
                + body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;

            return temperature;
        }

        /// <returns>
        /// The air density (rho) for the specified altitude on the specified body.
        /// This is an approximation, because actual calculations, taking sun exposure into account to compute air temperature,
        ///  require to know the actual point on the body where the density is to be computed (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference for trajectory prediction.
        /// </returns>
        /// <param name="altitude">Altitude above sea level (in meters)</param>
        /// <param name="body"></param>
        public static double GetDensity(double altitude, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0d;

            if (altitude > body.atmosphereDepth)
                return 0d;

            double pressure = body.GetPressure(altitude);

            // get an average day/night temperature at the equator
            double atmosphereTemperatureOffset = body.latitudeTemperatureBiasCurve.Evaluate(0f)
                + body.latitudeTemperatureSunMultCurve.Evaluate(0f) * 0.5d
                + body.axialTemperatureSunMultCurve.Evaluate(0f);
            double temperature = // body.GetFullTemperature(altitude, atmosphereTemperatureOffset);
                body.GetTemperature(altitude) + body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;


            return body.GetDensity(pressure, temperature);
        }

        /// <returns>
        /// The air density (rho) for the specified altitude on the specified body.
        /// This is an approximation, because actual calculations, taking sun exposure into account to compute air temperature,
        ///  require to know the actual point on the body where the density is to be computed (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference for trajectory prediction.
        /// </returns>
        /// <param name="position">position above sea level (in meters)</param>
        /// <param name="body"></param>
        public static double GetDensity(Vector3d position, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0d;

            double altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
                return 0d;

            double pressure = body.GetPressure(altitude);
            double temperature = GetTemperature(position, body);   // body.GetFullTemperature(position);

            return body.GetDensity(pressure, temperature);
        }
        #endregion

        #region METHODS_USING_GAMEDATACACHE_BODY
        /// <returns>
        /// This function should return exactly the same value as Vessel.atmDensity,
        ///  but is more generic because you don't need an actual vessel updated by KSP to get a value at the desired location.
        /// Computations are performed for the GameDataCache body position, which means it's theoretically wrong if you want to know the temperature in the future,
        ///  but since body rotation is not used (position is given in sun frame), you should get accurate results up to a few weeks.
        /// </returns>
        /// <param name="position"></param>
        public static double GetTemperature(Vector3d position)
        {
            GameDataCache.BodyInfo body = GameDataCache.VesselBodyInfo;

            if (!body.BodyHasAtmosphere)
                return PhysicsGlobals.SpaceTemperature;

            double altitude = (position - body.BodyWorldPos).magnitude - body.BodyRadius;
            if (altitude > body.BodyAtmosphereDepth)
                return PhysicsGlobals.SpaceTemperature;

            Vector3d up = (position - body.BodyWorldPos).normalized;
            float polarAngle = (float)Math.Acos(Vector3d.Dot(body.BodyTransformUp, up));
            if (polarAngle > Util.HALF_PI)
                polarAngle = Mathf.PI - polarAngle;

            float time = ((float)Util.HALF_PI - polarAngle) * Mathf.Rad2Deg;

            Vector3d sunVector = (GameDataCache.SunWorldPos - position).normalized;
            double sunAxialDot = Vector3d.Dot(sunVector, body.BodyTransformUp);
            double bodyPolarAngle = Math.Acos(Vector3d.Dot(body.BodyTransformUp, up));
            double sunPolarAngle = Math.Acos(sunAxialDot);
            double sunBodyMaxDot = (1d + Math.Cos(sunPolarAngle - bodyPolarAngle)) * 0.5d;
            double sunBodyMinDot = (1d + Math.Cos(sunPolarAngle + bodyPolarAngle)) * 0.5d;
            double sunDotCorrected = (1d + Vector3d.Dot(
                sunVector,
                Quaternion.AngleAxis(45f * Math.Sign(body.BodyRotationPeriod), body.BodyTransformUp) * up))
                * 0.5d;
            double sunDotNormalized = (sunDotCorrected - sunBodyMinDot) / (sunBodyMaxDot - sunBodyMinDot);

            // todo: change to use BodyInfo

            double atmosphereTemperatureOffset = GameDataCache.VesselBody.latitudeTemperatureBiasCurve.Evaluate(time)
                + GameDataCache.VesselBody.latitudeTemperatureSunMultCurve.Evaluate(time) * sunDotNormalized
                + GameDataCache.VesselBody.axialTemperatureSunMultCurve.Evaluate((float)sunAxialDot);
            double temperature = GameDataCache.VesselBody.GetTemperature(altitude) +
                GameDataCache.VesselBody.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;

            return temperature;
        }
        /// <returns>
        /// The air density (rho) for the specified altitude on the GameDataCache body.
        /// This is an approximation, because actual calculations, taking sun exposure into account to compute air temperature,
        ///  require to know the actual point on the body where the density is to be computed (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference for trajectory prediction.
        /// </returns>
        /// <param name="altitude">Altitude above sea level (in meters)</param>
        public static double GetDensity(double altitude)
        {
            if (!GameDataCache.VesselBodyInfo.BodyHasAtmosphere)
                return 0d;

            if (altitude > GameDataCache.VesselBodyInfo.BodyAtmosphereDepth)
                return 0d;

            // todo: change to use BodyInfo

            double pressure = GameDataCache.VesselBody.GetPressure(altitude);

            // get an average day/night temperature at the equator
            double temperature = // body.GetFullTemperature(altitude, atmosphereTemperatureOffset);
                GameDataCache.VesselBody.GetTemperature(altitude) +
                GameDataCache.VesselBody.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * GameDataCache.VesselBodyInfo.BodyAtmosTempOffset;


            return GameDataCache.VesselBody.GetDensity(pressure, temperature);
        }

        /// <returns>
        /// The air density (rho) for the specified altitude on the GameDataCache body.
        /// This is an approximation, because actual calculations, taking sun exposure into account to compute air temperature,
        ///  require to know the actual point on the body where the density is to be computed (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference for trajectory prediction.
        /// </returns>
        /// <param name="position">position above sea level (in meters)</param>
        public static double GetDensity(Vector3d position)
        {
            GameDataCache.BodyInfo body = GameDataCache.VesselBodyInfo;

            if (!body.BodyHasAtmosphere)
                return 0d;

            double altitude = (position - body.BodyWorldPos).magnitude - body.BodyRadius;
            if (altitude > body.BodyAtmosphereDepth)
                return 0d;

            double pressure = GameDataCache.VesselBody.GetPressure(altitude);    // todo: change to use BodyInfo
            double temperature = GetTemperature(position);   // body.GetFullTemperature(position);

            return GameDataCache.VesselBody.GetDensity(pressure, temperature);     // todo: change to use BodyInfo
        }
        #endregion

        /// <returns> The calculated aerodynamic forces vector (lift and drag) for the GameDataCache vessel with the given velocity and position parameters </returns>
        public static Vector3d SimAeroForce(Vector3d v_wrld_vel, Vector3d position)
        {
            double latitude = GameDataCache.VesselBody.GetLatitude(position) / 180d * Math.PI;
            double altitude = (position - GameDataCache.VesselBodyInfo.BodyWorldPos).magnitude - GameDataCache.VesselBodyInfo.BodyRadius;

            return SimAeroForce(v_wrld_vel, altitude, latitude);
        }

        /// <returns> The calculated aerodynamic forces vector (lift and drag) for the GameDataCache vessel with the given velocity, altitude and latitude parameters </returns>
        public static Vector3d SimAeroForce(Vector3d v_wrld_vel, double altitude, double latitude = 0d)
        {
            Profiler.Start("SimAeroForce");

            double rho = GetDensity(altitude);
            if (rho <= 0d)
                return Vector3d.zero;

            // dynamic pressure for standard drag equation
            double dyn_pressure = 0.0005d * rho * v_wrld_vel.sqrMagnitude;
            double pseudoredragmult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate((float)(rho * Math.Abs(v_wrld_vel.magnitude)));

            double mach = v_wrld_vel.magnitude / GameDataCache.VesselBody.GetSpeedOfSound(GameDataCache.VesselBody.GetPressure(altitude), rho);
            if (mach > 25d)
                mach = 25d;

            // Lift and drag for force accumulation.
            Vector3d total_lift = Vector3d.zero;
            Vector3d total_drag = Vector3d.zero;

            Vector3d sim_dragVectorDir = v_wrld_vel.normalized;

            // Loop through all parts, accumulating drag and lift.
            int part_index = -1;
            foreach (GameDataCache.PartInfo part_info in GameDataCache.VesselParts)
            {
                part_index++;

#if DEBUG
                TrajectoriesDebug partDebug = AerodynamicModel.DebugParts ? part_info.Part.FindModuleImplementing<TrajectoriesDebug>() : null;
                if (partDebug != null)
                {
                    partDebug.Drag = 0;
                    partDebug.Lift = 0;
                }
#endif

                // need checks on shielded components
                if (part_info.ShieldedFromAirstream || !part_info.HasRigidbody)
                    continue;

                #region CALCULATE_BODY_DRAG
                // get body drag
                Vector3d body_lift = Vector3d.zero;
                Vector3d body_drag = Vector3d.zero;

                Profiler.Start("SimAeroForce#BodyDrag");
                switch (part_info.DragModel)
                {
                    case Part.DragModel.DEFAULT:
                    case Part.DragModel.CUBE:
                        DragCubeList.CubeData drag_data = new DragCubeList.CubeData();

                        double drag;
                        if (!part_info.HasCubes) // since 1.0.5, some parts don't have drag cubes (for example fuel lines and struts)
                        {
                            drag = part_info.MaxDrag;
                        }
                        else
                        {
                            // Vector3d sim_dragVectorDirLocal = -part_transform.InverseTransformDirection(sim_dragVectorDir);   // Not thread safe
                            Vector3d sim_dragVectorDirLocal = -(part_info.Rotation.Inverse() * sim_dragVectorDir);
                            try
                            {
                                // implement manually, sometimes drag_data.areaDrag is NaN ??
                                part_info.DragCubes?.AddSurfaceDragDirection(-sim_dragVectorDirLocal, (float)mach, ref drag_data);
                            }
                            catch (Exception e)
                            {
                                part_info.DragCubes?.SetDrag(sim_dragVectorDirLocal, (float)mach);
                                part_info.DragCubes?.ForceUpdate(true, true);
                                part_info.DragCubes?.AddSurfaceDragDirection(-sim_dragVectorDirLocal, (float)mach, ref drag_data);
                                Util.DebugLogError("Exception {0} on drag initialization", e);
                            }

                            drag = !drag_data.areaDrag.IsNaN() ? drag_data.areaDrag * PhysicsGlobals.DragCubeMultiplier * pseudoredragmult : part_info.MaxDrag;
                            body_lift = drag_data.liftForce;
                        }

                        double sim_dragScalar = dyn_pressure * drag * PhysicsGlobals.DragMultiplier;
                        body_drag = -sim_dragVectorDir * sim_dragScalar;

                        break;

                    case Part.DragModel.SPHERICAL:
                        body_drag = -sim_dragVectorDir * part_info.MaxDrag;
                        break;

                    case Part.DragModel.CYLINDRICAL:
                        body_drag = -sim_dragVectorDir * Util.Lerp(part_info.MinDrag, part_info.MaxDrag,
                            Math.Abs(Vector3d.Dot(part_info.Rotation * part_info.DragVector, sim_dragVectorDir)));
                        break;

                    case Part.DragModel.CONIC:
                        body_drag = -sim_dragVectorDir * Util.Lerp(part_info.MinDrag, part_info.MaxDrag,
                            Vector3d.Angle(part_info.Rotation * part_info.DragVector, sim_dragVectorDir) / 180d);
                        break;

                    default:
                        // no drag to apply
                        body_drag = Vector3d.zero;
                        break;
                }

                total_drag += body_drag;

                Profiler.Stop("SimAeroForce#BodyDrag");

#if DEBUG
                if (partDebug != null)
                {
                    partDebug.Drag += (float)body_drag.magnitude;
                }
#endif
                #endregion CALCULATE_BODY_DRAG

                #region CALCULATE_BODY_LIFT
                // get body lift
                if (!part_info.HasLiftModule)
                {
                    Profiler.Start("SimAeroForce#BodyLift");

                    double simbodyLiftScalar = part_info.BodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier * dyn_pressure;
                    simbodyLiftScalar *= PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift").liftMachCurve.Evaluate((float)mach);
                    total_lift += Vector3.ProjectOnPlane(part_info.Rotation * (simbodyLiftScalar * body_lift), sim_dragVectorDir);

                    Profiler.Stop("SimAeroForce#BodyLift");
                }
                #endregion CALCULATE_BODY_LIFT

                #region CALCULATE_WINGS_DRAG_AND_LIFT
                // calculate lift force and drag for any wings.
                // Should catch control surface as it is a subclass
                Profiler.Start("SimAeroForce#LiftingSurfaces");

                foreach (GameDataCache.WingInfo wing in part_info.Wings)
                {
                    double liftQ = dyn_pressure * 1e3;
                    Vector3d liftVector = Vector3d.zero;
                    double absdot;

                    // wing.SetupCoefficients(v_wrld_vel, out nVel, out liftVector, out liftdot, out absdot);   // Not thread safe
                    #region SETUP_COEFFICIENTS
                    switch (wing.TransformDir)
                    {
                        case ModuleLiftingSurface.TransformDir.X:
                            liftVector = part_info.TransformRight;
                            break;
                        case ModuleLiftingSurface.TransformDir.Y:
                            liftVector = part_info.TransformUp;
                            break;
                        case ModuleLiftingSurface.TransformDir.Z:
                            liftVector = part_info.TransformForward;
                            break;
                    }
                    Vector3d nVel = v_wrld_vel.normalized;
                    liftVector *= wing.TransformSign;
                    double liftdot = Vector3d.Dot(nVel, liftVector);
                    if (wing.OmniDirectional)
                    {
                        absdot = Math.Abs(liftdot);
                    }
                    else
                    {
                        absdot = Util.Clamp01(liftdot);
                    }
                    #endregion SETUP_COEFFICIENTS

                    //Vector3d wing_lift = wing.GetLiftVector(liftVector, (float)liftdot, (float)absdot, liftQ, (float)mach);   // Not thread safe
                    //Vector3d wing_drag = wing.GetDragVector(nVel, (float)absdot, liftQ);   // Not thread safe

                    Vector3d wing_lift = wing.GetLiftVector(liftVector, liftdot, absdot, liftQ, mach);
                    Vector3d wing_drag = wing.GetDragVector(nVel, absdot, liftQ, mach);

                    total_lift += wing_lift;
                    total_drag += wing_drag;

#if DEBUG
                    if (partDebug != null)
                    {
                        partDebug.Lift += (float)wing_lift.magnitude;
                        partDebug.Drag += (float)wing_drag.magnitude;
                    }
#endif
                }

                Profiler.Stop("SimAeroForce#LiftingSurfaces");
                #endregion CALCULATE_WINGS_DRAG_AND_LIFT
            }

            Profiler.Stop("SimAeroForce");

            return total_lift + total_drag;
        }

        private static Vector3d GetLiftVector(this GameDataCache.WingInfo wing, Vector3d liftVector, double liftdot, double absdot, double Q, double mach)
        {
            if (wing.HasPartAttached)
                return Vector3d.zero;

            double lift_scalar = (Math.Sign(liftdot) * wing.LiftCurve.Evaluate((float)absdot) * wing.LiftMachCurve.Evaluate((float)mach)) * wing.DeflectionLiftCoeff;

            if (lift_scalar != 0d && !lift_scalar.IsNaN())
            {
                lift_scalar = Q * (lift_scalar * PhysicsGlobals.LiftMultiplier);

                if (wing.PerpendicularOnly)
                    return Vector3.ProjectOnPlane(-liftVector * lift_scalar, -wing.VelocityNormal);

                return -liftVector * lift_scalar;
            }
            return Vector3d.zero;
        }

        private static Vector3d GetDragVector(this GameDataCache.WingInfo wing, Vector3d nVel, double absdot, double Q, double mach)
        {
            if (wing.HasPartAttached)
                return Vector3d.zero;

            double drag_scalar = (wing.DragCurve.Evaluate((float)absdot) * wing.DragMachCurve.Evaluate((float)mach)) * wing.DeflectionLiftCoeff;

            if (drag_scalar != 0d && !drag_scalar.IsNaN())
                return -nVel * (Q * (drag_scalar * PhysicsGlobals.LiftDragMultiplier));

            return Vector3d.zero;
        }
    } //StockAeroUtil
}

