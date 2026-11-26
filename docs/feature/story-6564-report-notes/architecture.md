# Architecture brief: story\-6564\-report\-notes

## Paradigm

object_oriented

## Shared records

### Decisions

#### D1

```text
Placement. The note entity ReportNote sits in Models/Reports beside Report, not in Modules/Reports/Domain, because LighthouseAppContext (Data) must map it and the Reports rules R-RP2 and R-RP3 let only API, the composition root and Reports Application reach Domain. It is a kernel persistence carrier of the Report aggregate, moves with Report when S04b ratchets Models down, and adds one type to Models/Reports. The rest is in Modules/Reports/Application: ports IReportNoteCommands, IReportNoteQueries and the driven port IReportNoteStore, use cases ReportNoteCommands and ReportNoteQueries, in Modules/Reports/Domain the caller choice type ReportNoteCaller (Anonymous, Signed, Unresolved) and the record ReportNoteRead, which are no port files and reach only the shared Models, the base library and the Domain. IReportNoteStore is implemented by the existing ReportRepository (one aggregate root, Report with its notes), so R-RP4 gains no outside type.
```

#### D2

```text
Aggregate and owners. A note is a child of the Report aggregate and is reached only through its owner and report id (a note reached through another report or owner finds nothing). Each decision has one owner: text normalisation, length bound, creation and revision sit on ReportNote (Write, Revise); who may change a note sits on the new kernel value NoteSignature, shared by ReportNote and DeliveryNote so the Delivery rule is decided once; refusals and ordering sit in ReportNoteCommands and ReportNoteQueries; write access on the owner stays a route guard (RbacGuard TeamWrite or PortfolioWrite), read access a TeamRead or PortfolioRead guard on the controller.
```

#### D3

```text
Maximum note length: 2000 characters after trimming, refused with code note-text-too-long. Reason: the live report lists every note without paging, so an unbounded text can swamp the report; 2000 holds a paragraph of context. Deviation from Delivery notes, which have none. The constant is ReportNote.LongestText; the mapping in LighthouseAppContext uses HasMaxLength(ReportNote.LongestText) on the text column, so the AddReportNotes migration creates the column bounded to 2000 and the database refuses a longer direct write. The number of notes per report is not capped (residue, owner maintainer).
```

#### D4

```text
Concurrency: confirmed, the last save wins, as for Delivery notes. A note carries no concurrency token and a note change never touches the Report concurrency token, so adding or editing a note cannot conflict with an edit of the report. A change or delete of a note that another writer deleted meanwhile is translated in ReportRepository (DbUpdateConcurrencyException becomes a false save) into the port outcome NotFound, so no EF type reaches Application (R-RP5).
```

#### D5

```text
Deleting a Team or Portfolio: confirmed from the code. Reports carry cascade foreign keys to Teams and Portfolios (LighthouseAppContext ConfigureReports) and single and bulk report removal both end in IReportWriter.Remove plus one save (ReportCommands.Delete, ReportBulkRemover). ReportNotes gets a cascade foreign key to Reports, so notes follow in all three paths with no code of their own. The author foreign key to UserProfiles is SetNull, as for Delivery notes, so removing a person keeps the note and its captured name.
```

#### D6

```text
Author lookup through Access. Reports Application must not reach Access (R-RP5, R-A4), so the API adapter resolves the caller and hands Reports a value ReportNoteCaller, a choice of three: Anonymous (authentication off: the note is unsigned), Signed(profileId, displayName), Unresolved (authentication on but no profile resolves: no stable subject claim, or an API key with no profile row; or the authentication mode is not usable). The controller learns the mode from IAuthModeResolver.Resolve().Mode, the application's authentication mode (not the RBAC switch IRbacAdministrationService.IsRbacEnforcedAsync, which can be off while authentication is on, and never from a null profile). Mapping: Disabled gives Anonymous; Enabled gives Signed when ICurrentUserProfileService resolves a profile and Unresolved when it answers null; Misconfigured gives Unresolved, so a note is refused rather than saved unsigned when the mode is unsure; Blocked gives Unresolved as well, though BlockedModeFilter already answers 403 before any controller action runs on these routes. ReportNoteCommands refuses Add, Edit and Delete for Unresolved with code note-author-unknown (403), so an unsigned note exists only when authentication is off. Delivery notes keep their behaviour for this case (a pre-existing gap, reported to the maintainer, outside this Request). No edge from Reports to Access is added; the story 6382 contracts-only rule is unaffected because API is not a context and ReportNote has no navigation to UserProfile. The author name is the name at write time.
```

#### D7

```text
Time stays out of Application. ReportNoteCommands takes the instant as an argument and the read record carries instants; the controller holds ILighthouseClock, passes now and reduces CreatedAt and LastEditedAt to the instance-zone day in the DTO, as DeliveryNoteDto does. Reason: a clock in the new Application types would add listed edges to R-RP6, whose list may only shrink.
```

#### D8

```text
Wire shape. Separate routes api/latest/teams/{teamId}/reports/{reportId}/notes and api/latest/portfolios/{portfolioId}/reports/{reportId}/notes (GET list newest first, POST, PUT {noteId}, DELETE {noteId}), two controllers over one ReportNotesControllerBase, mirroring TeamReportsController and PortfolioReportsController. ReportRead and the report routes the CLI and MCP clients read stay unchanged. The note DTO has id, text, createdAt, createdOn, lastEditedAt, lastEditedOn, authorDisplayName and canModify (decided on the server). A refusal is a ProblemDetails with a code: note-text-missing and note-text-too-long are 400, note-not-yours and note-author-unknown are 403.
```

#### D9

```text
Frontend. A Reports-owned component ReportNotes in pages/Common/Reports is rendered by ReportView beside ReportPanels, never inside ReportPanels or anything the print bundle imports, so print and PDF stay free of notes by construction; the print enforcement test gets a never-reaches rule for ReportNotes.tsx and useReportNotes.ts only (models/Reports/ReportNote.ts is a shared data type that print already reaches through the shared service interface and renders nothing). It is not extracted from DeliveryNotesPanel (FE2 will move that file); layout and copy are reused by value. Notes are read with a keyed TanStack query keyed by owner and report id, writes invalidate it. The add control shows when useMayEditReports(owner).isEditor; the change and delete controls show per note from canModify. The exact placement is decided at the UI sketch at the start of DELIVER.
```

#### D10

```text
Loading look and failure look of the notes area. Loading: the notes area is wrapped in WidgetBody with status from queryStatusOf, so it shows a lone spinner with the message Loading notes when it has nothing yet and dims the earlier list under a small spinner when it refetches; never an empty area that reads as no notes. Failure: WidgetBody shows the could-not-load message Could not load the notes with a retry, and the add control stays hidden while the list is unknown, so a note is never added blind. The report itself keeps its own status; a notes failure does not hide the numbers. A save in flight disables the Save control; a failed save keeps the typed text and shows the refusal under the field. The acceptance designer pins both states, and E2E waits for data-widget-status ready.
```

#### D11

```text
ADR-270 changes for the host to apply at finalize: add the glossary term Report note to Reports (a short, dated, attributed text a person writes on one report to give its numbers context); change the Reports row aggregate list to Reports and their notes; state that notes go with their report and that no domain event is published or consumed; add ReportNote and NoteSignature to the Models kernel list with the reason. Membership lists to update in S1: ReportsMembership.TypesThatStay gains ReportNote; ReportsMembership.MovingTypes gains the new types: IReportNoteCommands, IReportNoteQueries, IReportNoteStore, ReportNoteCommands and ReportNoteQueries with layer Application, ReportNoteCaller and ReportNoteRead with layer Domain; ReportsMembership.TypesThatStay also gains ReportNoteDto and ReportNoteRequest; ReportsModuleBoundariesArchUnitTest TypesByLayer gains the same types under the Application and Domain keys. NoteSignature is a Models kernel type outside Reports and is on no Reports list. The R-RP1 baseline count (36 types, Application 25) rises by the new Application types and the host updates the ADR table; R-RP4 and R-RP6 baselines stay unchanged because no new outside caller and no new listed outbound edge arise, to be confirmed by ReportsModuleBoundariesArchUnitTest. ARCHITECTURE.md section 4 Reports text gets the same sentence.
```

