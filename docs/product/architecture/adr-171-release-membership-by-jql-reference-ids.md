# ADR-171: Release membership resolves by a second JQL call returning reference ids, batched once per refresh pass

- **Status**: **Proposed** (DESIGN, 2026-08-22)
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slices 01a-02)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

D1 says a source handler owns **which Features are in the Delivery** as well as its date. AC-01.4,
AC-03.1 and AC-03.3 all depend on that membership being computable.

**The obvious implementation does not work, and the DISCUSS inventory said otherwise.** S6 claimed the
full Jira fetch requests `AllFields = "*all"`, "so `fixVersions` already arrives in the payload today".
The payload arrives and is then **discarded**: `grep -rn "fixVersion"` over the whole backend returns
zero, consistent with S5's "the Jira connector has zero Release/`fixVersion` code". The only place an
arbitrary Jira field is retained is `WorkItemBase.AdditionalFieldValues`
(`Models/WorkItemBase.cs:53`) — a `Dictionary<int, string?>` keyed by **admin-configured**
`AdditionalFieldDefinition` ids, holding one string per key. That is S11's complaint exactly: a single
string cannot hold an array of Versions, and nothing configures `fixVersions` by default.

So there is no stored membership to read, and matching against in-memory `portfolio.Features` is not
possible without one. *(S6 has been corrected in the feature delta on the strength of this finding.)*

## Decision

**A second remote call, keyed on the Version id, returning reference ids — batched once per refresh
pass, not once per Delivery.**

1. **The adapter asks Jira which issues carry the Version**, with JQL `fixVersion in (...)`. It returns
   **reference ids only**. Nothing Jira-shaped crosses the port: no Version object, no `fixVersions`
   field name, no JQL. The port speaks
   `IReadOnlyList<string> MemberReferenceIds` and the resolution shape is
   `(string Name, DateTime Date, IReadOnlyList<string> MemberReferenceIds)`.

2. **The domain turns reference ids into Features**, by intersecting with `portfolio.Features` on
   `Feature.ReferenceId`. The intersection is what enforces D1's "matches at the level Lighthouse
   tracks and does not roll up from children" — a Release containing stories and sub-tasks that are not
   Features in this Portfolio contributes nothing, with no filtering rule to write, because they are
   simply not in the set being intersected.

3. **Resolution is batched per pass.** The port method is
   `ResolveMany(portfolio, sourceKey, IReadOnlyList<string> sourceReferences)` returning a dictionary
   keyed by source reference. One refresh pass therefore costs **two remote calls regardless of how many
   Deliveries are bound**:

   - one project-wide `GET rest/api/3/project/{key}/versions` — the list is the same for every bound
     Delivery in the Portfolio, so fetching it per Delivery was pure waste;
   - one `fixVersion in (id1, id2, …)` search asking for `fields=fixVersions`, whose results are grouped
     back to versions in memory.

   Create-time resolution ([ADR-170](./adr-170-broken-source-as-recorded-verdict.md), point 6) calls
   the same method with a single-element list, so there is no second signature to keep in step.

   The narrow `fields=fixVersions` here does **not** touch the identity sweep, which stays
   `SweepFields = "key,updated"` (S6). Different query, different call site.

4. **Matching is on the Version id, which is also the injection answer.** The stored reference is the
   numeric Version id (ADR-167, D3.3), so the JQL term is `fixVersion in (10042, 10043)` with no
   quoting and no user-supplied text. The id is validated as numeric before it is interpolated, and the
   query is built through the connector's existing JQL construction and encoding path (the one behind
   `encodedJql` in the search methods) rather than by concatenation. **Never build this query from the
   Version name**: it is user-supplied text, it needs escaping, and it would break on rename — which
   AC-02.5 forbids anyway.

5. **A batch transport failure is one verdict, not N.** If the search call fails, every reference in the
   batch resolves to `Unavailable` (ADR-170). This is strictly better than per-Delivery calls, where a
   partial failure would produce a mix of verdicts from one outage.

## Cost against the KPI

The DISCUSS KPI is: *Portfolio refresh duration with 5 bound Deliveries within 5% of the same Portfolio
unbound.*

| Shape | Calls per refresh, N bound Deliveries | N = 5 |
|---|---|---|
| Per-Delivery (naive) | 2N — a versions list and a search each | 10 |
| **Batched (chosen)** | **2, constant in N** | **2** |

**The KPI holds with batching and would be at risk without it.** Two extra calls sit against a refresh
that already fetches every Feature in the Portfolio with `*all` fields; the marginal cost is a rounding
error. Ten calls on a small Portfolio — where the baseline refresh is itself short — could plausibly
exceed 5%, which is why this is a design decision rather than an optimisation to be added later.

