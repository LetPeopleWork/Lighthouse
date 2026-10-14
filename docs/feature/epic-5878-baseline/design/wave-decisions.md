# Wave Decisions — DESIGN — epic-5878-baseline

**Agent**: Morgan (`nw-solution-architect`) · **Date**: 2026-10-03 · **Interaction mode**: PROPOSE (autonomous
subagent; the maintainer was not available mid-run, so genuine choices carry an adopted recommendation, listed under
"Decisions for the maintainer to confirm").
**Predecessor**: DISCUSS (Luna; D16–D44 and Q8–Q10 settled, not re-opened except where code contradicts them — see
Upstream Changes). **Successor**: DEVOPS (`nw-platform-architect`), then DISTILL (`nw-acceptance-designer`).

**Revision 2026-10-03.** The maintainer answered the eight decisions to confirm: four accepted, four changed as
D45–D48 (recorded in DISCUSS and applied here, in the ADRs and in the SSOT).

**Revision 2026-10-13 (Final Wave Review: needs_revision).** Addressed B1–B3, H1–H6, M1–M9, L1–L2 against DISTILL's
final UI decisions DT1–DT7 and `distill/upstream-issues.md`. DD19, DD24, DD25 revised; DD26–DD34 added; a
"Loading and failure looks" section and an Observability quality attribute added; ADR-219..222 **Accepted**.

**Revision 2026-10-13, round 2 (second-pass review: needs_revision).** NB1 contract-shape rows added for every
REUSE / EXTEND on a report path; NB2 the policies take `today`, cutoff, licence result and count as arguments (new
read-only `IReportOwnerLookup` so queries can read the cutoff without `IRepository<>`), template create reclassified
as read-only I/O, C4 L3 arrows moved to the query / command; NH1 `WidgetShell`'s exported body look gains optional
`failureMessage` / `onRetry`; NH2 / NM1 / NM2 DD26 restated (copy interpolates server values, presets are UI
shortcuts, `earliestStart` null = no lower bound, values from one source and comparisons in two implementations);
NL1 templates query refetches on wizard open; NL2 E14; NL3 list failure copy is the working copy; NL4 collision check
recorded. **No wire-contract change.**

**Revision 2026-10-14 (maintainer decisions D49–D52, after the DISTILL sketch review).** No limits are frozen and
nothing is counted beyond them (D49): DD9, DD10 withdrawn, DD11 values only, `BeyondLimits` not built, E5 withdrawn,
E15 repointed, E2 extended to `XmRCalculator`; one absence code left (`no-finished-items`); {WIP} and Total {Work
Item Age} as Average, Median, Min, Max of daily values (D50, D51); DD35–DD37 added; ADR-221 amended. **Wire change**:
`beyondThenLimits` dropped (no release carried it), `wip` / `work-item-age` sides become `{ average, median, min, max }`.
Superseded text below is kept and marked.

