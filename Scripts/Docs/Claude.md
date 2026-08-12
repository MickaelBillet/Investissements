# CLAUDE.md — Scripts Google Apps Script

---

## 1. Rôle

Les Scripts s'exécutent exclusivement dans l'**éditeur Google Apps Script** (script.google.com) — aucune commande de build locale.

Deux responsabilités, toutes deux déclenchées par des triggers temporels (jamais par une requête HTTP — il n'y a plus de Web App) :
- **ETL quotidien** : synchroniser les valeurs courantes depuis le Bilan (SOURCE), calculer et appender un snapshot dans la feuille historique (DEST)
- **Rapport hebdomadaire** : email HTML récapitulatif envoyé chaque lundi

Le dashboard (Api Azure Functions) ne parle plus à ces scripts — il lit le Sheet `InvestData` directement via l'API Google Sheets officielle (compte de service), voir `Api/Docs/CLAUDE.md`.

---

## 2. Structure des fichiers

| Fichier | Rôle |
|---|---|
| `Config.gs` | Constantes partagées : IDs des feuilles, index de colonnes, enumerations |
| `Router.gs` | Helpers partagés lus par l'ETL et le rapport hebdo : `getAssetsData()`, `getPortfolioTotal()`, `aggregateGroup()`, `getReferenceIds()`, `groupBy()`, `sumColumn()` |
| `SyncData.gs` | ETL : `syncCurrentTotal()` — synchronise les colonnes I–L de l'onglet Asset depuis le Bilan |
| `SnapshotService.gs` | ETL : `snapshotQuotidien()` — calcule et appende un snapshot quotidien ; `handleSnapshot("getHistory", ...)` utilisé par le rapport hebdo |
| `StockValueService.gs` | Récupère les prix des ETF de référence via `GOOGLEFINANCE` (cellule temporaire) |
| `AssetClasseService.gs` | `handleAssetClass("getDistribution", ...)` — utilisé par le rapport hebdo |
| `SupportTypeService.gs` | `handleSupportType("getDistribution", ...)` — utilisé par le rapport hebdo |
| `AssetService.gs` | `handleAsset("getDistributionByRisk", ...)` — utilisé par le rapport hebdo |
| `MetricsService.gs` | Calcul du ROI et des variations S/M/YTD/1A depuis l'historique snapshot |
| `WeeklyReportService.gs` | Rapport email HTML hebdomadaire — envoyé chaque lundi à 08h00 |
| `Test.gs` | Fonctions de test manuelles — appellent les handlers directement (plus de simulation HTTP) |

> `AssetTypeService.gs`, `SectorService.gs`, `SupportService.gs` et `GeographyService.gs` ont été supprimés — ils ne servaient que l'ancien Web App HTTP, plus appelés par personne depuis la migration vers l'API Google Sheets côté Api.

---

## 3. Exécution et déploiement

- **Exécuter une fonction** : sélectionner dans le menu déroulant, cliquer Run
- **Exécuter un test** : sélectionner une fonction `test*` dans `Test.gs`, cliquer Run — résultats dans les Logs (`Ctrl+Entrée`)
- **Créer le déclencheur quotidien** : exécuter `creerDeclencheurSnapshot()` une fois — enregistre `snapshotQuotidien` à 06h00 chaque jour
- **Créer le déclencheur hebdomadaire** : exécuter `creerDeclencheurHebdomadaire()` une fois — enregistre `rapportHebdomadaire` chaque lundi à 08h00

Plus de déploiement Web App à gérer (pas de `setApiToken()`, pas de token d'API) — ces scripts ne sont plus jamais appelés depuis l'extérieur.

---

## 4. ETL quotidien — `snapshotQuotidien()`

Appelé automatiquement à 06h00 via le déclencheur créé par `creerDeclencheurSnapshot()`.

```
1. syncCurrentTotal()    → met à jour les colonnes I–L de l'onglet Asset (DEST)
2. getAssetsData()       → lit toutes les lignes valides de l'onglet Asset
3. resultSheet C42 (NET_PURCHASES)   → netCapital     (capital net réellement engagé, lu depuis le Bilan)
4. resultSheet F66 (TOTAL_PURCHASES) → totalPurchases (lu directement depuis le Bilan)
5. resultSheet F58 (TOTAL_RETURNS)   → totalReturns   (lu directement depuis le Bilan)
6. resultSheet F68 (TOTAL_SALES)     → totalSales     (lu directement depuis le Bilan)
7. fetchStockValues()    → prix LifeStrategy (AMS:V40A) et MSCI World (EPA:MWRD)
8. Si une ligne existe déjà pour la date du jour → overwrite ; sinon → appendRow
   [date, netCapital, ref1, ref2, totalPurchases, totalReturns, totalSales]
```

`netCapital`, `totalPurchases`, `totalReturns` et `totalSales` sont lus directement depuis des cellules du Bilan (SOURCE) car ils couvrent l'historique complet incluant les actifs vendus, non listés dans l'onglet Asset.

---

## 5. `fetchStockValues()`

Utilise une cellule temporaire `ZZ1` pour forcer le calcul `GOOGLEFINANCE` — Apps Script ne supporte pas nativement cette fonction. La cellule est effacée après lecture.

Retourne `[prixLifeStrategy, prixMSCIWorld]`. En cas d'erreur, retourne `-1` pour le ticker concerné.

---

## 6. Handlers restants (rapport hebdo uniquement)

`handleAssetClass`, `handleSupportType`, `handleAsset` et `handleSnapshot` ne gardent plus qu'une seule action chacun — celle utilisée par `rapportHebdomadaire()` (`WeeklyReportService.gs`). Appel direct de fonction, pas de requête HTTP :

```js
handleSnapshot("getHistory", {})
handleAssetClass("getDistribution", {})
handleSupportType("getDistribution", {})
handleAsset("getDistributionByRisk", {})
```

Si un nouveau besoin de lecture apparaît côté rapport hebdo, ajouter l'action directement dans le handler concerné plutôt que de réintroduire un point d'entrée HTTP générique.

---

## 7. Enumerations et constantes (`Config.gs`)

Toutes les valeurs de dimension sont des constantes dans `Config.gs` (`ASSET_CLASS`, `ASSET_TYPE`, `SUPPORT_TYPE`, `SUPPORT`, `RISK`). Ne jamais coder de chaînes en dur — utiliser toujours ces constantes.

Les constantes `COL_SOURCE_*` définissent les index de colonnes de la feuille source (Bilan). Les constantes `COL_*` définissent les index de colonnes de l'onglet Asset (DEST).

---

## 8. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
