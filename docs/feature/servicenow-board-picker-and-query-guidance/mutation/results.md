# Mutation testing — 5610 (ServiceNow: query guidance + Visual Task Board picker)

Run 2026-08-01 against `main` @ `23e23afc5`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **89.37 %** | 381 | 348 | 28 | 5 | 8 m 06 s |
| Frontend (StrykerJS) | **94.44 %** | 180 | 170 | 9 | 0 | 4 m 06 s |

Configs: `stryker.5610.backend.json`, `stryker.5610.frontend.json`, `vitest.stryker.mutation.ts`.

**Both stacks were run twice.** The first pass (against `da7306d85`) scored backend **88.35 %** and
frontend **71.89 %** — the frontend missed the gate. Step 02-06 added 12 tests; the numbers above are
the re-run. The frontend delta is +22.6 points, and it is the interesting part of this record.

## Frontend

| file | tested | survived |
| --- | --- | --- |
| `useCreateWizard.ts` | 109 | **0** (was 32) |
| `BoardWizard.tsx` | 39 | 7 (was 15) |
| `GeneralSettingsComponent.tsx` | 11 | 2 |
| `CreateTeamWizard.tsx` | 11 | 0 |
| `DataRetrievalSchemaDefaults.ts` | 8 | 0 |
| `DataRetrievalWizardRegistry.ts` | 7 | 0 |

### Closed by this pass

**`hasEveryConfigInput` — 32 survivors, now 0.** This is the finding worth keeping. The function is
the wizard's config gate, extracted in 02-05 to fix a defect the maintainer hit while dogfooding: a
board wizard that returned no states dropped the user on **Name & Create** with nothing mapped. Every
clause in it could be mutated without a single test noticing — `&&`→`||` on the state conjunction,
`>` → `>=` on each `.length`, `.trim()` removed, the `""` comparison blanked. The suite's only failing
case was *nothing filled in at all*, under which no individual clause can be shown to carry weight.
One 6-row `it.each` — each row holding every other clause valid and breaking exactly one — closed the
whole cluster. **The fix for a real, user-found bug was effectively unpinned, and the score is what
said so.**

Also closed: the `mergedWith` state ternaries (the existing test set only query + work item types, so
the three state branches were free), the `setValidating` lifecycle on both entry paths, and both arms
of `handleWizardComplete`'s `ApiError` catch. In `BoardWizard`, a plain `Error` must render
Lighthouse's fallback rather than the exception's own message (`&&`→`||` at L27), the refusal and
empty-list strings are now asserted verbatim so blanking them fails, and both `useCallback` dependency
arrays are pinned by re-pointing the wizard at a second connection.

### Accepted survivors

| Mutant | Reason |
| --- | --- |
| `BoardWizard.tsx` L37/L41/L43 — `useState` initialisers | Unobservable, not equivalent. RTL flushes the mount effect inside `render()` and `loadBoards` overwrites all three synchronously, so no test can see the first paint. |
| `BoardWizard.tsx` L47/L75 — `setError("")` | Killing these needs an assertion on the *absence of an unnamed DOM node*; the error `<Typography>` has no role or accessible name. Reachable only by adding a `data-testid` for a mutant's benefit. |
| `BoardWizard.tsx` L60/L84 — `console.error` labels | Log strings. No observable behaviour. |
| `GeneralSettingsComponent.tsx` L174 — `settings?.` optional chaining | Null-safety guard; `settings` is supplied on every reachable path. A test forcing null would exist only to feed the mutant. |
| `GeneralSettingsComponent.tsx` L61 — `ArrayDeclaration` | Guards RBAC wizard-button gating, which belongs to that component's own suite rather than #5610's surface. Killable if that suite is ever hardened. |

## Backend

| file | tested | killed | survived |
| --- | --- | --- | --- |
| `ServiceNowBoardMapper.cs` | 14 | 14 | **0** |
| `ServiceNowBoardVerdict.cs` | 14 | 14 | **0** (was 4) |
| `ServiceNowReadException.cs` | 5 | 5 | 0 |
| `WizardsController.cs` | 4 | 4 | 0 |
| `DataRetrievalSchemaDto.cs` | 68 | 58 | 10 |
| `ServiceNowWorkTrackingConnector.cs` | 276 | 253 | 23 (incl. 5 timeout) |

**Every file this feature created scores 100 %.** The two new pure cores, the exception type and the
controller arm have no survivors.

### Closed by this pass

The four survivors that were #5610's, all in `ServiceNowBoardVerdict`, needed a new test file against
the **pure core** rather than through the connector — and the reason is worth recording. `FromBoardList`'s
empty-list interception returns `SuccessWith(...)`, and `GetBoards` only asks `!refusal.IsValid`, so
**both arms of that rung look identical through the connector**. The advisory is observable only at the
core. `SuccessWith` populates `AdvisoryCode`/`Advisory`, not `Code`/`Message`.

- `boardCount < 1` → `<= 1`: a list of exactly **one** board would have been reported as
  "no boards available". Boundary now pinned.
- `&&` → `||` on both `FromBoardList` and `FromBoardRead`: a 200 whose body is not a record set
  (ADR-114's sign-in page) must reach neither interception.
- The `no_boards_available` code string: judged load-bearing and pinned — it is the machine-readable
  half that ADR-126 D3's both-causes copy hangs off, and blanking it drops the reason from a success.

### Accepted survivors — out of scope

The remaining 33 backend survivors are in code #5610 never touched, and belong to whoever owns it:

- **`ServiceNowWorkTrackingConnector.cs` (23)** — pre-existing paging, `Link` header parsing and date
  logic at L254, L320, L507, L575-586, L959, L983, L1001-1048. Shipped by #5611/#5621.
- **`DataRetrievalSchemaDto.cs` (10)** — display-label and boolean mutants on the **other** connectors'
  rows (ADO, Jira, Linear, CSV) at L9, L11, L15, L17, L42, L51, L69, L107, L116, L133. #5610 added two
  nullable fields to this file; it did not touch those rows.

### Not killed, and not equivalent — one honest gap

**`ServiceNowWorkTrackingConnector.cs` L173** — `lanes.CarriesRecords ? lanes.Records : []` on the lane
read. It differs from the forced-`true` mutant only when `CarriesRecords` is false *and* `Records` is
non-empty, which happens on exactly one path: `ReadEveryPage`'s downgrade early-return carrying rows
accumulated from earlier pages. The lane read uses `HowTheResultSetIsSized.OnlyTheRowsCount`, so a
second page is fetched only on a `Link … rel="next"` header, which `StubbedInstance` does not emit.
Killing it means teaching the stub to page and then refuse page 2. That is a real scenario — a
truncated flow must map **no** states rather than an invented split — so it is recorded as unfinished
rather than dressed up as equivalent.

## Notes for the next run

- `mutate` globs are whole-file on .NET and line-ranged on the frontend. The backend headline
  "13655 mutants created" is the whole backend; the number that matters is "381 will be tested".
- **Background job handles get reaped and kill the run.** The first backend attempt died ~25 minutes in
  with the log frozen mid-coverage-capture. Both runs here were launched detached via `nohup` from a
  script, with output redirected to `/tmp/claude-1000/stryker-5610-{backend,frontend}.log`.
- `inPlace: true` on the frontend mutates the working tree. `git status` was clean after both runs and
  no `@ts-nocheck` leaked into `src/`.
