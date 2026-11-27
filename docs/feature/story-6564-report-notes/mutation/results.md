# Mutation testing — 6564 (Add notes to reports)

Run 2026-11-27 against `main` @ `a24e5d365` (the collapsed notes included). Gate is 80 % kill rate on both stacks.

| stack | score | mutants | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET), first run | 78.11 % | 169 | 132 | 15 | 22 | 7 m 26 s |
| Backend, after the kill rounds | **86.39 %** | 169 | 146 | 18 | 5 | + 4 m 10 s + 2 m 16 s |
| Frontend (StrykerJS), first run | 70.95 % | 179 | 127 | 34 | 18 | 5 m 22 s |
| Frontend, after the kill round | **91.06 %** | 179 | 163 | 12 | 4 | 2 m 11 s (incremental) |

Configs: `stryker.6564.backend.json` (the full run), `stryker.6564.backend.kill.json` and
`stryker.6564.backend.kill2.json` (the narrowed kill rounds), and `Lighthouse.Frontend/stryker.6564.frontend.json` with
`Lighthouse.Frontend/vitest.mutation.6564.config.ts`.

The backend score after the kill rounds combines the first run's figures for the files no round touched with each
round's figures for the files it re-mutated (`ReportNotesControllerBase.cs`, `ReportNoteCaller.cs`, `ReportRepository.cs`).

## Backend

| file | first run | after the kill rounds |
| --- | --- | --- |
| `API/DeliveryNotesController.cs` (whole file, 19 lines changed) | 45/55 | 45/55 |
| `API/DTO/Reports/ReportNoteDto.cs` | 3/4 | 3/4 |
| `API/ReportNotesControllerBase.cs` | 0/16 | 11/16 |
| `Models/DeliveryNote.cs` | 1/2 | 1/2 |
| `Models/NoteSignature.cs` | 5/5 | 5/5 |
| `Models/Reports/ReportNote.cs` | 15/16 | 15/16 |
| `Modules/Reports/Application/ReportNoteCommands.cs` | 22/22 | 22/22 |
| `Modules/Reports/Application/ReportNoteQueries.cs` | 2/2 | 2/2 |
| `Modules/Reports/Domain/ReportNoteCaller.cs` | 0/1 | 1/1 |
| `Services/Implementation/Repositories/ReportRepository.cs` (whole file) | 39/46 | 41/46 |

### Closed by this pass

- `ReportNotesControllerBase`: nothing in the unit filter reached the HTTP mapping. The kill rounds add the wire contract
  suite, the one place that pins it, and a new wire scenario
  (`A_writer_adds_a_note_is_told_why_an_empty_one_or_one_on_a_missing_report_is_refused_and_deletes_their_own`): 201 with
  the notes route as the location, 400 with a `code` property and the problem's status for an empty note, 404 for a missing
  report, and 204 when the author deletes their own note.
- `ReportNoteCaller.Unresolved`: `ReportNoteCallerTest` pins that a signed-in caller the instance cannot name may change
  no note, not even one nobody signed.
- `ReportRepository` note reads (`Any` mutated to `All`): `ReportRepositoryTest` now holds a Team with two reports, and a
  note is listed and found for edit on its own report alone. The port suite cannot show this, because without a premium
  licence an owner holds one report.

### Accepted survivors

- `?? default` on `HttpContext?.RequestAborted` (7 in `DeliveryNotesController`, 1 in `ReportNotesControllerBase`):
  equivalent. A test request is never aborted, so the token is the same either way.
- `ReportNotesControllerBase:86`, the condition set to `true`: the wire host runs with sign-in on, so the mutant asks for
  the profile exactly when the code does. With sign-in off, `CallerOf` ignores any profile (pinned by
  `ReportNotesControllerCallerTest`).
- `ReportNotesControllerBase:101` and `:103`, an emptied `ProblemDetails` or `ObjectResult` initializer, and the title
  string: equivalent on the wire. ASP.NET fills the problem's status from the result's status and the result's status
  from the problem, and the title is free text no client reads.
