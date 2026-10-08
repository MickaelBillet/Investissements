# Client.Shared — Architecture technique

## 1. Rôle
Bibliothèque Razor (RCL) contenant **tout le code Blazor du dashboard** de visualisation du portefeuille d'investissement personnel : `App`, vues, composants, ViewModels, services, ressources. Elle est consommée par deux hôtes :

| Hôte | Projet | Doc |
|---|---|---|
| Site web (Blazor WebAssembly, Azure Static Web Apps) | `Client/` | `Client/Docs/CLAUDE.md` |
| Application Windows (MAUI + `BlazorWebView`) | `Maui/` | `Maui/Docs/CLAUDE.md` |

Elle consomme les endpoints Azure Functions (`/api/*`), n'embarque aucune clé API ni donnée sensible. Les namespaces sont restés `InvestissementsDashboard.Client.*` (`RootNamespace` du `csproj`) — ne pas les renommer : ils déterminent aussi le nom du manifeste de ressources `.resx`.

### Contrat avec les hôtes
Un hôte doit :
1. enregistrer une implémentation de `IKeyValueStore` (stockage persistant de la session et du mode confidentialité) — `LocalStorageKeyValueStore` (fournie, WASM) ou `SecureStorageKeyValueStore` (`Maui/`) ;
2. appeler `services.AddInvestissementsClient(apiBaseUri)` (`Extensions/ServiceCollectionExtensions.cs`) : enregistre MudBlazor, ApexCharts, la localisation, `ClientOptions`, `ILocalizationService`, `IPrivacyModeService`, `ISessionService` (avec son propre `HttpClient` sans handler de mot de passe), `DashboardPasswordHandler`, les `HttpClient` typés `IPortfolioService`/`ISyncService`, les sept ViewModels (scoped) et force la culture `fr-FR` ;
3. monter `App` sur `#app` et `HeadOutlet` sur `head::after`, et lier dans sa page d'hébergement le CSS de MudBlazor, `_content/InvestissementsDashboard.Client.Shared/css/app.css` et le bundle CSS isolé.

L'URL de l'Api est toujours fournie par l'hôte (`ClientOptions.ApiBaseUri`) — jamais déduite de `NavigationManager.BaseUri`, qui n'a aucun sens dans un `BlazorWebView`. Assets de la RCL : `_content/InvestissementsDashboard.Client.Shared/` (`css/app.css`, `icon.svg`).

## 2. Stack

| Élément | Choix |
|---|---|
| Runtime | .NET 10 — `Microsoft.NET.Sdk.Razor` (RCL), exécutée en WASM ou dans un `BlazorWebView` |
| UI Components | MudBlazor 9.9.0 |
| Graphiques | Blazor-ApexCharts 7.0.0 |
| Tests | xUnit + bUnit 2.9.0 + Moq — projet `Client.Tests/` (référence `Client.Shared`) |
| Déploiement | via les hôtes : Azure Static Web Apps (`Client/`), poste Windows (`Maui/`) |

## 3. Architecture — MVVM

- **Views** (`Client.Shared/Views/`) — pages .razor, logique de rendu et navigation locale uniquement
- **ViewModels** (`Client.Shared/ViewModels/`) — agrégation et calculs de présentation, pas de logique UI
- **Model** (`Client.Shared/Model/`) — modèles de données côté client uniquement
- **Shared** (`Client.Shared/Shared/`) — composants réutilisables transverses
- **Services** (`Client.Shared/Services/`) — appels HTTP vers les Azure Functions

Les modèles partagés Client + Api sont dans `Shared/Models/` à la racine du repo.

## 4. Structure des dossiers

