# CLAUDE.md — Investment Dashboard

**Statut :** Brouillon  
**Version :** 1.0  
**Date :** 2026-04-21  

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

> ⚠️ **Section à compléter avec Claude Code**
> La structure détaillée des onglets (noms de colonnes, types de données, relations entre onglets) sera définie lors de la phase de développement, en analysant le Google Sheets existant de l'utilisateur.

### 6.1 Principe général
Le Google Sheets est structuré comme une base de données relationnelle. Chaque onglet représente une table distincte avec un rôle précis.

### 6.2 Onglets attendus (à confirmer)

| Onglet | Rôle | Type |
|---|---|---|
| `Positions` | État du portefeuille du jour, une ligne par actif | Snapshot |
| `Historique_Portfolio` | Valeur totale du portefeuille jour par jour | Série temporelle |
| `Historique_Allocations` | Répartition par catégorie jour par jour | Série temporelle |
| `Referentiel_Actifs` | Métadonnées fixes par actif (nom, type, zone géo...) | Référentiel |
| `Referentiel_Types` | Listes de valeurs (types de supports, zones géo...) | Référentiel |

### 6.3 Taxonomie des données

**Types de supports :**
Actions, Obligations, ETF, Immobilier (SCPI, crowdlending), Crypto, Fonds (assurance-vie, PER), Livrets

**Zones géographiques :**
Europe, Amérique du Nord, Asie-Pacifique, Marchés émergents, Monde, France, Autre

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
| 1 | Structure détaillée des onglets du Google Sheets (noms de colonnes, types de données) | Parsing dans l'Azure Function |
| 2 | Nombre d'actifs à afficher dans le top holdings (10, 15, 20 ?) | Fonctionnalité dashboard |
| 3 | Palette de couleurs souhaitée pour les graphiques | UI/UX |
| 4 | Heure d'exécution quotidienne de l'Apps Script | Automatisation |
| 5 | Sous-domaine ou racine du domaine custom ? (ex: `dashboard.mondomaine.com`) | Déploiement |

### 10.2 Étapes suivantes

Les composants sont à développer dans cet ordre :

1. **Google Sheets** — Définir et structurer les onglets avec Claude Code
2. **Google Apps Script** — Développer l'ETL quotidien avec Claude Code
3. **Azure Functions** — Développer les endpoints REST avec Claude Code
4. **Blazor WASM** — Développer le dashboard avec Claude Code
5. **CI/CD** — Configurer GitHub Actions + Azure Static Web Apps
6. **Domaine custom** — Configurer le CNAME
