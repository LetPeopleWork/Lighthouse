# Slice 02 — Dependencies that cross teams count too (premium)

**Feature**: epic-5792-dependency-aware-forecasting · **ADO**: Epic #5792 ·
**Stories**: US-07 `@infrastructure`
(precursor commit), US-08 · **Estimate**: ~6h **if** OQ-2 comes back cheap — see the probe below
**Reference class**: none. This is the highest-risk slice in the epic and the only one whose estimate
is conditional.

## Goal

The warning that said "waits on another team's Feature — not included in the forecast" disappears,
because the date now sits behind that Feature.

## IN scope

**Three precursor commits (US-07, `@infrastructure`), in this order** (maintainer, 2026-08-14: get it
right serially, then make it fast; DESIGN added the first):

**Precursor commit 0 — the addressable draw stream.** `RandomNumberService` is
`new Random().Next(maxValue)` — a fresh `Random` allocated per draw, seeded from nothing. So a
fixed-seed test today can only assert *draw order*, which this slice changes on purpose. Making a draw
a pure function of `(seed, trial, team, day, ordinal)` makes each team's sequence independent of how
the teams are interleaved, which turns AC-5.3 / AC-7.1 / AC-8.6 into **exact equality** rather than
"within Monte Carlo noise", and makes per-trial parallelism result-identical by construction rather
than by test. Without it the rest of this slice cannot be proved. See ADR-154.

**Precursor commit 1 — the serial joint loop. Correctness only.**

- `RunMonteCarloSimulation` swaps its loop nesting from **team → trial → day** to
  **trial → day → team**. One trial advances a single day counter; on each day every team with
  throughput draws its own throughput and consumes from its own `SimulationResult` rows. The per-day
  work is untouched — only the nesting changes.
- **Serial**: today's per-team `Task.Run` parallelism is removed and nothing replaces it yet. This is
  deliberately slower than the current release — per-team concurrency is gone and per-trial
  concurrency has not arrived. Acceptable because both precursor commits land before the slice ends;
  it is never a released state.
- **Separate RNG streams per team.** Interleaving teams changes the order draws are taken from a
  shared random source, so AC-7.1's fixed-seed equality fails on draw order rather than on
  distribution — a red test for a reason that is not a bug.
- A team with no throughput stays excluded exactly as today (AC-7.3).
- Verified by **AC-7.1 alone** (fixed-seed equality against the pre-change run) plus AC-7.3. AC-7.2's
  wall-clock bound is deliberately *not* asserted here — it cannot pass on a serial loop, and pretending
  otherwise would either weaken the bound or block a correct commit.

**Precursor commit 2 — per-trial parallelism.**

- Concurrency returns as **per-trial** rather than per-team — 10,000 units of work instead of a
  handful, which is the better unit anyway. It is not a free `Parallel.For`:
  `ResetRemainingItems()` mutates the shared rows and `AddSimulationResult` writes a plain
  `Dictionary`, both safe today only because each team's task owns its group exclusively. Each trial
  needs its own remaining-count state and a thread-safe histogram.
- **AC-7.1 must still pass unchanged** after this commit. If parallelising moves a percentile, the
  state isolation is wrong — that is the whole reason correctness is proven serially first.
- **AC-7.2** (wall clock) is asserted here, against the pre-epic baseline rather than against the
  serial intermediate.

None of the three precursor commits ships user-visible behaviour **by design** — that is D3's
correctness argument, not a gap.

**Not in this slice: replacing the multi-team aggregation.** DESIGN proposed a fourth precursor
replacing ADR-110's product of CDFs with an observed per-trial maximum, on the grounds that a
dependency breaks its independence assumption. The maintainer **deferred** it (2026-08-14) on two
counts: the bias it corrects points the safe way — the product *under*-states the joint CDF when a
Feature's teams share a blocker, so such a Feature reads slightly late, not early — and it was the
only change in the epic that would have moved a date on a Feature with no dependency at all. One
change to forecasting at a time. ADR-156 holds it if it is ever wanted.

**The story commit (US-08):**

- Cross-team edges become honourable: the eligibility rule from slice 01 now sees every team's rows
  within the trial, so a blocker on team Y constrains a dependent on team X.
