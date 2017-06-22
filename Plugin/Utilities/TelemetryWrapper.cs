#if DEBUG_TELEMETRY

using System;
using System.Reflection;
using UnityEngine;

namespace Trajectories
{
    public class Telemetry
    {
        private static string thisAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;

        private static object telemetryServiceInstance = null;
        private static MethodInfo addChannelMethod;
        private static MethodInfo sendMethod;


        public static void AddChannel<ChannelType>(string id, string format = null)
            where ChannelType : IFormattable
        {
            if (telemetryServiceInstance == null)
                return;

            // AddChannel is generic, we find the concrete implementation
            MethodInfo addChannelMethodConcrete =
                addChannelMethod.MakeGenericMethod(typeof(ChannelType));

            var parameters = new object[] { thisAssemblyName + "/" + id, format };
            addChannelMethodConcrete.Invoke(telemetryServiceInstance, parameters);
        }

        public static void Send(string id, object value)
        {
            if (telemetryServiceInstance == null)
                return;

            var parameters = new object[] { thisAssemblyName + "/" + id, value };
            sendMethod.Invoke(telemetryServiceInstance, parameters);
        }


        static Telemetry()
        {
            // Search for telemetry assembly
            Type telemetryServiceType = null;
            foreach (var loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name != "Telemetry")
                    continue;

                telemetryServiceType = loadedAssembly.assembly.GetType("Telemetry.TelemetryService");
            }

            // if it's not loaded, bow and excuse ourselves
            if (telemetryServiceType == null)
            {
                Debug.Log(thisAssemblyName + " could not find Telemetry module. Continuing without Telemetry.");
                return;
            }

            // if it's loaded, get instance and Send/AddChannel Methods
            telemetryServiceInstance = telemetryServiceType.GetProperty("Instance", telemetryServiceType).GetValue(null, null);
            addChannelMethod = telemetryServiceType.GetMethod("AddChannel");
            sendMethod = telemetryServiceType.GetMethod("Send");

            Debug.Log(thisAssemblyName + " connected to Telemetry module.");
        }
    }
}

#endif
