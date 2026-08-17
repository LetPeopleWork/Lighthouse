Feature: A refusal that cannot quote the key (Epic 5775, Slice 07 — US-07)
  As the operator who got the key ring slightly wrong
  I want to be told which entry is at fault without the key being written down anywhere
  So that a typo costs me a restart rather than a rotation

  # Scenario 114 in milestone-17 already specifies this property — "no refusal repeats a byte of the
  # key it refused" — and the shipped build does not hold it. The specification was right; what was
  # translated into a test was narrower than what was written, and the branch it missed is the one that
  # leaks. These scenarios say the same thing again in the shapes that actually reach it.
  #
  # The mechanism: an entry is split at its first colon and everything before it is taken to be the key
  # name. When that text cannot be a name, the refusal quotes it back whole. Base64 key material can
  # never be a name — wrong alphabet, wrong length — so every supplied value that puts material before
  # a colon has its material quoted into the sentence, and that sentence is written to a log, to a
  # console, and to whatever a cluster ships its logs to.
  #
  # Naming the offending text is worth keeping: an operator with a mistyped name has to find it. What
  # cannot be kept is quoting it at whatever length it happens to be, because that is what lets 44
  # characters of key through a sentence written for a 12-character name.

  @error @driving_port @us-07 @slice-07
  Scenario: A key written before its name is refused without the key being repeated
    Given a supplied key ring whose first entry is the key material followed by a colon and a name
    When Lighthouse starts
    Then it refuses to start
    And the refusal says which entry is at fault
    And the refusal contains no part of the key material in any encoding

  @error @driving_port @us-07 @slice-07
  Scenario: A keys file written one key to a line is refused without the key being repeated
    Given a mounted keys file holding one key on its first line and a named key on its second
    When Lighthouse reads that file
    Then the keys in force are left exactly where they are
    And the reason is said once
    And nothing said about it contains any part of either key

  @error @edge @us-07 @slice-07
  Scenario: A key left with a colon after it is refused without the key being repeated
    Given a supplied key ring whose only entry is a key with a stray colon after it
    When Lighthouse starts
    Then it refuses to start
    And the refusal contains no part of the key material

  @edge @us-07 @slice-07
  Scenario: A mistyped key name is still named, so it can be found
    Given a supplied key ring whose first entry is named with characters a key name may not use
    When Lighthouse starts
    Then the refusal names what was typed
    And it says which characters a name may use

  @property @error @us-07 @slice-07
  Scenario: Nothing a refusal names is longer than a name is allowed to be
    Given any supplied entry whose name cannot be used
    When Lighthouse refuses it
    Then whatever the refusal quotes is no longer than the longest name a key may have
    And a value long enough to be key material is therefore never carried whole

  @property @error @us-07 @slice-07
  Scenario: No sentence about a key ring carries the key, however the key is written down
    Given any of the ways a supplied key ring can be malformed
    When Lighthouse refuses it, whether at a start or on a reload
    Then no rendered sentence contains the key material
    And no structured property beside that sentence contains it either
    And neither base64 nor hexadecimal nor any other encoding of it appears

  @error @driving_port @us-07 @slice-07
  Scenario: A refusal read from a console says no more than one read from a log
    Given a supplied key ring that stops the start
    When the failure is written to the console and to the log
    Then both carry the same sentence
    And neither carries anything the other does not