#### D12

```text
Checker per claim. Quint 0.33.1 (model docs/feature/story-6564-report-notes/design/reportNoteModifyRule.qnt, receipt docs/feature/story-6564-report-notes/design/reportNoteModifyRule.receipt.txt) gives bounded checking of the safety law that a signed note is changed only by its signer over authentication switching on and off with two users and three note ids: a simulation of 2000 samples of 12 steps and a bounded verify of 8 steps found no violation, and a mutant that always answers true violates the invariant. That is bounded evidence, not a proof; the model does not generate a caller with authentication on and no profile, which is a separate rule pinned by native example tests (note-author-unknown). Cascade delete, the day reduction, ordering, the text rule and the print exclusion name no shared mutable protocol or crash window, so they get No model: single transition per request, state held by one database transaction, native example and property tests cover every transition. TLA+/TLC and Agda are not used: no liveness claim and no proof over types. Liveness has no claim: a note request is one synchronous transaction with no waiting step or background progress.
```

#### D13

```text
Cross-layer algebra. Domain (ReportNote, NoteSignature): states Unsigned(text, createdAt) and Signed(profileId, name, text, createdAt), optionally edited; operations Write, Revise, MayBeChangedBy; observations text, days, author name, canModify; equality by those observations. Application: ReportNoteCommands and ReportNoteQueries answer ReportOutcome Done, NotFound or Refused(code), never null for a domain outcome. Adapter: ReportRepository and the two controllers translate DbUpdateConcurrencyException to NotFound, outcomes to 200, 201, 204, 400, 403, 404, and the route guard refuses a caller without read or write access before any use case runs. Infrastructure: table ReportNotes with cascade to Reports and SetNull to UserProfiles; a failed save leaves prior notes intact.
```

### Constraints

#### K1

```text
Authority bounds carried: note text is plain text rendered as characters with line breaks kept; no markup is interpreted. Authority names no length bound; the design adds 2000 characters after trim as a labelled deviation. Authority names no bound on the number of notes; none is added. A text empty or only blanks is refused and nothing is saved (P4). The author name is the name when the note was written. The day shown is the instance-zone day of the instant. Newest first means ordered by CreatedAt then Id, both descending.
```

#### K2

```text
Rules that bind the code: EF migration AddReportNotes is generated for both providers with the CreateMigration script, expand-only and additive (a new table). HistoricalSchemaPatch needs no entry because it patches columns on tables the migration fixtures seed and ReportNotes is a new table none of them seeds. New backend acceptance scenarios drive the application port with no web host except one named wire contract. Controllers use constructor injection and declare access with RbacGuard so EveryActionDeclaresItsAccessArchitectureTest passes. No new permission, no new role, no CLI or MCP change, no usage-data event in DESIGN (DEVOPS decides).
```

### Reuse

ReportRepository

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/ReportRepository\.cs\:8

Extend

```text
It already stores the Report aggregate and is the one outside type R-RP4 lists; implementing IReportNoteStore there keeps one repository per aggregate root and adds no listed caller. A separate note repository would be a new outside type on Reports Application.
```

MayModify

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs\:168

Extend

```text
The per-note author rule P10 and P12 copy. The same business fact must not be decided twice, so it moves unchanged into the kernel value NoteSignature and both controllers and entities ask it.
```

DeliveryNoteDto

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/DeliveryNoteDto\.cs\:5

CreateNew

```text
Report notes need a DTO of the same shape for the reader, but a shared DTO would tie the Delivery wire to the Reports wire and carry DeliveryId; a Reports DTO ReportNoteDto is separate.
```

ReportOwnerWriteAccess

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportOwnerWriteAccess\.cs\:12

Reuse

```text
Pattern reused, not called: the API band bridges Access for Reports. Note writes use the route guard RbacGuard TeamWrite and PortfolioWrite as the report write routes do, so no new write-access port is needed.
```

WidgetBody

Lighthouse\.Frontend\/src\/pages\/Common\/MetricsView\/WidgetShell\.tsx\:221

Reuse

```text
Gives the notes area its loading, dimmed refetch and could-not-load look.
```

useMayEditReports

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/useMayEditReports\.ts\:22

Reuse

```text
The report view already gates its write controls on it; the add control of the notes uses the same gate.
```

DeliveryNotesPanel

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/DeliveryNotesPanel\.tsx\:20

CreateNew

```text
A Reports-owned component is created rather than extracting a shared one: DeliveryNotesPanel is archived-aware, terminology-aware and sits in a file FE2 will move; the two share layout and copy by value only.
```

### Impact

persistence

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs\:101

Changed

```text
DbSet ReportNotes and its mapping: cascade to Reports, SetNull to UserProfiles, an index on ReportId and HasMaxLength(ReportNote.LongestText), 2000, on the text column.
```

migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Migrations\/LighthouseAppContextModelSnapshot\.cs\:1

Changed

```text
The snapshot gains ReportNotes with its text column bounded to 2000, and AddReportNotes is generated with CreateMigration.
```

migration

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/LighthouseAppContextModelSnapshot\.cs\:1

Changed

```text
Same migration for Postgres, expand-only, text column bounded to 2000.
```

composition\-root

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs\:1327

Changed

```text
RegisterReports registers IReportNoteStore over ReportRepository, IReportNoteQueries and IReportNoteCommands.
```

public\-interface

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportCommands\.cs\:8

UnchangedCompatible

```text
Report commands, queries, bulk removal and the report DTOs are untouched; notes are separate ports and routes.
```

client\-consumer

Lighthouse\.Frontend\/src\/services\/Api\/ReportService\.ts\:26

Changed

```text
IReportService and ReportService gain getNotes, addNote, updateNote and deleteNote; every mock of IReportService (tests/MockApiServiceProvider.ts) gets the new methods.
```

external\-client

Lighthouse\-Clients\/packages\/client\/src\/reportReads\.ts\:1

UnchangedCompatible

```text
CLI and MCP read the report routes whose DTOs do not change; notes stay out of them by decision.
```

print\-bundle

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/print\/ReportPrintContent\.tsx\:1

UnchangedCompatible

```text
The print bundle draws ThenAndNowSummary and ReportPanels only; notes are never imported by it.
```

### Agreement

Report note author rule\: a note nobody signed may be changed by any writer\, a signed note only by its signer\, whatever the authentication setting is now

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs\:202

Producer

Migrated

```text
The rule moves into NoteSignature and the controller asks it.
```

Report note author rule\: a note nobody signed may be changed by any writer\, a signed note only by its signer\, whatever the authentication setting is now

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportNote\.cs\:1

Consumer

Migrated

```text
ReportNote.MayBeChangedBy delegates to NoteSignature, so canModify and the refusal come from one decision.
```

Report note author rule\: a note nobody signed may be changed by any writer\, a signed note only by its signer\, whatever the authentication setting is now

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/DeliveryNote\.cs\:4

Consumer

Migrated

```text
DeliveryNote gains MayBeChangedBy delegating to NoteSignature.
```

Report note wire\: GET\, POST\, PUT and DELETE on reports\/\{reportId\}\/notes of a Team or Portfolio

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportNotesControllerBase\.cs\:1

Producer

Migrated

```text
New routes and ReportNoteDto, both owner kinds through one base.
```

