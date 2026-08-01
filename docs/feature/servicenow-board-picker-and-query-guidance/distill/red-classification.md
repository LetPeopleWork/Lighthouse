# RED classification — slices 01 and 02 (ADO Story #5610)

Pre-DELIVER fail-for-the-right-reason gate. Every acceptance test authored in this DISTILL run was
executed **before** its skip marker was applied, and classified against what it actually failed on.
`MISSING_FUNCTIONALITY` is the only acceptable RED; anything else is a test bug and blocks handoff.

Run date: 2026-08-01. Commands and their output are at the bottom.

## The scaffold, and why the compiler chose it

The first attempt reached the board port through a cast — `(IBoardInformationProvider)connector` —
so that no production code would move. **The build rejected it**, and correctly:

```
error S1944: Review this cast; in this project there's no type that extends
'ServiceNowWorkTrackingConnector' and implements 'IBoardInformationProvider'.
```

`TreatWarningsAsErrors` turns that Sonar rule into a build failure for everyone, so the port has to
exist before a test can name it. Three scaffold edits, all inert:

| Scaffold | Why it cannot ship a wrong answer |
|---|---|
| `DataRetrievalSchemaDto.Placeholder` / `.HelpText` — two nullable properties, default `null` | `null` is exactly today's behaviour: MUI renders no placeholder attribute and no helper row for an absent value. The JSON payload gains two nulls the frontend interface does not declare and therefore ignores |
| `IServiceNowWorkTrackingConnector : …, IBoardInformationProvider` + the amended xmldoc | The xmldoc asserted the opposite (*"ServiceNow has no board concept"*) and became false the moment the interface changed, so amending it is part of the same edit rather than deferred work |
| `ServiceNowWorkTrackingConnector.GetBoards` / `.GetBoardInformation` returning empty | **Unreachable from the API.** `WizardsController`'s switch has no ServiceNow arm, so a ServiceNow connection still falls through to `NotImplementedException` — which is what the two acceptance tests observe as `500`. No user can reach the empty answer until DELIVER adds the arm, and adding the arm is the commit that also un-skips the tests |

`grep -rn "DISTILL scaffold for #5610"` finds the two stub scaffolds plus every skipped test — 29
occurrences; zero should remain at the end of DELIVER. The interface amendment is deliberately
**unmarked**: it is the permanent change ADR-125 requires, not a stub to be removed.

## Skip markers

Per the project's standing rule (never push red; skip what is not yet passing, un-skip in DELIVER):

- **C#** — `[Ignore("DISTILL scaffold for #5610 - un-skip in DELIVER (ADR-025).")]` on 15 tests, plus
  a longer reason on the 4 live-PDI tests that were compiled but never executed against the instance.
- **TypeScript** — `describe.skip` on two blocks and `it.skip` on six tests, 12 tests in all.

Gate state after the markers: `dotnet build` 0 warnings / 0 errors · `dotnet test` (ServiceNow +
schema + wizards + architecture filter) 372 passed, 19 skipped, 0 failed · `pnpm test` 3 821 passed,
12 skipped, 0 failed · `pnpm exec tsc -b` exit 0 · `pnpm biome check ./src` clean.

## Slice 01 — the query field says what to put in it

