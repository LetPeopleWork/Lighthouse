Feature: A Feature list that says what each Feature is waiting on (walking skeleton — Epic 4365, Slice 01)
  As a product owner
  I want the Feature list to tell me how many other Features each one is waiting on
  So that I stop discovering a dependency for the first time in a stakeholder review

  # Walking skeleton: the one scenario that closes the whole loop through the production
  # composition root — a real Predecessor link recorded in Azure DevOps, read by the refresh
  # that already runs, stored against the Feature, and read back off the Features view by a
  # person who never opens the work tracking system.
  #
  # Driving adapter = the Features view in the running application, exercised through the real
  # browser and the existing Page Object, not through a service call.
  #
  # Litmus test: a product owner reads this scenario and confirms "yes, that is what I need".

  @walking_skeleton @real-io @driving_adapter @us-01 @slice-01 @contract-shape:bounded-change
  Scenario: A product owner sees, without leaving Lighthouse, that a Feature is waiting on two others
    Given Lighthouse is running against a work tracking system in which "Checkout redesign"
      is recorded as waiting on "Payment gateway upgrade" and "Address book rewrite"
    And all three Features belong to the same Portfolio
    When the Portfolio is refreshed
    And the product owner opens the Features view
    Then the row for "Checkout redesign" says it is waiting on 2 Features
    And the row for "Payment gateway upgrade" says it is waiting on nothing
    And the product owner has read the answer without opening the work tracking system
