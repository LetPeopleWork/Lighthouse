# Epic 5775 — independent security review, findings

Run 2026-08-17 against `main` (shipped, unreleased), from the prompt in `security-review-brief.md`.
Read by path rather than by commit range; the parser behaviour in E1 was executed against the shipped
source rather than inferred. No code was changed by the review.

Finding ids are stable and are what the remediation slices cite. `E` findings are the epic's,
`W` findings are the wider sweep's.

## Verdicts on the brief's claims

| Claim | Verdict | Where |
|---|---|---|
| 1 · No key material escapes | **Does not hold** | E1 |
| 2 · The published key can never be the active key | **Does not hold** | E3 |
| 3 · Resolution precedence is one ordered list in one place | **Does not hold after boot** | E2 |
| 4 · Refuses rather than starting on a key it cannot use | Holds | — |
| 5 · Re-encryption never overwrites what it could not read, and is safe against a key change | **Half holds** | E4 |
| 6 · Only a System Administrator learns custody | Holds | see E6 for the guard |
| 7 · The chart cannot generate a key; file not env; not on the reloader watch | Holds | — |
| 8 · Lighthouse never writes to a Kubernetes Secret | Holds | — |
| 9 · A reload is fail-safe-old | Holds | — |

Claim 4: the readability probe degrades to "start" when the database will not answer. Deliberate,
documented, and the right way round. Claim 6: verified at runtime; `EncryptionController` is
`[Authorize]` + `SystemAdmin` at class level with no method opting out, and `SystemInfo.Encryption` is
cleared for anyone failing the same check and omitted from the wire rather than sent as null.
Claim 8: the only occurrences of `k8s.` or `KubernetesClient` in the backend are the literal strings
inside the ArchUnit test forbidding them, and the chart renders no ServiceAccount, Role or RoleBinding.

## Findings — the epic

### E1 · High · A malformed key ring writes the key itself into the log

`KeyRingSerializer.cs:102` → `KeyRingFileWatcher.cs:161`, and `Program.Main`'s catch (stderr +
`Log.Fatal`).

`TryParseEntry` splits an entry at the first colon and quotes the text before it back in full when it is
not a usable key id, with no length cap. Base64 key material is never a usable id, so any supplied value
that puts material before the first colon has that material quoted into the defect —which the watcher
writes as the structured property `{Defect}` and as the exception object.

Executed against the shipped parser. Three shapes leak, one does not:

```
multi-line file, second line carries an id      LEAKS: True
id and material written the wrong way round     LEAKS: True
trailing colon on a bare key                    LEAKS: True
"not a key ring" (the test's own input)         LEAKS: False
k-new:MAT1,k-old:MAT2 (control)                 parses
```

**Refutation attempts.** `.Trim()` trims only the ends, so an embedded newline survives into the quoted
id. There is no length cap. The comma-separated forms the documentation teaches are safe. The watcher's
own guard test, `ReadOnce_NothingItWrites_CarriesKeyMaterialInAnyEncoding`, feeds `"not a key ring"` —
no colon, so it never reaches the quoting branch and passes while the branch beside it leaks.

**On the ArchUnit rule.** It is not decorative: it would fail today on an injected `ILogger` in
`Services.Implementation.Encryption`. But it guards the wrong half of the seam — material leaves as an
exception message composed inside the guarded namespace and written by the one type deliberately placed
outside it. Placing `KeyRingFileWatcher` in `BackgroundServices` was right; the missing guarantee is
about the sentences handed to it.

### E2 · High · The file watcher overrides a configuration key that beat it at startup

`Program.cs:510-528` `WatchTheMountedKeysFile`; `KeyRingFileWatcher.cs:150-158` `Apply`.

Registration keys off `Encryption:KeysFile` being non-blank, never off whether the file answered the
resolution. `Apply` compares only against the ring in force. With both `Encryption__Key` and
`Encryption__KeysFile` set, the instance boots on configuration and moves to the file within thirty
seconds; everything written under either key in the wrong half of the cycle becomes unreadable, and a
restart reverses it. `WhatTheKeyArrivedIn` asks configuration first, so the panel names the setting that
is *not* in force.

