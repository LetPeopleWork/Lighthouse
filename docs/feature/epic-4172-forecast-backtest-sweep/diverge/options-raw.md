# Options (raw) — epic-4172-forecast-backtest-sweep

**Wave**: DIVERGE · Phase 3 (Brainstorming)
**Agent**: Flux (nw-diverger)
**Date**: 2026-09-22

> **Generation only.** Nothing in this file is evaluated, ranked, preferred, or dismissed. No option is
> described as good, bad, cheap, expensive, risky or safe. Costs and forecloses statements appear as
> *properties* — facts about what each option is — because Axis 1 was explicitly asked for in that form;
> they are not judgements and they carry no verdict. Scoring happens in `taste-evaluation.md`, after this
> file is closed.

---

## 1. HMW framing

Derived from the strategic job in `job-analysis.md` §3.

**Bad HMW (rejected, solution embedded):** "How might we build a better back-test grid?"
**Bad HMW (rejected, still a mechanism):** "How might we run many back-tests at once?"

**Validated HMW:**

> **How might we give a forecaster evidence, from their own Team's completed history, that the
> configuration behind their forecasts fits how that Team actually delivers — without inviting them to
> read more into the comparison than the history can support?**

The second clause is part of the question rather than a caveat on the answer, because ODI outcome **O4**
(don't rank incomparable windows) scored the highest opportunity of the six. An HMW that omitted it would
open a solution space in which the highest-opportunity outcome is not addressable.

**What the HMW deliberately leaves open**: whether there is a grid; whether there is a sweep at all;
whether anything is stored; whether anything is asynchronous; whether the output is a picture, a
sentence, or a file.

---

## 2. Locked constraints observed during generation

No option below reopens any of these. They were checked per option after generation.

| | Constraint |
|---|---|
| C1 | Free / Community tier, not premium. |
| C2 | On demand / manual only. No scheduling, no continuous mechanic in MVP. |
| C3 | The forecast-throughput filter is a config option of the run, never a fifth sweep dimension. |
| C4 | Single anchor: **today is the END anchor**. Each horizon reaches backward from today; its history window sits immediately before it. No date picker. **Cells are therefore not comparable to each other.** |
| C5 | The minimum-data bar composes with the shipped 5-active-days rule; it is never replaced. |
| C6 | Terminology: Team, Work Item, Feature, throughput, cycle time, WIP. Never Epic / Initiative / Story in user-facing copy or proposed names. **Extended by Phase 2**: every one of those configurable terms — including **throughput**, Team, Delivery, Cycle Time, WIP, Blocked, SLE — is unsafe *inside a feature name*, because it would render as the user's own word. Safe naming vocabulary: forecast, calibration, hindsight, replay, accuracy, check, reality, sampling, history, percentile. |

---

## 3. Measured facts available to generation

Established by reading the code, not assumed. These are inputs to generation, not verdicts.

| Fact | Where it was read |
|---|---|
| `Team.ThroughputHistory` is an `int` in days, model default **30**. `UseFixedDatesForThroughput` + `ThroughputHistoryStartDate/EndDate` are the fixed-window alternative. | `Models/Team.cs:11-17` |
| The frontend seeds **90** for a new Team in two places while the entity defaults to **30**. | `CreateTeamWizard.tsx:35`, `EditTeam.tsx:80` vs `Team.cs:17` |
| `ForecastService.HowMany` is 10,000 trials over an inner loop of `days` — for an 8-week horizon roughly 40 working days. Sixteen cells is on the order of 6.4 million draws of a list index, in process, no I/O. | `ForecastService.cs:33-53`, `ForecastSimulationLimits.cs` (`Default = 10_000`) |
| The per-cell I/O is `GetBlackoutAwareThroughputForTeam` plus `GetThroughputForTeam` — sixteen of each. | `ForecastController.cs:210-219` |
| The update queue is **one** `Channel.CreateUnbounded` with **one** sequential reader. ADR-195's three lanes shipped and were reverted the same day (`f216ef558`). A field report in ADR-195 records a 77-minute portfolio refresh starving every Team refresh for 3h38m. | `UpdateQueueService.cs:11`, ADR-195 |
| `UpdateType` has five members; adding one reaches `UpdateEntityKinds.Of`, `UpdateNotificationHub.RefreshTypeFor`, `UpdateController.CancelTask`. | `UpdateType.cs`, `UpdateEntityKinds.cs`, `UpdateNotificationHub.cs:119-123`, `UpdateController.cs:88-97` |
| `UpdateStatus` already carries `QueuedAt` / `StartedAt`; `UpdateController` already serves "what is running", "what is waiting", "what is it waiting for", and "cancel that one"; `UpdateNotificationHub` already pushes progress over SignalR. | `UpdateStatus.cs`, `UpdateController.cs` |
| **`ThroughputQuickSetting` already exists** and already writes `throughputHistory` (and the fixed-date fields) to the Team, with validation, from the `QuickSettingsBar` in the Team detail **header** — present on every Team tab. | `components/Common/QuickSettings/ThroughputQuickSetting.tsx`, `TeamDetail.tsx:402-430` |
| The single-shot back-test lives in an `InputGroup` titled "Forecast Backtesting" inside `TeamForecastView`, alongside the Team Forecast and New Work Item Creation Forecast groups. | `TeamForecastView.tsx:438-460` |
| `@mui/x-charts` 9.0.1 is installed. MUI X's Heatmap is a Pro-licensed component (to be verified before use); a sixteen-cell matrix needs no charting component at all — it is `Box` and `Typography`. | `Lighthouse.Frontend/package.json:28`, `components/Common/Charts/` |
| Export precedent is client-side: the caller builds one settled table (ADR-172), with a generic toolbar header block (ADR-162). No server-side document renderer exists. | ADR-172, ADR-162 |
| An advisory channel on team settings exists as a pattern (ADR-127): a verdict computed server-side, rendered by `ValidationAdvisory.tsx`, surfaced on the settings surface. | ADR-127 |

---

## 4. SCAMPER — one option per lens

Seven lenses, seven options. Generated in lens order without looking back.

### S — Substitute: replace the verdict, not the mechanism

**S1 · "Where did reality land?"**

**Core idea**: Every cell reports **one number on one scale** — the percentile position at which the
Team's actual throughput fell inside that cell's forecast distribution — instead of a three-state
under / over / within verdict. A cell reads "actual landed at the 62nd percentile", not "within range".

**Key mechanism**: The `HowManyForecast` already holds the full simulated distribution; reading the
position of a known actual inside it is the inverse of the percentile lookup the product already does.
The output is a strip of sixteen landing positions on a single 0-100 axis, with the honest reading being
*the spread and centring of the landings*, not any individual cell.

**Key assumption**: A forecaster can read "actual landed at the 62nd percentile" as easily as "within
range" — that is, the scale is teachable in one sentence of chart copy.

**SCAMPER origin**: Substitute (the score, not the machinery).

**Closest analogue**: probabilistic-forecast verification — the PIT histogram and the reliability
diagram, which are the standard honest answer to "was my probabilistic forecast well calibrated?" (see
`competitive-research.md` Q3).

**Axis 1 — Report abstraction**: none. The landing positions are computed and returned; nothing is
stored.
**Axis 2 — Surface**: a single horizontal calibration strip in the existing "Forecast Backtesting"
group; sixteen dots on one axis, shaped by history window. In flight: an inline progress bar. Previous
run: replaced. Scope: per-Team.
**Axis 2 — Proposal**: "the 60-day window's four landings are the most centred" — the sentence names the
window, the user opens the existing `ThroughputQuickSetting`.
**Axis 2 — Disqualified cells**: a dot cannot be drawn where there is no distribution, so a disqualified
cell is a labelled tick on the axis with no dot and an explicit "not enough history" caption — never a
gap, per ADR-194.
**Axis 3 — Name candidate**: "Forecast Calibration".

---

### C — Combine: merge with the job next door

**C1 · "Front door to the back-test"**

**Core idea**: The sweep is not a new feature; it becomes the **default view of the back-test that
already ships**. Opening Team → Forecast → Forecast Backtesting shows sixteen results already computed
from defaults. Clicking any one of them loads that cell's four dates into the existing single-shot
`BacktestForecaster` inputs, where the user can then change anything by hand. The overview and the
drill-down are one surface at two depths.

**Key mechanism**: The existing component's props are already fully controlled and lifted into
`TeamForecastView` (`startDate`, `endDate`, `historicalMode`, `historicalWindowDays`, and the setters).
A cell click is a call to four setters that already exist.

**Key assumption**: The user who arrives at "Forecast Backtesting" wants an answer before they want
controls — so replacing four empty date pickers with sixteen filled-in answers is what they came for.

**SCAMPER origin**: Combine (this job merged with the already-shipped single-shot back-test job).

**Closest analogue**: ActionableAgile's approach of showing the chart populated from the team's data on
arrival, with the date range as an adjustment rather than a precondition.

**Axis 1 — Report abstraction**: none, plus a cache. The most recent sweep is held as a single nullable
owned row on the Team (`LastCalibrationRun`: computed-at, payload JSON) so the page is not recomputing
on every navigation.
**Axis 2 — Surface**: exactly where the back-test is today. In flight: the group renders a skeleton.
Finished: it renders. Previous run: overwritten by the new one; the computed-at timestamp is shown.
Scope: per-Team.
**Axis 2 — Proposal**: a sentence above the results, with the drill-down as its evidence.
**Axis 2 — Visual form**: a **ranked list**, one row per history window (four rows), each row carrying
its four horizon outcomes as inline chips. A list is one-dimensional and therefore does not present the
sixteen results as a coordinate system to be scanned across.
**Axis 2 — Disqualified cells**: a chip reading "history too thin", in the neutral chip style the
product already uses for the filter chip.
**Axis 3 — Name candidate**: "Backtest Overview".

---

### A — Adapt: borrow the run object from a different domain

**A1 · "The Report object"**

**Core idea**: Introduce a generic **Report** the way an ML platform introduces a Run: a first-class
record with a kind, a status, who asked for it, when it finished, and a payload. Exactly one kind is
implemented (`ForecastCalibration`). A Reports tab lists every report the Team has produced, newest
first; opening one renders it.

**Key mechanism**: New entity `Report { Id, Kind, ScopeType, ScopeId, Status, RequestedBy, RequestedAt,
CompletedAt, PayloadJson, FailureReason }`, a `IReportRunner` registry keyed by kind, and a completion
event that a future in-app notification (#4754) subscribes to without the Report knowing it exists.

**Key assumption**: A second and third Report kind genuinely arrive — the parked constellation (#1822,
#4753, #4754, #4755, #4155, #4080) is a real roadmap and not a wish-list — so the generalisation is paid
back rather than carried.

**SCAMPER origin**: Adapt (Weights & Biases / MLflow run objects, transferred to a delivery tool).

**Closest analogue**: W&B Sweeps and MLflow Runs — a parameter sweep is a first-class stored object with
a status, a parameter set, and an artifact, listed and revisitable.

**Axis 1 — Report abstraction**: this *is* the abstraction. Shape (b) on the commitment spectrum, plus
the (c) notification seam.
- **Costs now**: one entity, one migration, one runner registry, one status vocabulary, one list surface,
  one detail surface, one completion event. Plus the question of *which* runner runs it (see §6).
- **Buys later**: #1822 and #4753 become a new `Kind` with a PDF payload; #4080 a `Kind` with a CSV
  payload; #4754 subscribes to the completion event; #4755 subscribes to the same event with a channel
  adapter; #4155 supplies a different trigger for the same runner.
- **Forecloses**: a report whose result is not a document — anything streaming, incremental, or
  continuously-recomputed does not fit a completed-at-plus-payload shape, and #4155's rule-based
  thresholds may be exactly that. Also forecloses cheap deletion of the concept, because a Reports tab
  is a promise to users.
- **What the ADR must decide**: (1) is a Report an entity or a projection; (2) what a Report's payload
  is — opaque JSON, a typed per-kind column set, or a blob reference; (3) whether the runner is the
  existing update queue, a second queue, or in-request; (4) whether completion is an event on the
  existing domain-event bus or a direct SignalR push; (5) retention — how many reports per scope are
  kept and who deletes them; (6) whether Report is scoped to a Team, or to any entity, or global.

**Axis 2 — Surface**: a new **Reports tab** on the Team, beside Forecast and Metrics. In flight: the
report appears in the list immediately with a Queued/Running status and an elapsed time — the same three
facts `UpdateController` already publishes for refreshes. Finished: SignalR flips the row and a snackbar
fires. Previous runs: **kept**, listed, comparable over time. Scope: per-Team, with the entity shaped to
allow global later.
**Axis 2 — Visual form**: a 4x4 matrix of sixteen cells, hand-rolled from `Box`, coloured by outcome,
with row and column headers naming the real date span each cell covers so the non-comparability is
readable off the axis labels rather than asserted in a caption.
**Axis 2 — Proposal**: a headline block at the top of the report; apply is a link to the header's
existing `ThroughputQuickSetting` with the recommended value pre-filled.
**Axis 2 — Disqualified cells**: a distinct hatched cell with its own legend entry, never the same
treatment as any outcome.
**Axis 3 — Name candidate**: the feature is "Forecast Calibration Report"; the *kind* is
`ForecastCalibration`.

---

### M — Modify / Magnify: amplify the verdict, shrink everything else

**M1 · "One sentence, evidence on request"**

**Core idea**: The default output is **one sentence and one button**. "Over the last 8 weeks, a 60-day
sampling window would have put your actual delivery inside the forecast range; your current 30-day
window put it above the 95th percentile. Change the window to 60 days?" Everything else — all sixteen
results, every date span, every percentile — sits behind a "show the evidence" disclosure that most
users never open.

**Key mechanism**: The recommendation is computed server-side and returned as a rendered claim plus its
supporting data; the client renders the claim first and the data on demand.

**Key assumption**: The user wants the answer, not the analysis — and will trust a single sentence from
a tool that has, elsewhere, refused to give them a number it could not stand behind (`forecast-confidence-cap`,
`forecast-minimum-data-guard`).

**SCAMPER origin**: Modify / Magnify (the recommendation, which was the last line of the request,
becomes the whole of the surface).

**Closest analogue**: the flow-forecasting-readiness assessment's own shape — one memorable number, one
named band, one concrete next rung; the wall of sub-metrics deliberately not shown.

**Axis 1 — Report abstraction**: none. Shape (a) on the spectrum — a result held for the session and
recomputed on demand. The refactor is deferred to the arrival of the second Report.
**Axis 2 — Surface**: a single card in the Forecast tab. In flight: the button becomes a spinner in
place. Finished: the sentence replaces it. Previous run: gone. Scope: per-Team.
**Axis 2 — Proposal**: this *is* the option. Apply is MVP here, because the sentence without the button
is a dead end — and it costs a pre-filled open of `ThroughputQuickSetting`.
**Axis 2 — Visual form**: no chart by default. Behind the disclosure, small multiples — four small
distribution sparklines, one per history window, each with its four actuals marked.
**Axis 2 — Disqualified cells**: named in prose in the sentence itself ("two of the sixteen checks could
not run — the 2-week history windows hold fewer than five days with completed Work Items").
**Axis 3 — Name candidate**: "Sampling Window Check".

---

### P — Put to other use: the same artifact for the person who does not have the tool

**P1 · "The one-pager"**

**Core idea**: The run produces a **document** — a self-contained Markdown or HTML page with the numbers,
the date spans, the method, and the conclusion — that the user downloads and pastes into a deck, sends to
their leadership, or posts in a channel. The in-product surface shows only the run status and a download
link. The reader of the artifact is explicitly allowed to be someone who does not use Lighthouse.

**Key mechanism**: Client-side document construction from the result payload, following the ADR-172 /
ADR-162 export precedent (the calling surface builds one settled artifact; no server-side renderer is
introduced).

**Key assumption**: The artifact travels — the thing that converts a sceptic is a page with their own
team's numbers on it, in their own inbox, not a screen they have to be logged in to see.

**SCAMPER origin**: Put to other use (the forecaster's configuration check re-used as the prospect's
proof).

**Closest analogue**: Troy Magennis / FocusedObjective's spreadsheets, which spread precisely because
they are artifacts people send each other rather than screens people visit.

**Axis 1 — Report abstraction**: minimal — a payload and a status, with "a Report is a document" taken
literally, which is the user's own framing ("more a document form"). Emailing is then a transport over an
artifact that already exists rather than a new rendering path.
**Axis 2 — Surface**: a button and, beneath it, the last artifact with its timestamp and a download link.
In flight: the button is disabled with a progress caption. Finished: the link appears. Previous artifact:
replaced, with the timestamp making the replacement visible. Scope: per-Team.
**Axis 2 — Visual form**: whatever renders in a document — a table with outcome words and the real date
span per row, plus a written conclusion. No interactive chart at all.
**Axis 2 — Proposal**: a paragraph in the document naming the recommended window; applying it is done by
hand afterwards in the header control.
**Axis 2 — Disqualified cells**: a table row reading "not run — history too thin (needs 5 days with
completed Work Items)".
**Axis 3 — Name candidate**: "Forecast Calibration Report" as the document's title.

---

### E — Eliminate: remove the most complex part

**E1 · "Sweep in a breath"**

**Core idea**: Delete the asynchrony, the entity, the notification, the tab and the dialog. One button,
no configuration, sixteen results computed in a single request, rendered in place. The measured basis is
that sixteen `HowMany` calls are 10,000 trials each over at most forty in-memory iterations, with the
only I/O being the throughput reads the single-shot back-test already performs once.

**Key mechanism**: `POST /api/forecast/backtest/{teamId}/sweep` returning all sixteen cells in one
response, computed inside the request, reusing `ValidateBacktestInput`'s logic only where C4's fixed
anchoring still makes it meaningful.

**Key assumption**: The run completes inside an acceptable request budget on a real instance, including
the slowest realistic case (a large Team on SQLite with blackout periods configured). This is the
option's single load-bearing unknown and it is measurable in an hour.

**SCAMPER origin**: Eliminate (the async machinery, which was in the request as a premise rather than as
a requirement).

**Closest analogue**: Prophet's `cross_validation()` — a function call that returns a dataframe. Nobody
built a job queue for it.

**Axis 1 — Report abstraction**: **none, deliberately.** Shape (a) on the spectrum, with the position
stated in the ADR rather than left implicit: the ADR records *what a Report will be when the second one
arrives* and *what this feature must not do to make that harder*, and implements none of it.
- **Costs now**: nothing beyond the endpoint. Nothing to migrate, nothing to retain, nothing to delete.
- **Buys later**: nothing directly; it buys the information needed to design the abstraction correctly
  from two real examples instead of one imagined one.
- **Forecloses**: history — there is no record that the run ever happened, so "has this Team ever been
  checked?" is unanswerable, and the disruption path in `job-analysis.md` §4 (scheduled re-runs) starts
  from zero. Also forecloses emailing, which needs something durable to send.
- **What the ADR must decide**: that the sweep result is *deliberately* not persisted, why, and the named
  trigger for revisiting — which is stated as "the second Report kind", not a date.
**Axis 2 — Surface**: a button inside the existing "Forecast Backtesting" group. In flight: the button
becomes a spinner; the whole wait is a few seconds. Finished: results appear below it. Previous run:
replaced. Scope: per-Team.
**Axis 2 — Visual form**: **small multiples** — four panels, one per history window, each showing its
four horizons as a forecast range with the actual marked against it. Four separate panels make four
separate statements; a single grid makes one statement about a coordinate system that does not exist.
**Axis 2 — Proposal**: a sentence beneath the panels naming a window, with apply deferred to slice 02.
**Axis 2 — Disqualified cells**: a panel that cannot be drawn says so in words where the chart would be.
**Axis 3 — Name candidate**: "Forecast Check".

---

### R — Reverse: swap who makes the claim

**R1 · "Your setting, on trial"**

**Core idea**: Invert the direction of the question. Do not search a space of sixteen and rank it.
Instead, take the configuration the Team **already has** and put it on trial against the four horizons —
"your 30-day window, checked". Alternatives are computed but shown **only if the current setting fails**,
and then framed as "here is one that would have held", never as a ranked league table.

**Key mechanism**: The current `ThroughputHistory` is always one of the rows; the verdict is about that
row; the other rows are a remedy offered on failure rather than a result presented on success. Because
nothing is ever ranked, the non-comparability trap has no surface to appear on — the option is
structurally incapable of inviting the comparison.

**Key assumption**: Most Teams' current setting is either fine or clearly wrong, so a binary verdict with
a conditional remedy covers nearly every case — and the ambiguous middle is rare enough to be handled by
"we could not tell" rather than by a grid.

**SCAMPER origin**: Reverse (claim-then-test instead of search-then-rank; and pull instead of push — the
verdict waits on the Team rather than notifying).

**Closest analogue**: a lint rule or a health check — it does not enumerate configurations, it judges
yours.

**Axis 1 — Report abstraction**: none; a verdict, which is a different thing from a document. The verdict
is a small typed record on the Team (verdict, checked-at, the window checked), not a payload.
**Axis 2 — Surface**: the advisory channel described by ADR-127 — a verdict rendered on the
Team settings surface and echoed as a chip near the throughput control in the header.
*(**Factual correction, 2026-09-22, after generation**: this option was generated on the belief that the
channel exists, which ADR-127's own Context asserts. **It does not.** Story #5612 deleted the
`Advisory`/`AdvisoryCode` pair and `SuccessWith`; `ValidationAdvisory.tsx` is absent from the entire
frontend; and a later rung was explicitly built *"rather than reviving them"*
(`ConnectionValidationResult.test.ts:23-26`, `ServiceNowBoardVerdict.cs:37-41`). The option stands — it
can render its verdict in a card of its own — but it gets no free channel. **ADR-127 is stale and carries
no note saying so.** See `taste-evaluation.md` §1 and §6.)* In flight:
the chip reads "checking". Finished: the chip carries the verdict. Previous verdict: superseded, with the
checked-at date visible so a stale verdict is recognisable as stale. Scope: per-Team.
**Axis 2 — Visual form**: no matrix in the default path. On failure, a **comparison of two** — your
window against the one that would have held — which is two things, and two things can be compared
honestly in a way that sixteen cannot.
**Axis 2 — Proposal**: this is the option's whole output. Apply is MVP and is the header control.
**Axis 2 — Disqualified cells**: if the Team's own window cannot be evaluated, the verdict is "cannot be
checked yet", which is a first-class verdict rather than an absence.
**Axis 3 — Name candidate**: "Sampling Window Health". *(Corrected after Phase 2: the original candidate was "Throughput Window Health", which breaks C6 — **"throughput" is itself a user-configurable term** under Settings → Terminology, so a name containing it renders as the user's own word. See `competitive-research.md` §5.)*

---

## 5. Crazy 8s supplements

One minute each, no looking back.

**X1 · API and client first, no UI.** Ship the sweep as an endpoint plus a Lighthouse-Clients CLI command
and an MCP tool. Dogfood against the project's own instance immediately. UI arrives whenever it arrives.

**X2 · Demo-data marketing page.** Run the sweep once against the seeded demo data, publish the result as
a page on letpeople.work with the method written out, and build the product feature afterwards, informed
by which part of the page people actually read.

**X3 · Sweep every Team at once.** Make the unit of work the **instance**, not the Team: one run covers
every Team the caller can see, and the output is a list of Teams whose configuration does not fit. Turns
a per-Team curiosity into an operator's worklist.

**X4 · Refuse the sweep; fix the default instead.** Use the sweep offline, as a research instrument
against real instances, to discover the right default for `ThroughputHistory` — and ship a changed
default plus an explanation, with no user-facing feature at all. (Generated in direct response to the
measured fact that the entity defaults to 30 and the creation wizard seeds 90.)

---

## 6. An open mechanism question, recorded not resolved

Every option that runs anything outside the request has to answer **which runner**, and the measured
facts make this sharper than it looks. It is recorded here because it cuts across options rather than
belonging to one, and because resolving it during generation would have been evaluation.

The existing `UpdateQueueService` is one unbounded channel with one sequential reader. ADR-195's field
report records a portfolio refresh that ran 77.7 minutes and starved every Team refresh for 3h38m; the
three-lane fix shipped and was reverted the same day. So a calibration run placed on that queue would
wait behind whatever is refreshing, and anything refreshing would wait behind it.

Three answers exist and none is chosen here:

1. **In the request.** No queue involved.
2. **On the existing queue**, as a sixth `UpdateType`, inheriting status, cancel, elapsed-time and
   SignalR progress for free — and inheriting the head-of-line behaviour with them.
3. **Beside it** — a second, separate channel for work that is user-initiated, short, and read-only, which
   is a different kind of work from a tracker sync.

This is the sharpest thing ADR-207 has to decide, and it is a question about *this product's queue*, not
about report abstractions in general.

---

## 7. Curation to six

Ten options generated (7 SCAMPER + 3 usable Crazy 8s; X4 is counted below). Curated to six.

### Kept

| # | Option | Lens |
|---|---|---|
| **1** | **E1 · Sweep in a breath** | Eliminate |
| **2** | **A1 · The Report object** | Adapt |
| **3** | **C1 · Front door to the back-test** | Combine |
| **4** | **M1 · One sentence, evidence on request** | Modify / Magnify |
| **5** | **S1 · Where did reality land?** | Substitute |
| **6** | **R1 · Your setting, on trial** | Reverse |

### Merged or set aside, with reason

| Option | Disposition |
|---|---|
| **P1 · The one-pager** | **Merged into the evaluation as a property rather than kept as a rival.** Its distinguishing claim — the artifact must travel outside the tool — applies to all six and is scored as MARKETING SURFACE FITNESS. Kept alive as a slice-02 addition to whichever direction wins, because the export precedent (ADR-172, ADR-162) is client-side and therefore adds to any of them at the same cost. |
| **X1 · API and client first** | **Set aside.** It is a *sequencing* choice, not a different mechanism — every one of the six has an endpoint, and any of them can ship endpoint-first. Recorded so the DISCUSS wave can apply it to the winner rather than choose it instead of one. |
| **X2 · Demo-data marketing page** | **Set aside — different repository, different wave.** It is a website change, not a Lighthouse product change, and it depends on the feature existing first to be honest. Carried forward as a DELIVER-wave marketing item. |
| **X3 · Sweep every Team at once** | **Set aside as scope, retained as a shape constraint.** It changes the unit of work from Team to instance, which is a larger feature than the Epic describes; but it is the strongest argument for *not* hard-coding a Team into whatever entity or endpoint is built. Recorded as a forward-compatibility requirement on the winner. |
| **X4 · Fix the default instead** | **Set aside as a rival, escalated as a finding.** As an option it answers no part of the job — a better default helps nobody understand their own Team. But the measured inconsistency it was generated from (entity default 30, creation wizard 90) is real, is a separate defect, and is escalated to DISCUSS on its own. |

### Diversity test on the six

Each pair must differ in at least one of mechanism, assumption, cost profile. Reported per option against
the set.

| # | Mechanism | Assumption about the user | Cost profile |
|---|---|---|---|
| 1 · E1 | Synchronous computation, nothing stored | Wants the answer now and will not return for it | Smallest — one endpoint, one component |
| 2 · A1 | Durable run object with a lifecycle, generic over kind | Will come back to a report, and will accumulate several | Largest — entity, migration, runner, two surfaces, an event |
| 3 · C1 | Replacement of an existing surface's default state; drill-down into shipped controls | Arrived wanting an answer, not controls | Small-to-medium — no new surface, one cache row, one wiring pass |
| 4 · M1 | Server-rendered claim; data withheld behind disclosure | Wants a verdict and will not read a grid | Small — one card, plus the recommendation logic which no other option can avoid either |
| 5 · S1 | Substituted scoring function — continuous position, not categorical verdict | Can learn one new number in one sentence | Medium — the arithmetic is small, the chart and its teaching copy are not |
| 6 · R1 | Inverted question — judge the held configuration, offer a remedy on failure | Wants to be told whether they are wrong, not shown a space | Small-to-medium — reuses the advisory channel, adds a verdict record |

Pairwise check for the two closest pairs, since the test only bites where options are near each other:

- **1 vs 3** share "no new surface, nothing durable". They differ in **mechanism** (1 adds a control to a
  surface; 3 replaces that surface's default state and re-points the shipped single-shot controls) and in
  **cost** (3 carries a cache row and a wiring pass that 1 does not). Distinct.
- **4 vs 6** share "a sentence rather than a grid". They differ in **assumption** (4 assumes the user
  wants the best option found by a search; 6 assumes they want a verdict on the option they already hold)
  and in **mechanism** (6 never ranks and therefore cannot present a league table; 4 ranks and then hides
  the ranking). Distinct — and the difference is the exact axis O4 cares about, so it is load-bearing
  rather than cosmetic.
- **1 vs 4** share "small, synchronous, nothing stored" and are **the thinnest distinction in the set**,
  which is recorded rather than smoothed over. They differ in **mechanism** (4's output is a claim the
  server authors and the client renders first; 1's output is rendered data with a sentence appended) and
  in **assumption** (4 assumes the evidence goes unread and designs for that; 1 assumes it is read). Cost
  profiles are close. They pass, but by the narrowest margin of any pair, and a reader is entitled to
  read 4 as "1 with the evidence collapsed".
- **2 vs 5** differ in every column.

**All six pass the three-point diversity test**, with the 1-vs-4 margin noted as the narrowest.

### SCAMPER coverage

| S | C | A | M | P | E | R |
|---|---|---|---|---|---|---|
| S1 kept | C1 kept | A1 kept | M1 kept | P1 generated, folded into evaluation | E1 kept | R1 kept |

Seven lenses applied, all seven produced an option, six carried forward.

---

## 8. Gate G3 evaluation

| Criterion | Result |
|---|---|
| 6 curated options | **PASS** |
| Each passes the 3-point diversity test | **PASS** — §7, with the two closest pairs checked explicitly |
| SCAMPER coverage documented | **PASS** — all seven lenses, §4 and §7 |
| Crazy 8s supplements generated | **PASS** — four, §5 |
| No evaluation language in this file | **PASS** — no option is ranked, preferred, or dismissed; the "set aside" table records structural dispositions (merged as a property, different repository, different unit of work, answers no part of the job), not quality verdicts |
| Options do not reopen locked constraints | **PASS** — C1-C6 checked per option; no option introduces a date picker, a schedule, a premium gate, a fifth sweep dimension, a second data bar, or forbidden terminology |

**G3: PASS.** Taste evaluation may begin.
