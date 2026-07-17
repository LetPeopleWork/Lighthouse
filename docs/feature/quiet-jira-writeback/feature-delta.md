<!-- markdownlint-disable MD024 -->
# Feature Delta - quiet-jira-writeback (Story 5500 "Prevent 'noise' when doing write-backs on Jira")

DISCUSS wave output. Density: lean + ask-intelligent, Tier-1 [REF] only. UX research depth: Lightweight
(brownfield, additive, one existing config surface). Premium feature. Feature-id: `quiet-jira-writeback`.
ADO: Story #5500, tag `Release Notes`, reported by Manuel and Chris.

## [REF] Summary

Lighthouse writes forecast percentiles, feature size and work-item age back into Jira fields on every
Team/Portfolio update. Each write is a normal issue edit, so Jira emails **every watcher** of the issue.
Teams get spammed, and the admin's only escape today is to switch write-back off entirely - killing a
Premium feature to stop an inbox problem.

The noise has **two independent causes**, and the original DISCUSS pass only found the first:

1. **Every write emails.** The Azure DevOps connector solves this: `AzureDevOpsWorkTrackingConnector.cs:356`
   passes `suppressNotifications: true` unconditionally. The Jira connector at
   `JiraWorkTrackingConnector.cs:325` issues a bare `PUT rest/api/latest/issue/{id}` with no suppression
   parameter. A connector parity gap, not a missing platform capability. Addressed by slices 04-06.
2. **Lighthouse writes far more often than it needs to** - amplified twice over, on *both* write-back
   connectors, for reasons that have nothing to do with Jira (see "Write amplification" below). Addressed
   by slices 01-02.

Cause 2 was discovered on 2026-07-17 and reframes the epic. It matters disproportionately because it is
**permission-free, deployment-free, and connector-agnostic** - the only lever that survives a bad SPIKE-03
verdict, and it reduces the very channels D1 wrote off as unsuppressible.

The story's own doubt ("This may not be possible at all...") resolves to: **possible, for email only** -
and separately, a large part of the volume is self-inflicted and fixable outright.

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
| D11 | **Forecast re-simulation jitter is out of scope.** Monte Carlo re-simulation can flip a percentile date by a day, making each pass a "genuine" change and defeating the `!=` guard. Raised 2026-07-17 as a possible noise floor; **user decision: ignore, do not design around it.** No hysteresis, no write-threshold, no local-value-after-write slice. (User decision, 2026-07-17.) |

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
- AC-01.2: Given a real Jira DC instance with a watcher on the issue, when write-back updates the field,
  then the watcher receives **no email**, and the change **is** present in the issue history (D1 - we
  assert the history entry exists, so nobody later "fixes" the wrong thing).
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
- AC-02.3: The required permission is deployment-correct per D4: DC -> `ADMINISTER` or
  `ADMINISTER_PROJECTS`; Cloud -> per SPIKE-03's verdict (`BULK_CHANGE` once slice 06 lands).
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
| Watcher emails per Jira write-back cycle | **0** on a credential with the required permission | SPIKE-03 + slice AC on a real Cloud and DC instance with a watcher |
| **API calls per issue per write-back pass** | **1**, down from one-per-changed-field (~6 with a full mapping set) | AC-05.1 / AC-05.2 - both connectors |
| **Write-back passes per refresh round** | **1**, down from N+2 for N teams | AC-04.1 - measured on a portfolio with >=3 teams |
| **Watcher emails per issue per cycle without any granted permission** | **~1**, down from ~4N | slices 01+02 on a stock credential; the floor D1 leaves standing |
| Jira connectors at ADO notification parity | 2/2 (Jira, ADO) | code assertion: every notification-capable connector suppresses |
| Cloud permission required for quiet write-back | "Make bulk changes", **not** `Administer Jira` | AC-03.2 on a non-admin credential |
| Admins who learn suppression is off *before* their team does | 100% of connections with an under-permissioned credential show the warning | AC-02.2 |
| Write-back disable-rate attributable to noise | 0 new reports post-release | ADO #5500 follow-ups; Manuel + Chris confirm |

## [REF] Pre-requisites

- **SPIKE-03 must complete before slice 05 and slice 06 are designed** (D7). Slices 01, 02 and 04 do not
  depend on it - 01/02 are permission-free and deployment-free, 04 is a query param.
- Test access to a real Jira **Cloud** site and a real Jira **DC** instance (7.2.0+), each with an issue
  that has a watcher, and **two** credentials per site: one with the elevated permission, one without.
  Without the non-admin credential, the silent-ignore case (the whole point of D5) cannot be observed.
- Premium licence in the dev seed (`reference_premium_license_dev_seed`) - write-back is licence-gated.

## [REF] Definition of Done

1. Jira write-back sends no watcher email on DC and Cloud with an adequately-permissioned credential.
2. Cloud path needs only "Make bulk changes" - never `Administer Jira` (AC-03.2).
3. Under-permissioned credentials produce an honest warning on the connection, never a false claim (D5).
4. ADO behaviour unchanged; Linear/CSV untouched.
5. `WriteBackResult` per-item semantics preserved across the Cloud transport swap (AC-03.4).
6. Backend `dotnet build` zero warnings + `dotnet test` green; frontend `pnpm test` + `pnpm build` clean.
7. SonarQube Cloud: no new issues.
8. Mutation kill rate >=80% on changed backend + frontend units.
9. Docs page for write-back added, stating the per-deployment permission and D1's email-only scope.

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
