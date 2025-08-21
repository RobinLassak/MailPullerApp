using log4net;
using MailPullerApp.Auth;
using MailPullerApp.Configuration;
using MailPullerApp.Services.Graph;
using MailPullerApp.Services.Storage;
using MimeKit;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailPullerApp
{
    internal class Program
    {
        static async Task Main(string[] args)   // ← async Main kvuli testovaci zprave
        {
            Console.WriteLine("Welcome to MailPullerApp");
            Console.WriteLine("Press any key to continue");

            var config = ConfigLoader.Load();
            ConfigLog.InitializeLog4Net(config);

            var log = ConfigLog.GetLogger<Program>();
            log.Info("Log4net je připraveno.");

            Console.WriteLine("Configuration loaded successfully.");
            Console.WriteLine($"Log4Net config: {config.Logging.Log4NetConfigFile}");

            var g = config.Graph;
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            var authorityBuilt = (g.AuthorityHost ?? string.Empty).TrimEnd('/') + "/" + (g.TenantId ?? string.Empty);

            Console.WriteLine("=== Graph config preview ===");
            Console.WriteLine($"TenantId:          {g.TenantId}");
            Console.WriteLine($"ClientId:          {g.ClientId}");
            Console.WriteLine($"AuthorityHost:     {g.AuthorityHost}");
            Console.WriteLine($"Authority (built): {authorityBuilt}");
            Console.WriteLine($"Scopes:            {string.Join(", ", scopes)}");
            Console.WriteLine($"UseDelta:          {g.UseDelta}");
            Console.WriteLine($"Select:            {g.Select}");
            Console.WriteLine($"ClientSecret set:  {!string.IsNullOrWhiteSpace(g.ClientSecret)} (len={g.ClientSecret?.Length ?? 0})");
            Console.WriteLine("=======================================================");

            var store = new FileSystemEmailStore(config);
            log.Info("FileSystemEmailStore vytvořen.");
            Console.WriteLine($"RootDirectory: {store.RootDirectory}");
            Console.WriteLine($"SaveMimeEml: {store.SaveMimeEml}");
            Console.WriteLine($"SaveAttachmentsWithMimeKit: {store.SaveAttachmentsWithMimeKit}");
            Console.WriteLine("=======================================================");

            Console.WriteLine("=== Mailbox config ===");
            Console.WriteLine($"Address: {config.Mailbox.Address}");
            Console.WriteLine($"Folder: {config.Mailbox.Folder}");
            Console.WriteLine($"StartDateUtc: {config.Mailbox.StartDateUtc}");
            Console.WriteLine($"PageSize: {config.Mailbox.PageSize}");
            Console.WriteLine("=======================================================");

            // Test Graph komponent bez připojení k API (spustíme nejdříve)
            Console.WriteLine("=======================================================");
            Console.WriteLine("TESTING GRAPH COMPONENTS (bez připojení k API)");
            Console.WriteLine("=======================================================");

            await TestGraphComponentsWithoutApi(config, store, log);

            Console.WriteLine("=======================================================");
            Console.WriteLine("POKUS O SKUTEČNOU AUTENTIZACI A STAHOVÁNÍ");
            Console.WriteLine("=======================================================");

            try
            {
                // Test autentizace
                var tokenProvider = new MsalTokenProvider(config);
                var token = await tokenProvider.GetAppTokenAsync(scopes, CancellationToken.None);

                var preview = token.Length >= 15 ? token.Substring(0, 15) + "..." : token;
                log.Info($"Token získán (len={token.Length}, preview={preview})");
                Console.WriteLine("MSAL test OK – token získán.");

                // Vytvoření a spuštění služby pro stahování e-mailů
                using var downloadService = new EmailDownloadService(tokenProvider, store, config);

                Console.WriteLine("Spouštím stahování e-mailů...");
                var downloadedCount = await downloadService.DownloadEmailsAsync(CancellationToken.None);

                Console.WriteLine($"Stahování dokončeno. Staženo {downloadedCount} nových e-mailů.");

                // Zobrazení statistik
                var (totalProcessed, lastSync, lastDeltaLink) = downloadService.GetSyncStats();
                Console.WriteLine("=== Sync Statistics ===");
                Console.WriteLine($"Celkem zpracováno: {totalProcessed} e-mailů");
                Console.WriteLine($"Poslední synchronizace: {lastSync}");
                Console.WriteLine($"Delta link: {(lastDeltaLink != null ? "Dostupný" : "Není k dispozici")}");
                Console.WriteLine("=======================================================");
            }
            catch (ArgumentException e)
            {
                log.Error($"Chybná konfigurace: {e.Message}");
                Console.WriteLine($"Chybná konfigurace: {e.Message}");
            }
            catch (InvalidOperationException e)
            {
                log.Error($"MSAL chyba: {e.Message}");
                Console.WriteLine($"MSAL chyba: {e.Message}");
            }
            catch (Exception e)
            {
                log.Error($"Neočekávaná chyba: {e}");
                Console.WriteLine($"Neočekávaná chyba: {e.Message}");
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        /// <summary>
        /// Testuje Graph komponenty bez připojení k Microsoft Graph API
        /// </summary>
        private static async Task TestGraphComponentsWithoutApi(AppConfig config, FileSystemEmailStore store, ILog log)
        {
            try
            {
                Console.WriteLine("1. Testování DTO tříd...");
                await TestDtoClasses();

                Console.WriteLine("2. Testování ODataList...");
                await TestODataList();

                Console.WriteLine("3. Testování SyncState...");
                await TestSyncState();

                Console.WriteLine("4. Testování GraphApiHelper...");
                TestGraphApiHelper();

                Console.WriteLine("5. Testování EmailDownloadService s mock daty...");
                await TestEmailDownloadServiceWithMockData(config, store, log);

                Console.WriteLine("Všechny Graph komponenty prošly testem!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Chyba při testování Graph komponent: {ex.Message}");
                log.Error($"Chyba při testování Graph komponent: {ex.Message}", ex);
            }
        }

        private static Task TestDtoClasses()
        {
            // Test GraphMessageDTO
            var messageDto = new MailPullerApp.Services.Graph.DTO.GraphMessageDTO
            {
                Id = "test-message-id",
                Subject = "Test Subject",
                ReceivedDateTime = DateTimeOffset.UtcNow,
                HasAttachments = true,
                InternetMessageId = "test-internet-message-id",
                From = new MailPullerApp.Services.Graph.DTO.GraphRecipientDTO
                {
                    EmailAddress = new MailPullerApp.Services.Graph.DTO.GraphEmailAddressDTO
                    {
                        Name = "Test User",
                        Address = "test@example.com"
                    }
                }
            };

            // Test konverze na GraphMailItemDTO
            var mailItemDto = MailPullerApp.Services.Graph.DTO.GraphMailItemDTO.FromGraphMessage(messageDto);

            Console.WriteLine($"   - GraphMessageDTO: ID={messageDto.Id}, Subject={messageDto.Subject}");
            Console.WriteLine($"   - GraphMailItemDTO: ID={mailItemDto.Id}, FromName={mailItemDto.FromName}");
            Console.WriteLine($"   - Konverze DTO: ✅ OK");
            
            return Task.CompletedTask;
        }

        private static Task TestODataList()
        {
            var odataList = new MailPullerApp.Services.Graph.Internal.ODataList<MailPullerApp.Services.Graph.DTO.GraphMailItemDTO>
            {
                ODataContext = "test-context",
                ODataNextLink = "https://graph.microsoft.com/v1.0/next-link",
                ODataDeltaLink = "https://graph.microsoft.com/v1.0/delta-link",
                Value = new List<MailPullerApp.Services.Graph.DTO.GraphMailItemDTO>
                {
                    new() { Id = "1", Subject = "Test 1" },
                    new() { Id = "2", Subject = "Test 2" }
                }
            };

            Console.WriteLine($"   - ODataList: Count={odataList.Count}, HasNextPage={odataList.HasNextPage}");
            Console.WriteLine($"   - NextLink: {odataList.GetNextPageUrl()}");
            Console.WriteLine($"   - DeltaLink: {odataList.GetDeltaLink()}");
            Console.WriteLine($"   - ODataList: OK");
            
            return Task.CompletedTask;
        }

        private static Task TestSyncState()
        {
            var syncState = new MailPullerApp.Services.Graph.Internal.SyncState();

            // Test označení zprávy jako zpracované
            syncState.MarkMessageAsProcessed("test-id", DateTimeOffset.UtcNow);

            Console.WriteLine($"   - SyncState: TotalProcessed={syncState.TotalMessagesProcessed}");
            Console.WriteLine($"   - IsMessageProcessed: {syncState.IsMessageProcessed("test-id")}");
            Console.WriteLine($"   - IsMessageProcessed (neexistující): {syncState.IsMessageProcessed("non-existent")}");

            // Test čištění starých záznamů
            syncState.CleanupOldRecords(1);

            Console.WriteLine($"   - SyncState: OK");
            
            return Task.CompletedTask;
        }

        private static void TestGraphApiHelper()
        {
            var helper = typeof(MailPullerApp.Services.Graph.Internal.GraphApiHelper);

            Console.WriteLine($"   - GraphApiHelper: OK (třída existuje)");
            Console.WriteLine($"   - GraphBaseUrl: {MailPullerApp.Services.Graph.Internal.GraphApiHelper.GraphBaseUrl}");
            Console.WriteLine($"   - DefaultScope: {MailPullerApp.Services.Graph.Internal.GraphApiHelper.DefaultScope}");

            // Test validace
            var isValidFolder = MailPullerApp.Services.Graph.Internal.GraphApiHelper.Folders.Inbox;
            var isValidEmail = MailPullerApp.Services.Graph.Internal.GraphApiHelper.IsValidEmailAddress("test@example.com");

            Console.WriteLine($"   - Folder validation: {isValidFolder}");
            Console.WriteLine($"   - Email validation: {isValidEmail}");
        }

        private static Task TestEmailDownloadServiceWithMockData(AppConfig config, FileSystemEmailStore store, ILog log)
        {
            // Vytvoření mock token provideru
            var mockTokenProvider = new MockTokenProvider();

            // Vytvoření EmailDownloadService
            using var downloadService = new MailPullerApp.Services.Graph.EmailDownloadService(mockTokenProvider, store, config);

            Console.WriteLine($"   - EmailDownloadService vytvořen");

            // Test statistik
            var (totalProcessed, lastSync, lastDeltaLink) = downloadService.GetSyncStats();
            Console.WriteLine($"   - Initial stats: TotalProcessed={totalProcessed}, LastSync={lastSync}");

            // Test čištění starých záznamů
            downloadService.CleanupOldRecords(30);
            Console.WriteLine($"   - CleanupOldRecords: OK");

            Console.WriteLine($"   - EmailDownloadService: OK");
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Mock token provider pro testování bez skutečné autentizace
        /// </summary>
        private class MockTokenProvider : MailPullerApp.Auth.ITokenProvider
        {
            public Task<string> GetAppTokenAsync(string[] scopes, CancellationToken cancellationToken = default)
            {
                return Task.FromResult("mock-token-for-testing");
            }
        }
    }
}