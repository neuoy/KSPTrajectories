using AutomatedTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Framework
{
    class KSP
    {
        public string Version { get; set; }
        public TestAutomation.Config GameConfig { get; set; }
        public IEnumerable<ModInfo> Mods { get; set; }

        public void RunTest()
        {
            Trace.TraceInformation("Preparing to run KSP test...");

            string kspRoot = Program.TestZoneRoot + "/" + Version + "/KSP";
            string gameData = kspRoot + "/GameData";
            File.Delete(kspRoot + "/KSP_Data/output_log.txt");
            
            // Clean up GameData
            string[] exclude = File.ReadAllLines(gameData + "/persistent.txt").Select(f => f.ToLower()).ToArray();
            foreach (string file in Directory.GetFiles(gameData).Where(f => !exclude.Contains(Path.GetFileName(f).ToLower())))
            {
                File.Delete(file);
            }
            foreach (string file in Directory.GetDirectories(gameData).Where(f => !exclude.Contains(Path.GetFileName(f).ToLower())))
            {
                Directory.Delete(file, true);
            }

            // Copy mods required for this test
            foreach (var mod in Mods)
            {
                Util.CopyDirectoryContents(Program.TestZoneRoot + "/Mods/" + mod.Name + "#" + mod.Version, gameData + "/" + mod.Name);
            }

            // Clean up saves
            foreach (string file in Directory.GetFiles(kspRoot+"/Saves/default"))
            {
                File.Delete(file);
            }

            // Copy the Trajectories mod
            var info = new ProcessStartInfo(Program.TrajectoriesRoot + "\\build\\7z.exe", "x \"" + Program.TrajectoriesRoot + "/Trajectories.zip\" \"-o" + gameData + "\"");
            info.UseShellExecute = false;
            Process proc = Process.Start(info);
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new Exception("Failed to deploy the Trajectories plugin");
            Directory.Move(gameData + "/GameData/Trajectories", gameData + "/Trajectories");
            Directory.Delete(gameData + "/GameData");

            // Copy the TestAutomation plugin
            string testAutomationPath = Path.GetDirectoryName(AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.ToLower().Contains("testautomation")).Location);
            File.Copy(testAutomationPath + "/TestAutomation.dll", gameData + "/TestAutomation.dll");
            File.Copy(testAutomationPath + "/TrajectoriesAPI.dll", gameData + "/TrajectoriesAPI.dll");
            File.Copy(Program.TrajectoriesRoot + "/TestAutomation/Newtonsoft.Json.dll", gameData + "/Newtonsoft.Json.dll");

            // Send automation config to the game
            string data = JsonConvert.SerializeObject(GameConfig, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(gameData + "/TestAutomation.json", data);

            // Copy the save file
            File.Copy(Program.TrajectoriesRoot + "/AutomatedTesting/Saves/" + GameConfig.SaveFile+".sfs", kspRoot + "/Saves/default/" + GameConfig.SaveFile+".sfs");

            Trace.TraceInformation("Starting KSP");

            info = new ProcessStartInfo(kspRoot+"/KSP.exe");
            info.UseShellExecute = false;
            proc = Process.Start(info);
            proc.WaitForExit();

            // Copy log files
            string logDir = Program.TrajectoriesRoot + "/AutomatedTesting/Results/" + Program.CurrentTestName;
            Directory.CreateDirectory(logDir);
            File.Copy(gameData + "/TestAutomation.log", logDir + "/TestAutomation.log", true);
            File.Copy(kspRoot + "/KSP_Data/output_log.txt", logDir + "/output_log.txt", true);

            if (proc.ExitCode != 0)
                throw new Exception("KSP exited with error code "+proc.ExitCode);
        }
    }
}
