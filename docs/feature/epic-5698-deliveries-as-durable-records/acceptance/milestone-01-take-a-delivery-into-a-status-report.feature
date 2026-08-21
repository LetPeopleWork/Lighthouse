Feature: Take a Delivery into a status report in one paste (Epic 5698, Slice 01 — US-01)
  As a Delivery Forecaster building a status report
  I want the Delivery's headline numbers and its Feature grid as one file or one paste
  So that the report is a paste rather than a retyping exercise that can silently change a number

  # The Work Items dialog exports today. This grid — the one people actually ask about in a status
  # meeting — does not, because the component that renders it never forwards the prop that turns the
  # export on. So the first thing this slice does is not a feature at all; it is a forwarded prop, and
  # the first scenario below is the only evidence anyone will ever have that it was forwarded.
  #
  # The artifact is deliberately ONE thing: nine header rows, a blank row, then the grid. Two files, or
  # a header on the clipboard and none in the file, would leave the person doing the pasting to
  # reassemble it — which is the transcription step this slice exists to remove.
  #
  # The export reads the grid as rendered, so a hidden column is genuinely absent rather than blanked,
  # and the sort the reader chose is the order they get. That is a decision with a sharp edge: it also
  # means a row scrolled out of view must still be exported, and there is a scenario for that below
  # because "what is on screen" and "what is in the grid" are two different things in a virtualised
  # grid and only one of them is correct.
  #
  # Pre-requisite for every scenario except the premium-gate one: a licence fixture that is gitignored
  # and absent from a fresh checkout. Import it from a licensed checkout before running this file.

  Background:
    Given a Portfolio holding a Delivery

  @driving_adapter @us-01 @slice-01 @ac-01.1 @contract-shape:bounded-change
  Scenario: The Delivery's Feature grid offers the same two ways out as the Work Items list already does
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    When the forecaster expands the Delivery
    Then the Feature grid's toolbar offers Copy to Clipboard and Export to CSV
    And they are the same two actions, with the same wording, as the ones on the Work Items list

  # The gate is the one that already exists. A second premium concept for the same act of exporting
  # would be a second thing to reason about at licence-renewal time.
  @error @driving_adapter @us-01 @slice-01 @ac-01.2 @contract-shape:bounded-change
  Scenario: Without a premium licence both ways out are offered but refused, in the words already used
    Given the instance has no premium licence
    When the forecaster expands the Delivery
    Then Copy to Clipboard and Export to CSV are shown and cannot be used
    And each explains itself with the premium wording the product already uses elsewhere
    And no new licensing idea is introduced to say it

  @driving_adapter @us-01 @slice-01 @ac-01.3 @contract-shape:bounded-change
  Scenario: Exporting to a file produces the headline block, a blank line, then the Feature grid
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    When the forecaster exports the Delivery's Feature grid to a file
    Then the file opens with nine labelled values: the Delivery's name, its date, the three forecast
      dates, the likelihood, and the total, completed and remaining Work Items
    And those nine are in that order
    And one blank line follows them
    And the Feature grid's own column headings follow the blank line
    And every Feature row follows the headings

  @driving_adapter @us-01 @slice-01 @ac-01.4 @contract-shape:bounded-change
  Scenario: Copying to the clipboard lands in cells in a spreadsheet and as a table in a document
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    When the forecaster copies the Delivery's Feature grid to the clipboard
    And pastes it into a spreadsheet
    Then the headline values land one per row, the blank line separates them from the grid, and each
      Feature lands one per row with its values in their own cells
    When the forecaster pastes the same clipboard content into a document
    Then it arrives as a table rather than as a run of text
    And the content is the same in both, so a reader cannot tell which paste produced which

  @us-01 @slice-01 @ac-01.5 @contract-shape:bounded-change
  Scenario: What the forecaster chose to look at is what the forecaster takes away
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    And the forecaster has hidden one column, moved another to the front and sorted by a third
    When the forecaster exports the Delivery's Feature grid
    Then the hidden column is absent from the export rather than present and empty
    And the remaining columns appear in the order they appear on screen
    And the Feature rows are in the order the chosen sort put them in

  # A virtualised grid renders a window, not a list. Exporting what is painted would silently truncate
  # exactly the long Deliveries a status report is written about.
  @edge @us-01 @slice-01 @ac-01.3 @ac-01.5 @contract-shape:bounded-change
  Scenario: A Delivery with more Features than fit on screen exports all of them
    Given the instance is licensed for the premium export
    And the Delivery has more Features than the grid shows at once
    When the forecaster exports the Delivery's Feature grid without scrolling
    Then every Feature in the Delivery appears in the export
    And their order is the order the grid would show if it could show them all at once

  # A fabricated zero is worse than a blank, because a blank reads as "we do not know" and a zero reads
  # as an answer. This is the one the ledger has seen ship before.
  @error @us-01 @slice-01 @ac-01.6 @contract-shape:bounded-change
  Scenario: A Delivery that cannot be forecast exports blanks, not a number nobody computed
    Given the instance is licensed for the premium export
    And the Delivery has no forecast, because there is not enough delivery history behind it
    When the forecaster exports it
    Then the likelihood and the three forecast values are present as labels with nothing after them
    And none of them reads as a zero, as a dash-placeholder, or as any word the product uses for an
      absent value internally

  @error @us-01 @slice-01 @ac-01.7 @contract-shape:pure-function
  Scenario: A Delivery whose name contains a comma, a quote or a line break survives the round trip
    Given the instance is licensed for the premium export
    And the Delivery is named 'Q3 "Platform", phase one' followed by a line break and 'and two'
    When the forecaster exports it to a file and opens the file in a spreadsheet
    Then the name reads exactly as it does in Lighthouse, punctuation and line break included
    And it occupies one cell, not several
    And no following row has been shifted into the wrong column
    And the same holds for a Feature name carrying the same punctuation

  # The tenant that renamed Delivery to Milestone gets a file about Milestones. A header block that
  # hard-codes the seeded words would be the one place in the product that ignores the rename.
  @us-01 @slice-01 @ac-01.8 @contract-shape:bounded-change
  Scenario: The headline labels are the words this tenant uses, not the words the product ships with
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    And the tenant has renamed Delivery to Milestone and Work Item to Ticket
    When the forecaster exports the Delivery's Feature grid
    Then the headline labels read Milestone and Milestone Date, and Total, Completed and Remaining
      Tickets
    And no label in the file uses a word the tenant has renamed away from

  @edge @us-01 @slice-01 @ac-01.3 @contract-shape:bounded-change
  Scenario: A Delivery with no Features yet still exports its headline
    Given the instance is licensed for the premium export
    And the Delivery has no Features
    When the forecaster exports it
    Then the nine headline values are present
    And the blank line is present
    And the Feature grid's column headings are present with no rows beneath them
    And the file is not empty and does not fail to be produced
