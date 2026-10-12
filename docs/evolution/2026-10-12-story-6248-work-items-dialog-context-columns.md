# Story 6248 — the Work Items dialog brings the columns that explain where it was opened

**Pushed to `main` 2026-10-12, not yet released** · ADO User Story #6248 *"Additional Column(s) in Work Items
Dialog based on Context"* (no parent Epic, tagged `Release Notes`) · workspace
`docs/feature/story-6248-work-items-dialog-context-columns/` (kept as delivery history) · Lighthouse commits
`569963951..de178193e` (46 commits, `b369cc03f` … `de178193e`), then the finalize commits (docs, screenshots,
release note, this record) · ADRs
[ADR-234](../product/architecture/adr-234-the-work-items-dialog-takes-its-columns-from-one-catalogue-and-its-defaults-from-one-map.md)
and
[ADR-235](../product/architecture/adr-235-a-work-item-carries-its-estimate-normalised-by-the-code-the-estimation-chart-uses.md)

## Why

Steve, from the community, clicked a bubble on the Estimation vs. Cycle Time chart and got a table without the
estimate: *"this datatable doesn't really provide enough detail to be more informative than the chart or help
with a drill-down"*. The bubble existed because of its estimate, and the list behind it did not show one.

The cause was general. One dialog served sixteen call sites, with ID, Name, Type and State fixed and one
`highlightColumn` that meant Cycle Time, Age, Size or Days Contributed depending on the caller. The rows already
carried started and closed dates, parent, blocked-since and time in state, but no dialog could show them, and the
estimate never reached a row at all. Every dialog also shared one saved layout, so hiding a column in one chart's
list hid it in all of them. The maintainer's call: rework the dialog so it always opens with the columns that
matter where it was opened; estimation is one case of many.

## What users get

- **Every dialog opens with the columns that explain its point.** Estimation vs. Cycle Time shows Estimate, Cycle
  Time and Closed; Blocked lists show Work Item Age and Blocked since; Feature Size sorts by Size; Work
  Distribution shows the parent by name; a Feature's child items show Started, Closed, Work Item Age, Cycle Time
  and Estimate. One declared map holds every context's defaults, so changing one is a one-line edit.
- **Manage columns offers the rest.** The dialog knows every column a row can fill (Started, Closed, Work Item
  Age, Cycle Time, Parent, Blocked since, Time in State, named Cycle Times, and for Feature rows Size and
  Forecasted Completion). Those outside the context's defaults are hidden until turned on. The Estimate is
  offered only where the Team or Portfolio has an estimation field configured.
- **The layout is remembered per chart, for Teams and Portfolios separately.** A change in one kind of list
  never leaks into another; *Reset layout to defaults* returns that one to its defaults. Only the user's
  overrides are saved, so a later change to a context's defaults still reaches every column the user never
  touched.
- **Loading and failure looks** for the two dialogs that fetch their own items (Cumulative Time per State and a
  Feature's child items): the dialog opens at once with its headers and the grid's loading overlay, says "These
  Work Items couldn't be loaded. Close and reopen to try again." when the load fails, and a late answer for an
  earlier click never lands in the next dialog.
- **For agents through MCP**, every work-item row now carries `estimate` (value, display value, unit) where the
  owner has estimation configured.

## What shipped, slice by slice

| Slice | What | Main commits |
|---|---|---|
| 01a — the estimate on the row | `EstimateOf` / `EstimatesOf` normalise one item's estimate exactly as the Estimation chart does; `WorkItemEstimateDto` carries it on work-item rows at the 12 places where the owner is known (Team rows, Portfolio rows, a Feature's child items with each item's own Team's estimate). The CSV connector reads configured additional-field columns (D-01-07), and Team Zenith's demo data gains a `Story Points` column configured as its estimation field (S7). | `929c916e1` … `f518def14` |
| 01b — the dialog machinery | `DataGridBase` gains default column visibility with override-only persistence; one declared map of each context's defaults; the column catalogue (judgement columns moved into it); per-context and per-owner-kind layout keys; the `estimation` context with the Estimate column. | `69a19a80a` … `f90ae7229` |
| 02 — every chart context | Feature rows offer Size and Forecasted Completion; judgements (SLE colouring, Age band, SLE risk) keep their look under each context's sort; caller-supplied age day, named Cycle Times and Days Contributed; every chart and View Data opens its context; Parent shows the parent's name; Cumulative Time per State opens at once with its loading and failure look. | `2bc456936` … `ab463b7f6` |
| 03 — a Feature's child items | Child-items and Delivery timeline contexts; the child items load through a keyed query with loading and could-not-load looks; the Delivery timeline opens its context (Warnings kept, no Estimate, no Blocked since); `highlightColumn` removed, so every dialog opens a context. | `7fd500740` … `21a2aa788` |

