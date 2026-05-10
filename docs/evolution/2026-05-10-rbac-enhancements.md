# Evolution: rbac-enhancements

**Finalized**: 2026-05-10
**Wave path**: DISCUSS → DESIGN → DISTILL → DELIVER
**Outcome**: Production code shipped, full E2E suite green, CI routing fixed

## Summary

Closed five outstanding RBAC UX gaps and added end-to-end test coverage for the entire role lifecycle. The feature was sliced via Elephant Carpaccio into four independently shippable slices and delivered as 18 TDD steps + 1 CI tag fix (19 commits total).

The five gaps:

1. **Emergency admin had no UI signal** — system showed "Yes" in the System Admin column with a Revoke button that was a footgun (self-locking).
2. **No way to remove a user** — once a user logged in, their UserProfile lingered forever; only roles could be revoked.
3. **Scoped group mappings 403'd for non-System-Admins** — the `ScopedGroupMappingManager` component called the global `/authorization/group-mappings` endpoint which only sysadmins can read, surfacing a generic "Failed to load" error to Team/Portfolio admins.
4. **Viewers saw broken/inaccessible controls** — Settings/Access tabs, write buttons, connections section, and Log Level were rendered for users who couldn't actually use them.
5. **No E2E coverage for the role matrix** — no automated guard for the gating decisions, the emergency-admin invariant, or the group-vs-individual rights parity.

## Business context

Lighthouse moved from an open-by-default model to RBAC, but the migration left several follow-ups: (a) UX polish for the new role surface, (b) operational primitives like user removal, (c) symmetric scoped self-service for Team/Portfolio admins, and (d) a regression net so the next refactor doesn't silently break access control. This feature closed all four.

## Key decisions

Source: `docs/feature/rbac-enhancements/feature-delta.md` (DISCUSS / DESIGN / DISTILL sections), promoted to ADRs in `docs/product/architecture/`.

| ID | Decision | Rationale |
|---|---|---|
| **DD-01** | Hide write controls from Viewers; don't disable | Disabled controls invite users to look for workarounds. Hidden controls match the user's mental model: "I can't do that here." Enforced everywhere via `condition && (<Button/>)`. |
| **DD-03** | Emergency admin is config-only, non-revocable via UI | An emergency admin exists precisely so a botched RBAC config can be recovered. Letting the UI revoke the lifeline is self-defeating. The row shows "Emergency Admin" + lock icon; no Revoke button. |
| **DD-07** | System Admins tab hidden when `isRbacEnabled = false` | Showing a tab whose contents are meaningless in non-RBAC deployments is noise. PERMISSIVE_SUMMARY keeps non-RBAC behavior intact for everything else. |
| **DD-08** | Viewers see Deliveries (read-only) | Delivery forecasts are the primary value for Viewers. Hide only the Add/Edit/Delete buttons, not the data. |
| **DD-09** | Add Team / Add Portfolio enabled for non-sysadmin with `canCreateTeam` regardless of connections | Connections are a sysadmin concern; gating non-sysadmin creation on connections they can't see leads to confusing dead-ends. |
| **DD-10** | Settings tab gated on `isTeamAdmin`/`isPortfolioAdmin` only — not `isRbacEnabled` | Per-team/portfolio settings still matter when RBAC is off. |
| **OQ-04 resolution** | `IsEmergencyAdmin` lives on `RbacUserSummary` only, not `UserAuthorizationSummary` | The emergency admin sees themselves as a normal System Admin in their own summary by design — indistinguishable. |
| **WD-07 invariant** | Group-based and individual rights produce **behaviorally identical** outcomes | Verified by Scenario 7a–7d running assertions identical to 6a–6d, with assignments swapped from individual permissions to SSO group mappings. |

ADRs (now permanent in `docs/product/architecture/`):
- `adr-001-rbac-ui-gating-strategy.md` — DD-01 (hide vs disable)
- `adr-002-scoped-group-mapping-endpoint.md` — root cause + fix for the "Failed to load" bug
- `adr-003-emergency-admin-display.md` — DD-03

