# ADR-146: The stored secret is a prefixed ASCII envelope whose key id is bound into the authentication tag

**Status**: Accepted
**Date**: 2026-08-14
**Feature**: `epic-5775-secret-encryption-key-custody` (ADO Epic #5775, slice 01 / Story #5777)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Implements**: D1 · AC-1.1, AC-1.2, AC-1.4, AC-1.5

---

## Context

A stored secret today is `Convert.ToBase64String(iv || aes-cbc-ciphertext)`
(`CryptoService.cs:22-45`). It carries no version, no key identifier, and no authentication tag. Three
consequences follow, and all three block the rest of the epic rather than merely annoying it.

**Nothing proves a decrypt was correct.** AES-CBC with PKCS7 will produce *some* plaintext for most
wrong keys, and the padding check that would usually catch it succeeds by chance roughly one time in
256. A re-encryption pass that trusts such a result writes garbage over a working credential and reports
success. Rotation is not safe to build on a cipher that cannot say no.

**Nothing says which key wrote a row.** A rotation has to know which rows it still owes work on, and a
readability report has to attribute each secret to a key. Both are impossible to answer without reading
every row with every key and guessing from the result.

**The column is a string and it already holds two other shapes.** Values written before encryption
existed are plain text. Values written since are bare CBC blobs. D2 deletes the catch-all that made all
three indistinguishable, so the reader now has to tell them apart *by inspection* — the DISCUSS wave was
explicit that catching an exception is not an acceptable discriminator, because an exception-driven
classifier is exactly what hid the original defect.

The binding constraint on the format is therefore **provable disjointness from a legacy CBC blob**, not
compactness.

## Decision

**A secret written from slice 01 onward is stored as four dot-separated ASCII fields:**

```
LH1.<keyId>.<base64url(nonce)>.<base64url(ciphertext || tag)>
```

1. **`LH1` is the format version token**, matching `LH<major>`. A future incompatible format is `LH2`
   and the reader keeps accepting `LH1` for as long as any row may hold one.
2. **`keyId`** matches `[a-z0-9-]{1,32}` — a charset that cannot contain the `.` delimiter, so the
   envelope can never be mis-split.
3. **AES-256-GCM**, 12-byte nonce drawn from `RandomNumberGenerator`, 16-byte tag. .NET's `AesGcm`
   returns tag and ciphertext separately; they are concatenated in that order for storage.
4. **The header is Associated Data.** The UTF-8 bytes of `LH1.<keyId>` are passed to `AesGcm.Encrypt`
   and `AesGcm.Decrypt` as AAD. Relabelling a valid ciphertext with a different ring key's id therefore
   fails its tag rather than decrypting under the wrong key and being believed. Without this the key id
   is an unauthenticated hint an attacker with database write access can steer.
5. **`base64url` without padding** (`-`/`_`, no `=`) so no field can contain `.` or `=` and the whole
   value stays URL-safe and copy-pasteable out of a database browser.

**The discriminator is `value.StartsWith("LH1.")`, and it is provably unambiguous against legacy CBC.**
A legacy value is standard base64 over `[A-Za-z0-9+/=]`. `.` is not in that alphabet. No legacy CBC blob
can begin with `LH1.` — this is a property of the alphabets, not a probability.

**Column widths are unchanged.** `WorkTrackingSystemConnectionOption.Value`,
`OAuthCredential.AccessToken` and `.RefreshToken` carry no `HasMaxLength` in `OnModelCreating` and
appear in both model snapshots as unbounded `text` / `TEXT`. The envelope adds roughly 10% to a stored
value (a header of ~20 characters plus 22 characters of tag, against a saved CBC IV of 24). **No EF
migration is required by this ADR** — which corrects the DISCUSS pre-requisite that anticipated a
width-growing migration.

## Alternatives Considered

**A binary struct, base64-encoded whole** — `base64(0x01 || keyIdLen || keyId || nonce || ct || tag)`.
More compact by roughly a third, and the conventional choice. **Rejected because its discriminator is
probabilistic.** A legacy CBC blob is also valid base64, and its first decoded byte is a random IV byte
that equals the version marker one time in 256. Widening the magic to eight bytes reduces the collision
to 2⁻⁶⁴ but never removes it, and the requirement written into the DISCUSS wave is inspection, not luck.
It also forces a base64 decode before a row can be classified, so the readability check and the
rotation cursor lose the ability to filter rows with a `LIKE 'LH1.%'` predicate the database can answer.

**A JSON envelope** — `{"v":1,"kid":"…","n":"…","ct":"…"}`. Discriminating on `{` is equally disjoint
from base64, so it is correct. **Rejected on cost and on drift.** It roughly triples the stored overhead,
puts a JSON parse in the hottest read path in the credential flow, and — the real objection — an
open-ended object invites fields. A stored secret's format is a contract that has to be readable by
every future version of the product; four positional fields cannot quietly grow a fifth that an older
reader ignores.

**Keep AES-CBC and add an HMAC** — encrypt-then-MAC over the existing blob. Preserves the current
cipher and the current column shape. **Rejected**: it is strictly more code than AES-GCM for the same
guarantee, it needs a second key or a documented derivation, .NET ships `AesGcm` as a single primitive
that cannot be composed wrongly, and the composition it replaces is the classic place to get
constant-time comparison and ordering wrong. There is no compatibility argument for keeping CBC, because
old rows are read by the legacy branch either way.

## Consequences

**Positive**

- A wrong key, a truncated value or a single flipped byte is a failure with a name, not plausible
  bytes. AC-1.4 and AC-1.5 become properties of the primitive rather than things to remember to check.
- Rotation gains its cursor for free: "rows not yet on the active key" is a prefix comparison on a
  string column, answerable without decrypting anything.
- The key id is visible to a human reading the database, which is what makes the readability report
  explainable to an operator and the upgrade path auditable.
- Relabelling attacks are closed by construction, at zero runtime cost, because AAD is a parameter the
  primitive already takes.

**Negative / accepted**

- Roughly 10% storage growth on a few hundred rows. Immaterial, and stated so it is not rediscovered.
- The key id is stored in clear next to the ciphertext. It names a key, never any part of its material,
  and a reader who has the database already knows how many keys are in play from the distinct-id count.
  Accepted deliberately: the alternative — an opaque id — would make the operator surface unexplainable.
- Four positional fields cannot be extended. That is the point, and a fifth field means `LH2`.

## Earned Trust — what is probed, not assumed

| Assumption | Probe |
|---|---|
| `.NET AesGcm` rejects a modified tag on this runtime and both target platforms | Gold test: flip one bit of every field in turn → decrypt raises, four cases |
| The key id is genuinely bound | Gold test: take a valid envelope, rewrite its `keyId` to another ring key's id, decrypt under that key → raises |
| A legacy CBC blob cannot be read as an envelope | Property test over 10 000 random CBC blobs → none starts `LH1.` |
| Nonces do not repeat within a key | Test that 100 000 encrypts of the same plaintext under one key yield 100 000 distinct nonces; GCM nonce reuse is the one way to lose everything at once |
| The column really is unbounded on both providers | Integration test writing a 100 KB secret on SQLite and Postgres |

## Cross-reference

- [ADR-147](./adr-147-stored-secret-states-classified-by-inspection.md) — the reader that classifies
  the three stored shapes and the failure that replaces the plaintext fallback.
- [ADR-148](./adr-148-key-ring-canonical-form-and-retired-default.md) — where `keyId` values come from
  and how the ring is expressed.
- [ADR-008](./adr-008-oauth-credential-separation.md) — the two storage shapes this format has to
  serve: option rows and `OAuthCredential` columns.
