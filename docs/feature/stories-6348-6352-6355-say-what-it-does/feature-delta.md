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
| Closed Items Cutoff (days), Team and Portfolio settings and both create wizards (`AdvancedInputs.tsx:54`) | Bare number field | Nothing says what it limits. **Not documented anywhere under `docs/`.** |
| Parent Override Field (`AdvancedInputs.tsx:36`, connection defaults `ModifyConnectionSettings.tsx:162,173`) | Select with a "Connection default (…)" entry | Nothing says which way round a link phrase is read |
| Dependency Field (`DependenciesComponent.tsx:37`, connection default `ModifyConnectionSettings.tsx:175`) | Same select | Same; a field that matches nothing also silently hides the native links |
| Release picker (`DeliverySourceTab.tsx:205`) | MUI Autocomplete; disabled rows carry the caption "No Release Date Set" | The caption says what, not why or what to do; nothing hints that typing filters |

`InfoTooltip` (info icon + tooltip, the icon named by its text) already exists and is used in 4 places.

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
| D3 | Reuse `InfoTooltip`. Where it lives (moved from `pages/Teams/Detail/Refinement/` to `components/Common/`) and whether it gains a docs link is DESIGN's call. | Avoids a second info-icon look. |
| D4 | Tooltip copy uses the configurable terms (`Work Items`/`Features`, `Team`/`Portfolio`) through the terminology hook, never "Epic" or "Story". | Terminology rule. |
| D5 | **#6348 needs a docs section first-of-its-kind**: "Closed Items Cutoff" in `docs/teams/edit.md` and `docs/portfolios/edit.md`, written at finalization. | The setting has no docs today, so a "learn more" has nowhere to land. |
| D6 | **#6348 fact, verified in code:** the cutoff is in the fetch fingerprint (`FetchFingerprint.cs:42`), so raising it makes the next update a full fetch that brings older finished items back. Saving starts no update by itself. `0` keeps everything (`ReportWindowPolicy.EarliestStart`, `RetentionEdge`). The tip says both. | Answers the Story's open "check". |
| D7 | **#6352 direction copy is tracker-aware**: Jira's waiting end is *is blocked by* (not *blocks*); Azure DevOps' is the `-Reverse` end (`System.LinkTypes.Dependency-Reverse`, Predecessor). Parent Override on Jira names the phrase read from the Feature/Work Item towards its parent (e.g. *is caused by*). | A Jira-only example misleads an ADO admin, and direction is the whole point. |
| D8 | #6352 covers every place the two fields are chosen: connection defaults, Team settings (Parent Override), Portfolio settings (both), both create wizards. One change in the shared select reaches all but the connection page. | Story scope: "wherever they are chosen". |
| D9 | #6355: the disabled entry says why and what to do; the input carries a search placeholder. Exact copy and whether the reason sits inline or in a tooltip are pinned at the DISTILL sketch. | UI sketch rule. |

### Maintainer decisions — 2026-10-22, before AFK (sketches reviewed)

| ID | Decision |
|---|---|
| MD-1 | **#6348: rewrite the existing always-visible helper line; no info icon.** Copy: "Finished {Work Items} older than this many days are not kept. This limits metrics, forecasts and how far back Reports can reach. 0 keeps them all. Raising it brings older ones back with the next update." Say "Reports", not "Then & Now report". Supersedes D3 for this slice and AC-1.1/AC-1.4's icon. |
| MD-2 | **#6352: info icon beside the select; the tooltip holds the direction copy plus a "Learn more" link to the docs, opening in a new tab.** Same `InfoTooltip` look as the Refinement settings. |
| MD-3 | **#6355: inline caption on the row, no tooltip.** A dateless Release reads "No date in {tracker}: set one there to follow it"; the input's placeholder reads "Type to search {Releases}". "No longer available" unchanged. |
| MD-4 | All three Stories tagged **Release Notes** in ADO (done 2026-10-22). Standard auto-transitions apply. |

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

Simplest first, as asked: 01 is one field, 02 spans five places and two trackers' wording, 03 touches the
Premium Delivery form. No slice depends on another; 02 reuses whatever 01 settles about `InfoTooltip`.

### Slice taste tests

- No slice ships 4+ components; none rests on a new abstraction (D3 reuses one).
- Each has a hypothesis that can fail (see briefs) and is checked against production-shaped data on the dev
  instance (`:5169`, real history; Jira Releases where the dev connection has them).
- Not scale variants of each other. A slice with only synthetic data: none.

## Wave: DISCUSS / [REF] User Stories

### US-01 — Closed Items Cutoff says what it limits where it is set (slice 01, ADO #6348)

`job_id: job-config-admin-know-what-a-setting-limits-before-it-bites`

