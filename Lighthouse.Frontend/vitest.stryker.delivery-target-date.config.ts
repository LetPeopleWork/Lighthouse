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
			"src/models/Delivery/deliveryTargetHistory.test.ts",
			"src/models/Delivery/DeliveryMetricsHistory.test.ts",
			"src/components/Common/Charts/DeliveryPredictabilityChart.test.tsx",
			"src/components/Common/Charts/DeliveryBurnupChart.test.tsx",
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
