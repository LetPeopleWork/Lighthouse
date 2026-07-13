# RCA: ADO #5480 — Blocked RAG is not properly detecting Rules for Blocked

**Method:** Toyota 5 Whys, multi-causal investigation with backward validation.
**Status:** Root cause identified and independently verified twice (two separate read-only investigation passes, matching file:line evidence). Not yet fixed — analysis only.

## 1. Problem Definition & Scope

**Symptom:** The Overview widget's Blocked-Items RAG status is stuck on the "no rule configured" message (RED) regardless of whether a blocked-item rule exists or how many items it flags, for teams/portfolios configured through the current rule-based editor. Teams configured via the legacy (pre-Epic-5074) blocked-states/blocked-tags mechanism are unaffected.

**In scope:** `Lighthouse.Frontend` Team/Portfolio Overview metrics views, the shared RAG computation (`ragRules.ts`), the settings DTOs and `BlockedItemService` on `Lighthouse.Backend`.

**Out of scope (checked and ruled out, see §3):** React Query caching, backend `IMemoryCache`/response caching, race conditions between save and refetch.

**Evidence gathered:** Two independent code-reading passes over the full call chain from persistence → API DTO → frontend fetch → RAG computation, both producing identical file:line citations.

## 2. Five Whys

### WHY 1 — Symptom: widget shows "Define blocked indicators in settings…" even though a rule exists and finds items

**Evidence:** `Lighthouse.Frontend/src/pages/Common/MetricsView/ragRules.ts:48-75`
```ts
export function computeBlockedOverviewRag(
    blockedCount: number,
    hasBlockedConfig: boolean,
    terms: RagTerms,
): RagResult {
    if (!hasBlockedConfig) {
        return { ragStatus: "red", tipText: `Define ${terms.blocked} indicators in settings to track blocked work.` };
    }
    if (blockedCount >= 2) { ... }   // "many blocked items" message
    if (blockedCount === 1) { ... } // amber
    return { ragStatus: "green", ... };
}
```
`hasBlockedConfig` is a hard gate evaluated **before** `blockedCount` is ever consulted. If it's `false`, no value of `blockedCount` (0, 1, or many) can produce anything but the "configure a rule" message. This single `if` fully explains why both "0 blocked items" and "many blocked items" cases collapsed onto the same wrong message.

### WHY 2 — Context: why is `hasBlockedConfig` false when a rule exists?

**Evidence:** `Lighthouse.Frontend/src/pages/Teams/Detail/TeamMetricsView.tsx:97-99`
```ts
setHasBlockedConfig(
    settings.blockedStates.length > 0 || settings.blockedTags.length > 0,
);
```
Identical logic at `Lighthouse.Frontend/src/pages/Portfolios/Detail/PortfolioMetricsView.tsx:45-47`. Neither reads `settings.blockedRuleSetJson` — the field the current rule editor actually writes to. `hasBlockedConfig` is computed solely from the two **legacy** array fields.

`grep -rn "hasBlockedConfig" Lighthouse.Frontend/src` confirms these are the only two `setHasBlockedConfig(...)` producer call sites in the entire frontend (full match list: `ragRules.ts:50,53,395,409` consume the parameter only; `BaseMetricsView.tsx:128,233,293,348,1063,1485` and `BaseMetricsView.test.tsx` pass/inject it only; `TeamMetricsView.tsx:41,137` and `PortfolioMetricsView.tsx:21,67` are the sole state-producer sites, at exactly the lines quoted above). Neither `TeamMetricsView.tsx` nor `PortfolioMetricsView.tsx` reference `blockedRuleSetJson` or `parseBlockedRuleSet` anywhere in either file.

### WHY 3 — System: why do `blockedStates`/`blockedTags` stay empty even though a rule was configured through the UI?

