# ADR-137: Viewer-identity embed session — the sign-in hop, the handshake nonce, and what replaces the API key

**Status**: Accepted
**Date**: 2026-08-06
**Feature**: `epic-5146-jira-forge-app` (ADO Epic 5146)
**Decider**: Morgan (Solution Architect), DESIGN wave after the `router.open` probe and the viewer-identity PoC
**Supersedes**: [ADR-129](./adr-129-embed-session-token-exchange-and-identity.md) — its identity model and both of its endpoints. ADR-129 stays on the record as the reasoning that produced the shipped code and the security review that found this design's remaining requirements.
**Keeps unchanged**: [ADR-130](./adr-130-embed-only-cookie-policy.md) in full; [ADR-131](./adr-131-embed-token-lifecycle-and-revocation-store.md) except its `ApiKeyId` binding and revocation lever 1.

---

## Context

ADR-129 answered *whose identity does a framed session carry?* with **a scoped API key the Jira
administrator supplies**. That answer was forced: slice 01 proved the identity provider refuses to be
framed (`X-Frame-Options`, a category result across Auth0, Entra, Okta and Keycloak), so a framed
Lighthouse could not complete an interactive login, so the identity had to arrive from somewhere that
was not the viewer.

Two probes on 2026-08-06 removed that constraint.

**The `router.open` probe.** Forge's Custom UI sandbox blocks `window.open` (measured:
`NotSupportedError`), but `router.open` is Atlassian's own navigation performed outside our frame and
the sandbox never applies to it. It opens a **top-level tab** to any origin, declared or not, and the
promise rejects with `cancelled` when the user declines. The sign-in hop can therefore leave the
sandbox, and at top level the identity provider frames nothing and refuses nothing.

**The viewer-identity PoC.** Built and deleted the same day. `router.open` carried the hop out, Entra
authenticated the viewer at top level, Lighthouse minted a session for **that person**, a resolver
polled it back, and the frame rendered an authenticated Lighthouse inside Jira showing the viewer's
own name in the user menu (D47). Not "it rendered" — "it rendered as me".

The maintainer then closed the remaining question (D48, asked directly, answered *"nope."*): there is
no viewer-less case. Wallboards and kiosks were the only scenario needing a session with nobody
present, and Lighthouse is not serving them. **The API-key embed mode is dropped and interactive
viewer sign-in becomes the only mode.** Installation becomes zero-credential — an administrator
supplies a URL and nothing else.

With that, `POST /api/v1/embed/session-token` has no caller. A minting endpoint with no consumer is
dead code carrying a live security surface.

This ADR settles what replaces it.

## Decision

**Three hops. A viewer signs in to Lighthouse in a top-level tab; Lighthouse records the outcome
against a nonce the Jira page already holds; the Jira page polls that nonce and frames the result.**

| | Hop | Surface | Who is authenticated |
|---|---|---|---|
| 1 | `router.open` → `GET /embed/start?nonce=N` | Top-level tab, browser | Nobody yet — challenges OIDC when the interactive session cookie is absent |
| 2 | `GET /api/v1/embed/handshake/{nonce}` | Forge resolver, server-side | **Nobody. Unauthenticated by construction** — see the residual below |
| 3 | `GET /embed/enter?token=T` | The nested frame | The viewer, under `LighthouseEmbedCookie` |

Hop 1 is the whole point: it happens at top level, where no `X-Frame-Options` applies, against
**whatever identity provider that Lighthouse instance is already configured with**. Forge never
learns the provider exists. There is no `providers.auth` block, no per-customer manifest entry, no
per-tenant OAuth client and no reason for a customer to move their SSO anywhere (D43). The
maintainer's phrasing — *"the same provider you have set up for Lighthouse"* — becomes true by
construction rather than by configuration.

### The identity is the viewer's, and it is a subject, not a key

The handshake outcome carries `Subject` — the same stable string
`CurrentUserProfileService.ResolveStableSubject` produces (`sub`, falling back to `oid`) and the same
string `UserProfile.Subject` stores. It is deliberately **not** a foreign key to `UserProfile`:
profiles are created lazily and by two different writers
(`CurrentUserProfileService.GetOrCreateFromPrincipalAsync` and
`OidcGroupSnapshotWriter.WriteAsync`), so an FK would couple the credential's lifetime to a row whose
creation this design does not own. The subject is resolved to a profile **on every request** by the
cookie validator instead, which is what makes profile deletion end live frames.

### `/embed/start` requires the interactive session cookie, and challenges everything else

Security review **F2** found that an embed cookie carried `api_key_id` and could therefore mint its
own successor, making the 30-minute bound unbounded. The same shape exists here: an embed cookie
satisfies a bare `[Authorize]`, and if that were enough to start a handshake, a session would renew
itself forever.

