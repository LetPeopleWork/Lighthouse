# Architecture brief: story\-6535\-arrivals\-in\-reports

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/Metrics\/ArrivalsReportMetric\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Reports\/IReportMetricSeries\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/TeamReportMetricSeries\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/PortfolioReportMetricSeries\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportMetricSeriesByOwner\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.tsx

Create

None.

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/reportPanelRegistry\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThenNowLineParts\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ReportPanels\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/MetricsAndNameStep\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/Reports/panels/ArrivalsPanel.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ArrivalsReportMetricTest
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Services\/Implementation\/Reports\/ArrivalsReportMetricTest\.cs

Lighthouse\.Frontend\/src\/tests\/ReportsTestKit\.ts

#### Public oracle

```text
Open the report (ReportView, and the print document) for a Team report whose Then started 1.10 and finished 0.80 and Now 0.90 and 1.00 per day, with Arrivals shown; also a gap of 0.004 and 0.006, and a report with no arrivals capture
```

```text
Panel reads 0.30 more started than finished per day (Then), 0.10 fewer started than finished per day (Now), change 0.40 per day marked better with no percent, header lists Arrivals under Improved; 0.004 reads as many started as finished per day and 0.006 reads 0.01 more; the old report reads Not captured for this report; the print document shows the same words
```

```text
A build that rounds after comparing, keeps a percent, signs a zero gap, rebuilds Then from live data, or omits the print panel fails a named row
```

##### Promises

P1

P2

P3

P4

P5

#### Prefactoring

None.

#### Records

##### Decisions

```text
OQ3 Clients: the wire gains one metric key arrivals whose sides carry the members total, perDay, throughputPerDay and gapPerDay (each a CapturedValue, doubles unrounded). lighthouse-clients holds no list of report metric keys or members (packages/client reportWording.ts and mcp-core reportTools.ts pass the raw answer through, per ADR-247), so no client code or release is needed. Only the lighthouse-reports skill reference (separate repo, prose) must learn the gap wording, the lower-is-better direction, and that Then is never rebuilt; the skill evals and drift test there own it. No changeset.
```

```text
OQ5 Wording: started and finished are fixed English words, not terminology tokens (Arrivals is a fixed name too: the Metrics tab uses the literal word and the seeder has no Arrivals key; a new key would need seeding for no reader gain). The tokens {Work Item}/{Work Items} (total line, as ThroughputPanel) and {Throughput} (line title) stay tokens. Rows: Total started N {Work Items}; Arrivals per day X.XX per day; {Throughput} per day X.XX per day; gap line. Gap line copy from the rounded gap g: g>0 -> g.toFixed(2) more started than finished per day; g<0 -> |g|.toFixed(2) fewer started than finished per day; g==0 -> as many started as finished per day. Number is never signed with a plus or minus on the Then and Now sides; the words carry the sign. Singular/plural: none needed, the sentence uses the unit per day, and the total line reuses the {Work Item}/{Work Items} choice of ThroughputPanel.
```

```text
Rounding: PER_DAY_DECIMALS = 2 (ThroughputPanel). The backend freezes gapPerDay = arrivalsPerDay - throughputPerDay from the unrounded per-day values of one pair of series; the frontend rounds each side with roundForDisplay(.,2), and M10 balanced iff the rounded gap is 0 (so 0.004 reads balanced, 0.006 reads 0.01 more). The change line is compareThenNow on the SHOWN gaps (lineComparisonsOf already rounds both sides before subtracting), so shown Then + shown change = shown Now, the ADR-220 rule; 0.30 and -0.10 give -0.40 better. Rejected: change from unrounded values, because the displayed numbers could then contradict the displayed change.
```

```text
Gap owner: the arithmetic arrivals minus throughput lives once, in the new backend ArrivalsReportMetric (cohesive owner, a WindowedReportMetric beside ThroughputReportMetric), so Then and Now use one computation and the clients receive the gap raw. The frontend ArrivalsPanel owns only presentation: rounding, the three sentences, the mark. Direction of good (lower) lives only in reportPanelRegistry (ADR-220). No domain decision sits in a React component beyond the wording of an already computed value.
```

