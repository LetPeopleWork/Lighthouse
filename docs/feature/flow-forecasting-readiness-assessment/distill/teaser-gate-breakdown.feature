@feature:flow-forecasting-readiness-assessment @slice-03
Feature: See the teaser, unlock the breakdown, and act on the next step

  On completion the visitor sees a teaser — the number, the band, and the
  framework credibility anchor — with no email required and the full breakdown
  visibly gated below. A single volunteered email unlocks the band-specific
  breakdown in place, which explains the band across both pillars (what the team
  measures and how it forecasts) and offers the band's recommended next steps.
  The free Community next step is present in every band. A capture failure never
  costs the visitor the breakdown.

  Background:
    Given a visitor has completed the assessment with a result of "Flow-aware" at 58 out of 100

  @driving_port @in-memory @US-03 @pending
  Scenario: Teaser shows the number and band without asking for an email
    When the results come into view
    Then the visitor sees "58 / 100" and the band "Flow-aware" prominently
    And no email is required to see the teaser
    And the full breakdown is shown as gated below the teaser

  @driving_port @in-memory @US-03 @pending
  Scenario: The credibility anchor is visible on the teaser
    When the results come into view
    Then the framework credibility attribution is visible

  @driving_port @in-memory @US-04 @pending
  Scenario: A valid email unlocks the breakdown and records the lead
    When the visitor submits a valid email address
    Then the full band breakdown unlocks in place
    And the email and the full set of answers, score and band are recorded as a readiness-assessment lead

  @error @driving_port @in-memory @US-04 @pending
  Scenario: An invalid email is rejected and nothing is recorded
    When the visitor submits "tomas@"
    Then an inline validation error is shown
    And the breakdown stays gated
    And nothing is recorded

  @error @driving_port @in-memory @US-04 @pending
  Scenario: A capture failure never costs the visitor the breakdown
    Given the lead-recording service is unavailable
    When the visitor submits a valid email address
    Then the full band breakdown still unlocks
    And a non-blocking notice is shown
    And recording the lead is retried once

  @driving_port @in-memory @US-05 @pending
  Scenario: The breakdown explains the band across both pillars
    Given the visitor has unlocked the breakdown
    When the visitor reads the explanation
    Then it speaks to both what the team measures and how it forecasts
    And it names the next rung to climb

  @driving_port @in-memory @US-05 @pending
  Scenario Outline: The recommended next steps match the band
    Given a visitor has unlocked the breakdown for the "<band>" band
    When the breakdown comes into view
    Then a free Lighthouse Community next step is offered
    And the "<secondary>" next step is offered

    Examples:
      | band           | secondary                  |
      | Flying blind   | book a consulting call     |
      | Drifting | light coaching             |
      | Predictable  | paid and portfolio tiers   |

  @property @driving_port @in-memory @US-05 @pending
  Property: The free Community next step appears in every band
    Given any of the four bands
    When that band's breakdown comes into view
    Then a free Lighthouse Community next step is present

  @driving_port @in-memory @US-05 @pending
  Scenario: A completer who never gives an email still leaves a captured completion
    Given a visitor has completed the assessment and seen the teaser
    When the visitor leaves without giving an email
    Then the completion is still counted
    And no lead is recorded for that visitor
