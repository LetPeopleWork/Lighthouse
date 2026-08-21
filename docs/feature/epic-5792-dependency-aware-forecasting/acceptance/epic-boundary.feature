Feature: What this epic promised not to change (Epic 5792 — the boundary as a test)
  As the maintainer of a product whose core output is a forecast
  I want the things this epic said it would not touch to be asserted rather than assumed
  So that a date that moves after release can be attributed to the dependency mechanic or ruled out

  # Every scenario here is a guarantee this epic gave and can only keep by measurement.
  #
  # The baselines these scenarios compare against are CHECKED-IN GOLD FILES with a provenance: the
  # percentiles, the write-back count and the wall-clock number are captured on the released product,
  # at a named commit, and committed as their own reviewed change BEFORE the first production commit
  # of slice 00. That ordering is the whole difference between a gate and a tautology — a baseline
  # computed from the build under test asserts that the build equals itself.
  #
  # The released product draws from an unseeded source, so a gold percentile set can only be taken
  # from it by driving that build with a test-only seeded source at the tagged commit. Where that is
  # not possible, the claim is carried by slice 02's own commit-to-commit equality instead, and this
  # file says which.
  #
  # The mirror of Epic #4365's boundary file: that one asserts nothing in the community epic moves a
  # date. This one asserts that everything in this epic which is not the mechanic itself moves none
  # either.

  @regression @kpi @slice-01 @contract-shape:unbounded-preservation
  Scenario: With no dependency anywhere, the dates are what the gold set says
    Given a Portfolio in which no Feature is waiting on another
    And the gold percentiles for it, captured on the released product at the tagged commit and checked in
    When the Portfolio is forecast after the forecast learns to account for dependencies
    Then every percentile for every Feature is identical to the gold set

  @regression @slice-01 @contract-shape:unbounded-preservation
  Scenario: A Feature with no dependency is unaffected by one that has several
    Given a Portfolio in which one Feature waits on two others and a fourth waits on nothing
    And the fourth Feature is worked by a Team of its own
    When the Portfolio is forecast
    Then the fourth Feature's percentiles are identical to a run of the same build in which no
      dependency was recorded

  @regression @slice-02 @contract-shape:unbounded-preservation
  Scenario: How several Teams' dates are combined into one is not touched by this epic
    Given a Feature worked by three Teams, waiting on nothing
    And the gold percentiles for it, checked in before this epic's first production commit
    When the Portfolio is forecast after every commit in this epic
    Then the combined date for that Feature is identical to the gold set

  # The residual this epic accepted rather than corrected, asserted in the direction it was accepted
  # in. Its own scenario, because a failure here means something different from the one above.
  @regression @slice-02 @contract-shape:bounded-change
  Scenario: A Feature that is both worked by several Teams and waiting reads late, never early
    Given a Feature worked by three Teams, waiting on a Feature those Teams also work on
    When the Portfolio is forecast
    Then its combined date is no earlier than the date a run that accounted for the shared wait would give
    And the direction of the difference is recorded, since it is the residual this epic accepted

  @architecture @slice-01 @contract-shape:unbounded-preservation
  Scenario: Nothing in this epic can be reached by asking the product a new question
    Given the product's routes before this epic
    When the routes are compared after it
    Then no route has been added

  @regression @slice-01 @contract-shape:unbounded-preservation
  Scenario: The warnings that were already on a row are left exactly as they were
    Given a Feature carrying the warnings the product showed before this epic
    When it is also waiting on another Feature that cannot be accounted for
    Then the warnings it already had read exactly as they did
    And the dependency's own warning is added beside them rather than replacing anything

  @regression @slice-00 @contract-shape:unbounded-preservation
  Scenario: How often a forecast runs is the only thing slice 00 changed about forecasting
    Given the gold percentiles for a Portfolio, checked in before slice 00's first production commit
    When the same Portfolio is forecast after slice 00
    Then every percentile is identical to the gold set
    And the second and third forecasts of the batch no longer happen
