Feature: A token refresh that lands in the middle of a rotation loses nothing (Epic 5775, Slice 03 — US-03)
  As the administrator rotating the key while the instance is still syncing
  I want a refresh landing mid-pass to keep the token it just obtained
  So that containing an exposure never costs a credential that has to be re-authorised with the tracker

  # A rotation walks the same stored tokens a refresh rewrites when one is near expiry. The hazard is one
  # direction only: the rotation writing a re-encryption of the token it read *before* the refresh over
  # the newer token the refresh just obtained. That is a credential nobody can recover without going back
  # to the work tracking system for a new one — exactly the cost this work exists to remove.
  # The answer is not a lock, and could not be: nothing an instance holds in memory coordinates a second
  # replica. The answer is that a move names the value it observed, so a value somebody else has since
  # rewritten is simply not moved — and it does not need to be, because every write already uses the key
  # in force. Losing the race is a row that arrived by another route (ADR-151).
  # Measured 2026-08-16 against real SQLite and a real PostgreSQL: both honour it.

  @real-io @adapter-integration @us-03 @slice-03
  Scenario: A refresh that lands mid-rotation keeps the token it obtained
    Given a rotation that has read a stored token and not yet written it
    And a refresh that obtains a new token for that same Connection and stores it
    When the rotation writes
    Then the stored token is the one the refresh obtained
    And it is readable under the key now in force
    And nobody had to re-authorise that Connection

  @edge @us-03 @slice-03
  Scenario: A secret somebody else rewrote is counted as already moved, not as a failure
    Given a stored secret that was rewritten between the rotation reading it and the rotation writing it
    When the rotation reaches that secret
    Then the pass does not stop
    And the secret is not counted as one that could not be read
    And the pass carries on to the rest

  @property @us-03 @slice-03
  Scenario: The pass never writes over a value it did not read
    Given any stored secret and any other writer touching it during a pass
    When the pass writes
    Then it writes only where the stored value is still the one it read
    And this holds on each database Lighthouse supports

  @edge @us-03 @slice-03
  Scenario: A secret the database would not let go of this time is taken by the next run
    Given a stored secret the database declined to hand over while the pass was running
    When the pass finishes
    Then it is not reported as a secret that could not be read
    And running the rotation again moves it
