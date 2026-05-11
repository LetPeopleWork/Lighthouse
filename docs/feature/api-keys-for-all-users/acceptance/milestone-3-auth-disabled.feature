Feature: API Keys panel degrades gracefully when authentication is disabled
  As an operator running Lighthouse with authentication disabled
  I want the API Keys tab to remain reachable but clearly inert
  So that users understand keys are unavailable until authentication is enabled
  And so that they cannot create keys that would have no stable owner

  Background:
    Given Lighthouse is running

  @in-memory @milestone-3 @error
  Scenario: Tab is visible but panel shows the "Enable authentication" alert when auth is disabled
    Given authentication is disabled at runtime
    When the Settings page is rendered
    And the user opens the API Keys tab
    Then a tab with testId "api-keys-tab" is in the DOM
    And an alert with testId "api-keys-disabled-message" is visible
    And the alert text contains "Authentication is not enabled"

  @in-memory @milestone-3 @error
  Scenario: Create button is disabled when auth is disabled
    Given authentication is disabled at runtime
    When the Settings page is rendered
    And the user opens the API Keys tab
    Then the button with testId "create-api-key-button" is rendered
    And the button with testId "create-api-key-button" is disabled
