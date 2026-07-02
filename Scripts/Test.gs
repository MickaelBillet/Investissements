function testSnapshotGetLast() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "Snapshot",
      action  : "getLast"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testSnapshotGetHistory() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "Snapshot",
      action  : "getHistory",
      limit   : "30"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testDoGetAllAssetClass() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "AssetClass",
      action  : "getAll"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testDoGetAllAssetType() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "AssetType",
      action  : "getAll"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testDoGetAllSupportType() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "SupportType",
      action  : "getAll"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testDoGetAllSupport() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "Support",
      action  : "getAll"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testDoGetAllAsset() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "Asset",
      action  : "getAll"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testDoGetAllSector() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "Sector",
      action  : "getAll"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testGetEtfStocksByInformation() {
  const e = {
    parameter: {
      apiKey  : "token-zapto",
      service : "AssetType",
      action  : "getEtfStocksByInformation"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testGetByAssetTypeAndInformation() {
  const e = {
    parameter: {
      apiKey      : "token-zapto",
      service     : "AssetType",
      action      : "getByAssetTypeAndInformation",
      assetType   : "ETF_Stocks",
      information : "ETF Hydrogen"
    }
  };

  const result = doGet(e);
  Logger.log(result.getContent());
}

function testGeographyGetDistribution() {
  const e = { parameter: { apiKey: "token-zapto", service: "Geography", action: "getDistribution" } };
  Logger.log(doGet(e).getContent());
}

function testGeographyGetByZone() {
  const e = { parameter: { apiKey: "token-zapto", service: "Geography", action: "getByZone", zone: "Europe" } };
  Logger.log(doGet(e).getContent());
}

function testRapportHebdomadaire() {
  rapportHebdomadaire(); // Sends real email — check Gmail inbox
}

function testBuildSnapshotRow() {
  // Full row — all fields populated
  const rowComplete = ["2026-05-04", 78450.00, 42.15, 87.30, 65000.00, 83200.00];
  Logger.log("complete   : " + JSON.stringify(buildSnapshotRow(rowComplete)));

  // Missing lifeStrategy/msciWorld — must fall back to null, not 0
  const rowMissingRefs = ["2026-05-05", 79000.00, "", "", 65000.00, 83200.00];
  Logger.log("missingRefs: " + JSON.stringify(buildSnapshotRow(rowMissingRefs)));

  // Missing portfolioTotal — must fall back to 0
  const rowNoPortfolio = ["2026-05-06", "", 42.20, 87.35, 65000.00, 83200.00];
  Logger.log("noPortfolio: " + JSON.stringify(buildSnapshotRow(rowNoPortfolio)));
}
