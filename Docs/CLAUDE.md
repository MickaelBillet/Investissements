# CLAUDE.md — Investment Dashboard

**Statut :** Brouillon  
**Version :** 1.0  
**Date :** 2026-04-21  

---

## 0. Instructions de collaboration

### 0.1 Gestion des branches et PR
- Toujours créer les modifications sur une **nouvelle branche** dédiée
- Ne jamais merger directement dans `main` — les PR et merges sont effectués **manuellement par l'utilisateur sur GitHub**
- Après un push de branche, fournir le lien GitHub de création de PR

---

## 1. Contexte et objectif

Développer un dashboard web personnel permettant de visualiser un portefeuille d'investissement multi-supports. L'application doit offrir deux niveaux de lecture :

- **Vue instantanée** : état du portefeuille à la date du jour (allocations par support, par type d'actif, par actif, par zone géographique)
- **Vue historique** : évolution dans le temps de ces mêmes indicateurs, constituée progressivement au fil des mises à jour quotidiennes

Les données sont maintenues dans un Google Sheets personnel, mis à jour quotidiennement. Un script automatique consolide chaque jour les données dans une feuille historique. Le dashboard est strictement personnel et privé.

---

## 2. Contraintes

**Techniques :**
- Application web en Blazor WebAssembly (C#)
- Pas de base de données dédiée — Google Sheets joue le rôle de base de données
- Pas de serveur backend traditionnel — Azure Functions en mode serverless

**Financières :**
- Budget zéro (hors nom de domaine déjà possédé)
- Tous les services utilisés doivent être sur leurs tiers gratuits

**Sécurité :**
- Usage strictement personnel et privé
- La clé API Google Sheets ne doit jamais être exposée côté client

**Opérationnelles :**
- Mise à jour des données entièrement automatique (aucune intervention manuelle)
- Le dashboard doit toujours afficher les données les plus récentes disponibles

---

## 3. Architecture technique

### Vue d'ensemble

```
Google Sheets (multi-onglets, style BDD)
       │ Apps Script (ETL quotidien automatique)
       │ Google Sheets API v4 (clé API sécurisée)
       ▼
Azure Functions (C# — liées à Static Web Apps)
       │ API interne sécurisée (pas de clé exposée)
       ▼
Blazor WASM + MudBlazor + ApexCharts
       │ GitHub Actions (CI/CD)
       ▼
Azure Static Web Apps + nom de domaine custom
```

### Stack technique

| Composant | Technologie | Justification |
|---|---|---|
| **Données** | Google Sheets API v4 | Ecosystème déjà en place, gratuit |
| **Automatisation** | Google Apps Script | Intégré à Google Sheets, gratuit |
| **Backend** | Azure Functions (C#) | Serverless, gratuit, clé API sécurisée |
| **Frontend** | Blazor WASM (C#) | Langage maîtrisé par le développeur |
| **UI Components** | MudBlazor | Riche, bien maintenu |
| **Graphiques** | ApexCharts for Blazor | Couvre tous les types de graphiques requis |
| **Hébergement** | Azure Static Web Apps (Free) | Gratuit, intégration native Azure Functions |
| **CI/CD** | GitHub Actions | Intégré avec Azure Static Web Apps |
| **Sécurité** | Static Web Apps + Functions liées | Pas de clé exposée côté client |

---

## 4. Composants et responsabilités

### 4.1 Google Sheets (base de données)
- Source de vérité du portefeuille
- Structuré en plusieurs onglets (style base de données relationnelle)
- Mis à jour manuellement par l'utilisateur au quotidien
- Structure détaillée des onglets à définir avec Claude Code

### 4.2 Google Apps Script (ETL quotidien)
- S'exécute automatiquement chaque jour à heure fixe
- Lit les données du jour depuis les onglets snapshot
- Calcule les agrégats (valeur totale, % par catégorie, etc.)
- Appende une ligne dans les onglets historiques
- Aucune intervention manuelle requise

### 4.3 Azure Functions (backend C#)
- Détient la clé API Google Sheets (stockée dans App Settings)
- Expose des endpoints REST consommés par le Blazor WASM
- Interroge Google Sheets API v4 et retourne les données transformées
- Liées à Azure Static Web Apps (sécurité interne, pas d'exposition publique)

### 4.4 Blazor WASM (frontend C#)
- Consomme les endpoints de l'Azure Function
- Affiche les graphiques et visualisations
- Ne détient aucune clé API ni donnée sensible

### 4.5 GitHub Actions (CI/CD)
- Déclenché automatiquement sur chaque push sur la branche `main`
- Build et déploiement vers Azure Static Web Apps

---

## 5. Sécurité

### 5.1 Protection de la clé API Google Sheets
- La clé API Google Sheets est stockée dans les **Application Settings** de l'Azure Function
- Elle est chiffrée au repos par Azure, accessible uniquement par la Function
- Elle n'apparaît jamais dans le code source ni dans le bundle Blazor WASM
- En cas de rotation de clé, la mise à jour se fait uniquement dans les App Settings sans redéploiement

### 5.2 Protection des endpoints Azure Functions
- Les Azure Functions sont liées à Azure Static Web Apps via le mécanisme de **Managed Functions**
- Elles ne sont pas exposées publiquement sur Internet
- Seul le Blazor WASM hébergé sur le même Static Web Apps peut les appeler
- Aucune clé de fonction (Function Key) nécessaire

### 5.3 Protection des données Google Sheets
- Le Google Sheets est partagé en **lecture seule**
- La clé API ne permet aucune écriture sur la feuille
- Seul l'Apps Script (authentifié via le compte Google propriétaire) peut écrire

### 5.4 Restriction de la clé API Google
- La clé API Google Sheets est restreinte dans la Google Cloud Console
- Elle n'autorise que l'accès à l'API Google Sheets, aucune autre API Google

---

## 6. Structure du Google Sheets

Deux Google Sheets sont utilisés :

| Constante | Rôle |
|---|---|
| `SOURCE_ID` | Feuille personnelle de l'utilisateur — onglet "Bilan" (source des valeurs brutes) |
| `DEST_ID` | Feuille structurée API — onglets "Asset" et "Snapshot" (servie par l'Apps Script) |

### 6.1 Principe général
Le Google Sheets DEST est structuré comme une base de données relationnelle. Chaque onglet représente une table distincte avec un rôle précis.

### 6.2 Onglet `Asset` (DEST_ID)

Une ligne par actif. Colonnes (index 0-based) :

| Index | Colonne | Constante | Description |
|---|---|---|---|
| 0 | A | `COL_ID` | Identifiant |
| 1 | B | `COL_NAME` | Nom de l'actif |
| 2 | C | `COL_ASSET_CLASS` | Classe d'actif (`ASSET_CLASS`) |
| 3 | D | `COL_SUPPORT_TYPE` | Type d'enveloppe (`SUPPORT_TYPE`) |
| 4 | E | `COL_SUPPORT` | Enveloppe / broker (`SUPPORT`) |
| 5 | F | `COL_ASSET_TYPE` | Type d'actif (`ASSET_TYPE`) |
| 6 | G | `COL_INFORMATION` | Informations libres |
| 7 | H | `COL_RISK` | Niveau de risque 0–4 (`RISK`) |
| 8 | I | `COL_TOTAL_PURCHASES` | Total achats en EUR (peut être `"ND"`) |
| 9 | J | `COL_TOTAL_SALES` | Total ventes en EUR |
| 10 | K | `COL_DIVIDENDS` | Dividendes perçus en EUR |
| 11 | L | `COL_CURRENT_TOTAL` | Valeur actuelle en EUR |

Les colonnes I–L sont remplies automatiquement par `syncCurrentTotal()` depuis l'onglet "Bilan" du SOURCE_ID. Les lignes `"Not Defined"` sont ignorées partout.

### 6.3 Onglet `Snapshot` (DEST_ID)

Une ligne par jour. Colonnes (index 0-based) :

| Index | Colonne | Constante | Description |
|---|---|---|---|
| 0 | A | `COL_SNAP_DATE` | Date (yyyy-MM-dd) |
| 1 | B | `COL_SNAP_PORTFOLIO` | Valeur totale du portefeuille (EUR) |
| 2 | C | `COL_SNAP_LIFESTRATEGY` | Référence LifeStrategy 60 |
| 3 | D | `COL_SNAP_MSCI_WORLD` | Référence MSCI World |

### 6.4 Valeur sentinelle `"ND"`

Quand une valeur financière n'est pas disponible, la feuille contient la chaîne `"ND"` (Not Defined) à la place d'un nombre. Les fonctions d'agrégation ignorent ces lignes plutôt que de sommer zéro, et posent `hasIncompleteData: true` sur le résultat agrégé. Les métriques calculées (`unrealizedGain`, `yield`, `roi`) sont retournées `null` quand les données sont incomplètes.

### 6.5 Taxonomie des données

**Classes d'actifs (`ASSET_CLASS`) :**
`Stocks`, `Bonds`, `Cash`, `PrivateDebt`, `RealEstate`, `Commodities`, `Crypto`, `Miscellaneous`

**Types d'actifs (`ASSET_TYPE`) :**
`Stock`, `ETF_Stocks`, `ETF_Bunds`, `Cash_Deposite`, `MarketBonds`, `Savings`, `Direct loans (P2P)`, `SCI_SCPI`, `ETC_ETC_Commodities`, `Crypto`, `UnlistedBonds`, `OPCVM`, `EuroFunds`, `MoneyMarketETF`

**Types d'enveloppes (`SUPPORT_TYPE`) :**
`AccountBank`, `Booklet`, `Platform`, `CTO`, `PEA`, `LifeInsurance`

**Enveloppes / brokers (`SUPPORT`) :**
`CTO TR`, `Livret A`, `LDD`, `Trade Republic`, `PEA TR`, `Spirica`, `Generali`, `PerrBerry`, `Mintos`, `Enerfip`, `BienPrêter`, `Lendosphère`, `Kraken`

---

## 7. Fonctionnalités du dashboard

> ⚠️ **Section à compléter** — Les spécifications fonctionnelles détaillées feront l'objet d'un document dédié `SPECS.md`.

### 7.1 Vue instantanée (snapshot du jour)

| Visualisation | Type de graphique | Données |
|---|---|---|
| Allocation par type de support | Donut chart | % et valeur EUR par support |
| Allocation par type d'actif | Donut chart ou Treemap | % et valeur EUR par type d'actif |
| Allocation par actif | Bar chart horizontal | Top N actifs triés par valeur décroissante |
| Répartition géographique | Bar chart horizontal | % et valeur EUR par zone géographique |
| Allocation par enveloppe fiscale | Donut chart | % et valeur EUR par enveloppe (PEA, CTO, AV, PER, Livrets) |

### 7.2 Vue historique (évolution dans le temps)

| Visualisation | Type de graphique | Données |
|---|---|---|
| Évolution de la valeur totale | Courbe (line chart) | Valeur totale en EUR par date |
| Évolution des allocations par support | Courbe multi-séries | % par support par date |
| Évolution des allocations par type d'actif | Courbe multi-séries | % par type d'actif par date |
| Évolution de la répartition géographique | Courbe multi-séries | % par zone géo par date |

### 7.3 KPIs globaux (en-tête)
- Valeur totale du portefeuille en EUR
- Nombre d'actifs en portefeuille
- Date de dernière mise à jour des données

---

## 8. Déploiement et CI/CD

### 8.1 Repository GitHub
- Un seul repository contenant le projet Blazor WASM et les Azure Functions
- Branche principale : `main`
- Tout push sur `main` déclenche automatiquement le pipeline de déploiement

### 8.2 Structure du repository

```
investment-dashboard/
├── Client/                          # Projet Blazor WASM
│   └── Docs/
│       ├── CLAUDE.md                # Architecture technique du Client
│       └── SPECS.md                 # Spécifications fonctionnelles du Client
├── Api/                             # Azure Functions (C#)
│   └── Docs/
│       ├── CLAUDE.md                # Architecture technique de l'Api
│       └── SPECS.md                 # Spécifications fonctionnelles de l'Api
├── Scripts/                         # Google Apps Script (référence versionnée)
│   └── Docs/
│       ├── CLAUDE.md                # Architecture technique des Scripts
│       └── SPECS.md                 # Spécifications fonctionnelles des Scripts
├── Shared/                          # Modèles partagés Client + Api
├── Docs/                            # Documentation globale du projet
│   ├── CLAUDE.md                    # Architecture globale (ce fichier)
│   └── SPECS.md                     # Spécifications globales
├── .github/
│   └── workflows/
│       └── deploy.yml               # Pipeline GitHub Actions
```

### 8.3 Règle de contexte pour Claude Code

> Quand tu travailles sur un sous-projet, tu lis **uniquement** :
> - `Docs/CLAUDE.md` et `Docs/SPECS.md` (contexte global)
> - `<sous-projet>/Docs/CLAUDE.md` et `<sous-projet>/Docs/SPECS.md` (contexte spécifique)
>
> Tu ne lis pas les fichiers `Docs/` des autres sous-projets.

### 8.3 Pipeline GitHub Actions

```yaml
name: Deploy to Azure Static Web Apps
on:
  push:
    branches: [main]
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Build and deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          app_location: "Client"
          api_location: "Api"
          output_location: "wwwroot"
```

### 8.4 Variables et secrets

| Secret | Stockage | Accessible par |
|---|---|---|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | GitHub Secrets | GitHub Actions uniquement |
| `GOOGLE_SHEETS_API_KEY` | Azure Function App Settings | Azure Function uniquement |
| `GOOGLE_SHEET_ID` | Azure Function App Settings | Azure Function uniquement |

### 8.5 Domaine custom
- Configurer le domaine custom dans Azure Static Web Apps
- Ajouter un enregistrement CNAME chez le registrar pointant vers l'URL Azure Static Web Apps

---

## 9. Coûts

| Service | Plan | Limites gratuites | Coût mensuel |
|---|---|---|---|
| **Azure Static Web Apps** | Free | 100 Go bande passante, 2 environnements | 0 € |
| **Azure Functions** | Incluses dans Static Web Apps Free | 100 000 exécutions/mois | 0 € |
| **Google Sheets API** | Gratuit | 60 requêtes/minute, 500 requêtes/100 secondes | 0 € |
| **Google Apps Script** | Gratuit | 90 min d'exécution/jour | 0 € |
| **GitHub Actions** | Gratuit | 2 000 min/mois (repo privé) | 0 € |
| **Nom de domaine** | — | Déjà possédé | 0 € |
| **Total** | | | **0 €/mois** |

---

## 10. Questions ouvertes et étapes suivantes

### 10.1 Questions ouvertes

| # | Question | Impact |
|---|---|---|
| 1 | ~~Structure détaillée des onglets du Google Sheets~~ — résolu, voir section 6 | — |
| 2 | Nombre d'actifs à afficher dans le top holdings (10, 15, 20 ?) | Fonctionnalité dashboard |
| 3 | Palette de couleurs souhaitée pour les graphiques | UI/UX |
| 4 | ~~Heure d'exécution quotidienne de l'Apps Script~~ — 06h00 (après clôture des marchés européens) | — |
| 5 | Sous-domaine ou racine du domaine custom ? (ex: `dashboard.mondomaine.com`) | Déploiement |

### 10.2 Étapes suivantes

Les composants sont à développer dans cet ordre :

1. **Google Sheets** — ✅ Structure définie (section 6)
2. **Google Apps Script** — ✅ ETL + API REST implémentés (`Scripts/`)
3. **Azure Functions** — Développer les endpoints REST avec Claude Code
4. **Blazor WASM** — Développer le dashboard avec Claude Code
5. **CI/CD** — Configurer GitHub Actions + Azure Static Web Apps
6. **Domaine custom** — Configurer le CNAME

---

## 11. Scripts — Implémentation (référence Claude Code)

### 11.1 Exécution et tests

Les fichiers `.gs` s'exécutent exclusivement dans l'**éditeur Google Apps Script** (script.google.com). Il n'y a pas de commande de build ou de test locale.

- **Exécuter une fonction** : sélectionner la fonction dans le menu déroulant, cliquer Run.
- **Exécuter un test** : sélectionner une fonction `test*` dans `Test.gs`, cliquer Run — résultats dans les Logs (`Ctrl+Entrée`).
- **Déployer en Web App** : Deploy → New deployment → Web App (execute as me, access: anyone).
- **Initialiser le token API** : exécuter `setApiToken()` une fois après chaque nouveau déploiement.
- **Créer le déclencheur quotidien** : exécuter `creerDeclencheurSnapshot()` une fois — enregistre `snapshotQuotidien` à 06h00 chaque jour.

### 11.2 Flux de requête

`doGet(e)` dans `Router.gs` est l'unique point d'entrée HTTP. Chaque requête doit passer un paramètre `apiKey` (token stocké dans Script Properties, jamais dans le code). Le routage se fait sur `?service=X&action=Y` :

```
GET ?apiKey=...&service=AssetClass&action=getAll
         │
    Router.gs → doGet(e)
         ├── AssetClass   → AssetClasseService.gs
         ├── AssetType    → (inline dans Router.gs)
         ├── SupportType  → SupportTypeService.gs
         ├── Support      → SupportService.gs
         ├── Asset        → AssetService.gs
         └── Snapshot     → SnapshotService.gs
```

Les helpers partagés (`getAssetsData`, `getPortfolioTotal`, `buildAssetRow`, `aggregateGroup`, `groupBy`, `sumColumn`) sont tous dans `Router.gs`.

### 11.3 Pattern des services

Chaque fichier service suit le même schéma :

| Fonction | Rôle |
|---|---|
| `handle*(action, params)` | Switch sur `action`, retourne les données ou `{ error: "..." }` |
| `get*All()` | Agrège toutes les lignes groupées par la dimension du service |
| `get*Distribution()` | Vue allégée `{ name, currentTotal, weightInPortfolio }` par groupe |
| `getBy*()` | Drill-down : filtre par valeur de dimension, retourne actifs individuels ou sous-groupes |

### 11.4 Pattern de test (Test.gs)

Les tests simulent des requêtes HTTP en construisant un faux objet `e.parameter` et en appelant `doGet(e)` directement :

```js
function testDoGetAllAssetClass() {
  const e = { parameter: { apiKey: "token-zapto", service: "AssetClass", action: "getAll" } };
  Logger.log(doGet(e).getContent());
}
```

Ajouter une fonction de test pour chaque nouveau service ou action avant de déployer.

### 11.5 Enumerations (Config.gs)

Toutes les valeurs de dimension sont définies comme constantes dans `Config.gs` (`ASSET_CLASS`, `ASSET_TYPE`, `SUPPORT_TYPE`, `SUPPORT`, `RISK`). Toujours utiliser ces constantes — ne jamais coder en dur des chaînes de caractères — pour rester cohérent avec ce qui est stocké dans la feuille.
