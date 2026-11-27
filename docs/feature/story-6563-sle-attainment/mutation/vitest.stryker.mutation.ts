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
		// Only the specs that reach the Within SLE widgets, their hooks, the metrics view wiring and the
		// service calls run; the whole suite per mutant OOMs the node heap.
		include: [
			"src/hooks/useMetricsData.test.ts",
			"src/hooks/metricsDataModules.test.tsx",
			"src/models/Metrics/WithinSleMinimumClosed.test.ts",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.hooks.test.tsx",
			"src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell.status.test.tsx",
			"src/pages/Common/MetricsView/WidgetShell.infoPopover.test.tsx",
			"src/pages/Common/MetricsView/WithinSleOverTimeWidget.test.tsx",
			"src/pages/Common/MetricsView/WithinSleOverTimeWidget.filledIn.test.tsx",
			"src/pages/Common/MetricsView/WithinSleWidget.test.tsx",
			"src/pages/Common/MetricsView/WithinSleWidget.loading.test.tsx",
			"src/pages/Common/MetricsView/WithinSleWidget.verdicts.test.tsx",
			"src/pages/Common/MetricsView/categoryMetadata.test.ts",
			"src/pages/Common/MetricsView/categoryMetadata.withinSle.test.ts",
			"src/pages/Common/MetricsView/ragRulesTimeAndAge.test.ts",
			"src/pages/Common/MetricsView/widgetInfoMetadata.test.ts",
			"src/pages/Common/MetricsView/widgetStatus.test.ts",
			"src/pages/Common/MetricsView/widgetViewData.test.ts",
			"src/pages/Common/MetricsView/withinSleLines.filledIn.test.ts",
			"src/services/Api/MetricsService.test.ts",
			"src/services/Api/TeamMetricsService.test.ts",
			"src/services/Api/ProjectMetricsService.test.ts",
			"src/services/Api/MetricsService.withinSle.test.ts",
			"src/pages/Common/MetricsView/withinSleLines.ordering.test.ts",
			"src/pages/Common/MetricsView/withinSleFooterAndMetadata.test.ts",
			"src/pages/Common/MetricsView/WithinSleWidget.oneVerdict.test.tsx",
			"src/pages/Common/MetricsView/WithinSleShareMark.test.tsx",
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
