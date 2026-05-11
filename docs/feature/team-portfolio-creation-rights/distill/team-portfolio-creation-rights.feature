Feature: Team and Portfolio Creation Rights
  As a user with administrative responsibility for a team or portfolio
  I want to create new teams or portfolios when my role grants that responsibility
  So that I can act independently without escalating every creation to a System Admin
  And so that the entity I just created lists me as its administrator

  Background: RBAC enforced
    Given Lighthouse has RBAC authentication enabled
    And at least one System Admin has been bootstrapped

  # ───────────────────────────────────────────────────────────────────
  # Walking Skeleton — end-to-end creation flow proving the inferred-rights
  # contract via the real RbacGuardAttribute pipeline, real EF SQLite, and
  # observable UserPermission persistence.
  # ───────────────────────────────────────────────────────────────────

  @walking_skeleton @real-io @driving_adapter
  Scenario: Team Admin creates a team and is recorded as its administrator
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan is not a System Admin
    When Jordan submits a Create Team request for "Beta"
    Then the request is accepted
    And team "Beta" exists in the system
    And Jordan holds the Team Admin role on team "Beta"

  # ───────────────────────────────────────────────────────────────────
  # Authorization summary — what the frontend sees in /my-summary
  # ───────────────────────────────────────────────────────────────────

  @summary @real-io
  Scenario: System Admin can create teams and portfolios
    Given Alex is a System Admin
    When Alex requests the authorization summary
    Then the summary reports Alex can create teams
    And the summary reports Alex can create portfolios

  @summary @real-io
  Scenario: Team Admin can create teams but not portfolios
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan holds no portfolio admin role
    When Jordan requests the authorization summary
    Then the summary reports Jordan can create teams
    And the summary reports Jordan cannot create portfolios

  @summary @real-io
  Scenario: Portfolio Admin can create portfolios but not teams
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    And Riley holds no team admin role
    When Riley requests the authorization summary
    Then the summary reports Riley can create portfolios
    And the summary reports Riley cannot create teams

  @summary @real-io @error
  Scenario: Viewer cannot create teams or portfolios
    Given Morgan holds only Viewer roles
    When Morgan requests the authorization summary
    Then the summary reports Morgan cannot create teams
    And the summary reports Morgan cannot create portfolios

  # ───────────────────────────────────────────────────────────────────
  # Group-derived rights — rights inherited from SSO group mapping
  # must behave identically to direct user assignment (invariant from
  # rbac-enhancements/WD-07).
  # ───────────────────────────────────────────────────────────────────

  @group_rights @real-io
  Scenario: Team admin rights granted via an SSO group enable team creation
    Given the SSO group "team-admins" maps to Team Admin on team "Alpha"
    And Sam's identity provider asserts membership in group "team-admins"
    And Sam has no direct user permission grants
    When Sam requests the authorization summary
    Then the summary reports Sam can create teams

  @group_rights @real-io
  Scenario: Portfolio admin rights granted via an SSO group enable portfolio creation
    Given the SSO group "portfolio-admins" maps to Portfolio Admin on portfolio "Vision"
    And Casey's identity provider asserts membership in group "portfolio-admins"
    And Casey has no direct user permission grants
    When Casey requests the authorization summary
    Then the summary reports Casey can create portfolios

  # ───────────────────────────────────────────────────────────────────
  # HTTP authorization contract — what the create endpoints accept and reject
  # ───────────────────────────────────────────────────────────────────

  @driving_adapter @real-io
  Scenario: Create Team endpoint accepts a user with Team Admin role
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan is not a System Admin
    When Jordan submits a Create Team request for "Beta"
    Then the request is not refused for authorization reasons

  @driving_adapter @real-io
  Scenario: Create Portfolio endpoint accepts a user with Portfolio Admin role
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    And Riley is not a System Admin
    When Riley submits a Create Portfolio request for "Horizon"
    Then the request is not refused for authorization reasons

  @driving_adapter @real-io @error
  Scenario: Create Team endpoint refuses a Viewer
    Given Morgan holds only Viewer roles
    When Morgan submits a Create Team request for "Gamma"
    Then the request is refused for authorization reasons
    And no new team "Gamma" exists

  @driving_adapter @real-io @error
  Scenario: Create Portfolio endpoint refuses a Viewer
    Given Morgan holds only Viewer roles
    When Morgan submits a Create Portfolio request for "Mirage"
    Then the request is refused for authorization reasons
    And no new portfolio "Mirage" exists

  # ───────────────────────────────────────────────────────────────────
  # Auto-admin assignment on successful creation
  # ───────────────────────────────────────────────────────────────────

  @auto_admin @real-io
  Scenario: System Admin who creates a portfolio is recorded as portfolio admin
    Given Alex is a System Admin
    When Alex submits a Create Portfolio request for "Horizon"
    Then the request is accepted
    And portfolio "Horizon" exists in the system
    And Alex holds the Portfolio Admin role on portfolio "Horizon"

  @auto_admin @real-io
  Scenario: Portfolio Admin who creates a portfolio is recorded as admin of the new portfolio
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    When Riley submits a Create Portfolio request for "Horizon"
    Then the request is accepted
    And Riley holds the Portfolio Admin role on portfolio "Horizon"
    And Riley still holds the Portfolio Admin role on portfolio "Vision"

  @auto_admin @real-io
  Scenario: Team admin who creates a team via group-derived rights is recorded as the new team's admin
    Given the SSO group "team-admins" maps to Team Admin on team "Alpha"
    And Sam's identity provider asserts membership in group "team-admins"
    And Sam has no direct user permission grants
    When Sam submits a Create Team request for "Beta"
    Then the request is accepted
    And Sam holds the Team Admin role on team "Beta"
