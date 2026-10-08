# SPECS.md — Client.Shared (dashboard partagé WASM + MAUI)

**Statut :** Implémenté  
**Version :** 1.7  
**Date :** 2026-10-05

---

## 1. Vue d'ensemble

Ces spécifications décrivent le dashboard tel qu'il s'affiche dans les deux hôtes (site `Client/` et application `Maui/`), qui partagent le même code. Avant toute page, un **écran de connexion** (§8) protège l'ensemble ; une fois connecté, le dashboard expose deux pages :

| Route | Vue |
|---|---|
| `/` | Dashboard — état instantané du portefeuille |
| `/suivi` | Suivi — évolution de la performance dans le temps et échéancier de remboursement obligataire |

---

## 2. En-tête KPI

5 cartes affichées en haut de chaque page :

| Carte | Source | Comportement si indisponible |
|---|---|---|
| Capital net engagé | `SnapshotDto.NetCapital` | `—` |
| Dernière mise à jour | `SnapshotDto.Date` | `—` |
| Actifs en portefeuille | `AssetDto[]` count | `0` |
| ROI / Capital Engagé | `PortfolioMetricsDto.RoiOnCapitalEngaged` | `N/A` |
| Risque moyen (0 – 4) | `PortfolioMetricsDto.AverageRisk` | `—` |

La carte ROI est colorée en vert (`roi-positive`) si positif, rouge (`roi-negative`) si négatif, neutre si `null`.

Les cartes **Capital net engagé** et **ROI (Capital Engagé)** affichent à droite de leur valeur jusqu'à cinq chips de variation, mesurant deux choses différentes :

- **Capital net engagé** : variation de `NetCapital` — combien a été versé/retiré sur la période, pas une performance
- **ROI (Capital Engagé)** : variation du `ROIC` (série TWR, `GetIndexedHistoryAsync`) — la vraie performance de marché du portefeuille, neutralisée de l'effet des versements/retraits

| Chip | Calcul | Source |
|---|---|---|
| J (quotidien) | `(last − ref) / ref × 100` | dernier vs avant-dernier point |
| S (hebdomadaire) | idem | dernier vs point ≤ J−7 |
| M (mensuel) | idem | dernier vs point ≤ J−30 |
| YTD (depuis le 1er janvier) | idem | dernier vs **1er point de l'année courante** |
| 1A (annuel) | idem | dernier vs point ≤ J−365 |

- Capital net engagé : calculé depuis `_snapshotHistory` (`GET /api/snapshot/history`), base `NetCapital`
- ROI (Capital Engagé) : calculé depuis `_performanceHistory` (`GET /api/portfolio/metrics/history`, série TWR), base `ROIC`
- Chip vert/rouge via `roi-positive` / `roi-negative`
- Chaque chip n'est affichée que si une référence existe pour sa période ; `null` (chip masquée) si historique insuffisant, aucun point de référence trouvé, ou valeur de référence = 0. Pour YTD avec un seul point dans l'année, la référence est ce point → variation `0 %`

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

Dans le drill-down **par zone géographique** (§3.7), une colonne **Coefficient zone** (%) s'insère après Valeur actuelle : part de chaque actif allouée à la zone sélectionnée. Le total du footer est alors pondéré par ces coefficients au lieu d'être la somme brute des valeurs.

### 3.6 Tableau de distribution (niveaux intermédiaires)

Colonnes affichées : Nom, Valeur actuelle (€), Poids (%).  
Footer : total de la colonne Valeur actuelle.

### 3.7 Répartition géographique et par secteur (Stocks / Bonds — niveau 1)

Quand le drill-down Classes d'actifs atteint le niveau 1 et que la classe sélectionnée est `Stocks` ou `Bonds`, la colonne droite affiche **deux donuts côte à côte** à la place du tableau de distribution habituel :

