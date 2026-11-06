<!-- markdownlint-disable MD024 -->
# Feature Delta — story-6501-ordered-parent-fields

> ADO User Story #6501, now titled "Ordered parent sources" (was "Multiple Parents"). Maintainer's words, lightly
> cleaned: *"Some (strange) setups use different fields for parents. So even with the parent override it won't help.
> One possible solution is to allow specifying more than one additional field and go through them in order:
> 1. Parent, 2. Is Caused by, 3. ... If Parent is not set, it goes to 'is caused by' etc., which supports those odd
> configs."*
>
> Mode: PROPOSE, then revised after a peer review (NEEDS_REVISION) and the maintainer's decisions of 2026-11-02, which
> are binding and sit in the last section ("Maintainer decisions"). Assumptions are numbered A1..; what is still
> open is listed under "Resolved and still open". Nothing is committed, nothing touched on ADO, no shared file
> edited (ADR-270, ARCHITECTURE.md and jobs.yaml changes are *proposed* below).

## AFK hold (2026-11-06, night; nothing pushed)

All 21 steps, the slice refactors and reviews, the final refactor and review, and mutation are done. The maintainer stopped the run here for the night. On the next word: rebase if `origin/main` moved, push, `/clean-ci`, then the maintainer reviews by hand. The demo Jira instance with 2 parent sources waits for the maintainer's word; work items on the demo Jira may be changed for it.

Gates, as the crafter reported them on the last commit: backend 12,796 passed, 2 skipped, 0 failed; frontend 9,493 passed; builds clean. Live Jira 117 of 117 and live Azure DevOps 100 of 100 ran before the last connector-neutral edits. Mutation: backend 96.32 % (489 tested), frontend 92.67 % (464 tested); see `mutation/results.md`. 183 backend mutants did not compile and were never tested; counted as survivors the backend reads 70.09 %. E2E was not run for this feature yet.

For the maintainer:

1. A lone link to a Feature stored later is repaired only by a full refresh, and nothing schedules one on Jira or Azure DevOps. Accept and say so in the docs and release note, or re-place Team records after a Portfolio refresh stores a new Feature.
2. `ParentSourceMatchedNothing` says "link type" on Azure DevOps, which calls it a relation type.
3. A refresh whose query returns nothing reports no parent findings.
4. Backend refusal and log labels say Parent sources too; revert if only the screens were meant.
5. The connection bridge stayed: the old number and fields added in the same save still use it.
6. Clearing a default means leaving only the own Parent row; there is no None.
7. By hand: keyboard focus after Remove can still drop, and keyboard behaviour was not tried in a browser.
8. Mutation survivors worth a look: `JiraWorkTrackingConnector.cs:2031`, `ModifyConnectionSettings.tsx:206`.

Owed at finalize: docs still say Parent Override Field, and the tips link to `#parent-override-field`, so renaming those headings breaks them; the release note on items that move and on Azure DevOps reading relations on every full fetch.

## Slice 01 double review (2026-11-06, sonnet, read only; fixes wait for the maintainer)

No blocker, no high. Fixes are owed before slice 02 starts.

| # | Severity | Finding | State |
|---|---|---|---|
| 1 | Medium | `ParentSourcesList.tsx:214,228`: a Portfolio's list says "Work Item" where its items are Features | Confirmed; wording is the maintainer's |
| 2 | Medium | An API client can store a list on Azure DevOps, which reads only the first field until 02-01; no page can send it | Interim, closes in slice 02 if it ships |
| 3 | Low | The sentence "The next source in the list was read instead" is untrue when the unmatched entry is the last one (Refresh History and `WorkItemService.cs:1070`) | Wording is the maintainer's |
| 4 | Low | Disabled Remove button: the reason reaches mouse users only; a move can drop keyboard focus | Open |
| 5 | Low | `ModifyConnectionSettings.parentSources.test.tsx:53,55` comments name the story and a wave; stale Ignore text in the two skipped scenario classes | Fix when those steps activate them |
| 6 | Low | Azure DevOps findings say "One of the Parent sources" while it still has one field | Follows from the rename; closes in slice 02 |
| 7 | Low | `AdvancedInputs.parentSources.test.tsx`: `/Connection default.*Parent \(Jira's own\)/` also matches a longer default | Open |
| 8 | Low | `usagedata.md` wording against the code's "differs from the one last saved" | Check |

Maintainer answers (2026-11-06, after the review):

- Finding 1: a Portfolio's list says Feature for its items.
- Finding 3: the sentence drops "The next source in the list was read instead." when the unmatched entry is the last one.
- A lone link to a Feature not stored yet: check whether a later full refresh repairs it, and fix it if not.
- A follower of an unset default who picks Set here keeps the radio until the page is reloaded.
- The follower sketch is approved: while following, rows and picker are hidden, the line names the default, and the cost line shows when the default leaves out the own Parent.
- A Portfolio's cost line reads: `Not in the list: Parent (Jira's own). Features whose only parent is Jira's own parent get no parent.`

Not covered: the bodies of `ParentSourcesRefreshScenarios`, `ParentSourcesSettingsApiIntegrationTest` and the new `WorkItemServiceTest` tests; clone and unsaved-owner paths; the DD-7 loading and failure looks.

## Slice 02 review (2026-11-06, haiku, read only)

Needs revision, no blocker. One revision pass done, test commits only: 5cd99bf82, aee6c4a7c, e7a15170e, 0ecc739ec.

| # | Severity | Finding | State |
|---|---|---|---|
| 1 | Medium | Findings ride on records, so a refresh whose query returns nothing reports none | Open, for the hold; known since slice 01 |
| 2 | Medium | The dependency scenario could not fail if the relations guard went | Fixed: a Portfolio scenario, seen to fail with the guard removed |
| 3 | Medium | A stale tab sending None without a list could lose the list | Not a defect: the concurrency token answers 409; pinned. A save with no token skips the check |
| 4 | Low | Connection defaults stay two entries; `ParentSourcesSetting.tsx` rebuilds one from the old int | Slice 03, noted on 03-02 |
| 5 | Medium | An unsaved owner might show Set here while the server follows the default | Not a defect: only the clone page renders it, and it carries the source; server side pinned |
| 6 | Low | `useModifySettings.ts`: an update with no settings loaded is dropped | Left: the form sits behind the loader |
| 7 | Low | "was read instead" suggests a parent was found | Left: the maintainer's wording |
| 8 | Low | One unreadable stored list fails every load of that owner | Left: deliberate; whether overview pages survive is unchecked |
| 9 | Low | Scenario named After the upgrade seeds no legacy owner | Fixed: renamed |
| 10 | Low | Unsaved-Team test asserted only the heading | Fixed |
| 11 | Low | `itsParent` is plain "parent" | Rejected: parent is not a configurable term |
| 12 | Low | `relations.ParentReferences.Add` throws on a repeated id | Left: older code, repeats unchecked |

Also for the hold: `ParentSourceMatchedNothing` says "link type" on Azure DevOps, which calls it a relation type.

## Slice 03 review (2026-11-06, haiku, read only)

Needs revision, no blocker. Spot-checked by the orchestrator before acting.

| # | Severity | Finding | State |
|---|---|---|---|
| 1 | High | The page pruned a removed field from a default's list, so the save was not refused and followers moved silently | Revision: lists go to the server as they stand and the refusal shows |
| 2 | Medium | The same prune dropped an entry for a missing definition | Revision: closes with 1 |
| 3 | Medium | Event absent from `usagedata.md` | Rejected: that page lists friendly names; the row is there |
| 4 | Medium | The event fires from three places | Rejected: one event for all three is the spec |
| 5 | Medium | No scenario sends an int-only change on a list default | Revision |
| 6 | Medium | The first scenario cannot see a Portfolio queued by a Teams change | Revision |
| 7 | Low | The in-use scenario covers one position and a loose message | Revision |
| 9-12 | Low | Old int on a default: null beside `[own]` keeps it, null beside an echo clears it, stale int with no default sets one, a changed int drops later entries | Left: the agreed API rule; for the hold |
| 13 | Low | Unset to `[own]` logs as a change | Left: log only, no update queued |
| 14 | Low | A list naming a field added in the same save reads "does not have" | Left: the page cannot send it |

## Final review (2026-11-06, sonnet, read only)

Needs revision, no blocker, no high. The upgrade, refresh, effective-list, authorisation and copy checks held.

| # | Severity | Finding | State |
|---|---|---|---|
| 1 | Medium | The connection page's tip ended "With None, ..." though the list has no None | Revision |
| 2 | Medium | A lone link to a Feature stored later is repaired only by a full refresh, and nothing schedules one on Jira or Azure DevOps | Maintainer's call, at the hold |
| 3 | Low | `FollowParentSourcesDefault` had test callers only and a false comment | Revision |
| 4 | Low | An owner with source Own and no list reads the default; only a rollback can leave that state | Left: the rollback limit below covers it |
| 5 | Low | Down then Up does not restore a list: it is rebuilt from the old columns | Left: rollback limit below |
| 6 | Low | A rule's failure message named the Azure DevOps connector | Revision |
| 7 | Low | The relation-type log line said it was read as if nothing were named | Revision |
| 8 | Low | The old int's setter and EF's load order were unverified | Revision: pinned by a reload test |

Rollback limits, in addition to the DD-1 text: running `Down` then `Up` rebuilds each list from the old columns, so `[A,B,own]` becomes `[A,own]`, `[own,A]` becomes `[A,own]` and `[A,B]` gains the own Parent. An older build's edit of a follower leaves source Own with no list, and that owner reads the connection default after rolling forward.

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ⊘ DISCOVER: none for this Story. Evidence: the maintainer's sentence, plus the shipped Parent Override Field and its
  history (Epic 6028, Bug 6203, Story 6301).
- ⊘ DIVERGE: none. The maintainer named the direction (an ordered list), so no option set was generated. Risk: the
  design space (a chain versus several parents per item) was not explored; see "Alternatives considered".
- ⊘ DEVOPS: not yet run. The usage-data question is answered as a proposal in the House Checklist.
- ✓ `docs/feature/epic-6455-deeper-hierarchies/feature-delta.md` (its current-state inventory: a record carries one
  parent; its DISCOVER decision "one parent per item per level, read by the existing parent-source rule"; its
  DIVERGE decision that link-typed and multi-parent levels may wait; its "connection-level mapping moves to the level"
  note).
- ✓ `docs/feature/epic-6474-one-structure/feature-delta.md:105-135` (Epic 6474, One structure: Parent Override sits in
  the shared base that becomes the Unit; the connection default goes "per level (leaf)"; Team and Portfolio become one
  Unit type).
- ✓ `docs/feature/epic-6473-settings-split/` (searched for Parent Override and 6501: no mention; its connection entry
  point slices move the connection page, which slice 03 edits).
- ✓ `docs/feature/story-6301-only-features-as-parent-candidates/feature-delta.md` (several link candidates are
  narrowed to stored Features and placed by oldest link, in `WorkItemService`), and ADR-193 (the parent source is a
  declared connector capability; a parent is 0..1).
- ✓ Epic 6028 and Bug 6203 (link types as parent sources): their feature folders are **absent** from this repository,
  so the evidence is the shipped code, `JiraWorkTrackingConnector.cs:1907-1952` (the override resolves to a field, a
  link type or nothing; a link type reads the counterpart at the end the typed phrase names), and what users are told,
  `docs/portfolios/edit.md:289-300` (line 293: three phrases per link type, which one you type decides the end read).
- ✓ `docs/concepts/worktrackingsystems/azuredevops.md:222` (a relation type named as Parent Override reads nothing).
- ✓ Code read (read-only, 2026-11-02), relative to `Lighthouse.Backend/Lighthouse.Backend/`:
  `Models/WorkTrackingSystemOptionsOwner.cs:145-185`, `Models/IWorkItemQueryOwner.cs:25`,
  `Services/Implementation/Parents/ParentSourceSelector.cs`, `Models/ParentResolution.cs`,
  `…/Jira/JiraWorkTrackingConnector.cs:1867-1993` (line 1879 sets `ParentReferenceId = issue.ParentKey`; 1907-1936
  lets the override replace it), `…/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:136-153, 672-694, 953-964,
  1384-1446, 1516-1527`, `Services/Implementation/WorkItems/FetchFingerprint.cs:50,92`,
  `Services/Implementation/Connections/ConnectionSaveService.cs:91-120`, `API/DTO/SettingsOwnerDtoBase.cs`,
  `API/DTO/ConnectionDefaultsDto.cs:36-45`, `Models/RefreshFinding.cs:30`.
- ✓ Frontend: `Lighthouse.Frontend/src/pages/Common/AdvancedInputs/AdvancedInputs.tsx`,
  `components/Common/ConnectionDefault/ConnectionDefaultFieldSelect.tsx`, `fieldDirectionTips.ts`,
  `components/Common/ItemListManager/ItemListManager.tsx`.
- ✓ `/storage/repos/lighthouse-clients` (grep: no `parentOverride`, no `additionalFieldDefinitions` use).
- ⊘ Basecamp: no Basecamp connector in this repository (grep: 0 hits). The connectors are Jira, Azure DevOps, Linear,
  CSV, ServiceNow.

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

How the Parent Override Field works today, end to end.

### The setting

| Piece | Today | Where |
|---|---|---|
| Value | **One** Additional Field Definition id per Team and per Portfolio, `ParentOverrideAdditionalFieldDefinitionId`, plus a source (`ConnectionDefault` or `Own`, ADR-241) | `Models/WorkTrackingSystemOptionsOwner.cs:145-185`; DTO `API/DTO/SettingsOwnerDtoBase.cs:27-29,78-83` |
| Connection default | Two ints on the connection: `ParentOverrideForTeams`, `ParentOverrideForPortfolios` | `API/DTO/ConnectionDefaultsDto.cs:36-45` |
| What it points at | An **Additional Field Definition** of the connection. Its `Reference` is a field name/id, or (Jira only) the name or a phrase of an issue **link type** | `JiraWorkTrackingConnector.cs:1944-1952`; `docs/portfolios/edit.md:293` |
| UI | A single select, "Parent Override Field", in Advanced Configuration on the Team and Portfolio settings, with a "Connection default (field)" first entry; the same select on the connection page for both defaults | `AdvancedInputs.tsx:42-60`, `ConnectionDefaultFieldSelect`, `ModifyConnectionSettings.tsx:163-174` |
| Who else reads the id | the fetch fingerprint (a change refetches the owner), the connection-save guard that refuses removing an Additional Field an owner still names, the refresh findings ("a name that matched nothing") | `FetchFingerprint.cs:50,92`, `ConnectionSaveService.cs:91-120`, `RefreshFinding.cs:30` |

### Per connector

| Connector | Reads Parent Override? | How the parent is resolved today | Links versus fields |
|---|---|---|---|
| **Jira** (Cloud, DC) | Yes | The record is built with Jira's own `parent` (`issue.ParentKey`, line 1879). If the override names a **field**, a non-empty value replaces it; if it names a **link type**, the counterpart at the matching end replaces it. An override that yields nothing leaves Jira's own parent standing (lines 1903-1936) | **"Is caused by" is a link type, and the single override already places link-only items** (Epic 6028, Bug 6203; evidence above). The real gaps are two: *more than one* non-native source, and an own Parent that is *not wanted* (it is always the fallback) |
| **Azure DevOps** | Yes, **fields only** | `GetParentReference`: the override field's value if non-empty, else the native Hierarchy-Reverse parent. A relation type named as Parent Override reads nothing, and Refresh History says so | Relation types as parents are unsupported (only the Dependency Field reads one). A chain on ADO is fields plus, if listed, the native parent link |
| **Linear** | **No.** Parent is the project (and the initiative above it) | n/a | The selector is offered but saved and ignored (documented, `docs/portfolios/edit.md:287`) |
| **CSV** | **No.** The Parent Reference Id column | n/a | n/a |
| **ServiceNow** | n/a: Teams only, no parent record, Portfolios cannot use it | n/a | n/a |
| **Basecamp** | Not in this repository | n/a | n/a |

### How parent resolution feeds Features and Work Items

- A record carries exactly **one** `ParentReferenceId`. The Team's Work Items hang under a Feature by exact key match
  (`ParentReferenceId == feature.ReferenceId`, `WorkItemService.cs:~703`), so a record with no resolvable parent counts
  toward no Feature and the Feature is sized by the default-size guess (Epic 6455 inventory: Feature sizing from
  children).
- A Portfolio's Feature uses the same rule for its parent, stored as a parent row and shown in the Parent column
  (display only, one level).
- Today the order is fixed and implicit: **the override first, the tracker's own parent as the fallback when the
  override yields nothing.** There is no way to say "never use the own parent". That is the second gap.

## Wave: DISCUSS / [REF] Persona IDs

- `config-admin`: sets up Teams and Portfolios on a tracker whose hierarchy is recorded inconsistently: some Work Items
  under a "Caused by" link, others in a custom field, the tracker's own Parent holding something that is not a Feature.
- `delivery-lead-rte` (reads the consequence only): Features sized from the children they really have.

## Wave: DISCUSS / [REF] JTBD One-Liner

