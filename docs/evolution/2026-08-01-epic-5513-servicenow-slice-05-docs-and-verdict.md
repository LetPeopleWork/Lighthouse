# Four slices shipped a connector nobody had ever driven through a browser — Epic 5513 slice 05

**Feature:** epic-5513-servicenow-integration | **ADO:** Story #5578 (parent Epic #5513) | **Shipped:** 2026-08-01 | **Commits:** eight carrying `(#5578)`, `d67a99a13..6339ef5eb`, interleaved with Story #5610's

## What shipped

A ServiceNow administrator can now self-serve. `docs/concepts/worktrackingsystems/servicenow.md` opens with a section written to be handed over as-is — four numbered asks, each a yes/no its reader can answer, each linked to the detail behind it. Below that: the basic-auth restriction and the four `sys_properties` to check before connecting, the minimum role set measured one role at a time, why a wrong role looks like an empty backlog, encoded-query guidance, the record-class model, the board wizard, state mapping, and time in state with click-by-click instructions for checking and creating a metric definition. ServiceNow is now named in the four places `docs/compliance/` enumerates external systems, and in both supported-system lists in `docs/concepts/concepts.md`.

`Scripts/DemoEnv/ServiceNowSystemUpdater.py` reached parity with its three siblings: a branching flow so *On Hold* happens to a subset rather than to everything (which is what a blocked-rule demo needs), `state_extras` generalising the close-fields mechanism to any state with mandatory fields, and `ensure_state_metric_definition`, so a rebuilt or different PDI does not silently lose time in state.

And, last: the epic got an end-to-end test.

## Four slices, no walking skeleton in the browser

Every other connector in this repo has a spec that connects a real instance and builds a team from it — `tests/specs/ado`, `jira`, `linear`, `csv`. ServiceNow shipped **four slices** without one. The connector had unit tests, a functional core with 90 %+ mutation coverage, and live integration tests at the C# boundary. What it had never had was a single run of the actual user journey: open the wizard, type the credentials into the fields the frontend renders, pick a board, get a team.

Nothing was broken when the test was finally written — it passed on the first run against `dev191338`. That is the interesting part. The gap was invisible precisely *because* the layers on either side of it were so well tested, and because slice after slice ended with a manual dogfood that a human performed and did not automate. **A dogfood proves the path works today; a spec proves it works tomorrow.** The two are not substitutes, and a rich unit-test suite makes the absence of the second easier to overlook, not harder.

Two smaller things fell out of writing it, both of which had been sitting in plain sight:

- The two ServiceNow secrets were **already mapped** in `ci_verifysqlite.yml` and `ci_verifypostgres.yml` — added when the backend integration tests landed. The E2E harness had no getter for either. The plumbing was there and unused for four slices.
- `BaseSettingsPage.selectWizard` builds its button name as `Select ${system} ${what}`, which works for Jira, Azure DevOps, Linear and CSV. ServiceNow's wizard is registered as **`Select Visual Task Board`** — named after the ServiceNow feature it reads rather than after the connector. A page object that reconstructs a label from parts is fine right up until one product declines the pattern; it now carries an explicit override rather than a cleverer template.

## Documentation written from a DESIGN describes intent, not behaviour

The Board Wizard section of the page was written while Story #5610 sat unpushed in another worktree. It was written from #5610's DESIGN, and it was not wrong so much as **thin in exactly the places the implementation had made decisions**:

