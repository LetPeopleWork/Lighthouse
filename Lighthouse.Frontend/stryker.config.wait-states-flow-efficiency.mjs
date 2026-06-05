// @ts-check
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "command",
	commandRunner: {
		command:
			"pnpm exec vitest run --config vitest.stryker.wait-states-flow-efficiency.config.ts --reporter=dot",
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
		"src/utils/flowEfficiency.ts",
		"src/pages/Common/MetricsView/ragRules.ts:855-882",
		"src/pages/Common/MetricsView/FlowEfficiencyOverviewWidget.tsx:89-138",
		"src/components/Common/StateMappings/WaitStatesEditor.tsx",
		"src/components/Common/Charts/CumulativeStateTimeChart.tsx:33-470",
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
	tempDirName: ".stryker-tmp-wait-states-flow-efficiency",
	cleanTempDir: true,
};

export default config;
