# Feature Delta: rbac-enhancements

<!-- markdownlint-disable MD024 -->

Wave: DISCUSS | Date: 2026-05-10 | Persona: Luna (Product Owner)

---

## Wave: DISCUSS / [REF] Phase 1 — JTBD Analysis

### Persona Overview

Four RBAC personas have been identified and grounded in jobs-to-be-done:

| Persona | Primary Job | Opportunity Gap |
|---|---|---|
| First-Time System Admin | Bootstrap RBAC as the first administrator | 4/5 (blocker — nothing works without this) |
| System Admin (ongoing) | Manage who can access teams and portfolios | 3/5 |
| Team/Portfolio Admin | Administer own scope without System Admin help | 3/5 |
| Viewer | Understand what I can and cannot do in Lighthouse | 3/5 |

All four jobs are documented in `docs/product/jobs.yaml`.

### Job: Bootstrap RBAC (job-rbac-bootstrap)

**Job Story**: When I am the first person to configure Lighthouse with OIDC enabled and no System Admin exists, I want to claim the System Admin role and designate my SSO group, so I can take ownership without external help.

**Four Forces**:
- Push: No admin exists — users are blocked from uploading a license or configuring anything; must resolve before rollout
- Pull: Single in-app action grants immediate control without IT ticket or config file edit
- Anxiety: What if I bootstrap incorrectly? What if the emergency admin falls through? What if the license cannot be uploaded before a System Admin exists?
- Habit: Developers expect to edit a config file or environment variable to set the first admin

**Opportunity Score**: Importance 5, Satisfaction 1 — Gap 4 (blocker)

### Job: Manage Users and Groups (job-rbac-manage-users)

**Job Story**: When I need to control which colleagues can view or administer specific teams/portfolios, I want to assign individual users or SSO groups to scoped roles, so I can enforce least-privilege access without modifying the identity provider.

**Four Forces**:
- Push: Colleagues from other teams can see each other's delivery forecasts when RBAC is off — confidentiality concern
- Pull: Scoped roles mean team admins self-serve their own membership
- Anxiety: Will granting the wrong role silently give write access? What if I forget to remove a departed employee? Will group-based rights behave differently from individual rights?
- Habit: Admins manage access via Keycloak/AAD groups and expect Lighthouse to trust those groups completely

**Opportunity Score**: Importance 5, Satisfaction 2 — Gap 3

### Job: Scoped Self-Service (job-rbac-scoped-admin)

**Job Story**: When I am the owner of a specific team or portfolio, I want to manage member roles independently, so I can onboard colleagues without waiting for the System Admin.

**Four Forces**:
- Push: Waiting for System Admin to grant access delays onboarding; frustrates new members
- Pull: Access management is in the same UI as team settings — one coherent workflow
- Anxiety: Can I accidentally grant someone access to a portfolio I don't own? Will errors in the access groups UI break my team configuration?
- Habit: Team leads rely on the identity provider or email IT to manage group membership

**Opportunity Score**: Importance 4, Satisfaction 1 — Gap 3

### Job: Viewer Clarity (job-rbac-viewer-clarity)

**Job Story**: When I log in with read-only access, I want the interface to show exactly what's visible to me and hide controls I cannot use, so I can navigate confidently without confusing errors.

**Four Forces**:
- Push: Currently, viewers see error messages, broken "Update all" buttons, and inaccessible settings tabs — eroding trust
- Pull: Clean read-only interface that focuses on forecasting data relevant to the viewer
- Anxiety: What if I inadvertently trigger a data refresh I have no rights to? What if I see partial data and draw wrong conclusions?
- Habit: Non-admin users are used to seeing everything (RBAC disabled mode) and may be confused when elements disappear

**Opportunity Score**: Importance 4, Satisfaction 1 — Gap 3

---

## Wave: DISCUSS / [REF] Phase 1.5 — Scope Assessment

### Scope Assessment: SPLIT RECOMMENDED — 4 coherent clusters, ~22 stories, spanning 5 bounded contexts

**Bounded contexts touched**:
1. Authorization / RBAC administration (bootstrap, user management, group mappings)
2. System Settings UI gating (tabs visibility, chip removal, emergency admin display)
3. Overview Dashboard UI gating (sections, buttons, onboarding stepper)
4. Team/Portfolio detail gating (settings tab, access tab, clone/delete, deliveries)
5. E2E test coverage (all 7 scenarios from the scaffold)

**Oversized signals present (3 of 5)**:
- Estimated stories: ~22 (exceeds 10)
- Bounded contexts: 5 (exceeds 3)
- Multiple independent user outcomes: bootstrap / user-management / scoped-admin / viewer-clarity all ship independently

**Decision**: Feature is split into 4 independent delivery slices. Each slice is a coherent vertical end-to-end value delivery. The slices are defined in Phase 2.5.

---

## Wave: DISCUSS / [REF] Phase 2 — Comprehensive UX Journey Design

### Design Principle: Web Application (React/MUI)

Platform patterns applied: side navigation (System Settings), tabs for sub-sections (team/portfolio detail), table patterns for user management, progressive disclosure for RBAC status diagnostics, toast/snackbar for action confirmations, modal dialogs for destructive actions.

Emotional arc pattern applied: **Confidence Building** (Uncertain → Focused → Confident) for the bootstrap journey; **Problem Relief** (Frustrated → Hopeful → Relieved) for the viewer cleanup journey.

Full journey schema: `docs/product/journeys/rbac-enhancements.yaml`

### The 19 Design Decisions — Resolved

All 19 open questions from the E2E test scaffold are resolved here. Each decision is authoritative for all downstream waves.

| # | Question | Decision | Rationale |
|---|---|---|---|
| Q1 | How to upload license if no sys admin configured yet? | License upload is pre-auth, accessible from the blocked page. A specific banner guides the user after login to bootstrap. | Separates the two unrelated gates — license (product) and RBAC (access control) |
| Q2 | Emergency user — show "Emergency System Admin" instead of "Yes"? | Show three states: "Yes", "Emergency Admin" (with lock icon, non-revocable via UI), "No" | Prevents accidental revocation of the emergency fallback |
| Q3 | Don't show onboarding (or only for sys admins)? | Onboarding stepper shown only to users with canCreateTeam or canCreatePortfolio. Hidden from Viewers. | Reduces noise for read-only users; onboarding is only relevant if you can take action |
| Q4 | Should there be a specific right to create teams/portfolios? | Yes — canCreateTeam / canCreatePortfolio gate the Add buttons. Default false for non-System-Admins. System Admins always true. | Prevents accidental entity creation by scoped admins |
| Q5 | Remove chips from System Admins settings page? | Yes — replace with a single collapsed "RBAC Status" disclosure panel | Reduces cognitive load for day-to-day admin work |
| Q6 | Remove users from system (housekeeping)? | Yes — add "Remove from Lighthouse" action in System Admin's user table, guarded by confirmation dialog | Essential for GDPR / departed-employee hygiene |
| Q7 | Hide Work Tracking Systems, Teams, Portfolios in Overview if none (authz mode)? | Work Tracking Systems section hidden for non-System-Admins. Teams/Portfolios sections hidden if user has no access to any. | Prevents empty/error states from confusing Viewers |
| Q8 | License Info — read-only if not sys admin? | License Info visible to all authenticated users (read-only). Upload button shown only to System Admins. | Transparency about license status; action gate prevents accidental changes |
| Q9 | "Update all" — specifically for teams/portfolios user has write access? | "Update All" only shown when user has Admin rights for that entity. Hidden from Viewers. | Prevents 403 errors from confusing Viewers |
| Q10 | Reload data — don't show if not admin? | "Reload data" hidden from Viewers in team/portfolio detail. Visible only to Admins for that scope. | Same principle as Q9 — hide unactionable controls |
| Q11 | Deliveries visible for viewers, but not editable? | Deliveries tab visible to Viewers (read-only). Add/Edit/Delete actions hidden. | Delivery forecasts are the primary value for Viewers |
| Q12 | Quick Settings for Viewers — don't show? | Quick Settings hidden from Viewers. Visible to Admins for their scope only. | Configuration actions require write access |
| Q13 | Create Team/Portfolio broken for non-sys-admins (thinks no connections)? | For non-System-Admins, the connections-required check is bypassed since they cannot see connections anyway. canCreateTeam/Portfolio being false means the button is hidden — the disabled-check never fires. | Removes the confusing "create a connection first" blocker for scoped admins |
| Q14 | Team Admin — "Failed to load team access groups" error? | ScopedGroupMappingManager must call the scoped endpoint /authorization/teams/{teamId}/group-mappings instead of the global endpoint. | Scoped endpoint uses CanManageTeamMembership not CanManageRbac |
| Q15 | Log Level not visible for anyone? | Log Level visible ONLY to System Admins. Hidden for all other roles. | Configuration setting with no value for non-admins |
| Q16 | Clone/Delete don't work for Team/Portfolio Admin? | Clone and Delete require Admin rights for that scope. Team Admins can Clone/Delete their own team(s). Viewers see neither button. | Consistent with the principle: you can manage what you own |
| Q17 | Settings not visible for team/portfolio admin? | Settings sub-page visible to Team/Portfolio Admins for their own scope. Hidden from Viewers. | Self-service configuration is part of the Admin role |
| Q18 | Get Work Tracking System Connections — allowed for all? | No — restricted to System Admins. Non-admins get 403 (silently handled). No connections section rendered. | Least-privilege; connection config has no viewer value |
| Q19 | Show "Access" tab / "System Admins" in Settings only if RBAC enabled? | Yes — Access tab in team/portfolio detail and System Admins tab in Settings are only rendered when isRbacEnabled is true. | Reduces clutter in non-RBAC deployments |

---

## Wave: DISCUSS / [WHY] Phase 2.5 — User Story Mapping

### Backbone (across all 4 personas)

```
[Bootstrap &     [Manage System    [Scoped Team/    [Viewer         [E2E Test
 First Admin]     Admins & Users]   Portfolio        Experience]     Coverage]
                                    Access]
─────────────────────────────────────────────────────────────────────────────
Walking           Manage user       Manage team      Read-only       Smoke test
Skeleton:         table; grant/     membership;      overview;       bootstrap +
bootstrap as      revoke admin;     settings tab     no admin        viewer
first admin;      emergency admin   visible          controls        access
set SSO group     display
─────────────────────────────────────────────────────────────────────────────
Release 1:        Remove user       Access tab        Deliveries     Scenario 1-4
License           housekeeping;     (RBAC Status);    read-only;     E2E tests
upload            chip removal;     group mapping     Quick
pre-auth          status panel      (scoped)          Settings
                                                      hidden
─────────────────────────────────────────────────────────────────────────────
Release 2:        canCreate         Clone/Delete;     Overview        Scenario 5-7
                  gates;            Reload;           sections        E2E tests
                  Q19 (Access tab   Update All        gated; Log
                  visibility)       gated             Level hidden
```

### Walking Skeleton

