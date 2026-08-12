// =====================================================================
// AssetClassService.gs — AssetClass distribution, used by rapportHebdomadaire()
// =====================================================================

function handleAssetClass(action, params) {

  switch (action) {

    // --- Return distribution (weight) of each asset class ---
    case "getDistribution":
      const rows           = getAssetsData();
      const portfolioTotal = getPortfolioTotal(rows);
      return getAssetClassDistribution(rows, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Return weight distribution of each AssetClass ---
function getAssetClassDistribution(rows, portfolioTotal) {

  const groups = groupBy(rows, COL_ASSET_CLASS);
  const ids    = getReferenceIds(SHEET_ASSET_CLASS);

  return Object.keys(groups).map(assetClass => {
    const currentTotal = sumColumn(groups[assetClass], COL_CURRENT_TOTAL);

    return {
      id: ids[assetClass] !== undefined ? ids[assetClass] : null,
      name: assetClass,
      currentTotal,
      weightInPortfolio: portfolioTotal !== 0
        ? Math.round(currentTotal / portfolioTotal * 10000) / 100
        : 0
    };
  });
}
