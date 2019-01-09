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
using UnityEngine;

namespace Trajectories
{
    public class Telemetry
    {
#if DEBUG_TELEMETRY
        private const int APIVersionMajor = 0;
        private const int APIVersionMinor = 1;

        private static string thisAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        private static object telemetryServiceInstance = null;
        private static MethodInfo addChannelMethod = null;
        private static MethodInfo sendMethod = null;
#endif

        [System.Diagnostics.Conditional("DEBUG_TELEMETRY")]
        public static void AddChannel<ChannelType>(string id, string format = null)
            where ChannelType : IFormattable
        {
#if DEBUG_TELEMETRY
            if (telemetryServiceInstance == null)
                return;

            // AddChannel is generic, we find the concrete implementation
            MethodInfo addChannelMethodConcrete =
                addChannelMethod.MakeGenericMethod(typeof(ChannelType));

            var parameters = new object[] { thisAssemblyName + "/" + id, format };
            addChannelMethodConcrete.Invoke(telemetryServiceInstance, parameters);
#endif
        }

        [System.Diagnostics.Conditional("DEBUG_TELEMETRY")]
        public static void Send(string id, object value)
        {
#if DEBUG_TELEMETRY
            if (telemetryServiceInstance == null)
                return;

            var parameters = new object[] { thisAssemblyName + "/" + id, value };
            sendMethod.Invoke(telemetryServiceInstance, parameters);
#endif
        }


        static Telemetry()
        {
#if DEBUG_TELEMETRY
            int versionMajor = 0, versionMinor = 0;

            // Search for telemetry assembly
            Type telemetryServiceType = null;
            foreach (var loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name != "Telemetry")
                    continue;

                telemetryServiceType = loadedAssembly.assembly.GetType("Telemetry.TelemetryService");

                var fvi = FileVersionInfo.GetVersionInfo(loadedAssembly.path);

                versionMajor = fvi.FileMajorPart;
                versionMinor = fvi.FileMinorPart;
            }

            if (telemetryServiceType == null)
            {
                UnityEngine.Debug.Log(thisAssemblyName + " could not find Telemetry module. Continuing without Telemetry.");
                return;
            }

            if (versionMajor != APIVersionMajor || versionMinor < APIVersionMinor)
            {
                UnityEngine.Debug.Log(thisAssemblyName +
                    " Telemetry module version " + versionMajor + "." + versionMinor + " is incompatible with the Wrapper version "
                    + APIVersionMajor + "." + APIVersionMinor);
                return;
            }


            // if it's loaded, get instance and Send/AddChannel Methods
            telemetryServiceInstance = telemetryServiceType.GetProperty("Instance", telemetryServiceType).GetValue(null, null);
            addChannelMethod = telemetryServiceType.GetMethod("AddChannel");
            sendMethod = telemetryServiceType.GetMethod("Send");

            UnityEngine.Debug.Log(thisAssemblyName + " connected to Telemetry module.");
#endif
        }
    }
}
