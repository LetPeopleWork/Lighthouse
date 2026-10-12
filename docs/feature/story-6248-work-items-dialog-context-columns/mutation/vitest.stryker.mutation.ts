// @ts-nocheck
import { defineConfig } from "vitest/config";

// Copy to Lighthouse.Frontend/vitest.stryker.6248.ts before the run; the frontend config points there.
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
		// Stryker runs the include set per mutant, so a full sweep OOMs the node heap. Only the specs
		// that import a mutated file are listed: a spec left out makes every mutant in the code it covers
		// survive for want of a test run, and the report cannot tell that apart from a real gap.
		include: [
			"src/components/Common/WorkItemsDialog/*.test.ts",
			"src/components/Common/WorkItemsDialog/*.test.tsx",
			"src/components/Common/DataGrid/DataGridBase.test.tsx",
			"src/components/Common/DataGrid/DataGridBase.defaults.test.tsx",
			"src/components/Common/FeatureListDataGrid/featureChildItemsDialog.test.tsx",
			"src/components/Common/Charts/BarRunChart.test.tsx",
			"src/components/Common/Charts/BlockedItemsOverTimeChart.test.tsx",
			"src/components/Common/Charts/CycleTimeScatterPlotChart.test.tsx",
			"src/components/Common/Charts/EstimationVsCycleTimeChart.test.tsx",
			"src/components/Common/Charts/FeatureSizeScatterPlotChart.terminology.test.tsx",
			"src/components/Common/Charts/FeatureSizeScatterPlotChart.test.tsx",
			"src/components/Common/Charts/LineRunChart.dialog.test.tsx",
			"src/components/Common/Charts/LineRunChart.test.tsx",
			"src/components/Common/Charts/ProcessBehaviourChart.test.tsx",
			"src/components/Common/Charts/TotalWorkItemAgeRunChart.test.tsx",
			"src/components/Common/Charts/WorkDistributionChart.test.tsx",
			"src/components/Common/Charts/WorkItemAgingChart.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.cumulativeStateTimeDialog.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/pages/Common/MetricsView/PercentilesOverTimeWidget.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell.status.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.likelihoodCopy.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.overdue.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.publishRefused.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.sourceUnavailable.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.test.tsx",
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
