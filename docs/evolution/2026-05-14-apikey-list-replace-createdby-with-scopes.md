# Evolution — apikey-list-replace-createdby-with-scopes

Date: 2026-05-14
Branch: `main` (two commits ahead of `origin/main` at finalize)
Wave path: DISTILL (fast-tracked) → DELIVER (2 steps)
Outcome: shipped. "Created By" column dropped from the API Keys listing; replaced by a `useRbac()`-gated "Scopes" column on RBAC-on deployments. 9/9 acceptance scenarios green.

---

## Feature goal and user intent

On the **System Settings → API Keys** tab, drop the always-shown "Created By" column from the listing and — when RBAC is enabled — replace it with a "Scopes" column that surfaces the per-key `ApiKeyPermission` rows.

The contradiction this feature unrolls: `ApiKeyController.GetApiKeys` already filters by the caller's stable subject (`apiKeyService.GetApiKeysByOwnerSubject(stableSubject)`), so every row in the listing is, by construction, the caller's own key. The "Created By" cell was therefore redundant; in practice it also read "unknown" for any key created before the `ApiKeyOwnerReconciliationSeeder` reconciliation pass linked it to a `UserProfile`. The screen real estate is better spent on per-key scopes once RBAC is on — scopes are the only datum that varies per key on the caller's own row and the most operationally interesting one after the key name.

User-facing benefit: on an RBAC-enabled deployment, an operator opens **Settings → API Keys** and audits each key's `(Role, ScopeType, ScopeId)` rows at a glance without reopening the (write-once) Create dialog. On an RBAC-disabled deployment, the table simply gets leaner — one redundant column gone, no replacement.

---

## Wave-by-wave summary

### DISCUSS — implicit

No separate DISCUSS session was run. The four user stories (US-1 RBAC-off leaner list, US-2 RBAC-on per-key audit, US-3 ADR-004 zero-rows "Unrestricted" rendering, US-4 transitional deployment after RBAC flip) and the breaking-change classification were recorded directly in the feature delta. Rationale for skipping: one frontend conditional render + two backend DTO property edits + one `Dictionary` join. No new domain concept, no new endpoint, no new claim, no new role.

### DESIGN — not run

No new component, no new port, no infrastructure change, no migration, no feature flag. The DESIGN paragraph that would normally be produced lives inside the feature delta's "Inherited commitments" and "Scaffolds" sections: every module referenced by the change already exists.

### DISTILL — fast-tracked

The DISTILL session produced two `.feature` files (`walking-skeleton.feature`, `milestone-1-column-replacement.feature`) totalling 9 scenarios. Walking-skeleton strategy: **B (Real local + fake costly, split FE / BE)** — Vitest + Testing Library for the column-swap rendering rule, `WebApplicationFactory<Program>` against EF-Core InMemory for the one `@real-io @adapter-integration` HTTP contract scenario (M1.6). No testcontainers needed.

DISTILL gate: **APPROVED** by all four reviewers — Eclipse (PO), Morgan (Architect), Forge (Platform), Sentinel (Acceptance). Forge initially returned `conditionally_approved` on 1 critical (breaking-change classification missing from the delta) plus 2 high (`docs/settings/apikeys.md` scaffold + deprecated-field-read telemetry decision), all addressed in the revision cycle. Sentinel initially `conditionally_approved` on 1 high (M1.4 scenario title used the internal term "ApiKeyPermission row" instead of business language); revised to "scope row" and approved.

### DELIVER — 2 steps

| Step | Commit | What landed |
|---|---|---|
| 01-01 | `3de113d3` | Backend slice: drop `CreatedByUser` from `ApiKeyInfo` and `ApiKeyCreationResult`; add `Scopes: IReadOnlyList<ApiKeyScopeDto>` to `ApiKeyInfo`, populated once per `GetApiKeysByOwnerSubject` call from a single `apiKeyPermissionRepository.GetAll()` materialisation. M1.6 HTTP contract scenario + a unit test pinning the join. The `ApiKey` entity field `CreatedByUser` is left intact — the owner reconciliation seeder still reads it. |
| 01-02 | `8cb2d8e2` | Frontend slice: drop "Created By" header / body cell; add `rbac.isRbacEnabled`-gated "Scopes" column with chip rendering and lazy team / portfolio name resolution via `Promise.allSettled` (fallback to `Team #{id}` / `Portfolio #{id}`); type-level regression guard in `ApiKey.test-d.ts`; `docs/settings/apikeys.md` updated in the same commit. |

