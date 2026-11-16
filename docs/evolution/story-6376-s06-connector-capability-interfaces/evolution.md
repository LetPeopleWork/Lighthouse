# Story 6376 — S06 Connector capability interfaces: a connector offers only what it can do

**Finished:** 2026-11-16 · ADO User Story #6376 · Epic #6508 "Thin services and controllers"

## The problem

`IWorkTrackingConnector`, the port of Tracker integration, had 16 members, and three connectors could not serve all of
them. CSV threw `NotSupportedException` from 6 members, Linear from 6 and ServiceNow from 8: the sweeps and fetches by
id of incremental sync (15), write-back (3) and, for ServiceNow, reading a Portfolio's Features (2). The type claimed
what the connector could not do, and a caller learned the truth from a `Supports…` flag or a throw. ADR-139 had kept
incremental sync on the base port because Jira Cloud and Data Center answered differently; that is no longer so.

## What changed

Two nwdes features on `main`. Behaviour-free: nothing a user sees changed.

- **Feature A, the ratchet and the rename** (`docs/feature/story-6376-s06-a-ratchet-and-rename/`, 2 slices).
  - The connector port says Portfolio: `GetFeaturesForProject` is now `GetFeaturesForPortfolio`, and the Portfolio
    parameters are named `portfolio` (a pure rename, 55 files).
  - `ConnectorsDeclareNoUnsupportedMembersArchUnitTest` reads the compiled code of every type that implements a
    connector-port interface and fails on any method that creates a `NotSupportedException` (or a derived type) unless
    it is listed with its reason. Lambdas, local functions and async bodies count against the method that wrote them.
    It started with 22 entries. The examiner command `connector-throws-count` prints the count per connector folder.
- **Feature B, the capabilities** (`docs/feature/story-6376-s06-b-capabilities/`, 3 slices).
  - B1: `IWriteBackTarget`, implemented by Jira and Azure DevOps. `WriteBackService` asks for it inside its existing
    guard; a write-back to another connection still fails each update. List 22 → 19.
  - B2: `IFeatureSource`, implemented by Jira, Azure DevOps, Linear and CSV, not ServiceNow. The Work Items refresh asks
    for it where it reads Features (`FeatureSourceFor`); a ServiceNow Portfolio refresh still fails before anything is
    written. List 19 → 17.
  - B3: `IIncrementalSource`, implemented by Jira and Azure DevOps: the probe `SupportsIncrementalSync(connection)`, the
    Team, Portfolio and parent-Feature sweeps, and the Team and Portfolio fetches by id. The three scans in
    `WorkItemService` sweep only when the connector offers it and the probe says yes; otherwise the full path, as
    before. List 17 → 2.

The base port keeps 6 members all five connectors serve. Jira's probe says no while the connection's deployment is not
yet known in this process; Data Center sweeps too. A missing capability fails at the same point as the old throw, with
the log-only sentence "… is not supported for {system}".

## Key commits

| Commit | What |
|---|---|
| `2bfe80bc4`, `5585adf44` | Pick-up; DESIGN and DEVOPS |
| `ef1a424e7`, `2b2bd40ad` | A2: oracle, then the rename to `GetFeaturesForPortfolio` |
| `95acd5664`, `1d84781a5` | A1: oracle, then the ratchet with 22 listed methods and `connector-throws-count` |
| `6ca269a9a` | D-1 changed to one incremental capability |
| `1088c9131`, `dca205178` | B1: oracle, then write-back as a capability |
| `2dc57939d`, `5461075c5` | B2: oracle, then Feature reading as a capability |
| `7d42593d3`, `ff4a87502`, `0efc19afa` | B3: oracle (one rework), then incremental sync as one capability |
| `909737596` | Three service-port tests: a connector whose probe says no is never swept |
| `7feabab6f` | Mutation record |

## What it achieved

- **KPI 7: 20 → 0** in the CSV, Linear and ServiceNow folders (`connector-throws-count`: Csv 0, Linear 0, ServiceNow
  0, total 0). The ratchet keeps 2 genuine cases elsewhere: Jira `SweepIdentities` (a sweep before the deployment is
  known, which the probe prevents) and Azure DevOps `ConvertAuthorizationHeaderToVssCredentials` (an authentication
  scheme the client cannot turn into credentials).
- **Capability matrix**, pinned by reflection oracles:

  | Capability | Jira | Azure DevOps | Linear | ServiceNow | CSV |
  |---|:-:|:-:|:-:|:-:|:-:|
  | `IWorkTrackingConnector` (base, 6 members) | yes | yes | yes | yes | yes |
  | `IFeatureSource` | yes | yes | yes | — | yes |
  | `IIncrementalSource` (with probe) | yes | yes | — | — | — |
  | `IWriteBackTarget` | yes | yes | — | — | — |

