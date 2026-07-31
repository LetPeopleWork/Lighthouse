# ADR-124: ServiceNow record-class readability — what an ACL-blind count can and cannot prove

- **Status**: **Proposed** (2026-07-31, Story 5611 slice 01 DESIGN) — pending maintainer ratification.
- **Date**: 2026-07-31
- **Feature**: servicenow-multi-table-work-item-types (ADO Story 5611, parent Epic 5513)
- **Deciders**: Benjamin Huser-Berta (maintainer)
- **Builds on**: [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md),
  [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md)

## Context

Epic 5513's whole anxiety is that **quietly wrong beats visibly missing**. ADR-114 exists because
ServiceNow answers a denial with a success — `200` and zero rows. ADR-123 moves the read to a
hierarchy table filtered by `sys_class_name`, which moves that same trap onto a new axis: ServiceNow
evaluates ACLs **per record class**, so an account that may read `incident` but not `problem`, reading
`sys_class_nameINincident,problem`, gets a `200` with the `problem` rows simply absent.

Measured, 2026-07-31, against a live PDI with four accounts:

| account | HTTP | rows | classes returned |
|---|---|---|---|
| `admin` | 200 | 85 | incident 70, problem 15 |
| `lh_probe_itil` | 200 | 85 | incident 70, problem 15 |
| **`lh_probe_snc_read`** (no `sn_problem_read`) | **200** | **70** | **incident 70 — `problem` absent** |
| `lh_probe_none` | 200 | 0 | — |

No error, no header, no partial-result marker. **The correct answer and the ACL-truncated answer are
the same HTTP response with fewer rows in it**, so AC-B6 cannot be satisfied by inspecting the combined
read at all. A misspelt class name is equally quiet: `sys_class_nameINincident,not_a_real_class`
returns the 70 incidents and says nothing about the second name.

One asymmetry is what makes detection possible. **`X-Total-Count` is ACL-blind** — measured directly,
one `sysparm_limit=1` request per account per table:

| account | `/task` header / body | `/incident` header / body | `/problem` header / body |
|---|---|---|---|
| `admin` | 725 / 1 | 103 / 1 | 24 / 1 |
| `lh_probe_itil` | 725 / 1 | 103 / 1 | 24 / 1 |
| `lh_probe_snc_read` | 725 / 1 | 103 / 1 | **24 / 0** |
| `lh_probe_none` | **725 / 0** | **103 / 0** | **24 / 0** |

The header reports what the *instance* holds; the body reports what the *account* may read. That gap
is the only signal there is.

Two more facts constrain the design:

- **`sys_db_object` is not available to build on.** It would separate "empty class" from "misspelt
  name", and it answered 200 for three of the four accounts — but an account that cannot read
  `sys_db_object` also cannot read any class, so an earlier rung always fires first and the ambiguity
  never reaches a user who could act on it. (Drift noted: the epic SPIKE recorded `sys_db_object` as
  403 below `itil`; `lh_probe_snc_read` now reads it, having acquired `cmdb_query_builder_read` and
  friends since. A discriminator whose availability moves under us is not a discriminator.)
- **A pre-existing defect sits in the same mechanism.** `ValidateTeamSettings` compares `matched` to
  `everything`, both taken from `X-Total-Count` (`ServiceNowWorkTrackingConnector.cs:561-591`). Because
  that header ignores ACLs, `lh_probe_none` — which can read nothing at all — gets
  `matched=159, everything=725` and **passes** the widening comparison. Connection validation catches
  that account one rung earlier, which is why it has never been visible. It is recorded here so it is
  not re-derived; repairing it is not in this slice.

## Decision

### 1. AC-B6 is a per-class probe at team-settings validation, and nowhere else

One `GET …?sysparm_limit=1` per named class, run when the coach saves. **n requests at save time,
zero per sync.**

The rejected alternative is checking on every refresh. It pays n requests forever, on every team, to
cover the one case this does not: rights revoked *after* setup. That case is accepted as uncovered —
the connection-level ladder reports it a rung earlier, and a per-sync check would make every team's
refresh cost proportional to how many kinds of work it does.

### 2. The probe addresses the class's own table, and reads a four-rung ladder

`GET {instanceUrl}/api/now/table/{class}?sysparm_limit=1`.

