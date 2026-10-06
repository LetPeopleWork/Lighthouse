# Slice 08 — Every write confirms in one line

**Story** #6218 / US-08 · **Job** `job-read-lighthouse-answers-in-the-terminal` · **Repo**
`lighthouse-clients` · **Estimate** ~3h · **Sketch** `discuss/cli-sketches.md` §6

## Goal

Every create, update, delete and refresh confirms in one line shaped like the approved vote lines, and
a refresh stops claiming it is done.

## IN scope

- `Created: {Team|Portfolio} {name} [id: n].`, `Updated: …`, `Deleted: {Term} [id: n].`,
  `Refresh queued: {Term} [id: n]. Lighthouse updates it in the background.` (D11, D12).
- Blackout create/update confirmed with Lighthouse's own `summary` and the description; delete by id.
- Entity words from Terminology.

## OUT of scope

- Naming a deleted entity (D12). Changing `--json`/`--toon` text for delete/refresh — DESIGN decides
  how constraint 1 holds for those hand-written lines (feature-delta Handoff 4).

## Learning hypothesis

**Disproves that a write's answer carries what its confirmation names** — if create/update do not
return the created entity's name and id, or blackout create returns no `summary`, the confirmation falls
back to `{Term} [id: n]` or less, and the sketch's lines change.

## Acceptance criteria

AC-08.1 … AC-08.4. Risk carrier: **AC-08.4**, a real create/update/delete round trip on the demo
instance.

## Dependencies

Slice 01.

## Reference class

Story #6156 — the vote, comment and take-back lines (`refinementVoteWording.ts`).

## Pre-slice SPIKE

None.
