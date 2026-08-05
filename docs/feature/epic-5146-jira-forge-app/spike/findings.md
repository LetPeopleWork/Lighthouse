# SPIKE findings — epic 5146, slice 02 step 1: the `Partitioned` wire probe

**Date**: 2026-08-04 · **Verdict**: **WORKS**, at rung 2 of OQ-1's four-rung ladder.

## The assumption under test

Can ASP.NET Core on `net10.0` emit the `Partitioned` attribute on a `Set-Cookie` response header at
all? D28's second cookie scheme and D40's lifetime both rest on it, and ADR-130 recorded it as
genuinely unresolved. If no rung reached the wire, the embed session could not work in browsers that
require CHIPS, and that would have been a verdict finding for the epic rather than a bug to chase.

Chosen as the first step of slice 02 because it needs no tunnel, no Forge, no network and no design
decision — and because it was the last remaining thing that could kill the approach for free.

## What was run

A throwaway minimal-API app in `/tmp/spike_5146_partitioned/`, self-hosted on `127.0.0.1:5199`,
calling itself with `HttpClient` and printing the literal `Set-Cookie` headers it received. Rung 1
was set reflectively so that an absent property would report itself instead of failing the build.

## Result

```
CookieOptions:  Partitioned property = ABSENT, Extensions = present
CookieBuilder:  Partitioned property = ABSENT, Extensions = present
runtime: 10.0.10, aspnetcore: 10.0.0.0

rung1  first-class property     → CookieOptions.Partitioned does not exist; no header
rung2  CookieOptions.Extensions → .Probe.Rung2=v; path=/; secure; samesite=none; httponly; Partitioned
rung2b CookieBuilder.Extensions → .Probe.Rung2b=v; path=/; secure; samesite=none; httponly; Partitioned
rung3  raw header append        → works; not needed
```

| Rung | Mechanism | Reaches the wire |
|---|---|---|
| 1 | A first-class `CookieOptions` / `CookieBuilder` property | **No — the property does not exist on `net10.0`** |
| 2 | `CookieOptions.Extensions.Add("Partitioned")` | **Yes** |
| 2b | `CookieBuilder.Extensions.Add("Partitioned")` | **Yes** — and this is the shape that matters |
| 3 | `OnAppendCookie` / raw response-header append | Yes, but unnecessary — the ladder stops above it |

## Why rung 2b is the finding, not rung 2

Rung 2 proves the framework can serialize the attribute. **Rung 2b proves the production shape can.**
A cookie authentication scheme configures a `CookieBuilder`, not a `CookieOptions` — the handler calls
`Options.Cookie.Build(context)` and appends the result. Rung 2b exercises exactly that path, so D28's
second scheme carries `Partitioned` by configuration alone: no `OnAppendCookie` hook, no raw header
append, no middleware ordering to get wrong, and nothing that a future ASP.NET Core upgrade could
silently reorder around.

## Design implications

- **ADR-130's open ladder is closed at rung 2.** The negative consequence it recorded — that
  `Partitioned` support was unresolved — is resolved on the server side.
- **`Secure` is a hard prerequisite, not a preference.** The probe set `Secure` and `SameSite=None`
  on every rung; a partitioned cookie without `Secure` is not a cookie any browser will accept. This
  is already D24's shape, so nothing changes — it just stops being optional.
- **Nothing here says a browser accepts it.** The probe answers *can we emit it*. Whether Chrome,
  Firefox and Safari honour it in a real cross-site frame is steps 3 and 6, and it remains the
  question that can still kill the approach. Do not let this green result be read as the cookie
  question being answered.
- The string is unvalidated by the framework. `Extensions` appends whatever it is given, so the
  attribute name is a literal with no compile-time protection — worth one assertion on the literal
  header in step 2's test rather than trusting the call site.

## Edge cases and limits of this probe

- Plain HTTP on loopback. The `Secure` attribute was emitted regardless, because emitting is a
  server-side concern; the browser is what enforces it. Real HTTPS is steps 5–6.
- Not run through the cookie authentication handler itself — rung 2b simulates its call sequence.
  Step 2's `WebApplicationFactory` test asserts the literal header from a real `AddCookie` scheme,
  which is where that last inch gets closed.
- One SDK, one machine: `10.0.110`, runtime `10.0.10`. Recorded because "it works on `net10.0`" is a
  claim about a version, and CI should agree before anything depends on it.

---

# Probe 2 — mode A: can Forge authenticate the viewer to an external Lighthouse? (2026-08-05)

