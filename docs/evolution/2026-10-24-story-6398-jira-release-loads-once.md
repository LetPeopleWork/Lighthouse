# Choosing a Jira Release loads once and never shows a guess — Story 6398

Delivered 2026-10-24 in four slices and 16 roadmap steps, ADO User Story #6398 (no parent Epic). Raised by a user on
Jira Data Center who ties a Delivery to a Jira Release most mornings. Pushed to `main` at `3828c1487`; not yet
released.

Opening Add Delivery and choosing a Jira Release cost three waits of 3 to 5 s, one after the other, and none of them
looked like loading. First the Jira Release tab was missing (the Portfolio's delivery sources were still on their
way, and the only message under the form said the Delivery needed a name). Then the tab said a premium licence was
needed (the licence was still unknown, and unknown read as "not premium"). Only then did the Release list start,
because it was fetched when the tab was drawn. A failed sources call also read as "this connection offers no Jira
Release", and the Release-list error said "try again" without saying how.

Now the dialog reads the sources the Deliveries page already has, starts the Release list as soon as it opens on a
premium licence, shows a loading look wherever it does not know yet, and says when something could not be loaded and
that closing and reopening the dialog retries it. The Manual Feature list and the rule fields follow the same rules.
Every Jira Release sweep writes one Information log line with how long Jira took and how much it read, so an operator
can tell whether Jira or Lighthouse is slow before anyone decides on caching.

Personas: `delivery-forecaster` (primary) and `platform-operator` (slice 03). Jobs reused:
`job-forecaster-bind-a-delivery-to-the-release-it-tracks` (slices 01, 02, 04) and
`job-operator-read-an-update-log-that-says-something` (slice 03). DISCOVER and DIVERGE were skipped by the maintainer
(MD-1). Ran in parallel with Epic 6400 and touches none of its files (MD-11, MD-12).

## What shipped

### Slice 01 — The dialog never shows a guess

| Step | What it changed |
| --- | --- |
| 01-01 | `premiumVerdictOf` reads the licence as premium, not premium or not known yet; only an object with a boolean `canUsePremiumFeatures` is an answer (`49fd1045a`). |
| 01-02 | Both premium tabs show a loading look while the licence is unknown; Rule-Based stays enabled with no premium tooltip; the source tab body carries `data-widget-status` `loading`, `error` or `ready` (`d4ac52db4`). |
| 01-03 | The tab row shows a busy "Loading…" tab while the sources load, and an amber notice when they could not be checked, in add and edit mode; a bound Delivery never opens on the Manual body while it waits (`be9946eee`). |
| 01-04 | The Release-list error asks to close and reopen the dialog, and the preview shows it is on its way (`8d8c2a1f1`); a second GREEN keeps a bound Delivery on no tab while its Release is on its way (`63996369a`). |

### Slice 02 — The Release list loads once

| Step | What it changed |
| --- | --- |
| 02-01 | A Portfolio's delivery sources are one shared keyed query, `["deliverySources", portfolioId]`, which the Deliveries page reads (`45dc3393e`). |
| 02-02 | The dialog reads the same query, so opening it sends no request, and asks again only when the page's last answer failed (`67eb3ee21`). |
| 02-03 | The Release list is a query per source and per opening, started as soon as the dialog opens on a premium licence (`3f0d6bc3c`). |
| 02-04 | The module-level `Map` and `clearDeliverySourceOptionsCache` go; an enforcement test holds the Deliveries folder to asking the server only through its hooks (`3627c5f35`). |

### Slice 04 — The Manual Feature list and the rule fields are never stale

| Step | What it changed |
| --- | --- |
| 04-01 | One pure `dialogQueryStatusOf` says what a body shows: `loading`, `error` or `ready`; a query that has not started reads as loading, never ready (`d52978dd1`). |
| 04-02 | `FeatureSelector` and `FeatureGrid` gain an optional `loading` prop, passed to `DataGridBase`'s existing overlay (`382926340`). |
| 04-03 | The Manual Feature list is asked once per opening, shows the grid's loading overlay and an amber notice on failure, never an earlier opening's list (`4cd77d338`). |
| 04-04 | The rule fields are asked once per opening behind a latch, never loop after a failure and never flash the could-not-load notice before the first answer (`bb91ba8b1`). |

