# Slice 10 — The Access settings show every Initiative grant, even for Initiatives gone from the data

**Feature**: epic-5806-initiatives · **Epic**: #6119 "Initiative access and on-track status" ·
**Story**: US-10 · **ADO**: #6123 · **Estimate**: ~4h (≤1 day) ·
**Order**: 9 of 10

## Goal

A System Admin sees, in Settings → Access, every Initiative grant by name, and a grant whose Initiative
has left the tracker's data is kept, marked and removable, and works again if the Initiative returns.

## IN scope

- Users and Group Mappings list Initiative grants by Initiative name and tracker type.
- A grant whose Initiative no Feature names any more reads "not in current data", shows the reader
  nothing, and can be removed (D25).
- The grant, and the target date if slice 05 has shipped, apply again when the Initiative returns under
  the same tracker reference.

## OUT of scope

- Automatic expiry or clean-up of dormant grants.
- Any change to how parent rows are stored or cleaned up.

## Learning hypothesis

**Disproves, if it fails**: that keying grants by the tracker reference survives real re-parenting. If a
dev-instance Initiative comes back under a different reference after a tracker move, the key is wrong
and DESIGN must revisit it (see the reference-id risk in the feature delta, S2).

## Acceptance criteria

AC-10.1 to AC-10.4 in `feature-delta.md`.

## Dependencies

Slice 07 (08 for the Group Mappings half). D25 is settled.

## Effort

~4h.

## Dogfood / production data note

In the demo, remove the Parent value from every Feature of "Deep Space Readiness" in the Orion and
Altobelli CSVs locally, refresh, and check the grant's state; restore the files, refresh, and check the
reader sees the Initiative again.
