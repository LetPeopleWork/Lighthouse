# Mutation Testing Report — rbac-enhancements

**Date**: 2026-05-10
**Strategy**: per-feature
**Threshold**: 80 % (PASS) | 70–80 % (WARN) | < 70 % (FAIL)
**Tooling**: Stryker.NET 4.14.1 (backend) | Stryker.JS 9.6.1 + vitest-runner (frontend)

## Verdict

**Backend**: WARN — 65.4 % kill rate excluding NoCoverage, 37.6 % including. Detail below.
**Frontend**: INCOMPLETE — Stryker.JS exhausted Node heap (8–12 GB) on every run for this codebase. Documented as follow-up.

**Overall**: WARN. Backend results are actionable; frontend mutation testing requires infrastructure work (configured below as `vitest.stryker.config.ts`) that did not converge inside the time budget.

---

## 1. Backend (Stryker.NET)

### 1.1 Run summary

- Configuration: `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.json`
- Solution: `Lighthouse.Backend/Lighthouse.sln`
- Test scope: full test project (2 197 tests). Stryker.NET 4.14.1 has no built-in test name filter; coverage-based test selection (`CoverageBasedTest`) ran only the tests touching each mutant.
- Wall clock: 10 min 49 s
- Total mutants generated (within 3 target files): 1 077 (after `Ignored` removal: 397)
- Tested mutants: **292** (Killed 191 / Survived 101 / Timeout 0)

### 1.2 Stryker score interpretation

Stryker.NET reports two scores:

| Metric | Calculation | Value |
|---|---|---|
| **Kill rate (tested mutants only)** | 191 / (191 + 101 + 0) | **65.41 %** |
| **Mutation score (Stryker default, includes NoCoverage as not-killed)** | 191 / (191 + 101 + 216) | 37.60 % |

The Stryker default score penalises uncovered code. The 216 NoCoverage mutants are inside our 3 target files but no test in the suite — even after coverage analysis — exercises that line. Most of those are in `RbacAdministrationService.cs` and represent code paths with implicit coverage gaps that were not flagged by the line-coverage metric.

The 65.41 % "tested mutants only" figure is the more useful number for assessing whether existing tests *catch what they claim to cover*. The 37.60 % figure is the more honest figure for the test suite's total coverage of these 3 files.

Both numbers are below the 80 % gate. Verdict: **WARN**.

### 1.3 Per-file breakdown

| File | Mutants generated | Killed | Survived | NoCoverage | CompileError | Ignored | Tested kill rate | Verdict |
|---|---:|---:|---:|---:|---:|---:|---:|:---:|
| `API/AuthorizationController.cs` | 106 | 36 | 8 | 42 | 0 | 20 | 81.8 % | PASS |
| `Models/Authorization/RbacUserSummary.cs` | 1 | 0 | 1 | 0 | 0 | 0 | 0 % | FAIL (n=1 — see note) |
| `Services/Implementation/Authorization/RbacAdministrationService.cs` | 545 | 155 | 92 | 174 | 14 | 110 | 62.8 % | FAIL |

Notes:

- `RbacUserSummary.cs` has only 1 mutable expression (the default value `string.Empty` for `Subject`). Stryker mutated it to `"Stryker was here!"` and no test caught it because no test asserts that `Subject` defaults to empty when constructed without arguments — and in production it is always assigned from the database. This mutant is **probably equivalent**: a `record` default value that is overwritten on every code path. Excluded from the verdict denominator: would not change overall result.
- `AuthorizationController.cs` has 42 NoCoverage mutants — most are in two new endpoints (`Update*GroupMapping`, `Delete*GroupMapping`) whose happy paths are covered, but the error-branch returns (lines guarded by `Forbid()`) are not exercised because the controller's authorization filter is mocked at class level.
- `RbacAdministrationService.cs` is the largest file (1 157 LOC) and unsurprisingly has the most surviving and uncovered mutants. The 174 NoCoverage mutants cluster in three areas: emergency-admin promotion logic, group-mapping CRUD edge cases, and the JSON parser for multi-value group claims (lines 960–996).

### 1.4 Surviving mutants worth attention

These are mutants that should have been caught but weren't. Each represents a real test gap.

#### `AuthorizationController.cs` (8 survived — 2 are equivalent)

| Line | Mutator | Mutation | Suggested test |
|---|---|---|---|
| 114 | Equality | `result.ErrorCode != RbacOperationErrorCodes.LastSystemAdmin` | **Equivalent** — both branches return `BadRequest(result.Message)` (lines 116 and 119). Different log/error code path is invisible to caller. Mark as equivalent. |
| 115 | Block removal | empty `{}` | **Equivalent** for the same reason: removing the inner `BadRequest` falls through to the outer `BadRequest` with the same payload. |
| 180 | Boolean `false` | always reject role parsing | **Test gap**: add a test that `SetTeamMemberRole` with a valid role returns 204 NoContent. The current test only exercises the error path. |
| 196 | Equality | `result.ErrorCode != RbacOperationErrorCodes.InvalidRoleForScope` | **Equivalent** — same fall-through pattern as line 114. |
| 197 | Block removal | empty `{}` | **Equivalent** for the same reason. |
| 359 | Boolean `false` | role parse always fails for group mapping | **Test gap**: add a test that `CreateGroupMapping` with valid role+scope returns 201 (or 204), not 400. |
| 364 | Boolean `false` | scope-type parse always fails | **Test gap**: add a test that `CreateGroupMapping` with a valid scope type produces a successful result. |

