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
		// Stryker runs the whole suite per mutant; sweeping all 292 files OOMs the node heap,
		// so the mutation run sees only the specs covering the mutated file (Bug #5628 precedent).
		include: [
			"src/components/Common/Charts/DeliveryBurnupChart.test.tsx",
			"src/components/Common/Charts/DeliveryBurnupChart.painting.test.tsx",
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
