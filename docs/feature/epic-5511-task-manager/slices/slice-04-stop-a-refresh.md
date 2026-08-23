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
