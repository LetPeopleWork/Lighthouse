<!-- markdownlint-disable MD024 -->
# Feature Delta - quiet-jira-writeback (Story 5500 "Prevent 'noise' when doing write-backs on Jira")

DISCUSS wave output. Density: lean + ask-intelligent, Tier-1 [REF] only. UX research depth: Lightweight
(brownfield, additive, one existing config surface). Premium feature. Feature-id: `quiet-jira-writeback`.
ADO: Story #5500, tag `Release Notes`, reported by Manuel and Chris.

## SPIKE-03 OUTCOME - 2026-08-08 (read this before DESIGN)

The spike ran against Jira **Cloud** only (`letpeoplework.atlassian.net`); **no DC instance is obtainable
before release**, so Q1/Q2 are deferred to post-release verification. Full evidence:
`slices/slice-03-spike-jira-notification-suppression.md`. Three pre-commitments broke:

| Was | Now |
|---|---|
| D7 unresolved: Cloud errors *or* silently ignores | **RESOLVED - Cloud 403s and drops the whole write** |
| D2: Cloud bulk = lower permission bar (slice 06's premise) | **DISPROVED - suppression needs admin on the bulk path too** |
| D10 / slice 02: one call = one email | **VOID - Jira batches per (recipient, issue); 1 PUT and 4 PUTs both sent 1 email** |
| D4: `AuthenticationMethodKey` discriminates Cloud/DC | **REPLACE with `serverInfo.deploymentType`** |
| D5: `mypermissions` predicts suppression | **CONFIRMED** - but must pass `projectKey`, else it over-reports |

**Decisions taken (user, 2026-08-08):**

1. **Slice 04 - optimistic retry.** Always send `notifyUsers=false`; on 403 retry without it. Always-on (D3)
   is safe only because of this fallback - without it the slice is a regression for every customer whose
   credential lacks admin/project-admin. `mypermissions` is *not* a gate in the write path.
2. **Slice 06 - deferred out of this epic.** Its permission-bar rationale is gone; only call-count
   reduction survives, and slices 01+02 already deliver that. ADO #5507 needs a state decision.
3. **Slice 05 becomes a reporting companion, not a prerequisite** - it surfaces "this connection cannot
   suppress notifications" rather than gating the write.

Also corrected here: the slice **files** carried stale internal headings from the pre-2026-07-17 numbering
(`slice-04-*.md` was headed "Slice 01", `slice-06-*.md` was headed "Slice 03"). Headings now match filenames
and the story table below.

Remaining verification debt: **string-typed** custom-field write-back is unverified (the test site has no
plain-text custom field); DC behaviour (Q1/Q2) is unverified until post-release.

**Scope decision (user, 2026-08-08): design for Cloud, assume DC behaves identically.** The maintainer will
verify DC once this is live; if DC turns out to differ, that becomes a **dedicated feature**, not a change
of course here. Practical consequences for DESIGN:

- Do **not** build a Cloud/DC branch, a deployment discriminator, or DC-conditional behaviour. With slice 06
  deferred there is one code path, and D4 is therefore **dropped** rather than implemented.
  `serverInfo.deploymentType` stays recorded in the spike findings as the correct discriminator *if* one is
  ever needed; it is not needed now.
- The retry-on-403 fallback is what makes this assumption safe: whichever of the three possible DC behaviours
  turns out to be real (403 like Cloud / silent ignore / correct suppression), no customer ends up worse off
  than today. That is the property to preserve in design review.
- Docs and release notes state the **Cloud-verified** behaviour. Do not claim DC suppression works until the
  maintainer's post-release check confirms it.

## [REF] Summary

Lighthouse writes forecast percentiles, feature size and work-item age back into Jira fields on every
Team/Portfolio update. Each write is a normal issue edit, so Jira emails **every watcher** of the issue.
Teams get spammed, and the admin's only escape today is to switch write-back off entirely - killing a
Premium feature to stop an inbox problem.

The noise has **two independent causes**, and the original DISCUSS pass only found the first:

1. **Every write emails - but as a per-issue digest, not per write.** The Azure DevOps connector
   suppresses unconditionally (`AzureDevOpsWorkTrackingConnector.cs:356`, `suppressNotifications: true`);
   the Jira connector at `JiraWorkTrackingConnector.cs:325` issues a bare
   `PUT rest/api/latest/issue/{id}` with no suppression parameter. A connector parity gap, not a missing
   platform capability. **Amended 2026-08-08 (SPIKE-03 Q9):** Jira Cloud batches watcher mail per
   (recipient, issue) over a ~10 minute window, so the noise a user actually feels is **one email per
   issue per window**, not one per write. Addressed by slice 04; slice 06 is deferred, its premise
   disproved.
2. **Lighthouse writes far more often than it needs to** - amplified on *both* write-back connectors, for
   reasons that have nothing to do with Jira (see "Write amplification" below). Addressed by slices 01-02.
   **Amended 2026-08-08 (DESIGN):** the pass-count amplifier is smaller than stated - `UpdateQueueService`
   already coalesces duplicate `(Forecasts, portfolioId)` triggers, so N Teams produce at most **two**
   forecast executions, not N. See ADR-144.

Cause 2 was discovered on 2026-07-17 and reframes the epic. It matters disproportionately because it is
**permission-free, deployment-free, and connector-agnostic** - the only lever that survived SPIKE-03
untouched, and it reduces the very channels D1 wrote off as unsuppressible. Its measured value is **API-call
count and issue-history churn** (4:1); the email claim is **void** (Q9).

The story's own doubt ("This may not be possible at all...") resolves to: **possible, for email only, and
only where the credential holds Administer Jira or Administer Projects on that project** - and separately,
a large part of the volume is self-inflicted and fixable outright. Where the credential lacks the
permission, Jira **403s and drops the whole write**, which is why suppression ships with an optimistic
retry ([ADR-142](../../product/architecture/adr-142-writeback-suppression-optimistic-retry.md)) rather
than the unconditional form D3 originally described.

## [REF] Write amplification (verified 2026-07-17)

**One API call per field, not per issue - on both connectors.** `WriteBackFieldUpdate` is one field on one
item, and both connectors loop the flat list:

- Jira `JiraWorkTrackingConnector.cs:307-325` - `UpdateItem` serializes a single-entry
  `fields = { [ref]: value }` dictionary and PUTs it. Jira's `fields` object accepts many fields per call.
- ADO `AzureDevOpsWorkTrackingConnector.cs:345-353` - `UpdateItems` builds a `JsonPatchDocument` with
  exactly one `JsonPatchOperation`. A patch document accepts many operations per call.

A feature with 4 percentile mappings + FeatureSize + WorkItemAge = **6 calls to the same issue in one
pass** = 6 emails.

**Multiple write-back passes per refresh cycle.** Three inline call sites, no coordination:

- `PortfolioUpdater.cs:79-85` - feature write-back after features refresh, then forecast write-back after
  forecasts. Two passes over overlapping issues.
- `ForecastUpdater.cs:43-44` - a third pass, triggered **per team** via
  `TeamDataRefreshedForecastTriggerHandler.cs:21-24`, which loops `team.Portfolios` and calls
  `forecastUpdater.TriggerUpdate(portfolio.Id)` for each. A portfolio with N teams gets **N forecast
  write-back passes per refresh round.**
- `TeamUpdater.cs:53-54` - team-level write-back.

**The passes do not deduplicate.** `WriteBackService.WriteFieldsToWorkItems` calls the connector and
returns results; it never writes the new value into the local `AdditionalFieldValues`. The stored copy
holds the pre-write value until the next inbound sync, so pass 2 compares fresh values against a stale
local copy, `currentAdditionalFieldValue != update.Value` fires again, and the same field is written again.
Slice 01's collect-and-flush resolves this by construction - no separate fix needed.

Rough order of magnitude per issue per refresh round: **~4N calls for N teams**, against a floor of ~1.

## [REF] Scope reality (verified against Atlassian docs, not assumed)

Verbatim from [JRASERVER-34423](https://jira.atlassian.com/browse/JRASERVER-34423), the ticket that
shipped `notifyUsers`:

> "This will **only** affect email notifications - changes will still be logged, added to the issue
> change history, events will still be sent to listeners, webhooks will still fire."

| Noise channel | Suppressible? | Mechanism |
|---|---|---|
| Watcher **email** on issue edit | YES | `notifyUsers=false` (DC + Cloud) / `sendBulkNotification:false` (Cloud bulk) |
| Issue history / changelog entry | **NO** | none exists, any deployment |
| `Updated` timestamp churn | **NO** | inherent to any field write |
| Webhooks / listeners / automation rules | **NO** | fire regardless |
| Notification-scheme "Single User" rules | **NO** | [bypass `notifyUsers` entirely](https://community.atlassian.com/forums/Jira-questions/Using-notifyUsers-parameter-still-fires-notifications-on-api-2/qaq-p/816532) |

Done means **"no watcher email"**, never "invisible". D1 locks this so we never over-promise.

## [REF] Mechanism landscape (verified)

| | `notifyUsers=false` (PUT issue) | Bulk edit `sendBulkNotification:false` |
|---|---|---|
| Jira Cloud | yes | yes (**v3 only**, not v2) |
| Jira DC / Server | yes (**7.2.0+**) | **no such API** |
| Permission | Administer Jira **or** project admin | "Make bulk changes" global + browse + edit |
| Shape | sync, per-issue | **async** - taskId + progress polling |
| Limits | one issue per call | <=1000 issues, <=200 fields per call |

The permission axis is decisive: **"Make bulk changes" is a far lower bar than Jira admin.** Asking a
customer to grant Lighthouse's service account `Administer Jira` to stop emails is a security trade most
admins will refuse - which would leave the feature unadopted even though it "works".

## [REF] Locked decisions

| ID | Decision |
|----|----------|
| D1 | "Quiet" is defined as **no watcher email**. History, `Updated`, webhooks, listeners and automation rules cannot be *suppressed* on any Jira deployment (JRASERVER-34423, verbatim above). Copy and docs must say "no email notifications", never "silent" or "invisible". **Amended 2026-07-17:** unsuppressible per write, but the *number of writes* was never examined and is not fixed - D9/D10 cut it by roughly 6x, which proportionally cuts history entries, `Updated` churn and webhook/automation firings. D1 still bars promising their absence; it no longer implies their volume is untouchable. |
| D2 | **Deployment-split end state**: Jira Cloud -> bulk edit API `POST /rest/api/3/bulk/issues/fields` with `sendBulkNotification: false`; Jira DC/Server -> per-issue `PUT ...?notifyUsers=false`. Forced by reality: DC has no bulk API; Cloud batch needs only "Make bulk changes" instead of Jira admin. (User decision, 2026-07-16.) |
| D3 | Suppression is **ALWAYS-ON, no toggle**. Mirrors the ADO connector, which suppresses unconditionally with no setting (`AzureDevOpsWorkTrackingConnector.cs:356`). No new settings field, no DTO change, no EF migration, no new UI control. (User decision, 2026-07-16.) |
| D4 | Deployment discriminator = the connection's `AuthenticationMethodKey`: `jira.datacenter` -> DC path; `jira.cloud` / `jira.scopedtoken` / `jira.oauth` -> Cloud path. Cheap and already persisted. **Confirmed by SPIKE-03** - DC also supports OAuth, so `jira.oauth` must be proven Cloud-only before it is routed Cloud. |
| D5 | **Honesty gate.** Lighthouse pre-checks the required permission via `GET /rest/api/{2,3}/mypermissions` and surfaces the verdict on the Jira connection. Lighthouse never claims quiet write-backs it cannot deliver. Directly inherits the product's existing "no false certainty" stance (`job-forecast-no-false-certainty`). (User decision, 2026-07-16.) |
| D6 | **Slicing refinement (deviation from a literal reading of D2, same end state).** Slice 01 applies `notifyUsers=false` to **both** Cloud and DC, because Cloud supports it too and it is a query-param change - ADO parity ships in ~half a day for every Jira customer who has admin. Slice 03 then upgrades Cloud to the bulk API, whose value is precisely *"you no longer need to grant Lighthouse Jira admin"*. End state is exactly D2. Cost of the interim: one query param, discarded in slice 03. |
| D7 | **SPIKE-03 gates D4 and D5.** Atlassian's evidence conflicts on the permission-missing failure mode: Cloud docs say the request is silently ignored (204 + emails still sent), while a community report quotes a hard error *"To discard the user notification either admin or project admin permissions are required."* Silent-ignore is the dangerous case - Lighthouse would report success while the storm continues. Not designable on guesswork. |
| D8 | ~~Out of scope: ... ADO (already correct), and write-back cadence/volume. Cadence is confirmed already-correct: `WriteBackService.GetChangedFields` only emits updates where `currentAdditionalFieldValue != update.Value`, so Lighthouse writes genuine changes only. Noise is Jira-side, per user confirmation 2026-07-16.~~ **OVERTURNED 2026-07-17.** The no-op guard is real but bounds only *value* changes, not *call count*. Two amplifiers went unexamined - one call per field rather than per issue, and N+2 uncoordinated passes per refresh round (see "Write amplification"). Noise is **substantially Lighthouse-side**, and ADO is *not* already correct: it suppresses email but emits the same per-field call storm, so it churns work-item revisions. **Still out of scope:** Linear and CSV (both `throw new NotSupportedException("Write-back is not supported for ...")`). |
| D9 | **Event-driven write-back collection** (slice 01). Write intents are collected across a refresh cycle and flushed **once** at the end, replacing three uncoordinated inline call sites. Applies to both write-back-capable connectors (Jira, ADO) - the seam sits above the connector. Rationale: architectural seam first, so slice 02 groups once against the final shape rather than per-pass. (User decision, 2026-07-17.) |
| D10 | **Per-issue field batching** (slice 02). Group `WriteBackFieldUpdate` by `WorkItemId`; one Jira PUT with a multi-field `fields` object, one ADO `JsonPatchDocument` with multiple operations. Both connectors. Permission-free, deployment-free, and the only lever that survives a bad SPIKE-03. The "one call = one email" premise is **assumed, not verified** - SPIKE-03 Q9 checks it, and the email claim ships in no doc or release note until it reports. |
| D11 | **Forecast re-simulation jitter is out of scope.** Monte Carlo re-simulation can flip a percentile date by a day, making each pass a "genuine" change and defeating the `!=` guard. Raised 2026-07-17 as a possible noise floor; **user decision: ignore, do not design around it.** No hysteresis, no write-threshold, no local-value-after-write slice. (User decision, 2026-07-17.) **Scoped exception, 2026-08-08 (user, D-A7-R):** persisting a value *just successfully written to the tracker* into the local `AdditionalFieldValues` is permitted. It does not damp jitter — a re-simulated percentile genuinely differs and is still written — it makes the stored copy true rather than stale. D11's bars on hysteresis and write thresholds stand unchanged. |

## [REF] Technical grounding (verified)

- `Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs:325` - the offending
  call: `await client.PutAsync($"rest/api/latest/issue/{update.WorkItemId}", content)`. No `notifyUsers`.
  Note `rest/api/latest` - the Cloud bulk API is **v3-only**, so slice 03 cannot ride `latest`.
- `Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:356` -
  `witClient.UpdateWorkItemAsync(patchDocument, workItemId, suppressNotifications: true)`. The parity target.
- `Services/Implementation/WriteBackService.cs:96` - `GetChangedFields` suppresses no-op *value* writes, but
  **not** duplicate calls (D8 overturned). It also loads every feature and every work item
  (`featureRepository.GetAll()` + `workItemRepository.GetAll()`) and then rescans the full list per update
  via `allItems.Where(x => x.ReferenceId == update.WorkItemId)` inside the foreach - O(updates x items).
  Slice 02's group-by makes a dictionary lookup the natural shape.
- `Services/Implementation/WriteBackService.cs` - **never writes the new value back into the local
  `AdditionalFieldValues`.** The stored copy stays stale until the next inbound sync, so repeat passes in
  the same cycle re-detect the same "change". Root of the multi-pass duplication; resolved by slice 01.
- `Services/Implementation/WriteBackTriggerService.cs` - `TriggerWriteBackForTeam(Team)`; gated on
  `licenseService.CanUsePremiumFeatures()`; fires on Team/Portfolio update. Line 56: already catches and
  swallows every exception, so the dispatcher's swallow-and-log is not a new loss of signal.
- `Services/Implementation/DomainEvents/DomainEventDispatcher.cs:11-12` - **singleton**
  (`Program.cs:1052`), creates its **own scope per publish** via `serviceScopeFactory.CreateScope()`.
  Handlers never share the publisher's scope. `PublishAsync` awaits handlers inline, sequentially, in DI
  registration order. `InvokeHandlerSafely` catches all (`CA1031` suppressed with justification). All three
  facts constrain slice 01's design - see the slice brief.
- `Services/Implementation/BackgroundServices/Update/TeamDataRefreshedForecastTriggerHandler.cs:21-24` -
  loops `team.Portfolios` and calls `forecastUpdater.TriggerUpdate(portfolio.Id)` per portfolio. The
  N-teams write-back amplifier.
- `Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs:79-85` - two write-back passes per
  cycle (feature, then forecast). `ForecastUpdater.cs:43-44` - a third. `TeamUpdater.cs:53-54` - team-level.
- `Services/Implementation/WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs:307-325` - `UpdateItem`
  PUTs exactly **one field** per call. `AzureDevOpsWorkTrackingConnector.cs:345-353` - `UpdateItems` builds
  a `JsonPatchDocument` with exactly **one operation** per call. Both accept many per call. Slice 02's target.
- `Services/Implementation/WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs:320-325` -
  chunks via `Chunk(MaxChunkSize)`, parallelises with `Task.WhenAll`, throttles via `ExecuteWithThrottle`.
  The Jira connector has **none of these** - fully sequential, no throttle. Out of scope, logged.
- Write-back-capable connectors are **Jira and ADO only**. `CsvWorkTrackingConnector.cs:420` and
  `LinearWorkTrackingConnector.cs:812` both throw `NotSupportedException`. "All connectors" = 2.
- `Models/WriteBack/WriteBackValueSource.cs` - `WorkItemAgeCycleTime`, `FeatureSize`,
  `ForecastPercentile50/70/85/95`. Forecast percentiles move often -> genuine changes are frequent ->
  real email volume.
- `Services/Implementation/WorkTrackingConnectors/AuthenticationMethodKeys.cs` - `jira.cloud`,
  `jira.datacenter`, `jira.scopedtoken`, `jira.oauth` (D4 discriminator).
- `Models/WriteBack/WriteBackResult.cs` / `WriteBackItemResult.cs` - **sync, per-item** result contract.
  The Cloud bulk API is async (taskId + polling); slice 03 must preserve this contract or change it
  deliberately. This is the single biggest design risk in the feature.
- No `docs/` page mentions write-back today (grep: zero hits) -> DELIVER must add one (D2 mechanics +
  the permission each deployment needs). User explicitly asked for the docs update.

## [REF] Personas (SSOT)

- **config-admin** (`docs/product/personas/config-admin.yaml`) - PRIMARY and only actor. Owns the
  work-tracking Connection and the write-back mappings; the person who enables write-back, gets the
  complaints, and switches it off.

Watchers (the whole team) are *affected* by the noise but never act in this journey - they are not a
persona here. No new personas invented.

## [REF] JTBD one-liners (SSOT: `docs/product/jobs.yaml`)

- **job-config-admin-quiet-jira-writeback** (config-admin) - "Keep write-back switched on without
  emailing every watcher on every sync." Opportunity: importance 4 / satisfaction 0 / **gap 4**.
- **job-config-admin-know-writeback-is-quiet** (config-admin) - "See upfront whether Lighthouse can
  actually be quiet with the credential I gave it." Opportunity: importance 3 / satisfaction 0 / **gap 3**.

Both appended to `jobs.yaml` with `feature_context: quiet-jira-writeback`, `created: 2026-07-16`;
`config-admin.primary_jobs` updated.

## [REF] Opportunity scores

| Job | Importance | Satisfaction | Gap | Rationale |
|---|---|---|---|---|
| job-config-admin-quiet-jira-writeback | 4 | 0 | **4** | Two named customers (Manuel, Chris) reported it. Satisfaction 0: no suppression exists on the Jira path at all, and the only workaround - disabling write-back - destroys the feature. ADO already has it, so this is also a credibility gap. |
| job-config-admin-know-writeback-is-quiet | 3 | 0 | **3** | Lower importance (it enables trust rather than removing the pain) but satisfaction is 0 and the silent-ignore failure mode (D7) makes it the difference between a fix and a false promise. |

## [REF] User Stories

Slice order is 01 (event-driven) -> 02 (batching) -> 03 (spike) -> 04 (`notifyUsers`) -> 05 (pre-check) ->
06 (bulk API). Slices 01-02 are cross-connector (Jira + ADO); 03-06 are Jira-only. Story numbering is
authoring order, not slice order - US-04/US-05 map to slices 01/02.

### US-04 - Stop writing the same value over and over

**job_id:** `job-config-admin-quiet-jira-writeback` | **Slice:** 01 | **Connectors:** Jira + ADO

As a Configuration Administrator, I want Lighthouse to write each changed value once per refresh cycle, so
that my team's inbox reflects real forecast movement rather than Lighthouse's internal refresh topology.

#### Elevator Pitch
Before: a portfolio with 4 teams runs 4 forecast write-back passes per refresh round, writing the same percentile to the same issue 4 times - 4 emails, one real change.
After: one write-back flush per cycle, one write per genuinely-changed field.
Decision enabled: I stop counting Lighthouse's refresh passes in my inbox.

#### Acceptance Criteria
See `slices/slice-01-event-driven-writeback-collection.md` (AC-04.1 - AC-04.7). Key constraints: the
dispatcher runs handlers in a **fresh scope** (`DomainEventDispatcher.cs:11-12`, singleton per
`Program.cs:1052`), so a scoped accumulator shared between updater and handler is two instances;
`PublishAsync` awaits inline in DI registration order, so publishing does not defer; handler exceptions are
swallowed and logged (parity with today's `WriteBackTriggerService.cs:56`).

### US-05 - One write per issue, not one per field

**job_id:** `job-config-admin-quiet-jira-writeback` | **Slice:** 02 | **Connectors:** Jira + ADO

As a Configuration Administrator, I want Lighthouse to write all changed fields on an issue in a single
call, so that a rich mapping set does not cost me one email per mapping.

#### Elevator Pitch
Before: six mapped fields change on a feature, six API calls hit the same issue, six emails land in the same minute.
After: one call, one email, one history entry.
Decision enabled: I keep all my mappings instead of trimming them to survive the inbox.

#### Acceptance Criteria
See `slices/slice-02-batch-writeback-fields-per-issue.md` (AC-05.1 - AC-05.7). Note D10: the
"one call = one email" premise is unverified until SPIKE-03 Q9; the API-call, history and churn reductions
hold regardless.

### US-01 - Quiet write-back on Jira (both deployments)

**job_id:** `job-config-admin-quiet-jira-writeback` | **Slice:** 04

As a Configuration Administrator, I want Lighthouse's Jira write-backs to not email every watcher, so
that I can keep write-back enabled instead of switching it off to stop the complaints.

#### Elevator Pitch
Before: every Lighthouse write-back to Jira emails every watcher of the issue, so the team asks me to turn write-back off.
After: trigger a Team update from Team Settings -> the forecast percentile lands in the Jira field and **no watcher receives an email**.
Decision enabled: I leave write-back switched on.

#### Acceptance Criteria
- AC-01.1: Given a Jira connection with a write-back mapping and a changed forecast value, when a Team
  update triggers write-back, then the issue PUT carries `notifyUsers=false` and the field is updated.
- ~~AC-01.2: Given a real Jira DC instance with a watcher on the issue, when write-back updates the field,
  then the watcher receives no email, and the change is present in the issue history.~~ **RETIRED
  2026-08-08 as a release gate** - no Data Center instance is obtainable before release. Moved to the
  post-release DC checklist at the end of `slices/slice-03-spike-jira-notification-suppression.md`
  (Q1/Q2/Q10). Safe to ship without it because the 403 retry (ADR-142) leaves no DC customer worse off
  under any of the three possible behaviours. The history-entry assertion survives on AC-01.3 (D1).
- AC-01.3: Given the same on a real Jira Cloud instance with an admin credential, then no watcher email.
- AC-01.4: Given the ADO connector, when write-back runs, then behaviour is unchanged (`suppressNotifications: true`).
- AC-01.5: Given `GetChangedFields` finds no changed value, when write-back runs, then no Jira call is
  made at all (regression guard on D8).

### US-02 - Know whether write-back will actually be quiet

**job_id:** `job-config-admin-know-writeback-is-quiet` | **Slice:** 05

As a Configuration Administrator, I want the Jira connection to tell me whether Lighthouse's credential
can actually suppress notifications, so that I find out before my team does.

#### Elevator Pitch
Before: I cannot tell whether write-backs will email my team until the complaints arrive.
After: open the Jira connection in Settings -> see `Write-backs will email watchers - grant "<permission>" to <account> to silence them` (or a confirmation that they are quiet).
Decision enabled: I grant the permission, or I accept the emails knowingly - either way I am not surprised.

#### Acceptance Criteria
- AC-02.1: Given a Jira connection whose credential **has** the required permission, when the connection
  settings page loads, then it states that write-backs will not email watchers.
- AC-02.2: Given a credential **lacking** the permission, then the page states that write-backs **will**
  email watchers and names the exact permission to grant and the account to grant it to.
- ~~AC-02.3: The required permission is deployment-correct per D4: DC -> `ADMINISTER` or
  `ADMINISTER_PROJECTS`; Cloud -> per SPIKE-03's verdict (`BULK_CHANGE` once slice 06 lands).~~
  **RETIRED 2026-08-08** - D4 is dropped, so there is no deployment discriminator to assert, and slice 06
  is Removed so `BULK_CHANGE` is never the answer. The requirement is the same on both deployments:
  Administer Jira, or Administer Projects **on that project**. Covered by AC-02.1/AC-02.2 as restated per
  D-A9 (per project, naming the affected ones).
- AC-02.4: Given the `mypermissions` probe fails or times out, then the page degrades to an unknown
  state and never blocks saving the connection or claims quiet.
- AC-02.5: Given a Linear, CSV or ADO connection, then no such status is shown (D8).

### US-03 - Quiet write-back on Cloud without granting Jira admin

**job_id:** `job-config-admin-quiet-jira-writeback` | **Slice:** 06

As a Configuration Administrator on Jira Cloud, I want quiet write-backs without granting Lighthouse
`Administer Jira`, so that I do not have to trade a broad security permission for a quiet inbox.

#### Elevator Pitch
Before: quiet Cloud write-backs require granting Lighthouse's account `Administer Jira` - a trade my security team refuses.
After: grant only "Make bulk changes", trigger a Team update -> forecasts land in Jira, no watcher email, no admin rights.
Decision enabled: I can adopt quiet write-back under least privilege.

#### Acceptance Criteria
- AC-03.1: Given a Jira **Cloud** connection, when write-back runs, then Lighthouse calls
  `POST /rest/api/3/bulk/issues/fields` with `sendBulkNotification: false` - not the per-issue PUT.
- AC-03.2: Given a Cloud credential with **only** "Make bulk changes" (no `Administer Jira`), then the
  write-back succeeds and no watcher email is sent.
- AC-03.3: Given a Jira **DC** connection, then the per-issue `notifyUsers=false` path is still used (D2).
- AC-03.4: Given the bulk submit returns a taskId, when Lighthouse polls progress to completion, then
  per-item outcomes map back onto `WriteBackResult.ItemResults` with the same success/failure semantics
  callers see today.
- AC-03.5: Given more than 1000 changed issues, then updates are chunked into <=1000-issue requests.
- AC-03.6: Given the bulk task fails or polling times out, then the failure is recorded per item and
  logged - never a silent success.

## [REF] Out of scope

- **Eliminating** issue history, `Updated` timestamps, webhooks, listeners, automation rules (D1 -
  impossible per write). Reducing their *volume* is now IN scope via D9/D10.
- **Forecast re-simulation jitter** (D11 - user decision 2026-07-17: ignore). No hysteresis, no write
  threshold, no local-value-update-after-write.
- Linear and CSV write-back (unsupported by design - both throw `NotSupportedException`).
- **Jira request throttling / concurrency parity with ADO.** Real gap (ADO chunks + `ExecuteWithThrottle`;
  Jira is sequential with none) but a separate concern - explicitly not smuggled into slice 02.
- Comment write-back and issue **transitions** - `notifyUsers` is
  [ignored on the transitions endpoint](https://jira.atlassian.com/browse/JRASERVER-67061); Lighthouse
  does neither today.
- Any per-connection toggle (D3).

**No longer out of scope (was, until 2026-07-17):** write-back cadence/volume, and ADO. D8 assumed both
were already correct; the write-amplification finding overturned that. See D8.

## [REF] WS strategy

**B - brownfield extension.** No walking skeleton. Write-back is shipped, licence-gated and exercised by
`JiraWriteBackTest` / `AzureDevOpsWriteBackTest`. This feature adds a collection seam above the connectors,
groups the connector payloads, changes one call's query string, adds one read-only status surface, and
swaps one transport. SPIKE-03 replaces the skeleton as the de-risking step for the Jira-specific half;
slices 01-02 are de-risked by the existing write-back test suites instead, since they need no live
instance.

## [REF] Driving ports

- Jira REST: `PUT /rest/api/2/issue/{id}?notifyUsers=false` (DC), `POST /rest/api/3/bulk/issues/fields`
  + bulk progress polling (Cloud), `GET /rest/api/{2,3}/mypermissions` (both).
- Lighthouse HTTP: existing work-tracking connection read/validate surface (US-02 status). No new write endpoint (D3).
- UI: Settings -> Work Tracking Systems -> Jira connection (US-02 status line).

## [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| Watcher emails per Jira write-back cycle | **0** on a credential with the required permission **on that project** | SPIKE-03 Q3 (Cloud, verified); DC deferred to post-release |
| **API calls per issue per write-back pass** | **1**, down from one-per-changed-field (~6 with a full mapping set) | AC-05.1 / AC-05.2 - both connectors |
| ~~**Write-back passes per refresh round** — **1**, down from N+2 for N teams~~ | **CORRECTED 2026-08-08 (DESIGN):** `UpdateQueueService` already coalesces duplicate `(Forecasts, portfolioId)` triggers, so the pre-change figure is **≈4 portfolio-level passes**, capped independently of N — not N+2. Target: **≤1 flush per update execution**, and **at most one write per `(work item, field)` per round** — the second half delivered by the D11 exception (D-A7-R), which makes a repeat pass find no change rather than needing to be coordinated away. | AC-04.1 as restated; ADR-144 |
| ~~**Watcher emails per issue per cycle without any granted permission** — **~1**, down from ~4N~~ | **VOID 2026-08-08 (SPIKE-03 Q9):** Jira Cloud digests watcher mail per (recipient, issue) over a ~10 min window, so the pre-change figure was already ~1, not ~4N. Slices 01-02 change **call count and history churn**, not email count. | Do not carry into docs or release notes |
| Jira connectors at ADO notification parity | 2/2 (Jira, ADO) | code assertion: every notification-capable connector attempts suppression |
| ~~Cloud permission required for quiet write-back — "Make bulk changes", not `Administer Jira`~~ | **VOID (SPIKE-03 Q5):** suppression needs Administer Jira **or** Administer Projects on the bulk path too. Slice 06 Removed. The requirement is Administer Jira / Administer Projects, per project, on both transports. | SPIKE-03 Q5 |
| Admins who learn suppression is off *before* their team does | 100% of connections with an under-permissioned credential surface it — **naming the affected projects** | AC-02.2, restated per D-A9 (OQ-3) |
| Write-back disable-rate attributable to noise | 0 new reports post-release | ADO #5500 follow-ups; Manuel + Chris confirm |

## [REF] Pre-requisites

- **SPIKE-03 must complete before slice 05 and slice 06 are designed** (D7). Slices 01, 02 and 04 do not
  depend on it - 01/02 are permission-free and deployment-free, 04 is a query param.
- Test access to a real Jira **Cloud** site and a real Jira **DC** instance (7.2.0+), each with an issue
  that has a watcher, and **two** credentials per site: one with the elevated permission, one without.
  Without the non-admin credential, the silent-ignore case (the whole point of D5) cannot be observed.
- Premium licence in the dev seed (`reference_premium_license_dev_seed`) - write-back is licence-gated.

## [REF] Definition of Done

1. Jira write-back sends no watcher email on **Cloud** with a credential holding Administer Jira or
   Administer Projects on that project. **DC half retired 2026-08-08 as a release gate** - no instance is
   obtainable; it moves to the post-release checklist at the end of
   `slices/slice-03-spike-jira-notification-suppression.md` (Q1/Q2/Q10).
2. ~~Cloud path needs only "Make bulk changes" - never `Administer Jira` (AC-03.2).~~ **RETIRED
   2026-08-08** - SPIKE-03 Q5 disproved it: suppression needs admin or project-admin on the bulk path too.
   Slice 06 is Removed and AC-03.2 with it.
3. Under-permissioned credentials produce an honest signal - a Warning per connection per flush (ADR-142)
   and a per-project panel on the connection (ADR-145) - never a false claim (D5).
4. ADO behaviour unchanged; Linear, CSV and ServiceNow untouched.
5. ~~`WriteBackResult` per-item semantics preserved across the Cloud transport swap (AC-03.4).~~
   **RETIRED 2026-08-08** - there is no transport swap; slice 06 is Removed. Per-item semantics must still
   survive **batching**, which is AC-05.4/AC-05.8 and ADR-143 §3.
6. Backend `dotnet build` zero warnings + `dotnet test` green; frontend `pnpm test` + `pnpm build` clean.
7. SonarQube Cloud: no new issues.
8. Mutation kill rate >=80% on changed backend + frontend units.
9. Docs page for write-back added, stating the permission needed **per Jira project** (Cloud-verified
   only), and D1's email-only scope.

## [REF] Scope Assessment

**Signals evaluated (Phase 1.5, re-run 2026-07-17):** 5 user stories (<10, pass) | 1 bounded context -
WorkTracking-Integration, plus a thin settings-UI read surface (<3, pass) | no walking skeleton
(brownfield) | estimated ~4.5-6 days incl. spike (<2 weeks, pass) | **multiple independent user outcomes
that could ship separately - FIRES** (write dedup, per-issue batching, DC quiet, permission visibility and
Cloud least-privilege each ship and deliver value alone).

**One signal of five. Verdict: right-sized - PASS.** No split required; the independent outcomes are
handled as five carpaccio slices plus a spike rather than separate features. Scope grew from 3 slices to
5 + spike on 2026-07-17 (write amplification), and the estimate roughly doubled, but it stays inside the
2-week bound and the bounded-context count is unchanged.

**ADO:** restructured 2026-07-17 with user confirmation. #5500 converted in place from User Story to
**Epic** "Quiet write-back" (state `Planned`, tag `Release Notes`, `ReportedBy: Manuel, Chris` preserved),
with one child Story per slice, all `New`, no iteration set:

| Slice | Story | Title |
|---|---|---|
| 01 | **#5502** | Event-driven write-back collection |
| 02 | **#5503** | Batch write-back fields per issue |
| 03 | **#5504** | SPIKE: Jira notification suppression + permission failure mode |
| 04 | **#5505** | Jira write-back: notifyUsers=false on both deployments |
| 05 | **#5506** | Jira write-back: permission pre-check + connection status |
| 06 | **#5507** | Jira write-back: Cloud bulk edit API (least privilege) |

Epic retitled from "Prevent 'noise' when doing write-backs on Jira" - the original framing predates the
write-amplification finding and slices 01-02 touch the ADO connector too. `Release Notes` stays on the Epic
only (user decision): `release-notes.md:76` runs a second pass for tagged items in `Resolved`, which is
where Op 6 parks the Epic once children merge but before release, so the umbrella entry fires correctly.

## [REF] Slices

| # | Slice | Est | Connectors | Ships |
|---|---|---|---|---|
| 01 | Event-driven write-back collection (collect intents, flush once) | ~1-1.5d | Jira + ADO | US-04 |
| 02 | Batch write-back fields per issue | ~0.5-1d | Jira + ADO | US-05 |
| 03 | SPIKE - suppression + permission failure mode + Q9 multi-field | ~0.5d | - | knowledge only, no ship |
| 04 | `notifyUsers=false` on both deployments -> ADO parity | ~0.5d | Jira | US-01 |
| 05 | Deployment-aware permission pre-check + connection status | ~1d | Jira | US-02 |
| 06 | Cloud -> bulk edit API `sendBulkNotification:false` (async) | ~1d | Jira | US-03 |

**Order = architectural seam first (user decision, 2026-07-17).** 01 establishes the collection seam that
02-06 all sit on, so grouping is written once against the final shape rather than per-pass and reworked.
01-02 are the cross-connector platform change; 03-06 are Jira-only. The spike sits at 03 because 01-02 do
not depend on it - they are permission-free and deployment-free - so it is pulled in only once the
Jira-specific work is next.

01 clears the DISCUSS slice-composition value gate on its own: collecting and flushing once removes the
duplicate passes, which is fewer writes and fewer emails before any batching or Jira work lands. It is not
an `@infrastructure`-only slice.

**Re-test after SPIKE-03:** slice 06's value story is least-privilege ("you no longer need to grant
Lighthouse Jira admin"). If 02 already lands most customers at ~1 email per issue per cycle, 06's marginal
value drops and its priority should be revisited rather than assumed.

Briefs: `docs/feature/quiet-jira-writeback/slices/`.

## [REF] Slice taste tests

- **Ship 4+ new components?** No - max is slice 06 (bulk transport + poller). Slice 01 adds one (the
  collection seam). PASS.
- **Every slice depends on a new abstraction?** No - only slice 01 introduces one; 02/04 are a group-by and
  a query param on existing calls. PASS.
- **Does any slice disprove a pre-commitment?** Yes - SPIKE-03 can disprove D2/D4/D5 outright; slice 04
  disproves "notifyUsers is sufficient" if watchers still get mail; slices 01-02 already disproved D8. PASS.
- **Synthetic data only?** Mixed, deliberately. Slices 01-02 are fully assertable against the existing
  `JiraWriteBackTest` / `AzureDevOpsWriteBackTest` suites - call count and payload shape are observable
  without a live instance. Slices 04-06 are **not**: a mocked HttpClient can prove the query param is on
  the URL but can **never** prove an email was not sent, so they assert against a real Jira instance with a
  real watcher. PASS.
- **2+ slices identical except for scale?** No. PASS.
- **Every slice has a user-visible value story?** Yes - 01/US-04, 02/US-05, 04/US-01, 05/US-02, 06/US-03.
  No slice is `@infrastructure`-only; slice 01 was checked explicitly against this gate and clears it,
  because collect-and-flush removes duplicate writes on its own. PASS (slice composition hard gate).

Slice 03 is a probe, not a shipping slice - it is exempt from the value gate by design (D7).

## [REF] Requirements completeness

**0.94.** Every story has a job_id, an elevator pitch with a real entry point, and testable ACs; KPIs carry
numeric targets; mechanisms verified against primary sources or read directly from the code rather than
assumed. Residual gaps, both deliberately left to SPIKE-03 rather than guessed:

- D7's failure mode (silent-ignore vs 403) - gates slices 05-06.
- D10's "one call = one email" premise (Q9) - gates only slice 02's *email* claim, not the slice itself.

Down from 0.96 on 2026-07-17. Not a quality regression but a scope correction: the write-amplification
finding added a second unverified premise (Q9) alongside D7's.

## [REF] DoR Validation

| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | Business value articulated | PASS | Premium feature currently switched off to stop emails; 2 named reporters |
| 2 | Job traceability | PASS | 2 jobs in `jobs.yaml`, every story mapped (US-04/US-05 -> `job-config-admin-quiet-jira-writeback`) |
| 3 | Acceptance criteria testable | PASS | AC-01.1-5, AC-02.1-5, AC-03.1-6, AC-04.1-7, AC-05.1-7 |
| 4 | Dependencies identified | PASS | Real Cloud + DC access, 2 credentials per site, premium seed - all for slices 03-06 only; slices 01-02 need none |
| 5 | Sized / sliced | PASS | 5 slices + spike, each <=1.5d; scope assessment re-run 2026-07-17, PASS |
| 6 | Technical feasibility | **CONDITIONAL** | Slices 01-02 read directly from code, no external dependency - fully feasible. Email suppression verified via primary sources. Permission-missing behaviour gated on SPIKE-03 (D7); D10's email claim gated on Q9 |
| 7 | UX defined | PASS | One read-only status line on an existing connection surface (D3 = no new controls). Slices 01-02 have no UI surface at all |
| 8 | Out-of-scope explicit | PASS | D1 + D8 (overturned) + D11 + Out of scope section |
| 9 | Measurable outcome | PASS | Outcome KPIs, primary target = 0 watcher emails; secondary = 1 call/issue, 1 pass/round |

**DoR: PASS with one condition** - item 6 is satisfied for slices 01, 02 and 04, and open for slices 05-06
until SPIKE-03 reports. **Slice 01 may proceed to DESIGN now**, and unlike the previous ordering, nothing
in the critical path waits on external Jira access.

## [REF] Handoff

**To:** nw-solution-architect (DESIGN) + nw-platform-architect (DEVOPS, `outcome-kpis` only).
**Blocking:** SPIKE-03 before slices 05-06 are designed. Slices 01, 02 and 04 are unblocked; **slice 01 is
next.**
**Key design questions carried forward:**

- (a) **Where the collection seam lives** (slice 01, D9) - the dispatcher is a singleton that creates its
  own scope per publish (`DomainEventDispatcher.cs:11-12`, `Program.cs:1052`), so handlers never share the
  publisher's scope and a scoped accumulator would be two instances. Payload-in-event, correlation-keyed
  singleton, or explicit end-of-cycle flush - pick deliberately, and keep write-back ordering out of DI
  registration order.
- (b) **Preserving `WriteBackResult` per-item semantics** twice over: across per-issue batching (slice 02,
  AC-05.3/AC-05.4) and across the async bulk transport (slice 06, AC-03.4). Same contract, two pressures.
- (c) Whether `jira.oauth` reliably implies Cloud (D4).
- (d) Where the deployment discriminator lives - connector-internal vs a routing seam.
- (e) **Jira has no throttle.** ADO chunks and parallelises with `ExecuteWithThrottle`
  (`AzureDevOpsWorkTrackingConnector.cs:320-325`); Jira is fully sequential with none. Out of scope for
  slice 02 by design - logged here so it is not lost.

---

# Wave: DESIGN — 2026-08-08

Architect: Morgan (nw-solution-architect). Interaction mode **propose**. Density **lean** — Tier-1 `[REF]`
sections only; Tier-2 expansions are catalogued below and deliberately not rendered.

**Scope designed:** slices 01, 02, 04, 05 (ADO #5502, #5503, #5505, #5506).
**Out:** slice 06 / #5507 — Removed, its least-privilege premise disproved by SPIKE-03 Q5. Not designed,
not carried as future work beyond what the briefs already record.

## Wave: DESIGN / [REF] Changed Assumptions (back-propagation)

| # | Upstream claim | Where | Status after DESIGN | Action taken |
|---|---|---|---|---|
| CA-1 | `[REF] Summary`: "Every write emails" and suppression is always-on, mirroring ADO (D3 as written) | `[REF] Summary`, D3 | **Reconciled** with the SPIKE-03 OUTCOME block | Summary rewritten in place, dated and marked amended; D3 survives *because of* the ADR-142 retry, not unconditionally |
| CA-2 | "Addressed by slices 04-06" | `[REF] Summary` cause 1 | **Stale** — slice 06 deferred | Summary now says slice 04, premise-disproved note on 06 |
| CA-3 | Email is per write | `[REF] Summary`, D10, US-05 pitch | **Void** (Q9) — Jira Cloud digests per (recipient, issue) per ~10 min | Summary amended; slice 02's value story is call count + history churn only. Already corrected in `slice-02-*.md`; the Summary was the last place still carrying it |
| CA-4 | "N+2 write-back passes per refresh round"; KPI "1, down from N+2 for N teams" | `[REF] Write amplification`, `[REF] Outcome KPIs`, AC-04.1 | **Overstated** — `UpdateQueueService` coalesces duplicate `(Forecasts, portfolioId)` triggers, so N Teams yield **at most two** forecast executions. Real portfolio-level count ≈4, not N+2 | Summary amended; ADR-144 records the mechanism; **AC-04.1 and the KPI need restating in DISTILL** (OQ-3) |
| CA-5 | D4 — deployment discriminator via `AuthenticationMethodKey`, replaced by `serverInfo.deploymentType` | D4, AC-02.3, carried question (c)/(d) | **Dropped, not implemented** (user scope decision) | No discriminator is designed. Carried questions (c) and (d) are closed as not-applicable |
| CA-6 | D5 — `mypermissions` is the honesty gate in the write path | D5, slice 05 brief | **Split**: it is not a gate (ADR-142), it is the pre-flight *reporting* source (ADR-145) | Recorded in both ADRs; slice 05 is a reporting companion, per the 2026-08-08 user decision |
| CA-7 | "Write-back-capable connectors are Jira and ADO only… 'All connectors' = 2" | `[REF] Technical grounding` | **Incomplete** — there are **five** `IWorkTrackingConnector` implementations; ServiceNow also refuses (`ServiceNowWorkTrackingConnector.cs:956`) alongside CSV (`:422`) and Linear (`:814`) | Blast radius of any port change is **5**, and that is why ADR-143 keeps the port unchanged |
| CA-8 | Slice 05's surface can ride the connection-validation advisory channel | slice 05 brief, ADR-127 | **Unavailable** — `ConnectionValidationResult` has no `Advisory` and no `SuccessWith`; #5612's merge removed them | ADR-145 designs its own read-only endpoint instead, and says why |
| CA-9 | Carried question (b): preserve `WriteBackResult` semantics across batching *and* the async bulk transport | handoff §(b) | **Halved** — the bulk transport is out of scope, so only the batching pressure remains | ADR-143 §3 |
| CA-10 | Two `[REF] Outcome KPIs` rows rested on disproved premises: "passes per refresh round: 1, down from N+2" and "Cloud permission required: Make bulk changes, not Administer Jira" | `[REF] Outcome KPIs` | **Corrected / void** — the first by the queue's coalescing (CA-4), the second by SPIKE-03 Q5 (bulk needs admin too) | Both rows struck through in place with the corrected figure and its evidence, rather than silently deleted |
| CA-11 | KPI "watcher emails per issue per cycle: ~1, down from ~4N" | `[REF] Outcome KPIs` | **Void** — Q9's digest window means the pre-change figure was already ~1 | Struck through in place; must not reach docs or release notes |

## Wave: DESIGN / [REF] Design Decisions (D-A series)

| ID | Decision | Verdict | Where |
|---|---|---|---|
| **D-A1** | One Jira write path. No Cloud/DC branch, no deployment discriminator, no `serverInfo` read. | **LOCKED** (user scope decision) | — |
| **D-A2** | Send `?notifyUsers=false` always; on **403 only**, retry the identical payload once without the parameter. No error-body matching. | **LOCKED** | ADR-142 |
| **D-A2b** | **The retry's *outcome* discriminates a suppression refusal from an ordinary 403.** Retry succeeds → the 403 was about suppression. Retry also fails → it was not; the credential could not have written either way. A 403 that survives the retry is a plain write failure and is **never** reported as a suppression problem. | **LOCKED** (reviewer Finding 1, 2026-08-08) | ADR-142 §3 |
| **D-A3** | Suppression outcome is a first-class fact: `WriteBackItemResult.NotificationSuppression ∈ {Suppressed, NotSuppressed, Unknown, NotApplicable}`. `Unknown` = the question arose and could not be answered (403 through the retry); `NotApplicable` = it never arose. ADO always `Suppressed`. | **LOCKED** | ADR-142 §3/§5 |
| **D-A4** | The Warning log is emitted by `WriteBackService`, once per connection per flush, naming the connection, the affected Jira projects and the remedy — aggregating **`NotSuppressed` only, never `Unknown`**, so nobody is told to grant a permission that was not the problem. Not by the connector, not per issue. Deliberately louder than the surrounding `LogDebug`. | **LOCKED** | ADR-142 §6 |
| **D-A5** | Group by work item inside each adapter; the port signature is unchanged. On **any non-403 failure**, re-send that item's fields individually. One rule, two orthogonal degradations: *403 → drop suppression, keep batch; other failure → drop batch, keep suppression.* | **LOCKED** | ADR-143 |
| **D-A6** | `GetChangedFields` indexes items once via **`ToLookup(x => x.ReferenceId)`** — not `ToDictionary`, which would throw where today's code logs a warning and takes the first match on duplicate references. | **LOCKED** | ADR-143 §5 |
| **D-A7** | `IWriteBackTriggerService` returns a plan (`IReadOnlyList<WriteBackFieldUpdate>`) and performs no I/O. A **scoped** `IWriteBackCollector` stages intents; `UpdateServiceBase.TriggerUpdate` flushes once in a `finally`. Dedup key `(connectionId, workItemId, targetFieldReference)`, last stage wins. | **LOCKED** | ADR-144 |
| **D-A7-R** | **Scoped exception to D11:** after a *successful* write, `WriteBackService` persists the value into the item's local `AdditionalFieldValues`. The existing `!=` guard then sees the truth and the cross-execution duplicate disappears **by construction**. Bounded: success only, the written value only, inbound sync still wins. D11's jitter reasoning is untouched — a re-simulated percentile genuinely differs and is still written. | **RATIFIED 2026-08-08 (user)** | ADR-144 §The residue |
| **D-A8** | No domain event is introduced for the flush. The dispatcher makes its own scope, publishes inline in registration order, and would move the ordering contract into `Program.cs`. | **LOCKED** | ADR-144 §6 |
| **D-A9** | The suppression verdict's unit is the **Jira project**, never the connection. Connection-level rollup is derived (`Quiet` / `PartiallyNoisy` / `Noisy` / `Unknown`) and always names the affected projects. | **LOCKED** | ADR-145 §1 |
| **D-A10** | Project key = substring of `ReferenceId` before the last `-` (Jira `ReferenceId = issue.Key`, `JiraWorkTrackingConnector.cs:1030`). A reference that does not match `^[A-Z][A-Z0-9_]*-\d+$` is reported as `Unknown`, never dropped and never folded into a neighbour. | **LOCKED** | ADR-145 §2 |
| **D-A11** | **No request is ever issued without project context.** A required `projectKeys` parameter does not by itself forbid an empty collection; the rule that closes the Q6 trap is the behavioural one beside it — an empty set issues **zero** requests. Together they leave no path on which `mypermissions` goes out without a `projectKey`. Enforced by two named tests, not by the type system; a wrapper type was considered and rejected as ceremony. | **LOCKED** (wording sharpened per reviewer Finding 3) | ADR-145 §3 |
| **D-A12** | The probe is a **capability interface** (`IWriteBackNotificationProbe`), implemented by Jira alone, type-tested at the call site — deliberately diverging from ADR-139's port-widening idiom on ADR-139's own criterion (variance here is per connector class, not per connection). Read methods only. | **LOCKED** | ADR-145 §4 |
| **D-A13** | The surface is a separate read-only endpoint `GET /api/v1/worktrackingsystemconnections/{id}/writeback-notification-status` (`SystemAdmin`), not a widening of `WorkTrackingSystemConnectionDto` — which Lighthouse-Clients consumes. | **LOCKED** | ADR-145 §5 |
| **D-A14** | Discovery for slice 05 is a **probe on demand** — `mypermissions?projectKey=` per project when the page asks, nothing stored, no cache, no invalidation policy. The probe (*will* it be quiet?) and the observed 403 (*was* it quiet?) are complementary and both ship. | **RATIFIED 2026-08-08 (user)** — option S2 | ADR-145 §6 |
| **D-A15** | No EF migration, no settings field, no DTO change on the connection. D3 holds. | **LOCKED** | ADR-145 §6 |
| **D-A16** | **Probe latency budget**, because a human waits on the page load: **3 s** per request, **10 s** total fan-out, at most **4** concurrent. On expiry the unanswered projects read `Unknown` while the answered ones still report, and the panel states how many of how many were checked. Copy says "could not check", never "not quiet". Bounds the large-N case rather than assuming "typically 1-5". | **LOCKED** (reviewer Finding 2, 2026-08-08) | ADR-145 §3a |

## Wave: DESIGN / [REF] Component Decomposition

| Component | Layer | Responsibility after this feature | Slice |
|---|---|---|---|
| `WriteBackTriggerService` | Application (resolver) | Resolve mappings + entities → `IReadOnlyList<WriteBackFieldUpdate>`. **Pure.** No I/O, no swallow. | 01 |
| `WriteBackCollector` *(new, scoped)* | Application (shell) | Stage intents, dedupe by `(connection, item, field)`, flush once per update execution | 01 |
| `UpdateServiceBase` | Application (host) | Owns the single flush point in the enqueued lambda's `finally` | 01 |
| `WriteBackService` | Application | Index items once, diff against stored `AdditionalFieldValues`, delegate to the connector, aggregate the per-connection suppression rollup, emit the one Warning, and persist each **successfully-written** value back into `AdditionalFieldValues` (D-A7-R) | 01, 02, 04 |
| `JiraWorkTrackingConnector` | Driven adapter | Group by item → one PUT with a multi-key `fields` object + `?notifyUsers=false`; 403 → unsuppressed retry; other failure → unbatched retry; report per-field results and suppression outcome | 02, 04 |
| `AzureDevOpsWorkTrackingConnector` | Driven adapter | Group by item → one `JsonPatchDocument` with N operations, `suppressNotifications: true` preserved; failure → unbatched retry | 02 |
| `JiraNotificationSuppressionProbe` *(new, on the Jira adapter)* | Driven adapter | `mypermissions?projectKey=` per project → per-project verdict. Read-only. | 05 |
| `WriteBackNotificationStatusService` *(new)* | Application | Derive the project set from the connection's write-back targets, call the probe, roll up | 05 |
| `WorkTrackingSystemConnectionsController` | Driving adapter (HTTP) | One new read-only action | 05 |
| `WriteBackNotificationStatus.tsx` *(new)* | Frontend | Read-only panel beside `WriteBackMappingsEditor`; renders rollup + affected projects + `Unknown` degradation | 05 |

## Wave: DESIGN / [REF] Driving Ports

| Port | Shape | Change |
|---|---|---|
| `GET /api/v1/worktrackingsystemconnections/{id}/writeback-notification-status` | `{ rollup, projects: [{ projectKey, verdict }], checkedAt }`, `[RbacGuard(SystemAdmin)]` | **NEW** (slice 05) |
| Scheduled refresh (`UpdateServiceBase` background loop) → Team / Portfolio / Forecast update | unchanged externally; gains a terminal flush | EXTENDED (slice 01) |
| Settings → Work Tracking Systems → Jira connection page | gains one read-only panel | EXTENDED (slice 05) |

No new write endpoint. D3 stands — no toggle, no remedy action.

## Wave: DESIGN / [REF] Driven Ports and Adapters

| Port | Adapter | Change |
|---|---|---|
| `IWorkTrackingConnector.WriteFieldsToWorkItems` | Jira, Azure DevOps (2 of 5 implementations act; ServiceNow, Linear, CSV refuse) | **Signature UNCHANGED.** Batching and both retries are adapter-internal |
| `IWriteBackNotificationProbe` *(new)* | `JiraWorkTrackingConnector` only; type-tested at the call site | **NEW**, read-only |
| Jira REST — `PUT /rest/api/latest/issue/{key}?notifyUsers=false` | `HttpClient` via `GetJiraRestClientAsync` | EXTENDED (query param + retry). `latest` resolves to **v2** on Cloud (Q8); left as-is, nothing here needs v3 |
| Jira REST — `GET /rest/api/latest/mypermissions?projectKey=…&permissions=ADMINISTER,ADMINISTER_PROJECTS` | same client | **NEW** |
| Azure DevOps — `WorkItemTrackingHttpClient.UpdateWorkItemAsync(patch, id, suppressNotifications: true)` | Azure DevOps SDK | EXTENDED (multi-operation patch) |
| `IRepository<Feature>` / `IWorkItemRepository` | EF Core | **EXTENDED (write)** — D-A7-R persists a successfully-written value into `AdditionalFieldValues`. No schema change: the column already exists and is already written by the inbound sync path |

**External integration — contract testing.** Jira Cloud REST is a third-party API on which two behaviours
are now load-bearing: the 403 refusal shape on `notifyUsers=false`, and the atomic rejection of a
mixed-validity `fields` object. Consumer-driven contract tests (PactNet for .NET) are **recommended to
platform-architect** for the Jira write-back and `mypermissions` interactions, so a change in either
surfaces at build time rather than as a silent return to noisy write-back. Azure DevOps is exercised
through its SDK and is covered by the existing integration suites.

## Wave: DESIGN / [REF] Technology Choices

| Choice | Verdict | Rationale |
|---|---|---|
| Existing `HttpClient` + `System.Text.Json` for the Jira calls | REUSE | Already the connector's transport; nothing here needs more |
| Existing Azure DevOps SDK `JsonPatchDocument` | REUSE | Multi-operation is native to the type already in use |
| No resilience library (Polly or similar) for the retries | REUSE nothing | Two single-shot, status-specific, non-idempotent-sensitive fallbacks. A policy engine would add a dependency to express `if (403) once` |
| No cache/memo library | REUSE nothing | D-A7's residue is closed by the database or not at all (ADR-144 R2 rejected) |
| No new persistence, no EF migration | — | D-A7-R writes to an existing column; D-A14 (probe on demand) stores nothing at all |
| NUnit 4.6 + Moq + EF InMemory (backend), Vitest + RTL (frontend) | REUSE | Project standard; `JiraWriteBackTest` / `AzureDevOpsWriteBackTest` / `WriteBackServiceTest` already exist |
| PactNet for the Jira contract tests | PROPOSED to DEVOPS | Apache-2.0, the .NET Pact implementation; only if platform-architect adopts the recommendation |

All choices are open-source or already-owned. No proprietary component introduced.

## Wave: DESIGN / [REF] Reuse Analysis (MANDATORY HARD GATE)

Every overlapping component classified. `CREATE NEW` requires evidence that no existing component can
carry the responsibility. Contract shape per principle 12: **pure** (return-only) / **bounded-change**
(declared mutation set) / **unbounded-preservation** (must return a plan, never mutate).

| Component | Verdict | Evidence | Contract shape | Universe | Assertion mechanism |
|---|---|---|---|---|---|
| `IWorkTrackingConnector` | **REUSE UNCHANGED** | Batching and retries are transport concerns; 3 of 5 implementations refuse write-back and would be re-signed for nothing (ADR-143) | — | — | Compile: no member added. ArchUnitNET: no new port member |
| `WriteBackFieldUpdate` | **REUSE UNCHANGED** | Already `(WorkItemId, TargetFieldReference, Value)` — exactly the plan value | pure value | — | `required init` members, no setters |
| `WriteBackResult` | **REUSE UNCHANGED** | Rollup is derived by `WriteBackService`, not stored on the result | pure value | — | — |
| `WriteBackItemResult` | **EXTEND** | One additive member (`NotificationSuppression`); no existing field can express "written, but audibly". **No EF migration — verified:** `WriteBackResult` and `WriteBackItemResult` live only in `Models/WriteBack/` and appear in **no `DbSet<…>`** anywhere in the backend. They are ephemeral return types on `IWorkTrackingConnector`, never persisted, so widening them cannot touch the schema | pure value | — | Shared-contract rule: grep usages + extend test builders first |
| `IWriteBackTriggerService` / `WriteBackTriggerService` | **EXTEND** | The resolution logic (`ResolveTeamUpdates:115`, `ResolvePortfolioUpdates`) is exactly what is needed; only the return type and the removal of the `writeBackService` call change | **pure** after the change | mappings × entities × clock × blackout days | ArchUnitNET: the type may not depend on `IWriteBackService` or any repository write path. Unit: resolve issues zero HTTP calls |
| `WriteBackService` | **EXTEND** | Already the once-per-connection boundary that logs the summary — the natural aggregation and Warning site (ADR-142 §6), and the only place that knows a write succeeded, which is what D-A7-R needs | **bounded-change**: outbound writes via the connector, plus `AdditionalFieldValues` for successfully-written fields only | the connection's mapped fields | Unit: one Warning per connection per flush regardless of item count. Unit: a failed write leaves `AdditionalFieldValues` untouched |
| `WriteBackService.GetChangedFields` | **EXTEND** | The `!=` guard is correct and must be preserved (AC-04.3 / AC-01.5); only the O(updates × items) scan changes | **pure** over (updates, connection, item lookup) | the diffed field set | Unit: duplicate `ReferenceId` still warns and takes the first match (the `ToLookup` trap) |
| `JiraWorkTrackingConnector` write-back methods | **EXTEND** | The custom-field reference resolution and numeric coercion (`:310-312`) must survive per field inside the batch; rewriting would re-derive them | bounded-change: the named fields on the named Jira issue | one issue per call | Gold tests per ADR-142 / ADR-143 Earned-Trust tables |
| `AzureDevOpsWorkTrackingConnector` write-back methods | **EXTEND** | `ExecuteWithThrottle` + `MaxChunkSize` chunking already exist and must be preserved around the batched call | bounded-change | one work item per call | Gold test: batched call still passes `suppressNotifications: true` (AC-04.4 / AC-05.6) |
| `UpdateServiceBase` | **EXTEND** | The enqueued lambda already wraps `Update` in try/catch; the flush belongs in its `finally` — one site, inherited by all three updaters | bounded-change | the scope's staged intents | Integration: flush throws → refresh still completes and logs (AC-04.6) |
| `PortfolioUpdater` / `ForecastUpdater` / `TeamUpdater` | **EXTEND** | Call sites change from `await Trigger…` to `collector.Stage(Resolve…)`; explicit ordering preserved | bounded-change | — | Existing `*UpdaterTest` suites |
| `UpdateQueueService` | **REUSE UNCHANGED** | Its coalescing is what caps the forecast amplifier; touching it would be re-solving a solved problem in a file with a known race history | — | — | Existing coalescing tests |
| `DomainEventDispatcher` | **NOT USED — with evidence** | Singleton creating its own scope per publish (`:11-12`, `Program.cs:1052`); a scoped accumulator would be two instances, and dispatch order would become DI registration order | — | — | ADR-144 §6 records the rejection so it is not re-proposed |
| `ConnectionValidationResult` | **NOT REUSABLE — with evidence** | `Advisory` and `SuccessWith` do not exist in the file; removed by #5612's merge. Reuse would mean rebuilding a deleted backend + frontend contract | — | — | ADR-145 Alternatives |
| `WorkTrackingSystemConnectionDto` | **NOT EXTENDED — with evidence** | Consumed by Lighthouse-Clients; widening it is a client contract change for a field one connector populates. ADR-006 precedent: separate route, stable shape | — | — | ADR-145 §5 |
| `WorkTrackingSystemConnectionsController` | **EXTEND** | One additive read-only action on the controller that already owns this resource | pure read | — | RBAC guard test |
| `WriteBackMappingsEditor.tsx` | **EXTEND** (host only) | The panel renders beside it; the editor's own responsibility is unchanged | — | — | Vitest render test |
| **`IWriteBackCollector` / `WriteBackCollector`** | **CREATE NEW** | No existing component has update-execution lifetime *and* a staging responsibility. `WriteBackService` is scoped too but is the flush executor, not the accumulator; conflating them hides the seam slices 02-05 sit on. `UpdateQueueService` is a singleton and coalesces *updates*, not *field intents* | bounded-change on `Stage` (own dictionary only); the **only** impure member is `FlushAsync` | the staged intent map | Unit: `Stage` issues zero HTTP and zero DB calls. ArchUnitNET: `Stage` returns `void`, `FlushAsync` is the sole `Task`-returning member |
| **`IWriteBackNotificationProbe` + Jira implementation** | **CREATE NEW** | No connector member answers "may this credential suppress on project X". `SupportsTransitionHistory` and `SupportsIncrementalSync` are synchronous per-connection booleans and cannot carry a per-project, remotely-determined verdict | **pure read** | outbound GET only | Interface exposes no write member (compile-enforced). Unit: every issued URI contains `projectKey=`; empty set → zero requests |
| **`WriteBackNotificationStatusService`** | **CREATE NEW** | Project-set derivation + rollup belongs above the connector and outside the write path; putting it in `WriteBackService` would couple a read surface to a write pipeline that only runs on a schedule | **pure** over (reference ids, probe verdicts) | the connection's project set | Unit: unparseable reference → `Unknown`, never dropped |
| **`WriteBackNotificationStatus.tsx`** | **CREATE NEW** | No existing component renders a connection-scoped capability panel; `ValidationAdvisory.tsx`'s channel was removed (CA-8) | pure render | — | Vitest: `Unknown` state renders "could not check", never "not quiet" |

## Wave: DESIGN / [REF] Open Questions — ALL RESOLVED 2026-08-08

| # | Question | Resolution | Recorded in |
|---|---|---|---|
| **OQ-1** | Slice 05 discovery shape — where the per-project suppression state lives and how it is discovered | **RATIFIED: S2, probe on demand.** No stored state, no migration, no cache, no invalidation. The required-`projectKeys` signature stays, so the Q6 trap remains unreachable by construction; the observed 403 keeps driving the Warning — the two are complementary, not alternatives. S1 rejected (cannot answer before the first noisy cycle, and never for a project whose values did not change); S3 rejected (reintroduces the invalidation problem S2 deletes) | ADR-145 §6 + Alternatives; D-A14 |
| **OQ-2** | May `WriteBackService` persist a successfully-written value into the local `AdditionalFieldValues`? | **RATIFIED as a scoped exception to D11.** D11's motivation was forecast jitter and is untouched — a re-simulated percentile genuinely differs and is still written. The exception is limited to persisting a value *just successfully written to the tracker*, which makes the stored copy true rather than stale, and kills the residual duplicate pass **by construction rather than by coordination** | ADR-144 §The residue; D-A7-R |
| **OQ-3** | Acceptance criteria that no longer describe a reachable state | **APPLIED.** AC-04.1 restated against the real ≈4 figure; AC-02.3 retired (D4 dropped); AC-01.2 and the DC half of the DoD retired *as release gates* and cross-referenced to the existing post-release DC checklist. Each marked retired in place with a one-line reason, in the style of the retired AC-05.3 | `slices/slice-01-*.md`, `slices/slice-04-*.md`, US-01/US-02 below, `[REF] Definition of Done` |
| **OQ-4** | ADO #5507 state | Not a design question; DISCUSS already flagged it. Recorded so DISTILL does not inherit a dangling child | — |

## Wave: DESIGN / [REF] Outcome collision check

`nwave-ai outcomes check-delta docs/feature/quiet-jira-writeback/feature-delta.md` — run by the
maintainer, 2026-08-08. **Exit code 0**, output verbatim:

> 0 outcomes checked, 0 collisions across 0 outcomes

**This is a vacuous pass, not a clean collision check.** Nothing was registered, so nothing was compared;
the exit code says the tool ran, not that this feature's outcomes are unique. A manual proxy over
`docs/` found both job ids (`job-config-admin-quiet-jira-writeback`,
`job-config-admin-know-writeback-is-quiet`) and every KPI phrasing appearing only in this feature's
artifacts plus `jobs.yaml`, `personas/config-admin.yaml` and `journeys/quiet-jira-writeback.yaml` — no
overlap with another feature. Treat that grep, not the exit code, as the evidence.

## Wave: DESIGN / [REF] Reviewer gate

`nw-solution-architect-reviewer`, run by the maintainer 2026-08-08. Verdict **conditionally approved, no
blockers**. Five findings; two closed by the maintainer, three applied here.

| # | Finding | Disposition |
|---|---|---|
| 1 | **403 conflation.** A Jira PUT also 403s for reasons unrelated to suppression (no Edit Issues, work item not visible). Treating every 403 as a suppression refusal produces a *correct* outcome (INV-Q1 held) with a *wrong diagnosis* — telling the admin to grant a permission that was never the problem, on the surface whose whole job is diagnosis | **APPLIED** — D-A2b / INV-Q3b. Discriminated on **retry outcome**, not error body: retry succeeds → `NotSuppressed`; retry also fails → `Unknown`, excluded from the Warning and from slice 05's rollup. Earned-Trust probe added: *a 403 that persists across the retry is never reported as a suppression problem* |
| 2 | **Probe SLA missing.** N `mypermissions` requests on a page load with no timeout or latency policy, and "typically 1-5 projects" quietly assumed the large-N case away | **APPLIED** — D-A16 / ADR-145 §3a. 3 s per request, 10 s total, 4 concurrent; partial verdicts kept and shown; unanswered projects read "could not check"; the panel states how many of how many were checked. Large-N is bounded by the total budget and by fanning out over distinct project keys rather than work items |
| 3 | **"Structurally unreachable" overstates a test-enforced property.** Partly right — a required parameter does not forbid an *empty* collection; the trap is closed by the empty-set-issues-nothing rule beside it | **APPLIED** — D-A11 / INV-Q5 reworded to claim exactly what holds: *no request is ever issued without project context*, two rules together, enforced by two named tests. A non-empty wrapper type was considered and rejected as ceremony for one call site |
| 4 | EF migration for `NotificationSuppression` | **Closed by maintainer, no action.** Verified: `WriteBackResult` / `WriteBackItemResult` exist only in `Models/WriteBack/` with no `DbSet<…>` anywhere in the backend — ephemeral return types on `IWorkTrackingConnector`, never persisted. Evidence now stated in the Reuse Analysis row so the next reader does not re-derive it |
| 5 | Terminology | **Closed by maintainer, no action.** Every remaining `Epic` / `Story` / `Stories` hit is literal work-tracker vocabulary (`**Stories:** US-01 (#5505)` headers, "Epic 5500"), which the project rule exempts |

## Wave: DESIGN / [REF] Contradictions found between the briefs and the code

1. **Connector count.** `[REF] Technical grounding` says "'All connectors' = 2" and names CSV and Linear as
   the refusers. There are **five** implementations; **ServiceNow refuses too**
   (`ServiceNowWorkTrackingConnector.cs:956`). This is why ADR-143 refuses to widen the port. (CA-7)
2. **Amplification arithmetic.** "N+2 passes per refresh round" ignores `UpdateQueueService`'s coalescing
   (`EnqueueUpdate` → `TryAdmit` → single `pendingReruns` entry → `TryScheduleRerun`). The real figure is
   ≈4 portfolio-level passes, capped independently of N. (CA-4)
3. **The advisory channel slice 05 assumed.** `ConnectionValidationResult` has neither `Advisory` nor
   `SuccessWith`. (CA-8)
4. **`ForecastUpdater` is not an inline third pass.** `TeamDataRefreshedForecastTriggerHandler` *enqueues*
   via `IForecastUpdater.TriggerUpdate`; it does not call the updater. The three "call sites" are four, and
   they are not all in one process moment — which is the constraint that shapes ADR-144 and bounds what
   slice 01 can deliver.
5. **Stale slice numbering persists** despite the OUTCOME block's claim that headings were fixed:
   `slice-05-*.md` is still headed `# Slice 02 -…`, and slice 04's OUT-of-scope list still points at
   "slice 02" and "slice 03" under the pre-2026-07-17 numbering. Cosmetic, but it will mislead DISTILL.
6. **Line references verified accurate** (no contradiction, recorded because the brief asked for it):
   Jira `WriteFieldsToWorkItems:263` / `UpdateItems:276` / `UpdateItem:307-350`; ADO
   `WriteFieldsToWorkItems:302` / `UpdateItemsInChunks:316` / `UpdateItems:330` / `suppressNotifications:356`;
   `WriteBackService.GetChangedFields:86`; `WriteBackTriggerService.ResolveTeamUpdates:115`.

## Wave: DESIGN / [REF] Tier-2 expansion catalog (listed, not rendered — density = lean)

| # | Expansion | Trigger to render |
|---|---|---|
| T2-01 | ATAM sensitivity/trade-off worksheet for retry-versus-pre-check (ADR-142) | Reviewer challenges the rejection of the pre-check gate |
| T2-02 | Per-connector sequence diagrams for the four degradation paths (happy, 403, invalid field, both) | DISTILL asks for step-level acceptance scaffolding |
| T2-03 | `mypermissions` request/response fixture catalogue, including the project-less over-report | Slice 05 enters DISTILL |
| T2-04 | Data Center post-release verification runbook (Q1 / Q2 / Q10) | Release ships and a DC instance becomes available |
| T2-05 | Full ISO 25010 quality-attribute scenario set for write-back | Reviewer flags a completeness gap |
| ~~T2-06~~ | ~~EF migration sketch + invalidation policy for ADR-145 option S1~~ | **RETIRED 2026-08-08** — S1 rejected; nothing is stored, so there is no migration and no invalidation policy to sketch |
| T2-07 | Threat-model delta for the new read-only endpoint (project-key enumeration by a SystemAdmin) | Security review requested |
| T2-08 | Jira throttling / concurrency parity design (the known gap at `AzureDevOpsWorkTrackingConnector.cs:320-325`) | Taken up as its own feature |

## Wave: DESIGN / [REF] Handoff

**To:** nw-acceptance-designer (DISTILL) — all three open questions are resolved and the AC changes are
already applied in the slice briefs; nothing is owed back before DISTILL starts.
**To:** nw-platform-architect (DEVOPS) — consumer-driven contract tests recommended for the Jira Cloud
REST write-back and `mypermissions` interactions (PactNet), so the two load-bearing vendor behaviours
(403-on-suppression, atomic batch rejection) fail the build rather than silently returning customers to
noisy write-back.
**Paradigm:** object-oriented, ports-and-adapters, per `CLAUDE.md`.
**Blocking on the user:** nothing. OQ-1, OQ-2 and OQ-3 were ratified and applied on 2026-08-08. All four
slices (01, 02, 04, 05) are fully designed.
