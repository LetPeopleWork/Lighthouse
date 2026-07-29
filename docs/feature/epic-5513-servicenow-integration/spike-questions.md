# SPIKE — ServiceNow viability (Epic 5513)

**Status**: not started · **Gate**: blocks slices 01-05 and blocks DESIGN
**Timebox**: 2 days. If a question is still open at the box, record it as open and take the
documented fallback — do not extend to "just get one more answer".

**Environment**: a self-provisioned ServiceNow cloud Personal Developer Instance (D10), **provisioned
and seeded by the environment-prereq story before this SPIKE starts** — not inside the timebox. That
story also stands up a minimal `Scripts/DemoEnv/ServiceNowSystemUpdater.py`, which keeps the PDI from
hibernating, supplies this SPIKE's test records, and — because creating and transitioning `incident`
records is real Table API traffic — already answers part of Q2 and Q4. Read its findings before
re-asking them here.

On-prem is *not* used here; it is the slice-05 validation target.

**Rule for this SPIKE**: every answer is a recorded HTTP call — request, status, and response shape —
not a documentation quote. The desk research below is what we *expect*; the SPIKE exists because we
do not yet know.

**Already closed before the SPIKE ran** (2026-07-27, maintainer's first-hand evidence — do not re-open
these unless the instance contradicts them): **D3** auth = basic auth, confirmed working on the target
on-prem instance; **D4** data model = ITSM `task`-derived tables as the default, table configurable.
Q1 and Q2 below are correspondingly narrowed from "choose" to "confirm the mechanics". This makes the
SPIKE materially shorter than first scoped — likely 1 day, not 2.

---

## Q1 — Basic auth: confirm the mechanics, not the choice (unblocks slice 01)

**D3 is already decided — basic auth, on the maintainer's first-hand evidence from the target on-prem
instance. This question is no longer "which protocol?".** What remains is mechanical:

- Confirm basic auth on `GET /api/now/table/{table}` against the cloud dev instance and record the
  exact 401 vs 403 behaviour.
- Establish whether a *rights* failure is distinguishable from a *credential* failure in the response
  — status code, body, or error string. This is what US-01 AC4 needs, and without it an admin cannot
  tell "wrong password" from "no read access to this table".
- Note (do not act on) any instance property that would disable basic auth, so the docs can tell a
  customer what to check if it is off (D3a).
- **Fallback**: if 401 and 403 are indistinguishable, US-01 AC4 collapses from three messages to two
  — record that and amend the AC rather than inventing a probe that guesses.

## Q2 — ITSM field mapping: can we read a `task`-derived table generically? (unblocks **D4**, slice 02)

**D4 is already decided — ITSM is the default and the optimisation target.** This question is about
making that concrete, not about choosing.

- For `incident` (and then `sc_task` / `change_request`), list the fields that populate a Lighthouse
  work item, and confirm they are consistent across `task` descendants — i.e. can one generic
  `task`-family reader serve all of them?
- `sysparm_display_value`: do we need display values, raw values, or both? ServiceNow state is
  commonly a **numeric choice with a separate label** [hypothesis], and US-02 AC3 requires the
  mapping UI to show the label, never the integer. Establish exactly how to get both.
- Confirm (cheaply, as a note) that a configured `rm_story` table still reads through the same code
  path, so an Agile 2.0 shop is not locked out — this is a *non-goal to preserve*, not a feature to build.
- **Fallback**: if `task` descendants need per-table field maps, ship v1 against `incident` alone and
  say so in the docs, rather than half-supporting three tables.

## Q3 — Query: is `sysparm_query` our "query" concept? (unblocks **D5**, slice 02)

Confirm an encoded query string round-trips: a user-authored filter that Lighthouse passes through
opaquely, exactly as it does WIQL and JQL.

- Does `sysparm_query` support the predicates a team actually needs (state, assignment group, sprint,
  date ranges, ordering)?
- Is there a UI path where a user can *build* the query and copy it out (the equivalent of Jira's
  JQL bar)? This decides how the docs teach it.
- **Fallback**: if the query language cannot express a team boundary, fall back to a
  filter-by-assignment-group option field and drop the free-text query.

## Q4 — Fields: can we populate a Lighthouse work item? (unblocks slice 02, AC2)

For an ITSM `task`-derived table (D4), identify the source for: id, title, type, state, created,
**started**, closed, parent reference, and whatever "blocked" means.

