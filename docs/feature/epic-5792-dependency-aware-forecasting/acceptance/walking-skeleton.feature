Feature: A date that accounts for what cannot start yet (walking skeleton — Epic 5792, Slice 01)
  As a delivery forecaster
  I want the date beside a Feature to account for the Feature it is waiting on
  So that I stop presenting a number I already know is optimistic

  # Walking skeleton: the one scenario that closes the whole loop through the running product —
  # a real dependency recorded in the work tracking system, read by the refresh that already runs,
  # accounted for inside the simulation that produces every date in the product, and read back off
  # the Features view by the person who has to commit to that date.
  #
  # Strategy B, inherited from DISCUSS: nothing new is built to carry it. The Features view, the
  # refresh and the simulation are all in production, and Epic #4365 already put the dependency on
  # the row. This scenario is the first moment the *date* beside it means something different.
  #
  # The before-picture is constructed inside the run, and the licence is what constructs it. An
  # unlicensed instance forecasts exactly as though the dependency were not recorded (AC-6.2), so
  # its dates ARE the before-picture — obtainable in one browser session, from the same data, on the
  # same build. Comparing against "what the previous release produced" would need a number no
  # Playwright run can reach, and would leave the skeleton asserting nothing.
  #
  # Litmus test: a delivery forecaster reads this scenario and confirms "yes, that is what I need".

  @walking_skeleton @real-io @driving_adapter @us-05 @slice-01 @contract-shape:bounded-change
  Scenario: A forecaster sees a date that has moved because of what the Feature is waiting on
    Given Lighthouse is running against a work tracking system in which "Payment gateway upgrade"
      is recorded as waiting on "Checkout redesign"
    And "Address book rewrite" is ranked below "Payment gateway upgrade" and waits on nothing
    And all three Features are in the same Portfolio and worked by the same Team
    And the instance is not yet licensed for dependency-aware forecasting
    When the Portfolio is refreshed and forecast
    And the forecaster opens the Features view and notes the date beside each of the three Features
    And the instance is licensed and the Portfolio is forecast again
    Then the date for "Payment gateway upgrade" is later than the one the forecaster noted
    And the date for "Address book rewrite" is earlier than the one the forecaster noted
    And the row for "Payment gateway upgrade" no longer says its wait is left out of the forecast
    And the forecaster reached that answer without opening the work tracking system
