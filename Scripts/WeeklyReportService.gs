// =====================================================================
// WeeklyReportService.gs — Weekly portfolio report sent by email
// =====================================================================

// --- Main entry point called by the weekly trigger ---
function rapportHebdomadaire() {
  const history = handleSnapshot("getHistory", {});
  if (!Array.isArray(history) || history.length === 0) {
    Logger.log("No snapshot history available — report not sent.");
    return;
  }

  const lastSnapshot    = history[history.length - 1];
  const variations      = computeVariations(history);
  const assetClassDist  = handleAssetClass("getDistribution", {});
  const supportTypeDist = handleSupportType("getDistribution", {});
  const riskDist        = handleAsset("getDistributionByRisk", {});

  const rows           = getAssetsData();
  const assetCount     = rows.length;
  const portfolioTotal = getPortfolioTotal(rows);

  const averageRisk = portfolioTotal > 0
    ? rows.reduce((sum, row) => {
        const val = row[COL_CURRENT_TOTAL];
        return typeof val === "number" ? sum + row[COL_RISK] * val : sum;
      }, 0) / portfolioTotal
    : null;

  const reportData = {
    date         : lastSnapshot.date,
    portfolioTotal: lastSnapshot.portfolioTotal,
    assetCount,
    averageRisk,
    roi          : computeRoi(lastSnapshot),
    variations,
    assetClassDist,
    supportTypeDist,
    riskDist
  };

  const subject = `Rapport hebdomadaire — Investissements — ${lastSnapshot.date}`;
  MailApp.sendEmail(REPORT_EMAIL, subject, "", { htmlBody: buildReportHtml(reportData) });
  Logger.log("Weekly report sent to " + REPORT_EMAIL);
}

