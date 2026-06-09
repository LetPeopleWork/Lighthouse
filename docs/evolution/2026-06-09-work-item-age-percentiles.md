# Evolution: work-item-age-percentiles

- **Date finalized**: 2026-06-09
- **ADO**: Story #5257 ("Work Item Age Percentiles"). Non-premium, brownfield.
- **Status**: Delivered on `main` (DISCUSS → DESIGN → DISTILL → DELIVER all done; CI green; both Sonar gates `new_violations = 0`; mutation ≥ 80% on the feature surface — BE 83.3% adjusted, FE 95.9% adjusted). Adversarial review APPROVED, 0 blockers; 4-reviewer DISTILL gate passed.
- **Workspace (history)**: `docs/feature/work-item-age-percentiles/`
- **Builds on**: the Cycle Time Percentiles card + the Work Item Aging chart it extends, and the sibling `aging-pace-percentiles` (per-state bands, orthogonal).

## What shipped

Lighthouse now summarises the **current in-progress population's own ages** as percentiles —
a snapshot of live WIP, distinct from the existing cycle-time percentiles (which describe how
long *finished* work took). Two surfaces, Team + Portfolio:

1. A **"Work Item Age Percentiles" overview card** showing the 50/70/85/95 of the current
   in-progress items' `WorkItemAge` (descending rows, `ForecastLevel` colouring, graceful empty
   state when WIP is empty). A flow coach reads "85% of my WIP is younger than X days" at a glance
   instead of eyeballing every dot.
2. A **Cycle Time ↔ Work Item Age toggle** on the Work Item Aging chart (a segmented
   `ToggleButtonGroup`) that *swaps* the horizontal reference lines between the two server-fetched
   percentile sets — mutually exclusive, CT default, pure client-side source swap (no per-flip
   network call). The coach contrasts live WIP spread against historical completion times on one
   chart.

The percentiles are computed **server-side** (D8, user-confirmed) on two new read endpoints
(`GET …/metrics/workItemAgePercentiles`, Team + Portfolio), each returning a flat
`IEnumerable<PercentileValue>` — the same shape as `cycleTimePercentiles`, no new DTO. Each service
method composes existing primitives: the in-progress selection (`GetWipSnapshotForTeam` /
`GetInProgressFeaturesForPortfolio`) → each item's `WorkItemAge` (`> 0` filter) →
`BuildPercentiles` / `PercentileCalculator`. **Snapshot semantics**: `startDate`/`endDate` exist for
signature parity + the 400-guard + cache key, but only `endDate` selects the population and the cache
key (`WorkItemAgePercentiles_{endDate}`); `startDate` never filters (an integration test pins this).

Six steps across three slices (Team summary → chart toggle → Portfolio parity), every step CI-green:
01-01 Team endpoint (`23f4e50f`) · 01-02 Team card (`5d8d4450`) · 02-01 chart selector
(`0add4fb5`) · 02-02 live `@screenshot` (`ee511d50`) · 03-01 Portfolio endpoint (`50cef21c`) ·
03-02 Portfolio card+selector (`f466ab08`).

## Key decisions

- **ADR-065** — WIA percentiles computed **server-side** on a new read endpoint per scope, reusing
  the in-progress selection + `BuildPercentiles` / `PercentileCalculator` + `PercentileValue`. The
  user **overrode the prior pass's client-side recommendation** (*"do as little production work in
  the frontend"*), restoring percentile-computation uniformity with the existing CT/age-in-state
  endpoints. Cost: a new endpoint ⇒ version-gated client wrappers.
- **ADR-066** — the aging chart swaps the line *source*, not the line *renderer*: one derived
  `activePercentiles` feeds the single existing `ChartsReferenceLine` block. Mutual exclusivity is
  structural (exactly one line set on the canvas), `useChartVisibility` stays on its single-`percentiles`
  contract.

## User-review pivots (DELIVER)

Two corrections landed after the implementation steps, both from reviewing the rendered chart:

