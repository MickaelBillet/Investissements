// =====================================================================
// SupportTypeService.gs — SupportType distribution, used by rapportHebdomadaire()
// =====================================================================

function handleSupportType(action, params) {

  switch (action) {

    // --- Return distribution (weight) of each support type ---
    case "getDistribution":
      const rows           = getAssetsData();
      const portfolioTotal = getPortfolioTotal(rows);
      return getSupportTypeDistribution(rows, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Return weight distribution of each SupportType ---
function getSupportTypeDistribution(rows, portfolioTotal) {

  const groups = groupBy(rows, COL_SUPPORT_TYPE);
  const ids    = getReferenceIds(SHEET_SUPPORT_TYPE);

  return Object.keys(groups).map(supportType => {
    const currentTotal = sumColumn(groups[supportType], COL_CURRENT_TOTAL);

    return {
      id               : ids[supportType] !== undefined ? ids[supportType] : null,
      name             : supportType,
      currentTotal,
      weightInPortfolio: portfolioTotal !== 0
        ? Math.round(currentTotal / portfolioTotal * 10000) / 100
        : 0
    };
  });
}
