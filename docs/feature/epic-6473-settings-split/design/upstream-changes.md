# Upstream changes from DESIGN — epic-6473-settings-split

DESIGN, 2026-11-02 (revised after peer review and the maintainer's DESIGN answers MD1–MD4). No story is added,
removed or re-scoped. Rows 2, 5, 7, 8 and 9 change or add acceptance criteria or copy; the rest sharpen what DISCUSS
wrote, for DISTILL to carry into the scenarios. The slice files and the DISCUSS sections of `feature-delta.md` have
already been updated for rows 2, 5, 7 and 8 (labels, slugs, AC).

| # | Where (DISCUSS) | DISCUSS said | DESIGN found / decided | Effect on stories and AC |
|---|---|---|---|---|
| 1 | Slice 04 IN, "reusing `DataGridBase` ... `ReconnectBanner`, the delete dialog and the empty-state alert" | the list moves as an existing component | the connections section is inline JSX in `OverviewDashboard.tsx`; it is first extracted (refactor commit, behaviour unchanged) and then moved | none to AC; slice 04 effort about +0.25 day |
| 2 | Slice 04 IN, "The filter box stays on the Overview" | — | the Overview's "No connections found matching the filter" alert goes away with the list; **maintainer MD4: no text filter on System → Connections** | **AC change**: "holds the list ... unchanged except that it has no text filter" |
| 3 | Loading and failure looks, shell row | `useRbac()`'s existing failure handling | that handling is the permissive fallback, an architectural invariant (a failed access call never locks anyone out); confirmed, no new look | none |
| 4 | Navigation and URLs, "Keep the existing tab test ids" | keep them | kept where a tab survives; `configuration-tab` and `configuration-panel` retire in slice 02 with the Configuration tab; new ids listed in DD13 (`license-tab`, not `licence-tab`) | slice 02 POM split uses the new ids |
| 5 | M2, M3 labels "Organisation", "Licence" | British spelling | **maintainer MD1: American everywhere.** Labels "Organization" and "License"; slugs `/settings/organization/...`, `/settings/system/license`; test ids to match. Overrides M2/M3's spelling (names unchanged). #6474's Levels section uses the same "Organization" area name | **copy and AC change** in every story, scenario, sketch and slug that spells them (US-01–US-03, slices 01–03, journey, sketches, Navigation table) |
| 6 | Slice 02 technical notes | `BehaviourSettingsTable` reused with a filtered list | plus: the optional features become one keyed query shared by both tabs, and both tabs read the license through the shared license query, so `SystemSettingsTab.tsx` leaves the license-asker allow-list | none to AC; one enforcement-test edit |
| 7 | Slice 03 IN | `/settings/license` and `/settings/system/licence` open it | MD1: the tab is `/settings/system/license`; `/settings/license` redirects to it | **AC change**: slug `license` |
| 8 | Q7 (a) | keep one banner on the Overview that links to System → Connections | **maintainer MD2:** today's `ReconnectBanner` stays on the Overview unchanged (it fixes in place) and the same banner also shows on System → Connections; in slice 04 the Overview keeps its banners, so no warning is hidden between slices 04 and 05 | **slice 04 AC** adds: the Overview still shows a reconnect banner per connection that needs one; no link-only banner copy |
| 9 | Slice 02 AC | behaviour rows get loading and failure looks | the Calendar tab's Premium-gated controls also must not read an unknown license as "no Premium" | **slice 02 AC** adds: Calendar never shows Premium-required while the license is loading or unreadable |
| 10 | Navigation and URLs, E2E impact | specs listed | `RoleBasedAccessControl.spec.ts` (`goto("/connections/new")`, no-access alert) added; 11 connection-method call sites in 8 specs, not 8 | none to AC |
| 11 | Q8 | recommend (a) | **maintainer MD3: (a).** Pages under `/settings/system/connections/new` and `.../:id/edit`; old `/connections/...` redirect; Save and Cancel return to System → Connections | slice 05 IN updated; AC as written |
