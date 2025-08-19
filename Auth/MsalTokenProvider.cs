using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailPullerApp.Auth
{
    internal class MsalTokenProvider : ITokenProvider
    {
        private readonly IConfidentialClientApplication app;
        public MsalTokenProvider(AppConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null.");
            }
            if(config.Graph == null)
            {
                throw new ArgumentException("Graph settings are not properly configured in the application configuration.");
            }
            var graphSettings = config.Graph;
            if (string.IsNullOrEmpty(graphSettings.TenantId))
            {
                throw new ArgumentException("TenantId is not set in the Graph settings.");
            }
            if (string.IsNullOrEmpty(graphSettings.ClientId))
            {
                throw new ArgumentException("ClientId is not set in the Graph settings.");
            }
            if (string.IsNullOrEmpty(graphSettings.ClientSecret))
            {
                throw new ArgumentException("ClientSecret is not set in the Graph settings.");
            }
            if (string.IsNullOrEmpty(graphSettings.AuthorityHost))
            {
                throw new ArgumentException("AutorityHost is not set in the Graph settings.");
            }

            var authority = graphSettings.AuthorityHost.TrimEnd('/') + "/" + graphSettings.TenantId;
            app = ConfidentialClientApplicationBuilder.Create(graphSettings.ClientId)
                .WithClientSecret(graphSettings.ClientSecret)
                .WithAuthority(new Uri(authority))
                .Build();
        }
        public async Task<string> GetAppTokenAsync(string[] scopes, CancellationToken cancellationToken = default)
        {
            // Implementace získání tokenu pomocí MSAL (Microsoft Authentication Library)
            // Tato metoda by měla vrátit platný token pro dané scope

            if (scopes == null || scopes.Length == 0)
            {
                scopes = new[] { "https://graph.microsoft.com/.default" };
            }

            try
            {
                var result = await app
                    .AcquireTokenForClient(scopes)
                    .ExecuteAsync(cancellationToken)
                    .ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (MsalServiceException e)
            {

                // Chyby služby (špatný tenant/app, chybějící admin consent, blokace…)
                throw new InvalidOperationException(
                    $"MSAL service error ({e.ErrorCode}). " +
                    $"Zkontroluj TenantId/ClientId/ClientSecret a admin consent. " +
                    $"Detail: {e.Message}",
                    e);
            }
            catch (MsalClientException e)
            {
                // Chyby na klientovi (síť, chybné authority apod.)
                throw new InvalidOperationException(
                    $"MSAL client error ({e.ErrorCode}). Detail: {e.Message}",
                    e);
            }

        }
    }
}
