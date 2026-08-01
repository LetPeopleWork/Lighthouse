# ADR-124: ServiceNow record-class readability — what an ACL-blind count can and cannot prove

- **Status**: **Accepted** (ratified 2026-08-01 by the maintainer, at Epic 5513's close).
- **Date**: 2026-07-31
- **Feature**: servicenow-multi-table-work-item-types (ADO Story 5611, parent Epic 5513)
- **Deciders**: Benjamin Huser-Berta (maintainer)
- **Builds on**: [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md),
  [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md)
- **Decision 2 RE-ORDERED 2026-07-31 (maintainer, same story)**, after
  [ADR-116](./adr-116-servicenow-table-at-connection-scope.md) decision 1 was withdrawn and every
  read became rooted at `task`. The probe that runs **first** is now the one that asks about the
  read Lighthouse actually performs; the class's own table is a lazy explanation of a nil result.
  One request per kind of work when the configuration is right, two only for the one that is wrong.
  Everything decisions 1, 3, 4 and 5 say is unchanged.

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

### 2. Ask about the read, then — only if it found nothing — ask the class's own table why

> **RE-ORDERED 2026-07-31 (maintainer).** The DELIVER-review amendment below added the second probe
> in the right place but kept it second, so a correct configuration paid two requests per kind of
> work and the *first* question asked was a proxy: *"is this name a readable table somewhere on this
> instance?"* With the root now a constant, that is not a question the sync depends on. The order is
> inverted:
>
> | # | probe | when |
> |---|---|---|
> | 1 | `/api/now/table/task?sysparm_limit=1&sysparm_query=sys_class_name={class}` | always |
> | 2 | `/api/now/table/{class}?sysparm_limit=1` | only when probe 1 reported `header = 0` |
>
> Probe 1's ladder: `header > 0` with a row — **pass, done, one request**; `header > 0` with an
> empty body — `class_records_not_visible`, the ACL denial the whole AC rests on; `header = 0` —
> inconclusive, and the only case that pays for probe 2.
>
> Probe 2's ladder, reached only for a class the hierarchy holds none of: `400` — the name is not a
> table, say so and give the name-versus-label correction; `403` — `insufficient_permissions`;
> `200` with `header > 0` — a real, populated table that is not work: **`class_is_not_a_kind_of_work`**;
> `200` with `header = 0` — a kind of work the instance holds none of anywhere, **accepted** (OQ-8).
>
> **Measured on the PDI, 2026-07-31, one row per rung** — the ordering was verified before it was
> written, not after:
>
> | case | probe 1 (`/task?sys_class_name=X`) | probe 2 (`/X`) | verdict |
> |---|---|---|---|
> | `incident`, admin | 200, header 103, 1 row | *not run* | pass |
> | `problem`, `lh_probe_snc_read` | 200, header 32, **0 rows** | *not run* | `class_records_not_visible` |
> | `not_a_real_class`, admin | 200, header 0 | **400** | `unknown_table` |
> | `sys_user` / `cmdb_ci` / `kb_knowledge`, admin | 200, header 0 | 200, header 641 / 2784 / 53 | `class_is_not_a_kind_of_work` |
> | `incident_task`, admin | 200, header 0 | 200, header 0 | **accepted** |
> | `metric_definition`, `lh_probe_none` | 200, header 0 | **403** | `insufficient_permissions` |
>
> **Nothing the previous ordering caught is lost**, and the cost of a right configuration halves.
> The verdict code changed with it: `class_not_under_configured_table` named a relation to a
> configured table that no longer exists. `class_is_not_a_kind_of_work` says what the two probes
> jointly established — a real table whose records are not work — keeps the `class_*` prefix
> `class_records_not_visible` set, and reads the same as its message.
>
> One thing this ordering gives up, stated: probe 2's own ACL rung. An account shown no rows of a
> table that is not work now hears "that is not a kind of work" rather than "you cannot read it".
> That is the more useful of the two answers, and probe 1 already ruled on visibility for every
> class that is work.

**What follows is the 2026-07-31 DELIVER-review amendment that introduced the second probe. Its
reasoning about *why both probes are needed* stands verbatim; only their order changed.**

> **Amended 2026-07-31 (DELIVER review, finding 1). The original single probe validated the wrong
> fact.** `/api/now/table/{class}` establishes *"this name is a readable table on this instance"*.
> The read needs the strictly stronger *"records whose own `sys_class_name` is this name are readable
> **under the table this connection is rooted at**"*. Nothing checked the second, so every mismatch
> failed open and silent — the exact silent subset AC-B6 exists to prevent.
>
> Measured against the PDI as `lh_probe_snc_read`, 2026-07-31:
>
> | probe | status | header | rows |
> |---|---|---|---|
> | `/change_request` | 200 | **105** | 1 |
> | `/incident?sysparm_query=sys_class_name=change_request` | 200 | **0** | 0 |
> | `/incident?sysparm_query=sys_class_name=incident` | 200 | 103 | 1 |
> | `/task?sysparm_query=sys_class_name=task` | 200 | **30** | 0 |
> | `/task?sysparm_query=sys_class_name=problem` | 200 | 24 | **0** |
> | `/task?sysparm_query=sys_class_name=problem` (as `admin`) | 200 | 24 | 1 |
> | `/incident?sysparm_query=sys_class_name=not_a_real_class` (as `admin`) | 200 | **0** | 0 |
> | `/not_a_real_class` (as `admin`) | **400** | absent | — |
>
> Row 1 against row 2 is the defect: a connection rooted at `incident` whose team names
> `[incident, change_request]` passes the ladder on `change_request`, passes the widening comparison
> (70 matched against 103, unequal, therefore valid), saves — and syncs incidents only, saying
> nothing. That configuration is now reachable *by construction*, because the always-required
> amendment (ADR-123 decision 6) forces every coach to name kinds of work and
> `useModifySettings.handleWorkTrackingSystemChange` carries a Jira-shaped list across a connector
> change without clearing it. Row 4 generalises it: exact match is not hierarchy-inclusive
> (ADR-123 measurement 4), so naming a table that has descendants silently drops them.
>
> Rows 5 and 6 are what makes the second probe safe to build a rung on: **the ACL-blindness of
> `X-Total-Count` survives a class-scoped `sysparm_query`**, not only table granularity. That was
> the one cell alternative B was rejected over, and it is now on record.
>
> **The second probe is skipped for a class the instance holds nothing of anywhere** (`header = 0`
> on its own table). Under any table that class answers `header = 0`, so "not under this table" and
> "empty everywhere" would be one answer — and OQ-8 settled that an empty kind of work is a
> legitimate configuration. The honest limit: a genuinely empty class named against the wrong root
> is still accepted. The instance cannot tell the two apart, and the charitable reading is the one
> already decided.
>
> Cost: two requests per named class at save time, six for a three-class team, still serial. S2 and
> OQ-5 already accepted serial uncapped probing at Save; this is that budget doubled at the one
> moment a human is waiting. It is **not** fanned out.

#### 2a. The class's own table

`GET {instanceUrl}/api/now/table/{class}?sysparm_limit=1`.

In ServiceNow a class *is* a table, so every value that may legally appear in `sys_class_name` is
addressable this way. The ladder:

| answer | verdict |
|---|---|
| `400` | **the class does not exist** — name it, and say Lighthouse expects the system name (`change_request`), not the label (`Change Request`) |
| `403` | **explicitly refused** — name the class and the role shape to grant |
| `200`, header > 0, body ≥ 1 | readable |
| `200`, header > 0, body = 0 | **denied or invisible** — verdict code **`class_records_not_visible`** (maintainer, 2026-07-31); name the class; say both causes |
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

#### 2b. That class under the connection's table

`GET {instanceUrl}/api/now/table/{configuredTable}?sysparm_limit=1&sysparm_query=sys_class_name={class}`.

The clause is generated by the same pure function the read emits (`ServiceNowReadScope.Matching`),
so the probe cannot drift into a form the sync never asks in, and one class always means the
measured `=` shape rather than an unmeasured one-element `IN` (ADR-123 decision 2).

| answer | verdict |
|---|---|
| not a readable `200` | the connection's **table** is the subject, so the ladder names the table, not the class — `unknown_table` / `insufficient_permissions` / `unexpected_response` |
| `200`, no `X-Total-Count` | **`result_size_unknown`**, naming the table |
| `200`, header = 0 | **`class_not_under_configured_table`** — the class exists, holds records, and none of them are under this table |
| `200`, header > 0, body = 0 | **`class_records_not_visible`** — same rung as 2a, reached through the class filter |
| `200`, header > 0, body ≥ 1 | readable under this root — pass |

`class_not_under_configured_table` is the code because it names the *relation* that is wrong rather
than a property of either side. The class exists — probe 2a would have answered `400`. It is not
empty — probe 2a would have answered `header = 0` and this probe would never have run. "Not under
the configured table" is what is left, it keeps the `class_*` prefix `class_records_not_visible`
established, and it reads the same as the message. The message names **both** the class and the
table and deliberately does **not** claim the class does not exist: probe 2a covers that case with a
better message, and telling a coach to check a spelling that is right is the failure mode ADR-115
exists to forbid.

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

**Decision 2b joins it too, and is the clearest case of the rule** (added 2026-07-31): the two probes
diverge only against a real instance — `/change_request` answers 105 while
`/incident?sysparm_query=sys_class_name=change_request` answers 0, to the same account, in the same
second. A fixture can be made to say either. `AKindOfWorkThatDoesNotLiveUnderTheConfiguredTable_Is
ToldApartFromOneThatDoes` asserts both halves, so an instance that starts resolving `sys_class_name`
across the hierarchy fails loudly rather than leaving behind a refusal that has quietly become wrong.

## Alternatives Considered

**A. Inspect the combined read and compare the classes that came back against the classes that were
asked for.**
The obvious design, and it is wrong: it cannot tell an ACL-truncated class from a class that
legitimately has no records matching the team's query. Both are "asked for three, saw two". It would
report a denial to every coach whose change queue happens to be empty this week.

**B. Probe `sys_class_name=<class>` against the configured table instead of the class's own table.**

> **Corrected 2026-07-31 (DELIVER review, finding 1). This was rejected as an *alternative*, and that
> framing was the mistake.** The two probes answer different questions and neither subsumes the
> other, so the resolution is that **both run** — see decision 2 as amended. What follows is why B
> alone is insufficient, which is still true and is the reason 2a was not simply replaced.

The form the SPIKE measured, and closer to how the sync actually reads. It cannot stand alone,
because it collapses "class does not exist" into "class is not under this table": measured, `admin`
asking `/incident?sysparm_query=sys_class_name=not_a_real_class` gets `200` with `header = 0` — the
same answer `change_request` gives against the same table. Only `/not_a_real_class` distinguishes
them, with a `400`, credential-independently.

The second stated reason for rejecting B — that the ACL-blindness of `X-Total-Count` had been
measured at *table* granularity but not through a class-scoped `sysparm_query` — **has since been
measured and holds**: `/task?sysparm_query=sys_class_name=problem` reports 24 in the header and
returns zero rows to `lh_probe_snc_read`, and 24 with a row to `admin`. That was the cell that had to
exist before B could carry a rung of its own, and it does.

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
- Saving a team costs **one** round trip per class that is genuinely a kind of work, and two for one
  that is not (re-ordered 2026-07-31; it was two for every class between the DELIVER review and
  this). At ~600 ms each, ten correct classes is roughly six seconds of Save. Sequential, as the
  connector is everywhere else; a fan-out would be this adapter's only concurrent call path against
  an instance whose rate-limiting behaviour is measured at exactly one request rate.
- A class the instance holds nothing of **anywhere** is accepted, because at that point the two
  probes have said the same thing and neither can distinguish "a kind of work with nothing in it"
  from "a table that is not work and happens to be empty". Named rather than hidden; OQ-8 already
  chose the charitable reading for an empty class.
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
