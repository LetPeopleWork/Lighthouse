# ADR-114: ServiceNow connection validation returns a coded verdict ladder, and a permitted-but-unauthorised read is a failure — never a success

- **Status**: **Proposed** (2026-07-29, Epic 5513 slice 01 DESIGN) — **pending maintainer ratification**,
  jointly with the US-01 AC4 amendment this ADR forces (recorded as contradiction **C-1** in
  `docs/feature/epic-5513-servicenow-integration/feature-delta.md`).
- **Date**: 2026-07-29
- **Feature**: epic-5513-servicenow-integration (ADO Epic 5513, Story 5574 — walking skeleton)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Lighthouse has four work-tracking connectors. In all four, a connection either works or fails loudly:
Azure DevOps and Jira return a non-2xx on a bad credential, Linear returns GraphQL errors, CSV validates
locally. `ValidateConnection` has therefore never had to distinguish *authenticated* from *authorised*.

ServiceNow breaks that assumption. The SPIKE measured it directly on PDI `dev191338` (Australia release)
across five accounts — `admin`, no-roles, `snc_read_only`, `sn_*_read`, `itil`
(`docs/feature/epic-5513-servicenow-integration/spike/findings.md`, Q8 matrix):

| Table | no roles | `snc_read_only` | `sn_*_read` |
|---|---|---|---|
| `incident` | **200 / 0 rows** | **200 / 0 rows** | 200 / 5 rows |
| `task` | **200 / 0 rows** | **200 / 0 rows** | 200 / 5 rows |
| `change_request` | **200 / 0 rows** | **200 / 0 rows** | 200 / 5 rows |

**A permitted-but-unauthorised read returns `200` with an empty result set.** ServiceNow's ACL engine
filters rows rather than refusing the request. There is no error, no warning header, and `X-Total-Count`
reports 0.

A connector written to the precedent of the other four — "2xx means the credential works" — would report
*"Connection valid"* to an administrator whose account can read nothing, and then hand them an empty team
a week later to debug as if it were a query problem. That is the single highest-cost failure mode in the
epic, and D11 makes it a first-class concern rather than an edge case: the whole point of Epic 5513 is
that the customer's account is deliberately least-privilege.

Compounding it, three plausible discriminators were each measured **unavailable** to the account that
would need them: `sys_db_object` → 403 below `itil`; `sys_dictionary` → 200 with zero rows at *every*
role level including `itil`; `sys_properties` → 200 with zero rows. Lighthouse cannot look the answer up.

## Decision

**1. `ValidateConnection` performs a real read against the configured table and counts the rows.**
Reachability and authentication are necessary but not sufficient evidence. The probe is
`GET {instanceUrl}/api/now/table/{workItemTable}?sysparm_limit=1` — one call, ~600 ms measured, cheap
enough to run on every Validate click and every settings save.

`sysparm_fields` is deliberately **not** used. The SPIKE never measured whether field projection
interacts with ACL row filtering, and a probe whose whole job is to distrust the substrate must not
itself rest on an unmeasured mechanism.

**2. The verdict is produced by a pure function, not by branching inside the HTTP call.**
`ServiceNowValidationVerdict` is a static class with three entry points, each returning a
`ConnectionValidationResult`:

- `FromInvalidInstanceAddress(string instanceUrl)` — rung 0, pre-flight, before any IO.
- `FromUnreachableInstance(string technicalDetails)` — rung 1, the transport never got an answer.
- `FromResponse(HttpStatusCode statusCode, bool responseIsJson, int rowCount, string table)` — rungs 2-7,
  the instance answered and the three scalars are everything the ladder needs.

`ServiceNowWorkTrackingConnector` is the imperative shell: it performs the call, catches transport
exceptions, extracts the scalars and returns what the mapper says, unchanged. An earlier draft of this
ADR described a single `From(status, rowCount, contentIsJson, table)` entry point; the shipped shape
splits the two no-response rungs out so neither has to invent a status code it never received.

**3. The ladder** — the first matching rung wins:

| # | Observation | `Code` | `IsValid` |
|---|---|---|---|
| 0 | Base URL is not an absolute `Uri` (pre-flight, no IO) | `invalid_url` | false |
| 1 | `HttpRequestException` / timeout — DNS, refused, TLS | `connection_failed` | false |
| 2 | `401` | `authentication_failed` | false |
| 3 | `200` with a non-JSON body | `unexpected_response` | false |
| 4 | `400` | `unknown_table` | false |
| 5 | `403` | `insufficient_permissions` | false |
| 6 | `200`, JSON, **zero rows** | `no_records_visible` | **false** |
| 7 | `200`, JSON, **one or more rows** | `valid` | true |

**Rung 3's input is decided by parse-and-catch, never by the `Content-Type` header** (maintainer
ruling, 2026-07-29, closing the one open question the DESIGN peer review raised). The shell attempts
`JsonDocument.Parse(body)` and treats a `JsonException` as "not JSON". ServiceNow's gateway — and any
reverse proxy or SSO portal sitting in front of it — controls that header, so it can claim JSON while
serving a sign-in page; meanwhile the body is parsed anyway to count rows, so the check costs nothing
extra. Both the detection and rung 3 itself remain a hypothesis rather than a measurement: no SSO-fronted
instance was probed during the SPIKE (see Consequences).

**4. Rung 6 is a failure, and it names both of its possible causes.** Zero visible rows means *either*
the account lacks read access to the configured table *or* the table is genuinely empty. The platform
provides no way to separate them, so the message says so and gives the actionable next step:

