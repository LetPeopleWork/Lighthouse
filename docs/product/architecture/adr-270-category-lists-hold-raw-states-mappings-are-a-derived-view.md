# ADR-270: Category lists hold raw states; a state mapping is a derived view

**Status**: Accepted (2026-11-01, DESIGN of Bug 6458 part (a), propose mode, maintainer sign-off)
**Date**: 2026-11-01
**Feature**: `docs/feature/bug-6458-typed-category-entries/`

## Context

A Team's or Portfolio's To Do, Doing and Done lists are plain strings. Once a state mapping is "taken into"
Doing, the editor replaces the mapping's member states with the mapping name, so the stored list holds a
collapsed view, not the facts. The backend cannot uncollapse it without guessing: an entry equal to a mapping
name is always read as "the mapping". Two hand-edit sequences that the Bug 6454 guards cannot see therefore
still lose a raw tracker state (a mapping that shadows a state of the same name) or leave a ghost chip (a
mapping deleted after two mappings came to share a source state).

Every guard written so far lives in the frontend and reconstructs the facts from the view. Each new edit
sequence finds a view that two different fact sets produce. The backend validator checks names, emptiness and
shared source states only, because it receives the same collapsed strings and cannot decide the rest.

A related latent defect shares the cause. Stored state transitions carry the mapped name, and the as-of
projections (Team and Portfolio) categorise that name against the expanded lists, which hold members only.
A mapping whose name is not one of its own members therefore lands under `Unknown`.

## Decision

- **The three category lists hold raw tracker states only.** A mapping name never appears in them. A mapping
  (`StateMapping { Name, States }`, unchanged) is a display grouping computed on read, never stored in the lists.
  The single source of truth for what a mapping stands for is `StateMapping.States`.
- **The owner computes the view.** `DisplayStatesFor(category)` replaces each raw state by its mapped name and
  keeps the first occurrence of each name, so a group sits at its first member's index. The DTO gains read-only
  `toDoDisplayStates`, `doingDisplayStates` and `doneDisplayStates`; the metrics pages and the editor read
  these instead of re-deriving the view.
- **Two category lookups, one per namespace.** `CategoryOfRawState` serves connectors; `CategoryOfDisplayName`
  serves stored (already mapped) transitions and settles the as-of `Unknown`. A name whose members span
  categories is `Unknown`.
- **Expansion stays only for mapped-name references**: wait states, cycle-time boundaries and refinement
  states. `AllStates`, `OpenStates` and the three connector sites that expanded the lists read them as they are.
- **Write path.** Mappings are synced before the lists. Two pre-checks answer 400 with a message that names the
  fix: an entry that equals a mapping's name but is not one of its members (shadowing), and a mapping member
  that equals another mapping's name (chaining). Remaining legacy payloads that name a mapping are normalised
  to raw states, in order, with the members emitted after the entry that named them.
- **Validation** judges facts: a new or changed mapping's listed members share one category; a mapping name
  equals no listed raw state of another category; the existing name, emptiness, duplicate-name and
  shared-source-state rules stay. Legacy stored rows that break the category rule keep saving; only mappings
  changed in the save are judged.
- **Data rewrite** is an idempotent `ISeeder` (precedent: `ApiKeyOwnerReconciliationSeeder`), provider-neutral
  because it goes through the repositories. It applies the write-path normalisation to stored lists, writes
  members at the old chip's index, expands stored shadowing rows with the legacy rule (today's meaning, logged
  per owner), and expands stored chained mappings one level with a warning log. No column is added, dropped or
  renamed.
- **Frontend.** Reconciliation and the doing-clash rules are deleted. The Doing list renders the display list
  with mapping chips marked as such. Removing a mapping chip is refused with copy telling the user to delete
  the mapping instead. The editor mirrors the backend rules against shared fixtures, and name and state
  comparison is folded the same way on both sides.

## Alternatives considered

| Alternative | Why rejected |
|---|---|
| (i) Typed entry list (`{kind, value}` per entry, three new columns per table) | Removes the string ambiguity but keeps two sources of truth, so reconciliation survives as "dangling typed entry" when a mapping is deleted. Needs new columns on both providers, dual-write for a release, and all ~25 expansion sites change shape. Heaviest migration |
| (ii-a) A category or "in effect" flag on the mapping | Keeps the lists as a derived view and adds a second thing the editor must keep in step with them. Changes the mapping JSON and needs a position field for the group |
| Another frontend-only guard | Excluded by the maintainer. The post-edit input of the first failing sequence is byte-identical to a correctly applied mapping, so no function of `(mappings, doing)` can separate them |
| EF migration with C# for the data rewrite | Per-provider (`text[]` versus JSON text), harder to test, no advantage over an idempotent seeder |
| Accept shadowing payloads and expand them as legacy | Keeps the silent data loss that Bug 6454 found; only API clients can send the shape |

## Consequences

- **Positive:** the backend can validate what it could not before; reconciliation code (the source of 6454 and
  both 6458 paths) is deleted; no schema change; the as-of `Unknown` is fixed at its root; display ordering
  has one implementation.
- **Public API v1 break, documented.** `doingStates` (and the To Do and Done lists) in `v1` GET now hold raw
  states instead of "may contain mapping names". The field types are unchanged. A script that read
  `doingStates` as display columns should read `doingDisplayStates`. A shadowing or chained payload that was
  accepted before (and silently lost a state) is now a 400. Write payloads that still name a mapping are
  normalised, so their meaning is unchanged. Lighthouse-Clients passes settings through as opaque JSON and
  needs no code change, but its docs and skills must state the new meaning and the 400.
- **Rollback.** An older backend is safe on normalised lists: its expansion yields the same set. An older
  **frontend** re-collapses members into the mapping name on the next mapping edit, so a rollback needs a
  test and a release-note line.
- **Side effects:** the stored lists change on first start (one full fetch cycle, because the fetch
  fingerprint covers them); a stored shadowing row loses its raw state for good (as it already had) and the
  release note says to re-add it; a group's position follows its first member.
- **Enforcement:** a property test (the expansion of the normalised list equals the expansion of the original,
  and normalising twice changes nothing), shared backend/frontend validation fixtures, and an architecture
  test that no code outside the owner and the three mapped-name validators calls the expansion method.
- External integrations: none touched; no contract-test recommendation applies.
