# Feature WIP change forecast refresh fix (ADO bug 5022)

Author date: 2026-05-17 | ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5022 | Symptom: changing Feature WIP does not refresh the displayed forecasts; user must navigate to another tab and back to see new dates. Reproduced from BOTH the portfolio detail page (quick-setting on a team within the portfolio) AND the team detail page (quick-setting on the team itself).

## Wave: DISCUSS / [REF] Bug summary

| Field | Value |
|---|---|
| Reported | 2026-05-17 by Benj Huser (ADO 5022) |
| Severity / Priority | 3 - Medium / 2 |
| Reproduction (portfolio) | On Portfolio detail / Features (or Deliveries) tab, change a team's Feature WIP via quick-setting. Team WIP persists; backend forecast refresh now succeeds (after ADO 5021 fix); UI feature dates stay stale until the user tabs away and back. |
| Reproduction (team) | On Team detail / Features tab, change Feature WIP via quick-setting. Same stale-display symptom on the team's own feature list. |
| Tags | `Release Notes` |
| Related | ADO 5021 (commit `e9d1bc96`, already on `origin/main`) — the 500 that previously blocked the portfolio forecast refresh from being queued at all. 5021 fixed = pre-requisite for the portfolio side of this bug to surface clearly. |
| Originally reported as | "portfolio-only ('works at least for teams')". User retested after raising 5021 and identified the team-side symptom as well — broader scope confirmed. |

## Wave: DISCUSS / [REF] Root cause

The bug has **two distinct mechanisms**, one per page, both leading to the same user-visible symptom.

### Cause A — Portfolio page: SignalR subscription churn

`Lighthouse.Frontend/src/pages/Portfolios/Detail/PortfolioDetail.tsx:58` declares the subscription guard as a plain `let`:

```tsx
const PortfolioDetail: React.FC = () => {
    ...
    let subscribedToUpdates = false;   // ← reset every render
    ...
    useEffect(() => {
        ...
        if (portfolio && !subscribedToUpdates) {
            subscribedToUpdates = true;
            setUpPortfolioUpdateSubscription();
        } else {
            fetchPortfolio();
        }
        return () => {
            updateSubscriptionService.unsubscribeFromFeatureUpdates(portfolioId);
            updateSubscriptionService.unsubscribeFromForecastUpdates(portfolioId);
        };
    }, [portfolio, portfolioId, fetchPortfolio, updateSubscriptionService, subscribedToUpdates]);
};
```

Two coupled defects:

1. **`let` does not persist state across renders.** `subscribedToUpdates` is re-initialised to `false` on every render. The intent ("subscribe once") fails — every render that re-runs the effect sees `subscribedToUpdates === false` and treats the connection as un-subscribed.

2. **`portfolio` is in the effect's dependency array.** Each successful `fetchPortfolio()` produces a fresh `Portfolio` instance via `plainToInstance(Portfolio, data)` (`models/Portfolio/Portfolio.ts:37`). React detects the new reference, runs the effect cleanup (sync `connection.off(updateKey)`, fire-and-forget `connection.invoke("UnsubscribeFromUpdate")`), then runs the new effect which calls `setUpPortfolioUpdateSubscription` again. `connection.invoke("SubscribeToUpdate")` is an async server roundtrip; the SignalR group-join is NOT instantaneous.

Combined effect — the WIP-change flow at `PortfolioDetail.tsx:258-282`:

```tsx
const updateTeamSettingsFromPortfolio = useCallback(
    async (teamId, updateFn, shouldRefreshForecasts = false) => {
        if (!portfolio) return;
        const settings = await teamService.getTeamSettings(teamId);
        updateFn(settings);
        await teamService.updateTeam(settings);
        await fetchPortfolio();          // ← (A) re-render → unsub/resub cycle begins
        if (shouldRefreshForecasts) {
            await portfolioService.refreshForecastsForPortfolio(portfolio.id);  // ← (B) queues bg job
        }
    },
    ...
);
```

After (A) the SignalR Forecasts handler is briefly detached. After (B) the backend dequeues and runs the forecast refresh; on completion it notifies group `Forecasts_{portfolioId}`. For small portfolios the bg job finishes in single-digit ms, so the notification lands during the resubscribe gap and is silently dropped. Tab-switch fixes the symptom because `PortfolioForecastView`/`PortfolioFeatureList` unmount and remount; on mount the feature list refetches from `featureService.getFeaturesByIds(...)` independently of SignalR.

### Cause B — Team page: no Forecasts subscription at all

