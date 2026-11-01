# RCA: Bug 6454, a state mapping that reuses the name of an existing state confuses Lighthouse

All paths are relative to the repository root. Line numbers are against `75e90b4f3`.

## 1. Problem statement and scope

A team (or portfolio) has the same workflow state in two languages, say `In Arbeit` and `In Progress`, both
in its Doing states. The user creates a state mapping named `In Arbeit` meant to hold both states. Once it
is saved and the team updated, work items that are in the German state `In Arbeit` are no longer counted.

**In scope:** how state mappings are stored, expanded (mapping name to raw states), applied (raw state to
mapping name), categorised (To Do / Doing / Done), queried from the trackers, validated (backend and
frontend), and edited (the State Mappings editor and the reconciliation of the Doing list).

**Out of scope:** persistence. Mappings are a JSON column (`Data/LighthouseAppContext.cs:643-648`) with no
unique constraint, and nothing threw or overwrote on a duplicate key. React keys are not involved either:
the mapping cards are keyed by a stable id (`StateMappingsEditor.tsx:100-105,128`), and the chip lists
never hold the same string twice in this scenario.

### Reproduced (throwaway probes, deleted afterwards)

Backend, `Team` model plus `StateMappingValidator`:

| Configuration | `AllStates` (what the connectors query) | Category of raw `In Arbeit` | Validator |
|---|---|---|---|
| A. Doing `[In Arbeit]`, mapping `In Arbeit = [In Arbeit, In Progress]` | `Neu, In Arbeit, In Progress, Erledigt` | **Doing** (correct) | valid |
| B. Doing `[In Arbeit]`, mapping `In Arbeit = [In Progress]` | `Neu, In Progress, Erledigt`: **German state missing** | **Unknown** | valid |
| C. Doing `[In Arbeit, In Arbeit]`, mapping `In Arbeit = [In Progress]` | `Neu, In Progress, Erledigt` | **Unknown** | valid |

Frontend, `reconcileDoingStates` run once per editor event, and the editor's source picker:

| Flow | Resulting Doing list | Source picker options for the mapping |
|---|---|---|
| Add mapping, type the name `In Arbeit` first, then pick a source state | `[In Arbeit]`, which is configuration **B** | `[In Progress]` only: **the German state is not offered** |
| Pick both source states first, then type the name | `[In Arbeit]`, which is configuration **A** | n/a |
| Type the name `Review DE/EN` while an unrelated Doing state `Review` exists | `[Testing, Review DE/EN]`: **raw `Review` deleted** | n/a |

**So the backend model can already handle a mapping that is named after one of its own states (A). The bug
is that the editor drives the user into B, which every layer accepts, and B makes the German state's items
disappear without any warning.**

## 2. User-visible symptoms

1. **S1. The German state cannot be added to its own mapping.** Once the mapping is named `In Arbeit`, the
   "Add source state" dropdown offers only `In Progress`. That is the order the form suggests, since the
   name field comes first.
2. **S2. The raw state disappears from the Doing list, and the change cannot be seen.** After `In Progress` is
   picked, the Doing list shows one chip, `In Arbeit`, exactly as before. But that chip now means the
   mapping (which holds only `In Progress`), not the raw German state.
3. **S3. Items in the German state vanish.** On the next update the connectors no longer ask the tracker
   for the German state (Jira JQL, ADO WIQL, Linear and CSV all filter on `AllStates`). Items already
   stored categorise as `Unknown`. WIP, cycle time, throughput and forecasts drop those items, and nothing
   warns the user.
4. **S4. No validation message appears.** Neither the frontend nor the backend validator flags the
   collision, although both contain a check that looks as if it should.
5. **S5 (same mechanism, wider reach).** Typing a mapping name one character at a time deletes any
   unrelated Doing state whose name is a prefix the user types through, for example `Review` while typing
   `Review DE/EN`.

## 3. Toyota 5 Whys

### Branch A: the stored lists cannot tell a raw state from a mapping name (core model)

