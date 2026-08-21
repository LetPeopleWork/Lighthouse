# Slice 03 — Correct your own note

**Epic** #5698 · **Story** US-03 · **ADO** #5639 · **Estimate** ≤1 day

## Goal
The author of a note can fix it or withdraw it; nobody else can.

## IN scope
- `PUT` and `DELETE /api/latest/deliveries/{deliveryId}/notes/{noteId}`.
- Author-only enforcement in the API, refusing another user's note with 403 independently of the UI.
- Edit and Delete affordances rendered only on the current user's own notes.
- An edited marker carrying the edit date, alongside the unchanged creation date.
- Auth-off behaviour: no author means no author restriction — any user with write access to the
  Portfolio may edit or delete, and the affordances are shown to them.

## OUT of scope
- An edit history or a diff of previous versions. The marker says edited; it does not say what changed.
- Restoring a deleted note.
- Any archive interaction.

## Learning hypothesis
**Disproves if it fails**: that "author owns the note" can be enforced without a new permission
concept. If the author check cannot be expressed alongside the existing `RbacGuard` attribute and
needs its own authorization handler, then the same will be true of every future per-row ownership
feature (Delivery owner, system notes) — worth knowing before either is designed.
**Confirms if it succeeds**: per-row ownership is a controller-level check over an existing scope
guard, not a new RBAC requirement.

## Acceptance criteria
AC-03.1 … AC-03.6 in `../feature-delta.md`.

## Dependencies
Slice 02.

## Reference class
`RbacGuard` attribute usage in `DeliveriesController`; the API-key ownership checks, which are the
nearest existing per-row rule.

## Dogfood moment
Fix a typo in the note written on the Slice 02 dogfood run.
