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
			"src/models/Delivery/DeliveryMetricsHistory.test.ts",
			"src/components/Common/Charts/DeliveryBurnupChart.test.tsx",
			"src/services/Api/DeliveryService.test.ts",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.metrics.test.tsx",
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