**Reused, no new job (review finding B1).** `docs/product/jobs.yaml` (8884 lines, over the search tool's size limit) was
searched by anchored id-line patterns (`id: job-config-admin…` and `id: job-…(parent|hierarch|override|link)`; 24 id
lines for `config-admin`) and the candidates were read in full:

- **US-01 and US-02 -> `job-config-admin-name-the-link-our-hierarchy-lives-in`** (`jobs.yaml:6965`, feature_context
  `parent-from-issue-links`, revised by Story 6301). Same persona, same job story ("the hierarchy lives in a place the
  connector does not look; let me tell Lighthouse where, in the form I already use"), same Parent Override Field as the
  mechanism. 6501 widens it from "one place" to "several, in order, own Parent optional". It fits as a **revision of
  that job**, not a new job.
- **US-03 -> `job-config-admin-set-a-connection-setting-once`** (`jobs.yaml:8331`, Epic 6303), which already names
  Parent Override Field (Teams; Portfolios) as one of the settings set once per connection.
- Consequence job (not a `job_id` of these stories): `job-lead-size-features-from-real-children-not-a-default`.

To apply in DELIVER (not now, shared file): a `revisions:` entry on the first job (story
`story-6501-ordered-parent-fields`, date, wave DISCUSS): "Same job and persona. The hierarchy sits in two or more
non-native places, and the tracker's own Parent is sometimes not wanted at all; the Parent Override Field takes one
place and always falls back to the own Parent. Forces: Push = one source places only part of the items and the rest
land under an own Parent that is not a Feature; Pull = list the places in order, drop the own Parent; Anxiety = a wrong
order or a dropped own Parent silently leaves items unparented, mitigated by the visible order and a copy line naming
the consequence; Habit = the existing single override must keep its result." Also add the story to the feature list;
add a `feature_context` mention to the second job.

## Wave: DISCUSS / [REF] Standalone Story: relationship to Epics 6455, 6473 and 6474

**6501 is a standalone Story with no parent Epic and ships now, before Epic 6474 (maintainer decision).** It has value
without any level work: a Jira or Azure DevOps customer whose parents sit in several non-native places gets their
items under their Features with the two levels that exist. None of 6455/6473/6474 needs 6501 and 6501 needs none.

| Cross-Epic decision (by title) | How 6501 stays consistent |
|---|---|
| Epic 6455, Deeper hierarchies, DISCOVER: *one parent per item per level, read by the existing parent-source rule; a skipped level is the Parent Override Field's business* | 6501 keeps one parent per item. It widens only *where that one parent is read from*: an ordered list, the first source that yields a parent wins. No levels, no further walking, no multi-valued parent |
| Epic 6455, DIVERGE: link-typed and **multi-parent** levels may wait until after v1 | 6501 is **not** multi-parent; the title was renamed in ADO to "Ordered parent sources" for that reason. It adds no link-typed *level*; Jira link types already work as parent *sources* (Epic 6028) |
| Epic 6474, One structure: Team and Portfolio become one Unit type; Parent Override is in the shared base | The new value is stored on the **shared base** (`WorkTrackingSystemOptionsOwner`), as one setting, so 6474's "units keep their two tables" migration carries it with the Parent Override columns; 6474's migration carries the extra column. No per-class code, no per-level or per-unit-type concept |
| Epic 6455, "connection-level mapping moves to the level" (not scheduled) | The list is one value that moves as a whole when the Parent Override moves, with the same "follows the connection default / set here" behaviour (ADR-241) |
| Epic 6473, Settings split: connections move to System; connection entry points | Slice 03 edits the connection page's defaults. If 6473 has moved the page, edit it where it is |

Must not pre-empt or conflict with:

1. Do **not** put the list on a "level" or in Organisation. It stays where Parent Override is today.
2. Do **not** let a record hold several parents, a parent list, or an ancestor chain.
3. Do **not** add a data type or term named "level" or "parent level".
4. Do **not** change how Deliveries, Feature WIP or the roll-up read the parent: they keep reading the single stored
   `ParentReferenceId`.
5. Do **not** rename the existing setting's wire names. The new wire value is additive (ADR-241's expand-only rule).
6. Sequencing: 6501 ships first; 6474 slices that restructure the owner tables are rebased on it, not the reverse.

Story 6301 (shipped): its "narrow to stored Features, oldest link wins" runs *within one link source* after the
download. 6501 composes with it: each link entry keeps that behaviour, and a link entry whose candidates contain no
stored Feature **yields nothing**, so the next entry is read (maintainer decision).

## Wave: DISCUSS / [REF] Context-Map Placement (ADR-270)

- **Contexts touched (3):** *Tracker integration* (primary: the connector reads the list), *Organisation set-up* (the
  list is Mapping: "which tracker fields count as the parent", held on the owner and as a connection default),
  *Work Items* (stores the one resulting `ParentReferenceId`; the 6301 placement step in `WorkItemService`). One
  outcome, so no split is needed at three contexts.
- **How the list reaches the connector: as data from its owner.** The owner (Organisation set-up) already hands the
  connector the settings it fetches by (the existing Organisation set-up -> Tracker integration relationship in
  ADR-270). The list is one more of those values. **No new edge on the map**, no new event.
- **New glossary terms** (style of ADR-270's glossary; applied in DELIVER):
  - Organisation set-up, add: **Parent sources**: "The ordered list of places a record's parent is read from, one to
    five entries. Each entry is the tracker's own parent, an Additional Field, or (Jira) a link type. The first entry
    that yields a parent wins; the tracker's own parent is read only when it is in the list." Code name today:
    `ParentOverrideAdditionalFieldDefinitionId` (one entry). Drift: the UI still says "Parent Override Field".
  - Tracker integration, add: **Parent source**: "One place a connector can read a parent from (the tracker's own, a
    field, a link type). Resolved per record by the shared selector; a connector declares what it can read (ADR-193)."
  - Proposed change to the existing **Mapping** row in Organisation set-up: add "the parent source order".
  - Not a new concept: "Parent" stays the singular.
- **Domain events:** none published, none reacted to. A change of the list moves the owner's fetch fingerprint, which
  already queues a refetch (ADR-242; the fingerprint must include every entry and its position). "No new event, because
  the settings change already reaches Work Items through the fingerprint."
- **UI surface (ADR-271):** Team and Portfolio settings are the unit page's settings tab (Advanced Configuration group);
  the defaults sit on the connection page (System after Epic 6473). No new surface; no step towards the target needed,
  the control is kept where Parent Override is so it moves with it.
- **Proposed edits to shared files, applied in DELIVER, not now:**
  1. `docs/product/architecture/adr-270-…` glossary: the two rows above; the Mapping row addition.
  2. `ARCHITECTURE.md` §4 (and line ~63 where the parent source rule is described): "the parent is read from the owner's
     ordered parent sources; the first that yields a parent wins; the tracker's own parent only when listed".
  3. `docs/product/jobs.yaml`: the revision entry above and the story in the feature list.
  4. A new ADR at DESIGN (the shape of the list, the migration, the position of the native parent entry).

## Wave: DISCUSS / [REF] Locked Decisions

- **MD-1** (maintainer, ADO #6501): support more than one source for the parent and go through them in order.
- **MD-2..MD-8**: the maintainer decisions of 2026-11-02 in the last section (own Parent optional and explicit,
  link fall-through, at most 5 entries, duplicates rejected, standalone, Release Notes tag, ship now).

## Wave: DISCUSS / [REF] Assumptions (to confirm)

| ID | Assumption | If wrong |
|---|---|---|
| A1 | "First wins" means: the first entry that **yields a parent**; later entries are not read for that record. A field with a value yields it (even if it names a non-Feature, as today); a link entry yields only a Feature Lighthouse holds; the own Parent yields when the tracker reports one | n/a, decided |
| A2 | "Is caused by" is typed as a Jira link phrase in an Additional Field, exactly as `docs/portfolios/edit.md:293` documents; no new kind of entry for links | If a link needs its own entry kind, DESIGN adds one; the Story's shape holds |
| A3 | Teams and Portfolios get the list together (shared base, shared UI component) | Split Team from Portfolio in slice 01 |
| A4 | Migration writes today's behaviour explicitly: override X -> `[X, own Parent]`; no override -> `[own Parent]`; the upgrade queues one re-read of every Jira owner and every Azure DevOps override owner, and the items AC-1.6 names move once (UPGRADE-1 (A), DD-4) | If another default is wanted, change only the mapping |
| A5 | Five entries is generous for "odd configs" and keeps fetch cost bounded | Raise or remove; one number in DESIGN |
| A6 | Linear, CSV and ServiceNow keep ignoring the setting, as documented | A later Story, or hide the control under 6473/6474 |
| A7 | Azure DevOps entries are fields plus the native parent link when listed; relation types as parent sources stay unsupported | A separate Story |
| A8 | The Story needs no DISCOVER or DIVERGE; one maintainer sentence and an existing, documented mechanism are the evidence | The risk is that the customer's real data is not seen (ask for counts, see Q7 below) |

## Wave: DISCUSS / [REF] Journey (light)

Persona Maria Santos, config-admin at Contoso Payments, Jira Data Center, Team "Payments Core", 120 Work Items.

The real situation: 38 Work Items record their Feature in a **"Caused by" link** (to PAY-7), 29 in a **custom field
"Program Ref"** (to PAY-12), 53 have neither. Jira's own Parent holds PAY-1 "Payments housekeeping" on all 120, put
there by an automation, and it is not a Feature.

1. Maria sees the Features PAY-7 and PAY-12 are sized by the default guess although their Work Items exist. Today the
   Parent Override Field can name **one** of the two places, and whichever she picks, the rest fall back to Jira's own
   Parent PAY-1: with "Caused by", 38 are placed, **29 Program Ref items sit under the housekeeping epic** and 53 sit
   there too. She cannot say "never use Jira's own Parent".
2. She opens Payments Core settings, Advanced Configuration, finds **Parent sources**, picks "Set here". The list shows
   "Is caused by" then "Parent (Jira's own)" (what the upgrade wrote for her old override). She adds "Program Ref" in second place,
   removes "Parent (Jira's own)", and sees the line "Not in the list: Parent (Jira's own). Work Items whose only parent
   is Jira's own parent get no Feature." Feeling: relief, then caution ("what happens to the 53?").
3. She saves and refreshes. 38 items land under PAY-7, 29 under PAY-12, the 53 have no Feature (honestly, instead of
   under PAY-1). Feeling: confidence.
4. A Work Item with both a Caused-by link and a Program Ref stays under the earlier entry. She swaps the two, saves,
   refreshes: that item moves.
5. Emotional arc: stuck ("only one place, and the wrong fallback") -> hopeful -> cautious (the consequence line is
   visible) -> confident, because the order and the consequence are visible and reversible.

Error paths: an entry names an Additional Field that matches nothing on this tracker -> Refresh History says so, per
entry (as for a single override today), and the next entry is still read. A duplicate entry or a sixth entry is
refused on save with a plain message. The connection loses an Additional Field a list names -> the connection save
refuses, as today for a single override. A list without the own Parent leaves native-only items unparented, and the
copy says so before saving.

## Wave: DISCUSS / [REF] UI Sketch (to be walked through at the start of DISTILL)

Reuses: `ConnectionDefaultFieldSelect` (the "Connection default (field) / Set here / None" select) as the **per-row
picker and the follow/own switch**, and the drag-and-drop row list pattern of `ItemListManager` (used for state lists
with `onReorderItems`) for the rows. `ItemListManager` takes free text, so DESIGN decides whether to extend it or wrap
its row pattern around the select. Visualization skill: N/A, nothing here shows data.

```text
Advanced Configuration
+----------------------------------------------------------------------------+
| Parent sources                                                   (?) Learn |
|                                                                            |
| The first source that has a value gives the parent; the rest are ignored   |
| for that Work Item.                                                        |
|                                                                            |
| (o) Set here   ( ) Connection default: Is caused by, then Parent (Jira's own) |
|                                                                            |
|  1  ::  Is caused by                                   [ up ][ down ][ x ] |
|  2  ::  Program Ref                                    [ up ][ down ][ x ] |
|                                                                            |
| [ + Add a source  v ]                                                      |
| Adds Parent (Jira's own) or an Additional Field not yet in the list.       |
|                                                                            |
| Not in the list: Parent (Jira's own). Work Items whose only parent is      |
| Jira's own parent get no Feature.                                          |
+----------------------------------------------------------------------------+
```

Notes: the "Connection default" radio shows only when the connection holds a default. The "Not in the list" warning
shows only while the own Parent is absent (it names the consequence, the "no own Parent" case). "Add a source" is
disabled at five entries, with the text "A list holds at most 5 sources". A duplicate is refused on save with "Is caused
by is already in the list". With nothing set, the list is the single entry "Parent (Jira's own)" (what the upgrade
writes). Copy uses the configurable terms (Work Item, Feature are the seeded defaults; the page passes `getTerm`). On
Azure DevOps the entry reads "Parent (Azure DevOps' own)". On Linear/CSV the control is unchanged from today (offered,
ignored; A6). Loading/failure looks (DESIGN's list): the list renders from the Additional Fields already loaded with the
connection, so it adds no request; if they fail to load, the existing page error covers it (to confirm at DESIGN).
Connection page (slice 03): the same list under "Parent Override" for Teams and for Portfolios.

Alternative rejected for the sketch: N separate "Parent Override Field 2/3/4" selects. Ordering by position is awkward
and does not scale; the list is the same cost with one control.

## Wave: DISCUSS / [REF] Scope Assessment

**PASS.** 3 stories, 3 contexts (one outcome), 4-5 days in total (slice 01: 2-3, slice 02: 1, slice 03: 1), 0 new
integration points beyond the two connectors that already read the setting. Watch item: slice 01 is at the top of the
1-3 day band (model, migration, API, Jira, UI, fingerprint, save guard); if it overruns, split Team from Portfolio, not
backend from UI.

## Wave: DISCUSS / [REF] Story Map & Slices

Backbone (config-admin): **notice items are in the wrong place or nowhere** -> **name the sources in order** ->
**refresh** -> **check where each item landed** -> **make it the default for the connection**.

| Slice | Story | Walking skeleton? | Size | Brief |
|---|---|---|---|---|
| 01 | US-01 A Jira Team or Portfolio reads its parent from the first source that yields one | Yes: Jira, Team and Portfolio, set here, fields and link entries, own Parent optional, migration | 2-3 days | `slices/slice-01-jira-reads-the-parent-sources-in-order.md` |
| 02 | US-02 An Azure DevOps Team or Portfolio does the same | No | 1 day | `slices/slice-02-azure-devops-reads-the-parent-sources-in-order.md` |
| 03 | US-03 The connection holds the parent sources as the default | No, droppable alone | 1 day | `slices/slice-03-connection-default-holds-the-parent-sources.md` |

### Priority Rationale

- **01 first**: the maintainer's customer case (Jira), releasable alone, carries the model, the migration and the UI.
  Existing installs keep their result through the explicit migration, except the items AC-1.6 names.
- **02 and 03 depend on slice 01** (its model, migration, API and list control) and **can only be released after it**.
  Neither is needed for 01 to be valuable, so dropping either is safe.
- **02 after 01**: same rule on the other connector that reads the setting; adds only the ADO read.
- **03 last**: convenience for installs with many Teams. Droppable.
- Order inside the Story only; there are no sibling Epics.

### Slice taste tests

- No slice ships 4+ new components. 01: owner value + migration + selector change + UI list (one new component, the
  list). Watch item above.
- 01 disproves "the first source that yields a parent is what the customer meant" on the customer's own data.
- Every slice has a user-visible outcome (a Work Item lands under its Feature).
- Production-like data: Jira DC items with a Caused-by link, a custom field, and a non-Feature native Parent.

## Wave: DISCUSS / [REF] User Stories

System constraints (all stories): one parent per record; installs keep **the same `ParentReferenceId` per Work Item**
before and after the upgrade, except the items named in AC-1.6 (UPGRADE-1 (A)); the migration is expand-only and writes
today's behaviour explicitly; the upgrade queues one re-read of every Jira owner and every Azure DevOps override owner;
the tracker's own parent is read only when it is in the list;
no new request on the fetch path for installs with one source, except that an Azure DevOps owner that had an override reads relations on every refresh from the upgrade on (see AC-2.4 and DEVOPS).

### US-01 — A Jira Team or Portfolio reads its parent from the first source that yields one (slice 01, ADO #6501)

As Maria Santos, config-admin whose Jira Data Center records a Work Item's Feature in a "Caused by" link for some items
and in a custom field "Program Ref" for others, while Jira's own Parent holds a housekeeping epic that is not a
Feature, I want to list where Lighthouse looks for a parent, in order, and leave Jira's own Parent out, so every Work
Item lands under the Feature it has instead of under the housekeeping epic.

`job_id: job-config-admin-name-the-link-our-hierarchy-lives-in` (revision applied in DELIVER; see JTBD One-Liner)

#### Elevator Pitch

Before: Parent Override Field takes one place (Caused by **or** Program Ref) and always falls back to Jira's own Parent,
so of Payments Core's 120 Work Items 29 sit under the housekeeping epic PAY-1 though they name PAY-12, and the 53 with
no Feature sit there too.
After: Team settings -> Advanced Configuration -> **Parent sources** -> Set here -> "Is caused by", "Program Ref",
without "Parent (Jira's own)" -> save and refresh -> 38 Work Items show under PAY-7, 29 under PAY-12, and the other 53
under no Feature, on the Team's Features view.
Decision enabled: Maria trusts the Feature forecasts for PAY-7 and PAY-12 and stops adding a default size to cover for
missing children.

#### Domain examples

1. **Happy path, two non-native sources.** PAY-142 has a link reading "is caused by" PAY-7; PAY-143 has Program Ref =
   PAY-12. Sources `[Is caused by, Program Ref]` -> PAY-142 under PAY-7, PAY-143 under PAY-12.
2. **Both present.** PAY-150 has Program Ref PAY-12 and a Caused-by link to PAY-7. `[Is caused by, Program Ref]` ->
   PAY-7. Maria swaps the order -> PAY-12.
3. **No own Parent wanted.** PAY-160 has only Jira's own Parent PAY-1. List `[Is caused by, Program Ref]` -> no parent;
   nothing is guessed. With `[Is caused by, Program Ref, Parent (Jira's own)]` it would hang under PAY-1.
4. **Link falls through.** PAY-171 has Caused-by links to INC-88 (an incident, not a Feature Lighthouse holds) only, and
   Program Ref PAY-12. The link entry yields nothing after the Story 6301 narrowing, so the next entry gives PAY-12.
5. **Portfolio.** Feature PAY-7 has no Program Ref and a Caused-by link to PRG-2 -> parent row PRG-2, same rule.
6. **Existing install.** Hans Weber's Team has Parent Override Field = Caused by. After the upgrade the list reads
   "Is caused by", then "Parent (Jira's own)"; every Work Item whose override yielded a parent has the parent it had, and an
   item with no Caused-by link and a Jira Parent keeps hanging under that Parent (Jira already fell back to its own
   Parent for an empty override). What can move: on Data Center an item with no Jira Parent now reads the instance's
   parent field, and an item whose only Caused-by link is no Feature Lighthouse holds falls through to the next entry.
   A Team with no override reads `[Parent (Jira's own)]` and changes nothing.

#### Acceptance Criteria

- AC-1.1 A Team or Portfolio on a Jira connection can hold an ordered list of one to five parent sources; each entry is
  "Parent (Jira's own)" or an Additional Field of the connection (a field or a link type phrase).
- AC-1.2 A Work Item that has a parent in two listed places shows under the one named by the earlier entry, and moving
  the later entry up and refreshing moves it under the other.
- AC-1.3 Jira's own Parent is used for a Work Item only when "Parent (Jira's own)" is in the list, and then at the
  position the list gives it. A list without it leaves a Work Item whose only parent is Jira's own under no Feature.
- AC-1.4 A Work Item that no listed source gives a parent has no parent; nothing is guessed.
- AC-1.5 Within a link entry, several candidates are narrowed and placed exactly as Story 6301 does; when no candidate
  is a Feature Lighthouse holds, the next entry is read. A field with a value wins even when it names a non-Feature.
- AC-1.6 A Team or Portfolio the upgrade migrated shows exactly one visible list (`[X, Parent (Jira's own)]` for override
  X, `[Parent (Jira's own)]` for none) and every Work Item whose override yields a parent, or whose owner had no override,
  keeps the parent it had. The upgrade re-reads every Jira owner and every Azure DevOps override owner once, and these
  items move: on Data Center an item with an empty Jira Parent now reads the instance's parent field; on Azure DevOps an
  item of an override owner with no override value now hangs under its native parent; and on any tracker with link
  entries an item whose only link candidate is no stored Feature falls through to the next entry. On Jira an item with
  an empty override already fell back to Jira's own Parent, so it does not move.
- AC-1.7 Saving a different list (an entry added, removed or moved) refetches the owner's items on the next refresh.
- AC-1.8 An entry matching nothing on this tracker is reported in Refresh History per entry and does not stop the next
  entries being read.
- AC-1.9 An Additional Field cannot be removed from its connection while any Team or Portfolio lists it; the refusal
  names the owner.
- AC-1.10 A sixth entry, a repeated entry and an empty list are refused on save with a plain message, and nothing is saved.
- AC-1.11 While "Parent (Jira's own)" is not in the list, the settings say so and name the consequence before saving.

#### UAT scenarios (BDD)

```gherkin
Scenario Outline: A Work Item lands under the Feature named by a non-native source
  Given Maria's <owner> lists "Is caused by" then "Program Ref" as parent sources
  And <item> has <record>
  When <owner> refreshes
  Then <item> shows under <parent>

  Examples:
    | owner                         | item    | record                                     | parent |
    | Payments Core                 | PAY-142 | a link reading "is caused by" PAY-7        | PAY-7  |
    | Payments Core                 | PAY-143 | Program Ref = PAY-12 and no Caused-by link | PAY-12 |
    | Contoso Programme (Portfolio) | PAY-7   | a Caused-by link to PRG-2                  | PRG-2  |

Scenario: The earlier source wins and moving it changes the result
  Given PAY-150 has Program Ref PAY-12 and a link reading "is caused by" PAY-7
  When Payments Core refreshes with "Is caused by" first
  Then PAY-150 shows under PAY-7
  And after Maria moves "Program Ref" to the top and refreshes, PAY-150 shows under PAY-12

Scenario: A link with no Feature behind it hands over to the next source
  Given PAY-171 has a Caused-by link to INC-88 only, which is not a Feature Lighthouse holds
  And PAY-171 has Program Ref = PAY-12
  When Payments Core refreshes
  Then PAY-171 shows under PAY-12

Scenario: Without Jira's own Parent in the list a native-only Work Item has no Feature
  Given Payments Core lists "Is caused by" then "Program Ref" and not "Parent (Jira's own)"
  And PAY-160 has only the Jira parent PAY-1 and PAY-1 is the housekeeping epic
  When Maria opens the Parent sources
  Then the settings say that Work Items whose only parent is Jira's own get no Feature
  And after the Team refreshes PAY-160 has no Feature and nothing is guessed

Scenario: An existing install keeps the parents it had and moves only the items named
  Given Hans Weber's Team named "Caused by" as its only Parent Override Field
  And another Team had no Parent Override Field
  And item PAY-310 of Hans Weber's Team has a Caused-by link to Feature PAY-7
  And item PAY-311 has no Caused-by link and the Jira Parent PAY-1
  And item PAY-312 has a Caused-by link to the incident INC-88 only, and the Jira Parent PAY-1
  When both Teams refresh after the upgrade
  Then PAY-310, PAY-311 and every Work Item of the other Team have the parent they had before
  And PAY-312, which hung under INC-88, now shows under PAY-1
  And Hans Weber's list reads "Is caused by" then "Parent (Jira's own)"
  And the other Team's list reads "Parent (Jira's own)"
  And the upgrade queued one re-read of each Jira owner

Scenario: A source that matches nothing is named and the next one is still read
  Given Maria listed an Additional Field whose reference matches no field or link type on the Jira site
  When the Team refreshes
  Then Refresh History names that entry
  And the remaining entries still give parents

Scenario Outline: A list that cannot be kept is refused
  Given Maria has opened the parent sources of Payments Core
  When she <action>
  Then nothing is saved and she is told <message>

  Examples:
    | action                                                            | message                               |
    | saves a list of six sources                                       | "A list holds at most 5 sources"      |
    | saves "Is caused by" twice                                        | "Is caused by is already in the list" |
    | saves a list of no sources                                        | "A list needs at least one source"    |

Scenario: A field a Team lists cannot be removed from the connection
  Given Maria's Payments Core lists "Program Ref" among its parent sources
  And a System Admin has opened the Jira connection page
  When they remove "Program Ref" from the connection and save
  Then nothing is saved and they are told "Program Ref is used by Payments Core"
```

AC coverage: AC-1.1/1.2 scenarios 1-2; 1.3/1.4/1.11 scenario 4; 1.5 scenario 3; 1.6 scenario 5; 1.7 is pinned in
scenario 2 (the move refetches) and by the fingerprint test
`Saving_a_different_list_moves_the_owners_fingerprint_and_a_follower_renders_the_defaults_list`; 1.8 scenario 6; 1.9/1.10 scenario 7.

#### Technical notes

- Selector: `ParentSourceSelector.ReadsTheTrackersOwnParent` (`!HasValue`) is today the single place that decides whether
  the tracker's own parent is read; it becomes "is the own Parent in the list, and where". Anything that asks it (what to
  fetch, what to read it from) must follow, as its own doc says. Jira builds the record with `issue.ParentKey` first
  (line 1879): the list decides whether that value is kept.
- The list travels into the connector as data from the owner (see Context-Map Placement). Candidates of several link
  entries must reach the 6301 placement step in `WorkItemService` (`ParentCandidatesFromLinks` carries one set today).
  DESIGN decides the shape.
- Fingerprint, connection-save guard, refresh findings and the field list a connector requests all read the single id
  today and must read every entry and its position.
- Storage: expand-only, additive, through the `CreateMigration` script; on the shared base. The migration writes the
  explicit list (override X -> `[X, own Parent]`, none -> `[own Parent]`) and must not change the fingerprint of an
  unchanged install. Shape is DESIGN's.
- Dependencies: none open. Shipped: Epic 6028, Bug 6203, Story 6301.

#### Outcome KPIs

- **Who**: config-admins whose Work Items record their parent in more than one non-native place, or whose own Parent
  is not a Feature.
- **Does what**: have Work Items resolve to the Feature the tracker names for them, instead of a fallback or nothing.
- **By how much**: hypothesis until measured: on the first adopting instance, Work Items that name a Feature in a
  listed place but sit elsewhere drop to 0%. Baseline from the reporter (Q7, to be measured); illustrative 29 of 120
  Payments Core items. Guardrail: **the same `ParentReferenceId` per Work Item for installs that do not touch the list,
  except the items AC-1.6 names**.
- **Measured by**: the reporter's Team before and after (count of items under the Feature they name), and the acceptance
  suite for the guardrail.
- **Baseline**: to be measured on the reporter's Team (Q7); illustrative 29 of 120 under the wrong parent.

### US-02 — An Azure DevOps Team or Portfolio does the same (slice 02, ADO #6501)

As Tomas Novak, config-admin on Azure DevOps whose Work Items keep the Feature in a custom "Epic Link" field for some
items and in a second custom field "Program Ref" for others, and whose native parent link points at a work package that
is not a Feature, I want the same ordered list, so both kinds of item land under their Feature.

`job_id: job-config-admin-name-the-link-our-hierarchy-lives-in`

#### Elevator Pitch

Before: Parent Override Field takes one field and falls back to the native parent, so items kept in the second custom
field land under the work package, and Tomas cannot say "never the native parent".
After: Team settings -> Parent sources -> Set here -> "Custom.EpicLink", "Custom.ProgramRef" (no native parent) -> save
and refresh -> the Team's items show under their Features whichever field held the parent.
Decision enabled: Tomas stops copying parents into one field with a pipeline.

#### Domain examples

1. Item 4711 has `Custom.EpicLink = 9001` -> parent 9001.
2. Item 4712 has no `Custom.EpicLink`, `Custom.ProgramRef = 9100` -> 9100.
3. Item 4713 has neither and a native parent 9050. Without the native entry -> no parent; with `[Custom.EpicLink,
   Parent (Azure DevOps' own)]` -> 9050.
4. A relation type listed as a source reads nothing and Refresh History says so, as today (A7).
5. Existing install: a Team with override `Custom.EpicLink` reads `[Custom.EpicLink, Parent (Azure DevOps' own)]` after
   the upgrade; items whose override yields a parent keep it, and an item of that Team with no override value and a native
   parent now hangs under the native parent (one re-read).

#### Acceptance Criteria

- AC-2.1 The ordered list works on an Azure DevOps connection with fields and "Parent (Azure DevOps' own)".
- AC-2.2 The first entry that yields a value wins; the native parent is read only when listed; a Team with an unchanged
  single override or none has the same parents after the upgrade, except the items AC-1.6 names (migration, one re-read
  of the override Teams).
- AC-2.3 A relation type listed as a source is reported in Refresh History, per entry, and skipped.
- AC-2.4 A Team whose list names only fields shows the same dependencies as before the upgrade, and a Team that lists
  the native parent shows its items under their native parents; neither makes more requests than before for an install
  with one source.
- AC-2.5 After the list of a Team is changed, the next refresh refetches that Team's items; and an Additional Field the
  list names cannot be removed from the connection (the refusal names the Team).

#### UAT scenarios (BDD)

```gherkin
Scenario: The second custom field gives the parent when the first is empty
  Given Tomas listed "Custom.EpicLink" then "Custom.ProgramRef"
  And item 4712 has only "Custom.ProgramRef" = 9100
  When the Team refreshes
  Then item 4712 shows under 9100

Scenario: The native parent link is read only when it is listed
  Given item 4713 has no custom value and the native parent 9050
  When the Team lists "Custom.EpicLink" then "Parent (Azure DevOps' own)" and refreshes
  Then item 4713 shows under 9050
  And when the Team lists "Custom.EpicLink" only and refreshes, item 4713 has no parent

Scenario: A relation type in the list is named, not guessed
  Given Tomas listed an Additional Field referencing "System.LinkTypes.Related"
  When the Team refreshes
  Then Refresh History says that entry reads nothing as a parent
  And the other entries still give parents

Scenario: An existing install keeps the parents it had and moves only the items named
  Given a Team named "Custom.EpicLink" as its only Parent Override Field before the upgrade
  And item 4713 has no "Custom.EpicLink" value and the native parent 9050
  When it refreshes after the upgrade
  Then every item whose "Custom.EpicLink" gave a parent has the parent it had before
  And item 4713 now shows under 9050
  And its list reads "Custom.EpicLink" then "Parent (Azure DevOps' own)"

Scenario: A Team that reads only fields leaves its Portfolio's dependencies as they were
  Given the Portfolio "Retail Rollout" reads its dependencies from the Predecessor relation
  And Feature 4711 waits on 4702 through that relation
  And the Team "Payments Mobile" lists "Custom.EpicLink" only
  When "Payments Mobile" refreshes and then "Retail Rollout" refreshes
  Then Feature 4711 still waits on 4702 only, as it did before the Team's list was saved

Scenario: Changing the list refetches, and a field in use cannot be removed
  Given Tomas saved a different order of the Team's parent sources
  When the Team's next refresh runs
  Then it refetches the Team's items
  And when a System Admin removes "Custom.ProgramRef" from the connection and saves, the save is refused, naming the Team
```

#### Technical notes

- `AzureDevOpsWorkTrackingConnector.GetParentReference` (`:1516-1527`) and the relations read (`:1384-1418`) are the two
  sites; the override-set-skips-relations optimisation (`:1398-1406`) must be restated for a list.
- Dependencies: slice 01's model, migration, API and UI. Released only after slice 01.

#### Outcome KPIs

- **Who**: config-admins on Azure DevOps with parents in several fields. **Does what**: get items under their Features
  without copying parents into one field. **By how much**: hypothesis: items placed under the Feature their field names
  reach 100% on the reporting customer's Team once measured; guardrail: the same `ParentReferenceId` per item for
  installs that do not touch the list, in 100% of the pin scenarios. **Measured by**: acceptance suite plus the
  reporter's count. **Baseline**: one source only; real count to be measured (Q7).

### US-03 — The connection holds the parent sources as the default (slice 03, ADO #6501)

As Maria Santos with fourteen Teams on one Jira connection, I want to set the parent sources once on the connection, so
new and existing Teams follow it without opening each one.

`job_id: job-config-admin-set-a-connection-setting-once`

#### Elevator Pitch

Before: the connection's Parent Override defaults for Teams and Portfolios each take one field.
After: Settings -> Connections -> the Jira connection -> Defaults -> Parent Override (Teams) shows the ordered list;
Teams that "follow the connection default" read it, and the Team settings show "Connection default: Is caused by, then
Program Ref".
Decision enabled: Maria sets the order once and sees how many Teams follow it before she saves (the existing owner
count).

#### Domain examples

1. Default for Teams = `[Is caused by, Program Ref]`. 11 Teams follow, 3 own theirs; saving asks to refresh the 11.
2. A new Team created with "follow default" reads the list on its first refresh.
3. Portfolios keep their own, separate default (they name different links).
4. An old Teams default "Caused by" alone reads `[Is caused by, Parent (Jira's own)]` after the upgrade.

#### Acceptance Criteria

- AC-3.1 The connection's Teams default and Portfolios default each hold an ordered list under the same rules as an
  owner's (one to five entries, no duplicate).
- AC-3.2 A change of the default refreshes only the owners that follow it, and they show and read the new list.
- AC-3.3 An existing single default is shown as `[default, own Parent]` and an unset default as `[own Parent]`, and the
  followers keep the parents they had.
- AC-3.4 An owner that owns its list is untouched by a change to the default.
- AC-3.5 Saving the connection is refused when a default names an Additional Field the connection no longer holds; the
  refusal says which default.

#### UAT scenarios (BDD)

```gherkin
Scenario: Teams that follow the default read its order
  Given 11 Teams follow the Jira connection's Parent Override default for Teams
  When a System Admin saves "Is caused by" then "Program Ref" as that default
  Then the 11 followers are listed for a refresh
  And their Work Items resolve parents in that order after it

Scenario: A Team with its own list is left alone
  Given the Payments Core Team owns "Program Ref" then "Is caused by"
  When the connection default changes
  Then Payments Core reads its own list unchanged

Scenario: A new Team follows the default
  Given the connection default for Teams is "Is caused by" then "Program Ref"
  When Maria creates the Team "Payments Mobile" without choosing a Parent source
  Then it follows the connection default

Scenario: An old single default keeps its result
  Given the Teams default was "Caused by" alone before the upgrade
  When the connection is read after the upgrade
  Then the default lists "Is caused by" then "Parent (Jira's own)"
  And the Teams that follow it have the parents they had

Scenario: An unset default shows the tracker's own parent as the only source
  Given nobody has ever set the Teams default on the Jira connection
  When Maria opens a Team that follows that default
  Then its list reads "Parent (Jira's own)" only

Scenario Outline: A default list is held to the same rules as a Team's
  Given a System Admin has opened the Teams default of the Jira connection
  When they <action>
  Then nothing is saved and they are told <message>

  Examples:
    | action                          | message                          |
    | save a list of six sources      | "A list holds at most 5 sources" |
    | save "Is caused by" twice       | "Is caused by is already in the list" |
    | save a list of no sources       | "A list needs at least one source" |

Scenario: A default cannot name a field the connection lacks
  Given the Teams default lists "Program Ref"
  When a System Admin removes "Program Ref" from the connection and saves
  Then the save is refused and says the Teams default still uses it
```

#### Technical notes

- `ConnectionDefaultsDto` (`ParentOverrideForTeams`, `ParentOverrideForPortfolios` ints), `ConnectionFieldDefaults`, the
  `ParentOverrideConnectionDefault` virtuals on the owners, `ConnectionSaveService` follower counts.
- Dependencies: slice 01; released only after it. If Epic 6473 has moved the connection page, edit it where it is.

#### Outcome KPIs

- **Who**: config-admins with several Teams on one connection. **Does what**: set the sources once. **By how much**:
  one save instead of N Team edits (hypothesis, N from the reporter's connection, Q7); guardrail: owners that own their
  list unchanged in 100% of scenarios. **Measured by**: acceptance suite; qualitative from the reporter. **Baseline**:
  N Team edits.

## Wave: DISCUSS / [REF] Alternatives considered

- **Several real parents per record** (the old title's literal reading). Rejected: a record holds exactly one parent and
  the roll-up, forecast and Delivery logic read that one; multi-parent was parked for after v1 (Epic 6455).
- **Walk the sources and merge** (a record under every parent found). Same rejection.
- **N numbered selects** ("Parent Override Field 2, 3"). Rejected for the UI; see sketch.
- **A rule language** ("Parent else link X"). Rejected: more power than the stated problem needs.
- **The own Parent always last.** Rejected by the maintainer: an install may want no own Parent at all, and it could not
  express the maintainer's own example (own Parent first).

## Wave: DISCUSS / [REF] Out-of-scope

- Several parents per record, multi-valued parents (parked by Epic 6455).
- Link-typed or relation-typed **levels** and the levels work (Epics 6455/6474).
- Relation types as parent sources on Azure DevOps (A7).
- Linear, CSV, ServiceNow behaviour (A6); hiding the control there.
- Showing, per record, which source supplied its parent.
- Deliveries, Feature WIP and any roll-up change.

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

Strategy A (real, thinnest end-to-end): slice 01 on Jira, Team and Portfolio together, from the settings list through the
real connector parse (recorded payloads of an issue with a native parent, a Caused-by link and a custom field) to the
stored `ParentReferenceId`, including the migration of a single override. No live Jira is needed for DISCUSS; the
Story 6301 live probes already cover the link payload.

## Wave: DISCUSS / [REF] Driving Ports

- `PUT /api/latest/teams/{id}` and `PUT /api/latest/portfolios/{id}` (settings DTO, additive field), slice 01.
- `PUT` of the connection with defaults (`ConnectionDefaultsDto`), slice 03.
- Team and Portfolio settings page, connection page (UI).
- The refresh itself (scheduled or manual) as the trigger that applies the list.

## Wave: DISCUSS / [REF] Pre-requisites

| # | Pre-requisite | State |
|---|---|---|
| P1 | Epic 6028, Bug 6203, Story 6301 shipped | Done |
| P2 | The customer's real item shapes (items per source, which wins when several are set) | Open, Q7, not blocking |
| P3 | Maintainer walk-through of the sketch | Start of DISTILL |
| P4 | Slices 02 and 03 wait for slice 01 | Tracked in the slice order |

## Wave: DISCUSS / [REF] Definition of Ready (8 items, KPIs as an extra check)

Gate: the 8 items. Outcome KPIs are checked in addition.

| # | Item | Evidence |
|---|---|---|
| 1 | Problem in domain language | Real gap restated: several non-native sources and an own Parent that is not wanted; Maria's 29 of 120 under the housekeeping epic |
| 2 | Persona with characteristics | Maria Santos, config-admin, Jira DC, 14 Teams; Tomas Novak on ADO |
| 3 | 3+ domain examples with real data | US-01: six; US-02: five; US-03: four |
| 4 | UAT in Given/When/Then, 3-7 scenarios | US-01: 8 (two are outlines; the field-in-use rule was split out at review so its Given is the connection page); US-02: 6; US-03: 7 (one outline, one for the unset default) |
| 5 | AC derived from UAT | AC coverage line under US-01; AC-2.x and most of 3.x map to a UAT scenario; AC-3.1 and the unset default in AC-3.3 are pinned by the list rule tests and the DISTILL tests instead |
| 6 | Right-sized | Slice 01: 2-3 days (top of the band, watch item); 02: 1; 03: 1; total 4-5 days |
| 7 | Technical notes | Each story |
| 8 | Dependencies resolved or tracked | P1 done; P2 open and not blocking; slices 02 and 03 depend on slice 01 |
| + | Outcome KPIs (extra check) | Each story, baseline to be measured (Q7), target stated as a hypothesis |

Elevator pitch present in all three. `job_id` uses existing jobs (B1 resolved). JTBD revision pending in DELIVER (SSOT
edit). Peer review: the first review returned NEEDS_REVISION; this is the revised draft, **re-review pending**.

## Wave: DISCUSS / [REF] House Checklist (every item answered)

- **RBAC impact: none, because** the list is saved through the existing Team, Portfolio and connection endpoints with
  their existing guards, adds no route, page or permission, and the UI gating stays through `useRbac()`; no component
  fetches `my-summary`. The connection defaults stay with whoever may save a connection today.
- **Lighthouse-Clients CLI/MCP versioning: N/A, because** a grep of `/storage/repos/lighthouse-clients` (excluding
  `node_modules`) finds no `parentOverride` and no `additionalFieldDefinitions` use; the clients read Team settings as
  loose records and the wire change is additive. Re-grep at finalize.
- **Website marketing surface: N/A (expected), because** no marketing page is known to describe the Parent Override
  Field or link-based parentage and no screenshot of this control is hot-linked. Finalize re-runs
  `grep -rniE "parent override|issue link|parent source"` over `/storage/repos/website`.
- **Docs at finalize (not now):** `docs/teams/edit.md` and `docs/portfolios/edit.md` (Parent Override Field becomes
  Parent sources, with the order rule, the optional own Parent and the migration), `docs/settings/worktrackingsystems.md`
  (defaults), `docs/concepts/worktrackingsystems/azuredevops.md:222`, `docs/settings/systeminfo.md` (Refresh History
  per-entry finding), per-feature screenshots of the new list, demo data (a Team with two sources). Release notes are
  written at `/release`; the ADO Story carries the `Release Notes` tag.
- **Usage data (DEVOPS) proposal: one name-only event**, emitted when an owner or default is saved with two or more
  parent sources, appended to `UsageDataEventName` with the next integer and listed in `docs/settings/usagedata.md`; no
  properties. Emitted in DELIVER in slice 01. Settled in DEVOPS (Q6).
- **Continuous-improvement check: N/A, because** this is a standalone Story, not a new feature Epic, so the
  `/continuous-improvement before <epic-id>` check at DISCUSS does not apply.
- **Context map:** answered above (3 contexts, 2 terms, no new event, no new edge, same UI surface).
- **Terminology:** prose uses Feature, Work Item, Team, Portfolio (seeded defaults); UI copy goes through `getTerm`.
  Literal tracker values stay literal: "Is caused by", "Parent (Jira's own)", "Custom.EpicLink".
- **Visualization skill: N/A, because** nothing shows data (a settings list).
- **Sketch before building:** done above; walked through at the start of DISTILL.
- **Slow connection (DESIGN):** loading and failure looks are a DESIGN task in the sketch notes.
- **Mutation testing:** per-feature, both stacks; targets are the selector and the list reducer.
- **No nWave scenario tags in code** (DISTILL reminder). Keep the classification in this workspace.
- **ADO:** nothing touched by this run. The Story is standalone (no parent Epic), titled "Ordered parent sources", with
  the `Release Notes` tag (maintainer decisions).

## Wave: DISCUSS / [REF] Risks

| # | Risk | P | I | Response |
|---|---|---|---|---|
| R1 | "First that yields" picks a parent the user did not mean (a native Epic listed first) | M | M | The order is the user's and visible; helper text says so |
| R2 | A migration colliding with Epic 6474's table restructure | M | M | Store on the shared base; 6501 ships first, 6474 carries the column |
| R3 | Slice 01 over 3 days | M | L | Split Team from Portfolio |
| R4 | The customer's real data is never seen (A8) | M | M | Ask the reporter for one Team's counts before DELIVER (Q7) |
| R5 | UI adds weight to a settings page that Epic 6473 is about to restructure | L | L | Same group, same place, moves with it |
| R6 | An install drops its own Parent and a native-only item silently loses its Feature | M | M | The consequence line before saving (AC-1.11); the migration keeps the own Parent in the list |

## Wave: DISCUSS / [REF] Changed Assumptions

None against DISCOVER (none exists). Against the first draft: the problem is no longer "link-only items are not placed"
(the single override already does that) but "several non-native sources, and an own Parent that is not wanted".

## Wave: DISCUSS / [REF] Resolved and still open

The first draft's twelve open questions are answered in the table "Maintainer decisions (2026-11-02)" below; that
table is the record and supersedes the draft. Four of its rows stand as recommendations and are not asked again (the
numbering is the table's own): Q4 Teams and Portfolios together in slice 01; Q6 the usage-data event is settled in
DEVOPS; Q7 ask the reporter for item counts before DELIVER, not blocking; Q11 the control stays on Linear/CSV.
Still open for the maintainer: nothing.

### First DISCUSS review: closure

One row per finding of the first DISCUSS review, and where it is addressed in this document.

| Finding | Was | Addressed in |
|---|---|---|
| B1 | The `job_id` did not exist in `jobs.yaml` | "JTBD One-Liner": reuses `job-config-admin-name-the-link-our-hierarchy-lives-in` (revision to apply) and `job-config-admin-set-a-connection-setting-once`; no new job |
| B2 | The "Before" premise contradicted the code: the single override already falls back to the own Parent | "Current-State Surface Inventory" (Jira row, "Today the order is fixed and implicit") and the first row of "Maintainer decisions"; the problem is restated as several non-native sources and an unwanted own Parent |
| M1 | Missing UAT scenarios for AC-1.9, AC-1.10, AC-2.5 and AC-3.5 | The scenarios under US-01 (AC-1.9, AC-1.10), US-02 (AC-2.5) and US-03 (AC-3.5) |
| M2 | Slice dependency versus independence | "Story Map & Slices" and each slice file's "Release order": slice 01 first and standalone, slices 02 and 03 depend on it and are droppable |
| M3 | DoR count and size inconsistency | "Definition of Ready (8 items, KPIs as an extra check)" and "Scope Assessment"; slice sizes (2-3 days, 1, 1) match the Story total of 4-5 days |
| M4 | KPI measurability and the byte-identical guardrail | The "Outcome KPIs" under each story; the guardrail is now a stated test (golden fingerprints, migration test) and its one exception is the set of items AC-1.6 names (UPGRADE-1 (A), DD-4) |
| M5 | 6028/6203 evidence | "Prior-Wave Reading Confirmation": the feature folders are absent, so the evidence is the shipped code and `docs/portfolios/edit.md` |
| m1 | Cross-Epic D-ids | "Standalone Story: relationship to Epics 6455, 6473 and 6474": decisions cited by title, not by id |
| m2 | Q-range references | "Resolved and still open": the numbering is the maintainer table's own |
| m3 | ACs that pre-decide or describe implementation | The acceptance criteria under each story are phrased as observable behaviour; storage and ordering mechanics sit in DESIGN |
| m4 | Context-map wording (data handed in) | "Context-Map Placement (ADR-270)": "How the list reaches the connector: as data from its owner" |
| m5 | Sketch border | "UI Sketch (to be walked through at the start of DISTILL)" |

## Maintainer decisions (2026-11-02, after the first DISCUSS draft)

Binding. They supersede the draft above where the two disagree.

| Topic | Decision |
|---|---|
| The real gap | **One override is not enough because the parent can sit in two or more non-native places** (custom fields and/or link phrases). An install may explicitly NOT want the tracker's own Parent at all, only two other Additional Fields. The setting is **1..n sources, ordered; the first one that yields a parent wins.** (The draft's "Before" story is wrong: today's single override already falls back to the own Parent, so "override = Is caused by" already places link-only items. Restate the problem around "several non-native sources" and "own Parent not wanted".) |
| Q1 own Parent | **The tracker's own Parent is an optional entry like any other,** movable, and only read when it is in the list. It is **not** an implicit last fallback. Migration keeps today's behaviour by writing it explicitly: override X becomes `[X, own Parent]`; no override becomes `[own Parent]`. |
| Q2 fall-through | **Yes.** A link source whose candidates contain no Feature Lighthouse holds (after the Story 6301 narrowing) yields nothing, and the next source is read. A plain field value still wins as today. |
| Q3 + Q10 limits | **At most 5 entries. A duplicate is rejected on save** with a plain message. |
| Q5 + Q12 | **Standalone Story**, no parent Epic. ADO 6501 now carries the `Release Notes` tag. |
| Q8 title | **Renamed in ADO to "Ordered parent sources".** |
| Q9 timing | **Ship now**, before Epic 6474. 6474's migration carries the extra column. |
| Q4, Q6, Q7, Q11 | Not asked individually; the recommendations stand unless the maintainer says otherwise at DESIGN: Teams and Portfolios together in slice 01 (Q4); the usage-data event is settled in DEVOPS (Q6); ask the reporter for item counts before DELIVER, not blocking (Q7); leave the control on Linear/CSV (Q11). |

## Wave: DESIGN / [REF] Design (Morgan, PROPOSE, 2026-11-02)

Scope: application architecture. Nothing committed, ADO untouched, no file outside this folder edited. The ADR draft is
`adr-draft-ordered-parent-sources.md` (this folder); it is promoted to `docs/product/architecture/` in DELIVER, with
the ADR-270 / `ARCHITECTURE.md` §4 / `jobs.yaml` edits DISCUSS listed. Maintainer decisions MD-1..MD-8 are binding and
are not re-opened. Prior reading: ADR-193 (parent source is a declared connector capability; `ParentSourceSelector`
is the one tracker-neutral place), ADR-241 (own value + source, the owner answers with the effective value), ADR-242
(a connection save queues the owners whose fingerprint moved), ADR-270/271, and Epic 6474's owner-table inventory.

### Context-map placement (DESIGN confirms DISCUSS)

Contexts: Tracker integration (readings), Organisation set-up (the list is Mapping), Work Items (the one stored
`ParentReferenceId`, the 6301 placement). No new edge, no domain event: the list reaches the connector as data on the
owner, and a change reaches Work Items through the fetch fingerprint (ADR-242). Event weighed and rejected, DD-8.
UI surface unchanged (unit settings tab, Advanced Configuration; connection page for defaults).

### What exists and is reused (no new component without "no alternative")

| Existing | Used as | Contract shape (principle 12): shape, universe, delta, assertion |
|---|---|---|
| `ParentSourceSelector` (static, tracker-neutral, ADR-193 §2) | **Becomes the one home of the ordering rule**: `Choose` and `ChooseProvisionally` | pure-function: readings (+ stored Features, narrowing) in, `ParentChoice` out; no I/O, no mutation; asserted by the table test with no fakes |
| `ParentSourceSelector.HangUnder` (**modified**, the one and only writer of a parent; DD-3) | Applies a `ParentChoice` to a record: always sets `ParentReferenceId` to the choice's key, **an empty key clears it**; shows the winning link entry's key in that entry's Additional Field and **clears the shown value of every other link entry of the list**. Today it writes `ParentReferenceId` only for a non-empty key and only the single override slot (`ParentSourceSelector.cs:70-78`, called at `WorkItemService.cs:245`), which cannot retract. **Order (Jira):** the clear runs after `PopulateAdditionalFieldValues` (today `JiraWorkTrackingConnector.cs:1895`, then the hang at `:1897`), so the populate step cannot write a cleared value back. **ADO:** the direct writes at `AzureDevOpsWorkTrackingConnector.cs:148` and `:694` go through `HangUnder` (slice 02) | bounded-change. Universe: the one record passed in. Delta: `ParentReferenceId` and the `AdditionalFieldValues` entries of the link entries named by the record's `ParentReadings` (which positions are link entries is read from the readings, so the helper knows no connector), nothing else. Idempotent: applying the same choice twice leaves the record equal. Assertion: record compared by value before and after, minus those keys; a retraction table (key present then empty choice; winner moved from one link entry to another) |
| `ParentResolution` + `Place` (Story 6301 narrowing) | **Modified (H-1, DD-3)**: a single candidate is no longer "not asked"; it goes through the same narrowing. Only an empty set stays `NotAsked`. Doc comment, `ParentResolutionPlacementTest` and the service filter change with it | pure-function; asserted by the placement table, lone-candidate rows added |
| `WorkItemService.PlaceWhatTheLinksLeftOpen` | **Generalised**: runs on every record whose provisional choice needs stored Features (DD-3) | bounded-change. Universe: the records of the one download handed in (a Team's work items, a Portfolio's Features, or the parents above them), and no stored row, no repository write. Delta: `ParentReferenceId` and the link entries' shown field values on records that need stored Features; every other record is equal by value before and after. It reads the stored Features at most once. Assertion: a mixed-download test comparing each untouched record, and a repository double that fails on any write. Placement runs over the `NeedsStoredFeatures` records, but the counts and warnings (`:184-195`, `:220-229`, `RecordsPlacedFromLinksNamingMoreThanOneParent` / `RecordsWhoseLinksNamedMoreThanOneParent`, the per-record log at `:222-225`) are computed only over the `IsAmbiguous` subset; `HangUnder` also runs on `NotPlaced` records in the second pass (today's call at `:243-246` is `Placed`-only), so a failed Feature test retracts a provisional key (DD-3) |
| `WorkItemBase.ParentReadings` (replaces `ParentCandidatesFromLinks`) | Carries the ordered readings from the connector pass to the second pass. **The copy constructor (`WorkItemBase.cs:11-18`, which today copies `ParentCandidatesFromLinks` at :16) copies `ParentReadings` instead; `Feature` (`Feature.cs:35`) and `WorkItem` (`WorkItem.cs:10`) wrap the record through it, and the second pass runs on the wrapped record (DD-3)** | bounded-change on the record, transient (`NotMapped`): written once per record by the connector's record factory and carried across the wrap by the copy constructor, read by the second pass, never written by anything else; never stored. `Update(...)` (`WorkItemBase.cs:170`, the stored-record merge) does not copy it |
| `StoredSetting` / `SettingSource` / ADR-241 mutators | Reused for follow-or-own of the list | pure value type; owner mutators are bounded-change. Universe: the one owner (and, for the connection, its two list members). Delta: own source, own list, and the write-through legacy column. Assertion: owner compared by value before and after |
| `FetchFingerprint` | Renders the list | pure-function; legacy shapes golden-pinned |
| `ConnectionFieldDefaults` registry, `ConnectionDefaultsDto` | Extended with a list-valued default, as Estimation already is | registry is data; DTO bounded-change on the connection's two list members |
| `OwnerSettingRefusals`, `ConnectionSaveService.FieldsStillInUseAmong` | Extended: read every entry | read-only checks; the first returns a refusal value, the second a list; the second's EF predicates are replaced (DD-5) |
| `LighthouseAppContext` JSON `HasConversion` + `ValueComparer` (StateMappings, CycleTimeDefinitions) | Storage precedent, serialiser options copied exactly (DD-1) | n/a |
| `ConnectionDefaultFieldSelect`, `FormSelectField`, `followOrOwn` | Row picker and follow/own switch | n/a |

Genuinely new, each with no existing alternative (the single `int?` cannot hold an ordered list, and the selector has
no input shape for several readings), and each with its contract shape declared:

| New | Contract shape | Universe / assertion |
|---|---|---|
| `ParentSources` (value object) | pure-function / immutable value: every operation (add, remove, move, projection to the legacy int, rendering) returns a new value or a plain result; the factory returns the list or the violations | No universe: nothing it touches. Asserted by property tests (1..5, no duplicate, order preserved, round trip through the serialiser) and by the type having no setters |
| `ParentSourceEntry` | pure value; the invalid states (TrackerParent with a field id, AdditionalField without one) cannot be built | Same |
| `ParentReadings` | pure value, positional with the list it was read from; built once per record by the adapter, never mutated afterwards; the record only holds it | Same; the connector tests assert it is set for every record, the selector tests assert it is not mutated by `Choose` |
| `ParentChoice` | pure value: key, winner position, `NeedsStoredFeatures` | Same |
| `ParentSourcesList` (UI component) | pure controlled component: renders its `value`, emits the whole new list through one `onChange`; the reducer is pure | No effects but rendering; asserted by RTL and by mutation testing of the reducer |

### Constraint and priority data

The behavioural surface is small and measured by grep (2026-11-02), but **the compiler does not name every reader**:
removing the effective, unmapped `ParentOverrideAdditionalFieldDefinitionId` from `IWorkItemQueryOwner` names the
connectors, the selector, the fingerprint and the DTO, but not the readers of the concrete mapped property
`OwnParentOverrideAdditionalFieldDefinitionId` or of the connection's `ParentOverrideForTeams` / `ParentOverrideForPortfolios`.
The full reader list, to be moved to the list (or deleted) in the slice that owns it:

| Reader | Where | Moves to |
|---|---|---|
| EF predicates and projections on `Own...` | `ConnectionSaveService.cs:91-124` (`TeamsOwningAnyOf` at 91-98, `PortfoliosOwningAnyOf` at 100-124) | connection-scoped load plus in-memory scan of each owner's list (DD-5) |
| Parent field requested / relation-type finding / relations-skip | `AzureDevOpsWorkTrackingConnector.cs:779, 958, 964, 1398-1401, 1519` (`1398-1406` decides whether relations are read at all, including the `CouldNotTellWhatItNames` branch at :1399-1401: relations are kept when a field cannot be classified, because the reference may well be a relation type) | the list. Reading per entry: a classified field is `FieldValue`; a relation-type entry is `CannotBeRead` plus the finding; an entry that **could not be classified** is read as a field (`FieldValue` if the item carries a value, else `NoValue`), exactly as today. **Relations-skip rule:** relations are read when the list holds `TrackerParent` or dependencies are read, and skipped otherwise. The "could not tell" and relation-type branches no longer force the relations read on their own, because today they only did so to keep the own-parent fallback alive, and that fallback is now an explicit `TrackerParent` entry; a test pins `[X]` with an unclassifiable X (no relations read, one finding). **Found while reading the code (UPGRADE-1, DD-4):** today a classified field override with no dependencies skips the relations read (`:1403-1406`), so `GetParentReference` (`:1516-1527`) finds no native parent and the field alone decides; whether the native parent is today's fallback is decided at read time by the grain and the dependency setup (`:71-83`, `:139`, `:1398-1403`; `DependencySourceSelector.cs:32-33`), so no static migration reproduces it. What the migration does about that is UPGRADE-1 |
| Jira read, `HangUnder` | `JiraWorkTrackingConnector.cs:1914`; `ParentSourceSelector.cs:34, 58, 77` | the readings, `Choose` and the modified `HangUnder` (DD-3) |
| **Jira Data Center own-parent readers (H-5, AFK-8)** | `JiraWorkTrackingConnector.cs:327` and `:1339` (`TheFieldTheInstanceHangsParentsOn(owner, EpicLinkFieldName / ParentLinkFieldName)`, which returns empty while an override is named), `:335` and `:1350` (`TrySetParentFromTheFieldTheInstanceHangsItOn`, called after the record is built and before it is wrapped), `:1964-1967` (the field-name lookup, gated by `ReadsTheTrackersOwnParent`) and `:1981-1992` (the setter with its `IsNullOrEmpty(ParentReferenceId)` guard at :1983) | folded into the `TrackerParent` reading (below). The setter as a *writer* goes, because it assigns `ParentReferenceId` (rule 3); the field-name lookup stays, gated by "the list holds `TrackerParent`" instead of "no override" |
| Fingerprint | `FetchFingerprint.cs:50, 92` | the rendered list (DD-4) |
| DTOs and refusals | `SettingsOwnerDtoBase.cs:27, 78`, `ConnectionDefaultsDto.cs:36-45`, `OwnerSettingRefusals.cs:61`, `WorkTrackingSystemOptionsOwnerExtensions.cs:23` | list members, deprecated int members as projections (DD-6) |
| Connection default registry, follower counts, owner hooks | `ConnectionFieldDefaults.cs:31-34`, `IWorkTrackingSystemConnectionUsage.cs:21-32`, `Team.cs:33, 39`, `Portfolio.cs:107, 113`, `SupportedConnectionDefaults.cs:6, 8` | the list-valued default; the setting names stay so follower counts keep working |

Because a rename is not enough to find these, architecture rule 5 (below) fails any production file outside its
allow-list that still reads the legacy members.

**Every reader of `ParentCandidatesFromLinks` (M-5), each migrated to `ParentReadings` in slice 01.** Re-grep at the first
DELIVER step for `ParentCandidatesFromLinks`, `IsAmbiguous`, `HangUnder`, `TheParentOf` and `ReadsTheTrackersOwnParent`; the
compiler names the property readers once the property is removed, the others it does not.

| Where | Reader | Migrates to |
|---|---|---|
| `Models/WorkItemBase.cs:16` | copy constructor copies it | copies `ParentReadings` (the wrap carries the readings to the second pass; DD-3) |
| `Models/WorkItemBase.cs:79` | the property | `ParentReadings`, same `[NotMapped]`, defaults empty |
| `Jira/JiraWorkTrackingConnector.cs:1898` | `workItem.ParentCandidatesFromLinks = fromTheMatchingLinks...` | the factory builds the readings and sets `ParentReadings` once |
| `WorkItems/WorkItemService.cs:240` | `ParentResolution.From(record.ParentCandidatesFromLinks)` in `PlaceAmongTheirCandidates` | the full `Choose` over `record.ParentReadings` |
| `WorkItems/WorkItemService.cs:369` | `LinksNameMoreThanOneParent` filter (`IsAmbiguous`) | `ChooseProvisionally(record.ParentReadings, narrowing).NeedsStoredFeatures` (DD-3) |
| `Tests/.../WorkItemServiceTest.cs:1355, 1934, 2857` | arrange records with link candidates; assert placement | arrange `ParentReadings`; the scenario with one non-Feature candidate changes meaning under UPGRADE-1 (A), and stays under (B) for a legacy-mode owner (DD-3, DD-4) |
| `Tests/.../Parents/ParentSourceSelectorTest.cs:172` | `ARecordWithEveryMemberFilledIn` sets it | sets `ParentReadings`; the `TheParentOf` and `HangUnder` tests (:44-143) become `Choose` / `HangUnder` tables |
| `Tests/.../Jira/JiraParentLinkDirectionDogfoodTest.cs:126` | reads the candidates' link dates back off the records | reads them out of the `LinkCandidates` readings |
| `Tests/.../Models/ParentResolutionPlacementTest.cs:52` | the "lone link is stored as it is" assertion on `Place` (not a reader of the property, but pinned to the same behaviour) | split per DD-3 (the `none` half stays `NotAsked`; the `lone` half asserts the narrowing) |

**Rule 5's allow-list, and the legacy members that stay or go (M-4).** The scan covers production source. Allowed to name
`OwnParentOverrideAdditionalFieldDefinitionId`, `ParentOverrideForTeams` and `ParentOverrideForPortfolios` as property reads:
(1) the owner and the connection models, for the write-through; (2) `LighthouseAppContext` and the migrations;
(3) **`ConnectionDefaultsDto`, kept on purpose** for its deprecated wire members (`ConnectionDefaultsDto.cs:36-45`), which
are computed from the list through the one projection function and read no column. Allow-listed in slice 01 (so rule 5 lands there) and removed from the allow-list in **slice 03, which migrates them** (M-1): the two registry rows at `ConnectionFieldDefaults.cs:31-34`, whose
getter and setter lambdas read and write the legacy ints; they are rewritten to read the list-valued default
(`ParentSourcesForTeams` / `ParentSourcesForPortfolios`, the way the Estimation row already reads a typed bundle).
`ConnectionDefaultSetting.ParentOverrideForTeams/ForPortfolios` (`SupportedConnectionDefaults.cs:6, 8`) is an enum member,
the setting's *name* that follower counts and `IWorkTrackingSystemConnectionUsage` key on, not a read of a column: it stays
and the file is allow-listed by name (the scan matches property reads, `.ParentOverrideForTeams` not preceded by
`ConnectionDefaultSetting`). Tests are outside the scan. Tests that arrange through the legacy members
(`PortfolioExtensionsTest.cs:27, 49`, `TeamExtensionsTest.cs:22-54`, the `WorkTrackingSystemConnection*ControllerTest`
files, `PortfoliosControllerTest.cs:423`, `EstimationFieldSyncTest.cs:58`, `ConnectionDefaultsDtoTest.cs:55`) are
migrated to the list in the slice that changes the code they exercise; those that assert the write-through value stay,
as they are the golden projection.

### Decisions, each with options and a recommendation

#### DD-1 Storage of the ordered list

| Option | For | Against |
|---|---|---|
| **A. One JSON text column `ParentSources` on each owner table, and one per grain on the connection (recommended)** | Established precedent in this context (`StateMappings`, `CycleTimeDefinitions`, `RefinementSettings`: `HasConversion` + `ValueComparer`); one nullable column added, expand-only trivially; the list is read and written whole, never queried by element; Epic 6474 copies one column per table into the Unit table, with no discriminator and no join; no `Include` to forget (ADR-241 rejected child tables for that reason) | Not queryable per element in SQL: the "field in use" guard filters in memory over the owners of one connection (bound below). Data step must build JSON text in provider-neutral SQL |
| B. Child table `OwnerParentSource(OwnerType, OwnerId, Position, Kind, FieldId)` | Queryable, FK-able | Polymorphic owner key (two owner tables, ids overlap) needs a discriminator that 6474 must then re-key; an `Include` on every owner read (a forgotten one reads as "follows nothing", ADR-241's own warning); position maintenance; two more tables for the connection defaults. Heavy for at most five rows |
| C. Five columns (`ParentSource1..5`) | Flat, ADR-241 style | The cap becomes schema; five nullable pairs (kind + id) per table, ten on the connection; reorder becomes five writes; raising the cap is a migration |

**Chosen: A.** NULL means "no own value" (a follower, or an owner never saved with a list); the effective list is the
owner's own list while `Own`, else the connection default's, else the single entry `[TrackerParent]` (exactly today's
behaviour with no override).

**Stored form, pinned (one precedent, the list-valued settings `StateMappings` and `CycleTimeDefinitions`,
`LighthouseAppContext.cs:628-674`).**

| Pin | Value |
|---|---|
| Serialiser | `JsonSerializer.Serialize(value, (JsonSerializerOptions?)null)`, i.e. default options, exactly as those two converters call it: PascalCase member names in declaration order, no indentation, `null` written, **enums as integers** |
| The other precedent | `RefinementSettings` uses `JsonSerializerDefaults.General` (camelCase). Not chosen: the data step would have to hand-write camelCase, and the two list-valued settings that move with the owner (and that Epic 6474 copies) use null options |
| Column type | text, **not `jsonb`**: SQLite `TEXT`, PostgreSQL `text`. `jsonb` would normalise key order and spacing and break byte equality with the serialiser |
| Element | `ParentSourceEntry`: `Kind` (enum `ParentSourceKind`: `TrackerParent` = 0, `AdditionalField` = 1, append-only like `SettingSource`) then `AdditionalFieldDefinitionId` (int, null for `TrackerParent`) |
| Examples | `[{"Kind":1,"AdditionalFieldDefinitionId":12},{"Kind":0,"AdditionalFieldDefinitionId":null}]` and `[{"Kind":0,"AdditionalFieldDefinitionId":null}]` |
| Converter (M-1) | A `ValueConverter<ParentSources?, string?>` on a **nullable** property. **NULL stays NULL**: it means "no own value" (a follower) and is never turned into an empty list, unlike the two precedents, whose converters map a blank value to `new List<...>()` (`LighthouseAppContext.cs:630-633`, `:654-657`) because for them "empty" is a legal value. Here an empty list is *not* a legal value (1..5 entries) and an empty list read for a follower would silently turn "follow the default" into "own, nothing". Write: `value.Entries` serialised with null options. Read: `null` stays `null`; blank text is read as `null`; any other text deserialises to `List<ParentSourceEntry>` and goes through the validating factory (`ParentSources.Of`), and a text the factory refuses **throws rather than being repaired**, because a silently repaired list changes where parents come from. Writes of such text are only possible through the migration's data step (proved by the round trip) or a hand edit. A test reads a row with NULL and asserts `null`, and a row with a bad text and asserts the loud failure. **Blast radius and recovery of such a row:** the throw happens when the owner is loaded, so every read that loads that one owner fails (its settings page, its refresh, a connection save that scans it) until the row is corrected; other owners are unaffected. The recovery is to fix or null the `ParentSources` text of that row in the database (NULL makes it follow its default again), which needs database access, as does the hand edit that could have caused it |
| Comparer (M-1) | `ValueComparer<ParentSources?>`: both null are equal, null versus a value is unequal, otherwise equal when the serialised texts are equal (as the precedents compare). Hash: `0` for null, else the text's hash. Snapshot: the value itself, because `ParentSources` is immutable (the precedents deserialise a copy only because their lists are mutable) |
| Construction by the serialiser (M-1) | `ParentSourceEntry` has public **get-only** properties (`Kind`, `AdditionalFieldDefinitionId`) and a **private `[JsonConstructor]`** whose parameter names match them. That constructor **throws on invalid input** (TrackerParent with an id, AdditionalField without one, an unknown kind), the same check the per-kind factories use, so the read path cannot build an invalid entry. `ParentSources` itself is never deserialised by the serialiser: the converter reads the array of entries and calls `ParentSources.Of`, and **it does not catch** the entry constructor's exception or `Of`'s refusal, so a bad text fails loudly. Nothing is public-settable. DELIVER proves by a **round-trip test** that `System.Text.Json` with **null options** (the converter's call) really builds the entry through the private constructor, and a test feeds the serialiser each invalid shape and asserts the throw reaches the caller |

**Source column: reused.** The stored `ParentOverrideSource` column (0 = `ConnectionDefault`, 1 = `Own`,
`SettingSource.cs`) keeps its meaning (follow the default, or own), so no second source column is added. Only the value
changes shape.

**Migration (expand-only; generate with `Lighthouse.Backend/Create-Migration.ps1`, never `dotnet ef migrations add`).**
Additive (table names confirmed against `20261018092146_AddPortfoliosParentOverrideDefault` in `Lighthouse.Migrations.Sqlite` and `20261018092156_…` in Postgres, which use `"Portfolios"`, and against `20261017150710_AddTeamsParentOverrideDefault`, which uses `"Teams"`): `ParentSources` on `Teams` and `Portfolios`, `ParentSourcesForTeams` and `ParentSourcesForPortfolios` on
`WorkTrackingSystemConnections`, nullable text. The data steps are hand-added to the generated migrations in both
provider projects, the same SQL text in both, the way the precedent migrations do it
(`20261017150710_AddTeamsParentOverrideDefault` in `Lighthouse.Migrations.Sqlite`, `20261017150720_…` in
`Lighthouse.Migrations.Postgres`: `migrationBuilder.Sql` with a raw string literal, quoted identifiers). The integer is
cast explicitly, because `||` between text and an integer is not portable.

Data step, per owner table (`"Teams"` shown; `"Portfolios"` is the same with the table name changed):

```sql
UPDATE "Teams" SET "ParentSources" =
  '[{"Kind":1,"AdditionalFieldDefinitionId":' || CAST("ParentOverrideAdditionalFieldDefinitionId" AS TEXT) || '},{"Kind":0,"AdditionalFieldDefinitionId":null}]'
WHERE "ParentOverrideSource" = 1 AND "ParentOverrideAdditionalFieldDefinitionId" IS NOT NULL;

UPDATE "Teams" SET "ParentSources" = '[{"Kind":0,"AdditionalFieldDefinitionId":null}]'
WHERE "ParentOverrideSource" = 1 AND "ParentOverrideAdditionalFieldDefinitionId" IS NULL;
```

Connections (NULL stays NULL, effective `[TrackerParent]`):

```sql
UPDATE "WorkTrackingSystemConnections" SET "ParentSourcesForTeams" =
  '[{"Kind":1,"AdditionalFieldDefinitionId":' || CAST("ParentOverrideForTeams" AS TEXT) || '},{"Kind":0,"AdditionalFieldDefinitionId":null}]'
WHERE "ParentOverrideForTeams" IS NOT NULL;
```

and the same for `ParentSourcesForPortfolios` from `ParentOverrideForPortfolios`. Followers get NULL. `CAST(x
AS TEXT)` renders the integer with digits only on both providers, and the serialiser writes an int the same way; the
round-trip test (DD-9) proves it on both providers rather than relying on that sentence.

**The legacy integer columns are kept in sync for one release (write-through), not frozen.** The owner's mutator (and
the connection's) writes the legacy column whenever it sets a list, through the one projection function that the
deprecated wire members also use (DD-6): `[X, TrackerParent]` writes X; `[TrackerParent]` writes NULL; any other shape
writes its first `AdditionalField` id, or NULL. A follower leaves its own legacy column untouched; the connection's
two columns follow the default list the same way. Nothing in the new build **reads** the legacy columns (architecture
rule 5), so they cannot become a second source of truth; they exist for the build one release back.

**Rollback limit (stated, tested, and the reason this is not called safe).** A build one release back reads only the
legacy column and then falls back to the tracker's own parent. For a list of the two legacy shapes it behaves like the
list on the data it could read; it cannot read the Data Center parent field after an empty `ParentKey`, nor the native
parent of an Azure DevOps override owner (its relations are skipped), and both are rollback limits. For every other shape it resolves as the projection: `[A, B, TrackerParent]` reads A, then the own
parent (B is lost); `[TrackerParent, A]` reads A first (the order is lost); `[A, B]` reads A, then the own parent (the
decision not to use the own parent is lost). Edits made on the older build are not carried forward: rolling forward
again restores each list as it was when the rollback happened. DD-9 pins what the list and the old column read for each shape as literal expectations; no test runs against the previous build.

**Down step.** `Down` drops the new columns (four) and nothing else; because the legacy columns were written through,
they already hold what the older schema needs, so no data step is run on the way down. The migration test runs `Down`
and asserts the legacy values still equal the projection. A later release drops the legacy columns (contract), not this
one.

Interaction with slice 03: a follower holds no list; the connection default is a list, with the same two data shapes
above, so "an old single default `Caused by` reads `[Is caused by, Parent (Jira's own)]`" falls out of the connection
statement with no special code.

#### DD-2 How the own Parent is a list entry

| Option | For | Against |
|---|---|---|
| **A. Typed entry kind (recommended): `ParentSourceEntry { Kind, AdditionalFieldDefinitionId? }`, kinds `TrackerParent` and `AdditionalField`** | The invalid state "TrackerParent with a field id" and "AdditionalField without one" is unrepresentable (a factory per kind); no filtering of a magic id out of the "fields in use", "fields to request", findings and `DisplayNameOfField`; duplicate rule is value equality; JSON reads as what it is | One more type than a bare `int?` list |
| B. Sentinel field id (0 / -1 = own Parent) | Reuses `List<int>` | A magic number leaks into every consumer that walks field ids (ADO field request list, save guard, DTO, TS mirror); `HoldsStoredField(0)` is already "unsaved field" in this model (`id > 0`); one forgotten filter turns into a lookup of field 0. Rejected |
| C. A third kind `LinkType` | Names links explicitly | ADR-193 §5: nothing is persisted about what a reference turned out to be, so the kind would lie when a Jira admin renames a link type. A2 holds: links stay `AdditionalField`, resolved at read time. Rejected |

#### DD-3 Where "the first source that yields a parent wins" lives

Two phases, one rule. The rule lives in **`ParentSourceSelector`** (a pure, tracker-neutral static: ADR-193 §2's
existing home; architecture rule 1 in "Quality attributes and enforcement" makes "imports no connector" a real, new
check). Connectors keep only *reading*.

| Option | For | Against |
|---|---|---|
| **A. Adapters produce `ParentReadings` (one reading per list position); the selector alone orders and decides (recommended)** | Order, own-Parent position, fall-through and what "yields" means exist once and are tested by a table (readings x stored Features x narrowing); Jira and ADO cannot drift (the `DependencySourceSelector` lesson in ADR-193); connector-specific work (field vs link phrase, ADO relation) stays in the adapter | A new in-memory shape on `WorkItemBase` replacing `ParentCandidatesFromLinks` |
| B. Each connector walks the list itself | No new shape | The rule is written twice and a third connector copies it; fall-through needs stored Features, which a connector (a leaf adapter) must not reach. Rejected |
| C. Strategy interface per entry kind implemented by each connector | Textbook strategy | The data a reading needs is already at hand in the static parse loop; an interface call per record per entry adds indirection without a second implementation to vary. Rejected (YAGNI) |

**Reading, per entry, produced by the adapter** (closed set): `NoValue`; `FieldValue(key)`; `LinkCandidates(ParentResolution)`
(Jira, for an entry that resolved to a link type); `TrackersOwn(key)`; `CannotBeRead` (ADO relation type, reported as a
finding). `ParentReadings` is positional with the list it was read from.

**The `TrackerParent` reading (H-5, AFK-8).** On **Jira Cloud** it is `issue.ParentKey` (empty gives `NoValue`). On **Jira
Data Center** it is exactly today's own-parent reading: `issue.ParentKey` first, and when that is empty the value of the
field the instance hangs parents on (Epic Link for a Team, Parent Link for a Portfolio), looked up by name against the
ordinary field list, so "which deployment this is" still never enters into it. The record factory builds this reading
inside `CreateWorkItemFromJiraIssue`, taking the instance's parent-field name as one more argument (resolved once per
fetch, where `TheFieldTheInstanceHangsParentsOn` is called now, `:327` and `:1339`). What goes:

- the unconditional `ParentReferenceId = issue.ParentKey` at `:1879`: the factory starts the record with an empty
  `ParentReferenceId`, and the only thing that ever sets it is `HangUnder` applying a choice;
- `TrySetParentFromTheFieldTheInstanceHangsItOn` (`:1981-1992`, called at `:335` and `:1350`) as a *writer*. Its
  `IsNullOrEmpty(ParentReferenceId)` guard at `:1983` is exactly the "key empty, then the field" order, and moves into the
  reading (`ParentKey` empty, then the field value), no longer reading the record's parent, which is no longer pre-set;
- the gate in `TheFieldTheInstanceHangsParentsOn` (`:1964-1967`), "empty while an override is named", becomes "empty when
  the list holds no `TrackerParent` entry" (`ReadsTheTrackersOwnParent`, below). With no `TrackerParent` entry the
  reading is `NoValue` and the field is not even looked up.

`ReadsTheTrackersOwnParent` (`ParentSourceSelector.cs:33`) is now list level: true when the owner's **effective** list
contains a `TrackerParent` entry (own list while `Own`, else the connection default, else `[TrackerParent]`). It stays the
one question every consumer asks (what to fetch, which field name to look up, whether relations are read on ADO).

**Consequence for Data Center (UPGRADE-1 (A), DD-4).** Today an owner that names an override X has the Data Center parent
field *not* read (`:1964-1967` returns empty), so only `ParentKey` stands behind X. The migrated `[X, TrackerParent]` reads
the field after `ParentKey`, as AFK-8 defines it, so an item with an empty X, an empty `ParentKey` and a set Epic Link /
Parent Link gains a parent after the upgrade. The maintainer accepted that (UPGRADE-1 (A)).

**What the record keeps after the connector pass (the fall-through needs it).** Today the Jira connector writes the link
key straight into `ParentReferenceId` (`JiraWorkTrackingConnector.cs:1879`, `:1928`, `:1932`, `HangUnder`), and the record
holds one `ParentReferenceId` and one `ParentCandidatesFromLinks` (`WorkItemBase.cs:24`, `:79`); the refresh re-places
only records whose links name several issues (`WorkItemService.cs:368-369`, `:213`). That cannot fall through: after
the connector pass the readings behind the winner are gone. So:

- `WorkItemBase` gains **`ParentReadings`**, the whole ordered set of readings the connector took, one per list
  position, **replacing `ParentCandidatesFromLinks`** (the link candidates now live inside the `LinkCandidates` reading,
  so nothing is lost). It is `[NotMapped]` and **transient within one refresh**: set once per record by the connector,
  read by the second pass in the same refresh, then dropped with the record. Defaults to empty, which never needs
  stored Features.
- **The record is wrapped between the connector pass and the second pass, so the wrap must carry the readings (H-3).**
  The connector builds a plain `WorkItemBase` and then wraps it: `new WorkItem(workItem, team)` (`Jira…cs:337`,
  `WorkItem.cs:10`) for a Team, `new Feature(workItem, …)` (`Jira…cs:1355`, `Feature.cs:35`) for a Portfolio. Both go
  through the protected `WorkItemBase` copy constructor (`WorkItemBase.cs:11-18`), which today copies
  `ParentCandidatesFromLinks` at :16 and will copy `ParentReadings` there. The second pass runs **after** the wrap, on the
  wrapped records the service holds (`fetch.WorkItems` at `WorkItemService.cs:184`, `fetch.Features` at :983, the parent
  Features at :1356), so a copy constructor that forgets the readings makes every record look like "no readings": no
  record needs stored Features, no retraction happens, and the result is silently the provisional one. A test per wrapper
  (`WorkItem`, `Feature`) asserts the readings survive the wrap; D6 below covers the failure. The first pass
  (`HangUnder` applying `ChooseProvisionally`) runs inside the record factory, **before** the wrap, on the unwrapped record,
  and the copy constructor then carries `ParentReferenceId` and the shown field values through `Update(...)` as it does
  today; the second pass (`HangUnder` applying `Choose`) writes the wrapped record, which is the one that is saved.
- **Not persisted**, deliberately. The second pass runs on the very records just downloaded, before they are saved
  (`WorkItemService.cs:184`, `:983`, `:1356`); a change of the list moves the fingerprint, which re-downloads every
  record; a record whose tracker stamp did not move keeps its stored `ParentReferenceId`, which is exactly how an
  ambiguous link behaves today (a Feature stored later does not re-place a record until it moves; unchanged, now
  stated). Persisting the readings would need a column on the work item table, a migration and staleness rules for no
  gain. Rejected alternatives: re-deriving from the Additional Field values (a link entry's field shows only the key
  that won, so the candidates are gone); keeping only the readings after the first link entry (ties the record to the
  winner position and saves nothing).

**The two calls, one rule, one contradiction removed.** The earlier text said both "a single candidate is judged by the
Feature test" and "a single candidate is taken at face value". Both are true, of different calls, and the rule is now
stated once:

- **Full call** `Choose(readings, storedFeatures, narrowing)`, used by the second pass: walks positions in order and
  stops at the first that *yields*. `FieldValue` yields when non-empty, even if it names a non-Feature (today's rule).
  `TrackersOwn` yields when the tracker reported a parent. `LinkCandidates` yields when `ParentResolution.Place`
  places a candidate. For a Team **every** candidate must be a stored Feature, the oldest link wins (Story 6301; with two
  or more Feature candidates the winner is the oldest link, then the earliest-created Feature, then the lowest key, as
  `ParentResolution.cs:57-74` ranks them). **`Place` judges a single candidate like several, for every list
  (UPGRADE-1 (A), DD-4).** A link entry with no stored Feature behind it yields nothing and
  the walk continues. `CannotBeRead`
  and `NoValue` yield nothing. For a Portfolio the narrowing is `DoNotNarrow` (its parents are stored only once read),
  so any candidate yields and fall-through on links is a Team matter. DISTILL pins both.
- **Provisional call** `ChooseProvisionally(readings, narrowing)`, used by the connector (which has no Feature
  knowledge): the same walk, but it **stops at the first link reading that needs a Feature test** (Team: one or more
  candidates; Portfolio: two or more) and returns
  `NeedsStoredFeatures`. Its `Key` is only so that a caller with no second pass still gets a value: the single
  candidate's key (face value) or empty when several. The connector **returns this choice to the one helper, `HangUnder`,
  and assigns nothing itself** (H-4, below).
- **The placement filter is widened** from "links name several issues" to "`ChooseProvisionally(record.ParentReadings,
  narrowing).NeedsStoredFeatures`", which is true for any record with a link reading ahead of its winner or as its
  winner. `WorkItemService.PlaceWhatTheLinksLeftOpen` runs the full call on exactly those records, **overwrites
  `ParentReferenceId` (it may clear it again: a face-value key that failed the Feature test is retracted)** and the
  link entries' displayed field values, reads the stored Features once per fetch, and not at all when no record needs
  them. For an install with a link entry the stored Features are now read whenever a record has a link candidate, not
  only an ambiguous one: one database read per fetch, no tracker request.
- The unconditional `ParentReferenceId = issue.ParentKey` (Jira line 1879) goes: the tracker's own parent is read only as
  the `TrackersOwn` reading of a list that holds it (the reading above, with the Data Center field).

**Change to `ParentResolution.Place` for a single candidate (H-1).** Today `Place` returns `NotAsked` for anything that
is not ambiguous (`ParentResolution.cs:52-54`, `if (!IsAmbiguous)`), its doc says "One link or none is not a question: the
caller stores it as it is" (`:43-48`), `ParentResolutionPlacementTest.cs:52-62` pins that ("A lone link is stored as it is,
Feature or not"), and the service filter `LinksNameMoreThanOneParent` (`WorkItemService.cs:368-369`) selects only
`IsAmbiguous` records, so a single candidate never reaches `Place`. Fall-through for a single non-Feature candidate
needs all four to change:

- `Place`: the early return becomes `candidates.Count == 0` (still `NotAsked`: no link names nothing to choose from). A single
  candidate is narrowed like any other: a Team's lone candidate that is a stored Feature is `Placed` (rule
  `OnlyFeatureAmongLinks`, which already exists), one that is not is `NotPlaced(NoneIsAStoredFeature)`, and a Portfolio's
  lone candidate (`DoNotNarrow`) is `Placed` under a new rule `OnlyCandidate` (appended to `ParentPlacementRule`), never `OnlyFeatureAmongLinks`: that name says "the only Feature among the links", is the Team rule, and is logged only when narrowing ran (M-3). The class summary ("Several is not guessed at here") and the `Place` doc
  are rewritten to say that any candidate is judged by the narrowing and only an empty set is not asked.
- `ParentResolutionPlacementTest.One_candidate_is_not_a_question_for_the_pick` is split: the `none` half stays
  (`NotAsked`, no rule, empty key); the `lone` half becomes the lone-candidate rows of the placement table (Team + stored
  Feature, Team + non-Feature, Portfolio), with the old message about "stored as it is" removed.
- The filter becomes the provisional call's `NeedsStoredFeatures` (above), no longer `IsAmbiguous`: placement runs over
  those records. **The counts and warnings are computed only over the `IsAmbiguous` subset of them (H-2):**
  `RecordsPlacedFromLinksNamingMoreThanOneParent` / `RecordsWhoseLinksNamedMoreThanOneParent` (`WorkItemService.cs:184-195`,
  `:220-229`) and the per-record warning lines (`:222-225`) describe only ambiguous links. A single-candidate placement or
  fall-through adds no count and no warning line; it appears in the per-fetch debug line (DEVOPS section 4, "fell
  through"). `HangUnder` also runs on `NotPlaced` records in this pass (today's call at `:243-246` is `Placed`-only),
  with whatever the full call chose, so a provisional key that failed the Feature test is retracted. A test: a download
  of single links gives zero counts and no warning lines. `IsAmbiguous` and `IsResolved` stay on `ParentResolution` for
  this report.

**Retraction: how the full call overwrites and clears (H-2).** Today `HangUnder` writes `ParentReferenceId` only when the key
is non-empty (`ParentSourceSelector.cs:70-78`) and only the one override field's shown value, so a face-value key that fails
the Feature test in the second pass could never be taken back. `HangUnder` is modified (component table above) to apply a
whole `ParentChoice` to the record: `ParentReferenceId` is **set to the choice's key even when it is empty**, which clears a
provisional key; the winning link entry's Additional Field shows the key; every other link entry's Additional Field is set
to empty (their readings are cleared, which is also what removes a stale shown key when the winner moves from one entry to
another). Which positions are link entries is read from the record's `ParentReadings`, so the helper takes no connector
knowledge. The full call depends only on the readings and the stored Features, never on what the record held, so it is
idempotent and re-running it after the provisional pass is safe.

**One decider, one writer (H-4).** `ParentSourceSelector` **returns** a `ParentChoice` and writes nothing; the record's
`ParentReferenceId` and shown field values are assigned only from that result, in one helper, `HangUnder`. The connector
(provisional choice) and `WorkItemService` (full choice) both call it; neither assigns `ParentReferenceId` from anything
else. So "the connector writes that key" in the earlier text means "the connector hands its provisional choice to
`HangUnder`". Rule 3 below is worded to match: it limits who may *assign*, and `HangUnder` is the only place that does.

**ADO.** Readings are `FieldValue` (a classified field, and an entry that could not be classified, read as a field as
today: `CouldNotTellWhatItNames`, `AzureDevOpsWorkTrackingConnector.cs:1399-1401`), `TrackersOwn` (the native Hierarchy-Reverse
parent, read from the relations only when the list holds `TrackerParent`) and `CannotBeRead` (a relation-type entry, plus
the finding) only, so an ADO record never needs stored Features and the second pass never runs for it. Relations-skip
rule and the migration (UPGRADE-1 (A)): see the reader table.

#### DD-4 Refetch trigger, and the upgrade (UPGRADE-1)

`FetchFingerprint` renders the **effective ordered list** (order-sensitive, unlike its `Unordered` helpers): the existing
registered property is replaced by `ParentSources` in `RegisteredProperties` and the guard test records why. Rendering
is canonical per entry (`F:<id>` / `P`), joined in order by `,`, so every entry and its position move the fingerprint
(ADR-242's connection save then queues exactly the followers whose fingerprint moved, with no new code). A new-form
string always starts with `F` or `P`, so it can never equal a legacy digit string or `-` (property-tested, DD-9).

**Legacy shapes.** `[TrackerParent]` renders as `-` (no override, so no link entry is possible and its meaning cannot have
moved), named in the code by that reason. Both render the **effective** list, so a follower of a default renders what the
default renders. Every other shape, `[X, TrackerParent]` included, renders in the new form (`F:<X>,P`), which is what
queues the one-time re-read of every override owner, Jira and ADO alike (golden test, DD-9).

#### UPGRADE-1: what the upgrade does to parents that are read today (DECIDED: (A), 2026-11-02)

No static migration reproduces today's parent results exactly, because whether the tracker's own parent is today's
fallback is decided at read time: on Azure DevOps it depends on the grain and on the dependency setup
(`AzureDevOpsWorkTrackingConnector.cs:71-83`, `:139`, `:1398-1403`; `DependencySourceSelector.cs:32-33`), and on Jira Data
Center an override owner does not read the instance's parent field today while a `TrackerParent` entry does. Writing `[X]`
instead of `[X, TrackerParent]` does not preserve them either: it drops the `ParentKey` fallback on Jira (Cloud and Data
Center), and it renders a new fingerprint (a re-read). The maintainer decided **(A) Accept the change**; it closes AFK-1,
HELD-A and HELD-B.

- **(A) Accept the change (decided).** The data step migrates every override owner to `[X, TrackerParent]` and every other
  owner to `[TrackerParent]`, and the new fall-through rule applies everywhere, with a one-time re-read (Jira-wide through
  `JiraReadingVersion`, `FetchFingerprint.ForConnection`, `FetchFingerprint.cs:113-116`, `:129`, because whether `X` is a
  link type is known only when reading; ADO override owners through the new fingerprint form above). Golden constants at
  `JiraReadingVersionFingerprintTest.cs:34-36` are regenerated.
  - Items that move, once, on the re-read: (1) ~~an item whose override field is empty and that has Jira's own Parent
    now hangs under it~~ (struck, see the correction after this decision: Jira already fell back to its own Parent for an
    empty override, so those items do not move); (2) on Data Center, an item with an empty `ParentKey` now reads the
    instance's parent field, which an override owner did not read before; (3) on Azure DevOps, an item of an override owner with no
    override value now hangs under its native Hierarchy-Reverse parent; (4) an item whose only link candidate is no stored
    Feature, held today, falls through to the next entry. They may start counting towards a Feature, so Feature sizes and
    forecasts move once. Items whose override yields a parent, and every item of an owner with no override, keep their
    parent. A **release note is required** (finalize checklist).
  - Refetch cost: one re-read of every Jira owner, plus every ADO override owner, on its next refresh (watch for 429
    back-off, DEVOPS section 3); ADO owners with an override also read relations on every refresh from then on (more
    requests) because `TrackerParent` is listed.
  - Design complexity: none added. No legacy-mode flag and no per-option branch in the selector, either connector, the
    fingerprint or the tests; one rule for every list.
  - Slice 01 done-when: parents identical for owners whose override yields a parent and for owners with no override; the
    items named above re-read once and move; the DISCUSS identical-results criterion (AC-1.6) and "the migration queues no
    refetch" are amended to this.
- **(B) Preserve until edited: rejected**, because a hidden per-owner legacy flag would have to match a conditional Azure
  DevOps read path exactly, adding a stored flag, clear-on-save rules, a selector branch and a cleanup migration to spare
  a one-time move of items whose configured sources already named nothing usable.

Tests: golden fingerprints (new form, `-`, regenerated Jira constants) and the re-read case.


#### DD-5 Validation: domain first, UI mirrors

`ParentSources` is an immutable value object with a validating factory returning the list or the violations:
at least one and at most five entries (MD-4), no duplicate (`TrackerParent` once; each field once), every
`AdditionalField` entry has an id. Violations are values (an enum), not exceptions, so the API maps them to the plain
messages in the UAT ("A list holds at most 5 sources", "{name} is already in the list") through the existing
`OwnerSettingRefusals` path (400, nothing saved). "A field that is not on the owner's connection" generalises the
existing `NamesAFieldItsConnectionLacks` to every entry. "A field in use cannot be removed" stays in
`ConnectionSaveService.FieldsStillInUseAmong`, but its two EF predicates (`ConnectionSaveService.cs:91-124`:
`TeamsOwningAnyOf` at 91-98 and `PortfoliosOwningAnyOf` at 100-124, which filter on `OwnParentOverrideAdditionalFieldDefinitionId`) cannot
stay: a JSON text column is not filterable by element, and the write-through legacy column holds only the first field of
a list, so using it as a filter would let a second field (`[A, B]`, B removed) go unnoticed. **Approach that works on
SQLite and Postgres:** the predicate keeps only `WorkTrackingSystemConnectionId == connectionId` (the owners of one
connection; a plain column both providers translate), the query projects the owner's name, its own list and
its other own field ids, and the intersection with the removed ids is made in memory after `AsEnumerable()`, which is
where the other field members are already compared. The connection defaults' `FieldsOn` replaces `FieldOn` in the
registry (`ConnectionFieldDefaults.cs:31-34`) so a default naming several fields is checked too. The UI mirrors but is not
the authority: the add control is disabled at five with the text, the picker offers only fields not yet listed, so a
duplicate is unreachable by clicking; the server message exists for scripts and stale tabs.

**Bound on the "owners of one connection" load (m-B).** The guard runs only on a connection save that removes at least one
Additional Field, and loads one connection's Teams and Portfolios with three columns each (name, list text, a few ids), no
work items. The bound is a **measured DELIVER check, not a claim**: slice 01's guard step counts the owners of the largest
connection in the demo data and in the Epic 6400 many-Features fixture, and records the number and the guard's time in the
step's log; if one connection holds more than a few hundred owners, the guard moves the filter into SQL with a `LIKE` on
the list text as a pre-filter (a superset, then the exact in-memory check). Until measured, the Teams-per-connection
figure is unknown to this design.

#### DD-6 API and DTO (additive; public v1 API)

Every route is under `api/v1` (`api/latest` aliases it), so the Team/Portfolio settings DTO and `ConnectionDefaultsDto`
**are** the public API. Usages listed before editing: backend `SettingsOwnerDtoBase` (ctor + members, lines 27-29, 78-83),
`TeamSettingDto`, `PortfolioSettingDto`, `PortfolioExtensions.SyncWithPortfolioSettings`,
`WorkTrackingSystemOptionsOwnerExtensions.TakeFollowableSettingsAsSent`, `OwnerSettingRefusals`, `ConnectionDefaultsDto`,
`PortfolioController`/`PortfoliosController` (refusal seam), the approved snapshot files under
`Lighthouse.Backend.Tests/API/Integration/FastWithManyFeatures/Approved/` and `EstimationFieldSyncTest`,
`ConnectionDefaultsDtoTest`; frontend `models/Common/BaseSettings.ts`, `useModifySettings.ts` (`NULLABLE_FIELDS`),
`AdvancedInputs.tsx`, `CreateTeamWizard.tsx`, `ModifyProjectSettings`/`ModifyTeamSettings` and their tests,
`fieldDirectionTips.ts`, `connectionDefaultEntry`, the E2E `BaseEditPage`. **Lighthouse-Clients: no use**
(grep of `/storage/repos/lighthouse-clients`, 0 hits for `parentOverride` and the settings types).

- Team/Portfolio settings GET and PUT gain `parentSources: [{ kind: "TrackerParent" | "AdditionalField", additionalFieldDefinitionId?: number }]`
  (kind as a string enum, the project's out-as-string convention) and `parentSourcesDefault` (the followed default's
  entries, so a follower's page shows "Connection default: Is caused by, then Parent" without a connection read; the
  existing `parentOverrideDefaultName` precedent).
- `parentOverrideSource` keeps its name and meaning. `parentOverrideAdditionalFieldDefinitionId` **stays on the wire
  for one release, deprecated**: GET returns the list's legacy projection (the one function that also feeds the
  write-through column, DD-1: the first `AdditionalField` entry's id, or null); PUT accepts it only
  when `parentSources` is absent, and then applies ADR-241's rule (act only if it differs from the projection of the
  effective list before the request; the new list is `[X, TrackerParent]` or `[TrackerParent]`, the migration mapping). An
  old client echoing a GET therefore cannot overwrite a richer list.
- Connection: `ConnectionDefaultsDto` gains `parentSourcesForTeams` and `parentSourcesForPortfolios`, held the way
  Estimation already is (a typed value beside the `sent` map, `Sends(...)`, absent = unchanged, explicit null = cleared).
  The old `parentOverrideForTeams/Portfolios` ints stay as deprecated projections of the list, same one-release rule,
  and are written through to the connection's legacy columns on every save of a list.
- `RefreshFindingSetting.ParentOverrideField` keeps its wire name; findings become one per failing entry.

#### DD-7 Frontend control: extend `ItemListManager` or compose

`ItemListManager` is a chip list for free-text strings: identity is the string (`key={item}`), the only input is a
text/Autocomplete field, there is no per-row select. The sketch needs rows with a position, a picker, up/down buttons
and a remove, over typed entries. **Recommendation: compose, do not extend.** Add `ParentSourcesList`
(in `components/Common/ConnectionDefault/`, beside `ConnectionDefaultFieldSelect`) built from MUI list rows. Reuse:
`ConnectionDefaultFieldSelect`'s follow/own pattern for the radio and the "Connection default (...)" line (its
`followOrOwn` handler shape), `FormSelectField` for the add picker, and the **drag-reorder behaviour of
`ItemListManager`**: extract its drag handlers into a small `useDragReorder(items, onReorder)` hook used by both, only if
that stays a pure move; otherwise duplicate the ~15 lines rather than touch a shared component for an unrelated need.
Up/down buttons are the primary, keyboard-accessible control and drag is the enhancement, so the control works without a
pointer. The list is a controlled value `ParentSourceEntry[]` in settings, edited through one `onChange(list)`; a
frontend `reduceParentSources(list, action)` pure function (add, remove, move) is the mutation target and mirrors the
domain rules. Generic `onSettingsChange(key, string|number|null)` is widened for an array value.

**Loading and failure looks (slow connection).** The control adds no request: it renders from the settings and the
connection's Additional Fields that `useModifySettings` already loads. Loaded: as the DISCUSS sketch. Loading: the
existing page-level `LoadingAnimation isLoading={loading}` covers the whole form, so the list is never shown half-filled
or from the previous owner; nothing is shown as "empty list". Failure: the existing `refreshFailed`/`retry` path of
`useModifySettings` and the page error, unchanged; if the saved list names a field the loaded definitions do not hold,
the row reads "Unknown field (id 12)" in warning colour rather than blank, and the add picker still works. Saving:
the page's existing autosave state indicator; each edit is a whole-list value, so a late response cannot interleave
entries. Connection page (slice 03) loads with the connection through the same path. Reuses `WidgetShell`: N/A (not a
dashboard widget); to verify by hand with the browser throttled.

```text
LOADING (page)             LOADED                                 COULD NOT LOAD
+---------------+   +--------------------------------------+   +------------------------------+
|   (spinner)   |   | Parent sources                       |   | Could not load the settings. |
| form dimmed,  |   | (o) Set here  ( ) Connection default |   | [ Retry ]                    |
| no list shown |   |  1 :: Is caused by    [up][dn][x]    |   | (existing page error; the    |
+---------------+   |  2 :: Program Ref     [up][dn][x]    |   |  list is not shown at all)   |
                    |  3 :: Unknown field (id 12) !  [x]   |   +------------------------------+
                    | [ + Add a source v ]  Not in list: ... |
                    +--------------------------------------+
```

#### DD-8 Pattern ledger (chosen and rejected)

| Pattern | Verdict | One line |
|---|---|---|
| Value object (`ParentSources`, `ParentSourceEntry`) | Chosen | Immutable, validated at construction, equality gives "no duplicates" and fingerprint rendering for free |
| Parse, don't validate / invalid states unrepresentable (typed entry kind) | Chosen | A TrackerParent with a field id cannot exist |
| Functional core / imperative shell, plan-value (`ParentReadings` in, `ParentChoice` out) | Chosen | The ordering rule is pure; connectors and `WorkItemService` are the shell that gathers readings and writes `ParentReferenceId` |
| Capability injection | Chosen (narrow) | The selector receives only a "does Lighthouse hold this Feature" view (`StoredFeatures`, already used), never the repository, so it stays testable and leaf-adapter-safe |
| Single responsibility / open-closed | Chosen | Adapters read, the selector orders; a new tracker adds a reader, not a rule |
| JSON column | Chosen | Whole-list read/write, precedent, cheapest for Epic 6474 (DD-1) |
| Child table | Rejected | Polymorphic owner key and an `Include` to forget for five rows |
| N columns | Rejected | Cap in the schema |
| Sentinel id | Rejected | Magic number leaks to every field-id consumer |
| Strategy interface per kind | Rejected | One implementation point, no variation to abstract |
| Domain event (`ParentSourcesChanged`) | Rejected | The fingerprint already drives the refetch (ADR-242); an event would be a second path to the same refetch. Maintainer prefers events by default; this is the reaction the existing mechanism already performs, so none is added |
| Unit of work / outbox | Not needed | One owner row written by the existing save; no cross-aggregate write |
| Optimistic concurrency | Reused | The owner's `ConcurrencyToken` already guards the save |
| Specification / rule language ("Parent else link X") | Rejected | DISCUSS rejected more power than the problem needs |
| Transient readings on the record (two-pass choice) | Chosen | The second pass runs in the same refresh on the same records; nothing to store, nothing to go stale |
| Persisted readings on the work item | Rejected | A column, a migration and staleness rules for data only the same refresh uses |
| Write-through projection to the legacy columns | Chosen (one release) | Keeps the build one release back honest for legacy-shaped lists; frozen columns would silently resolve every edited owner differently on rollback |
| Legacy-mode flag per owner | Rejected (UPGRADE-1 (B), DD-4) | A stored flag, clear-on-save rules and read-time branches in the selector and both connectors, to spare a one-time move of items whose sources already named nothing usable |

#### DD-9 Test strategy (high level; DISTILL turns it into scenarios, untagged)

- **Domain, unit + property tests**: `ParentSources` (1..5, no duplicates, order preserved, JSON round trip);
  `ParentSourceSelector.Choose` as a table: readings x stored Features x narrowing (first wins; own Parent only when
  listed and at its position; fall-through on a link with no stored Feature; field value wins even when non-Feature;
  Portfolio does not narrow). Mutation targets: `ParentSources`, `Choose`, the frontend `reduceParentSources`.
- **Connectors, recorded payloads (Strategy A walking skeleton)**: Jira issue with native parent + Caused-by link + custom
  field; ADO item with two custom fields + native parent + a relation-type entry (finding named, rest still read);
  request-count pin: an install with one source makes no more requests than before (the ADO relations-skip restated for
  a list: relations are read when a `TrackerParent` is listed or dependencies are read; a relation-type entry does
  not force the read and raises one finding. Corrected in DISTILL, 2026-11-04).
- **Migration test on SQLite and Postgres** (as the Teams parent-override migration's): seed legacy rows, run the real
  migration, read through EF. Legacy X -> `[X, TrackerParent]`, own none -> `[TrackerParent]`, followers NULL,
  connection defaults. **Round trip:** for every migrated shape the stored text deserialises and serialises back to the
  identical string (`Serialize(Deserialize(text)) == text`), so the SQL text and the serialiser cannot drift apart.
  The Down step is run too: the legacy columns still hold the projection, the new columns are gone.
- **Write-through and rollback**: a table of lists -> legacy value (the golden projection test: `[X, TrackerParent]` -> X;
  `[TrackerParent]` -> NULL; `[A, B, TrackerParent]` -> A; `[TrackerParent, A]` -> A; `[A, B]` -> A; a connection default
  likewise). The rollback results are stated as literal expectations per shape (what the list reads, and which
  old column value it writes); no test is run against the previous build or a stand-in for its rule.
- **Fingerprint**: order and entry sensitivity; golden values (a) `[X, TrackerParent]` renders in the new form (`F:<X>,P`, which queues the one-time re-read),
  (b) a follower of a default renders what the default renders (the effective list, not the owner's own NULL),
  (c) `[TrackerParent]` renders `-`, (d) a property test over every list of up to five entries from fields 1..3 plus
  the own Parent: no new-form string matches the legacy pattern (a digit string or `-`), because a new-form string always
  starts with `F` or `P`; and two different lists never render alike except the deliberate legacy collapse (`[TrackerParent]` as `-`).
  `FetchShapingPropertyGuardTest` updated with the reason.
- **Record readings and the second pass**: a download of mixed records (field winner before a link entry, link entry
  with a single non-Feature then a field, link entry with several candidates, no link candidates, Portfolio single
  candidate) -> `ParentReferenceId` per record, and records that need no stored Features are equal by value before and
  after the pass (among them a Linear-shaped record, whose `ParentReferenceId` is the project or initiative id the Linear
  connector built, and a CSV-shaped record, whose parent comes from the Parent Reference Id column); the stored Features are read once, and not at all when no record needs them. Added by the re-review:
  the `Place` lone-candidate rows (H-1); a retraction table for `HangUnder` (a provisional key cleared by an empty full
  choice; a winner moving between two link entries clears the first one's shown value; applying twice is a no-op) (H-2);
  readings surviving the `WorkItem` and `Feature` wrap (H-3); a Data Center record with an empty `ParentKey` and a set
  parent field reads the field, a Cloud record does not look for one, and a list without `TrackerParent` never looks
  (H-5); the NULL-stays-NULL, bad-text and invalid-entry serialiser tests (M-1); a single-link download gives zero counts and no warning lines, while an ambiguous one still counts and warns (H-2); a Portfolio lone candidate logs `OnlyCandidate`, never `OnlyFeatureAmongLinks` (M-3); the Jira clear runs after `PopulateAdditionalFieldValues` so a cleared link-entry value stays cleared; behaviour tests (not a source scan) for a link entry's shown value: it shows the winner's key, and is empty for a losing link entry and when no link entry wins (this covers the writes Jira `:2004` and `WriteBackService.cs:259` make legitimately); a `[JsonConstructor]` round trip with null options through the private entry constructor (M-1); upgrade tests for UPGRADE-1 (A) (DD-4): the named items move once, the others keep their parent.
- **Save and API**: `ConnectionSaveService` guard names the owner for any entry (including a second field of a list)
  and the default; refusals for 6 entries, duplicate, unknown field; echo of an old GET does not overwrite a richer
  list; approved snapshots regenerated.
- **Architecture**: the five rules in "Quality attributes and enforcement" (all new), each watched to fail once.
- **Frontend**: RTL for `ParentSourcesList` (reorder by button and drag, add disabled at five, picker excludes listed
  fields, consequence line when `TrackerParent` is absent, unknown-field row, follow/own switch); loading and failure
  states pinned through the host page. No scenario tags in code.
- **E2E**: none (maintainer, DISTILL 2026-11-04). The browser-to-API step is pinned by the same literal list text on
  both sides, and one skipped live case in `JiraWorkTrackingConnectorTest` reads an ordered list against real Jira.

### Component diagram

```text
  DRIVING ADAPTERS                    APPLICATION / DOMAIN                         DRIVEN ADAPTERS
  ----------------                    --------------------                         ---------------
  Team/Portfolio settings UI          Team/Portfolio controllers (modified)
   ParentSourcesList (NEW)  --PUT-->   OwnerSettingRefusals (modified)
  Connection page (slice 03)            -> ParentSources.Of(...) violations       EF Core, JSON text column
   same list (MODIFIED)     --PUT-->   ConnectionSaveService (modified)  ------>   ParentSources (NEW col)
                                         guard loads the connection's owners       ParentSourcesFor* (NEW)
                                         and scans every list in memory            legacy int cols: WRITTEN THROUGH
  Scheduled / manual refresh ------>   WorkItemService (modified)                  by the owner mutator, read by nobody
                                         PlaceWhatTheLinksLeftOpen (generalised):
                                         records whose provisional choice
                                         needs stored Features
                                         -> ParentSourceSelector.Choose (2nd call, with StoredFeatures)
                                                    ^
                           Owner (Models, MODIFIED)  |  ParentSources / ParentSourceEntry (NEW value objects)
                           effective list = own | default | [TrackerParent]
                                                    |
    WorkItemBase (MODIFIED): transient ParentReadings (NEW, NotMapped; replaces ParentCandidatesFromLinks)
                                                    ^ set once per record by the connector, read by the 2nd pass
    Jira connector (MODIFIED) --reads entries--> ParentReadings --> ParentSourceSelector.ChooseProvisionally (1st call, no Features)
                                                    --> HangUnder (MODIFIED, the one writer; also used by WorkItemService)
    ADO connector  (MODIFIED) --reads entries-->     (NEW)            FetchFingerprint (MODIFIED: ordered, legacy-stable)
    Linear / CSV / ServiceNow: UNCHANGED (ignore the setting, as documented)
```

### Quality attributes and enforcement

Maintainability: one ordering rule, one place. Reliability: for an untouched install the same `ParentReferenceId`, by
golden fingerprint and migration test, with the exceptions UPGRADE-1 names (DD-4). Testability: pure
selector. Operability: no new event, one finding per failing entry; an Azure DevOps owner that had an override reads relations on every refresh from the upgrade on, which is more requests than before; one extra database read per
fetch for an install with a link entry. Earned Trust: no new external dependency; the one
environmental assumption is that the data step writes the same bytes on SQLite and Postgres, probed by the migration
round-trip test on both (the only place a provider can "lie"). External integrations: Jira and Azure DevOps already read
these settings; contract tests recommended for both connectors' recorded payloads (consumer-driven, Pact-style), no new
service. Open source only, no new dependency.

**Enforcement: five rules to ADD. None exists today** (the existing `DependencySingleDecisionArchUnitTest` rules are
about `DependencySourceSelector`, not `ParentSourceSelector`; a statement that the selector "imports no connector" was
never a rule). **All five are new.** Rules 2, 3 and 5 are new **source-scan** rules modelled on helpers that exist in
that file: `TheBackendSourceOutsideTheSelector` / `LinesMatching` (`DependencySingleDecisionArchUnitTest.cs:357-371, 421, 689`,
"a reference built anywhere but the selector") and `EveryTrackerThatReadsDependencies_AsksTheSelectorWhereToReadThemFrom`
(`:384-388`, "every tracker names the selector"). Rules 1 and 4 are new **type-dependency** rules: that file has no
IL-dependency rule to copy for them. The nearest shape is `TheOneDecider_ReachesNothingThatLoadsOrStoresAnything`
(`:483-495`, `Types().That().Are(...).Should().NotDependOnAny(...).Check(Architecture)`), also used in
`AdditionalFieldInUseRulePurityArchUnitTest.cs:15-20`; for the Release-build async-edge gap the IL reader to model on is
`IlDependencies.ReadModule` as `DbContextComesFromAScopeGuardTest.cs:34` uses it. DELIVER decides which of the two a rule
needs and watches it fail once.

| # | New rule (name proposed) | Copies | Fails when |
|---|---|---|---|
| 1 | `TheParentSourceSelector_ImportsNoConnector` | new type-dependency rule (shape: `:483-495`) | `ParentSourceSelector`, `ParentReadings` or `ParentChoice` depend on a type in a connector namespace |
| 2 | `EveryTrackerThatReadsParents_AsksTheSelector` | new source-scan rule (shape: `:384-388`) | a connector that **reads the parent-source list** (it names the owner's `ParentSources`; today Jira and Azure DevOps only, M-2) does not also name `ParentSourceSelector`. Linear, CSV and ServiceNow do not read the list and are out of scope |
| 3 | `NoOneButTheOneHelperAssignsAParent` | new source-scan rule (shape: `:357-371`) | any production source file (all of `Lighthouse.Backend/Lighthouse.Backend`, tests excluded) outside the allow-list assigns `ParentReferenceId`. The scan matches `ParentReferenceId` assignments only. A link entry's shown field value is **not** source-scanned: it cannot be found that way (Jira `:2004` and `WriteBackService.cs:259` write `AdditionalFieldValues` legitimately), so behaviour tests cover it (DD-9). The selector decides by *returning* a `ParentChoice`; the helper is the single writer (H-4). **Allow-list, with the reason for each:** `ParentSourceSelector.HangUnder` (`ParentSourceSelector.cs:74`, the one writer); Linear `LinearWorkTrackingConnector.cs:191`, `:361`, `:438` (Linear ignores the setting; the parent is the project or initiative it built, not a choice among sources); CSV `CsvWorkTrackingConnector.cs:239` (the Parent Reference Id column is its only source); `PortfolioMetricsService.cs:455` (copies `feature.ParentReferenceId` into a rebuilt record: a copy, not a decision); `WorkItemBase`'s `Update` (`:173`) and copy constructor (a stored value carried over). **Not allow-listed, so they change:** Jira `:1879`, `:1932`, `:1991` go (the factory starts the record empty and `HangUnder` applies the choice); the ADO direct writes at `AzureDevOpsWorkTrackingConnector.cs:148` and `:694` go through `HangUnder` in slice 02 |
| 4 | `ConnectorsNeverUseStoredFeatures` | new type-dependency rule (shape: `:483-495`) | a connector namespace depends on `StoredFeatures` or the feature repository |
| 5 | `NobodyReadsTheLegacyParentColumns` | new source-scan allow-list | a production file outside the allow-list reads `OwnParentOverrideAdditionalFieldDefinitionId`, `ParentOverrideForTeams` or `ParentOverrideForPortfolios` as a property. Allow-list: the owner and connection models (write-through), `LighthouseAppContext`, the migrations, `ConnectionDefaultsDto` (deprecated wire members), and `SupportedConnectionDefaults.cs` (enum names, not reads). `ConnectionFieldDefaults.cs` (the two registry rows) is **allow-listed in slice 01 and removed from the list in slice 03**, which migrates the rows ("Rule 5's allow-list", above; M-1) |

Each is watched to fail once (reintroduce the violation, see the file named) before it is trusted, as the existing ones
were. Also: `FetchShapingPropertyGuardTest` records why `ParentSources` replaced the old registered property.

### Re-review closure (second DESIGN revision)

| Finding | Addressed in |
|---|---|
| H-1 `Place` and single candidates | DD-3, "Change to `ParentResolution.Place` for a single candidate"; component table (`Place`); reader table (`ParentResolutionPlacementTest.cs:52`) |
| H-2 retraction | DD-3, "Retraction"; component table (`HangUnder` modified); DD-9 retraction table; risk D7 |
| H-3 copy constructor and wrap order | DD-3, the wrap bullet; component table (`ParentReadings`); risk D6 |
| H-4 one decider, one writer | DD-3, "One decider, one writer"; rule 3 renamed `NoOneButTheOneHelperAssignsAParent` |
| H-5 / AFK-8 Data Center own parent | Reader table (Jira Data Center row); DD-3, "The `TrackerParent` reading"; UPGRADE-1 |
| M-1 converter, comparer, serialiser construction | DD-1 stored-form table (Converter, Comparer, Construction by the serialiser) |
| M-2 reading-version bump | DD-4 (UPGRADE-1 (A): Jira-wide re-read through `JiraReadingVersion`); slice 01; ADR decision 6 |
| M-3 ADO `CouldNotTellWhatItNames` | Reader table (ADO row); DD-3 "ADO" paragraph; UPGRADE-1 |
| M-4 rule 5 allow-list | "Rule 5's allow-list, and the legacy members that stay or go"; enforcement table row 5 |
| M-5 readers of `ParentCandidatesFromLinks` | "Every reader of `ParentCandidatesFromLinks`" table |
| M-6 no default for AFK-1 | Closed by the maintainer's decision UPGRADE-1 (A) (DD-4); DD-3, the slices and the ADR state (A) |
| m-A line numbers | Reader table and DD-5 (`ConnectionSaveService.cs:91-124`, `PortfoliosOwningAnyOf` at 100-124) |
| m-B owners-per-connection | DD-5 "Bound on the owners of one connection" (a measured DELIVER check) |
| m-C Portfolio with 2+ candidates | DD-3 full-call bullet (`ParentResolution.cs:57-74`: oldest link, then earliest-created Feature, then lowest key) |
| m-D rules 1 and 4 | Enforcement section: all five are new; the source-scan and type-dependency shapes are cited where they exist |
| m-E table name | DD-1 migration paragraph (`"Portfolios"` confirmed against the Portfolios migration) |

**Previous DESIGN review findings m-1..m-6: closure.**

| Finding | Was | Addressed in |
|---|---|---|
| m-1 | Slice 01 inconsistent with the design | `slices/slice-01-jira-reads-the-parent-sources-in-order.md` scope and done-when, now consistent with DD-3 and DD-4 |
| m-2 | Jira citation range | The choice is `JiraWorkTrackingConnector.cs:1907-1936` and the resolution of the named field is `:1944-1952`; both are cited as such in the reader table and DD-3 |
| m-3 | Internal references in the ADR | The ADR draft cites only ADRs and files that resolve (ADR-193, ADR-241, ADR-242); no archived feature ids or finding ids |
| m-4 | Migration `Down` step | DD-1 "Down step" and DD-9 (the Down is run in the migration test) |
| m-5 | Golden fingerprint follower path and digit-string property | DD-9 fingerprint bullet, goldens (b) a follower of a default renders what the default renders and (d) no new-form string matches a legacy digit string or `-` |
| m-6 | List-to-legacy-int projection | DD-9 write-through bullet, the golden projection table |

### Third review closure (round 3)

| Finding | Addressed in |
|---|---|
| H-1 rule 3 scope | Enforcement row 3 (all production source, `ParentReferenceId` assignments only, allow-list with reasons; ADO writes through `HangUnder` in slice 02; shown values covered by behaviour tests) |
| H-2 counts and warnings | DD-3 filter bullet; component table (`PlaceWhatTheLinksLeftOpen`); DD-9 |
| M-1 rule 5 and `ConnectionFieldDefaults.cs` | "Rule 5's allow-list" and enforcement row 5: allow-listed in slice 01, migrated and removed in slice 03 |
| M-2 rule 2 | Enforcement row 2 (reads the parent-source list: Jira and ADO) |
| M-3 Place decision name | DD-3, `Place` change: `OnlyCandidate` for a Portfolio lone candidate |
| Minors | `HangUnder` order (component table); entry constructor throws and the converter does not swallow it (DD-1); Linear- and CSV-shaped records (DD-9) |
| Held questions | Closed: UPGRADE-1 (A) (DD-4), replacing AFK-1, HELD-A and HELD-B |

### Slice impact

Slice 01: model, JSON storage and migration, selector, the record's readings and the widened placement step, Jira, API
with write-through, list control, fingerprint, save guard (still the top of the band; the split, if needed, is Team vs
Portfolio as DISCUSS said). Slice 02: ADO readings and the relations-skip restatement (ADO never needs stored Features,
so it adds no second pass). Slice 03: connection defaults (registry `FieldsOn` replacing the two legacy-int registry rows, DTO members, the same list control). Slice 01 also owns, per the re-review: `Place` and its test, `HangUnder`, the copy constructor, the Data Center reading, the serialiser pins, and the regenerated Jira goldens in `JiraReadingVersionFingerprintTest`.
Slice 01's migration and done-when follow UPGRADE-1 (A) (DD-4). Per the third review: rule 5 lands in slice 01 with the two `ConnectionFieldDefaults.cs` rows allow-listed, and slice 03 migrates them and drops the allow-list entry (M-1); slice 02 routes the ADO writes through `HangUnder` (H-1).

### Risks added by DESIGN

| # | Risk | Response |
|---|---|---|
| D1 | Fall-through changes a result for an existing Jira Team whose single link candidate is not a stored Feature (UPGRADE-1) | Accepted by the maintainer (UPGRADE-1 (A), DD-4); one re-read, release note required, golden pin of the new rule |
| D2 | Autosave can drop a newer edit while a save is in flight (known open item in `useModifySettings`) | Whole-list value; test two quick reorders; do not widen the debounce |
| D3 | The data step writes JSON text that the serialiser does not read back identically | Exact text pinned (DD-1), round-trip test through the serialiser on both providers |
| D4 | Legacy columns tempt a reader (the concrete `Own...` property and the connection ints have readers the compiler does not name) | Reader list in "Constraint and priority data", architecture rule 5 |
| D5 | Rollback to the previous build resolves some lists differently | Write-through keeps legacy-shaped lists exact; the limit is stated (DD-1) and tested |
| D6 | A record reaches the second pass without its readings (a connector forgets to set them, or the wrap's copy constructor drops them) | The record's readings default to empty, which never needs stored Features; the connector tests pin that Jira sets them for every record; a wrap test per `WorkItem` and `Feature` pins the copy constructor (DD-3) |
| D7 | A provisional face-value key is never retracted, or a stale shown key stays on a link entry that lost | `HangUnder` always writes the key (empty clears) and clears the other link entries' shown values; retraction table (DD-3, DD-9) |
| D8 | The migrated `[X, TrackerParent]` changes results for Azure DevOps or for a Data Center owner with an override | Accepted by the maintainer (UPGRADE-1 (A), DD-4); surfaced in the reader table and DD-3; release note required; upgrade tests pin the items that move |

### Open questions for the maintainer (also in the hand-off message)

- **UPGRADE-1 (decided: (A), replaces Q1, HELD-A and HELD-B)** The maintainer accepted the change: a one-time re-read and
  a release note (DD-4). The DISCUSS acceptance criterion AC-1.6, its scenario and the "no refetch" sentences are amended
  to name the items that move.
- **Q2 (decided, AFK-2)** A list default names only saved fields; a field added in the same save is picked on the next
  save.

## Maintainer decisions (DESIGN, to be filled)

| Topic | Decision |
|---|---|
| Q1 single non-Feature link candidate | **Decided (A), 2026-11-02:** the fall-through applies to migrated lists too; one re-read on upgrade; release note required (DD-4). |
| Q2 same-save fields in a list default | **(a), AFK decision:** a list default names only saved fields; a field added in the same save is picked on the next save. See AFK-2. |

## AFK decisions (2026-11-02, orchestrator; the maintainer reviews them on return)

| # | Decision | Why |
|---|---|---|
| AFK-1 (DESIGN Q1) | **Decided (A) by the maintainer, 2026-11-02:** a link entry whose only candidate is not a stored Feature yields nothing and the chain falls through, also for migrated lists. | This changes results for existing installs on upgrade without anyone touching the setting: a Team with override = a link phrase whose single candidate is not a Feature today stores that key (so the item sits under no Feature); after the upgrade the item falls through to the own Parent and may start counting towards a Feature, moving Feature sizes and forecasts once. Accepted; release note required. The per-owner legacy-mode alternative was rejected. |
| AFK-2 (DESIGN Q2) | **(a)**: a list default can name only saved fields. | Bounded; one extra save in a rare flow; no new pending-resolution step. |
| ~~AFK-3~~ (superseded by the revised AFK-3 below) | ~~**Accepted as decided by DESIGN:** legacy int columns frozen~~ (not kept in sync) for one release; old int members stay on the wire for one release as deprecated projections, with the ADR-241 "only act if it differs" rule. | Expand-only and rollback-safe; Lighthouse-Clients has no usages. |
| AFK-4 | **No domain event** for a changed list; the fingerprint drives the refetch (ADR-242). | A second path to the same refetch; a deliberate exception to prefer-events, recorded in the ADR draft. |
| AFK-3 (revised after the DESIGN review, M-2) | ~~Legacy int columns frozen~~ **Legacy int columns are kept in sync for one release (write-through projection):** a list of shape `[X, TrackerParent]` writes X; `[TrackerParent]` writes NULL; any other shape writes its first AdditionalField id (or NULL) and is documented as the **rollback limit** ("a list a pre-upgrade build cannot express resolves as its first field, then the tracker's own parent"). Drop the word "rollback-safe". | A one-release rollback must not silently resolve parents differently for every owner edited after the upgrade; write-through is cheap and keeps the old build honest for every legacy-shaped list. |
| AFK-8 (DESIGN re-review H-5) | **On Jira Data Center, the `TrackerParent` entry means exactly today's own-parent reading: `issue.ParentKey` first, then the instance's parent field (EpicLink / ParentLink, `TheFieldTheInstanceHangsParentsOn`) when the key is empty.** On Cloud it is `ParentKey`. | Keeps DISCUSS example 6 ("a Team with no override changes nothing") true on every Jira flavour; the DC field is part of "the tracker's own parent" as users know it. |
| AFK-7 (DESIGN review B-2) | **Fall-through applies to migrated lists too (UPGRADE-1 (A)), so the upgrade queues a refetch for every Jira owner and every Azure DevOps override owner** (otherwise stored parents stay stale). Migration, fingerprint and the slice 01 done-when are consistent with that. | The review showed the "no refetch" claim was inconsistent with fall-through for migrated lists. |

## Wave: DEVOPS (Apex, PROPOSE, 2026-11-02)

Scope: delivery and operations of an existing platform; no new infrastructure, no new pipeline, no deployment topology
change. Nothing committed, ADO untouched, no file outside this folder edited. UPGRADE-1 is decided as (A); its
operational cost is in section 3.

### 1. Usage-data event (answers DISCUSS Q6)

| Item | Decision |
|---|---|
| Name | `ParentSourcesSavedWithSeveralEntries` |
| Integer | **25**. Re-checked: `origin/main` and this checkout end at `ReportCombinedPdfExportStarted = 24`; Bug 6499's DEVOPS states `UsageDataEventName` stays at 24; Bug 6500 and Story 6502 reserve none (6502 only names a candidate, "outlier popover opened", undecided). No feature folder under `docs/feature` claims 25. **DELIVER slice 01's first step re-reads the file on `main` and takes the next integer then**; tests pin the name and append order after 24, never the integer. If 6502 lands first with 25, this one becomes 26. Append only, never renumber |
| Emitted when | A Team, a Portfolio or a connection default is saved with an own or default parent-source list of **two or more** entries, **and the saved list differs from the one last saved**. Not on a follower, not on a list of one entry, not on a repeat autosave of an unchanged list. Counted once per accepted save |
| Where | The frontend save path of the list (`ParentSourcesList`'s host: unit settings tab and connection page), through the existing `reportUsage` call after the server accepts the PUT, the same way `ReportDeleted` and `WorkTrackingSystemFieldsChanged` are emitted; the backend forwards it through the opt-in pipe. Not emitted by the server's save path: the server also saves on migration and on `lh` echo, which are not somebody choosing a list |
| Properties | **None** (name-only). Not which fields, not how many entries, not the owner, not the tracker kind. No change to `UsageDataEventShapes.cs`; the oracle test for "name-only" covers it |
| Personal data | None. No id, no name, no field name, no free text, no count. The daily event budget is unaffected (one event per deliberate save) |
| KPI served | The story's adoption signal: how many instances use more than one parent place. Outcome KPI "Work Items resolve to the Feature the tracker names" cannot be counted from usage data (it is measured on the reporter's Team, Q7); this event shows only that the feature is used, and is the leading indicator for whether a second tracker's reporter exists |
| Worth it | Yes: cheap (one call in one hook), no property, and without it the multi-source claim in the release notes has no evidence. Emitted in DELIVER slice 01 (the slice that first makes the feature usable); slices 02 and 03 reuse the same call, since the same control is used. The same step lists it in `docs/settings/usagedata.md` |
| `usagedata.md` row (for DELIVER) | **Parent sources were saved with several entries** / Somebody saved the parent sources of a Team, a Portfolio or a connection default with two or more entries, and your server accepted it. Saving an unchanged list, a list of one entry, or switching to follow the default is not counted / Nothing. **Not which fields, not how many, not whose** |

### 2. CI and test placement

- **Categories.** `ci_backend.yml` runs `Category!=Integration` on every build. The migration tests in
  `Lighthouse.Backend.Tests/Integration/Containers/` (for example `ConnectionDefaultsUpgradeMigrationTests`) carry
  **no** `Integration` category (they are `acceptance` plus their story tags; the folder name is not a filter), so they
  run in the regular CI. Put the new migration test, Down-step test and JSON round-trip test in that folder, one fixture
  that is parametrised over SQLite (in-process) and Postgres (`SharedPostgresContainer`, one shared container, a fresh
  database per fixture). Do **not** tag them `Integration`: that would skip them in CI. Connector tests on recorded
  payloads stay in the unit tree (no live tracker, no `JiraIntegration` / `AdoIntegration` tags). The one live ordered-list
  case lives in `JiraWorkTrackingConnectorTest` under `Integration` and `JiraIntegration`, so it runs only in the
  path-scoped job that runs when the `jira_connector` filter fires. That filter (`JIRA_REGEX` in
  `Scripts/test-selection/path-classifier.sh`) covers only `Services/Implementation/WorkTrackingConnectors/Jira/` in
  production and tests; the shared filter adds `Models/WorkTrackingSystemConnection*`, `WorkTrackingSystems.cs`,
  connector base folders and csproj files. It does **not** cover `Models/ParentSource*`, `WorkTrackingSystemOptionsOwner`,
  `WorkItemBase`, `ParentSourceSelector` or `FetchFingerprint`, so a change only there never runs the live case; DELIVER
  either adds those paths to the filter or runs the live case by hand before release. Architecture rules 1-5
  go in the existing ArchUnit fixture and use IL dependencies (Release build drops async edges).
- **HistoricalSchemaPatch.** The new nullable columns `Teams.ParentSources`, `Portfolios.ParentSources`,
  `WorkTrackingSystemConnections.ParentSourcesForTeams` / `ParentSourcesForPortfolios` (all `TEXT`) must be appended to
  `ColumnsAddedAfterTheseFixturesPinnedTheirSchema` in `TestHelpers/HistoricalSchemaPatch.cs`, with the new migration's
  suffix. The same table already carries `ParentOverrideSource`, `ParentOverrideForTeams` and the other defaults
  columns for Teams, Portfolios and connections, so this is the established pattern, not a Deliveries-only case. Missing
  it turns the old-schema fixtures red naming the column. A DELIVER step in slice 01 (slice 03 adds none: the connection
  columns arrive in the same migration).
- **E2E workflows.** `ci_verifysqlite` / `ci_verifypostgres` run E2E against a started backend. `Program.cs` is not
  edited by this design (confirmed: the design lists no startup, DI-root or middleware change; DI additions, if any, are
  in the existing service registration), so the "Program.cs edit forces the full Integration suite" rule does **not**
  apply. These workflows do apply the new migration at startup on both providers over the demo data, which is a free
  second probe. There is no Playwright check for this story (dropped at DISTILL, 2026-11-04), so no new workflow. If DELIVER does touch `Program.cs`, that rule turns on and the live connector categories run.
- **Local gates** unchanged: `dotnet build`, `pnpm build`, the filtered `dotnet test` from CLAUDE.md.

### 3. Migration operations

- **Generation.** One migration via `Lighthouse.Backend/Create-Migration.ps1` (both provider projects), additive only:
  four nullable text columns. Build the migration projects first (HintPath references). Hand-add the data step to both
  provider migrations with the same SQL text (DD-1). The `Up` fills only the new columns; **no legacy column is
  touched, dropped or renamed** (expand-only; the old columns are dropped no earlier than a later release).
- **Release ordering.** Single step: the migration runs at startup, the new build reads only the new columns. Old and
  new build never run against one database at once in a single-instance install. In a multi-replica install a replica one
  version back reads the legacy columns (kept in sync by write-through) and ignores the new ones, so a rolling upgrade
  does not break it; lists of the two legacy shapes resolve identically, the others as the documented rollback limit.
- **Rollback.** Write-through keeps the legacy integer columns equal to the projection of every list written by the new
  build, so rolling the app back one release resolves legacy-shaped lists like the list, within the limits below, and others as
  the stated limit (first field, then the tracker's own parent). The old build cannot read the Data Center parent field
  after an empty `ParentKey`, nor the native parent of an Azure DevOps override owner whose relations it skips; both are
  rollback limits. A rollback also changes `JiraReadingVersion` back, which changes every Jira fingerprint again, so it
  costs a second one-time full re-read per Jira owner, with the same rate-limit exposure as the upgrade's. The `Down` step drops the four new columns only; the test runs `Down`
  and asserts the legacy columns still hold the projection and the new columns are gone. The rollback plan is written
  first in the runbook for the release: restore previous image, no data step to undo, edits made on the older build are
  not carried forward (lists return as they were when rolling forward again).
- **UPGRADE-1 (A): operational cost of the one re-read (`JiraReadingVersion` bump).**
  - Load at upgrade: every Jira Team and Portfolio, and every Azure DevOps owner with an override, is re-read **once**,
    on its next scheduled refresh, as a full download instead of a delta. Refreshes are already scheduled and spread by
    the update loop, not fired at once, so the burst is bounded by the existing refresh cadence and worker concurrency;
    the cost is one full read per owner, the same as after any fetch-fingerprint change (it happened on the
    original-names bump). Azure DevOps override owners also read relations on every refresh from then on.
  - Jira rate limits: Jira Cloud's cost-based limits throttle with 429 and `Retry-After`; the existing connector
    retry/back-off applies. The exposure is a large Jira install (hundreds of Teams on one site and token) whose first
    refresh round after upgrade is heavier. The exposure is accepted: no mitigation is built, and the connector's
    `Retry-After` back-off is the only control. An install can stagger by refresh interval, but nothing here does it.
  - Visible change: some existing items move (see the corrected list in UPGRADE-1, DD-4). The release note says so,
    says Feature sizes and forecasts may change once, and says "first refresh after upgrading re-reads your data once".
  - Linear, CSV and ServiceNow owners ignore the setting and queue nothing.

### 4. Observability

- **Per-item source log:** none. A per-item line is noise at fetch volume. Instead, **one `LogDebug` per fetch** per
  owner: "Parent sources for {Owner}: {Rendered list}; {Count} Work Items resolved by position {Histogram}; {n} fell
  through to stored-Feature placement" built from the already computed `ParentChoice` winner positions (an array
  count, no extra pass, no extra read). Debug level, so it is free in production and available when an admin asks "why
  is this item under that Feature".
- **Findings stay the operator's signal.** One refresh finding per failing entry, as DESIGN says (a name that matched
  nothing, a field that is gone), shown in the existing Refresh History (`docs/settings/systeminfo.md`). No new metric,
  no dashboard, no alert: this changes how a value is chosen, not any SLO. Existing refresh-failure and fetch-duration
  signals cover regressions.
- **How an operator sees that a list changed:** the fingerprint moves, the owner is queued, and the next Refresh History
  entry shows a full download for that owner (existing behaviour of ADR-242). The connection save already logs the
  owners it queued. Add to that existing log call the count of owners moved by the parent-sources list; no new line.
- **Two checks after release (smoke, not monitoring):** (1) the reporter's Team (Q7): the items that name a Feature in a
  listed place now sit under it; (2) after upgrade, one Jira owner's first refresh shows a full download once.

### 5. Lighthouse-Clients

Searched `/storage/repos/lighthouse-clients` (excluding `node_modules` and `.git`) for `parentOverride` and
`parentSources` (case-insensitive): **no match**. The DTO additions (`ParentSources`, connection defaults members) are
additive and the deprecated int members stay on the wire for one release, so the CLI and MCP keep working unchanged and
need no version bump for this story. N/A for a clients release, because nothing consumes the field. The public v1 API
note (DD-6) stands: the new members are additive; the deprecated ones are removed no earlier than a later release.

### 6. Mutation targets (per-feature, break 80)

Run last, on frozen code, with the acceptance suite excluded (it takes 80 minutes against 3).

| Stack | Targets (mutate patterns) | Notes |
|---|---|---|
| Stryker.NET | `Models/**/ParentSources.cs`, `ParentSourceEntry.cs`, `Services/**/ParentSourceSelector.cs` (`Choose`, `ChooseProvisionally`, `HangUnder`), the projection to the legacy int (inside `ParentSources` or its owner write-through), the migration projection (the SQL is not mutated by Stryker; it is pinned by the round-trip test on both providers plus the golden projection table), `FetchFingerprint` rendering of the list | Line-span and whole-file `mutate` patterns do not work in .NET; select per file. Kill survivors with one targeted test rather than long reruns |
| StrykerJS | `ParentSourcesList.tsx` and `reduceParentSources` (add, remove, move, cap at five, picker exclusion, consequence line condition) | Add a feature config like `stryker.config.story-6501-*.mjs`: mutate only the changed files; `break: 80` |

### 7. Docs at finalize (explicit; DELIVER's finalize owns them, not now)

| Doc | Owed | Note |
|---|---|---|
| `docs/portfolios/edit.md` | Yes | "Parent Override Field" becomes Parent sources: the order rule, the optional own Parent, follow or own, the migration |
| `docs/teams/edit.md` | Yes | Same for Teams (also `docs/teams/detail.md` and `docs/portfolios/detail.md` if they mention the old name; grep at finalize) |
| `docs/settings/worktrackingsystems.md` | Yes | Connection defaults for the list |
| `docs/concepts/worktrackingsystems/jira.md` | Yes | Link-phrase entry, fall-through with no stored Feature, the one-time re-read |
| `docs/concepts/worktrackingsystems/azuredevops.md` (line ~222) | Yes | Custom field entries, relation-type entry, the relations-read condition |
| `docs/settings/systeminfo.md` | Yes | The per-entry refresh finding in Refresh History |
| `docs/settings/usagedata.md` | Yes | The row in section 1, in the slice that emits it |
| ~~Per-feature screenshots, demo data (a Team with two sources)~~ | **Dropped** (maintainer, DISTILL 2026-11-04) | The demo data reads from one CSV connection and the control is hidden on CSV, so it cannot show a Team with two sources. No Jira or Azure DevOps demo connection is added for this story |
| Release notes | Not written at finalize (they are written at `/release`), but **release note required: some existing items may move on upgrade (a Data Center item with no Jira Parent reads the instance's parent field, an Azure DevOps item of an override owner hangs under its native parent, an item whose only link is no Feature falls through to the next source), and Feature sizes and forecasts may change once** (UPGRADE-1 (A), DD-4); also say the first refresh re-reads data once, and that Azure DevOps owners that had an override now read relations on every refresh. The ADO Story carries the `Release Notes` tag | Checklist item for finalize and `/release` |
| ADR promotion, ADR-270 and `ARCHITECTURE.md` section 4, `jobs.yaml` | Yes | As DESIGN listed |

### AFK decisions (DEVOPS)

| # | Decision | Why (blast radius) |
|---|---|---|
| AFK-D1 | Usage event `ParentSourcesSavedWithSeveralEntries`, integer 25 (re-read on `main` at DELIVER step one), name-only, emitted by the frontend save path after an accepted, changed save of a list of two or more entries | Additive, name-only; small |
| AFK-D2 | Migration tests live in `Integration/Containers`, with no `Integration` category, so CI runs them on both providers; `HistoricalSchemaPatch` gets the four new TEXT columns | Test placement; small |
| AFK-D3 | No new workflow and no change to `ci_verifysqlite` / `ci_verifypostgres`; `Program.cs` untouched | Nothing to change; small |
| AFK-D4 | Migration is one additive step; `Down` drops only the new columns; rollback plan written first; legacy columns written through and never read (rule 5) | Matches DESIGN; small |
| AFK-D5 | Observability: one debug line per fetch per owner with position counts, no per-item log, no metric, no alert; the existing connection-save log gains an owners-moved count | Cheap; small |
| AFK-D6 | Lighthouse-Clients: no consumers, no release step | Verified by grep; small |
| AFK-D7 | Mutation targets and docs list as in sections 6 and 7 | Process; small |
| AFK-1 | Decided (A) by the maintainer. The (A) refetch cost and the 429 back-off note are in section 3 | Existing installs change results once on upgrade; release note required |

## Maintainer decision on UPGRADE-1 (2026-11-02, evening)

Binding; it closes AFK-1, HELD-A and HELD-B.

**UPGRADE-1 = (A) Accept the change.** The data step migrates every override owner to `[X, TrackerParent]` and every
other owner to `[TrackerParent]`; the new fall-through rule applies everywhere; one re-read on upgrade (Jira-wide via
`JiraReadingVersion`; Azure DevOps override owners through the new fingerprint form). Some items whose override named
nothing usable newly hang under their own Parent (or the Data Center parent field) and may start counting towards a
Feature, so Feature sizes and forecasts move once; **a release note is required**. Option (B), the per-owner legacy
flag (`AtFaceValue`), is rejected and removed from the design, the ADR draft, the slices and DEVOPS.

**Correction (DISTILL review, verified against the code):** on Jira an item with an empty override already falls back to
`issue.ParentKey` today (`JiraWorkTrackingConnector.cs:1899`, and `HangUnder` keeps the parent for an empty key), so such
items do **not** move, and class (1) of "Items that move" above is struck. What does move: (a) on Data Center an item with
an empty `ParentKey` now reads the instance's parent field; (b) on Azure DevOps an item of an override owner with no override
value now hangs under its native parent; (c) an item whose only link candidate is no stored Feature falls through to the
next entry. The release note stays required.

## Maintainer decisions (DISTILL sketch walk-through, 2026-11-04)

Binding. They supersede the DISCUSS sketch, DD-7 and DD-9 where they disagree. This closes pre-requisite P3.

| Topic | Decision |
|---|---|
| Layout | **As sketched.** The heading is "Parent sources" on Team and Portfolio settings, and it replaces "Parent Override" on the connection page too. |
| Override is a list | **Confirmed by the maintainer:** the override is a list as well, not only the default. "Set here" on a Team or Portfolio holds its own ordered list, the same control and rules as the connection default. |
| Reorder | **Up and down buttons only. No drag.** Supersedes DD-7's drag enhancement: no `useDragReorder` hook, `ItemListManager` stays untouched, and the "::" handle leaves the sketch. |
| Last row | **The remove button on the only remaining row is disabled,** with the tooltip "A list needs at least one source". The server still refuses an empty list. |
| Linear, CSV, ServiceNow | **The Parent sources control is hidden on trackers that do not read it.** Supersedes Q11, A6's "offered, ignored" and the out-of-scope line "hiding the control there". Their parent behaviour is unchanged; docs say the setting applies to Jira and Azure DevOps only. |

### Test weight (maintainer, 2026-11-04)

Keep heavy tests few. Rules, conversions and validation are pinned at the cheapest level that can show them: plain unit and table tests on the domain and on `reduceParentSources`; acceptance scenarios at the application-service port; one thin HTTP skeleton per flow; one or two rendered wiring tests per user flow; ~~one Playwright sanity check~~ (dropped by the later decision on the DISTILL upstream issues). Where DD-9 reads as a wider list of host-level or rendered tests, this rule wins. Drag tests are gone with the drag.

## Wave: DISTILL / [REF] Scope and reconciliation (2026-11-04)

Nothing was compiled or run; no commit, no ADO. The compile check and the fail-for-the-right-reason run belong to the orchestrator.

Reconciliation of DISCUSS, DESIGN, DEVOPS and the DISTILL maintainer decisions: **no contradiction that blocks the scenarios**; one real issue below, and the stale wording the last section supersedes is listed as such.

Superseded, not contradictions: DD-7's drag enhancement and the "::" handle; DD-9's "reorder by button and drag"; Q11, A6's "offered, ignored" and the out-of-scope line about hiding the control; the DISCUSS sketch's "On Linear/CSV the control is unchanged". The heading "Parent sources" replaces "Parent Override" on the connection page too.

Design claims checked against the code (line references have moved, the claims hold; DELIVER re-greps as DESIGN says):

| Claim | Code today |
|---|---|
| `HangUnder` writes `ParentReferenceId` only for a non-empty key, one override slot | holds (`ParentSourceSelector.cs`) |
| `Place` returns `NotAsked` for anything not ambiguous | holds (`ParentResolution.cs`) |
| Copy constructor copies `ParentCandidatesFromLinks` | holds (`WorkItemBase.cs:16`) |
| Jira `ParentReferenceId = issue.ParentKey` | now `JiraWorkTrackingConnector.cs:1899` (was 1879) |
| Jira populate then hang | now `:1915` / `:1918` / `:1948` (was 1895 / 1897 / 1914) |
| Data Center field readers | now `:327`, `:335`, `:1359`, `:1370`, `:1984`, `:2001` |
| ADO direct parent writes | now `AzureDevOpsWorkTrackingConnector.cs:148` and `:713` (was 694) |
| ADO reads of the override | now `:798`, `:977`, `:983`, `:1417`, `:1538` |
| Save guard predicates | `ConnectionSaveService.cs:91-120`, as described |
| `ParentPlacementRule` is matched in a total switch | holds (`ParentPlacementReport.cs:60-64`), so no `OnlyCandidate` member is scaffolded; the lone-candidate test asserts "not `OnlyFeatureAmongLinks`" |

## Wave: DISTILL / [REF] Upstream issues

1. **Closed (maintainer, 2026-11-04): the Playwright check, its page object and the finalize demo-data item are dropped; a skipped live ordered-list case sits in `JiraWorkTrackingConnectorTest` instead, and the same literal list text is pinned in the frontend and backend tests.** Original issue, kept for the record: E2E "on seeded demo data" cannot show the control. DD-9 and the maintainer's E2E rule want one Playwright check on demo data "a Team with two sources". The demo data reads from one **CSV** connection (`DemoDataFactory.CreateDemoWorkTrackingSystemConnection`), and the maintainer decided (2026-11-04) that the control is **hidden on CSV**. So demo data cannot carry the check. Taken here, until the maintainer says otherwise: the one E2E check builds a Team on a Jira connection through the API helpers, as `DeliverySourcePicker.spec.ts` does (live Jira credentials, which CI already holds), and never refreshes it. Finalize's "demo data (a Team with two sources)" item in DEVOPS section 7 has the same problem: a Jira or Azure DevOps demo connection would have to be added to the demo data, or that item is dropped.
2. **Scaffold signature versus design.** DESIGN has `HangUnder` read the link entries from the record's own readings. The record does not carry them yet and `WorkItemBase` is an existing entity, so the scaffold takes the readings as an argument: `HangUnder(record, readings, choice)`. DELIVER may reshape it; the three `HangUnder` tests then change their call, nothing else.
3. **Closed (maintainer, 2026-11-04): a relation-type entry reads no relations and raises one finding; DD-9 is corrected in place.** Original issue: DD-9 and the reader table disagree about when Azure DevOps reads relations (slice 02). DD-9 ("Connectors" bullet) says relations are read "when a `TrackerParent` **or a relation-type entry** is listed or dependencies are read". The reader table (DESIGN, "Constraint and priority data"), the ADO paragraph of DD-3 and slice 02's scope all say relations are read when the list holds `TrackerParent` or dependencies are read, **and that a relation-type entry or an unclassifiable field no longer forces the read**. The Azure DevOps scenario pins the second form (three places against one, and the more specific text): `[X]` with a relation-type or unclassifiable X reads no relations and raises one finding. If the maintainer prefers DD-9's wording, one sentence of that scenario flips; nothing else depends on it.
4. **What the written scenario bodies cannot show (second pass).** (a) The Jira harness cannot show that records needing no stored Features are equal before and after the second pass, nor that the stored Features are read once: both need arrangement at `WorkItemServiceTest` level, left to DELIVER. (b) The Data Center scenario answers the deployment as `Server` but the harness serves only the Cloud search endpoint, so DELIVER may have to serve the Data Center path. (c) The connection harness's stand-in tracker hangs an item under the first field it is handed, so "reads the new order" is shown by which field it was asked to read first. (d) The live Jira case shows the link entry and the own-parent-only-when-listed rule on `LGHTHSDMO-1726`; which of two filled sources wins would need an item carrying two different parents seeded in the test project, which was not done.

## Wave: DISTILL / [REF] Test weight

The maintainer's rule (few heavy tests, cheapest level per behaviour) was applied as follows. Counts are test methods, with parameter rows in brackets. Every test is skipped (`[Ignore]` / `it.skip` / `test.skip`).

| Level | Slice 01 | Notes |
|---|---|---|
| Backend plain unit and table | 18 [about 62 rows] | `ParentSourcesTest` 6 [22], `ParentSourceSelectorOrderTest` 6 [22], `ParentResolutionLoneCandidateTest` 4, `FetchFingerprintParentSourcesTest` 2 [6, plus an enumeration of the 64 valid lists over three fields and the own Parent] |
| Backend application-service scenarios | 5 (Jira refresh) | `ParentSourcesRefreshScenarios`, bodies written (second pass) |
| Backend HTTP | 2 [3 rows] | one host built once: save a list and read it back; refusals for a sixth entry and a duplicate |
| Backend migration (real SQLite and Postgres) | 2 | one class; Postgres from the shared container with a fresh database |
| Frontend pure | 6 declarations [22 rows] | `reduceParentSources` and its helpers |
| Frontend rendered | 3 on the control, 2 on the host | the host pair is "shown on Jira, hidden on Linear" |

Heavy tests, and why each earns its cost: (a) the migration class, because two real providers are the only place the data step's SQL text can disagree with the serialiser, and it also runs the Down step; (b) the HTTP fixture, one host for the fixture, because the DTO, the refusal mapping and the write-through are only seen together there; (c) the Jira refresh scenarios, five of them on one host built once for the fixture; (d) `A_refresh_places_every_Work_Item_by_its_own_readings_and_counts_only_the_ambiguous_one`, a real database with the real work item service, because the retraction and "stored Features read once" claims are about that seam.

Loading and failure looks: the list adds no request and renders inside the form. The existing `ModifyTeamSettings` tests ("renders loading state initially", "shows the could-not-load words when the settings cannot be fetched") show the loading and failure words but did not assert that the Advanced Configuration section stays away, so one small rendered test was added to that file, "shows no Advanced Configuration, where the Parent sources sit, while the settings load or when they cannot be loaded", and it runs now (it passes on today's code, since the section is already hidden). The row for a saved field the loaded definitions do not hold ("Unknown field (id 14)") is pinned in the control's rendered test.

## Wave: DISTILL / [REF] Scenarios, slice 01 (Jira reads the parent sources in order)

Untagged by design; the classification lives here.

| Scenario | Level | File |
|---|---|---|
| One to five different entries are kept in the order given (4 rows) | unit | `Models/ParentSourcesTest.cs` |
| An empty list, a sixth entry, a duplicate (field or own Parent), a field with no id are refused with the reason (5 rows) | unit | same |
| A list projects to the first field an older build reads, or nothing (5 rows, the golden projection) | unit | same |
| The stored text is the exact text the upgrade writes, and reads back identically (2 rows) | unit | same |
| Stored text the rules refuse fails loudly (5 rows) | unit | same |
| The first listed source with a value wins; order decides; no value hands over; nothing is guessed; a field wins though it names no Feature; a source that cannot be read yields nothing; a link to a stored Feature wins; a link whose only candidate is no stored Feature falls through; a Portfolio does not narrow (11 rows) | unit | `Services/Implementation/Parents/ParentSourceSelectorOrderTest.cs` |
| Several candidates: the oldest link wins | unit | same |
| The connector's provisional choice stops at the first link needing a Feature test (7 rows) | unit | same |
| Applying a choice moves the shown key to the winning link entry; an empty choice retracts; applying twice is a no-op (3) | unit | same |
| A lone candidate is judged like several (Team Feature, Team non-Feature, Portfolio, none) (4) | unit | `Models/ParentResolutionLoneCandidateTest.cs` |
| A list renders each entry and position; no new form matches an old fingerprint (6 rows + 64-list enumeration) | unit | `Services/Implementation/WorkItems/FetchFingerprintParentSourcesTest.cs` |
| The upgrade lists each override with the own Parent, followers stay NULL, defaults list; round trip; Down keeps the old columns | migration, both providers | `Integration/Containers/ParentSourcesUpgradeMigrationTests.cs` |
| A Team admin saves a list and reads it back in order; a sixth entry and a duplicate are refused and nothing is saved | HTTP | `API/Integration/OrderedParentSources/ParentSourcesSettingsApiIntegrationTest.cs` |
| A refresh places every Work Item by its own readings and counts only the ambiguous one; moving a source up moves the item; Data Center parent field only when listed; an unmatched entry is named and the next read; after the upgrade only the items named change parent | application service | `API/Integration/OrderedParentSources/ParentSourcesRefreshScenarios.cs` (5, bodies written) |
| Readings survive the wrap; a changed list moves only its owner's fingerprint and a follower renders the default's list | model, no host | `API/Integration/OrderedParentSources/ParentSourcesOwnerScenarios.cs` (2) |
| A field cannot be removed while any list holds it, in any place, an owner's or the default | application service | `ConnectionParentSourcesScenarios.cs` (counted under slice 03) |
| The list writes its first field into the old column and a follower leaves it; a build one release back agrees with the list for the two shapes it can express and gives the documented limited result for the rest (5 + 1 + 6 rows) | unit | `Models/ParentSourcesOwnerWriteThroughTest.cs` |
| Entries are written and read with the default options as the upgrade writes them; an invalid entry is never built by the serialiser | unit | `Models/ParentSourceEntrySerialiserTest.cs` |
| One ordered list read against real Jira, read-only, skipped | live | `JiraWorkTrackingConnectorTest.GetWorkItemsForTeam_UseOrderedParentSources_ReadsTheLinkAndTheOwnParentOnlyWhenListed` |
| Reorder by buttons, no drag; the last row cannot be removed; the picker offers only what is not listed; at five add is off with its reason; an unknown field reads "Unknown field (id N)"; the consequence line when the own Parent is absent | frontend | `parentSources.test.ts`, `ParentSourcesList.test.tsx` |
| The control reads "Parent sources" on Jira and is absent on Linear, CSV and ServiceNow | frontend host | `pages/Common/AdvancedInputs/AdvancedInputs.parentSources.test.tsx` |

Error and edge share: about half of the unit rows are refusals, fall-throughs or no-value cases.

## Wave: DISTILL / [REF] Adapter coverage, slice 01

| Adapter | Real-I/O scenario |
|---|---|
| EF `LighthouseAppContext` (the new text columns, the data step) | the migration class on SQLite and Postgres |
| HTTP API (Team settings GET and PUT) | `ParentSourcesSettingsApiIntegrationTest` |
| Jira connector, recorded payloads (readings, Data Center field) | `ParentSourcesRefreshScenarios`, through the Jira wire-format builders; one skipped live case in `JiraWorkTrackingConnectorTest` |
| React control and host | rendered tests above; the browser-to-API step is pinned by the same literal list text on both sides |

## Wave: DISTILL / [REF] Driving adapter coverage, slice 01

Team settings endpoints: the HTTP fixture. The refresh (scheduled or manual): the first application-service scenario. The settings page: the rendered host test, whose list text is the one the HTTP fixture posts. The Portfolio settings endpoint is the same controller seam as the Team's and is **not** exercised separately; DELIVER adds a Portfolio row to the HTTP refusal table if the Portfolio controller turns out to hold its own refusal code.

## Wave: DISTILL / [REF] Production scaffolds, slice 01

Each carries `SCAFFOLD:`; `grep -rn "SCAFFOLD: Story 6501" Lighthouse.Backend/Lighthouse.Backend Lighthouse.Frontend/src` lists them.

- `Models/ParentSourceEntry.cs` (new): `ParentSourceKind` enum and `ParentSourceEntry` record, plain values.
- `Models/ParentSources.cs` (new): the value object. Every member throws `NotImplementedException("SCAFFOLD: ...")`; `ParentSourcesViolation` and `ParentSourcesOutcome` are plain values.
- `Models/ParentReadings.cs` (new): `ParentReading`, `ParentReadingAt`, `ParentReadings`, plain values.
- `Models/ParentChoice.cs` (new): plain value.
- `ParentSourceSelector.cs`: added `Choose`, `ChooseProvisionally` and the `HangUnder(record, readings, choice)` overload, all throwing. Existing members untouched.
- `FetchFingerprint.cs`: added `RenderParentSources`, throwing. Existing members untouched.
- `components/Common/ConnectionDefault/parentSources.ts` and `ParentSourcesList.tsx` (new), throwing.

Not scaffolded, on purpose: the owner and connection members (`ParentSources` property, mutators, write-through), the DTO members, the record's `ParentReadings`, the new migration and its columns. They change existing contracts or the EF model, so the tests that need them are the application-service scenarios and the HTTP fixture (raw JSON, which compiles without them). The second pass added additive throwing scaffolds for the pieces the scenario bodies call: `OwnParentSources`, `FollowParentSourcesDefault` and `TheEffectiveParentSources` on the owner, `SetParentSourcesDefaultForTeams` and `...ForPortfolios` on the connection, and a `[NotMapped]` `ParentReadings` property on the work item record that nothing sets and the copy constructor does not carry. They are methods and a not-mapped property, so the EF model is unchanged.

## Wave: DISTILL / [REF] Test placement, slice 01

Backend unit tests sit beside the code they pin (`Models/`, `Services/Implementation/Parents/`, `Services/Implementation/WorkItems/`); the migration class is in `Integration/Containers/` with **no** `Integration` category, so CI runs it on both providers; the scenarios and the HTTP fixture are in a new `API/Integration/OrderedParentSources/`. Frontend tests are beside the component. The scenario harnesses gained vocabulary (additive): `ParentPlacementAcceptanceTest` (entries, a list the Team owns, custom field values, a Data Center switch), `ConnectionDefaultsAcceptanceTest` (a default and an owner's list saved and read by name) and `NamedDependencyLinkAcceptanceTest` (a Team's list).

## Wave: DISTILL / [REF] Pre-requisites for DELIVER, slice 01

1. The migration test pins the previous migration ids (`20261023085259_...` for SQLite, `20261023085311_...` for Postgres). If another migration lands first, bump them.
2. Add the four new TEXT columns to `HistoricalSchemaPatch.ColumnsAddedAfterTheseFixturesPinnedTheirSchema` in the same step as the migration, or the old-schema fixtures turn red naming the column (DEVOPS AFK-D2).
3. The new settings field goes through the whole path (`SettingsOwnerDtoBase`, the sync helper and `SettingsOwnerDtoRoundTripTest`, hand-built PUT payloads, pinned payload shapes), per `docs/ci-learnings.md`.
4. The existing `ParentResolutionPlacementTest.One_candidate_is_not_a_question_for_the_pick` asserts today's behaviour for a lone link; replace its lone half with `ParentResolutionLoneCandidateTest` when `Place` changes.
5. Accessible names the rendered tests rely on: `Move {name} up`, `Move {name} down`, `Remove {name}`, a combobox named "Add a source", the heading "Parent sources", the texts "A list holds at most 5 sources" and "A list needs at least one source" (the last as the tooltip of the disabled remove button). Change the tests with the control if its names differ.
6. The DEVOPS usage event is emitted by the frontend save path; no test was written for it here (see "Left to DELIVER").
7. Copy a licence file into a fresh worktree (two Licensing tests).

## Wave: DISTILL / [REF] Left to DELIVER, slice 01

The five architecture rules (new ArchUnit rules, each watched to fail once); the `WorkItemServiceTest` arrangements that move from `ParentCandidatesFromLinks` to readings, with the "records needing no stored Features are equal before and after, stored Features read once" checks; the `JiraReadingVersionFingerprintTest` golden regeneration; the null-stays-null and bad-text tests on the EF converter (they need the converter and its column, which DELIVER writes); the usage-data event test. The scenario bodies, the owner write-through and rollback table and the entry serialiser tests are written (second pass).

## Wave: DISTILL / [REF] Scenarios, slice 02 (Azure DevOps reads the parent sources in order)

| Scenario | Level | File |
|---|---|---|
| The tracker reads its own parent only when the list holds it (5 rows); serves Jira's Data Center lookup and Azure DevOps' relations read | unit | `Services/Implementation/Parents/ParentSourceSelectorOwnParentTest.cs` |
| The second custom field gives the parent when the first is empty, and the order decides | application service | `API/Integration/OrderedParentSources/AzureDevOpsParentSourcesScenarios.cs` |
| The native parent and the relations are read only when the list or the dependencies ask; an unclassifiable field is read as a field and reads no relations | application service | same |
| A relation type in the list is named in Refresh History and the other entries still give parents | application service | same |
| After the upgrade an item with no field value and a native parent hangs under it, once; relations are read from then on | application service | same |
| A Team that reads only fields keeps the dependencies it showed | application service | same |
| An Azure DevOps row reads "Parent (Azure DevOps' own)" | frontend unit | `parentSources.test.ts` (slice 01 file, one row) |

Not duplicated here, on purpose: "changing the list refetches" and "a field in use cannot be removed" are tracker-neutral and are pinned once in slice 01 (fingerprint and connection-save scenarios). The routing of the connector's two direct parent writes through the one helper is the architecture rule on parent assignments, which DELIVER adds; the helper itself is pinned by the `HangUnder` tests of slice 01.

Weight: 1 unit method [5 rows] and 5 application-service scenarios (bodies written in the second pass). No new HTTP or rendered test: a Team's settings page and its save are the same code for either tracker.

## Wave: DISTILL / [REF] Adapter coverage and driving adapters, slice 02

| Adapter | Real-I/O scenario |
|---|---|
| Azure DevOps connector over the faked work item tracking client (real connector, real service, real database) | the five scenarios above |
| Azure DevOps API | none live; the contract is the recorded work item, as in the named-dependency scenarios |

The driving adapter is the refresh, covered by the same scenarios. The settings endpoints are covered by slice 01's HTTP fixture.

## Wave: DISTILL / [REF] Production scaffolds, slice 02

- `ParentSourceSelector.ReadsTheTrackersOwnParent(ParentSources)`: an overload, throwing. The existing overload is untouched.

## Wave: DISTILL / [REF] Pre-requisites for DELIVER, slice 02

1. Slice 01's model, migration, API and control are built first.
2. Build the scenarios on the harness the named-dependency scenarios use (`NamedDependencyLinkAcceptanceTest`, `AnAzureDevOpsOrganisation`); the request count is read off the faked client's calls.
3. ADO owners with an override read relations on every refresh after the upgrade (more requests; watch for 429 back-off, DEVOPS section 3).

## Wave: DISTILL / [REF] Scenarios, slice 03 (the connection default holds the parent sources)

| Scenario | Level | File |
|---|---|---|
| A change of the default refreshes exactly the Teams that follow it, and a Team with its own list is left alone | application service | `API/Integration/OrderedParentSources/ConnectionParentSourcesScenarios.cs` |
| A new Team follows the default and reads its list on the first refresh | application service | same |
| A System Admin saves the default as a list and reads it back; the old number on the wire is its first field; a save that leaves the list out changes nothing, an explicit null clears it | application service, over the connection harness | same |
| The connection page shows the Teams default as the same ordered list under "Parent sources" and saves the new order | frontend host | `components/Common/Connection/ModifyConnectionSettings.parentSources.test.tsx` |
| An old single default shows as the field then the tracker's own parent, and its followers keep their parents | covered | the Jira slice's migration class (connection defaults) and its upgrade scenario |
| A default cannot name a field the connection lacks, and the refusal names the default | covered | the Jira slice's connection-save scenario ("any list holds it", a default included) |
| A default list obeys the same one-to-five, no-duplicate rules; its legacy projection | covered | `ParentSourcesTest` (the same value object and the same projection function) |
| The control is hidden on Linear, CSV and ServiceNow on the connection page | covered | the connection already offers only the defaults its tracker supports (`supportedDefaults`); `parentSourcesAreOffered` pins the rule for the Team and Portfolio pages |

Weight: 4 application-service scenarios (bodies written in the second pass; the fourth is the field-in-use rule shared with the Jira slice) and 1 rendered test. No new unit or HTTP test.

## Wave: DISTILL / [REF] Adapter coverage and driving adapters, slice 03

The connection save and the follower queueing run over the real database in the connection harness; the connection page is the rendered test. The Portfolios default is the same code with its own column and is **not** given its own scenario; DELIVER adds a Portfolio row to the third scenario's payload check.

## Wave: DISTILL / [REF] Production scaffolds, slice 03

None. Everything slice 03 needs to compile is slice 01's scaffolds; the connection's list members change an existing contract (the defaults payload and the EF model), so that test body is left to DELIVER.

## Wave: DISTILL / [REF] Pre-requisites for DELIVER, slice 03

1. Slice 01 first; the two registry rows in `ConnectionFieldDefaults.cs` move to the list in this slice and leave the legacy-column rule's allow-list.
2. The existing tests that name the old label or arrange the single default move with the code: `ModifyConnectionSettings.directionTips.test.tsx` (the "Parent Override Field" row and its About button), `ModifyConnectionSettings.test.tsx`, `ConnectionDefaultsDtoTest`, and the connection-defaults scenarios named in the class comment of `ConnectionParentSourcesScenarios`.
3. `ConnectionParentSourcesScenarios` builds on `ConnectionDefaultsAcceptanceTest`; extend that harness rather than adding a host.

## Wave: DISTILL / [REF] Totals and what is unwritten

| Level | Methods or declarations | Rows | Skipped |
|---|---|---|---|
| Backend plain unit and table | 24 (19, plus the owner write-through and rollback table with 3, and the entry serialiser with 2) | about 90 | all (`[Ignore]` on the fixture) |
| Backend application-service scenarios, bodies written | 16: Jira refresh 5, owner and record 2, connection 4, Azure DevOps 5 | | all |
| Backend live (real Jira, read-only) | 1, in `JiraWorkTrackingConnectorTest` | | `[Ignore]` and the class's `Integration` categories |
| Backend HTTP (one host, built once) | 2 | 3 | all |
| Backend migration, both providers | 2 | | all |
| Frontend pure | 6 | 22 | all (`it.skip`) |
| Frontend rendered | 3 on the control, 2 on the Team host, 1 on the connection page | | all |

Second pass: `dotnet build` of `Lighthouse.Backend.Tests.csproj` and `pnpm exec tsc -b` both finish clean; no test was run. Still left to DELIVER: the five architecture rules, the `WorkItemServiceTest` arrangements, the EF converter's null-stays-null and bad-text tests, the Jira reading-version golden regeneration and the usage-data event test, all listed under slice 01 "Left to DELIVER"; a Portfolio-specific HTTP row and a Portfolio default scenario. Loading and failure are pinned by the new `ModifyTeamSettings` test named above. Playwright is gone from the story.

Fix pass after the review gate: the entry constructor validates, so refused lists are built from stored text; the connection scenarios seed a Portfolio default and the HTTP refusals start from a saved list and include the empty list; the saved-list text is pinned at the request body in `TeamService.test.ts` and posted byte-identically by the backend HTTP test (the host test now compares objects); every `it.skip` carries a narrow Biome suppression where Biome flags it (`it.skip.each` is not flagged); the hostless record-and-owner test moved to `Models/ParentSourcesOnTheRecordAndOwnerTest.cs`.

## Maintainer decisions on the DISTILL upstream issues (2026-11-04)

Binding. They close upstream issues 1 and 3 and the "unwritten on purpose" note on the scenario bodies.

| Topic | Decision |
|---|---|
| Application-service scenarios | **Written in DISTILL,** with real Given/When/Then bodies, still skipped. Where a body needs a contract that is not there yet, it is written against the planned shape as far as the code compiles, and what is missing is named. |
| Playwright check | **Dropped, with its page object.** No UI run is needed for this story. A live ordered-list case joins `JiraWorkTrackingConnectorTest` (real Jira, read-only on the test project), next to the existing parent override tests. The finalize "demo data (a Team with two sources)" item is dropped too. |
| Browser-to-API step | Without Playwright nothing covers it end to end, so the same literal JSON for a saved list is pinned on both sides: the frontend's rendered save test and the backend's HTTP test. |
| Azure DevOps relation-type entry | **Reads no relations and raises one finding.** DD-9's "or a relation-type entry" is wrong and is corrected. |

## Wave: DISTILL / [REF] Fail-for-the-right-reason check (2026-11-04)

Every Story 6501 test except the live Jira case was unskipped once, run, and skipped again.

| Stack | Result | Reason |
|---|---|---|
| Backend | 74 fail on a scaffold | Behaviour unbuilt, as intended |
| Backend | 16 fail on an assertion or a missing column | Behaviour unbuilt: connection refuses `parentSourcesForTeams` (4), Team settings carry no list (3), lone candidate not placed and readings not carried (4), serialiser does not refuse a bad entry (3), the migration's columns are absent (2) |
| Backend | 12 pass | 10 existing `ParentSourceSelector` tests caught by the name filter, 1 existing link test, 1 serialiser round trip that needs no new behaviour |
| Frontend | 25 fail on a scaffold, 2 on the missing control | Behaviour unbuilt, as intended |
| Frontend | 1 failed for the wrong reason, then corrected | An absence check threw because today's page shows "Parent Override Field" twice. It now counts matches. |

No test failed on set-up, a fixture or an import. The live Jira case was not run.

Rerun after the review fixes (2026-11-04): backend 73 fail on a scaffold and 17 on an assertion or a missing column, frontend 26 on a scaffold and 4 on the missing control or a missing line, all because the behaviour is unbuilt. None failed on set-up, a fixture or an import. The request-body pin in `TeamService.test.ts` already passes, because it pins text the code hands on unchanged. With everything skipped, the backend suite and all frontend tests pass.

## Wave: DISTILL / [REF] Final review gate (2026-11-04)

DISCUSS and DEVOPS and DESIGN conditionally approved, DISTILL approved, no blocker and no high open. The conditions were applied in the fix pass.

Open lows for DELIVER, to pick up when each file is touched:

- Rewrite "second pass" in `ParentSourcesOnTheRecordAndOwnerTest.cs` and `ParentSourceSelectorOrderTest.cs` as plain behaviour.
- Arrange the starting list in the Given of the last test in `ParentSourcesSettingsApiIntegrationTest.cs`.
- Remove the hedge "check that when this is first run" from the live case in `JiraWorkTrackingConnectorTest.cs`, once it has run.
- The Linear host check in `AdvancedInputs.parentSources.test.tsx` overlaps the `parentSourcesAreOffered` table; keep it as the one wiring check or drop it.
- The `jira_connector` path filter does not cover `Models/ParentSource*`, `ParentSourceSelector` or `FetchFingerprint`, so a later change only there skips the live Jira case. Slice 01 touches the connector, so it runs then.

## Maintainer decisions (DELIVER entry, 2026-11-05)

| Topic | Decision |
|---|---|
| Rigor profile | Slice 01 `thorough`, slice 02 `standard`, slice 03 `standard`. |
| nWave version | The story finishes on 3.22.2; the 4.x upgrade follows it. |
| AFK run | Refactor and review per slice, mutation once in the final gates, nothing pushed before the hold. |

## Maintainer decisions (DELIVER, copy, 2026-11-06)

| Topic | Decision |
|---|---|
| Remaining "Parent Override Field" copy | Renamed to "Parent sources" wherever a user reads it. The wire name of the setting stays. |
| Refresh History, a Jira entry matching nothing | `Parent source "Epic Name" matched no field and no link type. The next source in the list was read instead.` |
| The tracker's own Parent in the list | Confirmed as designed: it shows as a regular row, pre-filled, and can be moved or removed like any other. It is not a hidden fallback. |

## AFK decisions (DELIVER, 2026-11-06; the maintainer reviews them at the hold)

| Topic | Decision |
|---|---|
| DD-6, the old int on PUT | Amended. DD-6 said the old int counts only when `parentSources` is absent. Today's page and scripts echo the whole GET, so that rule would answer 200 and silently drop an edit of the old field. Now: when the list sent equals what the owner already reads, and the old field was sent and differs from the list's projection, the old field is the edit and becomes `[X, own Parent]`. In every other case a sent list wins. A client that sends the list must leave the old int out or keep it in step. |
| A list entry whose field is gone | Named `Unknown field (id N)` in Refresh History, the same words as the settings control. |