```text
Old reports (M6, Epic 5878 D27, followed as is): Read leaves out members the report never froze, so an old report has no arrivals metric and ReportPanels draws NOTHING_CAPTURED; the panel shows dashes and the existing absence note Not captured for this report on both sides. The edit wizard already lists a metric whose key is missing from the captured keys as a disabled, unticked checkbox with the helper Not captured for this report, and ThenAndNowTemplate.Edit refuses MetricNotCaptured for it; no new code. An old report cannot show Arrivals at all (shown keys are only ever from the stored list), so the panel appears only if a user had ticked it, which is impossible; the not captured read stays as the defensive look. New reports capture arrivals for every owner, shown or not, like the other metrics.
```

```text
Header summary: the registry entry has one change line, gapPerDay, with changeLines [gapPerDay, label gap per day, decimals 2], so summariseChange needs no change: gap change below zero lists Arrivals under Improved, above zero under Got worse, shown change zero under Unchanged, and a dash on either side (not captured) puts it in no group, as for other metrics. The group name is the panel title Arrivals.
```

```text
Edge cases: nothing finished gives throughput total 0 and perDay 0.00 as a real value (the Throughput rule; no absentReason), so gap = arrivals per day. Nothing started gives arrivals 0 and 0.00 and gap = -throughput per day (fewer started), or zero (as many started as finished) if nothing finished. Window days come from the series length, as Throughput, so no division by zero for a window of at least one day. Absent members use the existing CapturedValue.Absent plus absenceTextOf. Windows inside the done-items cutoff: Then creation is already refused by ReportWindowPolicy.ThenRefusal; Now uses the same series. Portfolio arrivals are Features started: PortfolioReportMetricSeries reads IPortfolioMetricsService.GetArrivalsForPortfolio (the Metrics tab's own reader), Team reads ITeamMetricsService.GetStartedItemsForTeam (the Metrics tab's arrivals endpoint). One definition of arrival; BuildArrivalsInfoDto is not reused because it computes a previous-period comparison, a different shape.
```

```text
Throughput repeated: throughputPerDay is stored inside the arrivals capture from the same IReportMetricSeries.DailyThroughput the Throughput metric reads over the same window and day count, so both panels show the same number: Then is frozen in one CaptureThen call per metric at one instant over the same series and Now is computed from the same Metrics tab reader on each read. Stored twice by design (the panel must read alone and a report may hide Throughput); never disagree because both come from one reader and one window. The change mark of Throughput never depends on it.
```

```text
Loading and failure looks: no new fetch. The panel is a pure function of its props (ADR-220 item 5) inside ThenNowPanelFrame and renders from the one report read ReportView already makes, so ReportView loading/failure looks, the print page preparing/failed looks and WidgetShell are unchanged and reused. No separate loading or failure look exists for the panel. The panel also reads a dash with the reason in the Not captured and no-finished states.
```

```text
PDF: ReportPrintDocument renders ReportPanels, the same component as the screen, so Arrivals prints with the same values and words; no print-specific work. Pin it by rendering the print document through its existing test kit once with Arrivals shown.
```

```text
Metric order and name: the catalog order is cycle-time, throughput, wip, work-item-age, arrivals (Arrivals last, a new metric in a later version goes last). Backend catalog order follows DI registration: register ArrivalsReportMetric last. Name Arrivals is a fixed name through a new fixedName on the frontend METRIC_CATALOG entry read by a new metricNameOf(key,getTerm) used by the wizard checkbox, UnsupportedMetricPanel title and panelTitle; no terminology key.
```

```text
Default ticking (open, see question to maintainer): ThenAndNowDraft defaults to every offered metric ticked; Arrivals follows that rule unless the maintainer wants it off by default. DESIGN records the default-on rule because changing it would add a new per-metric default concept.
```

