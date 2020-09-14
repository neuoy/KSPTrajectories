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
            if (!GameDataCache.BodyHasAtmosphere)
                return PhysicsGlobals.SpaceTemperature;

            double altitude = (position - GameDataCache.BodyWorldPos).magnitude - GameDataCache.BodyRadius;
            if (altitude > GameDataCache.BodyAtmosphereDepth)
                return PhysicsGlobals.SpaceTemperature;

            Vector3d up = (position - GameDataCache.BodyWorldPos).normalized;
            float polarAngle = (float)Math.Acos(Vector3d.Dot(GameDataCache.BodyTransformUp, up));
            if (polarAngle > Util.HALF_PI)
                polarAngle = Mathf.PI - polarAngle;

            float time = ((float)Util.HALF_PI - polarAngle) * Mathf.Rad2Deg;

            Vector3d sunVector = (GameDataCache.SunWorldPos - position).normalized;
            double sunAxialDot = Vector3d.Dot(sunVector, GameDataCache.BodyTransformUp);
            double bodyPolarAngle = Math.Acos(Vector3d.Dot(GameDataCache.BodyTransformUp, up));
            double sunPolarAngle = Math.Acos(sunAxialDot);
            double sunBodyMaxDot = (1d + Math.Cos(sunPolarAngle - bodyPolarAngle)) * 0.5d;
            double sunBodyMinDot = (1d + Math.Cos(sunPolarAngle + bodyPolarAngle)) * 0.5d;
            double sunDotCorrected = (1d + Vector3d.Dot(
                sunVector,
                Quaternion.AngleAxis(45f * Math.Sign(GameDataCache.BodyRotationPeriod), GameDataCache.BodyTransformUp) * up))
                * 0.5d;
            double sunDotNormalized = (sunDotCorrected - sunBodyMinDot) / (sunBodyMaxDot - sunBodyMinDot);
            double atmosphereTemperatureOffset = GameDataCache.Body.latitudeTemperatureBiasCurve.Evaluate(time)
                + GameDataCache.Body.latitudeTemperatureSunMultCurve.Evaluate(time) * sunDotNormalized
                + GameDataCache.Body.axialTemperatureSunMultCurve.Evaluate((float)sunAxialDot);
            double temperature = GameDataCache.Body.GetTemperature(altitude) +
                GameDataCache.Body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;

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
            if (!GameDataCache.BodyHasAtmosphere)
                return 0d;

            if (altitude > GameDataCache.BodyAtmosphereDepth)
                return 0d;

            double pressure = GameDataCache.Body.GetPressure(altitude);

            // get an average day/night temperature at the equator
            double temperature = // body.GetFullTemperature(altitude, atmosphereTemperatureOffset);
                GameDataCache.Body.GetTemperature(altitude) +
                GameDataCache.Body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * GameDataCache.BodyAtmosTempOffset;


            return GameDataCache.Body.GetDensity(pressure, temperature);
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
            if (!GameDataCache.BodyHasAtmosphere)
                return 0d;

            double altitude = (position - GameDataCache.BodyWorldPos).magnitude - GameDataCache.BodyRadius;
            if (altitude > GameDataCache.BodyAtmosphereDepth)
                return 0d;

            double pressure = GameDataCache.Body.GetPressure(altitude);
            double temperature = GetTemperature(position);   // body.GetFullTemperature(position);

            return GameDataCache.Body.GetDensity(pressure, temperature);
        }
        #endregion

        //*******************************************************
        public static Vector3d SimAeroForce(Vector3d v_wrld_vel, Vector3d position)
        {
            double latitude = GameDataCache.Body.GetLatitude(position) / 180d * Math.PI;
            double altitude = (position - GameDataCache.BodyWorldPos).magnitude - GameDataCache.BodyRadius;

            return SimAeroForce(v_wrld_vel, altitude, latitude);
        }

        //*******************************************************
        public static Vector3d SimAeroForce(Vector3d v_wrld_vel, double altitude, double latitude = 0.0)
        {
            Profiler.Start("SimAeroForce");

            CelestialBody body = GameDataCache.Body;

            double rho = GetDensity(altitude);
            if (rho <= 0d)
                return Vector3d.zero;

            // dynamic pressure for standard drag equation
            double dyn_pressure = 0.0005d * rho * v_wrld_vel.sqrMagnitude;

            double mach = v_wrld_vel.magnitude / body.GetSpeedOfSound(body.GetPressure(altitude), rho);
            if (mach > 25d)
                mach = 25d;

            // Lift and drag for force accumulation.
            Vector3d total_lift = Vector3d.zero;
            Vector3d total_drag = Vector3d.zero;

            // Loop through all parts, accumulating drag and lift.
            int part_index = -1;
            foreach (Part p in GameDataCache.VesselParts)
            {
                part_index++;

#if DEBUG
                TrajectoriesDebug partDebug = VesselAerodynamicModel.DebugParts ? p.FindModuleImplementing<TrajectoriesDebug>() : null;
                if (partDebug != null)
                {
                    partDebug.Drag = 0;
                    partDebug.Lift = 0;
                }
#endif

                // need checks on shielded components
                if (p.ShieldedFromAirstream || p.Rigidbody == null)
                    continue;

                // get drag
                Vector3d sim_dragVectorDir = v_wrld_vel.normalized;

                Vector3d liftForce = Vector3d.zero;
                Vector3d dragForce;

                Profiler.Start("SimAeroForce#drag");
                switch (p.dragModel)
                {
                    case Part.DragModel.DEFAULT:
                    case Part.DragModel.CUBE:
                        DragCubeList cubes = p.DragCubes;

                        DragCubeList.CubeData p_drag_data = new DragCubeList.CubeData();

                        //Vector3d sim_dragVectorDirLocal = -part_transform.InverseTransformDirection(sim_dragVectorDir);   // Not thread safe
                        // temporary until I get a better InverseTransformDirection workaround
                        Vector3d sim_dragVectorDirLocal = -(GameDataCache.PartRotations[part_index].Inverse() * sim_dragVectorDir);

                        double drag;
                        if (cubes.None) // since 1.0.5, some parts don't have drag cubes (for example fuel lines and struts)
                        {
                            drag = p.maximum_drag;
                        }
                        else
                        {
                            try
                            {
                                cubes.AddSurfaceDragDirection(-sim_dragVectorDirLocal, (float)mach, ref p_drag_data);
                            }
                            catch (Exception e)
                            {
                                cubes.SetDrag(sim_dragVectorDirLocal, (float)mach);
                                cubes.ForceUpdate(true, true);
                                cubes.AddSurfaceDragDirection(-sim_dragVectorDirLocal, (float)mach, ref p_drag_data);
                                Util.DebugLogError("Exception {0} on drag initialization", e);
                            }

                            double pseudoreynolds = rho * Math.Abs(v_wrld_vel.magnitude);
                            double pseudoredragmult = PhysicsGlobals.DragCurvePseudoReynolds.Evaluate((float)pseudoreynolds);
                            drag = p_drag_data.areaDrag * PhysicsGlobals.DragCubeMultiplier * pseudoredragmult;

                            liftForce = p_drag_data.liftForce;
                        }

                        double sim_dragScalar = dyn_pressure * drag * PhysicsGlobals.DragMultiplier;
                        dragForce = -sim_dragVectorDir * sim_dragScalar;

                        break;

                    case Part.DragModel.SPHERICAL:
                        dragForce = -sim_dragVectorDir * p.maximum_drag;
                        break;

                    case Part.DragModel.CYLINDRICAL:
                        dragForce = -sim_dragVectorDir * Util.Lerp(p.minimum_drag, p.maximum_drag, Math.Abs(Vector3d.Dot(GameDataCache.PartRotations[part_index] * p.dragReferenceVector, sim_dragVectorDir)));
                        break;

                    case Part.DragModel.CONIC:
                        dragForce = -sim_dragVectorDir * Util.Lerp(p.minimum_drag, p.maximum_drag, Vector3d.Angle(GameDataCache.PartRotations[part_index] * p.dragReferenceVector, sim_dragVectorDir) / 180d);
                        break;

                    default:
                        // no drag to apply
                        dragForce = Vector3d.zero;
                        break;
                }

                Profiler.Stop("SimAeroForce#drag");

#if DEBUG
                if (partDebug != null)
                {
                    partDebug.Drag += (float)dragForce.magnitude;
                }
#endif
                total_drag += dragForce;

                // If it isn't a wing or lifter, get body lift.
                if (!p.hasLiftModule)
                {
                    Profiler.Start("SimAeroForce#BodyLift");

                    float simbodyLiftScalar = p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier * (float)dyn_pressure;
                    simbodyLiftScalar *= PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift").liftMachCurve.Evaluate((float)mach);
                    Vector3 bodyLift = GameDataCache.PartRotations[part_index] * (simbodyLiftScalar * liftForce);
                    bodyLift = Vector3.ProjectOnPlane(bodyLift, sim_dragVectorDir);
                    // Only accumulate forces for non-LiftModules
                    total_lift += bodyLift;


                    Profiler.Stop("SimAeroForce#BodyLift");
                }


                Profiler.Start("SimAeroForce#LiftingSurface");

                // Find ModuleLifingSurface for wings and lift force.
                // Should catch control surface as it is a subclass
                foreach (PartModule m in p.Modules)
                {
                    double mcs_mod;
                    if (m is ModuleLiftingSurface)
                    {
                        ModuleLiftingSurface wing = (ModuleLiftingSurface)m;
                        mcs_mod = 1.0d;
                        double liftQ = dyn_pressure * 1000d;
                        Vector3d liftVector = Vector3d.zero;
                        double absdot;

                        //wing.SetupCoefficients(v_wrld_vel, out nVel, out liftVector, out liftdot, out absdot);
                        #region SETUP_COEFFICIENTS
                        switch (wing.transformDir)
                        {
                            case ModuleLiftingSurface.TransformDir.X:
                                liftVector = GameDataCache.PartTransformsRight[part_index];
                                break;
                            case ModuleLiftingSurface.TransformDir.Y:
                                liftVector = GameDataCache.PartTransformsUp[part_index];
                                break;
                            case ModuleLiftingSurface.TransformDir.Z:
                                liftVector = GameDataCache.PartTransformsForward[part_index];
                                break;
                        }
                        Vector3d nVel = v_wrld_vel.normalized;
                        liftVector *= wing.transformSign;
                        double liftdot = Vector3d.Dot(nVel, liftVector);
                        if (wing.omnidirectional)
                        {
                            absdot = Math.Abs(liftdot);
                        }
                        else
                        {
                            absdot = Util.Clamp01(liftdot);
                        }
                        #endregion SETUP_COEFFICIENTS

                        double prevMach = p.machNumber;
                        p.machNumber = mach;
                        Vector3d local_lift = mcs_mod * (Vector3d)wing.GetLiftVector(liftVector, (float)liftdot, (float)absdot, liftQ, (float)mach);
                        Vector3d local_drag = mcs_mod * (Vector3d)wing.GetDragVector(nVel, (float)absdot, liftQ);
                        p.machNumber = prevMach;

                        total_lift += local_lift;
                        total_drag += local_drag;

#if DEBUG
                        if (partDebug != null)
                        {
                            partDebug.Lift += (float)local_lift.magnitude;
                            partDebug.Drag += (float)local_drag.magnitude;
                        }
#endif
                    }
                }

                Profiler.Stop("SimAeroForce#LiftingSurface");

            }
            // RETURN STUFF
            Vector3 force = total_lift + total_drag;

            Profiler.Stop("SimAeroForce");
            return force;
        }
    } //StockAeroUtil
}

