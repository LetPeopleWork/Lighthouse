# RED classification — slice 01 (ADO Story #5611)

Pre-DELIVER fail-for-the-right-reason gate. Every acceptance test authored in this DISTILL run was
executed **before** its skip marker was applied, and classified against what it actually failed on.
`MISSING_FUNCTIONALITY` is the only acceptable RED; anything else is a test bug and blocks handoff.

Run date: 2026-07-31. Commands and their output are at the bottom.

## How "RED, not BROKEN" was achieved — and why no scaffolds exist this time

Slices 01, 02 and 04 needed production scaffolds because their tests named types that did not exist.
**This slice needed none.** Every scenario is driven through a production entry point that already
ships — `GetWorkItemsForTeam`, `ValidateTeamSettings`, `ValidateConnection`,
`ServiceNowWorkItemMapper.MapRecord`, `new TeamSettingDto(team, today)`, `getDefaultTeamSchema` — so
the whole suite compiles and runs against today's `main` with no production change of any kind. That
is a consequence of the design rather than luck: ADR-123 adds no route, no DTO type and no persisted
column, and `ServiceNowReadScope` / `ServiceNowTableHierarchy` are internal to the connector, which
Mandate 1 forbids a test from addressing directly anyway.

Two consequences worth stating:

- **`dotnet build` is clean (0 warnings, 0 errors) and `pnpm exec tsc -b` exits 0** with every new
  test in the tree. The C# BROKEN class ("does not compile") is empty by construction; the
  TypeScript one is empty because nothing imports a symbol that does not exist.
- **Two signature changes DESIGN specifies could not be written against directly**, because a test
  calling a signature that does not exist yet does not compile and would break `pnpm build`:
  - `DataRetrievalSchemaDto.ForTeam(system, workItemTable)` — the scenarios go through
    `TeamSettingDto`, which is the driving-port shape the settings screen actually reads, and which
    resolves the table from the connection it already holds.
  - `getDefaultTeamSchema(connection)` — the scenarios call it through a one-line
    `as unknown as (connection: unknown) => IDataRetrievalSchema` cast, commented at the call site.
    DELIVER deletes the cast when the signature lands.

  Both still fail on the *behaviour* (`isWorkItemTypesRequired` is `false` where `true` is
  specified; the frontend lookup falls through to the `"query"` fallback arm), never on plumbing.

The `serviceNowSchemaTwin.enforcement.test.ts` guard reads **both** sides as source text rather than
importing the frontend constant, following `formatLikelihood.enforcement.test.ts`. That is what
turns "the constant does not exist yet" into an assertion failure naming the missing declaration
instead of a module-resolution error — RED rather than BROKEN — and it is also the stronger guard,
because a rename on the frontend side cannot pass unnoticed through the module system.

## Skip markers

Per the project's standing rule (never push red; skip what is not yet passing, un-skip in DELIVER):

- **C#** — `[Ignore("DISTILL scaffold for #5611 slice 01 — un-skip in DELIVER (ADR-025).")]` on each
  not-yet-passing `[Test]`. 18 tests skipped, 242 passed, 0 failed across the ServiceNow + schema
  filter.
- **TypeScript** — `describe.skip` on the two new blocks, with the same one-line reason above it.
  5 tests skipped, 3 passed.

`grep -rn "DISTILL scaffold for #5611"` finds every one of them; zero should remain at the end of
DELIVER.

## Backend — `ServiceNowRecordClassTest` (new fixture, layer 3, stubbed transport)

