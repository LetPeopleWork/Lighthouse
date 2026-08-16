Feature: Every stored secret, and the key it is on (Epic 5775, Slice 04 — US-04)
  As the administrator about to rotate, or having just rotated
  I want to be told what every stored secret is encrypted under, without anything being written
  So that I find out what I am sitting on before a sync tells me at three in the morning

  # The check walks the same stored secrets the rotation walks and reads them the same way, and that is
  # deliberate: an operator should read the same words before and after. What it does differently is the
  # thing that makes it worth having — it looks at every stored secret, including the ones already on the
  # key in force, where the rotation looks only at what it still has work to do on. A check that reported
  # nothing on a freshly rotated instance would be answering a different question from the one asked.
  # Four states, never collapsed into "broken": on the key in force, on a key that was retired, in the
  # format this version replaced, and unreadable. The first two are fine, the third is a value nothing was
  # ever encrypted with, and only the fourth asks somebody to do something.

  @real-io @driving_port @us-04 @slice-04
  Scenario: Checking names every stored secret, what owns it, and the key it is on
    Given an instance holding stored secrets across several Connections
    When an administrator checks the stored secrets
    Then every stored secret is listed
    And each one names the Connection that owns it and the field that holds it
    And each one names the key it is encrypted under

  @property @us-04 @slice-04
  Scenario: The check writes nothing at all
    Given an instance holding stored secrets in every state a secret can be in
    When an administrator checks the stored secrets
    Then every stored value is byte for byte what it was before
    And nothing the check was handed has a way to write one, so it could not have done otherwise

  @edge @us-04 @slice-04
  Scenario: A secret on the key in force and one on a key that was retired are told apart
    Given an instance holding one secret on the key in force and one on a key that was retired
    When an administrator checks the stored secrets
    Then the first is reported as being on the key in force
    And the second is reported as being on the earlier key, named
    And the two are counted separately rather than together

  @edge @us-04 @slice-04
  Scenario: A value in the format this version replaced is reported as that, not as broken
    Given a stored secret written in the format this version replaced, which a held key can still read
    When an administrator checks the stored secrets
    Then it is reported in its own state rather than as unreadable
    And it names the key that can still read it

  @error @us-04 @slice-04
  Scenario: A value nobody can read is reported as that, and not as something ordinary
    Given a stored secret that no held key can read
    When an administrator checks the stored secrets
    Then it is reported as unreadable
    And it is not reported as a value that was never encrypted, because those two send an operator to different places

  @property @us-04 @slice-04
  Scenario: The four states account for every secret checked, with none left over
    Given an instance holding secrets in all four states at once
    When an administrator checks the stored secrets
    Then the four counts add up to the number of secrets listed
    And no secret is counted in two of them