```text
Cross-layer algebra. Domain (Reports metric): state = per-window CapturedValue members {total, perDay, throughputPerDay, gapPerDay} with total integer and gapPerDay = perDay - throughputPerDay; operations CaptureThen/Read over a window; observation = the four values per side; equality = field-wise CapturedValue equality. Application/port: IReportMetricSeries gains DailyArrivals(owner, window) returning one count per day; outcome is values or ReportOwnerGoneException like the other series. Adapter: TeamReportMetricSeries/PortfolioReportMetricSeries map the Metrics services' RunChartData to counts through DailyCounts (the order and the day count are the window's). Presentation: ArrivalsPanel maps four values to rows plus one gap sentence; absent values map to dash and note. Infrastructure/recovery: none new; the Then payload is the existing JSON column captures, a new key in the existing JsonObject, no migration (EF migrations N/A); old payloads lack the key and read as not captured.
```

```text
Carriers and owners: decisions about the balance live in ArrivalsReportMetric (C#, Services/Implementation/Reports/Metrics); effects in the two series adapters; errors as ReportOwnerGoneException (existing); presentation decision in ArrivalsPanel.tsx plus a pure exported function gapSentenceOf(gap) in the panel module (testable with no render); direction in reportPanelRegistry. Per-process state: none.
```

```text
Checker mapping: no state machine, concurrency or liveness is introduced, so Quint (installed, 0.33.1) and TLC/Agda are not applicable; the claims are total pure arithmetic on two numbers and a sign-word mapping, falsified by native properties: fast-check (frontend package.json, fast-check 4.10.2) for the rounding and sign-word laws, and NUnit TestCase rows for the backend capture (no FsCheck in the backend manifest). Type strength: the C# CapturedValue record and the TS union of sentence kinds; invalid states (a plus-signed zero, '0.00 more') are excluded by a three-way closed sum returned by gapSentenceOf, but the TS wire type stays a nullable number as for other members. Property tests: see laws.
```

```text
Prefactoring: none needed on behavior; one small move before the feature, extracting the 'metric name or term key' choice behind metricNameOf in reportPanelRegistry is part of the feature itself, not a preserving move. No smell blocks the new metric: code-fact (code-fact query.callers-of) was not run; readings were by hand with ctx_shell/grep, which is noisy and could miss dynamic callers, so a consumer of ReportMetricSide keys outside the grepped Reports tree is unknown.
```

```text
UI sketch and visualization section 8 (DESIGN answers): one question (did the started-versus-finished balance fall); simplest form is the existing three-column panel, no chart, so axis, labelling, bar baseline and limit-count items are N/A with that reason; awkward data covered (nothing finished, nothing started, balanced, not captured, one-day window); colour: the existing mark plus word, never colour alone (LineChange draws the mark icon and the Improved/Got worse words); title Arrivals; info text (the i help) states: Arrivals are Work Items started; the gap is started minus finished per day; lower is better; no percent because the gap can cross zero; docs how-to-read passage goes in docs/reports/thenandnow.md at finalize. Phone, dark and enlarged: reuse ThenNowPanelFrame unchanged. The gap sentence wraps on its own line, as the AbsenceNotes do; the sketch is for DELIVER's start.
```

```text
Usage data is left to DEVOPS (OQ4); no event is designed. ADR/context map: ADR-270 and ARCHITECTURE.md section 4 need no change (a metric-catalog entry inside the Reports context); no new ADR; ADR-220's catalog gains an entry within its decision, and ADR-247's table of web presentation rules (the skill reference, a drift list) gains a row at DELIVER finalize.
```

##### Constraints

```text
Per-day values are shown to 2 decimals; totals to 0 decimals; no percent change on the gap line (M7); direction of good is lower and fixed in the frontend registry only; no new direction kind (M2).
```

