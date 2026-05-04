// =====================================================================
// AssetService.gs — Handles all Asset-related endpoints
// =====================================================================

function handleAsset(action, params) {

  const rows           = getAssetsData();
  const portfolioTotal = getPortfolioTotal(rows);

  switch (action) {

    // --- Return all individual assets with full metrics ---
    case "getAll":
      return getAssetAll(rows, portfolioTotal);

    // --- Return distribution (weight) of each individual asset ---
    case "getDistribution":
      return getAssetDistribution(rows, portfolioTotal);

    // --- Return individual assets filtered by Risk level ---
    case "getByRisk":
      if (params.risk === undefined) return { error: "Missing parameter: risk" };
      return getAssetByRisk(rows, parseInt(params.risk), portfolioTotal);

    // --- Return distribution of assets grouped by Risk level ---
    case "getDistributionByRisk":
      return getAssetDistributionByRisk(rows, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Return all individual assets with full metrics ---
function getAssetAll(rows, portfolioTotal) {

  return rows.map(row => {
    const asset = buildAssetRow(row);

    asset.weightInPortfolio = portfolioTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / portfolioTotal * 10000) / 100
      : 0;

    return asset;
  });
}

// --- Return weight distribution of each individual asset ---
function getAssetDistribution(rows, portfolioTotal) {

  return rows.map(row => ({
    id              : row[COL_ID],
    name            : row[COL_NAME],
    currentTotal    : row[COL_CURRENT_TOTAL] || 0,
    weightInPortfolio: portfolioTotal !== 0
      ? Math.round((row[COL_CURRENT_TOTAL] || 0) / portfolioTotal * 10000) / 100
      : 0
  }));
}

// --- Return individual assets filtered by Risk level ---
function getAssetByRisk(rows, risk, portfolioTotal) {

  const filtered = rows.filter(row => row[COL_RISK] === risk);
  if (filtered.length === 0) return { error: "No assets found for risk: " + risk };

  const groupTotal = sumColumn(filtered, COL_CURRENT_TOTAL);

  return filtered.map(row => {
    const asset = buildAssetRow(row);

    // Weight within the risk group and within the full portfolio
    asset.weightInGroup     = groupTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / groupTotal * 10000) / 100
      : 0;
    asset.weightInPortfolio = portfolioTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / portfolioTotal * 10000) / 100
      : 0;

    return asset;
  });
}

// --- Return aggregated metrics grouped by Risk level ---
function getAssetDistributionByRisk(rows, portfolioTotal) {

  // Group rows by Risk level
  const groups = groupBy(rows, COL_RISK);

  return Object.keys(groups)
    .map(risk => {
      const groupRows  = groups[risk];
      const groupTotal = sumColumn(groupRows, COL_CURRENT_TOTAL);

      return aggregateGroup("Risk " + risk, groupRows, groupTotal, portfolioTotal);
    })
    // Sort by risk level ascending
    .sort((a, b) => parseInt(a.name.split(" ")[1]) - parseInt(b.name.split(" ")[1]));
}