Feature: Cycle-time percentiles over time (walking skeleton — Epic 5427, Slice 01)
  As a flow coach
  I want to see my team's cycle-time percentiles plotted day by day for a chosen lookback horizon
  So that I can tell whether cycle-time predictability is tightening or drifting

  # Walking skeleton: proves the full backbone in one thin slice —
  # record daily snapshot -> serve over-time series -> render over-time chart -> read the trend.
  # Driving adapter = the "Percentiles Over Time" widget (UI) through the production composition root.
  # Demo data (DemoPercentilesBackfillHandler) backdates snapshots so the chart is populated.

  @walking_skeleton @real-io @driving_adapter @us-01 @slice-01
  Scenario: Flow coach opens the Percentiles Over Time widget and reads a dated CT-30 trend
    Given Lighthouse is running with the "When Will This Be Done?" demo data loaded
    And the demo team's daily cycle-time percentiles have been recorded over the demo window
    When the flow coach opens the team's Metrics view and switches to the Predictability category
    And the flow coach opens the "Percentiles Over Time" widget
    Then the widget shows a toggle row with a "CT-30" option selected by default
    And four dated percentile lines (50th, 70th, 85th, 95th) are plotted across the date range
    And the lines use the red-to-green percentile colouring
    When the flow coach switches the toggle from "CT-30" to "CT-60" and then "CT-90"
    Then each horizon re-plots its own dated percentile lines from already-recorded history
    And no new day-by-day recomputation of past days is triggered by the toggle
