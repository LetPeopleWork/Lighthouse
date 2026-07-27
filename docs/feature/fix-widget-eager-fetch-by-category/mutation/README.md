# Mutation testing — Bug #5571 (feature-scoped)

Run 2026-07-27 against commits `35f3524e1..3c36064bb`.

## Result — PASSES the project's 80% gate

| File | Killed | Survived | No cov | Score |
|---|---|---|---|---|
| `useCategorySelection.ts` | 39 | 5 | 0 | **88.64%** |
| `categoryMetadata.ts` | 264 | 43 | 1 | **85.71%** |
| `useMetricsData.ts` | 242 | 49 | 13 | **79.61%** |
| `TotalWorkItemAgeWidget.tsx` | 11 | 25 | 0 | **30.56%** |
| **Total** | **556** | **122** | **14** | **80.35%** |

## Reading the two low numbers honestly

**`TotalWorkItemAgeWidget.tsx` at 30.56% is not a coverage gap.** All 25 survivors are MUI
`sx={{…}}` object literals and style strings (`"100%"`, `"flex"`, `"column"`, `"center"`, `"h6"`,
`"bold"`). Every one of its 11 behavioural mutants was killed. Asserting on `sx` values to move this
number would be testing theater; the widget's only logic is `totalAge === null ? spinner : value`,
and that is pinned.

**`useMetricsData.ts` at 79.61% is 0.39pp short with nothing meaningful left.** After step 01-07 the
remaining survivors are exclusively: `console.error` message string literals (12), `.catch((error)
=> console.error(...))` arrow bodies (19), `useEffect` dependency arrays and `useState([])`
initial values mutated to `[]` (17, equivalent for single-render tests), and 13 no-coverage
`console.error` strings plus the `refetchThroughputPbc` catch block. Zero non-cosmetic survivors.
Closing the last 0.39pp would mean asserting on console-error text or dep-array identity.

The honest route to a higher number on that file is a **production** change — folding the repeated
`console.error` catch handlers into a shared helper, which shrinks the mutant population rather than
adding hollow assertions. Deliberately not done as part of a bugfix.

## What step 01-07 actually killed

- **Gate guards no test ever excluded**: `if (!needsX) return;` at `:277` (predictability), `:297`
  (throughput), `:435` (feature size). These survived because every prior test passed either the
  full default key set or a flow-overview set that happens to include them. One test with an
  **empty** key set, asserting every mock method is untouched, killed the cluster.
- **`isProjectMetricsService`'s five-way `&&` chain** (`:99`): mutating any `&&` to `||` lets a team
  service carrying one portfolio method masquerade as a portfolio service. Table-driven test, one
  case per missing probe method, plus an all-five control so it cannot pass by never fetching.
- **`isTeamMetricsService`'s guard** (`:623`) — the team-side twin, found by the crafter and not on
  the original target list. Without it, a service lacking `getFeaturesWorkedOnInfo` throws inside a
  `useEffect`, which blanks the whole dashboard rather than one widget.

## Reproducing (the repo has no committed Stryker config)

`@stryker-mutator/{core,vitest-runner}` are in `devDependencies` but there is **no** `stryker.conf`
anywhere, so each run re-derives the setup. The two files beside this README are that setup. Copy
both into `Lighthouse.Frontend/` and run:

```
TZ=Europe/Zurich pnpm exec stryker run stryker.bug5571.json
```

Three things cost a cycle to work out — keep them:

1. **`inPlace: true` is mandatory on TypeScript 7.** Stryker 9.6.1's `TSConfigPreprocessor` calls
   `ts.parseConfigFileTextToJson`, removed in TS 7 (`node_modules/typescript` is 7.0.2), so any
   sandboxed run dies with `TypeError: ts.parseConfigFileTextToJson is not a function`. `inPlace`
   skips that preprocessor entirely. It overwrites your working tree and restores from
   `.stryker-tmp-*/backup-*` on exit — including on SIGTERM, verified — but commit first.
2. **`plugins: ["@stryker-mutator/vitest-runner"]` must be declared explicitly.** pnpm's isolated
   `node_modules` defeats Stryker's plugin auto-discovery: `Cannot find TestRunner plugin "vitest".
   In fact, no TestRunner plugins were loaded.`
3. **Point it at a narrow vitest config.** Against the default config the run estimated **5 hours** —
   279 static mutants (40%) each paying a ~32s full-suite re-run, dominated by vitest's ~21s jsdom
   startup. Restricting `include` to the feature's own suites took the same run to **12 minutes**.
   Note the scope trade-off: excluding `BaseMetricsView.test.tsx` scored the feature at 78.61%;
   including it gave the honest 80.35%, because much of the gating behaviour is tested there.