| Test | File | AC | What it failed on | Verdict |
|---|---|---|---|---|
| `AServiceNowTeamsQueryField_ShowsAWorkedExampleOfTheQueryItWants` | `DataRetrievalSchemaDtoTest` | AC-A1 / DD-5 | `Placeholder` was `null`, expected `active=true^assignment_group=Service Desk` | MISSING_FUNCTIONALITY |
| `AServiceNowTeamsQueryField_NamesBothWaysAQueryFailsQuietlyAndWhereToGetAGoodOne` | same | AC-A4 | `HelpText` was `null` | MISSING_FUNCTIONALITY |
| `shows a worked example of the query it wants` | `DataRetrievalSchemaDefaults.serviceNow.test.ts` | AC-A1 / AC-A3 | `placeholder` was `undefined` | MISSING_FUNCTIONALITY |
| `names both ways a query fails quietly, and where ServiceNow will hand you a good one` | same | AC-A4 | `helpText` was `undefined` | MISSING_FUNCTIONALITY |
| `backend shows the same worked example` | `serviceNowQueryGuidance.enforcement.test.ts` (new) | AC-A3 / D4 | the example literal is absent from `DataRetrievalSchemaDto.cs` — reported as a named containment failure, not an ENOENT | MISSING_FUNCTIONALITY |
| `frontend shows the same worked example` | same | AC-A3 / D4 | absent from `DataRetrievalSchemaDefaults.ts` | MISSING_FUNCTIONALITY |
| `backend carries the help text beside it` | same | AC-A3 / AC-A4 | neither hazard token present | MISSING_FUNCTIONALITY |
| `frontend carries the help text beside it` | same | AC-A3 / AC-A4 | neither hazard token present | MISSING_FUNCTIONALITY |
| `shows a worked example in the empty field and the guidance beneath it` | `GeneralSettingsComponent.test.tsx` | AC-A1 / AC-A5 | the rendered field carried no `placeholder` attribute | MISSING_FUNCTIONALITY |

### Green on `main`, deliberately — the absence pins

AC-A2 and AC-A6 are claims about the **absence** of change, so their tests pass today by
construction. They are not RED and must not be: they exist to fail the day guidance leaks onto a
shared arm or onto a surface that renders no query field. They are **not** skipped.

| Test | AC | Why it is green now |
|---|---|---|
| `AConnectorWithNothingToExplain_LeavesItsQueryFieldExactlyAsItWas` (×4 connectors) | AC-A2 | the scaffolded properties default to `null` for every arm but ServiceNow's |
| `AServiceNowPortfolio_IsOfferedNoGuidanceForAFieldItNeverRenders` | AC-A6 | `inputKind` is already `none` and no guidance is set |
| `offers no query guidance for a field it never renders` (frontend) | AC-A6 | ditto |
| `leaves a connector with nothing to explain exactly as it was` | AC-A2 | MUI renders no helper `<p>` for an absent `helperText` and no placeholder attribute for `undefined` — the property AC-A2's "no layout shift" rests on, asserted structurally rather than by CSS |

## Slice 02 — pick a Visual Task Board

### Driving port (`ServiceNowConnectionAcceptanceTest`, layer 5, real stack)

| Test | AC / decision | What it failed on | Verdict |
|---|---|---|---|
| `AnAdministratorAskingAServiceNowConnectionForItsBoards_IsToldWhyRatherThanShownAFault` | AC-B1 / AC-B3 / DD-1 / DD-6 | `500` where `400` is specified — ServiceNow still falls through the wizard's switch to `NotImplementedException` | MISSING_FUNCTIONALITY |
| `AnAdministratorAskingForOneBoardOfAnUnreachableInstance_IsToldWhyRatherThanOfferedABlankPreFill` | AC-B3 / DD-6 | `500` where `400` is specified | MISSING_FUNCTIONALITY |

The instance is a closed local port, which is a real unreachable host and needs no external system to
be deterministic — the same device the slice-01 walking skeleton uses.

### The board reads (`ServiceNowBoardPickerTest`, new fixture, layer 3, stubbed transport)

