// =====================================================================
// AssetTypeService.gs — Handles all AssetType-related endpoints
// =====================================================================

function handleAssetType(action, params) {

  const rows           = getAssetsData();
  const portfolioTotal = getPortfolioTotal(rows);

  switch (action) {

    // --- Return all asset types with full aggregated metrics ---
    case "getAll":
      return getAssetTypeAll(rows, portfolioTotal);

    // --- Return distribution (weight) of each asset type ---
    case "getDistribution":
      return getAssetTypeDistribution(rows, portfolioTotal);

    // --- Return individual assets belonging to a given AssetType ---
    case "getByAssetType":
      if (!params.assetType) return { error: "Missing parameter: assetType" };
      return getByAssetType(rows, params.assetType, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Aggregate all rows by AssetType ---
function getAssetTypeAll(rows, portfolioTotal) {

  // Group rows by AssetType
  const groups = groupBy(rows, COL_ASSET_TYPE);

  return Object.keys(groups).map(assetType => {
    const groupRows  = groups[assetType];
    const groupTotal = sumColumn(groupRows, COL_CURRENT_TOTAL);

    return aggregateGroup(assetType, groupRows, groupTotal, portfolioTotal);
  });
}

// --- Return weight distribution of each AssetType ---
function getAssetTypeDistribution(rows, portfolioTotal) {

  const groups = groupBy(rows, COL_ASSET_TYPE);

  return Object.keys(groups).map(assetType => {
    const currentTotal = sumColumn(groups[assetType], COL_CURRENT_TOTAL);

    return {
      name             : assetType,
      currentTotal,
      weightInPortfolio: portfolioTotal !== 0
        ? Math.round(currentTotal / portfolioTotal * 10000) / 100
        : 0
    };
  });
}

// --- Return individual assets belonging to a given AssetType ---
function getByAssetType(rows, assetType, portfolioTotal) {

  // Filter rows matching the requested AssetType
  const filtered = rows.filter(row => row[COL_ASSET_TYPE] === assetType);
  if (filtered.length === 0) return { error: "AssetType not found: " + assetType };

  const groupTotal = sumColumn(filtered, COL_CURRENT_TOTAL);

  // Return each individual asset with its metrics
  return filtered.map(row => {
    const asset = buildAssetRow(row);

    // Add weights relative to the group and the portfolio
    asset.weightInGroup      = groupTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / groupTotal * 10000) / 100
      : 0;
    asset.weightInPortfolio  = portfolioTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / portfolioTotal * 10000) / 100
      : 0;

    return asset;
  });
}