- **Is there a defensible *started* timestamp?** This is the highest-consequence question in the whole
  SPIKE. `task` exposes `opened_at`, `sys_created_on`, `work_start`, `resolved_at`, `closed_at`
  [hypothesis] — establish which are actually populated in practice, not merely present in the schema.
  Cycle time is meaningless without a defensible start, and a `work_start` that is null on every real
  record is worse than no field at all.
- `On Hold` (and its `hold_reason`) is the natural ITSM blocked-state candidate — note whether it is
  reachable as a plain state value, since that decides whether Lighthouse's blocked rules work here
  later without new mechanism.
- **Fallback**: if there is no usable started date, derive it from the first transition into a
  Doing-mapped state — which makes **Q6 (history) load-bearing for cycle time**, not just for
  time-in-state, and **inverts the slice order** (04 becomes a prerequisite of 02). If this fires,
  stop and re-plan the slices rather than patching around it.

## Q5 — Hierarchy: is there a parent concept for Portfolios? (unblocks **D7**, slice 03)

**ITSM first (D4)** — and this is where the ITSM default costs us something, so ask it honestly.

- `task.parent` exists as a self-reference [hypothesis]. Is it *populated* in real ITSM use, and would
  a customer recognise a parent ticket as a Lighthouse "feature"? A field that exists but is empty is
  a no.
- Any other ITSM rollup a customer would accept — `parent_incident`, a Demand, an SPM Project, an
  Epic from SPM rather than Agile 2.0?
- Secondary, for completeness: does `rm_story` carry a usable reference to `rm_epic`? [hypothesis: yes]
  If Agile 2.0 has a clean hierarchy and ITSM does not, say so plainly — that is a real finding about
  which customers get portfolio support, and it belongs in the docs and the viability verdict.
- Can children be queried *by* parent in one call, or is it N+1 per feature? (N+1 may still be viable
  but changes the refresh-cost story — measure, do not assume.)
- **Decides**: whether US-03 is built or **cancelled** (US-03 AC5 — cancelled loudly, in the docs).
  Choosing ITSM makes cancellation **more** likely than the Agile-2.0-first framing did; that is an
  accepted, eyes-open consequence of D4, not a surprise.

## Q6 — Transition history: affordable and readable? (unblocks **D6**, slice 04)

