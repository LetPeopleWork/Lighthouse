# Secret encryption and key custody — Epic 5775

Shipped 2026-08-11 → 2026-08-18 in thirteen slices, plus an independent security review and a nine-run
manual walkthrough. Free on every instance. Released in v26.8.31.7.

Lighthouse was built for one person forecasting one team, and its secret handling was sized for that:
one encryption key, shipped inside the product, identical in every copy ever downloaded. The documented
override for it never reached the code that read it. Nothing had gone wrong — but *who holds the key to
our Jira token* had become a question people expect answered in writing before they connect anything.

Companion evidence lives in [`epic-5775-secret-encryption-key-custody/`](epic-5775-secret-encryption-key-custody/):
the security review brief and findings, the walkthrough script, and the run log.

## What shipped

| Slice | ADO | What it added |
| --- | --- | --- |
| 01 | 5777 | A self-describing envelope, so a secret that cannot be read says so instead of returning noise. |
| 02 | 5024 | Every instance mints and keeps a key of its own on first start. |
| 03 | 5778 | Rotate the key without asking anyone for a token — re-encryption in place. |
| 04 | 5779 | Check before you rotate, and prove it after: a read-only report of what sits under which key. |
| 04b | 5789 | The published key can never be the active key. |
| 05 | 5780 | The cluster owns the key — the chart refuses to install without one. |
| 05b | 5790 | A way back in after the key is gone. |
| 06 | 5781 | Say what is actually true about stored credentials: the panel, the banner, and `docs/security.md`. |
| 06a | 5791 | The panel offers only what would help in the state it is in. |
| 06b | 5793 | What a caller who is merely signed in gets to learn. |
| 07 | 5794 | A refusal that cannot quote the key. |
| 08 | 5795 | The key that won at startup is the key that stays. |
| 09 | 5796 | A pass that survives the ring changing under it. |

ADRs **146**–**153**. Bug #5776 (the configured-key override never applied) is the defect that opened
the Epic.

## The four slices that exist because a review said so

Slices 04b, 07, 08 and 09 were not in the original plan. They are remediation for an **independent
security review** run against the shipped-but-unreleased code, from a written brief of nine claims the
Epic believed it had established. Four of those claims did not survive:

| Claim | Verdict | Became |
| --- | --- | --- |
| No key material escapes | **Does not hold** (E1) | slice 07 |
| The published key can never be the active key | **Does not hold** (E3) | slice 04b |
| Resolution precedence is one ordered list in one place | **Does not hold after boot** (E2) | slice 08 |
| Re-encryption never overwrites what it could not read | **Half holds** (E4) | slice 09 |

E1 is the one worth remembering: `TryParseEntry` split a key-ring entry at the first colon and quoted
the text before it back **in full, with no length cap**, when it was not a usable key id. Base64 key
material is never a usable id — so a value with material before the first colon had that material
quoted into the defect message, which the watcher then logged. A claim of the form *no key material
escapes* is exactly the kind that reads as obviously true and is falsified by one error path.

Two findings from the review's **wider sweep** were outside the Epic and shipped anyway, because they
were worse than anything inside it:

- **W1 · Critical** — an anonymous POST replaced the binaries and restarted the instance. Now behind
  the System Administrator check (ADO #5797).
- **W2** — the connection-string filter was a denylist splitting on a character the password may
  itself contain, so the system information response could carry part of the database password
  (ADO #5798).

## Nine walkthrough runs, and why "the tests pass" was not the bar

Three slices changed **startup refusal** behaviour, where the difference between green tests and a
working product is whether the refusal text names something an operator can act on. All nine runs were
executed against real substrates — upgrade groups starting from the released `v26.8.14.1` binary and
image, never a hand-made database, with a real work-tracking credential proved end to end by validating
with the secret field sent empty, so the stored ciphertext is what goes on the wire.

All nine passed. **Nothing found was a behaviour defect** — every finding on the final pass was wording
or documentation (F-30 through F-36), and all were fixed the same day.

The findings that mattered were about text, and text is the product here:

- The refusal named `EncryptionSettings:EncryptionKey`, the internal configuration path, whether the
  value arrived from a file or an environment variable. A Docker or Kubernetes operator greps their
  compose file for the colon form and finds nothing — while the same sentence spelled its remedy
  `Encryption__Key`. One message, two conventions.
- The `Kept in` row rendered the key-store directory unconditionally, including in the custody where
  the key is *not* in it — which is the backup trap the row exists to prevent.
- The panel offered a move that the report said was unnecessary: the difference was the storage format
  (`LegacyCbc` → `Envelope`), which appeared nowhere on the screen.
- The rotation instruction echoed `keySuppliedThrough`, so an instance on the retired setting name was
  told to keep using the setting the banner had just said was going away.

## The state with no button, found on a live deployment

The highest-value finding of the whole Epic came from repairing a real Docker-Compose-on-Postgres
instance on 2026-08-17. A Postgres deployment, or a container whose database file is not on a mounted
volume, has **nowhere durable to keep a key** — so it starts on the published key, keeps using it for
credentials entered from then on, warns on every start, and offers **no *Move* or *Rotate* button**,
because a move needs a destination key and there is none.

That is correct behaviour and a terrible experience, and it is why the release notes lead with an
ACTION REQUIRED callout rather than a feature announcement. Two documentation defects fell out of the
same repair and were blocking: the documented Docker install bind-mounted a host directory the
non-root container cannot write (so it failed on every Linux host), and the shipped Postgres Compose
example crash-looped on its second start under `restart: always`.

**Minting a key is not re-encrypting.** Stated everywhere it can be, because an upgrade that silently
did half the job would be worse than one that did nothing.

## Mutation testing

Per-slice, gate 80 %. Representative: slice 01 (5777) **94.88 %** backend / **94.59 %** frontend; slice
04b (5789) **100 %** backend; slice 09 (5796) **96.34 %** backend / **92.31 %** frontend, reached over
three runs (93.90 → 95.12 → 96.34), each jump driven by the survivor list rather than by adding volume.

## Lessons

- **Write the claims down, then have someone try to break them.** The review brief was a list of nine
  sentences the Epic believed. Four were false, and none of the four would have been found by asking
  "is this tested?" — they were found by asking "on which path is this not true?".
- **A security review of one Epic will find things outside it.** W1 was a critical unauthenticated
  remote-code path in unrelated code. Scoping the review to the Epic would have left it there.
- **Grade the wording, not just the behaviour.** Every unfixed finding on the final walkthrough pass was
  a sentence, and for a feature whose entire surface is *tell the operator what is true and what to do*,
  a remedy that does not work verbatim is the highest kind of defect.
- **Repair a real deployment before shipping.** The no-button state was reproducible in principle and
  invisible in practice until someone upgraded an actual instance.

## Open at finalize

- ADO #5775 and its Stories are left **Resolved**, not Closed.
- No security advisory — maintainer decision V7, on the grounds that the key shipped in the open in
  every build, so an advisory would describe the fix rather than a breach.
- A link checker over `docs/` (UI-06-5) was **dropped**, not filed — worth having, not this Epic's to
  carry.
- The dogfood check: answer the security questionnaire that started the Epic from `docs/security.md`
  alone, with no maintainer in the loop. Anything unanswerable that way is a gap in the page.
