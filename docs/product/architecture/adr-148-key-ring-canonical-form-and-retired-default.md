# ADR-148: One canonical ring string, three transports; the published default is a compiled-in retired key

**Status**: Accepted
**Date**: 2026-08-14
**Feature**: `epic-5775-secret-encryption-key-custody` (ADO Epic #5775, slices 02 and 05 / Stories #5024, #5780)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Implements**: D3, D4 · AC-2.1, AC-2.3, AC-2.4, AC-2.5, AC-2.6, AC-2.9, AC-3.9, AC-5.9

---

## Context

D3 replaces one key with a ring: one active key for writes, any number of retired keys for reads, each
with an id. The ring has to arrive from three different places — a generated file beside the database,
operator configuration, and a Kubernetes Secret an external store owns — and every one of them has to
express the same thing. If each transport gets its own shape, the parser gets three code paths and the
documentation gets three sections that will disagree within a release.

Two constraints narrow the choice sharply.

**The environment-variable provider cannot bind a list from a scalar.** `Program.cs:374-386` already
carries the scar: `Authentication__AllowedOrigins` had to grow a scalar-recovery branch because
operators set the flat form and .NET's environment provider only binds lists through `__0`, `__1`
indices. Asking an operator — or an External Secrets template, or a `stringData` block — to emit
`Encryption__Keys__0__Id`, `Encryption__Keys__0__Key`, `Encryption__Keys__1__Id` … reproduces exactly
that failure, in a place where getting it wrong means unreadable credentials rather than a CORS error.

**AC-2.4 removes the published default key from `appsettings.json`, and AC-2.5 requires every secret
written under it to stay readable.** Those two are only compatible if the default arrives from
somewhere other than configuration.

## Decision

**One canonical text form, used by every transport:**

```
<keyId>:<base64-32-bytes>[,<keyId>:<base64-32-bytes>]*
```

**Position is state: the first entry is the active key, every later entry is retired.** No `state`
token, no ordering field, no way to express two active keys or none. The invariant that made the
readability report possible is enforced by the grammar rather than by a validator.

Key ids match `[a-z0-9-]{1,32}` (ADR-146's charset). Minted ids are `k-YYYY-MM-DD-NN`, matching the
DISCUSS examples. Whitespace around entries is trimmed so a Secret can be written across lines.

**Three transports, one parser:**

| Custody | Transport | Notes |
|---|---|---|
| Generated for this instance | `encryption-keyring.protected` in the resolved key store directory — the canonical string, wrapped with ASP.NET Data Protection exactly as `oauth-state-secret.protected` is | Written temp-then-`File.Move` so an interrupted rotation cannot leave a truncated ring |
| Supplied by configuration | `Encryption:Keys` (ring) or `Encryption:Key` (one key) | The scalar is sugar for a one-entry ring |
| Supplied by external secret | A file whose whole content is the canonical string, path in `Encryption:KeysFile` | A mounted Kubernetes Secret key; see ADR-153 |

**A configured single key gets a derived id.** `Encryption:Key` carries material and no id, so the id is
`k-cfg-` plus the first eight hex characters of `SHA-256(material)`. It must be **derived, not random**:
two pods of the same Deployment, and the same pod after a restart, have to label envelopes identically
or a value written by one is unattributable to the other. Eight hex characters of a hash of the key are
disclosed alongside the ciphertext; that is 32 bits of a preimage-resistant digest and buys an attacker
nothing, and it is stated here so it is not discovered in review.

**The published default is a compiled-in retired entry.** `k-legacy-default` carries the material
currently at `appsettings.json:43-44` as a `const` in the crypto assembly, appended to every resolved
ring as the last (therefore retired) entry. It is never eligible to be active, in any custody mode. It
is dropped from the ring when no stored value references it, which is what AC-3.9 measures.

Two things follow that are worth being explicit about. The constant will be flagged by SonarQube as a
hard-coded credential; it takes a **narrow inline suppression at the declaration** whose justification
says, in plain language, that this key is already public, that it exists only to keep secrets written
before this release readable, and that it can never encrypt anything. That is the one place CLAUDE.md
permits a reference-free rationale in code, and it belongs there because that is where a reviewer meets
it. Second, an operator who wants to prove no row is on it can set `Encryption:AllowLegacyDefault=false`
and see the affected secrets reported unreadable rather than silently working.

**Validation is total and happens once, at bootstrap.** An entry whose material is not 32 bytes of
base64, a duplicate id, an empty ring, or an id outside the charset stops startup with a message naming
*which entry* and *what is wrong with it* — never the material (AC-2.9). Data-Protection unwrap failure
on the generated file stops startup and names the file; it never mints a replacement (D10, AC-2.7).

## Alternatives Considered

**Indexed configuration binding** — `Encryption:Keys:0:{Id,Key}`. Idiomatic .NET, schema-checkable,
and what `IConfiguration.Bind` is for. **Rejected** on the `AllowedOrigins` precedent: the environment
provider's indexed form is the documented way operators get this wrong, and here the punishment is
credentials nobody can read. A Kubernetes Secret written by External Secrets templating is also
markedly harder to author as six indexed keys than as one line, and the ESO template is the artifact
most likely to be hand-edited under time pressure during an incident.

**JSON in one variable** — `Encryption__Keys={"keys":[{"id":…,"key":…}]}`. One scalar, so it dodges the
indexing trap, and it is self-describing. **Rejected**: quoting JSON inside YAML `stringData` inside an
ESO template is three layers of escaping over a value where a mis-escape is silent, and it re-opens the
"which one is active?" question as a field that can be set to two keys or to none. The positional
grammar cannot express an invalid ring.

**Separate active and retired settings** — `Encryption:ActiveKey` plus `Encryption:RetiredKeys`. Reads
well. **Rejected** because the Kubernetes rotation flow (AC-5.10) is *one edit that promotes a key*, and
under this shape it is two edits that must not be interleaved. An operator who adds to `RetiredKeys`
before changing `ActiveKey` is briefly correct; one who does it the other way round has a window where
the old key is in neither setting and every existing secret is unreadable.

**Store the ring in the database.** Removes the filesystem question entirely. **Rejected outright**: a
key in the database it encrypts is not encryption at rest, it is obfuscation, and the leaked-backup
scenario this epic exists for is exactly the one it fails.

## Consequences

**Positive**

- One grammar, one parser, one documentation section, three transports. The Kubernetes rotation
  procedure is a one-line edit an operator can perform correctly under pressure.
- "Exactly one active key" is unrepresentable-otherwise rather than validated, so no code path can
  produce a ring with two.
- The upgrade is invisible without a data migration: the legacy default is present from the first boot
  of the new version, so every existing secret reads, and every new write goes to the instance's own
  key because the legacy entry can never be active.
- Removing the literal from `appsettings.json` (AC-2.4) no longer conflicts with keeping old secrets
  readable (AC-2.5), because the two facts now live in different places.

**Negative / accepted**

- The published key remains in the shipped binary. It is already public — that is the defect — and
  removing it would orphan every secret written before this release. It leaves the ring the moment a
  rotation removes the last row referencing it.
- A hand-written ring string can be mistyped in a way that parses. Mitigated by validating every entry
  at bootstrap and by the startup line listing the ids it resolved, which is the operator's read-back.
- The derived config key id leaks 32 bits of `SHA-256(key)`. Stated, accepted, no mitigation offered
  because none is needed and a random id would break multi-pod attribution.

## Earned Trust — what is probed, not assumed

| Assumption | Probe |
|---|---|
| Data Protection can unwrap what it wrapped, on the filesystem in front of it | After writing the ring file, the bootstrapper re-reads and unprotects it and compares byte-for-byte before declaring success. Docker overlayfs and WSL2 DrvFs are catalogued liars about durability; a write that cannot be read back must fail startup, not be assumed |
| A rotation cannot leave a half-written ring | Test: kill the process between `File.Move` and activation → next boot resolves the previous ring intact |
| Every legacy secret still reads after the default leaves `appsettings.json` | Integration test against the `:5169` restored backup: readability check before upgrade and after → identical counts, zero unreadable |
| An invalid ring stops startup and says why | Table test over: 31-byte key, non-base64, duplicate id, empty string, uppercase id, id with a `.` — each names the entry and never the material |
| The legacy default can never become active | Test: rotate on an instance holding only the legacy default → the minted key is active and `k-legacy-default` is retired, never the reverse |

## Cross-reference

- [ADR-146](./adr-146-secret-envelope-wire-format.md) — the key-id charset this grammar depends on.
- [ADR-149](./adr-149-key-store-beside-the-database.md) — where the generated ring file lives.
- [ADR-150](./adr-150-key-ring-resolved-at-builder-time-into-a-singleton.md) — when it is parsed.
- [ADR-153](./adr-153-kubernetes-key-custody-is-operator-supplied.md) — the mounted-file transport.
