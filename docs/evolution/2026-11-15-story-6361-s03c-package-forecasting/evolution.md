# Story 6361 — S03c Package Forecasting: the second context in its own module

**Finished:** 2026-11-15 · ADO User Story #6361 · Epic #6510 "Modules: package the bounded contexts"

## The problem

Forecasting lived flat in `Services/Implementation/Forecast` and `Services/Interfaces/Forecast`, with its updater,
seeder and triggers among the shared background services. It also formed a cycle with Flow metrics, the first packaged
module: Forecasting read throughput from Flow metrics, and Flow metrics called back into Forecasting three times
(the forecast filter rules, the data-sufficiency rule and a How Many forecast for the predictability score). With a
cycle, neither module can have its own rules, and Epic 6474 slice 07 has no clean seam to write `LeafCapacity` into.

## What changed

Three nwdes features on `main`, with no behaviour change.

- **Feature A, rules first** (`docs/feature/story-6361-s03c-a-rules-first/`, 3 slices). `FlowMetricsModuleRules`
  became the shared `ModuleRules`, with the Application inbound and outbound shapes lifted into it. Every rule keyed on
  Forecasting's namespaces, folders or files was widened to also see `Modules/Forecasting`, each proven with a made-up
  type. A safety net was added: `SeededForecastsStayTheSameTest` (pinned-seed percentiles at the port) and the public
  command `forecast-answers-compare` (128 forecast answers on demo data, two revisions).
- **Feature B, the move** (`docs/feature/story-6361-s03c-b-move/`, 3 slices). 31 production files moved, pure moves
  apart from namespace edits: `Domain` (the simulation, draw streams, run plan and the two forecast policies),
  `Application` (the forecast, reality-check and filter-rule services and their ports, `ForecastLane`,
  `IForecastUpdater`), `Application/Policies` (the four forecast triggers) and `Adapters/Background` (the updater and
  the start-up seeder). Handler order stayed pinned; three more handlers got order pins.
- **Feature C, cycle and rules** (`docs/feature/story-6361-s03c-c-cycle-and-rules/`, 4 slices).
  - A public command, `forecast-triggers-show`, shows on demo data that a Team refresh is followed by a forecast for
    every Portfolio the Team serves, and that a restart makes the next forecast round run in full (the start-up
    seeding).
  - The cycle is gone. Flow metrics owns three ports in its Application layer, `IThroughputFilter`,
    `IForecastDataSufficiency` and `IHowManyForecaster`. `ForecastFilterRuleService` answers the first; the new
    `ForecastingAnswersToFlowMetrics` answers the other two by delegating to the sufficiency policy and the forecast
    service. Forecasting → Flow metrics is the one legal direction.
  - Forecasting has its own module rules (Domain reaches only the kernel; outside reaches Domain, Adapters and
    Application only from inline lists; Application and Adapters outbound), the shared rule wording names the module
    it checks, and the rules no longer count a generic type parameter as an edge.
  - Forecasting's 24 unit-test files moved to `Lighthouse.Backend.Tests/Modules/Forecasting`, and
    `ForecastingUnitTestPlacementTest` keeps them there.
- **Docs, at finalize:** ADR-270, `ARCHITECTURE.md` §4, a new section in `docs/product/architecture/brief.md`, and the
  Epic catalogue's Improvement log.

## Key commits

| Commit | What |
|---|---|
| `f3a2b4a5d`, `634031db6` | A: `ModuleRules` renamed and the Application shapes lifted |
| `1f53489bb`, `15b8544f4` | A: every Forecasting-keyed rule sees `Modules/Forecasting` |
| `c33d5814f`, `091968d2f` | A: `forecast-answers-compare` and the seeded forecast check |
| `f1e51259b`, `1a724abd0` | B: Domain move, then callers |
| `a4d210610`, `92ff8d5bf` | B: Application move, then callers |
| `3ec86bdee`, `dfc5e1193` | B: policies and background adapters move, then callers |
| `10a991da5` | B: the perf harness reads `ForecastUpdater`'s log category under its new name |
| `c9e243e25` | B: order pins for the moved triggers |
| `a144816a5` | C: `forecast-triggers-show` |
| `9232beb3f` | C: Flow metrics asks Forecasting through its own three ports |
| `666c08ffa`, `48e38992f` | C: rule wording as a parameter; Forecasting's module rules; Flow metrics' inbound list 12 → 5 |
| `8abb0341d`, `e88d37a0c` | C: the 24 unit-test files moved; test clock and plain comments in the moved tests |