**Stated honestly**: this is reasoning from call counts, not a measurement. The KPI is measured off
`RefreshLogService` duration at DELIVER, and if batching still misses 5%, the next lever is to skip the
search entirely when no bound Delivery's stored membership could have changed — which cannot be known
without the call, so the real fallback is a longer cadence for source re-sync than for the Feature
fetch, and that would reopen D9.

## Alternatives considered

- **Persist `fixVersions` on `Feature` as a first-class field**, populated by the existing fetch (which
  already receives it), and match in memory with no second call. Cheapest at run time. **Rejected on
  three grounds**: it puts a **Jira concept on a shared model** used by five connectors, four of which
  have no such thing; it needs a **migration** on a large table; and it is **forward-only**, so slice
  01a would preview nothing at all until every Portfolio had completed a full re-sync — which turns the
  walking skeleton into something that cannot be demonstrated on the day it ships. The chosen design
  works against existing data immediately, with no schema change.

- **Configure `fixVersions` as an `AdditionalFieldDefinition`** and read it from
  `AdditionalFieldValues`. **Rejected** — S11: the value is a single string and `fixVersions` is an
  array, so an issue in two Releases stores one of them unpredictably. It also makes the feature depend
  on an admin having configured a field, which is the "quiet wrongness" D3 exists to remove.

- **One call per bound Delivery.** **Rejected** — see the cost table. It is 2N where 2 will do, and the
  data it re-fetches (the project's version list) is identical every time.

- **A single call that fetches every issue in the project and groups by version locally.** **Rejected** —
  it is unbounded in the project's size to serve a handful of Deliveries, and it duplicates the Feature
  fetch that just ran.

## Consequences

**Positive**

- No schema change, no migration, no forward-only gap. Slice 01a previews real Releases against real
  Features on the day it ships.
- Nothing Jira-shaped crosses the port, so a second connector's source handler reuses the whole domain
  side unchanged.
- Cost is constant in the number of bound Deliveries, so the feature does not degrade as it is adopted.

**Negative / accepted**

- Two remote calls the refresh did not previously make, on every Portfolio whose connection offers
  sources — including when nothing has changed. There is no cheap way to know that in advance.
- The adapter now issues a query the identity-sweep discipline does not cover. It must not be confused
  with the sweep, and it must not widen `SweepFields`.
- Hand-exploring the Jira API from a dev machine has previously tripped rate limits that surface as
  unrelated-looking backend test failures. Slice 01a's manual verification should keep its call volume
  low and not run alongside a backend suite.

**Reuse verdict**: the connector's JQL construction and encoding path → **REUSE AS IS** (point 4 — this
is the injection control, and it must not be re-implemented). `Feature.ReferenceId` → **REUSE AS IS**
(the join key; no new field). `WorkItemBase.AdditionalFieldValues` → **UNCHANGED** — evaluated and
rejected as the storage, on S11's evidence. `Feature` → **UNCHANGED**, deliberately: the rejected
alternative was the one that would have modified it. `DeliverySourceResolution` → **EXTEND** — carries
`MemberReferenceIds` rather than Features, so the adapter cannot hand back domain objects.

**Enforcement**

| Rule | Mechanism |
|---|---|
| Nothing Jira-shaped crosses the port | ArchUnitNET: the resolution and option types must not reference any Jira namespace; NUnit asserting the resolution carries `IReadOnlyList<string>`, never `Feature` |
| Membership is the intersection, so foreign issues cannot join a Delivery | NUnit: a batch returning reference ids for issues absent from `portfolio.Features` yields only the Features that are present |
| One pass costs two calls, not 2N | NUnit with a counting fake connector: five bound Deliveries ⇒ exactly one versions call and one search call |
| The query is never built from the Version name | NUnit: a Version whose name contains `")` and `OR` resolves normally, and the issued JQL contains only the numeric id |
| The identity sweep stays narrow | Existing `SweepFields` assertions, re-run; plus a test that resolving does not alter the sweep query |
| A transport failure is one verdict for the whole batch | NUnit: the search throws ⇒ every reference in the batch resolves to `Unavailable`, none to `NotFound` |

Cross-refs [ADR-166](./adr-166-delivery-source-handler-registry-not-connector-port.md) (the provider
this method sits on), [ADR-167](./adr-167-source-binding-as-nullable-columns-behind-a-paired-mutator.md)
(the stored Version id this keys on),
[ADR-170](./adr-170-broken-source-as-recorded-verdict.md) (the verdicts `ResolveMany` returns, and the
shared resolver both callers depend on).
