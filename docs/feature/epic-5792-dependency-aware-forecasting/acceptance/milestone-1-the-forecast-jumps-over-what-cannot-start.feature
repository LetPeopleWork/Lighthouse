Feature: The forecast jumps over a Feature that cannot start yet (Epic 5792, Slice 01 — US-05, US-06)
  As a delivery forecaster
  I want the forecast to give a waiting Feature's capacity to the ones behind it until the Feature
    it is waiting on is done
  So that the dates reflect the order the work can actually happen in

  # This is the slice that decides whether the epic was worth building. "Jumping over" is not a date
  # shift applied afterwards: the waiting Feature is left out of the eligible set inside each trial,
  # so the Features below it move up and take the capacity it did not use. The second scenario is the
  # one that tells those two designs apart, and it is the one that can disprove the whole epic.
  #
  # Same-Team only in this slice. A dependency that crosses a Team carries a warning saying it was
  # left out — the warning slice 02 exists to delete.
  #
  # Every date here is read off a forecast that really ran, against a pinned starting number. Two
  # unpinned runs over identical data return different percentiles — the draw source is unseeded by
  # deliberate choice (ADR-154) — so any scenario comparing two runs without pinning it would pass on
  # sampling noise alone.
  #
  # Several scenarios assert that a date did NOT move. Each one carries a Feature in the same run
  # whose date DID move, because "correctly left alone" and "the whole mechanic is missing" are
  # otherwise the same observation.
  #
  # The word this product reserves for work held up right now appears nowhere in this file.

  Background:
    Given a Portfolio containing the Features "Checkout redesign", "Payment gateway upgrade" and
      "Address book rewrite", in that order
    And all three are worked by the same Team, which has measured delivery to forecast from

  @driving_port @us-05 @slice-01 @contract-shape:bounded-change
  Scenario: A Feature never finishes before the Feature it is waiting on
    Given the instance is licensed for dependency-aware forecasting
    And "Payment gateway upgrade" is waiting on "Checkout redesign"
    When the Portfolio is forecast
    Then in every simulated run, "Payment gateway upgrade" finishes no earlier than "Checkout redesign"

  # The scenario that distinguishes accounting for a dependency from merely postponing a date. Its
  # baseline is a second forecast of the same build with the dependency simply not recorded, which is
  # why it needs no stored gold number and cannot go stale.
  @driving_port @us-05 @slice-01 @kpi @contract-shape:bounded-change
  Scenario: The capacity the waiting Feature could not use goes to the Feature below it
    Given the instance is licensed for dependency-aware forecasting
    And a forecast of this Portfolio against a pinned starting number with no dependency recorded,
      and the dates it produced for all three Features
    When "Payment gateway upgrade" is recorded as waiting on "Checkout redesign"
    And the Portfolio is forecast again against the same starting number
    Then "Payment gateway upgrade" is later than it was
    And "Address book rewrite" is EARLIER than it was

  @error @driving_adapter @us-05 @slice-01 @contract-shape:pure-function
  Scenario: A dependency on another Team's Feature is left out, and the row says so
    Given the instance is licensed for dependency-aware forecasting
    And "Payment gateway upgrade" is waiting on a Feature worked by a different Team
    When the Portfolio is forecast
    Then no date moves because of that dependency
    And the row for "Payment gateway upgrade" warns that the wait crosses a Team and is not in the forecast
    And the warning uses the instance's own word for a Feature

  @error @driving_port @us-05 @slice-01 @contract-shape:bounded-change
  Scenario: Two Features waiting on each other constrain nothing, and the forecast still finishes
    Given the instance is licensed for dependency-aware forecasting
    And "Checkout redesign" and "Payment gateway upgrade" are waiting on each other
    When the Portfolio is forecast
    Then the forecast completes in the time an unconstrained forecast takes
    And neither Feature held the other back in any simulated run
    And both rows carry the warning that names the other

  @error @driving_port @us-05 @slice-01 @contract-shape:bounded-change
  Scenario: Waiting on a Feature that can never be forecast drops the wait for this run, and says so
    Given the instance is licensed for dependency-aware forecasting
    And "Payment gateway upgrade" is waiting on a Feature whose Team has no measured delivery
    When the Portfolio is forecast
    Then the forecast completes rather than running forever
    And "Payment gateway upgrade" is presented as the earliest it could possibly be, not as a forecast
    And its row names the Feature the wait was dropped for, and why

  @edge @driving_port @us-05 @slice-01 @contract-shape:pure-function
  Scenario: Waiting on something already finished holds nothing up and warns about nothing
    Given the instance is licensed for dependency-aware forecasting
    And "Checkout redesign" has no work remaining
    And "Payment gateway upgrade" is waiting on it
    And "Address book rewrite" is waiting on a Feature that still has work remaining
    When the Portfolio is forecast
    Then "Payment gateway upgrade" is forecast exactly as it would be with no dependency at all
    And its row carries no dependency warning
    And "Address book rewrite" has moved, so the mechanic was running while it left the other alone

  @edge @driving_port @us-05 @slice-01 @contract-shape:unbounded-preservation
  Scenario: Waiting on a Feature in another Portfolio changes no date anywhere
    Given the instance is licensed for dependency-aware forecasting
    And "Payment gateway upgrade" is waiting on a Feature that shares no Portfolio with it
    And "Address book rewrite" is waiting on "Checkout redesign", which shares its Portfolio
    When both Portfolios are forecast
    Then every date in both Portfolios is what it was before the cross-Portfolio dependency existed
    And the row for "Payment gateway upgrade" says why that wait is not in the forecast
    And "Address book rewrite" has moved, so the mechanic was running while it left the other alone

  @edge @driving_port @us-05 @slice-01 @contract-shape:bounded-change
  Scenario: A day on which everything is waiting is simply an idle day
    Given the instance is licensed for dependency-aware forecasting
    And every Feature the Team could work on is waiting on something unfinished
    When the Portfolio is forecast
    Then that day's delivery is not carried over to a later day
    And the run still reaches an end

  @edge @driving_port @us-05 @slice-01 @contract-shape:bounded-change
  Scenario: A Feature worked by several Teams is only finished when all of them are done
    Given the instance is licensed for dependency-aware forecasting
    And "Checkout redesign" is worked by three Teams, each with work remaining
    And "Payment gateway upgrade" is waiting on "Checkout redesign"
    When the Portfolio is forecast
    Then "Payment gateway upgrade" stays out of the running until all three Teams' work on
      "Checkout redesign" is done
    And it is not released when only the first of the three finishes

  @edge @driving_port @us-05 @slice-01 @contract-shape:pure-function
  Scenario: A Feature recorded as waiting on itself waits for nothing
    Given the instance is licensed for dependency-aware forecasting
    And "Checkout redesign" is recorded as waiting on itself
    And "Payment gateway upgrade" is waiting on "Address book rewrite"
    When the Portfolio is forecast
    Then "Checkout redesign" has the dates it would have with no dependency recorded
    And the forecast completes rather than running forever
    And "Payment gateway upgrade" has moved, so the mechanic was running while it left the other alone

  # The first two Then steps are Epic #4365's shipped behaviour, asserted here as a non-regression
  # claim rather than as something this slice builds. Only the third step is new.
  @driving_adapter @us-06 @slice-01 @contract-shape:pure-function
  Scenario: An unlicensed instance is told the dependency exists and is being ignored
    Given the instance is not licensed for dependency-aware forecasting
    And "Payment gateway upgrade" is waiting on "Checkout redesign"
    When a forecaster opens the Features view
    Then the row shows how many Features it is waiting on
    And the list of what it is waiting on can still be opened
    And the row warns that dependencies are not included in forecasts without a premium licence

  # The second Then step is what stops this scenario passing vacuously. Until the licence answer is
  # actually handed to the one decision, every instance behaves as unlicensed and the first step is
  # true of a product that does nothing at all.
  @driving_port @us-06 @slice-01 @kpi @contract-shape:unbounded-preservation
  Scenario: An unlicensed instance's dates are exactly a forecast that never saw the dependency
    Given the instance is not licensed for dependency-aware forecasting
    And "Payment gateway upgrade" is waiting on "Checkout redesign"
    When the Portfolio is forecast against a pinned starting number
    Then every percentile for every Feature is identical to the same forecast run with no dependency recorded
    And the same forecast on a licensed instance, against the same starting number, is NOT identical

  @driving_port @us-06 @slice-01 @contract-shape:bounded-change
  Scenario: Licensing the instance is the only thing that has to change for the dates to move
    Given an unlicensed instance whose dates have been recorded against a pinned starting number
    And "Payment gateway upgrade" is waiting on "Checkout redesign"
    When the instance is licensed and the Portfolio is forecast against the same starting number
    Then at least one date has moved
    And nothing else about any Feature has changed

  @edge @terminology @driving_adapter @us-06 @slice-01 @contract-shape:pure-function
  Scenario: The hint says what is withheld and why, in the instance's own words
    Given the instance is not licensed, and has renamed what it calls a Feature and a Portfolio
    When a forecaster reads the warning on a Feature that is waiting on another
    Then it names the thing being waited on
    And it says the wait is not accounted for in the dates
    And it says a premium licence is what accounts for it
    And it does not use the word this instance reserves for work held up right now

  # Asserted the way the repository already asserts its other seams — as an architecture rule over the
  # compiled types, not as behaviour. There is no runtime observation that can say "and nowhere else".
  @architecture @kpi @slice-01 @contract-shape:unbounded-preservation
  Scenario: Exactly one place in the product decides whether a dependency counts
    Given the forecast now acts on dependencies
    When the code is inspected for anything that decides whether a dependency can be acted on
    Then there is exactly one such decision
    And nothing inside the forecast reads the order of Features, the licence, or which Features are
      in a loop for itself
