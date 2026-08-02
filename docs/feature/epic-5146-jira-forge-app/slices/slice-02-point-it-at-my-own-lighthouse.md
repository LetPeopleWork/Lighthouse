# Slice 02 — Point it at my own Lighthouse

**Story**: US-02 · **Epic**: [#5146](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5146)
**Repo**: `LetPeopleWork/Lighthouse-Jira-App`. **Not** this repository.

## Goal

Replace slice 01's hardcoded URL with one the user types, and confirm on entry that the URL really
points at a Lighthouse.

## IN scope

- Settings view inside the app: URL field, **Connect** action, status line.
- Site-scoped Forge storage for the URL — one configuration per Jira site, shared by all users of
  that site.
- Handshake probe: `GET {url}/api/v1/version/current` from the Forge backend (D4). Backend-side, so
  browser CORS never applies and Lighthouse's fail-closed `AllowedOrigins` is never involved.
- Auth pre-warning: `GET {url}/api/latest/auth/mode`; if `Mode = Enabled`, warn that login inside the
  frame is expected to fail and an auth-disabled instance should be used for demos (D8, R2).
- `https://` enforcement with a stated reason (P3).
- The global page frames whatever URL is stored.

## OUT of scope

- Multiple saved instances / instance switching.
- Per-user (rather than per-site) configuration.
- Any new Lighthouse endpoint — D4 is explicit that the existing anonymous `version/current` is what
  gets used, precisely because it already exists on instances predating this epic.
- Making the auth-enabled case actually *work* — that is step 2 (D5). This slice only warns.
- Scoped views (D2).

## Learning hypothesis

**Disproves if it fails**: *"a Lighthouse origin unknown at manifest-build time can be framed and
fetched by the app."*

Forge declares egress and framing permissions **statically in the manifest**. Slice 01 declares one
known origin. This slice needs an origin supplied at runtime, which requires a wildcard declaration —
and wildcard egress is exactly what Atlassian discourages and what marketplace review rejects.

Failure here is survivable, unlike slice 01's: D3 degrades to a small allow-list or a single
hardcoded demo instance, the epic continues, and the constraint becomes a finding in the verdict
("a real Forge app would need per-customer manifests, or a Lighthouse-hosted proxy origin"). That
finding is itself worth knowing before anyone scopes a marketplace app.

**Confirms if it succeeds**: a prospect can evaluate against their own data — the difference between
a canned demo and a real evaluation.

## Acceptance criteria

- **AC-02.1** Valid URL + **Connect** → `Connected — Lighthouse v{version}`; URL persisted.
- **AC-02.2** Reachable non-Lighthouse URL → *"That URL responded, but it does not look like a
  Lighthouse instance"*; **not** persisted.
- **AC-02.3** Unreachable or timing-out URL → failure shown; not persisted.
- **AC-02.4** Instance reporting `Mode = Enabled` → embedded-login warning displayed.
- **AC-02.5** URL saved by one user is the URL a second user on the same site sees.
- **AC-02.6** `http://` URL rejected with a reason.
- **AC-02.7** Lighthouse repository unchanged (K4).

**Production-data requirement**: AC-02.1 is accepted against a **self-hosted Lighthouse holding real
team data**, not only the demo instance. A probe that only ever sees the demo instance proves the
plumbing, not the value — this is the slice where the carpaccio synthetic-data test is paid off.

## Dependencies

- **Slice 01 green.** If framing is impossible, this slice has nothing to configure.
- A second, real-data Lighthouse instance reachable over HTTPS (for the production-data AC).
- The auth-mode warning needs one instance with auth **enabled** to verify against — the dev
  instance at `:5169` runs without auth, so this needs a deliberate second target.

## Effort estimate

≤1 day. One settings view, one storage read/write, two HTTP calls with enumerated outcomes. Nothing
here is novel except the manifest permission question, which the learning hypothesis isolates.

## Reference class

Connection-setup UIs in Lighthouse itself — the work-tracking-system connection dialog and the
ServiceNow board picker (`docs/feature/servicenow-board-picker-and-query-guidance/`). Same shape:
paste a URL, press a button, get told plainly whether it worked. The recurring lesson from those is
that the **error paths cost more than the happy path**, which is why AC-02.2, AC-02.3 and AC-02.6
are separate criteria rather than one "handles bad input" line.

## Pre-slice SPIKE

**Recommended, timeboxed, if slice 01's manifest work left the wildcard question open.** One
question: does a Forge manifest permit egress/framing to a domain supplied at runtime, and at what
cost? Documentation-answerable; no code. Running it before building the settings view avoids
designing a URL field for a URL the platform will refuse.

## Dogfood moment

Same day: point the app at the dev instance, then at a real-data instance, and switch between them
by editing one field. If that is comfortable, the demo story works; if it is fiddly, slice 03's
README has a problem to solve.
