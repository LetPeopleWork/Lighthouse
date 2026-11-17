# Architecture brief: story\-6625\-baseline\-report\-creation

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Now is derived, never stored: every read computes the Now window as the window of Then length days ending on the instance today. The stored payload member nowLengthDays is never read or written again and stays in the JSON payload unread (expand-only). No migration, no data update, no concurrency-version change. This resolves the DISCUSS open item on snap and version bump: nothing is migrated, so no Edit is refused as stale because of this change.
```

#### D2

```text
Carriers and owners: the value object ReportWindow (Models/Reports/ThenAndNowPayload.cs) gains NowEndingOn(today), so a Now window can only be built from a Then window and cannot take another length. The pure static ReportWindowPolicy (Services/Implementation/Reports/ReportWindowPolicy.cs) owns window refusals: NowLengthRefusal(requestedNowLengthDays, thenLengthDays) replaces NowRefusal and NowWindow. ReportRefusalCodes gains now-length-differs-from-then; now-too-short, now-too-long and now-before-cutoff stay as constants because their spelling is a client contract, but are no longer emitted. ThenAndNowTemplate stays the template that freezes, edits and reads, and delegates every window decision to those two owners. Errors stay ReportOutcome Refused(code), mapped to 400 ProblemDetails by ReportsControllerBase; a stale save stays 409.
```

#### D3

```text
Cross-layer algebra. Domain: states are a frozen Then window (start, end, length) and a Now window derived per day; operations EndingOn, NowEndingOn, ThenRefusal, NowLengthRefusal; observation is the read model thenWindow and nowWindow. Application: Create and Edit return Done or Refused(code); Edit returns stale (409) only after a real concurrent save. Adapter: refusal codes map to 400 ProblemDetails, unchanged. Infrastructure: no change, no migration, no recovery path added.
```

#### D4

```text
Claim-to-checker: the state and request claims (Now length equals Then length on every read; accepted requests carry no Now or an equal one; no stale refusal from the deploy; Edit never refused for kept history; a valid Then implies a Now inside kept history; Total and per-day changes never opposed with equal lengths) are checked with Quint (state model, quint run simulation with behavioural mutants) in docs/feature/story-6625-baseline-report-creation/evidence/quint/design.qnt. The window-relation and total-line claims reuse the DISCUSS model evidence/quint/baseline.qnt. Agda is not selected: no general theorem beyond these finite rules is claimed. TLA+/TLC is not selected: there is no temporal or fairness claim.
```

#### D5

```text
Liveness is not applicable: every create, edit and read is a synchronous request answered once, and no claim promises eventual progress. The daily movement of Now follows the instance clock, not a system process.
```

#### D6

```text
Second stressor walk over the finished design (walkthrough, not execution): the day changes while the wizard is open (today comes from the keyed templates query refetched each time the wizard opens, and the server derives Now at create, so the saved report is still equal length); the oldest kept day moves later after creation (Then is frozen and Edit never re-checks it, P8); a client sends nowLengthDays 0 or a negative value (not equal to Then, refused with the same code); a payload written by a later version carries unknown members (Edit still edits the JSON tree and writes nothing about windows). None showed a new residue.
```

#### D7

```text
Test substrate. Backend: new acceptance scenarios at the application-service port in Lighthouse.Backend/Lighthouse.Backend.Tests/Acceptance/BaselineReportCreation/ (namespace Lighthouse.Backend.Tests.Acceptance.BaselineReportCreation), built with ServicePortHarness and FakeLighthouseClock; no new host test. Frontend: Vitest with React Testing Library; rules on functions in .test.ts files, one render per wiring in .test.tsx files. The Lighthouse test-writing skill wins over nWave per-test timing and PBT-density rules. Mutation: new configs Lighthouse.Backend/Lighthouse.Backend.Tests/stryker.6625.backend.json (ReportWindowPolicy, ReportWindow, ThenAndNowTemplate; its test-case filter names Tests.Acceptance.BaselineReportCreation) and Lighthouse.Frontend/stryker.6625.frontend.json (thenAndNowDraft.ts, reportWindowCheck.ts, thenNowComparison.ts, ThroughputPanel.tsx), run once on frozen code, 80 percent kill minimum.
```

#### D8

```text
Context map unchanged: Reports context only, no new glossary term, no domain event, no new external call. ADR-270 and ARCHITECTURE.md section 4 need no change.
```

### Constraints

#### K1

```text
Then length: at least 14 days (ReportWindowPolicy.MinimumLengthDays); no upper bound other than the first day the calendar can name (then-too-long).
```

#### K2

```text
Then end: on or before the instance today. Then start: on or after the oldest kept day of the owner; no bound when the owner keeps every finished item.
```

#### K3

```text
Now length in a create or edit request: absent, or exactly equal to the Then length. Every other value is refused, including 0 and negative values.
```

#### K4

```text
EF migrations are expand-only: this feature adds no migration and drops no column; the unread payload member stays.
```

#### K5

```text
Copy decided at the start of DELIVER (feature-delta.md, maintainer decisions 2026-11-17): the too-little-history line reads '{owner name} keeps history from {d MMM yyyy}: too little for a {L}-day Then and Now. Pick a shorter length or a later end date.' and the Throughput Total change reads like '▲ 13 (+25 %)'. Configurable terms render through the terminology hook.
```

#### K6

```text
Loading and failure looks: the Then step fetches nothing new. Today, the oldest kept day and the minimum length come from the existing keyed reportTemplates query; while it loads, the wizard shows its existing lone spinner with Next disabled, and on failure its existing could-not-load alert. The bulk owner step keeps its existing owner-limit looks. The report view and the Throughput panel keep the existing report loading and failure looks.
```

### Reuse

ReportWindow

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ThenAndNowPayload\.cs\:26

Extend

```text
Existing value object for an inclusive window; NowEndingOn(today) makes equal length a property of construction instead of a caller rule.
```

ReportWindowPolicy

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs\:9

Extend

```text
The single existing owner of window refusals, already held pure by architecture rule E15; NowLengthRefusal replaces NowRefusal.
```

ReportRefusalCodes

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportRefusalCodes\.cs\:4

Extend

```text
Existing owner of refusal code spellings; one code is added and none is renamed.
```

ThenAndNowTemplate

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:14

Extend

```text
Existing template that freezes, edits and reads Then and Now; its own Now handling is replaced by calls to the two window owners.
```

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Every producer and consumer of NewReportRequest, ReportEditRequest and the refusal codes is in one deployable and changes in the same commit; Lighthouse-Clients only lists and reads reports and never sends a Now length.
```

### Boundaries

```text
IReportCommands (Create, Edit) and IReportBulkCommands (create for several owners) for writes; IReportQueries (Get, List, Templates) for reads; the frontend ReportWizard and BulkReportWizard reach them through the reports HTTP API
```

#### Driven ports

```text
IReportReader
```

```text
IReportWriter
```

```text
IReportMetricSeries
```

```text
ILighthouseClock
```

```text
API controllers to application services to ThenAndNowTemplate to ReportWindow and ReportWindowPolicy; the pure policies depend on no port and no clock
```

#### Failures

##### B1

```text
A create or edit carries a Now length different from the Then length
```

Refusal

