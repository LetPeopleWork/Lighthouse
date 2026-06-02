@US-04 @milestone-4
Feature: Likelihood and when-distribution over time — predictability trend
  As a Delivery Forecaster
  I want the delivery's likelihood and forecast-completion-date spread plotted over time
  So that I can tell whether our predictability is firming up or slipping, a different question from "are we on track today?"

  # Backend acceptance — WebApplicationFactory<Program>, real EF context. The likelihood series is
  # RAG-banded by the existing thresholds; the when view returns completion-date percentiles
  # (default 70%, with the 50/70/85/95 spread) against the target date. [Ignore] until DELIVER.

  @real-io @driving_adapter
  Scenario: The metrics-history endpoint returns the likelihood-over-time series
    Given a delivery with snapshots carrying a recorded likelihood per day
    When the forecaster requests the delivery's metrics history
    Then each point carries the recorded likelihood for that day

  @real-io @driving_adapter
  Scenario: The when view returns forecast completion-date percentiles against the target date
    Given a delivery with snapshots carrying a recorded when-distribution per day
    When the forecaster requests the delivery's metrics history
    Then each point carries the recorded completion-date percentiles
    And the delivery target date is returned as the reference for the when view

  @real-io
  Scenario: An improving delivery reads differently from a degrading one
    Given an improving delivery whose likelihood rises and whose completion-date spread narrows over time
    And a degrading delivery whose completion-date spread widens over time
    When the forecaster requests each delivery's metrics history
    Then the improving delivery's spread narrows toward the target date across recorded days
    And the degrading delivery's spread widens across recorded days

  @real-io @error
  Scenario: A delivery with sparse or no predictability data renders consistently with the forecast view
    Given a delivery whose snapshots carry no likelihood or when-distribution yet
    When the forecaster requests the delivery's metrics history
    Then no point carries a likelihood
    And no point carries a when-distribution
