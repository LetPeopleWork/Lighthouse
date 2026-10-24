# Mutation testing — 6398 frontend (a Jira Release loads once)

Run 2026-10-24 against `main` @ `a0787b6d0` (frontend code unchanged since), with StrykerJS 10.0.0 and the
vitest runner. The gate is an 80 % kill rate. The backend run is recorded in `results-backend.md`.

| run | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Full run (`--force`) | 85.68 % | 398 | 341 | 56 | 1 | 31 m 22 s |
| Incremental re-run after the kill round | **89.95 %** | 398 (44 reused) | 358 | 39 | 1 | 20 m 7 s |

Configs: `stryker.6398.frontend.json` and `vitest.stryker.6398.ts`, both copied into `Lighthouse.Frontend/` and
run from there with `TZ=Europe/Zurich`, `coverageAnalysis: "perTest"`, incremental mode and concurrency 2.

Four of the 39 survivors are false: applied by hand, each one fails the scoped suites (see below). Counting them as
killed, the score is 362 of 398, **90.95 %**. The recorded figure stays Stryker's own.

## What was mutated

Whole files, because the Story wrote or reshaped all of them: `premiumVerdict.ts`, `useDeliverySources.ts`,
`useDeliverySourceOptions.ts`, `useDialogDeliverySources.ts`, `useDeliveryFormQueries.ts`, `dialogQueryStatus.ts`,
`deliverySelectionBody.ts` and `RetryByReopeningNotice.tsx`.

