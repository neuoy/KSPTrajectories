/*
  Copyright© (c) 2016-2017 Youen Toupin, (aka neuoy).
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

using System;
using System.Reflection;
using UnityEngine;

namespace Trajectories
{
    internal static class AerodynamicModelFactory
    {
        /// <summary> Searches for compatible atmospheric mod API's and sets their required MethodInfo's </summary>
        /// <returns> The aerodynamic model for a found API or the stock model if none  or an error occurs </returns>
        internal static VesselAerodynamicModel GetModel()
        {
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                try
                {
                    switch (loadedAssembly.name)
                    {
                        case "FerramAerospaceResearch":
                            return new FARModel(loadedAssembly.assembly.GetType("FerramAerospaceResearch.FARAPI").
                                GetMethodEx("CalculateVesselAeroForces", BindingFlags.Public | BindingFlags.Static,
                                new Type[] {
                                    typeof(Vessel),
                                    typeof(Vector3).MakeByRefType(),
                                    typeof(Vector3).MakeByRefType(),
                                    typeof(Vector3),
                                    typeof(double)
                                }));
                            // case "MyModAssembly":
                            // implement your atmospheric mod detection here
                    }
                }
                catch (Exception e)
                {
                    Util.LogError("Failed to interface with assembly {0}, exception was {1}, using stock model instead", loadedAssembly.name, e.ToString());
                }
            }
            // Using stock model if no other aerodynamic model is detected or if any error occurred
            return new StockModel();
        }
    }
}

