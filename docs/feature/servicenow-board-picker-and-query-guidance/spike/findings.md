# SPIKE findings — ServiceNow: pick a Visual Task Board

**Feature**: `servicenow-board-picker-and-query-guidance` · **ADO**: [#5610](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5610)
**Run**: 2026-08-01, against PDI `dev191338.service-now.com` (the epic-5513 instance), timebox 1 h, actual ~35 min.
**Accounts**: `admin`, `lh_probe_itil` (itil + `sn_*_read/write`), `lh_probe_snc_read` (`sn_incident_read`,
`sn_change_read`, `sn_request_read`, **no** `sn_problem_read`), `lh_probe_none` (only
`snc_basic_auth_api_access`) — the set 5611's SPIKE created, reused unchanged.

## Verdict: **WORKS** — slice 02 is buildable, but three of its written assumptions are wrong

The mechanism is there: boards exist, carry a table and a verbatim encoded query, and are readable by
a non-admin account. What the probe overturns is *how* they are reached, *what* the card set means,
and *which* of the two filter columns is safe to copy.

---

## The claim this probe was called to disprove — disproven

`IServiceNowWorkTrackingConnector.cs:3-5` has said since `4b55362be` (2026-07-29, slice-04 DISTILL):

> *Deliberately does NOT extend IBoardInformationProvider: ServiceNow has no board concept and table
> discovery is unavailable to a least-privilege account, so there is no wizard to feed.*

The first half is false. `vtb_board` exists on a stock PDI and holds exactly what a picker needs:

| column | value on the dogfood board |
|---|---|
| `name` | `Incidents Kanban` |
| `table` | `incident` |
| `filter` | `correlation_id=LIGHTHOUSE_DEMO` |
| `readable_filter` | `Correlation ID = LIGHTHOUSE_DEMO^ORDERBY` |
| `field` | `state` (the lane field) |
| `last_synchronized` | `2026-07-31 17:11:25` |

The second half is a different claim (`sys_db_object` discovery, ADR-116, still true) and does not
support the first. Both comments — the interface xmldoc and `DataRetrievalSchemaDefaults.ts:64`
("No wizard: SPIKE Q8 measured table/field discovery unavailable below itil") — must be amended when
slice 02 lands, or review will read them as authority against the slice.

---

## OC-1 — SETTLED, and the ladder's first answer was the wrong one

**Question**: is the board table readable by a least-privilege account, or does it need `itil`/admin?

The naive ladder says admin-only, in the epic's signature shape — 200 with zero rows:

| account | `vtb_board` | `vtb_card` | `vtb_lane` |
|---|---|---|---|
| `admin` | 200, 2 rows | 200, 50 rows | 200, 14 rows |
| `lh_probe_itil` | **200, 0 rows** | **200, 0 rows** | **200, 0 rows** |
| `lh_probe_snc_read` | **200, 0 rows** | **200, 0 rows** | **200, 0 rows** |
| `lh_probe_none` | **200, 0 rows** | **200, 0 rows** | **200, 0 rows** |

Read as a role gate that would have cancelled slice 02. It is not a role gate. The read ACL on
`vtb_board` carries **no role at all** — `sys_security_acl_role` has rows for `vtb_card.*`,
`vtb_lane.*`, `vtb_board_member.*` and `vtb_card_history.*` (all `admin`) and **none for
`vtb_board`**. What guards it is a script:

```javascript
answer = new VTBBoardSecurity().canAccess(current);
```

A visual task board is **shared, not roled**. `vtb_board_member` was empty on this instance, so both
boards were visible to their owner (`admin`) and to nobody else.

**Proved by construction.** One `vtb_board_member` row for `lh_probe_itil` on *Incidents Kanban*, then
the same reads, then the row deleted:

| | `vtb_board` rows | board by `sys_id` | its `vtb_card` | its `vtb_lane` |
|---|---|---|---|---|
| before | 0 | 0 | — | — |
| **as a member** | **1** (`Incidents Kanban`, with `table` and `filter`) | **1** | **38** | **6** |
| after delete | 0 | 0 | — | — |

`itil` was never the gate: the account that read nothing before and everything after held the same 46
roles throughout.

**Design consequence, and it is the whole shape of the feature**: the board list is scoped to the
*connection's* service account. Lighthouse authenticates as one user, so the picker shows the boards
that user owns or is a member of — never "all boards on the instance". Two things follow:

1. An empty list is **ambiguous** — no boards exist, or none are shared with this account — and must be
   worded as the latter, because that is the case a customer will actually hit. This is the R-2 lesson
   in a new place: do not render "no boards found" when the true statement is "this account is not a
   member of any board".
2. Onboarding gains a step that belongs in #5578's docs: *share the board with the Lighthouse account*.
   Nothing in Lighthouse can do it — `vtb_board_member` is admin-write.

## OC-7 — SETTLED: yes, `X-Total-Count` is ACL-blind here too

Every 0-row read above returned `X-Total-Count: 2`. Same defect 5611 measured on `incident`
(header 103 / body 0), on a second table. The picker must never count boards from the header, and the
existing widening detector's use of that header is confirmed wrong on a second surface, not just the
one ADR-124 recorded.

## OC-3 — SETTLED: two filter columns, and the human-readable one is poison

| column | value | run as `sysparm_query` |
|---|---|---|
| `filter` | `correlation_id=LIGHTHOUSE_DEMO` | 38 of 105 incidents · 13 of 118 change requests |
| `readable_filter` | `Correlation ID = LIGHTHOUSE_DEMO^ORDERBY` | **105 of 105** · **118 of 118** |

`filter` is a **verbatim encoded query in column form** — copy it into the team's query field unchanged,
no translation, no parsing. `readable_filter` is the label form, and running it silently matches the
**whole table** — exactly the `query_matches_whole_table` widening the slice-04 dogfood hit and
`ValidateTeamSettings` exists to block.

The trap is sharper than the open call assumed: `readable_filter` is what the **ServiceNow UI shows**,
so it is the string a user would read off their screen and the one a careless implementation would
prefer for being legible. Pre-fill `filter`. Never `readable_filter`. Show `readable_filter` in the
dialog as the *description* of what was picked if a human-readable label is wanted — never as the value.

## OC-2 — SETTLED: **no**, a board's cards are not its filter's result

| board | cards | filter result | cards − filter | filter − cards |
|---|---|---|---|---|
| Incidents Kanban (`incident`) | 38 | 38 | 0 | 0 |
| Change Requests by State (`change_request`) | **7** | **13** | 0 | **6** |

The card set is a **snapshot** — `vtb_board` carries `last_synchronized`, and a filtered board
materialises `vtb_card` rows at sync time. Six change requests match the board's filter today and have
no card. Cards are always a *subset*; the filter is live.

This does not cancel the slice — it corrects it. Lighthouse wants the **filter**, which is a live
query and the right thing to copy. But **AC-B6 as written is unsatisfiable**: "the synced item set
equals the board's card set" can never hold on a drifting board, and asserting it would fail on a
correct implementation. Restate it as *the synced set equals the board's filter run against the board's
table* — which is the invariant that actually holds, and which the probe verified on both boards.

The freeform case needs no heuristic. A board created with no table and no filter stores exactly that:

| board | `table` | `filter` |
|---|---|---|
| freeform (created and deleted in this run) | `''` | `''` |

So D10's "excluded or refused by name" is decidable from two empty strings — no card-set inspection, no
guessing. All boards live in one `vtb_board` table; `sys_class_name` is empty on every row, so there is
no board-type discriminator to read. Emptiness *is* the discriminator.

## OC-5 (the narrowed survivor) — a non-task board is real, and unreachable

A board on `cmdb_ci` was created without complaint (`table='cmdb_ci'`, `filter='operational_status=1'`).
Against a task-rooted read it yields nothing:

```
task?sysparm_query=sys_class_name=cmdb_ci   -> X-Total-Count: 0
task?sysparm_query=sys_class_name=incident  -> X-Total-Count: 105
```

So a `cmdb_ci` board pre-filled into Work Item Types produces a team that syncs zero items — the quiet
wrong number this epic exists to prevent. The picker must refuse it by name.

**Constraint for DESIGN** — *corrected 2026-08-01, see the correction note at the end of this file*:
verifying that a board's table is a task descendant needs the class hierarchy. This section originally
said `sys_db_object` is **403 below `itil`**; that is **stale** — 5611's own findings measured 200 for
three of four probe accounts, 403 only for `lh_probe_none`. The conclusion survives for a better
reason: an account that cannot read `sys_db_object` cannot read any class either, so the readability
ladder's first rung fires first and the ambiguity never reaches a user who could act on it. **Do not
build on `sys_db_object`.** DESIGN settled this by reuse instead — see ADR-125 and the DESIGN sections
of `../feature-delta.md`.

---

## What this changes for DESIGN

1. **The picker is buildable.** `IServiceNowWorkTrackingConnector` extends `IBoardInformationProvider`,
   `WizardsController`'s switch gains its ServiceNow arm, and `BoardInformation` is filled from
   `vtb_board` with zero contract change — `DataRetrievalValue` ← `filter`, `WorkItemTypes` ← `table`.
   D6 and D7 hold as written.
2. **Scope the promise to the service account.** Not "your boards" — the boards this connection can
   see. Empty-list copy must say so.
3. **`filter`, never `readable_filter`.** Worth a named test: pre-filling the label form reproduces the
   whole-table widening on both tables measured here.
4. **AC-B6 must be restated** before DISTILL — see OC-2. Filed as an upstream correction, not a
   design choice.
5. **Refusals are cheap and total**: empty `table` or empty `filter` ⇒ freeform ⇒ refuse. Non-task
   `table` ⇒ refuse. Both decidable from the board row alone, except the hierarchy check above.
6. **D9's empty-fallback fix is load-bearing here**, not merely tidy — though *not* for the reason D9
   gives. Every failure mode this probe found (not a member, freeform, wrong hierarchy) arrives at
   `BoardWizard.tsx:71-82` as an all-empty `IBoardInformation` that is truthy and enables Confirm.
   **Corrected 2026-08-01**: it does not blank a typed query.
   `GeneralSettingsComponent.tsx:59-95` guards every assignment on non-emptiness, so an all-empty
   payload writes nothing at all. The defect is a refusal wearing a success costume — Confirm succeeds
   and silently does nothing — which is the same family, one rung less severe. Fixing it lands for
   Jira, ADO and Linear too.
7. **OC-6 is untouched by this probe.** **Corrected 2026-08-01**: it is not true that no channel
   exists. `ConnectionValidationResult.Advisory`/`AdvisoryCode` ship (ADR-118 D5) and
   `ValidationAdvisory.tsx` renders them on both connection surfaces. What is missing is the *team*
   leg: `TeamService.ts:97-109` collapses the validate response to `isValid === true`, so an advisory
   riding a successful team validation is dropped before it can be shown. DESIGN settled this in
   ADR-127.

## Constraints discovered

- `vtb_board` read is script-guarded (`VTBBoardSecurity.canAccess`), not role-guarded. Roles predict
  nothing; membership predicts everything.
- `vtb_board_member` is admin-write. Lighthouse cannot grant itself board access — this is a documented
  onboarding step or nothing.
- `X-Total-Count` is ACL-blind on `vtb_board`/`vtb_card`/`vtb_lane`, confirming the defect generalises.
- A filtered board's `vtb_card` set drifts behind its filter. Never treat cards as membership.
- `sys_class_name` is empty on every `vtb_board` row — one table, no subclasses, no type column.
- Do not build task-descendance on `sys_db_object`. **Not** because it is 403 below `itil` — that was
  stale, corrected 2026-08-01 — but because an account that cannot read it cannot read any class
  either, so the readability ladder answers first and better (5611 findings, `:110-122`).

## Reproducing

Probe scripts (throwaway, no build): `snow.py`, `snowrw.py`, `survey.py`, `oc1.py`, `oc1b.py`,
`oc1c.py`, `oc1d.py`, `oc2.py`, `oc2b.py` — plain `urllib`, credentials from
`$ServiceNowLighthouseIntegrationTestToken`, same account set as
`ServiceNowWorkTrackingConnectorIntegrationTest`.

**Instance left as found**: `oc1d.py` deletes the `vtb_board_member` row it creates; `oc2b.py` deletes
both boards it creates. Verified at the end of each run — `vtb_board_member` empty, board list back to
*Change Requests by State* and *Incidents Kanban*.

**Recommendation**: the OC-3 pair (`filter` narrow / `readable_filter` whole-table) deserves to become a
standing guard in `ServiceNowWorkTrackingConnectorIntegrationTest`, alongside 5611's OC-2 ladder. It
asserts instance behaviour that a future ServiceNow release could change underneath us, and getting it
wrong ships the exact bug the epic's validation exists to catch.

---

## Corrections (2026-08-01, from DESIGN)

DESIGN checked three claims in this file against the codebase and disproved all three. Corrected in
place above; recorded here so the record shows what was believed and why it was wrong. **None of them
changes a measurement this probe took** — all three were inherited assertions restated from upstream,
not things the probe observed.

| Claim as written | What is actually true | Where |
|---|---|---|
| `sys_db_object` is **403 below `itil`** (OC-5, Constraints) | Stale — it answers **200 for three of the four probe accounts**, 403 only for `lh_probe_none`. Inherited from the *epic* SPIKE matrix; 5611's own findings already flagged it as drift and the reason: `lh_probe_snc_read` has since acquired `cmdb_query_builder_read`. The "don't build on it" conclusion stands for a different reason. | `servicenow-multi-table-work-item-types/spike/findings.md:110-122` |
| D9's all-empty pre-fill **blanks a typed query** | It writes **nothing**. `handleWizardComplete` guards every assignment on non-emptiness (`if (boardInfo.dataRetrievalValue.trim() !== "")` and four siblings). Confirm succeeds and silently no-ops. Still worth fixing — a refusal wearing a success costume — but one rung less severe than data loss. | `GeneralSettingsComponent.tsx:59-95` |
| OC-6 has **no channel at all** | The channel exists and ships: `ConnectionValidationResult.Advisory`/`AdvisoryCode` (ADR-118 D5) rendered by `ValidationAdvisory.tsx` on both connection surfaces. What is missing is the *team* leg — `validateTeamSettings` collapses the body to `isValid === true`, dropping an advisory that rides a success. | `ConnectionValidationResult.cs:35-39`, `TeamService.ts:97-109` |

The lesson worth keeping: a probe's own measurements were sound, and every claim that turned out wrong
was one it repeated from an upstream document without re-checking. Re-measure inherited constraints,
or cite them as inherited.
