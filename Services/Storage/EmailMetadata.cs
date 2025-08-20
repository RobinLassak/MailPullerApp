using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Storage
{
    // Třída pro metadata e-mailu, která může obsahovat informace jako odesílatel, příjemce, předmět atd.
    public class EmailMetadata
    {
        public string Subject { get; init; } = string.Empty;
        public DateTime ReceivedDateUtc { get; init; }
        public string InternetMessageId { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;
        public string FromEmail { get; init; } = string.Empty;

    }
}
