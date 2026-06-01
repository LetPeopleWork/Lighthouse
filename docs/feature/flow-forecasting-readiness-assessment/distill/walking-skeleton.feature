@feature:flow-forecasting-readiness-assessment
Feature: Forecasting Readiness Assessment — walking skeleton

  The thinnest end-to-end slice that delivers real value: a visitor opens the
  assessment, answers all six questions, and sees a memorable number, a named
  band, and a band-specific next step — with no email asked and nothing stored.
  This is the demo a non-technical stakeholder confirms is "what visitors need".

  @walking_skeleton @driving_port @real-io @US-01 @US-02 @US-03 @US-05 @slice-01
  Scenario: Visitor completes the assessment and sees a number, a band, and a next step
    Given a visitor opens the forecasting readiness assessment
    When the visitor answers all six questions
    Then the visitor sees a readiness score out of 100
    And the visitor sees the name of the band that matches that score
    And the visitor sees a recommended next step for that band
