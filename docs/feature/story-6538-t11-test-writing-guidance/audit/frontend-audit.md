# Frontend test audit: how much of the `.tsx` test layer is logic

Read-only. No test, source file or config under `Lighthouse.Frontend` was edited. Written 2026-11-08 during DELIVER of slice A. The verdict block is the last
section of this record.

Method as decided in `feature-delta.md` (DESIGN "The audit", DISTILL "The audit verdict going to the maintainer"). The scripts and data that produced every
number below are in this folder and are named where used; a reader with the commit can redo them.

## 1. Population (scenario A-1)

- **Commit.** The Vitest listing ran with `HEAD` at `ced6e04acb72b5adda3fa87fe45e749e584e9f80`. `git diff --stat ced6e04ac HEAD -- Lighthouse.Frontend` is empty
  at the time of writing (other sessions have committed backend work since), so the population is the same at `HEAD`.
- **Listing command.** From `Lighthouse.Frontend`: `TZ=Europe/Zurich pnpm exec vitest list --json=<file>`. It collects the tests and runs none.
- **File list.** `git ls-files 'Lighthouse.Frontend/src/**/*.test.tsx'`, saved as `git-ls-files-tsx.txt`.

Output of `population.py` (`population.txt`), counts:

```
git ls-files .test.tsx: 410
vitest listing files .test.tsx: 410
vitest listing files total: 617  tests total: 7970
tsx tests in listing: 5392
in git ls-files, not in listing: []
in listing, not in git ls-files: []
```

**Difference between the listing and `git ls-files`: none.** The two sets of 410 files are equal, file by file (both difference lists are empty), so
nothing is named and the audit goes on.

One count differs from the brief and is explained here, not left open: the listing holds **5,392** `.tsx` tests and the brief and the timing CSV say **5,857**.
The CSV has one row per executed case, the listing one entry per test title, and a parametrised title (`it.each`, a loop) is one entry in the listing and
several rows in the CSV. The weights below use the listing's counts, so a parametrised title counts once. The 465 difference is not itemised.

### Groups, quotas recomputed from 410

Quota = 50 x group files / 410, rounded half up; the rounded quotas add to 50, so the largest group takes no correction.

| Group (path under `Lighthouse.Frontend/src`) | Files | Tests (listing) | 50 x files / 410 | Quota | Quota in DESIGN (408) |
|---|---|---|---|---|---|
| `components/Common/Charts` | 37 | 591 | 4.51 | 5 | 5 |
| `components/Common`, without `Charts` | 125 | 1,673 | 15.24 | 15 | 15 |
| `pages/Common/MetricsView` | 31 | 507 | 3.78 | 4 | 4 |
| `pages/Portfolios` | 54 | 542 | 6.59 | 7 | 7 |
| `pages/Common/Reports` and `pages/Reports` | 36 | 511 | 4.39 | 4 | 4 |
| `pages/Settings` | 28 | 350 | 3.41 | 3 | 3 |
| `pages/Teams` | 45 | 573 | 5.49 | 5 | 6 |
| the rest | 54 | 645 | 6.59 | 7 | 6 |
| **Total** | **410** | **5,392** | | **50** | 50 |

"The rest" is every file outside the seven named groups; it holds 54 files, two more than DESIGN's 52, which are the two files the per-area count of 408
missed. Teams moves from 6 to 5 and the rest from 6 to 7 because of the recount. Sample: 50 files, 682 tests.

## 2. The picks, and that they can be redone (scenario A-2)

Rule: sort the group's files by path; the i-th pick, for i = 1 to the quota, is the file at position ceil(i x files / quota), counting from 1. The full
list, in pick order, is `picks.json`; the table in section 4 lists the 50 files in that order.

Positions per group, computed by `population.py` at DELIVER time from the 410-file list (not hard-coded from 408):

