using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using log4net;

namespace MailPullerApp.Services.Graph.Internal
{
    // Třída pro správu stavu synchronizace mezi běhy aplikace
    internal class SyncState
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SyncState));
        
        public string? LastDeltaLink { get; set; }
        public DateTimeOffset? LastSyncTime { get; set; }
        public string? LastProcessedMessageId { get; set; }
        public int TotalMessagesProcessed { get; set; }
        public Dictionary<string, DateTimeOffset> ProcessedMessages { get; set; } = new Dictionary<string, DateTimeOffset>();

        // Načte stav ze souboru
        public static async Task<SyncState> LoadAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Info($"Soubor se stavem neexistuje: {filePath}");
                    return new SyncState();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var state = JsonSerializer.Deserialize<SyncState>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (state == null)
                {
                    Log.Warn("Nepodařilo se deserializovat stav synchronizace");
                    return new SyncState();
                }

                Log.Info($"Načten stav synchronizace: {state.TotalMessagesProcessed} zpráv, poslední sync: {state.LastSyncTime}");
                return state;
            }
            catch (Exception ex)
            {
                Log.Error($"Chyba při načítání stavu synchronizace: {ex.Message}", ex);
                return new SyncState();
            }
        }

        // Uloží stav do souboru
        public async Task SaveAsync(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json);
                Log.Debug($"Stav synchronizace uložen: {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Chyba při ukládání stavu synchronizace: {ex.Message}", ex);
            }
        }

        // Označí zprávu jako zpracovanou
        public void MarkMessageAsProcessed(string messageId, DateTimeOffset receivedDate)
        {
            ProcessedMessages[messageId] = receivedDate;
            LastProcessedMessageId = messageId;
            TotalMessagesProcessed++;
        }

        // Zkontroluje, zda byla zpráva již zpracována
        public bool IsMessageProcessed(string messageId)
        {
            return ProcessedMessages.ContainsKey(messageId);
        }

        // Vyčistí staré záznamy (starší než 30 dní)
        public void CleanupOldRecords(int daysToKeep = 30)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToKeep);
            var oldKeys = ProcessedMessages
                .Where(kvp => kvp.Value < cutoffDate)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldKeys)
            {
                ProcessedMessages.Remove(key);
            }

            if (oldKeys.Count > 0)
            {
                Log.Info($"Vyčištěno {oldKeys.Count} starých záznamů ze stavu synchronizace");
            }
        }

        // Aktualizuje delta link
        public void UpdateDeltaLink(string? deltaLink)
        {
            LastDeltaLink = deltaLink;
            LastSyncTime = DateTimeOffset.UtcNow;
        }
    }
}
