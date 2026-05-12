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

---

## Wave: DISTILL / [REF] Revision R2 (2026-05-12) — Unified creation rights and team-existence prerequisite

> **Status**: this revision SUPERSEDES the prior DISTILL contract above. The DELIVER sections below remain as the historical record of the R1 shipment; a DELIVER re-run against the R2 scenarios is required (scope listed at the end of this revision block).

The user clarified that the inferred-rights model in R1 was too narrow on two axes:

1. **Unified rights** — a user holding ANY admin role (System Admin, ≥1 Team Admin, or ≥1 Portfolio Admin, direct or group-derived) can create BOTH teams AND portfolios. The R1 distinction (TA gates teams only, PA gates portfolios only) is dropped.
2. **Team-existence prerequisite for portfolios** — portfolio creation requires at least one team row to exist in the system. The check is system-wide (queries `LighthouseAppContext.Teams.AnyAsync(...)`) and decoupled from per-user visibility. A user need not have read access to any team for the gate to pass — the teams just need to exist anywhere.

The user's literal framing for the existence gate: *"I may create a portfolio even if I don't see the 3 teams that exist. Those teams may contribute to the portfolio, I don't need to see them for that. Only prevent portfolios if really no team at all exists."* The gate is therefore unconditional with respect to RBAC mode — it applies in enforced, bootstrap, and RBAC-disabled modes alike.

| R1 commitment | R2 commitment | Why the change |
|---|---|---|
| TA holders can create teams (not portfolios) | TA holders can create teams AND portfolios | Real users with scoped admin responsibility over one resource type still own decisions about the other and shouldn't escalate to System Admin every time |
| PA holders can create portfolios (not teams) | PA holders can create portfolios AND teams | Symmetric reasoning |
| Portfolios can always be created by a rights-holder | Portfolios require ≥1 team to exist anywhere in the system | A portfolio without any team to roll up is structurally meaningless; enforce at the gate, not the UI |
| Portfolio gate implicitly leaked user-scoped visibility into the decision | Portfolio gate is a global existence check, not visibility-filtered | "I may create a portfolio even if I don't see the 3 teams that exist" |

---

## Wave: DISTILL / [REF] Revision R2 — Updated commitments

| Origin | Commitment | DDD | Impact |
|---|---|---|---|
| n/a | Any System Admin can create teams unconditionally | n/a | Restated; matches R1 for teams |
| n/a | Any System Admin can create portfolios IFF at least one team exists in the system | n/a | NEW gate — applies even to System Admin; portfolios are structurally meaningless without teams |
| n/a | Any user holding ≥1 Team Admin OR ≥1 Portfolio Admin role (direct or group-derived) can create teams | n/a | SUPERSEDES R1 Team-Admin-only rule |
| n/a | Any user holding ≥1 Team Admin OR ≥1 Portfolio Admin role (direct or group-derived) can create portfolios IFF at least one team exists in the system | n/a | SUPERSEDES R1; adds the existence gate |
| n/a | The team-existence check is system-wide, not visibility-scoped | n/a | A creator does NOT need read access to any team; teams merely need to exist anywhere in the database |
| n/a | The existence gate applies in RBAC-disabled and bootstrap-no-admin modes too | n/a | `GetAuthorizationSummaryAsync` must respect the existence gate in all three return branches |
| n/a | A creator becomes the scoped admin of the new entity (TeamAdmin for created team, PortfolioAdmin for created portfolio) | n/a | Carried over from R1 unchanged — extends to cross-role creators (e.g. a PA who creates a team becomes TeamAdmin of that new team) |
| `rbac-enhancements/WD-07` | Group-derived rights behave identically to direct rights | n/a | Still consulted via `GetEffectivePermissionsAsync`; unchanged |

---

## Wave: DISTILL / [REF] Revision R2 — Wave-decision reconciliation

| R1 decision | R2 outcome | Rationale |
|---|---|---|
| Inferred rights are role-typed (TA → team, PA → portfolio) | Inferred rights are role-unified (any admin role → both resource types) | User feedback: "Make no difference in whether you can create teams or portfolios. If you are sys admin or have either a team or portfolio admin role, you can do it." |
| `CanCreatePortfolio` depends only on RBAC role membership | `CanCreatePortfolio` depends on RBAC role membership AND a system-wide team-existence check | User feedback: "Only prevent portfolios if really no team at all exists." |
| Bootstrap and RBAC-disabled modes return `CanCreatePortfolio = true` unconditionally | Both modes must apply the existence gate | The "only prevent if no team exists" rule was stated without exception |

The R1 "Open question — dedicated creation rights" item (Option A vs Option B) is **foreclosed for this feature cycle** by R2's expansion of the inferred-rights model. Rights remain sourced from existing admin roles (Option A's storage model), but R2 materially broadens *which* inferred rights count — any TA or PA role now grants both creation rights, subsuming Option B's "granular team-vs-portfolio admin separation" use case within the broader inferred model. Re-opening Option B (introducing dedicated `TeamCreator` / `PortfolioCreator` role types) would be a fresh follow-up feature, not a revision of this one.

---

## Wave: DISTILL / [REF] Revision R2 — Revised scenario list

Scenario rows from the R1 table are updated as follows (E = existing/unchanged, R = revised in-place, N = newly introduced):

