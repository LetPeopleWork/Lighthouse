# Mutation testing: 6500 (a full disk reads as plain language in Recent problems)

Run 2026-11-04 against `main` @ `dcbf50757` plus the fix. Gate is 80 % kill rate on each touched stack.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **96.77 %** | 31 | 30 | 1 | 0 | 2 m 35 s |
| Frontend | N/A, because no frontend file changed | | | | | |

Config: `stryker.6500.backend.json`.

## Backend

Stryker.NET mutates whole files, so the run covers all of `RecentProblemsSink.cs`, not only the change.

| lines | killed | survived | ignored | compile error |
| --- | --- | --- | --- | --- |
| changed (`AsProblem`, `DiskFullSentenceFor`) | 6 | 0 | 2 | 0 |
| rest of the file | 24 | 1 | 11 | 20 |

The two ignored mutants on the changed lines are block removals dropped by Stryker's "block already covered"
filter. The 20 compile errors are all in code this Bug did not touch, so those mutants were never tested.

### Accepted survivors

- Line 164, `numberAt < kindAt` to `numberAt <= kindAt`: untouched code, and equivalent, because the two positions
  are of different placeholders in one template and can never be equal.

### Not mutated

Nothing else: the sink is the only production file changed.
