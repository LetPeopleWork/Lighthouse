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
		// Stryker's coverage runs the whole suite per mutant; sweeping all ~294 files OOMs the
		// node heap, so the mutation run sees only the specs covering the mutated files (Story 5688).
		include: [
			"src/components/Common/FeatureListDataGrid/columns.test.tsx",
			"src/components/Common/FeatureListDataGrid/columns.position.test.tsx",
			"src/components/Common/FeatureListDataGrid/FeatureListDataGrid.test.tsx",
			"src/components/App/Header/Header.test.tsx",
			"src/components/App/Header/Header.featuresNav.test.tsx",
			"src/pages/Features/FeaturesView.test.tsx",
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
				inline: [/@mui\//, /react-transition-group/],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