Report note wire\: GET\, POST\, PUT and DELETE on reports\/\{reportId\}\/notes of a Team or Portfolio

Lighthouse\.Frontend\/src\/services\/Api\/ReportService\.ts\:26

Consumer

Migrated

```text
ReportService reads the DTO through a zod schema and surfaces the refusal code as an ApiError.
```

Report note wire\: GET\, POST\, PUT and DELETE on reports\/\{reportId\}\/notes of a Team or Portfolio

Lighthouse\-Clients\/packages\/client\/src\/reportReads\.ts\:1

Consumer

UnchangedCompatible

```text
The clients do not call the note routes now.
```

ReportNotes table\, additive\, cascade from Reports

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/ReportRepository\.cs\:8

Consumer

Migrated

```text
The only reader and writer of the table, through IReportNoteStore.
```

### Not applicable

None.

### Boundaries

```text
IReportNoteQueries and IReportNoteCommands (Modules/Reports/Application), reached over HTTP by TeamReportNotesController and PortfolioReportNotesController (GET, POST, PUT, DELETE); frontend driving port ReportNotes through IReportService
```

#### Driven ports

```text
IReportNoteStore (implemented by ReportRepository over LighthouseAppContext)
```

```text
API and composition root -> Reports Application ports -> Models/Reports kernel (ReportNote, NoteSignature, ReportOutcome); ReportRepository (Services) -> Reports Application; Reports Application reaches no EF Core, Data, API, adapter, Access or platform clock type; API reaches Access Application for the caller profile and the clock for the day
```

#### Failures

##### B1

```text
A note has empty or blank text, or exceeds the length bound
```

Refusal

```text
400 with code note-text-missing or note-text-too-long and nothing saved
```

##### B2

```text
The caller may not change this note
```

Refusal

```text
403 with code note-not-yours and the note unchanged
```

##### B3

```text
The report, owner or note does not exist or was deleted meanwhile
```

Refusal

```text
404
```

##### B4

```text
The notes cannot be read by the browser
```

Indeterminate

```text
The notes area shows Could not load the notes with a retry and the add control hidden
```

##### B5

```text
Authentication is on and no profile resolves for the caller (no stable subject claim, or an API key with no profile row)
```

Refusal

```text
403 with code note-author-unknown on add, change and delete; nothing saved or changed
```

### Observations

None.

### Equality

None.

### Constructors

None.

### Laws

None.

### Refuted

None.

### Claims

None.

### States

Admitted

```text
A note on a report, unsigned with no author name (written with authentication off)
```

ReportNote

Admitted

```text
A note signed by a profile id with the name at write time, optionally edited with the instant of the edit
```

ReportNote

Excluded

```text
A note with empty or blank text, or text over 2000 characters after trim: ReportNote.Write and Revise refuse it, so none is stored
```

ReportNote

Excluded

```text
A note without a report, or a note under a different owner than the route names: the store finds nothing and the foreign key refuses an orphan
```

ReportRepository

Unknown

```text
A signed note whose signer was removed: the profile reference is cleared and the captured name stays; the note then counts as nobody signed it and any writer may change it, as for Delivery notes
```

maintainer

Unknown

```text
A very large number of notes on one report: no cap and no paging
```

maintainer

### Models

Quint

docs\/feature\/story\-6564\-report\-notes\/design\/reportNoteModifyRule\.qnt

##### Properties

signedStaysItsAuthors

Invariant

```text
A note whose signer is a profile is changed or deleted only by that profile, whatever the authentication setting was when it was written or is now
```

NoteSignature\.MayBeChangedBy

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/NoteSignature\.cs\:1

##### Atoms

State

authOn

ICurrentUserProfileService\.GetOrCreateFromPrincipalAsync

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Access\/Application\/ICurrentUserProfileService\.cs\:8

```text
Authentication on yields a profile for the caller, off yields null; the model does not generate on-without-profile
```

State

author

ReportNote\.AuthorUserProfileId

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportNote\.cs\:1

None.

State

changedBy

ReportNote\.LastEditedAt

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportNote\.cs\:1

```text
Model-only record of who changed a note; the runtime keeps only the instant, so the invariant is observed through refusals
```

Action

modify

ReportNoteCommands\.Edit

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportNoteCommands\.cs\:1

```text
Also stands for Delete, which asks the same rule
```

Action

mayModify

NoteSignature\.MayBeChangedBy

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/NoteSignature\.cs\:1

None.

##### Assumptions

```text
Profile id 0 stands for no profile: a note nobody signed, or a caller on an instance with authentication off
```

```text
Two users and three note ids, authentication switchable at any step
```

```text
Write access on the owner is already granted by the route guard; the model covers only the per-note author rule
```

##### Mutants

###### N2

```text
mayModify answers true for every caller, as if the author check were dropped (file design/reportNoteModifyRule.mutantAlwaysMay.qnt; quint run violates signedStaysItsAuthors, see the receipt)
```

###### Kills

signedStaysItsAuthors

### Closure

```text
Add, list, change and delete a report note, and the print exclusion
```

Safety

Pass

DesignedNotBuilt

```text
Bounded checking in Quint (simulation and bounded verify, not a proof) of signedStaysItsAuthors with a mutant that violates it; text, owner scoping and the author-unknown refusal are enforced in ReportNote.Write and ReportNoteCommands; the length bound by HasMaxLength; the route guard refuses a caller without write access before any use case
```

##### Evidence

docs\/feature\/story\-6564\-report\-notes\/design\/reportNoteModifyRule\.qnt

docs\/feature\/story\-6564\-report\-notes\/design\/reportNoteModifyRule\.receipt\.txt

```text
A valid note can be built and read back by its writer
```

Inhabitation

Pass

DesignedNotBuilt

```text
input text, author -> ReportNote.Write -> ReportNoteStore.Add and Save -> ReportNoteQueries.List -> ReportNoteDto -> GET notes route -> ReportNotes component
```

##### Evidence

None.

```text
Notes reach the reader through public ports
```

PublicReachability

Pass

DesignedNotBuilt

```text
IReportNoteCommands and IReportNoteQueries are reached by TeamReportNotesController and PortfolioReportNotesController, registered in Program.RegisterReports, and by ReportService from the ReportView in the live report
```

##### Evidence

None.

```text
Note requests make progress
```

Liveness

Pass

DesignedNotBuilt

```text
No liveness claim: each request is one synchronous transaction with no waiting step, retry loop or background progress, so there is no fairness assumption to model
```

##### Evidence

None.

### Risks

#### Y1

Stressor

```text
Assumption: authentication stays on once a note is signed. Negated: authentication is switched off
```

```text
A signed note stays its signer's and shows its name
```

```text
Nobody can change or delete it until authentication is on again and the signer returns
```

```text
Any writer changes or deletes a signed note while authentication is off
```

```text
A signed note whose signer is removed counts as unsigned
```

ReportNote via NoteSignature

#### Y2

Stressor

```text
Assumption: two writers do not touch one note at once. Negated: both edit, or one deletes while the other edits
```

```text
No half-written note, no orphan
```

```text
Last save wins; an edit of a vanished note answers NotFound
```

```text
An unhandled concurrency exception reaches the caller
```

```text
A writer can overwrite another writer's edit of an unsigned note
```

ReportRepository

#### Y3

Stressor

```text
Assumption: notes reach the reader only in the live report. Negated: the print bundle imports the notes component
```

```text
Print and PDF show no notes
```

```text
The live report shows them
```

```text
A note appears in a printed or PDF report
```

```text
A future change could import the component into print; the never-reaches rule catches it
```

frontend architecture test

#### Y4

Bypass

```text
A direct HTTP request without the UI, or with another report id or owner in the route
```

```text
Only a writer on the owner adds, and a note is found only through its own report and owner
```

