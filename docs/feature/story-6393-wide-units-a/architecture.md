# Architecture brief: story\-6393\-wide\-units\-a

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm object_oriented as the project rule; the code is React function components and hooks, so no class is added: the cohesive owner of each decision is a module or a hook, never the page. Behaviour-free split by responsibility; the only behaviour change is the six member failure texts in S5.
```

#### D2

```text
Formal verification: no Quint, TLA+ or Agda model is written and none is claimed. A file split and one hook extraction change no state, transition, ordering or concurrency; the guarantees are checked by running code: test ids and outcomes equal before and after, rendered output equal, effect dependency arrays and hook call order equal in the diff, and two source-scan enforcement tests that refuse a planted case. Types are checked by tsc in pnpm build.
```

#### D3

```text
Size rule carrier: src/fileSize.enforcement.test.ts scans production .ts/.tsx under src (not *.test.ts(x), not src/tests, no TestKit or Fixture in the name, not *.d.ts), counting line terminators like wc -l; a file over 1000 lines offends. The offender list lives in src/fileSize.offenders.ts (name plus plain-words reason), owned by the crafter, so removing an entry in S2 and S4 never edits S1's oracle. It fails both ways: a new offender not listed, and a listed file at or under the line. The list starts with BaseMetricsView.tsx and ragRules.ts and only shrinks.
```

#### D4

```text
No barrel and no re-export from any old file for moved symbols; importers change their import. Moved tests travel with the code they cover and keep their rows unmodified.
```

#### D5

```text
Failure carriers: the size rule and the access guard report offending file paths in the failing assertion; the hook reports member failures as an error string state (one owner, useScopedAccess) read by the Access tab; no new error type.
```

### Constraints

#### K1

```text
Input bound: the line limit is 1000, a file of more than 1000 lines offends; the offender list is not extended, only shortened.
```

#### K2

```text
Behaviour-free: existing tests stay green unmodified apart from tests that move with their code; effect dependency arrays, hook call order and useCallback dependencies are copied as they are. Default Team and Portfolio terms give today's texts word for word.
```

#### K3

```text
S5 copy is approved text: Failed to load {team} members. / Failed to update {team} member role. / Failed to remove {team} member., same for {portfolio}; the term is getTerm(TERMINOLOGY_KEYS.TEAM or PORTFOLIO) lower-cased and read when the failure happens, not at mount.
```

#### K4

```text
Test rules: the test-writing skill and its frontend half win over nWave's timing and PBT density; no nWave scenario tags in code; no broad property tests (finite example rows only); the new enforcement tests walk about 700 files, well inside the default per-test budget, no derogation.
```

#### K5

```text
One oracle file per slice; an earlier slice's oracle and support files are never edited by a later slice.
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

contract_tests

```text
Every caller and callee is inside the one frontend deployable and changes in the same commit; the primary oracles cover them.
```

### Boundaries

```text
React Testing Library render of the component, renderHook of the hook with its providers, pure modules called as functions, and source-scan enforcement tests over src
```

#### Driven ports

None.

```text
Pages and views depend on extracted hooks and pure modules; extracted modules never import their former parent; useScopedAccess depends on rbacService from ApiServiceContext and useTerminology only; no barrel.
```

#### Failures

None.

### Observations

None.

### Equality

None.

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

None.

### States

Admitted

```text
A production file of at most 1000 lines, or a file over 1000 lines named on the offender list
```

src\/fileSize\.offenders\.ts

Excluded

```text
A new production file over 1000 lines that is not listed; a listed file at or under 1000 lines that is still listed
```

src\/fileSize\.enforcement\.test\.ts

Excluded

```text
A page calling rbacService member or group-mapping methods directly; my-summary fetched outside RbacService.ts
```

src\/rbacAccess\.enforcement\.test\.ts

Unknown

```text
Cohesion of a file under the line is not judged; the check sees length only
```

reviewer

### Models

None.

### Closure

```text
size rule and access guard
```

Safety

Pass

DesignedNotBuilt

