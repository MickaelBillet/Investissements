// =====================================================================
// SupportService.gs — Handles all Support-related endpoints
// =====================================================================

function handleSupport(action, params) {

  const rows           = getAssetsData();
  const portfolioTotal = getPortfolioTotal(rows);

  switch (action) {

    // --- Return all supports with full aggregated metrics ---
    case "getAll":
      return getSupportAll(rows, portfolioTotal);

    // --- Return distribution (weight) of each support ---
    case "getDistribution":
      return getSupportDistribution(rows, portfolioTotal);

    // --- Return individual assets belonging to a given Support ---
    case "getBySupport":
      if (!params.support) return { error: "Missing parameter: support" };
      return getBySupport(rows, params.support, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Aggregate all rows by Support ---
function getSupportAll(rows, portfolioTotal) {

  // Group rows by Support
  const groups = groupBy(rows, COL_SUPPORT);

  return Object.keys(groups).map(support => {
    const groupRows  = groups[support];
    const groupTotal = sumColumn(groupRows, COL_CURRENT_TOTAL);

    return aggregateGroup(support, groupRows, groupTotal, portfolioTotal);
  });
}

// --- Return weight distribution of each Support ---
function getSupportDistribution(rows, portfolioTotal) {

  const groups = groupBy(rows, COL_SUPPORT);

  return Object.keys(groups).map(support => {
    const currentTotal = sumColumn(groups[support], COL_CURRENT_TOTAL);

    return {
      name             : support,
      currentTotal,
      weightInPortfolio: portfolioTotal !== 0
        ? Math.round(currentTotal / portfolioTotal * 10000) / 100
        : 0
    };
  });
}

// --- Return individual assets belonging to a given Support ---
function getBySupport(rows, support, portfolioTotal) {

  // Filter rows matching the requested Support
  const filtered = rows.filter(row => row[COL_SUPPORT] === support);
  if (filtered.length === 0) return { error: "Support not found: " + support };

  const groupTotal = sumColumn(filtered, COL_CURRENT_TOTAL);

  // Return each individual asset with its metrics
  return filtered.map(row => {
    const asset = buildAssetRow(row);

    // Add weights relative to the group and the portfolio
    asset.weightInGroup     = groupTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / groupTotal * 10000) / 100
      : 0;
    asset.weightInPortfolio = portfolioTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / portfolioTotal * 10000) / 100
      : 0;

    return asset;
  });
}