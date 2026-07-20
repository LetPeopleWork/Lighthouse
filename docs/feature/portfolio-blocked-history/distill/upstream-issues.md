# DISTILL Upstream Issues — portfolio-blocked-history

**Feature**: portfolio-blocked-history (ADO #5524)
**Date**: 2026-07-20
**Upstream waves consulted**: DISCUSS (D1-D7, 5 user stories), DESIGN (DDD-1..DDD-7, ADR-102/103/104 Accepted)

---

## UI-1: ci-learnings candidate — EF InMemory does not enforce foreign keys (OQ-5)

**Category**: SPECIFICATION_AMBIGUITY → routed to DEVOPS (`docs/ci-learnings.md` maintainer)

**Finding**: The original `DemoBlockedHistoryBackfillHandler` defect shipped green because its tests used `UseInMemoryDatabase`, which does not enforce foreign keys. The repo's ci-learnings ledger does not yet contain this rule despite it being the root cause of a live correctness defect (C2/C5 in feature-delta.md).

**Proposed entry** (ready for insertion into `docs/ci-learnings.md`):

```
### 2026-07-20 — EF InMemory does not enforce foreign keys; FK-dependent tests must run on SQLite
- **Symptom**: Integration tests green locally and in CI against EF InMemory, but the same code
  writes FK violations (e.g., a Feature.Id into a column with FK→WorkItems) that fail silently
  on InMemory and throw `DbUpdateException` on SQLite/Postgres.
- **Root cause**: `UseInMemoryDatabase` disables all foreign-key enforcement. A test asserting
  FK-dependent behavior passes identically for broken and fixed code.
- **Fix**: For any test that inserts/deletes rows relying on FK cascades or FK-constraint
  rejection, use SQLite (the test factory already supports it via `EnsureDeleted()` +
  `EnsureCreated()`). The `PortfolioBlockedHistoryAcceptanceTest` base class enforces this —
  classes inheriting from it always run against SQLite.
- **Rule going forward**: A `[Test]` that asserts a row CANNOT be inserted (FK violation), that
  a cascade DELETE removes child rows, or that an FK-referenced row must exist, MUST run on
  SQLite — never on `UseInMemoryDatabase`. The `IntegrationTestBase` class in the backlog
  tracker's test helpers (epic-5305) already uses SQLite; the blocked-items tests inherited
  from `PortfolioBlockedHistoryAcceptanceTest` do the same. Grep for `UseInMemoryDatabase` in
  new test code and flag it unless the test explicitly documents why FK enforcement is
  irrelevant.
```

**Status**: Awaiting insertion by the ci-learnings maintainer (typically `/clean-ci` or the DELIVER wave close-out). Filed here so DISTILL does not block on a file outside its ownership.

---

## UI-2: No design for the `blockedSince` drill-through null disparity (OQ-3 — RESOLVED, no issue)

**Resolution**: `TeamMetricsController:521` passes `null` for `blockedSince` in historic drill-through while `:152` populates it in historic wip. This is deliberate: drill-through is a membership list, not a duration read. The portfolio path mirrors exactly. No upstream change needed.

---

## UI-3: OQ-2 (reconciliation sweep) is intentionally deferred — not an issue

ADR-104's OQ-2 ("Should the capture path gain a reconciliation sweep to recover Option B's self-healing property?") is deliberately out of scope for slice 02. DISTILL records it here as a known deferred improvement — not a gap in the current wave. The DEPARTURE SWEEP (DDD-5) covers the "permanently blocked because we stopped looking" case; OQ-2 would also cover "handler failure swallowed by dispatcher." Both are detected by the ADR-099 reconciliation guard, which is called from slice 04 (US-04 AC4).

---

## Summary

| ID | Category | Upstream owner | Status |
|---|---|---|---|
| UI-1 | C6 (error contract) → FK enforcement test substrate | DEVOPS (ci-learnings.md) | Filed — await insertion |
| UI-2 | C6 (null `blockedSince` in drill-through) | N/A | RESOLVED (parity-as-is) |
| UI-3 | C2 (reconciliation sweep) | DESIGN (ADR-104 OQ-2) | Deferred — not a gap |