In ServiceNow a class *is* a table, so every value that may legally appear in `sys_class_name` is
addressable this way. The ladder:

| answer | verdict |
|---|---|
| `400` | **the class does not exist** — name it, and say Lighthouse expects the system name (`change_request`), not the label (`Change Request`) |
| `403` | **explicitly refused** — name the class and the role shape to grant |
| `200`, header > 0, body ≥ 1 | readable |
| `200`, header > 0, body = 0 | **denied or invisible** — name the class; say both causes |
| `200`, header = 0 | the class exists and holds nothing. Not an error |

Rungs 3 and 4 are directly measured (the `/incident` and `/problem` rows above). **Rung 1 was measured
after this ADR was drafted** (2026-07-31, same PDI): `GET /api/now/table/not_a_real_class` answers
`400` with `{"error":{"message":"Invalid table not_a_real_class"},"status":"failure"}` — identically
for `admin`, `lh_probe_itil`, `lh_probe_snc_read` and the no-roles `lh_probe_none`. The verdict is
therefore **credential-independent**: an unknown class name reads the same to a least-privilege
integration account as to an administrator, which is the property the rung needed and the one that
could not be assumed.

Rung 2 (`403`) is the one that stays derived, and the same measurement narrowed it: no ITSM
task-descendant returned `403` at any privilege level — a class the account may not read answers
`200`/0 (rung 4), and `403` appeared only for platform tables (`sys_db_object` to a no-roles account,
`metric_definition` below `itil`). So rung 2 is correct where it fires but is probably **unreachable
for the class names a coach actually types**. It stays in the ladder because an instance with
class-level ACLs configured differently may reach it, and because falling through to rung 4 there
would still be truthful. It does not need its own integration assertion.

**This ladder resolves an ambiguity the SPIKE declared unresolvable, and it is worth being explicit
about why.** The SPIKE's three-way table (`header=0` is "empty *or* misspelt, indistinguishable")
describes probing `sys_class_name=<class>` against the *rooted* table, where a bogus class returns
`200` with zero rows. Addressing the class table directly turns that same case into a `400`. The
finding is not contradicted; a different probe was chosen, and it is strictly more informative at the
same cost of one request.

Rung 4 is **suspicion, not proof** — a class whose rows are all filtered out by row-level ACLs for
legitimate reasons produces the same answer as a class-level denial. The message therefore names both
causes rather than asserting a certainty the platform cannot supply. That is the same shape as
`no_records_visible` (ADR-114 decision 4) and `query_matches_whole_table`, and the same house style:
Lighthouse says what it saw and what the two explanations are.

### 3. The widening probe's "everything" becomes the class-filtered baseline

`ValidateTeamSettings` keeps its two-count comparison. What changes is the denominator:

| | before | after |
|---|---|---|
| `matched` | team query on the table | **class filter + team query** |
| `everything` | the table, no query | **class filter, no team query** |

On the PDI: 159 matched against 208, instead of 159 against 725. The ratio keeps meaning *"how much of
your kind of work did this query select"* rather than *"how much of the instance"*.

For a leaf-rooted team with no classes both definitions coincide exactly, so every shipped team's
comparison is unchanged — same two URLs, same two numbers.

Without this, a `task`-rooted team's `everything` counts the whole hierarchy, and the detector that
exists to catch a silently-widened query would compare a correct answer against a number it has no
relationship to. The detector would not fire wrongly; it would simply stop being about anything.

### 4. The verdicts live on `ServiceNowTeamQueryVerdict`, as new rungs

Not a new type. Every rung here answers the same question the existing ones answer — *why can this
team's settings not be saved* — in the same vocabulary, pointing at a settings field by name, and it is
the second of the two things that would have to exist before a third justified extraction. The rungs
added are `missing_work_item_types` (ADR-123 decision 4, pre-flight, no IO) and the four-way class
probe result.

The core stays pure, per ADR-114: the connector performs the requests and hands the verdict scalars —
class name, status, header count, row count. The purity ArchUnit fixture is extended to cover
`ServiceNowTeamQueryVerdict` alongside `ServiceNowValidationVerdict`; today it pins only the latter,
which is a gap this slice closes for the price of one string constant.

### 5. Both behaviours become standing integration assertions, not throwaway probe scripts