```text
Refused now-length-differs-from-then, HTTP 400 ProblemDetails; nothing is written
```

##### B2

```text
At create, Then is shorter than 14 days, ends after today, or starts before the oldest kept day
```

Refusal

```text
Existing then-* codes, HTTP 400; the wizard shows the refusal before Next
```

##### B3

```text
An Edit is saved after somebody else saved the same report
```

Refusal

```text
HTTP 409 with the existing stale message
```

### Observations

```text
Read model thenAndNow.thenWindow and thenAndNow.nowWindow (start, end, lengthDays) of a report on a given instance day
```

ThenAndNowRead

```text
Whether the stored payload still holds a legacy nowLengthDays member
```

```text
Outcome of a create or edit: Done, Refused with a refusal code (400 ProblemDetails), or stale (409)
```

ReportOutcome

```text
Which later checks would also have failed; checks stop at the first refusal
```

### Equality

ThenAndNowRead

```text
Two reads are equal when their Then and Now windows have the same start, end and length on the same instance day and the shown metric values are the same
```

### Constructors

None.

### Laws

#### L1

```text
An edit is refused as stale only after a real concurrent save of the same report; this change bumps no version
```

Invariant

Application

ThenAndNowTemplate

##### Treatment

NoTest

```text
Holds by construction: no migration and no write of any version exists in this change; the existing 409 test of the concurrency token stays; model evidence in runs/design-invStaleOnlyAfterRealChange.meta
```

##### Evidence

None.

#### L2

```text
A Then accepted at create has a Now of the same length that starts on or after the oldest kept day
```

Invariant

Domain

ReportWindowPolicy

##### Treatment

Broad

###### Generator

```text
today fixed; Then length 14..400; Then end 0..800 days before today; oldest kept day null or 0..900 days before today
```

length \<\= 400\, offsets \<\= 900

fixed seed per run\, printed on failure

###### Must include

```text
Then ending today
```

```text
Then starting exactly on the oldest kept day
```

None.

```text
Native FsCheck or seeded generation with smallest-first replay of the failing case
```

```text
Now start computed independently as today minus length plus one, compared with the oldest kept day
```

```text
A policy that accepts a Then starting before the oldest kept day
```

##### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-invCreatedNowInsideKeptHistory\.meta

#### L3

```text
With equal lengths, the Throughput Total change and the per-day change never point in opposite directions, so Throughput is never judged mixed by its own lines
```

Invariant

Domain

ThroughputPanel

##### Treatment

Broad

###### Generator

```text
Then total 0..500, Now total 0..500, length 14..400
```

totals \<\= 500\, length \<\= 400

fast\-check fixed seed\, printed on failure

###### Must include

```text
Totals differing by 1 over a long window
```

```text
Both totals 0
```

None.

```text
fast-check integer shrinking
```

```text
Sign of Now total minus Then total, computed apart from lineComparisonsOf
```

```text
Per day computed over a Now of another length
```

##### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-invTotalAndPerDayAgree\.meta

#### L4

```text
For every report and every instance day, nowWindow.lengthDays equals thenWindow.lengthDays and nowWindow.end equals today
```

Invariant

Domain

ReportWindow

##### Treatment

Finite

###### Cases

```text
Legacy payload Then 30, stored Now 45: Now 30 days ending today
```

```text
New report Then 14 ending today: Now equals Then
```

```text
Then 90 ending 45 days ago: Now 90 days ending today
```

```text
The input domain that matters is closed: legacy payload with an unequal stored Now, no stored Now, Then ending today, Then in the past
```

##### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-invShownNowEqThen\.meta

#### L5

```text
A create or edit is accepted only when its Now length is absent or equal to the Then length; otherwise it is refused with now-length-differs-from-then and nothing is written
```

Invariant

Application

ReportWindowPolicy

##### Treatment

Finite

###### Cases

```text
Then 90, Now 45: refused
```

```text
Then 90, Now 90: accepted
```

```text
Then 90, no Now: accepted
```

```text
Then 90, Now 0: refused
```

```text
Three outcome classes (absent, equal, different) cover the rule; boundary rows pin each
```

##### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-invAcceptOnlyEqualOrAbsentNow\.meta

#### L6

```text
Edit never refuses for kept history, so a report whose Then starts before the oldest kept day stays editable
```

Invariant

Application

ThenAndNowTemplate

##### Treatment

Finite

###### Cases

```text
Then starts one day before the oldest kept day: edit of name and shown metrics accepted
```

```text
One boundary decides it: Then start one day before the oldest kept day
```

##### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-invEditNeverRefusedForHistory\.meta

### Refuted

```text
A one-time snap of saved Now lengths keeps every open Edit safe
```

```text
A snap that bumps the concurrency version makes an Edit opened before it stale though nobody changed the report; without a bump there is a gap between deploy and migration. Carrier diagnosis: storing a derivable fact made the state representable; deriving Now removes it.
```

```text
docs/feature/story-6625-baseline-report-creation/evidence/quint/runs/design-mutant-bump.out: the deploy bumps the version of a 30/45 report while an Edit is open; the save is refused as stale with no concurrent save
```

### Claims

#### C1

```text
Every Then and Now report read shows a Now window exactly as long as its Then window, ending today
```

```text
A reader compares windows of different lengths and draws a false conclusion from Throughput totals and other counts
```

ReportWindow

```text
A frozen Then window from the payload and the instance today
```

ReportWindow\.NowEndingOn\(today\)

ThenAndNowTemplate\.Read

```text
IReportQueries Get returns nowWindow.lengthDays equal to thenWindow.lengthDays
```

```text
A payload without thenWindow throws, as today; a previous Lighthouse version after rollback reads the stored member again (recorded residue); no other type computes a Now window (architecture check)
```

#### C2

```text
A create or edit request is accepted only with no Now length or one equal to the Then length
```

```text
An old page or client silently saves a Now of another length
```

ReportWindowPolicy

```text
NewReportRequest or ReportEditRequest with an optional thenAndNow.nowLengthDays
```

ReportWindowPolicy\.NowLengthRefusal\(requestedNowLengthDays\, thenLengthDays\)

ThenAndNowTemplate\.Freeze and ThenAndNowTemplate\.Edit

```text
Refused with code now-length-differs-from-then, HTTP 400
```

```text
None in production code: single and bulk create both go through ThenAndNowTemplate.Freeze
```

### States

Admitted

```text
Legacy report whose payload holds a nowLengthDays different from its Then length: the read shows a Now of Then length and the member is kept unread
```

None.

Admitted

```text
Report whose Then starts before the current oldest kept day: readable and editable (name, shown metrics)
```

None.

Admitted

```text
Then ending today: Then and Now are the same days on that day
```

None.

Excluded

```text
A report shown with a Now length different from its Then length
```

None.

Excluded

```text
A report created by this version with a stored Now length
```

None.

Unknown

```text
After a rollback to a previous Lighthouse version, legacy reports show their stored Now length again
```

maintainer

### Models

Quint

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/design\.qnt

##### Properties

invShownNowEqThen

Invariant

```text
After deploy every report shows a Now exactly as long as its Then
```

ThenAndNowTemplate\.Read

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:95

invNoStoredNowOnCreate

Invariant

