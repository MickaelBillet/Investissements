# Client — Architecture technique

## 1. Rôle
Blazor WebAssembly — dashboard de visualisation du portefeuille d'investissement personnel. Consomme les endpoints Azure Functions (`/api/*`), n'embarque aucune clé API ni donnée sensible.

## 2. Stack

| Élément | Choix |
|---|---|
| Runtime | .NET 10 / Blazor WebAssembly |
| UI Components | MudBlazor (dernière version stable) |
| Graphiques | ApexCharts for Blazor |
| Tests | xUnit + bUnit — projet `Client.Tests/` |
| Déploiement | Azure Static Web Apps (Managed Functions) |

## 3. Architecture — MVVM

- **Views** (`Client/Views/`) — pages .razor, logique de rendu et navigation locale uniquement
- **ViewModels** (`Client/ViewModels/`) — agrégation et calculs de présentation, pas de logique UI
- **Model** (`Client/Model/`) — modèles de données côté client uniquement
- **Shared** (`Client/Shared/`) — composants réutilisables transverses
- **Services** (`Client/Services/`) — appels HTTP vers les Azure Functions

Les modèles partagés Client + Api sont dans `Shared/Models/` à la racine du repo.

## 4. Structure des dossiers

```
Client/
├── Extensions/   → DecimalExtensions.cs (ToEurAmount, ToPercentage, CssRoiClass)
├── Layout/       → MainLayout.razor, NavMenu.razor
├── Model/        → DistributionItem.cs, IndexedPoint.cs, PanelState.cs
├── Services/     → IPortfolioService.cs, PortfolioService.cs
├── Shared/       → DrillDownDonut.razor, AssetTable.razor,
│                   KpiHeader.razor, KpiCard.razor, HistoryChart.razor
├── ViewModels/   → DashboardViewModel.cs, HistoryViewModel.cs
├── Views/        → Dashboard.razor (/), History.razor (/historique)
└── wwwroot/      → index.html, css/app.css, favicon

Client.Tests/
├── Components/   → KpiHeaderTests, AssetTableTests, DrillDownDonutTests
├── Extensions/   → DecimalExtensionsTests
├── Helpers/      → TestData (factory d'AssetDto et SnapshotDto de test)
├── Models/       → PanelStateTests
└── ViewModels/   → DashboardViewModelTests, HistoryViewModelTests
```

## 5. UI — Règles MudBlazor

- Utiliser exclusivement les composants MudBlazor — pas de HTML natif si un équivalent existe
- Grille responsive : `MudGrid` + `MudItem` avec breakpoints xs/md/lg
- Toujours `MudText`, `MudStack`, `MudPaper` plutôt que div/p/span bruts
- Icônes : `Icons.Material.Outlined.*` (pas de FontAwesome ni autre lib)

## 6. Palette de couleurs

| Usage | Couleurs |
|---|---|
| Classes d'actifs | `#CE8BA0 #E06D6D #4DAB9A #9B8DD6 #D4A844 #A0A0A0 #787774 #2383E2` |
| Types de supports | `#A0A0A0 #4DAB9A #9B8DD6 #2383E2 #CE8BA0 #D4A844` |
| Niveaux de risque | `#4DAB9A #A0A0A0 #D4A844 #CE8BA0 #E06D6D` |
| Texte principal | `#37352F` |
| Texte secondaire / labels | `#787774` |
| Bordures | `#E9E9E7` |

## 7. Patterns clés

### 7.1 Navigation drill-down — PanelState

`PanelState` (dans `Client/Model/`) gère l'état de navigation d'une hiérarchie. `DashboardViewModel` en expose trois instances publiques :

```csharp
public PanelState AssetClassPanel  { get; } = new(PanelType.AssetClass);   // 3 niveaux
public PanelState SupportTypePanel { get; } = new(PanelType.SupportType);  // 3 niveaux
public PanelState RiskPanel        { get; } = new(PanelType.Risk);         // 2 niveaux
```

Méthodes : `DrillDown(name)`, `GoBack()`. Propriétés : `Level`, `CanGoBack`, `IsAtLeafLevel`, `Selected(level)`, `BreadcrumbLabel`.

### 7.2 API unifiée du ViewModel

```csharp
IReadOnlyList<DistributionItem> GetDistribution(PanelState panel)  // données du donut
IReadOnlyList<AssetDto>         GetAssetsForPanel(PanelState panel) // données du tableau (feuille seulement)
```

`GetDistribution` sélectionne automatiquement le bon filtre et le bon groupement selon `panel.Type` et `panel.Level`.

### 7.3 DrillDownDonut — directive @key obligatoire

ApexCharts for Blazor ne redessine pas le graphique sur simple mise à jour des paramètres. Toujours ajouter `@key` pour forcer la recréation du composant quand le niveau change :

```razor
<DrillDownDonut @key="@($"{_activeHierarchy}:{panel.Level}")"
                Items="@ViewModel.GetDistribution(panel)" ... />
```

### 7.4 Extensions décimales

Toujours formater les montants et pourcentages via `DecimalExtensions` :

| Méthode | Exemple de sortie |
|---|---|
| `value.ToEurAmount()` | `€ 12 345,67` |
| `value.ToPercentage()` | `15,50 %` |
| `value.CssRoiClass()` | `"roi-positive"` / `"roi-negative"` / `""` |

## 8. Tests

Framework : xUnit + bUnit. Nommage : `[MethodName]_[Scenario]_[ExpectedResult]`.

- `TestData` dans `Client.Tests/Helpers/` fournit les factories `Asset(...)` et `Snapshot(...)` pour les tests
- Les tests de ViewModel instancient directement la classe avec un `Mock<IPortfolioService>`
- Les tests de composants héritent de `BunitContext` et enregistrent MudBlazor via `Services.AddMudServices(...)`
- Lancer les tests : `dotnet test Client.Tests`
