using System;
using System.Reflection;
using UnityEngine;

namespace Trajectories
{
    public static class AerodynamicModelFactory
    {
        public static VesselAerodynamicModel GetModel(Vessel ship, CelestialBody body)
        {
            foreach (var loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                try
                {
                    switch (loadedAssembly.name)
                    {
                        case "FerramAerospaceResearch":
                            var FARAPIType = loadedAssembly.assembly.GetType("FerramAerospaceResearch.FARAPI");

                                var FARAPI_CalculateVesselAeroForces = FARAPIType.GetMethodEx("CalculateVesselAeroForces", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(Vessel), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(Vector3), typeof(double) });

                            return new FARModel(ship, body, FARAPI_CalculateVesselAeroForces);

                        //case "MyModAssembly":
                        // implement here your atmo mod detection
                        // return new MyModModel(ship, body, any other parameter);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Trajectories: failed to interface with assembly " + loadedAssembly.name);
                    Debug.Log("Using stock model instead");
                    Debug.Log(e.ToString());
                }
            }

            // Using stock model if no other aerodynamic is detected or if any error occured
            return new StockModel(ship, body);
        }
    }
}
