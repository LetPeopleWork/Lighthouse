# Mutation testing — 5641 (embed session: exchange an API key for a framed Lighthouse session)

Run 2026-08-04 against `main` @ `6fb1ea02a`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **90.91 %** | 165 | 150 | 15 | 0 | 10 m 16 s |
| Frontend (StrykerJS) | *N/A* | — | — | — | — | — |

**Frontend is N/A, not skipped**: slice 02a is backend-only (D37 — "no frontend change in slices
02–03"). `git show --stat 7b8ae6bf5 6fb1ea02a 39fad8575` touches zero files under
`Lighthouse.Frontend/`, so there is nothing for StrykerJS to mutate.

Config: `stryker.5641.backend.json` (unchanged between the two runs below — the scope was not
widened or narrowed to move the number).

## Backend

### Before and after

A first run on 2026-08-04 scored **62.42 %** (103 killed, 48 survived, 14 with no test reaching them
at all). This pass added tests only — **no production file was modified**, which `git status` on the
`Lighthouse.Backend/Lighthouse.Backend/` tree confirms.

| file | tested | killed before | killed after | score before | score after |
| --- | --- | --- | --- | --- | --- |
| `Services/Implementation/Auth/ApiKeyIdentityResolver.cs` | 16 | 3 | 16 | 18.8 % | **100 %** |
| `Models/Auth/EmbedSessionRefusal.cs` | 2 | 0 | 2 | 0 % | **100 %** |
| `Models/Auth/EmbedSessionToken.cs` | 2 | 0 | 2 | 0 % | **100 %** |
| `Models/Auth/EmbedSessionTokenMintResult.cs` | 1 | 0 | 1 | 0 % | **100 %** |
| `Models/Auth/EmbedSessionTokenRedemption.cs` | 1 | 0 | 1 | 0 % | **100 %** |
| `Models/Auth/EmbedSessionTokenResponse.cs` | 2 | 0 | 2 | 0 % | **100 %** |
| `Services/Implementation/Auth/ApiKeyPrincipalFactory.cs` | 17 | 10 | 17 | 58.8 % | **100 %** |
| `API/EmbedSessionController.cs` | 25 | 18 | 25 | 72.0 % | **100 %** |
| `Services/Implementation/Repositories/EmbedSessionTokenRepository.cs` | 22 | 18 | 22 | 81.8 % | **100 %** |
| `Services/Implementation/Auth/SmartAuthSchemeSelector.cs` | 12 | 10 | 12 | 83.3 % | **100 %** |
| `API/EmbedEntryController.cs` | 23 | 17 | 19 | 73.9 % | 82.6 % |
| `Services/Implementation/Auth/EmbedSessionTokenService.cs` | 42 | 27 | 31 | 64.3 % | 73.8 % |
| **total** | **165** | **103** | **150** | **62.42 %** | **90.91 %** |

`Configuration/EmbedConfiguration.cs` is in `mutate` but yields no mutants — it is two `int`
properties with literal defaults and no logic.

Zero mutants are left with no coverage. Ten of the twelve files with logic are at 100 %; the two
that are not consist entirely of accepted survivors listed below.

### Closed by this pass

New file `Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyIdentityResolverTest.cs` —
the owner-resolution path had **no test at all** (9 no-coverage mutants). Seven scenarios, all
about who a redeemed token turns into:

| scenario | mutant it kills |
| --- | --- |
| the key was deleted between mint and redemption → resolves nothing | `if (apiKey is null)` block removal |
| a key with no owner at all → `Unlinked`, still `IsValid`, still names the key | the `Unlinked` result initializer and its `IsValid` |
| owner linked by profile id → carries subject, display name, email | `!OwnerUserProfileId.HasValue`, `byId is null`, the `return byId` block, `IsValid` |
| owner linked by **subject only**, no profile id → resolves | all four `IsNullOrWhiteSpace(OwnerSubject)` mutants |
| the profile id dangles → falls back to the subject | `byId is null` from the other direction |
| the subject matches no profile → `Unlinked`, does not throw | `SingleOrDefault()` → `Single()` |
| a blank owner subject (`null`, `""`, `"   "`) never matches a profile | the `return null` block for a blank subject |

New file `Lighthouse.Backend.Tests/Services/Implementation/Repositories/EmbedSessionTokenRepositoryTest.cs`
— the two conditional statements D27 and D31 rest on, asserted at the **exact instant** rather than
at whatever the wall clock happened to be during an HTTP test:

| scenario | mutant it kills |
| --- | --- |
| redemption at exactly `ExpiresAt` marks nothing, one second earlier marks one row | `ExpiresAt > redeemedAt` → `>=` |
| already-redeemed / revoked / expired each mark nothing, outstanding marks one | pins the whole `TryMarkRedeemed` predicate at the layer that owns it |
| prune deletes a token expiring exactly now, a redeemed-but-unexpired one, and a revoked-but-unexpired one — and keeps the outstanding one | `ExpiresAt <= now` → `<`, and **both** `\|\|` → `&&` in the prune predicate |
| revoke-all leaves other keys' and already-spent tokens alone | the `RevokeOutstanding` predicate |

New file `Lighthouse.Backend.Tests/Models/Auth/EmbedSessionContractShapeTest.cs` — the five DTOs had
**zero** killed mutants; every string default and the `Refused` singleton were unasserted:

| scenario | mutant it kills |
| --- | --- |
| a mint response serializes as `token` / `expiresAt` / `embedUrl`, with empty strings when unset | both `= string.Empty` in `EmbedSessionTokenResponse` |
| a refusal serializes as `reason` / `message`, empty when unset | both `= string.Empty` in `EmbedSessionRefusal` |
| an unset mint result and an unset token entity carry empty identifiers, not a placeholder | `EmbedSessionTokenMintResult.Token`, `EmbedSessionToken.TokenId` / `SecretHash` |
| `EmbedSessionTokenRedemption.Refused` is not successful and names no key | `new(false, 0)` → `new(true, 0)` — a truthy `Refused` signs **everyone** in |

`ApiKeyPrincipalFactoryTest.cs` — five scenarios added:

| scenario | mutant it kills |
| --- | --- |
| `Create(null, scheme)` throws `ArgumentNullException` | `ArgumentNullException.ThrowIfNull` removal |
| an **unlinked** key that still carries a stale subject emits no `sub` | `state != Resolved \|\| IsNullOrWhiteSpace(sub)` → `&&` |
| a **resolved** owner with a blank subject emits no `sub` | the same guard from the other direction |
| a resolved owner's display name becomes the `name` claim | the `!` on `IsNullOrWhiteSpace(displayName)`, and the `claims.Add` statement |
| a `null` / `""` / `"   "` display name emits **no** `name` claim | the three `IsNullOrWhiteSpace(displayName)` string rewrites |

`SmartAuthSchemeSelectorTest.cs` — the `Select(HttpRequest)` overload (ADR-130) had no test; the
conditional that distinguishes the two cookies was `(true ? Embed : Cookie)`-mutable with nothing
noticing. Five scenarios, pinning **both** directions:

| scenario | mutant it kills |
| --- | --- |
| `Select((HttpRequest)null)` throws `ArgumentNullException` | `ArgumentNullException.ThrowIfNull` removal |
| a request bearing `.Lighthouse.Embed` selects the embed scheme | — (the positive direction alone cannot kill a `true?` mutation) |
| a request bearing `.Lighthouse.Session` selects the **ordinary** cookie scheme | `(true ? EmbedCookieScheme : CookieScheme)` |
| a request with no cookie at all selects the ordinary cookie scheme | the same, redundantly |
| `X-Api-Key` alongside an embed cookie still selects the API-key scheme | the header-first ordering |

`S7_EmbedSessionTokenExchangeTests.cs` — four scenarios, plus the refusal body strengthened:

| scenario | mutant it kills |
| --- | --- |
| the unlinked-owner refusal carries `reason: "api_key_owner_unlinked"` and a message naming both cause and fix | the `EmbedSessionRefusal` initializer and all three of its strings (D30) |
| a signed-in user with no `api_key_id` cannot mint, while the key can | `if (!TryGetApiKeyId(...))` block removal in `MintSessionToken` |
| a signed-in user with no `api_key_id` cannot revoke | the same block in `RevokeAllSessionTokens` — without it the caller falls through to key `0` |
| revoke-all is absent with authentication disabled, while enabled it returns 204 | `if (!IsEmbedSurfaceAvailable())` block removal in `RevokeAllSessionTokens` |
| minting prunes what is already spent — one row before and one row after a second mint | `await repository.PruneSpentAsync(now, ...)` statement removal |

`S8_EmbedEntryPointTests.cs` — three scenarios:

| scenario | mutant it kills |
| --- | --- |
| a genuine token id with a **wrong secret** is refused, **and the real token stays spendable** | `if (stored is null \|\| !SecretMatches(...))` block removal — under it the wrong secret redeems, and the attempt spends the token |
| the owner unlinked **after** the token was minted → 401, no cookie | `identity is null \|\| state != Resolved` → `&&` (D30) |
| `Embed:TokenLifetimeSeconds` of `0` or `-30` falls back to the 60-second default | `configured > 0` → `>= 0`, and the whole conditional → `true` |

`S9_EmbedCookiePolicyTests.cs` — one scenario:

| scenario | mutant it kills |
| --- | --- |
| the embed `Set-Cookie` carries no `expires=` and no `max-age=` | `IsPersistent = false` → `true` (D40 — a persistent cookie outlives the browser session and widens the revocation gap past the 30 minutes S6 settled on) |

`TestHelpers/EmbedSessionTestHost.cs` gained two helpers the above rest on: `ReadEmbedSessionTokens()`
(a scoped read of the token table, for the prune assertion) and `PostAsSignedInUserAsync()` (a
principal authenticated through `TestAuthHandler`, i.e. signed in but carrying no `api_key_id`).

### Accepted survivors

15 survivors, in two groups. None is a missing test; each is named with its reason.

**Log-only (8).** Removing the call, or blanking its message template, changes nothing an operator
or a caller can observe. Asserting on log text would pin prose, not behaviour.

| file:line | mutant | reason |
| --- | --- | --- |
| `EmbedEntryController.cs:62` | statement removal | drops the `LogWarning` for an unlinked owner; the 401 and the absent cookie are asserted |
| `EmbedEntryController.cs:63` | string → `""` | that warning's message template |
| `EmbedSessionTokenService.cs:42` | statement removal + string | the `LogDebug` on mint |
| `EmbedSessionTokenService.cs:69` | statement removal + string | the `LogWarning` on a refused redemption |
| `EmbedSessionTokenService.cs:83` | statement removal + string | the `LogInformation` on revoke-all |

**Equivalent (7).** The mutation cannot change observable behaviour, because a guard further down
produces the identical outcome.

| file:line | mutant | why it is equivalent |
| --- | --- | --- |
| `EmbedEntryController.cs:55` | `{ return Refuse(); }` block removal | a refused redemption reports `ApiKeyId = 0`; the very next guard resolves key `0`, gets `null`, and returns the same 401 with the same HTML and no cookie. The two guards are defence in depth, and the second subsumes the first. Rows never start at id 0 — SQLite and Postgres both begin at 1 |
| `EmbedEntryController.cs:72` | `new AuthenticationProperties { }` initializer removal | `IsPersistent` reads `false` whether the item is absent or explicitly `false`, so the cookie is a session cookie either way. The *value* mutation on the same line (`IsPersistent = true`) **is** killed by the new S9 test, so D40 itself is pinned; only the redundant explicit `false` survives |
| `EmbedSessionTokenService.cs:54` | `{ return Refused; }` block removal | `TrySplit` leaves `tokenId` empty on failure, so the fall-through looks the token up as `""`, finds nothing, and refuses at the next guard — the same 401 |
| `EmbedSessionTokenService.cs:94` and `:95` | `tokenId`/`secret` `= string.Empty` → placeholder | these are `out` parameters on `TrySplit`'s *failure* path. The single caller checks the `bool` before reading them, and even under the `:54` mutant above a placeholder token id still matches no row. Killing them would require asserting on a private helper's out-parameters — an implementation detail, not a behaviour |
| `EmbedSessionTokenService.cs:99` and `:105` | `return false` → `return true` in `TrySplit` | same reason: a blank or malformed token yields an empty `tokenId`, the lookup finds nothing, and the redemption is refused with the identical 401. `TokenId` is always a 16-byte URL-safe value, so no stored row can ever match `""` |

The two files below 100 % consist *entirely* of these: `EmbedEntryController.cs` at 82.6 % is 2
log mutants + 2 equivalents, and `EmbedSessionTokenService.cs` at 73.8 % is 6 log mutants + 5
equivalents. Excluding accepted survivors, every mutated file is at 100 %.

### Not mutated

Four files the slice touched are **deliberately absent** from `mutate`. Stryker.NET ignores line
ranges — mutating a whole file to reach a small addition buries the change's score under hundreds
of mutants of untouched code, and would have inflated the denominator without testing anything new.
Each is covered another way:

| file | what the slice added | how it is covered instead |
| --- | --- | --- |
| `Program.cs` (+40 lines of ~700) | registration of the `LighthouseEmbedCookie` scheme and its cookie options — name, `SameSite=None`, `Secure`, `Partitioned`, `ExpireTimeSpan`, `SlidingExpiration` | `S9_EmbedCookiePolicyTests` reads the registered `CookieAuthenticationOptions` out of the container by scheme name and asserts all of them, **and** asserts the literal `Set-Cookie` header on a live response — because the framework does not validate `CookieBuilder.Extensions`, so the call site alone would not prove `Partitioned` reaches the wire. `S9_OrdinarySessionCookie_StillSameSiteLaxAndUnpartitioned` pins the blast radius from the other side |
| `Data/LighthouseAppContext.cs` (+20 lines of ~400) | the `EmbedSessionTokens` `DbSet` and its entity configuration, including the cascade delete from `ApiKey` | `EmbedSessionTokenRepositoryTest` runs against real SQLite through `IntegrationTestBase`; the mapping is exercised on every insert, update and delete. The foreign key is not theoretical — the first draft of that fixture failed with `SQLite Error 19: FOREIGN KEY constraint failed` until the owning API keys were seeded |
| `Configuration/RateLimitingConfiguration.cs` (+1 line) | the `EmbedSession` policy name constant | `S7_Exchange_ExceedsRateLimit_Throttled_WithRetryAfter` configures that policy by name through `EmbedSessionTestHost.WithEmbedRateLimit` and drives the real limiter until it throttles, asserting both the 429 and a parseable `Retry-After` |
| `Services/Implementation/Auth/ApiKeyAuthenticationHandler.cs` (−25/+2 lines) | the handler stopped building claims inline and now calls `ApiKeyPrincipalFactory` (D29) | the factory itself is mutated and now at **100 %**. `ApiKeyPrincipalFactoryTest.Create_SameValidationResult_ProducesTheSameClaimsForBothAuthenticationPaths` pins parity, and the four `S10_EmbedScopeEquivalenceTests` compare what the header-borne and cookie-borne principals actually *reach* — in scope, out of scope, on write, and in the effective-permission summary the SPA gates its admin UI on |

## Production defects exposed

None. Every survivor triaged to a missing test or an equivalent mutant; no survivor revealed
behaviour that differs from what D25–D44 specify. The one that came closest is
`EmbedSessionTokenService.cs:60` — under block removal a token id presented with the **wrong
secret** redeemed successfully, which would have been a real vulnerability had the guard ever been
weakened. It is now pinned by
`S8_Enter_GenuineTokenIdWithAWrongSecret_Refused_AndLeavesTheRealTokenSpendable`, which asserts both
halves: the forged attempt is refused, *and* it does not consume the genuine token.

## Verification

`dotnet build` — 0 warnings, 0 errors. `dotnet test` over the mutation scope — 169 passing, 0
failing, 0 skipped.

Two of the new assertions were sabotage-verified before the run, per the ledger rule that an
assertion whose failure you have not seen proves nothing: `configured > 0` was temporarily changed
to `>= 0` and the entry-point guard's `||` to `&&`; both new tests went red, and both mutations were
reverted before the run started.