Run after the maintainer chose **mode A** (see #5664) and dropped mode B story-wise. Three questions,
answered in one sitting: **P1** (can Lighthouse use Atlassian as its OIDC provider), **P2** (can Forge
hand an external Lighthouse a verifiable viewer assertion), **Q2** (does customer-managed egress reach
these module types).

## P1 — DOESN'T WORK. Atlassian is not an OIDC provider for third-party apps.

`https://auth.atlassian.com/.well-known/openid-configuration` returns **200** with a complete, real
document: `issuer` `https://auth.atlassian.com`, a `userinfo_endpoint`, `openid`/`profile`/`email` in
`scopes_supported`, PKCE `S256`, `code` response type. It is unmistakably an Auth0 tenant
(`mfa_challenge_endpoint`, `/oidc/register`).

**It is unusable from a 3LO app.** Bisected live against a real OAuth 2.0 (3LO) integration
("Lighthouse in Jira Test", account-level, scope `read:me`) with callback
`http://localhost:5169/api/auth/callback`:

| Authorize request | Result |
|---|---|
| `audience=api.atlassian.com` + `scope=read:me` (console's own URL) | **consent screen, then Lighthouse's callback** |
| same + `openid` added to scope | `Something went wrong / There's nothing here` |
| `audience` + `openid profile email` (no API scope) | `Something went wrong` |
| `audience` + `openid profile email read:me` | `Something went wrong` |
| no `audience`, `openid profile email` (what Lighthouse sends) | 302 to login, then `The authorize request was incomplete or invalid` |

One variable changed from a known-good URL is what makes this decisive: adding `openid` to the
console's own working URL breaks it. The `audience` parameter is required for 3LO but is **not** the
blocker — `openid` is.

**Corrects the record.** DESIGN's claim that "Atlassian publishes a real OIDC discovery document and
Lighthouse's OIDC is generic enough to point at it" is half right and the wrong half is load-bearing.
The document is real. It serves Atlassian's own tenant, not third-party integrations.

**Method note, recorded because it cost a wrong call twice**: a `302` to `id.atlassian.com/login` from
`curl` proves only that the request was not rejected up front. Both failing variants produced that same
302. Authorize-parameter validity is only observable *after* the login page, in a browser.

**Consequence**: mode A's precondition — Lighthouse on Atlassian OIDC, so OIDC `sub` == `accountId`,
no mapping table — is **unreachable**. The identity join that D-decision ruled out is back.

## P2 — WORKS, on paper. Forge Remote + Forge Invocation Token.

Not yet exercised live; read from current Atlassian documentation.

- Lighthouse becomes a Forge **remote**, not something a resolver calls. FIT arrives as a bearer token.
- Verify against `https://forge.cdn.prod.atlassian-dev.net/.well-known/jwks.json`, with
  `iss` = `forge/invocation-token` and `aud` = the app id. **That pinning is the trust anchor** — it is
  what stops any other Forge app asserting identities at us. No shared secret, no API key.
- **`principal` claim = the invoking user's account id, "UI modules only"** — `jira:globalPage` and
  `jira:adminPage` are UI modules.
- `requestRemote` goes straight from the frame to the remote and **omits OAuth tokens**; FIT is still
  included. Routing through an `endpoint` module instead also carries `x-forge-oauth-user`.

**The route P1's failure opens up**: go through the `endpoint` module, use `x-forge-oauth-user` to call
the User Identity API `/me`, and join on **email** against Lighthouse's existing users — under whatever
IdP the customer already runs. Lighthouse keeps its own identity provider and the Atlassian-OIDC
precondition disappears. Arguably better than what was designed: no customer has to move their SSO to
Atlassian to embed a page. Alternative join: an accountId stored per Lighthouse user, set once.

Unverified and load-bearing: the exact string form of `principal` (documentation example is the legacy
`655362:312d3308-…` shape, not the modern `5b10a2844c20165700ede21g`), and whether `remotes` +
`endpoint` are accepted on `jira:globalPage` / `jira:adminPage` specifically — the docs list module
*categories* and show `confluence:globalPage`, without naming the Jira ones.

## Q2 — WORKS, narrower than D9 assumed. (#5663)

Customer-managed egress and remotes let an admin approve a runtime-supplied origin, but the manifest
must declare a `configurable` object, optionally with `supportedPatterns` to validate what the admin
types. So "point it at any URL the user types" is really "any URL matching a pattern we declared".
Limits confirmed: **10 egress groups per installation, 10 domains each, 40 entries total**. Still a
Forge **Preview** feature. Applies to remotes as well as egress. Customer-managed remotes are **not
eligible for data residency**, on top of the already-forfeited "Runs on Atlassian".

## Environment

`lighthouse-app` (docker-compose) is up on `:48331`, auth **Enabled**. The dev instance was
reconfigured onto Atlassian OIDC for P1 and runs on `:5169`. `tailscaled` is **down** — the funnel was
not needed for any of the above. Forge app still installed on the real site at `5.2.0`, pointing at a
dead origin.
