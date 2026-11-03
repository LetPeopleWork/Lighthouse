# Configuration candidates, ready to measure

Each patch was cut against `c98f49b05` and changes only `Lighthouse.Frontend` files. Nothing here has been timed.
The smoke checks were small, untimed runs at 3 workers (`TZ=Europe/Zurich`, time budget off).

## How to apply one

In the candidate worktree (`/storage/repos/Lighthouse-fe7-arms/candidate`), restore the baseline files and apply the patch:

```
git checkout -- . && git clean -fd Lighthouse.Frontend && git apply <this folder>/<patch>
```

Run it from the worktree root. The patches that add files (`a`, `d`) need the clean step, otherwise `git apply` refuses
because `setupTests.node.ts` already exists. Every patch except `d` stands alone on the baseline; `d` contains `a`.
To combine two (for example `a` with `b0`), apply them one after the other; they touch different lines of
`vitest.config.ts`, but check that `git apply` succeeds.

The candidate worktree was left with patch `a` applied (uncommitted).

## Current settings, for reference

`pool: "threads"`, `isolate: true`, `fileParallelism: true`, `maxWorkers` from `LIGHTHOUSE_TEST_WORKERS`,
`server.deps.inline: [/@mui\//, /react-transition-group/]`, `fsModuleCache` not set.

## The patches

| Patch | What it changes | Smoke result | Notes |
|---|---|---|---|
| `a-node-project.patch` | Splits the tests into two projects. DOM-free `.test.ts` files (125 files) run in a `node` environment with a trimmed setup file (`setupTests.node.ts`); everything else (457 files) stays in jsdom. Adds `setupTests.node.ts` and `src/tests/domTsTestFiles.ts` (the list of `.test.ts` files that stay in jsdom). | Mixed set: 7 files, 148 tests, pass. `--project node`: 125 files, 1,786 tests, pass. | Five node files print a harmless "Error starting SignalR connection" line on stderr (no `location` in node). |
| `b0-inline-off.patch` | Not in the original list, added because it turned out to work: sets `server.deps.inline` to an empty list, so MUI and `react-transition-group` are no longer processed by Vite. | Mixed set: 7 files, 148 tests, pass. Plus 26 files that mock MUI with `importActual` (including `DateRangeSelector` and `ManualForecaster`, which mock `@mui/x-date-pickers`): 517 tests, pass. | The old failure (a directory import inside MUI, rejected by Node's ESM resolver) did not appear on Vitest 5.0.5 in these 26 files. Only a full suite run can prove it is gone everywhere. |
| `b1-inline-narrow.patch` | Inlines only `@mui/x-*` and `react-transition-group`; `@mui/material`, `@mui/icons-material` and the rest stay external. | Mixed set: 7 files, 148 tests, pass. | The narrow version of the recorded reason. Since `b0` passes too, this is the fallback if `b0` shows a failure in the full run. |
| `b2-optimizer.patch` | Inlines nothing and instead pre-bundles `@mui/material`, `@mui/x-data-grid`, `@mui/x-date-pickers`, `@mui/x-charts` and `react-transition-group` through `deps.optimizer.web` (the option names are `deps.optimizer.{client,ssr,web}.{enabled,include}` in the Vitest 5.0.5 types). | Mixed set: 7 files, 148 tests, pass. | Not checked whether the optimizer is actually used for these tests (jsdom tests may take the `web` or `client` path); check that before trusting a time difference. |
| `b3-alias` (no patch) | Not written. | n/a | Not viable and not needed. An alias only changes imports that Vite processes; the failing import sits inside an external MUI file that Node resolves itself, so an alias cannot reach it. `react-transition-group` is also not a direct dependency (pnpm keeps it under `node_modules/.pnpm`), so there is no stable path to alias to. A quick try with the alias plus no inlining passed only because inlining is not needed at all (see `b0`). |
| `d-node-isolate-false.patch` | Patch `a` plus `isolate: false` in the `node` project only. | `--project node`: **23 files failed, 173 tests failed** (1,554 passed). The mixed set passed (148 tests). | Not viable as it stands. Files share module state without isolation: `TypeError: Invalid URL` (33 times) and `Cannot read properties of undefined (reading 'interceptors')` (24 times) come from the API service tests, which mock `axios` and read a base URL set up per file. Making it work would mean editing tests, which the rules for this spike rule out. It stays as a patch so the failure can be reproduced. |
| `e1-pool-forks.patch` | `pool: "forks"` instead of `"threads"`. | Mixed set: 7 files, 148 tests, pass. | Child processes instead of worker threads; expect more memory per worker. |
| `e2-pool-vmthreads.patch` | `pool: "vmThreads"` instead of `"threads"`. | Mixed set: 7 files, 148 tests, pass. | The memory-risk one (verdict item 3); watch peak memory. |
| `f` (no patch) | Worker count. | n/a | Needs no patch: run `ab.sh` with `WORKERS_B=12` (or another count). CI has no cap, so it uses Vitest's default of cores minus one; a gain at 12 workers on a 12-core machine may not carry to a 4-core runner. |
| `g-fs-module-cache.patch` | Adds `fsModuleCache: true`. | Mixed set: 7 files, 148 tests, pass. | The option exists in Vitest 5.0.5 (`fsModuleCache`, `fsModuleCachePath`). The type comment says the default is `true`, but the code reads `config.fsModuleCache ?? false`, so today it is off. It writes transformed modules to `node_modules/.vitest-cache`: a time comparison must say whether the cache was cold or warm, and the A/B arms would otherwise share nothing but the candidate would warm up over the first runs. |

## Combining

`a` with `b0`, `a` with `g`, and `a` with `e1`/`e2` are the likely combinations. They have not been smoke-tested together.
