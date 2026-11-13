# Architecture brief: story\-6393\-wide\-units\-b

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Paradigm: the repository rule is object_oriented; the code is React function components and hooks. No class is added. The cohesive owner of each decision is a hook or a module in the folder of the code it leaves; a decision about one value never moves into a page or component. Procedural smell catalog: none applies; the general classes used are mixed responsibility, long unit, hidden agreement between units and semantic duplicate.
```

#### D2

```text
Layers for this behaviour-free move. Domain/pure rules: metricsApplicability, metricsOwnerKey, deliveryFormRules (states: input in, value out; operations: pure functions; observation: return value). Application/ports: hooks useNamedPercentilesScope, useStateDrillDown, useMetricsQuery (states: query pending, answered, failed, superseded; observation: hook return value and the requests the service receives). Adapter: none changes; MetricsService and the Delivery API services are untouched. Infrastructure/recovery: none; a failed request keeps today behaviour (revert to Default scope, keep last drawn answer). Presentation: Delivery components get new files with unchanged props.
```

#### D3

```text
Formal verification: no Quint, TLA+ or Agda model is written and no checker is run. A file split and two hook extractions add no state, transition, protocol, concurrency or recovery behaviour; the set of states and effects is the same before and after, so there is nothing for a model to cover that the existing tests do not run. Claims are covered by executed code: existing tests unchanged and green, one new oracle per slice that is RED until the module exists, the two enforcement tests (fileSize, layerBoundaries), tsc -b. Liveness is not applicable: no progress claim is made or changed. Type strength: TypeScript with strict tsc; moved symbols keep their exact types, so tsc -b is the check for exclusion of a wrong import.
```

#### D4

```text
Query order in useMetricsData is kept: its useMetricsQuery calls stay in the one hook in the same order, are not split into sub-hooks, and no call moves above or below another. In BaseMetricsView the two new hooks are called at the positions of the code they replace, so the hook order of the component does not change.
```

#### D5

```text
Moves and edits are separate commits: refactor(<area>): move <unit> for each move; import-line edits of importers and the layer-list renames are their own edit commit. No re-export from a file for a symbol it gave away; importers change their import line. The three Delivery-folder slices keep every new file in pages/Portfolios/Detail/Components/DeliveryGrid/, add no barrel, and leave DeliveryGrid/index.ts untouched.
```

#### D6

```text
Tests: the existing tests of each split file stay as they are, apart from import lines and the rule-only rows that move down to deliveryFormRules.test.ts. Each slice has one new oracle file that imports the new module from its target path (RED until the move), driven as a user of the code drives it: renderHook with providers for hooks, a function call for pure modules, RTL for components. The test-writing skill rules apply (frontend half); no nWave scenario tags in code; no vi.mock of the unit under test.
```

### Constraints

#### K1

```text
Behaviour-free: no visible, request-order, copy, timing or accessibility change. Moved code keeps its dependency arrays, memo and callback identities and effect order exactly; comments that already explain a why move with their code and no new internal-reference comment is added.
```

#### K2

```text
Size: each of useMetricsData.ts, DeliverySection.tsx and DeliveryCreateModal.tsx ends well under 1000 lines (design estimates about 750, 480 and 470) and src/fileSize.offenders.ts stays empty. The threshold stays 1000. Part A oracle and support files are never edited.
```

#### K3

```text
Layer list: src/layerBoundaries.enforcement.test.ts ALLOWED_LEAKS only shrinks or renames; a new pair is never added to make the build pass. The ratchet is run after each move and the list edited to equal what it measures.
```

#### K4

```text
No input bound applies: nothing validated or stored changes. The form rules keep their current acceptance exactly (date must be after today at local midnight; name must be non-blank after trim); no narrower or wider bound is chosen.
```

#### K5

```text
Fast loop per slice: cd Lighthouse.Frontend and pnpm test:fast --changed, pnpm test --changed after a component edit, plus the slice oracle by path. Full gates once at the end of a slice: pnpm test and pnpm build (check git status afterwards, no */docs symlink under Lighthouse.Frontend). DeliveryCreateModal.test.tsx may time out under a loaded full run: re-run it alone first. After edits run git grep -n REDACTED -- Lighthouse.Frontend/src.
```

#### K6

```text
Mutation testing runs once at the end on frozen code with a config by file listing the new files; not part of any slice here.
```

#### K7

```text
code-fact: CLI query results used for callers are cross-checked with a text search of import lines; the importer lists in the slices come from that text search of Lighthouse.Frontend/src. typesafe: unavailable (Jev refused by configuration; no remote judgment used).
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

None.

### Boundaries

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
Each moved unit is imported by its new path only; the file it left holds no re-export of it.
```

crafter

Excluded

```text
A second copy of a moved function or component left behind in the file it left.
```

crafter

Unknown

```text
Whether fallow counts type-only imports as layer leaks, which decides whether the S2 move adds new pairs to the layer list (see slice S2 risk).
```

orchestrator

### Models

None.

### Closure

None.

### Risks

#### Y1

Stressor

```text
A moved effect loses a dependency or two effects swap order
```

```text
Same requests, same loading and revert behaviour as before
```

```text
none
```

```text
A refetch loop, a stale scope landing after a reset, a changed query start order
```

```text
Diff reading is the only check on dependency arrays beyond the existing loading and drill-down tests
```

crafter and whole\-candidate reviewer

#### Y2

Bypass

```text
A test mocks a moved module by path and stays green whatever the code does
```

```text
Each moved module is exercised by a real call in the slice oracle
```

```text
vi.mock of chart components as today
```

```text
Mocking the moved unit in its own oracle
```

```text
Reviewer reads the oracle for mocks of the unit under test
```

acceptance designer

#### Y3

Stressor

```text
Second stressor set: a fourth file crosses 1000 lines during the slices; an importer is missed; a moved layer import keeps its old name in the list
```

```text
tsc -b and the two enforcement tests fail loudly and name the file
```

```text
none
```

```text
Silent green
```

```text
Walked against the finished design, not executed
```

crafter

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useNamedPercentilesScope\.ts

Create

```text
Owns the named-percentiles scope: scope id, scoped values and info, the generation guard, the revert-to-Default on a failed fetch and the reset when entity or window change
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useStateDrillDown\.ts

