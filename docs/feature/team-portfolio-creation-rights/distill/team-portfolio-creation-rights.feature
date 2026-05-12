Feature: Team and Portfolio Creation Rights (Revision R2 — unified rights + team-existence gate)
  As a user holding any administrative role in Lighthouse
  I want to create new teams or portfolios regardless of which scoped admin role I hold
  So that scoped admins act independently of the System Admin
  And so that portfolio creation is only blocked when no team exists in the system at all
  And so that I do not need read access to a team to create a portfolio that may reference it

  Background: RBAC enforced
    Given Lighthouse has RBAC authentication enabled
    And at least one System Admin has been bootstrapped

  # ───────────────────────────────────────────────────────────────────
  # Walking skeleton — two e2e proofs of the R2 contract.
  # WS1 (carried over from R1) proves the inferred-rights pipeline for teams.
  # WS2 (new in R2) proves unified rights + the visibility-decoupled
  # existence gate for portfolios: a Team Admin who can see only their
  # own team creates a portfolio in a tenant that contains other teams
  # the user has no read access to.
  # ───────────────────────────────────────────────────────────────────

  @walking_skeleton @real-io @driving_adapter
  Scenario: Team Admin creates a team and is recorded as its administrator
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan is not a System Admin
    When Jordan submits a Create Team request for "Beta"
    Then the request is accepted
    And team "Beta" exists in the system
    And Jordan holds the Team Admin role on team "Beta"

  @walking_skeleton @real-io @driving_adapter @unified_rights @visibility_decoupled
  Scenario: Team Admin creates a portfolio in a system where other teams exist but are invisible to them
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan is not a System Admin
    And teams "Beta", "Gamma", and "Delta" exist in the system
    And Jordan has no read access to teams "Beta", "Gamma", or "Delta"
    When Jordan submits a Create Portfolio request for "Vision"
    Then the request is accepted
    And portfolio "Vision" exists in the system
    And Jordan holds the Portfolio Admin role on portfolio "Vision"

  # ───────────────────────────────────────────────────────────────────
  # Authorization summary — what the frontend sees in /my-summary.
  # Under R2 any admin role grants both flags, and the portfolio flag
  # is additionally gated on at least one team existing.
  # ───────────────────────────────────────────────────────────────────

  @summary @real-io
  Scenario: System Admin can create teams and can create portfolios when at least one team exists
    Given Alex is a System Admin
    And at least one team exists in the system
    When Alex requests the authorization summary
    Then the summary reports Alex can create teams
    And the summary reports Alex can create portfolios

  @summary @real-io @error @existence_gate
  Scenario: System Admin cannot create portfolios when no teams exist in the system
    Given Alex is a System Admin
    And no teams exist in the system
    When Alex requests the authorization summary
    Then the summary reports Alex can create teams
    And the summary reports Alex cannot create portfolios

  @summary @real-io
  Scenario: Team Admin can create teams
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan holds no portfolio admin role
    When Jordan requests the authorization summary
    Then the summary reports Jordan can create teams

  @summary @real-io @unified_rights
  Scenario: Team Admin can create portfolios when at least one team exists
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan holds no portfolio admin role
    And at least one team exists in the system
    When Jordan requests the authorization summary
    Then the summary reports Jordan can create portfolios

  @summary @real-io
  Scenario: Portfolio Admin can create portfolios when at least one team exists
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    And Riley holds no team admin role
    And at least one team exists in the system
    When Riley requests the authorization summary
    Then the summary reports Riley can create portfolios

  @summary @real-io @unified_rights
  Scenario: Portfolio Admin can create teams
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    And Riley holds no team admin role
    When Riley requests the authorization summary
    Then the summary reports Riley can create teams

  @summary @real-io @error
  Scenario: Viewer cannot create teams or portfolios
    Given Morgan holds only Viewer roles
    And at least one team exists in the system
    When Morgan requests the authorization summary
    Then the summary reports Morgan cannot create teams
    And the summary reports Morgan cannot create portfolios

  @summary @real-io @error @existence_gate
  Scenario: Authorization summary in RBAC-disabled mode still blocks portfolio creation when no teams exist
    Given RBAC authentication is disabled for Lighthouse
    And no teams exist in the system
    When an unauthenticated session requests the authorization summary
    Then the summary reports the caller can create teams
    And the summary reports the caller cannot create portfolios

  @summary @real-io @error @existence_gate
  Scenario: Authorization summary in bootstrap-no-admin mode still blocks portfolio creation when no teams exist
    Given Lighthouse has RBAC authentication enabled
    And no System Admin has been bootstrapped yet
    And no teams exist in the system
    When the first authenticated user requests the authorization summary
    Then the summary reports the caller can create teams
    And the summary reports the caller cannot create portfolios

  # ───────────────────────────────────────────────────────────────────
  # Group-derived rights — direct and group-derived admin roles must
  # behave identically (rbac-enhancements/WD-07 invariant). Under R2
  # either admin role enables BOTH creation rights.
  # ───────────────────────────────────────────────────────────────────

  @group_rights @real-io @unified_rights
  Scenario: SSO group-derived Team Admin enables both team and portfolio creation
    Given the SSO group "team-admins" maps to Team Admin on team "Alpha"
    And Sam's identity provider asserts membership in group "team-admins"
    And Sam has no direct user permission grants
    And at least one team exists in the system
    When Sam requests the authorization summary
    Then the summary reports Sam can create teams
    And the summary reports Sam can create portfolios

  @group_rights @real-io @unified_rights
  Scenario: SSO group-derived Portfolio Admin enables both portfolio and team creation
    Given the SSO group "portfolio-admins" maps to Portfolio Admin on portfolio "Vision"
    And Casey's identity provider asserts membership in group "portfolio-admins"
    And Casey has no direct user permission grants
    And at least one team exists in the system
    When Casey requests the authorization summary
    Then the summary reports Casey can create portfolios
    And the summary reports Casey can create teams

  # ───────────────────────────────────────────────────────────────────
  # HTTP authorization contract — what the create endpoints accept and
  # reject under the unified-rights contract.
  # ───────────────────────────────────────────────────────────────────

  @driving_adapter @real-io
  Scenario: Create Team endpoint accepts a user with Team Admin role
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan is not a System Admin
    When Jordan submits a Create Team request for "Beta"
    Then the request is not refused for authorization reasons

  @driving_adapter @real-io @unified_rights
  Scenario: Create Team endpoint accepts a user with only a Portfolio Admin role
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    And Riley holds no team admin role
    When Riley submits a Create Team request for "Beta"
    Then the request is not refused for authorization reasons

  @driving_adapter @real-io
  Scenario: Create Portfolio endpoint accepts a user with Portfolio Admin role when a team exists
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    And Riley is not a System Admin
    And at least one team exists in the system
    When Riley submits a Create Portfolio request for "Horizon"
    Then the request is not refused for authorization reasons

  @driving_adapter @real-io @unified_rights
  Scenario: Create Portfolio endpoint accepts a user with only a Team Admin role when a team exists
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan holds no portfolio admin role
    And at least one team exists in the system
    When Jordan submits a Create Portfolio request for "Horizon"
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
    And at least one team exists in the system
    When Morgan submits a Create Portfolio request for "Mirage"
    Then the request is refused for authorization reasons
    And no new portfolio "Mirage" exists

  @driving_adapter @real-io @error @existence_gate
  Scenario: Create Portfolio endpoint refuses every caller when no team exists in the system
    Given Alex is a System Admin
    And no teams exist in the system
    When Alex submits a Create Portfolio request for "Horizon"
    Then the request is refused for authorization reasons
    And no new portfolio "Horizon" exists

  @existence_gate @real-io @visibility_decoupled
  Scenario: Portfolio existence gate is global, not visibility-scoped
    Given Jordan holds the Team Admin role on team "Alpha"
    And teams "Beta", "Gamma", and "Delta" exist in the system
    And Jordan has no read access to teams "Beta", "Gamma", or "Delta"
    When Jordan's authorization to create a portfolio is evaluated
    Then the authorization check reports Jordan can create a portfolio
    And the authorization check did not consult Jordan's per-team read access

  # ───────────────────────────────────────────────────────────────────
  # Auto-admin assignment on successful creation — including the new
  # cross-role cases where a TA creates a portfolio (and becomes its PA)
  # or a PA creates a team (and becomes its TA).
  # ───────────────────────────────────────────────────────────────────

  @auto_admin @real-io
  Scenario: System Admin who creates a portfolio is recorded as portfolio admin
    Given Alex is a System Admin
    And at least one team exists in the system
    When Alex submits a Create Portfolio request for "Horizon"
    Then the request is accepted
    And portfolio "Horizon" exists in the system
    And Alex holds the Portfolio Admin role on portfolio "Horizon"

  @auto_admin @real-io
  Scenario: Portfolio Admin who creates a portfolio is recorded as admin of the new portfolio
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    And at least one team exists in the system
    When Riley submits a Create Portfolio request for "Horizon"
    Then the request is accepted
    And Riley holds the Portfolio Admin role on portfolio "Horizon"
    And Riley still holds the Portfolio Admin role on portfolio "Vision"

  @auto_admin @real-io @unified_rights
  Scenario: Team Admin who creates a portfolio becomes that new portfolio's admin
    Given Jordan holds the Team Admin role on team "Alpha"
    And Jordan holds no portfolio admin role
    And at least one team exists in the system
    When Jordan submits a Create Portfolio request for "Horizon"
    Then the request is accepted
    And Jordan holds the Portfolio Admin role on portfolio "Horizon"
    And Jordan still holds the Team Admin role on team "Alpha"

  @auto_admin @real-io @unified_rights
  Scenario: Portfolio Admin who creates a team becomes that new team's admin
    Given Riley holds the Portfolio Admin role on portfolio "Vision"
    And Riley holds no team admin role
    When Riley submits a Create Team request for "Beta"
    Then the request is accepted
    And Riley holds the Team Admin role on team "Beta"
    And Riley still holds the Portfolio Admin role on portfolio "Vision"

  @auto_admin @real-io
  Scenario: Team admin who creates a team via group-derived rights is recorded as the new team's admin
    Given the SSO group "team-admins" maps to Team Admin on team "Alpha"
    And Sam's identity provider asserts membership in group "team-admins"
    And Sam has no direct user permission grants
    When Sam submits a Create Team request for "Beta"
    Then the request is accepted
    And Sam holds the Team Admin role on team "Beta"
