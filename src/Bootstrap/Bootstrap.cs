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

  * Thanks to blowfish for guiding me through this! (N70 aka steamp0rt)
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TrajectoriesBootstrap
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class TrajectoriesBootstrap: MonoBehaviour
    {

        public void Start()
        {
            if (Util.IsDllLoaded || (Util.FindTrajectoriesAssembly(Util.BinName) != null))
                print("[TrajectoriesBootstrap] WARNING: TRAJECTORIES HAS ALREADY LOADED BEFORE US!");

            string load_bin = Path.Combine(AssemblyDirectory(Assembly.GetExecutingAssembly()), "Trajectories.bin");
            string our_bin = Path.Combine(AssemblyDirectory(Assembly.GetExecutingAssembly()), Util.BinName + ".bin");
            string possible_dll = Path.Combine(AssemblyDirectory(Assembly.GetExecutingAssembly()), "Trajectories.dll");

            if (File.Exists(our_bin))
            {
                print("[TrajectoriesBootstrap] Found Trajectories bin file at '" + our_bin + "'");
                if (File.Exists(possible_dll))
                {
                    try
                    {
                        File.Delete(possible_dll);
                        print("[TrajectoriesBootstrap] Deleted non-bin DLL at '" + possible_dll + "'");
                    }
                    catch
                    {
                        print("[TrajectoriesBootstrap] Could not delete non-bin DLL at '" + possible_dll + "'");
                    }
                }
            }
            else
            {
                print("[TrajectoriesBootstrap] ERROR: COULD NOT FIND TRAJECTORIES BIN FILE (" + Util.BinName + ".bin" + ")! Ditching!");
                return;
            }

            if (Util.IsDllLoaded)
            {
                print("[TrajectoriesBootstrap] Trajectories non-bin DLL already loaded! Ditching!");
                return;
            }

            //copy version specific Trajectoriesxx.bin file to Trajectories.bin and load it if successful
            try
            {
                File.Copy(our_bin, load_bin, true);
                print("[TrajectoriesBootstrap] Copied version specific Trajectories bin file to '" + load_bin + "'");
            }
            catch
            {
                print("[TrajectoriesBootstrap] Could not copy bin file '" + our_bin + "'");
            }

            if (!File.Exists(load_bin))
            {
                print("[TrajectoriesBootstrap] Trajectories.bin not found! Ditching!");
                return;
            }

            AssemblyLoader.LoadPlugin(new FileInfo(load_bin), load_bin, null);
            AssemblyLoader.LoadedAssembly loadedAssembly = Util.FindTrajectoriesAssembly();
            if (loadedAssembly == null)
            {
                print("[TrajectoriesBootstrap] Trajectories failed to load! Ditching!");
                return;
            }
            else
            {
                print("[TrajectoriesBootstrap] Trajectories loaded!");
            }

            loadedAssembly.Load();

            foreach (Type type in loadedAssembly.assembly.GetTypes())
            {
                foreach (Type loadedType in AssemblyLoader.loadedTypes)
                {
                    if (loadedType.IsAssignableFrom(type))
                    {
                        loadedAssembly.types.Add(loadedType, type);
                        PropertyInfo temp = typeof(AssemblyLoader.LoadedAssembly).GetProperty("typesDictionary");
                        if (temp != null)
                        {
                            Dictionary<Type, Dictionary<String, Type>> dict = (Dictionary<Type, Dictionary<String, Type>>)temp.GetValue(loadedAssembly, null);
                            Util.AddToLoadedTypesDict(ref dict, loadedType, type);
                        }

                    }
                }

                if (type.IsSubclassOf(typeof(MonoBehaviour)))
                {
                    KSPAddon addonAttribute = (KSPAddon)type.GetCustomAttributes(typeof(KSPAddon), true).FirstOrDefault();
                    if (addonAttribute != null && addonAttribute.startup == KSPAddon.Startup.Instantly)
                    {
                        AddonLoaderWrapper.StartAddon(loadedAssembly, type, addonAttribute, KSPAddon.Startup.Instantly);
                    }
                }
            }
        }

        public string AssemblyDirectory(Assembly a)
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}
