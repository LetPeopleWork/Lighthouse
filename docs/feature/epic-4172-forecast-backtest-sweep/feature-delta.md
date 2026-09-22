<!-- markdownlint-disable MD024 -->

# Feature Delta — epic-4172-forecast-backtest-sweep

**Feature**: **Forecast Reality Check** — a one-click, synchronous check that replays the Team's own
forecast against its own completed history at four sampling windows and four horizons, and answers in
one sentence that names a *region* of sound sampling windows rather than a winner, and says which
confidence levels actually held up.
**ADO**: Epic #4172, internal codename "The Full Monte" (state Planned, tag `Community`, priority 2).
**Origin**: no named customer. The business-primary objective is demonstrating the method; the
forecaster's own job is genuine but light, and the DIVERGE wave recorded that in the open rather than
inflating it. Source method: Nick Brown (ASOS), *The Full Monte*, ASOS Tech Blog, January 2024.
**Waves present**: DIVERGE (complete, peer-approved), DISCUSS (this).
**Density**: lean, `expansion_prompt: ask-intelligent`.

> **Four user decisions taken after DIVERGE override `recommendation.md` where they differ.** They are
> carried here as D1-D8 and are the substance of this wave. The largest is **U2**: Flux recommended
> scoring every cell against one fixed 85th percentile; the user chose instead to score several and show
> them side by side. That restores the axis Brown's study found carried the entire signal, and it
> changes the feature's headline. See D1-D4.

---

## Wave: DISCUSS / [REF] Prior Wave Consultation

Read on 2026-09-22 before any decision below.

| Source | State |
|---|---|
| `docs/product/` | ✓ exists — no migration gate |
| `docs/product/jobs.yaml` (7432 lines, 136 jobs) | ✓ paged, not read whole — lines 7340-7432 (the SSOT job), 303-382, 1243-1312, 1097-1146 |
| `docs/product/outcomes/registry.yaml` | ✓ present |
| `docs/product/architecture/brief.md` (872 KB) | ✓ searched only, never read whole |
| `docs/product/vision.md`, `docs/project-brief.md`, `docs/stakeholders.yaml` | ⊘ do not exist |
| `docs/feature/epic-4172-*/discover/` | ⊘ DISCOVER deliberately skipped — problem already evidenced in SSOT |
| `recommendation.md` (496 lines) | ✓ read whole, in three pages |
| `diverge/job-analysis.md` §6, §7, §8 | ✓ read — ODI statements quoted from §7 of this file, **never** from `review.yaml` |
| `diverge/competitive-research.md` | ✓ findings carried via `recommendation.md` §1 |
| `diverge/taste-evaluation.md` §6 | ✓ weighting sensitivity carried via `recommendation.md` §6 |
| `wave-decisions.md` | ✓ read whole |
| `docs/product/personas/delivery-forecaster.yaml`, `forecasting-prospect.yaml` | ✓ read whole |
| `docs/product/journeys/forecast-minimum-data-guard.yaml` | ✓ read — the shipped guard this composes with |

Code read directly, listed in the surface inventory below. Nothing in this document rests on an
inference about the codebase that was not checked against the tree on 2026-09-22.

---

## Wave: DISCUSS / [REF] Persona IDs

SSOT ids from `docs/product/personas/`. None is new to this feature.

| Persona | Role here |
|---|---|
| `delivery-forecaster` | **Primary.** Owns the conversation with leadership about how much the Team will deliver. Set `ThroughputHistory` once, probably never, and has no way to find out whether it was right. Presses the button, reads the sentence, decides whether to change the setting and which number to quote. |
| `forecasting-prospect` | **Secondary.** Does not yet use the product. Reads the same artifact — in the launch post, in the docs, or in a one-pager somebody pasted into a Slack channel — and judges the method by it. Recorded on the SSOT job rather than given a job of its own, per the DIVERGE decision. |
| `flow-coach` | **Explicitly not this persona.** Their jobs are diagnostic — stuck items, blocked duration, pace outliers. Forecast-model calibration is not among them. |
| `config-admin` | **Explicitly not this persona.** Owns the `ThroughputHistory` field, but owning a field is not knowing what to put in it. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

**One job. It already exists in SSOT and was written for this feature — it is traced to, not re-derived.**

- **`job-forecaster-check-the-forecast-against-what-happened`** (`delivery-forecaster`,
  `docs/product/jobs.yaml:7340-7432`) — When I am about to publish a forecast from a Team whose sampling
  configuration I set once and have never checked, I want evidence from that Team's own completed history
  about whether that configuration would have got the last few periods right, so I can either stand
  behind the number or change the configuration before anyone anchors on it.

All four user stories below trace to this one job (N:1). No new job is created. Two corrections are made
to the existing job by this wave, recorded under SSOT Updates.

### ODI outcome statements

Quoted verbatim from `diverge/job-analysis.md` §7. These are **reasoned estimates, not survey results** —
DISCOVER was skipped by design and no customer interviews exist. They must not be quoted downstream as
measured.

| # | Outcome statement | Imp. | Sat. | Score | Status |
|---|---|---|---|---|---|
| **O4** | Minimize the likelihood of ranking one sampling window above another when the comparison rests on periods that are not equivalent. | 8.2 | 1.5 | **14.9** | Under-served |
| **O1** | Minimize the time it takes to determine whether a Team's throughput sampling window produces forecasts that match what that Team actually delivered. | 7.8 | 2.4 | **13.2** | Under-served |
| **O2** | Minimize the likelihood of publishing a forecast whose sampling window has never been compared against a completed period. | 7.1 | 2.0 | **12.2** | Under-served |
| **O5** | Minimize the effort required to show a sceptical stakeholder that probabilistic forecasting held up on a Team's own history. | 6.9 | 2.2 | **11.6** | Under-served (marginal) |
| **O3** | Minimize the number of configuration attempts required before a Team's forecast settings match its observed delivery. | 6.4 | 3.1 | **9.7** | **Over-served — do not invest** |
| **O6** | Minimize the likelihood of reading a calibration result for a period whose history cannot support one. | 7.4 | 6.8 | **8.0** | **Over-served — do not invest** |

**O4 outranks O1**: staying honest about non-comparability scores higher than answering the question.
That is the evidence base for the three honesty requirements being hard acceptance criteria rather than
prose, and for the 30% HONESTY weight in the DIVERGE matrix.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from the tree on 2026-09-22, before any decision below was taken.

