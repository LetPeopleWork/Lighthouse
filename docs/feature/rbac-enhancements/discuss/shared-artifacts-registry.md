# Shared Artifacts Registry — rbac-enhancements

## Purpose

Every piece of data that flows between the backend, frontend components, and E2E tests must have a single documented source of truth. This registry prevents integration failures caused by components reading from different sources or caching stale values.

---

## Registry

### user_authorization_summary

| Field | Value |
|---|---|
| Source of truth | `GET /api/latest/authorization/my-summary` → `UserAuthorizationSummary` |
| Frontend owner | `useRbac()` hook (`src/hooks/useRbac.ts`) |
| Cached in | React state via `useRbac()` hook; fetched on component mount |
| Refresh trigger | On login; after any role mutation (grant/revoke/bootstrap/remove) |
| Integration risk | HIGH — if stale, users see wrong controls or are incorrectly blocked |

**Consumers**:

| Consumer | Field(s) Used | Display/Gate |
|---|---|---|
| `OverviewDashboard` | `canCreateTeam`, `canCreatePortfolio`, `isSystemAdmin` | Add Team/Portfolio/Connection button visibility |
| `OverviewDashboard` | `isRbacEnabled`, `systemAdminDisplayNames` | No-access alert content |
| `OnboardingStepper` | `canCreateTeam`, `canCreatePortfolio` | Stepper visibility |
| Team detail page | `adminTeamIds` (via `isTeamAdmin()`) | Settings/Access tabs, Clone/Delete/Reload/Update All |
| Portfolio detail page | `adminPortfolioIds` (via `isPortfolioAdmin()`) | Settings/Access tabs, Clone/Delete/Reload/Update All |
| Delivery section | `isTeamAdmin()` / `isPortfolioAdmin()` | Add/Edit/Delete delivery controls |
| Quick Settings | `isTeamAdmin()` / `isPortfolioAdmin()` | Quick Settings visibility |
| System Settings navigation | `isRbacEnabled` | System Admins tab visibility |
| System Settings navigation | `isSystemAdmin` | Log Level, Upload License button, full tab access |
| `ScopedMembershipManager` | N/A (system admin context, separate summary fetch) | Membership management UI |

**Validation**:
- `PERMISSIVE_SUMMARY` fallback on error — ensures no user is locked out if summary fetch fails
- Re-fetch after any mutation that changes roles (bootstrap, grant, revoke, remove)
- Stale summary risk: if another admin changes the current user's role, they will not see the change until next page load or explicit re-fetch

---

### rbac_status

| Field | Value |
|---|---|
| Source of truth | `GET /api/latest/authorization/status` → `RbacStatus` |
| Frontend owner | `RbacSettings.tsx` (local component state) |
| Cached in | Component state; re-fetched after any admin action in `RbacSettings` |
| Integration risk | HIGH — if stale, bootstrap banner appears incorrectly or System Admin is not shown |

**Consumers**:

| Consumer | Field(s) Used | Display/Gate |
|---|---|---|
| `RbacSettings` | `hasSystemAdmin` | Bootstrap banner visibility; user/group table visibility |
| `RbacSettings` | `enabled` | Diagnostic panel: RBAC enabled chip |
| `RbacSettings` | `premiumGateSatisfied` | Diagnostic panel: premium gate chip |
| `RbacSettings` | `hasEmergencyAdminConfigured` | Diagnostic panel: emergency admin chip |
| `RbacSettings` | `readyForEnablement` | Diagnostic panel: ready chip |
| `RbacSettings` | `unassignedUserCount` | Diagnostic panel: unassigned count; collapsed panel badge |
| `RbacSettings` | `groupClaimName` | Diagnostic panel: group claim chip |
| `BlockedPage` | `hasSystemAdmin` | Informational banner about bootstrap (Q1) |

---

### rbac_user_summary (list)

