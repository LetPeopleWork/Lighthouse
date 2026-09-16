# Jira additional-field lookup — Bug #6012

**Delivered** 2026-09-16 · Backend only · ADO Bug #6012 · follow-up to Bug #5973 · reported by Steve Pereira

## What was wrong

Bug #5973 fixed the **search** path: a JQL Jira rejects now surfaces Jira's own sentence instead of a
.NET status line. The reporter then hit the same generic wording again while configuring **additional
fields** on a Jira connection. That path had not been touched, and what it turned out to be hiding was
worse than a bad message.

`GetIdForCustomFieldByProperty` read each JSON property with the throwing accessor:

```csharp
f.GetProperty(propertyIdentifier).GetString()
```

Callers pass `["name", "id", "key"]`. Jira **Data Center**'s `/rest/api/2/field` returns no `key`
property at all. The `key` pass is reached only after `name` and `id` have both missed — which is
exactly when a field reference resolves to nothing — so the one case the code exists to report became
an unhandled `KeyNotFoundException` on that deployment.

## Two failures, one line

**The verdict was unreachable.** "Some additional fields could not be found: X" could never fire for a
Data Center customer. A mistyped field name produced "Connection validation failed due to an
unexpected error." with no detail — the reporter's screen.

**Every Data Center refresh died, with nothing configured.** `GetPredefinedAdditionalFields` registers
a Flagged field whose fallback reference is `DefaultFlaggedFieldReference = "customfield_10001"` — a
Jira *Cloud* field id, as the repo's own test already said. On an instance with no matching field that
unresolvable reference went through the same lookup from `CreateWorkItemsFromIssues` and
`CreateFeaturesFromIssues`, so every team and portfolio refresh threw. Silently: `ValidateTeamSettings`
only counts issues, so validation passed and the refresh died afterwards with nothing on any screen.
No offline test caught it because the two fixtures that drive the same path answer `[]` to the field
endpoint, and an empty array never enumerates.

## The premise, checked against a real instance

The whole fix rests on Data Center omitting `key`. That was taken from Atlassian's DC REST reference
during the RCA, then confirmed against the live Cloud instance while diagnosing something else: all 28
field objects carry `id, key, name, custom, orderable, navigable, searchable, clauseNames, schema` —
the documented DC shape plus `key`. The same payload carries no field named `Flagged` and none with id
`customfield_10001`, which is a live example of the shape that triggers the refresh failure.

## What shipped

| Commit | |
|---|---|
| `b36aec176` | `TryGetProperty` — a property that is not there is a miss, not a crash |
| `b21dc1d4b` | RCA and roadmap |
| `628fefa54` | `JiraReadException.FieldListRefused`, the body read, and the three catches |
| `c6a936ee6` | The message pin mutation testing forced |
| `d504bfeec` | `RefusedFieldList` extracted beside `RejectedQuery` |

A refused field list now reports `field_list_unreadable` against the Additional Fields input, carrying
Jira's own sentence — instead of "Could not reach Jira with the provided URL." against the URL input,
which sent an administrator to edit a URL that had answered `rest/api/2/myself` moments earlier.

```
before:  Could not reach Jira with the provided URL.
         Response status code does not indicate success: 403 (Forbidden).

after:   Lighthouse could not read the list of fields on this Jira instance, so it cannot
         check the additional fields against it. Jira answered 403 (Forbidden). The account
         this connection signs in with needs to be able to browse at least one project…
         GET rest/api/latest/field answered 403 Forbidden. You do not have permission to view fields.
```

## Key decisions

**The four message edits had to ship as one commit.** `JiraReadException` is deliberately not an
`HttpRequestException`, which is what stops the URL catch swallowing it — but it also means the throw
without the matching catches would fall past that handler into the bare catch-all and say *less* than
before. The RCA wrote the dependency order down; it was followed.

**The URL verdict was left exactly as it was.** After this change the only `HttpRequestException` the
field call can raise is a genuine transport failure, which is what that message is for. The fix stops
it being used for something else rather than weakening it.

**The hard-coded `customfield_10001` stays.** After the fix it resolves to "no flag", which is correct
for an instance that has no such field. On a DC instance where some unrelated field happens to hold
that id it would silently read the wrong field — a separate defect with its own reproduction, and a
deliberate decision not to fold it in.

## Lessons

**A clean adversarial review still cannot see a negative assertion.** The review returned "zero
defects, ready for merge" and was right about everything it checked — null handling, catch ordering,
the rename, test isolation. Mutation testing then found that every fragment of the user-facing message
could be replaced with `""` with the suite staying green. The cause was one assertion:
`Does.Not.Contain("provided URL")` passes just as happily against an empty string. **This is the second
time the same defect class has reached a mutation run on Jira validation messages** — #5973 lost its
messages the same way. A review reads an assertion and sees coverage; only a mutant distinguishes
asserting a thing from asserting the absence of another thing.

**Whole-file mutation scores are noise on a large file.** The run scored 63% across the 2,693-line
connector. The number that meant anything was the 14 mutants on lines this change introduced, and
getting it required grouping the report against `git diff -U0`. Both the first baseline and the first
re-measurement were computed against misaligned line ranges and disagreed with each other; the ranges
only line up once the work is committed.

**The failure that blocked the push was the one nobody predicted.** The local suite came back with
three failures against a known two. The third was a DI container test failing in `Dispose` while
deleting a temp SQLite file — pre-existing, reproduced with the entire feature reverted. Worth the
twenty minutes: "probably environmental" and "measured as environmental" are not the same claim.

**Live verification arrived from CI, not locally.** The local Atlassian token had expired (401 on
`/myself`), so the `JiraIntegration` category could not run. CI's secret was valid and ran it — the
change is verified against a real Jira, just not from the machine that wrote it.

## Still open

- **Bug #6013** — the Azure DevOps connector has the identical misattribution:
  `GetMissingAdditionalFields` sits inside the `try` whose `catch (HttpRequestException)` returns
  "Could not reach Azure DevOps with the provided URL." with `fieldName = Url`. Linear, ServiceNow and
  CSV do not validate additional fields at all. ServiceNow is the reference implementation.
- **`DefaultFlaggedFieldReference = "customfield_10001"`** — see above.
- **`rest/api/latest/field` is written twice**: once as the connector's `const string url`, once as
  prose in the technical detail. Change the endpoint and the detail line lies. The new test asserts
  both against the same fixture constant, so a divergence fails a test rather than shipping.
- **The changelog and tenant_info sites** still use `EnsureSuccessStatusCode()`. Same shape, both
  Cloud-only, neither on this path.
- **A connection-level outcome on `WriteBackResult`**, so a flush that could not start is
  distinguishable from one that wrote nothing. A contract change across five connectors.

## CI

The first run went red on a single Linear test (`ServiceUnavailable`) and a re-run of the same commit
passed, confirming an upstream outage. Diagnosing why a Jira-only change ran Linear tests at all turned
up something more useful than this bug: the change-detector's base ref had resolved to a commit from
2026-08-03, making the diff window 1,462 commits, which guarantees `connector_shared=true` for
everybody's change. Recorded in `docs/ci-learnings.md` with the rule for recognising it.

## Artifacts

- `docs/feature/fix-jira-field-lookup-error-message/rca.md` — five-whys analysis, the endpoint
  measurements, and the fix dependency order
- `docs/feature/fix-jira-field-lookup-error-message/deliver/roadmap.json`
- `docs/feature/fix-jira-field-lookup-error-message/mutation/stryker.6012.backend.json`