```text
Refusal 403 or 404
```

```text
A note read or changed through a foreign owner or report
```

```text
Reading needs read access on the owner only, like the report itself
```

RbacGuard and ReportNoteStore scoping

#### Y5

SinglePointOfFailure

```text
ICurrentUserProfileService answers null while authentication is on (no stable subject claim, or an API key with no profile row), the authentication mode is Misconfigured, or the database is down when a note is written
```

```text
No note is saved with a wrong or missing author while authentication is on or unsure
```

```text
The request is refused loudly with note-author-unknown (403), or fails
```

```text
An unsigned note saved because authentication is on, or the mode is unsure, and the lookup found nobody; or no note signed because RBAC is off while authentication is on
```

```text
Delivery notes still save such a note unsigned; a pre-existing gap reported to the maintainer
```

ReportNoteCommands via ReportNoteCaller

#### Y6

Bypass

```text
An outside writer to the ReportNotes table with text over 2000 characters
```

```text
Reads still work
```

```text
The column bound HasMaxLength(ReportNote.LongestText) refuses the write
```

```text
A silent truncation
```

```text
On a provider that does not enforce a length the bound relies on ReportNote.Write; tested on the provider the test project uses
```

LighthouseAppContext mapping and migration AddReportNotes

### Architectural tests

#### T1

```text
Reports module rules R-RP1 to R-RP6 hold with the new types: every new Models.Reports type is on ReportsMembership.TypesThatStay, every new Modules.Reports type is on ReportsMembership.MovingTypes and in TypesByLayer, no new outside caller of Reports Application, no new listed outbound edge, and no EF Core, Access or clock reached by ReportNoteCommands, ReportNoteQueries or IReportNoteStore
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsModuleBoundariesArchUnitTest
```

#### T2

```text
Every note route action declares its access (RbacGuard or CheckedInHandler)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~EveryActionDeclaresItsAccessArchitectureTest
```

#### T4

```text
Every Reports type is on its membership list: ReportNote in TypesThatStay, the new Application types in MovingTypes
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsMembershipArchUnitTest
```

### Contract tests

#### T3

```text
The real note routes answer the real client schema and refuse a reader on write: GET lists for a reader, a reader's POST is 403, a reader's PUT and DELETE are not a success and leave the note unchanged (405 until S3 adds them, 403 after), and a refusal carries its code
```

Report note wire

TeamReportNotesController and PortfolioReportNotesController

ReportService note methods and ReportNoteSchema

```text
A reader gets the list with canModify false and no write succeeds; a writer's POST is read back through the same schema
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportNotesWireContractTest
```

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportNote\.cs

Create

```text
namespace Lighthouse.Backend.Models.Reports. public sealed class ReportNote with public const int LongestText = 2000; properties int Id, int ReportId, string Text (default empty), DateTime CreatedAt, DateTime? LastEditedAt, int? AuthorUserProfileId, string? AuthorDisplayName, all get and set for EF. Static factory public static ReportOutcome<ReportNote> Write(int reportId, string? rawText, int? signedByProfileId, string? signerDisplayName, DateTime createdAt): trims rawText, answers ReportOutcome<ReportNote>.Refused(ReportRefusalCodes.NoteTextMissing) when the trimmed text is empty or the input null, Refused(ReportRefusalCodes.NoteTextTooLong) when it is over LongestText, else Done with the note (LastEditedAt null). It references no Application type, so Models stays free of Reports Application
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportRefusalCodes\.cs

Extend

```text
Adds public const string NoteTextMissing = "note-text-missing", NoteTextTooLong = "note-text-too-long", NoteAuthorUnknown = "note-author-unknown" (S3 adds NoteNotYours = "note-not-yours")
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportNoteCommands\.cs

Create

```text
namespace Lighthouse.Backend.Modules.Reports.Application. public interface IReportNoteCommands { Task<ReportOutcome<ReportNoteRead>> Add(ReportOwner owner, int reportId, ReportNoteCaller caller, string? text, DateTime now); }. Outcomes: Done with the read; NotFound when the owner holds no such report; Refused with ReportRefusalCodes.NoteAuthorUnknown for an Unresolved caller, NoteTextMissing or NoteTextTooLong for the text. The check order is report found, then caller, then text. S3 adds Edit(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller, string? text, DateTime now) returning Task<ReportOutcome<ReportNoteRead>> and Delete(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller) returning Task<ReportOutcomeStatus> The store is held as an internal property, not an unused private field (analyzer S1144).
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportNoteQueries\.cs

Create

```text
public interface IReportNoteQueries { ReportOutcome<IReadOnlyList<ReportNoteRead>> List(ReportOwner owner, int reportId); }. Done with the notes of that report newest first (CreatedAt then Id, both descending), empty when it has none; NotFound when the owner holds no such report. S3 adds the overload List(ReportOwner owner, int reportId, ReportNoteCaller caller) whose reads carry CanModify; the S1 overload stays The store is held as an internal property, not an unused private field (analyzer S1144).
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportNoteStore\.cs

Create

```text
public interface IReportNoteStore { bool ReportExists(ReportOwner owner, int reportId); IReadOnlyList<ReportNote> NotesNewestFirst(ReportOwner owner, int reportId); void Add(ReportNote note); Task Save(); }. A note is reachable only through its owner and report id. S3 adds ReportNote? FindForEdit(ReportOwner owner, int reportId, int noteId), void Remove(ReportNote note) and Task<bool> TrySave(), which answers false when a concurrent delete made the save conflict
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportNoteCaller\.cs

Create

```text
public abstract record ReportNoteCaller with a private protected constructor and three public nested sealed records in the same file: Anonymous (no members; authentication off), Signed(int ProfileId, string? DisplayName), Unresolved (authentication on, nobody resolved). Built with new ReportNoteCaller.Anonymous(), new ReportNoteCaller.Signed(7, "Ann"), new ReportNoteCaller.Unresolved() Namespace Lighthouse.Backend.Modules.Reports.Domain, layer Domain (it declares no port and reaches only the shared Models, the base library and the Domain).
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportNoteRead\.cs

Create

```text
public sealed record ReportNoteRead(int Id, string Text, DateTime CreatedAt, DateTime? LastEditedAt, string? AuthorDisplayName). Instants are UTC DateTime; the instance-zone day is not on this record. S3 appends bool CanModify as the last positional member Namespace Lighthouse.Backend.Modules.Reports.Domain, layer Domain (it declares no port and reaches only the shared Models, the base library and the Domain).
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportNoteCommands\.cs

Create

```text
public sealed class ReportNoteCommands(IReportNoteStore store) : IReportNoteCommands. Add as declared on IReportNoteCommands: stores ReportNote.Write(reportId, text, signed ? profileId : null, signed ? displayName : null, now) for Anonymous (unsigned) and Signed callers, saves once, answers the read The store is held as an internal property, not an unused private field (analyzer S1144).
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportNoteQueries\.cs

Create

```text
public sealed class ReportNoteQueries(IReportNoteStore store) : IReportNoteQueries. List as declared on IReportNoteQueries The store is held as an internal property, not an unused private field (analyzer S1144).
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/ReportRepository\.cs

Extend

```text
Also implements IReportNoteStore over context.ReportNotes: ReportExists and NotesNewestFirst filter through the owner column of Reports, ordered by CreatedAt then Id descending, AsNoTracking; Add and Save as declared
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Data\/LighthouseAppContext\.cs

Extend

```text
public DbSet<ReportNote> ReportNotes. Table ReportNotes: Id int primary key; ReportId int required, foreign key to Reports.Id with cascade delete, indexed; Text required, HasMaxLength(ReportNote.LongestText); CreatedAt required; LastEditedAt nullable; AuthorUserProfileId int nullable, foreign key to UserProfiles with SetNull; AuthorDisplayName nullable
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Program\.cs

Extend

```text
RegisterReports wires the note ports
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportNotesControllerBase\.cs

