@US-02 @milestone-2
Feature: Mark target changes with dots on the How Likely? view
  As a Delivery Forecaster
  I want a dot on the likelihood line wherever the target moved
  So that I can attribute a likelihood jump to a replan rather than to delivery progress

  # Frontend behaviour — Vitest + RTL on DeliveryPredictabilityChart (How Likely? view)
  # + the pure deliveryTargetHistory.targetChanges helper. test.fixme/[Ignore] until DELIVER.

  @fe-component @in-memory
  Scenario: A dot marks each day the target changed on the likelihood line
    Given a metrics history whose recorded target changes on two separate days
    When the forecaster views the "How Likely?" predictability view
    Then an emphasized dot sits on the likelihood line at each of the two change days

  @fe-component @in-memory
  Scenario: Hovering a change dot shows the old and new target dates
    Given a metrics history whose recorded target moved from an earlier date to a later date
    When the forecaster hovers the change dot on the "How Likely?" view
    Then the dot reveals the move from the earlier target to the later target

  @fe-component @in-memory @error
  Scenario: No change dots appear when the target never moved
    Given a metrics history whose recorded target is the same on every point
    When the forecaster views the "How Likely?" predictability view
    Then no change dot is shown and the likelihood line's normal marks are unaffected

  @fe-component @in-memory @error
  Scenario: No change dots appear when no per-day target was recorded
    Given a metrics history whose recorded target is absent on every point
    When the forecaster views the "How Likely?" predictability view
    Then no change dot is shown

  @fe-component @in-memory
  Scenario: The When? view carries no change dot because its step line already shows the move
    Given a metrics history whose recorded target changes on one day
    When the forecaster compares the "How Likely?" and "When?" views
    Then the "How Likely?" view shows a change dot on that day
    And the "When?" view shows the change through its stepped target line with no separate dot
