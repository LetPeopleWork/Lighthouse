Feature: The same thing on Jira and on Linear (Epic 4365, Slice 03 — US-09)
  As a product owner whose teams work in Jira or in Linear
  I want my own tracker's dependency links read too
  So that everything the earlier slices delivered applies to my instance without my re-entering
    anything

  # Chained narrative: the Given of every scenario here is a Portfolio in exactly the state Slice 02
  # left it in — counts, detail list and warnings all working — with only the tracker changed.
  #
  # The Linear scenario below is the one trap in this feature, and it is a trap about direction rather
  # than about spelling. Linear records a dependency once and offers it from both ends: a Project's own
  # relations are what it blocks, and its inverse relations are what it is waiting on. Reading the near
  # side gives a count that looks right on every screen while pointing every edge the wrong way, which
  # is why it gets its own scenario rather than being left to the reader of the mapper.
  #
  # (An earlier draft of this file said the trap was a lower-case fold on a Linear identifier. That is
  # true of the Work Item path and false here: a Feature is a Linear Project, keyed by the id Linear
  # itself returns, so nothing is folded.)
  #
  # Jira's own name for its link — "is blocked by" — is quoted below because it is the fact under
  # test: it is the string Lighthouse looks for, and an administrator can rename it. It is read,
  # never shown. Nothing Lighthouse displays uses that word; that is asserted in Slice 02.

  @real-io @driving_port @us-09 @slice-03 @contract-shape:bounded-change
  Scenario: A Jira Feature's inward links become dependencies, and its outward ones do not
    Given a Portfolio whose Features are read from Jira
    And "Checkout redesign" carries an "is blocked by" link pointing at "Payment gateway upgrade"
    And "Checkout redesign" also carries a link in the opposite direction, pointing at
      "Address book rewrite"
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 1 Feature
    And the Feature it is waiting on is "Payment gateway upgrade"
    And "Address book rewrite" is not among the Features it is waiting on

  @error @regression @real-io @driving_port @us-09 @slice-03 @contract-shape:bounded-change
  Scenario: A Linear Feature's dependencies resolve even though the tracker names them differently
    Given a Portfolio whose Features are read from Linear
    And Linear reports "Payment gateway upgrade" as blocking "Checkout redesign"
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 1 Feature
    And the Feature it is waiting on is "Payment gateway upgrade"
    And the count is not zero, which is what a Portfolio with no dependencies would show

  @edge @real-io @driving_port @us-09 @slice-03 @contract-shape:bounded-change
  Scenario: Linear's other direction contributes nothing
    Given a Portfolio whose Features are read from Linear
    And Linear reports "Payment gateway upgrade" as blocking "Checkout redesign"
    When the Portfolio is refreshed
    Then "Payment gateway upgrade" is waiting on nothing
    And only the "waiting on" direction is read from the tracker

  @error @us-09 @slice-03 @contract-shape:pure-function
  Scenario: A Jira instance that has renamed its link type says so instead of failing quietly
    Given a Portfolio whose Features are read from Jira
    And the administrator has renamed the inward link type Lighthouse looks for
    When the Portfolio is refreshed
    Then no Feature is recorded as waiting on another
    And Lighthouse reports which inward link names it did see
    And the report names the Portfolio, so the administrator knows where to look

  @regression @real-io @us-09 @slice-03 @contract-shape:unbounded-preservation
  Scenario: Reading Jira's link information changes nothing else Lighthouse already read
    Given a Jira Feature whose every value Lighthouse holds is recorded before this change
    When the Portfolio is refreshed with dependency reading in place
    Then every value Lighthouse holds about that Feature is unchanged
    And only the Features it is waiting on are new

  @regression @real-io @us-09 @slice-03 @contract-shape:unbounded-preservation
  Scenario: Azure DevOps behaviour is unchanged by the two trackers added beside it
    Given an Azure DevOps Feature whose every value Lighthouse holds is recorded after Slice 01
    When the Portfolio is refreshed after Jira and Linear reading is in place
    Then every value Lighthouse holds about that Feature is unchanged
    And it is waiting on exactly the Features it was waiting on before

  @edge @us-09 @slice-03 @contract-shape:pure-function
  Scenario Outline: A tracker with no dependency link yields nothing and complains about nothing
    Given a Portfolio whose Features are read from <tracker>
    When the Portfolio is refreshed
    Then every Feature is waiting on nothing
    And no row carries a dependency warning
    And no error is reported, because the absence of a dependency link is not a failure

    Examples:
      | tracker    |
      | ServiceNow |
      | a CSV file |

  @driving_adapter @us-09 @slice-03 @contract-shape:pure-function
  Scenario Outline: Everything the earlier slices delivered behaves the same on every tracker
    Given a Portfolio whose Features are read from <tracker>
    And "Checkout redesign" is waiting on "Payment gateway upgrade" and on "Warehouse sync"
    And "Warehouse sync" belongs to no Portfolio that "Checkout redesign" belongs to
    When the product owner opens the Features view
    Then the row for "Checkout redesign" says it is waiting on 2 Features
    And it names both of them, each leading into the tracker it was read from
    And the entry for "Warehouse sync" says it is outside this Portfolio
    And the entry for "Payment gateway upgrade" carries no reason at all

    # This scenario said "opening what it is waiting on names both of them with their states and
    # Portfolios" until Slice 03 ran it. Slice 02 replaced the count-plus-dialog with the names on the
    # row itself, and the state and the Portfolios went with the dialog — so the sentence described a
    # screen that no longer exists.

    Examples:
      | tracker       |
      | Azure DevOps  |
      | Jira          |
      | Linear        |

  @kpi @real-io @us-09 @slice-03 @contract-shape:unbounded-preservation
  Scenario Outline: Reading dependencies costs each tracker's refresh nothing extra to speak of
    Given a full refresh from <tracker> was timed before dependency reading was added
    When the Portfolio is refreshed with dependency reading in place
    Then the refresh takes no more than 110% of the time it took before
    And the refresh makes no additional request to <tracker>

    Examples:
      | tracker |
      | Jira    |
      | Linear  |