The SPIKE's own recommendation, adopted: the `X-Total-Count`-is-ACL-blind ladder and the
class-scoped definition read become assertions in
`ServiceNowWorkTrackingConnectorIntegrationTest` (the fixture slice 02 extended rather than
duplicated). Both assert *instance* behaviour that a future ServiceNow release could change underneath
Lighthouse, and neither is provable from a fixture. Rung 1 of decision 2 — that an unknown class name
yields `400` from `/api/now/table/{class}` — joins that set: it has now been measured (above), and the
assertion exists so a future ServiceNow release cannot quietly turn it into a `200`.

## Alternatives Considered

**A. Inspect the combined read and compare the classes that came back against the classes that were
asked for.**
The obvious design, and it is wrong: it cannot tell an ACL-truncated class from a class that
legitimately has no records matching the team's query. Both are "asked for three, saw two". It would
report a denial to every coach whose change queue happens to be empty this week.

**B. Probe `sys_class_name=<class>` against the configured table instead of the class's own table.**
The form the SPIKE measured, and closer to how the sync actually reads. Rejected because it collapses
"class does not exist" into "class is empty" (measured: a bogus class returns `200` with zero rows,
never widening), and because the ACL-blindness of `X-Total-Count` was measured at *table* granularity,
not through a class-scoped `sysparm_query`. Decision 2 uses the form whose every cell is on record.
This was the named fallback while rung 1 was still inferred. **The `400` has since been measured across
all four probe accounts, so the fallback is not needed** — it stays recorded as the answer if a
customer instance ever disagrees with the PDI.

**C. Build on `sys_db_object` to split "empty" from "misspelt".**
Rejected on the SPIKE's own reasoning: an account that cannot read it cannot read any class either, so
the earlier rung always fires first — plus its readability moved between two SPIKE runs a day apart.
This is ADR-116 alternative C and ADR-115 alternative C again: a discriminator that works for the
developer's account and degrades silently for the customer's.

**D. Run the per-class probe on every sync so revoked rights are caught.**
Rejected on cost, per S2. n requests per refresh, forever, at ~600 ms each, to detect a change of
configuration that the administrator made.

**E. Fix the ACL-blind widening comparison in this slice.**
Rejected as scope. It is pre-existing, it is caught a rung earlier by connection validation, and fixing
it properly means finding a count that is *not* ACL-blind — which the SPIKE did not find and this slice
does not need. Recorded, not repaired.

**F. Leave `everything` as the unfiltered table count.**
Rejected. The comparison would still run and would still not fire wrongly, but it would compare the
team's work against the whole instance and its message ("this query selects every record in 'task'")
would be unreachable and meaningless for exactly the configuration this feature introduces. A detector
that cannot say a true thing is worse than no detector, because it looks like one.

## Consequences

**Positive.**
- A class named but not readable produces a message naming *that class*, instead of a team that quietly
  syncs two thirds of its work.
- A misspelt class name is caught at the moment it is typed, with the name-vs-label correction in the
  message — the single most likely mistake, given that the coach reads "Change Request" on their screen
  and must type `change_request`.
- The widening detector keeps meaning what its message says it means.
- Cost is bounded and paid where a human is already waiting: n cheap requests on Save, none on refresh.

**Negative.**
- Rights revoked after setup are not detected until someone re-validates. Accepted.
- Rung 4 names two causes because it cannot separate them. A coach with a genuinely empty-for-them class
  is told something ambiguous. The alternative is asserting a denial that may not exist.
- Saving a team with many classes costs one round trip per class, ~600 ms each, serially — ten classes
  is roughly six seconds of Save. The DELIVER implementation should say plainly whether that is
  sequential or fanned out; the connector is sequential everywhere else and consistency is worth more
  here than latency.
- Rung 2 (`403`) is retained without a measurement that reaches it — no ITSM class produced a `403` at
  any privilege level. Correct where it fires, likely dead for the inputs coaches supply.

## Related

- [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md) — the ladder this extends, and
  the source of the `unknown_table` / `insufficient_permissions` rungs reused here.
- [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md) — the class filter whose blind
  spot this closes.
- [ADR-115](./adr-115-servicenow-basic-auth-prerequisite-not-detected.md) — the standing prohibition on
  claiming a detection the platform cannot support; alternative C here is the same shape.
- SPIKE evidence: `docs/feature/servicenow-multi-table-work-item-types/spike/findings.md` (OC-2).
