# CLAUDE.md — Scripts Google Apps Script

---

## 1. Rôle

Les Scripts constituent la couche ETL et API du projet. Ils s'exécutent exclusivement dans l'**éditeur Google Apps Script** (script.google.com) — aucune commande de build locale.

Deux responsabilités :
- **ETL quotidien** : synchroniser les valeurs courantes depuis le Bilan (SOURCE), calculer et appender un snapshot dans la feuille historique (DEST)
- **API REST** : exposer les données de la feuille DEST via un Web App HTTP (lecture seule)

---

## 2. Structure des fichiers

| Fichier | Rôle |
|---|---|
| `Config.gs` | Constantes partagées : IDs des feuilles, index de colonnes, enumerations |
| `Router.gs` | Point d'entrée HTTP `doGet(e)`, authentification, dispatch, helpers partagés |
| `SyncData.gs` | ETL : `syncCurrentTotal()` — synchronise les colonnes I–L de l'onglet Asset depuis le Bilan |
| `SnapshotService.gs` | ETL : `snapshotQuotidien()` — calcule et appende un snapshot quotidien ; endpoints `getLast` / `getHistory` |
| `StockValueService.gs` | Récupère les prix des ETF de référence via `GOOGLEFINANCE` (cellule temporaire) |
| `AssetClasseService.gs` | Service `AssetClass` |
| `AssetTypeService.gs` | Service `AssetType` |
| `SupportTypeService.gs` | Service `SupportType` |
| `SupportService.gs` | Service `Support` |
| `AssetService.gs` | Service `Asset` |
| `Test.gs` | Fonctions de test manuelles |

---

## 3. Exécution et déploiement

- **Exécuter une fonction** : sélectionner dans le menu déroulant, cliquer Run
- **Exécuter un test** : sélectionner une fonction `test*` dans `Test.gs`, cliquer Run — résultats dans les Logs (`Ctrl+Entrée`)
- **Déployer en Web App** : Deploy → New deployment → Web App (execute as me, access: anyone)
- **Initialiser le token API** : exécuter `setApiToken()` une fois après chaque nouveau déploiement
- **Créer le déclencheur quotidien** : exécuter `creerDeclencheurSnapshot()` une fois — enregistre `snapshotQuotidien` à 06h00 chaque jour

---

## 4. Flux de requête HTTP

`doGet(e)` dans `Router.gs` est l'unique point d'entrée. Chaque requête doit passer un paramètre `apiKey` (token stocké dans Script Properties). Le routage se fait sur `?service=X&action=Y` :

```
GET ?apiKey=...&service=AssetClass&action=getAll
         │
    Router.gs → doGet(e)
         ├── AssetClass   → AssetClasseService.gs
         ├── AssetType    → AssetTypeService.gs
         ├── SupportType  → SupportTypeService.gs
         ├── Support      → SupportService.gs
         ├── Asset        → AssetService.gs
         └── Snapshot     → SnapshotService.gs
```

---

## 5. ETL quotidien — `snapshotQuotidien()`

Appelé automatiquement à 06h00 via le déclencheur créé par `creerDeclencheurSnapshot()`.

```
1. Guard : si un snapshot existe déjà pour aujourd'hui → abort
2. syncCurrentTotal()    → met à jour les colonnes I–L de l'onglet Asset (DEST)
3. getAssetsData()       → lit toutes les lignes valides de l'onglet Asset
4. getPortfolioTotal()   → sum(currentTotal)
5. resultSheet F63       → totalPurchases (lu directement depuis le Bilan)
6. resultSheet F55       → totalReturns   (lu directement depuis le Bilan)
7. fetchStockValues()    → prix LifeStrategy60 (AMS:V60A) et MSCI World (EPA:MWRD)
8. appendRow             → [date, portfolioTotal, ref1, ref2, totalPurchases, totalReturns]
```

`totalPurchases` et `totalReturns` sont lus directement depuis des cellules du Bilan (SOURCE) car ils couvrent l'historique complet incluant les actifs vendus, non listés dans l'onglet Asset.

---

## 6. `fetchStockValues()`

Utilise une cellule temporaire `ZZ1` pour forcer le calcul `GOOGLEFINANCE` — Apps Script ne supporte pas nativement cette fonction. La cellule est effacée après lecture.

Retourne `[prixLifeStrategy60, prixMSCIWorld]`. En cas d'erreur, retourne `-1` pour le ticker concerné.

---

## 7. Pattern des services

Chaque fichier service suit le même schéma :

| Fonction | Rôle |
|---|---|
| `handle*(action, params)` | Switch sur `action`, retourne les données ou `{ error: "..." }` |
| `get*All()` | Agrège toutes les lignes groupées par la dimension du service |
| `get*Distribution()` | Vue allégée `{ name, currentTotal, weightInPortfolio }` par groupe |
| `getBy*()` | Drill-down : filtre par valeur de dimension |

Helpers partagés dans `Router.gs` : `getAssetsData()`, `getPortfolioTotal()`, `getDividendsTotal()`, `getTotalPurchases()`, `getTotalSales()`, `buildAssetRow()`, `aggregateGroup()`, `groupBy()`, `sumColumn()`.

---

## 8. Pattern de test (`Test.gs`)

Les tests simulent des requêtes HTTP en construisant un faux objet `e.parameter` et en appelant `doGet(e)` :

```js
function testDoGetAllAssetClass() {
  const e = { parameter: { apiKey: "token-zapto", service: "AssetClass", action: "getAll" } };
  Logger.log(doGet(e).getContent());
}
```

Ajouter une fonction de test pour chaque nouveau service ou action avant de déployer.

---

## 9. Enumerations et constantes (`Config.gs`)

Toutes les valeurs de dimension sont des constantes dans `Config.gs` (`ASSET_CLASS`, `ASSET_TYPE`, `SUPPORT_TYPE`, `SUPPORT`, `RISK`). Ne jamais coder de chaînes en dur — utiliser toujours ces constantes.

Les constantes `COL_SOURCE_*` définissent les index de colonnes de la feuille source (Bilan). Les constantes `COL_*` définissent les index de colonnes de l'onglet Asset (DEST).
