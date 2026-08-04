# ADR-130: Embed-only cookie policy — a second cookie scheme, not a relaxed global one

**Status**: Accepted
**Date**: 2026-08-04
**Feature**: `epic-5146-jira-forge-app` (ADO Epic 5146, Story 5641)
**Decider**: Morgan (Solution Architect), DESIGN re-run after slice 01

---

## Context

Slice 01 finding F5: `Program.cs:643` sets `options.Cookie.SameSite = SameSiteMode.Lax`
unconditionally on `.Lighthouse.Session`. A session established inside a cross-site frame would
produce a cookie the browser declines to send back from that frame — a second wall standing behind the
identity-provider wall of F3/F4.

[ADR-129](./adr-129-embed-session-token-exchange-and-identity.md) establishes the embed session
without an identity-provider hop. This ADR settles how that session's cookie reaches the browser with
attributes a cross-site frame will honour, **without** weakening any other session.

D24, decided by the maintainer on 2026-08-03, is the constraint: only the cookie issued by the embed
exchange relaxes `SameSite`. Instance-wide relaxation would weaken every session on every Lighthouse
deployment to serve a feature most of them never enable. Confining it makes the security review
answerable — the question becomes *"is this token safe?"* rather than *"have we weakened everyone's
cookies?"*

## Decision

**Register a second cookie authentication scheme with its own cookie name and its own attributes.**
`.AddCookie(EmbedCookieScheme, …)` sits alongside the existing default `.AddCookie(…)` block, which is
not touched.

| Property | Ordinary session (`Program.cs:639-671`) | Embed session (new) |
|---|---|---|
| Scheme | `CookieAuthenticationDefaults.AuthenticationScheme` | `LighthouseEmbedCookie` |
| Cookie name | `.Lighthouse.Session` | `.Lighthouse.Embed` |
| `SameSite` | `Lax` | `None` |
| `Secure` | `Always` | `Always` |
| `Partitioned` | absent | **set** |
| `HttpOnly` | `true` | `true` |
| `ExpireTimeSpan` | `authConfig.SessionLifetimeMinutes` | **30 minutes** (settled 2026-08-04), configurable, own key |
| `SlidingExpiration` | `true` | **`false`** |

Two names means an embed session and an ordinary session coexist in one browser without clobbering
each other — which matters immediately, because the maintainer demoing inside Jira is usually signed
into the same Lighthouse in another tab.

`SlidingExpiration = false` is deliberate. The ordinary session renews itself because a human is
driving it; the embed session should not outlive its window, because the Forge resolver can always
mint a fresh token and because a cookie that renews itself indefinitely defeats the short lifetime
that bounds ADR-131's revocation gap.

**30 minutes, settled 2026-08-04.** The number falls out of the re-mint being cheap: every page open
exchanges the API key again, so expiry costs a round trip rather than a login. That argues short.
Non-sliding then means a frame left open all day cannot outlive its window — and since this *is* the
bound on ADR-131's revocation gap, a sliding embed cookie would quietly make revocation unbounded.
The only person inconvenienced is one staring at an untouched frame past the half hour, and ADR-129's
entry point makes that failure legible rather than blank.

**Routing.** `SmartAuthSchemeSelector.Select` currently decides on headers alone
(`Program.cs:606-607` passes `ctx.Request.Headers`). It gains one branch: a request carrying the embed
cookie forwards to the embed scheme. The existing precedence is preserved — `X-Api-Key` first,
`Authorization: Bearer` second, then embed cookie, then the ordinary cookie scheme. The selector stays
a pure function over the request; the change is one more input, not a new mechanism.

### `Partitioned` — stated as unverified, with a ladder and a probe

The backend targets `net10.0` (`Lighthouse.Backend/Lighthouse.Backend.csproj:4`). **Whether a
first-class `Partitioned` property exists on `CookieBuilder` at that TFM was not verified during this
DESIGN wave** — the session had no shell and no access to the reference assemblies. Asserting it would
be exactly the unearned trust this project's principles forbid. The ladder, in preference order:

1. A first-class `CookieBuilder.Partitioned` property, if it exists at this TFM.
2. `options.Cookie.Extensions.Add("Partitioned")` — `CookieBuilder.Extensions` appends raw attributes
   to `Set-Cookie` and is the documented CHIPS route in ASP.NET Core.
