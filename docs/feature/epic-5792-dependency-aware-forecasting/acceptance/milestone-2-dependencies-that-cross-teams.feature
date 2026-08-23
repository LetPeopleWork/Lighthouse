Feature: Dependencies that cross Teams count too (Epic 5792, Slice 02 — US-07, US-08)
  As a delivery forecaster
  I want a dependency on another Team's Feature to move my dates
  So that the most common kind of real dependency stops being the one Lighthouse ignores

  # Today each Team is simulated on its own clock, so "has the other Team finished yet?" has no
  # answer inside a run. This slice puts every Team on one clock: one run advances a single day
  # counter and each day every Team draws its own delivery and works on its own Features.
  #
  # Three of this slice's commits ship nothing a user can see, by design — that IS the correctness
  # argument. Two of the three are proved by EXACT equality against the run before them. The first
  # cannot be, and saying so is the point: it replaces the source the numbers come from, so its
  # before and after are not comparable draw for draw. It is proved by agreement of the
  # distributions plus the properties of the new source, and it is the commit that ESTABLISHES the
  # recorded baseline the two after it are held to.
  #
  # "Within Monte Carlo noise" is not used anywhere in this file for the two commits that can be
  # exact. A statistical assertion cannot tell "the restructure is correct" apart from "the
  # restructure is wrong by less than the noise floor".
  #
  # Every recorded baseline named below is a committed artifact with a provenance — captured at a
  # named commit, reviewed, and checked in — never a number computed from the build under test.

  Background:
    Given a Portfolio on an instance licensed for dependency-aware forecasting
    And Team X works "Payment gateway upgrade" and Team Y works "Checkout redesign"
    And both Teams have measured delivery to forecast from

  # The ordinal is what separates the day's delivery draw from the draws that pick which Feature is
  # worked on. If those two ever share a coordinate, a high-delivery day would correlate with which
  # Feature received the work, and nothing about the output would look wrong.
  @property @us-07 @slice-02 @contract-shape:pure-function
  Scenario: A draw is decided by where it sits, never by how many draws came before it
    Given the same starting number for a forecast
    When the same run, Team, day and position are asked for twice, in any order, from anywhere
    Then the same number comes back both times
    And asking for other draws in between changes nothing about it
    And two draws at the same position asked for over different ranges are independent of each other
    And within one Team's day, the delivery draw and each Feature pick hold distinct positions

  # The one commit in this epic with no exact net, named here so nobody mistakes its absence for an
  # oversight. The released product draws from an unseeded source, so there is no before-run to
  # match draw for draw.
  # DONE. Asserted by TheDrawSourceChangedTheDistributionDidNotTest, which reproduces the released
  # product's draw source exactly - a fresh random number per draw, blind to where the draw sat - runs
  # the benchmark Portfolio through it five times to see its own spread, and holds the new source's
  # dates to that spread widened by itself, never by less than a day. The recorded baseline it writes is
  # slice-02-shared-clock-percentiles.json.
  @us-07 @slice-02 @kpi @contract-shape:bounded-change
  Scenario: Changing where the numbers come from leaves the distribution where it was
    Given the percentiles the released product produces for a fixture, over enough runs to see past
      the spread between two of its runs
    When the same fixture is forecast from the new draw source
    Then each percentile agrees with the released product's within that spread
    And the numbers drawn are uniform over the range asked for
    And the percentiles this run produced are recorded and checked in as the baseline the rest of
      this slice is held to

  @regression @us-07 @slice-02 @kpi @contract-shape:unbounded-preservation
  Scenario: Putting every Team on one clock moves no date at all
    Given no Feature in the Portfolio is waiting on another
    And the checked-in baseline percentiles recorded when the draw source changed
    When the Portfolio is forecast on the shared clock against the same starting number
    Then every percentile for every Feature is identical to the baseline, not merely close
    And each Team's Features were worked on only by that Team

  @regression @us-07 @slice-02 @kpi @contract-shape:unbounded-preservation
  Scenario: Running the trials side by side moves no date either
    Given the checked-in baseline percentiles recorded when the draw source changed
    When the same forecast is run with its trials carried out concurrently
    Then every percentile for every Feature is identical to the baseline

  # The fixture has to be one where the Team is working on fewer Features at once than it has left,
  # because that is the only shape in which the order Features are considered in can be got wrong.
  @regression @us-07 @slice-02 @contract-shape:unbounded-preservation
  Scenario: The Features a Team works on are still the ones nearest the top of its order
    Given a Team with more Features left than it works on at once
    And the checked-in baseline percentiles recorded when the draw source changed
    When the Portfolio is forecast on the shared clock
    Then every percentile is identical to the baseline
    And the Features that received work are the ones nearest the top of the order, as before

  # DONE, in two halves that are not interchangeable. The wall clock itself was measured by hand on one
  # machine, before and after, and both numbers are recorded in the slice brief - a time recorded on one
  # machine asserts nothing on another, so a test comparing against a checked-in number would go red in
  # CI for a reason that is not a defect. TheJointForecastIsAffordableTest carries the half that travels:
  # a bound loose enough for the slowest build agent, and the dates held to the recorded baseline,
  # because a forecast that got faster by doing less work is the failure worth catching.
  @us-07 @slice-02 @kpi @contract-shape:bounded-change
  Scenario: The joint forecast is not slower than the product was before this epic
    Given the wall-clock time of a full forecast, recorded on the released product on the machine
      this comparison is run on, and checked in beside the percentiles
    When the full Feature set is forecast with the trials running concurrently
    Then it completes within one and a half times that recorded time
    And the percentiles it produced are identical to the checked-in baseline

  @edge @us-07 @slice-02 @contract-shape:unbounded-preservation
  Scenario: A Team with no measured delivery is left out exactly as it was before
    Given a third Team with no measured delivery works some of the Portfolio's Features
    And Team X and Team Y still have measured delivery
    When the Portfolio is forecast
    Then that Team is left out of the run as it always was
    And its Features are reported exactly as they were before this epic
    And Team X's and Team Y's Features were forecast, so the run did work while it left that Team out

  @driving_port @us-08 @slice-02 @contract-shape:bounded-change
  Scenario: A Feature never finishes before the other Team's Feature it is waiting on
    Given "Payment gateway upgrade" is waiting on "Checkout redesign"
    When the Portfolio is forecast
    Then in every simulated run, "Payment gateway upgrade" finishes no earlier than "Checkout redesign"
    And its dates have moved out to sit behind the other Team's work

  # DONE by deletion, which is the strongest form this can take: the reason a cross-Team wait was left
  # out no longer exists to be rendered. The closed set of reasons is asserted in two places that both
  # had to be changed for this to compile - the C# enum against the list the browser holds, and the list
  # the Features view is allowed to produce - so the warning cannot come back without both noticing.
  @driving_adapter @us-08 @slice-02 @contract-shape:bounded-change
  Scenario: The warning that said a cross-Team wait was ignored is gone
    Given "Payment gateway upgrade" carried the warning that its wait crossed a Team
    When the Portfolio is forecast after this slice
    Then that warning no longer appears on the row
    And no warning is left standing for a dependency the forecast now accounts for

  @us-08 @slice-02 @contract-shape:unbounded-preservation
  Scenario: A shared clock shares time, never delivery
    Given Team X and Team Y have very different measured delivery
    When the Portfolio is forecast on the shared clock
    Then every Feature of Team X was worked on at Team X's own rate
    And no Feature was ever worked on at another Team's rate

  @error @driving_port @us-08 @slice-02 @contract-shape:bounded-change
  Scenario: Two Features on different Teams waiting on each other constrain nothing
    Given "Payment gateway upgrade" and "Checkout redesign" are waiting on each other across the two Teams
    When the Portfolio is forecast
    Then the forecast completes in the time an unconstrained forecast takes
    And both rows carry the warning that names the other

  @error @driving_port @us-08 @slice-02 @contract-shape:bounded-change
  Scenario: Waiting on another Team's Feature that can never be forecast drops the wait, and says so
    Given "Payment gateway upgrade" is waiting on a Feature whose Team has no measured delivery
    When the Portfolio is forecast
    Then the forecast completes rather than running forever
    And "Payment gateway upgrade" is presented as the earliest it could possibly be
    And its row names the Feature the wait was dropped for, and why

  @regression @us-08 @slice-02 @contract-shape:unbounded-preservation
  Scenario: With nothing waiting on anything, the dates are still the dates
    Given no Feature in the Portfolio is waiting on another
    And the checked-in baseline percentiles recorded when the draw source changed
    When the Portfolio is forecast after cross-Team waits are accounted for
    Then every percentile for every Feature is identical to the baseline

  # The hazard is reachable from real data rather than invented: a Team whose measured delivery is
  # zero on every day drawn, working a Feature that still has work left, never clears that Feature.
  @error @us-07 @slice-02 @contract-shape:bounded-change
  Scenario: A run that will not end is stopped and says exactly which run it was
    Given a Team whose measured delivery is zero on every day the forecast draws
    And a Feature of that Team with work remaining that no other Team works on
    When the Portfolio is forecast
    Then the run stops when it reaches the limit of days a single run may simulate
    And what is reported names the run, so it can be reproduced on its own
    And the Portfolio's other Features are still reported

  @architecture @slice-02 @contract-shape:unbounded-preservation
  Scenario: What one simulated run knows cannot leak into another
    Given the trials of a forecast run side by side
    When the code is inspected for state a run holds
    Then no run's own counts or readiness are reachable from the forecasting service or from a Feature's results
    And nothing shared between runs is written to while they are running

  # Not a test. A count taken by hand on the dogfood instance, whose dependency population nobody
  # controls, recorded with its date and its denominator in the slice brief.
  @manual @kpi @slice-02 @contract-shape:bounded-change
  Scenario: Most of the dependencies an instance actually has are now accounted for
    Given the dogfood instance's dependencies, counted as detected and as accounted for after slice 01
    And the count excludes the awkward shapes planted for the tests, which can never be accounted for
    When the same count is taken after cross-Team waits are accounted for
    Then at least four in five of the dependencies within a Portfolio are accounted for
    And both counts, their date and their denominator are recorded in the slice brief
