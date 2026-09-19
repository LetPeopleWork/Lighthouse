# Slice 01 — A link type name survives connection validation

**Feature**: parent-from-issue-links · **ADO**: pending · **Story**: US-01 · **Estimate**: ~3h
**Reference class**: `GetCustomFieldMappings` / `GetMissingAdditionalFields` — the lookup that resolves an
Additional Field's `Reference` against `rest/api/latest/field` already exists and already handles
"no match" without throwing. This slice adds a second place to look and one validation branch.

## Goal

An administrator can register a Jira issue link type as an Additional Field, and the connection validates.

## IN scope

- Link-type resolution: when an Additional Field's `Reference` matches nothing in `rest/api/latest/field`,
  ask `rest/api/latest/issueLinkType` and match against each type's `name`, `inward` and `outward`
  labels, case-insensitively.
- Field lookup keeps precedence. The link-type call happens only for references the field list did not
  resolve — so an instance with a real field of that name is untouched, and an instance with no unresolved
  references makes no extra call at all.
- `GetMissingAdditionalFields` (`:1058-1066`) stops reporting a resolved link type as missing, so
  `ValidateConnection` (`:408-416`) stops failing with `additional_fields_invalid`.
- The "nothing matched" failure lists the link type names the instance defines, so a mistyped name can be
  corrected from the error text. Same instinct as `InwardLinkNames` (`IssueExtensions.cs:87`).
- A distinct failure when the link-type endpoint itself refuses — unreachable, 401, 403. It must not
  present as "field not found", which would send an administrator to look at the wrong configuration.
- Unit coverage over both deployments' payload shapes plus the refusal paths.

## OUT of scope

- Reading a link to establish a parent. Nothing consults the registered type yet — that is slice 02.
- Any change to what `PopulateAdditionalFieldValues` stores. A registered link type still yields `""` as
  its field value after this slice, exactly as an unresolved reference does today (`:1595-1604`).
- Any other connector. Azure DevOps, ServiceNow, Linear and CSV keep their field-only resolution.

## Learning hypothesis

**Disproves, if it fails**: that a Jira issue link type can be named through the same path a field is
named through — which is the whole of D2, and therefore the whole of D1's "the admin still just picks a
Parent Override Field".

The concrete risks, in the order they are likely to bite:

1. `rest/api/latest/issueLinkType` needs a permission that `rest/api/latest/field` does not, so a
   credential a customer will actually grant can read fields but not link types.
2. Data Center and Cloud disagree on the payload, the path, or the API version — the same class of
   difference that already forced `GetIdForCustomFieldByProperty` to treat a missing `key` property as
   "no match here" rather than an error (`:1488-1490`).

**Confirms, if it succeeds**: the rest of the feature is a read of data already in hand (`*all` carries
`issuelinks` — `:41`, `:1721`), and slices 02–04 are arithmetic over it.

If (1) holds, the shape changes rather than the feature dying: the admin names the link type as free text
against `issuelinks` instead, which costs a form change and loses the "is that the right spelling"
answer this slice delivers. Worth knowing on day one, not after slice 02 is built on it.

## Acceptance criteria

AC-1.1 through AC-1.5 in `feature-delta.md`.

## Dependencies

P1 — the endpoint's reachability. This slice *is* the test of P1; nothing else waits on anything.

## Pre-slice SPIKE

**Yes, timeboxed to 30 minutes, before any code.** Call `rest/api/latest/issueLinkType` against a Jira
Data Center instance and a Jira Cloud instance with the same class of credential a customer grants, and
record both payloads verbatim in this brief. Two cheap outcomes: either the response shapes go straight
into the parser, or the permission problem surfaces before anything is built on the assumption.

Record the answer here before writing the first test.

### Spike result — 2026-09-19, Jira Cloud (`letpeoplework.atlassian.net`)

**P1 is confirmed. `rest/api/latest/issueLinkType` is reachable, and the feature is not re-shaped.**

Three credential classes were tried against the endpoint, each alongside `rest/api/latest/field` as the
control. All three read the link types:

| Credential | Route | `issueLinkType` | `field` |
|---|---|---|---|
| Admin API token (`atlassian.pushchair@…`) | `https://letpeoplework.atlassian.net` direct | 200, 5 types | 200, 26178 B |
| Restricted identity (`benjamin@letpeople.work`) | direct | 200, **same 5 types** | 200, 26178 B |
| Scoped token | `api.atlassian.com/ex/jira/<cloudId>` gateway | 200, same 5 types | 200, 26178 B |

Risk 1 — "the link-type endpoint needs a permission the field endpoint does not" — **did not hold**. The
restricted identity, which lacks Delete Issues and is confined to `SPIKEPRM`, reads the full link-type
list. Risk 2, a payload disagreement, does not arise on Cloud: `latest` resolves to v2 (every `self` is
`rest/api/2/issueLinkType/<id>`), and the gateway route returns a byte-identical body apart from the
`self` host.

Payload, verbatim, from the direct route:

```json
{
  "issueLinkTypes": [
    { "id": "10000", "name": "Blocks", "inward": "is blocked by", "outward": "blocks",
      "self": "https://letpeoplework.atlassian.net/rest/api/2/issueLinkType/10000" },
    { "id": "10001", "name": "Cloners", "inward": "is cloned by", "outward": "clones",
      "self": "https://letpeoplework.atlassian.net/rest/api/2/issueLinkType/10001" },
    { "id": "10002", "name": "Duplicate", "inward": "is duplicated by", "outward": "duplicates",
      "self": "https://letpeoplework.atlassian.net/rest/api/2/issueLinkType/10002" },
    { "id": "10006", "name": "Polaris work item link", "inward": "is implemented by", "outward": "implements",
      "self": "https://letpeoplework.atlassian.net/rest/api/2/issueLinkType/10006" },
    { "id": "10003", "name": "Relates", "inward": "relates to", "outward": "relates to",
      "self": "https://letpeoplework.atlassian.net/rest/api/2/issueLinkType/10003" }
  ]
}
```

Three things the parser has to take from that shape:

1. The array is under the `issueLinkTypes` key. It is not a bare array, and it is not the `values` +
   `isLast` page envelope the field-and-search endpoints use, so there is no paging to carry.
2. `name`, `inward` and `outward` are all present on every entry, which is what the three-way
   case-insensitive match assumes.
3. `Relates` has `inward` equal to `outward` ("relates to"). A match on a label can therefore hit the
   same type through two routes, and the matcher must yield one type, not two — otherwise slice 03's
   ambiguity refusal fires on a single unambiguous type.

**New finding, not anticipated by the brief: a credential Jira does not accept is answered anonymously,
not refused.** With a bogus token, or with no `Authorization` header at all, `issueLinkType` returns
`200` and `{"issueLinkTypes":[]}`, and `field` returns a reduced 5964-byte list rather than an error.
The planned failure taxonomy above — "unreachable, 401, 403" — does not describe this. The empty list
is the failure.

This is bounded, and it does not change the slice: `ValidateConnection` calls `rest/api/2/myself`
first (`:389`), and `myself` *does* answer `401` to both the bogus and the anonymous credential, so a
bad credential is turned away before any link-type lookup happens. The consequence is confined to the
error text this slice owns. When the list comes back empty, "no link type named X — this instance
defines: <nothing>" reads as though the administrator invented a link type, when the likelier cause is
that nothing authenticated. An empty list deserves its own sentence, distinct from "nothing matched".

**Not covered: Data Center.** No DC instance is available, per the Changed Assumptions below. Every
number here is Cloud. The DC payload shape stays unverified until P3 lands, and slice 02's AC-2.6 is
where that bites.

**Dogfood correction.** This instance has no "Caused by"/"Results in" pair — Steve's customer's scheme
is not replicated here. The same-day demo has to register one of the five types above; `Blocks` is the
closest analogue, being a directional pair with distinct inward and outward labels.

## Effort

~3h including the spike. One resolution path, one validation branch, one error message.

## Dogfood moment

Same day: register `Caused by` on the replicated Data Center connection and press Validate. Green is the
whole demo.

## Changed Assumptions

**Original (this brief, Pre-slice SPIKE, 2026-09-18):** "Call `rest/api/latest/issueLinkType` against a
Jira Data Center instance **and** a Jira Cloud instance with the same class of credential a customer
grants, and record both payloads verbatim in this brief."

**New (user, 2026-09-18, DESIGN wave):** only a Jira Cloud instance is available. The spike runs against
Cloud only.