## What it achieved

- **Behaviour unchanged.** Metrics answers: 366 responses and 3 snapshot tables identical (base `7ec328cb6`).
  Forecast answers: all 128 identical (base `39a7f598f`). The forecast triggers and the start-up seeding are observed
  firing. No behavioural assertion changed.
- **Cycle removed.** Flow metrics' outbound Forecasting list 3 → 0 (kept as an empty, two-way list); its inbound list
  no longer names a Forecasting type.
- **Contexts packaged with namespace rules:** 1 → 2 of 11. Files under `Services`: 487 → 456; `Modules/Forecasting`:
  32 files.

## Quality

| Gate | Result |
|---|---|
| Feature A | Candidate review accepted; EXAMINE accepted (E1–E3 met, 128 forecast answers identical) |
| Feature B | Candidate review accepted after one rejection on a pin approval; EXAMINE E1, E2 and E4 met, E3 waived by the maintainer (no public command showed the triggers firing; feature C's S1 closed that gap) |
| Feature C | Every slice: oracle approved with its RED observed, slice review accepted. Candidate review accepted with no findings. EXAMINE accepted, E1–E6 met |
| Full backend suite | Green before each push (C: 13,447 → 13,513 passed, 0 failed); CI green for each push incl. sonar-gates |
| Mutation | Bar met; see `docs/feature/story-6361-s03c-c-cycle-and-rules/mutation/results.md` |

No frontend file changed, so no frontend gate ran.

## Decisions taken without the maintainer, open to revisit

- **Earlier features' oracles edited in C.** Removing the cycle empties lists that features A and B had pinned. Since
  those features were closed and integrated, their oracle checks were rewritten in place to the new shape (the
  outbound and inbound lists name no Forecasting type) instead of being kept stale.
- **Lazy forecast lookup.** `ForecastingAnswersToFlowMetrics` resolves `IForecastService` on first use, because
  constructor injection closes a DI cycle through `ITeamMetricsService` that the provider validation does not catch.
  It is a service-locator call, like the one `BaseMetricsService` already had; both go with S07b.
- **One instance answers two interfaces.** `Program.cs` registers `ForecastFilterRuleService` and the new adapter
  once each and forwards their interfaces to them, so DI lifetimes are unchanged and no interface is downcast.
- **Generic parameters are no edge.** The Adapters → shared update code baseline first listed `TEntity`; the shared
  rules now skip generic parameters, as the Domain rule already did, and a made-up-type test pins it.
- **B's unclosed EXAMINE draft** was moved out of the checkout (to the host's nwdes cache) because a dirty tree blocks
  every nwdes close.

## Knowingly left open

- **`ForecastUpdater` still builds on `UpdateServiceBase`, `UpdateKey` and `UpdateCancellationContext`** in
  `Services.Implementation.BackgroundServices.Update`. The edge is listed inline (3 entries) and only shrinks; the
  shared update code moves with S09a.
- **`IlDependencies` misses catch-clause types, events and named attribute arguments.** No current code hits it;
  candidate for a small improvement item.
- **Non-blocking review notes:** `OutboundEdges`' empty-subject text always says "Application", also for Adapters;
  the placement oracle pins the moved test names, so renaming one later needs an oracle edit in the same change.
- **Maintainer's production-like comparison** (hold 1 decision): one forecast-answers comparison on the production-like
  database, run by hand.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed: no route, screen, setting or answer. The architecture docs are under "Context map" below. |
| Screenshots | N/A, because no screen changed. |
| Demo data | N/A, because no data shape or seeded value changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed and nothing user-visible is new. |
| Clients (CLI, MCP) | N/A, because no route, DTO or enum changed (366 metrics and 128 forecast answers compared identical); no client release is due. |
| Context map, `ARCHITECTURE.md` | Done in this finalize: ADR-270, `ARCHITECTURE.md` §4, `docs/product/architecture/brief.md`, Epic catalogue. |
| Usage-data event | N/A, because no feature became usable or is used differently (DEVOPS answered the same). |
| Mutation | Done: no survivor added in the moved code (30 of 32 files byte-identical apart from `using`/`namespace` lines, proved mechanically); the new adapter has one tested mutant, an equivalent survivor, recorded. Scope option A chosen by the maintainer on 2026-11-15. |
| Release notes | N/A; written at /release. |
