// =====================================================================
// SupportTypeService.gs — Handles all SupportType-related endpoints
// =====================================================================

function handleSupportType(action, params) {

  const rows           = getAssetsData();
  const portfolioTotal = getPortfolioTotal(rows);

  switch (action) {

    // --- Return all support types with full aggregated metrics ---
    case "getAll":
      return getSupportTypeAll(rows, portfolioTotal);

    // --- Return distribution (weight) of each support type ---
    case "getDistribution":
      return getSupportTypeDistribution(rows, portfolioTotal);

    // --- Return aggregated Supports belonging to a given SupportType ---
    case "getBySupportType":
      if (!params.supportType) return { error: "Missing parameter: supportType" };
      return getBySupportType(rows, params.supportType, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Aggregate all rows by SupportType ---
function getSupportTypeAll(rows, portfolioTotal) {

  // Group rows by SupportType
  const groups = groupBy(rows, COL_SUPPORT_TYPE);

  return Object.keys(groups).map(supportType => {
    const groupRows  = groups[supportType];
    const groupTotal = sumColumn(groupRows, COL_CURRENT_TOTAL);

    return aggregateGroup(supportType, groupRows, groupTotal, portfolioTotal);
  });
}

// --- Return weight distribution of each SupportType ---
function getSupportTypeDistribution(rows, portfolioTotal) {

  const groups = groupBy(rows, COL_SUPPORT_TYPE);

  return Object.keys(groups).map(supportType => {
    const currentTotal = sumColumn(groups[supportType], COL_CURRENT_TOTAL);

    return {
      name             : supportType,
      currentTotal,
      weightInPortfolio: portfolioTotal !== 0
        ? Math.round(currentTotal / portfolioTotal * 10000) / 100
        : 0
    };
  });
}

// --- Return aggregated Supports belonging to a given SupportType ---
function getBySupportType(rows, supportType, portfolioTotal) {

  // Filter rows matching the requested SupportType
  const filtered = rows.filter(row => row[COL_SUPPORT_TYPE] === supportType);
  if (filtered.length === 0) return { error: "SupportType not found: " + supportType };

  const groupTotal = sumColumn(filtered, COL_CURRENT_TOTAL);

  // Group filtered rows by Support
  const groups = groupBy(filtered, COL_SUPPORT);

  return Object.keys(groups).map(support => {
    const groupRows = groups[support];
    return aggregateGroup(support, groupRows, groupTotal, portfolioTotal);
  });
}