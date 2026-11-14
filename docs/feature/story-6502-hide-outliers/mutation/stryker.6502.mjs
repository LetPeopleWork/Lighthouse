// @ts-nocheck
/** Mutation run for story 6502, hide outliers. Copy next to package.json to run. */
const C = "src/components/Common/Charts/";
const config = {
	packageManager: "pnpm",
	testRunner: "vitest",
	plugins: ["@stryker-mutator/vitest-runner"],
	vitest: { configFile: "vitest.stryker.6502.config.ts", related: false },
	reporters: ["clear-text", "progress", "json"],
	coverageAnalysis: "perTest",
	concurrency: 2,
	timeoutMS: 120000,
	inPlace: true,
	disableTypeChecks: false,
	incremental: true,
	incrementalFile: "reports/mutation/stryker-incremental-6502.json",
	ignorePatterns: ["dist", "coverage", "playwright-report", "/reports"],
	mutate: [
		"src/utils/charts/outliers.ts",
		"src/utils/charts/offScaleLines.ts",
		"src/utils/charts/outlierWording.ts",
		`${C}OutlierChip.tsx`,
		`${C}OffScaleLineMarkers.tsx`,
		`${C}outlierHeaderChip.tsx`,
		`${C}outlierPlot.ts`,
		`${C}widgetHeaderSlot.ts`,
	],
	thresholds: { high: 90, low: 80, break: 80 },
	jsonReporter: { fileName: "stryker-6502-frontend.json" },
	tempDirName: ".stryker-tmp-6502",
};
export default config;
