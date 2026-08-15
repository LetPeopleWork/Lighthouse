Feature: A scheduled refresh that could not read a credential says which one (Epic 5775, Slice 01 — US-01, OQ-5)
  As a platform operator
  I want the refresh record for a failed scheduled sync to name the connection and the field whose
  credential could not be read
  So that I fix the key I am missing instead of hunting a token the work tracking system never rejected

  # Driving port = the periodic update pipeline. Driving adapter = the refresh record the operator
  # reads through the system information surface.
  # What already exists, verified 2026-08-15: both updaters wrap their work in try/finally rather than
  # try/catch, so the finally runs before the exception propagates and a failed refresh already
  # persists a refresh-log row marked unsuccessful and already emits its summary line. That record is
  # already served to the operator. So the failure surface is NOT missing — only the wording is, and
  # the wording is this milestone's whole subject.
  # Known and deliberately out of scope: after that finally, the exception is swallowed higher up, so
  # the live update status still reports the run as completed and the browser is told it succeeded.
  # That mismatch is not credential-specific — a work tracking system outage produces exactly the same
  # disagreement between the refresh record and the live status. Making the status honest about
  # unreadable credentials alone would leave it dishonest about every other failure, so it is recorded
  # as a defect in its own right rather than fixed here.
  # These run as backend NUnit tests (updater unit tests + WebApplicationFactory integration).

  @error @real-io @driving_port @us-01 @slice-01
  Scenario: The refresh record for an unreadable credential names the connection and the field
    Given a Team whose connection holds a credential that can no longer be read
    When the scheduled refresh for that Team runs
    Then the refresh is recorded as unsuccessful
    And the record names the connection and the field holding the unreadable credential

  @error @driving_port @us-01 @slice-01
  Scenario: The record says the credential could not be read, not that it was refused
    Given a Team whose connection holds a credential that can no longer be read
    When the scheduled refresh for that Team runs
    Then the record says the stored credential could not be read
    And it does not present the failure as the work tracking system rejecting the credential

  @error @driving_port @us-01 @slice-01
  Scenario: A Portfolio refresh names its unreadable credential the same way a Team refresh does
    Given a Portfolio whose connection holds a credential that can no longer be read
    When the scheduled refresh for that Portfolio runs
    Then the refresh is recorded as unsuccessful
    And the record names the connection and the field holding the unreadable credential

  @error @driving_port @us-01 @slice-01
  Scenario: No call is made to the work tracking system with a credential that could not be read
    Given a Team whose connection holds a credential that can no longer be read
    When the scheduled refresh for that Team runs
    Then no request is sent to the work tracking system for that connection

  @regression @us-01 @slice-01
  Scenario: A refresh whose credentials all read still succeeds and records nothing about encryption
    Given a Team whose connection holds credentials that can all be read
    When the scheduled refresh for that Team runs
    Then the refresh is recorded as successful
    And the record says nothing about credentials being unreadable

  @regression @us-01 @slice-01
  Scenario: A refresh that fails for a reason unrelated to credentials reads as it does today
    Given a Team whose refresh fails because the work tracking system is unreachable
    When the scheduled refresh for that Team runs
    Then the refresh is recorded as unsuccessful
    And the record attributes the failure to the work tracking system being unreachable
    And it says nothing about a credential being unreadable
