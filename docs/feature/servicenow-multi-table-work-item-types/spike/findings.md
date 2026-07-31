# SPIKE findings — ServiceNow: several kinds of work on one team

**Feature**: `servicenow-multi-table-work-item-types` · **ADO**: [#5611](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5611)
**Run**: 2026-07-31, against PDI `dev191338.service-now.com` (the epic-5513 instance), timebox ~1 h, actual ~40 min.
**Accounts**: `admin`, `lh_probe_itil` (itil + sn_*_read/write), `lh_probe_snc_read` (`sn_incident_read` +
`sn_change_read` + `sn_request_read`, **no** `sn_problem_read`), `lh_probe_none` (no roles).
Wrong-password control returned 401 throughout, so every row below is genuinely that account.

**Instance shape**: `task` holds **725** records across **14** classes — `cert_follow_on_task` 268,
`sysapproval_group` 125, `change_request` 105, `incident` 103, `alm_transfer_order_line_task` 45,
`task` 30, `problem` 24, the rest ≤10. Every one of the 14 extends `task` **directly**; the tree is
one level deep on this instance.

---

## Verdict: **WORKS** — D2 holds, with four consequences DESIGN must absorb

`sys_class_name` filtering on a hierarchy-rooted read is the right model. It is also *not* the whole
job: three of the four consequences below were not visible from the DISCUSS wave, and one of them
(history) silently removes a capability slice 04 just shipped.

---

## OC-1 — SETTLED: the class filter binds correctly, in **both** forms

Four query shapes read from `/api/now/table/task`, compared as **sets of `sys_id`** against the
reference answer (each class read from its own table with the same team query). Full pager, not a
first page.

| team query | C reference | A `^OR` chain, prepended | A′ `^OR` appended | B `IN`, prepended | B′ `IN` appended | D no class filter |
|---|---|---|---|---|---|---|
| `active=true` | 159 | **159 identical** | 159 identical | 159 identical | 159 identical | 579 / 13 classes |
| `active=true^ORpriority=1` | 171 | **171 identical** | 171 identical | 171 identical | 171 identical | 591 / 13 classes |
| `active=true^ORDERBYsys_created_on` | 159 | **159 identical** | 159 identical | 159 identical | 159 identical | 570 / 13 classes |
| *(none)* | 208 | **208 identical** | 208 identical | 208 identical | 208 identical | 725 / 14 classes |

Zero extra rows, zero missing rows, in every cell. The precedence worry was specific and it was
tested specifically: row 2 gives the team its **own** `^OR`, so a filter that bound wider than
intended would have returned inactive incidents. It did not — ServiceNow grouped it as
`(class OR class) AND (active OR priority)`. Row 3 adds the `^ORDERBYsys_created_on` the connector
appends unconditionally (`ServiceNowWorkTrackingConnector.cs:497-502`); a trailing `ORDERBY` does not
disturb the grouping either.

**So OC-1 neither survives nor dissolves — it is answered: both forms are correct here.** The tie is
broken on other grounds, and `IN` still wins them: it is one condition instead of *2n−1*, it keeps
the URL budget that `ServiceNowHistoryQuery.RecordsPerBatch` already measured against an 8192-byte
cliff, and it is the form whose correctness does not depend on a grouping rule this probe verified on
exactly one instance version. **Generate `sys_class_nameIN…`.**

**D3 is confirmed by the D column**: a `task`-rooted team with a query but no class filter reads
**579** records of 13 classes where it wanted 159 of 2 — 3.6× too much, and 725/14 with no query at
all. Rooting at `task` without a class filter is exactly the "reports the whole instance" failure
AC1 was written to prevent.

---

## OC-2 — SETTLED, and worse than the open call assumed

### An unreadable class vanishes without a trace

`lh_probe_snc_read` holds `sn_incident_read` but not `sn_problem_read`. Reading
`sys_class_nameINincident,problem^active=true` on `task`:

| account | HTTP | rows | classes returned |
|---|---|---|---|
| `admin` | 200 | 85 | incident 70, problem 15 |
| `lh_probe_itil` | 200 | 85 | incident 70, problem 15 |
| **`lh_probe_snc_read`** | **200** | **70** | **incident 70 — `problem` simply absent** |
| `lh_probe_none` | 200 | 0 | — |

No error, no header, no partial-result marker. This is the epic's `200/EMPTY` trap moved to the class
dimension, and **AC-B6 cannot be satisfied by one read** — the correct answer and the ACL-truncated
answer are the same HTTP response with fewer rows in it.

A wrong class name is equally quiet: `sys_class_nameINincident,not_a_real_class` returns the 70
incidents and says nothing about the second name. One reassurance, though —
`sys_class_name=not_a_real_class` alone returns **0 rows, not the whole table**, so a bogus class
value does *not* reproduce the widening that a bogus *field* caused
(`ServiceNowTeamQueryVerdict.cs:12`). Wrong class names narrow; they never widen.

### The detection that does work — and why it works

`X-Total-Count` is **ACL-blind**. Measured directly, one `sysparm_limit=1` request per account:

| account | `/task` header / body | `/incident` header / body | `/problem` header / body |
|---|---|---|---|
| `admin` | 725 / 1 | 103 / 1 | 24 / 1 |
| `lh_probe_itil` | 725 / 1 | 103 / 1 | 24 / 1 |
| `lh_probe_snc_read` | 725 / 1 | 103 / 1 | **24 / 0** |
| `lh_probe_none` | **725 / 0** | **103 / 0** | **24 / 0** |

The header reports what the instance holds; the body reports what the account may read. That gap is
a defect in the shipped widening detector (below) and, at the same time, **the mechanism AC-B6
needs** — one cheap request per named class yields a three-way verdict:

| header | body | verdict |
|---|---|---|
| > 0 | ≥ 1 | class is readable |
| > 0 | 0 | **ACL denial** — name the class in the message |
| 0 | 0 | class is empty *or* the name does not exist — indistinguishable at the Table API |

Verified across all four accounts and five class names, including `not_a_real_class`.

**Post-DESIGN addendum, same day.** DESIGN (ADR-124 decision 2) proposed a strictly better probe:
address the class's **own table** (`/api/now/table/{class}`) rather than filtering the rooted table.
Measured afterwards, all four accounts: an unknown class answers **`400`
`{"error":{"message":"Invalid table not_a_real_class"},"status":"failure"}`** — including the no-roles
account, so the verdict is credential-independent. That splits the `header = 0` ambiguity this section
called unresolvable: *misspelt* is a `400`, *empty* is a `200` with `header = 0`. The finding above is
not wrong — it describes the rooted-table probe, where a bogus class genuinely is indistinguishable —
DESIGN simply chose a different probe at the same cost of one request.

Same measurement narrowed the `403` rung: **no ITSM task-descendant returned `403` at any privilege
level.** A class the account may not read answers `200`/0; `403` appeared only for platform tables
(`sys_db_object` to a no-roles account, `metric_definition` below `itil`). The `403` rung is correct
where it fires and probably unreachable for the class names a coach types.

`sys_db_object` also separates "empty" from "misspelt", and answered 200 for three of the four accounts (403 only for
`lh_probe_none`) — but an account that cannot read `sys_db_object` also cannot read any class, so the
first rung fires first and the ambiguity never reaches a user who could act on it. **Do not build on
`sys_db_object`.** (Noted as drift: the epic SPIKE matrix recorded `sys_db_object` as 403 below
`itil`; `lh_probe_snc_read` now reads it, having acquired `cmdb_query_builder_read` and friends since.)

### The shipped widening detector measures the wrong quantity

`ValidateTeamSettings` compares `matched` to `everything`, both taken from `X-Total-Count`
(`ServiceNowWorkTrackingConnector.cs:561-591`). Since that header ignores ACLs, `lh_probe_none` —
which can read *nothing* — gets `matched=159, everything=725` and passes the comparison. This is a
**pre-existing** defect, not one this feature introduces; connection validation catches that account
one rung earlier. What this feature *does* change is the denominator: for a `task`-rooted team,
`everything` becomes the whole hierarchy (725), so the ratio the detector reasons about stops meaning
"how much of your table did this query select". DESIGN must decide whether the "everything" probe
carries the class filter or not, and say so.

---

## OC-3 — SETTLED at zero cost: **names, not labels**

Under the connector's own read (`sysparm_display_value=all`, no `sysparm_fields`):

```
sys_class_name: {"display_value": "Change Request", "value": "change_request"}
```

`sysparm_query` matches the **stored value**, so Work Item Types holds `change_request`, never
`Change Request`. `sys_class_name` is present in every record of the connector-shaped read without
adding `sysparm_fields` — so D4 (`Type = sys_class_name`) costs one `ReadForm(record, …, UniversalForm)`
call and no extra request, and does not disturb the "field projection was never measured against ACL
row filtering" caution at `ServiceNowWorkTrackingConnector.cs:813`.

**Exact match is not hierarchy-inclusive**: `sys_class_name=task` returns the **30** records whose
own class is `task`, while `/api/now/table/task` returns all **725**. A coach who types `task` into
Work Item Types gets the base-class records only. That is defensible — it is what "kind of work"
means — but it is a documentation line, because the same word means two things one field apart.

---

## Not asked, found anyway — two design implications

### 1. A `task`-rooted team loses transition history entirely

`ServiceNowHistoryQuery.DefinitionQueryFor(table)` builds `table=<configured>^type=field_value_duration`.
Measured on this instance:

| query | definitions |
|---|---|
| `table=task^type=field_value_duration` (what a task-rooted team asks) | **0** |
| `table=incident^…` (today's leaf-rooted team) | 4 |
| `tableINincident,change_request^…` | 6 |
| `tableINincident,problem^…` | 5 |

`metric_instance` agrees: 196 rows for `table=incident`, 7 for `problem`, **0 for `table=task`**.
Metric definitions are attached to concrete classes; nothing is ever attached to the base table. So
the shipped slice-04 history read returns nothing for exactly the configuration slice 01 introduces,
and the team degrades to no started dates and no state spans. It degrades *visibly* —
`ServiceNowHistoryVerdict` already reports history availability at connection validation — but it
degrades.

**Repair is one query**: scope definitions to the classes (`tableIN…`) rather than the table. Both
`IN` forms above returned the correct union. **This makes slice 01 touch `ServiceNowHistoryQuery`,
which the slice brief's IN-scope list does not mention.**

Worth flagging separately: `change_request` on a stock PDI carries **no state-tracking definition at
all** (its two are on `approval` and `type`). So "incidents and changes on one team" yields history
for the incidents and none for the changes, and no amount of Lighthouse code changes that. It is an
instance-configuration fact for the docs, not a bug.

### 2. One team, several classes, one state mapping — and the labels collide *usefully*

State labels and their underlying choice values, per class, over the same read:

| class | labels (value) |
|---|---|
| `incident` | New (1), In Progress (2), On Hold (3), Resolved (6), Closed (7) |
| `change_request` | New (**−5**), Assess (−4), Authorize (−3), Scheduled (−2), Implement (−1), Review (0), Closed (**3**), Canceled (4) |
| `problem` | New (**101**), Assess (102), Root Cause Analysis (103), Fix in Progress (104), Resolved (106), Closed (**107**) |
| `sc_task` | Open (1) |

A team spanning those four classes must map **14 distinct labels** instead of 5. `Closed` is choice
`3`, `7` and `107` depending on class; `New` is `−5`, `1` and `101`. Because the connector maps by
**label** (`ReadStateLabel` → `display_value`, epic SPIKE Q10), one "Closed" mapping covers all
three — a value-based mapping would have needed three and would have collided with
`change_request`'s `Closed = 3` against `incident`'s `On Hold = 3`. **The label decision, taken for a
different reason, is what makes multi-class teams workable at all.**

The cost lands on the coach: 14 labels to map by hand, and an unmapped label is work that silently
does not exist (`GetWorkItemsForTeam` drops `StateCategories.Unknown`; `ReportStatesTheTeamNeverMapped`
is the only thing that says so). On this instance a coach who maps the five incident labels and stops
loses **61 change requests sitting in `Authorize`** — 69 % of that class. DESIGN should treat the
state-mapping step, not the class list, as this feature's real usability risk.

---

## What this changes for DESIGN

1. **Generate `sys_class_nameIN…`**, prepended to the team's query, before `InAStableOrder` appends
   the `ORDERBY`. Both orders and both forms measured identical; `IN` wins on URL budget and on not
   depending on a grouping rule.
2. **AC-B6 is a per-class probe**, not an inspection of one combined read: header > 0 with an empty
   body is the denial, and it is the only signal there is. Cost is one `sysparm_limit=1` request per
   named class, at validation time — not per sync.
3. **Slice 01 must also touch `ServiceNowHistoryQuery.DefinitionQueryFor`**, or ship a task-rooted
   team with no history. Add it to the slice's IN scope.
4. **Decide what the widening detector's "everything" means** for a hierarchy-rooted team, and
   record that `X-Total-Count` is ACL-blind so the next reader does not re-derive it.
5. **Docs**: class **names** not labels; `sys_class_name=task` is 30 records and not 725; the state
   labels of every class you name have to be mapped, and mapping only one class's labels loses the
   others without an error.

## Constraints discovered

- `X-Total-Count` ignores ACLs. Any count read from it is "what the instance holds", never "what this
  account can see".
- Metric definitions exist only on concrete classes; the base table never has them.
- A bogus `sys_class_name` narrows to zero and never widens — the opposite of a bogus field name.
- Measured on one PDI (Zurich-era stock content, one-level task tree). The `^OR` grouping result in
  particular is an observation about this instance version, which is the second reason to emit `IN`.

## Reproducing

Probe scripts (throwaway, no build): `survey.py`, `oc1.py`, `oc2.py`, `oc2b.py`, `oc2c.py`, `oc3.py`,
`oc4.py`, `oc5.py`, `oc5b.py` — plain `urllib`, credentials from
`$ServiceNowLighthouseIntegrationTestToken`, same account set as
`ServiceNowWorkTrackingConnectorIntegrationTest`. Kept until the promotion gate is decided.
**Recommendation**: the OC-2 ladder and the history-definition scoping deserve to become standing
guards in `ServiceNowWorkTrackingConnectorIntegrationTest` (the fixture slice 02 extended rather than
duplicated), because both assert instance behaviour that a future ServiceNow release could change
underneath us.