```text
Exclusion is a source scan executed by the host; no formal model is claimed; planted-project scenarios are the falsifiers.
```

##### Evidence

None.

```text
useScopedAccess and moved modules
```

Inhabitation

Pass

DesignedNotBuilt

```text
input scope kind and id -> useScopedAccess -> TeamDetail or PortfolioDetail Access tab -> ScopedMembershipManager is acyclic; moved modules are called by their former parent.
```

##### Evidence

None.

```text
useScopedAccess and moved modules
```

PublicReachability

Pass

DesignedNotBuilt

```text
Reached through the rendered Access tab and the page components; the pure modules through their function exports.
```

##### Evidence

None.

```text
all slices
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: no scheduler, fairness or progress claim; the sets of states and effects are the same before and after.
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
A moved effect loses an item in its dependency array or two effects swap order
```

```text
No refetch loop and no stale scope
```

```text
Nothing
```

```text
Changed effect dependencies or hook call order
```

```text
Read line by line in review; S1-S4 move only pure code and the form; effects stay in their components
```

crafter

#### Y2

Bypass

```text
ctx_patch writes REDACTED into moved type= lines
```

```text
Password fields stay password
```

```text
Nothing
```

```text
REDACTED text in source
```

```text
git grep REDACTED gate after each edit; ConnectionDefaults Playwright spec
```

crafter

#### Y3

Stressor

```text
The renamed term is read before terminology loads
```

```text
Failure text uses the configured word at failure time
```

```text
Default word before terminology is ready
```

```text
Term read once at mount
```

```text
Hook reads getTerm when the failure happens; one row where the term arrives after mount
```

crafter

#### Y4

Stressor

```text
A fourth file crosses 1000 lines during the work, or the scan misses a folder
```

```text
The rule names every offender
```

```text
Nothing
```

```text
Silent none from an unreadable folder
```

```text
Scenario with a deep planted file and a loud failure on an unreadable folder; the fix is a split, not a list entry
```

acceptance\-designer

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx

Extend

```text
Parent keeps the component and the two layer-leak imports; sections move out.
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionDefaultsSection\.tsx

Create

```text
Defaults section moved out of the parent.
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/connectionFormRules\.ts

Create

```text
Copy and pure form helpers moved out.
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/OAuthPremiumNotice\.tsx

Create

```text
OAuth upgrade notice with the settings/license link.
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionAuthSection\.tsx

Create

```text
Authentication section of the form.
```

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/useConnectionSave\.ts

Create

```text
Validate, 409 handling and update question of the save flow.
```

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts

Create

