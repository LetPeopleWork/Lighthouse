# Evolution: team-portfolio-creation-rights

**Finalized**: 2026-05-11
**Wave path**: DISTILL → DELIVER (DISCUSS/DESIGN/DEVOPS waves skipped — bug-fix scope, contradicting decisions reconciled inline in feature delta)
**Outcome**: Production code shipped, 5 commits across 4 roadmap steps + 1 refactor pass

## Summary

Closed a multi-layer mismatch in team/portfolio creation rights. The backend `IRbacAdministrationService.CanCreateTeamAsync` / `CanCreatePortfolioAsync` service methods already implemented the "System Admin OR holds at least one TeamAdmin / PortfolioAdmin role (direct or group-derived)" rule, but the HTTP gate (`[RbacGuard(SystemAdmin)]`) on the create endpoints refused everyone except System Admins, and the frontend `OverviewDashboard` hid the Add buttons behind `rbac.isSystemAdmin`. The result: a user with the appropriate role saw `canCreateTeam=true` in `/my-summary` but could neither see the button nor reach the endpoint. The feature aligns all three layers and adds the missing auto-admin grant on successful creation.

This feature also **supersedes** two prior locked decisions:

- `rbac-enhancements/WD-03`+`Q4` — "canCreateTeam/Portfolio = System Admin only" — overridden by the inferred-rights model.
- `rbac-ui-completeness/D8` — "tighten frontend canCreateTeam/canCreatePortfolio to require SystemAdmin" — rolled back; the backend was the layer that needed fixing, not the frontend.

Both prior feature deltas were annotated inline with the supersession references in step 01-04.

## Business context

The bug report from the user (paraphrased): a Team Admin in production sees `canCreateTeam=true` from `/my-summary` but the Add Team button is invisible (frontend gating bug) and a direct POST returns 403 (backend gating bug). Even if either layer were fixed, the resulting team would belong to a user with no explicit admin permission on it — they could create but not manage. This feature delivers what the contract was always meant to be: inferred creation rights, automatic creator-admin assignment, group-derived rights honoured.

## Key decisions

| ID | Decision | Rationale |
|---|---|---|
| Override `rbac-enhancements/WD-03` and `rbac-ui-completeness/D8` | Inferred-rights at the HTTP gate (delegate to existing service logic), not new system-level roles | Smallest blast radius; service code was already correct. Recorded as supersession annotations in the prior deltas. |
| Auto-grant creator as scoped admin on successful create, gated on `IsRbacEnforcedAsync` | Without this, a Team Admin can create a team but cannot manage it (RbacGuard would 403 on the next request) | Disabled-auth-mode skip prevents pollution of permission rows by the synthetic "lighthouse\|auth-disabled" subject. |
| Two new `RbacGuardRequirement` enum values (`CanCreateTeam`, `CanCreatePortfolio`) + matching switch arms in `CanSatisfyRequirementAsync` | Reuse the existing attribute-driven authorization filter pipeline | Avoids a new authorization plumbing path; the `[RbacGuard]` attribute stays the single way to declaratively gate an endpoint. |
| New service methods `GrantCreatorTeamAdminAsync` / `GrantCreatorPortfolioAdminAsync` + private orchestration helper `EnsureCreatorAdminAsync` (added in refactor pass) | Idempotent persistence + single orchestration point | Refactor pass moved the controller-level `if (IsRbacEnforced) { resolve user; if (not null) grant; }` workflow into the service so controllers shed `ICurrentUserProfileService` dep. |
| Defer Option B (dedicated `TeamCreator` / `PortfolioCreator` system-level rights) | User raised the question; we acknowledged and shipped Option A first | Open question carried in feature-delta's `DISTILL / [REF] Open question — dedicated creation rights` section. Re-evaluate after one release. |

## Steps completed (4 roadmap steps + 1 refactor pass)

