# Epic 5878 — Then & Now reports: a frozen "before" beside today's flow

**Pushed to `main` 2026-10-13 … 2026-10-15, not yet released** · ADO Epic #5878 *"Baseline"* (tag `ValueFlow`,
Community), Stories #6159–#6166 and #6168 (slices 01–08 and 10; slice 05b has no item of its own and is tracked
under #6163), Story #6305 (slice 12); #6167 (slice 09) and #6169 (slice 11) were Removed · workspace
`docs/feature/epic-5878-baseline/` (kept as delivery history) · DELIVER commits `1a60b93f7..6089fe795`, then the
finalize commits (docs, screenshots, release note, this record) · ADRs
[ADR-219](../product/architecture/adr-219-a-report-is-an-owner-scoped-record-with-a-template-payload.md),
[ADR-220](../product/architecture/adr-220-a-metric-catalog-captures-values-and-the-frontend-panel-owns-the-direction-of-good.md),
[ADR-221](../product/architecture/adr-221-then-is-frozen-as-captured-values-and-unrounded-limits-from-its-own-series.md)
and
[ADR-222](../product/architecture/adr-222-report-windows-are-inclusive-instance-days-and-now-length-is-stored-on-the-report.md)

## Why

A coach who changes how a Team or Portfolio works sooner or later has to answer "did it help?", in numbers
management trusts. Lighthouse could show today's flow, and could recompute any past window, but a recomputed "before"
quietly moves: it follows today's state mapping, today's item set and today's settings, and once the window falls
outside the kept history it cannot be rebuilt at all. A "before" that changes after the fact is the opposite of what
the coach needs.

The demand evidence was thin, and that was accepted as a risk. Thrivve Partners had built and used a before/after view
(ValueFlow) in client engagements, and one consultant showed interest after a pitch; Lighthouse ran no interviews of
its own, so the problem, opportunity and solution gates of DISCOVER all failed on threshold. On 2026-10-02 the
maintainer decided to build anyway as a learning investment (D2), with an interview protocol running alongside and the
outcome KPIs below as the way to find out whether the job is real for Lighthouse users.

## What users get

- **A Reports tab on every Team and Portfolio.** It lists the owner's reports by name, Type and creation day. Anyone
  who can read the owner can open them; Team and Portfolio admins create, edit and delete them.
- **A Then & Now report, created in a short wizard** (Type → Then → Now → Metrics & name). Then is a window the coach
  picks by its end date and length (presets 30 / 90 days for a Team, 90 / 180 for a Portfolio, or any number of days,
  at least 14, inside the kept history), so it can end on the day the engagement started. Now is a rolling window of a
  saved length that always ends today.
- **Then is frozen when the report is created.** Its values are computed once and stored; later changes to settings,
  history or the kept-history cutoff never move them.
