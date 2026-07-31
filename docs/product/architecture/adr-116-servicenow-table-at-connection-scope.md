# ADR-116: The ServiceNow work-item table is typed at connection scope; discovery is not offered, and portfolio support is declined in the schema

- **Status**: **Proposed** (2026-07-29, Epic 5513 slice 01 DESIGN) — pending maintainer ratification.
- **Date**: 2026-07-29
- **Feature**: epic-5513-servicenow-integration (ADO Epic 5513, Story 5574)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Every existing connector answers "which records are work items?" implicitly. Azure DevOps and Jira infer
it from the query (WIQL / JQL name the projects and types), Linear from the selected team, CSV from the
uploaded file. **ServiceNow is the first system where the entity *kind* is a separate axis from the
filter**: `incident`, `change_request`, `sc_task` and `sc_req_item` are distinct tables, all derived from
`task`, and D4 locked ITSM as the default while keeping the table configurable so an Agile Development
2.0 shop is not locked out.

Three SPIKE measurements constrain how that configuration can work.

**1. Discovery is impossible for the account it would serve.**

| Table | no roles | `snc_read_only` | `sn_*_read` | `itil` |
|---|---|---|---|---|
| `sys_db_object` (table list) | 403 | 403 | **403** | 200 / 4 |
| `sys_dictionary` (field list) | 200 / empty | 200 / empty | **200 / empty** | **200 / empty** |

A wizard that enumerates tables works for the maintainer's `admin` account and shows a customer's
least-privilege account a silent empty list. `sys_dictionary` is worse — it returns 200-with-zero-rows
at *every* level including `itil`, so field discovery fails even for a fulfiller account.

**2. Slice-01 validation needs something to read.** US-01 AC4 requires a distinguishable
insufficient-rights failure, and [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md)
implements it by reading a table and counting rows. If the table name lives only at team scope, then at
connection-validation time there is nothing to probe and AC4's third failure is structurally unreachable.

**3. Portfolio support has been cancelled on evidence.** `task.parent` is a genuine self-referencing
hierarchy that would work mechanically, but was populated on **0 of 94** records; `incident.parent_incident`
on 1 of 94. No portfolio-shaped table exists without plugins — `pm_project`, `pm_portfolio`, `rm_story`,
`rm_epic`, `demand` all return **400, table does not exist**. US-03 / ADO 5576 is Removed. US-03 AC5
requires the limitation to be stated prominently rather than silently deferred.

Meanwhile the frontend's `DataRetrievalSchemaDefaults` holds two **exhaustive**
`Record<WorkTrackingSystemType, IDataRetrievalSchema>` maps — one for teams, one for portfolios — so
adding `"ServiceNow"` to the union forces both entries to be written. There is no way to add ServiceNow
to the frontend and stay silent about portfolios; the only choice is *what* the portfolio entry says.

## Decision

**1. The work-item table is a connection-scope option with an ITSM default.**

| Key | Default | Secret | Optional |
|---|---|---|---|
| `Instance Url` | `""` | no | no |
| `Username` | `""` | no | no |
| `Password` | `""` | **yes** | no |
| `Work Item Table` | **`incident`** | no | yes |

The default encodes D4: a customer who accepts it gets the ITSM path the docs, demo data and worked
examples are built around. Marking it optional means a connection created without touching it is valid.

**2. The query stays at team scope**, as it does for every other system — schema key `servicenow.query`,
`inputKind: "freetext"`, holding a ServiceNow encoded query (`sysparm_query`). This preserves D5: an
opaque, user-authored filter string Lighthouse passes through, the same concept as WIQL and JQL, with no
new UX vocabulary.

**3. Slice 02 may add an optional per-team table override** that falls back to the connection value. It is
not built in slice 01. The split is stated now so the read path is not later forced to invent a second
home for the table name under delivery pressure.

**4. No discovery, and no wizard entry** (`wizardHint: null`). Not deferred for effort reasons — measured
impossible for the target account. Discovery may later be offered as a convenience *for privileged
accounts only*, and if it ever is, it must fail visibly rather than render an empty list.

**5. The portfolio schema entry declines the capability:**

```ts
ServiceNow: { key: "servicenow.portfolio.unsupported",
              displayLabel: "Not supported for ServiceNow",
              inputKind: "none", isRequired: false,
              isWorkItemTypesRequired: false, wizardHint: null }
```

`ValidatePortfolioSettings` returns a matching declared failure. US-03 AC5's "state the limitation
prominently" becomes **structural rather than documentary** — there is no half-working portfolio path to
stumble into, and the type system carries the cancellation.

