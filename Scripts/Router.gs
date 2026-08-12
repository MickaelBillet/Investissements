// =====================================================================
// Router.gs — Shared read/aggregation helpers used by the daily ETL
// and the weekly email report (rapportHebdomadaire).
//
// There is no HTTP entry point here anymore — the dashboard reads the
// Google Sheet directly via the Sheets API (see Api/Services/AssetsService.cs,
// SnapshotService.cs). This file only holds what snapshotQuotidien() and
// rapportHebdomadaire() still call.
// =====================================================================

// --- Read all asset rows from the Assets sheet (header excluded) ---
function getAssetsData() {
  const ss        = SpreadsheetApp.openById(DEST_ID);
  const sheet     = ss.getSheetByName(SHEET_ASSETS);

  // Get all rows up to the last row with any content
  const lastRow   = sheet.getLastRow();
  const lastCol   = sheet.getLastColumn();

  const data = sheet.getRange(1, 1, lastRow, lastCol).getValues();

  // Skip header row and filter out empty and Not Defined rows
  return data.slice(1).filter(row => row[COL_NAME] !== "Not Defined");
}

function getPortfolioTotal(rows) {
  let sum = 0;
  for (let i = 0; i < rows.length; i++) {
    const ct = rows[i][COL_CURRENT_TOTAL];
    if (ct && ct !== "ND") {
      sum += ct;
    }
  }
  return sum;
}

// --- Aggregate a group of rows into a single summary object ---
function aggregateGroup(name, rows, groupTotal, portfolioTotal) {
  let totalPurchases = 0;
  let totalSales     = 0;
  let dividends      = 0;
  let currentTotal   = 0;
  let hasND          = false;

  for (let i = 0; i < rows.length; i++) {
    const tp = rows[i][COL_TOTAL_PURCHASES];
    const ts = rows[i][COL_TOTAL_SALES];
    const div = rows[i][COL_DIVIDENDS];
    const ct = rows[i][COL_CURRENT_TOTAL];

    // Check if any value is ND
    if (tp === "ND") {
      hasND = true;
    }

    totalPurchases += tp && tp !== "ND" ? tp : 0;
    totalSales     += ts && ts !== "ND" ? ts : 0;
    dividends      += div && div !== "ND" ? div : 0;
    currentTotal   += ct && ct !== "ND" ? ct : 0;
  }

  const netInvested = totalPurchases - totalSales;

  return {
    name,
    totalPurchases,
    totalSales,
    dividends,
    currentTotal,
    hasIncompleteData: hasND,
    unrealizedGain: !hasND && netInvested !== 0
      ? currentTotal - netInvested
      : null,
    yield: !hasND && netInvested !== 0
      ? Math.round(dividends / netInvested * 10000) / 100
      : null,
    roi: !hasND && totalPurchases !== 0
      ? Math.round((currentTotal + totalSales + dividends - totalPurchases) / totalPurchases * 10000) / 100
      : null,
    weightInGroup: groupTotal !== 0
      ? Math.round(currentTotal / groupTotal * 10000) / 100
      : 0,
    weightInPortfolio: portfolioTotal !== 0
      ? Math.round(currentTotal / portfolioTotal * 10000) / 100
      : 0
  };
}

// =====================================================================
// Shared utility functions
// =====================================================================

// --- Read a reference sheet and return a { name: id } map ---
function getReferenceIds(sheetName) {
  const ss    = SpreadsheetApp.openById(DEST_ID);
  const sheet = ss.getSheetByName(sheetName);
  if (!sheet) return {};

  const data = sheet.getRange(1, 1, sheet.getLastRow(), 2).getValues();
  const map  = {};
  data.slice(1).forEach(row => {
    if (row[1] !== "") map[row[1]] = row[0];
  });
  return map;
}

// --- Group an array of rows by the value of a given column index ---
function groupBy(rows, colIndex) {
  const acc = {}; // empty dictionary

  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    const key = row[colIndex]; // ex: "Stocks", "Bonds", "Crypto"

    // If the key does not exist yet, initialize an empty array
    if (!acc[key]) acc[key] = [];

    // Append the row to the array for this key
    acc[key].push(row);
  }

  return acc;
}

// --- Sum the values of a given column across an array of rows ---
function sumColumn(rows, colIndex) {
  let sum = 0;

  for (let i = 0; i < rows.length; i++) {
    const valeur = rows[i][colIndex] || 0;
    sum = sum + valeur;
  }

  return sum;
}
