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
| Tests | Pas de projet dédié : `AgentRunner`, `RssNewsService` et les instructions sont testés dans `Maui.Tests/` |

## 3. Structure

```
AgentAI.Core/
├── AgentAI.Core.csproj
├── Agents/
│   ├── IAgentFactory.cs        # CreateAsync(AgentKind, contextInput, optionalInput) → (AIAgent, ICustomChatHistoryProvider)
│   ├── AgentFactory.cs         # implémentation : un AIAgent par AgentKind, historique client (StoredOutputEnabled = false)
│   ├── AgentKind.cs            # Chat, Weather, Stock, Portfolio, News
│   ├── AgentAIOptions.cs       # configuration fournie par l'hôte (voir §4)
│   ├── AgentDefinition.cs      # record interne : instructions, outils, fichier d'historique, fournisseurs de contexte
│   └── InstructionLoader.cs    # charge les instructions embarquées
├── Instructions/               # AnalyseAction.md, ActualitesSociete.md, PortfolioAllocation.md (ressources embarquées)
├── Providers/                  # AIContextProvider : Composite, Date, News, Stock, Weather
├── History/                    # ICustomChatHistoryProvider, CustomChatHistoryProvider (historique JSON sur disque)
├── Mcp/                        # InvestZaptoMcpClient, ForceContentLengthHandler
├── Services/                   # INewsService/RssNewsService, IWeatherService/WeatherService, NewsArticle
├── Hosting/ServiceCollectionExtensions.cs   # AddAgentAI(options) / AddAgentAI(optionsFactory)
└── Docs/                       # ce dossier : CLAUDE.md, diagramme de classes
```

## 4. Configuration (`AgentAIOptions`)

La bibliothèque **ne lit jamais les variables d'environnement** : un hôte MAUI n'a pas d'environnement de processus significatif, chaque hôte décide d'où viennent les valeurs.

| Propriété | Rôle |
|---|---|
| `FoundryProjectEndpoint` (requis) | endpoint du projet Azure AI Foundry |
| `Model` | nom du déploiement du modèle, `gpt-5-mini` par défaut (`DefaultModel`) |
| `InvestZaptoMcpUrl` | URL du MCP InvestZapto — requise uniquement par l'agent Portfolio |
| `HistoryDirectory` (requis) | dossier **inscriptible** des historiques de conversation (le dossier de l'exécutable est en lecture seule pour une application packagée) |

`Maui` les construit via `AgentOptionsProvider` à partir de la page Paramètres.

## 5. Règles et pièges

- **Instructions embarquées** (`EmbeddedResource`, `LogicalName = Instructions.<fichier>`) et non copiées à côté de l'exécutable : un hôte MAUI n'a pas de dossier de base prévisible. Toute modification d'un `.md` impose une recompilation.
- **Échange à tour unique** : `ActualitesSociete.md` et `AnalyseAction.md` contiennent la règle « pas de question finale, pas de suite proposée » (l'hôte ne propose pas de boucle conversationnelle). La conserver dans tout nouvel agent destiné à Maui. Un test (`Maui.Tests/AgentInstructionsTests.cs`) vérifie sa présence.
- **`HttpClient` injecté** (`InvestZaptoMcpClient`, `RssNewsService`) : l'hôte choisit ses handlers. Le backend InvestZapto échoue en `500 Backend call failure` sur les POST en `Transfer-Encoding: chunked` ; `ForceContentLengthHandler` (devant un `SocketsHttpHandler`) bufferise le corps pour envoyer un `Content-Length`. Ne pas réutiliser le `HttpClient` du dashboard (il ajoute le mot de passe du dashboard) pour les flux RSS.
- **Actualités** : `RssNewsService` filtre les résultats **Google News** sur le titre (le titre doit contenir le nom de la société, sans tenir compte de la casse, des accents ni des suffixes juridiques `NV`/`SA`/`SE`…, par mots entiers). Raison : Google cherche la phrase dans tout le texte de l'article, et l'agent ne voit que les titres — il signalait sinon des articles « ambigus ». Le flux Yahoo (ciblé par ticker) n'est pas filtré. Limite : un titre pertinent qui ne nomme pas la société est écarté.
- **MCP** : seul le transport Streamable HTTP est géré par le serveur ; `InvestZaptoMcpClient` le force (sinon repli sur un SSE hérité refusé en 404) et réessaie 3 fois sur `HttpRequestException`.
- **`AgentFactory`** possède la connexion MCP (`McpClient`) : à libérer (`IAsyncDisposable`). `AddAgentAI` l'enregistre en **singleton** ; `Maui` en crée une par lancement pour relire les paramètres (donc n'appelle pas `AddAgentAI`).
- **Historique** : `CustomChatHistoryProvider` persiste les messages en JSON. En cas d'échec de l'appel (format incompatible après une mise à jour NuGet par exemple), appeler `ResetWithBackup(reason)` : le fichier est sauvegardé puis vidé.
- `ClientResultException`/erreurs réseau : ne pas les avaler ; l'hôte les journalise et les affiche.
- Un agent = une entrée dans `AgentKind` + un `case` dans `AgentFactory.BuildAgentDefinitionAsync` (+ instructions `.md` + fournisseur de contexte si besoin).

## 6. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
