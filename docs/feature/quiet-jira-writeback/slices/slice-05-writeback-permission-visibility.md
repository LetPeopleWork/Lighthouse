# Slice 05 - Write-back notification-suppression visibility

**Type:** vertical | **Est:** ~1 day | **Stories:** US-02 (#5506)

> **REVISED 2026-08-08 after SPIKE-03 and DESIGN.** Retitled twice over: the heading still carried the
> pre-2026-07-17 numbering ("Slice 02"), and "deployment-aware" is now wrong because D4 is dropped - there
> is no Cloud/DC branch to be aware of.
>
> **This slice is no longer a pre-check gate.** Slice 04's retry-on-403 means the write path never needs to
> ask permission in advance, so nothing here gates a write. What ships is **visibility**: telling the admin
> which projects cannot suppress notifications, and why.
>
> **The permission is per Jira project, not per connection** (SPIKE-03 Q6). A connection spanning five
> projects with two lacking project-admin must name those two - a connection-level boolean would send the
> admin to grant the permission in the wrong place. See ADR-145 and OQ-1.
>
> The ACs below were written for the gate framing and the advisory channel; both are obsolete. Note also
> that `ConnectionValidationResult` no longer has `Advisory` or `SuccessWith` - #5612's merge deleted them.

## Learning hypothesis

`mypermissions` is a **trustworthy predictor** of whether suppression will actually work, so Lighthouse can
tell an admin the truth before their team finds out. Disproved if the probe says "permitted" while watchers
still receive email (or vice versa) - in which case the honest surface has to be built on the observed
write-back **result** instead of a pre-flight probe, and D5 needs re-designing.

## Why it matters (D5)

**REVISED 2026-08-08.** The silent-ignore fear this slice was written against did not materialise:
SPIKE-03 Q4 found Jira **403s and drops the whole write**, and the retry (ADR-142) both keeps the write
and records the fact. So this slice is no longer the *only* signal - it is the one that arrives **before**
the first cycle rather than after it, which is the job it was actually chartered for
(`job-config-admin-know-writeback-is-quiet`: "see upfront"). The log tells you what happened; this tells
you what will happen, and which projects it will happen in. It is the same stance as
`job-forecast-no-false-certainty`: never present a confident answer we cannot stand behind.

## What ships

> **REVISED 2026-08-08 after SPIKE-03 Q6 and the DESIGN wave.** D4 is dropped, so there is no deployment
> routing; the permission is the same on both deployments. And the verdict is **per Jira project**, not
> per connection - `mypermissions` called without `projectKey` answers `havePermission: true` at HTTP 200,
> and a connection writing into five projects can be silent in three and noisy in two. See
> [ADR-145](../../../product/architecture/adr-145-writeback-notification-suppression-visibility.md).

- A permission probe on the Jira connection: `GET /rest/api/latest/mypermissions?projectKey=<key>&permissions=ADMINISTER,ADMINISTER_PROJECTS`,
  issued **once per project** the connection writes into. The project set is derived from the work-item
  reference ids (Jira `ReferenceId` is the issue key); an empty set issues **zero** requests.
- A read-only status on the Jira connection settings surface, in the admin's language: a rollup
  (quiet / partially noisy / noisy / could not check) plus **the names of the projects that will email
  watchers** and the permission to grant on each. Naming the projects is the point - a connection-level
  yes/no would send the admin to grant a permission where it changes nothing.
- Nothing is stored. The verdict is computed when the page asks and never persisted, so there is no
  migration, no cache and no invalidation policy.
- Shown only for Jira connections that have write-back mappings. Never for ADO / Linear / CSV /
  ServiceNow (D8).

## IN scope

- The probe, its per-project fan-out, the rollup, and the status surface.
- Graceful degradation: probe failure/timeout -> unknown state, never blocks saving, never claims quiet.

## OUT of scope

- Any toggle or remediation action (D3) - the surface is read-only; granting the permission is a Jira-side
  admin action, deliberately not automated.
- ~~The Cloud bulk transport (slice 03)... slice 03 flips the Cloud permission to "Make bulk changes".~~
  **RETIRED 2026-08-08** - slice 06 (the bulk transport) is Removed; SPIKE-03 Q5 showed the bulk path
  needs the same admin/project-admin permission, so there is no second Cloud path and no copy to update.
- Any persisted verdict. Nothing is stored (ADR-145 §6), so no migration and no invalidation.

## Production-data AC

- Given a Jira connection whose credential has the required permission, when the connection settings page
  loads, then it states write-backs will not email watchers.
- Given a credential lacking it, then the page states write-backs **will** email watchers, and names the
  exact permission and the account to grant it to.
- Given the probe times out or errors, then the status degrades to unknown, saving still works, and no
  claim of quiet is made.
- Given a connection whose projects exceed the latency budget (3 s per request / 10 s total / 4
  concurrent), then the projects that answered are shown with their verdicts, the rest read "could not
  check", the panel states how many of how many were checked, and the page never hangs.
- Given a connection writing into several Jira projects where only some grant the permission, then the
  page reports **per project** and names the ones that will email watchers.
- Given an ADO, Linear, CSV or ServiceNow connection, then no write-back permission status is rendered.

## Dependencies

- ~~**SPIKE-03 Q6 gates this slice**~~ **ANSWERED 2026-08-08.** `mypermissions` predicts accurately in
  both directions; it **does** need `projectKey`, and without it over-reports `havePermission: true` at
  HTTP 200. "Which project, when a batch spans several" is answered by asking about **all** of them - the
  verdict is per project by design (ADR-145 §1).
- Slice 01 landed (the thing whose permission we are reporting on).

## Taste tests

- Value-bearing: the admin learns the truth before the team does. Decision-enabling. PASS.
- Right-sized: one probe + one read-only status line on an existing surface, no new controls. PASS.
- Not decoration: disproves the probe's predictive value - the assumption D5 rests on. PASS.
- Production data: asserted against real instances with both permitted and under-permissioned credentials. PASS.