- **Zones géographiques** : alimenté par `ViewModel.GetGeographyForClass(assetClass)`, pré-chargé au démarrage depuis `GET /api/portfolio/geography/{assetClass}`
- **Secteurs** : alimenté par `ViewModel.GetSectorForClass(assetClass)`, calculé côté client depuis les actifs chargés, filtré aux `AssetType` marqués éligibles (`GeoSectorEligible = TRUE` dans l'onglet `AssetType` du Sheet, exposé via `GET /api/assets/types/reference`)

**Agents IA (MAUI uniquement)** : dans l'application Windows, chaque ligne `Stock` d'un `AssetTable` propose un bouton « Lancer un agent » ; un dialogue demande l'agent (Stock ou News), puis une fenêtre affiche la réponse (progression, résultat ou erreur). Absent du site web. Voir `CLAUDE.md` §7.10.

**Navigation zone** : cliquer sur une zone remplace les deux donuts par un `AssetTable` filtré via `ViewModel.GetAssetsForZone(assetClass, zone)` — actifs dont le champ `geography` contient la zone. Bouton **Retour** ramène aux deux donuts. Géré par `DashboardViewModel.SelectedZone`.

**Navigation secteur** : cliquer sur un secteur remplace les deux donuts par un `AssetTable` filtré via `ViewModel.GetAssetsForSector(assetClass, sector)` — actifs dont le champ `sector` correspond au secteur. Bouton **Retour** ramène aux deux donuts. Géré par `DashboardViewModel.SelectedSector`.

`SelectedZone` et `SelectedSector` (`DashboardViewModel`) sont mutuellement exclusifs — en sélectionner un efface l'autre. Les deux sont indépendants de `PanelState`.

---

## 4. Vue Suivi (`/suivi`)

La vue présente 2 onglets (`MudTabs`/`MudTabPanel`), chacun occupant toute la hauteur disponible sans scroll — plutôt qu'un empilement vertical des deux graphiques.

### 4.1 Onglet "Performance"

Graphique en courbes (ApexCharts, `HistoryChart.razor`) représentant l'évolution de la performance, indexée à 100 à la date T0 (première entrée disponible). 3 séries :

| Série | Calcul | Masquée si |
|---|---|---|
| Portefeuille (ROIC) | TWR chaîné (rendement journalier neutralisant les flux de capital), base 100 — voir `Api/Docs/SPECS.md` §2.7 | jamais |
| LifeStrategy 40 | prix unitaire / prix T0 × 100 | `LifeStrategy` absent sur un point |
| MSCI World | prix unitaire / prix T0 × 100 | `MsciWorld` absent sur un point |

Les données sont fournies par `GET /api/portfolio/metrics/history` (`PerformancePointDto[]`), déjà normalisées base 100 par l'Api. Seuls les snapshots avec `NetCapital > 0`, `LifeStrategy` et `MsciWorld` renseignés sont inclus dans le calcul.

### 4.2 Onglet "Échéancier"

Graphique en barres (ApexCharts, `BondScheduleChart.razor`) représentant le capital obligataire à percevoir par période d'échéance (hors coupons).

Les données sont fournies par `GET /api/assets/bondschedule` (`BondScheduleDto[]`), agrégées **par mois** par l'Api (granularité la plus fine), avec le détail par obligation (`bonds[]`) — voir `Api/Docs/SPECS.md` §2.5 pour la logique de calcul. C'est le Client qui ré-agrège ensuite ces données mensuelles en trimestre ou en année pour l'affichage (`TrackingViewModel.BondScheduleDisplayed`), sans nouvel appel réseau.

**Bascule trimestre/année** : un `MudSwitch` (`BondScheduleQuarterlyView`, clé `BondSchedule_QuarterlyToggle`) au-dessus du graphique permet de choisir la granularité d'affichage. Par défaut, la vue est annuelle. Chaque bascule réinitialise la période sélectionnée pour le drill-down.

**Drill-down au clic** : cliquer sur une barre (période — année ou trimestre selon le mode actif) affiche un tableau (`BondScheduleDetailTable.razor`) précédé d'un en-tête rappelant la période sélectionnée (clé `BondSchedule_DetailTitle`, ex. "2027" ou "T2 2027"), listant les obligations de cette période (nom, montant) avec une ligne de total. Sur écran large (`MudItem md="7"`/`md="5"`), le tableau apparaît à droite du graphique, côte à côte — le graphique passant de `md="12"` (pleine largeur, aucune période sélectionnée) à `md="7"` dès qu'une période est cliquée. Sur écran étroit (`xs="12"` sur les deux blocs), le tableau reste empilé sous le graphique.

---

## 5. Page de chargement

Affichée pendant les deux phases de démarrage :
1. **Phase hôte** (`index.html` de l'hôte) : téléchargement du runtime Blazor (WASM) ou démarrage du `BlazorWebView` (MAUI)
2. **Phase données** (`Dashboard.razor`) : pendant les appels API parallèles à l'initialisation

Overlay plein écran (`position: fixed`, `z-index: 9999`) — couvre la barre de navigation. Texte "Chargement" animé en typewriter (lettre par lettre, 1s), maintenu 1s, puis réinitialisé — sans écriture inversée. Police 38px semi-bold, couleur `#787774`. Classes CSS : `.loading-screen`, `.loading-text` dans `css/app.css`.

---

## 6. Formatage

| Méthode | Exemple de sortie |
|---|---|
| `value.ToEurAmount()` | `€ 12 345,67` |
| `value.ToEurAmount(hidden: true)` | `*****` |
| `value.ToPercentage()` | `15,50 %` |
| `value.CssRoiClass()` | `"roi-positive"` / `"roi-negative"` / `""` |
| `value.ToSignedPercentage()` | `"+1,23 %"` / `"-0,45 %"` |

---

## 7. Mode confidentialité (masquage des montants)

Entrée du menu « ⋮ » de la barre d'application (icône `Visibility`/`VisibilityOff`) qui masque tous les montants en euros affichés dans l'application (KPIs, tableaux d'actifs/répartition/échéancier, tooltips et axes des graphiques ApexCharts) — utile pour un partage d'écran.

- Masquage : chaîne fixe `*****` à la place du montant formaté.
- État persisté (clé `investissements.hideAmounts`) via `IKeyValueStore` — `localStorage` du navigateur sur le site, stockage sécurisé de l'OS dans l'application MAUI ; retrouvé au rechargement / redémarrage.
- La bascule se trouve dans le menu « ⋮ » de la barre d'application (voir §9).
- Voir `Client.Shared/Docs/CLAUDE.md` §7.6 pour le détail d'implémentation (`IPrivacyModeService`).

---

## 8. Écran de connexion

Tant que l'utilisateur n'est pas authentifié, seul `LoginGate` est affiché (logo, champ mot de passe, bouton de validation) — aucune route du dashboard n'est montée.

- Le mot de passe saisi est testé via `GET /api/auth/verify`, mémorisé via `IKeyValueStore`, puis envoyé automatiquement (`x-dashboard-password`) sur chaque appel à l'Api.
- Session à **expiration glissante d'1 h** : chaque appel réussi la repousse ; après 1 h sans appel réussi, retour à l'écran de connexion.
- Mot de passe refusé : message d'erreur `Login_InvalidPassword` ; bouton désactivé pendant la vérification.
- ⚠️ Anomalie connue côté Api : `auth/verify` répond toujours `200` (voir `Api/Docs/CLAUDE.md` §6).

---

## 9. Barre d'application et menu « ⋮ »

Barre en haut de page : logo + titre, liens **Portefeuille** (`/`) et **Suivi** (`/suivi`), puis le menu « ⋮ » :

| Entrée | Action |
|---|---|
| Masquer / Afficher les montants | Bascule le mode confidentialité (§7) |
| Synchroniser | Relance l'ETL via `POST /api/sync` ; spinner pendant l'appel, puis notification de succès (avec le nombre d'actifs ajoutés s'il est > 0) ou d'erreur |
| Documentation | Ouvre `docs/presentation.pdf` du site (lien absolu construit depuis l'URL de l'Api — nouvel onglet / navigateur externe) |
| Déconnexion | Efface la session et revient à l'écran de connexion |

---

## 10. Hôtes

| Hôte | Particularités fonctionnelles |
|---|---|
| Site (`Client/`) | Stockage de session dans `localStorage` ; Api à la même origine que le site |
| Application Windows (`Maui/`) | Stockage chiffré (`SecureStorage`) ; Api de production (URL fixe) ; voir `Maui/Docs/SPECS.md` |
