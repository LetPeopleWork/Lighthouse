# ADR-196: A write-back round is a bounded-change component with one lock, and every take is atomic

**Status**: Accepted (DESIGN, 2026-09-18; interaction mode PROPOSE)
**Date**: 2026-09-18
**Feature**: story-5877-update-queue-lanes (ADO User Story #5877, slice 01 precursor commit)
**Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

---

## Context

`WriteBackRound` is half thread-safe, and the half that is missing is the half that is about to
matter.

`Join` and `Leave` are `Interlocked`; `HasFinished` is a `Volatile.Read`
(`WriteBackRound.cs:25,30,39`). Those are the parts written for concurrency. `Stage`, `TakeStaged`,
`ReportRefresh`, `ReportForecast` and `TakeSummary` mutate two plain `Dictionary` fields and two plain
reference fields with no synchronisation at all (`:14-20,45-107`).

A Portfolio refresh and the Forecast it triggers **already share one round**. `PortfolioUpdater:123`
hands the forecast intent to the forecast updater, which enqueues; `RoundForNewWork` sees the ambient
round of the execution that asked and `Join`s it. The same happens when a refreshed team's data
triggers a forecast from inside a *Team* execution. They are safe today only because one reader runs
them one after the other.

ADR-195 removes that reader. Round safety is therefore not an optional hardening — it is a
precondition, and it has to land before the second consumer exists rather than after.

Two further facts were read from the code and decide the shape.

*`TakeStaged` is already race-free on its guard.* `WriteBackCollector.FlushAsync` calls it only when
`round.Leave()` returned `true` (`WriteBackCollector.cs:31-40`), which is an atomic
decrement-and-test. Exactly one execution ever takes the staged set. What is unsafe is the
**concurrent `Stage`** from two executions into one `Dictionary`.

*`TakeSummary` is not race-free on its guard, and becomes reachable twice.*
`UpdateServiceBase.WriteSummaryIfTheRoundIsOver` gates on the `HasFinished` *read*, performed by every
execution in `TriggerUpdate`'s `finally` immediately after the flush already called `Leave()`
(`UpdateServiceBase.cs:127-140`). With one reader, only one execution is ever in that `finally` at a
time. With lanes, the execution that decremented to one can read `HasFinished` after the other
decremented to zero, and **both** call `TakeSummary()`.

## Decision

**`WriteBackRound` becomes a bounded-change component: one lock, held across every member that
touches its state, and the mutation universe is its own four fields and nothing else.**

Concretely:

1. `Stage`, `TakeStaged`, `ReportRefresh`, `ReportForecast` and `TakeSummary` all run under one lock
   owned by the round. `Join`, `Leave` and `HasFinished` keep their `Interlocked` / `Volatile`
   implementations — they are correct, they are the counter rather than the contents, and changing
   them would put a lock between the atomic decrement and its own return value.
2. **Every take is atomic and idempotent by emptiness.** `TakeStaged` and `TakeSummary` read-and-clear
   in one step. A second caller legitimately receives nothing and writes nothing. This is what makes
   the newly reachable double-`TakeSummary` a non-event rather than a duplicated summary line, and it
   is the property, not the lock, that is asserted.
3. **Last stage wins is preserved exactly as ADR-144 §3 fixed it.** Staging is a dictionary upsert
   keyed `(connectionId, workItemId, targetFieldReference)`, and a later pass holds the fresher value.
   Nothing about ordering *within* an execution changes.
4. **Nothing may stage into a round after leaving it.** A round whose last execution has left has
   already handed its staged set to the writer, so a late stage would be silently dropped. The round
   refuses it loudly instead. This is an invariant the code can hold rather than a convention a reader
   has to keep.

**Observable behaviour is unchanged.** The set the round hands to the writer after two concurrent
stagings is the union of what both staged — nothing lost, nothing sent twice — which is what the
single lane already delivered.

## Alternatives Considered

**Concurrent collections instead of a lock.** Swap both `Dictionary` fields for
`ConcurrentDictionary`. Rejected, and not on taste: it fixes the smallest of the three problems and
leaves the other two. `TakeStaged` is a read-then-`Clear` across two collections and a
`ConcurrentDictionary` makes neither the pair nor the sequence atomic; `TakeSummary` reads two plain
reference fields, folds them and nulls both, which no concurrent collection touches at all. It would
be more code than the lock and would look safe while two of the three races survived.

**A per-execution staging area merged on `Leave`.** Each execution stages into its own buffer; the
buffers are merged into the round as each execution leaves. This is the option DISCUSS singled out,
because it is the only one that also removes the cross-execution last-stage-wins ambiguity — and it is
the only one that changes observable behaviour, so it needed a decision rather than a preference.

**Rejected, on three grounds.**

*It does not remove the ambiguity; it relocates it.* If two concurrent executions stage different
values for the same `(connection, item, field)`, some order must win. "Whichever merged last" is no
more meaningful than "whichever staged last" — it is the same arbitrariness, read off a different
clock. A merge order that *was* meaningful would have to rank the roles (the forecast's value beats
the refresh's, say), and no such rule is needed: the two passes resolve disjoint field sets in
practice, so the tie the merge would arbitrate does not occur.

*It needs the very lock it was meant to replace, plus a merge step.* The merge has to be visible
before the decrement that tells the last leaver it may take the staged set. Otherwise the winner of
the `Leave` race reads a staging area another execution is still merging into, and writes a subset.
Making merge-then-decrement atomic against other leavers is exactly one lock — with an extra buffer
object per execution and an extra copy on top.

*It costs the property that currently makes the write terminal.* Draining before the first write is
what makes a second flush attempt find nothing rather than re-send (ADR-144 §3). Splitting the staged
state across N buffers plus the round gives that invariant more than one home.

**Make `TakeSummary` race-free by gating it on `Leave()`'s return value, like `TakeStaged`.**
Attractive — it uses the mechanism that is already correct. Rejected because `Leave()` is called by
the collector during the flush and the summary is written by the updater base afterwards, so the
return value is not in scope where the decision is made, and threading it there widens a seam for one
caller. Take-and-clear under the lock answers the same question at the point it is asked.

**Do nothing and rely on the fact that the two executions rarely overlap.** Rejected without
argument: the overlap is the feature being built.

## Consequences

**Positive.** "Did two executions of one round lose a write?" becomes answerable from the type rather
than from timing. The round is the single place a round's state lives, and it stays that way. The
double-`TakeSummary` that lanes newly make reachable is absorbed by a property the round already
needed.

**Negative / accepted.**

- Every stage takes a lock. The contention is at most three lanes, the critical section is a
  dictionary upsert, and the alternative is a data race on the write path to a customer's work
  tracking system.
- The round grows an explicit refusal for a stage after the last leave. That is a new way for the
  pipeline to throw, and it throws on a path that should never happen. It is deliberate: the
  alternative is losing writes quietly, which is the failure this whole ADR exists to prevent.
- This lands as a precursor commit with no user-visible output of its own. A slice made of only
  infrastructure is a structural failure, so it is a commit inside slice 01, ordered before the second
  consumer exists.

## Earned Trust

`WriteBackRound` has no dedicated test today; it is exercised only through the collector, and always
with a fresh single-execution round. Every row below is therefore new coverage, and every one is
stated as behaviour rather than as "a lock exists".

| Assumption | Probe |
|---|---|
| Two concurrent stagings lose nothing and duplicate nothing | The staged set the round hands to the writer equals the union of what both executions staged. Driven from two threads, repeated enough to make an unsynchronised dictionary fail. |
| Last stage wins still holds within one execution | Two passes in one execution over the same field: the later value is written, once. This is the existing contract and it must not move. |
| The round counts executions correctly under concurrent join and leave | Interleaved `Join`/`Leave` from several threads; exactly one `Leave` returns `true`. |
| Two executions can both believe the round has finished, and it costs nothing | Both call `TakeSummary()`; exactly one summary comes out, the other gets nothing, and one line is written. |
| A second flush re-sends nothing | Existing collector test, re-run against the concurrent round. |
| Staging after the round has finished is refused, not dropped | Direct test on the round: stage after the last leave throws. |
| The whole pipeline still writes back once per round under lanes | Acceptance test: a Portfolio refresh and the forecast it triggers, running in two lanes, produce exactly one flush per connection. The flush-counting collector the task-manager suite already carries is the instrument. |
| The round's mutation universe is its own fields | Structural check: nothing outside `WriteBackRound` reads or writes its backing state, and `WriteBackRoundContext` has exactly one writer. |

## Cross-reference

- [ADR-144](./adr-144-writeback-collection-seam.md) — the staging seam and the last-stage-wins rule
  this ADR preserves. Its Context states "there is no 'refresh round' object anywhere in the system",
  which was true on 2026-08-08 and is not now.
- [ADR-195](./adr-195-update-queue-is-three-lanes-one-channel-each.md) — the lanes that make two
  executions of one round concurrent. This ADR is that one's precondition, not its follow-up.
- [ADR-183](./adr-183-cancellation-ambient-token-with-paging-widened.md) — its recorded invariant that
  a cancelled run must still flush or explicitly abandon its round. Four call sites can leave a round
  and only one can take its staged set; the invariant is unchanged and now has the atomicity it
  assumed.
