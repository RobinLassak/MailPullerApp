using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Graph.Internal
{
    // Pomocná třída s konstantami a metodami pro Microsoft Graph API
    internal static class GraphApiHelper
    {
        // Graph API konstanty
        public const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
        public const string DefaultScope = "https://graph.microsoft.com/.default";
        
        // Standardní složky
        public static class Folders
        {
            public const string Inbox = "Inbox";
            public const string SentItems = "Sent Items";
            public const string Drafts = "Drafts";
            public const string DeletedItems = "Deleted Items";
            public const string Archive = "Archive";
            public const string JunkEmail = "Junk Email";
        }

        // Standardní pole pro výběr
        public static class SelectFields
        {
            public const string Basic = "id,subject,receivedDateTime,hasAttachments,internetMessageId";
            public const string Extended = "id,subject,receivedDateTime,hasAttachments,internetMessageId,from,toRecipients,ccRecipients,bccRecipients,importance,isRead,conversationId";
            public const string Full = "id,subject,receivedDateTime,hasAttachments,internetMessageId,from,toRecipients,ccRecipients,bccRecipients,importance,isRead,conversationId,body,uniqueBody";
        }

        // Pomocné metody pro validaci
        public static bool IsValidFolderName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return false;

            // Kontrola standardních složek
            var standardFolders = new[]
            {
                Folders.Inbox,
                Folders.SentItems,
                Folders.Drafts,
                Folders.DeletedItems,
                Folders.Archive,
                Folders.JunkEmail
            };

            return standardFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsValidEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static DateTimeOffset? ParseDateTimeOffset(string dateTimeString)
        {
            if (string.IsNullOrWhiteSpace(dateTimeString))
                return null;

            if (DateTimeOffset.TryParse(dateTimeString, out var result))
                return result;

            return null;
        }

        // Pomocné metody pro URL
        public static string BuildUserMessagesUrl(string userEmail, string folder)
        {
            var escapedUser = Uri.EscapeDataString(userEmail);
            var escapedFolder = Uri.EscapeDataString(folder);
            return $"{GraphBaseUrl}/users/{escapedUser}/mailFolders/{escapedFolder}/messages";
        }

        public static string BuildMessageUrl(string userEmail, string messageId)
        {
            var escapedUser = Uri.EscapeDataString(userEmail);
            var escapedMessageId = Uri.EscapeDataString(messageId);
            return $"{GraphBaseUrl}/users/{escapedUser}/messages/{escapedMessageId}";
        }

        public static string BuildMessageMimeUrl(string userEmail, string messageId)
        {
            return $"{BuildMessageUrl(userEmail, messageId)}/$value";
        }

        // Pomocné metody pro query parametry
        public static string BuildQueryString(params (string key, string value)[] parameters)
        {
            var queryParams = new List<string>();
            
            foreach (var (key, value) in parameters)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    queryParams.Add($"{key}={Uri.EscapeDataString(value)}");
                }
            }

            return string.Join("&", queryParams);
        }

        public static string BuildFilterString(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var filters = new List<string>();

            if (startDate.HasValue)
            {
                filters.Add($"receivedDateTime ge {startDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
            }

            if (endDate.HasValue)
            {
                filters.Add($"receivedDateTime le {endDate.Value:yyyy-MM-ddTHH:mm:ssZ}");
            }

            return string.Join(" and ", filters);
        }
    }
}
