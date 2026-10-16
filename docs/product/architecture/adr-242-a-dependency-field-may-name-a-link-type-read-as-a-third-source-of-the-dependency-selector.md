# ADR-242: A Dependency Field may name a link type, read as a third source of the dependency selector; ADO's relation-type lookup stays inside its adapter

**Status**: Accepted (maintainer, 2026-10-16, DESIGN pass 2). Proposed by Morgan in pass 1; the maintainer confirmed
D2 for Azure DevOps — a non-directional relation type or a base name without its end reads nothing and warns — and
kept Story #6312 in scope.
**Feature**: `epic-6303-configure-once-per-connection` (Stories #6300 slices 01–02, #6312 slices 03–04)
**Relationship to prior ADRs**: applies ADR-193 §2–§3 and §5 (selection outside the tracker, direction from the
named end, nothing persisted about what a reference turned out to be) to dependencies; does **not** revive ADR-193 §1's
port, which DELIVER retired for want of a caller. Keeps ADR-157 (references stored on the Feature).

## Context

A Portfolio's Dependency Field replaces the tracker's default dependency link (epic-4365 D15). On Jira a reference
naming a link type resolves (Epic 6028) but stores an empty value (`JiraWorkTrackingConnector.cs:1733`), so a
link-type Dependency Field reads zero dependencies, and the "no link matched" warning is off once a field is set
(`:1282`). On Azure DevOps references resolve against the field list only (`AzureDevOpsWorkTrackingConnector.cs:690-736`)
and the relations batch is skipped when a Parent Override and a Dependency Field are both set (`:1142`). The maintainer
decided: any Additional Field, field first, then link type per tracker; one-way only; one link type replacing the
default; no fallback.

`DependencySource` is persisted per reference and a closed zod enum on the frontend (`FeatureDependency.ts:38`).

## Decision

1. **`DependencySourceSelector` gains a third source**, "a link type the Portfolio named". The connector supplies the
   counterparts it read on the named end; the selector decides, as it does today for the tracker's own link and for a
   field. Stored references say `DependencySource.TrackerLink` — the link is the tracker's — so no enum member is added.
2. **Jira**: the named end is the one the typed phrase reads as (`JiraIssueLinkType.DirectionReadFrom`); a Feature waits
   on the counterpart of each of its links of that type held at that end, reusing the walker `ResolveParentFromLinks`
   uses. A type name or a phrase both ends share reads nothing and raises a finding naming both phrases. No new request.
3. **Azure DevOps**: a relation-type listing (`GET _apis/wit/workitemrelationtypes`; the SDK's `GetRelationTypesAsync`
   is the candidate) is asked only for references that missed the field list, at most once per connection per Validate
   and per refresh. A reference resolves to a relation type when it equals a listed work-item-link reference name,
   case-insensitively; directional types are listed end by end (`…-Forward`, `…-Reverse`). A non-directional type
   (e.g. `System.LinkTypes.Related`) or a base name without its end resolves to "names no end": nothing read, a finding
   naming the ends. Display names are never matched. The resolution is connector-local, never persisted.
   **Where the listing is remembered.** Connector instances are cached per tracker for two minutes and shared by every
   connection on that tracker (`WorkTrackingConnectorFactory.cs:15`, `:39`), so the listing is **never** stored on the
   connector. It is a value created at the start of one Validate call or one owner's fetch, for that call's connection,
   and passed down that call's chain as a parameter; it ends with the call. A second connection, a second owner's
   refresh or the next refresh asks again. (Recommended option, taken while the maintainer was AFK.)
4. **The lookup is not on `IWorkTrackingConnector`.** Its only caller is the ADO adapter's own reference resolution.
5. **Relations are read when the Dependency Field names a relation type**, whatever the Parent Override says (the
   `:1142` condition reads the resolution, not only whether a field is set). `ExtractDependencyReferences` takes the
   `Rel` to match; the default stays `System.LinkTypes.Dependency-Reverse`.
6. **A relation type named by any other setting** (Parent Override, Size Estimate, Feature Owner, Estimation) reads as if
   nothing were named and raises a finding for the owner (AC-3.10).
7. **`DependencySourceSelector` stays in `Services/Implementation/Dependencies`.** It is a pure rule, like the
   effective-value rule, but the two sit apart for a reason: the effective-value rule is called by `Models` entities
   (the owner's own properties), and `Models` may not depend on `Services`, so it must live in `Models`. The selector is
   called only by connectors, beside its sibling `ParentSourceSelector`; moving the pair is a refactor this feature
   does not need. Its purity is held by the existing "no connector `using`" posture and by unit tests without fakes.

## Alternatives considered

- **A new `DependencySource` member.** Truer to the code's internals; fails every Dependencies read in a browser that
  predates it and needs an append-only enum change, for a distinction no reader shows.
- **ADR-193 §1's port on `IWorkTrackingConnector`.** A member every connector implements for one caller; DELIVER
  retired it for the parent case for exactly that reason.
- **Parsing `-Forward` / `-Reverse` off the typed reference.** Duplicates what the listing already says, and cannot tell
  a typo from a custom type.
- **Remembering the listing on the connector instance**, keyed by connection: it would outlive the refresh by up to the
  factory's two-minute cache and miss relation types added in that window. **A shared memory cache with expiry**: a
  new moving part for one cheap request per refresh.
- **Matching display names on ADO.** Localised and renameable — the reason the default keys on the reference name.

## Consequences

**Positive**
- The silent-zero trap on Jira and the relations-skip trap on ADO close where the reading happens.
- Nothing changes for a Portfolio that names nothing or names an ordinary field (field lookup first).

**Negative / accepted**
- One more Azure DevOps request, only when a reference missed the field list. Whether a `vso.work` PAT may list relation
  types is slice 03's hypothesis; an empty or refused listing must fail Validate saying what happened, never resolve
  every reference to nothing.
- US-03's `System.LinkTypes.Related-Forward` example most likely does not exist; Related reads nothing by rule 3.
  Slice 04's test vehicle is a directional type that slice 03's spike records as present on the dev organisation.

## Enforcement

- Wire-fixture units (`TrackerWireFormats`): both ends, rename, case, both-ways refusal, field-valued and empty
  Dependency Field unchanged — Jira and ADO, plus a relation-type listing fixture for Services and Server 2022.
- Request-count tests: Jira unchanged; ADO ≤ 1 listing per connection per Validate and per refresh, and no request per
  work item; relations read with a Parent Override set.
- Acceptance (one cached connector instance, as in production): refresh two Portfolios on **two** ADO connections
  back to back → exactly one listing request per connection, each against its own organisation; refresh the first
  again → one new request (nothing carried over). A structural check: the ADO connector holds no field of the
  listing's type.
- `DependencySourceSelector` keeps no connector `using` (existing posture).

## Cross-reference

- [ADR-193](./adr-193-parent-source-is-a-declared-connector-capability.md) (§2, §3, §5 applied; §1 not).
- [ADR-157](./adr-157-dependency-references-stored-on-the-feature.md).
- [ADR-243](./adr-243-refresh-findings-ride-on-the-records-and-land-on-the-refresh-log-row.md) (where the findings go).
