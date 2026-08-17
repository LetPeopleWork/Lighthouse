Feature: What a Feature is waiting on, read from Azure DevOps (Epic 4365, Slice 01 — US-01)
  As a product owner
  I want every Feature list to show how many other Features each one is waiting on
  So that the constraint the plan cannot express stops being invisible

  # Chained narrative: each scenario's Given picks up where the previous one's Given + When left
  # off. Scenario 1 establishes a refreshed Portfolio with links; the rest vary one fact at a time.
  # The word "blocked" appears nowhere — it already names a different, shipped concept.

  Background:
    Given a Portfolio whose Features are read from Azure DevOps
    And "Checkout redesign", "Payment gateway upgrade" and "Address book rewrite" are Features
      in that Portfolio

  @real-io @driving_port @us-01 @slice-01 @contract-shape:bounded-change
  Scenario: Predecessor links recorded in the tracker become a count on the Feature row
    Given "Checkout redesign" is recorded in Azure DevOps as waiting on
      "Payment gateway upgrade" and "Address book rewrite"
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 2 Features
    And the two Features it is waiting on are "Payment gateway upgrade" and "Address book rewrite"

  @real-io @driving_adapter @us-01 @slice-01 @contract-shape:pure-function
  Scenario: The same count is read on both Feature lists, because there is only one of them
    Given "Checkout redesign" is waiting on 2 Features after a refresh
    When the product owner opens the Features view
    And the product owner opens the Portfolio's own Feature list
    Then both lists say "Checkout redesign" is waiting on 2 Features
    And both lists take that column from the same single definition

  @edge @driving_adapter @us-01 @slice-01 @contract-shape:pure-function
  Scenario: A Feature waiting on nothing reads as nothing, not as zero
    Given "Payment gateway upgrade" is recorded in Azure DevOps as waiting on no other Feature
    When the Portfolio is refreshed
    And the product owner opens the Features view
    Then the row for "Payment gateway upgrade" shows the empty marker
    And the row for "Payment gateway upgrade" does not show a count of 0

  @edge @driving_port @us-01 @slice-01 @contract-shape:bounded-change
  Scenario: A link pointing at something Lighthouse does not keep as a Feature is passed over
    Given "Checkout redesign" is recorded in Azure DevOps as waiting on
      "Payment gateway upgrade" and on a Work Item that Lighthouse does not keep as a Feature
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 1 Feature
    And the refresh reports no error for the entry it passed over
    And the row for "Checkout redesign" is complete in every other respect

  @regression @driving_port @us-01 @slice-01 @contract-shape:bounded-change
  Scenario: A link removed in the tracker lowers the count on the next refresh
    Given "Checkout redesign" is waiting on 2 Features after a refresh
    And the link to "Address book rewrite" is removed in Azure DevOps
    When the Portfolio is refreshed again
    Then "Checkout redesign" is waiting on 1 Feature
    And the only Feature it is waiting on is "Payment gateway upgrade"

  @regression @driving_port @us-01 @slice-01 @contract-shape:unbounded-preservation
  Scenario: Reading dependencies changes nothing else about a Feature
    Given "Checkout redesign" has been placed by hand at a chosen position in the Portfolio
    And every value Lighthouse holds about "Checkout redesign" is recorded before the refresh
    When the Portfolio is refreshed and dependency information is read for the first time
    Then "Checkout redesign" is waiting on 2 Features
    And its hand-chosen position is unchanged
    And every other value Lighthouse holds about it is unchanged

  @edge @terminology @driving_adapter @us-01 @slice-01 @contract-shape:pure-function
  Scenario: The column speaks the instance's own vocabulary
    Given the instance has renamed what it calls a Feature
    When the product owner opens the Features view
    Then the new column's heading uses the instance's own word for Features
    And no heading, cell or tooltip in that column uses the word this instance reserves
      for an item that is held up right now

  @error @regression @driving_port @us-01 @slice-01 @contract-shape:unbounded-preservation
  Scenario: A Portfolio that already names its own parent field still gets its dependencies
    # This is the cheapest defect in the epic to write and the hardest to notice: the column
    # would read empty forever, and an empty column is a legitimate answer, so nothing about
    # the screen would look wrong.
    Given the Portfolio names a custom field that carries the parent of each Work Item
    And the Portfolio names no custom field for dependencies
    And "Checkout redesign" is recorded in Azure DevOps as waiting on "Payment gateway upgrade"
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 1 Feature
    And Lighthouse still asked the tracker for the link information it needs
    And the parent of each Feature is still read from the custom field the Portfolio named

  @error @regression @driving_port @us-01 @slice-01 @contract-shape:unbounded-preservation
  Scenario: A Team that names its own parent field is completely unaffected
    Given a Team that names a custom field carrying the parent of each Work Item
    When that Team is refreshed
    Then the Team's Work Items are read exactly as they were before this change
    And no dependency information is read for a Team, because dependencies are between Features

  @kpi @real-io @us-01 @slice-01 @contract-shape:unbounded-preservation
  Scenario: Reading dependencies costs the refresh nothing extra to speak of
    Given a full refresh of the Portfolio was timed before dependency reading was added
    When the Portfolio is refreshed with dependency reading in place
    Then the refresh takes no more than 110% of the time it took before
    And the refresh makes no additional request to Azure DevOps
