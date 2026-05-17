# SPECS.md — Client (Blazor WASM)

**Statut :** Implémenté  
**Version :** 1.1  
**Date :** 2026-05-17

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
| ROI / Total des Achats | `PortfolioMetricsDto.RoiOnTotalPurchases` | `N/A` |
| Risque moyen (0 – 4) | `PortfolioMetricsDto.AverageRisk` | `—` |

Les cartes ROI sont colorées en vert (`roi-positive`) si positif, rouge (`roi-negative`) si négatif, neutre si `null`.

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

---

## 4. Vue historique (`/historique`)

Graphique en courbes (ApexCharts) représentant l'évolution de la performance, indexée à 100 à la date T0 (première entrée disponible). 3 séries :

| Série | Source |
|---|---|
| Portefeuille | `SnapshotDto.PortfolioTotal` |
| LifeStrategy 60 | `SnapshotDto.LifeStrategy60` |
| MSCI World | `SnapshotDto.MsciWorld` |

Les séries de référence sont masquées si les données sont indisponibles.

---

## 5. Formatage

| Méthode | Exemple de sortie |
|---|---|
| `value.ToEurAmount()` | `€ 12 345,67` |
| `value.ToPercentage()` | `15,50 %` |
| `value.CssRoiClass()` | `"roi-positive"` / `"roi-negative"` / `""` |
