# Former parent Features are cleaned up (ADO Bug #6665)

Delivered 2026-11-24 in AFK mode, nwdes feature `bug-6665-former-parents-stranded`, rigor `thorough` (opus for the
crafter, acceptance designer and architect; sonnet for the reviewers and the examiner).

## What was delivered

A Feature that left every Portfolio used to stay on the Team's Features tab and in its forecasts for good, with its
remaining work frozen. The orphaned-Feature clean-up kept any Feature flagged as a parent, and the flag was never
cleared. The maintainer chose decision A:

1. A Feature is kept while it is in a Portfolio, or while a Feature in a Portfolio names it as its parent.
2. A Feature in no Portfolio loses its remaining work (its FeatureWork rows). A parent kept outside every Portfolio
   is only a name: it leaves the Team's Features tab and the forecasts, and keeps its history.
3. Deleting a Portfolio runs the clean-up at once: `PortfolioController` publishes `PortfolioDeleted` after the
   save, and Work Items reacts with `PortfolioDeletedOrphanedFeatureCleanupHandler`.
4. Existing instances are cleaned on the next refresh or start. No migration.

The keep rule lives in one place, `OrphanedFeatureCleanupService.CleanupAsync`, as set-based deletes. It no longer
reads `IsParentFeature`: a parent that the Feature half of a refresh had already downloaded was stored without the
flag, so it was deleted and created again on every refresh, losing its history and forecasts each time.
`PortfolioRepository` no longer deletes a Portfolio's Features itself.

Feature↔Portfolio stays many-to-many for now. The maintainer wants one item mapped to exactly one unit with the
deeper hierarchies; this is logged on Epic 6455.

## Slices and key commits

| Slice | What it delivers | Oracle | Fix |
|---|---|---|---|
| S1 | A former Feature that is no one's parent leaves the Team page, on refresh and at once on Portfolio delete | `5cd26027a`, `83944aae7` | `f43d3c630` |
| S2 | A parent kept outside every Portfolio is only a name (no remaining work, off the Team page and forecasts) | `0bb69debb` | `65c58eae5` |
| S3 | A parent of a Portfolio Feature is never deleted and re-created | `bed0323bb` | `908fbdb77` |

Final-gate commits: `fd26609d3` (two deletion tests run the clean-up after the delete, as the application does),
`ae04352c7` (the tests name the event and its handler by type), `79775f6ce` (mutation kill round).

## Gates

- Slice reviews: S1, S2 and S3 accepted. The S1 oracle review first rejected the oracle because the controller's
  publish was untested; a controller test was added and the re-review accepted.
- Full backend gates at `908fbdb77`: `dotnet build` 0 warnings; filtered `dotnet test` 14,619 passed, 2 failed.
  The 2 were `DeletionTests`, which pinned the old repository delete; fixed in `fd26609d3` (6/6).
- Whole-candidate review: accepted at `ae04352c7`. Its one fixable finding (tests found the event by name, with
  stale comments) was fixed in `ae04352c7`; the S1 and S2 oracles were re-approved because their files changed.
- EXAMINE: accepted, E1–E6 met, from narrow test probes. The first run had no observation for "cleaned at the next
  refresh"; a fresh run observed it through the refresh test.
- Mutation: backend 100 % (10/10) on the changed lines after one kill round, 87.5 % (14/16) counting the whole
  `PortfolioRepository.Remove`; the kill round added `79775f6ce` (the queued delete and the event get the request's
  abort token). See `docs/feature/bug-6665-former-parents-stranded/mutation/results.md`.
- Frontend: untouched; no frontend gates or mutation.

## Decisions taken in AFK mode, worth a second look

- The FeatureWork rows of a Feature in no Portfolio are deleted, not set to zero; its forecasts stay.
- `IsParentFeature` stays as a column. The parent half of a refresh still writes it and the blocked-item check still
  reads it; only the clean-up stopped reading it.

## Knowingly left open

- The domain event dispatcher logs and swallows a failing handler. If the clean-up fails on Portfolio delete, the
  delete still succeeds and the Features stay on the Team pages until the next refresh or start, which clean them.
- The clean-up at start runs through existing, unchanged code (`Program.cs`); EXAMINE observed the refresh path and
  the service itself, not a real start.
- Not observed in a real instance: a Portfolio refresh against a live tracker after the change.

## Finalize checklist

| Item | Answer |
|---|---|
| Docs prose | Done. `docs/portfolios/portfolios.md`: deleting a Portfolio takes its Features off the Teams' Features tabs and forecasts at once, and keeps a parent another Portfolio still names. `docs/teams/detail.md`: a Feature in no Portfolio leaves the Features tab with the next refresh. |
| Screenshots | N/A, because nothing a user sees changed shape: Features only disappear when they should. |
| Demo data | N/A, because demo data seeds Features in Portfolios; there is nothing stranded to show. |
| Website assets | N/A, because no asset under `docs/assets/` changed. |
| Clients (CLI, MCP) | N/A, because no API route or DTO changed. |
| Context map, `ARCHITECTURE.md` | Done. ADR-270: `PortfolioDeleted` in the event list, "Portfolio deleted" removed from the facts with no event, the controller-published event noted, and "Orphaned Feature" in the Work Items glossary. `ARCHITECTURE.md` §4 row 2: seven Work Items reactions. |
| Usage-data event | N/A, because a fix to data clean-up adds nothing to count (decided at DEVOPS). |
| Mutation | Done. Backend 100 % on the changed lines (87.5 % with the whole `PortfolioRepository.Remove`); survivors on unchanged log lines accepted in `mutation/results.md`. Frontend N/A, because no frontend file changed. |
| Release notes | N/A; written at /release. |
