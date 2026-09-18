# ADR-193: Parent Source Is a Declared Connector Capability, Selected Outside the Connector, and Refuses Ambiguity Rather Than Guessing

**Status**: Accepted (2026-09-18 — Morgan, DESIGN wave, interaction mode PROPOSE). No code implements it yet; `parent-from-issue-links` slice 01 (ADO Story #6029) is the first commit that will.

**Feature**: `parent-from-issue-links` — ADO Epic #6028, "Parentage from Jira Issue Links"

**Decider**: Morgan (Solution Architect); the three product-level inputs (one setting, no persisted kind, warning surface) by user decision 2026-09-18

**Relationship to prior ADRs**: applies ADR-071's connector-capability port pattern to a second capability. Sits beside ADR-157, which chose where a Portfolio's dependency references are stored and read from; this one answers the adjacent question for a parent, and deliberately does not share its type.

## Context

A Jira instance reported through the community on 2026-09-18 records its hierarchy as **issue links** — "caused by", "results in" — rather than as the tracker's native parent field or as a custom field whose value is an issue key. Lighthouse reads only the latter two. On that instance every Feature carries the no-children warning and is sized by the Portfolio default, so every forecast restates one number somebody typed into settings.

Three facts from the code set the shape of the answer.

**The existing override fails silently rather than loudly.** `ParentOverrideAdditionalFieldDefinitionId` (`IWorkItemQueryOwner:27`) names an Additional Field whose *value* is taken as the parent key (`JiraWorkTrackingConnector.cs:1570`). Pointed at `issuelinks`, `GetFieldValue`'s array branch hands each link object to `GetObjectDisplayValue`, which finds neither `value` nor `name` on it and returns raw JSON (`IssueExtensions.cs:22-47`). The work item gets a JSON blob as its parent key, matches nothing, and is indistinguishable from an instance with no hierarchy at all.

**An unresolved reference is already a non-throwing miss.** `GetCustomFieldMappings` always adds an entry, empty when nothing matched (`:1480`), and `GetMissingAdditionalFields` turns those into `additional_fields_invalid` at validation (`:1058-1066`, `:408-416`). So there is already a hook where a reference that is not a field can be given a second meaning, and already a place where it is refused.

**A parent is 0..1, and that is the whole difference from dependencies.** ADR-157 and epic-4365's D15 chose split-and-skip for a 0..n dependency list: a bad entry is dropped beside the good ones. A parent has no "beside".

## Decision

### 1. A connector declares whether it can read a link type — it is not inferred from the outside

`IWorkTrackingConnector` gains one member:

```csharp
// The link types this connection can resolve a parent from. Jira answers with the instance's own
// issue link type names; a tracker with no such concept answers with nothing, and a reference naming
// one of its link types is then simply a reference that matched nothing.
Task<IReadOnlyList<string>> GetParentLinkTypeNames(WorkTrackingSystemConnection connection);
```

Jira implements it against `rest/api/latest/issueLinkType`. Azure DevOps, ServiceNow, Linear and CSV inherit an empty default and have **no code written for this feature at all**.

No `CancellationToken`: the method does not loop pages, and the interface's own header records why a token it could not honour would be worse than none (ADR-183).

Per-*connection* rather than per-connector, matching `SupportsIncrementalSync` — Jira Cloud and Jira Data Center are one class that need not answer identically, which is exactly the unknown this feature carries.

This is ADR-071's pattern, second application. That ADR added `GetPredefinedAdditionalFields` and stated the reason plainly: a future connector contributes by returning a non-empty set, with zero change to any consuming site. The alternative considered and rejected was to leave the other four connectors refusing link-type references *by accident* — they resolve references against their own field lists, so the refusal already happens — and guard it with tests. An accident guarded by tests is still an accident; the next person to touch field resolution has to know the tests exist and why.

### 2. Which source a parent comes from is decided outside every connector

A new `ParentSourceSelector` — static, tracker-neutral — answers "does this owner's Parent Override name a field, or a link type?". The Jira-specific walking of `issuelinks` stays in `IssueExtensions`, beside `ExtractDependencyReferences`, because the payload shape is Jira's.

`DependencySourceSelector` established this split and its header records the cost of getting it wrong: a rule written inside one tracker "was told to one of three, and the other two accepted the setting and ignored it, which reads from the outside exactly like a field everyone left empty."

`ParentSourceSelector` is a **new type rather than a generalisation of `DependencySourceSelector`**, and this is the one new type the feature introduces. The two are structurally near-identical and encode different knowledge: dependencies are 0..n, Portfolio-only, skip-the-bad-entry; a parent is 0..1, on both Team and Portfolio, refuse-the-ambiguous. Merging them produces a selector whose every method carries a cardinality flag, and they diverge again at the first change to either.

### 3. Direction is inferred, never configured

For the named link type, the parent is the counterpart issue at the *other* end of a matching link — the `outwardIssue` where this item holds the inward end, the `inwardIssue` where it holds the outward end. An instance where the work item names its Feature and one where the Feature names its work items are the same instance to this code.

This rests on Jira serving each link from both ends, which `IssueExtensions.cs:53-57` states as the reason the dependency reader deliberately takes one end only. **That is evidence and not proof** — the comment was written about a different reader for a different purpose — so the feature's slice 02 opens with a spike that settles it before any code is written. If it fails, the reverse direction needs a child-to-parent index and that slice roughly doubles. The decision does not change either way; its cost does.

### 4. Two candidates is a refusal, not a partial answer

| Matching links on the item | Result |
|---|---|
| 0 | No parent. Identical to an empty override today. No warning — an item outside the hierarchy is not a fault. |
| 1 distinct counterpart key | That key is the parent. |
| 2+ distinct counterpart keys | **No parent**, plus a warning naming the item and every candidate. |

Duplicate links to the same key collapse to one.

Resolution therefore returns a **three-case result** rather than a nullable string. A `string?` expresses two of the three, so the ambiguous case would have to be recovered by re-walking the links at the call site — two places deciding one thing.

No tie-break rule exists, and none is a near miss. A wrongly chosen parent moves a work item into a Feature it does not belong to, corrupts that Feature's size and every forecast drawn from it, and looks exactly like correct data. A refusal is visible; a guess is not.

The assumption underneath — that a hierarchy link type is specific enough that an item carries at most one — is an assumption about how people use link types, not about the API. It is measured against a **pre-registered gate: 5% of fetched items ambiguous sends the feature back to DESIGN** for a narrowing rule, most likely on the counterpart's work item type.

### 5. Nothing is persisted about what a reference turned out to be

`AdditionalFieldDefinition` is unchanged — no `Kind` column, no migration across the four providers. The field list is consulted first and the link-type list second, once per connection per refresh, on the lifetime `FieldNames` already has.

The alternative — persisting the resolved kind at validation time — buys one avoided lookup per refresh and costs a stored answer that lies the moment a Jira administrator renames a link type, until somebody re-validates.

### 6. Ambiguity surfaces as a count on a page and detail in one log line

`RefreshLog` gains one additive column, `AmbiguousParentCount`, appended below `Cancelled` per that file's stated append-only rule and generated with the `CreateMigration` script. The Team page renders it when non-zero, in the instance's configurable term, and is silent at zero.

The detail — which items, which candidates — goes in **one aggregated warning per refresh**, the shape `ReportLinksThatMeantNothingHere` already uses. Not one line per item: Epic #5687 measured the update path down to a budget of two Information lines per entity, and a per-item warning would undo that on exactly the instances this feature is for.

## Alternatives Considered

**A separate "parent source" setting with an explicit direction picker.** Rejected by user decision: it makes a rare configuration cost every administrator a more complicated form, and asks for a fact — which end holds the parent — that Lighthouse can work out for itself (§3). The chosen shape adds no control at all; the administrator picks an Additional Field in the dropdown they already use.

**Extending the existing override to accept a raw `issuelinks` reference.** Rejected: `issuelinks` is one field carrying every link type, so pointing at it says nothing about *which* link is the hierarchy. The reference has to name the link type to mean anything.

**Teaching Azure DevOps the same capability in this feature.** Deferred, not rejected. ADO has the equivalent shape (`GetParentReference`, `:1241`) and is the obvious second. Nobody has asked, and it doubles the connector and acceptance surface for a hypothetical. §1's port method is what makes it cheap later.

**Replacing the Parent Override Field with a unified parent-source setting, with a data migration.** Rejected: the cleanest end state, and it changes the meaning of a shipped, documented setting for every existing instance to serve a configuration most of them do not have.

## Consequences

**Positive**
- Four connectors gain correct behaviour with no code written for them, and a fifth would too.
- No schema change on a user-facing configuration entity; one additive expand-only column on a log table.
- No additional Jira request on the fetch path — `*all` already carries `issuelinks` — and one extra call at most per connection per refresh, only when a reference did not resolve as a field.
- Existing instances cannot change behaviour: field lookup keeps precedence on a name collision.

**Negative, stated honestly**
- One more member on `IWorkTrackingConnector`, an interface that is already long. Defaulted, so no connector is forced to answer.
- A second selector that reads almost identically to the first. Argued in §2, and it will look like duplication to anyone who meets it without the argument — which is why the argument is here rather than in a code comment.
- The Data Center half of the link-type endpoint is **unverified**: only a Jira Cloud instance is available (user, 2026-09-18). Data Center is where the reported configuration lives, so the unverified half is the one that matters, and slice 01's brief records it as owed rather than closed.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Capability is declared, never inferred | NUnit: `GetParentLinkTypeNames` returns the instance's link types for Jira and empty for ADO / ServiceNow / Linear / CSV; the selector reads the port, not a connector type check |
| Source selection stays tracker-neutral | `ParentSourceSelector` has no `using` of any connector namespace; ArchUnitNET rule, same posture as `DependencySourceSelector` |
| Ambiguity never yields a parent | NUnit: an item with two distinct counterpart keys has `ParentReferenceId` empty and contributes exactly one to `AmbiguousParentCount` |
| Both link directions resolve | NUnit over both wire shapes, and the slice 02 spike against a live instance before the code is written |
| No extra fetch request | NUnit: request count against the pre-change baseline is unchanged |
| Existing instances unaffected | NUnit: a real field whose name equals a link type name still resolves as a field |
| Log budget respected | Logger-capturing test: one warning per refresh regardless of how many items were ambiguous |

## Cross-feature impact

- **ADR-071**: second application of its connector-capability port pattern. Nothing in ADR-071 changes.
- **ADR-157 / epic-4365**: reads the same `issuelinks` payload through the same helpers and changes nothing that feature owns. Its `BlockedByLinkName` const has the same weakness this feature fixes for parents, and the same customer will likely hit it next — recorded as out of scope here so it is not rediscovered.
- **Lighthouse-Clients (CLI + MCP)**: the refresh-log payload gains one scalar. Version-gating is assessed at feature finalization against the real payload, not assumed here.
