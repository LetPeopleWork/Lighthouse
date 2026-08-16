Feature: Only actions that would change something (Epic 5775, Slice 06a — US-06a)
  As the administrator who opened the encryption panel to find out what to do
  I want to be offered the action this instance actually needs, once
  So that the most prominent control on the screen is not one with nothing to act on

  # Three separate observations from the same screen turn out to be one defect. The move is offered
  # unconditionally, so a clean instance is invited to move nothing and told "Moved 0 stored secrets".
  # The same move is then offered twice at once — inside the warning and again in the button row — under
  # two different names, both calling the same thing. And the only emphasised control is Rotate, which
  # mints yet another key and is not what an upgraded instance needs.
  #
  # The resolution is the maintainer's own: keep one action, in the button row, and let the alert name
  # it rather than carry its own copy. Emphasis follows the open problem rather than a fixed favourite.
  #
  # The case that matters most is the one where the key in force IS the published key. There the move
  # re-encrypts that key onto itself, changes nothing, and leaves the warning standing — so it must not
  # be offered at all. What fixes that instance is the custody sentence above it.

  @edge @us-06a @slice-06a
  Scenario: Nothing to move, so no move is offered
    Given an instance holding no secrets under any key it is not writing with
    When an administrator opens the encryption settings
    Then no action to move stored secrets is offered
    And nothing on the screen invites an operator to act on nothing

  @edge @error @us-06a @slice-06a
  Scenario: The move is not offered where it could not achieve anything
    Given an instance whose key in force is the key published with the product
    When an administrator opens the encryption settings
    Then no action to move stored secrets is offered
    And the panel says what would fix this instance instead, which is a key of its own

  @driving_adapter @us-06a @slice-06a
  Scenario: The alert names the action rather than carrying its own copy of it
    Given an instance holding secrets under the key published with the product
    When an administrator opens the encryption settings
    Then the alert names the action by the name it carries in the button row
    And the alert has no button of its own

  @property @us-06a @slice-06a
  Scenario: One thing is never offered twice under two names
    Given any state the encryption panel can be in
    When an administrator opens the encryption settings
    Then no two controls on the screen do the same thing

  @driving_adapter @us-06a @slice-06a
  Scenario: What is emphasised is this instance's open problem
    Given an instance holding secrets under the key published with the product
    When an administrator opens the encryption settings
    Then moving the stored secrets is the emphasised action
    And rotating the key is offered without emphasis, because it is not what this instance needs

  @edge @us-06a @slice-06a
  Scenario: An instance with nothing wrong emphasises nothing
    Given an instance whose every secret is on the key in force
    When an administrator opens the encryption settings
    Then no action is emphasised
    And the actions that remain are the ones an administrator might still choose to take

  @edge @us-06a @slice-06a
  Scenario: An instance that cannot make a key is not offered to make one
    Given an instance whose key was supplied to it
    When an administrator opens the encryption settings
    Then rotating the key is not offered at all
    And the action it is offered is the one it can carry out