| The page said | The code does |
|---|---|
| "fill in the query, the work item types, and the state configuration derived from the board's lanes" | the work item type arrives as a **display label** — `Change Request`, not `change_request` (#5610 OC-4) — because #5612 deleted the save-time normalisation, so a class name pre-filled here would produce a team that syncs nothing |
| — | first lane → *To Do*, last → *Done*, everything between → *Doing* |
| — | *Canceled* / *Cancelled* lanes drop **wherever they sit**, not only last |
| — | fewer than three usable lanes leaves the state lists **empty on purpose**, rather than inventing a split |
| "Boards that cannot produce a usable query are not offered" | `active=true^tableISNOTEMPTY^filterISNOTEMPTY` — **inactive** boards are filtered too, which the sentence did not cover |
| — | the wizard is offered for teams only; there is no portfolio equivalent |

Every one of those is a rule an administrator meets on their first board. The lesson is not that the writer was careless — it is that **a design document and a shipped implementation are different sources, and prose written against the first must be re-read against the second before it is published.** The correction cost twenty minutes of reading `ServiceNowBoardMapper` and one constant in the connector. Publishing without it would have cost a support thread per reader who hit the empty-lane rule.

## The negative result is the epic's most valuable output

Slice 05 carries US-06, whose learning hypothesis was that state history is affordable on a least-privilege account. It is **formally disproven.** `metric_definition` and `metric_instance` answer `403` to every read-only role and open only at `itil` — a fulfiller-grade role. `itil` was accepted as an adoption cost, not because it is cheap.

This is easy to lose now that slice 04 shipped and looks like a win, which is why both the docs page and the epic delta state it in the maintainer's own framing: everything except time in state works with genuinely read-only roles, and a shop whose security posture will not allow `itil` still gets throughput, WIP, age and forecasts. An adoption argument that hides its own cost is not an adoption argument.

Two of US-06's five acceptance criteria closed on evidence rather than on the deliverable they named:

- **AC1 (a standalone, build-free validation script)** was superseded and deliberately not built. Its premise was that Lighthouse could not be built or run on the customer's on-prem side. The maintainer ran Lighthouse itself against that instance and it worked, so the constraint never bound. **A deliverable whose premise dissolves should be retired, not reinterpreted into something adjacent** — building a standalone reimplementation of a path that already succeeded would have been ceremony with a ticket number.
- **AC3 (cloud-vs-on-prem divergence list)** is met and the list is **empty**. Basic auth was accepted; everything observable matched the PDI. An empty list is the finding the criterion asked for, not a failure to produce one.

AC2 is partly met and says so: the role set is measured on cloud, and the on-prem run predates time in state with its account's roles unrecorded. The page carries that provenance and invites a correction rather than claiming proof.

## Where Lighthouse tells you, and where it does not

Bug #5630 landed on `main` mid-slice. The history verdict is now per record class and evidence-based, and the connector logs a warning naming the kinds of work no state metric measures. It is deliberately a **warning, not a downgrade**: reporting the class as unavailable would date a sync-delta transition at sync time on the classes that *do* work, because `SupportsTransitionHistory` is answered per connection rather than per class.

The consequence is that there is still **no in-app signal**. On a team's own pages, the Time in State column for an unmeasured class simply stays empty — the same as for work that genuinely moved fast. The docs note was rewritten to describe exactly that gap rather than deleted once the bug was fixed. **A fix that improves the log is not a fix that reaches the screen**, and documentation that quietly upgrades "we do not tell you" to "this is fixed" spends the reader's trust on the next thing that surprises them.

## Gates

| Gate | Result |
|---|---|
| `servicenow.spec.ts` vs live `dev191338` | passed, 13.5 s, first run |
| ServiceNow connection-creation `@screenshot` | passed, 5.7 s |
| `linear` + `csv` specs (regression on the shared page object) | 2 passed |
| E2E `pnpm run build` (Biome + `tsc`) | 0 errors, 0 warnings |

No backend or frontend suite was re-run, and none was owed: this slice changed documentation, one Python demo script, and test-side TypeScript. No production code was touched, so **mutation testing does not apply** — there is nothing under test to mutate.

## Two screenshots, taken two different ways

The connection-creation shot lives in `Screenshots.spec.ts`, driven by demo data with placeholder credentials. The board-picker shot lives in the new ServiceNow spec, because it needs a real board on a real instance. Splitting them keeps `Screenshots.spec.ts` **live-independent**, which is a property worth defending: it is the file that regenerates every documentation asset in the repo, and the day it depends on a third-party instance is the day `update-docs` starts failing for reasons that have nothing to do with the docs.

## Still open

- **Nine ServiceNow ADRs remain `Proposed`** — 114, 115, 116, 118, 123, 124, 125, 126, and ADR-118's D2 amendment, whose code shipped in slice 04 while its text still describes the definition-type-only rule. The maintainer ratifies; nothing here can.
- **US-06 AC4/AC5 — ≥3 users' feedback and the go / narrow / stop verdict — are deferred post-release** by maintainer decision, 2026-08-01. There is nothing for a user to react to until ServiceNow ships, and the KPI's window is calendar time. This is the epic's stated outcome and the only thing standing between it and closure.
- **Nothing ServiceNow has ever been released.** The marketing surface on letpeople.work still reads "Jira, Azure DevOps, and Linear" in roughly twenty places across `website/`, and `lighthouse-clients/skill/SKILL.md` says the same. Both are separate repositories, both are positioning rather than mechanics, and slice 05's own scope put marketing copy **out**, pending the maintainer confirming the new-system claim.
- **No DEVOPS wave has ever run for this epic**, carried since slice 01. DESIGN's Pact / contract-test recommendation for the Table API response shapes is still unrouted.
- **The ServiceNow E2E now runs on every `verify`.** `ci_verifysqlite` and `ci_verifypostgres` invoke `pnpm run test` with no grep filter, so the spec reaches the PDI on every run. Accepted by the maintainer: the instance is exercised continuously enough that hibernation is not a realistic source of red builds. If that changes, gate the spec — not the secrets, which the backend integration tests also depend on.
- **Ten mutation survivors** remain in the connector's paging, `Link`-header parsing and URI helpers, inherited from slices 01/02 and untouched here.

## Why this one is worth re-reading

Slice 04's lesson was that a written-down risk is worth nothing on its own. Slice 05's is narrower and, in a way, more uncomfortable: **the things missing here were not risks anyone had written down, because nothing about the system looked incomplete.** The connector was well tested. The docs page existed and was good. The secrets were configured. Each layer, examined alone, passed. What was absent was a single thread running through all of them — one test that started at a text field in a browser and ended at a team reading real records — and its absence produced no symptom for four slices.

The general form: **coverage within layers hides gaps between them, and the better each layer is, the more confidently the gap goes unnoticed.** The cheapest defence is not more tests; it is one test per integration that refuses to respect layer boundaries, written at the first slice rather than the last.
