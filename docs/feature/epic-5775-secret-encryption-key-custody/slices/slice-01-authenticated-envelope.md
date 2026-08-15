# Slice 01 — A secret that cannot be read says so

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5777 · **Story**: US-01, US-08 (+US-07 precursor) · **Estimate**: ~7h
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
- **One generic secret-handling notice on the connection form** (US-08), rendered once where the form
  contains at least one secret field — not once per field, and naming no connector, so a single string
  serves every secret any connector defines now or later. It says what happens to a pasted credential
  in plain words and links to the docs, and every claim in it is already true on every install
  regardless of key custody, which is why it belongs here rather than waiting for slice 02. It makes no
  claim about which key the instance holds and carries no warning styling — the person pasting a token
  usually cannot configure a key, so alarm there is alarm without a remedy. Key truth lives on
  Settings → Encryption instead.
- No EF migration. The three secret columns are unbounded `text`/`TEXT` in both model snapshots with no
  `HasMaxLength`, so envelope values need no width change.

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

AC-1.1 through AC-1.9 and AC-8.1 through AC-8.7 in `feature-delta.md`. The three that carry the slice:

- **AC-1.4** — a bad authentication tag raises. It does not return ciphertext, plaintext, or empty.
- **AC-1.5** — any flipped byte fails to decrypt rather than producing altered plaintext.
- **AC-8.3** — every claim in the notice is true on every install regardless of key custody.

## Dependencies

- `:5169` restored from a real backup — the only place AC-1.2 (legacy blobs still read) can be
  honestly tested.
- No EF migration. The three secret columns are unbounded `text`/`TEXT` with no `HasMaxLength` in
  either model snapshot, so the column widening this slice originally assumed does not exist
  (DESIGN F-6).
- Bug #5776 is **not** a dependency of this slice. It moved to slice 02 when D7 was retired, and
  nothing here reads configuration, so the key in effect during acceptance is whatever the instance
  already had.

## Dogfood moment

Same day: point a dev build at the restored `:5169` database, confirm every existing Connection still
syncs, then corrupt one stored byte and watch the Connection say so instead of Jira saying 401. Then
open a connection form as somebody who has never seen it, read the notice, and check that reloading
leaves the secret field blank — the one claim in it a user can verify in four seconds.

## Pre-slice SPIKE

None. The uncertainty here is data-shaped and is answered by the acceptance test itself.

## Verdict

**Confirmed, 2026-08-15. The format migrates in place.** The hypothesis said this slice would be
disproved if a real stored blob turned out to be ambiguous between the legacy form and the envelope.
None is, and the reason is structural rather than probabilistic.

**The count of true legacy-plaintext rows is zero** (OQ-4, recorded in full in `feature-delta.md`).
Measured against the real backup in `Lighthouse.Backend/DB_Backup` rather than against seeded demo
data: five connections, 28 options, four of them marked secret. All four decode from base64 to 112,
224, 80 and 32 bytes, and each is sixteen bytes of initialisation vector followed by a whole number of
sixteen-byte blocks — the shape AES-CBC produces, and one no typed credential lands on by accident.
`OAuthCredentials` holds no rows at all. So the residual the four-state reader documents — a stored
value that was never encrypted but happens to look like a legacy blob, and is therefore reported
unreadable rather than handed back — describes nothing that exists on this data, and the release note
owes operators no re-entry instruction.

**The two forms cannot be confused, and that is a property of the two alphabets.** An envelope begins
`LH1.`, and `.` is outside the standard base64 alphabet, so no value the previous implementation could
write can begin with the prefix. Ten thousand randomly generated legacy blobs confirm it: none carries
the prefix and none classifies as an envelope.

**Nothing about the stored value's size forces a schema change.** A 100 KB credential round-trips
through the three secret columns on both real SQLite and real PostgreSQL, so the columns need no
widening and this slice ships no EF migration.

**What the verdict does not cover.** The dogfood run described above — pointing a dev build at the
`:5169` restored database, watching every existing Connection still sync, then corrupting one stored
byte — is a manual step for the maintainer and is not claimed here. The evidence above is what the
slice can establish without it, and it is what the hypothesis asked for.

Per-feature mutation testing on the changed surface is recorded in `../mutation/results.md`.
