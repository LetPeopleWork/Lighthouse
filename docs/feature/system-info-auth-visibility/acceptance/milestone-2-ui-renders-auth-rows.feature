Feature: System Info display renders authentication, authorization, and emergency-admin rows
  As an authenticated user on Settings -> System Info
  I want to see the auth posture alongside the existing OS/runtime/database rows
  So that I can verify the deployment is secured without leaving the UI

  @in-memory @milestone-2 @driving_adapter
  Scenario: All three rows are present when auth and RBAC are enabled with an emergency admin
    Given the SystemInfo API responds with:
      | authenticationEnabled  | true                |
      | authorizationEnabled   | true                |
      | emergencyAdminSubjects | ["alex@example.com"] |
    When the user opens Settings -> System Info
    Then a row "Authentication" with value "Enabled" is visible
    And a row "Authorization" with value "Enabled" is visible
    And a row "Emergency Admin" with value "alex@example.com" is visible

  @in-memory @milestone-2
  Scenario: Authentication and Authorization rows display "Disabled" when toggled off
    Given the SystemInfo API responds with:
      | authenticationEnabled  | false |
      | authorizationEnabled   | false |
      | emergencyAdminSubjects | []    |
    When the user opens Settings -> System Info
    Then a row "Authentication" with value "Disabled" is visible
    And a row "Authorization" with value "Disabled" is visible

  @in-memory @milestone-2 @error
  Scenario: Emergency Admin row is hidden when no subjects are configured
    Given the SystemInfo API responds with:
      | authenticationEnabled  | true |
      | authorizationEnabled   | true |
      | emergencyAdminSubjects | []   |
    When the user opens Settings -> System Info
    Then no row labelled "Emergency Admin" is rendered

  @in-memory @milestone-2 @error
  Scenario: Emergency Admin row is hidden when RBAC is disabled even if subjects are configured
    Given the SystemInfo API responds with:
      | authenticationEnabled  | true                |
      | authorizationEnabled   | false               |
      | emergencyAdminSubjects | ["alex@example.com"] |
    When the user opens Settings -> System Info
    Then no row labelled "Emergency Admin" is rendered

  @in-memory @milestone-2
  Scenario: Multiple emergency admins render as a comma-separated value
    Given the SystemInfo API responds with:
      | authenticationEnabled  | true                                       |
      | authorizationEnabled   | true                                       |
      | emergencyAdminSubjects | ["alex@example.com", "sam@example.com"] |
    When the user opens Settings -> System Info
    Then a row "Emergency Admin" with value "alex@example.com, sam@example.com" is visible
