@feature:lighthouse-user-survey @US-07
Feature: The nudge is a respectful three-choice invitation with a two-tier cadence

  Once shown, the nudge feels like an invitation rather than a nag. It is a small,
  non-blocking prompt in Lighthouse's own look and feel that links out to the survey
  page rather than embedding it, and it makes clear that feedback is completely
  optional while explaining that, because Lighthouse never tracks usage, that feedback
  is how the tool improves. As a thank-you it hints at a free Premium trial available
  at the end of the survey. The member is offered three clear choices. Taking the
  survey or declining quiets the nudge for about six months. Asking to be reminded
  later quiets it for only about one week, so someone who simply lacks time now is not
  lost. Closing the nudge with the X is treated as "remind me later", never as a
  refusal, so no one is pushed into declining out of a guilty conscience. To keep the
  weekly reminder from becoming a nag, the short one-week cadence backs off to the
  six-month cadence after the member has been reminded twice. Every choice is persisted
  server-side, so the cadence survives a restart and is not held only in the session.

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: Taking the survey opens it and quiets the nudge for about six months
    Given the nudge is shown to a community member
    When the community member chooses to take the survey
    Then the standalone survey page opens at its stable address
    And the nudge does not reappear until about six months have passed

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: Declining with no interest quiets the nudge for about six months
    Given the nudge is shown to a community member
    When the community member chooses no interest
    Then the nudge closes without opening the survey
    And the nudge does not reappear until about six months have passed

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: Asking to be reminded later quiets the nudge for only about one week
    Given the nudge is shown to a community member
    When the community member chooses to be reminded later
    Then the nudge closes without opening the survey
    And the nudge reappears after about one week

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: Closing with the X is treated as remind me later, not as a refusal
    Given the nudge is shown to a community member
    When the community member closes the nudge with the X
    Then the choice is recorded as remind me later
    And the nudge reappears after about one week

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: The one-week reminder backs off to six months after two reminders
    Given the community member has already been reminded later twice
    And the nudge is shown again
    When the community member chooses to be reminded later
    Then the nudge does not reappear until about six months have passed

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: Every choice is remembered across a restart
    Given the nudge is shown to a community member
    When the community member makes any choice
    Then the choice is persisted server-side
    And the same cadence still holds after a restart

  @driving_port @in-memory @US-07 @slice-05 @pending
  Scenario: The nudge is a non-blocking, opt-in link-out, not an embedded survey
    Given the nudge is shown to a community member
    When the nudge is on screen
    Then it is non-blocking and dismissible
    And it makes clear that the survey is completely optional
    And it hints at a free trial as a thank-you
    And it links out to the survey page rather than embedding the questions

  @error @property @driving_port @in-memory @US-07 @slice-05 @pending
  Property: A clock that jumps backward never re-shows the nudge early
    Given the nudge has been quieted until a future instant
    When the clock is skewed or jumps backward
    Then the nudge is still treated as quieted
    And the nudge is not shown