```text
Offender list by name with reasons; read by the size rule.
```

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/fileSize.enforcement.test.ts
pnpm --dir Lighthouse.Frontend test src/components/Common/Connection src/pages/Connections src/layerBoundaries.enforcement.test.ts
```

#### Supports

None.

#### Public oracle

```text
Run the size rule over the source tree and over planted temp projects (1001 lines, 1000 lines, a 1500-line .test.tsx, src/tests/Kit.tsx, a listed file that is now short, an unreadable folder); run the existing ModifyConnectionSettings, whatASaveAsks, EditConnection and CreateConnectionWizard tests against the split form.
```

```text
The scan reports 1001 lines and no more, does not report 1000, a test file or src/tests, reports a stale list entry, fails loudly on an unreadable folder, and lists exactly BaseMetricsView.tsx and ragRules.ts for the real tree; the existing connection form tests pass unmodified.
```

```text
Limit 1000 changed to 1100 turns the 1001-line scenario red; removing the link text in OAuthPremiumNotice.tsx, returning the 409 copy for every save error in useConnectionSave.ts, or dropping readOnly on the OAuth locked fields in ConnectionAuthSection.tsx turns the named existing rows red.
```

##### Promises

P1

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

ModifyConnectionSettings

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:302

Extend

```text
Component stays the owner of the form; only responsibilities with their own reason to change leave.
```

compareWithAllowList

Lighthouse\.Frontend\/src\/layerBoundaries\.enforcement\.test\.ts\:1

Reuse

```text
The size rule reuses the list-by-name enforcement shape; no shared import, the small helpers are copied.
```

fileSizeOffenders

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts\:1

CreateNew

```text
Nothing owns file size; Fallow has no size rule and a baseline count would hide which file was fixed.
```

fieldsChangedSince

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/whatASaveAsks\.ts\:30

Reuse

```text
Already owns the update-question decision rules; useConnectionSave calls it unchanged.
```

ConnectionDefaultsSection

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionDefaultsSection\.tsx\:1

CreateNew

```text
New file holds the section moved unchanged from the parent; no other file owns it.
```

CONNECTION\_COPY

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/connectionFormRules\.ts\:1

CreateNew

```text
New file holds the copy and pure helpers moved unchanged from the parent.
```

OAuthPremiumNotice

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/OAuthPremiumNotice\.tsx\:1

CreateNew

```text
New file holds the upgrade notice moved unchanged, with the one settings/license link.
```

ConnectionAuthSection

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionAuthSection\.tsx\:1

CreateNew

```text
New file holds the authentication section moved unchanged.
```

useConnectionSave

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/useConnectionSave\.ts\:1

CreateNew

```text
New hook holds the save flow moved unchanged; no existing hook owns it.
```

TOO\_WIDE\_TODAY

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts\:1

CreateNew

```text
No candidate owns file size; see the size rule decision.
```

##### Impact

source

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:302

Changed

```text
Sections and the save flow move to new files; props and export unchanged.
```

consumer

Lighthouse\.Frontend\/src\/pages\/Connections\/EditConnection\.tsx\:137

UnchangedCompatible

```text
Imports the unchanged export; return path stays here.
```

test

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.test\.tsx\:1

UnchangedCompatible

```text
Renders the parent component; unmodified.
```

test

Lighthouse\.Frontend\/src\/layerBoundaries\.enforcement\.test\.ts\:1

UnchangedCompatible

```text
Both leak imports stay in the parent file, so the allow-list does not change.
```

source

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionDefaultsSection\.tsx\:1

Migrated

```text
Declaration moves here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/connectionFormRules\.ts\:1

Migrated

```text
Declarations move here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/OAuthPremiumNotice\.tsx\:1

Migrated

```text
Declaration moves here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionAuthSection\.tsx\:1

Migrated

```text
Declaration moves here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/useConnectionSave\.ts\:1

Migrated

```text
Save flow moves here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts\:1

Changed

```text
New list read by the size rule.
```

##### Agreement

ModifyConnectionSettings props and default export

Lighthouse\.Frontend\/src\/pages\/Connections\/EditConnection\.tsx\:1

Consumer

UnchangedCompatible

```text
Props and export unchanged.
```

ModifyConnectionSettings props and default export

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:302

Producer

UnchangedCompatible

```text
Props and export unchanged.
```

##### Not applicable

prefactoring

```text
The slice is the behaviour-free move itself; no behaviour change follows in S1 that a separate preserving move would ease.
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

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
No production frontend file is over 1000 lines unless named on the offender list; the list only shrinks; a stale entry fails
```

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend test src/fileSize.enforcement.test.ts
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesOverview\.ts

Create

```text
WIP, Blocked, Stale, FeaturesWorkedOn, Predictability, FlowEfficiency rules.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.ts

Create

```text
SleRisk, CycleTimePercentiles, TotalWorkItemAge, scatterplot, age chart, age over time, age percentiles, cumulative state time rules.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesVolumeAndFlow\.ts

Create

```text
StartedVsClosed, Throughput, WipOverTime, SimplifiedCfd, WorkDistribution, FeatureSize, EstimationVsCycleTime, LoadBalance, Arrivals, Pbc rules.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragTypes\.ts

Create

```text
RagResult and RagTerms shared by the three families.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/blockedMaxAgeRag\.ts

Extend