| # | Surface | What is actually there |
|---|---|---|
| S1 | `Lighthouse.Backend/API/ForecastController.cs:195-234` | `RunBacktest`. **`public ActionResult<BacktestResultDto> RunBacktest(...)` — already synchronous, no `async`/`await`.** One `forecastService.HowMany`, one `GetBlackoutAwareThroughputForTeam`, one `GetThroughputForTeam`, one `GetForecastThroughputStatus`, one `CreateForecastDtos(50, 70, 85, 95)`. Guard `[RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]`. |
| S2 | `ForecastController.cs:169-193` | `ValidateBacktestInput` — four date rules. Under D6 (today as the end anchor) three of the four are vacuous; only the 14-day minimum survives, and it survives as a property of the 2-week horizon rather than as input validation. |
| S3 | `API/DTO/BacktestResultDto.cs` | Four dates, `List<ForecastDto> Percentiles`, `ActualThroughput`, `FilterApplied`, `ExcludedSummary`. **All four percentiles are already returned by every run.** |
| S4 | `Services/Implementation/Forecast/ForecastDataSufficiencyPolicy.cs:7,9` | `public const int MinimumActiveDays = 5;` and `HasEnoughData(RunChartData throughput) => throughput.DaysWithThroughput >= MinimumActiveDays`. **This is the shipped bar C5 composes with. It is a pure function over a `RunChartData` and can be called per cell without modification.** |
| S5 | `Services/Implementation/TeamMetricsService.cs:110-118` | `GetForecastThroughputStatus` returns `status with { HasSufficientData = ForecastDataSufficiencyPolicy.HasEnoughData(status.Throughput) }` — but over the *Team's configured* window. Per-cell sufficiency means calling S4 on each cell's own `RunChartData`, not calling S5 sixteen times. |
| S6 | `Models/Team.cs:17` and `Team.GetThroughputSettings(DateOnly today)` | `public int ThroughputHistory { get; set; } = 30;`. `GetThroughputSettings` is already today-anchored — `today.AddDays(-(ThroughputHistory - 1))` to `today`. **The end-anchored shape D6 requires is the shape the product already uses.** |
| S7 | `Models/Team.cs` (whole file), `Models/WorkTrackingSystemOptionsOwner.cs:40` | **There is no percentile field on `Team`.** A search for `Percentile` in `Team.cs` returns zero matches. `ServiceLevelExpectationProbability` (default 0) lives on the base class but is a **cycle-time** SLE: its only consumer is `TeamMetricsService.GetSleRiskForTeam:359-397`, via `SleRiskCalculator.For(item.Age, team.ServiceLevelExpectationRange, cycleTimes)`. It governs how long one Work Item may take, not which percentile a How-Many forecast is quoted at. **This is the decisive finding for D4.** |
| S8 | `Lighthouse.Frontend/src/pages/Teams/Detail/TeamForecastView.tsx:438-460` | `<InputGroup title="Forecast Backtesting">` wrapping `<BacktestForecaster>` with six date/mode props. The new control goes inside this group, above the shipped pickers. |
| S9 | `pages/Teams/Detail/TeamDetail.tsx:402-430` | `QuickSettingsBar` → `ThroughputQuickSetting`, present on every Team tab, already writing `throughputHistory` with validation. |
| S10 | `Lighthouse.Frontend/package.json` | `@mui/x-charts` 9.0.1 is the only charts package. There is no `@mui/x-charts-pro`, so no Heatmap component exists. A sixteen-cell matrix needs no charting component regardless. |

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — Score all four confidence levels (50 / 70 / 85 / 95), not Brown's three

**This implements U2.** Flux recommended one fixed 85th. The user chose several, side by side. Given
that, the question is *which several*, and the answer is all four.

Four reasons, in order of weight:

1. **It is free, and dropping one costs code.** S1 shows `CreateForecastDtos(50, 70, 85, 95)` already
   runs on every backtest. Scoring three would mean filtering output — more code, to show less.
2. **The rest of the product shows four.** `ForecastPredictabilityScore` adds all four;
   `DeliveryWhenPercentile` takes a `params int[]` that every caller fills with four; the shipped
   backtest display renders four. A calibration artifact that scores three while every neighbouring
   surface shows four is an inconsistency a user would notice and nobody could explain.
3. **The 95th is where the nominal-rate lesson is legible.** A 95% forecast is supposed to be beaten
   about one time in twenty. Across four horizons it will usually be beaten zero times — and that is
   precisely how a user learns that "never beaten" means over-forecasting rather than excellence
   (§4.3). **The 95th is the teaching cell.** Dropping it would remove the clearest instance of the
   honesty requirement it exists to serve.
4. **Comparability to Brown is preserved, not traded away.** His published figures are for 50 / 70 / 85,
   and all three are present. The 95th is an addition, not a substitution. The launch post can quote
   his three and show our four, and that is honest.

### D2 — The confidence level is a *position*, not a fifth panel. Four panels stay four

**This is the main UX problem of the wave, and it dissolves rather than trades off.**

The temptation is to treat the confidence level as a fourth axis and plot it — which at 4 windows × 4
horizons × 4 levels gives 64 marks and an unreadable artifact. That temptation rests on a mistake: a
Monte Carlo How-Many forecast at 50/70/85/95 is **not four forecasts. It is one distribution, read at
four places.** Drawing it as four points throws away the thing that makes it readable.

So the small multiple is: **one panel per sampling window; one row per horizon; each row drawn as a band
running from the 95th to the 50th with the four levels marked along it; one mark showing where the
Team's actual completed count landed.**

```text
  Sampling window: 30 days
                     95    85    70    50
   8 weeks   ├──────┼─────┼─────┼─────┤        ● 42
   4 weeks   ├────┼────┼────┼────┤  ●  19
   2 weeks   not enough history in this window to check
   1 week    ├──┼──┼──┼──┤          ●  6
```

Where the mark falls in the band **is** the per-level verdict, read directly. A mark to the left of the
85th means the 85% forecast held. A mark to the right of the 50th means the 50% forecast was beaten.
There is no percentile selector, no fifth panel, no toggle. **The confidence dimension costs zero panels
and zero controls.**

The nominal-rate roll-up (§4.3) is then four lines of text below the panels, not a visual:

```text
  Across the 14 checks that could be run:
    50%   beaten in 9   (about 7 expected)    about right
    70%   beaten in 5   (about 4 expected)    about right
    85%   beaten in 1   (about 2 expected)    about right
    95%   beaten in 0   (about 1 expected)    never beaten — this is over-forecasting, not excellence
```

### D3 — The denominator states two numbers and discloses the correlation between them

§4.2 required the artifact to print its own denominator. With D1 the honest denominator is no longer one
number, and saying "64 configurations were checked" would be dishonest in a *new* direction: the four
levels of one cell are read off **the same simulation** and are perfectly correlated by construction.

The permanent copy therefore states both numbers and their relationship:

> *"16 forecast runs were checked, each read at 4 confidence levels — 64 scores in all. The four levels
> of a single run come from the same simulation, so they are not independent of one another. And each run
> covers a different stretch of real time — every one ends today and reaches back by its own length — so
> they are not repeated trials of one experiment and should not be ranked against each other."*

This is a **better** disclosure than the one §4.2 asked for, and it exists only because U2 forced the
question. It is recorded as an improvement the override produced, not as a concession to it.

### D4 — Two verdicts, one button. The sampling window is a setting; the confidence level is not

**This resolves the coherence problem U2 raised, and it was resolved by searching the codebase rather
than by assuming.**

The question was: *if the signal is on the confidence-level axis, is there a Team setting to act on?*

**There is not.** S7 records the search. `Team` carries no percentile field.
`ServiceLevelExpectationProbability` exists on the base class but is a cycle-time SLE consumed only by
`GetSleRiskForTeam` — it bounds how long one Work Item may take, and has nothing to do with which
percentile a How-Many forecast is quoted at. Forecast percentiles are the fixed set 50/70/85/95,
hard-coded at every call site. **The product shows all four and lets the human choose which to say out
loud.**

That is not a gap to be filled here. It is the honest shape of the feature, and it is what the artifact
must say:

> *The sampling window is a setting. It has a control, and this check found it barely matters in this
> range.*
> *The confidence level is not a setting — it is which of the four numbers you say out loud in the room.
> This check found it matters enormously. There is no button for it, because there is nothing to press.*

So: **two verdicts in one sentence, one Apply button, and permanent copy saying why there is only one.**
Far from being an incoherence, this is the strongest thing the feature says — it is a *behaviour*
recommendation, which is exactly what a Community feature meant to convince people of a method should
produce.

**Named residual, escalated rather than resolved here**: *should Lighthouse gain a per-Team default
forecast confidence level?* That is a real product question with real consequences across every forecast
surface, and this feature's evidence is exactly what would justify opening it. **It gets its own ADO
item. It is not folded into this Epic, and no setting is invented in DISCUSS.**

### D5 — Apply appears only when the current window is outside the sound region

Corollary of §4.1 and of O3 scoring 9.7 (over-served). When the check returns the modal answer — every
window behaves alike, yours is inside the region — there is nothing to apply, and rendering a disabled
or no-op button would contradict the sentence beside it. In that case no control appears and the sentence
says the current setting is fine.

The button is therefore present only in the minority case where the window genuinely is wrong, which is
the only case where it matters. That is also the answer to "you showed the axis that matters and gave a
button for the one that does not": the button is for the axis that has a setting, and it only shows up
when that setting is actually wrong.

### D6 — Today is the end anchor. No date picker anywhere

**U4, settled before this wave.** Every cell ENDS today and reaches backward by its horizon. The 8-week
cell scores `[today-8w, today]`, with its history window immediately before it at
`[today-8w-W, today-8w]`. S6 shows this is the shape `Team.GetThroughputSettings` already uses.

