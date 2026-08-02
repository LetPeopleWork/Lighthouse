# Slice 01 — A Lighthouse renders inside Jira

**Story**: US-01 · **Epic**: [#5146](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5146)
**Repo**: `LetPeopleWork/Lighthouse-Jira-App` (new — D7). **Not** this repository.

## Goal

Prove that a Forge app can render a live Lighthouse SPA inside a Jira Cloud page — or prove it
cannot, before anything else is built.

## IN scope

- Forge app skeleton: `manifest.yml`, one `jira:globalPage` module, one Custom UI resource.
- A nested `<iframe>` pointing at a **hardcoded** HTTPS Lighthouse URL.
- Whatever manifest permissions the framing requires (`permissions.external.*`), declared for that
  one known origin.
- A visible diagnostic when the frame is blocked: the blocked origin and the browser/Forge reason.
- `forge deploy` + `forge install` onto the LetPeopleWork Atlassian cloud instance (the one already
  used for testing — no new tenancy needed).

## OUT of scope

- Settings UI, stored configuration, user-entered URLs → slice 02.
- The `version/current` probe and the `auth/mode` warning → slice 02.
- Jira project/board context or scoped views → deferred (D2).
- Atlassian identity / SSO → out of epic (D5).
- Any automated test beyond opening the page and looking at it.
- Any change to the Lighthouse repository.

## Learning hypothesis

**Disproves if it fails**: *"Forge Custom UI can frame an arbitrary external HTTPS origin, and the
Lighthouse SPA remains usable when nested inside it."*

Two distinct ways to fail, and they mean different things:

1. **Forge/CSP blocks the frame** (R1) → the wrapper approach is dead as designed. D1 and D3 must be
   re-decided; the epic likely closes or pivots to a non-visual integration. This is the outcome
   worth buying first, because it is the cheapest possible way to learn the epic is not viable.
2. **The frame loads but the SPA misbehaves** — routing breaks, storage is partitioned away, the app
   renders but cannot navigate (R2) → framing works; the wrapper needs conditions attached. Record
   which, and carry them into slice 03's verdict.

**Confirms if it succeeds**: the whole-UI wrapper is technically real, and slice 02's remaining
question narrows to "can the origin be unknown at build time?".

## Acceptance criteria

- **AC-01.1** Opening **Apps → Lighthouse** on our Jira Cloud instance renders the Lighthouse SPA; a Teams list
  is reachable and one team detail page opens, from inside the frame.
- **AC-01.2** Against an auth-disabled instance, no login prompt appears and data is visible.
- **AC-01.3** If the frame is blocked, the page shows the blocked origin and the reason — not an
  empty rectangle.
- **AC-01.4** `git status` in the Lighthouse repository is unchanged by this slice (K4).

## Dependencies

Both are ADO stories in their own right, and both must be green before this slice starts:

- **#5634** — Forge toolchain, hello-world app on our own Atlassian cloud instance, new repo
  (pre-reqs P2 + P4). The Jira Cloud *site* already exists — LetPeopleWork has one for testing — so
  what is missing is the CLI, the login and the first-contact learning, not the tenancy.
- **#5635** — an HTTPS, publicly-certificated, auth-disabled Lighthouse with demo data (pre-req P3).
  `localhost` cannot be framed, so the usual dev instance on `:5169` is not a candidate.

Neither can be substituted by a mock without voiding the slice's entire purpose.

## Effort estimate

≤1 day of crafter dispatch **once #5634 and #5635 are done** — not instead of them. The Forge
scaffolding is generated; the slice itself is configuration plus one iframe. The risk is in whether
the platform permits it, not in volume of code.

The estimate is only honest because R4 was split out. With a first-contact toolchain still inside
this slice, an unfamiliar CLI and a forbidden frame produce the *same* symptom — a blank page — and
the slice would return "it didn't work" instead of the platform answer the whole epic is waiting on.
#5634 exists to make the toolchain boring before it has to carry a real question.

## Reference class

New-vendor-platform walking skeletons in this project — the MCP OAuth work
(`docs/feature/mcp-oauth-discovery-fix/`) is the closest analogue: small code, most of the elapsed
time spent discovering what the external platform actually permits. Budget accordingly; a same-day
"it just worked" and a two-day "the manifest fights back" are both within the reference class.

## Pre-slice SPIKE

**No separate spike — #5634 is the spike**, and it is a better one than a documentation review would
be, because it ends with a working toolchain rather than a note. Read the Forge docs on external
frame/egress permissions while doing it; if they answer R1 outright, slice 01 becomes a
confirmation rather than an experiment, which is a good outcome, not a wasted slice.

## Dogfood moment

Same day: the maintainer opens the LetPeopleWork Jira instance and looks at Lighthouse under
**Apps**. That screenshot is the first artifact of the epic and the first thing worth showing
anybody.
