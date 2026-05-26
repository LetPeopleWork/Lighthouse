// @ts-check
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "command",
	commandRunner: {
		command:
			"pnpm exec vitest run --config vitest.stryker.aging-pace-percentiles.config.ts --reporter=dot",
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
		"src/components/Common/Charts/WorkItemAgingChart.tsx:39",
		"src/components/Common/Charts/WorkItemAgingChart.tsx:57-129",
		"src/components/Common/Charts/PercentileLegend.tsx:20",
		"src/components/Common/Charts/PercentileLegend.tsx:93-117",
		"src/services/Api/MetricsService.ts:271-282",
		"src/hooks/useMetricsData.ts:216-221",
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
	tempDirName: ".stryker-tmp-aging-pace-percentiles",
	cleanTempDir: true,
};

export default config;
