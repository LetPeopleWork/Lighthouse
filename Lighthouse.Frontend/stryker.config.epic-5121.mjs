// @ts-check
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "command",
	commandRunner: {
		command:
			"pnpm exec vitest run --config vitest.stryker.epic-5121.config.ts --reporter=dot",
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
		"src/hooks/useModifySettings.ts:51-77",
		"src/hooks/useModifySettings.ts:169-285",
		"src/components/Common/ValidationActions/SaveStateIndicator.tsx:34-52",
		"src/components/Common/Connection/ModifyConnectionSettings.tsx:46-67",
		"src/components/Common/Connection/ModifyConnectionSettings.tsx:424-436",
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
	tempDirName: ".stryker-tmp-epic-5121",
	cleanTempDir: true,
};

export default config;
