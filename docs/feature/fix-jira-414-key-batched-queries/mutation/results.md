# Mutation testing — Bug #6315 (Jira Cloud search over POST)

Stryker.NET 5.0.0, backend only. Run 2026-10-16 against `3e72392d6`, on a clean tree, 10 m 43 s wall
clock. No frontend file changed in this fix, so there is no StrykerJS run.

Config: `stryker.6315.backend.json`. It mutates `JiraWorkTrackingConnector.cs` on its own, as the
`fix-jira-dc-portfolio-refusal` precedent did. The test filter takes every Jira unit test and leaves
out the live connector categories (`Integration`, `JiraIntegration` and the other connectors), the
`*IntegrationTest` classes, `API.Integration` and the `Dogfood` suites. Before the run, the same filter
was run through `dotnet test`: 397 tests, all green, and none of them from `JiraWorkTrackingConnectorTest`,
which talks to a real Jira instance and carries `JiraIntegration` at class level.

## Score

The gate is the kill rate **on the lines this fix changed**, against a project floor of 80 %.

| Scope | First run | After a kill round |
| --- | --- | --- |
| **Lines changed by this fix** (`git diff -U0 a76fe2eb6..HEAD`) | **100.00 % (2/2)** | not needed |
| Whole-file headline, same config | 52.04 % (408/784) | not needed |

Whole-file breakdown: 398 killed, 10 timeout, 222 survived, 154 no coverage, 225 ignored, 229 compile
error. That number covers about 2800 lines of connector code that this fix never touched, so it
describes the file's history rather than this change. The changed-line row is the gate.

The scope was taken from `reports/mutation-report.json` rather than the console. A mutant counts as
on a changed line when any line it spans is in the new side of the diff: lines 19, 55–59, 63–64,
67–73, 281–282, 349–350, 374–375, 2382, 2384 and 2407–2417. The log reported `21511 mutants created`
and `630 total mutants will be tested`. The first number is the whole backend before `mutate` is
applied; the second is this file. No `{a..b}` range was used, for the reason the precedent gives: the
numbers are character offsets, and they go stale on any edit above them.

Five more mutants span changed lines and are not in the score. All five are block removals of whole
enclosing method or loop bodies (lines 271, 339, 368, 2372 and 2377) that Stryker reports as `Ignored`
("Removed by block already covered filter"). Each one is a body that the statements inside it already
cover.

## Why only two mutants landed on the changed lines

Most of the change is a kind of code Stryker.NET does not mutate. The rewritten comments are not code.
`CloudChangelogExpansion` is a `const`. `CloudSearchPath` and `CloudSearchBodyOptions` are static
field initialisers, for which Stryker generated no mutant. The anonymous object in `CloudSearchBody`
and its `','` separator are not mutated either. Removing `var payload = …` or the `PostAsync` call
would not compile, and Stryker does not offer either one. The only operator it applied is the one on
`request.ExpandChangelog ? CloudChangelogExpansion : null`, both of whose forms were killed.

The behaviour those unmutated lines carry is still pinned, by tests that assert it directly rather
than leaving it to the mutation score:

- **The search is a POST and nothing rides in the query string.**
  `GetWorkItemsForTeam_OnCloud_PostsTheSearchWithTheChangelogExpansionAsOneString` asserts the method
  and that the request path ends at the Cloud search route.
- **Expand is a single string and fields is a list.** The same test checks the JSON value kinds and
  the literal values.
- **A first page carries no token, not even a null one.** The same test pins the exact set of names
  in the body (`jql`, `fields`, `expand`, `maxResults`), so taking `WhenWritingNull` off
  `CloudSearchBodyOptions` would fail it.
- **Every later page carries the token Jira handed back, in the body.** The paging test in
  `JiraIncrementalSyncTest` reads `nextPageToken` from the second request's body.
- **A full chunk of long keys fits on both deployments.**
  `GetWorkItemsForTeamByReferenceId_AsksForAQueryNoRequestLineCouldCarry` now runs for Cloud as well
  as Data Center, with a positive control that the query alone is over 8 KB.

## Closed survivors

None. There were no survivors on the changed lines, so no kill round was run and no test was added.

## Accepted survivors

None on the changed lines.

Three survivors sit in `WalkCloudSearchPages`, close to the change but on lines it did not touch:
the `ThrowIfCancellationRequested()` call (2380), the `pageCount++` statement and its `--` form
(2400), and `<` → `<=` on the `MaxCloudSearchPages` cap (2402). They predate this fix, so they are
recorded here and not chased.

## Not mutated

- `JiraRestRoutes.cs`. The only change there is the doc comment on `Search`, which now says both
  deployments take their parameters in a JSON body. The diff touches no code, so there is nothing to
  mutate.
- Test files (`StubTransport.cs`, `JiraConnectorTestSetup.cs` and the Jira and acceptance test
  classes). They are the instrument, not the subject.
