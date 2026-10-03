# ADR-220: A metric catalog captures values on the server; one frontend panel per metric owns its direction of good

- **Status**: Proposed (DESIGN, 2026-10-03)
- **Date**: 2026-10-03
- **Feature**: epic-5878-baseline (slices 01, 03, 04, 05, 06)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

The report shows one panel per metric (D39): v1 catalog M1 {Cycle Time}, M2 {Throughput}, M3 {WIP}, M4 {Work Item
Age}, for Team and Portfolio. More metrics come later; Signals (5935) evaluates rules over the same catalog on live
data; 5882 renders the same panels server-side (Spike 6052: only the real frontend components are acceptable, and
components that derive data in `useEffect` render empty under SSR). The maintainer fixed that the direction of good
is a property of each metric **known only to its frontend panel** — never stored, never computed in the backend.

Existing parts: `ITeamMetricsService` / `IPortfolioMetricsService` expose the four series per owner;
`PercentileFamilies` already lists "which reader serves which family" per owner kind as one list; the Metrics page's
info widgets compute a previous-period comparison in the backend with neutral arrows and no notion of good.

## Decision

1. **Backend: one `IReportMetric` class per metric** (stable string key: `cycle-time`, `throughput`, `wip`,
   `work-item-age`), registered in DI and listed by a `ReportMetricCatalog`. Each declares which owner kinds it applies
   to and offers one operation: **capture this metric for a window** — the values its panel shows, the sample sizes,
   and (for the beyond-limits count) the series it needs. The same capture runs for Then (at creation) and Now (on
   read), so the two sides cannot be computed differently.
2. **Metrics are owner-agnostic.** They read through one driven port, `IReportMetricSeries`, with a Team adapter and
   a Portfolio adapter over the existing metrics services. Owner-kind differences live in the two adapters, the way
   `PercentileFamilies` keeps them in one place today.
3. **No direction, verdict or colour on the server.** The wire carries values, sample sizes, limits and counts. Change
   and % change are plain arithmetic done **in the panel**, from the values as displayed, so Then + change = Now on
   screen (precedent: ADR-211, a grade is read in the client from facts the server sends).
4. **Frontend: a panel registry** — `Record<ReportMetricKey, PanelDefinition>` checked with `satisfies`, so a key
   without a panel does not compile. A `PanelDefinition` holds the panel component, its Terminology title token and
   its **direction of good**, the one place that fact exists per metric. Every panel renders inside one shared frame
   (title, Then column, accent bar, Now column, and an empty footer slot reserved for the later author note).
5. **Panels are pure functions of their props**: no fetching, no state derived in effects, no size measured from the
   DOM. The report view fetches once and hands each panel its slice. This is the seam 5882's server-side rendering
   needs, and it costs nothing now.
6. **Metric keys are strings on the wire**, not enum ordinals (enums serialise as strings out and integers in on this
   API, which has bitten numeric TypeScript mirrors before).

## Alternatives considered

- **Direction of good on the backend metric definition** (and colour classification on the server). Simpler for a
  CLI or an email built without the frontend, but it puts a presentation fact in two places the moment the frontend
  also needs it, and the maintainer ruled it out. **Rejected.**
- **One generic panel driven by a metadata schema** (labels, value list, direction as data). Saves four components
  but makes the metadata the real panel language; per-metric layout differences (two values per side for {Work Item
  Age}, a range for {WIP}, a week count for {Throughput}) become schema features. **Rejected.**
- **Reuse the Metrics page info widgets** (`InfoWidgetComparisonDto`, `WidgetShell` trend). Their comparison is
  previous-period, formatted as strings on the server, with neutral up/down arrows — none of it frozen, none of it
  knows good from bad. Reused: the icon set and the theme's success/error colours only. **Rejected as a base.**
- **Metrics call `ITeamMetricsService` / `IPortfolioMetricsService` directly** with an owner-kind switch in each.
  Four metrics × two owners of branching, and no single seam to fake in unit tests. **Rejected.**

## Consequences

- **Positive**: adding a metric = one backend class + one panel + one registry line; reports created earlier show
  "Not captured for this report" because their payload has no capture for the new key (D27).
- **Positive**: 5935 can evaluate rules over the same captures; 5882 can render the same panels.
- **Negative**: a non-frontend consumer (a future CLI) cannot colour a change without re-declaring direction; that
  is the maintainer's chosen trade, recorded here.
- **Negative**: two catalogs to keep in step (backend keys, frontend registry). A key the frontend does not know
  renders a neutral "not supported in this version" panel rather than crashing; an E2E-free unit test pins that the
  frontend key union equals the backend catalog's keys through the templates endpoint fixture.

## Enforcement

- TypeScript: `reportPanelRegistry satisfies Record<ReportMetricKey, PanelDefinition>`; exhaustive maps, no
  `default:` branches.
- NUnit: the report read DTO has no member about direction or colour (shape test).
- Vitest: every panel renders identically when its props are identical and no network call is made (purity).
- ArchUnitNET: `IReportMetric` implementations depend on `IReportMetricSeries`, never on `ITeamMetricsService` or
  `IPortfolioMetricsService` directly.

Cross-refs: [ADR-211](./adr-211-a-reality-check-grade-is-read-in-the-client-from-facts-the-server-already-sends.md),
[ADR-208](./adr-208-a-past-day-is-computed-by-the-recorders-own-code.md) (one family list per scope), ADR-219,
ADR-221.
