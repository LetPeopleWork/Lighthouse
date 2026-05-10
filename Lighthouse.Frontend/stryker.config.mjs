// @ts-check
/** @type {import('@stryker-mutator/api/core').PartialStrykerOptions} */
const config = {
	packageManager: "pnpm",
	testRunner: "vitest",
	// typescript checker disabled: pnpm's symlinked node_modules layout prevents
	// the child workers from resolving the checker plugin. TS errors caused by
	// mutations surface as runtime test failures, which is sufficient for the
	// per-feature mutation gate.
	checkers: [],
	reporters: ["progress", "clear-text", "html", "json"],
	coverageAnalysis: "perTest",
	concurrency: 1,
	timeoutMS: 60000,
	timeoutFactor: 2,
	disableTypeChecks: true,
	thresholds: {
		high: 80,
		low: 70,
		break: 0,
	},
	// High-value subset (per per-feature strategy): focus on files with the most
	// new business logic touched by this feature. Gating-only files (Settings.tsx,
	// SystemSettingsTab.tsx, OverviewDashboard.tsx, TeamDetail.tsx,
	// PortfolioDetail.tsx, PortfolioDeliveryView.tsx, DeliverySection.tsx) are
	// shallow conditional renders driven by useRbac() and were downscoped.
	// Highest-value subset: ScopedGroupMappingManager (ADR-002 fix logic) and
	// RbacService (HTTP API contract). RbacSettings.tsx remains worth covering
	// but its render-heavy tests pushed memory above 8GB; documented as
	// follow-up in mutation-report.md.
	mutate: [
		"src/components/Common/Authorization/ScopedGroupMappingManager.tsx",
		"src/services/Api/RbacService.ts",
	],
	vitest: {
		// scoped vitest config with `include` restricted to the 2 relevant
		// test files — without this, vitest loads all 2668 tests' modules at
		// dry-run time which exceeds the Node heap limit.
		configFile: "vitest.stryker.config.ts",
	},
	tempDirName: ".stryker-tmp",
	cleanTempDir: true,
};

export default config;
