# Mutation testing — 6665 (former parents stranded on the Team's Features tab)

Run 2026-11-24 against `main` @ `ae04352c7` (first run) and `79775f6ce` (kill round). Gate is 80 % kill rate on the
feature's changed code.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.1.0), changed lines, after kill round | **100 %** | 10 | 10 | 0 | 0 | 4 m 55 s + 2 m 38 s |
| Backend, changed lines plus the whole touched `PortfolioRepository.Remove`, after kill round | **87.5 %** | 16 | 14 | 2 | 0 | (same runs) |
| Backend, whole files, first run (Stryker's own score, no-coverage counted) | 58.76 % | 81 (+16 no coverage) | 57 | 24 | 0 | 4 m 55 s |
| Frontend | N/A | | | | | |

Frontend: N/A, the bug changed no frontend file.

Configs: `stryker.6665.backend.json` (first run, all changed files), `stryker.6665.backend.kill-round.json`
(narrowed to `PortfolioController.cs` and `PortfolioControllerTest`, the one file that had a changed-line survivor).

## Backend

Stryker.NET mutates whole files, so the whole-file score is buried under code the bug did not touch (the
controller's other actions, the repository's forecast bookkeeping). The gate is computed on the mutants whose span
lies on lines `git diff -U0 584185bb5 HEAD` adds; `Ignored` (block-already-covered filter) and `CompileError`
mutants are left out of both counts. `PortfolioRepository.cs` lost lines only (the in-repository orphan removal moved
to the clean-up service), so it has no added line; its touched method `Remove` is reported instead.

| file | whole file (first run) K / S / NC | changed lines K / S (first run) | changed lines K / S (kill round) |
| --- | --- | --- | --- |
| `Services/Implementation/OrphanedFeatureCleanupService.cs` | 6 / 5 / 0 | 6 / 0 | 6 / 0 |
| `Modules/WorkItems/Application/Policies/PortfolioDeletedOrphanedFeatureCleanupHandler.cs` | 1 / 0 / 0 | 1 / 0 | 1 / 0 |
| `API/PortfolioController.cs` | 38 / 10 / 4 (kill round: 39 / 9 / 4) | 2 / 1 | 3 / 0 |
| `Services/Implementation/Repositories/PortfolioRepository.cs` (`Remove` only) | 12 / 9 / 12 (`Remove`: 4 / 2) | no added line | `Remove`: 4 / 2 |
| `Models/Events/PortfolioDeleted.cs` | no mutants (a positional record) | | |

Test-case filter: `OrphanedFeatureCleanupServiceTests`, `PortfolioDeletedOrphanedFeatureCleanupHandlerTest`,
`PortfolioControllerTest`, `PortfolioRepositoryTest`, `DeletionTests`, `PortfolioUpdaterTest`; `API.Integration`,
`Dogfood` and `Contracts` left out. No acceptance suite under `Tests/Acceptance/` belongs to this bug.

### Closed by this pass

- `PortfolioController.cs:81`, null-coalescing "remove left" (`HttpContext?.RequestAborted ?? default` to `default`):
  nothing checked which token the queued delete and the `PortfolioDeleted` event carry. New unit test
  `PortfolioControllerTest.DeletePortfolio_QueuesTheDeleteAndPublishesPortfolioDeletedUnderTheRequestsAbortToken`
  pins that both get the request's abort token. Seen red with the line replaced by `default(CancellationToken)`.
  Commit `79775f6ce`.

### Accepted survivors

- `OrphanedFeatureCleanupService.cs:32-34` (5 mutants: `deleted > 0` to `< 0`, `>= 0`, negated; the log statement
  removed; its message emptied). Unchanged lines. They only decide whether an Information log line is written;
  what is deleted and the returned count are unaffected, so no observable behaviour changes.
- `PortfolioRepository.cs:116` (2 mutants: the "Removing Project" log statement removed, its message emptied).
  Unchanged line inside the touched method; log wording only.
- Whole-file survivors and no-coverage mutants elsewhere in `PortfolioController.cs` (lines 53, 93, 128-218) and
  `PortfolioRepository.cs` (lines 20-111): code the bug did not touch, outside this gate. `PortfolioController.cs:93`
  (removing the refresh-log clean-up in the queued delete) is the nearest one; it predates the bug.

### Not mutated

- `Program.cs`: one DI registration line for the new handler. Mutating `Program.cs` drags the whole start-up into
  scope; the registration is pinned by `DomainEventHandlerOrderPinTest`, which reads the `PortfolioDeleted` handlers the container hands out.
- `Modules/WorkItems/Application/WorkItemService.cs`: comment-only change; there is nothing to mutate.
