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
| Q8 minimum role set | **ANSWERED** | `sn_incident_read` for metrics; `itil` only for history |
| Q10 label-based mapping | **WORKS for every account** | `sys_choice` readable with no roles at all |
| Q3 query concept | **WORKS — fails silently** | Unknown field → returns the whole table |
| Q5 hierarchy | **MECHANISM EXISTS, UNUSED** | `task.parent` populated on 0 of 94 — cancel slice 03 |
| Q7 volume / rate limits | **NO LIMITS HIT** | ~600 ms/call; batch, never per-item |
| Q9 on-prem detection | **NOT AVAILABLE to the integration account** | Checklist needs elevated rights |

**Epic-level signal: GO**, narrowed — slice 03 cancelled, slice 04 conditional on the customer
accepting an `itil`-grade account. The stop-signal candidate (Q8 returning "effectively admin") did
not materialise: read-only works for everything except history.

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
3. **Detection is NOT available to the integration account.** An earlier draft of this document
   claimed connection validation could read `restriction.active` / `enforce` / `enforcement_date`
   and warn the customer before their integration breaks. That was measured as `admin` and is wrong
   for a least-privilege account: as `sn_*_read`, `sys_properties` returns **200 with zero rows** for
   all three properties, `sys_plugins` returns 403, and `metric_definition` returns 403. So Lighthouse
   cannot see the enforcement date using the credentials a customer would give it, and cannot warn
   them. A privileged human can check; the product cannot. Anything built on "we'll detect and warn"
   must be scoped to privileged accounts or dropped.

Note the enforcement date was written `2026-07-28 20:31:17` — 24 hours before it fires. Combined with
the point above, a customer can get ~24 hours of notice that their integration is about to break, and
Lighthouse has no way to see it coming on their behalf. The mitigation is documentation and the OAuth
successor, not detection.

---

## Q8 — ANSWERED: `sn_incident_read` for metrics, `itil` for history

### The measurement obstacle, recorded because it will recur

Probe users created through the Table API could not authenticate: all tables returned 401 for all
three, a deliberate wrong-password control also returned 401, and the `admin` control returned 200
throughout. **The Table API silently ignores writes to `sys_user.user_password`.** The users existed
with no usable password. The UI's *Set Password* action only offers a generated value; setting a
chosen one needs the `Password` field added to the form via *Form Layout*. Anyone reproducing this
matrix on another instance will hit the same wall.

### The matrix

All four accounts authenticate; reads are shown as *status/rows*.

| Table | admin | no roles | `snc_read_only` | `sn_*_read` | `itil` |
|---|---|---|---|---|---|
| `incident` | 200/5 | 200/EMPTY | 200/EMPTY | **200/5** | 200/5 |
| `task` | 200/5 | 200/EMPTY | 200/EMPTY | **200/5** | 200/5 |
| `change_request` | 200/5 | 200/EMPTY | 200/EMPTY | **200/5** | 200/5 |
| `sc_task` | 200/1 | 200/EMPTY | 200/EMPTY | **200/1** | 200/1 |
| `sys_choice` | 200/5 | **200/5** | **200/5** | **200/5** | 200/5 |
| `metric_definition` | 200/5 | 403 | 403 | **403** | 200/5 |
| `metric_instance` | 200/5 | 403 | 403 | **403** | 200/5 |
| `sys_db_object` | 200/4 | 403 | 403 | 403 | 200/4 |
| `sys_dictionary` | 200/5 | 200/EMPTY | 200/EMPTY | 200/EMPTY | 200/EMPTY |

`sn_*_read` = `sn_incident_read` + `sn_change_read` + `sn_request_read`, granted together.

### What it means

- **The minimum for flow metrics is a genuinely read-only role.** `sn_incident_read` (plus its
  per-table siblings) reads every ITSM table the connector needs. `itil` is *not* required for
  slices 01–03. This is a good adoption story: a platform team can grant read-only.
- **Time-in-state costs more.** `metric_definition` and `metric_instance` return 403 for every
  read-only role and only open up at `itil` / `itil_admin` / `metric_admin` — all fulfiller-grade.
  So US-04 carries an adoption cost beyond its build cost: the customer must escalate the
  integration account from read-only to fulfiller purely to get history. That belongs in the US-04
  build/cancel decision, and in the docs if it ships.
