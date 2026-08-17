# Slice 07 — A refusal that cannot quote the key

**Feature**: epic-5775-secret-encryption-key-custody · **ADO**: Epic #5775 → Story #5794 · **Estimate**: ~4h
**Origin**: not from DISCUSS. Found by the independent security review of 2026-08-17
(`verification/security-review-findings.md`, findings E1, E6 and E7; prompt in `security-review-brief.md`).
**Ordering**: first of the three remediation slices, and it blocks the release. The other two cost an
operator their credentials; this one costs them their key.

## Goal

No sentence Lighthouse writes about a key ring can carry the key, and the two tests that exist to
guarantee that can both see the thing they were written to catch.

## The defect

`KeyRingSerializer.TryParseEntry` splits an entry at the first colon and treats everything before it
as the key id. When that text is not a usable id, the refusal quotes it back in full, with no length
cap. Base64 key material is never a usable id — it carries uppercase letters and `+/=`, and runs to 44
characters against a limit of 32 — so **any supplied value that puts material before the first colon
has that material quoted into the defect**. `KeyRingFileWatcher` then writes that defect out as a
structured property and as the exception object, so it reaches every sink twice; on the configuration
and mounted-file startup paths the same string is raised, caught in `Program.Main`, and written to
stderr and to `Log.Fatal`.

Three shapes reach it, all of them ordinary mistakes rather than contrivances: a keys file written one
key per line where a later line carries an `id:`, which the source comment on `MountedFileKeyRingSource`
actively invites; the documented `id:key` order written the wrong way round; and a trailing colon left
on a bare key. Confirmed by running the shipped parser, not by reading it.

In Docker and Kubernetes that line goes to stdout and into whatever the cluster collects with — a place
with a far wider readership than the key store this epic exists to protect. The active key then has to
be treated as compromised, and under mounted-file custody Lighthouse cannot rotate it itself.

The reason nothing caught this is the second half of the slice. Two tests were written to make it
impossible:

- `KeyRingFileWatcherTests.ReadOnce_NothingItWrites_CarriesKeyMaterialInAnyEncoding` feeds the watcher
  the literal `"not a key ring"`. That string contains no colon, so it never reaches the quoting branch
  at all. The test passes against the one input that cannot fail.
- `SecretCustodySeamArchUnitTest.SystemInfo_DisclosesExactlyThisPropertySetAndNothingAboutKeys` builds a
  `SystemInfo` with twelve positional arguments and leaves `Encryption` at its null default. That
  property carries `[JsonIgnore(WhenWritingNull)]`, so it serialises to nothing and never appears in the
  set the test asserts against. Its own docstring says it exists because "the way key state would arrive
  is somebody adding a convenient 'which key is active' line for support" — which is precisely the shape
  it cannot see, and precisely the shape the last person to add key state to that response used.

The ArchUnit rule that bars `Services.Implementation.Encryption` from depending on
`Microsoft.Extensions.Logging` is **not** decorative — it would fail today on an injected `ILogger`.
But it guards the wrong half of the seam. Key material does not need a logger inside that namespace to
escape; it leaves as an exception message composed inside the namespace and written by the one type
deliberately placed outside it.

## IN scope

- **A defect message never repeats what was supplied.** Name the entry by position, say what is wrong
  with it, and stop. Where naming the offending text genuinely helps an operator — a mistyped key id is
  the only case — it is quoted at a fixed short length, so a 44-character value cannot arrive whole.
  The comment above `TryParseEntry` already claims this property; this slice makes the claim true.
- **The watcher's key-material test is fed content that reaches the quoting branch**, including all
  three shapes above. A test whose only malformed input cannot fail is worse than no test, because it
  is read as coverage.
- **The system-information property-set test asserts against the record's declared properties** rather
  than against one serialised instance, so a property added with a null default and omitted-when-null
  is inside the assertion rather than invisible to it.
- **The two files Lighthouse writes into the key store are created with an explicit file mode.** The
  Data Protection key beside them is 0600 because the framework sets it; `encryption-keyring.protected`
  and `oauth-state-secret.protected` are 0644 because nothing does. Measured on three separate key
  stores, not inferred from the umask.

## OUT of scope

- Moving `KeyRingFileWatcher` back inside the guarded namespace. It logs because it runs on a timer and
  has no caller to hand a sentence to, and that reasoning still holds; what was missing is a guarantee
  about the sentences handed to it, which is what this slice supplies.
- A rule that forbids exception messages from carrying supplied text generally. Worth wanting, and too
  large to fit here without turning a four-hour fix into a refactor of every parser in the backend.
- The precedence defect (slice 08) and the re-encryption pass (slice 09).

## Learning hypothesis

**Disproves** "the seam is guarded by the namespace rule" if writing the fixed-length quoting turns up
further places where a sentence built inside `Services.Implementation.Encryption` is handed to a caller
that writes it down. `GeneratedKeyRingStore` and `MountedFileKeyRingSource` both compose refusals
naming a path; those are safe, but they are the same pattern and are worth reading in the same pass.

**Confirms**, if it holds, that the right guard for this seam is a property of the sentences rather than
a property of the namespace — which would say the ArchUnit rule should eventually be replaced rather
than extended.

## Acceptance criteria

- **AC-7.1** — A supplied ring whose first entry puts key material before a colon is refused, and no
  rendered message, structured log property or exception message anywhere on that path contains the
  material in base64, hex, or any other encoding. Asserted for all three shapes, through the watcher and
  through the configuration bootstrap.
- **AC-7.2** — A genuinely mistyped key id is still named in the refusal, so the operator can find it,
  and the naming is bounded so that no value long enough to be key material can arrive whole.
- **AC-7.3** — The watcher's key-material test fails if the quoting is reintroduced. Verified by
  reverting the fix locally and watching it go red, not by reading it.
- **AC-7.4** — Adding a nullable, omitted-when-null property to `SystemInfo` fails the property-set
  test until it is added to the list deliberately.
- **AC-7.5** — `encryption-keyring.protected` and `oauth-state-secret.protected` are created 0600 on
  a fresh key store, and a key store carried across by `KeyStoreMigration` keeps the modes it had.

## Dependencies

Slices 01–06b. No new data, no migration, no configuration name. `KeyStoreMigration.CopyAcross` already
preserves the source mode on Unix — that half of the finding was withdrawn on checking — so the file
mode work is confined to the two creation sites.

## Dogfood moment

Same day: mount a keys file written one key per line on a local instance, watch the reload reject it,
and read the whole log — including structured properties, not only the rendered sentence — for the key.
Then repeat with the fixed build.

## Pre-slice SPIKE

None.

## Verdict

_To be recorded at slice close: confirmed / disproved._