- **WHY 1A.** Items in raw `In Arbeit` are `Unknown` and are not fetched.
  [Evidence: probe B. `MapStateToStateCategory` returns `Unknown` (`Models/WorkTrackingSystemOptionsOwner.cs:188-206`).
  `AllStates` lacks the state (`:33-35`). The connector queries filter on `AllStates`: Jira
  `JiraWorkTrackingConnector.cs:2705`, ADO `AzureDevOpsWorkTrackingConnector.cs:318,357`, Linear
  `LinearWorkTrackingConnector.cs:130,609`, CSV `CsvWorkTrackingConnector.cs:222`.]
- **WHY 2A.** The Doing entry `In Arbeit` expands to the mapping's members only, which here is just `In Progress`.
  [Evidence: `GetRawStatesForCategory` (`WorkTrackingSystemOptionsOwner.cs:208-228`). When an entry matches a
  mapping name, its members replace it and the entry itself is dropped (`:217-220`). It is kept as a raw
  state only when no mapping matches (`:221-224`).]
- **WHY 3A.** The same entry cannot be both the mapping and the raw state. Repeating it (probe C) does not
  help either, because every copy resolves to the mapping.
  [Evidence: `ToDoStates`, `DoingStates` and `DoneStates` are plain `List<string>` (`:27-31`). The mapping
  wins by name lookup (`:214-215`).]
- **WHY 4A.** The design put mapping names and raw tracker states into one string namespace, and resolves
  them by "the mapping wins". So the stored configuration cannot express "this entry is a raw state" once a
  mapping shares its name.
  [Evidence: the frontend writes mapping names into `doingStates` (`utils/stateMappingReconciliation.ts:52-64`,
  the "the group name is appended" doc comment at `:23-30`). `StateMapping` is just `Name` plus `States`
  (`Models/StateMapping.cs`). The case is valid only if the mapping contains its own name (probe A).]
- **WHY 5A. Root cause A.** The data model makes a mapping whose name equals a raw state the mapping
  does **not** contain (a "shadowing" mapping) silently take that state out of every category, and of every
  tracker query, with no invariant to stop it. The only safe case (name ∈ own states) depends on the user
  happening to build it that way.

### Branch B: the editor makes the safe shape unreachable and the unsafe one the default