- **`sys_choice` is readable with no roles at all**, so Q10's label-based state mapping works for
  every account that can reach the instance. The wizard need never show a magic number.
- **`snc_read_only` grants nothing** — identical to holding no roles. It is a UI-write-restriction
  role, not a read-grant role. Anyone writing the customer-facing docs should say so explicitly,
  because its name invites exactly the wrong guess.
- `sys_db_object` (403 below `itil`) and `sys_dictionary` (200/EMPTY at every level) mean **table and
  field discovery cannot be offered to a least-privilege account.** A wizard that enumerates tables
  would work for the maintainer and silently show an empty list to the customer. Configuration must
  therefore accept a typed table name, with discovery as at most a convenience for privileged users.

### The trap this exposed, and the slice-01 requirement it creates

**A permitted-but-unauthorised read returns `200` with an empty result set, not an error.** Every
`200/EMPTY` above is a denial wearing a success costume. A naive connector validating a connection
would report *"connected successfully, 0 work items found"* — sending the customer to hunt for a
query bug that is actually a permissions problem.

Connection validation must therefore distinguish *authenticated* from *authorised*: read a table the
account should be able to see, and treat zero rows against a known-non-empty table as a permissions
failure rather than an empty result. This is a slice-01 acceptance criterion, and it is not something
the DISCUSS wave could have anticipated.

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

## Q3 — `sysparm_query` works, and fails silently in two opposite directions

Every operator we need behaves, measured as the least-privilege account:

| Query | Meaning | Result |
|---|---|---|
| `state=1` | equality | 29 |
| `state=1^priority=3` | AND | 9 |
| `state=1^ORstate=2` | OR | 56 |
| `stateIN1,2,3` | IN list | 64 |
| `short_descriptionSTARTSWITHVPN` | prefix | 1 |
| `short_descriptionLIKEemail` | contains | 8 |
| `opened_at>javascript:gs.beginningOfLastMonth()` | relative date | 28 |
| `active=true^ORDERBYDESCopened_at` | sort | 66 |

**So D5 holds: `sysparm_query` is a good fit for Lighthouse's query concept.** But it fails silently,
and in two opposite directions (baseline = 94 incidents):

| Bad input | Total returned | Behaviour |
|---|---|---|
| `no_such_field=banana` | **94** | unknown field → **term ignored, returns everything** |
| `^^^` | **94** | garbage → term ignored, returns everything |
| `state=NOT_A_NUMBER` | 0 | bad value on a real field → matches nothing |
| `state==1` | 0 | malformed operator → matches nothing |

Neither case returns an error. **A typo in a customer's query silently pulls the entire table** and
Lighthouse reports metrics over every incident in the instance rather than the intended team's —
wrong numbers, confidently displayed, with no failure anywhere. The opposite typo silently yields
nothing, which at least looks broken.

No platform-side validation flag exists (`sysparm_query_category=strict` is not a thing; it was
ignored like any other unknown parameter). **Detection is on us**: run the configured query and the
same query with the filter removed, and treat "identical totals" as a suspicious query worth warning
about. That is a slice-02 acceptance criterion.

This bug also contaminates investigation. The first Q5 pass reported `rfc` and `problem_id` as
"populated on 94 of 92 incidents" — neither field exists on `incident`, so `<field>ISNOTEMPTY` became
a no-op returning everything. **Any count derived from a field name must verify the field exists in
`sys_dictionary` first.**

## Q5 — the parent mechanism exists; nobody uses it

Dictionary-verified reference fields on `incident` / `task` that could carry a hierarchy:

| Field | References | Populated (of 94) |
|---|---|---|
| `task.parent` | `task` | **0** |
| `incident.parent_incident` | `incident` | 1 |
| `task.universal_request` | `task` | 0 |
| `task.rejection_goto` | `task` | 0 |
| `task.business_service` | `cmdb_ci_service` | 1 |

And no portfolio-shaped tables exist at all — `pm_project`, `pm_portfolio`, `rm_story`, `rm_epic`,
`demand` all return **400 (table does not exist)**, consistent with D4's ITSM-first decision and the
Agile Development 2.0 plugin being absent.