**6. `isWorkItemTypesRequired: false` on the team schema**, on the reasoning that for the ITSM-first
default the **table is the type** (the Linear precedent, which also declares `false`). A `task`-rooted
read scopes by `sys_class_name` inside the query instead. This is decided on thinner evidence than the
rest of this ADR and is recorded as **C-3** in the feature delta for deliberate revisit at slice 02, where
the type-vs-`sys_class_name` mapping is first exercised. Cost of being wrong: one boolean and one test.

> **Amended 2026-07-31 by [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md)** (Story
> 5611 slice 01) — this is the revisit C-3 asked for, and the cost estimate was wrong twice over. The
> flag does not merely skip validation, it also *hides* the field (Bug #5613), and the two stacks
> disagreed about it. `isWorkItemTypesRequired` is now conditional on the configured table: `true` for a
> known hierarchy root, `false` for a leaf. Decisions 1-5 and 7 of this ADR stand unchanged.

**7. `Password` is marked secret**, so AC5 (never returned in plaintext) is satisfied by the existing
`EncryptSecrets` change-tracker hook and existing DTO redaction. No new mechanism, no schema change, no
EF migration — the enum member is appended after `Csv` and the options are rows, not columns.

## Alternatives Considered

**A. Table at team scope only, encoded in or beside the query.**
Rejected: it makes US-01 AC4's third failure unreachable, because connection validation would have
nothing to read. It also splits one concept — "which table" — across the query string for some users and
a field for others.

**B. Table at connection scope only, with no future team override.**
Rejected as a needless lock-in. A shop reading both `incident` and `change_request` would need two
connections with duplicated credentials. Nothing in slice 01 forces that constraint, so it is not adopted.

**C. Discovery wizard listing tables from `sys_db_object`.**
Rejected on measurement (403 below `itil`), not on effort. It is the same failure shape as
[ADR-115](./adr-115-servicenow-basic-auth-prerequisite-not-detected.md) alternative C: a feature that
works for the developer and silently degrades to an empty list for the customer.

**D. Hardcode `incident` with no configurability at all.**
Rejected — it contradicts D4 and locks out both change-management shops and the Agile 2.0 case for the
sake of one option key.

**E. Leave the portfolio schema at the `defaultSchema` fallback.**
Rejected, and this is the one worth spelling out. The `Record` is exhaustive, so this is not even
reachable without weakening the type; and if it were, ServiceNow would render a generic "Query" field for
a capability that cannot work, producing a portfolio that syncs zero features and looks like a
misconfiguration. That is a silent no-op, which DoD 5 and KPI-3 forbid, and it is the precise outcome
US-03 AC5 was written to prevent.

**F. Omit ServiceNow from the portfolio record entirely.**
Not available: `Record<WorkTrackingSystemType, …>` is exhaustive and `tsc` rejects it. Recorded because
the compiler's refusal is doing real design work here — the frontend type system makes "add the system
but stay quiet about portfolios" unrepresentable.

## Consequences

**Positive.**
- AC4 is satisfiable at connection scope, which is what makes the walking skeleton a real walking
  skeleton rather than a form that only proves reachability.
- A cancelled slice is visible in the product, not just in a document.
- No discovery code is written that would have to be deleted after the first customer report.
- The ITSM default means the common case needs three fields, not four.

**Negative.**
- One connection is bound to one probe table. A multi-table shop configures multiple connections until
  the slice-02 override lands. Accepted for a walking skeleton.
- The administrator must know their table name. This is the direct cost of the discovery finding, and it
  moves load onto the US-05 docs page, which must carry the ITSM table list and a worked example.
- `isWorkItemTypesRequired: false` is a soft call (C-3).
- The frontend `workTrackingSystemGetDataRetrievalDisplayName()` switch has a `default:` arm, so unlike
  the two `Record`s it will **not** force the new case — it would silently render "Query". The only
  drift-prone touch point in the frontend set; it needs an explicit unit test rather than compiler trust.

## Related

- [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md) — consumes `Work Item Table`
  as its probe target.
- [ADR-115](./adr-115-servicenow-basic-auth-prerequisite-not-detected.md) — the same
  measured-invisible-to-the-customer pattern applied to auth.
- [ADR-123](./adr-123-servicenow-record-classes-as-work-item-types.md) — amends decision 6; keeps
  decision 4 (no runtime discovery) by deciding hierarchy membership from a static set rather than
  from `sys_db_object`.
- SPIKE evidence: `docs/feature/epic-5513-servicenow-integration/spike/findings.md` (Q5, Q8).
- DISCUSS D4 / D5 / US-03 AC5: `docs/feature/epic-5513-servicenow-integration/feature-delta.md`.