### Slice 03 — The Release list says what it cost

| Step | What it changed |
| --- | --- |
| 03-01 | `JiraReleaseSweepCost` writes one sweep's counts and time as one Information line that names no customer (`9654f01e6`). |
| 03-02 | A picker sweep writes one line through a per-sweep `JiraReleaseSweepTally`; a list served from the 5-minute cache writes none (`8dfb016da`). |
| 03-03 | A sweep that fails, or cannot reach Jira, still writes what it had counted, from a `finally`; an unexpected exception still reaches the caller (`5f803094c`). |
| 03-04 | Every caller names its sweep's purpose (`DeliverySourceSweepPurpose`: Picker, Preview, Save, Refresh) through the resolver and the provider (`f961e1a65`), and the line is documented in `docs/settings/systeminfo.md` (`7a5131aaf`). |

Around the steps: refactor passes (`c670f1f63`, `545df086f`, `44abdff14`, `7c311e001`, `e201f35dc`, `6a61bcdb5`,
`db8c718eb`, `f945c47be`), pinning tests (`4cbf4df26`, `f2bf84823`, `eb0de15f9`, `462bfae41`, `a0787b6d0`), a second
docs pass on the sweep line (`801d33199`), one fix that keeps the Selection Mode tabs shut while a bound Delivery waits
for its tab (`a35f7cf26`), and the two mutation kill rounds (`eba8b25e1`, `097867515`). Every step has a complete DES
trace (RED, GREEN, COMMIT); 01-03 and 04-03 each needed a second GREEN. Slices ran 01, 02, 04, 03; each ships alone.

## Decisions

- **Unknown is never shown as an answer, and failure never as "no"** (D1, D2). The premium notice, the locked
  Rule-Based tab and its tooltip appear only once the licence is known not to be premium (DDD-1, MD-28c).
- **One shared keyed query for the sources, read by the page and the dialog** (DDD-2), not props threaded down and not
  a request of the dialog's own. Opening the dialog asks again only after a failed answer, once per opening.
- **The Release list, the Feature list and the rule fields are queries per Portfolio and per opening** (DDD-3, DDD-9):
  the dialog bumps an opening counter when it closes, so a late answer lands on a key nobody reads and reopening is
  the one retry. Starting the Release list on every premium opening is intended, also on Manual (MD-27).
- **No Try again button anywhere** (MD-23): both failure looks ask to close and reopen the dialog. The sources and
  Feature-list notices are amber `warning` Alerts (MD-22, MD-29). No new control.
- **The preview keeps its hand-written late-answer guard** (DDD-10), the one named exception to keyed queries.
- **One Information line per sweep, written by the connector, naming no customer** (DDD-6): no host, connection,
  project, Release or credential. The refresh, the preview and the save sweeps log it too, each with its purpose
  (MD-17, DDD-11). Counts sit in a tally updated as they become known, so a failed sweep still reports them.
- **Caching is decided from measurement** (MD-9): if the reporter's cold-cache median is over 2 s, follow-up F2 chooses
  between a longer cache and a narrower sweep.
- **The walking skeleton was folded into the existing Jira Release binding skeleton** (MD-28), which now waits for
  `data-widget-status="ready"`.
- **No usage-data event** (DEVOPS, MD-18): the Story adds no new action; a Delivery-saved event is F4.
- Recorded in [ADR-263](../product/architecture/adr-263-the-delivery-dialog-reads-its-sources-from-the-page-s-shared-query-and-its-release-list-from-a-query-per-opening.md)
  and [ADR-264](../product/architecture/adr-264-every-jira-release-sweep-writes-one-information-line-of-counts-and-time-that-names-no-customer.md)
  (both now Accepted) and the Story's section of [`brief.md`](../product/architecture/brief.md).

## Quality

