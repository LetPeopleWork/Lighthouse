Feature: One forecast per Portfolio per refresh batch (Epic 5792, Slice 00 — US-10, US-11)
  As a delivery forecaster
  I want a refresh to produce one date rather than a sequence of them
  So that I can read the number the moment it appears instead of waiting to see whether it settles

  # Two paths ask for a Portfolio forecast today and neither can see the other, so a batch produces
  # two or three forecasts — and because the simulation draws from an unseeded source, each returns
  # a different date over identical data. This slice removes the redundant runs. It changes nothing
  # about what a forecast computes.
  #
  # The rule this slice adds waits for sibling work that is *queued*, and deliberately not for work
  # already running: a Team announces its refreshed delivery while its own refresh is still finishing
  # off, so a rule that also waited on running work would wait on the announcement's own execution
  # and nothing would ever be forecast.
  #
  # The Background holds only what every scenario shares. Scenarios that need a different shape —
  # one Team, nothing in flight, a shared Team — say so themselves.
  #
  # The word this product reserves for work held up right now appears nowhere in this file.

  Background:
    Given a Portfolio whose Features are worked by three Teams

  @driving_port @us-10 @slice-00 @contract-shape:bounded-change
  Scenario: Refreshing everything produces one forecast for the Portfolio, not one per Team
    Given every one of those Teams is due a refresh
    When everything is refreshed in one batch
    Then the Portfolio is forecast exactly once
    And the forecast that ran is the one that saw all three Teams' refreshed delivery

  @driving_port @us-10 @slice-00 @contract-shape:bounded-change
  Scenario: A Portfolio refresh and a Team refresh overlapping in time produce one forecast
    Given the Portfolio's own refresh and one of its Teams' refreshes are asked for at the same moment
    When both are carried out
    Then the Portfolio is forecast exactly once
    And neither refresh forecast the Portfolio privately, out of sight of the other

  @regression @driving_port @us-10 @slice-00 @kpi @contract-shape:unbounded-preservation
  Scenario: Moving the forecast out of the Portfolio refresh costs the work tracking system nothing
    Given the number of times refreshing this Portfolio writes back to its work tracking system,
      recorded on the released product and kept as the number to beat
    When the Portfolio is refreshed after the change
    Then the work tracking system is written to exactly as many times as the recorded number
    And every value that was written before is written again, with the same content

  # The failure the split flush creates, which the happy-path count above cannot see.
  @error @driving_port @us-10 @slice-00 @contract-shape:bounded-change
  Scenario: A forecast write-back that fails leaves nothing half-written
    Given the Portfolio writes both Feature values and forecast values back to its work tracking system
    When the Feature values are written successfully and the forecast values fail to write
    Then no value is written twice
    And the next refresh writes the forecast values that did not land
    And the failure is reported rather than swallowed

  @driving_port @us-11 @slice-00 @contract-shape:bounded-change
  Scenario: A forecast asked for while a sibling Team is still waiting to refresh is not run yet
    Given one Team has finished refreshing while the other two are still waiting their turn
    When that Team asks for the Portfolio to be forecast
    Then no forecast runs yet
    And the forecast that eventually runs reflects all three Teams' refreshed delivery

  @edge @driving_port @us-11 @slice-00 @contract-shape:bounded-change
  Scenario: A Portfolio with one Team is forecast immediately, with nothing to wait for
    Given a Portfolio worked by a single Team, with no other work of its own in flight
    When that Team finishes refreshing
    Then the Portfolio is forecast without waiting for anything

  # A Team and a Portfolio are related two ways: a stored pairing, and the Features the Team actually
  # works. Only the second is kept current, so a Portfolio related only that way was never forecast
  # when its Team refreshed - silently, and for as long as the pairing stayed missing.
  @error @driving_port @us-11 @slice-00 @contract-shape:bounded-change
  Scenario: A Portfolio a Team reaches only through the Features it works is forecast too
    Given a Portfolio with no stored pairing to the Team that works its Features
    When that Team finishes refreshing
    Then the Portfolio is forecast
    And a Portfolio whose Features that Team does not work is left alone

  @error @driving_port @us-11 @slice-00 @contract-shape:bounded-change
  Scenario: The last Team failing to refresh still releases the forecast its siblings are owed
    Given two of the three Teams have refreshed successfully
    And a forecast has been asked for while the third was still waiting its turn
    When the third Team's refresh fails
    Then the Portfolio is still forecast exactly once
    And the forecast reflects the two Teams whose delivery did refresh
    And the Portfolio does not sit without a forecast until the next scheduled refresh

  @error @driving_port @us-11 @slice-00 @contract-shape:pure-function
  Scenario: A Team's own refresh is never mistaken for something it has to wait for
    Given a Team announces its refreshed delivery while its own refresh is still finishing off
    When that announcement asks for the Portfolio to be forecast
    Then the ask is not held back by the announcing Team's own work
    And the Portfolio is forecast

  @error @driving_port @us-11 @slice-00 @contract-shape:pure-function
  Scenario: Work elsewhere in the instance never delays this Portfolio's forecast
    Given an unrelated Team that shares no Portfolio with this one is refreshing
    And every Team of this Portfolio has finished
    When this Portfolio's forecast is asked for
    Then it runs without waiting for the unrelated Team

  # A Team worked into two Portfolios is where "which siblings does this Portfolio wait for" stops
  # being obvious, and where one Portfolio can end up waiting on work that cannot change its answer.
  @error @driving_port @us-11 @slice-00 @contract-shape:bounded-change
  Scenario: A Team belonging to two Portfolios forecasts both, and neither waits on the other's work
    Given a Team that works Features in this Portfolio and in a second Portfolio
    And the second Portfolio has another Team of its own still waiting its turn
    When the shared Team finishes refreshing
    Then this Portfolio is forecast exactly once, without waiting for the second Portfolio's other Team
    And the second Portfolio is forecast exactly once, once its own Teams have finished

  # A person pressing refresh is not a member of a refresh batch, and must never be told "done"
  # while nothing happens.
  @error @driving_adapter @us-11 @slice-00 @contract-shape:bounded-change
  Scenario: A forecaster who asks for a forecast is never told it happened when it did not
    Given a Team of this Portfolio is still waiting its turn to refresh
    When a forecaster asks for this Portfolio's forecast by hand
    Then either the forecast runs, or the forecaster is told it is waiting and what it is waiting for
    And the forecaster is never shown a completed refresh that did not run

  @real-io @adapter-integration @us-11 @slice-00 @contract-shape:pure-function
  Scenario: The same answer is given where the record of work in flight is kept outside the application
    Given an instance that keeps its record of work in flight in a shared store
    And two of that Portfolio's Teams are still waiting their turn
    When the Portfolio's forecast is asked for
    Then it is held back exactly as it would be on an instance keeping that record to itself
    And the shared store is asked once, not once per Team

  # What this slice waits for is a refresh batch, not every reason a forecast could be wanted.
  @edge @driving_port @slice-00 @contract-shape:bounded-change
  Scenario: A change to the order of Features still forecasts straight away
    Given nothing in this Portfolio is refreshing
    When someone changes the order of the Features in it
    Then the Portfolio is forecast without waiting for anything

  # Someone reordering Features is asking for an answer now, exactly as pressing refresh is. The
  # scenario above only proves the quiet case; this one proves the intent while a batch is running.
  @edge @driving_port @slice-00 @contract-shape:bounded-change
  Scenario: A change to the order of Features forecasts straight away even mid-refresh
    Given the Teams working this Portfolio are still refreshing
    When someone changes the order of the Features in it
    Then the Portfolio is forecast without waiting for the batch to finish

  @driving_adapter @us-10 @slice-00 @contract-shape:bounded-change
  Scenario: The date a forecaster is reading stops changing seconds after it appears
    Given a forecaster is watching this Portfolio's Features while everything is refreshed
    When the batch completes
    Then the dates settle once
    And no date on screen changes again without something else changing first

  @regression @driving_port @slice-00 @contract-shape:unbounded-preservation
  Scenario: Everything else the Portfolio refresh does is left exactly where it was
    Given the Portfolio's Features, its delivery rules and its recorded delivery metrics before a refresh
    When the Portfolio is refreshed after the change
    Then its Features are refreshed as before
    And its rule-based deliveries are recomputed as before
    And its delivery metric snapshot for the day is recorded once and holds what it held before
