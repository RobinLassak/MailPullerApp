# MailPullerApp

Aplikace pro stahování e-mailů z Microsoft Graph API a jejich ukládání do souborového systému.

## Funkce

- Stahování e-mailů z Microsoft Graph API
- Ukládání e-mailů do strukturovaných složek
- Extrakce a ukládání příloh
- Delta synchronizace pro efektivní stahování
- Logování pomocí log4net
- Konfigurace přes appsettings.json
- Správa stavu synchronizace mezi běhy

## Konfigurace

### 1. Microsoft Graph API nastavení

V souboru `appsettings.json` nastavte skutečné hodnoty:

```json
{
  "Graph": {
    "TenantId": "YOUR-ACTUAL-TENANT-ID",
    "ClientId": "YOUR-ACTUAL-CLIENT-ID", 
    "ClientSecret": "env:GRAPH_CLIENT_SECRET",
    "AuthorityHost": "https://login.microsoftonline.com/",
    "UseDelta": true,
    "Select": "id,subject,receivedDateTime,hasAttachments,internetMessageId,from,toRecipients,importance,isRead,conversationId"
  }
}
```

### 2. Nastavení poštovní schránky

```json
{
  "Mailbox": {
    "Address": "your-actual-email@yourdomain.com",
    "Folder": "Inbox",
    "StartDateUtc": "2025-01-01T00:00:00Z",
    "PageSize": 50
  }
}
```

### 3. Nastavení výstupu

```json
{
  "Output": {
    "RootDirectory": "C:\\MailDownload",
    "SaveMimeEml": true,
    "SaveAttachmentsWithMimeKit": true
  }
}
```

### 4. Nastavení stavu synchronizace

```json
{
  "State": {
    "CheckpointFile": "state.json"
  }
}
```

## Instalace a spuštění

### Požadavky
- .NET 8.0 Runtime
- Microsoft Graph API aplikace s příslušnými oprávněními

### Kroky pro nastavení

1. **Vytvoření Azure AD aplikace:**
   - Přejděte na Azure Portal → Azure Active Directory → App registrations
   - Vytvořte novou aplikaci
   - Získejte Client ID a Tenant ID
   - Vytvořte Client Secret

2. **Nastavení oprávnění:**
   - Mail.Read (pro čtení e-mailů)
   - Mail.ReadWrite (pokud potřebujete i zapisovat)

3. **Konfigurace aplikace:**
   - Nastavte TenantId a ClientId v appsettings.json
   - Nastavte proměnnou prostředí GRAPH_CLIENT_SECRET s vaším Client Secret

4. **Spuštění aplikace:**
   ```bash
   dotnet run
   ```

## Použití hlavní služby

Aplikace používá `EmailDownloadService` jako hlavní službu pro koordinaci stahování a ukládání e-mailů:

```csharp
// Vytvoření hlavní služby
using var downloadService = new EmailDownloadService(
    tokenProvider,    // ITokenProvider pro autentizaci
    emailStore,       // IEmailStore pro ukládání
    config            // AppConfig s konfigurací
);

// Spuštění stahování
var downloadedCount = await downloadService.DownloadEmailsAsync();

Console.WriteLine($"Staženo {downloadedCount} nových e-mailů");
```

## Struktura výstupu

E-maily se ukládají do následující struktury:

```
RootDirectory/
├── 20250115_143022__Test Subject__A1B2C3D4/
│   ├── message.eml
│   └── attachments/
│       ├── document.pdf
│       └── image.jpg
├── 20250115_143045__Another Email__E5F6G7H8/
│   └── message.eml
```

## Logování

Aplikace používá log4net pro logování:
- Konzolový výstup
- Souborový výstup do `logs/app.log`
- Rotace log souborů (max 1MB, 5 záloh)

## Delta synchronizace

Aplikace podporuje delta synchronizaci pro efektivní stahování pouze nových e-mailů:
- První běh stáhne všechny e-maily od zadaného data
- Následující běhy stáhnou pouze nové e-maily
- Stav synchronizace se ukládá do `state.json`

## Architektura

```
MailPullerApp/
├── Auth/                    # Autentizace pomocí MSAL
├── Configuration/           # Načítání konfigurace
├── Models/                  # Konfigurační modely
├── Services/
│   ├── Graph/                   # Microsoft Graph API klient
│   │   ├── DTO/                 # Data Transfer Objects
│   │   ├── Internal/            # Interní pomocné třídy
│   │   ├── EmailDownloadService.cs # Hlavní služba pro stahování e-mailů
│   │   ├── GraphMailClient.cs   # HTTP klient pro Graph API
│   │   └── IGraphMailClient.cs  # Rozhraní pro Graph klienta
│   └── Storage/                 # Ukládání e-mailů
│       ├── FileSystemEmailStore.cs # Ukládání do souborového systému
│       └── IEmailStore.cs       # Rozhraní pro ukládání
└── Program.cs                  # Hlavní vstupní bod
```

## Řešení problémů

### Chyba autentizace
- Zkontrolujte TenantId, ClientId a ClientSecret
- Ověřte, že aplikace má správná oprávnění
- Zkontrolujte, že Client Secret není vypršel

### Chyba při stahování e-mailů
- Ověřte, že uživatel má přístup k poštovní schránce
- Zkontrolujte název složky (Inbox, Sent Items, atd.)
- Ověřte, že StartDateUtc je ve správném formátu

### Problémy s ukládáním
- Zkontrolujte oprávnění k zápisu do RootDirectory
- Ověřte dostatek místa na disku
- Zkontrolujte log soubory pro detailní chyby

## Licence

Tento projekt je vytvořen jako testovací úkol pro Allium.
