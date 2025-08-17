using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Models
{
    internal class StateSettings
    {
        // Kazda vlastnost v teto tride reprezentuje jednu vlastnost objektu State v appsettings.json
        public string CheckpointFile { get; set; } = default!;
    }
}
