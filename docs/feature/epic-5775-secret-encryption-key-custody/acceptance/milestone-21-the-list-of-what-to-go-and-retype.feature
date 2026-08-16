Feature: The list of what to go and retype (Epic 5775, Slice 05b — US-05b)
  As the operator who has just started past the refusal
  I want to be handed the exact Connections and fields to re-enter
  So that getting back to a working instance is a list of tasks rather than a hunt

  # The hatch opens the door. This is what stops it leaving the room locked. An operator who starts past
  # the refusal is looking at an instance whose credentials are all unreadable, and the only way back to
  # a working install is to type them in again — so the two things that must work are knowing which ones
  # and being able to save them.
  #
  # The check pass already produces exactly that list, Connection and field, and has since slice 04. It
  # is reused rather than rebuilt.
  #
  # The guard that stops a save overwriting a credential it cannot read is correct and stays. It exists
  # so that a value encrypted under a key sitting in somebody's backup is not buried under a second
  # layer nobody can unwrap. It must not also stand between an operator and a value they just typed —
  # those are different acts, and telling them apart is what this milestone is about.

  @driving_port @us-05b @slice-05b
  Scenario: The check names every credential that has to be re-entered
    Given an instance started past the refusal
    When an administrator checks the stored secrets
    Then every unreadable credential is listed
    And each names the Connection that owns it and the field that holds it

  @real-io @us-05b @slice-05b
  Scenario: Re-entering a credential works
    Given a Connection whose stored credential cannot be read
    When an administrator enters the credential again and saves
    Then the save succeeds
    And the Connection works again

  @real-io @us-05b @slice-05b
  Scenario: What was re-entered is stored under the key in force
    Given a Connection whose stored credential cannot be read
    When an administrator enters the credential again and saves
    Then the stored value is on the key this instance is writing under
    And a check afterwards no longer lists that field

  @error @us-05b @slice-05b
  Scenario: A credential nobody re-entered is left exactly as it was
    Given a Connection with two stored credentials, both unreadable
    When an administrator enters only one of them again and saves
    Then the one they typed is stored under the key in force
    And the other is byte for byte what it was, because a key that reads it may still turn up

  @edge @us-05b @slice-05b
  Scenario: Once everything has been re-entered, the switch is not needed any more
    Given an instance started past the refusal, with every unreadable credential re-entered
    When the switch is removed and Lighthouse starts
    Then it starts without it
    And the hatch closes behind the operator rather than staying open by habit
