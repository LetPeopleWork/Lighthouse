# Slice 05 — Explain the 40–60 s single backend tests

**Goal**: Each of `S8_EmbedEntryPointTests` (62 s case), `CacheTest.ConcurrentReadersAndWriters…` (61 s), `BlockedCountSnapshotMigrationTests…OnPostgres` (51 s) and `DataProtectionKeyRingTest…Redis` (43 s) has a measured cause; the ones waiting on nothing finish in seconds, the rest are allowlisted with that reason.

**Owner story**: US-05. **Effort**: ½ day. **Reference class**: flaky/slow-test triage.

**Kind**: investigation slice. Its output is a recorded cause and verdict per test, plus the fix for each one found waiting on nothing.

**Learning hypothesis**
- Disproves "the 40–60 s tests are waiting on nothing" if each turns out to be container start-up or deliberate concurrency load.
- Confirms it if a timeout, retry or sleep is the cost.

## IN scope
1. Run each class alone locally with `dotnet test --filter "FullyQualifiedName~<Class>"` (`S8_EmbedEntryPointTests`, `CacheTest`, `BlockedCountSnapshotMigrationTests`, `DataProtectionKeyRingTest`); compare with ≥3 CI timing CSVs (is it stable or contention?).
2. Fix wait-bound ones with D7 holding; a fixed test takes under 10 s locally.
3. Record cause per test; feed accepted ones to the slice 06 allowlist.

## OUT scope
- Live-connector categories.
- Replacing Testcontainers.

## Acceptance criteria
AC-05.1, AC-05.2 in `feature-delta.md`.

## Dependencies
None.

## Taste tests
Four tests, one triage; production timings. The deliverable is the recorded cause and verdict for each; fixes follow only where the verdict is "waiting on nothing".
