# Bug #6254 — `lh delivery list` and the MCP Delivery list stopped reading the server's answer

**Pushed to `lighthouse-clients` main 2026-10-10, not yet released** — ships with the next clients release
(client minor, cli and mcp-core patch) · ADO Bug #6254 Closed · workspace
`docs/feature/bug-6254-deliveries-active-archived/` · `lighthouse-clients` commits `4e76513..08733b5`

## What users get

Since Lighthouse v26.8.31.7 the server answers "which Deliveries does this Portfolio have" with the running
ones and the archived ones apart, as `{active, archived}`. The clients still expected a bare list, so for
every server since then the readable `lh delivery list` printed the generic indented dump instead of the
Delivery table, and the MCP tool `lighthouse_delivery_list` lost its `summary`. Nothing failed loudly; both
quietly fell back to their "I do not recognise this" path.

After the fix:

- **`lh delivery list`** shows the Delivery table again, exactly as before. Below it, only when there are
  any, an **"Archived <Deliveries>"** section lists the archived ones with Name, Delivery Date, Archived On,
  Done and Likelihood. The heading sits directly on its table, the way the active title does; one blank line
  separates the two sections. No archived Deliveries means no section; no running ones means the usual
  "no Deliveries" line, with the archived section still shown.
- **An archived row's likelihood reads as the web's archived table does**: "Cannot forecast" when a team in
  it could not be forecast, "Not enough data" when work remained on thin history.
- **`lighthouse_delivery_list`** carries its summary again, counting the running Deliveries. Because the
  answer is now an object, the summary is a field of it (not a second text block), and the tool's
  description says where the running Deliveries, the archived ones and the summary are.
- **An older server still works.** Its plain list is read as the running Deliveries with none archived.
  Accepted consequence: `--json` against such a server prints the `{active, archived}` object, not the list.
- **Any other shape is an error, not a silent fallback**: the client reports an `unexpected` failure
  ("Lighthouse's list of Deliveries is not readable."). A request that fails outright keeps the server's
  own failure (a 404 stays a 404) rather than being reported as an unreadable list.

## How a server change reached users unnoticed

1. **The server reshaped a `/v1` answer in place.** Commit `8c0b99756` (2026-08-22, first released in
   v26.8.31.7) handed a Portfolio its archived Deliveries "in a list of their own" by turning
   `GET /v1/deliveries/portfolio/{id}` from an array into `{active, archived}`, on the same versioned route.
2. **The client cannot notice.** Its response types are written by hand, and `requestJson` casts the parsed
   answer to that type without checking it. The wrong type travelled on as if it were right.
3. **Both consumers degrade silently by design.** The Delivery reader returned "not recognised" for anything
   that was not an array; the CLI then fell back to the generic view and the MCP tool dropped its summary.
   Neither is an error, so nothing went red.
4. **The tests agreed with themselves.** The fixtures were written from the client's own type, not from
   what the server sends, so every unit test kept passing against a shape the server no longer produced.
5. **The only real-server check ran after publishing.** The integration smoke against a real Lighthouse
   container ran after the release job, against the CLI already on npm. It could only report the bug once it
   had shipped — which is how it was found.

## What shipped

| Commit | |
|---|---|
| `4e76513` | The client returns `{active, archived}`; an older server's list is normalised; anything else is an `unexpected` failure |
| `0cb568d` | `lh delivery list` reads the new answer: the Delivery table again, archived ones under their own heading |
| `649b9c5` | The MCP Delivery list summary counts the running Deliveries again, as a field of the answer object |
| `113b365` | The Archived heading sits directly on its table, like the active title |
| `30baed8` | The integration smoke moves into a shared script and runs **before** the Release gate (below) |
| `5aeaf3c` | Refactor, test-only: one named empty Delivery list for the MCP runtime test fakes |
| `dd49302` | Review fix: an archived row's likelihood says when it cannot be forecast or its data was thin |
| `078db55` | Review fix: the MCP tool's description says where its Deliveries and its summary are |
| `48f24b1` | Review fix: dev-latest red outside main pushes, packed-client check, archived check, 409 retry |
| `08733b5` | Mutation pass: the Delivery list keeps the server's own failure |

No server change. No Lighthouse backend, frontend, schema or migration change.

## The pre-release integration smoke

The second half of the fix is about catching the next one of these before a client ships.

- **One script, two moments.** `scripts/smoke-integration.sh <lighthouse-image>` starts a Lighthouse
  container, seeds the demo scenario and runs every check against whatever `lh` is on `PATH`. The
  post-publish `smoke-integration` job runs it with the CLI from npm, as before; the new `smoke-prerelease`
  job runs it before anything is published.
