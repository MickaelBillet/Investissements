# Client.Shared — Architecture technique

## 1. Rôle
Bibliothèque Razor (RCL) contenant **tout le code Blazor du dashboard** de visualisation du portefeuille d'investissement personnel : `App`, vues, composants, ViewModels, services, ressources. Elle est consommée par deux hôtes :

| Hôte | Projet | Doc |
|---|---|---|
| Site web (Blazor WebAssembly, Azure Static Web Apps) | `src/Frontend/Client/` | `src/Frontend/Client/Docs/CLAUDE.md` |
| Application Windows (MAUI + `BlazorWebView`) | `src/Frontend/Maui/` | `src/Frontend/Maui/Docs/CLAUDE.md` |

Elle consomme les endpoints Azure Functions (`/api/*`), n'embarque aucune clé API ni donnée sensible. Les namespaces sont restés `InvestissementsDashboard.Client.*` (`RootNamespace` du `csproj`) — ne pas les renommer : ils déterminent aussi le nom du manifeste de ressources `.resx`.

### Contrat avec les hôtes
Un hôte doit :
1. enregistrer une implémentation de `IKeyValueStore` (stockage persistant de la session et du mode confidentialité) — `LocalStorageKeyValueStore` (fournie, WASM) ou `SecureStorageKeyValueStore` (`src/Frontend/Maui/`) ;
2. appeler `services.AddInvestissementsClient(apiBaseUri)` (`Extensions/ServiceCollectionExtensions.cs`) : enregistre MudBlazor, ApexCharts, la localisation, `ClientOptions`, `ILocalizationService`, `IPrivacyModeService`, `ISessionService` (avec son propre `HttpClient` sans handler de mot de passe), `DashboardPasswordHandler`, les `HttpClient` typés `IPortfolioService`/`ISyncService`, `IPanelDistributionService`, les huit ViewModels (scoped) et force la culture `fr-FR` ;
3. monter `App` sur `#app` et `HeadOutlet` sur `head::after`, et lier dans sa page d'hébergement le CSS de MudBlazor, `_content/InvestissementsDashboard.Client.Shared/css/app.css` et le bundle CSS isolé.

L'URL de l'Api est toujours fournie par l'hôte (`ClientOptions.ApiBaseUri`) — jamais déduite de `NavigationManager.BaseUri`, qui n'a aucun sens dans un `BlazorWebView`. Assets de la RCL : `_content/InvestissementsDashboard.Client.Shared/` (`css/app.css`, `icon.svg`).

## 2. Stack

| Élément | Choix |
|---|---|
| Runtime | .NET 10 — `Microsoft.NET.Sdk.Razor` (RCL), exécutée en WASM ou dans un `BlazorWebView` |
| UI Components | MudBlazor 9.9.0 |
| Graphiques | Blazor-ApexCharts 7.0.0 |
| Tests | xUnit + bUnit 2.9.0 + Moq — projet `tests/Client.Tests/` (référence `Client.Shared`) |
| Déploiement | via les hôtes : Azure Static Web Apps (`src/Frontend/Client/`), poste Windows (`src/Frontend/Maui/`) |

## 3. Architecture — MVVM

- **Views** (`src/Frontend/Client.Shared/Views/`) — pages .razor, logique de rendu et navigation locale uniquement
- **ViewModels** (`src/Frontend/Client.Shared/ViewModels/`) — agrégation et calculs de présentation, pas de logique UI
- **Model** (`src/Frontend/Client.Shared/Model/`) — modèles de données côté client uniquement
- **Shared** (`src/Frontend/Client.Shared/Shared/`) — composants réutilisables transverses
- **Services** (`src/Frontend/Client.Shared/Services/`) — appels HTTP vers les Azure Functions, et calculs de présentation qui ont besoin d'une dépendance (`PanelDistributionService`, qui traduit)
- **Utils** (`src/Frontend/Client.Shared/Utils/`) — calculateurs purs et statiques, sans état ni dépendance (`DistributionCalculator`, `VariationCalculator`, `GeographySectorFilter`), testés directement

Un ViewModel délègue ses calculs à ces classes et ne garde que le chargement, l'état et les commandes (voir §7.2).

Les modèles partagés Client + Api sont dans `src/Common/Shared/Models/`.

## 4. Structure des dossiers

