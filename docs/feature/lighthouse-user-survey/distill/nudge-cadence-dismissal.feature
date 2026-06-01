@feature:lighthouse-user-survey @US-07
Feature: The nudge is a respectful, dismissible invitation that recurs rarely

  Once shown, the nudge feels like an invitation rather than a nag. It is a small,
  non-blocking prompt in Lighthouse's own look and feel that links out to the
  survey page rather than embedding it. Whether the member clicks through to the
  survey or dismisses the nudge, the choice is remembered so the nudge does not
  reappear for about six months — and the memory survives a restart because it is
  persisted, not held only in the session.

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: Clicking through opens the survey and quiets the nudge for about six months
    Given the nudge is shown to a community member
    When the community member clicks the nudge's primary action
    Then the standalone survey page opens at its stable address
    And the time the nudge was shown is recorded
    And the nudge does not reappear until about six months have passed

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: Dismissing quiets the nudge for about six months and is remembered across restarts
    Given the nudge is shown to a community member
    When the community member dismisses the nudge
    Then the nudge closes without side effects
    And the dismissal is remembered across a restart
    And the nudge does not reappear until about six months have passed

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: The nudge is a non-blocking link-out, not an embedded survey
    Given the nudge is shown to a community member
    When the nudge is on screen
    Then it is non-blocking and dismissible
    And it links out to the survey page rather than embedding the questions

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario Outline: Both acting and dismissing remember the time so the rare cadence holds
    Given the nudge is shown to a community member
    When the community member chooses to "<choice>"
    Then the time the nudge was shown is recorded
    And the nudge does not reappear until about six months have passed

    Examples:
      | choice         |
      | click through  |
      | dismiss        |
