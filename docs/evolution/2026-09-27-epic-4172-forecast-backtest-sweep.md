# Forecast Reality Check — Epic 4172

**Pushed to `main`, not yet released** · ADO Epic #4172 *"The Full Monte"* (`Community`) · workspace
`docs/feature/epic-4172-forecast-backtest-sweep/` · planned 2026-09-22, delivered 2026-09-26 → 2026-09-27
in 65 roadmap steps (01-01 … 06-17), all committed · Stories #6072, #6073 and #6094 built; #6074 deferred.

The Epic's internal codename, *The Full Monte*, is Nick Brown's (ASOS Tech Blog, January 2024), and the
method is his: replay the forecasts a Team would have made, compare them with what it then completed. The
feature a user sees is called **Forecast Reality Check**. The codename was kept off the product on
purpose — "Full" claims a completeness sixteen non-comparable checks cannot carry, it is a gambling pun on
a probabilistic feature, and it is the title of someone else's article.

## What users get

A Team's Forecast tab has a **Run reality check** button under the Backtesting inputs. One click, no date
picker, no configuration. It replays the Team's own finished history — four recent periods of 7, 14, 28
and 56 days, each ending today, each forecast from 14, 30, 60 and 90 days of history before it (plus the
Team's own sampling window when it is off that ladder) — and opens a dialog:

- **A headline** — "Backtested 16 scenarios · 64 forecasts" (20 · 80 for an off-ladder Team, "N of T"
  when some could not run), with an explanation, Brown's credit and a link to his article behind an info
  icon.
- **A badge for the Team's own sampling window** — fine, did not hold up, could not be checked, or not
  tested (fixed dates, or not a positive length).
- **One bar per confidence level (50 / 70 / 85 / 95)** with a tick at the rate that level should hold at,
  reading e.g. "94% (15 of 16) · 3 accurate".
- **A table** of every forecast, grouped by period, each cell a ✓ or ✗ and the forecast, shaded on Brown's
  scale (within 10%, 10–25% off, more than 25% off, held or missed), with the miss in Work Items and the
  percentage of the actual in a tooltip.

Read-only end to end, free, available to anyone who can read the Team.

## What shipped, slice by slice

| Slice | ADO | What it added |
|---|---|---|
| 01 — one sentence about your sampling window | #6072 | The endpoint and the sweep; a verdict naming a *region* of sampling windows that hold up, where the Team's own setting stands, each level held against its own rate, the denominator; the usage-data event |
| 02 — the evidence you can look at | #6073 | One panel per sampling window, each check drawn as a band with the actual marked. **Retired by slice 04** |
| 03 — the answer travels | #6074 | **Deferred 2026-09-26, not built.** How the check is reported is being re-evaluated now that it can be seen in use |
| 04 — the answer opens in a dialog | #6094 | Everything moved into a dialog; the period × window table; `scoredPeriods` added to the reply so every period prints what the Team completed |
| 05 — how close each forecast landed | #6094 | Brown's grading: six shades, percentage of the actual, legend, credit |
| 06 — reads at a glance | #6094 | After the maintainer's review of 04+05 ("far too wordy and hard to read"): the prose replaced by headline, badge and level bars; compact cells with tooltips; the trigger moved; a backend Information log line per check |

Slices 04–06 exist because the maintainer saw 01+02 running and asked for Brown's graded table in a
dialog — which superseded slice 02's "no matrix" rule and the earlier ruling that 64 marks are unreadable.
The invariants those rules protected (no per-window score, read-only, an unevaluable check never blank, no
hard-coded renameable term) all still hold.

No migration, no new permission, no CLI/MCP exposure. Architecture:
[ADR-209](../product/architecture/adr-209-a-report-is-a-response-not-a-record.md),
[ADR-210](../product/architecture/adr-210-a-forecast-level-holds-or-it-does-not-and-its-nominal-rate-is-the-level.md),
[ADR-211](../product/architecture/adr-211-a-reality-check-grade-is-read-in-the-client-from-facts-the-server-already-sends.md).
Journey: [epic-4172-forecast-reality-check.yaml](../product/journeys/epic-4172-forecast-reality-check.yaml).
User docs: [Teams → Forecast Reality Check](../teams/detail.md).

## Decisions worth keeping

- **DISCOVER was skipped.** The job was already evidenced in `docs/product/jobs.yaml`; DIVERGE elevated it
  (`job-forecaster-check-the-forecast-against-what-happened`) and said plainly that no named customer
  asked for this — the secondary objective is demonstrating the method.
- **No Apply button — removed, not deferred.** A button must write *one* number, so "Apply 60 days" names
  a winner, which is exactly the claim a sixteen-check search over months of history cannot support. The
  throughput control is already in the Team header for anyone who can act. An ArchUnit rule
  (`RealityCheckReadOnlyArchUnitTest`) makes reintroducing a write path fail the build: the sweep is handed
  a resolved Team and never a repository.
- **Today is the END anchor, and the verdict names a region, never a winner.** Checks ending on the same
  day are not repeated trials and must not be ranked. The reply carries the sound windows as a set filtered
  from the ascending ladder, with no per-window score anywhere, so "the reply ranks the windows" is
  unrepresentable rather than untested.
- **All four percentiles are read, because percentile is where the signal is.** In Brown's own data the
  percentile moved the correct-rate from 68% to 90% while the sampling window moved it by four points of
  noise. The Epic as raised swept the noisy axis only; the check must be able to say "your window barely
  matters in this range".
- **A level holds, and its nominal rate is the level (ADR-210).** Expected held = evaluated × P / 100. The
  DISCUSS mockups used (100 − P); three of four rows were wrong, the 95% row — the headline lesson — by an
  order of magnitude. It survived review because **the 50% row is identical under both formulas**, and
  that was the row everyone checked first.
- **A report is a response, not a record (ADR-209).** Nothing stored, no entity, no queue. The revisit
  trigger is the second report kind, not a date.
- **The grade is read in the client from facts the server sends (ADR-211).** *Held* stays the server's;
  the shade is a pure function over fields already on the wire. Terminology stays in the browser — the
  reply carries facts, never a sentence.
- **The Team's own window is always swept** (maintainer, reversing DESIGN): 16 checks on the ladder, 20
  off it. The measurement made the cost objection moot; the denominator principle never required the
  constant 16.
- **A sampling window holds up** when its 95% forecast held in more than half of the checks that could run
  on it; over-delivery never counts against a window.
- **Usage data: `TeamForecastRealityCheckRun = 11`, name only**, one per answer shown; failed requests do
  not count. No property — none of the outcome KPIs needs one, and the standing of a setting would be the
  first event reporting a result computed from a customer's Work Items.

## Lessons

- **An off-by-one the whole pipeline passed.** The metrics reads include both ends; the working-day count
  excludes its start. The check mixed them, scoring H + 1 days against a forecast of H and learning from a
  day it was also scored on — so a Team finishing exactly one Work Item a day read *under-forecast in every
  check*. DISCUSS, DESIGN, DISTILL and the first DELIVER pass all passed it; an adversarial review with a
  worked example found it (`ccc42325b`, DES-19). The acceptance harness now fails any scenario handed a
  history that is not exactly a swept window long.
- **A score chosen in isolation hides its cost on a heavier criterion.** Apply scored 5/5 on
  decision-changing while quietly undoing the 30%-weighted honesty requirement; nobody saw it from DIVERGE
  until the maintainer did.
- **A constant quoted inside a locked principle borrows the principle's authority.** "State your
  denominator" was locked; "16 runs / 64 scores" never was, and treating it as locked nearly cost the one
  cell a user most wants checked.
- **Dependency arrays that "could not matter" did.** Two Stryker survivors in `ForecastRealityCheck.tsx`
  froze the Team's filter setting and the usage-data reporter at first render. Stubs hid it — the test
  Team had no filter and the reporter never changed.
- **A spec title is not an assertion.** "A tick at 85%" asserted only the bar's value; four mutants moving
  the tick survived until the computed style was read.
- **A fixed-dates Team only surfaced when the Team's own window became load-bearing.** It had been latent
  since planning.

## Quality

Mutation, gate 80% ([full ledger](../feature/epic-4172-forecast-backtest-sweep/mutation/results.md)):

| Run | Backend | Frontend |
|---|---|---|
| #6072 (slice 01) | **97.53 %** | N/A — UI replaced by #6094 |
| #6094 slices 04–05 | **90.48 %** | **90.61 %** (92.60 % counting the 11 closed by hand-applied mutants) |
| #6094 slice 06 | N/A — one log line, pinned by acceptance | **84.22 %** (85.94 %) |

Every remaining survivor is equivalent, a guard against a reply the server never sends, or styling jsdom
cannot see. Slice 02's UI had no mutation run of its own; slice 04 deleted it.

Time budget ([budget-measurement.md](../feature/epic-4172-forecast-backtest-sweep/deliver/budget-measurement.md)):
cold median **540.5 ms** (16 checks) and **657 ms** (20) against a 5,000 ms budget; exactly 20 / 24
queries on a cold cache, 0 warm, not growing with Team size. `RealityCheckQueryCountTest` holds that line.

## Still open, knowingly

- **The zero-count answer for a Team with no finished history** ("None of the N scenarios could be backtested") is **kept
  as is** by maintainer decision.
- **OQ-6 — a fixed-dates Team** sweeps the standard four windows and is told its setting was not tested
  (`currentSettingWasTested: false`, reason `UsesFixedDates`). **Left as is** by maintainer decision.
- **#6074, the answer travels** — deferred, reporting format under re-evaluation.
- **Bug #6071** — `Team.ThroughputHistory` defaults to 30 while the create and edit forms seed 90. Found in
  DIVERGE, raised on its own, not part of this Epic.
- **The per-Team default confidence level** was declined as an item (2026-09-22): the sampling window is a
  setting, the confidence level is a reading the human makes, by decision.

Delivery history: `docs/feature/epic-4172-forecast-backtest-sweep/`.