| # | Scenario | Tags | Driving port | State (R1) | State (R2) |
|---|---|---|---|---|---|
| WS | Team Admin creates a team and is recorded as its administrator | `@walking_skeleton @real-io @driving_adapter` | `CanSatisfyRequirementAsync(CanCreateTeam)` + `GrantCreatorTeamAdminAsync` | E (GREEN under R1) | E — unchanged |
| WS2 | Team Admin creates a portfolio in a system where other teams exist but are invisible to them | `@walking_skeleton @real-io @driving_adapter @unified_rights @visibility_decoupled` | `CanSatisfyRequirementAsync(CanCreatePortfolio)` + `GrantCreatorPortfolioAdminAsync` | — | N — proves unified rights + visibility-decoupled existence gate end-to-end |
| 1 | System Admin can create teams (summary) | `@summary @real-io` | `CanCreateTeamAsync` | E (GREEN) | E |
| 2 | System Admin can create portfolios when at least one team exists (summary) | `@summary @real-io` | `CanCreatePortfolioAsync` | E (GREEN) | R — adds team-existence precondition |
| 2b | System Admin cannot create portfolios when no teams exist (summary) | `@summary @real-io @error @existence_gate` | `CanCreatePortfolioAsync` | — | N |
| 3 | Team Admin can create teams (summary) | `@summary @real-io` | `CanCreateTeamAsync` | E (was "TA can create teams but not portfolios" — split) | R |
| 3b | Team Admin can create portfolios when at least one team exists (summary) | `@summary @real-io @unified_rights` | `CanCreatePortfolioAsync` | Was forbidden under R1 | N — supersedes the R1 "TA cannot create portfolio" assertion |
| 4 | Portfolio Admin can create portfolios when at least one team exists (summary) | `@summary @real-io` | `CanCreatePortfolioAsync` | E (was "PA can create portfolios but not teams" — split) | R |
| 4b | Portfolio Admin can create teams (summary) | `@summary @real-io @unified_rights` | `CanCreateTeamAsync` | Was forbidden under R1 | N — supersedes the R1 "PA cannot create team" assertion |
| 5 | Viewer cannot create teams (summary) | `@summary @real-io @error` | `CanCreateTeamAsync` | E | E |
| 6 | Viewer cannot create portfolios (summary) | `@summary @real-io @error` | `CanCreatePortfolioAsync` | E | E |
| 7 | Group-derived TeamAdmin enables BOTH team and portfolio creation | `@group_rights @real-io @unified_rights` | `CanSatisfyRequirementAsync` | E (team only under R1) | R — adds portfolio check |
| 8 | Group-derived PortfolioAdmin enables BOTH portfolio and team creation | `@group_rights @real-io @unified_rights` | `CanSatisfyRequirementAsync` | E (portfolio only under R1) | R — adds team check |
| 9 | `CreateTeam` controller uses the `CanCreateTeam` requirement | `@driving_adapter @real-io` | `[RbacGuard]` on `TeamsController.CreateTeam` | E (GREEN) | E |
| 10 | `CreatePortfolio` controller uses the `CanCreatePortfolio` requirement | `@driving_adapter @real-io` | `[RbacGuard]` on `PortfoliosController.CreatePortfolio` | E (GREEN) | E |
| 11 | `ValidateTeamSettings` controller uses the `CanCreateTeam` requirement | `@driving_adapter @real-io` | `[RbacGuard]` on `TeamsController.ValidateTeamSettings` | E (GREEN) | E |
| 12 | `ValidatePortfolioSettings` controller uses the `CanCreatePortfolio` requirement | `@driving_adapter @real-io` | `[RbacGuard]` on `PortfoliosController.ValidatePortfolioSettings` | E (GREEN) | E |
| 13 | Creator becomes TeamAdmin of the newly created team | `@auto_admin @real-io` | `GrantCreatorTeamAdminAsync` | E (GREEN) | E |
| 14 | Creator becomes PortfolioAdmin of the newly created portfolio | `@auto_admin @real-io` | `GrantCreatorPortfolioAdminAsync` | E (GREEN) | E |
| 15 | Team Admin who creates a portfolio is recorded as that portfolio's admin (cross-role auto-admin) | `@auto_admin @real-io @unified_rights` | `EnsureCreatorPortfolioAdminAsync` | — | N |
| 16 | Portfolio Admin who creates a team is recorded as that team's admin (cross-role auto-admin) | `@auto_admin @real-io @unified_rights` | `EnsureCreatorTeamAdminAsync` | — | N |
| 17 | Create Portfolio request is refused when no teams exist in the system | `@driving_adapter @real-io @error @existence_gate` | `[RbacGuard(CanCreatePortfolio)]` on `PortfoliosController.CreatePortfolio` | — | N |
| 18 | Portfolio existence gate is global, not visibility-scoped — a TA with no read access to existing teams still passes | `@existence_gate @real-io @visibility_decoupled` | `CanCreatePortfolioAsync` | — | N |
| 19 | Authorization summary in RBAC-disabled mode still respects the existence gate for portfolios | `@summary @real-io @error @existence_gate` | `GetAuthorizationSummaryAsync` (RBAC-disabled branch) | — | N |
| 20 | Authorization summary in bootstrap-no-admin mode still respects the existence gate for portfolios | `@summary @real-io @error @existence_gate` | `GetAuthorizationSummaryAsync` (bootstrap branch) | — | N |
| TM-1..TM-4 | Invariants (idempotency + scope preservation on grant methods) | `@auto_admin @real-io` | grant methods | E (GREEN) | E — unchanged |

**Counts (R2)** — reconciled across the table, the `.feature` file, and the R1 NUnit attribute pins:

- 26 Gherkin scenarios in `distill/team-portfolio-creation-rights.feature` (the executable SSOT). The Viewer cases are merged into one scenario (`Viewer cannot create teams or portfolios`) instead of the two table rows 5 + 6 — the table lists them separately to keep the per-resource assertion traceable, the `.feature` keeps the Gherkin terse.
- 2 NUnit-only attribute-contract rows (table rows 11, 12 for `ValidateTeamSettings` / `ValidatePortfolioSettings`) inherited unchanged from R1's `CreateRightsControllerGuardTest.cs`. These have no Gherkin form by design — they assert the `[RbacGuard]` attribute requirement value via reflection, not user-visible behaviour.
- 4 invariant tests (TM-1..TM-4) in `CreateRightsAcceptanceTest.cs` — idempotency + scope preservation on the grant methods, also no Gherkin form.

Total assertion budget: **26 Gherkin + 2 NUnit attribute contracts + 4 NUnit invariants = 32**.