- `Text { get; set; } = string.Empty` in `ReportNoteDto`, `ReportNote` and `DeliveryNote`: equivalent. Every row and every
  DTO the code builds sets the text.
- `ReportRepository:82`, `:113` and `:119`: report reads that existed before this Story (the per-template count, the
  concurrency reset and `GetForOwner`). They are mutated only because Stryker.NET mutates whole files.

### Not mutated

- `Program.cs` and `Data/LighthouseAppContext.cs`: a few registration and model lines in very large files. The port and wire
  suites fail at once if a registration or the table mapping is missing.
- Interfaces (`IReportNote*`), request DTOs and `ReportRefusalCodes` / `UsageDataEventName` constants: no logic to mutate.

### No coverage, left

- `DeliveryNotesController:88`, `:125` and `:133`: the RBAC scope checks of the delivery-note routes, which 6564 did not
  change (its diff there only removes lines). They are covered by the host suites under `API/Integration`, which mutation
  testing leaves out.
- `ReportRepository:47` and `:51`: the `DbUpdateConcurrencyException` branch of `TrySave`, met when somebody deletes a
  note between reading and saving it. The in-memory provider never raises it, and no test in the mutation filter forces
  the race. Not verified whether any host or container suite reaches it; a candidate for a follow-up test.

## Frontend

| file | first run | after the kill round |
| --- | --- | --- |
| `pages/Common/Reports/ReportNoteItem.tsx` | 38/54 | 49/54 |
| `pages/Common/Reports/ReportNotes.tsx` | 65/83 | 76/83 |
| `pages/Common/Reports/ReportView.tsx:87-89` | 4/5 | 4/5 |
| `pages/Common/Reports/reportNoteByline.ts` | 3/3 | 3/3 |
| `pages/Common/Reports/reportNoteRefusalCopy.ts` | 14/15 | 14/15 |
| `pages/Common/Reports/useReportNotes.ts` | 3/5 | 3/5 |
| `services/Api/ReportService.ts:180-232` | 0/14 | 14/14 |

### Closed by this pass

- `ReportService`: the four note calls had no test. `ReportService.test.ts` now pins each route and body, the answer as
  parsed, a refusal as an `ApiError` with its code, and a malformed answer refused.
- Add note and Save are not offered while a note is on its way, and are offered again after a refusal.
- The field is marked invalid on a refusal (`aria-invalid`), and the mark and the reason clear on the next save or on
  Cancel. A failed delete's reason clears once a later change is saved.
- The edit field is found by its label "Note text", and the heading is pinned as a level-2 heading.
- "No notes yet." never shows while notes are listed.

### Accepted survivors

- `if (isBusy)` / `if (isSaving)` set to `false` (and their no-coverage blocks): unreachable. The button that starts the
  work is disabled while the work runs, and that is now pinned.
- `whiteSpace: "pre-wrap"` and `alignItems: "flex-start"`: layout only. jsdom does no layout, so no test can see them.
- `hasContentToDim` set to `false` (`ReportNotes:104`): only changes whether the spinner dims the old list or stands
  alone. Shared `WidgetBody` behaviour, pinned in its own tests.
- `ReportNotes:81`, the `status === "ready"` part set to `true`: reported as survived, but a manual probe of the same
  change under `vitest.mutation.6564.config.ts` fails
  `shows no count while the notes are fetched again, so an old count never passes for the current one`. Treated as a
  Stryker per-test coverage artefact, not a missing test.
- `ReportView:87`, the notes shown before the report has arrived: harmless. The notes have their own loading look.
- `reportNoteRefusalCopy:9`, the `instanceof ApiError` check set to `true`: equivalent for every error the service
  throws, which is always an `ApiError`.
- `useReportNotes:13` (the query key builder emptied) and `:21` (optional chaining removed): the key is only compared with
  itself, and the test client always has default query options.
- `ReportNotes:42`, `ReportNoteItem:47` (no coverage): the `?? []` and `?? ""` fallbacks, reached only when the cache or
  the draft is empty, which the flow never produces.
