# Slice 06a — The panel offers only what would help

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5791 · **Estimate**: ~5h
**Origin**: not from DISCUSS. Everything the manual verification walkthrough of 2026-08-16 found about
the encryption panel (`verification/manual-walkthrough.md`, findings F-2, F-4, F-5, F-7, F-9, F-10,
F-11, F-12, F-14, F-15, F-18; maintainer decisions V2 and V3).

## Goal

An administrator opening the encryption panel understands what it is about, is offered only actions
that would change something, and reads numbers that mean something.

## Why behaviour and words are one slice

Which actions exist and what the sentences say are the same decision made twice. Suppressing the move
where it cannot help (V3) is also a rewrite of the warning that recommends it; hiding unreferenced keys
(V2) is also what stops the ring table needing an explanation. Splitting them means writing every
sentence twice and reconciling them later.

## IN scope — behaviour

- **Hide keys nothing references.** They stay in the ring, so old backups stay restorable; they leave
  the table, so a rotated instance stops showing chips that never encrypted anything. This also removes
  `k-legacy-default` from a first install for free.
- **Do not offer a move that cannot achieve anything** — with nothing to move, and above all where the
  active key *is* the published key. There, the fix is the custody sentence already above it, not a
  button that re-encrypts the published key onto itself and leaves the warning untouched.
- **One action, in the button row.** The alert names it rather than carrying its own copy of it, and
  the primary style goes to whatever this instance actually needs — not to Rotate by default.
- **`Kept in` stops naming the key-store directory under configuration custody**, where the key is not
  kept and never will be. That directory exists and is full of key-shaped files, so the current row
  invites an operator to back it up believing they have taken their key with them.

## IN scope — words

- **Drop every zero from the reports.** Four categories of nothing compete with the one number that
  matters. Say only the non-zero ones.
- **Agree in number.** `1 stored secrets` appears in both summaries and in the warning banner.
- **Rotation says what happened** — a new key was minted — rather than `Moved 0 stored secrets`.
- **The published-key warning states the situation then the action.** Why the published key is bad
  belongs behind a docs link.
- **A header saying what the panel is for**, plus that docs link. Read cold, the table opens on *Key
  source* with nothing establishing that this is about credentials stored in Connections.
- **A followable rotation instruction** for operator-owned custody: the right setting name, the fact
  that the singular and plural forms coexist and which wins, and the ring grammar — comma-separated
  entries, each bare base64 or `name:base64`, first entry active. The same sentence reaches every
  Kubernetes operator once slice 05 lands.
- **A shorter startup custody line.** Take the length out of the custody phrase, not the key id: the id
  is the most useful thing there when diagnosing a refusal later.

## OUT of scope

- Removing keys from the ring — hiding only. Explicit removal is a later action, and depends on 05b.
- The material comparison behind the published-key count (slice 04b).
- Docs pages and the Docker install (slice 06 / #5781).

## Dependencies

Slice 04b, so the count the warning is built on is telling the truth before its wording is settled.

## Verdict

_To be recorded at slice close._
