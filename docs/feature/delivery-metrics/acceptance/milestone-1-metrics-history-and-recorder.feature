@US-01 @milestone-1
Feature: Metrics-history series and the forward recorder
  As a Delivery Forecaster
  I want the delivery's backlog and done counts recorded forward each day into one store and read back as an ordered series
  So that the burnup reflects what actually happened, accruing one day at a time from the day recording begins

  # Backend acceptance — WebApplicationFactory<Program> against /api/latest, real EF context.
  # Read scenarios seed DeliveryMetricSnapshot rows directly via the real EF context over a
  # known multi-day series. Recorder scenarios publish PortfolioForecastsUpdated and assert the
  # upsert. All [Ignore] until DELIVER lands the endpoint + recorder.

  @real-io @driving_adapter
  Scenario: The metrics-history endpoint returns the recorded backlog and done series in date order
    Given a delivery with snapshots recorded over three consecutive days with known backlog and done counts
    When the forecaster requests the delivery's metrics history
    Then the response lists one point per recorded day in date order
    And each point carries that day's backlog, done, and remaining counts
    And the delivery target date is returned for the on-track marker

  @real-io @driving_adapter @error
  Scenario: An empty store yields an honest empty series, not an error
    Given a delivery for which no snapshots have been recorded yet
    When the forecaster requests the delivery's metrics history
    Then the response succeeds with an empty list of points
    And the first-snapshot date is absent so the chart can show "builds forward from today"

  @real-io @kpi
  Scenario: The recorder upserts one row per delivery per day when forecasts are updated
    Given a delivery with known current backlog and done counts
    When a portfolio-forecasts-updated event is handled for that delivery's portfolio
    Then today's snapshot row records the delivery's exact backlog, done, and remaining counts

  @real-io @kpi
  Scenario: Re-handling the same day is idempotent on delivery and date
    Given a delivery with known current backlog and done counts
    And a portfolio-forecasts-updated event has already been handled today for that delivery's portfolio
    When the same portfolio-forecasts-updated event is handled again on the same day
    Then exactly one snapshot row exists for that delivery and date
    And the row holds the latest recomputed counts, overwritten in place

  @real-io
  Scenario: A re-opened item lowers the next recorded done count
    Given a delivery whose recorder ran yesterday with a higher done count
    And an item has since left the done state
    When the recorder runs again today
    Then today's recorded done count is lower than yesterday's
    And the backlog count is unchanged

  @real-io @error
  Scenario: Deleting a delivery cascades away its snapshot rows
    Given a delivery with recorded snapshot rows
    When the delivery is deleted
    Then no snapshot rows remain for that delivery

  @real-io @error @premium
  Scenario: A non-premium instance does not expose the delivery metrics history
    Given a non-premium Lighthouse instance with a delivery
    When the forecaster requests the delivery's metrics history
    Then the metrics history is not available

  @real-io @rbac @driving_adapter
  Scenario: A portfolio viewer can read the delivery metrics history
    Given a delivery with recorded snapshots on a premium instance
    When a portfolio viewer requests the delivery's metrics history
    Then the response succeeds with the recorded series
    And there is no write surface exposed to the viewer
