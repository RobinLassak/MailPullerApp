using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MailPullerApp.Services.Graph.Internal
{
    // Umožní jednoduše stránkovat a ukládat stav pro příští běh
    // Obsahuje aktuální stránku položek a další metadata
    internal class ODataList<T>
    {
        [JsonPropertyName("@odata.context")]
        public string? ODataContext { get; set; }

        [JsonPropertyName("@odata.nextLink")]
        public string? ODataNextLink { get; set; }

        [JsonPropertyName("@odata.deltaLink")]
        public string? ODataDeltaLink { get; set; }

        [JsonPropertyName("value")]
        public List<T> Value { get; set; } = new List<T>();

        // Pomocné vlastnosti pro práci se stránkováním
        public bool HasNextPage => !string.IsNullOrEmpty(ODataNextLink);
        public bool HasDeltaLink => !string.IsNullOrEmpty(ODataDeltaLink);
        public int Count => Value?.Count ?? 0;
        public bool IsEmpty => Count == 0;

        // Metoda pro získání další stránky
        public string? GetNextPageUrl() => ODataNextLink;

        // Metoda pro získání delta linku (pro delta sync)
        public string? GetDeltaLink() => ODataDeltaLink;

        // Metoda pro vytvoření prázdného seznamu
        public static ODataList<T> CreateEmpty()
        {
            return new ODataList<T>
            {
                Value = new List<T>(),
                ODataContext = null,
                ODataNextLink = null,
                ODataDeltaLink = null
            };
        }
    }
}
