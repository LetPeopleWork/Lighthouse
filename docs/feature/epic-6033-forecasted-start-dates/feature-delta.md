# Feature Delta — epic-6033-forecasted-start-dates

**Feature**: A Feature's forecast says when work on it is expected to *begin*, not only when it is
expected to end — read off the same simulated runs that already produce the completion date.
**ADO**: Epic #6033, "Sync Forecasted Start Dates with the Work Tracking System" (state Planned, tags
`Community`, `Productboard`).
**Origin**: Chris Graves (Focusrite), an existing paying customer, on the public idea board
(ideas.letpeople.work), posted as "Sync forecasted start dates to Jira". The motivating use is Jira
Plans: with a start and an end in fields, a Plan timeline populates itself from measured flow instead of
from manually estimated dates.
**Waves present**: DISCUSS.
**Density**: lean (`~/.nwave/global-config.json` → `documentation.density: lean`,
`expansion_prompt: ask-intelligent`).

> **The Epic description is superseded in its central mechanism.** It records the requester's initial
> thinking — derive a Feature's start from the *preceding* Feature's forecasted completion date — and
> flags that this needs a notion of sequence and does not answer what "preceding" means with zero or
> several predecessors. That derivation is rejected here, on the record, in D1. The simulation already
> knows the day it starts each Feature; the number does not need deriving, only recording.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `delivery-lead-rte` | **Primary.** Plans a Delivery across several Features and needs to tell a stakeholder when each one begins, not only when the last one ends. Reads the timeline. Never opens a setting. |
| `config-admin` | Maps the forecasted start percentile onto a work tracking system field, in the same write-back screen already used for completion percentiles. Touched only by slice 03. |
| `product-owner` | Reads the start column in the Feature table to answer "when will you get to mine?" — today the only honest answer is a completion date for everything above it in the order. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

- **`job-lead-say-when-a-feature-starts-not-only-when-it-ends`** (delivery-lead-rte) — When I am asked
  when a Feature further down the order will be picked up, I want an answer that comes from how work
  actually flows through this system rather than from me counting backwards off the one above it, so the
  answer moves on its own as throughput and order change.
- **`job-lead-see-a-delivery-as-a-timeline`** (delivery-lead-rte) — When I am planning a Delivery
  spanning several Features, I want to see them laid out against time with their real sequencing, so I
  can see which ones overlap and which ones wait, instead of reading a column of dates and holding the
  shape in my head.
- **`job-admin-let-the-plan-draw-itself`** (config-admin) — When our roadmap lives in Jira Plans and
  every bar on it is a date somebody typed, I want Lighthouse to fill both ends of the bar from the
  forecast, so the plan re-draws itself from measured flow instead of ageing the moment it is published.

All three are new. All three are appended to `docs/product/jobs.yaml` by this wave.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Established by reading the code on 2026-09-20, before any decision below was taken. Line references are
to the state of `main` at commit `a5ef65589`.

