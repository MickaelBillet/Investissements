// =====================================================================
// SyncWebApp.gs — Manual sync trigger, called by the Api on behalf of
// the dashboard's "Synchroniser" button. Runs the same full ETL as the
// 06h00 trigger (syncCurrentTotal() + today's Snapshot row), so the
// "Capital net engagé" KPI reflects the Bilan immediately instead of
// waiting for the next daily run. Protected by a shared secret
// (Script Property SYNC_SECRET_KEY, never committed to this repo).
// =====================================================================

function doGet(e) {
  const expectedKey = PropertiesService.getScriptProperties().getProperty('SYNC_SECRET_KEY');
  const providedKey = e.parameter.key;

  if (!expectedKey || providedKey !== expectedKey) {
    return jsonOutput({ success: false, error: "Unauthorized" });
  }

  try {
    const result     = snapshotQuotidien();
    const addedCount = typeof result === "number" ? result : 0;
    return jsonOutput({ success: true, addedCount });
  } catch (err) {
    return jsonOutput({ success: false, error: String(err) });
  }
}

function jsonOutput(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}
