# A connection field change starts its owners' updates — Story 6307

Delivered 2026-10-15 to 2026-10-16 in three slices of ten steps. Free, on every licence, no setting. ADO User
Story #6307, split out of Bug #6306 (Jira Releases on Server and Data Center), no parent Epic.

Adding an Additional Field to a connection left it empty on every Work Item and Feature already in Lighthouse
until each Team and Portfolio on that connection had been updated, so a rule on the new field quietly matched
nothing. The docs said to run an update; the maintainer himself forgot. Saving a connection whose Additional
Fields change what its Teams and Portfolios fetch now starts their updates, and the browser asks first.

## What shipped

| Step | What it changed |
| --- | --- |
| 01-01: the connection's share of the fetch fingerprint is one function | `FetchFingerprint.ForConnection` renders the connection-owned inputs, and `For` composes it. A pinned-digest test, committed first, proves the rendered input is byte-identical, so no stored fingerprint moves on upgrade and the upgrade itself starts no downloads. |
| 01-02: a field change updates every Team and Portfolio on the connection | The connection PUT renders the connection share before the merge and again after `Save()`. When it moved, it publishes `ConnectionFetchShapeChanged(ConnectionId, TeamIds, PortfolioIds)`, and `ConnectionFetchShapeChangedUpdateTriggerHandler` queues each owner through the existing update queue, Teams before Portfolios, with one Information log line (connection id and the two counts, never field names). |
| 01-03: the fan-out holds at its edges | Other field edits (removal, re-reference, label-only rename), owners already waiting or running, a failing owner, and the community licence's one field. A credential, URL-option, authentication, name or write-back-only save queues nothing; Teams and Portfolios on other connections are never queued. |
| 01-04: a stored save answers 200 | If the owner look-up or the publish throws after the save, the PUT logs one Error line and answers 200: the save is stored, and every owner recovers at its next scheduled update. Only the PUT publishes; predefined fields registered when a connection is read, or on the Jira sync path, start nothing. |
| 02-01: the list says who uses each connection | The connection list read carries `usedBy { teams, portfolios }` from a new read-only `IWorkTrackingSystemConnectionUsage` port. The edit page asks `Update N Teams and M Portfolios?` when the field definitions differ from the ones it loaded and at least one Team or Portfolio uses the connection. No extra fetch, so no new loading or could-not-load look. |
| 02-02: Save and update, Cancel, and the old promise is gone | `Save and update` saves and returns to the overview; `Cancel` saves nothing and keeps the edits. The "Additional Fields will be loaded with the next refresh of data" alert is removed. |
| 02-03: the question in this instance's words | Title and body use the configured Terminology, singular and plural. |
| 03-01: Save only | `Save only` sends `PUT …?startUpdates=false`: the save is stored exactly as usual, nothing is queued, and each owner picks the field up at its next scheduled update. The CLI and MCP send no choice and update now. |
| 03-02: the server accepts the usage event | `WorkTrackingSystemFieldsChanged = 19` in `UsageDataEventName`, with the closed enum `Updates: Now \| Later` declared in `UsageDataEventShapes` and checked in `Fits`; a value outside the list is refused. |
| 03-03: the browser sends it | Answering the question sends the event after the save succeeds (`Save and update` → `Now`, `Save only` → `Later`). Not on `Cancel`, not when no question showed, not when the save is refused. |

Added at the hold before mutation, at the maintainer's request (H3): a busy state while saving. The question
dialog stays open while the save runs; the clicked button spins, and every button, Esc and the backdrop do
nothing until the save settles. A 409 closes the dialog and shows the existing conflict message. The page's
Save button spins and stays disabled through validation and save (`ValidationActions` now waits for `onSave`),
reusing the `ActionButton` spinner, and assistive technology is told when a save is running.

Refactor and test hygiene in the remaining commits: harness tidying, test names without document pointers, the save
choice in the page's typed signature, and the conflict-only blame message pinned. No migration, no change to
the PUT's request or response, and one additive field (`usedBy`) on the list read.

**ADR-239** (`docs/product/architecture/adr-239-a-connection-save-that-moves-the-fetch-fingerprint-starts-the-affected-updates.md`)
is Accepted, 2026-10-16. It narrows ADR-030's "expensive means intentional" rule for connection saves and
supersedes ADR-140's "a connection change fans out passively" consequence for saves through the PUT; both carry
dated amendment notes. It was written as ADR-238 and renumbered, because story 6301 reached main first with
its own ADR-238.

## Decisions

### DISCUSS, DESIGN and DISTILL (maintainer, 2026-10-15)

- **A connection save follows the save-then-update precedent** that Team and Portfolio settings saves already
  follow (D1), starting immediately through the existing queue (D2), on every licence (D4), with no
  large-instance threshold (D5). The Task Manager's per-row stop is the relief valve.
- **A label-only rename still costs a full fetch** (D3); the fingerprint is left as it is.
- **Rules keep reading empty until each update finishes** (D7). Accepted, not a follow-up.
- **"Moved by this save" is the connection's share of the fingerprint, before against after** (DDD-1), so an
  owner that owed a full fetch for another reason is not swept up.
- **Fan-out through the domain-event bus**, with the controller as publisher (DDD-3, DDD-4), so the CLI and MCP
  get the behaviour too.
- **Queue coalescing is reused unchanged** (DDD-10, UC-1): an owner waiting at Save may run twice but downloads
  everything only once per save and keeps one Task Manager row.
- **The UI sketch session (S1 to S7)**: a dialog with `Cancel`, `Save only` and `Save and update` (default);
  counts in the title; no dialog for an unused connection; no after-save notice, navigate to the overview as
  before; the event carries `Updates: Now | Later` and is sent only on an answer.

