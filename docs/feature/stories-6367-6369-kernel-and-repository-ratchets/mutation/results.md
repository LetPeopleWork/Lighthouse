# Mutation testing — 6367 + 6369 (shared kernel and repository ratchets)

Run 2026-10-27 in the worktree at `6cba75d8b`..`47819c052`, before the rebase onto `main`. The gate is an
80 % kill rate on each stack the feature touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, full run (Stryker.NET) | **84.95 %** | 92 | 78 | 13 | 1 | 13 m 01 s |
| Backend, after the kill round (full-run mutants) | **91.30 %** | 92 | 83 | 8 | 1 | — |
| Frontend | N/A: this feature changes no frontend file | | | | | |

The second row is not a separate run. It is the full run's 92 mutants, of which the kill round's new tests
were each proved, by applying the mutation by hand, to kill five more: 78 + 5 killed, 1 timeout.

Configs: `stryker.6367-6369.backend.json` (the full run) and `stryker.6367-6369.backend.killround.json` (the
narrowed check after the kill round: the two files with survivors, only their own test classes: 87 mutants,
83.91 %, 2 m 56 s). The narrowed check reports four more survivors than the full run (BlackoutDaysExtensions
L21, EncryptionKeyRing L63 and L74), because its filter leaves out the bootstrapper, holder and forecast
tests that kill them in the full run.

## Backend

| file | tested | killed | survived (full run) | after the kill round |
| --- | --- | --- | --- | --- |
| `Models/BlackoutDaysExtensions.cs` | 59 | 46 | 11 + 1 with no coverage | 9 equivalent left |
| `Models/Encryption/EncryptionKeyRing.cs` | 28 | 26 | 2 | 0 |
| `Services/Implementation/Encryption/LegacyDefaultEncryptionKey.cs` | 6 | 6 | 0 | 0 |
| `Models/Reports/ReportOwner.cs` | 0 | — | — | — |

### Closed by this pass

- BlackoutDaysExtensions L91 (`p.End >= rangeStart` to `>`): a blackout that ends on the first day of the range
  overlaps it.
- BlackoutDaysExtensions L97 (no coverage) and L102: a chart without data points, and a chart with no blackout
  periods, come back as the same chart instance.
- EncryptionKeyRing L48: the ring refuses a missing key passed as retired.
- EncryptionKeyRing L68: the refusal to drop the active key explains that nothing written under it could be
  read afterwards (only the stable part of the message is pinned; it is developer-facing).
- A further test covers marking only the chart points that fall inside a blackout, a path no test reached.

### Accepted survivors (equivalent mutants)

- L18 `<` to `<=` and L19 `>` to `>=`: at equality both branches give the same date.
- L19 clamp forced off: the loop guard `i < totalDays` on L29 already stops at the end of the range.
- L23 `continue` removed: for a period with no overlap the loop range is empty, so the body never runs.
- L29 `<` to `<=`: the L19 clamp keeps the end index at most `totalDays - 1`.
- L51/L52 (`<= 0` to `< 0`, early return removed): with no working days the loop never runs and adding zero
  days returns the start date.
- L76/L77 (same shape for calendar days): with zero days the count is zero; with a negative span the overlap
  check skips every period and the negative count comes back unchanged.

### Not mutated

The other moves (`IEntity`, `IConcurrencyTokenEntity`, `WorkTrackingSystems`, `UpdateType`) are interfaces
and enums: Stryker makes no mutants from them. The architecture tests and their helpers (`ProductionSource`,
`IlDependencies`, `KernelRules`) are test code, not production code, and are proved instead by the
fixture tests that run each rule against a deliberately offending and a clean set of types.