The guard is stated positively rather than as an exclusion: **only a principal authenticated on
`SmartAuthSchemeSelector.CookieScheme` counts as signed in at `/embed/start`.** An embed-cookie-only
caller is not refused — it is *challenged*, completes an ordinary OIDC login, and arrives holding a
real session cookie. F2 closes by construction: an embed cookie is not a credential that can start a
handshake; it is simply not seen as one.

The check is written inside the action rather than as
`[Authorize(AuthenticationSchemes = CookieScheme)]`, for the reason already recorded on
`EmbedSessionController`: the scheme is not registered when authentication is disabled, and the
attribute would pre-empt the `AuthMode` 404.

### The handshake channel carries a grant **or** a refusal, and nothing else

D49: a viewer who authenticates but resolves to no Lighthouse access gets an explicit refusal, not an
empty Lighthouse. The refusal is produced by **Lighthouse**, where the permissions live, and merely
rendered by the Forge app — putting the decision in the app would repeat exactly the mistake findings
F2 and F4 recorded, a permission decision sitting somewhere other than the data it governs.

The refusal therefore travels the same channel as the grant, as a `RefusalCode`, so the Jira page can
stop polling and say something true. Three response shapes:

| Shape | When | Observable |
|---|---|---|
| **Grant** | The viewer signed in and holds at least one readable scope | `{ token, expiresAt }` |
| **Refusal** | The viewer signed in and holds nothing, or the instance refused for a stated reason | `{ refusalCode }` |
| **Pending** | Everything else | One response, indistinguishable from unknown, expired and already-read |

"Everything else" is load-bearing: **unknown, pending, expired and already-consumed are the same
response.** Under this design that is structural rather than careful, because a handshake row does not
exist until the outcome does — so "pending" and "unknown" are literally the same state of the
database. There is no existence oracle to suppress.

One bit does leak: an attacker who presented a correct nonce learns that a refusal happened rather
than a grant. Against a 256-bit nonce that is unreachable, and it is stated here rather than left for
someone to rediscover as a finding.

### Consumption is a conditional update, twice, over two different secrets

ADR-131's single-use discipline applies to both halves and for the same reason — the database performs
the atomicity, and a read-then-write would pass every test and lose the race in production.

- **The nonce** is consumed by the poll: clear `HandshakeNonceHash` where it matches and is not yet
  cleared and the row has not expired, then require exactly one affected row. The loser of a race gets
  the pending response, identical to every other miss.
- **The token** is consumed by the frame: ADR-131's existing `TryMarkRedeemedAsync`, unchanged.

The nonce is hashed at rest exactly as `SecretHash` is, and for the same reason: a database read
should not yield a live credential.

### RBAC is no longer untouched, and that is the price of viewer identity

ADR-129's headline property was that RBAC needed no change, because
`GetEffectivePermissionsAsync` reads `api_key_id` off the principal and everything else followed.
**That property does not survive.** The reason is one conjunct:

```csharp
// RbacAdministrationService.GetVirtualPermissionsAsync:1093
if (groupValues.Count == 0 && TryGetApiKeyId(principal, out _))
{
    groupValues = await LoadOwnerGroupSnapshotAsync(principal, cancellationToken);
}
```

The stored group snapshot — the mechanism that lets a principal without live `id_token` group claims
still resolve group-mapped roles — is gated on `api_key_id` being present. A viewer-identity embed
principal carries `sub` and, correctly, no `api_key_id`. It also carries no group claims, because it
is rebuilt from a stored subject rather than from a live token.

So a viewer whose entire Lighthouse access comes from an `RbacGroupMapping` resolves **zero
permissions inside the frame** while working perfectly in an ordinary tab. Worse for D49: the refusal
decision at `/embed/start` runs under the interactive OIDC principal, which *does* carry live group
claims, so Lighthouse would grant, then render nothing. **The refusal decision and the session it
authorises would evaluate different permission sets** — the exact failure D49 exists to prevent,
arriving through a different door.

**The change**: the embed principal carries `auth_method=embed`, and the snapshot fallback is gated on
`auth_method` ∈ {`api-key`, `embed`} rather than on `api_key_id`. That names the actual predicate —
*principals that structurally cannot carry live group claims* — instead of a proxy for it. An ordinary
OIDC cookie principal has no `auth_method` claim at all, so its behaviour is byte-identical to today
and the fail-open risk of a bare widening (a live token that genuinely returned zero groups silently
inheriting a stale snapshot) does not arise.

The snapshot is fresh by construction: the interactive login at hop 1 runs
`WriteGroupSnapshotOnTokenValidatedAsync`, which writes the snapshot the framed session will read
minutes later. That is why this is a correct design rather than a patch.

### The cookie validator re-resolves a subject, and must never create one

