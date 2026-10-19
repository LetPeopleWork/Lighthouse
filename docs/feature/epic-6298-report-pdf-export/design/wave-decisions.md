# Wave Decisions — DESIGN — epic-6298-report-pdf-export

**Agent**: Morgan (`nw-solution-architect`) · **Date**: 2026-10-17 · **Mode**: PROPOSE, autonomous subagent ·
**Scope**: application (frontend + `lighthouse-clients`; backend unchanged) · **Predecessor**: DISCUSS (D10–D28 and the
maintainer's answers of 2026-10-17, locked) · **Successor**: DEVOPS (`nw-platform-architect`). Per-wave peer review
skipped by instruction; the consolidated review runs at the end of DISTILL.

**Revised after review 2026-10-19** — see the section of that name below; the decisions listed here are amended by
it where they differ.

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
| `ReportView`, `ThenAndNowHeader`, comparison / mark code, `App` routing, themes, `index.html`, usage-data enum, `useReportOpenedReporter`, E2E `ReportPage` POM, clients' client package, `mcp-core` tools, `lighthouse` skill; (rev. 2026-10-19) `ReportList`, `ReportsTab` | EXTEND (14) |
| Report query key and service, `WidgetBody` looks, summary and panels, owner reads, `TerminologyContext`, backend report endpoints, clients' packing / drift / eval tooling | REUSE (7) |
| Print page, print frame, `printPageStateOf`, print-once hook, `ChangeMarkIcon`, bundled font + licence file, print POM, clients' report reader + header summary, `lh report` group, `lighthouse-reports` skill; (rev. 2026-10-19) export starter, print read port, print effect port; (rev. 2) `reportPrintRoutes` as the print page's composition root | CREATE NEW (14; the report contract fixture was dropped after MQ-3 = b), each justified, with its contract shape, in the feature delta |

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

- AC-1.8 rewording (rev. 2: cancelling returns to where the export started — the report, now showing the fresh
  values, or the Reports list) and the journey's
  produced-date/Now-end checkpoint (instance zone vs viewer zone): `upstream-changes.md`.
- DoD 6 gains the PDF font check from slice 02.
- AC-4.2 stands for the clients; slice 04 stays ~1 day; slice 05 carries the presentation rules (U-5 to U-7).
- CLAUDE.md says React 18; the app is React 19 (not edited here).

## Questions for the maintainer

All answered by the maintainer on 2026-10-17: MQ-1 = (a) the print page itself; MQ-2 = (a) drawn icons everywhere;
MQ-3 = (b) raw values only (against the recommendation of (a)). Options as put are kept in the feature delta.

## Revised after review 2026-10-19

Autonomous revision (Morgan); the maintainer was away and was not asked. Closes the review gate's F-01..F-14 and
folds in the DISTILL walk-through (NMD-1..7, locked). Full text in the feature delta (DESIGN sections, marked
"rev. 2026-10-19"); ADR-245, 246, 247 each carry a "Revised after review 2026-10-19" section.

- **DDD-3 (amended)** Fresh read by mechanism: the print page's own observer uses `refetchOnMount: "always"` (the
  app's `staleTime` is 5 min); fresh = `dataUpdatedAt ≥ mountedAt`; a cached answer from before mount is never
  rendered as the print. The report view's query options are unchanged.
- **DDD-4 (amended)** `printPageStateOf` — a pure function of explicit inputs (fetchedAt, mountedAt, report error,
  owner name/error, terms loading/error, fonts) → preparing / failedRetryable / failedFinal / ready.
- **DDD-13 (amended)** The surface that starts the export emits, before navigating: the report view's button and
  the Reports list's icon, both through one export starter. Exactly one event per click; the print route emits none.
- **DDD-15 / DDD-16 (amended)** No Export PDF for a report this version cannot show (PD-3); the list icon follows the
  row's type only (PD-4); the print chunk is prefetched from the list too.
- **DDD-24** Read-only port for the print page (`Pick` of `getReport` and the owner reads), passed as a prop; no
  write method reachable; type test.
- **DDD-25** Print, after-print and navigation are an injected effect port of the print-once hook.
- **DDD-26** The flag is consumed (entry replaced without it) before print; reload, Forward and StrictMode's double
  effect never print again; consumed only by an actual print.
- **DDD-27** Origin (`report` | `list`) travels with the flag; Back and the after-print return follow it; a final
  failure and a direct load (PD-5) go to the Reports list.
- **DDD-28** Fonts asked for after the document renders, with `document.fonts.load` per face and weight shown.
- **DDD-29** A return to the Reports list may count one tab opening — accepted, so the page-opening detector keeps
  reading the path alone.
- **DDD-30** The print page paints its own light ground (`color-scheme: light`, full height; `html`/`body` white in
  print); the shell's container moves into the shell layout route.
- **DDD-31** A successful Retry prints once (PD-1).
- **DDD-32** Two surfaces, one starter; `ReportListActions` becomes an export action for every reader plus an
  optional editor group; viewers get an Actions column holding the icon alone.
- Earned-trust wording: the E2E checks run on every CI run once slice 01 (walking skeleton) / slice 02 (font check)
  has shipped; the clients' pass-through test proves pass-through, not wire compatibility, until DELIVER swaps the
  hand-written answers for a dev-instance capture; the Quicksand subset's coverage of — − – · … is checked in
  slice 02.
- Example window corrected: the 90-day Now ending 30 Oct 2026 is 2 Aug – 30 Oct 2026.

**Provisional defaults — PROVISIONAL, maintainer confirms**: PD-1 Retry continues the export; PD-2 a renamed panel
reads the term alone ("Lead Time"); PD-3 no Export PDF on the view for a report this version cannot show; PD-4 the
list icon for every row whose type this version can show; PD-5 Back on a directly loaded print page goes to the
Reports list. Story wording changes for the product owner: `upstream-changes.md` U-8..U-15.

## Second revision 2026-10-19

Second revision 2026-10-19: N1–N8, PD-6. Autonomous (Morgan); the maintainer was away and was not asked. Full text in
the feature delta (marked "rev. 2, 2026-10-19") and in each ADR's "Revised after review 2026-10-19" section.

- **DDD-25 / DDD-28 (N1)** Font loading is an effect, so it sits on the effect port: `loadFonts(faces) → "loaded" |
  "failed"`, called by the page after the document rendered; the production binding is the only module naming
  `document.fonts` (and `globalThis.print`). Contract shape: bounded change over the document's `FontFaceSet`. The
  page's only effects outside its own declared set (title, its attribute, one cache entry) now really all go through
  the port, and its tests pass a fake.
- **DDD-25 (N2)** One `afterprint` subscription per mount, released on unmount, independent of the flag and the state;
  clearing the flag must not drop it.
- **DDD-24 (N3, N8)** `reportPrintRoutes` builds the read port as an object literal of exactly `getReport`, `getTeam`,
  `getPortfolio` (bound), and the terms read as `{ getTerm, isLoading, error }`; a runtime test pins both literals'
  keys, because `Pick` is erased at runtime.
- **N4** Cancel returns the reader to where the export started (the report or the Reports list): Changed Assumptions,
  U-1.
- **N5** brief.md's epic-6298 header names this revision, DDD-1..32 and PD-1..PD-6.
- **R35 (N6)** `reportPrintRoutes` is a bounded change (universe: the port literals and the browser binding it passes).
  **DDD-4 (N6)** the report error is an `ApiError` read through its status; 404 / 422 are final.
- **DDD-3 (N7)** The print observer does not retry a 404 / 422 (as `ReportView`'s `retriesUnlessFinal`), the app
  default otherwise; budget: dialog within 2 s of the click on the dev instance, checked by hand.
- **DDD-4 (PD-6)** A fifth state, `failedPart`: the report read did not fail but the owner's name, the terms or the
  fonts could not be loaded → "Part of the page couldn't be loaded, so no PDF was made." with Retry. **PD-6 —
  PROVISIONAL, maintainer confirms.** U-16 for AC-1.11.
- The skill's presentation rules write a percent change as the web does, with a space before the percent sign
  ("−43 %").

## Outcome collision check

Not run: this agent has no shell, so `nwave-ai outcomes check-delta` could not be executed. The coordinating session
runs it.
