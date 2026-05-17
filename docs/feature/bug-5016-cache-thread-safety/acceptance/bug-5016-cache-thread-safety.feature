Feature: Cache<TKey,TValue> survives concurrent access without data-structure corruption
  As an operator running Lighthouse with multiple teams or background services
  I want concurrent metric requests to never crash with IndexOutOfRangeException
  So that user-facing metric endpoints stay available under normal operating load

  Background:
    Given a fresh Cache<string, string> instance
    And an entry expiry window long enough that no entry expires during the run unless the scenario forces it

  @bug-5016 @regression @in-memory @concurrency
  Scenario: Many threads writing unique keys in parallel do not throw and every value is retrievable
    Given 64 worker threads each holding a distinct key/value pair
    When all 64 threads call Store on the same Cache instance simultaneously
    Then no thread observes an IndexOutOfRangeException or any other exception
    And after the storm has settled, Get returns the expected value for every one of the 64 keys
    And Keys enumerated after the storm contains all 64 keys exactly once

  @bug-5016 @regression @in-memory @concurrency
  Scenario: Concurrent readers and writers on overlapping keys never observe a corrupted entry
    Given 32 writer threads continuously calling Store on a shared pool of 16 keys with rotating values
    And 32 reader threads continuously calling Get against the same 16 keys
    When the readers and writers race for 1 second
    Then no thread observes an IndexOutOfRangeException or any other exception
    And every non-null value returned by Get equals one of the values that was Stored for that key (never a torn or partial value)

  @bug-5016 @regression @in-memory @concurrency
  Scenario: Concurrent Get on an expired key serialises the lazy Remove without crashing
    Given a single key "metric:expired" has been Stored with a 10ms expiry window
    And the clock has advanced past the expiry
    When 32 reader threads call Get on "metric:expired" simultaneously
    Then no thread observes an IndexOutOfRangeException or any other exception
    And every thread receives the default value (the expired entry is treated as absent)
    And after the storm has settled, Keys does not contain "metric:expired"

  @bug-5016 @regression @in-memory @concurrency
  Scenario: Keys enumeration during concurrent Store and Remove does not throw "Collection was modified"
    Given a writer loop continuously calling Store and Remove against a shared pool of 100 keys
    When an InvalidateMetrics-style consumer takes a snapshot via Cache.Keys.Where(prefix-match).ToList() 200 times
    Then no snapshot iteration observes an InvalidOperationException ("Collection was modified during enumeration")
    And no snapshot iteration observes an IndexOutOfRangeException or any other exception
    And every snapshot returns only keys that were Stored at some point during the run

  @bug-5016 @regression @in-memory @concurrency
  Scenario: BaseMetricsService.InvalidateMetrics is safe when called while metric calculations are racing
    Given a Cache populated with 50 entries whose keys follow the pattern "{entityId}_{metric}"
    And background threads continuously calling Store and Get against the same Cache for the same entityId prefix
    When InvalidateMetrics removes every key starting with "{entityId}_" while the background threads run
    Then no thread observes an IndexOutOfRangeException, InvalidOperationException, or any other exception
    And after invalidation completes and background activity stops, no key beginning with "{entityId}_" remains in Cache.Keys

  @bug-5016 @regression @in-memory
  Scenario: Single-threaded behaviour is preserved (parity guard for the fix)
    Given a freshly constructed Cache<string, string>
    When the same single thread calls Store("a", "1", 1 minute), then Get("a"), then Remove("a"), then Get("a")
    Then the first Get returns "1"
    And the second Get returns the default value (null for string)
    And Keys is empty

  @bug-5016 @regression @in-memory
  Scenario: Single-threaded expiry-on-read still removes the entry (parity guard for the fix)
    Given a freshly constructed Cache<string, string>
    And Store("temp", "v", 10ms) has been called
    And the clock has advanced past the expiry window
    When the same thread calls Get("temp")
    Then Get returns the default value (null for string)
    And Keys does not contain "temp"
