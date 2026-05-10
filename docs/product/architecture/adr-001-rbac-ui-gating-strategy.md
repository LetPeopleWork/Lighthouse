# ADR-001: UI Gating Strategy — Hidden vs Disabled Controls for Viewers

**Status**: Accepted
**Date**: 2026-05-10
**Feature**: rbac-enhancements
**Decider**: Morgan (Solution Architect) based on WD-06 (DISCUSS wave)

---

## Context

The rbac-enhancements feature introduces role-based access control that determines which users may perform write actions (create, update, delete) on teams, portfolios, and system settings. The frontend must visually communicate to Viewers that they do not have permission to perform these actions.

Three options were evaluated: hide the controls entirely, disable them with a tooltip, or redirect to a 403 page.

The quality attributes in priority order for this feature are: Correctness > Maintainability > Testability > Developer Experience > Performance.

---

## Decision

**All write controls are hidden (not rendered) from users who lack the required permission.**

Concretely:
- Write controls are wrapped in a conditional expression using `isTeamAdmin(teamId)`, `isPortfolioAdmin(portfolioId)`, or `isSystemAdmin` from the `useRbac()` hook.
- When the condition is false, the JSX element is not rendered — no `disabled` prop, no tooltip, no placeholder.
- This applies to: Add/Edit/Delete delivery buttons, Update All, Clone, Delete, Reload buttons on team/portfolio detail; Quick Settings bars; Add Connection, Add Team, Add Portfolio buttons in the Overview (when the user lacks the requisite permission); Log Level and WTS connections sections in System Settings.

---

## Alternatives Considered

### Option A: Disable with tooltip (rejected)

Controls remain rendered but have `disabled={true}` and a tooltip such as "You don't have permission to do this".

**Rejected because**:
- Disabled controls invite curiosity and accidental click attempts, especially for users unfamiliar with the RBAC model.
- Accessibility: disabled interactive elements are still present in the DOM and accessible tree, requiring additional ARIA to communicate their purpose.
- Tooltip text adds maintenance burden: every guarded control needs a bespoke message.
- Controls that are disabled due to permission look identical to controls disabled for functional reasons (e.g., "Add Team" disabled because no connections exist), creating confusion.
- Testing is harder: the test must assert both "disabled" state and absence of 403 errors.

### Option B: Redirect to 403 page on access attempt (rejected)

Controls are visible. Clicking them redirects to a 403 "Access Denied" page.

**Rejected because**:
- Users encounter error states as part of normal navigation — this violates the Viewer experience quality objective (0 error states).
- Server-side 403 is already enforced (defence in depth). A UI-level 403 redirect adds no security value and degrades UX.
- Full-page redirect interrupts context; user loses their place.

### Option C: Hide controls (selected)

Controls are conditionally rendered and simply absent when the user lacks permission.

**Accepted because**:
- Zero error exposure: users never click something that returns a failure.
- Clean, role-appropriate interface: the UI shows exactly what the user can do.
- Simpler implementation: no tooltip text to maintain, no disabled-state styling to test.
- Consistent with established Material UI patterns in the codebase (the `visibleTabs` filter in `Settings.tsx` already hides tabs rather than disabling them).
- Testable: E2E tests simply assert element absence.

---

## Consequences

**Positive**:
- Viewer experience is clean and error-free (KPI: 0 error states triggered during Viewer navigation).
- Simplified component code: a single conditional expression per control, no tooltip component.
- E2E tests are straightforward: `await expect(element).not.toBeVisible()`.

**Negative**:
- A Viewer who wonders "why can't I update this team?" has no in-app explanation. Accepted trade-off: the Overview page already shows "no access" guidance when the user has no assignments, and the general product documentation covers this.
- If a user's role changes mid-session, hidden controls do not reappear until the page is refreshed (authorization summary is fetched once on mount). Accepted: role changes are administrative acts; a re-login or refresh is a reasonable expectation.

**Quality attribute impact**:
- Correctness: improved — no UI path leads to a 403 error for a normal Viewer session.
- Maintainability: improved — one pattern ("use `useRbac()` conditional") for all guarded controls.
- Testability: improved — element absence is easier to assert than disabled state.