DES audit trail: `docs/feature/apikey-list-replace-createdby-with-scopes/deliver/execution-log.json`. Integrity verification reported clean — "All 2 steps have complete DES traces."

### DEVOPS — implicit

No infrastructure change, no migration, no new env var. The existing `ApiKeyPermissions` table (from `security-review-2026-05`) carries the data. The breaking-change UI / API note in the feature delta is the release-notes payload.

---

## Wave-decision reconciliation

This feature contradicts no prior wave decision. It is a UX cleanup made possible — and made more urgent — by three already-shipped features.

| Prior decision | Status under this feature |
|---|---|
| `security-review-2026-05/S-5` → ADR-004 (per-key scopes via `ApiKeyPermissions`) | **Compatible.** This feature surfaces those rows on the listing screen for the first time. Storage and creation paths unchanged. Zero-row keys render "Unrestricted" per ADR-004's backwards-compatible default. |
| `api-keys-for-all-users` (tab visible to every authenticated user) | **Compatible.** Confirms every listing row belongs to the caller — "Created By" is provably redundant on a per-user view. |
| `apikey-scope-ui-hidden-when-rbac-off` (Create-dialog accordion gated on `useRbac().isRbacEnabled`) | **Compatible and reused.** The new Scopes column uses the same gate, the same hook, the same loading / fetch-fail default (no Scopes column). Two surfaces (create + list) now share one RBAC source of truth. |

---

## Breaking-change classification (carried from DISTILL)

`GET /api/v1/apikeys` and `GET /api/latest/apikeys` response shape changed:

- `createdByUser` removed in place (no version bump, no deprecation window).
- `scopes` array added — purely additive.

Decision rationale recorded in the feature delta:

1. The removed field was already unreliable: it stored `User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown"` at creation time. For keys created before the `ApiKeyOwnerReconciliationSeeder` ran, the value was frequently `"unknown"` or an opaque `sub` identifier. No client could reasonably depend on it.
2. No public API consumer contract documents the field. `docs/api/` does not exist; `docs/settings/apikeys.md` is end-user UI documentation, not an API consumer reference. The only frontend consumer is `Lighthouse.Frontend` itself, which ships in lock-step with the backend.
3. No OpenAPI / JSON-Schema / typed-SDK contract is published from Lighthouse — consumers parse loosely.

Rollback story: two C# property edits and two TS interface edits. Revert is `git revert` against the two feature commits. The database schema is untouched — no data migration to undo.

Deprecated-field-read telemetry was deliberately not added — the field is removed in this release (no deprecation window during which old reads could be counted) and no public consumer depends on it. Logged as an accepted residual risk.

---

## Production change minimality

Two commits, both small and behaviour-isolated:

- Backend (`3de113d3`): two property edits on response DTOs + one `Dictionary` materialisation in `GetApiKeysByOwnerSubject`. The entity model, the controller signature, the migration set, and the seeder are all untouched.
- Frontend (`8cb2d8e2`): drop a `<TableCell>` and add a `useRbac()`-gated `<TableCell>` with a chip renderer. Reuses the existing `scopeDataLoaded` lazy-load pattern from the Create dialog for human-readable team / portfolio names. No new hook, no new service.

The test diff (and the `docs/settings/apikeys.md` edit, and the type-level regression guard) is the rest of the delivery. This is the same "smallest blast radius" pattern as the sister feature `apikey-scope-ui-hidden-when-rbac-off`: when the backend already does the right thing, the UI fix is a column swap.

---

## Trade-offs accepted

### Mutation testing PASS-WITH-CAVEATS rather than full per-feature target

- Backend `ApiKeyService.cs`: 60/76 mutants killed (78.9%). The 4-point gap from the 80% feature target is in legacy `CreatedByUser` resolution paths that this feature did NOT modify — adding tests purely to chase those mutants would test code unrelated to the change.
- Backend `ApiKeyInfo.cs`: 0/2 — both surviving mutants are equivalent mutants on default string initialisation. No real escape.
- Frontend `ApiKeysSettings.tsx`: ~140/307 killed (~46%), partial, deferred at the 12-minute host-time budget. Survivors are dominated by JSX presentation noise — heading text, ARIA label substrings, MUI prop toggles — not feature behaviour. Feature behaviour is port-to-port pinned by 23 Vitest cases across `F_FE_2_ApiKeyListScopesColumn.test.tsx` and `ApiKeysSettings.test.tsx`.

