// @ts-check
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "command",
	commandRunner: {
		command:
			"pnpm exec vitest run --config vitest.stryker.cumulative-state-time-completion-filter.config.ts --reporter=dot",
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
		"src/components/Common/Charts/CumulativeStateTimeChart.tsx:28-29",
		"src/components/Common/Charts/CumulativeStateTimeChart.tsx:91-102",
		"src/components/Common/Charts/CumulativeStateTimeChart.tsx:178-184",
		"src/components/Common/Charts/CumulativeStateTimeChart.tsx:202",
		"src/components/Common/Charts/CumulativeStateTimeChart.tsx:207-208",
		"src/components/Common/Charts/CumulativeStateTimeChart.tsx:213-214",
		"src/pages/Common/MetricsView/BaseMetricsView.tsx:927-933",
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
	tempDirName: ".stryker-tmp-cumulative-state-time-completion-filter",
	cleanTempDir: true,
};

export default config;