Create

```text
Owns the state drill-down: selected state, open flag, the keyed query for the items, and the work items and days column derived from its rows
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

```text
Calls the two hooks at the positions of the code they replace; keeps its render output
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.hooks\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Common/MetricsView/BaseMetricsView.hooks.test.tsx src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx src/pages/Common/MetricsView/BaseMetricsView.cumulativeStateTimeDialog.test.tsx src/pages/Common/MetricsView/BaseMetricsView.test.tsx
```

#### Supports

None.

#### Public oracle

```text
renderHook(useNamedPercentilesScope) and renderHook(useStateDrillDown) with a provider-wrapped fake metrics service and deferred promises; call the scope-change and bar-click handlers
```

```text
Scoped values equal the answers of the latest selection only; a failed fetch or a changed entity or window returns to Default; the drill-down returns the rows of the keyed query as work items with the rounded days column and asks again when the selected item ids change
```

```text
Drop the generation bump in the reset effect (a response for the old entity re-populates the scope); remove selectedItemIds from the drill-down key (no second request); the oracle imports modules that do not exist before the move, so it is RED until they do
```

##### Promises

P1

P2

#### Prefactoring

```text
L4 extract: move the inline named-percentiles scope and the state drill-down of BaseMetricsView into two hooks, each in its own move commit, before any later change to the metrics view
```

```text
Every metrics widget loads, draws and drills down as before; same requests, same revert and reset behaviour
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.loading\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx src/pages/Common/MetricsView/BaseMetricsView.cumulativeStateTimeDialog.test.tsx src/pages/Common/MetricsView/BaseMetricsView.test.tsx
```

#### Records

##### Decisions

```text
useNamedPercentilesScope takes the metrics service, the entity id and the window (start and end date) and returns the scope definition id, the scoped percentile values, the scoped info and the scope-change handler. It keeps one generation counter in a ref; each selection and each reset bumps it and a response applies only while its generation is current. State carrier: React state and the ref inside the hook; owner: the hook file. Effect: the reset effect keyed on entity id, start date and end date, with its biome-ignore reason, moves unchanged.
```

```text
useStateDrillDown takes the metrics service, the owner type, the entity, the window and the selected item ids and returns the drill-down state, open flag, the bar-click handler, a close handler, the dialog status, the work items and the days column. The useQuery keeps its key (owner type, owner key, state, from, to, item ids) and enabled rule; selected item ids stay owned by BaseMetricsView because useMetricsData also reads them. The hook is called after useCumulativeStateTimeCandidates, where the old query was, so no query starts earlier; the old drill-down state calls moved into the hook with it.
```

##### Constraints

None.

##### Reuse

useNamedPercentilesScope

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useNamedPercentilesScope\.ts\:1

CreateNew

```text
No existing owner of the named-percentiles scope: the logic lives inline in BaseMetricsView.tsx l.242-320
```

useStateDrillDown

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useStateDrillDown\.ts\:1

CreateNew

```text
No existing owner of the drill-down state and query: inline in BaseMetricsView.tsx l.342-435
```

useCumulativeScopeChoice

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useCumulativeStateTimeChoices\.ts\:24

Reuse

```text
Style owner for a hook in this folder; shape reused, no code shared
```

dialogRowsOf

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useStateDrillDown\.ts\:86

Reuse

```text
Existing helper that turns a query into dialog status and rows; the drill-down hook calls it unchanged; no other candidate decides this
```

useStateDrillDown

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:290

Extend

```text
Calls the two hooks at the positions of the code they replace; render output unchanged
```

##### Impact

component

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1

Changed

```text
Loses the inline state, query and effect; calls the hooks; props and output unchanged; its four importers (TeamMetricsView, PortfolioMetricsView, two tests) need no change
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.test\.tsx\:1

UnchangedCompatible

```text
Drives the component through its public surface; no import of the moved code
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.loading\.test\.tsx\:1

UnchangedCompatible

```text
Same
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.cumulativeStateTimeDialog\.test\.tsx\:1

UnchangedCompatible

```text
Same; its row on the item-ids query key is the plant for the drill-down hook
```

module

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useNamedPercentilesScope\.ts\:1

Changed

```text
New file; receives the named-percentiles logic of BaseMetricsView unchanged
```

module

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useStateDrillDown\.ts\:1

Changed

```text
New file; receives the drill-down logic of BaseMetricsView unchanged
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract changes: the hooks are private to the folder, BaseMetricsView props and exports are untouched
```

architectural_tests

```text
The two new files sit in the folder of the component, import only what it imported, and BaseMetricsView stays under the threshold, so neither the layer list nor the size list can change
```

contract_tests

```text
Producer and consumer are in one deployable and change in one commit; the component tests cover the join
```

##### Boundaries

```text
BaseMetricsView rendered through React Testing Library with its providers; each hook through renderHook with QueryClientProvider and a fake metrics service
```

###### Driven ports

```text
IMetricsService (getCycleTimePercentiles, getCycleTimePercentilesInfo, getCumulativeStateTimeItemsForTeam and ForPortfolio)
```

```text
BaseMetricsView -> two hooks in the same folder -> hooks/useMetricsData (metricsQueryOptions) and services; the hooks import nothing from BaseMetricsView
```

###### Failures

None.

##### Observations

