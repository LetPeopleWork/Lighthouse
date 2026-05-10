# Story Map: rbac-enhancements

## Users: System Admin · Team/Portfolio Admin · Viewer · First-Time Admin
## Goal: Every user role experiences a clean, correct, error-free Lighthouse UI gated by their RBAC rights

---

## Backbone

| Bootstrap & First Admin | System Admin Operations | Scoped Team/Portfolio Admin | Viewer Experience | E2E Verification |
|---|---|---|---|---|
| Claim first admin role | Manage user table | Manage own team/portfolio | View forecasts (read-only) | Automate core flows |
| Assign SSO group | Remove departed users | Configure scoped SSO groups | No admin controls visible | Cover all 7 scenarios |
| Show license guidance | Control tab/section visibility | Self-serve settings changes | Understand access state | Group vs individual parity |

---

### Walking Skeleton
Minimum end-to-end slice proving the system is usable:

1. US-01: First admin bootstraps and assigns SSO group (Q1, Q2 prerequisite)
2. US-02: Emergency admin displayed distinctly (safety invariant)
3. US-09: Viewer sees clean read-only overview with no admin controls or errors
4. US-11: E2E scenarios 1-4 pass (bootstrap + viewer + admin flow verified)

Every persona has at least one covered behaviour. The system is usable end-to-end.

---

### Release 1: Target Outcome — "Any deployment can be bootstrapped and basic role correctness is verified"

Stories: US-01, US-02, US-03, US-11

Activities covered:
- Bootstrap (all tasks)
- Emergency admin display (safety)
- Status chips removed (UX polish)
- E2E scenarios 1-4 automated

Outcome KPI targeted: 100% of new deployments complete bootstrap in-app; E2E suite green on scenarios 1-4.

Priority rationale: Bootstrap is a hard dependency — no other story is meaningful until a System Admin exists. US-11 immediately validates the bootstrap story end-to-end.

---

### Release 2: Target Outcome — "System Admins can fully manage users, groups, and access tab visibility"

Stories: US-04, US-05

Activities covered:
- User housekeeping (remove departed users)
- Access tab visibility gated by RBAC enabled state

Outcome KPI targeted: 100% of offboarding completable in-app; 0 confusion from RBAC tabs in non-RBAC deployments.

Priority rationale: These stories are independent of scoped-admin work and add safety/compliance value immediately after bootstrap is stable.

---

### Release 3: Target Outcome — "Team/Portfolio Admins are fully self-sufficient in their scope"

Stories: US-06, US-07, US-08, US-10, US-12

Activities covered:
- Scoped Settings and Access tabs
- Scoped Clone/Delete/Reload/Update All controls
- Group mapping fix (critical bug — was causing errors)
- Create Team/Portfolio button fix for scoped admins
- E2E scenarios 5-7 automated (individual + group parity)

Outcome KPI targeted: Eliminate 100% of routine admin escalations for Team/Portfolio Admins.

Priority rationale: US-08 is technically a bug fix and highest urgency in this release. US-12 provides the regression gate for the group equivalence invariant (critical correctness concern).

---

### Release 4: Target Outcome — "Viewer experience is error-free and focused"

Stories: US-09 (if not already in walking skeleton), viewer-specific refinements

Activities covered:
- All Q7, Q8, Q11, Q12, Q15, Q18 resolutions
- Log Level hidden from non-admins
- Delivery read-only mode
- Overview sections correctly gated

Outcome KPI targeted: 0 403 errors or disabled-control confusion in normal Viewer navigation sessions.

Priority rationale: While viewer experience is important for user satisfaction, it is lower risk than bootstrap and admin correctness. Viewers are currently functional — just noisy.

---

## Priority Rationale

Stories are ordered by: Walking Skeleton > Riskiest Assumption > Highest Value.

1. **Riskiest assumption for bootstrap**: Can the first admin complete setup in-app without IT escalation? (US-01, US-11)
2. **Safety invariant**: Will the emergency admin fallback remain intact? (US-02)
3. **Compliance**: Can System Admins remove departed users? (US-04)
4. **Correctness**: Will Team/Portfolio Admins encounter errors in their scope? (US-08 bug fix)
5. **Self-service**: Can scoped admins manage their own teams without escalation? (US-06, US-07, US-10)
6. **Parity**: Are group-based rights equivalent to individual rights? (US-12)
7. **Viewer polish**: Are Viewers fully shielded from admin controls? (US-09 completeness)

## Story-to-Slice Mapping

| Story | Slice | Priority | Outcome Link | Dependencies |
|---|---|---|---|---|
| US-01 | Slice 01 | P1 (WS) | KPI-1 (bootstrap in-app) | None |
| US-02 | Slice 01 | P1 (WS) | KPI-2 (safety) | US-01 |
| US-11 | Slice 01 | P1 (WS) | KPI-5 (E2E coverage) | US-01, US-02 |
| US-03 | Slice 01 | P2 | UX polish | US-01 |
| US-04 | Slice 02 | P2 | KPI-2 (offboarding) | US-01 |
| US-05 | Slice 02 | P2 | UX correctness | US-01 |
| US-06 | Slice 03 | P2 | KPI-3 (scoped self-service) | US-01, US-05 |
| US-07 | Slice 03 | P2 | KPI-4 (viewer correctness) | US-06 |
| US-08 | Slice 03 | P1 (bug) | KPI-3 (scoped self-service) | US-06 |
| US-10 | Slice 03 | P2 | KPI-3 (scoped self-service) | US-06 |
| US-12 | Slice 03 | P2 | KPI-5 (E2E coverage) | US-11, US-06 |
| US-09 | Slice 04 | P2 | KPI-4 (viewer experience) | US-07 |
