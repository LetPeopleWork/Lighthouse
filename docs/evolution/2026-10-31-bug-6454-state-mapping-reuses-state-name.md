# Bug #6454 — A state mapping that reuses the name of an existing state confuses Lighthouse

**Pushed to main 2026-10-31, not yet released** · ADO Bug #6454 · workspace
`docs/feature/bug-6454-state-mapping-reuses-state-name/` (RCA, roadmap, execution log, mutation record) · commits
`0294870a3..9dfb938c6` · the remaining model limitation and a pre-existing autosave error are Bug #6458

## What users get

A workflow with the same state in two languages (`In Arbeit` and `In Progress`) needs one state mapping that
groups both under one name, and the natural name is one of the two. Before the fix that mapping could not be
built, and a mapping named after a state it did not group silently made that state stop counting. Now:

- **A mapping can group the state it is named after.** The source picker offers a mapping its own name, and
  typing the name no longer removes an unrelated Doing state.
- **A mapping named after a state it does not group is reported**, and autosave pauses until it is fixed. This
  covers a To Do, Doing or Done state and a state another mapping groups, for Teams and Portfolios alike.
- **The Doing list stays right while mappings are edited**: deleting, renaming or re-pointing a mapping in the
  orders the reviews found no longer drops a raw state or leaves a stale chip.

## How it happened

1. **To Do, Doing and Done hold raw states and mapping names as the same plain strings.** The backend expands
   any entry equal to a mapping name into the mapping's states, so the mapping wins over a raw state of the
   same name.
2. **The frontend keeps Doing in step with the mappings by guessing from those strings** whether a mapping is
   in effect: applying a mapping swaps its states for its name, undoing it swaps back.
3. **Two guards were wrong.** The picker hid a mapping's own name from itself, and reconciliation applied and
   undid mappings it should not have. The backend's collision check could never fire, because it filtered
   mapping names out of the very set it compared against.

## The fix

- The picker hides only the other mappings' names.
- Reconciliation undoes only a mapping that is in effect. It never applies a mapping that:
  - has no states;
  - is named after a Doing state it does not group;
  - is named after a state another mapping groups;
  - shares a source state with another mapping.
- Validation reports the name clashes with copy that says how to fix them, and the autosave blockers pause on
  them. The unreachable backend check is gone.
- Each rule is defined once, in `stateMappingReconciliation.ts`, and shared by reconciliation, validation and
  the picker.

## Decisions

- Fixed in the frontend rather than by changing the model. Three review rounds each found a new edit sequence
  into the same ambiguity. The maintainer chose one last guard, then shipped. Further paths of that kind go on
  Bug #6458 (typed category entries: each entry says whether it is a raw state or a mapping).
- Accepted limitation: removing a mapping's other states from Doing by hand while a clash shows, or hand-editing
  Doing so two mappings share a state, can still lose a raw state or leave a stale chip. Both need #6458.
- Out of scope, also on #6458: a freshly added, still empty mapping makes autosave fail with a bad request.

## Quality gates

- Rigor `standard` (Sonnet crafter, Haiku reviewer), 6 DES steps, integrity verified.
- Mutation:
  - Backend `StateMappingValidator.cs`: 100 % (24 mutants).
  - Frontend, five files: 97.25 % (255 mutants), with 7 survivors accepted with reasons. See
    `mutation/results.md`.
- CI found one Sonar issue, typescript:S7778 (two `push()` calls in a row). It is fixed, and a new GritQL Biome
  rule, `merge-consecutive-push.grit`, now fails `pnpm build` on it locally.

## Lessons

- **A Haiku review that marks an edge "Safe" is not proof.** Each round traced one more sequence of edits; the
  class only ends when entries are typed.
- **The test suites competed for memory with a parallel session.** Claude Code twice killed the backend suite
  for low memory, so run one stack at a time on a shared machine.