Line ranges, one entry per range, from `git diff -U0 1f9043ea1 HEAD`: `DeliveryCreateModal.tsx` (32 ranges),
`DeliverySourceTab.tsx` (9), `FeatureGrid.tsx` and `FeatureSelector.tsx` (the `loading` prop and its pass-through,
two lines each) and `useDeliveryManagement.ts:492` (the list of sources now read from the page's query). Single
lines are written `N-N`, since StrykerJS drops a bare `file:N` without saying so.

Not mutated: `askedAfreshQueryOptions.ts`, a constant object of query options; the type-only `types.ts` files.

The vitest config runs the dialog's, the source tab's and the hooks' suites plus `FeatureGrid`, `FeatureSelector`
and `ArchivedFeatureGrid`. `deliveryGridServerState.enforcement.test.ts` is left out: it reads the source files off
disk, and in-place mutation puts Stryker's own scaffolding in them.

| file | score | killed | survived | no coverage |
| --- | --- | --- | --- | --- |
| `DeliveryCreateModal.tsx` (Story lines) | 90.83 % | 99 | 9 | 1 |
| `deliverySelectionBody.ts` | 93.33 % | 98 | 7 | 0 |
| `DeliverySourceTab.tsx` (Story lines) | 71.05 % | 27 | 11 | 0 |
| `dialogQueryStatus.ts` | 100.00 % | 21 | 0 | 0 |
| `useDeliveryFormQueries.ts` | 95.35 % | 41 | 2 | 0 |
| `useDialogDeliverySources.ts` | 90.63 % | 29 | 3 | 0 |
| `useDeliverySourceOptions.ts` | 74.07 % | 20 | 7 | 0 |
| `premiumVerdict.ts` | 100.00 % | 13 | 0 | 0 |
| `useDeliverySources.ts` | 100.00 % | 4 | 0 | 0 |
| `RetryByReopeningNotice.tsx` | 100.00 % | 2 | 0 | 0 |
| `useDeliveryManagement.ts` (Story line) | 100.00 % | 2 | 0 | 0 |
| `FeatureGrid.tsx` (Story lines) | 100.00 % | 1 | 0 | 0 |
| `FeatureSelector.tsx` (Story lines) | 100.00 % | 1 | 0 | 0 |

`DeliverySourceTab.tsx` and `useDeliverySourceOptions.ts` are under 80 % alone. Every survivor in them is
styling, equivalent or a false survivor, as listed below.

## Closed by this pass

- `deliverySelectionTabs.test.ts`, "what stands under the tabs" and the two blocks after it: the Manual list and the
  rule builder are content once their answers arrive; a source tab is a loading look while the licence has not
  answered; while a delivery that follows a tracker entry is edited, the body is a loading look while the sources
  are on their way and stops loading once they fail; the form stops waiting once the sources answer without the
  entry's source; a form already on its source tab keeps it when a later check fails; a loading body keeps Save
  off. Kills `deliverySelectionBody.ts:64`, `:81` (three mutants), `:101` (four), `:109` (three) and, by hand,
  `:44` and `:125`.
- `dialogQueryStatus.test.ts`, "what a body of the Delivery dialog is handed": an earlier answer is withheld while
  it is asked for again and after asking again failed. Kills `dialogQueryStatus.ts:38`.
- `DeliveryCreateModal.featureListAndRuleFields.test.tsx`: the dialog asks for exactly the Value Stream's own
  Deliverables, and the grid holds no row while its list is on its way. Kills `useDeliveryFormQueries.ts:28` and
  `DeliveryCreateModal.tsx:933`.
- `DeliveryCreateModal.neverAGuess.test.tsx`: a premium licence puts no premium tooltip on Rule-Based
  (`DeliveryCreateModal.tsx:405`, two mutants); editing a Launch that follows a Jira Release shows a loading look
  under the tabs while the sources load and nothing spinning once they fail; a connection that no longer offers
  Jira Releases leaves the form open once it has answered (kills `useDialogDeliverySources.ts:20` by hand).

## False survivors

Stryker reports these as survived; applying each by hand and running `vitest.stryker.6398.ts` fails the suites.
All but the last sit in module-level constants, which the vitest runner does not seem to re-evaluate per mutant.

- `useDialogDeliverySources.ts:20` (`success: ""`): 1 test fails.
- `deliverySelectionBody.ts:44` (`ready: {}`): 1 test fails. The `"content"` → `""` mutant on the same line meets
  the same `toEqual({ kind: "content" })`.
- `deliverySelectionBody.ts:125` (`"loading"` → `""` in the bodies that block saving): 1 test fails.
- `DeliverySourceTab.tsx:382` (whole condition → `false`): 42 tests fail.

## Accepted survivors

- **MUI `sx` objects and their values** (styling, no behaviour): `DeliveryCreateModal.tsx:416`, `:440`, `:441`,
  `:460`; `DeliverySourceTab.tsx:302-305` (four) and `:412` (three).
- **`DeliveryCreateModal.tsx:691` and `:696`** (the name and date checks in `validateForm`, plus the one
  no-coverage block): unreachable. Save is disabled whenever `getFirstBlockingError` finds a name or date
  error, so `validateForm` never meets one.
- **`DeliveryCreateModal.tsx:809`** (`setErrors({})` on close) and **`:810`** (its dependency list): equivalent.
  `validateForm` only runs when nothing blocks, so the errors are always empty when the dialog closes, and
  `startNewOpening` is a stable callback.
- **`DeliveryCreateModal.tsx:367`** (`Content === undefined`): every tab today is Manual, Rule-Based or a
  source tab, so the guard for a tab with no registered body cannot be reached.
- **`DeliverySourceTab.tsx:382`**, the other three (`&&`, `"loading"` → `""`, `options === undefined` → `false`):
  equivalent. `dialogAnswerOf` hands the options over only when the status is ready, so "loading" and "no options
  yet" are always true together.
- **`deliverySelectionBody.ts:15-18`** (the four body kinds → `""`): equivalent. Every reader compares against
  the constant, and one kind emptied stays distinct from the others.
- **`useDeliverySourceOptions.ts:27`, `:28` and `:60`** (four mutants on `!enabled || result === undefined`):
  unreachable. The early answer serves only a closed dialog or a licence not known to be premium, and in both
  cases no source tab body is drawn: the dialog is shut, or the body is the premium notice or the loading look.
  `result === undefined` cannot happen, because every key asked about comes from the offered sources. A hand
  probe of `:28` fails no test, as expected.
- **Query-key names** → `""` (`useDeliverySourceOptions.ts:51`, `useDeliveryFormQueries.ts:33` and `:88`):
  equivalent. Each key stays unique through its Portfolio, source and opening.
- **`useDialogDeliverySources.ts:75`** (`previous - 1`) and **`:76`** (the dependency list): equivalent. The
  opening number still changes on every opening, and the callback has no dependencies to miss.
