@feature:flow-forecasting-readiness-assessment @US-06
Feature: Capture completions and review leads on an internal dashboard

  Every completed assessment is captured once, marked as a readiness-assessment
  response, carrying the answers, the raw total, the score, and the band. The
  LetPeopleWork team reviews captured leads on a protected internal results
  dashboard showing total responses, the email-capture count, the band
  distribution, and a lead table. The captured response carries no personal
  information beyond the volunteered email, and the answers and the email are
  kept separate so the answers stay anonymous.

  @driving_port @real-io @adapter-integration @US-06 @slice-02 @pending
  Scenario: A completed assessment is captured as a single response
    Given a visitor completes the assessment
    When the result is computed
    Then exactly one response is captured
    And the captured response carries the answers, the raw total, the score, the band, and a readiness-assessment marker

  @driving_port @in-memory @US-06 @slice-04 @pending
  Scenario: The internal dashboard summarizes captured results
    Given several assessments have been completed and some emails captured
    When a signed-in team member opens the results dashboard
    Then they see the total number of responses
    And they see how many emails were captured
    And they see how the results are distributed across the four bands
    And they see a lead table of email, score, band, and date

  @driving_port @in-memory @US-06 @slice-04 @pending
  Scenario: The dashboard filters by the kind of response it shows
    Given both readiness-assessment responses and other kinds of responses have been captured
    When a signed-in team member views the readiness-assessment results
    Then only readiness-assessment responses are counted and listed

  @error @security @driving_port @US-06 @slice-04 @pending
  Scenario: The results dashboard is not visible to a visitor who is not signed in
    Given a visitor who is not signed in
    When the visitor goes to the results dashboard
    Then access is denied
    And no lead information is shown
