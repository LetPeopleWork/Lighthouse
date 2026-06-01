@feature:flow-forecasting-readiness-assessment @US-01 @slice-01
Feature: Take the six-question assessment one at a time

  A visitor answers six 0-to-3 questions one screen at a time, with a "N of 6"
  progress indicator, back navigation that preserves prior answers, refresh-resume
  from the in-session draft, and a guard that blocks a result until all six are
  answered. The flow is usable on a narrow phone.

  Background:
    Given a visitor opens the forecasting readiness assessment

  @driving_port @in-memory @pending
  Scenario: Visitor advances through all six questions one at a time
    When the visitor answers each of the six questions on its own screen
    Then the progress indicator advances from "1 of 6" to "6 of 6"
    And the assessment is ready to produce a result

  @driving_port @in-memory @pending
  Scenario: Visitor corrects an earlier answer without losing progress
    Given the visitor has answered the first three questions
    When the visitor goes back to the third question and changes the answer
    Then the answers to the first and second questions are still intact
    And the changed answer is the one carried forward

  @error @driving_port @in-memory @pending
  Scenario: Visitor refreshes mid-assessment and resumes where they left off
    Given the visitor has answered the first four questions
    When the visitor refreshes the page
    Then the four earlier answers are restored
    And the visitor resumes at the fifth question

  @error @driving_port @in-memory @pending
  Scenario: Visitor returns to a cleared assessment and is restarted gently
    Given the visitor had started the assessment but the in-session draft is gone
    When the visitor returns to the assessment
    Then the visitor is restarted at the first question with a gentle notice

  @error @driving_port @in-memory @pending
  Scenario: A result cannot be produced before all six questions are answered
    Given the visitor has answered only the first two questions
    When the visitor tries to jump straight to the result
    Then no result is produced
    And the visitor is taken to the first unanswered question

  @driving_port @in-memory @pending
  Scenario: The assessment is usable on a narrow phone screen
    Given the visitor is on a phone-width screen of 375 pixels
    When the visitor works through the questions
    Then each question is presented in a single readable column
    And every answer choice is reachable without horizontal scrolling
