function syncCurrentTotal() {

  const source = SpreadsheetApp.openById(SOURCE_ID);
  const dest   = SpreadsheetApp.openById(DEST_ID);

  const assetSheet  = dest.getSheetByName(SHEET_ASSETS);
  const resultSheet = source.getSheetByName(SOURCE_RESULTS);

  // --- Clear the 4 columns before writing ---
  const lastRow = assetSheet.getLastRow();
  assetSheet.getRange(2, COL_TOTAL_PURCHASES + 1, lastRow - 1, 4).clearContent();

  // --- Read source and destination data once ---
  const resultData = resultSheet.getDataRange().getValues();
  const assetData  = assetSheet.getDataRange().getValues();

  for (let i = 1; i < assetData.length; i++) {
    const name = assetData[i][COL_NAME];

    if (!name || name === "Not Defined") continue;

    // Find matching row in source data (in memory)
    let sourceRow = -1;
    for (let j = 0; j < resultData.length; j++) {
      if (resultData[j][COL_SOURCE_ASSETS] === name) {
        sourceRow = j;
        break;
      }
    }

    if (sourceRow === -1) {
      Logger.log("⚠️ Not found: " + name);
      continue;
    }

    // Read all 5 values from source row
    const risk            = resultData[sourceRow][COL_SOURCE_RISK] || "ND";
    const totalPurchases  = resultData[sourceRow][COL_SOURCE_TOTAL_PURCHASES] || "ND";
    const totalSales      = resultData[sourceRow][COL_SOURCE_TOTAL_SALES] || 0;
    const dividends       = resultData[sourceRow][COL_SOURCE_DIVIDEND] || 0;
    const current         = resultData[sourceRow][COL_SOURCE_CURRENT_TOTAL] || 0;

    // Write all () values in one single operation
    assetSheet.getRange(i + 1, COL_RISK + 1, 1, 5).setValues([[risk, totalPurchases, totalSales, dividends, current]]);

    Logger.log("✅ " + name + " → " + current);
  }

  const cashPEA = resultSheet.getRange(CASH_PEA).getValue(); // Cash PEA
  const smartCashMintos = resultSheet.getRange(SMART_CASH_MINTOS).getValue(); // Smart Cash Mintos

  // --- Auto-create assets found in the source but missing from the destination ---
  const existingNames = getExistingAssetNames(assetData);
  const newAssets      = findNewAssetsToAdd(resultData, existingNames);

  if (newAssets.length > 0) {
    let nextId = getNextAssetId(assetData);
    const newRows = newAssets.map(asset => buildNewAssetRow(nextId++, asset));

    assetSheet.getRange(assetSheet.getLastRow() + 1, 1, newRows.length, newRows[0].length).setValues(newRows);
    newAssets.forEach(asset => Logger.log("➕ Added: " + asset.name));

    sendNewAssetsAlertEmail(newAssets);
  }
}

// --- Names already present in the destination Asset sheet ---
function getExistingAssetNames(assetData) {
  const names = new Set();
  for (let i = 1; i < assetData.length; i++) {
    const name = assetData[i][COL_NAME];
    if (name && name !== "Not Defined") names.add(name);
  }
  return names;
}

// --- Next COL_ID to assign, based on the current max in the Asset sheet ---
function getNextAssetId(assetData) {
  let maxId = 0;
  for (let i = 1; i < assetData.length; i++) {
    const id = assetData[i][COL_ID];
    if (typeof id === "number" && id > maxId) maxId = id;
  }
  return maxId + 1;
}

// --- Assets present in the source Bilan but missing from the destination Asset sheet,
//     eligible only when their current value is a strictly positive number ---
function findNewAssetsToAdd(resultData, existingNames) {
  const seen   = new Set();
  const result = [];

  for (let i = 1; i < resultData.length; i++) {
    const name = resultData[i][COL_SOURCE_ASSETS];

    if (!name || existingNames.has(name) || seen.has(name)) continue;

    const current = resultData[i][COL_SOURCE_CURRENT_TOTAL];
    if (typeof current !== "number" || current <= 0) continue;

    seen.add(name);
    result.push({
      name,
      risk           : resultData[i][COL_SOURCE_RISK] || "ND",
      totalPurchases : resultData[i][COL_SOURCE_TOTAL_PURCHASES] || "ND",
      totalSales     : resultData[i][COL_SOURCE_TOTAL_SALES] || 0,
      dividends      : resultData[i][COL_SOURCE_DIVIDEND] || 0,
      current
    });
  }

  return result;
}

// --- Build an Asset sheet row for a newly discovered asset ---
// Classification columns are unknown from the source and marked "Not Defined"
// until the user completes them manually in the destination sheet.
function buildNewAssetRow(id, asset) {
  const row = [];
  row[COL_ID]              = id;
  row[COL_NAME]            = asset.name;
  row[COL_ASSET_CLASS]     = "Not Defined";
  row[COL_SUPPORT_TYPE]    = "Not Defined";
  row[COL_SUPPORT]         = "Not Defined";
  row[COL_ASSET_TYPE]      = "Not Defined";
  row[COL_SECTOR]          = "Not Defined";
  row[COL_INFORMATION]     = "";
  row[COL_GEOGRAPHY]       = "Not Defined";
  row[COL_RISK]            = asset.risk;
  row[COL_TOTAL_PURCHASES] = asset.totalPurchases;
  row[COL_TOTAL_SALES]     = asset.totalSales;
  row[COL_DIVIDENDS]       = asset.dividends;
  row[COL_CURRENT_TOTAL]   = asset.current;
  return row;
}

// --- Alert email listing assets auto-created by the ETL, to be classified manually ---
function sendNewAssetsAlertEmail(newAssets) {
  const today   = new Date().toISOString().slice(0, 10);
  const subject = `Nouveaux actifs détectés — Investissements — ${today}`;

  const items = newAssets
    .map(asset => `<li>${asset.name} — ${asset.current} €</li>`)
    .join("");

  const htmlBody = `
    <p>${newAssets.length} nouvel(aux) actif(s) ont été ajoutés automatiquement dans l'onglet <b>Asset</b> :</p>
    <ul>${items}</ul>
    <p>Merci de compléter manuellement, pour chacun, les colonnes AssetClass, SupportType, Support, AssetType, Sector et Geography (actuellement à "Not Defined").</p>
  `;

  MailApp.sendEmail(REPORT_EMAIL, subject, "", { htmlBody });
  Logger.log("New assets alert email sent to " + REPORT_EMAIL);
}
