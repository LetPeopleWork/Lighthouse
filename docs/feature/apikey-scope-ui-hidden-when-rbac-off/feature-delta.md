# Feature Delta: apikey-scope-ui-hidden-when-rbac-off

<!-- markdownlint-disable MD024 MD041 -->

Wave: DISTILL | Date: 2026-05-13 | Density: lean (per ~/.nwave/global-config.json)

Feature goal: in the **Create API Key** dialog, hide the "Restrict scope
(optional)" section when RBAC is disabled in the running deployment. The
section currently renders unconditionally whenever authentication is on,
inviting the user to configure per-key scopes that the backend silently
ignores — `RbacAdministrationService` short-circuits every permission check
to "allow" before `IntersectWithApiKeyScope` ever runs (verified at
`Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs`,
line ~44 `IsRbacEnforcedAsync` gate; intersection at line ~947).

This is a fast-tracked DISTILL. DISCUSS / DESIGN / DEVOPS are not run as
separate sessions. The change is a single conditional render in
`Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx`. No
backend change, no endpoint change, no schema change, no migration, no new
service or hook. The relevant prior decisions live in
`docs/product/architecture/adr-004-apikey-scope-storage.md` (which
introduced scopes) and `docs/feature/api-keys-for-all-users/feature-delta.md`
(which widened the tab to every authenticated user, making the
inert-when-RBAC-off scope picker visible to a larger population).

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| `Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs` | Every permission-check method opens with `if (!await IsRbacEnforcedAsync(...)) return true;` (or equivalent permissive return). Scope intersection (`IntersectWithApiKeyScope`) is only reached when RBAC is enforced. | n/a | The backend contract guarantees scopes have zero effect when RBAC is off. The UI fix is purely cosmetic / discoverability — it removes a control that would have no consequence regardless. No backend change needed. |
| `Lighthouse.Backend/Services/Implementation/Auth/ApiKeyAuthenticationHandler.cs` | The handler validates the API key, emits `sub` / `oid` / `api_key_id` / `auth_method=api-key` claims, but performs no scope evaluation. | n/a | Authentication path is unaffected. Hiding the scope picker does not change which claims the API key carries; it only removes the *option to attach scope rows during creation* when RBAC is disabled. |
| `docs/product/architecture/adr-004-apikey-scope-storage.md` | "If `ApiKeyPermissions` has zero rows for the key, the key inherits the full owner scope (backwards-compatible default; existing keys remain functional after migration)." | ADR-004 | Hiding the picker means newly created keys on RBAC-off deployments are persisted with zero scope rows — which is exactly the documented backwards-compatible default. No new semantics introduced. |
| `Lighthouse.Frontend/src/hooks/useRbac.ts` | `useRbac()` exposes `isRbacEnabled: boolean`, sourced from `UserAuthorizationSummary.isRbacEnabled` via `/authorization/my-summary`. While loading, and on fetch failure, it returns the `PERMISSIVE_SUMMARY` default with `isRbacEnabled=false`. | n/a | The driving port for "is RBAC on right now" is already in place. The fix consumes the existing hook; no new RBAC port is added. The loading / error defaults conveniently align with the safe behaviour (hide the picker). |
| `docs/feature/api-keys-for-all-users/feature-delta.md` | The API Keys settings tab is visible to every authenticated user, not only System Admins. | n/a | This is the reason the bug matters more now: Viewers and Team Admins on RBAC-off deployments will see an inert scope picker on first contact with the feature. Hiding it cleans up the surface for the wider audience. |
| `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` (auth-disabled branch, lines 357–362, 425–434) | When authentication is disabled, the panel shows an info alert ("Authentication is not enabled. API keys are unavailable until auth is enabled.") and disables the "New API Key" button. | n/a | Behaviour preserved verbatim. The dialog cannot be opened when auth is off, so the RBAC-off scope-hiding rule never applies on auth-off deployments. The two gates compose correctly. |

---

## Wave: DISTILL / [REF] Wave-decision reconciliation