```text
A report created by this version stores no Now length
```

ThenAndNowTemplate\.Freeze

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:24

invAcceptOnlyEqualOrAbsentNow

Invariant

```text
An accepted create or edit carried no Now length or one equal to the Then length
```

ReportWindowPolicy\.NowLengthRefusal

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs\:56

invStaleOnlyAfterRealChange

Invariant

```text
An edit is refused as stale only after somebody else saved the same report meanwhile
```

ReportCommands\.Edit

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportCommands\.cs\:1

invEditNeverRefusedForHistory

Invariant

```text
An edit is never refused because Then starts before the oldest kept day
```

ThenAndNowTemplate\.Edit

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:66

invCreatedNowInsideKeptHistory

Invariant

```text
The Now of an accepted create starts on or after the oldest kept day
```

ReportWindowPolicy\.ThenRefusal

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs\:32

invTotalAndPerDayAgree

Invariant

```text
With equal lengths the Throughput Total change and per-day change never point in opposite directions
```

lineComparisonsOf

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts\:123

##### Atoms

State

reports

Report\.TemplatePayloadJson

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ThenAndNowPayload\.cs\:10

```text
One row write per create or edit through IReportWriter
```

State

cutoff

ReportWindowPolicy\.EarliestStart

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs\:17

None.

Action

create

ThenAndNowTemplate\.Freeze

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:24

```text
Validation is pure; the single row insert is the only write
```

Action

saveEdit

ThenAndNowTemplate\.Edit

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:66

```text
Concurrency token compare-and-set on the row, existing
```

Action

shownNowLen

ThenAndNowTemplate\.Read

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:95

None.

Action

deploy

none\: release of this version\; model\-only

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/design\.qnt\:80

None.

Constant

perDayHundredths

roundForDisplay

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts\:44

None.

##### Assumptions

```text
Simulation with quint run, max 10 steps, 20000 samples, seed 6625; not exhaustive
```

```text
Day 1000 is 2026-11-15; windows inclusive at both ends
```

```text
Lengths 14, 30, 45, 90; Then end 0, 15, 31 or 45 days before today; oldest kept day 800 or 960, moving one day per day
```

```text
Per day rounded to hundredths half away from zero, as the shipped panel rounds to 2 decimals
```

##### Mutants

###### N1

```text
Read uses the stored Now length after deploy; kills invShownNowEqThen (runs/design-mutant-read-stored.out)
```

###### Kills

None.

###### N2

```text
Create stores the requested Now length; kills invNoStoredNowOnCreate (runs/design-mutant-store-now.out)
```

###### Kills

None.

###### N3

```text
Now rule skipped on create and edit; kills invAcceptOnlyEqualOrAbsentNow (runs/design-mutant-skip-now-check.out)
```

###### Kills

None.

###### N4

```text
Deploy snaps Now and bumps the concurrency version; kills invStaleOnlyAfterRealChange (runs/design-mutant-bump.out)
```

###### Kills

None.

###### N5

```text
Edit re-checks Then against kept history; kills invEditNeverRefusedForHistory (runs/design-mutant-edit-checks-history.out)
```

###### Kills

None.

###### N6

```text
Create skips the kept-history check on Then; kills invCreatedNowInsideKeptHistory (runs/design-mutant-skip-history.out)
```

###### Kills

None.

###### N7

```text
Per day computed over a Now 15 days longer than Then; kills invTotalAndPerDayAgree (runs/design-mutant-unequal-lengths.out)
```

###### Kills

None.

Quint

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/baseline\.qnt

##### Properties

invOverlapIffShareDay

Invariant

```text
The overlap line is shown exactly when Then and Now share a day
```

windowsRelationOf

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:1

invOverlapUntilIsLastSharedDay

Invariant

```text
The overlap line names the last shared day, which is the last day of Then
```

windowsRelationOf

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:1

invChangeIffBothCaptured

Invariant

```text
The Total line shows a change only when Then and Now totals are both captured
```

lineComparisonsOf

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts\:123

invPctOnlyWhenThenPositive

Invariant

```text
A percent change is shown only when the Then total is above 0
```

compareThenNow

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts\:90

##### Atoms

None.

##### Assumptions

```text
DISCUSS model reused for the window-relation and Throughput total-line claims; simulation with quint run, max 12 steps, seed 6625; not exhaustive
```

```text
Per day rounded to tenths in this model, coarser than the shipped 2 decimals
```

##### Mutants

None.

### Closure

```text
Now derived from Then on every read
```

Safety

Pass

DesignedNotBuilt

```text
Quint invariant invShownNowEqThen held; mutant read-stored killed
```

##### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-invShownNowEqThen\.meta

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-mutant\-read\-stored\.meta

```text
Now derived from Then on every read
```

Inhabitation

Pass

DesignedNotBuilt

```text
A legacy 30/45 payload and a new report both build a Now window through ReportWindow.NowEndingOn from an admitted Then window
```

##### Evidence

None.

```text
Now derived from Then on every read
```

PublicReachability

Pass

DesignedNotBuilt

```text
payload to ThenAndNowTemplate.Read to IReportQueries.Get to the reports API to the report view and PDF
```

##### Evidence

None.

```text
Now derived from Then on every read
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: synchronous request and response; no eventual-progress claim
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: this version stays deployed. Negated: rollback to the previous version
```

```text
Then windows, frozen values and past PDF exports
```

```text
Legacy reports show their old stored Now length again; reports created by this version show Now equal to Then because the old reader falls back to the Then length
```

```text
Data loss or a refused read
```

```text
Accepted by the maintainer: old stored Now lengths reappear after a rollback
```

maintainer

#### Y2

Stressor

```text
Assumption: every client sends the new request shape. Negated: a page opened before the deploy resends Now 45 on Edit
```

```text
The stored report
```

```text
Refusal with now-length-differs-from-then and its clear message
```

```text
A silent save of an unequal Now, or a stale refusal caused by the deploy
```

```text
The user reloads the page and saves again
```

ThenAndNowTemplate

#### Y3

Bypass

```text
Deserialization of TemplatePayloadJson, and any outside writer of the Reports table
```

```text
Reads derive Now from thenWindow whatever nowLengthDays member is present
```

```text
A foreign nowLengthDays member stays unread
```

```text
A read that uses a stored Now length
```

```text
A payload without thenWindow still throws, as before this change
```

ThenAndNowTemplate

#### Y4

SinglePointOfFailure

```text
ReportWindow.NowEndingOn is the only producer of a Now window
```

```text
One definition of Now for the read, the PDF export and the overview
```

```text
A defect there affects every report the same way and shows in the shared tests
```

```text
A second Now computation that disagrees
```

```text
Held by the architecture check and the port scenarios
```

ReportWindow

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts

Extend

```text
Remove nowLength and thenEndsBeforeNow; back-to-back default end; windowsOf and windowsRelationOf as recorded in this slice's constructors; newReportOf and newReportForOwnersOf send no Now length; reportChangeOf sends the frozen Then length as Now length until S2 drops it; thenAndNowDraftOf drops nowLength
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/ThenStep\.tsx

Extend

```text
Show Then and Now by date and the relation line; the too-little-history line names the owner from the ownerName step prop
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/NowStep\.tsx

