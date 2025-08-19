using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Configuration
{
    internal class ConfigLoader
    {
        // Metoda pro načtení konfigurace z appsettings.json
        public static AppConfig Load(string filePath = "appsettings.json")
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath)); //Pokud neni cesta k souboru nastavena, vyhodime vyjimku
            }
            try
            {
                // Načteme obsah souboru a deserializujeme ho do objektu AppConfig
                var config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(System.IO.File.ReadAllText(filePath)); 
                if (config == null)
                {
                    // Pokud se konfigurace nepodařilo načíst, vyhodíme vyjimku
                    throw new InvalidOperationException("Configuration could not be loaded from the specified file.");
                }
                return config;
            }
            catch (Exception ex)
            {
                // Pokud dojde k chybě při načítání konfigurace, vyhodíme vyjimku s informacemi o chybě
                throw new InvalidOperationException($"Failed to load configuration from {filePath}.", ex);
            }
        }
    }
}
