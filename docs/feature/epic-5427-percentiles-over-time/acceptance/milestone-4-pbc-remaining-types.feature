Feature: PBC over time — remaining metric-type toggles (Epic 5427, Slice 04 — US-05)
  As a delivery lead
  I want the PBC-over-time widget to toggle across all process-behaviour metric types
  So that I can read limit-stability for whichever behaviour I'm reviewing

  # Driving adapter = the "PBC Over Time" widget's metric-type toggle, extended to the full set.
  # Each type reads its own recorded daily limit series. Feature Size stays portfolio-only.

  @real-io @driving_adapter @us-05 @slice-04
  Scenario Outline: Delivery lead reads dated process-behaviour limits for each metric type
    Given Lighthouse is running with the demo data loaded and daily <metric_type> limits recorded
    When the delivery lead opens the "PBC Over Time" widget and selects "<metric_type>"
    Then three dated lines — upper limit, average, lower limit — are plotted for <metric_type>

    Examples:
      | metric_type   |
      | Throughput    |
      | Work Item Age |
      | Work In Progress |
      | Cycle Time    |
      | Arrivals      |

  @real-io @driving_adapter @us-05 @slice-04
  Scenario: Feature Size is offered only in portfolio scope
    Given Lighthouse is running with the demo data loaded
    When the delivery lead opens the "PBC Over Time" widget on a portfolio
    Then "Feature Size" is offered in the metric-type toggle
    When the delivery lead opens the "PBC Over Time" widget on a team
    Then "Feature Size" is not offered

  @regression @us-05 @slice-04
  Scenario: Adding the remaining types does not change the Throughput behaviour
    Given the PBC Over Time widget with all metric types available
    When the delivery lead selects "Throughput"
    Then the Throughput limits behave exactly as they did when Throughput was the only type