**Evidence:** `Lighthouse.Frontend/src/components/Common/BaseSettings/FlowMetricsConfigurationComponent.tsx:274-295` (`persistBlockedRuleSet`)
```ts
const persistBlockedRuleSet = (conditions, mode) => {
    if (conditions.length === 0) {
        onSettingsChange("blockedRuleSetJson" as keyof T, null);
        onSettingsChange("blockedTags" as keyof T, []);
        onSettingsChange("blockedStates" as keyof T, []);
    } else {
        onSettingsChange("blockedRuleSetJson" as keyof T, serializeBlockedRuleSet({ version, mode, conditions }));
        // blockedTags / blockedStates are NOT touched here
    }
};
```
When a rule has conditions (the "I added a rule" case), only `blockedRuleSetJson` is populated. `blockedTags`/`blockedStates` are only ever cleared, never populated, by the current editor. A brand-new team/portfolio starts with both legacy arrays empty (`CreateTeamWizard.tsx:45-46`, `CreatePortfolioWizard.tsx:46-47`), so a rule-set-only configuration permanently leaves `blockedStates.length === 0 && blockedTags.length === 0` — exactly the condition WHY 2's check treats as "not configured."

**Backend confirms this is a legitimate, intentional data shape, not a bug in persistence:**
- `Lighthouse.Backend/API/Helpers/TeamExtensions.cs:91` and `PortfolioExtensions.cs:56` round-trip `BlockedRuleSetJson` faithfully on save.
- `Lighthouse.Backend/API/DTO/SettingsOwnerDtoBase.cs:26-28` copies `BlockedStates`, `BlockedTags`, `BlockedRuleSetJson` verbatim from the entity.
- `Lighthouse.Backend/API/TeamController.cs:200-212` (`GetTeamSettings`) overwrites the DTO's `BlockedRuleSetJson` with `blockedItemService.GetEffectiveRuleSetJson(team)` — the **effective** rule set — but does **not** similarly normalize/synthesize `BlockedStates`/`BlockedTags`. `PortfolioController.cs:168` does the same for portfolios.
- `Lighthouse.Backend/Services/Implementation/WorkItems/BlockedItemService.cs:58-75`: `GetEffectiveRuleSetJson` always returns a serialized, non-empty JSON string (even `{"conditions":[]}` when nothing is configured) — this is a landmine for a naive fix: checking `blockedRuleSetJson` truthiness alone would not work; the fix must parse and check `conditions.length`.

### WHY 4 — Design: why does this diverge from the settings-editor page, which displays the rule correctly?

**Evidence:** `Lighthouse.Frontend/src/components/Common/BaseSettings/FlowMetricsConfigurationComponent.tsx:62-68`
```ts
const [isBlockedItemsEnabled, setIsBlockedItemsEnabled] = useState(
    () =>
        (parseBlockedRuleSet(settings.blockedRuleSetJson)?.conditions.length ?? 0) > 0 ||
        (settings.blockedTags && settings.blockedTags.length > 0) ||
        (settings.blockedStates && settings.blockedStates.length > 0),
);
```
This is the **correct** three-way OR, using `parseBlockedRuleSet` (`Lighthouse.Frontend/src/models/Common/BaseSettings.ts:32-46`) — a shared, schema-validated parser that already exists in the codebase. Confirmed null-safe (relevant to the fix in §6, since the input is `string | null | undefined`):
```ts
export function parseBlockedRuleSet(json: string | null | undefined): BlockedRuleSet | null {
    if (!json || json.trim() === "") {
        return null;
    }
    // JSON.parse wrapped in try/catch; schema.safeParse — never throws, returns null on any invalid input
}
``` The Overview widget's `hasBlockedConfig` in `TeamMetricsView.tsx`/`PortfolioMetricsView.tsx` is an **independent, divergent reimplementation** of the same "is a blocked rule configured" question that dropped the `blockedRuleSetJson` clause. There is no single shared source of truth for "is blocked-item detection configured" consumed by both the settings editor and the Overview widgets — the same predicate was written twice, once correctly and once incompletely.

### WHY 5 — Root Cause: why did this ship, and why does it only affect newly-configured teams?