// --- HTML email builder ---
function buildReportHtml(data) {
  const { date, portfolioTotal, assetCount, averageRisk, roi, variations, assetClassDist, supportTypeDist, riskDist } = data;

  const C = {
    primary  : "#37352F",
    secondary: "#787774",
    positive : "#4DAB9A",
    negative : "#E06D6D",
    border   : "#E9E9E7",
    headerBg : "#F7F6F3",
    white    : "#ffffff"
  };

  function fmtEur(val) {
    if (val === null || val === undefined) return "—";
    return new Intl.NumberFormat("fr-FR", { style: "currency", currency: "EUR", minimumFractionDigits: 2 }).format(val);
  }

  function fmtPct(val) {
    if (val === null || val === undefined) return "—";
    return new Intl.NumberFormat("fr-FR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(val) + " %";
  }

  function fmtVariation(val) {
    if (val === null || val === undefined) return `<span style="color:${C.secondary}">—</span>`;
    const color = val >= 0 ? C.positive : C.negative;
    const sign  = val >= 0 ? "+" : "";
    const pct   = new Intl.NumberFormat("fr-FR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(val);
    return `<span style="color:${color}; font-weight:600;">${sign}${pct} %</span>`;
  }

  function pv(key, field) {
    return variations[key] ? variations[key][field] : null;
  }

  function riskLabel(name) {
    const labels = { "0": "0 — Sans risque", "1": "1 — Très faible", "2": "2 — Faible", "3": "3 — Moyen", "4": "4 — Élevé" };
    return labels[String(name)] || String(name);
  }

  function distributionTable(title, items, isRisk) {
    if (!Array.isArray(items) || items.length === 0) return "";
    const tbRows = items.map(item => `
      <tr>
        <td style="padding:7px 10px; border-bottom:1px solid ${C.border};">${isRisk ? riskLabel(item.name) : item.name}</td>
        <td style="padding:7px 10px; border-bottom:1px solid ${C.border}; text-align:right;">${fmtEur(item.currentTotal)}</td>
        <td style="padding:7px 10px; border-bottom:1px solid ${C.border}; text-align:right;">${fmtPct(item.weightInPortfolio)}</td>
      </tr>`).join("");

    const total = items.reduce((s, i) => s + (i.currentTotal || 0), 0);

    return `
      <h3 style="margin:24px 0 8px; font-size:15px; font-weight:700; color:${C.primary};">${title}</h3>
      <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse; border:1px solid ${C.border};">
        <thead>
          <tr style="background:${C.headerBg};">
            <th style="padding:8px 10px; text-align:left; font-size:13px; font-weight:600; border-bottom:1px solid ${C.border};">Nom</th>
            <th style="padding:8px 10px; text-align:right; font-size:13px; font-weight:600; border-bottom:1px solid ${C.border};">Valeur</th>
            <th style="padding:8px 10px; text-align:right; font-size:13px; font-weight:600; border-bottom:1px solid ${C.border};">Poids</th>
          </tr>
        </thead>
        <tbody>${tbRows}</tbody>
        <tfoot>
          <tr style="background:${C.headerBg}; font-weight:600;">
            <td style="padding:7px 10px;">Total</td>
            <td style="padding:7px 10px; text-align:right;">${fmtEur(total)}</td>
            <td style="padding:7px 10px;"></td>
          </tr>
        </tfoot>
      </table>`;
  }

  const base = `font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif; font-size:14px; color:${C.primary}; line-height:1.5;`;

  return `<!DOCTYPE html>
<html lang="fr">
<head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1.0"></head>
<body style="margin:0; padding:0; background:#F9F9F8;">
<div style="max-width:640px; margin:0 auto; padding:24px 16px; ${base}">

  <!-- Header -->
  <div style="background:#37352F; color:#fff; padding:20px 24px; border-radius:8px 8px 0 0;">
    <div style="font-size:18px; font-weight:700;">Rapport hebdomadaire — Investissements</div>
    <div style="font-size:13px; color:#aaa; margin-top:4px;">${date}</div>
  </div>

  <!-- KPIs -->
  <div style="background:${C.white}; padding:20px 24px; border:1px solid ${C.border}; border-top:none;">

    <!-- Valeur totale -->
    <div style="font-size:13px; color:${C.secondary}; margin-bottom:4px;">Valeur totale</div>
    <div style="font-size:28px; font-weight:700; margin-bottom:8px;">${fmtEur(portfolioTotal)}</div>
    <div style="font-size:13px; margin-bottom:16px;">
      S : ${fmtVariation(pv("weekly", "portfolio"))}&nbsp;&nbsp;
      M : ${fmtVariation(pv("monthly", "portfolio"))}&nbsp;&nbsp;
      YTD : ${fmtVariation(pv("ytd", "portfolio"))}&nbsp;&nbsp;
      1A : ${fmtVariation(pv("yearly", "portfolio"))}
    </div>

    <hr style="border:none; border-top:1px solid ${C.border}; margin:0 0 16px;">

    <!-- Actifs + Risque -->
    <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:16px;">
      <tr>
        <td width="50%" style="padding-right:16px;">
          <div style="font-size:13px; color:${C.secondary};">Actifs en portefeuille</div>
          <div style="font-size:22px; font-weight:700; padding-top:4px;">${assetCount}</div>
        </td>
        <td width="50%">
          <div style="font-size:13px; color:${C.secondary};">Risque moyen (0–4)</div>
          <div style="font-size:22px; font-weight:700; padding-top:4px;">${
            averageRisk !== null
              ? new Intl.NumberFormat("fr-FR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(averageRisk)
              : "—"
          }</div>
        </td>
      </tr>
    </table>

    <hr style="border:none; border-top:1px solid ${C.border}; margin:0 0 16px;">

    <!-- ROI -->
    <table width="100%" cellpadding="0" cellspacing="0">
      <thead>
        <tr style="font-size:12px; color:${C.secondary};">
          <th style="text-align:left; font-weight:400; padding:4px 8px 6px 0;">Indicateur</th>
          <th style="text-align:right; font-weight:400; padding:4px 8px 6px;">Valeur</th>
          <th style="text-align:center; font-weight:400; padding:4px 8px 6px;">S</th>
          <th style="text-align:center; font-weight:400; padding:4px 8px 6px;">M</th>
          <th style="text-align:center; font-weight:400; padding:4px 8px 6px;">YTD</th>
          <th style="text-align:center; font-weight:400; padding:4px 8px 6px;">1A</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td style="padding:6px 8px 6px 0; font-weight:600;">ROI Capital Engagé</td>
          <td style="padding:6px 8px; text-align:right; font-weight:600;">${roi ? fmtPct(roi.roiOnCapitalEngaged) : "—"}</td>
          <td style="padding:6px 8px; text-align:center;">${fmtVariation(pv("weekly",  "roiOnCapitalEngaged"))}</td>
          <td style="padding:6px 8px; text-align:center;">${fmtVariation(pv("monthly", "roiOnCapitalEngaged"))}</td>
          <td style="padding:6px 8px; text-align:center;">${fmtVariation(pv("ytd",     "roiOnCapitalEngaged"))}</td>
          <td style="padding:6px 8px; text-align:center;">${fmtVariation(pv("yearly",  "roiOnCapitalEngaged"))}</td>
        </tr>
        <tr>
          <td style="padding:6px 8px 6px 0; font-weight:600;">ROI Total Achats</td>
          <td style="padding:6px 8px; text-align:right; font-weight:600;">${roi ? fmtPct(roi.roiOnTotalPurchases) : "—"}</td>
          <td style="padding:6px 8px; text-align:center;">${fmtVariation(pv("weekly",  "roiOnTotalPurchases"))}</td>
          <td style="padding:6px 8px; text-align:center;">${fmtVariation(pv("monthly", "roiOnTotalPurchases"))}</td>
          <td style="padding:6px 8px; text-align:center;">${fmtVariation(pv("ytd",     "roiOnTotalPurchases"))}</td>
          <td style="padding:6px 8px; text-align:center;">${fmtVariation(pv("yearly",  "roiOnTotalPurchases"))}</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Distribution tables -->
  <div style="background:${C.white}; padding:8px 24px 24px; border:1px solid ${C.border}; border-top:none; border-radius:0 0 8px 8px;">
    ${distributionTable("Répartition par classe d'actifs", assetClassDist, false)}
    ${distributionTable("Répartition par type de support", supportTypeDist, false)}
    ${distributionTable("Répartition par niveau de risque", riskDist, true)}
  </div>

  <!-- Footer -->
  <div style="text-align:center; padding:16px 0; font-size:12px; color:${C.secondary};">
    invest.zapto.fr — Rapport généré automatiquement
  </div>

</div>
</body>
</html>`;
}

// --- Setup weekly trigger — run once manually in Apps Script editor ---
function creerDeclencheurHebdomadaire() {
  ScriptApp.getProjectTriggers()
    .filter(t => t.getHandlerFunction() === "rapportHebdomadaire")
    .forEach(t => ScriptApp.deleteTrigger(t));

  ScriptApp.newTrigger("rapportHebdomadaire")
    .timeBased()
    .everyWeeks(1)
    .onWeekDay(ScriptApp.WeekDay.MONDAY)
    .atHour(8)
    .create();

  Logger.log("Weekly trigger created: every Monday at 08:00");
}
