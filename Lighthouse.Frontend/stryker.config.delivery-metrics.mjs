// @ts-check
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "command",
	commandRunner: {
		command:
			"pnpm exec vitest run --config vitest.stryker.delivery-metrics.config.ts --reporter=dot",
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
		"src/models/Delivery/DeliveryMetricsHistory.ts",
		"src/components/Common/Charts/DeliveryBurnupChart.tsx:15-95",
		"src/services/Api/DeliveryService.ts:137-144",
		"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx:81-105",
		"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx:447-554",
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
	tempDirName: ".stryker-tmp-delivery-metrics",
	cleanTempDir: true,
};

export default config;
