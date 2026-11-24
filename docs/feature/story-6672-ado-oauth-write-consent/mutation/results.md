# Mutation testing — 6672 (Azure DevOps OAuth asks only for the rights write-back needs)

Run 2026-11-24 against `main` @ `affac2f41` (feature base `f5cbef519`). Gate is 80 % kill rate on each stack,
scoped to the feature's changed code.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.1.0), changed lines, after kill round | **93.55 %** | 31 | 29 | 2 | 0 | 6 m 26 s + 4 m 32 s |
| Backend (Stryker.NET 5.1.0), whole files, first run | 74.92 % | 295 | 220 | 30 (+44 no coverage) | 1 | 6 m 26 s |
| Frontend (StrykerJS 10.1.1), first run | 63.72 % | 113 | 72 | 35 (+6 no coverage) | 0 | 4 m 36 s |
| Frontend (StrykerJS 10.1.1), after the kill round | **87.61 %** | 113 | 99 | 14 | 0 | 6 m 46 s (full `--force` run) |

Configs: `stryker.6672.backend.json` (first run, all changed files), `stryker.6672.backend.killround.json`
(kill round, `AdoOAuthProvider.cs` only).
Frontend: `stryker.6672.frontend.json` and `vitest.stryker.mutation.ts` (the JSON report is not committed; copied to
`Lighthouse.Frontend/vitest.stryker.6672.mutation.ts` for the run; that name is gitignored).

## Backend

Stryker.NET mutates whole files, so the gate is computed on the mutants whose span touches a line added or
changed by `git diff -U0 f5cbef519 HEAD -- <file>`. The whole-file score is recorded beside it; its
survivors outside the change are listed below, not killed.

The test-case filter covers the unit tests of `OAuthService`, `AdoOAuthProvider`, `JiraOAuthProvider`,
`OAuthProviderRegistry`, `WriteBackService`, `WriteBackSuppressionWarning`, `WorkTrackingSystemConnectionDto`,
`OAuthCredential` and `WorkTrackingSystemConnection`, and leaves out `API.Integration`, `Dogfood` and
`Contracts`. It **includes the acceptance suite `Lighthouse.Backend.Tests.Acceptance.AdoOAuthWriteConsent`**:
it runs at the application-service port and starts no web host, so it is fast, and it is the only suite that
pins the consent rules end to end (which rights a sign-in asks for, what Microsoft's granted scope means, and
write-back being skipped without write consent). 129 tests ran; "251 total mutants will be tested" on the
first run, which matches the eleven changed files.

### Per file

| file | whole file tested / killed / survived / no coverage | changed lines tested / killed / survived |
| --- | --- | --- |
| `API/DTO/WorkTrackingSystemConnectionDto.cs` | 13 / 8 / 4 / 1 | 5 / 5 / 0 |
| `Models/OAuth/OAuthCredential.cs` | 6 / 4 / 2 / 0 | 4 / 4 / 0 |
| `Models/OAuth/WorkItemRights.cs` | 0 (enum, nothing to mutate) | 0 |
| `Models/WorkTrackingSystemConnection.cs` | 26 / 14 / 2 / 10 | 3 / 3 / 0 |
| `Modules/TrackerIntegration/Adapters/OAuth/AdoOAuthProvider.cs` (kill round) | 57 / 51 / 4 / 2 | 13 / 12 / 1 |
| `Modules/TrackerIntegration/Adapters/OAuth/JiraOAuthProvider.cs` | 43 / 38 / 3 / 2 | 0 (rename only) |
| `Modules/TrackerIntegration/Adapters/OAuth/StubOAuthProvider.cs` | 21 / 0 / 0 / 21 | 0 (rename only) |
| `Modules/TrackerIntegration/Application/OAuth/IOAuthProvider.cs` | 0 (interface) | 0 |
| `Modules/TrackerIntegration/Application/OAuth/OAuthService.cs` | 74 / 54 (+1 timeout) / 12 / 7 | 2 / 1 / 1 |
| `Modules/TrackerIntegration/Application/OAuth/OAuthTokens.cs` | 0 (record) | 0 |
| `Services/Implementation/WriteBackService.cs` | 55 / 52 / 2 / 1 | 4 / 4 / 0 |

First run on changed lines: 28 / 31 = 90.32 %. After the kill round: 29 / 31 = 93.55 %.

### Closed by this pass

- `AdoOAuthProvider.cs`, the `"/"` in `entry.EndsWith("/" + WorkItemsReadAndWriteScope)` replaced by `""`.
  New test `AdoOAuthProviderTest.ExchangeCodeAsync_ReadsTheGrantedRightsFromTheScopeMicrosoftReturns`
  (commit `00760922f`), five rows at the provider: write rights named with and without their resource grant
  reading and writing; read rights alone, a scope that only ends like write rights without the resource
  separator, and a token answer without any scope grant reading only. The scope parsing had been reached only
  through the port scenarios; the new row "A scope that only ends like write rights grants reading only" was
  seen red with the separator removed from the production line, and green with it restored.

