# SPIKE Decisions — epic-5146-jira-forge-app

Scope: slice 02 step 1 only. This is a targeted probe inside an epic whose DESIGN wave is already
complete — not the usual pre-DESIGN spike — so it answers one open question (OQ-1) and hands back.

## Assumption Tested

Can ASP.NET Core on `net10.0` emit the `Partitioned` attribute on a `Set-Cookie` response header at
all? (OQ-1; D28's second cookie scheme and ADR-130 both rest on it.)

## Probe Verdict

**WORKS — rung 2 of four.** No first-class `Partitioned` property exists on `CookieOptions` or
`CookieBuilder` at this TFM, but `CookieBuilder.Extensions.Add("Partitioned")` puts the attribute on
the wire verbatim — and `CookieBuilder` is what a cookie authentication scheme configures, so D28's
scheme carries it by configuration alone. Rung 3 (an `OnAppendCookie` hook or a raw header append) is
not needed. Full evidence in `findings.md`.

## Promotion Decision

**DISCARD** — maintainer, 2026-08-04. The findings are the deliverable; the probe code is deleted.

Rationale: a walking skeleton here would be nearly all of slice 02 step 2, and its *thin* version is
a bearer-token endpoint with no expiry, no single-use and no revocation, committed to `main` under
trunk-based development. The design refuses exactly that — *"the token is a bearer credential that
grants a session, so expiry and revocation are part of this slice, not follow-ups"* — so promoting
would reopen a decision already closed, in exchange for reaching something demo-able a few hours
sooner. Step 2 goes through DISTILL and DELIVER with the security work inside it.

## Walking Skeleton

Not built (DISCARD).

## Design Implications

- **OQ-1 is closed and ADR-130's open ladder with it**, on the server side only.
- The mechanism is configuration, not a hook: no middleware ordering to get wrong and nothing a
  future ASP.NET Core upgrade can silently reorder around.
- `Secure` stops being a preference and becomes a prerequisite — already D24's shape.
- **The browser half is untouched.** This says the server can emit the attribute; whether Chrome,
  Firefox and Safari honour it in a real cross-site frame is steps 3 and 6, and it remains the thing
  that can still kill the approach. A green server-side result must not be read as the cookie
  question being answered.

## Constraints Discovered

- The framework does not validate the extension string — `Extensions` appends whatever it is given.
  There is no compile-time protection on the attribute name, so step 2's guard is an assertion on the
  literal `Set-Cookie` header rather than on the call site.
- The result is a claim about one SDK (`10.0.110`, runtime `10.0.10`) on one machine. CI should agree
  before anything depends on it.
