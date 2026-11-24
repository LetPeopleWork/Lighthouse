# Mutation testing — 6686 (Jira OAuth: write-back cannot write with read-only scopes)

Run 2026-11-24 on the worktree at `cb24eeab4` (candidate `d60d9f157` plus review and examination records).
Gate is 80 % kill rate on each stack touched.

| stack | score | mutants | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **80.56 %** (one more killed by hand after the run, see below: 81.94 %) | 72 | 58 | 11 | 3 | 3 m 52 s |
| Frontend (StrykerJS) | **100 %** | 2 | 2 | 0 | 0 | 1 m 53 s |

Configs: `stryker.6686.backend.json`, `stryker.6686.frontend.json`, `vitest.stryker.mutation.ts` (copied to
`Lighthouse.Frontend/vitest.stryker.6686.mutation.ts` for the run).

## Backend

Mutated whole files (Stryker.NET ignores line ranges): `JiraOAuthProvider.cs`, `OAuthCredential.cs`,
`WorkTrackingSystemConnectionDto.cs`. Tests: the provider, credential, connection-DTO, OAuth service and
write-back service unit tests; the acceptance suites stay out of the filter.

| file | killed | survived | no coverage |
| --- | --- | --- | --- |
| `Modules/TrackerIntegration/Adapters/OAuth/JiraOAuthProvider.cs` | 44 | 4 | 2 |
| `Models/OAuth/OAuthCredential.cs` | 7 | 2 | 0 |
| `API/DTO/WorkTrackingSystemConnectionDto.cs` | 7 | 5 | 1 |

### Closed by this pass

- `WorkTrackingSystemConnectionDto.cs:57`, `&& oAuthCredential is not null` mutated to `||`. No test covered an
  OAuth connection that has not signed in yet; under the mutant it throws. New row in
  `WorkTrackingSystemConnectionDtoTest`: "A Jira OAuth connection not signed in yet shows no rights and needs no
  write consent". Checked by applying the mutant by hand: the new row fails, the rest pass; reverted.

### Accepted survivors

- `JiraOAuthProvider.cs:134`, `string.Empty` → `"Stryker was here!"` in `(scope ?? string.Empty)`: equivalent. A
  missing scope becomes one entry that is not `write:jira-work`, so the answer stays Read either way.
- `JiraOAuthProvider.cs:123-124`: the log call and its message (granted-scope information log). Log text, not
  behaviour.
- `JiraOAuthProvider.cs:120-121` (no coverage) and `:140`: error-code strings of an empty token body and the
  default `unknown_error` code. Pre-existing lines this fix did not touch.
- `OAuthCredential.cs:9, 11`: default values of `AccessToken`/`RefreshToken`. Every stored credential sets both;
  pre-existing.
- `WorkTrackingSystemConnectionDto.cs:49, 72, 78, 80`: write-back mapping copy and string defaults, all
  pre-existing lines covered by the connection API and contract tests outside this filter.

### Not mutated

- `WriteBackService.cs`: a one-line call-site change (`LacksRightsFor(connection)`) in a large file; mutating the
  whole file would bury it. The skipped write-back on a read-only Jira sign-in is pinned by the
  `JiraOAuthWriteConsent` acceptance scenarios.

## Frontend

`WriteConsentAlert.tsx:14-15` (the warning copy built from the tracker's name): 2 mutants, both killed by
`WriteConsentAlert.trackerName.test.tsx`.
