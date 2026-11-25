# Mutation testing — bug-6697 (Features with a default size of 0 show on every team's Features tab)

Run 2026-11-25 against `main` @ `6ae4de036`. Gate is 80 % kill rate on each stack the fix touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (hand-applied mutants) | **100 %** | 6 | 6 | 0 | 0 | about 3 min |
| Frontend | N/A, the fix changes no frontend file | | | | | |

## Backend

The fix is a 5-line guard at the top of `WorkItemService.AssignExtrapolatedWorkToTeams`:

```csharp
if (remainingWork <= 0)
{
    return;
}
```

Each mutant was applied to the guard alone, the tests were run, and the file was restored. M1 and M2 ran
against the oracle (`FullyQualifiedName~ZeroDefaultFeatureSizeTest`). M3 to M6 ran against the Work Items unit
tests (`FullyQualifiedName~Modules.WorkItems|FullyQualifiedName~Models.Feature|FullyQualifiedName~Acceptance.WorkItems`,
567 tests), with warnings not treated as errors, because the build rejects M4 to M6 as unreachable code or an
empty block. Stryker's mutant switching tests them all the same.

| mutant | result | killed by |
| --- | --- | --- |
| M1 `<= 0` → `< 0` | killed | the 3 zero-size oracle cases |
| M2 `<= 0` → `> 0` | killed | all 6 oracle cases |
| M3 `<= 0` → `<= 1` | killed | `WorkItemServiceTest.UpdateFeaturesForProject_UseCalculatedDefault_AddsDefaultRemainingWorkBasedOnPercentileToFeature` (percentile default of 1); the oracle alone does not see it |
| M4 condition forced `false` | killed | the 3 zero-size oracle cases |
| M5 condition forced `true` | killed | the oracle's estimate cases and the default-sizing tests |
| M6 `return` removed | killed | the 3 zero-size oracle cases |

### Closed by this pass

None needed. Every mutant was killed by an existing or oracle test.

### Accepted survivors

None.

### Not mutated

`WorkItemService.cs` as a whole (about 1,500 lines). Stryker.NET cannot limit a run to changed lines, so a
whole-file run would score code this fix does not touch. The maintainer was offered the whole-file run
(20–40 minutes) and the 15-minute default chose the hand-applied mutants above.
