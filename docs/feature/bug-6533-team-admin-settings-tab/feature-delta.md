# Bug #6533: Team Admins and Portfolio Admins cannot load the Settings tab when RBAC is on

## Wave: DELIVER / [REF] Scope

Bugfix flow (`/nw-bugfix`), root cause approved by the maintainer on 2026-11-04. See `rca.md`.

**Rigor:** `standard` for every step, agreed by the maintainer. Crafter on sonnet, reviewer on haiku.

**Context map:** touches the access and work-tracking connection areas only. No new concept, context, edge or
domain event, so ADR-270 and `ARCHITECTURE.md` §4 do not change.

**UI surface:** the Settings tab of a Team and of a Portfolio. No layout or copy change: scoped admins get the form
System Admins already see, so no sketch was needed.

**Loading and failure looks:** unchanged. `useModifySettings` already has `loading` and `loadFailed`.

**Usage data:** N/A, because this restores an existing screen and no new behaviour needs counting.

**Out of scope:** Bug #6534, the unguarded read of a single connection.

## Wave: DELIVER / [REF] Decisions

- Keep ADR-006's shape: the full list stays System Admin only, and `/summary` is what the settings forms read. A
  scoped projection on the full-list route was rejected, because ADR-006 rejected one route with two shapes.
- `/summary` grows by the non-secret fields the forms read: additional field definitions, defaults and supported
  defaults. It carries no options, usage or follower data.
- `getConfiguredWorkTrackingSystems()` stays on the full list. `OverviewDashboard` relies on its 403, and the
  connection pages need the full shape.
- The 403 is not caught and turned into an empty list: the form would load with no connection, and a save could
  write connection id 0.
- `AnyScopedAdmin` keeps its check order. A System Admin on a lapsed licence is refused `/summary`, but the Team and
  Portfolio read checks run the same gate first, so that person cannot reach the tab anyway. A reorder tried in step
  01-01 was reverted in 01-04 because it bought nothing.
- Loading the form makes a save reachable, so a Portfolio Admin's save keeps an Owning Team they cannot read. The read
  filter hides it, the form sends the null back, and without this the save would clear it.
- ADR-006 fixes the summary at three fields, so it gets a dated amendment at finalize, with the docs correction.
- The `@rbac` check may have its first real run in CI if the identity provider cannot be started locally, agreed by
  the maintainer on 2026-11-04.
