Feature: Take a Delivery into a status report in one paste (Epic 5698, Slice 01 — US-01)
  As a Delivery Forecaster building a status report
  I want the Delivery and its Features as one table, in one file or one paste
  So that the report is a paste rather than a retyping exercise that can silently change a number

  # The Work Items dialog exports today. This grid — the one people actually ask about in a status
  # meeting — does not, because the component that renders it never forwards the prop that turns the
  # export on. So the first thing this slice does is not a feature at all; it is a forwarded prop, and
  # the first scenario below is the only evidence anyone will ever have that it was forwarded.
  #
  # The artifact is deliberately ONE table: headings, the Delivery as the first data row, then its
  # Features. Anything that separates the Delivery's numbers from the Features' — a block above the
  # grid, a second file, a header on the clipboard and none in the file — leaves the person doing the
  # pasting to reassemble it, which is the transcription step this slice exists to remove.
  #
  # The column set is settled rather than copied from the screen, because the file is compared against
  # last week's file by someone who never saw either grid. What the reader chose still decides the
  # rows: their sort is the order they get, and a filtered-out Feature stays out. That has a sharp
  # edge — a row scrolled out of view must still be exported, and there is a scenario for it below,
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

  # One table, not a headline block above a grid. A block cannot be sorted, cannot be filtered, and
  # puts the Delivery's own numbers in different columns from the Features' — so the two things a
  # reader most wants to compare are the two the file will not line up.
  @driving_adapter @us-01 @slice-01 @ac-01.3 @contract-shape:bounded-change
  Scenario: Exporting to a file produces one table with the Delivery as its first row
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    When the forecaster exports the Delivery's Feature grid to a file
    Then the file opens with a single row of column headings
    And the Delivery is the first data row beneath them, named for itself and marked as the Delivery
    And the Delivery's progress, its four forecast dates and its likelihood sit in the same columns
      the Features use for theirs
    And every Feature follows as one row apiece
    And no separate block of labelled values appears anywhere in the file

  @driving_adapter @us-01 @slice-01 @ac-01.4 @contract-shape:bounded-change
  Scenario: Copying to the clipboard lands in cells in a spreadsheet and as a table in a document
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    When the forecaster copies the Delivery's Feature grid to the clipboard
    And pastes it into a spreadsheet
    Then the headings land in one row of cells, the Delivery lands in the row beneath them, and each
      Feature lands one per row with its values in their own cells
    When the forecaster pastes the same clipboard content into a document
    Then it arrives as a table rather than as a run of text
    And the content is the same in both, so a reader cannot tell which paste produced which

  # The file is a status report, not a picture of the screen. Two people looking at the same Delivery
  # through differently arranged grids must produce the same document, or a reader comparing this
  # week's against last week's is comparing two layouts as much as two Deliveries.
  @us-01 @slice-01 @ac-01.5 @contract-shape:bounded-change
  Scenario: The columns are settled, but the forecaster's sort and filter still decide the rows
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    And the forecaster has hidden one column, moved another to the front and sorted by a third
    When the forecaster exports the Delivery's Feature grid
    Then the file carries every column of the settled set, in the settled order
    And hiding a column on screen has changed nothing about the file
    And the Feature rows are in the order the chosen sort put them in
    And a Feature the grid is filtering out is absent from the file too

  # A virtualised grid renders a window, not a list. Exporting what is painted would silently truncate
  # exactly the long Deliveries a status report is written about.
  @edge @us-01 @slice-01 @ac-01.3 @ac-01.5 @contract-shape:bounded-change
  Scenario: A Delivery with more Features than fit on screen exports all of them
    Given the instance is licensed for the premium export
    And the Delivery has more Features than the grid shows at once
    When the forecaster exports the Delivery's Feature grid without scrolling
    Then every Feature in the Delivery appears in the export
    And their order is the order the grid would show if it could show them all at once

  # Half of what a reader looks at is drawn rather than stored — the progress bar, the team links, the
  # chance-of-landing chip, the warning icon. A file built by reading the stored values back out gets
  # a raw list where a date belongs, a count meant for sorting where a name belongs, and nothing at
  # all where the cell was drawn from something the row does not itself hold.
  @us-01 @slice-01 @ac-01.3 @contract-shape:bounded-change
  Scenario: A cell that is drawn on screen exports what it says, not what it is made of
    Given the instance is licensed for the premium export
    And a Feature in the Delivery has a forecast, a team, a chance of landing and a dependency
    When the forecaster exports the Delivery's Feature grid
    Then each of the Feature's four forecast cells holds one date, written the way dates are written
      everywhere else in the file
    And its chance of landing reads as it reads on screen, rather than being absent
    And its dependency is named, rather than counted
    And its warning column answers yes or no, so a reader can filter the file down to the rows that
      need them

  # A fabricated zero is worse than a blank, because a blank reads as "we do not know" and a zero reads
  # as an answer. This is the one the ledger has seen ship before.
  @error @us-01 @slice-01 @ac-01.6 @contract-shape:bounded-change
  Scenario: A Delivery that cannot be forecast exports blanks, not a number nobody computed
    Given the instance is licensed for the premium export
    And the Delivery has no forecast, because there is not enough delivery history behind it
    When the forecaster exports it
    Then the Delivery's likelihood cell and its four forecast cells are empty
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

  # The tenant that renamed Delivery to Milestone gets a file about Milestones. A file that hard-codes
  # the seeded words would be the one place in the product that ignores the rename.
  @us-01 @slice-01 @ac-01.8 @contract-shape:bounded-change
  Scenario: The file reads in the words this tenant uses, not the words the product ships with
    Given the instance is licensed for the premium export
    And the Delivery has several Features and a forecast
    And the tenant has renamed Delivery to Milestone and Feature to Epic
    When the forecaster exports the Delivery's Feature grid
    Then the first data row names the Delivery and marks it as a Milestone
    And a dependency the forecaster may not open is described as an Epic they do not have access to
    And no text in the file uses a word the tenant has renamed away from

  @edge @us-01 @slice-01 @ac-01.3 @contract-shape:bounded-change
  Scenario: A Delivery with no Features yet still exports itself
    Given the instance is licensed for the premium export
    And the Delivery has no Features
    When the forecaster exports it
    Then the column headings are present
    And the Delivery is there as the single data row, carrying everything known about it
    And the file is not empty and does not fail to be produced
