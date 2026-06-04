// --- Compute ROI from a snapshot ---
function computeRoi(snapshot) {
  if (!snapshot || snapshot.totalPurchases === null || snapshot.totalReturns === null) return null;
  const tp = snapshot.totalPurchases;
  const pt = snapshot.portfolioTotal;
  if (!tp || !pt) return null;
  const netReturn = pt + snapshot.totalReturns - tp;
  return {
    roiOnTotalPurchases: netReturn / tp * 100,
    roiOnCapitalEngaged: netReturn / pt * 100
  };
}

// --- Compute S / M / YTD / 1A variations from snapshot history ---
function computeVariations(history) {
  if (history.length < 2) {
    return { weekly: null, monthly: null, ytd: null, yearly: null };
  }

  const last     = history[history.length - 1];
  const lastDate = new Date(last.date);
  const lastRoi  = computeRoi(last);

  const periods = {
    weekly : { date: new Date(lastDate.getFullYear(), lastDate.getMonth(), lastDate.getDate() - 7),  firstOfPeriod: false },
    monthly: { date: new Date(lastDate.getFullYear(), lastDate.getMonth(), lastDate.getDate() - 30), firstOfPeriod: false },
    ytd    : { date: new Date(lastDate.getFullYear(), 0, 1),                                          firstOfPeriod: true  },
    yearly : { date: new Date(lastDate.getFullYear(), lastDate.getMonth(), lastDate.getDate() - 365), firstOfPeriod: false }
  };

  const result = {};
  for (const [key, { date, firstOfPeriod }] of Object.entries(periods)) {
    const ref = findRefSnapshot(history, date, firstOfPeriod);
    if (!ref || !last.portfolioTotal || !ref.portfolioTotal) {
      result[key] = null;
      continue;
    }
    const refRoi = computeRoi(ref);
    result[key] = {
      portfolio: (last.portfolioTotal - ref.portfolioTotal) / ref.portfolioTotal * 100,
      roiOnTotalPurchases: (lastRoi && refRoi && refRoi.roiOnTotalPurchases !== 0)
        ? (lastRoi.roiOnTotalPurchases - refRoi.roiOnTotalPurchases) / Math.abs(refRoi.roiOnTotalPurchases) * 100
        : null,
      roiOnCapitalEngaged: (lastRoi && refRoi && refRoi.roiOnCapitalEngaged !== 0)
        ? (lastRoi.roiOnCapitalEngaged - refRoi.roiOnCapitalEngaged) / Math.abs(refRoi.roiOnCapitalEngaged) * 100
        : null
    };
  }
  return result;
}

// --- Find reference snapshot for a given period ---
// firstOfPeriod=true  → earliest snapshot at or after targetDate (YTD)
// firstOfPeriod=false → latest snapshot at or before targetDate (S/M/1A)
function findRefSnapshot(history, targetDate, firstOfPeriod) {
  if (firstOfPeriod) {
    for (let i = 0; i < history.length - 1; i++) {
      if (new Date(history[i].date) >= targetDate) return history[i];
    }
    return null;
  }
  for (let i = history.length - 2; i >= 0; i--) {
    if (new Date(history[i].date) <= targetDate) return history[i];
  }
  return null;
}