So the answer is nuanced rather than a flat no: `task.parent` is a genuine self-referencing hierarchy
and would work mechanically, and `parentIN<list>` batches children in one call. But **it is empty in
practice** — ITSM shops track incidents flat. Building slice 03 on it means building for a hypothesis
about how a customer *might* configure their instance.

**Recommendation: cancel US-03 / slice 03 (5576) loudly**, per US-03 AC5, recording that the mechanism
exists but is unpopulated. Revisit only if a real customer shows a populated `parent`. This is the
outcome DISCUSS predicted when it chose ITSM over Agile 2.0 — an accepted, eyes-open consequence.

## Q7 — pagination is sound, throughput is the constraint

- `X-Total-Count` is present and stable, and a `Link` header carries paging relations.
- `sysparm_limit` / `sysparm_offset` page correctly — consecutive pages verified disjoint.
- A 1000-row request returned all 94 available rows in 0.71 s.
- 25 sequential requests: **all 200, no throttling**, at ~1.6 req/s sustained — that is ~600 ms of
  latency per call, not a rate limit. No rate-limit rules are configured (`sys_rate_limit_count` is
  empty) and no rate/quota/retry headers appear on responses.

**No backoff is needed before slice 02 ships.** The risk is not 429s but wall-clock: at ~600 ms per
call on a PDI, a 500-item sync is a page or two if fields are projected, but N+1 per-item calls would
be ~5 minutes. **Design for batched reads; never per-item.** Re-measure on a customer instance before
promising the "time to first metric" KPI, since PDI latency is not representative.

## Q9 — the on-prem checklist cannot be run by the integration account

Detection surface, measured as `sn_*_read` versus `admin`:

| Signal | As `sn_*_read` | As `admin` |
|---|---|---|
| `glide.war` (release/patch) | 200, **empty** | `glide-australia-02-11-2026__patch3-05-25-2026…` |
| `glide.product.description` | 200, **empty** | `Service Management` |
| basic-auth restriction properties | 200, **empty** | full values |
| `sys_plugins` (plugin presence) | **403** | readable |
| `metric_definition` (history availability) | **403** | readable |

Everything diagnostic is invisible to a least-privilege account, mostly via the 200-with-zero-rows
filter rather than an honest 403. **The US-06 AC1 checklist therefore has to be run by someone with
elevated rights on the on-prem instance** — it cannot be a script the customer runs under the
integration account, which was the original intent. Either the checklist gains a stated prerequisite
("run as a user who can read `sys_properties` and `sys_plugins`"), or it shrinks to the only question
a restricted account can answer: *can I read the configured table, and does it return rows?*

For version identification specifically, `glide.war` is the reliable signal and is admin-only.

## CORRECTION 2026-07-29 — Q10's mechanism was wrong, and the replacement carries a date trap

Re-measured while preparing slice 02, against the same PDI, using the probe accounts.

**Q10 as recorded is disproven.** It says `sys_choice` "is readable with no roles at all, so
label-based state mapping works for every account that can reach the instance". Readable, yes —
*queryable*, no. A bare `sys_choice` read succeeds for every account, but non-admin accounts get only
`label`; the `name`, `element` and `value` fields are stripped by field-level ACL. Filtering by them —
which is the only way to ask "what are the state labels for `incident`?" — fails outright:

| Account | bare read | `name=incident` | `name=incident^element=state` |
|---|---|---|---|
| no roles | 3 rows, label only | **403-shaped error** | **403-shaped error** |
| `snc_read_only` | 3 rows, label only | **error** | **error** |
| `itil` | 3 rows, label only | **error** | **error** |
| `admin` | 3 rows, all fields | 5 rows | 6 rows (`New`/`In Progress`/`On Hold`/…) |

Error body: *"Insufficient rights to query records — Field(s) present in the query do not have
permission to be read."* So the `ServiceNowChoiceLabelResolver` seam named in DESIGN would work for
the maintainer's `admin` and silently fail for every customer — the same works-for-me shape as the
discovery wizard, and the same shape as the epic's headline bug.

**The replacement is `sysparm_display_value=all` on the record query itself**, which needs no
`sys_choice` access at all. It works for `sn_*_read` and returns every field as
`{display_value, value}`:

