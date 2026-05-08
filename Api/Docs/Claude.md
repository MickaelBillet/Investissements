# CLAUDE.md — Api (Azure Functions)

## 1. Rôle

Couche backend serverless entre Google Sheets et le Blazor WASM. Détient la clé API Google Sheets et expose des endpoints REST internes consommés uniquement par le frontend hébergé sur le même Azure Static Web Apps.

---

## 2. Stack

| Élément | Choix |
|---|---|
| Runtime | .NET 8, Azure Functions v4 isolated worker |
| Modèle HTTP | `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` |
| Tests | xUnit |
| Déploiement | Lié à Azure Static Web Apps (Managed Functions) |

---

## 3. Structure du projet

```
Api/
├── InvestissementsDashboard.Api.csproj
├── Program.cs                  # Point d'entrée, DI
├── host.json                   # Config Azure Functions
├── local.settings.json         # Variables locales (gitignorées)
└── Functions/                  # Un fichier par endpoint
```

---

## 4. Configuration

Les variables d'environnement sont injectées via `IConfiguration` (App Settings en prod, `local.settings.json` en dev) :

| Variable | Usage |
|---|---|
| `GOOGLE_SHEETS_API_KEY` | Clé API Google Sheets (lecture seule) |
| `GOOGLE_SHEET_ID` | ID du Google Sheets DEST |

Ne jamais lire ces valeurs autrement que via `IConfiguration` injecté.

---

## 5. Endpoints prévus

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/snapshot` | Dernière ligne de l'onglet Snapshot |
| GET | `/api/snapshot/history` | Toutes les lignes de l'onglet Snapshot |
| GET | `/api/assets` | Tous les actifs (onglet Asset) |
| GET | `/api/assets/distribution/{dimension}` | Vue agrégée par dimension (assetClass, assetType, support, supportType) |

---

## 6. Règles d'implémentation

- Un fichier par Function dans `Functions/`
- Injection de dépendances systématique — pas de `new` dans les Functions
- Toujours logger les erreurs avant de retourner un 500
- Pas de logique métier dans les Functions — déléguer à des services injectés
- Tests unitaires xUnit pour chaque service (pas de mock Google Sheets API — utiliser une interface)
