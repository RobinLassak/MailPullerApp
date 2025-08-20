using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Storage
{
    // Rozhraní pro ukládání e-mailů, oddeliuje logiku ukládání od samotné implementace
    internal interface IEmailStore
    {
        Task<EmailSaveResult> SaveEmailAsync(
            EmailMetadata metadata,
            Stream mimeContent, CancellationToken cancellationToken = default);

    }
}
