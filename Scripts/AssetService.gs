// =====================================================================
// AssetService.gs — Asset-by-risk aggregation, used by rapportHebdomadaire()
// =====================================================================

function handleAsset(action, params) {

  switch (action) {

    // --- Return aggregated metrics grouped by Risk level ---
    case "getDistributionByRisk":
      const rows           = getAssetsData();
      const portfolioTotal = getPortfolioTotal(rows);
      return getAssetDistributionByRisk(rows, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
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
