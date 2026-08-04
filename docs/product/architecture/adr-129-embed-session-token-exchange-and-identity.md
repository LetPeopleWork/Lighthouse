# ADR-129: Embed session — token exchange and identity model

**Status**: Accepted
**Date**: 2026-08-04
**Feature**: `epic-5146-jira-forge-app` (ADO Epic 5146, Story 5641)
**Decider**: Morgan (Solution Architect), DESIGN re-run after slice 01

---

## Context

Slice 01 of Epic 5146 ran live on 2026-08-03 and produced findings F3/F4: a Lighthouse instance
frames correctly inside a Jira Forge page, but the login redirect does not. The identity provider
refuses to be framed with `X-Frame-Options`, and this is a category result — Auth0 Universal Login is
deliberately un-framable, and Entra, Okta and Keycloak default the same way. No manifest declaration
reaches past it.

The consequence: a third-party frame can never complete an interactive login. If a framed Lighthouse
is to show anything but a sign-in wall, the session must be established **inside the frame, without
an identity-provider hop**.

The blocking question was *whose identity does an embed session carry?* Answered by the maintainer on
2026-08-03 (D23): the identity of a scoped Lighthouse API key that the Jira administrator supplies.
Everyone who opens the Jira page sees exactly what that key sees.

This ADR settles how that identity reaches a browser session.

## Decision

**A two-step exchange. The caller presents an API key over `X-Api-Key` to a new token endpoint and
receives a short-lived, single-use, opaque token; the browser then presents that token once to an
embed entry point, which converts it into a cookie session carrying the API key's principal.**

### Endpoint contract

| | |
|---|---|
| Route | `POST /api/v1/embed/session-token`, dual-routed `api/latest` per the project convention |
| Authentication | `X-Api-Key`. No new scheme — `SmartAuthSchemeSelector.Select` already routes any request carrying that header to `ApiKeyAuthenticationHandler` (`Program.cs:613`, `SmartAuthSchemeSelector.cs`) |
| Authorization | The default fallback policy (`Program.cs:577-580`, `RequireAuthenticatedUser`). No `RbacGuard` — minting a session for *your own* identity requires no privilege beyond holding the key |
| Rate limiting | New `EnableRateLimiting` policy, registered alongside the three existing ones (ADR-005) |
| Availability | `404` unless `AuthMode` is **`Enabled`** — under both `Disabled` and `Blocked` (settled 2026-08-04). With authentication disabled there is no cookie scheme to sign into. Under `Blocked`, `BlockedModeFilter` permits only `/api/latest/auth`, `/license` and `/version`, so a minted session would meet a 403 on every data endpoint — correct and useless. This is deliberately **narrower** than `AuthController.Login`'s guard (`AuthController.cs:41-45`), which still accepts `Blocked`: a login into a blocked instance can at least reach the licence page, and a framed embed cannot |
| Response | `{ token, expiresAt, embedUrl }`. `embedUrl` is the absolute entry-point URL with the token already applied, so the caller never composes it |

### Token shape

The token is **opaque to its bearer and backed by server-side state** — a random identifier plus a
random secret, in the form `{tokenId}.{secret}`. `tokenId` is the indexed database lookup key;
`secret` is verified in constant time against a stored hash. Lifecycle, storage and revocation are
[ADR-131](./adr-131-embed-token-lifecycle-and-revocation-store.md).

Two constraints bind the implementation and are architectural, not stylistic:

- **The secret is high-entropy random, so a fast digest is correct and a password KDF is not.**
  `ApiKeyService.FindMatchingKey` iterates every key and runs PBKDF2 at 100 000 iterations per row
  (`ApiKeyService.cs`). That is right for a credential a human may have chosen badly and wrong for a
  256-bit random value redeemed on every page load. The `tokenId`/`secret` split exists precisely so
  redemption is one indexed lookup and one constant-time comparison.
- **Comparison is constant-time.** `CryptographicOperations.FixedTimeEquals`, as the existing key
  path already does.

### The browser hand-off — the token travels in the query string

**Settled 2026-08-04** by the maintainer: *"lets go with query param first, this is just about
feasibility, so we can live with this."* The entry point is reached as `GET /embed/enter?token=…`, and
the token is therefore in a URL: browser history, `Referer` headers, and any proxy or Atlassian log on
the path.

The feasibility framing is what makes the decision **quick to take**, not what makes it safe. It is
accepted with three mitigations that are part of the slice rather than follow-ups, and those do not
travel with the framing — a later slice that stops being "just about feasibility" inherits the
mitigations already in place rather than discovering it needs them:

- `Referrer-Policy: no-referrer` on the entry-point response.
- **A 302 to a clean URL immediately after the cookie is set**, so history and access logs hold the
  token exactly once — and hold it already spent.
- Token scrubbing named explicitly in the security-review checklist, rather than left to a logging
  configuration nobody reviews.

The reason acceptance is defensible rather than lazy: the token's **60-second single-use** window
(ADR-131) means a value written to a log is a dead credential before anyone could read it. The
exposure is of something already spent.

The POST-based hand-off — an auto-submitting form instead of a plain navigation — was weighed and is
**not taken now, but stays available**. It would cost the "an iframe `src` is just a URL" simplicity
the whole wrapper approach is built on, including turning the verification harness from a static
`<iframe src>` into a form-submitting page, which adds moving parts to precisely the experiment whose
job is a clean yes or no. Critically, **switching later is a change on the Forge side, not in
Lighthouse** — the entry point would accept the token from a form post with no change to identity,
lifetime or cookie semantics. The decision is cheap to reverse, which is why it can be made early.

### Identity model — claims parity is the invariant

