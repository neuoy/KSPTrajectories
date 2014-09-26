using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    class Util
    {
        public static void CopyDirectoryContents(string sourceDir, string destDir)
        {
            // Create subdirectory structure in destination    
            foreach (string dir in Directory.GetDirectories(sourceDir, "*", System.IO.SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(destDir + dir.Substring(sourceDir.Length));
            }

            foreach (string file_name in Directory.GetFiles(sourceDir, "*.*", System.IO.SearchOption.AllDirectories))
            {
                File.Copy(file_name, destDir + file_name.Substring(sourceDir.Length));
            }
        }
    }
}
