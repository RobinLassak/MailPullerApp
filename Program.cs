using log4net;
using log4net.Config;
using MailPullerApp.Auth;
using MailPullerApp.Configuration;
using System.IO;
using System.Reflection;
using MailPullerApp.Services.Storage;

namespace MailPullerApp
{
    internal class Program
    {
        static void Main(string[] args)
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

            try
            {

                var tokenProvider = new MsalTokenProvider(config);

                
                var token = tokenProvider.GetAppTokenAsync(scopes, CancellationToken.None)
                                         .GetAwaiter().GetResult();

                
                var preview = token.Length >= 15 ? token.Substring(0, 15) + "..." : token;
                log.Info($"Token získán (len={token.Length}, preview={preview})");

                Console.WriteLine("MSAL test OK – token získán.");


            }
            catch (ArgumentException e)
            {
                // Chybějící/špatná konfigurace (Graph.TenantId je prázdné, atd.)
                log.Error($"Chybná konfigurace: {e.Message}");
                Console.WriteLine($"Chybná konfigurace: {e.Message}");
            }
            catch (InvalidOperationException e)
            {
                // MSAL service/client error (např. chybí admin consent, špatné přihlašovací údaje)
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
