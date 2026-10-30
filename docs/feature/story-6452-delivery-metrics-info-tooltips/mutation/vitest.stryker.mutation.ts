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
		// Stryker reruns the suite per mutant; the whole suite OOMs the node heap, so the mutation
		// run sees only the specs covering the mutated delivery chart files.
		include: [
			"src/components/Common/Charts/DeliveryBurnupChart.test.tsx",
			"src/components/Common/Charts/DeliveryBurnupChart.painting.test.tsx",
			"src/components/Common/Charts/DeliveryEpicSizeChart.test.tsx",
			"src/components/Common/Charts/DeliveryFeverChart.test.tsx",
			"src/components/Common/Charts/DeliveryPredictabilityChart.test.tsx",
			"src/components/Common/Charts/DeliveryPredictabilityChart.beyondTheHorizon.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryMetricsTab.test.tsx",
			"src/pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.metrics.test.tsx",
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
