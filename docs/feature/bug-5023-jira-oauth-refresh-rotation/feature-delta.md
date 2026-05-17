# Feature Delta — bug-5023-jira-oauth-refresh-rotation

**ADO Bug**: #5023 — "OAuth Connection for Jira break after some time"
**Parent feature**: work-tracking-oauth-authentication (slice-02-token-refresh)
**Severity**: 3 — Medium (Atlassian rotates refresh tokens; reuse of stale token kills the connection)

> Bug workflow: DISCUSS / DESIGN / DEVOPS are skipped — there is no new product capability, no new architecture, and no new infrastructure. The regression test in this delta defines the contract; DELIVER will RCA + fix.

## Wave: DISTILL

### [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| n/a | Bug reproduces against `OAuthService.EnsureFreshTokenAsync` when the Jira OAuth credential's refresh token has been rotated by Atlassian on a prior successful refresh. | n/a | The regression test asserts the rotation chain is honoured across consecutive refreshes — each provider call must receive the most recently persisted `refresh_token`, never a stale one — so DELIVER cannot ship a fix that silently relies on a single refresh working in isolation. |
| n/a | The Atlassian-rotated `refresh_token` returned from `JiraOAuthProvider.RefreshTokenAsync` MUST be encrypted, persisted, and re-read on the next refresh before being sent back to Atlassian. | n/a | Pins the persistence + re-read path against a real EF Core `LighthouseAppContext` (InMemory), so a mock-only test cannot give false confidence. If the bug is EF entity-tracking related, this test will be RED until the fix lands. |
| n/a | After N consecutive token refreshes within a single connection lifetime, the credential status remains `Valid` and the connection does NOT degrade to `RefreshFailed` for any reason other than a genuine provider rejection. | n/a | Defines "fixed" as a chain — not a single hop — and forces the DELIVER step to think about idempotency, save ordering, and any background-service holding stale `OAuthCredential` references. |

### [REF] Scenario list with tags

| Scenario (regression) | Tags |
|---|---|
| Jira OAuth refresh chain: 5 consecutive rotations all succeed and persist each new refresh_token | `@regression @bug-5023 @oauth @jira @real-io` |
| Jira OAuth refresh chain: provider call N+1 receives the refresh_token from provider response N (rotation honoured) | `@regression @bug-5023 @oauth @jira @real-io` |
| Jira OAuth refresh chain: credential status stays Valid across N rotations | `@regression @bug-5023 @oauth @jira @real-io` |

All three scenarios are exercised by ONE NUnit test that loops the rotation N times and asserts every intermediate state — see test placement below.

### [REF] WS strategy

**n/a — bug fix.** Walking skeleton is optional for bugs (per skill: "Features only; optional for bugs"). The OAuth walking skeleton was established in `work-tracking-oauth-authentication` slice 01. This bug fix consumes that skeleton; it does not create a new one.

### [REF] Adapter coverage table

| Adapter | @real-io scenario | Covered by |
|---------|-------------------|------------|
| `OAuthCredentialRepository` (EF Core / `LighthouseAppContext`) | YES | Regression test uses `UseInMemoryDatabase` + real `OAuthCredentialRepository` (no `IRepository<OAuthCredential>` mock). Catches stale-entity / change-tracking bugs that mock-based tests miss. |
| `WorkTrackingSystemConnectionRepository` (EF Core) | YES | Regression test uses real repo against the same context. |
| `IOAuthProvider` (`JiraOAuthProvider` HTTP to `auth.atlassian.com`) | NO — substituted with `Mock<IOAuthProvider>` | The provider's real HTTP call is unit-tested in `JiraOAuthProviderTest`. The bug surfaces in the `OAuthService` ↔ repository ↔ provider hand-off, not in the HTTP serialisation itself. Mock returns rotating tokens (`rt-1 → rt-2 → rt-3 …`) so the regression test can verify each call sent the previously-rotated token. |
| `ICryptoService` | NO — substituted with `Mock<ICryptoService>` | Identity-mapped (`Decrypt(x) → x`, `Encrypt(x) → x`) so encryption is not the variable under test; bug lives upstream. |

### [REF] Scaffolds

**None.** No new production modules — the fix will modify `OAuthService.PerformRefreshAsync` and/or its call sites. Mandate 7 scaffolding does not apply because every type the regression test imports (`OAuthService`, `OAuthCredential`, `OAuthCredentialRepository`, `IOAuthProvider`) already exists in production with full implementations.

### [REF] Test placement