```
src/Frontend/Client.Shared/
├── App.razor      → thème MudBlazor, providers, garde d'authentification (LoginGate) puis <Router>
├── _Imports.razor
├── Extensions/    → DecimalExtensions.cs (ToEurAmount, ToPercentage, ToSignedPercentage, CssRoiClass),
│                    ServiceCollectionExtensions.cs (AddInvestissementsClient)
├── Layout/        → MainLayout.razor (barre d'application + menu « ⋮ »)
├── Model/         → DistributionItem.cs, IndexedPoint.cs, PanelState.cs, BondSchedulePeriodDto.cs,
│                    AnalysisStep.cs, AnalysisStepStatus.cs, AnalysisStepUpdate.cs, AnalysisStepState.cs, AnalysisPhase.cs (progression de l'analyse, voir §7.13),
│                    LoginResult.cs (voir §7.7)
├── Pages/         → Home.razor (/home-legacy, redirige vers /)
├── Resources/     → Translations.cs (classe marqueur), Translations.resx (toutes les chaînes UI)
├── Services/      → IKeyValueStore.cs,
│                    IPortfolioService.cs, PortfolioService.cs, ISyncService.cs, SyncService.cs,
│                    ILocalizationService.cs, LocalizationService.cs,
│                    IPrivacyModeService.cs, PrivacyModeService.cs,
│                    ISessionService.cs, SessionService.cs, DashboardPasswordHandler.cs,
│                    IPanelDistributionService.cs, PanelDistributionService.cs (répartitions des panneaux du dashboard),
│                    IAgentRunner.cs, IPortfolioReviewRunner.cs (seams optionnels, voir §7.10 et §7.13)
├── Shared/        → DrillDownDonut.razor, AssetTable.razor, DistributionTable.razor,
│                    KpiHeader.razor, KpiCard.razor, HistoryChart.razor, BondScheduleChart.razor, SynthesisReportView.razor, AnalysisStepList.razor,
│                    BondScheduleDetailTable.razor, LoginGate.razor,
│                    Dialogs/AgentPickerDialog.razor, Dialogs/AgentResultDialog.razor
├── Utils/         → DistributionCalculator.cs, VariationCalculator.cs, GeographySectorFilter.cs, ClientOptions.cs, LocalStorageKeyValueStore.cs
├── ViewModels/    → IRefreshableViewModel.cs, DashboardViewModel.cs, TrackingViewModel.cs, LoginGateViewModel.cs, AppViewModel.cs, MainViewModel.cs, AssetViewModel.cs, AgentViewModel.cs, AnalysisViewModel.cs
├── Views/         → Dashboard.razor (/), Tracking.razor (/suivi), Analysis.razor (/analyse, MAUI uniquement)
├── wwwroot/       → css/app.css, icon.svg (servis sous _content/InvestissementsDashboard.Client.Shared/)
└── Docs/          → CLAUDE.md, SPECS.md, client-shared-class-diagram.drawio/.png (ce dossier)

tests/Client.Tests/      (xUnit + bUnit, référence Client.Shared)
├── Components/    → SynthesisReportView, AnalysisStepList, KpiHeader, AssetTable, DistributionTable, DrillDownDonut, HistoryChart,
│                    BondScheduleChart, BondScheduleDetailTable, LoginGate, AgentPickerDialog, AgentResultDialog, Analysis, RefreshAfterSync
├── Extensions/    → DecimalExtensionsTests, ServiceCollectionExtensionsTests
├── Helpers/       → TestData (factories AssetDto, SnapshotDto, PerformancePointDto + mocks)
├── Models/        → PanelStateTests
├── Services/      → SessionServiceTests, PrivacyModeServiceTests, LocalStorageKeyValueStoreTests, PanelDistributionServiceTests
├── Utils/         → DistributionCalculatorTests, VariationCalculatorTests, GeographySectorFilterTests
└── ViewModels/    → DashboardViewModelTests, TrackingViewModelTests, LoginGateViewModelTests, AppViewModelTests, MainViewModelTests, AssetViewModelTests, AgentViewModelTests, AnalysisViewModelTests
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

`PanelState` (dans `src/Frontend/Client.Shared/Model/`) gère l'état de navigation d'une hiérarchie. `DashboardViewModel` en expose trois instances publiques :

```csharp
public PanelState AssetClassPanel  { get; } = new(PanelType.AssetClass);   // 3 ou 4 niveaux selon toggle
public PanelState SupportTypePanel { get; } = new(PanelType.SupportType);  // 3 niveaux
public PanelState RiskPanel        { get; } = new(PanelType.Risk);         // 2 niveaux
```

Méthodes : `DrillDown(name)`, `GoBack()`. Propriétés : `Level`, `CanGoBack`, `IsAtLeafLevel`, `Selected(level)`.

L'état de navigation de la page est porté par `DashboardViewModel`, pas par `Dashboard.razor` : `ActivePanel` / `HasActivePanel` (le panel descendu d'au moins un niveau — déduit, pas stocké), `SelectedZone` / `SelectedSector` (mutuellement exclusifs), `GeoClass` (`Stocks` ou `Bonds` juste sous la classe d'actifs, sinon `null`), `ShowEtfGroupingToggle`, et les commandes `DrillDown(panel, name)` (sans effet au niveau feuille, remet à zéro zone et secteur), `GoBack(panel)`, `SelectZone`, `SelectSector`, `ClearGeographySelection`. La vue ne garde que les palettes de couleurs (`ColorsFor(panel)`).

Le titre d'un panel (ex : "Classes d'actifs") est calculé par `DashboardViewModel.GetPanelTitle(panel)` — jamais par `PanelState` directement.

**Ne pas utiliser `panel.IsAtLeafLevel` directement dans les Views** — appeler `ViewModel.IsLeafLevel(panel)` qui prend en compte le toggle ETF et le type de panel.

### 7.2 API unifiée du ViewModel

```csharp
IReadOnlyList<DistributionItem> GetDistribution(PanelState panel)  // données du donut
IReadOnlyList<AssetDto>         GetAssetsForPanel(PanelState panel) // données du tableau (feuille seulement)
bool                            IsLeafLevel(PanelState panel)       // true si niveau feuille atteint
decimal                         GetDistributionTotal(PanelState panel) // total du pied de DistributionTable
string                          GetZonesTitle / GetSectorsTitle / GetSelectionTitle(PanelState panel) // titres localisés : donuts zones / secteurs, liste après sélection
```

Règle commune aux tableaux : le total du pied est toujours fourni au composant (paramètre `Total` de `DistributionTable`, `CapitalTotal`/`CouponsTotal`/`Total` de `BondScheduleDetailTable`) ou calculé par un ViewModel (`AssetViewModel.GetTotal`) — jamais calculé dans le balisage Razor.

`GetDistribution` sélectionne automatiquement le bon filtre et le bon groupement selon `panel.Type`, `panel.Level` et `EtfStocksGroupByInformation`.

**`DashboardViewModel` est une façade** : son API publique ci-dessus est celle que lit `Dashboard.razor`, mais il ne calcule rien lui-même. Il garde le chargement (`InitializeAsync`), l'état de navigation, les commandes et les titres, et délègue en passant `ActiveAssets()` (actifs à valeur > 0, filtre tenu à un seul endroit) et `_assetTypeRef` :

| Méthode(s) du ViewModel | Déléguée(s) à |
|---|---|
| `GetDistribution`, `GetAssetsForPanel`, libellé `AssetType` de `GetPanelTitle` | `IPanelDistributionService` (`GetDistribution`, `GetLeafAssets`, `TranslateAssetType`) |
| `GetAssetsForZone`, `GetZoneCoefficients`, `GetAssetsForSector` | `GeographySectorFilter` |
| `GetGeographyForClass`, `GetSectorForClass` | `DistributionCalculator.FromApi` |
| Les 10 propriétés de variation | `VariationCalculator` |

`DistributionCalculator.Compute(assets, groupBy, toLabel)` est le moteur commun des donuts calculés côté client : regroupement par `groupBy`, somme de `CurrentTotal`, poids en % du total, tri décroissant.

`EtfStocksGroupByInformation` (bool, bindable via `@bind-Value`) active le regroupement des ETF_Stocks par champ `information` et ajoute un niveau intermédiaire dans la hiérarchie Classes d'actifs.

**Services appelés à l'initialisation (en parallèle) :**
```csharp
portfolioService.GetAssetsAsync(ct)            // → _assets
portfolioService.GetLastSnapshotAsync(ct)      // → LastSnapshot
portfolioService.GetMetricsAsync(ct)           // → _metrics (ROIC + AverageRisk)
portfolioService.GetSnapshotHistoryAsync(ct)   // → _snapshotHistory (variations J/S/M/YTD/1A)
portfolioService.GetGeographyDistributionAsync // → _geoStocks / _geoBonds    } via LoadDistributionAsync : un échec est journalisé en Warning
portfolioService.GetSectorDistributionAsync    // → _sectorStocks / _sectorBonds } et laisse le donut vide sans bloquer le reste
portfolioService.GetAssetTypeReferenceAsync    // → _assetTypeRef (labelFr + geoSectorEligible par AssetType)
```

Les donuts géographie **et** secteurs sont calculés par l'Api (`GeographyService`, `SectorService`) et seulement affichés ici : une seule règle pour le dashboard et pour le workflow d'analyse de l'application MAUI (`AssetDistributionSource`, qui appelle les mêmes routes). `GetSectorForClass` garde le nom du secteur comme clé (utilisée par le drill-down) et n'en traduit que le libellé. Les listes affichées au clic sur une zone ou un secteur (`GetAssetsForZone`, `GetAssetsForSector`) restent filtrées côté client, avec le même filtre `GeoSectorEligible` que l'Api, pour que leur total corresponde au donut.

**Propriétés de variation (calculées côté client depuis `_snapshotHistory`) :**

Deux familles de métriques (capital net engagé, ROIC Capital Engagé) × cinq périodes (J / S / M / YTD / 1A) = 10 propriétés.

| Famille | Préfixe propriété | Formule |
|---|---|---|
| Capital net engagé | `…VariationPercent` | `(last - ref) / ref × 100` — variation relative de `NetCapital` |
| ROIC Capital Engagé | `…ROICapitalEngagedVariation` | `(ROIC_today - ROIC_ref) / \|ROIC_ref\| × 100` |

Préfixes de période : `Daily` (J−1), `Weekly` (≤ J−7), `Monthly` (≤ J−30), `Ytd` (1er snapshot de l'année courante), `Yearly` (≤ J−365).
Ex. : `MonthlyVariationPercent`, `YtdROICapitalEngagedVariation`, `YearlyROICapitalEngagedVariation`.

Le calcul est fait par `VariationCalculator` (`src/Frontend/Client.Shared/Utils/`) : `SinceDaysBack(history, n)` pour J/S/M/1A, `SinceYearStart(history)` pour YTD, chacun surchargé pour `SnapshotDto` (sur `_snapshotHistory`, base `NetCapital` : variation du capital net) et `PerformancePointDto` (sur `_performanceHistory`, la série TWR de `GetIndexedHistoryAsync` : variation du `ROIC`). Les deux surcharges partagent une même implémentation générique privée. Pour J−1, la référence est le point précédent quelle que soit sa date (pas de snapshot le week-end). La variation du `ROIC` — c'est ce dernier qui alimente les puces `*ROICapitalEngagedVariation` sous la carte "ROI (Capital Engagé)", pour refléter la vraie performance du portefeuille plutôt qu'une variation relative du ratio ROI (petit dénominateur, amplifiait artificiellement l'écart).

Retournent `null` si historique insuffisant, si aucune référence n'est trouvée pour la période, ou si la valeur de référence (`NetCapital` ou `ROIC`) vaut `0`. Pour YTD avec un seul point dans l'année, la référence est ce point → `0 %`.

**`KpiCard` — slot `SubContent` :**
`KpiCard` accepte un `RenderFragment? SubContent` affiché à droite de la valeur (même ligne, `MudStack Row`). Utilisé pour les chips de variation J/S/M/YTD/1A dans `KpiHeader.razor`, rendues par le helper local `VariationChips(params (string Prefix, decimal? Value)[])` (une chip par période non nulle).

### 7.3 DrillDownDonut — directive @key obligatoire

ApexCharts for Blazor ne redessine pas le graphique sur simple mise à jour des paramètres. Toujours ajouter `@key` pour forcer la recréation du composant quand le niveau ou le toggle change :

```razor
<DrillDownDonut @key="@($"{panel.Type}:{panel.Level}:{ViewModel.EtfStocksGroupByInformation}")"
                Items="@ViewModel.GetDistribution(panel)" ... />
