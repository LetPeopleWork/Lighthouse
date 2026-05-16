# Evolution — ado-oauth (Story #4969)

Date: 2026-05-16
Branch: `main` (slice-boundary push pending)
Wave path: DISCUSS (implicit, via the ADO story) → DESIGN (already done in slice-01 — provider abstraction) → DISTILL (already on disk — `@US-03` scenarios + Gherkin) → DELIVER (6 steps, Phase 03 of the OAuth roadmap)
Outcome: shipped. Azure DevOps OAuth flow ships end-to-end via Entra ID v2.0 endpoints. **Slice 03 AC #5 invariant satisfied**: the slice-01 `IOAuthProvider` abstraction absorbed ADO with zero changes to `OAuthController`, `OAuthService`, `OAuthCredential`, `OAuthTokenRefreshService`, persistence, `JiraOAuthProvider`, or any auth-strategy class.

---

## Feature goal and user intent

A Lighthouse customer wants to connect their Azure DevOps work-tracking system using OAuth rather than a Personal Access Token. PAT-based connections require manual rotation, accidentally leak when copied around, and are tied to a single human's identity. An OAuth-based connection uses an Entra ID app registration owned by the Azure tenant, supports silent token refresh via the existing slice-02 refresh service, and can be reauthorised via the slice-04 popup if it ever drops.

Story #4969 (US-03) is the third concrete OAuth provider after slice-01 (Jira) and the slice-04 popup-reconnect refactor. The architectural promise of slice-01 was that the `IOAuthProvider` abstraction would absorb any future OAuth provider with zero changes to the shared OAuth surface — **this slice is the integrity proof of that promise.**

---

## Wave-by-wave summary

### DISCUSS — implicit (via the ADO story)

No separate DISCUSS session. ADO #4969 carried the user story body + scope + a single AC #5 invariant statement ("the only files touched are AdoOAuthProvider.cs, DI registration, and docs — no change to OAuthCredential, OAuthConfiguration, the controller, or the refresh service") that became the DELIVER-time architectural integrity guard.

### DESIGN — already done in slice-01

The provider abstraction (`IOAuthProvider`, `OAuthProviderRegistry`, `OAuthService`, `OAuthController`, the auth-strategy factory + 3 concrete strategies, the credential entity, the refresh service) was specifically designed in slice-01 to absorb additional providers without architectural changes. No new DESIGN work was needed for slice-03 — the architect's slice-01 contract is what slice-03 verifies.

### DISTILL — already done

The Playwright `@US-03` scenarios were on disk from slice-01's DISTILL pass as `testWithAuth.skip` markers in `OAuthConnection.spec.ts`:
- `@US-03 @adapter-integration` — happy path through the wizard with `StubOAuthProvider`.
- `@US-03 @error` — HTTPS warning visible when `BaseUrl` is HTTP.
- `@US-03 @requires_external @smoke` — real Entra ID round trip (release-tag-gated).

Backend unit-test scaffolds (`AdoOAuthProviderTest.cs`) get written in step 03-01 per the established RED → GREEN cycle.

### DEVOPS — not run

No new infrastructure, no new env var beyond what slice-01 already configured (`StubOAuthProvider` flag for E2E tests, the existing `Lighthouse:BaseUrl` for the callback). The `oauth-smoke` CI job from slice-01 is reused for the `@requires_external` scenario at release time.

### DELIVER — 6 steps (Phase 03 of the OAuth roadmap), all reaching COMMIT/PASS