| Test | AC / decision | What it failed on | Verdict |
|---|---|---|---|
| `AnAdministratorOpeningThePicker_SeesTheBoardsThisConnectionCanTurnIntoATeam` | AC-B1 | got an empty list where two boards were named | MISSING_FUNCTIONALITY |
| `ABoardThatCannotBecomeAQuery_NeverReachesTheAdministrator` | AC-B4 / D14 / ADR-125 §3 | no `vtb_board` read was issued at all, so no scoping was asked for | MISSING_FUNCTIONALITY |
| `PickingABoard_HandsTheTeamTheBoardsOwnFilterAsItsQuery` | AC-B2 / DD-2 | `DataRetrievalValue` was empty, expected `correlation_id=LIGHTHOUSE_DEMO` | MISSING_FUNCTIONALITY |
| `PickingABoard_HandsTheTeamTheBoardsTableAsTheKindOfWorkItHandles` | AC-B2 / D6 | `WorkItemTypes` was empty, expected `["incident"]` | MISSING_FUNCTIONALITY |
| `PickingABoard_NeverHandsOverTheFilterAsItReadsOnTheServiceNowScreen` | DD-2 / ADR-125 §2 | the pre-fill was empty — the assertion is two-sided on purpose, because "does not contain the label form" passes vacuously on an empty string | MISSING_FUNCTIONALITY |
| `PickingABoardThatNoLongerQualifies_IsRefusedRatherThanHandedOverAsAnEmptyQuery` | ADR-125 §3 | no refusal was raised | MISSING_FUNCTIONALITY |
| `PickingABoardWhoseWorkIsNotAKindOfWork_IsRefusedByName` | AC-B4 / DD-4 / ADR-124 | no refusal was raised; `class_is_not_a_kind_of_work` is never reached | MISSING_FUNCTIONALITY |
| `AnAccountThatMayNotReadBoards_IsToldSoRatherThanShownAnEmptyPicker` | AC-B3 / ADR-126 §1 | no refusal was raised on a `403` | MISSING_FUNCTIONALITY |
| `ACredentialTheInstanceRejects_IsToldSoWhenThePickerOpens` | AC-B3 / ADR-126 §3 | no refusal was raised on a `401` | MISSING_FUNCTIONALITY |
| `AnAccountThatSharesNoBoard_IsOfferedAnEmptyListRatherThanToldTheConnectionIsBroken` | DD-7 / ADR-126 §3 | the instance was never asked — the assertion is two-sided on purpose, because "the list is empty" passes vacuously against a connector that reads nothing | MISSING_FUNCTIONALITY |

Two of these ten passed on the first run and were **strengthened rather than accepted**: the
`readable_filter` test and the empty-list test were both vacuously true against a connector that
returns nothing. Each now asserts that something happened as well as what did not.

### Architecture (`ServiceNowValidationVerdictPurityArchUnitTest`, extended)

| Test | Decision | What it failed on | Verdict |
|---|---|---|---|
| `TheBoardPickersDecisions_LiveInPureCoresOfTheirOwn` | ADR-125 / ADR-126 §3 | neither `ServiceNowBoardVerdict` nor `ServiceNowBoardMapper` exists | MISSING_FUNCTIONALITY |

The three existing purity rules were widened to both names. They are stated by full name, so an
absent type satisfies them by not being there — which is exactly why the existence test above was
added rather than trusting the widening to carry the claim.

### Frontend

| Test | File | AC / decision | What it failed on | Verdict |
|---|---|---|---|---|
| `is offered to a team` | `DataRetrievalWizardRegistry.test.ts` | AC-B1 / DD-1 | no `servicenow.board` row | MISSING_FUNCTIONALITY |
| `shows the reason the board list was refused` | `BoardWizard.test.tsx` | AC-B3 / ADR-126 §2 | the canned "Failed to load boards. Please try again." was rendered instead of the refusal | MISSING_FUNCTIONALITY |
| `names both reasons a connection may have no board to offer` | same | DD-7 / ADR-126 §3 | the dialog said "No boards available for this connection.", which names neither cause | MISSING_FUNCTIONALITY |
| `cannot be confirmed when the board could not be read` | same | AC-B3 / D9 / ADR-126 §2 | the reason never appeared; Confirm was enabled by the all-empty fallback | MISSING_FUNCTIONALITY |
| `does not offer it to someone who cannot open it` | `GeneralSettingsComponent.test.tsx` | DD-8 / ADR-126 §4 | the "Select Jira Board" button rendered for a non-administrator | MISSING_FUNCTIONALITY |

Three existing frontend tests were **replaced rather than deleted**, because each pinned behaviour
this feature changes:

| Replaced | Why it becomes wrong |
|---|---|
| `shows error message when board fetch fails` | asserted the canned retry string that ADR-126 §2 deletes |
| `shows error message when no boards are available` | asserted "No boards available for this connection.", which ADR-126 §3 replaces with copy naming both causes |
| `calls onComplete with empty board information when fetch fails` | **pinned the defect itself** — it asserted that a failed read completes with an all-empty board. Its successor asserts that it cannot be confirmed at all |

### Green on `main`, deliberately — slice 02's pins

