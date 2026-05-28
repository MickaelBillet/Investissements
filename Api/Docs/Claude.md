# CLAUDE.md — Api (Azure Functions)

## 1. Rôle

Couche backend serverless entre le Google Apps Script et le Blazor WASM. Détient l'URL et le token de l'Apps Script Web App, expose des endpoints REST internes consommés uniquement par le frontend hébergé sur le même Azure Static Web Apps.

---

## 2. Stack

| Élément | Choix |
|---|---|
| Runtime | .NET 9, Azure Functions v4 isolated worker |
| Modèle HTTP | `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` |
| Tests | xUnit + Moq |
| Déploiement | Lié à Azure Static Web Apps (Managed Functions) |

> **Note CI :** Oryx (builder Azure SWA) supporte net8.0. Pour net9.0, prévoir un build explicite dans le pipeline si le déploiement échoue.

---

## 3. Structure du projet

```
Api/
├── InvestissementsDashboard.Api.csproj
├── Program.cs                  # Point d'entrée, DI
├── host.json                   # Config Azure Functions
├── local.settings.json         # Variables locales (gitignorées)
├── Functions/                  # Un fichier par endpoint
│   ├── AssetsFunction.cs
│   ├── GeographyFunction.cs
│   ├── McpFunction.cs             # Endpoint MCP POST /api/mcp
│   ├── PortfolioMetricsFunction.cs
│   └── SnapshotFunction.cs
├── Interfaces/                 # Interfaces des services
│   ├── IAppsScriptService.cs
│   ├── IAssetsService.cs
│   ├── IGeographyService.cs
│   ├── IPortfolioMetricsService.cs
│   ├── ISnapshotService.cs
│   └── Mcp/
│       └── IMcpService.cs         # Interface du handler JSON-RPC
├── JsonConverter/              # Converters System.Text.Json
│   ├── FlexibleIntConverter.cs
│   └── FlexibleStringConverter.cs
├── Mcp/
│   └── McpToolRegistry.cs         # Registre statique des 8 outils MCP
├── Services/
│   ├── AppsScriptService.cs
│   ├── AssetsService.cs
│   ├── GeographyService.cs
│   ├── PortfolioMetricsService.cs
│   ├── SnapshotService.cs
│   └── Mcp/
│       └── McpService.cs          # Handler JSON-RPC — route vers les services
└── Properties/
    └── AssemblyInfo.cs
```

Les modèles JSON-RPC (`JsonRpcRequest`, `JsonRpcResponse`, etc.) sont dans `Shared/Models/Mcp/McpModels.cs`.

---

## 4. Architecture — flux de données

**Flux dashboard (Blazor WASM) :**
```
Blazor WASM
    │ HTTP GET /api/...
    ▼
Azure Functions (C#)
    │ GET {APPS_SCRIPT_URL}?apiKey=...&service=X&action=Y
    ▼
Google Apps Script Web App
    │ lit Google Sheets DEST
    ▼
JSON response → désérialisé en DTO
```

**Flux MCP (Claude Code) :**
```
Claude Code (MCP client)
    │ POST /api/mcp  { "method": "tools/call", "params": { "name": "get_assets", ... } }
    ▼
McpFunction → McpService (JSON-RPC router)
    │ délègue au service métier existant
    ▼
IAssetsService / ISnapshotService / etc.
    │ appelle Apps Script (même flux que le dashboard)
    ▼
JSON response → sérialisé en McpContent
```

Les Azure Functions ne lisent **pas** directement Google Sheets — elles appellent uniquement l'Apps Script Web App.

---

## 5. Configuration

Les variables d'environnement sont injectées via `IConfiguration` (App Settings en prod, `local.settings.json` en dev) :

| Variable | Usage |
|---|---|
| `APPS_SCRIPT_URL` | URL du déploiement Web App Google Apps Script |
| `APPS_SCRIPT_API_KEY` | Token d'authentification (`apiKey`) de l'Apps Script |

Ne jamais lire ces valeurs autrement que via `IConfiguration` injecté.

---

## 6. Endpoints

| Méthode | Route | Source |
|---|---|---|
| GET | `/api/snapshot` | Apps Script `Snapshot.getLast` |
| GET | `/api/snapshot/history` | Apps Script `Snapshot.getHistory` |
| GET | `/api/assets` | Apps Script `Asset.getAll` |
| GET | `/api/assets/distribution/{dimension}` | Apps Script `{Dimension}.getDistribution` |
| GET | `/api/assets/etfstocks/information` | Apps Script `AssetType.getEtfStocksByInformation` |
| GET | `/api/assets/etfstocks/information/{information}` | Apps Script `AssetType.getByAssetTypeAndInformation` |
| GET | `/api/portfolio/metrics` | Compose `AssetsService` + `SnapshotService` |
| GET | `/api/portfolio/geography/{assetClass}` | `GeographyService` — parsing pondéré depuis `Asset.getAll` |
| POST | `/api/mcp` | MCP JSON-RPC 2.0 — `McpService` |

Dimensions valides pour `/api/assets/distribution/{dimension}` : `assetClass`, `assetType`, `support`, `supportType`.

Valeurs valides pour `/api/portfolio/geography/{assetClass}` : `Stocks`, `Bonds`.

### Endpoint MCP

`POST /api/mcp` reçoit des requêtes JSON-RPC 2.0. Méthodes supportées :

| Méthode JSON-RPC | Rôle |
|---|---|
| `initialize` | Poignée de main — retourne version protocole (`2024-11-05`) et capacités |
| `tools/list` | Retourne les 8 outils disponibles (définis dans `McpToolRegistry`) |
| `tools/call` | Exécute un outil — délègue aux services métier |
| `notifications/initialized` | Accusé de réception silencieux |

**Outils exposés :** `get_assets`, `get_assets_distribution`, `get_etf_stocks`, `get_portfolio_metrics`, `get_portfolio_history`, `get_snapshot`, `get_snapshot_history`, `get_geography_distribution`.

**Transport :** Streamable HTTP (POST uniquement). Compatible avec Claude Code (`type: "http"` dans `.mcp.json`). Claude Desktop ne supporte pas ce transport.

---

## 7. Désérialisation JSON

`AppsScriptService` utilise `System.Text.Json` avec :
- `PropertyNameCaseInsensitive = true` — les DTOs C# (PascalCase) matchent le JSON camelCase de l'Apps Script
- `NumberHandling = AllowReadingFromString` — Google Sheets peut retourner des nombres comme strings
- `FlexibleIntConverter` — gère les IDs et risks retournés comme floats ou strings (`"5.0"` → `5`)
- `FlexibleStringConverter` — tolère les tokens JSON Number là où un string est attendu (ex : champ `information` numérique)

---

## 8. Règles d'implémentation

- Un fichier par Function dans `Functions/`
- Logger les erreurs avant de retourner un 500 ou 502 (`HttpRequestException` → 502, autres → 500)
- Pas de logique métier dans les Functions — déléguer aux services
- Tests unitaires xUnit pour chaque service (mock `IAppsScriptService`)
- `InternalsVisibleTo("DynamicProxyGenAssembly2")` dans `AssemblyInfo.cs` pour Moq sur interfaces internes
> Prendre en compte les conseils dans le fichier `clean-code-tips.md`

---

## 9. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