**Refutation attempts.** No startup validation rejects both being set. Identical content is a no-op
(`EncryptionKeyRing.Equals` compares the key array). It is not silent — `Announce` logs at Warning. The
chart cannot reach it alone: it sets only `Encryption__KeysFile` and has no extra-env passthrough. The
reachable population is Compose and bare installs, and anyone migrating from an environment-variable key
to a file who leaves the old variable set.

### E3 · Medium · On one branch the published key *is* the active key, permanently

`EncryptionKeyRingBootstrapper.PublishedDefaultOnly` / `NoKeyAnywhereYet`; `CryptoService.Encrypt`.

The branch returns a one-key ring whose only entry is the published key, with custody `NoDurableStore`.
`Encrypt` writes under `ActiveKey` unconditionally, so every credential entered afterwards is protected
by a key that ships in every copy of the product. Reachable by any deployment resolving to
`DefaultLocationNoDurableStore` that already holds a secret — every Postgres install that upgrades
without setting a key, plus the "cannot tell" probe answer. `CanMint` is false there, so the panel
offers no Rotate and the administrator cannot act from the interface at all.

**Not a code finding.** The banner and panel disclose it honestly, and the wording is accurate. What is
wrong is the claim in the brief and an incompleteness in the configuration page, which says such an
instance "starts and keeps working" without adding that new credentials also go under the published key.

**Refutation attempts.** `RefuseWhenNothingStoredCanBeRead` does not catch it and should not — the
stored secrets read fine. `WithLegacyDefault()` does not demote it: `WithRetired` deduplicates on
equality and returns the same one-key ring. The frontend does not hide it.

### E4 · Medium · A pass pins the active key id once but encrypts fresh on every row

`SecretCustodyService.WalkAsync` / `WalkPastAsync`.

`activeKeyId` is read once and used for both the candidate filter and the report label;
`cryptoService.Encrypt` re-reads `Current.ActiveKey` per row. A mid-pass reload splits the results and
leaves the report's `ActiveKeyId` stale. The damaging part is the filter: rows excluded as already on
the *old* active key are never revisited, so if the new ring dropped that key they are unreadable and
unnamed.

**Refutation attempts.** `OneSecretPassAtATime` serialises passes against each other, not against the
watcher, which takes no gate. The minter and watcher cannot collide (a mounted file means custody is
never `GeneratedForThisInstance`). The compare-and-swap stops rows being clobbered but does nothing for
rows filtered out. Re-running heals it only while the old key is still on the ring.

### E5 · Medium · Two replicas sharing a key store can each mint and diverge

`GeneratedKeyRingStore.Write` / `Mint`; the single-writer argument in `OneSecretPassAtATime`.

Both replicas find no ring file, both mint, both write; the last `Move` wins and the loser holds a key
the file no longer names. The read-back-and-compare catches this only when the other move lands inside
the window between this replica's move and its read — a race, not a guarantee.

**Deferred rather than fixed** (maintainer decision, 2026-08-17): minting only happens under
`GeneratedForThisInstance`, and the chart — the one supported multi-replica topology — refuses to
install without an operator-supplied key. Reaching it needs an unsupported shared-key-store shape. The
gap is that this is nowhere written down. Slice 09 writes it down.

**Refutation attempts.** No lock file or advisory lock exists. Data Protection does not save it: both
replicas share the directory, so each can unwrap the other's ring, which enables the divergence rather
than preventing it.

### E6 · Medium · The "exactly this property set" test cannot see the property it was written for

`SecretCustodySeamArchUnitTest.SystemInfo_DisclosesExactlyThisPropertySetAndNothingAboutKeys`;
`Models/SystemInfo.cs`.

