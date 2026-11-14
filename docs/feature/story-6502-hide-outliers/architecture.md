# Architecture brief: story\-6502\-hide\-outliers

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Rule: a closed Work Item is an outlier when its Cycle Time is above 5 times the chart's 50th percentile, computed with the backend convention (sort ascending, index floor(0.5 x N) - 1, clamped to 0). The constant OUTLIER_MEDIAN_MULTIPLE = 5 lives once in Lighthouse.Frontend/src/utils/charts/outliers.ts; the rule is a pure function over the full closed set of the selected date range and named Cycle Time, never over the visible-type subset, so toggling a type never moves the cut or the axis.
```

#### D2

```text
Hexagonal view (frontend only): driving UI = BaseMetricsView/widgetNodes (unchanged); core = pure modules outliers.ts, outlierWording.ts, offScaleLines.ts; shell = CycleTimeScatterPlotChart (derives with useMemo, replacing the two effect+state derivations, so chip count, axis and dots come from one render), OutlierChip, OutlierPopoverContent, OffScaleLineMarkers. Backend, API, DTOs, RBAC, migrations, domain events: unchanged. No Plan/dry-run function exists. Layers: domain = outlier partition of the closed set; application = chip and popover state (one open flag), dialog state {title, items}; adapter = MUI X drawing-area hooks for markers, WidgetShell header slot; infrastructure = tagged named-percentile fetch so a late answer for an old definition never lands.
```

#### D3

```text
Statistics stay untouched: percentiles, SLE share and RAG, tiles, View Data, forecasts and the dialog still receive the unpartitioned arrays; only the chart drawing and the chip receive the partition. Props are never mutated (tests pass deep-frozen props). With no outliers the chart renders exactly as before (no chip, today's axis, no markers).
```

#### D5

```text
Copy (maintainer S-4, S-6): popover title '{n} outlier(s)', line 1 'Not drawn: longer than {cut} days (5 times the median, {median} days).', line 2 'Still counted in the percentiles, {SLE} and View Data.', conditional line 'Above the axis: 95% at {p95} days, {SLE} at {sle} days.', then a table (closed date, Cycle Time days, times the median; factor rounding from 10 up is half-up) capped at 10 rows, then 'Open in a list' (slice 3). Empty plot: 'All visible {Work Items} are outliers.' or 'All types are hidden.' Work Item, Cycle Time and SLE render in the configured terms; 'outlier' is a fixed word.
```

#### D6

```text
Edge markers (maintainer S-5): a percentile or SLE line above the new axis top is drawn as a labelled triangle marker in the plot's top margin via OffScaleLineMarkers (aria-hidden, reads only useDrawingArea); fallback if the browser spike fails is a text line under the legend row. The Zenith demo has no line above the cut, so markers are proven by RTL and unit tests on fixtures, not E2E geometry.
```

#### D7

```text
Usage data: name-only event OutlierPopoverOpened = 27 (next integer after SystemWideAccessGranted = 26, appended, never renumbered) in UsageDataEventName.cs and UsageDataService.ts, row in docs/settings/usagedata.md; reported once per closed-to-open change through the chip's usage-event callback prop (the chip imports no service). Emitted in slice 1, the slice that first makes the chip usable.
```

#### D8

```text
Claim-to-checker: Quint covers the pure partition and type-toggle state model (docs/feature/story-6502-hide-outliers/model/outliers.qnt: partition of visible items, cut stable under toggles, chip count, all-types-hidden); TLA+ and Agda not needed (no concurrency, no proof obligation beyond finite sets). Broad numeric laws (median convention, boundary equality, monotone axis, line split) are checked by fast-check at the pure-module seam, numRuns 100, because Quint int sets do not cover arbitrary arrays. Liveness: not applicable, render-time pure derivation and one boolean open flag, no eventual-progress claim.
```

#### D9

```text
Type strength: native TypeScript only (repository checker is tsc -b with Biome). Outlier partition is returned as a record {drawn, outliers, median, cut} so a caller cannot take the cut without the median; OutlierChip props make rows, wording and the three effect ports (onOpenInList, onClose, usage callback) explicit. Representable invalid states left to runtime: negative or NaN Cycle Time (backend never emits; findOutliers treats the empty set as no median and returns no outliers).
```

#### D10

```text
Paradigm object_oriented project, functional-leaning React; no anemic-entity concern since no domain entity is added: the new rule is a cohesive pure module (outliers.ts) owning the median, cut and partition decisions, and the chart component owns only view state. nw-code-design-oo smell classes apply only to the chart component (keep it free of the median and cut decision).
```

#### D11

```text
Test substrate: Vitest + React Testing Library with the existing MUI mocks; one oracle file per slice under Lighthouse.Frontend/src/components/Common/Charts; command is pnpm --dir Lighthouse.Frontend exec vitest run <oracle path relative to Lighthouse.Frontend>. Per-test default budget applies, no derogation requested. No nWave scenario tags in code. The backend demo row (TZ-301) and enum member are pinned by existing backend test conventions at the application-service port, owned by the crafter and acceptance designer of slice 1; E2E page object for CycleTimeScatterPlotChart is extended with chip and popover and waits for data-widget-status=ready.
```

#### D12

```text
Chip placement (maintainer S-3): OutlierChip renders in the WidgetShell header after the RAG chip, small outlined, (!) icon plus '1 outlier' / '{n} outliers', words dropped below 600 px, accessible name keeps the full sentence, absent with no outliers. The chart hands the chip to the header through a header-slot context offered by WidgetShell and rendered by WidgetHeader; the same context carries frameIsReady (frame ready). While the widget loads the chip is disabled and never shows an earlier count and its popover closes; when the widget has failed, the could-not-load message replaces the plot and the chip (hidden, not disabled). Crafter may choose portal or context-held node; the oracle observes the header.
```

#### D13

```text
Slice 4 (maintainer guided review) supersedes every popover statement in the shared and slice 1 and 3 records: the outlier chip is a plain amber outlined disclosure button (MUI Chip color warning, variant outlined, at every width and while loading) that opens the existing Work Items dialog directly with exactly the visible-type outliers, longest first. No popover, no table, no extra dialog lines; the rule stays in the widget help. The chip has no open state: it is stateless apart from disabled while the frame is not ready. Usage event 27 is named OutlierListOpened, reported once per click that opens the dialog, name-only, through the chip single open callback. The chip accessible name and tooltip are one string built by outlierWording: "{n} outlier(s) not drawn on the chart. [Above the axis: ... .] Show the list." with the Above the axis sentence only when a percentile or SLE line is above the axis. The partition rule, cut, axis, markers, filters, empty-plot copy, dialog title and the Quint model of the partition are unchanged; the model never modelled the popover, so no re-run is owed for it. Checker mapping for S4: Quint model for the unchanged partition claims; the S4 claims are finite example observations in Vitest and one Playwright skeleton; no new state machine exists (a state flag is removed), so no TLA+ or Agda claim is made and liveness is not applicable.
```
Governed slices: S4

#### D14

```text
Follow-up for docs, owned by the crafter of S4 at the finalize checklist, not a decision change: ADR-282 and the feature brief still say popover in several sentences (chip plus popover, the popover line naming the lines above the axis, the chip disabled and open popover closing); their rule (5 times the median, per-chart, backend convention) is unchanged. ADR-282 should read: the chip opens the list dialog, and the chip accessible name names the lines above the axis.
```
Governed slices: S4

### Constraints

#### K2

```text
Scope out: other charts (Estimation vs Cycle Time, Feature Size, Aging, process behaviour, throughput), View Data, backend percentile convention (mirrored, table test against PercentileCalculator index for N = 1 to 6, 10, 11), showing outliers in place. Deviation: none from the authority.
```

#### K3

```text
Input bounds: Cycle Time is a whole number of days >= 1 (inclusive calendar days); closed Work Item count N is unbounded (0, 1 and 2 items give a median but, for 1 item, the item cannot exceed 5 times itself so no outlier). Multiple 5 and the exclusive comparison (value > 5 x median; value == cut is drawn) are fixed, not configurable. Popover lists outliers longest first, up to 10 rows, then 'and k more' for the rest; no bound on outlier count.
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
No external service, wire format or independently released consumer changes; the usage event name is a same-commit enum pair covered by the slice 1 oracle and existing backend tests.
```

architectural_tests

```text
The existing fallow layer check (layerBoundaries.enforcement.test.ts) already forbids utils importing services, hooks and pages and components importing pages, and it governs the new modules by directory; the design adds no new dependency law.
```

### Boundaries

```text
CycleTimeScatterPlotChart React props (closed work items, percentiles, SLE, owner kind, frameIsReady) rendered through React Testing Library; pure functions in utils/charts/outliers.ts
```

#### Driven ports

```text
WorkItemsDialog (props only), usage-event callback prop, useChartVisibility, MUI X drawing-area hooks
```

```text
components -> utils/charts (pure) ; pages/Common/MetricsView/WidgetShell provides header slot context consumed by components; utils import only chartAxisUtils; no component imports a page (fallow layer rule already enforced)
```

#### Failures

##### B2

```text
named Cycle Time answer for a superseded definition arrives
```

Refusal

```text
answer ignored; cut and chip follow the selected definition
```

##### B3

```text
frame has failed to load
```

Refusal

```text
the widget could-not-load message replaces the plot and the chip; the chip is hidden, not disabled, and no popover or count remains
```

### Observations

```text
Outlier partition of the full closed set: {drawn, outliers, median, cut}; chip count = outliers among visible types; axis top = 1.1 x max of drawn values, percentiles and SLE
```

OutlierPartition record \/ integer count \/ number

```text
Which hidden-type items would have been outliers (only visible ones are counted); two items with equal Cycle Time and date
```

### Equality

OutlierPartition

```text
Equal iff same median, same cut and same drawn/outlier id sets; independent of input order and of which types are visible
```

### Constructors

None.

### Laws

#### L1

```text
For every closed set, the cut is 5 x the backend-convention 50th percentile of the full set, and an item is an outlier iff its Cycle Time is strictly above the cut; drawn and outliers partition the set, order kept
```

Invariant

Domain

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

##### Treatment

Broad

###### Generator

```text
arrays of positive integers (Cycle Time days) with duplicates and with items placed exactly at, one below and one above the cut
```

0 to 200 items\, values 1 to 400

fast\-check fixed seed in the file\, numRuns 100\, failing seed printed

###### Must include

```text
empty set
```

```text
single item
```

```text
item exactly at the cut
```

```text
all items equal
```

flat arrays

```text
fast-check built-in array and integer shrinking
```

```text
reference oracle: sort copy, index floor(N/2)-1 clamped, compare with strict >, written in the test and not reusing the module
```

```text
mutant using >= instead of >, floor(N/2) without minus one, or 4 instead of 5 fails the property
```

##### Evidence

docs\/feature\/story\-6502\-hide\-outliers\/model\/outliers\.qnt

#### L2

```text
Toggling a Work Item type never changes the cut or the axis top; the chip count equals the outliers of visible types only; all types hidden gives zero count
```

Invariant

Application

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

##### Treatment

Finite

###### Cases

```text
toggle each of two types off and on over a set with one outlier: cut and axis equal before and after
```

```text
all types hidden: no chip, empty-plot copy
```

```text
state space is two type flags; Quint model checks all toggle traces to 6 steps
```

##### Evidence

docs\/feature\/story\-6502\-hide\-outliers\/model\/outliers\.qnt

#### L3

```text
The axis top is 1.1 x the largest of the drawn Cycle Times, every percentile and the SLE, so no drawn item or line value is lost, and a line above the top is split into off-scale markers with drawn + off-scale = all lines
```

Invariant

Domain

Lighthouse\.Frontend\/src\/utils\/charts\/offScaleLines\.ts

##### Treatment

Broad

###### Generator

```text
arrays of drawn Cycle Times and arrays of line values (percentiles, SLE) as positive numbers
```

0 to 50 values\, 1 to 500

fast\-check fixed seed\, numRuns 100

###### Must include

```text
line exactly at the axis top
```

```text
no lines
```

flat

```text
fast-check built-in
```

```text
test recomputes the max and splits with a filter
```

```text
mutant that drops the SLE from the axis or splits on >= instead of > fails
```

##### Evidence

None.

### Refuted

```text
Cut computed on the visible-type subset keeps the chip truthful
```

```text
carrier: the subset changes the median, so toggling a type moves the cut and axis
```

```text
Types A (1..9 days) and B (50 days items): hiding B changes the median and re-labels A items as outliers; the cut is therefore taken from the full set
```

### Claims

#### C1

```text
A hidden outlier is never lost from statistics: every percentile, SLE share, RAG, tile, View Data and forecast is computed from the unpartitioned data
```

```text
A silent number change after the feature ships
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

```text
closed work items, percentiles and SLE props
```

findOutliers in outliers\.ts

chart drawing and OutlierChip only

```text
percentile legend values, SLE line, dialog items and View Data unchanged with an outlier present
```

```text
a future edit passing drawn items to statistics; guarded by a frozen-props test and a wiring-point test
```

### States

Admitted

```text
Zero, one or many outliers; every type visible, some hidden, all hidden; named Cycle Time selected or default
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

Excluded

```text
A chip showing a count while the frame is loading or failed; a cut computed from the visible-type subset; a chip with zero outliers
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx

Unknown

```text
MUI X behaviour of drawing in the top margin in the installed version; resolved by a browser spike inside slice 1 with the text-line fallback
```

orchestrator at slice 1 review

### Models

Quint

docs\/feature\/story\-6502\-hide\-outliers\/model\/outliers\.qnt

##### Properties

partitionOk

Invariant

```text
drawn and outliers partition the visible items
```

findOutliers

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

cutStable

Invariant

```text
the cut is unchanged by any type toggle
```

findOutliers

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

chipOk

Invariant

```text
chip count counts visible outliers only; all types hidden gives none
```

OutlierChip

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx

outlierIsSeparated

Invariant

```text
every outlier is strictly above the cut and every drawn item at or below it
```

findOutliers

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

##### Atoms

State

hidden

useChartVisibility

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

None.

Action

toggle

useChartVisibility

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

None.

Constant

cut

OUTLIER\_MEDIAN\_MULTIPLE

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

None.

##### Assumptions

```text
Finite fixture of eight Cycle Times and two types stands for the rule; arbitrary arrays are covered by fast-check, not by Quint
```

##### Mutants

None.

### Closure

```text
outlier partition and chip count
```

Safety

Pass

DesignedNotBuilt

```text
Quint verify NoError to 6 steps on four invariants; invalid states (cut from visible subset, stale count) excluded by deriving from props with useMemo and disabling the chip while not frameIsReady
```

##### Evidence

docs\/feature\/story\-6502\-hide\-outliers\/model\/outliers\.qnt

```text
outlier partition and chip count
```

Inhabitation

Pass

DesignedNotBuilt

```text
Zenith demo data with TZ-301 (computed 58 days against cut 30) constructs a one-outlier chart from admitted input
```

##### Evidence

None.

```text
outlier chip and popover
```

PublicReachability

Pass

DesignedNotBuilt

```text
closed items prop -> findOutliers -> OutlierChip in WidgetShell header slot -> RTL screen queries; Open in a list -> WorkItemsDialog (slice 3). Acyclic, seeded by the chart props
```

##### Evidence

None.

```text
outlier chip and popover
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: render-time pure derivation and one open flag; no eventual-progress claim, no concurrency beyond the late named-percentile answer, which is a safety tag check
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: the median is stable across the date range; negate: range with one closed item or all equal items
```

```text
chart exactly as before when no item exceeds 5 x median
```

```text
no outlier on a single item
```

```text
a drawn item dropped without a chip entry
```

```text
very small N gives a noisy median; accepted
```

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

#### Y2

Stressor

```text
Assumption: named Cycle Time answer arrives for the selected definition; negate: late answer for an old definition after the reader switched
```

```text
chip and axis follow the selected definition
```

```text
briefly the default percentiles
```

```text
percentiles of an old definition used for the cut
```

```text
none beyond the tag check
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

#### Y3

Stressor

```text
Assumption: widget data is current when drawn; negate: slow connection, request failed
```

```text
no stale count
```

```text
chip dimmed or disabled, widget could-not-load message
```

```text
count from a previous answer; empty area read as no data
```

```text
header-slot context must reset when the chart unmounts
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WidgetShell\.tsx

#### Y4

Bypass

```text
A caller of the chart passes already-filtered items or a second chart adopts the rule differently
```

```text
single owner of the 5 x median rule
```

```text
second chart imports outliers.ts
```

```text
a second multiple constant
```

```text
constant-versus-copy test pins the wording to the constant
```

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

Create

```text
Pure rule: fiftiethPercentile, findOutliers, axisTop, timesTheMedian, formatTimesTheMedian, OUTLIER_MEDIAN_MULTIPLE
```

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts

Create

```text
Term-injected popover, chip and tooltip strings
```

Lighthouse\.Frontend\/src\/utils\/charts\/offScaleLines\.ts

Create

```text
splitLinesByAxis and markerSpecs, pure
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OffScaleLineMarkers\.tsx

Create

```text
Top-margin markers, aria-hidden
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx

Create

```text
Header chip with popover open flag and effect-port props
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx

Create

```text
Pure popover view with 10-row table
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

Extend

```text
Memoised partition, filter, axis, chip handoff, tagged named percentiles
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WidgetShell\.tsx

Extend

```text
Header slot context rendered after the RAG chip, carries frameIsReady
```

Lighthouse\.Frontend\/src\/services\/Api\/UsageDataService\.ts

Extend

```text
Add OutlierPopoverOpened name
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs

Extend

```text
Append OutlierPopoverOpened = 27
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoData\/Team Zenith\.csv

Extend

```text
Add closed item TZ-301 above 5 x Zenith's 50th percentile
```

docs\/settings\/usagedata\.md

Extend

```text
Row for OutlierPopoverOpened
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.outliers.test.tsx
```

#### Supports

None.

#### Public oracle

```text
Render CycleTimeScatterPlotChart (RTL, inside the WidgetShell header host) with a Team Zenith-like closed set whose median is 6 days and one item of 58 days, plus percentiles and an SLE; click the chip in the header. Also render a set with 11 outliers, toggle Work Item type chips off and on, and render the frame loading and failed
```

```text
The 58-day item is not drawn and the axis top is 1.1 x the largest drawn value, percentile and SLE; the header shows chip '1 outlier'; the popover says 'Not drawn: longer than 30 days (5 times the median, 6 days).', lists the item with closed date, 58 and 9.7x; with 11 outliers the popover lists the 10 longest first and then 'and 1 more'; a line above the axis appears as a top-edge marker and in the 'Above the axis' line; percentile legend values, SLE, dialog items are those of the unpartitioned data; with no item above the cut there is no chip and the axis is as before; toggling types keeps the cut and axis still and the chip counts only visible-type outliers; while loading the chip is disabled with no count; when failed the could-not-load message replaces plot and chip (chip hidden)
```

```text
A chart that drops the item from statistics, uses >= or another multiple, computes the median from drawn items, moves the cut or axis on a type toggle, lists more than 10 rows or not longest first, shows a chip or a stale count after failure or while loading, or draws the item, fails
```

##### Promises

P1

P2

P3

P5

#### Prefactoring

```text
L2 extract: turn the axis and grouped-dots useEffect+setState derivations of CycleTimeScatterPlotChart into useMemo values, before any outlier behaviour; makes the partition and chip count derive in one render
```

```text
Chart output for sets without outliers, dialog items, legend and axis values
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

getMaxYAxisHeight

Lighthouse\.Frontend\/src\/utils\/charts\/chartAxisUtils\.ts\:19

Reuse

```text
Already returns 1.1 x the largest of data values, percentiles and SLE; axisTop delegates to it; Feature Size and Estimation callers untouched
```

CycleTimeScatterPlotChart

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:22

Extend

```text
Owns the view state of this chart; two useEffect+setState derivations (axis, grouped dots) become useMemo so chip, axis and dots share one render
```

LegendChip

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/LegendChip\.tsx\:1

CreateNew

```text
LegendChip has aria-pressed toggle semantics; the outlier chip is a disclosure button, so a new OutlierChip (MUI Chip outlined) copies its look only
```

WidgetShell

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WidgetShell\.tsx\:42

Extend

```text
Header row already owns info, View Data, trend and RAG chips; adding a slot after RAG is cheaper than lifting type-chip state out of the chart
```

fiftiethPercentile

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts\:1

CreateNew

```text
Frontend has no median or outlier code; paceBands.ts and sleRisk.ts are the precedent for a pure chart module
```

OffScaleLineMarkers

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/BlackoutOverlay\.tsx\:2

CreateNew

```text
BlackoutOverlay shows the useDrawingArea overlay pattern but draws bands, not labelled markers
```

PercentileCalculator

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/FlowMetrics\/Domain\/PercentileCalculator\.cs\:8

Reuse

```text
Backend convention is mirrored not changed; fiftiethPercentile reproduces the index and a table test pins both
```

outlierWording

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:1

CreateNew

```text
Term-injected wording; paceBands.ts and sleRisk.ts are the precedent, nothing owns outlier copy
```

splitLinesByAxis

Lighthouse\.Frontend\/src\/utils\/charts\/offScaleLines\.ts\:1

CreateNew

```text
No existing function splits percentile and SLE lines by the axis top
```

OffScaleLineMarkers

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OffScaleLineMarkers\.tsx\:1

CreateNew

```text
Thin SVG component over the pure spec; BlackoutOverlay draws bands, not labelled markers
```

OutlierChip

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx\:1

CreateNew

```text
LegendChip has toggle semantics; this is a disclosure button with a popover
```

OutlierPopoverContent

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx\:1

CreateNew

```text
InfoPopoverButton body is text only; this body is a table of rows
```

UsageDataEventName

Lighthouse\.Frontend\/src\/services\/Api\/UsageDataService\.ts\:51

Extend

```text
Existing closed list of usage events
```

UsageDataEventName

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs\:96

Extend

```text
Existing enum; append 27
```

TZ\-301

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoData\/Team Zenith\.csv\:1

CreateNew

```text
New data row in the existing Zenith file; no row owns an outlier case
```

OutlierPopoverOpened

docs\/settings\/usagedata\.md\:1

CreateNew

```text
New catalog row in the existing file
```

##### Impact

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:22

Changed

```text
Partition, axis, header chip handoff
```

component

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WidgetShell\.tsx\:42

Changed

```text
Header slot context; every other widget renders unchanged
```

consumer

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetNodes\.tsx\:362

UnchangedCompatible

```text
cycleScatter node still passes the same props; frameIsReady arrives through the shell context
```

test\-support

Lighthouse\.EndToEndTests\/tests\/models\/metrics\/CycleTimeScatterPlotChart\.ts\:1

Changed

```text
Page object gains chip and popover accessors; waits for data-widget-status=ready
```

existing\-test

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.test\.tsx\:1

UnchangedCompatible

```text
Existing scenarios use sets without an item above 5 x median, so output is unchanged
```

enum

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs\:96

Changed

```text
Appended member 27; no renumbering
```

demo\-data

Lighthouse\.Backend\/Lighthouse\.Backend\/Factories\/DemoData\/Team Zenith\.csv\:1

Changed

```text
One closed item TZ-301 added
```

new\-module

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts\:1

Changed

```text
New pure module
```

new\-module

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:1

Changed

```text
New pure module
```

new\-module

Lighthouse\.Frontend\/src\/utils\/charts\/offScaleLines\.ts\:1

Changed

```text
New pure module
```

new\-component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OffScaleLineMarkers\.tsx\:1

Changed

```text
New component, mounted only by the chart
```

new\-component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx\:1

Changed

```text
New component, mounted only by the chart
```

new\-component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx\:1

Changed

```text
New component, used only by the chip
```

export\-list

Lighthouse\.Frontend\/src\/services\/Api\/UsageDataService\.ts\:51

Changed

```text
One name appended; consumers read by name
```

docs

docs\/settings\/usagedata\.md\:1

Changed

```text
One row
```

##### Agreement

UsageDataEventName OutlierPopoverOpened \= 27

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs\:96

Producer

Migrated

```text
Member appended
```

UsageDataEventName OutlierPopoverOpened \= 27

Lighthouse\.Frontend\/src\/services\/Api\/UsageDataService\.ts\:51

Consumer

Migrated

```text
Name appended to the frontend list; enums serialise as strings out
```

WidgetShell header slot

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WidgetShell\.tsx\:42

Producer

Migrated

```text
Offers slot and frameIsReady
```

WidgetShell header slot

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:22

Consumer

Migrated

```text
Fills the slot
```

##### Not applicable

architectural_tests

```text
Covered by the shared record: existing fallow layer check governs the new files
```

contract_tests

```text
Covered by the shared record: no independently released consumer
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

### S3

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx

Extend

```text
Open in a list button, onOpenInList effect port
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx

Extend

```text
Focus returns to the chip on close
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

Extend

```text
Dialog state becomes {title, items}; chip hands the outliers to the single dialog
```

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts

Extend

```text
WidgetInfoTerms gains cycleTime; cycleScatter description built from terms for both owner kinds, long outlier rule sentence
```

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts

Extend

```text
Help sentence and dialog title wording
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.outlierList.test.tsx
```

#### Supports

None.

#### Public oracle

```text
Render the chart with outliers, open the chip popover, activate 'Open in a list'; render the cycleScatter widget info through getWidgetInfo with custom Terminology
```

```text
The outliers open in the existing Work Items dialog with exactly the chip's outliers (same ids, effective copies), the popover closes, and on dialog close focus returns to the chip; the widget help names the outlier rule (5 times the 50th percentile) using the configured Work Item and Cycle Time terms, with 'outlier' fixed
```

```text
A dialog with all items or the drawn items, focus lost to the body, or help text with hard-coded Work Item or Cycle Time words fails
```

##### Promises

P4

#### Prefactoring

```text
L1: dialog state {items, derived title} becomes one {title, items} state in the chart, before wiring the chip
```

```text
Dot-click dialog opens with the same title and items
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

WorkItemsDialog

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.tsx\:1

Reuse

```text
Presentation only; handed {title, items, context}; no change to the dialog
```

getWidgetInfo

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts\:156

Extend

```text
Existing cycleScatter entry; today only team gets term-built wording
```

CycleTimeScatterPlotChart

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:22

Extend

```text
Owns the single dialog state
```

OutlierPopoverContent

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx\:1

CreateNew

```text
File is created in slice 1 and does not exist yet; this slice adds the Open in a list button to it
```

OutlierChip

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx\:1

CreateNew

```text
File is created in slice 1 and does not exist yet; this slice adds focus return to it
```

outlierWording

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:1

CreateNew

```text
File is created in slice 1 and does not exist yet; this slice adds the help and dialog title strings to it
```

##### Impact

consumer

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.test\.ts\:1

UnchangedCompatible

```text
Existing expectations hold for other widgets; cycleScatter wording changes but is checked by the slice oracle
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:557

Changed

```text
Single dialog gets a title
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx\:1

Changed

```text
New button and effect port
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx\:1

Changed

```text
Focus return
```

pure\-module

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts\:156

Changed

```text
cycleScatter help text and cycleTime term
```

pure\-module

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:1

Changed

```text
Extra strings
```

##### Agreement

WidgetInfoTerms\.cycleTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/widgetInfoMetadata\.ts\:156

Producer

Migrated

```text
New term field
```

WidgetInfoTerms\.cycleTime

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/BaseMetricsView\.tsx\:1

Consumer

Migrated

```text
Passes the configured Cycle Time term
```

##### Not applicable

architectural_tests

```text
Covered by the shared record
```

contract_tests

```text
Covered by the shared record
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

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx

Extend

```text
Amber outlined chip (color warning), no Popover and no open state; props reduce to wording, count, aboveTheAxis, disabled, onOpen, chipRef; onClick calls onOpen; aria-label and Tooltip title are the one accessible name; keeps disabled Outliers look while loading, phone label (!) N
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx

Extend

```text
Removed: the popover view and its OutlierRow type go; nothing else imports them
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/outlierHeaderChip\.tsx

Extend

```text
Drops OUTLIER_POPOVER_ROWS, row building, median and cut inputs and the two callbacks; passes count, aboveTheAxis sentence, disabled, onOpen, chipRef; still returns null with no outlier of a visible type
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/outlierPlot\.ts

Extend

```text
Removes buildOutlierRows and the OutlierRow import (popover only); formatClosedDate goes with it; the axis and marker helpers stay
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

Extend

```text
Chip handoff: onOpen reports OutlierListOpened and sets the dialog to {title: wording.listTitle, items: visible outliers}; stops passing median, cut, rows; chipRef focus return on dialog close stays
```

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts

Extend

```text
chipAccessibleName(count, aboveTheAxis | null) builds the D16 sentence; removes popoverTitle, notDrawn, stillCounted, openInAList, moreRows, columns; keeps chipLabel, chipLabelWhileLoading, chipAccessibleNameWhileLoading, aboveTheAxis, listTitle, empty-plot strings, outlierRuleSentence
```

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts

Extend

```text
Removes timesTheMedian, formatTimesTheMedian and their text type, which only the popover table used (dead code otherwise); the rule, cut and axisTop stay
```

Lighthouse\.Frontend\/src\/services\/Api\/UsageDataService\.ts

Extend

```text
Rename the list entry OutlierPopoverOpened to OutlierListOpened (same position)
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs

Extend

```text
Rename member OutlierPopoverOpened = 27 to OutlierListOpened = 27 and its summary; number unchanged
```

docs\/settings\/usagedata\.md

Extend

```text
Row text: the list opens when the chip is clicked; drop the closes-because-the-chart-reloads clause
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierChip\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.outlierChip.test.tsx
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.outliers.test.tsx src/components/Common/Charts/CycleTimeScatterPlotChart.outlierList.test.tsx src/components/Common/Charts/CycleTimeScatterPlotChart.outlierViews.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~OutlierListOpenedEventNameTests
```

#### Supports

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierViews\.test\.tsx

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.mutation\.test\.tsx

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.mutation\.test\.ts

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.mutation\.test\.ts

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.test\.ts

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/outlierPlot\.mutation\.test\.ts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/UsageData\/OutlierListOpenedEventNameTests\.cs

Lighthouse\.EndToEndTests\/tests\/specs\/metrics\/OutlierChip\.spec\.ts

Lighthouse\.EndToEndTests\/tests\/models\/metrics\/CycleTimeScatterPlotChart\.ts

#### Public oracle

```text
Render CycleTimeScatterPlotChart (RTL, inside the WidgetShell header host) with a Team Zenith-like closed set (median 6 days, one 58-day item) and again with a line above the axis and with no line above it; read the chip, click it with the mouse and with the keyboard, close the dialog; render the frame loading; render a narrow viewport. Playwright walking skeleton on Team Zenith demo data: click the header chip
```

```text
The chip is amber outlined (warning colour on border, icon and text) loaded, loading and narrow; its accessible name is "1 outlier not drawn on the chart. Above the axis: 95% at {p95} days, {SLE} at {sle} days. Show the list." with the Above the axis sentence only when a line is above the axis, and the tooltip shows the same text; one click opens the Work Items dialog titled "{Work Items} with a {Cycle Time} more than 5 times the median" listing exactly the visible-type outliers (TZ-301 on demo data), with no popover, no role=presentation popover paper and no extra lines; usage event OutlierListOpened is reported once per open and never while disabled; on close focus returns to the chip; loading shows a disabled amber "Outliers" chip with no count; E2E shows the dialog row TZ-301 straight after the click
```

```text
A chip that is not warning-coloured at any width or while loading, a click that opens a popover or any step before the dialog, an accessible name without the Above the axis sentence when a line is above the axis or with it when none is, a dialog with drawn items or all items, the old event name OutlierPopoverOpened reported, an event on a disabled click or twice per open, or focus lost to the body, fails
```

##### Promises

P2

P4

#### Prefactoring

```text
L1: collapse the chip two callbacks (onOpened, onOpenInList) into one onOpen in the chart and chip before the popover is deleted, so the click opens the dialog and reports usage in one handler
```

```text
The dialog opened through the chip: same title, same items, same focus return on close
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Charts/CycleTimeScatterPlotChart.outlierList.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

OutlierChip

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx\:1

Extend

```text
Owns the chip view; the popover is deleted from it and the colour set to warning; no second chip component
```

OutlierPopoverContent

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx\:1

Replace

```text
Removed; its only consumer is the chip and its table is not wanted; the existing WorkItemsDialog already lists, sorts and exports the outliers
```

WorkItemsDialog

Lighthouse\.Frontend\/src\/components\/Common\/WorkItemsDialog\/WorkItemsDialog\.tsx\:1

Reuse

```text
Opened directly with the unchanged {title, items} state the chart already owns; no change to the dialog
```

outlierWording

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:1

Extend

```text
Single owner of all outlier words; the accessible name gains the Above the axis sentence by reusing its aboveTheAxis phrase builder, so the sentence cannot drift from the markers
```

outlierHeaderChip

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/outlierHeaderChip\.tsx\:1

Extend

```text
Existing adapter between the chart and the chip; shrinks, no new carrier
```

CycleTimeScatterPlotChart

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:22

Extend

```text
Owns the dialog state, usage reporting and focus return; one callback replaces two
```

UsageDataEventName

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs\:103

Extend

```text
Rename in place, number 27 kept; no release has carried the old name
```

buildOutlierRows

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/outlierPlot\.ts\:113

Replace

```text
Popover table rows only; removed, the file keeps its axis and marker helpers
```

formatTimesTheMedian

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts\:114

Replace

```text
Only the popover table used it; removed as dead code, the rule in the same file is reused unchanged
```

UsageDataService

Lighthouse\.Frontend\/src\/services\/Api\/UsageDataService\.ts\:52

Extend

```text
Existing event-name list; one entry renamed
```

outlier list was opened

docs\/settings\/usagedata\.md\:71

Extend

```text
Existing row for the event; wording follows the direct open
```

##### Impact

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.tsx\:1

Changed

```text
Popover and open state removed; amber; one callback
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierPopoverContent\.tsx\:1

Changed

```text
File deleted; its only importers are OutlierChip and outlierPlot.ts
```

adapter

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/outlierHeaderChip\.tsx\:1

Changed

```text
Smaller props; callers: the chart only
```

helper

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/outlierPlot\.ts\:1

Changed

```text
buildOutlierRows removed; reservedMarkerRows and line helpers unchanged
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:371

Changed

```text
Chip handoff and usage event name
```

pure\-module

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts\:1

Changed

```text
Accessible name builder changes; popover strings removed; help sentence and list title unchanged
```

pure\-module

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.ts\:114

Changed

```text
formatTimesTheMedian and timesTheMedian removed as dead code; findOutliers and axisTop unchanged
```

export\-list

Lighthouse\.Frontend\/src\/services\/Api\/UsageDataService\.ts\:52

Changed

```text
Entry renamed; the only reader is the chart
```

enum

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs\:103

Changed

```text
Member renamed; no other C# reference besides the event-name test
```

docs

docs\/settings\/usagedata\.md\:71

Changed

```text
Row wording
```

docs

docs\/product\/architecture\/adr\-282\-outlier\-is\-a\-per\-chart\-median\-multiple\.md\:17

UnchangedCompatible

```text
Rule unchanged; popover sentences are a docs follow-up for the finalize checklist, not a decision change
```

test\-support

Lighthouse\.EndToEndTests\/tests\/models\/metrics\/CycleTimeScatterPlotChart\.ts\:27

UnchangedCompatible

```text
outlierChip (button named /outlier/), openOutlierList (click) and outlierRow (row role) work when the click opens the dialog; the spec may assert the dialog, owned by the acceptance designer
```

existing\-test

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outliers\.test\.tsx\:1

Migrated

```text
Popover-dependent scenarios revised by the acceptance designer to the direct-dialog behaviour; others kept
```

existing\-test

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierList\.test\.tsx\:1

Migrated

```text
Open in a list step becomes the chip click; focus-return and help scenarios kept
```

existing\-test

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.outlierViews\.test\.tsx\:1

Migrated

```text
Popover-dependent assertions revised; view-state regressions kept
```

existing\-test

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/OutlierChip\.mutation\.test\.tsx\:1

Migrated

```text
Popover content and row mutation tests go; chip tests follow the new props
```

existing\-test

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/UsageData\/OutlierPopoverOpenedEventNameTests\.cs\:1

Migrated

```text
Renamed to the new member, class and file renamed to OutlierListOpenedEventNameTests; assertions kept
```

existing\-test

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.mutation\.test\.ts\:14

Migrated

```text
Entries for removed strings go; accessible name entries follow D16
```

existing\-test

Lighthouse\.Frontend\/src\/utils\/charts\/outliers\.mutation\.test\.ts\:22

Migrated

```text
formatTimesTheMedian cases go with the function, likewise in outliers.test.ts and outlierPlot.mutation.test.ts
```

##### Agreement

UsageDataEventName OutlierListOpened \= 27

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/UsageData\/UsageDataEventName\.cs\:103

Producer

Migrated

```text
Member renamed, number kept; nothing released under the old name
```

UsageDataEventName OutlierListOpened \= 27

Lighthouse\.Frontend\/src\/services\/Api\/UsageDataService\.ts\:52

Consumer

Migrated

```text
String name renamed to match; enums serialise as strings out
```

UsageDataEventName OutlierListOpened \= 27

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx\:388

Consumer

Migrated

```text
The chart reports the new name on opening the list
```

UsageDataEventName OutlierListOpened \= 27

docs\/settings\/usagedata\.md\:71

Consumer

Migrated

```text
Documented row follows the rename
```

##### Not applicable

architectural_tests

```text
Covered by the shared record: the existing fallow layer check governs the files; S4 only removes a component and adds no dependency
```

contract_tests

```text
Covered by the shared record: the usage event name is a same-commit enum pair, no independently released consumer
```

##### Boundaries

```text
CycleTimeScatterPlotChart React props rendered through React Testing Library inside the WidgetShell header host; click on the header chip (role button, name matches outlier)
```

###### Driven ports

```text
WorkItemsDialog props only
```

```text
usage-event callback prop (reportUsage) called with UsageDataEventName.OutlierListOpened
```

```text
unchanged: chart (shell) -> OutlierChip (view, props in, one onOpen callback out) and outlierWording (pure); chip imports no service, no dialog and no popover
```

###### Failures

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L4

```text
The chip accessible name equals the tooltip text and equals "{n} outlier(s) not drawn on the chart." + (" Above the axis: ..." only when a line is above the axis) + " Show the list.", with n = count of visible-type outliers
```

Invariant

Application

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts

###### Treatment

Finite

###### Cases

```text
one outlier, no line above
```

```text
three outliers, 95% above
```

```text
SLE above
```

```text
percentile and SLE above
```

```text
loading
```

```text
Finite closed cases cover count singular/plural and each line-above shape; no broad input domain
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C2

```text
Activating the enabled chip opens the Work Items dialog with exactly the visible-type outliers and reports OutlierListOpened once; a disabled chip does nothing
```

```text
A reader reaches no list, a wrong list, or usage data counts opens that did not happen
```

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/CycleTimeScatterPlotChart\.tsx

```text
chip click or keyboard activation on the header chip
```

OutlierChip onOpen callback built by CycleTimeScatterPlotChart

WorkItemsDialog and reportUsage

```text
dialog title and rows after the click; usage call count and name
```

```text
A future edit reintroducing a popover step or a second opener; guarded by the S4 oracle asserting no popover and one dialog
```

##### States

None.

##### Models

None.

##### Closure

```text
chip click opens the outlier list directly
```

Safety

Pass

DesignedNotBuilt

```text
The open flag and popover are removed, so no intermediate state exists; dialog items derive from the same visible-outlier value that gives the count; no checker run is claimed for the S4 claims, which are finite examples
```

###### Evidence

None.

```text
chip click opens the outlier list directly
```

Inhabitation

Pass

DesignedNotBuilt

```text
Team Zenith demo item TZ-301 -> findOutliers -> outlierHeaderChip -> OutlierChip onOpen -> WorkItemsDialog
```

###### Evidence

None.

```text
chip click opens the outlier list directly
```

PublicReachability

Pass

DesignedNotBuilt

```text
Input closed items -> chart partition -> header slot chip -> click -> dialog rows, through the chart public props and the WidgetShell header host; E2E reaches it through the page object
```

###### Evidence

None.

```text
chip click opens the outlier list directly
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: a synchronous click handler with no eventual-progress claim and no temporal property
```

###### Evidence

None.

##### Risks

###### Y5

Stressor

```text
Assumption: the chip click is the only route to the list; negated: a screen-reader or keyboard user cannot see the colour or tooltip
```

```text
The reader learns the count, the lines above the axis, and that activating lists them
```

```text
Tooltip not shown on keyboard focus-less touch
```

```text
Information only in colour or only in the removed popover
```

```text
The accessible name carries the full text; amber is not the only signal since the label and icon remain
```

Lighthouse\.Frontend\/src\/utils\/charts\/outlierWording\.ts

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