```

**Slot `TopRightContent`** : `RenderFragment` facultatif affiché en haut à droite du titre. Utilisé pour placer le `MudSwitch` ETF_Stocks dans `Dashboard.razor`. Ce contenu n'est rendu que si non null — le composant `DrillDownDonut` n'a aucune connaissance du toggle.

### 7.4 Extensions décimales

Toujours formater les montants et pourcentages via `DecimalExtensions` — voir `src/Frontend/Client.Shared/Docs/SPECS.md` §6 pour les signatures et exemples de sortie.

`ToEurAmount(hidden: bool = false)` masque le montant (`"*****"`) quand le mode confidentialité est actif — voir §7.6.

### 7.6 IPrivacyModeService — mode confidentialité (masquage des montants)

Service transverse avec état partagé + notification de changement (pattern réutilisable pour une future feature similaire, ex. dark mode). La persistance passe par `IKeyValueStore` — aucun accès direct à `IJSRuntime`/`localStorage` dans la RCL.

```csharp
public interface IPrivacyModeService
{
    bool IsHidden { get; }
    event Action? OnChange;
    Task InitializeAsync();
    Task ToggleAsync();
}
```

- Singleton (`AddInvestissementsClient`), état persisté via `IKeyValueStore` (clé `investissements.hideAmounts` ; localStorage en WASM, SecureStorage en MAUI).
- `MainLayout.razor` appelle `InitializeAsync()` une fois (`OnInitializedAsync`) et propose la bascule (`MudMenuItem`, icône `Visibility`/`VisibilityOff`) dans le menu « ⋮ » du `MudAppBar`.
- Tout composant affichant un montant € injecte `IPrivacyModeService`, s'abonne à `OnChange += StateHasChanged` dans `OnInitialized` et se désabonne via `IDisposable` — puis passe `Privacy.IsHidden` à `ToEurAmount(hidden)`.
- Pour les graphiques ApexCharts (formatters JS en chaîne statique dans `ApexChartOptions`, ex. tooltip de `DrillDownDonut`, axe Y de `BondScheduleChart`) : régénérer la chaîne du `Formatter` selon `Privacy.IsHidden` dans un handler `OnChange`, puis appeler `await _chart.UpdateOptionsAsync(false, false, false)` pour forcer le redraw JS (la simple mutation de `_options` ne suffit pas, contrairement au cas `@key` du §7.3 — ici le composant n'est pas recréé, juste rafraîchi).

### 7.5 BondScheduleChart — granularité trimestre/année et drill-down au clic sur une barre

L'Api (`GET /api/assets/bondschedule`) renvoie `BondScheduleDto[]` à la granularité **mois** (`Year` + `Month`). `TrackingViewModel.BondScheduleDisplayed` ré-agrège ces données en `BondSchedulePeriodDto[]` (`src/Frontend/Client.Shared/Model/BondSchedulePeriodDto.cs` — `Year`, `Quarter` nullable, `Amount` (capital), `Bonds`, `Coupons`, propriétés calculées `Label` = `"T{Quarter} {Year}"` ou `"{Year}"` et `Total` = capital + coupons), recalculé à chaque lecture selon `TrackingViewModel.BondScheduleQuarterlyView` (bool, pattern identique à `EtfStocksGroupByInformation` — pas de cache, pas d'appel réseau supplémentaire). `Tracking.razor` expose ce choix via un `MudSwitch` en `@bind-Value` : le setter de `BondScheduleQuarterlyView` réinitialise lui-même la sélection de drill-down quand la granularité change.

Contrairement à `DrillDownDonut` (donut, `OnDataPointSelection` → nom de la tranche via `Items.FirstOrDefault()?.Name`), `BondScheduleChart` est un graphique en barres dont `TItem = BondSchedulePeriodDto` n'a pas de propriété `Name` : `data.DataPoint.Items.FirstOrDefault()` renvoie directement le `BondSchedulePeriodDto` complet du point cliqué (`XValue` utilise `Label`), remonté au parent via `EventCallback<BondSchedulePeriodDto> OnPeriodClicked`.

`TrackingViewModel` porte l'entrée sélectionnée (`SelectedPeriodEntry`, `SelectPeriod(entry)`), comme `DashboardViewModel` porte l'état de drill-down (§7.1) : `Tracking.razor` ne garde aucun état et l'affiche via `BondScheduleDetailTable.razor` (paramètre `PeriodLabel` en `string`, pas `Year` en `int` — même style que `DistributionTable.razor` : bordure `#E9E9E7`, `Dense`, `Hover`, colonnes `Col_Name`/`Col_Capital`/`Col_Coupons`/`Table_Total`, ligne de pied `Table_Total`, `NoRecordsContent` sur `Empty_NoData`). Les totaux du pied sont fournis (`CapitalTotal`, `CouponsTotal`, `Total` = `SelectedPeriodEntry.Amount`/`.Coupons`/`.Total`) et le total d'une ligne vient de `BondScheduleItemDto.Total` (propriété `[JsonIgnore]`) : aucun calcul dans le balisage. Le composant affiche tel quel les `Bonds` reçus : la fusion par nom (une obligation peut avoir une ligne coupon et une ligne capital dans la même période), le filtre (lignes sans rien à percevoir masquées) et le tri décroissant sur capital + coupons sont faits par `TrackingViewModel.SelectedPeriodBonds`. `BondScheduleChart` empile deux `ApexPointSeries` (`Amount` puis `Coupons`, `Chart.Stacked = true`).