Create

```text
public abstract class ReportNotesControllerBase(IReportNoteQueries queries, IReportNoteCommands commands, ICurrentUserProfileService profiles, IAuthModeResolver authModes, ILighthouseClock clock) : ControllerBase, with public static ReportNoteCaller CallerOf(AuthMode mode, UserProfile? profile): Disabled gives new ReportNoteCaller.Anonymous(); Enabled with a profile gives new ReportNoteCaller.Signed(profile.Id, profile.DisplayName); Enabled with null, Misconfigured and Blocked give new ReportNoteCaller.Unresolved(). Protected actions answer the ports' outcomes with ProblemDetails carrying the code (400, 403, 404) The ports (queries, commands, profiles, authModes, clock) are exposed as protected properties, because analyzer S1144 forbids unused private fields.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportNotesController\.cs

Create

```text
Routes under teams, RbacGuard TeamRead on the class and TeamWrite on writes
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportNotesController\.cs

Create

```text
Routes under portfolios, PortfolioRead and PortfolioWrite
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/ReportNoteDto\.cs

Create

```text
public sealed class ReportNoteDto with int Id, string Text, DateTime CreatedAt, DateOnly CreatedOn, DateTime? LastEditedAt, DateOnly? LastEditedOn, string? AuthorDisplayName, and public static ReportNoteDto From(ReportNoteRead note, ILighthouseClock clock) that reduces the instants to instance-zone days with clock.ToInstanceDay. S3 adds bool CanModify
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/ReportNoteRequest\.cs

Create

```text
Request body with the text
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Sqlite\/Migrations\/LighthouseAppContextModelSnapshot\.cs

Extend

```text
Regenerated by CreateMigration for AddReportNotes; the migration files are generated, never hand-written
```

Lighthouse\.Backend\/Lighthouse\.Migrations\.Postgres\/Migrations\/LighthouseAppContextModelSnapshot\.cs

Extend

```text
Regenerated by CreateMigration for AddReportNotes
```

Lighthouse\.Frontend\/src\/models\/Reports\/ReportNote\.ts

Create

```text
export interface IReportNote { id: number; text: string; createdAt: string; createdOn: string; lastEditedAt?: string | null; lastEditedOn?: string | null; authorDisplayName?: string | null }; export const ReportNoteSchema: z.ZodType<IReportNote> = z.object({...}); export const ReportNotesSchema = z.array(ReportNoteSchema). S3 adds canModify?: boolean
```

Lighthouse\.Frontend\/src\/services\/Api\/ReportService\.ts

Extend

```text
IReportService and ReportService gain getNotes(owner: IReportOwner, reportId: number): Promise<IReportNote[]> (GET /{teams|portfolios}/{owner.id}/reports/{reportId}/notes) and addNote(owner: IReportOwner, reportId: number, text: string): Promise<IReportNote> (POST with body { text }); S3 adds updateNote(owner, reportId, noteId, text) and deleteNote(owner, reportId, noteId)
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.tsx

Create

```text
export default ReportNotes: React.FC<Readonly<{ owner: IReportOwner; reportId: number; mayWrite: boolean }>>; test ids report-notes (area), report-note (each note), report-note-input, save-report-note-button; text from the field is sent as typed
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/useReportNotes\.ts

Create

```text
export const reportNotesQueryKeyOf = (owner: IReportOwner, reportId: number) => [...reportQueryKeyOf(owner, reportId), "notes"] as const; export const useReportNotes = (owner: IReportOwner, reportId: number) => UseQueryResult<IReportNote[]> reading reportService.getNotes
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportView\.tsx

Extend

```text
Renders <ReportNotes owner={owner} reportId={reportId} mayWrite={isEditor} /> when isEditor (S2: for every reader)
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembership\.cs

Extend

```text
TypesThatStay gains ReportNote (Models.Reports); MovingTypes gains IReportNoteCommands, IReportNoteQueries, IReportNoteStore, ReportNoteCommands, ReportNoteQueries with layer Application and ReportNoteCaller, ReportNoteRead with layer Domain; TypesThatStay also gains ReportNoteDto and ReportNoteRequest (the API types of the notes), in the skeleton commit
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

Extend

```text
TypesByLayer gains the new Application types ReportNoteCaller and ReportNoteRead go under the Domain key, the other new types under the Application key.
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportNotesControllerCallerTest\.cs

Create

```text
Calls ReportNotesControllerBase.CallerOf(AuthMode, UserProfile?) over the four AuthMode members and a present or absent profile, including Enabled with a profile when the RBAC switch is off
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/ReportNotesTableConstraintsTest\.cs

Create

```text
Over the real context: table ReportNotes, columns Id, ReportId, Text, CreatedAt, LastEditedAt, AuthorUserProfileId, AuthorDisplayName; a 2001-character Text is refused and 2000 accepted; deleting a Report deletes its ReportNotes; removing a UserProfile sets AuthorUserProfileId to null
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.add\.test\.tsx

Create

```text
Component test of the writer path: add, list newest first, blank refused, loading and failure look
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportNoteScenarios
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/Reports/ReportNotes.add.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsModuleBoundariesArchUnitTest
pwsh Lighthouse.Backend/Test-Fast.ps1
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportNotesTableConstraintsTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsMembershipArchUnitTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportNotesControllerCallerTest
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.add\.test\.tsx

Lighthouse\.Frontend\/src\/tests\/MockApiServiceProvider\.ts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/ReportNotesTableConstraintsTest\.cs

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportNotesControllerCallerTest\.cs

None.

None.

#### Public oracle

```text
A writer calls IReportNoteCommands.Add on a report with text, with a caller profile on and with none, then reads IReportNoteQueries.List; a second call adds blank text; then the report is deleted
```

```text
The list holds the note first, with its text, the instance-zone day and the author name (no name when no profile); the blank add answers Refused note-text-missing and stores nothing; after the report is deleted the notes cannot be read The refusal of a caller without write access (P5) is a route guard and is observed in S2's wire contract, not here.
```

```text
A list that is ascending, shows a name with no profile, saves blank text, or still returns notes of a deleted report
```

##### Promises

P1

P2

P3

P4

P11

#### Prefactoring

```text
Before any behaviour, in a separate commit, add the declared public surface of this slice as behaviour-free skeletons: the C# types ReportNote (entity with LongestText and properties; Write throws NotImplementedException), ReportNoteCaller with its three nested records, ReportNoteRead, IReportNoteCommands, IReportNoteQueries, IReportNoteStore, ReportNoteCommands and ReportNoteQueries (every member throws NotImplementedException), ReportNotesControllerBase with CallerOf throwing, the DTO ReportNoteDto with From throwing, the three refusal code constants, and the TypeScript IReportNote, ReportNoteSchema, the getNotes and addNote members on IReportService and ReportService (throwing), and the ReportNotes and useReportNotes exports (not rendered). Nothing is registered in Program.cs, mapped in LighthouseAppContext, migrated, routed or rendered, so no behaviour changes; ReportsMembership and TypesByLayer list the new types so the architecture tests stay GREEN. Where an analyser flags NotImplementedException the skeleton carries a narrow inline suppression with its reason. The S1 oracle is then written against this surface and fails on behaviour (NotImplementedException, missing rows), a semantic RED, not on a missing type
```

