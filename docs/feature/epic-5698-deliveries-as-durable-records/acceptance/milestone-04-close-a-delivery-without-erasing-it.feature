Feature: Close a Delivery without erasing it (Epic 5698, Slice 04 — US-04)
  As a Delivery Forecaster whose Portfolio runs indefinitely
  I want to retire a Delivery that has finished or been called off
  So that the list of live commitments is about what is ahead, without paying for that in lost history

  # Everything a Delivery shows is worked out fresh on every read, from the Features it holds right
  # now. So closing a Delivery cannot be a flag: a closed Delivery that keeps recomputing shows numbers
  # derived from Features that have since been re-synced or re-matched, and a closed Delivery that
  # stops recomputing has nothing at all to show. Closing therefore writes down the answer — once, at
  # the moment of closing — and that written answer is what a closed Delivery shows from then on.
  #
  # "Once" is load-bearing and is asserted three ways below: exactly one written record per Delivery
  # after closing; one after closing on a day the daily recorder has already run; and one after closing
  # a Delivery the daily recorder has never run for at all. The third is not an exotic case — it is a
  # Delivery created and closed the same afternoon, which is what happens when somebody sets one up by
  # mistake and tidies up.
  #
  # Closing also has to stop things, not merely hide a row. Two background paths keep touching a
  # Delivery after it is closed if nobody stops them: the daily recorder, which would keep adding rows
  # around the one that is supposed to be *the* record, and the rule-based re-matching, which would
  # empty a closed Delivery's Feature list with no human involved. Both get a scenario, and both are
  # counted rather than described, because "it skips them" is not something you can read off a screen.
  #
  # The sharpest scenario in this file is the race. A Portfolio refresh that was already in flight when
  # somebody clicked Archive is holding a copy of the Delivery from before it was closed, and that copy
  # believes it is still open. Whatever stops that copy from being written back has to stop it without
  # blowing up a background job that has done nothing wrong.

  Background:
    Given a Portfolio holding a Delivery whose work has finished

  @driving_adapter @us-04 @slice-04 @ac-04.1 @contract-shape:bounded-change
  Scenario: A finished Delivery offers a way to retire it beside the ways to change and destroy it
    Given a forecaster who may change this Portfolio
    When the forecaster opens the Delivery's header
    Then it offers Archive alongside Edit and Delete

  @error @driving_adapter @us-04 @slice-04 @ac-04.1 @contract-shape:unbounded-preservation
  Scenario: A reader who may not change the Portfolio is not offered the way to retire a Delivery
    Given a user who may read this Portfolio and not change it
    When that user opens the Delivery's header
    Then there is no Archive action
    When that user asks the product directly to archive it
    Then the request is refused as beyond their rights
    And the Delivery is still in the list of live commitments

  @driving_adapter @us-04 @slice-04 @ac-04.2 @contract-shape:bounded-change
  Scenario: Retiring a Delivery asks first, and says what it will and will not do
    Given a forecaster who may change this Portfolio
    When the forecaster chooses Archive
    Then a confirmation appears before anything happens
    And it says the Delivery can be brought back
    And it says this is not the same as deleting it
    And declining leaves the Delivery exactly where it was

  # Archiving and Delete both remain. A confirmation that let a reader infer "archived means safe" would
  # be setting up the day somebody archives a Delivery instead of backing it up.
  @error @us-04 @slice-04 @ac-04.2 @contract-shape:unbounded-preservation
  Scenario: The confirmation does not promise a protection that archiving does not give
    When the forecaster reads the archive confirmation
    Then it does not claim the Delivery becomes protected, safe, permanent or impossible to remove
    And it does not present archiving as an alternative to a backup

  @driving_adapter @us-04 @slice-04 @ac-04.3 @contract-shape:bounded-change
  Scenario: Retiring a Delivery writes down what it said at that moment, once
    Given the Delivery currently shows a likelihood, three forecast dates and a grid of Features with
      their totals
    When the forecaster archives it
    Then exactly one written record is held for that Delivery
    And it carries the likelihood, the three forecast dates and every Feature row as they stood at
      that moment
    And the day it was archived is recorded on the Delivery itself

  @edge @us-04 @slice-04 @ac-04.4 @contract-shape:bounded-change
  Scenario: A Delivery created and retired the same afternoon still has a complete written record
    Given a Delivery created today, for which the daily recorder has never run
    When the forecaster archives it
    Then exactly one written record is held for it
    And that record carries its likelihood, its forecast dates and its Feature rows in full
    And nothing about the record is empty because the daily run had not happened yet

  @edge @us-04 @slice-04 @ac-04.3 @ac-04.4 @contract-shape:bounded-change
  Scenario: Retiring a Delivery on a day its numbers were already recorded still leaves one record
    Given the daily recorder has already recorded this Delivery's numbers today
    When the forecaster archives it
    Then exactly one written record is held for it, and archiving did not fail
    And today's daily recording is still there, unchanged, as one day of its history

  @driving_adapter @us-04 @slice-04 @ac-04.5 @contract-shape:bounded-change
  Scenario: A retired Delivery leaves the live list and is found under the ones that are done
    Given a forecaster who may change this Portfolio
    When the forecaster archives the Delivery
    Then it is no longer among the Portfolio's live commitments
    And an Archived section holds it, folded away until someone opens it
    And that section shows its name, its date and the headline numbers that were written down

  # Storage stops growing at closing time. Counted across several refreshes rather than asserted once,
  # because "it skipped it that one time" and "it skips it" are different claims.
  @kpi @us-04 @slice-04 @ac-04.6 @contract-shape:unbounded-preservation
  Scenario: A retired Delivery stops accumulating daily rows
    Given the Delivery has been archived
    And the number of days recorded for it is noted
    When the Portfolio is refreshed and forecast five more times over five days
    Then the number of days recorded for it is exactly the number that was noted
    And the other, still-live Deliveries in the Portfolio each recorded five more days as usual

  @kpi @us-04 @slice-04 @ac-04.6 @contract-shape:unbounded-preservation
  Scenario: Retiring a Delivery keeps its history, where destroying one loses it
    Given the Delivery has weeks of recorded history behind it
    And the number of days recorded for it is noted
    When the forecaster archives it
    Then the number of days recorded for it is unchanged
    When the forecaster instead deletes a second Delivery with weeks of history
    Then that second Delivery's recorded days are gone
    And the difference between the two outcomes is the reason archiving exists

  @us-04 @slice-04 @ac-04.7 @contract-shape:unbounded-preservation
  Scenario: A retired Delivery that picks its Features by rule stops picking them
    Given the Delivery chooses its Features by a rule rather than by hand
    And the Features it currently holds are noted
    When the forecaster archives it
    And the Portfolio is refreshed with Features that the rule would now match differently
    Then the retired Delivery holds exactly the Features that were noted
    And a still-live Delivery using the same rule did re-match, as it always has

  @us-04 @slice-04 @ac-04.8 @contract-shape:unbounded-preservation
  Scenario: Features disappearing from the Portfolio do not change what a retired Delivery said
    Given the Delivery has been archived and its headline numbers are noted
    When two of its Features are removed from the Portfolio entirely
    Then the retired Delivery's headline numbers are exactly what was noted
    And its Feature grid still lists the removed Features as it did at closing

  # The race. A refresh that started before the click is holding a copy of the Delivery that still
  # believes it is live, and it will try to save that copy after the archive has landed.
  @error @us-04 @slice-04 @ac-04.7 @contract-shape:unbounded-preservation
  Scenario: A refresh already under way when a Delivery is retired does not undo the retirement
    Given a Portfolio refresh has loaded the Delivery and is about to re-match its Features
    When the forecaster archives the Delivery before that refresh saves
    And the refresh then tries to save its re-matched Features
    Then the Delivery still holds the Features it held when it was archived
    And it is still archived
    And the refresh completes without raising anything to the job that ran it
    And the refresh does not retry the change it was about to make

  @error @edge @us-04 @slice-04 @ac-04.5 @contract-shape:bounded-change
  Scenario: A Delivery retired late in the evening is recorded as retired that evening, not the day before
    Given an instance whose people work in a time zone well ahead of the one the server reasons in
    When the forecaster archives the Delivery at eleven at night, local time
    Then the Delivery is shown as archived on that day
    And it is not shown as archived on the previous day

  @edge @us-04 @slice-04 @ac-04.3 @contract-shape:unbounded-preservation
  Scenario: Deliveries that existed before this was possible are simply not retired
    Given a Portfolio carrying Deliveries created by an earlier release
    When the release that makes retiring possible starts against it
    Then every one of those Deliveries is still live
    And none of them carries a written record it never asked for
    And each still shows its numbers worked out fresh, exactly as before

  @us-04 @slice-04 @ac-04.9 @contract-shape:bounded-change
  Scenario: Deleting a Delivery still deletes it, retired or not
    Given one retired Delivery and one live Delivery in the Portfolio
    When the forecaster deletes each of them
    Then both are gone from the Portfolio
    And the retired one's written record and recorded days are gone with it
    And nothing about being retired made a Delivery harder to remove

  @real-io @adapter-integration @us-04 @slice-04 @migration @contract-shape:bounded-change
  Scenario: An instance upgraded to the release that brings retiring keeps everything it already had
    Given an instance running the previous release, carrying real Portfolios, Deliveries, notes and
      weeks of recorded history
    When it is upgraded to the release that makes retiring possible, on each kind of storage the
      product supports
    Then every Portfolio, Delivery, note and day of recorded history that was there is still there and
      reads exactly as it did
    And every Delivery can be retired, and none of them already is
    And nothing that already existed was removed, renamed or rewritten to make room

  # Retiring a Delivery is the capability this Epic sells. Writing notes is not — a free user can
  # still say what happened to a Delivery, which is the habit worth building. The way out is
  # deliberately not gated: a licence that lapses leaves archived Deliveries readable and reversible,
  # so nobody is trapped in a state they cannot leave.
  @error @us-04 @slice-04 @ac-04.1a @contract-shape:bounded-change
  Scenario: Retiring a Delivery needs a licence, and says so in the words already used elsewhere
    Given an instance with no premium licence
    When the forecaster opens a Delivery's header
    Then Archive is shown but cannot be used
    And hovering it gives the same upgrade wording the export actions give
    And asking the product to archive that Delivery directly is refused too
    And Edit and Delete are unaffected

  @us-04 @slice-04 @ac-04.1a @contract-shape:bounded-change
  Scenario: Writing about a Delivery needs no licence
    Given an instance with no premium licence
    When the lead opens a Delivery's Notes tab
    Then a note can be written, corrected and withdrawn exactly as on a licensed instance
    And nothing on that tab offers an upgrade

  # Only reachable after a licence lapses, because archiving needed one in the first place. The
  # refusal wording is knowingly left alone; un-archive and delete both resolve it.
  @edge @us-04 @slice-04 @ac-04.10 @contract-shape:unbounded-preservation
  Scenario: A Delivery archived under a licence still holds its slot once the licence has lapsed
    Given a Portfolio whose only Delivery was archived while the instance was licensed
    And the licence has since lapsed
    When the forecaster tries to create a new Delivery in that Portfolio
    Then it is refused, because the archived Delivery still counts
    And bringing the archived Delivery back lets them work on it again without a licence
    And deleting it outright also frees the slot