Extend

```text
Deleted: the Now step and the bulk Same as Then / Custom choice go
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportTypes\.ts

Extend

```text
Drop the Now step from the Then & Now steps (create and edit share the list); ReportTypeStepProps gains optional ownerName
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportWizardRules\.ts

Extend

```text
Edit now opens on Metrics & name, index unchanged; comment updated
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportWindowCheck\.ts

Extend

```text
Remove the Now side of the check
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.ts

Extend

```text
then-before-cutoff in the single-owner wizard reads '{owner name} keeps history from {d MMM yyyy}: too little for a {L}-day Then and Now. Pick a shorter length or a later end date.'; RefusalWords gains the owner name and the Then length; with no owner name it reads 'This {Team or Portfolio term}'; the bulk not-created line for then-before-cutoff stays as it is; now-* copies may go because the wizard and the server no longer produce them on create
```

Lighthouse\.Frontend\/src\/pages\/Reports\/ownerLimitOfWindow\.ts

Extend

```text
Remove the per-owner Now branch; a valid Then always fits a same-length Now
```

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts

Extend

```text
INewReport loses nowLengthDays; IReportOwner and ReportChange stay unchanged in this slice
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs

Extend

```text
Add NowLengthRefusal(requestedNowLengthDays, thenLengthDays): null when absent or equal, now-length-differs-from-then otherwise; NowRefusal and NowWindow stay until S2 because Edit and Read still call them
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportRefusalCodes\.cs

Extend

```text
Add now-length-differs-from-then
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs

Extend

```text
Freeze checks Then as today, then NowLengthRefusal, and writes no nowLengthDays member; Read and Edit are unchanged in this slice (a payload without the member already reads a Now as long as Then)
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.tsx

Extend

```text
Optional ownerName prop, passed to the create ReportWizard
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/ReportWizard\.tsx

Extend

```text
Optional ownerName prop, passed to every step and to the refusal words; the Edit mount leaves it out because Then is frozen there
```

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx

Extend

```text
Pass team.name to ReportsTab as ownerName
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx

Extend

```text
Pass portfolio.name to ReportsTab as ownerName
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.createWizard\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/Reports/ReportsTab.createWizard.test.tsx src/pages/Common/Reports/wizard/thenAndNow/thenAndNowDraft.test.ts src/pages/Common/Reports/wizard/reportWindowCheck.test.ts src/pages/Reports/BulkReportWizard.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Acceptance.BaselineReportCreation
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Reports src/pages/Common/Reports src/services/Api/ReportService.test.ts src/pages/Teams/Detail/TeamDetail.reportsTab.test.tsx src/pages/Portfolios/Detail/PortfolioDetail.reportsTab.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter 'FullyQualifiedName~Lighthouse.Backend.Tests.API.Integration.Reports|FullyQualifiedName~ThenAndNowTemplateTest'
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportWindowCheck\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.test\.tsx

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/CreateReportWithEqualNowScenarios\.cs

Lighthouse\.Frontend\/src\/tests\/ReportsTestKit\.tsx

Lighthouse\.Frontend\/src\/tests\/BulkReportsTestKit\.tsx

Lighthouse\.Frontend\/src\/pages\/Reports\/bulkWizardSteps\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.portfolios\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.ownerLimits\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Reports\/ownerLimitOfWindow\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportTypes\.test\.ts

Lighthouse\.Frontend\/src\/services\/Api\/ReportService\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportView\.editWizard\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.reportsTab\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.reportsTab\.test\.tsx

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice01TeamReportScenarios\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice05WipAndWorkItemAgeScenarios\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice07NowLengthScenarios\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportWindowBoundaryScenarios\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs

#### Public oracle

```text
On instance day 15 Nov 2026 a Team Admin opens Create Report on the Team Reports tab, picks Then & Now and a Then length of 30 days, keeps the default end, then creates the report; variants set Then to end 15 Nov 2026 with 14 days, to end 31 Oct 2026 with 30 days, and pick a length the owner kept history cannot hold; the bulk wizard on the Reports page creates for several Teams with 30 days
```

```text
The Then step shows Then 17 Sep - 16 Oct 2026 and Now 17 Oct - 15 Nov 2026 with no overlap line and the wizard has no Now step; Then ending today shows the same-days line and can be created; Then ending 31 Oct shows Now 17 Oct - 15 Nov 2026 and the overlap-until line naming 31 Oct 2026; the too-little-history case shows a refusal naming the owner and Next stays disabled; every create request carries no Now length; every saved report (single and bulk) reads back a 30-day Now ending today
```

```text
A wizard that keeps the Now step, defaults Then to end today, shows no Now dates or a wrong overlap day, lets Next through on too little history, or a server that stores or applies a Now length different from Then
```

##### Promises

P1

P2

P3

P4

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

```text
Owner name for the too-little-history line: passed down, not fetched. TeamDetail passes team.name and PortfolioDetail passes portfolio.name to ReportsTab as an optional ownerName prop; ReportsTab hands it to the create ReportWizard; ReportWizard hands it to every step through ReportTypeStepProps.ownerName and to the refusal words, so both the Then step's own check and a then-before-cutoff refusal from the server after Create name the owner. No new request, no wire change, IReportOwner and IReportTemplates unchanged, so the Then step still fetches nothing new. The Edit mount (useReportEditing) passes none: Then is frozen there and never refused. Without a name the line reads 'This {Team or Portfolio term} keeps history from ...'. The bulk wizard keeps its own per-owner not-created line. Test kit fixture fact: ReportsTestKit exports the owner names the oracle passes as ownerName, 'Lightspeed' for LIGHTSPEED (team 1) and 'Apollo' for APOLLO (portfolio 3); TeamDetail.reportsTab.test.tsx and PortfolioDetail.reportsTab.test.tsx observe the detail pages handing the loaded name to ReportsTab.
```

```text
Window contract in thenAndNowDraft.ts, consumed by ThenStep and checked by the window law of this slice: windowsOf(draft, templates) returns the Then window (start = Then end - L + 1, end = thenEndOf, lengthDays = L) and the Now window (start = today - L + 1, end = today, lengthDays = L), L = lengthOf(draft.thenLength), today = templates.today, or null while the end or the length is not a valid day or whole number. windowsRelationOf(draft, templates) returns null in the same case, else {kind: 'sameDays'} when Then ends today, {kind: 'overlapUntil', until} with until = Then's last day as yyyy-MM-dd when today - L + 1 <= Then end < today, and {kind: 'apart'} when Then end < today - L + 1. ThenStep renders apart as no line, sameDays as 'Now and Then are the same days today. Now moves forward from tomorrow; the comparison becomes meaningful as days pass.' and overlapUntil as 'Now overlaps Then until {until as d MMM yyyy}'. The default (thenEnd null) ends Then on today - L, so the default relation is apart.
```

```text
Backend scope of this slice is Create only. ReportWindowPolicy gains NowLengthRefusal; ThenAndNowTemplate.Freeze calls it after the Then checks and stores no nowLengthDays. ThenAndNowPayload.NowLengthDays, ReportWindowPolicy.NowRefusal and NowWindow, NewThenAndNow.NowLengthDays, NewReportRequest and the create logs stay unchanged: Read and Edit still use them until S2 derives Now and refuses an unequal edit. A payload written by this slice has no member, so the unchanged Read already shows a Now as long as Then. ReportWindow.NowEndingOn and the architectural test that only ReportWindow and ReportWindowPolicy build a Now window move to S2, the slice that changes the Now derivation.
```

```text
The create and edit wizards share one step list, so removing the Now step here removes it from Edit too: Edit opens on Metrics & name (the first editable index stays 2) and asks no Now question. Until S2, reportChangeOf sends the report's frozen Then length as nowLengthDays, which the unchanged edit path accepts. S1 owns the Now-step part of ReportView.editWizard.test.tsx and of ReportsTab.test.tsx; S2 keeps ReportView.editWizard.test.tsx for its own edit observations.
```

```text
Supports: every test this slice breaks is declared and changes its expectation to the new behaviour; a row that only exercised the removed Now choice may be deleted with its reason. Left out because this slice does not break them: BulkReportWizard.openings.test.tsx, BulkReportWizard.ownerStepLooks.test.tsx and ReportsPage.usageData.test.tsx reach the owner step only through BulkReportsTestKit; reportWindowBoundaries.json keeps its Now rows for the backend NowRefusal unit rows until S2; ReportWindowPolicyTest, ThenAndNowTemplateEditTest, ReportLoggingTest, ReportBulkCommandsTest, ReportQueriesTest, CycleTimeReportMetricTest, ThenAndNowPayloadRoundTripTest and TeamReportsControllerTest touch only Edit, Read, logs or stubbed ports this slice leaves unchanged; Slice02, Slice04, Slice05b, ReportCreationTimingProbe, BulkReportsAcceptanceTest, CreateForSeveralTeamsScenarios and PortfoliosJoinTheSelectionScenarios already create with equal or no Now; Slice10EditReportScenarios and ReportsWithoutSignInScenarios are edits, S2. No cut: narrowing the backend to Create keeps most backend tests untouched, the remaining support edits are expectation updates of one kind (no Now step, no Now length), and splitting the bulk wizard off would split one product slice.
```

##### Constraints

None.

##### Reuse

ThenAndNowDraft

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:24

Extend

```text
Existing owner of the wizard draft rules; dayBeforeNowStarts already gives the back-to-back end for several owners
```

ThenStep

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/ThenStep\.tsx\:15

Extend

```text
Existing step already shows the Then days line and refusal; the Now line and relation line join it
```

reportWindowCheck

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportWindowCheck\.ts\:88

Extend

```text
Existing frontend mirror of the server window rules; only the Now side goes
```

limitOfFor

Lighthouse\.Frontend\/src\/pages\/Reports\/ownerLimitOfWindow\.ts\:16

Extend

```text
Existing per-owner limit; the Now branch becomes unreachable and is removed
```

NowStep

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/NowStep\.tsx\:1

Replace

```text
The question it asks no longer exists; the Then step shows the derived Now
```

reportTypes

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportTypes\.ts\:74

Extend

```text
Existing registry of Then & Now steps; the Now step leaves it
```

firstStepOf

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportWizardRules\.ts\:17

Reuse

```text
Edit start index stays 2, which now names Metrics & name; only the comment changes
```

cutoffRefusal

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.ts\:79

Extend

```text
Existing copy for then-before-cutoff becomes the too-little-history line naming the owner
```

INewReport

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:120

Extend

```text
Existing create request type; loses nowLengthDays
```

ReportsTab

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.tsx\:43

Extend

```text
The detail pages already mount it with the owner and hold the owner's name; an optional ownerName prop carries the name to the wizard with no new request and no wire change
```

ReportWizard

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/ReportWizard\.tsx\:50

Extend

```text
Already hands owner and templates to each step and builds the refusal words; it hands the owner name on the same way
```

TeamDetail

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:546

Extend

```text
Holds the loaded team, whose name is the owner name the line needs
```

PortfolioDetail

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:470

Extend

```text
Holds the loaded portfolio, whose name is the owner name the line needs
```

IReportTemplates

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:31

Reuse

```text
Considered as the name carrier and rejected: adding the owner name to the templates answer changes the wire and the server for a fact the page already holds
```

IReportOwner

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:6

Reuse

```text
Considered as the name carrier and rejected: every report query key, path and test owner builds it from kind and id; a name in it would touch every caller for one line of copy
```

##### Impact

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:24

Changed

```text
Draft without Now; new relation function
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/ThenStep\.tsx\:15

