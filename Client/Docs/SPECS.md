# SPECS.md — Client (Blazor WASM)

**Statut :** Implémenté  
**Version :** 1.4  
**Date :** 2026-05-22

---

## 1. Vue d'ensemble

Le dashboard expose deux pages :

| Route | Vue |
|---|---|
| `/` | Dashboard — état instantané du portefeuille |
| `/historique` | Historique — évolution de la performance dans le temps |

---

## 2. En-tête KPI

6 cartes affichées en haut de chaque page :

| Carte | Source | Comportement si indisponible |
|---|---|---|
| Valeur totale | `SnapshotDto.PortfolioTotal` | `—` |
| Dernière mise à jour | `SnapshotDto.Date` | `—` |
| Actifs en portefeuille | `AssetDto[]` count | `0` |
| ROI / Capital Engagé | `PortfolioMetricsDto.RoiOnCapitalEngaged` | `N/A` |
| Risque moyen (0 – 4) | `PortfolioMetricsDto.AverageRisk` | `—` |

La carte ROI est colorée en vert (`roi-positive`) si positif, rouge (`roi-negative`) si négatif, neutre si `null`.

Les cartes **Valeur totale** et **ROI (Capital Engagé)** affichent à droite de leur valeur jusqu'à cinq chips de variation :

| Chip | Calcul | Source |
|---|---|---|
| J (quotidien) | `(last − prev) / \|ref\| × 100` | dernier vs avant-dernier snapshot |
| S (hebdomadaire) | idem | dernier vs snapshot ≤ J−7 |
| M (mensuel) | idem | dernier vs snapshot ≤ J−30 |
| YTD (depuis le 1er janvier) | idem | dernier vs **1er snapshot de l'année courante** |
| 1A (annuel) | idem | dernier vs snapshot ≤ J−365 |

- Valeur totale : variation relative de `PortfolioTotal`
- ROI : variation relative du taux ROI — `\|ROI_ref\|` au dénominateur pour gérer les ROI négatifs
- Chip vert/rouge via `roi-positive` / `roi-negative`
- Chaque chip n'est affichée que si une référence existe pour sa période ; `null` (chip masquée) si historique insuffisant, aucun snapshot de référence trouvé, ou `ROI_ref = 0`. Pour YTD avec un seul snapshot dans l'année, la référence est ce snapshot → variation `0 %`
- Calculés côté Client dans `DashboardViewModel` depuis `_snapshotHistory` (`GET /api/snapshot/history`), via les sélecteurs de référence `RefDaysBack` (J/S/M/1A) et `RefYearStart` (YTD)

---

## 3. Vue principale — Dashboard (`/`)

### 3.1 Vue initiale — 3 donuts côte à côte

3 graphiques à secteurs (ApexCharts) affichés simultanément :

| Donut | Dimension |
|---|---|
| Classes d'actifs | `AssetClass` |
| Types de supports | `SupportType` |
| Niveaux de risque | `Risk` (0–4) |

Cliquer sur un secteur active le **mode Master-Detail** pour la hiérarchie correspondante.

### 3.2 Mode Master-Detail — layout drill-down

Quand une hiérarchie est active, la vue affiche :
- **Colonne gauche (5/12)** : `DrillDownDonut` — graphique en mode plein écran avec fil d'Ariane en titre et bouton retour
- **Colonne droite (7/12)** :
  - `DistributionTable` si le niveau courant n'est pas le niveau feuille
  - `AssetTable` si le niveau feuille est atteint

### 3.3 Hiérarchies de drill-down

| Hiérarchie | Niveau 0 | Niveau 1 | Niveau 2 | Niveau 3 |
|---|---|---|---|---|
| Classes d'actifs | AssetClass | AssetType | Actifs (feuille) | — |
| Classes d'actifs (ETF_Stocks + toggle) | AssetClass | AssetType=ETF_Stocks | Information (thématique) | Actifs (feuille) |
| Classes d'actifs (Stocks/Bonds + géographie) | AssetClass | Stocks ou Bonds | Zone géographique | Actifs (feuille) |
| Classes d'actifs (Stocks/Bonds + secteur) | AssetClass | Stocks ou Bonds | Secteur économique | Actifs (feuille) |
| Types de supports | SupportType | Support | Actifs (feuille) | — |
| Niveaux de risque | Risk | Actifs (feuille) | — | — |

### 3.4 Toggle ETF_Stocks — groupement par thématique

