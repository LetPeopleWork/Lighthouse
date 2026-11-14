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
		// Only the specs covering the mutated files are listed: a full sweep runs out of memory, and a
		// spec left out makes every mutant in the code it covers survive for want of a test run.
		include: [
			"src/components/Common/Charts/CycleTimeScatterPlotChart.outliers.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.outlierViews.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.outlierList.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.test.tsx",
			"src/utils/charts/outliers.test.ts",
			"src/pages/Common/MetricsView/widgetInfoMetadata.test.ts",
			"src/utils/charts/outlierWording.mutation.test.ts",
			"src/utils/charts/outliers.mutation.test.ts",
			"src/components/Common/Charts/outlierPlot.mutation.test.ts",
			"src/components/Common/Charts/widgetHeaderSlot.mutation.test.tsx",
			"src/components/Common/Charts/OffScaleLineMarkers.mutation.test.tsx",
			"src/components/Common/Charts/OutlierChip.mutation.test.tsx",
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
