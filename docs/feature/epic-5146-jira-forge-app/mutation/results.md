# Mutation testing — 5692 (viewer-identity embed session, slice 01)

Run 2026-08-06 against `main` @ `8e2846f43`. Gate is 80 % kill rate.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **88.46 %** | 208 | 184 | 19 | 5 | 0 | 12 m 03 s |
| Frontend (StrykerJS) | *N/A* | — | — | — | — | — | — |

**Frontend is N/A, not skipped**: slice 01 is backend-only — three hops of a server-side sign-in
flow plus the cookie they issue. Nothing under `Lighthouse.Frontend/` changed.

Config: `stryker.5692.backend.json`, byte-identical between the two runs below. Neither the `mutate`
list nor the `test-case-filter` was touched — the score moved because the tests did.

## Backend

### Before and after

A first run at 16:17 scored **76.44 %**. This pass added tests only; `git diff --stat --
Lighthouse.Backend/Lighthouse.Backend/` is empty.

| file | tested | killed before | killed after | score before | score after |
| --- | --- | --- | --- | --- | --- |
| `Services/Implementation/Auth/ApiKeyPrincipalFactory.cs` | 25 | 18 | 25 | 72.0 % | **100 %** |
| `Models/Auth/EmbedSessionTokenRedemption.cs` | 1 | 1 | 1 | 100 % | **100 %** |
| `Services/Implementation/Repositories/UserProfileLookup.cs` | 1 | 1 | 1 | 100 % | **100 %** |
| `API/EmbedStartController.cs` | 37 | 27 | 35 | 73.0 % | 94.6 % |
| `Services/Implementation/Repositories/EmbedSessionTokenRepository.cs` | 26 | 24 | 24 | 92.3 % | 92.3 % |
| `Services/Implementation/Auth/EmbedSessionTokenService.cs` | 79 | 57 | 67 | 72.2 % | 84.8 % |
| `API/EmbedHandshakeController.cs` | 5 | 4 | 4 | 80.0 % | 80.0 % |
| `API/EmbedEntryController.cs` | 34 | 27 | 27 | 79.4 % | 79.4 % |
| **total** | **208** | **159** | **184** | **76.44 %** | **88.46 %** |

### Closed by this pass

New file `Lighthouse.Backend.Tests/API/EmbedStartControllerTest.cs` — hop 1 at the cheapest level
that still runs the real controller. The journey tests already cover the wiring; these cover the
edges of the nonce contract, the challenge's return address and the shape of the terminal page.

