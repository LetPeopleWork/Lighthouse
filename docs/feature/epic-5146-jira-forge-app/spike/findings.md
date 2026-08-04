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
