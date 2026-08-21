# Mutation testing — Epic 4365, slice 05 (the named field, on Jira)

Run 2026-08-21, backend only. Config: `stryker.4365.slice05.backend.json`.

| Stack | Result |
|---|---|
| Backend (Stryker.NET) | **100.00 %** — 4 killed, 0 survived, 0 timeout, of 4 tested |
| Frontend (StrykerJS) | not run — this slice changes no frontend file |

`16209 total mutants are skipped` is the whole-project pre-filter count and says nothing about scope.
The line that does is `4 total mutants will be tested`. The `mutate` filter worked, as it did for
slice 04 — slice 03's conclusion that the key is inert in this repo still does not reproduce.

## Read the denominator before the percentage

**100 % of four mutants is a thinner result than slice 04's 93.75 % of forty-eight, and it should be
read that way.** The whole of this slice's new logic is one small pure class, so there is very little
in it to mutate: a negation, a branch, and the two shapes a reference can be stamped with. Every one
was killed, and that is worth having, but nobody should read the headline as four times the assurance
slice 04 bought.

## What is deliberately not covered by this number

The other behaviour this slice changes is inside `JiraWorkTrackingConnector` — the report about
renamed link names now stays quiet for a Portfolio reading a field of its own. That file is over three
thousand lines and Stryker.NET ignores line ranges, so mutating it whole would bury this slice under
untouched code, exactly as slice 04 found for the Azure DevOps connector. It has **no mutants here**.

What stands in for them is better evidence for this particular line, not worse: the gate was removed
on purpose and the test was watched to fail, naming the assertion. A mutant would have told us the
same thing about one operator; the sabotage told us the test detects the whole behaviour going away.

The same discipline was applied to both architecture guards. With a tracker stamping its own
references again, the single-decider rule names the file and the line. The second rule stayed green
under that sabotage, because that tracker still names the decider elsewhere — recorded in the test
itself, so the next reader is not misled about which of the two is load-bearing.

## Scope

Mutated: `Services/Implementation/Dependencies/DependencySourceSelector.cs`.

Not mutated, and why: the three connectors (thousands of lines each, no line-range support); the
reconciler and the honour policy (slice 01 and 02 territory, unchanged here).