```text
Percentiles scope: after a selection the returned scoped values and info equal the answers of the latest selection only; after a failed fetch or a change of entity or window the scope id is null and no scoped values remain. Drill-down: the work items and days column returned for an open state equal the rows of the keyed query
```

hook return value plus the requests the fake service received

```text
Rendering details of the percentile widget and dialog; those are observed by the existing BaseMetricsView tests
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
A response for a superseded selection, or for an entity or window the reader has left, never changes the returned scope; a failed named fetch returns to Default
```

Invariant

Application

useNamedPercentilesScope

###### Treatment

Finite

###### Cases

```text
slow response for definition A lands after a fast response for definition B
```

```text
fetch fails for a named definition
```

```text
entity id changes while a response is in flight
```

```text
Finite enumerable cases of one small state machine; table rows through renderHook with deferred promises
```

###### Evidence

None.

###### L2

```text
The drill-down query key contains the selected item ids, so narrowing the selection asks again
```

Invariant

Application

useStateDrillDown

###### Treatment

Finite

###### Cases

```text
open a state, then change the item ids, the service is asked with the new ids
```

```text
Finite; one row through renderHook
```

###### Evidence

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
named-percentiles scope and drill-down hooks
```

Inhabitation

Pass

DesignedNotBuilt

```text
input: entity, window and service from BaseMetricsView; producer: the two hooks; consumer: BaseMetricsView; public port: its rendering and renderHook; import graph is acyclic (hooks do not import the component)
```

###### Evidence

None.

```text
named-percentiles scope and drill-down hooks
```

PublicReachability

Pass

DesignedNotBuilt

```text
BaseMetricsView.tsx calls both hooks; the oracle reaches them by path through renderHook
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

Lighthouse\.Frontend\/src\/hooks\/metricsApplicability\.ts

Create

```text
Owns which metrics questions an owner can ask: the is-service guards, applicabilityFor, the choices-made type and narrowsAChartAlreadyDrawn
```

Lighthouse\.Frontend\/src\/hooks\/metricsOwnerKey\.ts

Create

```text
Owns the owner type, MetricsOwnerKey, metricsOwnerKeyOf and the request types
```

Lighthouse\.Frontend\/src\/hooks\/useMetricsQuery\.ts

Create

```text
Owns the keyed query helper with its last-drawn memory, metricsQueryOptions, MetricsQuery, ownerOf, listOf and dayOf
```

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts

Extend

```text
Keeps the hook and its query calls in the same order; imports the three new modules
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx

Extend

```text
Import lines only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx

Extend

```text
Import lines only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useCumulativeStateTimeChoices\.ts

Extend

```text
Import lines only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useFilteredView\.ts

Extend

```text
Import lines only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PbcOverTimeWidget\.tsx

Extend

```text
Import line only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PercentilesOverTimeWidget\.tsx

Extend

```text
Import line only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PredictabilityScoreDetailsWidget\.tsx

Extend

```text
Import line only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ThroughputRunChartCard\.tsx

Extend

```text
Import line only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ThroughputRunChartCard\.loading\.test\.tsx

Extend

```text
Import line only; no row changes
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePbcOverTime\.ts

Extend

```text
Import line only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePbcOverTime\.test\.ts

Extend

```text
Import line only; no row changes
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePercentilesOverTime\.ts

Extend

```text
Import line only
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePercentilesOverTime\.test\.ts

Extend

```text
Import line only; no row changes
```

Lighthouse\.Frontend\/src\/layerBoundaries\.enforcement\.test\.ts

Extend

```text
Rename or reduce the two ALLOWED_LEAKS entries of useMetricsData.ts to equal what the ratchet measures after the move; the list never grows
```

Lighthouse\.Frontend\/src\/hooks\/metricsDataModules\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/hooks/metricsDataModules.test.tsx src/hooks/useMetricsData.test.ts src/pages/Common/MetricsView/usePbcOverTime.test.ts src/pages/Common/MetricsView/usePercentilesOverTime.test.ts src/pages/Common/MetricsView/useCumulativeStateTimeChoices.test.ts src/pages/Common/MetricsView/ThroughputRunChartCard.loading.test.tsx src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx src/pages/Common/MetricsView/BaseMetricsView.test.tsx src/layerBoundaries.enforcement.test.ts src/fileSize.enforcement.test.ts
```

#### Supports

None.

#### Public oracle

```text
Call applicabilityFor and metricsOwnerKeyOf as functions and renderHook(useMetricsQuery) and renderHook(useMetricsData) with provider-wrapped fake services, importing the three new modules by path
```

```text
Same applicability record per service shape; equal owners give equal keys; the helper keeps the last answer drawn for the owner on a failure and none for another owner; the fake service receives its calls in the order of the old hook
```

```text
Make providesSleRisk always true (the bare and portfolio rows fail); drop the lastDrawn update (the failure row fails); the oracle imports modules that do not exist before the move so it is RED until they do
```

##### Promises

P1

P3

#### Prefactoring

```text
L4 extract: move the applicability rules, the owner key and the query helper out of useMetricsData.ts in three move commits, import-line edits in a separate commit, before any later change to the metrics queries
```

```text
Every metrics query starts in the same order and answers as before; failures keep the last drawn answer
```

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.test\.ts

##### Verification

```text
pnpm --dir Lighthouse.Frontend test src/hooks/useMetricsData.test.ts src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx src/pages/Common/MetricsView/usePbcOverTime.test.ts src/pages/Common/MetricsView/usePercentilesOverTime.test.ts src/pages/Common/MetricsView/useCumulativeStateTimeChoices.test.ts
```

#### Records

##### Decisions