Changed

```text
Shows Now and the relation line
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/NowStep\.tsx\:1

Changed

```text
Deleted
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportTypes\.ts\:91

Changed

```text
Now step removed from the step list
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportWizardRules\.ts\:15

UnchangedCompatible

```text
Index 2 now names Metrics & name; only the comment changes
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportWindowCheck\.ts\:4

Changed

```text
now-* variants leave the union
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.ts\:88

Changed

```text
Too-little-history copy
```

production

Lighthouse\.Frontend\/src\/pages\/Reports\/ownerLimitOfWindow\.ts\:20

Changed

```text
Now branch removed
```

interface

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:1

Changed

```text
INewReport without nowLengthDays
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs\:56

Changed

```text
NowLengthRefusal replaces NowRefusal
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportRefusalCodes\.cs\:20

Changed

```text
New code; now-* kept, NamesEarliestStart keeps NowBeforeCutoff harmlessly
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:46

Changed

```text
Freeze stores no Now
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateTest\.cs\:1

Migrated

```text
Now-length freeze cases change to the equal-length rule; touched rows move toward the port
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice07NowLengthScenarios\.cs\:1

Migrated

```text
Host scenarios for a chosen Now length no longer hold; replaced by port scenarios, line off HostUsersStillToMove when emptied
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportWindowCheck\.test\.ts\:1

Migrated

```text
Now rows removed from the shared table
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.test\.ts\:1

Migrated

```text
Draft without Now; relation cases added
```

test

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.portfolios\.test\.tsx\:1

Migrated

```text
No Now choice
```

test

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.test\.tsx\:1

Migrated

```text
No Now choice
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.createWizard\.test\.tsx\:1

Migrated

```text
No Now step; back-to-back default
```

build

Lighthouse\.Frontend\/stryker\.6625\.frontend\.json\:1

Changed

```text
New per-feature mutation config
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.tsx\:43

Changed

```text
Optional ownerName prop
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/ReportWizard\.tsx\:44

Changed

```text
Optional ownerName prop handed to steps and refusal words
```

production

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:546

Changed

```text
Passes team.name
```

production

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:470

Changed

```text
Passes portfolio.name
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/useReportEditing\.tsx\:94

UnchangedCompatible

```text
Edit mount passes no ownerName; Then is frozen on edit, so the too-little-history line never shows there
```

production

Lighthouse\.Frontend\/src\/pages\/Reports\/bulkWizardSteps\.ts\:21

UnchangedCompatible

```text
Step labels and OWNER_STEP derive from reportTypes; the owner step becomes step 4 of 4 with no code change
```

production

Lighthouse\.Frontend\/src\/pages\/Reports\/OwnerStep\.tsx\:36

UnchangedCompatible