| Test | AC / decision | What it failed on | Verdict |
|---|---|---|---|
| `ATeamThatHandlesIncidentsAndChanges_SeesBothKindsOfWorkAsOneTeam` (walking skeleton) | AC-B1 + AC-B2 | got 3 kinds where 2 were named; every `Type` was `"task"` | MISSING_FUNCTIONALITY |
| `ATeamThatHandlesSeveralKindsOfWork_AsksForThemInOneRead` | AC-B1 / D-D2 | no `sys_class_nameIN…` clause in the read | MISSING_FUNCTIONALITY |
| `ATeamThatHandlesOneKindOfWork_AsksForItByName` | AC-B1 / ADR-123 §2 | no `sys_class_name=incident` clause | MISSING_FUNCTIONALITY |
| `ATeamOnTheWholeHierarchyThatNamedNoKindsOfWork_ReadsNothingRatherThanEverything` | AC-B3 / D3 | 3 work items returned and 2 requests issued where none should be | MISSING_FUNCTIONALITY |
| `SavingATeamOnTheWholeHierarchyThatNamedNoKindsOfWork_IsAskedWhichKindsWithoutContactingTheInstance` | AC-B3 / D-D4 | `IsValid` true; no `missing_work_item_types` rung exists | MISSING_FUNCTIONALITY |
| `SavingATeamThatNamesAKindOfWorkTheInstanceDoesNotHave_IsToldWhichNameIsWrong` | AC-B6 / ADR-124 rung 1 | `IsValid` true — the class is never probed | MISSING_FUNCTIONALITY |
| `SavingATeamThatNamesAKindOfWorkTheInstanceRefuses_IsToldItIsAPermissionsProblem` | AC-B6 / ADR-124 rung 2 | `IsValid` true — the class is never probed | MISSING_FUNCTIONALITY |
| `SavingATeamThatNamesAKindOfWorkTheAccountCannotSee_IsToldWhichKindIsHidden` | AC-B6 / ADR-124 rung 4 | `IsValid` true; message names no class | MISSING_FUNCTIONALITY |
| `SavingATeamThatNamesAKindOfWorkWithNothingInItYet_IsAccepted` | AC-B6 / OQ-8 | verdict is right by accident; **0 probes were made** | MISSING_FUNCTIONALITY |
| `SavingATeamThatNamesThreeKindsOfWork_AsksTheInstanceAboutEachOfThemOnce` | S2 / OQ-5 | 0 class probes, 2 requests total instead of 5 | MISSING_FUNCTIONALITY |
| `SavingATeamThatHandlesSeveralKindsOfWork_MeasuresItsQueryAgainstItsOwnKindsOfWork` | S1 / ADR-124 §3 | neither count probe carries the class clause | MISSING_FUNCTIONALITY |
| `ATeamThatHandlesSeveralKindsOfWork_LooksForStateHistoryOnEachOfThoseKinds` | S4 / ADR-123 §9 | definition query is `table=task`, which measures nothing | MISSING_FUNCTIONALITY |
| `ValidatingAConnectionRootedAtTheWholeHierarchy_SaysStateHistoryIsDecidedPerTeam` | D-D10 | advisory is `history_requires_state_metric` — advice that cannot be followed | MISSING_FUNCTIONALITY |

### Green on `main`, deliberately — the backward-compatibility pins

AC-B2 and AC-B5 are claims about the **absence** of change, so their tests pass today by
construction. They are not RED and must not be: they exist to fail the moment DELIVER changes the
wire form of a shipped team's read. They are **not** skipped.

| Test | AC | Why it is green now |
|---|---|---|
| `AnIncidentTeamThatNamedNoKindsOfWork_AsksExactlyWhatItAskedBefore` | AC-B5 | no class clause is emitted, which is the shipped behaviour |
| `AnIncidentTeamThatNamedNoKindsOfWork_LooksForStateHistoryExactlyWhereItDidBefore` | AC-B5 | `table=incident` is the shipped definition scope |
| `SavingAnIncidentTeamThatNamedNoKindsOfWork_IsStillAccepted` | AC-B5 | a leaf-rooted team never reaches the new refusal |
| `ServiceNowWorkItemMapperTest.WorkOnATeamReadingOneKindOfWork_IsLabelledExactlyAsItWasBefore` | AC-B2 | record class and configured table are the same string |
| `…WorkThatLeavesItsKindBlank_KeepsTheKindTheTeamReadsThrough` | AC-B2 / D-D5 | the fallback is what ships today |
| `…WorkFromATableThatDoesNotRecordItsKind_KeepsTheKindTheTeamReadsThrough` | AC-B2 / D-D5 | ditto |
| `DataRetrievalSchemaDtoTest.ATeamOnASingleKindOfServiceNowWork_IsNotAskedForKindsOfWorkAtAll` | AC-B5 | `IsWorkItemTypesRequired` is `false` unconditionally today |
| `…ATeamOnAServiceNowConnectionThatNamedNoTable_IsNotAskedForKindsOfWorkEither` | AC-B5 | ditto |

## Backend — extended fixtures

| Test | File | AC | What it failed on | Verdict |
|---|---|---|---|---|
| `WorkThatSaysWhatKindItIs_IsLabelledWithItsOwnKind` | `ServiceNowWorkItemMapperTest` (layer 1, pure) | AC-B2 / ADR-123 §8 | `Type` was `"task"`, expected `"change_request"` | MISSING_FUNCTIONALITY |
| `ATeamOnAWholeServiceNowHierarchy_IsAskedWhichKindsOfWorkAreItsOwn` | `DataRetrievalSchemaDtoTest` | AC-B4 / D6 | `IsWorkItemTypesRequired` was `false` | MISSING_FUNCTIONALITY |

