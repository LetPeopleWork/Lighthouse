# ADR-132: Feature ordering — a derived total order over a Feature attribute, not an ordering aggregate

**Status**: Accepted
**Date**: 2026-08-06
**Feature**: `epic-5375-manual-sorting` (ADO Epic #5375 "Manual Sorting")
**Decider**: Hera (DDD Architect), DESIGN domain layer, interaction mode = PROPOSE

---

## Context

Epic #5375 gives the instance one manually-maintained order over **every** Feature (D2, D3) that the
Monte Carlo simulation consumes: each simulated day a team draws from the first `FeatureWIP` remaining
Features **in sequence** (`ForecastService.cs:201-209`, fed by `featureRepository.GetAll()` at `:72`).
Ordering is therefore a forecasting input, not a display preference.

Three facts from the codebase shape who may own that order:

- **`Feature` is a high-churn, sync-owned entity with no concurrency token.** ADR-027 put optimistic
  tokens on the five human-edited config roots only; `LighthouseAppContext.cs:221-243` shows tokens on
  Team, Portfolio, WorkTrackingSystemConnection, Delivery and UserProfile — and none on `Feature`,
  deliberately, because it is rewritten on every sync.
- **`Feature ↔ Portfolio` is many-to-many** (`LighthouseAppContext.cs:217-219`). No Portfolio contains
  the order; a team spans Portfolios, so per-Portfolio orderings would give its simulation no
  unambiguous next Feature (D3).
- **`WorkItemBase.Update` overwrites `Order` from the source system on every refresh**
  (`WorkItemBase.cs:142`), so the manual value must live in a separate column (D5).

The order is instance-global, but no existing aggregate root spans the instance. The question this ADR
settles is what owns it, and how strong the "ranks form a permutation" property has to be.

## Decision

**Manual rank is a plain nullable attribute of `Feature`. The order is a *derived total order* over
that attribute. There is no ordering aggregate, and contiguity is not an invariant.**

### 1. Ownership

| Concern | Owner |
|---|---|
| The rank value | `Feature.ManualRank` (`int?`), a scalar attribute of the existing `Feature` aggregate |
| The *policy* — which of the two orders is authoritative | An instance-scoped **Ordering Policy** (`SourceOrder` \| `ManualOrder`), single-valued, read at exactly **one** selection point |
| The *sequence* | Nobody. It is **derived** at read time from the rank attribute; it is not stored, not versioned, and has no root |
| Writes to the rank | A single domain service (`IFeatureRankingService`) — the sole writer, one command: insert-at-target |

Vernon's rules, applied:

- **Rule 1 (model true transactional invariants).** After the relaxation in §2 there is no invariant
  that spans two Features, so there is nothing for a new aggregate to protect. Rule 1 therefore
  *forbids* creating one.
- **Rule 2 (small aggregates).** A "Backlog Ordering" root containing every Feature in the instance is
  the textbook god aggregate — hundreds to low thousands of members, loaded in full to move one row,
  serialising every move against every other. Rejected.
- **Rule 3 (reference by identity).** The move command carries Feature **ids** only, never rank
  numbers or positions. This is what makes concurrent moves safe without a token (§3).
- **Rule 4 (eventual consistency outside the boundary).** The forecast recompute a move implies runs
  after commit, off the domain-event seam — see [ADR-133](./adr-133-feature-rank-change-publishes-domain-event.md).

### 2. The consistency contract — a total order, not a permutation

> **INV-O1 (derived total order).** The Feature ordering is `ManualRank` ascending, **nulls last**,
> ties broken by `Feature.Id` ascending. This function is **total over any rank multiset** — including
> gaps, duplicates and nulls — so it never returns an ambiguous or partial order.

> **INV-O2 (contiguity is a post-condition, not a contract).** No consumer may read a rank *value* or
> assume ranks are contiguous, dense, or 1-based. D13's dense block renumber is retained as the *move
> algorithm* because at this scale it is one set-based statement; it is **demoted from an invariant to
> an implementation property**.

> **INV-O3 (the user-visible position is a computed ordinal).** The `#` column is the Feature's
> 1-based index in the global ordering, computed on read — not the stored rank. This is free:
> `FeatureRepository.GetAll` (`:16-18`) already materialises and sorts the entire Feature table in
> memory on every call.

> **INV-O4 (rank assignment is a repairable property, not a transactional one).** A Feature with no
> rank sorts at the tail by `Id`, which is exactly D7's "append silently to the end". The sync path
> *should* assign `max + 1` on arrival, but correctness does not depend on it, and a missed assignment
> needs no repair job. Move-to-Bottom materialises a rank for any null-ranked row it must jump —
> bounded work, on the one operation that cares about the tail.

The relaxation is not a performance argument. A dense renumber over 500 rows is one `UPDATE … WHERE
rank BETWEEN …`, milliseconds on SQLite, comfortably inside K6's 500 ms budget. What the relaxation
buys is (a) the absence of any cross-Feature invariant, which is what makes §1's "no aggregate" answer
available at all, (b) the transaction boundary in §3, and (c) the freedom to swap the move algorithm
later — slice 03's D4 fallback to slot permutation, or stride-seeding plus midpoint insert if K6 ever
fails — **without touching a single consumer**.

### 3. Concurrency

- **No concurrency token on `Feature`.** Adding one would contradict ADR-027 and manufacture
  `DbUpdateConcurrencyException` on every routine sync, since `Feature` is rewritten on each refresh.
- **The move command carries identities, not positions** — `{ featureId, beforeFeatureId |
  afterFeatureId }`. A position-carrying command (`moveTo: rank 7`) would need a token to be safe; an
  identity-carrying one is meaningful against whatever the order currently is. D18's "every gesture
  reduces to insert-at-target" already produced exactly this shape.
- **Smallest correct transaction boundary: one database transaction per move**, containing the
  re-read of the target's current rank *inside* the transaction and the shift of the affected block.
  Not a lock over all Features, not an aggregate-wide version.
- **Two concurrent moves**: last-writer-wins on *intent*. If A moves X above Y while B moves Y above
  X, the surviving order is whichever committed last. No 409, no merge. This is honest and acceptable
  at 20-150 rarely-concurrent users; the failure mode is a surprising order, never a corrupt one.
- **A move concurrent with the sync appending a Feature (D7)**: the sync's `max + 1` and the move's
  shift can collide on a value. INV-O1 makes the collision harmless — a duplicate rank resolves by
  `Id`. This is the concrete payoff of the relaxation: the renumber does **not** need to serialise
  against the refresh, which retires DISCUSS open question 3's "decide the transaction boundary so a
  concurrent refresh cannot interleave with a renumber" — it may interleave, and nothing breaks.

### 4. Authority to move (D11), and the empty-set trap

Authority is **write on every Portfolio the Feature belongs to**, because the many-to-many relation
means one move re-sequences an object another Portfolio forecasts against. Confirmed as written, with
one correction that is a security bug if taken literally:

> `feature.Portfolios.All(canWrite)` is **`true` for a Feature in no Portfolio.** The 4 orphaned
> Features on the dev instance would be movable by anyone reaching the endpoint. The rule is
> `Portfolios.Any() && Portfolios.All(canWrite)` — an orphan is movable by nobody, which matches the
> fact that D11's `PortfolioRead` filter already makes it invisible to everybody.

## Alternatives Considered

### A. A new "Backlog Ordering" aggregate root owning the sequence — rejected

Attractive because it makes "the order" a first-class thing with a natural home for a version token,
and because it puts the total-order invariant inside a boundary where Rule 1 would protect it.

Rejected on Rule 2. The root's members are every Feature in the instance, so it is a god aggregate by
construction: a move loads the whole backlog, and every move serialises against every other move and
against the sync appending at the tail. It also creates a second source of identity that must track
Feature lifecycle — creation, deletion, and `OrphanedFeatureCleanupService` — for no invariant that
the relaxation in §2 does not already dissolve. An aggregate whose boundary is "the entire instance"
is a lock wearing a domain-model costume.

### B. The instance-settings aggregate owns the ranks — rejected

Rejected because it is the same god aggregate at a different address: a 500-entry ordered list in a
settings row, with Feature identity duplicated into it and no cascade when a Feature is deleted.
Settings correctly owns the **policy** (§1) — one value, instance-scoped, human-edited, low-churn,
exactly the shape ADR-027 tokens. It does not own the data.

### C. Keep "dense contiguous permutation 1..N" as a transactional invariant (D13 as written) — rejected as a *contract*, retained as an *algorithm*

Rejected as a contract because it forces every move to serialise against every other move **and**
against the sync path's tail append, for a property that no consumer reads. Retained as the algorithm
because at this scale the block shift is one statement and is simpler than a midpoint-insert branch
plus a rebalance path.

### D. Sparse / LexoRank-style fractional keys — deferred, contract-compatible

D13 rejected these as buying nothing at this size, and that holds. INV-O2 makes them a drop-in later:
seed with a stride, insert at the midpoint, renumber a local window when the gap closes. Recorded here
so the option stays open rather than being re-derived.

## Consequences

**Positive**

- No new aggregate, no new root, no new concurrency token, no repair job. The domain surface of this
  epic is one nullable column, one policy value, and one service.
- The move algorithm is swappable without touching consumers — which is precisely what slice 03's
  learning hypothesis (D4 may be wrong) needs, and what makes its fallback cheap.
- A concurrent refresh may interleave with a renumber. This removes the coarse transaction the DISCUSS
  handoff assumed would be necessary.
- The `Order` column keeps its exact source semantics, so D9's off-switch is a genuine revert.

**Negative / cost**

- Last-writer-wins between two simultaneous moves. Stated, not mitigated; there is no 409 to surface
  and no UI affordance that would help.
- The displayed position is a computed ordinal, so it is *not* the stored value. Anyone debugging
  against the database sees ranks that do not match the screen once gaps appear. This is a real
  ergonomics cost of the relaxation and is the main argument the rejected option C had going for it.
- Every consumer must sort by the full key (`rank`, nulls last, `Id`). A consumer that sorts by rank
  alone is subtly wrong only when duplicates or nulls exist, which is the hardest kind of bug to
  notice. Guarded by an integration test that feeds a deliberately gapped, duplicated and
  partially-null rank set through all five ordering call sites and asserts identical sequences.

**Quality attribute impact**

- Correctness: the ordering function is total under every reachable rank state, including states no
  algorithm intends to produce.
- Performance: unchanged read path (the whole table was already sorted in memory); one set-based
  UPDATE per move.
- Modifiability: the rank storage strategy is behind one service and one comparison point.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Exactly one point selects source-order vs manual-order; not five | Integration test asserting the Portfolio DTO, `GET /features` and the forecast input agree in every policy state (K4 / AC-2.3) |
| No consumer assumes contiguity | Integration test over a gapped + duplicated + partially-null rank set; sequences must be identical across all five call sites |
| A move changes ranks and nothing else | Complement-equality test: `Order`, `State`, `FeatureWork`, `Forecasts` and Portfolio membership byte-identical before and after |
| An orphaned Feature is movable by nobody | RBAC test asserting 403 for a SystemAdmin on a Feature belonging to no Portfolio |
| The rank is never written by the sync path except as a tail append | Test: a full refresh with the policy on changes no existing Feature's rank (K2 / AC-2.2) |

## Cross-reference

- Extends ADR-027 (aggregate/token set unchanged; `Feature` stays untokened) — no clause superseded.
- [ADR-133](./adr-133-feature-rank-change-publishes-domain-event.md) — what a completed move publishes,
  and what recomputes.
- Full analysis: `docs/product/architecture/brief.md` → `## Domain Model — epic-5375-manual-sorting`.
- DISCUSS decisions D2, D3, D4, D5, D7, D11, D13, D15, D18 in
  `docs/feature/epic-5375-manual-sorting/feature-delta.md`.
