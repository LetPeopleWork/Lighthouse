@feature:lighthouse-user-survey @US-05
Feature: The maintainer reviews survey responses and trial requests

  The maintainer reads the collected feedback on the existing internal dashboard,
  which gains a survey view. Responses are listed — anonymous unless a member
  volunteered a trial email — alongside the per-question tallies, and trial
  requests are listed with their emails so the maintainer can follow up by hand.
  The view reuses the existing dashboard sign-in and layout and does not redesign
  the platform, so feedback is never visible to anyone who is not signed in.

  @driving_port @in-memory @US-05 @slice-01 @pending
  Scenario: The maintainer sees survey responses listed with per-question tallies
    Given several survey responses have been collected
    When a signed-in maintainer opens the survey view
    Then the responses are listed
    And the responses without a volunteered email are shown as anonymous
    And the per-question answer tallies are shown

  @driving_port @in-memory @US-05 @slice-03 @pending
  Scenario: The maintainer sees trial requests with their emails
    Given some respondents have asked for a trial
    When a signed-in maintainer opens the trial-requests view
    Then each trial request is listed with its volunteered email

  @driving_port @in-memory @US-05 @slice-02 @pending
  Scenario: Responses from old and new question sets are both readable
    Given responses were collected under an earlier set of questions and under the current set
    When a signed-in maintainer opens the survey view
    Then both the earlier and the current responses are readable

  @error @security @real-io @adapter-integration @US-05 @slice-01 @pending
  Scenario: Survey feedback is not visible to anyone who is not signed in
    Given several survey responses and trial requests have been collected
    When someone who is not signed in tries to read the survey feedback
    Then no responses are returned
    And no trial requests are returned
