# Big Picture Event Storming — draft board seed (status quo)

What this is: a starting wall for the live session, harvested from the code and the concept docs as they stand on
2026-11-01. It describes what Lighthouse **does today**. It does not propose bounded contexts. Every sticky is there to be
moved, merged, renamed or torn down in the room.

How to read a sticky: `- [TYPE] Name — what it means — where it lives — who publishes / handles / triggers it`.
Types: **EVENT** (orange, a fact that has a type in `Models/Events/`), **IMPLICIT-EVENT** (orange with a dashed border: a
business fact that happens but has **no event type**), **COMMAND** (blue, a user intent), **POLICY** (lilac, "whenever X,
then Y"), **EXTERNAL** (pink), **READ-MODEL** (green), **ACTOR** (small yellow), **HOTSPOT** (red).

Code paths are relative to `Lighthouse.Backend/Lighthouse.Backend/` unless they start with `docs/` or `Lighthouse.Frontend/`.
"Dispatcher" means the in-process `IDomainEventDispatcher` (transport only, after commit, no outbox, no store).

---

## A. Install & platform

- [ACTOR] Instance operator / System Admin — installs, upgrades, backs up, rotates keys
- [ACTOR] Start-up of the process (a clock-less "system" actor that runs once per boot)
- [EXTERNAL] GitHub Releases (LetPeopleWork/Lighthouse) — version check and release notes, unauthenticated, cached 1 h / 30 h — `Services/Implementation/GitHubService.cs:21-45`
- [EXTERNAL] Licence file — a signed file the operator imports, verified offline against a bundled public key; no licence server — `Services/Implementation/Licensing/LicenseVerifier.cs`, `API/LicenseController.cs:35`
- [EXTERNAL] PostHog (EU collector) — opt-in usage data, forwarded by the backend — `Services/Implementation/UsageData/PostHogUsageDataPublisher.cs:36`
- [EXTERNAL] Mounted key-ring file / Redis key ring — where the secret-encryption keys live — `Services/Implementation/BackgroundServices/KeyRingFileWatcher.cs:25`
- [EXTERNAL] Database (SQLite or PostgreSQL), optional Redis for the cluster branch — `Program.cs:1605-1620`
- [IMPLICIT-EVENT] Instance Installed — install timestamp seeded on first start — `Services/Implementation/Seeding/InstallTimestampSeeder.cs` — no event type
- [IMPLICIT-EVENT] Default Terminology Seeded — Feature, Work Item, Team, Portfolio, Delivery, Cycle Time, Throughput, WIP, Blocked, SLE, Refinement… — `Services/Implementation/Seeding/TerminologySeeder.cs:29-161` — no event type
- [IMPLICIT-EVENT] Stored Forecast Inputs Forgotten At Start — every Portfolio's forecast fingerprint wiped on boot so the first round recomputes — `Services/Implementation/Seeding/ForecastInputsForgottenAtStartSeeder.cs` — no event type
- [IMPLICIT-EVENT] Orphaned Features Removed — Features no Portfolio holds any more are deleted at start-up — `Program.cs:735-740`, `Services/Implementation/OrphanedFeatureCleanupService.cs:7` — no event type
- [IMPLICIT-EVENT] Encryption Key Ring Loaded / Minted — `Services/Implementation/Encryption/EncryptionKeyRingBootstrapper.cs` — no event type
- [POLICY] Whenever the mounted key file changes, reload the key ring (every 30 s by default) — `Services/Implementation/BackgroundServices/KeyRingFileWatcher.cs:29,68-70`
- [COMMAND] Rotate Encryption Key — `API/EncryptionController.cs:106`
- [COMMAND] Re-encrypt Stored Secrets — `API/EncryptionController.cs:137`
- [IMPLICIT-EVENT] Encryption Key Rotated / Secrets Re-encrypted — no event type
- [COMMAND] Import Licence — `API/LicenseController.cs:35`
- [COMMAND] Clear Licence — `API/LicenseController.cs:69`
- [IMPLICIT-EVENT] Licence Imported / Licence Cleared — changes what every Premium gate answers; nothing is told — no event type
- [READ-MODEL] Licence status (count of Teams/Portfolios vs. allowed, Premium yes/no) — `API/LicenseController.cs:23`
- [COMMAND] Check for Update / Install Update — `API/VersionController.cs:41`, `:68`
- [IMPLICIT-EVENT] New Lighthouse Version Became Available — derived on read from GitHub — no event type
- [IMPLICIT-EVENT] Update Installed (process replaced itself) — `Services/Implementation/LighthouseReleaseService.cs` — no event type
- [COMMAND] Back Up / Restore / Clear Database — `API/DatabaseManagementController.cs:22`, `:56`, `:86`
- [IMPLICIT-EVENT] Database Backed Up / Restored / Cleared — guarded by the maintenance gate, not announced — `Services/Implementation/DatabaseManagement/DatabaseMaintenanceGate.cs` — no event type
- [COMMAND] Load Demo Scenario — `API/DemoController.cs:41`
- [IMPLICIT-EVENT] Demo Data Loaded — no event type (but the demo backfill handlers react to the refresh events that follow, see F)
- [COMMAND] Change Log Level — `API/LogsController.cs:63`
- [READ-MODEL] System info, recent problems, refresh log — `API/SystemInfoController.cs:44,96`, `API/LogsController.cs:45`
- [COMMAND] Record Usage-Data Consent / Revoke Consent / Record "asked" — `API/UsageDataController.cs:59,83,111`
- [COMMAND] Hand In Usage-Data Events (browser reports what it saw) — `API/UsageDataController.cs:133`
- [IMPLICIT-EVENT] Usage Data Consent Given / Revoked — `Services/Implementation/UsageData/UsageDataConsentService.cs` — no domain event type
- [POLICY] Whenever usage-data events are waiting and a send permit is granted, forward the batch to PostHog — `Services/Implementation/BackgroundServices/UsageDataForwardingService.cs:31-58`
- [POLICY] Once a day, prune consent rows not seen within the retention window — `Services/Implementation/BackgroundServices/UsageDataConsentPruningService.cs:26,48-49,103`
- [POLICY] On shutdown, drain in-flight work before stopping — `Health/GracefulShutdownService.cs:6`
- [HOTSPOT] A second, browser-side event vocabulary exists: `UsageDataEventName` has TeamCreated, TeamDeleted, PortfolioCreated, PortfolioDeleted, WorkTrackingSystemConnected, TeamRefreshTriggered… (`Models/UsageData/UsageDataEventName.cs:8-90`) — names the backend domain events do not have (only TeamDeleted overlaps)
- [COMMAND] Record Survey Nudge Action — `API/SurveyNudgeController.cs:28`

## B. Access (sign-in, RBAC, API keys, embed)

- [ACTOR] Signed-in user (OIDC) — or, with auth off, "everyone shares one subject"
- [ACTOR] System Admin — grants roles, maps IdP groups
- [ACTOR] CLI / MCP client (Lighthouse-Clients) holding an API key
- [ACTOR] Host page embedding Lighthouse (embed session)
- [EXTERNAL] OIDC identity provider — sign-in and group claims — `API/AuthController.cs:35`, `Services/Implementation/Auth/OidcGroupSnapshotWriter.cs:14`
- [COMMAND] Sign In / Sign Out — `API/AuthController.cs:35`, `:52`
- [IMPLICIT-EVENT] User Signed In — no event type
- [IMPLICIT-EVENT] User Profile Created (first sign-in) — `Services/Implementation/Auth/CurrentUserProfileService.cs:15` (GetOrCreateFromPrincipalAsync) — no event type
- [IMPLICIT-EVENT] IdP Group Snapshot Recorded — `Services/Implementation/Auth/OidcGroupSnapshotWriter.cs:14` — no event type
- [COMMAND] Bootstrap First System Admin — `API/AuthorizationController.cs:32`
- [COMMAND] Grant / Revoke System Admin — `API/AuthorizationController.cs:73`, `:99`
- [COMMAND] Add / Change / Remove Team Member — `API/AuthorizationController.cs:166`, `:207`
- [COMMAND] Add / Change / Remove Portfolio Member — `API/AuthorizationController.cs:242`, `:283`
- [COMMAND] Map IdP Group To Role / Change / Remove Mapping — `API/AuthorizationController.cs:348`, `:392`, `:433`
- [COMMAND] Delete User — `API/AuthorizationController.cs:125`
- [IMPLICIT-EVENT] Role Granted / Role Revoked / Group Mapped — all RBAC changes flow through `IRbacAdministrationService` and announce nothing — `Services/Implementation/Authorization/RbacAdministrationService.cs` — no event type
- [COMMAND] Create API Key / Delete API Key — `API/ApiKeyController.cs:24`, `:111`
- [IMPLICIT-EVENT] API Key Issued / Revoked — no event type
- [COMMAND] Start Embed Session / Handshake / Enter — `API/EmbedStartController.cs:53`, `API/EmbedHandshakeController.cs:24`, `API/EmbedEntryController.cs:39`
- [IMPLICIT-EVENT] Embed Session Started — `Services/Implementation/Auth/EmbedSessionTokenService.cs` — no event type
- [READ-MODEL] My authorization summary (drives every UI gate through `useRbac()`) — `API/AuthorizationController.cs:24`
- [READ-MODEL] Users, Team members, Portfolio members, group mappings — `API/AuthorizationController.cs:58,151,227,303`
- [HOTSPOT] Licence limits are checked in three different places with three different numbers: create guard `TeamLimitOverride = 2` (`API/TeamsController.cs:89`), refresh guard (`LicenseGuard(CheckTeamConstraint)`, `API/TeamController.cs:75`), and the updater silently skips a refresh above 3 Teams / 1 Portfolio (`Services/Implementation/BackgroundServices/Update/TeamUpdater.cs:34`, `PortfolioUpdater.cs:68`)

## C. Connect a tracker

- [ACTOR] Admin setting up a connection
- [EXTERNAL] Jira Cloud — `Services/Implementation/WorkTrackingConnectors/Jira/`
- [EXTERNAL] Jira Server / Data Center (same connector class, different routes chosen at run time) — `ARCHITECTURE.md:67`
- [EXTERNAL] Azure DevOps — `Services/Implementation/WorkTrackingConnectors/AzureDevOps/`
- [EXTERNAL] Linear — `Services/Implementation/WorkTrackingConnectors/Linear/`
- [EXTERNAL] ServiceNow (Teams only, no Portfolios) — `Services/Implementation/WorkTrackingConnectors/ServiceNow/`
- [EXTERNAL] CSV file — `Services/Implementation/WorkTrackingConnectors/Csv/`
- [EXTERNAL] Jira / Azure DevOps OAuth providers — `Program.cs:1528-1542`
- [COMMAND] Create Connection — `API/WorkTrackingSystemConnectionsController.cs:89`
- [COMMAND] Validate Connection (before saving) — `API/WorkTrackingSystemConnectionsController.cs:124`, port `Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs:70`
- [COMMAND] Change Connection (credentials, Additional Fields, per-owner defaults) — `API/WorkTrackingSystemConnectionController.cs:84` → `Services/Implementation/Connections/ConnectionSaveService.cs`
- [COMMAND] Delete Connection — `API/WorkTrackingSystemConnectionController.cs:150`
- [COMMAND] Connect / Disconnect OAuth — `API/OAuthController.cs:19`, `:74` (callback `:29`)
- [COMMAND] Test Connection Now — `API/ConnectionHealthController.cs:34`
- [IMPLICIT-EVENT] Connection Created — no event type (usage data has `WorkTrackingSystemConnected`)
- [IMPLICIT-EVENT] Connection Validated / Validation Failed — answered in the response only — no event type
- [IMPLICIT-EVENT] Connection Default Changed (e.g. Teams' Parent Override Field) — logged at Information, not published — `ARCHITECTURE.md:69,175` — no event type
- [EVENT] Connection Fetch Shape Changed — what this connection's Teams/Portfolios download moved — `Models/Events/ConnectionFetchShapeChanged.cs:3` — published by `Services/Implementation/Connections/ConnectionSaveService.cs:278`; handled by `ConnectionFetchShapeChangedUpdateTriggerHandler`
- [POLICY] Whenever a connection's fetch shape changed, queue an update for each affected Team, then each affected Portfolio — `Services/Implementation/BackgroundServices/Update/ConnectionFetchShapeChangedUpdateTriggerHandler.cs:12-33`
- [IMPLICIT-EVENT] OAuth Credential Connected / Refreshed / Refresh Failed — `Services/Implementation/OAuth/OAuthService.cs` — no event type
- [IMPLICIT-EVENT] Connection Health Verdict Recorded (healthy / failing) — written after every refresh and by the prober — `Services/Implementation/BackgroundServices/Update/UpdateServiceBase.cs:219-249` — no event type (ARCHITECTURE §5 lists "connection/credential health" as an event family, but no type exists)
- [POLICY] Every cadence tick, re-probe connections that have not been heard from — `Services/Implementation/BackgroundServices/ConnectionHealthProber.cs:31-47,70-74`
- [IMPLICIT-EVENT] Connection Deleted — no event type
- [IMPLICIT-EVENT] Incremental Sync Capability Detected (per connection: can it sweep?) — decided per cycle, `SupportsIncrementalSync(connection)` — `ARCHITECTURE.md:61` — no event type
- [READ-MODEL] Connection list with "N owners follow this default" — `API/WorkTrackingSystemConnectionsController.cs:57,79`
- [READ-MODEL] Connection health — `API/ConnectionHealthController.cs:28`
- [READ-MODEL] Boards wizard (pick a board, prefill a Team) — `API/WizardsController.cs:23,31`
- [READ-MODEL] Suggested work item types and states (per Team / per "project") — `API/SuggestionsController.cs:33-78`
- [HOTSPOT] The secret that unlocks a connection can become unreadable (lost key ring); every refresh then fails with a reason built from `UnreadableSecretException` — `Services/Implementation/BackgroundServices/Update/UpdateServiceBase.cs:154-172` — a "Credential Became Unreadable" fact with no name

## D. Configure Teams & Portfolios

- [ACTOR] Team lead / Team member with Team Write
- [ACTOR] Portfolio owner with Portfolio Write
- [ACTOR] System Admin (instance-wide settings: refresh cadence, Feature ordering, terminology, blackouts)
- [COMMAND] Create Team — `API/TeamsController.cs:88`
- [COMMAND] Validate Team Settings (ask the tracker whether the query works) — `API/TeamsController.cs:183`, port `IWorkTrackingConnector.cs:72`
- [COMMAND] Change Team Settings (query, states mapping, throughput window, Feature WIP, blocked rules, SLE, refinement, staleness) — `API/TeamController.cs:122`
- [COMMAND] Delete Team — `API/TeamController.cs:92`
- [COMMAND] Create Portfolio — `API/PortfoliosController.cs:79`
- [COMMAND] Validate Portfolio Settings — `API/PortfoliosController.cs:124`, port `IWorkTrackingConnector.cs:74`
- [COMMAND] Change Portfolio Settings (Feature query, default Feature size, owning Team, Parent Override Field, dependency field, write-back mapping) — `API/PortfolioController.cs:94`
- [COMMAND] Delete Portfolio — `API/PortfolioController.cs:68`
- [IMPLICIT-EVENT] Team Created / Portfolio Created — no event type (usage data has both names)
- [IMPLICIT-EVENT] Team Settings Changed / Portfolio Settings Changed — saved with an optimistic-concurrency token (409 on a lost race) — `API/TeamController.cs:153-158` — no event type
- [IMPLICIT-EVENT] Team's Work Items Discarded Because Its Query Changed — `API/TeamController.cs:142-146` (and the Portfolio twin at `API/PortfolioController.cs:143`) — no event type, and no refresh is triggered from here
- [EVENT] Team Deleted — `Models/Events/TeamDeleted.cs:3` — published **by the controller** inside a queued delete, `API/TeamController.cs:115`; handled by `TeamDeletedRefreshLogCleanupHandler`, `TeamDeletedForecastRetriggerHandler`
- [POLICY] Whenever a Team is deleted, remove its refresh-log rows — `Services/Implementation/DomainEvents/TeamDeletedRefreshLogCleanupHandler.cs:8`
- [POLICY] Whenever a Team is deleted, refresh every Portfolio it worked on or owned — `Services/Implementation/BackgroundServices/Update/TeamDeletedForecastRetriggerHandler.cs:7-18`
- [IMPLICIT-EVENT] Portfolio Deleted — refresh-log cleanup is inlined instead — `API/PortfolioController.cs:77-89` — no event type
- [COMMAND] Change Team / Feature refresh cadence — `API/AppSettingsController.cs:30`, `:70`
- [COMMAND] Change Feature Ordering (tracker rank vs. Lighthouse rank) — `API/AppSettingsController.cs:47`, `API/OptionalFeaturesController.cs:40`
- [EVENT] Feature Ordering Policy Changed — `Models/Events/FeatureOrderingPolicyChanged.cs:4` — published by `Services/Implementation/OptionalFeatures/FeatureOrderingApplier.cs:44`; handled by `FeatureOrderingPolicyChangedForecastTriggerHandler`
- [POLICY] Whenever the ordering policy changes, forecast every Portfolio immediately — `Services/Implementation/BackgroundServices/Update/FeatureOrderingPolicyChangedForecastTriggerHandler.cs:25-30`
- [COMMAND] Toggle Optional Feature — `API/OptionalFeaturesController.cs:40`
- [IMPLICIT-EVENT] Optional Feature Toggled — only Feature Ordering has an applier that publishes; every other toggle is silent — `Services/Implementation/OptionalFeatures/OptionalFeatureApplierRegistry.cs` — no event type
- [COMMAND] Rename Terminology — `API/TerminologyController.cs:26`
- [IMPLICIT-EVENT] Terminology Renamed — no event type
- [COMMAND] Add / Change / Remove Blackout Period — `API/BlackoutPeriodsController.cs:24,40,60`
- [COMMAND] Add / Change / Remove Recurring Blackout Rule — `API/RecurringBlackoutRulesController.cs:24,40,60`
- [EVENT] Blackout Configuration Changed — carries no data at all — `Models/Events/BlackoutConfigurationChanged.cs:3` — published by `Services/Implementation/BlackoutPeriodService.cs:56,74,89` and `Services/Implementation/RecurringBlackoutRuleService.cs:32,52,67`; handled by `BlackoutConfigurationChangedMetricsInvalidationHandler`
- [POLICY] Whenever blackouts change, drop the cached metrics — `Services/Implementation/DomainEvents/BlackoutConfigurationChangedMetricsInvalidationHandler.cs:13`
- [READ-MODEL] Team settings / Portfolio settings (with "where does this value come from: connection default or own") — `API/TeamController.cs:165`, `API/PortfolioController.cs:168`
- [READ-MODEL] Teams overview / Portfolios overview (catalogue shape, no histograms) — `API/TeamsController.cs:36`, `API/PortfoliosController.cs:36`
- [READ-MODEL] Portfolio size note (above 100 Features) — `API/PortfolioController.cs:196`
- [READ-MODEL] Terminology — `API/TerminologyController.cs:19`
- [HOTSPOT] Team deletion publishes an event, Portfolio deletion does not; the same reaction (clean the refresh log) is a handler for one and inline code for the other
- [HOTSPOT] A domain event published from a controller (`TeamDeleted`) rather than from the place that owns the Team

## E. Sync (scheduled and forced refresh)

- [ACTOR] Timer — Team refresh cadence and Feature refresh cadence (App Settings), with a start delay — `Services/Implementation/BackgroundServices/Update/UpdateServiceBase.cs:267-321`
- [ACTOR] User pressing Refresh (Team, all Teams, Portfolio, all Portfolios)
- [ACTOR] System Admin stopping work from the Task Manager
- [COMMAND] Refresh Team — `API/TeamController.cs:74` (forgets forecast inputs of its Portfolios first, `:86`)
- [COMMAND] Refresh All Teams — `API/TeamsController.cs:71`
- [COMMAND] Refresh Portfolio — `API/PortfolioController.cs:57`
- [COMMAND] Refresh All Portfolios — `API/PortfoliosController.cs:63`
- [COMMAND] Cancel One Task / Cancel All Tasks — `API/UpdateController.cs:90`, `:111`
- [POLICY] Every Team-refresh interval, for each Team whose last refresh is older than "refresh after", queue a Team update — `UpdateServiceBase.cs:299-313`, `TeamUpdater.cs:121-129`
- [POLICY] Every Feature-refresh interval, for each Portfolio due, queue a Portfolio update — `UpdateServiceBase.cs:299-313`, `PortfolioUpdater.cs`
- [IMPLICIT-EVENT] Update Queued / Admitted / Coalesced / Started / Finished / Cancelled — one status per `UpdateKey`, pushed to browsers over SignalR (`GlobalUpdateNotification`) — `Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs:816-822` — no domain event type
- [IMPLICIT-EVENT] Refresh Skipped Because Of Licence Limit — logged as an error only — `TeamUpdater.cs:34-37`, `PortfolioUpdater.cs:68-71` — no event type
- [IMPLICIT-EVENT] Sync Mode Decided (full vs. sweep-then-fetch-what-moved) — `Services/Implementation/WorkItems/SyncModeResolver.cs` — no event type
- [IMPLICIT-EVENT] Tracker Swept (reference id + changed-at for every record of the query) — `IWorkTrackingConnector.cs:38,55,68` — no event type
- [IMPLICIT-EVENT] Work Items Fetched For Team — `Services/Implementation/WorkItems/WorkItemService.cs:73-102`, port `IWorkTrackingConnector.cs:25,31` — no event type
- [IMPLICIT-EVENT] Work Item Created In Lighthouse (first seen) / Updated — `WorkItemService.cs:575-588` — no event type
- [IMPLICIT-EVENT] Work Item Left The Team's Query (deleted locally) — `WorkItemService.cs:277-288` — no event type
- [IMPLICIT-EVENT] Work Item Placed Under A Feature (parent decided among several link candidates) — `WorkItemService.cs:210-252`, `Services/Implementation/Parents/ParentPlacementReport.cs` — no event type
- [EVENT] Work Item Transitioned — a new state move read from the tracker's changelog (corrections raise none) — `Models/Events/WorkItemTransitioned.cs:3` — published by `WorkItemService.cs:417` → `:524`; **no handler subscribes**
- [EVENT] Work Item Blocked — `Models/Events/WorkItemBlocked.cs:3` — published by `WorkItemService.cs:421`; handled by `WorkItemBlockedTransitionCaptureHandler`
- [EVENT] Work Item Unblocked — `Models/Events/WorkItemUnblocked.cs:3` — published by `WorkItemService.cs:426`; handled by `WorkItemBlockedTransitionCloseHandler`
- [POLICY] Whenever a Work Item is blocked, open a blocked spell — `Services/Implementation/DomainEvents/WorkItemBlockedTransitionCaptureHandler.cs:12`
- [POLICY] Whenever a Work Item is unblocked, close its blocked spell — `Services/Implementation/DomainEvents/WorkItemBlockedTransitionCloseHandler.cs:11`
- [EVENT] Work Item Became Stale — crossed the Team's staleness threshold — `Models/Events/WorkItemBecameStale.cs:3` — published by `WorkItemService.cs:472` → `:533`; **no handler subscribes**
- [IMPLICIT-EVENT] Team Metrics Invalidated + Team Feature WIP Re-counted (auto-adjust: distinct direct parents of in-progress items) — `Services/Implementation/TeamMetricsService.cs:889-908` — called inline, not a handler
- [EVENT] Team Data Refreshed — the Team's Work Items are current — `Models/Events/TeamDataRefreshed.cs:3` — published by `Services/Implementation/TeamData/TeamDataService.cs:25`; handled by 7: forecast trigger, blocked-count recorder, percentiles recorder, process-behaviour recorder, two demo backfills, reconstruction memo
- [POLICY] Whenever a Team's data refreshed, queue a forecast for every Portfolio the Team works on — `Services/Implementation/BackgroundServices/Update/TeamDataRefreshedForecastTriggerHandler.cs:12-19`
- [IMPLICIT-EVENT] Write-back Staged For Team (fields Lighthouse writes back to the tracker) — `TeamUpdater.cs:57-60`, `Services/Implementation/WriteBackTriggerService.cs:25` — no event type
- [IMPLICIT-EVENT] Team Refresh Logged (mode, scanned, fetched, placement findings) — `TeamUpdater.cs:96-114` — no event type
- [IMPLICIT-EVENT] Features Fetched For Portfolio — `WorkItemService.cs:48-71,878-942`, port `IWorkTrackingConnector.cs:40,47` — no event type
- [IMPLICIT-EVENT] Feature Transitioned — Feature state transitions are captured into their own table, with no event — `WorkItemService.cs:1238-1266`, `Services/Implementation/Repositories/FeatureStateTransitionRepository.cs` — no event type
- [EVENT] Feature Blocked — `Models/Events/FeatureBlocked.cs:6` — published by `WorkItemService.cs:1203`; handled by `FeatureBlockedTransitionCaptureHandler`
- [EVENT] Feature Unblocked — `Models/Events/FeatureUnblocked.cs:4` — published by `WorkItemService.cs:1207` and, for Features that left the Portfolio, `:1228`; handled by `FeatureBlockedTransitionCloseHandler`
- [POLICY] Whenever a Feature is blocked / unblocked, open / close its blocked spell — `Services/Implementation/DomainEvents/FeatureBlockedTransitionCaptureHandler.cs:12`, `FeatureBlockedTransitionCloseHandler.cs:11`
- [IMPLICIT-EVENT] Parent Features Fetched (one level above the Feature, display only, stored as Feature rows flagged `IsParentFeature`) — `WorkItemService.cs:1302-1329`, port `IWorkTrackingConnector.cs:57,68` — no event type
- [IMPLICIT-EVENT] Feature Work Rolled Up From Work Items (remaining/total per Team, exact match on direct parent) — `WorkItemService.cs:696-724` — no event type
- [IMPLICIT-EVENT] Feature Sized By Default (no child Work Items: extrapolated with the Portfolio's default size, `IsUsingDefaultFeatureSize`) — `WorkItemService.cs:726-857`, `Models/Feature.cs:79` — no event type
- [IMPLICIT-EVENT] Dependencies Reconciled / Dependency Loop Detected — `Services/Implementation/Dependencies/DependencyReconciler.cs`, `DependencyCycleDetector.cs:25` — no event type
- [EVENT] Portfolio Features Refreshed — the Portfolio's Features and their work are current — `Models/Events/PortfolioFeaturesRefreshed.cs:3` — published by `Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs:96`; handled by 7: metrics invalidation, blocked-count recorder, percentiles recorder, process-behaviour recorder, two demo backfills, reconstruction memo
- [POLICY] Whenever a Portfolio's Features refreshed, drop its cached metrics — `Services/Implementation/DomainEvents/PortfolioFeaturesRefreshedMetricsInvalidationHandler.cs:11`
- [POLICY] Whenever either refresh event fires, forget the memo of reconstructed history for that owner — `Services/Implementation/BackgroundServices/ReconstructionMemo.cs:21-23`, `Program.cs:1414-1419`
- [IMPLICIT-EVENT] Portfolio Grew Past 100 Features — one Warning per update — `PortfolioUpdater.cs:129` — no event type
- [IMPLICIT-EVENT] Tracker Refused / Could Not Be Read — reason recorded on the refresh log — `UpdateServiceBase.cs:187-202` — no event type
- [READ-MODEL] Task Manager (what is queued, running, held) — `API/UpdateController.cs:29,47`
- [READ-MODEL] Refresh log — `API/SystemInfoController.cs:96`
- [HOTSPOT] `WorkItemTransitioned` and `WorkItemBecameStale` are published into the void — no subscriber (`Program.cs:1630-1654`); the stale signal is computed and thrown away
- [HOTSPOT] One `WorkItemService` (1,445 lines) syncs both the Team half (Work Items) and the Portfolio half (Features, parent Features, roll-up, default sizing, Feature blocked events) — `WorkItemService.cs:48-102`
- [HOTSPOT] Team refresh invalidates metrics inline (`TeamDataService.cs:23`), Portfolio refresh does it in a handler — same reaction, two shapes
- [HOTSPOT] The Portfolio refresh is a strict imperative pipeline (fetch → publish → Deliveries by rule → Deliveries by source → save → write-back → forecast trigger) — `PortfolioUpdater.cs:95-124`; only the independent reactions are events
- [HOTSPOT] Changing a Team's query discards its Work Items in the controller (`TeamController.cs:142-146`) with no event and no refresh

## F. Metrics

- [ACTOR] Team member / Portfolio owner reading charts
- [ACTOR] Browser asking for a chart that has missing days (triggers the history filler)
- [EVENT] (reuse) Team Data Refreshed / Portfolio Features Refreshed — drive the history recorders below
- [POLICY] Whenever a Team or Portfolio refreshed, record today's blocked count — `Services/Implementation/DomainEvents/BlockedCountSnapshotRecordingHandler.cs:11-13`
- [POLICY] Whenever a Team or Portfolio refreshed, record today's cycle-time / age percentiles — `Services/Implementation/DomainEvents/PercentilesOverTimeRecordingHandler.cs:10-12`
- [POLICY] Whenever a Team or Portfolio refreshed, record today's process-behaviour (XmR) values — `Services/Implementation/DomainEvents/ProcessBehaviorRecordingHandler.cs:10-12`
- [POLICY] Whenever a demo Team or Portfolio refreshed, backfill synthetic blocked history / percentiles — `Services/Implementation/DomainEvents/DemoBlockedHistoryBackfillHandler.cs:22-24`, `DemoPercentilesBackfillHandler.cs:27-29`
- [IMPLICIT-EVENT] Daily Snapshot Recorded (blocked count, percentiles, XmR) — idempotent on the day key — `ARCHITECTURE.md:192-201` — no event type
- [POLICY] Whenever a chart read finds missing days, fill them in the background from stored items, only if absent — `Services/Implementation/BackgroundServices/OverTimeHistoryFiller.cs:96,133-141`, `Services/Implementation/GapAskingPercentilesOverTimeSeriesQuery.cs`
- [IMPLICIT-EVENT] Missing History Day Reconstructed — no event type, and the row carries no marker
- [IMPLICIT-EVENT] Throughput Recalculated — never stored; derived on read from closed Work Items in the Team's window — `Services/Implementation/TeamMetricsService.cs:910-915`, `Models/Team.cs:53-63` — no event type
- [IMPLICIT-EVENT] Work Item Missed Its SLE (risk crossed 100%) — derived on read — `Services/Implementation/SleRiskCalculator.cs` — no event type
- [READ-MODEL] Team metrics: throughput, arrivals, WIP over time, WIP now, Features in progress, cycle-time / age / age-in-state percentiles, cumulative state time, cycle-time scatter, SLE risk, total age, process behaviour charts, blocked history, percentiles over time — `API/TeamMetricsController.cs:54-598`
- [READ-MODEL] Portfolio metrics: same family at Feature level plus Feature size, size percentiles, estimation vs. cycle time, predictability score — `API/PortfolioMetricsController.cs:39-605`
- [READ-MODEL] Team Feature size charts — `API/TeamFeatureSizeMetricsController.cs:33-68`
- [READ-MODEL] "Features in progress" on a Team, including "(Item not tracked by Lighthouse)" rows for parents no Portfolio holds — `Services/Implementation/InProgressFeatureResolution.cs:17-18,45-62`
- [HOTSPOT] "Snapshot day key" is named three ways across the four snapshot tables (RecordedAt vs. others) — `ARCHITECTURE.md:194-199`
- [HOTSPOT] Metrics module is a flat type-set in `Services/Implementation` (`*MetricsService`, `Percentile*`, `XmR*`), not a folder — `ARCHITECTURE.md:95`

## G. Forecasting

- [ACTOR] Forecast queue (driven only by triggers, never by its own timer: `ForecastUpdater.cs:140-148` throws on periodic refresh)
- [ACTOR] Team member running a manual forecast
- [ACTOR] Portfolio owner dragging a Feature to a new rank
- [COMMAND] Run Manual Forecast (How Many / When for a Team) — `API/ForecastController.cs:97` (POST, but Team Read, nothing stored)
- [COMMAND] Predict Work Item Creation — `API/ForecastController.cs:76`
- [COMMAND] Run Backtest — `API/ForecastController.cs:175`
- [COMMAND] Run Forecast Reality Check — `API/ForecastRealityCheckController.cs:28`
- [COMMAND] Re-forecast Portfolio Now — `API/ForecastController.cs:36`
- [COMMAND] Re-forecast All Portfolios Of A Team — `API/ForecastController.cs:56`
- [COMMAND] Move Feature (change rank) — `API/FeaturesController.cs:131` → `Services/Implementation/FeatureRankingService.cs:15-27`
- [EVENT] Feature Rank Changed — `Models/Events/FeatureRankChanged.cs:7` — published by `Services/Implementation/FeatureRankingService.cs:27`; handled by `FeatureRankChangedForecastTriggerHandler`
- [POLICY] Whenever a Feature's rank changed, forecast each of its Portfolios immediately — `Services/Implementation/BackgroundServices/Update/FeatureRankChangedForecastTriggerHandler.cs:22-29`
- [POLICY] Whenever a Portfolio refresh completes, ask for a coalesced forecast of that Portfolio — `PortfolioUpdater.cs:124` (a direct call, not an event)
- [POLICY] Hold a Portfolio's forecast while any of its Teams is still waiting to refresh — `ForecastUpdater.cs:92-133`
- [IMPLICIT-EVENT] Manual Forecast Run — answered and forgotten — `Services/Implementation/Forecast/ForecastService.cs:43,65` — no event type (usage data: `TeamManualForecastRun`)
- [IMPLICIT-EVENT] Forecast Inputs Forgotten (user-started refresh/forecast forces recompute) — `API/TeamController.cs:86`, `API/PortfolioController.cs:62`, `API/ForecastController.cs:45` — no event type
- [IMPLICIT-EVENT] Forecast Confirmed Unchanged (input fingerprint matched; nothing simulated) — `ARCHITECTURE.md:75`, `ForecastUpdater.cs:250,313-322` — no event type
- [IMPLICIT-EVENT] Portfolio Simulated (Monte Carlo over the whole forecastable set, shared clock across Teams, Features at once = Team Feature WIP) — `Services/Implementation/Forecast/ForecastService.cs:89`, `ForecastSimulation.cs`, `SimulatedRun.cs:110-115` — no event type
- [IMPLICIT-EVENT] Feature Completion Forecasts Set / Start Forecasts Set — `Models/Feature.cs:288,306` — no event type
- [IMPLICIT-EVENT] Forecast Stopped (operator or shutdown; previous forecast stands) — `ForecastUpdater.cs:173-176,208-211` — no event type
- [IMPLICIT-EVENT] Forecast Beyond The Five-Year Horizon — `ARCHITECTURE.md:73` — no event type
- [EVENT] Portfolio Forecasts Updated — the Portfolio's Feature forecasts are current (fresh or confirmed) — `Models/Events/PortfolioForecastsUpdated.cs:3` — published by `Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs:257`; handled by `DeliveryMetricSnapshotRecordingHandler`, `DeliveryForecastPublishingHandler`
- [IMPLICIT-EVENT] Forecast Write-back Staged (dates written to tracker fields at round end) — `ForecastUpdater.cs:252-306`, `UpdateServiceBase.cs:59-73`, port `IWorkTrackingConnector.cs:76` — no event type
- [IMPLICIT-EVENT] Fields Written Back To Tracker / Write-back Refused — `Services/Implementation/WriteBackService.cs` — no event type
- [READ-MODEL] Feature list per Portfolio / per Team (forecast dates, start dates, progress, default-size warning, parent column, dependencies) — `API/FeatureListController.cs:18,32`
- [READ-MODEL] Feature details and their Work Items — `API/FeaturesController.cs:69-103`
- [READ-MODEL] Manual forecast result, backtest, reality check — `API/ForecastController.cs:97,175`, `API/ForecastRealityCheckController.cs:28`
- [READ-MODEL] Forecast filter schema — `API/TeamController.cs:189`
- [HOTSPOT] "Forecast" means at least four things: a stored per-Feature completion histogram (`WhenForecast`), a stored per-Team start histogram (`StartForecast`), an unstored manual Team answer, and a replayed reality check
- [HOTSPOT] `Feature` carries Forecasting's rows (`Forecasts`, `StartForecasts`, `Models/Feature.cs:60,66`) so every Feature load drags forecast histograms along

## H. Deliveries

- [ACTOR] Portfolio owner planning a release
- [EXTERNAL] Jira Releases (a Delivery may follow a source and publish its forecast back) — `Services/Implementation/DeliverySources/`
- [COMMAND] Create Delivery (by hand, by rule [Premium], or bound to a source) — `API/DeliveriesController.cs:127`
- [COMMAND] Change Delivery (rename, reschedule, re-select, bind/unbind, publish on/off) — `API/DeliveriesController.cs:171`, `Models/Delivery.cs:111-298`
- [COMMAND] Delete Delivery — `API/DeliveriesController.cs:307`
- [COMMAND] Archive / Unarchive Delivery — `API/DeliveriesController.cs:324`, `:353`, `Models/Delivery.cs:454,472`
- [COMMAND] Validate Delivery Rule — `API/DeliveryRulesController.cs:42`
- [COMMAND] Preview Delivery Source — `API/DeliverySourcesController.cs:94`
- [COMMAND] Add / Change / Remove Delivery Note — `API/DeliveryNotesController.cs:46,80,116`
- [IMPLICIT-EVENT] Delivery Created / Renamed / Rescheduled / Deleted — no event type
- [IMPLICIT-EVENT] Delivery Archived (closure pin frozen: likelihood, dates, Features) / Unarchived — `Models/Delivery.cs:454-472`, `ARCHITECTURE.md:215` — no event type
- [IMPLICIT-EVENT] Rule-based Delivery Recomputed (Features matched by rule, incl. parent reference) — `PortfolioUpdater.cs:101`, `Services/Implementation/DeliveryRuleService.cs:58`, `Services/Implementation/WorkItemRules/FeatureFieldProvider.cs` — no event type
- [IMPLICIT-EVENT] Delivery Synced From Its Source (name, date, members from the Jira Release) / Source Unavailable — `Services/Implementation/DeliverySources/DeliverySourceSyncService.cs:14`, `Models/Delivery.cs:341,404` — no event type
- [POLICY] Whenever a Portfolio's forecasts updated, publish them to the sources its Deliveries follow (best effort) — `Services/Implementation/DomainEvents/DeliveryForecastPublishingHandler.cs:21-75`
- [IMPLICIT-EVENT] Delivery Forecast Published To Jira Release / Publish Refused — `Services/Implementation/DeliverySources/DeliveryForecastPublishingService.cs:24`, `Models/Delivery.cs:223,254` — no event type
- [POLICY] Whenever a Portfolio's forecasts updated, record today's Delivery metric snapshot (progress, scope, count, size) — `Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs:9-14`
- [IMPLICIT-EVENT] Delivery Joint Forecast Computed ("all Features done by…") — derived on read — `Models/Delivery.cs:556-575` — no event type
- [READ-MODEL] Deliveries of a Portfolio (active only; archived through a narrowed port) — `API/DeliveriesController.cs:35`
- [READ-MODEL] Delivery metrics history (burn-up) — `API/DeliveriesController.cs:104`
- [READ-MODEL] Delivery notes, sources and options, rule schema — `API/DeliveryNotesController.cs:26`, `API/DeliverySourcesController.cs:30,65`, `API/DeliveryRulesController.cs:26`
- [HOTSPOT] `Delivery` is the richest aggregate (14 behaviour methods, refuses writes when archived) and emits no events at all
- [HOTSPOT] A Delivery edited while a refresh runs: the refresh "leaves it as it now stands" — a lost-race outcome with only a log line — `PortfolioUpdater.cs:55-57`

## I. Refinement

- [ACTOR] Team member voting on sizing (signed in, or anonymous key + name when auth is off)
- [COMMAND] Cast Sizing Vote — `API/RefinementVotesController.cs:32` → `Services/Implementation/Refinement/SizingLogCommands.cs:17`
- [COMMAND] Comment On A Work Item In Refinement — `API/RefinementVotesController.cs:57` → `SizingLogCommands.cs:25`
- [COMMAND] Take Back Vote — `API/RefinementVotesController.cs:88` → `SizingLogCommands.cs:33`
- [IMPLICIT-EVENT] Sizing Vote Cast / Comment Added / Vote Taken Back — appended to the sizing log, never edited — `ARCHITECTURE.md:98` — no event type (usage data: `TeamSizingVoteCast`)
- [IMPLICIT-EVENT] Work Item Became Ready (enough Yes votes, or Ready stage rule matched) — derived on read — `Services/Implementation/Refinement/StageRuleMatcher.cs`, `RefinementViewQuery.cs` — no event type (usage data: `TeamSizingReadinessReached`)
- [IMPLICIT-EVENT] Work Item Sent To Discussion — derived on read — no event type
- [READ-MODEL] Refinement tab: Work Items in refinement, stage, votes, need band (Below / In / Above) — `API/RefinementController.cs:18`, `Services/Implementation/Refinement/RefinementNeedCalculator.cs`
- [READ-MODEL] Sizing log of one Work Item — `API/RefinementController.cs:30`

## J. Reports

- [ACTOR] Team member / Portfolio owner keeping a "Then & Now" record
- [COMMAND] Create Report (freezes "Then") — `API/TeamReportsController.cs:25`, `API/PortfolioReportsController.cs:25`
- [COMMAND] Edit / Delete Report — `API/TeamReportsController.cs:29,33`, `API/PortfolioReportsController.cs:29,33`
- [COMMAND] Create Reports In Bulk (Premium, at most 50) / Remove In Bulk — `API/ReportsController.cs:35`, `:58`
- [IMPLICIT-EVENT] Report Created ("Then" window frozen) / Deleted — `Services/Implementation/Reports/ReportCommands.cs` — no event type (usage data: `ReportCreated`, `ReportDeleted`)
- [IMPLICIT-EVENT] Report Removed With Its Owner — cascading foreign key, nobody is told — `ARCHITECTURE.md:99` — no event type
- [READ-MODEL] Report (Then vs. Now, Now computed on every read) — `API/TeamReportsController.cs:22`, `Services/Implementation/Reports/ThenAndNowTemplate.cs`
- [READ-MODEL] All reports across owners the caller may read — `API/ReportsController.cs:15`

## K. Clients (CLI / MCP)

- [ACTOR] CLI user / AI agent through the MCP server (separate Lighthouse-Clients repository, not in this repo)
- [EXTERNAL] Lighthouse-Clients (CLI + MCP) — consumes the public HTTP API as a published language
- [COMMAND] Same commands as the browser, through the API with an API key (refresh, manual forecast, votes, connection save without asking `?startUpdates=false`) — `ARCHITECTURE.md:81`, `API/ApiKeyController.cs:24`
- [HOTSPOT] The connection-save question ("start updates now?") is answered by the browser but not by CLI/MCP, which always update now — `ARCHITECTURE.md:81`

---

## Candidate pivotal events

Events where the story hands over to a new phase or where a word changes meaning. Candidates only; the session decides.

1. **Connection Validated** (implicit) — a tracker becomes something Lighthouse can talk to; "board", "project", "query" are tracker words before, Lighthouse words after.
2. **Work Items Fetched / Work Item Created In Lighthouse** (implicit) — tracker issues become Lighthouse Work Items; the connectors are the translation (state → state category, parent field → `ParentReferenceId`).
3. **Team Data Refreshed** (event) — a Team's Work Items become history that throughput, cycle time and WIP are read from; the first point where "a Team" means "a flow" rather than "a query".
4. **Feature Work Rolled Up / Feature Sized By Default** (implicit) — Work Items become remaining work on a Feature; the moment a count can turn into a guess.
5. **Portfolio Features Refreshed** (event) — the Portfolio's Features and their remaining work are ready to be forecast; also where Deliveries by rule and by source are decided.
6. **Portfolio Simulated → Portfolio Forecasts Updated** (implicit → event) — throughput becomes dates; "forecast" stops being an input and becomes a promise that is written back and published.
7. **Delivery Archived** (implicit) — a live, moving forecast becomes a frozen record.
8. **Report Created** (implicit) — live metrics become a frozen "Then" to compare with "Now".
9. **Feature Rank Changed / Feature Ordering Policy Changed** (events) — the one place a human decision, not a tracker change, moves every date.

## Hot spots (consolidated; [6455] marks those the "named levels above the Feature" change would hit)

1. [6455] **One parent, direct only.** Every Work Item and Feature carries exactly one `ParentReferenceId`; no ancestor chain — `Models/WorkItemBase.cs:24`.
2. [6455] **Roll-up is an exact match on the direct parent**, not scoped by connection or Team — `Services/Implementation/WorkItems/WorkItemService.cs:696-724`. A level above the Feature needs a second roll-up of a different kind (forecasts composed, not Work Items counted).
3. [6455] **Default Feature size extrapolation** — a Feature with no children is guessed with the Portfolio's default — `WorkItemService.cs:726-857`, `Models/Portfolio.cs:15,84`, `Models/Feature.cs:79`. Upper levels must show how much of their date is guess.
4. [6455] **Team Feature WIP counts distinct direct parents** of in-progress items — `Services/Implementation/TeamMetricsService.cs:899-908`, `Services/Implementation/InProgressFeatureResolution.cs:17-18`; parents no Portfolio holds show as "(Item not tracked by Lighthouse)" (`:45-62`).
5. [6455] **Simulation parallelism = Team Feature WIP** — `Services/Implementation/Forecast/SimulatedRun.cs:110-115`. The recommendation keeps this at the Feature level; the board should make that boundary explicit.
6. [6455] **Parent Features are Feature rows with a flag** (`IsParentFeature`, `Models/Feature.cs:77`), fetched one level up for display only — `WorkItemService.cs:1302-1329`. One type now means "forecast unit" and "label above it".
7. [6455] **Connectors' parent fetch is one level deep** — `IWorkTrackingConnector.cs:57,68` (`GetParentFeaturesDetails`, `SweepParentFeatures`); Linear alone walks sub-issues; CSV skips non-Feature rows; ServiceNow has no Portfolios.
8. [6455] **Deliveries are a cut across the Feature level**; rule-based Deliveries matching `parentreferenceid` are today's workaround for "when does this Change land" — `Services/Implementation/WorkItemRules/FeatureFieldProvider.cs`, `Models/Delivery.cs:556-575`.
9. [6455] **Terminology has one fixed key pair per concept** (feature/features, workItem/workItems, portfolio/…) and no notion of "level N" — `Services/Implementation/Seeding/TerminologySeeder.cs:29-161`.
10. [6455] **Portfolio metrics read Features' own states** (fine at any level) but there is no event that a Feature transitioned, so a level above has nothing to subscribe to — `WorkItemService.cs:1238-1266`.
11. [6455] **The Portfolio refresh is a strict imperative pipeline** (`PortfolioUpdater.cs:95-124`); adding a level adds steps (fetch level, roll up, compose forecasts) to an already long ordered chain.
12. **WorkItem vs. Feature vs. parent-Feature row all inherit `WorkItemBase`** — `Models/WorkItem.cs:4`, `Models/Feature.cs:6`. "Work item" means a Team item, any tracker record, and the base type of a Feature.
13. **Project vs. Portfolio drift** — the connector port says `GetFeaturesForProject(Portfolio project)` (`IWorkTrackingConnector.cs:40,47`), the forecast endpoint is `UpdateForecastForProject` (`API/ForecastController.cs:38`), suggestions use `/projects` (`API/SuggestionsController.cs:47,78`), the updater variable is `project` (`PortfolioUpdater.cs:74`).
14. **Generic *Service names** hide which language a class speaks: `WorkItemService` (both Team and Portfolio sync), `TeamDataService`, `AppSettingService`, `LicenseService`.
15. **Events published into the void** — `WorkItemTransitioned`, `WorkItemBecameStale` have no handler (`Program.cs:1630-1654`).
16. **Two event vocabularies** — backend domain events vs. browser usage-data names (`Models/UsageData/UsageDataEventName.cs`); lifecycle facts (Team Created, Portfolio Deleted, Connection Created) exist only in the second.
17. **Lifecycle asymmetry** — Team Deleted is an event (raised in a controller), Portfolio Deleted is inline code; Team refresh invalidates metrics inline, Portfolio refresh via a handler.
18. **Licence limits checked three ways with different numbers** — `API/TeamsController.cs:89` (2), `TeamUpdater.cs:34` (3), `PortfolioUpdater.cs:68` (1), plus `LicenseGuard` attributes; the updater's refusal is a log line.
19. **Changing a query silently discards history** — `API/TeamController.cs:142-146`, `API/PortfolioController.cs:143`.
20. **Feature carries Forecasting's rows** (`Models/Feature.cs:60,66`) — the Feature aggregate and the Forecast are not separated.
21. **Cross-module fan-out from one event** — `TeamDataRefreshed` and `PortfolioFeaturesRefreshed` each reach Forecasting, Metrics history, demo data and a memo (7 handlers each).
22. **Loop risk** — Portfolio refresh → forecast trigger; Team refresh → forecast trigger; Team deleted → Portfolio refresh → forecast. No cycle back into sync today, but the forecast "hold while a Team is waiting" logic (`ForecastUpdater.cs:92-133`) is what keeps the two refresh streams from racing.

## Cross-section flows (event → policy → command chains)

- **C → E → G:** Connection saved with new Additional Fields → [EVENT] Connection Fetch Shape Changed → [POLICY] queue each Team, then each Portfolio → Team refresh → [EVENT] Team Data Refreshed → [POLICY] queue forecast of its Portfolios → [EVENT] Portfolio Forecasts Updated.
- **E → F:** [EVENT] Team Data Refreshed / Portfolio Features Refreshed → [POLICY] record blocked count, percentiles, XmR for today; drop cached Portfolio metrics; forget the reconstruction memo.
- **E → G → H:** Portfolio refresh → [EVENT] Portfolio Features Refreshed; inline: recompute Deliveries by rule, resync Deliveries by source, stage write-back, call forecast trigger → forecast round → [EVENT] Portfolio Forecasts Updated → [POLICY] record Delivery snapshot; publish forecast to Jira Release.
- **E → G (Team side):** Timer or user → Team refresh → [EVENT] Team Data Refreshed → [POLICY] forecast every Portfolio the Team works on (held until the Portfolio's other Teams finish).
- **D → G:** Admin changes Feature ordering → [EVENT] Feature Ordering Policy Changed → [POLICY] forecast every Portfolio now.
- **G (user) → G:** User drags a Feature → [EVENT] Feature Rank Changed → [POLICY] forecast its Portfolios now.
- **D → E → G:** Team deleted → [EVENT] Team Deleted → [POLICY] clean refresh log; refresh every affected Portfolio → [EVENT] Portfolio Features Refreshed → forecast.
- **D → F:** Blackout added → [EVENT] Blackout Configuration Changed → [POLICY] drop cached Portfolio metrics (Team metrics, manual forecasts and refinement cycles read blackouts on the fly).
- **E → E:** Work Item blocked / unblocked → [POLICY] open / close a blocked spell → read by the blocked-count snapshot (F) and blocked-at-date views.
- **F (read) → F:** Chart read finds missing days → [POLICY] history filler reconstructs them in the background.
- **G → tracker:** Forecast round end → write-back flush writes dates into tracker fields (`UpdateServiceBase.cs:59-73`).

## Gaps / things I could not determine

- **Handler count.** I count **20 handler classes** and **26 (event, handler) registrations** (`Program.cs:1418-1419`, `:1630-1654`), not 23. `ReconstructionMemo` subscribes twice; the four recorders and two demo backfills subscribe to both refresh events.
- **Event count.** 14 event records in `Models/Events/`; the 6264 vocabulary table says 15 (it counts `IDomainEvent.cs`).
- **"11 background services".** Hosted: `TeamUpdater`, `PortfolioUpdater`, `OverTimeHistoryFiller`, `ConnectionHealthProber`, `UsageDataForwardingService`, `UsageDataConsentPruningService`, `KeyRingFileWatcher`, `GracefulShutdownService` (8). Not hosted but `UpdateServiceBase`-shaped: `ForecastUpdater` (trigger-only). Plus `UpdateQueueService` (the single reader) and the start-up `OrphanedFeatureCleanupService`. I did not confirm how the queue's reader loop is started.
- **Portfolio settings save.** I confirmed it discards Features when the query changes (`API/PortfolioController.cs:143`) but did not read whether it also triggers a refresh or publishes anything.
- **Write-back flush details** (`WriteBackService`, `WriteBackRound`) — not read; whether a refused write-back is recorded anywhere a user sees is open.
- **Licence verification** — no network call found in `Services/Implementation/Licensing`; I assume it is offline signature checking only. The public key file is skipped by the search tool.
- **ADR-139** (incremental sync capability per connection) and **ADR-163** (archived Deliveries excluded by a narrowed port) — taken from `ARCHITECTURE.md:61` and `:215`, not re-read in full.
- **Lighthouse-Clients (CLI/MCP)** — lives in another repository; its commands are inferred from `ARCHITECTURE.md` and the API, not from its code.
- **Frontend read models** — taken from controller endpoints; I did not map which screen reads which endpoint.
- **Who the "Portfolio owner" actor is** — RBAC has Portfolio members with roles, but whether "owner" is a role or the `OwningTeamId` (`API/TeamController.cs:98-101`) is itself a language question for the session.