| Artifact | Path | Rationale |
|---|---|---|
| Regression NUnit test | `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/OAuth/Bug5023JiraRefreshRotationTest.cs` | Co-located with `OAuthCredentialConcurrentLoadTest.cs` (the precedent for OAuth regression tests against a real EF `LighthouseAppContext`). Bug-id in the class name preserves traceability without a separate `Regression/` folder (the C# project has no such convention). |
| Gherkin documentary | `docs/feature/bug-5023-jira-oauth-refresh-rotation/regression/bug-5023.feature` | Human-readable scenario contract. Not executable — Lighthouse's backend test stack is NUnit (no pytest-bdd / SpecFlow wired up). Lives in the bug workspace alongside this feature-delta. |

### [REF] Driving Adapter coverage

| Driving entry point | Regression coverage |
|---|---|
| `OAuthService.EnsureFreshTokenAsync(connectionId, ct)` — the public application-service method that every `IWorkTrackingConnector` hits via `OAuthBearerAuthStrategy.ApplyAsync` | YES — regression test calls it directly across N rotation cycles. |
| `JiraOAuthProvider.RefreshTokenAsync` (HTTP driving port to Atlassian) | NOT directly — covered indirectly by `Mock<IOAuthProvider>.Setup(RefreshTokenAsync)`. Real HTTP path is unit-tested separately in `JiraOAuthProviderTest`. |
| Background service entry (`TeamUpdater.Update` → `JiraWorkTrackingConnector.GetWorkItemsForTeam`) — the path in the bug stack trace | NO direct subprocess test (would require WebApplicationFactory + fake Atlassian). DEFERRED — if the regression test passes but the bug still reproduces in DELIVER's manual dogfood, escalate to an integration test then. |

### [REF] Pre-requisites

- `OAuthService` continues to expose `EnsureFreshTokenAsync(int connectionId, CancellationToken)` with current signature (unchanged contract from slice-02).
- `IOAuthProvider.RefreshTokenAsync(OAuthRefreshContext, CancellationToken)` returns `OAuthTokens(AccessToken, RefreshToken, ExpiresAt)` — the `RefreshToken` field carries the Atlassian-rotated token verbatim.
- `OAuthCredential.RefreshToken` is persisted as the encrypted, rotated value on each refresh (per `OAuthService.PerformRefreshAsync` lines 257-262).
- `LighthouseAppContext` in-memory provider supports `OAuthCredential` change tracking (verified by `OAuthCredentialConcurrentLoadTest`).

### [REF] Definition of fixed

The bug is considered fixed when:

1. The regression test (`Bug5023JiraRefreshRotationTest`) is GREEN against a fresh build.
2. The dogfood scenario from the bug report — "create Jira OAuth connection, wait a few hours, trigger Update All" — succeeds without any `oauth.token.refresh_failed` log event for any reason other than a genuine Atlassian rejection (e.g., user revoked the app).
3. `OAuthCredentialStatus` remains `Valid` for the full Atlassian refresh-token lifetime (90 days idle / 1 year absolute, per Atlassian default), not "a few hours".

Item 1 is the hard gate. Items 2 and 3 are validated by the DELIVER finalize step against a real Jira sandbox before the bug is closed.

### [REF] RCA outcome (resolved 2026-05-17)

`/nw-bugfix` Phase-1 RCA identified the root cause as **double-encryption of the rotated tokens on refresh save**. `OAuthService.PerformRefreshAsync` pre-encrypted `AccessToken`/`RefreshToken` in service code and then `LighthouseAppContext.EncryptSecrets` (`LighthouseAppContext.cs:352-365`) encrypted the same fields again on `SaveChangesAsync` because they were flagged `IsModified`. The stored value was therefore `Encrypt(Encrypt(plaintext))`. On the next refresh, one `Decrypt` peeled off only the outer layer, and the still-encrypted inner ciphertext was sent to Atlassian as the supposed `refresh_token` — Atlassian responded with HTTP 403 `unauthorized_client - refresh_token is invalid`. Manual reconnect worked because `CompleteAsync` → `UpsertValidCredential` correctly assigns plaintext (`OAuthService.cs:128-129`) and lets `EncryptSecrets` encrypt exactly once.

The asymmetry between the connect path (plaintext-in, DbContext-encrypts) and the refresh path (service-encrypts, DbContext-encrypts-again) was invisible to existing tests because every OAuth test stubbed `ICryptoService` as identity (`v => v`) — double-identity-encryption is still identity. `CryptoService.Decrypt`'s silent fallback (`CryptoService.cs:79-83`) also returned the ciphertext on failure, suppressing any backend stack trace that would have flagged the corruption.

**Fix**: a 2-line change at `OAuthService.cs:257-258` — remove the in-service `Encrypt` calls and assign plaintext to the credential, matching the `CompleteAsync` contract. The DbContext continues to own the encryption boundary.

**New test** (`EnsureFreshTokenAsync_FiveConsecutiveRefreshes_WithRoundTripCryptoBoundary_ProviderReceivesPlaintextEveryCycle`): exercises the REAL `EncryptSecrets` path against a non-identity round-trip crypto stub. Confirmed RED before the fix (cycle 2 sent `ENC(plain-rt-1)` to the provider) and GREEN after.

**Suggested follow-ups (not in this fix, separate issues)**:
- Make `CryptoService.Decrypt` raise on cryptographic failure instead of returning the ciphertext — the silent-fallback path is what hid this bug from production logs.
- Replace the identity-crypto mock idiom across OAuth tests with the round-trip stub introduced in `Bug5023JiraRefreshRotationTest`, so future encryption-layer regressions surface immediately.