- Mutation, on frozen code at `a0787b6d0`: **frontend 89.95 %** (358 of 398) after one kill round, from 85.68 %; four
  of the 39 survivors are false (each fails the suites when applied by hand), and the rest are styling or equivalent.
  `DeliverySourceTab.tsx` (71.05 %) and `useDeliverySourceOptions.ts` (74.07 %) are under 80 % on their own.
  **Backend 100 %** (22 of 22) after one kill round on `DeliverySourceResolver.cs`, from 86.36 %; the sweep inside
  `JiraWorkTrackingConnector.cs` was not mutated (the file is too large for whole-file mutation and Stryker.NET
  ignores line spans) and is covered by `JiraReleaseSweepLineTest`. Details:
  `docs/feature/story-6398-jira-release-loads-once/mutation/results-frontend.md` and `results-backend.md`.
- No HTTP contract changed; `DeliverySourceSweepPurpose` is an internal parameter.
- Six `OUT-6398-*` KPI contracts in `docs/product/kpi-contracts.yaml`.

## Lessons

- **Two of the three waits were not Jira.** Waits 1 and 2 were `PortfolioRepository.GetById` and the licence hook's
  `/teams` and `/portfolios` calls, both reading every Portfolio. They belong to Epic 6400; this Story made the dialog
  honest and parallel without touching them.
- **A status derived from `isFetching` alone reads a query that has not started as `ready`.** The metrics widgets'
  `queryStatusOf` would have shown an empty grid as the answer; `dialogQueryStatusOf` reads data, error and fetching.
- **The rule-fields effect re-asked in a loop after a failure** (N3, confirmed in code at DESIGN). A latched, keyed
  query asks once per opening and has a pending state from the first render.

## Known limits and follow-ups

- **F1** A failed licence check (any of the hook's `/license`, `/teams`, `/portfolios`) leaves the premium tabs on
  the loading look until the dialog is reopened. That is today's licence hook, owned by Epic 6400 (not started); a new
  Story gives the tabs a licence failure look once the hook reports a failed status (MD-12, MD-28a).
- **F2** The cache decision, if the MD-9 gate says so; it also decides single-flight for a close and reopen during a
  running sweep (OQ-6).
- **F3** The existing Jira Release log lines still carry project keys, the could-not-ask line may carry a host in its
  exception text, and a Debug line writes the site address. Review them against the no-customer-identifiers rule.
- **F4** A Delivery-saved usage event (for example `DeliveryCreated` with a closed `Selection` enum). Trigger: the
  first slice touching the Delivery save path after Epic 6400 slice 02 ships. Not done here.
- The Release-list error notice stays red (`error`) while the newer sources and Feature-list notices are amber.
- The rule fields' `SchemaLoadError` still reads "Failed to load rule schema. Please try again." (MD-30 kept its copy).
- Update stays enabled while a bound Delivery's Release list is loading or has failed.
- The Feature list is not asked again if a Portfolio's Features change while the dialog is open.
- The folded Playwright skeleton (`DeliverySourcePicker.spec.ts`) has not been run locally; CI runs E2E.
- KPI readings come from production: the reporter's sweep line on Data Center (the MD-9 gate) and the
  click-before-licence and cold-cache throttled readings (MD-19, MD-28d).

## Open items for the maintainer

- ADO: the Release Notes tag on Story #6398 (MD-10 said yes; the tag itself waits for confirmation). Draft:
  > On Jira Data Center, choosing a Jira Release in the new-Delivery dialog meant three slow loads one after another, with a missing tab and a premium notice that were only guesses along the way.
  > The dialog now loads what it needs together, shows that it is loading instead of guessing, and says how to retry when something could not be loaded; each Jira Release read also logs how long Jira took.

## Artifacts

- Delivery history (preserved): `docs/feature/story-6398-jira-release-loads-once/` — `feature-delta.md`, `slices/`,
  `environments.yaml`, `deliver/roadmap.json`, `deliver/execution-log.json`, `mutation/`.
- Decisions: `docs/product/architecture/adr-263-…` and `adr-264-…`, already in their permanent place; nothing to
  migrate from the workspace (no `design/`, `distill/` or `discuss/` journey files).
- KPI contracts: `docs/product/kpi-contracts.yaml` (`OUT-6398-*`).
- User docs: `docs/settings/systeminfo.md` ("Jira Release sweeps"), `docs/portfolios/detail.md` ("Following a Jira
  Release").
