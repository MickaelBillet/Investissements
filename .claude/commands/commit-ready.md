Prépare le commit et la PR pour Investment Dashboard.

## Étape 1 — Contrôle sécurité
Vérifier qu'aucun secret n'est présent dans les fichiers modifiés :
- GOOGLE_SHEET_ID, GOOGLE_SERVICE_ACCOUNT_EMAIL, GOOGLE_SERVICE_ACCOUNT_KEY
- DASHBOARD_PASSWORD, MCP_API_KEY, WEB_SEARCH_API_KEY
- APPS_SCRIPT_SYNC_URL, APPS_SCRIPT_SYNC_KEY, SYNC_SECRET_KEY
Le dépôt est public : vérifier aussi qu'aucune donnée personnelle (montants, positions) n'entre dans le commit.
Si un secret est détecté : stopper immédiatement et alerter l'utilisateur.

## Étape 2 — Contrôle qualité
Lancer les tests du sous-projet concerné :
- Api    → dotnet test "Api.Tests/InvestissementsDashboard.Api.Tests.csproj"
- Client, Client.Shared → dotnet test "Client.Tests/InvestissementsDashboard.Client.Tests.csproj"
- Maui, AgentAI.Core → dotnet test "Maui.Tests/InvestissementsDashboard.Maui.Tests.csproj"
- Scripts → rappeler d'exécuter manuellement les fonctions test* dans l'éditeur Apps Script
Stopper et rapporter si des tests échouent — ne pas continuer.

## Étape 3 — Nom de branche
Proposer un nom de branche selon la convention :
- feat/description-courte
- fix/description-courte
- refactor/description-courte
- docs/description-courte
- chore/description-courte

## Étape 4 — Message de commit
Générer le message Conventional Commits :
- Format : type(scope): description courte
- Scope : api | client | maui | agents | scripts | shared
- Corps : liste des fichiers modifiés avec une ligne de résumé par fichier

## Étape 5 — Commandes à exécuter (proposer, ne pas exécuter)
- Exécuter les commandes de commit et PR sur la nouvelle branche
- L'utilisateur doit valider la PR manuellement dans github