# Slice 04 — Stop a refresh that is doing harm

**Epic** #5511 Task Manager · **Story** US-04 ·
**Job** `job-operator-stop-a-refresh-that-is-doing-harm`

## Goal

An administrator can stop a queued or running refresh from the popover, without restarting the process.

## IN scope

- `Cancelled` appended to `UpdateProgress` **after** `Failed`, so existing ordinals keep their meaning
  and monotonic `Advance` can still reach it.
- A `CancellationTokenSource` per admitted `UpdateKey`, owned by `UpdateQueueService`.
- An ambient scoped cancellation context, mirroring the existing `WriteBackRoundContext`, so an update
  task can observe its own token without every method signature learning about it.
- Checkpoints at whatever boundaries the probe below establishes are actually reachable.
- A `SystemAdmin`-guarded cancel route on `UpdateController`, and a Cancel control per popover row.

## OUT of scope

- Hard abort (D5).
- Widening `IWorkTrackingConnector` beyond the six **paging** methods. DESIGN decided the port *does*
  widen — the two `GetWorkItemsForTeam` overloads, the two `GetFeaturesForProject` overloads and the
  sweeps — but not `ValidateConnection`, `GetPredefinedAdditionalFields` or `WriteFieldsToWorkItems`,
  which do not loop and cannot honour a token.
- Cancelling a whole refresh round across entities. Cancel is per `UpdateKey` (AC-04.7).

## Learning hypothesis

**Disproves that cancellation can be honoured without changing `IWorkTrackingConnector`.**

This is the Epic's sharpest hypothesis and the reason its probe runs early. The evidence points the
wrong way: S9 says none of the port's 16 methods takes a `CancellationToken`, and S10 says the wall-clock
of a refresh is overwhelmingly connector paging — Epic #5687's Data-Center dogfood went 468 856 ms to
2 087 ms by changing paging alone, on an identical scanned set. If all the time is inside one
un-cancellable call, a checkpoint between entities cancels nothing an operator would notice, and
"cooperative cancel" would be a label on a button that does not work.

If it fails: the honest options are (a) widen the port so the paging loops observe a token — smaller
than it sounds, because it is the loops, not all 16 methods — or (b) ship dequeue-only and say so
plainly in the UI. Either is a scope change the user decides, not one this slice makes quietly.

## Acceptance criteria

See US-04 in `feature-delta.md` — AC-04.1 through AC-04.8. The two that carry the risk:

- **AC-04.2** — the achieved checkpoint granularity is written into this brief **as a number**, from the
  probe, before the slice is called done. "Best effort" without a measured granularity is not an
  acceptance criterion, it is a hedge.
- **AC-04.4** — a cancelled run must still flush or explicitly abandon its `WriteBackRound` and release
  anything held behind its key. Same failure mode as AC-01.3/AC-01.4, reached through a different door:
  a round that never finishes drops every write it staged, silently.

## Dependencies

Slices 02 and 03 — there is no row to cancel from, and no elapsed time to justify cancelling by.

## Effort

One day after the probe. If the probe returns "the port must change", re-estimate before starting.

## Reference class

`epic-5687-faster-updates` — the last change that reached into connector paging, and the source of the
timing evidence above.

## DESIGN verdict (2026-08-23)

[ADR-183](../../../product/architecture/adr-183-cancellation-ambient-token-with-paging-widened.md):
**both parts.** A `UpdateCancellationContext` (`AsyncLocal`, written only by `UpdateQueueService`,
sibling of `WriteBackRoundContext`) **and** a narrow widening of the six connector paging methods.
`Cancelled` appends after `Failed`. Cancel is per `UpdateKey` and idempotent.

`WriteBackRoundContext` was deliberately not extended: its round has `Join`/`Leave`/`HasFinished`
semantics a token has no use for, and an execution with no write-back would have to carry a round in
order to carry a token.

The probe below still runs — its job is now to **measure** the achieved granularity for AC-04.2, and to
confirm that widening the paging loops is where the time actually is. If it shows the phase boundaries
alone already suffice, the port widening is not done: the decision rule outranks the decision.

## Pre-slice SPIKE — half a day, timeboxed, **run during slice 02/03**

**Question**: where does a real Team refresh actually spend its wall-clock, and which of those points
can observe a token today?

1. Instrument one real Team refresh against a real connector. Record time spent per phase: sweep, fetch,
   per-page, persistence, forecast.
2. For each phase, record whether a `CancellationToken` can reach it without changing
   `IWorkTrackingConnector`'s public signatures.
3. Report the coarsest interval an operator would actually experience between pressing Cancel and the
   refresh stopping.

Record the verdict here, including the number that AC-04.2 asserts against.

## Probe verdict — 2026-09-14

Run during slice 02, as scheduled. Experiments live in
`API/Integration/TaskManager/Slice04CancellationReachProbe.cs` and run in the ordinary suite, because a
probe whose result is load-bearing should keep being true.

### 1. A token *can* reach inside a connector call, without touching the port

**This corrects the premise, not just the answer.** S9 is a true statement about signatures and was read
as implying that cancellation cannot reach inside a connector call. It does not imply that.

`WriteBackRoundContext` is an `AsyncLocal`, and `UpdateQueueService` sets it immediately before invoking
an update task. The probe asserts that this value — production code, nothing added — is readable
*inside* `GetWorkItemsForTeam`, through the queue, the updater, the data service and the work-item
service, into a method whose signature mentions nothing of the kind. A sibling `UpdateCancellationContext`
reaches exactly as far.

So widening the six paging methods is **not required for reach**. It is a choice about whether a
connector's dependency on the update pipeline is written down at the boundary or left ambient.

### 2. Phase boundaries are not a granularity anyone would feel

Measured: a Team refresh took **954 ms**, of which **600 ms** was one connector call and **355 ms** was
everything Lighthouse does itself — database reads, the sync loop, the saves, the events — against a
seeded-empty tracker that makes Lighthouse's own share look as large as it ever will.

Structurally it is worse than the ratio suggests. The ordinary full-fetch path makes **one** connector
call, so it contains **zero** phase boundaries to check a token at. Only the delta path has one, between
the sweep and the fetch.

**The number AC-04.2 asserts against.** With phase-boundary checkpoints only, the interval between
pressing Cancel and the refresh stopping is *the whole remaining fetch*. The worst real observation
available is Epic #5687's Data Center dogfood: **468 856 ms** before that Epic's paging work, **2 087 ms**
after. With the token observed inside the connector's paging loops instead, the interval is **one page
round-trip** — Jira chunks reference ids at 200 per query and pages boards at 50, so one HTTP call.

### 3. What this means for the slice

ADR-183's conclusion survives — both parts — and the slice's own decision rule settles it: phase
boundaries alone do **not** suffice, so the paging work is done. But the rule's other half now applies to
the port widening, which the probe shows is optional:

- **Ambient only** — `UpdateCancellationContext` read by the paging loops. No port change at all. The
  connector quietly depends on an update-pipeline concept.
- **Ambient + widened paging signatures** — ADR-183 as written. The dependency is explicit and a
  connector can be tested against a token directly, at the cost of six signatures.

**This is a decision the maintainer takes, not one the slice should take quietly**, because ADR-183 chose
the second on a premise the probe has just corrected.

### 4. What this means for #5877 item B

B was sequenced next to slice 04 partly because "whether cancellation can reach inside a connector call
determines whether an eviction path is even possible". It can. So evicting a running Portfolio refresh to
let starved Teams through is on the table, not only reordering what has not started yet. That widens B's
options; it does not decide them.
