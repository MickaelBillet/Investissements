// =====================================================================
// GeographyService.gs — Geographic distribution for Stocks & Bonds
// =====================================================================

function handleGeography(action, params) {

  const rows     = getAssetsData();
  const eligible = rows.filter(isGeographyEligible);

  switch (action) {

    // --- Return weighted distribution by geographic zone ---
    case "getDistribution":
      const filtered = params.assetClass
        ? eligible.filter(r => r[COL_ASSET_CLASS] === params.assetClass)
        : eligible;
      return getGeographyDistribution(filtered);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Filter: Stocks/Bonds classes AND eligible asset types ---
function isGeographyEligible(row) {
  return GEOGRAPHY_ASSET_CLASSES.includes(row[COL_ASSET_CLASS])
      && GEOGRAPHY_ASSET_TYPES.includes(row[COL_ASSET_TYPE]);
}

// --- Parse "Zone1 : X% - Zone2 : Y%" into [{ zone, pct }] ---
function parseGeography(geoStr) {
  if (!geoStr || geoStr.trim() === "") return [];
  return geoStr.split(" - ").reduce((acc, part) => {
    const sepIdx = part.lastIndexOf(" : ");
    if (sepIdx === -1) return acc;
    const zone   = part.substring(0, sepIdx).trim();
    const pctStr = part.substring(sepIdx + 3).replace("%", "").trim();
    const pct    = parseFloat(pctStr);
    if (zone && !isNaN(pct)) acc.push({ zone, pct: pct / 100 });
    return acc;
  }, []);
}

// --- Aggregate weighted totals by zone ---
function getGeographyDistribution(rows) {

  const zoneMap = {};

  rows.forEach(row => {
    const ct = typeof row[COL_CURRENT_TOTAL] === "number" ? row[COL_CURRENT_TOTAL] : 0;
    if (ct <= 0) return;

    parseGeography(row[COL_GEOGRAPHY]).forEach(({ zone, pct }) => {
      zoneMap[zone] = (zoneMap[zone] || 0) + ct * pct;
    });
  });

  const total = Object.values(zoneMap).reduce((s, v) => s + v, 0);

  return Object.entries(zoneMap)
    .map(([zone, value]) => ({
      id               : null,
      name             : zone,
      currentTotal     : Math.round(value * 100) / 100,
      weightInPortfolio: total > 0
        ? Math.round(value / total * 10000) / 100
        : 0
    }))
    .sort((a, b) => b.currentTotal - a.currentTotal);
}
