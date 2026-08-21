Feature: What this epic promised not to do (Epic 5698 — the boundary as a test)
  As the maintainer of a product whose Deliveries are about to gain a lifecycle
  I want the things this epic said it would not touch to be asserted rather than assumed
  So that behaviour appearing after release can be attributed to this work or ruled out of it

  # Every scenario here is a promise this epic made by leaving something out. A promise kept only by
  # nobody having got round to it yet is not a promise, and the ones below are precisely the features a
  # reader would assume came along with archiving and notes.
  #
  # Two of them deserve saying out loud because they are counter-intuitive. Archiving is NOT protection
  # from deletion — a closed Delivery deletes as completely as an open one, and its written record and
  # its history go with it. And a closed Delivery still says what the forecast believed at closing but
  # has nowhere to record what actually happened, so nothing in this epic can tell you whether the
  # forecast was any good. That reading is the next epic, and it needs one column this one did not add.
  #
  # The baselines the regression scenarios compare against are gold values captured on the released
  # product, at a named commit, and committed as their own reviewed change BEFORE the first production
  # commit of this epic. That ordering is the difference between a gate and a tautology — a baseline
  # taken from the build under test asserts only that the build equals itself.

  @regression @slice-05 @contract-shape:unbounded-preservation
  Scenario: Nothing here says whether a forecast turned out to be right
    Given a Delivery archived while its forecast said one date
    When the forecaster reads everything the product will show about it
    Then nowhere does it record when the work actually finished
    And nowhere does it compare what was forecast with what happened
    And no reading of how well the forecast performed is offered anywhere in the product

  @regression @slice-04 @contract-shape:unbounded-preservation
  Scenario: Retiring or annotating a Delivery is invisible to the work tracking system
    Given the number of times a Portfolio refresh writes back to its work tracking system, captured on
      the released product and kept as the number to beat
    When a Delivery in it is archived, annotated and un-archived, and the Portfolio is refreshed
    Then the work tracking system is written to exactly as many times as the captured number
    And every value written is the same value the released product wrote
    And nothing about a Delivery being closed, or about anything written against it, leaves the product

  @regression @slice-04 @contract-shape:unbounded-preservation
  Scenario: A Portfolio cannot be retired — only a Delivery can
    When the forecaster looks for a way to archive a Portfolio
    Then there is none, in the interface or by asking the product directly
    And the only thing this work taught the product to retire is a Delivery

  @regression @slice-02 @contract-shape:unbounded-preservation
  Scenario: A note is text somebody typed and nothing else
    When a note is written containing formatting marks, a link and a person's name preceded by an at-sign
    Then it is displayed as the characters that were typed
    And nothing in it became bold, a link, or a mention of anybody
    And there is nowhere to attach a file to a note

  @regression @slice-02 @contract-shape:unbounded-preservation
  Scenario: Nothing writes a note by itself
    Given a Delivery whose likelihood falls from eighty-five per cent to sixty-one over a week
    When the Portfolio is refreshed each day of that week
    Then the Delivery's notes are exactly the ones people wrote
    And no note appeared that nobody typed

  @regression @slice-01 @contract-shape:unbounded-preservation
  Scenario: The history charts stay on the screen
    Given the instance is licensed for the premium export
    When the forecaster opens a Delivery's Metrics tab
    Then there is no way to export what it shows
    And the export added by this work covers the headline numbers and the Feature grid, and nothing else

  @regression @slice-05 @contract-shape:unbounded-preservation
  Scenario: A closed Delivery's numbers are never borrowed by another Delivery
    Given a Portfolio holding one archived Delivery and two live ones
    And the gold percentiles for the two live ones, captured on the released product at the tagged
      commit and checked in
    When the Portfolio is forecast
    Then each live Delivery's percentiles are identical to the gold set
    And nothing the archived Delivery holds took part in producing them

  @regression @kpi @slice-05 @contract-shape:unbounded-preservation
  Scenario: A Portfolio with nothing archived forecasts exactly as it did before
    Given a Portfolio in which no Delivery has ever been archived
    And the gold percentiles for it, captured on the released product at the tagged commit and checked in
    When it is forecast after every commit of this work
    Then every percentile for every Delivery is identical to the gold set
    And every Delivery's headline numbers are identical to the gold set

  @regression @slice-04 @contract-shape:unbounded-preservation
  Scenario: Nobody needs a new permission or a new licence for any of this
    Given the permissions the product recognised before this work
    When they are compared afterwards
    Then no permission has been added
    And the only thing gated by a licence is the export, by the gate that already gated exports
    And no new licensing idea has been introduced

  # The one people will get wrong. Archiving is the alternative to deleting, not a defence against it.
  @regression @slice-04 @contract-shape:bounded-change
  Scenario: Retiring a Delivery does not make it harder to destroy
    Given an archived Delivery with a written record and weeks of recorded history
    When the forecaster deletes it
    Then it is deleted, with no extra confirmation beyond the one a live Delivery gets
    And its written record and its recorded history are gone with it
    And nothing in the product claimed archiving would have prevented that

  @regression @architecture @slice-04 @contract-shape:unbounded-preservation
  Scenario: Retiring a Delivery announces nothing to the rest of the product
    Given everything the product announces to itself when a Portfolio changes
    When a Delivery is archived and then un-archived
    Then nothing new is announced
    And no part of the product learns that a Delivery closed by being told about it

  @regression @slice-05 @contract-shape:unbounded-preservation
  Scenario: The daily history of a Delivery is read the same way whether it is closed or not
    Given a Delivery with three weeks of recorded history
    And the history the product returns for it, captured before it is archived
    When it is archived
    And its history is asked for again
    Then it is the same three weeks, value for value
    And asking for it did not change, and did not need to know, whether the Delivery was closed

  @regression @slice-04 @contract-shape:unbounded-preservation
  Scenario: Deliveries are retired and taken away one at a time
    Given a Portfolio holding six finished Deliveries
    When the forecaster looks for a way to retire them together, or to export them together
    Then there is none
    And each is archived, and each is exported, on its own

  # The one out-of-scope promise that is easiest to drift into, because a note now carries an author
  # and an owner field is one step from an author field. Naming somebody responsible for a Delivery,
  # and telling them things, is a separate piece of work with its own questions.
  @regression @slice-04 @contract-shape:unbounded-preservation
  Scenario: A Delivery still belongs to nobody in particular
    Given a Delivery carrying several notes, each attributed to whoever wrote it
    When the forecaster looks for a way to say who owns the Delivery, or who should hear about it
    Then there is none
    And archiving it, un-archiving it, or writing a note on it tells nobody anything
    And the only person named anywhere on a Delivery is the author of a note
