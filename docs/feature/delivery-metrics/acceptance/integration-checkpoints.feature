@integration @US-01
Feature: Integration checkpoints — recorder freshness, single-endpoint shape, gates
  These checkpoints guard the seams the journey depends on: that the recorder records
  POST-forecast figures (not the stale pre-forecast ones), that one consolidated endpoint
  carries every series, and that premium and RBAC gates wrap the read.

  # Backend acceptance — WebApplicationFactory<Program>, real EF context. [Ignore] until DELIVER.

  @real-io @kpi
  Scenario: The recorder records fresh post-forecast figures, not stale pre-forecast ones
    Given a delivery whose forecast figures change when the portfolio forecast update runs
    When the recorder reacts to the post-forecast portfolio-forecasts-updated event
    Then the recorded forecast and likelihood match the just-computed fresh figures
    And they do not match the stale pre-forecast figures

  @real-io @kpi
  Scenario: The forecasts-updated event is dispatched once per portfolio forecast completion
    Given a portfolio whose forecast update completes on the portfolio-update path
    And a portfolio whose forecast update completes on the forecast-update path
    When each forecast update completes
    Then the portfolio-forecasts-updated event is dispatched exactly once on each path

  @real-io @driving_adapter
  Scenario: One consolidated endpoint carries the actual, inferred, forecast, and predictability series
    Given a delivery with snapshots carrying every recorded series
    When the forecaster requests the delivery's metrics history
    Then a single response carries the backlog, done, estimated-total, forecast, likelihood, and when-distribution series

  @real-io @premium @rbac @error
  Scenario: The read is gated by premium and portfolio read access
    Given a delivery on a non-premium instance
    When the forecaster requests the delivery's metrics history
    Then the metrics history is not available
