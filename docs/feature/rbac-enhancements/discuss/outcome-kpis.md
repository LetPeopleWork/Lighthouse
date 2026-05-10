# Outcome KPIs: rbac-enhancements

## Feature: RBAC Enhancements and E2E Tests

### Objective

By the end of Q2 2026, any Lighthouse operator can configure role-based access control entirely from within the app — and every user role navigates a clean, error-free interface that shows exactly what they are authorised to do — validated by a complete automated E2E test suite.

---

### Outcome KPIs

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|---|---|---|---|---|---|
| KPI-1 | IT leads deploying Lighthouse | Complete RBAC bootstrap (first admin + SSO group) without editing server config | 100% of new deployments bootstrapped in-app | 100% require config file edit | Support ticket: "how to set first admin" count | Leading |
| KPI-2 | System Admins | Remove departed users in-app during offboarding | 100% offboarding completable in-app | No in-app removal mechanism | Support requests for DB-level user cleanup | Leading |
| KPI-3 | Team/Portfolio Admins | Manage their own scope (add members, update settings) without escalating to System Admin | Eliminate routine escalation tickets | All team/portfolio changes require System Admin | Support ticket category: "add me to team X" | Leading |
| KPI-4 | Viewers | Complete a forecasting session without triggering a 403 error or encountering a broken admin control | 0 error states in a normal Viewer session | Multiple error states currently triggered | Frontend error boundary events; API 403 rate filtered by Viewer sessions | Leading |
| KPI-5 | Development team | Run E2E suite and catch RBAC regressions before production | 7/7 core RBAC scenarios automated and green | 0% automated E2E coverage | E2E suite pass/fail per RBAC test scenario | Leading |

---

### Metric Hierarchy

**North Star**: Every user role navigates Lighthouse without errors and sees exactly the controls they are authorised to use.

**Leading Indicators**:
- Bootstrap completion rate in-app (KPI-1)
- Support ticket volume for RBAC administration tasks (KPI-2, KPI-3)
- API 403 error rate for Viewer-role sessions (KPI-4)
- E2E test pass rate for RBAC scenarios (KPI-5)

**Guardrail Metrics** (must NOT degrade after this feature):
- Zero users locked out due to RBAC hook errors (permissive fallback invariant)
- Non-RBAC deployments: no regression in existing functionality (all RBAC gating behind `isRbacEnabled` flag)
- Authentication success rate unchanged (RBAC layer must not interfere with OIDC login flow)

---

### Measurement Plan

| KPI | Data Source | Collection Method | Frequency | Owner |
|---|---|---|---|---|
| KPI-1 | Support tickets; deployment survey | Ticket tagging; post-deployment questionnaire | Per deployment | Product team |
| KPI-2 | Support tickets | Ticket category tagging | Monthly | Product team |
| KPI-3 | Support tickets | Ticket category tagging "add user to team" | Monthly | Product team |
| KPI-4 | Frontend error monitoring; API logs | Error boundary events; API access log filtered by 403 + user role | Continuous | Platform team |
| KPI-5 | CI/CD E2E pipeline | Automated test result reporting | Per PR / per release | Dev team |

---

### Hypotheses

**KPI-1**: We believe that a "Become First System Admin" button with an SSO group mapping form for first-time IT leads will achieve 100% in-app bootstrap.
We will know this is true when IT leads deploying Lighthouse complete RBAC bootstrap without any support tickets referencing config file edits.

**KPI-3**: We believe that giving Team/Portfolio Admins Settings and Access tabs for their own scope will eliminate routine admin escalations.
We will know this is true when the volume of "add user to team" support tickets drops to zero within 30 days of release.

**KPI-4**: We believe that hiding (not disabling) all write controls from Viewers will reduce Viewer-triggered 403 errors to zero.
We will know this is true when the API 403 rate for Viewer-role sessions reaches 0 in normal navigation flows (excluding deliberate URL manipulation).

**KPI-5**: We believe that implementing all 7 E2E scenarios from the scaffold will give the team confidence to ship RBAC changes without manual regression testing.
We will know this is true when E2E scenario 7 (group-based rights parity) passes consistently across 10 consecutive CI runs.

---

### Per-Story KPI Traceability

| Story | KPI(s) |
|---|---|
| US-01: Bootstrap | KPI-1 (primary) |
| US-02: Emergency Admin Display | KPI-2 (safety guardrail) |
| US-03: Status Panel | KPI-3 (admin usability) |
| US-04: User Removal | KPI-2 (offboarding) |
| US-05: Tab Visibility | KPI-4 (non-RBAC regression guardrail) |
| US-06: Scoped Tabs | KPI-3 (self-service) |
| US-07: Scoped Controls | KPI-4 (viewer errors eliminated) |
| US-08: Group Mapping Fix | KPI-3 (self-service, bug fix) |
| US-09: Viewer Experience | KPI-4 (primary) |
| US-10: Create Button Fix | KPI-3 (scoped admin usability) |
| US-11: E2E Scenarios 1-4 | KPI-5 (primary) |
| US-12: E2E Scenarios 5-7 | KPI-5 (parity validation) |
