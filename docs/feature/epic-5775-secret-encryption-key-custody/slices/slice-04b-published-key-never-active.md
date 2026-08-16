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

**Shipped 2026-08-16.** Three changes, all behind one idea: stop reasoning about a key's name and start
asking about its material.

- `LegacyDefaultEncryptionKey` learned two questions — *is this key that key* (constant-time over the
  compiled-in bytes) and *can that key read this value*. Both hand back a boolean; the material still
  leaves the class nowhere.
- The refusal went into `SuppliedKeyRing.ParsedFrom`, the one parser every transport funnels through, so
  configuration under any of its three names, a mounted key file and a key store this instance wrote are
  all refused on the same terms and in the same words — and a fifth transport added later inherits it.
  It is scoped to the first entry of a ring. Behind an active key the same material stays welcome, which
  scenario 112 and the pre-existing upgrade tests pin.
- The count kept its SQL narrowing predicate — that is what keeps the settings page free for an instance
  that has already moved everything — and replaced the guess it made on the narrowed set with a read.

Two things deliberately left: an instance that already wrote secrets under published material wearing a
`k-cfg-` id will now refuse to start, and the way back in is slice 05b's subject. No released build can
be in that state — the path into it was opened and closed inside this epic. And the panel's wording,
which still says "the key published with Lighthouse" in a sentence slice 06a owns.

Owed before the epic closes: the **B2b** and **C1** walkthrough runs repeated on the same binary
substrate — B2b must refuse to start naming `EncryptionSettings__EncryptionKey`, C1 must start and
report zero.
