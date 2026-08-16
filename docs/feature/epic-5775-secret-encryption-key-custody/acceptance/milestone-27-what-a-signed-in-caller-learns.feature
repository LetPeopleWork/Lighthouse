Feature: What a caller who is merely signed in gets to learn (Epic 5775, Slice 06b — US-06b)
  As the administrator diagnosing an instance, and as the operator of one behind an embedded frame
  I want the custody line where I actually look, and nothing about it where I am only a viewer
  So that a standalone operator can see which key they are on without handing that answer to everybody

  # The startup banner is the design's primary custody surface and the entire standalone population
  # never sees a console. The encryption panel answers half of it — but somebody working out why an
  # instance is behaving oddly opens system information first, and that page says nothing about
  # encryption at all.
  #
  # The reason it says nothing is good and stands: that response answers before anyone is authorised,
  # because the application shell needs the version and the authentication posture to render, and a
  # viewer who opens Lighthouse inside an embedded frame satisfies "signed in". So the rule is not
  # relaxed — the response simply carries a field that only some callers are given.
  #
  # The same page already carries the emergency administrator subjects, unguarded, and those are not a
  # category but the names of real people who can administer the installation. It is the same question,
  # so it is answered in the same place rather than left as two halves that drift apart.

  @driving_port @us-06b @slice-06b
  Scenario: An administrator sees which key this instance is on
    Given an instance running on a key it made for itself
    When a System Administrator reads the system information
    Then it says which custody the key is under
    And it says where the key store is

  @error @driving_port @us-06b @slice-06b
  Scenario: A signed-in viewer is told nothing about the key
    Given an instance running on a key it made for itself
    When somebody who is signed in but not a System Administrator reads the system information
    Then nothing in the answer says which custody the key is under
    And nothing in it says where the key store is
    And the page draws no row for either

  @error @us-06b @slice-06b
  Scenario: The same viewer is told nothing about who can administer the installation
    Given an instance with emergency administrators configured
    When somebody who is signed in but not a System Administrator reads the system information
    Then the emergency administrator subjects are absent
    And that is because they name people rather than a category

  @edge @us-06b @slice-06b
  Scenario: An administrator still sees who the emergency administrators are
    Given an instance with emergency administrators configured
    When a System Administrator reads the system information
    Then the emergency administrator subjects are there, exactly as before

  @edge @us-06b @slice-06b
  Scenario: Everything the shell needs is still answered to everyone
    Given any signed-in caller
    When they read the system information
    Then the version, the authentication posture and the authorisation posture are all answered
    And nothing that was unguarded before this change became guarded by accident

  @edge @us-06b @slice-06b
  Scenario: An instance not enforcing access control tells its operator
    Given a standalone instance with access control switched off
    When its operator reads the system information
    Then they are shown the custody and the key store
    And they are shown it because with nobody to tell apart there is nobody to withhold it from

  @error @property @us-06b @slice-06b
  Scenario: No key and no key id appears under any role
    Given any caller and any custody
    When the system information is read
    Then nothing in the answer is key material in any encoding
    And nothing in it is a key id, because the custody and the place are the whole of what this page says

  @property @us-06b @slice-06b
  Scenario: One place decides what only an administrator may see
    Given a field that only a System Administrator may be told
    When a second such field is added later
    Then it is withheld by the same decision rather than by a new one
    And nobody has to remember to guard it separately

  @property @us-06b @slice-06b
  Scenario: The custody reads the same here as it does on the startup line
    Given an instance in any of the four custodies
    When its startup line is written and its system information is read
    Then both say the same word for the custody in force
    And neither can be changed without the other changing with it
