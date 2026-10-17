# Upstream changes from DEVOPS — epic-6298-report-pdf-export

For DESIGN and the product owner to apply. DEVOPS does not edit their sections. Date: 2026-10-17. Source:
`../feature-delta.md` → "Wave: DEVOPS / [REF] Changed Assumptions".

| # | Artifact | Now says | Should say | Why |
|---|---|---|---|---|
| DU-1 | DESIGN DDD-13, "Usage data" section, Component table (`UsageDataEventName` row); DISCUSS Checklist usage-data row | Export event with closed `report_template`, integer after 19 | `ReportPdfExportStarted` **with closed `report_template`** (as DDD-13 said; the same property and backend mechanism as `ReportOpened`, so `UsageDataEventShapes.EventsThatSayWhichTypeOfReport` gains it), provisional **20** (re-read on `main` at landing). An unmapped type reports nothing | `WorkTrackingSystemFieldsChanged = 19` is already taken. DEVOPS first proposed name-only; **the maintainer kept the property on 2026-10-17**: "would have taken the property, so we can see which reports get printed (I'd be curious about this)". Only the integer changes in DESIGN's text |
| DU-5 | DISCUSS Outcome KPIs | K1–K5 | Add **K6**: which report types get exported (share of exports per type; meaningful once a second type exists), contract `OUT-6298-K6-exported-report-types` | Maintainer, 2026-10-17 |
| DU-6 | DISCUSS D26 / Project DISCUSS Checklist (clients row) | Clients release timing not stated | **One clients release carries slices 04 and 05** (maintainer, 2026-10-17, locked) | A skill-only change triggers no clients release |
| DU-2 | DESIGN "Usage data" / Open questions; slice-04 brief "Usage data"; US-04 | "DEVOPS decides whether the clients report `ReportOpened`" | The clients report `ReportOpened` (`ThenAndNow`, source `Cli` / `Mcp`) on a successful `report get` only. List, refusals and unknown types report nothing. Slice 04 gains this step; `docs/settings/usagedata.md`'s "A report was opened" row is extended at finalize | K4 has no other instrument (D27) |
| DU-3 | DESIGN Reuse R25 ("`pack-skills.sh` … unchanged") | Unchanged | `pack-skills.sh` and the release glob unchanged; **the clients `ci.yml` "Pack skills (dry run)" `expected=(…)` list gains `lighthouse-reports`** in slice 05 | The list is hard-coded, so without the entry the dry run would not prove the new zip |
| DU-4 | DESIGN Open questions: "`reports` version-gate baseline — DELIVER reads it at release" | Open | `v26.10.10.1` (the endpoints first shipped in `v26.10.16.6`); DELIVER re-checks it against the fields the client reads | Git tags on this checkout |