End-to-end flow proving RBAC works:
1. US-01: First admin self-bootstraps (Q1 banner + bootstrap action + SSO group)
2. US-02: Emergency admin displayed distinctly (Q2)
3. US-07: Viewer sees clean read-only overview (Q7, Q12, Q18)
4. US-11: E2E tests scenarios 1-4

### Slices

Four delivery slices aligned to the split decision in Phase 1.5:

**Slice 01 — Bootstrap & Initial Admin Setup** (Resolves Q1, Q2, Q3, Q4, Q5)
Learning hypothesis: A first-time System Admin can fully initialise RBAC in under 5 minutes without external help.

**Slice 02 — System Admin User/Group Management** (Resolves Q6, Q19, ongoing admin operations)
Learning hypothesis: System Admins can housekeep departed users and maintain SSO group mappings without support tickets.

**Slice 03 — Scoped Team/Portfolio Access** (Resolves Q9, Q10, Q13, Q14, Q16, Q17)
Learning hypothesis: Team/Portfolio Admins can manage their own scope's membership and settings independently.

**Slice 04 — Viewer Experience Polish** (Resolves Q7, Q8, Q11, Q12, Q15, Q18)
Learning hypothesis: Viewers navigate Lighthouse without encountering errors or admin controls that do not apply to them.

---

## Wave: DISCUSS / [REF] Phase 3 — User Stories

### System Constraints

The following constraints apply across all user stories in this feature:

- **RBAC is a Premium feature gate**: All RBAC behaviour (role checks, scoped views) is only active when `isRbacEnabled === true` in `UserAuthorizationSummary`. When RBAC is disabled, the system behaves as fully permissive (existing behaviour unchanged).
- **Permissive fallback**: The `useRbac` hook defaults to `PERMISSIVE_SUMMARY` on error, ensuring a failed RBAC call never locks users out. This invariant must not be broken.
- **Authorization summary cache**: `GET /api/latest/authorization/my-summary` is called on login and re-called after any role mutation. All UI gating derives from this single summary.
- **Emergency admin is config-only**: The emergency admin subject is set in `appsettings.json` / environment variable. It cannot be created or revoked via the UI. The UI must reflect its presence but not offer a revoke action.
- **Group-based rights are behaviourally identical to individual rights**: Scenario 7 (E2E) must be verified to produce identical behaviour to Scenario 6.

---

### US-01: First Admin Bootstrap and SSO Group Assignment

**Slice**: 01 — Bootstrap & Initial Admin Setup
**Job ID**: job-rbac-bootstrap
**Priority**: P1 (Walking Skeleton)

#### Elevator Pitch
Before: The first person to set up Lighthouse with OIDC has no way to claim admin rights from within the app — they must edit config files or escalate to IT.
After: After logging in, they navigate to System Settings → Access and click "Become First System Admin" — the page confirms their new role and shows them the SSO group mapping form.
Decision enabled: The admin decides which SSO group should receive automatic System Admin elevation for future logins.

#### Problem
Alex Chen is an IT lead who has deployed Lighthouse with OIDC authentication. They find it impossible to complete RBAC setup because no in-app mechanism exists to establish the first System Admin — they must edit server configuration, restart the service, and hope nothing breaks. Meanwhile, their colleagues cannot use RBAC at all.

#### Who
- IT Lead / DevOps engineer deploying Lighthouse for the first time | OIDC configured, no prior Lighthouse admin experience | Motivated to get RBAC running before user rollout

#### Solution
A "Become First System Admin" button in System Settings → Access, visible only when no System Admin exists. One click + confirmation grants the current user System Admin rights. Immediately followed by the SSO group mapping form to automate future admin elevation.

#### Domain Examples

**Example 1 — Happy path**: Alex Chen (alex@company.com) logs in to a fresh Lighthouse instance with OIDC. No System Admin exists. Alex navigates to System Settings → Access and sees the "No System Admin assigned" banner. Alex clicks "Become First System Admin", and within 2 seconds the banner disappears and Alex appears in the System Admins table with role "Yes". Alex then types "lighthouse-admins" in the SSO group field and clicks "Add Mapping".

**Example 2 — License upload before bootstrap**: Sam Lee visits the blocked page because no valid license exists. Sam sees a specific informational banner: "No System Admin is configured. Log in and visit System Settings → Access to initialise access control." Sam uploads the license (pre-auth action), then logs in and proceeds to bootstrap.

**Example 3 — Bootstrap race condition**: Two IT admins Alex and Jordan both discover the bootstrap button at the same time. Alex clicks first and succeeds. Jordan clicks simultaneously and sees "Another admin has already been assigned. Reload the page." — Jordan reloads and sees Alex in the table.

#### UAT Scenarios (BDD)

```gherkin
Scenario: First admin self-bootstraps with no prior System Admin
  Given Lighthouse has OIDC enabled and no System Admin has been assigned
  And Alex Chen is authenticated
  When Alex navigates to System Settings → Access
  Then a banner reads "No System Admin is assigned yet"
  And a "Become First System Admin" button is visible
  When Alex clicks "Become First System Admin"
  Then Alex appears in the System Admins table with "System Admin: Yes"
  And the bootstrap banner is no longer visible

Scenario: Bootstrap blocked when user has no stable identity claim
  Given Lighthouse has OIDC enabled and no System Admin exists
  And the authenticated user's OIDC token has no stable subject claim
  When the user clicks "Become First System Admin"
  Then an error message reads "Your account cannot be used to bootstrap (missing stable user identifier). Contact your identity provider administrator."
  And no System Admin is assigned

Scenario: Bootstrap blocked when System Admin already exists (race condition)
  Given two administrators attempt bootstrap simultaneously
  And the first succeeds
  When the second clicks "Become First System Admin"
  Then an error message reads "Another admin has already been assigned. Reload the page."
  And the page shows the existing System Admin on reload

Scenario: System Admin assigns SSO group for automatic admin elevation
  Given Alex is System Admin and the SSO group mapping section is visible
  When Alex enters "lighthouse-admins" in the Group Value field
  And clicks "Add Mapping"
  Then "lighthouse-admins → SystemAdmin (System)" appears in the SSO Groups table
  And the group value input is cleared

Scenario: License upload informational banner shows before bootstrap
  Given no valid license is uploaded and no System Admin exists
  When an unauthenticated user visits the blocked/login page
  Then a banner explains "No System Admin is configured — log in and visit System Settings → Access to initialise access control"
```

#### Acceptance Criteria
- [ ] "Become First System Admin" button is visible in System Settings → Access when and only when no System Admin exists and RBAC has not yet been bootstrapped
- [ ] Clicking the button and succeeding: current user appears in the System Admins table with "Yes" status; bootstrap banner disappears
- [ ] Clicking the button when user has no stable subject: inline error message in plain language; no state change
- [ ] Concurrent bootstrap race: second caller receives a clear message instructing them to reload
- [ ] SSO group mapping: adding a group creates a row in the SSO Groups table; input clears after save
- [ ] License upload page shows an informational banner about bootstrap when no System Admin is configured

#### Outcome KPIs
- Who: IT leads deploying Lighthouse for the first time
- Does what: Complete RBAC bootstrap (first admin + SSO group) without support tickets or config file edits
- By how much: 100% of new deployments complete bootstrap within the app (0 config-file workarounds)
- Measured by: Support ticket volume for "how do I set the first admin"
- Baseline: All bootstrap currently requires server config edit

#### Technical Notes
- Backend: `POST /api/latest/authorization/bootstrap/system-admin` — 204 success, 403 no stable subject, 409 already bootstrapped
- Frontend: `RbacSettings.tsx` already has the bootstrap button; banner logic needs the "license upload hint" copy on `BlockedPage`
- Q1 resolution: license upload on `BlockedPage` is pre-auth and independent of RBAC bootstrap
- Q2 is a dependency (Emergency Admin display) — tracked as US-02

---

### US-02: Emergency Admin — Distinct Display and Non-Revocable UI

**Slice**: 01 — Bootstrap & Initial Admin Setup
**Job ID**: job-rbac-manage-users
**Priority**: P1

#### Elevator Pitch
Before: The emergency admin (configured via environment variable) shows as "Yes" in the System Admin column — indistinguishable from a normal admin. A System Admin could accidentally try to revoke it.
After: The emergency admin row shows "Emergency Admin" with a lock icon and a tooltip explaining it is managed via server configuration. The Revoke button is absent for this row.
Decision enabled: System Admins decide whether they need to change the emergency admin (which must go to server config — they now know where).

#### Problem
Sam Lee is the emergency admin configured in `appsettings.json`. When Alex (System Admin) views the user table, Sam shows as "Yes" (System Admin). Alex cannot distinguish Sam from a normal admin, might accidentally revoke Sam's rights, and would break the emergency fallback without knowing it.

#### Who
- System Admin managing the user table | Any deployment using the emergency admin feature | Motivated to maintain correct access state

#### Solution
The `isSystemAdmin` column in the user table uses three display states: "Yes" (normal), "Emergency Admin" (with lock icon), "No". The "Emergency Admin" state has no Revoke button — a tooltip reads "This is an emergency administrator configured via server settings. To change it, update the application configuration."

#### Domain Examples

**Example 1 — Happy path**: Alex opens the System Admins table. Sam Lee's row shows "Emergency Admin 🔒" in the System Admin column. No Revoke button appears on Sam's row. Hovering over the lock icon shows "Managed via server configuration".

**Example 2 — Normal admin next to emergency**: The table shows Alex (Yes, [Revoke]), Sam (Emergency Admin 🔒, no button), and Jo (No, [Grant]). Alex can revoke Jo or grant Jo without confusion.

**Example 3 — Emergency admin is the only admin**: If the emergency admin is the only admin (no other System Admin exists), the table shows one row with "Emergency Admin 🔒". The bootstrap button is hidden (a System Admin exists — even if only emergency).

#### UAT Scenarios (BDD)

```gherkin
Scenario: Emergency admin displayed distinctly in user table
  Given Sam Lee is the emergency admin configured in server settings
  And Alex is a normal System Admin
  When Alex views the System Admins table
  Then Sam's "System Admin" column shows "Emergency Admin" with a visual indicator
  And no "Revoke" button is present on Sam's row
  And Alex's row shows "Yes" with a "Revoke" button

Scenario: Emergency admin row shows explanatory tooltip
  Given Sam's row shows the Emergency Admin indicator
  When Alex hovers over the indicator on Sam's row
  Then a tooltip reads "This is an emergency administrator configured via server settings. To change it, update the application configuration."

Scenario: Bootstrap banner suppressed when only emergency admin exists
  Given Sam is configured as emergency admin and no normal System Admin exists
  When a new user logs in
  Then the bootstrap banner ("No System Admin is assigned yet") is NOT shown
  And the user cannot bootstrap (a System Admin — even emergency — already exists)
```

#### Acceptance Criteria
- [ ] Emergency admin user row shows "Emergency Admin" text and a visual lock indicator instead of "Yes"
- [ ] No Revoke button is rendered on the emergency admin row
- [ ] A tooltip on the indicator explains that the emergency admin is managed via server configuration
- [ ] Bootstrap banner is suppressed when an emergency admin exists (hasSystemAdmin is true)
- [ ] Normal admin rows continue to show "Yes" and a Revoke button