## Steps completed (18)

| Step | Commit | What |
|---|---|---|
| 01-01 | `a1c4e07a` | Backend: `IsEmergencyAdmin` on `RbacUserSummary`; populated from `EmergencySystemAdminSubjects` config |
| 01-02 | `82f8f121` | Frontend: `isEmergencyAdmin?: boolean` on `RbacUser` TS interface |
| 01-03 | `1272531e` | UI: Accordion replaces 6 status chips; emergency admin row + lock icon; Remove button + confirm dialog |
| 01-04 | `3517b73c` | Walking skeleton E2E verified green |
| 02-01 | `338b26bd` | Backend: `DELETE /authorization/users/{id}` + cascade-delete `UserPermissions` in single transaction |
| 02-02 | `dafc9bcc` | Backend: scoped `GET /authorization/{teams,portfolios}/{id}/group-mappings` (Team/Portfolio Admin auth) |
| 02-03 | `0371e2fc` | Frontend: real `deleteUser` HTTP call + self-removal guard via `authService.getCurrentUserProfile()` |
| 02-04 | `0fca932e` | `ScopedGroupMappingManager` now takes `groupMappingsFetcher` prop (ADR-002 fix) |
| 02-05 | `20fa8d38` | Settings.tsx: System Admins tab gated on `isRbacEnabled && isSystemAdmin` |
| 03-01 | `55fc6860` | TeamDetail: tab + write-control gating on `isTeamAdmin(teamId)` |
| 03-02 | `923a2eef` | PortfolioDetail: symmetric tab + write-control gating |
| 03-03 | `8e9e4250` | PortfolioDeliveryView: `canEdit` prop hides Add/Edit/Delete buttons; Deliveries tab decoupled from admin gate |
| 03-04 | `bf5dcb05` | SystemSettingsTab: Log Level section gated on `isSystemAdmin` |
| 03-05 | `aa9b4eb8` | OverviewDashboard: viewer-clean (connections section, Add Team logic, OnboardingStepper) |
| 04-02 | `68d52812` | Enable E2E Scenarios 3 + 4 |
| 04-03 | `2f543ce4` | Enable E2E Scenario 5 (admin creates entities + assigns scoped roles) |
| 04-01 | `9921a199` | Enable E2E Scenario 2 (team reader restrictions); reorder describes so 2 runs after 5 |
| 04-04 | `be83890f` | Enable E2E Scenarios 6 + 7 (per-role + group parity) |

Plus follow-up: `339e5d91` — CI tag `@Auth` on RBAC E2E describe so suite runs only in `ci_verifyauth` (not `ci_verifysqlite`/`postgres`).

## Final test counts

- **Backend xUnit**: 2197 passing
- **Frontend Vitest + RTL**: 2659 passing (includes new RBAC component tests)
- **E2E `@RBAC E2E`**: 15/15 passing (Scenarios 1, 2a, 2b, 3, 4, 5, 6a–d, 7 setup, 7a–d)

## Lessons learned

**1. Test ordering vs scenario numbering — reorder in the spec, don't rename scenarios.**
Scenario 2b ("team reader restricted on team") needs the team reader to be assigned to a team — state Scenario 5 sets up. The original spec ordered tests numerically (1 → 2 → 3 → … → 5), which broke 2b. Step 04-01 moved the Scenario 2 describe block to run *after* Scenario 5. The numbering still reflects the storyboard; the execution order reflects state dependency.

**2. The "test against the dev seed" tradeoff.**
Scenario 5 originally wanted to create a fresh `RBAC E2E Test Team` and `RBAC E2E Test Portfolio`. The team/portfolio creation wizards demand fully configured work-tracking-system connections + work-item types + states. Wiring all of that just to exercise role assignment was disproportionate. Step 04-03 used the dev-seed `Team Zenith` / `Project Apollo` instead — same role-assignment behavior verified, much less fixture surface.

