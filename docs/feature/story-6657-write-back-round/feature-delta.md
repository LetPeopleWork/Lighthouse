# Feature: story-6657-write-back-round
Story #6657 · Place the write-back round (candidate: Forecasting `Application/WriteBack`) · Epic #6510 (Modules: package the
bounded contexts) · catalogue Epic #6264

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
  (S03d "done" note line 937 and "Later, not this Story" lines 942, 954; S09a row lines 347-360; D-6264-16).
- Origin: [S03d D-6](../story-6362-s03d-package-promises-deliveries/feature-delta.md) (line 290: "not Promises; stays,
  named exception"; option b "Forecasting `Application`" deferred to its own Story with its own measurement), and S03a D-6.
- Authority: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md) ("Left open",
  write-back entry lines 970-981; "Every edge" table lines 180-202; Forecasting row line 67), `ARCHITECTURE.md` §4 row 3
  (line 100) and the "Forecasting module rules" bullet (lines 160-166).
- Precedents copied, same shape: [S03h](../story-6366-s03h-package-reports/feature-delta.md) (structure, three slices,
  sweep with fixed rename list) and [S03g](../story-6365-s03g-package-refinement/feature-delta.md) (`NamespaceMoveLaw`,
  `…PatternRoles`, `…StringsTheMoveRenames`). Unlike both, this Story moves files **into an existing module** (Forecasting),
  out of the **loose** `Services.Implementation` / `Services.Interfaces` namespaces, which no rule pattern names.

DESIGN and DEVOPS run 2026-11-24, **AFK, PROPOSE mode**: every engineering choice takes the recommended option and is
recorded as such; the open questions at the end are confirmations with the default taken, not blockers. Nothing a user
uses changes. Nothing was built or run with `dotnet` or `pnpm`; every count is a **text measurement on `main` at
`952040471`** (`git ls-files`, `git grep`), confidence `noisy` (misses same-namespace and fully qualified references,
matches comments). The compiled IL measurement in slice S1 decides.

**Start condition.** S03g (#6365, being delivered in worktree `wise-sleeping-firefly`, head `4ac5350ae`), S03h (#6366,
designed) and S03i (#6468, designed) are delivered and merged into `main` before S1 starts. The maintainer's order is
6365 → 6366 → 6468 → **6657** → 6382 (S09a). All three edit files this Story edits (see "DoR re-run at DELIVER start").

**What "merged" means, checked by S1 for each of #6365, #6366, #6468; all three checks must hold:**
1. ADO: the Story's state is `Closed` (`az boards work-item show --id <id> --query 'fields."System.State"'`).
2. Git: `git log main -1 --format="%h %s" --grep="story <id>" -- Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture`
   returns that Story's final rules commit; S1 records the hash and runs `git merge-base --is-ancestor <hash> HEAD`
   (exit 0). No hit is "not merged".
3. Content (the property, not the message): `git ls-files` on `main` shows no `Services/Implementation/Refinement/`,
   no `Services/Implementation/Reports/`, no `Services/Interfaces/IBlackoutPeriodService.cs`; `Tests/Architecture/NamespaceMoveLaw.cs`
   exists.
A failed check stops S1 and is reported with the Story, the check and its output; S1 never starts on a partial merge.

## Wave: DESIGN / [REF] Inherited
- ADR-270 "Left open", write-back: the **translation** into tracker fields is Tracker integration's (`IWriteBackTarget`
  in `Modules/TrackerIntegration/Application`, implementations in `Adapters/Connectors`) — S03a. The **round** decides
  what is written and when; not Tracker integration (the ACL would reach Flow metrics and Licensing, S03a), not
  Promises (Forecasting would reach Promises, the reverse of the legal edge, S03d). #6657 places it.
- ADR-270 Decision 6 layout: `Modules/<Context>/{Domain, Application[/<Sub>], Adapters/<Kind>}`, namespace = folder,
  ports co-located in `Application`; rules read from the IL (`IlDependencies`); lists inline, compared both ways, only
  shrink (ADR-268).
