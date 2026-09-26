# Recommendation — epic-4172-forecast-backtest-sweep

**Wave**: DIVERGE (complete) · **For**: Luna (`nw-product-owner`), DISCUSS wave
**Agent**: Flux (`nw-diverger`) · **Date**: 2026-09-22
**ADO**: Epic #4172 "The Full Monte" — Planned, tag Community, priority 2

Supporting artifacts, all under `docs/feature/epic-4172-forecast-backtest-sweep/`:
`diverge/job-analysis.md` · `diverge/competitive-research.md` · `diverge/options-raw.md` ·
`diverge/taste-evaluation.md` · `diverge/review.yaml` · `wave-decisions.md`
SSOT: `docs/product/jobs.yaml` — `job-forecaster-check-the-forecast-against-what-happened`.

---

## 0. The decision, up front

> **Proceed with M1 · "One sentence, evidence on request", shipped as *Forecast Reality Check* — a
> synchronous, one-click, no-new-entity check on the Team's Forecast tab that answers with a plain-language
> verdict naming a *region* of acceptable sampling windows, with the sixteen results one click behind it as
> small multiples, and an Apply control that pre-fills the `ThroughputQuickSetting` that already sits in
> the Team detail header.**
>
> **Assuming**: (a) sixteen cells complete inside an acceptable request budget — measurable in an hour, and
> the first thing slice 01 must do; and (b) the team accepts that the *evidence-behind-a-disclosure* posture
> is mitigated by the three named honesty requirements in §4, not merely asserted.
>
> **ADR-209 decides that there is no Report abstraction yet**, records why, and names the trigger for
> revisiting it. It builds none of the parked constellation.

This is not "both options are viable". It is one direction, with a named second place whose case is made
in full in §6 and which the scoring nearly chose.

---

## 1. Two research findings that change the brief

These emerged in Phase 2 and are load-bearing. They are reported here rather than buried because they
change what DISCUSS should write stories against.

### 1.1 The Epic sweeps the axis that carried no signal, and omits the one that carried all of it

Brown's original study swept **horizon × window × percentile** across 25 teams. Results:

| Axis | Effect on the correct-rate |
|---|---|
| **Percentile** (50 / 70 / 85) | **68% → 84% → 90%** — the whole of the finding |
| **History window** (6 / 8 / 10 / 12 weeks) | **19-23% incorrect** — four points of noise |

Brown says so himself, eighteen months later: *"the amount of historical data does not play a significant
factor in the outcomes of forecast accuracy."*

Epic 4172 sweeps horizon × window. **It keeps the null axis and drops the signal axis.** Three
consequences for DISCUSS:

1. **The feature must be able to return a null result.** "Your sampling window barely matters in this
   range, and here is the evidence" is the answer the source study actually supports, and it is a *good*
   answer — honest, differentiated, and impossible for any competitor to contradict. A direction that is
   architecturally obliged to name a winner cannot say it. **This is a hard requirement on the
   recommendation, not a nice-to-have.**
2. **Lighthouse's shortest window (2 weeks) is below Brown's floor (6 weeks)**, so the sweep is still worth
   running — but its most informative cell is also the one most likely to fail the data-sufficiency bar.
3. **Percentile is an open question for DISCUSS**, not a decision taken here. Adding it as a third axis
   makes 48 cells, which §1.2 says is worse. The recommended resolution is to **fix the percentile, print
   which one, and report coverage against its nominal rate** — see §4.

### 1.2 Sixteen configurations against months of history is already in the noise regime

Bailey, Borwein, López de Prado & Zhu (*Notices of the AMS*, 2014, peer-reviewed) give the arithmetic:
*"if only five years of data are available, no more than forty-five independent model configurations should
be tried or one is almost guaranteed to produce strategies with an annualized Sharpe ratio in-sample of 1
but an expected Sharpe ratio out-of-sample of zero."* A Lighthouse Team has months, not years. This is the
same operation — grid-search a configuration space against one finite historical sample and report the
argmax.

