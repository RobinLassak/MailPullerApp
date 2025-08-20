using log4net;
using MailPullerApp.Configuration;        
using MailPullerApp.Services.Storage;
using MimeKit;                            
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Storage
{
    // Hlavni třída pro ukládání e-mailů do souborového systému, implementuje IEmailStore, využívá FilenameSanitizer pro sanitaci názvů souborů,
    // a vyuziva objekt output v appsettings.json aby se urcilo, kam se maji emaily ukládat, zda ukladat cely email a zda ukladat prilohy.
    internal class FileSystemEmailStore : IEmailStore
    {
        private readonly string _RootDirectory;
        private readonly bool _SaveMimeEml;
        private readonly bool _SaveAttachmentsWithMimeKit;
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileSystemEmailStore));

        //docasne pro overeni v program.cs
        internal string RootDirectory => _RootDirectory;
        internal bool SaveMimeEml => _SaveMimeEml;
        internal bool SaveAttachmentsWithMimeKit => _SaveAttachmentsWithMimeKit;

        //Konstruktor
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
        public async Task<EmailSaveResult> SaveEmailAsync(EmailMetadata metadata, Stream mimeContent, CancellationToken cancellationToken = default)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata), "Email metadata cannot be null.");
            }
            if (mimeContent == null)
            {
                throw new ArgumentNullException(nameof(mimeContent), "MIME content cannot be null.");
            }
            var ts = metadata.ReceivedDateUtc == default ? DateTime.UtcNow : metadata.ReceivedDateUtc;

            var tsPrefix = ts.ToString("yyyyMMdd_HHmmss");
            var safeSubject = FilenameSanitizer.SanitizeFolderName(metadata.Subject);
            var unique = ShortHash(metadata.InternetMessageId);

            var folderNameBase = string.IsNullOrWhiteSpace(safeSubject)
                ? $"{tsPrefix}__{unique}"
                : $"{tsPrefix}__{safeSubject}__{unique}";

            var folderPath = EnsureUniqueFolderPath(_RootDirectory, folderNameBase);
            Directory.CreateDirectory(folderPath);

            Log.Info($"Ukládám e-mail → složka: {folderPath}");

            string? emlPath = null;
            var attachments = new List<String>();

            // 2) Potřebujeme .eml soubor, pokud je uložení zapnuto, nebo pokud budeme tahat přílohy
            var mustPersistEml = _SaveMimeEml || _SaveAttachmentsWithMimeKit;
            if (mustPersistEml)
            {
                emlPath = Path.Combine(folderPath, "message.eml");
                // Když SaveMimeEml=false, ale extrahujeme přílohy, stejně uložíme dočasně message.eml
                // (kvůli MimeKit) – po extrakci případně smažeme.
                await SaveStreamToFileAsync(mimeContent, emlPath, cancellationToken).ConfigureAwait(false);

                Log.Debug($"EML uložen: {emlPath}");
            }
            // 3) vytáhne přílohy přes MimeKit
            if (_SaveAttachmentsWithMimeKit)
            {
                var attachDir = Path.Combine(folderPath, "attachments");
                Directory.CreateDirectory(attachDir);

                // Je-li emlPath null, načteme z paměti; jinak z disku
                if (!string.IsNullOrEmpty(emlPath))
                {
                    // Načtení z uloženého .eml – spolehlivé i pro neseekovatelný stream
                    using var fs = File.OpenRead(emlPath);
                    var message = await MimeMessage.LoadAsync(fs, cancellationToken).ConfigureAwait(false);
                    await ExtractAttachmentsAsync(message, attachDir, attachments, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Fallback (neměl by nastat, necháváme pro úplnost)
                    using var buffer = new MemoryStream();
                    await mimeContent.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                    buffer.Position = 0;
                    var message = await MimeMessage.LoadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    await ExtractAttachmentsAsync(message, attachDir, attachments, cancellationToken).ConfigureAwait(false);
                }
            }

            // 4) Pokud nechtěl uživatel ukládat EML (SaveMimeEml=false) a jen jsme ho potřebovali pro přílohy → smažeme
            if (!_SaveMimeEml && _SaveAttachmentsWithMimeKit && emlPath != null)
            {
                try
                {
                    File.Delete(emlPath);
                    Log.Debug("Dočasný EML smazán (SaveMimeEml=false).");
                    emlPath = null;
                }
                catch (Exception e)
                {
                    Log.Warn($"Dočasný EML se nepodařilo smazat: {e.Message}");
                }
            }
            // 5) Hotovo – výsledek
            Log.Info($"Uloženo: folder='{folderPath}', eml='{emlPath ?? "(neuložen)"}', příloh={attachments.Count}");
            return new EmailSaveResult
            {
                FolderPath = folderPath,
                EmlPath = emlPath,
                Attachments = attachments.AsReadOnly(),
                SafeFolderName = Path.GetFileName(folderPath),
                WasSkipped = false
            };
        }

        //Pomocne metody
        private static string ShortHash(string? input)
        {
            if (string.IsNullOrEmpty(input)) return "NOID";
            var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));
            // prvních 4 bajty → 8 hex znaků
            return BitConverter.ToString(bytes, 0, 4).Replace("-", "");
        }
        private static string EnsureUniqueFolderPath(string root, string folderName)
        {
            var candidate = Path.Combine(root, folderName);
            if (!Directory.Exists(candidate)) return candidate;

            for (int i = 1; ; i++)
            {
                var alt = Path.Combine(root, $"{folderName}-{i}");
                if (!Directory.Exists(alt)) return alt;
            }
        }

        // Uloží přílohy z MimeMessage do cílové složky.
        // - Vytváří bezpečné názvy souborů (FilenameSanitizer) a řeší duplicity (-1, -2…)
        // - Do 'outputPaths' přidává plné cesty uložených příloh.
        private static async Task ExtractAttachmentsAsync(
            MimeMessage message,
            string attachmentsDir,
            List<string> outputPaths,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(attachmentsDir);

            foreach (var entity in message.Attachments)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Zkus zjistit původní název; když není, dáme "attachment"
                var rawName =
                    (entity.ContentDisposition?.FileName) ??
                    (entity.ContentType?.Name) ??
                    "attachment";

                // Bezpečný název souboru
                var safeName = FilenameSanitizer.SanitizeFileName(rawName, maxLength: 120);
                var savePath = EnsureUniqueFilePath(attachmentsDir, safeName);

                // Dva případy: .eml (MessagePart) vs běžná příloha (MimePart)
                if (entity is MessagePart rfc822)
                {
                    // Pro .eml zajistíme příponu .eml
                    if (!savePath.EndsWith(".eml", StringComparison.OrdinalIgnoreCase))
                        savePath += ".eml";

                    await using var fs = File.Create(savePath);
                    await rfc822.Message.WriteToAsync(fs, cancellationToken).ConfigureAwait(false);
                    outputPaths.Add(savePath);
                }
                else if (entity is MimePart part)
                {
                    await using var fs = File.Create(savePath);

                    // MimeKit nemá DecodeToAsync; je to sync operace
                    // → pro jednoduchost použijeme sync volání (je to I/O bound)
                    part.Content?.DecodeTo(fs);

                    outputPaths.Add(savePath);
                }
            }
        }

        // Vrátí unikátní cestu k souboru: pokud existuje, přidává -1, -2, ...
        private static string EnsureUniqueFilePath(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            if (!File.Exists(path)) return path;

            var ext = Path.GetExtension(fileName);
            var baseName = Path.GetFileNameWithoutExtension(fileName);

            for (int i = 1; ; i++)
            {
                var candidate = Path.Combine(directory, $"{baseName}-{i}{ext}");
                if (!File.Exists(candidate)) return candidate;
            }
        }

        // (volitelně) pokud ještě nemáš → zkus vrátit stream na začátek (jen když to jde)
        private static void SeekableTryRewind(Stream stream)
        {
            if (stream.CanSeek)
            {
                try { stream.Position = 0; } catch { /* ignore */ }
            }
        }

        private static async Task SaveStreamToFileAsync(Stream input, string path, CancellationToken ct)
        {
            // Nevracíme stream na začátek – caller ho vlastní. Tohle jen vytvoří kopii do souboru.
            input.SeekableTryRewind(); // benign – zkusíme, když to jde
            using var fs = File.Create(path);
            await input.CopyToAsync(fs, 81920, ct).ConfigureAwait(false);
        }
        
    }
    internal static class StreamExtensions
    {
        /// <summary>
        /// Pokud je stream seekovatelný, vrátí pozici na začátek. V opačném případě nic nedělá.
        /// </summary>
        public static void SeekableTryRewind(this Stream stream)
        {
            if (stream.CanSeek)
            {
                try { stream.Position = 0; } catch { /* ignore */ }
            }
        }
    }
}
