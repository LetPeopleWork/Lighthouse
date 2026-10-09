// @ts-nocheck
/**
 * Mutation run for story 6249, chart loading indicators.
 *
 * BaseMetricsView.tsx is 2000 lines and the story changed about 150 of them, so it is mutated by the
 * story's line ranges only: whole-file it would score the pre-existing view, not this change, and
 * every survivor costs a full run of the included specs. StrykerJS honours one `start-end` span per
 * entry; a single line must still be written `N-N` or it is silently dropped.
 *
 * @type {import('@stryker-mutator/api/core').PartialStrykerOptions}
 */
const config = {
	packageManager: "pnpm",
	testRunner: "vitest",
	plugins: ["@stryker-mutator/vitest-runner"],
	vitest: {
		configFile: "vitest.stryker.6249.config.ts",
		related: false,
	},
	reporters: ["clear-text", "progress", "json"],
	coverageAnalysis: "off",
	concurrency: 6,
	timeoutMS: 120000,
	inPlace: true,
	disableTypeChecks: false,
	ignorePatterns: ["dist", "coverage", "playwright-report", "reports"],
	mutate: [
		"src/hooks/useMetricsData.ts",
		"src/components/Common/Charts/TotalWorkItemAgeWidget.tsx",
		"src/pages/Common/MetricsView/widgetStatus.ts",
		"src/pages/Common/MetricsView/WidgetShell.tsx",
		"src/pages/Common/MetricsView/useFilteredView.ts",
		"src/pages/Common/MetricsView/useCumulativeStateTimeChoices.ts",
		"src/pages/Common/MetricsView/usePbcOverTime.ts",
		"src/pages/Common/MetricsView/usePercentilesOverTime.ts",
		"src/pages/Common/MetricsView/PbcOverTimeWidget.tsx",
		"src/pages/Common/MetricsView/PercentilesOverTimeWidget.tsx",
		"src/pages/Common/MetricsView/ThroughputRunChartCard.tsx",
		"src/pages/Common/MetricsView/PredictabilityScoreDetailsWidget.tsx",
		"src/pages/Common/MetricsView/PredictabilityScoreOverviewWidget.tsx",
		"src/pages/Common/MetricsView/FlowEfficiencyOverviewWidget.tsx",
		"src/pages/Common/MetricsView/categoryMetadata.ts",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:34-38",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:143-146",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:153-157",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:862-862",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:871-871",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:934-954",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1026-1026",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1032-1035",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1064-1064",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1087-1087",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1206-1206",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1214-1214",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1259-1259",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1264-1306",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1354-1359",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1362-1362",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1369-1374",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1410-1411",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1414-1415",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1422-1425",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1558-1569",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1571-1571",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1756-1757",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1772-1772",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1946-1946",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1958-1958",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1960-1960",
	],
	thresholds: { high: 90, low: 80, break: 0 },
	jsonReporter: { fileName: "stryker-6249-frontend.json" },
	tempDirName: ".stryker-tmp-6249",
};

export default config;