```text
Import of RagTerms points at ragTypes.ts; only edit it needs.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

```text
Imports point at the family modules.
```

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts

Extend

```text
Delete the ragRules.ts entry.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesOverview\.test\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Common/MetricsView/ragRulesOverview.test.ts src/pages/Common/MetricsView/ragRulesTimeAndAge.test.ts src/pages/Common/MetricsView/ragRulesVolumeAndFlow.test.ts
pnpm --dir Lighthouse.Frontend test src/pages/Common/MetricsView/blockedMaxAgeRag.test.ts src/pages/Common/MetricsView/BaseMetricsView.test.tsx src/fileSize.enforcement.test.ts
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.test\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesVolumeAndFlow\.test\.ts

#### Public oracle

```text
Call each rag rule function from its family module with the existing rows (the describe blocks of ragRules.test.ts, moved by family), and run blockedMaxAgeRag and BaseMetricsView tests.
```

```text
Every rule gives the same answer as before; the moved rows pass unmodified; ragRules.ts is gone, no file of the three families exceeds 1000 lines, and the offender list no longer names ragRules.ts.
```

```text
Swapping two bands in computeCycleTimePercentilesRag after the move turns its row in ragRulesTimeAndAge.test.ts red; making RagTerms miss blocked in ragTypes.ts fails tsc in pnpm build; leaving ragRules.ts listed fails the size rule as stale.
```

##### Promises

P1

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

RagTerms

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragTypes\.ts\:6

Extend

```text
Type moves unchanged to ragTypes.ts so the three family files share it without importing each other.
```

computeWipOverviewRag

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesOverview\.ts\:1

CreateNew

```text
New family file holds the overview rules moved unchanged.
```

BlockedRagResult

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/blockedMaxAgeRag\.ts\:11

Extend

```text
Only its RagTerms type import changes.
```

computeWipOverviewRag

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesOverview\.ts\:3

Replace

```text
31 independent pure functions with no shared table; split by metric family, no barrel.
```

computeBlockedOverviewRag

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesOverview\.ts\:2

CreateNew

```text
Same family file, second rule moved unchanged.
```

computeCycleTimePercentilesRag

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.ts\:1

CreateNew

```text
New family file holds the time and age rules moved unchanged.
```

computeThroughputRag

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesVolumeAndFlow\.ts\:1

CreateNew

```text
New family file holds the volume and flow rules moved unchanged.
```

RagTerms

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragTypes\.ts\:1

CreateNew

```text
New types file; the type moves unchanged so the families share it.
```

BaseMetricsView

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:92

Extend

```text
Only its imports change.
```

TOO\_WIDE\_TODAY

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts\:1

CreateNew

```text
List created in the first slice; its entry is deleted here.
```

##### Impact

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRules\.ts\:1

Migrated

```text
Functions move to three family files; behaviour identical.
```

consumer

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1

Changed

```text
Import lines point at the family modules.
```

consumer

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/blockedMaxAgeRag\.ts\:1

Changed

```text
RagTerms import path.
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRules\.test\.ts\:1

Migrated

```text
Describe blocks move to the matching family test files, rows unmodified.
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/blockedMaxAgeRag\.test\.ts\:1

Changed

```text
Type import of RagTerms points at ragTypes.ts.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesOverview\.ts\:1

Migrated

```text
Functions move here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesTimeAndAge\.ts\:1

Migrated

```text
Functions move here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragRulesVolumeAndFlow\.ts\:1

Migrated

```text
Functions move here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragTypes\.ts\:1

Migrated

```text
Types move here; shape identical.
```

source

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts\:1

Changed

```text
ragRules.ts entry deleted.
```

##### Agreement

RagResult and RagTerms types

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ragTypes\.ts\:1

Producer

Migrated

```text
Declared once in ragTypes.ts.
```

RagResult and RagTerms types

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1

Consumer

Migrated

```text
Imports from the family modules and ragTypes.ts.
```

RagResult and RagTerms types

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/blockedMaxAgeRag\.ts\:1

Consumer

Migrated

```text
Imports RagTerms from ragTypes.ts.
```

##### Not applicable

prefactoring

