# Security review — embed session (epic 5146, slice 02a / #5641)

Reviewed 2026-08-04 against the working tree at `main` (clean, `bb5d18f47`). Scope: the S1–S10
product checklist in `feature-delta.md:1124-1136`, read adversarially against the shipped code.
S11 (demo tunnel) is out of scope by instruction.

Method note: findings were verified against code, and four of them against a **temporary probe test
harness** driven through `EmbedSessionTestHost` (written, run, deleted; the tree is clean and no
production or test file was modified). Probe output is quoted verbatim where it is load-bearing.

---

## Verdict

**Yes — there are findings that must be fixed before this is put behind a customer demo.**

Two are blocking:

- **F1 — the rate limiter on both embed endpoints is a no-op in every shipped configuration.** The
  `EnableRateLimiting("EmbedSession")` attribute is present but `appsettings.json` never defines an
  `EmbedSession` policy, so the factory falls through to `RateLimitPartition.GetNoLimiter`. Probed:
  *40 consecutive mints, never throttled.* S7's "defence in depth" does not exist; the token's
  entropy is the **only** control.
- **F2 — an established embed session can mint itself a fresh embed token, indefinitely.** The
  embed cookie principal carries `api_key_id`, so `POST /api/v1/embed/session-token` accepts it.
  Probed: `PROBE renew status=OK`. This directly contradicts ADR-130:48-51, which states the
  non-sliding cookie exists "because a cookie that renews itself indefinitely defeats the short
  lifetime that bounds ADR-131's revocation gap". **The design says the gap is bounded at 30
  minutes; the code makes it unbounded.** Per instruction, the code wins and the disagreement is
  itself the finding.

F3 (no revocation path for an established session at all) is a close third and is arguably in scope
for the same fix as F2. F4–F7 are not demo blockers but should be recorded.

Everything else on the checklist holds. The redemption path, the single-use conditional update, the
cookie split, the `AuthMode` gating and the `returnPath` validation are all correct, and several are
better than the design promised.

---

## Checklist

| # | Verdict | Evidence |
|---|---|---|
| **S1** | **CANNOT VERIFY** | The Forge app is in a different repository (`lighthouse-jira-app@0c63b42`, per `feature-delta.md:1296`); no `setSecret` call exists anywhere under `/storage/repos/Lighthouse`. D32 is unverifiable from here — see "Questions for Atlassian" below |
| **S2** | **PASS WITH NOTE** | The control is administrative and correctly *not* enforced in code (`RbacAdministrationService.cs:973-983` intersects per-key scope); but no customer-facing guidance exists in `docs/` yet. Guidance text supplied below |
| **S3** | **PASS WITH NOTE** | `Referrer-Policy: no-referrer` at `EmbedEntryController.cs:46`, set *before* every return path; clean-URL 302 at `:76`, asserted at `S8_EmbedEntryPointTests.cs:36-51`; no logging call anywhere emits the token. But there is **no scrubbing** — see N1 |
| **S4** | **PASS** | `EmbedSessionTokenRepository.cs:23-34` `ExecuteUpdateAsync` with a `RedeemedAt == null && RevokedAt == null && ExpiresAt > now` predicate; 8-way barrier-synchronised probe on a real Postgres container at `EmbedSessionSingleUseConcurrencyTests.cs:29-59`; sequential replay covered at `S8_EmbedEntryPointTests.cs:70-92` |
| **S5** | **PASS WITH NOTE** | Both halves asserted: embed on the wire at `S9_EmbedCookiePolicyTests.cs:31-49`, session `Lax` at `:104-118`. `Program.cs:642-674` (the original block) is genuinely untouched — the embed scheme is a separate `.AddCookie` at `:675-702`. Note N2: the session half is asserted off `Cookie.Build()`, not a live response |
| **S6** | **FINDING (F2, F3)** | `Program.cs:688-689` sets 30 min / `SlidingExpiration = false` correctly, and `S9_..:97-102` pins it. But the bound does not hold (F2) and there is no way to end a live session (F3) |
| **S7** | **FINDING (F1)** | `EnableRateLimiting` at `EmbedSessionController.cs:23,60` and `EmbedEntryController.cs:36`; policy registered at `Program.cs:889`; **no `EmbedSession` entry in `appsettings.json:64-71`** → `Program.cs:898-901` returns `GetNoLimiter` |
| **S8** | **PASS** | `EmbedEntryController.cs:81` `Url.IsLocalUrl`. All four documented negatives plus 7 further forms probed (`~/`, `~//`, `%2F%2F`, `/..//`, tab, backslash-at, CRLF) — every off-host form landed on `/` |
| **S9** | **PASS WITH NOTE** | Behaviourally correct — `S10_EmbedScopeEquivalenceTests.cs:48-72` asserts the out-of-scope refusal, which is the *presence* test in effect. But it is **not structural** — see N3 |
| **S10** | **PASS** | `EmbedSessionController.cs:81` and `EmbedEntryController.cs:48` both gate on `AuthMode.Enabled`; `BlockedModeFilter` registered at `Program.cs:266-269` pre-empts with 403 (`BlockedModeFilter.cs:37-45`). Asserted for all four combinations: `S7_..:180-207` and `S8_..:236-267` |

