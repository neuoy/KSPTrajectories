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
using System.Collections.Generic;

namespace TrajectoriesBootstrap
{
    public static class Util
    {
        public static AssemblyLoader.LoadedAssembly FindTrajectoriesAssembly(string name = "Trajectories")
        {
            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
                if (a.name == name)
                    return a;
            return null;
        }

        public static bool IsDllLoaded
        {
            get
            {
                foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
                    if (a.name == "Trajectories")
                        return true;
                return false;
            }
        }

        public static string BinName
        {
            get
            {
                return "Trajectories" + Versioning.version_major.ToString() + Versioning.version_minor.ToString();
            }
        }

        // This is just so we have 1.3 compat!
        public static void AddToLoadedTypesDict(ref Dictionary<Type, Dictionary<String, Type>> dict, Type loadedType, Type type)
        {
            if (!dict.ContainsKey(loadedType))
            {
                dict[loadedType] = new Dictionary<string, Type>();
            }
            dict[loadedType][type.Name] = type;
        }
    }
}
