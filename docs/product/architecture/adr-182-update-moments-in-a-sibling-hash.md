# ADR-182: Admission and start moments live in a sibling Redis hash, written outside the Lua scripts

- **Status**: **Proposed** (DESIGN, 2026-08-23)
- **Date**: 2026-08-23
- **Feature**: epic-5511-task-manager (ADO Epic #5511, slice 03 / ADO #5841)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

A row that says "running" cannot distinguish a refresh that started four seconds ago from one that has
been going for forty minutes, and that distinction is the judgement the operator is actually making.
So `UpdateStatus` needs the moment it was admitted and the moment it started running.

The obstacle is the Redis store's representation. `RedisUpdateStatusStore` persists **one integer per
key** — the `UpdateProgress` ordinal — in the hash `lighthouse:update-status`, and reconstructs the
whole `UpdateStatus` from key plus ordinal in `StatusFor`. Two Lua scripts operate on that value:

- `MonotonicAdvanceScript` reads the current value, `tonumber()`s it, compares, and writes only if the
  new ordinal is greater or equal. This is what makes `Advance` monotonic across replicas.
- `RequeueIfAdmittedScript` is `HEXISTS`-guarded so that a key another pod already removed cannot be
  resurrected into a phantom active entry that never completes.

Both guarantees are load-bearing and both are currently enforced *inside Lua, over a bare number*.

## Decision

**The ordinal hash and both its scripts are frozen. The moments go in a sibling hash,
`lighthouse:update-moments`, written and deleted alongside the ordinal but never from inside a script.**

The moments are therefore **best-effort**: absent is a legitimate state, and every consumer must render
a row without a duration rather than a nonsense one. This is not a weakness introduced by the choice —
it is required regardless, because during a rolling upgrade an entry admitted by an older replica has
no moments recorded at all.

A coalesced follow-up (`Requeue`) resets the admission moment, because it is new work waiting rather
than the old work still waiting.

## Consequences

**Positive.** The monotonic-advance and requeue-if-admitted guarantees are provably unaffected: the
scripts are byte-identical to what ships today, which a unit test asserts. The change is additive and
carries no risk to the correctness of the queue. Any future field takes the same route.

**Negative.** Two hashes to keep in step, and a window in which the ordinal exists and the moment does
not. Both are absorbed by treating an absent moment as normal rather than exceptional — which the
rolling-upgrade case already forces. Whether the moments hash needs its own expiry, in case an
abandoned key leaves a moment behind, is recorded as an open question to answer once the code exists.

## Alternatives considered

**Serialise a record per field, and have the advance script parse the ordinal out.** One store, one
write, no drift. Rejected: it changes the atomicity guarantee on the hot path of every admit and every
advance in order to serve a read a human triggers occasionally. The cost lands where the risk is
highest and the benefit lands where the traffic is lowest. The maintainer chose the sibling hash on
exactly this trade.

**No server-side timing — the browser counts from when it first saw the row.** Zero backend change.
Rejected: wrong after a page reload, wrong for a refresh that began before the tab was opened, and
wrong across replicas. Those are most of the cases where the operator is asking the question.
