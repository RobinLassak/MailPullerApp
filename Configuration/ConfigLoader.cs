using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Configuration
{
    internal class ConfigLoader
    {
        public static AppConfig Load(string filePath = "appsettings.json")
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(System.IO.File.ReadAllText(filePath));
                if (config == null)
                {
                    throw new InvalidOperationException("Configuration could not be loaded from the specified file.");
                }
                return config;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from {filePath}.", ex);
            }
        }
    }
}
