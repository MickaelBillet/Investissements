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
