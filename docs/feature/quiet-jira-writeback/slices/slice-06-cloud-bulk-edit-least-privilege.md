# Slice 03 - Jira Cloud write-back via bulk edit API (`sendBulkNotification: false`)

**Type:** vertical | **Est:** ~1 day | **Stories:** US-03

## Learning hypothesis

Quiet Cloud write-back does **not** require `Administer Jira`: the bulk edit API delivers suppression under
"Make bulk changes" alone, and its async taskId/polling contract can be mapped back onto Lighthouse's
existing per-item `WriteBackResult` without changing what callers see. Disproved if either (a) bulk edit
demands admin anyway - which collapses D2's entire Cloud rationale, since lower privilege *is* the reason
we chose it - or (b) the async contract cannot preserve per-item outcomes, forcing a `WriteBackResult`
redesign that ripples into `WriteBackService`, the updaters, and every existing write-back test.

## What ships

- Deployment routing per D4: `jira.cloud` / `jira.scopedtoken` / `jira.oauth` -> bulk path;
  `jira.datacenter` -> the slice-01 per-issue `notifyUsers=false` path.
- Cloud write-back via `POST /rest/api/3/bulk/issues/fields` with `sendBulkNotification: false`, pinned to
  **`/rest/api/3/`** - the bulk API does not exist in v2, and Lighthouse currently calls `rest/api/latest`.
- Submit -> poll the bulk progress endpoint -> map per-item outcomes onto `WriteBackResult.ItemResults`.
- Chunking at <=1000 issues per request (Atlassian's documented cap; 200 fields also capped).
- Slice 02's Cloud permission check flips to "Make bulk changes" and the copy updates to match.

## IN scope

- The bulk transport, the poller, the routing seam, chunking, per-item result mapping.
- Removal of the slice-01 Cloud interim (D6) - Cloud no longer uses the per-issue PUT.

## OUT of scope

- DC (no bulk API exists - stays on slice 01's path, permanently. This is not a temporary state).
- Bulk **move**, transitions, comments.
- Retry/backoff policy beyond what write-back does today.

## Production-data AC

- Given a Jira Cloud connection, when write-back runs, then `POST /rest/api/3/bulk/issues/fields` is called
  with `sendBulkNotification: false` and the per-issue PUT is **not**.
- Given a Cloud credential with only "Make bulk changes" (no `Administer Jira`), then write-back succeeds
  and the watcher receives no email.
- Given a Jira DC connection, then the per-issue `notifyUsers=false` path is still used.
- Given a bulk submit returning a taskId, when polling completes, then per-item outcomes appear in
  `WriteBackResult.ItemResults` with today's success/failure semantics preserved.
- Given >1000 changed issues, then requests are chunked to <=1000.
- Given the bulk task fails or polling times out, then each item is recorded as failed and logged - never
  a silent success.

## Dependencies

- **SPIKE-03 Q5 and Q8 gate this slice.** If Q5 shows bulk edit needs more than "Make bulk changes", the
  reason for the Cloud/DC split evaporates and the user must decide whether Cloud simply keeps
  `notifyUsers=false` from slice 01. Escalate before designing.
- Slices 01 and 02 landed.

## Reference class

Closest prior art: `epic-5305` slice-07's async/queue work and the OAuth single-flight refresh
(`adr-010`) - both cases of wrapping an async external contract behind a synchronous internal seam.
Expect the design cost to sit in the **result mapping**, not the HTTP call.

## Taste tests

- Value-bearing: adopt quiet write-back under least privilege - no `Administer Jira` grant. PASS.
- Right-sized: 2 new components (bulk transport + poller) plus a routing seam. Under the 4+ bar, but this
  is the fattest slice; if the async result mapping proves harder than the reference class suggests, split
  the poller out rather than letting the slice sprawl. PASS with a watch.
- Not decoration: disproves the permission-bar premise that D2 rests on. PASS.
- Production data: asserted on a real Cloud site with a deliberately non-admin credential. PASS.