Security review **F3** found that deleting an API key did not end sessions already established from
it, and `RejectEmbedPrincipalWhoseKeyIsGone` was the answer. The carried-over equivalent re-resolves
the **subject** on every request and rejects the principal when no `UserProfile` matches, so deleting
a user (`IRbacAdministrationService.DeleteUserAsync`, which removes the profile and every role
assignment) ends their live frames within one request rather than within the cookie's lifetime.

Two things this ADR fixes in advance, because both are easy to get wrong and neither is visible in a
passing test:

1. **The lookup must be read-only.** `ICurrentUserProfileService.GetOrCreateFromPrincipalAsync`
   creates. Calling it from the validator would re-create the profile an administrator just deleted,
   on the deleted user's very next request, turning the entire control into a no-op that looks like it
   works. A separate read-only port is required, and it must not expose a create method — a driving
   port that only reads must not offer a write.
2. **Lighthouse has no user *deactivation*.** Only `DeleteUserAsync` exists. The control is "delete
   the user", and any documentation that says "deactivate" is describing a feature that is not there.

### What ADR-130 and ADR-131 keep

**ADR-130 is untouched, all of it.** The second cookie scheme, `.Lighthouse.Embed`,
`SameSite=None; Secure; Partitioned`, 30 minutes, `SlidingExpiration = false`, the untouched ordinary
cookie block, and F4's precedence inversion (an ordinary session cookie outranks an embed cookie in
`SmartAuthSchemeSelector`) all carry over unchanged and for unchanged reasons. Nothing in viewer
identity touches how the cookie reaches the browser.

**ADR-131 keeps** the database-backed store, the conditional-update single use, the 60-second token
expiry, opportunistic pruning, and its three rejected alternatives (memory, Redis, stateless +
denylist) — all of whose reasoning is about *topology*, which viewer identity does not change.

**ADR-131 loses** its identity binding and revocation lever 1. `ApiKeyId` becomes nullable and stops
being the thing a token belongs to; "deleting the API key revokes every token it minted" becomes
"deleting the user profile ends every session that names them", which is a stronger lever because it
acts on established sessions rather than only on unredeemed tokens. Lever 2 (revoke-all scoped to the
calling key) has no caller under D48 and is deleted. Lever 3 — the honest limit, that revoking a
*token* does not end a *session* — is superseded outright: the subject validator ends the session.

## Alternatives Considered

### A. Keep the API-key path alongside viewer identity — rejected

Two modes, chosen by an instance setting: API key for wallboards, viewer identity for people.

Rejected on the maintainer's answer (D48). The kiosk case is not one Lighthouse serves, and with it
gone the shared credential has no justification left: no `setSecret`, no key scoping at install, no
permanent "everyone sees what this key sees" disclaimer. Keeping it would mean keeping every finding
the security review recorded against it — F2's self-renewal, S1's customer credential in Atlassian's
infrastructure, S2's whole-authorization-boundary key — as maintained surface, in exchange for a
scenario nobody asked for. **D23's entire consequence stops existing rather than being mitigated**,
which is a strictly better outcome than any mitigation.

### B. A separate `EmbedHandshake` table beside `EmbedSessionToken` — rejected

Two entities, two lifecycles, two conditional updates, each with a single responsibility.

Genuinely cleaner on paper, and rejected for one concrete reason: **a second table needs a row before
the outcome exists**, or it is the same table with extra steps. A pre-registered handshake row means
an unauthenticated *write* endpoint the Forge resolver calls before opening the tab — a new
anonymous-write surface on the auth boundary, and a pending state that is now distinguishable from an
unknown one, which is the existence oracle D45 forbids. Creating the row only at resolution collapses
"pending" and "unknown" into "no row", and once that is true there is nothing left for a second table
to hold.

The cost is accepted and named: one table now carries a two-shaped outcome, and its name no longer
describes it. See "Consequences".

### C. Bind the handshake to the Forge installation — rejected as unachievable, not as undesirable

D45 said the nonce should be *"bound to the installation"*, which is the correct instinct and would
close the residual described below.

**It is unachievable under D48.** Binding requires something the two sides share, and zero-credential
install is precisely the property of D48 that removes it. Bootstrapping a per-installation secret on
first use does not rescue it: the first flow is unbound, the secret would ride the same visible URL,
and it re-introduces a stored credential — the thing D48 deleted. The two decisions are in genuine
tension and D48 wins, because the residual it leaves is narrower than the surface it removes.

### D. Copy the viewer's group claims into the embed principal at mint time — rejected

Store the group values on the handshake row and replay them into the embed `ClaimsIdentity`, avoiding
any RBAC change.

Rejected because it duplicates `UserProfile.LastKnownGroupClaimValues` into a second location with a
second staleness clock, and puts a group membership list into the credential store. The one-conjunct
change to `GetVirtualPermissionsAsync` is smaller, reads the value from where it already lives, and
makes the predicate honest.

