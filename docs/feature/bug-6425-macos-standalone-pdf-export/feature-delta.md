# bug-6425-macos-standalone-pdf-export

## Wave: DELIVER / [REF] Maintainer decisions

- 2026-10-27 — RCA confirmed (see `rca.md`). Fix direction: disable Export PDF on the macOS Standalone app with a
  tooltip; no host-print rework.
- 2026-10-27 — Sketch approved: both controls (report list PDF icon, report header "Export PDF" button) render truly
  disabled, wrapped in a `<span>` so the existing MUI `Tooltip` still shows (the `EditButton` pattern in
  `ReportList.tsx`). Copy: "PDF export is not available in the macOS desktop app. To export PDFs on a Mac, run
  Lighthouse with Docker and open it in your browser."
- 2026-10-27 — Rigor profile: `standard` (crafter `sonnet`, reviewer `haiku`). Mutation testing per project gate
  (StrykerJS, touched files).
