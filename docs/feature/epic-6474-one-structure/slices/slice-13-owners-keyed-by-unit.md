# Slice 13 — Permissions, free-tier limits, reports and history stay with each Team and Portfolio

Epic #6474 · invisible · `job_id: infrastructure-only` · ≤ 1 day (**may split at DESIGN**: 13a Access +
Licensing, 13b Reports + snapshots) · depends on: 03 (12 soft)

`infrastructure_rationale`: RBAC scopes (`PermissionScopeType`), report owners (`ReportOwnerKind`), snapshot
owners (`OwnerType`) and the free-tier limits each branch on Team versus Portfolio. Resolving them through the
unit and its level lets #6455 add level-3 units without a fourth branch. Wire values stay. Prepares
`job-forecaster-know-when-each-promise-level-lands`.

## Problem

Tom Becker is Admin of one Team and Viewer of one Portfolio; Ana Lima runs the free tier with three Teams.
Four separate "is it a Team or a Portfolio" switches decide who may see a unit, how many units the free tier
allows, who owns a report and whose history a snapshot is. Each would need a new case per level.

## Learning hypothesis

Each switch can read "unit on level N, leaf or not" while every stored value and API answer stays the same.
**Disproved if** a stored discriminator must change (then it is kept and mapped, never rewritten).

## IN

- Access: membership per unit; Team and Portfolio scopes resolve through the unit. System Admin unchanged.
- Licensing: free tier "3 units on the leaf level, 1 on the Feature level" (today's limits), same messages in
  the configured words.
- Reports and the three snapshot tables address the unit; `OwnerType` and `ReportOwnerKind` values unchanged.

## OUT

- Membership on level-3 units, and D15's Premium gate for levels above (#6455).

## Domain examples

1. Tom, Admin of "Payments Crew", can edit it and only it, as today.
2. Ana, free tier, creates a fourth Team: refused with "Free users can only use up to 3 Teams" in her words.
3. The blocked-count history of "Checkout Revamp" shows the same days after the upgrade.

## UAT scenarios

### Scenario: Unit membership grants what it granted before

Given Tom is Admin of "Payments Crew" and Viewer of "Checkout Revamp"
When he uses Lighthouse after the upgrade
Then he can edit "Payments Crew" and only read "Checkout Revamp"

### Scenario: The free tier allows three Teams and one Portfolio

Given Ana has three Teams and one Portfolio on the free tier
When she tries to add a fourth Team or a second Portfolio
Then she is refused, as today

### Scenario: History and reports stay with their unit

Given "Checkout Revamp" has a Then and Now report and 90 days of blocked history
When the install is upgraded
Then the report and the history still belong to "Checkout Revamp"

### Scenario: An API key scoped to a Team still works

Given an API key scoped to "Payments Crew" as Viewer
When the CLI reads that Team
Then the read succeeds and other Teams are refused

## Acceptance criteria

- [ ] Everyone can do exactly what they could before, on the UI and through API keys.
- [ ] The free tier allows 3 Teams and 1 Portfolio, with today's messages in the configured words.
- [ ] Reports and history stay with their Team or Portfolio.
- [ ] Slice 01 check green.

## Outcome KPI

OUT-1, OUT-4.

## Technical notes

- Stored values `PermissionScopeType`, `OwnerType`, `ReportOwnerKind` are not rewritten; `useRbac()` and the
  `my-summary` shape unchanged.
- All RBAC logic stays in `IRbacAdministrationService` (CLAUDE.md).
- `LicenseGuardAttribute` counts with `count > limit` and per-route overrides; keep those numbers exactly.
