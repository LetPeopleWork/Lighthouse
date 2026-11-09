# Story 6358 — S02 Flow metrics pilot: the first context in its own module

**Finished:** 2026-11-09 (delivered; the go/no-go gate awaits the maintainer) · ADO User Story #6358 · Epic #6510
"Modules: package the bounded contexts"

## The problem

ADR-270 decided where the backend is going: one folder per bounded context, `Modules/<Context>/{Domain, Application,
Adapters}`. Nobody had moved a context yet. Flow metrics was the hardest place to try it: it had no namespace of its
own, only a type-set rule over files flat in `Services/Implementation`, and its edges to other contexts were not
written down. S02 is the pilot and the gate: its lessons decide whether and how the other contexts move (S03*).

## What changed

The work ran as three nwdes features on `main`, with no behaviour change.

- **Feature A, rules first** (`docs/feature/story-6358-s02a-rules-first/`, 2 slices). Every architecture rule that
  examines or forbids the application core now uses one pattern, `ApplicationCore` (`Services` or `Modules`), and
  finds domain-event handlers by their interface. A made-up type under `Modules` fails each rule as the same type
  under `Services` does, also when the edge sits only inside an `async` method. A fixed-list rule fails when a named
  type is gone. A new test, `DomainEventHandlerOrderPinTest`, pins the order of the handlers of `TeamDataRefreshed` and
  `PortfolioFeaturesRefreshed`. No production file changed.
- **Feature B, the move** (`docs/feature/story-6358-s02b-flow-metrics-move/`, 5 slices). 42 production files moved,
  namespace and folder only, into `Lighthouse.Backend.Modules.FlowMetrics`: `Domain` (4 files), `Application` (27),
  `Application.Policies` (7), `Adapters.Background` (1) and `Adapters.Persistence` (3). 23 more production files
  outside the module changed, `Program.cs` among them. Repository recognition was gathered into one test support,
  `RepositoryRecognition`, and widened to a module's `Adapters.Persistence` and to `I…Repository` ports in a module's
  `Application`. `FlowMetricsModuleLayoutArchUnitTest` pins every moved type to its layer and folder, and pins the
  Domain to the kernel and the base library (R2).
- **Feature C, the module rules and the test move** (`docs/feature/story-6358-s02c-flow-metrics-rules/`, 4 slices).
  Three dependency rules, read from the compiled IL, each with an inline two-way baseline: R3 (outside → Domain from
  6 listed types; outside → Adapters from the composition root only), R4 (outside → Application from API, the
  composition root, Reports, Refinement and 10 listed types) and R5 (Application → no EF Core, `Data`, `Adapters` or
  `API`; → Forecasting along 3 listed edges). The two-way comparison moved into a shared `InlineBaseline`. The old
  type-set rule's hand-written opcode scanner is deleted; the two name rules on the metrics services stay. 33 unit-test
  files moved under `Lighthouse.Backend.Tests/Modules/FlowMetrics/{Domain, Application, Adapters}`, and
  `FlowMetricsUnitTestPlacementTest` keeps each class in the namespace of its folder.
- **Examiner commands.** `.nwave/examine.json` and `Scripts/examine/` declare four public commands for the
  source-blind EXAMINE: `architecture-rules`, `metrics-answers-compare`, `ef-model-unchanged` and `backend-tests`.
- **Docs, at finalize:** ADR-270 (settled folder names, measured edges, glossary, the D-12 answer, lessons and the
  gate recommendation), `ARCHITECTURE.md` §4, a new section in `docs/product/architecture/brief.md`, and the Epic
  catalogue's Improvement log and S03*/S09a notes.

## Key commits

