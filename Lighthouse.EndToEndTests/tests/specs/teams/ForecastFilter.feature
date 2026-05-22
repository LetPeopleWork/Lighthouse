# Forecast Filter scenarios — Gherkin documentation form
# Feature: filter-forecast-throughput (Epic 4896, customer ask Liz / JLP)
# Executable form: ./ForecastFilter.spec.ts (Playwright). Same scenario titles.
# Wave: DISTILL
# Date: 2026-05-22
#
# Walking Skeleton Strategy: B — Real local + faked WTS.
#   - Real WebApplicationFactory backend, real Sqlite, real Vitest.
#   - Work-tracking system connector (Jira / ADO / Linear) faked via the existing
#     stub pattern (Lighthouse.Backend.Tests/Services/Implementation/WorkTrackingConnectors/).
#   - ONE Playwright @walking_skeleton @premium scenario covers the end-to-end user
#     journey from team-admin configures rule → flips every toggle in the system.
#
# Scenarios that test implementation invariants (not user-observable behaviour) have
# been routed to backend NUnit integration / frontend Vitest layers per the
# Lighthouse CI minimalism rule. Migrated scenarios and their new homes:
#
#   - "Premium team admin saves a forecast-filter rule set" (US-01)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterTeamSettingsIntegrationTest.cs
#   - "Non-premium team admin sees the upgrade teaser instead of the rule editor" (US-07)
#       → Lighthouse.Frontend/src/pages/Teams/Edit/ForecastSettingsComponent.test.tsx
#         + Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterTeamSettingsIntegrationTest.cs (license read-path no-op)
#   - "Viewer sees the rule editor read-only" (US-01)
#       → Lighthouse.Frontend/src/components/Teams/ForecastFilterEditor/ForecastFilterEditor.test.tsx
#         + Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterTeamSettingsIntegrationTest.cs (PUT returns 403 for non-TeamAdmin)
#   - "Unknown field key is rejected on save" (US-01)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterTeamSettingsIntegrationTest.cs
#   - "License downgrade preserves the persisted rule set but suppresses the filter" (US-07 / invariant #7)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterRuleServiceIntegrationTest.cs
#   - "Feature forecast applies the team's filter automatically (no toggle)" (US-02 / invariant #2)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterFeatureForecastIntegrationTest.cs
#   - "Multi-team feature forecast applies each team's own filter independently" (invariant #3)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterFeatureForecastIntegrationTest.cs
#   - "Empty filter is a no-op identical to today's behaviour" (invariant #4)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterRuleServiceIntegrationTest.cs
#   - "Rule set that excludes all throughput falls back with a warning on forecast surfaces" (US-02 / D5)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterFeatureForecastIntegrationTest.cs
#   - "Team forecast per-run override returns unfiltered when applyFilterOverride is false" (US-04)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterTeamForecastIntegrationTest.cs
#         + Lighthouse.Frontend/src/components/Teams/TeamForecastForm/TeamForecastForm.test.tsx (toggle visibility + default)
#   - "Throughput PBC chart serves filtered counts on ?view=filtered" (US-05)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterThroughputChartIntegrationTest.cs
#   - "Throughput Run Chart toggles client-side without a backend round-trip" (US-05 / DDD-5)
#       → Lighthouse.Frontend/src/components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle.test.tsx
#   - "Throughput chart Filtered view with zero matches renders empty-state" (US-05 / D5 chart half)
#       → Lighthouse.Frontend/src/components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle.test.tsx
#   - "Backtest per-run override returns unfiltered when applyFilterOverride is false" (US-06)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ForecastFilterBacktestIntegrationTest.cs
#         + Lighthouse.Frontend/src/components/Teams/BacktestForm/BacktestForm.test.tsx (toggle visibility + default)
#   - "Filtered throughput chip shows the rule list in its tooltip" (US-03)
#       → Lighthouse.Frontend/src/components/Common/Forecasting/FilteredThroughputChip.test.tsx
#   - "Rule editor lives inside the existing Forecast Configuration InputGroup on the team edit page" (US-01 / row-359 design correction)
#       → Lighthouse.Frontend/src/pages/Teams/Edit/ForecastSettingsComponent.test.tsx
#   - "DeliveryRuleSet JSON shape is reused verbatim across both rule-engine consumers" (invariant #6 / DDD-7 canary)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/Models/DeliveryRules/RuleEngineReuseCanaryTests.cs
#   - "Forecast is deterministic given identical team state + rule set + seed" (invariant #8)
#       → Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Forecast/ForecastFilterFeatureForecastIntegrationTest.cs
#
# Tag legend:
#   @walking_skeleton    — the one end-to-end-thin-slice that proves the full pipeline (browser → API → DB)
#   @premium             — requires a premium license to be active in the test fixture
#   @driving_adapter     — exercises the HTTP entry point via real network call (not a service-function invocation)
#   @real-io             — uses real DB, real ITeamMetricsService, real DeliveryRuleSet persistence
#   @US-N                — traceability to feature-delta.md user story N
#   @kpi-OUT-N           — verifies the named outcome (links to docs/product/kpi-contracts.yaml)