| Step | Commit | What |
|---|---|---|
| 01-01 | `923d1f01` | Service-layer: two new switch arms + two grant methods; replaced Mandate 7 scaffolds with real implementations. 14 RED → GREEN. |
| 01-02 | `66234c68` | Controllers: `[RbacGuard]` requirement swapped on the four create/validate endpoints; auto-admin grant wired into `CreateTeam` / `CreatePortfolio`; two obsolete pin tests deleted. 4 RED → GREEN. |
| 01-03 | `2ce65551` | Frontend: `OverviewDashboard` outer guard switched from `isSystemAdmin` to `canCreateTeam` / `canCreatePortfolio`; existing tests rewritten in place to assert role-derived visibility; new tests for Team/Portfolio Admin and Viewer cases. |
| 01-04 | `113a8909` | Doc-only: inline supersession annotations on `rbac-enhancements` WD-03 / Q4 and `rbac-ui-completeness` D8 rows. |
| Phase 3 refactor | `8260f304` | L2 duplication: extracted controller-level orchestration into `IRbacAdministrationService.EnsureCreatorTeamAdminAsync` / `EnsureCreatorPortfolioAdminAsync`; controllers shed `ICurrentUserProfileService` direct dependency. |

## Quality gates summary

- **DES integrity**: all 4 steps have complete TDD traces (PREPARE → RED_ACCEPTANCE → RED_UNIT(skipped, justified) → GREEN → COMMIT).
- **Test density**: 18 NUnit tests (14 service-level acceptance + 4 controller-attribute contract). All GREEN. Plus 219 module regression tests GREEN. Plus 2740 frontend tests GREEN.
- **Adversarial review**: APPROVED with zero defects (0 BLOCKER / 0 HIGH / 0 LOW). No Testing Theater patterns. Contract conformance, hexagonal boundary, security gates all verified.
- **Mutation testing**: deferred to nightly CI Stryker pass. Per-feature pass would require 2+ hours (2311 baseline tests + mutation runs). Proxy evidence documented in feature delta `Quality gates` section: the modified file (`RbacAdministrationService.cs`) had ≥95% kill rate in the most recent Stryker run on 2026-05-10.
- **Wiring smoke**: every new public method has a production call site (`EnsureCreator*Async` called from `TeamsController.CreateTeam` / `PortfoliosController.CreatePortfolio`; `GrantCreator*Async` called from the `Ensure*` helpers AND covered by direct acceptance tests).
- **Scaffold removal**: `grep -rn "__SCAFFOLD__"` returns zero matches in `Lighthouse.Backend/`.

## Carry-forward / follow-ups

- **HTTP-level integration tests with controllable auth** — the existing `TestWebApplicationFactory` runs with `Authentication.Enabled = false`, short-circuiting RBAC enforcement. Adding a test-only `AuthenticationHandler` that injects a configurable `ClaimsPrincipal` would let acceptance tests exercise the full HTTP pipeline with RBAC enforced. Documented as Pre-requisites row 1 in feature delta. Tracked as a separate follow-up feature.
- **Option B — dedicated creation rights at system level** — open question carried in feature delta. Re-evaluate after one release cycle.
- **Branch hygiene** — this feature's commits ended up split across `main` (01-01, 01-02, 01-03) and `fix/bug-4975-manual-delivery-date-update` (01-04, refactor, finalize) due to a branch switch by a parallel work stream mid-orchestration. Cherry-pick or rebase as appropriate for clean merge.

## Cross-references

- Feature delta (single SSOT): `docs/feature/team-portfolio-creation-rights/feature-delta.md`
- Gherkin scenarios: `docs/feature/team-portfolio-creation-rights/distill/team-portfolio-creation-rights.feature`
- Acceptance tests: `Lighthouse.Backend.Tests/Services/Implementation/Authorization/CreateRightsAcceptanceTest.cs`, `Lighthouse.Backend.Tests/API/CreateRightsControllerGuardTest.cs`
- Roadmap: `docs/feature/team-portfolio-creation-rights/deliver/roadmap.json`
- DES execution log: `docs/feature/team-portfolio-creation-rights/deliver/execution-log.json`
- Superseded prior decisions: `rbac-enhancements/feature-delta.md` (WD-03, Q4), `rbac-ui-completeness/feature-delta.md` (D8)
