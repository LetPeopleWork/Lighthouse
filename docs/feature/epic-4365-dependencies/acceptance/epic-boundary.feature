Feature: Nothing in this epic moves a date (Epic 4365 — the boundary, asserted at every slice)
  As the maintainer
  I want the claim "seeing dependencies changes no forecast" to be a test rather than an intention
  So that the moment anyone accidentally imports forecasting behaviour into this half, the build
    says so

  # This runs at EVERY slice, not only at the first. What it guards against is an accidental
  # import, not a deliberate change — so it has to be standing at the door on the day it happens,
  # whichever day that is.
  #
  # Acting on a dependency belongs to the separate premium epic. This one reads, shows and warns.

  @regression @kpi @architecture @slice-01 @slice-02 @slice-03 @slice-04 @contract-shape:unbounded-preservation
  Scenario Outline: Adding dependency information to an instance changes no forecast anywhere
    Given a Portfolio and its Teams with a fixed starting point for every forecast, so that two
      runs of the same question give the same answer
    And the forecast dates and likelihoods for every Feature and every delivery are recorded with
      no dependency information present
    When dependency information is read and stored, as delivered by <slice>
    And every forecast is run again from the same fixed starting point
    Then every recorded date is identical
    And every recorded likelihood is identical
    And no Feature has become eligible or ineligible for a forecast

    Examples:
      | slice                                     |
      | slice 01, dependencies read and counted   |
      | slice 02, the detail list and the warnings|
      | slice 03, Jira and Linear dependencies    |
      | slice 04, the Portfolio's own field       |

  @regression @architecture @slice-01 @slice-02 @slice-03 @slice-04 @contract-shape:unbounded-preservation
  Scenario: The forecasting code is not touched by this epic at all
    Given the components that produce a forecast, its results and its randomness
    When the changes this epic makes are examined
    Then none of those components has been changed
    And nothing in this epic reads whether the instance holds a premium licence