| # | Surface | What is actually there |
|---|---|---|
| S1 | `Services/Implementation/Forecast/SimulatedRun.cs:70-88` | `WorkOneDayOf`. The row being worked is named in `worked` on every delivered item; `completions.RecordThat(worked, day)` fires only when `CloseOneItemOf` returns true, i.e. on the day the row *finished*. **The day it started is the same `worked` variable, one branch earlier.** |
| S2 | `SimulatedRun.cs:90-95` | `HowManyFeaturesAtOnce` returns `Team.FeatureWIP`, floor 1. It bounds the *draw range* over the ready rows — it is not a tracked in-progress set. With Feature WIP 1 the draw range is 1, so `ready[0]` is always taken and the order is strict. |
| S3 | `TrialState.cs:118-135` | `RowsReadyToBeWorkedOnBy` is re-evaluated per delivered item, inside one day. A team that finishes a Feature with drawn capacity left starts the next one **the same day**. |
| S4 | `TrialState.cs:75-98` | `ReadyToBeWorkedOn`. A row learns its own team's finish the same day and another team's the following day — deliberate, and documented in place. So the one-day gap exists across teams and nowhere else. |
| S5 | `TrialCompletions.cs` | One `Dictionary<int,int>[]` per worker, written without a lock, summed once after all runs. A second parallel array is the entire storage change inside the run. |
| S6 | `ForecastService.cs:160-186` | `RecordTheDaysEachRowFinishedOn` merges the workers' shares and writes into `plan.RowAt(row).SimulationResults`. |
| S7 | `ForecastService.cs:228-250` | `UpdateFeatureForecasts` calls `feature.SetFeatureForecasts(...)`, one `WhenForecast` per `(Feature, Team)` row. |
| S8 | `Models/Feature.cs:207-215` | `SetFeatureForecasts` **clears** `Forecasts` and rewrites it on every refresh. Forecasts are current state, never history. |
| S9 | `Models/Feature.cs:53-58` | `Feature.Forecast => new AggregatedWhenForecast(Forecasts)` — aggregates **every** entry in the list, unconditionally. Putting start rows in that collection would silently corrupt the completion forecast, which is the most load-bearing number in the product. |
| S10 | `Models/Forecast/AggregatedWhenForecast.cs` | Combines per-team histograms through `JointCompletionDistribution.Combine` — a Feature is done when its **last** team is done. A start is the mirror: a Feature has started when its **first** team starts. |
| S11 | `Models/Forecast/ForecastBase.cs` `GetProbability` | Percentile read straight off the histogram; `KeyOrder` carries the direction, already parameterised. |
| S12 | `Models/WriteBack/WriteBackValueSource.cs` | Ordinal-persisted enum carrying an in-place comment that new members **must stay last**. Appending start sources is safe; inserting one is not. |
| S13 | `Services/Implementation/WriteBackTriggerService.cs:254-292` | `ResolveForecastValue`: `GetProbability(p)` then `ProjectWorkingDays` over effective blackout days then `yyyy-MM-dd`, and `null` when the Feature is Done. Exactly the shape a start resolver needs, blackout handling included. |
| S14 | `Models/WorkItemBase.cs:33,41` | `StateCategory` (`StateCategories.Doing`) and `DateTime? StartedDate`, both inherited by `Feature`. The observed start date already exists on the entity. |
| S15 | `Models/WorkItemBase.cs:37` | `string Order` — board order, maintained by manual sorting (Epic 5375). The timeline's row order, already there. |
| S16 | `API/DTO/FeatureDto.cs:29-31` | `feature.Forecast?.CreateForecastDtos(clock.Today, blackoutPeriods, 50, 70, 85, 95)`, guarded by `feature.CanBeForecast`. Dated percentiles with blackout projection already leave the API in this shape. |
| S17 | `Services/Implementation/Forecast/ForecastRunPlan.cs` `For` | Rows exist only for teams with measured throughput. A team with no history is left out of the run — so it has no start forecast either, for the same reason and by the same code. |
| S18 | `Models/DeliveryMetricSnapshot.cs` + `Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs` | The house pattern for "this number, over time": a dedicated entity plus a handler on a domain event. What a future forecast-start-over-time would reuse rather than invent. |
| S19 | `Lighthouse.Frontend/package.json:28-30` | `@mui/x-charts` 9.0.1, `@mui/x-data-grid` 9.13, `@mui/x-date-pickers` 9.0.0 — the MIT tier throughout. **MUI X Gantt is Premium-licence-only and is not owned.** |
| S20 | `pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryMetricsTab.tsx` | A Delivery already has tabs. The slot beside Features and Metrics that slice 04 needs is not new structure. |
| S21 | `ForecastService.cs` `RunMonteCarloSimulation` | `Parallel.For` over `limits.Trials` with one `OneWorkersShareOfTheRuns` per worker. Ten thousand runs already happen; nothing about the volume changes. |
| S22 | `API/DTO/FeatureDto.cs:70` | `List<WhenForecastDto> Forecasts` is filled from `feature.Forecast` — the **aggregate**. The per-team completion forecasts exist in `feature.Forecasts`, are used to build the aggregate, and then **never reach the client**. "Which team is the late one" has been unanswerable from the API since before this Epic. |
| S23 | `Lighthouse.Frontend/package.json:29`, `FeatureListDataGrid.tsx:8` | `@mui/x-data-grid`, the MIT package, imported directly. Detail panels, tree data and row grouping are all `DataGridPro`. Nothing in `src/` references a Pro feature — an expandable row is hand-built or not built. |
| S24 | `Models/Forecast/JointCompletionDistribution.cs` + `ComonotonicCompletionDistribution.cs` | The house already has a worked theory of combining per-team distributions, with two ADRs. `Combine` (ADR-110) multiplies CDFs across teams, assuming independence; `Min` (ADR-113) takes the elementwise minimum within a team, where rows share draws. Both operate on marginals *after* the run. |

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — The start day comes out of the run. It is not derived from the Feature before it

The Epic description proposes deriving a Feature's start from the preceding Feature's forecasted
completion date, and correctly notes that this needs a notion of sequence and breaks down with zero or
several predecessors, or with parallel teams.

Rejected, by the product owner, in the idea-board thread on 2026-09-19:

> I would NOT use the "completion date of the preceding Epic" — the way Lighthouse forecasts we actually
> know at which "day" we start with an epic. If we store this value, we can take those values and you
> can read off a percentile.

That is right, and S1 is why: `SimulatedRun.WorkOneDayOf` already names the row it is working on the day
it works it. The number exists ten thousand times over and is thrown away. Deriving it from a neighbour
would be reconstructing, less accurately, something the run already computed.

The derivation also fails exactly where it would matter most. It is only well-defined for a single linear
chain at Feature WIP 1. Recording the pull day is defined for every Feature WIP, every branching order,
every number of teams, and every dependency shape — because those are already what the run models.

### D2 — "Started" means the first day an item of that Feature is pulled

Not "the first day it was eligible to be pulled". With Feature WIP 3 and five ready rows, the draw ranges
over the top three (S2) — all three are eligible, and on any given day only some are drawn. A Feature
nobody has touched has not started.

So: the first day on which `worked` names a row of this Feature, whether or not that item finishes it.

### D3 — Start forecasts get their own identity. They do not join `Feature.Forecasts`

S9 is the reason: `Feature.Forecast` aggregates every entry of that collection unconditionally, so a
start row landing in it would be folded into the completion forecast, silently, on every Feature. That is
the one number in the product where a silent error is least acceptable.

A separate collection is also what makes D12 possible without a rewrite.

### D4 — Both grains are recorded: per Feature, and per Feature and Team

A Feature's start is stored twice over, and both fall out of the same pass:

- **Per `(Feature, Team)` row** — the day that team first pulls an item of this Feature. The same grain
  the completion forecast already uses (S7).
- **Per Feature** — the earliest across that Feature's rows, taken *inside* each simulated run.

The Feature-level number is **stored**, not computed on read, and that is the one place start and
completion differ in shape. `AggregatedWhenForecast` derives a Feature's completion from the per-team
histograms at read time through `JointCompletionDistribution.Combine` — the product of the teams' CDFs,
because a Feature is done when the last team is done (ADR-110).

