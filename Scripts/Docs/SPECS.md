# SPECS.md — Scripts Google Apps Script

**Statut :** Implémenté
**Version :** 2.1 — lecture directe du Sheet par l'Api ; Web App HTTP réduit à la synchronisation manuelle ; création automatique des actifs
**Date :** 2026-10-05

---

## 1. Vue d'ensemble

L'Api Azure Functions lit le Google Sheet `InvestData` (`DEST_ID`) directement via l'API Google Sheets officielle, avec un compte de service (voir `Api/Docs/CLAUDE.md`). Les fichiers `.gs` couvrent trois responsabilités ; seule la troisième est atteignable par HTTP :

- **ETL quotidien** (`SnapshotService.gs` : `snapshotQuotidien()`, `SyncData.gs` : `syncCurrentTotal()`) — synchronise les valeurs courantes depuis le Bilan (SOURCE) et appende un snapshot dans la feuille historique (DEST), tous les jours à 06h00.
- **Rapport hebdomadaire** (`WeeklyReportService.gs` : `rapportHebdomadaire()`) — envoie un email HTML récapitulatif chaque lundi à 08h00. Réutilise en interne les handlers `handleSnapshot`, `handleAssetClass`, `handleSupportType`, `handleAsset` (appel de fonction direct, pas HTTP).
- **Synchronisation manuelle** (`SyncWebApp.gs` : `doGet`) — relance `snapshotQuotidien()` à la demande du bouton « Synchroniser » du dashboard (voir §2.2).

---

## 2. ETL quotidien — `snapshotQuotidien()`

### 2.1 Déroulé

Appelé automatiquement à 06h00 via le déclencheur créé par `creerDeclencheurSnapshot()`.

```
1. syncCurrentTotal()    → met à jour les colonnes J–N de l'onglet Asset (DEST), puis crée les actifs manquants (§2.3)
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

`fetchStockValues()` utilise une cellule temporaire `ZZ1` pour forcer le calcul `GOOGLEFINANCE` (Apps Script ne le supporte pas nativement). Retourne `[prixLifeStrategy, prixMSCIWorld]`, `-1` en cas d'erreur sur un ticker.

### 2.2 Synchronisation manuelle (`SyncWebApp.gs`)

`GET <URL /exec>?key=<SYNC_SECRET_KEY>` exécute `snapshotQuotidien()` (ETL complet, ligne `Snapshot` du jour incluse). Réponses JSON :

| Cas | Réponse |
|---|---|
| Clé absente ou incorrecte | `{ "success": false, "error": "Unauthorized" }` |
| Succès | `{ "success": true, "addedCount": 0 }` (`addedCount` vaut 0 : `snapshotQuotidien()` ne retourne rien) |
| Exception | `{ "success": false, "error": "<message>" }` |

La clé est la Script Property `SYNC_SECRET_KEY` (jamais commitée). Seul appelant légitime : `SyncService` de l'Api (`POST /api/sync`, voir `Api/Docs/SPECS.md`).

### 2.3 Création automatique des actifs

| Règle | Détail |
|---|---|
| Détection | Ligne du Bilan (`COL_SOURCE_ASSETS`) absente de l'onglet Asset, dédoublonnée par nom |
| Éligibilité | Valeur actuelle numérique **strictement positive** |
| Identifiant | max(`COL_ID`) + 1, écrit en texte |
| Colonnes de classification | `"Not Defined"` (AssetClass, SupportType, Support, AssetType, Sector, Geography) |
| Alerte | Email à `REPORT_EMAIL` listant les nouveaux actifs, à classer manuellement |

---

## 3. Rapport hebdomadaire (`WeeklyReportService.gs`)

Envoyé automatiquement chaque **lundi à 08h00** à `mickael.billet@gmail.com` via `MailApp`.

**Déclencheur :** exécuter `creerDeclencheurHebdomadaire()` une fois pour l'enregistrer.

**Contenu du rapport :**

| Section | Données |
|---|---|
| Capital net engagé | `netCapital` du dernier snapshot + variations S/M/YTD/1A |
| Actifs en portefeuille | Nombre d'actifs actifs |
| Risque moyen | Moyenne pondérée par `currentTotal` sur l'échelle 0–4 |
| ROI Capital Engagé | Valeur courante + variations S/M/YTD/1A |
| Répartition par classe d'actifs | `handleAssetClass("getDistribution", {})` |
| Répartition par type de support | `handleSupportType("getDistribution", {})` |
| Répartition par niveau de risque | `handleAsset("getDistributionByRisk", {})` |
| Historique complet | `handleSnapshot("getHistory", {})` |

**Calcul des variations (`MetricsService.gs`) :**

| Période | Snapshot de référence |
|---|---|
| S (hebdo) | Dernier snapshot ≤ J−7 |
| M (mensuel) | Dernier snapshot ≤ J−30 |
| YTD | Premier snapshot ≥ 1er janvier de l'année en cours |
| 1A | Dernier snapshot ≤ J−365 |

**Formule ROI** (calculée dans `computeRoi`) :
```
roiOnCapitalEngaged = totalReturns / netCapital × 100
```

---

## 4. Objets retournés par les handlers restants

Ces handlers ne sont plus exposés en HTTP — ils ne sont appelés que par `rapportHebdomadaire()` (voir section 3) et testables directement depuis `Test.gs`.

### `Aggregate` (`handleAsset("getDistributionByRisk", {})`)
```json
{
  "name": "Risk 4",
  "totalPurchases": 12500.00,
  "totalSales": 800.00,
  "dividends": 320.00,
  "currentTotal": 14200.00,
  "hasIncompleteData": false,
  "unrealizedGain": 2520.00,
  "yield": 2.73,
  "roi": 16.12,
  "weightInGroup": 45.20,
  "weightInPortfolio": 18.30
}
```

### `Distribution` (`handleAssetClass`/`handleSupportType`, action `getDistribution`)
```json
{ "id": 0, "name": "Stocks", "currentTotal": 14200.00, "weightInPortfolio": 18.30 }
```

### `Snapshot` (`handleSnapshot("getHistory", {})`)
```json
{
  "date": "2026-05-04",
  "netCapital": 59149.20,
  "lifeStrategy": 42.15,
  "msciWorld": 87.30,
  "totalPurchases": 65000.00,
  "totalReturns": 83200.00,
  "totalSales": 1351.28
}
```
