Feature: Over-time widgets respect the dashboard date range (Epic 5427, Slice 03b — US-06)
  As a flow coach — and as a delivery lead on the PBC widget
  I want the metrics dashboard's date pickers to apply to "Percentiles Over Time" and "PBC Over Time"
  So that I read the trend for the period I am actually reviewing instead of all of recorded history

  # Read-path only: nothing about recording, the two snapshot tables or the forward-only contract changes.
  # Driving adapters = the two shipped widgets (UI) and the two shipped GET actions on both controllers,
  # whose request shape widens by two OPTIONAL params (D9 / ADR-108 slice-03b amendment).
  # The empty-state discriminator is the selected range's END, not "narrowed vs default" — the dashboard
  # has no unfiltered state, its default IS a 30-day (team) / 90-day (portfolio) window (DDD-13).

  @real-io @driving_adapter @us-06 @slice-03b
  Scenario: Flow coach narrows the range and the percentile trend follows
    Given Lighthouse is running with the demo data loaded and daily percentile snapshots recorded across the last two weeks
    When the flow coach opens the Predictability category and reads the "Percentiles Over Time" widget on the default range
    And the flow coach narrows the dashboard date pickers to a window inside that recorded period
    Then the widget plots only the recorded days inside the selected window
    And it plots strictly fewer days than it did on the default range

  @real-io @driving_adapter @us-06 @slice-03b
  Scenario: Delivery lead narrows the range and the process-behaviour limits follow
    Given Lighthouse is running with the demo data loaded and daily Throughput limits recorded across the last two weeks
    When the delivery lead opens the "PBC Over Time" widget on the default range
    And the delivery lead narrows the dashboard date pickers to a window inside that recorded period
    Then the upper limit, average and lower limit lines plot only the recorded days inside the selected window
    And they plot strictly fewer days than they did on the default range

  @real-io @driving_port @us-06 @slice-03b
  Scenario Outline: The window is inclusive at both ends
    Given an owner with percentile and process-behaviour snapshots recorded on three consecutive days
    When the series is requested for a window whose <bound> falls exactly on a recorded day
    Then that day is present in the series
    Examples:
      | bound     |
      | startDate |
      | endDate   |

  @real-io @driving_port @us-06 @slice-03b
  Scenario Outline: Either bound may be omitted on its own
    Given an owner with percentile and process-behaviour snapshots recorded on three consecutive days
    When the series is requested with <supplied> only
    Then the series contains every recorded day <direction> that bound
    Examples:
      | supplied  | direction     |
      | startDate | on or after   |
      | endDate   | on or before  |

  @regression @us-06 @slice-03b
  Scenario: Omitting both params reproduces the shipped contract exactly
    Given an owner with percentile and process-behaviour snapshots recorded across several days
    When the series is requested with no startDate and no endDate, as every shipped caller does
    Then the full history comes back, identical to the response before this slice
    And no shipped acceptance test needed a URL change

  @error @us-06 @slice-03b
  Scenario: An inverted window is rejected rather than answered emptily
    Given an owner with percentile and process-behaviour snapshots recorded across several days
    When the series is requested with a startDate later than the endDate
    Then the request is rejected with 400 and the controllers' existing start-before-end message
    And no empty series is returned that the widget could mislabel as honest in-range emptiness

  @real-io @driving_adapter @us-06 @slice-03b
  Scenario: Changing the range refetches instead of replaying a cached series
    Given a widget that has already loaded a series for one date range
    When the selected range changes
    Then the widget requests the series again for the new range
    And it never renders the previously cached series against the new range

  @edge @us-06 @slice-03b
  Scenario: An empty series in a past window says so, rather than blaming forward-only recording
    Given an owner that does have recorded snapshots
    When the selected range ends before today and contains none of them
    Then both widgets read "no data recorded in the selected range"
    And neither widget claims that no snapshots have been recorded yet

  @edge @us-06 @slice-03b
  Scenario: An empty series in a window that includes today keeps the forward-only copy
    Given an owner with no recorded snapshots at all
    When the widgets load on the dashboard's default range, which ends today
    Then both widgets read the unchanged forward-only empty state, verbatim
    And the two shipped empty-state assertions still pass without modification

  # Filtering must stay server-side: RepositoryBase.GetAllByPredicate returns IQueryable<T>, so the date
  # bounds compose into the same SQL as the owner/type predicate. No path materialises the unfiltered
  # series and filters in memory. Asserted at the repository level rather than through the HTTP surface,
  # because the HTTP response is identical either way.
  @real-io @adapter-integration @us-06 @slice-03b
  Scenario: The date bounds are applied by the database, not in memory
    Given an owner with snapshots recorded inside and outside a window
    When the repository serves the series for that window
    Then only the in-window rows are materialised