```text
Every existing Reports behaviour and every architecture rule: the surface has no caller and no registration, the Reports module and membership rules stay satisfied
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsMembershipArchUnitTest|FullyQualifiedName~ReportsModuleBoundariesArchUnitTest|FullyQualifiedName~Slice01TeamReportScenarios
pnpm --dir Lighthouse.Frontend build
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

architectural_tests

```text
Slice law covered by the shared architectural tests on the Reports module rules and on the access declaration of every action; no extra slice-level law is reached
```

contract_tests

```text
The shared wire contract test covers this slice; frontend and backend ship in one deployable and change in one commit
```

reuse

```text
Reuse is decided per consulted owner in the shared reuse records: ReportRepository (Extend), MayModify (Extend into NoteSignature), DeliveryNoteDto and DeliveryNotesPanel (CreateNew), ReportOwnerWriteAccess, WidgetBody and useMayEditReports (Reuse). The slice targets that already exist are those owners or their siblings; files the slice creates have no existing owner (nothing decides report notes today), and their analogues are Report, ReportCommands, ReportQueries, TeamReportsController, ReportView.
```

impact

```text
Impact is recorded in the shared impact and agreement records: persistence, both migration snapshots, composition root, report ports (UnchangedCompatible), IReportService and its mocks, clients (UnchangedCompatible), print bundle (UnchangedCompatible). Files this slice creates have no consumers yet; the existing files it extends are covered by those records and by the slice oracle.
```

##### Boundaries

```text
IReportNoteCommands.Add(ReportOwner, int, ReportNoteCaller, string?, DateTime) and IReportNoteQueries.List(ReportOwner, int), both over ReportOutcome; ReportNoteDto.From for the day; ReportNotesControllerBase.CallerOf for the mode mapping; POST and GET reports/{reportId}/notes; ReportNotes in ReportView; IReportService.getNotes and addNote
```

###### Driven ports

```text
IReportNoteStore (ReportExists, NotesNewestFirst, Add, Save), implemented by ReportRepository over the ReportNotes table
```

```text
Controller -> ports -> ReportNote and store; the instant comes in from the controller clock; ReportNote references no Application type
```

###### Failures

None.

##### Observations

```text
The notes of one report as the live report lists them: newest first, text, instance-zone day written, author name or none
```

ReportOutcome of IReadOnlyList of ReportNoteRead

```text
Two notes with equal text, day and author written at different instants of one day (order is by instant then id)
```

```text
The outcome of adding a note: Done with the note, NotFound, or Refused with a code
```

ReportOutcome of ReportNoteRead

```text
A refusal for blank text from one for over-length text, except by their code
```

```text
The instance-zone day a note was written, observed through ReportNoteDto.From(read, clock) with the harness clock and no web host; the port itself exposes only instants. The wire contract test belongs to S2 and is not needed for S1, so one wire test serves the Request
```

ReportNoteDto with CreatedOn as DateOnly

```text
A DTO built by From from one the controller serialises; the serialised shape is observed in S2
```

##### Equality

notes of a report

```text
Two notes agree when their text, written day, edited day and author name agree; ids and instants are not compared
```

##### Constructors

ReportNote\.Write

static ReportOutcome\<ReportNote\> Write\(int reportId\, string\? rawText\, int\? signedByProfileId\, string\? signerDisplayName\, DateTime createdAt\)

ReportNoteCaller

new ReportNoteCaller\.Anonymous\(\) \| new ReportNoteCaller\.Signed\(int profileId\, string\? displayName\) \| new ReportNoteCaller\.Unresolved\(\)

ReportNotesControllerBase\.CallerOf

public static ReportNoteCaller CallerOf\(AuthMode mode\, UserProfile\? profile\)

ReportNoteDto\.From

public static ReportNoteDto From\(ReportNoteRead note\, ILighthouseClock clock\)

##### Laws

###### L1

```text
For any raw text, Write stores it trimmed when the trimmed length is 1 to 2000 and refuses it otherwise, and the stored text of an accepted note equals the trimmed input
```

Invariant

Domain

ReportNote

###### Treatment

Broad

###### Generator

```text
Strings of 0 to 2200 characters over letters, digits, spaces, tabs, newlines and markup characters
```

2200 characters\, 200 cases

Fixed seed per run printed on failure

###### Must include

```text
Empty, only blanks, exactly 2000 and 2001 characters after trim, leading and trailing blanks, interior line breaks
```

None.

```text
No shrinking: the test project has no property library. Seeded generation with the seed printed on failure, fixed boundary cases in must-include, replay by seed
```

```text
The expected outcome from string length arithmetic on the generated text, not from Write
```

```text
A Write that bounds before trimming, or keeps blanks, fails the boundary lengths
```

###### Evidence

None.

###### L2

```text
For any sequence of adds to one report, the list is ordered by CreatedAt then Id descending and holds exactly the accepted notes of that report and no note of another report or owner
```

Invariant

Application

ReportNoteQueries

###### Treatment

Broad

###### Generator

```text
Sequences of 0 to 12 adds over two reports and two owners with instants that tie
```

12 adds\, 100 cases

Fixed seed printed on failure

###### Must include

```text
Equal instants, adds to the other report, blank adds interleaved
```

None.

```text
No shrinking: the test project has no property library. Seeded generation with the seed printed on failure, fixed boundary cases in must-include, replay by seed
```

```text
A list model sorted by the generated instants and insertion order
```

```text
An ascending order, or a list that includes another report's note
```

###### Evidence

None.

###### L3

```text
Deleting a report, alone or through bulk removal, leaves none of its notes readable
```

Invariant

Infrastructure

ReportRepository

###### Treatment

Finite

###### Cases

```text
Single delete of a report with two notes
```

```text
Bulk removal of two reports with notes, one other report keeps its notes
```

```text
Deleting the owning Team removes its reports and their notes
```

```text
Finite domain: the listed cases are every member of the case table, run as native examples
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

###### L8

```text
With authentication on and no resolvable profile (Unresolved), add is refused with note-author-unknown and stores nothing; with authentication off (Anonymous) the note is stored unsigned; with a profile (Signed) it is stored signed with the name
```

Invariant

Application

ReportNoteCommands

###### Treatment

Finite

###### Cases

```text
Anonymous add
```

```text
Signed add
```

```text
Unresolved add
```

```text
Finite domain: the three caller variants are every member of the closed choice type, run as native examples
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

###### L9

```text
The text column refuses a direct write of 2001 characters and accepts 2000
```

Invariant

Infrastructure

LighthouseAppContext mapping

###### Treatment

Finite

###### Cases

```text
2000 characters accepted
```

```text
2001 characters refused
```

```text
Finite domain: the two boundary lengths of a single bound, run as native examples
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/ReportNotesTableConstraintsTest\.cs

###### L11

```text
The controller maps the sign-in mode and the resolved profile to the caller and takes no other input: Disabled to Anonymous; Enabled with a resolved profile to Signed; Enabled with no profile, Misconfigured and Blocked to Unresolved. CallerOf has no RBAC parameter, so the RBAC switch cannot change the result
```

Invariant

Adapter

ReportNotesControllerBase

###### Treatment

Finite

###### Cases

```text
Disabled
```

```text
Enabled with profile
```

```text
Enabled with no profile
```

```text
Misconfigured
```

```text
Blocked
```

```text
Finite domain: AuthMode has four members and the profile answers present or absent, run as native examples over the closed set
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportNotesControllerCallerTest\.cs

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

### S2

#### Targets

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportView\.tsx

Extend

```text
Mounts the notes area for every reader of the report
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.tsx

Extend

```text
Shows the add control only when the viewer may edit reports and a read-only list otherwise, with its own empty line
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

Create

```text
Wire contract in namespace Lighthouse.Backend.Tests.Contracts.Reports, deriving WireContractTest, as TestHostUseGuardTest requires of a host-started test
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/print\/reportPrint\.enforcement\.test\.ts

Extend