### Accepted survivors (changed lines)

- `AdoOAuthProvider.cs` L154, `string.Empty` in `(scope ?? string.Empty)` replaced by `"Stryker was here!"`.
  Equivalent: for a missing scope the mutant splits that text into three words, none of which is a write
  scope, so the answer is still "read only", exactly as for the empty string.
- `OAuthService.cs` L111, removal of the `oauth.flow.completed` log statement. The changed part is only the
  scopes it logs (`flowContext.Scopes` instead of the provider's defaults). Log output is not behaviour the
  tests pin; the scopes actually sent to Microsoft are pinned by the provider and port tests.

### Survivors outside the change (listed, not killed)

- `WorkTrackingSystemConnectionDto.cs`: L48 removal of `WriteBackMappingDefinitions.AddRange`; string
  defaults at L70, L76, L78; no coverage L113.
- `OAuthCredential.cs`: `string.Empty` defaults of `AccessToken` and `RefreshToken` (L9, L11).
- `WorkTrackingSystemConnection.cs`: L122 `<` / `<=` boundary, L130 message text; no coverage at L65-70,
  L118, L122, L155 (legacy parent-field mapping and option lookups the filtered tests do not reach).
- `AdoOAuthProvider.cs`: log statement and its text (L142-143), error-code default text (L163); no coverage
  on the empty-body exception's texts (L139-140).
- `JiraOAuthProvider.cs`: the matching log and error-text survivors (L115-128).
- `StubOAuthProvider.cs`: all 21 mutants without coverage; it is the development stand-in provider, and its
  tests are not in this filter. The change here is a rename of `DefaultScopes` to `ScopesFor`.
- `OAuthService.cs`: log statements and texts (L89-90, L155-160, L171-172, L189-190), exception message texts
  (L216, L222), the refresh-window boundary `>` / `>=` (L230), the catch filter's `and` / `or` (L257), the
  callback-host guard (L359-363).
- `WriteBackService.cs`: `stopwatch.Stop()` removal (L39), ordering of suppression warnings (L94), one log
  text without coverage (L169).

### Not mutated

- The EF migrations of the change (SQLite and Postgres `AddOAuthCredentialGrantedRights` plus the model
  snapshots): generated code.

## Frontend

Run from `Lighthouse.Frontend/`:

```bash
cp ../docs/feature/story-6672-ado-oauth-write-consent/mutation/vitest.stryker.mutation.ts vitest.stryker.6672.mutation.ts
pnpm exec stryker run ../docs/feature/story-6672-ado-oauth-write-consent/mutation/stryker.6672.frontend.json --incremental
```

`mutate` takes the changed lines of `git diff -U0 f5cbef519 HEAD` for the six changed files, and
`SignInAlert.tsx`, `WriteConsentAlert.tsx` and `useOAuthSignIn.ts` whole (new files). "Instrumented 9 source
file(s) with 113 mutant(s)", which matches. `coverageAnalysis` is `perTest` with an incremental file.

The vitest config narrows `include` to the specs that cover the mutated code: `WriteConsentAlert.test.tsx`,
`ReconnectBanner.test.tsx`, the four `ConnectionsSettings*.test.tsx`, the three
`ModifyConnectionSettings*.test.tsx` that existed before, and the three spec files the kill round added. No spec
of `connectionFormRules.ts` or `ConnectionAuthSection.tsx` exists; the rights line is pinned through
`WriteConsentAlert.test.tsx`. The config also copies `onUnhandledError` from `vitest.config.ts`: without it, the
`ExpectedTestFailure` that a `ModifyConnectionSettings.test.tsx` test raises on purpose crashes Stryker's dry run
("Cannot convert object to primitive value" in the vitest runner).

Incremental-mode trap: the first incremental re-run after the new tests kept the old "Survived" for the copy
constants of `WriteConsentAlert.tsx` and `ReconnectBanner.tsx`. Module-level constants are static mutants with
no per-test coverage, so the incremental differ sees no changed test for them. The recorded score is a full
`--incremental --force` run; a later plain incremental run reproduced it.

### Per file

| file | first run tested / killed / survived / no coverage | after the kill round tested / killed / survived |
| --- | --- | --- |
| `Connection/ConnectionAuthSection.tsx` (changed lines) | 6 / 5 / 1 / 0 | 6 / 5 / 1 |
| `Connection/connectionFormRules.ts` (changed lines) | 5 / 5 / 0 / 0 | 5 / 5 / 0 |
| `Connection/ModifyConnectionSettings.tsx` (changed lines) | 32 / 9 / 21 / 2 | 32 / 21 / 11 |
| `Connections/ReconnectBanner.tsx` (changed lines) | 6 / 5 / 1 / 0 | 6 / 6 / 0 |
| `Connections/SignInAlert.tsx` (new) | 6 / 6 / 0 / 0 | 6 / 6 / 0 |
| `Connections/WriteConsentAlert.tsx` (new) | 17 / 12 / 5 / 0 | 17 / 17 / 0 |
| `hooks/useOAuthSignIn.ts` (new) | 31 / 21 / 6 / 4 | 31 / 30 / 1 |
| `models/WorkTracking/WorkTrackingSystemConnection.ts` (changed lines) | 0 (types and plain assignments) | 0 |
| `pages/Settings/Connections/ConnectionsSettings.tsx` (changed lines) | 10 / 9 / 1 / 0 | 10 / 9 / 1 |

### Closed by this pass

Commit `66e954003`, `test(oauth): pin the sign-in outcomes and the rights after a reload`.

- `useOAuthSignIn.test.ts` (new, the hook with no page): each popup outcome gives its exact severity and text
  (blocked: error; cancelled: info; failed with a reason: error with the reason; failed without one: error,
  "OAuth failed."), a sign-in that cannot start says the start-failed copy and opens no popup, a success with
  no `onSignedIn` leaves no message (kills `onSignedIn?.()` to `onSignedIn()`), and a new sign-in drops the
  previous message while it runs. Kills the severity literals, the fallback text, the whole `catch` block and the
  clear at the start.
- `WriteConsentAlert.outcomes.test.tsx` (new, the alert rendered alone): nothing shows when the sign-in may
  already write or the connection has no id (kills the guard forced to `false`); a blocked popup, a cancelled
  popup and a failed start each show the "Sign in again" wording (kills the copy object and its three
  literals).
- `ReconnectBanner.test.tsx`: a sign-in that cannot start shows "Failed to start reconnect. Please try
  again." and opens no popup. The banner's IdP-reason test moved down to the hook test, and the test named
  after a story acceptance criterion was renamed in plain words.
- `ModifyConnectionSettings.signInRights.test.tsx` (new): after signing in again the page shows the rights the
  reloaded connection has and no warning (kills `refreshed === null` forced `true` and `!==`); when the
  reload fails or finds no connection, the rights and the warning go but the loaded connection stays: its
  name and its Authentication Method choice are still there (kills `forgetSignInRights` dropping the whole
  connection: the arrow to `undefined`, `current === null` forced `true` and `!==`, the spread to `{}`, and
  the null branch removed or forced `false`); picking another authentication method hides the saved
  sign-in's rights (kills `isEditMode && key === original` forced `true` or turned into `||`).

Each kill was seen by the Stryker re-run itself (the mutants went from Survived or NoCoverage to Killed).

### Accepted survivors

- `ModifyConnectionSettings.tsx` L278, `current === null` forced `false`. Equivalent in practice: the
  write-consent sign-in that leads here is only shown for a loaded connection, so `current` is never `null`
  when the rights are forgotten.
- `ModifyConnectionSettings.tsx` L286 and L302, the `useCallback` dependency arrays. L286 has no
  dependencies to change; L302 emptied keeps the first `getConnectionSettings`, which `EditConnection`
  creates with `useCallback`, so it is the same function.
- `ModifyConnectionSettings.tsx` L299, the `console.error` text: log output.
- `ModifyConnectionSettings.tsx` L440 and L441, optional chaining removed on `selectedAuthMethod` and
  `selectedWorkTrackingSystem`. Defensive: `isEditMode` is set together with the loaded connection and its
  method, and the form renders behind the loading guard, so neither is `null` while `isEditMode` is true.
- `ModifyConnectionSettings.tsx` L611 and L612, the outer guard around `WriteConsentAlert` forced `true` or
  turned into `||`, and L612's optional chaining. Equivalent: `WriteConsentAlert` returns nothing itself unless
  `needsWriteConsent` is true and the connection has an id, so the mutants only leave an empty grid cell; the
  optional chaining is defensive for the reason above. L613, the grid cell's `size` emptied: layout only.
- `ConnectionAuthSection.tsx` L66, the rights line's `sx={{ mt: 1 }}` emptied: spacing only.
- `ConnectionsSettings.tsx` L216, the list filter's `requiresReconnect === true` forced `true`. Equivalent:
  `ReconnectBanner` and `WriteConsentAlert` each return nothing for a connection that needs neither, so the
  mutant only adds an empty box per connection.
- `useOAuthSignIn.ts` L58, `setInlineMessage(null)` on success removed. Equivalent: the sign-in already cleared
  the message when it started, and nothing sets one before a success.
