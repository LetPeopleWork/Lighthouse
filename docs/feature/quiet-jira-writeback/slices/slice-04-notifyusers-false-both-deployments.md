# Slice 04 - `notifyUsers=false` on Jira write-back (both deployments)

**Type:** vertical | **Est:** ~0.5-1 day | **Stories:** US-01 (#5505)

> **REVISED 2026-08-08 after SPIKE-03 Q4.** The always-on form of this slice (D3) is a **regression** and
> must not ship. An under-permissioned credential does not get a silently-ignored param - it gets
> `403 "To discard the user notification either admin or project admin permissions are required."` and the
> **field update is dropped entirely** (verified: `SPIKEPRM-1.duedate` stayed `null`). Today those customers
> have noisy-but-working write-back; unconditional `notifyUsers=false` would give them no write-back at all.
>
> **Decision (user, 2026-08-08): optimistic retry.** Send with `notifyUsers=false`; on **403**, retry the
> identical PUT **without** the param. Write-back can then never regress. The `mypermissions` probe is *not*
> a gate in the write path - it moves to slice 05 as the **visibility** surface that tells the admin this
> connection cannot suppress notifications.

## Learning hypothesis

Jira watcher-email noise is a **connector parity gap, not a platform limitation**: adding
`?notifyUsers=false` to the existing per-issue PUT reaches ADO's behaviour
(`AzureDevOpsWorkTrackingConnector.cs:356`) for every Jira customer whose credential has admin or
project-admin. Disproved if watchers still receive email despite an adequately-permissioned credential -
which would mean notification-scheme rules bypass `notifyUsers` (the "Single User" scheme case) and D2's
whole approach needs rethinking.

## What ships

- `JiraWorkTrackingConnector.UpdateItem` issues `PUT rest/api/latest/issue/{id}?notifyUsers=false`
  instead of the bare PUT at line 325. Applied to **both** Cloud and DC (D6).
- **403 fallback (new, from SPIKE-03 Q4):** when that PUT returns 403, retry the same payload without the
  query param and record the item's outcome from the retry. A 403 on the suppressed attempt is *not* a
  write-back failure - the retry is what determines success. Any other non-success status keeps today's
  failure semantics.
- The 403-and-retried condition is recorded on the result so slice 05 can surface it, and is **logged at
  `Warning`** (user decision, 2026-08-08). Until slice 05 ships this is the only signal the admin gets, so
  it must be visible at default production log levels.
  - **Deliberately louder than the surrounding code.** Write-back failures currently log at `LogDebug`
    (`JiraWorkTrackingConnector.cs:292` and `:339`) and are therefore invisible in production. Matching
    that style would swallow this warning - do not "align" it back down during review.
  - **Once per connection per cycle, not once per issue** - a portfolio-wide retry storm must not flood
    the log. The message names the connection and the affected project(s), and states the remedy: grant
    `Administer Jira` globally, or `Administer Projects` on those projects.
  - **Future:** once the task manager exists, the same condition should surface as a warning in the UI.
    Out of scope for this epic - the log is what ships here; slice 05's connection-status surface is the
    richer interim.
- No settings, no DTO, no migration, no UI (D3 - always-on, mirroring ADO). D3 survives *because* of the
  retry: always-on is safe only when the fallback guarantees the write still lands.

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
- Given the same on Jira Cloud with an admin credential, then no watcher email. **(SPIKE-03 Q3: verified -
  `DUMMY-6` suppressed, control `DUMMY-7` delivered.)**
- Given a Jira Cloud credential **without** admin or project-admin, when write-back runs, then the first PUT
  returns 403, the retry without the param succeeds, **the field value is written**, and the item is
  reported successful. This is the anti-regression AC - it is the whole reason the slice was revised.
- Given that same connection, then the inability to suppress is recorded once for slice 05 to surface.
- Given an ADO connection, when write-back runs, then behaviour is unchanged.
- Given `GetChangedFields` yields no changed value, then no Jira HTTP call is made at all.

## Dependencies

- **SPIKE-03 reported 2026-08-08.** Q3/Q4 answered on Cloud; Q1/Q2 (DC) **deferred to post-release** - no DC
  instance is obtainable before then. The DC path therefore ships on Cloud-verified behaviour plus
  Atlassian's docs, and the retry fallback is what makes that acceptable: if DC turns out to behave
  differently, the retry still lands the write.
- The outcome was neither branch this note anticipated. Not "silent ignore" - a hard **403 that drops the
  write**. Hence the retry, and hence slice 05 is now a *reporting* companion rather than a prerequisite.

## Taste tests

- Value-bearing: watchers stop getting mail on every properly-permissioned Jira connection. PASS.
- Right-sized: one query param, one test file, two manual verifications. PASS.
- Not decoration: disproves "notifyUsers is sufficient" if the inbox still fills. PASS.
