# The forecast waits for what the work waits for — Epic 5792

Shipped 2026-08-21 → 2026-08-23 in three slices. Premium. Released in v26.8.31.7.

Split out of [Epic 4365](2026-08-31-epic-4365-feature-dependencies.md) once it was clear that reading
dependencies and forecasting with them are two different products: the column belongs on every
instance, and the date it sits next to is what a licence buys.

The complaint: seeing the dependency was half of it. The date beside it was still built from one Team's
throughput alone, as though the work could start tomorrow — so the number everybody plans with was the
one number that ignored the thing everybody was worried about.

## What shipped

| Slice | ADO | What it added |
| --- | --- | --- |
| 00 | 5826 | One forecast per Portfolio per refresh round, and one write-back, instead of one per delivering Team. |
| 01 | 5784 | Within one Team, a Feature is not worked until what it waits on has finished — inside every trial. |
| 02 | 5785 | Every Team on one shared clock per trial, so a wait can cross Team boundaries. |

ADRs **154** (addressable draw streams), **155** (joint trial clock replaces per-team simulation),
**158** (the honour policy, shared with 4365) and **159** (un-forecastable blocker drops, date reads as
a floor). **ADR-156** — replacing the joint-likelihood combination with a per-trial max — was
**deferred**; ADR-110 stands unchanged.

## Slice 00 was infrastructure, and it found a defect nobody was looking for

The batching slice existed to make the later work tractable: a Portfolio forecast that runs once per
delivering Team cannot be put on one clock. Measuring the baseline on the dogfood instance turned up
something else — **the Team-triggered forecast never fired on that Portfolio at all**, and the same
wrong read cost more than the forecast it was meant to trigger. The suite had never caught it because
nothing asserted the count.

That is the argument for baseline capture as a step rather than a formality: the number you take to
prove an improvement is also the number that tells you the thing was not running.

Five defects in `UpdateQueueService` were repaired with regression tests during this slice rather than
carried: among them a completion that could be lost silently, and `ReleaseClearedHolds()` running
unguarded before the completion publish, which stranded cross-pod awaiters when caller code threw.

## The termination risk was real, and it is not the risk the design named

DISTILL's *owed before slice 02* list said a stale `Feature.CanBeForecast` — computed from the previous
run's persisted forecasts — could let the policy honour an edge whose blocker never completes, and that
**the loop would not terminate**.

It terminates. A blocker whose Team has no measured delivery has no row in the run at all, so it
appears in no Feature's list of what must finish first, so it holds nothing up. The real failure is a
wait that is **silently not acted on for one refresh**: the date reads as the earliest it could
possibly be, which is what a dropped edge is meant to produce, but the row carries no note saying so.

It self-heals. The run that just finished writes a forecast with no trials for that Team, which makes
`CanBeForecast` false, and the next run's policy drops the edge and warns. Pinned by
`WaitingOnAFeatureWhoseTeamHasNothingMeasured_StillReachesAnEnd`. Closing the remaining gap means
widening `IWhatTheForecastWaitsFor`, a guarded seam, so it was left to the maintainer rather than taken
at the end of a slice.

## Two Features move, not one

The measured result worth keeping, because it is the counter-intuitive one. Three Features, one Team,
Feature WIP of 2. Nothing waiting: **17, 13, 22** working days. Record that the second waits on the
first: **16, 22, 20**.

The Feature *below* the waiting one came in by two days. A Team works the top *Feature WIP* Features in
parallel; a Feature that cannot start yet stops occupying one of those places and the next startable
Feature moves up into it. Modelling the wait frees capacity that was being modelled as spent.

## A shared clock shares time, never capacity

Stated in the release notes because it is the question people ask on seeing "one clock". Each Team
still draws its own throughput from its own history. Sitting on the same clock as a faster Team has
never made a Team faster and does not now.

The licence gate lives in `DependencyHonourPolicy`, applied **after** the other reasons rather than
before them — deliberately, and the code carries the why: asked first, a licence would be put in front
of a circle or a cross-Portfolio wait, waits no licence would have honoured either, and the reader
would be told that buying something would fix a thing it would not fix.

## Mutation testing

| Slice | Backend | Frontend |
| --- | --- | --- |
| 00 (5826) | 55.23 % — accepted, see below | N/A, frontend untouched |
| 01 (5784) | 84.76 % (160 mutants) | 100 % (32/32) |

**Nothing in the eligibility rule survived** on slice 01. Every survivor is log content, log ordering,
or an equivalent mutant. The 27 timeouts are the interesting number: they all land on the simulation
loop's own machinery — the trial counter, the `while` on remaining work, the day loop's bound — where
mutating the bound hangs the run until Stryker kills it. A timeout there is the loop's termination
being load-bearing, not a test gap.

Slice 00's 55.23 % was **accepted rather than chased**. Thirty-one of the unkilled mutants belong to
`RedisUpdateStatusStore`, which has real tests in `Integration/Containers/UpdateStatusStoreContainerTests.cs`
that the run's `test-case-filter` never whitelisted — so Stryker saw a tested adapter as untested code.
The filter is corrected in `stryker.5826.backend.json`; the 24-minute run was not repeated. About 38
more rewrite log messages. The genuine gaps are recorded as outstanding: `statusStore.Advance` at two
call sites, the coalescing path's `Requeue`, and eleven non-log statement survivors in
`UpdateServiceBase`.

## Lessons

- **A baseline is a measurement, not a ceremony.** The one taken to prove slice 00's improvement is
  what revealed the Team-triggered forecast was not firing.
- **Name the failure mode you actually have.** The design predicted a hang and got a silent dropped
  edge. Both are bad; only one of them is invisible, and writing down which one you have is what tells
  the next person whether to look at a stack trace or a row that reads too optimistic.
- **Put the licence check after the reasons that are not about licensing.** Otherwise the product
  offers to sell a fix for a problem money does not solve.

## Open at finalize

- Widening `IWhatTheForecastWaitsFor` so a Team's measurability comes from the throughput set the run
  is building, rather than from the previous run's persisted forecast — a maintainer decision on a
  guarded seam.
- Slice 00's outstanding mutation gaps, listed above.
- ADO #5792 and its Stories are left **Resolved**, not Closed.
