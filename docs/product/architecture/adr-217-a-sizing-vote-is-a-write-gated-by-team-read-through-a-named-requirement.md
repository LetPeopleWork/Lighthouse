# ADR-217: A sizing vote is a write gated by Team read, through a named `TeamContribute` requirement that maps to the read predicate

- **Status**: Proposed (DESIGN, 2026-10-02)
- **Date**: 2026-10-02
- **Feature**: epic-5510-5881-refinement (ADO Epic #5510 slices 11, 12, 15, 16, 17b)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Readers vote and comment; editors change settings (C4, D4, AC-11.4, AC-15.2). In RBAC terms a Viewer on a Team —
who can read it — must be able to persist a sizing entry, while a Team admin alone edits refinement settings.
Lighthouse has never had a **persisting** write gated by a read permission; DISCUSS asked DESIGN to make that
explicit and safe rather than reach for an edit requirement.

What exists:

- `RbacGuardRequirement` {SystemAdmin, TeamRead, TeamWrite, PortfolioRead, PortfolioWrite, CanCreateTeam,
  CanCreatePortfolio, SystemAdminOrBootstrap, AnyScopedAdmin} (`RbacGuardRequirement.cs:3-14`), evaluated in
  `IRbacAdministrationService.CanSatisfyRequirementAsync` (`RbacAdministrationService.cs:341-367`).
- `TeamRead` = TeamAdmin or Viewer; `TeamWrite` = TeamAdmin (`RbacAdministrationService.cs:1231-1245`). With RBAC
  not enforced (auth off, or RBAC off) both pass.
- `RbacGuardAttribute` answers a failed **read** requirement with 404 (non-disclosing) and a failed write with 403
  (`RbacGuardAttribute.cs:69-76`).
- Several POST actions already use `TeamRead` (`RunManualForecast`, `RunBacktest`, the Reality Check) — but none of
  them persists anything.

## Decision

1. **Add `RbacGuardRequirement.TeamContribute`** (appended member). In `CanSatisfyRequirementAsync` it is
   **`CanReadTeamAsync`** — the same call, not a second rule — so "may contribute" and "may read" cannot drift until
   someone decides they should, in this one line.
2. **`RbacGuardAttribute` treats it as scoped and non-disclosing**: a caller who cannot read the Team gets 404, as for
   any read; a caller who can read it never sees a 403 from the guard.
3. **Only the sizing endpoints carry it**: `POST …/votes`, `POST …/comments`, `DELETE …/votes/mine` in
   `RefinementVotesController`. Refinement settings stay on the unchanged `TeamWrite` Team settings write.
4. **UI gating stays in `useRbac()`**: a new `canContributeToTeam(teamId)` (true wherever the Team is readable); no
   component fetches the authorization summary itself.
5. **The write is bounded**: a named rate-limit policy `RefinementContribution` (partitioned by subject, else by the
   hashed voter key, else by remote address), comment ≤ 2,000 chars, name ≤ 100, and only on Work Items currently in
   a refinement state (409 otherwise). Identity rules are ADR-216's.

Why the write is safe at read level: it only **appends an entry attributed to the caller**; it changes no Work Item,
no setting and nothing in the work tracking system (D9); every entry records who, when and through which channel.

## Alternatives considered

- **Reuse `TeamRead` on the POST.** Works today and is what the `RunManualForecast` precedent does. Rejected because
  those POSTs persist nothing; putting a persisting action under the read name hides it from anyone auditing
  "what can a Viewer change?", and a future restriction (e.g. a Premium "contributor" role, D10) would have to touch
  every read. Kept as the fallback if the maintainer prefers no new member.
- **`TeamWrite`.** Only Team admins could vote; contradicts C4 and AC-11.4. **Rejected.**
- **A new role (e.g. "Contributor").** No role differs from Viewer today; a role is a user-visible RBAC change with
  migration and UI cost. **Rejected** — a requirement adds a name, a role adds a concept.

## Consequences

- **Positive**: "what can a Viewer persist?" has a greppable answer — every action carrying `TeamContribute`.
- **Positive**: non-disclosure is preserved; a non-reader cannot learn a Team exists by voting on it.
- **Negative**: one more enum member every exhaustive switch over `RbacGuardRequirement` must handle (the existing
  switch has a `_ => false` default, which is the safe failure).
- **Negative**: an abusive reader can still add noise within the rate limit. Visible (attributed) and bounded;
  admins cannot delete entries (append-only, ADR-216). A moderation tool is out of scope.

## Enforcement

- Parameterised test: `TeamContribute` evaluates exactly as `TeamRead` for every role × RBAC on/off × auth mode.
- Allowlist test: every controller action carrying `TeamContribute` is in `RefinementVotesController`.
- Guard test: a non-reader gets 404, an unlinked-key reader gets 403 `vote-needs-a-person` (ADR-216), a Viewer gets
  201.

Cross-refs: [ADR-001](./adr-001-rbac-ui-gating-strategy.md),
[ADR-005](./adr-005-rate-limiting-middleware.md),
[ADR-136](./adr-136-feature-move-authorization-and-non-disclosing-block-reason.md),
[ADR-216](./adr-216-the-sizing-log-is-append-only-and-keyed-by-a-voter-key.md).
