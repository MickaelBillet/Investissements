Prépare le commit et la PR pour Investment Dashboard.

## Étape 1 — Contrôle sécurité
Vérifier qu'aucun secret n'est présent dans les fichiers modifiés :
- APPS_SCRIPT_API_KEY
- URL Apps Script
- GOOGLE_SHEET_ID
Si un secret est détecté : stopper immédiatement et alerter l'utilisateur.

## Étape 2 — Contrôle qualité
Lancer les tests du sous-projet concerné :
- Api    → dotnet test "Api.Tests/InvestissementsDashboard.Api.Tests.csproj"
- Client → dotnet test "Client.Tests/InvestissementsDashboard.Client.Tests.csproj"
- Scripts → rappeler d'exécuter manuellement les fonctions test* dans l'éditeur Apps Script
Stopper et rapporter si des tests échouent — ne pas continuer.

## Étape 3 — Nom de branche
Proposer un nom de branche selon la convention :
- feat/description-courte
- fix/description-courte
- chore/description-courte

## Étape 4 — Message de commit
Générer le message Conventional Commits :
- Format : type(scope): description courte
- Scope : api | client | scripts | shared
- Corps : liste des fichiers modifiés avec une ligne de résumé par fichier

## Étape 5 — Commandes à exécuter (proposer, ne pas exécuter)
- Exécuter les commandes de commit et PR sur la nouvelle branche
- L'utilisateur doit valider la PR manuellement dans github