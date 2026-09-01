# CLAUDE.md — Api (Azure Functions)

## 1. Rôle

Couche backend serverless entre Google Sheets et le Blazor WASM. Lit le Sheet `InvestData` directement via l'API Google Sheets officielle (compte de service, projet `GoogleSheets/`), expose des endpoints REST internes consommés uniquement par le frontend hébergé sur le même Azure Static Web Apps.

---

## 2. Stack

| Élément | Choix |
|---|---|
| Runtime | .NET 9, Azure Functions v4 isolated worker |
| Modèle HTTP | `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` |
| Accès données | `Google.Apis.Sheets.v4` via le projet `GoogleSheets/` (référencé, pas de dépendance Google directe dans l'Api) |
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
│   ├── BondScheduleFunction.cs
│   ├── GeographyFunction.cs
│   ├── McpFunction.cs             # Endpoint MCP POST /api/mcp
│   ├── PortfolioMetricsFunction.cs
│   └── SnapshotFunction.cs
├── Interfaces/                 # Interfaces des services
│   ├── IAssetsService.cs
│   ├── IBondScheduleService.cs
│   ├── IGeographyService.cs
│   ├── IPortfolioMetricsService.cs
│   ├── ISnapshotService.cs
│   └── Mcp/
│       └── IMcpService.cs         # Interface du handler JSON-RPC
├── Mappers/
│   └── SheetMappers.cs            # Fonctions pures : lignes brutes Sheets → DTOs (mapping + agrégation)
├── Mcp/
│   └── McpToolRegistry.cs         # Registre statique des 8 outils MCP
├── Services/
│   ├── AssetsService.cs
│   ├── BondScheduleService.cs
│   ├── GeographyService.cs
│   ├── PortfolioMetricsService.cs
│   ├── SnapshotService.cs
│   └── Mcp/
│       └── McpService.cs          # Handler JSON-RPC — route vers les services
└── Properties/
    └── AssemblyInfo.cs
```

Les modèles JSON-RPC (`JsonRpcRequest`, `JsonRpcResponse`, etc.) sont dans `Shared/Models/Mcp/McpModels.cs`.

`SheetMappers` est un `static class` interne sans état — fonctions pures (ligne brute → DTO), pas de DI, à distinguer des vrais services injectés dans `Services/`.

---

## 4. Architecture — flux de données

**Flux dashboard (Blazor WASM) :**
```
Blazor WASM
    │ HTTP GET /api/...
    ▼
Azure Functions (C#)
    │ IGoogleSheetsClient.GetRangeAsync("Asset" | "Snapshot" | "AssetType")
    ▼
GoogleSheets/ (Google.Apis.Sheets.v4, compte de service)
    │ lit Google Sheets DEST directement
    ▼
Lignes brutes → SheetMappers → DTO
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
    │ même flux que le dashboard (lecture directe du Sheet)
    ▼
DTO → sérialisé en McpContent
```

Il n'y a plus d'Apps Script Web App dans ce flux — Apps Script ne fait plus que l'ETL quotidien et le rapport hebdomadaire par email (voir `Scripts/Docs/CLAUDE.md`), sans lien avec l'Api.

---

## 5. Configuration

Les variables d'environnement sont injectées via `IConfiguration` (App Settings en prod, `local.settings.json` en dev) :

| Variable | Usage |
|---|---|
| `GOOGLE_SHEET_ID` | ID du Google Sheet `InvestData` (`DEST_ID`) |
| `GOOGLE_SERVICE_ACCOUNT_EMAIL` | Email du compte de service Google (accès Lecteur sur le Sheet) |
| `GOOGLE_SERVICE_ACCOUNT_KEY` | Clé privée du compte de service (format PEM, `\n` littéraux) |

Ne jamais lire ces valeurs autrement que via `IConfiguration` injecté.

---

## 6. Endpoints

| Méthode | Route | Source |
|---|---|---|
| GET | `/api/snapshot` | Sheet `Snapshot`, dernière ligne |
| GET | `/api/snapshot/history` | Sheet `Snapshot`, toutes les lignes |
| GET | `/api/assets` | Sheet `Asset`, toutes les lignes |
| GET | `/api/assets/distribution/{dimension}` | Sheet `Asset`, groupé par colonne (`assetClass`/`assetType`/`support`/`supportType`) |
| GET | `/api/assets/etfstocks/information` | Sheet `Asset`, filtré `ETF_Stocks`, groupé par `information` |
| GET | `/api/assets/etfstocks/information/{information}` | Sheet `Asset`, filtré par type + `information` |
| GET | `/api/assets/types/reference` | Sheet `AssetType` — `AssetTypeReferenceDto` (`labelFr`, `geoSectorEligible`) |
| GET | `/api/portfolio/metrics` | Compose `AssetsService` + `SnapshotService` |
| GET | `/api/portfolio/metrics/history` | `PortfolioMetricsService.GetIndexedHistoryAsync` — historique `Snapshot`, normalisé base 100 |
| GET | `/api/assets/bondschedule` | `BondScheduleService` — agrège les assets par année extraite du champ `information`, avec le détail par actif (`bonds[]`) |
| GET | `/api/portfolio/geography/{assetClass}` | `GeographyService` — parsing pondéré depuis les assets |
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

**Transport :** Streamable HTTP (POST uniquement). Compatible avec Claude Code, Claude Desktop et Claude Web.

---

## 7. Mapping des lignes brutes (`Mappers/SheetMappers.cs`)

`IGoogleSheetsClient.GetRangeAsync` retourne des lignes brutes (`IReadOnlyList<IReadOnlyList<object>>`), avec `ValueRenderOption = UNFORMATTED_VALUE` côté `GoogleSheetsClient` (valeurs typées, pas de formatage locale/devise). `SheetMappers` porte en C# pur la logique historiquement dans `Scripts/Router.gs` (`buildAssetRow`, `buildSnapshotRow`, `groupBy`, `sumColumn`, `aggregateGroup`) :

- Les cellules numériques arrivent en `double` ou `long` selon qu'elles ont une partie décimale ou non — toujours passer par `AsDecimalOrNull`/`AsDecimalOrZero`, jamais de cast direct.
- Les dates (colonne `Snapshot.Date`) : Google Sheets convertit automatiquement un texte reconnu comme une date en valeur interne — elles reviennent en numéro de série (jours depuis 1899-12-30), typé `long` ou `double` selon le cas. `AsDate` gère les deux, avec un fallback texte pour compatibilité.
- Sentinel `"ND"` (Not Defined) et lignes `"Not Defined"` : même traitement que côté Apps Script historique (voir `Scripts/Docs/CLAUDE.md`).

---

## 8. Cache — pattern single-flight

Le dashboard déclenche plusieurs services en parallèle au chargement (`Task.WhenAll` côté Client), et certains appels Sheets identiques sont ainsi redemandés simultanément par plusieurs services :
- Sheet `Asset` : `AssetsFunction`, `GeographyService` (×2, Stocks/Bonds), `PortfolioMetricsService`, `BondScheduleService`
- Sheet `Snapshot` : `SnapshotFunction`, `PortfolioMetricsService`

`AssetsService.GetAllAsync` et `SnapshotService.GetLastAsync`/`GetHistoryAsync` appliquent donc un cache single-flight :

- `IMemoryCache` (enregistré via `services.AddMemoryCache()` dans `Program.cs`), TTL 30s pour `GetAllAsync`/`GetLastAsync` — largement suffisant car les données ne changent qu'une fois par jour (`snapshotQuotidien` à 6h côté Apps Script)
- `AssetsService.GetAssetTypeReferenceAsync` et `SnapshotService.GetHistoryAsync` appliquent le même pattern avec une TTL plus longue (5 min) — ces données changent rarement/une fois par jour
- Vérification rapide sans verrou (chemin pris par la quasi-totalité des appels)
- Si cache vide : `SemaphoreSlim` statique dédié à la clé de cache + double-checked locking, pour qu'une seule requête concurrente déclenche l'appel réel à l'API Sheets

À reproduire pour tout nouvel appel Sheets partagé par plusieurs services invoqués en parallèle.

---

## 9. Règles d'implémentation

- Un fichier par Function dans `Functions/`
- Logger les erreurs avant de retourner un 500 ou 502 (`HttpRequestException` → 502, autres → 500)
- Pas de logique métier dans les Functions — déléguer aux services
- Tests unitaires xUnit pour chaque service (mock `IGoogleSheetsClient`, lignes brutes en entrée)
- `InternalsVisibleTo("DynamicProxyGenAssembly2")` dans `AssemblyInfo.cs` pour Moq sur interfaces internes
> Prendre en compte les conseils dans le fichier `clean-code-tips.md`

---

## 10. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
