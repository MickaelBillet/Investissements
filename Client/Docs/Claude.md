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
│                   KpiHeader.razor, KpiCard.razor, HistoryChart.razor, BondScheduleChart.razor,
│                   BondScheduleDetailTable.razor
├── ViewModels/   → DashboardViewModel.cs, SuiviViewModel.cs
├── Views/        → Dashboard.razor (/), Suivi.razor (/suivi)
└── wwwroot/      → index.html, css/app.css, favicon
                     appsettings.json          (ApiBaseUrl vide — fallback sur BaseAddress en prod)
                     appsettings.Development.json  (ApiBaseUrl: http://localhost:7071/)

Client.Tests/
├── Components/   → KpiHeaderTests, AssetTableTests, DistributionTableTests, DrillDownDonutTests,
│                   BondScheduleChartTests, BondScheduleDetailTableTests
├── Extensions/   → DecimalExtensionsTests
├── Helpers/      → TestData (factories AssetDto, SnapshotDto, PerformancePointDto + AddLocalizationMock)
├── Models/       → PanelStateTests
└── ViewModels/   → DashboardViewModelTests, SuiviViewModelTests
```

## 5. UI — Règles MudBlazor

- Utiliser exclusivement les composants MudBlazor — pas de HTML natif si un équivalent existe
- Grille responsive : `MudGrid` + `MudItem` avec breakpoints xs/md/lg
- Toujours `MudText`, `MudStack`, `MudPaper` plutôt que div/p/span bruts
- Icônes : `Icons.Material.Outlined.*` (pas de FontAwesome ni autre lib)
- Toujours qualifier `MudBlazor.Size.*` (jamais `Size.*` seul) — ambiguïté avec `ApexCharts.Size`
- Onglets : `MudTabs`/`MudTabPanel` — utilisé sur la page Suivi (`/suivi`) pour que chaque graphique occupe toute la hauteur disponible, plutôt qu'un empilement vertical (l'onglet Échéancier a `overflow-y:auto` pour le drill-down, voir §7.5)

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
portfolioService.GetMetricsAsync(ct)           // → _metrics (ROIC + AverageRisk)
portfolioService.GetSnapshotHistoryAsync(ct)   // → _snapshotHistory (variations J/S/M/YTD/1A)
portfolioService.GetGeographyDistributionAsync // → _geoStocks / _geoBonds
portfolioService.GetAssetTypeReferenceAsync    // → _assetTypeRef (labelFr + geoSectorEligible par AssetType)
```

**Propriétés de variation (calculées côté client depuis `_snapshotHistory`) :**

Deux familles de métriques (capital net engagé, ROIC Capital Engagé) × cinq périodes (J / S / M / YTD / 1A) = 10 propriétés.

| Famille | Préfixe propriété | Formule |
|---|---|---|
| Capital net engagé | `…VariationPercent` | `(last - ref) / ref × 100` — variation relative de `NetCapital` |
| ROIC Capital Engagé | `…ROICapitalEngagedVariation` | `(ROIC_today - ROIC_ref) / \|ROIC_ref\| × 100` |

Préfixes de période : `Daily` (J−1), `Weekly` (≤ J−7), `Monthly` (≤ J−30), `Ytd` (1er snapshot de l'année courante), `Yearly` (≤ J−365).
Ex. : `MonthlyVariationPercent`, `YtdROICapitalEngagedVariation`, `YearlyROICapitalEngagedVariation`.

La référence à comparer est fournie par un sélecteur : `RefDaysBack(history, n)` pour J/S/M/1A, `RefYearStart(history)` pour YTD (surchargés pour `SnapshotDto` et `PerformancePointDto`). L'helper `ComputeVariation` (sur `_snapshotHistory`, base `NetCapital`) calcule la variation du capital net ; `ComputePerformanceVariation` (sur `_performanceHistory`, la série TWR de `GetIndexedHistoryAsync`) calcule la variation du `ROIC` — c'est ce dernier qui alimente les puces `*ROICapitalEngagedVariation` sous la carte "ROI (Capital Engagé)", pour refléter la vraie performance du portefeuille plutôt qu'une variation relative du ratio ROI (petit dénominateur, amplifiait artificiellement l'écart).

Retournent `null` si historique insuffisant, si aucune référence n'est trouvée pour la période, ou si la valeur de référence (`NetCapital` ou `ROIC`) vaut `0`. Pour YTD avec un seul point dans l'année, la référence est ce point → `0 %`.

**`KpiCard` — slot `SubContent` :**
`KpiCard` accepte un `RenderFragment? SubContent` affiché à droite de la valeur (même ligne, `MudStack Row`). Utilisé pour les chips de variation J/S/M/YTD/1A dans `KpiHeader.razor`, rendues par le helper local `VariationChips(params (string Prefix, decimal? Value)[])` (une chip par période non nulle).

### 7.3 DrillDownDonut — directive @key obligatoire

ApexCharts for Blazor ne redessine pas le graphique sur simple mise à jour des paramètres. Toujours ajouter `@key` pour forcer la recréation du composant quand le niveau ou le toggle change :

```razor
<DrillDownDonut @key="@($"{_activeHierarchy}:{panel.Level}:{ViewModel.EtfStocksGroupByInformation}")"
                Items="@ViewModel.GetDistribution(panel)" ... />
```

**Slot `TopRightContent`** : `RenderFragment` facultatif affiché en haut à droite du titre. Utilisé pour placer le `MudSwitch` ETF_Stocks dans `Dashboard.razor`. Ce contenu n'est rendu que si non null — le composant `DrillDownDonut` n'a aucune connaissance du toggle.

### 7.4 Extensions décimales

Toujours formater les montants et pourcentages via `DecimalExtensions` — voir `Client/Docs/SPECS.md` §5 pour les signatures et exemples de sortie.

### 7.5 BondScheduleChart — drill-down au clic sur une barre

Contrairement à `DrillDownDonut` (donut, `OnDataPointSelection` → nom de la tranche via `Items.FirstOrDefault()?.Name`), `BondScheduleChart` est un graphique en barres dont `TItem = BondScheduleDto` n'a pas de propriété `Name` : `data.DataPoint.Items.FirstOrDefault()` renvoie directement le `BondScheduleDto` complet du point cliqué (pas besoin de re-résoudre l'année depuis un label), remonté au parent via `EventCallback<BondScheduleDto> OnYearClicked`.

