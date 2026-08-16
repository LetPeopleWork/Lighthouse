Feature: Never overwrite a secret that could not be read, and finish what an interruption left (Epic 5775, Slice 03 — US-03)
  As the administrator running a rotation on a live instance
  I want the pass to leave alone anything it could not verify, and to be safe to run again
  So that a rotation can never turn a recoverable problem into a lost credential

  # A rotation writes over stored credentials. The one thing it must never do is write over something it
  # did not first read back as the credential it was — a value nobody can decrypt is a value nobody can
  # re-encrypt, and replacing it destroys the only copy. So the pass reads, verifies, and only then
  # writes; anything it could not verify is left byte for byte and named in the report instead.
  # Being safe to re-run comes from the same place: what still needs moving is written on each stored
  # value itself, as the name of the key it is under, so an interrupted pass leaves a working instance
  # and the next run picks up exactly the remainder (ADR-151).

  @error @us-03 @slice-03
  Scenario: A secret that cannot be read is left exactly as it was, and named
    Given a Connection holding a stored secret that no held key can read
    When the key is rotated
    Then that stored value is byte for byte what it was before
    And the report names the Connection and the field that holds it
    And the rest of the secrets were still moved

  @edge @us-03 @slice-03
  Scenario: A value that was never encrypted is reported rather than encrypted
    Given a stored value that is plain text rather than an encrypted secret
    When the key is rotated
    Then that value is left exactly as it was
    And it is named in the report as something no key was used on
    And nothing about it was encrypted, because a value mistaken for plain text would be buried under a layer nobody unwraps

  @property @us-03 @slice-03
  Scenario: Running it again moves nothing and says the same thing
    Given an instance whose key has just been rotated
    When the same action is run twice more
    Then each run moves nothing
    And each run reports the same totals as the run before it
    And no stored secret changed between the runs

  @edge @us-03 @slice-03
  Scenario: Interrupted halfway, the instance still works and the next run finishes the rest
    Given a rotation that stopped after moving some of the secrets but not all of them
    When each Connection is asked for its credential
    Then every one of them is readable, whichever key it is under
    And running the rotation again moves exactly the ones that were left
    And the second run needed nothing to be told about where the first one stopped

  @edge @us-03 @slice-03
  Scenario: Nothing is left stored under the key published with the product
    Given an instance where the last secret still under the key published with the product is readable
    When the key is rotated
    Then that secret is moved onto the new key
    And nothing readable is stored under the key published with the product any more

  @error @us-03 @slice-03
  Scenario: A secret under the published key that nobody can read is left exactly as it was
    Given an instance holding one secret under the key published with the product that nobody can read
    When the key is rotated
    Then that secret is left exactly as it was and named in the report
    And the key published with the product is still one of the keys held

  @edge @us-03 @slice-03
  Scenario: A rotation only ever adds to the keys an instance can read with
    Given an instance holding several keys, and a request that has already loaded a credential
    When the key is rotated
    Then every key that was held before is still held
    And the credential that request is holding is still readable
    And taking a key away would have made it unreadable in the middle of somebody's work

  @error @us-03 @slice-03
  Scenario: Saving a Connection leaves a secret nobody can read exactly as it was
    Given a Connection holding a stored secret that no held key can read
    When somebody saves that Connection after changing something else about it
    Then the stored secret is byte for byte what it was before
    And it still says it cannot be read, rather than looking healthy from then on
    And restoring the key store it belongs to would still bring the credential back

  @regression @us-03 @slice-03
  Scenario: A rotation does not reject an edit somebody already had open
    Given a System Administrator with a Connection open for editing
    When the key is rotated while that form is open
    And they save the Connection
    Then the save is accepted
    And the secret they did not retype is still readable