`SystemInfo.Encryption` is null-defaulted and carries `[JsonIgnore(WhenWritingNull)]`. The test builds
the record with twelve positional arguments, so the property serialises to nothing and never enters the
asserted set. The assertion passes against a list that does not mention it, and would pass identically
for a thirteenth field of the same shape — which is exactly the shape its docstring says it exists to
catch.

**Refutation attempts.** The runtime behaviour is correct and
`WithoutWhatOnlyAnAdministratorMaySee` does clear both fields, so this is a hole in the guard rather
than a live disclosure. `JsonSerializerOptions.Web` does not emit the null; the attribute suppresses it.

### E7 · Low · Lighthouse's own key-store files are world-readable

`GeneratedKeyRingStore.Write`; `Program.ResolveOrCreateProtectedOAuthStateSecret`.

Measured on three key stores on the development machine:

```
600  data-protection-keys/key-45ce4ceb-....xml          <- framework-written
644  data-protection-keys/encryption-keyring.protected  <- Lighthouse-written
644  data-protection-keys/oauth-state-secret.protected  <- Lighthouse-written
755  data-protection-keys/                              <- directory
```

Not a break on its own — both are Data-Protection-wrapped and the wrapping key is 0600 — but the
asymmetry is the point: three files in one directory, two of them weaker than the mode the framework
chose for the third.

**Refutation attempts.** Assumed `KeyStoreMigration.CopyAcross` would downgrade the 0600 when carrying a
legacy store across and checked it: `File.Copy` preserves the source mode on Unix, so it does not. That
half of the finding was withdrawn.

## Findings — the wider sweep

### W1 · Critical · An anonymous POST replaces the binaries and restarts the instance

`API/VersionController.cs:13` (`[AllowAnonymous]`, class level), `:65`
(`[HttpPost("installUpdate")]`), `LighthouseReleaseService.cs:126` `InstallUpdate`.

With no credentials, `POST /api/v1/version/installUpdate` makes the instance download the latest GitHub
release asset, overwrite its own installation directory, and call `processService.Exit(0)`.
`IsUpdateSupported()` excludes standalone, Docker and macOS — and returns true for Windows and Linux
server installs, the deployment the installation documentation describes.

Reachable consequences: remote shutdown at will, repeatable; forced version change, because
`InstallUpdate` never consults `UpdateAvailable()` and takes `allReleases.First()`; and an
unauthenticated write primitive into the install directory. It matters most on instances that enabled
authentication, since `[AllowAnonymous]` beats the `RequireAuthenticatedUser` fallback policy.

Introduced in `e57661da6` ("Blocked mode"), where the evident intent was to let the version *GETs*
answer before sign-in. `BlockedModeFilter` additionally puts `/api/latest/version` on its explicit
allow-list, so the endpoint is permitted in the lockdown state too.

**Refutation attempts.** `BlockedModeFilter` allows it by name. The rate limiter is only registered when
`RateLimiting:Enabled` and no policy is attached to the route. `[AllowAnonymous]` short-circuits the
fallback policy. The SPA-fallback middleware only handles requests that matched no endpoint. Gating in
the frontend is irrelevant — the API is called directly.

### W2 · Medium · The connection-string filter is a denylist that splits on a character the password may contain

`Services/Implementation/SystemInfoService.cs:14-17`, `:92-101`.

`GetSafeDatabaseConnection` splits on `;`, reads the text before the first `=` as the key, and drops the
part if that key is in a six-entry denylist. A password containing a semicolon —
`Password='p;ssw0rd'`, which Npgsql accepts — splits into a dropped `Password='p` and a kept
`ssw0rd'`, publishing the tail. Separately, Npgsql accepts `PSW` as a `Password` alias and the denylist
has `pwd` but not `psw`. `DatabaseConnection` is not withheld by
`WithoutWhatOnlyAnAdministratorMaySee`, so it reaches every signed-in caller, including an embed viewer.

