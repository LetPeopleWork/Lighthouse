// @ts-check
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "command",
	commandRunner: {
		command:
			"pnpm exec vitest run --config vitest.stryker.time-in-state.config.ts --reporter=dot",
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
		"src/utils/staleness/deriveStaleness.ts",
		"src/pages/Common/MetricsView/StaleOverviewWidget.tsx",
		"src/components/Common/TimeInStateBadge/TimeInStateBadge.tsx",
		"src/pages/Common/MetricsView/ragRules.ts:77-104",
		"src/pages/Common/MetricsView/ragRules.ts:427-446",
		"src/utils/charts/scatterMarkerUtils.tsx:85-96",
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
	tempDirName: ".stryker-tmp-time-in-state",
	cleanTempDir: true,
};

export default config;
