# Maui — Hôte application Windows (BlazorWebView)

## 1. Rôle

Application **MAUI Windows**, strictement personnelle (PC du propriétaire), qui affiche le dashboard dans un `BlazorWebView`. Elle réutilise **tout** le code Blazor de la bibliothèque `Client.Shared/` (voir `Client.Shared/Docs/CLAUDE.md`) et appelle la même Api que le site. Elle ne contient aucun composant ni logique métier : uniquement l'hôte (DI, stockage sécurisé, page d'hébergement, icône).

Elle exécute les agents IA **Stock**, **Actualités** et **Allocation** (CLAUDE.md racine §14) dans ce processus, sans la limite de 45 s du proxy SWA : `Services/AgentRunner.cs` implémente `IAgentRunner` (seam de `Client.Shared`, §7.10 de sa doc) et s'appuie sur la bibliothèque `AgentAI.Core` (`AgentAI.Core/Docs/CLAUDE.md`). Les agents Chat et Météo existent dans la bibliothèque mais ne sont pas exposés dans l'interface.

Diagramme de classes : `maui-class-diagram.drawio` / `.png` (ce dossier).

## 2. Stack

| Élément | Choix |
|---|---|
| SDK / cible | `Microsoft.NET.Sdk.Razor`, `net10.0-windows10.0.19041.0` (Windows uniquement), `UseMaui`, `SingleProject` |
| Paquets | `Microsoft.Maui.Controls`, `Microsoft.AspNetCore.Components.WebView.Maui` (versions `$(MauiVersion)`), `Microsoft.Extensions.Logging.Debug` |
| Packaging | **Non packagé** par défaut (`WindowsPackageType=None`) pour `dotnet run` et les tests ; **MSIX signé** pour l'installation sur le poste (voir §7). Le comportement d'un MSIX avec le futur flux d'identité Azure (`az login`) reste à valider |
| Références | `Client.Shared`, `AgentAI.Core` (agents IA) |
| Tests | `Maui.Tests/` (xUnit + Moq) |

## 3. Structure

```
Maui/
├── InvestissementsDashboard.Maui.csproj
├── App.xaml / App.xaml.cs      # fenêtre unique « Suivi des Investissements »
├── MainPage.xaml(.cs)          # BlazorWebView : #app → App (Client.Shared), head::after → HeadOutlet
├── MauiProgram.cs              # DI et configuration (voir §4)
├── Services/SecureStorageKeyValueStore.cs   # IKeyValueStore → ISecureStorage (chiffré par l'OS)
├── Services/AgentRunner.cs                  # IAgentRunner → AgentAI.Core : une AgentFactory par lancement (voir §5bis)
├── Services/PortfolioReviewRunner.cs        # IPortfolioReviewRunner → workflow PortfolioReview d'AgentAI.Core (voir §5ter)
├── Services/AssetDistributionSource.cs      # IAssetDistributionSource → IPortfolioService (répartitions géographique et sectorielle via l'Api)
├── Services/PreferencesAgentSettings.cs     # IAgentSettings → IPreferences (page Paramètres)
├── Services/AgentOptionsProvider.cs         # IAgentSettings → AgentAIOptions (MCP déduit de l'URL de base stockée, repli sur l'Api ; historique injecté)
├── Services/McpApiKeyHandler.cs             # DelegatingHandler du seul client MCP : ajoute x-mcp-api-key lue dans IKeyValueStore
├── Services/FileLoggerProvider.cs           # ILoggerProvider fichier (logs/maui.log dans AppDataDirectory, 1 Mo max)
├── Platforms/Windows/          # App.xaml(.cs), app.manifest (boilerplate WinUI), Package.appxmanifest (identité MSIX)
├── Scripts/                    # Update-App.ps1 (tout-en-un), New-DevCertificate.ps1, Publish-Msix.ps1, Install-Msix.ps1 (packaging, voir §7)
├── Resources/AppIcon/appicon.svg, Resources/Splash/splash.svg   # même visuel que le favicon du site
├── wwwroot/index.html          # page d'hébergement du BlazorWebView
└── Docs/                       # CLAUDE.md, SPECS.md, maui-class-diagram.drawio/.png

Maui.Tests/
├── SecureStorageKeyValueStoreTests.cs
├── PreferencesAgentSettingsTests.cs
├── AgentOptionsProviderTests.cs
├── AgentRunnerTests.cs          # AgentFactory/agent mockés (Moq.Protected sur AIAgent)
├── PortfolioReviewRunnerTests.cs # builder mocké renvoyant un workflow d'une étape de test : sortie, fabrique et source du lancement transmises, échec, 401 MCP, délai, annulation
├── PortfolioReviewWorkflowBuilderTests.cs # vrai builder, AgentFactory/agent/source mockés : agent Allocation demandé, synthèse produite après l'allocation et les répartitions, échec d'une étape sans synthèse, arguments null, annulation
├── SynthesisExecutorTests.cs     # barrière fan-in réelle, trois sources de test : un seul rapport, attend l'entrée la plus lente
├── ContextExecutorTests.cs       # squelette : contexte à la date demandée
├── AssetDistributionExecutorTests.cs # source mockée : Stocks puis Bonds, chaque classe avec ses répartitions, jeton transmis, événement émis, erreur propagée
├── AssetDistributionSourceTests.cs   # IPortfolioService mocké : géographie et secteurs relayés, erreur propagée
├── AllocationExecutorTests.cs   # étape d'allocation du workflow : désérialisation JSON, événement émis
├── AgentInstructionsTests.cs    # la règle « tour unique » est présente dans les instructions embarquées
├── McpApiKeyHandlerTests.cs     # en-tête x-mcp-api-key présent / absent / non écrasé
├── FileLoggerProviderTests.cs   # écriture, plafond de taille, échec d'écriture sans exception
├── NewsServiceRegistrationTests.cs # bout en bout de AddNewsServices (conteneur réel, filtre de titres Google News, Yahoo via ISIN) avec HttpMessageHandler factice
└── News/                        # tests unitaires : NewsServiceTests (agrégation), GoogleNews/YahooFinanceFeedSource, YahooTickerResolver, RssFeedParser, CompanyTitleFilter
```