**Refutation attempts.** The comparer is `OrdinalIgnoreCase`, so case is not the gap. The SQLite branch
does not share the problem: it parses through `SqliteConnectionStringBuilder` and returns only
`DataSource` — which is the shape the Postgres branch should adopt.

### W3 · Low · Every NuGet vulnerability advisory is exempted from the build gate

`Lighthouse.Backend/Directory.Build.props:24`.

`WarningsNotAsErrors` exempts NU1901–NU1904 for every project with no expiry, so an advisory disclosed
tomorrow against a shipped dependency leaves the build green. The intent is documented and defensible;
what is missing is any in-repository record of the "tracked separately" process it defers to. The
SSH.NET `NU1903` named in the brief is transitive and confined to the test project, so it is not in the
shipped product — it is a symptom of the mechanism rather than a finding.

## Paths explicitly exercised, including where nothing was found

- **Upgrading from the published key** — two outcomes. Durable key store: mints and keeps the published
  key behind it, correct. Non-durable with secrets present: E3.
- **A key removed while re-encryption runs** — E4.
- **A mounted file replaced mid-write** — nothing worth raising. A partial read either fails to parse
  (running ring stays) or parses as a truncated prefix, which always keeps the first entry, so the
  active key never changes. Kubernetes swaps a symlink atomically; this is a bind-mount concern that
  self-heals on the next tick.
- **Two replicas resolving a ring at once** — E5.
- **Anything reading a stored secret outside `CryptoService`** — **nothing does.** Every read goes
  through `ICryptoService.Decrypt` or `.Read`: the five auth strategies, `OAuthService`,
  `LinearWorkTrackingConnector`, `LighthouseAppContext`, `ConnectionSecrets`, `SecretCustodyService`.
  The two startup probes read raw column values over a bare `DbConnection` but classify through
  `SecretStateClassifier` and return only a verdict and a key id.
- **The envelope cryptography** — nothing found. AES-GCM, fresh 12-byte random nonce per encryption,
  16-byte tag, version and key id bound as associated data, `FixedTimeEquals` for the published-key
  check. The legacy CBC reader uses `PaddingMode.None` with its own PKCS#7 and printable-UTF-8 checks so
  a wrong key is a returned no rather than a caught exception.
- **Frontend** — nothing found. No `dangerouslySetInnerHTML` anywhere in `src/`; the panel renders
  through JSX, builds no URL from user input, and holds no key material.
- **CORS, forwarded headers, cookies** — nothing found. `EnsureCorsFailsClosed` refuses to start when
  authentication is enabled with empty `AllowedOrigins`, and `AllowAnyOrigin` cannot carry credentials.
  The embed cookie is re-resolved against the user profile per request.
- **Other unauthenticated routes** — `AuthController`, the three embed controllers and the OAuth
  callback are all deliberately anonymous and correct for what they do. `VersionController` is the
  exception (W1).

## Remediation

| Finding | Disposition |
|---|---|
| E1, E6, E7 | Slice 07 — Story #5794, *a refusal that cannot quote the key* |
| E2 | Slice 08 — Story #5795, *the key that won at startup is the key that stays* |
| E4 | Slice 09 — Story #5796, *a pass that survives the ring changing under it* |
| E5 | Slice 09 (#5796), documented rather than built |
| E3 | Story #5781 (slice 06), AC-6.19 and AC-6.20 — documentation, plus a release-note line |
| W1 | Bug #5797 — outside the epic surface, parented to it because the review found it |
| W2 | Bug #5798 — same |
| W3 | Maintainer decision, no work item |

Maintainer decision 2026-08-17: **all of E1–E7 and W1–W2 block the release.**

## Not re-litigated

Per the brief: the compiled-in published key, the 0444 mount mode, `helm lint` not enforcing
`required`, the chart refusing rather than generating, the absent advisory, and the embed nonce. The
review found no reason to reopen any of them. The `IStoredSecretSummary` merge is a design question with
no security dimension.
