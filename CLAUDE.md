# CLAUDE.md — Investment Dashboard

---

## 1. Contexte et objectif

Développer un dashboard web personnel permettant de visualiser un portefeuille d'investissement multi-supports. L'application doit offrir deux niveaux de lecture :

- **Vue instantanée** : état du portefeuille à la date du jour (allocations par support, par type d'actif, par actif, par zone géographique)
- **Vue historique** : évolution dans le temps de ces mêmes indicateurs, constituée progressivement au fil des mises à jour quotidiennes

Les données sont maintenues dans un Google Sheets personnel, mis à jour quotidiennement. Un script automatique consolide chaque jour les données dans une feuille historique. Le dashboard est strictement personnel et privé.

- À 70% de contexte : prévenir et proposer /compact
- Avant /clear : sauvegarder toute décision nouvelle dans le CLAUDE.md concerné
- Après /compact : relire CLAUDE.md + sous-projet CLAUDE.md avant de continuer
- Ne jamais supposer le contenu d'un autre sous-projet sans lire son CLAUDE.md


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
- Les identifiants du compte de service Google (email + clé privée) ne doivent jamais être exposés côté client

**Opérationnelles :**
- Mise à jour des données entièrement automatique (aucune intervention manuelle)
- Le dashboard doit toujours afficher les données les plus récentes disponibles

---

## 3. Architecture technique

### Vue d'ensemble

```
Google Sheets (multi-onglets, style BDD)
       ▲                              │
       │ Apps Script (ETL quotidien)  │ API Google Sheets (compte de service)
       │ écrit chaque jour à 06h00    │ lue directement, pas d'intermédiaire
       │                              ▼
       └──────────────────  Azure Functions (C# — liées à Static Web Apps)
                                       │ API interne sécurisée (pas de token exposé)
                                       ▼
                              Blazor WASM + MudBlazor + ApexCharts
                                       │ GitHub Actions (CI/CD)
                                       ▼
                              Azure Static Web Apps + nom de domaine custom
```

Apps Script n'expose plus de Web App HTTP — il ne fait plus qu'écrire (ETL quotidien + rapport hebdomadaire par email). L'Api lit le Sheet directement via l'API Google Sheets officielle (`Google.Apis.Sheets.v4`, compte de service), dans un projet dédié `GoogleSheets/`.

### Stack technique

| Composant | Technologie | Justification |
|---|---|---|
| **Données** | Google Sheets | Ecosystème déjà en place, gratuit |
| **Lecture données** | API Google Sheets (compte de service) | Lecture directe, rapide, sans cold start — remplace l'ancien Web App Apps Script |
| **ETL** | Google Apps Script (triggers temporels) | Écrit dans le Sheet chaque jour à 06h00, gratuit |
| **Backend** | Azure Functions (C#) | Serverless, gratuit, lit le Sheet via `GoogleSheets/` (projet dédié) |
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

### 4.2 Google Apps Script (ETL quotidien + rapport hebdomadaire)
- S'exécute automatiquement chaque jour à 06h00 (ETL) et chaque lundi à 08h00 (rapport email)
- Lit les données du jour depuis les onglets snapshot
- Calcule les agrégats (valeur totale, % par catégorie, etc.)
- Appende une ligne dans les onglets historiques
- Crée automatiquement dans l'onglet `Asset` toute nouvelle ligne trouvée dans le `Bilan` mais absente du sheet DEST (dès que sa valeur actuelle est strictement positive) — les colonnes de classification (AssetClass, SupportType, Support, AssetType, Sector, Geography) sont posées à `"Not Defined"` et un email d'alerte est envoyé pour rappeler de les compléter manuellement
- Expose un unique Web App HTTP minimal et protégé par clé secrète (`Scripts/SyncWebApp.gs`), dédié au déclenchement manuel de `snapshotQuotidien()` depuis le bouton "Synchroniser" du dashboard — voir §5.2.5. Aucun autre point d'entrée HTTP.

### 4.3 Azure Functions (backend C#)
- Détient les identifiants du compte de service Google (email + clé privée, stockés dans App Settings)
- Expose des endpoints REST consommés par le Blazor WASM
- Lit le Google Sheet directement via l'API Sheets officielle (projet `GoogleSheets/`) et construit les DTOs C#
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

### 5.1 Règles Générales

Voir SECURITY.MD

### 5.2 Règles particulières

#### 5.2.1 Protection des identifiants Google
- L'ID du Sheet et les identifiants du compte de service (`GOOGLE_SHEET_ID`, `GOOGLE_SERVICE_ACCOUNT_EMAIL`, `GOOGLE_SERVICE_ACCOUNT_KEY`) sont stockés dans les **Application Settings** de l'Azure Function
- Ils sont chiffrés au repos par Azure, accessibles uniquement par la Function
- Ils n'apparaissent jamais dans le code source ni dans le bundle Blazor WASM
- Le compte de service n'a qu'un accès **Lecteur** sur le Sheet (partage explicite, pas d'accès projet GCP plus large)
- En cas de rotation, la mise à jour se fait uniquement dans les App Settings sans redéploiement

#### 5.2.2 Protection des endpoints Azure Functions
- Les Azure Functions sont liées à Azure Static Web Apps via le mécanisme de **Managed Functions**
- Elles ne sont pas exposées publiquement sur Internet
- Seul le Blazor WASM hébergé sur le même Static Web Apps peut les appeler
- Aucune clé de fonction (Function Key) nécessaire

#### 5.2.3 Protection des données Google Sheets
- Lecture : l'Azure Function lit directement le Sheet `DEST_ID` via l'API Google Sheets, authentifiée par un compte de service dédié avec accès **Lecteur uniquement** (partagé explicitement sur ce Sheet, pas d'accès projet GCP plus large)
- Écriture : seul l'Apps Script (authentifié via le compte Google propriétaire) écrit sur les feuilles — ETL quotidien, rapport hebdomadaire, et synchro manuelle déclenchée depuis le dashboard (voir §5.2.5)
- Le compte de service Azure ne peut donc jamais modifier les données, uniquement les lire

#### 5.2.5 Synchronisation manuelle (bouton dashboard)

Le bouton "Synchroniser" du Client déclenche `snapshotQuotidien()` (le même ETL complet que le trigger quotidien de 06h00 — sync des actifs *et* recalcul de la ligne `Snapshot` du jour) sans attendre ce trigger. Comme l'Api ne peut pas écrire (§5.2.3), l'écriture reste effectuée par Apps Script, exposé cette fois via un Web App HTTP minimal et dédié à cette seule action (`Scripts/SyncWebApp.gs`, `doGet`) — réintroduction ciblée du mécanisme retiré en §4.2, motivée par ce nouveau besoin.

- Protection par clé partagée : `doGet` compare `?key=` à la Script Property `SYNC_SECRET_KEY` (jamais commitée dans `Scripts/`, contrairement à `Config.gs`) — pas de protection Google (déploiement "Anyone")
- Flux : `Client` → `POST /api/sync` (protégé par `DashboardAuthMiddleware`, mot de passe dashboard) → `SyncFunction`/`SyncService` (Api) → `GET .../exec?key=...` (Apps Script) → `snapshotQuotidien()`
- Le recalcul immédiat de la ligne `Snapshot` du jour met à jour le KPI "Capital net engagé" sans attendre le lendemain 06h00
- Le compte de service Azure reste lecteur uniquement — aucun changement de son périmètre ; seul Apps Script écrit, comme avant
- Config Api (App Settings, jamais commitées) : `APPS_SCRIPT_SYNC_URL`, `APPS_SCRIPT_SYNC_KEY` — voir `Api/Docs/CLAUDE.md` §5

#### 5.2.4 Authentification du dashboard (accès restreint au propriétaire)

Le dashboard entier est protégé par un **mot de passe unique géré par notre propre code** — pas par l'authentification d'Azure Static Web Apps (voir historique ci-dessous).

- `DashboardAuthMiddleware` (`Api/Middleware/`) vérifie le header `x-dashboard-password` sur toutes les requêtes HTTP de l'Api, comparé à l'App Setting `DASHBOARD_PASSWORD` — sauf `/api/mcp` (protégée par sa propre clé `MCP_API_KEY`) et `/api/auth/verify` (doit rester accessible pour que l'écran de connexion puisse tester un mot de passe)
- Fail-safe : si `DASHBOARD_PASSWORD` n'est pas configuré, l'accès est refusé par défaut (jamais d'accès ouvert par omission)
- Côté Client, `App.razor` affiche un écran de connexion (`Client/Shared/LoginGate.razor`) tant que `ISessionService.IsAuthenticated` est faux — le mot de passe saisi est mémorisé dans le `localStorage` du navigateur et renvoyé automatiquement sur chaque appel Api (`DashboardPasswordHandler`)
- Le bouton de masquage des montants (barre de menu) reste un confort indépendant (ex. partage d'écran par le propriétaire une fois connecté), pas une frontière de sécurité

**Historique — pourquoi pas l'authentification native Azure Static Web Apps.** Une première version utilisait les rôles personnalisés d'Azure Static Web Apps (Microsoft Entra ID + rôle `owner`, via `rolesSource` puis via Invitations). Les deux mécanismes se sont révélés peu fiables en pratique sur le plan **Free** (contrainte budget zéro, §2) : `rolesSource` nécessite l'authentification personnalisée, réservée au plan Standard, et le mécanisme d'Invitations a montré un comportement incohérent (rôle attribué puis perdu entre sessions, 403 sur des ressources statiques). Le mot de passe interne ci-dessus, entièrement sous notre contrôle, remplace ces deux approches.

---

## 6. Structure du Google Sheets

Deux Google Sheets sont utilisés :

| Constante | Rôle |
|---|---|
| `SOURCE_ID` | Feuille personnelle de l'utilisateur — onglet "Bilan" (source des valeurs brutes) |
| `DEST_ID` | Feuille structurée API — onglets "Asset" et "Snapshot" (écrite par l'Apps Script, lue directement par l'Api via l'API Google Sheets) |

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
| 6 | G | `COL_SECTOR` | Secteur économique (valeur libre) |
| 7 | H | `COL_INFORMATION` | Informations libres |
| 8 | I | `COL_GEOGRAPHY` | Zone géographique (valeur libre) |
| 9 | J | `COL_RISK` | Niveau de risque 0–4 (`RISK`) |
| 10 | K | `COL_TOTAL_PURCHASES` | Total achats en EUR (peut être `"ND"`) |
| 11 | L | `COL_TOTAL_SALES` | Total ventes en EUR |
| 12 | M | `COL_DIVIDENDS` | Dividendes perçus en EUR |
| 13 | N | `COL_CURRENT_TOTAL` | Valeur actuelle en EUR |

Les colonnes K–N sont remplies automatiquement par `syncCurrentTotal()` depuis l'onglet "Bilan" du SOURCE_ID. Les lignes `"Not Defined"` sont ignorées partout.

### 6.3 Onglet `Snapshot` (DEST_ID)

Une ligne par jour. Colonnes (index 0-based) :

| Index | Colonne | Constante | Description |
|---|---|---|---|
| 0 | A | `COL_SNAP_DATE` | Date (yyyy-MM-dd) |
| 1 | B | `COL_SNAP_NET_CAPITAL` | Capital net réellement engagé — cellule `NET_PURCHASES` (C48 du Bilan), EUR |
| 2 | C | `COL_SNAP_LIFESTRATEGY` | Prix unitaire LifeStrategy 40 (EUR) |
| 3 | D | `COL_SNAP_MSCI_WORLD` | Prix unitaire MSCI World (EUR) |
| 4 | E | `COL_SNAP_TOTAL_PURCHASES` | Total des achats depuis l'origine (EUR), lu depuis le Bilan |
| 5 | F | `COL_SNAP_TOTAL_RETURNS` | Plus-values réalisées et latentes depuis l'origine (EUR), lu depuis le Bilan — les cours d'actions sont saisis à la main chaque jour |
| 6 | G | `COL_SNAP_TOTAL_SALES` | Total des ventes depuis l'origine (EUR), lu depuis le Bilan |

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

> Spécifications fonctionnelles détaillées dans `Docs/SPECS.md`.


---

## 8. Déploiement et CI/CD

### 8.1 Repository GitHub
- Un seul repository contenant le projet Blazor WASM et les Azure Functions
- Branche principale : `main`
- Tout push sur `main` déclenche automatiquement le pipeline de déploiement

### 8.2 Structure du repository

```
investment-dashboard/
├── CLAUDE.md                        # Architecture globale (ce fichier)
├── Client/                          # Hôte Blazor WASM mince (Program.cs, index.html, staticwebapp.config.json)
│   └── Docs/
│       ├── CLAUDE.md                # Architecture technique du Client
│       └── SPECS.md                 # Spécifications fonctionnelles du Client
├── Client.Shared/                   # Bibliothèque Razor partagée (App, Views, composants, ViewModels, Services) — WASM + MAUI
├── Maui/                            # Hôte MAUI Windows (BlazorWebView) — Investissements.Maui.slnx
├── Maui.Tests/                      # Tests xUnit du projet MAUI
├── Api/                             # Azure Functions (C#)
│   └── Docs/
│       ├── CLAUDE.md                # Architecture technique de l'Api
│       └── SPECS.md                 # Spécifications fonctionnelles de l'Api
├── Scripts/                         # Google Apps Script (référence versionnée) — ETL + rapport hebdo uniquement
│   └── Docs/
│       ├── CLAUDE.md                # Architecture technique des Scripts
│       └── SPECS.md                 # Spécifications fonctionnelles des Scripts
├── GoogleSheets/                    # Client API Google Sheets (C#, utilisé par l'Api)
├── Shared/                          # Modèles partagés Client + Api
├── Docs/                            # Documentation globale du projet
│   └── SPECS.md                     # Spécifications globales
├── .github/
│   └── workflows/
│       └── deploy.yml               # Pipeline GitHub Actions
```

### 8.3 Règle de contexte pour Claude Code

> Quand tu travailles sur un sous-projet, tu lis **uniquement** :
> - `CLAUDE.md` et `Docs/SPECS.md` (contexte global)
> - `<sous-projet>/Docs/CLAUDE.md` et `<sous-projet>/Docs/SPECS.md` (contexte spécifique)
>
> Tu ne lis pas les fichiers `Docs/` des autres sous-projets.

### 8.3 Pipeline GitHub Actions

```yaml
name: Azure Static Web Apps CI/CD
on:
  push:
    branches: [main]
jobs:
  build_and_deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'
      - name: Restore
        run: dotnet restore
      - name: Test
        run: dotnet test Api.Tests/InvestissementsDashboard.Api.Tests.csproj --no-restore
      - name: Publish Client
        run: dotnet publish Client/InvestissementsDashboard.Client.csproj -c Release -o Client/publish
      - name: Deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN_WHITE_CLIFF_055F3F803 }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          skip_app_build: true
          app_location: 'Client/publish/wwwroot'
          output_location: ''
          api_location: 'Api'
```

> Le Client (net10.0) est pré-compilé par le runner CI car .NET 10 n'est pas disponible dans Oryx.
> L'Api (net8.0) est passée en source à Azure — Oryx la construit (net8.0 est supporté par les managed functions SWA).

### 8.4 Variables et secrets

| Secret | Stockage | Accessible par |
|---|---|---|
| `AZURE_STATIC_WEB_APPS_API_TOKEN_WHITE_CLIFF_055F3F803` | GitHub Secrets | GitHub Actions uniquement |
| `GOOGLE_SHEET_ID` | Azure Function App Settings | Azure Function uniquement |
| `GOOGLE_SERVICE_ACCOUNT_EMAIL` | Azure Function App Settings | Azure Function uniquement |
| `GOOGLE_SERVICE_ACCOUNT_KEY` | Azure Function App Settings | Azure Function uniquement |

### 8.5 Domaine custom

- **Sous-domaine choisi** : `invest.zapto.fr`
- **Registrar** : Ionos
- **Enregistrement DNS à créer** :

| Type  | Hôte     | Valeur cible                    |
|-------|----------|---------------------------------|
| CNAME | `invest` | `<nom-app>.azurestaticapps.net` |

- Le certificat TLS/SSL est provisionné automatiquement par Azure (Let's Encrypt) après validation du CNAME.
- Validation dans Azure Portal : Static Web Apps → Custom domains → Add → coller `invest.zapto.fr` → valider après propagation DNS (< 30 min).

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
| 3 | ~~Palette de couleurs souhaitée pour les graphiques~~ — définie dans `Client/Docs/Claude.md` section 6 | — |
| 4 | ~~Heure d'exécution quotidienne de l'Apps Script~~ — 06h00 (après clôture des marchés européens) | — |
| 5 | ~~Sous-domaine ou racine du domaine custom ?~~ — résolu : `invest.zapto.fr` (sous-domaine, Ionos) | — |

### 10.2 Étapes suivantes

Les composants sont à développer dans cet ordre :

1. **Google Sheets** — ✅ Structure définie (section 6)
2. **Google Apps Script** — ✅ ETL quotidien + rapport hebdomadaire implémentés (`Scripts/`) — plus d'API REST, voir point 3
3. **Azure Functions** — ✅ Endpoints REST implémentés (`Api/`) — lecture directe du Sheet via l'API Google Sheets (`GoogleSheets/`)
4. **Blazor WASM** — ✅ Dashboard implémenté (`Client/`)
5. **CI/CD** — ✅ Pipeline opérationnel
6. **Domaine custom** — ✅ Décision prise : `invest.zapto.fr` — CNAME à créer chez Ionos, validation dans Azure Portal

---

## 11. Scripts — Implémentation (référence Claude Code)

### 11.1 Exécution et tests

Les fichiers `.gs` s'exécutent exclusivement dans l'**éditeur Google Apps Script** (script.google.com). Il n'y a pas de commande de build ou de test locale, et plus de Web App à déployer — ces scripts ne sont plus jamais appelés depuis l'extérieur (l'Api lit le Sheet directement via l'API Google Sheets, voir `Api/Docs/CLAUDE.md`).

- **Exécuter une fonction** : sélectionner la fonction dans le menu déroulant, cliquer Run.
- **Exécuter un test** : sélectionner une fonction `test*` dans `Test.gs`, cliquer Run — résultats dans les Logs (`Ctrl+Entrée`).
- **Créer le déclencheur quotidien** : exécuter `creerDeclencheurSnapshot()` une fois — enregistre `snapshotQuotidien` à 06h00 chaque jour.
- **Créer le déclencheur hebdomadaire** : exécuter `creerDeclencheurHebdomadaire()` une fois — enregistre `rapportHebdomadaire` chaque lundi à 08h00.

### 11.2 Ce qui reste dans les `.gs`

Deux responsabilités, toutes deux déclenchées par des triggers temporels :

- **ETL quotidien** (`SnapshotService.gs`, `SyncData.gs`, `StockValueService.gs`) — écrit dans le Sheet, ne renvoie rien à personne.
- **Rapport hebdomadaire** (`WeeklyReportService.gs`) — réutilise en interne (appel de fonction direct, pas HTTP) quatre handlers réduits à leur seule action utile : `handleSnapshot("getHistory", {})`, `handleAssetClass("getDistribution", {})`, `handleSupportType("getDistribution", {})`, `handleAsset("getDistributionByRisk", {})`.

Les helpers partagés (`getAssetsData`, `getPortfolioTotal`, `aggregateGroup`, `getReferenceIds`, `groupBy`, `sumColumn`) sont dans `Router.gs`.

### 11.3 Pattern de test (Test.gs)

Les tests appellent les handlers directement (plus de simulation HTTP via `doGet`) :

```js
function testAssetClassGetDistribution() {
  Logger.log(JSON.stringify(handleAssetClass("getDistribution", {})));
}
```

Ajouter une fonction de test pour toute nouvelle action ajoutée à un handler.

### 11.4 Enumerations (Config.gs)

Toutes les valeurs de dimension sont définies comme constantes dans `Config.gs` (`ASSET_CLASS`, `ASSET_TYPE`, `SUPPORT_TYPE`, `SUPPORT`, `RISK`). Toujours utiliser ces constantes — ne jamais coder en dur des chaînes de caractères — pour rester cohérent avec ce qui est stocké dans la feuille.

---

## 12. Conventions de test — Règle absolue

**Pour chaque nouvelle fonctionnalité, modification de comportement ou correction d'anomalie :**

1. Mettre à jour ou ajouter les tests couvrant le changement
2. Exécuter la suite complète et vérifier que tout est au vert

Cette règle s'applique sans exception, quelle que soit la taille de la modification.

### 12.1 Api / Azure Functions

- Framework : **xUnit** + **Moq**
- Projet : `Api.Tests/`
- Commande : `dotnet test "Api.Tests/InvestissementsDashboard.Api.Tests.csproj"`
- Pattern de nommage : `[MethodName]_[Scenario]_[ExpectedResult]`
- Chaque nouveau service ou endpoint → tests unitaires sur `AssetsService`, `SnapshotService`, etc.

### 12.2 Client / Blazor WASM

- Framework : **bunit** + **Moq**
- Projet : `Client.Tests/` (non inclus dans la solution — lancer directement)
- Commande : `dotnet test "Client.Tests/InvestissementsDashboard.Client.Tests.csproj"`
- `DashboardViewModel` → tests dans `ViewModels/DashboardViewModelTests.cs`
- Composants Razor → tests dans `Components/`
- Helpers de test centralisés dans `Helpers/TestData.cs`

### 12.3 Scripts / Apps Script

- Pas de framework de test automatisé — les tests sont des fonctions `test*` dans `Test.gs`
- Ajouter une fonction de test pour chaque nouveau service ou action
- Exécuter manuellement dans l'éditeur Apps Script avant tout déploiement

---

## 13. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.

---

## 14. Agents IA et application MAUI Windows (décisions — projet en préparation)

> **État d'avancement (branche `feat/maui-shared-razor`)** : la bibliothèque Razor partagée `Client.Shared/` est extraite, le `Client/` est un hôte WASM mince, et un hôte `Maui/` (BlazorWebView, non packagé) reprend le dashboard à l'identique. Seams : `IKeyValueStore` (localStorage côté WASM, `SecureStorage` côté MAUI) et `AddInvestissementsClient(apiBaseUri)` (enregistrement DI commun). Deux solutions : `Investissements.slnx` (CI, sans MAUI) et `Investissements.Maui.slnx`. Le workflow CI cible explicitement `Investissements.slnx`. Les agents (Bibliothèque `Agents`, page Agents) restent à faire. Compatibilité MudBlazor/ApexCharts dans le BlazorWebView : à valider visuellement.

> Rien de cette section n'est encore implémenté dans cette solution. Les agents existent aujourd'hui dans une console séparée, `C:\Users\mbillet.NOVACATH\source\AgentAI` (net10.0, Microsoft Agent Framework + Azure AI Foundry) ; son `CLAUDE.md` détaille les agents (Chat, Weather, Stock, Portfolio, News). Constats sur la doc Microsoft consultés le 2026-10-01.

### 14.1 Décision : les agents ne vont ni dans le Blazor WASM ni dans l'Api SWA

- **Proxy SWA : 45 s maximum par requête API** (tous backends). Mesure en local : l'agent `Stock` (« action », sans outil, génération pure) ≈ **39,5 s** — marge insuffisante en production (démarrage à froid, latence réseau, historique qui grossit).
- **Plan Free = fonctions managées uniquement** : déclencheurs HTTP seuls, pas de managed identity, pas de Key Vault, pas de Durable Functions. Une Function App séparée liée à la SWA exige le plan **Standard** (payant) → exclue par le principe « budget zéro » (§2). Une Function App séparée appelée directement a été écartée pour la même raison.
- **Navigateur :** Foundry exige une identité Entra (aucun secret n'est acceptable côté client), les flux RSS n'envoient très probablement pas de CORS (non vérifié), pas de système de fichiers persistant.
- **Auth native SWA :** écartée, voir §5.2.4 (fiabilité insuffisante sur le plan Free).
- **Pas de streaming, pas de contournement par SSE :** le proxy bufferise la réponse entière et la limite de 45 s porte sur la durée **totale**. Au-delà, le client reçoit une erreur 500 alors que la fonction continue de tourner (sources : fils Q&R Microsoft ; résumé de recherche web, pas relu en détail dans la documentation officielle). Des sources suggèrent qu'une Function App liée lèverait la limite, mais la page officielle « API overview » dit qu'elle s'applique à **tous** les backends — non testé, et de toute façon plan Standard (payant). Piste « lancer, laisser couper, relire le résultat » écartée : stockage partagé requis et rien ne garantit qu'une instance serverless termine un travail commencé après la réponse.
- **Limite de version .NET :** l'Api cible `net9.0` (le §8.3 dit net8.0, voir §14.5), la liste officielle des runtimes des fonctions managées s'arrête à .NET 8 (peut être obsolète), et `AgentAI` est en `net10.0`. Faire tourner les agents dans l'Api exigerait de lever cette limite de version ; MAUI Windows cible .NET 10 sans contrainte.
- **Conclusion :** même sans la limite de 45 s, les agents seraient difficiles à faire tourner dans un Blazor WASM (identité Entra, CORS, pas de fichiers, pas de secret côté client).

**Décision retenue :** une application **MAUI Windows**, strictement personnelle (PC du propriétaire), exécute les agents **dans son processus**. Plus de limite de 45 s, plus de CORS, aucun hébergement. Le **Blazor WASM reste inchangé, sans agents.**

### 14.2 Architecture cible

- **Bibliothèque `Agents`** (extraite de `AgentAI`, net10.0) : `AgentFactory`, fournisseurs de contexte, `RssNewsService`, `InvestZaptoMcpClient`, `ForceContentLengthHandler`, historique. La console `AgentAI` est conservée comme outil de développement.
- **Bibliothèque Razor partagée** extraite de `Client/` (composants, ViewModels) pour réutiliser le dashboard dans MAUI via `BlazorWebView` (Blazor Hybrid) ; le projet WASM devient un hôte mince. Compatibilité de MudBlazor et d'ApexCharts dans un `BlazorWebView` : **non vérifiée**, à tester tôt.
- **Projet MAUI Windows** : `BlazorWebView` + page Agents (choix de l'agent, saisies, réponse en Markdown, durée affichée).
- **Portfolio** : l'agent appelle le MCP existant (`POST /api/mcp`, protégé par `MCP_API_KEY`) via le `HttpClient` injecté. `ForceContentLengthHandler` est à conserver : le « 500 Backend call failure » sur les POST en chunked vient probablement du proxy SWA (hypothèse, non vérifiée).

### 14.3 Adaptations du code agents à prévoir (non faites)

- Instructions `.md` : ressources **embarquées** (et non `AppContext.BaseDirectory`).
- Historique des conversations : chemin **injecté**, dans `LocalApplicationData` (le dossier de l'exécutable est en lecture seule pour une application packagée).
- Configuration (`FOUNDRY_PROJECT_ENDPOINT`, `FOUNDRY_MODEL`, `INVESTZAPTO_MCP_URL`) : page de paramètres, avec repli sur les variables d'environnement.
- Identité Azure : `DefaultAzureCredential` (`az login`) sur le PC pour commencer ; option ultérieure `InteractiveBrowserCredential` avec cache de jeton (non vérifié en MAUI packagé). **Jamais de secret de principal de service dans l'application** (un binaire se décompile). `ExcludeManagedIdentityCredential` reste actif.
- Secrets de l'application (`MCP_API_KEY`, mot de passe du dashboard) : `SecureStorage`, jamais dans le code.
- `HttpClient` **injecté** dans `InvestZaptoMcpClient` (l'hôte choisit ses handlers : `SocketsHttpHandler` sous Windows).

### 14.4 Plan par étapes (une validation avant chaque suivante)

1. Extraire la bibliothèque `Agents` depuis `AgentAI` (comportement identique ; la console doit continuer à fonctionner) + tests (§12). **Peut démarrer dans le dossier `AgentAI`** sans trancher l'emplacement définitif (question 1 du §14.6) : la bibliothèque pourra être déplacée ou référencée ensuite.
2. **Spike MAUI Windows minimal, sans Blazor** : un bouton qui lance l'agent `news` de bout en bout. Objectif : valider tôt, à faible coût, l'identité Azure, les chemins de fichiers, les variables d'environnement et les durées dans MAUI (voir §14.5), avant de toucher au site.
3. Extraire la bibliothèque Razor partagée depuis `Client/` — **étape la plus risquée : elle touche le site en production** (s'appuyer sur `Client.Tests` et le pipeline).
4. Projet MAUI Windows : `BlazorWebView`, injection de dépendances, paramètres.
5. Page Agents.
6. Agent `Portfolio` branché sur l'Api / le MCP existants.

### 14.5 Pièges à ne pas oublier

- **CI :** le workflow fait `dotnet restore` **sans argument** à la racine, et `Investissements.slnx` existe. Ajouter un projet MAUI Windows à ce `.slnx` risque de **casser la restauration sur le runner Ubuntu** (non testé). Soit une solution séparée pour MAUI, soit des projets explicites dans le workflow. **Vérifier avant d'ajouter quoi que ce soit au `.slnx`.**
- **Incohérence de version .NET :** la note du §8.3 dit « Api net8.0 » (build Oryx des managed functions), mais `Api/InvestissementsDashboard.Api.csproj` cible actuellement **net9.0**. La liste officielle des runtimes des fonctions managées s'arrête à .NET 8 (peut être obsolète). Vérifier que le déploiement actuel est cohérent avant d'y toucher.
- **Tests (§12) :** la bibliothèque `Agents` et le projet MAUI sont soumis à la règle (xUnit + Moq ; bunit pour les composants). La décision « pas de projet de test » prise pour la console `AgentAI` ne s'applique pas ici.
- **Hypothèse de travail à valider par le spike :** ce qui tourne dans la console `AgentAI` devrait tourner dans MAUI Windows (même .NET 10, mêmes packages, `HttpClient` et `SocketsHttpHandler` identiques). Les différences attendues portent sur l'environnement, pas sur la logique :
  - chemins de fichiers (instructions, historique) — déjà prévu en §14.3 ;
  - identité Azure : `DefaultAzureCredential` s'appuie sur `az login` en lançant un processus externe ; **non vérifié** qu'une application **packagée** (MSIX) le permette — essayer aussi un MAUI Windows **non packagé** ;
  - variables d'environnement : une application lancée depuis le menu Démarrer ne voit pas forcément celles d'un terminal — d'où la page de paramètres.
- **Coût :** les agents consomment des **tokens Azure AI Foundry** (facturés à l'usage), ce qui sort du principe « budget zéro » du §2 — à assumer explicitement, à confirmer.

### 14.6 Questions ouvertes

| # | Question | Impact |
|---|---|---|
| 1 | Emplacement du MAUI et de `Agents` : solution séparée (recommandé, ne touche pas au CI) ou dans `Investissements.slnx` avec ajustement du workflow | CI de production — ne bloque pas l'étape 1 |
| 2 | Identité Azure : `DefaultAzureCredential` (`az login`) ou connexion par navigateur avec cache de jeton | Ergonomie de démarrage |
| 3 | Durées de `news`, `weather`, `portfolio` non mesurées (chronomètre `[Timing]` disponible dans la console `AgentAI`) | Information seulement : plus de limite de 45 s côté MAUI |
