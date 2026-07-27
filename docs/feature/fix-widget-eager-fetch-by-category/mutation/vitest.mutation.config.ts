import { defineConfig } from "vitest/config";

// Narrow config used ONLY by the Bug #5571 feature-scoped Stryker run.
// The default config's reporters (sonar XML + JSON files) and the full 3746-test
// include set make every static mutant pay a ~32s full re-run; restricting the
// include set to the feature's own suites is what makes the run finish in minutes.
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
		include: [
			"src/pages/Common/MetricsView/categoryMetadata.test.ts",
			"src/pages/Common/MetricsView/useCategorySelection.test.ts",
			"src/hooks/useMetricsData.test.ts",
			"src/components/Common/Charts/TotalWorkItemAgeWidget.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
		],
		exclude: [
			"**/node_modules/**",
			"**/dist/**",
			"**/.stryker-tmp*/**",
			"**/StrykerOutput/**",
		],
		reporters: ["default"],
		server: {
			deps: {
				inline: [/@mui\//, /react-transition-group/],
			},
		},
		pool: "threads",
		isolate: true,
		fileParallelism: true,
	},
});
