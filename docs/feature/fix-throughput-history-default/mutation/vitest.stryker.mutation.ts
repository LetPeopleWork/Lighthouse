// @ts-nocheck
import { defineConfig } from "vitest/config";

export default defineConfig({
	test: {
		globals: true,
		environment: "jsdom",
		setupFiles: ["./setupTests.ts"],
		env: {
			VITE_API_SERVICE_TYPE: "DEMO",
		},
		css: {
			modules: {
				classNameStrategy: "non-scoped",
			},
		},
		// Stryker's coverage runs the whole suite per mutant; sweeping all 282 files OOMs the
		// node heap, so the mutation run sees only the specs covering the mutated files.
		include: [
			"src/pages/Teams/Edit/EditTeam.test.tsx",
			"src/pages/Portfolios/Edit/EditPortfolio.test.tsx",
			"src/components/Common/QuickSettings/ThroughputQuickSetting.test.tsx",
			"src/components/Common/ProjectSettings/Advanced/FeatureSizeComponent.test.tsx",
			"src/components/Common/CreateWizards/CreateTeamWizard.test.tsx",
			"src/components/Common/CreateWizards/CreatePortfolioWizard.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