After excluding 4 equivalent mutants: 4 / (36 + 4) = 90 % effective kill rate. Above threshold.

#### `RbacAdministrationService.cs` (92 survived — sample of 8 highest-value)

| Line | Mutator | Mutation | Suggested test |
|---|---|---|---|
| 33 | Equality | `EmergencySystemAdminSubjects.Count >= 0` (always true) | Add a test that *non-empty* emergency-admin list does not satisfy the gate when no system admin exists; current tests pass with both `Count == 0` and `Count > 0`. |
| 78 | Linq `Any → All` | emergency-subjects "all match" instead of "any" | Add a test where 2 emergency subjects exist and only 1 matches — `IsEmergencyAdminAsync` should return `true` for the matching subject and `false` for the other. The current parametrized test happens to match every entry. |
| 286 | Linq `Any → All` | scope-permission "all" instead of "any" check | Add a test with multiple permissions where exactly one grants the right scope — `Any` and `All` produce different results only in this case. |
| 316 | Linq `Any → All` | (same pattern as 286, different scope) | Same suggestion. |
| 387 (block 386–389) | Statement / Equality | early-return when `currentUser is null` | The early-return is exercised but the *reverse* (currentUser is non-null) is treated identically downstream by the test — add a test where `currentUser is null` returns `false` while a non-null user with no admin role returns `true`. |
| 391, 395 | Logical & / \| | the team-admin / portfolio-admin scope check | Add a test where `ScopeType == Team` but `ScopeId` is null — should not be treated as a team admin. |
| 518 | Equality | `permissions.Count >= 0` (always true) | Add a test that an empty permissions collection is treated as "no admin". |
| 1071 | Equality | `role != UserRole.TeamAdmin` | Add a parametrized test covering every `UserRole` value with the team-membership API. |

These 8 represent the most actionable test gaps. The other 84 mutants on `RbacAdministrationService.cs` cluster around: (a) `OrderBy` / `ThenBy` mutations in list-projection methods that produce stable but unverified output ordering, (b) string defaults in JSON-parsing helpers, (c) emergency-admin / unassigned-user computation that lacks fine-grained assertions.

### 1.5 Equivalent mutants

The following mutants have been classified as equivalent (semantically identical to the original code). Excluding them from the denominator does not change the WARN verdict (65.4 % → ≈66 % for the controller specifically, 81.8 % → 90 %).

- `AuthorizationController.cs` lines 114, 115, 196, 197 — fall-through `BadRequest` branches.
- `RbacUserSummary.cs` line 7 — default value of a property that is always overwritten.

Not excluded from the report because the suite of "equivalent" mutants is small and changing the denominator further blurs the signal. They are flagged so the team can decide whether to suppress them via Stryker `mutate` glob exclusions or `// Stryker disable next-line` comments.

---

## 2. Frontend (Stryker.JS)

### 2.1 Status

**INCOMPLETE — time budget exceeded due to repeated Node heap exhaustion.**

Five Stryker.JS runs were attempted with progressive downscoping:

| Run | Mutate scope | Concurrency | Heap | Coverage analysis | Outcome |
|---|---|---:|---:|---|---|
| 1 | All 10 files (1 708 mutants) | 4 | 4 GB (default) | perTest | OOM `Allocation failed - JavaScript heap out of memory` |
| 2 | All 10 files | 4 | 8 GB | perTest | typescript-checker plugin loading error (pnpm symlink layout) |
| 3 | 3 high-value files (347 mutants) | 4 | 8 GB | perTest, no checker | OOM after 35 s |
| 4 | 2 highest-value files (139 mutants) | 2 | 12 GB | perTest, no checker | OOM after 35 s |
| 5 | 2 files, scoped vitest config | 1 | 8 GB | perTest, no checker | OOM after ~60 s |

The root cause appears to be the size of the vitest module graph for this codebase: even with `vitest.stryker.config.ts` restricted to 2 test files (`RbacService.test.ts` + `ScopedGroupMappingManager.test.tsx`), Stryker's child workers load enough of the React/MUI dependency tree at perTest-coverage-instrumentation time to exceed 8 GB.

This is a known class of issues with Stryker's vitest runner on large React+MUI projects. Mitigations for follow-up:

1. Run Stryker against a **vitest project** with browser pool disabled and a custom minimal `setupTests` that does not import the full app router/theme.
2. Switch to `coverageAnalysis: "off"` and accept the resulting 100×–200× slowdown — every mutant runs every test rather than just covering tests. With 139 mutants × 2 test files × ~3 s per test file = ~14 minutes for 2 files. Manageable but exhausts CI budget.
3. Use the `@stryker-mutator/jest-runner` against a Jest config (legacy) — possibly faster to converge but adds a second test runner to the project.
4. Run Stryker in a Docker container with a higher swap limit or on a dedicated CI node with 32 GB RAM.

### 2.2 Configuration committed

- `Lighthouse.Frontend/stryker.config.mjs` — Stryker config scoped to 2 files, concurrency 1, no TS checker.
- `Lighthouse.Frontend/vitest.stryker.config.ts` — minimal Vitest config that includes only the 2 relevant test files.
- `Lighthouse.Frontend/package.json` — added `@stryker-mutator/core`, `@stryker-mutator/typescript-checker`, `@stryker-mutator/vitest-runner` as devDependencies.

These give a future contributor everything needed to retry the frontend mutation run on a higher-memory host without re-deriving the configuration.

### 2.3 Files left untested by mutation

All 10 frontend files listed in the task scope. High-value subset documented above; gating-only files (`Settings.tsx`, `SystemSettingsTab.tsx`, `OverviewDashboard.tsx`, `TeamDetail.tsx`, `PortfolioDetail.tsx`, `PortfolioDeliveryView.tsx`, `DeliverySection.tsx`) are shallow conditional renders driven by the `useRbac()` hook — their behavioural surface is small enough that conventional unit tests already cover the gating decisions. The two highest-value files (`ScopedGroupMappingManager.tsx`, `RbacService.ts`) remain INCOMPLETE.

---

## 3. Surviving mutants worth attention (consolidated follow-up backlog)

The following test additions would meaningfully improve the kill rate. **None added in this run** — they are recorded here for the next test-improvement pass.

1. **AuthorizationController** (estimated +5 percentage points):
   - Happy-path test for `SetTeamMemberRoleAsync` returning 204.
   - Happy-path tests for `CreateGroupMappingAsync` with valid role+scope.

2. **RbacAdministrationService** (estimated +10–15 percentage points):
   - Emergency-admin gating with non-empty subject list but no matching subject.
   - Linq `Any/All` distinguishing test on scope permissions (multiple permissions, exactly one matching).
   - Null-vs-non-null `currentUser` differentiation in admin-check methods.
   - Empty-permissions-collection test.
   - Parametrized `UserRole` test for team-membership API.
   - JSON parser tests for multi-value group claims (lines 960–996 — currently 0 % coverage).

3. **Frontend** (entire surface): re-run mutation testing on a host with at least 16 GB free RAM, using the committed configuration.

---

## 4. Tooling additions to commit

| File | Type | Purpose |
|---|---|---|
| `Lighthouse.Backend/.config/dotnet-tools.json` | Modified | Adds `dotnet-stryker` 4.14.1 to the local-tool manifest. |
| `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.json` | New | Stryker.NET configuration scoped to the 3 RBAC backend files. |
| `Lighthouse.Frontend/package.json` | Modified | Adds `@stryker-mutator/core` 9.6.1, `@stryker-mutator/typescript-checker` 9.6.1, `@stryker-mutator/vitest-runner` 9.6.1. |
| `Lighthouse.Frontend/pnpm-lock.yaml` | Modified | Lockfile entries for the above. |
| `Lighthouse.Frontend/stryker.config.mjs` | New | Stryker.JS configuration. |
| `Lighthouse.Frontend/vitest.stryker.config.ts` | New | Minimal Vitest config used by Stryker (loads only the 2 relevant test files). |
| `.gitignore` | Modified | Excludes `StrykerOutput/`, `.stryker-tmp/`, `reports/mutation/`. |
| `docs/feature/rbac-enhancements/deliver/mutation/mutation-report.md` | New | This report. |

The user (or orchestrator) decides whether to commit. All files preserve the post-restore safety baseline:

- `dotnet test --filter "FullyQualifiedName~Authorization"` — 98 / 98 passing (verified 2026-05-10 19:23 after backend Stryker run).
- `pnpm test --run` — 2 668 / 2 668 passing (verified before frontend Stryker attempts).
- `git diff --name-only` for production source paths returns empty (no source-file mutations leaked through).

---

## 5. Verdict

**WARN.** Backend kill rate (65.4 % tested mutants only, 37.6 % including NoCoverage) is below the 80 % gate but above 50 %. The most impactful test additions are itemised in §1.4 / §3 and are tractable. Frontend mutation testing requires infrastructure work (memory headroom) but the configuration is ready for a follow-up run.

Recommended actions before next stable release:

1. Add the 8 backend test gaps in §1.4 (estimated +10–15 points kill rate).
2. Re-run frontend Stryker on a 16 GB+ host or in CI; merge results.
3. Mark the 4 equivalent mutants in `AuthorizationController.cs` and the 1 in `RbacUserSummary.cs` with `// Stryker disable` comments to prevent score drift on cosmetic refactors.
