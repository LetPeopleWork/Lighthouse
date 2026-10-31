# RCA: Bug 6458, category lists cannot tell a raw state from a mapping name

All paths are relative to the repository root. Line numbers are against `03b033c30` (main after Bug 6454).
Bug 6454's RCA (`docs/feature/bug-6454-state-mapping-reuses-state-name/rca.md`) is the predecessor. Its
branch A ("one string namespace") is the root this document carries to a fix.

## 1. Problem statement and scope

Two defects, investigated as separate chains:

- **(a)** A Team's or Portfolio's To Do, Doing and Done lists hold raw tracker states and state-mapping names
  as the same plain strings. Two hand-edit sequences that the 6454 guards cannot see still lose a raw state
  or leave a stale chip.
- **(b)** Adding a new state mapping starts an autosave with the still-empty mapping, and the backend answers
  400 Bad Request.

**In scope:** where the category lists are stored, read and written (entity, EF, DTOs both ways, TS types);
every place that expands a mapping name into raw states or collapses a raw state into a mapping name; the
settings editor, its reconciliation and its autosave gate; the public `v1` API that Lighthouse-Clients uses.

**Out of scope:** the 6454 fix itself, which is correct for every path its tests pin. Wait states,
cycle-time boundaries and refinement states also reference mapping names (section 3, completeness check).
They are listed because the fix must keep them working, not because they are defects of this bug.

### Reproduced (throwaway Vitest probe against the shipped helpers, deleted afterwards)

The inputs and outputs below are exact, so each row can be turned into a committed regression test in
DISTILL. That is recommended, because the probe file itself was not kept.

| Probe | Sequence | Result |
|---|---|---|
| P1 | Mapping `In Arbeit = [In Progress]`, Doing `[In Arbeit, In Progress]` | Clash error shown, autosave paused |
| P1 | ...the user removes the `In Progress` chip from Doing by hand | Doing `[In Arbeit]`, **no error, no blocker**. The backend expands `In Arbeit` into `[In Progress]`, so the raw German state is dropped (6454 probe B) |
| P2 | `A = [X]` applied: Doing `[Y, A]`. The user re-adds `X` by hand, then adds `B = [X]` | Doing `[Y, A, X]`. Error text reads `Mapping "A" has the same name as the Doing state "A"`, which is misleading |
| P2 | ...the user deletes `A` | Doing `[Y, A, B]`, **no error**. `A` is now a leftover raw string that no mapping explains |
| P3 | Add mapping (`{name:"", states:[]}`) | Autosave blockers `[]`. The editor shows 2 errors. The backend rejects the save |
| P3b/c | Name `In` with no states, or states with no name | Blockers `[]`, so the backend rejects the save |
| P4 | `A = [X]`, `B = [X]` | Blockers `[]`, so the backend rejects the save (`appears in multiple mappings`) |

## 2. Symptoms

- **S1 (P1).** Removing a shadowing mapping's other states by hand makes the shadowing mapping look applied.
  The error clears, autosave sends it, and items in the raw state fall out of WIP, cycle time, throughput
  and forecasts with no warning.
- **S2 (P2).** After a hand edit makes two mappings share a source state, deleting one of them leaves its
  name in Doing as a ghost raw state. The other mapping is applied next to it.
- **S3 (P3, P3b/c, P4).** Every Add mapping click, and every incomplete or overlapping mapping, triggers an
  autosave about 300 ms later. The backend answers 400 and the indicator reads "Couldn't save".

## 3. Toyota 5 Whys

### Branch A: plain-string category lists (S1, S2)

- **WHY 1A.** The raw state is dropped (S1), and a ghost entry survives (S2).
  [Evidence: P1 and P2. The backend expansion `GetRawStatesForCategory` replaces any entry equal to a mapping
  name with that mapping's members and keeps the entry only when no mapping matches
  (`Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystemOptionsOwner.cs:208-228`, the decision at
  `:214-224`). `AllStates` and `OpenStates` are built from it (`:33-38`), and so is `MapStateToStateCategory`
  (`:188-206`).]
