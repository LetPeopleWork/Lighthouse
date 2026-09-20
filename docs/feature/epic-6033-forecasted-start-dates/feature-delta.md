# Feature Delta — epic-6033-forecasted-start-dates

**Feature**: A Feature's forecast says when work on it is expected to *begin*, not only when it is
expected to end — read off the same simulated runs that already produce the completion date.
**ADO**: Epic #6033, "Sync Forecasted Start Dates with the Work Tracking System" (state Planned, tags
`Community`, `Productboard`).
**Origin**: Chris Graves (Focusrite), an existing paying customer, on the public idea board
(ideas.letpeople.work), posted as "Sync forecasted start dates to Jira". The motivating use is Jira
Plans: with a start and an end in fields, a Plan timeline populates itself from measured flow instead of
from manually estimated dates.
**Waves present**: DISCUSS, DESIGN. (DEVOPS skipped by user decision, 2026-09-20 — see below.)
**Density**: lean (`~/.nwave/global-config.json` → `documentation.density: lean`,
`expansion_prompt: ask-intelligent`).

> **The Epic description is superseded in its central mechanism.** It records the requester's initial
> thinking — derive a Feature's start from the *preceding* Feature's forecasted completion date — and
> flags that this needs a notion of sequence and does not answer what "preceding" means with zero or
> several predecessors. That derivation is rejected here, on the record, in D1. The simulation already
> knows the day it starts each Feature; the number does not need deriving, only recording.

---

## Wave: DISCUSS / [REF] Persona IDs

Defined in `docs/product/personas/` — these are the SSOT ids, not personas invented for this feature.

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
After: run `GET /api/latest/features` and each Feature carries a `startForecast` with dated
P50/P70/P85/P95, or an observed start date when it has already begun. **Corrected 2026-09-20 during
DISTILL**: this said `GET /api/latest/portfolios/{portfolioId}`, which returns only each Feature's id and
name and cannot serve this field. This sentence is where the error entered the document.
Decision enabled: whether a Feature further down the order can be promised to a stakeholder this quarter
at all, before anyone opens a timeline.

#### Acceptance Criteria

- **AC-1.1** — A Feature with remaining work and no observed start carries four dated start percentiles
  (50, 70, 85, 95) in the Feature read (`GET /api/latest/features`, and `…/features/ids?featureIds=…`),
  projected over working days and effective blackout days by the same path the completion percentiles
  use. **Corrected 2026-09-20 during DISTILL**: this said "the portfolio response", which carries only a
  Feature's id and name — an AC satisfiable by wiring the wrong DTO.
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
- **AC-1.10** — The same Feature read carries the per-team **completion** forecasts alongside the
  per-team starts (D16, S22). The existing aggregate `Forecasts` list is unchanged in shape and content,
  so nothing reading it today sees a difference. **Corrected 2026-09-20 during DISTILL**, same reason as
  AC-1.1.
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
| HTTP | `GET /api/latest/features` and `GET /api/latest/features/ids?featureIds=…` — Features carry start percentiles or an observed start date. **Corrected during DISTILL**; `GET /api/latest/portfolios/{id}` carries only id and name and cannot serve a `FeatureDto` | 01 |
| UI | Portfolio, Feature table, Forecasted Start column | 02 |
| UI | Settings, Work Tracking Systems, Write-Back, value source list | 03 |
| Outbound | Write-back round to a Jira or Azure DevOps date field | 03 |
| UI | Portfolio, Delivery, Timeline tab | 04, 05 |

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Forecast wall-clock cost of carrying start dates | at most 110% of `main` | Ten thousand runs over a fifty-Feature portfolio on the dev instance, before and after slice 01 |
| Start percentiles present where a completion percentile is | 100% of Features that carry a completion forecast | Assertion over the dev instance's Feature read |
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
| 2 | User stories with job traceability | US-01 to US-06, each carrying a `job_id` appended to `docs/product/jobs.yaml`. US-02's persona is `product-owner` while its job is the delivery lead's: deliberate, because the question is the same one ("when will you get to this?") asked from two seats, and a second job whose only difference is who is asking would be a duplicate rather than a distinction |
| 3 | Acceptance criteria testable | **39 ACs across six stories** — 10 / 5 / 6 / 9 / 4 / 5 for US-01 to US-06 — each asserting an observable output. *(Corrected 2026-09-20: this read "27 across five", which counted neither US-06 nor AC-1.9/AC-1.10.)* |
| 4 | Dependencies identified | P1-P8; only P8 open, and confined to slice 04 |
| 5 | Sized to fit a slice | **Six slices, 3-8h each, two severable** (05 and 06, independent of each other). *(Corrected 2026-09-20: this read "five slices … one severable".)* US-01 at ~7h and US-04 at ~6h plus a 2h evaluation both sit at the top of the band; slice 01's split is planned rather than contingent — see its brief |
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
10. **AC-1.8 re-measured, not assumed.** `StartForecastWallClockProbe` is `[Explicit]` and never runs in
    CI, so the criterion has no automated enforcement anywhere. Story #6045 does not reach Resolved
    until the probe has been run again on the machine that produced the baseline and both numbers are
    written into the slice brief. Baseline, taken before any of slice 01 was written: median **335 ms**,
    budget **≤ 369 ms**.

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

---

## Wave: DESIGN / [REF] Prior Wave Consultation

Read 2026-09-20 before any decision below. Scope: **application / components**. Mode: **propose**.

| File | State |
|---|---|
| `docs/product/architecture/brief.md` | ✓ read (8476 lines — base Application Architecture plus per-feature deltas) |
| `docs/product/architecture/adr-110-multi-team-forecast-joint-probability.md` | ✓ read |
| `docs/product/architecture/adr-111-aggregate-forecast-field-provenance.md` | ✓ read |
| `docs/product/architecture/adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md` | ✓ read |
| `docs/product/architecture/adr-156-per-trial-max-replaces-product-of-cdfs.md` | ✓ read — **decisive** |
| `docs/product/architecture/adr-159-un-forecastable-blocker-drops-and-the-date-reads-as-a-floor.md` | ✓ read |
| `docs/product/journeys/epic-6033-forecasted-start-dates.yaml` | ✓ read |
| This file's DISCUSS sections, and the six slice briefs | ✓ read |
| `docs/product/outcomes/registry.yaml` | ✓ checked — `check-delta`: 0 collisions |
| `docs/feature/epic-6033-forecasted-start-dates/spike/` | ⊘ not found (no spike was run) |

Rigor: `.nwave/des-config.json` carries no `rigor` key, so standard defaults apply.

**Contradictions with DISCUSS: none.** One decision is corroborated by an ADR that DISCUSS had not read,
and one is reframed. Both are recorded under Changed Assumptions below.

---

## Wave: DESIGN / [REF] The ADR-156 Finding

DISCUSS D4 — observe the Feature-level value inside the trial rather than derive it from the marginals —
is not new here. **ADR-156 proposes exactly that mechanic for completion, and is `Deferred`, not
refused.** Its decision text:

> `TrialState` holds an outstanding-row count per Feature; when a row reaches zero the count is
> decremented, and when the count reaches zero the current simulated day is recorded into that Feature's
> histogram.

