# Evolution: rbac-ui-completeness

**Finalized**: 2026-05-11
**Wave path**: DISCUSS → DESIGN → DISTILL → DELIVER
**Outcome**: Production code shipped, 7 commits, frontend-only

## Summary

Closed the residual RBAC UI gating gaps left over from `rbac-enhancements`. The prior feature's DISCUSS scoped Q9 (Update All button), Q10 (reload controls), and Q16 (clone/delete) to be admin-gated, but DELIVER landed only the per-team-detail reload button and not the global header / overview / route-level surfaces. Manual exploratory testing surfaced four high-visibility gaps plus four polish items; this feature closes all eight.

The eight stories:

1. **US-01** — Header "Update All" button hidden from non-System-Admins.
2. **US-02** — Per-row Edit / Clone / Delete icons in the Overview table gated by role (Edit/Delete by entity admin, Clone by SystemAdmin).
3. **US-03** — OnboardingStepper hidden from scoped admins (it leads to `/connections/new` which is sysadmin-only — revises rbac-enhancements WD-13).
4. **US-04** — Settings → API Keys tab hidden from non-System-Admins.
5. **US-05** — License Import / Clear buttons in the popover hidden from non-System-Admins; status text stays visible.
6. **US-06** — Access tab on TeamDetail / PortfolioDetail no longer flashes during entity load in non-RBAC mode.
7. **US-07** — Direct URL navigation to `/teams/new`, `/portfolios/new`, `/connections/new`, `/connections/:id/edit` renders an inline no-access alert with a back link instead of the wizard form when the user lacks SystemAdmin.
8. **US-08** — Add Team / Add Portfolio buttons in the Overview gated on SystemAdmin (resolves a frontend / backend mismatch where `canCreateTeam=true` for Team Admins but the backend `CreateTeam` endpoint requires SystemAdmin).

## Business context

The prior `rbac-enhancements` feature successfully shipped the core role model (System Admin, Team Admin, Portfolio Admin, Viewer) and the in-detail-page gating (Settings tab, Access tab, write-controls bar). Manual UAT revealed the global surfaces and route-level entry points were not fully gated — viewers and scoped admins could click controls that returned 403, hurting the "0 error states for non-admins" KPI. This feature closes that gap.

## Key decisions

| ID | Decision | Rationale |
|---|---|---|
| **DD-01** | Reuse the existing `*-no-access-alert` inline Alert pattern for route guards; do not introduce a new `<RbacGate>` wrapper component | The convention already exists at three call sites (TeamDetail, PortfolioDetail, OverviewDashboard). A wrapper would strip the page's `Container` / page-title chrome, forcing each page to either re-wrap inside or hoist its chrome outward — both regress readability. |
| **DD-02** | New thin hook `useRbacGate(requirement)` returning `{ allowed, isLoading }` | Hook centralises the boolean; page owns its JSX shell. Discriminated-union requirement gives type-safe call sites. |
| **DD-03** | `DataOverviewTable` accepts predicate props (`canEditRow`, `canCloneRow`, `canDeleteRow`); table stays RBAC-agnostic | ADR-001 composition principle. Defaults to `() => true` preserves backward compatibility for any existing caller. |
| **DD-08** | Tighten frontend `canCreateTeam` / `canCreatePortfolio` to require SystemAdmin (match backend); leave the summary fields in place with a doc comment for now | Lighthouse's model assumes team creation requires a Work Tracking System Connection, which is sysadmin-owned — non-sysadmins cannot reasonably complete the wizard anyway. Loosening the backend by adding a new `RbacGuardRequirement.CanCreateTeam` enum value is explicitly deferred to a future feature if customer demand surfaces. |
| **DD-10** | `useRbacGate` exposes `isLoading` flag separately from `allowed` | Prevents flashing the no-access alert during the initial `PERMISSIVE_SUMMARY` → real-summary transition. Pages render neither alert nor form while `isLoading` is true. |

Full DESIGN trail in `docs/feature/rbac-ui-completeness/feature-delta.md` under `## Wave: DESIGN`. Inherits ADR-001 (hide-not-disable) from the prior feature; no new ADRs written for this delivery.

