# Mutation Testing Report — rbac-ui-completeness

**Date**: 2026-05-11 (rerun after Stryker infrastructure fix)
**Strategy**: per-feature
**Threshold**: 80% (PASS) | 70-80% (WARN) | <70% (FAIL)
**Tooling**: Stryker.JS 9.6.1 with `command` runner + vitest 4.1.5 subprocess

## Verdict

**Frontend (`useRbacGate.ts`)**: **PASS — 100.00% (9/9 mutants killed)** in 34 seconds.

```
File            |  total | covered | # killed | # timeout | # survived | # no cov | # errors |
----------------|--------|---------|----------|-----------|------------|----------|----------|
 useRbacGate.ts | 100.00 |  100.00 |        9 |         0 |          0 |        0 |        0 |
```

**Backend**: N/A — feature is frontend-only (D9: no backend changes).

## Infrastructure fix

The first attempt OOM'd at 8 GB heap during the Stryker main-process project read. Root cause: Stryker walks the project tree and copies it into a sandbox; the default-ignored list (`node_modules`, `.git`, `.stryker-tmp`, `reports/`, `*.tsbuildinfo`, `stryker.log`) does NOT include this project's `src-tauri/` directory (≈25 GB of Rust build artifacts) or `publish/` (≈1 GB of Vite output). Stryker's `ProjectReader` exhausted heap simply trying to enumerate them.

**Fix**:
- Added `ignorePatterns: ["src-tauri", "publish", "dist", "coverage", "playwright-report", "test-results", "sonar-report.xml", "StrykerOutput"]` to `stryker.config.mjs`.
- Switched test runner from `@stryker-mutator/vitest-runner` (which OOMs during dry-run coverage instrumentation even with a tight `vitest.stryker.config.ts`) to Stryker's built-in `command` runner, invoking `pnpm exec vitest run --config vitest.stryker.config.ts --reporter=dot` per mutant. Trade-off: no per-test coverage filtering; runs the included tests for every mutant. With the include list limited to the relevant test file, per-mutant runtime is ~1.2 s.
- Project default `vitest.config.ts` also got an `exclude: ["**/.stryker-tmp/**", ...]` so default `pnpm test` discovery doesn't pick up stale Stryker sandboxes.

Run command: `pnpm exec stryker run stryker.config.mjs` (no special `NODE_OPTIONS` needed; default 4 GB heap is sufficient).

## Mutant detail

All 9 mutants are killed by the 8 Vitest tests in `src/hooks/useRbacGate.test.ts`. The 8th test (`systemAdmin requirement reads isSystemAdmin (not isTeamAdmin) — pins case body against switch-fallthrough mutants`) was added specifically to kill a `BlockStatement` mutant that survived the first run: removing the body of `case "systemAdmin":` causes the switch to fall through to `case "teamAdmin":` and call `rbac.isTeamAdmin(undefined)`. With `isRbacEnabled: false`, the `PERMISSIVE_SUMMARY` semantics make `isTeamAdmin(undefined)` return `true`, while `isSystemAdmin: false`. The added test asserts the systemAdmin requirement returns `allowed: false` under this configuration — killing the mutant.

## Status

Ready for release. `pre-release checklist` item "Mutation testing per `per-feature` strategy" in the feature evolution doc is now satisfied for this feature.