- Slice 01's cross-team warning is deleted for honoured edges (AC-8.2).
- D7 and D8 extended to cross-team: a cross-team cycle is warned, a cross-team blocker on a
  throughput-less team is dropped and warned (AC-8.4, AC-8.5).
- Throughput stays per-team. A joint clock shares **time**, never throughput (AC-8.3) — this is the
  single most likely place to introduce a silent forecasting bug.

## OUT of scope

- Any change to the eligibility rule itself. Slice 01 wrote it; this slice widens what it can see.
- Cross-Portfolio honouring (D6, permanently out for this epic).
- Jira and Linear ingestion (Epic #4365, slice 03).

## Learning hypothesis

**Disproves** "the joint simulation is affordable" **if** forecast wall-clock time on the dogfood
instance's full Feature set exceeds the multiple DESIGN sets in OQ-2. Today the work is spread across
one task per team and each task ends as soon as *its* team is done; jointly, every trial runs until
the **slowest** team finishes, and per-trial concurrency has a different contention profile.

If it fails, either concurrency moves to trial batches, or the joint clock is restricted to the teams
actually connected by a dependency — a smaller joint set per run, which is more code but bounded work.
Either way the fallback is known before the slice starts, which is why the probe comes first.

**Confirms**, if it holds, that the epic's central promise is delivered and KPI-7 is measurable.

## Verify the premise first (2h probe, before any code change — REQUIRED)

Prototype the **serial** joint loop behind the existing one on `:5169` and measure two things: a
fixed-seed percentile diff against the current implementation, and how slow serial actually is. The
second number sets expectations for precursor commit 2 and tells you whether per-trial parallelism has
to claw back 3× or 30×. **Two outputs**: the number AC-7.2 will assert, and a go/no-go on the slice's
shape. If the probe says the work is larger than a day, this brief is re-cut before dispatch rather
than run over — DoR item 5 records this slice as the epic's one conditional estimate.

## Acceptance criteria

AC-7.1 … AC-7.3 (precursor) and AC-8.1 … AC-8.6 verbatim from `feature-delta.md`. The three that carry
the slice:

- Fixed-seed percentiles match the pre-change run with no cross-team dependency present (AC-7.1).
- Team X's Feature B never completes before team Y's Feature A within a trial (AC-8.1).
- Team X's throughput is still drawn from team X's own history (AC-8.3).

## Dependencies

Slice 01's eligibility rule confirmed on real data — reversing the order would mean debugging a new
simulation loop and a new eligibility rule simultaneously. The cross-team Predecessor link created in
ADO alongside slice 01's shapes. Premium licence. A recorded pre-change forecast wall-clock baseline
and a fixed-seed percentile snapshot, both captured **before** the precursor commit.

## Dogfood moment

Same day: re-run the forecast on `:5169` and diff every percentile against the snapshot. Record both
the timing and the count of honoured-vs-detected edges (KPI-7) in this brief.

## Commit gate

**VOID.** The maintainer lifted the no-commit-without-approval rule for this epic on 2026-08-23; it
caused more trouble than it prevented. The working agreement that replaced it: run the whole slice,
then refactor, adversarial review and mutation testing, and hand over once those are fixed.

## Learning hypothesis verdict

**CONFIRMED — the joint simulation is affordable, and by a wide margin.** Measured on the maintainer's
machine over a benchmark Portfolio of 25 Features across 6 Teams, three runs each, fastest of three:

| | fastest | median |
|---|---|---|
| Released product (a Task per Team, `new Random()` per draw) | 2150 ms | 3179 ms |
| Serial joint loop + addressable draw source (precursor 1) | 1973 ms | 1980 ms |
| Per-trial parallelism (precursor 2) | **430 ms** | 540 ms |

**0.20× the pre-epic wall clock**, against AC-7.2's ceiling of 1.5× and an expectation of ≤1.0×. The
fallbacks the hypothesis named — concurrency in trial batches, or a joint clock restricted to the Teams
actually connected by a dependency — are not needed and were not built.

The surprise is in the middle row: the **serial** joint loop is already faster than the released
product. Removing the `Random` allocated per draw (ADR-154, taken for determinism rather than for
speed) saved more than the per-Team concurrency was buying. The whole backend suite runs about a minute
faster as a side effect.

Re-measure with `ForecastWallClockProbe`, which is `[Explicit]` and prints three timings.

## What the design did not predict

DISTILL's *Owed before slice 02* list said a stale `Feature.CanBeForecast` — computed from the previous
run's persisted forecasts — could let the policy honour an edge whose blocker never completes, and that
**the loop would not terminate**. It does terminate, and the reason is worth writing down because it
changes what is at risk.

A blocker whose Team has no measured delivery has no row in the run at all, so it appears in no
Feature's list of what must finish first, so it holds nothing up. The failure is therefore a wait that
is **silently not acted on** for one refresh, not a hang: the date reads as the earliest it could
possibly be, which is what a dropped edge is supposed to produce, but the row carries no note saying
so. It heals itself on the next run, because the run just finished writes a forecast with no trials for
that Team, which makes `CanBeForecast` false and the policy drops the edge and warns.

Pinned by `WaitingOnAFeatureWhoseTeamHasNothingMeasured_StillReachesAnEnd`. Whether to close the
remaining gap — computing that fact from the throughput set the run is building and handing it to the
policy — is a decision about widening `IWhatTheForecastWaitsFor`, which is a guarded seam, so it is
left for the maintainer rather than taken at the end of a slice.

## Left open for the maintainer, both surfaced by review

**One Feature in two Portfolios can get two different dates, depending on which refresh ran last.**
DISTILL raised this as *"Which Features the policy is asked about is unstated"* and called it a product
decision rather than a detail; honouring cross-Team waits is what made it reachable. Three callers ask
the one decision over three different sets of Features: the Features view over the whole graph, the
refresh warning over one Portfolio's Features, and the forecast over *everything any of that
Portfolio's Teams touches*. A Feature in Portfolios P and Q, waiting on a Feature that lives only in Q
and is worked by a Team P does not have, therefore has its wait honoured when Q refreshes and dropped
when P refreshes — and whichever ran last is the date on the screen, while the Features view calls the
dependency honoured either way. Before this slice the shape was unreachable: the two ends were not one
Team's work, so the wait was refused everywhere, consistently.

**A Team slow enough to reach the day ceiling now loses its dates rather than getting far-out ones.**
SA-5's ceiling is 100 000 simulated days. A Team averaging one item every ninety days against a
thousand still to do reaches it in every run, and those rows come back with no trials at all, which
also makes the Feature un-forecastable. The released product had no ceiling and would have kept going —
for something like a billion day-iterations, so "it produced a date" is generous. It is reported at
Error level naming the Teams, so it is not silent; it is recorded here because it is a real difference
in behaviour and not one the ADR called out.

## Documentation owed (maintainer, 2026-08-23)

`HowLighthouseForecasts` needs rewriting for the joint clock: what the addressable draw source is and
why it replaced a sequence, and a worked example of a forecast now that every Team advances on one day
counter. Not done in this slice — noted so it is not lost.

## Loose ends to clean up in this slice — both DONE

Both found during slice 01's manual verification on the live instance, 2026-08-23. Neither is worth its
own slice; both are the sort of thing that never gets fixed if it is not written down.

- **`RepositoryBase.cs:68` logs every single removal at Information.** A refresh that prunes a few
  hundred stale items writes a few hundred lines an operator has to scroll past, and Epic #5687's whole
  point was that a completed update is one line worth reading. It is `Debug` material: the count already
  reaches the update summary, and the individual ids matter only when somebody is debugging the pruning
  itself. Note this sits on a shared base class, so it affects every repository, not only work items.

- **The unlicensed warning says "1 Features".** `DependencyRefreshReporter` interpolates a count
  straight into a plural noun, so a single Feature reads as *"…what 1 Features are waiting on"*. Added
  in slice 01, mine. The line is otherwise right - one per Portfolio, names the Teams, silent when
  licensed and silent for a Portfolio that has set its dependencies aside - and it was read out loud
  during verification, which is exactly how a wording bug gets noticed.
