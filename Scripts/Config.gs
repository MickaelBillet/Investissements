// =====================================================================
// Config.gs — Shared enumerations and constants
// =====================================================================


const SOURCE_ID = "188bNY_oSVrHZKZ1Vadj7SxnCosDDw2g-oG8UjDARMAI";
const DEST_ID = "1Dvhz3AME0WoGkmiJQ5eBQ4FYno4l_kuyiShk75TZZNE";

// --- SOURCE - Sheet name
const SOURCE_RESULTS = "Bilan";

// --- SOURCE - Column
const COL_SOURCE_ASSETS = 13;

// --- Sheet names ---
const SHEET_ASSETS   = "Asset";
const SHEET_SNAPSHOT = "Snapshot";

// --- Assets sheet column indexes (0-based) ---
const COL_ID              = 0;  // A
const COL_NAME            = 1;  // B
const COL_ASSET_CLASS     = 2;  // C
const COL_SUPPORT_TYPE    = 3;  // D
const COL_SUPPORT         = 4;  // E
const COL_ASSET_TYPE      = 5;  // F
const COL_INFORMATION     = 6;  // G
const COL_RISK            = 7;  // H
const COL_TOTAL_PURCHASES = 8;  // I
const COL_TOTAL_SALES     = 9;  // J
const COL_DIVIDENDS       = 10; // K
const COL_CURRENT_TOTAL   = 11; // L

// --- AssetClass enumeration ---
const ASSET_CLASS = {
  STOCKS       : "Stocks",
  BONDS        : "Bonds",
  CASH         : "Cash",
  PRIVATE_DEBT : "PrivateDebt",
  REAL_ESTATE  : "RealEstate",
  COMMODITIES  : "Commodities",
  CRYPTO       : "Crypto",
  MISCELLANEOUS: "Miscellaneous"
};

// --- AssetType enumeration ---
const ASSET_TYPE = {
  STOCK           : "Stock",
  ETF_STOCKS      : "ETF_Stocks",
  ETF_BONDS       : "ETF_Bunds",
  CASH_DEPOSIT    : "Cash_Deposite",
  MARKET_BONDS    : "MarketBonds",
  SAVINGS         : "Savings",
  DIRECT_LOANS    : "Direct loans (P2P)",
  SCI_SCPI        : "SCI_SCPI",
  ETC_COMMODITIES : "ETC_ETC_Commodities",
  CRYPTO          : "Crypto",
  UNLISTED_BONDS  : "UnlistedBonds",
  OPCVM           : "OPCVM",
  EURO_FUNDS      : "EuroFunds",
  MONEY_MARKET_ETF: "MoneyMarketETF"
};

// --- SupportType enumeration ---
const SUPPORT_TYPE = {
  ACCOUNT_BANK  : "AccountBank",
  BOOKLET       : "Booklet",
  PLATFORM      : "Platform",
  CTO           : "CTO",
  PEA           : "PEA",
  LIFE_INSURANCE: "LifeInsurance"
};

// --- Support enumeration ---
const SUPPORT = {
  CTO_TR     : "CTO TR",
  LIVRET_A   : "Livret A",
  LDD        : "LDD",
  TRADE_REP  : "Trade Republic",
  PEA_TR     : "PEA TR",
  SPIRICA    : "Spirica",
  GENERALI   : "Generali",
  PERRBERRY  : "PerrBerry",
  MINTOS     : "Mintos",
  ENERFIP    : "Enerfip",
  BIENPRETER : "BienPrêter",
  LENDOSPHERE: "Lendosphère",
  KRAKEN     : "Kraken"
};

/// --- Risk scale (0=risk free, 1=very low, 4=high) ---
const RISK = {
  RISK_FREE: 0,
  VERY_LOW : 1,
  LOW      : 2,
  MEDIUM   : 3,
  HIGH     : 4
};