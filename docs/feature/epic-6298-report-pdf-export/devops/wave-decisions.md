# Wave Decisions — DEVOPS — epic-6298-report-pdf-export

**Agent**: Apex (`nw-platform-architect`) · **Date**: 2026-10-17 · **Mode**: autonomous subagent, documents only, light.
Per-wave review skipped by instruction. Full reasoning: `../feature-delta.md` → `## Wave: DEVOPS / …`. Machine
artifact: `../environments.yaml`. Design notes: `upstream-changes.md`.

## The nine decisions (brownfield, fixed by the project)

| # | Decision | Answer |
|---|---|---|
| 1 | Deployment target | Server zips (win-x64, linux-x64), Docker image (amd64 + arm64) + Helm chart, Standalone Tauri (Linux / Windows / macOS) |
| 2 | Orchestration | Docker / Helm for those who use it; no chart change |
| 3 | CI/CD | Existing GitHub Actions; **no Lighthouse workflow change**; clients CI: one entry in the pack dry-run list |
| 4 | Existing infrastructure | All reused |
| 5 | Observability | Opt-in usage data (PostHog via backend) + existing logs; nothing added |
| 6 | Deployment | Calver signed release (`/release`); clients separately (changesets, manual `pnpm release:version` + commit) |
| 7 | Continuous learning | N/A, because only one event and one client mapping are added; no flags or experiments |
| 8 | Branching | Trunk-based on `main` |
| 9 | Mutation | `per-feature`, ≥ 80%; `CLAUDE.md` not edited |

## Decisions taken in this wave

- **DVO-1** Export event `ReportPdfExportStarted`, **provisional 20** (the last member on this checkout is
  `WorkTrackingSystemFieldsChanged = 19`). **Carries `report_template`** (closed `UsageDataReportTemplate`, today
  `ThenAndNow`), the same property, wire values and backend mechanism as `ReportCreated` / `ReportOpened`.
  **Answered by the maintainer 2026-10-17**: "would have taken the property, so we can see which reports get printed
  (I'd be curious about this)". This is an explicit exception to name-only-first, like 5878's DVO-2; it serves K6.
  Not a plain event: `UsageDataEventShapes.EventsThatSayWhichTypeOfReport` gains it. A type the browser cannot map
  (`usageDataReportTemplateOf` → `undefined`) reports nothing, as `ReportOpened` does. No owner kind. Emitted on the
  Export PDF click before navigating (DDD-13), once per click; the print route never emits. DELIVER slice 01 re-reads
  the enum on `main` and adds the `docs/settings/usagedata.md` row ("A report was exported as a PDF", sent with the
  report's type). The "Which type of report" field row adds the event, and every other row's "other N events" count
  goes up by one.
- **DVO-2** The clients report the web's `ReportOpened` (with `report_template`, source `Cli` / `Mcp`) on a successful
  `lh report get` / `lighthouse_{team,portfolio}_report_get` of a type they can disclose. List, refusals and unknown
  types report nothing. Serves K4. No backend change.
- **DVO-3** KPI instruments: K1 and K2 per browser through `ReportPdfExportStarted` (K1's denominator is `ReportOpened`
  from source `Browser`); K4 through the clients' `ReportOpened`; K3 manual (conversation log); K5 eval record; G1/G3
  CI (AT + `page.pdf` font check) plus manual PDFs; G2 dogfood timing; G4 `du -sb` before and after slice 02; K6
  (maintainer, 2026-10-17) share of exports per report type from `report_template`, meaningful once a second type
  exists.
  Contracts `OUT-6298-*` appended to `docs/product/kpi-contracts.yaml`.
- **DVO-4** Packaging: `public/licenses/quicksand-OFL.txt` and the woff2 files reach `wwwroot` / server zips, the
  Docker image and the Tauri bundle with **no config change**. No CSP exists to adjust; offline now prints in
  Quicksand; the SBOM picks up the OFL component automatically; `@fontsource-variable/quicksand` 5.3.0 is far past
  pnpm 12's 24 h minimum release age.
- **DVO-5** Clients release: one changeset, minor, for client, cli, mcp-core, mcp-stdio, mcp-http;
  `FEATURE_REQUIRES_SERVER_NEWER_THAN.reports = "v26.10.10.1"` (re-checked at landing against the fields read); no
  dependency on a Lighthouse release. **One clients release carries slices 04 and 05 together (answered by the
  maintainer 2026-10-17, locked)**: slice 05 (skill-only, no changeset) lands before `release:version`. The pending
  `story-6247` changeset goes out with it.
- **DVO-6** CI: no Lighthouse workflow change. Clients `ci.yml` "Pack skills (dry run)" `expected=(…)` gains
  `lighthouse-reports` (slice 05). E2E: one walking skeleton in `ci_verify*`, `window.print` stubbed through the print
  POM, `data-render-ready` wait, `emulateMedia('print')` for print-only styles, `page.pdf` font check from slice 02.
- **DVO-7** Mutation: StrykerJS once after slice 03 on the print page, document, hook, export handler, marks and the
  how-to-read line (bootstrap and the moved theme module excluded, stated); Stryker.NET N/A (an enum member and one entry in a declared set, no logic; the set entry is pinned by the `Fits`
  test);
  clients StrykerJS after slice 04; skill N/A (evals are its gate).
- **DVO-8** Rollback = previous release; nothing stored, no migration, no endpoint. The SPA ships with its backend, so
  event 20 never reaches a server that lacks it.

## For the maintainer: answered 2026-10-17

1. **Export event property** (DVO-1): **answered, the property is kept.** `ReportPdfExportStarted` carries
   `report_template`. In the maintainer's words: "would have taken the property, so we can see which reports get
   printed (I'd be curious about this)".
2. **Clients release timing** (DVO-5): **answered, locked.** One clients release carries slices 04 and 05.
