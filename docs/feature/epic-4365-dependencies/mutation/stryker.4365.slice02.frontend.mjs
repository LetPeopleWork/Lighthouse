export default {
	packageManager: "pnpm",
	testRunner: "vitest",
	plugins: ["@stryker-mutator/vitest-runner"],
	vitest: { configFile: "vitest.stryker.slice02.ts" },
	reporters: ["clear-text", "progress", "json"],
	coverageAnalysis: "off",
	concurrency: 2,
	timeoutMS: 120000,
	inPlace: true,
	disableTypeChecks: false,
	ignorePatterns: ["dist", "coverage", "playwright-report", "reports"],
	// The decisions, not the decoration. What is left out is `sx` objects, `size` props and the block
	// that works out React keys - mutating those yields survivors nothing can kill from the outside,
	// because a key never reaches the DOM and a style prop can only be pinned by asserting how the
	// component looks. The words themselves are covered whole: every one of them is behaviour a reader
	// acts on.
	mutate: [
		"src/utils/dependencies/dependencySentences.ts",
		"src/components/Common/FeatureListDataGrid/WarningsIndicator.tsx:31-50",
		"src/components/Common/FeatureListDataGrid/WarningsIndicator.tsx:125-152",
		"src/components/Common/FeatureListDataGrid/columns.tsx:147-195",
	],
	thresholds: { high: 90, low: 80, break: 0 },
	jsonReporter: { fileName: "stryker-4365-slice02-frontend.json" },
	tempDirName: ".stryker-tmp-4365-slice02",
};
