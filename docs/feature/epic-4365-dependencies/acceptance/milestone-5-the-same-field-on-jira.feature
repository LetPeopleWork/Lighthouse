Feature: The same field, on Jira (Epic 4365, Slice 05 — US-04 on Jira)
  As a configuration administrator whose teams record dependencies in a field of their own
  I want the field I named to be read on Jira as it already is on Azure DevOps
  So that naming it once means the same thing wherever my Features are read from

  # Chained narrative: the scenarios that read a Jira Portfolio's own field start from the state
  # Slice 04 left behind — a Portfolio can name a field, and on Azure DevOps that field replaces
  # the tracker's own link — and change exactly one thing: this Portfolio's Features are read from
  # Jira. Four of them stand outside that chain and say so where they sit: two compare one tracker
  # against another, one asks the same question of the codebase rather than of an instance, and one
  # checks that a tracker nobody changed was left alone.
  #
  # Slice 04 shipped the setting for every Portfolio and honoured it on one tracker. On Jira the
  # setting was accepted, saved, and then silently ignored, which is worse than not offering it:
  # the administrator has no way to tell the difference between a field that is being read and a
  # field that everyone's Features happen to have left empty.
  #
  # A Jira entry is an issue key, not a number, and that is the whole reason these scenarios exist
  # rather than a tracker column on the Slice 04 file: what a valid entry looks like is different
  # here, and so is what a near-miss looks like.

  Background:
    Given a Portfolio whose Features are read from Jira
    And the connection defines a custom field named "Waits On"

  @driving_adapter @us-04 @slice-05 @contract-shape:bounded-change
  Scenario: A Jira Portfolio names the field that carries its dependencies, and the Feature list fills in
    Given the configuration administrator opens the Portfolio's advanced settings
    When the administrator sets the dependency field to "Waits On"
    And "Checkout redesign" has "LGHTHSDMO-7;LGHTHSDMO-9" in that field
    And the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 2 Features
    And opening what it is waiting on names both of them
    And each entry says it came from the field this Portfolio named

  # Naming a field is a declaration that the field is authoritative. A Portfolio that meant "both"
  # would have no way left to say "only the field", and the tracker's own link is the thing people
  # are naming a field to get away from.
  @us-04 @slice-05 @contract-shape:unbounded-preservation
  Scenario: Naming a field replaces Jira's own link rather than adding to it
    Given the Portfolio names "Waits On" as its dependency field
    And "Checkout redesign" has "LGHTHSDMO-7" in that field
    And "Checkout redesign" also carries an "is blocked by" link pointing at "Warehouse sync"
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 1 Feature
    And that Feature is the one named in "Waits On"
    And "Warehouse sync" is not among the Features it is waiting on
    And the "is blocked by" link is not read for this Portfolio at all

  # The detail list already tells the reader where each entry came from. Someone comparing two
  # Portfolios on two trackers should not be able to work out which tracker they are looking at
  # from that line alone — the line describes the Portfolio's own setting, not the tracker.
  @driving_adapter @us-04 @slice-05 @contract-shape:pure-function
  Scenario: Where an entry was read from reads the same on Jira as on Azure DevOps
    Given a Jira Portfolio and an Azure DevOps Portfolio that each name "Waits On" as their dependency field
    And a Feature in each has a single entry in that field
    When the delivery lead opens what each of those Features is waiting on
    Then both entries say they came from the field the Portfolio named
    And neither says it came from the tracker's own link
    And the two say it in the same words

  @edge @us-04 @slice-05 @contract-shape:bounded-change
  Scenario Outline: The field is read forgivingly on Jira too, and an empty one is not a problem
    Given the Portfolio names "Waits On" as its dependency field
    And "Checkout redesign" has <content> in that field
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on <count> Features
    And no error is reported

    Examples:
      | content                        | count |
      | "LGHTHSDMO-7,LGHTHSDMO-9"      | 2     |
      | "LGHTHSDMO-7;LGHTHSDMO-9"      | 2     |
      | " LGHTHSDMO-7 ; LGHTHSDMO-9 "  | 2     |
      | "LGHTHSDMO-7"                  | 1     |
      | ""                             | 0     |

  # The field is maintained by hand, so it will contain typos. A list that discards itself on the
  # first bad entry would be worse than no list.
  @error @us-04 @slice-05 @contract-shape:bounded-change
  Scenario: One mistyped key does not throw away the good ones beside it
    Given the Portfolio names "Waits On" as its dependency field
    And "Checkout redesign" has "LGHTHSDMO-7;LGHTHSDMO-404;LGHTHSDMO-9" in that field
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 2 Features
    And the entry that matched nothing is passed over, exactly as an unresolvable link is

  # People type into this box by hand and paste into it from a browser, and on Jira the letters
  # before the dash are part of the name rather than decoration. Tidying up what was typed —
  # correcting the letters, pulling a key out of a pasted address — would leave a Feature quietly
  # waiting on something nobody chose, which is far harder to notice than an entry that was left
  # out and can be seen missing.
  @error @edge @us-04 @slice-05 @contract-shape:bounded-change
  Scenario: What was typed in is read as written, and what is not a key names nothing
    Given the Portfolio names "Waits On" as its dependency field
    And "Checkout redesign" has "LGHTHSDMO-7;lghthsdmo-9;https://example.net/browse/LGHTHSDMO-11" in that field
    When the Portfolio is refreshed
    Then "Checkout redesign" is waiting on 1 Feature
    And the Feature it is waiting on is the one named by "LGHTHSDMO-7"
    And the other two entries are passed over, exactly as a mistyped one is
    And neither of them is tidied up into something that would have matched

  # A field name that exists nowhere is a typo in a settings box, and the person who made it is
  # still sitting in front of that box. Telling them while they are there is a correction; telling
  # them at the next refresh is an empty column and a hunt for the reason.
  @error @us-04 @slice-05 @contract-shape:pure-function
  Scenario: A field name that exists nowhere is caught while the administrator is still looking at it
    Given the configuration administrator opens the Portfolio's advanced settings
    When the administrator names a dependency field the connection does not define
    And the connection is checked
    Then Lighthouse reports that the field could not be found, and names it
    And it reads the same as it already does for a mistyped parent field

  # There is a report that names the inward link names Lighthouse did see, for the instance that
  # renamed the one it looks for and cannot work out why nothing is waiting on anything. A
  # Portfolio reading its dependencies from a field of its own has deliberately stopped reading
  # links, so that report is telling it about a decision it made on purpose — and telling it in
  # words that describe a misconfiguration.
  #
  # This is only worth asserting when the named field yields nothing. A field with entries in it
  # silences that report on its own, whichever way this is built, so a scenario written with a
  # populated field would pass just as well before any of this existed and would prove nothing.
  @error @regression @us-04 @slice-05 @contract-shape:unbounded-preservation
  Scenario: A Portfolio reading a field of its own is not told that its links are named wrong
    Given the Portfolio names "Waits On" as its dependency field
    And every Feature's "Waits On" field is empty
    And its Features do carry inward links, under a name Lighthouse does not look for
    When the Portfolio is refreshed
    Then no Feature is recorded as waiting on another
    And nothing at all is reported about how the inward links are named
    And the refresh reports nothing it did not report before this Portfolio named a field

  # The report is narrowed, not removed. The instance that renamed its inward link type and still
  # reads its dependencies from links is the one the report was written for, and without it that
  # instance is left with an empty column and nothing to go on.
  @error @regression @us-04 @slice-05 @contract-shape:bounded-change
  Scenario: The same Portfolio, once it names no field, is told about its links again
    Given the Portfolio names "Waits On" as its dependency field
    And its Features carry inward links under a name Lighthouse does not look for
    And nothing is reported about how those links are named
    When the administrator clears the Portfolio's dependency field
    And the Portfolio is refreshed
    Then Lighthouse reports which inward link names it did see
    And the report names the Portfolio, so the administrator knows where to look

  # The named field arrives inside the answer the refresh already asks for, on both the hosted and
  # the self-run flavour of the tracker. If reading it ever needed a second question, a Portfolio
  # would pay one request per Feature for a setting that reads as free — and would pay it on the
  # instances with the most Features, which are the ones that name a field.
  @kpi @real-io @us-04 @slice-05 @contract-shape:unbounded-preservation
  Scenario Outline: Reading the named field costs the refresh no extra question
    Given a Portfolio on <deployment> that names "Waits On" as its dependency field
    And a full refresh was timed before this Portfolio named a field
    When the Portfolio is refreshed
    Then the refresh makes no additional request to the tracker
    And the refresh asks for exactly what it asked for before, in exactly the same shape
    And the refresh takes no more than 110% of the time it took before

    Examples:
      | deployment       |
      | Jira Cloud       |
      | Jira Data Center |

  # A setting that needs some unrelated change to force a re-read appears to do nothing at all,
  # which arrives as a support case rather than a bug report. What already forces the re-read
  # covers this setting on every tracker, so counting it a second time would force a full download
  # for a change that never happened.
  @regression @us-04 @slice-05 @contract-shape:bounded-change
  Scenario: Changing the field reads every Feature again, and changing nothing reads only what changed
    Given the Portfolio has been refreshed and holds its Features
    When the administrator sets the dependency field to "Waits On"
    And the Portfolio is refreshed
    Then the refresh reads every Feature again rather than only the ones that changed
    And "Checkout redesign" is waiting on what "Waits On" says it is waiting on
    When the Portfolio is refreshed once more with no setting touched
    Then the refresh reads only the Features that changed

  @regression @us-04 @slice-05 @contract-shape:unbounded-preservation
  Scenario: A Jira Portfolio that names no field behaves exactly as it did before this slice
    Given the Portfolio names no dependency field
    And every value Lighthouse holds about its Features is recorded before this slice
    When the Portfolio is refreshed
    Then every Feature is waiting on exactly what it was waiting on before
    And every other value Lighthouse holds about them is unchanged
    And the tracker's own "is blocked by" link is read exactly as it was

  # Moving the choice out of the trackers and into one place is only safe if the tracker that
  # already honoured the setting comes through it unchanged. The proof is that the checks written
  # for Azure DevOps in the earlier slice still pass with nothing altered in them — a check edited
  # to keep it passing has stopped describing the behaviour it was written for, and the edit is
  # itself the report that behaviour changed.
  @regression @us-04 @slice-05 @contract-shape:unbounded-preservation
  Scenario: Azure DevOps is unchanged by the choice moving out of it
    Given an Azure DevOps Portfolio that names "Waits On" as its dependency field
    And an Azure DevOps Portfolio that names no dependency field
    And an Azure DevOps Portfolio that names both a parent field and a dependency field
    And every value Lighthouse holds about all three Portfolios' Features is recorded beforehand
    When the choice of where to read dependencies from moves out of the tracker
    And all three Portfolios are refreshed
    Then every Feature is waiting on exactly what it was waiting on before
    And each entry says it was read from exactly where it said before
    And only the Portfolio that names both fields skips asking for the tracker's link information
    And the checks written for Azure DevOps in the earlier slice pass with nothing altered in them

  # Where an entry was read from is a fact about the Portfolio's settings, not about the tracker,
  # so a tracker left to work it out for itself will get it wrong the first time a fourth tracker
  # is added and nobody remembers this rule. A guard like this one is only worth having once it
  # has been broken on purpose and watched to fail — a guard nobody has seen fail is an assumption
  # wearing a guard's name.
  @architecture @us-04 @slice-05 @contract-shape:unbounded-preservation
  Scenario: Exactly one place decides where a dependency was read from
    Given every tracker that can read dependencies from a field of the Portfolio's choosing
    When the codebase is examined
    Then exactly one component decides whether the named field or the tracker's own link was used
    And no tracker records "the field this Portfolio named" as the source by itself
    And a tracker that recorded it by itself would fail the build

  # Linear has no fields of its own for a Portfolio to name, so a Portfolio there goes on reading
  # the tracker's own links whatever is typed into the setting. Making Jira honour the field must
  # leave that exactly where it was.
  @edge @regression @us-04 @slice-05 @contract-shape:unbounded-preservation
  Scenario: A Linear Portfolio goes on reading its own links, undisturbed
    Given a Portfolio whose Features are read from Linear
    And Linear reports "Payment gateway upgrade" as blocking "Checkout redesign"
    When the Portfolio is refreshed after Jira honours the named field
    Then "Checkout redesign" is waiting on 1 Feature
    And each entry says it came from the tracker's own link
    And every other value Lighthouse holds about that Feature is unchanged
