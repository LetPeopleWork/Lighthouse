# ADR-143: Write-back batches per work item, and falls back to unbatched on failure — batching must not widen the blast radius of one bad mapping

**Status**: Accepted
**Date**: 2026-08-08
**Feature**: `quiet-jira-writeback` (ADO Epic #5500 "Quiet write-back", slice 02 / Story #5503)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Evidence**: SPIKE-03 Q9 and the batched-failure probe recorded in
`docs/feature/quiet-jira-writeback/slices/slice-02-batch-writeback-fields-per-issue.md`

---

## Context

Both write-back-capable connectors issue **one API call per field**, not per work item.
`WriteBackFieldUpdate` is one field on one item and both adapters loop the flat list:
`JiraWorkTrackingConnector.UpdateItem` (`:307-325`) serialises a single-entry `fields` dictionary;
`AzureDevOpsWorkTrackingConnector.UpdateItems` (`:330-353`) builds a `JsonPatchDocument` holding exactly
one `JsonPatchOperation`. A Feature with four percentile mappings plus FeatureSize plus WorkItemAge costs
six calls to the same item in one pass. Jira's `fields` object and ADO's patch document each accept many
per call.

Three measured facts shape the decision.

**The value is call count and history churn, not email.** SPIKE-03 Q9: one PUT with four fields produced
**1** changelog entry, four single-field PUTs produced **4**; ADO went `rev 1→2` batched against `rev 1→4`
unbatched. But Jira Cloud batches watcher mail per (recipient, issue) over a ~10 minute window, so both
shapes delivered **exactly one** email. The email claim is void and must not appear in docs or release
notes.

**Batching introduces a regression nobody listed.** Both providers reject a mixed-validity payload
**atomically** — verified against real instances, with the valid fields proven valid by re-sending them
alone immediately afterwards:

| Connector | Payload | Result | Valid fields landed? |
|---|---|---|---|
| Jira | 3 fields, `duedate:"not-a-date"` | `400 {"errors":{"duedate":"The duedate must be of the format \"yyyy-MM-dd\""}}` | **no** |
| Jira | 3 fields, unknown `customfield_99999` | `400 {"errors":{"customfield_99999":"Field … cannot be set…"}}` | **no** |
| ADO | 3 ops, `TargetDate:"not-a-date"` | `400 TF401326: Invalid field status 'InvalidType' …` | **no** (`rev` unchanged) |
| ADO | 3 ops, one field the credential may not write | `403` | **no** — permission failures are atomic too |

Today one bad mapping loses only its own field. Naive batching would let a single misconfigured mapping
take down **every** write-back field on that work item. That is a regression, and AC-05.8 exists for it.

**Error attribution differs by provider.** Jira returns a structured per-field map, so several culprits
are machine-readable at once. ADO returns one message naming one field behind a `TF`-code. Parsing
`TF`-codes is brittle.

## Decision

**Group by work item, send one call per item, and on any non-403 failure re-send that item's fields
individually.**

1. **Grouping lives in the adapter, not on the port.** `IWorkTrackingConnector.WriteFieldsToWorkItems`
   keeps its flat `IReadOnlyList<WriteBackFieldUpdate>` signature. Each adapter groups by `WorkItemId`
   and builds its own payload — Jira a multi-key `fields` object, ADO a multi-operation
   `JsonPatchDocument`. The port has **five** implementations (Jira, Azure DevOps, ServiceNow, Linear,
   CSV), three of which throw `NotSupportedException`; a batching-shaped signature would force all five
   to be re-signed to express a fact only two of them act on.
2. **Failure fallback, by status, with no overlap with ADR-142:**
   - **403** → drop the *suppression*, keep the *batch* ([ADR-142](./adr-142-writeback-suppression-optimistic-retry.md)).
   - **Any other failure** → drop the *batch*, keep the suppression. Each field is re-sent as its own
     call, each still carrying `notifyUsers=false` and each still eligible for its own 403 retry.

   One rule, two orthogonal degradations, evaluated in that order. The good fields land, the offending
   field fails alone, and attribution is exact without parsing a vendor error string.
3. **Per-field result granularity is preserved unconditionally.** `WriteBackResult.ItemResults` still
   carries one `WriteBackItemResult` per `(WorkItemId, TargetFieldReference)` whether the write went out
   batched or unbatched. Callers cannot tell which path ran, and nothing downstream needs to.
4. **AC-05.3 stays retired.** There is no partial-application case to assemble, because no provider
   partially applies. The only two shapes are "the batch landed" and "the batch was rejected whole".
5. **`GetChangedFields` is indexed, and the duplicate-reference warning survives.**
   `WriteBackService.GetChangedFields` (`:86`) currently materialises every Feature and every Work Item
   and rescans the whole list per update — O(updates × items). It becomes a single
   `ToLookup(x => x.ReferenceId)` built once. **Not `ToDictionary`**: the existing code deliberately
   handles and warns on more than one item sharing a reference, and a dictionary would throw where today
   it logs and takes the first match.
6. **Existing per-field coercion is applied per field inside the batch.** Jira's
   numeric-versus-string coercion (`JiraWorkTrackingConnector.cs:310-312`) and the custom-field reference
   resolution run per entry of the `fields` object, unchanged.

## Alternatives Considered

**Batch with no fallback, and report the whole item failed.** Simplest, and honest — the result contract
would correctly mark every field failed. **Rejected**: honest reporting of a self-inflicted regression is
still a regression. A customer with one stale mapping would silently lose every percentile on that item,
and the symptom ("nothing writes any more") points nowhere near the cause.

**Batch, and on failure parse the provider's error to identify the culprit, then re-send the remainder in
one corrected batch.** Two calls in the failure case instead of `1 + N`. **Rejected**: it makes correctness
depend on parsing `{"errors":{…}}` for Jira and `TF401326`-style prose for ADO, across API versions,
locales and two Jira deployment types — the one of which has never been probed. The unbatched retry gets
identical attribution from behaviour rather than from strings, and the failure case is rare enough that
`1 + N` calls is not worth defending.

**Move grouping above the port** — `WriteFieldsToWorkItems(connection, IReadOnlyList<WriteBackItemBatch>)`,
with the service grouping once. Superficially DRY: the `GroupBy` is written once instead of twice.
**Rejected**: it widens a five-implementation shared contract to encode a concern two implementations have
and three cannot express, and it pushes provider-specific batch limits (Jira's field-count ceiling, ADO's
existing `MaxChunkSize` chunking) up into a layer that has no business knowing them. The duplicated
knowledge is one `GroupBy` clause; the duplicated *decision* — "batch, fall back on failure" — is captured
here in the ADR and asserted per connector by test, which is where a cross-transport invariant belongs.

**Also fix Jira's missing throttling/concurrency while in the area.** ADO chunks and parallelises via
`Task.WhenAll` with `ExecuteWithThrottle` (`:320-325`); Jira is fully sequential with neither.
**Rejected — out of scope, and recorded as follow-up.** It is a real gap, but batching *reduces* Jira's
call count, so it moves the throttling need further away rather than closer.

## Consequences

**Positive**

- API calls per work item per pass drop from one-per-changed-field (≈6 with a full mapping set) to **1**.
- Issue-history churn drops with it — measured **4:1** on Jira changelog entries and `rev 1→2` against
  `rev 1→4` on ADO. That directly reduces the channels D1 correctly identified as unsuppressible per
  write but never counted.
- ADO notifies per revision, so the ADO cut is a genuine notification reduction as well — inferred from
  revision count, not inbox-verified.
- Today's per-field failure isolation is preserved exactly, so the change is behaviour-preserving on the
  failure path and behaviour-improving on the happy path.
- Permission-free and deployment-free: it grants nothing, asks for nothing, and is identical on Cloud and
  Data Center.

**Negative / accepted**

- The pathological case — a bad mapping on a connection that also cannot suppress — costs
  `2 + 2N` calls for an item with `N` fields, against `N` today. Both conditions are standing
  misconfigurations that the log and the slice-05 surface exist to surface, and the happy path is `1`.
- Two adapters now each own a small batch-and-degrade orchestration. The invariant is asserted per
  connector rather than shared, so a third write-back-capable connector must implement it rather than
  inherit it. Accepted deliberately (see the rejected alternative).
- The email reduction claim is gone. Slice 02's value story is API-call and history-churn reduction only.

## Earned Trust — the substrate lies, and the probe exercises the lie

| Substrate lie | Probe |
|---|---|
| "A partially-valid batch applies the valid parts" — the intuitive assumption, false on both providers | Gold test per connector: batch with one invalid field → provider rejects → unbatched retry issued → valid fields report `Success`, the invalid one alone reports failure |
| "Only validation errors are atomic" — false, ADO's `403` on an unwritable field is atomic too | Gold test: a batch failing on permission also falls back unbatched |
| "One item, one field behaves differently now" | Gold test: a single changed field issues exactly one call and one result — identical to today (AC-05.5) |
| "Grouping is free of ordering assumptions" | Gold test: field order within the payload does not affect the result set |

## Cross-reference

- [ADR-142](./adr-142-writeback-suppression-optimistic-retry.md) — the 403 degradation this composes
  with, and the status-based rule that keeps the two fallbacks from overlapping.
- [ADR-144](./adr-144-writeback-collection-seam.md) — the collection seam this batches against, so
  grouping happens once per flush rather than once per pass.
- Measured failure semantics, verbatim payloads and status codes:
  `docs/feature/quiet-jira-writeback/slices/slice-02-batch-writeback-fields-per-issue.md`
  → "Batched-write failure semantics — VERIFIED 2026-08-08".