## Steps completed (7)

| Step | Commit | What |
|---|---|---|
| 01-01 | `a0f9c98b` | Replace the `useRbacGate.ts` RED scaffold with a real composition over `useRbac()`; 7 Vitest cases covering every branch + `isLoading` |
| 01-02 | `ab881426` | Header `<UpdateAllButton/>`, Settings API Keys tab, LicensePopover Add/Clear buttons all gated on `isSystemAdmin`; new testids `license-add-button` and `license-clear-button` |
| 02-01 | `33879f49` | `DataOverviewTable` accepts `canEditRow` / `canCloneRow` / `canDeleteRow` predicates; defaults preserve existing callers |
| 02-02 | `d88d0e0e` | `OverviewDashboard` wires predicates from `useRbac` into both tables; OnboardingStepper + Add Team + Add Portfolio gated on `isSystemAdmin` |
| 03-01 | `90ae8971` | EditTeam / EditPortfolio / EditConnection use `useRbacGate` at the top of render; inline no-access alert with back link; loading-state shows neither alert nor form |
| 04-01 | `dfa08f39` | `showAccessTab` tightened to `!!entity && rbac.isRbacEnabled && rbac.isXAdmin(id)` in TeamDetail and PortfolioDetail (`showSettingsTab` intentionally unchanged); doc comment on `canCreateTeam` / `canCreatePortfolio` in `useRbac.ts` |
| 05-01 | `8c4d5bd2` | Playwright `@rbac` E2E extended with viewer / scoped-admin / sysadmin gating assertions; new `testWithAuth` block for direct-URL navigation guards |

## Final test counts

- **Frontend Vitest + RTL**: 2709/2709 passing (was 2682 before the feature — +27 tests)
- **Playwright `@rbac` E2E**: 13 scenarios authored; CI verification via `ci_verifyauth.yml` (local run deferred — Docker Keycloak stack was unhealthy on the dev host)
- **Backend xUnit**: unchanged (no backend code touched — D9)

## Quality gate outcomes

- **Adversarial review (Haiku)**: APPROVED — 0 blockers, 0 high, 0 low, 0 testing-theater findings
- **DES integrity verification**: PASS — "All 7 steps have complete DES traces"
- **Mutation testing (Stryker.JS)**: INCOMPLETE — same OOM blocker as `rbac-enhancements`; manual mutation analysis on the only file with non-trivial logic (`useRbacGate.ts`, 9 mutants) projects 100% kill rate against existing tests. See `docs/feature/rbac-ui-completeness/deliver/mutation/mutation-report.md`.

## Lessons learned

**1. Manual exploratory testing catches gating gaps that even thoughtful scoping misses.**
The prior feature's DISCUSS explicitly listed `Update All button` (Q9), `Reload` (Q10), `Clone/Delete` (Q16) as admin-gated. DELIVER landed only the per-entity Reload variant. Without the user's manual UAT pass, the other surfaces would have shipped to production silently. Take-away: the "all DoD checked" state is necessary but not sufficient — a manual session against each role is still cheap insurance.

**2. The `!entity || (...)` guard pattern is a load-state anti-pattern when the right side can be `false` in steady state.**
TeamDetail and PortfolioDetail used `showAccessTab = !team || (rbac.isRbacEnabled && rbac.isTeamAdmin(team.id))`. In non-RBAC mode (`isRbacEnabled === false`), the steady-state value is `false` — but the load-state value is `true` (because `!team`), producing a brief flash. The fix is `!!team && rbac.isRbacEnabled && rbac.isTeamAdmin(team.id)`. The `showSettingsTab` next to it kept the `!entity ||` pattern intentionally because admins want the tab during load — same code, different semantics, different correct guard. Worth documenting as a small idiom.

