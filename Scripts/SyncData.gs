function syncCurrentTotal() {

  const source = SpreadsheetApp.openById(SOURCE_ID);
  const dest   = SpreadsheetApp.openById(DEST_ID);

  const assetSheet  = dest.getSheetByName(SHEET_ASSETS);
  const resultSheet = source.getSheetByName("Bilan");

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
      if (resultData[j][14] === name) {
        sourceRow = j;
        break;
      }
    }

    if (sourceRow === -1) {
      Logger.log("⚠️ Not found: " + name);
      continue;
    }

    // Read all 4 values from source row
    const totalPurchases = resultData[sourceRow][20] || "ND";
    const totalSales     = resultData[sourceRow][21] || 0;
    const dividends      = resultData[sourceRow][22] || 0;
    const current        = resultData[sourceRow][23] || 0;

    // Write all 4 values in one single operation
    assetSheet.getRange(i + 1, COL_TOTAL_PURCHASES + 1, 1, 4).setValues([[totalPurchases, totalSales, dividends, current]]);

    Logger.log("✅ " + name + " → " + current);    
  }

  assetSheet.getRange(64 + 1, COL_TOTAL_PURCHASES + 1, 1, 4).setValues([["ND", 0, 0, resultSheet.getRange("B65").getValue()]]); // Cash PEA
  assetSheet.getRange(55 + 1, COL_TOTAL_PURCHASES + 1, 1, 4).setValues([["ND", 0, 0, resultSheet.getRange("C25").getValue()]]); // Account Trade Republic  
}

function diagColumnL() {
  const dest       = SpreadsheetApp.openById(DEST_ID);
  const assetSheet = dest.getSheetByName(SHEET_ASSETS);
  const data       = assetSheet.getDataRange().getValues();

  let total = 0;
  for (let i = 1; i < data.length; i++) {
    const name = data[i][COL_NAME];
    const ct   = data[i][COL_CURRENT_TOTAL];
    if (ct && ct !== "ND" && ct !== "") {
      total += ct;
      Logger.log("Row " + (i+1) + " | " + name + " → " + ct + " | running: " + Math.round(total * 100) / 100);
    }
  }
  Logger.log("Total col L: " + Math.round(total * 100) / 100);
}