The mirror is available and would work: a Feature has started when the first team starts, so
`1 - Π(1 - Fi(t))` gives it from the same marginals. It is not used. That formula and `Combine` both
assume the contributing teams are independent, and dependency-aware forecasting (Epic 5792) exists
precisely to model the case where they are not — one team's Feature waiting on another's is a
co-movement the marginals no longer carry. `Combine` already carries that approximation for completion;
there is no reason to inherit it somewhere new when the exact answer is cheaper.

Taking the earliest inside the run assumes nothing. It reads the actual joint realisation at the cost of
one comparison per pull. So it is recorded, not derived.

Completion is unaffected and stays exactly as it is.

### D5 — A Feature that has started reports the day it started, and the forecast is not consulted

Decided by the product owner, 2026-09-20. The forecast still runs, still records a start day for every
Feature with work remaining, and still stores it. What changes is what the API returns: when
`StateCategory` is `Doing`, the response carries `StartedDate` and says it is observed.

The simulation is not told which Features are in flight. No forecasting logic changes for this.

### D6 — The run forecasts the board as configured. Where reality disagrees, it is meant to look odd

`ForecastRunPlan` builds its rows in board order and the draw ranges over the top `FeatureWIP` ready rows
(S2). Every run therefore begins by working ranks 1, 2 and 3 at Feature WIP 3. If a team is really in
flight on ranks 1, 4 and 7, the run is modelling a different board — and rank 2, which nobody has
touched, is forecast to start today while the WIP it would need is spent on rank 7.

**This is intended, and it is not to be corrected.** Decided by the product owner, 2026-09-20: a team
working out of order, or an order nobody has updated, is the team's own signal about its process, and
Lighthouse's job is to show the board it was given rather than to guess at the one being worked. The
picture looking wrong is the picture being right about something. Seeding in-flight Features into the
WIP slots would launder exactly the discrepancy worth seeing — and would silently move completion
forecasts for every existing user, which have carried the same treatment since they existed.

D5 keeps it narrow. Ranks 1, 4 and 7 report their observed start dates, so what remains visible is the
not-started Feature forecast optimistically, which is the case that carries the signal.

**One thing follows that is not free.** A forecast that is meant to look odd, and is not documented as
such, arrives as a bug report. The docs have to say plainly what an out-of-order board does to the
picture, and the timeline is where someone will see it first. Named as a deliverable in slice 04 rather
than left to the release notes.

### D7 — At Feature WIP 1, the next Feature starts the *same* day, not the day after

