# ADR-003: Emergency Admin Display Approach

**Status**: Accepted
**Date**: 2026-05-10
**Feature**: rbac-enhancements
**Decider**: Morgan (Solution Architect) based on WD-02 (DISCUSS wave)

---

## Context

Lighthouse supports an "emergency admin" configured via server configuration (`appsettings.json` or environment variable). This user always has System Admin rights regardless of database state, providing a safety fallback in case all other System Admins are accidentally removed.

In the current implementation, the emergency admin appears in the `RbacUserSummary` list with `IsSystemAdmin: true` — indistinguishable from a normally-granted System Admin. A System Admin operating the user table might attempt to revoke the emergency admin's rights, which would fail silently at the backend (the config-based grant cannot be revoked via the API) or create confusion.

Three display approaches were evaluated.

---

## Decision

**Display the emergency admin with a distinct visual state: "Emergency Admin" label with a lock icon. No Revoke button is rendered for this row. A tooltip explains the admin is managed via server configuration.**

Implementation:
- `RbacUserSummary` gains `IsEmergencyAdmin: bool` on the backend.
- `RbacUser` TypeScript interface gains `isEmergencyAdmin?: boolean`.
- `RbacSettings.tsx` branches on `user.isEmergencyAdmin` to render the distinct state and omit the Revoke button.
- The emergency admin cannot be removed via `DELETE /authorization/users/{id}` — the backend rejects this with a meaningful error (and the frontend pre-empts it by not rendering a Remove button for this row).

---

## Alternatives Considered

### Option A: Display "Yes" with a disabled Revoke button and tooltip (rejected)

The row shows "Yes" (same as a normal System Admin) with the Revoke button rendered but `disabled`, with tooltip: "Emergency admins cannot be revoked via the UI."

**Rejected because**:
- A disabled button still implies the action exists, inviting the user to wonder why it doesn't work.
- Violates ADR-001 principle: controls that cannot be actioned should be hidden, not disabled.
- Does not communicate the key information: "this is a different kind of System Admin."

### Option B: Hide the emergency admin from the user table entirely (rejected)

The emergency admin user record is not shown in the System Admins table.

**Rejected because**:
- System Admins need to know the emergency admin exists to understand the current access state.
- If the emergency admin is the only admin (no normal admins assigned), the bootstrap button would incorrectly appear (thinking no System Admin exists), allowing a second bootstrap attempt.
- Transparency about who has System Admin rights is a security requirement.

### Option C: Distinct label + lock icon + no Revoke/Remove buttons (selected)

The row shows "Emergency Admin" with a lock icon. No Revoke or Remove button is rendered. A tooltip explains the configuration source.

**Accepted because**:
- Unambiguously communicates that this is a special, protected admin — not a candidate for revocation.
- No disabled control (consistent with ADR-001).
- Bootstrap banner logic correctly uses `hasSystemAdmin` (which is true even for emergency-only admin), preventing incorrect re-bootstrap.
- Provides actionable guidance via tooltip: "To change this, update the application configuration."

---

## Consequences

**Positive**:
- Zero risk of accidental emergency admin removal via the UI.
- System Admins understand at a glance which users are config-managed vs database-managed.
- Bootstrap banner behaves correctly: suppressed whenever `hasSystemAdmin` is true.

**Negative**:
- Requires a new `IsEmergencyAdmin` backend field, propagated through `RbacUserSummary` and the TypeScript model. This is a minimal addition (one boolean).
- The frontend must distinguish three states ("Yes", "Emergency Admin", "No") where it previously handled two. Acceptable complexity given the safety benefit.
