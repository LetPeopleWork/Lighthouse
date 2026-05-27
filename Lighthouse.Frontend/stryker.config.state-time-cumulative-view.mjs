// @ts-check
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "command",
	commandRunner: {
		command:
			"pnpm exec vitest run --config vitest.stryker.state-time-cumulative-view.config.ts --reporter=dot",
	},
	checkers: [],
	reporters: ["progress", "clear-text", "html", "json"],
	coverageAnalysis: "off",
	concurrency: 1,
	timeoutMS: 60000,
	timeoutFactor: 2,
	disableTypeChecks: true,
	thresholds: { high: 80, low: 70, break: 0 },
	mutate: [
		"src/components/Common/Charts/CumulativeStateTimeChart.tsx:30-230",
		"src/components/Common/Charts/CumulativeStateTimeItemPicker.tsx:25-135",
		"src/utils/date/formatDuration.ts:18-49",
		"src/pages/Common/MetricsView/ragRules.ts:852-892",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:1139-1168",
	],
	ignorePatterns: [
		"src-tauri",
		"publish",
		"dist",
		"coverage",
		"playwright-report",
		"test-results",
		"sonar-report.xml",
		"StrykerOutput",
	],
	tempDirName: ".stryker-tmp-state-time-cumulative-view",
	cleanTempDir: true,
};

export default config;
