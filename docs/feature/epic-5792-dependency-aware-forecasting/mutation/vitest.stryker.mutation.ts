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
		// Stryker's coverage runs the whole suite per mutant; sweeping every spec OOMs the node
		// heap, so the mutation run sees only the specs covering the mutated file (Story #5784).
		include: [
			"src/utils/dependencies/dependencySentences.test.ts",
			"src/utils/features/featureWarningSentences.test.ts",
			"src/models/FeatureDependency.test.ts",
			"src/components/Common/FeatureListDataGrid/WarningsIndicator.test.tsx",
			"src/components/Common/FeatureListDataGrid/columns.dependsOn.test.tsx",
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
