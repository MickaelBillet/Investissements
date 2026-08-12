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