| Group | Files | Quota | Positions |
|---|---|---|---|
| `components/Common/Charts` | 37 | 5 | 8, 15, 23, 30, 37 |
| `components/Common`, without `Charts` | 125 | 15 | 9, 17, 25, 34, 42, 50, 59, 67, 75, 84, 92, 100, 109, 117, 125 |
| `pages/Common/MetricsView` | 31 | 4 | 8, 16, 24, 31 |
| `pages/Portfolios` | 54 | 7 | 8, 16, 24, 31, 39, 47, 54 |
| `pages/Common/Reports` and `pages/Reports` | 36 | 4 | 9, 18, 27, 36 |
| `pages/Settings` | 28 | 3 | **10, 19, 28** |
| `pages/Teams` | 45 | 5 | 9, 18, 27, 36, 45 |
| the rest | 54 | 7 | 8, 16, 24, 31, 39, 47, 54 |

`pages/Settings` has 28 files with quota 3, so its positions are 10, 19 and 28, as expected. In every group the last pick is the group's last file
(`population.py` asserts it; the rightmost position equals the group's file count in every row above).

**The smallest and the largest group redone by hand**, with `redo_by_hand.sh` (shell tools only: `git ls-files`, `grep`, `LC_ALL=C sort`, `sed`, integer
arithmetic for the ceiling), output in `redo_by_hand.txt`:

```
pages/Settings: 28 files, quota 3
1 @10 pages/Settings/DemoData/DemoDataSettings.test.tsx
2 @19 pages/Settings/System/SystemSettingsTab.test.tsx
3 @28 pages/Settings/SystemInfo/ThirdPartyPackagesSection.test.tsx
components/Common without Charts: 125 files, quota 15
1 @9 components/Common/Connection/ModifyConnectionSettings.parentSources.test.tsx
2 @17 components/Common/Connections/OAuthPopupComplete.test.tsx
3 @25 components/Common/DataGrid/ColumnOrderDialog.test.tsx
4 @34 components/Common/DateRangeSelector/DateRangePresets.test.tsx
5 @42 components/Common/FeatureGrid/FeatureGrid.test.tsx
6 @50 components/Common/FeatureListDataGrid/columns.forecastedStart.beyondTheHorizon.test.tsx
7 @59 components/Common/FilterBar/FilterBar.test.tsx
8 @67 components/Common/FormSelectField/FormSelectField.test.tsx
9 @75 components/Common/OnboardingStepper/OnboardingStepper.licenseCheck.test.tsx
10 @84 components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx
11 @92 components/Common/QuickSettings/ThroughputQuickSetting.test.tsx
12 @100 components/Common/StateMappings/StateMappingsEditor.test.tsx
13 @109 components/Common/Team/ModifyTeamSettings.test.tsx
14 @117 components/Common/ValidationActions/SaveStateIndicator.test.tsx
15 @125 components/Common/WorkItemsDialog/WorkItemsDialog.test.tsx
```

Both lists match `picks.json` file for file and position for position (compare with the `population.txt` picks above). The first attempt of the hand redo for
Settings gave 27 files because a git pathspec with `**/` skips a file that sits directly in `pages/Settings/`; the script now lists the folder and filters
by suffix, and gives 28. That was a fault of the redo's pathspec, not of the pick list.

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

**Row 27 was relabelled after review.** `DeliverySection.overdue.test.tsx` first read all UI. Two of its tests state a result a rule decided (the word "overdue" for a past target, and the colour it is drawn in), so under the Mixed definition they are Mixed and the file's class is E1 (`labels_round2.tsv`, last row).

| # | File | Tests | Logic | UI | Mixed | E | Call | Note |
|---|---|---|---|---|---|---|---|---|
| 1 | components/Common/Charts/CumulativeStateTimeScopeControl.test.tsx | 8 | 0 | 5 | 3 | E1 | keep | the scope reset after an invalid pick is an effect; a pure validity check could come out |
| 2 | components/Common/Charts/DeliveryPredictabilityChart.beyondTheHorizon.test.tsx | 5 | 0 | 0 | 5 | E1 | keep | series and tooltip wording are built inside the component; the render sits in beforeEach |
| 3 | components/Common/Charts/LineRunChart.dialog.test.tsx | 1 | 0 | 0 | 1 | E1 | keep | one click on a mocked chart, then the age shown in the dialog |
| 4 | components/Common/Charts/StackedAreaChart.test.tsx | 7 | 0 | 5 | 2 | E1 | keep | series building (offset, sort) checked through the mocked chart's props |
| 5 | components/Common/Charts/WorkItemAgingChart.test.tsx | 72 | 17 | 26 | 29 | E1 | keep | computePaceBandRects is exported from the component file and has no own .test.ts of its own; paceBands.ts and sleRisk.ts do |
| 6 | components/Common/Connection/ModifyConnectionSettings.parentSources.test.tsx | 1 | 0 | 0 | 1 | E1 | keep | ordering rule proved through a click on one render |
| 7 | components/Common/Connections/OAuthPopupComplete.test.tsx | 3 | 0 | 0 | 3 | E1 | keep | postMessage and close are an effect on window.opener; the fallback text needs the DOM |
| 8 | components/Common/DataGrid/ColumnOrderDialog.test.tsx | 2 | 0 | 1 | 1 | E1 | keep | the reorder is computed in the component |
| 9 | components/Common/DateRangeSelector/DateRangePresets.test.tsx | 7 | 0 | 6 | 1 | E1 | keep | which window matches the dates is a rule; it is proved through the chips |
| 10 | components/Common/FeatureGrid/FeatureGrid.test.tsx | 14 | 0 | 12 | 2 | E1 | keep | filter and sort of the features happen inside the grid component |
| 11 | components/Common/FeatureListDataGrid/columns.forecastedStart.beyondTheHorizon.test.tsx | 2 | 1 | 1 | 0 | E1 | keep | the beyond-the-horizon wording comes out of createForecastedStartColumn |
| 12 | components/Common/FilterBar/FilterBar.test.tsx | 6 | 0 | 6 | 0 | - | keep | input wiring and keyboard listeners only |
| 13 | components/Common/FormSelectField/FormSelectField.test.tsx | 20 | 0 | 20 | 0 | - | keep | a MUI select wrapper: options, layout props, helper text |
| 14 | components/Common/OnboardingStepper/OnboardingStepper.licenseCheck.test.tsx | 3 | 0 | 1 | 2 | E1 | keep | which step blames the licence check is a rule shown as text |
| 15 | components/Common/ProjectSettings/ModifyProjectSettings.queryCommit.test.tsx | 6 | 0 | 0 | 6 | E2 | keep | the commit-on-leave rule sits in a component with the autosave and rbac contexts |
| 16 | components/Common/QuickSettings/ThroughputQuickSetting.test.tsx | 29 | 0 | 20 | 9 | E1 | keep | validation and tooltip qualifier wording come out as pure functions |
| 17 | components/Common/StateMappings/StateMappingsEditor.test.tsx | 17 | 0 | 13 | 4 | E1 | keep | the option-filter rules sit in the editor |
| 18 | components/Common/Team/ModifyTeamSettings.test.tsx | 30 | 0 | 10 | 20 | E2 | keep | form-level autosave and mapping validation sit in the component with contexts |
| 19 | components/Common/ValidationActions/SaveStateIndicator.test.tsx | 4 | 0 | 4 | 0 | - | keep | copy per save state |
| 20 | components/Common/WorkItemsDialog/WorkItemsDialog.test.tsx | 82 | 0 | 42 | 40 | E1 | keep | row colouring, ordering and tooltip rules sit in the dialog; paceBands.ts and sleRisk.ts have their own .test.ts |
| 21 | pages/Common/MetricsView/DashboardHeader.test.tsx | 13 | 0 | 11 | 2 | E1 | keep | date formatting and the committed-window fallback sit in the header |
| 22 | pages/Common/MetricsView/PredictabilityScoreDetailsWidget.loading.test.tsx | 9 | 0 | 3 | 6 | E2 | keep | the fetch-and-status flow is a hook with a query client; assertions are on calls and status, not on the DOM |
| 23 | pages/Common/MetricsView/TotalThroughputWidget.test.tsx | 5 | 1 | 3 | 1 | E1 | keep | one-decimal average is formatted in the widget |
| 24 | pages/Common/MetricsView/useDateRange.test.tsx | 37 | 37 | 0 | 0 | E0 | keep | a hook driven with renderHook and no DOM query; only the .tsx suffix and a router wrapper keep it here |
| 25 | pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.loadsOnce.test.tsx | 17 | 0 | 0 | 17 | E2 | keep | counts of asks per opening are a loading flow in a dialog with licence and terminology contexts |
| 26 | pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryNotesPanel.archived.test.tsx | 7 | 0 | 3 | 4 | E1 | keep | deliveryArchivedRefusal.ts exists and has no .test.ts of its own |
| 27 | pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.overdue.test.tsx | 6 | 0 | 4 | 2 | E1 | keep | overdue word, colour and tooltip |
| 28 | pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySourceTab.picker.test.tsx | 6 | 0 | 5 | 1 | E1 | keep | the picker's narrowing rule sits in the tab |
| 29 | pages/Portfolios/Detail/Components/DeliveryGrid/timeline/DeliveryGanttChart.test.tsx | 2 | 0 | 2 | 0 | - | keep | mount smoke |
| 30 | pages/Portfolios/Detail/PortfolioDetail.reportsTab.test.tsx | 4 | 0 | 4 | 0 | - | keep | tab order and routing |
| 31 | pages/Portfolios/Edit/EditPortfolio.test.tsx | 10 | 0 | 8 | 2 | E1 | keep | the settings loaded into the form (stored or a copy of a clone source) are prepared in the page |
| 32 | pages/Common/Reports/ReportsTab.exportPdf.test.tsx | 14 | 0 | 13 | 1 | E1 | keep | row actions, focus and print frame wiring |
| 33 | pages/Common/Reports/print/browserPrintEffects.test.tsx | 20 | 0 | 20 | 0 | - | keep | wrappers over window, iframe, style sheets and fonts: they need the DOM, although the file holds no JSX |
| 34 | pages/Reports/BulkReportWizard.test.tsx | 37 | 0 | 24 | 13 | E1 | keep | who may press, the 50 limit and the name fallbacks are rules inside the wizard |
| 35 | pages/Reports/ReportsPage.usageData.test.tsx | 10 | 0 | 0 | 10 | E2 | keep | which usage event is sent sits in the page with its contexts |
| 36 | pages/Settings/DemoData/DemoDataSettings.test.tsx | 8 | 0 | 8 | 0 | - | keep | titles, alerts, confirm dialog |
| 37 | pages/Settings/System/SystemSettingsTab.test.tsx | 8 | 0 | 8 | 0 | - | keep | feature toggles and sections |
| 38 | pages/Settings/SystemInfo/ThirdPartyPackagesSection.test.tsx | 10 | 0 | 2 | 8 | E1 | keep | reading SPDX and CycloneDX data into rows, filtering by scope and building npm links sit in the section |
| 39 | pages/Teams/Detail/Refinement/RefinementView.enoughFor.test.tsx | 16 | 0 | 6 | 10 | E1 | keep | how many rows are needed and where the line falls is a rule inside the view |
| 40 | pages/Teams/Detail/Refinement/VoteControl.test.tsx | 5 | 0 | 5 | 0 | - | keep | vote buttons and a hint |
| 41 | pages/Teams/Detail/TeamDetail.reportsTab.test.tsx | 5 | 0 | 5 | 0 | - | keep | tab order and routing |
| 42 | pages/Teams/Detail/TeamForecastView.realityCheck.grading.test.tsx | 6 | 0 | 0 | 6 | E1 | keep | the grade shade and the percentage wording come out of the view as a function |
| 43 | pages/Teams/Edit/ForecastSettingsComponent.test.tsx | 21 | 0 | 15 | 6 | E1 | keep | the rule-set JSON round-trip is a serialise and parse pair inside the component |
| 44 | components/App/Header/Header.test.tsx | 13 | 0 | 13 | 0 | - | keep | header controls |
| 45 | components/App/LetPeopleWork/LighthouseVersion.test.tsx | 13 | 0 | 12 | 1 | E1 | keep | the update-available and admin-only decisions sit in the component |
| 46 | components/SurveyNudge/SurveyNudge.test.tsx | 3 | 0 | 3 | 0 | E1 | keep | the two-week eligibility rule sits in the component |
| 47 | hooks/UsageDataConsentProvider.test.tsx | 3 | 0 | 0 | 3 | E2 | keep | a context provider: sharing one answer between parts of the screen needs the provider |
| 48 | hooks/useUsageDataConsent.test.tsx | 21 | 21 | 0 | 0 | E0 | keep | a hook driven with renderHook and no DOM query; only the .tsx suffix keeps it here |
| 49 | pages/Connections/Edit/EditConnection.test.tsx | 15 | 0 | 15 | 0 | - | keep | page wiring to services, titles and the RBAC guard |
| 50 | utils/charts/scatterMarkerUtils.test.tsx | 19 | 9 | 10 | 0 | E0 | keep | getBubbleSize and getMarkerColor are pure functions already tested directly; the render helpers need the DOM |
| | Sum | 682 | 86 | 374 | 222 | | |
files: 50 mismatches: 0

## 4. Second rater and agreement (scenario A-4)

**Who.** The first rater is this crafter agent (model `sonnet`, the profile's `agent_model`). The second rater is a separate dispatch of a general-purpose agent on model
`haiku` (the profile's `reviewer_model`), given only the labelling rule, the ten files' paths and their test titles (`rater2-input.md`), and told not to open any label or
audit file. The two models differ. The second rater read the same 10 files: the 5th, 10th, ... 50th picks, 242 tests.

**Agreement** is the unrounded share of tests given the same label. **80 % means at least 0.80.** Cohen's kappa is reported and does not gate.

### Round 1, labelling rule as written in the design

`agreement-round1.txt` (script `agreement.py`, the second rater's labels in `rater2-labels-round1.tsv`):

```
agreement 179/242 = 0.739669; kappa 0.5816
```

0.739669 is below 0.80, so the first rater's labels did not stand and the redo path ran. The 63 disagreeing tests are listed with their titles in
`disagreements-round1.md` (file, test number, first rater, second rater, title). They fall in three kinds: the first rater's Logic against the second rater's Mixed
(26 tests), Mixed against UI (23) and UI against Mixed (11), with 3 Logic against UI.

### The wording change

Only the labelling rule's wording (the section "What is decided per test" in `feature-delta.md`) was changed, in one paragraph headed "Tightened after the first agreement
round", with disagreeing tests as its examples. The verdict rule and the band wording were not touched: `rule_diff.sh` (section 5) prints nothing.

### The redo, one round

Both raters redid the 10 files with the tightened wording; the second rater was again blind to all earlier labels. The first rater also re-read, in the other 40 files, every
test of the kinds that had disagreed: every Logic test that mounts a component (now Mixed) and every Mixed test that states only presence (now UI). The rows changed are in
`labels_round2.tsv`.

`agreement-round2.txt`, with the 68 disagreeing tests listed with titles in `disagreements-round2.md`:

```
agreement 174/242 = 0.719008; kappa 0.5272
confusion (first, second): {('U','U'): 55, ('U','M'): 56, ('M','U'): 12, ('M','M'): 93, ('L','L'): 26}
```

**0.719008 is below 0.80 after the one round. No second round is run. The headline is marked "low confidence".**

- **What the second rater saw.** Of the 86 Logic tests in the sample, **26 were rated by both raters and all 26 agree**. The other 60 were never seen by the second rater, among them 58 of the
  67 tests that make up Low (picks 24 and 48, the two hook files; the other 9 are in pick 50). **Low rests mostly on tests only the first rater labelled.**
- **The tightening narrowed what counts as Logic.** The added wording says a test that mounts a component is never Logic. That is a categorical exclusion, made within the one agreement
  round the design allows. On the labels as first written (`labels-round1.tsv`) Middle was 17.9 % (interval 6.4 % to 29.7 %), on the final labels it is 12.6 % (2.5 % to 23.9 %); Low is 9.8 % on both,
  High 47.8 % against 45.2 % (`bootstrap-output-round1.txt` against `bootstrap-output.txt`). The band is 4 on both, because even the larger Middle is far below N.
- **The largest remaining kind was not re-read.** 56 of the 68 round-2 disagreements are first rater UI against second rater Mixed. The design asks the first rater to re-read, in the other 40
  files, every test of a kind that disagreed; that was done for the Logic-against-Mixed kind and for Mixed tests that state only presence, and **not for the UI tests that state a value a rule decided**.
  High (which takes the Mixed tests) is therefore likely understated. It is unlikely to lift the band: High's lower end is 33.9 % and N is 83.8 %, so a re-read would have to turn most of the UI tests (over half of all sampled tests) into Mixed.
  Low and Middle do not depend on this kind.

## 5. The estimate and the band (scenario A-5)

**Statistic** (the design's, unchanged): each share is weighted by test count and pooled over the sample. **Low** = logic tests in E0 files / all sampled tests. **Middle** = logic
tests in E0 and E1 files, the headline. **High** = logic plus mixed tests in files of every class. Low <= Middle <= High by construction. The comparison uses the **lower end** of
each 95 % interval; the point and the upper end are reported beside it.

**N, the needed share, and its source.** Frontend alone, N = 4,907 of 5,857 = **0.8378** (about 84 %), the design's figure from the brief's counts: 90 % of 9,505 tests is 8,554.5, of
which 3,648 are already fast, leaving 4,907 of the 5,857 `.tsx` tests to move. The 5,857 is also the number of `.test.tsx` rows in the CSV of the run named in section 6, but the brief's 9,505 total is its dated figure and not the current one:
that CSV has 9,530 rows, of which 3,673 are not `.tsx`, so on today's rows N = (8,577 - 3,673) / 5,857 = 0.8373. The bases are mixed (the brief's counts, the CSV's rows, and the sample's weights, which are listing titles
with a parametrised title counted once); the band is the same under each. No backend labels exist yet (T6 has not labelled them), so N is for the frontend alone and the whole-suite N is not used. As a sensitivity, N computed the
same way from today's listing (fast tier = the 2,578 non-`.tsx` tests of 7,970, 90 % = 7,173, so 4,595 of 5,392 must move) is 0.852; the band below is 4 under that N too.

**Method line.** Percentile bootstrap: files resampled with replacement within their group, 10,000 resamples, seed 6538, 2.5th and 97.5th percentiles (linear interpolation between order
statistics), test-count weighted pooled ratio (`bootstrap.py`, output as printed):

```
files 50, tests 682
method: percentile bootstrap, files resampled within group, 10000 resamples, seed 6538, 2.5th and 97.5th percentiles, test-count weighted pooled ratio
N used: 0.8378
| Share | Point | Lower end (2.5th) | Upper end (97.5th) |
|---|---|---|---|
| Low | 9.8 % | 0.0 % | 21.7 % |
| Middle | 12.6 % | 2.5 % | 23.9 % |
| High | 45.2 % | 33.9 % | 54.7 % |
band 4 (Low lower 0.0 %, Middle lower 2.5 %, High lower 33.9 %, against N 83.8 %)
```

Band: the lower end of High (33.9 %) is below N (83.8 %), so **band 4**: moving logic cannot reach 90 %.

Three sensitivity runs, printed by the same script (`bootstrap.py`), none changes the band:

- With the second rater's labels in place of the first rater's for the ten files they read (`labels-second-rater-on-its-files.tsv`): Low 9.8 % (lower end 0.0 %), Middle 12.6 % (lower end
  2.5 %), High 51.6 % (lower end 37.9 %, upper end 62.5 %). Band 4.
- With N = 0.852 (above), or 0.8373 (the current CSV total of 9,530 rows): lower ends unchanged, band 4.
- With the labels as first written, before the tightened wording (`labels-round1.tsv`, `bootstrap-output-round1.txt`): Low 9.8 % (0.0 % to 21.7 %), Middle 17.9 % (6.4 % to 29.7 %), High 47.8 % (37.0 % to 57.3 %). Band 4.

**The verdict rule and the band wording were not edited after a number was seen.** The verdict rule's text (the Statistic, the N definition and the four-band table, from "Statistic" to
the line before "Which model does what") is compared with the DESIGN commit `3ad54cfa0`, and the band wording table with the DISTILL commit `f249408df` (`rule_diff.sh`); each diff is
empty, and the line counts printed show that the text was found (18 and 7):


```
--- verdict rule text: current vs 3ad54cfa0 (empty = same)
18
--- band wording: current vs f249408df (empty = same)
7
--- end
```

## 6. Seconds, and the ten heaviest files (scenario A-6)

**Run.** `main` run **42457232387** of "Build And Deploy Lighthouse", 2026-11-08, head `cc4d4ed60`, status completed, conclusion **success** (all 20 jobs success, including Verify
Frontend). Its artifact `test-timings-frontend` was downloaded with `gh run download 42457232387 --repo LetPeopleWork/Lighthouse -n test-timings-frontend` and expires 2027-02-06. The
run named in the DEVOPS wave, 42372930241, also holds the artifact but its conclusion is empty (it waits for the deploy approval), so it is not "green" and was not used.

The CSV has 9,530 rows over 618 files; 410 of them are `.test.tsx` files with 5,857 rows. Seconds are the sum of the durations of a file's tests. The CSV expires, so the ten rows are pasted:

```
rows: 9530  files: 618  tsx files: 410
total seconds, all tests: 548.0; .test.tsx tests: 521.6

ten files with the most seconds (sum of their tests' durations):
| Rank | File | Tests | Seconds |
|---|---|---|---|
| 1 | src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx | 133 | 10.73 |
| 2 | src/components/Common/Team/ModifyTeamSettings.test.tsx | 36 | 10.60 |
| 3 | src/pages/Reports/ReportsPage.combinedPdf.test.tsx | 18 | 9.95 |
| 4 | src/pages/Overview/OverviewDashboard.test.tsx | 51 | 9.84 |
| 5 | src/pages/Reports/ReportsPage.removal.test.tsx | 21 | 9.71 |
| 6 | src/pages/Connections/Edit/EditConnection.fieldChange.test.tsx | 31 | 9.65 |
| 7 | src/components/Common/DataGrid/DataGridBase.paging.test.tsx | 28 | 9.18 |
| 8 | src/pages/Common/Reports/ReportsTab.createWizard.test.tsx | 64 | 8.86 |
| 9 | src/components/Common/ProjectSettings/ModifyProjectSettings.test.tsx | 35 | 8.38 |
| 10 | src/pages/Common/Reports/ReportsTab.test.tsx | 47 | 8.20 |

sample seconds matched by name: 666 of 682 tests (unmatched: parametrised or renamed titles, 16)
sample seconds, matched tests: 61.27
  logic: 1.15 s over 85 matched tests
  mixed: 33.82 s over 215 matched tests
  UI: 26.31 s over 366 matched tests
seconds in the Low tests (logic in E0 files): 1.13 s (1.9 % of the sample's matched seconds)
seconds in the Middle tests (logic in E0 and E1 files): 1.15 s (1.9 %)
seconds in the High tests (logic plus mixed): 34.97 s (57.1 %)
top-ten files that the pick rule also chose: ['components/Common/Team/ModifyTeamSettings.test.tsx']
```

**None of the ten enters the estimate unless the pick rule chose it.** One was chosen by the rule: `components/Common/Team/ModifyTeamSettings.test.tsx` (pick 18, group `components/Common`
without `Charts`, position 109). It is in the sample because the rule chose it, not because it is slow. The other nine are not in the sample. They say where a rewrite would pay first and
decide nothing here.

**Seconds in the sample's tests**, from the same CSV, matched to the sampled tests by their full title (666 of the 682 matched; the other 16 are parametrised titles that the CSV
names per case). Seconds are reported beside the shares because a share moved is not seconds saved:

| Tests | Seconds | Share of the matched seconds |
|---|---|---|
| Low (logic in E0 files) | 1.13 | 1.9 % |
| Middle (logic in E0 and E1 files) | 1.15 | 1.9 % |
| High (logic plus mixed) | 34.97 | 57.1 % |
| All matched sample tests | 61.27 | 100 % |

Per test, on the matched tests, the logic tests took about 13 ms on average (1.15 s over 85), the mixed tests about 157 ms (33.82 s over 215) and the UI tests about 72 ms (26.31 s over
366). So the logic tests are the cheap ones: moving them saves little time even where the share is large. Time stays the rule.

## 7. No rewrite or move is planned (scenario A-7)

Search of the roadmap file in the paths this slice changes (`docs/feature/story-6538-t11-test-writing-guidance/deliver/roadmap.json`; no other roadmap is in the slice's paths) for the words
`rewrite` and `move`, `grep -o -i -E '.{70}(rewrite|move).{60}' deliver/roadmap.json`:

```
earch of the roadmap files in the slice's changed paths for the words rewrite and move prints no step to rewrite or move a test (output p
 never edited after a number is seen (STOPPED block if they were). No rewrite is planned under any band. The audit record is written to d
on in the Wave section) with T5's bullet cut to the rule and its body moved into backend.md, and the ledger pointer in docs/ci-learnings
```

Three hits, none a step that rewrites or moves a test: the first is the text of this search's own criterion, the second says "No rewrite is planned under any band", the third is the cut of a
`CLAUDE.md` bullet into a document. This slice's roadmap was not edited by the audit.

## Verdict block (goes to the maintainer; the last section of this record)

**LOW CONFIDENCE.** Agreement between the two raters stayed below 0.80 after the one round (0.719008, kappa 0.5272), so the headline carries a low-confidence mark. Read it with these limits:

- The raters agree on the 26 Logic tests they both rated, which are 26 of the 86 in the sample. **Low rests mostly on tests only the first rater labelled** (58 of its 67 tests).
- The tightened wording narrowed what counts as Logic (a test that mounts a component is never Logic). On the labels as first written Middle was 17.9 %, on the final labels 12.6 %. The band is 4 on both.
- The UI-against-Mixed kind (56 of the 68 round-2 disagreements) was **not re-read** in the other 40 files, so **High is likely understated**. It is unlikely to reach N: that needs most UI tests to turn Mixed.

| Field | Content |
|---|---|
| Commit, file and test counts | Listing run at `ced6e04acb72b5adda3fa87fe45e749e584e9f80`; 410 `.test.tsx` files by `git ls-files` and by the Vitest listing, no difference to name; 5,392 tests in the listing (the CSV's 5,857 counts parametrised cases one by one). Sample: 50 files, 682 tests |
| N used and where it came from | 0.8378 (4,907 of 5,857 `.tsx` tests must move for the fast tier to reach 90 %), the design's figure from the brief's counts, frontend alone because the backend has no labels yet. It mixes bases: the brief's total is 9,505, the current CSV has 9,530 rows (N = 0.8373 on that total), and the sample's weights are listing titles. Sensitivity: 0.852 from today's listing. The band is the same under all three |
| Low, Middle, High | Test-count weighted, percentile bootstrap, files resampled within group, 10,000 resamples, seed 6538, 2.5th and 97.5th percentiles. **Low** 9.8 %, interval 0.0 % to 21.7 %. **Middle (headline)** 12.6 %, interval 2.5 % to 23.9 %. **High** 45.2 %, interval 33.9 % to 54.7 %. **Low includes the E0 reading** (tests that already call a hook or function directly, 67 tests in three files); without that reading Low is 0 %, and Middle and the band do not change. On the labels as first written (before the tightened wording): Low 9.8 % (0.0 % to 21.7 %), Middle 17.9 % (6.4 % to 29.7 %), High 47.8 % (37.0 % to 57.3 %), also band 4. With the second rater's labels for its ten files: High 51.6 % (37.9 % to 62.5 %) |
| Seconds | Run 42457232387 (green `main`, 2026-11-08), 666 of 682 sample tests matched: the Low tests took 1.13 s, the Middle tests 1.15 s, the High tests 34.97 s, out of 61.27 s. The logic tests are the cheap ones (about 13 ms each against about 157 ms for a mixed test), so a large share moved saves little time |
| Agreement and confidence | Round 1: 179 of 242 = 0.739669 (kappa 0.5816). Round 2, after the tightened wording: 174 of 242 = 0.719008 (kappa 0.5272). Low confidence, marked above |
| Band and verdict | **Band 4**: the lower end of High (33.9 %) is below N (83.8 %). The verdict rule and the band wording are unchanged since the DESIGN and DISTILL commits (empty diffs, section 5) |
| What the maintainer decides | Whether to plan any rewrite. No rewrite is planned in this Story under any band |

Wording sent to the maintainer for band 4: "Moving logic cannot reach 90 %. Keep the shares as a direction and let time stay the rule."

Nothing was edited in `Lighthouse.Frontend`; the call column of every row reads *keep*.
