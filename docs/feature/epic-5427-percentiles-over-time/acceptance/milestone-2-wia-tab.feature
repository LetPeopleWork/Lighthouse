Feature: Work-item-age percentiles over time (Epic 5427, Slice 02 — US-03)
  As a flow coach
  I want a Work Item Age tab on the same widget showing age percentiles day by day
  So that I read age and cycle-time predictability trends from one surface

  # Driving adapter = the same "Percentiles Over Time" widget (WIA toggle). Reuses the US-02 recording
  # pipeline — no second bespoke recorder. WIA has no horizon dimension (age is as-of-today).

  @real-io @driving_adapter @us-03 @slice-02
  Scenario: Flow coach reads a dated work-item-age percentile trend from the WIA tab
    Given Lighthouse is running with the demo data loaded and daily age percentiles recorded
    When the flow coach opens the "Percentiles Over Time" widget
    Then the toggle row now offers "WIA" alongside "CT-30", "CT-60" and "CT-90"
    When the flow coach selects the "WIA" toggle
    Then four dated age-percentile lines (50th, 70th, 85th, 95th) are plotted, with no horizon choice
    And the empty-state and red-to-green colouring behave exactly as the cycle-time tabs do

  @real-io @driving_port @us-03 @slice-02
  Scenario: Age percentiles are recorded by the same daily pipeline, not a second one
    Given a team whose metrics are refreshed today
    When the metrics refresh completes
    Then today's work-item-age percentiles are recorded by the same recording pipeline that records cycle-time percentiles

  @edge @us-03 @slice-02
  Scenario: A fresh team's WIA tab shows the honest forward-only empty state
    Given a team that has no recorded age percentiles yet
    When the flow coach opens the "Percentiles Over Time" widget and selects "WIA"
    Then the tab shows "builds forward from today — no snapshots recorded yet", never a broken chart
