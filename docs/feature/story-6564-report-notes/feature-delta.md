# Story 6564 — add notes to reports

ADO Story #6564 "Add notes to reports". No parent Epic. Size M. Route: spine waves, then nwdes at DELIVER.

- DISCOVER (2026-11-26): `docs/product/jobs.yaml`, `job-flow-coach-keep-the-context-with-the-numbers`. VALIDATED at
  low-to-medium confidence (two informants, one customer workshop).
- DIVERGE (2026-11-26): `diverge/recommendation.md`. Notes to reports, built like the Delivery notes (4.10). Two
  review rounds; the host fixed the last three wording findings (`diverge/review.yaml`).
- DISCUSS (2026-11-26): the product facts, promises P1–P11, decisions and slices S1–S3 are in `handoff.json`; read
  them in `brief.md`.

## DISCUSS: maintainer decisions (2026-11-26)

Asked one at a time. The answers, as given:

1. Event date: "day it was written is enough, no need for 2 kinds of notes".
2. Who may add: "write access on team/portfolio is enough".
3. Notes go when the report is deleted: "yes".
4. Print and PDF: out for now, "agreed".
5. Who may change or delete: "same as delivery notes". Delivery notes decide by the note
   (`DeliveryNotesController.MayModify`): a signed note only by its author, a note nobody signed by any user with
   write access, whether authentication is on or off at the time (P10, P12). The first recording said "any writer
   while authentication is off"; the DISCUSS review (F2) showed that is not the Delivery rule, and P10 was amended
   under this answer.
6. Notes in the CLI and MCP report reads: "Not now".
7. Continuous-improvement check before the feature: run it (result below).

Two points rest on the conversation, not on a separate question:

- **The DIVERGE direction** (one kind of note; how large the Team is goes in as note text). Before DISCUSS, the
  maintainer was asked to say if they disagreed with that direction. They answered with answers 1–4 and "Start
  discuss", and raised no objection.
- **The author name as it stood when the note was written.** The request says "similar to deliveries" and answer 5
  says "same as delivery notes"; Delivery notes keep the name as it stood (`DeliveryNote.AuthorDisplayName`).

The DISCUSS review (`discuss-review.yaml`) asked for these records (F1, F6). It also moved P11 (notes go with their
report) into S1, so no slice leaves notes behind a deleted report (F3), and recorded that reports have no archived
state, so no note is frozen (F5).

## DISCUSS: improvement backlog check (`/continuous-improvement before 6564`, 2026-11-26)