**Evidence (design/process root cause):**
- Epic 5074 introduced `blockedRuleSetJson` as an *additive* alternate configuration path alongside the pre-existing `blockedStates`/`blockedTags` legacy fields (confirmed via migration `20260704064148_AddBlockedRuleSetJson.cs`), intentionally not migrating existing data. This is consistent with the project's "expand-only migrations" convention (`docs/ci-learnings.md` pattern; see project memory `feedback_expand_only_migrations`).
- When the new field was added, the "is blocked-config present" predicate was updated in the settings-editor component (`FlowMetricsConfigurationComponent.tsx:62-68`, the file that directly owns the rule editor) but was **not** propagated to the two other independent call sites that ask the same question (`TeamMetricsView.tsx`, `PortfolioMetricsView.tsx`) for the Overview widget's RAG gate. No shared exported helper (e.g., `hasBlockedItemsConfigured(settings)`) exists in `BaseSettings.ts` alongside `parseBlockedRuleSet` to force a single implementation.
- **Test evidence of the gap:** `Lighthouse.Frontend/src/pages/Common/MetricsView/ragRules.test.ts` unit-tests `computeBlockedOverviewRag` by passing `hasBlockedConfig` directly as a hardcoded boolean (lines 82, 88, 94, 100, 106) — this correctly tests the RAG function in isolation but can never catch that the *producer* of that boolean (`TeamMetricsView.tsx`/`PortfolioMetricsView.tsx`) computes it wrong, because the producer and the consumer are tested in isolation from each other with no integration test connecting "rule-set-only settings payload" → "resulting hasBlockedConfig" → "resulting RAG."
- **Why "existing team" didn't reproduce:** an existing team configured before/without using the new rule editor has genuinely non-empty `blockedStates`/`blockedTags` in the database (the legacy path was and remains the only path that ever populated those two fields for that team), so WHY-2's check evaluates `true` correctly for it — coincidentally, not because of any cache. Any **new** team, or any **existing** team whose rule was edited/replaced through the current UI (which writes only to `blockedRuleSetJson`), reproduces the bug deterministically, every time, on every reload.

**ROOT CAUSE:** `hasBlockedConfig` in `TeamMetricsView.tsx:97-99` and `PortfolioMetricsView.tsx:45-47` is a divergent, incomplete reimplementation of "is a blocked-item rule configured" that omits the `blockedRuleSetJson` rule-set path introduced by Epic 5074, because no single shared predicate was established as the source of truth when that field was added, and no test exercises the producer-to-consumer path end to end.

## 3. Ruled-Out Branches (multi-causal investigation, with evidence)

The reporter suspected caching. Investigated and ruled out with evidence:

- **Frontend caching:** `Lighthouse.Frontend/src/services/Api/TeamService.ts` `getTeamSettings` is a plain axios GET called directly inside a `useEffect` (`TeamMetricsView.tsx:89-113`, `PortfolioMetricsView.tsx:36-59`) on every mount — not wired through TanStack Query's `useQuery`/cache (`App.tsx:66-70`'s `QueryClient` is not used for this call). No `useMemo`, no manual memoization, no stale dependency array anywhere in this chain.
- **Backend caching:** No `IMemoryCache`, `[ResponseCache]`, or static dictionary found anywhere in `BlockedItemService.cs`, `TeamController.cs`, `TeamExtensions.cs`, `PortfolioController.cs`, `PortfolioExtensions.cs`.
- **Race condition between save and refetch:** Not applicable — the bug reproduces deterministically on every reload for affected teams, not intermittently, which a race would not produce.

**Conclusion:** there is no cache to invalidate. The "existing team is fine" observation is fully explained by WHY 5 (which fields happen to be populated for that team), not by any caching/staleness mechanism. The reporter's hypothesis is a reasonable but incorrect inference from a correlation that has a different, deterministic explanation.

## 4. Backward Chain Validation

Root cause → predicted symptom, checked against every reported observation:

