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
		// Only the specs that cover the mutated files run: the whole suite per mutant OOMs the node heap.
		include: [
			"src/components/Common/Charts/cycleTimeMeasure.test.ts",
			"src/components/Common/Charts/CumulativeStateTimeScopeControl.test.tsx",
			"src/components/Common/Charts/CycleTimePercentiles.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.namedMeasure.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.outlierChip.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.outlierList.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.outliers.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.outlierSwitch.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.outlierViews.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.verdictQuestion.test.tsx",
			"src/components/Common/WorkItemsDialog/contextGridLayout.test.ts",
			"src/components/Common/WorkItemsDialog/workItemsDialogContexts.test.ts",
			"src/components/Common/WorkItemsDialog/WorkItemsDialog.contexts.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.hooks.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/pages/Common/MetricsView/namedPercentilesViewData.test.tsx",
			"src/pages/Common/MetricsView/useNamedPercentilesScope.test.ts",
			"src/pages/Common/MetricsView/widgetStatus.test.ts",
			"src/pages/Common/MetricsView/widgetViewData.test.ts",
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
				inline: [/@mui\//, /react-transition-group/],
			},
		},
		pool: "threads",
		isolate: true,
	},
});