```text
No stored bound beyond the existing ones: Then and Now windows are held to ReportWindowPolicy (minimum length, done-items cutoff); the design adds no input and widens nothing. The new members are doubles with unbounded sign for gapPerDay (it can be negative); that is a deliberate property, not a deviation.
```

```text
Public stimulus stays the report read (GET report by id through the application-service port) and the create report call; no endpoint, DTO type or enum ordinal changes; metric keys are strings on the wire (ADR-220 item 6).
```

```text
No scenario tags, no Epic/Story words in copy: copy uses {Work Items}, {Throughput}; the fixed words are started, finished, Arrivals.
```

```text
Test budgets are the defaults: per-test and property budgets and the CI suite ceiling (300 s) unchanged; the property uses a bounded example count with fast-check defaults; no derogation requested; no infrastructure suite.
```

##### Reuse

WindowedReportMetric

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/Metrics\/WindowedReportMetric\.cs\:15

Reuse

```text
Base class already freezes Then, recomputes Now and omits unfrozen members; ArrivalsReportMetric derives from it unchanged.
```

ArrivalsReportMetric

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/Metrics\/ThroughputReportMetric\.cs\:12

CreateNew

```text
No existing metric computes started-versus-finished; ThroughputReportMetric owns only finished and BaseMetricsService.BuildArrivalsInfoDto returns a previous-period comparison, a different shape. The new class holds the balance rule once, beside its sibling.
```

IReportMetricSeries

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Reports\/IReportMetricSeries\.cs\:5

Extend

```text
Add DailyArrivals(owner, window) next to DailyThroughput; the port is the single seam over the Metrics services.
```

TeamReportMetricSeries

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/TeamReportMetricSeries\.cs\:12

Extend

```text
Implement DailyArrivals through ITeamMetricsService.GetStartedItemsForTeam, the reader of the Team Metrics tab arrivals chart, so no second arrival definition exists.
```

PortfolioReportMetricSeries

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/PortfolioReportMetricSeries\.cs\:12

Extend

```text
Implement DailyArrivals through IPortfolioMetricsService.GetArrivalsForPortfolio (Features started), the reader the Portfolio Metrics tab uses.
```

ReportMetricSeriesByOwner

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportMetricSeriesByOwner\.cs\:7

Extend

```text
Delegate DailyArrivals to the series of the owner kind, as every other series member.
```

reportPanelRegistry

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/reportPanelRegistry\.ts\:47

Extend

```text
Add the arrivals entry (component, title, lower, changeLines) and the catalog entry; the satisfies Record<ReportMetricKey,...> forces the panel once the key exists.
```

ThenNowPanelFrame

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThenNowPanelFrame\.tsx\:1

Reuse

```text
The shared frame (title, Then, Now, Change columns) and PanelLine are reused by ArrivalsPanel unchanged.
```

ThroughputPanel

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThroughputPanel\.tsx\:1

Reuse

```text
Pattern, TOTAL/PER_DAY decimals, totalText and absence handling are the model; ArrivalsPanel does not copy the total-line rule where it can import PER_DAY_DECIMALS; totalText is exported for reuse if needed. Not a semantic duplicate: Throughput shows only finished and compares per day.
```

LineChanges

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThenNowLineParts\.tsx\:97

Extend

```text
LineChanges and changeText render a percent; the gap line needs none, so ChangeLine gains an optional showPercent (default true) honoured by changeText, keeping every other panel byte-identical.
```

compareThenNow

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts\:90

Extend

```text
Add the optional percent suppression on ChangeLine; compareThenNow and lineComparisonsOf are reused unchanged for the change on shown values.
```

summariseChange

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/changeSummary\.ts\:42

Reuse

```text
summariseChange already groups by each panel change lines; Arrivals needs no code here.
```

ReportMetricSide

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:14

Extend

```text
Add the members throughputPerDay and gapPerDay and the arrivals key to ReportMetricKey; total and perDay already exist.
```

ReportPanels

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ReportPanels\.tsx\:1

Extend

