// =====================================================================
// SnapshotService.gs — Handles portfolio snapshot history endpoints
// =====================================================================

// --- Snapshot sheet column indexes (0-based) ---
const COL_SNAP_DATE        = 0;  // A
const COL_SNAP_PORTFOLIO   = 1;  // B
const COL_SNAP_LIFESTRATEGY = 2; // C
const COL_SNAP_MSCI_WORLD  = 3;  // D

function handleSnapshot(action, params) {

  const sheet = SpreadsheetApp.openById(DEST_ID).getSheetByName(SHEET_SNAPSHOT);
  const data  = sheet.getDataRange().getValues();

  // Skip header row and filter out empty rows
  const rows = data.slice(1).filter(row => row[COL_SNAP_DATE] !== "");

  switch (action) {

    // --- Return the most recent snapshot ---
    case "getLast":
      return getSnapshotLast(rows);

    // --- Return snapshot history with optional limit ---
    case "getHistory":
      const limit = params.limit ? parseInt(params.limit) : rows.length;
      return getSnapshotHistory(rows, limit);

    default:
      return { error: "Unknown action: " + action };
  }
}

// --- Return the most recent snapshot ---
function getSnapshotLast(rows) {

  if (rows.length === 0) return { error: "No snapshot available" };

  // Last row is the most recent snapshot
  const last = rows[rows.length - 1];

  return buildSnapshotRow(last);
}

// --- Return the last N snapshots ordered by date ascending ---
function getSnapshotHistory(rows, limit) {

  if (rows.length === 0) return { error: "No snapshot available" };

  // Take the last N rows (most recent) and preserve chronological order
  const sliced = rows.slice(-limit);

  return sliced.map(row => buildSnapshotRow(row));
}

// --- Build a snapshot object from a raw sheet row ---
function buildSnapshotRow(row) {

  const date = Utilities.formatDate(
    new Date(row[COL_SNAP_DATE]),
    Session.getScriptTimeZone(),
    "yyyy-MM-dd"
  );

  return {
    date,
    portfolioTotal : row[COL_SNAP_PORTFOLIO]    || 0,
    lifeStrategy60 : row[COL_SNAP_LIFESTRATEGY] || null,
    msciWorld      : row[COL_SNAP_MSCI_WORLD]   || null
  };
}

// =====================================================================
// Snapshot daily job — to be triggered once a day
// =====================================================================

function snapshotQuotidien() {

  const dest           = SpreadsheetApp.openById(DEST_ID);
  const source = SpreadsheetApp.openById(SOURCE_ID);

  const sheetAssets  = dest.getSheetByName(SHEET_ASSETS);
  const sheetSnap    = dest.getSheetByName(SHEET_SNAPSHOT);

  const resultSheet = source.getSheetByName("Bilan")

  const today = Utilities.formatDate(
    new Date(),
    Session.getScriptTimeZone(),
    "yyyy-MM-dd"
  );

  // --- Guard: do not snapshot twice on the same day ---
  const lastRow = sheetSnap.getLastRow();
  if (lastRow > 1) {
    const lastDate = Utilities.formatDate(
      new Date(sheetSnap.getRange(lastRow, 1).getValue()),
      Session.getScriptTimeZone(),
      "yyyy-MM-dd"
    );
    if (lastDate === today) {
      Logger.log("⚠️ Snapshot already exists for today, aborting.");
      return;
    }
  }

  // --- Step 1: sync current values from source sheet ---
  syncCurrentTotal();

  // --- Step 2: compute total portfolio value from Assets sheet ---
  const rows           = getAssetsData();
  const portfolioTotal = Math.round(getPortfolioTotal(rows) * 100) / 100;

  // --- Step 3: append snapshot row ---
  sheetSnap.appendRow([today, portfolioTotal, resultSheet.getRange("H52").getValue(), resultSheet.getRange("H54").getValue()]);

  Logger.log("✅ Snapshot " + today + " — portfolio total: " + portfolioTotal + " €");
}

// --- Create the daily trigger (run once manually) ---
function creerDeclencheurSnapshot() {

  // Remove existing triggers to avoid duplicates
  ScriptApp.getProjectTriggers().forEach(t => ScriptApp.deleteTrigger(t));

  // Run every day at 6:00 — after European market close
  ScriptApp.newTrigger("snapshotQuotidien")
    .timeBased()
    .everyDays(1)
    .atHour(6)
    .create();

  Logger.log("✅ Daily snapshot trigger created");
}