---

## Findings

### F1 — HIGH — Rate limiting on both embed endpoints is inert in every shipped configuration

**Where.** `appsettings.json:64-71` defines `AuthLogin`, `ApiKeys`, `BootstrapSystemAdmin` — and no
`EmbedSession`. `Program.cs:898-901`:

```csharp
if (!snapshot.Policies.TryGetValue(capturedPolicyName, out var policyConfig))
{
    return RateLimitPartition.GetNoLimiter("unconfigured");
}
```

A `ctx_search` for `EmbedSession` across every `*.json`, `*.yaml` and `*.yml` in the repo returns
only the Stryker config. The Helm chart does not supply it either.

**What an attacker does.** Hammers `GET /embed/enter?token=…` at line rate against a 128-bit token
id, or hammers `POST /api/v1/embed/session-token` with candidate API keys, with no server-side
brake. Neither is likely to succeed against the entropy, but the endpoint is also a free
`ExecuteDelete` + `INSERT` per request (`EmbedSessionTokenService.cs:26-38` prunes on *every* mint),
which makes the mint endpoint a cheap database-amplification DoS for anyone holding one valid key.

**Why the test did not catch it.** `S7_Exchange_ExceedsRateLimit_Throttled_WithRetryAfter`
(`S7_..:210-236`) calls `host.WithEmbedRateLimit(3, 2)`, which *injects* the policy
(`EmbedSessionTestHost.cs:64-74`). It proves the wiring works when configured; it cannot see that
the shipped default leaves it unconfigured. Probe with the shipped defaults:

```
PROBE never throttled in 40 calls with shipped appsettings defaults
```

**What to change.** Add to `appsettings.json` under `RateLimits.Policies`:

```json
"EmbedSession": { "PermitLimit": 30, "WindowSeconds": 60, "QueueLimit": 0 }
```

and add a test that reads the shipped configuration rather than an injected one — e.g. assert that
every name in `RateLimitingConfiguration`'s policy-name constants has a matching entry in the bound
`RateLimitingConfiguration.Policies`. That guard catches the next policy added the same way.
(Note: `AuthLogin` at 100/60s and `ApiKeys` at 20/60s bracket a sensible value; the entry point is
hit once per framed page open, the exchange once per page open.)

**S7's own question, answered.** Yes — with multiple replicas an attacker spreads across them and
the per-instance fixed window is worth roughly `replicas × PermitLimit`. And yes, the real control
against token guessing is the 256-bit secret (`EmbedSessionTokenService.cs:19`,
`RandomNumberGenerator.GetBytes`), not the limiter. But that argument was allowed to be casual
*because* a limiter was also shipping. Right now nothing is shipping.

---

### F2 — MEDIUM-HIGH — An embed session can renew itself forever, defeating the 30-minute bound

