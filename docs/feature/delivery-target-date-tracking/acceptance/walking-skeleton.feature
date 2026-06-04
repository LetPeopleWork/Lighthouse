@walking_skeleton @driving_adapter @US-01 @premium @real-io
Feature: See a delivery's moving target on the predictability When? view
  As a Delivery Forecaster preparing a leadership status report
  I want the predictability When? view to show the target as it stood on each recorded day
  So that I can read the forecast against the target that actually applied, not just today's

  # E2E walking skeleton — Playwright, Page Object Model, seeded demo data.
  # Forward-only reality: the stepped target line only appears once the delivery has
  # accrued snapshots across at least one target-date change. DELIVER must extend the
  # demo seeding with DeliveryMetricSnapshot rows whose TargetDateAtSnapshot varies
  # across days (see the demo-data-time-in-state CSV-column precedent).
  # Skipped via test.fixme until DELIVER wires the column + step line.

  Scenario: Forecaster opens a replanned delivery and sees the target step on the When? view
    Given a premium Lighthouse instance with a seeded delivery whose target date was moved during its life
    And the forecaster is viewing the portfolio's deliveries
    When the forecaster opens the delivery, selects its "Metrics" tab, and switches the predictability chart to the "When?" view
    Then the predictability chart shows the forecast completion-date percentiles over time
    And the target is drawn as a stepped line that holds at the earlier target and steps to the later target where it was moved
