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
			"src/components/Common/Charts/CumulativeStateTimeChart.test.tsx",
			"src/components/Common/Charts/CumulativeStateTimeItemPicker.test.tsx",
			"src/utils/date/formatDuration.test.ts",
			"src/pages/Common/MetricsView/ragRules.test.ts",
			"src/services/Api/MetricsService.test.ts",
			"src/hooks/useMetricsData.test.ts",
			"src/pages/Common/MetricsView/BaseMetricsView.test.tsx",
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