## Key decisions

- **D1–D6 (DISCUSS, maintainer):** a full catalogue with context-picked defaults; catalogue = what rows carry plus
  the backend Estimate, no tags; layout per context; the defaults table locked and declared in one place; clients
  N/A; no Estimate column where estimation is not configured.
- **H1 — plain headers (DELIVER, maintainer, 2026-10-11).** No units in any header; the Estimate header is plain
  `Estimate` even when a unit is set or units are mixed. This superseded the `Estimate (Story Points)` header that
  DISTILL had pinned. The row still carries the unit for the MCP tools.
- **H2 — Work Item Age and Cycle Time replace the combined column (maintainer review, 2026-10-12).** The old
  combined "Work Item Age / Cycle Time" cell mixed two measures. The contexts that used it show both side by side,
  Age first, sorted by Age (Feature Size keeps Size); a finished item shows no Age and an unfinished one no Cycle
  Time, never a 0.
- **S7 — demo data carries an estimate.** Team Zenith has Story Points configured for estimation, so the E2E
  walking skeleton runs on demo data and configures nothing.
- **D-01-07 — the CSV connector reads additional fields (autonomous, AFK).** DESIGN assumed the demo CSV
  connection could carry an additional field; it could not (95 rows, 0 mapped). The step was widened so the CSV
  connector fills configured additional fields from matching header columns. Not customer-visible: the
  connection settings hide the additional-fields editor for CSV, so only demo data defines one. CSV docs
  unchanged.
- **R1 — slice 01 split** into 01a (backend, demo data; nothing changes on screen for a customer) and 01b (the
  dialog), each shippable alone. R2: Blocked since not offered on the Delivery timeline (its rows can never carry
  it). R3: one accepted exposure, see RBAC below. R4: a column turned on appears in its place in the fixed order.

## Quality gates after DELIVER

- **Refactor pass, 6 commits** (`21a2aa788`, `a8f8faa77`, `7c47a8f66`, `d46314b0b`, `b6c122f82`, `8e368b464`, plus
  the comment-only `1d1009dd4`): `highlightColumn` gone, every dialog names whose items it lists, the catalogue
  lays out a context in one pass, one reading of a query as the dialog's rows.
- **Adversarial review.** Three major findings and one minor fixed:
  - M1 `9d61098ad` — WIP Over Time lists showed an empty Age (the sort column) for every item finished since; they
    now read each item's age on the day the list describes.
  - M2 `faf07ce5c` — Time in State was a default even where the caller passed no way to measure it; it is now
    offered only where the caller does.
  - M3 `fe61c2641` — an item not started yet read as brand new (age 0); it now shows no age.
  - m1 `72fbf190e` — a non-finite numeric estimate (`NaN`, `Infinity`, `1e999`) turned every row endpoint into a
    500, because System.Text.Json refuses to write it; it now counts as invalid.

  Deferred minors:
  - m2 — a European decimal comma in a numeric estimate is read as a thousands separator, and rows now make that
    visible (the chart already did it).
  - m3 — a grid's default visibility was fixed at first render. The reviewer called it unreachable; mutation
    testing later proved it reachable and it was fixed (`b18534094`, below).
  - m4 — the parent-name lookup never refetches: after a failure each newly scrolled-in Parent cell asks again,
    and after a success a renamed parent stays stale. No wrong data.
  - m5 — the Blocked Over Time dialog for a past day shows today's age, not the age on that day (no `ageOn`). Not a
    regression: the dialog had no age column before.
- **Mutation testing** (`mutation/results.md`): backend Stryker.NET **100 %** on the story's lines (17/17; whole
  files 67.92 %, survivors all in untouched code); frontend StrykerJS **92.99 %** (544 of 585; 82.85 % before
  triage). The run found a real bug no test had caught, see Lessons.

## Lessons learned

- **A DESIGN assumption about a connector went unchecked until DELIVER.** DESIGN took for granted that the demo
  CSV connection could carry an additional field; the connector never filled one. A one-line live check during
  DESIGN ("does any demo item have this field mapped?") would have surfaced it two waves earlier.
- **A step agent amended a commit with `--no-verify`.** It slipped past the hooks the slice ritual relies on.
  Step agents must create new commits, never amend, never bypass hooks; the orchestrator should check the
  commit range after each step rather than trust the hand-back.
- **Mutation found a bug no test caught, and the reviewer had called it unreachable.** `DataGridBase` built its
  whole visibility model once at mount. The Work Items dialog mounts while rows are still loading and computes
  defaults from the rows, so a Portfolio's in-progress list opened while loading showed Size and Forecasted
  Completion, which its context hides. The surviving mutant was the `applyVisibility` dependency array. The grid
  now keeps only the user's overrides and lays the current defaults under them on every render (`b18534094`). A
  surviving mutant on a hook's dependency list deserves a scenario, not an "equivalent" label.
