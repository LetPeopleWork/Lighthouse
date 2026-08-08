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
- **The retry's OUTCOME decides the diagnosis, not the 403 itself** (added 2026-08-08, reviewer Finding 1).
  A Jira PUT also 403s when the credential lacks Edit Issues or cannot see the work item, and calling that
  a suppression problem would send the admin to grant a permission that was never at fault:
  - retry **succeeds** -> the 403 *was* about suppression -> record `NotSuppressed`, feed the Warning and
    slice 05's rollup.
  - retry **also fails** -> the 403 was *not* about suppression -> record `Unknown`, report a plain write
    failure, and emit **no** Warning and nothing into the rollup.
- The `NotSuppressed` condition is recorded on the result so slice 05 can surface it, and is **logged at
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

> **Numbering corrected 2026-08-08.** The three bullets below carried the pre-2026-07-17 slice numbers.
> They now match the filenames and the story table in `feature-delta.md`.

- Permission pre-check and the connection status surface (**slice 05**, #5506).
- The Cloud bulk transport (**slice 06**, #5507) - **Removed**; SPIKE-03 Q5 disproved its least-privilege
  premise, so nothing replaces this call for Cloud connections. This slice's PUT is the end state.
- Deployment routing - **not built at all.** D4 is dropped, not deferred: there is one code path for Cloud
  and Data Center, and no deployment discriminator exists anywhere in the design.

## Production-data AC

- ~~Given a Jira DC connection with a write-back mapping and a changed forecast percentile, when a Team
  update triggers write-back, then the field updates and the issue's watcher receives no email...~~
  **RETIRED 2026-08-08 as a release gate** - no Data Center instance is obtainable before release, so this
  cannot be asserted now. It moves to the post-release DC verification checklist that already exists at
  the end of `slice-03-spike-jira-notification-suppression.md` (Q1/Q2/Q10). The retry fallback is what
  makes shipping without it safe: no DC customer ends up worse off under any of the three possible
  behaviours. The history-entry half of the assertion survives on the Cloud AC below (D1).
- Given the same on Jira Cloud with an admin credential, then no watcher email. **(SPIKE-03 Q3: verified -
  `DUMMY-6` suppressed, control `DUMMY-7` delivered.)**
- Given a Jira Cloud credential **without** admin or project-admin, when write-back runs, then the first PUT
  returns 403, the retry without the param succeeds, **the field value is written**, and the item is
  reported successful. This is the anti-regression AC - it is the whole reason the slice was revised.
- Given that same connection, then the inability to suppress is recorded once for slice 05 to surface.
- Given a Jira credential that cannot edit the work item at all, when write-back runs, then the first PUT
  403s, the retry **also** fails, the item is reported as a plain write failure with suppression state
  `Unknown`, and **no** "grant Administer Projects" Warning is emitted. *A 403 that survives the retry is
  never reported as a suppression problem.*
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
