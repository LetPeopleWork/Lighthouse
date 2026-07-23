Feature: Forward-only daily snapshot recording (Epic 5427, Slice 01 — US-02, @infrastructure)
  As the system
  On each metrics refresh I record the fresh cycle-time percentile numbers for today, keeping only
  the latest write per calendar day, so the over-time chart has one honest point per day going forward

  # Driving port = the metrics-refresh domain event (TeamDataRefreshed / PortfolioFeaturesRefreshed).
  # Observable outcome = the recorded daily snapshot rows and the refresh staying healthy.
  # These run as backend integration/handler tests (real EF, in-memory + Postgres container for migration).

  @real-io @driving_port @us-02 @slice-01
  Scenario: A metrics refresh records today's cycle-time percentiles for every horizon
    Given a team whose metrics are refreshed today
    When the metrics refresh completes
    Then exactly one set of cycle-time percentile numbers is recorded for today for the 30, 60 and 90 day horizons

  @real-io @driving_port @us-02 @slice-01
  Scenario: Re-running the refresh the same day overwrites rather than duplicating
    Given a team whose metrics were already refreshed earlier today
    When the metrics refresh runs again the same day
    Then there is still exactly one recorded set of numbers per horizon for today, carrying the latest values

  @edge @us-02 @slice-01
  Scenario: Recording is forward-only — no historical days are fabricated
    Given a team that has never had its percentiles recorded before
    When the first metrics refresh completes today
    Then only today is recorded, and no earlier days are invented

  @error @us-02 @slice-01
  Scenario: A failure while recording does not break the refresh and is observable
    Given a team whose percentile recording will fail during a refresh
    When the metrics refresh runs
    Then the refresh still completes successfully
    And a recording-failed event is written to the logs for the operator to see

  @real-io @adapter-integration @us-02 @slice-01
  Scenario: The new snapshot storage is created additively on both database providers
    Given a Lighthouse instance being upgraded
    When the database schema is brought up to date
    Then the two new daily-snapshot stores are added without changing or removing any existing data
    And this holds on both the SQLite and the PostgreSQL provider
