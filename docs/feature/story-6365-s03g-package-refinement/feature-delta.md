# Feature: story-6365-s03g-package-refinement
Story #6365 · S03g Package Refinement · Epic #6510 (Modules: package the bounded contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (S03g row, D-6264-16, the S03a/S03d/S03e notes on module layout).
- Brief: [`slices/slice-09-s03g-package-refinement.md`](../epic-6264-modular-monolith/slices/slice-09-s03g-package-refinement.md) (Size S).
- Authority: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)
  (Refinement row "Packaged (the leaf form)", settled folder names, layout), `ARCHITECTURE.md` §4 row 8.
- Precedents copied: [S03f](../story-6364-s03f-platform-composition-root/feature-delta.md) (preservation law, E13
  lesson, D-4 repositories), [S03e](../story-6363-s03e-package-access-licensing/feature-delta.md) (Access list naming
  `VoterIdentityResolver`).

DESIGN and DEVOPS run 2026-11-21, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is
recorded as such; the open questions at the end are confirmations with the default taken, not blockers. Nothing a
user sees changes. Nothing was built or run with `dotnet` or `pnpm`; every count is a **text measurement on `main` at
`fcb1ff878`** (`git ls-files`, `git grep`), confidence `noisy` (misses same-namespace and fully qualified references,
matches comments). The compiled IL measurement in slice S1 decides.

