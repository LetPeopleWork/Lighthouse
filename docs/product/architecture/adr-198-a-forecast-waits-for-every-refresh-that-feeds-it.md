# ADR-198: A forecast waits for every refresh that feeds it, running as well as queued

**Status**: Accepted (DESIGN amendment, 2026-09-18; interaction mode PROPOSE)
**Date**: 2026-09-18
**Feature**: story-5877-update-queue-lanes (ADO User Story #5877, slice 01) — amends the forecast
coalescing that [ADR-195](./adr-195-update-queue-is-three-lanes-one-channel-each.md) makes concurrent
**Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

---

## Context

Splitting the update queue into three lanes (ADR-195) makes a Team refresh, a Portfolio refresh and a
Forecast of the same portfolio run at the same time. That is the whole point of the story. It also
breaks two acceptance tests from a different epic, deterministically, and the cause is intrinsic to the
lanes rather than to how the lanes were written:

- `DependencyAwareForecasting/Slice00OneForecastPerBatchScenarios.A_portfolio_refresh_overlapping_a_team_refresh_settles_on_one_delivery_date`
- `DependencyAwareForecasting/Slice00OneForecastPerBatchScenarios.Refreshing_everything_announces_a_new_delivery_date_once_for_each_portfolio`

Both fail because `IForecastService.UpdateForecastsForPortfolio` is invoked **twice** for one portfolio
in one round. The Monte Carlo simulation behind a delivery date is not seeded, so the second run moves
the date the first one just showed the user. That is the failure epic 5792 slice 00 exists to exclude.

### The mechanism, as it stands

Only two production call sites ask for a *coalesced* forecast, and both ask from **inside** an update
execution, while that execution's own key reads `InProgress`:

| Caller | Key of the execution it asks from |
|---|---|
| `PortfolioUpdater.Update` (`:123`) | `Features_{portfolioId}` |
| `TeamDataRefreshedForecastTriggerHandler.HandleAsync` | `Team_{teamId}` |

(`ForecastController` and the two rank/ordering handlers call `TriggerImmediateUpdate`, which
deliberately bypasses all coalescing: a person pressed a button and is watching for the answer.)

`ForecastUpdater.TriggerUpdate` then makes two decisions, both resting on
`IUpdateStatusStore.HasQueuedWork`, which counts `Queued` **only**:

1. `AForecastForThisPortfolioIsAlreadyOwed` — stand down if a forecast for this portfolio is held or
   queued.
2. `TeamsOfThePortfolioWaitingToRefresh` — hold this forecast if any team of the portfolio is queued.

The `Queued`-only exclusion is documented in two places and given a reason. `IUpdateStatusStore.cs:45-52`:

> *"Work that is already running deliberately does not count: a caller reacting to its own update would
> otherwise find its own key still running and wait for itself forever."*

and `ForecastUpdater.cs:106-118`:

> *"A forecast that is already running deliberately does not count: it read its data before this request
> existed, so that request still needs a run of its own."*

Under one lane that reasoning was complete, because a Team refresh and a Portfolio refresh could never be
running at the same time. Whichever of the two asked second always found the forecast still `Queued` — it
could not have started, the single reader was busy with the asker — and stood down.

### What lanes change

With a lane each, the sequence becomes:

1. The Portfolio execution finishes its work and asks for a forecast.
   `TeamsOfThePortfolioWaitingToRefresh` finds the team **`InProgress`**, not `Queued`, so it is not
   waited for. The forecast is admitted and starts in the Forecast lane.
2. The Team execution finishes and asks for a forecast. `IsHeld` is false and the forecast is no longer
   `Queued` — it is running, or already finished and removed. Nothing dedupes it. A second forecast runs.

This reproduces wherever the forecast runs, and folding `Forecasts` into the Portfolio lane does not fix
it: ADR-195 already examined and rejected that, because a forecast is *also* triggered from inside a Team
execution and joins the Team's round, so a round spans lanes whatever the forecast's lane is.

### Why this is a design decision and not a bug fix

By the *existing documented rule*, the second forecast is arguably correct. The comment says a running
forecast does not count because *"it read its data before this request existed, so that request still
needs a run of its own"* — and under lanes that is now literally true, because the team's writes were not
in the first forecast.

So the defect is not the second forecast. **The defect is that the first forecast started before its
inputs had settled.** The hold mechanism already exists to prevent exactly that, and
`TeamsOfThePortfolioWaitingToRefresh` was doing that job — it only ever needed to wait for *queued* teams,
because a *running* one could not be concurrent with the asker. That inference is what lanes invalidate.

---

## Decision

**A forecast waits for every refresh that feeds it — teams of the portfolio and the portfolio's own
Features refresh — counting work that is running as well as work that is queued. The dedup question,
"is a forecast for this portfolio already owed", is left exactly as it is.**

Four parts.

### 1. The wait counts running work. The dedup does not.

Two different questions were being answered by one predicate, and they separate cleanly:

| Question | Predicate | Change |
|---|---|---|
| *Have this forecast's inputs settled?* | every refresh that feeds it is finished | **widened** to `Queued` **or** `InProgress` |
| *Is a forecast for this portfolio already owed?* | it is held, or admitted and not yet started | **unchanged** — `IsHeld` or `Queued` |

The second keeps the documented exclusion and its stated reason, which is still sound: a forecast already
running read its data before this request existed, so this request genuinely needs a run of its own. What
changes is that under the new wait, a later asker practically never reaches that branch — see the
invariant in part 3.

### 2. The wait set gains the portfolio's own refresh

The set of work a forecast for portfolio *P* waits on becomes

> `{ Team_t : t is a team of P } ∪ { Features_P }`

filtered to whatever is still admitted and unfinished.

`Features_P` is in the set for a correctness reason, not as a dedup trick: the Features refresh is what
fetches the features being forecast. A forecast that runs while `Features_P` is mid-fetch is forecasting
over a half-updated feature set. Under one lane that could not happen and the set did not need to say so.

### 3. Nobody excludes themselves, and that is what makes it deterministic

A hold that names the asking execution's own key is **not** a wait forever. Both run paths remove a key
from the store *before* they sweep for holds to release — `RunUpdateAsync` at `:507` then `:521`,
`RunAwaitableUpdateAsync` at `:616` then `:618` — so a hold naming the asker releases the moment the
asker's own run ends. Waiting for yourself is a wait of bounded length ending at your own completion. The
store's stated fear of waiting "for itself forever" is over-cautious as written; the single lane simply
meant nobody ever had to find out.

Not excluding the asker is not a tolerated cost. It is what buys the guarantee:

> **Every in-execution asker is itself a member of the set it waits on.**
> `PortfolioUpdater` asks from `Features_P`; `TeamDataRefreshedForecastTriggerHandler` asks from
> `Team_t` where `t` is a team of `P`. Both are in the set.

Therefore no hold can have already cleared while another asker is still asking — clearing requires every
member to be finished, and the asker is a member and is running. The first asker registers the hold;
every later asker of the same overlapping group finds `IsHeld` true and stands down. One forecast per
overlapping group, structurally, with no dependence on who won a race.

This invariant has one precondition — that every coalesced caller is inside an execution whose key is in
the set. That precondition is enforced, not assumed: see Earned Trust.

### 4. The port gains a scoped liveness predicate; the Lua scripts are not touched

`IUpdateStatusStore` gains **one member**, an overload of the predicate it already answers fleet-wide:

```
bool HasActiveWork(IReadOnlyCollection<UpdateKey> keys)
```

Same predicate as the existing `HasActiveWork()` — admitted and not yet terminal, i.e. `Queued` or
`InProgress` — over the keys the caller names instead of over everything. `HasQueuedWork` keeps its name,
its meaning and its narrower question. The two now read as a pair: `HasActiveWork(keys)` asks
*is any of these still to finish*, `HasQueuedWork(keys)` asks *is any of these still to start*.

Both adapters implement it, behaviourally identically:

- `InProcessUpdateStatusStore` — the same `keys.Any(TryGetValue …)` shape as `HasQueuedWork`, with the
  status test widened to `is Queued or InProgress`.
- `RedisUpdateStatusStore` — the same single batched `HashGet(StatusHashKey, fields)` as `HasQueuedWork`,
  including its empty-`keys` guard (Redis rejects a field-less `HMGET`), with the same widened test.

**No Lua script changes.** ADR-182 froze `MonotonicAdvanceScript` and `RequeueIfAdmittedScript` and this
decision keeps that freeze absolutely: like `HasQueuedWork` and `HasActiveWork()` before it, the new
member is an ordinary read of the ordinal hash. `RedisUpdateStatusScriptFreezeTest` keeps comparing the
same two scripts character for character and keeps passing untouched.

The hold's own release predicate moves with the wait. `UpdateQueueService.TakeHoldsWhoseWaitHasCleared`
must ask the *same* question the hold was registered with, or a hold registered against a running team
would release while that team is still running. It therefore uses the scoped `HasActiveWork` too. Because
the meaning of "clears" changes, `IUpdateQueueService.HoldUntilQueuedWorkClears` is renamed
**`HoldUntilNamedWorkClears`** — leaving `Queued` in the name would be a signature that contradicts the
code, which is the exact failure mode this ADR is written to avoid elsewhere.

---

## Alternatives Considered

**Fold `Forecasts` into the Portfolio lane.** Rejected in ADR-195 already, and re-checked here because it
is the first thing this failure suggests. It does not work: a forecast is also triggered from inside a
Team execution and joins the Team's round, so Team ∥ Forecast concurrency survives the merge. It would
buy nothing and reintroduce head-of-line blocking between forecasts and portfolio refreshes.

**Widen the dedup instead of the wait** — count a *running* forecast in
`AForecastForThisPortfolioIsAlreadyOwed`, so the second asker stands down. Rejected: it makes both failing
tests pass and quietly loses data. The team whose refresh triggered the second request would have its
writes reflected in no forecast at all until the next periodic refresh, up to three hours later on
shipped defaults. The existing comment names this cost precisely and is right about it. Standing down is
only safe when the forecast that is standing in *will see* the stander-down's writes — which is exactly
what "held or queued" means and "running" does not.

**Hold behind a running forecast instead of standing down.** The obvious repair of the above: do not lose
the intent, park it, run a second forecast afterwards. Rejected as a fix for *this* defect, because it
still runs `UpdateForecastsForPortfolio` twice and the user still watches the date move. It is however
retained as the correct handling of a genuinely later request — see the collision re-check below, which
does exactly this.

**Track forecasts per `WriteBackRound` and coalesce on round identity.** Attractive, because "one forecast
per round" is how the promise is phrased. Rejected on a fact: in the overlapping scenario the Team
execution and the Portfolio execution are in **two different rounds** (each was triggered independently
and opened its own), and the test still demands one forecast. Round identity does not identify the group
that must coalesce. It would also put forecast bookkeeping into `WriteBackRound`, whose contract shape
ADR-196 has just finished bounding.

**An ambient "current update key" context, so the asker can exclude itself.** Rejected twice over. It
needs a new `AsyncLocal` component plus its own single-writer enforcement test, mirroring
`UpdateCancellationContext` — real cost for a real seam. And it is solving a problem that does not exist:
self-exclusion is not needed (part 3), and adding it would *destroy* the invariant that makes the outcome
deterministic, because an excluded asker's hold could clear while that asker is still running.

**Reuse `GetAdmittedWork()` and filter in `ForecastUpdater`, adding nothing to the port.** This is the
option that keeps ADR-195's "no store change" promise literally. Rejected on cost and on layering. On
cost: `GetAdmittedWork` is fleet-wide, and on Redis it is two whole-hash reads — the ordinal hash plus a
best-effort moments hash — reconstructing an `UpdateStatus` per admitted key, including timestamps, in
order to throw all of it away. It would replace a scoped `HMGET` of a handful of fields with that, on a
path that runs at the end of every team refresh. On layering: `GetAdmittedWork` is the **read model**
(ADR-181), shaped for an operator's popover; making a coordination decision out of it couples the queue's
correctness to a display projection and to `AdmittedWorkOrdering`'s stability.

**Filter the wait set in the store** — a member returning *which* named keys are still active rather than
*whether* any is. Rejected as premature: nothing needs the list, `Count > 0` is the only question asked,
and the boolean is one round trip where a filter invites a per-key one.

---

## Consequences

### Positive

- The two epic-5792 scenarios pass for a structural reason rather than by winning a race, via the
  invariant in part 3. That guarantee did not exist before — under one lane it was an accident of
  scheduling, and the tests were green on the accident.
- A forecast can no longer run over a half-fetched feature set. That was reachable before lanes too,
  whenever a hand-triggered refresh overlapped, and nothing named it.
- The two contradictory comments stop being contradictory. The store's `Queued`-only exclusion keeps its
  place and gains an honest statement of where it stops being sufficient.

### Negative / accepted

- **A forecast asked for by a team now waits for a concurrent Features refresh of a portfolio that team
  belongs to.** If that refresh is the reported 77-minute one, the forecast waits 77 minutes. Two things
  make this acceptable: a hold occupies no lane, so nothing else is delayed by it; and running the
  forecast earlier would produce a date over half-fetched data that the portfolio's own trigger would
  supersede within the same round anyway.
- **The write-back round a held forecast keeps open is held for that whole time.** The team's staged
  updates wait for the flush that the forecast's execution will perform as the last one out. This is not a
  new mechanism — the hold already deferred a round behind every *queued* team of a bulk refresh — but its
  reach is longer now. Slice 02's wall-time bound (ADR-197) is what puts a ceiling on it; until that
  lands, the ceiling is how long a refresh can run.
- **Which round the single forecast joins is decided by which asker registered the hold first**, and that
  is a race. It was a race before too (whichever asker was first admitted the forecast into its round).
  It is visible only as *how many* calls a round makes to the tracker, never as *what* they carry, which
  is why `A_portfolio_refresh_overlapping_a_team_refresh_settles_on_one_delivery_date` asserts the set of
  values written and not the call count, and why
  `Moving_the_forecast_into_its_own_execution_still_reaches_the_tracker_once` — which does assert the
  count — triggers the portfolio alone.
- **In the portfolio-only case the forecast now starts after the portfolio execution ends rather than
  during its tail.** The round arithmetic is unchanged: the hold joins the round in the enqueue's place
  and `ReleaseIntoItsRound` hands that place to the released work, so the round still finishes exactly
  once and still reaches the tracker once carrying both passes.
- **`ADR-195`'s "no change to `IUpdateStatusStore` or either adapter" is narrowed, not kept.** The reason
  it gave is preserved in full: *a lane is a queue-implementation fact and must not enter a port two
  adapters implement*, and no lane enters the port here. What enters is a scoped liveness predicate, of
  which the port already answers two. The roadmap's `out_of_scope` entry and the DESIGN Reuse Analysis row
  marking `IUpdateStatusStore` **REUSE AS-IS** are superseded by this ADR for this one member.
- **`ForecastUpdaterTest.Update_ShouldForecast_WhenTheTeamThatAskedForItIsStillRunningItsOwnRefresh`
  asserts the behaviour this ADR reverses**, with a comment explaining it. It is rewritten rather than
  deleted: the fact it pins is still worth pinning, restated as *a team that asks while still running is
  waited for, and the forecast runs exactly once its run ends*. Two sibling tests
  (`…WhenTheQueuedWorkBelongsToTeamsThatDoNotWorkOnThePortfolio`,
  `…ShouldForecastBothPortfolios_WhenTheSameTeamWorksOnTwoOfThem`) record a team as `InProgress` as
  scaffolding rather than as their subject and must be driven through the real queue, or through a hold
  that is actually released, to keep saying what they were written to say.
- **The wait set is the portfolio's whole candidate set, not only the members currently active.** One
  store call answers "is any of these still to finish", and the hold then names all of them; members that
  are not admitted contribute nothing to the release predicate. The consequence is that a team of the
  portfolio admitted *while* the forecast is held is waited for too. That is the right answer — it is one
  more input settling — but it means a portfolio whose teams refresh continuously could defer its forecast
  for a long time. The shape is pre-existing (`HasQueuedWork(teamKeys)` already named every team when any
  was queued); widening the predicate lengthens the window rather than creating it. Filtering to only the
  active members would not remove the risk either, since the check is re-run on every sweep. Left as is,
  and bounded in practice by teams finishing and, once slice 02 lands, by the wall-time bound.
- **One existing scenario stops being trivially green.**
  `TaskManager/Slice04StopARefreshScenarios.A_cancelled_refresh_still_lets_go_of_the_work_held_behind_it`
  registers its hold against a team that is already `InProgress`. Under the `Queued`-only predicate that
  hold was released *immediately on registration*, by the `ReleaseClearedHolds()` call inside
  `HoldUntilNamedWorkClears` — so the scenario was green before the cancel it is about had happened. Its
  own comment says it waits "on that key leaving the queue", which is what it will now actually do. It
  still passes, and it finally tests what it claims.
- **A hold whose wait names work abandoned at shutdown never releases.** `AbandonUnqueuedWork` removes a
  key without sweeping, because the queue is closed and released work could not be enqueued anyway. This
  is pre-existing and shutdown-only: the process is going away, and `DrainAsync` never waited for a held
  forecast in the first place. Recorded rather than fixed, so that nobody reads its absence as an
  oversight.

---

## Earned Trust

Every assumption below is a claim about something that can be false. Each has a probe that makes it
answer, and each probe exercises the lie rather than the happy path.

| Assumption | Probe |
|---|---|
| A Portfolio refresh and a Team refresh running in different lanes still produce **one** forecast | Acceptance scenario in `UpdateQueueLanes/Slice01TeamsKeepMovingScenarios.cs`, driven through the production queue with the tracker **held open** so the overlap is real rather than hoped for, asserting `UpdateForecastsForPortfolio` exactly once. The guarantee is defended where it is now at risk, not only in the epic whose suite happens to catch it. |
| A hold naming the asking execution's own key is released, not stranded | Test: register a hold naming a key that is `InProgress`, run that key's execution to completion through the real queue, assert the hold released and its callback ran. This is the probe for the ordering `Remove`-then-sweep — the single fact part 3 rests on, and one a future edit could silently reverse. |
| The hold's release predicate matches the predicate it was registered with | Test: hold on a key that is `InProgress` and stays so; assert the hold is **not** released while it runs. A release predicate left on `HasQueuedWork` passes every positive test and fails only this one. |
| Both adapters answer the new predicate identically | Extend `Integration/Containers/UpdateStatusStoreContainerTests` in the shape its `HasQueuedWork` case already uses: every combination of recorded states across both stores must give the same answer, against a real Redis container. |
| The Lua scripts did not move | `RedisUpdateStatusScriptFreezeTest`, unchanged and still passing. Its value is that nobody had to remember — it fails if the new member is implemented inside a script. |
| Every coalesced forecast caller is inside an execution whose key is in the wait set | Source scanner in `Tests/Architecture/`, in the shape of `UpdateCancellationContextWriterArchUnitTest`: the only production files that call `TriggerUpdate` on an `IForecastUpdater` are `PortfolioUpdater.cs` and `TeamDataRefreshedForecastTriggerHandler.cs`, plus `ForecastUpdater.cs` itself, whose `base.TriggerUpdate` is the admission call rather than a request (the same allowance the cancellation-context scanner makes for the type that declares the thing it guards). A third caller fails the build and makes someone check the invariant, rather than silently reintroducing a double forecast. An ArchUnitNET dependency rule cannot express this: `ForecastController` and both rank handlers legitimately depend on `IForecastUpdater` for `TriggerImmediateUpdate`. |
| A request arriving while a forecast is genuinely running is deferred, not lost and not doubled-up | Test driving `RunTheWaitingForecast`'s collision re-check against a running forecast: the released hold re-holds, and the forecast runs once the running one is out — the existing `TriggerImmediateUpdate_ShouldLeaveAWaitingForecastToStillRunExactlyOnce` shape, with the collision being a running forecast rather than a queued one. |
| **The instrument itself does not lie about idle** | `TaskManagerAcceptanceTest.TheQueueGoesIdle` waits on `store.HasActiveWork()` alone, and a held forecast is deliberately not in the store. Holds become routine here — every portfolio refresh now holds a forecast where it used to admit one — so the harness would read idle while a forecast is still owed, tear down the host, and record the late run against whichever test runs next. It must wait for held work too, and over consecutive readings rather than one, because a released forecast is briefly neither held nor admitted. The epic-5792 harness already carries exactly this shape; the fix belongs in the base harness that eight suites inherit. A probe that can report success early is not a probe. |
| A person pressing the button still gets a forecast now | `TriggerImmediateUpdate_*` tests unchanged and still green. This path takes none of the above and must keep taking none of it. |

---

## Cross-reference

- [ADR-195](./adr-195-update-queue-is-three-lanes-one-channel-each.md) — the lanes that make this
  concurrency reachable. This ADR **narrows** its "no change to `IUpdateStatusStore` or either adapter"
  consequence for exactly one member, while keeping the reason that consequence gave: no lane enters the
  port. Its rejection of folding `Forecasts` into the Portfolio lane is re-confirmed here against the
  concrete failure, not merely inherited.
- [ADR-182](./adr-182-update-moments-in-a-sibling-hash.md) — the frozen Lua scripts. The new store member
  is an ordinary `HMGET` over the ordinal hash, exactly as `HasQueuedWork` already is, so the freeze holds
  by construction and `RedisUpdateStatusScriptFreezeTest` needs no amendment.
- [ADR-196](./adr-196-write-back-round-concurrency-contract.md) — the round safety this depends on. A hold
  keeps a round open and hands its place to the released work; that handover is only safe because the
  round's takes are atomic.
- [ADR-076](./adr-076-cluster-aware-update-queue.md) — INV-1 (monotonic advance) and INV-4 (one active
  lifecycle per `UpdateKey`) are untouched: the new member reads the ordinal and writes nothing.
- [ADR-181](./adr-181-update-activity-is-a-read-through-the-status-store.md) — `GetAdmittedWork()` stays
  the read model. It is explicitly rejected above as the source for a coordination decision.
