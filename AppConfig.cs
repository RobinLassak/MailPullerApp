using MailPullerApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp
{
    internal class AppConfig
    {
        public GraphSettings Graph { get; set; } = default!;
        public MailboxSettings Mailbox { get; set; } = default!;
        public OutputSettings Output { get; set; } = default!;
        public StateSettings State { get; set; } = default!;
        public SecuritySettings Security { get; set; } = default!;
        public LoggingSettings Logging { get; set; } = default!;
    }
}
