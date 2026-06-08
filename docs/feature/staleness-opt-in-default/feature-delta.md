# Feature Delta — staleness-opt-in-default

> New teams and portfolios must ship with staleness **off** (opt-in), so first-time users are not confronted with unexplained red items. Completes the already-locked **revised D8** opt-in decision from `time-in-state-and-staleness` that was applied to the backend and the portfolio edit-form but never to the create-wizards or the team edit-form.

---

## Wave: DISCUSS / [REF] Persona

- **flow-coach** — runs team flow reviews; staleness is *their* deliberate signal to switch on, not a default imposed on everyone.
- **new team/portfolio admin** (secondary) — just connected a work-tracking system and opened the board for the first time; sees red badges with no idea why, before any conversation about what "stale" should mean for this team.

## Wave: DISCUSS / [REF] JTBD one-liner

Refines `job-flow-coach-spot-stuck-items` (`docs/product/jobs.yaml`). The job's **anxiety** force is the trust gap — "what if this red disagrees with reality?". Imposing a staleness threshold on a brand-new team realises that anxiety on day one: red noise the user never opted into and cannot interpret. Opt-in by default makes staleness a *chosen* signal, preserving the job's value while removing the unexplained-red onboarding tax.

## Wave: DISCUSS / [REF] Pre-requisites

- `time-in-state-and-staleness` is SHIPPED. Its **revised D8** (slice 03) already locked: *"Default staleness threshold = 0 (opt-in) for both newly-created teams AND portfolios… staleness becomes opt-in like every other Flow Metric."*
- Backend already honours this: `Team`/`Portfolio` entities default `StalenessThresholdDays = 0`; integration tests assert it (`TeamStalenessThresholdSettingsIntegrationTest`, `PortfolioStalenessThresholdSettingsIntegrationTest` — *"must default to 0 days (staleness opt-in)"*).
- This feature only finishes propagating that decision into the three frontend literals it never reached.

## Wave: DISCUSS / [REF] Locked decisions

| ID | Decision | Verdict |
|----|----------|---------|
| D1 | New teams **and** new portfolios created via the create-wizard default to `stalenessThresholdDays = 0` (staleness off). This is the real new-entity path (`useWizard = isNew && !cloneFrom`). | Locked — implements revised D8 |
| D2 | The team **edit/clone** default (`EditTeam.tsx` `getDefaultTeamSettings`) is also corrected `7 → 0` for consistency with the already-correct portfolio edit default (`0`). | Locked |
| D3 | **Existing** teams/portfolios are NOT touched. No data migration. An admin who set a threshold (or inherited 7/14 from the upgrade migration) keeps it — only newly-created entities change. (User decision, 2026-06-08.) | Locked |
| D4 | The shipped EF migration's DB column `defaultValue` (7 teams / 14 portfolios) is left as-is. It only ever affected rows present at upgrade time; new rows always carry the FE-sent `0` (the DTO field is `[JsonRequired]`, always sent). Changing it would be a no-op for new entities and risks D3. | Locked |
| D5 | The **seed-on-enable** values stay (team `5`, portfolio `14` in `ModifyTeam/ProjectSettings`) — the value that pre-fills when an admin *ticks the enable checkbox*. Opt-in changes the default state, not the suggested value once you opt in. | Locked |

## Wave: DISCUSS / [REF] User stories

### US-01 — New teams and portfolios start with staleness off

As a **person setting up a new team or portfolio**, I want staleness to start **off**, so that my first view of the board is not full of red items I didn't ask for and can't yet interpret.

`job_id: job-flow-coach-spot-stuck-items`

#### Elevator Pitch
Before: Creating a new team via the wizard ships `stalenessThresholdDays: 7` and a new portfolio ships `14` → items go red on day one with no explanation.
After: complete the **Create Team** (or **Create Portfolio**) wizard → the new team/portfolio's settings show staleness **disabled** (threshold `0`, no red staleness badges); enabling it is a deliberate tick in *Flow Metrics Configuration*.
Decision enabled: the admin decides *if and when* staleness is a meaningful signal for this team, instead of inheriting unexplained red.

#### Acceptance Criteria
1. Creating a new team through the create-wizard (no `cloneFrom`) persists `stalenessThresholdDays = 0`; the team's *Flow Metrics Configuration* shows staleness disabled and no work item renders a stale (red) badge on the basis of staleness alone.
2. Creating a new portfolio through the create-wizard (no `cloneFrom`) persists `stalenessThresholdDays = 0` with the same observable result.
3. Opening the team/portfolio settings and **ticking the staleness enable checkbox** still pre-fills the seed value (team `5`, portfolio `14`) and, once saved with a value `> 0`, badges render red past the threshold — the opt-in path is unchanged (regression guard, per D5).
4. Existing teams/portfolios retain whatever `stalenessThresholdDays` they currently have after this change ships (per D3) — verified by the unchanged backend persistence behaviour; no migration runs.
5. The three corrected literals (`CreateTeamWizard` `7→0`, `CreatePortfolioWizard` `14→0`, `EditTeam` getDefault `7→0`) leave the portfolio edit default (`0`) and all backend defaults (`0`) unchanged.

## Wave: DISCUSS / [REF] Out-of-scope

