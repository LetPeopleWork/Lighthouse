# Slice 02 - Deployment-aware write-back permission pre-check + connection status

**Type:** vertical | **Est:** ~1 day | **Stories:** US-02

## Learning hypothesis

`mypermissions` is a **trustworthy predictor** of whether suppression will actually work, so Lighthouse can
tell an admin the truth before their team finds out. Disproved if the probe says "permitted" while watchers
still receive email (or vice versa) - in which case the honest surface has to be built on the observed
write-back **result** instead of a pre-flight probe, and D5 needs re-designing.

## Why it matters (D5)

If SPIKE-03 confirms the silent-ignore failure mode, Lighthouse gets HTTP 204 and logs a successful
write-back while every watcher is emailed. There is then **no signal anywhere** - not in logs, not in the
UI - that suppression failed. This slice is the only thing that turns an invisible failure into a fact the
admin can act on. It is the same stance as `job-forecast-no-false-certainty`: never present a confident
answer we cannot stand behind.

## What ships

- A permission probe on the Jira connection: `GET /rest/api/{2,3}/mypermissions`, deployment-routed per D4
  (DC -> `ADMINISTER,ADMINISTER_PROJECTS`; Cloud -> whatever SPIKE-03 Q6 validates).
- A read-only status on the Jira connection settings surface, in the admin's language:
  `Write-backs will email watchers - grant "<permission>" to <account> to silence them`, or a confirmation
  that write-backs are quiet, or an honest unknown when the probe fails.
- Shown only for Jira connections that have write-back mappings. Never for ADO / Linear / CSV (D8).

## IN scope

- The probe, its deployment routing, and the status surface.
- Graceful degradation: probe failure/timeout -> unknown state, never blocks saving, never claims quiet.

## OUT of scope

- Any toggle or remediation action (D3) - the surface is read-only; granting the permission is a Jira-side
  admin action, deliberately not automated.
- The Cloud bulk transport (slice 03). This slice's Cloud branch reports against whatever Cloud path is
  live at the time; slice 03 flips the Cloud permission to "Make bulk changes" and updates the copy.

## Production-data AC

- Given a Jira connection whose credential has the required permission, when the connection settings page
  loads, then it states write-backs will not email watchers.
- Given a credential lacking it, then the page states write-backs **will** email watchers, and names the
  exact permission and the account to grant it to.
- Given the probe times out or errors, then the status degrades to unknown, saving still works, and no
  claim of quiet is made.
- Given an ADO, Linear or CSV connection, then no write-back permission status is rendered.

## Dependencies

- **SPIKE-03 Q6 gates this slice**: whether `mypermissions` predicts accurately, whether it needs a
  `projectKey` (and which project, when one write-back batch spans several - a real open question, since
  project-admin is per-project while a batch is not).
- Slice 01 landed (the thing whose permission we are reporting on).

## Taste tests

- Value-bearing: the admin learns the truth before the team does. Decision-enabling. PASS.
- Right-sized: one probe + one read-only status line on an existing surface, no new controls. PASS.
- Not decoration: disproves the probe's predictive value - the assumption D5 rests on. PASS.
- Production data: asserted against real instances with both permitted and under-permissioned credentials. PASS.
