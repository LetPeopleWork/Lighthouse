Feature: Before a rotation, and after one (Epic 5775, Slice 04 — US-04)
  As an administrator deciding whether to rotate now or fix something first
  I want the same check to work on an instance that has never rotated and on one that just did
  So that rotating becomes a routine action rather than a nerve-wracking one

  # An instance that has never rotated is the one where this matters most: it is still on the key
  # published with the product, it has just been upgraded, and nobody has yet told its operator what
  # that means. The check has to work there, before any key has ever been minted, or it only ever
  # answers the question of somebody who already knew enough to rotate.
  # Afterwards it is the proof. A rotation reports what it moved; the check, run again, reports what is
  # actually stored — and those are different claims. The first is what a pass believes it did, the
  # second is what a fresh read of every row says.

  @real-io @us-04 @slice-04
  Scenario: The check works before any rotation has ever run
    Given an instance that has never rotated, still on the key published with the product
    When an administrator checks the stored secrets
    Then every stored secret is reported with the key it is on
    And no key was made in order to answer the question

  @real-io @us-04 @slice-04
  Scenario: Immediately after a rotation, every readable secret is on the key in force
    Given an instance whose key has just been rotated
    When an administrator checks the stored secrets
    Then every readable secret is reported as being on the key in force
    And none of them is reported as being on an earlier key

  @error @us-04 @slice-04
  Scenario: A secret the rotation could not read is still named afterwards
    Given a rotation that left one secret behind because it could not be read
    When an administrator checks the stored secrets afterwards
    Then that secret is still named, with its Connection and its field
    And the rest are reported as being on the key in force
    And the two reports agree with each other, because they read the same rows the same way

  @property @us-04 @slice-04
  Scenario: Checking twice in a row says the same thing and changes nothing
    Given an instance holding stored secrets
    When an administrator checks the stored secrets twice
    Then both checks report the same counts
    And no stored value changed between them

  @edge @real-io @us-04 @slice-04
  Scenario: Checking a large instance costs no more looking up than checking a small one
    Given an instance holding many more stored secrets than a small install would
    When an administrator checks the stored secrets
    Then what it costs to look them all up does not grow with how many there are
    And the check finishes inside the time a request is allowed to take
