# RED classification — servicenow-pre-close-bucket slice 01

Run: `dotnet test Lighthouse.Backend.Tests --filter "FullyQualifiedName~ServiceNowClassLabelsTest|FullyQualifiedName~ServiceNowRecordClassTest"`, 2026-08-01.
Build: **0 errors**. No test is BROKEN — every failure reaches its assertion.

## Genuinely RED — MISSING_FUNCTIONALITY

| Test | AC | Fails because |
|---|---|---|
| `ServiceNowClassLabelsTest` (23 cases) | AC-D2, ADR-128 | `ServiceNowClassLabels` is a scaffold; both methods throw. |
| `ATeamThatNamesItsWorkTheWayServiceNowDoes_ReadsTheSameWorkAsOneNamingTheRecordClass` | AC-B1 | Labels reach the query verbatim: `sys_class_nameINIncident,Change Request`. |
| `WorkItemsOfAKnownKindOfWork_ReportTheKindTheCoachNamedRatherThanTheColumnValue` | AC-D3 | The team reads nothing, so `Type` is never produced. |
| `ATeamNamingItsWorkByLabel_EndsUpWithConfigAndWorkItemsSpeakingTheSameVocabulary` | **AC-D1** | Same cause. This is the silent-zero guard. |
| `ATeamNamingItsWorkByLabel_StillLooksForStateHistoryOnTheRecordClasses` | AC-D5 | `metric_definition` is asked about `Incident`, not `incident`. |
| `ATeamNamingAKindOfWorkItCannotSee_IsRefusedInTheWordsTheCoachTyped` | AC-D4 | No refusal at all — the label matches nothing, so nothing is hidden to report. |

## Green at DISTILL — regression guards, NOT red

Two tests pass today. Recorded explicitly so DELIVER does not read them as coverage of new behaviour.

| Test | Why it is green now | What it guards |
|---|---|---|
| `ATeamNamingItsWorkByRecordClass_EndsUpWithConfigAndWorkItemsSpeakingTheSameVocabulary` | Class names are today's only vocabulary, so config and `Type` trivially agree. | That introducing the map does not break the teams that already exist. |
| `ATeamNamingAKindOfWorkLighthouseDoesNotKnow_AsksForItAndReportsItUnchanged` | Passthrough is today's only behaviour, because there is no map to miss. | That the map, once added, does not mangle a custom class. |

**Both were nearly authored as vacuous passes.** The first was originally a single `[Test]` covering only the class-name configuration — green, and blind to the entire feature. Split into a two-case `[TestCase]` so the label half is genuinely RED. Kept as a pair because the class-name case is the regression the change actually risks.

## Not authored — would be BROKEN, not RED

**The deep-link ATs (AC-A1–AC-A3, AC-A7).** `ServiceNowWorkItemMapper.MapRecord(record, owner, table)` has no instance URL, and DD-5 needs one. A test written against the new signature does not compile, which fails the *whole* test project — every test BROKEN, not one test RED. That blocks the gate rather than driving it.

Authored in DELIVER's RED phase together with the signature change, in `ServiceNowWorkItemMapperTest`. Deliberate deviation from ADR-025's "DISTILL authors all ATs", recorded rather than silently skipped.

## Zero pre-existing tests broken

All 6 original `ServiceNowRecordClassTest` tests still pass. The only production change so far is the additive `ServiceNowClassLabels` scaffold, which nothing calls yet.
