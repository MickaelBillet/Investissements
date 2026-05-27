function fetchStockValues() {  
  const sheet = SpreadsheetApp.openById(DEST_ID);

  const tickers = ["AMS:V60A", "EPA:MWRD"]; 
  let prices = []; // Objet pour stocker les prix par ticker
    
  tickers.forEach(ticker => {
    try {
      // Utilisation d'une cellule temporaire pour forcer le calcul GoogleFinance
      const tempCell = sheet.getRange("ZZ1");
      tempCell.setFormula(`=GOOGLEFINANCE("${ticker}"; "price")`);      
      const val = tempCell.getValue();
      prices.push(typeof val === 'number' ? val : -1);

      tempCell.clearContent();
    } catch (e) {
      prices.push(-1);
      Logger.log("error : " + ticker + " : " + e.message);
    }
  });
  
  Logger.log("price : " + JSON.stringify(prices));

  return prices;
}
