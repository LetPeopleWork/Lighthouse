Feature: SystemInfo API exposes authentication, authorization, and emergency-admin fields
  As an authenticated user opening Settings -> System Info
  I want the backend to report whether auth and RBAC are enabled and which subjects are emergency admins
  So that the frontend can render the same posture the operator sees in the terminal

  @real-io @adapter-integration @milestone-1
  Scenario: Auth enabled and RBAC enabled with one emergency admin
    Given a Lighthouse server is configured with:
      | Authentication:Enabled                    | true                |
      | Authorization:Enabled                     | true                |
      | Authorization:EmergencySystemAdminSubjects | ["alex@example.com"] |
    When an authenticated client requests GET /api/latest/SystemInfo
    Then the JSON response contains "authenticationEnabled": true
    And the JSON response contains "authorizationEnabled": true
    And the JSON response contains "emergencyAdminSubjects": ["alex@example.com"]

  @real-io @adapter-integration @milestone-1
  Scenario: Auth disabled and RBAC disabled with no emergency admins
    Given a Lighthouse server is configured with:
      | Authentication:Enabled                    | false |
      | Authorization:Enabled                     | false |
      | Authorization:EmergencySystemAdminSubjects | []    |
    When an authenticated client requests GET /api/latest/SystemInfo
    Then the JSON response contains "authenticationEnabled": false
    And the JSON response contains "authorizationEnabled": false
    And the JSON response contains "emergencyAdminSubjects": []

  @real-io @adapter-integration @milestone-1 @error
  Scenario: Multiple emergency admins are reported verbatim
    Given a Lighthouse server is configured with:
      | Authentication:Enabled                    | true                                       |
      | Authorization:Enabled                     | true                                       |
      | Authorization:EmergencySystemAdminSubjects | ["alex@example.com", "sam@example.com"] |
    When an authenticated client requests GET /api/latest/SystemInfo
    Then the JSON response contains "emergencyAdminSubjects": ["alex@example.com", "sam@example.com"]

  @real-io @adapter-integration @milestone-1 @error
  Scenario: Emergency admin configured while RBAC is disabled
    Given a Lighthouse server is configured with:
      | Authentication:Enabled                    | true                |
      | Authorization:Enabled                     | false               |
      | Authorization:EmergencySystemAdminSubjects | ["alex@example.com"] |
    When an authenticated client requests GET /api/latest/SystemInfo
    Then the JSON response contains "authorizationEnabled": false
    And the JSON response contains "emergencyAdminSubjects": ["alex@example.com"]

  Note: the response keeps the subject list verbatim even when RBAC is disabled, because the
  configuration is still present on disk and the operator may have just toggled RBAC off.
  The frontend (milestone 2) is responsible for suppressing the emergency-admin row when
  RBAC is disabled, since the subject has no effect at runtime.