```text
OwnerLimit window stays Then or Now; limitOfFor now only yields Then
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ThenAndNowPayload\.cs\:16

UnchangedCompatible

```text
Freeze leaves NowLengthDays unset; the member stays readable for old payloads until S2 stops reading it
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/NewReportRequest\.cs\:40

UnchangedCompatible

```text
Optional nowLengthDays still bound and passed through as NewThenAndNow.NowLengthDays; its meaning is now the requested Now length, used only by the refusal
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportReadModels\.cs\:27

UnchangedCompatible

```text
NewThenAndNow keeps NowLengthDays as the requested length
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportCommands\.cs\:138

UnchangedCompatible

```text
Logs the requested Now or the Then length after a successful create; once unequal is refused the two are equal
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportBulkCommands\.cs\:90

UnchangedCompatible

```text
Same as the single create log
```

test

Lighthouse\.Frontend\/src\/tests\/ReportsTestKit\.tsx\:1

Migrated

```text
Owner-name fixture fact; CUTOFF_REFUSAL becomes the too-little-history sentence; theNowLengthField and nowNeedsAtLeast go
```

test

Lighthouse\.Frontend\/src\/tests\/BulkReportsTestKit\.tsx\:132

Migrated

```text
OWNER_STEP becomes Step 4 of 4; SAME_AS_THEN and the Now form of keptHistoryStarts go
```

test

Lighthouse\.Frontend\/src\/pages\/Reports\/bulkWizardSteps\.test\.ts\:1

Migrated

```text
Step labels lose Now; the Now step rows go
```

test

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.ownerLimits\.test\.tsx\:1

Migrated

```text
Rows typing a Now length and expecting the Now limit line go
```

test

Lighthouse\.Frontend\/src\/pages\/Reports\/ownerLimitOfWindow\.test\.ts\:1

Migrated

```text
Now-limit rows go
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportTypes\.test\.ts\:1

Migrated

```text
Now step completion rows go
```

test

Lighthouse\.Frontend\/src\/services\/Api\/ReportService\.test\.ts\:118

Migrated

```text
Create requests carry no nowLengthDays; the edit row keeps it until S2
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportView\.editWizard\.test\.tsx\:96

Migrated

```text
Edit opens on Metrics and name; rows that waited for or set the Now length change or go; the edit request carries the Then length
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportsTab\.test\.tsx\:651

Migrated

```text
Edit rows that read the Now length field change
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.test\.ts\:37

Migrated

```text
then-before-cutoff copy changes; now-* rows go
```

test

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.reportsTab\.test\.tsx\:59

Migrated

```text
Its ReportsTab stand-in shows ownerName so the row observes team.name reaching the tab
```

test

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.reportsTab\.test\.tsx\:29

Migrated

```text
Same for portfolio.name
```

test

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.openings\.test\.tsx\:1

UnchangedCompatible

```text
Reaches the owner step only through the kit's toTheOwnerStep and OWNER_STEP
```

test

Lighthouse\.Frontend\/src\/pages\/Reports\/BulkReportWizard\.ownerStepLooks\.test\.tsx\:54

UnchangedCompatible

```text
Reads only the kit's OWNER_STEP constant
```

test

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.usageData\.test\.tsx\:1

UnchangedCompatible

```text
Reaches the owner step only through the kit
```

test

Lighthouse\.Frontend\/src\/tests\/reportWindowBoundaries\.json\:75

UnchangedCompatible

```text
Now rows stay for the backend NowRefusal unit rows until S2; the frontend and host readers stop using them
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice01TeamReportScenarios\.cs\:404

Migrated

```text
The Now-past-the-calendar create row is unreachable now (unequal is refused first) and goes
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice05WipAndWorkItemAgeScenarios\.cs\:74

Migrated

```text
Creates Then 15 with Now 30; becomes Now 15 or no Now
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice07NowLengthScenarios\.cs\:1

Migrated

```text
Rows creating an unequal Now become the now-length-differs-from-then refusal or go
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportWindowBoundaryScenarios\.cs\:111

Migrated

```text
Then rows send Now equal to the row length; Now rows are no longer reachable by create and are skipped
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAcceptanceTest\.cs\:82

Migrated

```text
May gain the now-length-differs-from-then reason constant
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportWindowPolicyTest\.cs\:1

UnchangedCompatible

```text
NowRefusal and NowWindow stay in this slice
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportLoggingTest\.cs\:1

UnchangedCompatible

```text
Create log fields unchanged; the Now-too-short row is an edit, S2
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/BulkReports\/BulkReportsAcceptanceTest\.cs\:61

UnchangedCompatible

```text
Its comparisons already send equal lengths
```

##### Agreement

NewReportRequest thenAndNow\.nowLengthDays

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:203

Producer

Migrated

```text
Single-owner wizard stops sending a Now length
```

NewReportRequest thenAndNow\.nowLengthDays

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:218

Producer

Migrated

```text
Bulk wizard stops sending a Now length
```

NewReportRequest thenAndNow\.nowLengthDays

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/NewReportRequest\.cs\:22

Consumer

UnchangedCompatible

```text
Binds the optional member and passes it on unchanged
```

NewReportRequest thenAndNow\.nowLengthDays

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportBulkCommands\.cs\:90

Consumer

UnchangedCompatible

```text
Logs it after success, when it is absent or equal to Then
```

NewReportRequest thenAndNow\.nowLengthDays

docs\/feature\/story\-6625\-baseline\-report\-creation\/brief\.md\:1

Producer

UnchangedCompatible

```text
Lighthouse-Clients CLI and MCP only list and read reports and never send a Now length (DISCUSS checklist)
```

NewReportRequest thenAndNow\.nowLengthDays

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:46

Consumer

Migrated

```text
Freeze refuses an unequal value and stores none
```

ReportEditRequest thenAndNow\.nowLengthDays

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:240

Producer

Migrated

```text
Edit sends the frozen Then length, accepted by the unchanged edit path, until S2 sends none
```

ReportEditRequest thenAndNow\.nowLengthDays

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsControllerBase\.cs\:41

Consumer

UnchangedCompatible

```text
Unchanged in this slice
```

##### Not applicable

prefactoring

```text
The Now derivation replaces one expression and the draft change is local to thenAndNowDraft; no structure must move first
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

windowsRelationOf

