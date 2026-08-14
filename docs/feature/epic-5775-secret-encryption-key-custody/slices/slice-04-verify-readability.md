# Slice 04 — Check before you rotate, and prove it after

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5779 · **Story**: US-04 · **Estimate**: ~4h
**Reference class**: slice 03's rotation walk, minus the write. The traversal, the per-Connection
attribution and the report vocabulary are shared on purpose — an operator should read the same words
before and after.

## Goal

An administrator can find out, without writing anything, whether every stored secret is readable and
which key each one is on.

## IN scope

- A read-only pass over every stored secret that writes nothing at all.
- Per secret: the owning Connection, the field, and the key id it is encrypted under.
- Four distinct reported states, never collapsed into "broken": on the active key, on a retired key,
  legacy plaintext, unreadable.
- An unreadable secret reported with the Connection and field that own it, so a human can go fix that
  one thing.
- Available before any rotation has ever run, including on an instance still on the published default —
  this is the surface that answers "what am I actually sitting on?" for an operator who has just
  upgraded.
- Completes within the request timeout on a Tenant-Zero-sized secret set, or streams progress.
- System Admin only.
- Driving surface: Settings → Encryption → **Check secrets**, and the matching admin API action.

## OUT of scope

- Repairing anything. The check reports; the operator decides.
- Rotation itself (slice 03).
- Scheduled or background checking.

## Learning hypothesis

**Disproves** "the rotation report is enough" if an operator reading it cannot map an unreadable secret
back to something they can act on — a Connection they can open and a field they can retype. If the
report already does that and this slice adds nothing but a read-only flag, **merge it into slice 03 and
say so here**; that is the flagged taste test, and this is where its verdict is recorded.
**Confirms**, if it holds, that checking before rotating is the thing that makes rotation a routine
action rather than a nerve-wracking one.

## Acceptance criteria

AC-4.1 through AC-4.8 in `feature-delta.md`. The two that carry the slice:

- **AC-4.4** — an unreadable secret is reported with the Connection and field that own it.
- **AC-4.5** — the check works before any rotation has run, on an instance still on the default key.

## Dependencies

- Slice 01 (attributable secrets, failing reads) and slice 03 (the traversal, and a ring with a retired
  key so AC-4.6 has something to distinguish).

## Dogfood moment

Same day: run the check on the restored `:5169` instance before rotating, deliberately corrupt one
stored secret, confirm the check names its Connection and field, then rotate and confirm the corrupted
one is skipped and reported while the rest land on the active key.

## Pre-slice SPIKE

None.

## Verdict

_To be recorded at slice close: confirmed / disproved / merged into slice 03._