This feature contradicts no prior wave decision. It is a defensive
discoverability cleanup made possible — and made more urgent — by two
shipped features:

| Prior decision | Status under this feature |
|---|---|
| `security-review-2026-05/S-5` → ADR-004: introduce per-key scopes via `ApiKeyPermissions` table | **Compatible.** Scopes remain stored as before. Picker is hidden only when RBAC is off, in which case scope rows would be persisted but never read. |
| `api-keys-for-all-users`: tab is visible to every authenticated user | **Compatible and complementary.** That feature widened the audience for the API Keys panel; this feature ensures that wider audience is not invited to configure inert controls. |

**Open question explicitly out of scope**: should the scope picker also be
hidden when RBAC is enabled but the *caller* has no permissions wide enough
to create *any* scoped key (e.g. a pure Viewer)? Today the picker is shown
to such a caller, and the backend would reject a submission with a 4xx
("caller's own permissions must be a superset of the requested scope" per
ADR-004). That is a separate UX issue — tracked here as a deliberate
non-goal so the DELIVER wave does not silently expand scope. If we choose
to address it later, the candidate feature name is
`apikey-scope-ui-gated-on-caller-permissions`.

---

## Wave: DISTILL / [REF] User stories (implicit DISCUSS)

Recorded here because no separate DISCUSS wave was run.

**US-1 — Operator running without RBAC creates a personal API key**
As a Lighthouse user on a deployment where the operator has left RBAC off
(single-tenant / kiosk / pre-rollout install), I want the Create API Key
dialog to show me only the fields that *do* something, so that I do not
configure a scope I assume will be enforced when in fact it will be
silently ignored. Emotional arc: from "I think I just locked this key down
to one team" → "the UI never offered me a control that doesn't work." JTBD:
"Trust the UI: every visible control has an effect."
Covered by walking skeleton + M1.2 + M1.5.

**US-2 — Operator on a fully configured deployment sees no regression**
As a Lighthouse user on a deployment with RBAC enabled, I want the scope
picker to remain available exactly as it is today, so that the per-key
least-privilege flow introduced by `security-review-2026-05/S-5` is not
disturbed by the cosmetic fix for the RBAC-off case. Covered by M1.1
(regression pin) and the pre-existing
`F_FE_1_CreateApiKeyDialogScope.test.tsx` suite (the bulk of which keeps
working without modification because its `getAuthorizationSummary` mock
returns `isRbacEnabled=true`).

**US-3 — User on a transitional deployment (config flip)**
As a Lighthouse user whose System Admin has just flipped RBAC on or off, I
acknowledge that the dialog reflects the RBAC state captured at the time
`useRbac()` last fetched the authorization summary (i.e., page load). A
full browser refresh is required to pick up a config flip. This is
consistent with all other RBAC-gated UI in the product (`Header`,
`Settings`, `OverviewDashboard`, `PortfolioDetail`). Covered implicitly by
M1.4 and out of scope for a per-flip live update.

**Release notes draft for the DELIVER commit message**:

> **UI**: The "Restrict scope (optional)" section in the Create API Key
> dialog is now hidden on deployments where RBAC is disabled. Per-key
> scopes have no effect when RBAC is off (the authorization service
> short-circuits to "allow" before scope intersection runs), so the
> control no longer appears in that configuration. Behaviour with RBAC
> enabled is unchanged.

---

## Wave: DISTILL / [REF] Acceptance scenarios

Scenario SSOT lives in `docs/feature/apikey-scope-ui-hidden-when-rbac-off/acceptance/*.feature`.
Below is the index with tags. Each scenario maps to ONE TDD cycle in DELIVER.

