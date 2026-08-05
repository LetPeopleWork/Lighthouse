# Verdict — Epic 5146, Lightweight Jira App

**Status: DRAFT. The technical question is answered. The business question has not been asked.**
Written 2026-08-05, at the point where building stopped and pitching had not yet started.

## What the epic set out to learn

Whether "is there a Jira app?" is a real buying trigger or a conversational throwaway — cheaply, without
building a marketplace-grade product. D6 fixed the exit as a **written verdict, not shipped software**.

## What is now known

### The mechanism works, and it has been seen working

An authenticated Lighthouse renders inside a `jira:globalPage` on a real Atlassian site, entered by a
single-use token, with **no login and no identity provider declared in the Forge manifest**. Proven
live 2026-08-04 (Firefox), and again 2026-08-05 through the admin page and resolver against real
portfolios and teams. Tailscale's edge adds no framing headers, so the transport does not interfere.

### The founding premise was wrong, and this is the epic's most valuable finding

The epic assumed **zero Lighthouse changes**. That is dead. Framing an authenticated instance requires
Lighthouse to mint an embed session, which is now on `main`: one token exchange, one embed entry point,
and a cookie policy scoped to sessions it issues. The existing authentication flow, the RBAC model and
the permission vocabulary are untouched, and `SameSite=Lax` still governs every other session.

### Per-viewer identity is not reachable without changing Lighthouse's login

The frame carries one shared API key's identity: everyone opening the Jira page sees exactly what that
key sees. Acceptable for a demo, disqualifying for a product.

The intended fix — Lighthouse running Atlassian as its OIDC provider, so the viewer Forge names is
already a Lighthouse user — **is not available**. Atlassian publishes a real OIDC discovery document
that third-party apps cannot use: adding `openid` to the developer console's own working authorize URL
breaks it. Atlassian is an OIDC *consumer*, and an OIDC *provider* only for Bitbucket Pipelines. There
is no "sign in with Atlassian" for third-party web applications. Full evidence in `spike/findings.md`.

Forge itself can still identify the viewer — the Forge Invocation Token carries a `principal` claim,
verifiable against Atlassian's JWKS, needing no API key. What is missing is only the join from that
account to a Lighthouse user, which would cost either an email lookup through the User Identity API or
an entire new authentication adapter. **Maintainer decision 2026-08-05: not worth changing the whole
login for.** #5664 Removed.

### "Point it at my own Lighthouse" is not delivered

Forge frames and fetches only manifest-declared origins. A prospect's own instance needs a redeploy, a
version bump and a re-consent — none of which a customer can perform. Customer-managed egress (#5663)
is the fix and is still open. It is narrower than assumed: the manifest must declare a `configurable`
object with optional `supportedPatterns`, so it is "any URL matching a pattern we declared", limited to
10 groups × 10 domains / 40 entries, still a Forge Preview, and not eligible for data residency.

Until it is built, every demo shows **our** data, not the prospect's — which is the difference between
a persuasive demo and a canned one.

### Two defects that would meet a prospect before any chart does

1. The `embed_url_insecure` guidance names `LIGHTHOUSE_AUTHENTICATION__TRUSTEDPROXIES`. **No such
   configuration prefix exists.** The working name is `Authentication__TrustedProxies__0`. Following
   the instruction verbatim changes nothing, silently.
2. **Any TLS-terminating reverse proxy breaks the embed flow**, and nothing documents it. The scheme
   reads `http`, an `http://` embed URL is minted, the frame blocks it as mixed content, and the
   `Secure` embed cookie never comes back. Every prospect behind nginx, Traefik, Caddy or a cloud load
   balancer hits this first.

Both were found in minutes because the app explains its own failures well. The detection is good; the
guidance text is wrong.

## The verdict

**Not yet reachable.** The learning hypothesis — *Jira-nativeness is a real buying trigger rather than a
conversational throwaway* — is tested by prospect reaction, and **K1 = 0: no prospect has seen it.**
AC2 asks for three dated conversation notes capturing what someone said *unprompted*. There are none.

Declaring a go on technical success would answer a question nobody asked. The failure mode this epic
was built to detect is **a shrug**, and a shrug is only observable in front of a person.

**Next step is a pitch, not a build.** Nothing further is committed to the Forge app until the idea has
been put in front of prospects with the working demo.

## Open decisions

| Decision | Status |
|---|---|
| Does the embed session stay in Lighthouse on a no-go? | **Open.** It is on `main` now and gets harder to remove the longer it ships. Separate from the verdict. |
| #5663 customer-managed egress | **Open.** Real work, and it is what makes a demo show the prospect's own data. |
| File the two defects above as bugs? | **Open**, not yet filed. |
| Chrome and Safari | **Untested.** Safari unreachable on Linux. |
| Forge round trip, resolver, `setSecret`, nested frame | **No tests exist**, in any form. |
| Environment teardown (M8) | **Owed.** Funnel is currently up for the pitch; Forge app installed at 5.2.0. |

## Known limitations, for the README a prospect reads

- Jira **Cloud** only.
- **https** only, and behind a TLS-terminating proxy the instance must declare that proxy.
- The frame shows what **one API key** can see — not per-viewer permissions.
- The instance origin must be **declared in the manifest before the call**; a prospect's own URL is not
  yet accepted at runtime.
- Frame height is a fixed number: `view.resize()` does not exist and `window.parentIFrame` is absent in
  Forge Custom UI, so both sizing levers are closed.
- Double navigation — Jira's chrome plus Lighthouse's own — remains, and whether it matters is one of
  the things the pitch should reveal without being asked.

## If the pitch says go

The Forge app's quality bar is deliberately low and D7 put it in a separate repository so a throwaway
showcase would not face this repository's gates. That expires the moment anyone decides to keep it:
coding guidelines and a linter, SonarQube, an actual test suite (there is none), CI/CD, dependency
scanning (three high transitive findings via `@forge/bridge`, unreachable today), and a release process
— every origin change is a major version bump requiring re-consent from every installation.

That gap belongs in the estimate, not discovered afterwards.