- **Same-shaped review findings cluster.** M1 and m5 are the same past-day gap in two charts; a finding should
  prompt a search for its siblings before it is closed.

## Known gaps

- **No E2E opens a Feature's child-items dialog.** The walking skeleton covers the Estimation dialog on Team
  Zenith; the child-items contexts are pinned by component tests only.
- **A Portfolio's in-progress list never shows SLE risk**, though the context's defaults name the column; it is
  only ever filled for Teams.
- **Parent names are cached for the session** (m4): a parent renamed in the tracker keeps its old name in the
  dialog until the cache expires.
- **`ageOn` counts days on UTC day boundaries**, not the instance's time zone, so near midnight an age on a past
  day can be one off from what the instance's other day-based numbers say.
- **The aging View Data list has no Time in State column.** It never had one, but the commit message of
  `faf07ce5c` says it does; only the Work Item Aging chart's own click and the in-progress and Stale View Data
  lists pass the measure.
- m2, m4 and m5 above.

## Phase B — lasting artifacts

No lasting artifacts to migrate. The workspace is a single `feature-delta.md` (no `design/`, `distill/` or
`discuss/` subfolders); its lasting outputs already live in permanent places: ADR-234 and ADR-235 under
`docs/product/architecture/`, and the job `job-flow-coach-see-why-each-item-sits-behind-the-point` in
`docs/product/jobs.yaml`. The workspace stays as delivery history.

## Finalize checklist

| Item | Answer |
|---|---|
| RBAC | **No permission change; one accepted exposure (R3).** No endpoint, permission or `my-summary` read was added. A Feature's child items carry their own Team's estimate, so a reader who can see the Feature sees those values and that Team's estimation unit without access to the Team. Accepted by the maintainer in DISTILL review round 1. |
| Lighthouse-Clients (CLI + MCP) | **No change, no release.** The CLI ignores `estimate`; the MCP tools relay a row's JSON as it comes, so agents now see `estimate`. No tool input or name changed, and the clients have no strict response schemas. |
| Website | **N/A, re-checked.** `git grep` in the website repo finds no reference to `workitemsdialog.png`, `blockedOverTimeDrilldown.png` or `sle_risk_column.png`; it hot-links other `features/metrics/` images, not these. |
| Premium | **N/A, because** the dialog is free; CSV export stays premium and exports the visible columns, as before. |
| Docs | **Done.** `docs/metrics/widgets.md` gains *Columns in the Work Items Dialog* (context defaults, Manage columns, Reset layout to defaults, layout per chart for Teams and Portfolios). The Estimation vs. Cycle Time section lives in `docs/metrics/portfolio-features.md`, not `flow-metrics.md` as DISCUSS assumed; it now says the list shows each item's Estimate. Work Distribution's *Viewing Details* names the parent and the split Age / Cycle Time columns; `flow-overview.md`'s closed-items passage names Closed and Cycle Time. The demo-data docs describe scenarios, not Team settings, so Team Zenith's estimation field needs no mention. |
| Screenshots | **Done, three regenerated** against a fresh backend with demo data: `features/metrics/workitemsdialog.png` (Closed Work Items: Closed and Cycle Time, sorted by Cycle Time with SLE colouring), `blockedOverTimeDrilldown.png` (Work Item Age and Blocked since), `sle_risk_column.png` (in-progress list with plain headers: Work Item Age, Age band, SLE Risk). |
| E2E | **Walking skeleton green, one gap.** `WorkItemsDialogContextColumns.spec.ts` opens Team Zenith's Estimation vs. Cycle Time, clicks a bubble and reads the Estimate column by header. No E2E opens a Feature's child-items dialog (Known gaps). |
| Usage data | **N/A, by the maintainer's choice in DEVOPS (2026-10-10)**, over a name-only `WorkItemsDialogColumnShown`. Nothing was appended to `UsageDataEventName` and nothing was owed: catalogue adoption is unmeasured, and the KPI `OUT-6248-catalogue-used` was retired. |
| Demo data | **Done in slice 01a.** Team Zenith's CSV has a `Story Points` column, registered as an additional field and configured as the Team's estimation field. |
| Release note | **Added** to `docs/releasenotes/releasenotes.md` under a new `# Lighthouse vNext`, with Steve Pereira credited. |

## Release note (draft)

> **Clicking a chart now lists the Work Items with the columns that explain why they are there.** The Estimation
> vs. Cycle Time dialog shows each Work Item's Estimate next to its Cycle Time, the Blocked list shows since when
> each Work Item is Blocked, a Feature's child items show when each started and closed, and **Manage columns**
> offers the rest; the layout you choose is remembered for each chart.

Asked for by Steve Pereira, who found the list behind an Estimation bubble told him less than the chart did.