| Reported observation | Predicted by root cause? |
|---|---|
| No rule configured → RED "configure" | Yes: `blockedStates`/`blockedTags` empty → `hasBlockedConfig=false` → gate fires. Correct behavior, not part of the bug. |
| Add rule via new editor, data reloads → still RED "configure" | Yes: rule write only populates `blockedRuleSetJson`; legacy arrays stay empty → `hasBlockedConfig` stays `false` → gate still fires regardless of refetch. |
| Rule finds 0 blocked items → expected GREEN, got RED "configure" | Yes: `computeBlockedOverviewRag`'s `!hasBlockedConfig` check short-circuits before `blockedCount` (here, 0) is ever examined. |
| Rule changed to find many items → expected RED "you have many, act now", got RED "configure" (wrong reason) | Yes: same short-circuit — `blockedCount >= 2` branch is unreachable while `hasBlockedConfig` is `false`, so the message text never changes to reflect item count regardless of how large it is. |
| Existing (pre-existing) team unaffected | Yes: only teams whose `blockedStates`/`blockedTags` are non-empty (i.e., legacy-configured) evaluate `hasBlockedConfig=true`; this is orthogonal to any cache and fully explained by which fields are populated for that specific team's history. |

All five reported observations are produced by the single root cause with no unexplained residue. No contradiction found between the "no rule" and "rule added" cases when read through this cause. Confidence: high — independently re-derived twice from raw file contents.

## 5. Contributing Factors

1. **No single source of truth for "is blocked detection configured."** The predicate is implemented independently in `FlowMetricsConfigurationComponent.tsx` (correct) and duplicated incompletely in two more places (`TeamMetricsView.tsx`, `PortfolioMetricsView.tsx`). `BaseSettings.ts` already exports `parseBlockedRuleSet` but not a combined "is configured" helper.
2. **Backend DTO asymmetry.** `GetTeamSettings`/`GetPortfolioSettings` synthesize an "effective" `BlockedRuleSetJson` but leave `BlockedStates`/`BlockedTags` as raw, un-normalized legacy values (`TeamController.cs:200-212`, `SettingsOwnerDtoBase.cs:26-28`) — this asymmetric contract makes it easy for a frontend consumer to reason about only one representation and miss the other.
3. **Test gap at the producer/consumer seam.** `ragRules.test.ts` unit-tests the RAG function with `hasBlockedConfig` injected directly; no test exercises `TeamMetricsView`/`PortfolioMetricsView` deriving `hasBlockedConfig` from a settings payload that has `blockedRuleSetJson` populated and legacy arrays empty — the exact shape a modern rule-only configuration produces.
4. **Additive-migration pattern without a corresponding "update all readers" checklist step.** The expand-only migration approach (by design, per project convention) means old and new representations coexist indefinitely; nothing in the current process forces every "is X configured" reader to be re-audited when a new representation of X is introduced.

## 6. Proposed Fix

**Immediate mitigation:** None required — no data corruption, no ongoing risk beyond the misleading UI message. This is a pure logic-correctness bug; the fix below is both the mitigation and the permanent fix.

**Permanent fix (root-cause-mapped):**

- Root cause (divergent incomplete predicate) → Add the missing `blockedRuleSetJson` clause to both producer sites:
  - `Lighthouse.Frontend/src/pages/Teams/Detail/TeamMetricsView.tsx:97-99`
  - `Lighthouse.Frontend/src/pages/Portfolios/Detail/PortfolioMetricsView.tsx:45-47`

  Change from:
  ```ts
  settings.blockedStates.length > 0 || settings.blockedTags.length > 0
  ```
  to the same three-way check already proven correct in `FlowMetricsConfigurationComponent.tsx:62-68`:
  ```ts
  (parseBlockedRuleSet(settings.blockedRuleSetJson)?.conditions.length ?? 0) > 0 ||
      settings.blockedTags.length > 0 ||
      settings.blockedStates.length > 0
  ```
  Note the truthiness landmine confirmed in WHY 3: `GetEffectiveRuleSetJson` always returns a non-empty JSON string, so `!!settings.blockedRuleSetJson` alone is insufficient — it must be parsed and `conditions.length` checked, exactly as shown.