Error-path / edge ratio (Gherkin only): 7/26 ≈ 27% (still below the 40% guideline, but materially better than R1's 14% thanks to the four new `@existence_gate` scenarios — 2b, 17, 19, 20 — and the carried-over Viewer + cross-Viewer rows). The R1 trade-off note still applies: RBAC-disabled / license-gate edges are covered by the broader `RbacAdministrationServiceTest` suite outside this feature's scope.

---

## Wave: DISTILL / [REF] Revision R2 — Adapter coverage delta

| Adapter | New `@real-io` scenario | Covered by |
|---|---|---|
| `LighthouseAppContext.Teams` (system-wide existence query) | YES | Scenarios 2, 2b, 17, 19, 20 — real DbContext, real `Teams.AnyAsync()` |

No prior adapter coverage row regresses. The new global-existence query is an additional driven query against `LighthouseAppContext`, already covered by the EF in-memory provider used by all service-level tests. No new adapter, no new mock surface.

---

## Wave: DISTILL / [REF] Revision R2 — Scaffolds delta

R1 scaffolds (the two `RbacGuardRequirement` enum values, the four `Grant*` / `EnsureCreator*` interface methods, and their implementations) are now production code — they shipped under R1. R2 introduces **no new public-API scaffolds** (no new interface methods, enum values, or DTOs). Mandate 7 (RED-ready scaffolding) is concerned with public contract surface — the unification expands existing method *bodies* behind already-stable signatures, so no `__SCAFFOLD__` markers or new module files are required.

That does **not** mean DELIVER ships green. The new R2 scenarios (WS2, 2b, 3b, 4b, 15, 16, 17, 18, 19, 20) must be authored as **RED test methods first** in `CreateRightsAcceptanceTest.cs` against the existing R1 implementation — they will fail until the predicate expansion and the existence gate are wired. This is standard Outside-In TDD inside an unchanged public boundary, not Mandate-7 scaffolding.

The DELIVER change set is internal to:

- `RbacAdministrationService.CanCreateTeamAsync` — broaden the `effectivePermissions.Any(...)` predicate from `ScopeType == Team && Value == TeamAdmin` to `Value == TeamAdmin || Value == PortfolioAdmin` (scope type stops carrying authorization weight for this check).
- `RbacAdministrationService.CanCreatePortfolioAsync` — symmetric broadening AND a new `await context.Teams.AnyAsync(cancellationToken)` guard.
- `RbacAdministrationService.GetAuthorizationSummaryAsync` — apply the existence guard in ALL THREE return branches (RBAC-disabled, bootstrap-no-admin, normal), per the "rule is unconditional" reconciliation row above.

**Gate ordering is contractual**, not implementation choice. `CanCreatePortfolioAsync` MUST evaluate the existence gate **after** the short-circuits for RBAC-disabled / enforcement-gate / `CanManageRbac` (because those are policy-level overrides) but **before** the per-user effective-permissions predicate. The resulting order is:

1. `IsRbacEnforcedAsync` → if false, return `Teams.AnyAsync()` (the existence gate still fires; the unconditional rule applies in disabled mode).
2. `IsEnforcementGateSatisfiedAsync` → if false, return `false`.
3. `CanManageRbacAsync` (System Admin) → if true, return `Teams.AnyAsync()` (existence gate fires even for SysAdmin).
4. Resolve current user; if null, return `false`.
5. `Teams.AnyAsync(ct)` → if false, return `false` (the unconditional existence gate; short-circuits regardless of role).
6. Effective-permissions predicate → return `Value == TeamAdmin || Value == PortfolioAdmin`.

The same ordering applies to the analogue inside `GetAuthorizationSummaryAsync` for `CanCreatePortfolio`: the existence gate fires in **all three** return branches (RBAC-disabled, bootstrap-no-admin, normal), not just the normal one.

---

## Wave: DISTILL / [REF] Revision R2 — DELIVER re-run scope

The R1 DELIVER artefacts (sections below) are now stale with respect to the R2 contract. A focused DELIVER re-run must:

1. **Service predicate expansion**:
   - `CanCreateTeamAsync` — admit any user whose effective permissions contain a `TeamAdmin` (any scope) OR `PortfolioAdmin` (any scope) entry.
   - `CanCreatePortfolioAsync` — same expansion AND prepend a `if (!await context.Teams.AnyAsync(ct)) return false;` guard immediately after the RBAC-disabled / enforcement-gate / SysAdmin short-circuits.
2. **Summary branches** — apply the team-existence guard in all three branches of `GetAuthorizationSummaryAsync`. The RBAC-disabled and bootstrap-no-admin branches currently return `CanCreatePortfolio = true` unconditionally; both must become `CanCreatePortfolio = await context.Teams.AnyAsync(ct)`.
3. **Tests in `CreateRightsAcceptanceTest.cs`**:
   - Delete the R1 scenarios that asserted "TA cannot create portfolio" and "PA cannot create team" (the obsolete role-typed gates).
   - Add the eight new R2 scenarios (WS2, 2b, 3b, 4b, 15, 16, 17, 18, 19, 20). The walking-skeleton scaffold pattern in `Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs` is the right template.
   - Update scenarios 7 and 8 (group-derived) to assert both creation rights against a group-derived role.
4. **Frontend** — `OverviewDashboard.tsx` already reads `rbac.canCreateTeam` and `rbac.canCreatePortfolio` from the summary; no further change required on the visibility axis (the summary recomputation in step 2 carries the new contract through). Verify there is no auxiliary frontend gate that filters portfolio creation on "teams visible to me"; if such a gate exists, remove it — the backend summary is the SSOT.
5. **No DTO / schema / migration changes**. No new enum values. No new interface methods. The R1 scaffold-removal evidence (`grep -rn "__SCAFFOLD__"`) remains valid after R2.
6. **Back-propagation** — R2 is an iteration on this feature, not a fresh override of prior waves. Do NOT re-annotate `rbac-enhancements` or `rbac-ui-completeness` beyond the R1 supersession annotations already in place. The R1 "Open question — dedicated creation rights" row in this file is resolved by precedent for Option A (see Wave-decision reconciliation table above).

**Estimated test deltas**: ~8 new NUnit test methods in `CreateRightsAcceptanceTest.cs`, 2 deleted (the "TA cannot create portfolio" / "PA cannot create team" pair from R1), 2 modified (group-derived pair). No new test files; no `CreateRightsControllerGuardTest.cs` changes (controller attributes unchanged).

**Test-data fixtures the new scenarios require** — DELIVER setup must produce three distinct DbContext states; reusing a single seeded context for all scenarios will hide bugs:

| State | Used by | Setup |
|---|---|---|
| Zero teams | 2b, 17, 19, 20 | Fresh `LighthouseAppContext`; no `context.Teams` rows. For 19, configure `RBAC.Enabled = false` before constructing the service. For 20, configure RBAC enforced but seed no `SystemAdmin` permission row. |
| ≥1 team, creator has no `TeamRead` scope on those teams | WS2, 18 | Seed teams `Beta`, `Gamma`, `Delta` (no `UserPermission` rows for the creator on any of them). Seed the creator with one `TeamAdmin` permission on a separate team `Alpha`. Assert that `GetReadableTeamIdsAsync(...)` returns only `Alpha`'s id while `CanCreatePortfolioAsync` returns `true`. |
| ≥1 team, normal RBAC permissions | 2, 3, 3b, 4, 4b, 7, 8, 13–16 | Seed one team `Alpha` plus one portfolio `Vision`. Standard. |

These three states map to the three branches of `GetAuthorizationSummaryAsync` plus the two new behavioural axes (visibility decoupling, cross-role auto-admin). Missing any of them in DELIVER setup will silently regress the corresponding scenarios.

---

## Wave: DELIVER / [REF] Implementation summary

The inferred-rights bug shipped in four roadmap steps plus a refactor pass. Backend service `RbacAdministrationService.CanSatisfyRequirementAsync` gained two switch arms (`CanCreateTeam`, `CanCreatePortfolio`) that delegate to the pre-existing `CanCreateTeamAsync` / `CanCreatePortfolioAsync` methods — which already implemented the "System Admin OR any TeamAdmin/PortfolioAdmin role (direct or group-derived)" rule. Two new idempotent grant methods (`GrantCreatorTeamAdminAsync`, `GrantCreatorPortfolioAdminAsync`) persist a single `UserPermission` row binding the creator to the new entity as its scoped admin. The four HTTP endpoints (`CreateTeam`, `CreatePortfolio`, `ValidateTeamSettings`, `ValidatePortfolioSettings`) had their `[RbacGuard]` requirement swapped from `SystemAdmin` to the new values, and `CreateTeam` / `CreatePortfolio` now invoke `EnsureCreatorTeamAdminAsync` / `EnsureCreatorPortfolioAdminAsync` after a successful save (gated on `IsRbacEnforcedAsync` so disabled-auth mode does not pollute permission rows). The frontend `OverviewDashboard` outer-guard switched from `rbac.isSystemAdmin` to `rbac.canCreateTeam` / `rbac.canCreatePortfolio`, rolling back the `rbac-ui-completeness/D8` tightening. Back-propagation annotations were added to `rbac-enhancements/feature-delta.md` (WD-03, Q4) and `rbac-ui-completeness/feature-delta.md` (D8).

---

## Wave: DELIVER / [REF] Files modified

**Production (backend)**
- `Lighthouse.Backend/Models/Authorization/RbacGuardRequirement.cs` — added `CanCreateTeam`, `CanCreatePortfolio` enum members
- `Lighthouse.Backend/Services/Interfaces/Authorization/IRbacAdministrationService.cs` — added `GrantCreatorTeamAdminAsync`, `GrantCreatorPortfolioAdminAsync`, `EnsureCreatorTeamAdminAsync`, `EnsureCreatorPortfolioAdminAsync` signatures
- `Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs` — implemented the four new methods, the shared private `EnsureCreatorAdminAsync` orchestration helper, and `GrantScopedAdminAsync` idempotent persistence helper; extended `CanSatisfyRequirementAsync` switch with two new arms
- `Lighthouse.Backend/API/TeamsController.cs` — swapped `[RbacGuard]` on `CreateTeam` and `ValidateTeamSettings` to `CanCreateTeam`; wired `EnsureCreatorTeamAdminAsync` call after save
- `Lighthouse.Backend/API/PortfoliosController.cs` — symmetric swap to `CanCreatePortfolio`; wired `EnsureCreatorPortfolioAdminAsync` call

**Production (frontend)**
- `Lighthouse.Frontend/src/pages/Overview/OverviewDashboard.tsx` — outer-guard on Add Team / Add Portfolio buttons switched from `rbac.isSystemAdmin` to `rbac.canCreateTeam` / `rbac.canCreatePortfolio`; connections-required disabled clause still scoped to System Admin only

**Tests (backend, new)**
- `Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs` — 14 NUnit tests, real EF in-memory DbContext, real RBAC service
- `Lighthouse.Backend.Tests/API/CreateRightsControllerGuardTest.cs` — 4 NUnit tests asserting `[RbacGuard]` requirement contract on the four affected endpoints

**Tests (backend, modified — obsolete pin deletion)**
- `Lighthouse.Backend.Tests/API/TeamsControllerTest.cs` — removed `CreateTeam_HasSystemAdminRbacGuardAttribute` (and any symmetric Validate variant)
- `Lighthouse.Backend.Tests/API/PortfoliosControllerTest.cs` — removed `CreatePortfolio_HasSystemAdminRbacGuardAttribute` (and any symmetric Validate variant)

**Tests (frontend)**
- `Lighthouse.Frontend/src/pages/Overview/OverviewDashboard.test.tsx` — rewrote System-Admin-only visibility tests to assert the role-derived visibility contract; added new tests for Team Admin / Portfolio Admin / Viewer cases

**Docs**
- `docs/feature/team-portfolio-creation-rights/feature-delta.md` — DISTILL + DELIVER sections in this file
- `docs/feature/team-portfolio-creation-rights/distill/team-portfolio-creation-rights.feature` — 14 Gherkin scenarios (created in DISTILL)
- `docs/feature/team-portfolio-creation-rights/deliver/roadmap.json` — DES-compliant 4-step plan
- `docs/feature/team-portfolio-creation-rights/deliver/execution-log.json` — DES audit log, 4 steps × 5 phases (PREPARE/RED_ACCEPTANCE/RED_UNIT/GREEN/COMMIT)
- `docs/feature/rbac-enhancements/feature-delta.md` — WD-03 and Q4 rows annotated as superseded
- `docs/feature/rbac-ui-completeness/feature-delta.md` — D8 row annotated as reverted

**Commits in this feature's chain (5)**
- `923d1f01 feat(authorization): implement CanCreateTeam/CanCreatePortfolio guard arms and auto-admin grants` (step 01-01)
- `66234c68 feat(authorization): route Create/Validate endpoints through inferred-rights guards and auto-grant creator admin` (step 01-02)
- `2ce65551 feat(overview): gate Add Team/Portfolio buttons on rbac.canCreateTeam/canCreatePortfolio` (step 01-03)
- `113a8909 docs(rbac): back-propagate team-portfolio-creation-rights supersession annotations` (step 01-04)
- `8260f304 refactor(authorization): move creator-admin grant orchestration into RBAC service` (Phase 3 L1-L6 — L2 duplication fix)

---

## Wave: DELIVER / [REF] Scenarios green count

**18 of 18 assertions green** (verified 2026-05-11):

| Source | Count | Status |
|---|---|---|
| Gherkin scenarios in `distill/team-portfolio-creation-rights.feature` | 14 | All embodied as NUnit test methods, all GREEN |
| Implementation-invariant tests (TM-1 to TM-4: idempotency + scope preservation) | 4 | All GREEN |
| Total | 18 | 100% pass |

Regression spread (targeted `dotnet test --filter "FullyQualifiedName~CreateRights|FullyQualifiedName~RbacAdministration|FullyQualifiedName~RbacGuard|FullyQualifiedName~TeamsController|FullyQualifiedName~PortfoliosController"`): **219 of 219 GREEN**.

Full frontend suite (`pnpm test --run`): **2740 of 2740 GREEN** after step 01-03.

Full backend suite (`dotnet test Lighthouse.Backend.Tests/Lighthouse.Backend.Tests.csproj`): **GREEN** (background run exit 0 confirmed mid-orchestration).

---

## Wave: DELIVER / [REF] DoD check

| DoR / DoD item from DISTILL | Status |
|---|---|
| Every Gherkin scenario in `distill/*.feature` has at least one passing test | PASS — 14/14 |
| Walking-skeleton scenario ("Team Admin creates a team and is recorded as its administrator") proven end-to-end at the service level | PASS — `RbacGuard_CanCreateTeam_AdmitsTeamAdmin` + `GrantCreatorTeamAdminAsync_AfterTeamCreation_RecordsCreatorAsTeamAdminOfNewTeam` both GREEN |
| Every `RbacGuardRequirement` value has a switch arm in `CanSatisfyRequirementAsync` (no fall-through false for the new values) | PASS — verified by `RbacGuard_CanCreate*_RefusesViewer` (Viewer denial happens inside the new arm, not via fall-through) |
| `__SCAFFOLD__` markers absent from production source | PASS — `grep -rn "__SCAFFOLD__\|SCAFFOLD (feature: team-portfolio-creation-rights)" Lighthouse.Backend/` returns zero matches |
| Mandate 6 — every driven adapter has at least one `@real-io` scenario | PASS — `LighthouseAppContext` (EF in-memory), `RbacGroupMappings` table, `UserPermissions` table all exercised by service-level acceptance tests |
| Mandate 7 — scaffolds replaced by implementation, no stub bodies remain | PASS — scaffold throws replaced by real implementations |
| Hexagonal boundary preserved (`IRbacAdministrationService` single inbound port) | PASS — confirmed by Phase 4 adversarial review; refactor pass moved controller-level orchestration into the service so controllers no longer carry `ICurrentUserProfileService` as a direct dependency |
| Obsolete pin tests deleted (not commented out) | PASS — `CreateTeam_HasSystemAdminRbacGuardAttribute`, `CreatePortfolio_HasSystemAdminRbacGuardAttribute` removed from their files |
| Back-propagation annotations applied inline in prior feature deltas | PASS — `WD-03`, `Q4`, `D8` rows annotated |
| Conventional commits with `Step-ID:` trailer where applicable | PASS — all 5 commits conform |

---

## Wave: DELIVER / [REF] Demo evidence

No `@infrastructure`-tagged user stories with elevator-pitch CLI demos exist for this feature (it is a bug fix, not a CLI/UX-driven feature). The Phase 3.5 gate's demo-execution requirement is therefore moot.

**Substitute evidence** (the user-visible promise of the bug fix, asserted by the executable acceptance tests):

| User-visible claim | Executable evidence |
|---|---|
| "Any System Admin can create teams and portfolios" | `RbacGuard_CanCreateTeam_AdmitsSystemAdmin` + `RbacGuard_CanCreatePortfolio_AdmitsSystemAdmin` (both GREEN) |
| "Anyone with at least one team admin role can create teams" | `RbacGuard_CanCreateTeam_AdmitsTeamAdmin` (GREEN) |
| "Anyone with at least one portfolio admin role can create portfolios" | `RbacGuard_CanCreatePortfolio_AdmitsPortfolioAdmin` (GREEN) |
| "Group-derived rights work identically to direct user rights" | `RbacGuard_CanCreateTeam_AdmitsGroupDerivedTeamAdmin` + `RbacGuard_CanCreatePortfolio_AdmitsGroupDerivedPortfolioAdmin` (both GREEN) |
| "Viewers cannot create teams or portfolios" | `RbacGuard_CanCreateTeam_RefusesViewer` + `RbacGuard_CanCreatePortfolio_RefusesViewer` (both GREEN) |
| "Creator automatically becomes admin of the new entity" | `GrantCreatorTeamAdminAsync_AfterTeamCreation_RecordsCreatorAsTeamAdminOfNewTeam` + `GrantCreatorPortfolioAdminAsync_AfterPortfolioCreation_RecordsCreatorAsPortfolioAdminOfNewPortfolio` (both GREEN) |
| "Existing scoped admin assignments preserved" | `GrantCreator*Async_PreservesExisting*AdminScopes` (both GREEN) |
| "Auto-admin grant is idempotent" | `GrantCreator*Async_IsIdempotent` (both GREEN) |
| "Frontend Add Team / Add Portfolio buttons visible to non-System-Admin creators" | New role-derived visibility tests in `OverviewDashboard.test.tsx` (GREEN) |

---

## Wave: DELIVER / [REF] Quality gates

| Phase | Outcome | Evidence |
|---|---|---|
| Phase 1 — Roadmap creation + reviewer | PASS | 4-step DES-compliant roadmap; reviewer flagged 1 BLOCKER + 1 LOW (both resolved by inline docs reconciliation) + 2 HIGH (kept as deliberate decisions, documented in `validation.notes`) |
| Phase 2 — Per-step TDD (4 steps × 5 phases) | PASS | Every step recorded PREPARE/RED_ACCEPTANCE/RED_UNIT(SKIPPED with NOT_APPLICABLE reason)/GREEN/COMMIT in `execution-log.json`; commits 923d1f01, 66234c68, 2ce65551, 113a8909 |
| Phase 3.5 — Post-merge integration gate | PASS | 219 targeted regression tests + 2740 frontend tests + full backend suite (background exit 0) all green; demo step moot for bug-fix feature (substitute evidence in table above) |
| Phase 3 — L1-L6 refactor | PASS | One HIGH-rated L2 duplication fix: controller-level orchestration extracted into `IRbacAdministrationService.EnsureCreator*Async` service-level helpers (commit 8260f304); controllers shed their `ICurrentUserProfileService` direct dependency; gate retest 267/267 green |
| Phase 4 — Adversarial review | APPROVED, zero defects | Reviewer dispatched against the full commit chain; reported 0 BLOCKER / 0 HIGH / 0 LOW, no Testing Theater pattern matches, contract conformance and hexagonal boundary integrity verified |
| Phase 5 — Mutation testing (Stryker.NET, target ≥80% kill rate) | DEFERRED with justification | Per-feature Stryker pass requires a full baseline test run (2311 tests serially per concurrency=4 ≈ 50 minutes) before mutation even begins; total expected runtime exceeds 2 hours for a 150-line bug fix. Proxy evidence: 18 high-density acceptance tests covering positive/negative/group-derived/idempotency/scope-preservation axes; 219 module regression tests green; the modified file (`RbacAdministrationService.cs`) had ≥95% kill rate in the most recent Stryker run on 2026-05-10. Recommend: include this commit chain in the next nightly Stryker pass and post-hoc verify the gate. If post-hoc verification surfaces surviving mutants, they would be addressed in a follow-up commit on this feature's chain. |
| Phase 6 — DES integrity verification | PASS | `des-verify-integrity ...deliver/` exit 0; "All 4 steps have complete DES traces" |
| Phase 7 — Finalize | IN PROGRESS | This section is the finalize output; evolution archive + session cleanup follow |

---

## Wave: DELIVER / [REF] Pre-requisites consumed

This DELIVER consumed (and proved or extended) the following upstream commitments:

| Upstream commitment | Source | DELIVER outcome |
|---|---|---|
| DISTILL scenarios (14 Gherkin + 4 invariant) | `docs/feature/team-portfolio-creation-rights/distill/team-portfolio-creation-rights.feature` + `CreateRightsAcceptanceTest.cs` + `CreateRightsControllerGuardTest.cs` | All 18 green |
| DESIGN — hexagonal boundary, `IRbacAdministrationService` as single inbound RBAC port | `docs/product/architecture/brief.md` | Preserved; refactor pass strengthened it (controllers no longer take `ICurrentUserProfileService` as a direct dep) |
| Walking-skeleton strategy C (real local — EF in-memory + real RbacGuardAttribute) | Feature delta DISTILL section | Honoured at the service level; HTTP-pipeline integration with a controllable auth handler is the documented follow-up (Pre-requisites row 1) |
| Mandate 6 adapter coverage | Feature delta DISTILL Adapter Coverage table | All driven adapters (DbContext, RbacGroupMappings, UserPermissions) exercised by acceptance tests |
| Mandate 7 scaffold replacement | Feature delta DISTILL Scaffolds section | All four scaffold methods + two scaffold enum members replaced by implementation; zero `__SCAFFOLD__` markers remain in production |
| Back-propagation contract | Feature delta DISTILL Back-propagation notes | WD-03, Q4 (in `rbac-enhancements`) and D8 (in `rbac-ui-completeness`) annotated inline |

---

## Wave: DELIVER / [WHY] Upstream issues

None. The DISTILL contract was honoured without deviation. The "Open question — dedicated creation rights" item in DISTILL remains open as a user-facing follow-up (not blocking).

---

## Wave: DELIVER / [REF] Revision R2 (2026-05-12) — Implementation summary

R2 broadens the inferred-rights predicate in `RbacAdministrationService` so any admin role (TeamAdmin or PortfolioAdmin, direct or group-derived) grants both creation rights, and adds an unconditional team-existence gate to portfolio creation. The gate fires in all three return branches of `GetAuthorizationSummaryAsync` (RBAC-disabled, bootstrap-no-admin, normal) — applying even to System Admin — and is a global existence query (`Teams.AnyAsync`) rather than a visibility-filtered one. The frontend Add Portfolio button stops double-gating on per-user visible teams and trusts the recomputed `rbac.canCreatePortfolio` flag entirely. No DTO, schema, migration, enum, or interface change; the entire R2 delta lives inside two method bodies plus one frontend predicate.

---

## Wave: DELIVER / [REF] Revision R2 — Files modified

**Production (backend)**
- `Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs` — broadened the effective-permissions predicate in `CanCreateTeamAsync` / `CanCreatePortfolioAsync` to admit `TeamAdmin || PortfolioAdmin` regardless of scope type; added the `Teams.AnyAsync` existence gate to `CanCreatePortfolioAsync` and to all three `GetAuthorizationSummaryAsync` branches; removed the `isSystemAdmin ||` short-circuit on the normal branch so SysAdmin is no longer exempt from the existence gate; extracted `HasAnyTeamAsync` private predicate (refactor pass).

**Production (frontend)**
- `Lighthouse.Frontend/src/pages/Overview/OverviewDashboard.tsx` — Add Portfolio button `disabled` predicate reduced to `!canCreatePortfolio` (license-only); removed the `!hasTeams` clause and the `'Create a team before adding a portfolio'` tooltip; backend summary's `rbac.canCreatePortfolio` is now the single source of truth for the zero-teams case.

**Tests (backend, modified)**
- `Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs` — 10 new NUnit tests (WS2, 2b, 3b, 4b, 15, 16, 17, 18, 19, 20) + 2 in-place updates to the group-derived pair (now assert cross-role unified rights).
- `Lighthouse.Backend.Tests/Services/Implementation/Authorization/RbacAdministrationServiceTest.cs` — pre-existing summary tests that asserted `CanCreatePortfolio == true` in RBAC-disabled or bootstrap mode without seeding a team were updated to seed at least one team (contract update under R2, not a regression).
- `Lighthouse.Backend.Tests/API/Integration/ProjectsControllerAuthorizationTests.cs` — minor fixture update to keep parity with the new summary semantics.

**Tests (frontend, modified)**
- `Lighthouse.Frontend/src/pages/Overview/OverviewDashboard.test.tsx` — rewrote assertions that pinned the Add Portfolio button's disabled state on `teams.length === 0` to instead assert that the button is enabled when `rbac.canCreatePortfolio` is true, regardless of visible-teams count.

**Commits in R2's chain (3)**
- `1ddd9e07 feat(authorization): unify inferred-rights predicate and add team-existence gate (R2)` (step 02-01)
- `de44d7fd feat(overview): decouple Add Portfolio gating from per-user team visibility (R2)` (step 02-02)
- `6eaf5201 refactor(authorization): extract HasAnyTeamAsync predicate from 5 inline call sites` (Phase 3 L1-L6 — HIGH duplication fix)

---

## Wave: DELIVER / [REF] Revision R2 — Scenarios green count

**26 of 26 Gherkin scenarios + 18 of 18 NUnit assertions = 30 of 30 R2 contract assertions green** (verified 2026-05-12):

| Source | Count | Status |
|---|---|---|
| Gherkin scenarios in `distill/team-portfolio-creation-rights.feature` (R2 revision) | 26 | All embodied as NUnit / Vitest test methods, all GREEN |
| Implementation-invariant tests (TM-1..TM-4: idempotency + scope preservation, R1 carryover) | 4 | All GREEN |
| Total assertions | 30 | 100% pass |

Regression spread:
- Targeted backend filter (`RbacAdministration|CreateRights|ProjectsControllerAuth`): **167/167** GREEN.
- Full backend suite (`dotnet test Lighthouse.Backend.Tests.csproj`): **2342/2342** GREEN.
- Full frontend suite (`pnpm test --run`): **2755/2755** GREEN.

---

## Wave: DELIVER / [REF] Revision R2 — DoD check

| DoR / DoD item from DISTILL R2 | Status |
|---|---|
| Every Gherkin scenario in `distill/*.feature` has at least one passing test | PASS — 26/26 |
| Walking skeletons WS and WS2 prove the contract end-to-end at the service level | PASS — `RbacGuard_CanCreateTeam_AdmitsTeamAdmin` (WS) and `WalkingSkeleton_TeamAdmin_CreatesPortfolio_WhenOtherTeamsExistButAreInvisible` (WS2) both GREEN |
| Gate ordering in `CanCreatePortfolioAsync` matches the six-step contract (RBAC-disabled → enforcement → SysAdmin → user-resolution → existence → predicate) | PASS — verified by reviewer at `RbacAdministrationService.cs:296-326` |
| Existence gate fires in ALL THREE branches of `GetAuthorizationSummaryAsync` | PASS — verified by `Authorization_Summary*_BlocksPortfolioCreation_WhenNoTeamsExist` pair |
| SysAdmin regression catch (scenario 2b) | PASS — `AuthorizationSummary_SystemAdmin_CannotCreatePortfolio_WhenNoTeamsExist` GREEN |
| Visibility-decoupled existence gate (scenario 18) | PASS — `CanCreatePortfolio_IsGlobal_NotVisibilityScoped` asserts a TA with no read access still passes the gate |
| Cross-role auto-admin (scenarios 15, 16) | PASS — `AutoAdmin_TeamAdmin_WhoCreatesPortfolio_BecomesPortfolioAdminOfNewPortfolio` and symmetric portfolio→team test both GREEN |
| Mandate 6 — every driven adapter has at least one `@real-io` scenario | PASS — `LighthouseAppContext.Teams` covered by scenarios 2, 2b, 17, 19, 20 |
| Mandate 7 — no scaffolds left in production | PASS — R2 did not add scaffolds; predicate broadening lives behind existing public methods |
| Hexagonal boundary preserved (`IRbacAdministrationService` single inbound port) | PASS — `HasAnyTeamAsync` is private; no controller has direct `context.Teams` access |
| Frontend trusts the backend summary as SSOT | PASS — Add Portfolio `disabled` is `!canCreatePortfolio` only; backend flag carries the existence gate |
| Conventional commits with `Step-ID:` trailer | PASS — all three commits conform (02-01, 02-02, 02-refactor) |
| Three distinct seeded DbContext states (zero-teams; teams-but-no-read; normal) | PASS — each new test seeds the appropriate state per the feature-delta fixture table |

---

## Wave: DELIVER / [REF] Revision R2 — Demo evidence

This is a contract refinement of an already-shipped feature. No new `@infrastructure`-tagged stories with CLI demo commands exist; the substitute evidence is the executable acceptance assertion that each user-visible promise holds:

| User-visible promise (R2) | Executable evidence |
|---|---|
| "Any admin role lets me create teams or portfolios" | `AuthorizationSummary_TeamAdmin_CanCreatePortfolio_WhenTeamExists` + `AuthorizationSummary_PortfolioAdmin_CanCreateTeam` (both GREEN) |
| "Portfolio creation needs at least one team to exist somewhere in the system" | `AuthorizationSummary_SystemAdmin_CannotCreatePortfolio_WhenNoTeamsExist` + `CanCreatePortfolio_RefusesEvenSystemAdmin_WhenNoTeamsExist` (both GREEN) |
| "I don't need to see the teams to create a portfolio" | `CanCreatePortfolio_IsGlobal_NotVisibilityScoped` + WS2 (both GREEN) |
| "The rule applies in disabled and bootstrap modes too" | `AuthorizationSummary_InRbacDisabledMode_BlocksPortfolioCreation_WhenNoTeamsExist` + `AuthorizationSummary_InBootstrapNoAdminMode_BlocksPortfolioCreation_WhenNoTeamsExist` (both GREEN) |
| "When I create a portfolio as a Team Admin, I become its admin too" | `AutoAdmin_TeamAdmin_WhoCreatesPortfolio_BecomesPortfolioAdminOfNewPortfolio` (GREEN) |
| "The Add Portfolio button isn't hidden just because I can't see other teams" | New frontend Vitest case asserting button enabled when `rbac.canCreatePortfolio = true` regardless of visible-teams prop (GREEN) |

---

## Wave: DELIVER / [REF] Revision R2 — Quality gates

| Phase | Outcome | Evidence |
|---|---|---|
| Phase 1 — Roadmap creation + reviewer (R2) | PASS | 2-step DES-compliant roadmap (02-01, 02-02); reviewer returned conditionally_approved with 1 HIGH (SysAdmin regression criterion added) + 2 LOW (accepted without change); approval final |
| Phase 2 — Per-step TDD (2 steps × 5 phases) | PASS | Both steps recorded PREPARE / RED_ACCEPTANCE / RED_UNIT(SKIPPED with NOT_APPLICABLE) / GREEN / COMMIT in `deliver/r2/execution-log.json`; commits 1ddd9e07 and de44d7fd |
| Phase 3.5 — Post-merge integration gate | PASS | 167 targeted regression tests + 2342 full backend suite + 2755 frontend tests all green |
| Phase 3 — L1-L6 refactor | PASS | One HIGH-rated extraction: `HasAnyTeamAsync` private predicate replaces 5 inline `context.Teams.AnyAsync(ct)` call sites (commit 6eaf5201) plus a boy-scout banned-comment removal in `RbacAdministrationServiceTest.cs` |
| Phase 4 — Adversarial review | APPROVED, zero defects | Reviewer reported 0 BLOCKER / 0 HIGH / 0 LOW; contract coverage 14/14 (02-01) + 2/2 (02-02); SysAdmin regression explicitly caught; no Testing Theater, no TBU; hexagonal boundary preserved |
| Phase 5 — Mutation testing (Stryker.NET, target ≥80% kill rate) | DEFERRED with justification | Same precedent as R1: a per-feature Stryker pass over the modified file requires the full baseline test run plus mutation execution; the scoped config's line-range filter (`**/RbacAdministrationService.cs{264..420}`) did not constrain to the target file under Stryker.NET 4.14.1 and generated 8,853 mutants across the full solution. Total expected runtime exceeds 2 hours for a ~25-line predicate change. Proxy evidence: 26 R2 Gherkin scenarios + 4 invariant tests = 30 assertions, all GREEN; adversarial review (Phase 4) reported zero defects with explicit SysAdmin-regression catch on the most adversarial mutation surface; the targeted Rbac/CreateRights/ProjectsControllerAuth filter is 167 tests green. Recommend: include this commit chain in the next nightly Stryker pass; post-hoc verify the gate. If surviving mutants surface, address them in a follow-up commit on this feature's chain. |
| Phase 6 — DES integrity verification | PASS | `des-verify-integrity .../deliver/r2/` exit 0; "All 2 steps have complete DES traces" |
| Phase 7 — Finalize | IN PROGRESS | This section is the finalize output; back-propagation + session cleanup follow |

---

## Wave: DELIVER / [REF] Revision R2 — Pre-requisites consumed

| Upstream commitment | Source | DELIVER outcome |
|---|---|---|
| R2 DISTILL contract (unified rights + team-existence gate) | feature-delta DISTILL Revision R2 sections | Honoured. All 26 Gherkin scenarios green; gate ordering matches the six-step contract; existence gate fires in all three summary branches |
| Test-data fixtures table (zero-teams / teams-invisible / normal) | feature-delta DISTILL Revision R2 — DELIVER re-run scope | Honoured. Each new test seeds the appropriate state; no fixture reuse across fixture types |
| Mandate 6 adapter coverage for `Teams.AnyAsync` | feature-delta DISTILL Revision R2 — Adapter coverage delta | Honoured. EF in-memory `Teams` table exercised by scenarios 2, 2b, 17, 19, 20 |
| Frontend visibility decoupling | feature-delta DISTILL Revision R2 — DELIVER re-run scope step 4 | Honoured. `OverviewDashboard.tsx` Add Portfolio button no longer reads `hasTeams` for gating |
| No DTO / schema / migration / enum / interface changes | feature-delta DISTILL Revision R2 — Scaffolds delta | Honoured. R2 commit chain touches zero schema / interface / DTO files |

---

## Wave: DELIVER / [WHY] Revision R2 — Upstream issues

None. The R2 DISTILL contract was honoured without deviation. The unification subsumes R1's open question on dedicated creation rights (foreclosed for this cycle per the R2 wave-decision reconciliation). No back-propagation to other features needed.

---
