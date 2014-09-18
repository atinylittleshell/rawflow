using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RawFlow
{
    static class Settings
    {
        static Settings()
        {
            TemporaryWorkingDirectory = GetConfiguredDirectory("TemporaryWorkingDirectory");
            OutputDirectory = GetConfiguredDirectory("OutputDirectory");
        }

        private static string GetConfiguredDirectory(string key)
        {
            var result = System.Configuration.ConfigurationManager.AppSettings[key] ?? string.Empty;

            if (!string.IsNullOrEmpty(result))
            {
                if (!result.EndsWith("\\"))
                {
                    result = result + "\\";
                }

                if (!Directory.Exists(result))
                {
                    Directory.CreateDirectory(result);
                }
            }

            return result;
        }

        public static string OutputDirectory { get; set; }

        public static string TemporaryWorkingDirectory { get; set; }
    }
}
