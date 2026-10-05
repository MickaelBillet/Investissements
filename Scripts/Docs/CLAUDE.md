# CLAUDE.md — Scripts Google Apps Script

---

## 1. Rôle

Les Scripts s'exécutent exclusivement dans l'**éditeur Google Apps Script** (script.google.com) — aucune commande de build locale.

Trois responsabilités :
- **ETL quotidien** (trigger 06h00) : synchroniser les valeurs courantes depuis le Bilan (SOURCE), créer les actifs nouvellement apparus, calculer et appender un snapshot dans la feuille historique (DEST)
- **Rapport hebdomadaire** (trigger lundi 08h00) : email HTML récapitulatif
- **Synchronisation manuelle** (`SyncWebApp.gs`) : unique point d'entrée HTTP (`doGet`), protégé par clé secrète, qui relance `snapshotQuotidien()` à la demande du bouton « Synchroniser » du dashboard

Le dashboard (Api Azure Functions) **lit** le Sheet `InvestData` directement via l'API Google Sheets officielle (compte de service, lecteur seul), voir `Api/Docs/CLAUDE.md`. Il n'appelle ces scripts que pour la synchronisation manuelle (`POST /api/sync` → `SyncService` → `GET .../exec?key=...`).

---

## 2. Structure des fichiers

| Fichier | Rôle |
|---|---|
| `Config.gs` | Constantes partagées : IDs des feuilles, index de colonnes, enumerations |
| `Router.gs` | Helpers partagés lus par l'ETL et le rapport hebdo : `getAssetsData()`, `getPortfolioTotal()`, `aggregateGroup()`, `getReferenceIds()`, `groupBy()`, `sumColumn()` |
| `SyncData.gs` | ETL : `syncCurrentTotal()` — synchronise les colonnes J–N de l'onglet Asset (risque, achats, ventes, dividendes, valeur actuelle) depuis le Bilan, puis crée les actifs manquants (`getExistingAssetNames`, `getNextAssetId`, `findNewAssetsToAdd`, `buildNewAssetRow`, `sendNewAssetsAlertEmail`) |
| `SyncWebApp.gs` | Web App HTTP minimal : `doGet(e)` compare `?key=` à la Script Property `SYNC_SECRET_KEY` puis exécute `snapshotQuotidien()` ; réponse JSON `{success, addedCount}` ou `{success:false, error}` |
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

**Déploiement du Web App de synchro (`SyncWebApp.gs`)** :
1. Définir la Script Property `SYNC_SECRET_KEY` (Paramètres du projet → Propriétés du script). Elle n'est **jamais** commitée, contrairement à `Config.gs`.
2. Déployer → Nouveau déploiement → Application Web ; exécuter en tant que **moi**, accès **Tout le monde** (la protection est la clé partagée, pas Google).
3. Reporter l'URL `/exec` et la clé dans les App Settings de l'Api (`APPS_SCRIPT_SYNC_URL`, `APPS_SCRIPT_SYNC_KEY`).

Après modification des `.gs`, redéployer une **nouvelle version** du Web App pour que l'URL `/exec` serve le nouveau code. Aucun autre point d'entrée HTTP ne doit être introduit.

---

## 4. ETL quotidien — `snapshotQuotidien()`

Appelé automatiquement à 06h00 via le déclencheur créé par `creerDeclencheurSnapshot()`.

```
1. syncCurrentTotal()    → met à jour les colonnes J–N de l'onglet Asset (DEST) et crée les actifs manquants
2. getAssetsData()       → lit toutes les lignes valides de l'onglet Asset
3. resultSheet C49 (NET_PURCHASES)   → netCapital     (capital net réellement engagé, lu depuis le Bilan)
4. resultSheet F70 (TOTAL_PURCHASES) → totalPurchases (lu directement depuis le Bilan)
5. resultSheet F62 (TOTAL_RETURNS)   → totalReturns   (lu directement depuis le Bilan)
6. resultSheet F72 (TOTAL_SALES)     → totalSales     (lu directement depuis le Bilan)
7. fetchStockValues()    → prix LifeStrategy (AMS:V40A) et MSCI World (EPA:MWRD)
8. Si une ligne existe déjà pour la date du jour → overwrite ; sinon → appendRow
   [date, netCapital, ref1, ref2, totalPurchases, totalReturns, totalSales]
```

`netCapital`, `totalPurchases`, `totalReturns` et `totalSales` sont lus directement depuis des cellules du Bilan (SOURCE) car ils couvrent l'historique complet incluant les actifs vendus, non listés dans l'onglet Asset.

Les références de cellules sont des constantes de `Config.gs` : **toute insertion ou suppression de ligne dans le Bilan les décale** (ce fut le cas C48→C49, F66→F70, F58→F62, F68→F72) — les mettre à jour dans le même geste. `CASH_PEA` (B70) et `SMART_CASH_MINTOS` (B72) sont lues par `syncCurrentTotal()` mais leurs valeurs ne sont pas utilisées à ce jour.

### Création automatique des actifs
Après la synchro des valeurs, toute ligne du Bilan absente de l'onglet Asset est ajoutée **si sa valeur actuelle est strictement positive** (nombre > 0). `COL_ID` = max des IDs + 1 (coercés avec `Number()`, car certains sont saisis en texte), écrit en format texte `@`. Les colonnes de classification (AssetClass, SupportType, Support, AssetType, Sector, Geography) sont posées à `"Not Defined"` et `sendNewAssetsAlertEmail()` prévient `REPORT_EMAIL` pour qu'on les complète à la main. Les lignes `"Not Defined"` sont ignorées partout ailleurs.

### Synchronisation manuelle (`SyncWebApp.gs`)
`doGet` renvoie `{success:false, error:"Unauthorized"}` si la clé est absente ou fausse, sinon exécute `snapshotQuotidien()` (ETL complet, identique au trigger de 06h00). `addedCount` vaut toujours 0 aujourd'hui : `snapshotQuotidien()` ne retourne pas de valeur.

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

## 7. Tests (`Test.gs`)

Pas de framework : fonctions `test*` à lancer dans l'éditeur. Disponibles : `testSnapshotGetHistory`, `testAssetClassGetDistribution`, `testSupportTypeGetDistribution`, `testAssetGetDistributionByRisk`, `testRapportHebdomadaire`, `testGetNextAssetId`, `testFindNewAssetsToAdd`, `testBuildNewAssetRow`, `testBuildSnapshotRow`. Ajouter une fonction de test pour toute nouvelle fonction ou action.

---

## 8. Enumerations et constantes (`Config.gs`)

Toutes les valeurs de dimension sont des constantes dans `Config.gs` (`ASSET_CLASS`, `ASSET_TYPE`, `SUPPORT_TYPE`, `SUPPORT`, `RISK`). Ne jamais coder de chaînes en dur — utiliser toujours ces constantes.

`Config.gs` est commité (IDs de feuilles, adresse du rapport) : aucun secret ne doit y figurer. Les constantes `SHEET_ASSET_CLASS`, `SHEET_ASSET_TYPE`, `SHEET_SUPPORT_TYPE` et `SHEET_SUPPORT` ne sont plus utilisées.

Les constantes `COL_SOURCE_*` définissent les index de colonnes de la feuille source (Bilan). Les constantes `COL_*` définissent les index de colonnes de l'onglet Asset (DEST).

---

## 9. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
