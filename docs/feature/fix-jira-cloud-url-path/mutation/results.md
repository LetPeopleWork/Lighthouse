# Mutation testing — 6401 (Jira Cloud URL with a /jira/ path breaks connection setup)

Run 2026-10-23 against `fix-jira-cloud-url-path` @ `a02a9544d` (base `main` @ `ead31f93f`). Gate is 80 % kill rate on both stacks.

| stack | score (whole files) | score (changed lines) | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | 69.33 % | **100 %** of changed controller lines, **94.4 %** of `JiraSiteAddress.cs` | 163 | 113 | 18 | 32 | 2 m 12 s |
| Frontend (StrykerJS), first run | 73.58 % | — | 53 | 39 | 14 | 0 | 1 m 55 s |
| Frontend, after the kill round | **83.02 %** | — | 53 | 44 | 9 | 0 | 1 m 58 s |

Configs: `stryker.6401.backend.json`, `stryker.6401.frontend.json`, `vitest.stryker.mutation.ts` (copied into
`Lighthouse.Frontend/` for the run; it resolves `setupTests.ts` relative to itself).

## Backend

Stryker.NET mutates whole files, so the two controllers bring in all of their untouched code.

| file | killed | survived | no coverage | note |
| --- | --- | --- | --- | --- |
| `JiraSiteAddress.cs` (new) | 17 | 1 | 0 | 94.4 % |
| `WorkTrackingSystemConnectionController.cs` | 55 | 10 | 26 | changed lines 190–210: every mutant killed |
| `WorkTrackingSystemConnectionsController.cs` | 41 | 7 | 6 | changed line 249 (`ForOption` call): its object initializer mutant killed |

### Accepted survivors

- `JiraSiteAddress.cs:60` `authorityEnd < 0` → `<= 0`: equivalent. The search starts after `scheme://`, so
  `IndexOfAny` can never answer 0.
- The 17 survivors and 32 no-coverage mutants in the two controllers all sit on lines this fix did not change
  (additional-field merging, field mappings, secret-option pruning at `:182`, the usage endpoint, etc.). They are
  pre-existing gaps, out of scope for a bugfix, and listed in the Stryker report rather than chased here.

### Not mutated

- `JiraWorkTrackingConnector.cs` (~3000 lines, ~60 changed). Mutating it whole would bury the change under
  untouched connector code and pull in the whole Jira test surface. The changed members (`SiteAddressOf`,
  `AsBaseAddress`, `ResolveCloudIdAsync`'s handler choice, `TenantInfoFrom`) are pinned by request-URI assertions in
  `JiraWorkTrackingConnectorAuthDelegationTest`: the Data Center context-path, Cloud `/jira/` path, OAuth tenant_info
  URL and non-JSON verdict tests all failed on the old code (RED evidence in the step 01-01 commit).

## Frontend

Mutated line ranges: `CreateConnectionWizard.tsx` 272–291 (`handleCreate`), 433–443 (`renderErrorAlert`), 566;
`OverviewDashboard.tsx` 51–52, 180–193 (the connection-list fetch), 450–462 (refused alert / stepper gate).

| file | killed | survived | score |
| --- | --- | --- | --- |
| `CreateConnectionWizard.tsx` | 21 | 8 | 72.41 % |
| `OverviewDashboard.tsx` | 23 | 1 | 95.83 % |

### Closed by this pass (commit `5a929c1d4`)

- `CreateConnectionWizard.tsx:276` `clearValidationFeedback()` removed: "clears the earlier save failure when Create
  is pressed again".
- `CreateConnectionWizard.tsx:281` `error.code !== 403` → `true`: "falls back to the general reason when saving is
  refused (403)".
- `OverviewDashboard.tsx:187` `if (true)`, `instanceof ApiError || code === 403`, `instanceof ApiError && true`:
  "does not call a failed connection list a refusal" (a 500 reaches the page's error state, not the refused alert).

### Accepted survivors

- Styling only, no observable behaviour: `CreateConnectionWizard.tsx:435` (`sx={{ width: "100%" }}` → `{}` and
  `width: ""`), `:438` (`sx` → `{}` and `display: ""`), `OverviewDashboard.tsx:455` (`sx={{ mt: 2 }}` → `{}`).
- `CreateConnectionWizard.tsx:437` `validationTechnicalDetails && (…)`: code moved unchanged from step 2 into the
  shared `renderErrorAlert`; the mutant only changes the wrapping element around text the tests still find.
- Untouched lines inside the mutated `handleCreate` range: `:274` `if (!dto) return` (the Create button is only
  reachable with a buildable DTO), `:275` `setSaving(true)` (the busy state is owned by `ActionButton`'s own
  pending handling), `:279` `onComplete?.()` → `onComplete()` (every caller passes `onComplete`).

The CreateConnectionWizard file score sits below 80 % only because of those styling and untouched-line mutants; every
mutant on a line this fix added is killed.