| scenario | mutant it kills |
| --- | --- |
| `Start` sets `Referrer-Policy: no-referrer` | both string mutants on `EmbedStartController.cs:63` — the header name and its value. D39: the nonce rides in the query string, so a Referer carries a live handshake onward |
| a nonce of exactly 22 and exactly 128 characters proceeds to the identity provider | `:91` `nonce.Length < Minimum` → `<=`, and `> Maximum` → `>=` — the bounds are inclusive |
| a nonce of 21 and of 129 characters is refused | (the other side of the same bounds; no new mutant, but the boundary is meaningless asserted from one side) |
| a nonce whose first character is legal and whose remaining 30 are not is refused | `:96` `nonce.All(...)` → `nonce.Any(...)`. Security-relevant: under the mutant one legal character admits an otherwise arbitrary string |
| the challenge's `RedirectUri` round-trips `/embed/start?nonce=…` and names the OIDC scheme | `:117` `new AuthenticationProperties {}` and `:119` `$""` — under either the viewer completes a login that resolves nothing |
| a viewer with readable scope ends on a 200 `text/html; charset=utf-8` page whose body says so | `:149` `new ContentResult {}`. The empty initializer still answers 200 (that is `ContentResult`'s default), so status alone could never kill it — the type and the body are what pin D61 |

New file `Lighthouse.Backend.Tests/Services/Implementation/Auth/EmbedSessionTokenServiceTest.cs` —
the edges the journey and container tests cannot reach cheaply, driven through the injected
`TimeProvider` so the instants are exact rather than whatever the wall clock happened to be.

| scenario | mutant it kills |
| --- | --- |
| a `null` / `""` / `"   "` nonce is `Unresolved` and never reaches the store | `:88` block removal on the blank-nonce guard (previously **no coverage** at all) |
| an outcome whose window closes at exactly this instant is `Unresolved`, even when the consuming update would have succeeded | `:98` `stored.ExpiresAt <= now` → `<`. The repository mock is set to return 1 affected row on purpose: without that, the mutant falls through to a no-op update and answers `Unresolved` anyway, and the boundary stays invisible |
| seven malformed tokens (`null`, `""`, `"   "`, no separator, three parts, empty id, empty secret) are refused **without a store read** | `:125` block removal on the `TrySplit` guard, `:248` and `:254` `return false` → `true` inside `TrySplit`. All three produce an empty token id that finds no row and refuses anyway, so the response alone cannot separate them — the `Times.Never` verification is what makes them observable, and it states a real rule: a token that is not two non-empty halves names no row, and looking one up turns the store into a timing oracle |
| a grant row carrying **no digest** is refused even when the redeeming update would succeed | `:273` `SecretMatches`' `storedHash is null` → `return true` (previously **no coverage**). Under the mutant, whoever guesses a token id is signed in |
| the handshake outcome expires after the configured lifetime, and after `DefaultHandshakeOutcomeLifetimeSeconds` when the configured value is 0 | all four mutants on `:233` — `configured > 0` → `< 0` and → `>= 0`, plus both conditional collapses. DQ-2: an unconfigured instance falls back to the human-login window, not to zero |

`ApiKeyPrincipalFactoryTest.cs` — three scenarios added for the ADR-137 viewer overload, which had
no test at all. `Create(string, string?, string)` is `public static` and pure, so no host is needed.

| scenario | mutant it kills |
| --- | --- |
| a viewer with a display name carries it on **both** `name` and `ClaimTypes.Name` | `:42` the `!` on `IsNullOrWhiteSpace(displayName)`, and the two `claims.Add` statements at `:44` and `:45`. `ClaimsPrincipal.Identity.Name` reads the second one, which is what the framed SPA renders |
| a `null` / `""` / `"   "` display name emits **neither** claim | the three string rewrites of `IsNullOrWhiteSpace(displayName)` at `:42` — `!= null` is separated by the blank cases, `!= ""` and `.Trim() != ""` by the `null` and whitespace cases |
| a blank subject refuses to build a principal | `:34` `ArgumentException.ThrowIfNullOrWhiteSpace(subject)` removal. Only `""` and `"   "` kill it — with `null` the mutant throws from the `Claim` constructor anyway, which is why the case is parameterised rather than picked |

29 test cases added; backend suite 4493 → 4522, 0 failed, 0 skipped, 0 build warnings, and
`dotnet format analyzers --severity info` reports nothing in any of the three files.

### Survivors left behind, with their category

19 survived + 5 no-coverage. None is a missing behavioural test.

**Deliberately not asserted — log text (13).** `EmbedSessionTokenService.cs:23` states outright that
the message is free to change because the operator's alert keys on the EventId *name*. Asserting
the prose would contradict the decision that made it prose.

| file:line | mutant |
| --- | --- |
| `EmbedSessionTokenService.cs:44` | statement removal + string — the `LogDebug` on mint |
| `EmbedSessionTokenService.cs:143` | statement removal + string — the `LogWarning` on a refused redemption |
| `EmbedSessionTokenService.cs:157` | statement removal + string — the `LogInformation` on revoke-all |
| `EmbedSessionTokenService.cs:179` and `:196` | `LogNonceReplayed()` statement removal on the lost-race branches of `ConsumeGrantAsync` / `ConsumeRefusalAsync` (**no coverage**: only the container fixtures reach a lost race) |
| `EmbedSessionTokenService.cs:207` | the replay warning's message template |
| `EmbedEntryController.cs:93` and `:94` | statement removal + string — the `LogWarning` for an owner unlinked after minting |
| `EmbedEntryController.cs:118` | statement removal + string — the `LogWarning` for a viewer whose profile is gone (**no coverage**) |

**Covered outside the filter (1).** `EmbedSessionTokenService.cs:22`, the EventId *name*
`"EmbedHandshakeNonceReplayed"`. This one **is** the contract (D62/D67) and **is** asserted —
`EmbedSessionSingleUseConcurrencyTests.Handshake_ManySimultaneousPollsOfOneNonce_…` counts log
events by that name and requires `ConcurrentPolls - 1` of them. That fixture starts a real Postgres
container per test, so the config excludes it via `FullyQualifiedName!~Containers`; a container per
mutant is prohibitive, and the filter stays as it is.

**Equivalent (5).** The mutation cannot change anything a caller can observe.

| file:line | mutant | why |
| --- | --- | --- |
| `EmbedStartController.cs:81` | `!Succeeded \|\| Principal is null` → `&&` | ASP.NET's own invariant makes the second conjunct redundant: `AuthenticateResult.Succeeded` is `Ticket != null`, `Principal` is `Ticket?.Principal`, and `AuthenticationTicket`'s constructor rejects a null principal. So `Succeeded == false` implies `Principal == null` and the two forms are the same predicate. **This contradicts the run-1 triage**, which expected a case where exactly one conjunct holds; no handler can produce one |
| `EmbedStartController.cs:109` and `EmbedHandshakeController.cs:36` | `mode == AuthMode.Blocked ? 403 : 404` → always 404 | the `Blocked` branch is unreachable. `BlockedModeFilter` is a **global** MVC action filter (`Program.cs:268`) that short-circuits with 403 for every path outside `/api/latest/{auth,license,version}` — both embed routes are outside it, so neither action body ever runs in blocked mode. `S13_…_AreBlockedWhenThePremiumLicenceIsNotValid` already asserts the 403 on both endpoints and passes under the mutant, which is exactly why it survived. **This contradicts the run-1 triage**; killing it would need the guard moved or the filter's allow-list widened, i.e. a production change, and D44 records that the filter refusing first is the intended order |
| `EmbedSessionTokenService.cs:243` and `:244` | `tokenId = string.Empty` / `secret = string.Empty` → `"Stryker was here!"` | these are `TrySplit`'s `out` parameters on its **failure** path. The single caller checks the `bool` first, and on the success path both are overwritten by `parts[0]` / `parts[1]`. Killing them would mean asserting a private helper's out-parameters. **This contradicts the run-1 triage**, which read these two line numbers as the `Replace('+','-')` / `Replace('/','_')` calls in `Base64UrlEncode` — those are at `:290`–`:291` and were already killed |
| `EmbedEntryController.cs:58` | `if (!redemption.Succeeded) { return Refuse(); }` block removal | `EmbedSessionTokenRedemption.Refused` reports `ApiKeyId = 0`, so the fall-through resolves API key `0`, gets `null`, and returns the identical 401 with the identical HTML and no cookie. Defence in depth where the second guard subsumes the first; rows never start at id 0 on either provider. Same finding as the 5641 run, at a new line number |
| `EmbedEntryController.cs:71` | `new AuthenticationProperties { IsPersistent = false }` → `{}` | `IsPersistent` reads `false` whether absent or explicitly `false`, so the cookie is a session cookie either way. The *value* mutation on the same line is killed by `S9_EmbedCookiePolicyTests`, so D40 itself stays pinned |
| `EmbedEntryController.cs:109` | `if (string.IsNullOrWhiteSpace(subject)) { return null; }` block removal (**no coverage**) | F7 defence in depth, and `RedeemAsync` already refuses a row naming nobody — which is why nothing reaches it. Under the mutant a blank subject reaches `FindBySubjectAsync`, matches no profile and refuses with the same 401 |

**Not chased — repository predicate boundary (2).** `EmbedSessionTokenRepository.cs:94` (the
`&&` between the nonce hash and `HandshakeConsumedAt == null`) and `:96` (`ExpiresAt > consumedAt`
→ `>=`). I could **not** confirm the run-1 triage's claim that these are covered outside the
filter: `EmbedSessionTokenRepositoryTest.cs` is inside the filter and has no
`TryConsumeHandshakeGrantAsync` coverage at all, and the container fixture drives a single nonce
that is never at its expiry instant, so neither mutant would die there either. These are genuinely
unasserted boundaries on a private query predicate. They are left because the gate is met at
88.46 % with production untouched, not because they are unkillable — a repository-level fixture
asserting consumption at exactly `ExpiresAt`, and a second nonce that must not be swept up, would
close both.

## Production defects exposed

None. Every survivor triaged to log text, an equivalent mutant, or a boundary left unasserted by
choice. Three of the run-1 triage's targets turned out to be equivalent mutants rather than gaps
(`EmbedStartController.cs:81`, the two D31 ladders, and `EmbedSessionTokenService.cs:243`/`:244`);
they are recorded above with the evidence, so the next pass does not spend a cycle re-deriving it.

## Verification

`dotnet build` — 0 warnings, 0 errors. `dotnet test` — 4522 passed, 0 failed, 0 skipped.
`dotnet format analyzers Lighthouse.sln --severity info --verify-no-changes` — no finding in any
file this pass touched (the ~35 pre-existing CA1861/CA1825 hits are all in generated EF migrations).
`git diff --stat -- Lighthouse.Backend/Lighthouse.Backend/` — empty.

---

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

---

# Mutation testing — 5694 (slice 03: remove the API-key embed path)

Run 2026-08-06 against `main`. Gate is 80 % kill rate; `CLAUDE.md` sets `per-feature`, so this is a
real gate rather than a nightly.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **89.00 %** | 200 | 178 | 14 | 8 | 13 m 10 s |
| Frontend (StrykerJS) | *N/A* | — | — | — | — | — |

**Frontend is N/A, not skipped.** `git diff 4d91ea229..HEAD --stat -- Lighthouse.Frontend/` is
empty: this slice is a backend deletion plus an E2E spec split. Nothing for StrykerJS to mutate.

Config: `stryker.5694.backend.json` beside this file. Same arrangement as the earlier runs — copy it
into `Lighthouse.Backend.Tests/` under the conventional name and run from there:

```
cp docs/feature/epic-5146-jira-forge-app/mutation/stryker.5694.backend.json \
   Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.epic5146-slice03.json
cd Lighthouse.Backend/Lighthouse.Backend.Tests
TZ=Europe/Zurich dotnet stryker --config-file stryker-config.epic5146-slice03.json
```

## Per file

| file | tested | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| `Configuration/EmbedConfiguration.cs` | 4 | 4 | 0 | 0 | **100 %** |
| `Models/Auth/EmbedSessionTokenRedemption.cs` | 1 | 1 | 0 | 0 | **100 %** |
| `Services/Implementation/Auth/ApiKeyPrincipalFactory.cs` | 25 | 25 | 0 | 0 | **100 %** |
| `Services/Implementation/Repositories/UserProfileLookup.cs` | 1 | 1 | 0 | 0 | **100 %** |
| `API/EmbedStartController.cs` | 35 | 33 | 2 | 0 | 94.3 % |
| `API/EmbedHandshakeController.cs` | 9 | 8 | 1 | 0 | 88.9 % |
| `Services/Implementation/Repositories/EmbedSessionTokenRepository.cs` | 26 | 23 | 2 | 1 | 88.5 % |
| `Services/Implementation/Auth/EmbedSessionTokenService.cs` | 72 | 62 | 7 | 3 | 86.1 % |
| `API/EmbedEntryController.cs` | 27 | 21 | 2 | 4 | 77.8 % |
| **total** | **200** | **178** | **14** | **8** | **89.00 %** |

`EmbedEntryController` is the one file below the gate on its own. Its survivors are the log-text and
`AuthenticationProperties` mutants recorded as equivalent in the 5692 run — the same set, minus the
API-key branch that no longer exists to be mutated.

## What this run does not cover, on purpose

The `test-case-filter` excludes `Containers`, so the Postgres fixtures do not run: one container per
mutant is prohibitive. Their subjects — the conditional updates behind ADR-131, the check constraint,
the nonce-replay event name — are therefore **covered by tests that this score does not count**. That
was true of the 5692 run too and is recorded there for the same reason.

## Context

Ran after the adversarial review, not before. The review rejected the slice on a BLOCKER — the
readable-scope reversal was argued on "RBAC is enforced per request", and `LogsController` had no
guard at all, so a scope-less viewer with an embed cookie could download the instance log. Fixed and
covered by `AViewerWithNoReadableScope_CannotReachInstanceWideSurfaces` before this run. Scoring code
a reviewer had rejected would have measured the wrong thing.