#### Outcome KPIs
- Who: System Admins managing the user table
- Does what: Identify emergency admins without confusion; never accidentally attempt to revoke emergency admin
- By how much: 0 support tickets for "accidentally removed emergency admin"
- Measured by: Support ticket analysis
- Baseline: Currently indistinguishable (potential confusion)

#### Technical Notes
- Backend: `RbacUserSummary` needs an `isEmergencyAdmin` boolean field
- Frontend: The `isSystemAdmin` cell in the user table needs to branch on `isEmergencyAdmin`
- Q2 resolution: display "Emergency Admin" with lock icon, no revoke button

---

### US-03: RBAC Status — Diagnostic Panel Replaces Chips

**Slice**: 01 — Bootstrap & Initial Admin Setup
**Job ID**: job-rbac-manage-users
**Priority**: P2

#### Elevator Pitch
Before: Six status chips crowd the top of the System Admins settings page at all times — RBAC enabled/disabled, premium gate, emergency admin, ready status, unassigned count, group claim name — overwhelming admins who just want to manage users.
After: The chips are replaced by a single collapsed "RBAC Status" disclosure section. Admins who need diagnostic information expand it; the default view is clean.
Decision enabled: System Admins decide when to inspect detailed RBAC status (diagnostics) vs. focusing on user management (daily operations).

#### Problem
Alex (System Admin) visits System Settings → Access to check if a new user has the right role. Six status chips are shown above the table regardless of the task at hand. They clutter the page and force Alex to parse them before finding the user table.

#### Who
- System Admin | Day-to-day user management tasks | Motivated to manage users efficiently without configuration noise

#### Solution
Remove the six chips. Add a collapsed `<details>` / accordion labelled "RBAC Status" that expands to show the same diagnostic information. Default state is collapsed.

#### Domain Examples

**Example 1 — Default collapsed state**: Alex opens System Settings → Access. The page shows the user table immediately. A subtle "▶ RBAC Status" row sits above, collapsed. Alex does not need to parse chips.

**Example 2 — Expanding for diagnostics**: During a support call, Alex needs to confirm the group claim name. Alex clicks "▶ RBAC Status" — it expands to show the six status fields. Alex copies the group claim name and collapses the panel.

**Example 3 — Unassigned count visible when non-zero**: When there are unassigned users, the collapsed label shows a badge: "▶ RBAC Status (5 unassigned users)". Alex knows to investigate without expanding.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Status chips replaced by collapsed diagnostic panel
  Given Alex is System Admin and navigates to System Settings → Access
  When the page loads
  Then no status chips are visible at the top of the page
  And a collapsed "RBAC Status" disclosure panel is visible

Scenario: Diagnostic panel expands to show status details
  Given the RBAC Status panel is collapsed
  When Alex clicks on the "RBAC Status" panel
  Then it expands showing: RBAC enabled/disabled, premium gate, emergency admin, ready status, unassigned count, group claim name

Scenario: Unassigned user count badge shown on collapsed panel
  Given there are 5 unassigned users
  When Alex views System Settings → Access with the panel collapsed
  Then the panel label shows a count indicator for unassigned users
```

#### Acceptance Criteria
- [ ] Six status chips removed from the System Settings → Access page
- [ ] A collapsed disclosure panel labelled "RBAC Status" replaces them
- [ ] Expanding the panel reveals the same 6 status fields (enabled, premium gate, emergency admin, ready, unassigned count, group claim)
- [ ] When unassigned user count is > 0, the collapsed label shows the count
- [ ] Default state is collapsed

#### Outcome KPIs
- Who: System Admins using daily user management
- Does what: Reach the user table without parsing diagnostic chips
- By how much: Reduce time-to-first-user-action by removing visual overhead
- Measured by: Usability observation (qualitative)
- Baseline: 6 chips always visible

#### Technical Notes
- Pure frontend change in `RbacSettings.tsx`
- Q5 resolution

---

### US-04: System Admin Housekeeping — Remove Departed Users

**Slice**: 02 — System Admin User/Group Management
**Job ID**: job-rbac-manage-users
**Priority**: P2

#### Elevator Pitch
Before: When a colleague leaves the organisation, their Lighthouse user record and all role assignments persist forever. System Admins have no way to remove them from within the app.
After: A "Remove" button in the user table lets System Admins delete departed users and all their role assignments after confirming a dialog.
Decision enabled: System Admin decides whether to keep or remove a departed user's record, understanding it will remove all their role assignments.

#### Problem
Pat Wilson left the company 3 months ago. Pat still appears in the System Admins user table marked "unassigned". Alex (System Admin) wants to clean up the list for GDPR compliance and reduce confusion but has no in-app action to do so.

#### Who
- System Admin | Periodic housekeeping, triggered by offboarding notifications | Motivated to maintain clean user records for compliance and clarity

#### Solution
A "Remove" button in the user table for each non-System-Admin user (and unassigned users). Clicking opens a confirmation dialog: "Remove [Name]? This will delete all their role assignments. This cannot be undone." Confirming removes the user record and all scoped roles.

#### Domain Examples

**Example 1 — Remove unassigned user**: Pat Wilson shows as unassigned in the user table. Alex clicks "Remove" on Pat's row. A dialog shows "Remove Pat Wilson? This will delete all their role assignments and they will no longer appear in Lighthouse. This cannot be undone." Alex confirms. Pat disappears from the table.

**Example 2 — Remove user with role assignments**: Morgan Davies has TeamAdmin for Team Alpha and Viewer for Portfolio A. Alex clicks "Remove" on Morgan's row. The confirmation dialog lists: "This will remove: Team Alpha (TeamAdmin), Portfolio A (Viewer)." Alex confirms. Morgan is removed from all lists.

**Example 3 — Cannot remove the last System Admin**: Alex tries to remove themselves (the only System Admin). The Remove button is absent on their own row. (Self-removal is blocked at the backend; the frontend pre-empts it.)

#### UAT Scenarios (BDD)

```gherkin
Scenario: System Admin removes a departed unassigned user
  Given Pat Wilson appears in the user table as unassigned
  When Alex clicks "Remove" on Pat's row
  And confirms the confirmation dialog
  Then Pat Wilson is removed from the user table
  And Pat has no further access to Lighthouse

Scenario: Remove user with existing role assignments
  Given Morgan Davies has TeamAdmin on "Team Alpha" and Viewer on "Portfolio A"
  When Alex clicks "Remove" on Morgan's row
  Then the confirmation dialog lists Morgan's role assignments
  When Alex confirms
  Then Morgan is removed from the user table
  And Morgan's role assignments are deleted
  And Morgan can no longer access Team Alpha or Portfolio A

Scenario: Remove button absent for current user
  Given Alex is the only System Admin
  When Alex views the user table
  Then no "Remove" button appears on Alex's own row

Scenario: Remove dialog is cancellable
  Given Alex clicks "Remove" on Pat's row
  When Alex clicks Cancel in the confirmation dialog
  Then Pat remains in the user table with their state unchanged
```

#### Acceptance Criteria
- [ ] A "Remove" button appears in the user table for users that are NOT the current user and NOT the emergency admin
- [ ] Clicking Remove opens a confirmation dialog naming the user and listing their role assignments
- [ ] Confirming the dialog removes the user record and all their role assignments
- [ ] Cancelling leaves the user unchanged
- [ ] After removal the user disappears from the table immediately
- [ ] The "Remove" button does not appear on the current logged-in user's row or the emergency admin row

#### Outcome KPIs
- Who: System Admins performing offboarding housekeeping
- Does what: Remove departed users without database access or API calls
- By how much: 100% of offboarding can be completed in-app (0 manual DB cleanups)
- Measured by: Support requests for user removal
- Baseline: No in-app mechanism; requires direct DB access

#### Technical Notes
- Backend: new DELETE endpoint or leveraging existing patterns — needs design in DESIGN wave
- Q6 resolution

---

### US-05: Access Tab Visibility Gated by RBAC Enabled State

**Slice**: 02 — System Admin User/Group Management
**Job ID**: job-rbac-manage-users
**Priority**: P2

#### Elevator Pitch
Before: The "Access" sub-tab in team/portfolio detail pages and the "System Admins" tab in System Settings are always rendered, even in deployments where RBAC is disabled — cluttering the UI with non-functional sections.
After: These tabs are only rendered when isRbacEnabled is true. Deployments without RBAC see a clean, uncluttered navigation.
Decision enabled: The admin sees only relevant navigation for their deployment's configuration.

#### Problem
A Lighthouse deployment running without OIDC/RBAC shows "System Admins" in System Settings and "Access" in every team/portfolio detail page. These sections serve no purpose and confuse users who wonder if they need to configure something.

#### Who
- All users in non-RBAC deployments | System Admins in RBAC-enabled deployments checking which tabs should appear | Motivated to understand the system's current configuration

#### Solution
Conditional rendering: `isRbacEnabled` from `UserAuthorizationSummary` gates the rendering of the "System Admins" tab in System Settings and the "Access" sub-tab in team/portfolio detail pages.

#### Domain Examples

**Example 1 — RBAC disabled deployment**: A small team using Lighthouse without OIDC. System Settings shows General, Work Tracking Systems, Demo Data — no "System Admins" tab. Team detail shows Overview, Forecast, Deliveries, Settings — no "Access" tab.

**Example 2 — RBAC enabled deployment**: The same instance after OIDC is configured and `isRbacEnabled` is true. System Settings now shows a "System Admins" tab. Team detail shows the "Access" sub-tab.

**Example 3 — Viewer in RBAC enabled deployment**: Morgan (Viewer) sees the Access tab in Team Alpha's detail page (it is rendered) but cannot modify anything — the tab is read-only for Viewers (enforced via separate US-09).

#### UAT Scenarios (BDD)

```gherkin
Scenario: Access tab hidden when RBAC is disabled
  Given RBAC is disabled (isRbacEnabled is false)
  When any user navigates to a team or portfolio detail page
  Then the "Access" sub-tab is not rendered

Scenario: System Admins tab hidden when RBAC is disabled
  Given RBAC is disabled
  When any user navigates to System Settings
  Then the "System Admins" tab is not rendered in the settings navigation

Scenario: Access tab visible when RBAC is enabled
  Given RBAC is enabled (isRbacEnabled is true)
  When a user navigates to a team or portfolio detail page
  Then the "Access" sub-tab is rendered
  And the "System Admins" tab is visible in System Settings
