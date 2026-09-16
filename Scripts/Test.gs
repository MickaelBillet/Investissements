// =====================================================================
// Test.gs — Manual test functions. Run individually in the Apps Script
// editor (Run > select function), check Logs (Ctrl+Entrée).
//
// No more doGet-based tests — there is no HTTP entry point anymore.
// These call the remaining handlers directly, exactly as
// rapportHebdomadaire() does.
// =====================================================================

function testSnapshotGetHistory() {
  Logger.log(JSON.stringify(handleSnapshot("getHistory", {})));
}

function testAssetClassGetDistribution() {
  Logger.log(JSON.stringify(handleAssetClass("getDistribution", {})));
}

function testSupportTypeGetDistribution() {
  Logger.log(JSON.stringify(handleSupportType("getDistribution", {})));
}

function testAssetGetDistributionByRisk() {
  Logger.log(JSON.stringify(handleAsset("getDistributionByRisk", {})));
}

function testRapportHebdomadaire() {
  rapportHebdomadaire(); // Sends real email — check Gmail inbox
}

function testFindNewAssetsToAdd() {
  // Header row + 4 source rows:
  // - "ETF World"    → already in destination, must be excluded
  // - "Cash Livret"  → new, current = 0, must be excluded
  // - "Crypto Kraken"→ new, current > 0, must be included
  // - "Crypto Kraken"→ duplicate name in source, must not create a second entry
  const resultData = [
    [], // header
    buildBilanRow("ETF World", 2, 1000, 0, 0, 5000),
    buildBilanRow("Cash Livret", 0, "ND", 0, 0, 0),
    buildBilanRow("Crypto Kraken", 4, 200, 0, 0, 350),
    buildBilanRow("Crypto Kraken", 4, 200, 0, 0, 350)
  ];

  const existingNames = new Set(["ETF World"]);

  const result = findNewAssetsToAdd(resultData, existingNames);
  Logger.log(JSON.stringify(result)); // Expected: only "Crypto Kraken", once
}

function buildBilanRow(name, risk, totalPurchases, totalSales, dividends, current) {
  const row = [];
  row[COL_SOURCE_ASSETS]         = name;
  row[COL_SOURCE_RISK]           = risk;
  row[COL_SOURCE_TOTAL_PURCHASES] = totalPurchases;
  row[COL_SOURCE_TOTAL_SALES]    = totalSales;
  row[COL_SOURCE_DIVIDEND]       = dividends;
  row[COL_SOURCE_CURRENT_TOTAL]  = current;
  return row;
}

function testBuildNewAssetRow() {
  const asset = { name: "Crypto Kraken", risk: 4, totalPurchases: 200, totalSales: 0, dividends: 0, current: 350 };
  const row   = buildNewAssetRow(42, asset);

  Logger.log(JSON.stringify(row));
  // Expected: row[COL_ID] === 42, row[COL_NAME] === "Crypto Kraken",
  // classification columns (AssetClass, SupportType, Support, AssetType, Sector, Geography) === "Not Defined",
  // row[COL_CURRENT_TOTAL] === 350
}

function testBuildSnapshotRow() {
  // Full row — all fields populated
  const rowComplete = ["2026-05-04", 78450.00, 42.15, 87.30, 65000.00, 83200.00, 1200.00];
  Logger.log("complete   : " + JSON.stringify(buildSnapshotRow(rowComplete)));

  // Missing lifeStrategy/msciWorld — must fall back to null, not 0
  const rowMissingRefs = ["2026-05-05", 79000.00, "", "", 65000.00, 83200.00, 1250.00];
  Logger.log("missingRefs: " + JSON.stringify(buildSnapshotRow(rowMissingRefs)));

  // Missing netCapital — must fall back to 0
  const rowNoNetCapital = ["2026-05-06", "", 42.20, 87.35, 65000.00, 83200.00, 1300.00];
  Logger.log("noNetCapital: " + JSON.stringify(buildSnapshotRow(rowNoNetCapital)));
}
