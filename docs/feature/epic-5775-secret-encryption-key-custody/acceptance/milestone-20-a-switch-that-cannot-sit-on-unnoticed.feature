Feature: A switch that cannot sit on unnoticed (Epic 5775, Slice 05b — US-05b)
  As the administrator who inherits this instance six months from now
  I want to be told, without asking, that it is running past a refusal
  So that a hatch opened for one afternoon is not still open a year later with nobody aware of it

  # The emergency administrator is the precedent, and the reason it is the precedent is the visibility
  # rather than the delivery: that setting is surfaced through the system information and the RBAC
  # status precisely so it cannot sit switched on unnoticed. A hatch that hides is worse than no hatch,
  # because the operator who opens it is not the one who pays for it being left open.
  #
  # Two surfaces, for two different readers. The startup line is for whoever is watching the container
  # come up. The encryption panel is for whoever opens Settings months later and has no idea any of
  # this happened — and it is the only one a standalone operator will ever see.

  @driving_adapter @us-05b @slice-05b
  Scenario: The startup line says the instance started past a refusal
    Given an instance started with the switch set
    When the startup lines are written
    Then one of them says this instance is running with credentials it cannot read
    And it stands out the way the emergency administrator line does

  @driving_port @us-05b @slice-05b
  Scenario: The encryption state says so too
    Given an instance started with the switch set
    When an administrator opens the encryption settings
    Then the state says the instance started past the refusal
    And the panel can say so without having to ask a second question

  @edge @us-05b @slice-05b
  Scenario: An instance that never needed it says nothing about it
    Given an instance started without the switch
    When the startup lines are written and an administrator opens the encryption settings
    Then neither mentions the switch
    And a healthy install is not taught to worry about a hatch it never opened

  @edge @driving_adapter @us-05b @slice-05b
  Scenario: It is said on every start, not only the first
    Given an instance started with the switch set, and restarted
    When the startup lines are written again
    Then it says so again
    And the panel still says so, because nothing about the situation has changed

  @error @us-05b @slice-05b
  Scenario: Saying so costs no credential
    Given an instance started with the switch set
    When an administrator opens the encryption settings
    Then nothing in what comes back is a credential or any part of a key
