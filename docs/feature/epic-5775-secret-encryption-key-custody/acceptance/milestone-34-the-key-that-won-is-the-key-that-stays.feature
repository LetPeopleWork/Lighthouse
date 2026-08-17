Feature: The key that won at startup is the key that stays (Epic 5775, Slice 08 — US-08)
  As the operator moving my encryption key from a setting into a file my secret store owns
  I want the key I supplied to keep being the key in force, however many ways I have supplied one
  So that leaving the old setting behind costs me nothing rather than costing me every credential

  # The ordering that decides which key an instance runs on is written down once — configuration,
  # then a mounted file, then a key it made for itself. That ordering holds at the moment of a start
  # and is contradicted thirty seconds later by a shorter one.
  #
  # The reload is set up whenever a keys file is named, without asking whether the file was the source
  # that actually answered. So an instance given a key both ways starts on the configured one, reports
  # itself as configured, and moves onto the file's key on the first tick. Every secret written under
  # the configured key in that window stops being readable. A restart puts the configured key back and
  # everything written under the file's key stops being readable in turn.
  #
  # Nobody has to do anything wrong after the misconfiguration for this to cost them credentials, and
  # the panel makes it worse by naming the setting that is not in force - so an operator debugging it
  # is sent to edit the one thing that is not the problem.

  @error @driving_port @us-08 @slice-08
  Scenario: A key supplied two ways is the one the ordering names
    Given an operator has set an encryption key in configuration
    And has also pointed the instance at a mounted keys file holding a different key
    When Lighthouse starts
    Then it runs on the key from configuration
    And it reports that key as the one an operator supplied through a setting

  @error @property @driving_port @us-08 @slice-08
  Scenario: The key that won at startup is still the key in force later
    Given an instance running on a key from configuration while a mounted keys file names another
    When as much time passes as it takes to re-read that file, several times over
    Then the key in force is still the one from configuration
    And a credential saved before that time and one saved after it are both readable

  @edge @driving_port @us-08 @slice-08
  Scenario: Where the file is the only place a key came from, a key added to it is still picked up
    Given an instance whose key came from a mounted keys file and from nowhere else
    When the operator adds a key to that file
    Then the instance picks it up without being restarted
    And the key it had before is still on the ring behind it

  @error @edge @driving_port @us-08 @slice-08
  Scenario: A file that appears after the instance started does not take the key away from configuration
    Given an operator has set an encryption key in configuration
    And has pointed the instance at a keys file that is not mounted yet
    When Lighthouse starts and the file appears afterwards
    Then the key in force is still the one from configuration

  @edge @driving_port @us-08 @slice-08
  Scenario: An instance told a key two ways says so, once
    Given an operator has supplied an encryption key both in configuration and in a mounted file
    When Lighthouse starts
    Then it says that a key was supplied in more than one way
    And it names both places
    And it says which of them the key in force came from
    And it says it once, however many places were named

  @driving_adapter @us-08 @slice-08
  Scenario: The encryption settings name the place the key actually came from
    Given an instance running on a key from configuration while a mounted keys file names another
    When an administrator opens the encryption settings
    Then the setting named there is the one the key in force arrived in

  @property @error @driving_port @us-08 @slice-08
  Scenario: Nothing said about a key having been supplied twice is a key
    Given an encryption key supplied in more than one way
    When Lighthouse says so
    Then what it says names settings and nothing else
    And no part of either key appears in it, in any encoding