```

#### Acceptance Criteria
- [ ] "Access" sub-tab in team detail page is rendered only when `isRbacEnabled === true`
- [ ] "Access" sub-tab in portfolio detail page is rendered only when `isRbacEnabled === true`
- [ ] "System Admins" tab in System Settings is rendered only when `isRbacEnabled === true`
- [ ] When RBAC is disabled, the remaining tabs remain unchanged

#### Outcome KPIs
- Who: All Lighthouse users in non-RBAC deployments
- Does what: Navigate the application without encountering RBAC-only sections that do nothing
- By how much: 0 confusion reports about "System Admins" tab in non-RBAC deployments
- Measured by: Support ticket analysis
- Baseline: Tabs always visible regardless of RBAC state

#### Technical Notes
- Pure frontend gating via `isRbacEnabled` from `useRbac()`
- Q19 resolution

---

### US-06: Scoped Team/Portfolio Admin — Settings and Access Tabs

**Slice**: 03 — Scoped Team/Portfolio Access
**Job ID**: job-rbac-scoped-admin
**Priority**: P2

#### Elevator Pitch
Before: Team Admins and Portfolio Admins navigate to their team/portfolio and find neither the Settings nor the Access sub-tabs — only System Admins see those. Scoped admins cannot configure or manage their own entities.
After: Settings and Access tabs are visible to Team/Portfolio Admins for their own scope, enabling full self-service configuration and membership management.
Decision enabled: Team Admin decides whether to accept a new team member or update team settings — without waiting for System Admin involvement.

#### Problem
Jordan Reyes is TeamAdmin for Team Alpha. Jordan navigates to Team Alpha's detail page but sees no Settings or Access tab — these are currently gated to System Admins only. Jordan cannot add a new team member or update a forecast setting without escalating to Alex (System Admin), creating a bottleneck.

#### Who
- Team Admin or Portfolio Admin | Managing their own entity's settings and membership | Motivated to be self-sufficient and avoid escalation delays

#### Solution
Settings and Access tabs in team/portfolio detail pages are visible to users whose `isTeamAdmin(teamId)` or `isPortfolioAdmin(portfolioId)` returns true. The tabs are hidden from Viewers. System Admins see them everywhere.

#### Domain Examples

**Example 1 — Team Admin sees their tabs**: Jordan (TeamAdmin for Team Alpha) opens Team Alpha. The tab bar shows: Overview | Forecast | Deliveries | Settings | Access. Jordan opens Settings and updates the forecast configuration. Jordan opens Access and adds a new team member.

**Example 2 — Team Admin cannot see another team's tabs**: Jordan opens Team Beta (not their team). The tab bar shows only: Overview | Forecast | Deliveries. Settings and Access are not rendered.

**Example 3 — Viewer sees no Settings or Access tabs**: Morgan (Viewer for Team Alpha) opens Team Alpha. The tab bar shows: Overview | Forecast | Deliveries. No Settings or Access tabs.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Team Admin sees Settings and Access tabs for their own team
  Given Jordan is TeamAdmin for "Team Alpha" and RBAC is enabled
  When Jordan navigates to Team Alpha's detail page
  Then the Settings tab is visible
  And the Access tab is visible

Scenario: Team Admin does not see Settings or Access on a team they don't administer
  Given Jordan is TeamAdmin for "Team Alpha" but not "Team Beta"
  When Jordan navigates to Team Beta's detail page
  Then the Settings tab is not visible
  And the Access tab is not visible

Scenario: Viewer does not see Settings or Access on any team
  Given Morgan is a Viewer for "Team Alpha"
  When Morgan navigates to Team Alpha's detail page
  Then the Settings tab is not visible
  And the Access tab is not visible

Scenario: Portfolio Admin sees Settings and Access for their own portfolio
  Given Sam is PortfolioAdmin for "Portfolio A"
  When Sam navigates to Portfolio A's detail page
  Then the Settings and Access tabs are both visible
```

#### Acceptance Criteria
- [ ] Settings tab in team detail is visible when `isTeamAdmin(teamId)` is true; hidden otherwise
- [ ] Access tab in team detail is visible when `isTeamAdmin(teamId)` is true; hidden otherwise (and also subject to US-05 RBAC enabled gate)
- [ ] Same logic applied for portfolio detail using `isPortfolioAdmin(portfolioId)`
- [ ] System Admins see both tabs on all teams and portfolios
- [ ] Viewers see neither tab on any team or portfolio

#### Outcome KPIs
- Who: Team Admins and Portfolio Admins
- Does what: Configure and manage their own team/portfolio without System Admin escalation
- By how much: Eliminate 100% of escalations for routine team/portfolio configuration
- Measured by: Support ticket volume for "can you add X to my team"
- Baseline: All team/portfolio config requires System Admin

#### Technical Notes
- Frontend only: tab rendering conditionals in team and portfolio detail components
- Uses existing `isTeamAdmin` and `isPortfolioAdmin` from `useRbac()` hook
- Q17 resolution

---

### US-07: Scoped Admin — Clone, Delete, Reload, Update All Controls

**Slice**: 03 — Scoped Team/Portfolio Access
**Job ID**: job-rbac-scoped-admin
**Priority**: P2

#### Elevator Pitch
Before: Clone, Delete, Reload, and Update All controls on team/portfolio pages appear for everyone or for no one — either Viewers see actions they cannot perform (getting errors), or Team Admins are blocked from managing their own entities.
After: These controls are shown only to users with Admin rights for that specific entity. Viewers see a clean read-only view. Team/Portfolio Admins see their management controls only for their own scope.
Decision enabled: Team Admin decides to clone their team configuration for a new project sprint — without involving the System Admin.

#### Problem
Morgan (Viewer for Team Alpha) sees an "Update All" button. Clicking it returns a 403 error. Morgan doesn't know what went wrong. Jordan (TeamAdmin for Team Alpha) sees no "Update All" button — it's gated to System Admins only — so Jordan cannot refresh their team's data.

#### Who
- Team Admin / Portfolio Admin needing CRUD operations on their scope | Viewer who should see a clean read-only view | System Admin who always has full access

#### Solution
Clone, Delete, Update All, and Reload controls use `isTeamAdmin(teamId)` / `isPortfolioAdmin(portfolioId)` as rendering conditions. Viewers see none of these controls. Scoped admins see them for their own scope. System Admins see them everywhere.

#### Domain Examples

**Example 1 — Team Admin sees their controls**: Jordan (TeamAdmin for Team Alpha) opens Team Alpha. The page header shows: [Update All] [Clone] [Delete] [Reload]. Jordan clones Team Alpha to create Team Alpha Q3.

**Example 2 — Viewer sees no controls**: Morgan (Viewer for Team Alpha) opens Team Alpha. No Update All, Clone, Delete, or Reload buttons appear. The page is clean with only forecast data.

**Example 3 — Cross-scope isolation**: Jordan is TeamAdmin for Team Alpha but not Team Beta. Team Alpha shows all controls. Team Beta shows none (Jordan has Viewer-level access for Team Beta by default).

#### UAT Scenarios (BDD)

```gherkin
Scenario: Team Admin sees management controls for their own team
  Given Jordan is TeamAdmin for "Team Alpha"
  When Jordan opens Team Alpha's detail page
  Then Update All, Clone, Delete, and Reload buttons are visible

Scenario: Viewer sees no management controls
  Given Morgan is a Viewer for "Team Alpha"
  When Morgan opens Team Alpha's detail page
  Then Update All, Clone, Delete, and Reload buttons are not visible
  And no 403 error appears

Scenario: Team Admin cannot use management controls on teams they do not administer
  Given Jordan is TeamAdmin for "Team Alpha" but not "Team Beta"
  When Jordan opens Team Beta's detail page
  Then Update All, Clone, Delete, and Reload buttons are not visible for Team Beta

Scenario: Portfolio Admin clone operation succeeds for their portfolio
  Given Sam is PortfolioAdmin for "Portfolio A"
  When Sam clones "Portfolio A"
  Then a new portfolio "Portfolio A (Copy)" is created
  And Sam is the PortfolioAdmin for the new copy
```

#### Acceptance Criteria
- [ ] Update All button is visible only when `isTeamAdmin(teamId)` or `isPortfolioAdmin(portfolioId)` is true
- [ ] Clone button is visible only when admin rights are present for that scope
- [ ] Delete button is visible only when admin rights are present for that scope
- [ ] Reload button is visible only when admin rights are present for that scope
- [ ] Viewers see none of these controls on any entity
- [ ] System Admins see all controls everywhere

#### Outcome KPIs
- Who: Viewers using team/portfolio detail pages
- Does what: Navigate without encountering 403 errors from accidentally clicking admin controls
- By how much: 0 403 errors from admin-only actions triggered by non-admin users
- Measured by: Frontend error logging / API 403 rate from RBAC-scoped endpoints
- Baseline: Viewers occasionally click controls and receive error states

#### Technical Notes
- Q9, Q10, Q16 resolution
- Frontend-only change using `isTeamAdmin` / `isPortfolioAdmin` from `useRbac()` hook

---

### US-08: Scoped Group Mapping — Fix Team Admin "Failed to Load" Error

**Slice**: 03 — Scoped Team/Portfolio Access
**Job ID**: job-rbac-scoped-admin
**Priority**: P1 (bug fix with security implication)

#### Elevator Pitch
Before: When a Team Admin opens their team's Access tab and attempts to view SSO group mappings, they see "Failed to load team access groups" because the component is calling the global group mappings endpoint (System Admin only).
After: The team-scoped SSO group mapping section calls the scoped endpoint, which Team Admins are authorised to access, and group mappings load successfully.
Decision enabled: Team Admin decides which SSO groups to grant access to their team — without System Admin involvement.

#### Problem
Jordan (TeamAdmin for Team Alpha) opens Team Alpha → Access → SSO Groups. The component calls `GET /authorization/group-mappings` (the global endpoint), receives 403, and shows "Failed to load team access groups". Jordan cannot use SSO group management for their own team.

#### Who
- Team Admin or Portfolio Admin | Using the Access tab's group mapping section | Motivated to set up automatic SSO-based team membership

#### Solution
`ScopedGroupMappingManager` must call `GET /authorization/teams/{teamId}/group-mappings` (scoped endpoint) instead of the global endpoint. The scoped endpoint checks `CanManageTeamMembership` — which Team Admins satisfy.

#### Domain Examples

**Example 1 — Group mappings load for Team Admin**: Jordan opens Team Alpha → Access → SSO Groups. The component calls `/authorization/teams/42/group-mappings`. The response returns group mappings scoped to Team Alpha. Jordan sees the existing mappings and can add a new one.

**Example 2 — Team Admin cannot see other teams' mappings**: Jordan cannot access Team Beta's mappings — the API returns 403 for `/authorization/teams/99/group-mappings` (Jordan is not TeamAdmin for Team Beta). The UI shows a 403-specific message: "You do not have permission to manage access groups for this team."

**Example 3 — System Admin sees all scoped mappings**: Alex (System Admin) opens any team's Access tab. The scoped endpoint returns all mappings for that team. Alex can manage any team's group mappings without error.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Team Admin views SSO group mappings for their own team without error
  Given Jordan is TeamAdmin for "Team Alpha" with teamId 42
  When Jordan opens Team Alpha → Access tab → SSO Groups section
  Then the group mappings for Team Alpha load successfully
  And no "Failed to load" error is shown

Scenario: Team Admin adds an SSO group mapping for their team
  Given Jordan is viewing Team Alpha's SSO Groups section
  When Jordan enters "eng-alpha" and selects role "TeamAdmin"
  And clicks Add Mapping
  Then "eng-alpha → TeamAdmin (Team)" appears in the group mappings table