The start recorder is that, mirrored: a per-Feature marker set on first pull. Three consequences, all of
which changed this wave's output.

1. **ADR-110's cost objection to per-trial recording is already retired.** It claimed per-trial max
   "needs trial-level storage (10 000 ints × teams per feature)". ADR-156 corrected it: *"trial-level
   storage is not needed: with a shared clock, the maximum over a Feature's rows is a running count, not
   a retained array."* A minimum is a running value for the same reason. D4's cost claim now rests on an
   ADR rather than on assertion.
2. **Slice 01 builds part of the machinery ADR-156 needs — the indexing, not the recorder.** Narrowed
   2026-09-20: this said "same array lifecycle, same recorder, same fold, mirrored", which overstates it.
   ADR-156's mechanic is an outstanding-row counter per Feature that is seeded, decremented as rows
   finish and fires at zero; the start mechanic is a marker set once, on first pull, that never
   decrements. What they genuinely share is the row-to-Feature index and the per-worker array lifecycle
   around it. Un-deferring ADR-156 later becomes cheaper because that prerequisite exists, not because
   the recorder can be reused. **This wave does not un-defer it** —
   AC-1.5 forbids moving completion, and ADR-156's own deferral reason was "one change to forecasting at
   a time". ADR-199 says so explicitly so that a future reader sees the constraint was honoured rather
   than sidestepped.
3. **ADR-156 declares the completion aggregate would join `Feature.Forecasts` as its `TeamId == null`
   row.** That collection's intended future already contains a Feature-grain row. DDD-1 is reconciled
   against it deliberately rather than by coincidence.

---

## Wave: DESIGN / [REF] DDD List

### DDD-1 — Start forecasts live in `Feature.StartForecasts`, a collection of their own type

**ADR-200.** Per-team rows carry `TeamId`; the Feature-grain row carries `TeamId == null` per ADR-111.
`Feature.Forecast` is not touched, so AC-1.5 is a structural property rather than a test —
`AggregatedWhenForecast` cannot see a start row because start rows are not in the collection it reads.

**Amended 2026-09-20, during the DISTILL review gate.** This decision originally said the collection
would be a second `List<WhenForecast>`. That cannot be mapped. `WhenForecast` has exactly one
`FeatureId`/`Feature` pair and `LighthouseAppContext.cs:231-235` already binds it to `Feature.Forecasts`;
EF Core cannot carry two collections of one type over one foreign key. A second FK on `WhenForecast` does
not rescue it either: `FeatureId` is a **required** `int`, so a start row would still have to carry a
valid one and would be loaded straight into `Feature.Forecasts` — the silent corruption D3 exists to
prevent, reintroduced by the mapping.

The forecast hierarchy is already **TPH**: `ForecastBase` is the table, with a `Discriminator` column and
`WhenForecast` as one of its values (`IndividualSimulationResult.Forecast` is typed `ForecastBase`). So:

> **`StartForecast : ForecastBase`** — a TPH *sibling* of `WhenForecast`, not a derived type, carrying
> its own `FeatureId` and `TeamId`. `Feature.StartForecasts` is `List<StartForecast>`.

The guarantee gets stronger rather than weaker: `Feature.Forecasts` is `List<WhenForecast>` and a sibling
type cannot appear in it **by CLR type**, not by filter and not by convention. Expand-only, as the
standing migration rule requires: one new discriminator value and two new nullable columns in an existing
table, with no existing column altered. `SimulationResults` already hangs off `ForecastBase`, so start
rows reuse `IndividualSimulationResult` untouched.

Still rejected, and now for a second reason: one collection with a `ForecastKind` discriminator would put
a filter on the product's most load-bearing number for a storage-tidiness reason, and turn AC-1.5 into
something a test has to catch. Also still rejected: a separate entity with its **own repository**, which
would need a second clear-and-rewrite lifecycle kept in step with `SetFeatureForecasts` by hand — note
that `StartForecast` is not that. It is a collection on the aggregate, written by a setter mirroring
`SetFeatureForecasts`, with no repository of its own.

### DDD-2 — `ForecastBase` is reused as-is for start rows

**ADR-200.** `ForecastBase.GetProbability` with its ascending `KeyOrder` is exactly what a start
percentile needs, and it is on the base rather than on `WhenForecast`.

**Amended 2026-09-20 alongside DDD-1**, and the amendment improves it. This said `WhenForecast` was
reused as-is, and had to excuse `NumberOfItems` — meaningless for a start — as a tolerable wart on the
ADR-111 precedent. `NumberOfItems` is declared on `WhenForecast`, not on `ForecastBase`, so a sibling
never inherits it. There is no wart left to excuse and no unused field on the new type.

### DDD-3 — The recorder extends `TrialState` and `TrialCompletions`. No new type in the simulation

**ADR-199.** One marker array per worker indexed by row and one indexed by Feature, cleared in
`StartAgain()`, written at most once per entity per trial, folded by the existing merge.
`SimulatedRun.WorkOneDayOf` gains one call where `worked` is assigned. `ForecastRunPlan` gains an
unconditional dense row-to-Feature index (`int[]`, parallel to `teamOfRow`) and a Feature count.

**Corrected 2026-09-20**: this said `ForecastRunPlan` *hoists* the grouping it already builds privately
inside `WhatEachRowWaitsFor`. It cannot. That grouping is keyed by `Feature.ReferenceId`, a string,
rather than by a dense index — and `WhatEachRowWaitsFor` returns early with no grouping at all whenever
`NobodyWaitsForAnything`, which the class's own comment says is almost every forecast. There is nothing
to hoist in the ordinary case; the index is new work, and slice 01's estimate carries it as such.

`TrialCompletions` is worth renaming: after this it no longer records only completions.

### DDD-4 — An un-forecastable contributing team needs no new rule

**ADR-199, ADR-201.** `ForecastRunPlan.For` admits no row for a team with no measured throughput, so
nothing is recorded; ADR-112's unknown state already governs the whole Feature through
`Feature.CanBeForecast`, and `FeatureDto` already guards every forecast emission on it. Start forecasts
inherit unknown for free.

