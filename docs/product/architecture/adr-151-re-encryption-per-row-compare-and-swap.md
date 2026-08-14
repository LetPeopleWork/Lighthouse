# ADR-151: Re-encryption is a per-row compare-and-swap on the ciphertext; losing the race is a no-op, so no lock is needed

**Status**: Accepted — conditional on the slice-03 probe, with both fallbacks designed
**Date**: 2026-08-14
**Feature**: `epic-5775-secret-encryption-key-custody` (ADO Epic #5775, slices 03 and 04 / Stories #5778, #5779)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Resolves**: OQ-1 · Implements D6 · AC-3.2, AC-3.5, AC-3.6, AC-3.7, AC-3.13, AC-4.1, AC-4.8, AC-5.11

---

## Context

A rotation walks every stored secret and rewrites it under the active key.
`OAuthService.PerformRefreshAsync` (`OAuthService.cs:231-275`) rewrites `AccessToken` and `RefreshToken`
on those same rows whenever a token is near expiry. OQ-1 asked whether the pass has to hold off the sync
pipeline. DISCUSS scheduled a timeboxed probe and said that if a lock were needed, that would be a
design change rather than a slice tweak.

The probe is still worth running — see below — but the concurrency question turns out to be answerable
from the design rather than from measurement, and the answer removes the lock.

**Every write uses the active key.** That is D3's rule and ADR-148's grammar enforces it. So a row that
a token refresh rewrites *while a rotation is running* is written under the active key by the refresh
itself. The rotation has nothing left to do to that row. **Losing the race is not a lost update; it is a
row that arrived at the destination by another route.**

What must never happen is the other direction: the rotation writing a ciphertext derived from a
plaintext it read *before* the refresh, over a newer ciphertext holding a token the refresh just
obtained. That is a credential nobody can recover without re-authorising with the tracker — precisely
the cost this slice exists to remove. The requirement is therefore not mutual exclusion. It is: **never
write over a value you did not read.**

Prior art in the repository is relevant and partly misleading. `IConcurrencyTokenEntity` plus
`LighthouseAppContext.ApplyConcurrencyTokenForEdit` (`:551-558`) and the retry loop at `:520-548` handle
exactly this shape for user-facing edits — but `OAuthCredential` implements `IEntity`, **not**
`IConcurrencyTokenEntity`, so it has no token today. `OAuthService` also holds a per-connection
`SemaphoreSlim` (`:183`) which is in-process only and therefore does nothing across replicas.

## Decision

**Each secret is moved with a single guarded statement that names the ciphertext it observed.**

```
UPDATE <table> SET <column> = @newEnvelope
WHERE Id = @id AND <column> = @observedCiphertext
```

expressed as `ExecuteUpdateAsync` with the observed value in the `Where`. Rows affected = 1 means the
move succeeded. Rows affected = 0 means somebody else wrote the row between the read and the write —
which, by the argument above, means that row is already on the active key. It is counted as *moved by
another writer*, not as a failure, and the pass continues.

**The traversal, and the four rules on it:**

1. **Candidate selection is a prefix predicate, not a decrypt.** Rows whose stored value does not begin
   `LH1.<activeKeyId>.` are candidates. The database answers this; nothing is decrypted to find work.
   This is what makes the pass resumable with no cursor table and idempotent with no flag: re-running it
   simply finds fewer candidates, and finds none when it is done.
2. **Read, verify, then write — never write what did not verify.** Each candidate goes through
   `ICryptoService.Read` (ADR-147). Only `Envelope` and `LegacyCbc` results are re-encrypted.
   `Unreadable` and `LegacyPlaintext` are **left byte-for-byte untouched** and named in the report with
   their owning Connection and field (AC-3.5, AC-5.11). A `LegacyPlaintext` value is reported as such
   and not encrypted, because "encrypt everything that looks like plain text" is how a mis-classified
   ciphertext becomes a permanently doubly-encrypted one.
3. **One row per statement, no transaction spanning the pass.** An interruption leaves some rows moved
   and some not, and both are readable because both keys are in the ring (AC-3.6). A pass that wrapped
   everything in one transaction would hold write locks over a network round trip's worth of work and
   would still not be atomic across an interruption.
4. **`ExecuteUpdateAsync` bypasses `SaveChanges`, and that is deliberate.** It bypasses
   `LighthouseAppContext.EncryptSecrets` — correct, because the rotation writes the envelope itself and a
   second pass through `Encrypt` would double-encrypt. It bypasses `RegenerateConcurrencyTokens` —
   also correct, and worth naming: re-encryption is not a semantic edit, so an administrator with a
   Connection edit form open must not have their save rejected because a rotation ran.

**Rotation and the readability check are one traversal with a flag, not two implementations.**
`ISecretCustodyService` exposes `Task<SecretReadabilityReport> InspectAsync(CancellationToken)` and
`Task<SecretReadabilityReport> ReEncryptAsync(CancellationToken)`; the second is the first plus rule 2's
write step. The DISCUSS story map flagged slices 03 and 04 as "identical except one writes" and said to
merge them if that proved true. They stay separate as *slices* — the read-only one is what makes the
writing one safe to run, and it ships first in the release — but they are **one component**, and the
brief says so rather than discovering it in code review.

**Minting comes before any row is touched, and is itself verified.** Order for an app-owned rotation:
mint 32 random bytes → write the new ring file atomically → re-read and unprotect it → round-trip a probe
value through the new key → *only then* activate and walk. Any failure before activation leaves zero rows
written and the previous ring in force.

**Neither pass is a background job.** The encrypted set is exactly connection options flagged `IsSecret`
plus one `OAuthCredential` row per connection — bounded by the number of Connections, which is tens, not
by work items. On a Tenant-Zero-sized instance this is low hundreds of rows and completes in well under
a second of database work. AC-4.8 offered "or streams progress"; it is not needed and adding it would
buy complexity with nothing. KPI-3's 60-second budget has three orders of magnitude of headroom.

**No new permission, no new table, no migration.**

### The probe still runs, and what it must measure

The design above rests on the CAS being real. Slice 03's timeboxed probe measures three things, and each
has a designed fallback:

| # | Question | If it holds | If it does not |
|---|---|---|---|
| 1 | Does `ExecuteUpdateAsync` with a value predicate report 0 rows affected under a concurrent `SaveChanges` to the same row, on **both** SQLite (WAL, `busy_timeout=10000`) and Postgres? | Ship as decided | Fall back to **B** below |
| 2 | Under SQLite WAL with a writer holding the lock, does the statement block to `busy_timeout` and then surface `SQLITE_BUSY`? | Treat busy as *skip this row, next pass gets it* — never as a rotation failure | Same treatment; the property being checked is that busy is distinguishable from a lost update |
| 3 | Is `OAuthService`'s per-connection `SemaphoreSlim` per process only? | Expected yes — it is a field on a singleton, so it cannot coordinate replicas. Confirms that no in-process lock could ever have been the answer | If it were somehow shared, nothing changes; the CAS is still what makes the write safe |

**Fallback B, if the CAS is not honoured on one provider**: add `ConcurrencyToken` to `OAuthCredential`
via `IConcurrencyTokenEntity` — an additive migration and the repository's existing idiom — and use
`ApplyConcurrencyTokenForEdit` plus the retry loop already in `LighthouseAppContext`.
`WorkTrackingSystemConnectionOption` needs nothing, because its parent `WorkTrackingSystemConnection`
already carries a token. Cost: one migration via the `CreateMigration` script. **Still no lock.**

**Fallback C** — routing the rotation through a shared per-connection gate extracted from `OAuthService`
— is designed and rejected in advance: it is in-process only, so it is worthless on a multi-replica
deployment, and it would still need the CAS to be correct. It is recorded so that "we should just take a
lock" is answered before it is proposed.

## Alternatives Considered

**Hold off the sync pipeline for the duration of the pass.** The literal reading of OQ-1. **Rejected**:
it makes a routine administrative action a maintenance window, which is the cost the epic exists to
remove; the only mechanism available in-process (a semaphore) does not span replicas, so it would be a
lock that appears to work and does not; and it is unnecessary, because a concurrent writer moves the row
to the destination rather than away from it.

**Mark the secret columns as EF concurrency tokens in the model.** Would give the CAS through
`SaveChanges` with no raw statement. **Rejected**: `IsConcurrencyToken` is model-level, so *every* write
to those columns — including a plain "save this connection" from the settings page — would carry the
check, and a save that did not read the current ciphertext would start throwing. It changes the
semantics of the ordinary path to serve the exceptional one.

**A rotation cursor table with per-row status.** Explicit resumability, a progress bar. **Rejected**: the
key id in the envelope *is* the cursor, and it cannot go stale because it is the same fact the reader
uses. A cursor table would be a second, independently-wrong copy of the truth, plus a migration.

**Re-encrypt lazily, on read.** Every decrypt rewrites the row under the active key; a rotation becomes
"change the active key and wait". Genuinely elegant, and it never races. **Rejected**: it makes a read
path a write path, which turns a background sync into a database writer under load; it makes containment
unbounded in time, so an administrator responding to a suspected exposure can never be told it is done;
and AC-3.4 requires a number, which lazy re-encryption cannot produce.

## Consequences

**Positive**

- AC-3.7 holds without coordination: the refreshed token is never lost, and the row is never left
  unreadable, because the only write that can happen concurrently already produces a valid envelope
  under the active key.
- Resumability and idempotence are properties of the candidate predicate rather than of bookkeeping, so
  neither can be got wrong by an interruption at an awkward point.
- One component serves slices 03 and 04, and one code path serves both custody modes (AC-3.13) — the
  difference between them is which button is offered, not which code runs.
- No migration, no table, no lock, no permission, and no change to the ordinary save path.

**Negative / accepted**

- Rows affected = 0 is reported as *moved by another writer* and the pass does not re-verify it. In the
  vanishingly rare case that a concurrent writer wrote something that is not a valid envelope, that row
  is caught by the next readability check rather than by this pass. Running the check after a rotation
  is the documented step (AC-4.6) and is what makes this acceptable.
- `ExecuteUpdateAsync` writes outside the `SaveChanges` pipeline, so anything that pipeline does in the
  future must be considered against this call site. Enforced by an architecture test naming the two
  columns it may touch.
- The decision is provisional on a probe whose failure mode is a migration, not a redesign. That is
  weaker than "settled" and stronger than what DISCUSS feared.

## Earned Trust — what is probed, not assumed

| Assumption | Probe |
|---|---|
| The CAS is honoured on both providers | The slice-03 probe, run on SQLite-WAL and on Postgres, before the brief is committed to |
| A refresh landing mid-rotation loses nothing | Gold test: begin re-encryption of an `OAuthCredential`, drive `PerformRefreshAsync` to completion on the same row, finish the pass → the stored token decrypts to the **refreshed** value under the active key (AC-3.7) |
| An unreadable secret is never overwritten | Gold test: a corrupted value in the set → after the pass, the stored bytes are identical and the report names the Connection and field (AC-3.5) |
| The pass is idempotent | Test: run it three times → second and third move zero rows and report identical totals |
| An interruption leaves a working instance | Test: cancel after N rows → every Connection still decrypts, re-run completes the remainder (AC-3.6) |
| Re-encryption does not invalidate an open edit form | Test: load a connection, run a rotation, save the connection with the original concurrency token → succeeds |
| The published default leaves the ring only when nothing references it | Test: one row deliberately left unreadable under the default → the default stays; remove it → the default is dropped (AC-3.9) |

## Cross-reference

- [ADR-147](./adr-147-stored-secret-states-classified-by-inspection.md) — the `Read` this pass consumes,
  and the states it may and may not write over.
- [ADR-146](./adr-146-secret-envelope-wire-format.md) — the prefix that makes the candidate predicate a
  database-answerable question.
- [ADR-152](./adr-152-custody-mode-and-the-encryption-admin-surface.md) — the two actions this component
  serves and which custody mode gets which.
- `LighthouseAppContext.cs:520-558` — the `IConcurrencyTokenEntity` idiom fallback B would use.
- `OAuthService.cs:178-206` — the per-connection semaphore, and why it is not the answer.