Board: five open improvement Epics (6510 Active; 6507, 6508, 6509, 6511 New), 23 open items, and 2 unparented
clean-as-we-go items (#6647, #6652).

| Item | Verdict | Why |
|---|---|---|
| S09a #6382 Context contracts (Active) | Before, already in flight | Its feature A is on `main`. When it adds the contracts-only cross-context rule, the author lookup the notes need (a user profile from Access) must reach Access through its public surface. DESIGN reads S09a's current rule before placing that call. No separate pick-up. |
| S04b #6368 Reduce `Models` to the shared kernel (Next) | During, as a DESIGN rule only | The `Report` entity still sits in `Models`. The new note type must not grow `Models`: put it in `Modules/Reports`, or note why not. Doing S04b first is not needed. |
| FE2 #6390 Frontend by module, Delivery pilot (New) | Unrelated, with one watch-point | Only if DESIGN extracts a shared notes component from `DeliveryNotesPanel`: that edits files FE2 will move. Prefer a Reports-owned component unless sharing clearly pays. |
| #6647, #6652 Clean as we go | During, by the standing rule | "Improve the tests you touch" applies to the report test classes S1–S3 touch. |
| Every other open item | Unrelated | None touches Reports, report notes or Delivery notes. |

Recommendation: no improvement item before this Story. The two DESIGN rules above carry what matters.

### DISCUSS checklist

| Item | Answer |
|---|---|
| RBAC impact | No new permission and no new role. Adding a note needs write access on the Team or Portfolio that owns the report (`IRbacAdministrationService.CanWriteTeamAsync` / `CanWritePortfolioAsync`, as `ReportOwnerWriteAccess` does today). Reading the notes needs read access on the owner, the same as reading the report. Changing and deleting add the author rule of Delivery notes on top. In the UI, the add, change and delete controls are gated through `useRbac()`. |
| Lighthouse-Clients CLI / MCP | New API routes for report notes; the existing report routes and DTOs the clients read (`packages/client/src/reportReads.ts`) stay unchanged. No client change and no client release (maintainer: "Not now"). |
| Website marketing surface | N/A for now, because notes are a small addition to an existing capability (Reports) and no asset the website hot-links from `docs/assets/` changes. Revisit at finalize if a report screenshot with notes is taken. |
| Context map (ADR-270) | Touches **Reports** only. It reads the author's display name from **Access** (user profile); that edge exists already (Reports depends on RBAC). New glossary term in Reports: **Report note**, a short, dated, attributed text a person writes on one report to give its numbers context. Domain events: none published and none consumed. Notes go with their report inside the Reports context. ADR-270 gets the glossary term and the Reports row's aggregate list ("Reports and their notes …") in this feature. |
| UI surface (ADR-271) | The report view, opened from the **Reports** tab of the Team or Portfolio page (ADR-271: Reports, every level). Already in its target surface; nothing moves. The print bundle (`pages/Common/Reports/print/`) stays as it is. |

## DEVOPS (2026-11-26, AFK)

**Platform risk:** none material. One additive table (`ReportNotes`, a cascade foreign key to `Reports`, a set-null
foreign key to the author profile), generated with `CreateMigration` for every provider. It is expand-only, with no
new service, configuration, secret, job or external call. The usual gates (the filtered `dotnet test`, `pnpm test`,
`pnpm build`, `dotnet build`) and the existing migration tests cover it.

**Usage-data event:** `ReportNoteAdded = 30`, appended to `UsageDataEventName`. It is **name-only** and carries no
property: not which report or report type, not which Team or Portfolio, not the text or its length, not who wrote it.
- **What it answers:** whether anyone uses report notes at all, the outcome the job asks about. Counting notes per
  report type would add nothing while Then & Now is the only type. A note's length or count is a number we do not send.
- **When it is sent:** by the browser, after the server has accepted a new note, through the existing usage-data
  service (`services/Api/UsageDataService.ts`), the way *A report was deleted* is sent from `useReportDeletion.tsx`.
  A refused note, a change and a deletion are not reported.
- **Emitted by S1**, the slice that makes notes usable. S1 also adds the row to `docs/settings/usagedata.md` ("A note
  was added to a report | Somebody added a note to a report and your server accepted it. **Changing or deleting a
  note, or a note your server refused, is never reported** | Nothing. **Not which report, not its Team or
  Portfolio, not the note's text or length, not who wrote it**").
- **Host default, AFK:** the maintainer may want to revisit the choice of name-only.

## Start of DELIVER: maintainer decisions (2026-11-26, sketch review)

- **Placement: Option A**, the notes area above the numbers. It sits under the report summary and before the
  panels, outside anything the print bundle renders. Maintainer: "Lets take A. I can see later that we may be able
  to arrange the reports more freely, so let's stick with this for now and eventually change it later." A freer
  arrangement of the report is a possible later item, not part of this Story.
- **Layout and copy, as sketched and not objected to:**
  - "Notes" heading.
  - "No notes yet." when the list is empty.
  - A multi-line field with "Add note", for writers only.
  - Each note shows its text, then "<day> · <author>" and "· edited <day>" when changed. A note nobody signed
    shows the day only.
  - Edit and Delete on the notes the user may change.
  - Errors: "A note needs some text." and "A note can have at most 2000 characters."
  - Loading: "Loading notes" with a spinner. Failure: "Could not load the notes" with Retry, and no add field.
    The report numbers still show.
  - It reuses the Delivery notes panel's layout.
- **Rigor profile, all slices: `standard`.** The crafter and acceptance designer run on Sonnet; the reviewers and
  EXAMINE on Haiku. Agreed at AFK entry.
- **Defaults the maintainer has not answered, kept and listed again at the hold:**
  - 2000 characters as the maximum.
  - A note write is refused when authentication is on and no profile resolves.
  - `NoteSignature` is shared with Delivery notes.
  - The usage-data event `ReportNoteAdded` carries no property.
  - Delivery notes have the same gap for authentication on with no profile (a possible Bug, not created).

## Open, for DESIGN

- Maximum length of a note (Delivery notes have none).
- Placing the new types: `Modules/Reports`, not `Models` (see the improvement check).
- Concurrent edits: proposed "last save wins", as for Delivery notes.
- Deleting a Team or Portfolio: proposed "its reports and their notes go with it".
- Every new view needs a loading look and a failure look (CLAUDE.md, DESIGN).

## Open, for the start of DELIVER

- UI sketch: where the notes sit in the report view, and the add, change and delete controls. Reuse the
  `DeliveryNotesPanel` layout and copy where it fits.
