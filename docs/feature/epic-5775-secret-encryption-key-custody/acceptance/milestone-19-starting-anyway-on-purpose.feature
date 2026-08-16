Feature: Starting anyway, on purpose (Epic 5775, Slice 05b — US-05b)
  As the operator whose encryption key is genuinely gone
  I want a deliberate, uncomfortable way to start Lighthouse without it
  So that two lost API tokens do not take my teams, forecasts and history down with them

  # Refusing to start when nothing stored can be read is right. Having no way past it is not. The
  # refusal fires during bootstrap, before a port is bound, so the process writes one line and exits —
  # and on a container with restart: always it does that forever. Pointing it at a fresh key store
  # refuses again, because the database still holds unreadable secrets.
  #
  # The switch is shaped like the emergency administrator: a setting an operator has to go and set on
  # purpose, that changes nothing about how the instance behaves except that it starts, and that says
  # so on every surface for as long as it is in force. It is not a key and it is not a repair — nothing
  # is re-encrypted and nothing is discarded. The credentials that could not be read still cannot be
  # read; the operator re-enters them.

  @error @driving_port @us-05b @slice-05b
  Scenario: With the switch set, an instance nothing can read starts
    Given an instance whose every stored credential was written under a key that is gone
    And the switch is set
    When Lighthouse starts
    Then it starts and serves requests
    And nothing stored has been changed

  @edge @us-05b @slice-05b
  Scenario: Without the switch, it still refuses
    Given an instance whose every stored credential was written under a key that is gone
    And the switch is not set
    When Lighthouse starts
    Then it refuses to start, exactly as it did before

  @edge @us-05b @slice-05b
  Scenario: The switch changes nothing on an instance that is fine
    Given an instance that can read what it has stored
    And the switch is set
    When Lighthouse starts
    Then it starts on the same key it would have started on
    And every stored credential is still readable

  @error @us-05b @slice-05b
  Scenario: The switch is not a repair
    Given an instance whose every stored credential was written under a key that is gone
    And the switch is set
    When Lighthouse starts
    Then no stored value has been re-encrypted
    And no stored value has been discarded
    And the credentials that could not be read still cannot be read

  @property @error @us-05b @slice-05b
  Scenario: The switch lets past one refusal and no other
    Given any of the other reasons Lighthouse refuses to start — key material it cannot use, nowhere to keep a key with nothing stored yet, a key file that is not there, the key published with the product supplied as the key
    And the switch is set
    When Lighthouse starts
    Then it refuses for that reason, unchanged
    And the refusal does not mention the switch, because the switch would not help
