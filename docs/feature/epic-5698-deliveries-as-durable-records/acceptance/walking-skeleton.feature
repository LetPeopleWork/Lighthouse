Feature: A finished Delivery that still reads the same two months later (walking skeleton — Epic 5698, Slices 01/04/05)
  As a Delivery Forecaster preparing a quarterly review
  I want a Delivery I closed to show what it said on the day I closed it
  So that I can cite a past commitment instead of citing what today's data would say about it

  # Walking skeleton: the one scenario that closes the whole loop through the running product — a
  # Delivery closed by hand, a Portfolio that then genuinely moves on underneath it, and the same
  # Delivery read back and taken out to a report.
  #
  # DISCUSS declared Strategy B and no walking skeleton, on the grounds that every surface this Epic
  # touches already exists end to end. That is true of every surface and false of the loop. Closing a
  # Delivery, refreshing the Portfolio past it, and reading the frozen record is a path that has never
  # run in either direction, and it is the only path that can falsify the bet the whole Epic rests on:
  # that a Delivery can stop being recomputed and still have something to show. DISCUSS itself calls
  # that assertion "the whole Epic in one assertion". So a skeleton is authored for the loop, not for
  # the surfaces — everything the surfaces do on their own stays in the milestone files.
  #
  # The refresh in the middle is not decoration and must genuinely change the underlying data. A
  # refresh that leaves the Features alone proves that a read is repeatable, which nobody doubted.
  # Only a refresh that removes a Feature the closed Delivery counted, and moves another one's
  # remaining Work Items, can tell a frozen record apart from a live recomputation that happens to
  # agree today.
  #
  # The export at the end is in the skeleton rather than in a milestone of its own because it is what
  # makes the frozen numbers leave the machine. A record nobody can get out of the tool is not
  # evidence in a review; it is a screen.
  #
  # Pre-requisite: a premium licence. The export half of this scenario cannot run without one, and the
  # licence fixture is gitignored and absent from a fresh checkout.
  #
  # Litmus test: a Delivery Forecaster reads this scenario and confirms "yes, that is what I need".

  @walking_skeleton @real-io @driving_adapter @us-01 @us-04 @us-05 @slice-01 @slice-04 @slice-05
  @ac-01.3 @ac-04.3 @ac-04.5 @ac-05.1 @ac-05.2 @ac-05.3 @ac-05.4 @contract-shape:unbounded-preservation
  Scenario: A closed Delivery reads the same after the Portfolio it belonged to has moved on
    Given a Portfolio holding a Delivery whose Features are still being worked
    And the forecaster notes that Delivery's likelihood, its three forecast dates and every row of
      its Feature grid
    And the instance is licensed for the premium export
    When the forecaster archives that Delivery and confirms
    And the Portfolio is refreshed against a work tracking system in which one of that Delivery's
      Features has been removed and another's remaining Work Items have changed
    And the forecaster reopens the archived Delivery
    Then its likelihood, its three forecast dates and every row of its Feature grid are exactly what
      the forecaster noted
    And the section says it was archived, and on which day
    And exporting it produces those same numbers, the header block first and then the Feature grid
    And the Portfolio's list of live commitments no longer offers it as one