Layout responsive (`MudGrid`/`MudItem xs="12" md="X"`) : graphique en `md="12"` tant qu'aucune période n'est sélectionnée, puis `md="7"` dès le premier clic pour laisser la place au tableau en `md="5"` à droite. En dessous du breakpoint `md`, les deux blocs passent en `xs="12"` (empilés). Le `MudGrid` et le `MudItem` du graphique ont `Style="height:100%;"` — sans quoi le `height:100%` du `MudPaper` interne à `BondScheduleChart` se réduit à la hauteur du contenu (le `MudGrid` ne propage pas la hauteur de son conteneur par défaut).

`<BondScheduleChart>` a un `@key="@($"{ViewModel.BondScheduleQuarterlyView}:{ViewModel.SelectedPeriodEntry is null}")"` — combine deux raisons de forcer la recréation de l'instance ApexCharts JS sous-jacente : (1) un clic changeant `md="12"` en `md="7"` (ou l'inverse) sans quoi le graphique continue de se redessiner à l'ancienne largeur pendant que `MudTable` apparaît déjà dans son propre `MudItem`, provoquant un chevauchement visuel en cas de clics rapides successifs ; (2) la bascule du `MudSwitch` trimestre/année — sans `BondScheduleQuarterlyView` dans la clé, `Items` change bien de contenu (nouveaux `BondSchedulePeriodDto[]`) mais ApexCharts.Blazor ne redessine pas le graphique sur simple mise à jour du paramètre, le switch semble alors ne rien faire. Même règle que `@key` sur `DrillDownDonut` (§7.3) : forcer la recréation du composant chaque fois qu'un changement doit être suivi d'un redessin ApexCharts.