- **It tests this commit, not npm.** `smoke-prerelease` builds and packs `client` and `cli`, installs both
  tarballs into one prefix, and then checks that the CLI really resolved the packed client — no client
  nested under the CLI, and the top-level one resolved from a `file:` tarball, not the registry.
- **When it runs.** On every push to main, nightly (`cron: "17 3 * * *"`) and on manual dispatch.
- **Two images, two roles.** A matrix over `ghcr.io/letpeoplework/lighthouse:latest` and `:dev-latest`
  (`fail-fast: false`). The `latest` leg gates the Release environment: `unpublished` and `release` need it.
  The `dev-latest` leg carries server changes not yet released, so the two may legitimately differ; on a push
  to main its failure is a warning and step summary, never a blocker. On the nightly and manual runs it goes
  **red**, so the early warning for an unreleased server change is actually seen.
- **Releases only from a push.** `unpublished` and `release` now run only on a push to main, so a nightly or
  manual run never parks at the Release gate.
- **The archived view is checked against a real server.** Demo data seeds no archived Delivery, so after its
  other checks the smoke archives Project Apollo's Delivery and checks that `lh delivery list` prints the
  Archived table and that `--json` lists it under `archived`. Archiving is a premium feature: the licence
  comes from the optional repository secret `LIGHTHOUSE_SMOKE_LICENSE`, and without it (or against a server
  without the archive endpoint) the check is skipped with a notice. Both legs of the two green runs below
  skipped it, because the secret is not configured yet.
- **The demo reload is retried on 409.** Loading a demo scenario while Lighthouse is still updating the data
  it replaces answers 409 (a server-side race, see Still open). The smoke retries that load; before the retry
  it made the smoke red against the `latest` image on repeated runs.

## Key decisions

**D1 — one bugfix carries all of it**: client, CLI, MCP summary, the smoke's delivery check, *and* the
pre-release smoke gate.

**D2 — versioning**: client **minor** (a new exported type, `PortfolioDeliveries`, and a changed
`listDeliveries` return type; the changelog words it as a fix), cli **patch**, mcp-core **patch**; mcp-http and
mcp-stdio follow through `updateInternalDependencies`.

**D3 — normalise in the client.** `listDeliveries` always returns `{active, archived}`; an older server's list
becomes `{active: <the list>, archived: []}`. Accepted: `--json` against a pre-v26.8.31.7 server prints the
object.

**D4 — the readable layout** (sketch approved): the active table unchanged; below it, only when any exist, a
blank line, "Archived <Deliveries term>" and a Name / Delivery Date / Archived On / Done / Likelihood table.

**D5 — `latest` and `dev-latest` may differ** while server changes are unreleased. `latest` gates the clients
release; `dev-latest` is a visible warning, not a blocker.

**D6 — no blank line between the Archived heading and its table**, matching the active title (decided after
seeing the first output). The blank line before the heading stays.

Taken autonomously while addressing the review, each within the spirit of D1–D6:

- **Archived likelihood parity** with the web's archived table ("Cannot forecast" / "Not enough data") —
  the first version read every archived likelihood as forecastable.
- **The MCP description** now names `active`, `archived` and the `summary` field; it still promised a second
  text block.
- **`dev-latest` goes red on nightly and manual runs.** It had been `continue-on-error` on every event, so a
  nightly failure turned green and nobody would have seen it.
- **A licence-gated archived check** rather than none, skipped with a notice when no licence is configured.
- **A 409 retry on the demo reload**, since the server race otherwise makes the smoke flaky.

Out of scope, deliberately: a contract check against the server's `swagger.json`, any server change, and an
MCP summary that also counts the archived Deliveries.

## Quality gates

- **DES**: all five steps (01-01 … 01-04, 02-01) ran RED → GREEN → COMMIT; integrity verified. Each step
  ended with `pnpm run ci` green in `lighthouse-clients`.
- **Refactor**: `5aeaf3c`, test-only.
- **Adversarial review** (Opus): NEEDS_REVISION with 1 high, 3 medium and 3 low findings; all fixed in
  `dd49302`, `078db55` and `48f24b1`.
- **Mutation** (StrykerJS 10, gate 80 %): **97.86 % → 99.29 %** (139 of 140 killed). The two survivors in the
  client's failure path were killed by the new test in `08733b5`; the one left is equivalent (a missing `?.`
  whose `TypeError` the summary helper's `catch` turns into the same "no summary"). Per-survivor reasoning in
  `mutation/results.md`.
