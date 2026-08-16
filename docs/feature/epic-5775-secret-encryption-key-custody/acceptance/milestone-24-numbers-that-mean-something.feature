Feature: Numbers that mean something (Epic 5775, Slice 06a — US-06a)
  As the administrator reading what a check or a move just reported
  I want to be told only the counts that are not zero, in the right grammar
  So that the one number worth acting on is not buried under four categories of nothing

  # The strongest signal of the verification session, because it is a rule rather than a rewrite: a
  # count of zero is not information. Observed — "Checked 1 stored secrets. 1 on the active key
  # k-2026-08-16-01, 0 on an earlier key, 0 never encrypted, 0 could not be read." Four of those five
  # numbers say nothing happened, and they compete with the one that says something did.
  #
  # The plural is the same defect twice over: both summaries interpolate a count into a hardcoded
  # plural, and so does the warning banner. One Connection with one secret field is the smallest real
  # instance there is, and it is exactly what a first-time operator has.
  #
  # And rotation reports in the vocabulary of a move it did not perform. Rotating an empty instance is
  # legitimate, and what happened was that a key was minted — which the sentence never mentions.

  @edge @us-06a @slice-06a
  Scenario: A count of nothing is not shown at all
    Given a check on an instance whose every secret is on the key in force
    When the result is reported
    Then the categories that are empty are not mentioned
    And the operator reads one fact rather than five

  @property @us-06a @slice-06a
  Scenario: Every category that is not empty is still shown
    Given a check on an instance holding secrets in more than one state
    When the result is reported
    Then every state holding at least one secret is named with its count
    And the counts still add up to the number of secrets checked

  @edge @us-06a @slice-06a
  Scenario: One secret reads as one secret
    Given an instance holding exactly one stored secret
    When a check or a move reports on it
    Then the sentence is written in the singular throughout
    And the same is true of the unprompted warning, which is the sentence most operators will ever read

  @edge @us-06a @slice-06a
  Scenario: Rotation says a key was made
    Given an instance with nothing stored
    When an administrator rotates the key
    Then the report says a new key was made and is now the one in force
    And it says nothing about moving secrets, because none were moved

  @edge @us-06a @slice-06a
  Scenario: Rotation that did move something says both
    Given an instance holding secrets under an earlier key
    When an administrator rotates the key
    Then the report says a new key was made
    And it says how many secrets were moved onto it

  @error @us-06a @slice-06a
  Scenario: A secret nobody could read is always named, however few there are
    Given a check on an instance holding one secret that cannot be read
    When the result is reported
    Then that one is named with the Connection and the field it sits in
    And no rule about hiding empty categories ever hides it
