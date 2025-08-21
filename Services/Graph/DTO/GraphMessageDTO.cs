using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Graph.DTO
{
    // Slouzi k deserializaci JSON odpovědi MS Graphu se strukturou shodnou s Graphem
    // Pomáhá oddělit externí formát dat od interního modelu aplikace
    internal class GraphMessageDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("receivedDateTime")]
        public DateTimeOffset? ReceivedDateTime { get; set; }

        [JsonPropertyName("hasAttachments")]
        public bool HasAttachments { get; set; }

        [JsonPropertyName("internetMessageId")]
        public string? InternetMessageId { get; set; }

        [JsonPropertyName("from")]
        public GraphRecipientDTO? From { get; set; }

        [JsonPropertyName("toRecipients")]
        public List<GraphRecipientDTO>? ToRecipients { get; set; }

        [JsonPropertyName("ccRecipients")]
        public List<GraphRecipientDTO>? CcRecipients { get; set; }

        [JsonPropertyName("bccRecipients")]
        public List<GraphRecipientDTO>? BccRecipients { get; set; }

        [JsonPropertyName("body")]
        public GraphBodyDTO? Body { get; set; }

        [JsonPropertyName("importance")]
        public string? Importance { get; set; }

        [JsonPropertyName("isRead")]
        public bool? IsRead { get; set; }

        [JsonPropertyName("conversationId")]
        public string? ConversationId { get; set; }

        [JsonPropertyName("uniqueBody")]
        public GraphBodyDTO? UniqueBody { get; set; }
    }

    internal class GraphRecipientDTO
    {
        [JsonPropertyName("emailAddress")]
        public GraphEmailAddressDTO? EmailAddress { get; set; }
    }

    internal class GraphEmailAddressDTO
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }
    }

    internal class GraphBodyDTO
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
