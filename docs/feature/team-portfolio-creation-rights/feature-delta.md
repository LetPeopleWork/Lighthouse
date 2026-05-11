# Feature Delta: team-portfolio-creation-rights

<!-- markdownlint-disable MD024 MD041 -->

Wave: DISTILL | Date: 2026-05-11 | Density: lean (per ~/.nwave/global-config.json)

> **Note**: this feature was authored directly from a bug report. DISCUSS, DESIGN, and DEVOPS waves are skipped — the requirements are pre-known (matching the user's bug description), the design surface is fully constrained by the existing RBAC bounded context, and there is no new infrastructure or environment work. The DISTILL wave is the load-bearing artefact for this delivery: it is the single source of truth for the new behavioural contract and is what reconciles the contradiction with `rbac-enhancements/WD-03` and `rbac-ui-completeness/D8`.

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| n/a | Any System Admin can create teams and portfolios | n/a | Baseline RBAC contract; matches existing `RbacAdministrationService.CanCreateTeamAsync` / `CanCreatePortfolioAsync` service behaviour |
| n/a | Any user holding at least one Team Admin role (direct or group-derived) can create new teams | n/a | **Overrides** `rbac-enhancements/WD-03` (which restricted creation to System Admin only) and `rbac-ui-completeness/D8` (which tightened frontend semantics to require SystemAdmin). The new contract aligns the HTTP endpoint and frontend gating with what the service method already returns |
| n/a | Any user holding at least one Portfolio Admin role (direct or group-derived) can create new portfolios | n/a | Symmetric to team-admin rule above; same override semantics |
| n/a | A user who successfully creates a team becomes Team Admin of that team automatically | n/a | New behaviour. Required so that non-System-Admin creators retain the ability to manage what they just created. Symmetric for portfolios |
| `rbac-enhancements/WD-07` | Group-based rights behave identically to direct user rights | n/a | The two new requirements `CanCreateTeam` / `CanCreatePortfolio` must consult `GetEffectivePermissionsAsync`, which already merges direct and group-derived permissions — preserves the existing invariant |

---

## Wave: DISTILL / [REF] Wave-decision reconciliation

This feature **overrides** two prior wave decisions:

| Prior decision | Was | Now |
|---|---|---|
| `rbac-enhancements/WD-03` (2026-05-10) | `canCreateTeam/Portfolio = System Admin only (default)` | Inferred from existing scoped admin roles. Reason: real-world users with scoped admin responsibility need to create new entities without escalating every action to the System Admin. The "principle of least privilege" rationale of WD-03 is preserved because non-admins (Viewers) still cannot create |
| `rbac-enhancements/Q4` (2026-05-10) | `Default false for non-System-Admins. System Admins always true.` | Default still false for users with no admin role anywhere; true for any user holding at least one TeamAdmin or PortfolioAdmin role |
| `rbac-ui-completeness/D8` (2026-05-11) | `Frontend canCreateTeam/canCreatePortfolio semantics are tightened to require SystemAdmin` | Reverted. The backend service already returned the inferred value; D8 only patched the frontend to "match the broken HTTP gate" rather than fixing the gate. This feature fixes the gate and unrolls D8 |

**Open design question recorded for the user**: should team-creation and portfolio-creation be system-level rights that can be granted *independently* of scoped admin roles, rather than inferred? See `Open question — dedicated creation rights` below. The acceptance tests in this wave are written against the inferred-rights model (smallest blast radius, matches existing service code), with a clear migration path called out should the user choose the dedicated-right model in DISCUSS.

---

## Wave: DISTILL / [REF] Scenario list with tags

The executable acceptance scenarios are documented in `distill/team-portfolio-creation-rights.feature` and embodied as NUnit tests at:

- `Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs` (service-level)
- `Lighthouse.Backend.Tests/API/CreateRightsControllerGuardTest.cs` (controller-attribute contract)

| # | Scenario | Tags | Driving port | State |
|---|---|---|---|---|
| WS | Team Admin creates a team and is recorded as its administrator | `@walking_skeleton @real-io @driving_adapter` | `RbacAdministrationService.GrantCreatorTeamAdminAsync` + `CanSatisfyRequirementAsync(CanCreateTeam)` | RED |
| 1 | System Admin can create teams (summary) | `@summary @real-io` | `RbacAdministrationService.CanSatisfyRequirementAsync(CanCreateTeam)` | Existing GREEN (pinning) |
| 2 | System Admin can create portfolios (summary) | `@summary @real-io` | `RbacAdministrationService.CanSatisfyRequirementAsync(CanCreatePortfolio)` | Existing GREEN (pinning) |
| 3 | Team Admin can create teams (summary) | `@summary @real-io` | `RbacAdministrationService.CanSatisfyRequirementAsync(CanCreateTeam)` | RED |
| 4 | Portfolio Admin can create portfolios (summary) | `@summary @real-io` | `RbacAdministrationService.CanSatisfyRequirementAsync(CanCreatePortfolio)` | RED |
| 5 | Viewer cannot create teams (summary) | `@summary @real-io @error` | `RbacAdministrationService.CanSatisfyRequirementAsync(CanCreateTeam)` | Accidentally GREEN now (fall-through `_ => false`); remains GREEN after fix for the right reason |
| 6 | Viewer cannot create portfolios (summary) | `@summary @real-io @error` | `RbacAdministrationService.CanSatisfyRequirementAsync(CanCreatePortfolio)` | Accidentally GREEN now; remains GREEN after fix for the right reason |
| 7 | Group-derived TeamAdmin enables team creation | `@group_rights @real-io` | `RbacAdministrationService.CanSatisfyRequirementAsync` via virtual permissions | RED |
| 8 | Group-derived PortfolioAdmin enables portfolio creation | `@group_rights @real-io` | `RbacAdministrationService.CanSatisfyRequirementAsync` via virtual permissions | RED |
| 9 | `CreateTeam` controller uses the `CanCreateTeam` requirement | `@driving_adapter @real-io` | `[RbacGuard]` attribute on `TeamsController.CreateTeam` | RED |
| 10 | `CreatePortfolio` controller uses the `CanCreatePortfolio` requirement | `@driving_adapter @real-io` | `[RbacGuard]` attribute on `PortfoliosController.CreatePortfolio` | RED |
| 11 | `ValidateTeamSettings` controller uses the `CanCreateTeam` requirement | `@driving_adapter @real-io` | `[RbacGuard]` attribute on `TeamsController.ValidateTeamSettings` | RED |
| 12 | `ValidatePortfolioSettings` controller uses the `CanCreatePortfolio` requirement | `@driving_adapter @real-io` | `[RbacGuard]` attribute on `PortfoliosController.ValidatePortfolioSettings` | RED |
| 13 | Creator becomes TeamAdmin of the newly created team | `@auto_admin @real-io` | `RbacAdministrationService.GrantCreatorTeamAdminAsync` | RED |
| 14 | Creator becomes PortfolioAdmin of the newly created portfolio | `@auto_admin @real-io` | `RbacAdministrationService.GrantCreatorPortfolioAdminAsync` | RED |
| TM-1 | Creator-admin grant preserves existing TeamAdmin scopes (test-only invariant) | `@auto_admin @real-io` | `RbacAdministrationService.GrantCreatorTeamAdminAsync` | RED |
| TM-2 | Creator-admin grant preserves existing PortfolioAdmin scopes (test-only invariant) | `@auto_admin @real-io` | `RbacAdministrationService.GrantCreatorPortfolioAdminAsync` | RED |
| TM-3 | Creator-admin grant is idempotent — team (test-only invariant) | `@auto_admin @real-io` | `RbacAdministrationService.GrantCreatorTeamAdminAsync` | RED |
| TM-4 | Creator-admin grant is idempotent — portfolio (test-only invariant) | `@auto_admin @real-io` | `RbacAdministrationService.GrantCreatorPortfolioAdminAsync` | RED |

Rows 1–14 are Gherkin scenarios in `distill/team-portfolio-creation-rights.feature`. Rows TM-1–TM-4 are implementation-invariant tests in `CreateRightsAcceptanceTest.cs` that have no user-visible Gherkin form — they assert scope-preservation and idempotency properties of the grant methods. Counting in either currency: 14 Gherkin scenarios + 4 implementation-invariant tests = 18 assertions total.

**Counts**: 14 Gherkin scenarios + 4 invariant tests | 12 RED + 4 RED = 16 RED | 2 accidentally GREEN now (correct after fix).
**Error-path ratio (Gherkin)**: 2/14 = 14% (below 40% threshold — flagged; see note below).

> The 11% error-path ratio is below the 40% guideline. The justification: this is a focused bug fix with a single happy-path family (admit rights-holder, refuse non-rights-holder). Edge cases like "RBAC disabled" / "bootstrap mode" / "license gate" are already covered by the existing `RbacAdministrationServiceTest` suite (see `CanCreateTeamAsync_WhenRbacDisabled_ReturnsTrue`, `IsEnforcementGate_LicenseGate_FailsBeforeSystemAdminCheck`). Adding parallel tests here would duplicate coverage. The reviewer should weigh this trade-off; if rejected, the simplest remedy is to add explicit `@error` scenarios for RBAC-disabled and bootstrap modes against the two new `RbacGuardRequirement` values.

---

## Wave: DISTILL / [REF] Walking skeleton strategy

**Strategy C — Real local**: real `LighthouseAppContext` (EF Core in-memory provider), real `RbacAdministrationService`, real `RbacGuardRequirement` enum, real `UserPermission` / `RbacGroupMapping` persistence. Mocks only at the trust-boundary adapters (`ICurrentUserProfileService`, `ILicenseService`, `ICryptoService`, `ILogger`).

**Rationale for real adapters**: the bug at hand is precisely a wiring failure between layers — the service computes one thing, the HTTP gate enforces another, and persistence skips a step. Only real adapters can catch this. Mock-based tests are what allowed the gap to ship in `rbac-enhancements`.

**Walking skeleton scenario**: "Team Admin creates a team and is recorded as its administrator" — exercises the full inferred-rights pipeline (claim → effective permission lookup → guard evaluation → controller acceptance → entity persistence → auto-admin grant → permission row visible in DB).

**What the WS does NOT cover** (deliberate, documented in `Pre-requisites`):
- True HTTP integration with RBAC enforcement enabled. The existing `TestWebApplicationFactory` runs with `Authentication.Enabled = false`, which short-circuits `IsRbacEnforcedAsync` to `false`. Building a custom test auth handler that injects a controllable `ClaimsPrincipal` is out of scope for this feature and is deferred to a follow-up (see `Pre-requisites` row 1).
- The Lighthouse frontend gating change in `OverviewDashboard.tsx`. The backend HTTP contract is the SSOT; frontend follow-up is documented in `Pre-requisites` row 2.

---

## Wave: DISTILL / [REF] Adapter coverage table

| Adapter | `@real-io` scenario | Covered by |
|---|---|---|
| `LighthouseAppContext` (EF in-memory) | YES | All service-level scenarios — real DbContext, real EF queries, real persistence |
| `RbacGuardAttribute` (auth filter) | YES (via reflection on attribute requirement) | Scenarios 9, 10, 11, 12 — assert the attribute uses the new enum values; the runtime path through `CanSatisfyRequirementAsync` is covered by scenarios 3, 4, 7, 8 |
| `RbacGroupMappings` (group resolution) | YES | Scenarios 7, 8 — real group claim parsing, real DB lookup of `RbacGroupMapping` rows, real merge in `GetEffectivePermissionsAsync` |
| `UserPermissions` (persistence) | YES | Scenarios 13–18 — real `context.UserPermissions` writes verified by direct query |
| `TeamsController.CreateTeam` HTTP entry | PARTIAL — covered by attribute inspection (scenario 9). Full HTTP integration deferred to follow-up | See Pre-requisites row 1 |
| `PortfoliosController.CreatePortfolio` HTTP entry | PARTIAL — covered by attribute inspection (scenario 10) | See Pre-requisites row 1 |

No "NO — MISSING" rows. Two PARTIAL rows are deliberate trade-offs documented in Pre-requisites.

---

## Wave: DISTILL / [REF] Scaffolds

Mandate 7 (RED-Ready Scaffolding) compliance:

| Scaffolded artefact | File | Marker | Failure mode |
|---|---|---|---|
| `RbacGuardRequirement.CanCreateTeam` | `Lighthouse.Backend/Models/Authorization/RbacGuardRequirement.cs` | `// SCAFFOLD (feature: team-portfolio-creation-rights)` comment | Falls through `_ => false` in `CanSatisfyRequirementAsync` switch — assertion-level RED |
| `RbacGuardRequirement.CanCreatePortfolio` | (same) | (same) | (same) |
| `IRbacAdministrationService.GrantCreatorTeamAdminAsync` | `Lighthouse.Backend/Services/Interfaces/Authorization/IRbacAdministrationService.cs` | `// SCAFFOLD` comment on interface | Implementation throws `InvalidOperationException("__SCAFFOLD__ …")` — RED |
| `IRbacAdministrationService.GrantCreatorPortfolioAdminAsync` | (same) | (same) | (same) |
| `RbacAdministrationService.GrantCreatorTeamAdminAsync` (impl stub) | `Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs` | `// SCAFFOLD` comment + `__SCAFFOLD__` token in exception message | Throws — RED |
| `RbacAdministrationService.GrantCreatorPortfolioAdminAsync` (impl stub) | (same) | (same) | (same) |

**Verification**: `dotnet test --filter "FullyQualifiedName~CreateRightsAcceptanceTest|FullyQualifiedName~CreateRightsControllerGuardTest"` reports `Failed: 16, Passed: 2, Total: 18`. All 16 failures are assertion-level (RED), zero are infrastructure-level (BROKEN). The 2 passing tests are the Viewer-refusal cases that pass via the switch fall-through; they will remain GREEN after the implementation is wired correctly.

**Scaffold removal evidence** for DELIVER: `grep -rn "__SCAFFOLD__\|SCAFFOLD (feature: team-portfolio-creation-rights)" Lighthouse.Backend/` should return zero matches after implementation.

---

## Wave: DISTILL / [REF] Test placement

```
Lighthouse.Backend/
  Lighthouse.Backend.Tests/
    API/
      CreateRightsControllerGuardTest.cs          ← controller-attribute contract (4 tests)
    Services/Implementation/Authorization/
      CreateRightsAcceptanceTest.cs               ← service-level acceptance (14 tests)
docs/feature/team-portfolio-creation-rights/
  feature-delta.md                                ← this file (DISTILL SSOT)
  distill/
    team-portfolio-creation-rights.feature        ← Gherkin documentation (18 scenarios)
```

**Conventions followed**: NUnit `[TestFixture]` / `[Test]` matching the rest of `Lighthouse.Backend.Tests`. Moq matching the rest of the suite (the CLAUDE.md note on "xUnit + NSubstitute" is stale for this project — the backend tests are NUnit + Moq). EF in-memory DbContext per the `RbacAdministrationServiceTest` pattern. No new TestHelpers needed.

---

## Wave: DISTILL / [REF] Driving adapter coverage

The feature's driving adapters are the two HTTP endpoints `POST /api/latest/teams` and `POST /api/latest/portfolios`. Coverage strategy:

- **Attribute-level contract** (scenarios 9–12): assert via reflection that the `[RbacGuard]` attribute on each `Create*` and `Validate*` method points at the new requirement. This catches the wiring bug (the original `[RbacGuard(SystemAdmin)]` mismatch) at compile-adjacent speed.
- **Runtime behaviour** (scenarios 3, 4, 7, 8): assert via direct `CanSatisfyRequirementAsync` calls that the new requirement values resolve correctly for every persona — System Admin, Team Admin (direct), Team Admin (via group), Portfolio Admin (direct), Portfolio Admin (via group), Viewer.

Combined, these two layers prove that the runtime path used by `RbacGuardAttribute.OnAuthorizationAsync` (which calls `CanSatisfyRequirementAsync`) admits the correct callers for the correct endpoints. The gap — true HTTP-level integration with RBAC enforcement enabled — is documented in Pre-requisites and is a follow-up.

---

## Wave: DISTILL / [REF] Pre-requisites

| Requirement | Description | Blocks |
|---|---|---|
| Test auth handler with controllable identity | Add a test-only `AuthenticationHandler` that injects a `ClaimsPrincipal` per test (Subject, claims, group claim). Required to write full-HTTP integration tests for RBAC enforcement. Deferred to a follow-up feature; not blocking this delivery | Future `@driving_adapter` HTTP-level acceptance |
| Frontend `OverviewDashboard.tsx` gating | Change the Add Team / Add Portfolio button visibility from `rbac.isSystemAdmin` to `rbac.canCreateTeam` / `rbac.canCreatePortfolio`. The backend already returns the correct flags in `UserAuthorizationSummary`; this is a one-line frontend change per button. The DELIVER wave will pick this up as part of the same feature delivery | Frontend usability for non-System-Admin creators |
| `rbac-ui-completeness/D8` rollback | The `rbac-ui-completeness` feature's D8 tightened `canCreateTeam`/`canCreatePortfolio` to require `isSystemAdmin` on the frontend. That tightening must be unrolled as part of DELIVER for this feature. The crafter should grep `canCreateTeam` / `canCreatePortfolio` in the frontend, expand any AND-with-`isSystemAdmin` gates, and back-propagate to that feature delta | DELIVER must touch both feature deltas |
| `RbacGuard` requirement scoping | `RbacGuard.RequiresScope()` returns false for the two new enum values (system-level grants like `SystemAdmin`). The DELIVER implementation of `CanSatisfyRequirementAsync` must NOT require a scopeId for `CanCreateTeam` / `CanCreatePortfolio`. The interface signature for `CanSatisfyRequirementAsync` already accepts `int? scopeId = null`, so no breaking change | Implementation of the two new switch arms |

---

## Wave: DISTILL / [REF] Open question — dedicated creation rights

The user explicitly raised this in the bug report:

> "However, we can discuss if there should be a dedicated right on system level for team and portfolio creation instead of inferring the rights. That may make more sense. It could be handled next to the system admin to have rights to create teams and portfolios."

Two options remain on the table:

- **Option A — Inferred (what these scenarios assert)**: a user has `canCreateTeam` iff they hold at least one `TeamAdmin` role anywhere (direct or group-derived) OR they are a System Admin. Symmetric for portfolios. Smallest blast radius — the backend service method already computes this, the only changes needed are the new `RbacGuardRequirement` values + controller attribute swap + auto-admin grant + frontend gating change.
- **Option B — Dedicated**: introduce two new system-level roles (`TeamCreator`, `PortfolioCreator`) or two new permission flags that can be assigned to a `UserProfile` or `RbacGroupMapping` independently of TeamAdmin/PortfolioAdmin scoped roles. Larger change — needs schema migration, new role enum values, new RBAC admin UI to grant/revoke, new group-mapping scope type.

**Recommended path**: ship Option A first (closes the immediate bug). Re-evaluate after one release cycle whether the audit/compliance value of Option B (granular assignment) is worth the implementation cost. If the answer is yes, Option B becomes a follow-up feature `team-portfolio-creation-system-rights` and the scenarios in this feature port to that feature's tests with minimal change — only the seed data changes (grant a `TeamCreator` role instead of a `TeamAdmin` role on a specific team).

**No DELIVER blocking decision needed for this question** — the user can choose later. The scenarios in this feature are written against the contract described in their bug report, which matches Option A.

---

## Wave: DISTILL / [REF] Back-propagation notes

Two prior feature deltas should be annotated in DELIVER:

1. `docs/feature/rbac-enhancements/feature-delta.md` — `WD-03` and `Q4` rows should reference this feature as the override. Suggested annotation:
   > **Superseded 2026-05-11 by `team-portfolio-creation-rights`** — the inferred-rights model in `RbacAdministrationService.CanCreateTeamAsync` / `CanCreatePortfolioAsync` was always the service-level truth; this feature aligns the HTTP gate and frontend gating with it.
2. `docs/feature/rbac-ui-completeness/feature-delta.md` — `D8` row should reference this feature as the rollback. Suggested annotation:
   > **Reverted 2026-05-11 by `team-portfolio-creation-rights`** — frontend gating now uses `rbac.canCreateTeam` / `rbac.canCreatePortfolio` directly, not AND-with-`isSystemAdmin`.

DELIVER picks these up as part of the same change so the deltas stay coherent across the project.
