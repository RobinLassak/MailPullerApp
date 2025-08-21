using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using log4net;
using MailPullerApp.Auth;
using MailPullerApp.Services.Graph.DTO;
using MailPullerApp.Services.Graph.Internal;

namespace MailPullerApp.Services.Graph
{
    // Implementace IGraphMailClient
    // Pomocí MS Graph vyčte po stránkách přijaté e-maily ze zadané schránky (s filtrem od data a řazením), a pro dané ID umí stáhnout MIME/EML stream
    // Vyuziva MSAL Token
    internal class GraphMailClient : IGraphMailClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenProvider _tokenProvider;
        private readonly string _mailboxAddress;
        private readonly string _selectFields;
        private readonly bool _useDelta;
        private static readonly ILog Log = LogManager.GetLogger(typeof(GraphMailClient));

        private const string DefaultScopes = GraphApiHelper.DefaultScope;

        public GraphMailClient(ITokenProvider tokenProvider, string mailboxAddress, string selectFields, bool useDelta)
        {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _mailboxAddress = mailboxAddress ?? throw new ArgumentNullException(nameof(mailboxAddress));
            _selectFields = selectFields ?? throw new ArgumentNullException(nameof(selectFields));
            _useDelta = useDelta;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ODataList<GraphMailItemDTO>> GetMessagesAsync(
            string folder,
            DateTimeOffset? startDate = null,
            int pageSize = 50,
            string? nextLink = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Pokud máme nextLink, použijeme ho přímo
                if (!string.IsNullOrEmpty(nextLink))
                {
                    return await GetMessagesFromUrlAsync(nextLink, cancellationToken);
                }

                // Sestavíme URL pro první stránku
                var url = BuildMessagesUrl(folder, startDate, pageSize);
                return await GetMessagesFromUrlAsync(url, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Chyba při získávání seznamu e-mailů: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Stream> GetMessageMimeAsync(string messageId, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await _tokenProvider.GetAppTokenAsync(new[] { DefaultScopes }, cancellationToken);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = GraphApiHelper.BuildMessageMimeUrl(_mailboxAddress, messageId);
                
                Log.Debug($"Stahuji MIME obsah pro zprávu: {messageId}");
                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    Log.Error($"Chyba při stahování MIME obsahu: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"Graph API vrátilo chybu: {response.StatusCode} - {errorContent}");
                }

                return await response.Content.ReadAsStreamAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Chyba při stahování MIME obsahu zprávy {messageId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<string?> GetDeltaLinkAsync(string folder, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = BuildDeltaUrl(folder);
                var result = await GetMessagesFromUrlAsync(url, cancellationToken);
                return result.GetDeltaLink();
            }
            catch (Exception ex)
            {
                Log.Error($"Chyba při získávání delta linku: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<ODataList<GraphMailItemDTO>> GetDeltaChangesAsync(string deltaLink, CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetMessagesFromUrlAsync(deltaLink, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Chyba při získávání delta změn: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<ODataList<GraphMailItemDTO>> GetMessagesFromUrlAsync(string url, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetAppTokenAsync(new[] { DefaultScopes }, cancellationToken);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Log.Debug($"Volám Graph API: {url}");
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Log.Error($"Graph API vrátilo chybu: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Graph API vrátilo chybu: {response.StatusCode} - {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var odataList = JsonSerializer.Deserialize<ODataList<GraphMessageDTO>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (odataList == null)
            {
                Log.Warn("Graph API vrátilo null odpověď");
                return ODataList<GraphMailItemDTO>.CreateEmpty();
            }

            // Převedeme GraphMessageDTO na GraphMailItemDTO
            var result = new ODataList<GraphMailItemDTO>
            {
                ODataContext = odataList.ODataContext,
                ODataNextLink = odataList.ODataNextLink,
                ODataDeltaLink = odataList.ODataDeltaLink,
                Value = odataList.Value.Select(GraphMailItemDTO.FromGraphMessage).ToList()
            };

            Log.Debug($"Získáno {result.Count} e-mailů, další stránka: {result.HasNextPage}");
            return result;
        }

        private string BuildMessagesUrl(string folder, DateTimeOffset? startDate, int pageSize)
        {
            var baseUrl = GraphApiHelper.BuildUserMessagesUrl(_mailboxAddress, folder);
            
            var parameters = new List<(string key, string value)>
            {
                ("$filter", GraphApiHelper.BuildFilterString(startDate)),
                ("$select", _selectFields),
                ("$top", pageSize.ToString()),
                ("$orderby", "receivedDateTime desc")
            };

            var queryString = GraphApiHelper.BuildQueryString(parameters.ToArray());
            return $"{baseUrl}?{queryString}";
        }

        private string BuildDeltaUrl(string folder)
        {
            var baseUrl = GraphApiHelper.BuildUserMessagesUrl(_mailboxAddress, folder) + "/delta";
            
            var parameters = new List<(string key, string value)>
            {
                ("$select", _selectFields)
            };

            var queryString = GraphApiHelper.BuildQueryString(parameters.ToArray());
            return $"{baseUrl}?{queryString}";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
