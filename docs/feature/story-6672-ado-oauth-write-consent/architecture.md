# Architecture brief: story\-6672\-ado\-oauth\-write\-consent

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Requested scopes follow the connection's need. IOAuthProvider.DefaultScopes is replaced by ScopesFor(WorkItemRights needed). AdoOAuthProvider answers Read -> [499b84ac-1321-427f-aa17-267ca6975798/vso.work, offline_access] and ReadAndWrite -> [499b84ac-1321-427f-aa17-267ca6975798/vso.work_write, offline_access]; JiraOAuthProvider and StubOAuthProvider ignore the argument and keep their fixed scopes. OAuthService.BuildFlowContext passes connection.NeededWorkItemRights, so InitiateAsync and CompleteAsync both ask for what the saved connection needs; the completion log names the requested scopes instead of DefaultScopes.
```

#### D2

```text
The need is owned by the connection: WorkTrackingSystemConnection.NeededWorkItemRights is ReadAndWrite when WriteBackMappingDefinitions is not empty, else Read. Removing the last mapping changes nothing stored; the next sign-in asks for Read only.
```

#### D3

```text
Granted rights are what the identity platform answered, never what was asked. OAuthTokens gains WorkItemRights? GrantedRights. AdoOAuthProvider reads the token response scope: any space-separated entry equal to vso.work_write or ending in /vso.work_write gives ReadAndWrite; anything else, including an absent or empty scope, gives Read. Jira and Stub providers report null (they do not record rights; Jira is Bug 6686).
```

#### D4

```text
Granted rights are stored at sign-in only. OAuthService.UpsertValidCredential writes tokens.GrantedRights onto OAuthCredential.GrantedRights on both the insert and the update path. PerformRefreshAsync leaves GrantedRights unchanged: a refresh cannot widen consent, and the maintainer decision says the next sign-in stores what was granted.
```

#### D5

```text
Carriers: new enum WorkItemRights { Read, ReadAndWrite } in Models/OAuth beside OAuthCredential (kernel, like OAuthCredentialStatus). OAuthCredential owns the write-consent decision: GrantedRights (WorkItemRights?, null = signed in before rights were recorded), HasWriteConsent (GrantedRights is not Read, so null counts as write-consented per the maintainer decision) and LacksRightsFor(WorkItemRights needed) (needed is ReadAndWrite and not HasWriteConsent). No flag about the credential is computed elsewhere.
```

#### D6

```text
Persistence: one nullable int column GrantedRights on the OAuthCredentials table, mapped by EF convention (LighthouseAppContext unchanged), generated with Lighthouse.Backend/Create-Migration.ps1 for Sqlite and Postgres. Expand-only: no default, no backfill; existing rows stay null and so count as write-consented. The column is not a secret and is not encrypted by EncryptOAuthTokens.
```

#### D7

```text
Write guard lives in WriteBackService.WriteUpdates, the sole caller of IWriteBackTarget: after the changed fields are computed and before the connector is resolved, it loads the connection's OAuthCredential through IRepository<OAuthCredential>. When a credential exists and LacksRightsFor(connection.NeededWorkItemRights), it calls no connector, persists nothing, returns an empty WriteBackResult and logs one Warning per flush naming the connection id and name, the number of updates not written and the remedy (sign in again on the connection's settings to allow writing). Connectors may not reach a repository (ADR-270 R-T8), so the guard cannot sit in AzureDevOpsWorkTrackingConnector. PAT connections have no credential and are untouched.
```

#### D8

```text
Wire field: WorkTrackingSystemConnectionDto gains GrantedRights (WorkItemRights?, serialised as the string Read or ReadAndWrite) and NeedsWriteConsent (bool), both with private setters so a saved body cannot claim them. GrantedRights is set only for a connection whose AuthenticationMethodKey is AuthenticationMethodKeys.AzureDevOpsOAuth and that has a credential: ReadAndWrite when credential.HasWriteConsent, else Read. NeedsWriteConsent = credential?.LacksRightsFor(connection.NeededWorkItemRights) ?? false. Only the list endpoint passes the credential, and the edit page reads that list (EditConnection connectionQuery), so no endpoint changes.
```

#### D9

```text
UI: IWorkTrackingSystemConnection gains grantedRights?: Read | ReadAndWrite and needsWriteConsent?: boolean. ConnectionAuthSection shows one line 'Rights: Read work items' or 'Rights: Read and write work items' when grantedRights is set; the wording is a pure function in connectionFormRules.ts. A new WriteConsentAlert (MUI warning Alert, sketch copy, 'Sign in again' button) renders in ModifyConnectionSettings directly above WriteBackMappingsEditor when isEditMode and needsWriteConsent is true. Its sign-in starts the OAuth popup without disconnecting first, so reads keep working if the admin cancels; on success it reloads the connection (reloadConnection).
```

#### D10

```text
Loading and failure looks: no new fetch. The Rights line and the warning read the connection the edit page already loads through its keyed TanStack query; while it loads, ModifyConnectionSettings' LoadingAnimation covers the whole page, and when it cannot be loaded the page's existing failure handling applies and neither the line nor the warning renders, so no stale or guessed rights are shown. A sign-in in progress disables the button; a blocked, cancelled or failed popup shows the existing inline messages.
```

#### D11

```text
Context map: Tracker integration only. New glossary term 'write consent' (the connection's OAuth sign-in was granted the right to write work items) goes into ADR-270's Tracker integration glossary, and ARCHITECTURE.md section 4 row 1 gains one clause that write-back runs only with write consent; both ship with S1. No domain event: the need is derived on read from the connection's mappings and the credential's rights, nothing else reacts, and an event that marks a credential when mappings change would add state that can drift from the mappings. Rejected for that reason.
```

#### D12

```text
Lighthouse-Clients CLI/MCP: N/A. lighthouse-clients reads the connection list only for name, type, authentication method labels and options (packages/client/src/housekeepingWording.ts readConnectionListItem, declaredOptionsOf) and shows no authentication state; the new fields have private setters, so its PUT round-trip cannot bind them.
```

#### D13

```text
Formal verification: Quint (0.33.1, Apalache 0.62.4 backend) for the write-consent state machine of one connection (mappings, stored rights, sign-in, refresh, write-back), safety invariants only. TLA+/TLC not selected: no temporal or fairness claim; 'write-back works after sign-in' is a one-step property of the write-back action under granted ReadAndWrite. Agda not selected: no proof obligation beyond the finite state machine. Liveness not applicable: the only progress step, the admin signing in, is an external human action Lighthouse cannot be fair about.
```

#### D14

```text
Rejected alternatives: carrying the requested rights in the signed state token (closes a seconds-wide race between Initiate and Complete whose outcome is a loud invalid_scope failure the admin retries; not worth widening the state-token contract); a non-null column with default ReadAndWrite (would claim write consent for Jira credentials that never asked for it); guarding inside the ADO connector (connectors may not reach repositories); computing the warning in the frontend from unsaved editor mappings (Initiate reads the saved connection, so the warning must follow the saved state, and the rule would have two owners).
```

#### D15

```text
Confirmed (maintainer, option B): WriteConsentAlert renders in two places with the same component and copy: on the connection page directly above WriteBackMappingsEditor, and on the connections list (ConnectionsSettings) next to the existing ReconnectBanner for each connection whose needsWriteConsent is true, where the admin lands after saving. Sign in again on the list reloads the connection list on success.
```

### Constraints

#### K1

```text
EF migrations are expand-only (ExpandOnlyMigrationGuard) and generated only with Lighthouse.Backend/Create-Migration.ps1 for every provider; never dotnet ef migrations add.
```

#### K2

```text
Out of scope and unchanged: Jira OAuth scopes and Jira OAuth write-back (Bug 6686), PAT connections, Work Item Age and every metric.
```

#### K3

```text
Input bounds: WorkItemRights is the closed set {Read, ReadAndWrite}; the stored column additionally admits null. The token response scope is unbounded provider text; no length bound is added (none in the authority), it is only split on whitespace and compared.
```

#### K4

```text
No access token, refresh token, client secret or raw token-response body in any log; the granted scope string may be logged as today. GrantedRights is written only from the token response on the server.
```

#### K5

```text
Backend scenarios drive the application-service port under Lighthouse.Backend/Lighthouse.Backend.Tests/Acceptance/AdoOAuthWriteConsent/ with ServicePortHarness, never a web host; the Microsoft token endpoint is the only stub (HttpMessageHandler behind IHttpClientFactory). Time through a fake TimeProvider. No nWave scenario tags. The feature's Stryker config names Tests.Acceptance.AdoOAuthWriteConsent. No test filter selects a live tracker category.
```

#### K6

```text
RBAC unchanged: connection settings and write-back mappings are already SystemAdmin-gated; no new action or endpoint.
```

### Reuse

None.

### Impact

None.

### Agreement

None.

### Not applicable

None.

### Boundaries

```text
IOAuthService (InitiateAsync, CompleteAsync) and IWriteBackService.WriteFieldsToWorkItems; the connection list endpoint GET api/latest/worktrackingsystemconnections for the UI
```

#### Driven ports

```text
IOAuthProvider (AdoOAuthProvider -> Microsoft identity platform authorize and token endpoints; no new external call)
```

```text
IRepository<OAuthCredential> (EF, OAuthCredentials table)
```

```text
IWriteBackTarget (AzureDevOpsWorkTrackingConnector -> Azure DevOps work item PATCH)
```

```text
API and the update round call Application; Application (OAuthService, WriteBackService) depends on the kernel Models (WorkItemRights, OAuthCredential, WorkTrackingSystemConnection) and on ports; adapters implement IOAuthProvider and IWriteBackTarget and never reach a repository
```

#### Failures

##### B1

```text
Write-back with a credential that lacks write consent
```

Refusal

```text
No connector call, empty WriteBackResult, one Warning naming connection and remedy
```

##### B2

```text
Identity platform grants less than requested (admin or tenant declines vso.work_write)
```

Refusal

```text
GrantedRights stored as Read; NeedsWriteConsent stays true; warning stays
```

##### B3

```text
Mappings added between InitiateAsync and CompleteAsync
```

Retry

```text
Code redemption asks for more than authorized and Microsoft answers invalid_scope; existing OAuth error path, admin signs in again
```

##### B4

```text
Token response without a scope field
```

Refusal

```text
Stored as Read (least privilege); with mappings the warning shows and write-back is skipped
```

### Observations

```text
Scope query parameter of the authorization URL returned by InitiateAsync
```

space\-separated scope list

```text
what the identity platform finally granted; consent screen contents
```

```text
Whether WriteFieldsToWorkItems reached IWriteBackTarget for the connection, and the Warning it logged when it did not
```

call count and log entry

```text
whether Azure DevOps would have accepted the write
```

```text
GrantedRights and NeedsWriteConsent on the connection list DTO
```

Read \| ReadAndWrite \| absent\, and bool

```text
a credential signed in before rights were recorded from one recorded as ReadAndWrite (both show ReadAndWrite)
```

### Equality

stored rights

```text
Two credentials are equal for write consent when HasWriteConsent agrees; null and ReadAndWrite are equal under it, Read differs
```

### Constructors

WorkItemRights

enum WorkItemRights \{ Read\, ReadAndWrite \}

AdoOAuthProvider\.ScopesFor

IReadOnlyList\<string\> ScopesFor\(WorkItemRights needed\)

AdoOAuthProvider granted\-rights parse

WorkItemRights FromGrantedScope\(string\? scope\) \(private\, inside PostTokenRequestAsync\)

OAuthCredential\.LacksRightsFor

bool LacksRightsFor\(WorkItemRights needed\)

WorkTrackingSystemConnection\.NeededWorkItemRights

WorkItemRights NeededWorkItemRights \{ get\; \}

### Laws

None.

### Refuted

None.

### Claims

#### C1

```text
A stored GrantedRights value is what the identity platform granted at the last sign-in, or null when no sign-in since the Story recorded it
```

```text
A spoofed or guessed ReadAndWrite would let Lighthouse write with a token that cannot, or show the admin rights the token lacks
```

OAuthService\.UpsertValidCredential

```text
Microsoft token response scope string
```

AdoOAuthProvider granted\-rights parse \-\> OAuthTokens\.GrantedRights

OAuthCredential\.HasWriteConsent\, WriteBackService guard\, WorkTrackingSystemConnectionDto

```text
Connection list DTO GrantedRights; write-back reaches or skips the connector
```

```text
Direct database edits and backup restore can write any value (admitted, operator-owned); the DTO cannot because its setters are private; C# has no compile-time proof of the parse, only the port scenarios
```

### States

Admitted

```text
GrantedRights null with or without mappings: counts as write-consented (existing connections)
```

maintainer

Admitted

```text
GrantedRights Read with mappings: write-back skipped and logged, warning shown
```

nw\-solution\-architect

Admitted

```text
GrantedRights ReadAndWrite without mappings: token keeps write rights until the next sign-in; no write happens because nothing is staged
```

maintainer

Excluded

```text
A write reaching Azure DevOps while GrantedRights is Read
```

nw\-solution\-architect

Excluded

```text
A sign-in without mappings asking for vso.work_write
```

nw\-solution\-architect

Unknown

```text
What the identity platform returns in scope on a refresh without a scope parameter; not relied on, refresh does not touch GrantedRights
```

nw\-solution\-architect

### Models

Quint

docs\/feature\/story\-6672\-ado\-oauth\-write\-consent\/design\/write\-consent\.qnt

##### Properties

noWriteWithoutConsent

Invariant

```text
A write reaches the tracker only when the stored rights give write consent
```

WriteBackService\.WriteUpdates guard

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs

signInLeastPrivilege

Invariant

```text
A sign-in asks for ReadAndWrite only when the connection has write-back mappings
```

AdoOAuthProvider\.ScopesFor

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/AdoOAuthProvider\.cs

notRecordedNeverSkipped

Invariant

```text
A credential whose rights were not recorded is never skipped
```

OAuthCredential\.HasWriteConsent

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCredential\.cs

skipShowsWarning

Invariant

```text
Every skipped write-back coincides with NeedsWriteConsent shown on the connection
```

WorkTrackingSystemConnectionDto\.NeedsWriteConsent

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs

grantedSignInClearsWarning

Invariant

```text
A sign-in granted what it asked for leaves no write-consent warning
```

OAuthCredential\.LacksRightsFor

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCredential\.cs

##### Atoms

State

mappings

WorkTrackingSystemConnection\.WriteBackMappingDefinitions

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkTrackingSystemConnection\.cs

None.

State

rights

OAuthCredential\.GrantedRights

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCredential\.cs

None.

Action

signIn

OAuthService\.InitiateAsync \+ CompleteAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs

```text
one CompleteAsync save
```

Action

refresh

OAuthService\.PerformRefreshAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs

None.

Action

writeBack

WriteBackService\.WriteFieldsToWorkItems

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs

None.

Action

addMapping

connection save with WriteBackMappingDefinitions

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/WorkTrackingSystemConnectionsController\.cs

None.

##### Assumptions

```text
One connection; mappings collapsed to present or absent
```

```text
The identity platform grants what was requested or less, never more
```

```text
Refresh never changes the stored rights
```

##### Mutants

###### N1

```text
Guard removed: write-back writes whenever mappings exist
```

###### Kills

noWriteWithoutConsent

###### N2

```text
Not-recorded rights treated as Read
```

###### Kills

notRecordedNeverSkipped

###### N3

```text
Sign-in always requests ReadAndWrite
```

###### Kills

signInLeastPrivilege

###### N4

```text
Warning never computed
```

###### Kills

skipShowsWarning

###### N5

```text
Warning ignores stored rights
```

###### Kills

grantedSignInClearsWarning

### Closure

```text
write-consent state machine
```

Safety

Pass

DesignedNotBuilt

```text
Quint verify of allInvariants, max-steps 6, no violation; five behavioural mutants each violate their property in quint run
```

##### Evidence

docs\/feature\/story\-6672\-ado\-oauth\-write\-consent\/design\/write\-consent\.qnt

```text
read-only sign-in for a connection without mappings
```

Inhabitation

Pass

DesignedNotBuilt

```text
Admitted connection without mappings -> NeededWorkItemRights Read -> ScopesFor(Read) -> InitiateAsync URL; buildable from existing symbols plus the declared targets
```

##### Evidence

None.

```text
write consent reaching the UI and the write path
```

PublicReachability

Pass

DesignedNotBuilt

```text
Token response -> CompleteAsync -> OAuthCredential.GrantedRights -> list endpoint DTO -> ConnectionAuthSection and WriteConsentAlert; and -> WriteBackService guard -> IWriteBackTarget; acyclic, seeded by the token response
```

##### Evidence

None.

```text
write-back works after a granted sign-in
```

Liveness

Pass

DesignedNotBuilt

```text
Not applicable: the progress step is the admin's sign-in, an external human action; once granted, the next write-back writes in one step (safety model property grantedSignInClearsWarning plus noWriteWithoutConsent)
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Negates: the token response scope names vso.work_write when write was granted
```

```text
No write without recorded consent
```

```text
A granted write shown as Read; write-back skipped and logged until fixed
```

```text
A write with a token Lighthouse recorded as Read
```

```text
If Microsoft names the scope differently, write-back stays skipped; the port scenario pins the documented shape; a live check belongs to the maintainer's manual sign-in at finalize
```

maintainer

#### Y2

Stressor

```text
Negates: existing credentials all hold vso.work_write
```

```text
Existing connections keep writing
```

```text
A legacy credential that somehow lacks write fails at Azure DevOps as today
```

```text
Legacy connections skipped or shown as read-only
```

```text
None beyond today's behaviour; the next sign-in records the truth
```

maintainer

#### Y3

Bypass

```text
Direct database edit or backup restore of OAuthCredentials.GrantedRights
```

```text
Guard reads whatever is stored
```

```text
Operator sets any value
```

```text
None reachable through the app
```

```text
Operator-owned, outside the app's trust boundary
```

maintainer

#### Y4

SinglePointOfFailure

```text
WriteBackService is the only guard; a new caller of IWriteBackTarget would bypass it
```

```text
Single write path
```

```text
None
```

```text
A write path that does not pass the guard
```

```text
Architectural test pins WriteBackService as the sole caller
```

nw\-solution\-architect

#### Y5

Stressor

```text
Second set, walkthrough: Jira OAuth credential (rights null) with mappings
```

```text
Jira write-back unchanged
```

```text
Same failure as today (Bug 6686)
```

```text
Jira write-back skipped or Jira shown a Rights line
```

```text
None; null counts as consented and the DTO restricts GrantedRights to ado.oauth
```

nw\-solution\-architect

#### Y6

Stressor

```text
Second set, walkthrough: credential Disconnected or RefreshFailed with mappings and Read
```

```text
ReconnectBanner still shows
```

```text
Both the reconnect banner and the write-consent warning show; either sign-in fixes both
```

```text
A write attempt
```

```text
Two alerts at once on that rare state; accepted
```

maintainer

#### Y7

Stressor

```text
Second set, walkthrough: admin cancels the write-consent popup
```

```text
Reads keep working
```

```text
Warning stays
```

```text
Credential disconnected by the attempt
```

```text
None; the write-consent sign-in does not call disconnect
```

nw\-solution\-architect

### Architectural tests

None.

### Contract tests

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/WorkItemRights\.cs

Create

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCredential\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkTrackingSystemConnection\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/IOAuthProvider\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthTokens\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/AdoOAuthProvider\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/JiraOAuthProvider\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/StubOAuthProvider\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Migrations\/LighthouseAppContextModelSnapshot\.cs

Extend

```text
Create-Migration.ps1 adds a migration and updates this snapshot: nullable GrantedRights column
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/LighthouseAppContextModelSnapshot\.cs

Extend

```text
Same for Postgres
```

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md

Extend

```text
Glossary term write consent
```

ARCHITECTURE\.md

Extend

```text
Section 4 row 1: write-back runs only with write consent
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/AdoOAuthWriteConsent\/AdoOAuthWriteConsentTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~AdoOAuthWriteConsentTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackSinglePathArchUnitTest
```

#### Supports

None.

#### Public oracle

```text
At the port: InitiateAsync for an ado.oauth connection without mappings; CompleteAsync with a stubbed Microsoft token response whose scope names vso.work; WriteFieldsToWorkItems with one changed update on that connection after a mapping is added; the same write for a credential whose GrantedRights is null
```

```text
Authorization URL scope is .../vso.work offline_access (no vso.work_write); the read-only credential's write reaches no IWriteBackTarget and logs one Warning naming the connection; the null-rights credential's write reaches the target once; with mappings, InitiateAsync asks for .../vso.work_write offline_access and after a vso.work_write grant the write reaches the target
```

```text
Today's code: the URL always contains vso.work_write, and the read-only connection's write reaches the target
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

WorkItemRights

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/WorkItemRights\.cs\:1

CreateNew

```text
No type names work-item rights; OAuthCredentialStatus is token health, a different question
```

OAuthCredential

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Migrations\/LighthouseAppContextModelSnapshot\.cs\:898

Extend

```text
Generated by Create-Migration.ps1, never by hand
```

OAuthCredential

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/LighthouseAppContextModelSnapshot\.cs\:948

Extend

```text
Generated by Create-Migration.ps1, never by hand
```

OAuthCredential

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCredential\.cs\:3

Extend

```text
Owns the token of one connection, so it owns which rights that token holds
```

WriteBackMappingDefinitions

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkTrackingSystemConnection\.cs\:22

Extend

```text
The connection owns its mappings, so it owns the derived need
```

DefaultScopes

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/IOAuthProvider\.cs\:8

Replace

```text
A fixed list cannot express the need; ScopesFor(needed) replaces it
```

OAuthTokens

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthTokens\.cs\:3

Extend

```text
Carries what a token exchange returned; granted rights belong beside the tokens
```

UpsertValidCredential

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs\:121

Extend

```text
Single writer of a signed-in credential
```

AdoOAuthProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/AdoOAuthProvider\.cs\:10

Extend

```text
Already reads and logs the token response scope; it parses it instead of only logging
```

JiraOAuthProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/JiraOAuthProvider\.cs\:10

Extend

```text
Follows the port change only; scopes unchanged, reports no rights
```

StubOAuthProvider

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/StubOAuthProvider\.cs\:14

Extend

```text
Follows the port change only
```

WriteUpdates

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs\:145

Extend

```text
Sole caller of IWriteBackTarget; connectors may not reach repositories
```

Tracker

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:63

Extend

```text
Tracker integration glossary gains write consent
```

Tracker

ARCHITECTURE\.md\:98

Extend

```text
Section 4 row 1 gains one clause
```

##### Impact

domain type

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/WorkItemRights\.cs\:1

Changed

```text
New enum
```

entity

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/OAuth\/OAuthCredential\.cs\:3

Changed

```text
GrantedRights, HasWriteConsent, LacksRightsFor
```

entity

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/WorkTrackingSystemConnection\.cs\:6

Changed

```text
NeededWorkItemRights
```

port interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/IOAuthProvider\.cs\:4

Changed

```text
DefaultScopes replaced by ScopesFor; three providers, OAuthService and test stubs migrate
```

value type

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthTokens\.cs\:3

Changed

```text
Optional GrantedRights with default null keeps existing constructions compiling
```

application service

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs\:13

Changed

```text
Requests by need, stores granted rights at sign-in; OAuthServiceTest migrates its DefaultScopes mocks
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/AdoOAuthProvider\.cs\:10

Changed

```text
ScopesFor and scope parse
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/JiraOAuthProvider\.cs\:10

Migrated

```text
Port rename only
```

adapter

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/StubOAuthProvider\.cs\:14

Migrated

```text
Port rename only
```

application service

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/WriteBackService\.cs\:10

Changed

```text
Guard plus IRepository<OAuthCredential>; WriteBackServiceTest constructions add it; DI resolves it already
```

persisted schema

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Migrations\/LighthouseAppContextModelSnapshot\.cs\:898

Changed

```text
Additive nullable column; ExpandOnlyMigrationGuard holds
```

persisted schema

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/LighthouseAppContextModelSnapshot\.cs\:948

Changed

```text
Additive nullable column; ExpandOnlyMigrationGuard holds
```

documentation

docs\/product\/architecture\/adr\-270\-bounded\-contexts\-context\-map\-and\-module\-layout\.md\:63

Changed

```text
Glossary term
```

documentation

ARCHITECTURE\.md\:98

Changed

```text
One clause
```

##### Agreement

IOAuthProvider scopes contract

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Application\/OAuth\/OAuthService\.cs\:345

Consumer

Migrated

```text
Reads ScopesFor(connection.NeededWorkItemRights)
```

IOAuthProvider scopes contract

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/AdoOAuthProvider\.cs\:46

Producer

Migrated

```text
Answers per need
```

IOAuthProvider scopes contract

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/JiraOAuthProvider\.cs\:49

Producer

Migrated

```text
Same fixed list for any need
```

IOAuthProvider scopes contract

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/TrackerIntegration\/Adapters\/OAuth\/StubOAuthProvider\.cs\:46

Producer

Migrated

```text
Same fixed list for any need
```

##### Not applicable

contract_tests

```text
Producer and consumer of the changed contracts ship in one deployable and one commit; the Microsoft token endpoint is external and is covered by the port scenario's recorded response shape, not a contract test
```

prefactoring

```text
Each change extends one owner in place; no move makes the behaviour change easier
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L2

```text
Requested scopes are vso.work + offline_access without mappings and vso.work_write + offline_access with mappings
```

Algebraic

Adapter

AdoOAuthProvider\.ScopesFor

###### Treatment

Finite

###### Cases

```text
no mappings -> vso.work
```

```text
one mapping -> vso.work_write
```

```text
Closed set of cases; each case is one branch of the decision
```

###### Evidence

docs\/feature\/story\-6672\-ado\-oauth\-write\-consent\/design\/write\-consent\.qnt

###### L3

```text
Granted rights parse: ReadAndWrite iff a whitespace-separated scope entry equals vso.work_write or ends with /vso.work_write
```

Algebraic

Adapter

AdoOAuthProvider

###### Treatment

Finite

###### Cases

```text
resource/vso.work_write offline_access -> ReadAndWrite
```

```text
resource/vso.work offline_access -> Read
```

```text
vso.work_write -> ReadAndWrite
```

```text
resource/vso.work_write_x -> Read
```

```text
empty or absent -> Read
```

```text
Closed set of cases; each case is one branch of the decision
```

###### Evidence

None.

###### L4

```text
HasWriteConsent is false only for Read; LacksRightsFor(needed) is needed == ReadAndWrite and not HasWriteConsent
```

Invariant

Domain

OAuthCredential

###### Treatment

Finite

###### Cases

```text
null -> consent
```

```text
Read -> no consent
```

```text
ReadAndWrite -> consent
```

```text
needed Read never lacks
```

```text
Closed set of cases; each case is one branch of the decision
```

###### Evidence

docs\/feature\/story\-6672\-ado\-oauth\-write\-consent\/design\/write\-consent\.qnt

###### L5

```text
Write-back reaches IWriteBackTarget only when the credential is absent or has write consent; otherwise one Warning and nothing persisted
```

Invariant

Application

WriteBackService

###### Treatment

Finite

###### Cases

```text
no credential (PAT) -> written
```

```text
null rights -> written
```

```text
Read -> skipped and logged
```

```text
ReadAndWrite -> written
```

```text
Closed set of cases; each case is one branch of the decision
```

###### Evidence

docs\/feature\/story\-6672\-ado\-oauth\-write\-consent\/design\/write\-consent\.qnt

###### L6

```text
A refresh leaves GrantedRights unchanged
```

Invariant

Application

OAuthService\.PerformRefreshAsync

###### Treatment

Finite

###### Cases

```text
Read stays Read
```

```text
null stays null
```

```text
Closed set of cases; each case is one branch of the decision
```

###### Evidence

docs\/feature\/story\-6672\-ado\-oauth\-write\-consent\/design\/write\-consent\.qnt

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

###### T1

```text
Only WriteBackService calls IWriteBackTarget.WriteFieldsToWorkItems (implementers excluded), so the write-consent guard cannot be bypassed by a new caller
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/WriteBackSinglePathArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WriteBackSinglePathArchUnitTest
```

##### Contract tests

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs

Extend

None.

Lighthouse\.Frontend\/src\/models\/WorkTracking\/WorkTrackingSystemConnection\.ts

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/connectionFormRules\.ts

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionAuthSection\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/ReconnectBanner\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.tsx

Create

None.

Lighthouse\.Frontend\/src\/hooks\/useOAuthSignIn\.ts

Create

None.

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx

Extend

```text
Second render site of WriteConsentAlert beside ReconnectBanner
```

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Connections/WriteConsentAlert.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WorkTrackingSystemConnectionDtoTest
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Settings/Connections/ConnectionsSettings.test.tsx
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkTrackingSystemConnectionDtoTest\.cs

#### Public oracle

```text
Connection DTO for an ado.oauth connection with one mapping and a Read credential, then one with ReadAndWrite; render WriteConsentAlert for the first and click Sign in again; render the connection page and the connections list with that connection
```

```text
DTO: grantedRights Read and needsWriteConsent true, then ReadAndWrite and false; Rights wording 'Read work items' / 'Read and write work items'; the alert shows the sketch copy, the click calls initiateConnect without disconnect and opens the popup, success calls the reload; the alert appears above the write-back mappings on the connection page and next to the connection on the connections list; nothing renders in either place when needsWriteConsent is false
```

```text
Today's code: the DTO has no rights fields and no alert exists on either page
```

##### Promises

P4

P5

#### Prefactoring

```text
Extract the popup sign-in flow (initiateConnect, openOAuthPopup, inline messages for blocked, cancelled, failed) from ReconnectBanner into hook useOAuthSignIn; ReconnectBanner keeps disconnect-first and then calls it. Makes WriteConsentAlert reuse the flow without disconnecting.
```

```text
ReconnectBanner behaviour and copy
```

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/ReconnectBanner\.test\.tsx

##### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/components/Common/Connections/ReconnectBanner.test.tsx
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

RequiresReconnect

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs\:51

Extend

```text
Same pattern: a server-derived credential fact on the list DTO
```

requiresReconnect

Lighthouse\.Frontend\/src\/models\/WorkTracking\/WorkTrackingSystemConnection\.ts\:140

Extend

```text
Optional server-derived fields beside requiresReconnect
```

resolveAuthOptionHelperText

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/connectionFormRules\.ts\:1

Extend

```text
Home of the connection form's wording rules
```

ConnectionAuthSection

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionAuthSection\.tsx\:19

Extend

```text
The sketch places the Rights line in the Authentication section
```

WriteBackMappingsEditor

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:585

Extend

```text
Renders the alert directly above the existing editor
```

ReconnectBanner

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/ReconnectBanner\.tsx\:27

Extend

```text
Its popup flow moves into useOAuthSignIn; the banner keeps disconnect-first
```

WriteConsentAlert

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.tsx\:1

CreateNew

```text
No component warns about missing write consent; it reuses the Alert style and the sign-in hook
```

useOAuthSignIn

Lighthouse\.Frontend\/src\/hooks\/useOAuthSignIn\.ts\:1

CreateNew

```text
Extracted from ReconnectBanner so two alerts share one sign-in flow; useOAuthPopup stays the popup primitive
```

ReconnectBanner

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx\:216

Extend

```text
The list already renders ReconnectBanner per connection; the alert sits beside it
```

##### Impact

wire DTO

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs\:10

Changed

```text
Two additive read-only fields; lighthouse-clients ignores them
```

frontend model

Lighthouse\.Frontend\/src\/models\/WorkTracking\/WorkTrackingSystemConnection\.ts\:140

Changed

```text
Two optional fields
```

frontend rule

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/connectionFormRules\.ts\:1

Changed

```text
Rights wording function
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ConnectionAuthSection\.tsx\:19

Changed

```text
Rights line; new optional prop
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Connection\/ModifyConnectionSettings\.tsx\:70

Changed

```text
Passes rights and renders the alert
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/ReconnectBanner\.tsx\:27

UnchangedCompatible

```text
Behaviour and copy preserved through the extracted hook; ReconnectBanner.test.tsx unchanged
```

component

Lighthouse\.Frontend\/src\/components\/Common\/Connections\/WriteConsentAlert\.tsx\:1

Changed

```text
New
```

hook

Lighthouse\.Frontend\/src\/hooks\/useOAuthSignIn\.ts\:1

Changed

```text
New
```

component

Lighthouse\.Frontend\/src\/pages\/Settings\/Connections\/ConnectionsSettings\.tsx\:216

Changed

```text
Renders WriteConsentAlert per connection that needs write consent
```

##### Agreement

connection list DTO grantedRights\/needsWriteConsent

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkTrackingSystemConnectionDto\.cs\:51

Producer

Migrated

```text
Adds the fields
```

connection list DTO grantedRights\/needsWriteConsent

Lighthouse\.Frontend\/src\/models\/WorkTracking\/WorkTrackingSystemConnection\.ts\:140

Consumer

Migrated

```text
Reads them, optional
```

connection list DTO grantedRights\/needsWriteConsent

\/storage\/repos\/lighthouse\-clients\/packages\/client\/src\/housekeepingWording\.ts\:130

Consumer

UnchangedCompatible

```text
Ignores unknown fields
```

##### Not applicable

architectural_tests

```text
No dependency or owner law the UI change can violate beyond the existing useRbac and ApiServiceContext rules
```

contract_tests

```text
Backend DTO and frontend consumer ship in one deployable and one commit
```

##### Boundaries

None.

##### Observations

None.

##### Equality

None.

##### Constructors

None.

##### Laws

###### L1

```text
DTO: GrantedRights set only for ado.oauth with a credential (HasWriteConsent ? ReadAndWrite : Read); NeedsWriteConsent = credential lacks rights for the connection's need
```

Invariant

Application

WorkTrackingSystemConnectionDto

###### Treatment

Finite

###### Cases

```text
ado.oauth, null rights -> ReadAndWrite, false
```

```text
ado.oauth, Read, mapping -> Read, true
```

```text
ado.oauth, Read, no mapping -> Read, false
```

```text
jira.oauth -> absent, false
```

```text
PAT -> absent, false
```

```text
Closed set of cases; each case is one branch of the decision
```

###### Evidence

docs\/feature\/story\-6672\-ado\-oauth\-write\-consent\/design\/write\-consent\.qnt

##### Refuted

None.

##### Claims

None.

##### States

None.

##### Models

None.

##### Closure

None.

##### Risks

None.

##### Architectural tests

None.

##### Contract tests

None.

## ADRs

None.