**Revision 2026-10-14, second pass (peer review of D49–D52: needs_revision 1B/2H/8M/7L).** DD33 struck for DT9 (Then
freezes the 85th, 70th and 50th; a pre-DT9 report reads Then's 70th / 50th "Not captured for this report"); DD12 states
the header's closed count and that the sample size shows in the header, not per line; "days since Then ended" waits
for slice 07's sketch; DD35's median flagged to the maintainer; DD36 accepts binary-double rounding; reuse count
restated. No wire change.

**Revision 2026-10-14, third pass (maintainer decisions D53, D54, after the slice-04 live check; slice 05b).** DD38:
the {Cycle Time} 95th joins both sides as an additive payload member `p95` (no migration; a pre-D54 report reads
Then's 95th "Not captured for this report" through `WindowedReportMetric`'s read-back, as DT9's 70th / 50th); DD39:
the header's CHANGE segment is derived in the frontend only from the panels' own per-line verdicts
(`lineComparisonsOf` → `summariseChange` in `panels/changeSummary.ts`), never stored or sent; E16 parity test;
ADR-220 and ADR-221 amended; slice 05b's steps and test list added. **Wire change**: `cycle-time` sides gain `p95`.

Full text of everything below: `../feature-delta.md` → "Wave: DESIGN / …" sections. ADRs: ADR-219..ADR-222.

## Config

Paradigm OOP (unchanged, per CLAUDE.md) · style modular monolith + ports-and-adapters (ADR-027, unchanged) · C4 L1 +
L2 in the feature delta, L3 in `c4-diagrams.md` (the Reports module has more than five components) · no new
technology · no external integration (contract testing N/A).

## Architecture in three lines

1. A Report is an owner-scoped record: one table, common columns + a template-specific typed payload as JSON text;
   delivery and schedule will live outside it (ADR-219, superseding ADR-209's deferral).
2. A metric catalog captures each metric for a window on the server through one owner-agnostic series port; one
   frontend panel per metric owns the direction of good, the change and the colour (ADR-220).
3. Then is frozen at creation as captured values ~~and unrounded XmR limits from its own series~~; Now is captured by
   the same code on every read over a server-computed window whose length is stored on the report (ADR-221, ADR-222).
   *(superseded by D49 (2026-10-14): no XmR average or limits are frozen; Then is its captured values only.)*

## Decisions taken in this wave

- **DD1** New module `Reports`; only `API` depends on it.
- **DD2** One `Reports` table; `TemplatePayloadJson` + `PayloadSchemaVersion`; additive payload; absent = not captured.
- **DD3** Owner = two nullable cascading FKs, exactly one set.
- **DD4** Templates are strategies; v1 = `then-and-now`.
- **DD5** Catalog = one `IReportMetric` per metric over the `IReportMetricSeries` port (Team / Portfolio adapters).
- **DD6** No direction, verdict or colour on the server; change and % in the panel from displayed values.
- **DD7** Exhaustive frontend panel registry; panels pure; shared frame with an author-note slot.
- **DD8** Then read as raw series, never through the PBC builders.
- ~~**DD9** `XmRCalculator.Limits` extracted, unrounded; `Calculate` byte-identical.~~ *(superseded by D49
  (2026-10-14): nothing extracted; c206d65bd reverted in 058b4ba48.)*
- ~~**DD10** Beyond limits: > upper; < lower only when lower > 0, else "—".~~ *(superseded by D49 (2026-10-14).)*
- **DD11** Freeze values (+ sample size, absent reason) ~~and unrounded limits~~ for every applicable metric; no series.
  *(limits superseded by D49 (2026-10-14).)*
- **DD12** Every value carries its sample size; "—" only when truly empty, as closed codes; no threshold (D45).
  *Revised 2026-10-14*: one code left on the wire, `no-finished-items` ({Cycle Time}); {Throughput}, {WIP} and Total
  {Work Item Age} are never absent (a 0 is real); the sample size shows once, in the summary header (D52) — each
  side's length in days and "N {Work Items} closed" — not per line, superseding D45's "visible on every value" for
  the daily-series and percentile lines. Closed count (frontend only, DST-18): from slice 04 the side's {Throughput}
  `total` when captured, else its {Cycle Time} sample size (closed with a cycle time > 0); no count line when neither
  was captured; "0 … closed" when the source is 0 or `no-finished-items`. "Days since Then ended" (D35) is not in the
  header; it returns only after the maintainer approves a sketch at slice 07, and `daysSinceThenEnded` stays on the
  wire meanwhile.
- **DD13** Inclusive instance-zone day windows on the server; `ReportWindowPolicy`, not `BaselineValidationService`;
  `today` and the cutoff are arguments, read by the query / command (rev. 2026-10-13 round 2).
- **DD14** Now length in the payload; edit only, under Write.
- **DD15** Value definitions: M1 percentiles ~~+ per-item limits~~; M2 total and per-day average over the whole window,
  change on per-day~~, daily limits~~ (D47); ~~M3 average/min/max; M4 total and average, each as window average and last
  day, limits on daily totals (Q10, D48)~~. *Revised 2026-10-14 (D49–D51)*: M3 = Average, Median, Min, Max of the
  daily {WIP}; M4 = Average, Median, Min, Max of the daily **total** {Work Item Age}; each with sample size L days,
  never absent; no limits anywhere.
- **DD16** *Withdrawn by D46* — no settings record, no notice.
- **DD17** Cap policy in the create command, a pure function of (licence result, count for owner) which the command
  and list query read and pass in; list envelope says `creationBlockedByCap`; race accepted.
- **DD18** Two controllers (Team / Portfolio); Read on class, Write on actions; cross-owner 404; `useRbac` gating.
- **DD19** `IReportQueries` (write-free) vs `IReportCommands`; persistence split into `IReportReader` /
  `IReportWriter`, both on `ReportRepository`; queries reach the reader only (rev. 2026-10-13).
- **DD20** In-request, all-or-nothing creation; never the update queue.
- **DD21** Concurrency token on `Report`; 409 on stale edit.
- **DD22** Shared tab; linkable report route; never the viewer's clock — client day arithmetic only on server days
  (rev. 2026-10-13, DD26).
- **DD23** No domain event / queue / SignalR; owner-delete count from the list endpoint.
- **DD24** `thenRebuildable` on the read **and every list row**, at `thenAndNow.thenRebuildable` in both; drives the
  delete warning (rev. 2026-10-13).
- **DD25** Edit request carries no Then field; uncaptured keys refused. Universe declared: only name, shown keys, Now
  length and token change; owner, type, created, schema version, Then window, captures and unknown payload members
  stay byte-identical; no other table. Then members in a body are ignored (200) (rev. 2026-10-13).
- **DD26** Wizard pre-check from server bounds (`today`, `earliestStart`, `minimumLengthDays` on the templates
  read); the server re-validates and stays the authority; one boundary table on both stacks (with a cutoff-0 row and
  a copy-interpolation check). Copy interpolates the server's values; presets are UI shortcuts, not rule values;
  `earliestStart` null = no lower bound; values from one source, comparisons in two implementations; templates query
  refetches on wizard open (rev. round 2).
- **DD27** Wire names `atThen` / `atNow`.
- **DD28** Closed refusal codes (DISTILL's eleven) in ProblemDetails; 400 validation, 403 cap, 404, 409, 422 for a
  stored report of an unknown type.
- **DD29** Ports take a `ReportOwner`, never a user; controllers authorise. 5882's sender is the first user-less caller.
- **DD30** One `ReportWizard` (create | edit) on a `WizardStepperFrame` extracted from `CreateWizardShell`;
  `DataGridBase` list; `DeleteConfirmationDialog` + optional warning.
- **DD31** Loading / re-read / failure / not-found looks on `WidgetShell`'s body look (exported, with optional
  `failureMessage` / `onRetry` for the report's own sentence and Retry; dashboards unchanged) and keyed queries.
- **DD32** Unknown type: listed, opens to a not-found-family look; unknown metric: neutral frame.
- ~~**DD33** Then freezes the 85th only; no "Not captured" for Then's 70th / 50th.~~ *(Superseded by DT9, 2026-10-13:
  Then freezes the 85th, 70th and 50th; a report stored before DT9 holds the 85th only and reads Then's 70th and 50th
  "Not captured for this report" (D27), pinned by
  `A_report_that_froze_only_the_85th_percentile_says_nothing_of_Thens_70th_and_50th`.)*
- **DD34** Create ≤ 10 s is a hypothesis; slice 01 measures; fallback cheaper captures; reopen DESIGN if it misses.
- **DD35** *(2026-10-14)* One pure daily-series summary (Average, Median, Min, Max) shared by {WIP} and Total {Work
  Item Age}. Median = `PercentileCalculator.CalculatePercentile(values, 50)`, the Metrics tab's 50th: position
  max(1, ⌊n ÷ 2⌋) of the sorted values — the lower middle for an even count, just below the middle for an odd one;
  called on a copy (it sorts in place). Sample size of a daily-series value = L days. The median convention is the
  architect's call, flagged to the maintainer on 2026-10-14; no objection yet, so it stands.
- **DD36** *(2026-10-14)* Display rounding through `roundForDisplay` / `compareThenNow`: {WIP} and Total {Work Item
  Age} 0 decimals (as {Cycle Time}); {Throughput} total 0, per day 2; change and % from displayed values (DD6).
  Binary doubles accepted: `roundForDisplay(1.005, 2)` is 1, not 1.01 — pinned by the row `[1.005, 2, 1]` in
  `panels/thenNowComparison.test.ts` (DST-20).
- **DD37** *(2026-10-14)* `beyondThenLimits`, the old `work-item-age` keys and four absence codes removed from the
  frontend model in slice 04 (no release carried them); `median` added to `ReportMetricSide`.
- **DD38** *(2026-10-14, D54)* The {Cycle Time} 95th: `cycle-time` sides are `{ p95, p85, p70, p50 }`; the
  `CycleTimePercentiles` record gains `P95`, filled from the same Metrics-tab percentile read; same sample size and
  `no-finished-items` rule; additive payload member, schema version 1, no migration. A report frozen before D54 has
  no Then `p95` (`WindowedReportMetric` reads back only Then members that Now produces and the capture holds) and
  reads it "Not captured for this report" — DT9's path. The 95th row sits above the 85th with
  `new ForecastLevel(95)` = Certain, the Metrics tab's own mapping (`CycleTimePercentiles.tsx:57-58`).
- **DD39** *(2026-10-14, D53)* The header's CHANGE segment is frontend-only, never stored or sent. Registry entries
  list their change-marked lines (`changeLines`: key + decimals); `lineComparisonsOf(metric, panel)` in
  `panels/thenNowComparison.ts` gives one `{ key, change | null }` per line (null = a dash or not captured on either
  side) and feeds both the panel's change column and `summariseChange(shown)` in `panels/changeSummary.ts`, which
  returns the groups Improved, Got worse, Mixed, Unchanged in that order, empty ones dropped, keys in catalog order,
  `[]` (rendered "—") when nothing is comparable. Names are the panel titles; colour from `verdictColours`; loading
  and failure follow the report body's `WidgetBody`.

## Storage

One expand-only migration in slice 01 (`CreateMigration`, SQLite + PostgreSQL): table `Reports` (`Id`, `TeamId?`,
`PortfolioId?`, `TemplateKey`, `Name`, `CreatedAt`, `ShownMetricKeysJson`, `TemplatePayloadJson`,
`PayloadSchemaVersion`, `ConcurrencyToken`); FKs cascade; exactly-one-owner check. No further migration in slices
02–10: they add payload members.

## Reuse summary

~~23 overlaps examined: 12 reused (as code or pattern), 5 extended (`XmRCalculator`, the cycle-time selection in
`BaseMetricsService`, the two metrics interfaces' WIA series, owner tabs and delete dialogs, usage-data enums), 6
rejected with evidence~~ *(count restated 2026-10-14 below)*. Rejected with evidence: PBC builders, `BaselineValidationService`, the day-keyed PBC/percentile tables, info-widget
comparison, `CycleTimePercentiles` widget, `TeamDeleted` cleanup. Added 2026-10-13: `CreateWizardShell` (extend by
extracting its stepper frame), `DataGridBase` (reuse), `DeleteConfirmationDialog` (extend: optional warning),
`WidgetShell` body look (extend: export), keyed TanStack queries (reuse), `LoadingAnimation` (rejected for the view). CREATE NEW: the report entity / table / repository,
templates, catalog, window policy, ~~beyond-limits policy~~ *(not built, D49)*, panels and tab. Added 2026-10-14:
`PercentileCalculator` reused for the median; DD35's daily-series summary created (nothing gives the four statistics
together). (`FetchFingerprint` was an extension only for
the settings notice and left the list with D46.)

**Count as of 2026-10-14: 30 overlaps examined — 15 reused (12 + `DataGridBase`, keyed TanStack queries,
`PercentileCalculator`), 6 extended (the two metrics interfaces' daily total-age read, owner tabs and delete dialogs,
usage-data enums, `CreateWizardShell`, `DeleteConfirmationDialog`, `WidgetShell`), 7 rejected (the six above +
`LoadingAnimation`), and 2 examined but no longer touched since D49 (`XmRCalculator`, the cycle-time selection in
`BaseMetricsService`).**

## Decisions for the maintainer to confirm — resolved 2026-10-03

1. Payload as JSON text in one table — **accepted**.
2. Owner as two cascading FKs — **accepted**.
3. The full Then series is not stored — **accepted**.
4. Thin data — **changed (D45)**: every value with its sample size; "—" only when truly empty; no threshold.
5. What the settings record covers — **moot (D46)**: no settings record and no notice at all; slice 09 dropped.
6. {Throughput} statistic — **changed (D47)**: total and per-day average over the whole window; change on per-day.
7. Average {Work Item Age} — **changed (D48)**: window average and last day, both per side. *(Superseded by D51,
   2026-10-14: no per-item average age; the panel is "Total {Work Item Age}", Average, Median, Min, Max of the daily
   totals.)*
8. Change and % change computed in the panel from displayed values — **accepted**.

No open decision remains for the maintainer from DESIGN.

## Upstream Changes

See `upstream-changes.md`: the missing percentile guard (resolved by D45), `BaselineValidationService` not reusable
(S6, handled in DESIGN), the settings notice (dropped by D46), the {Throughput} statistic (D47), average {Work Item
Age} (D48, *superseded by D51*). DISCUSS was updated in place for D45–D48. ADR-209 gains a status note (superseded
deferral). *2026-10-14*: D49–D52 (no limits or counts; {WIP} and total age as four lines; the summary header) were
recorded in DISCUSS by the maintainer's decision and applied here and in ADR-221 — see `upstream-changes.md` §8.
D53 and D54 (the CHANGE segment, the {Cycle Time} 95th) likewise, applied as DD38, DD39 and E16 and in ADR-220 and
ADR-221 — see §9.

SSOT updated: `brief.md` (`## Application Architecture — epic-5878-baseline`), `c4-diagrams.md` (L1–L3),
`ARCHITECTURE.md` (ADR index row 219–222 and the 209 row; §4/§6 concepts follow the Refinement precedent and land when slice 01 ships), ADR-219..222, ADR-209 status note.

## Handoff

DEVOPS: usage-data route keys and events (D33, K1/K4/G1; K3 dropped in DEVOPS), one migration on both providers, `Program.cs`
registration triggers the full Integration suite, no new job or configuration. Outcome collision check run by the
coordinator on 2026-10-13: 6 outcomes checked, 0 collisions found (warnings only: `OUT-5878-*` referenced but not yet
in the registry).
