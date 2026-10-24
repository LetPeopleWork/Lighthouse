# ADR-263: The Delivery dialog reads its sources from the page's shared query, and its Release list from a query per opening

**Status**: Proposed (DESIGN, 2026-10-24, PROPOSE mode; extended after the final-wave review with slice 04, decision 4).
Number taken while Epic 6400 runs in parallel; re-checked at push and renamed if taken.
**Feature**: `story-6398-jira-release-loads-once` (ADO Story #6398, slices 01, 02 and 04)
**Relies on**: the server-side `LicenseGuard` and `RbacGuard` on the options and preview routes (unchanged); the Jira
connector's 5-minute process-wide cache of a complete sweep (unchanged); `useLicenseRestrictions()`, read only. The one
assumption made about that hook is that a **known** licence arrives as an object whose `canUsePremiumFeatures` is a
boolean. Anything else is read as "not known yet". ADR-259 (Epic 6400) adds a `status` to the hook but does not promise
that `licenseStatus` stays `null` while loading or failed, so nothing here depends on `null` in particular.

## Context

Opening Add or Edit Delivery costs three waits in series, none of which looks like loading. The dialog asks for the
Portfolio's delivery sources again although the Deliveries page already holds them, and turns both "loading" and
"failed" into `[]` (no Jira Release tab). It reads `licenseStatus?.canUsePremiumFeatures ?? false`, so an unknown
licence reads as "not premium". The Release list is fetched by the tab body when it mounts, through a `useEffect` and
a module-level `Map` cleared when the dialog closes, so the slowest call starts last. The dialog's two other fetches
behave the same way: the Manual tab's Feature list shows an empty grid on failure and the previous opening's list on
reopen, and the Rule-Based schema has no late-answer guard and, after a failure, asks again without end.

CLAUDE.md asks every view that fetches to have a loading and a failure look and to use keyed TanStack queries, so a
late answer for an old request never lands. The maintainer decided that every failure in this dialog is retried by
closing and reopening it, with no Try again button anywhere (MD-13, MD-23). The Release list starts while the dialog
is open, once the licence is known premium, also when the user stays on Manual (MD-7, MD-15, MD-27). This Story may
not touch the licence hook, create a licence query, or change `DataGridBase` (MD-12).

## Decision

1. **Sources: one shared keyed query**, `["deliverySources", portfolioId]`, behind a small hook used by the page's
   `useDeliveryManagement` and by the dialog. The dialog component is mounted with the page whenever the page is
   editable (`canEdit`), and then stays mounted while closed, so both observers share
   one request per page visit; opening the dialog sends none. Options: stale time 0 and gc time 0 (one page visit, one
   answer), no retry, no refetch on focus or reconnect. The page keeps `data ?? []` for the grid. On failure the dialog
   shows a notice asking to reopen it; a Try again button was designed first and dropped by the maintainer before any
   scenario was written.
   **Reopening after a failure asks again; after a good answer it asks nothing.** This uses one mechanism. The
   observer sits in the dialog component's own body, which never remounts on reopen (MUI's `Dialog` mounts only its
   children while open), so the dialog asks for itself. An effect on `open` keeps a ref of the last opening it
   handled. When the dialog is open on an opening it has not handled, it records that opening, and only if the shared
   query's status is `error` it calls `refetch()` once. A refetch that fails again does not re-trigger it, so nothing
   loops; a `pending` or `success` query is left alone. `refetchOnMount` is not used.
2. **Release list: one keyed query per offered source and per opening of the dialog**, owned by the dialog, key
   `["deliverySourceOptions", portfolioId, sourceKey, opening]`. Enabled while the dialog is open, the sources offer
   that source, and the licence is known premium. `opening` is incremented when the dialog closes, so each opening reads
   a fresh key; with gc time 0 the closed opening's query is collected once nothing observes it. Options as the existing
   `useFeatureChildItems`: stale 0, gc 0, no retry, no refetch on focus or reconnect. The tab receives the query state
   as a prop. The module `Map` and its clear function are removed.
3. **The licence is read as three answers** (`unknown`, `premium`, `notPremium`) from the unchanged hook: `premium` or
   `notPremium` only for an object with a boolean `canUsePremiumFeatures`, `unknown` for anything else. The premium
   notice, lock and tooltip apply only on `notPremium`.
4. **Slice 04: the Manual Feature list and the Rule-Based schema are keyed queries per Portfolio and opening**, sharing
   the same `opening` counter and the options of decision 2.
   - The Feature list is keyed `["deliveryDialogFeatures", portfolioId, opening]` and enabled while the dialog is open
     and the Portfolio has Features; an empty Portfolio is an explicit ready, empty list with no request.
   - The rule schema is keyed `["deliveryRuleSchema", portfolioId, opening]`. It is enabled by a per-opening latch
     **derived from state**: one effect sets it whenever the dialog is open, the licence is known premium and the active
     tab is Rule-Based, however the tab became active (a click, the hydration of an edited Rule-Based Delivery, or a
     licence that turns premium after Rule-Based was chosen). Once set it holds for the opening and is reset on close,
     so the schema is asked once per opening however often the tab is switched. A latch set by the click alone was
     rejected: editing a Rule-Based Delivery selects the tab without a click and would never ask.
   - Looks (MD-29, MD-30). The Feature list loads under the grid's own overlay, through `DataGridBase`'s existing
     `loading` prop. That file is untouched; `FeatureSelector` and `FeatureGrid` gain an optional prop that forwards
     it. A failed Feature list shows an amber warning in place of the grid. The schema keeps today's `LoadingSpinner`
     and `SchemaLoadError`.
   - Save is disabled while the active tab's body is not ready.
   - The preview of a picked Release keeps its existing ref guard against out-of-order answers, the one hand-written
     guard left. It cannot outlive an opening, because the tab unmounts with the `Dialog`'s children, and an existing
     test pins it.