- Can `sys_audit` / `sys_history_line` be read through the Table API with a **read-only, non-admin**
  role? [hypothesis: technically yes; ServiceNow's own guidance discourages querying these at scale]
- Is `metric_instance` (a Metric Definition on the State field) a better source — and does it require
  instance-side configuration the customer must perform? If it does, it conflicts with the
  no-instance-side-setup scope line.
- What does it cost for ~500 items — one call, or one per item?
- **Decides**: whether US-04 is built or cancelled. D6 already ships the honest downgrade in slice 02,
  so "no" here is cheap.

**Answered pre-SPIKE, as a two-rung ladder.** Probing on PDI dev191338 (admin, Australia release)
plus a seeded create-and-transition run settled the source question; what remains for the SPIKE is
whether a *customer's* least-privilege user can reach rung 1.

1. **`metric_instance` when available.** The PDI ships an active `field_value_duration` metric
   definition — "Incident State Duration" on `incident.incident_state`. A seeded incident driven
   `New → In Progress` produced, within seconds, one row per state span:
   `incident_state=New start=06:46:44 end=06:46:50 duration=00:00:06 complete=true`, then
   `incident_state=In Progress start=06:46:50` left open with `calculation_complete=false`. That is
   Lighthouse's transition-history shape directly, no reconstruction. Spans exist only from the
   moment the record was created under an active definition, so **partial history is the expected
   case and is acceptable.** Detect during connection validation whether definitions are active for
   the configured table, so the mode is known up front rather than discovered mid-refresh.
2. **Otherwise, an explicit unsupported mode.** Candidate: treat time-in-state as the whole time the
   item was active — a single Doing span from start to done. It must read visibly as a downgrade
   (D6's honest-downgrade precedent), never a silent approximation presented as measured data.

**`sys_audit` is ruled out by decision, not by evidence.** It is present (41 133 rows) and readable
as admin, and it is the only universal, retroactive source — but a customer platform team will
realistically never grant read on it, so building a fallback nobody can switch on is waste. Metrics
or nothing. Do not spend SPIKE time probing it beyond the Q8 role check.

`sys_history_line` is not a rung either — 0 rows, because it is a view populated on demand, not a
queryable store.

**Q4 is closed by the same evidence.** `work_start` stays empty after a real API-driven transition
with business rules firing, so there is no trustworthy started-timestamp on the record. The started
time is instead the `start` of the first metric span mapping to Doing — which means Q4 and Q6 have
one answer, and the "no started date re-orders the slice plan" risk is retired rather than realised.

Two parsing traps for slice 04: `duration` is a Glide duration rendered as an offset from the epoch
(`1970-01-01 00:00:06` is six seconds, not a date), and some rows carry an empty `field`, so a reader
must not assume every row is a state span.

## Q7 — Volume and rate limits (unblocks slice 02 AC7, KPI "time to first metric")

- Pagination mechanics (`sysparm_limit` / `sysparm_offset`) and whether a stable total count is available.
- Instance rate limits: what are they, are they per-user or per-instance, and what does exceeding one
  return? A refresh that trips a rate limit must fail visibly, not silently truncate.
- **Fallback**: if limits are tight, the connector needs backoff before slice 02 ships, not after.

## Q8 — Minimum role set (unblocks **D11**, US-05 AC2, US-06 AC2)

Starting from an admin account, remove rights until sync breaks; record the minimum that still works.
This is the deliverable a customer's platform team will actually read.

- Create a restricted user on the PDI and re-run the pre-SPIKE probe set as that user: `incident`,
  `task`, `change_request`, `sc_task`, `sys_choice` (Q10), `metric_definition` + `metric_instance`
  and `sys_audit` (Q6's rungs 1 and 2). All seven answered 200 as `admin`, which proves nothing about
  a customer. The rung-by-rung result *is* Q6's answer.
- **Fallback**: if the minimum is effectively `admin`, that is a serious adoption finding — record it
  as a headline risk in the SPIKE report, because it may sink the market thesis on its own.

## Q9 — Cloud vs on-prem divergence (unblocks slice 05, US-06 AC3)

Without on-prem access during the SPIKE, establish what *could* differ and how to detect it: API
version reporting, plugin availability, auth restrictions, table presence. Produce a short
**detection checklist** the maintainer can run under a restricted account with no Lighthouse build
(US-06 AC1) — this is the artifact that makes the on-prem fallback usable at all.

## Q10 — Can state mapping be authored in labels instead of magic numbers? (unblocks slice 01 wizard)

`state` is an integer whose meaning depends on the table it lives on. Confirmed on PDI dev191338:
`task.state` is `1=Open, 2=Work in Progress, 3=Closed Complete, 4=Closed Incomplete, 7=Closed Skipped,
-5=Pending`, while `incident.state` is `1=New, 2=In Progress, 3=On Hold, 6=Resolved, 7=Closed,
8=Canceled`. **`3` means "On Hold" on one and "Closed Complete" on the other.** Asking a user to type
a raw number into the ToDo/Doing/Done mapping is therefore both hostile and quietly wrong-able.

The choice list is readable through the Table API — `sys_choice` with
`name=<table>^element=state^language=en` returned the label sets above — so the connector can offer
labels at setup and store the numbers itself.

- Does that query work for an **arbitrary configured table**, including a custom `task`-derived one,
  or only for the ITSM tables that ship with choice lists?
- Does it work for a **least-privilege user**? Rides along with Q8's restricted-user run.
- Is `language` a complication on a non-English instance — do we filter by language, or take whatever
  the authenticated user's language yields?
- **Decides**: whether slice 01's wizard offers a label picker or falls back to numeric entry. Not a
  blocker either way; a "no" costs UX, not viability.

---

## SPIKE exit criteria

1. Every question above has a recorded answer or an explicit "still open + fallback taken".
2. **D6 and D7** are resolved to a verdict in `feature-delta.md` (edit the Locked Decisions table).
   D3 and D4 are already closed — amend them only if the instance actively contradicts them.
3. US-03 and US-04 are each marked **build** or **cancelled**, with the finding that decided it.
4. Q4's started-date answer is recorded explicitly, because a "no" re-orders the slice plan.
5. A go / narrow / stop signal for the epic as a whole, if the SPIKE surfaces a blocker (Q8 returning
   "effectively admin" is the live candidate) — do not proceed to slice 01 on a broken premise.
6. Requirements completeness re-scored; expected >0.95 (currently 0.93).

**Promotion**: if the SPIKE's probe code reaches a working authenticated Table API read, promote it
into slice 01 rather than rewriting it — the walking skeleton is the same call.
