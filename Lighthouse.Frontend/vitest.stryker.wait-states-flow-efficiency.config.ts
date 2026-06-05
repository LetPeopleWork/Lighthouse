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
			"src/utils/flowEfficiency.test.ts",
			"src/pages/Common/MetricsView/ragRules.test.ts",
			"src/pages/Common/MetricsView/FlowEfficiencyOverviewWidget.test.tsx",
			"src/components/Common/StateMappings/WaitStatesEditor.test.tsx",
			"src/components/Common/Charts/CumulativeStateTimeChart.test.tsx",
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
