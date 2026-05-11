Feature: Terminal startup banner reports authentication, authorization, and emergency-admin status
  As an operator watching the Lighthouse process start
  I want the existing ASCII banner to include auth posture lines next to the URL, OS, runtime, and DB lines
  So that I can verify security configuration in CI logs and container output

  @real-io @driving_adapter @milestone-3
  Scenario: Banner includes Authentication line set to "Enabled" when auth is enabled
    Given the host configuration sets Authentication:Enabled = true
    When the Lighthouse process starts and prints its startup banner
    Then the banner contains a line whose label is "Authentication" and value is "Enabled"

  @real-io @driving_adapter @milestone-3
  Scenario: Banner includes Authentication line set to "Disabled" when auth is disabled
    Given the host configuration sets Authentication:Enabled = false
    When the Lighthouse process starts and prints its startup banner
    Then the banner contains a line whose label is "Authentication" and value is "Disabled"

  @real-io @driving_adapter @milestone-3
  Scenario: Banner includes Authorization line set to "Enabled" when RBAC is enabled
    Given the host configuration sets Authorization:Enabled = true
    When the Lighthouse process starts and prints its startup banner
    Then the banner contains a line whose label is "Authorization" and value is "Enabled"

  @real-io @driving_adapter @milestone-3 @error
  Scenario: Emergency Admin line is omitted when no subjects are configured
    Given the host configuration sets Authorization:EmergencySystemAdminSubjects = []
    When the Lighthouse process starts and prints its startup banner
    Then the banner does not contain any line labelled "Emergency Admin"

  @real-io @driving_adapter @milestone-3
  Scenario: Emergency Admin line lists the configured subjects when present
    Given the host configuration sets:
      | Authorization:Enabled                     | true                                      |
      | Authorization:EmergencySystemAdminSubjects | ["alex@example.com", "sam@example.com"] |
    When the Lighthouse process starts and prints its startup banner
    Then the banner contains a line "Emergency Admin" with value "alex@example.com, sam@example.com"

  @real-io @driving_adapter @milestone-3 @error
  Scenario: Emergency Admin line is omitted when RBAC is disabled even if subjects are configured
    Given the host configuration sets:
      | Authorization:Enabled                     | false               |
      | Authorization:EmergencySystemAdminSubjects | ["alex@example.com"] |
    When the Lighthouse process starts and prints its startup banner
    Then the banner does not contain any line labelled "Emergency Admin"

  Note: line ordering and emoji selection are left to implementation. The crafter chooses
  consistent labels and emojis matching the surrounding style (see Program.cs existing
  Line(emoji, label, value) helper).
