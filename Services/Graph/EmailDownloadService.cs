using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MailPullerApp.Auth;
using MailPullerApp.Configuration;
using MailPullerApp.Services.Graph.DTO;
using MailPullerApp.Services.Graph.Internal;
using MailPullerApp.Services.Storage;

namespace MailPullerApp.Services.Graph
{
    // Hlavní služba pro stahování e-mailů z Microsoft Graph API
    // Koordinuje celý proces: získání seznamu e-mailů, stahování MIME obsahu a ukládání
    internal class EmailDownloadService : IDisposable
    {
        private readonly IGraphMailClient _graphClient;
        private readonly IEmailStore _emailStore;
        private SyncState _syncState;
        private readonly string _stateFilePath;
        private readonly string _mailboxAddress;
        private readonly string _folder;
        private readonly DateTimeOffset? _startDate;
        private readonly int _pageSize;
        private readonly bool _useDelta;
        private static readonly ILog Log = LogManager.GetLogger(typeof(EmailDownloadService));

        public EmailDownloadService(
            ITokenProvider tokenProvider,
            IEmailStore emailStore,
            AppConfig config)
        {
            _emailStore = emailStore ?? throw new ArgumentNullException(nameof(emailStore));
            
            // Vytvoření Graph klienta
            _graphClient = new GraphMailClient(
                tokenProvider,
                config.Mailbox.Address,
                config.Graph.Select,
                config.Graph.UseDelta);

            // Načtení konfigurace
            _mailboxAddress = config.Mailbox.Address;
            _folder = config.Mailbox.Folder;
            _startDate = DateTimeOffset.TryParse(config.Mailbox.StartDateUtc, out var startDate) ? startDate : null;
            _pageSize = config.Mailbox.PageSize;
            _useDelta = config.Graph.UseDelta;

            // Inicializace stavu synchronizace
            _stateFilePath = config.State.CheckpointFile;
            _syncState = new SyncState(); // Vytvoříme prázdný stav, načteme ho později
        }

