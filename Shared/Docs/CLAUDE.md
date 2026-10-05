# Shared — Modèles et constantes partagés

## 1. Rôle

Bibliothèque `net8.0` sans aucun paquet, référencée par `Api`, `Client.Shared` (donc `Client` et `Maui`) et les projets de tests. Elle porte le contrat de données entre l'Api et les clients : DTOs sérialisés en JSON, constantes de taxonomie, et un parseur. Elle reste en net8.0 pour être référençable par tous les projets (net8/net9/net10).

## 2. Contenu

```
Shared/
├── Models/
│   ├── AssetDto.cs, AssetTypeReferenceDto.cs, DistributionDto.cs, AggregateDto.cs
│   ├── SnapshotDto.cs, PerformancePointDto.cs, PortfolioMetricsDto.cs
│   ├── BondScheduleDto.cs (BondScheduleDto + BondScheduleItemDto)
│   ├── SyncResultDto.cs
│   └── Mcp/McpModels.cs        # JSON-RPC : JsonRpcRequest/Response/Error, McpInitializeResult, McpToolsListResult, McpToolsCallResult, McpContent, McpJsonOptions
├── Constants/
│   └── AssetClassNames.cs, AssetTypeNames.cs, SupportTypeNames.cs, SupportNames.cs
└── GeographyParser.cs          # Parse("Zone1 : X% - Zone2 : Y%") → (Zone, Pct)
```

Les champs de chaque DTO sont décrits dans `Api/Docs/SPECS.md` §4.

## 3. Règles

- Les DTOs sont des `record` immuables ; les champs `?` valent `null` quand la donnée est indisponible ou non calculable (sentinelle `"ND"` du Sheet, voir `CLAUDE.md` racine §6.4) — ne jamais les remplacer par `0`.
- Toute modification d'un DTO est un changement de contrat : mettre à jour l'Api (`SheetMappers`, services), les ViewModels/composants du Client, l'outil MCP concerné et `Api/Docs/SPECS.md` §4 dans le même geste, avec leurs tests.
- Les constantes de `Constants/` doivent rester alignées sur `Scripts/Config.gs` (`ASSET_CLASS`, `ASSET_TYPE`, `SUPPORT_TYPE`, `SUPPORT`) et sur la taxonomie du `CLAUDE.md` racine §6.5. Elles ne sont référencées par aucun code à ce jour (l'Api et le Client comparent encore des chaînes) : les utiliser pour tout nouveau code plutôt que de coder ces chaînes en dur.
- `GeographyParser` est utilisé par l'Api (`GeographyService`) et par le Client (`DashboardViewModel`, coefficients de zone) : un seul parseur pour les deux côtés.
- Aucune dépendance (ni paquet NuGet, ni référence de projet) : la bibliothèque doit rester utilisable partout.

## 4. Tests

Pas de projet de test dédié : les DTOs et le parseur sont couverts indirectement par `Api.Tests` et `Client.Tests`.

## 5. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
