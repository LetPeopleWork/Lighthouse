@integration @US-01
Feature: The per-day target is recorded alongside the existing delivery metrics
  As the Lighthouse system
  I want the per-day target recorded alongside the existing series in the same daily snapshot
  So that the moving-target story is purely additive and breaks nothing already shipped

  # Backend acceptance — WebApplicationFactory<Program>, real EF context. Proves the
  # additive field composes with the shipped delivery-metrics recorder + metrics-history
  # contract (no new endpoint, no regression). [Ignore] until DELIVER.

  @real-io @kpi
  Scenario: The recorder captures the target in the same pass as the other series
    Given a portfolio whose forecasts have just been updated
    When the recorder records the daily snapshots
    Then each delivery's snapshot carries its recorded target alongside its backlog, done, forecast, and likelihood

  @real-io @driving_adapter
  Scenario: The existing delivery metrics are unchanged when the per-day target is added
    Given a delivery with a full recorded history
    When the forecaster requests the delivery's metrics history
    Then the backlog, done, estimated, forecast, likelihood, and when-distribution series are returned as before
    And each point additionally carries its recorded target date

  @real-io @driving_adapter @error
  Scenario: Reading the history requires premium and portfolio read access as before
    Given a non-premium instance or a caller without portfolio read access
    When the caller requests the delivery's metrics history
    Then the history is not exposed
