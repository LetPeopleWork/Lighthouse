# Bug 6254 — mutation results (clients)

- Date: 2026-10-10
- Code: `lighthouse-clients` range `7fb022c..48f24b1` (mutated at `48f24b1`); the test-only commit from this
  pass is `08733b5`.
- Tool: StrykerJS `@stryker-mutator/core@10.0.0`, run through `pnpm dlx` (it is not a dependency of
  `lighthouse-clients`), `command` runner (`node_modules/.bin/vitest run --bail=1 --reporter=dot`,
  vitest 5.0.3), sandboxed (`inPlace: false`, `tsconfigFile` pointing at a file that does not exist so
  the TS preprocessor stays out of the way), `disableTypeChecks: false`, `coverageAnalysis: "off"`,
  concurrency 11, `TZ=America/Adak`.
- Configs in this folder: `stryker.6254.clients.json` (baseline) and `stryker.6254.clients.rerun.json`
  (the `packages/client/src/index.ts` ranges, re-run after the new test).
- Scope: the production lines the bug added or changed, from
  `git diff -U0 7fb022c..48f24b1 -- 'packages/*/src/*.ts' ':!*.test.ts'`. Import-only and type-only
  hunks are left out because they carry no mutants. 4 files, 140 mutants.
- Gate: 80 %. **PASS.**

Command, from the `lighthouse-clients` root:

```
TZ=America/Adak pnpm dlx --package=@stryker-mutator/core@10.0.0 stryker run <config>
```

## Score

| Stack | Run | Score | Tested | Killed | Survived | Timeout | Wall clock |
|---|---|---|---|---|---|---|---|
| clients | before (baseline, 4 files) | 97.86 % | 140 | 137 | 3 | 0 | 3 min 25 s |
| clients | after (client `index.ts` re-run) | 100 % on the re-run | 24 | 24 | 0 | 0 | 1 min 7 s |
| clients | after (whole scope, combined) | **99.29 %** | 140 | 139 | 1 | 0 | — |
| backend (Lighthouse C#) | N/A — no change in this bug | — | — | — | — | — | — |
| frontend (Lighthouse React) | N/A — no change in this bug | — | — | — | — | — | — |

## Per file

| File | Ranges | Mutants | Before | After |
|---|---|---|---|---|
| cli `deliveryOutput.ts` | 29-55, 66, 71-73 | 16 | 100 % | 100 % |
| client `deliveryWording.ts` | 43-46, 74, 93-171, 243-279 | 95 | 100 % | 100 % |
| client `index.ts` | 838-855, 3120-3121, 3126-3137 | 24 | 91.67 % (2 survived) | 100 % |
| mcp-core `index.ts` | 1242, 3074-3077 | 5 | 80.00 % (1 survived) | 80.00 % (1 equivalent) |

## Closed by this pass

- client `index.ts:3127` — `if (!result.ok) { return result; }` → `if (false) {…}` and the emptied
  block (2 mutants). Without the early return a failed request fell through to the shape check and was
  reported as "Lighthouse's list of Deliveries is not readable." instead of the server's own failure.
  Killed by the new case in `packages/client/src/runtime.test.ts`, *"listDeliveries keeps the
  server's own failure rather than calling its answer unreadable"*: the server answers 404 and the
  result must be `{ ok: false, error: { category: "misconfigured", statusCode: 404 } }`. Checked by hand
  first: with the mutant applied the case fails, on the frozen code it passes. Commit `08733b5`
  (with an empty changeset, `.changeset/bug-6254-mutation-tests.md`). No production code changed, no
  bug found.

## Accepted survivors

- mcp-core `index.ts:3076` — `readPortfolioDeliveries(value)?.active ?? null` →
  `readPortfolioDeliveries(value).active ?? null`. **Equivalent.** The reader runs inside
  `countSummary`, which wraps it in `summaryOrNull`'s `try { … } catch { return null; }`. When the
  answer cannot be read, the original yields `null` through `?.`/`??` and the mutant throws a
  `TypeError` that the catch turns into the same `null`. Both leave the tool answer without a
  `summary`, so no test can tell them apart.

## Not mutated

- Lighthouse backend (C#) and Lighthouse frontend: N/A, because bug 6254 changed neither. It touched
  only `lighthouse-clients`.
- `scripts/smoke-integration.sh` and `.github/workflows/ci.yml`: a shell script and CI configuration,
  not TypeScript that StrykerJS can mutate. Their behaviour is checked by the CI smoke itself.
- `test-support/lighthouseAnswers.ts` and the `*.test.ts` files: test code, not production code.
- `packages/client/src/index.ts:1624`, the import hunks in `deliveryOutput.ts` and mcp-core
  `index.ts`, and mcp-core `index.ts:760`: type and import lines with no mutable code.