```
"state":          { "display_value": "Closed",              "value": "7" }
"opened_at":      { "display_value": "2026-07-05 03:46:48", "value": "2026-07-05 10:46:48" }
"sys_created_on": { "display_value": "2026-07-28 23:46:48", "value": "2026-07-29 06:46:48" }
```

**The trap: `value` is UTC, `display_value` is the instance timezone — seven hours apart here, and
`sys_created_on` crosses a date boundary between the two.** Lighthouse buckets Throughput by day, so
reading `display_value` for a date silently files work under the wrong day, and only for instances
whose timezone is far enough from UTC to cross midnight. The rule is therefore split by field kind:

- **dates → `.value`** (UTC), never `.display_value`
- **state → `.display_value`** (the label the user recognises), with `.value` kept for diagnostics

This also removes a component from the slice-02 design rather than adding one.

### Q3's silent-filter trap, reproduced (same session)

Confirmed live rather than carried on trust, because slice 02 turns it into an acceptance criterion:

| `sysparm_query` | rows returned |
|---|---|
| `not_a_real_field=whatever` | **96 — the entire table** |
| *(no query at all)* | 96 |
| `state=99999` (real field, impossible value) | 0 |

An unknown field name is **silently dropped from the filter** and the query degrades to "everything".
A wrong value on a real field returns nothing. Neither errors. A flow coach who fat-fingers a field
name therefore gets a team whose metrics are computed over the whole incident table, looking
plausible and being wrong — the same failure family as the headline bug. The only detection available
is to compare the filtered count against the unfiltered count and treat equality as suspicious.

### Pagination, reconfirmed

`X-Total-Count: 96` and a `Link` header carrying `rel="first"` / `rel="next"` / `rel="last"` with
`sysparm_offset` values. Offset paging, disjoint pages.

## Q6 pre-slice probe — the slice-04 sizing question (measured 2026-07-30, live PDI)

The slice-04 brief made this the hard gate: **≤1 day if the history source is a single queryable
table, >1 day and a re-slice if it is per-item.** Measured as `lh_probe_itil`:

| Probe | Result |
|---|---|
| `incident?sysparm_limit=1` | 200, 0.67 s |
| `metric_instance?sysparm_limit=1` as `itil` | 200, 0.61 s |
| `metric_instance?sysparm_limit=1` as `snc_read` | **403** — Q8's role matrix reproduced |
| `metric_instance?sysparm_query=idIN<96 sys_ids>` | **200, 0.81 s — 157 spans in ONE call** |

**Verdict: batch. `metric_instance.id` takes an `IN` list of work-item sys_ids, so the whole team's
history is one call, not one per item.** Slice 04 is the ≤1-day shape; the re-slice branch does not fire.

### The real constraint is URL length, and it fails loudly

`sysparm_query` rides in the query string, so the batch is bounded by the **8192-byte URL limit**,
not by a row count. Pinned by bisection with synthetic non-matching sys_ids:

| ids | URL bytes | Status |
|---|---|---|
| 200 | 6697 | 200 |
| 245 | 8182 | 200 |
| **250** | **8347** | **414 URI Too Long** |
| 500 | 16597 | 414 |

**Chunk at 200 ids.** That is ~18 % headroom under the cliff, and the headroom is load-bearing: the
real query also carries `sysparm_fields` and `sysparm_limit`, and a customer instance may sit on a
longer hostname or a reverse-proxy subpath than this PDI does.

**414 is the good kind of failure** — a hard, visible status, not the 200/EMPTY denial-in-a-success-
costume this epic exists to prevent. An over-long batch cannot silently return partial history.

### Cost for ~500 items, as the brief demanded

500 items = 3 chunks × ~0.8 s ≈ **2.4 s added to a team refresh**. That is not material against the
existing refresh-duration expectations, so **AC5's opt-in team setting is not needed** — the feature
can ship on by default. Re-measure if a customer's team spans a table far larger than this PDI's 96
incidents; the cost scales with chunk count, which scales with item count, not with span count.

## Still open

Nothing from the original ten. Q2, Q3, Q5, Q7, Q9 and Q10 are answered above or in
`spike-questions.md`; Q1, Q4, Q6 and Q8 are answered here. Remaining uncertainty is not
question-shaped — it is that **every measurement is from one cloud PDI on the Australia release**.
The on-prem instance remains unmeasured, which is what Q9's checklist exists to close.