```text
The slice is the behaviour-free move itself; no behaviour change follows.
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

None.

##### Risks

None.

##### Architectural tests

###### T3

```text
ragRules.ts leaves the offender list in the commit that brings it under 1000 lines; the list does not grow
```

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend test src/fileSize.enforcement.test.ts
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/workItemLookup\.ts

Create

```text
buildWorkItemLookup moved out.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetFooters\.ts

Create

```text
buildWidgetFooters moved out.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.ts

Create

```text
buildViewData moved out.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

```text
Imports the three modules; no re-export for tests.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.test\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Common/MetricsView/widgetViewData.test.ts src/pages/Common/MetricsView/workItemLookup.test.ts
pnpm --dir Lighthouse.Frontend test src/pages/Common/MetricsView/BaseMetricsView.test.tsx src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx src/pages/Common/MetricsView/BaseMetricsView.cumulativeStateTimeDialog.test.tsx
```

#### Supports

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/workItemLookup\.test\.ts

#### Public oracle

```text
Call buildViewData, buildWorkItemLookup and the footer builders as functions with the existing DOM-free rows (the 390 lines of BaseMetricsView.test.tsx moved with their code), and render the metrics view through its existing test files.
```

```text
Every builder returns what it returned before; the moved rows pass unmodified; the metrics view tests pass with no change.
```

```text
Dropping sleRiskColumn from inFlight in widgetViewData.ts turns the which-lists-carry-the-risk-column row red; letting a Feature overwrite a Team Work Item in workItemLookup.ts turns the ownerKind team row red; swapping amber and red thresholds in widgetFooters.ts turns the widget-rag-aging amber row in BaseMetricsView.test.tsx red.
```

##### Promises

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

buildViewData

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:593

Extend

```text
Existing pure builder gains its own module; same function, same export name.
```

buildWorkItemLookup

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:210

Extend

```text
Existing pure builder gains its own module.
```

buildWidgetFooters

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1

Extend

```text
Existing pure builder gains its own module.
```

buildWorkItemLookup

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/workItemLookup\.ts\:1

CreateNew

```text
New module holds the builder moved unchanged.
```

buildWidgetFooters

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetFooters\.ts\:1

CreateNew

```text
New module holds the builder moved unchanged.
```

buildViewData

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.ts\:1

CreateNew

```text
New module holds the builder moved unchanged.
```

BaseMetricsView

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:92

Extend

```text
Only its imports change; no re-export.
```

##### Impact

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:210

Migrated

```text
Builders move to three modules; behaviour identical.
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.test\.tsx\:6970

Migrated

```text
The two DOM-free describes move to workItemLookup.test.ts and widgetViewData.test.ts.
```

consumer

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1347

Changed

```text
The component imports the builders instead of declaring them.
```

consumer

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:1

UnchangedCompatible

```text
Imports BaseMetricsView component through TeamMetricsView; export unchanged.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/workItemLookup\.ts\:1

Migrated

```text
Builder moves here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetFooters\.ts\:1

Migrated

```text
Builder moves here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.ts\:1

Migrated

```text
Builder moves here; behaviour identical.
```

##### Agreement

buildViewData and buildWorkItemLookup exports

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetViewData\.ts\:1

Producer

Migrated

```text
Same signature, new module.
```

buildViewData and buildWorkItemLookup exports

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.test\.tsx\:6970

Consumer

Migrated

```text
Rows move to the new test files with the code.
```

##### Not applicable

architectural_tests

```text
The size rule of S1 already covers the length law for every file, including the new modules; this slice adds no dependency or owner law of its own.
```

prefactoring

```text
The slice is the behaviour-free move itself; no behaviour change follows.
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

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx

Create

```text
buildPbcNodes and buildWidgetNodes moved out.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/dashboardFrame\.ts

Create

