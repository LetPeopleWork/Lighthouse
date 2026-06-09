# Upstream Issues — work-item-age-percentiles (Story #5257) DISTILL

No blocking upstream contradictions. The Wave-Decision Reconciliation HARD GATE passed upstream (0 contradictions) and was not re-run. The following are confirmations / sharpenings surfaced while authoring the acceptance tests — none change a story or AC; all are FYI for DELIVER.

## Confirmed contracts (resolve the DESIGN "Open questions")

1. **`BuildPercentiles([])` empty contract (D6) — CONFIRMED, NOT empty array.** `BaseMetricsService.BuildPercentiles` always returns four `PercentileValue` entries (50/70/85/95); for an empty list `PercentileCalculator.CalculatePercentile([], n)` returns `0` (index guard `if (index < 0 || index >= items.Count) return 0`). So **empty WIP ⇒ a four-entry all-zero set**, NOT an empty array. This is the OPPOSITE of `ageInStatePercentiles` (which omits states / returns `[]`). The DISCUSS D6 phrasing "graceful empty state … never crash" is satisfied either way, but the wire contract is the all-zero four-entry array — the FE card empty-state (US-01 AC3) should treat "all four values == 0" as the empty signal, not "array length 0". Pinned in tests 5 (Team) and 3 (Portfolio). DELIVER FE must read the card's empty-state from the all-zero set.

2. **CT percentile ordering / shape — CONFIRMED.** `cycleTimePercentiles` returns the flat `PercentileValue[]` in `BuildPercentiles` emission order: 50, 70, 85, 95 (JSON properties `percentile`, `value`). WIA must match byte-for-byte (test 3, Team). No "low-sample" language exists anywhere for this feature (correctly — D6 has no low-sample gate; a single item yields that one value at every percentile, test 6 Team).

## Sharpening for DELIVER (seam the architect's reuse note implies but did not spell out)

3. **`WorkItemAge` is measured against `DateTime.UtcNow`, NOT `endDate`/`asOfDate`.** `WorkItemBase.WorkItemAge` (`:73`) computes `GetDateDifference(StartedDate ?? CreatedDate, DateTime.UtcNow)` for `Doing` items. The `endDate` parameter only selects WHICH items the snapshot (`GenerateWorkInProgressByDay`/`GetWipSnapshotForTeam`) includes — it does NOT scope the age value. Implication for the service method: passing `endDate` as the `asOfDate` to the in-progress selection is correct (mirrors `cycleTimePercentiles`' date-keyed cache), and `startDate` must be ignored for the population (D4). The golden-value tests therefore anchor seeded `StartedDate` to today and use `endDate = today`; DELIVER's service method and mutation tests must respect that the age is a live "now" measurement, so the `endDate`-only cache key is correct but the cached value is still time-relative (cache TTL is the existing metrics-cache behaviour — no change owed).

No `design/upstream-changes.md` is needed — no AC or story moved.