**Where.** `EmbedSessionController.cs:34-46`. The mint endpoint's only identity requirements are
an `api_key_id` claim and a `sub` claim. The embed cookie principal is built by the *same* factory
(`EmbedEntryController.cs:68` → `ApiKeyPrincipalFactory.Create`), so it carries both.
`SmartAuthSchemeSelector.Select(request)` routes a cookie-only request bearing `.Lighthouse.Embed`
to the embed scheme, `[Authorize]` is satisfied, and the endpoint mints.

Probed:

```
PROBE renew status=OK body={"token":"<masked>","expiresAt":"...","embedUrl":"..."}
PROBE revoke status=NoContent
```

**What an attacker does.** Obtains one embed cookie — by any of the routes S3 already accepts as
in-scope (browser history, a proxy log, an Atlassian log, a shoulder-surfed URL) — and then, from
any HTTP client, loops `POST /api/v1/embed/session-token` → `GET /embed/enter?token=…` every 29
minutes. The session never expires. The API key itself is never needed again after the first
cookie. Revoking outstanding *tokens* does nothing (revoke-all only touches unredeemed rows,
`EmbedSessionTokenRepository.cs:36-44`), and the attacker can call revoke-all themselves
(`PROBE revoke status=NoContent`), which is a minor nuisance for the legitimate frame.

**Why this is a design/code disagreement, not just a gap.** ADR-130:48-51 and :56 argue explicitly
that `SlidingExpiration = false` is what keeps ADR-131's revocation gap bounded, and D40
(`feature-delta.md:1294`) repeats it: *"a frame left open all day cannot outlive its window"*. It
can. The cookie does not slide, but the session does, by a different mechanism the ADR did not
consider.

**What to change.** Restrict the exchange (and revoke-all) to the API-key scheme. Both are
one-liners and either is sufficient:

- `[Authorize(AuthenticationSchemes = SmartAuthSchemeSelector.ApiKeyScheme)]` on
  `EmbedSessionController`, replacing the bare `[Authorize]` at `EmbedSessionController.cs:17`; or
- an explicit guard alongside `TryGetApiKeyId`, e.g. refuse when
  `User.Identity?.AuthenticationType == SmartAuthSchemeSelector.EmbedCookieScheme`. The factory
  already stamps the scheme name onto the `ClaimsIdentity` (`ApiKeyPrincipalFactory.cs:22`) and it
  survives the cookie round-trip, so this is checkable.

Add the negative test in the same shape as
`S7_Exchange_SignedInUserWithoutAnApiKey_Refused_WhileTheKeyMints` — an embed cookie must not mint,
while the key still does.

---

### F3 — MEDIUM — There is no way to end an established embed session, including by deleting the key

**Where.** `Program.cs:675-702` registers the embed cookie scheme with no `SessionStore` and no
`Events.OnValidatePrincipal`. The ticket is a self-contained data-protection payload; nothing
re-reads the database on subsequent requests.

**Consequence, stated precisely.** The header-borne path re-validates the key on *every* request
(`ApiKeyAuthenticationHandler.cs:41`), so deleting an API key kills header access immediately. The
embed path does not. **Deleting or revoking the API key leaves every session already minted from it
alive for up to 30 minutes** (unbounded, given F2). ADR-131's negative consequence says
"Revocation granularity is the token, not the session, with a worst case of 30 minutes" — accurate
for token revocation, but it does not say that *key deletion* is equally powerless, and that is the
lever an administrator will reach for in an incident.

**What an attacker does.** Nothing new — this is the incident-response property. The operator
revokes the key, sees the API-key path die instantly, and reasonably concludes the frame is dead
too. It is not.

**What to change.** Add an `OnValidatePrincipal` handler on the embed scheme that re-resolves
`api_key_id` through `IApiKeyIdentityResolver` and calls `RejectPrincipal()` when the key is gone or
its owner is unlinked. That is one database read on a path that already loads an entire SPA, it
reuses a component the redemption path already calls (`EmbedEntryController.cs:60`), and it closes
F2's renewal loop as a side effect once the key is deleted. If that cost is unwanted for a
feasibility slice, then the *documentation* must say plainly: **revoking the key does not end live
embed frames; wait out the cookie lifetime.**

---

### F4 — MEDIUM — The embed cookie outranks the ordinary session cookie, so one link silently swaps a user's identity