## 4. `MauiProgram.cs`

1. `AddMauiBlazorWebView()` (+ `AddBlazorWebViewDeveloperTools()` et log debug en `DEBUG`).
2. `ISecureStorage` → `SecureStorage.Default`, `IKeyValueStore` → `SecureStorageKeyValueStore`, `IAgentSettings` → `PreferencesAgentSettings` (affiche la page Paramètres ; au premier lancement, `https://invest.zapto.fr/` — `ApiBaseUri` — est enregistrée dans les préférences comme URL de base du MCP, sans champ de saisie).
3. `AddAgents` (méthode privée de `MauiProgram`) : `AgentOptionsProvider` (historique dans `FileSystem.AppDataDirectory/agents-history`, dossier inscriptible même packagé), `AddNewsServices()` et `AddInvestZaptoMcp()` (clients typés `IHttpClientFactory` : le mot de passe du dashboard, ajouté aux seuls clients du dashboard, ne part pas vers les flux RSS ni le MCP ; `ForceContentLengthHandler` est fourni par la bibliothèque ; `McpApiKeyHandler` est ajouté **à ce seul client** pour envoyer la clé du serveur MCP), avec un `SocketsHttpHandler` à `PooledConnectionLifetime` de 2 min comme handler primaire de ces trois clients, puis `IAgentRunner` → `AgentRunner` et `IPortfolioReviewRunner` → `PortfolioReviewRunner` (même fabrique `AgentFactory`, plus `IPortfolioReviewWorkflowBuilder` → `PortfolioReviewWorkflowBuilder` en singleton et `IAssetDistributionSource` → `AssetDistributionSource` en transient, comme le client typé `IPortfolioService` qu'elle enveloppe ; le runner en demande une par lancement). `AddAgentAI` n'est **pas** utilisé (il enregistre la factory en singleton).
4. Journal fichier (méthode privée `AddLogger`, via `services.AddLogging`) : `FileLoggerProvider` → `FileSystem.AppDataDirectory/logs/maui.log` (filtres `Microsoft` et `System` à Warning). Une application fenêtrée n'a pas de console : c'est le seul moyen de voir à quelle étape un agent s'est bloqué. Le fichier est vidé au démarrage s'il dépasse 1 Mo ; une erreur d'écriture n'est jamais propagée.
4. `AddInvestissementsClient(ApiBaseUri)` — toute la DI du dashboard vient de `Client.Shared`.
**URL de l'Api** : `https://invest.zapto.fr/`, fixée dans `MauiProgram.ApiBaseUri` (l'Api n'est joignable que via le proxy SWA). Aucune surcharge par variable d'environnement : pour développer contre une Api locale, modifier la constante.

## 5. `SecureStorageKeyValueStore`

Stocke le mot de passe de session et le mode confidentialité chiffrés par l'OS (au lieu du clair du `localStorage` WebView2). `GetAsync` traite une entrée illisible (profil déplacé, clé de chiffrement perdue) comme absente : elle est journalisée (avertissement) puis supprimée, pour que l'utilisateur retombe sur l'écran de connexion au lieu d'être bloqué. `SetAsync` laisse remonter les erreurs.

## 5bis. `AgentRunner` — exécution des agents

`IAgentRunner.RunAsync(AgentChoice, assetName, isin, ct)` : lit les options via `AgentOptionsProvider.Create()` (lève une `InvalidOperationException` si l'endpoint Foundry n'est pas renseigné : l'erreur s'affiche au lancement, pas au démarrage), crée une `AgentFactory` **à chaque lancement** (une modification de la page Paramètres est prise en compte sans redémarrage) via un `Func<AgentAIOptions, IAgentFactory>` injecté (testable sans Foundry), puis `CreateAsync(AgentKind, assetName, isin)` → `CreateSessionAsync` → `RunAsync(message déclencheur)`. `AgentChoice.Stock` → `AgentKind.Stock`, `News` → `AgentKind.News`, `Allocation` → `AgentKind.Allocation` (agent global : `assetName` est `null`, message « Dresse l'état complet de mon portefeuille… ») ; le message déclencheur est le même que celui de la console `AgentAI`. En cas d'échec (hors annulation) l'historique de l'agent est réinitialisé avec sauvegarde (`ResetWithBackup`) puis l'exception est relancée ; une annulation (fermeture du dialogue) laisse l'historique intact. La factory est libérée (`await using`) : elle ferme la connexion MCP éventuelle.

**Délai maximal et diagnostic.** Un `CancellationTokenSource` lié borne le lancement à `DefaultTimeout` (3 min, injectable pour les tests) ; `AgentFactory.CreateAsync` ne prenant pas de jeton, l'attente passe par `WaitAsync(token)`. Un dépassement lève une `TimeoutException` qui nomme l'étape (« creating the agent (MCP connection) » ou « waiting for the model's answer ») et laisse l'historique réinitialisé comme toute autre erreur ; une annulation de l'utilisateur est relancée telle quelle, historique conservé. Un `401` à la connexion MCP devient une `InvalidOperationException` « The MCP key is missing or rejected: set it in the settings page. » **sans** réinitialiser l'historique (aucun appel au modèle n'a eu lieu). Le lancement, la création de l'agent et la réponse sont journalisés (niveau Information) ; `AgentFactory` ajoute une ligne par appel au modèle (durée, jetons entrée/sortie/raisonnement).

**Clé du serveur MCP.** Dès que `MCP_API_KEY` est configurée côté Api, `/api/mcp` exige l'en-tête `x-mcp-api-key`. La clé est saisie dans la page Paramètres et stockée via `IKeyValueStore` (`SecureStorage`, jamais `Preferences`) sous `AgentSecretKeys.McpApiKey` ; `McpApiKeyHandler` la relit à chaque requête MCP (une nouvelle clé est donc prise en compte sans redémarrage) et n'ajoute rien si elle est absente ou si l'en-tête existe déjà.

Les agents répondent en **un seul tour** (pas de boucle conversationnelle) : leurs instructions embarquées contiennent la règle « échange à tour unique » (pas de question finale).

## 5ter. `PortfolioReviewRunner` — exécution du workflow d'analyse

`IPortfolioReviewRunner.RunAsync(date, ct)` : comme `AgentRunner`, lit les options, crée une `AgentFactory` **par lancement** (libérée par `await using`) et borne l'exécution à `DefaultTimeout` (3 min). Il fait construire le workflow par l'`IPortfolioReviewWorkflowBuilder` injecté (`BuildAsync(agentFactory, distributionSource, token)` : le jeton interrompt la connexion MCP ; la source vient d'un `Func<IAssetDistributionSource>` injecté, appelé à chaque lancement pour que ce singleton ne retienne pas le client HTTP du dashboard), le lance en processus (`InProcessExecution.RunStreamingAsync` avec `AnalysisRequest(date)`) et parcourt `WatchStreamAsync` : `AllocationRetrievedEvent`, `AssetDistributionRetrievedEvent` et `SynthesisWrittenEvent` sont journalisés, le premier `WorkflowOutputEvent` portant un `SynthesisReport` (seule sortie du workflow) marque la fin — rien n'est renvoyé tant que la synthèse est un squelette —, `ExecutorFailedEvent`/`WorkflowErrorEvent` deviennent une `InvalidOperationException` (le framework signale l'échec d'une étape par un événement, pas par une exception). Un `401` MCP donne le même message qu'`AgentRunner` ; un dépassement de délai, une `TimeoutException`.

**Répartitions (`AssetDistributionSource`).** L'étape `AssetDistributionExecutor` d'AgentAI.Core lit les répartitions géographique et sectorielle via `IAssetDistributionSource` ; son implémentation MAUI relaie `IPortfolioService.GetGeographyDistributionAsync`/`GetSectorDistributionAsync` (Client.Shared), donc le même client que le dashboard, qui ajoute déjà le mot de passe (`DashboardPasswordHandler`). Une erreur de l'Api (dont un `401` sur mauvais mot de passe) fait échouer cette étape (`ExecutorFailedEvent`) et donc l'analyse.

**Piège — annulation silencieuse.** Un run annulé termine le flux d'événements **sans lever d'exception** : après la boucle, `token.ThrowIfCancellationRequested()` transforme ce cas en annulation (ou en délai dépassé) au lieu d'un faux « workflow sans sortie ». Dans les tests, un faux agent doit honorer son jeton (`Task.Delay(Infinite, ct)`) : un délai qui l'ignore garde la tâche de fond du workflow en vie et fait planter l'hôte de test.

## 6. `wwwroot/index.html`

Charge, dans l'ordre : `_content/MudBlazor/MudBlazor.min.css`, `_content/InvestissementsDashboard.Client.Shared/css/app.css`, `InvestissementsDashboard.Maui.styles.css` (qui importe le CSS isolé de la RCL), puis `_framework/blazor.webview.js` (`autostart="false"`) et `_content/MudBlazor/MudBlazor.min.js`. Pas d'`importmap`, pas de favicon (`data:,`).

## 7. Exécution, build et tests

- Les projets MAUI **ne figurent pas dans `Investissements.slnx`** (restauration impossible sur le runner Ubuntu de la CI). Ils sont dans **`Investissements.Maui.slnx`** (Client.Shared, Maui, Maui.Tests, Shared) — Windows uniquement, workload `maui-windows` requis.
- Build : `dotnet build Investissements.Maui.slnx`
- Lancer : `dotnet run --project Maui -f net10.0-windows10.0.19041.0` (ou exécuter `Maui/bin/Debug/net10.0-windows10.0.19041.0/win-x64/InvestissementsDashboard.Maui.exe`)
- Tests : `dotnet test "Maui.Tests/InvestissementsDashboard.Maui.Tests.csproj"` (148 tests). `Maui.Tests` cible aussi `net10.0-windows…` avec `UseMaui` (pour `ISecureStorage`) et **lie** les fichiers `SecureStorageKeyValueStore.cs`, `PreferencesAgentSettings.cs`, `AgentOptionsProvider.cs`, `AgentRunner.cs`, `PortfolioReviewRunner.cs`, `AssetDistributionSource.cs`, `McpApiKeyHandler.cs` et `FileLoggerProvider.cs` (et référence `AgentAI.Core`) au lieu de référencer le projet (une application `Exe` MAUI ne se référence pas depuis un projet de test).
- La CI ne construit ni ne teste ce projet.
- Si l'icône de l'application ne change pas après modification de `appicon.svg`, supprimer `Maui/obj` (cache de génération des icônes).

### Installer l'application (MSIX signé, auto-signé)

**En une commande** (PowerShell normal, depuis la racine du dépôt) : `.\Maui\Scripts\Update-App.ps1`. Le script crée le certificat s'il n'existe pas, construit et signe le MSIX, n'élève les droits **que** pour approuver le certificat (une seule invite UAC, la première fois ou après un renouvellement), puis installe ou met à jour l'application en fermant l'instance en cours. Si la version est déjà installée (même numéro de build), il s'arrête : committer pour obtenir une nouvelle version, ou `-Reinstall` pour réinstaller la même (**efface les données de l'application**, dont le mot de passe du dashboard mémorisé). `-WhatIf` déroule les étapes sans rien modifier.

Les trois étapes ci-dessous restent utilisables séparément (diagnostic, ou installation depuis un PowerShell administrateur) :

À faire dans l'ordre, depuis la racine du dépôt :

1. **Une seule fois** — créer le certificat de signature (PowerShell normal) : `.\Maui\Scripts\New-DevCertificate.ps1`. Le certificat vit dans le magasin utilisateur (`CN=Mickael Billet`, 3 ans) ; la signature se fait par empreinte, donc **aucun `.pfx` ni mot de passe** n'existe sur disque ou dans le dépôt. Seul le `.cer` public est exporté dans `%USERPROFILE%\.certs\`. Le `Publisher` du `Package.appxmanifest` doit rester égal au sujet du certificat.
2. **À chaque version** — construire le package : `.\Maui\Scripts\Publish-Msix.ps1` (le numéro de build `ApplicationVersion` est le nombre de commits du dépôt : Windows refuse d'installer un contenu différent sous la même version `1.0.0.N`, erreur `0x80073CFB` ; si l'on republie le même commit avec des modifications locales, désinstaller d'abord l'ancien package) → `Maui/artifacts/…/InvestissementsDashboard.Maui_<version>_x64.msix` (dossier ignoré par git).
3. **Dans un PowerShell administrateur** — `.\Maui\Scripts\Install-Msix.ps1` : fait confiance au `.cer` (`LocalMachine\TrustedPeople`, nécessite l'élévation) puis installe le `.msix` avec `Add-AppxPackage -ForceUpdateFromAnyVersion` (une mise à jour remplace la version installée en conservant ses données, à condition que le numéro de version ait augmenté).
4. L'application apparaît dans le menu Démarrer (« Suivi des Investissements ») avec l'icône du site. Désinstallation : Paramètres → Applications, ou `Get-AppxPackage fr.zapto.invest.maui | Remove-AppxPackage`.

Une application packagée est isolée : les données de `SecureStorage` et du profil WebView2 sont propres au package (une désinstallation les efface, le mot de passe est à ressaisir) et les variables d'environnement d'un terminal ne sont pas visibles.

## 8. Limites connues / à valider

- Le rendu de MudBlazor et d'ApexCharts dans le `BlazorWebView` n'est pas vérifié visuellement (l'application démarre et crée sa fenêtre).
- Le lien « Documentation » (`ClientOptions.DocumentationUri`, URL absolue du site) doit s'ouvrir dans le navigateur — non testé.
- Le certificat auto-signé expire au bout de 3 ans : en recréer un (`New-DevCertificate.ps1`), republier et réinstaller. Changer son sujet impose de changer le `Publisher` du manifeste, et donc de désinstaller l'ancien package.
- L'installation du MSIX n'est pas automatisée en CI (poste du propriétaire uniquement).
- La page Paramètres édite l'endpoint Foundry, le modèle et la clé du serveur MCP : l'URL de l'Api reste fixe (`MauiProgram.ApiBaseUri`). La clé n'est jamais relue ni affichée (champ vide à chaque ouverture ; un champ laissé vide conserve la clé enregistrée).
- Les agents utilisent l'identité Azure du poste (`DefaultAzureCredential`, `az login`) : non vérifié avec un MSIX packagé (voir CLAUDE.md racine §14.5). Ils consomment des tokens Foundry facturés à l'usage.
- Agents Chat et Météo non exposés dans l'interface (l'ancien agent Portefeuille a été supprimé : l'agent Allocation couvre la lecture du portefeuille).
- Agent Allocation : validé en application **non packagée** (`dotnet run`, ≈ 52 s de bout en bout avec un effort de raisonnement `Low`) ; non testé en MSIX (identité Azure, `SecureStorage` du package). La durée croît avec le nombre de positions.
- Le message `Invalid JSON-RPC message format` journalisé à chaque connexion MCP vient de la réponse de l'Api à `notifications/initialized` ; il disparaît une fois l'Api corrigée (réponse `202` sans corps) déployée.

## 9. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
