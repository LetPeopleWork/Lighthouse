Feature: Saying what the screen is about (Epic 5775, Slice 06a — US-06a)
  As an administrator opening the encryption panel for the first time
  I want the screen to establish its subject, and every sentence on it to be true of my instance
  So that I neither have to guess what I am looking at nor act on advice that does not apply to me

  # Read cold, the panel opens on a table whose first row is "Key source" — with nothing saying that
  # this is about the credentials your Connections store, that they are encrypted before they are
  # written, and that this is the key that encrypts them. The maintainer, reading it as a first-time
  # user: "I would genuinely not understand what I'm seeing."
  #
  # Two of its sentences are worse than unclear; they are false for the operator most likely to read
  # them. "Kept in" names a directory under configuration custody, where the key is not kept and never
  # will be — and that directory exists and is full of key-shaped files, so an operator who backs it up
  # has every reason to think they took their key with them. They did not.
  #
  # And the rotation instruction names a setting the operator did not set, differing from theirs by one
  # character, with a different grammar that is never given.

  @driving_adapter @us-06a @slice-06a
  Scenario: The panel says what it is about before it says anything else
    Given an administrator who has never opened this screen
    When they open the encryption settings
    Then the first thing they read establishes that this is about credentials stored in Connections
    And it offers somewhere to read more

  @driving_adapter @us-06a @slice-06a
  Scenario: The warning states the situation, then the action
    Given an instance holding secrets under the key published with the product
    When an administrator opens the encryption settings
    Then the warning says what is true of this instance first
    And then what to do about it
    And why that key is no protection is left to the page it links to

  @error @us-06a @slice-06a
  Scenario: Where Lighthouse does not keep the key, the panel does not name a directory
    Given an instance whose key was supplied through configuration
    When an administrator opens the encryption settings
    Then no row invites them to back up a directory the key is not in
    And what the panel names instead is where the key actually came from

  @edge @us-06a @slice-06a
  Scenario: Where Lighthouse does keep the key, it says exactly where
    Given an instance using a key it made for itself
    When an administrator opens the encryption settings
    Then the directory holding that key is named
    And it is named as the thing to back up alongside the database

  @error @us-06a @slice-06a
  Scenario: The rotation instruction can be followed exactly as written
    Given an instance whose key was supplied through configuration
    When an administrator reads how to replace that key
    Then it names the setting they actually set
    And it gives the grammar for more than one key, including the separator
    And it says which entry becomes the key in force
    And it says what to do with the setting they were using before

  @edge @us-06a @slice-06a
  Scenario: Two settings that differ by one character are told apart
    Given an administrator reading how to replace a supplied key
    When both the singular and the plural spelling exist
    Then the instruction says both exist and which one wins
    And an operator who has set one is not quietly told to set the other

  @error @property @us-06a @slice-06a
  Scenario: Nothing the panel says is a credential or a key
    Given any state the encryption panel can be in
    When an administrator opens the encryption settings
    Then nothing drawn on the screen is key material in any encoding
    And nothing drawn on the screen is a stored credential