```text
Split by the three responsibilities named in the request. metricsOwnerKey.ts: pure types and one pure function, no imports of other metrics modules. metricsApplicability.ts: pure functions over a service object and the choices made, imports only MetricsFetchKey and the service types. useMetricsQuery.ts: the only effectful module; it carries metricsQueryOptions because the helper reads them, which keeps the dependency one way (useMetricsData -> useMetricsQuery); the old export of metricsQueryOptions from useMetricsData is removed and its importers change their line. useMetricsData.ts imports all three and keeps ONE_DAY_MS, allMetricsFetchKeys, CumulativeStateTimeChoices, MetricsData and the hook. State carriers: the last-drawn ref inside useMetricsQuery (per call, per component instance); no per-process state.
```

```text
The query calls are not split into sub-hooks and are not reordered: hooks run in call order and the order in which requests start is observable. The helper keeps its enabled, placeholderData and failure-logging behaviour exactly.
```

```text
Layer list: useMetricsData.ts keeps its value imports of getMetricsFetchKeys and fetchKeyStateOf, so its two entries stay. The new files import MetricsFetchKey from categoryMetadata (metricsApplicability) and QueryProgress from widgetStatus (useMetricsQuery); if the ratchet counts those type-only imports they are two new pairs, which the constraint forbids adding. In that case the crafter does not edit the list and returns the question to the orchestrator (owner: maintainer); a candidate resolution is to import the two types through a narrower module already allowed by the rules, to be decided by the maintainer, not by the crafter.
```

```text
The hook useStateDrillDown.ts created by the first slice imports metricsQueryOptions and the owner key; that one import line moves to the new owners as part of the import-edit commit. The file does not exist when this design is recorded, so it is named here and not as a target: the crafter edits that one import line, and the orchestrator confirms the target authorization when the slice starts.
```

##### Constraints

None.

##### Reuse

useMetricsData

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts\:342

Extend

```text
Remains the single owner of the query list and the effect; only pure and helper code leaves it
```

metricsOwnerKeyOf

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts\:251

CreateNew

```text
Same function moves to its own file; no other file decides the owner key (searched the importers)
```

applicabilityFor

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts\:175

CreateNew

```text
Moves to its own file; no other applicability rule exists in the repository
```

useMetricsQuery

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts\:286

CreateNew

```text
Moves to its own file; usePbcOverTime and usePercentilesOverTime use their own queries, not this helper, so it has one caller family and no merge is warranted
```

metricsApplicability

Lighthouse\.Frontend\/src\/hooks\/metricsApplicability\.ts\:1

CreateNew

```text
New file receiving applicabilityFor, the service guards and narrowsAChartAlreadyDrawn; no other owner of applicability
```

metricsOwnerKey

Lighthouse\.Frontend\/src\/hooks\/metricsOwnerKey\.ts\:1

CreateNew

```text
New file receiving the owner key and request types; no other owner
```

useMetricsQueryModule

Lighthouse\.Frontend\/src\/hooks\/useMetricsQuery\.ts\:1

CreateNew

```text
New file receiving the query helper and metricsQueryOptions; no other owner
```

BaseMetricsView

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:12

Extend

```text
Import lines only
```

useCumulativeScopeChoice

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useCumulativeStateTimeChoices\.ts\:24

Extend

```text
Import lines only
```

useFilteredView

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useFilteredView\.ts\:5

Extend

```text
Import lines only
```

PbcOverTimeWidget

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PbcOverTimeWidget\.tsx\:15

Extend

```text
Import line only
```

PercentilesOverTimeWidget

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PercentilesOverTimeWidget\.tsx\:14

Extend

```text
Import line only
```

PredictabilityScoreDetailsWidget

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PredictabilityScoreDetailsWidget\.tsx\:5

Extend

```text
Import line only
```

ThroughputRunChartCard

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ThroughputRunChartCard\.tsx\:5

Extend

```text
Import line only
```

usePbcOverTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePbcOverTime\.ts\:2

Extend

```text
Import line only
```

usePercentilesOverTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePercentilesOverTime\.ts\:2

Extend

```text
Import line only
```

ALLOWED\_LEAKS

Lighthouse\.Frontend\/src\/layerBoundaries\.enforcement\.test\.ts\:19

Extend

```text
Existing list is the single owner of the allowed layer leaks; its entries are re-measured after the move
```

MetricsOwnerRequest

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ThroughputRunChartCard\.loading\.test\.tsx\:10

Extend

```text
Import line only
```

MetricsOwnerKey

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePbcOverTime\.test\.ts\:3

Extend

```text
Import line only
```

MetricsOwnerKey

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePercentilesOverTime\.test\.ts\:3

Extend

```text
Import line only
```

metricsOwnerKeyOf

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:27

Extend

```text
Import lines only
```

##### Impact

module

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts\:1

Changed

```text
Gives away three responsibilities; keeps its exported hook, MetricsData and CumulativeStateTimeChoices
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:12

Migrated

```text
Import of metricsOwnerKeyOf and request types points at metricsOwnerKey; metricsQueryOptions at useMetricsQuery
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:28

Migrated

```text
Imports per symbol from the new owner
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useCumulativeStateTimeChoices\.ts\:6

Migrated

```text
Imports per symbol from the new owner
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useFilteredView\.ts\:5

Migrated

```text
Imports per symbol from the new owner
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PbcOverTimeWidget\.tsx\:15

Migrated

```text
MetricsOwnerKey now from metricsOwnerKey
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PercentilesOverTimeWidget\.tsx\:14

Migrated

```text
Same
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/PredictabilityScoreDetailsWidget\.tsx\:5

Migrated

```text
MetricsOwnerRequest now from metricsOwnerKey
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ThroughputRunChartCard\.tsx\:5

Migrated

```text
Same
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePbcOverTime\.ts\:2

Migrated

```text
MetricsOwnerKey now from metricsOwnerKey
```

import

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePercentilesOverTime\.ts\:2

Migrated

```text
Same
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePbcOverTime\.test\.ts\:3

