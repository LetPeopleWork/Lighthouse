Feature: SystemInfo auth wire-format keys match the frontend contract
  As an operator viewing Settings -> System Info
  I want the Authentication and Authorization rows to reflect the configured posture
  So that the UI agrees with the startup banner and the actual runtime configuration

  Background:
    The original milestone-1 acceptance criteria require the JSON response from
    GET /api/latest/SystemInfo to contain the keys "authenticationEnabled" and
    "authorizationEnabled". The current implementation emits "isAuthenticationEnabled"
    and "isAuthorizationEnabled" instead (camelCase of C# "IsAuthenticationEnabled"
    / "IsAuthorizationEnabled" record properties under the default ASP.NET Core
    naming policy). The frontend reads "authenticationEnabled" / "authorizationEnabled"
    so the values resolve to undefined -> falsy -> the UI always renders "Disabled".

  @real-io @adapter-integration @regression @bugfix-wire-format @driving_adapter
  Scenario: HTTP response uses the agreed property name "authenticationEnabled"
    Given a Lighthouse server is configured with:
      | Authentication:Enabled | true |
      | Authorization:Enabled  | false |
    When an authenticated client requests GET /api/latest/SystemInfo
    Then the raw JSON response contains a property named exactly "authenticationEnabled"
    And the raw JSON response does NOT contain a property named "isAuthenticationEnabled"
    And the value of "authenticationEnabled" in the raw JSON response is true

  @real-io @adapter-integration @regression @bugfix-wire-format @driving_adapter
  Scenario: HTTP response uses the agreed property name "authorizationEnabled"
    Given a Lighthouse server is configured with:
      | Authentication:Enabled | true |
      | Authorization:Enabled  | true |
    When an authenticated client requests GET /api/latest/SystemInfo
    Then the raw JSON response contains a property named exactly "authorizationEnabled"
    And the raw JSON response does NOT contain a property named "isAuthorizationEnabled"
    And the value of "authorizationEnabled" in the raw JSON response is true

  @real-io @adapter-integration @regression @bugfix-wire-format @error
  Scenario: HTTP response keeps the agreed names when auth and RBAC are disabled
    Given a Lighthouse server is configured with:
      | Authentication:Enabled | false |
      | Authorization:Enabled  | false |
    When an authenticated client requests GET /api/latest/SystemInfo
    Then the raw JSON response contains a property named exactly "authenticationEnabled" with value false
    And the raw JSON response contains a property named exactly "authorizationEnabled" with value false
    And the raw JSON response does NOT contain a property named "isAuthenticationEnabled"
    And the raw JSON response does NOT contain a property named "isAuthorizationEnabled"

  @in-memory @regression @bugfix-wire-format @driving_adapter
  Scenario: SystemInfoService parses the wire-format JSON into the SystemInfo model
    Given the backend HTTP response body is the verbatim JSON:
      """
      {
        "os": "Linux 5.15.0",
        "runtime": ".NET 10.0.7",
        "architecture": "X64",
        "processId": 1,
        "databaseProvider": "postgres",
        "databaseConnection": "Host=postgres;Database=lighthouse",
        "logPath": "/app/logs",
        "authenticationEnabled": true,
        "authorizationEnabled": false,
        "emergencyAdminSubjects": []
      }
      """
    When the SystemInfoService deserialises the response
    Then the returned SystemInfo has authenticationEnabled set to true
    And the returned SystemInfo has authorizationEnabled set to false

  @in-memory @regression @bugfix-wire-format @driving_adapter
  Scenario: SystemInfoDisplay renders "Enabled" / "Disabled" against the wire-format response
    Given the backend HTTP response body is the verbatim JSON:
      """
      {
        "os": "Linux 5.15.0",
        "runtime": ".NET 10.0.7",
        "architecture": "X64",
        "processId": 1,
        "databaseProvider": "postgres",
        "databaseConnection": "Host=postgres;Database=lighthouse",
        "logPath": "/app/logs",
        "authenticationEnabled": true,
        "authorizationEnabled": false,
        "emergencyAdminSubjects": []
      }
      """
    When the user opens Settings -> System Info
    Then the row labelled "Authentication" shows value "Enabled"
    And the row labelled "Authorization" shows value "Disabled"
