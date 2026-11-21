# Feature: story-6366-s03h-package-reports
Story #6366 · S03h Package Reports · Epic #6510 (Modules: package the bounded contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (S03h row line 177, D-6264-6, D-6264-16, the "stale briefs" note naming `slice-10`).
- Brief: [`slices/slice-10-s03h-package-reports.md`](../epic-6264-modular-monolith/slices/slice-10-s03h-package-reports.md) (Size S).
- Authority: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)
  (Reports row, line 70; settled folder names; R3 list line 225), `ARCHITECTURE.md` §4 row 9.
- Precedents copied, same shape: [S03g](../story-6365-s03g-package-refinement/feature-delta.md) (closest: a small
  context; membership, law, sweep, three slices) and [S03f](../story-6364-s03f-platform-composition-root/feature-delta.md)
  (preservation law, D-4 repositories stay, E13 suppression lesson).

DESIGN and DEVOPS run 2026-11-21, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is
recorded as such; the open questions at the end are confirmations with the default taken, not blockers. Nothing a
user sees changes. Nothing was built or run with `dotnet` or `pnpm`; every count is a **text measurement on `main` at
`895e0e5ec`** (`git ls-files`, `git grep`), confidence `noisy` (misses same-namespace and fully qualified references,
matches comments). The compiled IL measurement in slice S1 decides.

