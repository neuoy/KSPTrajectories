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