        /// <summary>
        /// Spustí proces stahování e-mailů
        /// </summary>
        public async Task<int> DownloadEmailsAsync(CancellationToken cancellationToken = default)
        {
            Log.Info($"Spouštím stahování e-mailů pro {_mailboxAddress}, složka: {_folder}");
            
            // Načteme stav synchronizace
            _syncState = await SyncState.LoadAsync(_stateFilePath);
            
            var totalDownloaded = 0;
            var hasMorePages = true;
            string? nextLink = null;

            try
            {
                // Pokud používáme delta sync a máme delta link, použijeme ho
                if (_useDelta && !string.IsNullOrEmpty(_syncState.LastDeltaLink))
                {
                    Log.Info("Používám delta synchronizaci");
                    return await DownloadDeltaChangesAsync(cancellationToken);
                }

                // Jinak stahujeme všechny e-maily od zadaného data
                while (hasMorePages && !cancellationToken.IsCancellationRequested)
                {
                    Log.Debug($"Stahuji stránku e-mailů (nextLink: {nextLink != null})");
                    
                    var messages = await _graphClient.GetMessagesAsync(
                        _folder,
                        _startDate,
                        _pageSize,
                        nextLink,
                        cancellationToken);

                    if (messages.IsEmpty)
                    {
                        Log.Info("Žádné další e-maily k stažení");
                        break;
                    }

                    var downloadedInPage = await ProcessMessagesAsync(messages.Value, cancellationToken);
                    totalDownloaded += downloadedInPage;

                    Log.Info($"Stránka zpracována: {downloadedInPage} nových e-mailů, celkem: {totalDownloaded}");

                    // Kontrola další stránky
                    hasMorePages = messages.HasNextPage;
                    nextLink = messages.GetNextPageUrl();

                    // Pokud používáme delta sync, uložíme delta link
                    if (_useDelta && messages.HasDeltaLink)
                    {
                        _syncState.UpdateDeltaLink(messages.GetDeltaLink());
                    }
                }

                // Uložení stavu
                await _syncState.SaveAsync(_stateFilePath);
                
                Log.Info($"Stahování dokončeno. Celkem staženo: {totalDownloaded} e-mailů");
                return totalDownloaded;
            }
            catch (Exception ex)
            {
                Log.Error($"Chyba při stahování e-mailů: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Stáhne změny pomocí delta synchronizace
        /// </summary>
        private async Task<int> DownloadDeltaChangesAsync(CancellationToken cancellationToken)
        {
            Log.Info("Stahuji delta změny");
            
            var totalDownloaded = 0;
            var hasMorePages = true;
            string? deltaLink = _syncState.LastDeltaLink;

            while (hasMorePages && !cancellationToken.IsCancellationRequested)
            {
                var changes = await _graphClient.GetDeltaChangesAsync(deltaLink ?? throw new InvalidOperationException("Delta link is null"), cancellationToken);
                
                if (changes.IsEmpty)
                {
                    Log.Info("Žádné delta změny");
                    break;
                }

                var downloadedInPage = await ProcessMessagesAsync(changes.Value, cancellationToken);
                totalDownloaded += downloadedInPage;

                Log.Info($"Delta stránka zpracována: {downloadedInPage} nových e-mailů, celkem: {totalDownloaded}");

                hasMorePages = changes.HasNextPage;
                deltaLink = changes.GetNextPageUrl();

                // Uložíme nový delta link
                if (changes.HasDeltaLink)
                {
                    _syncState.UpdateDeltaLink(changes.GetDeltaLink());
                }
            }

            await _syncState.SaveAsync(_stateFilePath);
            return totalDownloaded;
        }

        /// <summary>
        /// Zpracuje seznam e-mailů - stáhne MIME obsah a uloží je
        /// </summary>
        private async Task<int> ProcessMessagesAsync(IEnumerable<GraphMailItemDTO> messages, CancellationToken cancellationToken)
        {
            var downloaded = 0;

            foreach (var message in messages)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Kontrola, zda už byla zpráva zpracována
                    if (_syncState.IsMessageProcessed(message.Id))
                    {
                        Log.Debug($"Zpráva {message.Id} již byla zpracována, přeskakuji");
                        continue;
                    }

                    Log.Debug($"Zpracovávám zprávu: {message.Id} - {message.Subject}");

                    // Stáhnutí MIME obsahu
                    using var mimeStream = await _graphClient.GetMessageMimeAsync(message.Id, cancellationToken);

                    // Vytvoření metadat
                    var metadata = new EmailMetadata
                    {
                        Subject = message.Subject ?? "(bez předmětu)",
                        ReceivedDateUtc = message.ReceivedDateTime?.UtcDateTime ?? DateTime.UtcNow,
                        InternetMessageId = message.InternetMessageId ?? Guid.NewGuid().ToString("N"),
                        FromName = message.FromName ?? "Neznámý odesílatel",
                        FromEmail = message.FromEmail ?? "unknown@example.com"
                    };

                    // Uložení e-mailu
                    var result = await _emailStore.SaveEmailAsync(metadata, mimeStream, cancellationToken);

                    if (!result.WasSkipped)
                    {
                        // Označení jako zpracovaná
                        _syncState.MarkMessageAsProcessed(message.Id, message.ReceivedDateTime ?? DateTimeOffset.UtcNow);
                        downloaded++;

                        Log.Debug($"E-mail uložen: {result.FolderPath}, příloh: {result.Attachments.Count}");
                    }
                    else
                    {
                        Log.Debug($"E-mail přeskočen: {message.Id}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Chyba při zpracování zprávy {message.Id}: {ex.Message}", ex);
                    // Pokračujeme s další zprávou
                }
            }

            return downloaded;
        }

        /// <summary>
        /// Vyčistí staré záznamy ze stavu synchronizace
        /// </summary>
        public void CleanupOldRecords(int daysToKeep = 30)
        {
            _syncState.CleanupOldRecords(daysToKeep);
        }

        /// <summary>
        /// Získá statistiky synchronizace
        /// </summary>
        public (int TotalProcessed, DateTimeOffset? LastSync, string? LastDeltaLink) GetSyncStats()
        {
            return (_syncState.TotalMessagesProcessed, _syncState.LastSyncTime, _syncState.LastDeltaLink);
        }

        public void Dispose()
        {
            _graphClient?.Dispose();
        }
    }
}
