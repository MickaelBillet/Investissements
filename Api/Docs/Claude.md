# CLAUDE.md — Api (Azure Functions)

## 1. Rôle

Couche backend serverless entre le Google Apps Script et le Blazor WASM. Détient l'URL et le token de l'Apps Script Web App, expose des endpoints REST internes consommés uniquement par le frontend hébergé sur le même Azure Static Web Apps.

---

## 2. Stack

| Élément | Choix |
|---|---|
| Runtime | .NET 10, Azure Functions v4 isolated worker |
| Modèle HTTP | `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` |
| Tests | xUnit + Moq |
| Déploiement | Lié à Azure Static Web Apps (Managed Functions) |

---

## 3. Structure du projet

```
Api/
├── InvestissementsDashboard.Api.csproj
├── Program.cs                  # Point d'entrée, DI
├── host.json                   # Config Azure Functions
├── local.settings.json         # Variables locales (gitignorées)
├── Functions/                  # Un fichier par endpoint
└── Services/
    ├── IAppsScriptService.cs   # Interface HTTP vers l'Apps Script
    ├── AppsScriptService.cs    # Implémentation — appelle le Web App
    ├── IAssetsService.cs
    ├── AssetsService.cs        # Délègue à IAppsScriptService
    ├── ISnapshotService.cs
    └── SnapshotService.cs      # Délègue à IAppsScriptService
```

---

## 4. Architecture — flux de données

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

| Méthode | Route | Service Apps Script |
|---|---|---|
| GET | `/api/snapshot` | `Snapshot.getLast` |
| GET | `/api/snapshot/history` | `Snapshot.getHistory` |
| GET | `/api/assets` | `Asset.getAll` |
| GET | `/api/assets/distribution/{dimension}` | `{Dimension}.getDistribution` |

Dimensions valides : `assetClass`, `assetType`, `support`, `supportType`.

---

## 7. Désérialisation JSON

`AppsScriptService` utilise `System.Text.Json` avec :
- `PropertyNameCaseInsensitive = true` — les DTOs C# (PascalCase) matchent le JSON camelCase de l'Apps Script
- `NumberHandling = AllowReadingFromString` — Google Sheets peut retourner des nombres comme strings
- `FlexibleIntConverter` — gère les IDs et risks retournés comme floats ou strings (`"5.0"` → `5`)

---

## 8. Règles d'implémentation

- Un fichier par Function dans `Functions/`
- Toujours logger les erreurs avant de retourner un 500
- Pas de logique métier dans les Functions — déléguer aux services
- Tests unitaires xUnit pour chaque service (mock `IAppsScriptService`)
- `InternalsVisibleTo("DynamicProxyGenAssembly2")` dans `AssemblyInfo.cs` pour Moq sur interfaces internes
> Prendre en compte les conseils dans le fichier `clean-code-tips.md`
