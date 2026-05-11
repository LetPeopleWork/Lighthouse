Feature: Authenticated non-admin user manages their own API keys via Settings
  As an authenticated Lighthouse user without a System Admin role
  I want to open the API Keys tab in Settings and create a personal API key
  So that I can authenticate CLI and MCP clients without asking a System Admin

  @walking_skeleton @real-io @driving_adapter
  Scenario: Authenticated non-admin opens Settings, sees API Keys tab, and creates a key end-to-end
    Given Lighthouse is running with authentication enabled and RBAC enabled
    And Jordan is signed in as an authenticated user with no System Admin role
    When Jordan navigates to "/settings"
    Then a tab labelled "API Keys" is visible in the Settings navigation
    When Jordan opens the "API Keys" tab
    And Jordan submits a new API key request with name "Jordan CLI" and description "Local CLI client"
    Then the create response is "201 Created"
    And the response body contains a non-empty plaintext key value exactly once
    When Jordan reloads the API Keys list
    Then the list contains exactly one row for "Jordan CLI"
    And the row records Jordan as the "Created By" user