windowsRelationOf\(draft\: ThenAndNowDraft\, templates\: IReportTemplates\)\: WindowsRelation \| null\; WindowsRelation \= \{kind\: \'apart\'\} \| \{kind\: \'sameDays\'\} \| \{kind\: \'overlapUntil\'\; until\: string\}

windowsOf

windowsOf\(draft\: ThenAndNowDraft\, templates\: IReportTemplates\)\: \{then\: ReportWindow\; now\: ReportWindow\} \| null\; ReportWindow \= \{start\: string\; end\: string\; lengthDays\: number\}

##### Laws

###### L8

```text
In the Then step: the overlap line shows exactly when Then and Now share a day, it names the last day of Then, and the same-days line shows exactly when Then ends today; the default Then ends the day before Now starts, so default windows never overlap
```

Invariant

Domain

thenAndNowDraft

###### Treatment

Broad

###### Generator

```text
today fixed; Then length 14..400; Then end 0..800 days before today or left at its default
```

length \<\= 400\, offset \<\= 800

fast\-check fixed seed\, printed on failure

###### Must include

```text
Then ending today
```

```text
Then ending the day before Now starts
```

```text
Then ending on the day Now starts
```

None.

```text
fast-check integer shrinking
```

```text
Day numbers of both windows compared directly: shared day iff Then end >= today - length + 1
```

```text
A relation that shows overlap one day early or names the Now start as the until day
```

###### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/invOverlapIffShareDay\.meta

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/invOverlapUntilIsLastSharedDay\.meta

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

```text
Create asks no Now question and saves Now equal to Then
```

Safety

Pass

DesignedNotBuilt

```text
Quint invNoStoredNowOnCreate and invAcceptOnlyEqualOrAbsentNow held; mutants store-now and skip-now-check killed
```

###### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-invNoStoredNowOnCreate\.meta

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-mutant\-store\-now\.meta

```text
Create asks no Now question and saves Now equal to Then
```

Inhabitation

Pass

DesignedNotBuilt

```text
A 30-day Then with the default end is accepted and builds a 30-day Now ending today (P2)
```

###### Evidence

None.

```text
Create asks no Now question and saves Now equal to Then
```

PublicReachability

Pass

DesignedNotBuilt

```text
ReportWizard / BulkReportWizard to reports API to IReportCommands.Create to ThenAndNowTemplate.Freeze; read back through IReportQueries.Get
```

###### Evidence

None.

```text
Create asks no Now question and saves Now equal to Then
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: synchronous request and response
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs

Extend

```text
Read derives Now through ReportWindow.NowEndingOn; Edit refuses an unequal Now and writes no window member
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Reports\/IReportCommands\.cs

Extend

```text
ReportEdit carries RequestedNowLengthDays
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsControllerBase\.cs

Extend

```text
Maps the optional Now length into RequestedNowLengthDays
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/ReportEditRequest\.cs

Extend

```text
Optional Now length kept only for the refusal
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts

Extend

```text
reportChangeOf sends no Now length; thenAndNowDraftOf drops nowLength
```

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts

Extend

```text
ReportChange loses thenAndNow.nowLengthDays; ReportRefusalCode gains now-length-differs-from-then
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.ts

Extend

```text
Copy for now-length-differs-from-then: Now is always as long as Then.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ThenAndNowPayload\.cs

Extend

```text
ReportWindow.NowEndingOn(today) builds the Now window from this window's length; the payload record stops carrying NowLengthDays (the JSON member stays, unread)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs

Extend

```text
NowRefusal and NowWindow go once Edit and Read no longer call them
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/BaselineReportCreation\/EqualLengthReportsScenarios\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~Lighthouse.Backend.Tests.Acceptance.BaselineReportCreation
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/Reports/ReportView.editWizard.test.tsx src/pages/Common/Reports/wizard/reportRefusalCopy.test.ts
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsModuleArchUnitTest
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportView\.editWizard\.test\.tsx

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.test\.ts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

#### Public oracle

```text
Through IReportQueries.Get and IReportCommands at the application-service port with a fake clock on 15 Nov 2026: read a report stored with Then 30 days and a legacy stored Now of 45 days; edit its name and shown metrics; edit a report whose Then starts before the oldest kept day; create and edit a 90-day Then report with Now 45, with Now 90 and with no Now; in the frontend open Edit report
```

```text
The legacy report reads a 30-day Now ending today with its Then window and frozen values unchanged and the stored payload member left unchanged; the edits are saved; the old report stays editable; Now 45 is refused with now-length-differs-from-then and nothing is written, Now 90 and no Now are accepted; Edit report opens on Metrics & name, offers no Now length and sends none
```

```text
A read that still shows the stored 45-day Now, an edit that writes a Now length or re-checks kept history, or a server that accepts Now 45
```

##### Promises

P1

P7

P8

P9

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

Edit

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:66

Extend

```text
ThenAndNowTemplate.Edit already edits the JSON tree and keeps unknown members; it stops writing the Now member
```

ReportEdit

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Reports\/IReportCommands\.cs\:6

Extend

```text
Existing edit carrier
```

REFUSAL\_COPY

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.ts\:82

Extend

```text
Exhaustive copy table for refusal codes gains one row
```

ReportsControllerBase

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsControllerBase\.cs\:10

Extend

```text
Existing controller mapping of edit requests
```

ReportEditThenAndNowRequest

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/ReportEditRequest\.cs\:14

Extend

```text
Existing edit DTO keeps the optional member so an unequal Now can be refused
```

reportChangeOf

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:234

Extend

```text
Existing builder of the edit request; stops sending a Now length
```

ReportChange

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:140

Extend

```text
Existing edit request type; loses thenAndNow.nowLengthDays
```

ReportWindow

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ThenAndNowPayload\.cs\:26

Extend

```text
Existing inclusive window value object; NowEndingOn makes equal length a property of construction
```

ReportWindowPolicy

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs\:9

Extend

```text
Single owner of window refusals; its Now members leave with their last callers
```

##### Impact

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:82

Changed

```text
Edit Now rule; no Now write
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:100

Changed

```text
Read derives Now
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Reports\/IReportCommands\.cs\:6

Changed

```text
RequestedNowLengthDays
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsControllerBase\.cs\:41

Changed

```text
Mapping
```

interface

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/ReportEditRequest\.cs\:16

UnchangedCompatible

```text
Member kept; meaning narrowed to refusal input
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:234

Changed

```text
No Now length in an edit
```

interface

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:1

Changed

```text
ReportChange and ReportRefusalCode
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.ts\:82

Changed

```text
New copy row
```

consumer

Lighthouse\.Frontend\/src\/pages\/Reports\/useReportsPdfExport\.ts\:1

UnchangedCompatible

```text
PDF export reads nowWindow from the read model, which is now derived; past PDF files are untouched
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ThenAndNowTemplateEditTest\.cs\:1

Migrated

```text
Edit universe: name, shown keys and token change; the legacy nowLengthDays member must survive unchanged
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice10EditReportScenarios\.cs\:1

Migrated

```text
Now-length edit rows go; touched rows move to the port
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ReportQueriesTest\.cs\:1

Migrated

```text
Reads with a stored Now now show Now equal to Then
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/TeamReportsControllerTest\.cs\:1

Migrated

```text
Edit mapping without a Now write
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportView\.editWizard\.test\.tsx\:1

Migrated

```text
Edit opens on Metrics & name; no Now step
```

test

Lighthouse\.Frontend\/src\/services\/Api\/ReportService\.test\.ts\:1

Migrated

```text
Request bodies without nowLengthDays
```

build

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/stryker\.6625\.backend\.json\:1

Changed

```text
New per-feature mutation config naming Tests.Acceptance.BaselineReportCreation
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ThenAndNowPayload\.cs\:16

Changed

```text
NowEndingOn added; NowLengthDays member removed from the record
```

production

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportWindowPolicy\.cs\:56

Changed

```text
NowRefusal and NowWindow removed
```

test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs\:1

Changed

```text
Gains the Now-window owner rule
```

##### Agreement

ReportEditRequest thenAndNow\.nowLengthDays

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/thenAndNowDraft\.ts\:234

Producer

Migrated

```text
Edit stops sending a Now length
```

ReportEditRequest thenAndNow\.nowLengthDays

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportsControllerBase\.cs\:41

Consumer

Migrated

```text
Reads it only to refuse an unequal Now
```

Refusal code set \(ReportRefusalCodes\)

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportRefusalCodes\.cs\:4

Producer

Migrated

```text
Adds now-length-differs-from-then; keeps every shipped spelling
```

Refusal code set \(ReportRefusalCodes\)

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/reportRefusalCopy\.ts\:82

Consumer

Migrated

```text
Exhaustive copy table gains the new code
```

Report read model nowWindow

Lighthouse\.Frontend\/src\/pages\/Reports\/useReportsPdfExport\.ts\:1

Consumer

UnchangedCompatible

```text
Same shape; value now always equal in length to thenWindow
```

##### Not applicable

prefactoring

```text
S2 adds ReportWindow.NowEndingOn and calls it from Read in the same change; Edit drops its Now check; no structure must move first
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

None.

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

```text
Saved and edited reports compare equal lengths
```

Safety

Pass

DesignedNotBuilt

```text
Quint invShownNowEqThen, invStaleOnlyAfterRealChange and invEditNeverRefusedForHistory held; mutants read-stored, bump and edit-checks-history killed
```

###### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-mutant\-bump\.meta

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-mutant\-edit\-checks\-history\.meta

```text
Saved and edited reports compare equal lengths
```

Inhabitation

Pass

DesignedNotBuilt

```text
A legacy 30/45 payload reads as a 30-day Now; an edit of name and metrics is accepted
```

###### Evidence

None.

```text
Saved and edited reports compare equal lengths
```

PublicReachability

Pass

DesignedNotBuilt

```text
Payload to ThenAndNowTemplate.Read to IReportQueries.Get to report view; Edit wizard to reports API to IReportCommands.Edit
```

###### Evidence

None.

```text
Saved and edited reports compare equal lengths
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: synchronous request and response
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
Only ReportWindow and ReportWindowPolicy compute a Now window: no other type in the Reports module calls ReportWindow.EndingOn with today or builds a Now window; E15 purity of ReportWindowPolicy stays
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsModuleArchUnitTest
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.tsx

Extend

```text
CHANGE_LINES gains the Total line (0 decimals) before per day; the Total change replaces the empty placeholder; both-0 form for both Throughput lines lives in this panel line definition
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts

Extend

```text
ChangeLine gains an optional both-0 form that a line definition opts into; compareThenNow and other panels keep their behaviour
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThenNowLineParts\.tsx

Extend

```text
Render the both-0 form with no mark and no percent
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/Reports/panels/ThroughputPanel.test.tsx src/pages/Common/Reports/panels/thenNowComparison.test.ts src/pages/Common/Reports/panels/changeSummary.test.ts src/pages/Common/Reports/panels/reportPanelRegistry.parity.test.tsx
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/changeSummary\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/reportPanelRegistry\.parity\.test\.tsx

#### Public oracle

```text
Render the Throughput panel and the report summary for a read of 30-day windows with Throughput Total 52 in Then and 65 in Now; then reads with totals 0 and 0, 0 and 30 over 14 days, 91 and 91, and a Then not captured
```

```text
52 to 65: the Total line shows the change and +25 percent marked better, per day keeps its own change, and the CHANGE summary judges Throughput on Total too; 0 and 0: both Throughput lines read No change (both 0) with no mark and no percent, and Throughput counts as unchanged; 0 to 30: the Total line reads the up mark, 30 and (Then was 0) with no percent; 91 and 91: the grey dash mark and No change, counted unchanged; not captured: no Total change and Throughput not judged. WIP and Work Item Age panels are unchanged
```

```text
A Total line with no change mark, a percent over a Then of 0, a both-0 line with a mark, a WIP or Work Item Age panel that changes its 0/0 look, or a summary that ignores the Total line
```

##### Promises

P1

P10

P11

P12

P13

P14

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

CHANGE\_LINES

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.tsx\:23

Extend

```text
The panel line definition already feeds both the panel and summariseChange through the registry
```

lineComparisonsOf

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts\:123

Extend

```text
Existing single owner of line judgement; the both-0 form is an opt-in per line
```

summariseChange

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/changeSummary\.ts\:40

Reuse

```text
Already judges every listed line; a neutral both-0 line counts as unchanged with no code change
```

LineChanges

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThenNowLineParts\.tsx\:64

Extend

```text
Existing renderer of a change line and its mark
```

##### Impact

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.tsx\:23

Changed

```text
Total change line
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts\:27

Changed

```text
Optional both-0 form on ChangeLine
```

production

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThenNowLineParts\.tsx\:64

Changed

```text
Both-0 rendering
```

consumer

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/reportPanelRegistry\.ts\:54

UnchangedCompatible

```text
Passes CHANGE_LINES as before
```

consumer

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/changeSummary\.ts\:40

UnchangedCompatible

```text
Judges the added line automatically
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/reportPanelRegistry\.parity\.test\.tsx\:1

Migrated

```text
Parity now includes the Total line
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/changeSummary\.test\.ts\:1

Migrated

```text
Throughput judged on Total too
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract changes: the read model already carries throughput total; the change is inside the frontend panel definitions
```

prefactoring

```text
Adding a line to an existing line list needs no preparatory move
```

architectural_tests

```text
Existing E16 (header judges like its panel, pure comparison modules) covers the added line; no new boundary
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L7

```text
The Throughput Total and per-day lines read No change (both 0) with no mark and no percent exactly when both shown sides are 0; equal non-zero sides read the grey dash and No change; a Then of 0 with Now above 0 reads Then was 0 with no percent; a dash on either side gives no change; other panels are unaffected
```

Invariant

Domain

ThroughputPanel

###### Treatment

Finite

###### Cases

```text
0 and 0: No change (both 0), no mark
```

```text
91 and 91: grey dash, No change
```

```text
0 and 30: up mark, 30 (Then was 0)
```

```text
52 and 65: up, 13, +25 percent, better
```

```text
Then not captured: no change
```

```text
Five closed classes of a line comparison
```

###### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/invChangeIffBothCaptured\.meta

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/invPctOnlyWhenThenPositive\.meta

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

```text
Throughput Total compared and counted in CHANGE
```

Safety

Pass

DesignedNotBuilt

```text
Quint invChangeIffBothCaptured, invPctOnlyWhenThenPositive (DISCUSS model) and invTotalAndPerDayAgree held; mutant unequal-lengths killed
```

###### Evidence

docs\/feature\/story\-6625\-baseline\-report\-creation\/evidence\/quint\/runs\/design\-mutant\-unequal\-lengths\.meta

```text
Throughput Total compared and counted in CHANGE
```

Inhabitation

Pass

DesignedNotBuilt

```text
A read with totals 52 and 65 builds a Total change of +13 and +25 percent
```

###### Evidence

None.

```text
Throughput Total compared and counted in CHANGE
```

PublicReachability

Pass

DesignedNotBuilt

```text
IReportQueries.Get read to ReportView to ThroughputPanel and ThenAndNowSummary
```

###### Evidence

None.

```text
Throughput Total compared and counted in CHANGE
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: pure rendering of a read
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