```
Client.Shared/
├── App.razor      → thème MudBlazor, providers, garde d'authentification (LoginGate) puis <Router>
├── _Imports.razor
├── Extensions/    → DecimalExtensions.cs (ToEurAmount, ToPercentage, ToSignedPercentage, CssRoiClass),
│                    ServiceCollectionExtensions.cs (AddInvestissementsClient)
├── Layout/        → MainLayout.razor (barre d'application + menu « ⋮ »)
├── Model/         → DistributionItem.cs, IndexedPoint.cs, PanelState.cs, BondSchedulePeriodDto.cs
├── Pages/         → Home.razor (/home-legacy, redirige vers /)
├── Resources/     → Translations.cs (classe marqueur), Translations.resx (toutes les chaînes UI)
├── Services/      → IKeyValueStore.cs, LocalStorageKeyValueStore.cs, ClientOptions.cs,
│                    IPortfolioService.cs, PortfolioService.cs, ISyncService.cs, SyncService.cs,
│                    ILocalizationService.cs, LocalizationService.cs,
│                    IPrivacyModeService.cs, PrivacyModeService.cs,
│                    ISessionService.cs, SessionService.cs, DashboardPasswordHandler.cs,
│                    IAgentRunner.cs (seam optionnel, voir §7.10)
├── Shared/        → DrillDownDonut.razor, AssetTable.razor, DistributionTable.razor,
│                    KpiHeader.razor, KpiCard.razor, HistoryChart.razor, BondScheduleChart.razor,
│                    BondScheduleDetailTable.razor, LoginGate.razor,
│                    Dialogs/AgentPickerDialog.razor, Dialogs/AgentResultDialog.razor
├── ViewModels/    → DashboardViewModel.cs, TrackingViewModel.cs, LoginGateViewModel.cs, AppViewModel.cs, MainViewModel.cs, AssetViewModel.cs, AgentViewModel.cs
├── Views/         → Dashboard.razor (/), Tracking.razor (/suivi)
├── wwwroot/       → css/app.css, icon.svg (servis sous _content/InvestissementsDashboard.Client.Shared/)
└── Docs/          → CLAUDE.md, SPECS.md (ce dossier)

Client.Tests/      (xUnit + bUnit, référence Client.Shared)
├── Components/    → KpiHeader, AssetTable, DistributionTable, DrillDownDonut, HistoryChart,
│                    BondScheduleChart, BondScheduleDetailTable, LoginGate
├── Extensions/    → DecimalExtensionsTests, ServiceCollectionExtensionsTests
├── Helpers/       → TestData (factories AssetDto, SnapshotDto, PerformancePointDto + mocks)
├── Models/        → PanelStateTests
├── Services/      → SessionServiceTests, PrivacyModeServiceTests, LocalStorageKeyValueStoreTests
└── ViewModels/    → DashboardViewModelTests, TrackingViewModelTests, LoginGateViewModelTests, AppViewModelTests, MainViewModelTests, AssetViewModelTests, AgentViewModelTests
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

`PanelState` (dans `Client.Shared/Model/`) gère l'état de navigation d'une hiérarchie. `DashboardViewModel` en expose trois instances publiques :

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

Règle commune aux tableaux : le total du pied est toujours fourni au composant (paramètre `Total` de `DistributionTable` et `BondScheduleDetailTable`) ou calculé par un ViewModel (`AssetViewModel.GetTotal`) — jamais calculé dans le balisage Razor.

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
<DrillDownDonut @key="@($"{panel.Type}:{panel.Level}:{ViewModel.EtfStocksGroupByInformation}")"
                Items="@ViewModel.GetDistribution(panel)" ... />
```

**Slot `TopRightContent`** : `RenderFragment` facultatif affiché en haut à droite du titre. Utilisé pour placer le `MudSwitch` ETF_Stocks dans `Dashboard.razor`. Ce contenu n'est rendu que si non null — le composant `DrillDownDonut` n'a aucune connaissance du toggle.

### 7.4 Extensions décimales

Toujours formater les montants et pourcentages via `DecimalExtensions` — voir `Client.Shared/Docs/SPECS.md` §6 pour les signatures et exemples de sortie.

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

L'Api (`GET /api/assets/bondschedule`) renvoie `BondScheduleDto[]` à la granularité **mois** (`Year` + `Month`). `TrackingViewModel.BondScheduleDisplayed` ré-agrège ces données en `BondSchedulePeriodDto[]` (`Client.Shared/Model/BondSchedulePeriodDto.cs` — `Year`, `Quarter` nullable, `Amount`, `Bonds`, propriété calculée `Label` = `"T{Quarter} {Year}"` ou `"{Year}"`), recalculé à chaque lecture selon `TrackingViewModel.BondScheduleQuarterlyView` (bool, pattern identique à `EtfStocksGroupByInformation` — pas de cache, pas d'appel réseau supplémentaire). `Tracking.razor` expose ce choix via un `MudSwitch` en `@bind-Value` : le setter de `BondScheduleQuarterlyView` réinitialise lui-même la sélection de drill-down quand la granularité change.

