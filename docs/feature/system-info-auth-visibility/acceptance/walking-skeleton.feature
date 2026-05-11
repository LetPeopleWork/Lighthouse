Feature: Walking skeleton -- auth posture visible end-to-end
  As an operator deploying Lighthouse
  I want to see, in one glance, whether authentication and RBAC are active
  And which subject(s) hold emergency System Admin rights
  So that I can confirm the security posture of the running instance without inspecting config files

  Background:
    Given a Lighthouse server is configured with:
      | Authentication:Enabled                    | true                |
      | Authorization:Enabled                     | true                |
      | Authorization:EmergencySystemAdminSubjects | ["alex@example.com"] |

  @walking_skeleton @real-io @driving_adapter
  Scenario: Operator sees auth posture in the terminal startup banner
    When the Lighthouse process starts up and prints its startup banner to stdout
    Then the banner contains a line reading "Authentication" with value "Enabled"
    And the banner contains a line reading "Authorization" with value "Enabled"
    And the banner contains a line reading "Emergency Admin" with value "alex@example.com"