Scenario: 403 error message is actionable when Team Admin lacks permission
  Given Jordan attempts to view Team Beta's group mappings (not their team)
  When the API returns 403
  Then the UI shows "You do not have permission to manage access groups for this team."
  And no generic "Failed to load" error is shown
```

#### Acceptance Criteria
- [ ] `ScopedGroupMappingManager` calls the scoped endpoint `/authorization/teams/{teamId}/group-mappings` (not the global endpoint)
- [ ] Group mappings for Team Admin's own team load without error
- [ ] Adding a group mapping via the scoped UI creates a mapping scoped to that team
- [ ] When the user lacks permission (403), the error message is actionable (not a generic "failed to load")

#### Outcome KPIs
- Who: Team Admins using the Access tab
- Does what: Manage SSO group mappings for their team without error
- By how much: 0 "Failed to load team access groups" errors for legitimate Team Admins
- Measured by: Frontend error logging / user report tracking
- Baseline: Error always appears for Team Admins attempting to use this feature

#### Technical Notes
- Q14 resolution
- Backend: verify scoped GET endpoint exists for team group mappings; may need to be added
- Frontend: update API call in `ScopedGroupMappingManager`

---

### US-09: Viewer Experience — Read-Only Delivery and No Admin Controls

**Slice**: 04 — Viewer Experience Polish
**Job ID**: job-rbac-viewer-clarity
**Priority**: P2

#### Elevator Pitch
Before: Viewers logging in to Lighthouse see a mix of accessible forecast data and inaccessible admin controls (Quick Settings, Update All, Settings tab) — they click buttons and get errors or see sections that confuse them.
After: Viewers see only the Forecast and Deliveries tabs (read-only), with all admin controls hidden. The experience is calm and focused on the data they need.
Decision enabled: Viewer decides to share forecast data with stakeholders, knowing the view they see is exactly what their stakeholders will see — no accidental admin exposure.

#### Problem
Morgan (Viewer for Team Alpha) opens Team Alpha. They see Settings, Access, Quick Settings, Update All, Clone, and Delete — all of which either error or silently fail. Morgan's confidence in the tool is eroded. They cannot tell what they are allowed to do.

#### Who
- Viewer with read-only access to specific teams/portfolios | Checking forecast data | Motivated to extract insight without admin distraction

#### Solution
All write-action controls (Quick Settings, Update All, Clone, Delete, Reload, Add Delivery, Edit Delivery, Delete Delivery) are hidden from Viewers. The Deliveries tab is visible in read-only mode. System Settings shows only License Info (read-only).

#### Domain Examples

**Example 1 — Clean team detail view**: Morgan (Viewer for Team Alpha) opens Team Alpha. Tab bar: Overview | Forecast | Deliveries. Header: no action buttons. Deliveries tab: list of deliveries visible, no Add/Edit/Delete buttons. Morgan reads the data and screenshots it for a stakeholder update.

**Example 2 — System Settings for Viewer**: Morgan navigates to System Settings. Only License Info is visible, showing license status in read-only mode. No System Admins tab, no Log Level section, no Work Tracking System connections.

**Example 3 — Overview for Viewer with no assignments**: Chris (Viewer with no team/portfolio assignments) logs in. The Overview shows the "no access" alert with contact guidance. No teams, portfolios, connections sections are shown. No action buttons.

#### UAT Scenarios (BDD)

```gherkin
Scenario: Viewer sees Deliveries in read-only mode
  Given Morgan is a Viewer for "Team Alpha"
  When Morgan opens Team Alpha → Deliveries tab
  Then delivery items are listed
  And no "Add Delivery", "Edit", or "Delete" buttons are visible

Scenario: Viewer does not see Quick Settings
  Given Morgan is a Viewer for "Team Alpha"
  When Morgan opens Team Alpha's detail page
  Then the Quick Settings widget or panel is not visible

Scenario: Viewer sees License Info as read-only in System Settings
  Given Morgan is a Viewer with no System Admin rights
  When Morgan navigates to System Settings
  Then the License Info section is visible showing license status
  And no "Upload License" button is shown
  And no other settings tabs are visible

Scenario: Viewer with no assignments sees guidance on Overview
  Given Chris is authenticated and has no team or portfolio assignments
  When Chris navigates to the Overview
  Then the "no access" alert is shown with contact guidance
  And no team, portfolio, or connections sections are shown

Scenario: Log Level not visible to Viewer
  Given Morgan is a Viewer
  When Morgan navigates to System Settings
  Then no Log Level section is visible
```

#### Acceptance Criteria
- [ ] Deliveries tab is visible to Viewers but shows no Add/Edit/Delete delivery controls
- [ ] Quick Settings widget is hidden from Viewers
- [ ] System Settings for Viewers shows only License Info (read-only); no Upload button
- [ ] Log Level setting is hidden from non-System-Admins
- [ ] Overview for Viewer with no assignments shows the no-access alert; Work Tracking Systems section is not shown
- [ ] Work Tracking Systems section in Overview is hidden for non-System-Admins

#### Outcome KPIs
- Who: Viewers using team/portfolio pages
- Does what: Complete a forecasting review session without encountering a 403 error or disabled control
- By how much: 0 error states triggered by Viewers during normal navigation
- Measured by: Frontend error boundary triggers / 403 API call rate for Viewer sessions
- Baseline: Multiple error states currently triggered by Viewers clicking accessible but unauthorised controls

#### Technical Notes
- Q7, Q8, Q11, Q12, Q15, Q18 resolution
- Delivery tab edit controls: conditional rendering based on `isTeamAdmin`/`isPortfolioAdmin`
- Quick Settings: conditional on admin rights for that scope
- License Info: always visible; upload button conditional on `isSystemAdmin`
- Log Level: conditional on `isSystemAdmin`
- Create Team/Portfolio button logic fix (Q13): for non-System-Admins, connections check is bypassed since connections always return [] (403 handled silently)

---

### US-10: Create Team/Portfolio — Fix Disabled State for Scoped Admins

**Slice**: 03 — Scoped Team/Portfolio Access
**Job ID**: job-rbac-scoped-admin
**Priority**: P2

#### Elevator Pitch
Before: When a System Admin grants canCreateTeam to a Team Admin, the "Add Team" button appears but remains disabled with the message "Create a Connection first" — because the user cannot see connections (they get a 403 that returns []). The button is misleadingly blocked.
After: For users who cannot see connections, the "connections required" disabled-check is not applied. Users who have canCreateTeam see an active "Add Team" button if they also have permission to configure things.
Decision enabled: The System Admin decides to grant a specific user the ability to create teams, knowing the button will actually work once granted.

#### Problem
Sam (TeamAdmin with canCreateTeam granted by Alex) tries to create a new team. The "Add Team" button is visible but disabled with tooltip "Create a Connection before adding a Team". Sam navigates to connections and gets a blank page (403). Sam cannot proceed and files a support ticket.

#### Who
- Team Admin or Portfolio Admin who has been granted canCreateTeam/canCreatePortfolio | Attempting to use that permission | Motivated to create entities independently

#### Solution
When `!isSystemAdmin`, the `disabled` condition for "Add Team" / "Add Portfolio" buttons uses only the RBAC permission check, not the `!hasConnections` / `!hasTeams` check. The latter is only applied when `isSystemAdmin` (who can also manage connections). For non-system-admins, assume connections exist (they simply cannot see them).

#### Domain Examples

**Example 1 — Team Admin with canCreateTeam creates team**: Sam has canCreateTeam granted. Sam opens the Overview. "Add Team" is enabled (not disabled). Sam clicks it and is navigated to the team creation form. The work tracking system dropdown is populated from the backend.

**Example 2 — System Admin still sees connection check**: Alex (System Admin) has no connections configured. "Add Team" is disabled with "Create a Connection first". Alex is expected to create a connection — they can see the connections section.

**Example 3 — Viewer never sees the button**: Morgan (Viewer) opens the Overview. No "Add Team" or "Add Portfolio" button is visible at all (canCreateTeam is false for Viewers).

#### UAT Scenarios (BDD)

```gherkin
Scenario: Non-system-admin with canCreateTeam sees active Add Team button
  Given Sam has canCreateTeam true and is not a System Admin
  And connections exist (but Sam cannot see them)
  When Sam views the Overview
  Then the "Add Team" button is visible and enabled

Scenario: System Admin with no connections sees disabled Add Team button
  Given Alex is System Admin and no work tracking connections exist
  When Alex views the Overview
  Then the "Add Team" button is visible but disabled
  And the tooltip reads "Create a Connection before adding a Team"

Scenario: Viewer never sees Add Team button
  Given Morgan is a Viewer
  When Morgan views the Overview
  Then no "Add Team" button is visible
```

#### Acceptance Criteria
- [ ] For non-System-Admin users with `canCreateTeam === true`, the "Add Team" button is enabled regardless of connections count
- [ ] For System Admin users, the existing `!hasConnections` disabled check remains in place
- [ ] For users with `canCreateTeam === false`, the button is not shown
- [ ] Same logic applied symmetrically for "Add Portfolio" and `canCreatePortfolio`

#### Outcome KPIs
- Who: Non-system-admins granted canCreateTeam/canCreatePortfolio
- Does what: Create teams/portfolios without a misleading disabled state
- By how much: 0 support tickets for "Add Team button is disabled but I have permission"
- Measured by: Support ticket analysis
- Baseline: Button always disabled for users who cannot see connections

#### Technical Notes
- Q13 resolution
- Frontend-only change in `OverviewDashboard.tsx`; modify disabled prop on Add Team/Portfolio buttons

---

### US-11: E2E Test Coverage — Scenarios 1-4 (Bootstrap and System Admin Flow)

**Slice**: 01 — Bootstrap & Initial Admin Setup
**Job ID**: job-rbac-bootstrap
**Priority**: P1 (Walking Skeleton validation)

#### Elevator Pitch
Before: The E2E test file has a 7-scenario comment scaffold with zero implemented tests. No automated verification exists that RBAC bootstrap or the System Admin flow works end-to-end.
After: Scenarios 1-4 are implemented as Playwright tests in the existing spec file, verifying the bootstrap flow, role restrictions for non-admins, System Admin rights, and emergency admin fallback.
Decision enabled: The team decides to ship the RBAC feature with confidence that the core auth flows are verified by automated E2E tests.

#### Problem
The E2E test file `RoleBasedAccessControl.spec.ts` was scaffolded with 7 test cases described in comments but not implemented. The team has no automated regression safety net for RBAC changes.

#### Who
- QA / Developer implementing RBAC features | Running E2E suite before releasing | Motivated to catch RBAC regressions before they reach production

#### Solution
Implement Playwright E2E tests for scenarios 1-4 from the scaffold: bootstrap flow, viewer restriction, System Admin verification, and emergency admin fallback. Use the existing fixture infrastructure (`testWithAuth`, `LighthouseFixture`).

#### Domain Examples

**Example 1 — Scenario 1 (bootstrap)**: Test user logs in. No System Admin exists. User bootstraps as System Admin. User sets system-admin SSO group. Assertions verify the user appears in the table and the SSO mapping is created.

**Example 2 — Scenario 2 (viewer restriction)**: Team reader logs in. Asserts cannot navigate to System Settings → System Admins tab. Asserts cannot see Settings or Access tabs on a team they only view.

**Example 3 — Scenario 4 (emergency admin fallback)**: Test user (removed as System Admin in scenario 3) logs in. Emergency admin config is present. User still sees full admin access (emergency admin fallback active).

#### UAT Scenarios (BDD)

```gherkin
Scenario: E2E scenario 1 — Bootstrap as first System Admin
  Given no System Admin exists in the test environment
  When the test user logs in and navigates to System Settings → Access
  Then the bootstrap banner is visible
  When the test user clicks "Become First System Admin"
  Then the test user appears in the System Admins table with "Yes"
  When the test user adds SSO group "lighthouse-test-admins"
  Then the group mapping appears in the SSO Groups table

