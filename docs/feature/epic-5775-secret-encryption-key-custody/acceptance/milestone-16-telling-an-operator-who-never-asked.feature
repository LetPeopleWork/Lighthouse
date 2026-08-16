Feature: Telling an operator who never asked (Epic 5775, Slice 04 — US-04)
  As someone who upgraded Lighthouse and read no release note
  I want to be told that my stored credentials are on a key anyone can obtain
  So that I learn it from the product rather than from somebody else finding out first

  # Upgrading re-encrypts nothing, which is the deliberate choice made in slice 03: a silent pass over
  # every credential in the installation, with nobody watching, is a worse risk than the one it closes.
  # The consequence is that an operator who upgrades and does nothing is left exposed by inaction, and
  # this is the surface that closes that gap. The notice is not behind the check button, because an
  # operator who knew to press it did not need telling.
  # It is counted without decrypting anything: an instance that has never rotated would otherwise pay
  # for a full read of every credential each time somebody opens the settings page. The action beside
  # the notice is the one that fixes it, so the sentence and the remedy are never a page apart.

  @driving_adapter @us-04 @slice-04
  Scenario: An operator who has just upgraded is told, without asking
    Given an instance whose stored secrets are still on the key published with the product
    When an administrator opens the encryption settings
    Then they are told that those secrets are readable with a key anyone who has Lighthouse can obtain
    And they were not required to run anything to be told it

  @driving_adapter @us-04 @slice-04
  Scenario: The notice says how many, and offers the one action that fixes it
    Given an instance with several secrets still on the key published with the product
    When an administrator opens the encryption settings
    Then the notice names how many there are
    And the action that moves them onto the key in force is offered beside it

  @edge @driving_adapter @us-04 @slice-04
  Scenario: The notice is gone once nothing is left under that key
    Given an instance whose stored secrets have all been moved onto the key in force
    When an administrator opens the encryption settings
    Then no such notice is shown
    And nothing suggests there is anything left to do about that key

  @edge @driving_adapter @us-04 @slice-04
  Scenario: A fresh install is never told to fix a problem it does not have
    Given a newly installed instance that has never held a secret under the key published with the product
    When an administrator opens the encryption settings
    Then no such notice is shown

  @driving_adapter @us-04 @slice-04
  Scenario: The check is offered on the panel, and what it found reads at a glance
    Given an administrator on the encryption settings
    When they check the stored secrets
    Then they see how many were checked, how many are on the key in force, how many are on an earlier key, and how many could not be read
    And each secret that could not be read is listed with its Connection and its field
    And the summary never claims anything was moved, because nothing was

  @error @driving_port @us-04 @slice-04
  Scenario: Somebody who is not a System Administrator cannot check the stored secrets
    Given a signed-in user who is not a System Administrator
    When they ask for the stored secrets directly, with the screen bypassed
    Then they are refused
    And they learn nothing about which keys the instance holds
