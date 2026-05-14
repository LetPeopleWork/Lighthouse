Feature: API Keys listing surfaces per-key scopes on RBAC-enabled deployments
  As a Lighthouse user managing my own API keys
  I want the listing screen to show each key's permission scopes when RBAC is on
  So that I can audit per-key blast-radius without reopening the create dialog

  Background:
    Given authentication is enabled on the deployment
    And I have at least one API key associated with my account

  @walking_skeleton @in-memory @driving_adapter
  Scenario: User on RBAC-enabled deployment opens the API Keys tab and sees a Scopes column
    Given RBAC is enabled on the deployment
    And one of my API keys is named "CI Key" and has a scope row "Team Admin on team #42"
    When I open the System Settings page and select the API Keys tab
    Then the API Keys table is rendered
    And the table column headers do not include "Created By"
    And the table column headers include "Scopes"
    And the row for "CI Key" shows a scope entry corresponding to "Team Admin · Team #42"