**Consequence, stated rather than absorbed:** AC-1.4 requires verification on Data Center *and* Cloud.
Only half of it can close. Data Center is where the reported configuration lives, so the unverified half
is the half that matters — and `GetIdForCustomFieldByProperty` already carries a comment
(`JiraWorkTrackingConnector.cs:1488-1490`) recording a real Data Center/Cloud divergence in the
neighbouring field-list endpoint, which is precisely the risk class here.

This slice therefore closes with **P1 UNVERIFIED ON DATA CENTER**, recorded in the brief and not
silently downgraded. Data Center coverage comes from fixtures shaped to the documented payload, which
proves the parser and not the endpoint. A verification pass on a real Data Center instance remains owed
before the reported customer's case can be called answered.

## SPIKE RESULT — 2026-09-18, Jira Cloud (letpeoplework.atlassian.net)

Run before any code, as the brief requires. **The hypothesis is not cleanly confirmed, and the reason is
more interesting than a pass would have been.**

### What was observed

| Call | Auth | Result |
|---|---|---|
| `GET /rest/api/latest/issueLinkType` | none | **HTTP 200**, body `{"issueLinkTypes":[]}` |
| `GET /rest/api/3/issueLinkType` | none | HTTP 200, same empty body |
| `GET /rest/api/2/issueLinkType` | none | HTTP 200, same empty body |
| `GET /rest/api/latest/field` | none | **HTTP 200**, full field list returned |
| `GET /rest/api/latest/myself` | none | HTTP 401, "Client must be authenticated" |

Both environment credentials (`JiraLighthouseIntegrationTestToken`,
`JiraScopedTokenIntegrationTestToken`, 192 chars each) returned **401 on `/myself` under Basic auth** and
403 "Failed to parse Connect Session Auth Token" under Bearer. They are expired or are a token class this
call shape does not accept, so every result above is an *anonymous* result. The authenticated shape of
the payload is still unknown.

### The finding that changes the design

**The link-type endpoint does not refuse when it cannot answer — it returns 200 with an empty list.**

That is the failure mode AC-1.4 was written against, and AC-1.4 as drafted assumes the wrong one: it
says an unreachable or forbidden endpoint must fail "with a message naming the endpoint and the
permission, not a generic failure". There is nothing to catch. An unauthenticated caller and an instance
with no link types configured are the same 200 and the same empty array.

**And the obvious safeguard does not work.** The natural fix — "only trust an empty link-type list when
the field lookup on the same client succeeded" — fails, because `/field` *also* answers anonymously with
200 and a populated list. Two successful calls prove nothing about authentication on this instance.

### What this does to the design

1. **AC-1.4 needs rewriting.** "Fails with a message naming the endpoint and the permission" is not
   reachable from the status code. The criterion has to become: an empty link-type list must never be
   reported to the administrator as "this instance has no link types" unless authentication has been
   positively established by a call that actually requires it.
2. **`ValidateConnection` is probably already safe, and by luck rather than design.** It runs an
   authentication check before `GetMissingAdditionalFields` (`JiraWorkTrackingConnector.cs:400-416`), so
   on the validation path an empty list is trustworthy. That ordering is now load-bearing for a reason
   nobody wrote down, and needs a test that fails if the two are ever reordered.
3. **The fetch path has no such pre-check, and that is the real exposure.** `GetCustomFieldReferences`
   runs during every refresh with no authentication probe. A token that expires mid-life would make
   link-type resolution return empty, references stop resolving, and parents silently stop being set —
   on an instance where the whole hierarchy depends on them. Silent degradation, exactly the class of
   failure this feature exists to remove.
4. **Data Center remains unverified**, as already recorded in Changed Assumptions. Nothing here speaks to
   it, and Data Center is where the reported configuration lives.

### Verdict

**P1 is NOT closed.** The endpoint exists and answers on Cloud at all three API versions, which is the
half that passed. What failed is the assumption that it refuses when it cannot answer — it does not, and
neither does the field endpoint beside it.

**Before slice 01 is written**, two things are owed: a working Jira Cloud credential, so the
authenticated payload shape can be recorded here rather than inferred; and a decision on whether the
fetch path gains an authentication probe, which is a DESIGN question this slice is too small to hold.
