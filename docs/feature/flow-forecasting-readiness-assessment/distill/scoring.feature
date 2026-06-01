@feature:flow-forecasting-readiness-assessment @US-02 @slice-01
Feature: Compute and normalize the score, assign the correct band

  Six 0-to-3 answers sum to a raw total of 0 to 18, which normalizes to a
  memorable 0-to-100 score by rounding the raw total over eighteen times one
  hundred. Each score falls into exactly one of four named bands whose ranges
  are exhaustive and non-overlapping: Flying blind (0-25), Drifting
  (26-50), Flow-aware (51-75), Predictable (76-100). Scoring is the headline
  contract — it is exercised as a property over every possible answer vector,
  with boundary examples pinned for review.

  @property @pure @slice-01 @pending
  Property: Every answer vector yields a deterministic score and exactly one band
    Given any combination of six answers each from 0 to 3
    When the result is computed
    Then the score equals the raw total over eighteen times one hundred, rounded
    And the score is between 0 and 100 inclusive
    And the score falls within exactly one of the four band ranges
    And the band name is the one whose range contains the score

  @property @pure @slice-01 @pending
  Property: Higher answers never produce a lower score
    Given two answer vectors where every answer in the second is at least the first
    When both results are computed
    Then the second score is greater than or equal to the first

  @pure @slice-01 @pending
  Scenario Outline: Boundary scores land in the documented band
    Given an assessment that normalizes to a score of <score>
    When the result is computed
    Then the band is "<band>"

    Examples:
      | score | band           |
      | 0     | Flying blind   |
      | 25    | Flying blind   |
      | 26    | Drifting |
      | 50    | Drifting |
      | 51    | Flow-aware     |
      | 75    | Flow-aware     |
      | 76    | Predictable  |
      | 100   | Predictable  |

  @pure @slice-01 @pending
  Scenario: The lowest possible answers map to the lowest band
    Given all six answers are 0
    When the result is computed
    Then the score is 0
    And the band is "Flying blind"

  @pure @slice-01 @pending
  Scenario: The highest possible answers map to the highest band
    Given all six answers are 3
    When the result is computed
    Then the score is 100
    And the band is "Predictable"

  @pure @slice-01 @pending
  Scenario: A representative middling result lands in the middle band
    Given the six answers sum to a raw total of 9
    When the result is computed
    Then the score is 50
    And the band is "Drifting"