**Where.** `SmartAuthSchemeSelector.cs:44-51`:

```csharp
return request.Cookies.ContainsKey(EmbedCookieName) ? EmbedCookieScheme : CookieScheme;
```

Probed with both cookies present: `PROBE scheme=LighthouseEmbedCookie`. The cookie is emitted with
`Path=/` (framework default via `CookieBuilder.Build`, `Path ?? "/"`), so it applies to the whole
site, not just `/embed`.

**What an attacker does.** Anyone holding *any* API key on the instance mints a token and sends a
signed-in colleague a link: `https://lighthouse.example/embed/enter?token=…&returnPath=/`. A single
top-level click sets `.Lighthouse.Embed`; from that moment every request the victim makes — the SPA,
`/api/…`, `my-summary` — authenticates as the **key owner**, not as the victim, and their own valid
`.Lighthouse.Session` cookie is ignored. There is no UI signal, because D37 deferred the embed-mode
indicator as cosmetic. The victim's effective permissions silently change (up or down), admin
surfaces appear or vanish, and anything they create is attributed to the key owner.

CHIPS partitioning does *not* mitigate this variant: a top-level navigation writes into the
first-party jar. `Partitioned` only helps against the cross-site-iframe variant.

**Why S8 did not catch it.** S8 is framed as "open redirect" — can the redirect leave the host. It
cannot. The risk here is the opposite: the redirect *stays*, carrying a cookie the victim did not
ask for. That is forced login / session fixation, and it is not on the checklist.

**What to change.** Cheapest correct fix: invert the precedence when both cookies are present —
prefer `.Lighthouse.Session`, so an embed cookie can never shadow a real login. A framed viewer
never has a session cookie in the partition, so nothing is lost. Add the missing test case to
`SmartAuthSchemeSelectorTest.cs` (it currently covers embed-only, session-only, neither, and
api-key-plus-embed — but **not** both cookies). Note that this also makes D37's deferred embed-mode
indicator a security control rather than a nicety, and should be re-labelled as such in the verdict.

---

### F5 — LOW-MEDIUM — `embedUrl` reflects the request's `Host` and `Scheme`

**Where.** `EmbedSessionController.cs:93-96`:

```csharp
return $"{Request.Scheme}://{Request.Host}{EmbedEntryController.EntryPath}?token={Uri.EscapeDataString(token)}";
```

`appsettings.json:35` ships `"AllowedHosts": "*"`, and `ForwardedHeadersConfigurator.cs:39-44` sets
`ForwardedHeaders.None` unless `Authentication:TrustedProxies` or `:TrustedNetworks` is populated —
which the shipped defaults (`appsettings.json:56-57`) leave empty.

**Two consequences.**

1. *Wrong scheme on the default reverse-proxy deployment.* Behind a TLS-terminating proxy with no
   `TrustedProxies` configured, `Request.Scheme` is `http`, so `embedUrl` comes back as
   `http://host/embed/enter?token=…`. The Forge frame would load it as mixed content (blocked), and
   if it were not blocked the token would cross the last hop in cleartext and the `Secure` embed
   cookie would be refused. This is a *functional* footgun that presents as an inexplicable blank
   frame, which is exactly what D26 exists to prevent.
