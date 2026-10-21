# Report PDF Export — Epic 6298

Delivered 2026-10-20 to 2026-10-21 across two code bases: the Lighthouse frontend (slices 01, 02 and the rework in
phase 06) and `lighthouse-clients` (slices 04 and 05). Community, on every licence, no setting, no backend change
beyond one usage-data event. Slice 03 ("How to read this") was built and then dropped by the maintainer.

## Summary

Any report can now be exported as a dated, branded PDF. **Export PDF** sits first on the report view, and a PDF icon
sits first in every Reports list row whose type this version can show, for every reader. The export reads the report
afresh, draws it into a hidden, same-origin frame on the page the reader is on, and opens the browser's print dialog
for that frame alone. The reader never leaves the page and no history entry is added. While the PDF is prepared the
control shows a spinner; when no PDF can be made, the app's error snackbar says why and offers Retry; a preparation
still unfinished after 30 s fails the same way.

The printed page is light whatever the theme, laid out for A4 portrait (Letter works), with a running line on every
page (owner, report, both windows, "Produced {d MMM yyyy, HH:mm}" in the reader's local time), the report's name as
the first page's title, the screen's summary and panels with Then | Now | Change always side by side, no panel split
across pages, and the Lighthouse mark with "Made with Lighthouse by LetPeopleWork · letpeople.work" at the foot of every
page. The marks are drawn icons and Quicksand ships with the app, so the PDF reads the same on every machine and
offline. Nothing is stored.

In the clients, reports can be read through `lh report list|get` and four read-only MCP tools
(`lighthouse_{team,portfolio}_report_{list,get}`) that hand over Lighthouse's raw values unchanged. A new skill,
`lighthouse-reports` (`lighthouse-reports-skill.zip`), lays a Then & Now report out as Lighthouse shows it.

## Business context

People who read a report in Lighthouse need to show it to readers who never open Lighthouse: a sponsor, a steering
meeting, a slide deck. Until now that meant screenshots with a hand-typed date. Job:
`job-share-numbers-outside-lighthouse`. Personas: Elena (consulting coach, Team Admin), Martin (Viewer, Head of
Delivery), Priya (Standalone on Linux), Sabine (sponsor, never opens Lighthouse). North-star KPI K1: at least 20 % of
report-opening browsers export. The maintainer accepted proceeding on one informant (D2).

## Key decisions

### Scope and product (DISCOVER, DIVERGE, DISCUSS)

- **Browser-printed PDF** (DIVERGE option 4) rather than a server renderer; a later server render (#5882) must be
  pixel-comparable, so the print page stays loadable on its own address.
- **Community, branded** (D10). Branding removal is a later Premium extra, not built. Export does not count against
  the reports cap.
- **Anyone who can read the report may export** (D14). No new RBAC guard, endpoint or migration.
- **On demand, never stored** (D15); the export reads the report afresh (D19), and Produced is when that answer arrived.
- **Reports readable through the clients, read-only, raw values** (Q1 = a, MQ-3 = b). Rounding, change and percent
  change live in the skill, which restates the web's rules with their sources named.
- **Skill name `lighthouse-reports`** (Q6). The assistant's own view only when asked, under its own heading (Q3).
- **Quicksand bundled for the whole app** (Q4), from `@fontsource-variable/quicksand` 5.3.0 under OFL-1.1, with
  `quicksand-OFL.txt` shipped in `public/licenses/`.

### Maintainer decisions during DISTILL and DELIVER

- **NMD-1, NMD-2:** an outlined "Export PDF" button first in the report header, and a PDF icon first in the Reports
  list's Actions column for every reader (Viewers get an Actions column holding the icon alone).
- **NMD-8 to NMD-13** (hands-on review of the slice-01 PDF): A4 portrait is the target; three columns on paper whatever
  the width; panels never split; a one-line running header on every page; the footer on every page with no page
  numbers; no empty trailing page.
- **NMD-14:** two A4 pages for a four-panel report is fine.
- **NMD-15:** the reader never sees a print view. Firefox's print dialog does not block the page, so the original
  "navigate to `/print/…` and step back on `afterprint`" left the reader on the print page and broke Back. This
  triggered the phase-06 rework (ADR-255).
- **NMD-16:** waiting shows on the control that started the export; failure in the error snackbar with Retry.
- **NMD-17:** "How to read this" dropped as more noise than value. Slice 03's code (`f7c6679cb`) reverted
  (`950344773`), ADO Story 6318 set to Removed.
- **PD-1 to PD-6** (confirmed 2026-10-20): a successful Retry continues the export; a renamed panel reads the term
  alone; no Export PDF for a type this version cannot show; the list icon on every showable row; a directly loaded
  print page leads back to the Reports list; owner, terms or fonts missing is "Part of the page couldn't be loaded,
  so no PDF was made." with Retry.
- **No screenshots** for this Epic, by the maintainer's decision.

### Accepted behaviours (maintainer, 2026-10-21)

- After Retry, focus goes back to the Export PDF control (being implemented at the time of writing).
- The report view dims while an export prepares its fresh read, and shows a failed read in place: kept for v1.
- If the browser never fires `afterprint`, the hidden frame stays until the next export or until the page is left.
  Kept; there is no timer.
- `ReportOpened` counts once per history entry, so Back and Forward count again. Kept.
- The renamed-terms skill eval is dropped: a report read carries no metric or Work Item terms.
- The K5 skill eval runs happen after the push.

### Architecture (DESIGN, `docs/product/architecture/`)

- **ADR-245:** a printed report is its own chrome-less route per owner kind, reading the report afresh through a
  read-only port of three reads and reaching the browser through one effect port. Its navigation, print flag and
  return are superseded by ADR-255.
- **ADR-246:** everything a report draws ships with the app: drawn change marks, Quicksand bundled.
- **ADR-247:** reports are read through the clients as four read-only tools returning raw values.
- **ADR-255:** Export PDF prints from a hidden frame the running app draws the print page into (React portal, the
  frame's own Emotion cache, the light theme, a copy of the app's style sheets), so there is no second boot and the
  fresh read lands in the cache the report view reads. A 30-second budget ends a stalled preparation.

### DEVOPS

- **Usage data:** `ReportPdfExportStarted` with `report_template` (today `ThenAndNow`), appended to
  `UsageDataEventName`, counted on the click from either entry point, before the dialog; listed in
  `docs/settings/usagedata.md`. The clients report the web's `ReportOpened` on a successful `get`, after consent.
- **Release once the whole Epic is done** (DVO-9). Rollback is the previous release: nothing stored, no migration.

## Work completed

All steps in `deliver/execution-log.json` reached RED, GREEN and COMMIT with PASS.

| Phase | Steps | What it delivered |
| --- | --- | --- |
| 01 Export a Then & Now report as a PDF (walking skeleton) | 01-01 … 01-18 | The usage event; the print page with its pure state function, print-once hook, browser binding and read-only ports; fresh read; light chrome-less ground; Export PDF on the report view and the Reports list icon, counted once per choice; E2E walking skeleton |
| 02 The PDF reads the same everywhere | 02-01 … 02-06 | Drawn marks; Quicksand bundled with its licence; page breaks and 12 mm margins; running header and footer on every page; waiting for the Lighthouse mark; E2E check that the PDF embeds only Quicksand |
| 03 How to read this | 03-01 | Built, then reverted (NMD-17) |
| 04 Read reports through the clients (`lighthouse-clients`) | 04-01 … 04-06 | `lh report list|get`, four MCP tools over stdio and HTTP, refusals in the reader's words, `ReportOpened` after consent |
| 05 The `lighthouse-reports` skill (`lighthouse-clients`) | 05-01 … 05-03 | The skill and its Then & Now reference, twelve eval cases against captured reports, the CI pack dry run expecting the zip |
| 06 Print from a hidden frame | 06-01 … 06-09 | Snackbar action and dismissal; window-bound print effects and the hidden frame; print-once reporting to its host; both surfaces printing from the frame; Retry from a fresh frame; the 30-second budget; the print route never prints; E2E walking skeleton |

Lighthouse commits run from `48852f11d` to `bb0404856`, plus the review and mutation commits; not yet pushed at the
time of writing. The clients' commits are pushed, not yet released.

## Quality gates

- **Mutation, frontend:** first run 79.01 % (715 / 905); after one kill round **83.87 %** (759 / 905), tests only.
  Record: `mutation/frontend.md`.
- **Mutation, clients:** 70.41 % before the kill round, **97.61 %** after, every file at or above 91 %. Record:
  `mutation/clients.md`.
- **Manual check:** the maintainer hand-checked Export PDF in Chrome and Firefox on 2026-10-21: all good.

## Lessons learned

- **A print dialog is not the same in every engine.** Chrome blocks inside `print()`; Firefox does not. A design that
  depended on returning after the dialog worked in one and broke Back in the other. Printing a hidden frame removes
  the return altogether.
- **Hands-on review of the real artifact changes the design.** Printing real reports produced six layout decisions
  (NMD-8 to NMD-13) that no sketch had raised, and later dropped a whole slice (NMD-17).
- **Raw values in the clients move every presentation rule into the skill.** The skill restates the web's rounding,
  units and change rules, each with its source named; drift is guarded only by its evals and a DELIVER checklist line.

## Issues encountered

- The navigate-and-return mechanism (ADR-245 decision 4) failed in Firefox and was replaced mid-DELIVER by phase 06.
- The frontend mutation run took 55 minutes for 905 mutants and landed just under the gate.

## Open items

- **Standalone hand check, owed by the maintainer:** the hidden-frame export has been checked only in Chrome and
  Firefox. It has not been checked in the Standalone app on any OS (the earlier Linux probe covered the previous
  whole-page mechanism). Check on Linux, macOS and Windows; the user docs ask readers to report a failure.
- Frontend mutation: the usePrintOnce "dialog opened" flag is the likeliest real gap left (a dialog that opens, closes, and the frame must go); see `mutation/frontend.md`.
- K5 skill eval runs, after the push.
- Clients release (one release carries slices 04 and 05).

## Finalize checklist

| Item | Answer |
| --- | --- |
| User docs | `docs/reports/reports.md` (Export PDF), `docs/reports/thenandnow.md` (In the PDF), `docs/aiintegration.md` (Read a Report, Skill for Reports, the zip), `docs/settings/rbac.md` (export row), `docs/settings/usagedata.md` (export event; clients' `ReportOpened`) |
| Screenshots | None, by the maintainer's decision |
| Website | Features table row for PDF export; the Reports skill on the AI integration section, shown "Coming soon" until a clients release carries the zip |
| Lighthouse-Clients | Pushed, not yet released |
| RBAC | No new permission: anyone who can read a report can export it |
| Migrations | None |

## Migrated out of the workspace

Nothing was moved. The workspace has no `design/architecture-design.md`, `component-boundaries.md`,
`technology-stack.md` or `data-models.md`, no `design/adrs/`, no `distill/walking-skeleton.md` and no
`discuss/journey-*` files. The lasting artifacts were written to permanent places from DESIGN on:

- ADR-245, ADR-246, ADR-247 and ADR-255 in `docs/product/architecture/`.
- The Epic's section in `docs/product/architecture/brief.md` ("Application Architecture —
  epic-6298-report-pdf-export") and the root `ARCHITECTURE.md`.
- The journey `docs/product/journeys/epic-6298-report-pdf-export.yaml` and the KPI contracts `OUT-6298-*` in
  `docs/product/kpi-contracts.yaml`.

The workspace `docs/feature/epic-6298-report-pdf-export/` stays in place as delivery history: the feature delta, the
slice briefs, the wave decisions, the roadmap, the execution log and the mutation records. Nothing was deleted.

## ADO

Epic #6298 with one Story per slice; Story 6318 (slice 03) Removed. The Epic's state is the maintainer's to move.
