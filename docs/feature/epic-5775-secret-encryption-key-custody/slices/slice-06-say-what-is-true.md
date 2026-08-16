# Slice 06 — Say what is actually true

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5781 · **Story**: US-06 · **Estimate**: ~4h
**Reference class**: `docs/Installation/configuration.md` already has an Encryption Key section with
the right intent — it documents a configuration path the code does not read. This slice rewrites it
against shipped behaviour rather than starting a new page.

## Goal

The documentation, the compliance self-assessment and the security policy describe what the product
actually does, and the people running the affected versions are told at the moment they can act.

## IN scope

- The Encryption Key section rewritten: the configuration path the code reads, first-boot generation,
  all three custody modes, and the observable signal that says which one is in effect.
- Rotation documented — that it needs no credential re-entry, and what the report's four secret states
  mean — in both forms: the one-action rotation where the application owns the key, and the Kubernetes
  sequence where the operator does (add the new key to the Secret alongside the old, roll the pod,
  re-encrypt, drop the old key), including that Lighthouse never writes to the Secret itself.
- The Docker page states where the key store lives, that it belongs on the mounted data volume, and
  what recreating a container without that volume costs.
- **What an operator must back up, and what is lost if they do not.** Losing the generated key loses
  every stored secret. That is the correct security property and it must not be discovered by a user.
- `docs/compliance/cra-self-assessment.md` rows 1.3 and 1.5 re-evidenced against shipped behaviour.
- **One canonical Security page in the docs navigation, two layers at one URL.** The plain answer first
  — a few sentences, no jargon, no algorithm names, the same four facts as the in-product notice from
  slice 01 — then a *verify our claims* section with what is encrypted and with what, what is
  deliberately hashed instead and why, key custody per deployment shape, and a **"what this does not
  protect against"** section. One URL, because the same link has to serve the person asking and the
  security person they forward it to. Today those facts are spread across a configuration reference,
  three connector pages, two compliance documents and a root `SECURITY.md` with no presence in the docs
  site — the gap is not a missing claim, it is a missing address.
- The connector concept pages swap their isolated "stored encrypted in its database" one-liner for the
  plain-language answer plus a link. Per-connector specifics stay there; nothing is duplicated.
- **`ARCHITECTURE.md` gains a dedicated section on secret encryption and key custody**, alongside the
  other load-bearing concerns rather than as a clause inside Persistence. It covers the envelope and
  where it is written, the key ring and its builder-time resolution, the three custody modes and what
  each can and cannot be asked to do, where the key store sits relative to the database, and the
  read-only check and the re-encryption pass as the two things that walk every stored secret. The ADR
  index row for 146-153 still says *"Designed, not yet built — nothing in this release implements
  them"*; that stopped being true at slice 01 and is corrected here. Today the whole document mentions
  encryption twice, and one of those two is that stale row.
- The vulnerability reporting path appears in the docs, not only in the repository-root file.
- Slice 01's in-product link is repointed at the Security page.
- `SECURITY.md`: reporting path, and what this epic changed.
- ~~A GitHub Security Advisory, published when the fixed version is installable, not before.~~
  **Withdrawn by maintainer decision V7, 2026-08-16**: the key was never withheld. It shipped in
  `appsettings.json` in every build and sat in the public repository, which is documented behaviour
  rather than a defect, so an advisory would describe the fix rather than a breach. What operators need
  is the upgrade instruction and the move, and the release notes carry both. **AC-6.7 is withdrawn with
  it.**
- Release notes leading with what the operator should do.
- Seeded terminology throughout — never one tracker's vocabulary.

## OUT of scope

- Marketing website security copy. Flagged for the DELIVER checklist; this slice does not edit another
  repository.
- A formal threat model document. If writing this slice produces one worth keeping, it lands as an
  architecture note, not as scope creep here. The `ARCHITECTURE.md` section is not that document: it
  says how the mechanism is built, not what an attacker would do with it — the limits belong on the
  Security page, where a reviewer is already reading.

## Learning hypothesis

**Disproves** "the configuration-path mismatch was the only false claim" if writing the three custody
modes end to end surfaces further gaps between what the docs assert and what the build does. The
compliance self-assessment is the likeliest place: it makes claims across several rows that nobody has
re-read against the code since they were written.
**Confirms**, if it holds, that a prospect's security reviewer can complete an assessment from the
public documentation — which is the conversation that lost the evaluation this epic came from.

## Acceptance criteria

AC-6.1 through AC-6.18 in `feature-delta.md`. The four that carry the slice:

- **AC-6.5** — the compliance self-assessment cites evidence that matches shipped behaviour.
- **AC-6.7** — the advisory publishes when the fixed version is installable, not before.
- **AC-6.13** — the Security page answers the plain question at the top and lets a reviewer verify the
  claims below it, at one URL.
- **AC-6.18** — `ARCHITECTURE.md` describes how the product protects credentials, in a section of its
  own. It is the document a reviewer opens before the docs site, and it is currently silent.

## Dependencies

- Slices 01-05 released, or the documentation describes something nobody can install.
- The release that carries the fix, so the advisory and the release notes point at a real version.

## Dogfood moment

Same day: take the security questionnaire that started this epic and answer every question from the
published documentation alone, with no maintainer in the loop. Anything that cannot be answered that
way is a gap in this slice, not in the questionnaire.

## Pre-slice SPIKE

None.

## Verdict

_To be recorded at slice close: confirmed / disproved._
