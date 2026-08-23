# ADR-181: Update activity is a read through `IUpdateStatusStore`, enriched by an application service, not a projection written on the update path

- **Status**: **Proposed** (DESIGN, 2026-08-23)
- **Date**: 2026-08-23
- **Feature**: epic-5511-task-manager (ADO Epic #5511, slice 02 / ADO #5840)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

The Task Manager needs a list of everything currently refreshing or waiting. Three facts constrain how
that list can be produced.

`IUpdateStatusStore` already holds exactly that set — every key admitted by `TryAdmit` and not yet
`Remove`d. But it cannot be enumerated. Its whole read surface is `TryGet(key, out status)`,
`HasActiveWork()` returning a bool, and `HasQueuedWork(keys)` scoped to keys the caller already names.
There is no "give me everything".

`UpdateController` does not use the port at all. It injects
`ConcurrentDictionary<UpdateKey, UpdateStatus>` directly and counts it. In the single-instance product
that dictionary *is* the store's backing field, so the answer is right by accident. Under Redis with
more than one replica it is the calling pod's dictionary and nothing else, so
`GET /api/latest/update/status` already under-reports in the multi-replica product — a defect that
exists today, independently of this Epic.

`UpdateStatus` carries three fields: `UpdateType`, `Id`, `Status`. There is no entity name. A list of
`Team_12` and `Features_4` is not a task manager. And the Redis store reconstructs `UpdateStatus` from
key plus ordinal in `StatusFor`, so a name pushed through the write path would have to survive a hash
that holds one integer per field, on the hot path of every admit and every advance, to serve a read
that happens when a human opens a popover.

## Decision

**The list is a read through the port. The port learns to enumerate. Name resolution happens in an
application service on the read path.**

Three parts:

1. `IUpdateStatusStore` gains an enumeration of everything currently admitted, implemented by both
   stores. `RedisUpdateStatusStore.HasActiveWork` already reads the whole hash with `HashValues`, so
   the Redis implementation is `HashGetAll` over a shape it already reads — not a new access pattern.

2. `UpdateController` stops injecting the dictionary and depends on `IUpdateStatusStore`. Its existing
   `status` route is re-implemented over the port, which corrects the multi-replica under-report as a
   side effect rather than leaving a known-wrong endpoint next to a new right one.

3. A new `UpdateActivityService` takes what the store enumerates and resolves each row's display name
   from the repositories. The controller does not fan out to repositories; the store does not touch a
   database.

A row whose entity no longer exists — the `PortfolioDelete` and `TeamDelete` update types are exactly
this case — renders by type and id. It does not vanish and it does not throw.

## Consequences

**Positive.** One truth about what is running, in the place that already owns it. A live defect is
corrected rather than routed around. The hot path is untouched: nothing new is written on admit or
advance. The store keeps no database dependency and the controller keeps no repository fan-out, so both
stay testable in isolation.

**Negative.** One repository lookup per row per popover open. This is accepted rather than optimised:
a popover open is not a hot path, the row count is bounded by concurrent refreshes, and caching a name
introduces a staleness question nobody has asked yet. Recorded as an open question, to be measured on a
real instance rather than pre-empted.

**Enforced by**: an ArchUnit rule that nothing outside the update package injects
`ConcurrentDictionary<UpdateKey, UpdateStatus>`, following the established `*SeamArchUnitTest`
convention.

## Alternatives considered

**A projection written on the update path** — the queue writes an activity record as work is admitted
and advanced, and the controller reads that. Rejected: it is a second copy of a set that already exists
in exactly one place, with its own lifecycle to keep in step, and the failure mode is the two
disagreeing. This Epic exists because two records already disagree about whether a refresh failed;
adding a third is the wrong direction.

**Keep the dictionary injection and add the list beside it** — smaller diff. Rejected: it leaves a
known-wrong endpoint permanently next to a right one, answering the same question differently depending
on which route the caller picks.

**Store the entity name in `UpdateStatus`** — no read-path lookup. Rejected on the Redis
representation: it forces a richer encoding onto the hash and its two Lua scripts, on every admit and
advance, for a read that a human triggers occasionally. See [ADR-182](./adr-182-update-moments-in-a-sibling-hash.md),
which reaches the same conclusion about the timing fields for the same reason.
