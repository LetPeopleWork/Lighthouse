# Slice 02 - Batch write-back fields per issue

**Type:** slice | **Est:** ~0.5-1 day | **Stories:** US-05 | **Connectors:** Jira + ADO

## Why this exists

**Both write-back-capable connectors issue one API call per field, not per issue.**

Jira - `JiraWorkTrackingConnector.cs:307-325`, `UpdateItem` serializes exactly one field and PUTs it:

```csharp
var payload = JsonSerializer.Serialize(new
{
    fields = new Dictionary<string, object> { [fieldReference] = fieldValue }
});
var response = await client.PutAsync($"rest/api/latest/issue/{update.WorkItemId}", content);
```

ADO - `AzureDevOpsWorkTrackingConnector.cs:345-353`, `UpdateItems` builds a `JsonPatchDocument` holding
exactly one `JsonPatchOperation`, then calls `UpdateWorkItemAsync` once.

`WriteBackFieldUpdate` is one field on one item, and both connectors loop over the flat list. So a feature
with four percentile mappings + FeatureSize + WorkItemAge = **6 separate API calls to the same issue in
one pass**.

Jira's `fields` object accepts many fields per PUT. ADO's `JsonPatchDocument` accepts many operations per
call. Grouping by issue collapses 6 calls into 1.

## Value story

> **REVISED 2026-08-08 after SPIKE-03 Q9.** The original claim - six mappings, six emails, one email after -
> is **void**. Jira Cloud batches notifications per (recipient, issue) over a ~10 min window, so 1 PUT and 4
> PUTs to the same issue both produced exactly **one** email. Batching already collapses per-issue mail; this
> slice does not change it. Do not state an email reduction in docs or release notes.

**Before:** every mapped field that changes costs its own API call and its own issue-history entry - six
mappings, six calls, six changelog entries on the same issue, same minute.
**After:** one write per issue per cycle - one call, one changelog entry (**measured 4:1** in SPIKE-03 Q9).
**Decision enabled:** the admin keeps write-back on even with a rich mapping set, instead of trimming
mappings to keep the issue history and API budget survivable.

## Why this is the epic's most robust lever

- **Permission-free.** Needs no `Administer Jira`, no "Make bulk changes", no `notifyUsers`, no bulk API.
- **Deployment-free.** Identical on Jira Cloud and DC. No D4 discriminator involved.
- **Survived the SPIKE.** SPIKE-03 found something worse than D7's dangerous branch - Cloud **403s** and
  drops the whole write for under-permissioned credentials - and this slice is unaffected: it grants
  nothing and asks for nothing.
- **Attacks what D1 wrote off.** D1 correctly locks that issue history, `Updated` churn and webhook firings
  are unsuppressible *per write*. Nobody examined the *count* of writes. This does not suppress history -
  it divides it by ~6. Same for `Updated` churn, webhooks, listeners and automation rules.

## Acceptance criteria

- AC-05.1: Given an issue with multiple changed mapped fields in one cycle, when write-back runs, then
  **one** Jira PUT is issued carrying all changed fields in a single `fields` object.
- AC-05.2: Given the same on ADO, then one `UpdateWorkItemAsync` call is issued with a `JsonPatchDocument`
  carrying one operation per changed field.
- AC-05.3: Given a batched write partially fails, when results are assembled, then each field still yields
  its own `WriteBackItemResult` with correct `TargetFieldReference` and error message - the per-field
  result contract is unchanged for callers.
- AC-05.4: Given a batched write fails wholesale, then **every** field in that batch is marked failed with
  the error - never a silent partial success.
- AC-05.5: Given an issue with exactly one changed field, then behaviour is identical to today (one call,
  one result).
- AC-05.6: Given ADO write-back, then `suppressNotifications: true` is still passed on the batched call.
- AC-05.7: Given a field value that parses as numeric, then the existing numeric-vs-string coercion
  (`JiraWorkTrackingConnector.cs:310-312`) is preserved per field within the batch.

## IN scope

- Group `WriteBackFieldUpdate` by `WorkItemId` and emit one call per issue, in both connectors.
- Preserve `WriteBackResult.ItemResults` per-field granularity across the batched call.
- Fix the O(updates x items) scan in `WriteBackService.GetChangedFields:96` while in there - it currently
  calls `featureRepository.GetAll()` + `workItemRepository.GetAll()` and then rescans the full list per
  update (`allItems.Where(x => x.ReferenceId == update.WorkItemId)` inside the foreach). Grouping makes a
  dictionary lookup the natural shape.

## OUT of scope

- **Throttling / concurrency parity.** ADO chunks and parallelises with a throttle
  (`AzureDevOpsWorkTrackingConnector.cs:320-325`, `ExecuteWithThrottle`); Jira is fully sequential with no
  throttle. Real gap, but a separate concern - do not smuggle it in here. Log it as follow-up.
- Jira-specific suppression (slices 03-06).
- Linear and CSV (D8).

## Dependencies

- **Slice 01** - lands on the collection seam so grouping happens once, across the whole cycle, rather than
  per-pass. Batching before the seam exists would group within each of the N+2 passes and have to be
  reworked.

## Verification note - RESOLVED

SPIKE-03 Q9 ran on 2026-08-08 against Jira Cloud. Outcome: **the email claim collapses, the API-call /
history / churn reduction survives** - exactly the branch this note anticipated, for an unanticipated
reason (notification batching, not per-field notification).

| | 1 PUT, 4 fields | 4 PUTs, 1 field each |
|---|---|---|
| changelog entries | **1** | **4** |
| emails to the watcher | **1** | **1** |

The slice still ships on its remaining wins. The email claim stays out of docs and release notes; it may
hold for a customer with notification batching disabled, but that is unverified.

## Taste tests

- Value-bearing: yes - ~6x fewer API calls and ~6x fewer issue-history entries, permission-free, both
  deployments. (Was "~6x fewer emails" - retired by SPIKE-03 Q9.) PASS.
- Right-sized: grouping logic in two connectors + one service. PASS.
- Disproves a pre-commitment: yes - D1's implicit assumption that write count was fixed. PASS.
- New abstraction required? No - a group-by on an existing list. PASS.
