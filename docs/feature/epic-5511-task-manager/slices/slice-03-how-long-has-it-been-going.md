# Slice 03 — How long has it been going

**Epic** #5511 Task Manager · **Story** US-03 ·
**Job** `job-operator-see-what-lighthouse-is-doing-right-now`

## Goal

Each row says how long it has been running or waiting, so a slow refresh can be told from a stuck one.

## IN scope

- `UpdateStatus` carries the moment it was admitted and the moment it started running.
- Both survive `RedisUpdateStatusStore`, which today persists a single integer per key and reconstructs
  the status from key + ordinal (S3). The two Lua scripts that `tonumber()` that value are updated in
  step, preserving monotonic advance and requeue-if-admitted exactly.
- The popover renders an elapsed duration per row.

## OUT of scope

- Queue position or a wait estimate — deferred item G.
- Next scheduled run — deferred item F.
- Historical durations. `RefreshLog.DurationMs` already records those and is already displayed under
  Settings → System Info; this slice is about work still in flight.

## Learning hypothesis

**Disproves that the Redis status hash can carry more than an ordinal safely.**

If it succeeds: the store is extensible and later slices can add fields without a redesign.
If it fails — the monotonic-advance script cannot be made to work over a richer value without losing
its atomicity — then timing has to live outside the store, in a parallel structure the enumeration
joins against, and every future field faces the same wall.

## Acceptance criteria

See US-03 in `feature-delta.md` — AC-03.1 through AC-03.5. The two that carry the risk:

- **AC-03.2** — the guarantees, not just the values. `Advance` must still refuse to move a key backwards
  and `Requeue` must still refuse a key another replica removed. Both are currently enforced *inside*
  Lua over a bare number.
- **AC-03.5** — during a rolling upgrade, an entry admitted by an older replica has no moments recorded.
  The row must render without a duration rather than showing a nonsense one or throwing.

## Dependencies

Slice 02 — there is no row to put a duration on before it.

## Effort

Half a day, plus the probe below.

## Reference class

`epic-5305-k8s-readiness` — Redis-backed state where the expand-only rollout constraint (old and new
replicas sharing one store) was the real design pressure, not the feature itself.

## Pre-slice SPIKE — **CLOSED by DESIGN, 2026-08-23**

The fork is decided. **The ordinal hash and both Lua scripts are frozen; the moments go in a sibling
hash `lighthouse:update-moments`, written and deleted alongside the ordinal but never from inside a
script** — [ADR-182](../../../product/architecture/adr-182-update-moments-in-a-sibling-hash.md),
maintainer ruling.

The serialised-record alternative was rejected because it changes the atomicity guarantee on the hot
path of every admit and advance to serve a read a human triggers occasionally. No probe is needed: the
question was which trade to take, not whether a mechanism exists.

What this slice must still prove, as a test rather than a probe: both scripts are byte-identical to
what ships today.
