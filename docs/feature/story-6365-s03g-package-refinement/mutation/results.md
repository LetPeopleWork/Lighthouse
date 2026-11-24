# Mutation testing — 6365 (S03g Package Refinement)

Run 2026-11-24 in the worktree at `10cbbc96e` (feature C closed, code frozen), Stryker.NET 4.14.1 on Linux x64, in
Stryker's own sandbox (never in place). Bar (D-11): at least 80 % of the moved code, **and** no survivor or no-coverage
mutant added by the move. The stricter of the two holds.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend, `Modules/Refinement/**` | **97.84 %** | 231 | 226 | 5 | 0 | 29 m |
| Frontend | N/A — no frontend file changed | | | | | |

"Tested" counts killed + survived + no coverage over the 16 mutated files. Stryker also marked 104 mutants as compile
errors (Safe Mode) and ignored 926 as covered by a block mutant; the 23,886 mutants outside `Modules/Refinement` were
skipped by the `mutate` globs.

Config: `stryker.6365.backend.json` (the two `mutate` globs `Modules/Refinement/{Domain,Application}`). Test filter: the
Refinement unit tests (`Tests.Modules.Refinement`), the port-level Refinement scenarios (`Tests.API.Integration.Refinement`,
without the three classes that start a web host: `Slice11CastAVoteScenarios`, `RefinementAcceptanceTest`,
`SizingVotesAcceptanceTest`), the Refinement controller tests and `SizingLogRepositoryTest`; no dogfood, no write-back, no
live connector category (1,316 tests, 19 s on their own).

## Survivors

The third column is a reading of the code, not checked by a test.

| File:line | Mutation | Likely reason it survives |
| --- | --- | --- |
| `Application/IRefinementViewQuery.cs:72` (×2) | `RowConversation.None = new(false, false)` → `true` for either flag | The empty conversation is never read for its flags where it is the default; a row with no votes reports no discussion through another path. |
| `Application/RefinementCadenceCalendar.cs:86` | `anchorWeek > tomorrow` → `>=` | Equivalent: when the two dates are equal both branches return the same date. |
| `Application/RefinementCalendar.cs:13` | the `cadence is null` early return removed | The callers check for a cadence before they ask, so the guard is not reached with null in the tested paths. |
| `Application/RefinementResolution.cs:138` | `voterKey is null ? null : …` → always the `Find` | `Find` with a null key matches no vote, because every stored vote has a key; the guard only saves the search. |

No kill round: the score is far above the bar, and each survivor is either equivalent or guards a path the callers never
take. They are recorded here rather than killed, as the mutation gate allows.

## Against the D-11 bar

**Kill rate: met at 97.84 %.**

**No survivor or no-coverage mutant added by the move: met, shown mechanically.** For each of the 16 production `.cs` files
that moved into `Modules/Refinement` (git renames from `8e0b2fe4e`, the base of the Story), the content without `using` and
`namespace` lines, blank lines and carriage returns is **identical to the pre-move file in 16 of 16**. The move changed
only namespaces and usings, so no mutant can be new, and each survivor above is one the pre-move file had. A
mutant-by-mutant comparison against a run on the base was therefore not made.

Command used for the proof, per pair: `git show 8e0b2fe4e:<old> | tr -d '\r' | grep -vE '^\s*(using |namespace |$)' | md5sum`
against the same filter on the new file at `HEAD`.