The field's answer is not "don't do it". It is **change the shape of the output**:

- **Quant practice**: robust parameters form *plateaus*; overfit ones form *needles*. **Name the region,
  not the winner.**
- **Hansen's Model Confidence Set**, the formal version: return *a set of statistically indistinguishable
  candidates*, not one.
- **Bailey's disclosure norm**: *"reporting only the best backtest result without disclosing how many
  strategy configurations were evaluated represents a form of selection bias."* Print the denominator.

**This is the single most useful thing the research produced**, because it resolves the tension between O4
(don't rank incomparable windows, opportunity **14.9**) and O1 (answer the question, **13.2**) without
sacrificing either. The recommendation adopts it in §4.

---

## 2. Top 3 options

Full matrix in `diverge/taste-evaluation.md` §4. The top three sit within **0.20** of one another — a
genuine cluster, reported as such.

### 1st — M1 · "One sentence, evidence on request" — **4.30**

**Core idea**: the default output is one sentence and one button. The sixteen results sit behind a "show
the evidence" disclosure.

**Why it scores well**: top marks on **DECISION-CHANGING (5)**, **CHEAP FIRST SLICE (5)** and
**LOAD-BEARING (5)**. It is the only option that puts the named change *and* the control that makes it in
the first interaction while building nothing speculative. The apply path is near-free because
`ThroughputQuickSetting` already exists in the Team detail header, on every tab, already writing
`throughputHistory` with validation.

**Core trade-off**: it sacrifices *default legibility*. The evidence exists but is not shown.

**Key risk**: **HONESTY scores 3 — the lowest of the top three, on the highest-weighted criterion.**
Withholding the evidence by default is the "black box / lead-gen toy" exposure, and `job-analysis.md` §4
establishes that legibility is exactly what earns the right to build the automatic version later. This is
flagged, not smoothed over; §4 turns it into three build requirements.

**Hire criteria**: a user would choose this when they want to be *told* whether their configuration is
sound and to act on the answer immediately — which is the modal case for a forecaster who did not know the
question existed until the button appeared.

### 2nd — R1 · "Your setting, on trial" — **4.15**

**Core idea**: don't search a space and rank it. Put the configuration the Team already holds on trial;
show an alternative only if it fails.

**Why it scores well**: **5 on HONESTY and 5 on DECISION-CHANGING** — the two highest-weighted criteria,
55% of the total. It is the only option **structurally incapable** of the false comparison, because it
never ranks. A Team whose own window cannot be evaluated gets "cannot be checked yet" as a first-class
verdict rather than an absence, which is exactly what ADR-194 demands.

**Core trade-off**: nothing travels. No artifact, no screenshot, nothing a sceptic could be sent — the
conversion objective that C1 makes an honest secondary is served not at all (MARKETING **2**).

**Key risk — resolved after scoring, against it.** Its stated surface was ADR-127's team-settings advisory
channel. That mechanism **no longer exists**: `ValidationAdvisory.tsx` is absent from the whole frontend,
and the tree records why — *"US 5612 removed the advisory channel: the only advisory any connector ever
returned was withdrawn as unactionable at connection scope, so a field nothing writes was deleted rather
than kept for a caller that might one day appear"* (`ConnectionValidationResult.test.ts:23-26`); and
*"#5612 deleted `SuccessWith` and the Advisory pair it wrote to once they had no producer left; this rung
is built here rather than reviving them"* (`ServiceNowBoardVerdict.cs:37-41`). **R1 would have to rebuild
a channel this project deliberately deleted, against a shipped decision declining to revive it.** Its
score was **not** lowered on this news (see §6).

**Hire criteria**: a user would choose this if they think of Lighthouse as infrastructure that should tell
them when something is wrong, rather than as an analysis tool they visit.

### 3rd — E1 · "Sweep in a breath" — **4.10**

**Core idea**: delete the asynchrony, the entity, the notification, the tab and the dialog. One button,
sixteen results computed in one request, rendered in place as four small-multiple panels.

**Why it scores well**: **5 on CHEAP and 5 on LOAD-BEARING**, **4 on HONESTY** (four panels make four
statements rather than one statement about a coordinate system). It is M1's evidence view, promoted to the
default.

**Core trade-off**: DECISION-CHANGING **3** — it names a window but defers Apply to slice 02, so the user
reads an answer and then has to go and act on it somewhere else.

**Key risk**: the same performance assumption as M1 — and it is a strong one. `ForecastController.RunBacktest`
is **already a synchronous action** (`public ActionResult<BacktestResultDto> RunBacktest(...)`, no
`async`/`await`) running one `HowMany` plus two throughput reads inside the request; every
`TeamMetricsService` read goes through `GetFromCacheIfExists` and computes over Work Items already in the
database, so **no work tracking system is contacted**. Sixteen cells is a bounded multiple of something the
product already does in a request. Strong, but still an inference.

**Hire criteria**: a user who wants to look at the data themselves and distrusts a tool that summarises.

---

## 3. Recommendation

**M1, as *Forecast Reality Check*.**

The rationale is the matrix, not a preference. M1 wins on three of five criteria and is the only option
that simultaneously satisfies the two things the job analysis established as non-negotiable: the
irreducible function is *replay → compare → **adjust*** (so the output must reach the adjustment), and O3
scored **9.7 — over-served** (so the adjustment must be *enabled*, not automated). M1 lands exactly there:
it names the change and pre-fills the control, and a human presses the button.

**The three runners-up are not discarded; two of them are absorbed.** This is not a hedge — it is what the
criteria, read together, actually point at:

- **E1's small multiples become M1's disclosure view.** M1 already specifies this; it is now fixed rather
  than optional. The winner therefore carries the third-place option's honest rendering.
- **R1's discipline becomes M1's copy rule.** The sentence must read as a verdict on a region, never as a
  league table (§4.1).
- **S1's `coverage` framing becomes the cell definition.** Prophet's `coverage` is the field-standard name
  for "within range"; adopting it gives the nominal-rate comparison for free (§4.3).

**The critical weakness, stated plainly**: M1 is weakest on the highest-weighted criterion. A single point
of HONESTY moves it from first to third (`taste-evaluation.md` §6). The three requirements in §4 are what
would legitimately earn that point — **and they are requirements to be built, not a score to be assumed.**
If DISCUSS will not commit to all three, the recommendation changes to R1.

---

## 4. Three honesty requirements — non-negotiable for the recommendation to stand

Each traces to a citation, not to taste.

### 4.1 The verdict names a **region**, never a winner

> *"Anything between 30 and 90 days would have behaved about the same for this Team. Your current 30 days
> is inside that range. The 14-day window is not — it over-forecast in three of the four checks."*

Never *"60 days is best, apply it"*. Source: the quant plateau/needle heuristic (§3.2 of the research) and
Hansen's Model Confidence Set (§4.2), which returns a *set* of indistinguishable candidates by
construction. This is also what makes §1.1's **null result** expressible: when every window behaves alike,
the region is "all of them" and the verdict is "your setting is fine".

### 4.2 The artifact states its own denominator and its own non-comparability

Two sentences of permanent copy, not a tooltip:

> *"16 configurations were checked. Each one covers a different stretch of real time — they end today and
> reach back by their own length — so they are not repeated trials of one experiment and should not be
> ranked against each other."*

Source: Bailey et al.'s disclosure norm (§4.3 of the research) and West/Clark on overlapping-window error
correlation (§4.2).

### 4.3 A cell's state is defined as **coverage against a nominal rate**, and the percentile is printed

The "within range" state is `coverage` in the field's own vocabulary (Prophet `performance_metrics`), and
its honest companion is the nominal level: **an 85% forecast is supposed to be beaten about 15% of the
time. A cell that is never beaten is over-forecasting, not excellent.** The percentile the check uses is
printed on the artifact rather than implied.

Two things DISCUSS must decide here, both surfaced by the research:

- **Which percentile.** Brown's finding was 70th for short horizons, 85th for long. Lighthouse's
  `BacktestResultDto` already returns 50/70/85/95. **Recommendation: score against the 85th and print it**,
  matching the percentile the rest of the product leads with — but this is DISCUSS's call, and it is the
  axis that actually carried the signal.
- **The three-way verdict is a departure from the source.** Brown scored one-sided (over-delivery counts
  as correct). The Epic's under / over / within trichotomy is arguably *more* honest and matches weather
  verification's symmetric treatment of conditional bias — but it must not be described as "what Nick
  Brown did". Own the departure.

### 4.4 The disqualified cell

Per-cell data sufficiency composes with the shipped `forecast-minimum-data-guard` bar (at least 5 distinct
days with at least 1 completed Work Item) — **C5: no second bar is invented.** Because each cell carries
its own history window, sufficiency varies *within* one report.

**ADR-194 governs the rendering**: on this product's charts a blank region already means "too little
history", so a calm result and an unevaluable one must never render alike, and "leave it empty" is the
option that ADR already ruled out. In M1 the disqualified cells are named in the sentence itself — *"two of
the sixteen checks could not run: the 14-day windows hold fewer than five days with completed Work Items"*
— and in the disclosure view a panel that cannot be drawn says so in words where the chart would be.

The research confirms this is a **differentiator worth protecting**: ActionableAgile's documentation
carries no insufficient-data warning at all, and the only tool found that grades history completeness
(Monte Carlo Azure) is an unaffiliated open-source project.

---

## 5. What this means concretely — for Luna to write stories against

### 5.1 The name

| Layer | Name |
|---|---|
| **User-facing feature** | **Forecast Reality Check** (shortens to **"Reality Check"** in-app) |
| In-app placement label | a control inside the existing `InputGroup` titled "Forecast Backtesting" |
| **Internal codename** | **"The Full Monte"** — keep it. It credits the source method, promises a user nothing, and belongs in the commit scope, the ADO Epic title, and the launch post *with attribution* ("inspired by Nick Brown's *The Full Monte*"), which converts a borrowed title into a citation |
| Engineering slug | `epic-4172-forecast-backtest-sweep` — unchanged, already accurate |

Why not the alternatives: **"Forecast Calibration"** borrows a word whose standard (reliability diagrams
over many trials) this artifact cannot meet — use *"calibrated"* in body copy where it is earned, not as
the label. **"The Full Monte"** as a user-facing name claims completeness ("Full") on sixteen
non-comparable cells, which is the exact claim O4 forbids; it is also a gambling pun on a probabilistic
feature and it is Nick Brown's article title. **Runner-up if memorability is wanted: "Hindsight."**

**C6 extended**: "throughput" is itself a user-configurable term, so it cannot appear *in a feature name* —
it would render as the user's own word. Safe vocabulary: forecast, calibration, hindsight, replay,
accuracy, check, reality, sampling, history, percentile.

### 5.2 The endpoint

```
POST /api/latest/forecast/reality-check/{teamId}      (also /api/v1/…, per ForecastController's two routes)
[RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
```

- **Route style verified**: kebab-case segments are this codebase's convention — `my-summary`,
  `group-mappings`, `system-admins`, `bootstrap/system-admin` (`AuthorizationController.cs`). `reality-check`
  is idiomatic.
- **Read-only.** Same guard as the shipped `POST backtest/{teamId}`. **No new RBAC surface.** Applying the
  proposal goes through the existing Team settings update path, which already requires write rights
  (`canUpdateTeamData`).
- **Synchronous**, returning the whole result in one response — pending the §7 measurement.
- **Request body**: at most `{ applyFilterOverride: bool? }` (C3 — the forecast-throughput filter is a
  config option of the run, never a fifth sweep dimension). **No dates**: C4 fixes today as the end anchor,
  which removes the date picker entirely and is what makes one-click viable.
- **Response**: the verdict (region, plain-language sentence, recommended value, null-result flag), the
  configuration count (the denominator, §4.2), the percentile used, and sixteen cells each carrying its
  real date span, its forecast percentiles, its actual, its coverage state, and its sufficiency state.
- **Reuses unchanged**: `forecastService.HowMany`, `teamMetricsService.GetBlackoutAwareThroughputForTeam`,
  `GetThroughputForTeam`, `blackoutPeriodService.GetEffectiveBlackoutDays` / `CountWorkingDays`,
  `GetForecastThroughputStatus`, and `CreateForecastDtos`. **Nothing in the forecast engine changes.**
- `BacktestInputDto`'s validation rules (`ForecastController.cs:169-193`) are **not** reused as written —
  C4's fixed anchoring makes three of the four checks vacuous. The 14-day minimum window has no
  successor: the request carries no dates, and whether a short horizon can be evaluated is decided per
  check by the shipped sufficiency bar. *(Corrected in DISTILL, 2026-09-26: the horizons are 1, 2, 4
  and 8 weeks, so a "2-week floor" could not hold.)*

### 5.3 The entity

**None.** No new table, no migration, no `UpdateType` member, no queue work. This is the LOAD-BEARING
score, and it is the position ADR-209 records.

### 5.4 The surface

- **Where**: inside the existing `InputGroup title="Forecast Backtesting"` in `TeamForecastView.tsx`
  (:438-460), above the shipped date pickers. Not a new tab, not a dialog, not a global page.
- **Trigger**: one button, no configuration dialog. C4 removed the need for a date picker; the filter
  toggle already exists on that surface.
- **In flight**: the button becomes a spinner in place. Measured expectation is seconds.
- **How they learn it finished**: it is on screen. No notification, because there is nothing to notify
  about — this is what removing the asynchrony buys.
- **Previous report**: replaced. Nothing is retained, and the ADR says so deliberately.
- **Scope**: per-Team.
- **Visual form**: **small multiples** — four panels, one per history window, each showing its four
  horizons as a forecast range with the actual marked. **Not a heatmap and not a ranked list**: a matrix is
  a coordinate system and a ranked list is a league table, and C4 says the cells are neither. A practical
  corroboration: `@mui/x-charts` 9.0.1 is the only charts package installed and there is no
  `@mui/x-charts-pro`, so a Heatmap component is not available — and a sixteen-cell matrix needs no
  charting component at all.
- **Apply**: pre-fill and open the existing `ThroughputQuickSetting` in the `QuickSettingsBar` of the Team
  detail header (`TeamDetail.tsx:402-430`) with the recommended value. It already writes
  `throughputHistory` with validation from every Team tab. **This is a pre-filled open of a shipped
  control, not new machinery** — which is why Apply is MVP here and would not have been otherwise.

### 5.5 Suggested elephant-carpaccio slicing

Flux does not write stories; this is a starting shape for Luna, not a decision.

| Slice | Content | Why here |
|---|---|---|
| **01** | The endpoint returning sixteen cells, **and the timing measurement** (§7). No UI. Dogfooded against this project's own Lighthouse instance. | It carries the one decisive unknown. If the budget is blown, everything downstream changes, and finding out costs an hour. |
| **02** | The verdict logic: region-not-winner (§4.1), null-result handling (§1.1), denominator and non-comparability copy (§4.2), coverage-against-nominal (§4.3). Still no UI. | The honesty requirements are logic, not decoration. Building them second means they cannot be dropped under UI pressure. |
| **03** | The sentence and the button on the Forecast tab. Disqualified cells named in prose. | First user-visible value. |
| **04** | The disclosure: four small-multiple panels, with the per-panel "could not be checked" state. | The mitigation that earns the HONESTY point. |
| **05** | Apply — pre-filled `ThroughputQuickSetting`. | Depends on 03. |
| **06** *(candidate, not committed)* | The artifact that travels: a client-built Markdown/CSV one-pager, per the ADR-172 / ADR-162 export precedent. | This is folded-in option P1. It is the whole of the MARKETING criterion and the honest first step toward the emailable Report — **but it is a slice, not an abstraction.** |

---

## 6. Dissenting case — R1 · "Your setting, on trial" (4.15)

**The scoring almost chose this, and the dissent is serious rather than ceremonial.**

R1 scores **5 on HONESTY and 5 on DECISION-CHANGING** — the two criteria carrying 55% of the weight. It is
the only option in the set that **cannot** present a false comparison, because it never ranks anything: it
judges the configuration the Team already holds and offers one alternative only on failure. Two things can
be compared honestly in a way sixteen cannot.

**Three arguments for overturning the recommendation:**

1. **It needs no honesty mitigations.** M1's winning margin depends on §4's three requirements actually
   being built. R1 is honest by construction; there is nothing to forget under delivery pressure. If
   DISCUSS is not confident those requirements survive, R1 is the safer pick on the criterion that matters
   most.
2. **Research §1.1 strengthens it, not M1.** If the window axis genuinely carries no signal, then a
   feature whose primary output is "your current setting is fine" is telling the truth most of the time
   with no machinery at all — and a sixteen-cell sweep is an expensive way to reach the same sentence.
3. **The weighting is contestable.** At HONESTY 40% / DECISION 15%, the ranking inverts to E1 (4.20), R1
   (4.15), M1 (4.10). A defensible reading of O4's dominance (14.9, the highest of six) supports that
   weighting.

**Why it did not win**: **MARKETING SURFACE FITNESS 2** and **CHEAP 3**. A health chip is invisible outside
the product, and C1 (free tier) makes conversion an honest secondary objective with real weight — the Epic
is tagged Community for exactly that reason. R1 serves it not at all. And R1's feasibility rests on
ADR-127's advisory channel, whose delivery was split to Story #5627 with one objection open.

**The decisive cheap check — run, and it went against the dissent.** The question was *has Story #5627
shipped?*, because if it had, R1 would reach **4.35 and first place**. It did not, and the situation is
worse than "not yet": **Story #5612 deleted the advisory channel entirely**, and a later rung was
explicitly *"built here rather than reviving them"* (citations above). **The branch in which R1 overtakes
the recommendation does not exist.**

**R1's score was deliberately left unchanged at 4.15.** The channel was a convenience, not a requirement —
R1 can render its verdict in a card of its own, exactly as the winner does — and marking a losing option
down on a fact discovered *after* scoring would flatter the winner. The discipline against retroactive
adjustment cuts both ways. What the finding removes is R1's upside, not R1.

**Two further dissents, recorded briefly**: **E1** (4.10) is M1 minus the hiding — choose it if the team
judges that a Community feature aimed at convincing sceptics must show its working by default. **S1**
(3.85) has the best-evidenced visual in the entire research (the PIT / rank histogram is the published
honest answer to "was my probabilistic forecast calibrated?") and the highest MARKETING score; it lost on
DECISION-CHANGING (2) because it answers in a unit the user did not ask for. **If M1's disclosure view is
ever rebuilt, S1's landing-position strip is the thing to build it as.**

---

## 7. Risks and open questions for DISCUSS

| # | Item | Why it matters | Cost to resolve |
|---|---|---|---|
| **R-1** | **Does the sixteen-cell run fit an acceptable request budget?** | The recommendation's load-bearing assumption. Strong evidence for (§2, E1) but it is an inference. | **One hour.** Slice 01. If it fails, the fallback is the existing `UpdateQueueService` — with the ADR-195 caveat in §8. |
| **R-2** | ~~Has Story #5627 (ADR-127 advisory channel) shipped?~~ **RESOLVED 2026-09-22 — no, and Story #5612 deleted the channel altogether.** | Closed the branch in which the dissenting option overtakes the recommendation. Also a standing caution: **ADR-127 describes a mechanism that is no longer in the tree** — anyone reaching for it in a later wave should read #5612 first. | Done. |
| **R-3** | **Which percentile does a cell score against?** | §1.1: this is the axis that actually carried the signal. Recommended: the 85th, printed. | A decision, not research. |
| **R-4** | **Is the three-way verdict kept?** | It departs from the source method (§4.3) and must not be attributed to it. | A decision. |
| **R-5** | **Confirm the source article from medium.com directly.** | Everything in research §1 is mirror-sourced. | One human page-load, before anything is quoted publicly. |
| **R-6** | **`ThroughputHistory` defaults disagree with themselves** — the entity defaults to **30** (`Team.cs:17`) while `CreateTeamWizard.tsx:35` and `EditTeam.tsx:80` both seed **90**. | A separate, real defect, found while researching this Epic. It also means no one currently knows which default is right — which is an argument *for* this feature. | **Raise as its own ADO item.** Do not fold it into this Epic. |
| **R-7** | **Rolling-origin evaluation is what every adjacent field does by default** (Prophet `cutoffs`, Darts `stride`, fpp3). C4 (single anchor) is respected throughout and is **not** reopened here. | Its cost is now measured rather than assumed. | Record as the strongest candidate for a later slice, if the honesty mitigations prove insufficient in use. |

---

## 8. ADR-209 — the framing

**Next free number confirmed: ADR-207**, since renumbered to 209 on 2026-09-24, because story 6053 had taken 207 and 208 in a parallel DESIGN wave (206 ADRs in `docs/product/architecture/`, highest `adr-206`).

### Proposed title

> **ADR-209: A report is a response, not a record — the calibration check stores nothing, and what a
> Report will be is decided when the second one arrives**

### The decision it records

**Shape (a) on the commitment spectrum: no abstraction.** The Forecast Reality Check computes in the
request and returns its result. No `Report` entity, no `Kind`, no status, no payload column, no runner
registry, no `UpdateType` member, no migration.

This is the project's own SOLUTION EFFICIENCY rule applied at its first step — *skip (YAGNI)* — and the
LOAD-BEARING criterion (15%) is where it earned its place in the matrix.

### What the ADR must actually decide, beyond "not yet"

An ADR that only says "we didn't build it" is worthless. It has to record the *shape the second Report
will find*, so that the third does not have to renegotiate it:

1. **That the result is deliberately not persisted**, and the consequence accepted: "has this Team ever
   been checked?" is unanswerable, and emailing (#4753, #4755) needs something durable that does not
   exist yet.
2. **The named revisit trigger** — *the second Report kind*, not a date. Two real examples beat one
   imagined one; six parked Epics of which none is scheduled are a roadmap, not a commitment.
3. **The six questions the eventual Report ADR will have to answer**, written down now while they are in
   view: is a Report an entity or a projection; is its payload opaque JSON, typed per-kind columns, or a
   blob reference; does the runner reuse the update queue, sit beside it, or run in-request; is completion
   a domain event or a direct SignalR push; what is the retention policy and who deletes; and is a Report
   scoped to a Team, to any entity, or global.
4. **The runner question, with the measurement behind it** — the sharpest thing in scope, and it is about
   *this product's queue*, not about reports in general:

   `UpdateQueueService` holds **one** `Channel.CreateUnbounded` with **one** sequential reader. ADR-195's
   three lanes shipped and were reverted the same day (`f216ef558`) because concurrent refreshes destroyed
   Feature ownership. ADR-195's own field report records a Portfolio refresh running **77.7 minutes** and
   starving every Team refresh for **3h38m**. So a calibration run placed on that queue would wait behind
   whatever is refreshing, *and* would block refreshes behind it. **A user-initiated, short, read-only
   computation that contacts no work tracking system is a different kind of work from a tracker sync, and
   the ADR should say so** — which is also the argument for running it in-request in the first place.
5. **The forward-compatibility constraint** that came out of a set-aside option: **do not hard-code a Team
   into the endpoint's result shape.** "Sweep every Team at once" is a plausible next unit of work, and
   nothing here should make it expensive.

### Existing ADRs it touches

| ADR | Relationship |
|---|---|
| **ADR-195** (update queue is three lanes) | Cited as **reverted**; the single-lane reality is the evidence for running in-request. Also the source of the starvation field report. |
| **ADR-194** (SLE risk is a number per item, never a background ladder) | The governing precedent for §4.4 — on this product's charts a blank region already means "too little history", so an unevaluable result and a calm one must never render alike. Also the precedent for *refusing a rendering whose grammar overclaims* (retiring a heatmap-like ladder), which is the direct argument against the matrix. |
| **ADR-039** (forecast data-sufficiency backend signal) | The shipped bar this composes with, per C5. Not amended. |
| **ADR-172** (a Delivery exports one settled table the caller builds) + **ADR-162** (export header block as a generic toolbar input) | The export precedent. It is **client-side**; there is no server-side document renderer, which is what makes slice 06's one-pager cheap and the "emailable Report" expensive. |
| **ADR-127** (the advisory channel reaches team settings) | Not used by the recommendation. **Flagged as stale**: it describes a mechanism that no longer exists — Story #5612 deleted the `Advisory`/`AdvisoryCode` pair and `SuccessWith`, `ValidationAdvisory.tsx` is gone, and a later rung was explicitly built rather than reviving them. ADR-127 carries no note saying so. **Worth a status correction in its own right**, independently of this Epic. |
| **ADR-181 / ADR-182 / ADR-186** (update activity as a read through the status store; update moments; live header summary) | The prior art a future Report status surface should reuse rather than reinvent — `UpdateStatus` already carries `QueuedAt`/`StartedAt` and `UpdateController` already serves "what is running / what is waiting / what is it waiting for / stop that one". |
| **ADR-046** (survey submission and team notification) | The nearest existing precedent for an asynchronous completion notification; worth reading before #4754 is designed, and **not** touched by this ADR. |
| **ADR-145** (write-back notification suppression visibility) | Precedent for how this product makes a suppressed/absent signal visible rather than silent — the same instinct §4.4 applies to a disqualified cell. |

**Not touched**: ADR-095 (migration before API) — there is no migration, which is the point.

**Verification note**: `ctx_search` skips `brief.md` for size, so no existing `Report` concept could be
confirmed absent from the architecture brief by search. The 206 ADR titles contain none. **DESIGN should
confirm before ADR-209 is written.**

---

## 9. Handoff to DISCUSS

**Decision statement:**

> **Proceed with M1, shipped as *Forecast Reality Check*: a synchronous, one-click,
> no-new-entity calibration check in the existing "Forecast Backtesting" group on the Team's Forecast tab.
> It answers with a plain-language verdict naming a *region* of acceptable sampling windows — able to say
> "they all behave the same, yours is fine" — with sixteen results one click behind it as small multiples,
> and an Apply control that pre-fills the `ThroughputQuickSetting` already in the Team detail header.
> ADR-209 records that there is no Report abstraction yet, why, and what the second Report will have to
> decide.**
>
> **Assuming**: (R-1) the sixteen-cell run fits an acceptable request budget — measured in slice 01 before
> any UI is written. *(R-2, the other standing assumption, has been **resolved**: Story #5627 did not ship
> and Story #5612 deleted the advisory channel the dissenting option needed, so that option cannot
> overtake this one.)*
>
> **Conditional on**: the three honesty requirements in §4 being built, not asserted. They are what moves
> the recommendation's weakest score from 3 to 4 on the criterion carrying the most weight. **If DISCUSS
> will not commit to all three, the decision changes to R1 · "Your setting, on trial".**

Luna has, concretely: the **name** (§5.1), the **endpoint and its guard** (§5.2), the **entity** — none,
and why (§5.3, §8), the **surface, trigger, in-flight state, visual form and apply path** (§5.4), a
**six-slice carpaccio shape** (§5.5), **seven open risks with costs** (§7), and the **ADR-209 framing with
the eight ADRs it touches** (§8).
