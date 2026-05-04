# SPECS.md — Scripts Google Apps Script

**Statut :** En cours  
**Version :** 1.0  
**Date :** 2026-05-04  

---

## 1. Vue d'ensemble

L'Apps Script expose une API REST en lecture seule via un déploiement Web App Google.  
Toutes les requêtes passent par la fonction `doGet(e)` dans `Router.gs`.

**URL de base**
```
https://script.google.com/macros/s/{DEPLOYMENT_ID}/exec
```

**Authentification**  
Chaque requête doit inclure le paramètre `apiKey`. Le token est stocké dans les Script Properties (jamais dans le code source).

**Format de requête**
```
GET {URL_BASE}?apiKey={TOKEN}&service={SERVICE}&action={ACTION}[&param=valeur]
```

**Format de réponse**  
JSON. En cas d'erreur, la réponse contient un champ `error` :
```json
{ "error": "message d'erreur" }
```

---

## 2. Objets de référence

### 2.1 Objet `Aggregate`

Retourné par les actions `getAll` et `getBy*` au niveau groupe.

```json
{
  "name": "string",
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

| Champ | Type | Description |
|---|---|---|
| `name` | string | Nom du groupe |
| `totalPurchases` | number | Total des achats en EUR |
| `totalSales` | number | Total des ventes en EUR |
| `dividends` | number | Dividendes perçus en EUR |
| `currentTotal` | number | Valeur actuelle en EUR |
| `hasIncompleteData` | boolean | `true` si au moins un actif du groupe a des données `"ND"` |
| `unrealizedGain` | number \| null | Plus-value latente en EUR (`null` si données incomplètes) |
| `yield` | number \| null | Rendement en % sur le capital net investi (`null` si données incomplètes) |
| `roi` | number \| null | Retour sur investissement en % (`null` si données incomplètes) |
| `weightInGroup` | number | Poids en % dans le groupe parent |
| `weightInPortfolio` | number | Poids en % dans le portefeuille total |

### 2.2 Objet `Distribution`

Retourné par les actions `getDistribution`.

```json
{
  "name": "string",
  "currentTotal": 14200.00,
  "weightInPortfolio": 18.30
}
```

### 2.3 Objet `Asset`

Retourné par les actions qui descendent au niveau de l'actif individuel.

```json
{
  "id": 1,
  "name": "MSCI World ETF",
  "assetClass": "Stocks",
  "supportType": "PEA",
  "support": "PEA TR",
  "assetType": "ETF_Stocks",
  "information": "",
  "risk": 4,
  "totalPurchases": 5000.00,
  "totalSales": 0.00,
  "dividends": 0.00,
  "currentTotal": 6200.00,
  "unrealizedGain": 1200.00,
  "yield": 0.00,
  "roi": 24.00,
  "weightInPortfolio": 8.10,
  "weightInGroup": 22.50
}
```

> `weightInGroup` est présent uniquement quand l'actif est retourné dans un contexte de groupe (`getBySupport`, `getByAssetType`, `getByRisk`).  
> Les champs financiers (`totalPurchases`, `totalSales`, etc.) sont `null` quand la valeur est `"ND"` dans la feuille.

### 2.4 Objet `Snapshot`

```json
{
  "date": "2026-05-04",
  "portfolioTotal": 78450.00,
  "lifeStrategy60": 12300.00,
  "msciWorld": 15600.00
}
```

| Champ | Type | Description |
|---|---|---|
| `date` | string | Date au format `yyyy-MM-dd` |
| `portfolioTotal` | number | Valeur totale du portefeuille en EUR |
| `lifeStrategy60` | number \| null | Valeur de référence LifeStrategy 60 |
| `msciWorld` | number \| null | Valeur de référence MSCI World |

---

## 3. Services

### 3.1 AssetClass

Regroupe les actifs par **classe d'actif** (`Stocks`, `Bonds`, `Cash`, etc.).

| Action | Paramètres | Réponse |
|---|---|---|
| `getAll` | — | `Aggregate[]` — toutes les classes avec métriques complètes |
| `getDistribution` | — | `Distribution[]` — poids de chaque classe |
| `getByAssetClass` | `assetClass` ✱ | `Aggregate[]` — AssetTypes dans la classe demandée |

**Exemples**
```
?service=AssetClass&action=getAll
?service=AssetClass&action=getDistribution
?service=AssetClass&action=getByAssetClass&assetClass=Stocks
```

Valeurs valides pour `assetClass` : voir `ASSET_CLASS` dans `Config.gs`.

---

### 3.2 AssetType

Regroupe les actifs par **type d'actif** (`ETF_Stocks`, `OPCVM`, `Crypto`, etc.).

| Action | Paramètres | Réponse |
|---|---|---|
| `getAll` | — | `Aggregate[]` — tous les types avec métriques complètes |
| `getDistribution` | — | `Distribution[]` — poids de chaque type |
| `getByAssetType` | `assetType` ✱ | `Asset[]` — actifs individuels du type demandé |

**Exemples**
```
?service=AssetType&action=getAll
?service=AssetType&action=getDistribution
?service=AssetType&action=getByAssetType&assetType=ETF_Stocks
```

Valeurs valides pour `assetType` : voir `ASSET_TYPE` dans `Config.gs`.

---

### 3.3 SupportType

Regroupe les actifs par **type d'enveloppe** (`PEA`, `CTO`, `LifeInsurance`, etc.).

| Action | Paramètres | Réponse |
|---|---|---|
| `getAll` | — | `Aggregate[]` — tous les types d'enveloppe avec métriques complètes |
| `getDistribution` | — | `Distribution[]` — poids de chaque type d'enveloppe |
| `getBySupportType` | `supportType` ✱ | `Aggregate[]` — Supports (brokers) dans le type d'enveloppe demandé |

**Exemples**
```
?service=SupportType&action=getAll
?service=SupportType&action=getDistribution
?service=SupportType&action=getBySupportType&supportType=PEA
```

Valeurs valides pour `supportType` : voir `SUPPORT_TYPE` dans `Config.gs`.

---

### 3.4 Support

Regroupe les actifs par **enveloppe / broker** (`PEA TR`, `Spirica`, `Kraken`, etc.).

| Action | Paramètres | Réponse |
|---|---|---|
| `getAll` | — | `Aggregate[]` — tous les supports avec métriques complètes |
| `getDistribution` | — | `Distribution[]` — poids de chaque support |
| `getBySupport` | `support` ✱ | `Asset[]` — actifs individuels dans le support demandé |

**Exemples**
```
?service=Support&action=getAll
?service=Support&action=getDistribution
?service=Support&action=getBySupport&support=PEA%20TR
```

Valeurs valides pour `support` : voir `SUPPORT` dans `Config.gs`.

---

### 3.5 Asset

Expose les **actifs individuels** avec leurs métriques complètes, filtrables par niveau de risque.

| Action | Paramètres | Réponse |
|---|---|---|
| `getAll` | — | `Asset[]` — tous les actifs avec métriques et `weightInPortfolio` |
| `getDistribution` | — | `Distribution[]` (+ `id`) — poids de chaque actif |
| `getByRisk` | `risk` ✱ (entier 0–4) | `Asset[]` — actifs du niveau de risque demandé |
| `getDistributionByRisk` | — | `Aggregate[]` — métriques agrégées par niveau de risque |

**Exemples**
```
?service=Asset&action=getAll
?service=Asset&action=getDistribution
?service=Asset&action=getByRisk&risk=4
?service=Asset&action=getDistributionByRisk
```

**Échelle de risque**

| Valeur | Niveau |
|---|---|
| `0` | Sans risque |
| `1` | Très faible |
| `2` | Faible |
| `3` | Moyen |
| `4` | Élevé |

---

### 3.6 Snapshot

Accès à l'**historique quotidien** de la valeur totale du portefeuille.

| Action | Paramètres | Réponse |
|---|---|---|
| `getLast` | — | `Snapshot` — dernier snapshot enregistré |
| `getHistory` | `limit` ○ (entier) | `Snapshot[]` — derniers N snapshots, ordre chronologique ascendant. Par défaut : tous. |

**Exemples**
```
?service=Snapshot&action=getLast
?service=Snapshot&action=getHistory
?service=Snapshot&action=getHistory&limit=30
```

---

## 4. Récapitulatif des endpoints

| Service | Action | Paramètre | Niveau de réponse |
|---|---|---|---|
| `AssetClass` | `getAll` | — | Groupe |
| `AssetClass` | `getDistribution` | — | Distribution |
| `AssetClass` | `getByAssetClass` | `assetClass` ✱ | Sous-groupe |
| `AssetType` | `getAll` | — | Groupe |
| `AssetType` | `getDistribution` | — | Distribution |
| `AssetType` | `getByAssetType` | `assetType` ✱ | Actif individuel |
| `SupportType` | `getAll` | — | Groupe |
| `SupportType` | `getDistribution` | — | Distribution |
| `SupportType` | `getBySupportType` | `supportType` ✱ | Sous-groupe |
| `Support` | `getAll` | — | Groupe |
| `Support` | `getDistribution` | — | Distribution |
| `Support` | `getBySupport` | `support` ✱ | Actif individuel |
| `Asset` | `getAll` | — | Actif individuel |
| `Asset` | `getDistribution` | — | Distribution |
| `Asset` | `getByRisk` | `risk` ✱ | Actif individuel |
| `Asset` | `getDistributionByRisk` | — | Groupe |
| `Snapshot` | `getLast` | — | Snapshot |
| `Snapshot` | `getHistory` | `limit` ○ | Snapshot[] |

✱ Requis — ○ Optionnel
