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
			"src/components/Common/Charts/WorkItemAgingChart.test.tsx",
			"src/components/Common/Charts/PercentileLegend.test.tsx",
			"src/services/Api/MetricsService.test.ts",
			"src/hooks/useMetricsData.test.ts",
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