> The credential authenticated, but the table `{table}` returned no visible rows. Either the account
> lacks read access to it — grant `sn_incident_read` or the matching per-table role; note that
> `snc_read_only` grants no read access at all despite its name — or the table is genuinely empty.

Claiming certainty here would be a lie, and reporting it as `connection_failed` would send the
administrator to check the network. Reporting it as `valid` is the bug this ADR exists to prevent.

**5. No port change.** `IWorkTrackingConnector` and `ConnectionValidationResult` are used unchanged.
`Code` is already a free-form per-connector string — Jira emits `invalid_url` / `authentication_failed` /
`connection_failed` / `additional_fields_invalid`, Linear `validation_failed` / `no_work_items_found`,
CSV `missing_required_option`. There is no shared enum to widen and no exhaustive switch to break, so a
fifth connector's codes cannot affect the other four.

## Alternatives Considered

**A. Follow the existing precedent — treat any 2xx as valid.**
Rejected. It is the defect described in Context, and it is silent: nothing in logs, metrics or the UI
would distinguish a correctly-configured connection from a blind one. The cost lands weeks later on the
person least able to diagnose it. It also directly violates the epic's KPI-3 ("0 silent no-ops") and D11.

**B. Detect the cause of rung 6 by reading instance metadata.**
Rejected on measurement, not on taste. Every candidate is invisible to a least-privilege account:
`sys_db_object` 403, `sys_dictionary` 200-with-zero-rows at all levels, `sys_properties`
200-with-zero-rows. An earlier SPIKE draft asserted this was possible; it had been measured as `admin`
and was **disproven** when re-run as `sn_*_read`. Building it would produce a diagnostic that works for
the maintainer and silently never fires for any customer.

**C. Use `/api/now/stats/{table}?sysparm_count=true` for an unfiltered count.**
Rejected as unmeasured. The aggregate API is almost certainly subject to the same ACL filtering — in
which case it returns 0 and buys nothing — but the SPIKE did not probe it. Adopting an unverified
mechanism as the cure for a substrate that lies would repeat the mistake this ADR is about. Listed in
the Tier-2 catalogue as a cheap future measurement, not as a design assumption.

**D. Probe a table guaranteed to be non-empty and role-gated (e.g. `sys_user`).**
Rejected. It would decide the verdict on a table the customer does not care about; a pass would prove
only that *some* read works, and the configured table could still be invisible. Also unmeasured.

**E. Inline the ladder in `ValidateConnection` rather than extracting a pure mapper.**
Rejected on testability. The ladder is the only interesting logic in slice 01 and the DoD demands ≥80 %
mutation kill. Inline, every rung is reachable only through an `HttpMessageHandler` mock; extracted, all
seven are a table-driven unit test and the mutants land where the risk is. The extraction costs one
~40-line static class.

**F. A cross-connector `IConnectionProbe` port implemented by all five connectors.**
Rejected as speculative generality. Exactly one connector has this problem today. Revisit at the rule
of three.

## Consequences

**Positive.**
- The headline ServiceNow failure mode is structurally unshippable: a `200`-with-zero-rows response
  cannot produce `IsValid == true` without deleting a rung, which an integration test asserts against.
- Seven observable `(Code, IsValid)` pairs give DISTILL an acceptance-test spine that needs no
  ServiceNow-specific test infrastructure.
- The other four connectors are untouched. Zero regression surface.
- The ladder is reusable reasoning: any future connector whose platform filters rows rather than
  refusing requests (a common ACL design) now has a precedent.

**Negative.**
- **US-01 AC4 must be amended.** It asks for a "lacks read access" verdict the platform cannot supply.
  The amendment is drafted as C-1 in the feature delta and this ADR does not proceed to DELIVER without
  it. Recorded rather than papered over.
- `ValidateConnection` now costs a real table read (~600 ms measured on a PDI, unmeasured on-prem) where
  the other connectors cost an identity call. Acceptable for a user-triggered action; re-measure on the
  customer instance before promising the "time to first metric" KPI (US-06 AC3).
- Rung 3 (`unexpected_response`, the SSO-fronted-instance case) is **a hypothesis, not a measurement** —
  the only such rung. It is tagged as such everywhere it appears so a later reader cannot mistake a
  defensive guess for a finding.
- One more class in the connector namespace than the Linear precedent has.

**Enforcement** — three orthogonal layers, following the project's existing ArchUnitNET convention
(`Lighthouse.Backend.Tests/Architecture/`, 7 fixtures):

1. **Structural** — `ServiceNowValidationVerdict` must not depend on `HttpClient`, `ILogger` or
   `Lighthouse.Backend.Data`. Keeps the functional core pure.
2. **Behavioural** — a table-driven fixture over all seven rungs.
3. **Contract** — an integration test asserting `IsValid == false` for a `200`-with-zero-rows response.
   The single assertion that makes the headline bug non-shippable.

## Related

- [ADR-115](./adr-115-servicenow-basic-auth-prerequisite-not-detected.md) — rung 2 carries the
  basic-auth-restriction hint, and why detection is forbidden.
- [ADR-116](./adr-116-servicenow-table-at-connection-scope.md) — where `{workItemTable}` comes from.
- SPIKE evidence: `docs/feature/epic-5513-servicenow-integration/spike/findings.md` (Q8 matrix).
