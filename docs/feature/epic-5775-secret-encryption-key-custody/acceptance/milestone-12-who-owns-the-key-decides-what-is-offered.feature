Feature: Who owns the key decides what an administrator is offered (Epic 5775, Slice 03 — US-03)
  As the administrator of an instance whose key was handed to it by somebody else
  I want to be offered the part of a rotation Lighthouse can actually do
  So that I am never offered an action that would take every secret out of reach on the next restart

  # Rotating is two jobs. Making a key and keeping it is one; moving every stored secret onto the key in
  # force is the other. Only the second is always Lighthouse's. Where an operator supplied the key — in
  # configuration, or from a secret store mounted into the container — a key Lighthouse minted would be
  # written where the supplied one wins again on the next start, leaving everything moved onto the minted
  # key unreadable. So the offer is derived from where the key in force actually came from, and there is
  # no setting that can contradict it (ADR-152).
  # The refusal is part of the contract rather than a UI convention: it holds with the screen bypassed.
  # These run as WebApplicationFactory integration tests and Vitest tests over the panel.

  @error @driving_port @us-03 @slice-03
  Scenario: Where an operator owns the key, Lighthouse refuses to make one
    Given an instance running on a key its operator supplied
    When a System Administrator asks it to rotate the key, going around the screen entirely
    Then it refuses and says the key belongs to the operator
    And the keys it holds are exactly the keys it held before
    And nothing was written where the supplied key came from

  @real-io @driving_port @us-03 @slice-03
  Scenario: Where an operator owns the key, moving the secrets onto the new one is offered and works
    Given an operator who has added a new key alongside the old one and restarted the instance
    And a System Administrator who can see both keys are held
    When they move the stored secrets onto the key now in force
    Then every readable secret is stored under the new key
    And the old key is still held, so nothing that was left behind became unreadable
    And no credential was requested or re-entered

  @driving_adapter @us-03 @slice-03
  Scenario: The panel says who owns the key and which keys are held
    Given a System Administrator opening the encryption settings
    When the panel loads
    Then it says where the key in force came from
    And it lists the keys the instance currently holds, by name
    And it shows no key material of any kind

  @edge @driving_adapter @us-03 @slice-03
  Scenario: The panel never shows an action Lighthouse cannot honour
    Given an instance running on a key its operator supplied
    When a System Administrator opens the encryption settings
    Then there is no control offering to make a new key, disabled or otherwise
    And one sentence says who owns the key and what the operator does to replace it
    And the action that is offered is moving the stored secrets onto the key in force

  @error @driving_adapter @us-03 @slice-03
  Scenario: Somebody who is not a System Administrator can neither rotate nor move anything
    Given a person signed in without System Administrator rights
    And a viewer reaching the instance through an embedded frame
    When either asks the instance to rotate the key or to move the stored secrets
    Then each is refused
    And no stored secret was written

  @driving_adapter @us-03 @slice-03
  Scenario: The rotation is recorded, and the record carries no key material
    Given a System Administrator who rotates the key
    When the rotation finishes
    Then a record says who did it, when, how many secrets were moved and how many could not be read
    And it names the key now in force
    And no part of any key appears anywhere in it

  @property @us-03 @slice-03
  Scenario: Moving the secrets is the same work whoever owns the key
    Given the same stored secrets on an instance that made its own key and on one that was handed a key
    When the secrets are moved onto the key in force on each
    Then each ends with the same secrets under the key in force
    And each reports the same counts
    And the only thing that differed was whether a new key was made first