Quand le drill-down Classes d'actifs atteint le niveau `AssetType = ETF_Stocks`, un switch **"Grouper par thématique"** apparaît dans l'en-tête du donut. Activé, il insère un niveau intermédiaire groupant les ETF_Stocks par leur champ `information` avant d'atteindre les actifs individuels. Désactivé, la hiérarchie descend directement aux actifs.

### 3.5 Tableau des actifs (niveau feuille)

Colonnes affichées : Nom, Valeur actuelle (€), Plus-value latente (€), ROI (%), Rendement (%).  
Tri : par valeur actuelle décroissante.  
Footer : somme de la colonne Valeur actuelle.  
Les champs `null` (données incomplètes) sont affichés `—`.

### 3.6 Tableau de distribution (niveaux intermédiaires)

Colonnes affichées : Nom, Valeur actuelle (€), Poids (%).  
Footer : total de la colonne Valeur actuelle.

### 3.7 Répartition géographique et par secteur (Stocks / Bonds — niveau 1)

Quand le drill-down Classes d'actifs atteint le niveau 1 et que la classe sélectionnée est `Stocks` ou `Bonds`, la colonne droite affiche **deux donuts côte à côte** à la place du tableau de distribution habituel :

- **Zones géographiques** : alimenté par `ViewModel.GetGeographyForClass(assetClass)`, pré-chargé au démarrage depuis `GET /api/portfolio/geography/{assetClass}`
- **Secteurs** : alimenté par `ViewModel.GetSectorForClass(assetClass)`, calculé côté client depuis les actifs chargés

**Navigation zone** : cliquer sur une zone remplace les deux donuts par un `AssetTable` filtré via `ViewModel.GetAssetsForZone(assetClass, zone)` — actifs dont le champ `geography` contient la zone. Bouton **Retour** ramène aux deux donuts. Géré par `_selectedZone` dans `Dashboard.razor`.

**Navigation secteur** : cliquer sur un secteur remplace les deux donuts par un `AssetTable` filtré via `ViewModel.GetAssetsForSector(assetClass, sector)` — actifs dont le champ `sector` correspond au secteur. Bouton **Retour** ramène aux deux donuts. Géré par `_selectedSector` dans `Dashboard.razor`.

`_selectedZone` et `_selectedSector` sont mutuellement exclusifs — en sélectionner un efface l'autre. Les deux sont indépendants de `PanelState`.

---

## 4. Vue historique (`/historique`)

Graphique en courbes (ApexCharts) représentant l'évolution de la performance, indexée à 100 à la date T0 (première entrée disponible). 4 séries :

| Série | Calcul | Masquée si |
|---|---|---|
| Portefeuille (ROI) | `(PortfolioTotal + TotalReturns) / TotalPurchases`, normalisé base 100 | jamais |
| Portefeuille (ROIC) | `(PortfolioTotal + TotalReturns) / PortfolioTotal`, normalisé base 100 | jamais |
| LifeStrategy 40 | prix unitaire / prix T0 × 100 | `LifeStrategy` absent sur un point |
| MSCI World | prix unitaire / prix T0 × 100 | `MsciWorld` absent sur un point |

Les données sont fournies par `GET /api/portfolio/metrics/history` (`PerformancePointDto[]`), déjà normalisées base 100 par l'Api. Seuls les snapshots avec `PortfolioTotal > 0` et `TotalPurchases > 0` sont inclus dans le calcul.

---

## 5. Page de chargement

Affichée pendant les deux phases de démarrage :
1. **Phase WASM** (`index.html`) : pendant le téléchargement du runtime Blazor
2. **Phase données** (`Dashboard.razor`) : pendant les appels API parallèles à l'initialisation

Overlay plein écran (`position: fixed`, `z-index: 9999`) — couvre la barre de navigation. Texte "Chargement" animé en typewriter (lettre par lettre, 1s), maintenu 1s, puis réinitialisé — sans écriture inversée. Police 38px semi-bold, couleur `#787774`. Classes CSS : `.loading-screen`, `.loading-text` dans `css/app.css`.

---

## 6. Formatage

| Méthode | Exemple de sortie |
|---|---|
| `value.ToEurAmount()` | `€ 12 345,67` |
| `value.ToPercentage()` | `15,50 %` |
| `value.CssRoiClass()` | `"roi-positive"` / `"roi-negative"` / `""` |
| `value.ToSignedPercentage()` | `"+1,23 %"` / `"-0,45 %"` |
