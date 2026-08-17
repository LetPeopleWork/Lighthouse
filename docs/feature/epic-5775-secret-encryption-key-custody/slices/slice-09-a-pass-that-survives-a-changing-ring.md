# Slice 09 — A pass that survives the ring changing under it

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5796 · **Estimate**: ~5h
**Origin**: not from DISCUSS. Found by the independent security review of 2026-08-17
(`verification/security-review-findings.md`, findings E4 and E5; prompt in `security-review-brief.md`).
**Ordering**: last of the three remediation slices, and it blocks the release. It is last because it is
the only one carrying a design question rather than a correction.

## Goal

A re-encryption pass either finishes against the ring it started on, or says it did not — and never
leaves a credential behind on a key it has stopped naming.

## The defect

`SecretCustodyService.WalkAsync` reads `keyRingHolder.Current.ActiveKey.Id` once, uses it to build the
"there is nothing left to do here" filter and to label the report, and then loops. Inside the loop,
`cryptoService.Encrypt` re-reads `Current.ActiveKey` on every single call, because that is the only
thing `ICryptoService` offers. The pass therefore has two different opinions about which key is active
and no way to notice that they have diverged.

If the watcher replaces the ring mid-pass — an operator rotating the mounted Secret while an
administrator is moving secrets, which is exactly the pair of actions this feature invites — rows before
the swap land on the old key, rows after it land on the new one, and the report claims a single
`ActiveKeyId` that is now stale. That much is untidy rather than dangerous.

The dangerous part is the filter. Rows already sitting on the *old* active key were excluded from the
candidate list before the swap happened, so the pass never looks at them again. If the operator's new
file dropped the old key, those credentials are now unreadable — and the report does not name them,
because naming them requires having walked past them. The administrator is told the rotation succeeded
and how many secrets moved, and the ones that quietly did not move are the ones they will not hear
about until a work tracking system stops updating.

**The design question this slice has to answer.** The clean fix is for the pass to snapshot the ring
once and encrypt against *that* — but `ICryptoService.Encrypt` takes only a plaintext, so encrypting
under a named key means widening a port that four connectors and the database context depend on. The
cheap fix is to detect the divergence and report it: capture the ring at the start, compare at the end,
and if it changed, say so and say the pass has to be run again. The cheap fix is honest and small; the
clean fix removes the failure rather than reporting it. Decide it in the slice with the port in front of
you, not here.

**E5, and why it is documented rather than built.** Two replicas sharing one key store can both find no
ring file, both mint, and both write — the last `Move` wins, and the loser holds a key in memory that
the file no longer names. It encrypts under that key until it restarts, at which point the key is gone.
The read-back-and-compare in `GeneratedKeyRingStore.Write` catches this only when the other replica's
move lands inside the window between this replica's move and its read, which is a race rather than a
guarantee.

It is documented rather than fixed because minting only happens under `GeneratedForThisInstance`
custody, and the one supported topology that runs more than one replica — the chart — refuses to install
without an operator-supplied key, which makes the minter a refusal object. Reaching it requires two
containers on one bind mount or NFS share, or a Compose file scaled past one, neither of which is a
shape the product claims to support. What is missing is that nobody has ever written that down, so an
operator who tries it gets no warning at all. A lock file would be real work for a topology we do not
support; a sentence is the proportionate answer, and if a real deployment ever hits it, the sentence is
what makes it diagnosable.

## IN scope

- **A pass is consistent about which ring it is working against, or it reports that it was not.** Which
  of the two shapes above delivers that is the slice's decision; both satisfy the goal, and the port
  change is the only thing separating them.
- **A pass that ran against a changing ring does not report success.** Whatever the mechanism, the
  administrator must not be told a rotation completed when rows were skipped under a key that has since
  gone.
- **The single-writer requirement on the key store is written down** — in `ARCHITECTURE.md` beside the
  custody modes, and in the configuration page where `Encryption__KeyStorePath` is described. Two
  instances must not share a key store unless the key was supplied to both.

## OUT of scope

- A lock file, a lease, or any other single-writer enforcement on the key store. Explicitly deferred, on
  the reasoning above. If it is ever built, the sentence this slice writes is what it will replace.
- The replica skew after a rotated mounted Secret — replicas pick a new ring up on independent timers,
  so for up to one interval a replica on the old ring cannot read what a replica on the new one has
  written. It self-heals within the interval and the only fix is coordination this epic has deliberately
  avoided. Worth a line in the rotation documentation; not worth code.
- Anything about who may start a pass. That is settled and unchanged.

## Learning hypothesis

**Disproves** "the compare-and-swap makes the pass safe against anything" — it stops a row being
clobbered, and it does nothing at all about a row that was never a candidate. If widening the port turns
out to be cheap, it also disproves the assumption behind `ICryptoService` taking only a plaintext, which
was that nothing would ever need to encrypt under a key other than the active one.

**Confirms**, if it holds, that "what is left to do is written on the data itself" — the property the
service's own header comment leans on for resumability — is true only while the ring holds still, and
that the pass needs to say which ring it read that data against.

## Acceptance criteria

- **AC-9.1** — A pass whose ring is replaced mid-flight either completes entirely against the ring it
  started on, or completes and reports that the ring changed and the pass must be run again. Silence is
  a failure of this criterion.
- **AC-9.2** — No row is excluded from a pass by a filter built against a key that is no longer on the
  ring by the time the pass ends, without that row being named in the report.
- **AC-9.3** — A rotation that finishes cleanly reports exactly what it reports today. The common path
  is not made noisier to describe the rare one.
- **AC-9.4** — `ARCHITECTURE.md` and the configuration page both state that a key store belongs to one
  instance unless the key was supplied from outside, and what happens if that is ignored.

## Dependencies

Slices 01–06b. Slice 08 ahead of it, because a pass that can be disturbed by a watcher that should not
have been running is the wrong defect to fix first. No new data, no migration. Whether `ICryptoService`
changes shape is the open decision; if it does, the shared-contract rule applies and the test factory
comes first.

## Dogfood moment

Same day: start a re-encryption pass on an instance holding enough credentials for it to take a few
seconds, replace the mounted keys file while it runs, and read the report. It must not say the rotation
finished cleanly.

## Pre-slice SPIKE

**Timeboxed, 1h, before the slice is planned.** Widen `ICryptoService` to encrypt under a named key on a
branch and count the call sites that have to change. If it is the four auth strategies and the database
context and nothing else, take the clean fix; if it spreads further, take the reporting fix and record
why. This is the only open design question in the three remediation slices and it is cheaper to answer
than to argue about.

## Verdict

_To be recorded at slice close: confirmed / disproved._
