# Wave Decisions — DESIGN — epic-5878-baseline

**Agent**: Morgan (`nw-solution-architect`) · **Date**: 2026-10-03 · **Interaction mode**: PROPOSE (autonomous
subagent; the maintainer was not available mid-run, so genuine choices carry an adopted recommendation, listed under
"Decisions for the maintainer to confirm").
**Predecessor**: DISCUSS (Luna; D16–D44 and Q8–Q10 settled, not re-opened except where code contradicts them — see
Upstream Changes). **Successor**: DEVOPS (`nw-platform-architect`), then DISTILL (`nw-acceptance-designer`).

**Revision 2026-10-03.** The maintainer answered the eight decisions to confirm: four accepted, four changed as
D45–D48 (recorded in DISCUSS and applied here, in the ADRs and in the SSOT).

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
3. Then is frozen at creation as captured values and unrounded XmR limits from its own series; Now is captured by the
   same code on every read over a server-computed window whose length is stored on the report (ADR-221, ADR-222).

## Decisions taken in this wave

- **DD1** New module `Reports`; only `API` depends on it.
- **DD2** One `Reports` table; `TemplatePayloadJson` + `PayloadSchemaVersion`; additive payload; absent = not captured.
- **DD3** Owner = two nullable cascading FKs, exactly one set.
- **DD4** Templates are strategies; v1 = `then-and-now`.
- **DD5** Catalog = one `IReportMetric` per metric over the `IReportMetricSeries` port (Team / Portfolio adapters).
- **DD6** No direction, verdict or colour on the server; change and % in the panel from displayed values.
- **DD7** Exhaustive frontend panel registry; panels pure; shared frame with an author-note slot.
- **DD8** Then read as raw series, never through the PBC builders.
- **DD9** `XmRCalculator.Limits` extracted, unrounded; `Calculate` byte-identical.
- **DD10** Beyond limits: > upper; < lower only when lower > 0, else "—".
- **DD11** Freeze values (+ sample size, absent reason) and unrounded limits for every applicable metric; no series.
- **DD12** Every value carries its sample size; "—" only when truly empty, as closed codes; no threshold (D45).
- **DD13** Inclusive instance-zone day windows on the server; `ReportWindowPolicy`, not `BaselineValidationService`.
- **DD14** Now length in the payload; edit only, under Write.
- **DD15** Value definitions: M1 percentiles + per-item limits; M2 total and per-day average over the whole window,
  change on per-day, daily limits (D47); M3 average/min/max; M4 total and average, each as window average and last
  day, limits on daily totals (Q10, D48).
- **DD16** *Withdrawn by D46* — no settings record, no notice.
- **DD17** Cap policy in the create command; list envelope says `creationBlockedByCap`; race accepted.
- **DD18** Two controllers (Team / Portfolio); Read on class, Write on actions; cross-owner 404; `useRbac` gating.
- **DD19** `IReportQueries` (write-free) vs `IReportCommands`.
- **DD20** In-request, all-or-nothing creation; never the update queue.
- **DD21** Concurrency token on `Report`; 409 on stale edit.
- **DD22** Shared tab; linkable report route; no client-side window maths.
- **DD23** No domain event / queue / SignalR; owner-delete count from the list endpoint.
- **DD24** `thenRebuildable` on the read drives the delete warning.
- **DD25** Edit request carries no Then field; uncaptured keys refused.

## Storage

One expand-only migration in slice 01 (`CreateMigration`, SQLite + PostgreSQL): table `Reports` (`Id`, `TeamId?`,
`PortfolioId?`, `TemplateKey`, `Name`, `CreatedAt`, `ShownMetricKeysJson`, `TemplatePayloadJson`,
`PayloadSchemaVersion`, `ConcurrencyToken`); FKs cascade; exactly-one-owner check. No further migration in slices
02–10: they add payload members.

## Reuse summary

23 overlaps examined: 12 reused (as code or pattern), 5 extended (`XmRCalculator`, the cycle-time selection in
`BaseMetricsService`, the two metrics interfaces' WIA series, owner tabs and delete dialogs, usage-data enums), 6
rejected with evidence (PBC builders, `BaselineValidationService`, the day-keyed PBC/percentile tables, info-widget
comparison, `CycleTimePercentiles` widget, `TeamDeleted` cleanup). CREATE NEW: the report entity / table / repository,
templates, catalog, window policy, beyond-limits policy, panels and tab. (`FetchFingerprint` was an extension only for
the settings notice and left the list with D46.)

## Decisions for the maintainer to confirm — resolved 2026-10-03

1. Payload as JSON text in one table — **accepted**.
2. Owner as two cascading FKs — **accepted**.
3. The full Then series is not stored — **accepted**.
4. Thin data — **changed (D45)**: every value with its sample size; "—" only when truly empty; no threshold.
5. What the settings record covers — **moot (D46)**: no settings record and no notice at all; slice 09 dropped.
6. {Throughput} statistic — **changed (D47)**: total and per-day average over the whole window; change on per-day.
7. Average {Work Item Age} — **changed (D48)**: window average and last day, both per side.
8. Change and % change computed in the panel from displayed values — **accepted**.

No open decision remains for the maintainer from DESIGN.

## Upstream Changes

See `upstream-changes.md`: the missing percentile guard (resolved by D45), `BaselineValidationService` not reusable
(S6, handled in DESIGN), the settings notice (dropped by D46), the {Throughput} statistic (D47), average {Work Item
Age} (D48). DISCUSS was updated in place for D45–D48. ADR-209 gains a status note (superseded deferral).

SSOT updated: `brief.md` (`## Application Architecture — epic-5878-baseline`), `c4-diagrams.md` (L1–L3),
`ARCHITECTURE.md` (ADR index row 219–222 and the 209 row; §4/§6 concepts follow the Refinement precedent and land when slice 01 ships), ADR-219..222, ADR-209 status note.

## Handoff

DEVOPS: usage-data route keys and events (D33, K1/K4/G1; K3 dropped in DEVOPS), one migration on both providers, `Program.cs`
registration triggers the full Integration suite, no new job or configuration. Outcome collision check **not run**
(no shell); the coordinator runs `nwave-ai outcomes check-delta docs/feature/epic-5878-baseline/feature-delta.md`.
