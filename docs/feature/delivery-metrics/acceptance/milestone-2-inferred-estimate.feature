@US-01b @milestone-2
Feature: Inferred-estimate line for not-yet-broken-down features
  As a Delivery Forecaster tracking an early-stage delivery
  I want features that have not been broken down into items to still contribute an estimated size to the backlog
  So that the burnup does not read artificially low and mislead me about the delivery's true size

  # Backend acceptance — WebApplicationFactory<Program>, real EF context. The recorder writes the
  # estimated-total column forward; the endpoint returns it as an additional series. [Ignore] until DELIVER.

  @real-io @kpi
  Scenario: A not-yet-broken-down feature contributes its estimated size to the inferred-estimate series
    Given a delivery containing a feature with no child work items and a configured estimated size
    When the recorder runs for that delivery's portfolio
    Then today's snapshot records an estimated total above the actual-item backlog
    And the estimated portion reflects the feature's configured size

  @real-io
  Scenario: A fully broken-down delivery records no estimated portion
    Given a delivery whose features are all broken down into child work items
    When the recorder runs for that delivery's portfolio
    Then today's snapshot records no estimated portion
    And the inferred-estimate total equals the actual-item backlog

  @real-io @error
  Scenario: Before any inferred estimate has been recorded only the actual-item series is present
    Given a delivery whose recorder has only ever recorded actual-item counts
    When the forecaster requests the delivery's metrics history
    Then every point carries an actual-item backlog
    And no point carries an estimated total