Scenario: E2E scenario 2 — Viewer cannot access System Settings admin tabs
  Given a team reader user is logged in
  When the user navigates to System Settings
  Then the System Admins tab is not accessible or not visible
  And navigating directly to the Access URL returns a restricted view

Scenario: E2E scenario 3 — New System Admin can manage rights and remove test user
  Given the new sys admin account is used to log in
  When navigating to System Settings → Access
  Then all System Settings tabs are visible
  And the user table shows the test user as System Admin
  When the new sys admin removes the test user's System Admin role
  Then the test user's row shows "No" for System Admin

Scenario: E2E scenario 4 — Emergency admin fallback remains active
  Given the test user was removed as System Admin in scenario 3
  And the emergency admin is configured in server settings
  When the test user logs in
  Then the test user can still access full admin functionality via emergency admin fallback
```

#### Acceptance Criteria
- [ ] Scenario 1 implemented: bootstrap flow passes with all assertions
- [ ] Scenario 2 implemented: viewer restriction assertions pass
- [ ] Scenario 3 implemented: new sys admin flow passes; test user removed as admin
- [ ] Scenario 4 implemented: emergency admin fallback verified
- [ ] All 4 scenarios run as part of the `@RBAC E2E` test suite
- [ ] Tests use the existing `testWithAuth` fixture and `TestConfig` credential config

#### Outcome KPIs
- Who: Development team running E2E suite
- Does what: Detect RBAC regressions before production
- By how much: 100% of the 4 core bootstrap/admin scenarios covered by automated tests
- Measured by: E2E suite pass/fail rate on RBAC scenarios
- Baseline: 0% automated E2E coverage of RBAC

#### Technical Notes
- Uses existing `Lighthouse.EndToEndTests/tests/specs/auth/RoleBasedAccessControl.spec.ts`
- Test users needed: `TestConfig.AUTH_TEST_USER_USERNAME`, a "team reader" user, a "new sys admin" user
- Emergency admin must be configured in the test environment's `appsettings.json`

---

### US-12: E2E Test Coverage — Scenarios 5-7 (Scoped Access and SSO Groups)

**Slice**: 03 — Scoped Team/Portfolio Access + E2E
**Job ID**: job-rbac-manage-users
**Priority**: P2

#### Elevator Pitch
Before: Scenarios 5-7 — covering scoped permissions for team/portfolio readers and admins, and SSO group-based permissions — are not implemented as E2E tests.
After: All three scenarios are implemented, verifying that individual-user permissions and group-based permissions produce identical behaviour.
Decision enabled: The team decides to ship group-based RBAC with confidence that it is behaviourally equivalent to individual assignments.

#### Problem
The critical invariant "group-based rights behave 100% the same as individual rights" (scenario 7) has no automated verification. A regression could silently break this equivalence.

#### Who
- QA / Developer | Running E2E suite pre-release | Motivated to verify the group-vs-individual equivalence invariant

#### Solution
Implement scenarios 5-7: create team/portfolio with scoped permissions for 4 user types (team reader, team admin, portfolio reader, portfolio admin), verify exact access for each, then repeat with group-based rights.

#### Domain Examples

**Example 1 — Scenario 5 (create and assign)**: System Admin creates a test team and portfolio (or uses demo data). Assigns individual users: team reader (Viewer), team admin (TeamAdmin), portfolio reader (Viewer), portfolio admin (PortfolioAdmin).

**Example 2 — Scenario 6 (verify per user)**: For each user, log in and verify: correct sections visible, incorrect sections blocked, no unexpected errors. Team reader cannot see Settings or Access tabs. Team admin can see Settings and Access, can add members.

**Example 3 — Scenario 7 (SSO group equivalence)**: System Admin removes individual rights, configures SSO group mappings for the same rights. Each user logs in again. All assertions from scenario 6 must pass identically.

#### UAT Scenarios (BDD)

```gherkin
Scenario: E2E scenario 5 — System Admin creates team/portfolio and assigns scoped roles
  Given the System Admin is logged in
  When a test team and test portfolio are created (or demo data used)
  And each test user is assigned their respective role (viewer/admin for team and portfolio)
  Then each user's role assignment is visible in the Access tab

Scenario: E2E scenario 6 — Each scoped user sees exactly what they should
  Given all four test users have their individual role assignments
  When each user logs in and navigates to the test team or portfolio
  Then viewers can see Forecast and Deliveries but not Settings, Access, or admin controls
  And team admins can see Settings, Access, and management controls for their team
  And each user cannot access entities outside their assignment

Scenario: E2E scenario 7 — Group-based rights produce identical behaviour to individual rights
  Given individual role assignments from scenario 6 have been removed
  And SSO group mappings have been configured for the equivalent rights
  When each user logs in (triggering group-based role resolution)
  Then every assertion from scenario 6 passes identically
