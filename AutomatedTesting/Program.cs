using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tests.Framework;

namespace AutomatedTesting
{
    class Program
    {
        public static string TrajectoriesRoot = @"D:\dev\KerbalSpaceProgram\KSPTrajectories";
        public static string TestZoneRoot = @"D:\dev\KerbalSpaceProgram\KSP_TestZone";

        static void Main(string[] args)
        {
            var tests = from type in Assembly.GetExecutingAssembly().GetTypes()
                        let attr = type.GetCustomAttributes(typeof(KSPTest), true)
                        where attr != null && attr.Length == 1
                        select new { Type = type, Attribute = attr.First() as KSPTest };

            foreach (var test in tests)
            {
                try
                {
                    Trace.TraceInformation("Starting test: " + test.Type.Name);
                    object inst = Activator.CreateInstance(test.Type);
                    test.Type.GetMethod("Run").Invoke(inst, null);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Test " + test.Type.Name + " failed with exception: " + e.ToString());
                }
            }
        }
    }
}