Contrairement à `DrillDownDonut` (donut, `OnDataPointSelection` → nom de la tranche via `Items.FirstOrDefault()?.Name`), `BondScheduleChart` est un graphique en barres dont `TItem = BondSchedulePeriodDto` n'a pas de propriété `Name` : `data.DataPoint.Items.FirstOrDefault()` renvoie directement le `BondSchedulePeriodDto` complet du point cliqué (`XValue` utilise `Label`), remonté au parent via `EventCallback<BondSchedulePeriodDto> OnPeriodClicked`.

`TrackingViewModel` porte l'entrée sélectionnée (`SelectedPeriodEntry`, `SelectPeriod(entry)`), comme `DashboardViewModel` porte l'état de drill-down (§7.1) : `Tracking.razor` ne garde aucun état et l'affiche via `BondScheduleDetailTable.razor` (paramètre `PeriodLabel` en `string`, pas `Year` en `int` — même style que `DistributionTable.razor` : bordure `#E9E9E7`, `Dense`, `Hover`, colonnes `Col_Name`/`Col_CurrentValue`, ligne `Table_Total`, `NoRecordsContent` sur `Empty_NoData`). Le composant affiche tel quel les `Bonds` reçus : le filtre (obligations à 0 masquées) et le tri décroissant sont faits par `TrackingViewModel.SelectedPeriodBonds`.

Layout responsive (`MudGrid`/`MudItem xs="12" md="X"`) : graphique en `md="12"` tant qu'aucune période n'est sélectionnée, puis `md="7"` dès le premier clic pour laisser la place au tableau en `md="5"` à droite. En dessous du breakpoint `md`, les deux blocs passent en `xs="12"` (empilés). Le `MudGrid` et le `MudItem` du graphique ont `Style="height:100%;"` — sans quoi le `height:100%` du `MudPaper` interne à `BondScheduleChart` se réduit à la hauteur du contenu (le `MudGrid` ne propage pas la hauteur de son conteneur par défaut).

`<BondScheduleChart>` a un `@key="@($"{ViewModel.BondScheduleQuarterlyView}:{ViewModel.SelectedPeriodEntry is null}")"` — combine deux raisons de forcer la recréation de l'instance ApexCharts JS sous-jacente : (1) un clic changeant `md="12"` en `md="7"` (ou l'inverse) sans quoi le graphique continue de se redessiner à l'ancienne largeur pendant que `MudTable` apparaît déjà dans son propre `MudItem`, provoquant un chevauchement visuel en cas de clics rapides successifs ; (2) la bascule du `MudSwitch` trimestre/année — sans `BondScheduleQuarterlyView` dans la clé, `Items` change bien de contenu (nouveaux `BondSchedulePeriodDto[]`) mais ApexCharts.Blazor ne redessine pas le graphique sur simple mise à jour du paramètre, le switch semble alors ne rien faire. Même règle que `@key` sur `DrillDownDonut` (§7.3) : forcer la recréation du composant chaque fois qu'un changement doit être suivi d'un redessin ApexCharts.

### 7.7 ISessionService — écran de connexion (mot de passe du dashboard)