**Start condition.** S03e (#6363) and S03f (#6364) are delivered and closed before S1 starts. Both edit the rule
files this Story edits (`ModuleBoundariesArchUnitTest`, `TrackerIntegrationModuleBoundariesArchUnitTest`, the Work
Items / Forecasting / Flow metrics boundaries files, `RepositoryRecognition`), S03e adds Access lists that name
`Services.Implementation.Refinement.VoterIdentityResolver` by full name, and S03f moves the clock, the blackout
services' neighbours, `DemoDataService` and the dispatcher into `Platform/*`. **Every count below is re-measured at
the Story's start (S1)**; a difference is recorded, not argued with.

## Wave: DESIGN / [REF] Inherited
- ADR-270: Refinement is a supporting context, "Packaged (the leaf form)"; folder `Modules/Refinement`; namespace =
  folder; `Domain`, `Application` (ports co-located), `Application/Policies`, `Adapters/Background`,
  `Adapters/Persistence`; tests mirror under `Lighthouse.Backend.Tests/Modules/Refinement`; rules read from the IL
  (`IlDependencies`); lists inline, compared both ways, only shrink (ADR-268).
- `ARCHITECTURE.md` §4 row 8: Refinement "depends **down only** — on WorkItems, Forecasting, Metrics and the kernel,
  never on Tracker integration or Portfolio/Delivery — and **nothing but `API` depends on it**". Anchor today:
  `Services.*.Refinement`, `API/Refinement*Controller`.
- S03f D-4: every repository stays in `Services/*/Repositories`; "S03g sizing log" named as owner.
- S03a D-3 (via S03f): a `Models` type moves only if it is not an EF entity, not on the wire, not reached by a kernel
  type and not used outside the context.

## Wave: DESIGN / [REF] DoR re-run (2026-11-21)
| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Refinement lives in `Services/Implementation/Refinement` (13) and `Services/Interfaces/Refinement` (3); its rules (`RefinementModuleArchUnitTest`, `ModuleBoundariesArchUnitTest.RefinementPattern`) key on those namespaces (E1). |
| 2 Users / value | PASS | Maintainers: the context sits in its ADR-270 folder; the "down only / only API" rules hold by `Modules.Refinement` namespace. No user-visible change. |
| 3 Hypothesis | PASS, sharpened | "Packages with namespace changes only." Holds as "namespace, `using` and fully qualified name changes; `using`s added where an ancestor namespace resolved a type implicitly": `Program.cs` lines 1432-1439 register by **fully qualified** name (8 lines change text, not order or lifetime; E3), and moved files lose the implicit resolution of `Lighthouse.Backend.Services.Implementation` types (E9). Disproved only if a production edit beyond those is needed. |
| 4 Size | PASS | **S, about 2.5 days**, one nwdes feature, 3 slices. |
| 5 AC from UAT | PASS, one AC made precise | (a) "No Refinement file remains in `Services/Interfaces` or `Services/Implementation`": holds for the 16 files of D-2. `SizingLogRepository` + `ISizingLogRepository` are in `Services/*/Repositories` and **stay** (D-4), named with owner in the membership. (b) Down-only and only-API rules green by namespace (S3). (c) `ARCHITECTURE.md` §4 anchors updated (S3). |
| 6 Dependencies | PASS | Hard: S01, S02 gate. **Added: S03e and S03f closed first** (shared rule files). Soft: S05* / S10 own repositories and `Data`; S03i owns `WeeklyRecurrence` and the blackout services; Epics #6137 / #6138 own new behaviour. |
| 7 Technical notes | PASS | No `GlobalSuppressions.cs`, `.editorconfig`, `.globalconfig`, `.csproj`, Stryker config or CI path classifier names a Refinement type or path today (E5). Rate-limit policy `RefinementContributionPolicy` and `RefinementController.VoterKeyHeader` are API / configuration names, unchanged. |

## Wave: DESIGN / [REF] Baseline (`main` at `fcb1ff878`, text search, `noisy`)
| Group | Files | Moves? |
|---|---|---|
| `Services/Implementation/Refinement/*` | 13: `NeedBand`, `RefinementCadenceCalendar`, `RefinementCalendar`, `RefinementList`, `RefinementNeedCalculator`, `RefinementResolution`, `RefinementViewQuery`, `SizingLogCommands`, `SizingRefusal`, `SleYardstickResolver`, `StageRuleHealing`, `StageRuleMatcher`, `VoterIdentityResolver` | yes (D-2) |
| `Services/Interfaces/Refinement/*` | 3 ports with their records: `IRefinementCalendar` (+`RefinementCalendarFacts`, `RefinementCycle`), `IRefinementViewQuery` (+ 15 records/enums), `ISizingLogCommands` (+`SizingVote`, `SizingComment`, `Voter`, `SizingOutcome`) | yes, co-located in `Application` (D-1) |
| `Services/*/Repositories/{ISizingLogRepository, SizingLogRepository}` | 2 | **no** (D-4) |
| `Models/Refinement/*` | 9: `RefinementSettings` (on `Team`, in `LighthouseAppContext`), `SizingLogEntry` (EF entity), `RefinementVerdict` (usage-data wire), `SizingAnswer`, `SizingChannel`, `SizingEntryKind`, `YardstickSource`, `RowReadiness`, `NeedUnavailableReason` (all in API DTOs) | **no** (D-3) |
| API: `RefinementController`, `RefinementVotesController`, `RefinementSettingsValidator`, 5 DTOs | 8 | no (driving adapter) |
| Unit tests `Tests/Services/Implementation/Refinement/*` | 11 | yes → `Tests/Modules/Refinement/{Domain, Application}` |
| `Tests/Services/Implementation/Repositories/SizingLogRepositoryTest` | 1 | no (follows D-4) |
| Acceptance / wire tests `Tests/API/Integration/Refinement/*` (33), `Tests/Contracts/Refinement/*` (4), `Tests/Models/Refinement/*` (2), usage-data host tests (3) | 42 | no (drive the API port; `using` lines only) |

Inbound (production, `using` or fully qualified): `API/RefinementController`, `API/RefinementVotesController`,
`API/TeamController`, `API/DTO/RefinementViewDto`, `API/DTO/SizingLogDto`, `Program.cs` (8 FQN lines). Nothing else:
the only-API rule holds today. Test files naming the namespaces: **21** (text search) — the 11 moving unit tests plus 10 others:
`API/Integration/Refinement/{RefinementAtThePortTest, Slice12CommentsScenarios, Slice12CommentsSpecifications}`,
`API/RefinementVotesControllerTest`, `Services/Implementation/Auth/CurrentUserProfileServiceTest` (line 4),
`Architecture/{CategoryListOwnershipGuardTest, FeatureOrderingSingleSourceArchUnitTest (line 29, `typeof(RefinementList)`),
RefinementModuleArchUnitTest, RepositoryLeakRatchetArchUnitTest, TrackerIntegrationModuleBoundariesArchUnitTest}`.

Outbound (from the 16 files, `using` lines): kernel `Models.*`; Forecasting `Application` (3 files:
`RefinementNeedCalculator`, `StageRuleHealing`, `StageRuleMatcher`); Flow metrics `Application` (2); Work Items
`Application` (1); loose `Services.Interfaces` (4 files: `ILighthouseClock`, `IBlackoutPeriodService`,
`IForecastFilterRuleService`, `IForecastService`, `ITeamMetricsService`, `IRuleFieldProvider`); repositories
(`ISizingLogRepository`, `IWorkItemRepository`); Access (`VoterIdentityResolver` → `Services.*.Auth`, `Models.Auth`);
Organisation set-up `WeeklyRecurrence` (same parent namespace, no `using`; seen in `RefinementModuleArchUnitTest`).
No edge to Tracker integration or the update code (existing rule).

## Wave: DESIGN / [REF] Evidence
| # | Finding | Consequence |
|---|---|---|
| E1 | Rule constants keyed on the old namespaces: `ModuleBoundariesArchUnitTest.RefinementPattern` `^…Services\.(Implementation\|Interfaces)\.Refinement(…)` — read by `RefinementModuleArchUnitTest`, `FlowMetricsInboundApplicationArchUnitTest`, `ForecastingModuleBoundariesArchUnitTest`, `WorkItemsModuleBoundariesArchUnitTest` and the non-empty guard (line 126); `TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing` (line 58); in `RefinementModuleArchUnitTest`: `ServicesImplementationPattern`, `RefinementImplementationNamespace`, `RefinementResolutionAndItsNestedTypesPattern`, `RefinementCadenceCalendarAndItsNestedTypesPattern`, `NeedBandAndItsNestedTypesPattern`. | A move without S1 drops Refinement from the only-API and down-only rules **silently** (they would check an empty set; only the line-126 guard fails). S1 widens `RefinementPattern` (one owner, every reader follows) and the per-type patterns; S3 narrows. |
| E2 | Non-constant / non-regex uses the reflection law misses: `ReadPath()` compares `type.Namespace.FullName == RefinementImplementationNamespace` (exact equality; a split into `Domain` + `Application` leaves `NeedBand` out of the read-path check); `The_stage_rule_matcher…` uses `DoNotResideInNamespace(RefinementImplementationNamespace)`; `The_need_calculator_lives_in_the_Refinement_module` asserts the exact old namespace; `ServicesImplementationPattern` used as "reaches no service" — after the move other Refinement services no longer match it, so `NeedBand` could reach `RefinementNeedCalculator` unseen. | S1 rewrites them over the membership map: read path = every Refinement type in `Domain` or `Application` minus the write path (`SizingLogCommands`), `Adapters` excluded; "reaches no service" = `ServicesImplementationPattern` ∪ the Refinement `Domain`/`Application` namespaces minus the type itself. |
| E3 | `Program.cs` lines 1432-1439 register 8 Refinement types by fully qualified name. | Text of 8 lines changes in S2; order, lifetime and service types do not (reviewer diff check). `Program.cs` is a shared path for CI selection (S03f D-12, re-checked in S1), so S2's push runs the live suites once. |
| E4 | String literals naming moving paths/types: `CategoryListOwnershipGuardTest.cs:20` and `RepositoryLeakRatchetArchUnitTest.cs:46` (`"Services/Implementation/Refinement/RefinementList.cs"`); `TrackerIntegrationModuleBoundariesArchUnitTest.cs:426` (`"…Services.Interfaces.Refinement.IRefinementViewQuery"`) and its fixture at line 851 (`global::` reference, compile-checked); after S03e, the Access list entry for `VoterIdentityResolver` (S03e lines 122, 164). Compile-checked `using` / `typeof` users outside the moving set: `FeatureOrderingSingleSourceArchUnitTest.cs:29`, `CurrentUserProfileServiceTest.cs:4`. | Renamed in S2's edit commit; counts unchanged. A missing path fails loudly; the fixture fails to compile. S1's literal sweep lists every one so none is left. |
| E5 | Lessons from the S03f review, checked for Refinement: `GlobalSuppressions.cs`, both `.editorconfig`, any `.globalconfig`: **no** Refinement / Sizing target. `.csproj` / `Directory.Build.props`: none. Repo Stryker configs (`stryker.6094.backend.json`): none (only archived `docs/feature/bug-6458…` names `RefinementSettingsValidator`, which stays in API). `Scripts/test-selection/path-classifier.sh` / `dev-test.*`: no Refinement path (backend selected by the generic backend prefix). `Scripts/test-timings/test-time-budget.json:109` and `test_check_budget_time_budgets.py:44` name `API.Integration.Refinement.Slice13ReadinessTest`, which **stays**. `TestHostUseGuardTest.HostUsersStillToMove` lines 158-172, 277-278 name 15 + 2 acceptance / usage-data classes in `API.Integration.Refinement` / `Integration.UsageData`, which **stay**. `SharedHostsDrainTheUpdateQueueGuardTest:43` names `RefinementAcceptanceTest`, stays. **Log categories**: of the 16 files only `SizingLogCommands` takes an `ILogger<SizingLogCommands>` (line 15), so its category becomes `Lighthouse.Backend.Modules.Refinement.Application.SizingLogCommands`; no Serilog override in any `appsettings*.json` names a Refinement namespace (`appsettings.json:70` `RefinementContribution` is a rate-limit key, unchanged); `Lighthouse.EndToEndTests/perf/helpers/collect.ts` keys on logger categories (line 81, `ForecastUpdater`) but names no Refinement category. | Nothing to rename there; S1's re-grep repeats this on the post-S03f `main` and records the result in the membership file's comment-free data, so a new target is caught. |
| E6 | `ForecastingRulesSeeTheModuleArchUnitTest:130` reads `RefinementModuleArchUnitTest.BackgroundUpdatesPattern` **by member name** through reflection. | The constant keeps its name and meaning; S3 must not rename it. |
| E7 | `ISizingLogRepository` is used by `DemoDataService` (Platform lane after S03f), `RefinementViewQuery`, `SizingLogCommands`, `Program`. | Moving the port into `Modules.Refinement` would add a Platform → Refinement edge and break "nothing but API depends on it" (D-4). |
| E8 | Static pure types by `using`: `NeedBand` (kernel only), `SizingRefusal` (kernel only); `RefinementResolution`, `RefinementCadenceCalendar` reach `Services.Interfaces.Refinement` records (→ `Application`); `StageRuleHealing` reaches Forecasting `Application`. | Expected `Domain` = {`NeedBand`, `SizingRefusal`}; the IL decides (D-1). |
| E9 | Moved files resolve ancestor-namespace types with no `using`: `RefinementCadenceCalendar.cs:27` uses `WeeklyRecurrence` from `Lighthouse.Backend.Services.Implementation`; the same holds for any loose `Services.Implementation` / `Services.Interfaces` type a Refinement file names without a `using`. | S2 adds those `using`s. S1's IL measurement lists every such implicit edge (origin, target) in the membership data; R-R6's list is built from the full IL edge set, never from `using` lines. |

## Wave: DESIGN / [REF] Decisions
All taken as recommended (AFK, 2026-11-21).

### D-0 No schema change, no behaviour change — RECOMMENDED
No entity, `DbSet`, migration, route, DTO, header, rate-limit policy or configuration key changes. Checked by
`ef-model-unchanged`, an empty diff under `Lighthouse.Migrations.*`, `Lighthouse.Frontend`, `Lighthouse.EndToEndTests`.

### D-1 Layout — RECOMMENDED: `Modules/Refinement/{Domain, Application}`; a type goes to `Domain` iff its IL edges reach only the kernel, the BCL and other Refinement `Domain` types
| Option | Verdict |
|---|---|
| **a (recommended)** `Domain` for kernel-only pure types (expected `NeedBand`, `SizingRefusal`), `Application` for the rest, ports co-located with their records | Same shape as Flow metrics, Forecasting, Work Items; "Domain → kernel only" is then a cheap `ModuleRules` rule. Membership by measured property, not by name (GDP-8). |
| b Everything in `Application` | Rejected: two pure decisions lose the kernel-only guarantee that the other contexts state. |
| c Pull more into `Domain` by moving records out of the ports | Rejected: that is an edit, not a move (disproves the hypothesis for no value). |
No `Application/Policies` (no Refinement file names `IDomainEventHandler`, text search),
no `Adapters/Background`, no `Adapters/Persistence` (D-4).

### D-2 Membership — RECOMMENDED: the 16 files move; the rest is named with owner
Moving: the 13 + 3 of the Baseline. Staying, each named in `RefinementMembership` with its reason: the 2 sizing-log
repository files (D-4), the 9 `Models/Refinement` types (D-3), the API controllers / validator / DTOs (driving adapter),
`WeeklyRecurrence` (Organisation set-up, S03i).

### D-3 `Models/Refinement` — RECOMMENDED: all 9 stay
Each fails at least one S03a condition: `SizingLogEntry` and `RefinementSettings` are EF-mapped (`LighthouseAppContext`,
`Team`); `RefinementVerdict` is on the usage-data wire; the other six are on the API wire (DTOs). Moving them changes
EF snapshots or wire namespaces for no value.

### D-4 Sizing-log repository — RECOMMENDED: stays in `Services/*/Repositories`
| Option | Verdict |
|---|---|
| **a (recommended)** Stay (S03f D-4); named in the membership with owner Refinement and target `Modules/Refinement/Adapters/Persistence` + port in `Application` "when S05*/S10 reshape repositories" | Keeps the only-API rule list-free: moving the port would make `DemoDataService` (Platform) → Refinement a listed inbound edge (E7). `RepositoryRecognition` and `Nothing_that_touches_the_sizing_log_table…` stay unchanged. |
| b Move both into `Modules/Refinement` and list `DemoDataService` → `ISizingLogRepository` | Rejected now: adds the context's first inbound exception and edits `RepositoryRecognition` users for one file, while S05* will reshape repository ports anyway. |

### D-5 Rules first — RECOMMENDED: EXTEND S03f's preservation law with a Refinement membership map
For every regex constant `P` in the test assembly and every moved type `T`: `IsMatch(P, old(T)) == IsMatch(P, new(T))`,
and no added alternative matches a production type outside `Modules.Refinement`. If S03f's
`PlatformLaneRulesSeeTheMoveArchUnitTest` was delivered with the map as a parameter, Refinement adds its map; if it is
platform-specific, S1 lifts the law into a shared `NamespaceMoveLaw` taking a map (one owner) and both call it. A second
assertion sweeps rule constants and string literals in the test project for `Services.Implementation.Refinement`,
`Services.Interfaces.Refinement` and `Services/Implementation/Refinement`. It is not circular: its rename list is
**fixed at S1 from the pre-move grep and only shrinks**; each entry must be a literal that S2 renames (S2 removes it from
the list when it renames it, and the sweep fails if a listed literal no longer exists or an unlisted one appears).
RED in S1: the E2 uses (`RefinementImplementationNamespace` `==`, `DoNotResideInNamespace`, the exact-namespace
assertion) are not on the list — they must be rewritten over the map, not renamed — so the sweep fails until S1 rewrites
them. Old-namespace **alternatives** added by S1's widening are allowed by the list until S3 (see S3's RED). Rejected:
one `…RulesSeeTheModule` fixture type per pattern (Forecasting style) — covers only the patterns someone remembered.

