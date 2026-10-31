# Bug 6458 (a): category lists cannot tell a raw state from a mapping name

ADO Bug #6458 (Active). Part (b) of the same item, the autosave of an unfinished mapping, shipped on its own
as `docs/feature/bug-6458-empty-mapping-autosave/` (commits `bfd39ec97`, `be3adace6`). This workspace is
part (a) only.

## Where we stopped (2026-10-31)

- **RCA done**, in `rca.md` here. The maintainer agreed both root causes and the split:
  part (a) is a design flaw (public API values, a data rewrite of stored lists, connector expansion sites,
  the settings editor), so it goes through **DESIGN** before any fix. No further frontend guard.
- **Next:** start the wave sequence for (a) at DESIGN (`/nw-design`), with `rca.md` section 6.2 as input.
  Then DEVOPS (answer the usage-data event question), DISTILL (walk the UI sketches with the maintainer at
  its start, one decision at a time), DELIVER (agree the rigor profile first).
- **Recommendation to re-cost in DESIGN, not a decision:** shape (ii-b), the lists hold raw states only and
  a mapping is a pure display grouping (no schema change, a data rewrite in place, reconciliation deleted,
  three connector sites stop expanding the lists). Shape (i), the reporter's typed entry list, is the
  alternative. Section 6.2 compares both and lists the call sites.
- **Open before anyone relies on it:** the as-of projections may put items under `Unknown` for a mapping
  whose name is not one of its own states (`TeamMetricsService.cs:743`, `PortfolioMetricsService.cs:251`).
  Code-read only; needs a unit probe. The revised (ii-b) section had one review round, not two.
- **Regression rows to commit in DISTILL:** probes P1 and P2 in `rca.md` section 1 (the probe file itself
  was not kept). P3 and P4 are already pinned by part (b)'s tests.
- **Carried over from (b)'s review:** the frontend folds names and states with `toLowerCase`/`trim`
  (`normalise` in `stateMappingReconciliation.ts`), the backend with `OrdinalIgnoreCase` and
  `IsNullOrWhiteSpace`, so `ı`/`I`, `ſ`/`s` or a U+0085 name still reach a 400. Fix it where (a) replaces
  the reconciliation.
- **API note for DISCUSS-level checklists:** the `doingStates` values in `v1` GET would change meaning (raw
  states instead of mapping names), and a shadowing mapping becomes a 400. Lighthouse-Clients passes
  settings through as opaque JSON, but its docs and skills must say so.