- **WHY 2A.** The frontend's only way to know whether a Doing entry is "the mapping" or "a raw state" is
  to infer it from the strings. A hand edit to Doing changes the inputs of that inference without passing
  through reconciliation.
  [Evidence: `isApplied` is a pure function of `(mapping, doing, mappings)`. It holds when the name is in
  Doing and no other member is (`Lighthouse.Frontend/src/utils/stateMappingReconciliation.ts:67-81`).
  `doingClashError` is suppressed whenever `isApplied` holds (`utils/stateMappingValidation.ts:75-91`).
  Removing a Doing chip goes through `doingHandlers.onRemove`, not `reconcileDoingStates`
  (`components/Common/Team/ModifyTeamSettings.tsx:225-245` against `:247-258`), so after P1's hand edit the
  shadowing mapping meets the "applied" predicate exactly. In P2 the undo step skips `A` because `A` shared a
  state when it was deleted (`stateMappingReconciliation.ts:108-117`, `:74`), so its name chip is never
  removed.]
- **WHY 3A.** The category lists carry two meanings at once. (1) They say which tracker states belong to
  To Do, Doing and Done, which drives the queries and the categorisation. (2) They are the ordered
  **display** columns, in the mapped namespace. The mapping name is written into Doing to serve (2), and
  that is what makes (1) ambiguous.
  [Evidence: the reconciliation appends the mapping name to Doing so that the list shows the group
  (`stateMappingReconciliation.ts:89-97`, `:135-139`, and the editor copy "replaced by the group name" at
  `components/Common/StateMappings/StateMappingsEditor.tsx:111-115`). The metrics page reads
  `settings.doingStates` as the Work Item Aging columns (`pages/Teams/Detail/TeamMetricsView.tsx:96-99`,
  then `components/Common/Charts/WorkItemAgingChart.tsx:103,279-280,618-623`), and items carry the
  **mapped** name as their state (`MapRawStateToMappedName`, `WorkTrackingSystemOptionsOwner.cs:240-246`, used
  by every connector: Jira `JiraWorkTrackingConnector.cs:1791`, ADO `AzureDevOpsWorkTrackingConnector.cs:1222`,
  Linear `LinearWorkTrackingConnector.cs:188,358,435`, CSV `CsvWorkTrackingConnector.cs:227`, ServiceNow
  `ServiceNowWorkItemMapper.cs:121`, and transitions in `WorkItemStateTransitionMapper.cs:13-14`).]
- **WHY 4A.** Two different sources of truth describe one fact: which raw states a mapping stands for.
  `StateMapping.States` says it, and so does the absence of those states from the category list, and nothing
  keeps the two consistent except a stateless frontend diff. The backend applies mappings for display
  **unconditionally** (`MapRawStateToMappedName` looks at no category list). "In effect" exists only as a
  frontend guess about the category lists. It is not stored, so the backend cannot validate it.
  [Evidence: `StateMapping` is `Name` plus `States` only (`Models/StateMapping.cs:3-8`), with no flag or
  category. The backend validator checks only names, emptiness and shared states
  (`API/Helpers/StateMappingValidator.cs:17-62`). The 6454 collision branch was removed because the backend
  could never decide it (6454 RCA WHY 4C, commit `186055be3`). Mapping names also leak into other teams'
  state suggestions as if they were raw states (`API/SuggestionsController.cs:70-72,87-89`;
  `components/Common/StatesList/StatesList.tsx:83-127`).]
- **WHY 5A. Root cause A.** The data model stores a **derived view** (the list with members collapsed under
  the mapping name) in place of the **facts** (the raw states of each category). Once that collapse has
  happened, the stored strings cannot be uncollapsed without ambiguity. Every guard written since (picker,
  reconciliation, validation, blockers) reconstructs the facts from the view, and each new hand-edit
  sequence finds a view that two different fact sets produce. 6454's three review rounds (feature-delta
  decisions, `docs/evolution/2026-10-31-bug-6454-state-mapping-reuses-state-name.md` "Decisions") are this
  root cause producing its next instance. It is a design flaw, not a code bug.

### Branch B: empty or incomplete mapping autosave returns 400 (S3)

- **WHY 1B.** The PUT fails with 400.
  [Evidence: `handleAddMapping` appends `{ name: "", states: [] }` straight into the settings
  (`StateMappingsEditor.tsx:65-68`). `onChange` writes it with `updateSettings`
  (`ModifyTeamSettings.tsx:249-257`, Portfolio `ModifyProjectSettings.tsx:259`). The autosave effect then
  dispatches 300 ms later whenever `formValid` holds (`hooks/useModifySettings.ts:50,327-353`). The backend
  rejects the empty name and the missing states (`StateMappingValidator.cs:23-31`) with `BadRequest`
  (`API/TeamController.cs:208-212`, `API/PortfolioController.cs:109-112`, and on create
  `TeamsController.cs:103`, `PortfoliosController.cs:94`). The error branch sets only `saveState = "error"`
  (`useModifySettings.ts:260-269`).]
