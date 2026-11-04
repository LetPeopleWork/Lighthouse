# RCA: Bug #6533, Settings tab does not load for scoped admins

**Symptom:** with RBAC on, a Team Admin or Portfolio Admin who is not a System Admin sees the Settings tab, but it
stays blank with no error (`v26.11.1.2`). `main` shows a could-not-load state instead, since `2e00dde23`.

## Root cause chain

1. The form never renders: `useModifySettings` loads settings and connections in one `Promise.all`, so one rejection
   leaves `settings` null.
2. The connections request returns 403: `TeamDetail.tsx` and `PortfolioDetail.tsx` call
   `getConfiguredWorkTrackingSystems()`, and `GET /worktrackingsystemconnections` is guarded `SystemAdmin`.
3. The frontend has no other way to get the list: `/summary` exists for scoped admins, but nothing calls it.
4. ADR-006 specified both halves, locking the list and repointing the frontend. `c389e80be` shipped only the backend
   half, bundled with three other security stories.
5. That commit reversed `e0aef89bb`, the real fix made earlier the same day. Both first shipped in `v26.5.14.2`, so
   the fix never reached a release, while the docs written for it (`c9b39f2e2`) stayed.

## Why nothing caught it

- Backend tests pin the 403 as intended behaviour.
- Frontend tests mock the service, so they never see a 403.
- No test opens the Settings tab as a scoped admin against the real endpoint.
- ADR-006's claim that the form reads only three fields went stale as the form grew.

## Affected

Every release with RBAC, `v26.5.14.2` to `v26.11.1.2`. RBAC itself first shipped in `v26.5.14.2`.
