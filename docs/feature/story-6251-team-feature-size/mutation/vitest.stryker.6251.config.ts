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
		// Stryker runs the whole include set per mutant, so a full sweep OOMs the node heap. Only the
		// specs covering the mutated files are listed, written from `ls`: a spec left out makes the
		// mutants in the code it covers survive for want of a test run, not for want of a test.
		include: [
			"src/components/Common/Charts/FeatureSizeScatterPlotChart.test.tsx",
			"src/components/Common/Charts/FeatureSizeScatterPlotChart.teamShare.test.tsx",
			"src/components/Common/Charts/FeatureSizeScatterPlotChart.terminology.test.tsx",
			"src/hooks/useMetricsData.test.ts",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx",
			"src/pages/Common/MetricsView/categoryMetadata.test.ts",
			"src/pages/Common/MetricsView/widgetInfoMetadata.test.ts",
			"src/pages/Common/MetricsView/FeatureSizePercentilesWidget.test.tsx",
			"src/services/Api/MetricsService.test.ts",
			"src/services/Api/ProjectMetricsService.test.ts",
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