- **WHY 2B.** `formValid` does not see an incomplete mapping.
  [Evidence: the only mapping rule in the autosave blockers is `stateNameClashErrors`
  (`ModifyTeamSettings.tsx:96-101`, `ModifyProjectSettings.tsx:104`), and it first filters out every
  mapping with no states or no name (`utils/stateMappingValidation.ts:122-123`). Probes P3, P3b and P3c
  return `[]`.]
- **WHY 3B.** The page runs two mapping validations with different rule sets. The display set
  (`validateStateMappings`, which covers fields, unique names, unique states and clashes,
  `stateMappingValidation.ts:132-141`, rendered via `ModifyTeamSettings.tsx:172-178`) is a superset of the
  save gate (clashes only). So the editor shows an error that autosave ignores. The same gap lets duplicate
  names and shared source states (P4) through to a 400.
  [Evidence: P3 shows the editor errors 2 while the blockers are 0. P4 shows the blockers `[]` while the backend
  rejects the save (`StateMappingValidator.cs:46-59`).]
- **WHY 4B.** The gate was designed for an explicit Save button, and autosave changed what "save" means
  without reclassifying draft input. Mappings shipped on 2026-03-28 (`3a02b6abd`), when `formValid` gated a
  Save click and did not include mapping errors either (`git show 42cd80d7f^:…/ModifyTeamSettings.tsx`, where
  `formValid` at `:53` is separate from `stateMappingErrors` at `:90`). Autosave replaced the button on
  2026-05-29 (`42cd80d7f`), so "the user clicks Save on a half-filled row" became "every Add mapping click
  saves a half-filled row". 6454 added clash blockers but left field completeness out (feature-delta: "This
  was already the case before Bug 6454").
- **WHY 5B. Root cause B.** The editor has no draft state. A mapping row the user is still filling in is the
  same object as a persisted mapping, so the payload autosave sends always includes unfinished rows. The
  save gate is a hand-maintained subset of the backend rules, with no shared contract that forces the two to
  agree.

### Completeness check (other places looked at)

- **Every expansion site** goes through `GetRawStatesForCategory` and `AllStates`/`OpenStates`, so a
  model-level fix reaches all of them:
  - Jira: the query (`JiraWorkTrackingConnector.cs:2705`), categorisation (`:1794`) and transitions (`:1795`).
  - ADO: the queries (`AzureDevOpsWorkTrackingConnector.cs:318,357,1542,1545`), categorisation (`:1203`) and
    started/closed dates (`:1266-1268`).
  - Linear: the queries (`LinearWorkTrackingConnector.cs:130,609`) and categorisation (`:346,426`).
  - CSV: the filter (`CsvWorkTrackingConnector.cs:218-227`), plus the demo journey, which reads
    `owner.DoingStates`/`DoneStates[0]` **unexpanded** (`:300-351`).
  - ServiceNow: `ServiceNowStateSpanMapper.cs:81-82,111`, `ServiceNowWorkItemMapper.cs:112`,
    `ServiceNowWorkTrackingConnector.cs:419`.
  - `IssueFactory.cs:248-252`.
  - Metrics: `BaseMetricsService.cs:384`, `TeamMetricsService.cs:420,439,495,594`,
    `PortfolioMetricsService.cs:349,359,391,743`.
  - Refinement (`RefinementList.cs:37`) and the cycle-time validator (`CycleTimeDefinitionValidator.cs:12-22,68`).
- **Board import** (`AzureDevOpsWorkTrackingConnector.cs:590-592`, `ServiceNowBoardMapper.cs:78-80`) writes
  raw states only. Not affected.
