// @ts-check
// Project-level Stryker config — covers the rbac-enhancements feature targets.
// Uses Stryker's built-in "command" runner to spawn vitest as a subprocess per
// mutant. The vitest-runner plugin OOMs on this codebase during dry-run module-
// graph instrumentation (>8 GB heap); the command runner sidesteps that path
// entirely by treating vitest as an opaque shell command.
//
// Trade-off: command runner runs the included tests for every mutant (no per-test
// coverage filtering). With the include list limited to 2 test files in
// vitest.stryker.config.ts this is still fast.
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "command",
	commandRunner: {
		command:
			"pnpm exec vitest run --config vitest.stryker.config.ts --reporter=dot",
	},
	checkers: [],
	reporters: ["progress", "clear-text", "html", "json"],
	// command runner supports only "all" or "off".
	coverageAnalysis: "off",
	concurrency: 1,
	timeoutMS: 60000,
	timeoutFactor: 2,
	disableTypeChecks: true,
	thresholds: { high: 80, low: 70, break: 0 },
	// High-value subset for rbac-enhancements per the per-feature strategy:
	// ScopedGroupMappingManager (ADR-002 fix logic) and RbacService (HTTP contract).
	// Gating-only files (Settings.tsx, SystemSettingsTab.tsx, OverviewDashboard.tsx,
	// TeamDetail.tsx, PortfolioDetail.tsx, PortfolioDeliveryView.tsx,
	// DeliverySection.tsx) are shallow conditional renders driven by useRbac()
	// and are downscoped — their mutation value is bounded.
	mutate: [
		"src/components/Common/Authorization/ScopedGroupMappingManager.tsx",
		"src/services/Api/RbacService.ts",
		"src/hooks/useRbacGate.ts",
	],
	// Critical: src-tauri (~25 GB Tauri build artifacts), publish (~1 GB Vite output),
	// dist, coverage, etc. must be excluded from the Stryker sandbox copy or the
	// main process OOMs reading the project tree. (node_modules / .git / .stryker-tmp /
	// reports / *.tsbuildinfo / stryker.log are always-ignored by Stryker core.)
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
	tempDirName: ".stryker-tmp",
	cleanTempDir: true,
};

export default config;