### D-6 Tests move with production — RECOMMENDED; acceptance and wire tests stay
The 11 unit tests move in S2's `refactor(refinement): move … tests` commit to `Tests/Modules/Refinement/Domain`
(`NeedBandTest`) and `.../Application` (10), namespace = folder, kept there by `RefinementUnitTestPlacementTest` (S3).
The 33 port/host acceptance files, 4 wire-contract files, 2 model tests, 3 usage-data tests and
`SizingLogRepositoryTest` stay; only `using`s change. "Improve the tests you touch": moved test classes are read for the
test-writing skill's backend half; any host user among them moves one row off `HostUsersStillToMove` in a separate
edit commit only if it is cheap (none expected: the 11 are unit tests).

### D-7 DI — RECOMMENDED: same registrations, new names
`Program.cs` lines 1432-1439: the fully qualified names change; order, lifetimes, service/implementation pairs do not.
No `AddRefinement()` extension (module start-up belongs to S09a).

### D-8 New rules — RECOMMENDED: `RefinementModuleBoundariesArchUnitTest` on `ModuleRules`, plus the existing guards kept
See Ratchet design. `RefinementModuleArchUnitTest` keeps every assertion (static-ness, read path, sizing log
append-only, votes controller, migrations' cascades); only its constants change (S1 widen, S3 narrow).

### D-9 CI and pushes — RECOMMENDED: commit per slice, push S1-S3 together
S2 touches `Program.cs`, which selects every live connector suite; one push means one live run. Push only with all three
green locally (fast loop and the filtered full gate).

### D-10 Behaviour-free proof and EXAMINE — RECOMMENDED: reuse admitted probes, no new compare command
| Expectation | Probe |
|---|---|
| X1 Same types, new namespaces; all rules green; Refinement rules green by namespace | `architecture-rules` (all) |
| X2 No EF model or migration change | `ef-model-unchanged` |
| X3 Same Refinement answers (tab, need, votes, comments, take-back, settings) | `backend-tests` on the unchanged-assertion Refinement acceptance and wire-contract classes, both revisions |
| X4 CI selects backend for `Modules/Refinement` paths; live suites for `Program.cs` | `test-selection-show` |
Rejected: a `refinement-answers-compare` command (thin, on the `deliveries-answers-compare` pieces) — the 37 port and
wire tests already observe every answer and their assertions do not change; open question 3.

### D-11 Mutation bar — RECOMMENDED
Last, on frozen code, Stryker.NET over `Modules/Refinement/**` (under the x64 SDK, `run-backend-x64.ps1`), compared with
the same files' run on the pre-move revision: **no survivor or no-coverage mutant added in the moved code**; the 80 %
project bar reported.

### D-12 Formal verification — RECOMMENDED: no Quint / TLA+ / Agda model; the law is executed exhaustively
The claims are structural relations over a finite, closed set (the compiled types and the membership map): no state,
transition, concurrency, time or unbounded input. The preservation law and the boundary rules are exhaustive finite
checks executed by NUnit over the IL — stronger than a model of them, and they are the executable projection the
default suite runs. PBT: N/A, closed finite domain (`nw-property-based-testing` falsifier gate). No proof obligation is
declared, so no checker install or waiver is needed.

## Wave: DESIGN / [REF] Slices
One nwdes feature `story-6365-s03g-package-refinement`, 3 slices, each ≤ 1 day, moves in commits apart from edits
(`refactor(refinement): …`). Size **S, about 2.5 days**.

| # | Delivers | Oracle (native NUnit, `Lighthouse.Backend.Tests/Architecture/`) | Production targets | Est. |
|---|---|---|---|---|
| S1 | **First action: confirm S03e (#6363) and S03f (#6364) are delivered and merged into `main` (`git merge-base --is-ancestor` of their tips), then re-measure every count of this file.** Rules see the move before it happens: membership measured from the IL (each type, layer, old and new namespace; staying files with owner and reason); every rule constant and literal that names Refinement widened to old ∪ new; read path and "reaches no service" rewritten over the map (E2); re-grep of suppression / analyser / CI / budget / host-user / Stryker targets recorded (E5) | `RefinementMembership.cs` + `RefinementMembershipArchUnitTest.cs` (namespace-agnostic: a listed type is found in its old **or** new namespace; RED when a listed name does not exist or a `Domain` candidate's IL edges leave the kernel); the law of D-5 over the Refinement map — RED today on `ServicesImplementationPattern`-based exclusions and the per-type patterns that do not match the new names, and on the literal sweep | none | 1 d |
| S2 | Refinement lives in `Modules/Refinement/{Domain, Application}`; its unit tests in `Tests/Modules/Refinement/…`; `using`s, `Program.cs` FQNs, the E4 literals and the Access list entry renamed | S1's membership test now resolves every type in its **new** namespace (tightened: old namespace no longer accepted for moved types — RED before the move); all existing rules green | 16 production files moved; `Program.cs`, 5 API files, 19 test files (`using`s) | 0.75 d |
| S3 | Rules by namespace only: `RefinementPattern` and per-type patterns narrowed to `Modules.Refinement`; new boundaries rules; unit-test placement; `ARCHITECTURE.md` §4 row 8 anchor + rules bullet, ADR-270 Refinement layout line ("Refinement today: `Domain` (2), `Application` (14)…"), catalogue S03g row and brief ACs | **RED oracle**: S1's sweep run with an **empty** rename / allowance list — no rule constant and no string literal in the test project may still match `Services\.(Implementation\|Interfaces)\.Refinement` or `Services/(Implementation\|Interfaces)/Refinement`. RED at S3's start (S1's widened old-namespace alternatives in `RefinementPattern`, `RefinementReportsAccessAndLicensing` and the three nested-type patterns still match), GREEN once S3 narrows them. Written first, observed RED, then narrowed. Alongside (green on writing, guards later regressions, not the RED): `RefinementModuleBoundariesArchUnitTest.cs` (R-R1..R-R6), `RefinementUnitTestPlacementTest.cs`, and planted-type falsifiers in the `ModuleRulesCatchANewOffenderArchUnitTest` style | none (test side and docs) | 0.75 d |

Commands (host runs; nothing run in DESIGN):
```
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Refinement"
dotnet build Lighthouse.Backend                                            # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```
Driving port for every oracle: the compiled production assembly read through `LighthouseArchitecture.Production` /
`IlDependencies` (in-process; the nearest executable argv is the `dotnet test … ~Architecture` line). Behaviour is
observed through the unchanged Refinement acceptance tests at the application-service port and the wire contracts.

Rigor (CLAUDE.md profiles, proposed): S1 `thorough` (opus / sonnet: the law and E2's non-constant uses are where the
S03f review found misses); S2, S3 `standard` (sonnet / haiku). Open question 1.

## Wave: DESIGN / [REF] Ratchet design
### S1 widening (old ∪ new), S3 narrowing (new only)
| Owner | Constant / use | S1 | S3 |
|---|---|---|---|
| `ModuleBoundariesArchUnitTest` | `RefinementPattern` (read by 4 files + guard) | `+ ^Lighthouse\.Backend\.Modules\.Refinement(\.\|$)` | new only |
| `TrackerIntegrationModuleBoundariesArchUnitTest` | `RefinementReportsAccessAndLicensing` | `+ Modules\.Refinement` (needed: its `OtherModules` names only WorkItems, Forecasting, FlowMetrics) | new only |
| `RefinementModuleArchUnitTest` | `RefinementResolution…`, `RefinementCadenceCalendar…`, `NeedBand…` nested-type patterns | `+` new full names | new only |
| `RefinementModuleArchUnitTest` | `ServicesImplementationPattern` as "a service" | `∪ Modules\.Refinement\.(Domain\|Application)` | kept |
| `RefinementModuleArchUnitTest` | `RefinementImplementationNamespace` (`==`, `DoNotResideInNamespace`, exact-namespace assertion) | replaced by the membership's layer namespaces | kept |
| `RefinementModuleArchUnitTest` | `BackgroundUpdatesPattern` | unchanged (E6) | unchanged |

### New rules (S3), baselines from the IL
| # | Rule | Expected baseline (text; IL decides) |
|---|---|---|
| R-R1 | Each listed Refinement type lives in its layer; none left under `Services` | membership map |
| R-R2 | `Domain` → kernel and BCL only | empty |
| R-R3 | Outside → `Domain` only from `API`, the composition root and Refinement `Application` | 0 listed: `RefinementVotesController` reaches `SizingRefusal` (lines 154-156), legal by the `API` namespace |
| R-R4 | Outside → `Application` only from `API` and the composition root (the existing only-API rule, by namespace) | empty list |
| R-R5 | `Application` → no EF Core, `Data`, `API`, module `Adapters`, Tracker integration or update code; Work Items, Flow metrics, Forecasting `Application` legal (down) | empty |
| R-R6 | `Application` → Access, Organisation set-up, Platform lane and repositories only along a listed edge | `VoterIdentityResolver` → Access (`IAuthModeResolver` and the `Auth` types, measured); `RefinementCalendar` → blackout service, clock; `SleYardstickResolver` → clock; `RefinementCadenceCalendar` → `WeeklyRecurrence`; `RefinementList` → `IWorkItemRepository`; `RefinementViewQuery`, `SizingLogCommands` → `ISizingLogRepository`; count fixed in S1 |
Lists inline, by full type name, compared both ways, only shrink. `ModuleWording("Refinement", "the API")`.

## Wave: DESIGN / [REF] Component decomposition
| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| `NeedBand`, `SizingRefusal` (if IL kernel-only) | `Services/Implementation/Refinement` | `Modules/Refinement/Domain` | MOVE | S2 |
| 11 other implementations, 3 ports with records | `Services/{Implementation,Interfaces}/Refinement` | `Modules/Refinement/Application` | MOVE | S2 |
| Sizing-log repository + port | `Services/*/Repositories` | — | UNCHANGED (D-4) | — |
| `Models/Refinement/*` | `Models/Refinement` | — | UNCHANGED (D-3) | — |
| Refinement rules, membership, placement | `Tests/Architecture` | same | EXTEND / CREATE_NEW | S1, S3 |

## Wave: DESIGN / [REF] Driving and driven ports
Driving: unchanged — `RefinementController` (tab read), `RefinementVotesController` (vote, comment, take back),
`TeamController` (settings save through `RefinementSettingsValidator`), CLI and MCP through the same endpoints.
Driven: `ISizingLogRepository` → `SizingLogRepository` (unchanged place), `IWorkItemRepository`, the Forecasting /
Flow metrics / Work Items `Application` ports, the clock and blackout services, Access' auth-mode resolver. No new
external call.

## Wave: DESIGN / [REF] Reuse analysis
| Existing | Overlap | Decision |
|---|---|---|
| S03f preservation law (`PlatformLaneRulesSeeTheMoveArchUnitTest`) | proving rules see a move | **EXTEND** (D-5): parameter by map, or lift to one shared law |
| `TrackerIntegrationMembership` / `PromisesMembership` + tests | namespace-agnostic membership | **REUSE** shape for `RefinementMembership` |
| `ModuleRules`, `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | layer and edge rules with lists, falsifiers | **REUSE** for R-R1..R-R6 |
| `…UnitTestPlacementTest` (Flow metrics, Forecasting, Promises, Work Items) | tests mirror layout | **REUSE** shape |
| `RefinementModuleArchUnitTest` | Refinement's own guards | **EXTEND** (constants only) |
| `architecture-rules`, `ef-model-unchanged`, `backend-tests`, `test-selection-show` | behaviour-free proof | **REUSE** |
| `refinement-answers-compare` | — | not created (D-10) |
No CREATE_NEW production type.

## Wave: DESIGN / [REF] Impact closure
| Locator | Kind | Decision | Reason |
|---|---|---|---|
| `Services/{Implementation,Interfaces}/Refinement/*` (16) | production | MIGRATED | D-1, D-2 |
| `Program.cs` lines 1432-1439 + `using`s | composition root | CHANGED (names only) | D-7 |
| `API/Refinement*Controller`, `TeamController`, `DTO/RefinementViewDto`, `DTO/SizingLogDto` | consumers | CHANGED (`using`s) | |
| 11 unit tests; 19 test files naming the namespaces | tests | MIGRATED / CHANGED | D-6 |
| Rule files of E1, E2, E4; Access list (post-S03e) | architecture tests | CHANGED | S1, S2, S3 |
| `GlobalSuppressions.cs`, `.editorconfig` ×2, `.globalconfig`, `.csproj`, Stryker configs | build / analyser | UNCHANGED_COMPATIBLE unless S1's re-grep finds a target | E5 |
| `path-classifier.sh`, `dev-test.*`, `test-time-budget.json` + Python test, `HostUsersStillToMove` | CI / test infra | UNCHANGED_COMPATIBLE | E5 (named classes stay) |
| Repositories, `Models/Refinement`, `Data`, migrations, frontend, E2E | — | UNCHANGED | D-0, D-3, D-4 |
| `SizingLogCommands` log category | runtime log | CHANGED (namespace) | E5; no override or perf helper keys on it |
| `FeatureOrderingSingleSourceArchUnitTest.cs:29`, `CurrentUserProfileServiceTest.cs:4` | tests | CHANGED (`using`s) | E4 |
| `ARCHITECTURE.md` §4, ADR-270 (layout line and table row 69 → "**Packaged** in `Modules/Refinement` by Story 6365 (the leaf form). Refinement above the leaf is target"), catalogue S03g row, slice brief ACs (lines 11, 24, amended per open question 6) | docs | CHANGED in S3 | context-map rule |
| `adr-277` line 114, `adr-214` lines 67 and 96, `docs/product/architecture/brief.md:9178` (old `Services.*.Refinement` locations) | docs | UNCHANGED: left as history (dated decision records of their time); ADR-270 and `ARCHITECTURE.md` §4 are the current authority | |

Agreement analysis: no shared wire / schema / config contract changes (D-0); `not-applicable` for agreement and
contract tests (single deployable, same commit). Architectural tests: applicable (R-R1..R-R6, the law).
Prefactoring: S1 is the prefactoring (rules widened, behaviour and production untouched; existing suite GREEN before
and after S1, then S2 moves).

## Wave: DESIGN / [REF] Constructive chain and residuality
- Chain: compiled assembly (input) → `RefinementMembership` + law (producer) → ArchUnit rules (consumer) → `dotnet test
  … ~Architecture` exit and message (public observation). Falsifier: a planted type in `Services.Implementation.Refinement`
  or a Refinement type reaching Tracker integration fails with WHAT / WHY / HOW. DESIGN `PASS`, runtime
  `DESIGNED_NOT_BUILT`, proof N/A (D-12).
- Stressors (negated assumptions): (1) "rules read constants" — false for E2's non-constant uses → literal sweep and
  rewrite in S1; (2) "only `using`s change in `Program.cs`" — false (E3) → reviewer checks order / lifetimes;
  (3) "S03e/S03f left the counts as measured" — false by construction → re-measure in S1; (4) "the repository move is
  free" — false (E7) → D-4; (5) "a suppression target follows a move" — false in general (S03f E13) → re-grep, none today.
  Walked afterwards with a second set (a reflection reader of a renamed member, E6; a test class name in budget/host
  lists, E5): both survive. Walkthrough, not execution.

## Wave: DESIGN / [REF] Context-map placement
- Contexts touched: **Refinement** (packaged, no behaviour). Edges unchanged: Work Items → Refinement, Forecasting →
  Refinement (manual How Many), Flow metrics → Refinement (SLE yardstick, throughput); Refinement → Access
  (`VoterIdentityResolver`, now written as a listed edge).
- New terms / concepts: none. Domain events: none published or reacted to.
- UI surface: none (nothing a user sees changes).
- ADR-270 / `ARCHITECTURE.md` §4 updates: S3 tasks (ADR-270 table row 69 to "**Packaged** in `Modules/Refinement` by Story 6365"; Refinement layout line with counts; row 8 anchor `Modules/Refinement`,
  `API/Refinement*Controller`; a "Refinement module rules" bullet like the other contexts).

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)
- [ ] S03e and S03f closed; counts re-measured in S1.
- [ ] Behaviour-free: routes, DTOs, headers, rate-limit policy, settings JSON unchanged; frontend and E2E untouched.
- [ ] No migration, `Data` or `Models/Refinement` file changes.
- [ ] Moves in `refactor(refinement): move …` commits apart from edits.
- [ ] `Program.cs`: only names change at lines 1432-1439; order and lifetimes unchanged.
- [ ] Strings and config re-grepped (E5) on the post-S03f `main`.
- [ ] `BackgroundUpdatesPattern` keeps its name (E6).
- [ ] Architecture docs updated in S3 (incl. ADR-270 row 69 and the brief's amended IN/AC); finalize checklist run before the push.

## Wave: DEVOPS
- **Platform lens**: none — no deployment, configuration, persistence, pipeline or runtime change; one push runs the live
  connector suites once because `Program.cs` changes (D-9).
- **Usage data**: **N/A, because** the Story changes no behaviour a user can use; Refinement's existing events
  (`TeamRefinement*`, `TeamSizing*` usage tests) keep firing unchanged, and no new outcome exists to count.
- KPI: KPI 1 of the catalogue (files under `Services`) drops by 16; measured at S3.

## Open questions (default taken)
1. Rigor: S1 `thorough`, S2 and S3 `standard`? **Default: yes.**
2. Sizing-log repository: stay in `Services/*/Repositories` until S05*/S10 (D-4 a)? **Default: stay.**
3. No `refinement-answers-compare` EXAMINE command; X3 uses the unchanged acceptance and wire tests (D-10)? **Default: no new command.**
4. `Domain` holds only the IL-measured kernel-only types (expected `NeedBand`, `SizingRefusal`), not an empty `Domain`
   if the IL says otherwise (D-1)? **Default: yes; if none qualifies, no `Domain` folder and R-R2/R-R3 are not added.**
5. Lift S03f's law to a shared `NamespaceMoveLaw` if it is platform-specific (D-5)? **Default: yes, in S1 as a
   `refactor(architecture)` commit before the Refinement map.**
6. `Models/Refinement` (9 types) stays in `Models` (D-3: EF-mapped or on the wire), and so does `ISizingLogRepository` (D-4). This narrows the brief's IN ("`Models/Refinement/**`", line 11) and AC (line 24). **Default: stays; S3 amends the brief's IN and AC to "no Refinement file remains in `Services/Interfaces` or `Services/Implementation` other than the sizing-log repository pair; `Models/Refinement` stays in the kernel".**

## Architecture summary for the maintainer
**Shape (hexagonal).** Nothing new is built; files move and rules are restated.
```
 Driving (unchanged)                  Application (MOVED)                       Driven (unchanged)
 RefinementController  ─┐   Modules/Refinement/Application                ┌─ ISizingLogRepository → SizingLogRepository
 RefinementVotesCtrl   ─┼──► RefinementViewQuery, SizingLogCommands,  ────┼─  (stays in Services/Repositories)
 TeamController (save) ─┘   RefinementNeedCalculator, StageRuleMatcher…  ├─ IWorkItemRepository
                            ports: IRefinementViewQuery, ISizingLogCmds,  ├─ Forecasting / Flow metrics / Work Items Application
                            IRefinementCalendar                           └─ clock, blackouts (Platform / Org set-up), Access auth mode
                            Modules/Refinement/Domain (MOVED): NeedBand, SizingRefusal
                            Models/Refinement (kernel, UNCHANGED): settings, sizing log entry, wire enums
```
- Driving adapters: the two Refinement controllers and the Team settings save — unchanged. Application services and
  ports: moved to `Modules/Refinement/Application`, ports beside their use cases. Domain: two pure calculations moved to
  `Domain`; the entities stay in `Models`. Driven adapters: the sizing-log repository stays; no new external call.

**Patterns weighed.**
- Rules first, then move (chosen): the rules must see the move, or they pass over an empty set.
- Preservation law over a membership map (chosen) vs one fixture per pattern (rejected: misses forgotten patterns).
- Domain / Application split by measured dependencies (chosen) vs all in Application (rejected: loses kernel-only guarantee).
- Context-owned repository in `Adapters/Persistence` (rejected for now): it would add an inbound edge from demo data and
  S05* reshapes repositories anyway.
- Moving kernel models (rejected): EF snapshots and wire shapes would change.
- Single responsibility / dependency inversion: ports stay with their use cases; Refinement depends down only, and only
  the API depends on it.
- New EXAMINE compare command (rejected): the unchanged port and wire tests already observe every answer.