- **WHY 1B.** The German state cannot be picked as a source (S1).
  [Evidence: probe. `getAvailableSourceOptions` (`components/Common/StateMappings/StateMappingsEditor.tsx:48-61`)
  drops every Doing entry equal to **any** mapping name, the mapping's own name included (`:49-51,59`).]
- **WHY 2B.** The exclusion assumes that a Doing entry equal to a mapping name is always the mapping (so it
  cannot be nested). While the mapping is still empty, though, that entry is the raw state.
  [Evidence: the doc comment at `:44-47` reads "Excludes mapping names (they cannot be nested)". The editor
  test `StateMappingsEditor.test.tsx:240-276` pins that an empty mapping `Closed` hides `Closed` from its
  own dropdown.]
- **WHY 3B.** Picking `In Progress` then deletes the raw German state from Doing (S2).
  [Evidence: probe NAME-FIRST gives `["In Arbeit"]`. In `reconcileDoingStates` (`stateMappingReconciliation.ts:41-49`)
  the "undo" step of a changed mapping removes every Doing entry equal to the previous name, even when the
  previous mapping had no states and was never applied, so that entry was the user's raw state. The "apply"
  step (`:52-64`) then sees the name "already present" and does not append it (`:59-61`).]
- **WHY 4B.** Reconciliation is stateless. It infers "this entry is my mapping" from the name alone, and it
  runs on every keystroke of the name field. Every intermediate prefix is treated as an added-then-removed
  mapping, which is S5.
  [Evidence: `handleNameChange` calls `onChange` per keystroke (`StateMappingsEditor.tsx:75-80,144`). The
  `onChange` handler reconciles on each call (`components/Common/Team/ModifyTeamSettings.tsx:246-254`,
  `components/Common/ProjectSettings/ModifyProjectSettings.tsx:254-262`). Probe PREFIX gives
  `["Testing","Review DE/EN"]`.]
- **WHY 5B. Root cause B.** The editor and the reconciliation never distinguish an **applied** mapping
  (its name stands for its members) from an **unapplied** one (its name is just text that may equal a raw
  state). Their tests pin the colliding inputs as the intended behaviour
  (`stateMappingReconciliation.test.ts:54-69` "does not duplicate mapping name when it is already in Doing
  states", where `Verification` is a pre-existing Doing entry and gets absorbed).

### Branch C: the validators were made unable to see the collision

- **WHY 1C.** No error is shown or returned (S4).
  [Evidence: probe V. `StateMappingValidator.Validate` returns valid for B.]
- **WHY 2C.** The collision check compares mapping names against a set from which all mapping names were
  removed first, so it can never match.
  [Evidence: backend `API/Helpers/StateMappingValidator.cs:66-79`, where `directStateSet` excludes
  `mappingNameSet` (`:72-74`) and the `collides with a direct state name` branch (`:76-79`) is unreachable.
  The frontend mirror `utils/stateMappingValidation.ts:49-68` filters the same way at `:56-59`.]
- **WHY 3C.** The filter was added deliberately, because the intended workflow puts the mapping name into
  Doing, and the original check flagged every correctly applied mapping.
  [Evidence: commit `3fa11b1ed` ("refactor: enhance state mapping validation logic…") replaced
  `new HashSet<string>(allDirectStates, …)` with the filtered set, in the backend and the frontend at once.]
- **WHY 4C.** Under branch A's single namespace, the validator receives the same strings for the intended
  case and the harmful one, so no check on names alone can tell them apart.
  [Evidence: `ValidateSettings` builds `allDirectStates` from the three category lists (`StateMappingValidator.cs:12-15`).
  Tests `Validate_MappingNameMatchesRawStateInDirectList_IsValid` (`StateMappingValidatorTest.cs:165-177`,
  mapping `Active = [In Work, Started]` while `Active` is listed) and the frontend twin
  (`stateMappingValidation.test.ts:70-77`) assert that the shadowing configuration is **valid**.]
- **WHY 5C. Root cause C.** Validation was relaxed to make the common case pass, rather than given
  information that separates the two cases. What remains is a no-op that reads like a guard, and two tests
  that state shadowing as intended.

### Completeness check (other places looked at, not contributing)

- **Raw state to mapping name** (`MapRawStateToMappedName`, `:240-246`). This works for name ∈ own states:
  both raw states map to `In Arbeit`, and `In Arbeit` expands back to both (probe A). Every connector and
  `WorkItemStateTransitionMapper.cs:13-14` uses it the same way.
- **Duplicate-key or `Distinct` crashes**: none. `AllStates` uses `Union`, which collapses the self-name
  harmlessly. The validator uses `GroupBy` and `HashSet`, not `ToDictionary`.
- **Wait states, cycle-time boundaries, refinement** (`TeamMetricsService.cs:495`,
  `BaseMetricsService.cs:384`, `RefinementList.cs:37`) all expand through `GetRawStatesForCategory`. They
  inherit branch A's behaviour and add no new failure.
- **Autosave**: frontend mapping errors do not block autosave (`ModifyTeamSettings.tsx:54-80` has no
  mapping blocker). Only the backend `BadRequest` paths block it (`TeamController.cs:208-211`,
  `TeamsController.cs:103`, `PortfolioController.cs:109-112`, `PortfoliosController.cs:94-97`). This
  matters for the fix.

### Alternative hypotheses considered and eliminated

| Hypothesis | Why it was eliminated |
|---|---|
| The DTO-to-model sync drops or reorders a mapping state | `TeamExtensions.cs:89-91` and `PortfolioExtensions.cs:66-68` copy `Name` and `States` one to one. Probe A, with the same strings through the model, works. |
| A connector drops the German state independently of the mapping | The connectors filter on `AllStates`. Probe A contains the German state there and probe B does not, so the mapping alone decides. |
| Mapping lookup is case- or whitespace-sensitive | Every comparison is `OrdinalIgnoreCase` (`WorkTrackingSystemOptionsOwner.cs:215,243`) or trimmed and lowercased in the frontend. The repro uses identical strings. |
| Persistence or a duplicate key throws | JSON column, no unique index, no `ToDictionary` on state names (see out of scope). Nothing was thrown in the probes. |
| The user wanted the mapping to *replace* the German state | The report states the intent: pack both states under the existing label. |

## 4. Backwards-chain validation

- **A to symptoms.** If an entry named like a mapping always resolves to the mapping, then a mapping that
  lacks its own name removes the raw state from `AllStates` (S3, not fetched) and from categorisation
  (S3, Unknown). Confirmed by probes B and C.
- **B to symptoms.** If the picker hides the mapping's own name, and the undo step deletes a raw entry equal
  to an unapplied mapping's name, then the name-first flow yields configuration B (S1, S2, S3), and typing
  through a prefix deletes unrelated states (S5). Confirmed by the NAME-FIRST, PREFIX and EDITOR probes. The
  states-first flow yields A, which explains why the bug depends on the order the user works in.
- **C to symptoms.** If the collision check is unreachable, B is accepted everywhere (S4). Confirmed by
  probe V.
- **Cross-validation.** The three branches are consistent. A is why B's output is harmful and why C cannot
  be fixed on names alone. All of S1 to S5 are explained.

## 5. Contributing factors

1. The name field comes first in each mapping card (`StateMappingsEditor.tsx:141-147`), so the order that
   breaks things is the natural one.
2. After the change the Doing chip looks the same (same text, no "mapping" marker), so S2 cannot be seen.
3. An unmapped or unknown state vanishes without a warning anywhere (a known gotcha). There is no
   "configured state matched no items" or "items in unconfigured states" signal.
4. The tests pin the faulty behaviour: `StateMappingValidatorTest.cs:165-177`,
   `stateMappingValidation.test.ts:70-77`, `stateMappingReconciliation.test.ts:54-69`,
   `StateMappingsEditor.test.tsx:240-276` (the `Closed` half).
5. Reconciliation runs on every keystroke rather than when the name is committed.
6. Mapping errors in the frontend do not block autosave, so a frontend-only rejection would still persist
   configuration B.

## 6. Proposed fix

### Which is smaller or safer?

**Recommendation: "just make it work" for the case where the mapping contains its own name, plus a clear
configuration-time rejection for the one shape that cannot work (a mapping named after a raw state it does
not contain).** Both live in the frontend, where it is still known whether an entry is a raw state.

- "Make it work" is the smaller fix, because the backend already supports the case (probe A). There is no
  model, migration, DTO or connector change, only the picker filter and a guard in the reconciliation.
- "Reject only" is not smaller. Rejecting needs the same applied/unapplied detection, the backend cannot
  make the decision on its own (WHY 4C), and it would forbid the case the report calls common
  (translations, renamed workflows).
- The backend should not try to enforce the name rule. The persisted strings are ambiguous (branch A), so
  a backend rule either stays a no-op or rejects valid, already-saved configurations.

### Changes

**F1. Make the case reachable** (root cause B), in `components/Common/StateMappings/StateMappingsEditor.tsx:48-61`.
Build the excluded-names set from the **other** mappings only:

```ts
const otherMappingNames = new Set(
  stateMappings
    .filter((_, i) => i !== mappingIndex)
    .map((m) => m.name.trim().toLowerCase()),
);
// filter: !otherMappingNames.has(lower) && !currentMappingStates.has(lower)
```

After this, the German `In Arbeit` is offered as a source of the mapping named `In Arbeit`. It also gives
users who already have configuration B a way to repair it: they open the mapping and add the German state.

**F2. Stop reconciliation from treating an unapplied mapping's name as its own** (root cause B, and S5),
in `utils/stateMappingReconciliation.ts`.

- Add `isApplied(m, doing)`. It holds when `m` has a non-empty name, has at least one state, and no member
  **other than its own name** is still in `doing`.
- Add `wouldShadow(m, doing)`. It holds when `m`'s name is in `doing` and the name is **not** one of `m`'s
  states.
- In the undo loop (`:41-49`), only undo a `prev` mapping that `isApplied(prev, doingStates)`. This stops
  the deletion of a raw entry for empty or skipped mappings and intermediate keystrokes.
- In the apply loop (`:52-64`), skip any `next` mapping that has **no states**, or that
  `wouldShadow(next, result)`. The Doing list is left as it is, the raw state survives, and F3 reports the
  problem. The no-states guard matters: today the apply step appends an empty mapping's name, so typing a
  name drops a chip per keystroke (`I`, `In`, …) that the next keystroke's undo cleans up. With the undo now
  limited to applied mappings, those chips would accumulate unless the apply step also leaves empty mappings
  alone, so both loops use the same precondition. (Found in peer review.)
- With this rule, an empty mapping never writes into Doing, so deleting one needs no undo either. Today
  deleting an empty mapping removes the chip it appended; under F2 there is no chip to remove.

Tracing the flows again with F1 and F2:

- Name first, then `In Progress`: the mapping is skipped and Doing stays `[In Arbeit, In Progress]`.
- Then add `In Arbeit`: the mapping is applied and Doing becomes `[In Arbeit]`, which is configuration A.
- Typing through `Review`: `Review` survives.

F1 also lets a user build a mapping that holds only its own name (`X = [X]`). That is harmless (it behaves
exactly like the raw state `X`) but pointless; leave it allowed rather than add a rule for it.

**F3. Reject the unworkable shape clearly, and block saving it** (root cause C), in
`utils/stateMappingValidation.ts:49-68`. Replace the dead check with one built on the same predicate: a
mapping that has states, whose name is in the Doing list, and that has a member other than its name still in
the Doing list (that is, it was skipped by `wouldShadow`). The message names the fix:
`Mapping "In Arbeit" has the same name as the Doing state "In Arbeit". Add "In Arbeit" to this mapping to group it, or choose another name.`
Then add the mapping errors to `teamAutoSaveBlockers` (`ModifyTeamSettings.tsx:54`) and to the portfolio
equivalent in `ModifyProjectSettings.tsx`. Otherwise the skipped mapping is still saved, and the backend
resolves the Doing entry `In Arbeit` to the mapping, which is configuration C, and the state is lost again.

**F4. Remove the false guard from the backend** (root cause C), in `API/Helpers/StateMappingValidator.cs:66-79`.
Delete the unreachable collision branch, or leave a narrow and decidable check if the maintainer wants
defence in depth. Rename `Validate_MappingNameMatchesRawStateInDirectList_IsValid` so it states what it
proves: the backend cannot see this collision, and it is enforced in the editor. This is optional, because
it changes no behaviour, but the dead branch is what made the issue look covered.

**Customer workaround until the fix ships.** In the mapping, pick both source states **before** typing the
name, or rename the mapping temporarily, add the German state, then rename it back. Either way the result is
configuration A, which works.

### Prevention and early detection (systemic, follow-up)

- **P1.** A warning when the configured states (after expansion) differ from the states seen on fetched
  items, and when items land in `Unknown`. This addresses contributing factor 3, which is broader than
  this bug.
- **P2.** Longer term: store category entries typed (`{kind: raw|mapping, value}`) or keep mapping names out
  of the category lists. That removes the namespace collision itself (root cause A). It is a model, DTO,
  connector and migration change, so it is not proposed for this bug.

## 7. Files affected

| File | Change |
|---|---|
| `Lighthouse.Frontend/src/components/Common/StateMappings/StateMappingsEditor.tsx` | F1 (picker filter) |
| `Lighthouse.Frontend/src/utils/stateMappingReconciliation.ts` | F2 (`isApplied` and `wouldShadow` guards) |
| `Lighthouse.Frontend/src/utils/stateMappingValidation.ts` | F3 (replace the dead check) |
| `Lighthouse.Frontend/src/components/Common/Team/ModifyTeamSettings.tsx` | F3 (autosave blocker) |
| `Lighthouse.Frontend/src/components/Common/ProjectSettings/ModifyProjectSettings.tsx` | F3 (autosave blocker) |
| `Lighthouse.Backend/Lighthouse.Backend/API/Helpers/StateMappingValidator.cs` | F4 (optional, behaviour-free) |
| Tests listed in section 8 | update and add |

There are no changes to the model, the DTOs, the connectors or the migrations.

## 8. Risk assessment and tests

**Risks**

- **R1 (medium).** F2 changes when the undo step runs. `isApplied` does not look at whether the name chip is
  in Doing, so a mapping whose name chip the user removed by hand still restores its members when it is
  deleted, as it does today. What changes: if the user re-added one of a mapping's members to Doing by hand,
  the mapping now reads as not applied, and deleting it leaves its name chip in Doing instead of removing
  it. The chip stays visible and can be removed by hand, so this is acceptable, but it needs a test.
- **R2 (low).** F3 blocks autosave in a new situation. It fires only on a shape that already loses data
  today. Mapping errors already show in the editor, so the copy needs the sketch/approval step (it is UI
  text).
- **R3 (low).** Existing configuration-B data cannot be detected after the fact (branch A), so it is not
  flagged automatically. F1 makes it repairable. The release note or support reply should mention the
  repair step.
- **R4 (low).** Portfolios share the same components (`ModifyProjectSettings.tsx:188-262`), so the fix and
  its tests cover both owners.
- **No risk to the backend sync path.** It is untouched (unless F4 is taken, which is behaviour-free).

**Existing coverage**

- Backend model: `Lighthouse.Backend.Tests/Models/TeamTest.cs`, `PortfolioTest.cs`,
  `WorkTrackingSystemOptionsOwnerTest.cs`.
- Backend validator: `API/Helpers/StateMappingValidatorTest.cs`, and `StateMappingSyncTest.cs` for the
  DTO-to-model sync.
- Frontend: `utils/stateMappingReconciliation.test.ts`, `utils/stateMappingValidation.test.ts`,
  `components/Common/StateMappings/StateMappingsEditor.test.tsx`, `ModifyProjectSettings.test.tsx`,
  `pages/Portfolios/Edit/EditPortfolio.test.tsx`.

**Regression tests to add or change**

1. `stateMappingReconciliation.test.ts`:
   - Name first, then `[In Progress]`: Doing stays `[In Arbeit, In Progress]`. Then `[In Progress, In Arbeit]`: Doing becomes `[In Arbeit]`.
   - Typing `Review DE/EN` keystroke by keystroke into a mapping with no states: assert the **exact** Doing
     array after every keystroke (unchanged, with no prefix chips). Then with states `[Prüfung]`: raw `Review`
     survives and Doing ends `[Review, Testing, Review DE/EN]`.
   - Adding and then deleting an empty mapping leaves Doing exactly as it was.
   - Rewrite `:54-69` ("Verification" pre-existing): it now expects the mapping to be skipped.
   - R1: deleting a mapping whose members were re-added by hand.
2. `StateMappingsEditor.test.tsx`:
   - A mapping named `In Arbeit` offers `In Arbeit` as its own source.
   - Update `:240-276` so the empty `Closed` mapping offers `Closed` to itself but `Active` stays hidden.
3. `stateMappingValidation.test.ts`:
   - The new message for the skipped shadowing mapping.
   - No error for a self-named applied mapping.
   - Replace `:70-77`.
4. `WorkTrackingSystemOptionsOwnerTest.cs`: pin probe A (self-named mapping: both raw states in `AllStates`
   and `Doing`), so the case the fix relies on cannot regress.
5. Optional: an `ModifyTeamSettings` test that autosave is blocked while the mapping error is present.
