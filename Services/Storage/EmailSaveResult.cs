using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Storage
{
    internal class EmailSaveResult
    {
        public string FolderPath { get; init; } = string.Empty;
        public string? EmlPath { get; init; }
        public IReadOnlyList<string> Attachments { get; init; } = Array.Empty<string>();
        public string SafeFolderName { get; init; } = string.Empty;
        public bool WasSkipped { get; init; }
    }
}
