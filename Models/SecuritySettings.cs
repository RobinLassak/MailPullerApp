using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Models
{
    internal class SecuritySettings
    {
        // Kazda vlastnost v teto tride reprezentuje jednu vlastnost objektu Security v appsettings.json
        public bool EnforceConfiguredMailboxOnly { get; set; }
    }
}
