# SPIKE Decisions — epic-5513-servicenow-integration

## Assumption Tested

Scope was chosen interactively as **Q8 — the minimum role set**, on the grounds that everything
proven pre-SPIKE had been proven as `admin` and told us nothing about a customer. Q3, Q5, Q7 and Q9
were probed in the same pass once Q8 resolved faster than expected.

## Probe Verdict

**WORKS — GO, narrowed.** A genuinely read-only role (`sn_incident_read` and its per-table siblings)
reads every ITSM table the connector needs. The epic-level stop signal DISCUSS was watching for —
"the minimum is effectively `admin`" — did not materialise.

Narrowing:

- **Slice 03 / US-03 (portfolio, 5576) → CANCEL.** `task.parent` exists and would work mechanically,
  but is populated on 0 of 94 records. No portfolio-shaped tables exist without plugins.
- **Slice 04 / US-04 (history, 5577) → CONDITIONAL, with a new cost.** `metric_instance` is the right
  source and works, but is `403` for every read-only role, opening only at `itil` / `itil_admin` /
  `metric_admin`. Time-in-state therefore requires the customer to escalate the integration account
  from read-only to fulfiller-grade. That is an adoption cost, not a build cost, and it belongs in
  the build/cancel decision.
- **D3a is live, not theoretical.** Inbound basic-auth restriction is armed on the Australia release
  with an enforcement date; after it, accounts without `snc_basic_auth_api_access` are blocked. v1
  docs must carry the role grant as a prerequisite. Lighthouse **cannot detect this** on the
  customer's behalf — the properties are invisible to the integration account.

## Promotion Decision

**PROMOTED** — accepted at the promotion gate on 2026-07-29. The walking skeleton is ADO **5574**
("Connect and validate a ServiceNow instance"); no separate skeleton work item is created. 5576 is
Removed on the board; 5577 stays conditional on the customer accepting an `itil`-grade account.

The recommendation as it stood before the gate: **PROMOTE**. The probe already performs authenticated read, create and patch against
the real Table API, and `Scripts/DemoEnv/ServiceNowSystemUpdater.py` is a working, committed client
for the same calls. The walking skeleton is a short hop: connect a ServiceNow work-tracking system in
the wizard and list work items for a configured table.

## Design Implications

1. **Connection validation must distinguish authenticated from authorised.** A permitted-but-
   unauthorised read returns `200` with zero rows, so "connected, 0 work items" is indistinguishable
   from a permissions failure. Slice-01 acceptance criterion.
2. **Query validation must detect the no-op case.** An unknown field makes a `sysparm_query` term
   silently vanish and the call returns the entire table — wrong metrics, confidently displayed.
   Compare the configured query's total against the unfiltered total and warn when identical.
   Slice-02 acceptance criterion.
3. **Never page bare `task`.** Always scope by `sys_class_name`; the superclass is full of plugin
   noise (152 `cert_follow_on_task` in the newest 200 rows, zero incidents).
4. **State mapping in labels, never numbers.** `sys_choice` is readable by every account including
   role-less ones, and `state` values collide across subclasses (`3` = On Hold on `incident`,
   Closed Complete on `task`).
5. **Table/field discovery cannot be offered to a least-privilege account** (`sys_db_object` 403,
   `sys_dictionary` empty). Configuration must accept a typed table name.
6. **Batch reads; never per-item.** ~600 ms per call with no rate limiting observed — the constraint
   is wall-clock latency, not throttling.
7. **History reading must tolerate asynchronous metric calculation** and rows with an empty `field`
   or empty `value`; `duration` is a Glide duration rendered as an epoch offset.

## Constraints Discovered

- The Table API silently ignores writes to `sys_user.user_password`; probe users must have passwords
  set through the UI with the `Password` field added via Form Layout.
- Choice values are instance- and release-specific. `close_code` on this release does not accept the
  value present in the instance's own 2016 sample data. Resolve choices at runtime.
- `snc_read_only` grants no read access whatsoever — it is a UI-write-restriction role. Its name
  invites the wrong guess and the customer docs must say so.
- Data policy gates Resolved/Closed on `close_code` + `close_notes`; the error names the form label
  ("Resolution code"), not the element.
- All measurements come from one cloud PDI on the Australia release. On-prem remains unmeasured.

## Open Question Deliberately Not Closed

Whether OAuth reduces the permission ask. Reasoning says no — a ServiceNow OAuth token resolves to a
user and inherits that user's roles, with no scope mechanism — so OAuth solves D3a but not the `itil`
cost for history. Not measured. One OAuth app registered against `lh_probe_snc_read` plus a re-run of
the Q8 matrix would settle it, and should happen before anyone plans OAuth work on the assumption it
lowers the permission bar.
