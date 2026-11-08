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
| 27 | pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.overdue.test.tsx | 6 | 0 | 6 | 0 | - | keep | overdue word, colour and tooltip |
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
| | Sum | 682 | 86 | 376 | 220 | | |
files: 50 mismatches: 0