```text
The never-reaches list gains pages/Common/Reports/ReportNotes.tsx and pages/Common/Reports/useReportNotes.ts. The model is a shared data type that print already reaches through the shared service interface and it renders nothing; what must never reach print is the component and the hook that fetch and render notes.
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/useReportNotes\.ts

Extend

```text
Reads for a reader as for a writer
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.reader\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/Reports/ReportNotes.reader.test.tsx src/pages/Common/Reports/print/reportPrint.enforcement.test.ts src/pages/Common/Reports/print/ReportPrintPage.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportNotesWireContractTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~EveryActionDeclaresItsAccessArchitectureTest
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

Lighthouse\.Frontend\/src\/tests\/MockApiServiceProvider\.ts

None.

None.

#### Public oracle

```text
Render ReportView for a Team and for a Portfolio report that has notes as a reader with read access only; send POST, PUT and DELETE on the note routes as that reader; render the print page of the same report
```

```text
The notes are listed with no add, change or delete control; the POST answers 403 and a PUT and a DELETE are not a success and leave the note unchanged (they answer 405 until S3 adds those routes); the print page and the print import closure show and reach no note
```

```text
An add field shown to a reader, a write request that succeeds, or note text in the printed page
```

##### Promises

P5

P6

#### Prefactoring

None.

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

prefactoring

```text
ReportView.tsx holds one component; ReportNotes.tsx is created in S1 with one component; nothing moves
```

architectural_tests

```text
The print never-reaches law is the slice law and is carried by the print enforcement test named in the slice law; the shared architectural tests cover the access declaration of the routes
```

contract_tests

```text
The shared wire contract test covers the reader refusal; no further parties
```

reuse

```text
Reuse is decided per consulted owner in the shared reuse records: ReportRepository (Extend), MayModify (Extend into NoteSignature), DeliveryNoteDto and DeliveryNotesPanel (CreateNew), ReportOwnerWriteAccess, WidgetBody and useMayEditReports (Reuse). The slice targets that already exist are those owners or their siblings; files the slice creates have no existing owner (nothing decides report notes today), and their analogues are Report, ReportCommands, ReportQueries, TeamReportsController, ReportView.
```

impact

```text
Impact is recorded in the shared impact and agreement records: persistence, both migration snapshots, composition root, report ports (UnchangedCompatible), IReportService and its mocks, clients (UnchangedCompatible), print bundle (UnchangedCompatible). Files this slice creates have no consumers yet; the existing files it extends are covered by those records and by the slice oracle.
```

##### Boundaries

```text
ReportView in the live report as a reader (useRbac gating through useMayEditReports); GET reports/{reportId}/notes with TeamRead or PortfolioRead; the print routes
```

###### Driven ports

```text
IReportService note reads
```

```text
ReportView -> ReportNotes -> useReportNotes -> ReportService; print pages never import ReportNotes
```

###### Failures

None.

##### Observations

```text
What a reader without write access sees under a report with notes, and what a printed report shows
```

Rendered notes list with no add\, change or delete control\; printed document with no note text

```text
A reader whose roles are still loading from a reader without write access: both see no control
```

##### Equality

rendered notes area

```text
Two renders agree when they list the same notes in the same order and show the same set of controls
```

##### Constructors

None.

##### Laws

###### L4

```text
For every viewer role and every report type, the print and PDF bundle imports nothing from the notes component or hook
```

Invariant

Application

print enforcement test

###### Treatment

Finite

###### Cases

```text
whatThePrintPagesReach contains none of ReportNotes.tsx and useReportNotes.ts
```

```text
ReportPrintPage render of a report that has notes shows none of their text
```

```text
Finite domain: the listed cases are every member of the case table, run as native examples
```

###### Evidence

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/print\/reportPrint\.enforcement\.test\.ts

###### L5

```text
For every viewer, the add control is present iff the viewer may edit reports on the owner and the notes are known
```

Invariant

Application

ReportNotes

###### Treatment

Finite

###### Cases

```text
Editor with notes loaded
```

```text
Reader
```

```text
Roles still loading
```

```text
Notes failed to load
```

```text
Finite domain: the listed cases are every member of the case table, run as native examples
```

###### Evidence

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.reader\.test\.tsx

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

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/NoteSignature\.cs

Create

```text
Value holding the signer profile id or nobody; MayBeChangedBy is the Delivery rule once
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/DeliveryNote\.cs

Extend

```text
MayBeChangedBy delegating to NoteSignature
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DeliveryNotesController\.cs

Extend

```text
MayModify asks NoteSignature; DeliveryNoteRequest moves to its own file
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/DeliveryNoteRequest\.cs

Create

```text
The existing request body moved out of the controller file by the prefactoring move
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportNote\.cs

Extend

```text
MayBeChangedBy and Revise
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Models\/Reports\/ReportRefusalCodes\.cs

Extend

```text
note-not-yours
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Domain\/ReportNoteRead\.cs

Extend

```text
CanModify decided by the note for the caller In Modules/Reports/Domain.
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportNoteCommands\.cs

Extend

```text
Edit and Delete with NotFound, then the author rule, then the text rule
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/ReportNoteQueries\.cs

Extend

```text
Reads carry CanModify for the caller
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Services\/Implementation\/Repositories\/ReportRepository\.cs

Extend

```text
Find, Remove and a save that answers false on a vanished note
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/ReportNotesControllerBase\.cs

Extend

```text
Edit and Delete answers; note-not-yours is 403
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/TeamReportNotesController\.cs

Extend

```text
PUT and DELETE with TeamWrite
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/PortfolioReportNotesController\.cs

Extend

```text
PUT and DELETE with PortfolioWrite
```

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/Reports\/ReportNoteDto\.cs

Extend

```text
canModify
```

Lighthouse\.Frontend\/src\/models\/Reports\/ReportNote\.ts

Extend

```text
canModify and edited fields
```

Lighthouse\.Frontend\/src\/services\/Api\/ReportService\.ts

Extend

```text
updateNote and deleteNote
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.tsx

Extend

```text
Edit and delete controls per note from canModify and the editor gate, with the edited marker
```

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.change\.test\.tsx

Create

```text
Component test of change and delete controls per note from canModify, with the edited marker
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

Extend

```text
Adds the S3 rows: a reader's PUT and DELETE are refused 403 and the note is unchanged
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportNoteCommands\.cs

Extend

```text
Gains Edit(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller, string? text, DateTime now) and Delete(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller), both returning Task<ReportOutcome<ReportNoteRead>>
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportNoteQueries\.cs

Extend

```text
Gains the overload List(ReportOwner owner, int reportId, ReportNoteCaller caller) whose reads carry CanModify
```

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/Reports\/Application\/IReportNoteStore\.cs

Extend

```text
Gains ReportNote? FindForEdit(ReportOwner owner, int reportId, int noteId), void Remove(ReportNote note) and Task<bool> TrySave(), false when a concurrent delete made the save conflict
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportNoteChangeScenarios
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/Reports/ReportNotes.change.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~DeliveryNote
quint verify docs/feature/story-6564-report-notes/design/reportNoteModifyRule.qnt --main=reportNoteModifyRuleTwoUsers --init=init --step=step --invariant=signedStaysItsAuthors --max-steps=8
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportNotesWireContractTest
```

#### Supports

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/ReportsAtThePortTest\.cs

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.change\.test\.tsx

Lighthouse\.Frontend\/src\/tests\/MockApiServiceProvider\.ts

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

None.

None.

#### Public oracle

```text
A signed author calls Edit and Delete on their note; another writer calls them on it; any writer calls them on a note written with no profile; with authentication off a writer calls them on a signed note; each is followed by IReportNoteQueries.List and the UI render of the list; a reader without write access sends PUT and DELETE on a note to the real routes
```

```text
The author's edit shows the new text marked as edited with the day and the delete removes the note; the other writer is refused note-not-yours with the note unchanged and canModify false; the unsigned note is changed or deleted by any writer; the signed note stays refused with authentication off; the reader's PUT and DELETE answer 403 and the note is unchanged (asserted in the S3 rows of the wire contract test)
```

