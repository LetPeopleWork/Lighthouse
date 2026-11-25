# Mutation testing — 6698 (pasting a state adds a different, earlier suggested state)

Run 2026-11-25 against `main` @ `46d592c8d`. Gate is 80 % kill rate on each stack touched.

| stack | scope | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 10.1.1) | lines the fix changed (160–166, 192–197) | **100 %** | 7 | 7 | 0 | 0 | 21 s |
| Frontend (StrykerJS 10.1.1) | everything StrykerJS mutated | 56.25 % | 32 | 18 | 5 | 9 | 21 s |
| Backend | — | N/A | — | — | — | — | — |

Backend: N/A, the fix changed no backend file.

Configs: `Lighthouse.Frontend/stryker.6698.frontend.json`, `Lighthouse.Frontend/vitest.mutation.6698.config.ts`
(only `ItemListManager.test.tsx` and the oracle `ItemListManager.pastedEntry.test.tsx` run). Raw report:
`stryker-6698-frontend.json`.

## Why two scores

`mutate` names only the changed lines, but StrykerJS also mutated the rest of the `Autocomplete` element those lines
sit in (lines 159–199), code this fix did not touch. The gate is judged on the changed lines: every mutant there was
killed, by the oracle's Enter scenarios (stale highlight after a text change, after the list closes, and after the
offered suggestions change) and by the existing casing test.

## Survivors and uncovered mutants (all in code the fix did not change)

- **No coverage, lines 168–171, `onChange` (clicking a suggestion adds it): 9 mutants.** No test clicks a suggestion
  in the list. This is a real test gap from before this fix. Follow-up: a test in `ItemListManager.test.tsx` that
  clicks an option and expects it added, and one that the field is empty afterwards.
- **Survived, line 177 `disableCloseOnSelect={false}` and line 178 `blurOnSelect={true}`.** No test checks that the
  list closes and the field loses focus after a pick. Same follow-up.
- **Survived, line 189 `e.preventDefault()` on Enter.** Nothing in a jsdom test observes it (no form submit around
  the field). Accepted.
- **Survived, line 199 `inputValue.trim()` → `inputValue` / `true`.** `addItem` trims and ignores a blank value
  itself, so this guard is equivalent: no observable change. Accepted as equivalent.
