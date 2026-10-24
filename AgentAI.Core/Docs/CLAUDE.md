# AgentAI.Core — Bibliothèque des agents IA

## 1. Rôle

Bibliothèque technique (sans interface) qui construit et fait tourner les agents IA du projet : Microsoft Agent Framework + Azure AI Foundry (`AIProjectClient`). Elle est consommée par l'application Windows `Maui/` (voir `Maui/Docs/CLAUDE.md` §5bis) et, en dehors de cette solution, par la console de développement `C:\Users\mbillet.NOVACATH\source\AgentAI`, dont elle est issue (CLAUDE.md racine §14).

Aucun agent ne tourne dans le site WASM ni dans l'Api (limite de 45 s du proxy SWA, pas d'identité Entra côté navigateur — CLAUDE.md racine §14.1). Comme `GoogleSheets` et `Shared`, c'est une bibliothèque technique : pas de `SPECS.md` (aucune fonctionnalité utilisateur propre — les fonctionnalités sont dans `Maui/Docs/SPECS.md`).

Diagramme de classes : `agentai-class-diagram.drawio` / `.png` (ce dossier).

## 2. Stack

| Élément | Choix |
|---|---|
| Cible | `net10.0` (`Microsoft.NET.Sdk`), `ImplicitUsings` et `Nullable` activés |
| Paquets | `Microsoft.Agents.AI.Foundry`, `Azure.AI.Projects` (préversion), `Azure.Identity`, `Microsoft.Extensions.AI`, `ModelContextProtocol`, abstractions DI et logging |
| Identité Azure | `DefaultAzureCredential` avec `ExcludeManagedIdentityCredential = true` (`az login` sur le poste). Jamais de secret de principal de service dans l'application |
| Solution | `Investissements.Maui.slnx` uniquement (pas dans `Investissements.slnx`, restauré par la CI Ubuntu) |
| Tests | Pas de projet dédié : `AgentRunner`, le service d'actualités (`NewsService`, sources, parseur, résolveur de ticker), l'outil `SearchWeb`, le workflow et les instructions sont testés dans `Maui.Tests/` |

## 3. Structure

