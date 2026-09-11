# Mutation testing — Bug #5973

Stryker.NET, backend only. No frontend file changed in this fix, so no StrykerJS run.

Config: `stryker.5973.backend.json` in this directory, copied from `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.bug-5973-jira-board-query.json` (the working copy is gitignored as local tooling).

## Score

The gate is **95.59 %** — the kill rate on the lines this fix actually changed, against a project
floor of 80 %.

| Scope | First run | After the added tests |
| --- | --- | --- |
| **Lines changed by this fix** | 72.88 % (43/59) | **95.59 % (65/68)** |
| Pre-existing code in the same files | 69.86 % | 72.60 % |
| Whole-file headline | 47.47 % | 61.17 % |

Read the first row and ignore the third. `JiraWorkTrackingConnector.cs` is ~2650 lines and this fix
touched a few hundred of them, so the whole-file number is mostly a measurement of code nobody
edited. Stryker.NET silently ignores line-span `mutate` patterns, so whole-file entries are the only
way to run it here and the scoping has to be done afterwards, by grouping the report's mutants
against `git diff -U0`.

Scope was confirmed from `reports/mutation-report.json`, not from the console. The run prints
`17385 total mutants are skipped` / `495 total mutants will be tested`; the first number is a
pre-filter artefact of Stryker.NET injecting into every file before applying the filter, and looks
alarming in a perfectly healthy run.

## What the first run found that the review did not

The adversarial review passed this code with no findings. The first mutation run then produced 16
survivors on the changed lines, three of which were real holes:

1. **The bracket-depth counter was not pinned at all.** `bracketDepth++` and `bracketDepth--` could
   be swapped, or the decrement deleted, and every test still passed. The only nested-ordering test
   asserted the query was left *whole*, which is also what a broken counter produces — so the test
   could not tell correct from broken. Fixed by a case carrying both a bracketed ordering and a real
   top-level one, where the depth genuinely decides the outcome.
2. **The fallback branch of `ExplanationIn` was never exercised.** Every test fed a response body
   carrying `errorMessages`, so the path that reports the status instead — what a user sees when the
   failure is *not* a JQL complaint — was untested. Now covered for a body without the property, one
   where it is not a list, and one that is not JSON at all.
3. **The user-facing message text was not asserted anywhere.** The headline, the field name that
   tells the UI which input to highlight, and the unreadable-filter explanation could all have been
   emptied to `""` silently. These messages are the whole deliverable for a support case.

Reading confirmed the logic was right. It could not reveal that nothing would notice if the logic
broke. That is the distinction this run was worth.

## Remaining survivors, and why they stand

- `JiraQueryRejectedException.cs:32` and `:36` — both sit on `RejectedQuery`. The property is
  populated at the throw site but **never read anywhere**; `QueryWasRejected` uses only
  `rejection.Message`. Both mutants are therefore unobservable, and a test that killed them would be
  asserting a value no caller consumes. Deleting the property is the honest remedy and is a
  production change, deferred.
- `JiraWorkTrackingConnector.cs:2473` — the transport branch in `TryWalkReleaseMembership`. No test
  distinguishes the Cloud walk from the Data Center walk on that path. Pre-existing shape, untouched
  by this fix.

## A separate defect this run surfaced

`PrepareGenericQuery`'s `options.Any()` guard — the only thing stopping an empty work item type or
state list from emitting an empty JQL bracket pair — survives being forced to always-true, because
nothing tests an empty list. The code's own comment records the blast radius: an empty pair read as
"matches nothing" makes the sweep report no records, and removal is stored-minus-swept, so every
stored record for that team or portfolio would be deleted. Pre-existing and outside this fix's diff,
so it was filed as **Bug #5974** rather than folded in here.

## Notes for the next run

- Two test classes (`JiraDeliverySourceProviderTest`, `JiraDeliveryForecastPublisherTest`) were
  missing from the first run's `test-case-filter`, which reported four lines as `NoCoverage` that
  were in fact covered. Adding them moved those to killed without a line of new test code — a
  reminder that a `NoCoverage` verdict can be a config artefact rather than a gap.
- Three mutants come back `CompileError` every run. The repo's `TreatWarningsAsErrors` plus promoted
  analyzer rules reject the mutated forms outright. They are structural and will recur.
- `ignore-mutations: ["string"]` is set in every other backend config in this repo and is
  deliberately **not** set here. This defect was string assembly — bracket and conjunction literals,
  escape sequences — so string mutants are the ones that matter. `ignore-methods: ["*Log*"]` still
  suppresses the log-message noise that the rule normally exists to avoid.