### E. Redirect the top-level tab into the SPA after the grant — rejected

Cheapest possible hop-1 ending: no new HTML surface at all.

Rejected because it leaves the viewer in a second, full Lighthouse with no indication that the Jira
page is the one that matters. The tab is orphaned either way (D44); a static terminal page makes that
legible in one sentence, and it reuses the shape `EmbedEntryController.RefusalHtml` already
established.

## Consequences

**Positive**

- **The frame shows the viewer their own permissions.** This is the property the whole epic was
  testing for, and it is the only version that could ever be a marketplace product. ADR-129
  alternative C named it as the follow-up a *go* verdict would have to fund; the `router.open` finding
  brought it inside the epic instead.
- **Installation is zero-credential.** No API key, no `setSecret`, no scoping decision at install, no
  customer credential in Atlassian's infrastructure. S1 — the security review's headline item, and the
  one it said any marketplace-grade successor must repeat verbatim — **stops existing**.
- **F2 and F4 stop being mitigations and become structural.** There is no shared identity to be
  swapped into, so the forced-login variant of F4 loses its payload; and an embed cookie cannot start
  a handshake, so the renewal loop has no entry point.
- **Revocation gets stronger.** Deleting a user ends their live frames within one request. The old
  lever only stopped unredeemed tokens.
- Net **deletion** on the auth surface: two endpoints, one revocation path and one principal-factory
  call site go away; three endpoints arrive, one of which replaces two.

**Negative**

- **The handshake is unauthenticated and cannot be made otherwise.** Assessed at length in the feature
  record; the residual is nonce disclosure through the modal Atlassian is *required* to display, not
  brute force. Entropy, TTL, single use and rate limiting are all necessary and none of them addresses
  it. Accepted for a feasibility epic with the blast radius stated: one 30-minute non-sliding session
  as the viewer, no renewal, ended by deleting the user.
- **RBAC is modified.** One conjunct, in the most security-sensitive method in the codebase. ADR-129's
  "no RBAC change" property is spent.
- **`EmbedSessionToken` now names a row that may hold no token.** The entity keeps its name this
  release because renaming a table is a destructive migration and the project is expand-only. The
  rename rides the same contract-phase drop that removes the `ApiKeyId` column. Recorded rather than
  fixed, in the same spirit as the `RecordedAt` / `RecordedDay` split already carried elsewhere.
- **`ApiKeyPrincipalFactory` builds principals for people.** Same treatment, same reason.
- **D29's fail-open alarm weakens.** "A principal carrying `sub` but not `api_key_id` silently widens
  to the owner's full scope" was the loudest safety signal in ADR-129. For a viewer principal, an
  absent `api_key_id` is *correct*, so the signal no longer separates a bug from the normal case.
  `auth_method=embed` is its replacement, and it is a weaker one until slice 03 removes the API-key
  branch entirely — after which the invariant becomes "an embed principal carries `sub` and never
  `api_key_id`", which is cleaner than what it replaces.
- **One extra database read per request under the embed cookie**, resolving the subject. Parity with
  what `RejectEmbedPrincipalWhoseKeyIsGone` already costs, and `UserProfile.Subject` carries a unique
  index (`LighthouseAppContext.cs:103-105`), so it is one indexed read on a path that is already
  loading an SPA.
- **A first-time viewer's click creates a `UserProfile` row.** `/embed/start` must resolve a profile
  before it can ask RBAC anything, and resolution creates. Every curious Jira user therefore appears
  in the customer's user list. This is not new behaviour — the SPA's `/auth/me` does the same on first
  load — but the Jira page makes it one click away for a much wider audience, and an administrator
  should be told.

**Quality attribute impact**

- **Security**: the sensitivity point moves. It was *"a shared credential in a third party's
  storage"*; it is now *"an unauthenticated correlator displayed on screen"*. The second is narrower
  in blast radius (one viewer's session, not everyone's) and narrower in attacker (someone watching
  the screen, not anyone holding the key), but it is no longer bounded by a credential the customer
  controls.
- **Maintainability**: improved. One mode instead of two, one identity concept instead of a shared
  one, and a net reduction in auth-surface code.
- **Testability**: improved, materially. The E2E suite already performs a real OIDC login against
  Keycloak (`tests/specs/auth/Auth.spec.ts` → `KeycloakLoginPage.login`), so hop 1 is exercisable end
  to end with no mocking — which the API-key design's browser question never was.
- **Usability**: worse by one step. The viewer must complete a sign-in, in a tab, past an Atlassian
  modal naming the destination, on every session. The API-key design asked nothing of them. That cost
  buys their own name in the user menu and their own permissions in the frame, and the maintainer has
  judged the trade (D48).