| Test | Decision | Why it is green now |
|---|---|---|
| `AServiceNowConnection_CanBeAskedForTheBoardsItAlreadyMaintains` | DD-1 | satisfied by the interface scaffold; it pins that the connector stays on the board port |
| `ATeamWhoseQueryWasTypedByHand_IsSavedWithoutTheInstanceBeingAskedAboutBoards` | AC-B5 | Save does not read boards today and must not start; the picker is a wizard an administrator opens, not a step in saving a team |
| `is not offered to a portfolio` | AC-B1 | no ServiceNow row exists yet, and none may be added for the portfolio context |
| `offers it to a system administrator` | DD-8 | the button renders for everybody today, so the administrator half already holds |

### Live PDI (`ServiceNowWorkTrackingConnectorIntegrationTest`, extended — not forked)

The four Earned Trust assertions ADR-125 asks for, plus AC-B6. **Compiled and skipped, not run** —
the credential is not in this working tree, so their classification is *derived*, not *observed*.

| Test | AC / decision | Verdict | Note |
|---|---|---|---|
| `ABoardsOwnFilter_SelectsLessWorkThanTheWholeTableItRunsAgainst` | Earned Trust 1 | expected GREEN (substrate pin) | the filter stopped being column-form is the lie it catches |
| `TheFilterAsItReadsOnScreen_SelectsTheWholeTable` | Earned Trust 2 | expected GREEN (substrate pin) | 105/105 and 118/118, measured 2026-08-01 |
| `AnAccountThatSharesNoBoard_IsAnsweredWithAnEmptySuccessWhoseCountStillNamesEveryBoard` | Earned Trust 3 + 4 | expected GREEN (substrate pin) | header 2 / body 0; a `403` here would move the empty-list copy from honest to false |
| `ABoardPickedOnTheInstance_PreFillsTheWorkItsOwnFilterSelects` | AC-B6 | MISSING_FUNCTIONALITY (**not executed**) | goes through `GetBoards` / `GetBoardInformation`, which do not read anything yet |

**Honest limit**: the three substrate pins assert instance behaviour rather than Lighthouse
behaviour, so they should be green the first time they run — but nobody has run them. DELIVER must
run all four against the PDI before calling slice 02 done; DoD item 5 already says so and is not
negotiable. The dogfood moment in `slices/slice-02-visual-task-board-picker.md` is the same run.

## Verdict

**27 of 27 executed failures are `MISSING_FUNCTIONALITY`** — 15 backend, 12 frontend. **Zero BROKEN,
zero WRONG_ASSERTION, zero OBSERVABLE_NOT_AT_PORT.** Four live-PDI tests are classified by
construction and must be run against the instance in DELIVER. Handoff to DELIVER is not blocked.

## Commands

```
# backend, before the skip markers were applied
dotnet build Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj
#   Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test --filter "FullyQualifiedName~ServiceNowBoardPickerTest|FullyQualifiedName~DataRetrievalSchemaDtoTest|FullyQualifiedName~ServiceNowConnectionAcceptanceTest|FullyQualifiedName~ServiceNowValidationVerdictPurityArchUnitTest"
#   Failed: 15, Passed: 28, Total: 43

# backend, after
dotnet test --filter "FullyQualifiedName~ServiceNow|FullyQualifiedName~DataRetrievalSchemaDtoTest|FullyQualifiedName~WizardsControllerTest"
#   Passed! Failed: 0, Passed: 309, Skipped: 19, Total: 328

# frontend, before
pnpm exec vitest run src/models/Common src/components/DataRetrievalWizards src/components/Common/BaseSettings/GeneralSettingsComponent.test.tsx
#   Test Files 5 failed | 1 passed (6) | Tests 12 failed | 81 passed (93)

# frontend, after
pnpm exec vitest run src/models/Common src/components/DataRetrievalWizards src/components/Common/BaseSettings/GeneralSettingsComponent.test.tsx
#   Test Files 5 passed | 1 skipped (6) | Tests 81 passed | 12 skipped (93)
pnpm exec tsc -b            # exit 0
pnpm biome check ./src      # Checked 665 files. No fixes applied.
```
