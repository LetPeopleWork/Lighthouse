# Mutation testing: not applicable (2026-11-02)

The story changes test code only, plus one production line: `Program.ConfigureHealthChecks` went from
`private static` to `internal static` so a test can call it. Stryker.NET mutates production
expressions and statements; an access modifier is neither, so there is no mutant to generate, and
the test project is not a mutation target. A run would report no mutants for the changed file.

What stands in for it on the test side: every guard ships with planted offenders that must be found
and planted non-offenders that must not, and each detection was mis-wired on purpose once to show
the planted cases fail (the evidence is in `feature-delta.md`, slice 4 and 5 review blocks and the
05-04 report). Each converted fixture with state worth leaking got a named pair that fails when the
per-test reset is removed (command counter, clock, rate-limit address).
