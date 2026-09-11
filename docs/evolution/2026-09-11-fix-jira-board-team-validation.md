# Jira board team validation — Bug #5973

**Delivered** 2026-09-11 · Backend only · ADO Bug #5973 · reported by Steve Pereira

## What was wrong

Two people, both on Jira Data Center, got a 400 when validating a team they had built by selecting a
board. The report carried a status code and nothing else, which is most of the reason it needed an
investigation rather than a glance.

The cause was in how a board's saved filter and its sub-filter were joined:

```csharp
var filter = await ExtractFilterQuery(client, root);   // "" when unreadable OR only an ordering
subQuery   = ExtractSubQuery(root, subQuery);          // already carried a leading "AND ("
return $"{filter} {subQuery}";                         // no brackets, no empty-filter guard
```

When the filter read as empty, the result began `" AND ("`, and `PrepareQuery` wrapped it into
`"( AND (…))"`. Jira answers:

> Error in the JQL Query: Expecting a field name but got 'AND'. You must surround 'AND' in quotation
> marks to use it as a field name. (line 1, character 3)

Three conditions produce an empty filter, and the code could not tell them apart: the filter endpoint
answering non-2xx, a payload with no `jql`, and a filter whose JQL is only an ordering clause and so
strips to nothing.

## Why it hit some boards and not others

The right-hand side of the broken join is Jira's own default. Every Kanban board ships with the
sub-filter `fixVersion in unreleasedVersions() OR fixVersion is EMPTY`; Scrum and simple boards carry
none and cannot reproduce it. So the trigger is an ordinary Kanban board whose filter leaves nothing
behind — not an exotic configuration.

## Reproduced before anything was fixed

Rather than infer, a board was built on the live demo instance: saved filter `10106` with the JQL
`ORDER BY Rank ASC` (no `WHERE`), and Kanban board `107` on top of it, sub-filter left at Jira's
default. The production connector was then driven against it.

```
BOARD 107 DataRetrievalValue => ' AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)'
BOARD 107 VALIDATION IsValid=False
BOARD 8   DataRetrievalValue => 'project = LIGHTHOUSE AND type IN (Bug, Story) AND (…)'
BOARD 8   VALIDATION IsValid=True
```

Board 8 is the control: identical code, identical board type, identical sub-filter, non-empty filter,
passes. The flip between them is the proof. After the fix, board 107 validates and board 8 is
unchanged apart from its new bracketing. Both artifacts were kept on the instance deliberately, so a
dev build handed to the reporter has a known-bad board to test against.

Details in `docs/feature/fix-jira-board-team-validation/live-proof.md`.

## The deployments diverge at the point of failure, and one lied

The reporters saw `400 (Bad Request)`; the demo instance produced `410 (Gone)`. Same root cause:

- **Data Center** sends the malformed JQL to `rest/api/latest/search`, which still exists there, and
  Jira rejects the query itself — 400.
- **Cloud** sends it to `rest/api/3/search/jql` and gets the real 400, but that walk answered a bare
  `false` and dropped the body. The caller read that as "this instance cannot page by token" and
  retried the Data Center endpoint, which Cloud has removed — 410, with a deprecation notice about an
  endpoint the operator never chose to call.

Measured directly: on Cloud the legacy endpoint answers 410 to *every* query, valid or not. The
fallback could never succeed there; it could only replace an accurate error with a misleading one.

## What shipped

Four steps, each with its own commit and DES trace.

| Step | Commit | Change |
|---|---|---|
| 01-01 | `da7aecf0b` | Bracket the filter and sub-filter independently; guard the empty filter; make ORDER BY removal bracket-aware |
| 01-02 | `36817a1b5` | Read Jira's response body and report its sentence; a rejected query gets its own code and headline |
| 01-03 | `7d485c8ee` | An unreadable board filter refuses the board instead of yielding an unscoped query |
| 01-04 | `734d4a1bf` | Drop the fallback to the endpoint Cloud removed; escape `"` and `\` in state and type names |

Then `02e11c35f` (L1-L6 refactor), `60dc850a5` (tests closing the mutation gaps), `c3035ad4e` (a CI
flake fix the run exposed), and three documentation commits.

The user-visible change:

```
before:  Team validation failed due to an unexpected error.
         Response status code does not indicate success: 400 (Bad Request).

after:   Jira could not run this query.
         Error in the JQL Query: Expecting a field name but got 'AND'. … (line 1, character 3)