### Decided in AFK, revisited at the hold

- **A zero count keeps its place in the title** ("Update 3 Teams and 0 Portfolios?"). Confirmed at H2.
- **A save refused after the question sends no usage event.**
- **The usage event lives in slice 03**, where the question gains its second answer.
- **The lowercase `now` refusal case was dropped** from the usage-data acceptance test, because the enum reader
  is case-insensitive and accepts it. An out-of-range number (`"updates":7`) was added to the same refusal test
  instead: with the server's `updates` check deleted it fails, with it restored all 12 cases pass.

### Hold before mutation (maintainer, 2026-10-16)

| # | Question | Decision |
| --- | --- | --- |
| H1 | ADR status | ADR-239 Accepted, with its amendments to ADR-030 and ADR-140. |
| H2 | Dialog copy when one count is zero | Kept as is. |
| H3 | No busy state while a save is in flight (older than this story) | Fixed in this story, dialog and page Save (see above). |
| H4 | The edit page never sends `concurrencyToken` (older than this story) | Noted only, no ticket. |

## Quality gates

- **Backend.** The final filtered suite (connector categories excluded) gave 10221 passed and 0 failed.
  `dotnet build` had 0 errors; its only warning is the NU1903 advisory on
  `Microsoft.AspNetCore.SignalR.Common` 10.0.12 in the test project, which this story did not introduce.
  `Program.cs` gained one handler registration and one port registration, so CI ran the full Integration suite.
- **Frontend.** `pnpm test` gave 7786 passed; `pnpm build` and Biome were clean.
- **Mutation** (`mutation/results.md`, gate 80 %).
  - Frontend, the story's changed lines: 86.52 % → 89.89 % (80 of 89) after one kill round. The nine
    survivors are equivalent or unreachable through the UI, each argued in the results file.
  - Backend, the story's changed lines: 76.92 % → 100 % (26 of 26). Whole files: 66.52 % → 69.13 %; every
    remaining survivor sits on older lines the story did not change (predefined-field and option merging,
    secret-option handling, the PostHog publisher's fallback and logging, consent-token checks).
- **Reviews.** Every slice had its own review, and a final adversarial review covered the whole story. All
  were APPROVED WITH FIXES, with no blockers; the fixes are in the commit range.
- **DES integrity.** All ten steps have complete RED, GREEN and COMMIT traces. Step 01-03's RED is recorded
  as not applicable, because its rows were already green on step 01-02's code and their RED was recorded at
  DISTILL (`distill/red-classification.md`).
- **CI.** Green on `c17973f28`, the last pushed commit.

## Finalize checklist

| Item | Answer |
| --- | --- |
| User docs | Done. `docs/settings/worktrackingsystems.md` gains a paragraph under Additional Fields: Lighthouse asks whether to update now or leave it to the next scheduled update, and a change through the API, CLI or MCP updates right away (`9bf2cf179`). |
| Release notes | Done. A vNext line, "A new Additional Field fills in without a manual update." (`9bf2cf179`). ADO tag `Release Notes` is on #6307. |
| Usage-data page | Done. `docs/settings/usagedata.md` has the row "A connection's Additional Fields were changed" and the field "When the updates run" (`Now` or `Later`) (`d731c6f56`, reordered in `bfa77df15`). |
| Screenshots | N/A, because the demo connection is a CSV connection, which cannot take Additional Fields, so there is no screen to show with demo data. The maintainer agreed to no screenshots. |
| Lighthouse-Clients | N/A, because the CLI and MCP send no choice and therefore update now, and the PUT contract is unchanged; `usedBy` is additive on the list read. The maintainer agreed. |
| Website | N/A, because no marketing page says when Additional Fields fill in; maintainer agreed. |
| Demo data | N/A, because no new entity or field is seeded and the CSV demo connection cannot carry Additional Fields. |
| `ARCHITECTURE.md` | Updated with the connection-save fan-out and its event. |
| RBAC | N/A, because nothing new is gated: the PUT stays SystemAdmin and the fan-out passes no licence gate. |
| Migrations | N/A, because nothing new is stored. |

## Known and open

- **The edit page never sends `concurrencyToken`.** Two admins saving the same connection overwrite each
  other. Older than this story; noted only, no ticket (H4).
- **The `ConnectionFieldChange` acceptance test class is close to CI's per-class time budget.** A scenario
  added there should make the class faster rather than split it: splitting a class only to stay under the
  budget hides the cost instead of cutting it.

## Migrated out of the workspace

Nothing was moved. The project has no `docs/architecture/<feature>/`, `docs/scenarios/<feature>/` or
`docs/ux/<feature>/` for single Stories (the 6084 precedent made none), and this workspace has nothing that
would go there: `design/` holds only `upstream-changes.md`, with no architecture-design, component-boundaries,
technology-stack or data-models file and no `design/adrs/`; `distill/` has no walking-skeleton file; DISCUSS
wrote no journey file. What lasts already lives in its permanent home: ADR-239 and the 6307 section of
`docs/product/architecture/brief.md`, the job and persona in `docs/product/jobs.yaml` and
`docs/product/personas/config-admin.yaml`, and `ARCHITECTURE.md`. The workspace stays in place, nothing
deleted, as delivery history.

## ADO

Story #6307 Closed 2026-10-16, after CI went green on `c17973f28`. Tagged `Release Notes`.

Workspace: `docs/feature/story-6307-refresh-on-additional-fields-change/`. Commits: `74d9bd6a9..c17973f28`
(pushed), plus the local docs commit `9bf2cf179` and this record.
