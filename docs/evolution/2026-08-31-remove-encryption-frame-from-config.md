# One fact, one panel — Story 5875

Shipped 2026-08-31 in a single slice. Not premium. Frontend only.

The Settings → Configuration tab carried a read-only "Secret Encryption Key" frame stating key source
and active key id. The Settings → Encryption tab already stated both, from the same `getKeyState()`
response, behind the same System-Admin gate, and added the key ring, the key store path, the custody
explanation and the rotate / re-encrypt / check actions on top. Two screens stating one fact, with no
rule for which one wins when they disagree.

## The argument was already written down

The job this traces to, `job-operator-know-the-key-is-actually-in-effect`, states its own pull as:

> "One line at startup, **one panel in settings**, one read-only check that writes nothing."

Singular. The second panel was drift introduced during Epic 5775 delivery, not a decision. Removing it
did not trade one value against another — it restored the design the job had been accepted against.
That is worth noticing as a pattern: when a removal is contested, the strongest case is often already
sitting in the job or ADR the feature was signed off on, not in a fresh argument.

The frame carried a comment defending its existence — that key custody is instance security posture and
so must be read from the System-Admin-guarded encryption endpoint rather than the broadly-readable
system-information response. That defended the **data source**, not the **placement**. The Encryption
tab reads from the same source behind the same gate, so the comment had nothing left to say and went
with the code it annotated.

## Scope held at one frame

The System Info tab's "Encryption" row stayed. It answers a different question — *do I have a key of my
own?* — for a broader audience, and `docs/settings/systeminfo.md` documents it as the fast custody
check. Removing the Configuration frame cost nothing; removing that row would have cost a documented
capability.

## What the removal turned up

**Coverage was hiding in the deleted block.** The `describe("secret encryption key")` block being
deleted held the only assertions on `KEY_CUSTODY_WORDING` — the per-custody phrasing for all four
values, the "never leak the enum name to screen" rule, and an exhaustiveness guard. `EncryptionPanel`'s
tests covered `encryption-custody-explanation` (a different map) and never `encryption-custody`.
Deleting outright would have dropped all three silently. They were relocated to
`EncryptionPanel.test.tsx`, where the rendering now lives, and a scoped Stryker run confirmed the move:
all five `KEY_CUSTODY_WORDING` mutants still die.

**A live RBAC gap, older than this story.** Adversarial review found that `EncryptionPanel` carries no
RBAC check of its own — the entire gate is the `systemAdminTabValues` set in `Settings.tsx` — and that
`Settings.test.tsx` enumerated every System-Admin tab except `encryption-tab`, which arrived with Epic
5775 and was never added. Deleting `"45"` from that set exposed key custody, active key id, the key ring
and the resolved key store path to every signed-in viewer, including an embedded-frame viewer per
ADR-137, with the entire suite green. The mutant survived on `main` too; what this story did was remove
the last test standing near it. Now it fails three tests, and the rule is in `docs/ci-learnings.md`.

**Two ADRs still described the deleted surface.** ADR-150 and ADR-152 each asserted that the Settings →
System page renders key state. The Epic 5775 journey was back-propagated when the frame was removed;
these two were missed. Both corrected.

**A docs screenshot had been quietly wrong for sixteen days.** `docs/assets/settings/configuration.png`
was last written 2026-08-14; the frame arrived 2026-08-15. The committed image never showed the frame,
so this removal needed no regeneration and made the image correct again by accident. That is a gap in
per-feature screenshot discipline, not a defect of this change.

## Worth carrying forward

A deletion has no code left for a mutation tool to mutate, so an ordinary Stryker run cannot prove the
thing stays gone. Two checks by hand covered it: changing an expected phrase to prove the relocated test
was not vacuous, and restoring the old component from `origin/main` to prove the absence assertions
actually fail when the frame comes back. Pinning an absence is the whole point — the frame arrived
originally without a single test turning red.

ADO: [#5875](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5875). Follows
[Epic 5775](epic-5775-secret-encryption-key-custody).
