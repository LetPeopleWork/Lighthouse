Feature: The two dependency settings a Portfolio owns (Epic 4365, Slice 04 — US-04, US-10)
  As a configuration administrator whose teams record dependencies in a field of their own
  I want to tell Lighthouse which field that is, once, for the whole Portfolio
  So that my instance gets everything the standard-link instances get, without anyone re-recording
    a single link

  And as a delivery lead trying out a different order of Features
  I want to set that Portfolio's dependencies aside without deleting or hiding them
  So that I see the plan I asked for rather than the one the dependencies allow

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

  # US-10 — setting the dependencies aside, added 2026-08-18.
  #
  # Ignoring is not hiding. Everything stays on the screen: the count, the list, where each entry
  # came from. What changes is that none of it is acted on. The switch exists for the lead who is
  # re-ordering Features to see what a different plan would look like, and who would otherwise have
  # to edit links in the tracker — changing the real plan in order to ask a hypothetical question.
  #
  # The Background's custom field plays no part below: these scenarios hold whatever the Portfolio
  # already reads its dependencies from, and change only whether they are acted on.

  @driving_adapter @us-10 @slice-04 @contract-shape:pure-function
  Scenario: Setting the dependencies aside leaves every one of them in plain sight
    Given "Checkout redesign" is waiting on 2 Features
    When the Portfolio is set to ignore its dependencies
    Then "Checkout redesign" is still waiting on 2 Features
    And opening what it is waiting on still names both of them, with where each was read from
    And every entry says it is ignored for this Portfolio
    And no entry has been deleted, changed or hidden

  @edge @driving_adapter @us-10 @slice-04 @contract-shape:pure-function
  Scenario: Nothing is warned about while the dependencies are set aside
    Given the Portfolio is set to ignore its dependencies
    And it holds a Feature waiting on one outside the Portfolio
    And a Feature waiting on one positioned below it
    And a loop between two of its Features
    When the delivery lead scans the Feature list
    Then no dependency warning appears against any of them
    And the warnings that existed before this feature was built are shown exactly as before

  @regression @us-10 @slice-04 @contract-shape:unbounded-preservation
  Scenario: The switch takes hold without a refresh, and putting it back changes nothing
    # A setting that needs a full re-download to take effect is a setting nobody experiments with,
    # and experimenting is the entire point of this one.
    Given the Portfolio holds its Features and their dependencies
    And every value Lighthouse holds about them is recorded
    When the Portfolio is set to ignore its dependencies
    Then the next page load shows them ignored, with no refresh asked for and nothing re-downloaded
    And what Lighthouse has stored about their dependencies is unchanged
    When the Portfolio is set to honour its dependencies again
    Then every value Lighthouse holds about them matches what was recorded

  @edge @us-10 @slice-04 @contract-shape:pure-function
  Scenario: A dependency another Portfolio still honours keeps the verdict it had
    # A Feature can belong to several Portfolios. One Portfolio trying out a plan must not decide
    # what another Portfolio's forecast is allowed to see.
    Given "Checkout redesign" and "Payment gateway upgrade" both belong to two Portfolios
    And "Checkout redesign" is waiting on "Payment gateway upgrade"
    When only one of those two Portfolios is set to ignore its dependencies
    Then that dependency is still honoured
    And it reads as ignored only once both Portfolios ignore theirs

  @rbac @us-10 @slice-04 @contract-shape:pure-function
  Scenario: The switch is offered per Portfolio, unlicensed, and starts off everywhere
    Given an instance with no premium licence
    And every Portfolio that existed before this feature was built
    Then each of them honours its dependencies, because the switch starts off
    And the switch appears in the Portfolio's advanced settings beside the dependency field
    And it appears nowhere on a Team's settings, because dependencies are between Features
    And a person who may not change the dependency field may not change this either
    And turning it on changes no date anywhere in the instance

  @error @us-10 @slice-04 @contract-shape:pure-function
  Scenario: A loop is still found while the dependencies are set aside
    # The loop check is what stops a forecast running forever. Switching the dependencies off must
    # switch off what is acted on, never what is looked for — otherwise the verdict a Feature gets
    # the moment the switch goes back off is one computed for the first time, on a plan already
    # being read.
    Given the Portfolio is set to ignore its dependencies
    And two of its Features wait on each other
    When the Portfolio is refreshed
    Then the loop is detected exactly as it would be otherwise, and still stored nowhere
    And both Features read as ignored for this Portfolio, which is the reason that takes precedence
    When the Portfolio is set to honour its dependencies again
    Then both Features read as part of a loop, with no second look needed