| Scenario | File | Tags | TDD slice |
|---|---|---|---|
| Non-admin opens Create API Key dialog on an RBAC-disabled deployment and sees no scope picker | `acceptance/walking-skeleton.feature` | `@walking_skeleton @in-memory @driving_adapter` | WS |
| M1.1 Scope picker is visible when RBAC is enabled (regression pin) | `acceptance/milestone-1-scope-visibility.feature` | `@in-memory @milestone-1 @driving_adapter` | M1.1 |
| M1.2 Scope picker is hidden when RBAC is disabled | `acceptance/milestone-1-scope-visibility.feature` | `@in-memory @milestone-1` | M1.2 |
| M1.3 Scope picker is hidden when the authorization summary fetch fails | `acceptance/milestone-1-scope-visibility.feature` | `@in-memory @milestone-1 @error` | M1.3 |
| M1.4 Scope picker is hidden while the authorization summary is still loading | `acceptance/milestone-1-scope-visibility.feature` | `@in-memory @milestone-1` | M1.4 |
| M1.5 Submitting without scopes (RBAC off) sends no scope field | `acceptance/milestone-1-scope-visibility.feature` | `@in-memory @milestone-1` | M1.5 |

---

## Wave: DISTILL / [REF] Walking skeleton strategy

**Strategy A — Full InMemory.** Justification: this is a pure
frontend visibility feature. The only driving adapter is the
`ApiKeysSettings` React component tree rendered via React Testing
Library; all driven adapters (`rbacService`, `apiKeyService`,
`authService`, `teamService`, `portfolioService`) are HTTP boundaries
already covered by their own service-level tests
(`RbacService.test.ts`, etc.). Real I/O at the component-test level
would add network setup with zero coverage gain.

The walking-skeleton scenario is executed inside Vitest + RTL using
the existing `createMockApiServiceContext` factory in
`Lighthouse.Frontend/src/tests/MockApiServiceProvider.ts`. The driving
adapter is the rendered `<ApiKeysSettings />` component; the test
clicks the real "New API Key" button (not a function call into a
service) and asserts on the DOM. That satisfies Mandate 6 (Driving
Adapter) for a frontend feature: the user's actual entry path is the
component, exercised through its rendered UI, not through a direct
service call.

InMemory cannot model: a *runtime* flip of RBAC enablement between
page-load and dialog-open (the test mounts with one state and that
state is captured by `useRbac()` exactly as in production). This is
deliberate — see US-3 above; live config-flip handling is out of
scope.

---

## Wave: DISTILL / [REF] Adapter coverage

| Adapter | @real-io scenario | Covered by |
|---|---|---|
| `RbacService.getAuthorizationSummary()` (HTTP GET `/authorization/my-summary`) | NO (covered transitively by Strategy A mock) | `Lighthouse.Frontend/src/services/Api/RbacService.test.ts` already pins the HTTP contract for this endpoint. This feature does not change the request or response shape. |
| `ApiKeyService.createApiKey()` (HTTP POST `/api/apikeys`) | NO (covered transitively by Strategy A mock) | Existing service tests pin the POST contract. M1.5 asserts only that the request body emitted by the *frontend* does not include a `scope` property when no scope rows exist — the body shape itself is unchanged. |
| `useRbac()` hook (frontend port) | YES via M1.1–M1.4 | The hook is exercised directly by the component under test in every M1 scenario. No new driven adapter is introduced. |

No "NO — MISSING" rows. No new driven adapter is introduced by this
feature; every adapter the component depends on has pre-existing
real-contract coverage in its own service test file.

---

## Wave: DISTILL / [REF] Driving adapter coverage

| Driving adapter (from DESIGN) | WS scenario exercising it |
|---|---|
| `<ApiKeysSettings />` React component (mounted at `/settings` → API Keys tab) | Walking skeleton: render component, click "New API Key", assert DOM absence of "Restrict scope (optional)" section. |

No CLI subcommand, no HTTP endpoint, no hook adapter is introduced by
this feature. The single driving adapter is the existing settings
panel.

