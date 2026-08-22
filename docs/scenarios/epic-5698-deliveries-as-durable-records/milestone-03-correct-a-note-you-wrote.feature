Feature: Correct or withdraw a note you wrote (Epic 5698, Slice 03 — US-03)
  As the person who wrote a note
  I want to fix a typo or take back a line I got wrong
  So that the record does not carry a mistake I can see and cannot touch

  # This slice is one rule, and the rule is a trap. "May I change this note?" invites the obvious
  # comparison — the note's author against the person asking — and the obvious comparison is wrong on
  # an instance where some notes have no author and some callers have no identity. Comparing nothing to
  # nothing succeeds, and it succeeds silently: it hands a caller with no identity the right to rewrite
  # somebody else's signed note, and it reads as correct code to everyone who reviews it.
  #
  # So the rule is written as two named cases, and both of them get their own scenario here — the one
  # where a note has an author and the one where it has none. The scenario that actually catches the
  # trap is the third: a signed note, a caller with no identity, refused. If that scenario is ever
  # deleted as redundant, the bug returns and nothing else in the suite notices.
  #
  # The affordances on screen are a courtesy, not the rule. Every scenario about who may change a note
  # is asserted against the product directly as well, because a hidden button has never stopped
  # anybody.

  Background:
    Given a Portfolio holding a live Delivery
    And "Anoop Kumar" wrote a note against it reading "Slipped a week — waiting on the vendor"

  @driving_adapter @us-03 @slice-03 @ac-03.1 @contract-shape:bounded-change
  Scenario: The person who wrote a note is offered a way to fix it
    Given "Anoop Kumar" is signed in
    When Anoop opens the Notes tab
    Then the note Anoop wrote offers a way to correct it and a way to withdraw it

  @error @driving_adapter @us-03 @slice-03 @ac-03.1 @ac-03.2 @contract-shape:unbounded-preservation
  Scenario: Somebody else's note offers no way to change it, and refuses if asked anyway
    Given "Chris Miller" is signed in and may change this Portfolio
    When Chris opens the Notes tab
    Then Anoop's note is listed with no way to correct or withdraw it
    When Chris asks the product directly to change Anoop's note
    Then the request is refused as beyond their rights
    And Anoop's note reads exactly as it did

  # The scenario the whole slice exists for. Nothing else in the suite fails if the comparison collapses
  # into "nothing equals nothing".
  @error @us-03 @slice-03 @ac-03.2 @contract-shape:unbounded-preservation
  Scenario: A caller with no identity cannot rewrite a note that somebody signed
    Given a caller who may change this Portfolio and has no identity of their own
    When that caller asks the product to change Anoop's signed note
    Then the request is refused as beyond their rights
    And the same caller is refused when asking to withdraw it
    And Anoop's note reads exactly as it did

  @us-03 @slice-03 @ac-03.5 @contract-shape:bounded-change
  Scenario: A note nobody signed may be corrected by anybody who may change the Portfolio
    Given a note against the Delivery that carries no author
    And "Chris Miller" is signed in and may change this Portfolio
    When Chris opens the Notes tab
    Then the unsigned note offers a way to correct it and a way to withdraw it
    When Chris corrects it
    Then the corrected text is listed
    And the note still carries no author

  @us-03 @slice-03 @ac-03.3 @contract-shape:bounded-change
  Scenario: A corrected note says it was corrected, and still says when it was first written
    Given "Anoop Kumar" is signed in
    When Anoop corrects the note to read "Slipped two weeks — waiting on the vendor"
    Then the note reads as corrected
    And it is marked as having been changed, with the day it was changed
    And the day it was first written is still shown, unchanged
    And it is still signed "Anoop Kumar"

  # Ordering is by when a note was written. A correction that jumped a six-week-old note to the top of
  # the list would rewrite the sequence of events the list exists to record.
  @us-03 @slice-03 @ac-03.3 @contract-shape:unbounded-preservation
  Scenario: Correcting an old note does not move it to the top of the list
    Given three notes written on three different days, the one from six weeks ago at the bottom
    When the author corrects the six-week-old one
    Then it is still at the bottom of the list
    And the other two have not moved

  @driving_adapter @us-03 @slice-03 @ac-03.4 @contract-shape:bounded-change
  Scenario: A withdrawn note is gone at once and does not come back
    Given "Anoop Kumar" is signed in
    When Anoop withdraws the note
    Then it disappears from the list without the page being reloaded
    When the lead reloads the Delivery
    Then the note is not there
    And the other notes on the Delivery are untouched

  @edge @us-03 @slice-03 @ac-03.5 @contract-shape:bounded-change
  Scenario: With nobody signed in, anybody who may change the Portfolio may correct any note
    Given the instance runs with authentication switched off
    And two notes were written against the Delivery, both unsigned
    When someone with rights to change the Portfolio opens the Notes tab
    Then both notes offer a way to correct them and a way to withdraw them
    And correcting one succeeds

  @error @us-03 @slice-03 @ac-03.6 @contract-shape:unbounded-preservation
  Scenario: A correction that empties a note is refused, and the note is left as it was
    Given "Anoop Kumar" is signed in
    When Anoop tries to save the note with nothing in it
    Then it is refused with a message on the field
    When the product is asked directly to save the note as whitespace alone
    Then it is refused with a reason naming the field
    And the note still reads "Slipped a week — waiting on the vendor"
    And it is not marked as having been changed

  # A note belongs to one Delivery. Addressing it through a different one is either a mistake or an
  # attempt, and neither should work.
  @error @us-03 @slice-03 @ac-03.2 @contract-shape:unbounded-preservation
  Scenario: A note cannot be reached through a Delivery it does not belong to
    Given a second Delivery in the same Portfolio
    And "Anoop Kumar" is signed in
    When Anoop asks to change the note as though it belonged to the second Delivery
    Then the request is refused
    And the note reads exactly as it did
    And the same holds for withdrawing it
