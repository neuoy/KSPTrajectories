/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
  Copyright© (c) 2017-2021 S.Gray, (aka PiezPiedPy).

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
    public static class AeroDynamicModelFactory
    {
        public static AeroDynamicModel GetModel(CelestialBody body)
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

                            return new FARModel(body, FARAPI_CalculateVesselAeroForces);

                        //case "MyModAssembly":
                        // implement here your atmo mod detection
                        // return new MyModModel(body, any other parameter);
                    }
                }
                catch (Exception e)
                {
                    Util.LogError("Failed to interface with assembly {0}, exception was {1}, using stock model instead", loadedAssembly.name, e.ToString());
                }
            }

            // Using stock model if no other aerodynamic is detected or if any error occurred
            return new StockModel(body);
        }
    }
}
