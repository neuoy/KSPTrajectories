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

  * Originally from Kerbalism under the Unlicensed License, for more information, please refer to <http://unlicense.org>
  * Ported to Trajectories by PiezPiedPy
*/

using System;
using System.Linq;
using System.Reflection;

namespace TrajectoriesBootstrap
{
    public static class AddonLoaderWrapper
    {
        private static readonly MethodInfo Method__StartAddon;

        static AddonLoaderWrapper()
        {
            Method__StartAddon = typeof(AddonLoader).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).First(delegate (MethodInfo method)
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 4)
                    return false;
                if (parameters[0].ParameterType != typeof(AssemblyLoader.LoadedAssembly))
                    return false;
                if (parameters[1].ParameterType != typeof(Type))
                    return false;
                if (parameters[2].ParameterType != typeof(KSPAddon))
                    return false;
                if (parameters[3].ParameterType != typeof(KSPAddon.Startup))
                    return false;
                return true;
            });
        }

        public static void StartAddon(AssemblyLoader.LoadedAssembly assembly, Type addonType, KSPAddon addon, KSPAddon.Startup startup)
        {
            Method__StartAddon.Invoke(AddonLoader.Instance, new object[] { assembly, addonType, addon, startup });
        }
    }
}