**Start condition.** S03e (#6363), S03f (#6364) and S03g (#6365) are delivered and merged into `main` before S1
starts. At `895e0e5ec` all three are designed only (`e6ad93130`, `c0144301c`, `fcb1ff878`, `895e0e5ec` are docs
commits). All three edit files this Story edits: `ModuleBoundariesArchUnitTest` (S03g widens and narrows
`RefinementPattern` beside `ReportsPattern`), `TrackerIntegrationModuleBoundariesArchUnitTest`'s joint constant
`RefinementReportsAccessAndLicensing` (S03e for Access/Licensing, S03g for Refinement, this Story for Reports), and
S03f moves `ILighthouseClock` (named by 6 Reports files) into `Platform/*` and may write a lane rule (R-PL4) naming
Reports. S03e may move `ILicenseService` (named by 2 Reports files). **Every count below is re-measured at S1**; a
difference is recorded, not argued with.

## Wave: DESIGN / [REF] Inherited
- ADR-270: Reports is a supporting context, conformist downstream of Flow metrics, the outbound channel to people outside
  Lighthouse; folder `Modules/Reports`; namespace = folder (`Lighthouse.Backend.Modules.<Context>.<Layer>[.<Sub>]`);
  `Domain`, `Application` (ports co-located), `Application/Policies`, `Adapters/Background`, `Adapters/Persistence`;
  tests mirror under `Lighthouse.Backend.Tests/Modules/Reports`; rules read from the IL (`IlDependencies`); lists
  inline, compared both ways, only shrink.
- `ARCHITECTURE.md` §4 row 9: Reports "depends **down only** — on Metrics, RBAC/licence and Platform — and nothing but
  `API` and the composition root depend on it"; "one `ReportRepository` implements both [ports] and only the
  composition root names it". Anchor today: `Models.Reports`, `Services.*.Reports`, the three Reports controllers.
- S03f D-4: every repository stays in `Services/*/Repositories`, named with owner; the list names "S03h reports".
- S03a D-3 (via S03f, S03g): a `Models` type moves only if it is not an EF entity, not on the wire, not reached by a
  kernel type and not used outside the context.
- D-6264-16: Platform is composition-root infrastructure, not a context. The brief's "Platform" and "RBAC/licence"
  wording is stale (catalogue line 970) and is restated in S3 as "Flow metrics, Access, Licensing, the kernel and the
  platform infrastructure".

## Wave: DESIGN / [REF] DoR re-run (2026-11-21)
| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Reports lives in `Services/Implementation/Reports` (12 + 8 in `Metrics`) and `Services/Interfaces/Reports` (11); its rules key on `ModuleBoundariesArchUnitTest.ReportsPattern` `^…Services\.(Implementation\|Interfaces)\.Reports(…)` (E1). |
| 2 Users / value | PASS | Maintainers: the context sits in its ADR-270 folder; "down only / only API and root" hold by `Modules.Reports` namespace. No user-visible change. |
| 3 Hypothesis | PASS, sharpened | "Packages with namespace changes only." Holds as "namespace and `using` changes; `using`s added where the Domain/Application split removes a same-namespace resolution". Unlike Refinement, `Program.cs` registers Reports by **short** name (lines 1327-1346) behind 3 `using` lines (29-31): only those 3 lines change (E3). No moved file resolves a loose `Services.Implementation` / `Services.Interfaces` type implicitly: all 6 `ILighthouseClock` users carry an explicit `using` (E9). Disproved only if a production edit beyond `namespace` / `using` lines is needed. |
| 4 Size | PASS | **S, about 2.5 days**, one nwdes feature, 3 slices. |
| 5 AC from UAT | PASS, two ACs made precise | (a) "No Reports file remains in `Services/Interfaces` or `Services/Implementation`": holds for the 31 files of D-2. `ReportRepository` is in `Services/Implementation/Repositories` and **stays** (D-4); its ports `IReportReader` / `IReportWriter` already sit in `Services/Interfaces/Reports` and move. (b) "Down-only rule green by namespace; only the composition root names `ReportRepository` preserved" (S3; the second is `typeof`-keyed and unchanged). (c) `ARCHITECTURE.md` §4 row 9 updated (S3). `Models/Reports/**` stays (D-3): the brief's IN narrows — open question 6. |
| 6 Dependencies | PASS | Hard: S01, S02 gate. **Added: S03e, S03f and S03g merged first** (shared rule files). Soft: S05*/S10 own repositories and `Data`; S09a owns module start-up; scheduled delivery and new report types are target (ADR-270 row). |
| 7 Technical notes | PASS | No `GlobalSuppressions.cs`, `.editorconfig` ×2, `.globalconfig`, `.csproj`, Stryker config, CI path classifier or `test-time-budget.json` entry names a Reports type or path today (E5). The `Reports` table, `TemplatePayloadJson` and `PayloadSchemaVersion` are untouched (brief note; D-0). |

## Wave: DESIGN / [REF] Baseline (`main` at `895e0e5ec`, text search, `noisy`)
| Group | Files | Moves? |
|---|---|---|
| `Services/Implementation/Reports/*` | 12: `PortfolioReportMetricSeries`, `ReportBulkCommands`, `ReportBulkRemover`, `ReportCapPolicy`, `ReportCommands`, `ReportMetricSeriesByOwner`, `ReportOwnerLookup`, `ReportQueries`, `ReportSeriesReading` (internal static), `ReportWindowPolicy`, `TeamReportMetricSeries`, `ThenAndNowTemplate` | yes (D-2) |
| `Services/Implementation/Reports/Metrics/*` | 8: `ArrivalsReportMetric`, `CycleTimeReportMetric`, `DailySeriesSummary`, `ReportMetricCatalog`, `ThroughputReportMetric`, `WindowedReportMetric`, `WipReportMetric`, `WorkItemAgeReportMetric` | yes → `Application/Metrics` (D-1) |
| `Services/Interfaces/Reports/*` | 11 ports: `IReportBulkCommands`, `IReportBulkRemover`, `IReportCommands`, `IReportMetric`, `IReportMetricSeries`, `IReportOwnerLookup`, `IReportOwnerWriteAccess`, `IReportQueries`, `IReportReader`, `IReportTemplate`, `IReportWriter` | yes, co-located in `Application` (D-1) |
| `Services/Implementation/Repositories/ReportRepository` | 1 | **no** (D-4) |
| `Models/Reports/*` | 7: `Report` (EF entity), `ReportOwner`, `ReportReadModels` (returned by `IReportQueries`, on the API wire), `ReportRefusalCodes`, `ReportBulkOutcomes`, `ReportOwnerGoneException`, `ThenAndNowPayload` (the stored JSON payload) | **no** (D-3) |
| API: `TeamReportsController`, `PortfolioReportsController`, `ReportsController`, `ReportsControllerBase`, `ReportOwnerWriteAccess` (implements `IReportOwnerWriteAccess`), 4 DTOs | 9 | no (driving adapter; `ReportOwnerWriteAccess` is pinned to `API` by `The_owner_write_access_is_built_in_the_API`) |
| Unit tests `Tests/Services/Implementation/Reports/*` | 18 | yes → `Tests/Modules/Reports/{Domain, Application, Application/Metrics}` |
| `Tests/Services/Implementation/Repositories/ReportRepositoryTest` | 1 | no (follows D-4) |
| Acceptance / port / host tests `Tests/API/Integration/Reports/**` (21), `Tests/Acceptance/BaselineReportCreation/*` (2), controller tests (3), `Data/ReportsTableConstraintsTest`, `Models/Reports/*` (2), usage-data host tests (3), Postgres container test (1) | 33 | no (drive the API / service port; `using` lines only where they name the moved namespaces) |

Inbound (production, `using`): `API/{TeamReportsController, PortfolioReportsController, ReportsController,
ReportsControllerBase, ReportOwnerWriteAccess}`, `API/DTO/Reports/NewReportRequest` (`ThenAndNowTemplate.TemplateKey`),
`Program.cs` (3 `using` lines), `Services/Implementation/Repositories/ReportRepository` (implements the read and write
ports). Nothing else: the only-API-and-root rule holds today (its exclusions are `Models.Reports` and `ReportRepository`
by `typeof`). Test files naming `Services.*.Reports` outside the moving 18: **13** —
`API/Integration/Reports/{ReportsAtThePortTest, BulkReports/BulkReportsAcceptanceTest, BulkReports/ChangesDuringABulkRun}`,
`API/{ReportsControllerTest, TeamReportsControllerTest}`, `Acceptance/BaselineReportCreation/{CreateReportWithEqualNowScenarios,
EqualLengthReportsScenarios}`, `Architecture/{FlowMetricsInboundDomainAndAdaptersArchUnitTest, PortFixtureLifetimesTest,
ReportsModuleArchUnitTest, RepositoryLeakRatchetArchUnitTest, TrackerIntegrationModuleBoundariesArchUnitTest}`,
`Contracts/PortFixtures/PortFixtureLifetimes`.

Outbound (from the 31 files, `using` lines): kernel `Models.Reports` (30), `Models`, `Models.Metrics`; Flow metrics
`Application` (2: `TeamReportMetricSeries`, `PortfolioReportMetricSeries` → `ITeamMetricsService`,
`IPortfolioMetricsService`) and `Domain` (1: `DailySeriesSummary`); loose `Services.Interfaces` (6 files, all
`ILighthouseClock`); `Services.Interfaces.Repositories` (3: `ReportOwnerLookup`, both series adapters → `IRepository<Team>`
/ `IRepository<Portfolio>`, Organisation set-up data); `Services.Interfaces.Licensing` (2: `ReportCommands`,
`ReportQueries` → `ILicenseService`); **EF Core** (2: `ReportBulkCommands` catches `DbUpdateException`,
`ReportBulkRemover` catches `DbUpdateConcurrencyException` and `DbUpdateException`); BCL `TimeProvider`,
`System.Text.Json`. No edge to `Data`, Tracker integration, RBAC (`IRbacAdministrationService`, `ClaimsPrincipal`,
`HttpContext`: existing rule) or the update code.

## Wave: DESIGN / [REF] Evidence
| # | Finding | Consequence |
|---|---|---|
| E1 | Rule constants keyed on the old namespaces: `ModuleBoundariesArchUnitTest.ReportsPattern` (line 26) — read by `ReportsModuleArchUnitTest` (5 rules + `ReportsModuleTypes()`), `FlowMetricsInboundApplicationArchUnitTest:51`, `ForecastingModuleBoundariesArchUnitTest:134`, `WorkItemsModuleBoundariesArchUnitTest:166` (all three inside **interpolated, non-const** pattern strings), `ModuleBoundariesArchUnitTest.ReportsModule` (line 71) and the non-empty guard (line 125). `TrackerIntegrationModuleBoundariesArchUnitTest.RefinementReportsAccessAndLicensing` (line 58) names Reports **inside an alternation**: `\.(Refinement\|Reports\|Auth\|Authorization\|Licensing)(\.\|$)`. | A move without S1 drops Reports from the only-API, Flow metrics inbound, Forecasting and Work Items rules **silently** (they check an empty set; only the line-125 guard fails). S1 widens `ReportsPattern` (one owner; the three interpolated readers follow without edits) and the joint constant; S3 narrows. |
| E2 | Lesson "inline fragments a reflection law over const patterns misses", checked: `ReportsModuleArchUnitTest` keys every type rule on `typeof(...)` / `NameAndNestedTypesOf(typeof(...))`; its `==` comparisons (`call.DeclaringType.FullName == typeof(IReportWriter).FullName`, `== typeof(ReportWindow).FullName`, `Is.EqualTo(new[] { typeof(ReportCommands).FullName })`) are type-identity, rename-safe; `Regex.IsMatch(adapter.Namespace, ApiLayerPattern)` and `StartsWith("Lighthouse.Backend")` name no Reports namespace; `Contains(OwnersBaselineMember)` is a member name. **No** `DoNotResideInNamespace`, namespace `==` or `.Contains("…Services.Implementation…")` on Reports anywhere in `Tests/Architecture`. Two real misses for a text sweep: the **alternation** form of E1 (a literal sweep for `Services\.(Implementation\|Interfaces)\.Reports` does not match `\.(Refinement\|Reports\|…)`), and the three **interpolated** readers (not `const`, so a reflection law over `const` fields does not see them). | The law (D-5) evaluates every regex-valued field it can reach **by behaviour** (`IsMatch` on old and new full names), not by text, which catches the alternation. The interpolated readers compose `ReportsPattern` by reference, so widening the owner covers them; S1's sweep lists them by file and line so S3 proves they carry no inline Reports fragment of their own. |
| E3 | `Program.cs` lines 29-31 `using …Services.Implementation.Reports`, `….Reports.Metrics`, `…Services.Interfaces.Reports`; `RegisterReports` (lines 1327-1346) uses short names. | Only 3 `using` lines change in S2; registration order, lifetimes and pairs do not (reviewer diff check). `Program.cs` is in `SHARED_REGEX` of `Scripts/test-selection/path-classifier.sh` (line 29), so S2's push runs the live connector suites once (D-9). |
| E4 | String literals naming moving paths/types: `RepositoryLeakRatchetArchUnitTest.cs:47` (`"Services/Implementation/Reports/ReportOwnerLookup.cs"`); `FlowMetricsInboundDomainAndAdaptersArchUnitTest.cs:47` (`"Lighthouse.Backend.Services.Implementation.Reports.Metrics.DailySeriesSummary"`, an inline list entry of Flow metrics' R3); `TrackerIntegrationModuleBoundariesArchUnitTest.cs:427` (`"Lighthouse.Backend.Services.Interfaces.Reports.IReportQueries"`) and its fixture at line 864 (`global::` reference, compile-checked). Compile-checked `using` / `typeof` users outside the moving set: `PortFixtureLifetimesTest.cs:3-6`, `Contracts/PortFixtures/PortFixtureLifetimes.cs`. | Renamed in S2's edit commit; counts unchanged. A missing path fails loudly; the fixture fails to compile. S1's literal sweep lists every one so none is left. `ACallerOnlyFlowMetricsHas = "Reports"` (Forecasting) and `ModuleWording.FlowMetrics` are **wording**, not namespaces: unchanged. |
| E5 | Lessons from the S03f/S03g reviews, checked for Reports: `GlobalSuppressions.cs`, both `.editorconfig`, any `.globalconfig`: **no** Report target. `.csproj` / `Directory.Build.props`: none. Repo Stryker configs (`stryker.6094.backend.json`, `mutate` list): none. `Scripts/test-selection/{path-classifier.sh, test-path-classifier.sh, dev-test.*}`: no Reports path (backend selected by the generic prefix; `Program.cs` shared, E3). `Scripts/test-timings/test-time-budget.json`: no Report entry; its Python tests name none. `TestHostUseGuardTest.HostUsersStillToMove` lines 173-189, 263, 271-272 and `TestHostPerFixtureGuardTest` lines 55, 73 name `API.Integration.Reports.*`, the Postgres container test and two usage-data classes, which all **stay**. **Log categories**: 5 moving classes take `ILogger<T>` — `ReportBulkCommands`, `ReportBulkRemover`, `ReportCommands`, `ReportQueries`, `ThenAndNowTemplate` — so their categories become `Lighthouse.Backend.Modules.Reports.Application.<T>`; the only Serilog override in `appsettings*.json` is `…Services.Implementation.Auth.DisabledAuthenticationHandler`; `Lighthouse.EndToEndTests/perf/helpers/collect.ts` overrides only `…Forecasting.Adapters.Background.ForecastUpdater`; `ReportLoggingTest` asserts through test doubles, not category strings. | Nothing to rename there; S1's re-grep repeats this on the post-S03g `main` and records the result in the membership data, so a new target is caught. |
| E6 | Reflection by member name: none on Reports rule members (text search for `nameof(ReportsModuleArchUnitTest` / `"ReportsPattern"`: none). `ReportsPattern` is read by symbol. | No name freeze needed; `ReportsPattern` keeps its name anyway (one owner, readers unchanged). |
| E7 | `ReportRepository` is named only by `Program.cs` (and tests); it implements `IReportReader` / `IReportWriter`, which move with the context. It does not derive from `RepositoryBase<T>`. | Moving it would add no inbound edge (unlike S03g's sizing log), but S03f D-4 decided "all stay, owner named" (owner: S03h reports). D-4 follows S03f; the cheap move is an open question. |
| E8 | Static pure types by `using`: `ReportWindowPolicy` (kernel `Models.Reports` only), `ReportCapPolicy` (none), `ReportSeriesReading` (`Models.Metrics`, `Models.Reports`); `DailySeriesSummary` reaches **Flow metrics `Domain`**. | Expected `Domain` = {`ReportWindowPolicy`, `ReportCapPolicy`, `ReportSeriesReading`}; `DailySeriesSummary` stays with the metrics in `Application/Metrics` (its edge to another context's `Domain` fails "Domain → kernel only"). The IL decides (D-1). |
| E9 | Implicit resolution: no moved file names a loose `Services.Implementation` / `Services.Interfaces` type without a `using` (all 6 clock users have it). But **same-namespace** resolution breaks on the split: `Services.Implementation.Reports` files name `ReportWindowPolicy` / `ReportCapPolicy` / `ReportSeriesReading` with no `using` today, and the `Metrics` files resolve `Services.Implementation.Reports` types through the ancestor namespace. | S2 adds `using Lighthouse.Backend.Modules.Reports.Domain;` where a moved `Application` file names a `Domain` type, and `using static …Modules.Reports.Domain.ReportSeriesReading` replaces the 2 existing `using static` lines. `Application/Metrics` keeps resolving `Application` types as its ancestor. S1's IL measurement lists every such edge (origin, target) in the membership data, never from `using` lines. |

## Wave: DESIGN / [REF] Decisions
All taken as recommended (AFK, 2026-11-21).

### D-0 No schema change, no behaviour change — RECOMMENDED
No entity, `DbSet`, migration, route, DTO, payload member, `PayloadSchemaVersion`, usage-data event or configuration key
changes. Checked by `ef-model-unchanged` and an empty diff under `Lighthouse.Migrations.*`, `Lighthouse.Frontend`,
`Lighthouse.EndToEndTests`.

### D-1 Layout — RECOMMENDED: `Modules/Reports/{Domain, Application, Application/Metrics}`; a type goes to `Domain` iff its IL edges reach only the kernel, the BCL and other Reports `Domain` types
| Option | Verdict |
|---|---|
| **a (recommended)** `Domain` for kernel-only pure types (expected the 3 of E8); `Application` for use cases, adapters-to-Flow-metrics and the 11 ports; `Application/Metrics` for the 8 catalog strategies | Same shape as Flow metrics, Forecasting, Refinement; `<Layer>.<Sub>` is allowed by ADR-270 and used by `Modules/WorkItems/Application/Dependencies`. Keeps today's `Metrics` grouping, so the move stays a move. Membership by measured property, not name (GDP-8). |
| b Flatten `Metrics` into `Application` | Rejected: renames a grouping that carries meaning (one strategy per metric) for no rule gain. |
| c Put `DailySeriesSummary` in `Domain` with a listed edge to Flow metrics `Domain` | Rejected: weakens "Domain → kernel only" for one type; Flow metrics' R3 list already names it as a legal caller from wherever it lives. |
No `Application/Policies` (no Reports file names `IDomainEventHandler`), no `Adapters/Background`, no
`Adapters/Persistence` (D-4).

### D-2 Membership — RECOMMENDED: the 31 files move; the rest is named with owner
Moving: the 12 + 8 + 11 of the Baseline. Staying, each named in `ReportsMembership` with its reason: `ReportRepository`
(D-4), the 7 `Models/Reports` types (D-3), the API controllers / base / DTOs and `ReportOwnerWriteAccess` (driving
adapter; the owner-write adapter must live in `API`).

### D-3 `Models/Reports` — RECOMMENDED: all 7 stay
`Report` is EF-mapped (`LighthouseAppContext`); `ReportOwner`, `ReportReadModels`, `ReportBulkOutcomes`,
`ReportRefusalCodes` are reached by `Report` or returned through the API; `ThenAndNowPayload` is the stored payload.
`ReportOwnerGoneException` (thrown and caught only inside Reports: the two series adapters and `ReportBulkCommands`) is the one type that passes all four conditions; it stays with its siblings by default (open question 8). The other six each fail at least one S03a condition; moving changes EF snapshots or wire
namespaces for no value. The existing rules already treat `Models.Reports` as part of the module (`ReportModelsPattern`).

### D-4 `ReportRepository` — RECOMMENDED: stays in `Services/Implementation/Repositories`
| Option | Verdict |
|---|---|
| **a (recommended)** Stay (S03f D-4); named in the membership with owner Reports and target `Modules/Reports/Adapters/Persistence` "when S05*/S10 reshape repositories" | Consistent with S03f D-4 and S03g D-4; `RepositoryRecognition` (`ConcreteRepositoriesNamespace`, `RepositoriesFolder`) and `Only_the_composition_root_names_the_report_repository` unchanged. |
| b Move to `Modules/Reports/Adapters/Persistence` now | Cheap (E7: no new inbound edge, no base type), and the brief's IN allows it "if S01 assigns it". Rejected by default only to keep one repository decision for the Epic; open question 2. |

### D-5 Rules first — RECOMMENDED: EXTEND S03f's preservation law with a Reports membership map
For every regex-valued field `P` in the test assembly (const **and** static readonly strings, found by reflection) and
every moved type `T`: `IsMatch(P, old(T)) == IsMatch(P, new(T))`, and no added alternative matches a production type
outside `Modules.Reports`. Behavioural evaluation catches the alternation form (E1, E2). If S03g left the law shared
(`NamespaceMoveLaw` taking a map), Reports adds its map; if not, S1 lifts it first (one owner). A second assertion sweeps
the test project's source text for `Services.Implementation.Reports`, `Services.Interfaces.Reports`,
`Services/Implementation/Reports`, `Services/Interfaces/Reports` **and** any alternation group containing `Reports` next
to `Services\.(Implementation|Interfaces)`. Its rename list is **fixed at S1 from the pre-move grep and only shrinks**;
each entry is a literal S2 renames (S2 removes it when it renames it; the sweep fails if a listed literal no longer
exists or an unlisted one appears). It also lists the three interpolated readers (E1) as "composes `ReportsPattern`, no
own fragment". RED in S1: `ReportsPattern` and `RefinementReportsAccessAndLicensing` match the old names and not the new
ones, so the law fails until S1 widens them. Old-namespace alternatives added by S1's widening are allowed by the list
until S3 (S3's RED). Rejected: one `…RulesSeeTheModule` fixture per pattern (misses forgotten patterns).

### D-6 Tests move with production — RECOMMENDED; acceptance and wire tests stay
The 18 unit tests move in S2's `refactor(reports): move … tests` commit to `Tests/Modules/Reports/Domain`
(`ReportWindowPolicyTest`, `ReportCapPolicyTest`), `…/Application/Metrics` (`ArrivalsReportMetricTest`,
`CycleTimeReportMetricTest`, `DailySeriesSummaryTest`, `ThroughputReportMetricTest`, `WorkItemAgeReportMetricTest`) and
`…/Application` (11), namespace = folder, kept there by `ReportsUnitTestPlacementTest` (S3). The 33 port/host/wire
files and `ReportRepositoryTest` stay; only `using`s change. "Improve the tests you touch": the moved classes are read
against the test-writing skill's backend half; none is a host user, so no `HostUsersStillToMove` row is expected to move.

### D-7 DI — RECOMMENDED: same registrations, new `using`s
`Program.cs` lines 29-31 change; `RegisterReports` (1327-1346) is untouched text. No `AddReports()` extension (module
start-up belongs to S09a).

### D-8 New rules — RECOMMENDED: `ReportsModuleBoundariesArchUnitTest` on `ModuleRules`, existing guards kept
See Ratchet design. `ReportsModuleArchUnitTest` keeps every assertion (only-API-and-root, no PBC baseline, series port,
read/write split, templates, no `then`/`now`, only root names the repository, policies handed every input, write
guards, owner-less list, never sees who asks, single add/remove, `NowEndingOn`, owner write access in API); it changes
only through `ReportsPattern` (S1 widen, S3 narrow) and its `using` lines.

### D-9 CI and pushes — RECOMMENDED: commit per slice, push S1-S3 together
S2 touches `Program.cs`, which selects every live connector suite; one push means one live run. Push only with all three
green locally (fast loop and the filtered full gate).

### D-10 Behaviour-free proof and EXAMINE — RECOMMENDED: reuse admitted probes, no new compare command
| Expectation | Probe |
|---|---|
| X1 Same types, new namespaces; all rules green; Reports rules green by namespace | `architecture-rules` (all) |
| X2 No EF model or migration change | `ef-model-unchanged` |
| X3 Same Reports answers (create, edit, read Then & Now, list across owners, bulk create, bulk removal, cap, refusals) | `backend-tests` on the unchanged-assertion `API.Integration.Reports`, `Acceptance.BaselineReportCreation` and Reports usage-data classes, both revisions |
| X4 CI selects backend for `Modules/Reports` paths; live suites for `Program.cs` | `test-selection-show` |
Rejected: a `reports-answers-compare` command — the unchanged port and host tests already observe every answer.

### D-11 Mutation bar — RECOMMENDED
Last, on frozen code, Stryker.NET over `Modules/Reports/**` (x64 SDK, `run-backend-x64.ps1`), compared with the same
files' run on the pre-move revision: **no survivor or no-coverage mutant added in the moved code**; the 80 % project bar
reported.

### D-12 Formal verification — RECOMMENDED: no Quint / TLA+ / Agda model; the law is executed exhaustively
The claims are structural relations over a finite, closed set (compiled types × membership map × regex fields): no
state, transition, concurrency, time or unbounded input. The preservation law and boundary rules are exhaustive finite
checks executed by NUnit over the IL — the executable projection the default suite runs. PBT: N/A, closed finite domain.
No proof obligation is declared, so no checker install or waiver is needed.

## Wave: DESIGN / [REF] Slices
One nwdes feature `story-6366-s03h-package-reports`, 3 slices, each ≤ 1 day, moves in commits apart from edits
(`refactor(reports): …`). Size **S, about 2.5 days**.

| # | Delivers | Oracle (native NUnit, `Lighthouse.Backend.Tests/Architecture/`) | Production targets | Est. |
|---|---|---|---|---|
| S1 | **First action: confirm S03e (#6363), S03f (#6364) and S03g (#6365) are merged into `main` (`git merge-base --is-ancestor <tip> main` for each), then re-measure every count of this file.** Rules see the move before it happens: membership measured from the IL (each type, layer, old and new namespace; staying files with owner and reason; implicit same-namespace edges of E9); `ReportsPattern` and the joint constant widened to old ∪ new; re-grep of suppression / analyser / CI / budget / host-user / Stryker / log-override targets recorded (E5) | `ReportsMembership.cs` + `ReportsMembershipArchUnitTest.cs` (a listed type is found in its old **or** new namespace; RED when a listed name does not exist or a `Domain` candidate's IL edges leave the kernel); the law of D-5 over the Reports map — **RED today** on `ReportsPattern` and `RefinementReportsAccessAndLicensing` (old name matches, new does not), GREEN after widening; the literal sweep with its fixed list | none | 1 d |
| S2 | Reports lives in `Modules/Reports/{Domain, Application, Application/Metrics}`; its unit tests in `Tests/Modules/Reports/…`; `using`s, `Program.cs` lines 29-31 and the E4 literals renamed | S1's membership test tightened to accept **only the new namespace** for moved types — RED before the move, GREEN after; all existing rules green | 31 production files moved; `Program.cs`, `ReportRepository`, 6 API files, 13 test files (`using`s / literals) | 0.75 d |
| S3 | Rules by namespace only: `ReportsPattern` and the joint constant narrowed to `Modules.Reports`; new boundaries rules; unit-test placement; `ARCHITECTURE.md` §4 row 9 anchor + "Reports module rules" bullet, ADR-270 row 70, R3 list line 225 and a "Reports today" layout line, catalogue S03h row, brief IN/AC/namespace rule | **RED oracle**: S1's sweep run with an **empty** rename / allowance list — no regex field and no string literal in the test project may still match `Services\.(Implementation\|Interfaces)\.Reports`, `Services/(Implementation\|Interfaces)/Reports` or an alternation naming `Reports` under `Services`. RED at S3's start (S1's widened old alternatives still match), GREEN once narrowed. Written first, observed RED, then narrowed. Alongside (green on writing, guards later regressions, not the RED): `ReportsModuleBoundariesArchUnitTest.cs` (R-RP1..R-RP6), `ReportsUnitTestPlacementTest.cs`, planted-type falsifiers in the `ModuleRulesCatchANewOffenderArchUnitTest` style | none (test side and docs) | 0.75 d |

Commands (host runs; nothing run in DESIGN):
```
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Report"
dotnet build Lighthouse.Backend                                            # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```
Driving port for every oracle: the compiled production assembly read through `LighthouseArchitecture.Production` /
`IlDependencies` (in-process; the nearest executable argv is the `dotnet test … ~Architecture` line). Behaviour is
observed through the unchanged Reports acceptance tests at the application-service port and the host tests.

Rigor (CLAUDE.md profiles, proposed): S1 `thorough` (opus / sonnet: the law and the alternation / interpolated readers
are where earlier reviews found misses); S2, S3 `standard` (sonnet / haiku). Open question 1.

## Wave: DESIGN / [REF] Ratchet design
### S1 widening (old ∪ new), S3 narrowing (new only)
| Owner | Constant / use | S1 | S3 |
|---|---|---|---|
| `ModuleBoundariesArchUnitTest` | `ReportsPattern` (read by `ReportsModuleArchUnitTest`, 3 interpolated readers, `ReportsModule`, guard) | `+ ^Lighthouse\.Backend\.Modules\.Reports(\.\|$)` | new only |
| `TrackerIntegrationModuleBoundariesArchUnitTest` | `RefinementReportsAccessAndLicensing` (alternation; post-S03e/S03g form re-measured) | `+ ^Lighthouse\.Backend\.Modules\.Reports(\.\|$)` | `Reports` dropped from the old alternation |
| `ReportsModuleArchUnitTest` | `ReportModelsPattern`, `RepositoryPortPattern`, `ApiLayerPattern`, `CompositionRootPattern` | unchanged (name staying code) | unchanged |
| `FlowMetricsInboundDomainAndAdaptersArchUnitTest:47` | literal `…Reports.Metrics.DailySeriesSummary` | on the S1 rename list | renamed in S2 |

### New rules (S3), baselines from the IL
| # | Rule | Expected baseline (text; IL decides) |
|---|---|---|
| R-RP1 | Each listed Reports type lives in its layer; none left under `Services` | membership map |
| R-RP2 | `Domain` → kernel and BCL only | empty |
| R-RP3 | Outside → `Domain` only from `API`, the composition root and Reports `Application` | 0 listed (no API file names a `Domain` candidate today) |
| R-RP4 | Outside → `Application` only from `API` and the composition root (the existing only-API-and-root rule, by namespace), plus `ReportRepository` (implements the ports) | 1 listed: `ReportRepository` |
| R-RP5 | `Application` → no `Data`, `API`, module `Adapters`, Tracker integration, RBAC (`IRbacAdministrationService`) or update code; Flow metrics `Application` legal (conformist down); EF Core only along a listed edge | EF list: `ReportBulkCommands` → `DbUpdateException`; `ReportBulkRemover` → `DbUpdateConcurrencyException`, `DbUpdateException` (3, measured in S1) |
| R-RP6 | `Application` → Flow metrics `Domain`, Licensing, Organisation set-up data, platform infrastructure only along a listed edge | `DailySeriesSummary` → Flow metrics `Domain`; `ReportCommands`, `ReportQueries` → `ILicenseService`; `ReportOwnerLookup`, `TeamReportMetricSeries`, `PortfolioReportMetricSeries` → `IRepository<Team>` / `IRepository<Portfolio>`; 6 types → `ILighthouseClock`; count fixed in S1 |
Lists inline, by full type name, compared both ways, only shrink. `ModuleWording("Reports", "the API")`.

## Wave: DESIGN / [REF] Component decomposition
| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| `ReportWindowPolicy`, `ReportCapPolicy`, `ReportSeriesReading` (if IL kernel-only) | `Services/Implementation/Reports` | `Modules/Reports/Domain` | MOVE | S2 |
| 9 other implementations + 11 ports | `Services/{Implementation,Interfaces}/Reports` | `Modules/Reports/Application` | MOVE | S2 |
| 8 metric strategies + catalog + `DailySeriesSummary` | `Services/Implementation/Reports/Metrics` | `Modules/Reports/Application/Metrics` | MOVE | S2 |
| `ReportRepository` | `Services/Implementation/Repositories` | — | UNCHANGED (D-4) | — |
| `Models/Reports/*` | `Models/Reports` | — | UNCHANGED (D-3) | — |
| Reports rules, membership, placement | `Tests/Architecture` | same | EXTEND / CREATE_NEW | S1, S3 |

## Wave: DESIGN / [REF] Driving and driven ports
Driving: unchanged — `TeamReportsController`, `PortfolioReportsController` (via `ReportsControllerBase`, Team/Portfolio
Read/Write guards), `ReportsController` (owner-less list, bulk create, bulk removal), the print route and CLI / MCP
read tools through the same endpoints. Driven: `IReportReader` / `IReportWriter` → `ReportRepository` (unchanged place),
`IReportMetricSeries` → the Team / Portfolio series adapters → Flow metrics `ITeamMetricsService` /
`IPortfolioMetricsService`, `IReportOwnerLookup` → `IRepository<Team|Portfolio>`, `IReportOwnerWriteAccess` →
`API.ReportOwnerWriteAccess`, `ILicenseService`, the clock. No new external call.

## Wave: DESIGN / [REF] Reuse analysis
| Existing | Overlap | Decision |
|---|---|---|
| S03f/S03g preservation law (`NamespaceMoveLaw` if S03g lifted it) | proving rules see a move | **EXTEND** (D-5) with the Reports map; add non-const regex fields if the law reads only `const` |
| S03g literal sweep | old-namespace literals | **EXTEND** with the alternation form (E1) |
| `TrackerIntegrationMembership` / `PromisesMembership` (+ `RefinementMembership` after S03g) | namespace-agnostic membership | **REUSE** shape for `ReportsMembership` |
| `ModuleRules`, `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | layer and edge rules with lists, falsifiers | **REUSE** for R-RP1..R-RP6 |
| `…UnitTestPlacementTest` (Flow metrics, Forecasting, Promises, Work Items, Tracker integration) | tests mirror layout | **REUSE** shape |
| `ReportsModuleArchUnitTest` | Reports' own guards | **REUSE** unchanged (keys on `typeof` and `ReportsPattern`) |
| `architecture-rules`, `ef-model-unchanged`, `backend-tests`, `test-selection-show` | behaviour-free proof | **REUSE** |
No CREATE_NEW production type. Semantic-duplicate check: no second owner of "which namespaces are Reports" — `ReportsPattern`
stays the single owner, the membership map is its measured projection.

## Wave: DESIGN / [REF] Impact closure
| Locator | Kind | Decision | Reason |
|---|---|---|---|
| `Services/{Implementation,Interfaces}/Reports/**` (31) | production | MIGRATED | D-1, D-2 |
| `Program.cs` lines 29-31 | composition root | CHANGED (`using`s only) | D-7, E3 |
| `API/{TeamReports,PortfolioReports,Reports}Controller`, `ReportsControllerBase`, `ReportOwnerWriteAccess`, `DTO/Reports/NewReportRequest` | consumers | CHANGED (`using`s) | |
| `Services/Implementation/Repositories/ReportRepository` | adapter | CHANGED (`using`) | D-4 |
| 18 unit tests; 13 test files naming the namespaces | tests | MIGRATED / CHANGED | D-6, E4 |
| Rule files of E1, E4 | architecture tests | CHANGED | S1, S2, S3 |
| `GlobalSuppressions.cs`, `.editorconfig` ×2, `.globalconfig`, `.csproj`, Stryker configs | build / analyser | UNCHANGED_COMPATIBLE unless S1's re-grep finds a target | E5 |
| `path-classifier.sh`, `dev-test.*`, `test-time-budget.json` + Python tests, `HostUsersStillToMove`, `TestHostPerFixtureGuardTest` | CI / test infra | UNCHANGED_COMPATIBLE | E5 (named classes stay) |
| Log categories of 5 moving classes | runtime log | CHANGED (namespace) | E5; no override or perf helper keys on them |
| `Models/Reports`, `Data`, migrations, frontend, E2E, Lighthouse-Clients | — | UNCHANGED | D-0, D-3 |
| `ARCHITECTURE.md` §4 row 9 (anchor `Modules/Reports`, `Models.Reports`, the controllers) + a "Reports module rules" bullet; ADR-270 row 70 → "**Packaged** in `Modules/Reports` by Story 6366; scheduled delivery and further report types are target", line 225 R3 entry → `Modules.Reports.Application.Metrics.DailySeriesSummary` (current authority listing a live ratchet), a "Reports today: `Domain` (3), `Application` (20), `Application/Metrics` (8)…" layout line; catalogue S03h row; slice brief IN/AC/namespace rule (open question 6) | docs | CHANGED in S3 | context-map rule |
| `docs/product/architecture/brief.md` lines 9381, 10885 (`Services.*.Reports`); ADRs 219-222, 265-267 | docs | UNCHANGED: left as history (dated records); ADR-270 and `ARCHITECTURE.md` §4 are the current authority | |

Agreement analysis: no shared wire / schema / config contract changes (D-0); `not-applicable` for agreement and contract
tests (single deployable, same commit; the clients read the unchanged HTTP API). Architectural tests: applicable
(R-RP1..R-RP6, the law). Prefactoring: S1 is the prefactoring (rules widened, behaviour and production untouched; the
existing suite GREEN before and after S1, then S2 moves).

## Wave: DESIGN / [REF] Constructive chain and residuality
- Chain: compiled assembly (input) → `ReportsMembership` + law (producer) → ArchUnit rules (consumer) → `dotnet test
  … ~Architecture` exit and message (public observation). Falsifier: a planted type in `Services.Implementation.Reports`
  or a Reports `Application` type reaching Tracker integration fails with WHAT / WHY / HOW. DESIGN `PASS`, runtime
  `DESIGNED_NOT_BUILT`, proof N/A (D-12).
- Stressors (negated assumptions): (1) "a text sweep finds every pattern" — false for the alternation (E1) → law
  evaluates regexes by behaviour; (2) "the law sees every pattern" — false for interpolated non-const readers → they
  compose the owner, listed in the sweep; (3) "only `using`s move with files" — false for same-namespace resolution on
  the split (E9) → S2 adds `using`s, IL lists them; (4) "S03e/S03f/S03g left the counts as measured" — false by
  construction → S1 re-measures, the joint constant in particular; (5) "a suppression or log override follows a move"
  — false in general (S03f E13) → re-grep, none today.
  Walked afterwards with a second set (a test class name in budget / host lists, E5; `ReportOwnerWriteAccess` drifting
  into the module with the ports — pinned to `API` by an existing rule; `ThenAndNowPayload` moved and the stored JSON
  changing — D-3 keeps it, and System.Text.Json stores no type name): all survive. Walkthrough, not execution.

## Wave: DESIGN / [REF] Context-map placement
- Contexts touched: **Reports** (packaged, no behaviour). Edges unchanged: Flow metrics → Reports (conformist, through
  `IReportMetricSeries`); Reports → Licensing (`ILicenseService`), Access (guards in API only; the module never sees
  who asks), Organisation set-up (owner lookup through `IRepository<Team|Portfolio>`); Reports → people outside
  Lighthouse (PDF export, published language).
- New terms / concepts: none. Domain events: none published or reacted to ("Report created" stays target only).
- UI surface: none (nothing a user sees changes).
- ADR-270 / `ARCHITECTURE.md` §4 updates: S3 tasks listed in Impact closure.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)
- [ ] S03e, S03f and S03g merged (`merge-base --is-ancestor`); counts re-measured in S1.
- [ ] Behaviour-free: routes, DTOs, payload, schema version, usage-data events unchanged; frontend and E2E untouched.
- [ ] No migration, `Data` or `Models/Reports` file changes.
- [ ] Moves in `refactor(reports): move …` commits apart from edits.
- [ ] `Program.cs`: only lines 29-31 change.
- [ ] Strings, suppressions, log overrides and CI targets re-grepped (E5) on the post-S03g `main`.
- [ ] Joint constant `RefinementReportsAccessAndLicensing` widened and narrowed for Reports only.
- [ ] Architecture docs updated in S3 (incl. ADR-270 row 70, line 225, the brief's amended IN/AC); finalize checklist run before the push.

## Wave: DEVOPS
- **Platform lens**: none — no deployment, configuration, persistence, pipeline or runtime change; one push runs the live
  connector suites once because `Program.cs` changes (D-9). Five log categories change namespace; no shipped override
  or harness reads them (E5).
- **Usage data**: **N/A, because** the Story changes no behaviour a user can use; Reports' existing events (report
  created / opened / PDF export / bulk, `ReportUsageEventsTests`, `ReportPdfExportUsageEventTests`,
  `BulkReportsUsageEventTests`) keep firing unchanged, and no new outcome exists to count.
- KPI: KPI 1 of the catalogue (files under `Services`) drops by 31; measured at S3.

## Open questions (default taken)
1. Rigor: S1 `thorough` (opus / sonnet), S2 and S3 `standard` (sonnet / haiku)? **Default: yes.**
2. `ReportRepository`: stay in `Services/Implementation/Repositories` until S05*/S10 (D-4 a), although moving it to
   `Modules/Reports/Adapters/Persistence` would add no inbound edge? **Default: stay, for one repository decision across
   the Epic.**
3. No `reports-answers-compare` EXAMINE command; X3 uses the unchanged port and host tests (D-10)? **Default: no new command.**
4. `Domain` holds only the IL-measured kernel-only types (expected `ReportWindowPolicy`, `ReportCapPolicy`,
   `ReportSeriesReading`), and `DailySeriesSummary` stays in `Application/Metrics` (D-1)? **Default: yes; if none
   qualifies, no `Domain` folder and R-RP2/R-RP3 are not added.**
5. Keep the `Metrics` sub-folder as `Application/Metrics` rather than flattening (D-1 b)? **Default: keep.**
6. `Models/Reports` (7 types) stays in `Models` (D-3) and `ReportRepository` stays (D-4). This narrows the brief's IN
   (line 11: "`Models/Reports/**`, `ReportRepository` if S01 assigns it") and restates its namespace rule (line 12,
   stale "Platform", "RBAC/licence"). **Default: S3 amends the brief to "no Reports file remains in
   `Services/Interfaces` or `Services/Implementation` other than `ReportRepository`; `Models/Reports` stays in the
   kernel; Reports depends down only on Flow metrics, Licensing, Organisation set-up data, the kernel and the platform
   infrastructure, and never sees Access beyond the API guards".**
7. List the 3 EF Core exception edges in R-RP5 instead of moving the catch into an adapter (that would be an edit and
   disprove the hypothesis)? **Default: list them; the list only shrinks.**

8. `Models/Reports/ReportOwnerGoneException` passes all four S03a conditions (no EF mapping, not on the wire, not
   reached by a kernel type, used only inside Reports). Move it to `Modules/Reports/Application` in S2? **Default: no;
   it stays with the other `Models/Reports` types, which the rules already count as part of the module, and S04b
   (reduce `Models` to the kernel) picks it up.**

## Architecture summary for the maintainer
**Shape (hexagonal).** Nothing new is built; files move and rules are restated.
```
 Driving (unchanged)                 Application (MOVED)                          Driven (unchanged)
 TeamReportsController   ─┐  Modules/Reports/Application                     ┌─ IReportReader/IReportWriter → ReportRepository
 PortfolioReportsCtrl    ─┼─► ReportQueries, ReportCommands, ReportBulk*, ───┼─   (stays in Services/Repositories)
 ReportsController       ─┘  ThenAndNowTemplate, ReportOwnerLookup,          ├─ IReportMetricSeries → Team/Portfolio series
 (bulk, list across owners)  11 ports co-located                             │   → Flow metrics ITeam/IPortfolioMetricsService
                             Application/Metrics (MOVED): catalog, 5 metrics ├─ IReportOwnerWriteAccess → API adapter
                             Modules/Reports/Domain (MOVED): window, cap,    └─ ILicenseService, clock, IRepository<Team|Portfolio>
                               series-reading policies
                             Models/Reports (kernel, UNCHANGED): Report entity, owner, payload, read models
```
- Driving adapters: the three Reports controllers — unchanged. Application services and ports: moved to
  `Modules/Reports/Application`, ports beside their use cases, metric strategies in `Application/Metrics`. Domain: three
  pure policies moved to `Domain`; the entity and wire records stay in `Models`. Driven adapters: the repository stays;
  the owner-write adapter stays in the API; no new external call.

**Patterns weighed.**
- Rules first, then move (chosen): the rules must see the move, or they pass over an empty set.
- Preservation law that evaluates regexes by behaviour (chosen) vs a text sweep alone (rejected: misses the joint
  alternation constant) vs one fixture per pattern (rejected: misses forgotten patterns).
- Domain / Application split by measured dependencies (chosen) vs all in Application (rejected: loses the kernel-only
  guarantee for three pure policies).
- Strategy (`IReportTemplate`, `IReportMetric`) and CQRS-style read/write ports (kept): already in place; the move keeps
  them intact and the existing rules keep pinning them.
- Context-owned repository in `Adapters/Persistence` (rejected for now): free here, but one repository decision for
  the Epic (S03f D-4) beats a one-off; open question 2.
- Moving kernel models (rejected): EF snapshots and wire shapes would change.
- Moving the EF exception catches into an adapter (rejected): an edit, not a move; listed edges instead.
- Single responsibility / dependency inversion: ports stay with their use cases; Reports depends down only, and only
  the API and the composition root depend on it.
