// =====================================================================
// SectorService.gs — Handles all Sector-related endpoints
// =====================================================================

function handleSector(action, params) {

  const rows           = getAssetsData();
  const portfolioTotal = getPortfolioTotal(rows);

  switch (action) {

    // --- Return all sectors with full aggregated metrics ---
    case "getAll":
      return getSectorAll(rows, portfolioTotal);

    // --- Return distribution (weight) of each sector ---
    case "getDistribution":
      return getSectorDistribution(rows, portfolioTotal);

    // --- Return individual assets belonging to a given Sector ---
    case "getBySector":
      if (!params.sector) return { error: "Missing parameter: sector" };
      return getBySector(rows, params.sector, portfolioTotal);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Aggregate all rows by Sector ---
function getSectorAll(rows, portfolioTotal) {

  const groups = groupBy(rows, COL_SECTOR);

  return Object.keys(groups).map(sector => {
    const groupRows  = groups[sector];
    const groupTotal = sumColumn(groupRows, COL_CURRENT_TOTAL);

    return aggregateGroup(sector, groupRows, groupTotal, portfolioTotal);
  });
}

// --- Return weight distribution of each Sector ---
function getSectorDistribution(rows, portfolioTotal) {

  const groups = groupBy(rows, COL_SECTOR);

  return Object.keys(groups).map(sector => {
    const currentTotal = sumColumn(groups[sector], COL_CURRENT_TOTAL);

    return {
      id               : null,
      name             : sector,
      currentTotal,
      weightInPortfolio: portfolioTotal !== 0
        ? Math.round(currentTotal / portfolioTotal * 10000) / 100
        : 0
    };
  });
}

// --- Return individual assets belonging to a given Sector ---
function getBySector(rows, sector, portfolioTotal) {

  const filtered = rows.filter(row => row[COL_SECTOR] === sector);
  if (filtered.length === 0) return { error: "Sector not found: " + sector };

  const groupTotal = sumColumn(filtered, COL_CURRENT_TOTAL);

  return filtered.map(row => {
    const asset = buildAssetRow(row);

    asset.weightInGroup     = groupTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / groupTotal * 10000) / 100
      : 0;
    asset.weightInPortfolio = portfolioTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / portfolioTotal * 10000) / 100
      : 0;

    return asset;
  });
}