| Commit | What |
|---|---|
| `a0be163c2` | DESIGN: module layout, slices and spelling |
| `dccce57e3` | DEVOPS: platform lens |
| `56029f201` | Behaviour capture script for the move (366 GET requests, 3 snapshot tables) |
| `9838582d7` | Baseline mutation run before the move |
| `af9faa739`, `b94606823`, `9ffb5b32c` | A: the application-core rules defined once and reaching `Modules` |
| `b32e172cb` | A: handler-order pin |
| `da764de00`, `23345b8f8` | Examiner command for the architecture rules |
| `699b5eed1`, `a28c90685`, `ed34e1a30`, `3ebe75461`, `676edaa7e` | B: the five pure-move commits (Domain, contract, implementations, policies, adapters) |
| `36dedd691`, `92454aa4c` | B: repository recognition in one place, then widened to a module |
| `6fa4f3b2c` | B: oracles S1 to S5 re-approved on the final bytes |
| `58b82ecfc`, `81ab8697c` | Examiner commands for the answer comparison, the EF model check and the backend suite |
| `d5dc9c031` | C: `InlineBaseline` lifted out of the repository ratchet |
| `13b0416c1`, `3822b4265`, `15007ef2f` | C: rules R3, R4 and R5 |
| `e0ab4fbbc` | C: opcode scanner deleted, module found by its namespaces |
| `132a6b441` | C: the 33 unit-test files moved |
| `4a99b633a` | C: rule explanations without internal reference ids |
| `7dafe5ffe` | Examiner sees each architecture rule by name, in Debug or Release |
| `d8b1ea4a2` | Slice 8: mutation run after the move |

## What it achieved

- **Behaviour unchanged.** The capture of 366 metrics API answers and 3 snapshot tables was identical on the base
  (`7ec328cb6`) and the moved code (`81ab8697c`), on the same day. `dotnet ef migrations has-pending-model-changes`
  reports no change on SQLite and on Postgres. No behavioural assertion changed.
- **KPI 1:** loose files in `Services/Interfaces` and `Services/Implementation` 41 + 61 → 34 + 44; all files there
  527 → 485.
- **KPI 2:** contexts packaged with namespace rules 0 → 1 of 11.
- **Edges named.** R3 6 types, R4 10 types (the design expected 8: `ForecastInputs` and
  `ForecastService+GatheredInputs` are extra), R5 3 Forecasting edges (the design expected 1 plus 1 invisible:
  `BaseMetricsService` → `IForecastService` turned out to be visible in the IL, and `TeamMetricsService` →
  `ForecastDataSufficiencyPolicy` was not in the design).

## Quality

| Gate | Result |
|---|---|
| Feature A, candidate review (`cead197bf`) | Accepted, no findings: 540 Architecture tests green in Release, order pin red under a swap probe, no production file in the diff |
| Feature A, EXAMINE | E1 met (540/540 rules); E2 and E3 indeterminate (see below) |
| Feature B, candidate review (`6fa4f3b2c`) | Accepted, no findings; rename diff checked |
| Feature B, EXAMINE | All 4 expectations met: 572 architecture tests passed; 366 answers and 3 tables identical; EF model unchanged on both providers; backend suite 13,063 tests, 13,051 passed, 12 skipped, 0 failed |
| Feature C, candidate review (`4a99b633a`) | Accepted with 4 non-blocking notes (see below). Reviewer's run 1,352 passed, 0 failed; a deliberate break of a Forecasting edge failed R5 and named it. Host gates on the same commit: `dotnet build` 0 warnings; filtered `dotnet test` 13,130 passed, 2 skipped, 0 failed; Architecture tests in Release 641/641 |
| Feature C, EXAMINE | All 3 expectations met: 102/102 Flow metrics rules in Debug and in Release; backend suite 13,132 tests, 13,120 passed, 12 skipped, 0 failed; EF model unchanged |
| Mutation (slice 8, `50c4a7367`) | Stryker.NET 5.0.0 on the 42 moved files, same test filter as the baseline: 1,477 mutants tested, 1,116 killed, 360 survived, 260 no coverage, 1 timeout, 183 compile errors, **64.31 %**. Identical to the baseline (`56029f201`), file by file. See `docs/feature/story-6358-s02-metrics-pilot/mutation/` |

No frontend file changed, so no frontend gate ran.

## Decisions taken without the maintainer, open to revisit

- **Examiner commands in the repository.** nWave 4's source-blind examiner runs only commands a candidate declares, so
  `.nwave/examine.json` (the one file kept from the otherwise ignored `.nwave/` folder) and `Scripts/examine/` were
  committed. A build step writes `Scripts/examine/bin/` (ignored) with the checkout path in it. They are reusable by
  every later S03*; they can also be removed if the maintainer prefers not to keep examiner tooling in the repository.
- **The docs slice left nwdes.** DESIGN slice 9 (ADR-270, `ARCHITECTURE.md`, `brief.md`, catalogue) was planned as a
  slice. nwdes accepts a prose-only slice only under its own doc folders, so these edits were made in this finalize
  checklist instead.
