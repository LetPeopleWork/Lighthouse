Feature: Counting what that key can actually read (Epic 5775, Slice 04b — US-04b)
  As the administrator reading the encryption panel
  I want the number of credentials said to be on the published key to be the truth in both directions
  So that I am neither told to fix a problem I do not have nor left with one I was told was fixed

  # The count decided the question by the shape of the stored value: an envelope naming the published key,
  # or no envelope at all. That is a guess about which key wrote a value, and it is wrong in both
  # directions. An install that set a key of its own before this release carries values with no envelope
  # on them and is told its credentials are public, which is false and is the kind of false that makes an
  # operator stop believing the panel. An install whose values were written under the published key
  # wearing some other name is told it is healthy, which is false in the direction that costs something.
  #
  # The question has one honest answer and it is not a guess: can that key read this value? Asking it
  # costs a decrypt, so the values it is asked about are narrowed in the database first — an instance that
  # has moved everything has nothing left to ask about, which is exactly the instance that opens the
  # settings page most often.

  @real-io @error @us-04b @slice-04b
  Scenario: A credential written under the operator's own key is not called public
    Given an install that set a key of its own before this version, holding a credential written under it
    When an administrator opens the encryption settings
    Then no credential is reported as being on the key published with the product
    And the panel does not offer to fix something that is not wrong

  @real-io @us-04b @slice-04b
  Scenario: A credential written under the published key is still called public
    Given a default install upgraded from before this version, holding a credential written under the published key
    When an administrator opens the encryption settings
    Then that credential is counted as being on the key published with the product

  @edge @us-04b @slice-04b
  Scenario: A credential in an envelope naming the published key is counted
    Given a stored credential in an envelope that names the key published with the product
    When an administrator opens the encryption settings
    Then it is counted

  @edge @us-04b @slice-04b
  Scenario: A credential in an envelope on any other key is not counted
    Given a stored credential in an envelope that names a key of this instance's own
    When an administrator opens the encryption settings
    Then it is not counted

  @edge @us-04b @slice-04b
  Scenario: A value nothing ever encrypted is not called public either
    Given a stored value that was never encrypted at all
    When an administrator opens the encryption settings
    Then it is not counted as being on the key published with the product
    And that is because it is a different problem, which the check reports in its own state

  @property @us-04b @slice-04b
  Scenario: What decides the count is the key, never the name on the value
    Given any stored credential in any of the shapes an install can hold
    When the count is taken
    Then it counts exactly the credentials the published key can read
    And two values written under the same key are counted the same way whatever names they wear

  @edge @us-04b @slice-04b
  Scenario: An instance that has moved everything pays nothing to be told so
    Given an instance whose every credential is on a key of its own
    When an administrator opens the encryption settings
    Then the count is zero
    And not one stored credential was decrypted to arrive at it
