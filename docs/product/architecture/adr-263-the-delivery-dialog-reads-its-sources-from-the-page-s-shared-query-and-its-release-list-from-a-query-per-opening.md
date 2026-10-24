# ADR-263: The Delivery dialog reads its sources from the page's shared query, and its Release list from a query per opening

**Status**: Proposed (DESIGN, 2026-10-24, PROPOSE mode). Number taken while Epic 6400 runs in parallel; re-checked at
push and renamed if taken.
**Feature**: `story-6398-jira-release-loads-once` (ADO Story #6398, slices 01 and 02)
**Relies on**: the server-side `LicenseGuard` and `RbacGuard` on the options and preview routes (unchanged); the Jira
connector's 5-minute process-wide cache of a complete sweep (unchanged); ADR-259 (Epic 6400's licence hook keeps
`licenseStatus: null` while loading or failed).

## Context

Opening Add or Edit Delivery costs three waits in series, none of which looks like loading. The dialog asks for the
Portfolio's delivery sources again although the Deliveries page already holds them, and turns both "loading" and
"failed" into `[]` (no Jira Release tab). It reads `licenseStatus?.canUsePremiumFeatures ?? false`, so an unknown
licence reads as "not premium". The Release list is fetched by the tab body when it mounts, through a `useEffect` and
a module-level `Map` cleared when the dialog closes, so the slowest call starts last.

CLAUDE.md asks every view that fetches to have a loading and a failure look and to use keyed TanStack queries, so a
late answer for an old request never lands. The maintainer decided that a failed Release list is retried by closing
and reopening the dialog (MD-13), that the list starts while the dialog is open and only once the licence is known
premium (MD-7, MD-15), and that this Story may not touch the licence hook or create a licence query (MD-12).

## Decision

1. **Sources: one shared keyed query**, `["deliverySources", portfolioId]`, behind a small hook used by the page's
   `useDeliveryManagement` and by the dialog. The dialog component is mounted with the page, so both observers share
   one request per page visit; opening the dialog sends none. Options: stale time 0 and gc time 0 (one page visit, one
   answer), no retry (Try again is the retry), no refetch on focus or reconnect. The page keeps `data ?? []` for the
   grid; the dialog reads the status and offers Try again (`refetch`) on failure.
2. **Release list: one keyed query per offered source and per opening of the dialog**, owned by the dialog, key
   `["deliverySourceOptions", portfolioId, sourceKey, opening]`. Enabled while the dialog is open, the sources offer
   that source, and the licence is known premium. `opening` is incremented when the dialog closes, so each opening reads
   a fresh key; with gc time 0 the closed opening's query is collected once nothing observes it. Options as the existing
   `useFeatureChildItems`: stale 0, gc 0, no retry, no refetch on focus or reconnect. The tab receives the query state
   as a prop. The module `Map` and its clear function are removed.
3. **The licence is read as three answers** (`unknown`, `premium`, `notPremium`) from the unchanged hook; `null` is
   `unknown`. The premium notice, lock and tooltip apply only on `notPremium`.

## Alternatives considered

- **Thread the sources down as props** (`PortfolioDeliveryView` → `DeliveryModals` → dialog, with a status and a retry
  callback): works without TanStack, but keeps two copies of one state and needs a callback back into the page hook for
  Try again. Rejected.
- **Lift the existing `useEffect` and `Map` into the dialog**: smallest diff, but re-implements keying, de-duplication
  and late-answer dropping by hand, the bug class the keyed-queries rule exists to remove. Rejected.
- **`prefetchQuery` on open and `useQuery` in the tab body**: two places spell the key, and with gc time 0 a prefetched
  result that nothing observes yet is collected before the tab mounts. Rejected.
- **One key per Portfolio and `removeQueries` on close**: correct, but imperative and easy to forget on a new close
  path. Kept as the fallback.
- **The App default options** (stale 5 min, retry 2): a failed list would be served on reopen, against MD-13, and a
  failing call would be made three times before the failure look. Rejected.

## Consequences

**Positive**
- The sources wait disappears whenever the page has answered; the Release list starts as soon as the licence is known
  premium, whichever tab is on screen.
- A late answer for a closed opening or another Portfolio cannot reach the picker; a failed list is never shown on
  reopen; reopening within five minutes is answered from the backend cache.
- No guess is shown: `[]` and `null` stop standing in for answers.

**Negative / accepted**
- Every test that renders the dialog, the tab or the page hook needs a `QueryClientProvider`; the fixture and about
  fourteen suites change once.
- The list request is not aborted on close (the service takes no signal); the server finishes the sweep and caches it.
  A close and reopen while that sweep is still running starts a second full sweep, because the connector stores only on
  completion and has no single-flight. Today's tab behaves the same; the sweep log line makes it visible, and whether
  to add single-flight is decided with the cache follow-up.
- Edit mode keeps Save disabled until the sources have answered, so a bound Delivery is never saved from the Manual
  fallback.
- If `canEdit` becomes true after the sources settle, the dialog's late mount refetches them once.
- Until Epic 6400 slice 02 ships, a failed licence check keeps the loading look while the dialog is open (follow-up F1).

## Enforcement

- A source-tree test in the Deliveries folder: only the two hooks call `getDeliverySources` and
  `getDeliverySourceOptions`; no module-level cache of server answers; no `getLicenseStatus` call and no `licenseStatus`
  query key.
- Call-count tests on the mocked service across open, close, reopen and a Portfolio switch; a late-answer test with a
  deferred promise; the E2E page object waits for `data-widget-status="ready"` on the source tab body.