```

#### Acceptance Criteria
- [ ] Scenario 5 implemented: team/portfolio creation and scoped role assignment verified
- [ ] Scenario 6 implemented: all 4 user types tested with correct access assertions
- [ ] Scenario 7 implemented: group-based rights verified as behaviourally identical to scenario 6
- [ ] Tests run as part of the `@RBAC E2E` suite in sequence after scenarios 1-4

#### Outcome KPIs
- Who: Development team
- Does what: Verify group-based RBAC equivalence with automated tests
- By how much: 100% of scoped access scenarios covered by E2E tests
- Measured by: E2E pass rate
- Baseline: 0% automated coverage of scoped permissions

#### Technical Notes
- Depends on US-11 (scenarios 1-4 infrastructure)
- May use demo data for team/portfolio creation to speed up test setup
- 4 test user accounts required in the test environment Keycloak realm; group memberships must be configurable

---

## Wave: DISCUSS / [REF] Phase 3 — Outcome KPIs Summary

Full KPI table for the feature:

| # | Who | Does What | By How Much | Baseline | Measured By | Type |
|---|---|---|---|---|---|---|
| 1 | IT leads deploying Lighthouse | Complete RBAC bootstrap in-app | 100% (0 config file workarounds) | All require config edit | Support ticket volume | Leading |
| 2 | System Admins | Remove departed users in-app | 100% offboarding in-app | No in-app mechanism | Support requests for user removal | Leading |
| 3 | Team/Portfolio Admins | Self-serve membership without System Admin escalation | Eliminate 100% of routine escalations | All escalated to System Admin | Support ticket categorisation | Leading |
| 4 | Viewers | Complete a session without a 403 error or disabled control | 0 error states in normal navigation | Multiple errors triggered currently | Frontend error boundary / 403 rate by role | Leading |
| 5 | Development team | Detect RBAC regressions via automated E2E | 100% of 7 core scenarios automated | 0% automated coverage | E2E suite pass rate | Leading |

**North Star**: System Admin can fully configure RBAC, and every user role experiences a clean, error-free UI that shows exactly what they are allowed to do — verified by automated E2E tests.

**Guardrail Metrics**:
- Permissive fallback must not be broken: 0 users locked out due to RBAC hook errors
- RBAC disabled deployments unaffected: feature flags and `isRbacEnabled` checks must prevent any regression for non-RBAC users

---

## Wave: DISCUSS / [REF] Wave Decisions

### Decision Log

| ID | Date | Decision | Rationale |
|---|---|---|---|
| WD-01 | 2026-05-10 | Feature split into 4 delivery slices (not monolithic) | 22 stories × 5 bounded contexts exceeds right-sizing threshold |
| WD-02 | 2026-05-10 | Emergency admin: display-only, non-revocable via UI | Prevents accidental removal of the fallback safety net |
| WD-03 | 2026-05-10 | canCreateTeam/Portfolio = System Admin only (default) | Principle of least privilege; scoped admins manage existing entities, not create new ones |
| WD-04 | 2026-05-10 | Status chips removed → collapsed diagnostic panel | Reduces cognitive load; diagnostics are rare, user management is frequent |
| WD-05 | 2026-05-10 | License upload is pre-auth and independent of RBAC bootstrap | Two orthogonal gates must not be coupled |
| WD-06 | 2026-05-10 | Viewer-clean: all write controls hidden (not disabled) | Hidden > disabled — disabled controls invite curiosity and error clicks |
| WD-07 | 2026-05-10 | Group-based rights must be behaviourally identical to individual rights | E2E scenario 7 is the regression gate for this invariant |
| WD-08 | 2026-05-10 | ScopedGroupMappingManager uses scoped endpoint only | Global endpoint is System Admin only; scoped endpoint uses team/portfolio membership check |

### Scope Assessment
SPLIT applied — 4 slices, estimated 22 stories, 5 bounded contexts.

### Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Emergency admin revocation via UI accident | Low (mitigated by WD-02) | High | Display-only row; no revoke button |
| Group-based rights behave differently from individual rights | Medium | High | E2E scenario 7 as explicit regression gate |
| canCreateTeam granted to wrong users creates entity sprawl | Low | Medium | Default false; explicit grant required |
| Viewer sees stale authorization summary after role change | Medium | Medium | Re-fetch summary after any role mutation; permissive fallback keeps users from being locked out |

---

## Wave: DISCUSS / [REF] DoR Validation

All user stories validated against 9-item DoR checklist.

| Story | Problem Clear | Persona Specific | 3+ Examples | 3-7 UAT Scenarios | AC from UAT | Right-Sized | Technical Notes | Dependencies | Outcome KPIs | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| US-01 Bootstrap | PASS | PASS | PASS | PASS (5) | PASS | PASS | PASS | PASS | PASS | READY |
| US-02 Emergency Admin | PASS | PASS | PASS | PASS (3) | PASS | PASS | PASS | PASS | PASS | READY |
| US-03 Status Panel | PASS | PASS | PASS | PASS (3) | PASS | PASS | PASS | PASS | PASS | READY |
| US-04 User Removal | PASS | PASS | PASS | PASS (4) | PASS | PASS | PASS | PASS | PASS | READY |
| US-05 Tab Visibility | PASS | PASS | PASS | PASS (3) | PASS | PASS | PASS | PASS | PASS | READY |
| US-06 Scoped Tabs | PASS | PASS | PASS | PASS (4) | PASS | PASS | PASS | PASS | PASS | READY |
| US-07 Scoped Controls | PASS | PASS | PASS | PASS (4) | PASS | PASS | PASS | PASS | PASS | READY |
| US-08 Group Mapping Fix | PASS | PASS | PASS | PASS (3) | PASS | PASS | PASS | PASS | PASS | READY |
| US-09 Viewer Experience | PASS | PASS | PASS | PASS (5) | PASS | PASS | PASS | PASS | PASS | READY |
| US-10 Create Button Fix | PASS | PASS | PASS | PASS (3) | PASS | PASS | PASS | PASS | PASS | READY |
| US-11 E2E Scenarios 1-4 | PASS | PASS | PASS | PASS (4) | PASS | PASS | PASS | PASS | PASS | READY |
| US-12 E2E Scenarios 5-7 | PASS | PASS | PASS | PASS (3) | PASS | PASS | PASS | PASS | PASS | READY |

### DoR Status: ALL PASSED

---

## Wave: DISCUSS / [WHY] ask-intelligent Trigger Detection

Three triggers fired at wave end:

### Trigger 1: persona-narrative (FIRED — 4 personas)

Four distinct personas (First-Time System Admin, System Admin, Team/Portfolio Admin, Viewer) have been documented with full JTBD analysis in `docs/product/jobs.yaml`. The emotional arcs in the journey schema (`docs/product/journeys/rbac-enhancements.yaml`) capture each persona's journey from uncertainty to confidence.

Recommendation: Run `*persona-narrative` to produce detailed persona briefs for handoff to the DESIGN wave. The solution-architect will need to understand the emotional arc of each persona when designing the UI components.

### Trigger 2: alternatives-considered (FIRED — 5+ bounded contexts)

Five bounded contexts are touched by this feature. Key alternatives considered during this wave:

- **Q5 (Status chips)**: Considered keeping chips but de-emphasising; decided on collapsible panel instead. Rationale: day-to-day admin tasks should not require parsing diagnostic state.
- **Q4 (canCreateTeam)**: Considered granting canCreateTeam automatically to Team Admins; decided against. Rationale: principle of least privilege — Team Admins manage existing teams, not create new ones.
- **Q13 (connections check)**: Considered requiring Team Admins to see a special "connections available" state; decided to bypass the check for non-system-admins. Rationale: they cannot see or manage connections; the check has no meaning for them.
- **Q6 (user removal)**: Considered soft-delete (mark as inactive); decided on hard delete with confirmation. Rationale: GDPR compliance requires actual data removal; soft-delete creates a false sense of security.

Recommendation: Document these in `docs/feature/rbac-enhancements/discuss/alternatives-considered.md` for the solution-architect's context.

### Trigger 3: gherkin-scenarios (FIRED — ambiguous ACs across stories)

US-06, US-07, and US-09 share overlapping AC around Settings/Access tab visibility and admin control rendering. There is potential for ambiguity about which conditions combine (e.g., is it `isRbacEnabled AND isTeamAdmin` or `isRbacEnabled OR isTeamAdmin`?).

Recommendation: Run `*gherkin-scenarios` on US-06, US-07, and US-09 together to produce a combined boundary-condition scenario set that explicitly covers all combinations of RBAC enabled/disabled × System Admin × Team Admin × Viewer.

---

## Wave: DESIGN / [REF] DDD List

### Bounded Contexts Touched

| Bounded Context | Scope | Change Type |
|---|---|---|
| Authorization / RBAC Administration | Backend: AuthorizationController, IRbacAdministrationService, RbacAdministrationService, RbacUserSummary models | EXTEND |
| System Settings UI | Frontend: Settings.tsx, SystemSettingsTab.tsx, RbacSettings.tsx | EXTEND |
| Overview Dashboard UI | Frontend: OverviewDashboard.tsx | EXTEND |
| Team Detail UI | Frontend: TeamDetail.tsx | EXTEND |
| Portfolio Detail UI | Frontend: PortfolioDetail.tsx, PortfolioDeliveryView.tsx | EXTEND |
| E2E Test Suite | Playwright: RoleBasedAccessControl.spec.ts | EXTEND |

### Domain Entities (RBAC)

- `UserPermission` — permission assignment (user × role × scope)
- `RbacGroupMapping` — SSO group → role × scope mapping
- `UserProfile` — known user identity record (subject + display name + email)
- `UserAuthorizationSummary` — per-request computed summary of a user's effective rights

No new domain entities are introduced. Existing entities are extended where noted in the Reuse Analysis.

---

## Wave: DESIGN / [REF] Component Decomposition

All components are EXTEND (no CREATE NEW). See `docs/product/architecture/brief.md` section "Component Decomposition" for the complete table with file paths, change types, and change summaries.

**Backend changes (2 new endpoints, 2 new service methods, 1 model field):**
- `AuthorizationController`: +`DELETE /authorization/users/{userProfileId}`, +`GET /authorization/teams/{teamId}/group-mappings`
- `IRbacAdministrationService`: +`DeleteUserAsync`, +`GetTeamGroupMappingsAsync`
- `RbacAdministrationService`: implement the 2 new methods
- `RbacUserSummary`: +`IsEmergencyAdmin bool`

**Frontend changes (UI gating, bug fix, UX polish):**
- `RbacModels.ts`: +`isEmergencyAdmin` on `RbacUser`
- `RbacService.ts`: +`deleteUser`, +`getTeamGroupMappings`
- `RbacSettings.tsx`: chips → Accordion panel, emergency admin display, user removal
- `ScopedGroupMappingManager.tsx`: bug fix (scoped endpoint)
- `Settings.tsx`: System Admins tab gated on `isRbacEnabled`
- `SystemSettingsTab.tsx`: Log Level gated on `isSystemAdmin`
- `TeamDetail.tsx`: Access tab gated on `isRbacEnabled`; write controls on `isTeamAdmin`; group mappings fix
- `PortfolioDetail.tsx`: symmetric with TeamDetail
- `PortfolioDeliveryView.tsx`: Add/Edit/Delete delivery controls hidden from Viewers
- `OverviewDashboard.tsx`: connections section hidden for non-admins; Add Team/Portfolio disabled-logic fix

**E2E:**
- `RoleBasedAccessControl.spec.ts`: implement all 7 scenarios

---

## Wave: DESIGN / [REF] Driving Ports

All inbound HTTP endpoints handled by `AuthorizationController` at `/api/latest/authorization`.

| Method | Route | Auth Gate | New / Existing |
|---|---|---|---|
| GET | `/authorization/status` | Authenticated | Existing |
| GET | `/authorization/my-summary` | Authenticated | Existing |
| POST | `/authorization/bootstrap/system-admin` | Authenticated | Existing |
| GET | `/authorization/users` | CanManageRbac | Existing |
| DELETE | `/authorization/users/{userProfileId}` | CanManageRbac | **NEW** |
| POST/DELETE | `/authorization/system-admins/{userProfileId}` | CanManageRbac | Existing |
| GET/PUT/DELETE | `/authorization/teams/{teamId}/members/{userProfileId?}` | CanManageTeamMembership | Existing |
| GET | `/authorization/teams/{teamId}/group-mappings` | CanManageTeamMembership | **NEW** |
| GET/PUT/DELETE | `/authorization/portfolios/{portfolioId}/members/{userProfileId?}` | CanManagePortfolioMembership | Existing |
| GET/POST/DELETE | `/authorization/group-mappings/{mappingId?}` | CanManageRbac | Existing |

---

## Wave: DESIGN / [REF] Driven Ports and Adapters

| Port | Adapter | Technology | Change |
|---|---|---|---|
| RBAC persistence | `LighthouseDbContext` | EF Core 8 / SQLite+PostgreSQL | No change to adapter; new service methods use existing DbSet queries |
| OIDC token validation | ASP.NET Core OIDC middleware | OpenIdConnect | No change |

No new driven ports or adapters introduced by this feature.

---

## Wave: DESIGN / [REF] Technology Choices

No new technologies introduced. All choices reuse the existing stack:
- Backend: C# .NET 8, ASP.NET Core, EF Core 8
- Frontend: React 18, TypeScript, Material UI 5 (MUI `Accordion` component used for status panel)
- Database: SQLite (dev/test), PostgreSQL (prod)
- E2E: Playwright, TypeScript

See `docs/product/architecture/brief.md` section "Technology Stack" for the full table with license and rationale.

---

## Wave: DESIGN / [REF] Decisions Table

| ID | Decision | Rationale | ADR |
|---|---|---|---|
| DD-01 | All write controls hidden (not disabled) from Viewers | Disabled controls invite confusion; hidden controls signal clearly the action doesn't exist in this context | ADR-001 |
| DD-02 | Scoped endpoint for group mapping reads in `ScopedGroupMappingManager` | Global endpoint is System Admin only; Team/Portfolio Admins must use the scoped endpoint for their scope | ADR-002 |
| DD-03 | Emergency admin: distinct display with lock icon, no Revoke/Remove button | Prevents accidental removal of safety net; communicates config-managed nature | ADR-003 |
| DD-04 | `useRbac` hook remains the single RBAC state source; no component fetches `/my-summary` independently | Single source of truth; permissive fallback invariant must be in one place | brief.md |
| DD-05 | User removal is a hard delete (DELETE /authorization/users/{id}) | GDPR compliance; soft-delete creates false sense of security (DISCUSS WD-17) | — |
| DD-06 | No new components created; all changes are additive extensions to existing files | Reuse analysis confirmed existing components cover all required functionality | brief.md Reuse Analysis |
| DD-07 | Access tab and System Admins tab gated on `isRbacEnabled && isTeamAdmin/isPortfolioAdmin/isSystemAdmin` | AND condition: both RBAC must be on and user must have the right role | — |
| DD-08 | Deliveries tab remains visible to Viewers; Add/Edit/Delete hidden | Viewing delivery forecasts is the primary value for Viewers (DISCUSS WD-12) | ADR-001 |

---

## Wave: DESIGN / [REF] Reuse Analysis

See `docs/product/architecture/brief.md` section "Reuse Analysis" for the complete table.

**Summary**: 16 existing components are EXTEND. 0 new components are created. Every proposed change was validated against the codebase before the decision to extend was made.

Key EXTEND decisions grounded in codebase analysis:
- `ScopedGroupMappingManager` — existing component, correct abstraction boundary; only the data source changes (bug fix)
- `useRbac` — existing hook with `PERMISSIVE_SUMMARY` invariant already implemented; all gating derives from it
- `AuthorizationController` — 2 new endpoint methods follow the exact same pattern as 12 existing methods in the same file
- `PortfolioDeliveryView` — Deliveries tab visibility is already gated on `showDeliveriesAndSettingsTabs`; only Add/Edit/Delete controls within need gating

---

## Wave: DESIGN / [REF] Open Questions

| # | Question | Owner | Blocking |
|---|---|---|---|
| OQ-01 | Does `GET /authorization/portfolios/{portfolioId}/group-mappings` also need to be added symmetrically with the team variant? | Software Crafter | US-08 for portfolio scope |
| OQ-02 | Should `DeleteUserAsync` cascade-delete all `UserPermission` records for the user in a single transaction, or are they soft-removed first? | Software Crafter | US-04 implementation |
| OQ-03 | E2E test environment: are the 4 required Keycloak test users pre-provisioned or must the test setup script create them? | DevOps / Platform Architect | US-11, US-12 |
| OQ-04 | `UserAuthorizationSummary` does not need `IsEmergencyAdmin` (the frontend `useRbac` hook doesn't expose it). Confirm the emergency admin flag is only needed in `RbacUserSummary` (the user list) and not in `/my-summary`. | Solution Architect → Crafter | US-02 scope |

---

## Wave: DISTILL / [REF] Scenario List with Tags

All acceptance test scenarios are implemented in:
`Lighthouse.EndToEndTests/tests/specs/auth/RoleBasedAccessControl.spec.ts`

| # | Scenario Name | Tags | US | State |
|---|---|---|---|---|
| 1 | First user self-bootstraps as System Admin and assigns SSO group | `@walking-skeleton @RBAC E2E` | US-01, US-11 | ENABLED |
| 2a | Team reader cannot see System Admins tab or Log Level | `@RBAC E2E` | US-09, US-11 | SKIPPED |
| 2b | Team reader cannot access team Settings or Access tabs | `@RBAC E2E` | US-06, US-07, US-09, US-11 | SKIPPED |
| 3 | New sys admin sees all tabs, revokes test user rights | `@RBAC E2E` | US-11 | SKIPPED |
| 4 | Emergency admin fallback remains active after revocation | `@RBAC E2E` | US-02, US-11 | SKIPPED |
| 5 | System admin creates team + portfolio, assigns individual roles | `@RBAC E2E` | US-12 | SKIPPED |
| 6a | Team reader — individual rights: sees Forecast, not Settings/Access/write | `@RBAC E2E` | US-06, US-07, US-09, US-12 | SKIPPED |
| 6b | Team admin — individual rights: sees Settings, Access, management controls; group mappings load without error | `@RBAC E2E` | US-06, US-07, US-08, US-12 | SKIPPED |
| 6c | Portfolio reader — individual rights: sees Deliveries read-only, not Settings/Access | `@RBAC E2E` | US-06, US-07, US-09, US-12 | SKIPPED |
| 6d | Portfolio admin — individual rights: sees Settings, Access, can manage deliveries | `@RBAC E2E` | US-06, US-07, US-08, US-12 | SKIPPED |
| 7 setup | Sys admin switches to group-based rights | `@RBAC E2E` | US-12 | SKIPPED |
| 7a | Team reader — group rights: identical to 6a | `@RBAC E2E` | US-12 (WD-07) | SKIPPED |
| 7b | Team admin — group rights: identical to 6b | `@RBAC E2E` | US-12 (WD-07) | SKIPPED |
| 7c | Portfolio reader — group rights: identical to 6c | `@RBAC E2E` | US-12 (WD-07) | SKIPPED |
| 7d | Portfolio admin — group rights: identical to 6d | `@RBAC E2E` | US-12 (WD-07) | SKIPPED |

**Total scenarios**: 15 | **Enabled**: 1 | **Skipped**: 14
**Error/restriction path ratio**: 10/15 = 67% (threshold: 40%)

---

## Wave: DISTILL / [REF] Walking Skeleton Strategy

**Strategy C — Real local**: All adapters real (Playwright browser, Keycloak OIDC, .NET API, SQLite DB).

**Walking Skeleton** = Scenario 1 (Bootstrap first System Admin + SSO group assignment).

Observable user value: A first-time deployer logs in, clicks "Become First System Admin", sees themselves appear in the System Admins table, adds an SSO group mapping, and sees the group mapping row — all without editing any config file. Demonstrates end-to-end RBAC initialisation works via `POST /authorization/bootstrap/system-admin` and `POST /authorization/group-mappings`.

**Litmus test**: Can the IT lead demonstrate RBAC bootstrap to a stakeholder from this test? Yes — the test narrates the bootstrap journey in observable UI terms.

**Rationale for real adapters**: RBAC's primary failure modes are wiring failures: the wrong endpoint called (WD-08 bug), the permissive fallback not triggering, the emergency admin not flagged in the user list. Stub-based tests cannot catch these. Real E2E is the only verification that matters for a security feature.

---

## Wave: DISTILL / [REF] Adapter Coverage Table

| Adapter | Driven By | Scenarios | Coverage Type |
|---|---|---|---|
| Playwright browser | External (test runner) | All | Real I/O |
| Keycloak OIDC | Driven port (OIDC middleware) | All | Real I/O |
| .NET API — `POST /authorization/bootstrap/system-admin` | AuthorizationController | Scenario 1 | Real I/O via browser |
| .NET API — `POST /authorization/group-mappings` | AuthorizationController | Scenario 1, 7 setup | Real I/O via browser |
| .NET API — `DELETE /authorization/system-admins/{id}` | AuthorizationController | Scenario 3 | Real I/O via browser |
| .NET API — `DELETE /authorization/users/{id}` (NEW) | AuthorizationController | Scenario 3 | Real I/O via browser |
| .NET API — `PUT /authorization/teams/{id}/members/{userId}` | AuthorizationController | Scenario 5, 7 | Real I/O via browser |
| .NET API — `GET /authorization/teams/{id}/group-mappings` (NEW scoped) | AuthorizationController | Scenario 6b, 7b | Real I/O via browser |
| .NET API — `PUT /authorization/portfolios/{id}/members/{userId}` | AuthorizationController | Scenario 5, 7 | Real I/O via browser |
| .NET API — `GET /authorization/portfolios/{id}/group-mappings` (NEW scoped) | AuthorizationController | Scenario 6d, 7d | Real I/O via browser |
| SQLite DB (EF Core) | LighthouseDbContext | 1, 3, 5 (implicit) | Real I/O via API |

**Both new scoped endpoints** (`GET /authorization/teams/{id}/group-mappings` and `GET /authorization/portfolios/{id}/group-mappings`) are covered by dedicated scenarios (6b/7b and 6d/7d respectively). This satisfies the real-I/O requirement for new driven adapters — wiring bugs and path resolution errors can only be caught here.

---

## Wave: DISTILL / [REF] Scaffolds

Two new Page Object Model classes were created as real implementations (not RED scaffolds, because the `data-testid` attributes are already specified in the component design):

### `RbacSettingsPage`
Path: `Lighthouse.EndToEndTests/tests/models/auth/rbac/RbacSettingsPage.ts`

Wraps the System Settings → Access tab. Key methods:
- `goToAccessTab()` — clicks `system-admins-tab` test ID, waits for status indicator
- `becomeFirstSystemAdmin()` — clicks `rbac-bootstrap-button`
- `addSystemAdminGroupMapping(groupName)` — opens create dialog, fills `rbac-group-mapping-group-value`, clicks Add
- `getSystemAdminStatus(userEmail)` — reads System Admin column text for a user row
- `bootstrapBanner` — locator for "No System Admin is assigned yet" text

### `ScopedAccessPage`
Path: `Lighthouse.EndToEndTests/tests/models/auth/rbac/ScopedAccessPage.ts`

Wraps the Access tab inside team/portfolio detail pages. Key methods:
- `goToAccessTab()` — clicks the "Access" role-tab
- `assignMember(email, role)` — opens add dialog, fills email, selects role, saves
- `addScopedGroupMapping(groupName, role)` — adds a scoped SSO group mapping
- `groupMappingsErrorMessage` — locator for "Failed to load" text (must be `.not.toBeVisible()` for US-08)

**RED scaffold note**: `ScopedAccessPage` methods reference `data-testid` values (`scoped-add-member-button`, `scoped-member-email-input`, `scoped-group-mappings-section`, `scoped-add-group-mapping-button`) that are not yet in the component implementations. The DELIVER wave crafter must add these test IDs or update the POM locators to match the actual implementation. Tests using `ScopedAccessPage` are all currently skipped — they will remain RED until these test IDs are added.

---

## Wave: DISTILL / [REF] Test Placement

```
Lighthouse.EndToEndTests/
  tests/
    specs/
      auth/
        RoleBasedAccessControl.spec.ts    ← 15 E2E scenarios (1 enabled)
    models/
      auth/
        rbac/
          RbacSettingsPage.ts             ← System Settings → Access tab POM
          ScopedAccessPage.ts             ← Team/Portfolio detail Access tab POM
