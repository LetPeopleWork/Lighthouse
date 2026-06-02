@US-03 @milestone-3
Feature: Forecast-over-time stacked on done — the on-track read
  As a Delivery Forecaster
  I want the daily "how many will we complete by the target date" forecast recorded forward and read back against the backlog
  So that I can read directly whether the delivery is on track at the target date

  # Backend acceptance — WebApplicationFactory<Program>, real EF context. The forecast-how-many
  # series is pinned to the 85% Monte-Carlo percentile (matching the customer Excel "to 85%").
  # No new RAG endpoint — the on-track read is geometric (D8). [Ignore] until DELIVER.

  @real-io @driving_adapter
  Scenario: The metrics-history endpoint returns the recorded forecast-how-many series
    Given a delivery with snapshots carrying a recorded forecast-how-many figure per day
    When the forecaster requests the delivery's metrics history
    Then each point carries the recorded forecast-how-many projection for that day
    And the forecast figure reflects the 85% completion-by-target-date percentile

  @real-io
  Scenario: An on-track delivery reads differently from an at-risk delivery
    Given an on-track delivery whose done plus forecast meets the backlog at the target date
    And an at-risk delivery whose done plus forecast falls short of the backlog at the target date
    When the forecaster requests each delivery's metrics history
    Then the on-track delivery's done-plus-forecast reaches the backlog by the target date
    And the at-risk delivery's done-plus-forecast stays below the backlog at the target date

  @real-io @error
  Scenario: A sparse forecast series is annotated as building forward, not treated as missing
    Given a delivery with only one day of recorded forecast data
    When the forecaster requests the delivery's metrics history
    Then the response carries the single recorded forecast point
    And the first-snapshot date is present so the chart can annotate "builds forward from that date"

  @real-io @error
  Scenario: A delivery with no forecast recorded yet exposes a null forecast series
    Given a delivery whose snapshots carry backlog and done but no forecast figure
    When the forecaster requests the delivery's metrics history
    Then every point carries backlog and done
    And no point carries a forecast-how-many figure
