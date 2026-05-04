// =====================================================================
// AssetClassService.gs — Handles all AssetClass-related endpoints
// =====================================================================

function handleAssetClass(action, params) {

  const rows           = getAssetsData();
  const portfolioTotal = getPortfolioTotal(rows);

  switch (action) {

    // --- Return all asset classes with full aggregated metrics ---
    case "getAll":
      return getAssetClassAll(rows, portfolioTotal);

    // --- Return distribution (weight) of each asset class ---
    case "getDistribution":
      return getAssetClassDistribution(rows, portfolioTotal);

    // --- Return aggregated AssetTypes within a given AssetClass ---
    case "getByAssetClass":
      if (!params.assetClass) return { error: "Missing parameter: assetClass" };
      return getByAssetClass(rows, params.assetClass, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Aggregate all rows by AssetClass ---
function getAssetClassAll(rows, portfolioTotal) {

  // Group rows by AssetClass
  const groups = groupBy(rows, COL_ASSET_CLASS);

  return Object.keys(groups).map(assetClass => {
    const groupRows  = groups[assetClass];
    const groupTotal = sumColumn(groupRows, COL_CURRENT_TOTAL);

    return aggregateGroup(assetClass, groupRows, groupTotal, portfolioTotal);
  });
}

// --- Return weight distribution of each AssetClass ---
function getAssetClassDistribution(rows, portfolioTotal) {

  const groups = groupBy(rows, COL_ASSET_CLASS);

  return Object.keys(groups).map(assetClass => {
    const currentTotal = sumColumn(groups[assetClass], COL_CURRENT_TOTAL);

    return {
      name: assetClass,
      currentTotal,
      weightInPortfolio: portfolioTotal !== 0
        ? Math.round(currentTotal / portfolioTotal * 10000) / 100
        : 0
    };
  });
}

// --- Return aggregated AssetTypes belonging to a given AssetClass ---
function getByAssetClass(rows, assetClass, portfolioTotal) {

  // Filter rows matching the requested AssetClass
  const filtered   = rows.filter(row => row[COL_ASSET_CLASS] === assetClass);
  if (filtered.length === 0) return { error: "AssetClass not found: " + assetClass };

  const groupTotal = sumColumn(filtered, COL_CURRENT_TOTAL);

  // Group filtered rows by AssetType
  const groups = groupBy(filtered, COL_ASSET_TYPE);

  return Object.keys(groups).map(assetType => {
    const groupRows = groups[assetType];
    return aggregateGroup(assetType, groupRows, groupTotal, portfolioTotal);
  });
}