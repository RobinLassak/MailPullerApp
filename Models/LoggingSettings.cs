using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Models
{
    internal class LoggingSettings
    {
        // Kazda vlastnost v teto tride reprezentuje jednu vlastnost objektu Logging v appsettings.json
        public string Log4NetConfigFile { get; set; } = default!;
    }
}
