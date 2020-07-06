/*
  Copyright© (c) 2017-2018 A.Korsunsky, (aka fat-lobyte).

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
using System.Diagnostics;
using System.Reflection;

namespace Trajectories
{
    public class Telemetry
    {
#if DEBUG_TELEMETRY || PROFILER_TELEMETRY || WATCHER_TELEMETRY
        private const int APIVersionMajor = 0;
        private const int APIVersionMinor = 1;

        private static string thisAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        private static object telemetryServiceInstance = null;
        private static MethodInfo addChannelMethod = null;
        private static MethodInfo sendMethod = null;
#endif

        [Conditional("DEBUG_TELEMETRY"), Conditional("PROFILER_TELEMETRY"), Conditional("WATCHER_TELEMETRY")]
        public static void AddChannel<ChannelType>(string id, string format = null)
            where ChannelType : IFormattable
        {
#if DEBUG_TELEMETRY || PROFILER_TELEMETRY || WATCHER_TELEMETRY
            if (telemetryServiceInstance == null)
                return;

            // AddChannel is generic, we find the concrete implementation
            MethodInfo addChannelMethodConcrete =
                addChannelMethod.MakeGenericMethod(typeof(ChannelType));

            object[] parameters = new object[] { thisAssemblyName + "/" + id, format };
            addChannelMethodConcrete.Invoke(telemetryServiceInstance, parameters);
#endif
        }

        [Conditional("DEBUG_TELEMETRY"), Conditional("PROFILER_TELEMETRY"), Conditional("WATCHER_TELEMETRY")]
        public static void Send(string id, object value)
        {
#if DEBUG_TELEMETRY || PROFILER_TELEMETRY || WATCHER_TELEMETRY
            if (telemetryServiceInstance == null)
                return;

            object[] parameters = new object[] { thisAssemblyName + "/" + id, value };
            sendMethod.Invoke(telemetryServiceInstance, parameters);
#endif
        }


        static Telemetry()
        {
#if DEBUG_TELEMETRY || PROFILER_TELEMETRY || WATCHER_TELEMETRY
            int versionMajor = 0, versionMinor = 0;

            // Search for telemetry assembly
            Type telemetryServiceType = null;
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name != "Telemetry")
                    continue;

                telemetryServiceType = loadedAssembly.assembly.GetType("Telemetry.TelemetryService");

                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(loadedAssembly.path);

                versionMajor = fvi.FileMajorPart;
                versionMinor = fvi.FileMinorPart;
            }

            if (telemetryServiceType == null)
            {
                Util.DebugLog("Could not find telemetry module, continuing without telemetry");
                return;
            }

            if (versionMajor != APIVersionMajor || versionMinor < APIVersionMinor)
            {
                Util.DebugLog("Telemetry module version {1}.{2} is incompatible with the wrapper version {3}.{4}",
                                versionMajor, versionMinor, APIVersionMajor, APIVersionMinor);
                return;
            }


            // if it's loaded, get instance and Send/AddChannel Methods
            telemetryServiceInstance = telemetryServiceType.GetProperty("Instance", telemetryServiceType).GetValue(null, null);
            addChannelMethod = telemetryServiceType.GetMethod("AddChannel");
            sendMethod = telemetryServiceType.GetMethod("Send");

            Util.DebugLog("Connected to telemetry module");
#endif
        }
    }
}