Migrated

```text
Import line only
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/usePercentilesOverTime\.test\.ts\:3

Migrated

```text
Import line only
```

test

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/ThroughputRunChartCard\.loading\.test\.tsx\:10

Migrated

```text
Import line only
```

test

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.test\.ts\:1

UnchangedCompatible

```text
Drives the hook through renderHook; keeps its imports unless it imports a moved symbol, in which case the crafter edits that import line only
```

architecture\-list

Lighthouse\.Frontend\/src\/layerBoundaries\.enforcement\.test\.ts\:33

Changed

```text
The two entries of useMetricsData.ts are measured again after the move; see the layer-list decision
```

module

Lighthouse\.Frontend\/src\/hooks\/metricsApplicability\.ts\:1

Changed

```text
New file; receives the applicability rules unchanged
```

module

Lighthouse\.Frontend\/src\/hooks\/metricsOwnerKey\.ts\:1

Changed

```text
New file; receives the owner key and request types unchanged
```

module

Lighthouse\.Frontend\/src\/hooks\/useMetricsQuery\.ts\:1

Changed

```text
New file; receives the query helper and metricsQueryOptions unchanged
```

##### Agreement

MetricsOwnerKey\, MetricsOwnerRequest\, metricsOwnerKeyOf and metricsQueryOptions exported by hooks\/useMetricsData\.ts

Lighthouse\.Frontend\/src\/hooks\/useMetricsData\.ts\:244

Producer

Migrated

```text
Exports move to metricsOwnerKey.ts and useMetricsQuery.ts with unchanged types and values
```

MetricsOwnerKey\, MetricsOwnerRequest\, metricsOwnerKeyOf and metricsQueryOptions exported by hooks\/useMetricsData\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:12

Consumer

Migrated

```text
Import line edited
```

MetricsOwnerKey\, MetricsOwnerRequest\, metricsOwnerKeyOf and metricsQueryOptions exported by hooks\/useMetricsData\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:28

Consumer

Migrated

```text
Import line edited
```

MetricsOwnerKey\, MetricsOwnerRequest\, metricsOwnerKeyOf and metricsQueryOptions exported by hooks\/useMetricsData\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useCumulativeStateTimeChoices\.ts\:6

Consumer

Migrated

```text
Import line edited
```

MetricsOwnerKey\, MetricsOwnerRequest\, metricsOwnerKeyOf and metricsQueryOptions exported by hooks\/useMetricsData\.ts

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/useFilteredView\.ts\:5

Consumer

Migrated

```text
Import line edited
```

useMetricsData hook signature and return value

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:202

Consumer

UnchangedCompatible

```text
Signature and returned fields stay
```

##### Not applicable

contract_tests

```text
Producers and consumers are in one deployable and change in one commit; the hook and component tests cover the join
```

##### Boundaries

```text
useMetricsData and its consumers through renderHook with QueryClientProvider; metricsApplicability and metricsOwnerKey as pure functions
```

###### Driven ports

```text
IMetricsService family (team, project, feature size) as fake services
```

```text
useMetricsData -> useMetricsQuery, metricsApplicability, metricsOwnerKey; useMetricsQuery -> metricsOwnerKey; metricsApplicability -> service types only; no cycle
```

###### Failures

None.

##### Observations

```text
applicabilityFor(service, choices) returns the record of fetch keys to booleans; metricsOwnerKeyOf returns id and update moment; useMetricsQuery returns progress and answer, keeps the last drawn answer for an owner on a failure, and keeps the previous window answer while the next loads; useMetricsData starts its requests in the same order
```

return values\; the ordered list of service calls on a fake service

```text
Timing between requests that start in the same render; only the order of starts is observed
```

##### Equality

None.

##### Constructors

None.

##### Laws

###### L5

```text
applicabilityFor is total over the fetch keys and returns for a team-shaped, a portfolio-shaped and a bare service the same record as before the move
```

Invariant

Domain

metricsApplicability

###### Treatment

Finite

###### Cases

```text
team service
```

```text
portfolio service
```

```text
service with feature sizes only
```

```text
bare service
```

```text
Four service shapes times two choice flags is a finite table; the old file is the reference
```

###### Evidence

None.

###### L6

```text
metricsOwnerKeyOf is pure and equal owners (same id, same lastUpdated) give equal keys
```

Algebraic

Domain

metricsOwnerKey

###### Treatment

Finite

###### Cases

```text
same id different update moment
```

```text
same owner twice
```

```text
Two-field value; rows suffice
```

###### Evidence

None.

###### L7

```text
useMetricsQuery on a failure for the same owner returns the last drawn answer; for another owner it returns none
```

Invariant

Application

useMetricsQuery

###### Treatment

Finite

###### Cases

```text
success, then failure, same owner
```

```text
success, then failure, other owner
```

```text
Small state machine; renderHook rows
```

###### Evidence

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
useMetricsData split
```

Inhabitation

Pass

DesignedNotBuilt

```text
input: owner and service from the metrics view; producers: the three new modules; consumer: useMetricsData and the eleven importers; public port: renderHook and the component tests; one-way import graph, no cycle
```

###### Evidence

None.

```text
useMetricsData split
```

PublicReachability

Pass

DesignedNotBuilt

```text
BaseMetricsView calls useMetricsData which calls the new modules; the oracle reaches each by path
```

###### Evidence

None.

##### Risks

###### Y4

Stressor

```text
Assumption: useMetricsQuery needs metricsQueryOptions from useMetricsData; negated, the helper and the hook import each other
```

```text
A one-way import graph
```

```text
metricsQueryOptions moves with the helper
```

```text
A cycle between useMetricsData and useMetricsQuery
```

```text
tsc and the module load would still accept a cycle between functions; reviewer reads the import lines
```