| Field | Value |
|---|---|
| Source of truth | `GET /api/latest/authorization/users` → `RbacUser[]` |
| Frontend owner | `RbacSettings.tsx` (local state) |
| Who can fetch | System Admins only (403 for others) |
| Integration risk | MEDIUM — table shows stale state if not refreshed after mutation |

**Key fields for US-02**:
- `isSystemAdmin: boolean` — existing field
- `isEmergencyAdmin: boolean` — **new field required** (not yet in `RbacUser` model)
- Consumers: user table column rendering, Revoke button conditional

---

### scoped_member_list (team/portfolio)

| Field | Value |
|---|---|
| Source of truth | `GET /api/latest/authorization/teams/{teamId}/members` or `/portfolios/{portfolioId}/members` |
| Frontend owner | `ScopedMembershipManager` component |
| Who can fetch | System Admin or the Team/Portfolio Admin for that scope |
| Integration risk | MEDIUM — scoped to entity; only affects that entity's Access tab |

---

### group_mappings (scoped)

| Field | Value |
|---|---|
| Source of truth | `GET /api/latest/authorization/teams/{teamId}/group-mappings` (scoped; not yet confirmed as existing endpoint) |
| Frontend owner | `ScopedGroupMappingManager` component |
| Who can fetch | System Admin or the Team Admin for that teamId |
| Integration risk | HIGH — US-08 bug is caused by this endpoint either not existing or `ScopedGroupMappingManager` calling the wrong (global) endpoint |
| Action required | Backend DESIGN wave must confirm or add the scoped group-mappings endpoint for teams and portfolios |

---

## Integration Checkpoints

### Checkpoint 1: Bootstrap flow data consistency

After `POST /bootstrap/system-admin` succeeds:
- `RbacStatus.hasSystemAdmin` must be `true` on next GET
- The bootstrapped user must appear in `GET /users` with `isSystemAdmin: true`
- The bootstrap banner must disappear (gated on `rbac_status.hasSystemAdmin`)

Risk: Race condition where component re-fetches before the DB write is visible. Mitigation: await both `status` and `users` fetches after bootstrap action (already done in `RbacSettings.load()`).

### Checkpoint 2: Role mutation summary cache invalidation

After any role grant/revoke/remove in `RbacSettings`:
- `GET /my-summary` should be re-fetched by the affected user at their next navigation
- The affected user's `useRbac()` hook does NOT automatically refresh unless the component remounts
- Risk: User A removes User B's admin — User B does not know until they reload
- Mitigation: This is acceptable for now (role changes do not take effect until the affected user reloads). Document as a known limitation.

### Checkpoint 3: Scoped group mappings endpoint availability

Before US-08 can be implemented:
- Backend must expose `GET /authorization/teams/{teamId}/group-mappings` scoped endpoint checked against `CanManageTeamMembership`
- If the endpoint does not exist, US-08 is blocked (tracked as dependency)
- Solution-architect in DESIGN wave must confirm endpoint availability

---

## Variables in UI Mockups — Source Mapping

| Variable | Mockup Location | Source |
|---|---|---|
| `${user.displayName}` | User table, confirmation dialogs | `RbacUser.displayName` from GET /users |
| `${user.email}` | User table | `RbacUser.email` from GET /users |
| `${user.isSystemAdmin}` | System Admin column | `RbacUser.isSystemAdmin` |
| `${user.isEmergencyAdmin}` | System Admin column (new) | `RbacUser.isEmergencyAdmin` (new field) |
| `${rbacStatus.unassignedUserCount}` | Status panel / badge | `RbacStatus.unassignedUserCount` |
| `${rbacStatus.groupClaimName}` | Status panel | `RbacStatus.groupClaimName` |
| `${team.name}` | Access tab header, confirmation dialogs | Team entity from GET /teams/{id} |
| `${groupMapping.groupValue}` | SSO group mappings table | `RbacGroupMapping.groupValue` |
| `${systemAdminDisplayNames}` | No-access alert in Overview | `UserAuthorizationSummary.systemAdminDisplayNames` |
