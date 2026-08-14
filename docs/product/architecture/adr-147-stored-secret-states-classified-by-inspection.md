# ADR-147: A stored secret has four states; `Decrypt` refuses and a separate total reader classifies

**Status**: Accepted
**Date**: 2026-08-14
**Feature**: `epic-5775-secret-encryption-key-custody` (ADO Epic #5775, slice 01 / Story #5777)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Implements**: D2 · AC-1.3, AC-1.4, AC-1.6, AC-1.7, AC-1.8, AC-4.3

---

## Context

`CryptoService.Decrypt` catches `CryptographicException` and `FormatException` and returns the
ciphertext it was given (`CryptoService.cs:79-83`). Six call sites hand that return value straight to a
remote system: `PatAuthStrategy`, `JiraCloudBasicAuthStrategy`, `LinearApiKeyAuthStrategy`,
`ServiceNowBasicAuthStrategy`, `LinearWorkTrackingConnector:633`, and `OAuthService` at `:197`, `:238`,
`:240`, `:328`, `:329`. None of them can tell a secret from a ciphertext, so a wrong key becomes a 401
from Jira that reads as an expired token.

D2 deletes the fallback. That immediately raises the question this ADR answers: **who absorbs the
failure?** The naive shape — every one of those six call sites grows a try/catch — spreads crypto
knowledge across four auth strategies, a GraphQL client factory and an OAuth service, and gives six
independent chances to swallow it again.

There is a second requirement pulling in the opposite direction. The readability check (slice 04) and
the rotation (slice 03) must walk every stored secret and *classify* it without an exception per row.
A pass over a few hundred rows that throws for every unreadable one is a control-flow abuse and, worse,
would tempt a `catch` that reintroduces the original defect one level up.

So the port needs two shapes: one that refuses, and one that answers.

## Decision

**`ICryptoService` gains a total classifier alongside a `Decrypt` that now raises.**

```csharp
public interface ICryptoService
{
    string Encrypt(string plainText);                    // unchanged signature; writes an envelope
    string Decrypt(string storedValue);                  // NOW THROWS UnreadableSecretException
    SecretReadResult Read(string storedValue);            // total — never throws
}

public sealed record SecretReadResult(SecretState State, string? PlainText, string? KeyId);

public enum SecretState { Envelope, LegacyCbc, LegacyPlaintext, Unreadable }
```

**The six consumers change by zero lines.** They keep calling `Decrypt`. The exception travels the
failure path each of them already has — `ValidateConnection` turns it into a
`ConnectionValidationResult`, a background refresh turns it into the refresh-log entry the update
surface already renders. No auth strategy learns anything about cryptography, which is the property
worth defending in review.

**Classification order, and it is a total function with no `catch` anywhere in it:**

1. **Envelope** — the value starts `LH1.` and parses per ADR-146. Tag verifies under the named ring key
   → `Envelope`. Key id not in the ring, or tag fails → `Unreadable`. Both answers are reached by
   inspection; neither is reached by an exception filter.
2. **Structurally CBC-shaped** — not an envelope, is valid standard base64, and the decoded length is
   at least 32 bytes and a multiple of 16. Only such a value *can* be a legacy CBC blob, because a
   16-byte IV followed by PKCS7-padded blocks always is. Decrypt under each ring key with
   `PaddingMode.None`, then validate the PKCS7 trailer arithmetically and check the result is
   well-formed UTF-8 containing no control characters. Exactly one key yielding a valid result →
   `LegacyCbc`. None → `Unreadable`.
3. **Anything else** → `LegacyPlaintext`.

**The padding and printability checks are computed, not caught.** `PaddingMode.None` makes the unpad an
arithmetic test on the last byte, and UTF-8 validity is a decoder result, so AC-1.3's "explicit branch,
not a caught exception" holds through the whole classifier.

**A CBC-shaped value that no key reads is `Unreadable`, never `LegacyPlaintext`.** This is the rule that
prevents step 3 from resurrecting the deleted fallback under a new name. The cost is stated under
Consequences.

**The Connection detail surface computes readability on read.** `WorkTrackingSystemConnectionDto` gains a
per-option `secretState` derived from `Read`, and the Connection page renders "Secret cannot be read
with the current encryption key" against the offending field. Nothing is persisted, nothing is
projected, no event is published. **This is what satisfies AC-1.8 structurally**: a state shown on a
page is shown once, because it is a value, not a stream.

**The log side of AC-1.8 is deduplicated inside `CryptoService`.** One `Warning` per distinct stored
value per process lifetime, keyed on `SHA-256(storedValue)` in a bounded dictionary (cap 1000,
oldest-evicted). The hash is a dictionary key and is never logged. The line carries the state and the
key id and nothing else, per AC-1.9. Because the dedup key is derived from the argument the method
already has, **no call site passes an identity and no call site changes** — which is the whole reason
the dedup lives here and not in a caller.

## Alternatives Considered

**Change `Decrypt` to return `SecretReadResult` and have all six call sites handle it.** Type-forces
every consumer to confront the failure, which is normally the right instinct. **Rejected**: it puts
four auth strategies, a GraphQL client factory and the OAuth service in the business of deciding what an
unreadable credential means, when all six already have exactly one correct behaviour — fail the
operation — and already have a path that does it. It also converts a shared contract change into six
call-site changes and six new tests that assert the same thing, for no additional guarantee. The
compile-time forcing buys nothing here because the alternative is not "forget to handle it" but "an
exception propagates", which is already correct.

**Publish a `SecretBecameUnreadable` domain event and project a registry.** The project defaults to the
domain-event bus for cross-component facts, so this deserved a hearing. **Rejected**: readability is
derivable from the stored value in microseconds, so a projection would be a cache of something cheaper
than the cache. It adds a table, a migration, an invalidation question ("when does a row stop being
unreadable?") whose only honest answer is "when someone re-reads it", and a staleness window in which
the panel disagrees with the database. Deriving on read has none of those and satisfies AC-1.8 more
directly.

**Keep a narrow fallback for legacy plaintext only** — return the value as-is when it is not
envelope-shaped and not CBC-shaped. Superficially this is what the classifier does. **Rejected as a
`catch`**: implemented as an exception filter it is the original defect with a smaller blast radius, and
the blast radius is not the problem — invisibility is. Implemented as the positive structural test in
step 3, it is a branch a test can pin, which is what was decided.

## Consequences

**Positive**

- Six call sites, four of them one-method classes, stay free of cryptography. The whole of "what does an
  unreadable secret do?" is one component with one test surface.
- The classifier is a pure function of a string and a ring, so every one of AC-1.3 through AC-1.8 is
  answerable from a unit test with no database and no HTTP.
- The four states are the same four the readability report names (AC-4.3), so the panel, the rotation
  report and the reader share one vocabulary rather than three that drift.

**Negative / accepted**

- **A legacy plaintext secret that happens to be CBC-shaped is reported unreadable.** A 192-character
  alphanumeric Jira API token decodes to 144 bytes, which is ≥32 and a multiple of 16, so it enters
  step 2; no ring key reads it; it lands in `Unreadable` rather than `LegacyPlaintext`. The operator
  sees a named field on a named Connection and re-enters one token. This can only affect installs
  predating the introduction of `EncryptSecrets` — `OAuthCredential` has been encrypted since ADR-008,
  so the exposure is `WorkTrackingSystemConnectionOption.Value` alone. **Slice 01 owes a count of such
  rows on the `:5169` restored backup before the slice closes**; if the count is zero the residual is
  academic, and if it is not, the release note says so.
- Legacy CBC can never be *verified*, only decrypted plausibly. The printability test reduces a wrong
  read to roughly one in a thousand, not to zero, because CBC has no tag. The design's answer is not a
  better heuristic but a shorter exposure: every rotation moves rows off CBC permanently, and a CBC row
  is always reported as its own state and never as "verified".
- `ICryptoService` widens by one member. It is a shared contract with two implementations (the real one
  and the test fake), so per the project rule usages are grepped and the fake extended before the first
  consumer lands.

## Earned Trust — what is probed, not assumed

| Assumption | Probe |
|---|---|
| No `catch` remains in the read path | Structural test: the classifier's source contains no `catch` and no `when (ex is …)` clause |
| An unreadable secret never reaches a remote system | Gold test per auth strategy: a corrupted stored value → `ApplyAsync` raises and the `HttpRequestMessage` carries no `Authorization` header |
| One log line per secret, not per sync | Test: 50 consecutive decrypts of the same bad value → exactly one Warning |
| No key material, ciphertext or plaintext in any log | Test asserting the emitted line's structured properties are exactly `{State, KeyId}` |
| The dedup cache cannot grow without bound | Test: 5 000 distinct bad values → dictionary size stays at the cap |

## Cross-reference

- [ADR-146](./adr-146-secret-envelope-wire-format.md) — the format step 1 parses and why step 2 can
  never be reached by an envelope.
- [ADR-151](./adr-151-re-encryption-per-row-compare-and-swap.md) — the rotation, which consumes `Read`
  and never `Decrypt`, and which may only write over a state it verified.
- [ADR-006](./adr-006-connection-list-payload-shape.md) — the DTO this widens, and the one-route-one-shape
  rule that keeps the readability report off it.