### 7.7 ISessionService — écran de connexion (mot de passe du dashboard)

`App.razor` est le point de garde unique (via `AppViewModel.IsAuthenticated` / `OnChange` / `InitializeAsync()`, qui relaient `ISessionService` : ni `App.razor` ni `MainLayout` n'injectent `ISessionService` ; la déconnexion passe par `MainViewModel.LogoutAsync()`) : tant que `ISessionService.IsAuthenticated` est faux, il affiche `src/Frontend/Client.Shared/Shared/LoginGate.razor` à la place du `<Router>` — aucune route du dashboard n'est jamais montée sans authentification préalable.

- `SessionService` (singleton, pattern similaire à `IPrivacyModeService`) stocke `{password, expiresAt}` (JSON, clé `investissements.dashboardSession`) via `IKeyValueStore` et revérifie le mot de passe au démarrage via `GET /api/auth/verify`
- **Expiration glissante d'1h** : `DashboardPasswordHandler` appelle `ExtendSessionAsync()` après chaque requête réussie (repousse `expiresAt` à +1h) et court-circuite (`LogoutAsync()`, 401 local sans appeler l'Api) si `IsSessionExpired` avant d'envoyer quoi que ce soit — après 1h sans aucun appel réussi, l'utilisateur retombe sur `LoginGate` au prochain appel ou rechargement
- `LoginAsync` renvoie un `LoginResult` : seul un `401` signifie « mot de passe refusé » (`Login_InvalidPassword`). Toute autre réponse en échec, ou une erreur réseau, donne `ServiceUnavailable` (`Login_ServiceUnavailable`) et est journalisée : lors d'une panne, le proxy d'Azure Static Web Apps répond 500 ou 503, ce qui s'affichait auparavant comme un mauvais mot de passe. Au démarrage, une session mémorisée qui ne peut pas être vérifiée reste stockée. `DASHBOARD_PASSWORD` absent de la configuration donne un `401`, donc « mot de passe incorrect »
- `DashboardPasswordHandler` (`DelegatingHandler`) ajoute automatiquement le header `x-dashboard-password` sur chaque requête du `HttpClient` typé `IPortfolioService` (enregistré via `.AddHttpMessageHandler<DashboardPasswordHandler>()` dans `AddInvestissementsClient`) — aucun appel manuel requis dans les ViewModels
- Vérification côté serveur : `src/Backend/Api/Middleware/DashboardAuthMiddleware.cs` (voir `src/Backend/Api/Docs/CLAUDE.md`) — cette vérification reste stateless (pas de notion d'expiration côté Api, uniquement côté Client)
- Remplace l'authentification native Azure Static Web Apps (rôles/Invitations), abandonnée pour manque de fiabilité sur le plan Free — voir `CLAUDE.md` (racine) §5.2.4
- **Limite connue (WASM)** : avec `LocalStorageKeyValueStore`, le mot de passe et l'expiration sont stockés en clair dans `localStorage`, visibles via les DevTools — protection suffisante contre un visiteur lambda, pas contre un accès physique à la session du navigateur. Côté MAUI, `SecureStorageKeyValueStore` les chiffre via l'OS.

### 7.8 Synchronisation manuelle — MainViewModel / ISyncService

Le menu « ⋮ » de `MainLayout` contient l'entrée « Synchroniser » : `MainViewModel.SyncAsync()` → `ISyncService` → `POST /api/sync` (voir `src/Backend/Api/Docs/SPECS.md` §2.12). `IsSyncing` désactive l'entrée et affiche un `MudProgressCircular` pendant l'appel. Le `MainViewModel` choisit le message localisé (`SyncMessage`, `IsSyncSuccess`) : succès (`Sync_Success_WithAdded` si `AddedCount > 0`, sinon `Sync_Success_NoAdded`) ou erreur (`Sync_Error` + message de l'Api ou de l'exception, journalisée). `MainLayout` se contente de l'afficher dans un `ISnackbar`. Après un succès, `MainViewModel` recharge les pages via les `IRefreshableViewModel` injectés (`DashboardViewModel`, `TrackingViewModel`, mêmes instances que les pages) et n'annonce le résultat qu'à la fin : `IsSyncing` couvre aussi le rechargement. Si le rechargement échoue, le message est `Sync_RefreshError` et l'écran garde ses données.

`ReloadAsync` ne touche pas à `IsLoading` : l'écran actuel reste affiché, et ne fait rien si la page n'a jamais été chargée, car son premier chargement lira des données fraîches. Les données ne sont remplacées qu'une fois toutes les lectures réussies. Les pages s'abonnent à `DataChanged` pour se réafficher, et `DataVersion` entre dans le `@key` des graphiques, qu'ApexCharts ne redessine pas sur un simple changement de données (§7.3). Le drill-down du dashboard est conservé ; la période sélectionnée dans l'échéancier est effacée.

### 7.9 AssetTable — colonne coefficient de zone

`AssetTable` accepte `IReadOnlyDictionary<int, decimal>? Coefficients` (clé = `AssetDto.Id`, valeur 0–1). Fourni uniquement par le drill-down géographique (`DashboardViewModel.GetZoneCoefficients(assetClass, zone)`), il ajoute la colonne « Coefficient zone » (`Col_GeoCoefficient`, après « Valeur actuelle ») et le total du pied de tableau devient **pondéré** (Σ valeur × coefficient) au lieu de la somme brute — calcul porté par `AssetViewModel.GetTotal(assets, coefficients)`, pas par le composant. Ordre des colonnes : Nom, Valeur actuelle, [Coefficient zone], Plus-value latente, ROI, Rendement.

### 7.10 IAgentRunner — lancement d'un agent IA depuis la liste des actions

Seam **optionnel** (même principe qu'`IKeyValueStore`, mais volontairement **non enregistré** par `AddInvestissementsClient`) : `Client.Shared` ne peut pas référencer `AgentAI.Core` (identité Azure, MCP), et les agents ne tournent pas dans le site WASM (CLAUDE.md racine §14). Seul l'hôte `src/Frontend/Maui/` enregistre une implémentation.

- `IAgentRunner.RunAsync(AgentChoice, assetName, isin, ct)` → texte de la réponse ; `isin` (`AssetDto.Isin`, peut être vide/`null`) est transmis aux agents Stock (lever l'ambiguïté sur la valeur) et News (résolu en ticker pour le flux Yahoo Finance) ; `AgentChoice` = `Stock` | `News` (enum propre à `Client.Shared`, indépendante d'`AgentKind`).
- Deux ViewModels (scoped, runner **optionnel** dans leur constructeur) :
  - `AssetViewModel` (celui d'`AssetTable`) : `IsAgentAvailable` (runner enregistré ?) et `CanLaunchAgent(AssetDto)` (runner présent **et** `AssetType == Stock`). Sans runner (site WASM), aucune colonne ni bouton.
  - `AgentViewModel` (celui d'`AgentResultDialog`) : `IsRunning`, `Response`, `Error`, `LaunchAsync(AgentChoice, assetName, isin = null)` (réinitialise l'état, journalise et capture l'erreur, ignore l'annulation) et `Cancel()`. Un seul dialogue modal étant ouvert à la fois, l'état est remis à zéro à chaque lancement. Avec runner, une icône `SmartToy` apparaît **uniquement sur les lignes `AssetType == Stock`** (`AssetTypeNames.Stock`).
- Clic → `AgentPickerDialog` (choix Stock / News, annulable) → `AgentResultDialog` (lie l'affichage à `AgentViewModel` : progression `MudProgressLinear`, puis réponse ou erreur ; sa libération appelle `Cancel()`). Les vues ne contiennent que l'ouverture des dialogues (`IDialogService`) : aucun paramètre à propager depuis `Dashboard.razor`.
- Tests : les classes de test de dialogues implémentent `IAsyncLifetime` et libèrent le contexte via `base.DisposeAsync()` (les services de dialogue MudBlazor n'implémentent que `IAsyncDisposable`).

### 7.11 Exception assumée : bornes de l'axe Y de `HistoryChart`

`HistoryChart.OnParametersSet` calcule les bornes de l'axe Y (min et max des trois séries, ±20) directement dans le composant. C'est une règle de rendu du graphique, au même titre que ses couleurs ou son style de ligne (comme les formatters d'`ApexChartOptions`), pas une règle de présentation de données : elle reste dans le composant.

### 7.12 (supprimé) Page Paramètres des agents

La page Paramètres (`IAgentSettings`, `SettingsViewModel`, `Views/Settings.razor`, `AgentSecretKeys`) a été supprimée : l'endpoint Foundry et le modèle sont servis par l'Api (`GET /api/agents/config`, App Settings `AGENT_FOUNDRY_ENDPOINT`/`AGENT_FOUNDRY_MODEL`) et le serveur MCP accepte le mot de passe du dashboard. L'application MAUI n'a donc plus rien à faire saisir (voir `src/Frontend/Maui/Docs/CLAUDE.md` §4).

### 7.13 IPortfolioReviewRunner — page Analyse (MAUI uniquement)

Troisième seam optionnel, **non enregistré** par `AddInvestissementsClient` (même raison que §7.10) : `IPortfolioReviewRunner.RunAsync(DateOnly date, IProgress<AnalysisStepUpdate>? progress, ct)` → `Task<SynthesisReport>` exécute le workflow `PortfolioReview` d'`AgentAI.Core` (allocation, contexte et répartitions en parallèle, puis synthèse) et renvoie le rapport. `SynthesisReport`, `ScenarioAssessment` et `Recommendation` sont dans `src/Common/Shared/Models`, cette bibliothèque ne référençant pas `AgentAI.Core`. Distinct d'`IAgentRunner`, qui renvoie le texte libre d'un agent.

- `MainViewModel.IsAnalysisAvailable` (runner présent) affiche le lien **Analyse** (`/analyse`) juste après **Suivi** dans la barre d'application.
- `AnalysisViewModel` (scoped, runner optionnel, `TimeProvider` — enregistré par `AddInvestissementsClient` en `TryAddSingleton(TimeProvider.System)`) : `IsAvailable`, `IsRunning`, `IsCancelled`, `Report`, `ReportDate`, `Error`, `Steps` (`AnalysisStepState` : statut et phase de chaque étape dans l'ordre d'affichage, liste remplacée à chaque mise à jour et non modifiée, car le runner peut écrire depuis un autre thread), `ShowSteps` (pendant l'exécution, et après un échec si une étape a démarré), `StepsChanged`, `LaunchAsync()` (date du jour, réinitialise l'état, journalise et capture l'erreur ; une annulation n'est pas une erreur : elle pose `IsCancelled` ; en fin d'exécution, une étape encore « en cours » repasse « en attente », car elle ne finira jamais) et `Cancel()`. Il passe au runner un `IProgress` synchrone plutôt qu'un `Progress<T>`, qui différerait chaque mise à jour : l'une d'elles pourrait arriver après la fin de l'exécution. Étant scoped, il conserve le rapport tant que l'application reste ouverte : revenir sur la page ne relance pas d'analyse payante. Le rapport n'est pas enregistré sur le disque.
- `Views/Analysis.razor` (`/analyse`) : l'analyse démarre **au clic sur le bouton** `Analysis_Launch`, jamais à l'ouverture de la page, car chaque exécution consomme des tokens ; bouton désactivé, bouton `Analysis_Cancel` à côté et carte des étapes (`Shared/AnalysisStepList.razor` : titre `Analysis_Steps_*`, phases `Analysis_Phase_*` qui reflètent la structure du workflow, soit la collecte en parallèle puis la rédaction, libellés `Analysis_Step_*`, statut porté par la seule icône, sans texte visible, avec `Analysis_StepStatus_*` en titre ou `aria-label` pour les lecteurs d'écran ; largeur limitée à 480 px) pendant l'exécution, rafraîchie par `StepsChanged` via `InvokeAsync(StateHasChanged)`, puis date et rapport (`Shared/SynthesisReportView.razor`), « Analyse annulée. » ou erreur `MudAlert` ; les deux boutons restent côte à côte, car un double-clic sur **Lancer** ne doit pas annuler l'exécution qu'il vient de démarrer ; le bouton **Annuler** et la sortie de la page (`Dispose`) annulent tous deux l'exécution. Rien n'est affiché ni lancé sans runner (site WASM).
- `Shared/SynthesisReportView.razor` : verdict par scénario (`Analysis_Verdict_*`), avertissements, recommandations dans l'ordre reçu, forces et faiblesses ; une section vide est masquée. Direction, niveau et confiance sont traduits (`Analysis_Direction_*`, `Analysis_Level_*`, `Analysis_Confidence_*`). La cible n'est traduite que pour une classe d'actifs : un thème ou une ligne est un texte libre du modèle. Le bouton de masquage des montants ne s'applique pas à ce texte.

## 8. Localisation

Toutes les chaînes UI sont externalisées dans `src/Frontend/Client.Shared/Resources/Translations.resx`.  
Le service `ILocalizationService` (implémenté par `LocalizationService`) est le seul point d'accès — ne jamais appeler `IStringLocalizer<Translations>` directement.

```csharp
// Dans un ViewModel (injection constructeur)
public DashboardViewModel(IPortfolioService portfolioService, ILocalizationService localizationService,
    IPanelDistributionService panelDistributionService, ILogger<DashboardViewModel> logger)

// Dans un composant Razor (injection directe)
@inject ILocalizationService L
// puis : @L.Translate("Ma_Cle")
```

`ILocalizationService` est enregistré **singleton** dans `AddInvestissementsClient`. Il est déjà importé globalement via `_Imports.razor` — aucun `@using` supplémentaire requis dans les composants.

Fallback : si une clé n'existe pas dans le `.resx`, `Translate()` retourne la clé brute (jamais d'exception).

**Exception — libellés `AssetType`** : ne passent pas par `Translations.resx` ni `ILocalizationService`. Ils sont lus depuis l'onglet `AssetType` du Sheet (colonne `LabelFR`) via `GET /api/assets/types/reference`, résolus dans `PanelDistributionService.TranslateAssetType(assetTypes, assetType)` (fallback : nom brut si `labelFr` absent). Ajouter un nouveau `AssetType` ne nécessite donc aucune entrée `.resx`.

---

## 9. Tests

Framework : xUnit + bUnit. Nommage : `[MethodName]_[Scenario]_[ExpectedResult]`.

- `TestData` dans `tests/Client.Tests/Helpers/` fournit les factories `Asset(...)`, `Snapshot(...)` et `PerformancePoint(...)`
- `TestData.AddLocalizationMock(this IServiceCollection services)` — extension à appeler dans le constructeur de tout test de composant qui rend un composant injectant `ILocalizationService`. Le mock utilise `ResourceManager` sur les vraies ressources compilées → les assertions peuvent vérifier les chaînes françaises.
- `TestData.AddPrivacyModeMock(this IServiceCollection services, bool isHidden = false)` — extension à appeler dans le constructeur de tout test de composant qui affiche un montant € (injecte `IPrivacyModeService`). Rappeler avec `isHidden: true` dans un test dédié pour vérifier le masquage.
- `DashboardViewModel` — instancier avec `Mock<IPortfolioService>` + `Mock<ILocalizationService>` (setup `Translate(key) → key`) + un **vrai** `PanelDistributionService` construit sur ce même mock (les tests du ViewModel couvrent ainsi la façade de bout en bout) + `NullLogger<DashboardViewModel>.Instance` (ou un `Mock<ILogger<…>>` pour vérifier la journalisation des échecs)
- `PanelDistributionService` — instancier avec un `Mock<ILocalizationService>` ; les calculateurs statiques de `Utils/` se testent sans aucun mock
- `TrackingViewModel` — instancier avec `Mock<IPortfolioService>` + `Mock<ILocalizationService>`
- Les tests de composants héritent de `BunitContext` et appellent `Services.AddMudServices(...)` + `Services.AddLocalizationMock()`
- `IKeyValueStore` : les tests de services (`SessionService`, `PrivacyModeService`) injectent `Mock<IKeyValueStore>` ; `LocalStorageKeyValueStore` est testé avec `Mock<IJSRuntime>`
- `ServiceCollectionExtensionsTests` vérifie que `AddInvestissementsClient` permet de résoudre services et ViewModels
- Lancer les tests : `dotnet test "tests/Client.Tests/InvestissementsDashboard.Client.Tests.csproj"` — non exécutés par la CI actuelle (seul `Api.Tests` l'est)

---

## 10. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
