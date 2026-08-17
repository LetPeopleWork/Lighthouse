Feature: Reading dependencies from the field this Portfolio actually uses (Epic 4365, Slice 04 — US-04)
  As a configuration administrator whose teams record dependencies in a field of their own
  I want to tell Lighthouse which field that is, once, for the whole Portfolio
  So that my instance gets everything the standard-link instances get, without anyone re-recording
    a single link

  # Chained narrative: every scenario starts from the state Slice 03 left behind — dependencies
  # read from the tracker's own link, counted, listed and warned about — and changes exactly one
  # thing: this Portfolio now names a field of its own.
  #
  # Naming a field REPLACES the tracker's own link; it does not add to it. A Portfolio that names
  # a field is declaring that field authoritative, which is how the parent setting beside it
  # already behaves.

  Background:
    Given a Portfolio whose Features are read from Azure DevOps
    And the connection defines a custom field named "Waits On"

  @driving_adapter @us-04 @slice-04 @contract-shape:bounded-change
  Scenario: A Portfolio names the field that carries its dependencies, and the Feature list fills in
    Given the configuration administrator opens the Portfolio's advanced settings
    When the administrator sets the dependency field to "Waits On"
    And "Checkout redesign" has "1234;5678" in that field
    And the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 2 Features
    And opening what it is waiting on names both of them
    And each entry says it came from the field this Portfolio named

  @us-04 @slice-04 @contract-shape:unbounded-preservation
  Scenario: Naming a field replaces the tracker's own link rather than adding to it
    Given the Portfolio names "Waits On" as its dependency field
    And "Checkout redesign" has "1234" in that field
    And "Checkout redesign" also carries a Predecessor link in Azure DevOps pointing elsewhere
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 1 Feature
    And that Feature is the one named in "Waits On"
    And Lighthouse did not ask the tracker for its link information at all

  @edge @us-04 @slice-04 @contract-shape:bounded-change
  Scenario Outline: The field is read forgivingly, and an empty one is not a problem
    Given the Portfolio names "Waits On" as its dependency field
    And "Checkout redesign" has <content> in that field
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on <count> Features
    And no error is reported

    Examples:
      | content            | count |
      | "1234,5678"        | 2     |
      | "1234;5678"        | 2     |
      | " 1234 ; 5678 "    | 2     |
      | "1234"             | 1     |
      | ""                 | 0     |

  @error @us-04 @slice-04 @contract-shape:bounded-change
  Scenario: One mistyped entry does not throw away the good ones beside it
    # The field is maintained by hand, so it will contain typos. A list that discards itself on the
    # first bad entry would be worse than no list.
    Given the Portfolio names "Waits On" as its dependency field
    And "Checkout redesign" has "1234;not-a-real-one;5678" in that field
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 2 Features
    And the entry that matched nothing is passed over
    And opening what it is waiting on shows the entry that matched nothing as unresolved,
      rather than omitting it

  @regression @us-04 @slice-04 @contract-shape:unbounded-preservation
  Scenario: A Portfolio that names no field behaves exactly as it did before this slice
    Given the Portfolio names no dependency field
    And every value Lighthouse holds about its Features is recorded before this slice
    When the Portfolio is refreshed
    Then every Feature is waiting on exactly what it was waiting on before
    And every other value Lighthouse holds about them is unchanged
    And the tracker's own link is read exactly as it was

  @regression @us-04 @slice-04 @contract-shape:bounded-change
  Scenario: Changing which field carries dependencies makes the next refresh read everything again
    # Without this, the setting appears to do nothing at all until some unrelated change happens
    # to force a full re-read — which is a support case, not a bug report.
    Given the Portfolio has been refreshed and holds its Features
    When the administrator sets the dependency field to "Waits On"
    And the Portfolio is refreshed
    Then the refresh reads every Feature again rather than only the ones that changed
    And "Checkout redesign" is waiting on what "Waits On" says it is waiting on

  @rbac @us-04 @slice-04 @contract-shape:pure-function
  Scenario: The setting is offered per Portfolio, from that connection's own fields, to the right people
    Given the configuration administrator opens the Portfolio's advanced settings
    Then a dependency field selector appears beside the one that names the parent field
    And it offers only fields defined on this Portfolio's connection
    And it is offered nowhere on a Team's settings, because dependencies are between Features
    And a person who may not change the parent field may not change this one either

  @edge @us-04 @slice-04 @contract-shape:unbounded-preservation
  Scenario: The setting works on an instance with no premium licence, and moves no date
    Given an instance with no premium licence
    And the Portfolio names "Waits On" as its dependency field
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 2 Features
    And the detail list and the warnings behave exactly as on a licensed instance
    And no date anywhere in the instance is different
