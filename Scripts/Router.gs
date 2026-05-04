// =====================================================================
// Router.gs — Single HTTP entry point, authentication and dispatch
// =====================================================================
function setApiToken() {
  PropertiesService.getScriptProperties().setProperty("API_TOKEN", "token-zapto");
  Logger.log("✅ Token saved");
}

function doGet(e) {

  // --- Token verification ---
  const API_TOKEN = PropertiesService.getScriptProperties().getProperty("API_TOKEN");
  if (e.parameter.apiKey !== API_TOKEN) {
    return buildResponse({ error: "Unauthorized" });
  }

  // --- Read query parameters ---
  const service = e.parameter.service || "";
  const action  = e.parameter.action  || "";

  // --- Dispatch to the appropriate service ---
  try {
    let result;

    switch (service) {
      case "AssetClass":
        result = handleAssetClass(action, e.parameter);
        break;
      case "AssetType":
        result = handleAssetType(action, e.parameter);
        break;
      case "SupportType":
        result = handleSupportType(action, e.parameter);
        break;
      case "Support":
        result = handleSupport(action, e.parameter);
        break;
      case "Asset":
        result = handleAsset(action, e.parameter);
        break;
      case "Snapshot":
        result = handleSnapshot(action, e.parameter);
        break;
      default:
        result = { error: "Unknown service: " + service };
    }

    return buildResponse(result);

  } catch (err) {
    return buildResponse({ error: err.message });
  }
}

// --- Build a JSON response ---
function buildResponse(data) {
  return ContentService
    .createTextOutput(JSON.stringify(data))
    .setMimeType(ContentService.MimeType.JSON);
}

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

// --- Build a single asset object from a raw sheet row ---
function buildAssetRow(row) {
  const totalPurchases = row[COL_TOTAL_PURCHASES];
  const totalSales     = row[COL_TOTAL_SALES];
  const dividends      = row[COL_DIVIDENDS];
  const currentTotal   = row[COL_CURRENT_TOTAL];

  // Check if values are available (not ND, not empty)
  const hasData = totalPurchases && totalPurchases !== "ND"
               && totalSales     !== "ND"
               && currentTotal   && currentTotal   !== "ND";

  const tp          = hasData ? (totalPurchases || 0) : 0;
  const ts          = hasData ? (totalSales     || 0) : 0;
  const div         = hasData ? (dividends      || 0) : 0;
  const ct          = hasData ? (currentTotal   || 0) : 0;
  const netInvested = tp - ts;

  return {
    id           : row[COL_ID],
    name         : row[COL_NAME],
    assetClass   : row[COL_ASSET_CLASS],
    supportType  : row[COL_SUPPORT_TYPE],
    support      : row[COL_SUPPORT],
    assetType    : row[COL_ASSET_TYPE],
    information  : row[COL_INFORMATION],
    risk         : row[COL_RISK],
    totalPurchases: hasData ? tp  : null,
    totalSales    : hasData ? ts  : null,
    dividends     : hasData ? div : null,
    currentTotal  : hasData ? ct  : null,
    // null when data is not available
    unrealizedGain: hasData && netInvested !== 0
      ? currentTotal - netInvested
      : null,
    yield: hasData && netInvested !== 0
      ? Math.round(div / netInvested * 10000) / 100
      : null,
    roi: hasData && tp !== 0
      ? Math.round((ct + ts + div - tp) / tp * 10000) / 100
      : null
  };
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
// Shared utility functions (used by all services)
// =====================================================================

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