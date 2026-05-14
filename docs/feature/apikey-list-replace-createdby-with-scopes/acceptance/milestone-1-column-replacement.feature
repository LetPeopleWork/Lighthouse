Feature: API Keys listing replaces "Created By" with conditional "Scopes" column
  As a Lighthouse user managing my own API keys
  I want the listing to drop the always-redundant "Created By" column
  And — only when RBAC is enabled — gain a Scopes column that reflects ApiKeyPermission rows
  So that the table carries only information that varies per row

  Background:
    Given authentication is enabled on the deployment
    And I have at least one API key associated with my account

  @in-memory @milestone-1
  Scenario: M1.1 "Created By" column is absent on RBAC-disabled deployment
    Given RBAC is disabled on the deployment
    And one of my API keys is named "CI Key"
    When I open the API Keys tab
    Then the table column headers do not include "Created By"

  @in-memory @milestone-1
  Scenario: M1.2 "Created By" column is absent on RBAC-enabled deployment (regression pin)
    Given RBAC is enabled on the deployment
    And one of my API keys is named "CI Key" with no scope rows
    When I open the API Keys tab
    Then the table column headers do not include "Created By"

  @in-memory @milestone-1
  Scenario: M1.3 "Scopes" column is hidden on RBAC-disabled deployment
    Given RBAC is disabled on the deployment
    And one of my API keys is named "CI Key"
    When I open the API Keys tab
    Then the table column headers do not include "Scopes"

  @in-memory @milestone-1
  Scenario: M1.4 "Scopes" column renders one entry per scope row when RBAC is enabled
    Given RBAC is enabled on the deployment
    And one of my API keys is named "Multi-scope Key" and has scope rows:
      | role            | scopeType | scopeId |
      | TeamAdmin       | Team      | 42      |
      | Viewer          | Portfolio | 7       |
    When I open the API Keys tab
    Then the row for "Multi-scope Key" shows two scope entries
    And the scope entries identify "Team Admin" on team 42 and "Viewer" on portfolio 7

  @in-memory @milestone-1
  Scenario: M1.5 "Scopes" cell shows "Unrestricted" when key has zero scope rows (ADR-004 default)
    Given RBAC is enabled on the deployment
    And one of my API keys is named "Legacy Key" with no scope rows
    When I open the API Keys tab
    Then the row for "Legacy Key" shows a single scope entry labelled "Unrestricted"

  @real-io @milestone-1 @adapter-integration @driving_adapter
  Scenario: M1.6 Backend GET /api/v1/apikeys response includes a scopes array and drops createdByUser
    Given an authenticated user with one API key that has a scope row "PortfolioAdmin on portfolio 7"
    When the client sends GET /api/v1/apikeys
    Then the response status is 200
    And the response body contains exactly one element
    And that element has a "scopes" property which is a JSON array of length 1
    And the single scopes element has "role" = "PortfolioAdmin", "scopeType" = "Portfolio", "scopeId" = 7
    And the element does not have a "createdByUser" property

  @in-memory @milestone-1
  Scenario: M1.7 "Scopes" column is hidden while the authorization summary is still loading
    Given the authorization summary fetch has not yet resolved
    And one of my API keys is named "CI Key"
    When I open the API Keys tab
    Then the table column headers do not include "Scopes"

  @in-memory @milestone-1
  Scenario: M1.8 Scope cell resolves team / portfolio name when the lookup completes; falls back to "Team #{id}" otherwise
    Given RBAC is enabled on the deployment
    And the teams lookup contains a team with id 42 named "Platform"
    And the portfolios lookup is unavailable for this session
    And one of my API keys is named "Mixed Key" with scope rows:
      | role            | scopeType | scopeId |
      | TeamAdmin       | Team      | 42      |
      | PortfolioAdmin  | Portfolio | 7       |
    When I open the API Keys tab
    Then the row for "Mixed Key" displays the team scope as "Team Admin · Platform"
    And the row for "Mixed Key" displays the portfolio scope as "Portfolio Admin · Portfolio #7"