Correcting the assumption the Epic was framed with ("the start date of feature x+1 is one day after
feature x completes"). S3: `RowsReadyToBeWorkedOnBy` is re-evaluated per delivered item within a day, so
a team that closes a Feature with drawn capacity left starts the next one immediately. The one-day gap
exists only when the next Feature belongs to a different team (S4), and there it is deliberate.

Consequence for slice 04: bars will abut, not gap. That is the model being honest, not a rendering fault,
and it is written down here so it is not later "fixed".

### D8 — Free: the forecast and the table. Premium: the timeline. Write-back inherits its existing gate

A start percentile is core forecasting, and core forecasting has never been gated. The write-back source
is configured in the mapping screen that is already Premium, so it inherits that gate without new code.
The timeline is the differentiated surface and is gated.

### D9 — Where the date is known it is shown as a date; where it is not, as percentiles

Table view. A not-started Feature shows P50/P70/P85/P95, the same four the completion forecast already
uses (S16). A started Feature shows one date, marked as observed. A done Feature likewise.

Four percentiles rather than one is deliberate: a single figure in a column is where a forecast starts
being read as a commitment, and the column costs nothing extra by carrying all four.

### D10 — One percentile selector drives both ends of every bar on the timeline

Default P70; P85 and P95 selectable. The selection moves the bar's left edge and its right edge together,
so a bar is always one internally consistent scenario rather than a P70 start welded to a P85 finish.

Row order comes from `WorkItemBase.Order` (S15) — the order the board already has, and the order the
simulation itself works in. Dependencies are drawn if they can be drawn cheaply; see slice 05.

### D11 — Build the timeline before buying one

MUI X Gantt would fit and is Premium-licence-only (S19); the product owner is open to buying it but wants
a build evaluated first. Slice 04 opens with a timeboxed evaluation whose output is a recommendation with
a number attached, not an opinion. What is being drawn is a horizontal bar per row against a date axis —
`@mui/x-charts` is already present and the existing charts establish the theming.

### D12 — Storage is shaped so that a snapshot can be added later. No snapshot is added now

Raised by the product owner as explicitly out of scope, with the request that the infrastructure not
foreclose it: seeing how a Feature's forecasted start date moved over the life of a Delivery.

S8 is the obstacle — `Forecasts` is cleared and rewritten every refresh, so there is nothing to look back
at. S18 is the house answer: a dedicated snapshot entity written by a handler on a domain event, as
blocked counts and delivery metrics already are.

The accommodation is D3 and D4 and nothing more: the start distribution is an addressable thing at
Feature grain, with its own identity and its own recompute moment. A later snapshotter has something to
point at. No snapshot table, no handler, no retention policy in this Epic.

### D13 — The flattened-bar risk is accepted, on the record, and is not to be re-litigated

The Epic description raises it as an open design risk: a single P85 start and a single P85 completion
render in Jira Plans as one solid bar, which reads as a commitment and hides the distribution it came
from. It is a fair objection and it has been answered — by the product owner and by the requester, in the
same thread:

> Of course you are right that then we have what looks like a fixed plan based on probabilities, but I
> trust that people using the tool can deal with this.
>
> — "ooo perfect! Yes, you said the forbidden word but that will be the result."

Recorded so the argument is not re-made in DESIGN. Two things follow from accepting it rather than
ignoring it: D9 keeps four percentiles in the table rather than one, and D10 makes the timeline's
percentile switchable rather than fixed. Neither removes the risk. Both keep the distribution one click
away from anyone who wants it.

The Epic description also notes the captured idea-board text was truncated mid-sentence and asks that the
board entry be re-read in case a mitigation was proposed there. The thread above is that re-read: the
continuation is the exchange quoted here, and no mitigation was proposed. The open question is closed.

### D14 — Our words, their values

"Feature" and "Features", per `TerminologySeeder.cs`. The Epic description says "Epic" throughout because
that is the requester's word and Jira's; it is not ours, and a Jira Plans user reading our docs sees
whatever they renamed Feature to. Where a literal work-tracking-system value is meant — a Jira field
named on a write-back mapping — it stays as written.

### D16 — The table says one thing. The timeline is where a Feature splits by team

Decided by the product owner, 2026-09-20, and the line is drawn at the surface rather than at the data.

**The table carries one forecast at four percentiles, exactly as the completion column does.** No
expander, no per-team column, no popover. A table row answers "when", and a second answer stacked inside
the first is what makes a table stop being readable.

**The timeline is where the split belongs.** A Feature with two or more contributing teams expands into
one sub-lane per team under its summary bar, each spanning that team's start to that team's completion at
the selected percentile (D10). Sub-lanes under a summary bar is a thing a Gantt does natively and well.

Two consequences worth naming.

**The data is served at both grains regardless** (D4, AC-1.9, AC-1.10). Where it surfaces is a UI
decision and this is it; the API carries the breakdown either way, so the question stays answerable and
the decision stays reversible without touching the backend.

**The sub-lanes are not start-only.** A lane spanning nothing but a start date is not a bar. Each lane
needs that team's completion too — and the per-team completion forecast already exists in the domain and
is discarded at the DTO boundary (S22): `FeatureDto.Forecasts` carries only the aggregate's four
percentiles. So serving it closes a gap that has been there for completion since before this Epic. Cheap,
because the data is already computed; named, because it changes an existing contract rather than adding
a new one.

S23 — that detail panels and row grouping are `DataGridPro`, which is not owned — is therefore not a
constraint on this Epic at all. It is recorded because it is the reason the question of a table expander
does not get reopened later on the assumption that the grid would just do it.

### D15 — Jira and Azure DevOps, matching the write-back sources that exist

The Epic asks whether this is Jira-only (the Plans use case) or Jira plus Azure DevOps as 5565 was. It is
whatever `WriteBackValueSource` already reaches — the resolver is connector-agnostic (S13) and gains
nothing from a restriction. No connector-specific work, and no connector excluded by hand.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS after splitting.** Assessed before journey work, per the early gate.

Oversized signals present at intake: the Epic spans backend simulation, persistence, API, table UI,
write-back configuration and a new visualisation — more than three modules, and a timeline of unknown
build cost. Two independent user outcomes ship separately (a date you can read; a plan that draws
itself).

Split into five slices below, each end-to-end. Slice 04 carries the only genuine unknown and opens with
a timeboxed evaluation. Slice 05 is severable — the product owner qualified dependency rendering with
"ideally", and it is the one piece that can be dropped without stranding anything.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — The forecast knows which day work starts

**Job**: `job-lead-say-when-a-feature-starts-not-only-when-it-ends`
**Persona**: delivery-lead-rte
**Slice**: 01

As someone planning a Delivery, I want the forecast to carry a start date for each Feature at the same
confidence levels it already carries a completion date, so that the question "when will you get to this
one?" has an answer that comes from measured flow.

#### Elevator Pitch

Before: the API returns four completion percentiles per Feature and nothing about when work on it begins.
After: run `GET /api/latest/portfolios/{portfolioId}` and each Feature carries `startForecasts` with
dated P50/P70/P85/P95, or an observed start date when it has already begun.
Decision enabled: whether a Feature further down the order can be promised to a stakeholder this quarter
at all, before anyone opens a timeline.

#### Acceptance Criteria

- **AC-1.1** — A Feature with remaining work and no observed start carries four dated start percentiles
  (50, 70, 85, 95) in the portfolio response, projected over working days and effective blackout days by
  the same path the completion percentiles use.
- **AC-1.2** — With Feature WIP 1 and two Features on one team with no dependency between them, the
  second Feature's P85 start equals the first Feature's P85 completion — the same day, not the day after
  (D7).
- **AC-1.3** — With Feature WIP 3 and five Features on one team, the top three carry a P85 start of day 1
  and the fourth and fifth carry later ones. Raising Feature WIP to 5 moves all five to day 1.
- **AC-1.4** — A Feature worked by two teams reports a Feature-level start taken as the earliest of its
  rows within each simulated run (D4). A constructed case with a dependency between the two teams — where
  the in-trial answer and `1 - Π(1 - Fi(t))` over the marginals differ — asserts that the stored value is
  the in-trial one.
- **AC-1.9** — The same Feature also reports one start per contributing team, at the same four
  percentiles (D4). For a single-team Feature the per-team value and the Feature-level value are the same
  number.
- **AC-1.10** — The portfolio response carries the per-team **completion** forecasts alongside the
  per-team starts (D16, S22). The existing aggregate `Forecasts` list is unchanged in shape and content,
  so nothing reading it today sees a difference.
- **AC-1.5** — `Feature.Forecast`, the completion percentiles and every existing forecast assertion are
  unchanged before and after this slice. The start distribution is not in `Feature.Forecasts` (D3).
- **AC-1.6** — A Feature whose `StateCategory` is `Doing` reports its `StartedDate`, marked as observed
  rather than forecast, and reports no start percentiles (D5).
- **AC-1.7** — A Feature on a team with no measured throughput carries no start forecast, and appears in
  `TeamsWithoutForecast` exactly as it does today (S17).
- **AC-1.8** — A ten-thousand-run forecast over a portfolio of fifty Features completes within 110% of
  the wall-clock time the same forecast takes on `main`, measured on the dev instance.

---

### US-02 — The Feature table says when each one starts

**Job**: `job-lead-say-when-a-feature-starts-not-only-when-it-ends`
**Persona**: product-owner (secondary: delivery-lead-rte)
**Slice**: 02

As a product owner looking at a portfolio's Features, I want a start column beside the completion
forecast, so that I can see where my Feature sits in the queue without reading the completion dates of
everything above it.

#### Elevator Pitch

Before: a Feature table row shows when a Feature will be finished and nothing about when it will be
picked up.
After: open a Portfolio and the Feature table shows a Forecasted Start column reading "~14 Oct (85%)" for
work not yet begun and "3 Oct" for work in flight.
Decision enabled: whether to re-order the board — seeing a Feature start eight weeks out is what prompts
moving it, and the completion date alone never said so.

#### Acceptance Criteria

- **AC-2.1** — The Feature table carries a Forecasted Start column, following the completion column's
  existing percentile presentation and the same terminology entries.
- **AC-2.2** — A started Feature shows a single date, visibly distinguished as observed rather than
  forecast, with no percentile attached.
- **AC-2.3** — A Feature with no start forecast (no throughput, or done) shows the same empty state the
  completion column already uses for that Feature. No new empty state is invented.
- **AC-2.4** — The column appears without a premium licence (D8), and a test asserts it with the licence
  absent.
- **AC-2.5** — Sorting and the existing column set are unchanged for anyone who does not look at the new
  column.

---

### US-03 — The work tracking system receives the forecasted start date

**Job**: `job-admin-let-the-plan-draw-itself`
**Persona**: config-admin
**Slice**: 03

As an administrator whose roadmap lives in Jira Plans, I want to map a forecasted start percentile onto a
field the same way I already map a completion percentile, so that a Plan bar gets both of its ends from
measured flow.

#### Elevator Pitch

Before: the write-back mapping screen offers four forecast percentiles, all of them completion dates.
After: open Settings, Work Tracking Systems, Write-Back, and the value list offers Forecasted Start
50/70/85/95 alongside the completion ones; pick one, pick a field, and the next refresh writes the date.
Decision enabled: whether the roadmap in Jira Plans can stop being maintained by hand — which is the
customer's actual request, and it is answered the moment both ends of the bar arrive on their own.

#### Acceptance Criteria

- **AC-3.1** — Four new `WriteBackValueSource` members for the start percentiles, appended after
  `SleRisk` and therefore last, so no stored mapping re-points (S12). A test asserts the ordinal of every
  pre-existing member.
- **AC-3.2** — Selecting a start source and a date field writes the forecasted start date to the work
  tracking system on the next write-back round, in the same `yyyy-MM-dd` default and honouring a custom
  `DateFormat` exactly as the completion sources do.
- **AC-3.3** — A Feature that has started writes its observed start date, not a forecast (D5), so the
  Plan bar begins where work began.
- **AC-3.4** — A Feature that is Done writes nothing for a start source, matching `ResolveForecastValue`'s
  existing behaviour for completion (S13).
- **AC-3.5** — A Feature with no start forecast writes nothing rather than writing an empty or epoch date.
- **AC-3.6** — The sources appear only under a premium licence, by the gate the mapping screen already
  has. No new gate is written (D8).

---

### US-04 — A Delivery shows its Features as a timeline

**Job**: `job-lead-see-a-delivery-as-a-timeline`
**Persona**: delivery-lead-rte
**Slice**: 04

As someone planning a Delivery, I want its Features laid out against a date axis with a percentile I can
change, so that I can see the shape of the plan — what overlaps, what waits — instead of assembling it in
my head from a column of dates.

#### Elevator Pitch

Before: a Delivery shows its Features as a grid and its metrics as charts. The sequencing between them is
in the reader's head.
After: open a Delivery, choose the Timeline tab, and see one bar per Feature in board order spanning
forecasted start to forecasted completion, with a P70/P85/P95 selector that moves both ends together.
Decision enabled: whether the Delivery holds together as planned — a bar sliding past the target date, or
three bars stacked where the team can only carry two, is visible at a glance and is not visible in a
table at all.

#### Acceptance Criteria

- **AC-4.1** — A Delivery carries a Timeline tab beside Features and Metrics (S20), rendering one bar per
  Feature in `WorkItemBase.Order` (S15).
- **AC-4.2** — A bar spans that Feature's start at the selected percentile to its completion at the same
  percentile. Changing the selector moves both ends (D10).
- **AC-4.3** — The selector defaults to P70 and offers P85 and P95.
- **AC-4.4** — A started Feature's bar begins at its observed start date (D5) and ends at its forecasted
  completion at the selected percentile.
- **AC-4.5** — A Feature with no forecast is listed with no bar and a stated reason, rather than omitted.
  A Delivery where none can be forecast says so instead of rendering an empty axis.
- **AC-4.6** — The Delivery's target date, where it has one, is marked on the axis.
- **AC-4.7** — The tab is premium-gated, using the notice the Delivery surface already uses (D8).
- **AC-4.8** — Legible in light and dark themes, and at the narrowest width the Delivery view supports.
- **AC-4.9** — The docs say plainly what an out-of-order board does to the picture (D6): a Feature nobody
  has started, drawn as starting now while work sits lower in the order, is the timeline reporting the
  board rather than misreading it. Without this the intended oddness arrives as a bug report.

---

### US-06 — A multi-team Feature splits into sub-lanes on the timeline

**Job**: `job-lead-say-when-a-feature-starts-not-only-when-it-ends`
**Persona**: delivery-lead-rte
**Slice**: 06 (severable, timeline only)

As a delivery lead looking at a Feature two or three teams contribute to, I want to open it on the
timeline and see a lane per team, so that when the summary bar looks wrong I can see which team it is.

#### Elevator Pitch

Before: a multi-team Feature is one bar, and which team drives either end of it is unanswerable from the
UI and from the API alike.
After: click the expander on a multi-team Feature's timeline row and see one lane per team beneath its
summary bar — Platform running Oct to early Nov, Mobile picking up in Nov.
Decision enabled: which team to talk to. A Feature running late because one team has not got to it is a
different conversation from one running late because both are saturated, and today they look identical.

#### Acceptance Criteria

- **AC-6.1** — A Feature with one contributing team has no expander on the timeline. Its row is unchanged.
- **AC-6.2** — A Feature with two or more contributing teams expands to one sub-lane per team, each
  spanning that team's start to that team's completion at the selected percentile. Changing the
  percentile moves the sub-lanes with the summary bar.
- **AC-6.3** — The summary bar is unchanged by expanding, and stays the stored Feature-level value (D4)
  rather than the earliest of the sub-lanes. A dependent pair where the two differ asserts it.
- **AC-6.4** — A team contributing to the Feature but carrying no forecast gets a named lane with its
  reason, not no lane — otherwise the expansion silently disagrees with `TeamsWithoutForecast`.
- **AC-6.5** — Removing this slice entirely leaves US-04 whole. **The Feature table is untouched by this
  story**: one forecast, four percentiles, no expander (D16).

---

### US-05 — The timeline shows what waits on what

**Job**: `job-lead-see-a-delivery-as-a-timeline`
**Persona**: delivery-lead-rte
**Slice**: 05 (severable)

As someone reading the timeline, I want to see the dependencies between Features drawn on it, so that a
bar sitting late reads as "it is waiting for that one" rather than as an unexplained gap.

#### Elevator Pitch

Before: the timeline shows when each Feature runs, and a Feature that starts late looks arbitrary.
After: on the Delivery's Timeline tab, a Feature that waits on another is connected to it, so a late bar
explains itself.
Decision enabled: which dependency to attack first — the one holding up the most downstream work is
visible as the bar with the most lines leaving it, and nothing in the product shows that today.

#### Acceptance Criteria

- **AC-5.1** — A Feature with a recorded dependency on another Feature in the same Delivery is connected
  to it on the timeline.
- **AC-5.2** — A dependency on a Feature outside this Delivery is indicated on the waiting bar without
  drawing a bar for the absent Feature.
- **AC-5.3** — The connections stay legible at the density a real Delivery reaches; above that density
  they degrade to a per-bar indicator rather than a thicket.
- **AC-5.4** — Removing this slice entirely leaves US-04 whole.

---

## Wave: DISCUSS / [REF] Story Map and Slices

**Backbone**: record the start day, store it, read it, write it out, draw it.

| Slice | Story | Ships | Estimate |
|---|---|---|---|
| 01 | US-01 | Start day recorded in the run at both grains, stored, served by the portfolio API with the observed-date override and the per-team breakdown | ~7h |
| 02 | US-02 | Forecasted Start column in the Feature table | ~3h |
| 03 | US-03 | Four write-back sources, resolver, premium gate by inheritance | ~4h |
| 04 | US-04 | Timeline tab with percentile selector (opens with a build-vs-buy evaluation) | ~6h + 2h evaluation |
| 05 | US-05 | Dependencies drawn on the timeline — **severable** | ~4h |
| 06 | US-06 | Per-team sub-lanes on the timeline — **severable**, and depends on 04 | ~4h |

Slices 05 and 06 are both severable and independent of each other, and both sit on slice 04. Either can
be dropped, or both, and what remains still ships. If slice 04's evaluation goes the other way, both are
re-estimated against whatever it recommends.

**Walking skeleton**: slice 01. It crosses every layer the Epic touches except the two UI surfaces — the
simulation, the storage decision, the aggregation grain and the API contract — and it is where every
assumption that could be wrong actually lives.

### Carpaccio taste tests

- **Four or more new components?** Slice 01 adds one recording array, one storage shape and one DTO
  member. Slice 04 is the largest and is one tab and one chart. Pass.
- **Every slice depending on a new abstraction?** Slices 02-05 all depend on slice 01's start
  distribution, which is why it ships first and alone. Pass.
- **Does any slice disprove a pre-commitment?** Slice 01 disproves D4 and D6's containment if the start
  forecast turns out to be systematically optimistic, and disproves the "minimal" premise if the storage
  cost is not small. Slice 04 disproves D11. Pass.
- **Synthetic data only?** Slices 01, 02 and 04 are demonstrated on the dev instance against real history.
  Slice 03 writes to a real Jira instance. Pass.
- **Two slices identical but for scale?** 04 and 05 are both the timeline, but 05 draws a different thing
  from different data and is severable. Not merged, deliberately. Pass.

### Prioritisation

1. **Slice 01 first — highest learning leverage.** Everything else is built on the number it produces. If
   the start distribution is wrong or the aggregation grain is wrong, it is wrong once here rather than
   in four places. It also carries the performance question, and it is the slice where the "it really
   should be minimal" premise either holds or does not.
2. **Slice 02 second — cheapest dogfood.** One column, and it is what makes slice 01's number readable by
   someone who is not reading JSON. Wrong start dates are far more likely to be spotted in a column
   beside the completion dates than in an API response.
3. **Slice 03 third — it is the customer's literal request.** Deliberately not first: writing a wrong date
   into someone's Jira instance is the most expensive way to discover that slice 01 was wrong, because
   the wrong number then lives in a system we do not control and a Plan gets published off it.
4. **Slice 04 fourth — the largest and the only one with an unknown.** Its evaluation runs before its
   code, and it is the first slice that could return a different answer than planned.
5. **Slice 05 last, and severable.** Qualified as "ideally" by the product owner.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Seeding in-flight Features into the simulation's WIP slots at day 0.** Not deferred — **declined**,
  per D6. The run forecasts the board it was given, and a board that disagrees with what the team is
  actually working is a signal the picture is supposed to carry. No follow-up item.
- **Snapshotting the forecasted start date over time.** D12. Explicitly deferred by the product owner;
  this Epic only avoids foreclosing it.
- **Any change to how completion is forecast, aggregated or displayed.** AC-1.5 asserts this.
- **Reading a start date back from the work tracking system.** Write-back is one-directional here, as it
  is for completion.
- **A timeline anywhere but on a Delivery.** Not on a Portfolio, not on a Team.
- **Purchasing a MUI X Premium licence.** Slice 04's evaluation may recommend it; the decision is the
  product owner's and is not taken here.
- **Folding this into Epic 5792 Dependency-Aware Forecasting.** The Epic asks whether it belongs there.
  It does not: 5792 shipped, its substrate (`IWhatTheForecastWaitsFor`, `ForecastRunPlan.MustFinishFirst`)
  is what makes start dates meaningful under dependencies, and this Epic consumes it rather than
  extending it. Recorded so the question is not re-opened.

---

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

**Strategy B — extend an existing end-to-end path.** Brownfield. Every layer this Epic touches already
carries the completion forecast from `SimulatedRun` to `FeatureDto` to the Jira field. Slice 01 runs a
second value down the same path rather than building a path.

No configurable or env-switching strategy is used, so the WS-strategy-D expansion trigger does not fire.

---

## Wave: DISCUSS / [REF] Driving Ports

| Port | Surface | Slice |
|---|---|---|
| HTTP | `GET /api/latest/portfolios/{portfolioId:int}` — Features carry start percentiles or an observed start date | 01 |
| UI | Portfolio, Feature table, Forecasted Start column | 02 |
| UI | Settings, Work Tracking Systems, Write-Back, value source list | 03 |
| Outbound | Write-back round to a Jira or Azure DevOps date field | 03 |
| UI | Portfolio, Delivery, Timeline tab | 04, 05 |

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Forecast wall-clock cost of carrying start dates | at most 110% of `main` | Ten thousand runs over a fifty-Feature portfolio on the dev instance, before and after slice 01 |
| Start percentiles present where a completion percentile is | 100% of Features that carry a completion forecast | Assertion over the dev instance's portfolio response |
| Start dates that are observed rather than forecast, for in-flight work | 100% of Features in a Doing state | AC-1.6, asserted |
| Completion forecast drift introduced by this Epic | 0 | AC-1.5 — existing forecast assertions unchanged |
| Write-back mappings using a start source, 60 days after release | at least 1 (Focusrite) | Usage data, if consent is given; otherwise the requester is asked directly |
| Mutation kill rate, both stacks | at least 80% | Stryker.NET and StrykerJS, per feature |

---

## Wave: DISCUSS / [REF] Pre-requisites

| # | Pre-requisite | State |
|---|---|---|
| P1 | The run names the row it works, per delivered item | **Confirmed** — `SimulatedRun.cs:70-88` (S1) |
| P2 | Feature WIP already bounds which Features a team works at once | **Confirmed** — `SimulatedRun.cs:90-95` (S2) |
| P3 | Dependencies already gate readiness in the run | **Confirmed** — `TrialState.ReadyToBeWorkedOn` (S4), shipped by Epic 5792 |
| P4 | An observed start date exists on a Feature | **Confirmed** — `WorkItemBase.StartedDate` and `StateCategory` (S14) |
| P5 | Board order exists and is maintained | **Confirmed** — `WorkItemBase.Order` (S15), Epic 5375 |
| P6 | The write-back value source enum can be extended safely | **Confirmed** — append-only, documented in place (S12) |
| P7 | A Delivery has a tab slot | **Confirmed** — `DeliveryMetricsTab.tsx` (S20) |
| P8 | A timeline can be built from what is installed | **Unverified** — slice 04's evaluation (D11, S19) |

P8 is the only open one, it is confined to slice 04, and slice 04 opens by closing it.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Evidence |
|---|---|---|
| 1 | Business value articulated | Jira Plans that draw themselves from measured flow; requested by a paying customer and seconded in-thread |
| 2 | User stories with job traceability | US-01 to US-05, each carrying a `job_id` appended to `docs/product/jobs.yaml` |
| 3 | Acceptance criteria testable | 27 ACs across five stories, each asserting an observable output |
| 4 | Dependencies identified | P1-P8; only P8 open, and confined to slice 04 |
| 5 | Sized to fit a slice | Five slices, 3-8h each, one severable |
| 6 | Technical approach known | D1-D4 name the mechanism; the surface inventory gives every line it touches |
| 7 | Risks named | D6 (optimism residual), D13 (accepted flattening), P8 (build vs buy) |
| 8 | Out of scope explicit | Seven items, two carrying owed follow-ups |
| 9 | Terminology settled | D14 |

**DoR: PASS.** One qualification, stated rather than absorbed: item 4 rests on P8, which slice 04 closes
rather than assumes. Slices 01-03 do not depend on it.

---

## Wave: DISCUSS / [REF] Definition of Done

1. `dotnet build` clean, zero warnings.
2. `dotnet test` green with the live-connector categories excluded.
3. `pnpm test` green; `pnpm build` clean with zero warnings; Biome clean.
4. Playwright specs run locally before commit, through page objects, against demo data.
5. SonarQube Cloud introduces no new issues of any severity.
6. Stryker at or above 80% on both stacks, acceptance suite excluded.
7. EF migrations generated by `CreateMigration`, expand-only.
8. Docs and per-feature screenshots updated at feature finalization, in the configurable terminology.
9. ADO Epic #6033 and its child Stories transitioned; Epic stops at Resolved, never Closed.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key decisions

- **[D1]** The start day is recorded from the simulated run, not derived from the preceding Feature's
  completion date — reversing the Epic description's proposal, on the product owner's instruction.
- **[D2]** "Started" is the first day an item of that Feature is pulled, not the first day it is eligible.
- **[D3]** Start forecasts get their own identity rather than joining `Feature.Forecasts`, which would
  silently corrupt the completion forecast.
- **[D4]** Both grains are stored — per Feature and per Feature-and-Team. The Feature-level value is
  taken inside each trial rather than derived from the marginals, because deriving it would assume the
  teams are independent and Epic 5792 exists to model when they are not.
- **[D5]** A started Feature reports its observed start date at read time; the simulation is unchanged.
- **[D6]** The run forecasts the board as configured. Where the team is working a different order, the
  picture is meant to look odd — declined as a fix, documented as a signal.
- **[D16]** The table carries one forecast at four percentiles, like completion, and gains no expander.
  A Feature splits by team on the timeline only, as sub-lanes — which need per-team completion as well as
  per-team start, and the API discards per-team completion today.
- **[D7]** At Feature WIP 1 the next Feature starts the same day, not the day after.
- **[D8]** Forecast and table free; timeline premium; write-back inherits the mapping screen's gate.
- **[D10]** One percentile selector moves both ends of every bar.
- **[D11]** Build the timeline before buying one; evaluation is timeboxed and produces a number.
- **[D12]** Storage shaped for a future snapshot; no snapshot built.
- **[D13]** The flattened-bar risk is accepted on the record and closes the Epic's open design risk.

### Requirements summary

- Primary jobs: say when a Feature starts, not only when it ends; see a Delivery as a timeline; let the
  Jira Plan draw itself from measured flow.
- Walking skeleton scope: slice 01 — record, store and serve the start day.
- Feature type: cross-cutting (simulation, persistence, API, two UI surfaces, outbound write-back).

### Constraints established

- The completion forecast must be unchanged before and after (AC-1.5).
- Ten thousand runs must not cost meaningfully more (AC-1.8).
- `WriteBackValueSource` is append-only; ordinals are persisted.
- MUI X Gantt is not owned and is Premium-licence-only.
- Start forecasts must not enter `Feature.Forecasts`.
- The Feature table shows one forecast at four percentiles. No per-team surface in the table at all.
- The existing aggregate `Forecasts` list on `FeatureDto` keeps its shape and content exactly.

### Upstream changes

No DISCOVER or DIVERGE artifacts exist for this Epic; the ADO description carries the discovery evidence
(named customer, public idea board, seconded in-thread). Two of its statements are changed here:

1. **Original (Epic #6033 description):** "For sequential work, an Epic's forecasted start date could be
   derived from the forecasted completion date of the *preceding* Epic." **Changed** by D1, on the
   product owner's instruction in the idea-board thread of 2026-09-19. The run already computes the
   number.
2. **Original (Epic #6033 description):** "The idea-board entry continues past this point... Re-read the
   board entry before DISCUSS — the requester may have proposed a mitigation." **Closed** by D13: the
   continuation was supplied on 2026-09-20 and contains no mitigation. The risk is accepted.

A third statement in the framing that reached this wave — "the start date of feature x+1 is one day after
feature x completes" — is corrected by D7 against `TrialState.cs:118-135`.

---

## Wave: DISCUSS / Tier-2 Expansion Menu

Density is `lean` with `expansion_prompt: ask-intelligent`. Triggers evaluated against the artifacts
above:

| Trigger | Fired | Why |
|---|---|---|
| AC ambiguity across 2 or more stories | No | Each AC names an observable output |
| Cross-context complexity (3 or more contexts or technologies) | **Yes** | Forecasting, persistence, write-back, React UI, charting |
| Multi-stakeholder (3 or more personas) | **Yes** | delivery-lead-rte, config-admin, product-owner |
| Compliance or regulatory | No | No regulated data |
| WS strategy = D (configurable) | No | Strategy B |

Two triggers fired, and both were examined rather than offered on the trigger alone.

**`persona-narrative` — declined.** The trigger fires on persona count, not on whether the personas are
described anywhere. All three already exist in SSOT (`docs/product/personas/`, 274 lines between them)
and none of them is new to this Epic. Rendering extended profiles here would restate the SSOT in a
feature workspace that gets archived, which is how two descriptions of one persona start to disagree.

**`alternatives-considered` — declined as written, one piece taken.** The rationale the expansion would
render is already inline: D1 records what the derivation was and why it went, D4 names the formula it
rejects and the assumption that kills it, D16 names the surface it chose over the other. Rendering it
again in a separate section is duplication.

The one thing genuinely missing is a candidate list for P8 — slice 04's build-versus-buy evaluation
starts from `@mui/x-charts` and nothing else, and two hours is not long to find out what else exists.
That belongs in the slice brief where the spike is run, not in a Tier-2 section here, and it is recorded
there instead.

No expansion rendered. Telemetry: one `choice = "skip"` per triggered id.
