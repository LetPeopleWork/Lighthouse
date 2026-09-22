# Competitive Research — epic-4172-forecast-backtest-sweep

**Wave**: DIVERGE · Phase 2
**Conducted by**: Nova (`nw-researcher`), commissioned by Flux · **Date**: 2026-09-22
**Sources**: 32 external + 5 local · **Overall confidence**: Medium-High

> Read §1.4 and §4 first. Between them they say that **the sweep as specified keeps the axis the source
> study found carried no signal, and drops the axis that carried all of it** — and that sixteen
> configurations scored against a typical Team's history is, by the quant literature's own arithmetic,
> already inside the regime where the winning cell is expected to be noise. Both findings are actionable
> in DIVERGE. Both independently corroborate the ODI result that **O4 outranks O1**.

---

## 1. The source method — Nick Brown, "The Full Monte" (ASOS Tech Blog, Jan 2024)

**Provenance, stated plainly.** medium.com returns 403 to every plain fetch, `r.jina.ai` returns 401, and
`freedium.cfd` no longer resolves. The article was read through the **readmedium.com** mirror, fetched
twice with different extraction prompts. Quotes below are therefore **mirror-sourced, not read off
medium.com directly** — confidence **High** for structure, **Medium-High** for exact wording. Three
independent checks support them: internal arithmetic consistency (below), corroboration of the headline
finding from Brown's own 2025 ProKanban post, and consistency between the two extractions.

**Action for DISCUSS**: a human should open the Medium URL once to confirm the four horizon values and
the three percentiles before any of this is quoted publicly.

### 1.1 What was actually swept

| Axis | Values in the study | Values in Epic 4172 |
|---|---|---|
| Forecast horizon | **2, 4, 8, 12 weeks** | 2, 4, 6, 8 weeks |
| Historical throughput window | **6, 8, 10, 12 weeks** | 2 weeks, 30, 60, 90 days (≈ 2, 4.3, 8.6, 12.9 weeks) |
| **Percentile** | **50th, 70th, 85th** | **absent — not an axis** |
| Teams | **25 ASOS teams** | one Team per run |

> "Historical data of 6, 8, 10 and 12 weeks was used to forecast for a given period (in this example, the
> next 2 weeks) the number of items a team would complete."
> "The forecasts to be used to compare would be the 50th, 70th and 85th percentiles."

**The arithmetic validates the mirror.** 25 teams × 4 horizons × 4 windows = 400 forecasts per percentile;
× 3 percentiles = 1,200. Both figures appear verbatim in the article ("361 out of 400", "243 out of 1200").

It is a **How Many** study, not a **When** study — which matches Lighthouse's shipped
`forecastService.HowMany` path exactly. Each forecast ran **10,000 simulations** — the same trial count as
`ForecastSimulationLimits.Default`. Brown sampled **weekly** throughput and notes himself that
ActionableAgile samples **daily**; Lighthouse samples daily, like ActionableAgile.

### 1.2 How Brown scores a cell — and it is not what the Epic specifies

> "Any time a cell is green this means that the forecast was correct (i.e. the team completed the exact
> OR more than number of items). Any time the cell is red this means that the forecast was incorrect
> (i.e. the team completed less than the number of items forecast)."

Plus a second, orthogonal shading for **margin of error**: within ±10%, 10-25%, beyond 25%.

**This is a one-sided test. Over-delivery scores as correct.** Brown has two states crossed with a
magnitude band — not a three-way under / over / within-range.

**Consequence for this Epic**: the requested under-forecast / over-forecast / within-range trichotomy is
**a deliberate departure from the source method, not an implementation of it.** It is arguably *more*
honest — a percentile forecast that is systematically beaten is mis-calibrated too, and weather
verification treats over- and under-forecasting as symmetric conditional biases (§3.4). But it must never
be presented as "what Nick Brown did", and DISCUSS should own the departure explicitly.

### 1.3 Results and conclusion

| Percentile | Correct | Rate |
|---|---|---|
| 85th | 361 / 400 | **90%** |
| 70th | 336 / 400 | **84%** |
| 50th | 270 / 400 | **68%** |

> "for short term forecasts, the 70th percentile is your best bet for the best balance of accuracy vs risk"
> "For long term forecasts, the 85th percentile is definitely the way to go"