```text
Use metricNameOf for the unsupported-metric title; otherwise unchanged.
```

MetricsAndNameStep

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/MetricsAndNameStep\.tsx\:1

Extend

```text
Label through metricNameOf so Arrivals reads as a fixed name; checkbox, disabled and Not captured behaviour are reused for old reports.
```

ThenAndNowTemplate

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:1

Reuse

```text
Freeze, Edit and Read iterate the catalog; a registered metric is captured, edited and read with no code change; MetricNotCaptured refusal covers old reports.
```

ReportPrintDocument

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/print\/ReportPrintDocument\.tsx\:144

Reuse

```text
Renders ReportPanels; the PDF needs no print-specific code.
```

ArrivalsReportMetric

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/Metrics\/ArrivalsReportMetric\.cs\:1

CreateNew

```text
New file; see the ArrivalsReportMetric row: no existing owner of the started-versus-finished balance.
```

ThroughputReportMetric

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1338

Extend

```text
Composition root registers each metric; ArrivalsReportMetric is registered after WorkItemAgeReportMetric.
```

ArrivalsPanel

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.tsx\:1

CreateNew

```text
New file; ThroughputPanel shows only finished and compares one line, the Arrivals panel has four rows and a sentence line, so extending ThroughputPanel would add mode flags.
```

##### Impact

backend\-metric\-implementation

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/Metrics\/ArrivalsReportMetric\.cs\:1

Changed

```text
New file: the metric.
```

di\-registration

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1340

Changed

```text
Register ArrivalsReportMetric last so the catalog order follows.
```

port\-interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Interfaces\/Reports\/IReportMetricSeries\.cs\:5

Changed

```text
New member DailyArrivals; every implementer (the three series classes) and every test double that implements it changes; Moq mocks need a setup only where Arrivals is read.
```

test\-registration

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs\:148

Changed

```text
The port harness lists the metric classes explicitly and must register ArrivalsReportMetric; ATD owns this support.
```

wire\-payload

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ThenAndNowPayload\.cs\:39

UnchangedCompatible

```text
Captures is a map of JsonObject by key; a new key is data, old payloads lack it, no migration.
```

frontend\-model

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:14

Changed

```text
ReportMetricKey and ReportMetricSide gain members.
```

frontend\-registry

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/reportPanelRegistry\.ts\:47

Changed

```text
New entry plus metricNameOf.
```

frontend\-panel

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ArrivalsPanel\.tsx\:1

Changed

```text
New file: the panel and gapSentenceOf.
```

frontend\-comparison

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/thenNowComparison\.ts\:1

Changed

```text
Optional percent suppression.
```

frontend\-line\-parts

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ThenNowLineParts\.tsx\:1

Changed

```text
LineChange passes the suppression to changeText.
```

frontend\-wizard

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/wizard\/thenAndNow\/MetricsAndNameStep\.tsx\:1

Changed

```text
Label through metricNameOf.
```

frontend\-panels\-host

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/panels\/ReportPanels\.tsx\:1

Changed

```text
Title through metricNameOf.
```

frontend\-summary

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ThenAndNowSummary\.tsx\:1

UnchangedCompatible

```text
Summary reads the registry change lines; throughput closed count unchanged.
```

frontend\-print

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/print\/ReportPrintDocument\.tsx\:144

UnchangedCompatible

```text
Same ReportPanels.
```

frontend\-test\-kit

Lighthouse\.Frontend\/src\/tests\/ReportsTestKit\.ts\:1

Changed

```text
Report fixtures may add the new members; ATD owns this support.
```

clients\-repo

\/storage\/repos\/lighthouse\-clients\/packages\/client\/src\/reportWording\.ts\:1

UnchangedCompatible

```text
Raw pass-through; no key list; no code or release. The lighthouse-reports skill reference needs a prose update in that repo, owner maintainer.
```

docs

docs\/reports\/thenandnow\.md\:1

Changed

```text
New metric and how to read the gap, at finalize.
```