crafter

##### Architectural tests

###### T3

```text
The layer-boundary list names exactly the leaking file pairs after the move and does not grow
```

Lighthouse\.Frontend\/src\/layerBoundaries\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend test src/layerBoundaries.enforcement.test.ts
```

###### T4

```text
No production file reaches 1000 lines: useMetricsData.ts ends well under the threshold and the offender list stays empty
```

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend test src/fileSize.enforcement.test.ts
```

##### Contract tests

None.

### S3

#### Targets

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/UnbindConfirmationDialog\.tsx

Create

```text
Owns the unbind confirmation dialog and its props
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/WorkItemsTab\.tsx

Create

```text
Owns the work-items tab of a Delivery and its props
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx

Create

```text
Owns the feature-row parts: DeliveryRowActions, SelectionModeMarker, OwningTeams, featureColumns and ACTION_BUTTON_SX
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryHeaderCopy\.tsx

Create

```text
Owns the header copy: howTheFeaturesWereChosen, WhatIsWrongWithTheSource, whatTheHeaderChipSays and the constants they use
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.tsx

Extend

```text
Keeps the section component and its default export; imports the four new files
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySectionParts\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySectionParts.test.tsx src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection src/fileSize.enforcement.test.ts
```

#### Supports

None.

#### Public oracle

```text
Render UnbindConfirmationDialog, WorkItemsTab and the DeliveryRowActions part with RTL, and call whatTheHeaderChipSays and howTheFeaturesWereChosen, importing each new file by path
```

```text
The unbind confirm button is disabled until the dialog allows it and fires its handler when enabled; the work-items tab shows its rows; the header chip and how-chosen sentence carry the same words as before for a source-bound, a rule-based and a manual Delivery. The Overdue chip is rendered by the section component itself, does not move, and stays covered by DeliverySection.overdue.test.tsx, which is part of the S3 verification command
```

```text
Make the confirm button always enabled (the disabled row fails); change the how-chosen sentence or the chip text of a source-bound Delivery (the header copy rows fail); the oracle imports files that do not exist before the move so it is RED until they do. The Overdue wording is not a falsifier of this oracle; DeliverySection.overdue.test.tsx covers it
```

##### Promises

P1

P3

#### Prefactoring

```text
L4 extract: move the unbind dialog, the work-items tab, the feature-row parts and the header copy out of DeliverySection.tsx in four move commits, before FE2 moves the folder
```

```text
A Delivery section looks and acts exactly as before
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection
```

#### Records

##### Decisions

```text
Four files in the one folder, no barrel, index.ts untouched. Components keep their props and render output; the pure copy functions keep their signatures. The default export of DeliverySection.tsx stays the only export used outside the folder. DeliveryDetailTab and constants stay where their users are: constants used by one new file move with it, constants used by both files are exported from the file that owns the knowledge (deliveryHeaderCopy for the header words, the section for the tab type) and imported one way, section -> parts. The new files import nothing from DeliverySection.tsx.
```

##### Constraints

None.

##### Reuse

DeliverySection

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.tsx\:450

Extend

```text
Remains the owner of the section state and JSX; only components and copy leave it
```

UnbindConfirmationDialog

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.tsx\:882

CreateNew

```text
Moves to its own file; ArchiveConfirmationDialog.tsx is a different dialog (archive, other copy and rule), so merging them would be a false abstraction
```

WorkItemsTab

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.tsx\:949

CreateNew

```text
Moves to its own file; DeliveryMetricsTab and DeliveryNotesPanel are sibling tabs with other content, not a shared owner
```

featureColumns

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.tsx\:321

CreateNew

```text
Moves to its own file; ArchivedFeatureGrid has its own columns for archived rows, so no existing owner decides the live row columns
```

howTheFeaturesWereChosen

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.tsx\:94

CreateNew

```text
Moves to its own file with the other header copy; no other file owns this wording
```

openTheDialog

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryDialogTestKit\.tsx\:244

Reuse

```text
Existing support that renders the section dialogs with providers; the ten DeliverySection tests use it and stay as the oracle
```

UnbindConfirmationDialogFile

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/UnbindConfirmationDialog\.tsx\:1

CreateNew

```text
New file for the dialog moved from DeliverySection.tsx
```

WorkItemsTabFile

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/WorkItemsTab\.tsx\:1

CreateNew

```text
New file for the tab moved from DeliverySection.tsx
```

deliveryFeatureRowFile

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx\:1

CreateNew

```text
New file for the row parts moved from DeliverySection.tsx
```

deliveryHeaderCopyFile

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryHeaderCopy\.tsx\:1

CreateNew

```text
New file for the header copy moved from DeliverySection.tsx
```

##### Impact

component

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.tsx\:450

Changed

```text
Loses the four units; default export, props and output unchanged
```

export

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/index\.ts\:4

UnchangedCompatible

```text
Re-exports the default of DeliverySection; untouched
```

import

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/PortfolioDeliveryView\.tsx\:11

UnchangedCompatible

```text
Default import of DeliverySection; untouched
```

test

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySection\.test\.tsx\:1

UnchangedCompatible

```text
The ten DeliverySection test files drive the component through RTL and stay as they are
```

module

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/UnbindConfirmationDialog\.tsx\:1

Changed

```text
New file; receives the dialog unchanged
```

module

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/WorkItemsTab\.tsx\:1

Changed

```text
New file; receives the tab unchanged
```

module

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFeatureRow\.tsx\:1

Changed

```text
New file; receives the row parts unchanged
```

module

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryHeaderCopy\.tsx\:1

Changed

```text
New file; receives the header copy unchanged
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract changes: the default export of DeliverySection and its props stay; the new files are private to the folder and have only DeliverySection as consumer
```

contract_tests

