using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Graph.DTO
{
    // Oriznuta podoba e-mailu z Graph API, která nese jen to, co potřebujeme pro uložení.
    internal class GraphMailItemDTO
    {
        public string Id { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public DateTimeOffset? ReceivedDateTime { get; set; }
        public bool HasAttachments { get; set; }
        public string? InternetMessageId { get; set; }
        public string? FromName { get; set; }
        public string? FromEmail { get; set; }
        public string? ConversationId { get; set; }

        // Konstruktor pro vytvoření z GraphMessageDTO
        public static GraphMailItemDTO FromGraphMessage(GraphMessageDTO message)
        {
            return new GraphMailItemDTO
            {
                Id = message.Id,
                Subject = message.Subject,
                ReceivedDateTime = message.ReceivedDateTime,
                HasAttachments = message.HasAttachments,
                InternetMessageId = message.InternetMessageId,
                FromName = message.From?.EmailAddress?.Name,
                FromEmail = message.From?.EmailAddress?.Address,
                ConversationId = message.ConversationId
            };
        }
    }
}