architecture\-docs

docs\/product\/architecture\/adr\-247\-reports\-are\-read\-through\-the\-clients\-as\-four\-read\-only\-tools\-that\-return\-the\-reports\-raw\-values\.md\:66

Changed

```text
Its table of web presentation rules and the drift-risk mitigation (b) call for the skill reference row for the gap wording; at finalize.
```

context\-map

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:1

UnchangedCompatible

```text
A metric-catalog entry inside Reports; no concept, context or edge changes.
```

series\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/TeamReportMetricSeries\.cs\:1

Changed

```text
Implements the new DailyArrivals member.
```

series\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/PortfolioReportMetricSeries\.cs\:1

Changed

```text
Implements the new DailyArrivals member.
```

series\-adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ReportMetricSeriesByOwner\.cs\:1

Changed

```text
Implements the new DailyArrivals member.
```

##### Agreement

report read payload\: metrics map by string key with members as CapturedValue

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Reports\/ThenAndNowTemplate\.cs\:84

Producer

Migrated

```text
Emits one more key arrivals and four members through the existing catalog loop.
```

report read payload\: metrics map by string key with members as CapturedValue

Lighthouse\.Frontend\/src\/models\/Reports\/Report\.ts\:14

Consumer

Migrated

```text
Adds the key and the two new members to the TS types and the registry.
```

report read payload\: metrics map by string key with members as CapturedValue

\/storage\/repos\/lighthouse\-clients\/packages\/client\/src\/reportWording\.ts\:1

Consumer

UnchangedCompatible

```text
Passes raw values through; an unknown key is shown as given.
```

report read payload\: metrics map by string key with members as CapturedValue

\/storage\/repos\/lighthouse\-clients\/skills\/lighthouse\-reports\:1

Consumer

Migrated

```text
Prose reference learns the gap wording and lower-is-better; separate repo, maintainer follow-up.
```

stored Then payload captures map

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ThenAndNowPayload\.cs\:39

Consumer

UnchangedCompatible

```text
Old rows lack arrivals and read as not captured; newer rows read by an older version are skipped with a warning (existing LogKeysNotInTheCatalog).
```

##### Not applicable

architectural_tests

```text
No new dependency or boundary: ArrivalsReportMetric sits in the same namespace under the existing ReportsModuleArchUnitTest rules, which cover members added later; the frontend registry's satisfies check and reportPanelRegistry.parity.test already govern the panel family.
```

contract_tests

```text
The only party that could change apart is lighthouse-clients, which keeps no list of report metric keys or members and passes the raw answer through (reportWording.ts), so there is no parsing contract the new key can break; the skill prose is checked by that repo's own skill drift tests.
```

prefactoring

```text
No behavior-preserving move is needed first: the registry, frame and comparison seams take a new entry as they are; the two small extensions (fixed metric name, optional percent suppression) are part of the feature and keep every existing panel byte-identical, guarded by reportPanelRegistry.parity.test.
```

##### Boundaries

```text
Report read through the application-service port (ReportQueries / IReportService) for the values, and the ReportView plus ReportPrintDocument components through ReportPanels for the words
```

###### Driven ports

```text
IReportMetricSeries.DailyArrivals and DailyThroughput over ITeamMetricsService / IPortfolioMetricsService
```

```text
ArrivalsReportMetric -> IReportMetricSeries -> the two series adapters -> Metrics services; frontend ArrivalsPanel -> thenNowComparison and ThenNowPanelFrame; nothing in the metrics services knows reports.
```

###### Failures

####### B1

```text
The owner of the report is gone when the series is read
```

Refusal

```text
ReportOwnerGoneException as for every other metric, translated by the existing report read to its existing refusal
```

####### B2

```text
A member of the arrivals capture is missing (old report) or absent
```

Indeterminate

```text
A dash on that line with Not captured for this report; the header summary lists the metric in no group
```

##### Observations

