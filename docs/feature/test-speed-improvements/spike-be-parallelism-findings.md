# Spike findings — Backend NUnit fixture parallelization (CS-P)

**Date**: 2026-05-18
**Spike brief**: `slices/spike-be-parallelism.md`
**Outcome**: **PARTIAL — GO with one production-code precondition.** Fixture-scope parallelization is viable and yields a meaningful wall-clock improvement, but a single static-state collision in `AuthenticationMethodSchema` accounts for ~95 % of the failures observed and must be fixed before the attribute can ship. A handful of other tests need narrower per-class fixes.

---

## Method

On `main` after slice-pre (`b404eb07`) and CS-G (`e2589815`) shipped:

1. **Baseline (no attribute)**: `dotnet test ... --filter "Category!=Integration"` — 1 run.
2. **Parallel (`[assembly: Parallelizable(ParallelScope.Fixtures)]` added to `Lighthouse.Backend.Tests/GlobalUsings.cs`)**: same filter — 3 consecutive runs.
3. Attribute reverted; no production or test-file changes shipped from the spike (other than this report).

All runs on the same machine, with the same JIT-warm dotnet test process between runs. Test count: 2 309 (Category!=Integration; the 225 Integration tests stay excluded).

## Wall-clock results

| Run | Duration | Status | Failed | Passed |
|---|---|---|---|---|
| Baseline (serial) | **6 min 04 s** (364 s) | ✓ Pass | 0 | 2 309 |
| Parallel run 1 | 3 min 17 s (197 s) | ✗ Fail | 186 | 2 123 |
| Parallel run 2 | 3 min 10 s (190 s) | ✗ Fail | 150 | 2 159 |
| Parallel run 3 | 3 min 38 s (218 s) | ✗ Fail | 109 | 2 200 |
| **Parallel mean** | **3 min 22 s (202 s)** | — | 148 mean | — |

**Wall-clock speedup observed: 1.8×.** Lower than the 4–8× I projected in the spike brief — confirms an Amdahl bottleneck. Likely sources: the slowest individual fixture(s) (e.g. ForecastServiceTest at ~16 s sum / 29 methods, the surviving Cache.CacheTest concurrency tests at ~5 s each, and the WAF spin-up cost paid per fixture). To go beyond 1.8× we'd need `ParallelScope.Children` (parallelize tests within fixtures too) which requires deeper isolation work.

## Failure analysis

Aggregated across 3 parallel runs:

- **183 distinct test names** failed at least once.
- **108 deterministic** (fail every run).
- **41 fail in 2 of 3 runs**.
- **34 flaky** (fail in exactly 1 of 3 runs).

### Single dominant root cause — `AuthenticationMethodSchema` static collision (~95 % of failures)

Every parallel run logs ~240 occurrences of:

```
System.InvalidOperationException : OAuth authentication methods declared in AuthenticationMethodSchema
  have no matching IOAuthProvider registered: [stub.oauth]
```

(also `[other.oauth]` and `[ado.oauth, jira.oauth, other.oauth]` variants.)

**Mechanism**:
- `Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/AuthenticationMethodSchema.cs:31` declares `public static class AuthenticationMethodSchema`.
- `Program.cs:404` calls `AuthenticationMethodSchema.SetExtraOAuthKeysForTesting(new[] { AuthenticationMethodKeys.StubOAuth })` during the startup of a WebApplicationFactory used by some test fixtures.
- `Program.cs:427-437` validates on every WAF startup that *every* OAuth key declared in the schema has a matching `IOAuthProvider` in DI. If a fixture without the stub provider boots in parallel, the validator finds `stub.oauth` in the (static, shared) schema, can't resolve a provider, and throws `FATAL` — which fails the fixture's `[OneTimeSetUp]` and every test in it.

**Fix path** (production code change — out of spike scope, scope of follow-up slice):
1. **Per-fixture schema isolation**: convert `AuthenticationMethodSchema` from a static class to an instance type registered as `Scoped` (or `Singleton` per WebApplicationFactory, since each WAF builds its own DI container). `Program.cs`'s test hook then writes to *that* WAF's schema instance, not a process-wide global.
2. *Or* — quicker, lower-quality — register a stub `IOAuthProvider` for `stub.oauth` and `other.oauth` keys in test-mode DI so the validator passes. Keeps the static schema but neuters the validator under test.

Option 1 is the right shape; option 2 is a temporary unblock if option 1 is too big.

### Secondary root causes (~5 % of failures)

Beyond the schema collision, three smaller clusters:

| Symptom | Tests affected | Likely cause | Suggested fix |
|---|---|---|---|
| `Moq.MockException` in `ExecuteAsync_*` | 5 tests in `BackgroundServices` namespace (e.g. `ExecuteAsync_MultipleProjects_RefreshesAllProjectsAsync`) | Shared `Mock<T>` instance reused across fixtures; `.Verify` sees calls from sibling fixtures running concurrently | `[NonParallelizable]` on the relevant test class, OR per-test fresh mocks |
| `SeedAsync_CanBeCalledMultipleTimes_WithoutErrors` (3 occurrences — 1 per run, flaky) | 1 test in `TerminologySeederTests` | Shared in-memory DB context across parallel fixtures; seeder sees pre-seeded state from a sibling | `[NonParallelizable]` on `TerminologySeederTests` OR per-test fresh `DbContext` |
| Various `SeedAsync_AddsTerminology_WhenDatabaseIsEmpty(...)` test cases | ~9 occurrences across runs, flaky | Same root cause as above | Same fix |

Total non-schema-collision distinct failures: roughly 15–20 tests across 3–5 fixtures. Tractable with targeted `[NonParallelizable]` annotations.

## Verdict: **PARTIAL — GO with one production-code precondition**

Recommended sequencing for the follow-up `slice-be-parallel-enable`:

1. **Fix `AuthenticationMethodSchema` static collision** (½–1 day, production code) — unblocks ~95 % of failures.
2. **Mark the residual fixtures `[NonParallelizable]`** (~½ day) — 3–5 test classes; lower-effort than refactoring each to be parallel-safe right now.
3. **Add `[assembly: Parallelizable(ParallelScope.Fixtures)]`** to `GlobalUsings.cs`.
4. **Run the full suite 3× locally, then on a PR branch in CI** — confirm green and capture the new wall-clock numbers.

Expected wall-clock after the slice:

- Local: 6 min → **~3 min**.
- CI test step: 11–12 min → **~6–7 min** (CI runners are lower core count than dev boxes, so the speedup factor is smaller).

Not the 4–8× originally projected. Worth doing? Yes — 2× wall-clock at a 1–1.5-day cost is the best lever currently in the catalog. The investment also makes future test additions cheap (parallel-by-default).

If you want a bigger speedup later, the next experiment would be `ParallelScope.Children` after the `[NonParallelizable]` opt-outs settle. But that's a separate spike — out of this one's scope.

## What the spike didn't cover

- `Category=Integration` tests under parallel (would need real API tokens locally).
- `ParallelScope.Children` / `ParallelScope.All` — more aggressive scopes that parallelize within fixtures. The fixture-scope failures suggest those would surface more isolation issues; defer to a follow-up spike if 2× isn't enough.
- CI-side wall-clock under parallel — only validated locally. CI behaves the same in principle (NUnit attribute is consumed identically by the test adapter), but per-runner variance differs.
- Mutation testing under parallel. Stryker per-feature configs are unchanged by this attribute; they invoke `dotnet test` and pick up the assembly attribute. No reason to expect Stryker breakage but the actual Stryker run is out of scope.
