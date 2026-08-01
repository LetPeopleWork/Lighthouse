# Mutation testing — 5628 (Team settings auto-save bypasses connector validation)

Run 2026-08-01 against `main` @ `da18633b1`. Gate is 80 % kill rate on every stack with changed files.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS) | **85.71 %** | 28 | 24 | 4 | 0 | 57 s |
| Backend (Stryker.NET) | N/A | — | — | — | — | — |

**Backend is N/A, not skipped.** Bug #5628 is a frontend control-flow defect: the auto-save path in
`useModifySettings` never called `validateSettings`. `POST /teams/validate` and
`POST /portfolios/validate` and every connector's `ValidateTeamSettings` /
`ValidatePortfolioSettings` were already correct and are untouched by this fix — zero backend files
changed, so there is nothing to mutate.

Configs: `stryker.5628.frontend.json`, `vitest.stryker.mutation.ts`. Report:
`stryker-5628-frontend.step-01-03.json`.

Scope mutated — the changed lines of `src/hooks/useModifySettings.ts` only: `101-112`
(`connectorFingerprint` + the warning message), `174-207` (`maybeValidateAfterSave`), `251` (the call
site in `dispatchSave`'s success handler). StrykerJS supports line ranges, so the untouched 400-odd
lines of the hook do not dilute the score.

## Two runs

| run | step | score | killed | survived | no coverage |
| --- | --- | --- | --- | --- | --- |
| first | after 01-02 | 67.86 % | 19 | 7 | 2 |
| second | after 01-03 | **85.71 %** | 24 | 4 | 0 |

The first run is why step 01-03 exists. Four of its seven survivors were genuine test gaps, and the
two no-coverage mutants marked a branch no test entered at all.

### Closed by this pass

| mutant | what survived | the case that now pins it |
| --- | --- | --- |
| `:103` ObjectLiteral | `JSON.stringify({...})` → `JSON.stringify({})` | a second settled save with a **changed** `dataRetrievalValue` must probe again — under the mutant the fingerprint is constant, so it never does |
| `:102` BlockStatement | whole `connectorFingerprint` body → `{}` | same pair, plus its `workItemTypes` sibling driven through `workItemTypeHandlers.onAdd`, which pins the field-level content rather than just the function's existence |
| `:179` ConditionalExpression | `if (pendingPayloadRef.current) return;` → `if (false)` | a payload queued while the first save is in flight yields exactly one probe, **asserted on the argument** (`final-query`). A count-only assertion passes at the moment of the superseded probe and leaves the mutant alive |
| `:194` ConditionalExpression + its block | `if (!(error instanceof ApiError))` → `if (false)` | a plain `new Error("boom")` rejection must leave `validationError` and `validationTechnicalDetails` null; under the mutant they become `"boom"` / `undefined` |

The two NoCoverage mutants (`:194` block, `:195` string) disappeared with the same case — the
non-`ApiError` branch is now entered, which is why `:195` reports as a covered survivor below rather
than as uncovered.

Why the first run scored so well on paper and so badly in truth: every case in the 01-01 suite probed
**at most once**. A constant fingerprint still lets the first save probe and stops every later one —
which is precisely what cases (d) "one probe per settled save" and (e) "no re-probe when nothing
connector-relevant changed" assert. The suite could not tell a working fingerprint from a broken one
until a case demanded a *second* probe.

## Accepted survivors

| line | mutant | why it stays |
| --- | --- | --- |
| `:189` | `if (!isLatest()) return;` → `if (false)` (success branch) | Killing it needs a superseded request whose validation resolves *after* a newer one has already written its verdict. The fake-timer harness can only stage that by reaching into the hook's request-sequence internals, which would pin the implementation rather than the behaviour. The guard is cheap insurance against a race the tests cannot honestly stage. |
| `:198` | `if (!isLatest()) return;` → `if (false)` (error branch) | Same race, same reasoning. |
| `:195` | `console.error("Error validating settings after save", error)` → `console.error("", error)` | Log-message string mutation — an acceptable survivor by the project's mutation-testing rules. Asserting on log text pins prose, not behaviour. |
| `:205` | `[modifyDefaultSettings]` → `[]` | Equivalent mutant. `modifyDefaultSettings` is a prop fixed for the lifetime of a settings page (`ModifyTeamSettings.tsx:142`, `ModifyProjectSettings.tsx:160` pass a constant), so the callback identity never needs to change and an empty dep list is behaviourally identical. |

## Not mutated

Nothing was excluded. The `mutate` ranges cover every line the fix added or changed. The rest of
`useModifySettings.ts` predates this bug and is covered by `useModifySettings.test.ts` (54 cases),
`.autosave.test.ts` (24) and `.conflict.test.ts` (7).

## Reproducing

From `Lighthouse.Frontend/`, with `vitest.stryker.mutation.ts` copied from this directory into the
frontend root (it is gitignored there):

```bash
pnpm exec stryker run ../docs/feature/bug-5628-settings-autosave-validation/mutation/stryker.5628.frontend.json
```

`inPlace` is `true` — check `git status` afterwards and revert any stray mutants before committing.
