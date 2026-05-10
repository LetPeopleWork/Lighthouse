# Wave Decisions: DISCUSS — rbac-enhancements

Date: 2026-05-10

## Scope Assessment: SPLIT — 4 slices, ~12 stories, 5 bounded contexts, estimated 8-10 days total

**Split rationale**: Feature touched 5 bounded contexts (auth/RBAC, system settings UI, overview dashboard UI, team/portfolio detail UI, E2E testing) with 19 open design decisions. Any single slice could ship independently and deliver verifiable user value.

---

## Decision Log

| ID | Decision | Why | Resolves |
|---|---|---|---|
| WD-01 | Feature split into 4 delivery slices | 5 bounded contexts, 19 open decisions, multiple independent outcomes | Scope gate |
| WD-02 | Emergency admin: display-only, non-revocable via UI; shows "Emergency Admin" + lock icon | Prevents accidental removal of the safety net; config-managed | Q2 |
| WD-03 | canCreateTeam/canCreatePortfolio defaults to false for non-System-Admins | Least privilege; scoped admins manage existing entities, do not create new ones | Q4 |
| WD-04 | Status chips → collapsed "RBAC Status" disclosure panel | Day-to-day admin tasks should not require parsing diagnostic state | Q5 |
| WD-05 | License upload is pre-auth, independent of RBAC bootstrap; informational banner after login | Two orthogonal gates (product license vs. access control) must not be coupled | Q1 |
| WD-06 | All write controls hidden (not disabled) from Viewers | Hidden > disabled — disabled controls invite confusion and error clicks | Q9, Q10, Q11, Q12, Q16, Q17 |
| WD-07 | Group-based rights must be behaviourally identical to individual rights; E2E scenario 7 is the regression gate | Critical product invariant — if groups behave differently from individuals, RBAC is untrustworthy | Q (implicit) |
| WD-08 | ScopedGroupMappingManager uses scoped endpoint `/authorization/teams/{teamId}/group-mappings` only | Global endpoint is System Admin only; scoped endpoint uses CanManageTeamMembership | Q14 |
| WD-09 | Non-System-Admin create-team/portfolio button: bypass connections-required check | Non-admins cannot see connections (403 → []); the check is meaningless and misleading for them | Q13 |
| WD-10 | Log Level visible to System Admins only | Configuration setting with no value for non-admins | Q15 |
| WD-11 | Work Tracking System connections restricted to System Admins; connections section hidden for others | Least privilege; connection config has no viewer value | Q18 |
| WD-12 | Deliveries tab visible to Viewers (read-only); Add/Edit/Delete hidden | Forecasting data is the primary value for Viewers; modifications require write access | Q11 |
| WD-13 | Onboarding stepper shown only to users with canCreateTeam or canCreatePortfolio | Stepper is only actionable for users who can create entities | Q3 |
| WD-14 | Access tab in team/portfolio and System Admins tab in Settings gated on isRbacEnabled | Reduces clutter in non-RBAC deployments; tabs serve no purpose when RBAC is off | Q19 |
| WD-15 | Settings and Access tabs in team/portfolio visible to Team/Portfolio Admins for their own scope | Self-service is the core value of scoped admin role | Q17 |
| WD-16 | License Info section visible to all authenticated users (read-only); Upload button System Admin only | Transparency about product license status; action gate prevents accidental changes | Q8 |
| WD-17 | User removal is a hard delete with confirmation dialog listing role assignments | GDPR compliance requires actual removal; soft-delete creates false sense of security | Q6 |
| WD-18 | Overview sections (teams/portfolios/connections) are conditionally rendered based on user's access | Empty sections confuse Viewers; only show what is relevant to the current user | Q7 |

---

## DIVERGE Artifacts

No DIVERGE wave was run prior to this DISCUSS wave. The 19 open design questions from the E2E scaffold served as the primary input in lieu of DIVERGE validation. Risk: decisions are product-owner judgement calls without prior user research validation. Recommend validating the bootstrap flow (WD-05) and scoped admin self-service (WD-15) with at least one real System Admin before committing to the DESIGN wave.

---

## Handoff Package for Solution-Architect (DESIGN Wave)

The DESIGN wave will receive:

1. `docs/feature/rbac-enhancements/feature-delta.md` — full narrative with all design decisions, stories, and DoR validation
2. `docs/product/jobs.yaml` — 4 validated JTBD jobs
3. `docs/product/journeys/rbac-enhancements.yaml` — 4 journey schemas with emotional arcs, mockups, and Gherkin
4. `docs/feature/rbac-enhancements/discuss/story-map.md` — backbone, walking skeleton, release slices
5. `docs/feature/rbac-enhancements/discuss/shared-artifacts-registry.md` — all shared data, sources, integration risks
6. `docs/feature/rbac-enhancements/discuss/outcome-kpis.md` — measurable KPIs
7. `docs/feature/rbac-enhancements/slices/slice-0{1-4}-*.md` — delivery briefs

**Open items for DESIGN wave to resolve**:
- Backend: `RbacUser.isEmergencyAdmin` field — new field required (US-02)
- Backend: User removal endpoint — new endpoint required (US-04)
- Backend: Scoped group mappings endpoint for teams/portfolios — confirm or add (US-08)
- Test environment: 4 test user accounts with Keycloak group membership management (E2E tests)
- Test environment: Emergency admin must be configured in `appsettings.json` for the test instance
