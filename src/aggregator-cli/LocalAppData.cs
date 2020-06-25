using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace aggregator.cli
{
    static class LocalAppData
    {
        public static string GetPath(string filename)
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData,
                    Environment.SpecialFolderOption.Create),
                        "aggregator-cli");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, filename);
        }
    }
}