`Lighthouse.Frontend/src/pages/Teams/Detail/TeamDetail.tsx:255-302` subscribes to **only** `Team` updates:

```tsx
await updateSubscriptionService.subscribeToTeamUpdates(
    teamId,
    handleTeamUpdate,
);
```

There is no `subscribeToFeatureUpdates` and no `subscribeToForecastUpdates` on this page. Meanwhile the team's WIP-change flow (`TeamDetail.tsx:221-244`):

```tsx
await teamService.updateTeam(settings);           // (1) persists team; backend does NOT trigger any forecast/team update
await fetchTeam();                                 // (2) refetches team — features still have OLD forecasts
if (shouldUpdatePortfolioForecasts) {
    await teamService.updateForecastsForTeamPortfolios(team.id);   // (3) queues Forecasts_{pId} for each portfolio this team is in
}
```

The backend `TeamController.UpdateTeam` (`Lighthouse.Backend/API/TeamController.cs:100-134`) just persists team settings; it does NOT call `teamUpdateService.TriggerUpdate(team.Id)`, so NO `Team_{teamId}` SignalR notification fires for a WIP change. The only notifications fired by the WIP flow are `Forecasts_{portfolioId}` for the portfolios the team is in — and the team page is not subscribed to any of those.

Result: the team page receives no signal to refetch after the backend forecast refresh completes. Tab-switch fixes the symptom for the same reason as the portfolio case (TeamFeatureList remounts and refetches via its own mount-effect).

Note: `TeamDetail.tsx:64` ALSO contains the same `let subscribedToUpdates = false;` antipattern as PortfolioDetail.tsx:58 — the structural defect is present on both files. On the team page it doesn't currently cause a SignalR-flap symptom because there's nothing time-critical subscribed; but any fix that adds a Forecasts/Features subscription to the team page MUST also fix the `let` defect at the same time, or it will reintroduce Cause A on the team page.

## Wave: DISCUSS / [REF] Fix-direction decision

| Option | Decision | Reasoning |
|---|---|---|
| A. Frontend-only fix: (i) replace `let subscribedToUpdates` with `useRef` on both `PortfolioDetail` and `TeamDetail`; (ii) split the subscription effect from the data-fetch effect so it depends only on the entity id + `updateSubscriptionService`; (iii) on `TeamDetail`, subscribe to `Forecasts` (and `Features`) updates for the team-level forecast group | **CHOSEN** | Single layer to change. Symmetry with how PortfolioDetail already covers both subscriptions. Test surface is contained in two Vitest files. Mirrors the project's existing SignalR notification model — no new backend SignalR groups required if a team-level `Forecasts_team_{teamId}` (or similar) is exposed. If the existing `Forecasts_{portfolioId}` is reused, the team page would need to subscribe per-portfolio for portfolios the team is in. Exact group key is a DELIVER-time decision; the regression contract test asserts that *some* Forecasts subscription is registered. |
| B. Backend-only fix: emit a `Team_{teamId}` notification after every `Forecasts_{portfolioId}` completion if the portfolio contains that team's features, so the team page's existing Team subscription naturally refreshes | Rejected | Couples portfolio-side work to team-side notifications; fan-out could be large for shared teams. Doesn't fix Cause A (the portfolio churn). Still useful as a complementary signal if needed later, but not the primary fix. |
| C. Mixed: backend emits an additional `Forecasts_team_{teamId}` notification AND frontend subscribes to it | Considered | Cleanest contractually (per-team SignalR group), but requires backend changes plus frontend changes plus a migration of the existing `update-portfolios-for-team` endpoint. Heavier than A. Use this only if A proves insufficient. |
| D. Poll forecast status after refresh-trigger until "Completed" then refetch | Rejected | Mask, not a fix. Fights against the existing SignalR infrastructure. |

## Wave: DISCUSS / [REF] Post-fix contract

When the user changes Feature WIP from EITHER page (Portfolio quick-setting OR Team quick-setting), with the relevant `shouldRefreshForecasts` / `shouldUpdatePortfolioForecasts` flag set:

1. The new WIP value is persisted server-side. (Unchanged — already worked.)
2. The backend queues forecast refresh job(s). (Works after ADO 5021.)
3. The relevant SignalR subscription remains alive across the whole flow — no unsubscribe/resubscribe cycle is triggered by intermediate `setPortfolio` / `setTeam` calls.
4. The Team page subscribes to a forecast-relevant SignalR notification stream at mount, so it can react to backend completion.
5. When the bg job completes (sub-second typical), the subscription handler calls `fetchPortfolio` / `fetchTeam`, the feature list re-fetches via `featureService.getFeaturesByIds(...)`, and updated forecast dates render without the user needing to switch tabs.
6. On unmount, the SignalR subscriptions are released exactly once.

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| ADO 5021 fix (commit `e9d1bc96`) | `POST /api/latest/forecast/update/{id}` no longer 500s; portfolio bg forecast refresh is queued and runs | n/a | Pre-requisite for the portfolio side of this bug. Without 5021 the portfolio SignalR completion would never fire. |
| `models/Portfolio/Portfolio.ts:37` and team equivalent | Entity classes are rehydrated via `plainToInstance(...)` — fresh instance per fetch | n/a | Don't try to fix the bug by making `Portfolio`/`Team` reference-stable. Fix subscription effects to not depend on entity reference. |
| `UpdateSubscriptionService.subscribeToUpdate` (`UpdateSubscriptionService.ts:179-194`) | Subscription registers `connection.on` synchronously but the server `SubscribeToUpdate` group-join is an async roundtrip | n/a | Minimise subscribe/unsubscribe churn; don't depend on instant group-join. |
| `TeamController.UpdateTeam` (`Lighthouse.Backend/API/TeamController.cs:100-134`) | Persists team settings without triggering any background update | n/a | Do NOT change this to trigger a Team SignalR notification — that's option B (rejected). The fix lives on the frontend (subscription wiring) plus optionally a team-level forecast notification group on the backend. |
| Existing PortfolioDetail subscription behaviour (`subscribeToFeatureUpdates` + `subscribeToForecastUpdates` for the portfolio id) | Already correct in concept; only the lifecycle wrapper is broken | n/a | Don't remove subscriptions on the portfolio side; just fix the wrapper. |

## Wave: DISTILL / [REF] Scenario list with tags

All three regression tests below are RED today against current code; they pin the post-fix contract.

| # | Scenario | Tags | Today |
|---|----------|------|-------|
| 1 | `PortfolioDetail` subscribes to `Forecasts` updates exactly once per mount, even after a SignalR-triggered `fetchPortfolio()` produces a fresh `Portfolio` instance | `@regression @react-lifecycle @signalr @bug-5022 @portfolio` | RED — subscribe called 2 times |
| 2 | `TeamDetail` subscribes to `Team` updates exactly once per mount, even after a SignalR-triggered `fetchTeam()` produces a fresh `Team` instance | `@regression @react-lifecycle @signalr @bug-5022 @team` | RED — subscribe called 2 times (same `let subscribedToUpdates` defect at `TeamDetail.tsx:64`) |
| 3 | `TeamDetail` subscribes to `Forecasts` updates on mount, so portfolio forecast-refresh completion (triggered by a WIP change) can drive a team feature refetch without requiring tab navigation | `@regression @signalr @contract-gap @bug-5022 @team` | RED — `subscribeToForecastUpdates` is never called on the team page |
| 4 | (existing) `PortfolioDetail` subscribe-to-feature-and-forecast-on-mount (`PortfolioDetail.test.tsx:235`) | `@regression @react-lifecycle @signalr` | GREEN — backwards-compat guard, stays GREEN. |
| 5 | (existing) `TeamDetail` settings-tab deferral tests (`TeamDetail.test.tsx:457-528`) | `@regression @react-lifecycle @signalr` | GREEN — backwards-compat guard, stays GREEN. |

Driving port for scenarios 1-3: the React component itself rendered through `MemoryRouter`/`BrowserRouter` with mocked `IPortfolioService` / `ITeamService` / `IUpdateSubscriptionService`. Test type: Vitest + React Testing Library.

Test design notes:
- Scenarios 1 and 2 both deliberately use `getPortfolio` / `getTeam` `mockImplementation(async () => buildFresh())` rather than `mockResolvedValue(sameInstance)`. The existing `mockResolvedValue(mockTeam)` / `mockResolvedValue(portfolio)` returns the SAME reference on every call, which makes `useState`'s `Object.is` short-circuit the re-render and hides the lifecycle bug. The "fresh instance" mock mirrors production's `plainToInstance(...)` behaviour. This rationale is documented in the test files inline.
- Scenario 3 is somewhat prescriptive — it asserts that `subscribeToForecastUpdates` is called. The fix's exact mechanism is open (subscribe per-portfolio, or to a new team-level forecast group), but any reasonable fix to the user-observable contract will register a Forecasts subscription on mount. If DELIVER chooses option C (backend team-level forecast group + frontend subscription to it), the test stays meaningful with the same assertion.

