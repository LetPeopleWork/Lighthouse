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

## Decisions taken before DISTILL (maintainer, 2026-08-16)

Four calls that are taste rather than derivation, so they were asked rather than guessed.

**The startup custody line drops both the phrase and the key id.** One word for custody, then the path:

```
🔑  Encryption    : instance · /storage/…/b2/keys
🔑  Encryption    : configured · /app/keys
🔑  Encryption    : mounted secret · /app/keys
🔑  Encryption    : published key · /app/keys
```

This reverses F-17's recommendation to keep the id on that line. It is safe to reverse now: slice 05b
made the refusal itself name both the key the instance started on and the key the stored credentials
were written under, so the id is available at the moment it is actually needed rather than on every
healthy start.

**The published-key warning states the situation, then the action, then what it does not cost.** The
reason the published key is bad goes behind the docs link:

> 1 stored credential is still encrypted with the key published with Lighthouse.
>
> Move it onto this instance's own key — nothing has to be re-entered. Why this matters ↗

**The panel header is one line and lets the docs teach.**

> How the credentials stored in your Connections are encrypted at rest. Read more ↗

**The primary button is whatever this instance's open problem is** — never Rotate by default:

| State | Button row |
|---|---|
| Secrets still on the published key | **Move stored secrets** · Rotate key · Check secrets |
| Healthy, own key, nothing to move | Rotate key · Check secrets — none emphasised |
| Operator-owned custody, cannot mint | **Move stored secrets** · Check secrets — no Rotate |
| The active key *is* the published key | Check secrets — no Move, because there is nothing to move onto |

## IN scope — the page the panel links to

**A `docs/settings/encryption.md` page**, following the pattern every other settings tab already has:
`parent: System Settings`, a screenshot in `assets/settings/`, and prose describing what the screen
shows and what each action does. `rbac.md`, `apikeys.md` and `systeminfo.md` are the reference shape.

It is written here rather than in slice 06 for two reasons: the panel gains a *Read more* link in this
slice, and shipping a link to a page that does not exist is the exact failure slice 06 was created to
fix; and the screenshot has to show the panel this slice produces, which does not exist until it lands.

Three pages, three readers, nothing duplicated:

| Page | Reader | Carries |
|---|---|---|
| `Installation/configuration.md` | the person installing | what to set and when — the key, the key store, the ring grammar |
| `settings/encryption.md` (new) | the person operating | what the panel shows, and what checking, moving and rotating each do |
| The Security page (slice 06) | the person assessing | the claims, and how to verify them — linking here rather than repeating it |

## OUT of scope

- Removing keys from the ring — hiding only. Explicit removal is a later action, and depends on 05b.
- The material comparison behind the published-key count (slice 04b).
- Docs pages and the Docker install (slice 06 / #5781).

## Dependencies

Slice 04b, so the count the warning is built on is telling the truth before its wording is settled.

## Verdict

_To be recorded at slice close._
