# Slice 02 — See what Lighthouse is doing right now

**Epic** #5511 Task Manager · **Story** US-02 ·
**Job** `job-operator-see-what-lighthouse-is-doing-right-now`

## Goal

One click from anywhere in the app shows a live, truthful list of what is refreshing and what is waiting.

## IN scope

- `IUpdateStatusStore` gains an enumeration of everything currently admitted. Implemented in
  `InProcessUpdateStatusStore` (iterate the dictionary) and `RedisUpdateStatusStore` (`HashGetAll` — the
  shape `HasActiveWork` already reads).
- `UpdateController` moves off its injected `ConcurrentDictionary<UpdateKey, UpdateStatus>` onto the
  port, and gains a `SystemAdmin`-guarded route returning the enriched list.
- Enrichment on the read path: entity display name looked up per row (D8).
- A new header icon — activity, with an active-count badge — and the popover it opens (D1, D2).
- Live refresh driven by the existing `GlobalUpdateNotification` on the `GlobalUpdates` group.

## OUT of scope

- Elapsed time — slice 03.
- Cancel — slice 04.
- Connection health and warnings — slices 05 and 06.
- **Removing `OAuthHealthIcon`.** It stays, unchanged, beside the new icon until slice 05 (D2).
- Distinguishing *why* a row is queued (held / coalesced / genuinely waiting) — deferred items A and B.

## Learning hypothesis

**Disproves that the status store can answer "what is running" at all.**

If it succeeds: the port can carry the Epic and the popover is real.
If it fails — the Redis hash cannot be enumerated cheaply, or name resolution per row is too expensive
to do on demand — then the Task Manager needs its own projection written on the update path, which is a
different and larger build.

## Acceptance criteria

See US-02 in `feature-delta.md` — AC-02.1 through AC-02.9. Worth calling out:

- **AC-02.2** is a correctness fix in disguise: the endpoint being replaced is already wrong under Redis
  with more than one replica (S4).
- **AC-02.3** — `UpdateType` has five members and the frontend union has three (S11). The two delete
  types will appear in this list; decide how they read rather than letting them render as `undefined`.
- **AC-02.4** — every label goes through Terminology. "Team" and "Portfolio" are renameable.
- **AC-02.9** — verified with a real connector refresh in flight, not a seeded dictionary.

## Dependencies

Slice 01 (D6) — otherwise this list renders every failed run as "Completed".

## Effort

One day. The largest slice in the Epic.

## Reference class

`system-info-auth-visibility`, `epic-5687-faster-updates` slice 05 (`SystemInfoController` +
`RefreshHistorySection`) — a new admin-only read surface over existing backend state.

## Pre-slice SPIKE

None for this slice. **But run slice 04's probe during this slice's implementation** — it is the Epic's
highest-uncertainty question and its answer can resize the remaining work (see prioritisation in
`feature-delta.md`).
