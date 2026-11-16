# Feature: story-6376-s06-connector-capability-interfaces

Story #6376 · S06 Connector capability interfaces · Epic #6508 (Thin services and controllers)

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md), Stories → S06
- Brief: [`slices/slice-20-s06-connector-capability-interfaces.md`](../epic-6264-modular-monolith/slices/slice-20-s06-connector-capability-interfaces.md)
- ADRs: [ADR-270](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md) (Tracker integration is the ACL; its glossary), [ADR-139](../../product/architecture/adr-139-incremental-sync-capability-probe-on-connector-port.md) (incremental-sync probe on the connector port; Jira Cloud vs Data Center)
- Why now: the maintainer put S06 before S03a #6359 on 2026-11-16, so S03a packages the final connector port once. Order: S06 → S03a, S03d, S03e, S03f, S03g, S03h, S03i → S09a → Epic #6474 (see the end of [`epic-6474-one-structure/feature-delta.md`](../epic-6474-one-structure/feature-delta.md)).

DISCUSS: covered at Epic level (see the Epic's Wave Decisions); waves start at DESIGN.

## Wave: DESIGN / [REF] Inherited

Linked, not copied. The catalogue is the single source of truth.

- Catalogue: Story Map (G2: "can run before or alongside packaging"), How to pick up a Story, Stories → S06 and S03a,
  Outcome KPI 7 (connector `NotSupportedException` throws, target 0), Wave Decision D-6264-10 (the
  `GetFeaturesForProject` rename belongs here), Cross-Cutting Impact Checklist, Improvement log.
- ADR-270: Tracker integration context, its glossary (Connection, Translation, Read refusal), the edge "Tracker
  integration → Work Items: the connector port is the negotiated contract".
- ADR-139: incremental sync as a per-connection runtime probe (`SupportsIncrementalSync`), Jira's
  `DeploymentNotKnownYet`.
- ADR-268 (ratchet convention: inline file lists, never loosened).
- S03b evolution `docs/evolution/2026-11-16-story-6360-s03b-package-work-items/` (the connector leaf rule; the source
  selectors stay with Tracker integration).

## Wave: DESIGN / [REF] DoR re-run (2026-11-16)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | `IWorkTrackingConnector` has 16 members; CSV, Linear and ServiceNow throw `NotSupportedException` from 20 of them; callers ask `Supports*` flags instead of the type. |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real throws and flags below. |
| 4 UAT | PASS (catalogue) | Four AC in the brief, CI-observable. |
| 5 AC from UAT | PASS | AC map to the Elevator Pitch "After" (`grep` finds no throw from an implemented member; callers test `connector is …`). |
| 6 Right-sized | TO CONFIRM at DESIGN | Brief says L; the slicing ("one capability per slice") decides whether it fits 5 nwdes slices. |
| 7 Technical notes | PASS | Re-answered below. |
| 8 Dependencies | PASS | Soft: S01 Closed. No hard dependency. Epic #5687 (Faster Updates) is **Closed**, so the brief's "coordinate with its open slices" no longer applies (see re-count). |
| 9 Outcome KPIs | PASS | KPI 7. |

## Wave: DESIGN / [REF] Baseline re-count (2026-11-16, `main` at `487be05c4`)

| Measure | Catalogue / brief | Today |
|---|---|---|
| `IWorkTrackingConnector` members | 15 | **16** (2 `Supports*` probes, `GetPredefinedAdditionalFields`, 2 `GetWorkItemsForTeam`, `SweepWorkItemsForTeam`, 2 `GetFeaturesForProject`, `SweepFeaturesForPortfolio`, `GetParentFeaturesDetails`, `SweepParentFeatures`, 3 `Validate*`, `WriteFieldsToWorkItems`) |
| Member throws, CSV / Linear / ServiceNow (KPI 7) | 20 (6 / 6 / 8) | **20 (6 / 6 / 8)** |
| Other `NotSupportedException` in the connector folder | — | 4, all genuine: Jira `DeploymentNotKnownYet` (ADR-139 runtime case), one in Azure DevOps (`AzureDevOpsWorkTrackingConnector.cs:1804`, to classify), `AuthenticationMethodKeys`, `AuthenticationMethodSchema` (no auth method for an unknown system) |
| `Supports*` on the connector port | — | `SupportsTransitionHistory`, `SupportsIncrementalSync`; one caller each outside the connectors: `Modules/WorkItems/Application/WorkItemService.cs` |
| Other `Supports*` flags | — | `SupportsAdditionalFields`, `SupportsWriteBackMappings` (API helpers and the two connection controllers), `SupportsDeliveryForecastPublishing` (`IDeliveryForecastPublisher`, Jira, `DeliveryForecastPublishingService`) |
| Capability interfaces that exist already | — | `IDeliveryForecastPublisher`, `IDeliverySourceProvider`, `IBoardInformationProvider`, plus per-system `IJira…`, `IAzureDevOps…`, `ILinear…`, `IServiceNow…WorkTrackingConnector` |
| Files referencing `IWorkTrackingConnector` | — | 9 production, 40 test |
| `GetFeaturesForProject` references | — | 19 |
| Epic #5687 | open slices | **Closed**. The Linear and ServiceNow throws still say "pending Epic #5687": that text is stale |

## Wave: DESIGN / [REF] Questions DESIGN must answer

1. **The capability set**: which interfaces, by which glossary names (ADR-270 Tracker integration), and which
   connector implements which. Reuse the existing `IDeliveryForecastPublisher` / `IBoardInformationProvider` pattern.
2. **Static vs runtime**: incremental sync is per connection (ADR-139, Jira Cloud vs Data Center). Which capabilities
   are static per type and which stay runtime probes; is the hypothesis ("only Jira's deployment is runtime") still
   true?
3. **ServiceNow's work-item reading** (`WorkItemReadingUnavailable…`, lines 760-771): a missing capability or a
   per-record-class runtime case?
4. **The other `Supports*` flags** (additional fields, write-back mappings, delivery publishing): in scope or not.
5. **The caller side**: `WorkItemService`, the write-back service, validation callers, the factory's return type.
6. **The `GetFeaturesForProject` rename** (D-6264-10) and the stale "pending Epic #5687" messages.
7. **The ratchet**: KPI 7 as a test (no `NotSupportedException` from a member of an implemented interface), by
   inline file list.
8. **Behaviour-free proof**: the same sync path is chosen per connector; the live connector categories run
   deliberately (Linear key shared with CI).
9. **What S03a then moves**: the final port, so S03a's leaf rule and S09a's contract can be written against it.
10. **Size and split** into at most 5 nwdes slices.

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels and topologies unchanged; existing API, E2E and response tests green unmodified, apart from tests that move with their code. The same sync path per connector and connection.
- [ ] **Ratchet in the same change**: KPI 7 rule lands in the commit range that makes it pass; inline lists; no rule loosened.
- [ ] **Moves separate from edits**: renames (`GetFeaturesForProject`) in their own `refactor(connectors): rename …` commits.
- [ ] **Re-count before designing**: done above.
- [ ] **Mutation last, on frozen code**: no survivor or no-coverage mutant added in the touched code.
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §4 Tracker integration row; ADR-139 or a new ADR for the Cloud/DC mapping; ADR-270 glossary for new capability names.
- [ ] **One context at a time**: the connectors stay in `Services/*/WorkTrackingConnectors`; packaging is S03a.
- [ ] **Terminology**: ADR-270 glossary names; configurable terms not hard-coded.
- [ ] **Cross-cutting re-check**: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, each answered, no silent N/A.
- [ ] **Live connector categories run deliberately** before push (connector code changes), watching the Linear rate limit.
- [ ] **The connector leaf rule holds**: no connector reaches `Modules.WorkItems`.

## Wave: DESIGN / [REF] Context-map placement (2026-11-16)

- **Context touched**: Tracker integration only (ADR-270): its port, the connector port, is reshaped. The connectors
  stay in `Services/*/WorkTrackingConnectors`; packaging into `Modules/TrackerIntegration` is S03a.
- **Neighbours whose edge becomes visible** (no behaviour change): Work Items (the customer of the port:
  `WorkItemService` asks for the capability by type instead of a flag), the write-back flow (`WriteBackService`, today
  loose in `Services/Implementation`), Promises (`DeliverySourceResolver`, `DeliveryForecastPublishingService`,
  `DeliverySourcesController` already ask by type; unchanged), Organisation set-up / API (validation callers;
  unchanged).
- **New terms / concepts**: one glossary term, **Capability** (what a connector can do for a connection), and four
  code names under it (D-1). The **Project → Portfolio** drift on the connector port is fixed (D-6).
- **Domain events**: none published, none reacted to. The port is a synchronous query/command port; ADR-270 keeps it
  the negotiated contract on the Tracker integration → Work Items edge.
- **UI surface**: none.
- **ADR-270 / `ARCHITECTURE.md` §4 edits**: "SSOT edits pending" below; written in the finalize checklist.
- **Improvement backlog check**: S06 is itself an improvement item (#6508); N/A, stated.

## Wave: DESIGN / [REF] Evidence from the design-time measurements (2026-11-16, `main` at `2bfe80bc4`)

Text searches over the production and test source (`grep -rn --include='*.cs'`, `bin/` and `obj/` excluded) and
reads of the named files. No build or IL scan was needed for these facts. Jev not used (`"jev": "unauthorized"`).

| # | Command (from `Lighthouse.Backend/`) | Result | Consequence |
|---|---|---|---|
| E1 | `grep -rn NotSupportedException Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors` | **24** lines: CSV 6, Linear 6, ServiceNow 8 (all `throw` from an interface member), Jira 1 (`:167`, inside the private `SweepIdentities`), Azure DevOps 1 (`:1804`, inside the private static `ConvertAuthorizationHeaderToVssCredentials`), `AuthenticationMethodKeys` 1, `AuthenticationMethodSchema` 1 | KPI 7 baseline confirmed (20). The ADO throw is **not** an interface member: an auth strategy produced a scheme the connector cannot turn into credentials (genuine, configuration error). The two auth-method static classes are not connectors. |
| E2 | Read the 20 throwing members | CSV/Linear/ServiceNow: `SweepWorkItemsForTeam`, `GetWorkItemsForTeam(ids)`, `SweepFeaturesForPortfolio`, `GetFeaturesForProject(ids)`, `SweepParentFeatures` (5 each = 15), `WriteFieldsToWorkItems` (3), ServiceNow `GetFeaturesForProject(portfolio)`, `GetParentFeaturesDetails` (2) | Three missing capabilities: incremental sync (15), write-back (3), Portfolio Feature reading (2). |
| E3 | Read ServiceNow `:30-37`, `:758-775`, `:962-977` | `WorkItemReadingUnavailableMessage` says reading **portfolio** work is not supported "and is declined rather than half-built"; `ValidatePortfolioSettings` refuses every Portfolio with `portfolio_not_supported`, independent of record class; team reads work for every record class (`ServiceNowReadScope` picks the table per Work Item type) | Question 3: a **static missing capability**, not a per-record-class runtime case (D-3). The constant's name is misleading (it is about Feature reading); it disappears with the members. |
| E4 | Read Jira `:131-170`, `:3290-3310` | `SupportsIncrementalSync` = deployment known (`SearchTransportOf(connection) is not Unknown`): the Atlassian gateway is always Cloud; anything else is known only after an earlier `GetDeploymentType` filled a **process-wide cache**. Cloud **and** Data Center sweep today | **Contradicts ADR-139 and the brief**: Cloud vs Data Center no longer answer differently. The runtime case is "deployment not yet known since this process started". |
| E5 | `grep -rn "Supports(IncrementalSync\|TransitionHistory\|AdditionalFields\|WriteBackMappings\|DeliveryForecastPublishing)"` outside the connectors | `SupportsIncrementalSync` 3× in `WorkItemService` (`:327`, `:1139`, `:1423`); `SupportsTransitionHistory` 1× (`:573`); `SupportsAdditionalFields`/`SupportsWriteBackMappings` are **licence** extension methods on definition lists (`API/Helpers`), used 6× in the two connection controllers; `SupportsDeliveryForecastPublishing` 1× (`DeliveryForecastPublishingService:96`), Jira returns constant `true` | Question 4 (D-4). |
| E6 | Read the `SupportsTransitionHistory` implementations | CSV: a state-entered column is configured; ServiceNow: history availability was **observed** on this instance; Jira, ADO, Linear: `true` | Also per connection at runtime, but it gates no member and nothing throws: it is a data-quality probe, not a capability (D-2). The brief's hypothesis "only Jira's deployment is runtime" holds for member capabilities only. |
| E7 | `grep -rln "IWorkTrackingConnectorFactory\b"` (production) | 12 files: factory pair, `Program.cs` (registration only), `WorkItemService`, `WriteBackService`, `ConnectionHealthService`, `DeliverySourceResolver`, `DeliveryForecastPublishingService`, `DeliverySourcesController`, `PortfoliosController`, `TeamsController`, the two connection controllers | Caller map in Reuse Analysis. Factory returns `IWorkTrackingConnector`; three callers already type-test (`is IDeliverySourceProvider`, `is IDeliveryForecastPublisher`, `as IDeliverySourceProvider`). |
| E8 | `WriteBackService.cs:17-67,145-160`; `UpdateServiceBase.cs:189,202` | A connector throw inside `WriteUpdates` is caught and becomes a failed `WriteBackItemResult` per update, `ErrorMessage = ex.Message`, which is **only logged** (`WriteBackCollector`). A refresh that fails with a `NotSupportedException` records `Success = false` with **no** reason; only `WorkTrackingRefusedException`/`WorkTrackingReadException` messages reach a user | The absent-capability sentence is log-only on both paths (D-5). |
| E9 | `grep -rn --include='*.cs' GetFeaturesForProject` | **19** production references in 7 files (fact confirmed); **391** test references in **47** test files; 42 hits in the frontend are a different, frontend name (FE6) | The rename (D-6) is mechanical but touches 47 test files: its own slice and commit. |
| E10 | `grep -rn "Mock<IWorkTrackingConnector>" Lighthouse.Backend.Tests` | **55** sites in **25** files; 11 of those files set up a sweep, by-id fetch, probe or write member, 12 set up `GetFeaturesForProject`/`GetParentFeaturesDetails` | These mocks move their set-ups to `.As<Capability>()`. A set-up left on the base mock no longer compiles, so no test can silently switch path (D-10). |
| E11 | `grep -rn NotSupportedException Lighthouse.Backend.Tests` for connector members | 3 test methods assert a throw of a member that disappears: `ServiceNowWorkTrackingConnectorTest` `:163-166` (Feature reads), `:176` (write-back), `ConnectorsWithoutDependenciesTest:45`. `JiraIncrementalSyncTest:287` asserts Jira's throw, which stays | These 3 are replaced, not kept unmodified (D-10, open question 3). |
| E12 | `TestHostUseGuardTest*` for the 16 touched test classes | only `Slice00OneForecastPerBatchScenarios` is named there | "Improve the tests you touch" reaches one host class (D-10). |
| E13 | `IBoardInformationProvider.cs` | the port interface takes `Board`/`BoardInformation` from `Services.Implementation.WorkTrackingConnectors.Boards` | A port type depends on an implementation namespace: S09a's contract inherits it. Out of S06, recorded for S03a/S09a (D-8). |
| E14 | `Factories/WorkTrackingConnectorFactory.cs` | `_ => throw new NotSupportedException()` for an unknown system; the factory implements `IWorkTrackingConnectorFactory`, outside the port namespace | Not a connector; outside KPI 7 and outside the ratchet's population. |
| E15 | `Program.cs:1491-1495` | markers `IJira…`, `IAzureDevOps…`, `ILinear…`, `IServiceNow…WorkTrackingConnector` registered; CSV as its class | Registrations unchanged; the markers gain the capabilities in their base list. `Program.cs` is **not** edited. |
| E16 | `git ls-tree origin/main docs/product/architecture` | highest ADR **284** | A new ADR is **ADR-285** (D-9). |

## Wave: DESIGN / [REF] Decisions

Status: **all RECOMMENDED, none decided**. Paradigm: **object_oriented** (CLAUDE.md); the decision "can this
connection do X" has one owner, the connector: its type says what it can ever do, and a probe on that same capability
says what it can do for this connection now.

### D-0 Behaviour-free scope — RECOMMENDED

No schema, route, DTO, serialized response, UI, event or registration change. The same sync path (full or sweep) is
chosen per connector and connection; the same validation verdicts; the same write-back results apart from the
log-only sentence of D-5. `Program.cs` is not edited.

### D-1 The capability set — RECOMMENDED: a base port plus four capability interfaces, incremental sync split by level

Same namespace and folder as today (`Services/Interfaces/WorkTrackingConnectors`), one file per interface.
`Feature`, `Work Item`, `Portfolio`, write-back are ADR-270 glossary words.

| Interface | Members (after D-6) | Implemented by |
|---|---|---|
| `IWorkTrackingConnector` (kept, reduced) | `SupportsTransitionHistory(connection)`, `GetPredefinedAdditionalFields(connection)`, `GetWorkItemsForTeam(team, ct)`, `ValidateConnection`, `ValidateTeamSettings`, `ValidatePortfolioSettings` | all five |
| `IFeatureSource` (new) | `GetFeaturesForPortfolio(portfolio, ct)`, `GetParentFeaturesDetails(portfolio, ids, ct)` | Jira, Azure DevOps, Linear, CSV |
| `IIncrementalWorkItemSource` (new) | `SupportsIncrementalSync(connection)`, `SweepWorkItemsForTeam(team, ct)`, `GetWorkItemsForTeam(team, ids, ct)` | Jira, Azure DevOps |
| `IIncrementalFeatureSource : IFeatureSource` (new) | `SupportsIncrementalSync(connection)`, `SweepFeaturesForPortfolio(portfolio, ct)`, `GetFeaturesForPortfolio(portfolio, ids, ct)`, `SweepParentFeatures(portfolio, ids, ct)` | Jira, Azure DevOps |
| `IWriteBackTarget` (new) | `WriteFieldsToWorkItems(connection, updates)` | Jira, Azure DevOps |
| `IBoardInformationProvider`, `IDeliverySourceProvider`, `IDeliveryForecastPublisher` | unchanged | unchanged |

16 members before, 16 after (the probe is declared on both incremental interfaces; one public method satisfies both
in C#). The per-system markers list their capabilities: `IJira…` and `IAzureDevOps…` gain all four; `ILinear…`
gains `IFeatureSource`; `IServiceNow…` gains none; `CsvWorkTrackingConnector` declares `IFeatureSource` itself.

| Option | What | Why not / why |
|---|---|---|
| a **(recommended)** | Base + `IFeatureSource` + incremental by level + `IWriteBackTarget` | Each interface is a thing some connector cannot do. The level split costs one interface and lets a connector sweep its Teams' Work Items without Features: exactly ServiceNow's case (its team sweep was planned, its Features never are). |
| b | One `IIncrementalSource` with all five incremental members | One interface fewer, 5 slices in one nwdes feature. Rejected: ServiceNow could never offer a team sweep without throwing for the Feature half, which is the smell this Story removes; splitting later changes a port S09a will already have published. |
| c | Also split the base by client (`IConnectionValidator`, `IWorkItemSource`) | All five connectors implement every base member with a real answer, so no throw and no KPI gain; it would move 6 more members and every validation caller for interface segregation alone. Left to S03a/S09a if the contract wants it. |
| d | Rename `IWorkTrackingConnector` | 9 production and 40 test files for no glossary gain ("connector" is ADR-270's word). |

### D-2 Static versus runtime — RECOMMENDED: the type says "ever", a probe on the capability says "for this connection now"

- Write-back, Feature reading and incremental sync are **static per connector type** for every member that throws
  today (E2, E3).
- Incremental sync keeps its **per-connection probe**, moved from the base port onto the two incremental interfaces.
  Jira implements both and answers `false` while its deployment is not yet known in this process (E4); its sweep's
  `NotSupportedException` stays as the assertion that the caller honoured the probe, and is on the ratchet's list
  with that reason (D-7). Azure DevOps answers `true`.
- `SupportsTransitionHistory` **stays on the base port**: per connection for CSV and ServiceNow (E6), but it gates no
  member, so it is a fact about the data, not a capability.
- The hypothesis is **partly disproved** as written: the runtime case is not Cloud versus Data Center any more but
  "deployment not yet known" (E4). The rule the brief asks for still holds: runtime variance extends the probe, not
  the type split.

### D-3 ServiceNow's Feature reading — RECOMMENDED: a missing capability

E3: the refusal covers every Portfolio and every record class, and validation already refuses the Portfolio. ServiceNow
does not implement `IFeatureSource`; `ValidatePortfolioSettings` keeps its `portfolio_not_supported` verdict (a
verdict is an answer, not a throw). The misnamed `WorkItemReadingUnavailableMessage` goes with its members.

### D-4 The other `Supports*` flags — RECOMMENDED: out of scope

- `SupportsAdditionalFields`, `SupportsWriteBackMappings`: licence checks on the connection's definition lists (E5),
  not connector capabilities. Out; their name overlap is noted for the reader.
- `SupportsDeliveryForecastPublishing`: already on a capability interface; Jira's answer is constant `true`, and the
  interface's own comment keeps it per connection by design. Out; listed as a later simplification, not KPI 7.

### D-5 The caller side — RECOMMENDED: type tests at the caller, factory unchanged, absence fails where the throw was

| Caller | Today | After |
|---|---|---|
| Factory `GetWorkTrackingConnector` | returns `IWorkTrackingConnector` | **unchanged** (no generic `Get<T>`: the three Promises callers already type-test, and the same idiom serves the new ones) |
| `WorkItemService` team scan | `if (!connector.SupportsIncrementalSync(...))` | `connector is IIncrementalWorkItemSource source && source.SupportsIncrementalSync(...)`; otherwise the same `IdentityScan(false, false, [])` |
| `WorkItemService` Feature and parent scans | same flag | `is IIncrementalFeatureSource` plus its probe; same fallback |
| `WorkItemService` Portfolio Feature read | `connector.GetFeaturesForProject(...)` throws for ServiceNow | resolved once per Portfolio refresh as `IFeatureSource`; when absent, a `NotSupportedException` is thrown **at the same point** (before any write), its sentence naming the connection's work tracking system. Same refresh outcome (`Success = false`, no reason, E8) |
| `WriteBackService.WriteUpdates` | `connector.WriteFieldsToWorkItems` throws for CSV, Linear, ServiceNow | `connector is IWriteBackTarget target`; when absent, throw `NotSupportedException("Write-back is not supported for {system}.")` inside the same `try`, so each update fails exactly as today. Byte-identical for CSV and Linear; ServiceNow's longer sentence becomes the generic one, in the log only (E8) |
| Validation callers (2 controllers, `TeamsController`, `PortfoliosController`, `ConnectionHealthService`), `WorkTrackingSystemConnectionController` (predefined fields) | base members | **unchanged** |
| `DeliverySourceResolver`, `DeliverySourcesController`, `DeliveryForecastPublishingService` | type tests | **unchanged** |

A caller that needs two capabilities (the incremental Portfolio path needs the full read and the sweep) asks once:
`IIncrementalFeatureSource : IFeatureSource`.

### D-6 Rename and stale text — RECOMMENDED

`GetFeaturesForProject` → **`GetFeaturesForPortfolio`** (both overloads), and the `Portfolio project` parameters on the
port and its implementations → `portfolio` (Sonar S927 wants implementation parameter names to match). Internal only,
no wire change. Its own commit `refactor(connectors): rename GetFeaturesForProject to GetFeaturesForPortfolio`, before
any split. The stale "pending Epic #5687" sentences are deleted with their members. The port's doc comments that cite
internal ids (`Epic #5687, DDD-1`, `D1`, `D2`, `DDD-2`, `ADR-183`) are rewritten as plain reasons in the edit commits
that touch them (CLAUDE.md comment rule).

### D-7 The ratchet — RECOMMENDED: from the compiled IL, every method of a connector type, inline list

See "Ratchet design". The rule is stronger than KPI 7's text: a throw moved into a private helper of a connector is
still caught.

### D-8 What S03a moves and S09a exposes — RECOMMENDED

The final port = `IWorkTrackingConnector`, the four new interfaces, the three existing capability interfaces, the
markers, `IWorkTrackingConnectorFactory`, `RemoteRecordStamp` and the read-refusal exceptions. All new signatures use
only kernel types (`Models`: `Team`, `Portfolio`, `WorkItem`, `Feature`, `WorkTrackingSystemConnection`,
`RemoteRecordStamp`, `Models.WriteBack`, `Models.Validation`), so S03a moves the folder as is and S09a can publish it.
Two leaks predate S06 and stay: `IBoardInformationProvider` takes `Board`/`BoardInformation` from the implementation
namespace (E13); the factory interface sits in `Lighthouse.Backend.Services.Factories` while its class sits in
`Lighthouse.Backend.Factories`. Both recorded for S03a/S09a, not fixed here.

### D-9 Architecture record — RECOMMENDED: new ADR-285 amending ADR-139

ADR-139 rejected "a separate opt-in interface" because a type cannot vary per connection. ADR-285 keeps that reason and
reverses the conclusion: the type expresses "never", the probe on the capability expresses "not for this connection
now". ADR-285 also corrects ADR-139's Cloud/Data Center statement (E4) and documents the capability matrix. ADR-139
gets "Amended by ADR-285" in its status line. Option "edit ADR-139 in place": rejected, it would rewrite an accepted
decision's record of a rejected alternative.

### D-10 Test migration — RECOMMENDED

1. Mocks: set-ups of moved members go to `mock.As<Capability>()`, called before `.Object` is first read (Moq).
   Compile errors find every one (E10).
2. The 3 tests asserting a removed throw (E11) are deleted in the slice that removes the member; their claim ("says
   unsupported rather than returning nothing") becomes the type fact the slice's oracle pins.
3. `GetFeaturesForProject` references in 47 test files change in the rename commit only.
4. "Improve the tests you touch": waived for edits that are only `.As<>()`, a rename or a deleted assertion of a removed
   member (same waiver as S03b/S03c for `using`-only edits). `Slice00OneForecastPerBatchScenarios` (on the host list)
   gets only such an edit.
5. Unit-test placement: unchanged (connector tests stay under `Tests/Services/Implementation/WorkTrackingConnectors`
   until S03a).

### D-11 Behaviour-free proof and EXAMINE — RECOMMENDED

1. Existing tests green, unmodified apart from D-10.
2. Each capability slice's oracle pins which connector types offer the capability (and, for incremental sync, the
   probe's answer for a reached and an unreached Jira connection, Azure DevOps, and the three that cannot).
3. `WorkItemServiceTest`, `FasterUpdatesAcceptanceTest` and the connector incremental tests already pin probe →
   path; they keep doing so through the capability.
4. Live connector categories run **deliberately once** before the push, one category at a time, Linear last and once
   (shared key, rate limit); the maintainer is told before they run.
5. EXAMINE commands: reuse `architecture-rules`, `backend-tests`, `refresh-answers-compare` (refresh bodies before and
   after on demo data); **create** `connector-throws-count`: the catalogue's KPI 7 command, printing the count per
   folder (expected `0 0 0`).

### D-12 Mutation bar — RECOMMENDED

Last, on frozen code: no survivor or no-coverage mutant added in the touched code (`WorkItemService` capability
sites, `WriteBackService.WriteUpdates`). Deleted members remove mutants; nothing else is new production logic.

## Wave: DESIGN / [REF] Slices

Every slice is a day or less with one new oracle file; renames are their own commit. Each capability slice's RED is an
assertion, not a compile error: its oracle names the new interface by full name through reflection, and the same
oracle commit removes that capability's entries from the ratchet list (two-way list: still-offending entries not on
the list fail). Two nwdes features, in this order. ADRs, `ARCHITECTURE.md`, brief, catalogue and mutation are in the
finalize checklist. Size: **M–L, about 4.5 days, 6 slices** (option D-1 b: about 4 days, 5 slices, one feature).

| Id | nwdes feature | Delivers, in plain words | Oracle file (new, one per slice) | Size |
|---|---|---|---|---|
| A1 | A `story-6376-s06-a-ratchet-and-rename` | KPI 7 becomes a test: no connector type creates a `NotSupportedException` apart from an inline list (today 22 methods); made-up connectors that throw from a member, from a private helper and from an async body are each caught | `Architecture/ConnectorsDeclareNoUnsupportedMembersArchUnitTest.cs` | 1 d |
| A2 | A | The connector port says Portfolio, not Project: `GetFeaturesForPortfolio`, parameters named `portfolio` (pure rename commit) | `Architecture/ConnectorPortSpeaksPortfolioArchUnitTest.cs` | 0.5 d |
| B1 | B `story-6376-s06-b-capabilities` | Only Jira and Azure DevOps offer write-back; a write-back to a CSV, Linear or ServiceNow connection fails each update as before; 3 throws gone | `Architecture/WriteBackTargetCapabilityArchUnitTest.cs` | 0.5 d |
| B2 | B | ServiceNow no longer claims to read Portfolio Features; the other four do; a ServiceNow Portfolio refresh fails as before; 2 throws gone | `Architecture/FeatureSourceCapabilityArchUnitTest.cs` | 0.5 d |
| B3 | B | Only Jira and Azure DevOps offer a Team sweep, with the same per-connection answer; CSV, Linear and ServiceNow Teams take the full path as before; 6 throws gone | `Services/Implementation/WorkTrackingConnectors/IncrementalWorkItemSourceCapabilityTest.cs` | 1 d |
| B4 | B | The same for Portfolio and parent Feature sweeps; the base port loses its last throwing members; ratchet list down to Jira's sweep assertion and Azure DevOps' auth-scheme check | `Services/Implementation/WorkTrackingConnectors/IncrementalFeatureSourceCapabilityTest.cs` | 1 d |

Why two features: A is rules and a rename (no behaviour reach), B is the edits. With D-1 b, B3 and B4 merge and all
five slices fit one feature.

## Wave: DESIGN / [REF] Ratchet design

**Rule** (A1): in `Lighthouse.Backend.dll`, take every type that implements at least one interface declared in
`Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors` (connectors, auth strategies and their factory; not
the connector factory, E14). For every method of such a type, with compiler-made nested types (async state machines,
lambdas) folded into the method that wrote them, report `Type.Method(ParameterTypes)` when its IL contains
`newobj` of `System.NotSupportedException` or a type derived from it. Compare with an inline list through
`InlineBaseline` (both ways, never loosened, ADR-268).

**Mechanism**: Mono.Cecil, as `IlDependencies` already does, reading compiled IL. Weighed: the catalogue's `grep`
(cannot tell a member from a helper, misses an exception made elsewhere and thrown here, and counts comments);
reflection (sees no method bodies); ArchUnitNET (drops edges inside async bodies in Release); Roslyn (not a test
dependency). Cecil walks `MoveNext` of the state machines, so Debug and Release agree.

**Fix hint** (WHAT/WHY/HOW, in the failure): "<Type.Method> creates a NotSupportedException. A connector that cannot
do something must not implement the interface that asks for it. Remove the interface from the connector's list (and
its member), and let the caller test `connector is <Capability>`; if the answer depends on the connection, add a
probe to the capability as `SupportsIncrementalSync` does."

| Point | Inline list | Count |
|---|---|---|
| A1 (today) | CSV 6, Linear 6, ServiceNow 8 member methods; `JiraWorkTrackingConnector.SweepIdentities(...)` (deployment not yet known: the probe said no, the caller asked anyway); `AzureDevOpsWorkTrackingConnector.ConvertAuthorizationHeaderToVssCredentials(...)` (an auth strategy produced a scheme the connector cannot use) | **22** |
| after B1 | − 3 `WriteFieldsToWorkItems` | 19 |
| after B2 | − 2 ServiceNow Feature reads | 17 |
| after B3 | − 6 (Team sweep and by-id fetch, 3 connectors) | 11 |
| after B4 | − 9 (Feature sweep, Feature by-id fetch, parent sweep, 3 connectors) | **2** (Jira, Azure DevOps, each with its reason) |

KPI 7 as the catalogue measures it (`grep` over `{Csv,Linear,ServiceNow}`) reaches **0** after B4.

**Falsifiers** (A1, made-up types in a test-only namespace, each implementing the one-member port interface
`IWorkTrackingAuthStrategy`; the rule's core takes the module and namespace so it runs on the test assembly):
1. `ApplyAsync => throw new NotSupportedException()` → reported.
2. `ApplyAsync` calls a private `Refuse()` that throws it → reported against `Refuse`.
3. an `async` `ApplyAsync` whose body throws it after an `await` → reported against `ApplyAsync` (state machine folded).
4. throws `PlatformNotSupportedException` → reported (derived type).
5. a type implementing no port interface that throws it → **not** reported (negative control).
6. a port member that throws `InvalidOperationException` → not reported.

Other rules: the connector leaf rule (`WorkTrackingIntegration_IsALeafInputAdapter`) and
`WorkTrackingConnectors_DoNotDependOnAnyRepository` are unchanged and keep holding (the new interfaces sit in the same
namespace). No rule is loosened.

## Wave: DESIGN / [REF] Component decomposition

| Type(s) | Where | Change | Slice |
|---|---|---|---|
| `ConnectorsDeclareNoUnsupportedMembersArchUnitTest` (+ falsifier fixtures) | `Lighthouse.Backend.Tests/Architecture` | NEW (test) | A1 |
| `IWorkTrackingConnector` and the five connectors, `WorkItemService`, 47 test files | as today | RENAME | A2 |
| `IWriteBackTarget` | `Services/Interfaces/WorkTrackingConnectors` | NEW | B1 |
| `IFeatureSource` | same | NEW | B2 |
| `IIncrementalWorkItemSource` | same | NEW | B3 |
| `IIncrementalFeatureSource` | same | NEW | B4 |
| `IWorkTrackingConnector` | same | EDIT (members leave, comments rewritten) | B1–B4 |
| `IJira…`, `IAzureDevOps…`, `ILinear…`, `IServiceNow…WorkTrackingConnector` | same | EDIT (base list) | B1–B4 |
| `CsvWorkTrackingConnector`, `LinearWorkTrackingConnector`, `ServiceNowWorkTrackingConnector` | `Services/Implementation/WorkTrackingConnectors/*` | EDIT (throwing members deleted; CSV declares `IFeatureSource`) | B1–B4 |
| `JiraWorkTrackingConnector`, `AzureDevOpsWorkTrackingConnector` | same | none (markers carry the capabilities) | — |
| `WriteBackService` | `Services/Implementation` | EDIT (type test) | B1 |
| `WorkItemService` | `Modules/WorkItems/Application` | EDIT (type tests, `IFeatureSource` once per Portfolio refresh) | B2–B4 |
| Factory, controllers, `ConnectionHealthService`, Promises callers, `Program.cs` | | STAY | — |

## Wave: DESIGN / [REF] Driving ports

Unchanged. Host: `TeamUpdater`, `PortfolioUpdater` → `ITeamDataService`/`IWorkItemService`; the update queue →
`IWriteBackService` through `WriteBackCollector`. HTTP: the Teams, Portfolios, connection and Delivery source
controllers (routes, guards and responses untouched). The behaviour proof drives `IWorkItemService` and
`IWriteBackService`, the application-service ports.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapters | Status |
|---|---|---|
| `IWorkTrackingConnectorFactory` | `WorkTrackingConnectorFactory` | unchanged |
| `IWorkTrackingConnector` (6 members) | all five connectors | reduced |
| `IFeatureSource` | Jira, Azure DevOps, Linear, CSV | new |
| `IIncrementalWorkItemSource`, `IIncrementalFeatureSource` | Jira, Azure DevOps | new; carry the per-connection probe |
| `IWriteBackTarget` | Jira, Azure DevOps | new |
| `IBoardInformationProvider`, `IDeliverySourceProvider`, `IDeliveryForecastPublisher` | as today | unchanged |
| No new external call, no new dependency, no new adapter. | | |

## Wave: DESIGN / [REF] Reuse Analysis

| Existing | Overlap | Decision |
|---|---|---|
| `IDeliveryForecastPublisher`, `IDeliverySourceProvider`, `IBoardInformationProvider` + their `is` callers | capability by type, probe on the capability | **REUSE** the idiom; the new interfaces copy their shape |
| `SupportsIncrementalSync` (ADR-139) | per-connection answer | **EXTEND**: moves onto the two incremental interfaces, same name and meaning |
| `SupportsTransitionHistory` | data-quality probe | **REUSE** on the base (D-2) |
| `IWorkTrackingConnector` | the port | **EXTEND** (reduced, renamed members) rather than **REPLACE** (D-1 d) |
| Per-system markers | DI registration | **EXTEND** base lists; no marker for CSV created (it registers as its class) |
| `IlDependencies`, `InlineBaseline`, `ModuleBoundariesArchUnitTest.WorkTrackingIntegrationPattern`, `ModuleRulesCatchANewOffenderArchUnitTest` (falsifier pattern) | IL reading, two-way lists, connector namespace, made-up offenders | **REUSE**; the ratchet adds an IL instruction scan beside `IlDependencies`, not a second reader |
| `Scripts/examine/bin/architecture-rules`, `backend-tests`, `refresh-answers-compare` | EXAMINE | **REUSE**; **CREATE_NEW** `connector-throws-count` (no command prints KPI 7 today) |
| `WorkTrackingReadException` (read refusal) for the absent Feature read | a refusal with a user sentence | **Not reused**: it would add a refresh reason the user sees today as none (E8), a behaviour change; recorded as a later improvement |
| A generic `GetCapability<T>()` on the factory | capability lookup | **Not created**: the `is` test already does it; a second way would compete |

**Impact closure** (every target and consumer):

| Locator | Kind | Disposition | Reason |
|---|---|---|---|
| `Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs` | port | CHANGED | members leave, rename |
| 4 new interface files | port | CHANGED (new) | D-1 |
| 4 marker interfaces | port | CHANGED | base lists |
| CSV, Linear, ServiceNow connectors | adapter | CHANGED | members deleted, rename |
| Jira, Azure DevOps connectors | adapter | CHANGED (rename only) | signatures already match |
| `Modules/WorkItems/Application/WorkItemService.cs` | consumer | MIGRATED | type tests, rename |
| `Services/Implementation/WriteBackService.cs` | consumer | MIGRATED | type test |
| `Factories/IWorkTrackingConnectorFactory.cs`, `WorkTrackingConnectorFactory.cs` | factory | UNCHANGED_COMPATIBLE | returns the base |
| `API/TeamsController.cs`, `PortfoliosController.cs`, `WorkTrackingSystemConnectionsController.cs`, `WorkTrackingSystemConnectionController.cs`, `Services/Implementation/ConnectionHealth/ConnectionHealthService.cs` | consumer | UNCHANGED_COMPATIBLE | base members only |
| `API/DeliverySourcesController.cs`, `Services/Implementation/DeliverySources/DeliverySourceResolver.cs`, `DeliveryForecastPublishingService.cs` | consumer | UNCHANGED_COMPATIBLE | already type-test unchanged interfaces |
| `Program.cs` | composition root | UNCHANGED_COMPATIBLE | registrations by marker (E15) |
| 25 test files with `Mock<IWorkTrackingConnector>`, 47 with the old name, 3 asserting removed throws | tests | MIGRATED | D-10 |
| `Architecture/ModuleBoundariesArchUnitTest.cs` | rule | UNCHANGED_COMPATIBLE | same namespaces |
| Stryker configs, `.nwave/examine.json` | build/verify metadata | CHANGED (examine: one command) | D-11, D-12 |
| Frontend, E2E, Lighthouse-Clients, package manifests | | UNCHANGED_COMPATIBLE | no wire change |

**Agreement analysis**: the shared contract is the connector port, inside one deployable. Producers (5 connectors)
and consumers (`WorkItemService`, `WriteBackService`, validation callers, Promises callers) change in the same commits:
covered by the primary oracles; no contract test applies (the parties cannot change apart).

## Wave: DESIGN / [REF] Cross-cutting

| Item | Answer |
|---|---|
| Behaviour-free | D-0; the only text change is log-only (D-5). |
| RBAC | N/A, because no endpoint, guard or authorisation call changes and nothing fetches `my-summary`. |
| Lighthouse-Clients (CLI/MCP) | N/A, because no route, DTO or enum changes. |
| Website | N/A, because nothing user-visible changes. |
| Usage-data event | N/A, because nothing becomes usable or used differently (DEVOPS restates it). |
| UI sketch, loading and failure look | N/A, because no view changes. |
| Terminology | No user-facing text. Code names from the glossary: Feature, Work Item, Portfolio, write-back. |
| `Program.cs` | Not edited (E15), so the full integration suite is not forced by it. |
| Live connector categories | Run deliberately once before the push (D-11), connector code changes. |
| Architectural tests | Apply: the ratchet and four capability oracles, each falsifying its law over all connector types, including ones added later. |
| Contract tests | N/A, because producer and consumers ship in one deployable and change in one commit. |
| Test time | Architecture tests read the IL once per fixture like their siblings; no derogation is requested. |

## Wave: DESIGN / [REF] Formal verification

Not applicable, with the reason. The formal-verification skill was read; **no checker (Quint, TLC, Agda) was probed
or run**, because no claim selects one. S06's claims: (1) "no connector type creates a `NotSupportedException` outside
the list", decided exactly by the compiled IL with six made-up falsifiers; (2) "each connector type offers exactly
these capabilities", decided by reflection over the built types; (3) "the same sync path is chosen per connector and
connection", a pure decision `type ∧ probe` replacing `probe` where the type test is true exactly for the two types
whose probe can answer `true` (E2, E4) — a finite, closed table, pinned row by row in B3/B4, not a broad domain, so no
property test either. The one state change in this area, Jira's deployment going from unknown to known in a process,
is unchanged code owned by ADR-139; S06 adds no state, transition, ordering or concurrency. If the maintainer wants
that unknown → known transition modelled, the claim is **INDETERMINATE** until a checker is probed.

## Wave: DESIGN / [REF] Residuality

Stressors from negated assumptions, walked against the finished design (a walkthrough, not an execution).

| Assumption negated | What happens | Preserved / forbidden | Residue and owner |
|---|---|---|---|
| A capability is static per type → it becomes per connection (e.g. ServiceNow reads Features from some instances only) | The connector implements the capability and adds a `Supports…(connection)` probe to it, like incremental sync | Forbidden: a throw from the member; the ratchet catches it | Pattern written in ADR-285; owner: whoever adds the capability |
| A new connector arrives | It implements the base and only what it can do; the factory returns it; callers' type tests route it | A throwing member fails the ratchet with the fix hint | none |
| Jira's deployment is unknown | Probe `false` → full path, as today; a caller that ignores the probe hits the listed assertion | Never a half sweep (ADR-138's removal rule) | Listed entry with reason |
| ServiceNow later sweeps its Teams | It implements `IIncrementalWorkItemSource` only (D-1 a) | No Feature half to throw for | none with D-1 a; with D-1 b it would need a throw or a port split |
| A decorator or proxy wraps connectors (caching, logging, a test double) | Type tests see the wrapper, not the connector: capabilities vanish silently and Teams fall back to the full path | Slower, never wrong (full path is always safe) | Single point: the factory hands out the concrete instance today; a future wrapper must forward the capability interfaces. Owner: S03a/S09a when they shape the contract. Test doubles: compile errors force `.As<>()` (E10) |
| The factory returns the base type | Callers must type-test; the compiler cannot prove a caller tested | A missing test is a compile error when the caller calls a capability member | none |
| A caller needs two capabilities | `IIncrementalFeatureSource : IFeatureSource` covers the only such caller | | none |
| A Portfolio is saved on ServiceNow without validation (direct API) | Refresh fails as today (`Success = false`, no reason) | Same as today | Later improvement: a read refusal with the user sentence (behaviour change, own item) |
| Someone constructs a `NotSupportedException` in another type and throws it from a connector | Not caught by the ratchet (it reads construction sites) | | Accepted gap, named in the rule's summary; the KPI 7 `grep` and review cover it |

Second set, not used to derive the design: a Jira instance switches from Data Center to Cloud while running (cache
keyed by site address; unchanged code, unchanged answer); two refreshes of one connection run at once (no new shared
state; the probe reads the same cache as today); a test assembly loads a stale build (the ratchet reads
`Lighthouse.Backend.dll` from the test output, as every IL rule does).

## Wave: DESIGN / [REF] Test substrate

- Language/runner: C# .NET 10, NUnit 4.6, Moq, Mono.Cecil (already referenced by `IlDependencies`). No new package.
- Driving ports for behaviour: `IWorkItemService`, `IWriteBackService` (application services); existing scenarios
  stay where they are. Capability oracles are architecture or connector-level tests: the claim is about types.
- Oracles: `Lighthouse.Backend.Tests/Architecture/*` (A1, A2, B1, B2), connector folder (B3, B4: they construct the
  real connectors with stubbed HTTP handlers as their unit tests do, and ask the probe for a reached Jira connection
  via the gateway auth method, an unreached Jira site, Azure DevOps, and the three that are not the type).
- Fast loop: `pwsh Lighthouse.Backend/Test-Fast.ps1` plus the slice's oracle:
  `dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~<OracleClass>"`.
- Full gate at the end of each slice and before the push: `dotnet build` (0 warnings) and
  `dotnet test --filter "TestCategory!=JiraIntegration&TestCategory!=LinearIntegration&TestCategory!=AdoIntegration&TestCategory!=ServiceNowIntegration&TestCategory!=GithubIntegration"`;
  then the live categories once, deliberately (D-11).
- EXAMINE: `.nwave/examine.json` gains `connector-throws-count` before the first EXAMINE.

## Wave: DESIGN / [REF] SSOT edits pending

Not made. Written in the finalize checklist after feature B.

- **ADR-285** (new, Proposed → Accepted at close-out): "Connector capabilities are interfaces; per-connection variance
  is a probe on the capability". Context: ADR-139's rejected alternative, KPI 7. Decision: D-1, D-2, D-5; the
  capability matrix; the Jira runtime case as it is now (E4). Consequences: the ratchet; the decorator residue.
- **ADR-139**: status line "Amended by ADR-285"; a dated note that Data Center sweeps since its probe passed, and the
  remaining runtime case is "deployment not yet known".
- **ADR-270**: Tracker integration glossary gains **Capability** ("What a connector can do for a connection: read
  Features, sweep, write back. A connector that cannot do something does not offer it; where the answer depends on
  the connection, the capability asks the connection") with code names `IFeatureSource`, `IIncrementalWorkItemSource`,
  `IIncrementalFeatureSource`, `IWriteBackTarget`; the "Project" drift row: "connector port renamed (Story 6376)".
- **`ARCHITECTURE.md` §4**: Tracker integration row names the capability interfaces; enforced-rules list gains the
  connector ratchet.
- **`brief.md`**: new section `## Application Architecture — story-6376 s06 connector capability interfaces`.
- **Catalogue**: KPI 7 → 0 (with the 2 listed genuine cases outside the three folders); Improvement log; S06 brief AC
  "Jira Cloud vs Data Center" corrected to "deployment not yet known" (E4) and "`Supports*` flags removed or reduced"
  answered by D-2/D-4.

## Wave: DESIGN / [REF] Open questions for the maintainer

1. **D-1**: split incremental sync by level (a: 6 slices, 2 nwdes features) or one interface (b: 5 slices, 1
   feature)? Recommended a.
2. **D-5**: accept the generic "not supported for {system}" sentence (log-only; differs from today only for
   ServiceNow write-back and the ServiceNow Portfolio refresh), or keep each connector's sentence (needs the sentence
   to live somewhere on the connector that does not offer the capability)? Recommended generic.
3. **D-10**: delete the 3 tests that assert a removed throw (replaced by the capability oracles), and waive "improve
   the tests you touch" for `.As<>()`/rename-only edits? Recommended yes.
4. **D-9**: a new ADR-285 rather than editing ADR-139 in place? Recommended new.

Later, not this Story: the two port leaks (D-8) to S03a/S09a; `SupportsDeliveryForecastPublishing` (D-4); a read
refusal for a ServiceNow Portfolio (Residuality).

## Wave: DESIGN / [REF] Architecture summary

A learning aid for the maintainer, not a replacement for `brief.md` or the ADRs.

**1. The shape in hexagonal terms (N = new, M = modified, U = unchanged).**

- Driving adapters: controllers and the update queue (U).
- Application: `WorkItemService` (M: asks "is this connector an incremental source / a Feature source?"),
  `WriteBackService` (M: asks "is it a write-back target?"), validation callers and Promises callers (U).
- Domain: none touched (`Models` types unchanged).
- Driven port: `IWorkTrackingConnector` (M: 16 → 6 members, the rest move); `IFeatureSource`,
  `IIncrementalWorkItemSource`, `IIncrementalFeatureSource`, `IWriteBackTarget` (N); the three existing capability
  interfaces (U); factory (U).
- Driven adapters: CSV, Linear, ServiceNow (M: their 20 throwing members are deleted); Jira, Azure DevOps (U apart
  from the rename). No new external call.

```text
 TODAY                                          AFTER S06
 WorkItemService ── if SupportsIncrementalSync  WorkItemService ── is IIncremental…Source && Supports…(conn)
 WriteBackService ─ WriteFieldsToWorkItems      WriteBackService ─ is IWriteBackTarget
        │                                               │
        v                                               v
 IWorkTrackingConnector (16 members)            IWorkTrackingConnector (6, all five connectors)
   CSV      throws 6                              IFeatureSource ............ Jira ADO Linear CSV
   Linear   throws 6                              IIncrementalWorkItemSource  Jira ADO
   ServiceNow throws 8                            IIncrementalFeatureSource . Jira ADO
   Jira, ADO implement all                        IWriteBackTarget .......... Jira ADO
                                                  (Jira's probe says no while its deployment is unknown)
```

**2. Patterns weighed.**

- Interface segregation (ISP) by capability: **chosen**; a connector implements only what it can do, so the
  substitution principle (LSP) holds: no member answers "I am not really this".
- Capability interfaces plus a per-connection probe on the capability: **chosen**; the type says "ever", the probe
  says "for this connection now" (Jira).
- Runtime capability object (`connector.Capabilities.CanSweep(connection)`) instead of types: **rejected**; keeps the
  throwing members, so KPI 7 stays at 20.
- Strategy per sync mode (a full and a sweep strategy chosen by the caller): **rejected**; the variation is in the
  connector, not in the caller's algorithm, and it moves ADR-138's removal rule out of `WorkItemService`.
- Type tests versus flags: **type tests** for static capability (already the idiom for Delivery sources and
  publishing); **flag** only where the answer varies per connection.
- Split the base by client (`IConnectionValidator`, `IWorkItemSource`): **rejected for now**; nothing throws there.
- One incremental interface: **rejected** (D-1 b); it repeats the smell the first time a connector can sweep only one
  level.
- Generic factory lookup `Get<T>()`: **rejected**; a second way to do what `is` does.
- Read refusal for an absent Feature read: **rejected here**; a user-visible change, its own item.
- Not needed: domain events, outbox, unit of work, value objects, optimistic concurrency (no data or state changes).

## Wave: DESIGN / [REF] Decisions taken in AFK mode (2026-11-16)

The maintainer is AFK (entered 2026-11-16 12:00; refactor + review per slice, push per slice, rigor **thorough**).
None of the open questions changes what a user sees or is told, so the recommendations stand. Each is listed for
the maintainer to revisit at the hold:

- **D-1:** a. Incremental sync split by level; 6 slices in 2 nwdes features (A rules and rename, B capabilities).
- **D-5:** the generic "not supported for {system}" sentence. Log-only; differs from today only for ServiceNow
  write-back and the ServiceNow Portfolio refresh.
- **D-10:** the 3 tests asserting a removed throw are deleted (the capability oracles replace them). Edits that only
  add `.As<>()` to a mock or follow the rename are not held to "improve the tests you touch". **Revisit:** this
  waives a CLAUDE.md rule for mechanical edits only; every other test class a slice changes still leaves better.
- **D-9:** a new ADR-285 (highest on `origin/main` is ADR-284 at 2026-11-16) amends ADR-139; ADR-139 gets a status
  note.
- All other decisions (D-0, D-2 to D-4, D-6 to D-8, D-11, D-12) as recommended.

## Wave: DEVOPS / [REF] Platform lens and usage data (2026-11-16)

- **Platform risk:** none. No deployment, environment, schema, configuration, CI workflow or observability change.
  The connector paths change, so CI's path-based selection runs the live connector categories on push (expected; the
  Linear key is shared with CI, so local live runs are done once, deliberately, before the push of the B feature).
- **Usage-data event:** N/A, because the Story is behaviour-free: nothing becomes usable or is used differently.

## Wave: DELIVER / [REF] Start of DELIVER (2026-11-16, AFK)

- **UI sketches: N/A, because nothing a user sees changes** (behaviour-free; the only text change is log-only, D-5).
- **Architecture review owed: none beyond DESIGN's self-falsification**; ADR-285 is written at finalize.
- **AFK options (maintainer, on entry):** slice wrap-up = refactor + review; push per slice with CI watched; no
  5-minute review window (improvement work).
- **Rigor profile (maintainer, on entry):** `thorough` for every slice.

| nwdes feature | Slices | Profile | Crafter, acceptance designer, architect | Reviewers, EXAMINE |
|---|---|---|---|---|
| A `story-6376-s06-a-ratchet-and-rename` | A1, A2 | `thorough` | opus | sonnet |
| B `story-6376-s06-b-capabilities` | B1–B4 | `thorough` | opus | sonnet |

## Wave: DESIGN / [REF] D-1 changed to b (maintainer note, 2026-11-16, AFK)

The maintainer noted that once Teams and Portfolios become units (Epic #6474), the connector will no longer tell
Portfolio from Work Items fetching: it will fetch the items for a unit, more generically. Splitting incremental sync
by level (D-1 a) would put the two-level shape into the port's types, and the 6474 prerequisites must not fix a
shape that Epic reverses. So **D-1 is now b**: one capability, `IIncrementalSource` ("this connector can sweep"),
implemented by Jira and Azure DevOps, holding the Team, Portfolio and parent-Feature sweep and by-id members, with
the per-connection probe. Feature B becomes **B1 write-back target, B2 Feature source, B3 incremental source (both
levels)**, about 4 days in total for S06. The reason D-1 a gave ("a connector that can sweep only one level")
returns, if ever, as a unit-level question in 6474, not as a second type now. Taken in AFK mode; revisit at the hold.