```text
Producer and consumer files are in one folder and change in one commit; the section tests cover the join
```

##### Boundaries

```text
DeliverySection rendered through React Testing Library with DeliveryDialogTestKit; the new components rendered directly with RTL; the copy functions as functions
```

###### Driven ports

```text
None changes: Delivery API services, ApiServiceContext and useDeliveryManagement are untouched
```

```text
DeliverySection -> UnbindConfirmationDialog, WorkItemsTab, deliveryFeatureRow, deliveryHeaderCopy; none of them imports DeliverySection
```

###### Failures

None.

##### Observations

```text
Rendered text, roles and enabled states of the unbind dialog, work-items tab, row actions and header chip, and the strings returned by the copy functions, equal what the section rendered before
```

RTL queries by role and name\; function return strings

```text
Pixel layout; covered by the existing @screenshot of retiring a Delivery
```

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
DeliverySection split
```

Inhabitation

Pass

DesignedNotBuilt

```text
input: delivery, feature rows and handlers from useDeliveryManagement; producers: the four new files; consumer: DeliverySection; public port: PortfolioDeliveryView renders the default export
```

###### Evidence

None.

```text
DeliverySection split
```

PublicReachability

Pass

DesignedNotBuilt

```text
PortfolioDeliveryView imports DeliverySection which imports the four files; the oracle reaches each by path
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
DeliverySection.tsx ends well under 1000 lines and no production file is listed as an offender
```

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend test src/fileSize.enforcement.test.ts
```

##### Contract tests

None.

### S4

#### Targets

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.ts

Create

```text
Owns the form rules: dateInputValue, offeredTabs, boundSourceSelection, isValidFutureDate, nameErrorOf, dateErrorOf, getFirstBlockingError and ValidationOptions
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySelectionContent\.tsx

Create

```text
Owns the selection tab content: ManualSelectionContent, RuleBasedContent, SelectionModeContent, their prop types and selectionTabContent
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySelectionTabRow\.tsx

Create

```text
Owns SelectionTabButton, BusySelectionTab and SelectionTabRow
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryDialogParts\.tsx

Create

```text
Owns LoadingSpinner, SchemaLoadError, PremiumFeatureNotice, ValidationButton, MatchedFeaturesAlert and MatchedFeaturesGrid
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryCreateModal\.tsx

Extend

```text
Keeps the dialog component and its named export; imports the four new files
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryCreateModal\.test\.tsx

Extend

```text
Rows that only prove one pure form rule move down to deliveryFormRules.test.ts; the render rows that prove the dialog stay
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.test\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryFormRules.test.ts src/pages/Portfolios/Detail/Components/DeliveryGrid/deliverySelectionTabs.test.ts src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal src/fileSize.enforcement.test.ts
```

#### Supports

None.

#### Public oracle

```text
Call the seven form rules as functions, importing deliveryFormRules by path, with dates built relative to today at local midnight and delivery and tab fixtures built as the existing deliverySelectionTabs tests build them
```

```text
The values listed in the form-rule laws: yesterday and today are refused, tomorrow is accepted; blank name and missing date name their field with the delivery term; a source tab speaks alone; a bound delivery is offered only its own tab; the bound source selection is returned only for a matching source key
```

```text
Make isValidFutureDate accept yesterday (the date rows fail); the oracle imports a module that does not exist before the move so it is RED until it does
```

##### Promises

P1

P3

P4

#### Prefactoring

```text
L4 extract: move the form rules (with their rows), the selection content, the tab row and the small parts out of DeliveryCreateModal.tsx in four move commits, before FE2 moves the folder
```

```text
The create and edit dialog looks and acts exactly as before
```

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryCreateModal\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend test src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal src/pages/Portfolios/Detail/Components/DeliveryGrid/deliverySelectionTabs.test.ts
```

#### Records

##### Decisions

```text
deliveryFormRules.ts is a pure module (no React, no effects) holding the seven functions with their current signatures; it imports deliveryTabForDelivery and the tab types from deliverySelectionTabs.ts, so the dependency is one way. offeredTabs and boundSourceSelection do not extend deliverySelectionTabs.ts or deliverySelectionBody.ts: deliveryTabForDelivery answers which tab claims a stored delivery, deliverySelectionBody answers what the dialog body shows and whether save is allowed, while offeredTabs answers which tabs an edit dialog offers and boundSourceSelection what the picker is told as current; they change when the edit dialog changes, and the request names them with the other form rules and tests them together. Neither file is edited. The new components import nothing from DeliveryCreateModal.tsx; the section/dialog state stays in the dialog component.
```

```text
Rule-only rows move down: a row in the DeliveryCreateModal render tests that only proves one of dateInputValue, offeredTabs, boundSourceSelection, isValidFutureDate, nameErrorOf, dateErrorOf or getFirstBlockingError, and needs the dialog only to reach it, becomes a function row in deliveryFormRules.test.ts and its render row is deleted. A row that proves what the user sees or does stays. The acceptance designer names each moved row before the crafter deletes it.
```

##### Constraints

None.

##### Reuse

deliveryTabForDelivery

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliverySelectionTabs\.ts\:262

Reuse

```text
offeredTabs calls it; it already owns which tab claims a stored delivery
```

selectionBodyOf

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliverySelectionBody\.ts\:96

Reuse

```text
Already owns the body of the selection area; the dialog keeps calling it; the moved content component renders what it names
```

deliveryFormRules

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.ts\:1

CreateNew

```text
No existing owner of date, name, blocking-error, offered-tab or bound-source rules: deliverySelectionTabs.ts owns tab values and claims, deliverySelectionBody.ts owns body and save-allowed; they are different questions (see the form-rules decision)
```

DeliverySelectionContent

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySelectionContent\.tsx\:1

CreateNew

