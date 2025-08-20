using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MimeKit;                            
using MailPullerApp.Configuration;        
using MailPullerApp.Services.Storage;

namespace MailPullerApp.Services.Storage
{
    // Hlavni třída pro ukládání e-mailů do souborového systému, implementuje IEmailStore, využívá FilenameSanitizer pro sanitaci názvů souborů,
    // a vyuziva objekt output v appsettings.json aby se urcilo, kam se maji emaily ukládat, zda ukladat cely email a zda ukladat prilohy.
    public sealed class FileSystemEmailStore
    {
        private readonly string _RootDirectory;
        private readonly bool _SaveMimeEml;
        private readonly bool _SaveAttachmentsWithMimeKit;
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileSystemEmailStore));

        //docasne pro overeni v program.cs
        internal string RootDirectory => _RootDirectory;
        internal bool SaveMimeEml => _SaveMimeEml;
        internal bool SaveAttachmentsWithMimeKit => _SaveAttachmentsWithMimeKit;

        internal FileSystemEmailStore(AppConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null.");
            }
            if (config.Output == null)
            {
                throw new ArgumentException("Output settings are not properly configured in the application configuration.");
            }
            if (config.Output.RootDirectory == null)
            {
                throw new ArgumentException("RootDirectory is not set in the Output settings.");
            }
            _RootDirectory = Path.GetFullPath(config.Output.RootDirectory);
            _SaveMimeEml = config.Output.SaveMimeEml;
            _SaveAttachmentsWithMimeKit = config.Output.SaveAttachmentsWithMimeKit;
            if (!Directory.Exists(_RootDirectory))
            {
                Directory.CreateDirectory(_RootDirectory);
            }
            Log.Debug($"FileSystemEmailStore: RootDirectory={_RootDirectory}, SaveMimeEml={_SaveMimeEml}, SaveAttachments={_SaveAttachmentsWithMimeKit}");
            
            
        }
    }
        
}
