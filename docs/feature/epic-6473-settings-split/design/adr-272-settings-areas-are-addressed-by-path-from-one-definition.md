# ADR-272: Settings areas are addressed by path, from one definition, and old addresses redirect

**Status**: Proposed (2026-11-02, DESIGN of Epic #6473). Moves to `docs/product/architecture/` at finalize. Labels and
slugs are American (maintainer, 2026-11-02): "Organization", "License", `/settings/organization/...`,
`/settings/system/license`.
**Builds on**: [ADR-271](adr-271-ui-surfaces-follow-units-and-levels-and-settings-split-by-audience.md) Decision 2
and step U1.

## Context

ADR-271 splits the old System Settings page into three surfaces: Organization, Access and System. Today one page at
`/settings` picks its tab from `?tab=<key>`, accepts 14 keys, writes the tab back with `history.replaceState`, and
decides which tabs a person sees in render effects. Three problems follow from that shape:

- Links into settings are written by hand in about ten places (header, Task Manager, demo-data links, license
  links, connection pages, onboarding stepper). One of them, `/settings/license`, already points at no route.
- The tab is chosen before the person's access has loaded, so a deep link to an admin tab is bounced to the first
  tab.
- Runbooks, docs and bookmarks use the 14 keys, and the connection pages' `/connections/...` addresses, and must
  keep working.

Later Epics add to these surfaces: #6474 adds levels to Organization's Terminology tab and may rename it, #6349 adds
an Organization tab, U3 moves mapping to Organization.

## Decision

1. A setting's address is a path: `/settings/:area/:tab?`, with areas `organization`, `access` and `system` (the
   License tab is `/settings/system/license`). The connection pages live beneath their tab: `/settings/system/connections/new` and
   `/settings/system/connections/:id/edit`.
2. One data-only definition in the frontend's `models` layer holds the areas, their tabs (slug, label, test ids,
   visibility rule, earlier slugs), the old `?tab=` keys and the connection page paths. Every link, the header
   menu, the routes and the page objects' expectations take their paths from it. It sits in `models` so that the
   header (in `components`) may import it under the existing layer rules.
3. One pure function decides, from the requested address and the person's access, whether to show an area and
   tab, redirect, or wait. It never decides before access has loaded. A tab or area the person may not see falls
   back to the first one they may see.
4. Every old address redirects with `replace`, so Back does not loop: the 14 `?tab=` keys, bare `/settings`,
   `/settings/license`, `/connections/new`, `/connections/:id/edit`, and any earlier tab slug. A renamed tab keeps
   its old slug as an earlier slug, so the rename never breaks a link.
5. An automated check fails the build when a production module other than the definition and the settings route
   module contains a path that starts with `/settings` or `/connections`.

## Alternatives considered

- **Keep query tabs** (`/settings?area=system&tab=database`): fewer routes, but unlike Team and Portfolio pages, and
  the area cannot be a route the router ranks; rejected.
- **One `/settings/*` route parsed by hand**: one route, but a second matcher beside the router's; rejected.
- **Server-side redirects**: the visibility fallback needs the person's access, which only the app has, and the
  Standalone build serves the same app; rejected.
- **Paths written where they are used**: what we have today, and how `/settings/license` died; rejected.

## Consequences

- Adding a tab or area is a change to the definition plus its panel; renaming one is additive.
- Old links keep working indefinitely; the legacy key list only grows if a key is ever added, never shrinks.
- Page objects keep their method names; specs do not change when a tab moves between areas.
- A path typed by hand anywhere else fails the build, which is the point.