As a config-admin, I want the Closed Items Cutoff setting to tell me what it limits, so I set it on purpose.

#### Elevator Pitch
Before: the admin learns what Closed Items Cutoff does only when a Then & Now report refuses a window.
After: open a Team's (or Portfolio's) settings → hover the info icon beside **Closed Items Cutoff (days)** → sees
"Finished Work Items older than this many days are not kept, which limits metrics, forecasts and how far back a
Then & Now report can reach. 0 keeps them all. Raising it brings older ones back with the next update." (draft copy)
Decision enabled: which cutoff to set for how far back this Team or Portfolio needs to look.

#### Acceptance Criteria
- AC-1.1 The info icon is beside the setting on Team settings, Portfolio settings and both create wizards.
- AC-1.2 The tip names what is not kept, what that limits (metrics, forecasts, Then & Now), what 0 means, and
  that raising it brings older items back with the next update.
- AC-1.3 The tip uses the configured term (Work Items on a Team, Features on a Portfolio).
- AC-1.4 The icon is reachable by keyboard and named by its text for a screen reader.
- AC-1.5 `docs/teams/edit.md` and `docs/portfolios/edit.md` gain a "Closed Items Cutoff" section saying the same.

### US-02 — Dependency and Parent Override fields say what they read, and which way round (slice 02, ADO #6352)

`job_id: job-config-admin-name-the-link-our-dependencies-live-in`, `job-config-admin-name-the-link-our-hierarchy-lives-in`

As a config-admin, I want each of these fields to tell me which end of a link it reads, so I do not pick a field
that matches nothing.

#### Elevator Pitch
Before: an admin picks *Issue Links* as the Dependency Field, which matches nothing and hides Jira's *is blocked
by* links, with no hint why.
After: open a Jira connection's defaults (or a Portfolio's settings) → hover the info icon beside **Dependency
Field** → sees "Name the link phrase on the Feature that waits, such as *is blocked by*, not *blocks*. Leave it on
None to read Jira's Blocks links." with a link to the Dependency Field docs (draft copy).
Decision enabled: which Additional Field, if any, to choose, and in which direction.

#### Acceptance Criteria
- AC-2.1 An info icon sits beside **Dependency Field** and **Parent Override Field** wherever they are chosen:
  connection defaults, Team settings (Parent Override), Portfolio settings (both), both create wizards.
- AC-2.2 The Dependency tip says it reads the waiting end and gives the tracker's example: Jira *is blocked by*
  (not *blocks*); Azure DevOps the `-Reverse` end (Predecessor). It says None reads the tracker's native links,
  and that a chosen field replaces them rather than adding to them.
- AC-2.3 The Parent Override tip says it reads the link or field on the Feature/Work Item that points to its
  parent, with the Jira example *is caused by*, and that None uses the native parent.
- AC-2.4 Each tip leads to the matching docs section (`portfolios/edit.html#dependency-field`,
  `…#parent-override-field`, `teams/edit.html#parent-override-field`).
- AC-2.5 No validation or save behaviour changes.

### US-03 — The Release picker says why a Release cannot be picked and invites searching (slice 03, ADO #6355)

`job_id: job-forecaster-bind-a-delivery-to-the-release-it-tracks`

As a delivery-forecaster, I want a disabled Release to tell me why, and the picker to show it can be searched,
so a long list of Releases does not read as broken.

#### Elevator Pitch
Before: a Release without a date is greyed out with "No Release Date Set", which reads like a broken picker,
and nothing hints that typing filters hundreds of Releases.
After: Portfolio → Deliveries → **Add Delivery** → **Jira Release** tab → open the picker → sees the placeholder
"Type to search Releases" and, on a Release with no date, "Set a release date in Jira to pick it: a Delivery
that follows a Release takes its date from it." (draft copy)
Decision enabled: set the date in Jira and come back, or search for the Release they actually meant.