```text
The four frame helpers moved out.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

```text
Imports the two modules; component stays; under 1000 lines.
```

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts

Extend

```text
Delete the BaseMetricsView.tsx entry; the list ends empty.
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/dashboardFrame\.test\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Common/MetricsView/dashboardFrame.test.ts src/fileSize.enforcement.test.ts
pnpm --dir Lighthouse.Frontend test src/pages/Common/MetricsView/BaseMetricsView.test.tsx src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx src/pages/Common/MetricsView/BaseMetricsView.cumulativeStateTimeDialog.test.tsx
```

#### Supports

None.

#### Public oracle

```text
Call the frame helpers (isPlaced and its siblings) as functions with rows taken from the existing loading test for estimation vs cycle time, render the metrics view through the existing test files, and run the size rule over the tree.
```

```text
The helpers answer as before; the metrics view tests pass unmodified; BaseMetricsView.tsx is at most 1000 lines and is no longer on the offender list; the offender list is empty.
```

```text
Making isPlaced always true in dashboardFrame.ts turns the dashboardFrame row and the loading-test row for estimation vs cycle time red; re-adding a 1001-line file or keeping a stale BaseMetricsView entry fails the size rule naming it.
```

##### Promises

P1

P2

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

buildWidgetNodes

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1

Extend

```text
Existing pure node builders gain their own module.
```

BaseMetricsView

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:92

Extend

```text
The component keeps state and effects; only pure code leaves.
```

buildWidgetNodes

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:1

CreateNew

```text
New module holds the node builders moved unchanged.
```

isPlaced

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/dashboardFrame\.ts\:1

CreateNew

```text
New module holds the frame helpers moved unchanged.
```

TOO\_WIDE\_TODAY

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts\:1

CreateNew

```text
List created in the first slice; its entry is deleted here.
```

##### Impact

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1

Migrated

```text
Node builders and frame helpers move; behaviour identical.
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.loading\.test\.tsx\:1

UnchangedCompatible

```text
Render tests keep their vi.mock module ids; unmodified.
```

config

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts\:1

Changed

```text
Offender list becomes empty.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:1

Migrated

```text
Node builders move here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/dashboardFrame\.ts\:1

Migrated

```text
Frame helpers move here; behaviour identical.
```

source

Lighthouse\.Frontend\/src\/fileSize\.offenders\.ts\:1

Changed

```text
BaseMetricsView.tsx entry deleted; list empty.
```

source

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1

Migrated

```text
Pure code leaves; component under 1000 lines.
```

##### Agreement

BaseMetricsView component export

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1347

Producer

UnchangedCompatible

```text
Props and export unchanged.
```

BaseMetricsView component export

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:1

Consumer

UnchangedCompatible

```text
Through TeamMetricsView and PortfolioMetricsView, unchanged.
```

##### Not applicable

prefactoring

```text
The slice is the behaviour-free move itself; no behaviour change follows.
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

None.

##### Risks

None.

##### Architectural tests

###### T4

```text
BaseMetricsView.tsx leaves the offender list in the commit that brings it under 1000 lines; the list ends empty and the rule stays
```

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend test src/fileSize.enforcement.test.ts
```

##### Contract tests

None.

### S5

#### Targets

Lighthouse\.Frontend\/src\/hooks\/useScopedAccess\.ts

Create

```text
One owner of members and group mappings for a Team or Portfolio scope, taking the scope kind and id.
```

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx

Extend

```text
Duplicated handlers replaced by the hook.
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx

Extend

```text
Duplicated handlers replaced by the hook.
```

Lighthouse\.Frontend\/src\/hooks\/useScopedAccess\.test\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/hooks/useScopedAccess.test.ts src/rbacAccess.enforcement.test.ts
pnpm --dir Lighthouse.Frontend test src/pages/Teams/Detail src/pages/Portfolios/Detail src/hooks/useRbac.test.ts
```

#### Supports

Lighthouse\.Frontend\/src\/rbacAccess\.enforcement\.test\.ts

#### Public oracle

```text
renderHook useScopedAccess for scope Team and for scope Portfolio inside a ready terminology provider and an ApiServiceContext with a fake rbacService: load members, assign, remove, create and remove a group mapping, with each call failing; with default terms and with Team renamed to Squad and Portfolio to Program. Render the Team and Portfolio Access tabs once each with a renamed term and a failing load. Run the access guard over the source tree and a planted temp project.
```

