@feature:lighthouse-user-survey @US-06
Feature: The in-app nudge appears only for eligible non-premium users

  Inside Lighthouse, a calm nudge inviting feedback appears only for a non-premium
  instance that has been in use for about two weeks. A brand-new install is never
  nudged on day zero, and a premium instance is NEVER nudged at any age — that is
  the hard guardrail. When the install age or the premium status cannot be trusted,
  the gate fails closed and shows nothing, so a paying customer is never bothered
  by accident. Install age is judged on a stable, timezone-proof clock, so a clock
  that jumps backward can never make a not-yet-eligible instance eligible early.

  @property @driving_port @in-memory @US-06 @slice-04 @pending
  Property: A premium instance is never nudged, at any install age
    Given a premium instance
    When eligibility is evaluated at any install age
    Then the nudge is never shown

  @driving_port @in-memory @US-06 @slice-04 @pending
  Scenario: A non-premium instance in use for about two weeks is nudged
    Given a non-premium instance whose install age is at least about two weeks
    When eligibility is evaluated
    Then the nudge is shown
    And the time the nudge was shown is recorded

  @error @driving_port @in-memory @US-06 @slice-04 @pending
  Scenario: A brand-new install is never nudged on day zero
    Given a non-premium instance whose install age is under about two weeks
    When eligibility is evaluated
    Then the nudge is not shown

  @error @driving_port @in-memory @US-06 @slice-04 @pending
  Scenario: An uncertain premium status fails closed and shows nothing
    Given an instance whose premium status cannot be trusted
    When eligibility is evaluated
    Then the nudge is not shown

  @error @driving_port @in-memory @US-06 @slice-04 @pending
  Scenario: An unknown install age fails closed and shows nothing
    Given a non-premium instance whose install age is unknown
    When eligibility is evaluated
    Then the nudge is not shown

  @error @property @driving_port @in-memory @US-06 @slice-05 @pending
  Property: A clock that jumps backward never makes an instance eligible early
    Given a non-premium instance that is not yet old enough to be nudged
    When the clock is skewed or jumps backward
    Then the instance is still treated as not yet eligible
    And the nudge is not shown

  @error @driving_port @in-memory @US-06 @slice-04 @pending
  Scenario Outline: The two-week threshold decides eligibility at the boundary
    Given a non-premium instance whose install age is "<age>"
    When eligibility is evaluated
    Then the nudge is "<shown>"

    Examples:
      | age                       | shown      |
      | just under about two weeks | not shown  |
      | exactly about two weeks    | shown      |
      | well over about two weeks  | shown      |