---

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Frontend/src/pages/Settings/ApiKeys/` — co-located with
the existing component tests:

- `ApiKeysSettings.test.tsx` (general dialog behaviour)
- `F_FE_1_CreateApiKeyDialogScope.test.tsx` (scope picker behaviour
  when RBAC is enabled; this feature **does not modify** that file —
  its scenarios remain the M1.1 regression pin)

DELIVER will add **one new test file** at this location, conventionally
named `CreateApiKeyDialogScope_RbacOff.test.tsx`, containing the five
scenarios above as Vitest cases. Precedent: every API-key UI test
already lives in this directory; deviating would obscure the test
graph.

---

## Wave: DISTILL / [REF] Scaffolds

**None.** All production modules referenced by the scenarios already
exist:

- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` — the
  component to be modified
- `Lighthouse.Frontend/src/hooks/useRbac.ts` — already exports
  `isRbacEnabled`
- `Lighthouse.Frontend/src/services/Api/RbacService.ts` — already
  serves `/authorization/my-summary`

The DELIVER change is a **single conditional render** inside
`CreateApiKeyDialog` (gate the `<Accordion>` block at
`ApiKeysSettings.tsx` lines 251–282 on
`useRbac().isRbacEnabled === true`). The tests will be RED against
the current code because the accordion renders unconditionally;
they will go GREEN once the conditional is added. No RED-ready stub
file is needed.

Mandate 7 is satisfied vacuously: zero new modules to scaffold, zero
`__SCAFFOLD__` markers to clean up later.

---

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN driving ports inherited: `useRbac()` (FE port), already in
  place from `rbac-enhancements`.
- DEVOPS environment matrix inherited from `api-keys-for-all-users`:
  - **Auth enabled + RBAC enabled** (default for production) — M1.1
    regression pin.
  - **Auth enabled + RBAC disabled** (single-tenant / kiosk / pre-rollout)
    — the case this feature targets. WS + M1.2 + M1.3 + M1.4 + M1.5.
  - **Auth disabled** — out of band: dialog cannot be opened (the
    "New API Key" button is disabled in `ApiKeysSettings.tsx` line 446
    when `authEnabled !== true`).
- No new infrastructure, no new config flag, no new env var.

---

## Wave: DISTILL / [REF] Observability

No new logs, no new metrics. The feature is purely a UI conditional.
Existing telemetry already captures every API key creation via
`ApiKeyService.CreateApiKeyAsync` (per the `api-keys-for-all-users`
observability section); operators wanting to detect "user submitted a
key on an RBAC-off deployment" can derive it from the existing log
stream by joining the creation event against the deployment's RBAC
config.

---

Wave: DELIVER | Date: 2026-05-13 | Commit: 4f443495

## Wave: DELIVER / [REF] Implementation summary

Shipped as a single TDD cycle (RED → GREEN → COMMIT). The Create API Key
dialog now consumes `useRbac()` and wraps the existing scope `<Accordion>`
in `{rbac.isRbacEnabled && (...)}`. Backend untouched. Production diff is
+1 import, +1 hook call, +1 conditional wrapper. Five new Vitest cases pin
the visibility contract across RBAC-enabled (regression), RBAC-disabled,
loading, fetch-failure, and no-scope-payload states.

## Wave: DELIVER / [REF] Files modified

**Production:**

- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` — imports `useRbac`, calls it inside `CreateApiKeyDialog`, gates the scope Accordion on `rbac.isRbacEnabled`.

**Tests:**

- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/CreateApiKeyDialogScope_RbacOff.test.tsx` (new) — five Vitest cases (M1.1 regression pin, M1.2 + WS, M1.3 fetch-fail, M1.4 loading, M1.5 no-scope-payload).
- `Lighthouse.Frontend/src/tests/MockApiServiceProvider.ts` — `createMockRbacService` default flipped from `isRbacEnabled: false` to `isRbacEnabled: true`. Required so that the pre-existing `F_FE_1_CreateApiKeyDialogScope.test.tsx` (which inherits the factory default and exercises scope-row interactions) keeps passing without modification under the new gate. Tests that need the RBAC-off branch override `getAuthorizationSummary` explicitly per scenario — same pattern Header / Overview / Settings / Portfolio test files already use.

**Docs:** none (this section is the only documentation update).

## Wave: DELIVER / [REF] Scenarios green count