- Contributing factor #1 (no shared predicate) → Extract a single exported helper, e.g. `hasBlockedItemsConfigured(settings: IBaseSettings): boolean`, in `Lighthouse.Frontend/src/models/Common/BaseSettings.ts` next to `parseBlockedRuleSet`, and have all three call sites (`FlowMetricsConfigurationComponent.tsx`, `TeamMetricsView.tsx`, `PortfolioMetricsView.tsx`) consume it. This removes the possibility of the same predicate diverging a third time.

- Contributing factor #3 (test gap) → Add unit tests to `TeamMetricsView.test.tsx` / `PortfolioMetricsView.test.tsx` asserting `hasBlockedConfig` resolves `true` when `getTeamSettings`/`getPortfolioSettings` returns a payload with populated `blockedRuleSetJson` conditions and empty legacy arrays — this is the regression guard for this exact bug shape, and should be written before the fix per the project's Outside-In TDD convention.

**Not required:** No backend change. The backend already returns correct raw `BlockedStates`/`BlockedTags` and a correctly-computed effective `BlockedRuleSetJson`; the defect is entirely in how the frontend derives a boolean from those three fields.

## 7. Files Affected

- `Lighthouse.Frontend/src/pages/Teams/Detail/TeamMetricsView.tsx` (fix, lines 97-99)
- `Lighthouse.Frontend/src/pages/Portfolios/Detail/PortfolioMetricsView.tsx` (fix, lines 45-47)
- `Lighthouse.Frontend/src/models/Common/BaseSettings.ts` (recommended: add shared helper near lines 28-46)
- `Lighthouse.Frontend/src/components/Common/BaseSettings/FlowMetricsConfigurationComponent.tsx` (recommended: refactor lines 62-68 to consume the shared helper for consistency)
- `Lighthouse.Frontend/src/pages/Teams/Detail/TeamMetricsView.test.tsx` (add regression test)
- `Lighthouse.Frontend/src/pages/Portfolios/Detail/PortfolioMetricsView.test.tsx` (add regression test)

No backend files require changes.

## 8. Risk Assessment of the Fix

- **Regression risk: low.** The change only widens an existing `||` chain with one additional true-producing clause; it cannot make any currently-`true` case become `false`. Legacy-configured teams (`blockedStates`/`blockedTags` populated, `blockedRuleSetJson` null) are unaffected — `parseBlockedRuleSet(null)` returns `null`, contributing `0`, and the existing OR clauses still carry them.
- **Edge case checked:** a team whose rule was explicitly cleared to zero conditions (`persistBlockedRuleSet`'s empty branch, `FlowMetricsConfigurationComponent.tsx:278-284`, which also clears the legacy arrays) correctly remains `hasBlockedConfig=false` after the fix — `conditions.length` is 0, and both legacy arrays are also empty.
- **Blast radius of the minimal fix (2 files):** contained to the RAG-gate boolean computation; does not touch `ragRules.ts`, backend, persistence, or migrations.
- **Blast radius of the recommended refactor (shared helper, 4 files):** larger surface, touches a component (`FlowMetricsConfigurationComponent.tsx`) that already has passing tests (`FlowMetricsConfigurationComponent.test.tsx`) — must confirm the refactor produces the identical boolean for all existing test cases before merging; per project convention (CLAUDE.md "shared contracts"), grep all usages of `parseBlockedRuleSet`/`isBlockedItemsEnabled` before extracting, which this analysis has already enumerated in full (§ verified evidence, item 9/11 grep lists).
- **No migration/persistence risk:** no schema or backend change involved.
- **Recommended sequencing:** ship the minimal 2-file fix + regression tests first (small, low-risk, directly closes the bug); treat the shared-helper extraction as a fast-follow refactor commit (per CLAUDE.md: "Refactor commits separate from feature commits"), since it's a design-hygiene improvement rather than something required to close ADO #5480.
