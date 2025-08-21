using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailPullerApp.Services.Graph.DTO;
using MailPullerApp.Services.Graph.Internal;

namespace MailPullerApp.Services.Graph
{
    // Rozhraní, které definuje, jak z Microsoft Graphu získat seznam e-mailů a stáhnout MIME obsah konkrétní zprávy, aby zbytek appky neřešil HTTP detaily.
    internal interface IGraphMailClient : IDisposable
    {
        /// <summary>
        /// Získá seznam e-mailů ze zadané složky s možností stránkování
        /// </summary>
        /// <param name="folder">Název složky (např. "Inbox", "Sent Items")</param>
        /// <param name="startDate">Počáteční datum pro filtrování</param>
        /// <param name="pageSize">Velikost stránky</param>
        /// <param name="nextLink">URL pro další stránku (null pro první stránku)</param>
        /// <param name="cancellationToken">Token pro zrušení operace</param>
        /// <returns>Seznam e-mailů s informacemi o stránkování</returns>
        Task<ODataList<GraphMailItemDTO>> GetMessagesAsync(
            string folder,
            DateTimeOffset? startDate = null,
            int pageSize = 50,
            string? nextLink = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stáhne MIME obsah konkrétní zprávy
        /// </summary>
        /// <param name="messageId">ID zprávy</param>
        /// <param name="cancellationToken">Token pro zrušení operace</param>
        /// <returns>Stream s MIME obsahem zprávy</returns>
        Task<Stream> GetMessageMimeAsync(
            string messageId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Získá delta link pro příští synchronizaci
        /// </summary>
        /// <param name="folder">Název složky</param>
        /// <param name="cancellationToken">Token pro zrušení operace</param>
        /// <returns>Delta link URL</returns>
        Task<string?> GetDeltaLinkAsync(
            string folder,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Získá změny od poslední synchronizace pomocí delta linku
        /// </summary>
        /// <param name="deltaLink">Delta link z předchozí synchronizace</param>
        /// <param name="cancellationToken">Token pro zrušení operace</param>
        /// <returns>Seznam změn s novým delta linkem</returns>
        Task<ODataList<GraphMailItemDTO>> GetDeltaChangesAsync(
            string deltaLink,
            CancellationToken cancellationToken = default);
    }
}
