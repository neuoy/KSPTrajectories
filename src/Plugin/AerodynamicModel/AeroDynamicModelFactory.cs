/*
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
