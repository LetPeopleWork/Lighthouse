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
		// Stryker's coverage runs the whole suite per mutant; sweeping all ~299 files OOMs the
		// node heap, so the mutation run sees only the specs covering the mutated files (Story 5690).
		include: [
			"src/components/Common/FeatureListDataGrid/FeatureMoveMenu.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.moveActions.test.tsx",
			"src/components/Common/FeatureListDataGrid/columns.test.tsx",
			"src/components/Common/FeatureListDataGrid/columns.position.test.tsx",
			"src/hooks/useFeatureOrdering.moveGate.test.tsx",
			"src/hooks/useFeatureOrdering.test.tsx",
			"src/pages/Features/FeaturesView.test.tsx",
			"src/pages/Portfolios/Detail/PortfolioFeatureList.test.tsx",
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