- **The same sync path per connector and connection.** `IncrementalSourceCapabilityTest` pins the probe's answers for
  a reached and an unreached Jira connection, Azure DevOps, and the three without the capability.
- A gap older than this Story closed: no test checked that the probe was honoured (`909737596`).
- Architecture record: ADR-285 (new, amends ADR-139).

## Quality

| Gate | Result |
|---|---|
| Feature A | Oracles approved with RED observed; slice reviews S1 and S2 accepted; whole-candidate review accepted; EXAMINE accepted, all four expectations met |
| Feature B | Oracles approved with RED observed, S3 after one rework; slice reviews S1, S2 and S3 accepted; whole-candidate review accepted; EXAMINE accepted, all three expectations met |
| Mutation | See the finalize checklist |

No frontend file changed, so no frontend gate ran.

## Maintainer decisions (at the hold, 2026-11-16)

- **Keep the generic sentence** "… is not supported for {system}" for a missing capability. It reaches only the log,
  and differs from before only for a ServiceNow write-back and a ServiceNow Portfolio refresh.
- **One incremental capability, not one per level.** Once Teams and Portfolios become units (Epic #6474), the
  connector fetches the items of a unit; a split by level would put a two-level shape into the port that Epic undoes.
- **The deleted tests are accepted.** The tests that only asserted ServiceNow's removed throws (write-back in B1,
  Feature reading in B2) went with the members; the capability oracles state the same fact as a type.
- **CI flakes seen during delivery are left alone**, not chased in this Story.

## Knowingly left open

- **For S07a** (noted in its brief): `WorkItemService` still has private methods whose `Portfolio` parameter is named
  `project`; `FeatureSourceCapabilityArchUnitTest` and `WriteBackTargetCapabilityArchUnitTest` sit under
  `Tests/Architecture`, while `IncrementalSourceCapabilityTest` sits under
  `Tests/Services/Implementation/WorkTrackingConnectors`, to be brought together as a plain refactor.
- **For S03a/S09a** (feature-delta D-8): `IBoardInformationProvider` takes `Board`/`BoardInformation` from the
  implementation namespace, and the factory interface and its class sit in different namespaces.
- **Whole-file mutation scores below 80 %** (`WorkItemService.cs` 79.0 %, `WriteBackService.cs` 59.6 %) come from old
  code this Story did not touch.
- **A wrapper around connectors** must forward the capability interfaces (ADR-285, consequences).
- Later items: a read refusal with a user sentence for a ServiceNow Portfolio (a behaviour change, its own item);
  `SupportsDeliveryForecastPublishing`, whose Jira answer is always yes.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | N/A, because no page under `docs/` describes the connector port, and nothing a user sees or reads changed. The only changed text is a log-only sentence. The architecture docs are under "Context map" below. |
| Screenshots | N/A, because no screen changed. |
| Demo data | N/A, because no data shape or seeded value changed. |
| Website assets | N/A, because no asset under `docs/assets/` changed and nothing user-visible is new. |
| Clients (CLI, MCP) | N/A, because no route, DTO or enum changed; the port is internal. No client release is due. |
| Context map, `ARCHITECTURE.md` | Done in this finalize: ADR-270 Tracker integration glossary gains **Capability** (`IFeatureSource`, `IIncrementalSource`, `IWriteBackTarget`), and its Project drift row records the connector port rename; `ARCHITECTURE.md` §3 (the probe is on the incremental-sync capability; ADR-139, ADR-285), §4 row 1 (base port and the three capabilities), the enforced-rules list (the connector ratchet) and the ADR index (ADR-285). With them: new ADR-285, ADR-139 status line and dated note, the new section in `docs/product/architecture/brief.md`, KPI 7 and the Improvement log in `docs/feature/epic-6264-modular-monolith/feature-delta.md`, the S06 brief (`slices/slice-20-…`) and the S07a brief (`slices/slice-21-…`). |
| Usage-data event | N/A, because the Story is behaviour-free: nothing becomes usable or is used differently (DEVOPS answered the same). |
| Mutation | Done: `docs/feature/story-6376-s06-connector-capability-interfaces/mutation/results.md`. The lines this Story changed: 3 of 3 killable mutants killed, 2 survivors accepted as equivalent or log-only. Stryker reported the 30 mutants on the new pattern-variable guards as CompileError, so they were checked by hand mutation; deleting the probe check survived every suite, and three tests in `WorkItemServiceTest` (`909737596`) now kill it. Whole files 76.71 % (old code, see above). Connector files: deletions only, no mutants. |
| Release notes | N/A; written at /release. |
