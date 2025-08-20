using log4net;
using MailPullerApp.Auth;
using MailPullerApp.Configuration;
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
            Console.WriteLine("Welcome to my PullerApp");
            Console.WriteLine("Press any key to continue");

            var config = ConfigLoader.Load();
            ConfigLog.InitializeLog4Net(config);

            var log = ConfigLog.GetLogger<Program>();
            log.Info("Log4net je připraveno.");

            Console.WriteLine("Configuration loaded successfully.");
            Console.WriteLine($"Email: {config.Logging.Log4NetConfigFile}");

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

            // 1) Sestaveni testovací MimeMessage
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("Alice Tester", "alice@example.com"));
            msg.To.Add(new MailboxAddress("Bob Tester", "bob@example.com"));
            msg.Subject = "Hello world - Test2";

            var builder = new BodyBuilder { TextBody = "Test 2" };
            builder.Attachments.Add("Poznámka.txt", Encoding.UTF8.GetBytes("Ahoj toto je moje druha testovaci zprava"));
            msg.Body = builder.ToMessageBody();

            // 2) Zapis do streamu
            using var ms = new MemoryStream();
            await msg.WriteToAsync(ms);   
            ms.Position = 0;

            // 3) Metadata
            var meta = new EmailMetadata
            {
                Subject = msg.Subject ?? "(bez předmětu)",
                ReceivedDateUtc = DateTime.UtcNow,
                InternetMessageId = msg.MessageId ?? Guid.NewGuid().ToString("N"),
                FromName = "Alice Tester",
                FromEmail = "alice@example.com"
            };

            // 4) Ulozeni zpravy
            Console.WriteLine("=== Ukládání e-mailu ===");
            var result = await store.SaveEmailAsync(meta, ms);

            Console.WriteLine("=== Uloženo ===");
            Console.WriteLine($"Folder: {result.FolderPath}");                 
            Console.WriteLine($"EML:    {result.EmlPath ?? "(neuložen)"}");
            Console.WriteLine($"Příloh: {result.Attachments.Count}");
            foreach (var a in result.Attachments)
            {
                Console.WriteLine($" - {a}");
            }
            Console.WriteLine("=======================================================");

            try
            {
                var tokenProvider = new MsalTokenProvider(config);
                var token = await tokenProvider.GetAppTokenAsync(scopes, CancellationToken.None);  

                var preview = token.Length >= 15 ? token.Substring(0, 15) + "..." : token;
                log.Info($"Token získán (len={token.Length}, preview={preview})");
                Console.WriteLine("MSAL test OK – token získán.");
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
                log.Error($"Neočekávaná chyba při získání tokenu: {e}");
                Console.WriteLine($"Neočekávaná chyba: {e.Message}");
            }

            Console.ReadKey();
        }
    }
}
