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
        /// <summary>
        /// This function should return exactly the same value as Vessel.atmDensity,
        ///  but is more generic because you don't need an actual vessel updated by KSP to get a value at the desired location.
        /// Computations are performed for the current body position, which means it's theoretically wrong if you want to know the temperature in the future,
        ///  but since body rotation is not used (position is given in sun frame), you should get accurate results up to a few weeks.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static double GetTemperature(Vector3d position, CelestialBody body)
        {
            if (!body.atmosphere)
                return PhysicsGlobals.SpaceTemperature;

            double altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
                return PhysicsGlobals.SpaceTemperature;

            Vector3 up = (position - body.position).normalized;
            float polarAngle = Mathf.Acos(Vector3.Dot(body.bodyTransform.up, up));
            if (polarAngle > Mathf.PI / 2.0f)
            {
                polarAngle = Mathf.PI - polarAngle;
            }
            float time = (Mathf.PI / 2.0f - polarAngle) * 57.29578f;

            Vector3 sunVector = (FlightGlobals.Bodies[0].position - position).normalized;
            float sunAxialDot = Vector3.Dot(sunVector, body.bodyTransform.up);
            float bodyPolarAngle = Mathf.Acos(Vector3.Dot(body.bodyTransform.up, up));
            float sunPolarAngle = Mathf.Acos(sunAxialDot);
            float sunBodyMaxDot = (1.0f + Mathf.Cos(sunPolarAngle - bodyPolarAngle)) * 0.5f;
            float sunBodyMinDot = (1.0f + Mathf.Cos(sunPolarAngle + bodyPolarAngle)) * 0.5f;
            float sunDotCorrected = (1.0f + Vector3.Dot(sunVector, Quaternion.AngleAxis(45f * Mathf.Sign((float)body.rotationPeriod), body.bodyTransform.up) * up)) * 0.5f;
            float sunDotNormalized = (sunDotCorrected - sunBodyMinDot) / (sunBodyMaxDot - sunBodyMinDot);
            double atmosphereTemperatureOffset = (double)body.latitudeTemperatureBiasCurve.Evaluate(time) + (double)body.latitudeTemperatureSunMultCurve.Evaluate(time) * sunDotNormalized + (double)body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            double temperature = body.GetTemperature(altitude) + (double)body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;

            return temperature;
        }

        /// <summary>
        /// Gets the air density (rho) for the specified altitude on the specified body.
        /// This is an approximation, because actual calculations, taking sun exposure into account to compute air temperature, require to know the actual point on the body where the density is to be computed (knowing the altitude is not enough).
        /// However, the difference is small for high altitudes, so it makes very little difference for trajectory prediction.
        /// </summary>
        /// <param name="altitude">Altitude above sea level (in meters)</param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static double GetDensity(double altitude, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0;

            if (altitude > body.atmosphereDepth)
                return 0;

            double pressure = body.GetPressure(altitude);

            // get an average day/night temperature at the equator
            double sunDot = 0.5;
            float sunAxialDot = 0;
            double atmosphereTemperatureOffset = (double)body.latitudeTemperatureBiasCurve.Evaluate(0)
                + (double)body.latitudeTemperatureSunMultCurve.Evaluate(0) * sunDot
                + (double)body.axialTemperatureSunMultCurve.Evaluate(sunAxialDot);
            double temperature = // body.GetFullTemperature(altitude, atmosphereTemperatureOffset);
                body.GetTemperature(altitude)
                + (double)body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude) * atmosphereTemperatureOffset;


            return body.GetDensity(pressure, temperature);
        }

        public static double GetDensity(Vector3d position, CelestialBody body)
        {
            if (!body.atmosphere)
                return 0;

            double altitude = (position - body.position).magnitude - body.Radius;
            if (altitude > body.atmosphereDepth)
                return 0;

            double pressure = body.GetPressure(altitude);
            double temperature = // body.GetFullTemperature(position);
                GetTemperature(position, body);

            return body.GetDensity(pressure, temperature);
        }

        //*******************************************************
        public static Vector3d SimAeroForce(Vector3d v_wrld_vel, Vector3 position)
        {
            CelestialBody body = Trajectories.AttachedVessel.mainBody;
            double latitude = body.GetLatitude(position) / 180.0 * Math.PI;
            double altitude = (position - body.position).magnitude - body.Radius;

            return SimAeroForce(v_wrld_vel, altitude, latitude);
        }

        //*******************************************************
        public static Vector3d SimAeroForce(Vector3d v_wrld_vel, double altitude, double latitude = 0.0)
        {
            Profiler.Start("SimAeroForce");

            CelestialBody body = Trajectories.AttachedVessel.mainBody;
            double pressure = body.GetPressure(altitude);
            // Lift and drag for force accumulation.
            Vector3d total_lift = Vector3d.zero;
            Vector3d total_drag = Vector3d.zero;

            // dynamic pressure for standard drag equation
            double rho = GetDensity(altitude, body);
            double dyn_pressure = 0.0005 * rho * v_wrld_vel.sqrMagnitude;

            if (rho <= 0)
            {
                return Vector3.zero;
            }

            double soundSpeed = body.GetSpeedOfSound(pressure, rho);
            double mach = v_wrld_vel.magnitude / soundSpeed;
            if (mach > 25.0) { mach = 25.0; }

            // Loop through all parts, accumulating drag and lift.
            for (int i = 0; i < Trajectories.AttachedVessel.Parts.Count; ++i)
            {
                // need checks on shielded components
                Part p = Trajectories.AttachedVessel.Parts[i];
                #if DEBUG
                TrajectoriesDebug partDebug = AeroDynamicModel.DebugParts ? p.FindModuleImplementing<TrajectoriesDebug>() : null;
                if (partDebug != null)
                {
                    partDebug.Drag = 0;
                    partDebug.Lift = 0;
                }
                #endif

                if (p.ShieldedFromAirstream || p.Rigidbody == null)
                {
                    continue;
                }

                // Get Drag
                Vector3d sim_dragVectorDir = v_wrld_vel.normalized;
                Vector3d sim_dragVectorDirLocal = -(p.transform.InverseTransformDirection(sim_dragVectorDir));

                Vector3d liftForce = Vector3d.zero;
                Vector3d dragForce;


                Profiler.Start("SimAeroForce#drag");
                switch (p.dragModel)
                {
                    case Part.DragModel.DEFAULT:
                    case Part.DragModel.CUBE:
                        DragCubeList cubes = p.DragCubes;

                        DragCubeList.CubeData p_drag_data = new DragCubeList.CubeData();

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
                        dragForce = -sim_dragVectorDir * Mathf.Lerp(p.minimum_drag, p.maximum_drag, Mathf.Abs(Vector3.Dot(p.partTransform.TransformDirection(p.dragReferenceVector), sim_dragVectorDir)));
                        break;

                    case Part.DragModel.CONIC:
                        dragForce = -sim_dragVectorDir * Mathf.Lerp(p.minimum_drag, p.maximum_drag, Vector3.Angle(p.partTransform.TransformDirection(p.dragReferenceVector), sim_dragVectorDir) / 180f);
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
                    Vector3 bodyLift = p.transform.rotation * (simbodyLiftScalar * liftForce);
                    bodyLift = Vector3.ProjectOnPlane(bodyLift, sim_dragVectorDir);
                    // Only accumulate forces for non-LiftModules
                    total_lift += bodyLift;


                    Profiler.Stop("SimAeroForce#BodyLift");
                }


                Profiler.Start("SimAeroForce#LiftingSurface");

                // Find ModuleLifingSurface for wings and liftforce.
                // Should catch control surface as it is a subclass
                for (int j = 0; j < p.Modules.Count; ++j)
                {
                    var m = p.Modules[j];
                    float mcs_mod;
                    if (m is ModuleLiftingSurface)
                    {
                        mcs_mod = 1.0f;
                        double liftQ = dyn_pressure * 1000;
                        ModuleLiftingSurface wing = (ModuleLiftingSurface)m;
                        Vector3 nVel = Vector3.zero;
                        Vector3 liftVector = Vector3.zero;
                        float liftdot;
                        float absdot;
                        wing.SetupCoefficients(v_wrld_vel, out nVel, out liftVector, out liftdot, out absdot);

                        double prevMach = p.machNumber;
                        p.machNumber = mach;
                        Vector3 local_lift = mcs_mod * wing.GetLiftVector(liftVector, liftdot, absdot, liftQ, (float)mach);
                        Vector3 local_drag = mcs_mod * wing.GetDragVector(nVel, absdot, liftQ);
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