6 of 6 acceptance scenarios green (2026-05-13T20:07Z):

- WS — Non-admin opens Create API Key dialog on RBAC-disabled deployment and sees no scope picker ✓
- M1.1 — Scope picker visible when RBAC is enabled (regression pin) ✓
- M1.2 — Scope picker hidden when RBAC is disabled ✓
- M1.3 — Scope picker hidden when authorization summary fetch fails ✓
- M1.4 — Scope picker hidden while authorization summary is loading ✓
- M1.5 — Submitting without scopes sends no `scope` field in POST body ✓

WS + M1.2 are both encoded by the same Vitest case in `CreateApiKeyDialogScope_RbacOff.test.tsx` (the walking-skeleton acceptance scenario IS the M1.2 negative — they share a Given/When/Then up to the assertion).

## Wave: DELIVER / [REF] DoD check

- [x] All acceptance scenarios green
- [x] `pnpm test` green — 2766/2766 tests across 216 files
- [x] `pnpm build` clean — zero TS errors, zero Biome errors, zero warnings
- [x] `F_FE_1_CreateApiKeyDialogScope.test.tsx` remains GREEN unmodified (M1.1 regression pin)
- [x] `ApiKeysSettings.test.tsx` remains GREEN unmodified
- [x] Conventional commit with `Step-ID: 01-01` trailer
- [x] Architecture mandate respected: UI gates on `useRbac()` hook output, not on a direct `/authorization/my-summary` fetch
- [x] No backend change (DWD-3 honored)

## Wave: DELIVER / [REF] Demo evidence

The Lighthouse UI is not a CLI; the "demo command" for this feature is the rendered component under the RTL harness. Test output (`pnpm test src/pages/Settings/ApiKeys/CreateApiKeyDialogScope_RbacOff.test.tsx -- --run`):

```
 ✓ CreateApiKeyDialogScope_RbacOff
   ✓ renders scope accordion when RBAC is enabled (M1.1 regression pin)
   ✓ does NOT render scope accordion when RBAC is disabled (M1.2 + WS)
   ✓ does NOT render scope accordion when authorization summary fetch fails (M1.3)
   ✓ does NOT render scope accordion while authorization summary is loading (M1.4)
   ✓ submits request without scope field when RBAC is off and no rows are configured (M1.5)

 Test Files  1 passed (1)
      Tests  5 passed (5)
```

Manual smoke verification deferred to the reviewer running the app locally with `Authorization:Rbac:Enabled=false` and opening Settings → API Keys → New API Key.

## Wave: DELIVER / [REF] Quality gates

| Phase | Status | Note |
|---|---|---|
| PREPARE | EXECUTED | All prior-wave files read; Accordion location confirmed at lines 251–282 |
| RED_ACCEPTANCE | EXECUTED | WS scenario encoded as Vitest case #2; RED verified in RED_UNIT |
| RED_UNIT | EXECUTED | 5/5 new tests failed for the right reason against unmodified production code |
| GREEN | EXECUTED | All 5 new + 2766 full-suite tests pass; `pnpm build` clean |
| COMMIT | EXECUTED | `4f443495 feat(apikey): hide scope picker when RBAC is disabled` |
| L1–L6 refactor | SKIPPED | 3-line production diff has nothing to refactor |
| Adversarial review | SKIPPED | Per orchestrator pragmatic scope for single-conditional UI fix |
| Mutation testing | SKIPPED | A single boolean conditional has no meaningful Stryker mutants beyond the inversion, which is already pinned by M1.1 + M1.2 |
| DES integrity | PASS | `des-verify-integrity` exit 0; all 5 phases logged for step 01-01 |

## Wave: DELIVER / [REF] Pre-requisites consumed

- DISTILL scenarios: walking-skeleton.feature + milestone-1-scope-visibility.feature (6 scenarios total)
- DESIGN component manifest: `useRbac()` hook (existing), `ApiKeysSettings.tsx` (existing), no new component
- Architecture mandate: brief.md — "all UI gating derives from `useRbac()` hook"