```text
With default terms the six texts equal today's (Failed to load team members., Failed to update team member role., Failed to remove team member., and the portfolio ones); with the renamed terms they read Failed to load squad members., Failed to update squad member role., Failed to remove squad member. and the program ones; assign and remove reload the members; the Access tabs show one renamed text on screen; the member and group-mapping methods of rbacService are called only from hooks/useScopedAccess.ts and my-summary appears only in RbacService.ts.
```

```text
Dropping toLowerCase of the term gives Failed to load Squad members.; hard-coding team in the portfolio text fails the Portfolio renamed row; calling upsert without reloading the members fails the assign row; calling rbacService.removeGroupMapping from a page again fails the access guard naming that file; reverting only the text change keeps the default-text rows green and turns the six renamed rows red.
```

##### Promises

P2

P3

P4

#### Prefactoring

```text
Extract useScopedAccess from TeamDetail, then use it in PortfolioDetail, before the failure texts change; makes the copy change one edit in one place.
```

```text
Handlers, reload after assign and remove, and today's default failure texts
```

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetailAccessTab\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Teams/Detail/TeamDetailAccessTab.test.tsx src/pages/Portfolios/Detail/PortfolioDetailAccessTab.test.tsx src/pages/Teams/Detail/TeamDetail.test.tsx src/pages/Portfolios/Detail/PortfolioDetail.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

useRbac

Lighthouse\.Frontend\/src\/hooks\/useRbac\.ts\:1

Reuse

```text
Gating stays there; useScopedAccess does not decide permission.
```

RbacService

Lighthouse\.Frontend\/src\/services\/Api\/RbacService\.ts\:1

Reuse

```text
Only owner of my-summary and of the member and group-mapping calls; unchanged.
```

useScopedAccess

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:158

CreateNew

```text
Two pages hold an 86-line copy of the same handlers; no hook owns them. One hook with the scope kind as an argument, no mode flag, since behaviour does not diverge.
```

useScopedAccess

Lighthouse\.Frontend\/src\/hooks\/useScopedAccess\.ts\:1

CreateNew

```text
One owner of the members and group-mapping handlers; see the earlier reuse record.
```

PortfolioDetail

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:122

Extend

```text
Handlers replaced by the hook; page keeps its layout.
```

##### Impact

source

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:158

Migrated

```text
Handlers and member state move to the hook.
```

source

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:122

Migrated

```text
Handlers and member state move to the hook.
```

test

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetailAccessTab\.test\.tsx\:1

Changed

```text
Characterization rows added first with default texts; page rows shrink to one wiring row after the hook has its own rows.
```

test

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetailAccessTab\.test\.tsx\:1

Changed

```text
Same as the Team Access tab test.
```

test

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.test\.tsx\:917

UnchangedCompatible

```text
createGroupMapping row keeps passing through the page.
```

source

Lighthouse\.Frontend\/src\/hooks\/useScopedAccess\.ts\:1

Changed

```text
New hook holds the handlers and the six failure texts with the configured terms.
```

##### Agreement

useScopedAccess members and group\-mapping handlers

Lighthouse\.Frontend\/src\/hooks\/useScopedAccess\.ts\:1

Producer

Migrated

```text
Single producer of the handlers and the failure texts.
```

useScopedAccess members and group\-mapping handlers

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/TeamDetail\.tsx\:158

Consumer

Migrated

```text
Reads state and handlers from the hook.
```

useScopedAccess members and group\-mapping handlers

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDetail\.tsx\:122

Consumer

Migrated

```text
Reads state and handlers from the hook.
```

##### Not applicable

None.

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

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
The member and group-mapping methods of rbacService are called only from hooks/useScopedAccess.ts and my-summary appears only in RbacService.ts; empty allow-lists
```

Lighthouse\.Frontend\/src\/rbacAccess\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend test src/rbacAccess.enforcement.test.ts
```

##### Contract tests

None.

## ADRs

None.
