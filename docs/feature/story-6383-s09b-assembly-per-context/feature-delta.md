# Feature: story-6383-s09b-assembly-per-context
Story #6383 · S09b Decision: one assembly per context? · Epic #6510 (Modules: package the bounded contexts) ·
catalogue Epic #6264 · pick-up group G5 (deliberately last)

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (line 13 "D5 re-examined only by S09b", G5 line 74, dependency graph line 93, S09b row line 362, AC line 369).
- Brief: [`slices/slice-27-s09b-assembly-per-context-decision.md`](../epic-6264-modular-monolith/slices/slice-27-s09b-assembly-per-context-decision.md)
  (Size S–M; "may conclude no"; preconditions all hard; hypothesis "ArchUnit rules are enough").
- Authority: [ADR-027](../../product/architecture/adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md)
  D5 (line 45) and "Alternatives considered" (line 64), [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)
  (context map, Decision 6 layout, Decision 7 kernel), `ARCHITECTURE.md` §4.
- Predecessor designs read, not edited: [S09a](../story-6382-s09a-context-contracts/feature-delta.md) (`Contracts`
  layer, R-C1/R-C2/R-C3, `Add<X>` module start-up, D-13 payoff note), [S04b](../story-6368-s04b-models-to-kernel/feature-delta.md)
  (kernel = 3 base types; 44 entities, ~86 wire types and 36 held-by types stay in `Models` with measured blockers),
  [#6657](../story-6657-write-back-round/feature-delta.md) (write-back round), [S03h](../story-6366-s03h-package-reports/feature-delta.md) (format).

DESIGN and DEVOPS run 2026-11-24, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is
recorded as such; the open questions at the end are confirmations with the default taken, not blockers. Nothing a
user sees changes. Nothing was built, published or run with `dotnet` or `pnpm`; every count is a **text measurement on
`main` at `952040471`** (`git grep`, `git log`, a scratch `awk` over `using` lines and changed paths), confidence
`noisy` (misses same-namespace and fully qualified references, matches comments). The compiled IL measurement in S1
decides. Values marked **"to re-measure at DoR re-run"** cannot be measured on `952040471` because their subject does
not exist yet (S03g/h/i, #6657, S09a, S04b) or because measuring them needs a build or a publish.

**Start condition.** Chain order: S03g (#6365, being delivered in `.claude/worktrees/wise-sleeping-firefly`), S03h
(#6366), S03i (#6468), #6657, S09a (#6382), S04b (#6368). All are **merged into `main` before S1 starts**, checked per
Story by the three checks of S03h's start condition (ADO `Closed`; the final rules commit found by
`git log main --grep="story <id>" -- Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture` and
`git merge-base --is-ancestor <hash> HEAD` exit 0; the content check). Content checks for this Story: S09a —
`git ls-files` finds `Modules/*/Contracts/**` and `ContextContractsArchUnitTest.cs`; S04b — `KernelCatalogueArchUnitTest.cs`
exists. A failed check stops S1 and is reported with the Story, the check and its output.

## Wave: DESIGN / [REF] Inherited
- ADR-027 D5: "seven logical modules … enforce via ArchUnitNET in a **single assembly**. ACCEPT (reject physical
  assembly split — complicates single-binary publish for enforcement the test-time rules already give)". Line 64: the
  split "buys compile-time enforcement that test-time ArchUnitNET rules already provide". D4: single store, single
  binary, no broker — **unchanged by this Story**.
- Catalogue line 13: D5 stands and is re-examined only by S09b. Brief OUT: doing the split; a "yes" gets a follow-up Story.
- ADR-270: ten packaged contexts after S03i (S09a R-C3's enumerator list); namespace = folder; rules read from the IL
  (`IlDependencies`); inline lists compared both ways, only shrink (ADR-268).
- S09a: `Modules/<X>/Contracts` is the only cross-context surface (R-C1), contracts point inward (R-C2), contexts
  enumerated from the assembly (R-C3); one `Add<X>` per context; D-13 writes the module-path test-selection payoff
  into `ARCHITECTURE.md` §4 and leaves implementing it to the #6302 line.
- S04b: `Models` keeps the 3 kernel base types plus every type with a measured blocker (`Entity`, `Wire`,
  `HeldBy`, event-awaiting-`Contracts`); entity CLR names are baked into both migration snapshots.
- CLAUDE.md: expand-only migrations; `Create-Migration.ps1` only; CI collects no coverage; Stryker per feature, 80 %.

## Wave: DESIGN / [REF] DoR re-run (2026-11-24, at DESIGN)
| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | ADR-027 D5 rejected a split for a reason (single-binary publish) that the csproj contradicts today (E1); the decision must be re-taken on current facts and recorded. |
| 2 Users / value | PASS | Maintainers: a decision with its reasons and a measurable reopen trigger instead of a standing doubt. No user-visible change. |
| 3 Hypothesis | PASS, **holds, with a corrected reason** | "ArchUnit rules are enough, assemblies add cost for little extra enforcement." Holds on text evidence: the CI payoff an assembly split would add over path-based selection is compile-time only (E7, E8), and three assembly cycles block a split anyway (E3–E5). The brief's premise that single-binary publish is a cost is **refuted** (E1). Disproved only if S1's IL measurement finds no cycle and S1's payoff measurement crosses D-4's thresholds. |
| 4 Size | PASS | **S, about 1.5 days**, one nwdes feature, 2 slices. |
| 5 AC from UAT | PASS, one AC made precise | (a) ADR covering every weigh-point of the brief, decision yes/no/partial → ADR-289 (S2), decision **"no, not now"** with a reopen trigger (D-4). (b) "If yes: a follow-up Story outline" → not applicable under "no"; the ADR instead carries the outline of what a later "yes" must keep (single binary, all topologies) so the trigger has a ready next step. |
| 6 Dependencies | PASS | Hard: all S03*, S09a, "no cycles between contexts". **The last one is not true today on text** (E3) and no test measures it (E4): S1 measures it from the IL and pins it. Soft: S04b (what `Models` keeps), S05*/S10 (repositories, `Data`), #6302 line (test selection). |
| 7 Technical notes | PASS | No production code, no schema, no publish, Stryker, classifier or workflow change (D-0). One new architecture test (S1); docs in S2. |

## Wave: DESIGN / [REF] Baseline (`main` at `952040471`, text search, `noisy`)
| Item | Value | Re-measure at S1 |
|---|---|---|
| Projects in `Lighthouse.sln` | 4: `Lighthouse.Backend` (assembly `Lighthouse`), `Lighthouse.Backend.Tests`, `Lighthouse.Migrations.Postgres`, `Lighthouse.Migrations.Sqlite` | no |
| Publish settings (csproj) | `SelfContained=true`, `PublishSingleFile=true`, `UseAppHost=true`, RIDs win/linux/osx-x64 | no |
| Publish commands | `.github/actions/build-backend/action.yml:48-50` win-x64, linux-x64 (`IncludeNativeLibrariesForSelfExtract`), osx-arm64 (single file from the csproj); `Dockerfile:39` publish to `/app/publish`, `ENTRYPOINT ["dotnet", "Lighthouse.dll"]` (line 82) | bundle contents: **to re-measure** (needs a publish) |
| Migration assemblies | Each migrations project `ProjectReference`s `Lighthouse.Backend` and copies its DLL to `Lighthouse.Backend/Migrations/`; the backend references those two DLLs by `HintPath` with `Private=True` — a build-order cycle broken by committed binaries | no |
| `InternalsVisibleTo` | 1 (`Lighthouse.Backend.Tests`) | no |
| Files declaring an `internal`/`file` type in production | 21 | yes (IL count of non-public types per context) |
| `Assembly.GetExecutingAssembly()` in production | 3: `LicenseVerifier.cs:104` (embedded `public_key.pem`), `AssemblyService.cs:10` (version), `Program.cs:1780` (version) | no |
| ArchUnit loader | `LighthouseArchitecture.Production` loads **one** assembly (`typeof(BaseMetricsService).Assembly`) | no |
| Files in `Tests/Architecture` | 153 | yes |
| Context→context edges (text, `using Lighthouse.Backend.Modules.<Y>`) | Access→Licensing; FlowMetrics→TrackerIntegration, WorkItems; Forecasting→FlowMetrics, Licensing, WorkItems; Promises→TrackerIntegration, WorkItems; WorkItems→Access, FlowMetrics, Licensing, TrackerIntegration | **yes, from the IL**, ten contexts |
| Module files naming `Services`/`API`/`Data` | 61 (top target: `Services.Interfaces`, 62 occurrences) | yes, from the IL |
| Remnant files naming `Modules` | `Services` 20, `API` 57, `Platform` 5, `Models` 1, `Data` 0 | yes |
| `LighthouseAppContext` usings | 13 `Models.*` namespaces, `Platform.Encryption`, **`Services.Interfaces`** | yes |
| Entity names in the SQLite snapshot | 118 lines name `Lighthouse.Backend.Models…` (type full names, no assembly name) | no |
| Stryker | `stryker-config.json`: one `project`, one test project, `mutate` globs, `coverage-analysis: perTest` | no |
| CI path classifier | `Scripts/test-selection/path-classifier.sh`: `BACKEND_REGEX` and per-connector regexes on **paths**; `SHARED_REGEX` forces every live suite | no |
| Test files | 1,344; 222 (17 %) under `Tests/Modules/<X>`; 210 name a web host (`WebApplicationFactory` or a host base) | yes |
| Last 300 backend commits (2026-11-12 → 11-24) | 29 touch exactly one context's `Modules` paths only; 1 several contexts only; 263 touch a path outside `Modules` | **yes, over a window after S04b** (this window is the packaging period itself, biased towards moves) |
| Backend build time, incremental and clean | not measured | **to re-measure at DoR re-run** (S1, median of 9) |
| CI backend test step | 9–14 min (catalogue line 541, October) | yes, from the run API |

## Wave: DESIGN / [REF] Evidence
| # | Finding | Consequence |
|---|---|---|
| E1 | `PublishSingleFile=true` bundles every managed assembly the app references into the one file; the app **already** ships three assemblies (`Lighthouse`, both migration DLLs via `HintPath`, `Private=True`) in that one binary on win/linux/osx. Docker runs `dotnet Lighthouse.dll` from a folder publish, where more DLLs beside it change nothing; Helm uses the image. | ADR-027 D5's stated reason does not hold: a project-reference bundle is still one binary in every topology. Bundle contents are confirmed by S1's publish measurement (to re-measure). The decision must rest on other facts. |
| E2 | `Assembly.GetExecutingAssembly()` is used for the licence public key (embedded resource of Licensing) and the version (2 places). | A split must move the embedded resource with Licensing and read the version from the entry assembly. Cost, not blocker; recorded for a later "yes". |
| E3 | Text shows a **context cycle**: FlowMetrics → WorkItems (`BlockedCountSnapshotRecordingHandler`, `DemoBlockedHistoryBackfillHandler`) and WorkItems → FlowMetrics (`WorkItemService`, the `PercentileCalculator` edge already listed in Flow metrics' R3). | Assemblies cannot have cycles: the brief's hard precondition fails today. S09a's `Contracts` moves the target, not the direction; the cycle survives S09a unless an event replaces one direction. |
| E4 | No architecture test measures acyclicity between contexts (`git grep -i cycle` in `Tests/Architecture` finds only `DependencyCycleDetector`, a domain class). | The precondition has no producer (GDP-8 authoring corollary). S1 adds the measurement as a rule. |
| E5 | 61 module files name the unpackaged remnant (`Services.Interfaces` most), while 20 `Services` and 57 `API` files name modules; `LighthouseAppContext` names `Services.Interfaces`. | A per-context assembly would need the remnant (`Services`, `Data`, `API`) split first or it forms an assembly cycle with every module. Owners: S05*/S10, S12c/S13. Even a **partial** split (persistence assembly, which would also remove the migration `HintPath` cycle) is blocked by `Data → Services.Interfaces`. |
| E6 | S04b leaves 44 EF entities, ~86 wire types and 36 held-by types in `Models`. Snapshots store type full names, not assembly names. | A split would put `Models` in one shared "kernel" assembly referenced by all — a fat shared assembly, not a kernel. Snapshots would survive a move of assembly (not of namespace): migrations are not the blocker, `Models` size is. |
| E7 | Test selection is already path-based (`path-classifier.sh`); S09a D-13 states module-path selection becomes sound once R-C1's residue is empty; R-C1 reads the IL, so it sees the same references a compiler would (plus generic arguments and `GetRequiredService<T>`). | Sound CI test selection does not need assemblies: it needs R-C1 empty and a classifier change (#6302 line). Assemblies add only what the IL rule cannot see — and neither sees reflection by string or `dynamic`. |
| E8 | 83 % of test files are outside `Tests/Modules`; 210 boot a web host spanning all contexts; measured CI slowness comes from test bodies (#6302 note, catalogue line 541). | Per-context test assemblies would hold ~17 % of test files; the host tests go to the one integration assembly and run on every change. The skippable share is small and is the same share path selection can skip (E7). |
| E9 | 29 of 300 recent backend commits touch one context only (10 %; biased window). | The payoff is below any threshold worth a restructure today; re-measured over a post-S04b window by D-4's trigger. |
| E10 | Stryker.NET's `perTest` coverage already runs only covering tests per mutant; `mutate` globs already scope files. CI collects no coverage, Sonar's gate has no coverage condition. | "Stryker scoped per context" adds little; "patchy Sonar coverage" is **not a cost here** (nothing to patch). |
| E11 | Compiler-enforced `internal` needs the module types to become `internal` with only `Contracts` public, registered through `Add<X>` (S09a D-9 makes that possible) and `InternalsVisibleTo` per context to its test assembly. Today 21 files declare a non-public type. | The one benefit ArchUnit cannot match is real but costs a visibility sweep of every module; R-C1 already fails the build's test step on the same edge. |
| E12 | Tools probed for formal verification: `quint --version` → `0.33.1`; `java -version` → OpenJDK 27.0.1.1; `agda` is blocked by the shell allow-list (not needed, see D-8). | See D-8. |
| E13 | Jev: `"jev": "unauthorized"` in the configuration. | No question sent; `typesafe: refused`. |

## Wave: DESIGN / [REF] Decisions
Paradigm: **object_oriented** (CLAUDE.md, settled); S1 adds one test class on the existing `ModuleRules` /
`IlDependencies` helpers; no production code.

### D-0 No production, schema, publish or pipeline change — RECOMMENDED
No `.csproj`, `.sln`, Dockerfile, workflow, `stryker-config.json`, classifier, entity, migration, route or usage-data
change. S1 adds one architecture test (and an inline list); S2 writes docs.

### D-1 The decision — RECOMMENDED: **no, not now**; keep one assembly, correct D5's reason, record a measurable reopen trigger
| Option | Verdict |
|---|---|
| **a (recommended) No, not now** | Keep one production assembly; ArchUnit/IL rules (S09a R-C1–R-C3, per-module rules) stay the enforcement. Reason replaced: not the single binary (E1) but (1) three assembly cycles block it (E3 context cycle, E5 modules ↔ remnant, E5 `Data → Services`), (2) `Models` would be a fat shared assembly (E6), (3) the payoff over path-based selection is compile-time only and small (E7–E9). |
| b Yes, per context | Rejected: blocked by E3/E5 today; the follow-up Story would first have to do S05*/S10/S12c/S13's work. |
| c Partial: one `Lighthouse.Persistence` assembly (DbContext + entities) so the migration projects reference it and the `HintPath` cycle disappears | Rejected now: `LighthouseAppContext → Services.Interfaces` (E5) makes it a cycle too; recorded in the ADR as the **first candidate** once S10 lands, because its payoff (no committed migration DLLs, ordinary build order) is independent of per-context enforcement. |
| d Partial: one assembly per **leaf** context (no inbound module edge) | Rejected: Licensing is reached by three contexts, Access by one; leaves today are only Promises and Forecasting-side code, which still name `Services.Interfaces` (E5). Two assemblies for two small contexts buy nothing measurable. |

### D-2 Pin the precondition, not the outcome — RECOMMENDED: `ContextDependencyGraphArchUnitTest` (S1)
"No cycles between contexts" is the hard precondition of any future split and a property ADR-270's map already
implies (customer/supplier directions); nothing measures it (E4). S1 adds one rule over `LighthouseArchitecture.Production`:
- **Graph:** nodes = the context set read by S09a's R-C3 enumerator (reused, not rebuilt — `gate:predicate-needs-its-own-enumerator`);
  edge A→B iff some type in `Modules.A.*` has an IL dependency on a type in `Modules.B.*` (`IlDependencies`, so
  generic arguments, attributes and `GetRequiredService<T>` count).
- **Law:** every strongly connected component has size 1, except along `ContextCycleEdges` — an inline list of
  `A -> B` context edges, compared both ways, only shrinks, each with an owner comment. Expected baseline from text:
  `FlowMetrics -> WorkItems` or `WorkItems -> FlowMetrics` (one direction listed, owner chosen at S1 from which edge
  an event can replace; to re-measure at DoR re-run). Removing every listed edge must leave the graph acyclic; a listed
  edge that is no longer in the IL fails as stale.
- **Refusal (WHAT/WHY/HOW):** names the cycle as a path `A -> B -> … -> A` with one type pair per edge; WHY "contexts in a
  cycle cannot be released or split apart, and a change in either can break the other"; HOW "react to an event of the
  other context, or move the shared concept into its owner's `Contracts`, or (last) list the edge with an owner".
- Rejected: a test that the solution has exactly N production assemblies (pins a designation, adds ceremony, and would
  have to be deleted by the one Story it exists to inform).

### D-3 Measurements S1 records (receipts in the slice's commit body, numbers in ADR-289)
| M | What | Command (literal at S1) | Feeds |
|---|---|---|---|
| M1 | Context graph and its SCCs from the IL | the S1 rule's own output | D-1 (1), D-4 P1 |
| M2 | Module ↔ remnant edges (IL) | S09a/S04b `IlDependencies` helpers in a throwaway probe, not committed | D-1 (1), D-4 P2 |
| M3 | Single-file bundle contents | `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o <scratch>` then list the output folder and the bundle manifest | E1 confirmed or refuted |
| M4 | Incremental and clean backend build, median of 9 | `dotnet build --no-incremental` ×9; one-line edit in a module file then `dotnet build` ×9 | D-4 T2 baseline |
| M5 | One-context-only share of backend commits, 6 months | the `awk` over `git log --name-only` used for E9, on the window ending at S1 | D-4 T1 baseline |
| M6 | CI backend test-step median, last 30 `main` runs | `gh run list` + `gh run view --json jobs` | D-4 T1 |
A measurement that disagrees with this design is recorded, not argued with; if M1 shows no cycle **and** M5/M6 cross
D-4's T1 thresholds, the hypothesis is disproved and S2 writes "partial/yes" with the follow-up outline (open question 1).

### D-4 Reopen trigger — RECOMMENDED: preconditions AND one payoff trigger, each with its measuring command
Reopen ADR-289 when **all** preconditions hold **and** at least one trigger fires:
- **P1** `ContextCycleEdges` is empty (S1 rule green with an empty list).
- **P2** No IL edge from `Modules.*` to `Lighthouse.Backend.Services|API|Data` and none from `Data` to `Services` (the
  remnant drained: S05*/S10).
- **P3** S09a's `CrossContextEdgesOutsideContracts` is empty.
- **T1 (test time)** Module-path test selection is live (#6302 line) **and** over 6 months ≥ 25 % of backend commits
  touch one context only **and** a selection miss happened that the IL rules could not see (reflection by string,
  `dynamic`), i.e. the compiler would have caught what R-C1 did not.
- **T2 (build time)** The incremental backend build after a one-line module edit has a median (of 9) ≥ 60 s on the
  maintainer's machine or the CI runner (M4 gives today's value).
- **T3 (escape)** Two or more cross-context references that broke a rule's intent reached `main` unnoticed by the IL
  rules within 6 months (each recorded as a Bug).
Owner: the maintainer, checked when S10 closes and at each Epic 6264 milestone review. When P1–P3 hold and no trigger
fires, the ADR stays "no" with a dated note.

### D-5 Docs — RECOMMENDED: a new ADR-289 that supersedes ADR-027 D5's reason; ADR-027 gets a pointer
ADR-289 "Module boundaries stay in one assembly (not now)": context, every brief weigh-point answered (table below),
decision D-1, trigger D-4, "what a later yes must keep" (one binary in Standalone/Docker/Helm, all four topologies,
expand-only migrations through `Create-Migration.ps1`, one `DbContext`, ArchUnit loader over all module assemblies,
embedded licence key and version read from the entry assembly). ADR-027 D5 row gets "reason superseded by ADR-289"
(the decision stays). `ARCHITECTURE.md` §4 gains one line under S09a's "Context contracts" bullet. Catalogue S09b row
ticked. ADR number: next free at S2 (288 at `952040471`).
Rejected: editing D5's reason in place (rewrites a dated decision's chronology).

| Brief weigh-point | Answer in ADR-289 |
|---|---|
| For: compiler-enforced `internal` | Real, only benefit the IL rules cannot match fully (E11); cost: visibility sweep + `InternalsVisibleTo` per context. |
| For: sound CI test selection | Achievable by path + R-C1 without assemblies (E7); assemblies add only the reflection/`dynamic` blind spot (T1). |
| For: Stryker scoped per context | Little gain: `perTest` + `mutate` globs already scope (E10). |
| For: module test assemblies + one integration assembly | 17 % of test files would move; 83 % stay in the integration assembly (E8). |
| Cost: one `DbContext` referencing all contexts | Entities stay in `Models` (S04b), so the `DbContext` references `Models`, not the contexts; blocked instead by `Data → Services.Interfaces` (E5). |
| Cost: migration `HintPath` setup | Snapshots store full type names, not assembly names (E6): a split does not rewrite them; a persistence assembly would remove the `HintPath` cycle (option c). |
| Cost: patchy Sonar coverage | Not applicable: CI collects no coverage, the gate has no coverage condition (E10). |
| Cost: one binary in every topology? | Yes: single-file bundles project references, as the migration DLLs show (E1, M3). Not a blocker. |
| #6302 note | Agreed: slowness is test bodies; only skipping helps, and path selection skips the same set (E7–E9). |

### D-6 Tests — RECOMMENDED: S1's rule is the only test; "improve the tests you touch" applies to none
S1 creates one file in `Tests/Architecture`; it touches no existing test class. Speed: one IL graph pass over the
already-loaded `ArchitectureModel` (shared, built once per run); per-test budget of the test-writing skill applies
(it wins over nWave's nine-run timing rule).

### D-7 Mutation bar — RECOMMENDED: N/A, with reason
No production file changes (D-0); Stryker.NET mutates production code only. The project's per-feature mutation run
has nothing to mutate; recorded as "N/A, no production change" in the finalize checklist.

### D-8 Formal verification — RECOMMENDED: no Quint / TLA+ / Agda model; the law is checked exhaustively on the IL
Claims: C1 "the context graph minus `ContextCycleEdges` is acyclic" (finite graph, ≤ 10 nodes, computed from the
compiled assembly each run — exhaustive, not sampled); C2 the decision's preconditions/triggers (measurements, not
laws). No transition system, no concurrency, no temporal property, no unbounded input: a Quint model would only
restate a fixed graph and could not observe the IL. Tools were probed (E12: Quint 0.33.1 and Java available) and
deliberately **not selected**; no waiver is needed because no formal claim is made. Falsifier for C1 (S1 RED): with
the inline list empty the rule fails on today's cycle (E3), naming it; that run is the RED the reviewer must see.
PBT: N/A (closed finite domain).

## Wave: DESIGN / [REF] Slices
Delivered 2026-11-28 as ADR-289 only; S1 (rule and measurements) dropped by the maintainer's decision of 2026-11-26.

One nwdes feature, `story-6383-s09b-assembly-decision`, 2 slices, committed per slice, pushed together after S2.

| Slice | Delivers | Oracle / proof | Rigor (proposed) |
|---|---|---|---|
| **S1** Context graph rule + measurements | `ContextDependencyGraphArchUnitTest` (D-2) with its inline `ContextCycleEdges` baseline measured from the IL; M1–M6 receipts (D-3) | RED: the rule with an empty list fails naming the cycle path; GREEN: with the measured list; stale-entry case: a listed edge not in the IL fails. Runs in the fast loop (`Test-Fast.ps1`, no host) | `thorough` (IL graph rule, owner choice for the cycle edge) |
| **S2** ADR-289 + doc pointers | ADR-289 (D-1, D-4, D-5 table, "what a later yes must keep"), ADR-027 D5 pointer, `ARCHITECTURE.md` §4 line, catalogue row | Prose-only slice: no oracle; verified by review and EXAMINE (read the ADR cold, answer "why not now, and when"). | `standard` |

Driving port: `dotnet test --filter FullyQualifiedName~Tests.Architecture.ContextDependencyGraphArchUnitTest`
(in-process ArchUnit model, `LighthouseArchitecture.Production`); nearest executable argv is that command.
Constructive chain: compiled assembly → `ArchLoader` → R-C3 enumerator (context set) → `IlDependencies` (edges) →
SCC check → NUnit result + refusal text. All parts exist after S09a except the new test (DESIGNED_NOT_BUILT).

## Wave: DESIGN / [REF] Reuse, impact closure, residuality
- **Reuse:** `LighthouseArchitecture.Production` (REUSE), `IlDependencies` and `ModuleRules` (REUSE), S09a R-C3
  context enumerator (REUSE; if S09a ships it as a private helper, S1 EXTENDs it to `internal static` rather than
  copying), `ModuleWording` refusal format (REUSE). CREATE_NEW: only the test class; no existing rule owns
  context-level acyclicity (E4).
- **Impact closure:** `Tests/Architecture/ContextDependencyGraphArchUnitTest.cs` CHANGED (new); S09a enumerator
  UNCHANGED_COMPATIBLE or visibility-only; ADR-289 new; ADR-027, `ARCHITECTURE.md`, catalogue CHANGED (prose);
  csproj/sln/classifier/workflows/Stryker UNCHANGED. Agreement analysis: no shared contract touched — not applicable.
  Contract tests: not applicable (one deployable, no wire). Architectural test: applicable, D-2.
- **Residuality** (assumption negated → outcome): *S09a lands without the enumerator* → S1 builds the context set
  from namespace roots with the same both-ways list (one owner after S09a); *a predecessor removes the cycle* → the
  list starts empty, stronger outcome; *M3 shows the bundle does not include project references* → E1 is refuted, D5's
  original reason returns as a fourth "no" reason, the decision is unchanged; *M1 finds more cycles* → listed with
  owners, P1 further away, decision unchanged; *M5/M6 cross T1* → open question 1. Second stressor set (walkthrough,
  not executed): a new context added later is caught by R-C3 and enters the graph automatically; a cycle through the
  remnant (`Modules.A → Services → Modules.B`) is **not** a context edge and stays invisible to this rule — residue,
  owner S05*/S10, recorded in ADR-289 under P2.

## Wave: DESIGN / [REF] Context-map placement
- Contexts touched: none in production. The rule observes all ten contexts' dependency graph.
- New terms: "context dependency graph", "context cycle" (ADR-270 glossary, Platform/architecture section, S2).
- Domain events: none published or handled. The ADR names "react to the other context's event" as the cure for a
  listed cycle edge.
- UI surface: none.
- ADR-270 changes: the "Ratchet candidates" list gains `ContextCycleEdges` with owners (S2); map unchanged unless M1
  finds an edge the map does not show (then the map is corrected in S2).

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)
- [ ] Start condition checked for all six predecessors (S1).
- [ ] M1–M6 measured and recorded; differences from this design noted, not argued (S1).
- [ ] RED of the empty-list rule seen and recorded before the list is filled (S1).
- [ ] ADR-289 answers every brief weigh-point and the #6302 note; trigger D-4 with commands (S2).
- [ ] ADR-027 D5 pointer, `ARCHITECTURE.md` §4 line, ADR-270 ratchet entry and glossary terms, catalogue S09b row (S2).
- [ ] Finalize checklist: docs prose N/A (no user docs), screenshots N/A, demo data N/A, website N/A, clients N/A,
      context map ✔ (above), usage data N/A (below), mutation N/A (D-7) — each answered explicitly at finalize.
- [ ] `dotnet build` and the filtered `dotnet test` green before the push.

## Wave: DEVOPS
- **Platform lens: relevant, and honestly so.** The question *is* packaging: single-file publish on win/linux/osx,
  the Docker folder publish (`dotnet Lighthouse.dll`), Helm via the image, the Standalone packages built from the
  single-file output. The decision changes none of them (D-0). The platform evidence the decision rests on is E1,
  confirmed by M3 in S1 (one scratch publish, not committed, outside the repository). Platform risk of this Story:
  none. Platform risk of a later "yes", recorded in ADR-289: the bundle must still contain every context assembly
  (`IncludeNativeLibrariesForSelfExtract` stays on linux-x64), the osx-arm64 publish relies on the csproj's
  `PublishSingleFile` (no CLI flag), the Docker image gains DLLs beside `Lighthouse.dll`, codesign/SBOM steps see
  only the bundle.
- **CI:** no workflow or classifier change. The push touches only `Tests/Architecture` and docs: the backend suite
  runs (`BACKEND_REGEX`), no live connector suite (no `SHARED_REGEX` path).
- **Usage-data event: N/A**, because nothing a user can do changes; an architecture decision and a test rule have no
  in-product use to count.

## Open questions (default taken)
1. **Answer if S1's measurements disprove the hypothesis** (no cycle and T1 thresholds crossed). Default: S2 still
   writes "no, not now" if P2 fails (remnant edges), because a split is impossible while the remnant forms a cycle;
   only with P1–P3 all true does S2 write "partial (option c first)" plus a follow-up Story outline.
2. **Add the acyclicity rule even though the answer is "no"?** Default: yes. It measures the brief's own hard
   precondition, guards ADR-270's map directions today, and is the only producer for trigger P1.
3. **Trigger thresholds** (25 % one-context commits, 60 s incremental build, 2 escapes in 6 months). Default: as
   written; the maintainer may change the numbers without changing the decision.
4. **New ADR-289 vs amending ADR-027 D5 in place.** Default: new ADR, ADR-027 keeps its dated decision and gains a
   pointer.
5. **Which direction of the FlowMetrics ↔ WorkItems cycle is listed, and its owner.** Default: the edge an existing
   or new event can replace (likely WorkItems → FlowMetrics `PercentileCalculator`, owner the Flow metrics ratchet);
   chosen at S1 from the IL.
6. **Rigor.** Default: S1 `thorough`, S2 `standard` (confirm at the start of DELIVER, per CLAUDE.md).
No question changes what a user sees or is told.

## Architecture summary for the maintainer
**Shape in hexagonal terms.** Nothing in the hexagon changes. Driving adapters (API controllers, background
services), application services and ports (`Modules/<X>/Application`, `Contracts` after S09a), domain (`Modules/<X>/Domain`,
the `Models` kernel and its blocked residue after S04b) and driven adapters (tracker connectors, EF persistence, two
migration assemblies) stay in **one production assembly** plus the two migration DLLs. New: one architecture test that
reads the compiled assembly and checks that contexts do not depend on each other in a circle. No new external call.

```
            one binary (single-file bundle)            test-time enforcement (IL)
  +------------------------------------------+    +-------------------------------+
  | Lighthouse.dll                           |    | Tests/Architecture            |
  |  API ─┐                                  |    |  per-module rules  (direction)|
  |       v                                  |<---|  R-C1..R-C3        (surface)  |
  |  Modules/<X>/Contracts <── other modules |    |  NEW context graph (no cycles)|
  |  Modules/<X>/{Application,Domain,Adapters}    +-------------------------------+
  |  Services / Data / Platform (remnant)    |
  |  Models (kernel + 44 entities, wire)     |
  +------------------------------------------+
  | Lighthouse.Migrations.{Sqlite,Postgres}  |  <- HintPath DLLs, already bundled
  +------------------------------------------+
  Blocks a split today:  FlowMetrics <-> WorkItems,  Modules <-> Services/API,  Data -> Services
```

**Patterns weighed.**
- *Physical modules (assembly per context)* — rejected now: blocked by three cycles and a fat shared `Models`; adds
  only compile-time `internal` over the IL rules.
- *Logical modules enforced by fitness functions (ArchUnit on IL)* — chosen: same edges a compiler sees, plus generic
  arguments and service lookups; cheap to change while the remnant drains.
- *Acyclic Dependencies Principle* — chosen as a rule (S1): the precondition for any later split and a guard on the
  context map.
- *Persistence assembly (shared kernel for EF)* — deferred: first candidate after S10, because it removes the
  committed-migration-DLL cycle independently of module enforcement.
- *Path-based test selection* — kept as the route to the CI payoff (#6302 line); assemblies are not needed for it.
- *Superseding ADR rather than editing history* — chosen: ADR-027 D5's decision stands, its reason is replaced by
  ADR-289.
