@feature:flow-forecasting-readiness-assessment @security
Feature: Security and correctness boundaries for capture and the dashboard

  The boundaries the design locked: a tampered result that claims a band its
  score does not earn is refused and leaves no lead; results and leads are never
  visible to anyone who is not a signed-in team member; and the two named team
  members can read the dashboard. These are tested at the data boundary, not
  just at the page, so a bypassed page cannot expose anything.

  @error @security @real-io @adapter-integration @US-04 @US-06 @slice-03 @pending
  Scenario: A result that claims a band it did not earn is refused and recorded nowhere
    Given a visitor whose honest score is 4 out of 100
    When a submission arrives claiming the band "Predictable" for that score
    Then the submission is refused
    And no lead is recorded

  @error @security @real-io @adapter-integration @US-06 @slice-03 @pending
  Scenario: A result with an impossible score is refused and recorded nowhere
    Given a submission arrives with a score outside the range 0 to 100
    When the submission is received
    Then the submission is refused
    And no lead is recorded

  @security @real-io @adapter-integration @US-04 @slice-03 @pending
  Scenario: An honest result whose band matches its score is recorded
    Given a visitor whose honest score is 58 out of 100
    When a submission arrives claiming the band "Flow-aware" for that score
    Then the submission is accepted
    And the lead is recorded with the band "Flow-aware"

  @error @security @real-io @adapter-integration @US-06 @slice-04 @pending
  Scenario: Captured responses are not readable by anyone who is not signed in
    Given several assessments have been completed
    When someone who is not signed in tries to read the captured responses directly
    Then no responses are returned

  @error @security @real-io @adapter-integration @US-06 @slice-04 @pending
  Scenario: Captured leads are not readable by anyone who is not signed in
    Given several emails have been captured as leads
    When someone who is not signed in tries to read the captured leads directly
    Then no leads are returned

  @security @real-io @adapter-integration @US-06 @slice-04 @pending
  Scenario Outline: A named team member can read the dashboard once signed in
    Given the team member "<account>" is signed in
    When they open the results dashboard
    Then they can read the captured responses and leads

    Examples:
      | account                   |
      | benjamin@letpeople.work   |
      | peter@letpeople.work      |
