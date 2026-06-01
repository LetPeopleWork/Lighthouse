@feature:lighthouse-user-survey @US-04 @US-08
Feature: Optionally raise a hand for a trial by volunteering an email

  A community member may choose to ask for a premium trial by giving an email on
  the final screen. When they do, a trial request and the email are recorded for
  a human to action, and the maintainer's notification names the trial and the
  email. The email is the ONLY personal datum ever stored, and only on an explicit
  opt-in — a member who does not opt in stays fully anonymous. Asking for a trial
  never grants a license automatically; a human follows up. Invalid emails are
  caught kindly without losing the rest of the answers, and if the trial request
  specifically fails to record, the member is told so they can retry just that.

  Background:
    Given a community member has answered the questions on the survey page

  @driving_port @in-memory @US-04 @US-08 @slice-03 @pending
  Scenario: Opting in with a valid email records a trial request and names it in the notification
    Given the community member opts in to a trial and enters a valid email
    When the community member submits and the recording succeeds
    Then a trial request and the email are recorded for a human to action
    And the maintainer is notified that a trial was requested
    And the notification includes the volunteered email
    And the thank-you notes that a human will follow up

  @driving_port @in-memory @US-04 @slice-03 @pending
  Scenario: A member who does not opt in stays anonymous and stores no email
    Given the community member does not opt in to a trial
    When the community member submits and the recording succeeds
    Then no email is stored anywhere
    And the response stays anonymous

  @driving_port @in-memory @US-04 @slice-03 @pending
  Scenario: Asking for a trial never grants a license automatically
    Given the community member opts in to a trial and enters a valid email
    When the community member submits and the trial request is recorded
    Then no premium license is granted automatically
    And the trial request waits for a human to action it

  @error @driving_port @in-memory @US-04 @slice-03 @pending
  Scenario: An invalid email is caught kindly without losing the answers
    Given the community member opts in to a trial and enters an invalid email
    When the community member submits
    Then the community member sees a friendly inline validation message
    And the rest of the answers are preserved

  @error @driving_port @in-memory @US-04 @slice-03 @pending
  Scenario: A trial request that fails to record is surfaced on its own
    Given the community member opts in to a trial and enters a valid email
    And the response records but the trial request fails to record
    When the community member submits
    Then the community member is told the trial request specifically did not go through
    And the community member can retry the trial opt-in