**Load-bearing consequence**: each horizon scores a *different* stretch of real time, so the cells are
not repeated trials of one experiment and must never be ranked against one another. This is honesty
requirement §4.2 and it is why D3's copy is permanent rather than a tooltip.

Practical consequence: the request body carries no dates, which is what makes one click viable and what
makes three of S2's four validation rules vacuous.

### D7 — The three-way verdict is kept, and the departure from the source is owned out loud

**U3, confirmed.** Under / over / within-range is kept. Brown scored one-sided — over-delivery counted as
correct. Ours is arguably more honest (a forecast never beaten is over-forecasting, not excellence) and
matches weather verification's symmetric treatment of conditional bias.

**It must never be described as "what Nick Brown did."** Wherever the source method is credited — the
UI, the docs, the launch post, the exported one-pager — the departure is stated in the same place.
AC-1.8 and AC-4.4 make this testable.

### D8 — No Report abstraction. ADR-207 is a DESIGN deliverable

**U1, confirmed as Flux recommended.** Nothing is stored: no entity, no table, no `UpdateType` member,
no queue work, no notification seam. The result is a response, not a record.

ADR-207 (next free number — 206 ADRs exist, highest `adr-206`) records the position, the accepted
consequence ("has this Team ever been checked?" is unanswerable, and emailing needs something durable
that does not exist), the named revisit trigger (**the second Report kind**, not a date), the six
questions the eventual Report ADR must answer, the ADR-195 single-lane measurement behind the runner
question, and the forward-compatibility constraint that the result shape must not hard-code one Team.
The parked constellation (#1822, #4753, #4754, #4755, #4155, #4080) is named as future context only.

**Luna does not write this ADR.** It is flagged here as a DESIGN-wave deliverable.

### D9 — Per-cell sufficiency reuses the shipped bar unchanged. No second bar

**C5.** Each cell's own history window is checked by `ForecastDataSufficiencyPolicy.HasEnoughData`
(S4, `MinimumActiveDays = 5`) called on that cell's own `RunChartData`. `MinimumActiveDays` is not
changed, no new threshold is introduced, and nothing in `ForecastDataSufficiencyPolicy` is touched.

Because each cell carries its own history window, **sufficiency varies within one report** — one artifact
can hold answerable and unanswerable cells at once. ADR-194 governs the rendering: on this product's
charts a blank region already means "too little history", so a calm result and an unevaluable one must
never render alike, and "leave it empty" is the option that ADR already ruled out. An unevaluable row
says so in words where the band would be.

### D10 — The name

| Layer | Name |
|---|---|
| User-facing | **Forecast Reality Check**, shortening to **"Reality Check"** in-app |
| Placement label | inside the existing `InputGroup title="Forecast Backtesting"` (S8) |
| Internal codename | **"The Full Monte"** — kept, in the commit scope, the ADO Epic title, and the launch post *with attribution to Nick Brown* |
| Engineering slug | `epic-4172-forecast-backtest-sweep` — unchanged |

**C6 extended**: "throughput" is itself user-renameable under Settings → Terminology, so it cannot appear
in a feature name — it would render as the user's own word. Safe vocabulary: forecast, calibration,
hindsight, replay, accuracy, check, reality, sampling, history, percentile.

### D11 — Re-sliced. Every slice carries user-visible value

**`recommendation.md` §5.5 is corrected here, as Flux invited ("a starting shape for Luna, not a
decision").** Its slice 01 was the endpoint with no UI and its slice 02 the verdict logic with still no
UI — two consecutive slices of `@infrastructure` stories with no release value, which is a structural
failure this workflow hard-gates on.

What Flux was protecting is preserved in full:

- **(a) The R-1 timing measurement still happens first**, before any UI is written. It is the first task
  of slice 01 and AC-1.1 gates the rest of the slice on it.
- **(b) The honesty requirements are still built early.** All three land in slice 01 — they are the
  sentence, and the sentence is the first thing that ships. They cannot be dropped under UI pressure
  because they *are* the UI.

The endpoint and the verdict logic become precursor commits inside slice 01 rather than slices of their
own. Four slices result, every one of them user-visible. See the story map.

### D12 — No CLI / MCP client exposure in this Epic

Answered explicitly rather than skipped; see the project checklist below.

### D13 — R-6 is out of scope and gets its own ADO item

`Team.ThroughputHistory` defaults to **30** (`Team.cs:17`) while `CreateTeamWizard.tsx:35` and
`EditTeam.tsx:80` both seed **90**. Independently verified. **Do not fold into this Epic.** It is
recorded here because it was found while researching this feature, and because it is incidentally an
argument *for* the feature: nobody currently knows which value is right.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized. 4 stories, 2 modules, ~20h (≈3 days).** Assessed before journey visualisation, per
the early gate.

Oversized signals checked:

| Signal | Present |
|---|---|
| More than 10 user stories | No — 4 |
| More than 3 bounded contexts or modules | No — 2 (backend forecasting/API; Team Forecast UI). Slice 04's export is client-side within the second |
| Walking skeleton needs more than 5 integration points | N/A — no walking skeleton (brownfield, Strategy B) |
| Estimated effort over 2 weeks | No — ~20h |
| Multiple independent user outcomes that could ship separately | One outcome, one job. Slice 04 is severable but serves the same job through a secondary persona |

No split required. Slice 04 is severable on its own merits, not because the feature is oversized.

---

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

**Strategy B — extend an existing end-to-end path. No walking skeleton is built.** Decision 2, confirmed
against the tree: the endpoint pattern (S1), the forecast engine, the blackout-aware working-day counting,
the data-sufficiency guard (S4), the Team Forecast surface (S8) and the settings control that applies the
answer (S9) are all shipped. Slice 01 runs a second kind of request down a path that already exists.

Nothing in this feature uses a configurable or environment-switching strategy, so the strategy-D
expansion trigger does not fire.

---

## Wave: DISCUSS / [REF] Driving Ports

| Port | Surface | Slice |
|---|---|---|
| HTTP | `POST /api/latest/forecast/reality-check/{teamId}` and `POST /api/v1/forecast/reality-check/{teamId}` | 01 |
| UI | Team → Forecast tab → "Forecast Backtesting" group → the check button and its verdict sentence | 01 |
| UI | the same group → "Show the evidence" → four small-multiple panels and the nominal-rate lines | 02 |
| UI | Team detail header → `ThroughputQuickSetting`, opened pre-filled | 03 |
| Clipboard | "Copy as Markdown" from the verdict card | 04 |

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — One sentence that says whether the configuration behind my forecasts is sound

**Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Persona**: `delivery-forecaster`
**Slice**: 01

As a delivery forecaster about to publish a number leadership will hold me to, I want one press to tell
me whether the sampling window behind every forecast this Team produces would have got the last few
periods right — and which of the four confidence levels actually held up — so that I can stand behind
tomorrow's forecast for a reason rather than out of habit.

#### Elevator Pitch

Before: the Team's Forecast tab can back-test exactly one hand-typed date pair at a time, and nothing
anywhere in the product says whether the sampling window driving every forecast this Team publishes is
sound. Nobody runs it, because you have to already suspect the answer to know which dates to type.

After: press **Run reality check** in the Forecast Backtesting group and within seconds one sentence
comes back — *"Anything between 30 and 90 days would have behaved about the same for Ocean Explorer, and
your current 30 is inside that range. At the 85% level the forecast held in 3 of the 4 checks; at 50% it
held in 1 of 4. Two of the sixteen checks could not run: their 14-day sampling windows hold fewer than
5 days with completed Work Items."*

Decision enabled: whether to publish tomorrow's forecast as configured or change the window first — and,
independently, which of the four numbers to say out loud in the room.

#### Domain Examples

##### 1 — Happy path, and the modal case: the null result

Maria Santos runs delivery for **Ocean Explorer**, a Team with fourteen months of Work Items and
`ThroughputHistory` left at the entity default of 30 days. She presses the button on a Tuesday. All
sixteen cells evaluate. Every sampling window behaves alike: the region is *all of them*. The sentence
reads *"Anything between 14 and 90 days would have behaved about the same for Ocean Explorer. Your
current 30 is inside that range — this setting is fine."* No Apply control appears (D5). The second
clause reads *"At 85% the forecast held in 4 of 4 checks; at 95% it held in 4 of 4 and was never beaten,
which is over-forecasting rather than excellence; at 50% it held in 1 of 4."* Maria keeps her setting and
starts quoting the 85th instead of the 70th.

##### 2 — Edge case: sufficiency varies inside one report

**Coastal Survey** was formed eleven weeks ago and closes work in bursts. Its 90-day and 60-day cells
evaluate; its 14-day cells hold 3 and 4 distinct days with a completed Work Item respectively, below
`MinimumActiveDays = 5`. Four of the sixteen cells are unevaluable. The sentence names them and their
reason, every count is taken over the twelve that ran, and the four rows in the evidence view say so in
words where the band would be — never blank (D9, ADR-194).

##### 3 — Error/boundary: the window really is wrong

**Deep Current** has `ThroughputHistory` at 14 days after somebody set it during a spike three quarters
ago. The 14-day window over-forecast in three of its four checks; 30, 60 and 90 all behaved alike. The
sentence reads *"Anything between 30 and 90 days would have behaved about the same for Deep Current. Your
current 14 is not inside that range — it over-forecast in 3 of its 4 checks."* This is the minority case,
and it is the one where slice 03's Apply control appears.

#### UAT Scenarios (BDD)

```gherkin
Scenario: The check answers in one sentence without asking for a date
  Given Maria Santos is on Ocean Explorer's Forecast tab
  And Ocean Explorer's sampling window is set to 30 days
  When Maria presses "Run reality check"
  Then a verdict sentence appears in the Forecast Backtesting group
  And she was asked for no dates at any point

Scenario: Every window behaving alike is a real answer, not an absence
  Given Ocean Explorer's history makes all four sampling windows behave alike
  When Maria runs the reality check
  Then the verdict names the whole range as acceptable and says her current setting is fine
  And no single sampling window is described as best or recommended

Scenario: The artifact states what it checked and why the checks cannot be ranked
  Given Maria has run the reality check on Ocean Explorer
  When she reads the result
  Then the count of runs, the count of scores, and the fact that a run's four levels share one simulation are on screen
  And the statement that each run covers a different stretch of real time ending today is on screen
  And neither statement is hidden behind a tooltip or a disclosure

Scenario: A confidence level that was never beaten is called over-forecasting
  Given Ocean Explorer's 95% forecast was not beaten in any of the evaluable checks
  When Maria reads the verdict
  Then the 95% level is described as over-forecasting rather than as excellent
  And the nominal rate it was expected to be beaten at is stated alongside it

Scenario: A period whose own history is too thin is excluded and named
  Given Coastal Survey's 14-day sampling windows hold fewer than 5 days with a completed Work Item
  When Maria runs the reality check on Coastal Survey
  Then those checks are reported as unable to run, with that reason
  And they are excluded from every count in the verdict

Scenario: Reading the check needs read rights on the Team and nothing more
  Given Tom Becker can read Ocean Explorer but cannot change its settings
  When Tom runs the reality check
  Then he sees the verdict
```

#### Acceptance Criteria

- **AC-1.1 (R-1, measurement gate — runs before any other work in this slice)** — a sixteen-run reality
  check on a Team holding at least twelve months of Work Items completes on the development instance with
  a **median at most 5 seconds and a maximum at most 10 seconds across twelve samples**, alongside the
  measured median of a single shipped `POST /api/latest/forecast/backtest/{teamId}` on the same Team as a
  baseline. Both numbers are written into the slice brief. **If the budget is missed, the slice stops and
  the fallback (`UpdateQueueService`, with the ADR-195 single-lane caveat) is re-scoped before any UI is
  written.**
- **AC-1.2** — `POST /api/latest/forecast/reality-check/{teamId}`, and the `/api/v1/…` twin, accept a body
  of at most `{ "applyFilterOverride": true | false | null }` and return the verdict, the denominator, the
  confidence levels used, and sixteen cells each carrying its real start and end date, its four forecast
  values, the Team's actual completed count, a coverage state per confidence level, and a sufficiency
  state. **The body accepts no dates**, and the response carries no field naming a single winning
  sampling window.
- **AC-1.3 (RBAC)** — the endpoint is guarded by
  `[RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]`, identical to the shipped
  backtest. A user with Team read but not Team write receives the full result. No new
  `RbacGuardRequirement` member is introduced and no new permission is added.
- **AC-1.4 (honesty §4.1 — region, never a winner)** — the verdict names a *range* of sampling windows
  that behaved alike and states whether the Team's current setting is inside it. For a Team where all four
  windows behave alike, the sentence reads that the current setting is fine. No response field and no
  rendered string names one window as best.
- **AC-1.5 (honesty §4.2 — denominator and non-comparability, per D3)** — permanently on screen, never
  behind a tooltip or a disclosure: the number of runs, the number of scores, the statement that the four
  levels of one run come from the same simulation and are therefore not independent, and the statement
  that each run covers a different stretch of real time ending today and must not be ranked against the
  others.
- **AC-1.6 (honesty §4.3 — coverage against a nominal rate, per D1)** — the verdict reports, for each of
  the four confidence levels, how many of the evaluable checks the forecast held in, and prints the level
  as a number. A level not beaten in any evaluable check is described as over-forecasting rather than as
  excellent.
- **AC-1.7 (C5 / D9)** — a cell whose own history window holds fewer than five distinct days with at
  least one completed Work Item is marked unevaluable by `ForecastDataSufficiencyPolicy.HasEnoughData`
  called unchanged, is excluded from every count, and is named in the sentence with its reason.
  `MinimumActiveDays` is not changed and no second threshold is introduced.
- **AC-1.8 (D7)** — no response field, UI string, doc page or launch-post sentence attributes the
  three-way under / over / within-range verdict to Nick Brown. Wherever the source method is credited, the
  departure is stated in the same place.

> **Sizing note, stated rather than gamed**: US-01 carries 8 acceptance criteria against a 3-7 band.
> AC-1.1 is a measurement gate rather than a behaviour, so the story is seven behaviours and a probe.
> The precedent is `epic-6033` US-01, which carried ten and passed DoR. The story remains demonstrable in
> one session and estimated at one day.

#### Technical Notes

- Reuses unchanged: `forecastService.HowMany`, `teamMetricsService.GetBlackoutAwareThroughputForTeam`,
  `GetThroughputForTeam`, `blackoutPeriodService.GetEffectiveBlackoutDays` / `CountWorkingDays`,
  `GetForecastThroughputStatus`, `CreateForecastDtos`, `ForecastDataSufficiencyPolicy.HasEnoughData`.
  **Nothing in the forecast engine changes.**
- `BacktestInputDto`'s validation (S2) is not reused as written; D6 makes three of its four rules vacuous.
- No entity, no migration, no `UpdateType` member (D8).
- Route style: kebab-case is this codebase's convention (`my-summary`, `group-mappings`, `system-admins`).
- Forward-compatibility (ADR-207): the result shape must not hard-code one Team — "check every Team at
  once" is a plausible next unit of work and nothing here should make it expensive.

#### Dependencies

`ForecastController` two-route pattern (shipped, S1) · `ForecastDataSufficiencyPolicy` (shipped, S4) ·
`TeamForecastView` `InputGroup` (shipped, S8). All confirmed present. AC-1.1 is the only open unknown
and it is resolved inside this slice.

---

### US-02 — The evidence, when the sentence is not enough

**Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Persona**: `delivery-forecaster` (secondary: `forecasting-prospect`)
**Slice**: 02

As a forecaster who is about to repeat this verdict to someone who will push back, I want to see the
sixteen checks themselves — each one's forecast range with the Team's actual marked against it — so that
I am repeating something I have looked at rather than something I was told.

#### Elevator Pitch

Before: the verdict is a sentence you either believe or you do not, and the sixteen checks behind it exist
only inside the response.

After: click **Show the evidence** under the verdict and four panels appear, one per sampling window, each
with a row per horizon drawn as a forecast band with a mark showing where the Team's actual landed — plus
four lines saying how often each confidence level was beaten and how often it should have been.

Decision enabled: whether the sentence is solid enough to repeat to a stakeholder — and, on seeing a 95%
band never beaten across sixteen checks, that "never beaten" is a symptom rather than a score.

#### Domain Examples

##### 1 — Happy path

Maria expands the evidence on Ocean Explorer. Four panels: 14, 30, 60, 90 days. In the 30-day panel the
8-week row's mark sits between the 70th and the 85th; the 4-week row's sits between the 85th and the 95th.
She can see without reading a number that the 85th has been holding and the 50th has not. The nominal-rate
lines below confirm it in four lines.

##### 2 — Edge case: an unevaluable row inside an otherwise healthy panel

On Coastal Survey, the 14-day panel's 2-week row cannot be drawn. Where the band would be, the row reads
*"not enough history in this window — 3 days with completed Work Items, 5 needed"*. It is visually
distinct from every evaluable outcome and is never blank (ADR-194).

##### 3 — Boundary: the whole panel is unevaluable

A Team created six weeks ago has no evaluable cell at all in its 90-day panel. The panel renders with all
four rows carrying their reason, rather than the panel being dropped — a missing panel would be read as
"this window was fine".

#### UAT Scenarios (BDD)

```gherkin
Scenario: The evidence shows one panel per sampling window and no more
  Given Maria has run the reality check on Ocean Explorer
  When she expands the evidence
  Then she sees four panels, one for each sampling window checked
  And she is offered no control for choosing a confidence level

Scenario: Where the actual landed is what says which levels held
  Given the 30-day panel's 8-week check forecast between 31 and 48 items and Ocean Explorer completed 42
  When Maria reads that row
  Then the forecast is drawn as a band with its four confidence levels marked
  And the actual of 42 is marked at its position within that band

Scenario: A check that could not run says so where the picture would be
  Given Coastal Survey's 14-day, 2-week check holds 3 days with completed Work Items
  When Maria expands the evidence for Coastal Survey
  Then that row states it could not be checked and why
  And it does not render as an empty or a calm result

Scenario: Each confidence level reports how often it was beaten against how often it should have been
  Given twelve of Coastal Survey's sixteen checks could be run
  When Maria reads the lines below the panels
  Then each of the four confidence levels reports how many of the twelve it was beaten in
  And each states the number of times its nominal rate would expect

Scenario: The non-comparability statement is visible whether or not the evidence is expanded
  Given Maria has run the reality check and has not expanded the evidence
  When she reads the collapsed result
  Then the denominator and non-comparability statements are on screen
```

#### Acceptance Criteria

- **AC-2.1 (D2)** — expanding the evidence reveals exactly four panels, one per sampling window, titled by
  window length. No per-confidence-level panel is added and no confidence-level control is rendered.
- **AC-2.2 (D2)** — each panel carries one row per horizon. The row draws the forecast as a band spanning
  the four confidence levels, with a single mark at the Team's actual completed count. The mark's position
  within the band is what reports which levels held.
- **AC-2.3 (ADR-194 / D9)** — an unevaluable row renders its reason in words where the band would be. It is
  visually distinct from every evaluable outcome and is never left blank. A panel whose rows are all
  unevaluable still renders, rather than being omitted.
- **AC-2.4 (honesty §4.3)** — below the panels, one line per confidence level: the count of evaluable
  checks it was beaten in, the count its nominal rate expects, and a plain reading of the two together.
- **AC-2.5** — the AC-1.5 denominator and non-comparability copy is on screen whether or not the evidence
  is expanded.
- **AC-2.6** — no charting package is added. The panels build on `@mui/x-charts` 9.0.1 or on plain layout.
  `@mui/x-charts-pro` is not introduced, and no heatmap or ranked-list rendering is used.

#### Technical Notes

Presentation only — consumes the slice 01 response and adds no computation. ADR-194 is the governing
precedent for AC-2.3 and is not amended. The rendering is deliberately not a matrix: a matrix is a
coordinate system and D6 says these cells are not one.

#### Dependencies

Slice 01 (the response shape). No external dependency.

---

### US-03 — Change the setting where I am standing, or be told there is nothing to change

**Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Persona**: `delivery-forecaster`
**Slice**: 03

As a forecaster looking at evidence that my sampling window is outside the sound range, I want to change
it from here in one press, so that the setting actually gets changed at the moment I have the reason in
front of me rather than at a settings visit I never make.

#### Elevator Pitch

Before: the verdict names a sound range, and acting on it means remembering the number, leaving the
Forecast tab, finding the Team header control, and typing it.

After: when the current window falls outside the sound range, an **Apply 60 days** control appears beside
the verdict; pressing it opens the Team header's throughput quick setting pre-filled with 60, and saving
writes it through the control that already does that job. When the window is already inside the range, no
control appears and the sentence says the setting is fine.

Decision enabled: change the setting now, in one press, while the evidence is on screen — and, separately,
understand from the copy why the confidence level that carries the real signal has no button.

#### Domain Examples

##### 1 — Happy path: the window is wrong and gets fixed

Deep Current sits at 14 days, outside the 30-90 sound range. **Apply 60 days** appears. Maria presses it;
the header quick setting opens pre-filled with 60; she saves; `throughputHistory` is written through the
shipped control's own validation. Nothing new writes to the Team.

##### 2 — The modal case: nothing to apply

Ocean Explorer's current 30 is inside the sound range. No control is rendered. The sentence reads *"Your
current 30 is inside that range — this setting is fine."* Maria's takeaway is a behaviour change, not a
settings change: start quoting the 85th.

##### 3 — Boundary: read-only user

Tom Becker can read Ocean Explorer but not change it. He sees the verdict and the full evidence. He does
not see the Apply control. The gating comes from `useRbac()`.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Applying the recommendation opens the control that already writes it
  Given Deep Current's sampling window of 14 days is outside the sound range of 30 to 90
  And Maria Santos can change Deep Current's settings
  When Maria presses the apply control beside the verdict
  Then the Team header's throughput quick setting opens pre-filled with the recommended value
  And nothing is written until Maria saves it there

Scenario: A sound setting offers nothing to apply
  Given Ocean Explorer's current sampling window of 30 days is inside the sound range
  When Maria reads the verdict
  Then no apply control is offered
  And the verdict states that her current setting is fine

Scenario: The artifact says why the confidence level has no button
  Given Maria has run the reality check on any Team
  When she reads the result
  Then it states that the sampling window is a setting and the confidence level is a choice of which number to quote
  And it states that there is therefore no control for the confidence level

Scenario: Someone who cannot change the Team is not offered the control
  Given Tom Becker can read Deep Current but cannot change its settings
  And Deep Current's window is outside the sound range
  When Tom reads the verdict
  Then he sees the full verdict and evidence
  And no apply control is offered to him
```

#### Acceptance Criteria

- **AC-3.1 (D5)** — an apply control appears beside the verdict only when the Team's current
  `ThroughputHistory` falls outside the sound range, and it names the value it would set.
- **AC-3.2** — pressing it opens the shipped `ThroughputQuickSetting` in the Team detail header,
  pre-filled with that value. The write occurs only on the user's save, through that control's existing
  validation and existing endpoint. **No new write endpoint and no new write path is added.**
- **AC-3.3 (D5)** — when the current window is inside the sound range, no apply control is rendered and
  the verdict states that the current setting is fine.
- **AC-3.4 (D4)** — permanent copy states that the sampling window is a setting with a control, that the
  confidence level is not a setting but a choice of which number to quote, and that there is therefore no
  control for it.
- **AC-3.5 (RBAC)** — a user with Team read but not Team write sees the verdict and the evidence and is
  not offered the apply control. The gating derives from the `useRbac()` hook; **no component fetches
  `/api/latest/authorization/my-summary` directly.**

#### Technical Notes

`ThroughputQuickSetting` (S9) is in `QuickSettingsBar` in the Team detail header and is present on every
Team tab, so no navigation is required. The write path is `canUpdateTeamData`, already in place. This is a
pre-filled open of a shipped control, not new machinery — which is the only reason Apply is in the MVP at
all, given O3 scored 9.7 (over-served).

#### Dependencies

Slice 01 (the recommended value and the region). `ThroughputQuickSetting` (shipped, S9).

---

### US-04 — The answer travels to the person who asked the question

**Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Persona**: `forecasting-prospect` (primary for this story; `delivery-forecaster` produces it)
**Slice**: 04 — **severable**

As a forecaster who was asked "how do you know this is right?" by someone who is not in the room, I want
to paste the whole check into a message, so that the answer is something they can check rather than
something they have to take from me.

#### Elevator Pitch

Before: nothing about the check leaves the browser. A sceptic has to be sitting at the screen, and a
prospect who has never used the product cannot see it at all.

After: press **Copy as Markdown** on the verdict card and the clipboard holds a one-pager — the verdict
sentence, the denominator and non-comparability paragraph, the sixteen rows with their real date spans and
outcomes, the unevaluable rows with their reasons, and the nominal-rate table — ready to paste into Slack,
a Confluence page, or an email.

Decision enabled: whether the person who asked gets an answer they can interrogate, or a claim they have to
trust. For a prospect, whether the method is worth their evaluation at all.

#### Domain Examples

##### 1 — Happy path

Maria pastes the one-pager into the delivery channel under her forecast. Her director reads the sentence,
the sixteen date spans and the line saying they are not repeated trials, and stops asking.

##### 2 — Edge case: terminology is the reader's, not ours

A customer has renamed "Work Item" to "Ticket" and "throughput" to "delivery rate". The pasted one-pager
reads *"fewer than 5 days with completed Tickets"* and never contains the words "Work Item" or
"throughput" except as that instance's configured terms.

##### 3 — Boundary: a report that is mostly unevaluable

Coastal Survey's one-pager has four unevaluable rows. They are in the table with their reasons, not
silently dropped — an export that omits them would mislead exactly the audience it exists for.

#### UAT Scenarios (BDD)

```gherkin
Scenario: The whole check pastes into a message
  Given Maria has run the reality check on Ocean Explorer
  When she presses the copy control on the verdict card
  Then the clipboard holds the verdict, the non-comparability statement, all sixteen rows with their date spans, and the nominal-rate table

Scenario: Checks that could not run travel with the ones that could
  Given four of Coastal Survey's sixteen checks could not be run
  When Maria copies the one-pager
  Then those four appear in the table with the reason they could not run

Scenario: The one-pager speaks the reader's vocabulary
  Given the instance has renamed Work Item to Ticket
  When Maria copies the one-pager
  Then it says Ticket wherever it refers to a work item

Scenario: The one-pager credits the source method and names where we departed from it
  Given Maria has copied the one-pager
  When she reads its footer
  Then it credits Nick Brown's method and states that the three-way verdict is our departure from it
```

#### Acceptance Criteria

- **AC-4.1** — the copy control produces a Markdown one-pager holding the verdict sentence, the AC-1.5
  denominator and non-comparability paragraph, all sixteen rows with their real date spans and outcomes,
  every unevaluable row with its reason, and the nominal-rate table.
- **AC-4.2 (ADR-172 / ADR-162)** — the one-pager is built in the client. No server-side document renderer
  is added and no new endpoint is introduced.
- **AC-4.3 (C6)** — every configurable term renders from the instance's terminology. The words "Epic",
  "Initiative" and "Story" do not appear. "Throughput" appears only as that instance's configured term.
- **AC-4.4 (D7)** — the one-pager credits Nick Brown's method and states, in the same paragraph, that the
  three-way under / over / within-range verdict is this product's departure from it.

#### Technical Notes

The export precedent is ADR-172 (a Delivery exports one settled table the caller builds) and ADR-162
(export header block as a generic toolbar input). Both are client-side, which is what makes this cheap and
what makes the durable emailable Report expensive (D8).

#### Dependencies

Slices 01 and 02 (the verdict and the cell detail). Severable — if dropped, the feature still ships.

---

## Wave: DISCUSS / [REF] Story Map and Slices

**Backbone**: press it → read the sentence → look at the evidence → act on it → send it to someone else.

| Slice | Story | Ships | Estimate |
|---|---|---|---|
| 01 | US-01 | R-1 measurement, the endpoint, the verdict logic with all three honesty requirements, and the sentence and button on the Forecast tab | ~1h probe + ~7h |
| 02 | US-02 | Four small-multiple panels, the unevaluable-row state, the nominal-rate lines | ~6h |
| 03 | US-03 | Apply, conditional on the window being outside the region, plus the "why no button for the level" copy | ~3h |
| 04 | US-04 | The Markdown one-pager — **severable** | ~4h |

**Walking skeleton**: none. Strategy B, brownfield — see the strategy section.

### Priority Rationale

1. **Slice 01 first — it carries the only decisive unknown and all three honesty requirements.** AC-1.1
   runs before anything else in the slice; if a sixteen-run sweep does not fit a request, every downstream
   decision changes and finding out costs an hour. The three honesty requirements land here because they
   *are* the sentence — building them into the first user-visible thing is what makes them impossible to
   drop later. This is what `recommendation.md` §5.5 was protecting, preserved without the
   `@infrastructure`-only slices.
2. **Slice 02 second — it is the mitigation that earns the recommendation's weakest score.** M1 scores 3/5
   on HONESTY, the highest-weighted criterion, and the evidence view is what moves it. Second, not first,
   because a verdict nobody can act on is still worth more than evidence nobody has a verdict for; but not
   later than second, because deferring it is exactly how the recommendation degrades into the "black box"
   exposure §3 names.
3. **Slice 03 third — it is real but it optimises an already-cheap step.** O3 scored 9.7, over-served.
   Changing `ThroughputHistory` is one number in a control the user already owns; Apply ships only because
   `ThroughputQuickSetting` already exists and the cost is near zero. It is also the slice most likely to
   be cut under pressure without harm.
4. **Slice 04 last and severable — it serves the declared secondary objective.** C1 makes conversion an
   honest secondary, the Epic is tagged Community for that reason, and this is the whole of the marketing
   surface. Last because a one-pager of a verdict that has not been dogfooded is a liability rather than an
   asset.

### Carpaccio taste tests

- **Four or more new components in one slice?** Slice 01 adds one endpoint, one verdict calculation and
  one card. Slice 02 adds one panel component. Pass.
- **Every slice depending on a new abstraction?** Slices 02-04 all consume slice 01's response, which is
  why it ships first. Nothing depends on an abstraction built speculatively. Pass.
- **Does any slice disprove a pre-commitment?** Slice 01 disproves the synchronous premise if AC-1.1 fails
  — the whole recommendation rests on it. Slice 02 disproves D2 if four bands per panel turn out to be
  unreadable at real data. Pass.
- **Synthetic data only?** Slices 01 and 02 are dogfooded against this project's own Lighthouse instance
  with real history, per `recommendation.md` §5.5. Pass.
- **Two slices identical but for scale?** 02 and 04 both render the sixteen cells, but one is on screen and
  one is a portable document for a different persona. Not merged, deliberately. Pass.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Any Report entity, table, migration, `UpdateType` member, queue work or notification seam.** Declined,
  not deferred (D8). ADR-207 records why and what the second Report must decide.
- **A per-Team default forecast confidence level.** **Escalated, not declined** (D4). Real product
  question, its own ADO item, and this feature's evidence is what would justify opening it. No setting is
  invented here.
- **The `ThroughputHistory` 30-vs-90 default disagreement** (D13). Its own ADO item.
- **Rolling-origin evaluation** (R-7). C4/D6 is not reopened. Recorded as the strongest candidate for a
  later slice if the honesty mitigations prove insufficient in use.
- **Scheduling, continuous checking or auto-adjustment** (C2). On demand only. The auto-adjusting future
  is deliberately not foreclosed but is not built.
- **The forecast-throughput filter as a sweep dimension** (C3). It is a config option of the run,
  `applyFilterOverride`, and nothing else.
- **A second data-sufficiency bar** (C5 / D9). The shipped `MinimumActiveDays = 5` is composed with,
  unchanged.
- **Any change to the forecast engine.** AC-1.2's reuse list asserts this.
- **CLI or MCP client exposure** (D12). See the checklist below for the explicit answer and the
  precondition that would reverse it.
- **ADR-127's team-settings advisory channel.** Story #5612 deleted it; `ValidationAdvisory.tsx` is absent
  from the frontend and a later rung was built *"rather than reviving them"*. **ADR-127 carries no note
  saying so — do not reach for that mechanism.** A status correction on ADR-127 is worth its own item,
  independently of this Epic.

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist

No silent N/A — every item answered.

### RBAC impact — no new surface

**Confirmed against the tree.** The read endpoint reuses
`[RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]`, byte-identical to the shipped
`POST backtest/{teamId}` (S1). No new `RbacGuardRequirement` member, no new permission, no new scope.

Applying the recommendation introduces **no write path of its own** — it opens the shipped
`ThroughputQuickSetting`, whose write already goes through `canUpdateTeamData` (AC-3.2).

UI gating: all of it derives from the `useRbac()` hook. **No component fetches
`/api/latest/authorization/my-summary` directly** — AC-3.5 asserts this, and it is the project's standing
architecture rule.

### Lighthouse-Clients CLI / MCP versioning — deliberately no, with the precondition for later

**Answer: no CLI or MCP exposure in this Epic, and no client version bump.**

The reason is not cost, it is honesty. The value of this feature is the *rendered artifact and its three
honesty requirements*: the region-not-a-winner verdict, the denominator with its correlation disclosure,
and the coverage-against-nominal reading. A CLI or MCP tool returning sixty-four scores as JSON strips
all three, and an agent reading the raw cells and reporting "the 60-day window is best" is the §4.1 failure
mode, automated and at scale. Shipping the grid to a machine consumer before the honest reading exists
would undo the mitigation the recommendation is conditional on.

**The precondition that reverses this**: AC-1.2 puts the verdict *sentence* in the response as a field. An
MCP tool that surfaces the sentence, the denominator statement and the nominal-rate lines — and does not
expose the cell grid — is both honest and cheap, and becomes a candidate once slice 02 has been dogfooded
and the sentence has proven it reads well. Recorded as a follow-up, not scheduled.

### Website marketing surface — real, secondary, and declared

C1 (free / Community tier) makes conversion an honest secondary objective, and the Epic is tagged
Community for the stated reason that it "should convince people of the method, so they flock to use the
tool". O5 scores 11.6, under-served. So the marketing surface is answered rather than waved at:

- **The carrier is slice 04's one-pager.** `recommendation.md` §5.5 named it as a candidate, and this wave
  commits to it as slice 04 (severable). It is the only thing in the feature that travels, and it is the
  whole of the MARKETING criterion.
- **The launch post** carries the internal codename "The Full Monte" **with attribution to Nick Brown**,
  which is what converts a borrowed article title into a citation. It states the U3 departure in the same
  place (D7, AC-1.8).
- **A docs page** under the Team / Forecast docs, in configurable terminology (C6), showing the collapsed
  verdict and the expanded evidence.
- **R-5 is a DELIVER-wave gate**: the source article was read via a readmedium.com mirror because
  medium.com returns 403. It is arithmetic-reconciled and corroborated, but **one human page-load of the
  original is required before anything from it is quoted publicly.** Nothing ships to the website or the
  launch post until that is done.
- The website hot-links `docs/assets` from `@main` via jsDelivr, so any asset this feature adds must not
  be renamed or removed without checking the website repo first.

### Per-feature docs and screenshots — what DELIVER will owe

1. A docs page for Forecast Reality Check under the Team / Forecast documentation, written in the
   configurable terminology seeded by `TerminologySeeder.cs` — Team, Work Item, Feature — and never using
   "Epic", "Initiative" or "Story". The feature name contains no configurable term (D10/C6).
2. Two `@screenshot` shots, one per theme per the project's per-theme convention: the collapsed verdict
   card, and the expanded four-panel evidence view. Both need a Team with enough real history to produce a
   non-degenerate result, so demo data has to carry one.
3. Demo data check: at least one demo Team whose history produces evaluable cells, and ideally one whose
   short windows do not — the unevaluable state is a differentiator worth showing.
4. Website asset freshness: if the docs page adds an image under `docs/assets`, confirm nothing in the
   website repo links a path this change renames.
5. The launch post, gated on R-5.

---

## Wave: DISCUSS / [REF] Outcome KPIs

Targets are declared as hypotheses. The Epic has no named customer and the ODI figures are reasoned
estimates, so nothing below may be reported downstream as a measured baseline.

| KPI | Who | Does what | By how much | Measured by | Baseline |
|---|---|---|---|---|---|
| **O1 — time to know whether the window fits** | A delivery forecaster on a Team with a year of history | Goes from "no idea" to a verdict they can repeat | One press, answer within 5 s median / 10 s max | AC-1.1, twelve samples on the dev instance | Currently unbounded — requires four typed dates and a guess about which to type |
| **O4 — do not rank incomparable windows** | Every reader of the artifact | Reads the denominator and the non-comparability statement | 100% of rendered results, collapsed and expanded | AC-1.5 and AC-2.5, asserted | 0 — nothing in the product says it today |
| **O4b — no winner is ever named** | The verdict | Names a region, never a single best window | 0 response fields and 0 rendered strings naming one window as best | AC-1.4, asserted | N/A — the surface does not exist |
| **O6 — no unsupportable cell is read as a result** | Every unevaluable cell | Says why it could not be checked, visibly distinct from a calm result | 100% of unevaluable cells; 0 blank | AC-1.7, AC-2.3, asserted | Shipped guard covers the Team-level case; the per-cell case does not exist |
| **§4.3 — the nominal-rate lesson lands** | Each of the four confidence levels | Reports beaten-count against expected-count, with never-beaten called over-forecasting | 4 of 4 levels, every run | AC-1.6, AC-2.4, asserted | 0 — no surface in the product states a nominal rate |
| **O2 — Teams whose window has been checked** | Teams on the dev and demo instances | Have been through a reality check at least once | At least 3 within 30 days of release | Dogfooding record in the slice briefs; usage data only if consent exists | 0 |
| **O5 — the answer travels** | A forecaster answering a sceptic | Pastes a one-pager instead of describing a screen | At least 1 one-pager shared externally within 60 days | Manual — the maintainer's own use, and any community mention | 0 |
| **Engine drift introduced** | The shipped forecast engine | Changes | 0 | Existing forecast assertions unchanged before and after slice 01 | N/A |
| **Mutation kill rate** | Both stacks | Stryker.NET and StrykerJS, acceptance suite excluded | At least 80% | Per-feature run on frozen code | Project standard |

---

## Wave: DISCUSS / [REF] Pre-requisites

| # | Pre-requisite | State |
|---|---|---|
| P1 | A synchronous forecast endpoint pattern exists in this controller | **Confirmed** — `RunBacktest` is already non-async (S1) |
| P2 | All four confidence levels are already computed per run | **Confirmed** — `CreateForecastDtos(50, 70, 85, 95)` (S1, S3) |
| P3 | A shipped data-sufficiency bar exists and is callable per cell | **Confirmed** — `ForecastDataSufficiencyPolicy.HasEnoughData`, a pure function over `RunChartData`, `MinimumActiveDays = 5` (S4) |
| P4 | Today-anchored, reach-back-by-length windowing is the product's existing shape | **Confirmed** — `Team.GetThroughputSettings` (S6) |
| P5 | A surface exists on the Forecast tab to host the control | **Confirmed** — `InputGroup title="Forecast Backtesting"` (S8) |
| P6 | A shipped control writes `throughputHistory` from every Team tab | **Confirmed** — `ThroughputQuickSetting` in `QuickSettingsBar` (S9) |
| P7 | There is a Team setting for the forecast confidence level | **Confirmed ABSENT** — searched; `Team` has no percentile field and `ServiceLevelExpectationProbability` is a cycle-time SLE (S7). **This is a finding, not a blocker** — see D4 |
| P8 | A sixteen-run sweep fits a request budget | **OPEN — the only one.** AC-1.1, resolved as the first task of slice 01 |
| P9 | A client-side export precedent exists | **Confirmed** — ADR-172 and ADR-162, both client-side |

P8 is the single open pre-requisite and is closed inside slice 01 before any UI is written.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | Problem statement clear, in domain language | **PASS** | Each story opens from the forecaster's pain — a setting made once, never checked, driving every published number. No solution language. The SSOT job states it at strategic level |
| 2 | User / persona identified with specific characteristics | **PASS** | `delivery-forecaster` primary and `forecasting-prospect` secondary, both SSOT personas with full profiles. `flow-coach` and `config-admin` explicitly excluded with reasons |
| 3 | 3+ domain examples with real data | **PASS** | Three per story, twelve total. Real personas (Maria Santos, Tom Becker), real Teams (Ocean Explorer, Coastal Survey, Deep Current), real numbers (30, 14, 60, 90 days; 42 items; 3 of 5 days). No `user123` |
| 4 | UAT in Given/When/Then, 3-7 scenarios | **PASS** | 6 / 5 / 4 / 4 across US-01 to US-04. All within band |
| 5 | AC derived from UAT | **PASS** | 8 / 6 / 5 / 4. Each traces to a scenario or to a named honesty requirement, and each asserts an observable output |
| 6 | Right-sized, 1-3 days, 3-7 scenarios | **PASS with a stated qualification** | Four slices at ~8h / ~6h / ~3h / ~4h, each demonstrable in one session. **US-01 carries 8 ACs against the 3-7 band**; AC-1.1 is a measurement gate rather than a behaviour, so the story is seven behaviours plus a probe. Stated rather than gamed; the `epic-6033` US-01 precedent carried ten |
| 7 | Technical notes identify constraints and dependencies | **PASS** | Per story. The surface inventory gives every line the feature touches. C1-C6 honoured and none reopened |
| 8 | Dependencies resolved or tracked | **PASS** | P1-P9. Eight confirmed, P7 confirmed absent as a finding that drives D4, **P8 open and closed inside slice 01 by AC-1.1** before any UI |
| 9 | Outcome KPIs defined with measurable targets | **PASS** | Nine KPIs with who / does what / by how much / measured by / baseline. All declared as hypotheses, none as measured |

Job traceability: **all four stories carry `job_id: job-forecaster-check-the-forecast-against-what-happened`**, an existing SSOT entry. No story is `@infrastructure`, and all four carry an Elevator Pitch with a real entry point and concrete output.

### DoR: PASS

The one qualification — US-01 at eight ACs — is recorded rather than engineered away. No genuine
ambiguity surfaced, so the optional per-wave peer review is not run; the mandatory consolidated review
fires at the end of DISTILL.

---

## Wave: DISCUSS / [REF] Definition of Done

1. `dotnet build` clean, zero warnings (`TreatWarningsAsErrors`).
2. `dotnet test` green with the live-connector categories excluded
   (`Integration`, `JiraIntegration`, `LinearIntegration`, `AdoIntegration`, `ServiceNowIntegration`).
3. `pnpm test` green; `pnpm build` clean with zero errors and zero warnings; Biome clean on `./src`.
4. Playwright specs run locally before commit, through page objects, against demo data.
5. SonarQube Cloud introduces no new issues of any severity.
6. Stryker at or above 80% on both stacks, acceptance suite excluded, run last on frozen code.
7. **No EF migration** — D8 says there is nothing to migrate, and a migration appearing in this Epic is a
   signal that D8 was violated.
8. **AC-1.1 measured and both numbers written into the slice 01 brief.** Not asserted, not inferred.
9. Docs page and two per-theme screenshots at feature finalization, in configurable terminology.
10. **R-5 discharged before anything from the source article is quoted publicly.**
11. ADR-207 written in the DESIGN wave, deciding more than "not yet".
12. Two separate ADO items raised: the `ThroughputHistory` 30-vs-90 default disagreement (D13), and the
    per-Team default confidence level question (D4). Neither folded into #4172.
13. ADO Epic #4172 and its child Stories transitioned; **the Epic stops at Resolved, never Closed.**

---

## Wave: DISCUSS / [REF] SSOT Updates

| File | Change |
|---|---|
| `docs/product/jobs.yaml` | `job-forecaster-check-the-forecast-against-what-happened` — `dimensions.functional` corrected (it said the answer names the window that "would have fitted best", which D1/§4.1 forbid), and a DISCUSS note appended recording D1, D3, D4 and the absent percentile setting. Patched by anchor; the file was not rewritten |
| `docs/product/personas/delivery-forecaster.yaml` | The SSOT job appended to `primary_jobs`. DIVERGE created the job but did not link it from the persona |
| `docs/product/personas/forecasting-prospect.yaml` | The same job appended to `primary_jobs`, marked as the secondary read |
| `docs/product/journeys/epic-4172-forecast-reality-check.yaml` | Created — one journey, five steps, emotional arc, per-step failure modes, shared-artifact registry and integration validation |

---

## Wave: DISCUSS / [REF] Risks Carried Forward

| # | Risk | Disposition |
|---|---|---|
| **R-1** | Does a sixteen-run sweep fit a request budget? | **OPEN, load-bearing.** AC-1.1, first task of slice 01. Evidence is strong — `RunBacktest` is already synchronous and every `TeamMetricsService` read goes through `GetFromCacheIfExists` over Work Items already in the database with no work tracking system contacted — but it is an inference. Fallback: `UpdateQueueService`, with the ADR-195 single-lane caveat |
| **R-3** | Which confidence level does a cell score against? | **RESOLVED by U2 and D1** — all four, 50/70/85/95, printed |
| **R-4** | Is the three-way verdict kept? | **RESOLVED by U3 and D7** — kept, and the departure from Brown is owned out loud in an AC |
| **R-5** | The source article was read via a readmedium.com mirror | **Carried to DELIVER** as a hard gate on the launch post. Arithmetic-reconciled and corroborated, but one human page-load is wanted before anything is quoted publicly |
| **R-6** | `ThroughputHistory` defaults to 30 on the entity and 90 in two UIs | **Out of scope, own ADO item** (D13). Found while researching this Epic; independently verified |
| **R-7** | Rolling-origin evaluation is what every adjacent field does by default | Recorded, not scheduled. D6 is not reopened |
| **R-8** *(new, this wave)* | There is no Team setting for the forecast confidence level (S7, P7) | **Resolved as a design finding, not a gap** (D4). The question of whether one should exist is escalated as its own ADO item |
| **Standing** | ADR-127 describes a team-settings advisory channel that no longer exists in the tree, and carries no note saying so | **Do not reach for that mechanism.** Story #5612 deleted it. A status correction on ADR-127 is worth its own item |

---

## Wave: DISCUSS / Tier-2 Expansion Menu

Density is `lean` with `expansion_prompt: ask-intelligent`. Triggers evaluated against the artifacts
above.

| Trigger | Fired | Why |
|---|---|---|
| AC ambiguity across 2 or more stories | No | Every AC names an observable output and a surface |
| Cross-context complexity (3+ contexts or technologies) | No | Two — backend forecasting/API and the Team Forecast UI. Slice 04's export is client-side inside the second |
| Multi-stakeholder (3+ personas) | No | Two — `delivery-forecaster` and `forecasting-prospect`. Two more are named only to be excluded |
| Compliance or regulatory | No | No regulated data; the endpoint is read-only over data already in the instance |
| Walking-skeleton strategy = D (configurable) | No | Strategy B |

**No trigger fired. No expansion menu is offered and no Tier-2 section is rendered.**

One thing that *would* have justified an expansion was considered and declined: a `ux-rendering-options`
section weighing alternatives to D2's band-with-a-mark. It is declined because the alternatives were
already eliminated on the record rather than on taste — the heatmap by ADR-194's precedent against a
rendering whose grammar overclaims and by `@mui/x-charts-pro` not being installed (S10), the ranked list
by §4.1, and the four-panels-per-level explosion by D2's argument that a distribution read at four places
is one object. Re-rendering that reasoning in a separate section would duplicate D2.

Telemetry: no triggered ids, so no `choice` records.
