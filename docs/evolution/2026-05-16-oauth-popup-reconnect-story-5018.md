# Evolution — oauth-popup-reconnect (Story #5018)

Date: 2026-05-16
Branch: `main` (slice-boundary push pending)
Wave path: DISCUSS (implicit, via the ADO story) → DESIGN (3 review iterations) → DISTILL (2 review iterations) → DELIVER (9 steps, Phase 06 of the OAuth roadmap)
Outcome: shipped. OAuth handshake migrated from full-page redirect to a same-origin popup; the originating connection-edit dialog stays mounted and the status badge flips live. Slice 04 of the OAuth feature (parent epic ADO #2438; story ADO #5018).

---

## Feature goal and user intent

When a customer's OAuth connection had gone stale and Lighthouse surfaced a "Reconnect required" banner, the slice-02 flow drove a **full-page redirect** to the IdP consent screen. Two consequences:

1. After consent, the user landed on the create-connection page rather than the dialog they were working in. They lost edit context and couldn't visually confirm the badge flip on their connection.
2. The backend persisted a half-baked `OAuthCredential` row so the round-trip could survive the redirect — the root cause of the slice-02 duplicate-key defensive read in `WorkTrackingSystemConnectionsController` and the `UpsertValidCredential` defence in `OAuthService`.

Manual testing of slice-02 surfaced both. Story #5018 fixes them by **replacing the redirect with a popup**. The user clicks Reconnect → a popup opens to the IdP → user consents → the popup `postMessage`s `oauth.complete` back to `window.opener` and closes itself → the edit dialog (still mounted) re-fetches the connection → the badge flips from `Reconnect required` to `Connected`. No page navigation, no half-baked credential row, no duplicate-row defence needed.

DESIGN also brought the initial-create flow (`CreateConnectionWizard.startOAuthHandshake`) under the same popup mechanism — the wizard never unmounts; success advances inline to step 3 (naming + create). The `?oauth=success&connectionId=` resume-URL contract that survived the redirect is deleted (DDD-14).

---

## Wave-by-wave summary

### DISCUSS — implicit (via the ADO story)

No separate DISCUSS session. ADO #5018 (`I can reconnect OAuth in a popup without leaving the connection edit dialog`) carried the JTBD + scope + 4 acceptance hints. The hints were refined in DISTILL into 23 covering scenarios. No new user-story breakdown was needed — slice-04 is one self-contained story.

### DESIGN — 3 review iterations, all under Architect (Atlas, Haiku)

Morgan produced the application-level DESIGN: ADR-011 (popup flow, Option A `postMessage` accepted, Option B `BroadcastChannel` pre-approved as same-shape fallback), DDD-11..DDD-16 covering the popup mechanism, origin + message-type filtering, DB-level UNIQUE index on `OAuthCredential.WorkTrackingSystemConnectionId`, the deleted resume-URL contract, popup-blocked / cancelled fallbacks, and the `window.opener === null` fallback.

**Iteration 1** (Atlas) returned NEEDS_REVISION with 2 blockers and 4 mediums: backend integration test + E2E test still asserted the old `/connections/new?oauth=success&connectionId=...` callback target after DDD-14; the dead `CreateConnectionWizard.test.tsx` resume-URL test had no replacement spelled out; OQ-5018-3 Safari Webkit gold-test lacked falsifiable acceptance criteria; the wizard's non-success UX matrix wasn't explicit; AC #4 wording ("user is redirected back") was mechanism-specific but the popup mechanism doesn't redirect.

**Iteration 2** (Morgan) addressed the two blockers + the OQ-5018-3 falsifiability gap (added a four-criterion 5 s pass spec with version pinned to `browserslist`; auto-trigger of the BroadcastChannel swap on handshake-never-reaches-opener; mirrored the criterion into ADR-011's Earned Trust table). Atlas re-reviewed and returned APPROVED. The other three mediums were explicitly deferred: AC #4 wording → PO confirmation; 90 s timeout constant naming → DELIVER; fallback copy refinement → DELIVER.

**PO confirmation on AC #4** — behavioural interpretation chosen (Option A). US-01 AC #4 was amended in place at `feature-delta.md:50` from *"the user is redirected back to the connection settings page"* to *"the connection settings surface shows `Status: Connected — OAuth (Jira Cloud)`"* — both the slice-02 redirect implementation and the Story #5018 popup mechanism now satisfy the same AC text. Comment posted on ADO #4967 (Closed) documenting the amendment; ADO #5018's description rewritten to record the DESIGN resolutions.

**Iteration 3** (Morgan, triggered by a follow-up reading of the wizard EXTEND row) — discovered that the wizard's non-success UX (popup_blocked / cancelled / error) was specified only as "success advances to step 3 inline" without explicit error UX, while the sibling `ReconnectBanner` and `OAuthAuthForm` rows spelled out all four statuses. Morgan tightened the wizard row to match the sibling pattern, added a draft-persistence decision (keep + reuse on retry — the draft is a hard prerequisite for `POST /api/oauth/{provider}/connect`, not a survive-the-redirect artifact), and added one new wizard-level popup_blocked test case to the DDD-14 test-updates subsection.

### DISTILL — 2 review iterations (Sentinel for artifacts, Atlas cross-wave for DDD coverage)

Sentinel produced the executable specifications: 23 scenarios across hook (7) / landing page (3) / banner (5) / form (5) / wizard (3) / backend integration (3) levels, plus one Playwright walking-skeleton-migration scenario. Walking-skeleton strategy: **migration of the existing slice-02 WS** (no new WS introduced — the existing slice-02 OAuth e2e walking skeleton is migrated to the popup transport).

RED scaffolds were created for the two new TS modules (`useOAuthPopup.ts`, `OAuthPopupComplete.tsx`) with `// SCAFFOLD: true` markers and `throw new Error("Not yet implemented — RED scaffold")` bodies. All RED tests verified to fail RED (not BROKEN — assertion-style failures, not import/compile errors). 21F / 35P Vitest at DISTILL handoff; 3F / 7P NUnit (one of the three known-RED was for DDD-13 UNIQUE-index integration, the other two for the DDD-14 redirect-target rewrite).

DISTILL was reviewed in one pass by Sentinel (APPROVED, 9-10/10 across 8 critique dimensions, all 4 mandates pass, zero Testing Theater patterns) and one cross-wave pass by Atlas (APPROVED — every DDD-11..16 has a covering RED test that would catch its regression).

### DEVOPS — not run

No new infrastructure, no new env var, no migration outside the additive EF UNIQUE-index migration (handled inside Phase 06 step 06-01). The existing `oauth-smoke` CI job + the existing `ci_e2e.yml` workflow are the deployment infrastructure; no DEVOPS-wave artifacts were needed.

### DELIVER — 9 steps (Phase 06 of the OAuth roadmap), all reaching COMMIT/PASS

| Step | Commit | What landed |
|---|---|---|
| 06-01 | `587793cd` | EF migration: UNIQUE index on `OAuthCredential.WorkTrackingSystemConnectionId` (DDD-13). SQLite + PostgreSQL migrations generated via the existing `CreateMigration` PowerShell script. Data-safe: slice-02 has not GA-shipped, zero customer instances carry duplicate rows. |
| 06-02 | `c404ff88` | Collapse defensive `GroupBy/OrderByDescending` in `WorkTrackingSystemConnectionsController` to plain `ToDictionary`. Behaviour-preserving refactor protected by the new UNIQUE index. One obsolete duplicate-tolerance test removed (Test Integrity rule — DDD-13 made the scenario structurally impossible). |
| 06-03 | `1286d716` | `OAuthController.Callback` 302 targets rewritten to `/oauth/popup-complete?status=success&connectionId={id}` and `/oauth/popup-complete?status=error&reason={code}`. The slice-02 `?oauth=success&connectionId=` resume-URL contract is intentionally deleted (DDD-14). |
| 06-04 | `68468920` | `useOAuthPopup` hook implementation (~80 LOC). Centred 600×700 popup; opener-side `event.origin === window.location.origin && event.data?.type === "oauth.complete"` filter; 500 ms popup-closed poller; 90 s wall-clock guard. Resolves with one of four `OAuthPopupResult` statuses (`success` / `error` / `cancelled` / `popup_blocked`). The 90 s constant carries a one-line WHY comment referencing the state-token TTL invariant (rare-WHY exemption per CLAUDE.md; resolves Iteration-2 deferral). |
| 06-05 | `6525b0ed` | `OAuthPopupComplete` landing-page component (~30 LOC) + `/oauth/popup-complete` route in `App.tsx`. Same-origin `postMessage` to `window.opener` with `window.location.origin` as `targetOrigin` (never `'*'`). Fallback render for `window.opener === null` with the Iteration-1 refined copy ("you may close this window — if it doesn't close automatically, close it manually"). |
| 06-06 | `55eb6d2b` | `ReconnectBanner` migrated to `useOAuthPopup`. Inline alerts for popup_blocked (`error`) / cancelled (`info`) / error (`error` with `result.reason`); banner stays visible until success; `onReconnected?.()` invoked exactly once on success. |
| 06-07 | `865c4eb9` | `OAuthAuthForm` migrated to `useOAuthPopup` — mirrors the banner pattern. `onConnect?.()` exactly once on success; Connect button re-enabled on every non-success. |
| 06-08 | `614295b4` | `CreateConnectionWizard.startOAuthHandshake` migrated to `useOAuthPopup`. Success advances inline to step 3 (no remount); non-success keeps step 2 mounted with severity-appropriate alert + Connect re-enabled. Mount-time `?oauth=success&connectionId=` resume `useEffect` deleted (DDD-14 dead code). Draft `WorkTrackingSystemConnection` row reused on retry — the draft is a hard prerequisite for `POST /api/oauth/{provider}/connect`, not a survive-the-redirect artifact (Iteration-3 Q2-grounded decision). |
| 06-09 | `fe3105c6` | `ModifyConnectionSettings` passes `onReconnected={() => reloadConnection()}` into `ReconnectBanner` (the closure point for the slice). Playwright walking-skeleton-migration scenario unskipped and implemented against `StubOAuthProvider` (`@walking_skeleton @popup-migration @driving_adapter @real-io @in-memory @US-01 @Story-5018`). The `@deferred @OQ-5018-3` Safari Webkit gold-test preserves its `.skip` — explicit non-goal of this slice; deferral resolution is a future slice. |

DES audit trail: `docs/feature/work-tracking-oauth-authentication/deliver/execution-log.json` (the harness-protected log; entries appended via `des-log-phase` only). Integrity verification reported clean for Phase 06 steps (the 16 pre-existing violations on phases 02/03/05 are historical gaps from before DES enforcement was applied to this project, unrelated to Story #5018).

### Phase 3 (refactor) — Path B, nothing to refactor

Per-step REFACTOR during GREEN already covered RPP L1-L6 for the Phase 06 surface. The reviewer noted one out-of-scope semantic-duplication finding (`buildCallbackUrl` in `OAuthAuthForm` vs. `buildOAuthCallbackUrl` in `CreateConnectionWizard` — both byte-identical helpers encoding the same `/api/oauth/callback` knowledge) — flagged for a future slice-02 cleanup PR, not in scope for this Phase-3 pass. The three call-site inline-alert state machines are structurally similar but represent three different business concepts (reconnect-from-edit-dialog vs. connect-from-form vs. connect-from-wizard-step-2) — intentional non-refactor per CLAUDE.md's "DRY = don't repeat KNOWLEDGE, not code" rule.

### Phase 4 (adversarial review) — APPROVED, zero defects

The nw-software-crafter-reviewer (Haiku) ran the full Testing Theater 7-pattern detection + Lighthouse-specific CLAUDE.md convention check + security checkpoints + wiring smoke + design compliance (RCA F-2) + wave completion (RCA F-3). Verdict: APPROVED. No blockers, no Testing Theater patterns. Security checkpoints all satisfied: `postMessage` `targetOrigin` is the BaseUrl (never `'*'`), opener-side `event.origin === window.location.origin` filter present, query string carries no secrets, state-token TTL invariant documented at the 90 s constant. Genuine praise on test quality, code discipline, security model, and external wiring.

### Phase 5 (mutation testing) — PASS-with-rationale

Stryker per-feature configs added in commit `c4821076`: `Lighthouse.Frontend/stryker.config.popup-reconnect.mjs` + matching `vitest.stryker.popup-reconnect.config.ts` for frontend; `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.popup-reconnect.json` for backend. Kill rates: backend **67.21%** (55 mutants; 41 killed) / frontend **~76.1%** (222 mutants; 169 killed including a new test added for the 90 s timeout branch).

The Phase 06 **changed-line surface** (the `OAuthController.Callback` redirect URL strings and the `WorkTrackingSystemConnectionsController` LINQ collapse) is 100% killed. Survivors classified:

- Equivalent mutants — defensive null-coalescing in DI constructors (never null in practice); popup-window centering pixel math (testing pixel-perfect positioning is implementation coupling).
- Acceptable misses — log-message string literals (Sonar-banned to assert on); UI literal strings (testing exact wording is implementation coupling, behaviour is asserted via role queries).
- Pre-existing — most backend survivors are slice-01/02/03 code paths outside the Phase 06 diff (the `WorkTrackingSystemConnectionsController` file has historical helpers in lines 149-173 that weren't touched here).

One genuine gap surfaced and closed with a new test: `useOAuthPopup` 90 s timeout branch. The test asserts the documented behavioural contract ("a user who leaves the popup open while away from their desk must see the hook clean up rather than hang") — real behavioural assertion, not Testing Theater. No tests added solely to chase mutants.

Stryker `break: 0` threshold leaves the gate informational — the project's per-feature mutation strategy uses kill rate as a signal, not a hard build gate. Verdict: PASS-with-rationale.

### Phase 7 (finalize, this entry) + Phase 8 (retro) — clean execution, no 5-Whys

Phase 06 execution was clean: every step reached COMMIT/PASS through DES on the first attempt. No re-dispatches, no timeouts, no blockers. The two DES hook rejections during the run (one for a missing `RECORDING_INTEGRITY` section in a Task prompt, one user-initiated terminal-mix-up interrupt) were corrected immediately and did not affect the underlying TDD work. No retrospective is warranted beyond noting the clean run.

---

## What changed externally (release-notes payload)

User-visible changes:

- **OAuth reconnect** no longer leaves the connection edit dialog. Clicking *Reconnect* opens the IdP in a popup; on success the popup closes itself and the connection's status badge flips from `Reconnect required` to `Connected` live, without a page reload or navigation.
- **OAuth initial connect** behaves the same way — the create-connection wizard stays on step 2 throughout the IdP round-trip; success advances inline to step 3 (naming + create).
- Popup-blocked / cancelled / IdP-error scenarios surface inline alerts in the originating dialog / form / wizard step; the user can retry by clicking Reconnect / Connect again. On Safari Webkit, if `window.opener` is severed by ITP for a specific landing-page sequence, the design's pre-approved BroadcastChannel fallback applies automatically (out of scope for this slice; gold-test for the trigger condition is `@deferred`).

Internal changes:

- New DB-level UNIQUE index on `OAuthCredential.WorkTrackingSystemConnectionId` (DDD-13). The slice-02 defensive `GroupBy/OrderByDescending` read in `WorkTrackingSystemConnectionsController` is collapsed to plain `ToDictionary`. The slice-02 `OAuthService.UpsertValidCredential` defence stays (retry-after-error is still a real path).
- Deleted: the `?oauth=success&connectionId=` resume URL contract (DDD-14).

---

## Outstanding work flagged for follow-up

| Item | Owner | Trigger |
|---|---|---|
| OQ-5018-3 Safari Webkit gold-test for `window.opener` severance under ITP | Future slice (separate DELIVER pass) | When Safari Webkit support becomes a hard requirement, or when an ITP-related popup failure is reported by a customer |
| `buildCallbackUrl` (OAuthAuthForm) vs. `buildOAuthCallbackUrl` (CreateConnectionWizard) — byte-identical slice-02 helpers, semantic duplication | Slice-02 cleanup PR | Opportunistic; not blocking any feature |

---

## Files committed in Phase 06 (10 commits, ~1100 insertions / ~250 deletions)

- `587793cd  feat(oauth): add UNIQUE index on OAuthCredential.WorkTrackingSystemConnectionId (DDD-13)`
- `c404ff88  refactor(oauth): collapse defensive GroupBy/OrderByDescending in WorkTrackingSystemConnectionsController (DDD-13)`
- `1286d716  feat(oauth): redirect OAuth callback to /oauth/popup-complete landing page (DDD-14)`
- `68468920  feat(oauth): implement useOAuthPopup hook with postMessage handshake (DDD-11/12/15/16)`
- `6525b0ed  feat(oauth): add OAuthPopupComplete landing page + register /oauth/popup-complete route (DDD-14/16)`
- `55eb6d2b  feat(oauth): migrate ReconnectBanner to useOAuthPopup with non-success inline surfaces (DDD-14/15)`
- `865c4eb9  feat(oauth): migrate OAuthAuthForm to useOAuthPopup with non-success inline surfaces (DDD-14/15)`
- `614295b4  feat(oauth): migrate CreateConnectionWizard.startOAuthHandshake to popup + delete dead resume-URL branch (DDD-14/15)`
- `fe3105c6  feat(oauth): wire ModifyConnectionSettings.onReconnected + unskip popup-migration walking-skeleton Playwright scenario (Story #5018)`
- `c4821076  chore(oauth): add Stryker per-feature configs for Story #5018 popup reconnect (+ timeout test)`
- `5e9f4756  docs(oauth): Phase 06 DELIVER summary for Story #5018 popup reconnect`

Plus DESIGN/DISTILL preparatory commits:
- `497c58a6  docs(oauth): DESIGN wave output for Story #5018 (popup reconnect)`
- `07875566  test(oauth): RED scaffolds + acceptance tests for Story #5018 popup reconnect`
- `7a79eff1  chore(oauth): extend roadmap with Phase 06 for Story #5018 popup reconnect`
