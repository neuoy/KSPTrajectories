/*
Trajectories
Copyright 2014, Youen Toupin

This file is part of Trajectories, under MIT license.

StockAeroUtil by atomicfury

*/

//#define PRECOMPUTE_CACHE
#define USE_CACHE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace Trajectories
{
    // this class provides several methods to access stock aero information
    public static class StockAeroUtil
    {
        public static double GetTemperature(double altitude, double latitude, CelestialBody body)
        {
            double maxAltitude = body.atmosphereDepth;
            double space_temp = PhysicsGlobals.SpaceTemperature;
            double surfc_temp = body.atmosphereTemperatureSeaLevel;
            if (altitude > maxAltitude)
            {
                return PhysicsGlobals.SpaceTemperature;
            }
            double base_temp = body.GetTemperature(altitude);
            double lat_bias = body.latitudeTemperatureBiasCurve.Evaluate((float)latitude);
            double lat_mult = body.latitudeTemperatureSunMultCurve.Evaluate((float)latitude);
            double altitude_temp_mult = body.atmosphereTemperatureSunMultCurve.Evaluate((float)altitude);
            double axial_temp_mult = body.axialTemperatureSunMultCurve.Evaluate(0);
            // Solar effect too complicated, worth ~4 degrees sea level Kerbin?
            double solar_effect = 1.0;
            double atmo_factor = (lat_bias + lat_mult * solar_effect + axial_temp_mult) * altitude_temp_mult;
            return base_temp + atmo_factor;
        }

        public static double GetTemperature(Vector3 position, CelestialBody body)
        {
            double latitude = body.GetLatitude(position) / 180.0 * Math.PI;
            double altitude = (position - body.position).magnitude - body.Radius;
            return GetTemperature(altitude, latitude, body);

        }

        public static double GetPressure(double altitude, double latitude, CelestialBody body)
        {
            // Get pressure
            if (!body.atmosphere) { return 0.0d; }
            if (altitude >= body.atmosphereDepth) { return 0.0d; }
            if (!body.atmosphereUsePressureCurve)
            {
                return body.atmospherePressureSeaLevel * Math.Pow(1 - body.atmosphereTemperatureLapseRate * altitude / body.atmosphereTemperatureSeaLevel, body.atmosphereGasMassLapseRate);
            }
            if (!body.atmospherePressureCurveIsNormalized)
            {
                return body.atmospherePressureCurve.Evaluate((float)altitude);
            }
            return Mathf.Lerp(0f, (float)body.atmospherePressureSeaLevel, body.atmospherePressureCurve.Evaluate((float)(altitude / body.atmosphereDepth)));
        }

        public static double GetPressure(Vector3 position, CelestialBody body)
        {
            double latitude = body.GetLatitude(position) / 180.0 * Math.PI;
            double altitude = (position - body.position).magnitude - body.Radius;
            return GetPressure(altitude, latitude, body);

        }

        public static double GetDensity(double altitude, double latitude, CelestialBody body)
        {

            if (!body.atmosphere) { return 0.0d; }
            if (altitude >= body.atmosphereDepth) { return 0.0d; }

            double temp = GetTemperature(altitude, latitude, body);
            double pressure = GetPressure(altitude, latitude, body);

            return FlightGlobals.getAtmDensity(pressure, temp, body);
        }

        public static double GetDensity(Vector3 position, CelestialBody body)
        {
            double latitude = body.GetLatitude(position) / 180.0 * Math.PI;
            double altitude = (position - body.position).magnitude - body.Radius;
            return GetDensity(altitude, latitude, body);

        }

        //*******************************************************
        public static Vector3 SimAeroForce(Vessel _vessel, Vector3 v_wrld_vel, Vector3 position)
        {
            CelestialBody body = _vessel.mainBody;
            double latitude = body.GetLatitude(position) / 180.0 * Math.PI;
            double altitude = (position - body.position).magnitude - body.Radius;
            double pressure = StockAeroUtil.GetPressure(position, body);
            // Lift and drag for force accumulation.
            Vector3d total_lift = Vector3d.zero;
            Vector3d total_drag = Vector3d.zero;

            if (altitude > body.atmosphereDepth)
            {
                return Vector3.zero;
            }

            // dynamic pressure for standard drag equation
            double dyn_pressure = 0.0005 * GetDensity(position, body) * v_wrld_vel.sqrMagnitude;
            double rho = GetDensity(altitude, latitude, body);

            double soundSpeed = body.GetSpeedOfSound(pressure, rho);
            double mach = v_wrld_vel.magnitude / soundSpeed;
            if (mach > 25.0) { mach = 25.0; }

            // Loop through all parts, accumulating drag and lift.
            for (int i = 0; i < _vessel.Parts.Count; ++i)
            {
                // need checks on shielded components
                Part p = _vessel.Parts[i];
                if (p.ShieldedFromAirstream)
                {
                    continue;
                }
                if (p.rb == null)
                {
                    continue;
                }
                if (true)
                {
                    // Get Drag
                    Vector3 sim_dragVectorDir = v_wrld_vel.normalized;
                    Vector3 sim_dragVectorDirLocal = -(p.transform.InverseTransformDirection(v_wrld_vel.normalized));

                    DragCubeList cubes = p.DragCubes;

                    DragCubeList.CubeData p_drag_data;
                    
                    // negative local air velocity should go into AddSurfaceDragDirection
                    try
                    {
                        p_drag_data = cubes.AddSurfaceDragDirection(-sim_dragVectorDirLocal, (float)mach);
                    }
                    catch (Exception e)
                    {
                        cubes.SetDrag(sim_dragVectorDirLocal, (float)mach);
                        cubes.ForceUpdate(true, true);
                        p_drag_data = cubes.AddSurfaceDragDirection(-sim_dragVectorDirLocal, (float)mach);
                        //Debug.Log(String.Format("Trajectories: Caught NRE on Drag Initialization.  Should be fixed now.  {0}", e));
                    }
                    // NRE occurs in AddSurfaceDragDirection call if SetDrag isn't run to initialize.
                    // ForceUpdate may not be necessary.
                    // Runs the risk of something else throwing an NRE, but what are you going to do?
                    // Logging disabled for performance - if someone is bug hunting turn it back on.

                    float areaDrag = p_drag_data.areaDrag;
                    float area = p_drag_data.area;
                    float dragCoeff = p_drag_data.dragCoeff;
                    Vector3 dragVector = p_drag_data.dragVector;
                    Vector3 liftForce = p_drag_data.liftForce;

                    double sim_dragScalar = dyn_pressure * (double)areaDrag * PhysicsGlobals.DragCubeMultiplier * PhysicsGlobals.DragMultiplier;

                    // If it isn't a wing or lifter, get body lift.
                    if (!p.hasLiftModule)
                    {
                        float simbodyLiftScalar = p.bodyLiftMultiplier * PhysicsGlobals.BodyLiftMultiplier * (float)dyn_pressure;
                        simbodyLiftScalar *= PhysicsGlobals.GetLiftingSurfaceCurve("BodyLift").liftMachCurve.Evaluate((float)mach);
                        Vector3 bodyLift = p.transform.rotation * (simbodyLiftScalar * liftForce);
                        bodyLift = Vector3.ProjectOnPlane(bodyLift, sim_dragVectorDir);
                        // Only accumulate forces for non-LiftModules
                        total_lift += bodyLift;
                        total_drag += -(Vector3d)sim_dragVectorDir * sim_dragScalar;
                    }

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
                            wing.SetupCoefficients(v_wrld_vel, rho, out nVel, out liftVector, out liftdot, out absdot);

                            float simLiftScalar = Mathf.Sign(liftdot) * wing.liftCurve.Evaluate(absdot) * wing.liftMachCurve.Evaluate((float)mach);
                            simLiftScalar *= wing.deflectionLiftCoeff;
                            simLiftScalar = (float)(liftQ * (double)(PhysicsGlobals.LiftMultiplier * simLiftScalar));

                            float simdragScalar = wing.dragCurve.Evaluate(absdot) * wing.dragMachCurve.Evaluate((float)mach);
                            simdragScalar *= wing.deflectionLiftCoeff;
                            simdragScalar = (float)(liftQ * (double)(simdragScalar * PhysicsGlobals.LiftDragMultiplier));

                            Vector3 local_lift = mcs_mod * wing.GetLiftVector(liftVector, liftdot, absdot, liftQ, (float)mach);
                            Vector3 local_drag = mcs_mod * wing.GetDragVector(nVel, absdot, liftQ);

                            total_lift += local_lift;
                            total_drag += local_drag;
                        }
                    } // module loop
                } //shielded

            }
            // RETURN STUFF
            Vector3 force = total_lift + total_drag;
            return force;
        }
    }
}

