# Epic 5698 — mutation testing

Run 2026-08-22, against the whole Epic (slices 01–05) after the review pass.

## Backend — 81.58 %, over the 80 % bar

| | |
|---|---|
| Config | `stryker.5698.backend.json` |
| Killed | 279 |
| Survived | 52 |
| No coverage | 11 |
| Ignored (filtered out) | 904 |
| Score | **81.58 %** |
| Duration | 14m38s |

`NoCoverage` counts against the score, which is why 279 / (279 + 52) = 84 % is not the
number reported. Scope is confirmed from the report, not from the created-count: Stryker.NET
prints a whole-project figure in the thousands before the `mutate` filter runs, and that number
says nothing about what was tested.

### What the first run found — 79.53 %

Two of the survivors were worth fixing on their own merits rather than for the score:

- **`Sum` → `Max` on a Feature's work survived.** Every fixture in the suite gives a Feature
  exactly one team, and for one team the two are the same number. A Feature split across two
  teams tells them apart, and the difference is a real defect class: reporting the busier team's
  share as the whole makes a Feature look smaller than it is and further along than it is.
- **The archived progress figure** could be computed the wrong way round and still land on a
  plausible percentage. Half of fourteen now pins it, along with the nothing-done and
  no-work-at-all cases.

Adding those took the score to 81.58 %.

### Survivors left, and why

| Count | File | Judgement |
|---|---|---|
| 23 | `API/DeliveriesController.cs` | Mostly ordering (`OrderBy` ↔ `OrderByDescending`) and null-coalescing on log and error text. Worth a pass if the controller is opened again; none changes a number a reader sees. |
| 9 | `API/DeliveryNotesController.cs` | Same shape — the note-ordering comparator and coalescing on messages. |
| 6 | `Services/.../DeliveryRepository.cs` | Three sit on `EveryConflictIsADelivery`, the guard deciding whether a background conflict is swallowed quietly. Killing them needs a hand-built `DbUpdateConcurrencyException` carrying entries, which is not cheaply constructible. **Left deliberately, and flagged rather than papered over** — the behaviour itself is covered by `ArchivedDeliveryStaleAggregateRaceIntegrationTest`, which drives the real interleaving. |
| 4 | `ArchivedDeliveryProjection.cs` | Remaining coalescing on absent JSON. |
| 4 | `Models/Delivery.cs` | Block/collection-initializer mutations with no behavioural consequence. |

## Frontend — 50.50 %, under the bar

| | |
|---|---|
| Config | `stryker.5698.frontend.json` + `vitest.stryker.5698.ts` |
| Score | **50.50 %** |
| Duration | 38m35s |

Run separately and **after** the backend finished. Running the two stacks concurrently once
produced a result that was 100 % `Timeout` mutants, which is not a result at all.

**The headline number is dominated by one pre-existing file.** Per-file:

| Score | File | |
|---|---|---|
| 83.33 % | `PortfolioDeliveryView.tsx` | new |
| 82.35 % | `deliveryExportTable.ts` | new |
| 77.27 % | `DeliveryMetricsTab.tsx` | new (extracted) |
| 75.00 % | `useDeliveryManagement.ts` | extended |
| 72.22 % | `deliveryArchivedRefusal.ts` | new |
| 64.58 % | `ArchivedFeatureGrid.tsx` | new |
| 50.00 % | `ArchiveConfirmationDialog.tsx` | new — 2 mutants total, so the percentage means little |
| 44.77 % | `ArchivedDeliveriesSection.tsx` | new — 90 survivors, the real gap |
| 25.00 % | `ApiError.ts` | touched — 4 mutants total |
| **10.74 %** | `DeliverySection.tsx` | **pre-existing**, 198 survivors |

`DeliverySection.tsx` is a large component this Epic only added an Archive button to; its 198
survivors are almost entirely code that predates this work. It is in the `mutate` list because
narrowing scope to make a number look better is the exact failure the ledger warns about — the
config that "excludes quietly while the number still looks good". So it stays in, and the number
is reported as it is.

**Honest read:** the code this Epic wrote sits in the 64–83 % band and `ArchivedDeliveriesSection`
is genuinely under-tested at the mutation level. Neither the whole-run figure nor a
DeliverySection-excluded figure would be the truth on its own; both are above.

Follow-up worth doing, not done here: raise `ArchivedDeliveriesSection.tsx`, and treat
`DeliverySection.tsx` as its own piece of test debt rather than this Epic's.

## Reproducing

```
cp docs/feature/epic-5698-deliveries-as-durable-records/mutation/stryker.5698.backend.json Lighthouse.Backend/
cd Lighthouse.Backend/Lighthouse.Backend && dotnet stryker --config-file ../stryker.5698.backend.json
```

Two things the config carries deliberately: whole-file `mutate` entries, because line-span
patterns are silently ignored and yield a clean-looking score describing none of the code under
test; and a `test-case-filter`, without which the run executes the whole suite under
`perTestInIsolation` before mutation even starts.