- **Feature A's EXAMINE was indeterminate on E2 and E3.** No public command could show "a reorder fails the pin" or
  "no production file changed". Both rest on review evidence instead: swap probes run by three independent agents, and
  a candidate diff of test files only. The indeterminate draft was kept out of the repository (`7ec328cb6`). Features
  B and C declared commands and got every expectation met.
- **The mutation score cannot reach the project's 80 %.** The bar set at DESIGN was "no file lower than its baseline",
  and it is met. Five files sat below 80 % before the Story and still do: `PortfolioMetricsService` 27.60 %,
  `TeamMetricsService` 30.58 %, `DemoBlockedHistoryBackfillHandler` 50.75 %, `PercentilesOverTimeSeriesQuery`
  66.67 %, `BaseMetricsService` 67.75 %. Their behaviour is mostly pinned by `API.Integration` scenarios that the
  filter leaves out. Killing the survivors needs new behaviour tests, which a behaviour-free move cannot add.
- **Feature C's four non-blocking review notes were not acted on:**
  1. a muddled comment above a fixture array in `FlowMetricsInboundDomainAndAdaptersArchUnitTest.cs`;
  2. the Application inbound and outbound rules are private helpers in their oracle files, not on
     `FlowMetricsModuleRules` as the design said;
  3. `TheHandWrittenScannerOfTransitionReads_IsGone_AndTheTwoNameRulesOfTheMetricsServicesStay` is a permanent guard
     of history, not behaviour; the reviewer advises deleting it after finalize, as R5 already forbids any EF Core
     reach;
  4. the five canary namespaces were not each broken by hand; the reviewer reasoned from `IlDependencies.TypesIn`.

## Gate decision

Go-with-changes, decided by the maintainer on 2026-11-09 (ADR-270, "Gate decision"). Changes 1, 2 and 4 apply to
every S03* Story from its start. The next Story that touches the module rules first lifts the Application rules into a
shared class. S03b decides the Work Items → Flow metrics edge, and S03c the Flow metrics ↔ Forecasting cycle.

## Knowingly left open

- **Open question 3 of the design:** should S04b record a hard dependency on a migration spike for the three
  snapshot entities, whose names sit in the EF model snapshots of both providers?
- **Open question 4 of the design:** `TeamMetricsService.UpdateTeamMetrics` writes through
  `IWorkItemRepository.Save`. Leave it to S07b and S05f, or note it for Work Items (S03b)?
- **Survivor-killing for the metrics services**, as its own improvement item (the five files above).
- **The advisory to drop the "scanner is gone" guard** (review note 3).
- Open question 5 is answered: the service-locator edge is visible in the IL, so no source scan is needed.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because nothing a user sees changed: no route, screen, setting or answer. The architecture docs are under "Context map" below. |
| Screenshots | N/A, because no screen changed. |
| Demo data | N/A, because no data shape or seeded value changed; the demo data reads the same. |
| Website assets | N/A, because no asset under `docs/assets/` changed and nothing user-visible is new. |
| Clients (CLI, MCP) | N/A, because no route, DTO or enum changed (366 answers compared identical), so no client follows and no client release is due. |
| Context map, `ARCHITECTURE.md` | Done in this finalize: `docs/product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md` (Decision 1 row, Decision 6 settled names, measured edges and "Every edge" rows, glossary, Decision 8 row, D-12 answer, lessons and gate recommendation); `ARCHITECTURE.md` §4 (row 5, diagram, enforced rules); `docs/product/architecture/brief.md` (new section); `docs/feature/epic-6264-modular-monolith/feature-delta.md` (Improvement log, S03* and S09a notes). |
| Usage-data event | N/A, because no feature became usable or is used differently; nothing to add to `UsageDataEventName` or `docs/settings/usagedata.md`. |
| Mutation | Done: 64.31 % (1,116 killed, 360 survived), identical to the baseline file by file; `docs/feature/story-6358-s02-metrics-pilot/mutation/baseline.md` and `slice8.md`. Below the project's 80 %; the shortfall is pre-existing in five files and a move cannot change it. Frontend: N/A, because no frontend file changed. |
| Release notes | N/A; written at /release. |
