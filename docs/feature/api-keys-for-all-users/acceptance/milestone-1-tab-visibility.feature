Feature: API Keys tab is visible to every authenticated user in System Settings
  As any authenticated Lighthouse user
  I want the API Keys tab to appear in the Settings page regardless of my role
  So that I can manage my own personal API keys without depending on a System Admin

  Background:
    Given Lighthouse is running with authentication enabled
    And RBAC enforcement is enabled

  @in-memory @milestone-1 @driving_adapter
  Scenario: API Keys tab is visible to a System Admin
    Given the current user has the System Admin role
    When the Settings page is rendered
    Then a tab with testId "api-keys-tab" is in the DOM

  @in-memory @milestone-1 @driving_adapter
  Scenario: API Keys tab is visible to an authenticated Team Admin (no System Admin role)
    Given the current user has the Team Admin role on at least one team
    And the current user does NOT have the System Admin role
    When the Settings page is rendered
    Then a tab with testId "api-keys-tab" is in the DOM
    And the tab with testId "configuration-tab" is NOT in the DOM
    And the tab with testId "rbac-tab" is NOT in the DOM

  @in-memory @milestone-1 @driving_adapter
  Scenario: API Keys tab is visible to an authenticated Portfolio Admin (no System Admin role)
    Given the current user has the Portfolio Admin role on at least one portfolio
    And the current user does NOT have the System Admin role
    When the Settings page is rendered
    Then a tab with testId "api-keys-tab" is in the DOM

  @in-memory @milestone-1 @driving_adapter
  Scenario: API Keys tab is visible to an authenticated Viewer (no admin role at all)
    Given the current user has no Team Admin, Portfolio Admin, or System Admin role
    When the Settings page is rendered
    Then a tab with testId "api-keys-tab" is in the DOM
    And the tab with testId "system-info-tab" is in the DOM
    And the tab with testId "configuration-tab" is NOT in the DOM
    And the tab with testId "rbac-tab" is NOT in the DOM

  @in-memory @milestone-1
  Scenario: API Keys tab is visible when RBAC enforcement is disabled
    Given RBAC enforcement is disabled
    When the Settings page is rendered
    Then a tab with testId "api-keys-tab" is in the DOM

  @in-memory @milestone-1 @error
  Scenario: Non-admin lands on the first visible tab when their previously selected tab is now hidden
    Given the current user has no Team Admin, Portfolio Admin, or System Admin role
    And the URL has no tab query parameter
    When the Settings page is rendered
    Then the user lands on the first visible tab
    And the panel for the selected tab is the only visible content area