Secondary and counter-intuitive: **long-horizon forecasts (8-12 weeks) were more accurate than short ones
(2-4 weeks)** — the opposite of the textbook expectation that error grows with horizon (§3.1).

### 1.4 The finding that should change the feature

**The history-window axis produced almost no signal**: incorrect rates of **19-23%** across 6, 8, 10 and
12 weeks. Brown restates it himself, eighteen months later, in his own words:

> "I found that when choosing between 6-12 weeks historical data, the amount of historical data does not
> play a significant factor in the outcomes of forecast accuracy."
> — Nick Brown, *Forecasting in the Real World: What Monte Carlo Is (and Isn't)*, ProKanban, 2025

Three consequences, each of which is a DISCUSS decision rather than a conclusion drawn here:

1. **Epic 4172 keeps the null axis and drops the signal axis.** Percentile moved the correct-rate from
   68% to 90%; the window moved it by about four points of noise. A horizon × window sweep therefore has
   a real prospect of rendering sixteen near-identical results and then "recommending" whichever one won
   by chance — **manufacturing the exact O4 harm the design is supposed to avoid.**
2. **Lighthouse's window range reaches below Brown's floor.** His shortest window was 6 weeks; the Epic's
   shortest is 2 weeks. The 2-week cell is genuinely outside his tested range and is where an effect is
   most plausible — so the sweep is still worth running, but its most informative cell is the one most
   likely to fail the data-sufficiency bar on a low-volume Team.
3. **A null result must be a first-class output.** "Your window barely matters in this range, and here is
   the evidence" is a defensible, honest, differentiated answer — and it is the one the source study
   actually supports. **The feature must be able to say it.** A direction that is architecturally obliged
   to name a winner (a ranked list, a heatmap with a gold cell) cannot.

### 1.5 The vocabulary Brown actually uses

- Primary nouns: **"accuracy"**, **"forecast accuracy"**, **"margin of error"**, **"correct/incorrect"**.
- He reaches for **"calibration"** and **"validation"** conceptually, and explicitly names then **declines
  the Brier Score** for accessibility — *"The study prioritizes practical accessibility over statistical
  complexity"*.
- He cites **Nate Silver's "all models are wrong"** as the framing device.
- He does **not** use "backtest", "sweep", "grid", "heatmap", "rolling origin" or "hindcast".
- "The Full Monte" is a pun on the film / "the full monty" — see §5.

**Unverified**: whether the article publishes per-cell tables per team or only aggregates. No Part 2
exists; the nearest sequel is the 2025 ProKanban post, which re-uses throughput from 11 of the original
25 teams.

---

## 2. What the field calls this — and what it actually ships

| Product | Capability exists? | What it is called | Citation |
|---|---|---|---|
| **ActionableAgile / AA for Jira / AA for ADO** (Vacanti, 55 Degrees) | Window selection **yes**; back-test / calibration **no** | "**Throughput Basis**" / "**Date Range**" on the Monte Carlo charts; defaults to *all* loaded history; presets incl. "Last 30 Days", "Last Quarter" | [How Many](https://55degrees.atlassian.net/wiki/spaces/ActionableAgile/pages/2407399482) · [When](https://55degrees.atlassian.net/wiki/spaces/ActionableAgile/pages/2408972289/Monte+Carlo+When) · [ADO](https://55degrees.atlassian.net/wiki/spaces/AA4A/pages/2407694337/Monte+Carlo:+How+Many) |
| **Nave** | Window selection **yes**; back-test **no** | "**relevant data**" / "**rolling window of data**"; guidance qualitative, not numeric | [How Much Data Do You Need](https://getnave.com/blog/relevant-forecasting-data/) |
| **FlowViz** (Brown's own Power BI template) | **Yes — the only agile tool found that ships one** | "**MCS Backtesting**" — an undocumented, hidden report page | [FlowViz wiki, 3. Forecast](https://github.com/nbrown02/FlowViz/wiki/3.-Forecast) |
| **Businessmap / Kanbanize** | Window selection **yes**; back-test **no** | "**past time frame**" / "**Requested after**" in Dataset Configuration | [Monte Carlo – How Many?](https://knowledgebase.businessmap.io/hc/en-us/articles/115000974592-Monte-Carlo-Simulation-How-Many) |
| **Kanban Zone** | **UNVERIFIED** | A native Monte Carlo feature could not be confirmed to exist at all; results showed only third-party tools consuming Kanban Zone *exports* | search only — do not assert either way |
| **Jira Monte Carlo plugins** (Broken Build, Agile Gadgets, agilemontecarlo.com) | Forecasting yes; back-test **no evidence** | plain "Monte Carlo simulation chart" | [Broken Build](https://www.brokenbuild.net/apps/agile-monte-carlo-charts) · [Agile Gadgets](https://marketplace.atlassian.com/apps/3709319691/monte-carlo-simulations-for-scrum-kanban-agile-gadgets) |
| **Jira Advanced Roadmaps / Plans** | **No Monte Carlo** | "**capacity and velocity**" scheduling — deterministic | [Atlassian](https://support.atlassian.com/jira-software-cloud/docs/what-is-advanced-roadmaps/) — *Medium confidence; Atlassian's page is silent on methodology* |
| **Monte Carlo Azure** (OSS, GaranceRichard) | No back-test, but **the closest thing to a data-sufficiency grade found anywhere** | grades history as *"non fiable, fragile, incertain ou fiable"* on **complétude** and **continuité**; asks for *"au moins six semaines exploitables"* | [github.com/GaranceRichard/monte-carlo-azure](https://github.com/GaranceRichard/monte-carlo-azure) |
| **Troy Magennis / FocusedObjective** | Spreadsheets + a "How Many Samples Do I Need?" notebook | "**throughput samples**", "**historical simulation**", "**sample size**" | [focusedobjective.com](https://focusedobjective.com/) · notebook 429'd, unread |
| **Vacanti / ProKanban** | No tool feature; the practice exists informally | "**re-executed the Monte Carlo Simulation**", "**sanity check**", "**simulating the conditions as of [date]**" | [ProKanban case study](https://www.prokanban.org/blog/https-prokanban-org-blog-monte-carlo-simulations-accuracy-and-unplanned-work-a-case-study) |

### 2.1 Three findings that matter more than the table

**(a) There is essentially no competition, and the one competitor is the source author's own tool.**
FlowViz ships a hidden page:

> "a hidden page called MCS Backtesting" where users can "compare what percentiles came close to reality"

Note what it compares: **percentiles** against reality — the axis Brown's study found mattered, not the
window axis. Corroborating its hidden status: FlowViz's release notes from July 2022 through August 2025
never mention backtesting, accuracy or validation, and neither does the FAQ page. So it is real but
undocumented, unadvertised, single-axis, and requires Power BI. **Confidence: Medium** — one wiki page
read twice, with two silent pages as weak corroboration.

**(b) Nobody sweeps, and nobody recommends a window.** No product in the table runs more than one
(horizon, window) pair, and none renders a two-dimensional grid of calibration outcomes. Every tool
exposes a date-range control and leaves the choice to the user. The closest anyone gets:

- **Nave**: *"if your delivery system is stable, you won't need more than 20 or 30 completed items"*, and
  *"only use your data up to the point where the mean cycle time remains consistent"* — a qualitative
  stopping rule.
- **ActionableAgile**: recommends a date range "that reflects conditions similar to upcoming work" —
  advice, not computation. **Its documentation carries no insufficient-data warning at all.**
- **Monte Carlo Azure**: validates the completeness of the window you chose; explicitly does not propose one.
- **ProKanban / Vacanti**: the 5/11-sample rule — *"With 5 samples we are confident that the median will
  fall inside the range of those 5 samples… With 11 samples… there is a 90% probability that every other
  sample will fall in that range"*. A floor, not a recommendation.

This **confirms against the market** what `job-analysis.md` §6 claimed against the workaround: the sweep
has no manual and no commercial equivalent. The feature creates capability rather than saving effort.

**(c) Lighthouse is already ahead of the field on the insufficiency flag.** The shipped guard — *"No
throughput history for Team Meridian. Forecast unavailable until that team has data."* — is stronger and
more automatic than anything the competitors ship; ActionableAgile has no such warning. **Constraint C5
(compose with the shipped bar, never invent a second) is therefore not merely a consistency rule — it
protects a genuine differentiator.**

### 2.2 The vocabulary, ranked by currency in this field

There is **no settled term**.

1. **"accuracy" / "forecast accuracy"** — Brown's own word, in both posts. Most used, least precise.
2. **"calibration"** — used by Brown conceptually; the standard term everywhere *outside* agile tooling
   (weather, ML). No agile tool uses it as a feature name.
3. **"sanity check"** / **"re-execute the simulation"** — ProKanban's informal phrasing.
4. **"backtesting"** — used by FlowViz ("MCS Backtesting") and by **Lighthouse itself** (the shipped
   `Forecast Backtesting` group). Borrowed from quant finance.
5. **"Throughput Basis" / "date range" / "sample window"** — how the *input* is named. Magennis's
   "throughput sample" and ActionableAgile's "Throughput Basis" are the two house styles; Lighthouse's own
   domain word is `ThroughputHistory`, surfaced as **"Throughput History (days)"**, default 30.

---

## 3. Non-obvious alternatives — the same job, solved in other categories

### 3.1 Time-series forecasting libraries — the closest structural analogue

**Meta/Facebook Prophet** maps almost one-to-one onto this feature. `cross_validation()` implements
*"simulated historical forecasts"*: it *"select[s] cutoff points in historical data, fit[s] the model only
to data before each cutoff, then compar[es] forecasted to actual values"*.

| Prophet parameter | Meaning | Default | Lighthouse analogue |
|---|---|---|---|
| `horizon` | forecast length | — | forecast horizon (2/4/6/8 wk) |
| `initial` | size of first training period | **3 × horizon** | throughput history window |
| `period` | spacing between cutoffs | **half the horizon** | **no analogue — Lighthouse has one cutoff: today** |

Two things worth taking:

1. **`initial` defaults to 3 × horizon.** Prophet's own default *couples* the training window to the
   horizon rather than treating them as free independent axes. That is a defensible prior for the
   recommendation, and it flags a real oddity in the Epic's grid: a 2-week history window under an 8-week
   horizon is a cell Prophet's designers would never generate.
2. **`coverage` is one of the six metrics `performance_metrics()` reports** — literally the fraction of
   actuals that fell inside the predicted interval. **That is precisely the "within range" cell state, and
   `coverage` is the field-standard name for it.** Its honest companion is the *nominal* level: an 85%
   interval is supposed to be beaten about 15% of the time. A cell that never misses is **over-forecasting,
   not excellent.**
   — [Prophet: Diagnostics](https://facebook.github.io/prophet/docs/diagnostics.html)

**Darts** uses `historical_forecasts()` / `backtest()` with `stride`, `retrain`, and a choice of
**expanding window** vs **fixed `train_length`** — "how much history" is a first-class knob with two named
regimes. — [Darts](https://unit8co.github.io/darts/)

**Hyndman & Athanasopoulos, *Forecasting: Principles and Practice*** gives the canonical name and picture:

> "sometimes known as 'evaluation on a rolling forecasting origin' because the 'origin' at which the
> forecast is based rolls forward in time"
> "there are a series of test sets, each consisting of a single observation."

and, critically for the chart: accuracy is computed **separately for each horizon h and plotted against
h** — the accepted visual is *error as a function of horizon*, **a line, not a ranked grid**. The textbook
shows error increasing with horizon; **Brown's study found the opposite**, which is itself worth saying in
the product copy. — [fpp3 §5.10](https://otexts.com/fpp3/tscv.html)

### 3.2 Quant backtest frameworks — the parameter-sweep heatmap, and how to read one honestly

This is where the *visual* precedent lives, and the field has a crisp, teachable rule:

> Robust strategies show **flat plateaus or gentle hills**. Overfit strategies show parameter surfaces
> that "look like needles, with chosen values sitting on narrow spikes of high performance surrounded by
> cliffs, where shifting any parameter slightly causes the strategy to collapse."

**Design rule**: *a broad plateau of good-enough cells is more trustworthy than a single brilliant cell.*
A recommendation should therefore **name a region, not a winner** — "anything from 30 to 90 days behaves
the same; pick 60" beats "60 is best".

**This is the single most transferable idea in the whole report. It resolves the tension between O4 and O1
without refusing to answer either.** Attested consistently across QuantInsti, IBKR Quant, Quanthop and
ClearEdge — **Confidence Medium-High**: many independent sources agree, but they are practitioner blogs,
not peer-reviewed. — [QuantInsti](https://blog.quantinsti.com/walk-forward-optimization-introduction/) ·
[IBKR Quant](https://www.interactivebrokers.com/campus/ibkr-quant-news/the-future-of-backtesting-a-deep-dive-into-walk-forward-analysis/)

**vectorbt** ships exactly this as a product feature — brute-force parameter sweeps rendered as heatmaps,
with walk-forward splitting as the stated defence, and its own framing is that the sweep *"makes
overfitting tempting"*. The tool warns about the thing the tool does.
— [vectorbt](https://github.com/polakowo/vectorbt) · Confidence Medium.

### 3.3 ML hyperparameter-sweep UIs — how a 2-D grid is actually visualised

**Neither Weights & Biases nor Optuna leads with a heatmap.** Both lead with **parallel coordinates**.

- **W&B** ships three defaults: a **parallel coordinates plot** (*"summarizes the relationship between
  large numbers of hyperparameters and model metrics at a glance"*), a **scatter plot**, and a **parameter
  importance plot** (identifies which hyperparameters are *"the best predictors of… desirable values of
  your metrics"*). **Notably, the docs give no guidance on interpreting the grid and carry no single-split
  warning.** — [W&B: Visualize sweep results](https://docs.wandb.ai/guides/sweeps/visualize-sweep-results/)
- **Optuna** provides `plot_contour` (2-D parameter-vs-objective — the closest to the Epic's grid),
  `plot_parallel_coordinate`, **`plot_slice`** (how *one* parameter moves the objective, marginally), and
  `plot_param_importances`. — [Optuna visualization](https://optuna.readthedocs.io/en/stable/reference/visualization/index.html)

**Transferable idea, and it is a big one**: `plot_slice` / parameter-importance is the direct answer to
"which axis actually matters?" — and given §1.4, the answer may well be *neither, much*. **A marginal view
("here is the effect of horizon; separately, here is the effect of window") may be a more honest primary
rendering than the sixteen-cell joint grid**, with the grid demoted to a drill-down.

### 3.4 Weather / probabilistic-forecast verification — the accepted honest visual

The most mature answer to "was my probabilistic forecast well calibrated?", and it is unambiguous. From
the WMO WWRP/WGNE Joint Working Group on Forecast Verification Research:

- **Reliability diagram** — *"plots the observed frequency against the forecast probability"*. Reliability
  is proximity to the diagonal; **curves below the diagonal indicate over-forecasting, above indicate
  under-forecasting**. The field's term of art for the Epic's red/blue cells is **conditional bias**.
- **Rank histogram / Talagrand diagram**, and its continuous cousin the **PIT histogram** — *"checks where
  the verifying observation usually falls with respect to the ensemble forecast data"*. **Flat = correct
  spread; U-shaped = insufficient spread; dome-shaped = excessive spread; asymmetric = bias.** For a Monte
  Carlo throughput ensemble this is *directly computable*, uses every past period, ranks nothing, and
  diagnoses over- vs under-confidence in one picture. **This is the published basis for option S1.**
- **Brier score** — *"measures the mean squared probability error"*, decomposing into reliability,
  resolution and uncertainty. Brown considered it and dropped it for accessibility.
- **Two warnings that land squarely on this feature**: results are *"naturally more trustworthy when the
  quantity and quality of the verification data are high"*, and stratifying into subsets *"helps to tease
  out forecast behavior in particular regimes"* — **but subsets must contain "enough samples to give
  trustworthy verification results."** A sixteen-cell grid on one Team's history is stratification into
  sixteen subsets of size roughly one.
  — [WWRP/WGNE Forecast Verification](https://jwgfvr.github.io/forecastverification) · High authority ·
  [Dimitriadis, Gneiting & Jordan, arXiv:2008.03033](https://arxiv.org/pdf/2008.03033)

---

## 4. The non-comparability problem — what the literature says

The hazard, restated: every cell's window **ends at today**, so the 8-week cell scores a different
real-world period than the 2-week cell; adjacent cells share most of their data; and "best cell" is chosen
from sixteen correlated, non-exchangeable trials. The literature names and corrects every part of this.

### 4.1 The corrective convention is *many cutoffs*, not one

Rolling-origin evaluation exists precisely because a single origin conflates *model quality* with *which
period you happened to land on*. fpp3: *"there are a series of test sets"*; the origin **rolls**. Prophet
makes it structural — `cutoffs` is a **list** and `period` defaults to half the horizon. Darts exposes
`stride` for the same reason.

> **The sharpest single statement available**: *every serious implementation of this idea in every
> adjacent field evaluates at multiple origins. Anchoring at one origin is the thing those conventions
> exist to prevent.*

**This does not reopen constraint C4**, which was resolved with the user this session and is respected
throughout. It does mean C4's cost is now **measured rather than assumed**, and it sets the bar the
mitigations in §4.4 have to clear. Recorded for DISCUSS as the strongest argument for a possible later
slice, not as a challenge to the decision.

### 4.2 Overlapping windows make the errors correlated

The precise statistical statement of the hazard:

- **West (1996)** and **Clark (1999)** established that *overlapping forecast horizons induce serial
  correlation* in the sequence of forecast errors.
- Consequence: *"a plain paired t-test on that sequence understates its true variance"*; the statistic
  comes out roughly √H too large.
- *"When loss differentials exhibit substantial autocorrelation — as is common with direct multi-step
  forecasts at h ≥ 2, where overlapping forecast horizons mechanically induce serial dependence — the
  effective sample size is smaller than the nominal one."*
- Standard fixes: the **Diebold-Mariano test** (extended for overlapping windows), **block bootstrap**,
  and — for comparing more than two candidates — **Hansen's Model Confidence Set**, which supplies the
  multiple-comparison correction and, tellingly, **returns a set of statistically indistinguishable models
  rather than a single winner.**
  — [Grant, *Journal of Forecasting*](https://onlinelibrary.wiley.com/doi/10.1002/for.70150) ·
  Medium-High confidence (abstract level) · a concrete instance in the wild:
  [skfolio #331](https://github.com/skfolio/skfolio/issues/331)

> **The Model Confidence Set is the statistically correct shape of this feature's output.** Not "cell X
> wins" but "cells {X, Y, Z} cannot be distinguished; cells {P, Q} are ruled out." That is the same answer
> the quant plateau heuristic (§3.2) gives by eye, arrived at formally — and it satisfies O4 while still
> answering O1.

### 4.3 Selecting the best of N configurations on one sample — Bailey & López de Prado

This literature speaks *directly* to "sweep a grid, then recommend the winner".

- **Backtest overfitting**: *"high simulated performance is easily achievable after backtesting a
  relatively small number of alternative strategy configurations… The higher the number of configurations
  tried, the greater is the probability that the backtest is overfit."*
- The expected maximum **grows with the number of trials even when every candidate is pure noise**: a toy
  strategy with seven binary parameters (N = 128) yields an expected maximum Sharpe above 2.6 from nothing.
- **Minimum Backtest Length (MinBTL)** — the arithmetic that lands hardest here:
  > *"if only five years of data are available, no more than forty-five independent model configurations
  > should be tried or one is almost guaranteed to produce strategies with an annualized Sharpe ratio
  > in-sample of 1 but an expected Sharpe ratio out-of-sample of zero."*

  **Scale that down.** A Lighthouse Team typically has *months*, not years, of throughput, and a 90-day
  window is one quarter. **Sixteen configurations against six to twelve months of history is, on this
  yardstick, already in the regime where the best cell is expected to be noise.** This is not a stretched
  analogy — it is the same operation: grid-search a configuration space against one finite historical
  sample and report the argmax.
- **Probability of Backtest Overfitting (PBO)**, via **Combinatorially Symmetric Cross-Validation** —
  a cheap, implementable honesty metric.
- **Deflated Sharpe Ratio** — *"corrects for… selection bias under multiple testing"* by adjusting the
  significance threshold **by the number of trials**.
- The disclosure norm: *"reporting only the best backtest result without disclosing how many strategy
  configurations were evaluated represents a form of selection bias."*
  — [Bailey, Borwein, López de Prado & Zhu, *Notices of the AMS* 61(5), 2014](https://www.ams.org/notices/201405/rnoti-p458.pdf)
  (peer-reviewed, High) · [Deflated Sharpe Ratio](https://www.davidhbailey.com/dhbpapers/deflated-sharpe.pdf) ·
  [PBO, SSRN 2326253](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2326253) (403; abstract-level only)

### 4.4 What this converts into — five conventions, ordered by how cheaply they buy honesty

| Convention | Source | What it looks like here |
|---|---|---|
| **Name the region, not the winner** | quant plateau/peak (§3.2); Hansen MCS (§4.2) | "30-90 days all behave the same; 14 days does not" — never a single gold cell |
| **State the denominator** | Bailey et al. disclosure norm (§4.3) | "16 configurations were evaluated" printed on the artifact itself |
| **Compare against the nominal rate** | Prophet `coverage` (§3.1); WMO reliability (§3.4) | an 85% forecast *should* be beaten ~15% of the time; a cell that never misses is over-forecasting, not excellent |
| **Say the cells are not trials** | West / Clark overlapping-window correlation (§4.2) | explicit copy: they share data and cover different periods |
| **Roll the origin** | fpp3; Prophet `cutoffs`; Darts `stride` (§4.1) | **out of MVP scope per C4** — recorded as the strongest candidate for a later slice |

And two renderings the literature would prefer over a ranked heatmap: **a reliability diagram / rank
histogram** (§3.4 — uses all history, ranks nothing) and **marginal slice plots per axis** (§3.3 —
answers "does the window axis matter at all?", which per §1.4 may well be *no*).

---

## 5. Naming

**Three constraints, one of which was not in the brief.**

- **C6 as given**: no "Epic", "Initiative" or "Story".
- **C6 extended — found by the research and it matters.** Under this project's own terminology rule,
  **"Throughput" is itself user-configurable** (Settings → Terminology), along with Team, Delivery, Cycle
  Time, WIP, Blocked and SLE. **A feature name containing any of those words renders as the user's word
  and is therefore unsafe.** This rules out the otherwise-obvious "Throughput Window Finder" — and it
  required a correction to one option's name candidate in `options-raw.md`. Safe vocabulary: *forecast,
  calibration, hindsight, replay, accuracy, check, reality, sampling, history, percentile*.
- **Collision with the shipped product**: Lighthouse already ships a UI group called **"Forecast
  Backtesting"**. Anything of the form "Forecast Backtest(ing) …" is weak as a user-facing name, though it
  remains correct as the engineering slug — which already exists as `epic-4172-forecast-backtest-sweep`.

### Candidates

| # | Name | What it promises | Honesty risk | Marketing fitness | Taken in the field? |
|---|---|---|---|---|---|
| 1 | **Forecast Reality Check** | "we compared your settings to what actually happened" | **Low.** Promises a *check*, not a proof or an optimum. **Survives a null result gracefully** — "we checked; it's fine" is a complete sentence | **High.** Plain English, no jargon, works in a headline and for the sceptical-prospect read (O5), translates | No. "Reality check" is generic English, unclaimed as a forecasting term — an advantage |
| 2 | **Forecast Calibration** | the stated percentage matches observed frequency | **Medium-High.** In weather and ML, calibration is a reliability-diagram property measured over many trials. A sixteen-cell, single-origin grid is not that; the word invites a question the artifact cannot answer | High — credible, professional, the word Brown reaches for | **Yes, heavily**: WMO verification, ML probability calibration, Prophet `coverage`. Borrowing the word borrows its standard |
| 3 | **Hindsight** / Hindsight Report | "what your settings would have told you, looking back" | **Low.** Inherently modest; carries its own caveat | **Highest memorability.** One word, ownable, brandable | Near-miss only: **hindcast** is the meteorology term for exactly this operation — a flattering adjacency |
| 4 | **Forecast Replay** | re-run past forecasts against known outcomes | **Lowest.** Purely mechanical and exactly true | Medium. Clear but flat; doesn't say why you'd care | No (Prophet's "simulated historical forecasts" is the nearest cousin) |
| 5 | **Sampling Window Check** | your configuration fits your history | Low-Medium | Medium. Risks reading as form validation | Adjacent: "goodness of fit" is a real statistical term |
| 6 | **The Full Monte** *(working title)* | the whole lot, exhaustively | **Highest of the set.** "**Full**" claims completeness and authority — precisely the claim O4 forbids, on a grid of sixteen non-comparable cells. Also a gambling pun on a probabilistic feature, the connotation this product category spends its life fighting | High but narrow. Delightful to practitioners who know Brown's post; opaque to non-native speakers; SEO-colliding with a 1997 film | **Yes — it is Nick Brown's article title.** Using it verbatim as a product feature name borrows his branding without his data |
| 7 | **Accuracy Check** | how accurate past forecasts were | **Medium.** "Accuracy" is Brown's word but loose; for probabilistic forecasts the precise concepts are calibration and sharpness, and "accurate" invites "so it's right 100% of the time?" | Medium-High. Instantly understood | Partially — Brown's own framing noun, so it reads as homage |

### Naming recommendation

- **User-facing: "Forecast Reality Check."** The only candidate that is simultaneously honest under a
  **null result** (critical given §1.4), honest under **non-comparability** (it promises a check, never a
  ranking or an optimum), strong for the O5 sceptical-prospect read, and free of collision with both the
  field's vocabulary and the shipped "Forecast Backtesting". Shortens to **"Reality Check"** in-app.
- **Runner-up: "Hindsight"** — better brand, vaguer promise; the one to use for a conference talk.
- **Avoid "Calibration" as the feature name**, but **do** use "calibrated" in body copy where the artifact
  earns it (reporting coverage against a nominal percentile). Borrow the word for a claim you can support,
  not for a label.
- **Internal codename: keep "The Full Monte."** It credits the source method, carries no promise to a
  user, and belongs in the commit scope, the ADO Epic title, and — with attribution — the launch post
  ("inspired by Nick Brown's *The Full Monte*"), which converts a borrowed title into a citation.
- **Engineering term: "backtest sweep"** — already the slug, already accurate, no change needed.

---

## 6. Gate G2 evaluation

| Criterion | Result |
|---|---|
| 3+ real products named | **PASS** — ten named with citations: ActionableAgile (Jira + ADO), Nave, FlowViz, Businessmap/Kanbanize, Broken Build, Agile Gadgets, Jira Advanced Roadmaps, Monte Carlo Azure, FocusedObjective, plus ProKanban as a practice source |
| At least one non-obvious alternative (different category, same job) | **PASS** — four categories, not one: time-series libraries (Prophet, Darts, fpp3), quant backtesting (vectorbt, walk-forward), ML sweep UIs (W&B, Optuna), probabilistic-forecast verification (WMO, Brier, PIT) |
| No generic market claims | **PASS** — every capability claim names a product and cites a page; three claims are explicitly marked UNVERIFIED (Kanban Zone, Advanced Roadmaps methodology, the FlowViz page's hidden status) rather than asserted |
| Source method understood | **PASS** — §1, with provenance, the arithmetic consistency check, and two departures from the source identified (§1.2, §1.4) |
| What each does well / where it fails the job | **PASS** — §2, §2.1 |
| Key assumptions competitors make | **PASS** — §2.1(b): every competitor assumes the user can choose their own window; none computes or recommends one |

**G2: PASS.**

---

## 7. Knowledge gaps, carried forward honestly

1. **The Full Monte primary source** read via the readmedium.com mirror only. Mitigations applied (two
   extractions, arithmetic reconciliation, independent corroboration of the headline). **A human should
   open the Medium URL once before any of it is quoted publicly.**
2. **Kanban Zone — UNVERIFIED.** A native Monte Carlo feature could not be confirmed to exist. Do not
   assert presence or absence.
3. **Jira Advanced Roadmaps** — "deterministic, not probabilistic" is **Medium** confidence; Atlassian's
   own page is silent on methodology.
4. **Magennis's "How Many Samples Do I Need?" notebook** — observablehq.com returned HTTP 429. It probably
   holds the most quantitative answer in the agile space to "how much history". Worth one retry.
5. **FlowViz "MCS Backtesting"** — attested by one page. Anyone with Power BI could settle it in five
   minutes by opening the template.
6. **SSRN 2326253** — 403; the PBO/CSCV description rests on search summaries plus the fully citable AMS
   paper, which covers the same ground.
7. **No source anywhere addresses the exact hazard as posed** — "all cells end at today, therefore each
   horizon scores a different period". The literature covers its two components (single-origin evaluation;
   overlapping-window error correlation) but names no paper or tool for this precise configuration. **§4 is
   therefore analysis built on cited components** — the citations are solid, the composition is
   interpretation, and it is labelled as such.
8. **Deliberately not researched**: Nixtla and sktime naming; MLflow's grid visualisation; backtrader's
   `optstrategy` heatmap API; QuantConnect's parameter-optimisation UI.
