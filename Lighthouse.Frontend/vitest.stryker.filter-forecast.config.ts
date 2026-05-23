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
		include: [
			"src/components/Teams/ForecastFilterEditor/ForecastFilterEditor.test.tsx",
			"src/components/Common/Forecasting/FilteredThroughputChip.test.tsx",
			"src/components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle.test.tsx",
			"src/components/Common/Charts/ThroughputChart/evaluateCondition.test.ts",
			"src/pages/Teams/Edit/ForecastSettingsComponent.test.tsx",
			"src/pages/Teams/Detail/ManualForecaster.test.tsx",
			"src/pages/Teams/Detail/BacktestForecaster.test.tsx",
			"src/pages/Teams/Detail/TeamForecastView.test.tsx",
			"src/pages/Teams/Detail/TeamMetricsView.test.tsx",
			"src/pages/Common/MetricsView/TotalThroughputWidget.test.tsx",
		],
		server: {
			deps: {
				inline: ["@mui/x-data-grid"],
			},
		},
		pool: "forks",
		isolate: true,
		fileParallelism: false,
	},
});
