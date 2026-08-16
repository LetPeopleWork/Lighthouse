Feature: Move every stored secret onto a new key without asking anyone for a credential (Epic 5775, Slice 03 — US-03)
  As the administrator of an instance whose key may have been exposed
  I want to move every stored secret onto a new key from inside Lighthouse
  So that I can contain the exposure this afternoon instead of asking every team to re-enter every credential

  # The documented way to change the key today is to change it and then reconfigure every work tracking
  # system by hand, with every sync down until the last token has been re-entered. This milestone is the
  # replacement: one action that makes a new key, moves every secret that can be read onto it, keeps the
  # previous key so nothing already stored becomes unreadable, and hands back a number.
  # The new key is proved before it is used — written, read back, and round-tripped — because a key that
  # cannot be read tomorrow takes every secret moved onto it today with it (ADR-151).
  # These run as backend NUnit tests over the re-encryption component, WebApplicationFactory integration
  # tests over the System-Admin-guarded surface, and one pass on a restored real backup.

  @real-io @driving_port @us-03 @slice-03
  Scenario: Rotating puts every readable secret onto a new key and asks nobody for anything
    Given an instance holding stored credentials for several Connections
    And a System Administrator who has decided the current key may have been exposed
    When they rotate the key
    Then every secret that could be read is stored under a key that did not exist before
    And no credential was requested, re-entered or invalidated
    And the number moved is reported back

  @edge @us-03 @slice-03
  Scenario: The key that was in force is retired, not discarded
    Given an instance that has just rotated its key
    When it reads a secret that the rotation did not move
    Then that secret is still readable
    And the key it was written under is still held, behind the new one
    And nothing is ever written under it again

  @us-03 @slice-03
  Scenario: The report says how many moved and how many could not be read, for each Connection
    Given several Connections, one of which holds a secret nobody can read
    When the key is rotated
    Then the report gives a count of secrets moved and a count that could not be read
    And each of those counts is attributed to the Connection it belongs to
    And each secret that could not be read is named by its Connection and the field holding it

  @real-io @regression @us-03 @slice-03
  Scenario: Every Connection works immediately afterwards
    Given an instance whose Connections were all working before the rotation
    When the key has been rotated
    And each Connection is asked to talk to its work tracking system
    Then each one is accepted
    And none of them asked anybody to supply a credential again

  @driving_port @us-03 @slice-03
  Scenario: Where Lighthouse owns the key, one action does the whole job
    Given an instance whose key Lighthouse made for itself
    When a System Administrator rotates the key
    Then a new key is made and kept where the previous one was kept
    And it becomes the key new secrets are written under
    And every readable stored secret is moved onto it
    And the previous key is retired in the same action

  @error @us-03 @slice-03
  Scenario: A new key that cannot be read back is never used, and no secret is moved
    Given an instance whose key store accepts a write and hands back something else
    When a System Administrator rotates the key
    Then the rotation stops and says the key could not be kept
    And the key in force is still the one that was in force before
    And not one stored secret was written

  @regression @us-03 @slice-03
  Scenario: Moving a secret changes nothing else about it
    Given a Connection whose settings and stored secret are both known
    When the key is rotated
    Then the secret reads back as exactly the credential it was before
    And nothing else about the Connection has changed