- Resetting staleness on **existing** teams/portfolios (D3) — explicitly not done.
- Changing the EF migration DB column default (D4).
- Changing the seed-on-enable values or any staleness *visualisation* / *evaluation* logic (D5) — staleness behaves identically once enabled.
- Removing the migration-era 7/14 defaults from the entity/migration history.
- Any new setting, flag, or API field — there is no contract change.

## Wave: DISCUSS / [REF] Cross-cutting impact (DoR Item 7 hard gate)

- **RBAC** — **N/A, because** no authorization surface changes. Editing `stalenessThresholdDays` is already gated by `[RbacGuard(TeamWrite)]` / `[RbacGuard(PortfolioWrite)]`, and UI gating already derives from `useRbac()`. This feature only changes the *initial value* a create-wizard sends through the existing, already-gated write path. Flows through `IRbacAdministrationService` unchanged.
- **Lighthouse-Clients (CLI + MCP)** — **No contract change, so no version-gate.** `stalenessThresholdDays` already exists on the settings DTOs and is already round-tripped — no new/changed endpoint. **Action:** check the clients' create-team / create-portfolio helpers for the *same* hardcoded `7`/`14` default and flip them to `0` for parity; if a client omits the `[JsonRequired]` field entirely it already fails today, so this is a value-parity fix, not a compatibility gate.
- **Website** — **N/A, because** this is a default-value correction to an existing shipped capability, not a new premium feature. Nothing to surface or market; the public docs for staleness already describe it as an opt-in Flow Metric.

## Wave: DISCUSS / [REF] WS strategy

Strategy **B (extend existing)** — single thin vertical slice, no skeleton needed. The change is end-to-end on its own: wizard literal → persisted `0` → no red badges on the new entity's board.

## Wave: DISCUSS / [REF] Driving ports

- Create Team wizard UI (`CreateTeamWizard`) — new-team entry point.
- Create Portfolio wizard UI (`CreatePortfolioWizard`) — new-portfolio entry point.
- Team settings edit/clone form (`EditTeam` default builder).
- (No new HTTP/CLI/MCP surface — existing settings PUT/GET unchanged.)

## Wave: DISCUSS / [REF] Scope Assessment: PASS

1 story, 1 slice, 1 bounded context (settings), 3 one-line literal edits + test alignment, ≪1 day. Zero oversized signals. Deeper carpaccio slicing unnecessary.

## Wave: DISCUSS / [REF] Outcome KPI

| KPI | Target | Granularity | Measurement |
|-----|--------|-------------|-------------|
| `OUT-new-entity-staleness-off` | 100% of teams/portfolios created after this ships have `stalenessThresholdDays = 0` at creation time | per_instance | Sample `StalenessThresholdDays` at first persist for entities with `CreationDate >` ship date; expect all `0` until an admin opts in |

(Supersedes the now-moot `OUT-staleness-threshold-tuning` premise that ≥20% would change a *non-zero* default — there is no non-zero default to change away from anymore.)

## Wave: DISCUSS / [REF] Definition of Done

1. Three FE literals corrected to `0` (`CreateTeamWizard`, `CreatePortfolioWizard`, `EditTeam` getDefault).
2. FE wizard/default tests assert the new `0` default (update any test asserting `7`/`14`).
3. Backend integration tests (already asserting `0`) stay green — no change expected.
4. Clients parity check actioned or recorded N/A.
5. Existing-data untouched verified (no migration added).
6. `pnpm build` + `pnpm test` green; Biome clean.
7. `dotnet build` (warn-as-error) + `dotnet test` green.
8. SonarCloud new-violations gate clean.
9. Public docs unchanged-or-noted (staleness already documented as opt-in).

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|----------|--------|----------|
| 1 | User-value clear | ✅ | US-01 elevator pitch; removes unexplained-red onboarding tax |
| 2 | Job traceability | ✅ | `job-flow-coach-spot-stuck-items` (anxiety force) |
| 3 | Acceptance criteria testable | ✅ | 5 ACs, each observable at a driving port |
| 4 | Dependencies known | ✅ | Pre-requisite: revised D8 from shipped `time-in-state-and-staleness` |
| 5 | Scope bounded | ✅ | Out-of-scope + D3/D4/D5 fence existing data, DB default, seed values |
| 6 | KPI defined | ✅ | `OUT-new-entity-staleness-off`, 100% target |
| 7 | Cross-cutting addressed (RBAC/Clients/Website) | ✅ | All three answered with evidence above |
| 8 | Sized ≤1 day | ✅ | Scope Assessment PASS |
| 9 | No blocking unknowns | ✅ | Root cause confirmed to exact file:line |

## Wave: DISCUSS / [REF] Wave Decisions Summary

**Key decisions:** D1 (both wizards → 0), D2 (team edit default → 0), D3 (existing data untouched), D4 (DB default left as-is), D5 (seed-on-enable values stay).

**Requirements summary:** Finish applying the already-locked opt-in default (revised D8) to the three frontend literals the original feature missed — the two create-wizards and the team edit-default. Backend already correct.

**Feature type:** User-facing (frontend defaults), with a clients-parity follow-up.

**Constraints:** Non-destructive (no data migration); no API/contract change; staleness behaviour once enabled is identical.

**Upstream changes:** None to DISCOVER. This *completes* `time-in-state-and-staleness` revised-D8 rather than contradicting it.
