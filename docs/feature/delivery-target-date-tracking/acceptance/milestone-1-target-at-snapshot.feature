@US-01 @milestone-1
Feature: Record the target as of each snapshot and step it on the When? view
  As a Delivery Forecaster
  I want each recorded day to remember the delivery's target as it stood that day
  So that the predictability When? view contrasts the forecast against the target that actually applied

  # Backend acceptance — WebApplicationFactory<Program>, real EF context (extends
  # DeliveryMetricsHistoryReadApiIntegrationTest + DeliveryMetricSnapshotRecordingHandlerTest).
  # Frontend behaviour — Vitest + RTL on DeliveryPredictabilityChart + the pure
  # deliveryTargetHistory helper. [Ignore] / test.fixme until DELIVER.

  @real-io @kpi
  Scenario: The recorder remembers the delivery's target as of the recording day
    Given a delivery whose target date is set
    When the daily recorder records a snapshot for the delivery
    Then the snapshot's recorded target equals the delivery's target date at that moment

  @real-io @kpi
  Scenario: Re-recording the same day keeps a single snapshot with the current target
    Given a delivery that already recorded a snapshot today
    When the recorder runs again the same day after the target date was moved
    Then the delivery still has exactly one snapshot for today
    And that snapshot's recorded target is the moved target date

  @real-io @driving_adapter
  Scenario: The metrics history returns the recorded target per day
    Given a delivery whose snapshots recorded different target dates across days
    When the forecaster requests the delivery's metrics history
    Then each point carries the target date recorded for that day

  @real-io @driving_adapter @error
  Scenario: History recorded before the target was captured returns no per-day target
    Given a delivery whose existing snapshots predate target-date capture
    When the forecaster requests the delivery's metrics history
    Then no point carries a recorded target date
    And the response still carries the delivery's current target date

  @fe-component @in-memory
  Scenario: The When? view steps the target where it was moved
    Given a metrics history whose recorded target holds at an earlier date then changes to a later date
    When the forecaster views the "When?" predictability view
    Then the target reference is a stepped line that holds at the earlier target over the days it applied and steps to the later target on the change day

  @fe-component @in-memory @error
  Scenario: The When? view falls back to one flat target line when no per-day target was recorded
    Given a metrics history whose recorded target is absent on every point
    When the forecaster views the "When?" predictability view
    Then the target is drawn as a single flat reference line at the delivery's current target date

  @fe-component @in-memory
  Scenario: The When? target line is one flat level when the target never moved
    Given a metrics history whose recorded target is the same on every point
    When the forecaster views the "When?" predictability view
    Then the stepped target line shows a single flat level with no spurious steps

  @fe-component @in-memory
  Scenario: The burnup no longer carries a delivery-date marker
    Given a delivery burnup is rendered
    When the forecaster views the burnup
    Then no delivery-date reference marker is shown on the burnup