```
AgentAI.Core/
├── AgentAI.Core.csproj
├── Agents/
│   ├── IAgentFactory.cs        # CreateAsync(AgentKind, contextInput, optionalInput) → (AIAgent, ICustomChatHistoryProvider) ; optionalInput = ISIN (Stock et News)
│   ├── AgentFactory.cs         # implémentation : un AIAgent par AgentKind, historique client (StoredOutputEnabled = false), effort de raisonnement, trace par appel au modèle
│   ├── AgentKind.cs            # Chat, Weather, Stock, Allocation, News, Context
│   ├── AgentAIOptions.cs       # configuration fournie par l'hôte (voir §4)
│   ├── AgentDefinition.cs      # record interne : instructions, outils, fichier d'historique, fournisseurs de contexte, effort de raisonnement facultatif
│   ├── InstructionLoader.cs    # charge les instructions embarquées
│   └── WebSearchTool.cs        # outil SearchWeb (IWebSearchService) plafonné à 15 recherches par agent
├── Instructions/               # AnalyseAction.md, ActualitesSociete.md, AllocationPortefeuille.md, Context.md, Synthesis.md (ressources embarquées ; Synthesis.md pas encore branché)
├── Workflows/PortfolioReview/  # workflow d'analyse du portefeuille (Microsoft.Agents.AI.Workflows)
│   ├── IPortfolioReviewWorkflowBuilder.cs  # BuildAsync(IAgentFactory, IAssetDistributionSource, ct) → Workflow
│   ├── PortfolioReviewWorkflowBuilder.cs   # implémentation sans état (singleton côté hôte)
│   ├── Executors/              # StartExecutor (transit), AllocationExecutor (→ AllocationResult), ContextExecutor (agent Context → PortfolioContext), AssetDistributionExecutor (→ DistributionResult), SynthesisExecutor (→ SynthesisReport, squelette)
│   ├── Messages/               # AnalysisRequest, AllocationResult (+ AllocationLine), PortfolioContext (+ ContextFactor, ContextSource, ContextIndicator, enums FactorType/FactorHorizon/ChangeUnit), DistributionResult (+ AssetClassDistribution), SynthesisReport
│   └── Events/                 # AllocationRetrievedEvent, ContextRetrievedEvent, AssetDistributionRetrievedEvent, SynthesisWrittenEvent
├── Providers/                  # AIContextProvider : Composite, Date, News, Stock, Weather
├── History/                    # ICustomChatHistoryProvider, CustomChatHistoryProvider (historique JSON sur disque)
├── Mcp/                        # InvestZaptoMcpClient
├── Http/                       # ForceContentLengthHandler (contournement HTTP générique : Content-Length explicite)
├── Services/
│   ├── News/                   # INewsService, NewsService
│   │   ├── Models/             # NewsArticle, NewsRequest
│   │   ├── Sources/            # INewsFeedSource
│   │   │   ├── Google/         # GoogleNewsFeedSource
│   │   │   └── Yahoo/          # YahooFinanceFeedSource, YahooTickerResolver (ISIN → ticker)
│   │   └── Helpers/            # IRssFeedReader/RssFeedReader, RssFeedParser, CompanyTitleFilter
│   ├── Weather/                # IWeatherService, WeatherService
│   ├── AssetDistribution/      # IAssetDistributionSource (contrat seul : répartitions géographique et sectorielle, implémenté par l'hôte)
│   └── WebSearch/              # IWebSearchService (contrat seul : recherche web, implémenté par l'hôte via l'Api ; DTOs dans Shared/Models/WebSearchDto.cs)
├── Hosting/ServiceCollectionExtensions.cs   # AddAgentAI(options) / AddAgentAI(optionsFactory)
├── Hosting/NewsServiceExtensions.cs            # AddNewsServices() : INewsService + sources + clients typés (flux RSS, résolveur Yahoo)
├── Hosting/InvestZaptoMcpServiceExtensions.cs  # AddInvestZaptoMcp() : InvestZaptoMcpClient en client typé + ForceContentLengthHandler
└── Docs/                       # ce dossier : CLAUDE.md, diagramme de classes
```

## 4. Configuration (`AgentAIOptions`)

La bibliothèque **ne lit jamais les variables d'environnement** : un hôte MAUI n'a pas d'environnement de processus significatif, chaque hôte décide d'où viennent les valeurs.

