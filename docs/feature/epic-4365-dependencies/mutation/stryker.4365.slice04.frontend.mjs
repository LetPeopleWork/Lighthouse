export default {
	packageManager: "pnpm",
	testRunner: "vitest",
	plugins: ["@stryker-mutator/vitest-runner"],
	vitest: { configFile: "vitest.stryker.slice04.ts" },
	reporters: ["clear-text", "progress", "json"],
	coverageAnalysis: "off",
	concurrency: 2,
	timeoutMS: 120000,
	inPlace: true,
	disableTypeChecks: false,
	ignorePatterns: ["dist", "coverage", "playwright-report", "reports"],
	// The decisions this slice added, not the form around them. The settings component is mutated whole
	// because every line of it is a decision somebody acts on; the two grid files are scoped to the
	// regions that learned about a dependency being set aside, because mutating them whole scores the
	// suite slice 02 already wrote rather than anything this slice changed.
	mutate: [
		"src/models/FeatureDependency.ts",
		"src/utils/dependencies/dependencySentences.ts",
		"src/components/Common/ProjectSettings/Advanced/DependenciesComponent.tsx",
		"src/components/Common/FeatureListDataGrid/WarningsIndicator.tsx:30-50",
		"src/components/Common/FeatureListDataGrid/WarningsIndicator.tsx:112-152",
		"src/components/Common/FeatureListDataGrid/columns.tsx:118-124",
		"src/components/Common/FeatureListDataGrid/columns.tsx:151-193",
	],
	thresholds: { high: 90, low: 80, break: 0 },
	jsonReporter: { fileName: "stryker-4365-slice04-frontend.json" },
	tempDirName: ".stryker-tmp-4365-slice04",
};