3. `CookiePolicyOptions.OnAppendCookie`, appending the attribute for the embed cookie name only. Costs
   a global middleware (`UseCookiePolicy` is not registered today) for one cookie — hence third.
4. If none of the above puts the attribute on the wire, the approach cannot serve browsers that
   require CHIPS. That is a **verdict finding, not a bug to chase.**

**This probe is the first thing slice 02 does** (confirmed 2026-08-04). It needs no tunnel, no second
site and no browser — and if no rung puts the attribute on the wire, every later step in the slice is
moot and the answer is already a verdict finding. Buying it first is the cheapest possible order.

**The probe that decides it** — and the enforcement of D24 — is one integration test over
`WebApplicationFactory` asserting the literal `Set-Cookie` header:

- the embed entry point's response contains `SameSite=None`, `Secure` and `Partitioned`;
- the ordinary sign-in path's response still contains `SameSite=Lax`.

Both halves matter. The second is the one that catches a future well-meaning edit to `Program.cs:643`,
and it is the reason D24 is enforced rather than merely documented.

## Alternatives Considered

### A. Relax the existing block instance-wide, behind configuration — rejected

Turn `Program.cs:643` into `options.Cookie.SameSite = authConfig.CookieSameSite`.

Rejected on the maintainer's decision D24 and on its own merits. It makes the blast radius of the
feature equal to *every session on every deployment*, including deployments that never enable the Jira
app. It also makes the security review unanswerable in the form that matters: a reviewer cannot
approve "the embed token is safe" when the change on the table is "every operator can now weaken every
cookie". Worse, it is a config flag whose wrong setting is invisible until exploited.

### B. Per-request mutation of the existing options — rejected

Set `SameSite`/`Partitioned` on `options.Cookie` at sign-in time for embed requests only.

Rejected as **incorrect, not merely inelegant.** `CookieAuthenticationOptions` is resolved once and
shared; mutating it per request is a data race that would intermittently mis-attribute ordinary
sessions under concurrency. It would also pass every single-threaded test.

### C. `CookiePolicyOptions.OnAppendCookie` as the primary mechanism — rejected as primary, retained as fallback

Intercept cookie append globally and rewrite attributes for the embed cookie name.

Rejected as the primary mechanism because it is action at a distance: the cookie's attributes would
live in a different file from the scheme that issues it, and a reader of the `.AddCookie` block would
see attributes that are not what ships. Retained at rung 3 of the `Partitioned` ladder, where the
alternative is nothing at all.

### D. One scheme, one cookie, relaxed for everyone with a `SameSite=None` fallback for old browsers — rejected

A common pattern for third-party embeds. Rejected for the same reason as A, plus: a single cookie name
means an embed session and an ordinary session in the same browser overwrite one another, so the
maintainer's own demo setup would log him out of the tab he is demoing from.

## Consequences

**Positive**

- The feature's cookie blast radius equals the feature's own footprint. `SameSite=Lax` still governs
  every non-embed session, and a test says so.
- Two cookie names means the two session kinds coexist; no surprising cross-eviction.
- The non-sliding short lifetime bounds how long a stolen embed cookie is useful, independently of
  ADR-131's token revocation.

**Negative**

- One more authentication scheme and one more branch in `SmartAuthSchemeSelector` — a small, testable
  increase in the auth surface's fan-out.
- `Partitioned` support is genuinely unresolved at the time of writing. Slice 02 answers it
  empirically, and the verdict-grade run happens inside the Forge app rather than in a stand-in page
  (D42). Chrome and Firefox are reachable from the maintainer's Linux machine; **Safari is not**, and
  it is the strictest of the three — a Safari result needs a Mac or a hosted browser service, and its
  absence is something the verdict states rather than omits. A browser that refuses is a finding for
  the verdict, not a bug to chase.
- The embed session ends abruptly after 30 minutes with no sliding renewal. Inside a frame that looks
  like the page went dead; the Forge app must re-exchange on load, and a tab left open past the half
  hour needs a refresh. Accepted, and recorded as a demo-script constraint — a demo running longer
  than 30 minutes without a page load is not a scenario worth widening the blast radius for.

**Quality attribute impact**

- Security: improved relative to the alternative that was on the table. The trade-off point is that
  `SameSite=None` is a genuine relaxation for the sessions that carry it — bounded by the short
  non-sliding lifetime and by the fact that such sessions exist only when deliberately minted.
- Maintainability: two schemes are more to read than one, bought in exchange for the ordinary path
  being provably untouched.
