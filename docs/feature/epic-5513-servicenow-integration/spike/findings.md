# SPIKE findings — ServiceNow viability (Epic 5513)

**Instance**: PDI `dev191338`, release **Australia**, cloud.
**Date**: 2026-07-29. **Probe code**: throwaway, in scratchpad `spike_epic-5513/`.

## Verdict summary

| Question | Verdict | Note |
|---|---|---|
| Q1 basic auth mechanics | **WORKS — with a deadline** | See D3a below; this is the headline |
| Q2 ITSM field mapping | **WORKS, with a constraint** | `sys_class_name` filter is mandatory |
| Q4 started timestamp | **DOESN'T EXIST on the record** | Derived from metrics instead |
| Q6 transition history | **WORKS via `metric_instance`** | `sys_audit` ruled out by decision |
| Q8 minimum role set | **BLOCKED** | Cannot set probe-user passwords via Table API |
| Q10 label-based mapping | **WORKS as admin** | Least-privilege reachability still unknown |

---

## D3a has a date on it — the headline finding

ServiceNow's inbound Basic Auth restriction is **present and armed** on the Australia release. Read
from `sys_properties` on this instance:

| Property | Value |
|---|---|
| `glide.authenticate.basic_auth.restriction.active` | `true` (feature enabled) |
| `glide.authenticate.basic_auth.restriction.enforce` | `false` (tracking mode — nothing blocked yet) |
| `glide.authenticate.basic_auth.restriction.enforcement_date` | **`2026-07-29 20:31:17` UTC** |
| `glide.authenticate.basic_auth.allowed_roles` | `snc_basic_auth_api_access` |
| `glide.authenticate.basic_auth.allowed_users` | *(empty)* |

The platform's own description of `enforcement_date`:

> UTC date/time when basic-auth restriction enforcement begins. […] Until this time, users
> authenticating via basic auth are auto-granted the `snc_basic_auth_api_access` role so they keep
> working; after it, requests from users without the role are blocked.

**Observed contradiction with that description**: `admin` has authenticated via basic auth many times
today and does **not** hold the role. The only holder is `lh_probe_itil`, which got it because the
probe granted it explicitly. So the auto-grant either runs on a schedule not yet fired, or does not
cover `admin`. Either way the observable state is: **the account our tooling uses is not on the
allow-list, and enforcement begins 2026-07-29 20:31:17 UTC.**

### Consequences

1. **Operational, immediate.** `updatedemoenv.yml` runs at 01:00 UTC — *after* enforcement. Without
   the role on `admin`, the ServiceNow seeder step fails on its first scheduled run, and so does any
   integration test using these credentials. Mitigation is one role grant.
2. **Product, and it moves a scope line.** A customer running a current release must grant
   `snc_basic_auth_api_access` (or list the user in `allowed_users`) before a basic-auth integration
   works. That is an **instance-side setup step**, which conflicts with the no-instance-side-setup
   scope line recorded against Q6. It does not invalidate the D3 decision — the user's on-prem
   evidence stands, and the grant is small and documentable — but v1 docs must carry it as a
   prerequisite, and it strengthens the case for OAuth as the named successor.
3. **The property values are readable through the Table API**, so connection validation can *detect*
   this: read `restriction.active`, `restriction.enforce`, `enforcement_date` and warn the user
   before their integration breaks rather than after. That is a concrete, cheap slice-01 feature this
   probe would not have found by reasoning.

Note the enforcement date was written `2026-07-28 20:31:17` — 24 hours before it fires. A customer
could get similarly short notice, which is what makes detection-at-validation worth building.

---

## Q8 is blocked on a mechanism, not a permission

The plan was: create probe users with escalating roles, read every table the connector needs as each,
and record the minimum that still works. Three users were created (`lh_probe_none`,
`lh_probe_snc_read` with `snc_read_only`, `lh_probe_itil` with `itil`), all `active=true`,
`locked_out=false`, `password_needs_reset=false`, `web_service_access_only=false`.

**All ten tables returned 401 for all three users** — including after granting
`snc_basic_auth_api_access`. A deliberate wrong-password control also returned 401, and the `admin`
control returned 200 throughout. 401 is *authentication* failure, so this says nothing about ACLs:
**the Table API silently ignores writes to `sys_user.user_password`.** The users exist; they have no
usable password.

