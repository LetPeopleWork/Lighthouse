# Live proof — Bug #5973

Run against the real `letpeoplework.atlassian.net` demo instance on 2026-09-11, driving the
production connector (`JiraWorkTrackingConnector`) rather than a stub. This file records what the
instance actually answered, so the fix is not built on inference.

## Repro artifacts created

| Artifact | Id | Shape |
| --- | --- | --- |
| Saved filter `Bug5973 Repro - No Where Clause` | 10106 | JQL is exactly `ORDER BY Rank ASC` — no `WHERE` clause |
| Kanban board `Bug5973 Repro` | 107 | Built on filter 10106, sub-filter left at Jira's default |

Delete both once the regression suite is green; nothing else references them.

The control case needed no setup: board **8** (`Stories`) already carries filter
`project = LIGHTHOUSE AND type IN (Bug, Story) ORDER BY Rank ASC` plus the same default sub-filter,
and is already asserted by `JiraWorkTrackingConnectorTest.GetJiraBoardInformation_GetsCorrectJqlQuery`.

## The trigger is Jira's own default, not an exotic configuration

Every Kanban board on the instance — 8, 9, 41 and the new 107 — carries the sub-filter
`fixVersion in unreleasedVersions() OR fixVersion is EMPTY`. That is what Jira creates a Kanban board
with. The right-hand side of the broken concatenation is therefore present by default; the only thing
a board needs to fail is a filter that leaves nothing behind once its ordering clause is stripped.
The three simple/Scrum boards (1, 6, 7, 74, 3, 4) carry no sub-filter and cannot reproduce it.

## Observed behaviour

```
BOARD 107 DataRetrievalValue => ' AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)'
BOARD 107 VALIDATION IsValid=False
BOARD 107 VALIDATION Code='validation_failed'
BOARD 107 VALIDATION Message='Team validation failed due to an unexpected error.'
BOARD 107 VALIDATION Technical='Response status code does not indicate success: 410 (Gone).'

BOARD 8   DataRetrievalValue => 'project = LIGHTHOUSE AND type IN (Bug, Story) AND (fixVersion in unreleasedVersions() OR fixVersion is EMPTY)'
BOARD 8   VALIDATION IsValid=True
```

Identical code path, identical board type, identical sub-filter. The only difference is whether the
saved filter survives `RemoveOrderByClause`, which is what makes this the proof rather than an
anecdote.

## What Jira actually says

Both endpoints, queried directly with the malformed query the connector builds:

```
MALFORMED   new /rest/api/3/search/jql    -> 400  {"errorMessages":["Error in the JQL Query: Expecting a
                                                   field name but got 'AND'. You must surround 'AND' in
                                                   quotation marks to use it as a field name.
                                                   (line 1, character 3)"],"errors":{}}
MALFORMED   old /rest/api/latest/search   -> 410  {"errorMessages":["The requested API has been removed.
                                                   Please migrate to the /rest/api/3/search/jql API…"]}
WELLFORMED  new /rest/api/3/search/jql    -> 200
WELLFORMED  old /rest/api/latest/search   -> 410  (same removal notice)
```

The last row is the important one: on Jira Cloud the legacy search endpoint answers 410 for *every*
query, valid or not. It is gone, not merely unhappy.

## Why the reported symptom differs from what the demo instance shows

The connector reports `410 (Gone)` here but the bug report shows `400 (Bad Request)`. Same root
cause, different surfaced status, because the deployments diverge at the point of failure:

- **Data Center** (both reporters) — `GetIssuesByQueryFromDataCenter` sends the malformed JQL to
  `rest/api/latest/search`, which still exists there. Jira rejects the query itself: **400**.
- **Cloud** (this demo instance) — `WalkCloudSearchPages` sends it to `rest/api/3/search/jql` and
  gets the real 400 above, but that method answers a bare `false` on any non-success and discards
  the body. `GetIssuesByQueryFromCloud` reads that `false` as "this instance cannot do token paging"
  and retries against the Data Center endpoint, which on Cloud is removed: **410**.

So the Cloud path replaces an accurate, actionable JQL error with a deprecation notice about an
endpoint the operator never chose to call. The fallback cannot succeed on Cloud under any
circumstances — every failed Cloud search, transient or not, now surfaces as "The requested API has
been removed".

## Consequences for the fix

Confirmed by this run, beyond what static reading established:

1. The empty-filter guard and the per-half bracketing are the actual fix; everything else is
   error-reporting quality.
2. Surfacing Jira's response body has to cover the Cloud swallow, not just the Data Center throw
   site — on Cloud the body is discarded one layer earlier and the eventual exception describes a
   different request entirely.
3. The Cloud-to-Data-Center search fallback is dead weight on Cloud and actively misleading. It
   should not run when the deployment is known to be Cloud.