#### Acceptance Criteria
- AC-3.1 An empty picker input shows a search placeholder naming the source (Release / the connection's term).
- AC-3.2 A Release without a date stays unpickable, and its entry says why (a following Delivery takes its date
  from the Release) and what to do (set a release date in the tracker).
- AC-3.3 "No longer available" (retired at source) keeps working as today.
- AC-3.4 `docs/portfolios/detail.md#following-a-jira-release` mentions both.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measure |
|---|---|---|
| K1 The reporter confirms each change answers his feedback | 3 of 3 Stories | Reply to the Release Notes / direct follow-up |
| K2 No new support question on these three settings | 0 in the 60 days after release | Slack / GitHub issues mentioning cutoff, dependency direction, or "can't pick Release" |

Usage-data event: left to DEVOPS to answer; a tooltip is not a usage signal worth an event, so "N/A" is expected.

## Wave: DISCUSS / [REF] Definition of Done

1. ACs green in Vitest (and one existing E2E POM stays green where the picker is driven).
2. `pnpm test`, `pnpm build` (Biome included) clean.
3. Sonar: no new issues.
4. Docs updated per AC-1.5, AC-2.4, AC-3.4 at finalization.
5. Screenshots regenerated only where a captured page changes visibly (Delivery create form, if captured).
6. Copy uses configurable terms.
7. Per-Story ADO transitions (Active on first commit, Resolved on push, Closed on green CI).
8. Each Story tagged Release Notes (confirm with maintainer).
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

Application scope (`nw-solution-architect` remit), propose mode, run in AFK; recommended option taken at each
point. Frontend only: no backend, no HTTP contract, no persistence, no ADR (no decision here outlives the copy it
carries). `docs/product/architecture/brief.md` unchanged: no component boundary moves. Outcome collision check
skipped: no new typed contract.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `InfoTooltip` | `pages/Teams/Detail/Refinement/InfoTooltip.tsx` | Info icon + tooltip | **EXTEND + MOVE** to `components/Common/InfoTooltip/` | Now used outside Refinement (settings already import it across folders). Gains an optional `learnMoreUrl`. |
| `FormSelectField` | `components/Common/FormSelectField/FormSelectField.tsx` | Renders every select involved, including the connection-default rows | **EXTEND** with optional `info` | One prop reaches the connection page (renders it directly) and every `ConnectionDefaultFieldSelect` use. |
| `ConnectionDefaultFieldSelect` | `components/Common/ConnectionDefault/ConnectionDefaultFieldSelect.tsx` | Parent Override + Dependency on Team/Portfolio settings and wizards | **EXTEND**: pass `info` through | Knows the tracker already (`connection?.workTrackingSystem ?? settings?.workTrackingSystem`). |
| `AdvancedInputs` helper text | `pages/Common/AdvancedInputs/AdvancedInputs.tsx:68` | The cutoff's explanation | **EXTEND** (rewrite the string) | MD-1. |
| `blockedReason` map | `DeliveryGrid/DeliverySourceTab.tsx:59` | Disabled-Release copy | **EXTEND** | MD-3. |
| Docs URL constants | e.g. `SecretHandlingNotice.tsx:8` | Absolute `https://docs.lighthouse.letpeople.work/...` links | **REUSE pattern** | Same convention for the new links. |

CREATE NEW: one copy module, `components/Common/ConnectionDefault/fieldDirectionTips.ts`. Justification: the
direction copy is per field × tracker and is needed by two renderers (the connection page and
`ConnectionDefaultFieldSelect`); keeping it in one place stops the two drifting.

## Wave: DESIGN / [REF] Decisions

| ID | Decision | Rejected |
|---|---|---|
| DDD-1 | `InfoTooltip` moves to `components/Common/InfoTooltip/InfoTooltip.tsx` (imports updated, four callers + tests). New optional `learnMoreUrl`: the tooltip title then renders the text plus a "Learn more" anchor (`target="_blank"`, `rel="noopener noreferrer"`). The icon's `aria-label` stays the text. MUI tooltips are interactive by default, so the pointer can reach the link. | A second, link-carrying tooltip component (two looks for one idea). |
| DDD-2 | `FormSelectField` gets an optional `info?: { text: string; learnMoreUrl?: string }`. When set, the `FormControl` and an `InfoTooltip` sit in one flex row (`alignItems: center`), the same arrangement as `RefinementBandSettings`. Unset renders exactly as today. | Icon inside the select's label or as an end adornment (the select's own arrow and label notch fight it). |
| DDD-3 | `fieldDirectionTips.ts` exports `directionTip(kind: "dependency" \| "parentOverride", tracker: WorkTrackingSystemType \| undefined, featureTerm, workItemTerm)` → `{ text, learnMoreUrl }`. Jira and AzureDevOps get their own example (Jira *is blocked by* not *blocks* / *is caused by*; ADO the `-Reverse` end, Predecessor; ADO Parent Override names a field whose value is the parent's id). Any other or unknown tracker gets the tracker-neutral sentence without an example. | Copy inline at each call site (five places, two renderers). |
| DDD-4 | Docs links: Dependency → `portfolios/edit.html#dependency-field`; Parent Override on a Portfolio or the Portfolio connection default → `portfolios/edit.html#parent-override-field`; on a Team or the Team connection default → `teams/edit.html#parent-override-field`. | One generic docs link. |
| DDD-5 | #6348: the helper text becomes a function of the configured term: Team → Work Items term, Portfolio → Features term. `AdvancedInputs` already distinguishes the two through `parentOverrideDefault`; it reads the terminology hook for the plural terms. | A new "owner kind" prop (the existing one already says it). |
| DDD-6 | #6355: placeholder `Type to search ${sourceName}s` on the Autocomplete's `TextField` (renders "Type to search Jira Releases"). The `NoDateSet` caption becomes `No date in ${tracker}: set one there to follow it`, the tracker word coming from a small frontend map keyed by the source `key` (`jira-release` → `Jira`), falling back to "the work tracking system". | Adding a tracker name to the backend `DeliverySourceDescriptor` (an API change for one word; only one source exists). |
| DDD-7 | Usage-data event, terminology, RBAC: no change beyond the copy; see DEVOPS for the event answer. | — |

## Wave: DESIGN / [REF] Component Decomposition

| Component | Path | Change |
|---|---|---|
| InfoTooltip | `components/Common/InfoTooltip/InfoTooltip.tsx` | MOVED + optional `learnMoreUrl` |
| FormSelectField | `components/Common/FormSelectField/FormSelectField.tsx` | optional `info` |
| fieldDirectionTips | `components/Common/ConnectionDefault/fieldDirectionTips.ts` | NEW (copy) |
| ConnectionDefaultFieldSelect | `components/Common/ConnectionDefault/ConnectionDefaultFieldSelect.tsx` | optional `info` passed through |
| AdvancedInputs | `pages/Common/AdvancedInputs/AdvancedInputs.tsx` | cutoff helper text (slice 01); Parent Override `info` (slice 02) |
| DependenciesComponent | `components/Common/ProjectSettings/Advanced/DependenciesComponent.tsx` | Dependency `info` (slice 02) |
| ModifyConnectionSettings | `components/Common/Connection/ModifyConnectionSettings.tsx` | `info` on the Parent Override and Dependency default rows (slice 02) |
| DeliverySourceTab | `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.tsx` | placeholder + `NoDateSet` copy (slice 03) |

Driving ports: the settings forms and the Delivery create modal (UI). Driven ports: none touched.

## Wave: DESIGN / [REF] Loading and Failure Looks

No new data fetch. Every changed control sits inside a form or picker that already has its loading and failure
states (settings pages load before the form renders; the Release picker's failed-load notice is unchanged).

## Wave: DESIGN / [REF] Open Questions (for DISTILL / DELIVER)

- Keyboard reach of the "Learn more" link inside a tooltip is limited (the tooltip opens on focus, but the link
  sits in a portal). Accepted for MD-2; the field's docs section is also reachable from the docs site. DISTILL
  pins the pointer path and the icon's accessible name.

## Wave: DESIGN / [REF] Changed Assumptions

- DISCUSS D3 said "reuse `InfoTooltip`" for all three; MD-1 dropped the icon for #6348, so slice 01 does not
  touch `InfoTooltip`. Slice 02 does the move and the link.
- DISCUSS AC-1.1 and AC-1.4 (icon beside the cutoff, keyboard-reachable icon) are superseded by MD-1: the
  rewritten helper line is always visible, so there is no icon. AC-1.2 and AC-1.3 now apply to the helper line.

## Wave: DEVOPS / [REF] Platform Readiness

Every platform choice is the project's standing one; nothing new is needed for copy on existing controls.

| Item | Answer |
|---|---|
| Environment matrix | One: `clean` (see `environments.yaml`). No install, migration or configuration. |
| CI/CD | Existing `ci.yml` "Build And Deploy Lighthouse" (frontend tests, build, Biome, Sonar; E2E via the verify workflows). No pipeline change. |
| Branching | Trunk-based on `main`; nothing pushed in AFK mode. |
| Deployment | Ships with the next regular release; rollback is the previous release. |
| Observability | No new logs or metrics: no server code changes. |
| Mutation testing | per-feature (project standard), StrykerJS on the touched frontend files, ≥ 80 %. |
| Coexistence | lefthook pre-commit (secret scan) must keep passing. |
| Pre-requisites | None from DESIGN. |

## Wave: DEVOPS / [REF] Usage-Data Event

**N/A, because** none of the three Stories adds a new capability whose use could be counted. They change
explanatory copy on existing controls, and neither "a tooltip was hovered" nor "a placeholder was seen" would
tell whether the copy worked. Their KPIs (K1 reporter confirmation, K2 no new support questions) are
qualitative and measured outside the product. `UsageDataEventName` and `docs/settings/usagedata.md` stay
unchanged.

## Wave: DEVOPS / [REF] Monitoring Contracts

| KPI | Instrument |
|---|---|
| K1 | Maintainer follows up with the reporter after the release that carries the three Stories. |
| K2 | Slack #lighthouse and GitHub issues, 60 days after that release. |