5. **Every body derives its status from data, not from `isFetching` alone.** `loading` while a query is fetching or has
   neither data nor an error, which includes a query not enabled yet; `error` when it failed and is not fetching;
   `ready` only with data. An empty Portfolio's Feature list is an explicit `ready` branch, since its query never runs.
   The existing `queryStatusOf` of the metrics widgets is not used, because it reads a query that has not started as
   `ready`. Only the `WidgetStatus` type is shared.

## Alternatives considered

- **Thread the sources down as props** (`PortfolioDeliveryView` → `DeliveryModals` → dialog, with a status and a retry
  callback): works without TanStack, but keeps two copies of one state and needs a callback back into the page hook.
  Rejected.
- **Lift the existing `useEffect` and `Map` into the dialog**, and fix the Feature-list and schema effects in place:
  smallest diff, but it hand-writes keying, de-duplication, late-answer dropping and "once per opening" three times,
  the bug class the keyed-queries rule exists to remove. Rejected.
- **`prefetchQuery` on open and `useQuery` in the tab body**: two places spell the key, and with gc time 0 a prefetched
  result that nothing observes yet is collected before the tab mounts. Rejected.
- **One key per Portfolio and `removeQueries` on close**: correct, but imperative and easy to forget on a new close
  path. Kept as the fallback.
- **`refetchOnMount` as a function that answers true on `error`**: it only works if the sources observer moves into the
  `Dialog`'s children, where every opening is a new mount; that placement is not chosen. Rejected.
- **Enable the schema with the tab instead of a latch**: a query without data is stale, so each switch back to
  Rule-Based would ask again. Rejected.
- **Set the latch in the tab's click handler**: misses every way the tab becomes active without a click. Rejected.
- **Reuse `queryStatusOf`**: one function fewer, but a disabled query would read as `ready` and show an empty grid or
  the schema error before any request. Rejected.
- **The App default options** (stale 5 min, retry 2): a failed answer would be served on reopen, against MD-23, and a
  failing call would be made three times before the failure look. Rejected.

## Consequences

**Positive**
- The sources wait disappears whenever the page has answered; the Release list starts as soon as the licence is known
  premium, whichever tab is on screen.
- A late answer for a closed opening or another Portfolio cannot reach the picker, the Feature grid or the rule form; a
  failed answer is never shown on reopen; reopening within five minutes is answered from the backend cache.
- No guess is shown: `[]` and `null` stop standing in for answers, and an empty grid always means "no Features".
- The schema's endless re-ask after a failure is gone.

**Negative / accepted**
- **Every opening by a premium user of a Portfolio that offers a source starts one Release-list request**, also when
  the user stays on Manual (MD-27). With a cold backend cache that is a full sweep of every visible project on the
  Jira connection; the 5-minute cache absorbs reopens and other users of the same connection within that window.
- Every test that renders the dialog, the tab or the page hook needs a `QueryClientProvider`; the fixture and about
  fourteen suites change once.
- The list request is not aborted on close (the service takes no signal); the server finishes the sweep and caches it.
  A close and reopen while that sweep is still running starts a second full sweep, because the connector stores only on
  completion and has no single-flight. Today's tab behaves the same; the sweep log line makes it visible, and whether
  to add single-flight is decided with the cache follow-up.
- Edit mode keeps Save disabled until the sources, and in slice 04 the Feature list or schema, have answered.
- If `canEdit` becomes true after the sources settle, the dialog's late mount refetches them once.
- `FeatureSelector` and `FeatureGrid`, shared components, gain one optional prop (default off). The Manual grid relies
  on `DataGridBase`'s `loading` prop keeping its name and meaning; Epic 6400 extends that file, so the prop is
  re-checked at merge or push.
- Until Epic 6400 slice 02 ships, a failure of any of the licence hook's three calls (`/license`, `/teams`,
  `/portfolios`) keeps the premium tabs on the loading look while the dialog is open (follow-up F1).

## Enforcement

- A source-tree test in the Deliveries folder: only the dialog's query hooks call `getDeliverySources`,
  `getDeliverySourceOptions`, `getFeaturesByIds` and `getRuleSchema`; no module-level cache of server answers; the
  preview's ref guard is the one named hand-written late-answer guard; no `getLicenseStatus` call and no `licenseStatus`
  query key.
- Call-count tests on the mocked services across open, close, reopen, tab switches and a Portfolio switch, including
  the sources pair (failed then reopened asks exactly once more; good then reopened asks nothing more) and "the schema
  is asked once per opening, failure included", plus the latch pairs (an edited Rule-Based Delivery asks once with no
  click; Rule-Based chosen while the licence is unknown asks only once it turns premium, and never on `notPremium`);
  a table test of the status rule and, per query, a test that its disabled state shows `loading` and never the failure
  look; late-answer tests with deferred promises; the E2E page object waits for
  `data-widget-status="ready"` on the source tab body.
