# Wave Decisions — DESIGN — epic-6298-report-pdf-export

**Agent**: Morgan (`nw-solution-architect`) · **Date**: 2026-10-17 · **Mode**: PROPOSE, autonomous subagent ·
**Scope**: application (frontend + `lighthouse-clients`; backend unchanged) · **Predecessor**: DISCUSS (D10–D28 and the
maintainer's answers of 2026-10-17, locked) · **Successor**: DEVOPS (`nw-platform-architect`). Per-wave peer review
skipped by instruction; the consolidated review runs at the end of DISTILL.

Full text: `../feature-delta.md` → "Wave: DESIGN" sections. ADRs: ADR-245, ADR-246, ADR-247 in
`docs/product/architecture/`. Brief: `docs/product/architecture/brief.md` → "Application Architecture —
epic-6298-report-pdf-export".

## Key Decisions

- **DDD-1** The print-designed report is its own chrome-less route per owner kind, outside a new app-shell layout
  route (ADR-245).
- **DDD-2** Export navigates in the same window and prints once the page is ready, returning on `afterprint`; a
  screen-only Back control is always there (ADR-245). **Decided by the maintainer 2026-10-17 (MQ-1 = a).**
- **DDD-3** Fresh read with the report view's own query key; Produced = when that answer arrived, in local time.
- **DDD-4** Ready = fresh answer ∧ owner name ∧ terms ∧ fonts, exposed as `data-render-ready`; failure never prints.
- **DDD-5** Light theme forced by a theme module moved out of `main.tsx`.
- **DDD-6** Print stylesheet: 12 mm margins, no fixed paper size, no split panels, footer kept with the last panel.
- **DDD-7** Document title names the report and its owner (the dialog's default file name).
- **DDD-8** The print body reuses the screen's summary and panels.
- **DDD-9** Marks are drawn SVG icons from a mark kind (ADR-246).
- **DDD-10** Same icons on screen and in print (ADR-246; slice 02 covers summary and panels on screen and in print).
  **Decided by the maintainer 2026-10-17 (MQ-2 = a).**
- **DDD-11** Quicksand bundled for the whole app via `@fontsource-variable/quicksand` 5.3.0, OFL-1.1, ≈64 KB of woff2
  (ADR-246; licence verified against Google Fonts' `OFL.txt`, `METADATA.pb` and the package's own `LICENSE`).
- **DDD-12** `quicksand-OFL.txt` ships in `public/licenses/`, reaching every distribution; guarded byte-equal.
- **DDD-13** The export usage event is emitted on the click, never by the print route (event itself: DEVOPS).
- **DDD-14** A return from print does not report a second `ReportOpened`.
- **DDD-15** Export availability follows the report view's existing status; no new RBAC gate.
- **DDD-16** The print chunk is prefetched to keep the dialog within 2 s.
- **DDD-17** Clients: `lighthouse_{team,portfolio}_report_{list,get}`, `lh report list|get`, version-gated client
  methods (ADR-247).
- **DDD-18** The client read returns the report's raw values unchanged; the summary names report, owner and windows
  only; nothing is rounded, compared or judged in the clients; no shared phrasing fixture (ADR-247). **Decided by the
  maintainer 2026-10-17 (MQ-3 = b, against the recommendation).**
- **DDD-19** Refusal relays: 403 no access, gate → upgrade, 422 → newer version, 404 → no such report.
- **DDD-20** Skill `lighthouse-reports`: `SKILL.md` + one reference file per report type + evals; zip by the existing
  script. `references/then-and-now.md` carries the web's presentation rules (order, direction of good, lines and
  decimals, units, rounding, change and percent, "No change" / "Then was 0" / dashes, summary groups, absence
  reasons), each naming its source file and symbol under `Lighthouse.Frontend/src/pages/Common/Reports/`; never more
  precision than the web shows.
- **DDD-21** No backend change except one usage-data enum member (DEVOPS).
- **DDD-22** No server rendering, render token or native print call in this Epic.
- **DDD-23** The skill is held to the web by its evals: fixtures are real dev-instance report answers paired with the
  lines the Reports tab shows for them; a DELIVER checklist line re-runs the evals and updates the reference file on
  any change to the named web sources or a new Then & Now metric. Drift between web and skill is an accepted risk.

## Architecture Summary

- Pattern: unchanged — ports-and-adapters backend (untouched), component + hook frontend over the existing API service
  adapters; modular `lighthouse-clients` packages.
- Paradigm: OOP project; the frontend part is hooks and components as today.
- Key components: `ReportPrintPage` and `ReportPrintDocument` (new), the ready / print-once hook (new),
  `ChangeMarkIcon` (new); `App`, `ReportView`, `ThenAndNowHeader`, the comparison code and the theme module extended.
  Clients: report methods, tools and `lh report` group, a header-only summary, the `lighthouse-reports` skill with the
  web's presentation rules.

## Reuse Analysis

| Existing | Verdict |
|---|---|
| `ReportView`, `ThenAndNowHeader`, comparison / mark code, `App` routing, themes, `index.html`, usage-data reporter and enum, `useReportOpenedReporter`, E2E `ReportPage` POM, clients' client package, `mcp-core` tools, `lighthouse` skill | EXTEND (12) |
| Report query key and service, `WidgetBody` looks, summary and panels, owner reads, `TerminologyContext`, backend report endpoints, clients' packing / drift / eval tooling | REUSE (7) |
| Print page, print frame, ready / print-once hook, `ChangeMarkIcon`, bundled font + licence file, print POM, clients' report reader + header summary, `lh report` group, `lighthouse-reports` skill | CREATE NEW (9; the report contract fixture was dropped after MQ-3 = b), each justified in the feature delta |

## Technology Stack

- `@fontsource-variable/quicksand` 5.3.0 — OFL-1.1 — new runtime dependency (font files only).
- `@mui/icons-material` 9 — MIT — installed.
- TanStack Query 5 — MIT — installed.
- Browser / webview print — no library.

## Constraints Established

- Nothing is stored; no endpoint, DTO, migration or RBAC guard is added.
- The print route never prints unless opened by the Export action, and never before it is ready.
- The print route emits no usage data.
- The report's marks never depend on a font; the app makes no runtime request for fonts.
- The clients pass the report's values through unchanged; the web's presentation rules are restated once, in the
  skill's reference file, with their sources named, and held to the web by web-paired evals.

## Upstream Changes

- AC-1.8 rewording (cancelling returns to the report, now showing the fresh values) and the journey's
  produced-date/Now-end checkpoint (instance zone vs viewer zone): `upstream-changes.md`.
- DoD 6 gains the PDF font check from slice 02.
- AC-4.2 stands for the clients; slice 04 stays ~1 day; slice 05 carries the presentation rules (U-5 to U-7).
- CLAUDE.md says React 18; the app is React 19 (not edited here).

## Questions for the maintainer

All answered by the maintainer on 2026-10-17: MQ-1 = (a) the print page itself; MQ-2 = (a) drawn icons everywhere;
MQ-3 = (b) raw values only (against the recommendation of (a)). Options as put are kept in the feature delta.

## Outcome collision check

Not run: this agent has no shell, so `nwave-ai outcomes check-delta` could not be executed. The coordinating session
runs it.
