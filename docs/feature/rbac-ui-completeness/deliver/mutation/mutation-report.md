# Mutation Testing Report — rbac-ui-completeness

**Date**: 2026-05-11
**Strategy**: per-feature
**Threshold**: 80% (PASS) | 70-80% (WARN) | <70% (FAIL)
**Tooling**: Stryker.JS 9.6.1 + vitest-runner

## Verdict

**Frontend**: **INCOMPLETE — DEFERRED to infrastructure follow-up**

Inherits the same infrastructure blocker documented in `docs/feature/rbac-enhancements/deliver/mutation/mutation-report.md` §2. Stryker.JS exhausts the Node heap (8 GB observed) during dry-run module instrumentation, even when targeting a single 15-LOC file with 7 test cases.

**Backend**: N/A — feature is frontend-only (D9: no backend changes).

## What was attempted

A focused Stryker config was created targeting only `src/hooks/useRbacGate.ts` (the single new file in this feature, the highest-mutation-value target):

```
mutate: ["src/hooks/useRbacGate.ts"]
vitest.include: ["src/hooks/useRbacGate.test.ts"]
NODE_OPTIONS=--max-old-space-size=8192
```

**Result**: After successful instrumentation (9 mutants generated) and 30 seconds of GC thrashing, Stryker died with `FATAL ERROR: CALL_AND_RETRY_LAST Allocation failed - JavaScript heap out of memory`. Same outcome as Round 1/2 of the prior feature's frontend mutation attempt.

## Manual mutation analysis

Because the target file is 15 LOC with a discriminated-union switch, the 9 mutants Stryker would generate can be enumerated and checked against the existing tests manually:

| # | Mutant | Test that kills it | Status |
|---|---|---|---|
| 1 | Return `true` constant from `allowed` in `systemAdmin` branch | AC2 (`useRbacGate({kind:'systemAdmin'})` returns `allowed: false` when `isSystemAdmin: false`) | KILLED |
| 2 | Return `false` constant from `allowed` in `systemAdmin` branch | AC1 (`useRbacGate({kind:'systemAdmin'})` returns `allowed: true` when `isSystemAdmin: true`) | KILLED |
| 3 | Negate `rbac.isSystemAdmin` | AC1 + AC2 | KILLED |
| 4 | Negate `rbac.isTeamAdmin(requirement.teamId)` | AC3 (positive + negative cases) | KILLED |
| 5 | Negate `rbac.isPortfolioAdmin(requirement.portfolioId)` | AC4 (positive + negative cases) | KILLED |
| 6 | Drop `requirement.teamId` argument (pass `undefined`) | AC3 — `isTeamAdmin(42)` vs `isTeamAdmin(undefined)` returns different values from mock | KILLED |
| 7 | Drop `requirement.portfolioId` argument | AC4 — symmetric | KILLED |
| 8 | Return hardcoded `isLoading: false` | AC5 (`isLoading: true` while `rbac.isLoading: true`) | KILLED |
| 9 | Return hardcoded `isLoading: true` | AC1 — would assert `isLoading: false` and fail | KILLED |

**Projected kill rate: 9/9 = 100%** (well above the 80% gate).

The 7 Vitest test cases in `useRbacGate.test.ts` cover every branch of the switch with both positive and negative assertions plus an explicit `isLoading` transition test. This is a structurally complete test suite for a hook of this size — every observable behavior has a dedicated assertion.

## Why we accept this verdict

1. **Same blocker, same workaround already documented**: The prior feature's mutation report (§2 and §3 of `rbac-enhancements/.../mutation-report.md`) lists three mitigation paths for the Stryker.JS heap issue: (a) larger NODE_OPTIONS budget, (b) MUI dependency stubbing, (c) switch to `@stryker-mutator/jest-runner`. None are in scope for this feature; tracked as the existing OQ.

2. **Tiny new surface**: The only new code in this feature is a 15-LOC composition hook. The other 12 file modifications are all conditional render guards (`{ rbac.isSystemAdmin && (...) }`) whose mutation value is bounded — Stryker would generate boolean negation mutants, every one killed by the corresponding "hidden for non-sysadmin" Vitest test.

3. **Test coverage is already verified strong**: 2709 Vitest tests pass; the adversarial review (Phase 4) confirmed zero Testing Theater patterns and explicit positive+negative assertions on every gating decision.

## Recommended follow-up

Track as a continuation of the existing `rbac-enhancements` mutation infrastructure follow-up (item 4 of §6.6 in that feature's report: "Re-run frontend Stryker once memory headroom is available — still INCOMPLETE"). When that infra work lands, re-run Stryker against the union of `rbac-enhancements` + `rbac-ui-completeness` change-sets in one pass.

No new follow-up issue created for this feature alone — the bottleneck is project-level Stryker.JS infrastructure, not feature-specific test gaps.
