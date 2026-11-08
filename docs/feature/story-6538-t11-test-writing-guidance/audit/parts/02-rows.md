
## 3. One row per sampled file (scenario A-3)

How each file was read: the five lookup steps of the design, for every file. (1) The test titles from the Vitest listing (`dump.py` prints them with line
numbers). (2) The file's local imports and mocks, and a count per test of render, interaction, screen and `expect` calls (`dump.py`). (3) The unit under test
and any function it uses, found by `ctx_search` or `ls` in the component's folder and `src/utils`. (4) A `.test.ts` for that function: one exists for
`paceBands.ts`, `sleRisk.ts`, `dateWindow.ts`, `widgetStatus.ts` and `stateMappingValidation.ts`; none for `deliveryArchivedRefusal.ts` or for
`computePaceBandRects`, which is exported from `WorkItemAgingChart.tsx`. (5) A label per test (L logic, U UI, M mixed), the file's extraction class and its call.
Where a test's body was not obvious from its title it was opened (for example `WorkItemAgingChart.test.tsx`, `DeliveryPredictabilityChart.beyondTheHorizon.test.tsx`,
`TotalThroughputWidget.test.tsx`, `EditPortfolio.test.tsx`).

**Labels are those after the agreement round** (section 4): the labelling rule's wording was tightened once, and the first rater re-read the other 40 files
for the kinds of test that had disagreed. The labels as first written are kept as `labels-round1.tsv`.

**Extraction class, as applied.** The class is per file and takes the dearest need of the file's logic and mixed tests. **E0**: the logic is already a function or
hook that the test calls directly, or a function with its own `.test.ts`; moving needs no extraction (three files: `useDateRange`, `useUsageDataConsent`, `scatterMarkerUtils`,
where only the `.tsx` suffix or a router wrapper keeps them here). **E1**: a pure function exists untested, or comes out of the component in one sitting. **E2**: it needs a hook,
a provider or a context change. **-**: the file holds no logic or mixed test. Reading E0 to include a test that already calls the function or hook directly is this audit's
reading of the design's wording ("the function exists"); it puts 67 tests into Low (37 + 21 + 9). If those three files were E1 instead, Low would be nothing and Middle would not change
(section 5: the band is 4 either way).

**The call is a label for the reader and plans nothing.** Every call is *keep*: no rewrite and no move of any test is planned in this Story under any band.

