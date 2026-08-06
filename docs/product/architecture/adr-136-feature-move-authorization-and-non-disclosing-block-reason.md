# ADR-136: Feature-level authorization — reuse the shipped result-set filter, evaluate the move conjunction once, and name no Portfolio the user cannot read

**Status**: Accepted
**Date**: 2026-08-06
**Feature**: `epic-5375-manual-sorting` (ADO Epic #5375 "Manual Sorting")
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE

---

## Context

`Feature ↔ Portfolio` is many-to-many (`LighthouseAppContext.cs:217-219`), which makes both halves of
this feature's authorization unusual:

- **Read.** `GET /features` returns rows from many Portfolios at once, filtered to the ones the caller
  can read (D11, AC-1.2).
- **Write.** [ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md) §4 fixed
  the rule as `Portfolios.Any() && Portfolios.All(PortfolioWrite)` — a conjunction over a set the
  *entity* determines, with the empty set denying rather than granting. The `Any()` is not pedantry:
  the premise check found 4 orphaned Features on the dev instance, and a literal `All()` would have
  made every one of them movable by anyone who reached the endpoint.

**One DISCUSS premise is wrong and the correction changes the answer.** DISCUSS records `GET /features`
as "the first endpoint whose *rows* are RBAC-filtered rather than whose *access* is RBAC-gated". It is
not. `FeaturesController.GetFeaturesByPredicate` already does exactly this at `:97-99`:

```
.Where(f => f.Portfolios.Count == 0 || f.Portfolios.Any(p => readablePortfolioIdSet.Contains(p.Id)))
```

Both shipped GETs (`/ids`, `/references`) route through that one private helper. The shape exists, it
has a single choke point, and it is in the controller this feature is extending.

The write side has no equivalent. `RbacGuardAttribute` resolves **one** scope id from a route key
(`RbacGuardAttribute.cs:63-72`, `:78-102`), and `RbacGuardRequirement` (`RbacGuardRequirement.cs`) has
no all-of-a-set member. The attribute mechanism cannot express this rule.

A third problem is disclosure. AC-3.8 wants a disabled move action whose tooltip *names* the blocking
Portfolio — but the blocker may be a Portfolio the user has no `PortfolioRead` on, and naming it leaks
both its existence and its name.

## Decision

**Reuse the shipped result-set filter unchanged for the new read endpoint. Evaluate the move
conjunction in exactly one component, consumed by both the enforcing endpoint and the DTO. Ship the
verdict to the client as a hint that names only Portfolios the caller may already read.**

### 1. Read — `GET /features` reuses `GetFeaturesByPredicate`

The new endpoint is `GetFeaturesByPredicate(_ => true)`. No new filter, no new `FeatureDto`
construction site, no query object.

**Consequence, stated rather than absorbed: orphans become visible.** The shipped filter admits
`f.Portfolios.Count == 0`, so a Feature in no Portfolio appears on the Features view for everyone. That
contradicts AC-1.2's "and lists nothing else". Three ways out were weighed:

| Option | Verdict |
|---|---|
| Tighten the shared helper to exclude orphans | Rejected — silently changes `/ids` and `/references` for every existing caller, to fix a new endpoint's wording |
| Give the new endpoint its own filter | Rejected — two result-set filter semantics on one controller is the exact thing this ADR exists to prevent |
| **Accept orphan visibility; make orphans unmovable** | **CHOSEN** |

Orphans are visible and frozen. This is strictly more honest than the DISCUSS plan: the premise check
warned that an orphan carrying work would be *forecast while invisible*; under this decision it is
forecast and visible. ADR-132 §4's `Portfolios.Any()` already makes it unmovable by everyone including
`SystemAdmin`, so no new rule is needed — the read and write halves agree without being coordinated.
Recorded as a refinement of AC-1.2, not applied silently.

### 2. Write — one authorization component, two consumers

```
IFeatureMoveAuthorization.EvaluateAsync(ClaimsPrincipal, Feature, CancellationToken)
    -> FeatureMoveVerdict(bool CanMove, MoveBlockReason Reason, IReadOnlyList<EntityReferenceDto> BlockingPortfolios)
```

`MoveBlockReason` ∈ `{ None, NoWriteOnAnyPortfolio, NoWriteOnSomePortfolio, Orphaned }`.

Two consumers, one of them authoritative:

| Consumer | Role |
|---|---|
| `PATCH api/v1\|latest/features/{featureId}/rank` | **Enforcement.** `CanMove == false` → 403. This is the security boundary |
| `GetFeaturesByPredicate` → `FeatureDto` | **Hint.** Populates `CanMove` / `MoveBlockReason` / `BlockingPortfolios` so the UI can render the disabled state without guessing |

Evaluation uses the shipped `IRbacAdministrationService.CanWritePortfolioAsync` per Portfolio. A
Feature belongs to one or two Portfolios in practice, so this is a bounded fan-out on the write path
and a per-row fan-out on the read path — the cost is stated in Consequences.

### 3. The block reason names nothing the caller cannot already read

`BlockingPortfolios` contains only Portfolios that **fail** `PortfolioWrite` **and pass**
`PortfolioRead`. The tooltip copy is chosen by reason:

| Situation | Copy |
|---|---|
| Blockers exist and are readable | *"You need edit rights on {Portfolio A} to move this {Feature}."* |
| Blocked, but every blocker is unreadable | *"This {Feature} also belongs to a {Portfolio} you cannot edit."* |
| Orphan | *"This {Feature} belongs to no {Portfolio} and cannot be reordered."* |

The middle row is the disclosure answer: true, actionable ("ask an admin"), and revealing nothing
beyond "at least one exists" — which the user can already infer from the disabled control. The bottom
row deliberately does **not** say "you lack permission", because nobody has it.

This is symmetric with behaviour already shipped: `FeatureDto`'s constructor filters `Projects` to
`readablePortfolioIds` (`FeatureDto.cs:47-55`), so a Feature's own row already hides Portfolios the
caller cannot read. The tooltip follows the DTO's existing disclosure rule rather than inventing one.

### 4. The client must not re-derive the verdict

The frontend renders from `feature.canMove`. It must **not** compute
`feature.projects.every(p => rbac.isPortfolioAdmin(p.id))`.

That expression is the obvious implementation and it **fails open**: `projects` is already
read-filtered, so `every()` runs over a truncated set and returns `true` for exactly the case AC-3.8
exists to block — a Feature shared with a Portfolio the user cannot see. It would also return `true`
for an orphan, whose `projects` is empty. Two fail-open paths in one expression.

`useRbac()` continues to own every role-shaped question (may this instance reorder at all, is the user
a `SystemAdmin`). The project rule that no component fetches `authorization/my-summary` directly is
unaffected. What changes is only that a **per-entity conjunction over a set the client cannot fully
see** is not a role-shaped question and is answered by the server.

## Alternatives Considered

### A. A dedicated query object / `IFeatureQuery` carrying the RBAC filter — rejected

The reviewer's instinct, and defensible: it would make the filter impossible to route around by
construction, rather than by convention.

Rejected on arithmetic. The three existing endpoints would keep `GetFeaturesByPredicate` while the new
one used the query object, taking the number of result-set filter implementations from **one to two**
on the way to an eventual one. Migrating all four at once is a bigger change than this feature, on a
shipped authorization path, for no behavioural difference. Revisit if a fourth Feature-reading
controller appears — at that point the arithmetic reverses.

### B. Push the RBAC filter into `FeatureRepository` — rejected

Would make bypass impossible for every caller at once.

Rejected because `RepositoryBase<Feature>` has no `ClaimsPrincipal` and must not acquire one: it is a
driven port, and RBAC is a driving-side concern (ADR-001's port boundary). Worse, it would be *wrong* —
`ForecastService.cs:72`, `WorkItemService` and every background refresh read Features with no principal
at all and must see the whole instance. A filter there would either silently return nothing for
background work or need a "system" bypass, which is the same hole with more ceremony.

### C. Extend `RbacGuardRequirement` with `PortfolioWriteAll` — rejected

Would keep the write rule in the attribute mechanism, where every other guard lives.

Rejected because the attribute resolves scope from a **route key** (`RbacGuardAttribute.cs:78-102`).
This rule's scope set is discovered from the entity's own state after loading it, so the attribute
would need a pluggable scope-set resolver — new machinery on the one authorization mechanism the whole
product depends on, to serve one endpoint. The explicit call is smaller and reads at the call site.

### D. Name the blocking Portfolio unconditionally — rejected

Best UX, and it is what AC-3.8 literally asks for.

Rejected as an information disclosure: RBAC hides a Portfolio's existence from a user without
`PortfolioRead`, and a tooltip that names it in the course of denying an action is a side channel that
leaks names to anyone who can enumerate Features. The conditional form in §3 gives the good message
whenever it is safe and a true, useful one when it is not.

### E. Return 404 rather than 403 for a Feature the user cannot move — rejected

Would avoid confirming the Feature exists.

Rejected because the Feature is already visible to this user on the very view the action is invoked
from — its existence is not a secret, only the blocking Portfolio's identity is. It would also break
the frontend's ability to distinguish "gone" from "not yours". Note the shipped
`RbacGuardAttribute.OnAuthorizationAsync` already makes exactly this read-vs-write distinction
(`:60-62`): `NotFound` for read requirements, `Forbid` for write. This decision is consistent with it.

## Consequences

**Positive**

- Zero change to a shipped authorization code path. The read filter this feature depends on is already
  in production and already covered by the existing controller's tests.
- One implementation of the move rule, and the consumer that enforces it is the same one the UI reads.
  A UI that renders "enabled" for something the server will refuse is a test failure, not a mystery.
- The `Portfolios.Any()` correction is enforced by the type of the rule rather than remembered: an
  orphan produces `Reason = Orphaned` on both the read and write paths.

**Negative / cost**

- **Per-row authorization on the read path.** `GET /features` evaluates the verdict for every returned
  Feature, each fanning out to `CanWritePortfolioAsync` per Portfolio. At 500 Features × 1-2
  Portfolios that is up to ~1000 permission checks per request. `GetReadablePortfolioIdsAsync` is
  already called once with the distinct id set (`FeaturesController.cs:103-111`); the writable set has
  no batch equivalent on `IRbacAdministrationService`. **Mitigation, and it is required rather than
  optional: resolve the caller's writable Portfolio ids once per request into a set and evaluate the
  conjunction against that set in memory.** If K6 or AC-1.9 shows this is still hot, the honest fix is
  a `GetWritablePortfolioIdsAsync` batch method mirroring the readable one — a small addition to a
  shipped interface, flagged here so it is a measurement away rather than a redesign.
- Orphaned Features appear on the Features view for every user. Cosmetic on the dev instance (4 inert
  Linear leftovers), and the alternative was changing shipped endpoint semantics.
- AC-3.8's tooltip is weaker than written whenever the blocker is unreadable. Accepted deliberately;
  the disclosure is not worth the sentence.
- **D11 remains field-unvalidated.** The premise check found no Feature in more than one Portfolio on
  the dev instance, so the whole conjunction ships proven only by integration tests and seeded demo
  data. Unchanged from DISCUSS, repeated because it is the residual risk of this ADR specifically.

**Quality attribute impact**

- Security: the mutation rule is enforced server-side at one point and cannot be satisfied by a client
  that computes it differently; the client's obvious wrong implementation is named and tested against.
- Confidentiality: no Portfolio name crosses to a user without `PortfolioRead` on it.
- Performance: read-path authorization cost scales with returned rows; bounded by the per-request set
  resolution above, with a named escalation.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| A move requires write on **every** Portfolio | Endpoint test: user with write on A but not B, Feature in both → 403 (AC-3.8, K5) |
| An orphan is movable by nobody | Endpoint test: `SystemAdmin`, Feature with no Portfolio → 403, `Reason = Orphaned` (ADR-132 §4) |
| A user cannot move a Feature outside their write scope | Endpoint test per K5 |
| The DTO hint and the endpoint verdict agree | Integration test asserting `canMove == false` ⟺ the `PATCH` returns 403, over a matrix of read/write scope combinations. This is the only test that can catch a fail-open UI |
| No Portfolio name is disclosed to a caller lacking `PortfolioRead` | Endpoint test asserting `blockingPortfolios` is empty and `canMove` is false for the unreadable-blocker case |
| The client does not re-derive the verdict | Vitest asserting `FeatureMoveMenu` renders disabled for `canMove: false` even when `projects` is empty or fully writable — the two fail-open shapes, pinned |
| Exactly one production type evaluates the move rule | `FeatureMoveAuthorizationSingleSourceArchUnitTest`, mirroring `LicenseGateSingleSourceArchUnitTest.cs` |

## Cross-reference

- [ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md) §4 — the rule
  itself and the empty-set correction. This ADR decides only where it lives and how it surfaces.
- [ADR-135](./adr-135-feature-position-computed-global-ordinal.md) — positions are computed *before*
  this filter runs, which is why filtering cannot renumber.
- Extends **ADR-001** (RBAC port boundary) and **ADR-004** — neither superseded.
- Corrects the DISCUSS checklist claim that `GET /features` is the first result-set-filtered endpoint.
- Refines **AC-1.2** (orphans are visible and unmovable) and **AC-3.8** (the tooltip names a Portfolio
  only when the caller may read it).