```text
Per side of the arrivals metric: total started, started per day, finished per day and gap per day (unrounded doubles, each with a sample size = the window days)
```

CapturedValue

```text
Two gaps equal after rounding to 2 decimals (0.004 and 0.000); a window with no start from one with a start balanced by a finish; which Work Items were started.
```

```text
Gap sentence per side as the panel shows it: more, fewer or as many started as finished per day, derived from the rounded gap
```

GapSentence

```text
The unrounded gap behind a rounded one; the Then window length.
```

##### Equality

gap sentence

```text
Two gaps are equal iff they round half away from zero to the same value at 2 decimals; the change is equal iff the difference of the rounded gaps is equal.
```

##### Constructors

gapSentenceOf

gapSentenceOf\(rounded\: number\)\: \{ kind\: \'more\' \| \'fewer\' \| \'balanced\'\; magnitude\: number \}

ArrivalsReportMetric\.ValuesOver

ValuesOver\(owner\: ReportOwner\, window\: ReportWindow\)\: Dictionary\<string\, CapturedValue\>

##### Laws

###### L1

```text
Balanced follows the shown value: for every finite gap g, gapSentenceOf(roundForDisplay(g,2)) is balanced iff roundForDisplay(g,2)==0, more iff it is above 0, fewer iff below 0, and the magnitude equals the absolute rounded value (0.004 balanced, 0.006 -> 0.01 more).
```

Invariant

Application

ArrivalsPanel

###### Treatment

Broad

###### Generator

```text
finite doubles in [-50,50] with a bias to values within 0.01 of zero and at half steps like 0.005
```

100 cases \(fast\-check default\)\, plus the fixed examples 0\.004\, 0\.005\, \-0\.005\, 0\.006

fast\-check seed printed on failure and replayable with the reported seed and path

###### Must include

```text
0
```

```text
0.004
```

```text
-0.004
```

```text
0.006
```

1

```text
fast-check native shrinking towards 0
```

```text
expected sentence kind written out from the rule on the rounded number by an inlined switch in the test, not by calling gapSentenceOf
```

```text
Dropping the rounding before the zero test, or using strict > 0 on the unrounded gap, makes 0.004 read more started than finished.
```

###### Evidence

None.

###### L2

```text
Shown Then + shown change = shown Now for the gap line and the change carries no percent: for generated Then and Now gaps, lineComparisonsOf on gapPerDay gives delta = round(round(now)-round(then)) and verdict better iff delta<0; percent text is absent.
```

Algebraic

Application

thenNowComparison

###### Treatment

Broad

###### Generator

```text
pairs of finite doubles in [-5,5] at 3 decimals
```

100 cases

fast\-check seed printed and replayable

###### Must include

```text
Then 0.30 and Now -0.10
```

```text
Then 0 and Now 0.35
```

1

```text
fast-check native
```

```text
expected delta computed as (Math.round(now*100)-Math.round(then*100))/100 in the test
```

```text
Computing the change from unrounded gaps lets a shown 0.30 and -0.10 sit beside a change other than -0.40; keeping a percent shows a Then was 0 text.
```

###### Evidence

None.

###### L3

```text
Capture: for any window of d>=1 days with s started and f finished, arrivals total is s, perDay is s/d, throughputPerDay is f/d, gapPerDay is (s-f)/d, all unrounded, each with sample size d; Then frozen equals the same values over the same series; a report that froze none reads no Then member and still computes Now.
```

Invariant

Domain

ArrivalsReportMetric

###### Treatment

Finite

###### Cases

```text
90 days started 99 finished 72 (1.10/0.80/0.30)
```

```text
30 days started 27 finished 30 (0.90/1.00/-0.10)
```

```text
30 days nothing finished (throughput 0 real)
```

```text
30 days nothing started
```

```text
1 day started 1 finished 2
```

```text
report with no frozen arrivals
```

```text
Finite examples through NUnit TestCase on the metric, as ThroughputReportMetricTest does; the rule is one division and one subtraction, so a broad generator adds nothing over the named edges.
```

###### Evidence

None.

##### Refuted

None.

##### Claims

###### C1

```text
The arrivals panel never shows 0.00 more or 0.00 fewer, never contradicts its change with its sides, and for old reports reads Not captured rather than a rebuilt Then
```

```text
A reader trusts a sign or a mark that disagrees with the shown numbers, or reads a rebuilt Then as frozen
```

ArrivalsPanel

```text
a report read from the port: any metric with members total, perDay, throughputPerDay, gapPerDay each a CapturedValue, a missing member, or no arrivals key
```

gapSentenceOf

ReportPanels

```text
the panel text and its change mark in ReportView and in the PDF print document
```

```text
a client or skill that restates the gap from raw values with its own rounding (separate repo, prose drift, owner maintainer); a hand-edited payload with a gap that disagrees with its perDay members (the stored value is shown as given).
```

##### States

Admitted

```text
gap below, at and above zero; a window with nothing started and/or nothing finished; Portfolio and Team owners
```

ArrivalsReportMetric

Excluded

```text
a plus-signed or minus-signed zero gap sentence; a percent change on the gap; a rebuilt Then from live data
```

ArrivalsPanel

Unknown

```text
a stored gapPerDay that disagrees with stored perDay minus throughputPerDay (only a hand-edited row); a client rendering the gap on its own
```

maintainer

##### Models

None.

##### Closure

```text
gap and sentence law
```

Safety

Pass

DesignedNotBuilt

```text
Exclusion by a three-way closed sum on the rounded gap; falsified by the broad property laws, not by a proof assistant; no executed formal checker (Quint 0.33.1 is installed but no state model exists to check).
```

###### Evidence

None.

```text
arrivals value
```

Inhabitation

Pass

DesignedNotBuilt

```text
Admitted input: a report window and owner -> DailyArrivals/DailyThroughput -> ArrivalsReportMetric.ValuesOver -> ThenAndNowTemplate.Read -> ReportPanels -> ArrivalsPanel; acyclic, every symbol exists or is a named new file.
```

###### Evidence

None.

```text
arrivals value
```

PublicReachability

Pass

DesignedNotBuilt

```text
Create through ThenAndNowTemplate.Freeze (catalog loop) and read through ThenAndNowTemplate.Read reach the ReportView and ReportPrintDocument, both via ReportPanels and the registry key.
```

###### Evidence

None.

```text
arrivals value
```

Liveness

Pass

DesignedNotBuilt

```text
Non-applicable: no process, retry, queue or eventual-progress claim exists; each read is one synchronous finite computation over a bounded window.
```

###### Evidence

None.

##### Risks

###### Y1

Stressor

```text
Assumption: Throughput and Arrivals series count the same days for a window (negated: they differ in length)
```

```text
Gap is started minus finished over the same days
```

```text
none
```

```text
A gap from windows of unequal length
```

```text
Both come from DailyCounts over the same ReportWindow so lengths are equal by construction; a service that returns a short run chart is the residue
```

ArrivalsReportMetric

###### Y2

Stressor

```text
Assumption: the Metrics tab arrival definition stays one definition (negated: Team and Portfolio started-item readers change)
```

```text
A report and the tab never disagree
```

```text
Later reports follow the new definition
```

```text
A second arrival rule inside Reports
```

```text
Frozen Then keeps the old definition by design
```

FlowMetricsModule

###### Y3

Bypass

```text
Skill or client restates the gap itself
```

```text
Values and words equal the web
```

```text
Raw values passed through
```

```text
A different rounding or a percent
```

```text
Prose drift across repos; the skill reference and its evals
```

maintainer

###### Y4

SinglePointOfFailure

```text
gapSentenceOf and compareThenNow are the single rounding sites
```

```text
Sentence and change agree
```

```text
none
```

```text
A second rounding site
```

```text
Covered by both broad laws
```

ArrivalsPanel

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