```text
Moves out of DeliveryCreateModal.tsx; DeliverySourceTab.tsx is the source picker which this content renders, not a competing owner
```

DeliverySelectionTabRow

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySelectionTabRow\.tsx\:1

CreateNew

```text
Moves out of DeliveryCreateModal.tsx; no other tab row exists in the folder
```

DeliveryDialogParts

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryDialogParts\.tsx\:1

CreateNew

```text
Small presentational parts move together; RetryByReopeningNotice.tsx and DeliverySourceUnavailableNotice.tsx are other notices with other copy, not the same parts
```

DeliveryCreateModal

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryCreateModal\.tsx\:582

Extend

```text
Remains the owner of the dialog state, handlers, effects and JSX
```

DeliveryCreateModal

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryCreateModal\.test\.tsx\:1

Extend

```text
Existing render test file; only rows that prove one pure rule leave it for the new rules test
```

##### Impact

component

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryCreateModal\.tsx\:582

Changed

```text
Loses four groups of code; named export, props and output unchanged
```

import

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryModals\.tsx\:6

UnchangedCompatible

```text
Named import of DeliveryCreateModal; untouched
```

test\-support

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryDialogTestKit\.tsx\:40

UnchangedCompatible

```text
Named import of DeliveryCreateModal; untouched
```

test\-support

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySourceTabFixture\.tsx\:25

UnchangedCompatible

```text
Named import of DeliveryCreateModal; untouched
```

test

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryCreateModal\.test\.tsx\:1

Changed

```text
Rule-only rows leave for deliveryFormRules.test.ts; other rows and the nine DeliveryCreateModal test files stay unmodified
```

export

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/index\.ts\:1

UnchangedCompatible

```text
Does not export DeliveryCreateModal; untouched, no barrel is added
```

module

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.ts\:1

Changed

```text
New file; receives the form rules unchanged
```

module

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySelectionContent\.tsx\:1

Changed

```text
New file; receives the selection content unchanged
```

module

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliverySelectionTabRow\.tsx\:1

Changed

```text
New file; receives the tab row unchanged
```

module

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryDialogParts\.tsx\:1

Changed

```text
New file; receives the small parts unchanged
```

##### Agreement

None.

##### Not applicable

agreement

```text
No shared contract changes: the named export of DeliveryCreateModal and its props stay; the new files are private to the folder and have only the dialog as consumer
```

contract_tests

```text
Producer and consumer files are in one folder and change in one commit; the dialog tests cover the join
```

##### Boundaries

```text
DeliveryCreateModal rendered through React Testing Library (create and edit); the form rules as pure functions
```

###### Driven ports

```text
None changes: useDeliveryFormQueries, Delivery API services and ApiServiceContext are untouched
```

```text
DeliveryCreateModal -> DeliverySelectionContent, DeliverySelectionTabRow, DeliveryDialogParts, deliveryFormRules -> deliverySelectionTabs; none of the new files imports DeliveryCreateModal
```

###### Failures

None.

##### Observations

```text
dateErrorOf, nameErrorOf, isValidFutureDate, getFirstBlockingError, offeredTabs, boundSourceSelection and dateInputValue return values; the dialog renders the same tab row, content, notices and errors as before
```

function return values\; RTL queries

```text
Whether the dialog passes the right arguments to a rule; covered by the dialog render tests that stay
```

##### Equality

deliveryFormRules observations

```text
Two implementations are equal when every rule returns the same value for the same arguments, including undefined for no error and the same array of tab objects (same identity of the tab values) for offeredTabs
```

##### Constructors

None.

##### Laws

###### L3

```text
isValidFutureDate accepts exactly the dates after today at local midnight; empty is invalid; dateErrorOf and nameErrorOf name the missing or invalid field with the delivery term; getFirstBlockingError lets a source tab speak alone, and otherwise orders name, date, tab error
```

Invariant

Domain

deliveryFormRules

###### Treatment

Finite

###### Cases

```text
empty date
```

```text
yesterday
```

```text
today
```

```text
tomorrow
```

```text
blank and padded name
```

```text
source tab with empty name and date
```

```text
manual tab with all three errors in order
```

```text
Boundary table around local midnight and a three-level precedence; a finite table of rows is the honest oracle, a generated property would need an oracle that copies the rule
```

###### Evidence

None.

###### L4

```text
offeredTabs returns all tabs when no delivery is edited or when the delivery is claimed by the manual or rule tab, and only the claiming source tab when a source claims it; boundSourceSelection returns a selection only when the delivery has a source reference and its source key equals the tab source key
```

Invariant

Domain

deliveryFormRules

###### Treatment

Finite

###### Cases

```text
no delivery
```

```text
delivery claimed by a source
```

```text
delivery with no source reference
```

```text
delivery bound to another source key
```

```text
delivery bound to this source
```

```text
Five finite cases
```

###### Evidence

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
DeliveryCreateModal split
```

Inhabitation

Pass

DesignedNotBuilt

```text
input: delivery, sources and form values from the dialog; producers: the four new files; consumer: DeliveryCreateModal; public port: DeliveryModals renders the named export; deliveryFormRules imports deliverySelectionTabs one way
```

###### Evidence

None.

```text
DeliveryCreateModal split
```

PublicReachability

Pass

DesignedNotBuilt

```text
DeliveryModals imports DeliveryCreateModal which imports the new files; the oracle reaches deliveryFormRules by path
```

###### Evidence

None.

##### Risks

None.

##### Architectural tests

###### T2

```text
DeliveryCreateModal.tsx ends well under 1000 lines and no production file is listed as an offender
```

Lighthouse\.Frontend\/src\/fileSize\.enforcement\.test\.ts

```text
pnpm --dir Lighthouse.Frontend test src/fileSize.enforcement.test.ts
```

##### Contract tests

None.

## ADRs

None.
