Feature: Checks that can fail (Epic 5775, Slice 07 — US-07)
  As the person who will change this code in a year
  I want the checks that stop a key or an unintended answer escaping to be able to fail
  So that a green build means the promise holds rather than that nothing tried to break it

  # Two checks shipped in this epic, both written for exactly the defect that later got past them.
  #
  # The first feeds the reload the words "not a key ring" and asserts no key reaches the log. That
  # string has no colon, so it never reaches the branch that writes anything back. The assertion is
  # true of the one input that could not have failed it.
  #
  # The second asserts that the system information answer tells a signed-in caller exactly a named set
  # of things and nothing about keys. It builds that answer leaving the encryption line at its default,
  # and that line is left out of the answer entirely when it is empty. So the very thing the check was
  # written for is not among the things it compares, and a second one added the same way would be just
  # as invisible to it.
  #
  # Both are the same mistake: written against one example rather than against the promise. On this
  # slice the checks are the deliverable, so what these scenarios state is the promises they make.

  @error @us-07 @slice-07
  Scenario: A refusal that carries the key is caught before it can ship
    Given a change that makes a key ring refusal repeat what was supplied
    When the build runs its checks
    Then the build fails
    And the failure names the sentence that carried it

  @property @error @us-07 @slice-07
  Scenario: Every way a key ring can be malformed is one the checks try
    Given the ways a supplied key ring can be malformed
    When the check that nothing written about a key ring carries the key runs
    Then every one of those ways is among the ones it tries
    And it is not satisfied by a single input that could never have failed it

  @error @us-07 @slice-07
  Scenario: A new thing the answer tells a signed-in caller is refused until somebody names it
    Given a new field on the answer an ordinary signed-in caller receives
    When the build runs its checks
    Then the build fails until that field is named as safe to publish

  @edge @us-07 @slice-07
  Scenario: A field left out when it is empty is still one of the things the answer promises
    Given a field that is left off the answer entirely when it carries no value
    When the promise about what the answer tells a signed-in caller is checked
    Then that field is among the things compared
    And leaving it empty is not a way past the promise

  @property @us-07 @slice-07
  Scenario: Which values an example happens to carry cannot change what the answer promises
    Given any way of building that answer, whatever values it leaves at their defaults
    When the promise about it is checked
    Then the set of things compared is the same set every time