Unblocking needs the password set outside the Table API — simplest is the UI (`sys_user` record →
*Set Password*). Until then the entire role matrix is unmeasured, and with it:

- whether a least-privilege account can read `metric_instance` (Q6 rung 1 — if not, history support
  collapses to the unsupported mode),
- whether it can read `sys_choice` (Q10 — if not, the wizard falls back to magic numbers),
- whether the minimum is effectively `admin` (the epic-level stop signal).

**A 200 response is not sufficient evidence here.** ServiceNow enforces read ACLs by filtering rather
than refusing, so a permitted-but-unauthorised read returns `200` with an empty result set. The probe
already records "(empty)" separately from the status code for this reason; the finished matrix must
be read as *status + row count*, never status alone.

---

## Carried forward from the pre-SPIKE work (already recorded in `spike-questions.md`)

- **Q4**: `work_start` stays empty after a real API-driven transition with business rules firing.
  No trustworthy started-timestamp on the record.
- **Q6**: `metric_instance` yields one row per state span with `start` / `end` / `duration`, created
  within seconds of a transition. Started time = `start` of the first span mapping to Doing. Spans
  begin at record creation under an active metric definition, so partial history is expected.
  Parsing traps: `duration` is a Glide duration rendered as an epoch offset (`1970-01-01 00:00:06`
  is six seconds); some rows carry an empty `field`.
- **Q2**: bare `task` is not a usable query root — the newest 200 rows were `cert_follow_on_task` and
  `alm_transfer_order_line_task` with zero incidents. Always filter `sys_class_name`.
- **Q10**: `state` values collide across subclasses (`3` = On Hold on `incident`, Closed Complete on
  `task`); `sys_choice` exposes the labels so the wizard need not expose numbers.

## Write path: what it takes to move an incident through its lifecycle

Not a listed question, but it had to be answered to make the seeder produce closed work, and the
answers bear directly on how the connector must read state.

- **A 200 is not proof the write landed, and a 403 is not always what it appears.** Moving an
  incident to Resolved with `close_code="Solved (Permanently)"` — a value copied from the instance's
  own 2016 sample data — was rejected with `403 Data Policy Exception: The following fields are
  mandatory: Resolution code`. The value is not in the current choice list, so the platform dropped
  the field silently and the *"Make close info mandatory when resolved or closed"* data policy then
  fired on the resulting empty field. **The error names the form label ("Resolution code"), not the
  element (`close_code`)** — worth knowing before someone spends an hour looking for a field by that
  name. Valid choices on this release: `Solution provided`, `Resolved by caller`, `Workaround
  provided`, `Duplicate`, `Resolved by change`, `User error`, `Known error`, `Resolved by problem`,
  `Resolved by request`, `No resolution provided`.
- **Choice values are instance- and release-specific**, which is Q10's argument arriving from the
  other direction: hardcoding any choice value is a latent break. The seeder now resolves
  `close_code` from `sys_choice` at runtime instead of carrying a constant.
- `New → In Progress` and `→ On Hold` need no extra fields; `hold_reason` is not mandatory here.
  Only Resolved/Closed are gated by the data policy, and once `close_code` is set, `Resolved → Closed`
  needs no further payload.
- **`state` and `incident_state` stay in sync** when only `state` is written, so a connector reading
  either sees the same truth. Worth re-checking on a customer instance, since the metric definition
  watches `incident_state` while the natural field to read is `state`.
- **Metric calculation is asynchronous.** Immediately after a transition the previous span can still
  read as open with no duration; ~30 s later the full chain was complete:
  `New 36m01s → In Progress 57s → Resolved 0s → Closed (open)`. Two consequences: slice 04 must
  tolerate a lagging tail rather than treating an open span as "still in that state", and connection
  validation must not conclude "metrics unavailable" from a read taken immediately after a write.
- **Rows with an empty `value` exist as well as rows with an empty `field`** — the "Open" definition
  produces `incident_state=(empty)` spanning the whole active period. A reader must filter on both.

## Still open

Q3 (`sysparm_query` semantics beyond basic use), Q5 (hierarchy — decides whether slice 03 is built or
cancelled), Q7 (volume and rate limits), Q9 (cloud vs on-prem divergence). None were probed in this
pass; scope was Q8 by explicit choice.
