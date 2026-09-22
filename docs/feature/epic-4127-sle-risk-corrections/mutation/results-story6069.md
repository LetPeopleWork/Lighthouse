# Mutation testing — Story #6069 (SLE risk tooltip states the second count)

Run 2026-09-22 on frozen code, after the adversarial review fixes landed.

| Stack | Mutants | Killed | Survived | Score | Floor |
|---|---|---|---|---|---|
| Backend (`SleRiskCalculator.cs`) | 22 | 20 | 2 | **90.91%** | 80% |
| Frontend (`sleRisk.ts`) | 91 | 91 | 0 | **100.00%** | 80% |

Configs: `stryker.story6069.backend.json`, `stryker.story6069.frontend.json`,
`vitest.stryker.story6069.ts`, all beside this file.

Stryker.NET must be run from `Lighthouse.Backend.Tests/`, not from the project under test — it
resolves `test-projects` against the working directory, and running it from the wrong one fails with
a misleading "No .csproj or .fsproj file found" naming a path that was never meant to exist.

The .NET run reports "19394 total mutants are skipped" before it reports the 22 in scope. That is
the pre-filter count and looks exactly like a broken `mutate` glob; the scope line arrives after it.

## What the first run found

The backend opened at **77.27%** — below the floor — with five survivors. Three were real gaps and
were closed; the two that remain are recorded below rather than argued away.

**The `ThrowIfNull` guards (three of them) could not be killed by exception type.** Counting over a
null sequence raises `ArgumentNullException` on its own, because that is what `Enumerable.Count`
does for a null `source`. A test asserting only the type therefore passes with the guard deleted,
and reports a method that validates nothing as one that validates. The discriminator is `ParamName`:
ours says `closedCycleTimes`, LINQ's says `source`. Two tests now assert it.

`For`'s own null guard had a sharper kill available. The certainty rule answers without reading the
history at all, so `For(11, 10, null)` is the one path that would cheerfully return 100 over a
history that is not there. That is now its own test.

**The frontend's two survivors were one guard**, `sleRangeInDays <= 0`, which nothing reached with a
zero — only with `undefined`. Zero is how a team stores "no target", so it arrives as a number, and
letting it through would draw a column promising "the 0 day SLE". One test closed both.

## The two survivors that remain

Both are equivalent under every input the system can produce. Neither is a missing test.

**`ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ageInDays)` in `For`** is dead code, and
deliberately kept. An age of zero or less can never be past a target of one or more, so `For` always
reaches the delegated count — which re-guards the same value and throws the same exception with the
same parameter name. No input can distinguish the guard from its absence. It stays because deleting
a public method's argument check on the grounds that some other method currently happens to cover it
makes validation depend on an implementation detail one refactor away from changing.

**The `if (comparableItems == 0) { return 0; }` block** survives on an accident rather than on
reasoning. Remove it and the method divides by zero: `0.0 / 0` is `NaN`, `Math.Round(NaN)` is `NaN`,
and the conversion to `int` yields 0 on this platform — which is the answer the tests expect. Killing
it would need a case with no comparable items but at least one breach, and that case cannot exist:
any finished item that ran longer than the target also ran at least as long as an item whose age is
at most the target, so it is always comparable. The block is kept because the CLR's conversion of
`NaN` to `int` is unspecified and need not be 0 everywhere.
