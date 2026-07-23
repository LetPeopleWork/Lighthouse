Feature: Throughput process-behaviour limits over time (Epic 5427, Slice 03 — US-04)
  As a delivery lead
  I want a chart of the Throughput process-behaviour limits plotted day by day
  So that I can see whether the natural process limits are shifting — a real process change vs a one-off signal

  # Driving adapter = a SEPARATE "PBC Over Time" widget with a metric-type toggle (Throughput active here).
  # Reuses the US-02 recording pipeline for a second snapshot family (upper limit / average / lower limit).

  @real-io @driving_adapter @us-04 @slice-03
  Scenario: Delivery lead reads dated Throughput process-behaviour limits
    Given Lighthouse is running with the demo data loaded and daily Throughput limits recorded
    When the delivery lead opens the Predictability category and opens the "PBC Over Time" widget
    Then the metric-type toggle shows "Throughput" selected
    And three dated lines — upper limit, average, lower limit — are plotted across the date range
    And the limit styling matches the existing point-in-time process-behaviour chart

  @real-io @driving_port @us-04 @slice-03
  Scenario: Throughput limits are recorded by the shared daily pipeline
    Given a team whose metrics are refreshed today
    When the metrics refresh completes
    Then today's Throughput upper/average/lower limits are recorded by the same pipeline that records the percentile families

  @edge @us-04 @slice-03
  Scenario: A fresh team's PBC Over Time widget shows the honest empty state
    Given a team with no recorded process-behaviour limits yet
    When the delivery lead opens the "PBC Over Time" widget
    Then it shows the forward-only empty state, never a broken chart