```text
A signed note changed by a non-author, an unsigned note refused to a writer, or a canModify that disagrees with the refusal, or a reader's PUT or DELETE that is not refused
```

##### Promises

P7

P8

P9

P10

P12

#### Prefactoring

```text
Move DeliveryNoteRequest out of DeliveryNotesController.cs into its own file DeliveryNoteRequest.cs (split-now, a separate commit before the behaviour change), then extract DeliveryNotesController.MayModify unchanged into the kernel value NoteSignature, which makes the report note rule a second asker of one decision Third step, in a separate commit after the two Delivery moves and before any behaviour: add the S3 member contracts as behaviour-free skeletons wired to no route and no UI, so the S3 oracle compiles and fails on behaviour (NotImplementedException), not on a missing member. C#: NoteSignature (Models, namespace Lighthouse.Backend.Models) is created by the second move with the Delivery rule unchanged, and sits in no Reports membership list; ReportRefusalCodes.NoteNotYours = "note-not-yours"; ReportNote.MayBeChangedBy(int? callerProfileId) returning bool and ReportNote.Revise(string? rawText, DateTime editedAt) returning ReportOutcome<ReportNote>, both throwing; ReportNoteRead gains the last positional member bool CanModify (the S1 mapping passes false); IReportNoteCommands gains Edit(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller, string? text, DateTime now) returning Task<ReportOutcome<ReportNoteRead>> and Delete(ReportOwner owner, int reportId, int noteId, ReportNoteCaller caller) returning Task<ReportOutcome<ReportNoteRead>> (the removed note on Done, so a refusal keeps its code); IReportNoteQueries gains the overload List(ReportOwner owner, int reportId, ReportNoteCaller caller); IReportNoteStore gains ReportNote? FindForEdit(ReportOwner owner, int reportId, int noteId), void Remove(ReportNote note) and Task<bool> TrySave(); ReportNoteCommands, ReportNoteQueries and ReportRepository implement them by throwing; ReportNoteDto gains bool CanModify taken from the read; ReportNotesControllerBase gains protected EditNote and DeleteNote members that throw and are exposed by no controller action. TypeScript: IReportNote gains canModify?: boolean; IReportService and ReportService gain updateNote(owner: IReportOwner, reportId: number, noteId: number, text: string): Promise<IReportNote> and deleteNote(owner: IReportOwner, reportId: number, noteId: number): Promise<void>, which reject with an Error; ReportNotes.tsx is unchanged. No new type lands in a Reports membership list (the only new type, NoteSignature, is in Models outside Reports, and ReportNoteRead stays in the Domain layer reaching only Models and the base library), so ReportsMembership and TypesByLayer need no change.
```

```text
Delivery note behaviour: a signed note only by its author, an unsigned note by any writer, whatever the authentication setting; same routes and same DTO; and for the skeleton step every Reports behaviour of S1 and S2, since nothing is registered, routed or rendered
```

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DeliveryNoteAuthorshipTest\.cs

##### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~DeliveryNoteAuthorshipTest|FullyQualifiedName~DeliveryNotesControllerTest
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportNoteScenarios
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Common/Reports/ReportNotes.reader.test.tsx
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ReportsMembershipArchUnitTest|FullyQualifiedName~ReportsModuleBoundariesArchUnitTest
pnpm --dir Lighthouse.Frontend build
```

#### Records

##### Decisions

None.

##### Constraints

None.

##### Reuse

None.

##### Impact

None.

##### Agreement

None.

##### Not applicable

architectural_tests

```text
No new dependency or boundary law is reached beyond the shared Reports module rules; NoteSignature sits in the Models kernel and reaches nothing
```

contract_tests

```text
The shared wire contract test covers the routes; Delivery and Report notes change in the same commit within one deployable
```

reuse

```text
Reuse is decided per consulted owner in the shared reuse records: ReportRepository (Extend), MayModify (Extend into NoteSignature), DeliveryNoteDto and DeliveryNotesPanel (CreateNew), ReportOwnerWriteAccess, WidgetBody and useMayEditReports (Reuse). The slice targets that already exist are those owners or their siblings; files the slice creates have no existing owner (nothing decides report notes today), and their analogues are Report, ReportCommands, ReportQueries, TeamReportsController, ReportView.
```

impact

```text
Impact is recorded in the shared impact and agreement records: persistence, both migration snapshots, composition root, report ports (UnchangedCompatible), IReportService and its mocks, clients (UnchangedCompatible), print bundle (UnchangedCompatible). Files this slice creates have no consumers yet; the existing files it extends are covered by those records and by the slice oracle.
```

##### Boundaries

```text
IReportNoteCommands.Edit and Delete; PUT and DELETE reports/{reportId}/notes/{noteId}; the per-note controls of ReportNotes
```

###### Driven ports

```text
IReportNoteStore
```

```text
Controller -> ports -> ReportNote.MayBeChangedBy -> NoteSignature; DeliveryNote asks the same NoteSignature
```

###### Failures

None.

##### Observations

```text
Whether a caller may change or delete a note, as canModify on the list and as the refusal of a direct request
```

bool inside ReportNoteRead\; Refused note\-not\-yours

```text
A refused signed note on an instance where authentication is off from one refused for a different signer
```

```text
A changed note in the list: new text, edited marker with the day of the edit; a deleted note absent
```

ReportNoteRead with LastEditedOn or absence

```text
An edit that stored identical text from none, except by the edited day
```

##### Equality

None.

##### Constructors

NoteSignature

NoteSignature\(signedBy\: int\?\) with MayBeChangedBy\(callerProfileId\: int\?\) \-\> bool

##### Laws

###### L6

```text
A note nobody signed may be changed or deleted by any writer, signed or anonymous; a signed note only by a caller whose profile id equals the signer, so never by a caller with no profile, whatever the authentication setting was when it was written or is now
```

Invariant

Domain

NoteSignature

###### Treatment

Broad

###### Generator

```text
Signer in none or profile ids 1 to 3, caller in none or profile ids 1 to 3
```

16 combinations\, exhaustive plus sequences of 0 to 10 switches\, writes and changes

Fixed seed printed on failure

###### Must include

```text
Unsigned note with a profile caller, signed note with no caller, signed note with the signer, signed note with another profile
```

None.

```text
No shrinking: the test project has no property library. Seeded generation with the seed printed on failure, fixed boundary cases in must-include, replay by seed
```

```text
Quint model reportNoteModifyRule.qnt reference: authors and changers tracked in separate maps
```

```text
A rule that compares ids directly so an unsigned note is changeable by nobody, or one that lets a no-profile caller change a signed note
```

###### Evidence

docs\/feature\/story\-6564\-report\-notes\/design\/reportNoteModifyRule\.qnt

###### L7

```text
The canModify value shown for a note equals the verdict the command gives a direct change or delete for the same caller
```

Invariant

Application

ReportNoteQueries

###### Treatment

Finite

###### Cases

```text
Own signed note
```

```text
Another's signed note
```

```text
Unsigned note with authentication on
```

```text
Unsigned note with authentication off
```

```text
Signed note with authentication off
```

```text
Finite domain: the listed cases are every member of the case table, run as native examples
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

###### L10

```text
With authentication on and no resolvable profile, change and delete are refused with note-author-unknown and the note is unchanged, whatever the note's signer
```

Invariant

Application

ReportNoteCommands

###### Treatment

Finite

###### Cases

```text
Unresolved edit of a signed note
```

```text
Unresolved delete of an unsigned note
```

```text
Finite domain: one variant of the closed caller choice crossed with the two verbs, run as native examples
```

###### Evidence

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

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