```

**Conventions followed**:
- Spec file: alongside existing `Auth.spec.ts` in `tests/specs/auth/`
- Models: under `tests/models/auth/rbac/` — auth sub-domain, RBAC sub-directory
- Uses `testWithAuth` fixture (provides `loginPage`, mirrors `Auth.spec.ts` pattern)
- Import path for `TestConfig`: `"../../../playwright.config"` (relative from `specs/auth/`)

---

## Wave: DISTILL / [REF] Driving Adapter Coverage

**Driving port**: Playwright browser (Strategy C — all real). Tests invoke the Lighthouse React SPA through Chromium. The SPA calls the API. No direct API calls from tests — all HTTP endpoints are exercised indirectly through browser interactions.

**Hexagonal boundary**: Tests enter through the browser UI surface only. No test imports backend service types or calls the API directly. Observable outcomes are asserted on UI elements (table rows, tab visibility, button presence).

**Mandate compliance evidence**:
- CM-A: All test interactions go through `RbacSettingsPage`, `ScopedAccessPage`, `TeamDetailPage`, `PortfolioDetailPage`, `OverviewPage` POMs — zero direct `page.goto('/api/...')` calls.
- CM-B: Zero technical terms in test descriptions. Scenarios describe observable user outcomes ("team reader sees Forecast but not Settings"), not implementation details ("GET /authorization/teams returns 403").
- CM-C: 1 walking skeleton (Scenario 1) + 14 focused scenarios covering all 4 personas.

---

## Wave: DISTILL / [REF] Pre-requisites

**Test environment requirements** (DEVOPS wave not yet run — using defaults):

| Requirement | Description | Blocks |
|---|---|---|
| Keycloak realm users | 6 users pre-provisioned with stable OIDC subjects (see credentials in `TestConfig`) | Scenarios 2-7 |
| Keycloak group memberships | `system-admins`, `team-admins`, `team-readers`, `portfolio-admins`, `portfolio-readers` groups with correct members | Scenario 7 (group-based rights) |
| Emergency admin config | `appsettings.json` emergency admin subject = Keycloak subject of `test@user.com` | Scenario 4 |
| DB reset between runs | No pre-existing System Admin in the DB when Scenario 1 runs | Scenario 1 (bootstrap) |
| New scoped endpoints | `GET /authorization/teams/{id}/group-mappings` and `GET /authorization/portfolios/{id}/group-mappings` implemented in backend | Scenarios 6b, 6d, 7b, 7d |
| `DELETE /authorization/users/{id}` endpoint | New hard-delete endpoint implemented | Scenario 3 |
| `rbac-testid` attributes | `scoped-add-member-button`, `scoped-member-email-input`, `scoped-group-mappings-section`, `scoped-add-group-mapping-button` added to React components | Scenarios 5, 6, 7 (ScopedAccessPage methods) |
