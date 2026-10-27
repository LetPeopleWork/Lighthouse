# Upstream changes — DEVOPS → DESIGN — epic-6350-bulk-reports

Raised by DEVOPS on 2026-10-27. **APPLIED 2026-10-27**, after the maintainer answered DVQ-1 (a): DU-1 to DU-4 edited
into the DESIGN sections of `../feature-delta.md` (revision note, DDD-9, DDD-13, the usage placement table),
`../design/wave-decisions.md` and ADR-266 (items 5 and 8); AC-5.3 and AC-7.8 and slice briefs 01, 02, 05, 06, 07
updated to match.

| # | Where | Was | Proposed | Why |
|---|---|---|---|---|
| DU-1 | DDD-9, the bulk-removal contract | A report already gone counts as `removed` | Still `status: removed` (AC-5.3 unchanged on screen), plus `code: "already-gone"` on that outcome. The browser fires `ReportDeleted` only for `removed` outcomes **without** a code | The single path (`useReportDeletion`) never reports a 404 as a deletion; without the marker two people removing the same report would count it twice, and the usage-data page row ("your server removed it") would be untrue |
| DU-2 | Usage data placement, "Reports page opened … (the `useReportOpenedReporter` rule)" | Implies the 5 s dwell | Once per history entry, counted when the list is shown, **no dwell** (DVQ-1 recommendation; 5 s dwell if the maintainer picks DVQ-1 (b)) | The page is a launcher; a quick find-and-open is the use K1 counts |
| DU-3 | Usage data placement, "Several reports in one PDF" | One event on the click | `ReportCombinedPdfExportStarted` when ≥ 2 are selected; exactly one selected fires the existing `ReportPdfExportStarted` with that report's type | One report makes the same PDF as the single export; a "several" event for it would overcount K4 |
| DU-4 | DDD-13 observability | One `report-bulk-run` Information line | Same line with fixed fields (`Action`, `TemplateKey`, window lengths, `Asked`, `Done`, `NotDone`, `RefusedByCode`, `ElapsedMs`; no names), emitted as `report-bulk-run-slow` Warning above 30 s (create) / 10 s (remove); plus `report-list-slow` and `report-creation-options-slow` Warnings above 2 s | G2 (≤ 2 s) has no signal from real instances otherwise; mirrors 5878's `report-read-slow` |
