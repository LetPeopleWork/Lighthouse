@walking_skeleton @driving_adapter @US-01 @premium @real-io
Feature: Open a delivery's Metrics tab and read its burnup over time
  As a Delivery Forecaster preparing a leadership status report
  I want to open a delivery and see how its backlog and done have moved over time
  So that I can tell an honest trend story instead of defending a single snapshot number

  # E2E walking skeleton — Playwright, Page Object Model, seeded demo data.
  # Forward-only reality: a fresh demo instance has NO snapshot history, so the
  # burnup is empty until the recorder accrues data. DELIVER must extend the demo
  # seeding with DeliveryMetricSnapshot rows (see the demo-data-time-in-state
  # CSV-column precedent) so this scenario renders a populated burnup.
  # Skipped via test.fixme until DELIVER wires the endpoint + chart.

  Scenario: Forecaster opens a delivery's Metrics tab and sees the backlog and done lines
    Given a premium Lighthouse instance with a seeded portfolio and a delivery that has recorded snapshots
    And the forecaster is viewing the portfolio's deliveries
    When the forecaster opens the delivery and selects its "Metrics" tab
    Then the delivery burnup chart is visible
    And the burnup shows a backlog line and a done line over time
    And switching back to the "Work Items" tab still shows the existing feature grid