`App.razor` est le point de garde unique (via `AppViewModel.IsAuthenticated` / `OnChange` / `InitializeAsync()`, qui relaient `ISessionService` : ni `App.razor` ni `MainLayout` n'injectent `ISessionService` ; la déconnexion passe par `MainViewModel.LogoutAsync()`) : tant que `ISessionService.IsAuthenticated` est faux, il affiche `Client.Shared/Shared/LoginGate.razor` à la place du `<Router>` — aucune route du dashboard n'est jamais montée sans authentification préalable.

- `SessionService` (singleton, pattern similaire à `IPrivacyModeService`) stocke `{password, expiresAt}` (JSON, clé `investissements.dashboardSession`) via `IKeyValueStore` et revérifie le mot de passe au démarrage via `GET /api/auth/verify`
- **Expiration glissante d'1h** : `DashboardPasswordHandler` appelle `ExtendSessionAsync()` après chaque requête réussie (repousse `expiresAt` à +1h) et court-circuite (`LogoutAsync()`, 401 local sans appeler l'Api) si `IsSessionExpired` avant d'envoyer quoi que ce soit — après 1h sans aucun appel réussi, l'utilisateur retombe sur `LoginGate` au prochain appel ou rechargement
- `DashboardPasswordHandler` (`DelegatingHandler`) ajoute automatiquement le header `x-dashboard-password` sur chaque requête du `HttpClient` typé `IPortfolioService` (enregistré via `.AddHttpMessageHandler<DashboardPasswordHandler>()` dans `AddInvestissementsClient`) — aucun appel manuel requis dans les ViewModels
- Vérification côté serveur : `Api/Middleware/DashboardAuthMiddleware.cs` (voir `Api/Docs/CLAUDE.md`) — cette vérification reste stateless (pas de notion d'expiration côté Api, uniquement côté Client)
- Remplace l'authentification native Azure Static Web Apps (rôles/Invitations), abandonnée pour manque de fiabilité sur le plan Free — voir `CLAUDE.md` (racine) §5.2.4
- **Limite connue (WASM)** : avec `LocalStorageKeyValueStore`, le mot de passe et l'expiration sont stockés en clair dans `localStorage`, visibles via les DevTools — protection suffisante contre un visiteur lambda, pas contre un accès physique à la session du navigateur. Côté MAUI, `SecureStorageKeyValueStore` les chiffre via l'OS.
- **Anomalie connue** : `GET /api/auth/verify` répond actuellement toujours `200` (route exemptée du middleware) — l'écran de connexion accepte donc n'importe quel mot de passe tant que l'Api n'est pas corrigée ; les données restent protégées (`401` sans le bon mot de passe). Voir `Api/Docs/CLAUDE.md` §6.

### 7.8 Synchronisation manuelle — MainViewModel / ISyncService

Le menu « ⋮ » de `MainLayout` contient l'entrée « Synchroniser » : `MainViewModel.SyncAsync()` → `ISyncService` → `POST /api/sync` (voir `Api/Docs/SPECS.md` §2.12). `IsSyncing` désactive l'entrée et affiche un `MudProgressCircular` pendant l'appel. Le `MainViewModel` choisit le message localisé (`SyncMessage`, `IsSyncSuccess`) : succès (`Sync_Success_WithAdded` si `AddedCount > 0`, sinon `Sync_Success_NoAdded`) ou erreur (`Sync_Error` + message de l'Api ou de l'exception, journalisée). `MainLayout` se contente de l'afficher dans un `ISnackbar`. Le `MainViewModel` ne porte aucune logique de rafraîchissement : les KPI se mettent à jour au rechargement.

### 7.9 AssetTable — colonne coefficient de zone

`AssetTable` accepte `IReadOnlyDictionary<int, decimal>? Coefficients` (clé = `AssetDto.Id`, valeur 0–1). Fourni uniquement par le drill-down géographique (`DashboardViewModel.GetZoneCoefficients(assetClass, zone)`), il ajoute la colonne « Coefficient zone » (`Col_GeoCoefficient`, après « Valeur actuelle ») et le total du pied de tableau devient **pondéré** (Σ valeur × coefficient) au lieu de la somme brute — calcul porté par `AssetViewModel.GetTotal(assets, coefficients)`, pas par le composant. Ordre des colonnes : Nom, Valeur actuelle, [Coefficient zone], Plus-value latente, ROI, Rendement.

### 7.10 IAgentRunner — lancement d'un agent IA depuis la liste des actions

Seam **optionnel** (même principe qu'`IKeyValueStore`, mais volontairement **non enregistré** par `AddInvestissementsClient`) : `Client.Shared` ne peut pas référencer `AgentAI.Core` (identité Azure, MCP), et les agents ne tournent pas dans le site WASM (CLAUDE.md racine §14). Seul l'hôte `Maui/` enregistre une implémentation.

- `IAgentRunner.RunAsync(AgentChoice, assetName, ct)` → texte de la réponse ; `AgentChoice` = `Stock` | `News` (enum propre à `Client.Shared`, indépendante d'`AgentKind`).
- Deux ViewModels (scoped, runner **optionnel** dans leur constructeur) :
  - `AssetViewModel` (celui d'`AssetTable`) : `IsAgentAvailable` (runner enregistré ?) et `CanLaunchAgent(AssetDto)` (runner présent **et** `AssetType == Stock`). Sans runner (site WASM), aucune colonne ni bouton.
  - `AgentViewModel` (celui d'`AgentResultDialog`) : `IsRunning`, `Response`, `Error`, `LaunchAsync(AgentChoice, assetName)` (réinitialise l'état, journalise et capture l'erreur, ignore l'annulation) et `Cancel()`. Un seul dialogue modal étant ouvert à la fois, l'état est remis à zéro à chaque lancement. Avec runner, une icône `SmartToy` apparaît **uniquement sur les lignes `AssetType == Stock`** (`AssetTypeNames.Stock`).
- Clic → `AgentPickerDialog` (choix Stock / News, annulable) → `AgentResultDialog` (lie l'affichage à `AgentViewModel` : progression `MudProgressLinear`, puis réponse ou erreur ; sa libération appelle `Cancel()`). Les vues ne contiennent que l'ouverture des dialogues (`IDialogService`) : aucun paramètre à propager depuis `Dashboard.razor`.
- Tests : les classes de test de dialogues implémentent `IAsyncLifetime` et libèrent le contexte via `base.DisposeAsync()` (les services de dialogue MudBlazor n'implémentent que `IAsyncDisposable`).

### 7.11 Exception assumée : bornes de l'axe Y de `HistoryChart`

`HistoryChart.OnParametersSet` calcule les bornes de l'axe Y (min et max des trois séries, ±20) directement dans le composant. C'est une règle de rendu du graphique, au même titre que ses couleurs ou son style de ligne (comme les formatters d'`ApexChartOptions`), pas une règle de présentation de données : elle reste dans le composant.

## 8. Localisation

Toutes les chaînes UI sont externalisées dans `Client.Shared/Resources/Translations.resx`.  
Le service `ILocalizationService` (implémenté par `LocalizationService`) est le seul point d'accès — ne jamais appeler `IStringLocalizer<Translations>` directement.

```csharp
// Dans un ViewModel (injection constructeur)
public DashboardViewModel(IPortfolioService portfolioService, ILocalizationService localizationService)

// Dans un composant Razor (injection directe)
@inject ILocalizationService L
// puis : @L.Translate("Ma_Cle")
```

`ILocalizationService` est enregistré **singleton** dans `AddInvestissementsClient`. Il est déjà importé globalement via `_Imports.razor` — aucun `@using` supplémentaire requis dans les composants.

Fallback : si une clé n'existe pas dans le `.resx`, `Translate()` retourne la clé brute (jamais d'exception).

**Exception — libellés `AssetType`** : ne passent pas par `Translations.resx` ni `ILocalizationService`. Ils sont lus depuis l'onglet `AssetType` du Sheet (colonne `LabelFR`) via `GET /api/assets/types/reference`, résolus dans `DashboardViewModel.TranslateAssetType(assetType)` (fallback : nom brut si `labelFr` absent). Ajouter un nouveau `AssetType` ne nécessite donc aucune entrée `.resx`.

---

## 9. Tests

Framework : xUnit + bUnit. Nommage : `[MethodName]_[Scenario]_[ExpectedResult]`.

- `TestData` dans `Client.Tests/Helpers/` fournit les factories `Asset(...)`, `Snapshot(...)` et `PerformancePoint(...)`
- `TestData.AddLocalizationMock(this IServiceCollection services)` — extension à appeler dans le constructeur de tout test de composant qui rend un composant injectant `ILocalizationService`. Le mock utilise `ResourceManager` sur les vraies ressources compilées → les assertions peuvent vérifier les chaînes françaises.
- `TestData.AddPrivacyModeMock(this IServiceCollection services, bool isHidden = false)` — extension à appeler dans le constructeur de tout test de composant qui affiche un montant € (injecte `IPrivacyModeService`). Rappeler avec `isHidden: true` dans un test dédié pour vérifier le masquage.
- `DashboardViewModel` — instancier avec `Mock<IPortfolioService>` + `Mock<ILocalizationService>` (setup `Translate(key) → key`)
- `TrackingViewModel` — instancier avec `Mock<IPortfolioService>` + `Mock<ILocalizationService>`
- Les tests de composants héritent de `BunitContext` et appellent `Services.AddMudServices(...)` + `Services.AddLocalizationMock()`
- `IKeyValueStore` : les tests de services (`SessionService`, `PrivacyModeService`) injectent `Mock<IKeyValueStore>` ; `LocalStorageKeyValueStore` est testé avec `Mock<IJSRuntime>`
- `ServiceCollectionExtensionsTests` vérifie que `AddInvestissementsClient` permet de résoudre services et ViewModels
- Lancer les tests : `dotnet test "Client.Tests/InvestissementsDashboard.Client.Tests.csproj"` — non exécutés par la CI actuelle (seul `Api.Tests` l'est)

---

## 10. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
