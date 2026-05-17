# Evolution — work-tracking-oauth-authentication (Epic ADO #2438)

Date: 2026-05-17
Branch: `main` (slice-boundary push pending finalize sign-off)
Wave path: single canonical artifact (`feature-delta.md`) covering DISCUSS → DESIGN → DISTILL → DELIVER, plus per-slice TDD plans under `slices/`. No separate wave subfolders for this feature.
Outcome: shipped. Lighthouse can now authenticate to Jira (3LO) and Azure DevOps (Entra ID OAuth) connectors using OAuth 2.0; access tokens refresh transparently; reconnect is a popup that keeps the originating dialog mounted; OAuth surfaces are premium-gated. Five child stories landed (#4967, #4968, #4969, #4971, #4972); #4970 (standalone guard) was removed during DELIVER; #5018 (popup reconnect) was added during DELIVER as Phase 06.

---

## Feature goal and user intent

A `connector-admin` previously had only Personal Access Tokens (Jira API tokens, ADO PATs) to wire Lighthouse to their work-tracking system. PATs are bound to an individual user's account: rotation is manual, offboarding breaks every connection that human owned, and the secret pasted into Lighthouse cannot be governed from the IdP. OAuth solves all three problems — Lighthouse holds a refreshable bearer issued to an OAuth *app*, not a person. Governance lives in Atlassian / Microsoft consoles; rotation is silent; offboarding doesn't break anything.

Epic 2438 frames this as a premium-gated, additive feature: existing PAT connections keep working untouched; OAuth is a new auth method in the existing dropdown. Five user-facing stories cover Jira OAuth, ADO OAuth, transparent token refresh, the `IOAuthProvider` abstraction that lets future providers slot in without DB migrations, and `Lighthouse:BaseUrl` so the displayed callback URL is correct behind reverse proxies. A sixth story (#4970, standalone-mode guard) was reversed mid-flight when its cost/benefit ratio failed under scrutiny.

---

## Business context

| Item | Value |
|---|---|
| Epic | ADO #2438 — *OAuth for Jira and Azure DevOps work-tracking connections* |
| Premium gate | Required by epic body; enforced via `LicenseGuardAttribute` + `AuthenticationMethodSchema.IsPremium` |
| Child stories shipped | #4967 (Jira OAuth), #4968 (token refresh), #4969 (ADO OAuth), #4971 (provider-agnostic abstraction), #4972 (BaseUrl) |
| Child stories removed | #4970 (standalone-mode guard) — reversed 2026-05-16, see decisions D2 below |
| Child stories added during DELIVER | #5018 (popup reconnect — Phase 06 slice) |
| Outcome KPIs | Admin-visible only (per-instance). Vendor telemetry blocked on Epic 5015 (opt-in telemetry, no timeline). |

---

## Decisions D1..D10 — status at close

Sourced from `feature-delta.md` "Locked decisions" plus the post-DELIVER notes.

| ID | Decision (summary) | Status at close |
|---|---|---|
| D1 | OAuth is premium-license-gated for both Jira and ADO | **Held.** Two-layer gate (schema flag + `LicenseGuardAttribute`) enforced at both ends. |
| D2 | OAuth is server-mode only; standalone (Tauri) shows it disabled with tooltip | **Reversed 2026-05-16.** Standalone users may attempt OAuth at their own risk; no UI guard ships. Story #4970 removed; Slice 04 deleted. The forward rule: if standalone-OAuth becomes a support burden, address it with a docs/runbook entry, not a UI guard. |
| D3 | OAuth is provider-agnostic — `IOAuthProvider` port, DI-registered, keyed by provider-name string | **Held.** Proved by Slice 03: ADO OAuth shipped as a new provider class + DI registration + docs page, with no touch to `OAuthCredential`, `OAuthController`, the refresh service, or the migration history. AC #5 ("Implementation invariant") verified at PR-diff time. |
| D4 | Callback URL displayed in the form is derived from `Lighthouse:BaseUrl`, not request origin; non-blocking warning when unset | **Held.** Slice 01 implemented; Slice 03 inherited the same shape. |
| D5 | Refresh tokens are stored and rotated on a pre-request expiry check (5-min window); refresh failure marks `RefreshFailed` + reconnect banner | **Held.** Slice 02 (`OAuthTokenRefreshService` + single-flight mutex) shipped; refresh is operationally silent against real Atlassian token expiry. |
| D6 | `OAuthCredential` (runtime tokens) persisted separately from static OAuth-app config (clientId/clientSecret); both encrypted via existing `ICryptoService` | **Held with DESIGN refinement.** No `OAuthConfiguration` entity exists — clientId/clientSecret reuse `WorkTrackingSystemConnectionOption` rows (DDD-3). The separation intent is preserved (`OAuthCredential` is its own table with cascade-delete FK); the static-config mechanism reuses existing infrastructure. |
| D7 | Jira (3LO) ships first; ADO (Entra ID) ships in a separate slice once the abstraction is proven | **Held.** Slice 01 → Slice 02 → Slice 03 ordering preserved end-to-end. |
| D8 | No automatic migration from existing PAT/API-token connections; OAuth is opt-in per connection | **Held.** Both PAT and OAuth auth methods coexist; no migration tooling shipped. |
| D9 | OAuth is connection-level, not per-user; downstream Lighthouse users still authenticate to the app via OIDC | **Held.** No per-user OAuth surface introduced. |
| D10 | OAuth integrates into the existing `AuthenticationMethodSchema` SSOT; no parallel auth-method registry | **Held.** Two new keys (`jira.oauth`, `ado.oauth`) registered as schema extensions. |

---

## What shipped per slice

### Slice 01 — Jira OAuth (stories #4967 + #4971 + #4972)

Foundation slice. Established the `IOAuthProvider` port, the `OAuthCredential` entity (with EF migrations on SQLite + PostgreSQL), the controller (`POST /api/oauth/jira/connect`, `GET /api/oauth/callback`, `POST /api/oauth/jira/disconnect`), the `OAuthAuthForm` + `AuthMethodDropdown` premium-gated UI, `Lighthouse:BaseUrl` plumbing, and the `JiraWorkTrackingConnector` bearer-header refactor (`IWorkTrackingAuthStrategy` extraction). 16 TDD steps (01-01..01-16); each a focused commit. Acceptance: 8 ACs across the Jira flow, including the "developer adds a stub provider without backend changes" honesty test (AC #8) that proves the abstraction.

Known limitation as shipped: Atlassian's Agile API (`/rest/agile/...`) returns `401 "scope does not match"` for OAuth tokens even when the token carries the documented scopes. Plain Jira platform endpoints work. Workaround: use `jira.cloud` (API token) for board-driven team setup. Open with Atlassian support; no Lighthouse-side fix.

### Slice 02 — Token refresh + reconnect banner + OAuth Health KPIs (story #4968, folded with OQ-DV1)

`OAuthTokenRefreshService` with single-flight refresh mutex per `OAuthCredential.Id`. `OAuthCredential.status` field (`Valid` / `RefreshFailed` / `Disconnected`). Pre-request expiry check (refresh if expiry within 5 min). `requiresReconnect: boolean` additive flag on the connections-list DTO. `ReconnectBanner` UI. `GET /api/oauth/health` endpoint + admin-visible header health icon (the originally-proposed `OAuthHealthTile` collapsed to a header status icon during refactor `801eee1d`). The walking-skeleton WS strategy for slice 02 was re-layered mid-flight (commit `a0dafcec`): four Playwright scenarios moved to backend-integration tests where they execute deterministically. BI-3 (32-concurrent-caller single-flight test) proves the refresh-storm invariant.

Post-DELIVER defect surfaced 2026-05-17: scoped `LighthouseAppContext` race when `TeamUpdater`'s 8-way fan-out reads OAuth credentials concurrently. Fix landed in `ee029034` (per-connection serialisation in `OAuthService.LoadValidCredentialOrThrow`).

### Slice 03 — Azure DevOps OAuth (story #4969)

`AdoOAuthProvider` implementing `IOAuthProvider`. `ado.oauth` auth-method key registered in the schema. `AzureDevOpsWorkTrackingConnector` delegates to `IWorkTrackingAuthStrategy` (commit `76b8c1ff`). HTTPS-warning rendering on the ADO OAuth form when `BaseUrl` is HTTP. 6 TDD steps (03-01..03-06). AC #5 invariant verified: the only files touched relative to Slice 01 are `AdoOAuthProvider.cs`, its DI registration, and the docs page — no churn on `OAuthCredential`, `OAuthConfiguration`, the controller, or the refresh service. The abstraction held.

Post-DELIVER defects on ADO OAuth: tenant-awareness (commit `e8c44ab9` — additive `OAuthFlowContext.TenantId`); `vso.work_write` scope resource-prefix for Entra v2.0 (`58e9310f`); `VssConnection` cache fingerprint so PAT-rotation and OAuth-refresh invalidate cleanly (`5d985e30`); Entra identity-materialization on first API call documented as troubleshooting (`053c26ce`).

### Slice 04 — Popup reconnect (story #5018, added during DELIVER)

Manual testing of Slice 02 surfaced two UX problems: the full-page redirect destroyed dialog context, and the backend had to persist a half-baked `OAuthCredential` row to survive the redirect (root cause of the defensive `GroupBy/OrderByDescending` read). Story #5018 replaced the redirect with a same-origin popup. 9 steps (06-01..06-09). DB-level UNIQUE index on `OAuthCredential.WorkTrackingSystemConnectionId`, collapsing the defensive read; new `/oauth/popup-complete` landing page; `useOAuthPopup` hook (centred 600×700, `postMessage` handshake, popup-blocked / cancelled / error fallbacks); `ReconnectBanner` + `OAuthAuthForm` + `CreateConnectionWizard` migrated to the hook; mount-time `?oauth=success` resume URL deleted as dead code. Full evolution write-up: `docs/evolution/2026-05-16-oauth-popup-reconnect-story-5018.md`.

### Slice 05 — Release-readiness (Phase 05 of the roadmap)

Tracked the few release-blocking items that surfaced during DELIVER:

| Step | Commit | What landed |
|---|---|---|
| 05-01 | (dropped) | Originally `oauth-smoke` CI job. Dropped 2026-05-17 — `ci_verifysqlite.yml` + `ci_verifypostgres.yml` already run the full Playwright suite against `StubOAuthProvider` on every PR, so a separate gate-job was redundant. |
| 05-02 | `8ad36543` | Deleted the `@requires_external @smoke` Playwright scaffolds (scenarios 11-12). The pre-release smoke contract is satisfied by the existing stub-mode suite; the scaffolds were duplicate. |
| 05-03 | `2c2f4fb4` | `docs/ci-learnings.md` entry: *"OAuth callback origin must be in `Authentication:AllowedOrigins` for reverse-proxy deployments"* — operators must add the public callback origin (indexed `__0` form) or the IdP redirect is fail-closed by the CORS guard. Cross-links the 2026-05-13 `AllowedOrigins` env-var entry. The originally-proposed `oauth-observability.md` page was dropped (`f2705e67`). |
| 05-04 | `7cb8e06c` | Persist `OAuthStateSecret` across restarts via `IDataProtectionProvider`. Before this, each boot regenerated the secret in-memory, invalidating outstanding 15-minute state tokens — a release blocker for any production environment that restarts during a user's OAuth flow. |
| Phase close | `55e3aac5` | Workspace marker — Phase 05 release-readiness closed out. |

Two collateral fixes during Phase 05 hardening:

- `5dff8492` — `AuthMethodDropdown` wires `aria-labelledby` via `useId()` + `<InputLabel id>` + `<Select labelId>`. Without this, the MUI Select's accessible name fell back to the selected value; Playwright's `getByRole('combobox', { name: 'Authentication Method' })` never resolved. CI-learnings entry filed.
- `60820204` + `68142a18` — landed an ADO HTTPS warning render in the wizard, then **reverted** when review noticed the new Playwright scenario was over-asserting against a `Connect` button that does not exist in the wizard (it lives in `OAuthAuthForm`, which the wizard does not render). The revert is intentional. The warning behaviour is covered at the right layer by `OAuthAuthForm.test.tsx` Vitest cases. This regression spawned the marquee lesson — see below.

---

## Lessons learned

### Marquee — never commit a Playwright test you have not RUN against a locally-started Lighthouse

Slice 05's `60820204` → `68142a18` revert cost two CI cycles on the same scenario (`OAuthConnection.spec.ts:157`, `[@US-03 @error] ADO OAuth form warns when BaseUrl is HTTP`). First failure: the wizard didn't render the warning. A production "fix" was committed to satisfy locator #1 — but locator #2 then failed because `wizard.connectButton` (`getByRole("button", { name: "Connect", exact: true })`) doesn't exist in the wizard at all; the Connect button lives in `OAuthAuthForm`, which the wizard does not render at any step. The test was authored against a mental model of the UI that did not match the actual markup.

The root cause: `pnpm build` (TypeScript) is happy with any well-typed locator chain, and the Vitest unit suite covers different components. Only `verify-sqlite` / `verify-postgres` (real browser + real backend) catch this class of mistake. CI is not a substitute for `pnpm playwright test`.

Rule landed as `docs/ci-learnings.md` entry **2026-05-17 — Never commit a Playwright test you have not actually run against a running Lighthouse** (committed in `a22bb30e` alongside the suspected-flake note). Before committing any new `*.spec.ts` scenario or POM getter, run it locally with `pnpm playwright test path/to/spec.ts --headed` (or `--ui`) against a locally-started backend. If you cannot run Playwright locally for the change at hand (no browser, no backend, no time), do not commit the spec — either land the production change without an E2E and cover it at Vitest / backend-integration layer, or pause and ask.

### Other durable rules harvested this feature

- **2026-05-13 — `Authentication__AllowedOrigins` scalar env var bound to empty list, crashing host.** Use indexed `__0` form (or the explicit comma/semicolon scalar fix). Filed before this feature; the OAuth callback hardened the rule.
- **2026-05-17 — OAuth callback origin must be in `Authentication:AllowedOrigins` for reverse-proxy deployments.** The IdP redirect arrives with an `Origin` header that the CORS guard rejects if the public UI hostname isn't allow-listed. Internal `Lighthouse__BaseUrl` is not enough. Slice 05 step 05-03.
- **2026-05-17 — MUI `<Select>` without `labelId`/`id` wiring has no aria-labelledby, so the InputLabel text is unreachable from Playwright.** Pair `<InputLabel id={X}>` with `<Select labelId={X} />`; in POMs target `getByRole("combobox", { name: ... })`, not `getByLabel(...)`. Spawned by `5dff8492`.
- **2026-05-17 — Suspected flake: `Refresh Features` button stuck disabled in verify-sqlite only.** `a22bb30e`. Treat as probable flake — rerun once before investigating. Escalate to `/clean-ci` if the pattern recurs three more times after 2026-05-17 without root cause.

### Methodology lessons (nWave-level)

- **Re-layer Playwright scenarios to the layer that covers them deterministically.** Slice 02 originally tried to drive scenarios 4–7 through Playwright; commit `a0dafcec` moved them to backend integration tests where the refresh path is deterministic. Same principle drove the slice-05 revert: warning behaviour belongs in Vitest, not Playwright.
- **Workflow consolidation beats new workflow files.** Slice 05 originally proposed a separate `ci_oauth_integration_smoke.yml`; the implemented design extended `ci_e2e.yml` with an `oauth-smoke` job. The job was then dropped entirely once `ci_verifysqlite.yml` + `ci_verifypostgres.yml` were noticed to already cover the contract with `StubOAuthProvider`.
- **A "carpaccio honesty test" caught a real abstraction risk.** Slice 01 AC #8 ("a developer adds a stub provider without touching the controller, the credential entity, or persistence migrations") would have failed loudly if `IOAuthProvider` had been decoration. Slice 03 confirmed the abstraction is honest: ADO shipped as one new provider class + one DI registration + one docs page, nothing else.

---

## Open items

- **Atlassian Agile API scope mismatch.** Open with Atlassian support; until resolved, the team-creation board picker returns zero results for `jira.oauth` connections. Workaround documented in slice-01 known-limitations and in `docs/troubleshooting/`.
- **Cross-instance OAuth health KPIs.** Customer self-hosted instances do not phone home; aggregate setup-success rate / refresh-success rate / time-to-first-sync KPIs are observable only on the vendor demo env until Epic **#5015** (opt-in telemetry) lands. No timeline.
- **`time_to_first_sync_p95_30d` KPI.** Deferred during Slice 02 — requires the `connection.sync.first_after_oauth` event source, which only has meaningful data post-Slice-01 + a few hours of runtime. Re-evaluate after first GA + 30 days.
- **Stryker mutation kill rate on the popup-reconnect surface — 67.21% backend.** Below the 80% per-feature target. The gap is on the new `useOAuthPopup` 90-s timeout branch; covered by one new test added in Phase 5 mutation pass. Re-run after the next mutation-budget review.
- **Migration tooling for PAT → OAuth.** Explicitly out of scope per D8. Reopen as a separate epic if customer demand surfaces.
- **Story #5019 — generalised connection health.** Mentioned in the Story-#4970 reversal note as a higher-value alternative; not in this epic's scope.

---

## Source artifacts (migrated)

- `docs/architecture/work-tracking-oauth-authentication/feature-delta.md` — canonical decisions + acceptance + post-DELIVER notes.
- `docs/architecture/work-tracking-oauth-authentication/slices/slice-01-jira-oauth.md`
- `docs/architecture/work-tracking-oauth-authentication/slices/slice-02-token-refresh.md`
- `docs/architecture/work-tracking-oauth-authentication/slices/slice-03-ado-oauth.md`

Workspace retained under `docs/feature/work-tracking-oauth-authentication/` (process state — DES audit log + roadmap + environment matrix). Session markers in `.nwave/des/` removed during finalize Phase C.
