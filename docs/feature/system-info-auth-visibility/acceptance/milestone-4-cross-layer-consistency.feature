Feature: Banner and Settings UI report the same auth posture for the same configuration
  As an operator who first sees the startup banner and later opens the System Info page
  I want to trust that both surfaces describe the same running configuration
  So that I can confidently verify the security state of the deployment without ambiguity

  @real-io @cross-layer @milestone-4 @driving_adapter
  Scenario: Banner labels and API fields agree for an auth-enabled, RBAC-enabled instance with an emergency admin
    Given a Lighthouse server is configured with:
      | Authentication:Enabled                    | true                |
      | Authorization:Enabled                     | true                |
      | Authorization:EmergencySystemAdminSubjects | ["alex@example.com"] |
    When the process starts and prints its startup banner
    And an authenticated client subsequently requests GET /api/latest/SystemInfo
    Then the banner reports "Authentication" = "Enabled" AND the API returns authenticationEnabled = true
    And the banner reports "Authorization" = "Enabled" AND the API returns authorizationEnabled = true
    And the banner lists "Emergency Admin" = "alex@example.com" AND the API returns emergencyAdminSubjects = ["alex@example.com"]

  @real-io @cross-layer @milestone-4 @error
  Scenario: Banner and API agree when auth is disabled and no emergency admin is configured
    Given a Lighthouse server is configured with:
      | Authentication:Enabled                    | false |
      | Authorization:Enabled                     | false |
      | Authorization:EmergencySystemAdminSubjects | []    |
    When the process starts and prints its startup banner
    And an authenticated client subsequently requests GET /api/latest/SystemInfo
    Then the banner reports "Authentication" = "Disabled" AND the API returns authenticationEnabled = false
    And the banner reports "Authorization" = "Disabled" AND the API returns authorizationEnabled = false
    And the banner does NOT contain an "Emergency Admin" line AND the API returns emergencyAdminSubjects = []

  Note: this milestone is the cross-layer journey gate. It enforces that the banner
  (operator-facing, pre-login) and the System Info display (user-facing, post-login) read
  from the SAME `IConfiguration` and report values that round-trip without drift. The
  scenario is implemented as a single backend integration test that starts a real
  `WebApplicationFactory<Program>`, captures the banner output, then issues an HTTP
  request to /api/latest/SystemInfo, and asserts the equivalences above.