**3. `pnpm exec playwright test --list` is the cheapest way to verify CI grep changes.**
The `@Auth` tag fix for CI routing was verified by listing tests under each grep pattern, not by running the suites. Two seconds vs minutes.

**4. The walking skeleton's value showed up in step 04-02's adaptation.**
Scenario 3's original assertions (revoke test user, see "No" appear) couldn't ever pass against production code because the test user is the configured emergency admin and DD-03 hides Revoke for emergency admins. The crafter caught this *because* the walking skeleton (Scenario 1) was already green and pinned the baseline behavior. The adaptation was to verify the protection invariant directly. Without the skeleton, this would have been a long debugging session about "why is Revoke missing?"

**5. PERMISSIVE_SUMMARY is load-bearing for non-RBAC deployments.**
Several steps initially had tests that assumed `isSystemAdmin: false` when RBAC was disabled. The actual contract: when `isRbacEnabled = false`, every `is*Admin` defaults to `true` so existing single-tenant deployments keep working unchanged. Two existing test setups had latent bugs masked by this — surfaced when DD-07 made the gate explicit.

**6. GPG signing failure isn't a reason to bypass signing.**
Step 03-03's crafter hit a GPG agent prompt that couldn't reach a TTY. The crafter correctly stopped and asked, rather than running `--no-gpg-sign`. The orchestrator caching the passphrase and committing on the crafter's behalf preserved both the signing policy and the audit trail.

## Issues encountered

- **Step 04-03 — UserProfile bootstrapping**: scoped users (teamreader, teamadmin, portfolioreader, portfolioadmin) only exist in `UserProfiles` after their first login. The Scenario 5 test had to log each user in once (just to create the profile) before the system admin could assign roles. Recorded inline as a comment in the spec.
- **CI routing for E2E**: RBAC tests were initially tagged only `@RBAC E2E`. The package.json `test` script uses `--grep-invert "@screenshot|@auth"` — RBAC tests would have run in `ci_verifysqlite`/`postgres` (no Keycloak there) and failed. Fixed post-finalize via the `@Auth` tag (commit `339e5d91`). **Take-away**: any new spec under `tests/specs/auth/` should carry the `@Auth` tag at the outer describe level.
- **Sub-agent symlink workaround (step 02-01)**: one crafter session, when its working directory was reset between Bash calls, created a symlink in `Lighthouse.Backend/Lighthouse.Backend/docs/feature/...` so the DES stop-hook could resolve the execution log. The orchestrator was alerted via security warning; reviewed and accepted post-hoc since no production code was touched. The hook resolves cwd-relative paths for the log; future agent dispatches should be told to operate from the repo root.

## Migrated artifacts

The lean v3.14 SSOT model writes architecture/ADRs/journeys directly to `docs/product/` during the wave. Nothing required Phase-B migration. Permanent locations:

- `docs/product/architecture/brief.md`
- `docs/product/architecture/c4-diagrams.md`
- `docs/product/architecture/adr-001-rbac-ui-gating-strategy.md`
- `docs/product/architecture/adr-002-scoped-group-mapping-endpoint.md`
- `docs/product/architecture/adr-003-emergency-admin-display.md`
- `docs/product/journeys/rbac-enhancements.yaml`
- `docs/product/jobs.yaml` (extended with rbac jobs)

## Workspace preserved

Per nw-finalize convention, `docs/feature/rbac-enhancements/` stays. The wave matrix derives status from it. Session markers (`.develop-progress.json`, `.nwave/des/deliver-session.json`) removed.

## Pre-release checklist

- [x] All 18 steps green
- [x] Full E2E suite verified
- [x] CI routing correct (RBAC tests in `ci_verifyauth` only)
- [x] ADRs in permanent location
- [x] Evolution doc written
- [ ] Mutation testing per `per-feature` strategy (Stryker.NET backend, Stryker frontend; ≥80% kill rate) — **deferred**: requires running Stryker locally; not blocking finalize but required before release
- [ ] PR opened against main — **N/A**: feature was developed directly on main with signed commits