Recorded as a deliberate trade-off in the feature delta's "Quality gates" table. Matches the precedent set by sister feature `apikey-scope-ui-hidden-when-rbac-off` (mutation testing SKIPPED for a 3-line conditional render — see `docs/feature/apikey-scope-ui-hidden-when-rbac-off/feature-delta.md`'s "Quality gates" table).

### Team / portfolio name resolution is client-side, not joined in the controller

`ApiKeyScopeDto` exposes `ScopeId` (numeric foreign key). The Scopes cell resolves it to a display name on the frontend via `Promise.allSettled` against `TeamService.getTeams()` / `PortfolioService.getPortfolios()`, falling back to `Team #{id}` / `Portfolio #{id}` when the lookup is unavailable. This avoids a new backend join and keeps `GetApiKeysByOwnerSubject` cheap. The fallback path is exercised by M1.8.

### Three CI flakes on first run (re-passed in isolation)

- Backend: 3 parallel-execution flakes on the first `dotnet test` run — all passed on a clean re-run with no code changes. Logged as environmental, not a regression.
- Frontend: `DeliveryCreateModal` date tests flaky in the full suite — pass in isolation. Pre-existing flake, not introduced by this feature.

Recorded so a future reader does not chase the "3 failed" / "1 failing test" first-run output as a defect.

---

## Lessons captured for future features

1. **The fast-track DISTILL pattern continues to work for narrow-scope UI / DTO column swaps.** Two consecutive features (`apikey-scope-ui-hidden-when-rbac-off` on 2026-05-13, this feature on 2026-05-14) shipped with one DISTILL session each and no separate DISCUSS / DESIGN / DEVOPS waves. The criteria are now well-established: no new endpoint, no migration, no new third-party dependency, no new operational surface, no cross-team consumer impact. When all five hold, run a single DISTILL.
2. **Forge's "breaking-change classification" check at the DISTILL reviewer gate caught a real omission.** The initial delta described the DTO change but did not classify it as breaking, did not enumerate the rollback story, and did not decide on deprecated-field-read telemetry. All three were addressed in revision before DELIVER started. Future features that touch a response DTO shape should ship a breaking-change classification section preemptively.
3. **The type-level `satisfies` regression guard (option 1 from DISTILL's "Test-factory regression guard") is the right default.** Shipped as `ApiKey.test-d.ts`; sanity-checked by temp-reintroducing `createdByUser` (tsc -b failed as expected), then reverted. Zero runtime cost, no test-suite slowdown, the regression fires at build time. Reuse this pattern for future DTO-shape contracts.
4. **Mutation-testing PASS-WITH-CAVEATS is acceptable when the survivor analysis is honest.** The 4-point backend gap and the frontend ~46% kill rate were each accompanied by a survivor-class summary identifying *what* survived (legacy paths not modified by the feature; JSX presentation noise) and *why* chasing them would not improve real-world signal. A bare kill-rate number without survivor analysis would not justify the trade-off; this analysis does.
5. **Two-commit DELIVER (backend slice → frontend slice) keeps each commit reviewable.** Step 01-01 added `scopes` to the response and could be deployed alone (the frontend would tolerate the extra field). Step 01-02 dropped `createdByUser` from the frontend after the backend had stopped emitting it. The ordering rules out a "frontend reads a field the backend no longer sends" intermediate state.

---

## Links

- Feature workspace: `docs/feature/apikey-list-replace-createdby-with-scopes/`
- Feature delta SSOT: `docs/feature/apikey-list-replace-createdby-with-scopes/feature-delta.md`
- Acceptance scenarios:
  - `docs/feature/apikey-list-replace-createdby-with-scopes/acceptance/walking-skeleton.feature`
  - `docs/feature/apikey-list-replace-createdby-with-scopes/acceptance/milestone-1-column-replacement.feature`
- DELIVER commits: `3de113d3` (backend), `8cb2d8e2` (frontend)
- Sister feature evolution (precedent for fast-track and PASS-WITH-CAVEATS mutation gating): `docs/feature/apikey-scope-ui-hidden-when-rbac-off/feature-delta.md`
- Prior feature this one builds on: `docs/evolution/2026-05-11-api-keys-for-all-users.md`
- ADR governing the zero-rows "Unrestricted" rendering rule: `docs/product/architecture/adr-004-apikey-scope-storage.md`
