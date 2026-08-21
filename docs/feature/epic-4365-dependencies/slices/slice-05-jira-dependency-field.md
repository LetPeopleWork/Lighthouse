# Slice 05 — The field this Portfolio actually uses, on Jira (free)

**Feature**: epic-4365-dependencies · **ADO**: Epic #4365 · **Stories**: to be created · **Estimate**: ~6h

**Reference class**: the Azure DevOps override, end to end —
`AzureDevOpsWorkTrackingConnector.cs:628` (the fetch decision) + `:1089-1108` (`TheDependenciesOf`,
the extraction branch) + `DependencyFieldReferences.In` + `FetchFingerprint.cs:41,:85`. Slice 05 is
that path made to work on a second connector, and relocated so it works on the third without being
rewritten again.

## Goal

A Portfolio whose Features come from Jira and which names an Additional Field in
`DependencyOverrideAdditionalFieldDefinitionId` reads its dependencies from that field, exactly as an
Azure DevOps Portfolio already does. Native `is blocked by` links are ignored while the field is set,
per replace-not-union. The edges are stamped `DependencySource.PortfolioField`, so the detail view says
they came from the field the Portfolio named.

Second goal, and the reason this slice is worth more than its user-visible output: the override-vs-native
decision stops living inside a connector. It moves to one pure collaborator with an enforcement rule
attached, so connector #4 inherits it with an alarm rather than with a memory.

## IN scope

### US-09 — Jira dependencies from the Portfolio's own field

- A new pure `IDependencySourceSelector` / `DependencySourceSelector` under
  `Services/{Interfaces,Implementation}/Dependencies/`. Takes the Portfolio, the populated work item
  and the connector's native references; returns the effective references and the `DependencySource`
  that produced them. No I/O, no connector types in the signature. Sibling in shape and placement to
  `IDependencyHonourPolicy` / `DependencyHonourPolicy`.
- `JiraWorkTrackingConnector.TheIssuesItWaitsOn` (`:1082-1084`) gains the Portfolio and the populated
  work item, and delegates the branch to the selector. It currently takes only an `Issue`, which is
  precisely why it cannot branch today.
- `AzureDevOpsWorkTrackingConnector.TheDependenciesOf` (`:1089-1108`) delegates the same branch to the
  same selector. Behaviour must not move a single byte; the fetch decision at `:628` and the
  both-overrides early return (F-4) are not touched.
- The Jira renamed-link diagnostic (`dependency.jira.unknown_link_type`, `:1087-1104`) falls silent when
  the Portfolio has overridden the source. This is a defect fix, not a nicety — see below.
- `DependencySourceStampingTest` under `Lighthouse.Backend.Tests/Architecture/`: `DependencySource.PortfolioField`
  is constructed nowhere outside `Services/Implementation/Dependencies/`.

## OUT of scope

- **Linear.** `LinearWorkTrackingConnector.cs:46` returns `[]` from `GetPredefinedAdditionalFields` and
  the file never touches `AdditionalFieldValues`. There is no additional-field support to override from.
  Not a deferral — an incapacity.
- **Gating the selector in the settings form.** It renders on Linear Portfolios where it does nothing,
  and so do the parent override, the Feature owner and the size estimate, for the identical reason.
  Fixing one of four is worse than fixing none; the honest fix is capability gating for
  additional-field-backed settings generally, and it is a story of its own.
- **Reference normalisation.** A URL pasted into the field stays unresolved and is skipped alongside
  typos. Slice 04's own verdict already ruled on this and it has not changed: *"If a URL turns up in
  such a column, the normalisation step the hypothesis names is still owed, and is still a slice of its
  own rather than a bigger version of this one."*
- **ServiceNow and CSV.** Neither carries Features.
- **Any `fields=` or GraphQL request change.** None is needed — see the hypothesis.
- **Docs, `docs/concepts/`, `docs/portfolios/`, and the `lighthouse-clients` repo.**

## Learning hypothesis

**It disproves that the Jira request has to change.** The hypothesis under test is that the named
additional field is already in the response the Feature fetch returns, on both deployments, so the
override costs zero additional requests. The reading says it holds: Cloud full-detail sends
`&fields=*all` (`:36`, `:1616`), and Data Center full-detail (`:1518`) names no `fields=` parameter at
all, which is why it receives every field — the comment at `:1655-1659` says so in as many words, as
the reason the narrow identity sweep exists. The standing proof is that `FeatureOwnerAdditionalFieldDefinitionId`
(`:1073`) and `SizeEstimateAdditionalFieldDefinitionId` (`:1129`) already read Portfolio-scoped
additional fields off this exact path.

**If it fails**, the cost is bounded and visible at the first test: the field arrives empty, every
override Portfolio silently reports zero dependencies, and the slice grows a request-shape change on
two deployments plus a re-verification of the zero-additional-requests property. That is the whole
downside risk, and it is discovered in the first hour rather than in production, because the fixture
test asserts on extracted references and not on a mock.

It does **not** test whether anyone maintains a dependency column in Jira. Slice 04 already found that
nobody reachable does.