```

Portfolio validation improved for free — it shares the search layer, so the fix landed there without
duplicating anything.

## Key decisions

**The fix opened a worse hole, and closing it became its own step.** Once the query was well-formed,
an *unreadable* filter would produce valid JQL scoped to nothing — the whole Jira instance — and
validation would pass. The item-count check is no safety net, because an over-broad query returns
plenty. Silent over-fetching is worse than the loud 400 it replaced, so step 01-03 makes the
unreadable case fail and say why. This was caught by asking what the fix does to the *other* two
causes of an empty filter, not by a test.

**Pick time was the only option, not the better one.** The failure was originally specified as a
validation failure. It cannot be: validation reads `team.DataRetrievalValue`, a string already
stored, and never asks Jira for the filter again. The read happens only in `GetBoardInformation`,
during board selection. That also turned out to need no new machinery — `WorkTrackingReadException`,
the wizard controller's handler, and the wizard's own error rendering already formed a complete path.

**The dead Cloud fallback was fixed in the same change rather than split off.** It is a separate
defect, but it sat directly on the error-reporting path this bug was about, and leaving it would have
meant shipping a fix whose new message was still wrong on Cloud.

## Lessons

**A clean review is not evidence the tests are any good.** The adversarial review passed roughly 700
changed lines with no findings, and every specific it cited was correct. Mutation testing then scored
72.88% on those same lines and produced three real holes: the bracket-depth counter could be inverted
with the suite still green, the fallback branch that reports a status when Jira sends no
`errorMessages` was never exercised, and the user-facing messages could have been emptied to `""`
silently. Reading confirms the logic is right; it cannot show that nothing would notice if the logic
broke. After the added tests: 95.59%.

**Whole-file mutation scores say almost nothing about a fix.** The headline for this run was 47% and
then 61%, against a connector of ~2650 lines that this work barely touched. The number that mattered
had to be computed by grouping the report's mutants against `git diff -U0`. Stryker.NET silently
ignores line-span patterns, so there is no way to scope the run itself.

**A silent degradation turns a glance into an investigation.** Five places on this path returned an
empty or default value on failure and logged nothing, which is precisely why "the filter is empty" and
"we could not read the filter" were indistinguishable. Four now log before degrading. A fifth,
`GetBoards` answering `[]` on a 403 so an administrator is told no boards exist, was deliberately left
as a separate problem.

**Verify the mechanism before believing the explanation.** The reporter's own hypothesis was that the
query was too long. It is measurable: the assembled query for the reported board is 1097 characters,
an 1813-byte request line, against a threshold around 5200. That hypothesis was wrong, and the
evidence took one script.

## Still open

- **Bug #5974** — nothing tests the `options.Any()` guard in `PrepareGenericQuery`, the only thing
  stopping an empty type or state list from emitting an empty JQL bracket pair. The code's own comment
  records the blast radius: an empty pair read as "matches nothing" makes removal delete every stored
  record for that team or portfolio.
- `SweepIdentities` throws `InvalidOperationException` without Jira's sentence — the one refusal path
  this fix did not reach. Needs its own commit with a test, because both available fixes change
  behaviour.
- `JiraQueryRejectedException.RejectedQuery` is populated and never read. Two surviving mutants sit on
  it; the honest remedy is deleting the property.
- `GetBoards` degrading to `[]` on a 403 (above).
- `PrepareCutoffDateFilter` formats its date without a culture, so a container under a non-Gregorian
  locale would emit a Buddhist or Hijri year into JQL. `Program.cs` propagates the host culture rather
  than pinning invariant. Latent, narrow trigger, and it would skew a forecast rather than error.
- **The reported incident is not confirmed fixed.** A later screenshot from the original report shows
  a board whose filter is substantial and whose query carries no leading `AND` — so that occurrence is
  not this defect. A bug with this signature is fixed and reproduced; whether it is the reporter's is
  still open, pending a dev build. If it is not, the new message will name the real cause, which is
  the more valuable outcome.

## Artifacts

- `docs/feature/fix-jira-board-team-validation/rca.md` — five-whys analysis, multi-causal
- `docs/feature/fix-jira-board-team-validation/live-proof.md` — the live reproduction and endpoint measurements
- `docs/feature/fix-jira-board-team-validation/mutation/results.md` — scores, survivors, and why two stand
- `docs/ci-learnings.md` — the write-back poll budget entry (Recurrence: 2)