ADR-159's competing precedent — a directionally-known date presented as a bound rather than blanked —
was considered and does not apply. ADR-159 distinguishes a team that owns work *inside* the Feature
(ADR-112's case, and ours) from one that is only a start constraint on a Feature whose own work is
forecastable. On our case ADR-159 defers to ADR-112.

### DDD-5 — The observed-start rule lives on `Feature`, not in the DTO

**ADR-201.** DISCUSS D5 said "at read time" and left the placement open. It is not one place: the table
and the timeline read `FeatureDto`, but Story #6047's write-back resolver reads `feature.Forecast`
directly and never passes through the DTO. Putting the rule in the DTO would leave the resolver to
re-implement it — ADR-156 names that failure mode by name ("two independent implementations of the same
verdict"), and the visible symptom would be a Jira Plan bar starting on a forecast date while the table
beside it shows the observed one.

So: one member on `Feature`, carrying the date **and** its provenance, with the DTO and the resolver as
two consumers. Provenance is carried explicitly, never inferred from the absence of percentiles —
ADR-112's rule, for the reason that produced the `return 100` trap.

### DDD-6 — Day-to-date translation is unchanged

**ADR-058** governs and is unaffected. A start day runs through the same `ProjectWorkingDays` over
effective blackout days that a completion day does, after the distribution is read, in both the DTO and
the write-back resolver. **REUSED AS IS.**

### DDD-7 — In-flight Features are not seeded into the WIP slots

**ADR-202.** Records DISCUSS D6 as an architectural decision rather than a product preference, because
seeding is the first thing a reader of ADR-199 will propose. Two reasons it is refused: it would launder
the discrepancy worth seeing, and it would move completion forecasts for every existing user as a side
effect of an Epic about start dates.

The docs obligation in AC-4.9 is the whole mitigation, not a nicety. If it is cut from Story #6048 the
decision is not implemented, only the code is.

---

## Wave: DESIGN / [REF] Component Decomposition

Every row is **EXTEND** but one. **Corrected 2026-09-20**: this said the feature introduces no new type in
the backend. DDD-1's amendment adds exactly one, `StartForecast`, and the reason is a mapping constraint
rather than a modelling preference.

| Component | File | Change | Summary |
|---|---|---|---|
| `TrialState` | `Services/Implementation/Forecast/TrialState.cs` | EXTEND | Per-row and per-Feature start markers, cleared in `StartAgain()`, set on first pull, read once at trial end. |
| `TrialCompletions` | `Services/Implementation/Forecast/TrialCompletions.cs` | EXTEND (rename) | Parallel arrays for the two start grains plus their fold. No longer records only completions. |
| `SimulatedRun` | `Services/Implementation/Forecast/SimulatedRun.cs` | EXTEND | One recording call in `WorkOneDayOf` where `worked` is assigned — before the `CloseOneItemOf` branch, so a pull that does not finish the row still counts as a start. |
| `ForecastRunPlan` | `Services/Implementation/Forecast/ForecastRunPlan.cs` | EXTEND | Build an unconditional row-to-Feature index (`int[]`, parallel to `teamOfRow`) plus a Feature count. **Corrected 2026-09-20**: this said "hoist the grouping already built privately in `WhatEachRowWaitsFor`". That grouping is keyed by `Feature.ReferenceId` (a string), not by a dense index, and `WhatEachRowWaitsFor` returns early with no grouping at all whenever `NobodyWaitsForAnything` — which the class's own comment says is almost every forecast. There is nothing to hoist in the common case; the index is new work. |
| `ForecastService` | `Services/Implementation/Forecast/ForecastService.cs` | EXTEND | A second fold beside `RecordTheDaysEachRowFinishedOn`; build and attach start forecasts in `UpdateFeatureForecasts`. |
| `StartForecast` | `Models/Forecast/StartForecast.cs` | **NEW** | A TPH sibling of `WhenForecast` under `ForecastBase`, carrying `FeatureId` and a nullable `TeamId`. The one new type in the backend — see DDD-1, amended. |
| `Feature` | `Models/Feature.cs` | EXTEND | `StartForecasts` (`List<StartForecast>`), its setter mirroring `SetFeatureForecasts`, and the ADR-201 observed-or-forecast member. |
| `LighthouseAppContext` | `Data/LighthouseAppContext.cs` | EXTEND | One `HasMany` and one nullable-team `HasOne` for the new sibling type, alongside the existing `Forecasts` pair rather than duplicating it over the same foreign key. **Name the columns explicitly**: `StartForecast.FeatureId`/`TeamId` collide by name with `WhenForecast`'s in the shared TPH table, and convention will either uniquify them to `FeatureId1` or refuse. Map them with `HasColumnName` and read the generated migration before accepting it. |
| `FeatureRepository` | `Services/Implementation/Repositories/FeatureRepository.cs` | EXTEND | `GetFeatures()` eager-loads `Forecasts` and must load `StartForecasts` the same way. Missed by DESIGN's first pass and found during DISTILL: without it the collection reads back empty through every path in the product, while any test that keeps the entity in the tracker still passes. |
| EF migration | `Lighthouse.Migrations.*` | NEW (generated) | Additive, expand-only, via the `CreateMigration` script across all providers. |
| `FeatureDto` | `API/DTO/FeatureDto.cs` | EXTEND | Start percentiles or observed date with provenance; **and** the per-team completion forecasts, which die at this boundary today (S22). Existing `Forecasts` list unchanged in shape and content. |
| `WriteBackValueSource` | `Models/WriteBack/WriteBackValueSource.cs` | EXTEND | Four members appended after `SleRisk`. Ordinals are persisted; a regression test pins every pre-existing member. |
| `WriteBackTriggerService` | `Services/Implementation/WriteBackTriggerService.cs` | EXTEND | Start resolver reading the ADR-201 member; the forecast-source list and the percentile switch both extended. |
| `WriteBackMappingValidator` | `API/Helpers/WriteBackMappingValidator.cs` | EXTEND | The second of the two hardcoded forecast-source lists. |
| Feature table column | `Lighthouse.Frontend/src/components/Common/FeatureListDataGrid/` | EXTEND | One forecast at four percentiles, following the completion column. No expander (D16). |
| Delivery Timeline tab | `pages/Portfolios/Detail/Components/DeliveryGrid/` | NEW (frontend) | The one genuinely new component. Its shape is P8 and is settled by slice 04's evaluation, not here. |

---

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `WhenForecast` | `Models/Forecast/WhenForecast.cs` | Carries a histogram, a nullable team, a Feature FK | **NOT REUSED — corrected 2026-09-20** | It cannot be: its single required `FeatureId` is already bound to `Feature.Forecasts`, so a start row of this type would be loaded into the completion collection. See DDD-1, amended. |
| `ForecastBase` | `Models/Forecast/ForecastBase.cs` | Percentile and likelihood reads over a histogram, and the TPH root the forecast table is keyed on | **REUSED AS IS — now by inheritance** | `KeyOrder` already parameterises direction; a start is ascending-by-day exactly as a completion is. `StartForecast` derives from it directly, so it inherits the percentile reads and the `SimulationResults` relationship and nothing else. |
| `AggregatedWhenForecast` | `Models/Forecast/AggregatedWhenForecast.cs` | Combines per-team histograms to Feature grain | **NOT REUSED — deliberately** | It combines *marginals after the run*, which is the assumption ADR-199 exists to avoid. The Feature-grain start is observed in-trial and stored. Leaving this type untouched is what makes AC-1.5 structural. |
| `JointCompletionDistribution` | `Models/Forecast/JointCompletionDistribution.cs` | Product of CDFs across teams | **NOT REUSED — deliberately** | Its mirror for a minimum is `1 - Π(1 - Fi)`, and it carries the independence assumption epic-5792 made false. Untouched; ADR-110 and ADR-156 both stand. |
| `TrialCompletions` | `Services/.../TrialCompletions.cs` | Per-worker lock-free day histograms, merged once | **EXTEND** | Adding two arrays to an existing per-worker recorder is a handful of lines against a new parallel recorder with its own merge, its own lifetime and a second chance to get the lock-free contract wrong. |
| `TrialState` | `Services/.../TrialState.cs` | Per-trial mutable state owned by one run | **EXTEND** | It already owns `dayEachRowFinished` with exactly this lifecycle. A start marker is the same array with the opposite trigger. |
| `ForecastRunPlan` | `Services/.../ForecastRunPlan.cs` | Row-to-Feature grouping | **EXTEND — new work, not a hoist** | Corrected 2026-09-20. The existing grouping is keyed by string `ReferenceId` and is skipped entirely when nothing waits on anything, which is the ordinary case. A dense `int[]` index has to be built unconditionally, and slice 01's estimate should carry it as new work rather than as a move. |
| `ResolveForecastValue` | `Services/.../WriteBackTriggerService.cs` | Percentile, working-day projection, blackout, format | **EXTEND** | The start resolver is this method with a different distribution and one branch. Writing a parallel resolver would duplicate the blackout handling, which is exactly where ADR-058 says one implementation belongs. |
| `Feature.CanBeForecast` / `TeamsWithoutForecast` | `Models/Feature.cs` | Unknown-forecast predicate | **REUSED AS IS** | ADR-159 reached the same verdict on the same members. Start inherits the gate; no new rule. |
| `ProjectWorkingDays` / blackout services | per ADR-058 | Day-to-date translation | **REUSED AS IS** | Identical for a start and a completion. |
| Completion column | `FeatureListDataGrid` | Percentile presentation in a table cell | **EXTEND** | The start column follows it; D16 keeps them the same shape deliberately, so divergence would be the defect. |
| `DeliveryMetricsTab` | `.../DeliveryGrid/DeliveryMetricsTab.tsx` | Tab slot on a Delivery | **EXTEND** | The Timeline tab reuses the tab structure; only its content is new. |
| A Gantt component | — | Bars on a date axis | **CREATE NEW — justified, and costed first** | Nothing in the codebase draws a bar against a date axis. MUI X Gantt exists and is Premium-licence-only and not owned. This is the only CREATE NEW here, it is the Epic's only open pre-requisite (P8), and slice 04 opens with a timeboxed evaluation and a candidate shortlist rather than a decision taken now. |

**Zero unjustified CREATE NEW.** The single CREATE NEW row is gated behind an evaluation.

### Contract shape per component

Added 2026-09-20 on review: the Effect Isolation mandate wants every touched component classified, and
neither table above did it. Kept as its own table rather than a sixth column, because the Reuse Analysis
row is already the widest thing in this document.

| Component | Contract shape | Universe it may touch |
|---|---|---|
| `TrialState`, `TrialCompletions` | bounded-change | The new per-row and per-Feature marker arrays, and nothing else. The completion arrays are read and written exactly as before. |
| `SimulatedRun` | bounded-change | One recording call. It may not change which row is drawn, how much is delivered, or when a row closes. |
| `ForecastRunPlan` | bounded-change | A new index and count. The row order, the team mapping and the waits are unchanged. |
| `ForecastService` | bounded-change | A second fold and a second attach. `RecordTheDaysEachRowFinishedOn` and `SetFeatureForecasts` are untouched. |
| `StartForecast`, `Feature.StartForecasts` | pure-function on read | A distribution and its percentile reads. No behaviour of `Feature.Forecast` is reachable from it. |
| `FeatureDto` | bounded-change | New fields only. `Forecasts` keeps its shape and its content — the `unbounded-preservation` half, asserted by scenario 6. |
| `WriteBackValueSource` | unbounded-preservation | Every existing ordinal. Members may be appended, never inserted or reordered; a stored mapping that re-points is a silent data corruption. |
| `WriteBackTriggerService` | bounded-change | The value-resolution branch. **The outbound write path itself is REUSED AS IS** — this is the one component here with a real external effect, and slice 03 may not touch how or when it writes, only what value it resolves. |

---

## Wave: DESIGN / [REF] Driving and Driven Ports

No new driving port. **`GET /api/latest/features`** (and `…/features/ids?featureIds=…`) carries the new
payload; the write-back mapping screen's existing endpoints carry the four new value sources.

**Corrected 2026-09-20 during DISTILL.** This section named `GET /api/latest/portfolios/{portfolioId:int}`,
inherited unchecked from US-01's pitch. That endpoint returns `PortfolioDto`, which carries only a
Feature's id and name and cannot serve a `FeatureDto` at all. "No new driving port" is a claim that
requires knowing the port exactly, so it is DESIGN's to verify rather than DISCUSS's to be trusted on.

| Driven port | Adapter | Change |
|---|---|---|
| Persistence | `LighthouseAppContext` → EF Core, all providers | EXTEND — one collection, one additive migration |
| Work tracking system | `WriteBackService` → Jira / Azure DevOps connectors | REUSED AS IS — the resolver is connector-agnostic, so both gain start sources by existing (D15) |

---

## Wave: DESIGN / [REF] Technology Choices

Nothing new is pinned. C# .NET 10 / EF Core on the backend, React 18 + TypeScript on the frontend, both
unchanged. The only open technology question is the timeline component (P8), deliberately unanswered
here — see Open Questions.

---

## Wave: DESIGN / [REF] Decisions Table

| ID | Decision | ADR |
|---|---|---|
| DDD-1 | Start forecasts live in `Feature.StartForecasts` | ADR-200 |
| DDD-2 | `WhenForecast` reused as-is for start rows | ADR-200 |
| DDD-3 | Recorder extends `TrialState` + `TrialCompletions`; no new simulation type | ADR-199 |
| DDD-4 | Un-forecastable contributor inherits ADR-112's unknown; no new rule | ADR-199, ADR-201 |
| DDD-5 | Observed-start rule on `Feature`, consumed by the DTO and the resolver | ADR-201 |
| DDD-6 | Day-to-date translation unchanged | ADR-058 (existing) |
| DDD-7 | In-flight Features are not seeded into the WIP slots | ADR-202 |

---

## Wave: DESIGN / [REF] Open Questions

| # | Question | Deferred to |
|---|---|---|
| P8 | What draws the timeline — `@mui/x-charts` primitives, plain SVG, a third-party Gantt, or a purchased MUI X Premium licence? | Slice 04's 2h evaluation, which carries a candidate shortlist and produces a costed recommendation |
| — | Can the chosen component render sub-lanes under a summary bar? | Same evaluation. Slice 06 is severable, so this does not gate the choice, but a component that forecloses it is worth knowing about while the choice is open |
| — | Does the summary-versus-sub-lane disagreement (ADR-199) read as correct to a user? | Slice 06's dogfood. If it cannot be made to read as correct, slice 06 does not ship |
| — | Is `TrialCompletions` renamed, and to what? | **Closed in DELIVER: `TrialRecordings`.** It records three things now and completions are one of them, so the old name named a third of the type |
| — | How many Features on a real instance have more than one contributing team? | Slice 06's pre-code count. It decides whether sub-lanes are worth building at all |

---

## Wave: DESIGN / [REF] Changed Assumptions

**1. D4's justification was overstated.**

> **Original** (this file, DISCUSS / Locked Decisions, D4, 2026-09-20): "A start cannot be [derived], and
> the difference is not a detail: the earliest of several teams' starts has to be taken **within one
> simulated run** … Taking the minimum of separately computed per-team percentiles afterwards gives a
> number that no simulated run ever produced."

**New**: the Feature-grain start *can* be recovered from the marginals, via `1 - Π(1 - Fi(t))` — the
exact mirror of what `JointCompletionDistribution.Combine` does for completion under ADR-110. The
argument for observing it in-trial is narrower and stronger: that formula assumes the contributing teams
are independent, and epic-5792 made that false. Same decision, honest reasoning. D4 was corrected in
place on 2026-09-20; ADR-199 carries the full form.

**2. D4's grain was widened at the maintainer's request.**

> **Original** (D4, first form): Feature grain only.

**New**: both grains are recorded and stored. Per-team rows cost nothing extra — the marker is per row
before it is folded per Feature — and slice 06's sub-lanes need them. Recorded as US-06, AC-1.9 and
AC-1.10, and reflected in ADR-199 and ADR-200.

**3. D6 moved from a documented residual to an architectural decision.**

> **Original** (D6, first form): "A follow-up item is owed" for seeding in-flight Features into the WIP
> slots.

**New**: declined, not deferred to an owed item. ADR-202 records why, and moves the mitigation to a docs
deliverable (AC-4.9) that is load-bearing rather than optional.

**No upstream changes are owed.** No DISCUSS user story or acceptance criterion is invalidated by this
wave; AC-1.9 and AC-1.10 were added during DISCUSS itself, before DESIGN began.

---

## Wave: DEVOPS / [REF] Skipped, by explicit decision

**Skipped by the user, 2026-09-20**, in as many words. Recorded rather than passed over silently: the
wave sequence runs DISCOVER → DIVERGE → DISCUSS → DESIGN → DEVOPS → DISTILL → DELIVER, and a wave that
does not run has to say so and say why, or a decision it would have made gets improvised downstream.

DEVOPS receives `outcome-kpis` only, and drives observability, instrumentation and deployment readiness
from it. Against this feature that surface is genuinely empty:

| DEVOPS concern | State for this feature |
|---|---|
| Deployment topology | **N/A** — no new container, no new service, no new external dependency. ADR-202's brief delta records that nothing is visible at C4 System Context or Container level. |
| Infrastructure as code | **N/A** — no chart change, no new environment variable, no new secret. |
| CI/CD pipeline | **N/A** — no new workflow. The existing gates cover it: `dotnet build`/`test`, `pnpm test`/`build`, Biome, SonarQube Cloud, and E2E through `ci_verifysqlite` / `ci_verifypostgres`. |
| Database migration | **Covered by DESIGN, not DEVOPS** — additive and expand-only, generated by the `CreateMigration` script across all providers, per the standing project rule. Why that is a sufficient safety argument, named on review rather than left to be rediscovered: migrations are applied automatically at startup (`Program.cs`, `DatabaseConfigurator.ApplyMigrations`) and gated by the existing `MigrationsAppliedHealthCheck` on the readiness and startup tags, so an instance that has not finished migrating never takes traffic. This feature adds no new logic to that mechanism; it relies on it. |
| Observability / instrumentation | **The one real item, and it is already owned.** The forecast wall-clock budget (AC-1.8: ten thousand runs over a fifty-Feature portfolio within 110 % of the `main` baseline) is a measurement slice 01 takes on the dev instance, before and after. It is an acceptance criterion rather than a dashboard, because it gates one change rather than running forever. **The residual, named on review:** nothing then watches forecast latency over time, so if a real portfolio grows past the shape the probe measures and the forecast slows, it surfaces as a support ticket rather than as a metric. Accepted — this Epic is not the place to build forecast-latency instrumentation — but accepted explicitly rather than by omission. Enforcement of the *one* measurement it does owe is Definition of Done item 10. |
| Rollout / feature gating | **Covered by DISCUSS D8** — forecast and table free, timeline premium via the Delivery surface's existing notice, write-back inheriting the mapping screen's existing gate. No new gate mechanism. |
| Production readiness sign-off | **Deferred to DELIVER**, where it always sits for this project. |

**What a skipped DEVOPS would otherwise have improvised, and where it landed instead:** the performance
budget. That is the only DEVOPS-shaped question this feature raises, and it is pinned as AC-1.8 with a
stated method and a stated threshold rather than left to be discovered under load. If it fails, slice
01's learning hypothesis names the fallback.

Next wave: DISTILL.

---

## Wave: DISTILL / [REF] Prior Wave Consultation

Read 2026-09-20, before a scenario was written.

| File | State |
|---|---|
| `docs/feature/epic-6033-forecasted-start-dates/feature-delta.md` (DISCUSS + DESIGN + DEVOPS sections) | ✓ read |
| `slices/slice-01-the-run-records-the-day-it-starts.md` | ✓ read |
| `docs/product/journeys/epic-6033-forecasted-start-dates.yaml` | ✓ present |
| `docs/architecture/atdd-infrastructure-policy.md` | ✓ read, and appended to (two rows, below) |
| `docs/product/outcomes/registry.yaml` | ✓ read — registration deferred, see below |
| `.nwave/des-config.json` | ✓ read — no `deliverable_type` key, so `application`; no type-specific reviewer routing applies |
| `spike/` | ⊘ not found (none was run, by decision) |

Wave-decision reconciliation: **0 contradictions.** DEVOPS is a recorded skip rather than a missing
wave, and the one concern it would have owned is already an acceptance criterion (AC-1.8), so the
graceful-degradation path does not apply — there is nothing to improvise a default for.

---

## Wave: DISTILL / [REF] Upstream Issues Found While Writing the Scenarios

Two, both corrected here rather than left for DELIVER to trip over.

**1. US-01's elevator pitch names the wrong endpoint.** It says the payload arrives from
`GET /api/latest/portfolios/{portfolioId}`. That endpoint returns `PortfolioDto`, which carries only
`EntityReferenceDto` rows — a Feature's id and name. `FeatureDto`, and therefore every forecast on it,
is served by **`GET /api/latest/features`** and `GET /api/latest/features/ids?featureIds=…`
(`FeaturesController.BuildFeatureDto`). The scenarios drive the real one. No decision changes; the
pitch's wording is wrong and the driving port below is authoritative.

**2. `FeatureRepository.GetFeatures()` eager-loads `Forecasts` and will have to load `StartForecasts`
too.** `Include(f => f.Forecasts).ThenInclude(f => f.SimulationResults)`. A second collection that is
not added there reads back empty through every path in the product while passing any test that keeps
the entity in the tracker. Named now because it is invisible until it is a bug report. Slice 01 owns it.

One thing DISTILL **confirmed** rather than corrected, because D6 and AC-1.3 both rest on it:
`FeatureRepository.GetAll()` applies `featureOrdering.Order(...)`, so the simulation really does build
its rows in board order. D6's "the run forecasts the board as configured" is a property of the code,
not an assumption.

---

## Wave: DISTILL / [REF] Scope of This Pass

**Slice 01's scenarios are executable now. Slices 02–06's are catalogued, not authored.** Stated rather
than skipped silently, with the reason per slice:

| Slice | State | Why |
|---|---|---|
| 01 | **Executable**, 10 scenarios + 1 probe, committed RED-ready | Next into DELIVER |
| 02 (table column) | Catalogued | Frontend. A skipped Vitest test is still type-checked, so a pending test that names a prop forces that prop to exist before the suite can be green — authoring it now would drag slice 02's component surface into slice 01's commit. Authored at the head of its own DELIVER, which is this repo's standing practice (`DeliverySources/Slice0*`) |
| 03 (write-back) | Catalogued | Backend, and authorable now; held with 02 so each slice's ATs land with the slice, per the same practice |
| 04–06 (timeline) | Catalogued | Blocked on **P8** — what draws the timeline is an open DESIGN question settled by slice 04's own 2h evaluation. Scenarios written against a component nobody has chosen would pin the wrong driving surface |

---

## Wave: DISTILL / [REF] The Wire Contract Slice 01 Is Written Against

DESIGN settled that `FeatureDto` carries "start percentiles or observed date with provenance, and the
per-team completion forecasts" without naming fields. The scenarios are executable, so they had to. This
is the contract; it is the spec, and DELIVER implements it rather than renaming it.

```jsonc
{
  "forecasts": [ /* unchanged - the aggregate completion, four percentiles */ ],

  "startForecast": {
    // Provenance first and always explicit. Never inferred from the absence of percentiles:
    // ADR-112's rule, for the reason that produced the `return 100` trap.
    "source": "Forecast" | "Observed" | "Unknown",
    "observedDate": "2026-09-14T00:00:00Z",   // set iff source == Observed
    "percentiles": [ { "probability": 50, "expectedDate": "…" }, … ]  // four iff source == Forecast
  },

  "teamForecasts": [
    {
      "teamId": 7,
      "startPercentiles": [ /* four */ ],
      "completionPercentiles": [ /* four - the gap S22 named, closed here */ ]
    }
  ]
}
```

`Unknown` is the third source rather than an omitted object, because "this Feature cannot be forecast"
and "this Feature has not been forecast yet" are different answers and a client that read absence would
be re-deciding, in every screen, a rule the domain already decided.

---

## Wave: DISTILL / [REF] Scenario List With Tags

All ten drive `GET /api/latest/features` against a real ASP.NET host with real EF over SQLite. Categories
carried by every fixture: `acceptance`, `epic-6033-forecasted-start-dates`, `slice-01`.

Each scenario also carries a `// @…` tag line above it, following the convention already used by
`API/Integration/BlockedItems/Slice0*Scenarios.cs` — `@driving_port`, `@real-io`, `@walking_skeleton`,
`@error`/`@edge`/`@regression`/`@invariant`, the `@us-01` trace and a
`@contract-shape:<pure-function|bounded-change|unbounded-preservation>` classification. Added on review;
they were missing from the first pass, and the per-component classification they roll up to is the
Contract shape table in the DESIGN sections.

| # | Scenario | AC | Fixture | Notes |
|---|---|---|---|---|
| 1 | `A_Feature_nobody_has_started_says_when_work_on_it_is_expected_to_begin` | AC-1.1 | deterministic | **walking skeleton** — the whole path, run to wire |
| 2 | `At_Feature_WIP_one_the_next_Feature_starts_the_day_the_one_above_it_finishes` | AC-1.2, D7 | deterministic | written from the reasoning, before it was run |
| 3 | `A_Feature_already_in_flight_reports_the_day_it_actually_started` | AC-1.6, D5 | deterministic | |
| 4 | `A_Feature_two_Teams_share_reports_a_start_and_a_completion_for_each_of_them` | AC-1.9, AC-1.10 | deterministic | |
| 5 | `A_Feature_one_Team_works_reports_the_same_number_at_both_grains` | AC-1.9 | deterministic | the roll-up must read, not compute |
| 6 | `Nothing_about_the_completion_forecast_moves` | AC-1.5 | deterministic | **Green from day one** — a regression guard, not a RED scaffold. See Red Classification |
| 7 | `A_Feature_a_Team_cannot_be_forecast_for_carries_no_start_date_either` | AC-1.7, DDD-4 | deterministic | |
| 8 | `Feature_WIP_bounds_how_many_Features_can_begin_on_the_first_day` | AC-1.3, S2 | sampled | |
| 9 | `Raising_Feature_WIP_lets_them_all_begin_on_the_first_day` | AC-1.3 | sampled | |
| 10 | `The_start_a_Feature_reports_is_the_one_the_run_saw_not_the_one_its_Teams_marginals_imply` | AC-1.4, D4, ADR-199 | sampled + dependency | **the decisive one** |
| — | `StartForecastWallClockProbe` | AC-1.8 | `[Explicit]` | measured by hand, before and after |

**Error and edge coverage: 4 of 10** (3, 7, 8, 10 — an already-started Feature, an un-forecastable
contributor, a Feature held out by WIP, and a Feature whose Teams do not move independently), and the
tags back the claim: `grep -c '@error\|@edge'` over the two scenario files returns 4. The
remaining six are the contract itself. The 40% target is met without inventing failure modes this slice
does not have: it adds no new input the user supplies, no new adapter and no new external call.

**How scenario 10 is decisive.** Both Teams of the shared Feature wait on the same upstream Feature, so
they begin on the same day in every run. Observed in-trial, the Feature's start is that day — exactly
what each Team reports. Derived from the marginals with `1 − Π(1 − Fi)` it would be strictly earlier,
because the formula credits the Feature with two independent chances of having started when it only ever
had one. The scenario also asserts the distribution is not a point mass, because on a degenerate one the
two methods agree and the test would prove nothing.

---

## Wave: DISTILL / [REF] Architecture of Reference and Infrastructure Policy

`--policy=inherit`. Every port in scope was already in
`docs/architecture/atdd-infrastructure-policy.md` except two, which were appended there per
write-if-absent rather than decided inline:

| Port | Class | Mechanism |
|---|---|---|
| HTTP API | Driving | `TestWebApplicationFactory<Program>`, existing row |
| `LighthouseAppContext` + repositories | Driven internal | Real EF over SQLite, existing row |
| `IForecastService` | Driven internal — **real, never mocked here** | The simulation is the subject. Existing row already carves out this exception for epic-4365's gold tests; this slice is the same case |
| `IDrawStreamFactory` | Driven external / non-deterministic | **New row.** `DrawsTheSameNumberEveryTime` where the claim is about which day; `DrawsFromAPinnedStartingNumber` where the claim needs spread |
| `ForecastSimulationLimits` | Configuration, not a port | **New row.** 50 runs deterministic, 1 000 sampled, against the product's 10 000 |
| `ITeamMetricsService` | Driven internal — **faked** | **New row.** Measured throughput is this feature's *input*. Seeding it as closed work items would put the metrics service's windowing, filtering and blackout arithmetic between the number the test chose and the number the run drew |
| `ILicenseService` | Driven external | Existing row — premium true, so the dependency decision scenario 10 needs is reachable |

---

## Wave: DISTILL / [REF] Adapter Coverage

| Driven adapter | `@real-io` scenario | Covered by |
|---|---|---|
| EF `LighthouseAppContext` (SQLite) | YES | all ten — the Feature is written, re-read through the repository and served |
| `FeaturesController` read path | YES | all ten |
| `IWorkTrackingConnector` | N/A | nothing in slice 01 reaches a tracker. Slice 03 is where a write-back adapter first appears, and its ATs are catalogued below |
| Work tracking system write-back | N/A | slice 03 |

---

## Wave: DISTILL / [REF] Driving Adapter Coverage

DESIGN declares **no new driving port**. Scanned for entry points: no new CLI, no new endpoint, no new
hook. The existing `GET /api/latest/features` carries the new payload and every scenario invokes it over
HTTP through the real host — not by calling `BuildFeatureDto`, which would prove the projection works
without proving it is wired, serialised and reachable.

---

## Wave: DISTILL / [REF] Scaffolds (Mandate 7)

**None, and that is the point.** Mandate 7 exists so a test importing a not-yet-written production type
fails RED rather than failing to build. The scenarios import no such type: they arrange through surfaces
that already exist (`Feature`, `FeatureWork`, `StateCategory`, `StartedDate`, `IForecastService`) and
assert against the **wire**, reading `startForecast` and `teamForecasts` out of the JSON.

So the test project compiles against `main` as it stands, and each scenario fails on the answer — "no
forecasted start at the 50th percentile" — rather than on a missing symbol. That is RED for the right
reason with nothing to clean up afterwards, and it is why a `__SCAFFOLD__` sweep has nothing to find.

The cost is named: a wire-level assertion cannot see inside the domain, so **AC-1.5's structural half —
that no start row is in `Feature.Forecasts` — is asserted by consequence rather than directly.** A start
row folded into that collection drags every completion percentile earlier, which scenario 6 catches. A
direct assertion on the collection belongs in DELIVER's unit tests, where the type exists.

---

## Wave: DISTILL / [REF] Test Placement

`Lighthouse.Backend.Tests/API/Integration/ForecastedStartDates/`, following
`API/Integration/DependencyAwareForecasting/` and `API/Integration/DeliverySources/` — the two nearest
precedents, both forecast-adjacent, both split `Slice0N…Scenarios.cs` (the Given/When/Then) from
`Slice0N…Specifications.cs` (the step definitions) across one partial class.

| File | Holds |
|---|---|
| `ForecastedStartDateAcceptanceTest.cs` | The pinned host and the shared Given/When/Then vocabulary |
| `Slice01StartForecastScenarios.cs` / `…Specifications.cs` | Scenarios 1–7, deterministic draws |
| `Slice01SampledStartScenarios.cs` / `…Specifications.cs` | Scenarios 8–10, pinned sampled draws |
| `StartForecastWallClockProbe.cs` | AC-1.8, `[Explicit]` |

---

## Wave: DISTILL / [REF] Red Classification

**Measured, not inferred.** The ten scenarios were unskipped once locally, run against unmodified `main`,
and the `[Ignore]` attributes put back; nothing from that run is committed. Build succeeded with 0 errors
and 0 warnings, and the run was **9 failed, 1 passed**.

Every one of the nine fails on an assertion — `Expected: "Forecast" But was: null`, `Expected: not null
But was: null`, `Expected: 2026-09-22 But was: null` — with no exception, no import error and no fixture
failure anywhere. That is RED for the right reason, stated on evidence.

Two things the run settled that a green build could not have:

- **Scenario 6 is a regression guard, not a RED scaffold.** It is the one that passes. It asserts only
  against the completion contract, which this slice must leave exactly where it is, so it is green from
  day one by design. It is marked as such in the code, because DELIVER reading its green as evidence of
  work done would be reading false progress.
- **The fixtures work end to end.** Scenario 10 reached its assertion, so the dependency seeding, the
  premium licence path, EF, the real forecast and the HTTP read are all wired correctly — the parts of
  this fixture most likely to be broken in a way that looks like a failing feature. And
  `At_Feature_WIP_one…` reported Alpha's completion as `2026-09-22`: simulated day 2, projected over
  working days, which is exactly the arithmetic scenario 2 was built on. The fixture's premise is
  confirmed by the half of it that already exists.

Every scenario carries `[Ignore("RED scaffold written by DISTILL. DELIVER slice 01 (Story #6045)
unskips these one at a time.")]`, so the hand-off commit is green and nothing RED reaches `main`.

One shape was chosen for that reason: every comparison between two dates asserts each date is present
**first, and outside the multiple-assert scope**. Inside one, a failed not-null assertion does not
throw, so the comparison that followed would dereference a null and the scenario would be reported as an
error rather than a failure — BROKEN dressed as RED, which is the exact signal this gate exists to keep
clean.

---

## Wave: DISTILL / [REF] Scenario Catalogue for Slices 02–06

Authored at the head of each slice's own DELIVER. Listed here so the ACs are already mapped and no slice
starts by re-deciding what to prove.

| Slice | Scenario | AC | Driving port |
|---|---|---|---|
| 02 | The Feature table shows a Forecasted Start column following the completion column's presentation | AC-2.1 | React component tree (Vitest + RTL) |
| 02 | A started Feature shows one date, visibly marked observed, with no percentile | AC-2.2 | same |
| 02 | A Feature with no start forecast shows the empty state the completion column already uses | AC-2.3 | same |
| 02 | The column renders with no premium licence | AC-2.4 | same |
| 02 | Sorting and the existing column set are unchanged | AC-2.5 | same |
| 03 | Four new `WriteBackValueSource` members, appended last; a test pins every pre-existing ordinal | AC-3.1 | unit — the enum is persisted by ordinal |
| 03 | A mapped start source writes the forecasted start on the next round, honouring `DateFormat` | AC-3.2 | write-back round through the real resolver |
| 03 | A started Feature writes its observed date | AC-3.3 | same |
| 03 | A Done Feature writes nothing | AC-3.4 | same |
| 03 | A Feature with no start forecast writes nothing rather than an epoch date | AC-3.5 | same |
| 03 | The sources appear only under a premium licence | AC-3.6 | mapping screen endpoints |
| 04 | A Delivery carries a Timeline tab, one bar per Feature in board order | AC-4.1 | **P8 — surface not chosen** |
| 04 | A bar spans start to completion at the selected percentile; the selector moves both ends | AC-4.2, AC-4.3 | P8 |
| 04 | A started Feature's bar begins at its observed start | AC-4.4 | P8 |
| 04 | A Feature with no forecast is listed with a stated reason rather than omitted | AC-4.5 | P8 |
| 04 | The Delivery's target date is marked on the axis | AC-4.6 | P8 |
| 04 | The tab is premium-gated using the existing notice | AC-4.7 | P8 |
| 04 | Legible in both themes and at the narrowest supported width | AC-4.8 | P8 |
| 04 | **AC-4.9 is a docs deliverable, not a test.** ADR-202's mitigation is the documentation saying plainly what an out-of-order board does to the picture. If it is cut, the decision is not implemented — only the code is | AC-4.9 | docs |
| 05 | Dependency lines on the timeline | US-05 | P8, severable |
| 06 | Per-team sub-lanes under the summary bar | US-06 | P8, severable |

---

## Wave: DISTILL / [REF] Outcomes Registry

**Registration deferred to DELIVER, deliberately.** Slice 01 introduces one new typed contract worth a
row — the observed-or-forecast start rule on `Feature` (ADR-201), a `specification`. Every existing row
in `docs/product/outcomes/registry.yaml` names an `artifact` path that exists; registering one now would
point at a file nobody has written. Registered in the same commit that creates it.

---

## Wave: DISTILL / [REF] Pre-requisites Carried Into DELIVER

| # | Pre-requisite | State |
|---|---|---|
| P1, P2, P4 | The `worked` variable, the WIP draw range, `StartedDate` on the entity | Confirmed by reading (S1, S2, S14) and again here |
| — | Board order really governs the run | **Newly confirmed** — `FeatureRepository.GetAll()` orders through `IFeatureOrdering` |
| — | `GetFeatures()` must eager-load `StartForecasts` | **Owed by slice 01** (upstream issue 2 above) |
| — | EF migration, additive and expand-only, via `CreateMigration` across all providers | Owed by slice 01 |
| AC-1.8 | Wall-clock budget | **Measured both sides, and it passes.** Twelve samples each, same machine, the baseline re-taken in a worktree at the pre-implementation commit: median **334 ms** before, **354.5 ms** after — **106.1%** against a budget of 110%. Detail, including why the sample was widened and what that cost in credibility, is in the slice brief |
| P8 | What draws the timeline | Still open. Gates slices 04–06 only |

---

## Wave: DISTILL / [REF] Self-Review

| Item | State |
|---|---|
| Walking skeleton exists, exercises the real driving adapter over HTTP | ✓ scenario 1 |
| Every driven adapter has a real-I/O scenario | ✓ — EF is real in all ten; no other adapter is in slice 01 |
| In-memory doubles: what they cannot model is written down | ✓ — the `ITeamMetricsService` fake cannot model throughput windowing, filtering or blackout arithmetic, and no scenario asserts any of them |
| Mandate 7: imports resolve, tests RED not BROKEN | ✓ — build clean, no scaffolds needed, and the reason is recorded |
| Business language in scenario names | ✓ — no scenario name contains API, DTO, endpoint or histogram |
| Error and edge coverage | ✓ 4 of 10 |
| Test placement follows precedent | ✓ |
| Prior-wave decisions reconciled | ✓ 0 contradictions |
| Upstream issues raised rather than worked around | ✓ two, both above |
| Red classification measured rather than inferred | ✓ — unskipped once locally, 9 failed / 1 passed, every failure an assertion |
| Scenario count against the sizing signal | ⚠ **10 scenarios, above the 8 that prompts a sizing conversation.** Not reduced: the count is AC-driven, and the ACs are the Epic's decisive claims. Flagged for the estimate rather than trimmed — and it is a second reason slice 01's planned split matters |
| Sub-assertions that only start working later | ⚠ Named rather than removed. In scenario 3, `no start percentiles` is vacuously true today; in scenario 7, the completion and `teamsWithoutForecast` assertions read pre-existing fields. Each sits beside a sibling that genuinely fails, so the scenario as a whole is honest RED — but a partial implementation that leaves *those* green has not proven that part, and DELIVER should not read it that way |

Next wave: DELIVER, slice 01 (Story #6045).

---

## Wave: DISTILL / [REF] Slice 02 — scenarios

Authored 2026-09-20, at the head of slice 02's own DELIVER, which is what the Scope of This Pass section
above said would happen. Story #6046.

**Driving port**: the React component tree, through Vitest and React Testing Library, rendering the real
column factory — the mechanism the ATDD infrastructure policy already records for frontend acceptance
tests. No shallow rendering and no component mocking.

**Test placement**: `components/Common/FeatureListDataGrid/columns.forecastedStart.test.tsx`, beside the
`columns.test.tsx` that covers the completion column. Same file-per-concern split the directory already
uses (`columns.dependsOn.test.tsx`, `columns.position.test.tsx`, `columns.warnings.test.tsx`).

**Surface**: `createForecastedStartColumn` in `columns.tsx`, a sibling of `createForecastsColumn` at
line 48. Wired into `FeaturesView.tsx` and the Delivery grid's `DeliverySection.tsx`, the two places
`createForecastsColumn` is used today.

### What the frontend binds to

Slice 01's wire contract, unchanged — this slice adds no backend field and needs none. If it turns out
to need one, slice 01 was incomplete and the change belongs there rather than here.

```jsonc
"startForecast": { "source": "Forecast" | "Observed" | "Unknown", "observedDate": "…", "percentiles": [ … ] },
"teamForecasts": [ { "teamId": 7, "startPercentiles": [ … ], "completionPercentiles": [ … ] } ]
```

Both are optional on the TypeScript model, for the same reason every other additive field on `IFeature`
is: a fixture built before they existed is still a valid fixture, and a client reading an older instance
must not crash. `teamForecasts` is carried on the model and read by nothing in this slice — the table
shows one forecast at four percentiles and no expander (D16), and the per-team breakdown is slice 06's.

### Scenarios

| # | Scenario | AC | Notes |
|---|---|---|---|
| 1 | A Feature nobody has started shows its four start percentiles | AC-2.1 | **walking skeleton** for this slice |
| 2 | A started Feature shows one date, marked observed, with no percentile beside it | AC-2.2, D5 | |
| 3 | The observed marking is legible without hovering | AC-2.2 | the brief's own wording: a reader scanning the column must not mistake an observed date for a P50 |
| 4 | A Feature no contributing team can be forecast for shows the column's existing empty state | AC-2.3 | the same `Cannot forecast` the completion column uses, not a new one |
| 5 | A Feature whose start is unknown but whose teams are all forecastable still says so | AC-2.3 | the `Unknown` source with no `teamsWithoutForecast` — reachable when a start row has no runs behind it |
| 6 | A payload with no `startForecast` at all renders rather than throwing | AC-2.3 | mirrors `columns.test.tsx`'s own "tolerates a backend payload that omits the field" |
| 7 | The column renders with no premium licence | AC-2.4 | core forecasting has never been gated (D8); asserted with the licence absent rather than assumed |
| 8 | The completion column is untouched — same cell, same content | AC-2.5 | |

**Error and edge coverage: 4 of 8** (4, 5, 6, 8). The column takes no user input, makes no call and has
no failure mode of its own; its edges are all shapes of "there is no answer", which is the thing this
Epic is most at risk of rendering as a confident date.

### What this slice cannot settle, and who has to

The brief names a **dogfood judgement**, not a test: reading the column in board order on the dev
instance and saying out loud whether the dates are *believable*. D6 predicts a not-started Feature will
show as starting today while others are in flight, and that is the design working as decided (ADR-202) —
but whether it reads as acceptable or as a bug is a product call, not one an acceptance test can make.
Brought to the maintainer with a screenshot rather than decided here.

Next: DELIVER, slice 02 (Story #6046).
