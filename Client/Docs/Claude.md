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
├── Resources/    → Translations.cs (classe marqueur), Translations.resx (toutes les chaînes UI)
├── Services/     → IPortfolioService.cs, PortfolioService.cs,
│                   ILocalizationService.cs, LocalizationService.cs
├── Shared/       → DrillDownDonut.razor, AssetTable.razor, DistributionTable.razor,
│                   KpiHeader.razor, KpiCard.razor, HistoryChart.razor
├── ViewModels/   → DashboardViewModel.cs, HistoryViewModel.cs
├── Views/        → Dashboard.razor (/), History.razor (/historique)
└── wwwroot/      → index.html, css/app.css, favicon
                     appsettings.json          (ApiBaseUrl vide — fallback sur BaseAddress en prod)
                     appsettings.Development.json  (ApiBaseUrl: http://localhost:7071/)

Client.Tests/
├── Components/   → KpiHeaderTests, AssetTableTests, DistributionTableTests, DrillDownDonutTests
├── Extensions/   → DecimalExtensionsTests
├── Helpers/      → TestData (factories AssetDto, SnapshotDto, PerformancePointDto + AddLocalizationMock)
├── Models/       → PanelStateTests
└── ViewModels/   → DashboardViewModelTests, HistoryViewModelTests
```

## 5. UI — Règles MudBlazor

- Utiliser exclusivement les composants MudBlazor — pas de HTML natif si un équivalent existe
- Grille responsive : `MudGrid` + `MudItem` avec breakpoints xs/md/lg
- Toujours `MudText`, `MudStack`, `MudPaper` plutôt que div/p/span bruts
- Icônes : `Icons.Material.Outlined.*` (pas de FontAwesome ni autre lib)
- Toujours qualifier `MudBlazor.Size.*` (jamais `Size.*` seul) — ambiguïté avec `ApexCharts.Size`

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
public PanelState AssetClassPanel  { get; } = new(PanelType.AssetClass);   // 3 ou 4 niveaux selon toggle
public PanelState SupportTypePanel { get; } = new(PanelType.SupportType);  // 3 niveaux
public PanelState RiskPanel        { get; } = new(PanelType.Risk);         // 2 niveaux
```

Méthodes : `DrillDown(name)`, `GoBack()`. Propriétés : `Level`, `CanGoBack`, `IsAtLeafLevel`, `Selected(level)`.

Le titre d'un panel (ex : "Classes d'actifs") est calculé par `DashboardViewModel.GetPanelTitle(panel)` — jamais par `PanelState` directement.

**Ne pas utiliser `panel.IsAtLeafLevel` directement dans les Views** — appeler `ViewModel.IsLeafLevel(panel)` qui prend en compte le toggle ETF et le type de panel.

### 7.2 API unifiée du ViewModel

```csharp
IReadOnlyList<DistributionItem> GetDistribution(PanelState panel)  // données du donut
IReadOnlyList<AssetDto>         GetAssetsForPanel(PanelState panel) // données du tableau (feuille seulement)
bool                            IsLeafLevel(PanelState panel)       // true si niveau feuille atteint
```

`GetDistribution` sélectionne automatiquement le bon filtre et le bon groupement selon `panel.Type`, `panel.Level` et `EtfStocksGroupByInformation`.

`EtfStocksGroupByInformation` (bool, bindable via `@bind-Value`) active le regroupement des ETF_Stocks par champ `information` et ajoute un niveau intermédiaire dans la hiérarchie Classes d'actifs.

**Services appelés à l'initialisation (en parallèle) :**
```csharp
portfolioService.GetAssetsAsync(ct)            // → _assets
portfolioService.GetLastSnapshotAsync(ct)      // → LastSnapshot
portfolioService.GetMetricsAsync(ct)           // → _metrics (ROI + AverageRisk)
portfolioService.GetSnapshotHistoryAsync(ct)   // → _snapshotHistory (variations J/S)
portfolioService.GetGeographyDistributionAsync // → _geoStocks / _geoBonds
```

**Propriétés de variation (calculées côté client depuis `_snapshotHistory`) :**

| Propriété | Formule |
|---|---|
| `DailyVariationPercent` | `(last - prev) / prev × 100` — variation relative valeur portefeuille J |
| `WeeklyVariationPercent` | idem sur 7 jours |
| `DailyROICapitalEngagedVariation` | `(ROI_today - ROI_ref) / \|ROI_ref\| × 100` — variation relative ROI CE J |
| `WeeklyROICapitalEngagedVariation` | idem sur 7 jours |
| `DailyROITotalPurchasesVariation` | variation relative ROI TP J |
| `WeeklyROITotalPurchasesVariation` | idem sur 7 jours |

Retournent `null` si historique insuffisant ou si `ROI_ref == 0`.

**`KpiCard` — slot `SubContent` :**
`KpiCard` accepte un `RenderFragment? SubContent` affiché à droite de la valeur (même ligne, `MudStack Row`). Utilisé pour les chips de variation J/S dans `KpiHeader.razor`.

### 7.3 DrillDownDonut — directive @key obligatoire

ApexCharts for Blazor ne redessine pas le graphique sur simple mise à jour des paramètres. Toujours ajouter `@key` pour forcer la recréation du composant quand le niveau ou le toggle change :

```razor
<DrillDownDonut @key="@($"{_activeHierarchy}:{panel.Level}:{ViewModel.EtfStocksGroupByInformation}")"
                Items="@ViewModel.GetDistribution(panel)" ... />
```

**Slot `TopRightContent`** : `RenderFragment` facultatif affiché en haut à droite du titre. Utilisé pour placer le `MudSwitch` ETF_Stocks dans `Dashboard.razor`. Ce contenu n'est rendu que si non null — le composant `DrillDownDonut` n'a aucune connaissance du toggle.

### 7.4 Extensions décimales

Toujours formater les montants et pourcentages via `DecimalExtensions` — voir `Client/Docs/SPECS.md` §5 pour les signatures et exemples de sortie.

## 8. Localisation

Toutes les chaînes UI sont externalisées dans `Client/Resources/Translations.resx`.  
Le service `ILocalizationService` (implémenté par `LocalizationService`) est le seul point d'accès — ne jamais appeler `IStringLocalizer<Translations>` directement.

```csharp
// Dans un ViewModel (injection constructeur)
public DashboardViewModel(IPortfolioService portfolioService, ILocalizationService localizationService)

// Dans un composant Razor (injection directe)
@inject ILocalizationService L
// puis : @L.Translate("Ma_Cle")
```

`ILocalizationService` est enregistré **singleton** dans `Program.cs`. Il est déjà importé globalement via `_Imports.razor` — aucun `@using` supplémentaire requis dans les composants.

Fallback : si une clé n'existe pas dans le `.resx`, `Translate()` retourne la clé brute (jamais d'exception).

---

## 9. Tests

Framework : xUnit + bUnit. Nommage : `[MethodName]_[Scenario]_[ExpectedResult]`.

- `TestData` dans `Client.Tests/Helpers/` fournit les factories `Asset(...)`, `Snapshot(...)` et `PerformancePoint(...)`
- `TestData.AddLocalizationMock(this IServiceCollection services)` — extension à appeler dans le constructeur de tout test de composant qui rend un composant injectant `ILocalizationService`. Le mock utilise `ResourceManager` sur les vraies ressources compilées → les assertions peuvent vérifier les chaînes françaises.
- `DashboardViewModel` — instancier avec `Mock<IPortfolioService>` + `Mock<ILocalizationService>` (setup `Translate(key) → key`)
- `HistoryViewModel` — instancier avec `Mock<IPortfolioService>` + `Mock<ILocalizationService>`
- Les tests de composants héritent de `BunitContext` et appellent `Services.AddMudServices(...)` + `Services.AddLocalizationMock()`
- Lancer les tests : `dotnet test Client.Tests`