2. *Host reflection.* A caller who can set the `Host` header on the mint request gets a token URL on
   a host of their choosing. The caller must already hold the API key, so this is not an escalation
   — but if the Forge resolver trusts `embedUrl` verbatim (it is documented as "so the caller never
   composes it", `EmbedSessionTokenResponse.cs:9`), any intermediary that rewrites `Host` can
   redirect the token to itself.

**What to change.** Build `embedUrl` from a configured public base URL rather than from the request,
or drop `embedUrl` from the response and let the Forge side compose it from its own configured
`targetInstance` (which it already stores, D11/D32). At minimum, document that
`Authentication:TrustedProxies` must be set for any proxied deployment that uses embed.

---

### F6 — LOW — Minting prunes every key's rows, not the calling key's

**Where.** `EmbedSessionTokenService.cs:26` calls `repository.PruneSpentAsync(now, …)`, which is
unscoped (`EmbedSessionTokenRepository.cs:46-54`) and deletes across all API keys.

The DESIGN declares the mint endpoint's mutation set as **"exactly `EmbedSessionToken` rows for the
calling key"** (`feature-delta.md:918`). It is not. No security impact — the rows deleted are spent,
expired or revoked and are refused either way — but the declared bounded-change contract is false as
written, and the delete is a cross-tenant write on a multi-key instance. Either scope the prune to
`apiKeyId`, or correct the contract line in the design. It also destroys any forensic record of
which tokens were redeemed, which matters if F3's incident-response question is ever asked in anger.

---

### F7 — INFO — One accepted mutation survivor rests on a data property, not a code property

`mutation/results.md` justifies the `EmbedEntryController.cs:55` block-removal survivor with: *"a
refused redemption reports `ApiKeyId = 0`; the very next guard resolves key `0`, gets `null` … Rows
never start at id 0 — SQLite and Postgres both begin at 1."*

The reasoning is correct today and I verified the chain (`EmbedSessionTokenRedemption.Refused` is
`new(false, 0)`; `ApiKeyIdentityResolver.ResolveByApiKeyId(0)` → `GetById(0)` → `null` → refuse).
But it is equivalence-by-database-sequence-behaviour, not equivalence-by-code. A seeded fixture, a
data import, or a provider whose identity column starts elsewhere would turn a documented
"equivalent mutant" into a live authentication bypass. Recommend keeping the guard (it is already
there) and softening the justification to "defence in depth, retained deliberately" rather than
"equivalent" — the distinction is what stops someone deleting the guard later on the strength of
this document.

The other 14 survivors I checked and believe: the 8 log-only ones are genuinely unobservable, and
the `TrySplit` `return false → true` pair are genuinely equivalent because an empty `tokenId` cannot
match a 16-byte base64url column value.

---

## Notes attached to PASS verdicts

**N1 (S3) — "token scrubbing" is not scrubbing; it is a log level.** No code redacts the token. What
keeps it out of the log is that `appsettings.json:7,10` overrides `Microsoft.AspNetCore` and
`Microsoft.AspNetCore.Hosting` to `Warning`, which suppresses ASP.NET Core's default
`Request starting … {QueryString}` Information line. There is no `UseSerilogRequestLogging` or
`UseHttpLogging` in the app (searched, zero matches), so that is the only exposure — but it is one
config edit away. A customer who raises `Microsoft.AspNetCore` to `Information` to debug something
will write live embed tokens to `./logs/log-*.txt` and never know. D39 named scrubbing as a required
mitigation *specifically* so it would not be "left to a logging config nobody reviews"; it has been
left to a logging config nobody reviews. Low severity given the 60-second single-use window (which
genuinely does hold — see S4), but the mitigation as shipped is not the mitigation as specified.
I also confirmed no `Cache-Control: no-store` on the entry-point response; a 302 is not
heuristically cacheable so this is not exploitable, but adding it is free.

**N2 (S5) — the session half is asserted off options, not the wire.** S5 asks for the wire assertion
to cover both halves. The embed half is genuinely on the wire
(`S9_EmbedCookiePolicyTests.cs:31-49`, reading the literal `Set-Cookie`). The session half
(`:104-118`) reads `CookieAuthenticationOptions` and calls `Cookie.Build(new DefaultHttpContext())`.
That is a good check and it would catch a regression, but it is one abstraction short of the
literal header. Also unasserted: `Path=/` on the embed cookie, which F4 depends on.

**N3 (S9) — correct, but not structural.** `ApiKeyPrincipalFactory.BuildClaims` adds `api_key_id`
under `if (validationResult.ApiKeyId.HasValue)` (`:35`) and `sub` under a *separate, independent*
condition (`:42-47`). A `ApiKeyValidationResult` with `ApiKeyId = null` and a resolved owner would
produce exactly the fail-open principal S9 exists to prevent —
`RbacAdministrationService.cs:968-971` returns the owner's permissions unchanged when the claim is
absent. That state is unreachable today because both producers always set it
(`ApiKeyService.cs:182,195`; `ApiKeyIdentityResolver.cs:25,34`), and the behavioural tests
(`S10_..:48-72`) would fail if it happened. But S9 asked for structural impossibility. To get it:
make `ApiKeyValidationResult.ApiKeyId` non-nullable, or have `BuildClaims` return early (no `sub`,
no claims at all) when `ApiKeyId` is null — one `if`, and the fail-open state stops being
representable.

**S2 — the guidance, stated for the record.** Because the key *is* the authorization boundary,
every person who can open the Jira page holds whatever the key holds. The customer must be told, in
these terms:

> Create a dedicated API key for the Jira app. Scope it to the portfolios or teams the page should
> show, with the **Viewer** role, and nothing else. Never use a key owned by a SystemAdmin, and never
> use an unscoped key — an unscoped key inherits its owner's full permissions
> (`RbacAdministrationService.cs:978-981`, legacy-key back-compatibility). Rotate it by creating a
> new key and deleting the old one. Deleting the key stops new sessions immediately, but does not end
> frames already open (see F3).

That guidance exists nowhere in `docs/` today. It is the single most important sentence in the
feature and it should ship with slice 02b, not with the verdict.

---

## Outside the checklist

Items the S1–S10 list did not name, in the order they matter:

1. **Forced login / session shadowing — F4.** The list's redirect item is scoped to "can it leave the
   host"; the risk that the redirect *stays* and plants an identity was not asked.
2. **Self-renewal — F2.** The list treated lifetime and revocation as one item (S6) and only asked
   whether the *number* reached the wire. It did, and the number is nonetheless not a bound.
3. **Live-session revocation — F3.** S6 says "revoking a token does not end a session". Nobody asked
   whether revoking the *key* does. It does not.
4. **Token entropy and comparison — clean.** `RandomNumberGenerator.GetBytes(16)` for the id and
   `(32)` for the secret (`EmbedSessionTokenService.cs:18-19`), base64url, CSPRNG. The secret is
   SHA-256'd (correct: a 256-bit random value does not need a password KDF, and ADR-129 says so) and
   compared with `CryptographicOperations.FixedTimeEquals` over the two base64 digests
   (`:117-122`) — both operands are always 44 chars, so the length-mismatch early-return cannot fire
   asymmetrically. No issue.
5. **Timing side channel on token-id existence — negligible, worth knowing.** `RedeemAsync` returns
   before hashing when the row is absent (`:59-63`), so a present token id costs one extra SHA-256
   over a ~10 µs database round trip. Against a 128-bit id the oracle is worthless. Not a finding;
   listed so nobody rediscovers it as one.
6. **Refusals are uniform — good.** Every failure path returns the identical 401 and the identical
   HTML (`EmbedEntryController.cs:24-31`, `:86-97`): malformed, unknown, wrong secret, expired,
   revoked, replayed, and owner-unlinked-since-mint are indistinguishable to the caller. Only the
   server log differentiates (`:59-63`, `EmbedSessionTokenService.cs:69`). The wrong-secret case is
   the one that matters and it is pinned by
   `S8_Enter_GenuineTokenIdWithAWrongSecret_Refused_AndLeavesTheRealTokenSpendable` — which also
   checks the failed attempt does not consume the genuine token, closing a guessed-id DoS. That test
   is the best one in the slice.
7. **Concurrency on revoke-all — bounded, no action.** `RevokeOutstandingForApiKeyAsync` is a
   set-based `ExecuteUpdate`. A redemption committing between the operator's decision and the update
   wins the race and gets a session. That window is bounded by the 60-second token lifetime and is
   inherent; it is the same window F3 describes from the other side.
8. **Prune path — F6**, plus: pruning runs on the mint request's critical path and is unbounded in
   rows deleted. On a busy instance the first mint after a quiet period pays for every stale row.
   Not a security issue; a latency one, on the page's first impression.
9. **`HttpOnly` / `Secure` and proxies — clean.** `CookieSecurePolicy.Always` (`Program.cs:680`)
   means `Secure` is emitted regardless of whether the request arrived over TLS, so a TLS-terminating
   proxy cannot cause a downgrade. `HttpOnly = true` (`:679`) is asserted on the wire. The corollary
   is that the embed flow **cannot work over plain HTTP at all** — which makes D35's `mkcert`
   requirement load-bearing rather than a convenience, and means the `:5169` dev instance can never
   exercise this path even if authentication were enabled on it.
10. **`returnPath` — probed harder than the four negative tests.** All seven additional forms
    behaved: `~/evil` → `/evil` (resolved by `UrlHelper.Content`, stays local); `~//evil.example`,
    `/\t/evil.example`, `//evil.example\@x` and `/\r\nSet-Cookie: x=y` all → `/`; `/%2F%2Fevil.example`
    and `/..//evil.example` pass through verbatim but stay same-origin, because RFC 3986 §5.2.2 fixes
    the authority from the base *before* `remove_dot_segments` touches the path. I could not construct
    a bypass. If you want belt-and-braces, reject any `returnPath` whose second character is `.` —
    it costs nothing and removes the need for anyone to re-derive the RFC argument.
11. **`AuthMode` gating has an ordering subtlety worth one line.** `EmbedEntryController.cs:46` sets
    `Referrer-Policy` before the mode check, so `Disabled` 404s still suppress the referrer. Under
    `Blocked` they do not, because `BlockedModeFilter` short-circuits before the action body runs.
    Harmless (the 403 body is a plain string that loads no subresources), but it is the one path where
    a token-bearing URL produces a response without the header.

---

## Questions only Atlassian's documentation can answer

Phrased so they can be looked up. All relate to S1, which is unverifiable from this repository
because the Forge app lives elsewhere.

1. **Read scope of `storage.setSecret`.** Which principals can read a value written with
   `storage.setSecret` — only the app's own backend Forge functions, or also anything running in the
   app's Custom UI frontend? Confirm the value is never returned to the browser. Look under *Forge
   platform → Runtime → Storage API → Secret storage*.
2. **Atlassian staff access.** Under what circumstances, and with what audit trail, can Atlassian
   personnel read the contents of an app's secret storage — for example during a support escalation
   or an incident investigation? Look for the Forge trust/security whitepaper and Atlassian's
   sub-processor and data-access commitments.
3. **Encryption at rest and key custody.** Is Forge secret storage encrypted with a key Atlassian
   controls, or per-installation? What is the stated breach posture if Atlassian's storage tier is
   compromised? *Forge security model* / *Data residency for Forge apps*.
4. **Uninstall semantics.** On app uninstall, is `setSecret` data deleted immediately, on a
   retention timer, or retained pending reinstall? A retained secret is a retained Lighthouse API
   key. Look for *Forge app lifecycle → uninstall* and the storage retention policy. This is the
   question that determines whether "uninstall the app" is a valid revocation instruction for a
   customer, or whether they must also delete the key in Lighthouse.
5. **Log surfaces.** Does Forge log the URLs that a Custom UI frame loads — specifically the
   `iframe src` we put the token in (D39) — in any log a customer or Atlassian can later read, and
   with what retention? This is the S3 exposure we accepted on the strength of the 60-second window;
   the window's adequacy depends on the retention being longer than 60 seconds, which it certainly
   is, and on nobody being able to *replay* from it, which S4 guarantees. Confirming the surface
   exists is still worth doing before a customer demo. Look for *Forge → Observability → logs* and
   *egress/audit logging*.
6. **Rotation.** Is there a supported way to update a `setSecret` value without a reinstall and
   re-consent, so a customer can rotate the Lighthouse API key without the MAJOR-version ritual D33
   describes? *Forge storage API → updating secrets* and *app upgrade rules*.

---

## What I did not verify

- The Forge-side code (S1, D32) — different repository, not present here.
- Real-browser behaviour of `SameSite=None; Partitioned` (D35's second phase). All cookie assertions
  in this review are against the literal `Set-Cookie` header, not a browser.
- Whether `EmbedSessionSingleUseConcurrencyTests` actually runs in CI. It is
  `[Category("requires-docker")]`, and the CI filter is `Category!=Integration`
  (`ci_backend.yml:113`), so it *is* selected — but I did not run the Docker-backed suite here to
  confirm the container starts on the runner. Worth one green CI run before relying on S4's evidence.