## Backend — `ServiceNowWorkTrackingConnectorIntegrationTest` (live PDI)

Extended, not forked — the fixture slice 02 extended. `[Category("Integration")]` +
`[Category("ServiceNowIntegration")]`, path-scoped via `Scripts/test-selection/path-classifier.sh`,
credentials from `$ServiceNowLighthouseIntegrationTestToken`.

| Test | AC / decision | Verdict | Note |
|---|---|---|---|
| `AKindOfWorkTheInstanceDoesNotHave_IsRefusedBySaveAndNamed` | AC-B6 / ADR-124 rung 1 | MISSING_FUNCTIONALITY (**not executed against the instance in this run**) | ADR-124's one inferred-then-measured link, kept as a standing guard so a ServiceNow release cannot quietly turn the `400` into a `200` |
| `AKindOfWorkTheAccountMayNotRead_IsToldApartFromOneItCan` | AC-B6 / ADR-124 rungs 3+4 | MISSING_FUNCTIONALITY (**not executed**) | uses `lh_probe_snc_read`, which reads `incident` but not `problem`. That asymmetry is the whole proof of AC-B6 |
| `ATeamCoveringSeveralKindsOfWork_StillLearnsWhenItsWorkChangedState` | S4 | MISSING_FUNCTIONALITY (**not executed**) | a `task`-rooted team gets 0 definitions today, so this cannot pass before the definition scope moves |

**Honest limit on these three**: they were compiled and skipped, not run against the PDI — the
credential is not in this working tree. They are classified `MISSING_FUNCTIONALITY` by construction
(each asserts behaviour the connector does not have), but the classification is *derived*, not
*observed*. DELIVER must run them against the instance before calling slice 01 done; DoD item 5
already says so and is not negotiable.

## Frontend

| Test | File | AC | What it failed on | Verdict |
|---|---|---|---|---|
| `asks which kinds of work are the team's own` | `DataRetrievalSchemaDefaults.serviceNow.test.ts` | AC-B4 | got the `"query"` fallback arm, expected `"servicenow.query"` | MISSING_FUNCTIONALITY |
| `leaves a team reading only incidents exactly as it was` | same | AC-B5 | same | MISSING_FUNCTIONALITY |
| `treats a connection that named no table as reading one kind of work` | same | AC-B5 | same | MISSING_FUNCTIONALITY |
| `names the same tables as holding several kinds of work` | `serviceNowSchemaTwin.enforcement.test.ts` (new) | AC-B4 / D-D7 | `ServiceNowTableHierarchy.cs` does not exist — reported as a named assertion failure, not an ENOENT | MISSING_FUNCTIONALITY |
| `calls the work item table setting the same thing on both sides` | same | AC-B4 / D-D7 | `serviceNowWorkItemTableOptionKey` not declared on the frontend | MISSING_FUNCTIONALITY |

One existing frontend test was **replaced rather than deleted**:
`does not ask for a separate list of work item types` asserted `isWorkItemTypesRequired === false`
for ServiceNow unconditionally. That answer is now conditional on the table, so the flat assertion
becomes wrong under this slice; its successor is the three-case connection-shaped block above.

## Verdict

**16 of 16 executed failures are `MISSING_FUNCTIONALITY`. Zero BROKEN, zero WRONG_ASSERTION, zero
OBSERVABLE_NOT_AT_PORT.** Three integration tests are classified by construction and must be run
against the PDI in DELIVER. Handoff to DELIVER is not blocked.

## Commands

```
# backend, before the skip markers were applied
dotnet build Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj
#   Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test  --filter "FullyQualifiedName~ServiceNowRecordClassTest|FullyQualifiedName~ServiceNowWorkItemMapperTest|FullyQualifiedName~DataRetrievalSchemaDtoTest"
#   Failed: 15, Passed: 57, Total: 72

# backend, after
dotnet test --filter "FullyQualifiedName~ServiceNow|FullyQualifiedName~DataRetrievalSchemaDtoTest"
#   Passed! Failed: 0, Passed: 242, Skipped: 18, Total: 260

# frontend, before
pnpm exec vitest run src/models/Common
#   Test Files 2 failed (2) | Tests 5 failed | 3 passed (8)

# frontend, after
pnpm exec vitest run src/models/Common
#   Test Files 1 passed | 1 skipped (2) | Tests 3 passed | 5 skipped (8)
pnpm exec tsc -b            # exit 0
pnpm biome check ./src/models/Common
#   Checked 6 files. No fixes applied.
```