**3. The `isLoading` field on `useRbacGate` is load-bearing for route guards.**
Without it, a sysadmin opening `/teams/new` would briefly see the no-access alert (because `PERMISSIVE_SUMMARY` initially returns `isSystemAdmin: true` and then the real summary returns `true` — wait, that's identical, no flicker). The actual flicker scenario is the inverse: PERMISSIVE_SUMMARY says `isSystemAdmin: true` initially, then the real summary returns `false` for a Viewer hitting `/teams/new` — at that point we need the alert. So the test fixture must reflect a 2-step transition (loading → real summary), not a single-state mock. The crafter caught this and added the `"renders neither alert nor form while RBAC summary is loading"` test for each of the three guards.

**4. Frontend / backend semantic mismatches are easy to miss without a paired contract.**
`canCreateTeam` on the `UserAuthorizationSummary` returned `true` for Team Admins (per `RbacAdministrationService.CanCreateTeamAsync`); the backend `POST /teams` endpoint required SystemAdmin. The frontend showed the Add Team button to Team Admins; the wizard loaded; submit → 403. The fix here is to tighten the frontend gate to match the backend; the cleaner long-term fix (a new `RbacGuardRequirement.CanCreateTeam` enum value) is deferred. Take-away: when a Boolean field on a summary DTO is consumed for UI gating, the field's semantics must be derived from the backend's authorization rule, not from an independent service-layer computation that drifts.

**5. Stryker.JS frontend mutation testing is still blocked by the same heap exhaustion as in `rbac-enhancements`.**
We attempted a focused run against just `useRbacGate.ts` (15 LOC, 9 mutants, 7 test cases) with `NODE_OPTIONS=--max-old-space-size=8192`. Still OOM'd during instrumentation. This is project infrastructure, not feature-specific — tracked as a continuation of the existing `rbac-enhancements` mutation follow-up.

## Issues encountered

- **DES log path resolution under crafter cwd switches**: Step 01-01's crafter ran from `Lighthouse.EndToEndTests/` for part of the work; the DES stop-hook resolved `docs/...` relative to that cwd and looked for the log at the wrong path. Crafter created a symlink to bridge the two paths. Take-away: future crafters should be told to operate from the repo root, or pass absolute paths to `des-log-phase`.
- **Stale Stryker sandboxes**: ~120 GB of `.stryker-tmp/sandbox-*` from a May 10 mutation run polluted `pnpm test` discovery. Cleaned up. Recommend adding `.stryker-tmp/**` to the project's `.gitignore` and to `vitest.config.ts` exclude list as a follow-up.
- **CI binding fix mid-feature**: Two out-of-band commits (`5cf1cd49`, `ce068dc3`) landed during this feature to fix the `Authorization__EmergencySystemAdminSubjects` env-var binding in `ci_verifyauth.yml` (from JSON array to indexed `__0` syntax). Not part of this feature's 7 commits; addressed a CI bug from earlier the same day.

## Migrated artifacts

Per the lean v3.14 SSOT model, no Phase-B migration needed. Architecture ADRs (ADR-001..003) and journeys are already in `docs/product/architecture/` and `docs/product/journeys/` from the prior feature. This feature wrote no new ADRs and no new journey YAML — it extends the existing pattern.

## Workspace preserved

Per nw-finalize convention, `docs/feature/rbac-ui-completeness/` stays. Wave matrix derives status from it. Session markers (`.nwave/des/deliver-session.json`) removed at finalize.

## Pre-release checklist

- [x] All 7 steps green (DES integrity verified)
- [x] Full Vitest suite passing
- [x] Adversarial review approved
- [x] CI workflow unchanged (frontend-only feature reuses existing `@rbac` pipeline)
- [x] Evolution doc written
- [ ] Mutation testing per `per-feature` strategy — **deferred**: inherits the same Stryker.JS infrastructure follow-up tracked in `rbac-enhancements`. Manual mutation analysis projects 100% kill rate on the only non-trivial new file.
- [ ] Playwright `@rbac` E2E suite run end-to-end — **deferred to CI**: local Docker Keycloak stack was unhealthy; CI `ci_verifyauth.yml` is the verification path.
- [ ] PR opened against main — **N/A**: feature developed directly on main with signed commits.
