@feature:lighthouse-user-survey @US-02 @US-08
Feature: Submit a response, store it anonymously, and notify the maintainer

  A community member's answers are recorded anonymously and confirmed only once
  the record has truly landed. The recording is done in a trusted, server-side
  way — never as an open, unauthenticated write — so survey answers cannot be
  injected from outside. On every submission the maintainer is notified, and the
  notification reflects whether a trial was requested. A failed recording never
  shows a false thank-you and never loses the member's answers; a double submit
  is not counted twice; and a notification that cannot be sent never blocks the
  feedback from being recorded.

  Background:
    Given a community member has answered the questions on the survey page

  @driving_port @in-memory @US-02 @slice-01 @pending
  Scenario: A submitted response is recorded anonymously and confirmed
    When the community member submits and the recording succeeds
    Then the response is recorded and marked as survey feedback
    And no personal information is recorded with it
    And the community member sees a thank-you confirmation

  @error @driving_port @in-memory @US-02 @slice-01 @pending
  Scenario: A failed recording shows a retry-able error and never a false thank-you
    Given recording the response will fail
    When the community member submits
    Then the community member sees a clear, retry-able error
    And the community member does not see a thank-you
    And the answers are preserved so the member can retry

  @error @driving_port @in-memory @US-02 @slice-01 @pending
  Scenario: A double submit is not counted twice
    When the community member submits the same answers twice in quick succession
    Then the feedback is recorded only once

  @security @real-io @adapter-integration @US-02 @US-08 @slice-01 @pending
  Scenario: Survey answers are recorded only through the trusted server-side path
    When the community member submits
    Then the answers are recorded through the trusted server-side path
    And the answers cannot be recorded through the open, unauthenticated write path

  @error @security @real-io @adapter-integration @US-02 @slice-01 @pending
  Scenario: A submission carrying a smuggled score or band is refused and recorded nowhere
    Given a submission arrives carrying a score or band it was never meant to contain
    When the submission is received
    Then the submission is refused
    And no response is recorded

  @driving_port @in-memory @US-08 @slice-01 @pending
  Scenario: A submission without a trial request notifies the maintainer with no email
    Given the community member did not ask for a trial
    When the community member submits and the recording succeeds
    Then the maintainer is notified that a new response came in
    And the notification states that no trial was requested
    And the notification contains no email address

  @error @real-io @adapter-integration @US-08 @slice-01 @pending
  Scenario: A notification that cannot be sent never blocks the recorded feedback
    Given the maintainer notification cannot be sent
    When the community member submits and the recording succeeds
    Then the response is still recorded
    And the community member still sees a thank-you confirmation
    And the failure to notify is logged
