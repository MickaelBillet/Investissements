# SPECS.md — Api (Azure Functions)

**Statut :** Implémenté  
**Version :** 1.0  
**Date :** 2026-05-08

---

## 1. Vue d'ensemble

L'Api expose 7 endpoints REST en lecture seule. Chaque endpoint délègue à l'Apps Script Web App via `IAppsScriptService` (sauf `PortfolioMetricsFunction` qui compose plusieurs services). Les endpoints sont accessibles uniquement depuis le Blazor WASM hébergé sur le même Azure Static Web Apps.

**Base URL (local)** : `http://localhost:7071`  
**Base URL (prod)** : interne au Static Web Apps

---

## 2. Endpoints

### 2.1 `GET /api/assets`

Retourne tous les actifs du portefeuille avec leurs métriques complètes.

**Réponse** : `AssetDto[]`

```json
[
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
    "weightInPortfolio": 8.10
  }
]
```

**Notes :**
- `totalPurchases`, `totalSales`, `dividends` sont `null` quand la valeur est `"ND"` dans le sheet
- `currentTotal` est `null` si indisponible dans le sheet
- `unrealizedGain`, `yield`, `roi` sont `null` quand les données financières sont incomplètes

---

### 2.2 `GET /api/assets/distribution/{dimension}`

Retourne la distribution du portefeuille par dimension, avec le poids de chaque groupe.

**Paramètre de route** : `dimension` — valeurs valides : `assetClass`, `assetType`, `supportType`, `support`

**Réponse** : `DistributionDto[]`

```json
[
  {
    "id": 0,
    "name": "Stocks",
    "currentTotal": 35000.00,
    "weightInPortfolio": 66.05
  }
]
```

**Notes :**
- `id` est l'identifiant de la dimension lu depuis l'onglet de référence (`AssetClass`, `AssetType`, `SupportType`, `Support`)
- Résultats triés par `currentTotal` décroissant (côté Apps Script)

---

### 2.3 `GET /api/assets/etfstocks/information`

Retourne les ETF_Stocks groupés par champ `information`, avec métriques agrégées.

**Réponse** : `AggregateDto[]`

```json
[
  {
    "name": "World",
    "totalPurchases": 12000.00,
    "totalSales": 0.00,
    "dividends": 0.00,
    "currentTotal": 14500.00,
    "hasIncompleteData": false,
    "unrealizedGain": 2500.00,
    "yield": 0.00,
    "roi": 20.83,
    "weightInGroup": 100.00,
    "weightInPortfolio": 27.36
  }
]
```

---

### 2.4 `GET /api/assets/etfstocks/information/{information}`

Retourne les actifs de type `ETF_Stocks` filtrés par valeur du champ `information`.

**Paramètre de route** : `information` — valeur libre correspondant au champ `information` de l'actif

**Réponse** : `AssetDto[]`

Même structure que `GET /api/assets`.

---

### 2.5 `GET /api/portfolio/metrics`

Retourne les métriques agrégées du portefeuille calculées côté API.

**Réponse** : `PortfolioMetricsDto`

```json
{
  "roiOnTotalPurchases": 15.23,
  "roiOnCapitalEngaged": 11.08,
  "averageRisk": 2.8
}
```

**Formules de calcul :**
```
RoiOnTotalPurchases = TotalReturns / TotalPurchases × 100
RoiOnCapitalEngaged = TotalReturns / PortfolioTotal × 100
AverageRisk         = Σ(risk_i × currentTotal_i) / Σ(currentTotal_i)  [actifs avec currentTotal > 0]
```

**Notes :**
- `roiOnTotalPurchases` et `roiOnCapitalEngaged` sont `null` si `TotalPurchases` ou `PortfolioTotal` sont nuls ou indisponibles
- `averageRisk` est `null` si la valeur totale des actifs actifs est zéro

---

### 2.6 `GET /api/snapshot`

Retourne le dernier snapshot du portefeuille.

**Réponse** : `SnapshotDto`

```json
{
  "date": "2026-05-08",
  "portfolioTotal": 52984.31,
  "lifeStrategy60": 35.93,
  "msciWorld": 150.25,
  "totalPurchases": 66659.86,
  "totalReturns": 58200.00
}
```

**Notes :**
- `lifeStrategy60`, `msciWorld`, `totalPurchases`, `totalReturns` peuvent être `null` si non disponibles

---

### 2.7 `GET /api/snapshot/history`

Retourne l'historique complet des snapshots en ordre chronologique ascendant.

**Réponse** : `SnapshotDto[]`

Même structure que `GET /api/snapshot`.

---

## 3. Codes de réponse

| Code | Cas |
|---|---|
| `200 OK` | Succès |
| `400 Bad Request` | Paramètre invalide (ex : dimension inconnue sur `/api/assets/distribution/{dimension}`) |
| `502 Bad Gateway` | Erreur HTTP lors de l'appel à l'Apps Script |
| `500 Internal Server Error` | Erreur inattendue (désérialisation, calcul) |

---

## 4. DTOs (Shared)

Les DTOs sont définis dans le projet `Shared` et partagés avec le Blazor WASM.

| DTO | Fichier | Champs |
|---|---|---|
| `AssetDto` | `Shared/Models/AssetDto.cs` | id, name, assetClass, supportType, support, assetType, information, risk, totalPurchases?, totalSales?, dividends?, currentTotal?, unrealizedGain?, yield?, roi?, weightInPortfolio |
| `DistributionDto` | `Shared/Models/DistributionDto.cs` | id?, name, currentTotal, weightInPortfolio |
| `SnapshotDto` | `Shared/Models/SnapshotDto.cs` | date, portfolioTotal, lifeStrategy60?, msciWorld?, totalPurchases?, totalReturns? |
| `PortfolioMetricsDto` | `Shared/Models/PortfolioMetricsDto.cs` | roiOnTotalPurchases?, roiOnCapitalEngaged?, averageRisk? |

> Les champs suffixés `?` sont nullable — `null` quand la valeur est indisponible ou non calculable.