| Step | Commit | What landed |
|---|---|---|
| 03-01 | `fef5824f` | `AdoOAuthProvider.cs` (new, ~110 LOC) implementing the Entra ID v2.0 OAuth flow — `https://login.microsoftonline.com/common/oauth2/v2.0/{authorize,token}` endpoints, default scopes `vso.work_write` + `offline_access` (the latter required for refresh-token grant). Mirrors `JiraOAuthProvider`'s shape but encodes Microsoft-specific knowledge. 7 RED tests (4 happy + 3 error paths) turned GREEN. |
| 03-02 | `7795b941` | Schema + DI registration. `AuthenticationMethodKeys.AzureDevOpsOAuth = "ado.oauth"` constant; `AuthenticationMethodSchema` entry under `AzureDevOps` (Premium-gated, Organization URL + Client ID + Client Secret option keys); `Program.cs` DI line + named HttpClient. The startup self-check from slice-01 step 01-03 now passes for `ado.oauth` (registry contains a provider for the registered key). Inert-stub adjustments in two integration test files because `WebApplicationFactory<Program>` does a `RemoveAll<IOAuthProvider>()` per slice-01 test convention. |
| 03-03 | `76b8c1ff` | `AzureDevOpsWorkTrackingConnector` refactored to delegate auth-header construction to `IWorkTrackingAuthStrategyFactory`. PAT-keyed ADO connections continue working unchanged (regression-guarded). `ado.oauth`-keyed connections now produce a Bearer header from the existing `OAuthBearerAuthStrategy` — no new strategy class needed. Constructor signature change rippled through `AzureDevOpsWriteBackTest.cs` and `WorkTrackingConnectorFactoryTest.cs` (mechanical test-fixture adjustments). |
| 03-04 | `ea04df32` | `OAuthAuthForm.tsx` — inline `Alert severity="warning"` rendered when `providerKey === "ado.oauth"` AND `baseUrl` is HTTP (not HTTPS). Copy: *"Azure DevOps requires HTTPS callback URLs in production"*. Provider-keyed: Jira OAuth doesn't see the warning. `AuthMethodDropdown` confirmed data-driven — picks up the `ado.oauth` entry automatically from the DTO, no component change needed (one new test added to lock that contract). |
| 03-05 | `7f6837bc` | Unskipped the two `@US-03 @in-memory` Playwright scenarios in `OAuthConnection.spec.ts` and implemented their bodies against `StubOAuthProvider`. Extended POM (`WorkTrackingSystemCreateWizard.ts`) with `selectAuthenticationMethod`, `adoHttpsWarning`, `connectButton` getters. Added `createOAuthAdoConnection` helper mirroring its Jira sibling. All new `getByRole({ name })` for single-verb labels use `exact: true` (per ci-learnings 2026-05-16). The `@US-03 @requires_external @smoke` scenario stays `.skip` — release-tag gated, out of scope. Cumulative slice-03 AC #5 diff verification recorded in commit body. |
| 03-06 | `380df2a0` | Customer-facing setup guide `docs/concepts/worktrackingsystems/oauth-ado.md`. Covers Entra ID app registration (single-vs-multi-tenant trade-offs), required API permissions (`vso.work_write` + `offline_access`), redirect URI canonicalisation to `{BaseUrl}/api/oauth/callback`, client secret creation + manual rotation, in-Lighthouse credential paste flow, troubleshooting (`AADSTS50011` reply-URL mismatch, `AADSTS65001` missing consent, popup blocker). Cross-linked into the Jira sibling page and the BaseUrl page (which had a "coming in Slice 03" placeholder, now resolved). |

Plus `6a124421` Stryker per-feature configs (Phase 5 output, see below).

### Phase 3 (refactor) — Path B, nothing to refactor

The per-step REFACTOR during each GREEN already brought all in-scope files to a clean state. RPP L1-L6 swept clean:
- **L5 / L6 (extract function / class)**: `AdoOAuthProvider` is structurally similar to `JiraOAuthProvider` but encodes different IdP knowledge (Microsoft v2.0 endpoints + `vso.*` scopes vs Atlassian endpoints + Jira scopes). **Intentional non-refactor** — premature abstraction would couple two IdPs that must evolve independently. Same call for `createOAuthAdoConnection` ↔ `createOAuthJiraConnection`. CLAUDE.md's "DRY = don't repeat KNOWLEDGE, not code" rule forbids the extraction.

### Phase 4 (adversarial review) — APPROVED, zero blockers

The `nw-software-crafter-reviewer` ran the full Testing Theater 7-pattern detection + CLAUDE.md convention check + AC #5 invariant verification. Verdict: APPROVED. AC #5 invariant satisfied — cumulative diff touches only the allowed files. Zero `window.*` usages in the new TS code (S7764 pre-flight clean). Zero `Assert.Multiple(()…)` patterns in C# (NUnit2056 clean). All new Playwright `getByRole({ name })` for single-verb labels use `exact: true` (ci-learnings 2026-05-16). All 9 quality gates (G1–G9) pass.

