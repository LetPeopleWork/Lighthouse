Feature: Only the keys that matter (Epic 5775, Slice 06a — US-06a)
  As an administrator looking at which keys this instance holds
  I want to see the keys something was actually written under
  So that a brand-new install does not appear to be holding a key called "legacy-default" for no reason

  # Every ring carries the key published with the product as a read-only entry, which is how an upgraded
  # instance keeps reading what it stored. On a first install that is technically true and practically
  # alarming: a fresh instance appears to hold a key named after a legacy it never had. Rotate a few
  # times and the table fills with chips that never encrypted anything.
  #
  # The decision is to hide, not to remove. Keeping a key costs nothing and an operator who dropped one
  # they still needed for an old backup would have no way back. So the ring is unchanged and the table
  # is narrowed — which also means a hidden key is still there to read with, and a restore that brings
  # back older values makes its key appear again on its own.
  #
  # Which keys are referenced is answered by asking the database which key ids the stored values carry,
  # not by decrypting anything. The id is already written on the front of every stored value, so reading
  # it costs a query and reading it any other way costs a decrypt that buys nothing. It also keeps the
  # table renderable on an instance that cannot read its own secrets — which is precisely the instance
  # whose administrator is looking at this screen.

  @edge @us-06a @slice-06a
  Scenario: A key nothing was ever written under is not listed
    Given a fresh install that has stored nothing yet
    When an administrator opens the encryption settings
    Then only the key it is writing with is listed
    And the key published with the product is not among them

  @edge @us-06a @slice-06a
  Scenario: A key something was written under is listed, however old it is
    Given an instance holding a secret written under a key it has since rotated away from
    When an administrator opens the encryption settings
    Then that earlier key is listed
    And it is shown as one the instance reads with rather than writes with

  @property @us-06a @slice-06a
  Scenario: Hiding a key never stops it being read with
    Given an instance whose table lists fewer keys than its ring holds
    When a stored secret written under one of the hidden keys is read
    Then it is read successfully
    And nothing about hiding a key from a table changed what the instance can open

  @edge @us-06a @slice-06a
  Scenario: A restore brings a key back into view on its own
    Given an instance whose table has stopped listing a key nothing referenced
    When a database is restored that holds secrets written under that key
    Then the key is listed again
    And nobody had to put it back, because it was never taken off the ring

  @edge @us-06a @slice-06a
  Scenario: The key in force is always listed, even with nothing stored
    Given an instance that has stored nothing at all
    When an administrator opens the encryption settings
    Then the key it would write with is listed
    And an operator can still see which key their backup has to match

  @driving_adapter @us-06a @slice-06a
  Scenario: The startup custody line is one word and a path
    Given an instance starting in any of the four custodies
    When the startup lines are written
    Then the encryption line says which custody in a single word, then where the key store is
    And the key id is not on that line, because the refusal that needs it names it there instead
