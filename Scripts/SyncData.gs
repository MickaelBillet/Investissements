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

    // Read all 4 values from source row
    const totalPurchases = resultData[sourceRow][COL_SOURCE_TOTAL_PURCHASES] || "ND";
    const totalSales     = resultData[sourceRow][COL_SOURCE_TOTAL_SALES] || 0;
    const dividends      = resultData[sourceRow][COL_SOURCE_DIVIDEND] || 0;
    const current        = resultData[sourceRow][COL_SOURCE_CURRENT_TOTAL] || 0;

    // Write all 4 values in one single operation
    assetSheet.getRange(i + 1, COL_TOTAL_PURCHASES + 1, 1, 4).setValues([[totalPurchases, totalSales, dividends, current]]);

    Logger.log("✅ " + name + " → " + current);    
  }

  const cashPEA = resultSheet.getRange(CASH_PEA).getValue(); // Cash PEA
  const tradeRepublicAccount = resultSheet.getRange(TRADE_REPUBLIC_ACCOUNT).getValue(); // Account Trade Republic  
  const smartCashMintos = resultSheet.getRange(SMART_CASH_MINTOS).getValue(); // Smart Cash Mintos  
}
