# SPECS.md — Api (Azure Functions)

**Statut :** Implémenté  
**Version :** 1.0  
**Date :** 2026-05-08

---

## 1. Vue d'ensemble

L'Api expose 4 endpoints REST en lecture seule. Chaque endpoint délègue à l'Apps Script Web App via `IAppsScriptService`. Les endpoints sont accessibles uniquement depuis le Blazor WASM hébergé sur le même Azure Static Web Apps.

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

### 2.3 `GET /api/snapshot`

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

### 2.4 `GET /api/snapshot/history`

Retourne l'historique complet des snapshots en ordre chronologique ascendant.

**Réponse** : `SnapshotDto[]`

Même structure que `GET /api/snapshot`.

---

## 3. Codes de réponse

| Code | Cas |
|---|---|
| `200 OK` | Succès |
| `500 Internal Server Error` | Erreur Apps Script ou désérialisation échouée |

---

## 4. DTOs (Shared)

Les DTOs sont définis dans le projet `Shared` et partagés avec le Blazor WASM.

| DTO | Fichier |
|---|---|
| `AssetDto` | `Shared/Models/AssetDto.cs` |
| `DistributionDto` | `Shared/Models/DistributionDto.cs` |
| `SnapshotDto` | `Shared/Models/SnapshotDto.cs` |
| `AggregateDto` | `Shared/Models/AggregateDto.cs` (prévu, non utilisé) |
