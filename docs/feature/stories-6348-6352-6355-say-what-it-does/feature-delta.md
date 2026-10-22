<!-- markdownlint-disable MD024 -->
# Feature Delta — stories-6348-6352-6355-say-what-it-does

> Three ADO User Stories from the **Next** column, batched for one light DISCUSS because each changes only
> copy or a hint on one existing control: #6348 (Closed Items Cutoff info tip), #6352 (Dependency and Parent
> Override info tips) and #6355 (Release picker explains disabled Releases and invites searching). All three
> come from one early adopter's feedback (large Jira Portfolios, 400 to 920 Features). No parent Epic. Each
> Story is its own slice and ships alone; batching them shares the reading, not the release.
>
> The other four Next stories (#6347, #6353, #6354, #6351) were ranked as more complex and are left for later
> DISCUSS rounds, in that order.

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ✓ `docs/product/jobs.yaml`: reused `job-config-admin-name-the-link-our-dependencies-live-in`,
  `job-config-admin-name-the-link-our-hierarchy-lives-in`, `job-config-admin-point-at-the-field-that-carries-dependencies`,
  `job-forecaster-bind-a-delivery-to-the-release-it-tracks`. Added one job (see JTBD).
- ✓ `docs/product/personas/config-admin.yaml`, `delivery-forecaster.yaml` (by reference).
- ⊘ DISCOVER / DIVERGE: none. Skipped as for every Story-sized copy change. The ADO descriptions quote the user
  directly and are the evidence.
- ✓ Code read: `pages/Common/AdvancedInputs/AdvancedInputs.tsx` (cutoff and Parent Override),
  `components/Common/ConnectionDefault/ConnectionDefaultFieldSelect.tsx`,
  `components/Common/ProjectSettings/Advanced/DependenciesComponent.tsx`,
  `components/Common/Connection/ModifyConnectionSettings.tsx` (connection-default entries),
  `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.tsx` (Release picker),
  `pages/Teams/Detail/Refinement/InfoTooltip.tsx`, `Services/Implementation/WorkItems/FetchFingerprint.cs`,
  `Services/Implementation/Reports/ReportWindowPolicy.cs`.
- ✓ Docs read: `docs/portfolios/edit.md` (Parent Override Field, Dependency Field), `docs/teams/edit.md`,
  `docs/portfolios/detail.md` (Following a Jira Release), `docs/concepts/worktrackingsystems/{jira,azuredevops}.md#dependencies`.

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

| Surface | Today | Gap |
|---|---|---|
| Closed Items Cutoff (days), Team and Portfolio settings (`AdvancedInputs.tsx:54`, helper text `:68`) | Number field with the helper "Number of days to retain closed/done items. Items older than this will be filtered out." | Nothing says what it limits. Documented only as "Cut Off Days" (`docs/teams/edit.md:143`, `docs/portfolios/edit.md:41`), under a heading that is not the UI label, and the Team page states a default of 180 days while the code seeds 365. |
| Parent Override Field (`AdvancedInputs.tsx:36`, connection defaults `ModifyConnectionSettings.tsx:162,173`) | Select with a "Connection default (…)" entry | Nothing says which way round a link phrase is read |
| Dependency Field (`DependenciesComponent.tsx:37`, connection default `ModifyConnectionSettings.tsx:175`) | Same select | Same; a field that matches nothing also silently hides the native links |
| Release picker (`DeliverySourceTab.tsx:207`, input at `:230`) | MUI Autocomplete; disabled rows carry the caption "No Release Date Set" | The caption says what, not why or what to do; nothing hints that typing filters |

The create wizards (`CreateTeamWizard.tsx`, `CreatePortfolioWizard.tsx`) do not show any of these fields: they seed
`doneItemsCutoffDays: 365` and leave the rest to the settings pages. So the surfaces are the Team and Portfolio
settings pages and the connection page only.

`InfoTooltip` (info icon + hover tooltip, the icon named by its text) is used in 4 places. `WidgetInfoButton`
(`pages/Common/MetricsView/WidgetShell.tsx:403`) is an info icon that opens a popover on click with a description
and a "Learn More" link; every dashboard widget uses it.

## Wave: DISCUSS / [REF] Persona IDs

- **config-admin**: sets up connections, Teams and Portfolios (#6348, #6352).
- **delivery-forecaster**: creates a Delivery that follows a Release (#6355).

## Wave: DISCUSS / [REF] JTBD One-Liners

- #6348 → **new** `job-config-admin-know-what-a-setting-limits-before-it-bites`: when I set how many days of
  finished work are kept, I want to read right there what that limits, so I choose it on purpose instead of
  finding out when a report refuses.
- #6352 → reused `job-config-admin-name-the-link-our-dependencies-live-in` and
  `job-config-admin-name-the-link-our-hierarchy-lives-in`: name the link my data lives in, the right way round,
  the first time.
- #6355 → reused `job-forecaster-bind-a-delivery-to-the-release-it-tracks`: point a Delivery at the Release it
  already tracks, among hundreds of Releases.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Why |
|---|---|---|
| D1 | One slice per Story; each ships and closes alone. | They share no code path and no dependency. |
| D2 | **Copy and hints only.** No validation, no new endpoint, no change to which Releases can be picked. | Story scope says so in each case; a Release without a date stays unpickable because the Delivery takes its date from it. |
| D3 | Reuse an existing info-icon look rather than invent one (#6352 settled by MD-5: the widget popover). | Avoids a second info-icon look. |
| D4 | Copy uses the configurable terms (`Work Items`/`Features`, `Team`/`Portfolio`, `Work Tracking System`) through the terminology hook, never "Epic" or "Story". | Terminology rule. |
| D5 | **#6348 docs: update the existing "Cut Off Days" sections** in `docs/teams/edit.md` and `docs/portfolios/edit.md` at finalization: retitle them to the UI label "Closed Items Cutoff", say what MD-1 says, and correct the Team default (the code seeds 365, the page says 180). | The setting is documented under a different name and with a wrong default. |
| D6 | **#6348 fact, verified in code:** the cutoff is in the fetch fingerprint (`FetchFingerprint.cs:42`), so raising it makes the next update a full fetch that brings older finished items back. Saving starts no update by itself. `0` keeps everything (`ReportWindowPolicy.EarliestStart`, `RetentionEdge`). The tip says both. | Answers the Story's open "check". |
| D7 | **#6352 direction copy is tracker-aware**: Jira's waiting end is *is blocked by* (not *blocks*); Azure DevOps' is the `-Reverse` end (`System.LinkTypes.Dependency-Reverse`, Predecessor). Parent Override on Jira names the phrase read from the Feature/Work Item towards its parent (e.g. *is caused by*). | A Jira-only example misleads an ADO admin, and direction is the whole point. |
| D8 | #6352 covers every place the two fields are chosen: connection defaults, Team settings (Parent Override), Portfolio settings (both). The create wizards do not show these fields. | Story scope: "wherever they are chosen". |
| D9 | #6355: the disabled entry says what to do; the input carries a search placeholder. Settled by MD-3. | UI sketch rule. |

### Maintainer decisions — 2026-10-22, before AFK (sketches reviewed)

| ID | Decision |
|---|---|
| MD-1 | **#6348: rewrite the existing always-visible helper line; no info icon.** Copy: "Finished {Work Items} older than this many days are not kept. This limits metrics, forecasts and how far back Reports can reach. 0 keeps them all. Raising it brings older ones back with the next update." Say "Reports", not "Then & Now report". Supersedes D3 for this slice and AC-1.1/AC-1.4's icon. |
| MD-2 | **#6352: info icon beside the select; the tooltip holds the direction copy plus a "Learn more" link to the docs, opening in a new tab.** The look is now the dashboard widgets' info popover (MD-5). |
| MD-3 | **#6355: inline caption on the row, no tooltip.** A dateless Release reads "No date in {tracker}: set one there to follow it"; the input's placeholder reads "Type to search {Releases}". "No longer available" unchanged. |
| MD-4 | All three Stories tagged **Release Notes** in ADO (done 2026-10-22). Standard auto-transitions apply. |
| MD-5 | **#6352: reuse the dashboard widgets' info popover instead of a hover tooltip** (decided 2026-10-22, after the review gate showed a link inside a hover tooltip cannot be reached by keyboard or announced by a screen reader). Same info icon beside the select; click or Enter opens a popover with the direction text and a "Learn More" link opening the docs in a new tab. Supersedes the hover part of MD-2. |

The MD-3 placeholder was sketched as "Type to search Releases" under a picker labelled "Release". The real label is
the source's name, "Jira Release", so the placeholder follows it: "Type to search Jira Releases". Recorded as an
autonomous reading of MD-3, for the maintainer to revisit.

## Wave: DISCUSS / [REF] Scope Assessment

PASS. 3 Stories, 1 bounded context each (settings UI; Delivery source UI), frontend only, no integration point,
each well under a day.

## Wave: DISCUSS / [REF] Story Map & Slices

Backbone: *configure a Team/Portfolio* → *configure a connection* → *create a Delivery*.

| Slice | Story | Brief | Est. |
|---|---|---|---|
| 01 | #6348 Closed Items Cutoff explains itself | `slices/slice-01-closed-items-cutoff-tip.md` | 2 h + docs |
| 02 | #6352 Dependency and Parent Override say which way round | `slices/slice-02-dependency-parent-override-tips.md` | 4 h + docs |
| 03 | #6355 Release picker says why and invites searching | `slices/slice-03-release-picker-why-and-search.md` | 2 h + docs |

### Priority Rationale

Simplest first, as asked: 01 is one string on one field, 02 spans four places and two trackers' wording, 03
touches the Premium Delivery form. No slice depends on another.

### Slice taste tests

- No slice ships 4+ components; none rests on a new abstraction (D3 reuses one).
- Each has a hypothesis that can fail (see briefs) and is checked against production-shaped data on the dev
  instance (`:5169`, real history; Jira Releases where the dev connection has them).
- Not scale variants of each other. A slice with only synthetic data: none.

## Wave: DISCUSS / [REF] User Stories

Definition of Ready items 3 and 4 (real-data examples, Given/When/Then): met by the DISTILL scenarios, which use
real-shaped data and renamed tenant terms (Tickets, Bets, Programs, Tracker); copy-only Stories carry no separate
Given/When/Then at DISCUSS.

### US-01 — Closed Items Cutoff says what it limits where it is set (slice 01, ADO #6348)

`job_id: job-config-admin-know-what-a-setting-limits-before-it-bites`

As a config-admin, I want the Closed Items Cutoff setting to tell me what it limits, so I set it on purpose.

#### Elevator Pitch
Before: the admin learns what Closed Items Cutoff does only when a Then & Now report refuses a window.
After: open a Team's (or Portfolio's) settings → sees, under **Closed Items Cutoff (days)**, "Finished Work Items
older than this many days are not kept. This limits metrics, forecasts and how far back Reports can reach. 0 keeps
them all. Raising it brings older ones back with the next update." (MD-1 copy; Features on a Portfolio, and the
tenant's own terms where renamed)
Decision enabled: which cutoff to set for how far back this Team or Portfolio needs to look.

#### Acceptance Criteria
- AC-1.1 The explanation is the always-visible helper line under the field on Team settings and Portfolio
  settings; no info icon (MD-1).
- AC-1.2 The line names what is not kept, what that limits (metrics, forecasts, how far back Reports can reach),
  what 0 means, and that raising it brings older items back with the next update.
- AC-1.3 The line uses the configured term (Work Items on a Team, Features on a Portfolio).
- AC-1.4 The line is the field's accessible description, so a screen reader reads it with the field.
- AC-1.5 The "Cut Off Days" sections in `docs/teams/edit.md` and `docs/portfolios/edit.md` are retitled
  "Closed Items Cutoff", say the same, and state the real Team default (365).

### US-02 — Dependency and Parent Override fields say what they read, and which way round (slice 02, ADO #6352)

`job_id: job-config-admin-name-the-link-our-dependencies-live-in`, `job-config-admin-name-the-link-our-hierarchy-lives-in`

As a config-admin, I want each of these fields to tell me which end of a link it reads, so I do not pick a field
that matches nothing.

#### Elevator Pitch
Before: an admin picks *Issue Links* as the Dependency Field, which matches nothing and hides Jira's *is blocked
by* links, with no hint why.
After: open a Jira connection's defaults (or a Portfolio's settings) → click the info icon beside **Dependency
Field** → sees a popover reading "Name the link phrase on the Feature that waits, such as *is blocked by*, not
*blocks*. Leave it on None to read Jira's Blocks links." and a "Learn More" link to the Dependency Field docs
(draft copy).
Decision enabled: which Additional Field, if any, to choose, and in which direction.

#### Acceptance Criteria
- AC-2.1 An info icon sits beside **Dependency Field** and **Parent Override Field** wherever they are chosen:
  connection defaults, Team settings (Parent Override), Portfolio settings (both). Click or Enter opens a popover
  with the tip; Escape or a click outside closes it (MD-5).
- AC-2.2 The Dependency tip says it reads the waiting end and gives the tracker's example: Jira *is blocked by*
  (not *blocks*); Azure DevOps the `-Reverse` end (Predecessor). It says None reads the tracker's native links,
  and that a chosen field replaces them rather than adding to them.
- AC-2.3 The Parent Override tip says it reads the link or field on the Feature/Work Item that points to its
  parent, with the Jira example *is caused by*, and that None uses the native parent.
- AC-2.4 Each tip's "Learn More" link leads to the matching docs section (`portfolios/edit.html#dependency-field`,
  `…#parent-override-field`, `teams/edit.html#parent-override-field`), in a new tab. These anchors exist today.
- AC-2.5 No validation or save behaviour changes.
- AC-2.6 The icon is a button named after its field (e.g. "About Dependency Field"), reachable by keyboard; the
  popover's link is reachable by keyboard once it is open.

### US-03 — The Release picker says why a Release cannot be picked and invites searching (slice 03, ADO #6355)

`job_id: job-forecaster-bind-a-delivery-to-the-release-it-tracks`

As a delivery-forecaster, I want a disabled Release to tell me why, and the picker to show it can be searched,
so a long list of Releases does not read as broken.

#### Elevator Pitch
Before: a Release without a date is greyed out with "No Release Date Set", which reads like a broken picker,
and nothing hints that typing filters hundreds of Releases.
After: Portfolio → Deliveries → **Add Delivery** → **Jira Release** tab → open the picker → sees the placeholder
"Type to search Jira Releases" and, on a Release with no date, "No date in Jira: set one there to follow it" (MD-3).
Decision enabled: set the date in Jira and come back, or search for the Release they actually meant.

#### Acceptance Criteria
- AC-3.1 An empty picker input shows a search placeholder naming the source (Release / the connection's term).
- AC-3.2 A Release without a date stays unpickable, and its entry says what to do, in MD-3's words (the "to follow it" carries the reason): "No
  date in {tracker}: set one there to follow it", the tracker's name, or the tenant's term for the work tracking
  system when the source names none.
- AC-3.3 "No longer available" (retired at source) keeps working as today.
- AC-3.4 `docs/portfolios/detail.md#following-a-jira-release` mentions both.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measure |
|---|---|---|
| K1 The reporter confirms each change answers his feedback | 3 of 3 Stories | Reply to the Release Notes / direct follow-up |
| K2 No new support question on these three settings | 0 in the 60 days after release | Slack / GitHub issues mentioning cutoff, dependency direction, or "can't pick Release" |

Usage-data event: left to DEVOPS to answer; a tooltip is not a usage signal worth an event, so "N/A" is expected.

## Wave: DISCUSS / [REF] Definition of Done

1. ACs green in Vitest. No new E2E; the existing suite runs in CI through the verify workflows, and the one POM
   locator near these controls (`BaseEditPage`, combobox "Parent Override Field") is run locally once after slice 02.
2. `pnpm test`, `pnpm build` (Biome included) clean.
3. Sonar: no new issues.
4. Docs updated per AC-1.5, AC-2.4, AC-3.4 at finalization.
5. Screenshots: checked at finalization against the `@screenshot` specs for the Team/Portfolio settings, the
   connection page and the Delivery create form; regenerated where one shows a changed control, otherwise "N/A,
   because no captured image shows it". Demo data: N/A, because no data shape changes.
6. Copy uses configurable terms.
7. Per-Story ADO transitions (Active on first commit, Resolved on push, Closed on green CI).
8. Each Story tagged Release Notes in ADO (done 2026-10-22, MD-4).
9. Mutation testing on touched frontend files ≥ 80%.

## Wave: DISCUSS / [REF] Out of Scope

- Validating a Dependency/Parent Override field that matches nothing (#6352 says tooltips only).
- Picking a Release without a date or overriding its date (#6355).
- Then & Now window defaults and one-click fix (#6347, separate Story).
- Listing who follows a connection default (#6353, separate Story).

## Wave: DISCUSS / [REF] WS Strategy, Driving Ports, Pre-requisites

- **WS strategy**: A (no walking skeleton; brownfield copy on existing controls).
- **Driving ports**: the settings UI (Team, Portfolio, connection, create wizards) and the Delivery create modal.
  No HTTP contract changes.
- **Pre-requisites**: none.

## Wave: DISCUSS / [REF] Standing Checklist

- **RBAC**: N/A, because no endpoint, guard or gated control changes; the tips sit on controls already gated by
  `useRbac()`.
- **Lighthouse-Clients (CLI + MCP)**: N/A, because no API payload changes (`blockedBecause` stays the same enum;
  the new copy is frontend-only). Checked: `lighthouse-clients` has no reference to `blockedBecause` or the cutoff.
- **Website**: N/A, because `website/src/pages/Lighthouse.tsx:915` only names "Follow a Jira Release" as a
  feature; none of the copy changed here appears on the website.

## Wave: DESIGN / [REF] Scope and Mode


Architecture summary (also posted to the maintainer in chat when DESIGN ended, and revised after the review gate):
everything is in the driving adapter, the React UI. Settings pages and the connection page reach
`FormSelectField` with a new optional `info`; that renders `InfoPopoverButton`, extracted from the dashboard
widgets' info button; the copy comes from one new module, `fieldDirectionTips.ts`. `AdvancedInputs` rewrites one
helper line; `DeliverySourceTab` gains a placeholder and a caption. API, services, Models, connectors and the
database are unchanged. Patterns weighed: open/closed through optional props (chosen), one home for the copy
(chosen), a per-tracker strategy hierarchy (rejected: a lookup function suffices), a backend tracker name on the
delivery source (rejected while one source exists).

Application scope (`nw-solution-architect` remit), propose mode, run in AFK; recommended option taken at each
point. Frontend only: no backend, no HTTP contract, no persistence, no ADR (no decision here outlives the copy it
carries). `docs/product/architecture/brief.md` unchanged: no component boundary moves. Outcome collision check
skipped: no new typed contract.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `WidgetInfoButton` | `pages/Common/MetricsView/WidgetShell.tsx:403` | Info icon → popover with text and a "Learn More" link | **EXTEND by extraction** into `components/Common/InfoPopoverButton/InfoPopoverButton.tsx` | MD-5. The popover is keyboard reachable. The widget keeps its status-guidance block by passing it as extra content, so widgets look exactly as today. The extracted button gains the accessible name the widget's button lacks today. |
| `InfoTooltip` | `pages/Teams/Detail/Refinement/InfoTooltip.tsx` | Info icon + hover tooltip | **NOT USED, unchanged** | MD-5 chose the popover; the Refinement uses stay as they are. |
| `FormSelectField` | `components/Common/FormSelectField/FormSelectField.tsx` | Renders every select involved, including the connection-default rows | **EXTEND** with optional `info` | One prop reaches the connection page (renders it directly) and every `ConnectionDefaultFieldSelect` use. |
| `ConnectionDefaultFieldSelect` | `components/Common/ConnectionDefault/ConnectionDefaultFieldSelect.tsx` | Parent Override + Dependency on Team/Portfolio settings | **EXTEND**: pass `info` through | The caller computes the tip, because only the caller knows the raw tracker (see DDD-3). |
| `AdvancedInputs` helper text | `pages/Common/AdvancedInputs/AdvancedInputs.tsx:68` | The cutoff's explanation | **EXTEND** (rewrite the string) | MD-1. |
| `blockedReason` map | `DeliveryGrid/DeliverySourceTab.tsx:59` | Disabled-Release copy | **EXTEND** into a function of the tracker word | MD-3. |

CREATE NEW: one copy module, `components/Common/ConnectionDefault/fieldDirectionTips.ts`. Justification: the
direction copy is per field × tracker and is needed by three callers (the connection page, `AdvancedInputs`,
`DependenciesComponent`); keeping it in one place stops them drifting.

## Wave: DESIGN / [REF] Decisions

| ID | Decision | Rejected |
|---|---|---|
| DDD-1 | Extract `WidgetInfoButton`'s icon + popover into `InfoPopoverButton({ label, text, learnMoreUrl?, children?, testId? })`. The `IconButton` carries `aria-label={label}` (the full accessible name: `FormSelectField` passes "About {field label}", a widget passes "About {widget title}", which gives the widget button the name it lacks today), `aria-expanded`, and `data-testid={testId}`; the popover holds `text`, any `children`, and a "Learn More" `Link` (`target="_blank"`, `rel="noopener noreferrer"`) when a URL is given. MUI's `Popover` moves focus into itself and closes on Escape or a click outside, so the link is reachable by keyboard. `WidgetShell` uses it with its guidance block as `children` and its existing `data-testid`. | Extending `InfoTooltip` with a link (a link in a hover tooltip cannot be reached by keyboard nor announced). A second, separate popover component (two looks for one idea). |
| DDD-2 | `FormSelectField` gets an optional `info?: { text: string; learnMoreUrl?: string }`. When set, the `FormControl` and an `InfoPopoverButton` labelled "About {label}" sit in one flex row (`alignItems: center`), the arrangement `RefinementBandSettings` uses. Unset renders exactly as today. | Icon inside the select's label or as an end adornment (the select's own arrow and label notch fight it). |
| DDD-3 | `fieldDirectionTips.ts` exports `directionTip(field, tracker, terms)`, where `field` is the connection-default name (`parentOverrideForTeams` \| `parentOverrideForPortfolios` \| `dependencyFieldForPortfolios`), `tracker` the raw `WorkTrackingSystemType \| undefined`, and `terms` the plural/singular words it needs; it returns `{ text, learnMoreUrl }`. The name picks both the copy and the per-owner docs link, and makes an impossible pair (a Team Dependency tip) unrepresentable; renaming a default key also touches the copy, which is accepted. Jira and AzureDevOps get their own example (Jira *is blocked by* not *blocks* / *is caused by*; ADO the `-Reverse` end, Predecessor; ADO Parent Override names a field whose value is the parent's id). Any other or unknown tracker gets the tracker-neutral sentence without an example. **Where the tracker comes from:** `AdvancedInputs` and `DependenciesComponent` read `connection?.workTrackingSystem ?? settings?.workTrackingSystem` (the same pair `trackerHoldingTheDefault` reads, but raw, so Linear and ServiceNow reach the neutral sentence) and pass the result as `info`; the connection page passes `selectedWorkTrackingSystem?.workTrackingSystem` when it builds the Parent Override and Dependency rows, read from a `tip` flag added to their `WhereEachDefaultSits` entries. | A `kind: "dependency" \| "parentOverride"` key (cannot pick the Team vs Portfolio docs link). Copy inline at each call site. Having `ConnectionDefaultFieldSelect` compute it (it only sees the tracker's display name, and only when that tracker holds the default). |
| DDD-4 | Docs links: Dependency → `portfolios/edit.html#dependency-field`; Parent Override on a Portfolio or the Portfolio connection default → `portfolios/edit.html#parent-override-field`; on a Team or the Team connection default → `teams/edit.html#parent-override-field`. All three anchors exist today, so the links work before any docs change. Absolute `https://docs.lighthouse.letpeople.work/…` constants, as `SecretHandlingNotice.tsx:8` does. | One generic docs link. |
| DDD-5 | #6348: the helper text becomes a function of the configured term: Team → Work Items term, Portfolio → Features term. `AdvancedInputs` tells the two apart through `parentOverrideDefault`, which becomes **required** (both callers already pass it: `ModifyTeamSettings.tsx:352`, `ModifyProjectSettings.tsx:371`), so no caller silently gets Team wording. `AdvancedInputs` gains a `useTerminology()` call for the plural terms. | A new "owner kind" prop beside the existing one that already says it. Keeping the default (a forgotten prop would put Team words on a Portfolio). |
| DDD-6 | #6355: the tab already holds `sourceKey`; it passes a `trackerWord` to `SourceOptionPicker`, which passes it to `SourceOptionRow`. `trackerWord` comes from a two-line map in `DeliverySourceTab.tsx` (`"jira-release"` → `"Jira"`); an unknown key falls back to the tenant's term for the work tracking system (`getTerm(TERMINOLOGY_KEYS.WORK_TRACKING_SYSTEM)`). The `NoDateSet` caption becomes `No date in ${trackerWord}: set one there to follow it`. Placeholder on the Autocomplete's `TextField`: `Type to search ${sourceName}s` ("Type to search Jira Releases"); appending "s" is a known limit for a source name already in the plural, accepted while Jira Release is the only source. | A tracker name on the backend `DeliverySourceDescriptor`: an API change for one word while one source exists; it becomes right when a second tracker offers a source. |
| DDD-7 | Usage-data event, terminology, RBAC: no change beyond the copy; see DEVOPS for the event answer. | — |

## Wave: DESIGN / [REF] Component Decomposition

| Component | Path | Change |
|---|---|---|
| InfoPopoverButton | `components/Common/InfoPopoverButton/InfoPopoverButton.tsx` | NEW by extraction from `WidgetInfoButton` (slice 02) |
| WidgetShell | `pages/Common/MetricsView/WidgetShell.tsx` | uses `InfoPopoverButton`; looks the same (slice 02) |
| FormSelectField | `components/Common/FormSelectField/FormSelectField.tsx` | optional `info` (slice 02) |
| fieldDirectionTips | `components/Common/ConnectionDefault/fieldDirectionTips.ts` | NEW (copy, slice 02) |
| ConnectionDefaultFieldSelect | `components/Common/ConnectionDefault/ConnectionDefaultFieldSelect.tsx` | optional `info` passed through (slice 02) |
| AdvancedInputs | `pages/Common/AdvancedInputs/AdvancedInputs.tsx` | cutoff helper text + terminology + required `parentOverrideDefault` (slice 01); Parent Override `info` (slice 02) |
| DependenciesComponent | `components/Common/ProjectSettings/Advanced/DependenciesComponent.tsx` | Dependency `info` (slice 02) |
| ModifyConnectionSettings | `components/Common/Connection/ModifyConnectionSettings.tsx` | `tip` flag on three `WhereEachDefaultSits` entries; `info` on those rows (slice 02) |
| DeliverySourceTab | `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.tsx` | placeholder, `trackerWord` prop path, `NoDateSet` copy (slice 03) |

Driving ports: the Team and Portfolio settings pages, the connection page, and the Delivery create modal (UI).
Driven ports: none touched.

## Wave: DESIGN / [REF] Loading and Failure Looks

- Settings pages and the connection page: no new fetch. The settings forms render once their settings have loaded;
  the cutoff line and the info icons are static copy, present whenever the field is.
- Release picker (slice 03): the tab already fetches the options. While they load it shows a centred spinner in
  place of the picker (`DeliverySourceTab.tsx:364-370`), so neither the placeholder nor a caption is visible yet; if the
  load fails it shows "The list of {source} entries could not be loaded. Check this {Portfolio}'s connection and
  try again." (`:357`). Both stay as they are; the new placeholder and caption appear only on a loaded list.

## Wave: DESIGN / [REF] Open Questions (for DISTILL / DELIVER)

- None open. The keyboard reach of "Learn More" is settled by MD-5. The link has no "opens in a new tab" cue, as
  with every dashboard widget today; changing that belongs to the shared button, not to this feature.

## Wave: DESIGN / [REF] Changed Assumptions

- DISCUSS D3 said "reuse `InfoTooltip`"; MD-1 dropped the icon for #6348 and MD-5 chose the widget popover for
  #6352, so `InfoTooltip` is not touched and `WidgetInfoButton` is extracted instead (DDD-1).
- DISCUSS said the create wizards show these fields and that Closed Items Cutoff is undocumented. Neither holds:
  the wizards show none of them, and the docs carry the setting as "Cut Off Days". DISCUSS D5, D8, AC-1.1, AC-1.5
  and AC-2.1 were amended in place.
- DISCUSS AC-1.1 to AC-1.4, both elevator pitches and AC-3.2 quoted pre-decision copy (an icon, "Then & Now", a
  longer Release caption); they were amended in place to MD-1 and MD-3.
- DDD-3's first version keyed the copy on a bare `kind`; DISTILL showed that cannot pick the per-owner docs link,
  so it keys on the connection-default name.

## Wave: DEVOPS / [REF] Platform Readiness

Every platform choice is the project's standing one; nothing new is needed for copy on existing controls.

| Item | Answer |
|---|---|
| Environment matrix | One: `clean` (see `environments.yaml`). No install, migration or configuration. |
| CI/CD | Existing `ci.yml` "Build And Deploy Lighthouse" (frontend tests, build, Biome, Sonar; E2E via the verify workflows). No pipeline change. |
| Branching | Trunk-based on `main`; nothing pushed in AFK mode. |
| Deployment | Ships with the next regular release; rollback is the previous release. |
| Observability | No new logs or metrics: no server code changes. |
| Mutation testing | per-feature (project standard): StrykerJS once, after the last slice, on frozen code, over every touched frontend file (slice 01: `AdvancedInputs.tsx`; slice 02: `InfoPopoverButton.tsx`, `FormSelectField.tsx`, `fieldDirectionTips.ts`, `ConnectionDefaultFieldSelect.tsx`, `DependenciesComponent.tsx`, the touched part of `ModifyConnectionSettings.tsx`, `WidgetShell.tsx`; slice 03: `DeliverySourceTab.tsx`). ≥ 80 % per file. A string-literal mutant on a phrase no test pins (draft wording) counts as a survivor to kill with a test or to justify in writing, not an automatic exception. |
| Docs links | All three anchors the "Learn More" links point at exist today (`portfolios/edit.md` Dependency Field and Parent Override Field, `teams/edit.md` Parent Override Field), and the docs site serves `main`, so the links work from the first build. |
| E2E | No new spec. The existing suite runs in CI (`ci_verifysqlite` / `ci_verifypostgres`); `BaseEditPage`'s "Parent Override Field" combobox locator is run locally once after slice 02. |
| Coexistence | lefthook pre-commit (secret scan) must keep passing. |
| Pre-requisites | None from DESIGN. |

## Wave: DEVOPS / [REF] Usage-Data Event

**N/A, because** none of the three Stories adds a new capability whose use could be counted. They change
explanatory copy on existing controls, and neither "a tooltip was hovered" nor "a placeholder was seen" would
tell whether the copy worked. Their KPIs (K1 reporter confirmation, K2 no new support questions) are
qualitative and measured outside the product. The one new interactive element, the "Learn More" link in the
#6352 popover, was weighed too: a click says someone wanted more than the tip, which is as much a sign the tip fell
short as that it worked, so it cannot read as success; and the dashboard widgets' identical link reports nothing
today. `UsageDataEventName` and `docs/settings/usagedata.md` stay unchanged.

## Wave: DEVOPS / [REF] Monitoring Contracts

| KPI | Instrument |
|---|---|
| K1 | Maintainer follows up with the reporter after the release that carries the three Stories. |
| K2 | Owner: the maintainer. Search Slack #lighthouse and the GitHub issues for "cutoff", "cut off days", "closed items", "dependency field", "parent override", "is blocked by", "release date" and "can't pick" over the 60 days before and the 60 days after the release date that carries the three Stories; record both counts as a dated note in this feature's archive. Read a zero (or a drop) against the before-count as the copy doing its job; other Stories in the same release do not touch these settings, so they do not muddy the count. |


## Wave: DISTILL / [REF] Reconciliation

WS strategy A; environment-to-scenario check N/A (no walking skeleton).

First pass: 0 contradictions logged. The review gate then found six, all resolved upstream before this revision;
the scenarios follow the resolutions:

| Contradiction found | Resolution the scenarios follow |
|---|---|
| DISCUSS AC-1.1/AC-1.4 and the US-01 pitch kept an info icon and "Then & Now" after MD-1 dropped both | DISCUSS amended to MD-1: an always-visible line naming "Reports", read as the field's accessible description (AC-1.4). Pinned word for word; a guard asserts no info icon or "About Closed Items Cutoff" button beside the cutoff. |
| AC-3.2 and the US-03 pitch quoted a longer pre-decision caption | Amended to MD-3: "No date in {tracker}: set one there to follow it", falling back to the tenant's term for the work tracking system. Pinned with a renamed term ("Tracker"). |
| MD-3 sketched the placeholder as "Type to search Releases" under a picker labelled "Release" | Read per the note under MD-5: the placeholder follows the real label, "Type to search Jira Releases". |
| D8/AC-2.1 said the create wizards show these fields | They do not (`CreateTeamWizard`/`CreatePortfolioWizard` render only `CreateWizardShell`). No wizard scenarios; driving ports are the two settings pages, the connection page and the Delivery modal. |
| D5/AC-1.5 said Closed Items Cutoff is undocumented | The docs carry it as "Cut Off Days"; a finalization docs task, no test impact. |
| DDD-3's bare `kind` could not pick the per-owner docs link | DESIGN now keys on the connection-default name: `directionTip(field, tracker, terms)`. The scaffold matches. |

MD-5 (popover, not hover tooltip) replaced every slice 02 scenario: the icon is found by its name "About {field
label}", opened by click (one scenario by Enter, then Tab to "Learn More"; one closes with Escape), and the
popover's text and "Learn More" link are asserted.

Draft copy is pinned by phrase, decided copy word for word. #6348's line (MD-1) and #6355's strings (MD-3, DDD-6)
are exact. #6352's tips are pinned on their essential phrases only (`is blocked by`, `not 'blocks'` with any quote
mark, `None`, `replace`, `Predecessor`, `Reverse`, `is caused by`, `parent's id`, the tenant's term), so DELIVER
can polish the wording.

## Wave: DISTILL / [REF] Test placement

Frontend only, Vitest + RTL, colocated beside the component each scenario enters through (project ATDD policy:
real component tree, real `TerminologyProvider`, `it.skip` for pending). Pending scenarios sit in a `describe`
titled "Story NNNN slice 0N (pending): …"; active guards in a sibling `describe` without "(pending)". Every
scenario that prints a configurable word renders under the real `TerminologyProvider` over a stubbed terminology
answer with renamed words (Tickets, Bets, Programs, Tracker) via `src/tests/FieldTipsTestKit.tsx`; no test mocks
the terminology hook. The Delivery scenarios reuse `DeliverySourceTabFixture.tsx` data. No E2E: copy only, and
the one POM locator near these controls (`BaseEditPage` combobox "Parent Override Field") is unaffected because
the info button sits beside the combobox and is not part of its name.

## Wave: DISTILL / [REF] Scenarios

Paths under `Lighthouse.Frontend/src/`. Status: P = pending (`it.skip`), G = active guard (green today).
E = error/edge/guard scenario.

| File | Scenario | Story / AC | Status | E |
|---|---|---|---|---|
| `pages/Common/AdvancedInputs/AdvancedInputs.closedItemsCutoff.test.tsx` | Team: the line under the cutoff says what it limits, of its Work Items | 6348 / AC-1.1, 1.2, 1.4 | P | |
| same | Portfolio: the same line speaks of its Features | 6348 / AC-1.2, 1.3, 1.4 | P | |
| same | Tickets on a Team, never Work Items | 6348 / AC-1.3 | P | E |
| same | Bets on a Portfolio, never Features | 6348 / AC-1.3 | P | E |
| same | Before the settings arrive, the line already explains | 6348 / AC-1.2 | P | E |
| same | Names Reports in general; old retain-and-filter wording gone (bounded-change) | 6348 / AC-1.2 | P | E |
| same | No info icon or About button within the cutoff field's place | 6348 / AC-1.1 | G | E |
| `pages/Common/AdvancedInputs/AdvancedInputs.parentOverrideTip.test.tsx` | Jira Team: is caused by, and what None does | 6352 / AC-2.1, 2.3 | P | |
| same | Jira Portfolio: same example | 6352 / AC-2.1, 2.3 | P | |
| same | Azure DevOps Team: the field holding the parent's id, no Jira phrase | 6352 / AC-2.3 | P | |
| same | Linear, Csv, ServiceNow, no tracker: neutral, no example (4 cases, tracker passed explicitly) | 6352 / AC-2.3 | P | E ×4 |
| same | Team tip speaks of a Ticket, never a Work Item | 6352 / AC-2.3, D4 | P | E |
| same | Portfolio tip speaks of a Bet, never a Feature | 6352 / AC-2.3, D4 | P | E |
| same | Team: Learn More → `teams/edit.html#parent-override-field`, new tab | 6352 / AC-2.4 | P | |
| same | Portfolio: Learn More → `portfolios/edit.html#parent-override-field`, new tab | 6352 / AC-2.4 | P | |
| `components/Common/ProjectSettings/Advanced/DependenciesComponent.directionTip.test.tsx` | Jira: the waiting end, is blocked by, not blocks | 6352 / AC-2.1, 2.2 | P | |
| same | Jira: None reads Jira's links, a chosen field replaces them | 6352 / AC-2.2 | P | |
| same | Azure DevOps: Reverse end, Predecessor, no Jira phrase | 6352 / AC-2.2 | P | |
| same | Linear, Csv, ServiceNow, no tracker: neutral (4 cases) | 6352 / AC-2.2 | P | E ×4 |
| same | Speaks of a Bet that waits, never a Feature | 6352 / AC-2.2, D4 | P | E |
| same | Learn More → `portfolios/edit.html#dependency-field`, new tab | 6352 / AC-2.4 | P | |
| same | Keyboard alone: Enter opens the tip, Tab reaches Learn More | 6352 / AC-2.1, 2.6 | P | E |
| same | Escape closes the tip; `aria-expanded` back to false | 6352 / AC-2.1 | P | E |
| same | Choosing a field saves exactly the calls it made before | 6352 / AC-2.5 | G | E |
| `components/Common/Connection/ModifyConnectionSettings.directionTips.test.tsx` | Jira: only "About Parent Override Field" (Teams) and "About Parent Override Field", "About Dependency Field" (Portfolios) | 6352 / AC-2.1, 2.6 | P | |
| same | Estimation, Size Estimate, Feature Owner rows: no About button, no icon | 6352 / AC-2.1, slice OUT | P | E |
| same | Jira: Teams' Parent Override tip gives is caused by | 6352 / AC-2.3 | P | |
| same | Jira: Dependency tip asks for is blocked by | 6352 / AC-2.2 | P | |
| same | Azure DevOps: Dependency names Predecessor | 6352 / AC-2.2 | P | |
| same | Tenant's words reach the tips (Programs group, Bet) | 6352 / D4 | P | E |
| same | Teams' Parent Override → Team docs | 6352 / AC-2.4 | P | |
| same | Portfolios' Parent Override → Portfolio docs | 6352 / AC-2.4 | P | |
| same | Dependency → Dependency Field docs | 6352 / AC-2.4 | P | |
| same | CSV connection's defaults carry no info button | 6352 / slice OUT | G | E |
| `components/Common/FormSelectField/FormSelectField.info.test.tsx` | Carries an info button named "About {label}", closed until asked | 6352 / AC-2.6 | P | |
| same | Opening it shows the tip and Learn More, new tab, `noopener noreferrer` | 6352 / AC-2.1, 2.4 | P | |
| same | A tip with nowhere to lead has no Learn More | 6352 / AC-2.1 | P | E |
| same | Escape closes the tip again | 6352 / AC-2.1 | P | E |
| same | A field list with nothing to explain shows no icon and no button | 6352 / AC-2.5 | G | E |
| `pages/Common/MetricsView/WidgetShell.infoPopover.test.tsx` | A widget's info button still opens description, guidance, Learn More (new tab); Escape closes | 6352 / MD-5 extraction | G | E |
| `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.whyAndSearch.test.tsx` | Add Delivery → Jira Release: placeholder "Type to search Jira Releases" | 6355 / AC-3.1 | P | |
| same | Dateless Release reads "No date in Jira: set one there to follow it" | 6355 / AC-3.2 | P | |
| same | Unknown source key: "Type to search Milestones", "No date in Tracker: …" | 6355 / AC-3.2 | P | E |
| same | A dateless Release still cannot be picked (no pick reported, box stays empty) | 6355 / AC-3.2, D2 | G | E |
| same | A retired Release still reads "No longer available", never the missing-date advice | 6355 / AC-3.3 | G | E |

Totals: 51 tests (44 pending, 7 active guards); 29 error/edge/guard (57 %). RED classification: every pending
scenario fails on the missing behaviour, see `distill/red-classification.md`.

## Wave: DISTILL / [REF] Driving-port coverage

| Driving port | Covered through |
|---|---|
| Team settings page (`ModifyTeamSettings` mounts `AdvancedInputs`) | `AdvancedInputs.closedItemsCutoff`, `AdvancedInputs.parentOverrideTip` (Team variant) |
| Portfolio settings page (`ModifyProjectSettings` mounts `DependenciesComponent` + `AdvancedInputs`) | the same two files (Portfolio variant) + `DependenciesComponent.directionTip` |
| Connection page, defaults section | `ModifyConnectionSettings.directionTips` (the real form in edit mode) |
| Delivery create modal, Jira Release tab | `DeliverySourceTab.whyAndSearch` (one scenario through `DeliveryCreateModal`, the rest through the tab) |

The create wizards show none of these fields, so they have no scenario. The dashboard widgets are not a driving
port of this feature; their one guard protects the popover the extraction moves.

## Wave: DISTILL / [REF] Adapter coverage

N/A: no driven adapter is touched (no endpoint, no persistence, no tracker call). The terminology and delivery
services are the existing frontend stand-ins.

## Wave: DISTILL / [REF] Scaffolds

| File | What it is |
|---|---|
| `components/Common/ConnectionDefault/fieldDirectionTips.ts` | RED scaffold, `export const __SCAFFOLD__ = true`; `directionTip(field, tracker, terms)` with `field: DirectionTipField` (the three connection-default names), `tracker: WorkTrackingSystemType \| undefined`, `terms: { feature; workItem }`, returning `{ text, learnMoreUrl }`; throws `Not yet implemented -- RED scaffold`. Not imported by any test. |
| `components/Common/FormSelectField/FormSelectField.tsx` | Type surface only: optional `info?: FieldInfo` (`{ text; learnMoreUrl? }`), accepted and ignored. `FieldInfo` exported. |
| `components/Common/ConnectionDefault/ConnectionDefaultFieldSelect.tsx` | Type surface only: optional `info?: FieldInfo`, accepted and not yet passed on. |
| `tests/FieldTipsTestKit.tsx` | Test kit: `renderWithTerms` (real `TerminologyProvider`), `RenamedTerms`, docs URLs, `theAboutButtonOf`, `theAboutButtonsIn`, `theInfoIconsIn`, `openTheTip`, `expectLearnMoreToOpen`. |

No `InfoPopoverButton` scaffold: every scenario reaches it through `FormSelectField` or a form, so no test imports it.

## Wave: DISTILL / [REF] Pre-requisites for DELIVER

- Slice 01: `parentOverrideDefault` becomes required on `AdvancedInputs`; `AdvancedInputsComponent.test.tsx`
  and most of `AdvancedInputs.connectionDefault.test.tsx` render it without the prop and have to pass one.
- Slice 02: the extracted `InfoPopoverButton` keeps `WidgetShell`'s `widget-info-${key}` test id; the widget
  guard and the existing `WidgetShell.test.tsx` popover tests must stay green.
- Slice 02: the button's accessible name is exactly "About {field label}" and it carries `aria-expanded`; every
  slice 02 scenario finds it by that name.
- Slice 03: `DeliverySourceTab.picker.test.tsx:180` pins the old "No Release Date Set"; update it in the same step
  that changes the caption (`optionList.test.tsx:106` only asserts its absence and stays green).
- Slice 03: the tab starts reading the terminology hook; the sibling `DeliverySourceTab.*` suites mock it and
  answer the key for an unmapped term, so check they still read sensibly once the fallback word is read.
- From the second DISTILL review, to fold into the first step that touches each file: the AC-2.5 guard asserts
  `mock.calls`; assert the resulting settings through a small stateful harness instead. Anchor the retired-Release
  locator (`/^Release 40 \(/`). Scope `openTheTip` to the popover holding the tip rather than any
  `presentation` role. Find the cutoff's container by role or test id, not `.MuiGrid-root`. Pin "rather than"
  beside "replace" in the Dependency tip.

## Wave: DISTILL / [REF] Review Gate

Iteration 1: DISCUSS needs_revision, DESIGN rejected, DEVOPS conditionally approved, DISTILL rejected. Iteration 2,
after the revisions and maintainer decision MD-5: DISCUSS conditionally approved (its conditions addressed in
place), DESIGN approved, DISTILL approved, DEVOPS conditions addressed. Zero blockers and zero highs open.