| Propriété | Rôle |
|---|---|
| `FoundryProjectEndpoint` (requis) | endpoint du projet Azure AI Foundry |
| `Model` | nom du déploiement du modèle, `gpt-5-mini` par défaut (`DefaultModel`) |
| `InvestZaptoMcpUrl` | URL du MCP InvestZapto — requise par l'agent Allocation |
| `HistoryDirectory` (requis) | dossier **inscriptible** des historiques de conversation (le dossier de l'exécutable est en lecture seule pour une application packagée) |

`Maui` les construit via `AgentOptionsProvider` à partir de la page Paramètres.

## 5. Règles et pièges

- **Instructions embarquées** (`EmbeddedResource`, `LogicalName = Instructions.<fichier>`) et non copiées à côté de l'exécutable : un hôte MAUI n'a pas de dossier de base prévisible. Toute modification d'un `.md` impose une recompilation.
- **Échange à tour unique** : `ActualitesSociete.md`, `AnalyseAction.md`, `AllocationPortefeuille.md` et `Context.md` contiennent la règle « pas de question finale, pas de suite proposée » (l'hôte ne propose pas de boucle conversationnelle). La conserver dans tout nouvel agent destiné à Maui. Un test (`Maui.Tests/AgentInstructionsTests.cs`) vérifie sa présence.
- **Clients HTTP = clients typés d'`IHttpClientFactory`** (`InvestZaptoMcpClient`, `RssFeedReader`, `YahooTickerResolver`), enregistrés par la bibliothèque (`AddInvestZaptoMcp`, `AddNewsServices`) : plus de `new HttpClient()`. Chaque client a son propre pipeline, donc le mot de passe du dashboard (ajouté aux clients du dashboard) ne part jamais vers les flux RSS ni le MCP. L'hôte choisit le handler **primaire** via `AddHttpClient<T>().ConfigurePrimaryHttpMessageHandler(...)` (un hôte navigateur n'a pas `SocketsHttpHandler`). Le backend InvestZapto échoue en `500 Backend call failure` sur les POST en `Transfer-Encoding: chunked` : `ForceContentLengthHandler` (neutre vis-à-vis de la plateforme) est ajouté par `AddInvestZaptoMcp` et bufferise le corps pour envoyer un `Content-Length`. Ces clients étant capturés par des singletons (`NewsService`, `AgentFactory`), l'hôte borne `PooledConnectionLifetime` pour garder le rafraîchissement DNS. `AddHttpClient<T>` n'est pas un `TryAdd` : substituer `InvestZaptoMcpClient` s'enregistre **après** `AddAgentAI`.
- **Actualités — architecture :** `NewsService` (façade `INewsService`) interroge en parallèle toutes les `INewsFeedSource` et fusionne : fenêtre de dates, dédoublonnage par URL puis par titre normalisé, tri décroissant, plafond de 30. Contrat d'une source : liste = résultat (vide = « pas d'actualité »), `null` = source non applicable (ignorée, ex. pas d'ISIN), exception = « pas de réponse » (loguée, les autres sources continuent ; si **toutes** les sources applicables échouent, `HttpRequestException`). **Ajouter une source** = implémenter `INewsFeedSource` et l'enregistrer dans le conteneur (`services.AddSingleton<INewsFeedSource, MaSource>()`, depuis l'hôte si besoin) ; `NewsService` reçoit toutes les sources enregistrées et aucune autre classe ne change. `AddNewsServices()` (`Hosting/NewsServiceExtensions.cs`) enregistre `NewsService` et les sources intégrées en `TryAdd`, plus `RssFeedReader` et `YahooTickerResolver` comme clients typés.
- **Actualités — Google News :** `GoogleNewsFeedSource` (via `CompanyTitleFilter`) filtre les résultats **Google News** sur le titre (le titre doit contenir le nom de la société, sans tenir compte de la casse, des accents ni des suffixes juridiques `NV`/`SA`/`SE`…, par mots entiers). Raison : Google cherche la phrase dans tout le texte de l'article, et l'agent ne voit que les titres — il signalait sinon des articles « ambigus ». Le flux Yahoo (ciblé par ticker) n'est pas filtré. Limite : un titre pertinent qui ne nomme pas la société est écarté.
  - **ISIN → ticker Yahoo :** le flux `feeds.finance.yahoo.com/rss/2.0/headline?s=` n'accepte qu'un ticker (un ISIN renvoie un flux vide sans erreur). `YahooTickerResolver` résout donc l'ISIN via `query1.finance.yahoo.com/v1/finance/search?q=<ISIN>` (premier résultat `EQUITY`/`ETF`, sinon premier résultat), puis interroge le flux. ISIN absent, mal formé, introuvable ou recherche en échec : Yahoo est ignoré (warning) et Google News répond seul. Endpoint non documenté, comme le flux : peut changer sans préavis.
- **MCP** : seul le transport Streamable HTTP est géré par le serveur ; `InvestZaptoMcpClient` le force (sinon repli sur un SSE hérité refusé en 404) et réessaie 3 fois sur `HttpRequestException`, **sauf sur un `401`** (clé absente ou refusée : réessayer n'y changerait rien ; l'hôte le traduit en message). La bibliothèque n'envoie aucune clé : l'hôte ajoute `x-mcp-api-key` par un `DelegatingHandler` sur le seul client MCP.
- **Agent de portefeuille (`Allocation`)** : il s'appuie sur `BuildMcpDefinitionAsync(instructions, historique, withDate, reasoningEffort)` (connexion MCP, liste des outils, date du jour facultative). `Allocation` (`AllocationPortefeuille.md`) lit `get_snapshot` puis `get_assets` et rédige un rapport descriptif ; `null` côté Api (données `"ND"`) s'écrit « non renseigné ». Historique : `chat-history-allocation.json`.
- **Workflow `PortfolioReview`** : `IPortfolioReviewWorkflowBuilder.BuildAsync(IAgentFactory, IAssetDistributionSource, ct)` (implémentation `PortfolioReviewWorkflowBuilder`, injectée, **sans état** donc singleton) assemble les étapes. La fabrique et la source de répartitions sont des **paramètres** et non des dépendances du constructeur : l'hôte en crée une par lancement (la fabrique est libérée après ; la source enveloppe un client HTTP typé que le singleton ne doit pas retenir) ; `CreateAsync` ne prenant pas de jeton, le builder l'attend via `WaitAsync(ct)`. Pas de classe statique : l'hôte teste son runner avec un builder mocké, indépendamment de l'assemblage. Graphe : `StartExecutor` (transit de l'`AnalysisRequest` : un workflow n'a qu'un point d'entrée et le fan-out part d'un executor) → **fan-out** (`AddFanOutEdge`) vers `AllocationExecutor`, `ContextExecutor` et `AssetDistributionExecutor`, exécutés **en parallèle** → **barrière fan-in** (`AddFanInBarrierEdge`) → `SynthesisExecutor`, seule sortie du workflow (`WithOutputFrom`). La barrière retient les résultats jusqu'à ce que **chaque** source en ait produit un, puis les livre à la synthèse au pas suivant, **comme trois messages distincts** (ordre non garanti) : `SynthesisExecutor` dérive donc d'`Executor` et déclare trois handlers dans `ConfigureProtocol` (`AllocationResult`, `PortfolioContext`, `DistributionResult`) plus `YieldsOutput<SynthesisReport>()` ; chaque handler range son entrée dans l'**état du workflow** (`QueueStateUpdateAsync`/`ReadStateAsync`, plutôt que des champs, pour rester compatible avec les checkpoints) et le rapport n'est écrit (`SynthesisWrittenEvent` puis `YieldOutputAsync`) qu'une fois les trois présentes. `SynthesisExecutor` (rapport vide) est encore un **squelette**. `ContextExecutor` fait établir par l'agent `Context` (`Context.md`, outil `SearchWeb`) l'état des lieux macro-économique et géopolitique à la date demandée, **sans connaître le portefeuille** : `RunAsync<PortfolioContext>` (situation, 8–12 facteurs classés avec les libellés du référentiel de l'application, 7 indicateurs avec leur variation sur 3 mois), puis impose la date de la requête (`with { Date = … }`) et publie `ContextRetrievedEvent`. Les dates des sources et des indicateurs sont du **texte** (une donnée mensuelle n'a pas de jour : un `DateOnly` ferait échouer toute l'étape) ; les valeurs introuvables sont `null`, jamais 0 ; les enums circulent en noms (`JsonStringEnumConverter<T>`) dans le schéma JSON envoyé au modèle. Une étape en échec (`ExecutorFailedEvent`) bloque la barrière : aucune synthèse n'est produite. `AllocationExecutor` réutilise l'agent `Allocation` (mêmes instructions `AllocationPortefeuille.md` que l'entrée du menu « ⋮ ») et appelle `RunAsync<AllocationResult>` : schéma JSON envoyé au modèle et désérialisation en un appel. Le message utilisateur ne porte que la date demandée (`AnalysisRequest.Date`). Il publie `AllocationRetrievedEvent` (observation : journal, progression) ; les données passent à l'étape suivante par sa **valeur de retour**. `AssetDistributionExecutor` n'utilise **aucun agent** : il lit, via `IAssetDistributionSource`, les répartitions géographique et sectorielle de `Stocks` et `Bonds` (quatre appels lancés en parallèle) — mêmes chiffres que le dashboard, calculés par l'Api (`/api/portfolio/geography|sector/{assetClass}`) — renvoie un `DistributionResult` (une `AssetClassDistribution` par classe, dans l'ordre Stocks puis Bonds) et publie `AssetDistributionRetrievedEvent`. La bibliothèque ne déclare que le contrat : l'appel à l'Api (et son mot de passe du dashboard) appartient à l'hôte. Exécuté par l'hôte (`Maui/Services/PortfolioReviewRunner.cs`). Ajouter une étape = un executor + ses arêtes dans `PortfolioReviewWorkflowBuilder.BuildAsync` ; une nouvelle entrée de la synthèse = une source de plus dans la barrière et un handler de plus dans `SynthesisExecutor`.
- **Recherche web (`SearchWeb`)** : `IWebSearchService` n'est qu'un **contrat** (comme `IAssetDistributionSource`) : la clé du fournisseur (Tavily) reste côté serveur, dans l'App Setting `WEB_SEARCH_API_KEY` de l'Api, qui fait la recherche (`POST /api/websearch`, voir `Api/Docs/CLAUDE.md` §10bis). L'hôte fournit l'implémentation (MAUI : `ApiWebSearchService`) et l'enregistre ; `AddAgentAI` la résout sans l'enregistrer. `AgentFactory` crée un `WebSearchTool` **par agent** : au-delà de 15 recherches (`MaxWebSearches`), l'outil renvoie un message au modèle au lieu de chercher — protège le quota gratuit (1 000 recherches/mois) d'un modèle qui boucle. Paramètres exposés au modèle : `query`, `topic` (`General`/`News`/`Finance`), `timeRange` (`Any`/`Day`/`Week`/`Month`/`Year`). Une erreur de recherche (Api ou fournisseur) est rendue au modèle par le framework, pas levée : l'étape aboutit avec des valeurs « non disponibles », visibles seulement dans le journal (warning d'`ApiWebSearchService`). Agent `Context` : pas de `DateContextProvider` (sa règle « ancienne au-delà de 6 mois » contredit les 30 jours de `Context.md`), `ReasoningEffort = Low`, `KeepHistory = false` (`chat-history-context.json`).
- **Piège — effort de raisonnement et délai réseau** : à l'effort par défaut, l'appel qui rédige le rapport d'allocation a duré 158 s (≈ 14 000 jetons de raisonnement), au-delà des 100 s de `NetworkTimeout` du client Foundry, qui relançait la requête : l'application attendait sans fin. `AgentDefinition.ReasoningEffort = Low` (porté par `CreateResponseOptions.ReasoningOptions`) ramène cet appel à ≈ 25 s (≈ 52 s pour tout le lancement). À régler pour tout nouvel agent qui enchaîne des outils puis rédige un long rapport.
- **Trace par appel au modèle** : un middleware du `IChatClient` journalise une ligne par appel (nombre de messages, durée, jetons entrée/sortie/raisonnement) ; c'est ce qui distingue un agent lent d'un agent bloqué (l'hôte écrit ces lignes dans son journal fichier).
- **`AgentFactory`** possède la connexion MCP (`McpClient`) : à libérer (`IAsyncDisposable`). `AddAgentAI` l'enregistre en **singleton** ; `Maui` en crée une par lancement pour relire les paramètres (donc n'appelle pas `AddAgentAI`).
- **Historique** : `CustomChatHistoryProvider` persiste les messages en JSON. En cas d'échec de l'appel (format incompatible après une mise à jour NuGet par exemple), appeler `ResetWithBackup(reason)` : le fichier est sauvegardé puis vidé.
- `ClientResultException`/erreurs réseau : ne pas les avaler ; l'hôte les journalise et les affiche.
- Un agent = une entrée dans `AgentKind` + un `case` dans `AgentFactory.BuildAgentDefinitionAsync` (+ instructions `.md` + fournisseur de contexte si besoin).
- Le streaming (`RunStreamingAsync`) n'est pas utilisé : avec `StoredOutputEnabled = false` il a produit un `400 invalid_payload` en sonde ; les hôtes appellent `RunAsync`.

## 6. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