- **Segmented-selector affordance.** The CT↔WIA control became a FeatureSize-style segmented
  `ToggleButtonGroup` (the AC-neutral "switch vs chip-pair" question DESIGN deferred to DELIVER,
  decided live against the chart per the run-Playwright-before-commit discipline).
- **Axis stabilisation + overlapping-line dedup** (`23f5eccc`). Swapping the reference-line source
  let the chart's x-axis re-anchor to the WIA set; the fix **anchors the axis to cycle time** so the
  plot doesn't jump on toggle, and **de-dups overlapping percentile-line values** so two coincident
  percentiles don't double-draw. This is a delta *beyond* the original ADR-066 (which only specified
  the source swap), driven by what the live render exposed.

## Cross-cutting

- **RBAC** — N/A (no new authorization). Non-premium (D3); the new endpoints ride the existing
  class-level `[RbacGuard(TeamRead/PortfolioRead)]`. No `useRbac()` change, no `ILicenseService` on
  the read path.
- **Lighthouse-Clients (CLI + MCP)** — **AFFECTED (version-gated).** The new endpoint × 2 scopes means
  the clients add a `getWorkItemAgePercentiles` wrapper that pre-checks the server version (an old
  server 404s opaquely) and fails with a clear "upgrade Lighthouse" error, pinned strictly newer than
  the last released version in `FEATURE_REQUIRES_SERVER_NEWER_THAN` (dev/unparseable versions never
  blocked). This **reversed** the prior pass's "unaffected" conclusion when the user moved compute to
  the backend. Committed in the **separate clients repo, awaiting release** — a **release-gate** (Forge
  HIGH): do not mark the feature released until the clients wrapper is confirmed merged.
- **Website** — marketing N/A (enhances an existing free metric surface). Docs NOT N/A:
  `docs/metrics/widgets.md` gained the card + selector prose with live screenshots.

## Durable lessons

- **`useChartVisibility` already de-dupes the CT lines — WIA needed parity.** The cycle-time
  reference lines already collapsed coincident percentile values; the first WIA pass didn't, so two
  overlapping WIA percentiles double-drew. The dedup had to be brought to parity on the WIA source
  (part of `23f5eccc`). When swapping a data *source* into an existing renderer, audit the renderer's
  existing per-source normalisation and reproduce it for the new source.
- **Swapping a reference-line source re-anchors the chart axis.** Feeding a second percentile array
  into the same axis let the x-axis range jump on toggle; anchoring the axis explicitly to cycle time
  keeps the plot stable across flips. A pure "swap the array" change is not visually pure — verify the
  axis on the live render.
- **Sonar S7735 fires on `!== false`, not just on `!`-negated booleans.** The reference-line render
  guard `visiblePercentiles[p.percentile] === false ? null : …` tripped S7735 (negated/awkward
  ternary) even though it reads naturally; flipping the ternary to the positive arm cleared it
  (`bac5552d`, now in `docs/ci-learnings.md`). A clean local build did not surface it — it is a CI-only
  Sonar gate, the usual trap.
- **Whole-file Stryker on giant shared service files is misleading.** The WIA logic lives inside
  `TeamMetricsService` / `PortfolioMetricsService` next to throughput/WIP/forecast; the whole-file
  `mutate` glob reports a meaningless aggregate (18.76% BE). Measure the new line spans (83.3% adjusted),
  justify the equivalents (debug logging, the unreachable `>= 0` age boundary since in-progress
  `WorkItemAge ≥ 1` by construction, the constant-cache-key marginal survivor) rather than writing
  vacuous tests.

## Links

- ADRs: `docs/product/architecture/adr-065-work-item-age-percentiles-compute-location.md`,
  `adr-066-aging-chart-ct-wia-line-source-swap.md`
- Architecture: `docs/product/architecture/brief.md` → "Application Architecture — work-item-age-percentiles (Story #5257)"
- Mutation reports: `docs/feature/work-item-age-percentiles/deliver/mutation/`
- Docs: `docs/metrics/widgets.md`
