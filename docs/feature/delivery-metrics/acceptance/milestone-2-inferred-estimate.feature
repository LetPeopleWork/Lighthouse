@US-01b @milestone-2
Feature: Estimated-portion transparency on the delivery burnup
  As a Delivery Forecaster tracking an early-stage delivery
  I want to see how much of the backlog comes from features that are not yet broken down
  So that I can tell how much of the burnup rests on estimates rather than counted work

  # The backlog total already INCLUDES extrapolated work for not-broken-down features
  # (WorkItemService.ExtrapolateNotBrokenDownFeatures runs before forecasting and persists
  # dummy FeatureWork sized from EstimatedSize or the portfolio default). This slice records,
  # forward, how much of each day's backlog total is that estimated portion, and surfaces the
  # broken-down-vs-estimated split. Backend: WebApplicationFactory<Program>, real EF context.
  # [Ignore] until DELIVER.

  @real-io @kpi
  Scenario: A not-yet-broken-down feature's extrapolated items are recorded as the estimated portion
    Given a delivery whose backlog includes a not-broken-down feature extrapolated to a configured size
    When the recorder runs for that delivery's portfolio
    Then today's snapshot records an estimated item count equal to that extrapolated size
    And that estimated item count is no greater than the backlog total (it is the estimated part of that total)

  @real-io
  Scenario: A fully broken-down delivery records no estimated portion
    Given a delivery whose features are all broken down into child work items
    When the recorder runs for that delivery's portfolio
    Then today's snapshot records no estimated item count
    And the whole backlog total is broken-down work

  @real-io @error
  Scenario: Before the estimated portion was recorded the history carries only the backlog total
    Given a delivery whose recorder has only ever recorded backlog totals
    When the forecaster requests the delivery's metrics history
    Then every point carries a backlog total
    And no point carries an estimated item count