- D-6264-16 / S03f (#6364): Platform is composition-root infrastructure below the contexts; a platform edge into a
  module is governed by that module's inbound list, not listed twice (precedent: `DatabaseMaintenanceGate` in Flow
  metrics' R4, ADR-270 line 192).
- S03f D-4: repositories stay in `Services/*/Repositories`. S03a D-3: a `Models` type moves only if it is not an EF
  entity, not on the wire, not reached by a kernel type and not used outside the context.
- S09a brief (slice-26) "no early start; all S03* first": S09a runs once over every context; its starting list is each
  module's inline edge lists. Registration extensions (`AddForecasting`) are S09a's, not this Story's.
- S03i D-8 (designed): downstream modules, Forecasting included, reach Organisation set-up's `Application` by right
  (`IBlackoutPeriodService` moves there).

## Wave: DESIGN / [REF] DoR re-run (2026-11-24)
| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | 8 files (5 classes, 3 ports) sit in the loose `Services.Implementation` / `Services.Interfaces`; 5 rule files name them one by one in 8-entry lists, one comment saying "The write-back round has no namespace of its own" (`PromisesModuleBoundariesArchUnitTest.cs:170`) (E1, E2). |
| 2 Users / value | PASS | Maintainers: the last named exception of S03d leaves `Services`; S09a sees the round inside a context. No user-visible change. |
| 3 Hypothesis | PASS, sharpened | "Moves with `namespace` and `using` changes only." Holds for production: every moved file already carries explicit `using`s for what it names (E8); `Program.cs` registers by short name (E3), so one `using` line is added. Disproved only if a production edit beyond `namespace` / `using` lines is needed. |
| 4 Size | PASS | **S-M, about 3 days**, one nwdes feature, 3 slices. The breadth is in the rule files (10 test files, ~60 literals), not in production. |
| 5 AC from UAT | PASS, made precise | (a) "Owner confirmed": Forecasting, `Modules/Forecasting/Application/WriteBack` (D-1). (b) "Edges to Tracker integration's write-back port": one listed caller, `WriteBackService` (D-4). (c) "Whether the move waits for S09a": **no; it must come before S09a** (D-9). (d) No write-back file remains in `Services`; `Models/WriteBack/*` stays in the kernel (D-3). |
| 6 Dependencies | PASS | Hard: #6365, #6366, #6468 merged first (shared rule files, Start condition). Blocks: #6382 S09a (D-9). Soft: Epic 6474 slice 12 (updaters), S05* (repositories). |
| 7 Technical notes | PASS | No `GlobalSuppressions.cs`, `.editorconfig`, `.globalconfig`, `.csproj`, `Directory.Build.props`, Stryker config, CI path classifier, `test-time-budget.json` entry, Serilog override (`appsettings*.json`) or perf helper (`Lighthouse.EndToEndTests/perf/helpers/collect.ts`) names a round type (E5). No schema change. |

## Wave: DESIGN / [REF] DoR re-run at DELIVER start
#6365, #6366 and #6468 each change files this Story changes. S1 re-measures **all** of the following on the `main` it
starts from and records each result in `WriteBackMembership.cs` or the S1 commit body; a difference is recorded, not
argued with.

| # | Re-measure | Why it can move |
|---|---|---|
| M1 | Baseline commit (replaces `952040471` everywhere in this file) | three Stories merge first |
| M2 | The three "merged" checks above | Start condition |
| M3 | `NamespaceMoveLaw` public shape (map type, `PatternDeclaration` roles, how an area is declared, whether a type may move into an **existing** module's area) and the `…PatternRoles` / `…StringsTheMoveRenames` convention | S03g creates it; S03h and S03i extend it (S03i adds per-pattern classification, its D-7) |
| M4 | `PlatformLaneBoundariesArchUnitTest.TheLanesEdgesIntoServices`: size (19 today) and the 5 `UpdateQueueService` write-back edges | S03h/S03i remove Reports / set-up edges |
| M5 | `PlatformLaneMembership.cs` "context code still in Services" entries (lines 321-329 today) and `PlatformLaneUnitTestPlacementTest` exceptions (lines 137-157) | S03g/h/i delete their own entries |
| M6 | `ForecastingModuleBoundariesArchUnitTest`: `OtherOutsideTypesReachingTheApplication` (1 today), `EdgesOfTheApplicationBelowItOrIntoTheLicence` (1), `ForecastingsLegalCallers` | S03i may add set-up as legal target; S03g/h touch `RefinementPattern` / `ReportsPattern` readers |
| M7 | `FlowMetricsInboundApplicationArchUnitTest.OtherOutsideTypesReachingTheApplication` (4 today) | S03h / S03i change the legal-caller expression |
| M8 | `LicensingModuleBoundariesArchUnitTest` R-L3 list (11 today; S03h renames 2 Reports entries) and its `TheWriteBackRound` list | S03h |
| M9 | `TrackerIntegrationModuleBoundariesArchUnitTest` R-T4 list (5 today) | S03i lists set-up callers of the ACL |
| M10 | `PromisesUnitTestPlacementTest` exceptions and `PromisesTypeNamePrefixes` (`WriteBack`, `IWriteBack`, `FeatureOrdering`, …) | S03i removes the ordering / ranking entries and prefixes |
| M11 | `AccessModuleBoundariesArchUnitTest` / `PromisesModuleBoundariesArchUnitTest` `TheWriteBackRound` lists and the predicates that read them | S03i edits Promises' upstream patterns |
| M12 | `RepositoryLeakRatchetArchUnitTest` path list (line 49 today) | S03g/h/i rename their own paths |
| M13 | `TestHostUseGuardTest.HostUsersStillToMove`, `TestHostPerFixtureGuardTest`, `TestTierGuardTest`, `test-time-budget.json` for any round test class | the test-speed Stories move rows |
| M14 | `Program.cs` line numbers of the round's 4 registrations (1483-1488 today) and the `using` block | S03i edits 1443-1452 and adds `using`s |
| M15 | Location of `IBlackoutPeriodService` (expected `Modules.OrganisationSetup.Application`) and S03i D-8 legality of Forecasting → set-up | S03i |
| M16 | Text sweep of `Services.Implementation.WriteBack`, `Services.Interfaces.IWriteBack`, `Services/Implementation/WriteBack`, `Services/Interfaces/IWriteBack` and the 8 short names in quotes across the test project, `Scripts/`, `appsettings*.json`, perf helpers | new literals since this design |
| M17 | ADR-270 and `ARCHITECTURE.md` §4 line numbers quoted in Impact closure | every packaging Story edits them |

## Wave: DESIGN / [REF] Baseline (`main` at `952040471`, text search, `noisy`)
| Group | Files | Moves? |
|---|---|---|
| `Services/Implementation/` (loose) | 5: `WriteBackCollector` (68 L), `WriteBackRound` (124 L), `WriteBackRoundContext` (19 L), `WriteBackService` (290 L), `WriteBackTriggerService` (365 L) | yes → `Modules/Forecasting/Application/WriteBack` (D-1) |
| `Services/Interfaces/` (loose) | 3 ports: `IWriteBackCollector`, `IWriteBackService`, `IWriteBackTriggerService` | yes, co-located (D-1) |
| `Models/WriteBack/*` | 9: `WriteBackMappingDefinition` (EF entity), `WriteBackAppliesTo`, `WriteBackTargetValueType`, `WriteBackValueSource` (stored by ordinal), `WriteBackValueSources`, `WriteBackFieldUpdate`, `WriteBackResult`, `WriteBackItemResult` (on `IWriteBackTarget`'s signature), `NotificationSuppression` | **no** (D-3) |
| `Modules/TrackerIntegration/Application/IWriteBackTarget` + Jira / ADO implementations | 3 | **no** (S03a: the translation is the ACL's) |
| API: `WriteBackMappingDefinitionDto`, `WriteBackMappingValidator`, `WriteBackMappingsHelper` | 3 | no (connection settings, driving adapter) |
| Unit tests `Tests/Services/Implementation/{WriteBackCollectorTest, WriteBackRoundTest, WriteBackServiceTest, WriteBackSuppressionWarningTest, WriteBackTriggerServiceTest (+ `.BeyondTheHorizon`, `.StartDates`), BlackoutForecastShiftWriteBackTest}` | 8 | yes → `Tests/Modules/Forecasting/Application/WriteBack` (D-6; `BlackoutForecastShiftWriteBackTest`'s subject confirmed in S1) |
| `Tests/Services/Implementation/BackgroundServices/Update/RoundWriteBackDuringDatabaseMaintenanceTest` | 1 | subject decided in S1 by what it constructs (S03f left it "with the subject it tests"); expected **stay** (its subject is the update queue and the maintenance gate) |
| Acceptance / host / container tests: `API/Integration/QuietWriteBack/*` (5), `RecurringBlackoutRulesWriteBackIntegrationTest`, `WriteBackMappingForNewAdditionalFieldIntegrationTest`, `API/WriteBack/*` (2), `Acceptance/AdoOAuthWriteConsent`, `TaskManager/*` (3), `SleRisk`, `ResponsiveDuringUpdates`, `Integration/Containers/*` (2) | ~18 | no (drive ports / hosts; `using` lines only) |

Inbound (production, `using` / name): `Modules/Forecasting/Adapters/Background/ForecastUpdater` (`IWriteBackCollector`,
`IWriteBackTriggerService`, by service locator, lines 259, 304); `Platform/UpdateQueue/UpdateQueueService` (+ nested
`HeldUpdate`: `WriteBackRound`, `WriteBackRoundContext`, static `WriteBackCollector.WriteEverythingStaged`,
`IWriteBackService`); `Services/Implementation/BackgroundServices/Update/{TeamUpdater, PortfolioUpdater,
UpdateServiceBase}`; `Program.cs` (4 registrations, lines 1483-1488, short names; `using …Services.Implementation` and
`…Services.Interfaces` stay for other loose types). `Platform/UpdateQueue/UpdateCancellationContext.cs:5` names
`WriteBackRoundContext` in prose only.

Outbound (from the 8 files, `using` / constructor): kernel `Models`, `Models.WriteBack`, `Models.Forecast`,
`Models.OAuth`; **Tracker integration `Application`** (`WriteBackService` → `IWorkTrackingConnectorFactory`,
`IWriteBackTarget`); **Flow metrics `Application`** (`WriteBackTriggerService` → `ITeamMetricsService`); **Licensing
`Application`** (`WriteBackTriggerService` → `ILicenseService`); `IBlackoutPeriodService` (loose today; set-up after
S03i); `Services.Interfaces.Repositories` (`IWorkItemRepository`, `IRepository<Feature>`, `IRepository<OAuthCredential>`);
`Platform.Host` (`ILighthouseClock`); `Platform.UpdateQueue` (`WriteBackRound` → `RefreshRoundSummary`). No EF Core,
`Data`, `API` or module `Adapters` edge.

## Wave: DESIGN / [REF] Evidence
| # | Finding | Consequence |
|---|---|---|
| E1 | **No pattern names the round.** It lives in the loose `Services.Implementation` / `Services.Interfaces`; every rule names it by full type name: `AccessModuleBoundariesArchUnitTest` (`TheWriteBackRound`, 8, lines 154-163, read at 434; fixture 594), `LicensingModuleBoundariesArchUnitTest` (`TheWriteBackRound`, 8, lines 146-155, read at 424; R-L3 entry line 115), `PromisesModuleBoundariesArchUnitTest` (`TheWriteBackRound`, 8, lines 172-181, read by `IsUpstreamCode` at 502; fixture 771), `FlowMetricsInboundApplicationArchUnitTest:59`, `TrackerIntegrationModuleBoundariesArchUnitTest:108`, `PlatformLaneBoundariesArchUnitTest:40-44` (5 edges), `PlatformLaneMembership.cs:321-329` (8 short names), `PlatformLaneUnitTestPlacementTest:137-157` (8 paths, owner wording "Tracker integration, the write-back round" — stale since S03a), `PromisesUnitTestPlacementTest:72-79` (8 paths) + prefixes line 97 + newcomer case line 149, `QuietWriteBackSeamArchUnitTest:19-20` (`HaveFullName` strings), `RepositoryLeakRatchetArchUnitTest:49` (path). Text count: **~60 literals in 11 files**. | The ratchet is not a pattern widening (as in S03g/S03h) but a **literal rename / delete** across lists compared both ways; those must change in the same commit as the move (S2). The rename list is fixed at S1 from the pre-move grep and only shrinks (D-5). |
| E2 | **A silent loss the move would cause.** Promises' upstream rule (R-P7: "no write-back round") recognises the round only by its 8 old full names. After the move the round is Forecasting `Application`, which Promises reaches **by right** (Forecasting → Promises is a legal edge). If the list were merely renamed, it would still hold; if it were dropped as "now covered by the module rules", a Promises type taking part in the round would pass. Same predicate shape in Access and Licensing (there the module rules would still catch it as "another context"). The comment at `PromisesModuleBoundariesArchUnitTest.cs:170` says why the lists exist: "The write-back round has no namespace of its own". | The move gives the round a namespace of its own. S1 adds one owner, `ModuleRules.TheWriteBackRound` (old ∪ new), and the three predicates read it; S3 narrows it to `^Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack(\.\|$)` and the three 8-entry lists are deleted. This is the argument for the `WriteBack` sub-namespace (D-1). |
| E3 | `Program.cs` registers the round by short name (lines 1483-1488) under `using …Services.Implementation` (23) / `…Services.Interfaces` (42), which stay for other loose types. | S2 adds one line `using Lighthouse.Backend.Modules.Forecasting.Application.WriteBack;`; no registration line, order or lifetime changes (reviewer diff check). `Program.cs` is in `SHARED_REGEX` of `Scripts/test-selection/path-classifier.sh`, so the push runs the live connector suites once (D-10). |
| E4 | `ForecastingModuleBoundariesArchUnitTest` lists after the move (IL decides): Application inbound `OtherOutsideTypesReachingTheApplication` 1 → **expected 5** (+ `TeamUpdater`, `UpdateServiceBase\`1`, `Platform.UpdateQueue.UpdateQueueService`, `UpdateQueueService+HeldUpdate` if the rule counts nested types); Application outbound `EdgesOfTheApplicationBelowItOrIntoTheLicence` 1 → **2** (+ `WriteBackTriggerService` → `ILicenseService`). Elsewhere: Flow metrics R4 4 → **3** (`WriteBackTriggerService` leaves: Forecasting is a legal caller by namespace); Licensing R-L3 unchanged count, 1 entry renamed; Tracker integration R-T4 unchanged count, 1 entry renamed; platform R-PL4 19 → **14** (5 `UpdateQueueService` edges now point into a module and are governed by Forecasting's inbound list, D-6264-16 / S03f). | Net: two lists grow (Forecasting inbound +4, Forecasting licence +1), one shrinks (Flow metrics −1), R-PL4 −5. The growth is a measured, listed fact, not a loosening: these edges existed and were listed elsewhere (R-PL4, R-T4, R-L3, R4). Recorded in ADR-270 with owner (D-7, D-8). |
| E5 | Lessons from the S03f/S03g/S03h reviews, checked for the round: `GlobalSuppressions.cs`, `.editorconfig` ×2, `.globalconfig`, `.csproj`, `Directory.Build.props`, Stryker configs: **none**. `Scripts/test-selection/*`, `Scripts/test-timings/*`: only `JiraWriteBackTest` (Tracker integration, stays). `appsettings*.json` Serilog overrides and `collect.ts`: **none**. **Log categories**: 3 moving classes take `ILogger<T>` — `WriteBackCollector`, `WriteBackService`, `WriteBackTriggerService` — so their categories become `Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.<T>`. `WriteBackSuppressionWarningTest` asserts through test doubles (S1 confirms it reads no category string). | Nothing to rename there; S1's re-grep (M16) repeats it. The log-category change is visible to an administrator reading logs: open question 3. |
| E6 | `QuietWriteBackSeamArchUnitTest:19-20` selects by `HaveFullName("…Services.Implementation.WriteBackTriggerService")`. After a move without a rename, the selection is empty; whether ArchUnitNET fails on an empty `Should()` depends on the positive-result setting the rule uses (not measured). | Treated as a **silent-pass risk**: on the S1 rename list; S2 replaces the two strings with `typeof(...).FullName` (rename-safe), so no list entry is needed afterwards. `WriteBackSinglePathArchUnitTest` already keys on `typeof` (rename-safe). |
| E7 | `QuietWriteBackAcceptanceTest.cs:199` has a `cref` with the full old name. | Compile-checked only when XML docs are generated; on the S1 rename list either way; S2 renames. |
| E8 | Implicit resolution: each moved file names its targets through explicit `using`s (`Services.Interfaces`, `Services.Interfaces.Repositories`, `Modules.TrackerIntegration.Application`, `Modules.FlowMetrics.Application`, `Modules.Licensing.Application`, `Platform.Host`, `Platform.UpdateQueue`). The 5 classes resolve their 3 ports today through `using …Services.Interfaces`; after the move the ports share the namespace. `UpdateQueueService`, the 3 updaters and `ForecastUpdater` resolve the round through `using …Services.Implementation` / `…Interfaces`, which stay for other types. | S2 adds `using …Modules.Forecasting.Application.WriteBack;` to `UpdateQueueService`, `TeamUpdater`, `PortfolioUpdater`, `UpdateServiceBase`, `ForecastUpdater`, `Program.cs`; the now-unused `using …Services.Interfaces` in moved files is removed only if the build flags it (IDE0005 is a warning-as-error here). S1 lists every edge (origin, target) from the IL. |
| E9 | The round's dependencies on Forecasting's own rules: Forecasting `Application` must reach no EF Core, `Data`, `API`, module `Adapters`; the round reaches none (Baseline, outbound). It reaches `Platform.UpdateQueue.RefreshRoundSummary` and `Platform.Host.ILighthouseClock`; Forecasting's Application rule does not forbid the platform lane. `ForecastingApplicationLayoutArchUnitTest` checks an exact list of `Application` types and files ("…AndNowhereElse"). | S1 confirms whether that check reads `^…Application$` only (then the `WriteBack` sub-namespace is outside it, like `Application.Policies`) or the folder recursively (then S2 adds the 8 files). Recorded in the membership data. |

## Wave: DESIGN / [REF] Decisions
All taken as recommended (AFK, 2026-11-24).

### D-0 No schema change, no behaviour change — RECOMMENDED
No entity, `DbSet`, migration, route, DTO, `WriteBackValueSource` ordinal, mapping shape, usage-data event, configuration key,
DI lifetime or registration order changes. What is written to Jira / Azure DevOps, when, how often per round, and the
quiet / suppression / licence behaviour are unchanged. Checked by `ef-model-unchanged`, an empty diff under
`Lighthouse.Migrations.*`, `Lighthouse.Frontend`, `Lighthouse.EndToEndTests`, and the unchanged write-back acceptance tests.

### D-1 Owner and layout — RECOMMENDED: Forecasting, all 8 files in `Modules/Forecasting/Application/WriteBack`
| Option | Verdict |
|---|---|
| **a (recommended)** Forecasting `Application/WriteBack`, namespace `Lighthouse.Backend.Modules.Forecasting.Application.WriteBack`, ports co-located | Forecasting triggers the round (`ForecastUpdater`) and writes its dates; it is the only context that legally reaches every publisher the round reads: Work Items (fields), Flow metrics (age, cycle time, SLE risk), Organisation set-up (blackouts, after S03i). The sub-namespace gives the round a name rules can key on (E2), keeps it apart from the forecast services, and follows `<Layer>.<Sub>` as used by `Application.Policies` and `WorkItems/Application/Dependencies`. |
| b Forecasting `Application`, flat | Rejected: the Promises / Access / Licensing guards would need an 8-name list again (E2). |
| c Split: round mechanics (`WriteBackRound`, `WriteBackRoundContext`, the static flush) to `Platform.UpdateQueue`, the rest to Forecasting | Rejected: the round stages **which connection, item and field** to write, which is context knowledge, not queue mechanics; and the flush still calls `IWriteBackService`, so the platform → Forecasting edge stays. An edit, not a move. |
| d Tracker integration / e Promises / f Work Items | Rejected: d the ACL would reach Flow metrics and Licensing (S03a); e Forecasting would reach Promises (S03d); f Work Items may not reach Flow metrics or Forecasting. |
| g Stay as a named exception in `Services` | Rejected: S09a runs once over every context and would carry the round as an exception again (D-9). |
No `Domain` member: the round's types are application orchestration (staging, flush, a per-execution ambient context),
not forecasting law, although `WriteBackRoundContext` would pass a kernel-only IL test. Membership by decision, checked
by the IL (D-2).

### D-2 Membership — RECOMMENDED: the 8 files move; the rest is named with owner
`WriteBackMembership.cs` lists: moving — the 5 classes and 3 ports with old and new full names; staying, each with its
reason — `Models/WriteBack/*` (D-3), `IWriteBackTarget` and the connector implementations (Tracker integration, S03a),
the 3 API helpers / DTO (connection settings, driving adapter), the updaters and `UpdateQueueService` (callers).

### D-3 `Models/WriteBack` — RECOMMENDED: all 9 stay in the kernel
`WriteBackMappingDefinition` is EF-mapped; `WriteBackValueSource` is stored by ordinal; `WriteBackFieldUpdate`,
`WriteBackResult`, `WriteBackItemResult` are on `IWriteBackTarget`'s signature in Tracker integration, so moving them into
Forecasting would make the ACL reach Forecasting. Each fails at least one S03a condition. S04b owns the kernel.

### D-4 Edge to Tracker integration's write-back port — RECOMMENDED: listed by name, one caller
| Option | Verdict |
|---|---|
| **a (recommended)** `WriteBackService` stays the one listed outside caller of Tracker integration `Application` (R-T4 entry renamed); Forecasting is **not** made a legal caller by namespace | Smallest change; matches `WriteBackSinglePathArchUnitTest` ("only `WriteBackService` calls `IWriteBackTarget.WriteFieldsToWorkItems`"); a second Forecasting type reaching the ACL fails. ADR-270 gets a row "Tracker integration → Forecasting: customer of the ACL's write-back capability, through `IWorkTrackingConnectorFactory` and `IWriteBackTarget`, listed (one type)", like Promises → Tracker integration. |
| b Add Forecasting `Application` to `TrackerIntegrationLegalCallers` | Rejected: widens the ACL's callers to the whole forecasting core for one service. |

### D-5 Rules first — RECOMMENDED: EXTEND the shared `NamespaceMoveLaw` with a write-back map, plus one round pattern
S1, before any move: (1) `WriteBackMembership` (the map: 8 types, old → new, area "Forecasting"). (2) The law over every
regex-valued field (const and static readonly, by reflection): for every moved type `T`, a pattern in a **Context** role
that matches `old(T)` must match `new(T)` (none expected: no context pattern sees the loose namespaces), a **Place**
pattern (the `Services` ones) must not match `new(T)`, and a pattern of the destination area (Forecasting) may start
matching — the law's declared exception for a move into an existing area (M3 decides how the S03g/S03i law expresses
it; if it cannot, S1 adds the area declaration, one owner). (3) `ModuleRules.TheWriteBackRound`, matched against the
**full name**, = the 8 old full names ∪ `^Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack\.`; the
Access, Licensing and Promises predicates read it instead of their lists. (4) `WriteBackStringsTheMoveRenames`: the
literal sweep, its list fixed from the pre-move grep (E1, E6, E7, M16), only shrinking; S2 removes each entry as it
renames or deletes it; an unlisted old literal or a listed one that no longer exists fails.
**RED in S1**: a planted fixture type in a made-up `…Fixtures.….Forecasting.Application.WriteBack` namespace, reached by a
made-up Promises `Application` type, must fail Promises' upstream rule — today it passes (the list names only old full
names, and Forecasting is legal for Promises: E2). GREEN once the predicate reads the round pattern. Rejected: renaming
the three lists in S2 only (keeps 24 hand-kept names and the "no namespace of its own" reason that the move removes).

### D-6 Tests move with production — RECOMMENDED; acceptance and host tests stay
The 8 unit-test files of the Baseline move in S2's `refactor(forecasting): move the write-back round tests` commit to
`Tests/Modules/Forecasting/Application/WriteBack`, namespace = folder, kept there by `ForecastingUnitTestPlacementTest`
(EXTEND with the sub-folder, S3). `RoundWriteBackDuringDatabaseMaintenanceTest` follows its subject (S1 decides; expected:
stays with the update code). Acceptance, host and container tests stay; only `using`s change. "Improve the tests you
touch": the moved classes are read against the test-writing skill's backend half; any that starts a host moves a row
down per the skill (M13 says whether one does).

### D-7 Platform lane → Forecasting edge — RECOMMENDED: listed in Forecasting's inbound list; inversion noted for S09a
`UpdateQueueService` creates the round, hands it over and flushes it. After the move that is a platform → Forecasting
`Application` edge. Per S03f it is governed by Forecasting's inbound list (not R-PL4), exactly like
`DatabaseMaintenanceGate` in Flow metrics' R4. Turning it round (the queue owns a "round finished" port that Forecasting
implements) is an edit with concurrency risk in the round's last-one-out flush; recorded as a catalogue note for S09a, not
scheduled here. Rejected: inverting now (an edit, disproves the hypothesis).

### D-8 Update code → Forecasting edges — RECOMMENDED: listed (`TeamUpdater`, `UpdateServiceBase<T>`), owner Epic 6474 slice 12
`PortfolioUpdater` is already listed. The Team pass writes Work Items' and Flow metrics' values, but it is staged into the
same round and flushed by it; the updaters stay in `Services` until Epic 6474 slice 12 replaces them.

### D-9 Order — RECOMMENDED: after #6468 (S03i), **before** #6382 (S09a); the move does not wait for S09a
Confirms the maintainer's chain. Hard reasons for "before S09a": S09a "runs once, over every context" and takes each
module's inline edge lists as its starting list; a round left in `Services` belongs to no context, so S09a could neither
put it behind `Forecasting.Contracts` nor sort its inbound calls (queue, updaters) into contracts or events. After S09a, the
same move would also have to cross the contracts-only rule and edit `AddForecasting`. No hard reason against: S09a's
registration extensions do not exist yet, so this Story changes only one `using` in `Program.cs`. "After S03i" is soft
but recommended: S03i edits `PromisesUnitTestPlacementTest`, the Promises upstream patterns, R-T4 and `Program.cs`, and
moves `IBlackoutPeriodService` (one of the round's targets) — after it, the round's edge to set-up is legal by S03i D-8.

### D-10 CI and pushes — RECOMMENDED: commit per slice, push S1-S3 together
S2 touches `Program.cs` (live connector suites run); one push, one live run. Push only with all three green locally.

### D-11 Behaviour-free proof and EXAMINE — RECOMMENDED: reuse admitted probes, no new compare command
| Expectation | Probe |
|---|---|
| X1 Same types, new namespace; all rules green; the round guarded by namespace | `architecture-rules` (all) |
| X2 No EF model or migration change | `ef-model-unchanged` |
| X3 Same writes: forecast dates, start dates, Feature size, age / cycle time, SLE risk; one write per connection per round; quiet write-back; licence gate; write consent; blackout shift | `backend-tests` on the unchanged-assertion `API.Integration.QuietWriteBack`, `RecurringBlackoutRulesWriteBackIntegrationTest`, `WriteBackMappingForNewAdditionalFieldIntegrationTest`, `API.WriteBack`, `Acceptance.AdoOAuthWriteConsent`, `TaskManager`, both revisions |
| X4 CI selects backend for `Modules/Forecasting` paths; live suites for `Program.cs` | `test-selection-show` |
Rejected: a `write-back-compare` command; the unchanged port and host tests observe every write through recording targets.

### D-12 Mutation bar — RECOMMENDED
Last, on frozen code, Stryker.NET over `Modules/Forecasting/Application/WriteBack/**` (x64 SDK, `run-backend-x64.ps1`),
compared with the same files' run on the pre-move revision: **no survivor or no-coverage mutant added**; the 80 % project
bar reported.

### D-13 Formal verification — RECOMMENDED: no Quint / TLA+ / Agda model; the law is executed exhaustively
The claims are structural relations over a finite closed set (compiled types × map × regex fields × literal list). The
round does have concurrency (an `AsyncLocal` context, last-execution-out flush), but its behaviour is unchanged (D-0) and
is already observed by the existing `TaskManager` / `ResponsiveDuringUpdates` / `CancelAllWhileWorkIsAdmitted` tests; no
new state, transition or temporal claim is made. PBT: N/A, closed finite domain. No proof obligation is declared, so no
checker install or waiver is needed.

## Wave: DESIGN / [REF] Slices
One nwdes feature `story-6657-write-back-round`, 3 slices, each ≤ 1 day, moves in commits apart from edits
(`refactor(forecasting): …`). Size **S-M, about 3 days**.

| # | Delivers | Oracle (native NUnit, `Lighthouse.Backend.Tests/Architecture/`) | Production targets | Rigor (proposed) | Est. |
|---|---|---|---|---|---|
| S1 | **First action: the three "merged" checks for #6365, #6366, #6468, then the DoR re-run at DELIVER start (M1-M17).** Rules see the move before it happens: membership measured from the IL; the law over the write-back map; `ModuleRules.TheWriteBackRound` (old ∪ new) read by the Access, Licensing and Promises predicates; the literal sweep with its fixed list | `WriteBackMembership.cs` + `WriteBackMembershipArchUnitTest.cs` (a listed type exists in its old **or** new namespace; RED on a missing name); **RED today**: the planted round type in a made-up `…Forecasting.Application.WriteBack` namespace reached by a made-up Promises type passes Promises' upstream rule (E2), GREEN once the predicate reads the pattern; `WriteBackStringsTheMoveRenames` sweep | none | `thorough` | 1 d |
| S2 | The round lives in `Modules/Forecasting/Application/WriteBack`; its unit tests in `Tests/Modules/Forecasting/Application/WriteBack`; `using`s in `Program.cs`, `UpdateQueueService`, 3 updaters, `ForecastUpdater`; every compared-both-ways list updated from the IL (E4); the E1/E6/E7 literals renamed and the sweep list emptied of them | S1's membership test tightened to accept **only the new namespace** — RED before the move, GREEN after; all existing rules green with the measured list changes | 8 files moved; 6 production files (`using` only) | `standard` | 1 d |
| S3 | Rules by namespace only: `TheWriteBackRound` narrowed to the new namespace and the three 8-name lists deleted; new rules R-WB1..R-WB4; `ForecastingUnitTestPlacementTest` and `PromisesUnitTestPlacementTest` (write-back exceptions and prefixes leave) updated; `ARCHITECTURE.md` §4 row 3 + Forecasting rules bullet, ADR-270 "Left open" answered + two edge rows + layout line, catalogue note | **RED oracle**: S1's sweep with an **empty** list and the round pattern holding **no** old alternative — no regex field or string literal in the test project may still match the 8 old full names or paths. The sweep excludes itself and its own fixtures by exact path (sweep file, `WriteBackMembership.cs`, its test, the map, the planted fixtures), and that exclusion list is asserted equal to exactly those paths. RED at S3's start, GREEN once narrowed. Alongside (green on writing): `WriteBackRoundBoundariesArchUnitTest.cs` (R-WB1..R-WB4), planted-type falsifiers in the `ModuleRulesCatchANewOffenderArchUnitTest` style | none (test side and docs) | `standard` | 1 d |

Commands (host runs; nothing run in DESIGN):
```
pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~Architecture"
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~WriteBack|FullyQualifiedName~TaskManager|FullyQualifiedName~AdoOAuthWriteConsent"
dotnet build Lighthouse.Backend                                            # 0 warnings
dotnet test Lighthouse.Backend --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"
```
Caution for the second filter: `JiraWriteBackTest` and `AzureDevOpsWriteBackTest` write to a real tracker in their
set-up. `~WriteBack` selects them, so that line must add `&TestCategory!=JiraIntegration&TestCategory!=AdoIntegration`
(and the crafter prefers `Test-Fast.ps1` for the fast loop, which already excludes them).

Driving port for every oracle: the compiled production assembly read through `IlDependencies` (in-process; nearest
executable argv: the `dotnet test … ~Architecture` line). Behaviour is observed through the unchanged write-back
acceptance tests at the application-service port and the host tests.

Rigor (CLAUDE.md profiles, proposed): S1 `thorough` (opus / sonnet: the law's "move into an existing area" case and the
silent-loss falsifier are where earlier reviews found misses); S2, S3 `standard` (sonnet / haiku). Open question 1.

## Wave: DESIGN / [REF] Ratchet design
### S1 widening (old ∪ new), S2 renames, S3 narrowing (new only)
| Owner | Constant / list | S1 | S2 | S3 |
|---|---|---|---|---|
| `ModuleRules` | `TheWriteBackRound` (new, full-name pattern) | old 8 names ∪ new namespace | — | new namespace only |
| `Access…`, `Licensing…`, `PromisesModuleBoundariesArchUnitTest` | `TheWriteBackRound` lists (8 each) | predicates read the pattern; lists kept, on the sweep list | — | deleted |
| `ForecastingModuleBoundariesArchUnitTest` | inbound list (1); licence edges (1) | — | measured: expected 5; 2 | — |
| `FlowMetricsInboundApplicationArchUnitTest` | R4 list (4) | — | entry deleted: 3 | — |
| `LicensingModuleBoundariesArchUnitTest` | R-L3 | — | entry renamed | — |
| `TrackerIntegrationModuleBoundariesArchUnitTest` | R-T4 | — | entry renamed (D-4) | — |
| `PlatformLaneBoundariesArchUnitTest` | `TheLanesEdgesIntoServices` (19) | — | 5 edges deleted: 14 | — |
| `PlatformLaneMembership`, `PlatformLaneUnitTestPlacementTest` | 8 + 8 "still in Services" entries | — | deleted | — |
| `PromisesUnitTestPlacementTest` | 8 exceptions, `WriteBack` / `IWriteBack` prefixes, newcomer case | — | exceptions deleted | prefixes and newcomer case leave (R-WB1 takes over) |
| `QuietWriteBackSeamArchUnitTest`, `RepositoryLeakRatchetArchUnitTest` | `HaveFullName` strings; path | — | `typeof(...)`; path renamed | — |

### New rules (S3), baselines from the IL
| # | Rule | Expected baseline (text; IL decides) |
|---|---|---|
| R-WB1 | Each listed round type lives in `Modules.Forecasting.Application.WriteBack`; no `WriteBack*` / `IWriteBack*` type under `Services` | membership map; a newcomer `Services/Interfaces/IWriteBackMadeUp` fails (moved from Promises' guard) |
| R-WB2 | Inside Forecasting, only the round reaches Tracker integration, and only `WriteBackService`, along a listed edge | `WriteBackService` → `IWorkTrackingConnectorFactory`, `IWriteBackTarget` (2) |
| R-WB3 | Promises, Access and Licensing reach no type of the round (by `ModuleRules.TheWriteBackRound`) | empty (existing R-P7 and the Access / Licensing outbound rules, now by namespace) |
| R-WB4 | Inside Forecasting, only `Adapters.Background.ForecastUpdater` and the round itself reach the round | 1 listed: `ForecastUpdater` |
Lists inline, by full type name, compared both ways, only shrink. `ModuleWording("the write-back round", "Forecasting's forecast updater, the update code and the update queue")`.

## Wave: DESIGN / [REF] Component decomposition
| Type(s) | From | To | Change | Slice |
|---|---|---|---|---|
| `WriteBackCollector`, `WriteBackRound`, `WriteBackRoundContext`, `WriteBackService`, `WriteBackTriggerService` | `Services/Implementation` | `Modules/Forecasting/Application/WriteBack` | MOVE | S2 |
| `IWriteBackCollector`, `IWriteBackService`, `IWriteBackTriggerService` | `Services/Interfaces` | `Modules/Forecasting/Application/WriteBack` | MOVE | S2 |
| `Models/WriteBack/*`, `IWriteBackTarget`, connectors, API helpers | — | — | UNCHANGED (D-3, S03a) | — |
| `UpdateQueueService`, `TeamUpdater`, `PortfolioUpdater`, `UpdateServiceBase`, `ForecastUpdater`, `Program.cs` | — | — | CHANGED (`using` only) | S2 |
| Round rules, membership, sweep, `ModuleRules.TheWriteBackRound` | `Tests/Architecture` | same | EXTEND / CREATE_NEW | S1, S3 |

## Wave: DESIGN / [REF] Driving and driven ports
Driving: unchanged — the update queue (`UpdateQueueService`) runs Team, Portfolio and forecast updates; `ForecastUpdater`,
`TeamUpdater`, `PortfolioUpdater` resolve write-back through `IWriteBackTriggerService` and stage through
`IWriteBackCollector`; the queue flushes through `IWriteBackService` when the round's last execution leaves. Driven:
`IWriteBackTarget` (Tracker integration, via `IWorkTrackingConnectorFactory`) → Jira / Azure DevOps connectors;
`ITeamMetricsService` (Flow metrics), `ILicenseService` (Licensing), `IBlackoutPeriodService` (set-up after S03i),
`IWorkItemRepository`, `IRepository<Feature>`, `IRepository<OAuthCredential>`, the clock. No new external call.

## Wave: DESIGN / [REF] Reuse analysis
| Existing | Overlap | Decision |
|---|---|---|
| `NamespaceMoveLaw` (S03g, extended by S03h/S03i) | proving rules see a move | **EXTEND** with the write-back map and, if missing, a "move into an existing area" declaration (M3) |
| `…StringsTheMoveRenames` / literal sweep (S03g/S03h) | old-namespace literals | **REUSE** shape: `WriteBackStringsTheMoveRenames` |
| `RefinementMembership` / `ReportsMembership` | namespace-agnostic membership | **REUSE** shape for `WriteBackMembership` |
| Three `TheWriteBackRound` lists (Access, Licensing, Promises) | three owners of "which types are the round" | **REPLACE** by one `ModuleRules.TheWriteBackRound` (semantic duplicate removed) |
| `ModuleRules`, `ModuleWording`, `ModuleRulesCatchANewOffenderArchUnitTest` | layer / edge rules with lists, falsifiers | **REUSE** for R-WB1..R-WB4 |
| `ForecastingModuleBoundariesArchUnitTest`, `ForecastingUnitTestPlacementTest` | Forecasting's rules | **EXTEND** (lists, sub-folder) |
| `WriteBackSinglePathArchUnitTest`, `WriteBackTargetCapabilityArchUnitTest`, `QuietWriteBackSeamArchUnitTest` | existing round guards | **REUSE** (`typeof`-keyed; the last one moves to `typeof`) |
| `architecture-rules`, `ef-model-unchanged`, `backend-tests`, `test-selection-show` | behaviour-free proof | **REUSE** |
No CREATE_NEW production type. Semantic-duplicate check: after S3 one owner answers "which types are the write-back
round" (`ModuleRules.TheWriteBackRound`); the membership map is its measured projection.

## Wave: DESIGN / [REF] Impact closure
| Locator | Kind | Decision | Reason |
|---|---|---|---|
| `Services/Implementation/WriteBack*.cs` (5), `Services/Interfaces/IWriteBack*.cs` (3) | production | MIGRATED | D-1, D-2 |
| `Program.cs` (1 `using` added) | composition root | CHANGED (`using` only) | E3 |
| `Platform/UpdateQueue/UpdateQueueService`, `Services/Implementation/BackgroundServices/Update/{TeamUpdater, PortfolioUpdater, UpdateServiceBase}`, `Modules/Forecasting/Adapters/Background/ForecastUpdater` | consumers | CHANGED (`using`s) | E8 |
| `Platform/UpdateQueue/UpdateCancellationContext.cs:5` | prose | UNCHANGED_COMPATIBLE (short name, still true) | |
| 8 unit-test files | tests | MIGRATED | D-6 |
| ~18 acceptance / host / container test files, `TestHelpers/UpdateServiceTestBase` | tests | CHANGED (`using`s) | |
| 11 rule files of E1, `ModuleRules`, `ForecastingModuleBoundariesArchUnitTest`, `ForecastingUnitTestPlacementTest` | architecture tests | CHANGED | S1-S3 |
| Log categories of 3 classes | runtime log | CHANGED (namespace) | E5; no override or harness reads them; open question 3 |
| `Models/WriteBack`, `Data`, migrations, frontend, E2E, Lighthouse-Clients, `Scripts/*`, suppressions, Stryker configs | — | UNCHANGED | D-0, D-3, E5 |
| `ARCHITECTURE.md` §4 row 3 (add "`Application.WriteBack` (the write-back round: what is written back to a tracker and when, staged per refresh round)"), Forecasting rules bullet (inbound count, licence edges, Tracker integration edge, R-WB*), Metrics bullet ("4 listed" → 3), §3 namespace list; ADR-270 "Left open" write-back → "*Answered (Story 6657)*: Forecasting `Application/WriteBack`", "Every edge" + rows **Tracker integration → Forecasting** (customer of the write-back capability, 1 listed type) and **Forecasting → Platform lane** (the queue opens and flushes the round; listed in Forecasting's inbound list; inversion is an S09a note), "Forecasting today" layout line; catalogue note (S09a: the round's inbound calls are on its starting list); slice-06 brief already dropped `WriteBack*` | docs | CHANGED in S3 | context-map rule |
| ADRs 142-145, 178, 192, 195, `brief.md` lines 1360, 1964, 1989 | docs | UNCHANGED: history (dated records); ADR-270 and `ARCHITECTURE.md` §4 are the current authority | |

Agreement analysis: no shared wire / schema / config contract changes (D-0); `not-applicable` for agreement and contract
tests (single deployable, same commit; the tracker sees the same writes). Architectural tests: applicable (R-WB1..R-WB4,
the law, `TheWriteBackRound`). Prefactoring: S1 is the prefactoring (rules widened, production untouched; the existing
suite GREEN before and after S1, then S2 moves).

## Wave: DESIGN / [REF] Constructive chain and residuality
- Chain: compiled assembly (input) → `WriteBackMembership` + law + `TheWriteBackRound` (producer) → module rules
  (consumer) → `dotnet test … ~Architecture` exit and message (public observation). Falsifiers: a planted Promises type
  reaching a made-up round type in the new namespace (S1 RED); a planted `Services/Interfaces/IWriteBackMadeUp` (R-WB1);
  a second Forecasting type reaching `IWriteBackTarget` (R-WB2). DESIGN `PASS`, runtime `DESIGNED_NOT_BUILT`, proof N/A
  (D-13).
- Stressors (negated assumptions): (1) "a pattern sees the round" — false, it has no namespace (E1) → literal sweep with
  fixed list; (2) "the module rules cover what the lists did" — false for Promises, which may call Forecasting (E2) →
  one round pattern; (3) "`HaveFullName` fails on an empty set" — unknown (E6) → `typeof`; (4) "the sibling Stories left
  the counts as measured" — false by construction → M1-M17; (5) "the law allows a move into an existing area" —
  unknown until S03g/S03i land (M3) → S1 adds the declaration if missing; (6) "a log override follows the move" — none
  today (E5), re-grepped.
  Walked afterwards with a second set (a host-test row naming a moved class, M13; `WriteBackValueSource` ordinals shifting
  — D-3 keeps the enum untouched; the queue flushing a round from another scope after the move — DI lifetimes and the
  singleton `WriteBackRoundContext` unchanged, D-0; ADR-270 drawing Forecasting → Tracker integration as new coupling — it
  existed, listed under `Services`): all survive. Walkthrough, not execution.
- Residue: the platform → Forecasting edge (D-7) and the update-code → Forecasting edges (D-8) remain, listed, owners
  S09a and Epic 6474 slice 12.

## Wave: DESIGN / [REF] Context-map placement
- Contexts touched: **Forecasting** (gains the write-back round). Edges: Tracker integration → Forecasting (new row; the
  edge existed from `Services`, now from Forecasting, one listed type); Flow metrics → Forecasting (legal, the R4 entry
  leaves); Licensing → Forecasting (listed edge, renamed); Organisation set-up → Forecasting (legal after S03i D-8); Work
  Items → Forecasting (the Team pass reads Work Items' fields, legal). Platform lane → Forecasting (listed).
- New terms: **write-back round** — "the per-refresh staging of tracker field updates, written once per connection when
  the round's last execution leaves" — added to Forecasting's glossary in ADR-270 if S3 finds it absent; the field-level
  terms (mapping, value source, write consent) stay with Tracker integration and the kernel. New concepts: none.
- Domain events: none published or reacted to. Rejected: Promises / Work Items reacting to "forecasts updated" to write
  back (changes the one-write-per-round behaviour).
- UI surface: none.
- ADR-270 / `ARCHITECTURE.md` §4 updates: S3 tasks in Impact closure.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)
- [ ] #6365, #6366, #6468 merged by the three checks; DoR re-run at DELIVER start (M1-M17) recorded in S1.
- [ ] Behaviour-free: writes, schedule, quiet / suppression / licence / consent behaviour, DI lifetimes unchanged; frontend and E2E untouched.
- [ ] No migration, `Data` or `Models/WriteBack` file changes.
- [ ] Moves in `refactor(forecasting): move …` commits apart from edits.
- [ ] `Program.cs`: only one `using` line added.
- [ ] Strings, suppressions, log overrides and CI targets re-grepped (M16).
- [ ] Three `TheWriteBackRound` lists replaced by `ModuleRules.TheWriteBackRound`; S1 RED observed on the Promises falsifier.
- [ ] Live-connector classes excluded from every filtered run (`JiraWriteBackTest`, `AzureDevOpsWriteBackTest`).
- [ ] Architecture docs updated in S3; finalize checklist run before the push.

## Wave: DEVOPS
- **Platform lens**: none — no deployment, configuration, persistence, pipeline or runtime change; one push runs the live
  connector suites once because `Program.cs` changes (D-10). Three log categories change namespace; no shipped override
  or harness reads them (E5).
- **Usage data**: **N/A, because** the Story changes no behaviour a user can use. Write-back has no usage-data event
  today, and adding one would measure an existing feature, which is not this Story's outcome; nothing new exists to count.
- KPI: KPI 1 of the catalogue (files under `Services`) drops by 8; S03d's last named exception closes. Measured at S3.

## Open questions (default taken)
1. Rigor: S1 `thorough` (opus / sonnet), S2 and S3 `standard` (sonnet / haiku)? **Default: yes.**
2. Owner Forecasting, namespace `Modules.Forecasting.Application.WriteBack` (D-1), rather than flat `Application`?
   **Default: the `WriteBack` sub-namespace.**
3. The log categories of `WriteBackService`, `WriteBackTriggerService` and `WriteBackCollector` become
   `Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.*`. An administrator who filters logs by the old category
   sees no lines under it. **Default: accept, as S03f-S03i did; no release-note line (no shipped setting names them).**
4. The edge to Tracker integration stays one listed type (`WriteBackService`) rather than making Forecasting a legal
   caller by namespace (D-4)? **Default: listed.**
5. Leave the platform → Forecasting edge listed and note its inversion for S09a, rather than inverting it now (D-7)?
   **Default: listed; S09a note.**
6. `RoundWriteBackDuringDatabaseMaintenanceTest` stays with the update code (its subject is the queue and the gate)?
   **Default: S1 decides by what it constructs; expected stay.**
7. Order 6365 → 6366 → 6468 → 6657 → 6382 confirmed (D-9). **Default: yes; no hard reason against; 6657 must precede S09a.**

## Architecture summary for the maintainer
**Shape (hexagonal).** Nothing new is built; 8 files move into Forecasting and rules are restated.
```
 Driving (unchanged)                    Forecasting Application/WriteBack (MOVED)        Driven (unchanged)
 UpdateQueueService (platform) ─┐   IWriteBackTriggerService → WriteBackTriggerService ─┬─ ITeamMetricsService (Flow metrics)
 ForecastUpdater (Forecasting)  ─┼─► IWriteBackCollector → WriteBackCollector           ├─ ILicenseService (Licensing)
 Team/PortfolioUpdater,          │   WriteBackRound, WriteBackRoundContext (staging)     ├─ IBlackoutPeriodService (set-up)
 UpdateServiceBase (update code)─┘   IWriteBackService → WriteBackService ───────────────┼─ IWorkTrackingConnectorFactory /
                                                                                         │  IWriteBackTarget (Tracker integration)
                                     Models/WriteBack (kernel, UNCHANGED)                └─ repositories, clock
```
- Driving adapters: the update queue and the three updaters plus the forecast updater — unchanged, one `using` each.
  Application services and ports: the round moves into `Modules/Forecasting/Application/WriteBack`, ports beside their
  implementations. Domain: none (orchestration, not forecasting law). Driven adapters: the Jira and Azure DevOps
  write-back targets stay in Tracker integration; repositories stay; no new external call.

**Patterns weighed.**
- Rules first, then move (chosen): the move would silently free Promises to join the round (E2); the rules must see it first.
- One named pattern for the round (chosen) vs three hand-kept 8-name lists (rejected: three owners of one fact).
- Forecasting as owner (chosen): the one context legally downstream of every value the round writes. Tracker integration
  (rejected: the ACL would reach Flow metrics and Licensing), Promises (rejected: reverses Forecasting → Promises), Work Items
  (rejected: may not reach Flow metrics), staying in `Services` (rejected: S09a would inherit it).
- Splitting the staging into the platform lane (rejected): staging knows connections and fields, which is context knowledge.
- Dependency inversion of the queue's flush (deferred to S09a): an edit with concurrency risk; listed edge for now.
- Domain events instead of a direct trigger (rejected): would change the one-write-per-round behaviour.
- Listed edge to the ACL (chosen) vs legal-by-namespace (rejected): keeps "one service sends" visible.
- Single responsibility: resolve (trigger), stage (collector / round), send (service) stay three ports, as ADR-144 set them.
