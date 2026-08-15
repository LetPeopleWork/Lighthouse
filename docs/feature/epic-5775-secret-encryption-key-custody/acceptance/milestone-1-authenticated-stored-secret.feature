Feature: A stored secret is authenticated and says which key wrote it (Epic 5775, Slice 01 — US-01, US-07 precursor)
  As a platform operator
  I want every stored secret to prove it was written by a known key and has not been altered
  So that "I read it back correctly" is something the system knows rather than something it hopes

  # Driving port = the secret write/read path used by every save of a connection or a set of OAuth
  # tokens (LighthouseAppContext.EncryptSecrets on SaveChanges, and ICryptoService on the read side).
  # Mechanism behind the steps: AES-GCM with the header bound as associated data (ADR-146), and a
  # total four-state reader — current format, legacy blob, never-encrypted value, unreadable —
  # classified by inspection with no catch anywhere in the path (ADR-147).
  # These run as backend NUnit tests: pure classifier/format tests with no database, plus the two
  # marked @real-io which need a real save through EF on both providers.

  @real-io @driving_port @us-01 @us-07 @slice-01
  Scenario: A newly saved secret is stored in the current format and reads back unchanged
    Given a connection is being saved with a credential in one of its secret fields
    When the connection is saved
    Then the stored value carries a format marker, the id of the key that wrote it, and proof it has not been altered
    And reading it back returns exactly the credential that was entered

  @real-io @driving_port @us-01 @slice-01 @upgrade-from-pre-epic
  Scenario: A secret saved before this change is still read correctly, with no migration and no user action
    Given an instance upgraded from a version that stored secrets in the previous form
    When each of its stored secrets is read
    Then every one of them returns the credential it originally held
    And nothing asked the operator to re-enter anything

  @edge @us-01 @slice-01 @upgrade-from-pre-epic
  Scenario: A value that was never encrypted at all is recognised as such, by inspection
    Given a stored value from an install old enough that its secrets were never encrypted
    When the value is read
    Then it is recognised as a never-encrypted value by a deliberate check on its shape
    And it is not recognised that way because something failed and was caught

  @error @us-01 @slice-01
  Scenario: A stored secret whose proof of integrity does not verify is refused
    Given a stored secret whose proof of integrity does not verify
    When the value is read
    Then reading it fails and names the connection and the field that holds it
    And it does not hand back the stored value, the credential, or an empty value

  @error @us-01 @slice-01
  Scenario: A single altered byte fails to be read rather than producing an altered credential
    Given a stored secret with exactly one byte changed
    When the value is read
    Then reading it fails
    And no altered credential is produced from it

  @error @us-01 @slice-01
  Scenario: A stored secret relabelled with another key's name is refused rather than believed
    Given a stored secret whose recorded key id has been rewritten to name a different key the instance holds
    When the value is read
    Then reading it fails
    And the instance does not read it under the key it was relabelled with

  @edge @us-01 @slice-01
  Scenario: Saving a connection again does not encrypt an already-protected secret a second time
    Given a saved connection whose secret is already stored in the current form under the active key
    When the connection is saved again without the secret being re-entered
    Then the stored secret is unchanged in meaning and still reads back as the original credential
    And it has not been wrapped a second time

  @property @edge @us-01 @slice-01
  Scenario: A secret stored in the previous form can never be mistaken for the current one
    Given any secret stored in the previous form
    When the reader decides which form the value is in
    Then it is never taken for the current form
    And this holds for every previous-form value, not merely for the ones we happened to try

  @property @edge @us-01 @slice-01
  Scenario: Two secrets holding the same credential are never stored identically
    Given the same credential is saved into two different connections
    When both stored values are compared
    Then they differ
    And this holds however many times the same credential is saved

  @real-io @adapter-integration @us-01 @slice-01
  Scenario: An unusually long credential survives a real save and read on both database providers
    Given a credential far longer than any a work tracking system issues
    When it is saved into a connection and read back
    Then it returns unchanged
    And this holds on both the SQLite and the PostgreSQL provider
