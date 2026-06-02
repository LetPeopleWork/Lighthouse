@US-05 @milestone-5 @stretch
Feature: Fever chart — buffer-vs-schedule at-a-glance signal (STRETCH)
  As a Delivery Forecaster or Delivery Lead in a leadership review
  I want a fever chart plotting buffer consumed against schedule consumed over the recorded snapshots
  So that leadership gets a single at-a-glance "how worried should we be?" signal per delivery

  # STRETCH — out of committed MVP (D9). Ships only on explicit greenlight after Slice 4 and
  # requires a pre-slice SPIKE. Every scenario stays test.fixme / [Ignore] and is NOT scheduled
  # for the MVP DELIVER run.

  @real-io @stretch
  Scenario: A bubble trails through green, amber, and red zones over the recorded snapshots
    Given a delivery with accumulated snapshots
    When the forecaster opens the fever chart
    Then a bubble plots buffer consumed against schedule consumed
    And the bubble trails across snapshot dates through the risk zones

  @real-io @stretch
  Scenario: An at-risk delivery's trail enters the red zone
    Given an at-risk delivery whose buffer is consumed ahead of schedule
    When the forecaster opens the fever chart
    Then the trail enters the red zone

  @real-io @stretch @error
  Scenario: A delivery with no or sparse snapshots shows no bubble
    Given a delivery with no recorded snapshots
    When the forecaster opens the fever chart
    Then no bubble renders
    And an empty-state message is shown