- **One panel per metric, each reading THEN | NOW | CHANGE**: Cycle Time (95th, 85th, 70th and 50th percentile, with
  the Metrics tab's certainty icons), Throughput (total and per day), WIP and Total Work Item Age (average, median, min
  and max of the window's days). Every line has its own change and % change; better is green, worse red, and the arrow
  and sign always carry the meaning without the colour.
- **A summary header** names both windows, their lengths and how many Work Items closed in each, and sums up per
  metric which way it moved: Improved, Got worse, Mixed or Unchanged.
- **Thin data is shown, not hidden.** Values carry their sample size through the header; a dash appears only where
  nothing finished, never a 0 that stands for "no data".
- **Edit report** changes the name, the Now length and which metrics show; Then can never be edited.
- **The Overview lists each Team's and Portfolio's reports** as chips (two, then "+N more" to the Reports tab), and
  the Deliveries column now follows the same two-then-more rule, with a loading look and a could-not-load look that
  no longer passes for "No Deliveries".
- **Community holds one report per Team or Portfolio for each report Type; Premium is unlimited.** A lapsed licence
  keeps every report readable and deletable and only stops creating.

## What shipped, slice by slice

| Slice | What | Main commits |
|---|---|---|
| 01 — Reports tab, a Team's Then & Now on Cycle Time (#6159) | The `Reports` table on SQLite and Postgres (one owner, cascading with it); the wizard's stepper frame extracted from `CreateWizardShell`; create, list and open on the Team; the Cycle Time panel; ArchUnit rules for the Reports module; `ReportCreated` / `ReportOpened` and the Team Reports tab address in usage data; create timing measured (0.23–0.60 s for a 365-day Then, against a 10 s budget) and a rollback rehearsal on both providers. After the maintainer's live check the panel was reworked to THEN \| NOW \| CHANGE with the 70th and 50th frozen too. | `1a60b93f7` … `9ad425757` |
| 02 — a Then that ended in the past (#6160) | Then by end date and length, the Team presets, the 14-day minimum, no end after today, no start before the kept history; the wizard checks the window before Create against the same bounds as the server; the default name follows Then until edited. | `f7c4e9002` … `7ac0eaa22`, mutation pins to `daeef7bd8` |
| 03 — the summary header (#6161) | Both windows, their lengths and the Work Items closed in each, once at the top of the report; XmR limits and the cycle-time selection extracted with the Metrics tab's charts unchanged. | `dee08dbce` … `01de0cf62` |
| 04 — choose the metrics; Throughput (#6162) | Throughput Then & Now (total and per day); shown metrics chosen and validated; a metric this version cannot show gets a plain note. | `96ca25b5b` … `d514641a9` |
| 05 — WIP and Total Work Item Age (#6163) | Four lines per side (average, median, min, max of the daily values); neutral icons on value lines and a dash mark for no change. | `5e2d97986` … `4752af2b2` |
| 05b — the CHANGE summary and the 95th (#6163) | The header's third segment (Improved / Got worse / Mixed / Unchanged per metric); the 95th percentile on both sides of Cycle Time; per-day Throughput counted over the days actually summed. | `ce07bd111` … `6202f8b82`, mutation `8a1d0ecd4` |
| 06 — reports on Portfolios (#6164) | The Portfolio Reports tab, presets 90 / 180, owner scoping and cascade, the Portfolio tab address in usage data. | `359043724` … `f1ec535b0` |
| 07 — Now length (#6165) | Now's length set at creation with presets or a number of days, refused on the same rules as Then; the Now step names Now's dates and that it slides. | `23e12283e` … `057526839`, `da584be71` |
| 08 — delete and the Community cap (#6166) | Delete from the list and the report page, with "This Then window can no longer be rebuilt." when it applies; one report per owner per Type on Community, refused by the server too, with the licence tooltip on Create Report; the owner's delete dialog names its reports; `ReportDeleted` in usage data. | `0f6777ed0` … `d2172b929` |
| 10 — edit a report (#6168) | Edit report reopens the wizard with Type and Then locked; stale saves and saves over a deleted report say so; Edit beside Delete in the list. | `2631014d2` … `9898b4a37` (mutation pins `12061323e`, `119429dfe`) |
| 12 — reports and Deliveries on the Overview (#6305) | Report chips on both Overview tables, Deliveries capped at two, both cells with loading and could-not-load looks, keyed queries read afresh on each visit. | `b04458a16` … `6089fe795` |

Slice 09 (a notice when settings changed since creation) was dropped, because the maintainer decided settings will
change and that is acceptable (D46). Slice 11 (open the chart behind a verdict) was dropped, because the report has no
verdicts any more (D39).

## Key decisions

- **Then is stored, never recomputed** (D10, D26; ADR-221). Recomputing would move the "before" with every change to
  settings or history, which defeats the purpose, and would stop working once the window leaves the kept history.
  Every applicable metric is frozen, ticked or not, so showing a metric later needs no rebuild. Creation is
  all-or-nothing.
- **No verdicts, one panel per metric** (D39). An early design judged each metric with a rule ("Cycle Time trending
  down = Holds"). The maintainer dropped it: the report shows numbers and how they moved, and people interpret.
  Deciding whether a change is real belongs to the planned Signals work (Epic 5935).
- **The direction of good lives only in each frontend panel** (D39, DD6; ADR-220). The server stores values, not
  judgements, so a new metric or a changed opinion about "good" never needs a migration.
- **No counts beyond Then's process-behaviour limits** (D49). They were designed and then cut after the DISTILL sketch
  review to keep the first version simple; WIP and Work Item Age show average, median, min and max instead (D50, D51).
- **Windows are inclusive calendar days in the instance's time zone; Now's length is stored on the report** (D37, D43;
  ADR-222). Planned scheduled sending needs a defined window, and every reader should see the same report, so viewers
  cannot change Now.
- **Now may overlap Then** (D38). A report frozen today shows no change, and the header's two windows make the
  overlap visible without extra text.
- **Nothing about the owner's settings is stored, and no notice appears when they change** (D46). An accepted risk:
  a panel can show a change caused by a new state mapping rather than by the Team's work. The docs say Now uses
  today's settings while Then stays frozen.
- **The summary counts per metric, not per line** (D53). "Mixed" is honest when the 85th worsens while the 50th
  improves; a majority rule would hide the tail forecasts depend on.
- **Community: one report per owner per report Type** (D57, superseding two earlier versions of the cap). It scales as
  more report Types arrive, and the maintainer judged it fair.
- **Reports are a general home, Then & Now is the first Type** (D36; ADR-219). One table holds every Type with a
  versioned payload, so Signals (5935) and PDF / email export (5882) can build on it. Users read "Type", never
  "Template" (DT2).
- **Reports go with their owner** (D42): deleting a Team or Portfolio deletes its reports, and its delete dialog says
  so.
- **The Overview shows at most two chips per cell, then "+N more"** (D64–D66), and never shows "No Reports" or "No
  Deliveries" while loading or after a failed read (D67).

## Quality gates after DELIVER

- **Mutation testing** (`mutation/results.md`, gate 80 % on both stacks, run on frozen code after each group of
  slices):

  | Run | Backend (Stryker.NET) | Frontend (StrykerJS) |
  |---|---|---|
  | Slices 01–02 | 72.12 % first, **89.09 %** after the kill round | 73.70 % first, **89.06 %** after the kill round |
  | Slices 03–05b | **86.79 %** | **88.21 %** |
  | Slice 06 | **85.71 %** | **81.48 %** |
  | Slices 07, 08, 10 | 87.86 % first, **94.80 %** re-run | 88.21 % first, **92.60 %** re-run |
  | Slice 12 | N/A, no backend change | 77.92 % first, **85.06 %** after the kill round |

  Accepted survivors are mostly styling values jsdom does not lay out and error-message strings; each is listed with
  its reason in `results.md`.
- **E2E**: two walking skeletons in `Lighthouse.EndToEndTests/tests/specs/teams/Reports.spec.ts`, both on Team
  Lightspeed's demo history: a coach creates the default Then & Now report and it opens with the Cycle Time panel;
  and a created report shows beside its Team on the Overview and opens from there. Everything else is pinned by
  backend acceptance suites and component tests.
- **Adversarial review per slice.** Among its catches: a Then or Now reaching back before the first calendar day was
  refused with a code naming the wrong rule, and a Team keeping every finished item answered 500 (`c11ec948d`,
  `33899c521`); per-day Throughput divided by the stored window length rather than the days actually summed
  (`6202f8b82`); and, in slice 12, stale cached lists shown as current (Lessons).

## Lessons learned

- **The shape changed most after people saw it running.** The slice-01 live check reworked the panel (DT9–DT13), the
  slice-02 check added the summary header (DT14), the DISTILL sketch review cut the beyond-limits counts (D49–D52),
  and the slice-04 check added the CHANGE summary and the 95th (D53, D54). Sketches and a live look early each saved
  later rework; the cap was still changed twice in one day (D55, D57), so copy that names a number is best written
  last.
- **A mutation config copied from another feature silently mutated nothing.** `ignorePatterns: ["reports"]` matches
  case-insensitively, so every `Reports/` directory was dropped with only "did not result in any files" as the hint.
  Root-anchored patterns (`/reports`) fix it.
- **A forked background session drove the same checkout in parallel during slice 12.** Two sessions committing into
  one working tree produced a second copy of step 12-01's commit (`17d15a7af`, which never reached `main`; its twin on
  `main` is `a451a9d7e`), and the step's tracking trailer named the ADO Story rather than the feature id, so an empty
  commit (`a5660000f`) had to record it. One session per checkout; a background session gets its own worktree.
- **The app-wide 5-minute `staleTime` made a read moved to TanStack Query show stale data as current.** Before slice
  12 the Deliveries cell read on every mount; once it went through the shared cache, returning to the Overview within
  five minutes showed old Deliveries and old report lists as fresh, and the owner delete dialog trusted a cached
  report count. The adversarial review caught it; both queries now take `staleTime: 0` and show the cached chips dimmed
  while they read again (`515b41505`). Moving a read into the shared cache changes its freshness, so check the app's
  default when doing it.
- **An awaited `invalidateQueries` hangs a test that holds the re-read open.** It only settles once the refetch does,
  and the test deliberately never lets the refetch finish (to see the dimmed look). Fire the invalidation without
  awaiting it (`39b30e026`).

## Known gaps

- **A long Now under-counts silently after the owner's kept-history cutoff is lowered.** Now's length is checked
  against the cutoff only when it is saved. If an admin later shortens how long finished Work Items are kept, a Now
  that now reaches past the oldest kept day counts fewer Work Items, and the report does not say so.
- **The wizard's window check reads years 0–99 as 1900–1999.** `reportWindowCheck.ts` counts days with
  `Date.UTC(year, …)`, which maps those years into the twentieth century, so a Then ending in the year 1–99 is checked
  against the wrong day in the browser. The server is right and still refuses it. Still open on `main`.
- **The Overview reads each owner's reports with one request per row**, as the Deliveries column already did. A
  summary endpoint is the follow-up if the Overview proves slow on large instances.
- **Survivors marked "worth a test later"** in `results.md` (for example the Edit report title, a two-row list with one
  report being read, and a refused read other than 404 during an edit).
- **The KPIs rest on per-browser proxies** (K1, K2, G1), and the cap's pull towards Premium (K4) only has an upper-bound
  proxy, because the maintainer chose no cap-refusal event.

## Phase B — lasting artifacts

Nothing was migrated. The workspace has no `design/architecture-design.md`, `component-boundaries.md`,
`technology-stack.md` or `data-models.md`, no `design/adrs/`, no `distill/walking-skeleton.md` and no
`discuss/journey-*` files. Its lasting outputs were written to their permanent homes during the waves:

- ADR-219 … ADR-222 in `docs/product/architecture/` (status Accepted; not duplicated into `docs/adrs/`);
- the Reports sections of `docs/product/architecture/brief.md`, `c4-diagrams.md` and the root `ARCHITECTURE.md`;
- the journey `docs/product/journeys/epic-5878-baseline.yaml` and the job
  `job-flow-coach-show-whether-flow-changed` in `docs/product/jobs.yaml`;
- the three report events in `docs/settings/usagedata.md`.

Everything else stays in the workspace as delivery history: `feature-delta.md` (decisions D1–D68, DT1–DT14,
DD1–DD39), the wave decisions, the slice briefs, `deliver/roadmap.json`, `deliver/execution-log.json`,
`deliver/slice-01-notes.md` (migration SQL, create timing, rollback rehearsal) and `mutation/` (results and configs).

## Finalize checklist

| Item | Answer |
|---|---|
| RBAC | **New guards, no new permission.** Create, edit and delete need `TeamWrite` / `PortfolioWrite`; list and view need `TeamRead` / `PortfolioRead`; another owner's report answers 404. The UI gates through `useRbac()`; nothing reads `my-summary`. The Overview chips only link to pages that keep their own guards. Check that the docs commits add the Reports row to `docs/settings/rbac.md`; it was missing when this record was written. |
| Lighthouse-Clients (CLI + MCP) | **N/A, because the maintainer kept CLI and MCP out of scope (D15).** Every endpoint is new and additive, and no existing response changed, so no client release is needed. |
| Website | **Owed, not done in this finalize.** DISCUSS planned a features-list entry (Community, Premium = unlimited reports) and a "show what changed since the engagement started" line for the consulting audience. The website repo does not mention Then & Now yet. Confirm the copy with the maintainer before editing. |
| Premium | **The count cap only.** Community holds one report per Team or Portfolio for each report Type, Premium is unlimited (D57); the server refuses too, and the disabled Create Report carries the licence tooltip. A lapsed licence keeps every report readable and deletable. Metrics, windows and editing are never gated. |
| Docs | **Done** in `5c3b2b3e9`: a first-level Reports section (`docs/reports/reports.md`, with `thenandnow.md` as its first type), links from the Team and Portfolio pages and the Overview docs, the two-and-more rule for the Overview columns, a Reports row in `rbac.md`, the report cap in `licensing.md`. |
| Screenshots | **Done** in `296ad2b78`: `features/overview.png` regenerated with report chips and "+1 more"; new `features/reports.png`, `features/report_thenandnow.png` and `features/report_wizard.png`, all run against a fresh backend with demo data and the premium licence. |
| E2E | **Two walking skeletons, green**: create-and-open on Team Lightspeed (slice 01) and open-from-the-Overview (slice 12), in `teams/Reports.spec.ts`. They run in CI through the SQLite and Postgres verify workflows. The rest is pinned below E2E on purpose. |
| Usage data | **Done.** `ReportCreated` (16) and `ReportOpened` (17) carry only the closed enum report Type (`ThenAndNow`); `ReportDeleted` (18) is name-only; the Reports tabs travel as `TeamTabOpened` / `PortfolioTabOpened` addresses. All are listed in `docs/settings/usagedata.md`. No cap-refusal event (maintainer, DEVOPS) and no event for the Overview chips, because `ReportOpened` already counts every opening wherever it starts (D68). |
| Demo data | **Checked, no change.** Team Lightspeed's demo history gives better and worse lines for the screenshots and the E2E; the Portfolios stay thin on purpose. No report is seeded, because a seeded report would freeze on load day. |
| Release note | **Done** in `5c3b2b3e9`: `## Reporting: Then & Now` under `# Lighthouse vNext`, plus a bugfix line for the {Deliveries} column. Website: "Reporting" feature section, comparison rows and a "Next release" card in the website repo (`6ba363a`), pushed after this. |

## Release note (draft)

> **Show what changed since you started.** The new **Reports** tab on every Team and Portfolio lets you freeze a
> **Then & Now** report: pick a window that ended, say, the day the engagement began, and Lighthouse keeps its Cycle
> Time, Throughput, WIP and Work Item Age exactly as they were, beside a rolling Now that always ends today. Each
> metric shows Then, Now and the change, and the summary tells you at a glance what improved, what got worse and
> what moved both ways. Community includes one report per Team or Portfolio; Premium has no limit.
