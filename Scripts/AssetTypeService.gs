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

    // --- Return reference metadata (id, name, labelFr, geoSectorEligible) from the AssetType sheet ---
    case "getReference":
      return getAssetTypeMeta();

    // --- Return individual assets belonging to a given AssetType ---
    case "getByAssetType":
      if (!params.assetType) return { error: "Missing parameter: assetType" };
      return getByAssetType(rows, params.assetType, portfolioTotal);

    // --- Return ETF_Stocks grouped by COL_INFORMATION with aggregated metrics ---
    case "getEtfStocksByInformation":
      return getEtfStocksByInformation(rows, portfolioTotal);

    // --- Return individual assets filtered by AssetType and information group ---
    case "getByAssetTypeAndInformation":
      if (!params.assetType) return { error: "Missing parameter: assetType" };
      if (!params.information) return { error: "Missing parameter: information" };
      return getByAssetTypeAndInformation(rows, params.assetType, params.information, portfolioTotal);

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
  const meta   = getAssetTypeMeta();
  const byName = {};
  meta.forEach(m => { byName[m.name] = m; });

  return Object.keys(groups).map(assetType => {
    const currentTotal = sumColumn(groups[assetType], COL_CURRENT_TOTAL);
    const m = byName[assetType];

    return {
      id               : m ? m.id : null,
      name             : assetType,
      labelFr          : m ? m.labelFr : null,
      currentTotal,
      weightInPortfolio: portfolioTotal !== 0
        ? Math.round(currentTotal / portfolioTotal * 10000) / 100
        : 0
    };
  });
}

// --- Return reference metadata for every row of the AssetType sheet ---
// Columns (0-based): A=id, B=name, C=assetClass, D=labelFr, E=geoSectorEligible
function getAssetTypeMeta() {
  const ss    = SpreadsheetApp.openById(DEST_ID);
  const sheet = ss.getSheetByName(SHEET_ASSET_TYPE);
  if (!sheet) return [];

  const data = sheet.getRange(1, 1, sheet.getLastRow(), 5).getValues();

  return data.slice(1)
    .filter(row => row[1] !== "")
    .map(row => ({
      id               : row[0],
      name             : row[1],
      labelFr          : row[3] !== "" ? row[3] : null,
      geoSectorEligible: row[4] === true
        || String(row[4]).trim().toUpperCase() === "TRUE"
        || String(row[4]).trim().toUpperCase() === "OUI"
    }));
}

// --- Return ETF_Stocks grouped by COL_INFORMATION with aggregated metrics ---
function getEtfStocksByInformation(rows, portfolioTotal) {
  const etfRows = rows.filter(row => row[COL_ASSET_TYPE] === ASSET_TYPE.ETF_STOCKS);
  if (etfRows.length === 0) return { error: "No ETF_Stocks found" };

  const groups     = groupBy(etfRows, COL_INFORMATION);
  const groupTotal = sumColumn(etfRows, COL_CURRENT_TOTAL);

  return Object.keys(groups).map(information => {
    const infoRows = groups[information];
    return aggregateGroup(information, infoRows, groupTotal, portfolioTotal);
  });
}

// --- Return individual assets filtered by AssetType and information group ---
function getByAssetTypeAndInformation(rows, assetType, information, portfolioTotal) {
  const filtered = rows.filter(
    row => row[COL_ASSET_TYPE] === assetType && row[COL_INFORMATION] === information
  );
  if (filtered.length === 0) return { error: "No assets found for: " + assetType + " / " + information };

  const groupTotal = sumColumn(filtered, COL_CURRENT_TOTAL);
  return filtered.map(row => {
    const asset = buildAssetRow(row);
    asset.weightInGroup     = groupTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / groupTotal * 10000) / 100 : 0;
    asset.weightInPortfolio = portfolioTotal !== 0
      ? Math.round(row[COL_CURRENT_TOTAL] / portfolioTotal * 10000) / 100 : 0;
    return asset;
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