`Suivi.razor` stocke l'entrée sélectionnée dans un champ code-behind (`_selectedYearEntry`, pas dans `SuiviViewModel` — état de sélection UI pure, même logique que `_activeHierarchy`/`_selectedZone` dans `Dashboard.razor`) et l'affiche via `BondScheduleDetailTable.razor` (même style que `DistributionTable.razor` : bordure `#E9E9E7`, `Dense`, `Hover`, colonnes `Col_Name`/`Col_CurrentValue`, ligne `Table_Total`, `NoRecordsContent` sur `Empty_NoData`).

Layout responsive (`MudGrid`/`MudItem xs="12" md="X"`) : graphique en `md="12"` tant qu'aucune année n'est sélectionnée, puis `md="7"` dès le premier clic pour laisser la place au tableau en `md="5"` à droite. En dessous du breakpoint `md`, les deux blocs passent en `xs="12"` (empilés). Le `MudGrid` et le `MudItem` du graphique ont `Style="height:100%;"` — sans quoi le `height:100%` du `MudPaper` interne à `BondScheduleChart` se réduit à la hauteur du contenu (le `MudGrid` ne propage pas la hauteur de son conteneur par défaut).

`<BondScheduleChart>` a un `@key="@(_selectedYearEntry is null)"` — sans lui, un clic changeant `md="12"` en `md="7"` (ou l'inverse) ne recrée pas l'instance ApexCharts JS sous-jacente : elle continue de se redessiner à l'ancienne largeur pendant que `MudTable` apparaît déjà dans son propre `MudItem`, ce qui provoque un chevauchement visuel du tableau sur le graphique en cas de clics rapides successifs. Même règle que `@key` sur `DrillDownDonut` (§7.3) : forcer la recréation du composant chaque fois qu'un changement de layout doit être suivi d'un redessin ApexCharts.

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

**Exception — libellés `AssetType`** : ne passent pas par `Translations.resx` ni `ILocalizationService`. Ils sont lus depuis l'onglet `AssetType` du Sheet (colonne `LabelFR`) via `GET /api/assets/types/reference`, résolus dans `DashboardViewModel.TranslateAssetType(assetType)` (fallback : nom brut si `labelFr` absent). Ajouter un nouveau `AssetType` ne nécessite donc aucune entrée `.resx`.

---

## 9. Tests

Framework : xUnit + bUnit. Nommage : `[MethodName]_[Scenario]_[ExpectedResult]`.

- `TestData` dans `Client.Tests/Helpers/` fournit les factories `Asset(...)`, `Snapshot(...)` et `PerformancePoint(...)`
- `TestData.AddLocalizationMock(this IServiceCollection services)` — extension à appeler dans le constructeur de tout test de composant qui rend un composant injectant `ILocalizationService`. Le mock utilise `ResourceManager` sur les vraies ressources compilées → les assertions peuvent vérifier les chaînes françaises.
- `DashboardViewModel` — instancier avec `Mock<IPortfolioService>` + `Mock<ILocalizationService>` (setup `Translate(key) → key`)
- `SuiviViewModel` — instancier avec `Mock<IPortfolioService>` + `Mock<ILocalizationService>`
- Les tests de composants héritent de `BunitContext` et appellent `Services.AddMudServices(...)` + `Services.AddLocalizationMock()`
- Lancer les tests : `dotnet test Client.Tests`

---

## 10. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
