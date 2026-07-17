# Slice 01 - `notifyUsers=false` on Jira write-back (both deployments)

**Type:** vertical | **Est:** ~0.5 day | **Stories:** US-01

## Learning hypothesis

Jira watcher-email noise is a **connector parity gap, not a platform limitation**: adding
`?notifyUsers=false` to the existing per-issue PUT reaches ADO's behaviour
(`AzureDevOpsWorkTrackingConnector.cs:356`) for every Jira customer whose credential has admin or
project-admin. Disproved if watchers still receive email despite an adequately-permissioned credential -
which would mean notification-scheme rules bypass `notifyUsers` (the "Single User" scheme case) and D2's
whole approach needs rethinking.

## What ships

- `JiraWorkTrackingConnector.UpdateItem` issues `PUT rest/api/latest/issue/{id}?notifyUsers=false`
  instead of the bare PUT at line 325. Applied to **both** Cloud and DC (D6) - Cloud supports the param
  too, so parity ships now rather than waiting for slice 03's bulk transport.
- No settings, no DTO, no migration, no UI (D3 - always-on, mirroring ADO).

## IN scope

- The query param on the existing call path.
- `JiraWriteBackTest` coverage asserting the param is present on the request URI.
- One real-instance verification per deployment (AC-01.2 / AC-01.3) - a mocked `HttpClient` proves the
  URL, never the inbox.

## OUT of scope

- Permission pre-check and the connection status surface (slice 02).
- The Cloud bulk transport (slice 03) - which replaces this call for Cloud connections.
- Deployment routing (slice 03 introduces it; this slice treats both the same).

## Production-data AC

- Given a Jira DC connection with a write-back mapping and a changed forecast percentile, when a Team
  update triggers write-back, then the field updates and the issue's watcher receives **no email**, while
  the change **does** appear in the issue history (D1 - asserted deliberately so the unsuppressible
  channel is never mistaken for a bug later).
- Given the same on Jira Cloud with an admin credential, then no watcher email.
- Given an ADO connection, when write-back runs, then behaviour is unchanged.
- Given `GetChangedFields` yields no changed value, then no Jira HTTP call is made at all.

## Dependencies

- SPIKE-03 answers Q1-Q4. If Q2/Q4 report **silent ignore**, this slice still ships (it is strictly better
  than today) but slice 02 becomes mandatory rather than merely valuable - because a silently-ignored
  param means Lighthouse cannot tell success from failure on its own.

## Taste tests

- Value-bearing: watchers stop getting mail on every properly-permissioned Jira connection. PASS.
- Right-sized: one query param, one test file, two manual verifications. PASS.
- Not decoration: disproves "notifyUsers is sufficient" if the inbox still fills. PASS.