## Wave: DISTILL / [REF] WS strategy

**Strategy C (real local).** Bug fix, no walking skeleton. Frontend-only Vitest tests with mocked driven adapters (`IPortfolioService`, `ITeamService`, `IUpdateSubscriptionService`) since the bug lives in React lifecycle / effect-cleanup behaviour and in subscription registration — real SignalR or real HTTP would obscure the test signal. The `PortfolioDetail` / `TeamDetail` components, their `useEffect`s, and `setPortfolio` / `setTeam`-driven re-renders are the real-under-test units.

## Wave: DISTILL / [REF] Adapter coverage table

| Adapter / boundary | `@real-io` scenario | Covered by |
|---|---|---|
| React `useEffect` cleanup/re-run on dependency change (`PortfolioDetail`) | YES | Scenario 1 |
| React `useEffect` cleanup/re-run on dependency change (`TeamDetail`) | YES | Scenario 2 |
| `useState` + `setPortfolio` / `setTeam` re-render trigger via `Object.is` | YES | Scenarios 1, 2 (fresh-instance mocks prove the re-render actually fires) |
| `IUpdateSubscriptionService.subscribeToForecastUpdates` call surface (portfolio) | YES | Scenario 1 (mocked but call-count asserted) |
| `IUpdateSubscriptionService.subscribeToTeamUpdates` call surface (team) | YES | Scenario 2 |
| `IUpdateSubscriptionService.subscribeToForecastUpdates` call surface (team) — contract gap | YES | Scenario 3 |
| SignalR transport (`@microsoft/signalr` `connection.on`/`invoke`) | NOT IN SCOPE | The transport itself is not under test; the bugs are in the consumers of the subscription API, not in SignalR. Real-transport E2E would belong to Playwright, out of scope here. |

No driven adapter relevant to either bug cause is missing real-lifecycle coverage.

## Wave: DISTILL / [REF] Scaffolds

None. Bug fix against existing production code in `PortfolioDetail.tsx:58, 308-376` and `TeamDetail.tsx:64, 255-302`. Mandate 7 (RED-ready scaffolds for unimplemented production modules) does not apply.

## Wave: DISTILL / [REF] Test placement

- `Lighthouse.Frontend/src/pages/Portfolios/Detail/PortfolioDetail.test.tsx` — nested describe `Forecasts subscription lifecycle (bug 5022)` with one `it()` for Scenario 1, inside the existing top-level `describe("PortfolioDetail component", ...)`, right after the existing `"should subscribe to feature and forecast updates on mount"` test.
- `Lighthouse.Frontend/src/pages/Teams/Detail/TeamDetail.test.tsx` — new nested describe `Forecast refresh subscription contract (bug 5022)` at the end of the existing top-level `describe("TeamDetail component", ...)`, containing Scenarios 2 and 3. Mirrors the `buildSettingsTabContext`/`buildTeamPageContext` factory pattern already used in the same file.

## Wave: DISTILL / [REF] Driving Adapter coverage

| Driving adapter | Scenario | Protocol |
|---|---|---|
| React component render (`<PortfolioDetail />`) via Vitest + RTL | 1 | `render()` against `MemoryRouter` + `ApiServiceContext.Provider` |
| React component render (`<TeamDetail />`) via Vitest + RTL | 2, 3 | `render()` against `BrowserRouter` + `ApiServiceContext.Provider` |
| Simulated SignalR notification through the captured subscription callback (Features for portfolio scenario 1; Team for team scenario 2) | 1, 2 | Direct invocation inside `act(async () => ...)` |
| Subscription-registration observation (no notification needed) | 3 | Assertion against `mockSubscribeToForecastUpdates.toHaveBeenCalled()` |

## Wave: DISTILL / [REF] Pre-requisites

- ADO 5021 must remain fixed in main (already on `origin/main` as commit `e9d1bc96`). If 5021 regresses, the portfolio side of this fix becomes meaningless — the bg forecast job is never queued and no completion notification fires.
- Test infrastructure: existing Vitest + RTL + `createMockApiServiceContext`. No new helpers.
- No backend test surface required for the chosen fix direction (option A). If DELIVER chooses option C (a new team-level forecast notification group), backend NUnit coverage will be added at that point and this section updated.
- Frontend build gate (`pnpm build` zero warnings) and `pnpm test` (Vitest) must pass post-fix.