- **Client CI**: runs `38033651827` (at `30baed8`) and `38035446128` (at `08733b5`) green, both smoke legs
  included. `release` was skipped in both, as expected: no package version was bumped.

## Finalize checklist

1. **Docs prose** — done: `git grep` for `lh delivery`, `delivery list` and `lighthouse_delivery_list` in
   `docs/` outside `docs/feature` and `docs/evolution` finds no user-facing page describing the readable Delivery
   list or its MCP summary (`docs/aiintegration.md` does not cover per-command output). It finds ADRs and
   journeys that mention the command only by name, plus one record that became wrong: ADR-224 listed
   `delivery_list` among the array answers whose summary goes in a second text block. ADR-224 gained a dated
   amendment saying the Delivery list is now an object answer with `summary` as a field. In
   `lighthouse-clients`, `packages/cli/README.md` was already updated in `0cb568d` and `docs/release-model.md` /
   `docs/deployment.md` in `30baed8`; `ARCHITECTURE.md` is updated alongside this record.
2. **Per-feature screenshots** — N/A, because nothing in the Lighthouse web UI changed; the fix is terminal
   and MCP output only, and no screenshot shows `lh` output.
3. **Demo data** — N/A, because the fix needed no demo-data change. Demo data still seeds no archived
   Delivery; the smoke archives Project Apollo's Delivery at run time instead, and unit tests pin the archived
   section.
4. **Website asset freshness** — N/A, because `git grep` in `/storage/repos/website` for `lh delivery`,
   `delivery list` and `lighthouse_delivery_list` finds nothing; the website only names the CLI and links the
   release downloads, and has no screenshot of `lh` output.
5. **Lighthouse-Clients versioning** — done: changesets `@letpeoplework/lighthouse-client` minor,
   `@letpeoplework/lighthouse-cli` patch, `@letpeoplework/lighthouse-mcp-core` patch, plus two empty changesets for
   the test-only commits. They ship with the next clients release (`pnpm release:version` before it).
6. **Terminology** — done: the archived heading is built as `Archived ${terms.deliveries}` from the same
   `Terms` the active view uses, so an instance that renamed "Deliveries" sees its own word; column headings
   come from the same terms.
7. **Usage-data event** — N/A, because this is a bug fix restoring existing output; there is no new usage to
   count.

## Still open

- **Server Bug candidate, not raised yet — awaiting the maintainer.** `POST /api/v1/demo/scenarios/0/load`
  intermittently answers 409 (`DbUpdateConcurrencyException`) while the server is still updating the data the
  load replaces. The smoke works around it with a retry; the race itself is in the server.
- **Add the `LIGHTHOUSE_SMOKE_LICENSE` repository secret** to `lighthouse-clients`, so the archived check runs
  instead of being skipped.
- **A contract check against the server's `swagger.json`** (for example a diff against `dev-latest`) would catch
  a reshaped answer without relying on the smoke happening to exercise it. Out of scope here; candidate Story.
- **Treat a reshaped `/v1` response as a breaking change on the server.** The clients now cope with both shapes
  of this one answer, but the next in-place reshape will break released clients the same way.

## Lessons learned

- **A fallback that never errors hides a contract break.** "Not recognised, print the generic view" was the
  right user experience and the wrong signal; the client now refuses a shape it cannot read, so the failure is
  named.
- **Fixtures written from your own types test your types, not the server.** The new fixtures are modelled
  field for field on the server's DTOs.
- **A check that runs after publishing can only report a shipped bug.** The same smoke, run before the gate
  against packed packages, would have stopped this release.
- **An early warning nobody sees is not one.** `continue-on-error` on every event would have turned the
  nightly `dev-latest` failure green; it is red now everywhere except where it must not block.

## Artifacts

Phase B (migration): nothing to migrate — this bugfix ran DELIVER only, so the workspace holds no `discuss/`,
`design/` or `distill/` folders.

- `docs/feature/bug-6254-deliveries-active-archived/` — the workspace
- `docs/feature/bug-6254-deliveries-active-archived/deliver/roadmap.json` — goal, D1–D6, scope, five steps
- `docs/feature/bug-6254-deliveries-active-archived/mutation/results.md` — mutation scores and survivor reasoning
- `docs/product/architecture/adr-224-an-mcp-summary-rides-beside-unchanged-facts.md` — amended for the Delivery
  list's object answer