The principal signed into the embed cookie **must be claim-for-claim the principal
`ApiKeyAuthenticationHandler` produces for the same key**: `sub` (owner subject), `name`,
`ClaimTypes.Name`, `auth_method=api-key`, and `api_key_id`.

This is not a convenience. `RbacAdministrationService.GetEffectivePermissionsAsync` reads
`api_key_id` **off the principal**, not off the request headers and not off the authentication scheme
(`RbacAdministrationService.cs:961-984`, `:1009-1019`). A cookie-borne principal carrying the same
claims therefore resolves through the identical `IntersectWithApiKeyScope` path as a header-borne one.
Per-key scoping (ADR-004) applies to the embed session unchanged, and no new authorization model,
permission vocabulary or guard is introduced.

The enforcement: **one claims-construction function, called by both `ApiKeyAuthenticationHandler` and
the embed redemption path.** If the two construct claims independently they will drift, and the drift
is silent — the embed session would keep authenticating while quietly resolving different permissions.
An integration test asserting the two principals are claim-equivalent for the same key is the probe.

### The exchange refuses an unlinked key

`ApiKeyService.ValidateApiKeyWithOwnerAsync` returns `ApiKeyOwnerResolutionState.Unlinked` when a key
authenticates but its owner cannot be resolved, and `ApiKeyAuthenticationHandler` then emits **no
`sub` claim**. `CurrentUserProfileService.GetOrCreateFromPrincipalAsync` returns `null` without a
stable subject (`CurrentUserProfileService.cs:17-22`), and every scoped RBAC check returns `false` on
a null profile (`RbacAdministrationService.cs:174-178` and its five siblings).

An embed session minted from an unlinked key would therefore authenticate successfully and render an
empty Lighthouse — the exact blank-rectangle failure mode D13 exists to prevent, one layer deeper.
**The exchange refuses such a key with a structured reason** rather than issuing a token that is
guaranteed to disappoint. This is Earned Trust at the boundary: the endpoint proves the key can honour
the contract before it hands out a credential that promises it can.

## Alternatives Considered

### A. Self-contained signed token (JWT), no server-side state — rejected

Mint a short-lived JWT carrying the API key id and owner subject, signed with a key derived from Data
Protection, and validate it statelessly at the entry point.

Rejected because **single use cannot be expressed statelessly.** A token that grants a session is a
bearer credential; making it single-use requires a server-side record of what has been redeemed, at
which point the stateless property is gone and the signing-key management is pure added cost. The JWT
would also invite the mistake of lengthening its lifetime to avoid the state — trading the one
property that bounds the damage of an intercepted token.

Secondary: the project already has a JWT bearer scheme for MCP/CLI callers (ADR-079,
`Program.cs:615`). A second, differently-issued JWT on the same surface is a reviewability cost with
no offsetting benefit.

### B. Reuse the API key directly as the frame's credential — rejected

Have the Forge app put the API key into the framed URL, or have the entry point accept `X-Api-Key`
and sign in directly.

Rejected because it puts a **long-lived** credential into a URL, a browser history, a referrer header
and Atlassian's logs. The whole point of the exchange is that what crosses into the browser is short-
lived, single-use and revocable, while the durable credential never leaves the Forge resolver's
backend fetch. Collapsing the two steps discards exactly the property that makes the design
reviewable.

### C. Jira identity mapped to a Lighthouse user — rejected for this epic, named as the follow-up

The correct end state, and the only version that could ever be a marketplace product. Rejected here
because it is D5's deferred step-2 authentication in full: a trust path between Atlassian and
Lighthouse that does not exist today, larger than the rest of the epic combined. Recorded in
`docs/verdict.md` as the follow-up a *go* verdict must name.

### D. Instance-level anonymous read-only embed mode — rejected

Marginally smaller than the token exchange. Rejected on 2026-08-03 (D23) because it puts the scope in
Lighthouse's own configuration rather than in something the customer's administrator controls per
installation, and because "anonymous read-only" is a new authorization concept — precisely the kind of
new vocabulary K4 forbids.

## Consequences

**Positive**

- Per-key least privilege reaches the framed session for free. A read-scoped key produces a read-only
  embed, enforced by the same `RbacGuard` path every other caller travels.
- The durable credential stays server-side in the Forge resolver; only a 60-second single-use token
  crosses into a browser.
- No change to the interactive login flow, the RBAC model, or the permission vocabulary — K4 as
  reworded on 2026-08-03 stays true and stays falsifiable.

**Negative**

- Everyone who can open the Jira page shares one identity. Acceptable for a demo and for small teams;
  explicitly *not* acceptable as a product, and recorded as such in the verdict.
- The administrator's Lighthouse API key lives in Atlassian's Forge storage. That is a customer secret
  in a third party's infrastructure and it must be named in the security review and in the verdict.
- The token appears in a URL, and no mitigation removes that — they only bound how long it matters and
  how many places it lands. A deployment that logs full query strings to a long-lived aggregator is
  storing spent credentials, which is untidy even when it is not dangerous.
- One new endpoint, one new entry point, one new table on the auth surface. The diff is bounded to
  that surface plus its tests, which is what K4 now measures.
- The framed SPA shows the *key owner's* display name and a sign-out control that would strand the
  frame. Accepted for slices 02–03 and recorded as a verdict finding rather than fixed in the
  frontend.

**Quality attribute impact**

- Security: the sensitivity point of the whole feature. Bounded deliberately — see
  [ADR-130](./adr-130-embed-only-cookie-policy.md) for the cookie blast radius and
  [ADR-131](./adr-131-embed-token-lifecycle-and-revocation-store.md) for the credential lifetime.
- Maintainability: improved by refusing a second authorization model. The cost is the claims-parity
  invariant, which is a test, not a convention.
- Testability: the whole flow is exercisable without Forge — one HTTP call to the exchange, one
  browser navigation to the entry point.
