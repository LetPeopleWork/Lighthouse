Feature: Which Features exactly, and which of those Lighthouse cannot act on (Epic 4365, Slice 02 — US-02, US-03)
  As a product owner and as a delivery lead
  I want to open the list of Features one is waiting on, and to be told plainly about every link
    Lighthouse will not be able to act on
  So that I can decide whether to chase, re-sequence or accept the wait — and so that I find the
    broken links by scanning a list rather than by auditing every Feature

  # Chained narrative continues from Slice 01: the Given of the first scenario here is exactly
  # where Slice 01's last scenario left off — a refreshed Portfolio whose Features carry counts.
  #
  # There is exactly one place in the product that decides whether a dependency can be acted on.
  # Every scenario below reads that one decision. Epic #5792 will read the same one.

  Background:
    Given a Portfolio whose Features have been refreshed and carry their dependency counts
    And "Checkout redesign" is waiting on "Payment gateway upgrade"

  @driving_adapter @us-02 @slice-02 @contract-shape:pure-function
  Scenario: Opening the list of Features one is waiting on
    When the product owner opens what "Checkout redesign" is waiting on
    Then the list names "Payment gateway upgrade"
    And it shows that Feature's state
    And it shows which Portfolios that Feature belongs to
    And it offers a way to open that Feature in the work tracking system

  @driving_adapter @us-02 @slice-02 @contract-shape:pure-function
  Scenario: Each entry says where Lighthouse read it from
    Given "Checkout redesign" is waiting on "Payment gateway upgrade" through the tracker's own link
    When the product owner opens what "Checkout redesign" is waiting on
    Then the entry for "Payment gateway upgrade" says it came from the tracker's own link
    And no entry claims a source other than the work tracking system

  @driving_adapter @us-02 @slice-02 @contract-shape:pure-function
  Scenario: An entry Lighthouse cannot act on says so, in words the reader already uses
    Given "Checkout redesign" is also waiting on "Warehouse sync", which belongs to no Portfolio
      that "Checkout redesign" belongs to
    When the product owner opens what "Checkout redesign" is waiting on
    Then the entry for "Warehouse sync" is marked as one Lighthouse cannot act on
    And the reason given is that it is outside this Portfolio
    And the reason is one of exactly four this epic can produce: outside this Portfolio, part of a
      loop, the Feature it waits on cannot be forecast, or this Portfolio ignores dependencies
    # The fourth cannot occur before Slice 04 gives a Portfolio the switch that produces it, and the
    # closed set holds a fifth — not licensed — that nothing in this epic can reach, because nothing
    # in this epic is licensed. They are all named because the set being closed is the point: a caller
    # meeting a reason it has never heard of has to guess, and the guess this feature exists to
    # prevent is "probably fine".

  @error @driving_adapter @us-02 @slice-02 @contract-shape:pure-function
  Scenario: A Feature the reader may not see is named as withheld, never quietly dropped
    Given "Checkout redesign" is also waiting on a Feature the reader has no access to
    When the product owner opens what "Checkout redesign" is waiting on
    Then that entry appears in the list
    And it is shown as withheld, with the reason
    And it discloses nothing about the Feature beyond the fact that it exists
    And the total number of entries matches what "Checkout redesign" is waiting on

  @error @rbac @driving_adapter @us-02 @slice-02 @contract-shape:pure-function
  Scenario: A reader who may not change anything sees the same list and is offered no action
    Given a reader with permission to read the Portfolio but not to change it
    When that reader opens what "Checkout redesign" is waiting on
    Then the list shows the same Features, states and Portfolios
    And no action to add, remove or suppress a dependency is offered
    And no such action exists anywhere in Lighthouse, because Lighthouse never records a dependency
      of its own

  @error @driving_adapter @us-03 @slice-02 @contract-shape:pure-function
  Scenario: Waiting on a Feature outside the Portfolio raises a warning that names it
    Given "Checkout redesign" is waiting on "Warehouse sync", which belongs to no Portfolio
      that "Checkout redesign" belongs to
    When the delivery lead opens the Features view
    Then the row for "Checkout redesign" carries a warning
    And the warning names "Warehouse sync"
    And the warning says the dependency is not included in the forecast

  @error @driving_adapter @us-03 @slice-02 @contract-shape:unbounded-preservation
  Scenario: Waiting on a Feature positioned below raises a different warning, and nothing is moved
    Given "Payment gateway upgrade" is positioned below "Checkout redesign" in the Portfolio
    When the delivery lead opens the Features view
    Then the row for "Checkout redesign" carries a warning about the ordering
    And that warning is distinct from the one about a Feature outside the Portfolio
    And the position of every Feature in the Portfolio is unchanged
    And Lighthouse has moved nothing

  @error @driving_adapter @us-03 @slice-02 @contract-shape:pure-function
  Scenario: A loop warns on every Feature in it and names the others
    Given "Payment gateway upgrade" is also waiting on "Checkout redesign"
    When the delivery lead opens the Features view
    Then the row for "Checkout redesign" carries a warning naming "Payment gateway upgrade"
    And the row for "Payment gateway upgrade" carries a warning naming "Checkout redesign"
    And the link that closes the loop is marked as one Lighthouse cannot act on
    And a chain of one hundred Features waiting on one another is reported without Lighthouse
      running out of room to work

  @edge @driving_adapter @us-03 @slice-02 @contract-shape:pure-function
  Scenario: A dependency with nothing wrong with it raises no warning at all
    # Phrased against the verdict, not against the forecast: until Epic #5792 ships, no dependency
    # has a forecast consequence either way, so "the forecast honours it" would be vacuously false
    # everywhere and would assert nothing.
    Given "Checkout redesign" is waiting only on "Payment gateway upgrade"
    And "Payment gateway upgrade" is in the same Portfolio, positioned above it, and can be forecast
    When the delivery lead opens the Features view
    Then no reason against that dependency is recorded
    And the row for "Checkout redesign" carries no dependency warning
    And having a dependency is not by itself a warning

  @edge @driving_adapter @us-03 @slice-02 @contract-shape:pure-function
  Scenario: A Feature waiting on one whose Team has no measured delivery is told why
    Given "Checkout redesign" is waiting on "Address book rewrite"
    And the Team owning "Address book rewrite" has no measured delivery to forecast from
    When the delivery lead opens the Features view
    Then the row for "Checkout redesign" carries a warning
    And the warning says the Feature it waits on cannot be forecast

  @regression @driving_adapter @us-03 @slice-02 @contract-shape:unbounded-preservation
  Scenario: The warnings that already existed are untouched
    Given "Address book rewrite" already carries the warning about being finished with work
      still remaining
    And "Payment gateway upgrade" already carries the warning about a default size
    When the delivery lead opens the Features view
    Then both existing warnings still appear, worded exactly as before
    And a dependency warning appears alongside them where one is due
    And a Feature with nothing wrong with it still shows the all-clear

  @regression @terminology @us-03 @slice-02 @contract-shape:unbounded-preservation
  Scenario: No dependency warning uses the word that already names something else
    Given every dependency warning this feature can produce
    When each one is rendered in the instance's own vocabulary
    Then none of them uses the word this instance reserves for an item that is held up right now
    And that word's existing meaning, and everywhere it is already used, are unchanged

  @architecture @kpi @us-03 @slice-02 @contract-shape:unbounded-preservation
  Scenario: Exactly one place decides whether a dependency can be acted on
    # Two independent implementations — one for the warning, one for the forecast — would show a
    # warning that disagrees with what the dates actually did. This is the whole point of stating
    # the verdict in this epic rather than in the one that consumes it.
    Given the warnings, the detail list and the count all need to know whether a dependency can
      be acted on
    When the codebase is examined
    Then exactly one component answers that question
    And every reader of the answer reads it from that one component
    And a second component that answered it would fail the build

  @kpi @real-io @us-03 @slice-02 @contract-shape:pure-function
  Scenario: The verdict is worked out from what the page already loaded
    Given a Portfolio with the number of Features a real instance carries
    When the delivery lead opens the Features view
    Then every dependency verdict on the page is worked out from what the page already loaded
    And nothing is stored as a result of working them out
