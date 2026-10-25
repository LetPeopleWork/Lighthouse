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
		// Stryker reruns this set for every mutant, so it lists only the suites that reach the one Features
		// list: its hook, the Portfolio and Team tabs that draw it, the grid they hand it to, and the service
		// call. A suite left out makes the mutants it covers survive for want of a run, not for want of a test.
		include: [
			"src/hooks/useFeatureList.test.tsx",
			"src/hooks/useFeatureList.refresh.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioFeatureList.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioFeatureList.oneList.test.tsx",
			"src/pages/Teams/Detail/TeamFeatureList.test.tsx",
			"src/pages/Teams/Detail/TeamFeatureList.oneList.test.tsx",
			"src/pages/Teams/Detail/TeamFeatureList.rows.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.moveActions.test.tsx",
			"src/services/Api/FeatureService.test.ts",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/, /@svar-ui\//],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
