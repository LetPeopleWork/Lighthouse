# Slice 04b — The published key can never be the active key

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5789 · **Estimate**: ~2h
**Origin**: not from DISCUSS. Found by the manual verification walkthrough of 2026-08-16
(`verification/manual-walkthrough.md`, findings F-20 and F-21).
**Ordering**: before slice 05. The chart hands cluster operators configuration custody, which is the
same path by which the published key can be supplied.

## Goal

An instance can never write a secret with the key that ships inside every copy of Lighthouse, and can
never be told it is safe when it is not.

## The defect

Every check in the codebase reasons about key **identity** or envelope **shape**. Nothing compares key
**material**. So the published key, supplied through configuration and wearing a `k-cfg-` id derived
from its own bytes, walks past the one check built to catch it.

Reached by the most ordinary upgrade mistake there is: keeping your own `appsettings.json`, which still
carries the pre-epic `EncryptionSettings` key — the literal published value. The instance is pinned to
the public key, cannot mint, has no Rotate button, is warned that a credential is exposed, offers the
move, and after the move reports itself healthy. Worse than never warning: the one prompt that would
have made the operator look again is spent.

The same gap fires in reverse for a genuine custom-key install, whose pre-epic values carry no envelope
prefix either and are therefore reported as readable with a key that has never been able to read them.

## IN scope

- Compare supplied key material against the compiled-in published key at bootstrap; refuse it as an
  **active** key, naming what happened and what to do.
- It must stay acceptable as a **retired** entry, or every upgrade stops being readable.
- Stop the published-key count deciding by envelope prefix alone, so it stops lying in both directions.

## OUT of scope

- The panel's wording and which actions it offers (slice 06a).
- Anything about recovering an instance that already cannot read its secrets (slice 05b).

## Acceptance criteria

- A key supplied through any configured name whose material equals the published key is refused as
  active, and the refusal says which setting carried it.
- The same material remains readable as a retired entry: an upgraded instance still reads what it
  stored.
- The published-key count reports zero for an install whose secrets are on a custom key, and non-zero
  for one whose secrets are genuinely on the published key — regardless of what id that key wears.

## Dependencies

Slices 01–04. No new data, no migration: the check pass already establishes which key reads each
stored value.

## Verdict

_To be recorded at slice close._
