Feature: Read a closed Delivery as the record it was (Epic 5698, Slice 05 — US-05)
  As a Delivery Forecaster preparing a quarterly review
  I want a Delivery closed two months ago to show exactly what it looked like at closing
  So that I am reporting what happened rather than what today's data would say about it

  # This is the slice the Epic is for. Everything before it writes the record down; this one is the
  # promise that reading it gives you the same thing every time.
  #
  # The central scenario is read, refresh, read again, and demand the two reads be identical — with a
  # refresh in the middle that genuinely changes the Features underneath. A refresh that changes
  # nothing proves that a function is deterministic, which nobody doubted; only a refresh that removes
  # a Feature and moves another's numbers can tell a written-down answer apart from a live calculation
  # that happens to agree today.
  #
  # A closed Delivery also stops accepting changes, and this file asserts the refusal from several
  # directions rather than one, because a rule enforced at one entrance is a rule with as many holes as
  # there are other entrances. What it must NOT refuse is deleting and re-opening — the first because
  # closing a Delivery was never meant to make it permanent, the second because closing it was always
  # meant to be undoable.
  #
  # The last decision DESIGN handed DISTILL is settled here and awaits confirmation: the Metrics tab
  # stays reachable on a closed Delivery, read-only, ending on the day it was closed. The trend that
  # led to the number is the most useful thing in a review about that number, the daily history is
  # untouched by closing, and the closed Delivery already carries how many days of it exist. Hiding the
  # tab would throw away the only part of a closed Delivery that shows movement.

  Background:
    Given a Portfolio holding a Delivery that was archived, with its headline numbers and Feature rows
      noted at that moment

  @driving_adapter @us-05 @slice-05 @ac-05.1 @contract-shape:bounded-change
  Scenario: A closed Delivery shows the Feature grid that was written down, not one worked out today
    When the forecaster expands the archived Delivery
    Then its Feature grid lists exactly the Feature rows that were noted
    And each row's totals are the ones that were noted
    And no Feature that has joined the Portfolio since appears in it
    And its columns are the ones that were written down — each Feature's name, its reference, how far
      along it was, its total Work Items and its own likelihood
    And it shows no work item state, no type, no owning Team, no per-Team remaining or total, no
      per-Feature forecast date and no blocked marking, because the record never held them
    And no row offers a way through to the Feature as it stands today, which is deliberate: the
      Feature may have been renamed, re-Teamed or deleted since

  @driving_adapter @us-05 @slice-05 @ac-05.2 @contract-shape:bounded-change
  Scenario: A closed Delivery says so, and says when
    When the forecaster expands the archived Delivery
    Then the section is marked as archived
    And it shows the day it was archived
    And a live Delivery beside it carries no such marking

  # The whole Epic in one assertion.
  @kpi @us-05 @slice-05 @ac-05.3 @contract-shape:unbounded-preservation
  Scenario: A closed Delivery reads identically either side of a refresh that changes its Features
    When the forecaster reads the archived Delivery's headline numbers and every row of its grid
    And the Portfolio is refreshed against a work tracking system in which one of that Delivery's
      Features has been removed, another has been renamed and a third's remaining Work Items have
      changed
    And the forecaster reads it again
    Then the second reading is identical to the first, value for value and row for row
    And repeating the refresh three more times changes neither reading

  # A Delivery closed while it could not be forecast is the case that quietly rewrites itself if the
  # record does not carry why. "Enough history to forecast" defaults to yes when nothing is stored,
  # so an absent value does not read as unknown — it reads as confident, and names no Teams. These
  # two scenarios exist because that failure looks like a working feature.
  @edge @us-05 @slice-05 @ac-05.1 @contract-shape:unbounded-preservation
  Scenario: A Delivery closed while it could not be forecast still says why, and still names the Teams
    Given a Delivery whose forecast is unavailable because two of its Teams have no delivery history
    And the forecaster notes what the Delivery says about itself and which Teams it names
    When the Delivery is archived
    And the forecaster reads it again a month later
    Then it still says its forecast is unavailable for want of history, not that it cannot be forecast
    And it still names the same two Teams
    And that remains true after a refresh in which both Teams have since built up history

  @edge @us-05 @slice-05 @ac-05.1 @contract-shape:unbounded-preservation
  Scenario: A closed rule-based Delivery still shows the rule it was built from
    Given a Delivery whose Features are chosen by a rule rather than picked by hand
    When the Delivery is archived
    Then it still shows that its Features were chosen by a rule, and which rule
    And that is still true after the Portfolio's Features have changed underneath it

  @driving_adapter @us-05 @slice-05 @ac-05.4 @contract-shape:bounded-change
  Scenario: Taking a closed Delivery into a report gives the numbers that were written down
    Given the instance is licensed for the premium export
    When the forecaster exports the archived Delivery's Feature grid
    Then the headline block holds the numbers that were noted at closing
    And the Feature rows are the ones that were noted
    And it is the same action, in the same place, as on a live Delivery
    And the exported columns are the narrower set an archived grid shows, not the wider set a live
      Delivery exports

  @us-05 @slice-05 @ac-05.5 @contract-shape:bounded-change
  Scenario: The notes on a closed Delivery are still there to read
    Given three notes were written against the Delivery before it was archived
    When the forecaster opens its Notes tab
    Then all three are listed, newest first, with their dates and their authors
    And there is nowhere to type a new one
    And none of them offers a way to correct or withdraw it

  @error @driving_adapter @us-05 @slice-05 @ac-05.5 @contract-shape:unbounded-preservation
  Scenario: A note cannot be added to a closed Delivery, however it is asked for
    When somebody who may change the Portfolio asks the product to add a note to the archived Delivery
    Then the request is refused because the Delivery is closed, not because of who asked
    And the reason given is one the interface can show to the person who tried
    And no note has been stored

  @error @us-05 @slice-05 @ac-05.5 @contract-shape:unbounded-preservation
  Scenario: The notes already on a closed Delivery cannot be corrected or withdrawn either
    Given "Anoop Kumar" wrote one of the notes and is signed in
    When Anoop asks to correct that note on the archived Delivery
    Then the request is refused because the Delivery is closed
    When Anoop asks to withdraw it
    Then that is refused for the same reason
    And all three notes read exactly as they did

  # Refused because of the Delivery's state, not because of the caller's rights — the same person
  # succeeds the moment it is re-opened. Told apart deliberately, because an interface that says "you
  # are not allowed" when it means "not while this is closed" sends somebody to ask for permissions
  # they already have.
  @error @us-05 @slice-05 @ac-05.5 @ac-05.8 @contract-shape:unbounded-preservation
  Scenario: Being refused for a closed Delivery reads differently from being refused for lack of rights
    Given a forecaster who may change this Portfolio
    When that forecaster is refused a change to the archived Delivery
    Then the refusal identifies the Delivery's state as the reason
    And it is distinguishable from the refusal a reader without rights receives
    When the Delivery is un-archived and the same change is made again
    Then it succeeds

  @error @driving_adapter @us-05 @slice-05 @ac-05.8 @contract-shape:unbounded-preservation
  Scenario: A closed Delivery's name, date, Features and rule cannot be changed
    Given a forecaster who may change this Portfolio
    When the forecaster asks to rename the archived Delivery
    Then it is refused because the Delivery is closed
    When the forecaster asks to move its date
    Then it is refused for the same reason
    When the forecaster asks to change which Features it holds
    Then it is refused for the same reason
    When the forecaster asks to change the rule it picks Features by
    Then it is refused for the same reason
    And the Delivery's name, date, Features and rule are exactly as they were

  @driving_adapter @us-05 @slice-05 @ac-05.6 @contract-shape:bounded-change
  Scenario: A Delivery closed too early can be brought back, and starts moving again
    When the forecaster un-archives the Delivery
    Then it returns to the Portfolio's live commitments
    And it is no longer marked as archived
    And its numbers are worked out fresh again from the Features it holds
    And its Notes tab accepts a new note
    And the next Portfolio refresh records a day of history for it again
    And its rule, if it has one, picks its Features again

  @edge @us-05 @slice-05 @ac-05.7 @contract-shape:bounded-change
  Scenario: Closing, re-opening and closing again on the same day leaves one written record, the newest
    Given the Delivery was archived this morning
    When the forecaster un-archives it
    And two Features are added to it
    And the forecaster archives it again the same afternoon
    Then exactly one written record is held for it
    And that record includes the two Features added this afternoon
    And the record written this morning is not also still held

  @edge @us-05 @slice-05 @ac-05.6 @contract-shape:bounded-change
  Scenario: Bringing back a Delivery whose Features have all vanished gives an empty live Delivery, not an error
    Given every Feature the Delivery held has been removed from the Portfolio while it was archived
    When the forecaster un-archives it
    Then it returns to the live commitments with no Features and no forecast
    And nothing fails while it is opened or listed
    And the written record it carried is still there for the next time it is closed

  @edge @us-05 @slice-05 @ac-05.6 @ac-05.7 @contract-shape:bounded-change
  Scenario: Closing a Delivery blocks changes to it without blocking the two things it was never meant to block
    Given the Delivery is archived
    When the forecaster un-archives it
    Then that succeeds
    When the forecaster archives it again and then deletes it
    Then that succeeds too
    And neither was refused on the grounds that the Delivery was closed

  # The daily history is untouched by closing, and the trend that led to the frozen number is the most
  # useful thing in a review about that number.
  @us-05 @slice-05 @ac-05.9 @contract-shape:bounded-change
  Scenario: The history behind a closed Delivery is still there to look at, and stops on the closing day
    Given the Delivery had eleven days of recorded history when it was archived
    When the forecaster opens its Metrics tab
    Then the tab can be opened
    And it shows the eleven days, ending on the day the Delivery was archived
    When the Portfolio is refreshed three more times
    And the forecaster opens the Metrics tab again
    Then it still shows eleven days, still ending on that day

  @edge @us-05 @slice-05 @ac-05.9 @contract-shape:bounded-change
  Scenario: A Delivery closed before it had enough history has the same empty Metrics tab a live one would
    Given a Delivery archived with two days of recorded history behind it
    When the forecaster expands it
    Then the Metrics tab is unavailable, for the same reason and with the same wording a live Delivery
      with two days would give
    And its Feature grid and headline numbers are shown in full regardless

  # The guarantee is that the closed read CANNOT reach live data, not that it currently does not.
  @architecture @us-05 @slice-05 @ac-05.1 @contract-shape:unbounded-preservation
  Scenario: The code that builds a closed Delivery's view has no way to reach a live Feature
    Given the code that builds a closed Delivery's view from its written record
    When what it is able to reach is examined
    Then it cannot reach a Feature, a Delivery, a blackout period or anything that produces a forecast
    And a change that gave it any of them is reported rather than passing quietly

  @us-05 @slice-05 @ac-05.6 @contract-shape:bounded-change
  Scenario: Bringing a closed Delivery back does not need a licence
    Given a Delivery archived while the instance was licensed
    And the licence has since lapsed
    When the forecaster brings it back
    Then it returns to the active list and starts moving again
    And it cannot be archived again until the instance is licensed once more
