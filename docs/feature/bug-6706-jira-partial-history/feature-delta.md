# Bug 6706 — Jira partial history dating: feature delta

## Root cause

- Data Center: `GetIssuesByQueryFromDataCenter` hands the search answer's changelog straight to
  `IssueFactory`. The paged re-read was removed in `e6f59d34d` because Data Center serves no paged
  `issue/{key}/changelog` route, so a truncated search answer dates the item from its latest entries only.
- Cloud: `ShouldFetchFullChangelog` re-reads only when `changelog.total > 30`, not when the answer holds
  fewer entries than `total`.

## Maintainer decisions (2026-11-26)

- **Option A.** When the search answer holds fewer entries than `changelog.total`, re-read the whole
  history before dating: Data Center through `rest/api/2/issue/<KEY>?expand=changelog`, Cloud through its
  paged changelog route. One trigger for both deployments. A re-read that is still partial leaves the item
  marked as partial (no regression).
- No Data Center instance is available. The fake-HTTP oracle proves the logic; whether a real Data Center
  issue endpoint returns the whole history is confirmed with the reporter by comparing `changelog.total`
  with the `histories` count on both the search and the issue endpoint.
- **Rigor profile:** S1 `standard` — acceptance designer and crafter on Sonnet, reviewers and examiner on Haiku.
- No UI change, so no sketch review.