Feature: Forecast-throughput filter (Premium) configures rules and propagates across forecast surfaces
  As a delivery-forecaster (also team-admin on the team)
  I want to define rules that exclude noise from my team's forecast throughput
  So that feature-forecast percentile dates honestly reflect feature-bearing pace
  And I can switch between filtered and raw views on charts, team forecasts, and backtests on demand


  Background:
    Given a premium-licensed Lighthouse instance
    And a faked work-tracking connector returning a mixed closed history of User Stories and Bugs
    And a team-admin user is signed in


  @walking_skeleton @premium @driving_adapter @real-io @US-01 @US-02 @US-03 @US-04 @US-05 @US-06 @kpi-OUT-filter-adoption
  Scenario: Premium delivery-forecaster configures the filter and propagates it across every forecast surface
    Given a team with a mixed history of forty closed user stories and twenty closed bugs in the throughput window
    When the team-admin opens the team's edit page
    And inside the Forecast Configuration section adds an exclusion rule "Type equals Bug"
    And saves the team
    Then the rule set persists across refresh
    When the team-admin views a feature forecast involving this team
    Then the percentile dates are computed from forty-item throughput (bugs excluded)
    And a Filtered throughput chip is visible next to the percentile dates
    And the chip tooltip enumerates "Type = Bug"
    When the team-admin opens the team's throughput chart
    Then the chart shows total throughput by default and a Show Raw or Filtered toggle is visible
    When the team-admin flips the throughput chart toggle to Filtered
    Then the chart shows only the forty non-bug closes and a Filtered throughput chip appears
    When the team-admin opens the Team Forecast How Many page
    Then the Apply forecast-throughput filter toggle is visible and defaulted on
    When the team-admin turns the toggle off and runs the forecast
    Then the team forecast result reflects unfiltered throughput and the chip is absent
    When the team-admin opens the Backtest page and runs a backtest with the filter toggle defaulted on
    Then the backtest result reflects filtered throughput and a Filtered throughput chip appears on the result


  # Slice 01 — Rule-engine generalisation + Feature Forecast (US-01, US-02, US-03 chip, US-07).
  # Re-layered (2026-05-22): the implementation-invariant scenarios below are exercised by
  # backend NUnit integration tests + frontend Vitest tests. See the migration list in this
  # file's header. The user-observable Slice 01 coverage is captured by the @walking_skeleton
  # scenario above.


  # Slice 02 — Team Forecast per-run toggle (US-04, US-03 chip extension).
  # All scenarios re-layered to ForecastFilterTeamForecastIntegrationTest.cs (backend)
  # plus TeamForecastForm.test.tsx (frontend). The toggle's user-observable round-trip is
  # exercised by the @walking_skeleton scenario above.


  # Slice 03 — Throughput chart toggle (US-05, US-03 chip extension).
  # All scenarios re-layered to ForecastFilterThroughputChartIntegrationTest.cs (backend, PBC ?view=)
  # plus ThroughputChartFilterToggle.test.tsx (frontend, Run Chart client-side filter and empty-state).
  # The user-observable Raw → Filtered flip is exercised by the @walking_skeleton scenario above.


  # Slice 04 — Backtest per-run toggle (US-06, US-03 chip extension).
  # All scenarios re-layered to ForecastFilterBacktestIntegrationTest.cs (backend) plus
  # BacktestForm.test.tsx (frontend). The user-observable round-trip is exercised by the
  # @walking_skeleton scenario above.
