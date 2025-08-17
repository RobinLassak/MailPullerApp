using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Models
{
    internal class OutputSettings
    {
        // Kazda vlastnost v teto tride reprezentuje jednu vlastnost objektu Output v appsettings.json
        public string RootDirectory { get; set; } = default!; // Cesta k vystupnimu souboru
        public bool SaveMimeEml { get; set; }
        public bool SaveAttachmentsWithMimeKit { get; set; }
    }
}
