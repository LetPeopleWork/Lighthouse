# Mutation testing — Bug #6019

Stryker.NET, backend only. No frontend file changed in this fix, so no StrykerJS run.

Two configs in this directory, because the surface splits in two:

- `stryker.6019.backend.json` — the refusal plumbing: the port exception type, `UpdateServiceBase`,
  and the two updaters.
- `stryker.6019.connector.json` — `JiraWorkTrackingConnector.cs` on its own, because at ~2800 lines
  it swamps a combined run.

Runner: `run-backend-x64.ps1`. Stryker hands VSTest `TargetPlatform=X64`, and on this win-arm64
machine an x64 test host launched by the arm64 VSTest discovers nothing — Stryker then reports
"Number of tests found: 0" and blames the NUnit adapter, which is not the problem. The script runs
the whole chain as x64.

## Score

The gate is the kill rate **on the lines this fix changed**, against a project floor of 80 %.

| Scope | First run | After the added tests |
| --- | --- | --- |
| **Lines changed by this fix** (refusal plumbing) | 77.27 % (17/22) | **100.00 % (22/22)** |
| Whole-file headline, same config | 61.07 % | 64.43 % |
| **Lines changed by this fix** (the connector) | 87.50 % (14/16) | **100.00 % (27/27)** |
| Whole-file headline, connector config | 59.75 % | 59.92 % |

Read the first row. The whole-file number spans pre-existing code in the same files that nobody
edited, so it measures the repo's history rather than this change.

Scope came from `reports/mutation-report.json`, not the console. The run prints
`18906 total mutants are skipped` / `131 total mutants will be tested` — the first number is a
pre-filter artefact of Stryker.NET injecting into every file before applying `mutate`, and it looks
alarming in a perfectly healthy run.

**No `{a..b}` line-range was used.** `docs/ci-learnings.md` records (recurrence 2, still true on
4.16.0) that the range suffix silently mutes every mutant and reports a vacuous pass. Whole-file
`mutate` entries are the only trustworthy way to run this, and the scoping is done afterwards by
grouping the report's mutants against `git diff -U0`.

## What the first run found that the adversarial review did not

The independent review had already rejected this change once, on a real blocker — the rejected query
was appended to the summary line untruncated. It passed everything else. The mutation run then found
five survivors on the changed lines, and four of them were genuine holes:

1. **The exactly-at-the-limit boundary was untested.** `query.Length <= LongestReportedQuery`
   survived mutation to `<`: a query of precisely 500 characters would be cut and given a `…` for
   nothing left out, and no test noticed. The review had asked about this boundary specifically and
   been told it was fine.
2. **The surrogate-pair guard was pinned only where it could not fail.** Both arithmetic mutants on
   the cut adjustment survived. The existing test's fixture was `499×'x'` then twenty rockets, so
   under one mutant the probed index was *still* a high surrogate and the cut did not move, and under
   the other the cut kept a whole rocket — valid UTF-8 either way. The fixture was rebuilt as
   `499×'x' + 🚀 + 600×'y'`, where index 501 is a plain character and both mutants change the result.
3. **The team updater's `throw;` could be deleted with the suite still green.** Killed 0 of 1. The
   cross-surface test compared only the *reason text*, which a deleted rethrow leaves intact — so the
   acceptance criterion "the same holds for a team refresh" was satisfied in wording and not in
   behaviour. The team now has its own refusal tests, mirroring the portfolio's.
4. **`ExplanationNamesTheQuery`'s empty-query guard.** `Length > 0` → `>= 0` looked like an
   equivalent mutant and was not: with no query, the mutant makes the exception claim its explanation
   already names a query it does not carry, which a future consumer would act on by leaving the query
   out. One-line test, no ignore needed.

Each of the five was verified the honest way — the mutation applied by hand to the production
source, the expected single failure observed by name, then reverted. A test added for a survivor
that stays green under that survivor's own mutation has closed nothing, and this feature had already
produced three tests that were green while guarding nothing.

Worth recording for the next run: mutant 4 **does not compile** locally. SonarAnalyzer S3981 ("the
`Length` of `String` always evaluates as `True`") is an error under `TreatWarningsAsErrors`, so
verifying it by hand needed a temporary `#pragma warning disable S3981`. Stryker reports such forms
as `CompileError` rather than as survivors; they are structural and will recur.

## What the connector run found: a hole one level down from the bug itself

The connector's two survivors sat on one line — `sentences.Count > 0 ? string.Join(…) : null` — and
both changed behaviour only for a refusal whose `errorMessages` is present but yields no sentence.
Under either mutant that returns `""` rather than null, so the caller's `??` fallback never fires and
the operator is shown an **empty** explanation where the status alone would have said more. Nothing
in the suite noticed, which meant nothing covered an empty `errorMessages`.

Chasing that fixture turned up the production gap it was hiding. Jira answers a 400 about one named
thing — a project, a status or a field that does not exist on the instance — with `errorMessages`
**empty** and the sentence under a sibling `errors` object, keyed by that name. `WhatJiraSaidAbout`
read only `errorMessages`, so Lighthouse reported "Jira answered 400 BadRequest without saying why"
about a refusal where Jira had said exactly why. That is the defect #6019 was opened for, one level
down. `JiraReleaseVersionReader.ReadRefusalMessage` in the same namespace already reads both halves
and in that order, so the shape was settled in this codebase and only the connector had missed it.

A third defect fell out of writing the test for it: a refusal body that parses as JSON but is not an
object — a bare array, which is what a gateway in front of Jira can answer with — made
`TryGetProperty` throw `InvalidOperationException`, and the verdict came back `validation_failed`
carrying a .NET sentence instead of `query_rejected` carrying Jira's. Confirmed by observation, not
by reading: the test failed with "The requested operation requires an element of type 'Object'".

## Remaining survivors on changed lines

None, for either config.

Five mutants on lines changed by the refusal plumbing come back `Ignored` from pre-existing inline
Stryker directives in those files; no ignore was added by this work, and neither config carries an
`ignore-mutations` entry of its own.

The connector run's second pass left two survivors, both on the newly added `errors` reading, and
both were real: the `??` order between the two halves (nothing carried a sentence in both at once,
so reversing which one wins was invisible), and `&&` → `||` on the `errors` guard (nothing answered
with a non-object under that name, so dropping the guard threw instead of falling back). One test
each, both verified by applying the mutation by hand and watching the one expected test fail by name.
