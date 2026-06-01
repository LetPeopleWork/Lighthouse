@feature:lighthouse-user-survey
Feature: Lighthouse User Survey — walking skeleton

  The thinnest end-to-end slice that delivers real value with NO Lighthouse app
  change: a community member opens the standalone survey page from a shared link,
  answers the questions, and submits; the response is recorded anonymously, the
  maintainer is notified, and the maintainer can see the response in the internal
  dashboard. This is the demo a non-technical stakeholder confirms is "what
  community users — and the maintainer — need".

  @walking_skeleton @driving_port @real-io @US-01 @US-02 @US-05 @US-08 @slice-01
  Scenario: A community member shares feedback and the maintainer is notified and can read it
    Given a community member opens the survey page from a shared link
    When the community member answers the questions and submits
    Then the community member sees a thank-you confirming the feedback was received
    And the response is recorded anonymously with no personal information
    And the maintainer is notified that a new response came in
    And the maintainer sees the response in the internal survey view
