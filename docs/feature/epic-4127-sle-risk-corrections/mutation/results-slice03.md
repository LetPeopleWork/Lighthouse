# Mutation testing — slice 03 (ADO Story #6035)

Both stacks run scoped to the files this slice changed, and run **sequentially** — an overlapping
run at 8 GB of node heap produces a result that is 100% `Timeout` and means nothing.

| Stack | Target | First run | After closing the gaps | Gate |
|---|---|---|---|---|
| Frontend | `utils/charts/sleRisk.ts`, `hooks/useEnlargedWorkItemsDialog.ts` | 92.50% (74/80) | **96.25%** (77/80) | ≥ 80% ✓ |
| Backend | `Services/Implementation/SleRiskCalculator.cs` | 78.95% (15/19) | **84.21%** (16/19) | ≥ 80% ✓ |

`sleRisk.ts` finished at **100%** — 64 of 64.

Configs: `Lighthouse.Frontend/stryker-config.sleRiskSlice03.json` +
`vitest.stryker.sleRiskSlice03.ts`, and
`Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.sle-risk-slice03.json`. The frontend
pair is gitignored as local tooling; the copies here are the reproducible record.

## The three gaps mutation found, which nothing else did

The adversarial review of the same eight commits returned **zero findings** — it checked design
compliance, and every one of these is a hole in a test's strength rather than in the code's shape.
Worth recording as the reason both gates exist.

### 1. The trap test was vacuous, and it was the slice's own centrepiece

`says the same about a certain item as about a safe one` asserts that the disclosure for a `100%`
row with an empty history is byte-identical to the one for a `0%` row with an empty history. It
compares the two answers **against each other** — which is satisfied by two of anything, including
two `undefined`s. Emptying `disclosureFor`'s whole body to `{}` therefore survived, and so did
flipping its `answer === undefined` check to always-true.

This is the same failure mode the test was written to avoid, one level up: DISTILL rejected a
word-banning assertion as "a negative assertion over an open set" and chose invariance instead, and
invariance turns out to need an anchor of its own. Fixed by `hands a row's own count to the
sentence`, which pins one real sentence from the real factory. Both mutants die.

### 2. The storage key was pinned by behaviour, which pins nothing

Every test of the enlarge toggle writes and reads the choice through the same hook, so all of them
pass against **any** key — including `""`. The mutant replacing
`"lighthouse:workItemsDialog:enlarged"` with the empty string survived.

That is exactly the change the key's naming argument exists to prevent: a renamed key silently
forgets every viewer's choice, and this repository already carries one key named after a feature
that no longer exists for that reason. Fixed by `remembers the size under a key named for the size`,
which asserts the literal.

### 3. The new backend method's age guard had no test

`For` refuses an age of zero or less and has `For_AgeThatCannotBeRead_Refuses` to say so.
`FinishedItemsStillOpenAtThisAge` refuses the same way and had nothing. Deleting its
`ThrowIfNegativeOrZero` survived — and the consequence is not an exception type, it is that an age of
zero silently counts every item the team ever finished and the cell claims a depth of evidence that
describes nothing. Fixed by `FinishedItemsStillOpenAtThisAge_AgeThatCannotBeRead_Refuses`.

## The four survivors that remain, all equivalent

| Where | Mutant | Why it is equivalent |
|---|---|---|
| `SleRiskCalculator.For:27` | `ArgumentNullException.ThrowIfNull(closedCycleTimes)` → `;` | The next statement is a LINQ `Count` over that same argument, which throws `ArgumentNullException` itself. The guard documents the contract; it does not change the outcome. **The ledger's standing rule, now at recurrence 3** — do not test it, do not delete it to raise a score |
| `SleRiskCalculator.FinishedItemsStillOpenAtThisAge:78` | the same guard, same method shape | Same |
| `SleRiskCalculator.For:50-52` | `{ return 0; }` → `{}` | Reaching that line means no finished item ran as long as this one. The certainty rule above has already established the age is at most the target, so anything that ran longer than the target also ran at least as long as this item — which means the breach count is necessarily zero too. The fallthrough computes `100.0 * 0 / 0`, and .NET saturates the resulting `NaN` back to `0` on the cast. Same answer by a worse route. Recorded rather than tested: the test would be asserting a property of NaN conversion, not of the risk |
| `useEnlargedWorkItemsDialog.ts:52` | `useCallback` deps `[]` → `["Stryker was here"]` | A dependency array only decides when the callback's identity is recreated. The body is unchanged and nothing here depends on that identity |

## What this run cost, and the trap that nearly ate it

Stryker.NET printed **`19092 mutants created`** and it looks exactly like a `mutate` filter that was
ignored. It is not — it is the pre-filter count, and the line that actually proves scope arrives
about two minutes later:

```
19073 total mutants are skipped for the above mentioned reasons
19    total mutants will be tested
```

Nineteen, all in `SleRiskCalculator.cs`. This is already in the ledger and was still nearly
re-derived here by killing a correct run. Never judge scope from the created-count; read the scope
line, and confirm from `reports/mutation-report.json`.
