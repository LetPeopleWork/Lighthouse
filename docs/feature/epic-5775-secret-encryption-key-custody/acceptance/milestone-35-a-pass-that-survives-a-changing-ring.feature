Feature: A pass that survives the keys changing under it (Epic 5775, Slice 09 — US-09)
  As the administrator moving every stored credential onto the key in force
  I want a pass to either finish against the keys it started on or tell me it did not
  So that I am never told a rotation succeeded while a credential was quietly left behind

  # A pass reads which key is in force once, uses it to decide what is left to do and to label what it
  # reports, and then works through the list. Every individual write asks again which key is in force,
  # because that is the only thing it can ask. So the pass holds two opinions about the same fact and
  # has no way to notice that they have come apart.
  #
  # An operator replacing a mounted keys file while an administrator is moving credentials is not a
  # strange thing to do - it is the pair of actions this feature invites. Credentials written before the
  # replacement land on one key, credentials written after it land on another, and what is reported
  # names a single key that is no longer the one in force.
  #
  # The part that costs credentials is the deciding, not the labelling. Credentials already sitting on
  # the key that was in force were taken off the list before the replacement happened, so the pass never
  # looks at them again. If the operator's new file no longer carries that key, those credentials cannot
  # be read by anything - and they are not named, because naming them means having walked past them.
  # The administrator is told how many moved and that the rotation is done. The ones that did not move
  # are the ones they find out about when a work tracking system stops updating.

  @error @driving_port @us-09 @slice-09
  Scenario: Every credential one pass moved is on one key
    Given several stored credentials waiting to be moved onto the key in force
    When the key in force is replaced while the pass is running
    Then every credential that pass moved is under the same key as every other one it moved
    And the key it names as the one it moved them onto is that key

  @error @driving_port @us-09 @slice-09
  Scenario: A pass whose keys changed under it does not report a rotation that finished
    Given a pass moving stored credentials onto the key in force
    When the key in force is replaced while the pass is running
    Then what the administrator is told does not present the move as finished
    And it says the keys changed while the pass was running
    And it says the pass has to be run again

  @error @driving_port @us-09 @slice-09
  Scenario: A credential the pass never looked at, on a key that has gone, is still named
    Given stored credentials already sitting on the key in force when a pass starts
    When the keys are replaced mid-pass by a set that no longer carries the key those credentials are on
    Then those credentials are named as ones nobody can read
    And the count of credentials nobody can read includes them

  @driving_port @us-09 @slice-09
  Scenario: A pass whose keys held still says nothing new about them
    Given stored credentials waiting to be moved onto the key in force
    When the pass runs from start to finish without the keys changing
    Then what the administrator is told is what a finished move has always said
    And it says nothing about the keys having changed

  @edge @driving_port @us-09 @slice-09
  Scenario: A check that only looks says which keys it read against
    Given an instance holding stored credentials
    When a check that only looks runs while the keys are replaced under it
    Then the key it names is one it actually read those credentials against
    And it says the keys changed while it was looking

  @driving_adapter @us-09 @slice-09
  Scenario: What an administrator sees after a pass that was disturbed
    Given an administrator who has started moving every stored credential onto the key in force
    When the keys are replaced before that finishes
    Then the encryption settings do not show the move as completed
    And they show that the keys changed and that it has to be run again

  @edge @driving_port @us-09 @slice-09
  Scenario: Running it again, as it asked, finishes it
    Given a pass that reported the keys changed while it was running
    When the administrator runs it again against keys that then hold still
    Then it moves what the disturbed pass left behind
    And it reports a finished move, saying nothing about the keys having changed

  @property @error @driving_port @us-09 @slice-09
  Scenario: Nothing said about the keys having changed is a key
    Given a pass disturbed by the keys being replaced under it
    When Lighthouse says so
    Then what it says names keys by their identifier and nothing else
    And no part of any key appears in it, in any encoding