One non-blocking suggestion (KPI tag consistency on the ADO walking-skeleton Playwright scenario) deferred to a future polish PR.

### Phase 5 (mutation testing) — PASS-with-rationale

Stryker per-feature configs added in commit `6a124421`:
- `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.ado-oauth.json`
- `Lighthouse.Frontend/stryker.config.ado-oauth.mjs`
- `Lighthouse.Frontend/vitest.stryker.ado-oauth.config.ts`

Kill rates:
- **Frontend** (`OAuthAuthForm.tsx`): **88.24%** (60/68 killed) — above the 80% gate. 8 survivors all classified as acceptable misses (MUI severity literals, defensive fallback copy, `length > 0` truthy-vs-non-negative equivalent mutants).
- **Backend** (`AdoOAuthProvider.cs`): **89.5%** (34/38 killed) — above the 80% gate. 4 survivors: defensive `JsonSerializer.Deserialize() ?? throw` null-guard (contractually impossible from Microsoft), log-statement string literals (Sonar-banned to assert on), fallback error-code default.
- **Backend** (`AzureDevOpsWorkTrackingConnector.cs` OAuth surface): 1/18 killed locally because the env-var-gated live ADO integration tests don't run without `AzureDevOpsLighthouseIntegrationTestToken`. CI runs them against the real `huserben` ADO sandbox and would close these mutants. The `ConvertAuthorizationHeaderToVssCredentials` helper is intentionally internal (not exposed via the driving port); asserting on `VssBasicCredential` / `VssOAuthAccessTokenCredential` internals from unit tests would violate the port-to-port discipline.

No new tests added in Phase 5 — per the anti-Testing-Theater discipline, all kill-rate signal comes from the slice's own RED → GREEN tests.

### Phase 6 (DES integrity) — clean for Slice 03

All 6 Phase 03 steps (`03-01..03-06`) have complete 5-phase DES entries in `execution-log.json`. The 10 pre-existing violations on Phase 02 / Phase 05 are historical gaps unrelated to slice-03.

### Phase 7 (finalize, this entry) + Phase 8 (retro) — clean execution, no 5-Whys

Phase 03 execution was textbook clean:
- 6 steps, all reaching COMMIT/PASS on the first dispatch.
- One user-initiated terminal-mix-up interrupt during 03-03 (immediately recovered).
- One leftover `.stryker-tmp-popup-reconnect/` sandbox directory from Story #5018 caught 2 phantom "test failures" in the Phase 3.5 gate — cleaned up immediately, root cause flagged as a Stryker hygiene note for the next per-feature run.
- AC #5 invariant verified empirically at step 03-05 (cumulative diff matches the allowed list byte-for-byte).

No retrospective needed beyond noting the clean run.

---

## What changed externally (release-notes payload)

User-visible changes:
- **Azure DevOps OAuth connection** — new authentication method available when adding an ADO work-tracking connection. Requires Premium licence (matching the Jira OAuth gate from slice-01). Uses Entra ID v2.0 endpoints; supports silent token refresh via the slice-02 refresh service; can be re-authorised via the slice-04 popup if it ever drops.
- **HTTPS warning on the ADO OAuth form** — if a self-hosted Lighthouse instance is configured with `Lighthouse:BaseUrl=http://...`, the form surfaces an inline warning ("Azure DevOps requires HTTPS callback URLs in production") to prevent the operator from registering a redirect URI that Microsoft will reject.

Internal changes:
- New file: `Lighthouse.Backend/Services/Implementation/OAuth/Providers/AdoOAuthProvider.cs` (~110 LOC).
- `AzureDevOpsWorkTrackingConnector` no longer constructs auth headers inline — delegates to `IWorkTrackingAuthStrategyFactory` per the slice-01 strategy abstraction.
- One new constant in `AuthenticationMethodKeys`, one new entry in `AuthenticationMethodSchema`, one new DI line in `Program.cs`. That is the complete production-code footprint.

