Feature: The key the documentation names is the key the instance uses (Epic 5775, Slice 02 — US-02, Bug #5776)
  As an operator who read the configuration page and set a key of my own
  I want the setting I was told to set to be the one that decides
  So that doing the right thing actually protects me, instead of leaving me confident and wrong

  # This milestone is Bug #5776. The configuration page names one setting and the code reads another,
  # so an operator who followed the documentation is still on the published key and nothing tells them.
  # The fix is one name across every way a key can be supplied — command line, environment, file — and
  # one parser (ADR-148). No alias is introduced: a second accepted name would have to be honoured
  # forever, and would recreate the very ambiguity this milestone exists to remove.
  # These run as backend NUnit tests over the ring parser and the bootstrap, plus WebApplicationFactory
  # boots for the ones that need a real start with a real configuration source.

  @driving_port @us-02 @slice-02 @bug-5776
  Scenario: A key supplied the way the documentation says is the key the instance uses
    Given an operator who supplied a key of their own exactly as the configuration page describes
    When the instance starts
    Then new secrets are written under the key that operator supplied
    And the instance reports that the key was supplied by the operator rather than generated

  @driving_port @us-02 @slice-02 @bug-5776
  Scenario Outline: The same key is understood the same way however it was supplied
    Given a key supplied <transport>
    When the instance starts
    Then it resolves to the same key, with the same identity, in every case

    Examples:
      | transport                             |
      | on the command line                   |
      | as an environment variable            |
      | in the settings file                  |
      | in a file the instance was pointed at |

  @edge @us-02 @slice-02
  Scenario: An instance given a key does not go and make one of its own
    Given an operator who supplied a key
    And a location where this instance could have kept a key it generated
    When the instance starts
    Then it generates nothing
    And it reports that this key is the operator's to change, not the instance's

  @error @us-02 @slice-02
  Scenario Outline: A supplied key that cannot be used stops startup and says what is wrong with it
    Given a supplied key that is <defect>
    When the instance starts
    Then it refuses to start
    And the message names the entry at fault and says <what it says>

    Examples:
      | defect                                | what it says                                     |
      | one byte short of the required length | how long it is and how long it has to be         |
      | not readable as encoded key material  | that it could not be decoded                     |
      | a ring naming the same key twice      | which name is repeated                           |
      | empty                                 | that no key was supplied under a name that was set |
      | named with characters a key may not use | which name is not allowed                        |

  @error @us-02 @slice-02
  Scenario: The complaint about a bad key never contains the key
    Given a supplied key the instance refuses
    When it reports why
    Then the report describes the fault
    And no part of the supplied material appears in it, whole or in fragments

  @property @edge @us-02 @slice-02
  Scenario: One supplied key gives every instance holding it the same name for it
    Given the same key supplied to two separate instances, and to one instance twice across a restart
    When each of them labels a secret it writes
    Then every one of them labels it with the same key name
    And a secret written by one is attributable by the others

  @edge @us-02 @slice-02
  Scenario: An operator can supply more than one key, and the first one is the one that writes
    Given an operator who supplied several keys as one setting
    When the instance starts
    Then new secrets are written under the first key only
    And secrets already written under any of the others are still read
    And there is no way to express two keys to write under, or none

  @regression @us-02 @slice-02 @bug-5776
  Scenario: The setting the code used to read no longer decides anything
    Given an instance where only the previously-read setting name carries a key
    When the instance starts
    Then that value does not become the key the instance writes under
    And the instance behaves as though no key had been supplied at all