## Why this slice exists (and what it is NOT for)

Slice 04's brief said the setting *"serves ADO, Jira and Linear instances that record dependencies in a
custom field rather than the tracker's native link type."* It serves one. Its OUT-of-scope line explained
the gap away: *"the override is connector-agnostic by construction."* There is no such construction.
Each connector builds `FeatureDependencyReference` itself; there is no shared port for extraction, so
nothing could fall out of one.

The gap is not carelessness, and reading it that way would produce the wrong fix. D4 specified the
override once, in one connector's vocabulary — one named Azure DevOps file, one named line range, and a
cost argument ("skip the relations fetch entirely") that is true of Azure DevOps and of nothing else.
Slice 04 implemented exactly what D4 described, correctly. Every downstream artifact was individually
consistent with it: the Component Decomposition Jira row scoped Jira to `issuelinks`, the Reuse table
granted Jira "one field-list entry", and SA-9 spoke only of request cost. No artifact was wrong. The
rule was simply never assigned to anyone but Azure DevOps, and no reviewer could see that from any one
document.

So this slice is **not** "add the missing `if` to Jira". That is option (a), it is two hours cheaper,
and it schedules this same slice a third time for connector #4. It is for moving the decision to a place
where a fourth connector's omission has a failing test attached to it. The user-visible outcome —
Jira Portfolios honour the field — is the smaller half.

A live defect falls out of the same change and must not be shipped separately, and it is the inverse of
the one you would guess. The renamed-link warning at `:1087-1104` opens with an early return —
`if (features.Exists(feature => feature.DependsOnReferences.Count > 0)) { return; }` — so a Portfolio
whose override field yields entries silences it for free, because those entries land in that same
collection.

The uncovered state is the one an administrator passes through while setting the field up. An override
whose field is empty, or whose every entry resolves to nothing, leaves `DependsOnReferences` empty on
every Feature. The early return does not fire, the warning scans the native inward link names it never
consulted, and Lighthouse reports that none of them is called `is blocked by` — on a Portfolio that is
deliberately not reading links. An administrator following that warning would rename a Jira link type to
fix nothing, on the exact refresh where the real problem is a mistyped field value.

## Acceptance criteria

The slice is carried by the existing override criteria, re-asserted against Jira rather than rewritten:

- **AC-4.1 / AC-4.2 on Jira** — a Portfolio naming an Additional Field reads its dependencies from that
  field; native `is blocked by` links on the same Feature are ignored while it is set.
- **AC-1.4 on Jira** — an entry that resolves to nothing is skipped and the good entries beside it
  survive. A hand-maintained field contains typos; one typo must not discard three good keys.
- **The regression assertion, on both connectors** — with no override set, the extracted references and
  the stamped `DependencySource` are identical to today. On Azure DevOps this is the sharp one: the
  existing override and native tests must pass **unmodified**. A test that had to be edited to stay green
  means the delegation changed behaviour and the slice is not done.
- **Source stamping** — override edges are `DependencySource.PortfolioField` on Jira, matching Azure
  DevOps. The detail view already renders off this value.
- **Diagnostic silence** — no `dependency.jira.unknown_link_type` is emitted while the Portfolio has
  overridden the source, whatever the Feature's native links look like. The case that carries this one
  is an override that yields **no** resolvable entry on any Feature while native inward links are
  present under other names: today that is exactly the state the early return does not cover, and a
  scenario written against a populated field would pass without exercising the fix.
- **The enforcement probe** — `DependencySourceStampingTest` fails when `DependencySource.PortfolioField`
  is constructed outside `Services/Implementation/Dependencies/`. Assert the probe by breaking it once,
  deliberately, before trusting it.

## Dependencies

- Slice 03 (Jira and Linear native links) must be in — this slice branches around what it built.
- Slice 04 (the Portfolio setting, the migration, the settings form, `FetchFingerprint`) must be in. All
  of it is reused unchanged; nothing in the setting, its persistence or its fingerprint entry is edited
  here. `FetchFingerprint.cs:41,:85` already registers the field connector-independently, so a Jira
  Portfolio changing it already forces a full re-download today — do not add a second entry.
- No migration. No frontend change. No API contract change.

## Dogfood moment

The Jira demo instance already carries Features `LGHTHSDMO-7`..`-10` with real `Blocks` links, exercised
by `JiraDependencyDogfoodTest` (NUnit category `JiraIntegration`). Finding them needs
`issueLinkType is not EMPTY` in the JQL — querying the project's Features returns the fifty newest, all
auto-generated and all empty.

The moment to look for: point a Portfolio at those Features, name an Additional Field carrying
`LGHTHSDMO-8`, and watch the detail view swap from the native `is blocked by` edge to the field-sourced
one with no warning in the log. The absence of the warning is as much the dogfood as the edge is.

**Warning**: the Jira and Linear API keys are shared with CI. Hand-exploring the API can rate-limit the
next backend run, and only one of the six failures it causes will name the 429 — the other five look
like unrelated breakage. Prefer the fixture tests for the override path and leave the live test on
native links.

## Commit gate

Normal — the approval gate is Epic #5792's only.
