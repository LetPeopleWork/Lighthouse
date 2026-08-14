# Slice 01 — A secret that cannot be read says so

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5777 · **Story**: US-01 (+US-07 precursor) · **Estimate**: ~6h
**Reference class**: `CryptoService` + `LighthouseAppContext.EncryptSecrets` — the write path and the
five call sites that consume a decrypted secret already exist and are unchanged in shape. This slice
changes what the stored bytes are and what happens when they do not verify.

## Goal

Every secret written from now on carries a format version, a key id, a nonce and an authentication tag,
and a secret that cannot be read raises and is named on the Connection that owns it instead of being
handed to a work tracking system as a credential.

## IN scope

- **US-07 precursor commit**: the key-ring type (one active key, N retired, each with an id) and the
  envelope reader/writer. No user-visible behaviour; ships first inside this slice, never as a slice.
  The ring holds exactly one key in this slice — the configured one — so nothing else has to move yet.
- AES-GCM write path with a versioned envelope: format version, key id, nonce, ciphertext, tag.
- Read path that dispatches on the envelope: a current envelope, a legacy AES-CBC blob, and a legacy
  never-encrypted plaintext value are three recognised states with three explicit branches.
- **Deleting the `catch (CryptographicException | FormatException) → return cipherText` fallback.**
  This is the point of the slice. Legacy plaintext is recognised by inspection, not by catching.
- An unreadable-secret state surfaced on the owning Connection's field, and once per affected secret in
  the log rather than once per sync attempt.
- Every auth strategy and the OAuth refresh path stop short rather than sending an unverified value.
- Expand-only migration widening the stored columns for the envelope. Nothing renamed, nothing dropped.

## OUT of scope

- Generating a key (slice 02) — the ring is populated from configuration only.
- Rotation (slice 03), the readability check (slice 04), the chart (slice 05), docs (slice 06).
- Removing the default key from `appsettings.json` — slice 02 owns that, because removing it before the
  ring can retire it would break existing installs.

## Learning hypothesis

**Disproves** "the format can migrate in place" if real stored blobs turn out to be ambiguous — a
legacy AES-CBC ciphertext whose leading bytes are indistinguishable from a new envelope header. Test
against every secret in the `:5169` restored database, not against fixtures.
**Confirms**, if it holds, that slices 02-05 can each assume a readable, attributable secret and that
a failed read is always a real failure rather than a format guess.

## Acceptance criteria

AC-1.1 through AC-1.9 in `feature-delta.md`. The two that carry the slice:

- **AC-1.4** — a bad authentication tag raises. It does not return ciphertext, plaintext, or empty.
- **AC-1.5** — any flipped byte fails to decrypt rather than producing altered plaintext.

## Dependencies

- Bug #5776 released, so the key in effect during acceptance is the one the operator meant.
- `:5169` restored from a real backup — the only place AC-1.2 (legacy blobs still read) can be
  honestly tested.
- `CreateMigration` for the column widening.

## Dogfood moment

Same day: point a dev build at the restored `:5169` database, confirm every existing Connection still
syncs, then corrupt one stored byte and watch the Connection say so instead of Jira saying 401.

## Pre-slice SPIKE

None. The uncertainty here is data-shaped and is answered by the acceptance test itself.

## Verdict

_To be recorded at slice close: confirmed / disproved._