- **Other mapping-name references.** Wait states, cycle-time boundaries and refinement states are compared
  in the mapped namespace and expanded on demand (`TeamMetricsService.cs:495`, `BaseMetricsService.cs:384`,
  `RefinementList.cs:33-38`, `RefinementSettingsValidator.cs:63-73`, which says "A mapped name is an entry
  of those lists in its own right", and `TeamExtensions.cs:127-129`). A fix that removes mapping names from
  the category lists must give these a list of display names to draw from.
- **Raw-list readers missed by a pure expansion search** (found in peer review): `TeamMetricsService.cs:463,584`
  and `PortfolioMetricsService.cs:319,496` use `DoingStates` directly as the Age-in-State and display column
  order. They compare against **mapped** item states, so they depend on mapping names being in the list.
  Section 6.2 moves them to the display list.
- **Adjacent, Hypothesis (code-read, not probed).** The as-of projections categorise the stored, already
  **mapped** transition state: `TeamMetricsService.cs:742-743` and `PortfolioMetricsService.cs:251` call
  `MapStateToStateCategory(stateOnDay.ToState)`. For a mapping whose name is not one of its members, the
  expanded lists do not contain the name, so the result would be `Unknown`. Verify with a unit probe. It is
  not caused by this bug, but the fix touches the same method and should settle it.
- **FetchFingerprint** renders the category lists and the mappings (`FetchFingerprint.cs:43-48,87-90`). Any
  stored rewrite costs one full fetch cycle. That is harmless, but say so in the release note.
- **Clients.** Lighthouse-Clients passes settings through as opaque JSON (`packages/client/src/index.ts:836`
  `LighthouseWritePayload = Record<string, unknown>`, `:2397-2413`). The CLI forwards a user-supplied
  `--payload-json` (`packages/cli/src/index.ts:607-612,1883,2023`). No client source names `doingStates` or
  `stateMappings` (grep on `56158e5`). The `v1` and `latest` routes share one controller and DTO
  (`TeamsController.cs:19-20`, `TeamController.cs:24-25`), so the DTO **is** the public contract.

### Alternative hypotheses considered and eliminated

| Hypothesis | Eliminated because |
|---|---|
| One more frontend predicate closes S1/S2 | Section 4: P1's post-edit input is byte-identical to a correctly applied self-consistent mapping as far as `(mappings, doing)` can tell, so no function of those inputs separates them. Excluded by the maintainer too |
| The backend could validate the shadowing case | It receives the same collapsed strings (6454 WHY 4C). It becomes decidable only once the lists hold facts |
| S3 is a backend over-strictness | The empty mapping is invalid by any definition; the defect is that it is sent |

## 4. Backwards-chain validation

- **Root A to S1.** If the stored list is a collapsed view, then `[In Arbeit]` with mapping
  `In Arbeit = [In Progress]` means "the mapping". The raw German state has no representation, so P1 drops
  it. Yes.
- **Root A to S2.** If "applied" is reconstructed from strings, a mapping that was not applied when deleted
  (because it shared a state) has its name chip treated as a raw state. That chip survives, which is P2. Yes.
- **Root B to S3.** If draft rows are part of the payload and the gate omits the field rules, every Add
  mapping click sends `{name:"",states:[]}` and gets 400 (P3, P3b, P3c, and P4 for shared states). Yes.
- **Cross-check.** A and B are independent: B occurs with no name collision at all, and A occurs with
  complete mappings. Together they explain S1 to S3.

## 5. Contributing factors

1. A Doing chip looks the same whether it is a raw state or a mapping (`StatesList.tsx`), so the user cannot
   see the meaning change in P1.
2. Mapping names are offered as suggestions in all three lists (`StatesList.tsx:83-127`), and other teams'
   mapping names come back as "states" (`SuggestionsController.cs:70-72,87-89`).
3. An item in an unmapped or unknown state vanishes without warning (6454 P1, still open).
4. The 400 response is shown only as "Couldn't save" (`useModifySettings.ts:268`). The backend's message
   names the fix but is not displayed.
5. Mapping rules exist twice, in C# and in TS, with different subsets enforced at save time.
6. The CSV demo journey reads unexpanded Doing entries (`CsvWorkTrackingConnector.cs:300-351`), so it
   silently depends on today's string semantics.

## 6. Proposed fix

### 6.1 Branch B (empty or incomplete mapping): frontend, small, independent

- **B1 (permanent).** Treat an incomplete row (blank name **or** no states) as a draft. A pure
  `toSavePayload(settings)` drops such rows from `stateMappings` before autosave dispatches. It also skips
  reconciliation for them, which already happens: `stateMappingReconciliation.ts:121-122` skips a mapping
  with no states. The row stays in the editor and keeps showing its field error. Wire it through
  `useModifySettings` (one call site at `:333-336`), so Teams, Portfolios and the default-settings pages all
  get it.
- **B2 (permanent).** Rules that make a **complete** set of mappings invalid (duplicate name, a source state
  in two mappings) join the autosave blockers. Build the blockers from the same `validateStateMappings`
  minus the draft-only field rules, so that the display and the gate cannot drift apart again (root cause B,
  part 2).
- **B3 (detection).** Surface the 400 body text in the save indicator (contributing factor 4). This is a UI
  change, so it needs a sketch.
- Ask the maintainer whether B1 or "pause autosave while a draft row exists" is preferred. B1 lets unrelated
  edits keep saving while a row is half filled. The pause variant blocks them. B1 is recommended.

### 6.2 Branch A: model change. Three shapes compared

| | (i) Typed entry list | (ii-a) Mapping carries its category or "in effect" flag | **(ii-b) Lists hold raw states only. A mapping is a pure display grouping** |
|---|---|---|---|
| Stored shape | `DoingEntries: [{kind: State\|Mapping, value}]`, a new JSON column per list | Raw lists, plus `StateMapping.Category` (and a position) | **Unchanged columns.** The lists hold raw tracker states only. `StateMapping {Name, States}` is unchanged |
| Ambiguity removed? | Yes | Yes | **Yes: a list entry is always a state** |
| Sources of truth for "which raw states a mapping stands for" | Still two (the mapping's members, and the entry kind with the members' absence) | Two (the flag, and the members in or out of the list) | **One (`StateMapping.States`)** |
| Frontend reconciliation | Still needed: apply swaps members for a mapping entry, undo swaps back, and a deleted mapping leaves a dangling entry to clean up (S2's class survives as "dangling typed entry") | Needed (keeping the flag in step with the list) | **Deleted.** The Doing chips are a derived view: `collapse(rawDoing, mappings)` |
| Position of the group in Doing (aging columns, cycle-time order) | The entry's index | Needs a new position field | The position of its first member (members kept adjacent by the editor) |
| Connector or expansion change | `GetRawStatesForCategory` takes typed entries, so every call site changes (about 25, section 3) | `AllStates` changes | **Small.** The category lists are read as they are, never expanded. Inside the owner that means `AllStates`, `OpenStates` and `MapStateToStateCategory`. Three connector call sites expand the lists directly and change to read them as they are: ADO `:1266-1268`, `IssueFactory.cs:250-252` and `ServiceNowStateSpanMapper.cs:81-82`. Jira, Linear and CSV read only `AllStates`, so they are untouched. The expansion stays for legacy payloads and for wait-state, cycle-time boundary and refinement names. Note that the expansion is **not** the identity on a raw list in which a state shares a mapping's name, which is why the lists must not go through it |
| Backend validation | Can check the kinds | Can check the flag | **Decidable for the first time**, because the lists hold facts. The invariants V1 to V5 are in the concrete shape below. V3 is the 6454 rule, now enforced on the backend |
| API `v1` compatibility | Breaking if `doingStates` changes type. It needs parallel fields, dual write and dual read across releases | Additive field. Old clients ignore it | **Same field types.** Values change meaning only from "may contain mapping names" to "raw states". Legacy payloads that contain mapping names are normalised on write (below) |
| Migration | Schema: add three JSON columns per table (Teams, Portfolios) for each provider, backfill, keep the old columns and dual-write (the drop needs a later release) | Schema: change the mapping JSON (a member inside an existing text column). Backfill the flag | **No schema change.** A data rewrite inside the existing columns: expand mapping names into their members, in place, preserving order |
| Rollback to the previous release | The old binary ignores the new columns, but the old columns must still be right, so dual-write is mandatory | Safe | **Safe for the backend on lists that pass V3 and V4**: the old expansion of such a list gives the same *set* (proof sketch below; `AllStates` uses `Union`, and categorisation is membership). **Not proven for the old frontend:** its `reconcileDoingStates` (`stateMappingReconciliation.ts:98-144`) re-collapses members on the next mapping edit. That is a rollback test plus a release-note line |

**Recommendation: (ii-b), subject to the DESIGN wave and to the property test below.** It removes root cause A rather than labelling it. The category lists go back to
holding facts, and the collapsed view is computed, never stored. It deletes the reconciliation code that
caused 6454 and both 6458 paths, instead of making that code type-aware. It needs three small connector edits, no
schema migration and no DTO type change. It also makes the backend able to validate mappings. (i) is what
the reporter proposed and is sound, but it keeps two sources of truth and needs the heaviest migration. Its
real advantages are an explicit index for the group and no write-path normalisation. (ii-b) pays for them
in the display-list call sites (section 3, about six backend sites) and in the normalisation rule. The
comparison is a recommendation, not a decision. The DESIGN wave should re-cost both with the site list
below.

**(ii-b) concrete shape**

- **Model** (`Models/WorkTrackingSystemOptionsOwner.cs`):
  - The invariant is that `ToDoStates`, `DoingStates` and `DoneStates` hold raw states. `AllStates`,
    `OpenStates` and `MapStateToStateCategory` (`:33-38,188-206`) read the lists as they are and stop
    calling `GetRawStatesForCategory` on them. The same goes for the three connector sites in the table.
  - Keep `GetRawStatesForCategory` for mapped-name references only: wait states, cycle-time boundaries
    and refinement.
  - Add `DisplayStatesFor(category)`, the one place the view is computed. It is a single pass over the raw
    list, comparing ignore-case and trimmed. Each raw state is replaced by `MapRawStateToMappedName(state)`,
    and only the first occurrence of each display name is kept. So a group sits at its **first member's
    index**, even when its members are not adjacent. Pin that with a test that has non-adjacent members.
    The API cannot enforce adjacency, so this rule, not the editor, defines the position.
  - These sites move to `DisplayStatesFor(Doing)`, because they compare against **mapped** item states:
    - the Age-in-State columns: `TeamMetricsService.cs:463`, `PortfolioMetricsService.cs:319`;
    - the cumulative-flow and display order: `TeamMetricsService.cs:584`, `PortfolioMetricsService.cs:496`;
    - refinement candidates: `TeamExtensions.cs:127-135` (`SyncRefinement`, where `.OfType<string>()`
      would otherwise drop a chosen mapped name silently), the Portfolio twin, and
      `RefinementSettingsValidator.cs:69`;
    - the boundary check in `CycleTimeDefinitionValidator.cs:12-22`.
  - `MapStateToStateCategory` also accepts a mapped name, because the as-of projections pass one
    (`TeamMetricsService.cs:743`, `PortfolioMetricsService.cs:251`). The category of a mapped name is its
    members' category when they all share one. Otherwise it is `Unknown`, and V1 keeps that case out of new
    data. This also settles the as-of hypothesis in section 3.
- **Write path** (`API/Helpers/TeamExtensions.cs:66-71` then `:89-91`; the same in `PortfolioExtensions.cs`).
  The mappings must be synced **before** the lists, because the normalisation needs the incoming mappings.
  Today the order is the other way round.
  - **Pre-check (rejects with 400, naming the fix):**
    - **V3**: an entry equals a mapping's name, and that name is not one of the mapping's members.
    - **V4**: a mapping member equals another mapping's name.
  - **Why V3 is a deliberate break.** V3 is the one shape whose two meanings cannot be told apart.
    - Read as legacy, it is the 6454 configuration, which silently loses the raw state.
    - Read as new, it is a raw state that happens to share the name.
    - Rejecting it is a documented break of a configuration that loses data today. The 6454 frontend already
      blocks it (`stateMappingValidation.ts:75-91`), so only API clients can send it.
    - This is the "marker" case, and it is handled by refusal rather than by guessing.
  - **Normalisation** (single pass over the original list, comparer `OrdinalIgnoreCase` after trim, the same as
    `GetRawStatesForCategory:214-215`):
    ```
    for each entry e in list (in order):
        emit e                                    # after the pre-check, e is a raw state; a mapping name passes V3
        if e names mapping M:                     # so e is one of M's own members
            emit each member of M not already in the list or emitted, in M's order
    dedupe ignore-case, keeping the first occurrence
    ```
  - **Equivalence proof sketch.** Let L pass V3 and V4, and let n be an entry that names M. Then n is in M.
    - The legacy expansion maps n to M.States. Every other entry stays (V4 means no other entry names a
      mapping through a member).
    - So the legacy set is (L minus n) ∪ M.States, which equals L ∪ M.States because n is in M.States.
    - The normalised set is also L ∪ M.States.
    - On a new-style list where every member is already listed, nothing is emitted beyond L, so the list
      passes through **unchanged, in order**.
  - **Property test before committing to (ii-b):** for every list and mapping set that passes today's
    validator and V3/V4, `set(Expand(normalise(L))) == set(Expand(L))`, and `normalise` is idempotent.
    Generate cases with self-named mappings, duplicates, case and whitespace variants, and members spread
    across lists.
  - **What still cannot be decided.** A new-style raw state `A`, sent together with a mapping `A = [A, X]`
    where `X` is unlisted, is read as legacy and gets `X` added. That changes what is fetched without a 400.
    It is consistent with today's meaning of the same payload, so it is accepted for API clients and
    documented in the API reference.
- **Read path** (`API/DTO/SettingsOwnerDtoBase.cs:18-41`): `toDoStates`, `doingStates` and `doneStates`
  return raw states. Add read-only `doingDisplayStates` (and To Do/Done twins if wanted): an **additive**
  field for the metrics page and charts. Whether to add them, and which, is a DESIGN decision.
- **Validation** (`API/Helpers/StateMappingValidator.cs`), checked against the normalised lists. V3 and V4
  are the pre-checks above. The other three are:
  - **V1 (new or changed mappings only).** The members that are listed sit in one category. A member listed
    nowhere is allowed and gets an informational warning only. Legacy rows that break V1 keep saving, the
    same as `RefinementSettingsValidator.cs:60-63` judges only states added in this save. Otherwise an
    unrelated edit would 400 forever.
  - **V2.** A mapping's name equals no listed raw state of a different category than its members.
  - **V5.** The existing rules: name, emptiness, duplicate names, shared source states. They need the category lists, so `ValidateSettings` takes the whole DTO, which it
  already receives (`:8-15`). Keep the frontend's `stateMappingValidation.ts` as a mirror of the same rules
  for inline messages, or serve the rules once (see 6.1 B2). Both directions are now judged on facts, not
  guessed.
- **Frontend:**
  - `models/Common/BaseSettings.ts:25-27` keeps `string[]` (raw).
  - `StatesList` renders Doing through `collapse(doing, mappings)`, with mapping chips marked as such. That
    needs a sketch.
  - Removing a mapping chip removes its members, or is refused with "delete the mapping instead". That is a
    DESIGN choice.
  - `StateMappingsEditor` picks sources from raw Doing.
  - `reconcileDoingStates` and its predicates (`isApplied`, `wouldShadow`, `sharesStateWithAnother`,
    `groupingMappingOf`) are deleted, and so are the doing-clash rules in `stateMappingValidation.ts`.
  - `TeamMetricsView.tsx:98` and the Portfolio twin read the display list.
  - Wait-state, cycle-time and refinement pickers draw from the display list.
  - Stop offering mapping names as list suggestions (`StatesList.tsx:83-127`), and stop
    `SuggestionsController` returning them.
- **Migration (expand-only):**
  - No column is added, dropped or renamed. A data step rewrites `Teams` and `Portfolios`
    `ToDoStates`/`DoingStates`/`DoneStates` with the write-path normalisation above, so no stored meaning
    changes. Members are written at the old chip's index, so the column order is kept (R3).
  - It runs in C# (a startup fix-up or a migration with code), because the mapping is JSON inside `text`
    and the lists are `text[]` on Postgres but JSON `text` on SQLite (`LighthouseAppContextModelSnapshot.cs`
    for both providers, `LighthouseAppContext.cs:628-651`).
  - It is idempotent: the normalisation's output passes V3 and V4 and holds every member of each mapping it
    names, so a second run emits nothing.
  - Use `CreateMigration` if a migration class is chosen.
  - Stored rows that fail the V3 pre-check (shadowing, configuration B) are migrated with the **legacy**
    expansion: the entry is replaced by its members, which keeps today's meaning, so the lost raw state stays
    lost. The rewrite is logged per owner. The raw state cannot be recovered from the strings, so the release
    note says to re-add it.
  - Stored rows that break V1 are left as they are (V1 judges only changed mappings).
  - Stored rows that break V4 (a chained mapping) keep the legacy one-level expansion. The rewrite is logged,
    and the next save reports V4.
- **CSV demo journey** (`CsvWorkTrackingConnector.cs:300-351`). Under raw lists, consecutive members of
  one mapping map to the same name. That builds self-transitions (`A` to `A`), which this path does not
  filter (unlike `WorkItemStateTransitionMapper.cs:17`). It must iterate `DisplayStatesFor(Doing)`, or filter
  equal from/to pairs. This is a demo-data regression to pin with a test.

**If (i) is chosen instead:** add `ToDoEntries`/`DoingEntries`/`DoneEntries` as new nullable JSON columns
(each provider). Keep the string lists as written mirrors for one release (expand-only). Add `doingEntries`
to the DTO as **additive**, and keep `doingStates` accepted and returned (the write path prefers entries when
present). Every `GetRawStatesForCategory` caller listed in section 3 moves to the typed lists. The CLI and
MCP need nothing for reads (opaque), but documentation and the skills must say which field wins on write.

### 6.3 Early detection (both chains)

- A warning when stored lists hold names that no tracker state or mapping explains (S2's ghost), and when
  items land in `Unknown` (contributing factor 3).
- One shared rule set for "is this payload saveable", tested against the backend validator with the same
  fixtures, so the two cannot drift (root cause B).

## 7. Files affected (recommended shape)

| Area | Files |
|---|---|
| Model | `Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystemOptionsOwner.cs`, `Models/IWorkItemQueryOwner.cs` |
| API | `API/Helpers/TeamExtensions.cs` (sync order, normalisation, `SyncRefinement:127-135`), `API/Helpers/CycleTimeDefinitionValidator.cs`, `API/Helpers/PortfolioExtensions.cs`, `API/DTO/SettingsOwnerDtoBase.cs`, `API/Helpers/StateMappingValidator.cs`, `API/Helpers/RefinementSettingsValidator.cs`, `API/SuggestionsController.cs` |
| Data | A data-fix migration (Postgres and SQLite) or a startup fix-up, `Data/LighthouseAppContext.cs` (unchanged columns) |
| Connectors | `AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:1266-1268`, `Factories/IssueFactory.cs:250-252`, `ServiceNow/ServiceNowStateSpanMapper.cs:81-82` (read the lists as they are), `Csv/CsvWorkTrackingConnector.cs:300-351` (demo journey, review only). Jira and Linear are untouched |
| Metrics | `TeamMetricsService.cs:463,584,742-743`, `PortfolioMetricsService.cs:251,319,496` (display list, mapped-name categorisation) |
| Frontend | `utils/stateMappingReconciliation.ts` (delete most), `utils/stateMappingValidation.ts`, `components/Common/StateMappings/StateMappingsEditor.tsx`, `components/Common/StatesList/StatesList.tsx`, `components/Common/Team/ModifyTeamSettings.tsx`, `components/Common/ProjectSettings/ModifyProjectSettings.tsx`, `hooks/useModifySettings.ts` (B1), `models/Common/BaseSettings.ts`, `pages/Teams/Detail/TeamMetricsView.tsx`, `pages/Portfolios/Detail/PortfolioMetricsView.tsx`, wait-state, cycle-time and refinement editors |
| Tests | `WorkTrackingSystemOptionsOwnerTest.cs`, `StateMappingValidatorTest.cs`, `StateMappingSyncTest.cs`, controller `*_InvalidStateMappings_ReturnsBadRequest`, a migration fixture test (legacy names expand, idempotent), `stateMappingReconciliation.test.ts` (mostly removed), `stateMappingValidation.test.ts`, `StateMappingsEditor.test.tsx`, `ModifyTeamSettings`/`ModifyProjectSettings` autosave tests (B1/B2), E2E `tests/helpers/api/teams.ts`, `portfolios.ts`, `models/common/BaseSettingsPage.ts` |
| Docs | Settings and state-mapping docs, the API reference for `doingStates` semantics, the Lighthouse-Clients skills/docs (DISCUSS checklist: versioning), `ARCHITECTURE.md` |

## 8. Risk assessment

- **R1 (medium).** The meaning of `doingStates` in `v1` GET changes from "may contain mapping names" to "raw
  states". It is not a type break, and write normalisation keeps old payloads meaning the same. But a script
  that **reads** `doingStates` to show columns would see members. Mitigation: additive display fields and a
  release note. Lighthouse-Clients needs no code change (opaque payloads).
- **R2 (medium).** The data rewrite runs on every instance. It is mitigated by today's expansion semantics
  (no meaning change), idempotency, and rollback safety (section 6.2 table). Test it on both providers.
- **R3 (medium).** Where a group sits in Doing changes from "where its chip was" to "where its first member
  is". Aging columns and cycle-time ordering could reorder for existing users. Mitigation: the migration
  writes members at the old chip's index, so the order is preserved.
- **R6 (medium).** The V3 and V4 pre-checks turn an API payload that is accepted today (a shadowing or
  chained mapping) into a 400. This is a deliberate break: today that payload silently loses data. The 400
  message names the fix, and the API reference and the Lighthouse-Clients skills must say so. That is the
  DISCUSS clients-versioning item.
- **R7 (medium).** The peer review found six raw-list readers that the first draft missed (section 3). A
  DESIGN wave should search `DoingStates`, `ToDoStates` and `DoneStates` again, and every expansion and
  collapse helper, before it fixes the site list.
- **R4 (low).** B1 changes what autosave sends. It is frontend only and leaves complete payloads unchanged.
- **R5 (low).** The latent as-of `Unknown` (hypothesis) may change historical charts once it is fixed.
  Probe it first.

**Verdict, plainly:**
- **Branch B is a code bug.** Fix it directly (B1 and B2). It does not need a DESIGN wave and can ship
  before the model change.
- **Branch A is a design flaw** (a derived view stored as fact) with a public-API, migration and UI
  dimension. It warrants a **DESIGN wave** before anyone writes code. That wave would settle (ii-b) versus
  (i), the display fields, the chip interaction, and the migration form, with the UI sketches CLAUDE.md
  requires. Do not add a further frontend guard for S1 or S2.