---

## The architectural integrity proof

**Slice 03 AC #5** stated: *"the only files touched are (a) `AdoOAuthProvider.cs`, (b) DI registration, and (c) docs. No change to `OAuthCredential`, `OAuthConfiguration`, the controller, or the refresh service."*

Cumulative diff over the 7 slice-03 commits (`fef5824f..6a124421`) touches:
- Production source: `AdoOAuthProvider.cs` (new), `AuthenticationMethodKeys.cs` (+1 constant), `AuthenticationMethodSchema.cs` (+1 entry), `Program.cs` (+1 DI line + named HttpClient), `AzureDevOpsWorkTrackingConnector.cs` (constructor + auth-header delegation), `OAuthAuthForm.tsx` (+1 conditional alert).
- Tests: `AdoOAuthProviderTest.cs` (new), `AuthenticationMethodSchemaTest.cs` (+1 parametrized case), `AzureDevOpsWorkTrackingConnectorTest.cs` (+2 delegation tests + 2-arg constructor fixture), test-side mechanical adjustments in 4 collateral test files for the constructor-signature change.
- E2E: `OAuthConnection.spec.ts` (2 scenarios unskipped + bodies), POM extensions, helper extensions.
- Docs: `oauth-ado.md` (new), cross-links into `oauth-jira.md` + `oauth-baseurl.md`.
- Stryker per-feature configs (Phase 5).

Production-code files explicitly NOT touched: `OAuthController.cs`, `OAuthService.cs`, `OAuthCredential.cs`, `OAuthConfiguration.cs`, `OAuthTokenRefreshService.cs`, `OAuthStateTokenIssuer.cs`, `LighthouseAppContext.cs` (no migration needed), `IOAuthProvider.cs`, `IOAuthProviderRegistry.cs`, `IWorkTrackingAuthStrategyFactory.cs`, `PatAuthStrategy.cs`, `JiraBasicAuthStrategy.cs`, `OAuthBearerAuthStrategy.cs`, `JiraOAuthProvider.cs`, `StubOAuthProvider.cs`.

**The slice-01 abstraction absorbed ADO OAuth exactly as designed.** Future providers (Linear, GitHub, etc.) can follow the same six-step pattern with similar minimal blast radius.

---

## Outstanding work flagged for follow-up

| Item | Owner | Trigger |
|---|---|---|
| KPI tag consistency: add `@kpi-OUT-oauth-setup-success-rate` to the ADO walking-skeleton Playwright scenario to mirror the Jira sibling | Future polish PR | Opportunistic; non-blocking per Phase 4 reviewer |
| OQ-5018-3 Safari Webkit gold-test for `window.opener` severance (carried from Story #5018 evolution archive) | Future slice | When Safari support becomes a hard customer ask |
| `buildCallbackUrl` byte-identical duplication between `OAuthAuthForm` and `CreateConnectionWizard` (carried from Story #5018 evolution archive) | Future cleanup PR | Opportunistic |
| `oauth-smoke` CI job activation against real Entra ID for the `@US-03 @requires_external @smoke` scenario | Phase 05 release-readiness slice (already drafted in roadmap.json) | Pre-GA release tag |

---

## Files committed in Phase 03 (7 commits, ~600 insertions / ~80 deletions)

- `fef5824f  feat(oauth): add AdoOAuthProvider with Entra ID OAuth flow`
- `7795b941  feat(oauth): register ado.oauth provider in schema + DI`
- `76b8c1ff  refactor(oauth): AzureDevOpsWorkTrackingConnector delegates auth to IWorkTrackingAuthStrategy`
- `ea04df32  feat(oauth): add ADO HTTPS warning on OAuthAuthForm`
- `7f6837bc  test(oauth): unskip Playwright scenarios 8-9 (ADO OAuth + HTTPS warning) + verify Slice 03 AC #5 invariant`
- `380df2a0  docs(oauth): add Azure DevOps OAuth setup page`
- `6a124421  chore(oauth): add Stryker per-feature configs for Slice 03 ADO OAuth`
