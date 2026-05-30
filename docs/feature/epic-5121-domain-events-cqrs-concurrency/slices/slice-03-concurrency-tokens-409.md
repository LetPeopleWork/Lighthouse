# Slice 03: Optimistic-concurrency tokens (HTTP 409) on config aggregates

**Feature**: epic-5121-domain-events-cqrs-concurrency
**ADO child**: #5100
**Story shipped**: US-5100 — **USER-VISIBLE**
**Job-id**: `job-config-edit-no-silent-lost-update`
**Persona**: `config-admin`
**Estimate**: ~2 crafter days
**ADR**: concurrency section

## Goal

Close the silent lost-update gap ADR-027 names: two administrators editing the same config aggregate today produce a silent last-writer-wins overwrite. Add optimistic-concurrency tokens (Postgres `xmin` / SQLite rowversion-style) on the human-edited config aggregate roots ONLY, surface a concurrent stale edit as **HTTP 409**, and scope the blanket `SaveWithRetry` to BYPASS tokened-aggregate saves so it cannot silently swallow the 409.

## IN scope

- Optimistic-concurrency token on the config aggregate roots: **Team, Portfolio, WorkTrackingSystemConnection, RBAC (UserProfile / RbacGroupMapping / ApiKey), Delivery**.
- Token surfaced on read (config GET payload) and echoed on save; a stale save returns **HTTP 409 Conflict** with no write, distinguishable from generic errors (so UI + CLI/MCP clients can render a "reload and re-apply" recovery affordance).
- Scope `LighthouseAppContext.SaveWithRetry` to BYPASS tokened-aggregate saves (its reload-and-retry is last-writer-wins and would re-hide the conflict).
- RBAC-aggregate concurrency flows through `IRbacAdministrationService`; the 409 is a concurrency outcome orthogonal to the existing 403 authorization path; UI gating stays via `useRbac()`.
- Read-your-writes for a user's own successful edit.
- Lighthouse-Clients (CLI + MCP): config-edit client methods handle 409 distinctly (stale-edit → re-fetch-and-retry guidance) rather than treating it as a generic error.

## OUT scope

- Tokens on high-churn single-writer sync entities (**WorkItem / Feature / FeatureWork / WorkItemStateTransition**) — explicitly NOT tokened; sync throughput must be unaffected.
- The dispatcher work (#5098/#5099), module rules (#5101), work-item events (#5122) — this slice is INDEPENDENT of the dispatcher.
- Any merge UI / three-way diff — recovery is "reload current values, re-apply your change", not automatic merge.
- A new endpoint requiring client version-gating — this changes EXISTING config-edit responses (adds a 409 path), it does not add a new endpoint; no `FEATURE_REQUIRES_SERVER_NEWER_THAN` bump needed for an endpoint, though clients should tolerate servers that never emit 409.

## Learning hypothesis

**Confirms if it succeeds**: a token on the ~5 config roots + a scoped `SaveWithRetry` bypass surfaces a genuine concurrent stale edit as a clean 409 with no write, read-your-writes holds, and the high-churn sync path is untouched (no 409, no throughput change). The lost-update gap is closed surgically.
**Disproves if it fails**: either (a) scoping `SaveWithRetry` away from tokened saves is too entangled to do without regressing the retry behaviour the rest of the app relies on (the careful-change risk ADR-027 flags), or (b) the SQLite rowversion-style token cannot be expressed equivalently to Postgres `xmin` across both providers without forking the persistence layer (would reopen the provider-switched-single-architecture assumption).

## Acceptance criteria

See US-5100 in `../feature-delta.md`. Slice specifics:

- Two-stale-writes integration test (the ADR enforcement row): two admins load the same Team; A saves; B saves against the stale token → B gets **409**, no write, A's value preserved. Repeat for Portfolio and WorkTrackingSystemConnection.
- Read-your-writes test: A's successful save, on reload, shows A's value; the token advanced.
- RBAC test: a stale RbacGroupMapping edit returns 409 THROUGH `IRbacAdministrationService`; an unauthorized actor on the same edit still gets 403 (409 and 403 not conflated).
- Sync-isolation `@property` test: repeated WorkItem / Feature / WorkItemStateTransition saves by the sync engine never require a token and never raise 409; sync throughput is unaffected.
- `SaveWithRetry` test: a tokened-aggregate stale save is NOT reloaded-and-retried (short-circuits to 409); a non-tokened save still benefits from the existing retry.

## Dependencies

**None on the dispatcher slices** — independent. **Coordination**: confirm the tokened-aggregate set (the 5 config roots) is final before scoping `SaveWithRetry` (ADR-027 open question — resolved here as the 5 roots above). EF migration via the existing `CreateMigration` PowerShell script (all providers), NOT `dotnet ef migrations add` directly.

## Production data requirement

**Required (light).** Verify on the project's own Lighthouse instance that a real concurrent edit to its own Team/Portfolio settings produces a 409 (two browser sessions), and that routine single-admin edits never see one. DEVOPS smoke against the production instance after deploy.

## Carpaccio taste tests

- **Independently shippable?** YES — fully user-visible value on its own (closes the lost-update gap); independent of every other slice.
- **One day or less?** NO — ~2 days. Acceptable: it is a single coherent user outcome (no silent lost updates), not splittable by user outcome without leaving a half-protected aggregate set. The 5 roots are one conceptual protection; splitting by root would ship a confusing partial guarantee.
- **End-to-end?** YES — load→edit→save→409→reload→re-apply, exercised by integration tests and a live two-session dogfood.
- **`@infrastructure`?** NO — user-visible (`config-admin` sees a 409 + recovery affordance). Carries the epic's first user-visible value.
