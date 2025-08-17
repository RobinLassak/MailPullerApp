using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Models
{
    internal class MailboxSettings
    {
        // Kazda vlastnost v teto tride reprezentuje jednu vlastnost objektu Mailbox v appsettings.json
        public string Address { get; set; } = default!;
        public string Folder { get; set; } = default!;
        public string StartDateUtc { get; set; } = default!;
        public int PageSize { get; set; }
    }
}
