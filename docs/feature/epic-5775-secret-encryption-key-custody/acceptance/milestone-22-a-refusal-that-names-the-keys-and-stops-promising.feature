Feature: A refusal that names the keys, and stops promising (Epic 5775, Slice 05b — US-05b)
  As the operator reading a FATAL line at the moment everything stopped
  I want to be told which two keys are involved and the remedy that actually fits
  So that I am not asked for a value nobody ever wrote down, on a directory that no longer exists

  # The refusal was observed twice in the walkthrough and was wrong in a different way each time. Both
  # remedies it offers assume the operator LOST a key. The far likelier cause — and the one reproduced —
  # is that they ADDED one: an instance that had been happily minting for months gets a key set for the
  # first time, and the configured key displaces the minted key out of the ring. In that state the key
  # store is already correct and already present, so pointing at it is a no-op, and "set the key this
  # instance was using before" asks for a value Lighthouse generated and kept in a file. The one
  # instruction that works — remove the key you just set — is the one that was missing.
  #
  # And where the key really is gone, the reassurance is false. "Nothing is lost" was true of the
  # instance that merely pointed at the wrong key; it is not true of the one whose key store was
  # destroyed. The message is the same in both cases because the application cannot tell them apart —
  # but it can stop asserting the comfortable one as fact.
  #
  # Both key ids are known and neither is a secret: the ring reports the key it started on, and every
  # stored envelope carries the id of the key that wrote it. Key ids are already on the encryption
  # panel. Naming them turns a puzzle into a diagnosis.

  @error @us-05b @slice-05b
  Scenario: The refusal leads with the remedy an operator can carry out unaided
    Given an instance that started on a supplied key none of its stored credentials were written under
    When Lighthouse refuses to start
    Then the first thing it offers is removing the key that was just set
    And that is first because it is both the likeliest cause and the only remedy needing nothing the operator does not have

  @error @us-05b @slice-05b
  Scenario: It names the key it started on and the key the credentials were written under
    Given an instance that started on a supplied key none of its stored credentials were written under
    When Lighthouse refuses to start
    Then it names the key the instance started on
    And it names the key the stored credentials say they were written under

  @error @us-05b @slice-05b
  Scenario: It stops asserting that nothing is lost as though it knew
    Given an instance whose key store was destroyed rather than misplaced
    When Lighthouse refuses to start
    Then it does not state as fact that nothing is lost
    And it says what is true in both cases: nothing has been changed by this start

  @error @us-05b @slice-05b
  Scenario: It names the way past itself
    Given an instance whose key is genuinely gone
    When Lighthouse refuses to start
    Then the refusal says how to start anyway and re-enter the credentials
    And says plainly what that costs, because an operator who does it by accident has thrown away every credential

  @property @error @us-05b @slice-05b
  Scenario: Naming the keys still repeats no key material
    Given any refusal that now names one or two keys
    When it is written
    Then it contains no part of any key's material
    And a key id is not a key, which is why it can be named at all
