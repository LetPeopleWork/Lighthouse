# Evolution — api-keys-for-all-users

Date: 2026-05-11
Branch: `feat/api-keys-for-all-users`
Wave path: DISTILL (fast-tracked) → DELIVER (4 steps)
Outcome: shipped. UI gate relaxed; backend untouched; 14/14 acceptance scenarios green.

---

## Feature goal and user intent

Make the **API Keys** settings tab visible to every authenticated user, and let every authenticated user create and manage their own API keys.

The contradiction this feature unrolls: `ApiKeyController` was already per-user scoped at the backend (create / list / delete keyed off the caller's stable `sub` / `oid` claim), but the React `<Settings />` component still hid the tab behind `rbac.isSystemAdmin`. Operators and non-admin users had to ask a System Admin to create CLI / MCP credentials they were perfectly entitled to manage themselves. This feature aligns the UI gate with what the controller actually does.

User-facing benefit: a Team Reader (or Viewer, or Team Admin, or Portfolio Admin) opens **Settings → API Keys**, creates a personal key, and authenticates their own CLI / MCP client — no admin queue.

---

## Wave-by-wave summary

### DISCUSS — implicit

No separate DISCUSS session was run. The four user stories (US-1 non-admin happy path, US-2 viewer discoverability, US-3 per-user isolation, US-4 auth-disabled operator) and the operator breaking-change note were recorded directly in the feature delta under "User stories (implicit DISCUSS)". The rationale for skipping a dedicated DISCUSS wave: the feature is a permission relaxation on an already-existing, already-per-user-scoped backend surface — no new domain concept, no new endpoint, no new claim, no new role.

### DESIGN — not run

No new component, no new port, no infrastructure change, no migration, no feature flag. The DESIGN artefact that would normally be produced (an architecture-design.md sketch) would have said exactly "no change to component decomposition; the single edit is in `Settings.tsx`'s tab-visibility filter." That paragraph lives in the feature delta's "Pre-requisites" and "RED-ready scaffolds" sections instead.

### DISTILL — fast-tracked

The DISTILL session produced four `.feature` files (`walking-skeleton.feature`, `milestone-1-tab-visibility.feature`, `milestone-2-non-admin-create.feature`, `milestone-3-auth-disabled.feature`) totalling 14 scenarios with a 50 % error / edge ratio (above the 40 % target). Walking-skeleton strategy: C (Real local) — Vitest + Testing Library for the React tab filter, `WebApplicationFactory<Program>` for the HTTP pipeline. No testcontainers needed.

### DELIVER — 4 steps

| Step | Commit | What landed |
|---|---|---|
| 01-01 | `b14ad8f2` | E2E pre-flight: inverted the Team Reader `api-keys-tab` assertion in `RoleBasedAccessControl.spec.ts` (deployment-safety: prevents the `@rbac` Playwright suite from going red in CI when the production edit lands later) |
| 01-02 | `8eadf300` | Backend integration test file `ApiKeyControllerNonAdminAccessTests.cs` — non-admin CRUD round-trip + M2.2/M2.3 per-user scoping pins + M2.4 401 + M2.5 400 (all CONTRACT_PIN_ON_FIRST_RUN; controller was already correct) |
| 01-03 | `e5c68d2a` | Frontend role-variant tests M1.1–M1.6 in `Settings.test.tsx` + the single-character production edit removing `"40"` from `systemAdminTabValues` (cohesive RED → GREEN) |
| 01-04 | `edddc10a` | Auth-disabled alert-text substring pin "Authentication is not enabled" in `ApiKeysSettings.test.tsx` (M3.1 fine-grained criterion) |

DES audit trail: `docs/feature/api-keys-for-all-users/deliver/execution-log.json`. Integrity verification reported clean.

### DEVOPS — implicit

No infrastructure change. The existing `ILogger<ApiKeyService>` signal carries the operator-facing abuse-detection capability for the now-wider population of key creators. The breaking-change UI note in the feature delta is the release-notes payload.

---

## Wave-decision override

This feature **overrides** `rbac-ui-completeness/D4` and inverts `rbac-ui-completeness/US-04` acceptance criterion AC3.

| Prior decision | Was | Now |
|---|---|---|
| `rbac-ui-completeness/D4` | API Keys tab gated on `isSystemAdmin`; tab value `"40"` in `systemAdminTabValues` | API Keys tab visible to every authenticated user; `"40"` removed from `systemAdminTabValues` |
| `rbac-ui-completeness/US-04` AC3 | Team Admin viewing `/settings` does NOT see api-keys-tab in DOM | Team Admin (and Portfolio Admin, and Viewer, and any authenticated user) viewing `/settings` DOES see api-keys-tab in DOM |

Reason: D4's rationale was "API keys are a system-wide admin surface" — but the backend was already per-user scoped at the time D4 shipped. D4 itself flagged the trigger ("If keys become per-user in the future, revisit") and the trigger had already fired before D4 was written. This feature unrolls D4 to align the UI with controller behaviour.

The override is recorded in `feature-delta.md` under "Wave-decision reconciliation" and cited in the inverted Team Reader E2E assertion's commit body (`b14ad8f2`).

---

## Production change minimality

One line of production code changed in the entire delivery:

```diff
-const systemAdminTabValues = new Set(['20', '25', '30', '40', '50']);
+const systemAdminTabValues = new Set(['20', '25', '30', '50']);
```

`Lighthouse.Frontend/src/pages/Settings/Settings.tsx`. Nothing else in `Settings.tsx` changed — same `tabConfig`, same `visibleTabs` filter, same `reverseTabMapping`, same `useEffect` blocks. The test diff (and the E2E assertion inversion) is the rest of the delivery.

This is the "smallest blast radius" principle in action: when the backend already does the right thing, the UI fix is a Set element.

---

## Trade-offs accepted

### No HTTP-level per-user scoping integration test below the test-auth-handler boundary

The new `ApiKeyControllerNonAdminAccessTests.cs` file uses `WebApplicationFactory<Program>` with a custom `AuthenticationHandler` registered as the default scheme. The handler emits a `ClaimsPrincipal` with a configurable `sub` claim. This is enough to exercise the controller's per-user filter end-to-end at the HTTP layer with two distinct synthetic users (Jordan, Riley) — and that is what M2.2 / M2.3 / M2.4 assert.

What this **cannot** exercise: the real OIDC token-introspection pipeline (`Microsoft.AspNetCore.Authentication.OpenIdConnect` against Keycloak). Anything that depends on the actual JWT being parsed and the actual `sub` / `oid` claim being extracted from a real ID token lives above this test's substitution point. Real OIDC wiring is covered by the existing Playwright `@rbac` suite end-to-end, not by `WebApplicationFactory<Program>`.

This is the same scope wall encountered in `team-portfolio-creation-rights` (see "Lessons captured" below). Accepted because: (a) the controller's per-user logic is independent of which authentication handler produced the principal — once a `ClaimsPrincipal` with a `sub` claim is on `HttpContext.User`, the controller treats it the same way regardless of provenance, (b) the OIDC wiring itself is unchanged by this feature, (c) the Playwright `@rbac` suite is the integration check that catches OIDC regressions.

### No mutation testing run

Mutation testing was skipped this delivery. Rationale:

1. Zero new backend production code — every backend test added is a contract pin on existing behaviour. Mutating already-tested logic that the new tests merely re-pin would not reveal anything those tests' authoring did not already address.
2. No frontend Stryker configuration exists in the repo today. Spinning one up purely for this feature's frontend would dwarf the production change in setup cost.
3. The single production change is a Set-literal element removal. The mutants that would survive against the new milestone-1 tests are limited to "what if a *different* tab value was removed instead" — and those mutants are caught by the System Admin regression pin (M1.1: configuration-tab / database-tab / rbac-tab / demo-data-tab MUST still be admin-only) in the same test file. The test coverage on the contract is already tight by construction.

Recorded as a deliberate skip in `feature-delta.md`'s "Quality gates per phase" table.

### Three out-of-scope incidental commits left on the branch

Documented in `feature-delta.md`'s "Upstream Issues" section. They do not break this feature's gates but should not be claimed as part of api-keys delivery — see PR review recommendation below.

---

## Out-of-scope incidental commits (PR review recommendation)

During DELIVER, three commits landed on `feat/api-keys-for-all-users` that are not part of this feature's stated scope. They were created as side-effects by the crafter sub-agents (likely while running pre-commit hooks or staging adjacent uncommitted work).

| Commit | Recommendation |
|---|---|
| `37284d30` chore(.gitignore): add Stryker reports ignore rule | KEEP — trivial, low-risk, non-domain-specific project hygiene |
| `df824e60` test(system-info): mutation testing scope configs + optional-field tests | **CHERRY-PICK** to `system-info-auth-visibility` branch or open its own PR. Belongs to a different feature; should not be claimed as part of api-keys delivery |
| `c5578c23` chore(LogsController): remove unused authorization guard | **CHERRY-PICK** to a dedicated branch (`chore/logs-controller-rbac-relaxation` or similar). The commit message says "unused" but the `[RbacGuard]` was active — this is a permission relaxation similar in shape to this feature's change, but on a different controller. Security-sensitive; deserves its own justification and PR |

PR reviewer is asked to decide whether to rebase the api-keys PR to drop the two flagged commits, or to land them as-is with the proviso that follow-up cherry-picks land afterwards.

---

## Lessons captured for future features

1. **The test authentication infrastructure is the recurring scope wall.** The same pattern surfaced in `team-portfolio-creation-rights`: integration tests stop at the `WebApplicationFactory<Program>` boundary with a synthetic `ClaimsPrincipal`, and the OIDC token-introspection pipeline is only exercised by Playwright. This is fine when (a) the feature does not change the OIDC wiring, and (b) the Playwright `@rbac` suite is healthy. Both conditions hold for permission-relaxation features that flip a `useRbac()` consumer-side check. Future features that *do* touch the OIDC wiring (claim transformation, group-mapping resolution, emergency-admin detection) cannot accept this scope wall — they need a dedicated integration test that includes the real OIDC middleware against a Keycloak fixture.
2. **One-line production changes are normal when the backend already does the right thing.** The DISTILL wave correctly identified this. The DELIVER wave correctly resisted padding: zero refactor in Phase 3, zero mutation testing in Phase 5, a single character group removed in `Settings.tsx`. Resist the urge to manufacture work to "justify" a feature; the size of the production change is what it is.
3. **DELIVER step 01-01 (E2E assertion inversion) as deployment-safety pre-flight is a reusable pattern.** Landing the Playwright assertion BEFORE the production edit prevents the `@rbac` suite from going red in CI between commits. This costs nothing (the assertion is RED locally for the duration of one branch's worth of commits and is not part of the per-commit gate) and buys clean CI history. The acceptance-designer-reviewer flagged this as deployment-safety in the roadmap review notes — the reviewer was right.
4. **Wave-decision overrides need to be explicit, named, and trigger-anchored.** D4's "if keys become per-user in the future, revisit" was the explicit trigger. This feature cited the trigger by name. Future features overriding a wave decision should follow the same pattern: name the prior decision, quote the trigger condition (if any), and explain why the trigger has fired.
5. **Out-of-scope incidental commits are a recurring nWave hygiene issue.** Crafter sub-agents staging adjacent uncommitted work has produced contamination in at least three features now. Worth a follow-up to either (a) restrict crafter sub-agents to `git add <specific-paths>` only, or (b) have finalize flag the contamination explicitly so the PR-time reviewer can act on it (this finalize did).

---

## Links

- Feature workspace: `docs/feature/api-keys-for-all-users/`
- Feature delta SSOT: `docs/feature/api-keys-for-all-users/feature-delta.md`
- Acceptance scenarios: `docs/feature/api-keys-for-all-users/acceptance/*.feature`
- DELIVER roadmap: `docs/feature/api-keys-for-all-users/deliver/roadmap.json`
- DES audit log: `docs/feature/api-keys-for-all-users/deliver/execution-log.json`
- DELIVER commits: `b14ad8f2`, `8eadf300`, `e5c68d2a`, `edddc10a`
- Prior wave overridden: `docs/feature/rbac-ui-completeness/feature-delta.md` (D4, US-04 AC3)
- Related feature with same scope-wall lesson: `docs/evolution/` entries for `team-portfolio-creation-rights`
