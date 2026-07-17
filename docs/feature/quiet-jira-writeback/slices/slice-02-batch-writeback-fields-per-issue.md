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

**Before:** every mapped field that changes emails every watcher separately - six mappings, six emails on
the same issue, same minute.
**After:** one write per issue per cycle - one email.
**Decision enabled:** the admin keeps write-back on even with a rich mapping set, instead of trimming
mappings to keep the inbox survivable.

## Why this is the epic's most robust lever

- **Permission-free.** Needs no `Administer Jira`, no "Make bulk changes", no `notifyUsers`, no bulk API.
- **Deployment-free.** Identical on Jira Cloud and DC. No D4 discriminator involved.
- **Survives a bad SPIKE.** If SPIKE-03 finds Cloud silently ignores `notifyUsers` on under-permissioned
  credentials (D7's dangerous branch), this slice still delivers its cut with nothing granted.
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

## Verification note

"Multiple fields in one PUT produces one watcher email" is near-certain but currently **assumed**. SPIKE-03
Q9 verifies it against a real instance. If it turns out false - if Jira emits one notification per changed
field regardless of call count - this slice's email claim collapses and only the API-call / history /
churn reduction survives. Not a blocker (those wins alone justify it), but the value story would need
rewording, so the claim is not stated in docs or release notes until Q9 reports.

## Taste tests

- Value-bearing: yes - ~6x fewer emails, permission-free, both deployments. PASS.
- Right-sized: grouping logic in two connectors + one service. PASS.
- Disproves a pre-commitment: yes - D1's implicit assumption that write count was fixed. PASS.
- New abstraction required? No - a group-by on an existing list. PASS.
